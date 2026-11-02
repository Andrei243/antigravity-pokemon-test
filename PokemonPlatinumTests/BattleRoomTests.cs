using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V13: the battle room's teams (built from nothing and held to the one legality check), the gauntlet's order,
/// the streak's draws from its seed, a run's battles replaying, and the room's and the builder's screens.
/// </summary>
public class BattleRoomTests
{
    private static BuiltTeam Legal() => new()
    {
        Name = "TEST",
        Members =
        {
            new BuiltPokemon { Species = "Garchomp", Level = 50, Nature = Nature.Jolly, Ability = "Sand Veil", Item = "Leftovers", Moves = { "Earthquake", "Dragon Claw", "Fire Fang", "Swords Dance" } },
            new BuiltPokemon { Species = "Lucario", Level = 50, Nature = Nature.Adamant, Moves = { "Close Combat", "Extreme Speed" } }
        }
    };

    [Fact]
    public void ABuiltTeamIsLegalByTheOneCheck()
    {
        Assert.Null(BuiltTeams.WhyNot(Legal()));
        var team = Legal();
        team.Members[0].Moves.Add("Surf");
        Assert.Equal("battleRoom.illegal.moveCount", BuiltTeams.WhyNot(team)!.Value.Key);
        team.Members[0].Moves.RemoveAt(4);
        team.Members[0].Moves[3] = "Thunderbolt";
        Assert.Equal("battleRoom.illegal.move", BuiltTeams.WhyNot(team)!.Value.Key);
        team.Members[0].Moves[3] = "Earthquake";
        Assert.Equal("battleRoom.illegal.twice", BuiltTeams.WhyNot(team)!.Value.Key);
        team = Legal();
        team.Members[0].Ability = "Levitate";
        Assert.Equal("battleRoom.illegal.ability", BuiltTeams.WhyNot(team)!.Value.Key);
        team = Legal();
        team.Members[0].Item = "Bicycle";
        Assert.Equal("battleRoom.illegal.item", BuiltTeams.WhyNot(team)!.Value.Key);
        team = Legal();
        team.Members[0].Level = 101;
        Assert.Equal("battleRoom.illegal.level", BuiltTeams.WhyNot(team)!.Value.Key);
        Assert.Equal("battleRoom.illegal.empty", BuiltTeams.WhyNot(new BuiltTeam())!.Value.Key);
        // A move a forebear knew counts: Gible's line keeps what the data teaches any of it
        Assert.Contains("Dragon Rage", BuiltTeams.MovesOf(new BuiltPokemon { Species = "Garchomp" }));
        // The hidden ability may be chosen
        Assert.Contains("Rough Skin", BuiltTeams.AbilitiesOf(new BuiltPokemon { Species = "Garchomp" }));
    }

    [Fact]
    public void AFreshMemberCanBattleAtOnce()
    {
        foreach (string species in new[] { "Bulbasaur", "Pikachu", "Garchomp", "Ditto", "Arceus", "Pecharunt" })
            Assert.Null(BuiltTeams.WhyNot(BuiltTeams.Fresh(species)));
    }

    [Fact]
    public void ABuiltPokemonIsMadeTheSameEveryTimeAndWhole()
    {
        var team = Legal();
        var a = BuiltTeams.PartyOf(team);
        var b = BuiltTeams.PartyOf(team);
        for (int i = 0; i < a.Members.Count; i++)
        {
            Assert.Equal(a.Members[i].Personality, b.Members[i].Personality);
            Assert.Equal(a.Members[i].MaxHP, a.Members[i].CurrentHP);
            Assert.Equal(31, a.Members[i].IvSpeed);
            Assert.False(a.Members[i].IsShiny);
        }
        Assert.Equal(Nature.Jolly, a.Members[0].Nature);
        Assert.Equal("Leftovers", a.Members[0].HeldItem!.Name);
        Assert.Equal(new[] { "Earthquake", "Dragon Claw", "Fire Fang", "Swords Dance" }, a.Members[0].Moves.Select(m => m.Name));
    }

    [Fact]
    public void TheTeamsFileRoundTripsAndABrokenOneReadsAsEmpty()
    {
        string folder = Path.Combine(Path.GetTempPath(), "battleroom-" + Guid.NewGuid().ToString("N"));
        string path = Path.Combine(folder, BuiltTeams.FileName);
        try
        {
            BuiltTeams.Write(path, new BuiltTeamsFile { Teams = { Legal() } });
            var back = BuiltTeams.Read(path);
            Assert.Single(back.Teams);
            Assert.Equal("Garchomp", back.Teams[0].Members[0].Species);
            Assert.Equal(Nature.Jolly, back.Teams[0].Members[0].Nature);
            File.WriteAllText(path, "{ not json");
            Assert.Empty(BuiltTeams.Read(path).Teams);
            Assert.Empty(BuiltTeams.Read(Path.Combine(folder, "none.json")).Teams);
        }
        finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
    }

    [Fact]
    public void TheGauntletsOrderIsSinnohsLeadersFourAndChampion()
    {
        var order = GauntletData.Default.Order.Select(s => s.Id).ToList();
        Assert.Equal(13, order.Count);
        Assert.Equal("leader_roark", order[0]);
        Assert.Equal("leader_volkner", order[7]);
        Assert.Equal("champion_cynthia", order[^1]);
        foreach (var step in GauntletData.Default.Order)
        {
            var foe = RoomRun.GauntletFoe(step);
            Assert.NotEmpty(foe.Party.Members);
            Assert.Equal(0, foe.PrizeMoney);
            Assert.False(string.IsNullOrEmpty(step.Look));
        }
        // Each the table's own levels: Roark's highest is 14, Cynthia's 62
        Assert.Equal(14, RoomRun.GauntletFoe(GauntletData.Default.Order[0]).Party.Members.Max(p => p.Level));
        Assert.Equal(62, RoomRun.GauntletFoe(GauntletData.Default.Order[^1]).Party.Members.Max(p => p.Level));
    }

    [Fact]
    public void AStreakDrawsTheSameTrainersFromTheSameSeed()
    {
        for (int won = 0; won < 20; won += 3)
        {
            var a = RoomRun.StreakFoe(won, 1234);
            var b = RoomRun.StreakFoe(won, 1234);
            Assert.Equal(a.Name, b.Name);
            Assert.Equal(a.Party.Members.Select(p => p.Species.Name), b.Party.Members.Select(p => p.Species.Name));
            Assert.Equal(RoomRun.StreakTeam, a.Party.Members.Count);
            Assert.All(a.Party.Members, p => Assert.Equal(RoomRun.StreakLevel, p.Level));
        }
        Assert.NotEqual(RoomRun.StreakFoe(0, 1).Party.Members.Select(p => p.Species.Name), RoomRun.StreakFoe(0, 2).Party.Members.Select(p => p.Species.Name));
    }

    // A run's battle on the bare core, the player's side thought for by the trainer AI as a tool's would be
    private static BattleCore Fight(RoomRun run)
    {
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = run.PlayerParty(),
            Trainers = { run.NextFoe() },
            Random = new BattleRandom(run.BattleSeed),
            Rules = Ruleset.Modern,
            PlayerController = TrainerAi.Instance,
            Frontier = true
        });
        core.Start();
        return core;
    }

    [Fact]
    public void ARunsBattleReplaysAndTheRunKeepsItsBest()
    {
        var run = new RoomRun(RoomChallenge.Streak, Legal(), 77);
        var core = Fight(run);
        Assert.NotEqual(BattleResult.None, core.Result);
        var again = Fight(new RoomRun(RoomChallenge.Streak, Legal(), 77));
        Assert.Equal(core.Log.OfType<Said>().Select(s => s.Text), again.Log.OfType<Said>().Select(s => s.Text));

        var records = new BattleRoomRecords();
        run.Won();
        run.Won();
        run.Lose();
        Assert.True(run.Over);
        Assert.True(run.Keep(records));
        Assert.Equal(2, records.StreakBest);
        var shorter = new RoomRun(RoomChallenge.Streak, Legal(), 78);
        shorter.Lose();
        Assert.False(shorter.Keep(records));
        Assert.Equal(2, records.StreakBest);
    }

    [Fact]
    public void AGauntletClearedIsCountedAndAnIllegalTeamIsRefused()
    {
        var run = new RoomRun(RoomChallenge.Gauntlet, Legal(), 5);
        for (int i = 0; i < 13; i++) run.Won();
        Assert.True(run.Cleared);
        var records = new BattleRoomRecords();
        run.Keep(records);
        Assert.Equal(13, records.GauntletBest);
        Assert.Equal(1, records.GauntletClears);
        Assert.Throws<ArgumentException>(() => new RoomRun(RoomChallenge.Gauntlet, new BuiltTeam(), 1));
    }

    [Fact]
    public void TheBuilderMakesATeamFromNothing()
    {
        var builder = new TeamBuilderScreen();
        builder.Open(new BuiltTeamsFile());
        Assert.Equal(1, builder.TeamsRows);
        builder.Confirm();                                   // NEW TEAM
        Assert.Equal(BuilderPage.Team, builder.Page);
        Assert.Single(builder.File.Teams);
        builder.Confirm();                                   // ADD A POKéMON
        Assert.Equal(BuilderPage.Pick, builder.Page);
        Assert.Equal(PickKind.Species, builder.Picking);
        int garchomp = builder.Options.ToList().FindIndex(o => o.Value == "Garchomp");
        for (int i = 0; i < garchomp; i++) builder.Move(0, 1);
        builder.Confirm();
        Assert.Equal(BuilderPage.Member, builder.Page);
        Assert.Equal("Garchomp", builder.Member!.Species);
        Assert.Null(BuiltTeams.WhyNot(builder.Member));
        // Its level up by ten on the shoulder, its nature by one to the right
        builder.Move(0, (int)MemberRow.Level);
        builder.Jump(1);
        Assert.Equal(60, builder.Member.Level);
        builder.Move(0, 1);
        builder.Move(1, 0);
        Assert.Equal(Nature.Lonely, builder.Member.Nature);
        // A move picked from what it can know
        while (TeamBuilderScreen.MemberRows[builder.Cursor] != MemberRow.Move1) builder.Move(0, 1);
        builder.Confirm();
        Assert.Equal(PickKind.Move, builder.Picking);
        Assert.All(builder.Options.Skip(1), o => Assert.Contains(o.Value!, BuiltTeams.MovesOf(builder.Member)));
        builder.Cancel();
        builder.Cancel();
        builder.Cancel();
        Assert.Equal(BuilderPage.Teams, builder.Page);
        Assert.True(builder.Changed);
        Assert.True(BuiltTeams.IsLegal(builder.File.Teams[0]));
        builder.Cancel();
        Assert.False(builder.IsActive);
    }

    [Fact]
    public void TheRoomAsksForWhatItWantsAndRefusesATeamThatCannotBattle()
    {
        var room = new BattleRoomScreen();
        room.Open(new[] { new BuiltTeam { Name = "EMPTY" }, Legal() }, Puzzles.All, new BattleRoomRecords());
        room.Move(1);                                        // GAUNTLET
        room.Confirm();
        Assert.Equal(RoomPage.PickTeam, room.Page);
        Assert.Equal(1, room.Cursor);                        // the first team that can battle
        room.Move(-1);
        room.Confirm();
        Assert.Null(room.TakeRequest());                     // the empty one is refused
        room.Move(1);
        room.Confirm();
        Assert.Equal(new BeginRun(RoomChallenge.Gauntlet, 1), room.TakeRequest());

        var run = new RoomRun(RoomChallenge.Gauntlet, Legal(), 1);
        run.Won();
        room.AfterRunBattle(run, false);
        Assert.Equal(RoomPage.Between, room.Page);
        room.Confirm();
        Assert.IsType<NextBattle>(room.TakeRequest());
        room.AfterRunBattle(run, false);
        room.Move(1);
        room.Confirm();
        Assert.IsType<GiveUpRun>(room.TakeRequest());

        room.AfterPuzzle(Puzzles.All[0], true);
        Assert.Equal(RoomPage.PuzzleOver, room.Page);
        room.Confirm();
        Assert.Equal(new PlayPuzzle(Puzzles.All[0].Id), room.TakeRequest());
    }

    [Fact]
    public void TheTitleOffersTheBattleRoomWithAndWithoutASave()
    {
        var bare = new TitleScreen(null);
        Assert.Contains(TitleChoice.BattleRoom, bare.Entries);
        var rects = bare.EntryRects();
        Assert.True(rects[^1].Y + rects[^1].Height <= 1080);
        var full = TitleScreen.WithSlots(new[]
        {
            new TitleSlot(1, new SaveData { SeenSpecies = { 1 } }), new TitleSlot(2, new SaveData()), new TitleSlot(3, new SaveData())
        });
        full.OfferMigration(true);
        Assert.Contains(TitleChoice.BattleRoom, full.Entries);
        var fullRects = full.EntryRects();
        Assert.True(fullRects[0].Y >= 120, $"the column begins at {fullRects[0].Y}");
        Assert.True(fullRects[^1].Y + fullRects[^1].Height <= 1040, $"the column ends at {fullRects[^1].Y + fullRects[^1].Height}");
    }
}
