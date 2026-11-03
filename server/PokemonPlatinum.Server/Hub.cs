using System.Security.Cryptography;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;

namespace PokemonPlatinum.Server;

/// <summary>
/// The server's state and every rule it plays by (plan 07 · O4 to O6): the connections, the rooms by link code, the
/// limits, the trade broker and the battle host. Everything happens under one lock, a message at a time: a server for
/// friends has little to do, and one lock keeps a trade's steps and a battle's turns in one order that a crash can be
/// reasoned about. GPU-free; the transport is <see cref="LinkHost"/>'s.
/// </summary>
public sealed class Hub
{
    private readonly object gate = new();
    private readonly Store store;
    private readonly ServerOptions options;
    private readonly ILogger log;
    private readonly Limits limits = new();
    private readonly Dictionary<string, Room> rooms = new();
    private readonly HashSet<Connection> connections = new();
    private DateTimeOffset lastForget = DateTimeOffset.MinValue;

    public Hub(Store store, ServerOptions options, ILogger log)
    {
        this.store = store;
        this.options = options;
        this.log = log;
        int open = store.Recover();
        if (open > 0) log.LogInformation("Recovery: {Count} trade(s) wait for their receipts to be applied", open);
    }

    /// <summary>The data this server checks Pokémon and battles against (<see cref="GameDataFiles.Version"/>).</summary>
    public string DataVersion => GameDataFiles.Version;

    /// <summary>Whether the server has been killed (<see cref="ServerOptions.CrashAt"/>): it does nothing more.</summary>
    public bool Dead { get; private set; }

    /// <summary>Whether the server is shutting down: no new connection is taken.</summary>
    public bool Closing { get; private set; }

    public Store Store => store;
    private DateTimeOffset Now => options.Clock.GetUtcNow();

    // ---- Connections ----

    /// <summary>A connection opens: whether the address may open another.</summary>
    public bool Open(Connection c)
    {
        lock (gate)
        {
            if (Dead || Closing || !limits.Open(c.IpHash)) return false;
            connections.Add(c);
            return true;
        }
    }

    /// <summary>A connection closed, however it closed.</summary>
    public void Closed(Connection c)
    {
        lock (gate)
        {
            connections.Remove(c);
            limits.Close(c.IpHash);
            if (Dead || c.Room is not { } room) return;
            int i = room.IndexOf(c);
            if (i < 0) return;
            var member = room.Members[i]!;
            member.Connection = null;
            if (!room.Full)
            {
                // A code nobody has joined goes with its maker
                Close(room, tellOthers: false);
                return;
            }
            member.AwaySince = Now;
            if (room.Kind == RoomKind.Trade && !(room.Confirmed[0] && room.Confirmed[1]))
            {
                room.ResetTrade();
                room.Other(i).Send(new TradeCancelled());
            }
            room.Other(i).Send(new OtherAway(OnlineRules.RejoinSeconds));
        }
    }

    /// <summary>One text from a connection.</summary>
    public void Receive(Connection c, string text)
    {
        lock (gate)
        {
            if (Dead) return;
            try
            {
                if (!Limits.Allow(c, Now))
                {
                    c.Send(new Refused(RefusalCode.TooFast));
                    return;
                }
                OnlineMessage message;
                try
                {
                    message = OnlineProtocol.Read(text);
                }
                catch (OnlineProtocolException e)
                {
                    c.Send(new Refused(e.Code, e.Message));
                    if (e.Code == RefusalCode.Outdated) _ = c.CloseAsync("outdated");
                    return;
                }
                if (c.Player == null)
                {
                    if (message is Hello hello) Greet(c, hello);
                    else c.Send(new Refused(RefusalCode.BadMessage, "hello first"));
                    return;
                }
                Handle(c, message);
            }
            catch (SimulatedCrash crash)
            {
                Die(crash.Message);
            }
        }
    }

    private void Greet(Connection c, Hello hello)
    {
        string? token = string.IsNullOrWhiteSpace(hello.Token) ? null : store.Hash("token", hello.Token);
        c.Player = store.Recognise(token, c.IpHash, Now);
        c.Name = string.IsNullOrWhiteSpace(hello.Name) ? "Player" : hello.Name.Trim()[..Math.Min(hello.Name.Trim().Length, 16)];
        c.DataVersion = hello.DataVersion ?? "";
        if (c.Loopback && !options.Local)
            log.LogWarning("A player came from a loopback address: is LinkServer:TrustedProxy set to Caddy's address? Every player would share one friend code");
        log.LogInformation("Player {Code} connected from address {Ip}", c.Player.FriendCode, c.IpHash[..12]);
        var away = rooms.Values.FirstOrDefault(r => r.Full && r.Members.Any(m => m!.AwaySince != null && m.Binds(c)));
        c.Send(new Welcome(c.Player.FriendCode, OnlineProtocol.Version, DataVersion, store.PendingFor(c.Player.FriendCode), away?.Code));
    }

    private void Handle(Connection c, OnlineMessage message)
    {
        switch (message)
        {
            case MakeCode make: MakeRoom(c, make); break;
            case JoinCode join: JoinRoom(c, join.Code); break;
            case Rejoin back: RejoinRoom(c, back.Code); break;
            case LeaveRoom: Leave(c); break;
            case TradeOffer offer: Offer(c, offer.Pokemon); break;
            case TradeConfirm: Confirm(c); break;
            case TradeCancel: CancelTrade(c); break;
            case TradeAck ack: Ack(c, ack.Receipt); break;
            case BattleTeam team: Bring(c, team.Team); break;
            case BattleMessage battle: BattleIn(c, battle.Text); break;
            default: c.Send(new Refused(RefusalCode.BadMessage, "not a message a game sends")); break;
        }
    }

    // ---- Rooms ----

    private void MakeRoom(Connection c, MakeCode make)
    {
        if (c.Room != null) { c.Send(new Refused(RefusalCode.InARoom)); return; }
        if (!limits.MayMake(c.IpHash, Now)) { c.Send(new Refused(RefusalCode.TooFast)); return; }
        string code;
        do code = NewLinkCode(); while (rooms.ContainsKey(code));
        var room = new Room(code, make.Kind, make.Kind == RoomKind.Battle ? make.Rules ?? LinkRoom.Default : null, new Member(c), Now.AddSeconds(OnlineRules.CodeSeconds));
        rooms[code] = room;
        c.Room = room;
        c.Send(new CodeMade(code, make.Kind, OnlineRules.CodeSeconds));
    }

    private static string NewLinkCode()
    {
        Span<char> digits = stackalloc char[OnlineRules.CodeDigits];
        for (int i = 0; i < digits.Length; i++) digits[i] = (char)('0' + RandomNumberGenerator.GetInt32(10));
        return new string(digits);
    }

    private void JoinRoom(Connection c, string code)
    {
        if (c.Room != null) { c.Send(new Refused(RefusalCode.InARoom)); return; }
        if (!limits.MayGuess(c.IpHash, Now)) { c.Send(new Refused(RefusalCode.TooManyGuesses)); return; }
        code = new string((code ?? "").Where(char.IsDigit).ToArray());
        if (!rooms.TryGetValue(code, out var room) || !room.Full && Now >= room.Expires)
        {
            bool blocked = limits.WrongGuess(c.IpHash, Now);
            c.Send(new Refused(blocked ? RefusalCode.TooManyGuesses : RefusalCode.NoSuchRoom));
            return;
        }
        if (room.Full) { c.Send(new Refused(RefusalCode.RoomFull)); return; }
        if (room.Members[0]!.FriendCode == c.Player!.FriendCode) { c.Send(new Refused(RefusalCode.BadMessage, "your own code")); return; }
        room.Members[1] = new Member(c);
        c.Room = room;
        Introduce(room, 0);
        Introduce(room, 1);
    }

    private static void Introduce(Room room, int i)
    {
        var other = room.Other(i);
        room.Members[i]!.Send(new Joined(room.Code, room.Kind, room.Rules, other.FriendCode, other.Name));
    }

    private void RejoinRoom(Connection c, string code)
    {
        if (!rooms.TryGetValue(code ?? "", out var room) || !room.Full) { c.Send(new Refused(RefusalCode.NoSuchRoom)); return; }
        int i = Array.FindIndex(room.Members, m => m!.AwaySince != null && m.Binds(c));
        if (i < 0) { c.Send(new Refused(RefusalCode.RoomFull)); return; }
        var member = room.Members[i]!;
        member.Connection = c;
        member.AwaySince = null;
        c.Room = room;
        Introduce(room, i);
        room.Other(i).Send(new OtherBack());
        if (room.Kind == RoomKind.Trade)
        {
            for (int k = 0; k < 2; k++)
                if (room.Offers[k] is { } offered) c.Send(new TradeOffered(k == i, offered));
        }
        else if (room.Battle != null) room.Battle.Rejoin(Room.SideOf(i), new SocketLink(member));
        AfterBattleStep(room);
    }

    private void Leave(Connection c)
    {
        if (c.Room is not { } room) return;
        int i = room.IndexOf(c);
        if (room.Battle is { Result: null } battle)
        {
            battle.GiveUp(Room.SideOf(i), LinkEnding.Forfeit);
            AfterBattleStep(room);
            if (!rooms.ContainsKey(room.Code)) return;
        }
        Close(room, tellOthers: true);
    }

    private void Close(Room room, bool tellOthers)
    {
        rooms.Remove(room.Code);
        foreach (var m in room.Members)
        {
            if (m?.Connection is not { } conn) continue;
            if (tellOthers) conn.Send(new RoomClosed(room.Code));
            conn.Room = null;
        }
    }

    // ---- The trade broker (O5) ----

    private bool InTrade(Connection c, out Room room, out int i)
    {
        room = c.Room!;
        i = room?.IndexOf(c) ?? -1;
        if (room is { Kind: RoomKind.Trade, Full: true } && i >= 0) return true;
        c.Send(new Refused(RefusalCode.BadMessage, "not in a trade room with someone"));
        return false;
    }

    private void Offer(Connection c, SavedPokemonData pokemon)
    {
        if (!InTrade(c, out var room, out int i)) return;
        if (c.DataVersion != DataVersion) { c.Send(new Refused(RefusalCode.DataVersion, DataVersion)); return; }
        if (pokemon == null) { c.Send(new Refused(RefusalCode.BadMessage)); return; }
        var faults = PokemonCheck.Faults(pokemon);
        if (faults.Count > 0) { c.Send(new Refused(RefusalCode.Illegal, string.Join("\n", faults))); return; }
        var id = pokemon.Id!.Value;
        var owner = store.OwnerOf(id);
        if (owner != null && owner != c.Player!.FriendCode || store.InOpenTrade(id))
        {
            log.LogWarning("Player {Code} offered a Pokémon the log gives to someone else", c.Player!.FriendCode);
            c.Send(new Refused(RefusalCode.Duplicate));
            return;
        }
        room.Offers[i] = pokemon;
        room.Confirmed[0] = room.Confirmed[1] = false;
        c.Send(new TradeOffered(true, pokemon));
        room.Other(i).Send(new TradeOffered(false, pokemon));
    }

    private void Confirm(Connection c)
    {
        if (!InTrade(c, out var room, out int i)) return;
        if (room.Offers[0] == null || room.Offers[1] == null || room.Members.Any(m => m!.Connection == null))
        {
            c.Send(new Refused(RefusalCode.BadMessage, "both must offer first"));
            return;
        }
        room.Confirmed[i] = true;
        c.Send(new TradeConfirmed(true));
        room.Other(i).Send(new TradeConfirmed(false));
        if (!(room.Confirmed[0] && room.Confirmed[1])) return;

        var a = room.Members[0]!;
        var b = room.Members[1]!;
        var fromA = room.Offers[0]!;
        var fromB = room.Offers[1]!;
        room.ResetTrade();
        if (fromA.Id == fromB.Id)
        {
            a.Send(new Refused(RefusalCode.Duplicate));
            b.Send(new Refused(RefusalCode.Duplicate));
            a.Send(new TradeCancelled());
            b.Send(new TradeCancelled());
            return;
        }
        Step(TradeSteps.BeforeCommit);
        var receipt = store.Commit(a.FriendCode, fromA, b.FriendCode, fromB, DataVersion, Now);
        log.LogInformation("Trade {Receipt} between {A} and {B} written", receipt, a.FriendCode, b.FriendCode);
        Step(TradeSteps.AfterCommit);
        a.Send(new TradeReceipt(receipt, fromA.Id!.Value, fromB, b.FriendCode));
        Step(TradeSteps.AfterReceiptA);
        b.Send(new TradeReceipt(receipt, fromB.Id!.Value, fromA, a.FriendCode));
        Step(TradeSteps.AfterReceiptB);
    }

    private void CancelTrade(Connection c)
    {
        if (!InTrade(c, out var room, out int i)) return;
        room.ResetTrade();
        c.Send(new TradeCancelled());
        room.Other(i).Send(new TradeCancelled());
    }

    /// <summary>A receipt applied: taken from anyone who is one of its two, in a room or not (a game back after a crash).</summary>
    private void Ack(Connection c, Guid receipt)
    {
        var before = store.Trade(receipt);
        var (known, done) = store.Ack(receipt, c.Player!.FriendCode);
        if (!known) { c.Send(new Refused(RefusalCode.BadMessage, "no such trade of yours")); return; }
        bool already = before != null && (c.Player.FriendCode == before.CodeA ? before.AckA : before.AckB);
        bool first = before != null && !before.AckA && !before.AckB;
        if (before is { Done: false } && !already) Step(first ? TradeSteps.AfterFirstAck : TradeSteps.AfterSecondAck);
        c.Send(new TradeDone(receipt));
        if (done && before is { Done: false }) log.LogInformation("Trade {Receipt} done", receipt);
    }

    private void Step(string step)
    {
        if (options.CrashAt == step) throw new SimulatedCrash(step);
    }

    /// <summary>The server is killed: nothing more is written or sent, every connection drops.</summary>
    private void Die(string why)
    {
        Dead = true;
        log.LogError("{Why}", why);
        foreach (var c in connections) c.Abort();
        connections.Clear();
        rooms.Clear();
    }

    // ---- The battle host (O6) ----

    private void Bring(Connection c, LinkPlayer team)
    {
        var room = c.Room;
        int i = room?.IndexOf(c) ?? -1;
        if (room is not { Kind: RoomKind.Battle, Full: true } || i < 0 || room.Battle != null)
        {
            c.Send(new Refused(RefusalCode.BadMessage, "not in a battle room waiting for teams"));
            return;
        }
        if (c.DataVersion != DataVersion) { c.Send(new Refused(RefusalCode.DataVersion, DataVersion)); return; }
        if (team?.Team == null || team.Team.Any(p => p?.Pokemon == null)) { c.Send(new Refused(RefusalCode.BadMessage)); return; }
        var faults = team.Team.SelectMany((p, n) => PokemonCheck.Faults(p.Pokemon, needsId: false).Select(f => $"{n + 1}: {f}")).ToList();
        faults.AddRange(LinkSetup.Check(room.Rules!, team));
        if (faults.Count > 0) { c.Send(new Refused(RefusalCode.Illegal, string.Join("\n", faults))); return; }
        // The name is the one the server greeted, never a team's own
        room.Teams[i] = team with { Name = c.Name };
        if (room.Teams[0] == null || room.Teams[1] == null) return;

        LinkSetup setup;
        try
        {
            setup = LinkSetup.Bring(room.Rules!, room.Teams[0]!, room.Teams[1]!);
        }
        catch (LinkRefusedException e)
        {
            room.Teams[0] = room.Teams[1] = null;
            foreach (var m in room.Members) m!.Send(new Refused(RefusalCode.Illegal, e.Message));
            return;
        }
        room.Seed = BitConverter.ToUInt32(RandomNumberGenerator.GetBytes(4));
        room.Battle = new LinkServer(setup, room.Seed, new SocketLink(room.Members[0]!), new SocketLink(room.Members[1]!));
        log.LogInformation("Battle in room {Code} between {A} and {B} begins", room.Code, room.Members[0]!.FriendCode, room.Members[1]!.FriendCode);
        AfterBattleStep(room);
    }

    private void BattleIn(Connection c, string text)
    {
        var room = c.Room;
        int i = room?.IndexOf(c) ?? -1;
        if (room?.Battle == null || i < 0) { c.Send(new Refused(RefusalCode.BadMessage, "no battle")); return; }
        room.Battle.Receive(Room.SideOf(i), text);
        AfterBattleStep(room);
    }

    /// <summary>After anything a battle did: its timers set for whoever is asked now, and its end counted once.</summary>
    private void AfterBattleStep(Room room)
    {
        var battle = room.Battle;
        if (battle == null) return;
        if (battle.Result is { } result)
        {
            if (room.Recorded) return;
            room.Recorded = true;
            var a = room.Members[0]!;
            var b = room.Members[1]!;
            string? record = null;
            if (battle.Opening != null && battle.Core != null)
            {
                try { record = BattleSnapshot.Take(battle.Opening, battle.Core).Serialize(); }
                catch (InvalidOperationException) { }
            }
            store.RecordBattle(a.FriendCode, b.FriendCode, result, room.Seed, DataVersion, record, Now);
            log.LogInformation("Battle in room {Code} over: {How}", room.Code, result.How);
            a.Send(new BattleRecorded(b.FriendCode, OutcomeFor(result, BattleSide.Player), result.How));
            b.Send(new BattleRecorded(a.FriendCode, OutcomeFor(result, BattleSide.Enemy), result.How));
            Close(room, tellOthers: true);
            return;
        }
        for (int i = 0; i < 2; i++)
        {
            var side = Room.SideOf(i);
            bool waiting = battle.Core == null ? battle.PickOf(side) == null : battle.IsWaitingOn(side);
            int question = battle.Core == null ? 0 : battle.Asking;
            if (waiting && room.TimedQuestion[i] != question)
            {
                room.TimedQuestion[i] = question;
                room.Deadline[i] = Now.AddSeconds(OnlineRules.TurnSeconds);
            }
            else if (!waiting) room.TimedQuestion[i] = -1;
        }
    }

    public static LinkOutcome OutcomeFor(LinkResult result, BattleSide side) =>
        result.Winner == null ? LinkOutcome.Draw : result.Winner == side ? LinkOutcome.Win : LinkOutcome.Loss;

    // ---- The clock ----

    /// <summary>
    /// What time does (called four times a second, or by a test): codes nobody joined expire, a player away too long
    /// loses the battle or leaves the trade room, a turn's timer that ran out answers for its side, and once a day the
    /// players not seen for a year are forgotten.
    /// </summary>
    public void Tick()
    {
        lock (gate)
        {
            if (Dead) return;
            var now = Now;
            foreach (var room in rooms.Values.ToList())
            {
                if (!room.Full)
                {
                    if (now >= room.Expires) Close(room, tellOthers: true);
                    continue;
                }
                for (int i = 0; i < 2 && rooms.ContainsKey(room.Code); i++)
                {
                    var m = room.Members[i]!;
                    if (m.AwaySince is { } since && now - since >= TimeSpan.FromSeconds(OnlineRules.RejoinSeconds))
                    {
                        if (room.Battle is { Result: null } away)
                        {
                            away.Disconnect(Room.SideOf(i));
                            AfterBattleStep(room);
                        }
                        if (rooms.ContainsKey(room.Code)) Close(room, tellOthers: true);
                    }
                }
                if (!rooms.ContainsKey(room.Code) || room.Battle is not { Result: null } battle) continue;
                for (int i = 0; i < 2; i++)
                {
                    if (room.TimedQuestion[i] < 0 || now < room.Deadline[i]) continue;
                    var side = Room.SideOf(i);
                    room.TimedQuestion[i] = -1;
                    if (battle.Core == null) battle.GiveUp(side, LinkEnding.Timer);
                    else battle.TimeOut(side);
                    AfterBattleStep(room);
                    if (!rooms.ContainsKey(room.Code)) break;
                }
            }
            if (now - lastForget >= TimeSpan.FromDays(1))
            {
                lastForget = now;
                int forgotten = store.ForgetOld(now);
                if (forgotten > 0) log.LogInformation("Forgot {Count} player(s) not seen for a year", forgotten);
            }
        }
    }

    // ---- Shutting down ----

    /// <summary>A graceful shutdown: every game is told, every connection closed; trades finish by their receipts later.</summary>
    public async Task ShutDownAsync()
    {
        List<Connection> all;
        lock (gate)
        {
            Closing = true;
            all = connections.ToList();
            foreach (var c in all) c.Send(new ServerClosing());
            rooms.Clear();
        }
        await Task.WhenAll(all.Select(c => c.CloseAsync("server closing")));
    }

    /// <summary>For tests: how many rooms are open.</summary>
    public int RoomCount { get { lock (gate) return rooms.Count; } }
}
