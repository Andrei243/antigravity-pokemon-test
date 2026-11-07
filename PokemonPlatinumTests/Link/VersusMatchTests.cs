using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Battle.Sim.Link;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests.Link;

/// <summary>
/// VERSUS, two people at one machine (plan 13 · V12): a link battle with its server and both players in one process,
/// each player's screen answered in turn behind a curtain. The battle ends, its record replays, the second player is
/// asked their own replacements, and nothing either player brought changes.
/// </summary>
public class VersusMatchTests
{
    private static LinkPlayer Player(string name, uint seed) => new(name, LinkBench.Team(seed, LinkRoom.Default));

    /// <summary>Plays a match to its end as two people would: the shown screen answered, the curtain lifted when it comes down.</summary>
    private static (List<BattleRequest> AskedOfSecond, int Curtains) PlayOut(VersusMatch match)
    {
        var asked = new List<BattleRequest>();
        int curtains = 0;
        var lines = new List<string>();
        for (int i = 0; i < 400_000; i++)
        {
            if (match.Curtain != null)
            {
                curtains++;
                match.PassTo();
            }
            if (match.Faces[1].LinkClient!.Asked is { } ask && (asked.Count == 0 || !ReferenceEquals(asked[^1], ask.Request))) asked.Add(ask.Request);
            bool going = LinkScreenTests.Act(match.Face, lines);
            match.Frame(0.05f);
            if (!going && match.Over) return (asked, curtains);
        }
        throw new InvalidOperationException("The versus match never ended");
    }

    [Theory]
    [InlineData(3u)]
    [InlineData(4u)]
    [InlineData(9u)]
    public void TwoPeopleAtOneMachineFinishABattleAndItsRecordReplays(uint seed)
    {
        var first = Player("Player 1", seed * 2 + 1);
        var second = Player("Player 2", seed * 2 + 2);
        string before = string.Join("|", first.Team.Concat(second.Team).Select(p => System.Text.Json.JsonSerializer.Serialize(p.Pokemon)));
        var match = new VersusMatch(first, second, GameEngine.VersusRoom(6, 6), seed);
        Assert.Equal(0, match.Shown);
        Assert.True(match.Faces[1].Unseen);

        var (askedOfSecond, curtains) = PlayOut(match);
        var result = match.Result!;
        Assert.NotNull(result.Winner);
        Assert.True(curtains > 1, "the curtain came down between the players");

        // Each screen from its own side
        bool firstWon = result.Winner == BattleSide.Player;
        Assert.Equal(firstWon ? BattleResult.PlayerVictory : BattleResult.PlayerDefeat, match.Faces[0].Result == BattleResult.None ? Ended(match.Faces[0]) : match.Faces[0].Result);
        Assert.Equal(firstWon ? BattleResult.PlayerDefeat : BattleResult.PlayerVictory, match.Faces[1].Result == BattleResult.None ? Ended(match.Faces[1]) : match.Faces[1].Result);

        // The second player was asked their own places, a replacement among them, from their own side
        Assert.All(askedOfSecond.OfType<ActionRequest>(), a => Assert.All(a.Places, p => Assert.Equal(BattleSide.Enemy, p.Side)));
        Assert.Contains(askedOfSecond, r => r is ReplacementRequest);

        // The server's record replays to the same log
        var core = match.Server.Core!;
        var again = new BattleCore(match.Server.Setup.Battle(match.Server.PickOf(BattleSide.Player)!, match.Server.PickOf(BattleSide.Enemy)!, seed));
        again.Replay(core.Record!);
        Assert.Equal(core.Log.Count, again.Log.Count);
        Assert.Equal(core.Result, again.Result);

        // Nothing either player brought changed
        string after = string.Join("|", first.Team.Concat(second.Team).Select(p => System.Text.Json.JsonSerializer.Serialize(p.Pokemon)));
        Assert.Equal(before, after);
    }

    /// <summary>A screen's result once its last events have been shown (the unseen one reads on by itself).</summary>
    private static BattleResult Ended(BattleEngine face)
    {
        for (int i = 0; i < 2000 && face.Result == BattleResult.None; i++)
        {
            face.Update(0.05f);
            if (face.IsWaitingForConfirm) face.ConfirmMessage();
        }
        return face.Result;
    }

    [Fact]
    public void TheCurtainWaitsForTheOtherPlayerAndHandsThemTheScreen()
    {
        var match = new VersusMatch(Player("Player 1", 71), Player("Player 2", 72), GameEngine.VersusRoom(6, 6), 7);
        var lines = new List<string>();
        for (int i = 0; i < 4000 && match.Curtain == null; i++)
        {
            LinkScreenTests.Act(match.Face, lines);
            match.Frame(0.05f);
        }
        Assert.Equal(1, match.Curtain);
        Assert.True(match.Faces[0].WaitingForOther);
        Assert.Equal(BattleMenuState.Main, match.Faces[1].HUD.MenuState);
        match.PassTo();
        Assert.Null(match.Curtain);
        Assert.Equal(1, match.Shown);
        Assert.False(match.Faces[1].Unseen);
        Assert.True(match.Faces[0].Unseen);
    }

    [Fact]
    public void VersusIsTheBattleRoomsLastRowAndPicksTwoTeams()
    {
        Assert.Equal(RoomEntry.Versus, BattleRoomScreen.Entries[^1]);
        var room = new BattleRoomScreen();
        var team = new BuiltTeam();
        room.Open(new[] { team, team }, Array.Empty<PuzzleFile>(), new BattleRoomRecords());
        while (BattleRoomScreen.Entries[room.Cursor] != RoomEntry.Versus) room.Move(1);
        room.Confirm();
        Assert.Equal(RoomPage.PickTeam, room.Page);
        Assert.Equal(RoomChallenge.Versus, room.Picking);
        Assert.Equal(-1, room.VersusFirst);
    }
}
