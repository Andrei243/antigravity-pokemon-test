using System;
using System.Linq;
using System.Threading.Tasks;
using PokemonPlatinum.Server;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;
using PokemonPlatinumTests.Link;

namespace PokemonPlatinumTests.Online;

/// <summary>
/// The battle host (plan 07 · O6): two fake games battle through the server in-process, plan 28's
/// <see cref="LinkServer"/> running in the room over the WebSocket and each game's <see cref="LinkClient"/> answering by
/// a seeded chooser, as <see cref="LinkBench"/> plays them over the loopback. A whole battle with its record against the
/// two friend codes, a player who drops and comes back, a turn's timer, and a player who never comes back.
/// </summary>
public class OnlineBattleTests
{
    private static readonly TimeSpan Wait = OnlineTestServer.Wait;

    private static LinkPlayer TeamOf(string name, uint seed) => new(name, LinkBench.Team(seed, LinkRoom.Default));

    /// <summary>Pumps both battles' clients until the condition holds.</summary>
    private static async Task PumpUntil(Func<bool> done, params LinkClient[] clients)
    {
        for (int i = 0; i < 6000 && !done(); i++)
        {
            foreach (var c in clients) c.Pump();
            await Task.Delay(5);
        }
        foreach (var c in clients) c.Pump();
        Assert.True(done(), "the battle never came to that: " + string.Join(" | ", clients.Select(c =>
            $"{c.Name}: {c.Events.Count} events, asked {c.Asked?.Number}, result {c.Result}, refused [{string.Join("; ", c.Refusals)}]")));
    }

    [Theory]
    [InlineData(3u)]
    [InlineData(8u)]
    public async Task TwoGamesBattleToTheEndAndTheResultIsCountedForBoth(uint seed)
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, b) = await server.RoomAsync(RoomKind.Battle);
        var battleA = a.StartBattle(TeamOf("Lucas", seed * 2 + 1), new SeededChooser(seed * 7 + 3));
        var battleB = b.StartBattle(TeamOf("Dawn", seed * 2 + 2), new SeededChooser(seed * 11 + 5));
        await PumpUntil(() => battleA.Result != null && battleB.Result != null, battleA, battleB);

        Assert.Equal(battleA.Result, battleB.Result);
        Assert.Equal(LinkEnding.KnockedOut, battleA.Result!.How);
        Assert.Equal(BattleSide.Player, battleA.Side);
        Assert.Equal(BattleSide.Enemy, battleB.Side);
        Assert.Equal("Dawn", battleA.Hello!.FoeName);

        var recordedA = await a.NextAsync<BattleRecorded>(Wait);
        var recordedB = await b.NextAsync<BattleRecorded>(Wait);
        Assert.Equal(b.FriendCode, recordedA.OtherFriendCode);
        Assert.Equal(a.FriendCode, recordedB.OtherFriendCode);
        Assert.Equal(Hub.OutcomeFor(battleA.Result, BattleSide.Player), recordedA.Outcome);
        Assert.NotEqual(recordedA.Outcome, recordedB.Outcome);

        // The game writes it into the save's record of link battles (plan 07 · O2, decision 9)
        var record = new LinkBattleRecord();
        OnlineClient.Record(record, recordedA);
        Assert.Equal(recordedA.Outcome == LinkOutcome.Win ? 1 : 0, record.With(b.FriendCode!).Wins);

        var (wins, losses, draws) = server.Hub.Store.Battles(a.FriendCode!, b.FriendCode!);
        Assert.Equal(1, wins + losses + draws);
        await a.NextAsync<RoomClosed>(Wait);
    }

    [Fact]
    public async Task ATeamThatBreaksAClauseOrTheDataIsRefused()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, _) = await server.RoomAsync(RoomKind.Battle);
        var team = TeamOf("Lucas", 21);
        var twins = new LinkPlayer("Lucas", team.Team.Take(5).Append(team.Team[0]).ToList());
        a.Send(new BattleTeam(twins));
        var refused = await a.NextAsync<Refused>(Wait);
        Assert.Equal(RefusalCode.Illegal, refused.Code);

        var hacked = TeamOf("Lucas", 22);
        hacked.Team[2].Pokemon.Moves[0].MoveName = "Spacial Rend";
        a.Send(new BattleTeam(hacked));
        Assert.Contains("Spacial Rend", (await a.NextAsync<Refused>(Wait)).Details);
    }

    [Fact]
    public async Task APlayerWhoDropsComesBackToTheSameBattle()
    {
        await using var server = await OnlineTestServer.StartAsync();
        string tokenA = OnlineClient.NewToken();
        var (a, b) = await server.RoomAsync(RoomKind.Battle, tokenA);
        var teamA = TeamOf("Lucas", 31);
        var battleA = a.StartBattle(teamA, new SeededChooser(41));
        var battleB = b.StartBattle(TeamOf("Dawn", 32), new SeededChooser(42));
        await PumpUntil(() => battleA.Events.Count > 20 && battleB.Events.Count > 20, battleA, battleB);
        Assert.Null(battleA.Result);

        a.Abort();
        Assert.Equal(OnlineRules.RejoinSeconds, (await b.NextAsync<OtherAway>(Wait)).Seconds);
        server.Clock.Advance(OnlineRules.RejoinSeconds - 5);
        server.Tick();

        await using var again = await server.ConnectAsync("Lucas", tokenA);
        Assert.Equal(a.FriendCode, again.FriendCode);
        Assert.NotNull(again.Welcome!.Room);
        await again.RejoinAsync(again.Welcome.Room!, Wait);
        await b.NextAsync<OtherBack>(Wait);
        var resumed = again.ResumeBattle(teamA, new SeededChooser(43));
        await PumpUntil(() => resumed.Result != null && battleB.Result != null, resumed, battleB);
        Assert.Equal(resumed.Result, battleB.Result);
        Assert.True(resumed.Events.Count > 20, "the view so far came back with the greeting");
        await again.NextAsync<BattleRecorded>(Wait);
    }

    [Fact]
    public async Task ATurnTimerThatRunsOutAnswersForThePlayerAndTheThirdGivesTheMatchUp()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, b) = await server.RoomAsync(RoomKind.Battle);
        var battleA = a.StartBattle(TeamOf("Lucas", 51), new SeededChooser(61));
        var battleB = b.StartBattle(TeamOf("Dawn", 52), new SeededChooser(62));
        battleB.AnswersByItself = false;
        for (int run = 1; run <= LinkServer.TimeoutsAllowed; run++)
        {
            await PumpUntil(() => battleB.Asked != null, battleA, battleB);
            int asked = battleB.Asked!.Number;
            server.Clock.Advance(OnlineRules.TurnSeconds - 1);
            server.Tick();
            battleB.Pump();
            Assert.Equal(asked, battleB.Asked?.Number);
            server.Clock.Advance(1);
            server.Tick();
            if (run < LinkServer.TimeoutsAllowed)
            {
                // Answered for B by the rules' default choice: the battle goes on to another question
                await PumpUntil(() => battleB.Asked == null || battleB.Asked.Number != asked || battleB.Result != null, battleA, battleB);
                if (battleB.Result != null) return;
            }
        }
        await PumpUntil(() => battleA.Result != null && battleB.Result != null, battleA, battleB);
        Assert.Equal(LinkEnding.Timer, battleA.Result!.How);
        Assert.Equal(BattleSide.Player, battleA.Result.Winner);
        Assert.Equal(LinkOutcome.Loss, (await b.NextAsync<BattleRecorded>(Wait)).Outcome);
    }

    [Fact]
    public async Task APlayerWhoNeverComesBackLosesTheMatch()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, b) = await server.RoomAsync(RoomKind.Battle);
        var battleA = a.StartBattle(TeamOf("Lucas", 71), new SeededChooser(81));
        var battleB = b.StartBattle(TeamOf("Dawn", 72), new SeededChooser(82));
        await PumpUntil(() => battleB.Events.Count > 5, battleA, battleB);
        b.Abort();
        await a.NextAsync<OtherAway>(Wait);
        server.Clock.Advance(OnlineRules.RejoinSeconds);
        server.Tick();
        await PumpUntil(() => battleA.Result != null, battleA);
        Assert.Equal(LinkEnding.Disconnect, battleA.Result!.How);
        var recorded = await a.NextAsync<BattleRecorded>(Wait);
        Assert.Equal(LinkOutcome.Win, recorded.Outcome);
        Assert.Equal(LinkEnding.Disconnect, recorded.How);
        Assert.Equal((1, 0, 0), server.Hub.Store.Battles(a.FriendCode!, b.FriendCode!));
    }

    [Fact]
    public async Task LeavingTheRoomGivesTheMatchUp()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, b) = await server.RoomAsync(RoomKind.Battle);
        var battleA = a.StartBattle(TeamOf("Lucas", 91), new SeededChooser(92));
        var battleB = b.StartBattle(TeamOf("Dawn", 93), new SeededChooser(94));
        await PumpUntil(() => battleA.Events.Count > 5, battleA, battleB);
        await a.SendAsync(new LeaveRoom());
        await PumpUntil(() => battleB.Result != null, battleB);
        Assert.Equal(LinkEnding.Forfeit, battleB.Result!.How);
        Assert.Equal(LinkOutcome.Win, (await b.NextAsync<BattleRecorded>(Wait)).Outcome);
    }
}
