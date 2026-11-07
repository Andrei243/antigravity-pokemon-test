using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim.Link;

/// <summary>Who answers a player's questions (plan 28 · I3): this machine's menus, an AI, or a test's own rule.</summary>
public interface ILinkChooser
{
    /// <summary>The positions of the six brought that go in, in order: as many as the room picks.</summary>
    IReadOnlyList<int> Pick(LinkHello hello, int brought);

    /// <summary>One choice for each place asked, from what the question says each may answer.</summary>
    IReadOnlyList<BattleChoice> Choose(LinkClient client, LinkAsk ask);
}

/// <summary>
/// What a player's machine knows of a link battle (plan 28 · I3): only what it was sent. Its own team (rebuilt from
/// what it brought, as the server rebuilt it), the foe's six as the preview's stand-ins, the view's events as they come,
/// and what they add up to (<see cref="ViewState"/>). It never runs the rules and never sees the other side's menus:
/// while the other side is asked it shows <see cref="WaitingLine"/>, and the turn plays when the server's next view
/// arrives. GPU-free; the screen that plays a view (a <see cref="BattleEngine"/> fed from outside) is still to come.
/// </summary>
public sealed class LinkClient
{
    public string Name { get; }
    public IReadOnlyList<RecordedPokemon> Team { get; }

    /// <summary>Answers by itself as each question comes; off, a question waits in <see cref="Asked"/> for <see cref="Answer"/>.</summary>
    public bool AnswersByItself { get; set; } = true;

    public LinkHello? Hello { get; private set; }
    public BattleSide Side => Hello?.Side ?? BattleSide.Player;

    /// <summary>The player's own Pokémon in the order they went in, as copies at the room's level.</summary>
    public IReadOnlyList<Pokemon> Own => own;

    /// <summary>The foe's six as the preview showed them.</summary>
    public IReadOnlyList<Pokemon> FoeStandIns => foe;

    /// <summary>Every event of the view, in order.</summary>
    public IReadOnlyList<BattleEvent> Events => events;

    public ViewState State { get; } = new();
    public LinkAsk? Asked { get; private set; }
    public LinkResult? Result { get; private set; }
    public IReadOnlyList<string> Refusals => refusals;

    private readonly ILinkChannel channel;
    private readonly ILinkChooser chooser;
    private readonly List<Pokemon> own = new(), foe = new();
    private readonly List<BattleEvent> events = new();
    private readonly List<string> refusals = new();
    private BattleRoster? roster;

    /// <summary>The names the view's Pokémon go by on the wire, once the room has greeted the player.</summary>
    public BattleRoster? Roster => roster;

    public LinkClient(string name, IReadOnlyList<RecordedPokemon> team, ILinkChannel channel, ILinkChooser chooser)
    {
        Name = name;
        Team = team;
        this.channel = channel;
        this.chooser = chooser;
    }

    /// <summary>The line the screen shows while the other player chooses (plan 28 · I3); empty while it is this player's turn or the battle is over.</summary>
    public string WaitingLine => Hello == null || Asked != null || Result != null ? "" : Text.Of("linkRoom.waiting", ("player", Hello.FoeName));

    /// <summary>Takes in everything the server has sent.</summary>
    public void Pump()
    {
        while (channel.TryReceive(out var text)) Receive(text);
    }

    private void Receive(string text)
    {
        var message = BattleWire.ReadMessage(text, roster);
        switch (message)
        {
            case LinkHello hello:
                Greeted(hello);
                break;
            case LinkTurn turn:
                events.AddRange(turn.Events);
                State.Apply(turn.Events, Side);
                break;
            case LinkAsk ask:
                Asked = ask;
                if (AnswersByItself) Answer(chooser.Choose(this, ask));
                break;
            case LinkRefused refused:
                refusals.Add(refused.Reason);
                break;
            case LinkOver over:
                Result = over.Result;
                Asked = null;
                break;
        }
    }

    private void Greeted(LinkHello hello)
    {
        Hello = hello;
        foe.Clear();
        foe.AddRange(hello.FoePreview.Select(TeamPreview.StandIn));
        // Brought back after a drop, the order is the one picked before, and the whole view so far follows
        var order = hello.Picked ?? chooser.Pick(hello, Team.Count);
        var copies = LinkSetup.Copies(hello.Room, Team);
        own.Clear();
        own.AddRange(order.Select(i => copies[i]));
        events.Clear();
        roster = new BattleRoster(BattleRoster.Team(Side, 0, own).Concat(BattleRoster.Team(Other, 0, foe)));
        State.Begin(own, foe);
        if (hello.Picked == null) channel.Send(BattleWire.WriteMessage(new LinkPick(order), null));
    }

    private BattleSide Other => Side == BattleSide.Player ? BattleSide.Enemy : BattleSide.Player;

    /// <summary>Answers the question asked.</summary>
    public void Answer(IReadOnlyList<BattleChoice> choices)
    {
        if (Asked == null) return;
        channel.Send(BattleWire.WriteMessage(new LinkAnswer(Asked.Number, choices), null));
        Asked = null;
    }

    /// <summary>Gives the match up.</summary>
    public void Forfeit() => channel.Send(BattleWire.WriteMessage(new LinkForfeit(), null));
}

/// <summary>
/// What a view adds up to (plan 28 · I3): who stands where, each Pokémon's HP as the view tells it (a foe's in 48ths),
/// who has fainted, what each foe has shown of its moves, item and ability, and how the battle ended. Made from the
/// events alone, so two players' states can be held against one another and against the rules'. GPU-free.
/// </summary>
public sealed class ViewState
{
    public Dictionary<Place, Pokemon> Standing { get; } = new();
    public Dictionary<Pokemon, int> Hp { get; } = new(ReferenceEqualityComparer.Instance);
    public HashSet<Pokemon> Fainted { get; } = new(ReferenceEqualityComparer.Instance);
    public Dictionary<Pokemon, HashSet<string>> MovesShown { get; } = new(ReferenceEqualityComparer.Instance);
    public Dictionary<Pokemon, string> ItemsShown { get; } = new(ReferenceEqualityComparer.Instance);
    public Dictionary<Pokemon, string> AbilitiesShown { get; } = new(ReferenceEqualityComparer.Instance);
    public BattleResult Result { get; private set; }

    private readonly List<Pokemon> own = new(), foes = new();

    /// <summary>Starts from the teams: the player's own whole, the foe's bars full.</summary>
    public void Begin(IEnumerable<Pokemon> mine, IEnumerable<Pokemon> foe)
    {
        own.Clear();
        own.AddRange(mine);
        foes.Clear();
        foes.AddRange(foe);
        Clear();
    }

    public void Clear()
    {
        Standing.Clear();
        Hp.Clear();
        Fainted.Clear();
        MovesShown.Clear();
        ItemsShown.Clear();
        AbilitiesShown.Clear();
        Result = BattleResult.None;
        foreach (var p in own) Hp[p] = p.MaxHP;
        foreach (var p in foes) Hp[p] = SideView.BarPixels;
    }

    public void Apply(IEnumerable<BattleEvent> events, BattleSide side)
    {
        foreach (var e in events) Apply(e, side);
    }

    private void Apply(BattleEvent e, BattleSide side)
    {
        switch (e)
        {
            case Said said:
                foreach (var inner in said.Shows) Apply(inner, side);
                foreach (var inner in said.OnImpact) Apply(inner, side);
                break;
            case Entered entered:
                Standing[entered.Place] = entered.Pokemon;
                Hp.TryAdd(entered.Pokemon, entered.Pokemon.MaxHP);
                break;
            case Rotated rotated:
                Standing[rotated.Place] = rotated.Pokemon;
                break;
            case PlacesSwapped swapped:
                bool hasA = Standing.Remove(swapped.First, out var a), hasB = Standing.Remove(swapped.Second, out var b);
                if (hasA) Standing[swapped.Second] = a!;
                if (hasB) Standing[swapped.First] = b!;
                break;
            case Struck struck when Standing.TryGetValue(struck.Place, out var hit):
                Hp[hit] = struck.Hp;
                break;
            case HpChanged changed when Standing.TryGetValue(changed.Place, out var changedOne):
                Hp[changedOne] = changed.Hp;
                break;
            case Dynamaxed big when Standing.TryGetValue(big.Place, out var grown):
                Hp[grown] = big.Hp;
                break;
            case DynamaxEnded small when Standing.TryGetValue(small.Place, out var shrunk):
                Hp[shrunk] = small.Hp;
                break;
            case Fainted fainted when Standing.TryGetValue(fainted.Place, out var down):
                Fainted.Add(down);
                Hp[down] = 0;
                break;
            case MoveShown move when move.From.Side != side && Standing.TryGetValue(move.From, out var user):
                Shown(MovesShown, user).Add(move.Move);
                break;
            case ItemShown item when Standing.TryGetValue(item.Place, out var holder):
                ItemsShown[holder] = item.Item;
                break;
            case AbilityShown ability when Standing.TryGetValue(ability.Place, out var bearer):
                AbilitiesShown[bearer] = ability.Ability;
                break;
            case Ended ended:
                Result = ended.Result;
                break;
        }
    }

    private static HashSet<string> Shown(Dictionary<Pokemon, HashSet<string>> shown, Pokemon p)
    {
        if (!shown.TryGetValue(p, out var set)) shown[p] = set = new HashSet<string>();
        return set;
    }
}

/// <summary>
/// A chooser for the tests and the bench (plan 28 · I3): picks at random among what the question allows, from a seed and
/// the question's number alone, so the same battle asks it the same way every time (and again after a resume).
/// </summary>
public sealed class SeededChooser : ILinkChooser
{
    private readonly uint seed;

    public SeededChooser(uint seed) => this.seed = seed;

    public IReadOnlyList<int> Pick(LinkHello hello, int brought)
    {
        var rng = new BattleRandom(seed ^ 0x5EED5EEDu);
        var order = Enumerable.Range(0, brought).ToList();
        for (int i = order.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }
        return order.Take(System.Math.Min(hello.Room.Picks, brought)).ToList();
    }

    public IReadOnlyList<BattleChoice> Choose(LinkClient client, LinkAsk ask)
    {
        var rng = new BattleRandom(unchecked(seed * 0x9E3779B1u ^ (uint)ask.Number * 0x85EBCA6Bu));
        var choices = new List<BattleChoice>();
        var taken = new HashSet<int>();
        foreach (var option in ask.Options)
        {
            var switches = option.Switches.Where(i => !taken.Contains(i)).ToList();
            if (ask.Request is SwitchOfferRequest)
            {
                choices.Add(BattleChoice.Stay(option.Place));
                continue;
            }
            bool switching = switches.Count > 0 && (option.MustSendIn || option.Moves.Count == 0 && !option.Struggle || rng.Next(8) == 0);
            if (switching)
            {
                int to = switches[rng.Next(switches.Count)];
                taken.Add(to);
                choices.Add(BattleChoice.Switch(option.Place, to));
            }
            else choices.Add(BattleChoice.Fight(option.Place, option.Moves.Count > 0 ? option.Moves[rng.Next(option.Moves.Count)] : -1));
        }
        return choices;
    }
}
