using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Online;
using PokemonPlatinumTests.Link;

namespace PokemonPlatinumTests.Online;

/// <summary>
/// The Global Link counter's battle room (plan 07 · O6, the game's half): two visits to the counter make and join a
/// battle room by code, bring their teams, and battle on their own screens (plan 28 · I3's <see cref="BattleEngine.ForLink"/>)
/// against the server started in-process on localhost; the battle's end is told and counted for both. Nothing here
/// reaches beyond 127.0.0.1.
/// </summary>
public class GlobalLinkBattleTests
{
    private static readonly TimeSpan Wait = OnlineTestServer.Wait;

    private static GlobalLinkSession Visit(OnlineTestServer server, string name) =>
        new(() => new OnlineClient(server.Address, OnlineClient.NewToken(), name), new FakeKeeper());

    private static async Task Until(Func<bool> done, Action? frame, params GlobalLinkSession[] sessions)
    {
        var clock = Stopwatch.StartNew();
        while (!done())
        {
            if (clock.Elapsed > TimeSpan.FromSeconds(60)) throw new TimeoutException($"Still waiting: {string.Join(", ", sessions.Select(s => $"{s.Phase} '{s.Line}' {s.Details}"))}");
            foreach (var s in sessions) s.Update(1f / 60f);
            frame?.Invoke();
            await Task.Delay(2);
        }
    }

    private static async Task<(GlobalLinkSession A, GlobalLinkSession B)> BattleRoom(OnlineTestServer server)
    {
        var a = Visit(server, "Lucas");
        var b = Visit(server, "Dawn");
        await Until(() => a.Phase == LinkPhase.Menu && b.Phase == LinkPhase.Menu, null, a, b);
        Assert.True(a.MakeBattleCode());
        await Until(() => a.Phase == LinkPhase.WaitingForOther, null, a);
        Assert.True(b.EnterCode());
        foreach (char c in a.Code!) b.TypeDigit(c - '0');
        Assert.True(b.SubmitCode());
        await Until(() => a.Phase == LinkPhase.BattleRoom && b.Phase == LinkPhase.BattleRoom, null, a, b);
        Assert.Equal("globalLink.line.battleRoom", a.Line);
        Assert.Equal(LinkRoom.Default, a.Rules);
        return (a, b);
    }

    [Fact]
    public async Task TwoVisitsBattleOnTheirOwnScreensAndTheBattleIsCountedForBoth()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, b) = await BattleRoom(server);
        Assert.True(a.BringTeam(new LinkPlayer("Lucas", LinkBench.Team(41, LinkRoom.Default))));
        Assert.True(b.BringTeam(new LinkPlayer("Dawn", LinkBench.Team(42, LinkRoom.Default))));
        Assert.Equal(LinkPhase.Battling, a.Phase);
        await Until(() => a.Battle!.Hello != null && b.Battle!.Hello != null, () => { a.Battle!.Pump(); b.Battle!.Pump(); }, a, b);

        // Each visit's battle on its own screen, answered through its menus
        var faceA = BattleEngine.ForLink(a.Battle!);
        var faceB = BattleEngine.ForLink(b.Battle!);
        var linesA = new List<string>();
        var linesB = new List<string>();
        bool going = true;
        await Until(() => !going, () =>
        {
            bool stillA = LinkScreenTests.Act(faceA, linesA);
            bool stillB = LinkScreenTests.Act(faceB, linesB);
            going = stillA || stillB;
        }, a, b);
        a.FinishBattle();
        b.FinishBattle();
        Assert.Equal(LinkPhase.Battled, a.Phase);
        await Until(() => a.Recorded != null && b.Recorded != null, null, a, b);

        // Both screens and both counts say the same battle from either side
        var result = a.Battle!.Result!;
        Assert.Equal(result, b.Battle!.Result);
        bool aWon = result.Winner == a.Battle.Side;
        Assert.Equal(aWon ? BattleResult.PlayerVictory : BattleResult.PlayerDefeat, faceA.Result);
        Assert.Equal(aWon ? BattleResult.PlayerDefeat : BattleResult.PlayerVictory, faceB.Result);
        Assert.Equal(aWon ? LinkOutcome.Win : LinkOutcome.Loss, a.Recorded!.Outcome);
        Assert.Equal(b.FriendCode, a.Recorded.OtherFriendCode);
        Assert.StartsWith(aWon ? "globalLink.battled.won." : "globalLink.battled.lost.", a.Line);
        Assert.Empty(faceA.Core.Log);

        // Read on: the counter's menu again
        a.DismissBattle();
        Assert.Equal(LinkPhase.Menu, a.Phase);
        Assert.Null(a.Battle);
        a.Close();
        b.Close();
    }

    [Fact]
    public async Task ATeamTheRoomRefusesIsChosenAgain()
    {
        await using var server = await OnlineTestServer.StartAsync();
        var (a, _) = await BattleRoom(server);
        var team = LinkBench.Team(51, LinkRoom.Default);
        var twins = new LinkPlayer("Lucas", team.Take(5).Append(team[0]).ToList());
        Assert.True(a.BringTeam(twins));
        await Until(() => a.Phase == LinkPhase.BattleRoom, null, a);
        Assert.Equal("globalLink.refused.illegal", a.Line);
        Assert.Null(a.Battle);
        Assert.True(a.Leave());
        Assert.Equal(LinkPhase.Menu, a.Phase);
    }
}
