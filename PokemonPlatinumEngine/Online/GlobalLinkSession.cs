using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.Online;

/// <summary>Where a visit to the Global Link counter stands (plan 07 · O5, O7: the game's half).</summary>
public enum LinkPhase
{
    /// <summary>The connection is being made and the server greeted.</summary>
    Connecting,
    /// <summary>Connected: the friend code shown, a code to make or to type.</summary>
    Menu,
    /// <summary>The digits of the other player's code being typed.</summary>
    EnterCode,
    /// <summary>A code asked for, not yet given.</summary>
    MakingCode,
    /// <summary>A code made and shown, the other player not yet in.</summary>
    WaitingForOther,
    /// <summary>A typed code sent, the room not yet joined.</summary>
    Joining,
    /// <summary>In the trade room: a Pokémon to choose, or chosen and both offers to be confirmed.</summary>
    Room,
    /// <summary>A receipt being applied to the save.</summary>
    Trading,
    /// <summary>A trade done and saved: what came is shown until the player reads on.</summary>
    Traded,
    /// <summary>Something went wrong that ends the visit (no server, another version, a dropped connection): its line is shown.</summary>
    Failed,
    /// <summary>The visit is over and the connection closed.</summary>
    Closed,
    /// <summary>In a battle room (plan 07 · O6): the team to bring.</summary>
    BattleRoom,
    /// <summary>A team brought: the battle is under way (its screen is shown once the room greets the player).</summary>
    Battling,
    /// <summary>The battle over: how it went is shown until the player reads on.</summary>
    Battled
}

/// <summary>
/// A visit to the Global Link counter (plan 07 · O5 and O7, the game's half; <c>docs/mechanics/rulings.md</c>, "The
/// Global Link counter"): one <see cref="OnlineClient"/> from the counter to the server and back, the friend code,
/// a room made or joined by link code, and a trade in it, applied to the save through an <see cref="ITradeKeeper"/>.
/// GPU-free and with no input or clock: the screen calls its methods (<see cref="MakeCode"/>, <see cref="TypeDigit"/>,
/// <see cref="Offer"/>, <see cref="ConfirmTrade"/>…) and <see cref="Update"/> once a frame with the frame's time, which
/// takes what the server sent and finishes what was awaited. Every line it shows is a key of <c>ui.json</c>'s
/// <c>globalLink</c> section (<see cref="Line"/>), so the screen draws it and a test reads it.
/// </summary>
public sealed class GlobalLinkSession
{
    /// <summary>How long a connection or a question to the server is given before the counter gives up.</summary>
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(15);

    private readonly Func<OnlineClient>? makeClient;
    private readonly ITradeKeeper keeper;
    private OnlineClient? client;
    private Task<Welcome>? connecting;
    private Task<ReceiptOutcome>? applying;
    private TradeReceipt? receipt;
    private bool receiptNow;
    // Receipts to apply, each with whether it is the trade of the room now (or one an earlier visit left unfinished)
    private readonly Queue<(TradeReceipt Receipt, bool Now)> pending = new();
    private bool greeted;
    // The kind of room the code being made is for
    private RoomKind making = RoomKind.Trade;

    /// <param name="makeClient">Makes the client to the server; null when no server is set, and the visit fails at once with its line.</param>
    /// <param name="keeper">The save the trades are applied to.</param>
    /// <param name="refused">A line's key that keeps the counter shut before anything is sent (a run that has ended can't be saved, so it can't trade); null to connect.</param>
    public GlobalLinkSession(Func<OnlineClient>? makeClient, ITradeKeeper keeper, string? refused = null)
    {
        this.makeClient = makeClient;
        this.keeper = keeper;
        if (refused != null) Fail(refused);
        else if (makeClient == null) Fail("globalLink.error.noServer");
        else Phase = LinkPhase.Connecting;
    }

    public LinkPhase Phase { get; private set; }

    /// <summary>This player's friend code, once greeted.</summary>
    public string? FriendCode { get; private set; }

    /// <summary>The code of the room made, while it waits for the other player.</summary>
    public string? Code { get; private set; }

    /// <summary>How many seconds the code made still works, counted down by <see cref="Update"/>.</summary>
    public float CodeSecondsLeft { get; private set; }

    /// <summary>The digits typed so far, at most <see cref="OnlineRules.CodeDigits"/>.</summary>
    public string Typed { get; private set; } = "";

    /// <summary>Who is in the room with this player, once it is joined.</summary>
    public string? OtherName { get; private set; }
    public string? OtherFriendCode { get; private set; }

    /// <summary>What this player offers, once the server has checked it; and what the other offers.</summary>
    public SavedPokemonData? MyOffer { get; private set; }
    public SavedPokemonData? TheirOffer { get; private set; }

    /// <summary>Whether an offer was sent and the server's check is awaited.</summary>
    public bool Checking { get; private set; }

    public bool MineConfirmed { get; private set; }
    public bool TheirsConfirmed { get; private set; }

    /// <summary>Whether the other player's connection dropped and the room waits for them.</summary>
    public bool OtherAway { get; private set; }

    /// <summary>The Pokémon the last trade brought, while <see cref="LinkPhase.Traded"/> shows it.</summary>
    public SavedPokemonData? Received { get; private set; }

    /// <summary>How many trades from before (the welcome's receipts) were applied as the counter connected.</summary>
    public int Caught { get; private set; }

    /// <summary>The line said now, as a key of <c>ui.json</c> with its values, or null; <see cref="LineText"/> says it.</summary>
    public string? Line { get; private set; }
    public (string Name, object Value)[] LineValues { get; private set; } = Array.Empty<(string, object)>();

    /// <summary>The details of a refusal (a Pokémon's faults, a line each, English from the server), or null.</summary>
    public string? Details { get; private set; }

    /// <summary>The line said now, in the language chosen.</summary>
    public string LineText => Line == null ? "" : Text.Of(Line, LineValues);

    /// <summary>Whether both have offered and this player may confirm.</summary>
    public bool MayConfirm => Phase == LinkPhase.Room && MyOffer != null && TheirOffer != null && !MineConfirmed && !Checking;

    /// <summary>Whether the connection is (still) open.</summary>
    public bool Connected => client?.Connected == true;

    // ---------------------------------------------------------------------------------------------- the frame

    /// <summary>Once a frame: what was awaited finished, what the server sent taken, the code's seconds counted down.</summary>
    public void Update(float dt)
    {
        if (Phase is LinkPhase.Closed) return;
        if (Phase == LinkPhase.Connecting) Connect();
        if (Phase == LinkPhase.Connecting) return;

        if (applying is { IsCompleted: true } done) Applied(done);

        // The code's seconds count down from the frame after it came
        if (Phase == LinkPhase.WaitingForOther)
        {
            CodeSecondsLeft = Math.Max(0f, CodeSecondsLeft - dt);
            if (CodeSecondsLeft <= 0f)
            {
                client?.Send(new LeaveRoom());
                Code = null;
                ToMenu("globalLink.line.codeExpired");
            }
        }

        if (client != null)
        {
            while (Phase != LinkPhase.Closed && client.TryTake(out var message)) Take(message);
            if (greeted && Phase is not (LinkPhase.Failed or LinkPhase.Closed) && !client.Connected) Fail("globalLink.error.dropped");
        }

        if (applying == null && pending.Count > 0 && Phase is not (LinkPhase.Failed or LinkPhase.Closed)) ApplyNext();
    }

    private void Connect()
    {
        if (connecting == null)
        {
            try
            {
                client = makeClient!();
                connecting = client.ConnectAsync(Wait);
            }
            catch (Exception e) when (e is UriFormatException or ArgumentException or InvalidOperationException)
            {
                Fail("globalLink.error.badServer");
            }
            return;
        }
        if (!connecting.IsCompleted) return;
        if (connecting.IsCompletedSuccessfully)
        {
            var welcome = connecting.Result;
            greeted = true;
            FriendCode = welcome.FriendCode;
            foreach (var r in welcome.Pending) pending.Enqueue((r, false));
            ToMenu(welcome.Pending.Count > 0 ? "globalLink.line.finishing" : "globalLink.line.welcome");
            return;
        }
        var error = connecting.Exception?.GetBaseException();
        if (error is OnlineRefusedException refused) Refuse(refused.Refusal);
        else Fail("globalLink.error.unreachable");
    }

    // ---------------------------------------------------------------------------------------------- what the player does

    /// <summary>MAKE A CODE: the server is asked for a code for a trade room.</summary>
    public bool MakeCode() => MakeCode(RoomKind.Trade);

    /// <summary>
    /// MAKE A BATTLE CODE (plan 07 · O6): the server is asked for a code for a battle room, of the room's own rules
    /// (<see cref="LinkRoom.Default"/>: Flat 50 under Platinum's rules, six against six, the species and item clauses).
    /// </summary>
    public bool MakeBattleCode() => MakeCode(RoomKind.Battle);

    private bool MakeCode(RoomKind kind)
    {
        if (Phase != LinkPhase.Menu || client == null) return false;
        making = kind;
        client.Send(kind == RoomKind.Battle ? new MakeCode(kind, LinkRoom.Default) : new MakeCode(kind));
        Phase = LinkPhase.MakingCode;
        Say("globalLink.line.making");
        return true;
    }

    /// <summary>ENTER A CODE: the digits begin.</summary>
    public bool EnterCode()
    {
        if (Phase != LinkPhase.Menu) return false;
        Typed = "";
        Phase = LinkPhase.EnterCode;
        Say("globalLink.line.typeCode", ("digits", OnlineRules.CodeDigits));
        return true;
    }

    /// <summary>A digit typed (0 to 9); false once the code is whole.</summary>
    public bool TypeDigit(int digit)
    {
        if (Phase != LinkPhase.EnterCode || digit is < 0 or > 9 || Typed.Length >= OnlineRules.CodeDigits) return false;
        Typed += (char)('0' + digit);
        return true;
    }

    /// <summary>The last digit taken back; false with none.</summary>
    public bool Erase()
    {
        if (Phase != LinkPhase.EnterCode || Typed.Length == 0) return false;
        Typed = Typed[..^1];
        return true;
    }

    /// <summary>OK: the code typed is sent, once it is whole.</summary>
    public bool SubmitCode()
    {
        if (Phase != LinkPhase.EnterCode || Typed.Length != OnlineRules.CodeDigits || client == null) return false;
        client.Send(new JoinCode(Typed));
        Phase = LinkPhase.Joining;
        Say("globalLink.line.joining");
        return true;
    }

    /// <summary>A Pokémon offered (it carries its id): the server checks it, and both see it once it passes.</summary>
    public bool Offer(SavedPokemonData pokemon)
    {
        if (Phase != LinkPhase.Room || Checking || client == null || pokemon.Id is not { } id || id == Guid.Empty) return false;
        client.Send(new TradeOffer(pokemon));
        Checking = true;
        Details = null;
        Say("globalLink.line.checking");
        return true;
    }

    /// <summary>TRADE: this player confirms the trade as both offers stand.</summary>
    public bool ConfirmTrade()
    {
        if (!MayConfirm || client == null) return false;
        client.Send(new TradeConfirm());
        return true;
    }

    /// <summary>The trade called off (until both have confirmed): both choose again.</summary>
    public bool CancelTrade()
    {
        if (Phase != LinkPhase.Room || client == null || MyOffer == null && TheirOffer == null) return false;
        client.Send(new TradeCancel());
        return true;
    }

    /// <summary>Out of the room (or the code made given up), back to the counter's menu.</summary>
    public bool Leave()
    {
        if (Phase is not (LinkPhase.Room or LinkPhase.WaitingForOther or LinkPhase.MakingCode or LinkPhase.EnterCode or LinkPhase.Joining or LinkPhase.BattleRoom)) return false;
        if (Phase is not LinkPhase.EnterCode) client?.Send(new LeaveRoom());
        Code = null;
        ResetRoom();
        ToMenu("globalLink.line.welcome");
        return true;
    }

    /// <summary>What came by the trade read: back to choosing in the room.</summary>
    public void Dismiss()
    {
        if (Phase != LinkPhase.Traded) return;
        Received = null;
        Phase = OtherName != null ? LinkPhase.Room : LinkPhase.Menu;
        Say(Phase == LinkPhase.Room ? "globalLink.line.choose" : "globalLink.line.welcome", ("name", OtherName ?? ""));
    }

    /// <summary>The visit ended: out of any room and the connection closed. A receipt being applied finishes first.</summary>
    public void Close()
    {
        if (Phase == LinkPhase.Closed) return;
        if (client != null)
        {
            var c = client;
            if (c.Room != null || Phase is LinkPhase.Room or LinkPhase.WaitingForOther) c.Send(new LeaveRoom());
            _ = CloseQuietly(c);
        }
        Phase = LinkPhase.Closed;
    }

    /// <summary>Whether the counter may be left now: never while a receipt is being written into the save.</summary>
    public bool MayClose => Phase != LinkPhase.Trading && applying == null;

    private static async Task CloseQuietly(OnlineClient c)
    {
        try { await c.DisposeAsync(); }
        catch (Exception e) when (e is WebSocketException or ObjectDisposedException or OperationCanceledException or InvalidOperationException) { }
    }

    // ---------------------------------------------------------------------------------------------- a battle (plan 07 · O6)

    /// <summary>The battle room's rules, once it is joined.</summary>
    public LinkRoom Rules { get; private set; } = LinkRoom.Default;

    /// <summary>The battle's client once a team is brought: its screen plays it (<c>BattleEngine.ForLink</c>) once the room greets the player.</summary>
    public LinkClient? Battle { get; private set; }

    /// <summary>The battle as the server counted it against the two friend codes, once it has.</summary>
    public BattleRecorded? Recorded { get; private set; }

    /// <summary>
    /// The team brought into the battle room: the server checks it (the clauses, each Pokémon possible, the data) and,
    /// once both are in, greets each player with the other's preview; the six go in in the order brought.
    /// </summary>
    public bool BringTeam(LinkPlayer team)
    {
        if (Phase != LinkPhase.BattleRoom || client == null || team.Team.Count == 0) return false;
        Battle = client.StartBattle(team, new BroughtInOrder());
        Battle.AnswersByItself = false;
        Recorded = null;
        Details = null;
        Phase = LinkPhase.Battling;
        Say("globalLink.line.bringing", ("name", OtherName ?? ""));
        return true;
    }

    /// <summary>The battle's screen has shown its end: how it went is told, the record once the server has counted it.</summary>
    public void FinishBattle()
    {
        if (Phase != LinkPhase.Battling) return;
        Phase = LinkPhase.Battled;
        SayHowItWent();
    }

    private void SayHowItWent()
    {
        var result = Battle?.Result;
        bool won = result?.Winner is { } side && side == Battle!.Side;
        string how = result?.How switch
        {
            LinkEnding.Forfeit => "forfeit",
            LinkEnding.Timer => "timer",
            LinkEnding.Disconnect => "disconnect",
            _ => "knockedOut"
        };
        Say(result?.Winner == null ? "globalLink.battled.none" : won ? $"globalLink.battled.won.{how}" : $"globalLink.battled.lost.{how}", ("name", OtherName ?? ""));
    }

    /// <summary>How the battle went read: back to the counter's menu (a battle room holds one battle).</summary>
    public void DismissBattle()
    {
        if (Phase != LinkPhase.Battled) return;
        Battle = null;
        ResetRoom();
        ToMenu("globalLink.line.welcome");
    }

    // ---------------------------------------------------------------------------------------------- what the server says

    private void Take(OnlineMessage message)
    {
        switch (message)
        {
            case Refused refused:
                Refuse(refused);
                break;
            case CodeMade made when Phase == LinkPhase.MakingCode:
                Code = made.Code;
                CodeSecondsLeft = made.Seconds;
                Phase = LinkPhase.WaitingForOther;
                Say("globalLink.line.codeMade");
                break;
            case Joined joined:
                Code = joined.Code;
                OtherName = joined.OtherName;
                OtherFriendCode = joined.OtherFriendCode;
                ResetOffers();
                OtherAway = false;
                if (joined.Kind == RoomKind.Battle)
                {
                    // A battle room (plan 07 · O6): the team to bring, under the room's rules
                    Rules = joined.Rules ?? LinkRoom.Default;
                    Battle = null;
                    Recorded = null;
                    Phase = LinkPhase.BattleRoom;
                    Say("globalLink.line.battleRoom", ("name", joined.OtherName));
                    break;
                }
                Phase = LinkPhase.Room;
                Say("globalLink.line.choose", ("name", joined.OtherName));
                break;
            case BattleRecorded recorded:
                Recorded = recorded;
                if (Phase == LinkPhase.Battled) SayHowItWent();
                break;
            case TradeOffered { Mine: true } mine:
                MyOffer = mine.Pokemon;
                Checking = false;
                MineConfirmed = TheirsConfirmed = false;
                Say(TheirOffer == null ? "globalLink.line.waitOffer" : "globalLink.line.mayTrade", ("name", OtherName ?? ""));
                break;
            case TradeOffered theirs:
                TheirOffer = theirs.Pokemon;
                MineConfirmed = TheirsConfirmed = false;
                Say(MyOffer == null ? "globalLink.line.theyOffered" : "globalLink.line.mayTrade", ("name", OtherName ?? ""));
                break;
            case TradeConfirmed { Mine: true }:
                MineConfirmed = true;
                if (!TheirsConfirmed) Say("globalLink.line.waitConfirm", ("name", OtherName ?? ""));
                break;
            case TradeConfirmed:
                TheirsConfirmed = true;
                if (!MineConfirmed) Say("globalLink.line.theyConfirmed", ("name", OtherName ?? ""));
                break;
            case TradeCancelled:
                ResetOffers();
                if (Phase == LinkPhase.Room) Say("globalLink.line.cancelled");
                break;
            case TradeReceipt r:
                pending.Enqueue((r, Phase == LinkPhase.Room));
                break;
            case TradeDone:
                break;
            case RoomClosed when Phase is LinkPhase.Battling or LinkPhase.Battled:
                // A battle room closes as its battle ends (or the other left it): what came of it stays on show
                Code = null;
                if (Phase == LinkPhase.Battling && Battle?.Hello == null)
                {
                    ResetRoom();
                    Battle = null;
                    ToMenu("globalLink.line.otherLeft");
                }
                break;
            case RoomClosed when Phase == LinkPhase.BattleRoom:
                Code = null;
                ResetRoom();
                ToMenu("globalLink.line.otherLeft");
                break;
            case RoomClosed:
                if (Phase is LinkPhase.Room or LinkPhase.WaitingForOther or LinkPhase.Traded)
                {
                    bool wasInRoom = Phase != LinkPhase.WaitingForOther;
                    Code = null;
                    ResetRoom();
                    if (Phase == LinkPhase.Traded) break;
                    ToMenu(wasInRoom ? "globalLink.line.otherLeft" : "globalLink.line.codeExpired");
                }
                break;
            case Online.OtherAway away:
                OtherAway = true;
                Say("globalLink.line.otherAway", ("name", OtherName ?? ""), ("seconds", away.Seconds));
                break;
            case OtherBack:
                OtherAway = false;
                Say("globalLink.line.otherBack", ("name", OtherName ?? ""));
                break;
            case ServerClosing:
                Fail("globalLink.error.closing");
                break;
        }
    }

    private void Refuse(Refused refused)
    {
        string why = RefusedKey(refused.Code);
        string line = $"globalLink.refused.{why}";
        Details = refused.Code == RefusalCode.Illegal ? refused.Details : null;
        switch (refused.Code)
        {
            case RefusalCode.Outdated:
            case RefusalCode.Closing:
                Fail(line);
                return;
        }
        switch (Phase)
        {
            case LinkPhase.Connecting:
                Fail(line);
                break;
            case LinkPhase.Joining:
                Typed = "";
                Phase = LinkPhase.EnterCode;
                Say(line);
                break;
            case LinkPhase.MakingCode:
                ToMenu(line);
                Details = null;
                break;
            case LinkPhase.Room:
                Checking = false;
                Say(line);
                break;
            case LinkPhase.Battling when Battle?.Hello == null:
                // The team brought was refused (a clause, an impossible Pokémon, other data): back to choosing it
                Battle = null;
                Phase = LinkPhase.BattleRoom;
                Say(line);
                break;
            default:
                Say(line);
                break;
        }
    }

    /// <summary>The key of a refusal's line under <c>globalLink.refused</c>.</summary>
    public static string RefusedKey(RefusalCode code) => code switch
    {
        RefusalCode.Outdated => "outdated",
        RefusalCode.DataVersion => "dataVersion",
        RefusalCode.BadMessage => "badMessage",
        RefusalCode.TooFast => "tooFast",
        RefusalCode.TooManyGuesses => "tooManyGuesses",
        RefusalCode.NoSuchRoom => "noSuchRoom",
        RefusalCode.RoomFull => "roomFull",
        RefusalCode.InARoom => "inARoom",
        RefusalCode.Illegal => "illegal",
        RefusalCode.Duplicate => "duplicate",
        _ => "closing"
    };

    // ---------------------------------------------------------------------------------------------- receipts

    private void ApplyNext()
    {
        (receipt, receiptNow) = pending.Dequeue();
        if (receiptNow) Phase = LinkPhase.Trading;
        try
        {
            applying = client!.Apply(receipt, keeper);
        }
        catch (Exception e) when (e is InvalidOperationException or System.IO.IOException)
        {
            applying = null;
            Fail("globalLink.error.notSaved");
        }
    }

    private void Applied(Task<ReceiptOutcome> done)
    {
        applying = null;
        var r = receipt!;
        receipt = null;
        if (!done.IsCompletedSuccessfully)
        {
            // The save was written (the keeper writes before it returns) or not at all: what failed is the word to the server, which the welcome hands back
            Fail(done.Exception?.GetBaseException() is InvalidOperationException or System.IO.IOException ? "globalLink.error.notSaved" : "globalLink.error.dropped");
            return;
        }
        switch (done.Result)
        {
            case ReceiptOutcome.NotThisSave:
                Say("globalLink.line.notThisSave");
                if (Phase == LinkPhase.Trading) Phase = LinkPhase.Room;
                break;
            case ReceiptOutcome.AlreadyApplied:
            case ReceiptOutcome.Applied:
                if (Phase != LinkPhase.Trading)
                {
                    // A trade of an earlier visit, finished now: told on the menu's line
                    Caught++;
                    Say("globalLink.line.finished", ("pokemon", ShownName(r.Received)));
                    break;
                }
                Received = r.Received;
                ResetOffers();
                Phase = LinkPhase.Traded;
                Say("globalLink.line.traded", ("name", OtherName ?? ""), ("pokemon", ShownName(r.Received)));
                break;
        }
    }

    /// <summary>A Pokémon's name as a trade line says it: its nickname, or its species in the language chosen.</summary>
    public static string ShownName(SavedPokemonData p) => p.Nickname is { Length: > 0 } nick ? nick : Data.LocalNames.Species(p.SpeciesName);

    // ---------------------------------------------------------------------------------------------- helpers

    private void ResetOffers()
    {
        MyOffer = TheirOffer = null;
        MineConfirmed = TheirsConfirmed = false;
        Checking = false;
    }

    private void ResetRoom()
    {
        ResetOffers();
        OtherName = OtherFriendCode = null;
        OtherAway = false;
    }

    private void ToMenu(string line)
    {
        Phase = LinkPhase.Menu;
        Say(line);
    }

    private void Fail(string line)
    {
        Phase = LinkPhase.Failed;
        Say(line);
    }

    private void Say(string line, params (string Name, object Value)[] values)
    {
        Line = line;
        LineValues = values;
    }
}
