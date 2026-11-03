using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Battle.Sim.Link;

/// <summary>
/// The server's half of a link battle (plan 28 · I3), with no network of its own: it holds one <see cref="ILinkChannel"/>
/// to each player, runs the only <see cref="BattleCore"/> (the only one who rolls), sends each player only their own
/// side's view (<see cref="SideView"/>) and only their own places' questions, waits for both answers before it works a
/// turn out, and never sends one player's choice to the other. Plan 07 · O6 puts it behind the real server; the tests
/// put it behind a <see cref="LoopbackLink"/>.
/// <para>
/// Its course: both players are greeted with the room and the other's team preview (<see cref="LinkHello"/>), each picks
/// their order (<see cref="LinkPick"/>), the battle is made (<see cref="LinkSetup.Battle"/>) and played: after every
/// answer the log's new part goes out as each side's <see cref="LinkTurn"/>, then each side asked gets its
/// <see cref="LinkAsk"/>. A side may give up at any time (<see cref="LinkForfeit"/>); a turn's timer that runs out
/// answers with <see cref="BattleCore.DefaultChoice"/> (<see cref="TimeOut"/>), and the third in a row gives the match up.
/// A player who drops can be brought back to the same battle (<see cref="Rejoin"/>). GPU-free, and with no clock: whoever
/// keeps the timer says when it has run out.
/// </para>
/// </summary>
public sealed class LinkServer
{
    /// <summary>Turns a player's timer may run out in a row before the match is given up for them.</summary>
    public const int TimeoutsAllowed = 3;

    public LinkSetup Setup { get; }
    public uint Seed { get; }

    /// <summary>The battle, once both players have picked.</summary>
    public BattleCore? Core { get; private set; }

    /// <summary>The battle as it began, the record of a battle kept to be resumed (plan 28 · I5): null until it begins.</summary>
    public BattleRecordFile? Opening { get; private set; }

    /// <summary>How it ended, once it has.</summary>
    public LinkResult? Result { get; private set; }

    /// <summary>What each side picked, once it has.</summary>
    public IReadOnlyList<int>? PickOf(BattleSide side) => picks[Index(side)];

    /// <summary>Each side's view of the battle, once it has begun.</summary>
    public SideView? ViewOf(BattleSide side) => views[Index(side)];

    private readonly ILinkChannel[] channels;
    private readonly IReadOnlyList<int>?[] picks = new IReadOnlyList<int>?[2];
    private readonly SideView?[] views = new SideView?[2];
    private readonly List<BattleEvent>[] shown = { new(), new() };
    private readonly int[] timeouts = new int[2];
    private readonly Dictionary<BattleSide, IReadOnlyList<BattleChoice>> answered = new();
    private readonly HashSet<BattleSide> waitingOn = new();
    private int asking;
    private LinkEnding ending = LinkEnding.KnockedOut;

    /// <param name="setup">The room and the two teams brought (<see cref="LinkSetup.Bring"/>).</param>
    /// <param name="seed">Where the battle's chance begins: the same setup, picks, answers and seed are the same battle.</param>
    /// <param name="a">The channel to side A, the core's player side.</param>
    /// <param name="b">The channel to side B, its other side.</param>
    public LinkServer(LinkSetup setup, uint seed, ILinkChannel a, ILinkChannel b)
    {
        Setup = setup;
        Seed = seed;
        channels = new[] { a, b };
        Greet(BattleSide.Player);
        Greet(BattleSide.Enemy);
    }

    private static int Index(BattleSide side) => side == BattleSide.Player ? 0 : 1;
    private static BattleSide Other(BattleSide side) => side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;

    private void Greet(BattleSide side) =>
        Send(side, new LinkHello(BattleWire.Version, side, (side == BattleSide.Player ? Setup.B : Setup.A).Name, Setup.Room, Setup.PreviewFor(side), picks[Index(side)]));

    private void Send(BattleSide side, LinkMessage message) =>
        channels[Index(side)].Send(BattleWire.WriteMessage(message, views[Index(side)]?.Roster));

    /// <summary>Takes in everything both players have sent, in turn, and answers it.</summary>
    public void Pump()
    {
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
            while (channels[Index(side)].TryReceive(out var text)) Receive(side, text);
    }

    /// <summary>One message from a side.</summary>
    public void Receive(BattleSide side, string text)
    {
        LinkMessage message;
        try
        {
            // What a player sends names no Pokémon: answers go by places and positions
            message = BattleWire.ReadMessage(text, null);
        }
        catch (BattleWireException e)
        {
            Send(side, new LinkRefused(e.Message));
            return;
        }
        switch (message)
        {
            case LinkPick pick: TakePick(side, pick.Order); break;
            case LinkAnswer answer: TakeAnswer(side, answer); break;
            case LinkForfeit: GiveUp(side, LinkEnding.Forfeit); break;
            default: Send(side, new LinkRefused(Text.Of("linkRoom.refused.notAsked"))); break;
        }
    }

    private void TakePick(BattleSide side, IReadOnlyList<int> order)
    {
        if (Core != null || Result != null || picks[Index(side)] != null)
        {
            Send(side, new LinkRefused(Text.Of("linkRoom.refused.notAsked")));
            return;
        }
        if (Setup.WhyNot(order, side) is { } refused)
        {
            Send(side, new LinkRefused(refused));
            return;
        }
        picks[Index(side)] = order.ToList();
        if (picks[0] != null && picks[1] != null) Begin();
    }

    private void Begin()
    {
        var setup = Setup.Battle(picks[0]!, picks[1]!, Seed);
        Opening = BattleRecordFile.Opening(setup);
        Core = new BattleCore(setup);
        views[0] = new SideView(BattleSide.Player, new[] { Core.PlayerParty.Members }, Core.Trainers[0].Party.Members);
        views[1] = new SideView(BattleSide.Enemy, new[] { Core.Trainers[0].Party.Members }, Core.PlayerParty.Members);
        Core.Start();
        Flush();
    }

    /// <summary>The log's new part out to each side as its view, then the next question to whoever it is for, or the end.</summary>
    private void Flush()
    {
        var core = Core!;
        var fresh = core.TakeLog();
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
        {
            var view = views[Index(side)]!.Filter(fresh);
            if (view.Count == 0) continue;
            shown[Index(side)].AddRange(view);
            Send(side, new LinkTurn(view));
        }
        if (core.Request == null)
        {
            Result = LinkSetup.ResultOf(core, ending);
            Send(BattleSide.Player, new LinkOver(Result));
            Send(BattleSide.Enemy, new LinkOver(Result));
            return;
        }
        asking++;
        answered.Clear();
        waitingOn.Clear();
        foreach (var side in new[] { BattleSide.Player, BattleSide.Enemy })
        {
            if (PlacesOf(side) is not { Count: > 0 }) continue;
            waitingOn.Add(side);
            Send(side, AskFor(side));
        }
    }

    /// <summary>The places of a side the question is for, in the question's order.</summary>
    public IReadOnlyList<Place> PlacesOf(BattleSide side) => Core?.Request switch
    {
        ActionRequest action => action.Places.Where(p => p.Side == side).ToList(),
        ReplacementRequest replacement when replacement.Place.Side == side => new[] { replacement.Place },
        SwitchOfferRequest offer when offer.Place.Side == side => new[] { offer.Place },
        _ => Array.Empty<Place>()
    };

    private LinkAsk AskFor(BattleSide side)
    {
        var places = PlacesOf(side);
        return new LinkAsk(asking, views[Index(side)]!.Request(Core!.Request!, places), places.Select(OptionsOf).ToList());
    }

    /// <summary>What a place may answer, by the rules' own refusals.</summary>
    private PlaceOptions OptionsOf(Place place)
    {
        var core = Core!;
        var b = core.At(place);
        var party = Enumerable.Range(0, b.Roster?.Count ?? 0);
        if (core.Request is ReplacementRequest)
            return new PlaceOptions(place, Array.Empty<int>(), false, party.Where(i => core.CanSendIn(b, i)).ToList(), MustSendIn: true);
        var switches = party.Where(i => core.WhyNot(BattleChoice.Switch(place, i)) == null).ToList();
        if (core.Request is SwitchOfferRequest) return new PlaceOptions(place, Array.Empty<int>(), false, switches, false);
        var moves = Enumerable.Range(0, b.Pokemon!.Moves.Count).Where(i => core.WhyNot(BattleChoice.Fight(place, i)) == null).ToList();
        bool struggle = moves.Count == 0 && core.WhyNot(BattleChoice.Fight(place, -1)) == null;
        return new PlaceOptions(place, moves, struggle, switches, false);
    }

    private void TakeAnswer(BattleSide side, LinkAnswer answer)
    {
        if (Core == null || Result != null || !waitingOn.Contains(side))
        {
            Send(side, new LinkRefused(Text.Of("linkRoom.refused.notAsked")));
            return;
        }
        if (answer.Number != asking)
        {
            Send(side, new LinkRefused(Text.Of("linkRoom.refused.stale")));
            return;
        }
        var places = PlacesOf(side);
        var choices = answer.Choices;
        if (choices.Count != places.Count || places.Any(p => choices.Count(c => c.Who == p) != 1) || choices.Any(c => c.Kind == ChoiceKind.Forfeit))
        {
            Send(side, new LinkRefused(Text.Of("linkRoom.refused.places")));
            return;
        }
        foreach (var choice in choices)
        {
            string? why = Core.Request is ReplacementRequest
                ? choice.Kind == ChoiceKind.Switch && Core.CanSendIn(Core.At(choice.Who), choice.SwitchTo) ? null : Text.Of("battle.thatPokemonCantBe")
                : Core.WhyNot(choice);
            if (why != null)
            {
                Send(side, new LinkRefused(why));
                return;
            }
        }
        answered[side] = choices;
        waitingOn.Remove(side);
        timeouts[Index(side)] = 0;
        if (waitingOn.Count > 0) return;

        // Both answers in: the turn is worked out with them in the question's own order
        var all = answered.Values.SelectMany(c => c).ToList();
        var order = Core.Request switch
        {
            ActionRequest action => action.Places.Select(p => all.Single(c => c.Who == p)).ToList(),
            _ => all
        };
        try
        {
            Core.Submit(order);
        }
        catch (ArgumentException e)
        {
            // Two answers that are each allowed and not together (one Mega Evolution a trainer): the question stands
            answered.Remove(side);
            waitingOn.Add(side);
            Send(side, new LinkRefused(e.Message));
            return;
        }
        Flush();
    }

    /// <summary>
    /// A side's turn timer ran out (plan 28 · I4): its places are answered with <see cref="BattleCore.DefaultChoice"/>, and
    /// the <see cref="TimeoutsAllowed"/>th time in a row the match is given up for it. Nothing when it isn't being asked.
    /// </summary>
    public void TimeOut(BattleSide side)
    {
        if (Core == null || Result != null || !waitingOn.Contains(side)) return;
        if (++timeouts[Index(side)] >= TimeoutsAllowed)
        {
            GiveUp(side, LinkEnding.Timer);
            return;
        }
        int kept = timeouts[Index(side)];
        TakeAnswer(side, new LinkAnswer(asking, PlacesOf(side).Select(Core.DefaultChoice).ToList()));
        timeouts[Index(side)] = kept;
    }

    /// <summary>A player's link dropped and didn't come back in time: the match is theirs to lose.</summary>
    public void Disconnect(BattleSide side) => GiveUp(side, LinkEnding.Disconnect);

    private void GiveUp(BattleSide side, LinkEnding how)
    {
        if (Result != null) return;
        ending = how;
        if (Core == null)
        {
            // Before the first turn: there was no battle, and the other side wins nothing but the match
            Result = new LinkResult(Other(side), 0, how);
            Send(BattleSide.Player, new LinkOver(Result));
            Send(BattleSide.Enemy, new LinkOver(Result));
            return;
        }
        Core.Submit(BattleChoice.Forfeit(new Place(side, 0)));
        Flush();
    }

    /// <summary>
    /// A player who dropped is back on a new channel: greeted again, sent their whole view so far and the question they
    /// owe, if any. The battle never stopped; the other player sees nothing of it.
    /// </summary>
    public void Rejoin(BattleSide side, ILinkChannel channel)
    {
        channels[Index(side)] = channel;
        Greet(side);
        if (shown[Index(side)].Count > 0) Send(side, new LinkTurn(shown[Index(side)].ToList()));
        if (Result != null) Send(side, new LinkOver(Result));
        else if (waitingOn.Contains(side)) Send(side, AskFor(side));
    }
}
