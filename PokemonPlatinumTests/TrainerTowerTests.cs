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
/// FireRed's Trainer Tower on Seven Island (plan 37 · FC6; rulings, "The Trainer Tower"): its four challenges of eight
/// floors whole in the data and legal, the floors' kinds and which of the six each brings, the foes at the challenger's
/// highest level, a knockout's three one after another, a climb from the counter to the roof played headless, the prize
/// once a challenge and the best time kept.
/// </summary>
[Collection("MapDatabase")]
public class TrainerTowerTests
{
    /// <summary>The tower's rooms of ours, laid out for what they hold and not on the hand-made rooms' plan (<c>RoomPlanTests</c>).</summary>
    internal static readonly string[] Rooms = { "TrainerTowerLobby", "TrainerTowerFloor", "TrainerTowerRoof" };

    private static Map Room(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    [Fact]
    public void EveryChallengeHasEightFloorsOfFireRedsKindsAndEveryPokemonIsLegal()
    {
        Assert.Equal(4, TrainerTowerDatabase.Challenges.Count);
        Assert.Equal(Enum.GetNames<TrainerTowerChallenge>(), TrainerTowerDatabase.Challenges.Select(c => c.Name));
        foreach (var challenge in Enum.GetValues<TrainerTowerChallenge>())
            for (int floor = 1; floor <= TrainerTower.Floors; floor++)
            {
                var record = TrainerTowerDatabase.Floor((int)challenge, floor);
                // The three named challenges are of their own kind; the Mixed one's floors are each of one kind
                if (challenge != TrainerTowerChallenge.Mixed) Assert.Equal((int)challenge, record.Kind);
                Assert.Equal(record.Kind + 1, record.Trainers.Count);
                Assert.NotNull(ItemDatabase.Get(record.Prize));
                foreach (var trainer in record.Trainers)
                {
                    Assert.False(string.IsNullOrEmpty(trainer.Name));
                    Assert.Equal(6, trainer.Team.Count);
                }
                // What the floor brings at its own place in the climb is made, every move one the data has
                foreach (int beaten in Enumerable.Range(0, record.Kind == TrainerTower.Knockout ? 3 : 1))
                    foreach (var foe in TrainerTower.OpponentsOf(challenge, floor, beaten, 60))
                        foreach (var mon in foe.Party.Members)
                        {
                            Assert.Equal(60, mon.Level);
                            Assert.InRange(mon.Moves.Count, 1, 4);
                            Assert.True(mon.EvHP + mon.EvAttack + mon.EvDefense + mon.EvSpeed + mon.EvSpAttack + mon.EvSpDefense <= 510);
                        }
            }
        Assert.Equal(new[] { 0, 2 }, TrainerTowerDatabase.SingleSlots[0]);
        Assert.Equal(new[] { 1, 4, 5 }, TrainerTowerDatabase.KnockoutSlots[7]);
    }

    [Fact]
    public void AFloorBringsWhatItsKindBringsAtTheChallengersHighestLevel()
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Charizard")!, 74, new Random(1)));
        party.Add(new Pokemon(PokemonDatabase.Get("Lapras")!, 81, new Random(2)));
        int level = TrainerHill.LevelFor(party);
        Assert.Equal(81, level);
        var single = TrainerTower.OpponentsOf(TrainerTowerChallenge.Single, 1, 0, level);
        Assert.Single(single);
        Assert.Equal(2, single[0].Party.Count);
        var dbl = TrainerTower.OpponentsOf(TrainerTowerChallenge.Double, 3, 0, level);
        Assert.Equal(2, dbl.Count);
        Assert.All(dbl, t => Assert.Single(t.Party.Members));
        var first = TrainerTower.OpponentsOf(TrainerTowerChallenge.Knockout, 2, 0, level);
        var third = TrainerTower.OpponentsOf(TrainerTowerChallenge.Knockout, 2, 2, level);
        Assert.Single(first);
        Assert.Single(first[0].Party.Members);
        Assert.NotEqual(first[0].Name, third[0].Name);
        Assert.All(single.Concat(dbl).Concat(first).SelectMany(t => t.Party.Members), m => Assert.Equal(81, m.Level));
    }

    [Fact]
    public void AKnockoutFloorIsClearedOnlyOnceItsThreeAreBeaten()
    {
        var story = new StoryState();
        TrainerTower.Begin(story, TrainerTowerChallenge.Knockout, 0);
        Assert.False(TrainerTower.Won(story));
        Assert.False(TrainerTower.Won(story));
        Assert.True(TrainerTower.Won(story));
        Assert.False(TrainerTower.Climb(story, 10));
        Assert.Equal(2, TrainerTower.FloorOf(story));
        Assert.Equal(0, story.Var(TrainerTower.Vars.Beaten));
        var other = new StoryState();
        TrainerTower.Begin(other, TrainerTowerChallenge.Double, 0);
        Assert.True(TrainerTower.Won(other));
    }

    [Fact]
    public void TheClockStopsAtTheRoofAndTheBestIsKeptForEachChallenge()
    {
        var story = new StoryState();
        TrainerTower.Begin(story, TrainerTowerChallenge.Mixed, 100);
        for (int floor = 1; floor < TrainerTower.Floors; floor++) Assert.False(TrainerTower.Climb(story, 200));
        Assert.True(TrainerTower.Climb(story, 400));
        Assert.Equal(TrainerTower.States.Reached, story.Var(TrainerTower.Vars.State));
        Assert.Equal(300 * 60, TrainerTower.Frames(story, 9999));
        Assert.Equal(0, TrainerTower.CheckTime(story));
        Assert.Equal(2, TrainerTower.CheckTime(story));
        Assert.Equal(300 * 60, story.Var(TrainerTower.Vars.Best(TrainerTowerChallenge.Mixed)));
        Assert.Equal(0, story.Var(TrainerTower.Vars.Best(TrainerTowerChallenge.Single)));
        TrainerTower.Begin(story, TrainerTowerChallenge.Mixed, 0);
        for (int floor = 1; floor <= TrainerTower.Floors; floor++) TrainerTower.Climb(story, 500);
        Assert.Equal(1, TrainerTower.CheckTime(story));
        // An hour or more is the longest time
        TrainerTower.Begin(story, TrainerTowerChallenge.Single, 0);
        Assert.Equal(TrainerTower.MaxFrames, TrainerTower.Frames(story, 99999));
    }

    private static HeadlessScriptHost Champion()
    {
        var host = new HeadlessScriptHost { Region = RegionDatabase.Kanto };
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Venusaur")!, 72, new Random(3)));
        host.Party.Add(new Pokemon(PokemonDatabase.Get("Pidgeot")!, 70, new Random(4)));
        host.Story.Set("KantoHallOfFame");
        host.MapNamed = Room;
        return host;
    }

    [Fact]
    public void TheCounterIsShutBeforeKantosHallOfFame()
    {
        var host = new HeadlessScriptHost { Region = RegionDatabase.Kanto, MapNamed = Room };
        host.Map = Room("TrainerTowerLobby");
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("Counter", "TrainerTowerLobby")!, host.Map.Everyone.First(n => n.Key == "tower_attendant"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal(TrainerTower.States.None, host.Story.Var(TrainerTower.Vars.State));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    public void AClimbFromTheCounterToTheRoofBeatsEveryFloorAndWinsThePrizeOnce(int pick)
    {
        var challenge = (TrainerTowerChallenge)pick;
        var host = Champion();
        host.Map = Room("TrainerTowerLobby");
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(pick);
        host.PlayTimeSeconds = 50;
        runner.Start(ScriptLibrary.Default.Find("Counter", "TrainerTowerLobby")!, host.Map.Everyone.First(n => n.Key == "tower_attendant"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal((TrainerTower.States.Climbing, pick, 1), (host.Story.Var(TrainerTower.Vars.State), host.Story.Var(TrainerTower.Vars.Challenge), host.Story.Var(TrainerTower.Vars.Floor)));
        Assert.Equal("TrainerTowerFloor", host.Map.Name);

        int battles = 0;
        for (int floor = 1; floor <= TrainerTower.Floors; floor++)
        {
            Assert.Equal("TrainerTowerFloor", host.Map.Name);
            runner.Start(ScriptLibrary.Default.Find("OnEnter", "TrainerTowerFloor")!, null);
            runner.RunToEnd();
            int kind = TrainerTower.KindOf(challenge, floor);
            for (int fight = 0; fight < (kind == TrainerTower.Knockout ? 3 : 1); fight++)
            {
                var person = host.Map.Everyone.Single(n => n.Key == "opponent");
                int before = host.FrontierBattles.Count;
                runner.Start(ScriptLibrary.Default.Find("Trainer", "TrainerTowerFloor")!, person);
                runner.RunToEnd();
                Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
                var battle = host.FrontierBattles[before];
                Assert.Same(host.Party, battle.Team);
                Assert.Equal(kind == TrainerTower.Double, battle.Doubles);
                Assert.All(battle.Foes, foe => Assert.All(foe.Party.Members, m => Assert.Equal(72, m.Level)));
                battles++;
            }
        }
        Assert.Equal("TrainerTowerRoof", host.Map.Name);
        Assert.Equal(TrainerTower.States.Reached, host.Story.Var(TrainerTower.Vars.State));
        Assert.True(battles >= TrainerTower.Floors);

        var owner = host.Map.Everyone.First(n => n.Key == "tower_owner");
        string prize = TrainerTower.PrizeOf(challenge);
        int had = host.Bag.GetQuantity(ItemDatabase.Get(prize)!);
        runner.Start(ScriptLibrary.Default.Find("Owner", "TrainerTowerRoof")!, owner);
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal(had + 1, host.Bag.GetQuantity(ItemDatabase.Get(prize)!));
        Assert.True(host.Story.Var(TrainerTower.Vars.Best(challenge)) > 0);
        runner.Start(ScriptLibrary.Default.Find("Owner", "TrainerTowerRoof")!, owner);
        runner.RunToEnd();
        Assert.Equal(had + 1, host.Bag.GetQuantity(ItemDatabase.Get(prize)!));

        // Back down, the climb ends and the team is seen to; another climb of the same challenge gives no prize again
        host.Map = Room("TrainerTowerLobby");
        host.Party.Members[0].CurrentHP = 1;
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "TrainerTowerLobby")!, null);
        runner.RunToEnd();
        Assert.Equal(TrainerTower.States.None, host.Story.Var(TrainerTower.Vars.State));
        Assert.Equal(host.Party.Members[0].MaxHP, host.Party.Members[0].CurrentHP);
        Assert.Equal(1, host.Story.Var(TrainerTower.Vars.Prize(challenge)));
    }

    [Fact]
    public void ABattleLostEndsTheClimbAndAFloorSavedOnIsLaidAgain()
    {
        var host = Champion();
        TrainerTower.Begin(host.Story, TrainerTowerChallenge.Double, 0);
        host.Map = Room("TrainerTowerFloor");
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        // Come back to (a game loaded on the floor): the floor's two stand dressed as FireRed's pair
        runner.Start(ScriptLibrary.Default.Find("OnEnter", "TrainerTowerFloor")!, null);
        runner.RunToEnd();
        Assert.True(host.Story.Has("FLAG_KANTO_TRAINER_TOWER_SHOW_OPPONENT"));
        Assert.True(host.Story.Has("FLAG_KANTO_TRAINER_TOWER_SHOW_OPPONENT_2"));
        host.Fight = _ => BattleOutcome.Lost;
        runner.Start(ScriptLibrary.Default.Find("Trainer", "TrainerTowerFloor")!, host.Map.Everyone.Single(n => n.Key == "opponent"));
        runner.RunToEnd();
        Assert.True(host.Problems.Count == 0, string.Join("; ", host.Problems));
        Assert.Equal(TrainerTower.States.Lost, host.Story.Var(TrainerTower.Vars.State));
        Assert.Equal("TrainerTowerLobby", host.Map.Name);
    }

    [Fact]
    public void TheTowersDoorOnSevenIslandLeadsIntoTheLobbyAndBack()
    {
        var lobby = Room("TrainerTowerLobby");
        var exit = lobby.Warps.Single();
        Assert.Equal("SevenIsland", exit.TargetMap);
        var island = MapDatabase.Get("SevenIsland");
        Assert.True(island.IsWalkable(exit.TargetX, exit.TargetY), "the lobby's way out lands on a blocked tile");
        Assert.Contains(island.Warps, w => w.TargetMap == "TrainerTowerLobby");
    }
}
