using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Trainer Hill on Route 111 (plan 21, part l; rulings, "The Trainer Hill"): Emerald's four challenges whole in the
/// data, every floor climbable from its way in to its stairs past its two trainers, the teams at the challenger's
/// highest level, Emerald's prize by the time, the best kept, and the counter, the floors and the roof played by
/// <see cref="HeadlessScriptHost"/>.
/// </summary>
[Collection("MapDatabase")]
public class TrainerHillTests
{
    /// <summary>The Hill's rooms of ours, laid out for what they hold and not on the hand-made rooms' plan (<c>RoomPlanTests</c>).</summary>
    internal static readonly string[] Rooms = { "TrainerHillLobby", "TrainerHillFloor", "TrainerHillRoof" };

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    private static IEnumerable<(TrainerHillMode Mode, int Floor, TrainerHillFloorRecord Record)> EveryFloor() =>
        Enum.GetValues<TrainerHillMode>().SelectMany(mode => Enumerable.Range(1, TrainerHill.Floors).Select(f => (mode, f, TrainerHillDatabase.Floor((int)mode, f))));

    // ------------------------------------------------------------------ the data

    [Fact]
    public void EveryChallengeHasFourFloorsOfTwoTrainersWithThreePokemonEach()
    {
        Assert.Equal(4, TrainerHillDatabase.Modes.Count);
        Assert.Equal(Enum.GetNames<TrainerHillMode>(), TrainerHillDatabase.Modes.Select(m => m.Name));
        foreach (var (mode, floor, record) in EveryFloor())
        {
            Assert.Equal(2, record.Trainers.Count);
            Assert.Equal(2, record.Spots.Count);
            Assert.Equal(16, record.Rows.Count);
            Assert.All(record.Rows, row => Assert.Matches("^[.#v]{16}$", row));
            foreach (var trainer in record.Trainers)
            {
                Assert.False(string.IsNullOrEmpty(trainer.Name), $"{mode} {floor}");
                Assert.Equal(3, trainer.Team.Count);
                foreach (var mon in trainer.Team)
                {
                    Assert.NotNull(PokemonDatabase.Get(mon.Species));
                    Assert.InRange(mon.Moves.Count, 1, 4);
                    Assert.All(mon.Moves, m => Assert.True(MoveDatabase.Has(m), m));
                    if (mon.Item != null) Assert.NotNull(ItemDatabase.Get(mon.Item));
                    Assert.Equal(6, mon.Evs.Count);
                    Assert.Equal(6, mon.Ivs.Count);
                    Assert.All(mon.Ivs, iv => Assert.InRange(iv, 0, 31));
                    Assert.True(mon.Evs.Sum() <= 510, $"{mon.Species}: {mon.Evs.Sum()} effort");
                }
            }
        }
        // Emerald's numbers for the Normal challenge's trainers, which the prize is worked out from
        Assert.Equal(Enumerable.Range(17, 8), TrainerHillDatabase.TrainersOf(0).Select(t => t.Number));
        // Two sets of ten prize lists of six, every prize an item of the data
        Assert.Equal(2, TrainerHillDatabase.PrizeSets.Count);
        Assert.All(TrainerHillDatabase.PrizeSets.SelectMany(s => s).SelectMany(l => l), item => Assert.NotNull(ItemDatabase.Get(item)));
    }

    [Fact]
    public void EveryFloorIsClimbedFromItsWayInToItsStairsThroughTheFloorAndPastItsTrainers()
    {
        foreach (var (mode, floor, record) in EveryFloor())
        {
            string where = $"{mode} floor {floor}";
            var spots = record.Spots.Select(TrainerHillFloor.SpotOf).ToList();
            // The trainers stand on open floor, and the way up stays open with them standing there
            Assert.All(spots, s => Assert.True(TrainerHillFloor.IsOpen(record, s.X, s.Y), $"{where}: a trainer stands on a wall at {s}"));
            var reach = TrainerHillFloor.Reachable(record, TrainerHillFloor.Arrival, spots.ToHashSet());
            Assert.True(reach.Contains(TrainerHillFloor.BesideTheStairs), $"{where}: the stairs can't be reached");
            // The strip's wall: the stairs are never reached without going down into the floor
            var stripOnly = new HashSet<(int, int)>();
            for (int x = 0; x < TrainerHillFloor.Width; x++) stripOnly.Add((x, TrainerHillFloor.Top + TrainerHillFloor.Strip));
            Assert.DoesNotContain(TrainerHillFloor.BesideTheStairs, TrainerHillFloor.Reachable(record, TrainerHillFloor.Arrival, stripOnly));
            // Each trainer sees at least the tile before them
            Assert.All(record.Spots, s => Assert.InRange(s.Range, 1, 15));
        }
    }

    [Fact]
    public void AFloorIsLaidFromTheClimbsVariablesWithItsTrainersAndThoseBeatenStandAside()
    {
        var story = new StoryState();
        Assert.Null(TrainerHillFloor.Make(story));
        TrainerHill.Begin(story, TrainerHillMode.Variety, 0);
        story.SetVar(TrainerHill.Vars.Floor, 3);
        TrainerHill.Beat(story, 3, 1);
        var map = TrainerHillFloor.Make(story)!;
        var record = TrainerHillDatabase.Floor((int)TrainerHillMode.Variety, 3);
        for (int t = 0; t < 2; t++)
        {
            var person = map.Everyone.Single(n => n.Key == TrainerHillFloor.TrainerKey(t));
            Assert.Equal(TrainerHillFloor.SpotOf(record.Spots[t]), (person.GridX, person.GridY));
            Assert.Equal((record.Spots[t].Facing, record.Spots[t].Range), (person.Facing, person.TrainerData!.SightRange));
            Assert.True(person.IsTrainer);
            Assert.Equal(t == 1, person.HasBattled);
            Assert.Equal(t, TrainerHillFloor.TrainerOf(person));
        }
        Assert.Equal(TrainerHillFloor.Width, map.Width);
        Assert.Contains(map.Triggers, tr => tr.Script == "Stairs");
        Assert.Contains(map.Triggers, tr => tr.Script == "Down");
    }

    // ------------------------------------------------------------------ the rules

    [Fact]
    public void ATrainersPokemonAreEmeraldsSetsAtTheChallengersHighestLevel()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Blaziken")!, 61, new Random(1)));
        party.Add(new Pokemon(PokemonDatabase.Get("Swellow")!, 74, new Random(2)));
        Assert.Equal(74, TrainerHill.LevelFor(party));

        var trainer = TrainerHill.TrainerOf(TrainerHillMode.Normal, 1, 0, TrainerHill.LevelFor(party));
        var record = TrainerHillDatabase.Floor(0, 1).Trainers[0];
        Assert.Equal(3, trainer.Party.Count);
        Assert.Equal(0, trainer.PrizeMoney);
        for (int i = 0; i < 3; i++)
        {
            var mon = trainer.Party.Members[i];
            var set = record.Team[i];
            Assert.Equal((set.Species, 74), (mon.Species.Name, mon.Level));
            Assert.Equal(set.Moves, mon.Moves.Select(m => m.Name));
            Assert.Equal(set.Item, mon.HeldItem?.Name);
            Assert.Equal(set.Ivs, new[] { mon.IvHP, mon.IvAttack, mon.IvDefense, mon.IvSpeed, mon.IvSpAttack, mon.IvSpDefense });
            Assert.Equal(set.Evs, new[] { mon.EvHP, mon.EvAttack, mon.EvDefense, mon.EvSpeed, mon.EvSpAttack, mon.EvSpDefense });
            Assert.False(mon.IsShiny);
            Assert.Equal(mon.MaxHP, mon.CurrentHP);
        }
    }

    [Fact]
    public void ThePrizeIsEmeraldsByTheChallengeAndTheTime()
    {
        // The prizes for a climb under twelve minutes, which trainer_hill.c names (GetPrizeItemId's comment)
        Assert.Equal("Sunny Day", ItemDatabase.Get(TrainerHill.PrizeFor(TrainerHillMode.Normal, 11 * 3600))!.TeachesMove);
        Assert.Equal("Elixir", TrainerHill.PrizeFor(TrainerHillMode.Variety, 0));
        Assert.Equal("Giga Drain", ItemDatabase.Get(TrainerHill.PrizeFor(TrainerHillMode.Unique, 0))!.TeachesMove);
        Assert.Equal("Brick Break", ItemDatabase.Get(TrainerHill.PrizeFor(TrainerHillMode.Expert, 0))!.TeachesMove);
        // And the rest by the minutes, the same in every list
        foreach (var mode in Enum.GetValues<TrainerHillMode>())
        {
            Assert.Equal("Ether", TrainerHill.PrizeFor(mode, 12 * 3600));
            Assert.Equal("Max Potion", TrainerHill.PrizeFor(mode, 13 * 3600 + 59 * 60));
            Assert.Equal("Revive", TrainerHill.PrizeFor(mode, 15 * 3600));
            Assert.Equal("Fluffy Tail", TrainerHill.PrizeFor(mode, 17 * 3600));
            Assert.Equal("Great Ball", TrainerHill.PrizeFor(mode, TrainerHill.MaxFrames));
        }
    }

    [Fact]
    public void TheClockRunsFromTheBeginningStopsAtTheRoofAndTheBestIsKept()
    {
        var story = new StoryState();
        TrainerHill.Begin(story, TrainerHillMode.Unique, 1000);
        Assert.Equal(90 * 60, TrainerHill.Frames(story, 1090));
        for (int floor = 1; floor < TrainerHill.Floors; floor++) Assert.False(TrainerHill.Climb(story, 1100));
        Assert.True(TrainerHill.Climb(story, 1600));
        Assert.Equal(TrainerHill.States.Reached, story.Var(TrainerHill.Vars.State));
        Assert.Equal(600 * 60, TrainerHill.Frames(story, 5000));
        // The first time is a record, held once; a slower climb is not, and leaves the best
        Assert.Equal(0, TrainerHill.CheckTime(story));
        Assert.Equal(2, TrainerHill.CheckTime(story));
        Assert.Equal(600 * 60, story.Var(TrainerHill.Vars.Best(TrainerHillMode.Unique)));
        TrainerHill.Begin(story, TrainerHillMode.Unique, 0);
        for (int floor = 1; floor <= TrainerHill.Floors; floor++) TrainerHill.Climb(story, 700);
        Assert.Equal(1, TrainerHill.CheckTime(story));
        Assert.Equal(600 * 60, story.Var(TrainerHill.Vars.Best(TrainerHillMode.Unique)));
        // An hour or more is Emerald's longest time
        TrainerHill.Begin(story, TrainerHillMode.Normal, 0);
        Assert.Equal(TrainerHill.MaxFrames, TrainerHill.Frames(story, 99999));
    }

    // ------------------------------------------------------------------ the counter, the floors and the roof

    private static HeadlessScriptHost Champion()
    {
        var host = new HeadlessScriptHost();
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Sceptile")!, 70, new Random(3)));
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Gardevoir")!, 66, new Random(4)));
        host.Story.Set("HoennHallOfFame");
        host.MapNamed = name => name == TrainerHillFloor.MapName && TrainerHillFloor.Make(host.Story) is { } floor ? floor : Room(name);
        return host;
    }

    [Fact]
    public void TheCounterIsShutBeforeTheHallOfFame()
    {
        var host = new HeadlessScriptHost();
        host.Map = Room("TrainerHillLobby");
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Counter", "TrainerHillLobby")!, host.Map.Everyone.First(n => n.Key == "hill_attendant"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal(TrainerHill.States.None, host.Story.Var(TrainerHill.Vars.State));
        Assert.DoesNotContain(host.Log, l => l.StartsWith("warp TrainerHillFloor"));
    }

    [Fact]
    public void AClimbFromTheCounterToTheRoofBeatsEveryTrainerAndWinsThePrize()
    {
        var host = Champion();
        host.Map = Room("TrainerHillLobby");
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // YES, then EXPERT
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(3);
        host.PlayTimeSeconds = 100;
        runner.Start(ScriptLibrary.Default.Find("Counter", "TrainerHillLobby")!, host.Map.Everyone.First(n => n.Key == "hill_attendant"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal((TrainerHill.States.Climbing, (int)TrainerHillMode.Expert, 1), (host.Story.Var(TrainerHill.Vars.State), host.Story.Var(TrainerHill.Vars.Mode), host.Story.Var(TrainerHill.Vars.Floor)));
        Assert.Contains(host.Log, l => l.StartsWith("warp TrainerHillFloor"));

        for (int floor = 1; floor <= TrainerHill.Floors; floor++)
        {
            var map = host.Map;
            Assert.Equal(TrainerHillFloor.MapName, map.Name);
            var record = TrainerHillDatabase.Floor((int)TrainerHillMode.Expert, floor);
            for (int t = 0; t < 2; t++)
            {
                var person = map.Everyone.Single(n => n.Key == TrainerHillFloor.TrainerKey(t));
                if (person.HasBattled) continue;
                // Spoken to from a tile only this trainer sees, or both where both see it
                host.PlayerTile = (person.GridX, person.GridY + 1);
                int before = host.FrontierBattles.Count;
                runner.Start(ScriptLibrary.Default.Find("Trainer", TrainerHillFloor.MapName)!, person);
                runner.RunToEnd();
                Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
                var battle = host.FrontierBattles[before];
                Assert.Same(host.Party, battle.Team);
                Assert.All(battle.Foes, foe => Assert.All(foe.Party.Members, m => Assert.Equal(70, m.Level)));
                Assert.Equal(battle.Foes.Count == 2, battle.Doubles);
                Assert.True(person.HasBattled);
                Assert.True(TrainerHill.IsBeaten(host.Story, floor, t));
            }
            runner.Start(ScriptLibrary.Default.Find("Stairs", TrainerHillFloor.MapName)!, null);
            runner.RunToEnd();
        }
        Assert.Equal("TrainerHillRoof", host.Map.Name);
        Assert.Equal(TrainerHill.States.Reached, host.Story.Var(TrainerHill.Vars.State));

        // Eleven minutes on, the owner: a new record and Expert's prize for a climb under twelve minutes
        host.PlayTimeSeconds = 100 + 11 * 60;
        host.Story.SetVar(TrainerHill.Vars.Time, 11 * 3600);
        runner.Start(ScriptLibrary.Default.Find("Owner", "TrainerHillRoof")!, host.Map.Everyone.First(n => n.Key == "hill_owner"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        string prize = TrainerHill.PrizeFor(TrainerHillMode.Expert, 11 * 3600);
        Assert.True(host.Bag.GetQuantity(ItemDatabase.Get(prize)!) > 0, prize);
        Assert.Equal(11 * 3600, host.Story.Var(TrainerHill.Vars.Best(TrainerHillMode.Expert)));
        // Asked again, the owner gives nothing more
        int count = host.Bag.GetQuantity(ItemDatabase.Get(prize)!);
        runner.Start(ScriptLibrary.Default.Find("Owner", "TrainerHillRoof")!, host.Map.Everyone.First(n => n.Key == "hill_owner"));
        runner.RunToEnd();
        Assert.Equal(count, host.Bag.GetQuantity(ItemDatabase.Get(prize)!));

        // Back down in the lobby the climb ends and the team is seen to
        host.Map = Room("TrainerHillLobby");
        host.Party.Members[0].CurrentHP = 1;
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "TrainerHillLobby")!, null);
        runner.RunToEnd();
        Assert.Equal(TrainerHill.States.None, host.Story.Var(TrainerHill.Vars.State));
        Assert.Equal(host.Party.Members[0].MaxHP, host.Party.Members[0].CurrentHP);
    }

    [Fact]
    public void ABattleLostEndsTheClimbAndTheLobbySeesToTheTeam()
    {
        var host = Champion();
        host.Fight = _ => BattleOutcome.Lost;
        TrainerHill.Begin(host.Story, TrainerHillMode.Normal, 0);
        host.Map = TrainerHillFloor.Make(host.Story)!;
        var person = host.Map.Everyone.Single(n => n.Key == TrainerHillFloor.TrainerKey(0));
        host.PlayerTile = (person.GridX, person.GridY + 1);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Trainer", TrainerHillFloor.MapName)!, person);
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal(TrainerHill.States.Lost, host.Story.Var(TrainerHill.Vars.State));
        Assert.False(TrainerHill.IsBeaten(host.Story, 1, 0));
        Assert.Equal("TrainerHillLobby", host.Map.Name);
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "TrainerHillLobby")!, null);
        runner.RunToEnd();
        Assert.Equal(TrainerHill.States.None, host.Story.Var(TrainerHill.Vars.State));
    }

    [Fact]
    public void TwoTrainersWhoBothSeeThePlayerBattleTogether()
    {
        // Find a floor where a tile is seen by both trainers, and fight there
        foreach (var (mode, floor, record) in EveryFloor())
        {
            for (int y = 0; y < TrainerHillFloor.Height; y++)
                for (int x = 0; x < TrainerHillFloor.Width; x++)
                {
                    if (!TrainerHillFloor.Sees(record, record.Spots[0], x, y) || !TrainerHillFloor.Sees(record, record.Spots[1], x, y)) continue;
                    var host = Champion();
                    TrainerHill.Begin(host.Story, mode, 0);
                    host.Story.SetVar(TrainerHill.Vars.Floor, floor);
                    host.Map = TrainerHillFloor.Make(host.Story)!;
                    host.PlayerTile = (x, y);
                    var runner = new ScriptRunner(ScriptLibrary.Default, host);
                    runner.Start(ScriptLibrary.Default.Find("Trainer", TrainerHillFloor.MapName)!, host.Map.Everyone.Single(n => n.Key == TrainerHillFloor.TrainerKey(0)));
                    runner.RunToEnd();
                    var battle = Assert.Single(host.FrontierBattles);
                    Assert.True(battle.Doubles);
                    Assert.Equal(2, battle.Foes.Count);
                    Assert.True(TrainerHill.IsBeaten(host.Story, floor, 0) && TrainerHill.IsBeaten(host.Story, floor, 1));
                    return;
                }
        }
        Assert.Fail("no floor has a tile both its trainers see");
    }

    // ------------------------------------------------------------------ the way in

    [Fact]
    public void Route111sDoorLeadsIntoTheLobbyAndBack()
    {
        Assert.All(Rooms, name => Assert.Equal("Hoenn", RegionDatabase.RegionOfMap(name)?.Id));
        var lobby = Room("TrainerHillLobby");
        var way = Assert.Single(lobby.Warps);
        Assert.Equal(("Hoenn", 231, 114), (way.TargetMap, way.TargetX, way.TargetY));
        var hoenn = MapDatabase.Get("Hoenn");
        var door = hoenn.GetWarpAt(231, 113);
        Assert.NotNull(door);
        Assert.Equal("TrainerHillLobby", door!.TargetMap);
        Assert.True(hoenn.IsWalkable(231, 114));
    }
}
