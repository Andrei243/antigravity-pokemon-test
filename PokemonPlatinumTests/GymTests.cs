using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Gyms of plan 01 · M9, part 1, each on the original's plan with its puzzle as the original's gym code makes it
/// work (<c>src/overlay008/gym_features.c</c>) and its Leader's script played through: the Eterna Gym's flower clock,
/// the Hearthome Gym's dark rooms and their doors, the Veilstone Gym's punching bags, the Pastoria Gym's water.
/// </summary>
[Collection("MapDatabase")]
public class GymTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;

    private static Map Room(string name) =>
        GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();

    /// <summary>Every tile someone on foot can come to from a tile, by the field's own rules (hops included).</summary>
    internal static HashSet<(int X, int Y)> Reach(Map map, int x, int y, float height = 0f)
    {
        var seen = new HashSet<(int, int)> { (x, y) };
        var heights = new Dictionary<(int, int), float> { [(x, y)] = height };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue((x, y));
        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            foreach (var dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var step = FieldMovement.Step(map, cx, cy, dir, new Walker(TravelMode.OnFoot, heights[(cx, cy)]));
                if (!step.Moves || !seen.Add((step.X, step.Y))) continue;
                heights[(step.X, step.Y)] = step.Height;
                queue.Enqueue((step.X, step.Y));
            }
        }
        return seen;
    }

    /// <summary>Whether someone can be walked up to and spoken to from a tile beside them.</summary>
    private static bool CanTalkTo(HashSet<(int X, int Y)> reach, NPC who) =>
        new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }.Any(d => reach.Contains((who.GridX + d.Item1, who.GridY + d.Item2)));

    // ------------------------------------------------------------------ the rooms on the original's plans

    /// <summary>
    /// Each Gym's room (as <c>RoomPlanTests</c> checks the other rooms rebuilt): where its floor begins, the original's
    /// exit mat, and where the door below it leads: out into the city, or for the Hearthome Gym's inner rooms back to
    /// its entrance (their exits, <c>events_hearthome_city_gym_*.json</c>).
    /// </summary>
    public static IEnumerable<object[]> Plans => new[]
    {
        new object[] { "OreburghGym", 1, 3, 5, 24, "Sinnoh" },
        new object[] { "PastoriaGym", 1, 2, 13, 41, "Sinnoh" },
        new object[] { "EternaGym", 1, 3, 11, 27, "Sinnoh" },
        new object[] { "VeilstoneGym", 1, 3, 12, 30, "Sinnoh" },
        new object[] { "HearthomeGym", 1, 3, 4, 8, "Sinnoh" },
        new object[] { "HearthomeGymRoom1", 1, 3, 8, 10, "HearthomeGym" },
        new object[] { "HearthomeGymRoom2", 1, 3, 14, 22, "HearthomeGym" },
        new object[] { "HearthomeGymLeaderRoom", 1, 3, 4, 13, "HearthomeGym" }
    };

    // The original's people (res/field/events/events_<map>.json): local id, tile, facing
    public static IEnumerable<object[]> People => new[]
    {
        new object[] { "OreburghGym", "roark", 5, 3, Direction.Down },
        new object[] { "PastoriaGym", "crasher_wake", 13, 4, Direction.Down },
        new object[] { "PastoriaGym", "gym_guide", 15, 40, Direction.Down },
        new object[] { "PastoriaGym", "tuber_jacky", 11, 33, Direction.Down },
        new object[] { "PastoriaGym", "sailor_damian", 7, 22, Direction.Down },
        new object[] { "PastoriaGym", "tuber_caitlyn", 21, 33, Direction.Left },
        new object[] { "PastoriaGym", "sailor_samson", 5, 8, Direction.Left },
        new object[] { "PastoriaGym", "fisherman_erick", 19, 18, Direction.Left },
        new object[] { "PastoriaGym", "fisherman_walter", 9, 11, Direction.Down },
        new object[] { "OreburghGym", "gym_guide", 6, 23, Direction.Down },
        new object[] { "OreburghGym", "youngster_jonathon", 4, 18, Direction.Right },
        new object[] { "OreburghGym", "youngster_darius", 7, 11, Direction.Left },
        new object[] { "EternaGym", "gym_guide", 9, 25, Direction.Down },
        new object[] { "EternaGym", "gardenia", 11, 3, Direction.Down },
        new object[] { "EternaGym", "lass_caroline", 14, 22, Direction.Left },
        new object[] { "EternaGym", "aroma_lady_jenna", 20, 17, Direction.Up },
        new object[] { "EternaGym", "aroma_lady_angela", 2, 7, Direction.Down },
        new object[] { "VeilstoneGym", "maylene", 12, 4, Direction.Down },
        new object[] { "VeilstoneGym", "gym_guide", 13, 29, Direction.Down },
        new object[] { "VeilstoneGym", "black_belt_colby", 15, 23, Direction.Right },
        new object[] { "VeilstoneGym", "black_belt_darren", 16, 14, Direction.Left },
        new object[] { "VeilstoneGym", "black_belt_jeffery", 2, 9, Direction.Down },
        // The original faces him west, but he only ever looks south (MOVEMENT_TYPE_LOOK_SOUTH)
        new object[] { "VeilstoneGym", "black_belt_rafael", 11, 13, Direction.Down },
        new object[] { "HearthomeGym", "gym_guide", 5, 6, Direction.Down },
        new object[] { "HearthomeGymRoom1", "youngster_donny", 12, 7, Direction.Left },
        new object[] { "HearthomeGymRoom1", "lass_molly", 4, 7, Direction.Right },
        new object[] { "HearthomeGymRoom2", "school_kid_mackenzie", 4, 15, Direction.Right },
        new object[] { "HearthomeGymRoom2", "ace_trainer_allen", 18, 7, Direction.Right },
        new object[] { "HearthomeGymRoom2", "school_kid_chance", 21, 17, Direction.Left },
        new object[] { "HearthomeGymRoom2", "ace_trainer_catherine", 11, 9, Direction.Down },
        new object[] { "HearthomeGymLeaderRoom", "fantina", 4, 10, Direction.Down },
        new object[] { "HearthomeGymLeaderRoom", "bollard_1", 8, 10, Direction.Up },
        new object[] { "HearthomeGymLeaderRoom", "bollard_2", 8, 9, Direction.Up }
    };

    [Theory]
    [MemberData(nameof(Plans))]
    public void EachGymsFloorBeginsWhereTheOriginalsDoesAndItsWayOutIsBelowItsMat(string room, int left, int back, int matX, int matY, string leadsTo)
    {
        var map = Room(room);
        Assert.True(map.IsIndoors);
        Assert.Equal(InteriorStyle.Gym, map.Interior);
        Assert.Equal((left, back), map.RoomCorner());
        Assert.Equal(map.Height - 1, matY + 1);
        Assert.Equal(TileType.Door, map.GetGroundTile(matX, matY + 1));
        Assert.Equal(leadsTo, map.GetWarpAt(matX, matY + 1)?.TargetMap);
        Assert.Equal(leadsTo == "Sinnoh" ? 1 : 0, map.Warps.Count(w => w.TargetMap == "Sinnoh"));
        Assert.True(map.IsWalkable(matX, matY));
    }

    [Theory]
    [MemberData(nameof(People))]
    public void TheOriginalsPeopleStandOnTheOriginalsTiles(string room, string id, int x, int y, Direction facing)
    {
        var map = Room(room);
        var npc = Assert.Single(map.Everyone, n => n.Key == id);
        Assert.Equal((x, y, facing), (npc.GridX, npc.GridY, npc.Facing));
        Assert.False(map.IsSolid(x, y), $"{room}: {id} stands in something solid");
    }

    // ------------------------------------------------------------------ the Oreburgh Gym

    /// <summary>
    /// The Oreburgh Gym stands on the original's tiers (its land data's height plates): the floor by the door, the pit
    /// before it, a second tier at two with a bridge over the pit, and Roark's dais at four, each side of it a tier at
    /// three that the original's edge behaviours keep one from stepping down off or up onto.
    /// </summary>
    [Fact]
    public void TheOreburghGymStandsOnTheOriginalsTiers()
    {
        var map = Room("OreburghGym");
        Assert.True(map.HasRelief);
        Assert.Equal(0f, map.HeightAt(5, 24));
        Assert.Equal(0f, map.HeightAt(3, 16));
        Assert.Equal(2f, map.HeightAt(5, 10));
        Assert.Equal(4f, map.HeightAt(5, 4));
        Assert.Equal(3f, map.HeightAt(1, 4));
        // The bridge over the pit: its deck at the second tier's height, the ground under it the pit's
        Assert.Equal(0f, map.HeightAt(5, 17));
        Assert.Equal(2f, map.DeckAt(5, 17));
        // The dais's sides: a step of one is no cliff, so it is the edge's behaviour that stops it, both ways
        Assert.Equal(TileBehavior.BlockWest, map.BehaviourAt(3, 4));
        Assert.Equal(TileBehavior.BlockEast, map.BehaviourAt(7, 4));
        Assert.False(FieldMovement.Step(map, 3, 4, Direction.Left, new Walker(TravelMode.OnFoot, 4f)).Moves);
        Assert.False(FieldMovement.Step(map, 2, 4, Direction.Right, new Walker(TravelMode.OnFoot, 3f)).Moves);
        Assert.False(FieldMovement.Step(map, 7, 4, Direction.Right, new Walker(TravelMode.OnFoot, 4f)).Moves);
    }

    [Fact]
    public void TheOreburghGymsWaysUpLeadFromTheDoorToRoarkPastBothTrainers()
    {
        var map = Room("OreburghGym");
        var reach = Reach(map, 5, 24);
        foreach (var id in new[] { "roark", "youngster_jonathon", "youngster_darius", "gym_guide" })
            Assert.True(CanTalkTo(reach, map.Everyone.Single(n => n.Key == id)), $"{id} can't be walked up to");
        // Both ways up: over the bridge from the west stairs, and through the pit under it to the middle stairs
        Assert.Contains((5, 17), reach);
        Assert.Contains((5, 13), reach);
        Assert.Contains((2, 19), reach);
        var step = FieldMovement.Step(map, 4, 17, Direction.Right, new Walker(TravelMode.OnFoot, 2f));
        Assert.True(step.Moves);
        Assert.Equal(2f, step.Height);
        step = FieldMovement.Step(map, 5, 18, Direction.Up, new Walker(TravelMode.OnFoot, 0f));
        Assert.True(step.Moves);
        Assert.Equal(0f, step.Height);
    }

    // ------------------------------------------------------------------ the Pastoria Gym

    /// <summary>
    /// Every place someone on foot can come to in the Pastoria Gym from a tile, stepping on the buttons as they go:
    /// the tile, the height they stand at and the button pressed last, by the field's own rules with the water's.
    /// </summary>
    private static Dictionary<(int X, int Y, float Height, PastoriaWater.Button Pressed), (int X, int Y, float Height, PastoriaWater.Button Pressed)?>
        WadeThrough(Map map, int x, int y, float height, bool pressing = true)
    {
        var water = (PastoriaWater)map.Puzzle!;
        var start = (x, y, height, PastoriaWater.Button.Green);
        var from = new Dictionary<(int X, int Y, float Height, PastoriaWater.Button Pressed), (int X, int Y, float Height, PastoriaWater.Button Pressed)?> { [start] = null };
        var queue = new Queue<(int X, int Y, float Height, PastoriaWater.Button Pressed)>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var here = queue.Dequeue();
            water.Settle(here.Pressed);
            foreach (var dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var step = FieldMovement.Step(map, here.X, here.Y, dir, new Walker(TravelMode.OnFoot, here.Height));
                if (!step.Moves) continue;
                var next = (step.X, step.Y, step.Height, (pressing ? PastoriaWater.ButtonAt(step.X, step.Y) : null) ?? here.Pressed);
                if (from.TryAdd(next, here)) queue.Enqueue(next);
            }
        }
        water.Settle(PastoriaWater.Button.Green);
        return from;
    }

    [Fact]
    public void ThePastoriaGymsWaterLeadsFromTheDoorToWakeByTheButtons()
    {
        var map = Room("PastoriaGym");
        Assert.IsType<PastoriaWater>(map.Puzzle);
        var ways = WadeThrough(map, 13, 41, map.HeightAt(13, 41));
        var wake = map.Everyone.Single(n => n.Key == "crasher_wake");
        var beside = ways.Keys.FirstOrDefault(k => (k.X, k.Y) == (wake.GridX, wake.GridY + 1));
        Assert.True(ways.ContainsKey(beside), "Wake can't be walked up to");
        // The way there, button by button
        var presses = new List<string>();
        for (var at = ((int X, int Y, float Height, PastoriaWater.Button Pressed)?)beside; at is { } a; at = ways[a])
            if (ways[a] is { } before && before.Pressed != a.Pressed) presses.Insert(0, $"{a.Pressed} at {a.X},{a.Y}");
        Assert.NotEmpty(presses);
        // and without them the water at two keeps the player from him
        Assert.DoesNotContain(WadeThrough(map, 13, 41, map.HeightAt(13, 41), pressing: false).Keys, k => (k.X, k.Y) == (wake.GridX, wake.GridY + 1));
        // Every trainer can be walked up to
        foreach (var trainer in map.Everyone.Where(n => n.IsTrainer))
            Assert.True(ways.Keys.Any(k => System.Math.Abs(k.X - trainer.GridX) + System.Math.Abs(k.Y - trainer.GridY) == 1), $"{trainer.Key} can't be walked up to");
    }

    /// <summary>
    /// The floor on the water carries someone only where the original's heights say so: onto a raft at the water's
    /// height from a walkway at the same, never where the room's own plate is as near (the plate wins a tie) or the
    /// water is too far below; and the high, middle and low ground are stepped onto only from nought, two and four.
    /// </summary>
    [Fact]
    public void ThePastoriaGymsFloorCarriesWalkersOnlyWhereTheOriginalsDoes()
    {
        var map = Room("PastoriaGym");
        var water = (PastoriaWater)map.Puzzle!;
        var fromTheMiddle = new Walker(TravelMode.OnFoot, 2f);
        // (13, 35) is middle ground, over the pool's floor at nought; the raft north of it is plain, its floor at nought
        Assert.Equal(TileBehavior.PastoriaGymMiddle, map.BehaviourAt(13, 35));
        Assert.Equal(0f, map.HeightAt(13, 34));
        water.Settle(PastoriaWater.Button.Green);
        var step = FieldMovement.Step(map, 13, 35, Direction.Up, fromTheMiddle);
        Assert.True(step.Moves);
        Assert.Equal(2f, step.Height);
        water.Settle(PastoriaWater.Button.Blue);
        Assert.Equal(Obstacle.Cliff, FieldMovement.Step(map, 13, 35, Direction.Up, fromTheMiddle).Obstacle);
        water.Settle(PastoriaWater.Button.Orange);
        Assert.Equal(Obstacle.Cliff, FieldMovement.Step(map, 13, 35, Direction.Up, fromTheMiddle).Obstacle);
        // The middle ground only from two: from the walkway below the entrance's stairs it is, whatever the water
        Assert.True(FieldMovement.Step(map, 13, 36, Direction.Up, fromTheMiddle).Moves);
        Assert.True(water.Refuses(map, 13, 35, 4f, false));
        Assert.True(water.Refuses(map, 13, 35, 0f, false));
        // The floor's own collision: deep water carries nobody on the floor, though its plate is walked when the floor is lower
        var deep = Enumerable.Range(0, map.Height).SelectMany(y => Enumerable.Range(0, map.Width).Select(x => (x, y)))
            .First(t => map.BehaviourAt(t.x, t.y) == TileBehavior.MovingFloor && !map.IsSolid(t.x, t.y));
        Assert.True(water.Refuses(map, deep.x, deep.y, 2f, afloat: true));
        Assert.False(water.Refuses(map, deep.x, deep.y, 2f, afloat: false));
        water.Settle(PastoriaWater.Button.Green);
    }

    [Fact]
    public void ThePastoriaGymsWaterMovesAtTheOriginalsPaceAndCarriesWalkersOnceItStops()
    {
        var water = new PastoriaWater();
        water.Settle(PastoriaWater.Button.Green);
        water.Press(PastoriaWater.Button.Blue);
        Assert.True(water.Moving);
        // The button's own press first, then a sixteenth of a tile a frame at thirty frames a second: two tiles in 32 frames
        for (int frame = 0; frame < 12 + 16; frame++) water.Update(1f / 30f);
        Assert.InRange(water.Level, 2.8f, 3.2f);
        Assert.Equal(2f, water.Floor);
        for (int frame = 0; frame < 20; frame++) water.Update(1f / 30f);
        Assert.False(water.Moving);
        Assert.Equal((4f, 4f), (water.Level, water.Floor));
        // Coming in by the door lays it out with the green down again
        water.Arrive(Room("PastoriaGym"), new StoryState(), new System.Random(0));
        Assert.Equal((PastoriaWater.Button.Green, 2f), (water.Pressed, water.Floor));
    }

    [Fact]
    public void ThePastoriaGymsButtonsArePressedByTheRoomsOwnTriggers()
    {
        var game = new OpeningTests.Game(0);
        game.Arrive("PastoriaGym", 13, 41);
        var water = (PastoriaWater)game.Map.Puzzle!;
        water.Settle(PastoriaWater.Button.Green);
        // The green is down as the player comes in, so only the others fire
        Assert.False(game.Fires("GreenButton"));
        Assert.True(game.Fires("BlueButton"));
        Assert.True(game.Fires("OrangeButton"));
        Assert.Contains("pressbutton orange", game.Step("OrangeButton").Log);
        Assert.Equal(0f, water.Floor);
        Assert.False(game.Fires("OrangeButton"));
        Assert.True(game.Fires("GreenButton"));
        Assert.Contains("pressbutton blue", game.Step("BlueButton").Log);
        Assert.Equal(4f, water.Floor);
        // Each of the original's coordinate events is a button of the puzzle's table, of its colour
        Assert.Equal(PastoriaWater.Buttons.Length, game.Map.Triggers.Count);
        foreach (var trigger in game.Map.Triggers)
            Assert.Equal(trigger.Script, PastoriaWater.ButtonAt(trigger.X, trigger.Y) + "Button");
        water.Settle(PastoriaWater.Button.Green);
    }

    [Fact]
    public void ThePastoriaGymPlaysThroughToTheFenBadge()
    {
        var game = new OpeningTests.Game(0);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Luxray")!, 45));
        game.Arrive("PastoriaGym", 13, 41);
        Assert.Contains(game.Talk("gym_guide").Transcript, l => l.Text.Contains("button"));
        foreach (var id in new[] { "tuber_jacky", "sailor_damian" })
            Assert.Contains(game.Talk(id).Log, l => l.StartsWith($"battle {id} Won"));

        var gym = game.Talk("crasher_wake");
        Assert.Contains("battle leader_wake Won", gym.Log);
        Assert.True(game.Story.HasBadge(Badge.Fen));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM55")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_WAKE_TM55"));
        Assert.True(game.Story.Has("FLAG_HIDE_PASTORIA_CITY_GRUNT_M"));
        Assert.Equal(3, game.Story.Var("VAR_PASTORIA_CITY_STATE"));
        // His trainers count as beaten, those not yet fought too
        Assert.DoesNotContain(game.Talk("fisherman_erick").Log, l => l.StartsWith("battle"));
        Assert.DoesNotContain(game.Talk("crasher_wake").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM55")!));
    }

    // ------------------------------------------------------------------ the Eterna Gym

    [Fact]
    public void TheFlowerClockKeepsTheOriginalsTimesTablesAndHops()
    {
        // sEternaGymClockTimes
        Assert.Equal(new[] { (7, 25), (6, 15), (9, 15), (0, 45), (0, 30) }, EternaClock.Times);
        // At a quarter past six the hour hand points south down the face and the minute hand east
        for (int z = 14; z <= 18; z++) Assert.False(EternaClock.Closed(EternaClock.FirstTrainer, 11, z));
        for (int x = 12; x <= 17; x++) Assert.False(EternaClock.Closed(EternaClock.FirstTrainer, x, 13));
        Assert.True(EternaClock.Closed(EternaClock.FirstTrainer, 10, 15));
        // At first nothing of the face is open but its bottom row
        Assert.True(EternaClock.Closed(EternaClock.Initial, 11, 13));
        Assert.False(EternaClock.Closed(EternaClock.Initial, 11, 19));
        // The fountains: both full, then the right one drained, then both
        Assert.True(EternaClock.Closed(EternaClock.FirstTrainer, 2, 19) && EternaClock.Closed(EternaClock.FirstTrainer, 20, 19));
        Assert.True(EternaClock.Closed(EternaClock.SecondTrainer, 2, 19) && !EternaClock.Closed(EternaClock.SecondTrainer, 20, 19));
        Assert.False(EternaClock.Closed(EternaClock.ThirdTrainer, 2, 19) || EternaClock.Closed(EternaClock.ThirdTrainer, 20, 19));
        // The hour hand's tip is hopped over along the hand only
        var clock = new EternaClock();
        var map = Room("EternaGym");
        var story = new StoryState();
        story.SetVar(EternaClock.StateVar, EternaClock.FirstTrainer);
        clock.Apply(map, story);
        Assert.True(clock.HopsOver(11, 18, Direction.Up));
        Assert.True(clock.HopsOver(11, 18, Direction.Down));
        Assert.False(clock.HopsOver(11, 18, Direction.Left));
        Assert.False(clock.HopsOver(6, 13, Direction.Right));
    }

    [Fact]
    public void EachTimeOfTheClockLetsThePlayerOnToTheNextTrainerAndNoFurther()
    {
        var map = Room("EternaGym");
        var story = new StoryState();
        Assert.Equal(EternaClock.PuzzleName, map.Puzzle!.Name);
        NPC Who(string id) => map.Everyone.Single(n => n.Key == id);
        var (caroline, jenna, angela, gardenia) = (Who("lass_caroline"), Who("aroma_lady_jenna"), Who("aroma_lady_angela"), Who("gardenia"));

        HashSet<(int X, int Y)> At(int state)
        {
            story.SetVar(EternaClock.StateVar, state);
            map.Puzzle.Apply(map, story);
            return Reach(map, 11, 27);
        }

        var reach = At(EternaClock.Initial);
        Assert.True(CanTalkTo(reach, caroline));
        Assert.False(CanTalkTo(reach, jenna));
        Assert.False(CanTalkTo(reach, angela));

        // A quarter past six: up the hour hand, over its tip, and east along the minute hand to Jenna
        reach = At(EternaClock.FirstTrainer);
        Assert.True(CanTalkTo(reach, jenna));
        Assert.False(CanTalkTo(reach, angela));
        Assert.Equal(StepKind.Hop, FieldMovement.Step(map, 11, 19, Direction.Up, new Walker()).Kind);
        Assert.Equal((11, 17), (FieldMovement.Step(map, 11, 19, Direction.Up, new Walker()).X, FieldMovement.Step(map, 11, 19, Direction.Up, new Walker()).Y));

        // A quarter past nine: across the face from east to west, over the hour hand's tip, to Angela
        reach = At(EternaClock.SecondTrainer);
        Assert.True(CanTalkTo(reach, angela));
        Assert.False(CanTalkTo(reach, gardenia));

        // A quarter to one: north up the hour hand to the Leader
        reach = At(EternaClock.ThirdTrainer);
        Assert.True(CanTalkTo(reach, gardenia));

        // Half past twelve: the hands lie in one line down the middle, the straight way back to the door
        At(EternaClock.Leader);
        var back = Reach(map, 11, 4);
        Assert.Contains((11, 27), back);
    }

    [Fact]
    public void TheEternaGymPlaysThroughToTheForestBadge()
    {
        var game = new OpeningTests.Game(0);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Monferno")!, 30));
        game.Arrive("EternaGym", 11, 27);
        var map = game.Map;
        map.Puzzle!.Apply(map, game.Story);

        // The guide speaks of the clock; the statue lists the rival alone
        Assert.Contains(game.Talk("gym_guide").Transcript, l => l.Text.Contains("clock"));

        // Each trainer battles once, in order, and turns the clock on
        foreach (var (id, state) in new[] { ("lass_caroline", 1), ("aroma_lady_jenna", 2), ("aroma_lady_angela", 3) })
        {
            var host = game.Talk(id);
            Assert.Contains(host.Log, l => l.StartsWith($"battle {id} Won"));
            Assert.Contains($"flowerclock {state - 1} {state}", host.Log);
            Assert.Equal(state, game.Story.Var(EternaClock.TrainersVar));
            Assert.Equal(state, game.Story.Var(EternaClock.StateVar));
            // A fountain drains with the second and the third
            Assert.Equal(state >= 2, host.Transcript.Any(l => l.Text.Contains("fountain")));
            Assert.DoesNotContain(game.Talk(id).Log, l => l.StartsWith("battle"));
        }

        // Gardenia: the battle, the Forest Badge, TM86, her trainers counted as beaten, and the clock's last time
        var gym = game.Talk("gardenia");
        Assert.Contains("battle leader_gardenia Won", gym.Log);
        Assert.True(game.Story.HasBadge(Badge.Forest));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM86")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_GARDENIA_TM86"));
        Assert.False(game.Story.Has("FLAG_HIDE_ETERNA_FOREST_GARDENIA"));
        Assert.True(game.Story.HasDefeated("beauty_lindsay"));
        Assert.Equal(EternaClock.Leader, game.Story.Var(EternaClock.StateVar));
        Assert.DoesNotContain(game.Talk("gardenia").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM86")!));

        // Once she is beaten the statue lists the player too
        Assert.Contains(game.Talk("gym_guide").Transcript, l => l.Text.Contains("beat Gardenia"));
    }

    [Fact]
    public void TheEternaGymsDoorLeadsInFromTheCity()
    {
        var world = MapDatabase.Get("Sinnoh");
        var door = world.Warps.Single(w => w.TargetMap == "EternaGym");
        Assert.Equal((312, 562), (door.SourceX, door.SourceY));
        var room = Room("EternaGym");
        Assert.True(room.IsWalkable(door.TargetX, door.TargetY));
        var back = Assert.Single(room.Warps);
        Assert.Equal(("Sinnoh", 312, 563), (back.TargetMap, back.TargetX, back.TargetY));
    }
    // ------------------------------------------------------------------ the Veilstone Gym

    private static NPC BagAt(Map map, int x, int y) => map.NPCs.Single(n => VeilstoneBags.IsBag(n) && (n.GridX, n.GridY) == (x, y));
    private static bool StackAt(Map map, int x, int y) => map.NPCs.Any(n => VeilstoneBags.IsTireStack(n) && (n.GridX, n.GridY) == (x, y));

    [Fact]
    public void TheBagsRunAlongTheOriginalsTracksAndKnockTheStacksDown()
    {
        var map = Room("VeilstoneGym");
        var story = new StoryState();
        Assert.Equal(VeilstoneBags.PuzzleName, map.Puzzle!.Name);
        map.Puzzle.Arrive(map, story, new System.Random(0));
        Assert.Equal(9, map.NPCs.Count(VeilstoneBags.IsBag));
        Assert.Equal(11, map.NPCs.Count(VeilstoneBags.IsTireStack));

        // The bag at the top of the dojo, kicked east: three tiles to the point it stops at, where a stack stands in
        // front of it (VeilstoneGym_CalculateDistanceBagWillTravel, VEILSTONE_TILE_FLAG_TIRE_STACK_PRESENT)
        var bag = BagAt(map, 8, 7);
        Assert.Equal((0, VeilstoneBags.Flags.Blocked), VeilstoneBags.Run(8, 7, Direction.Up, (x, y) => StackAt(map, x, y)));
        Assert.Equal(0, VeilstoneBags.KickBag(map, bag, Direction.Left).Distance);
        Assert.Equal((8, 7), (bag.GridX, bag.GridY));
        var kick = VeilstoneBags.KickBag(map, bag, Direction.Right);
        Assert.Equal((3, 11, 7), (kick.Distance, bag.GridX, bag.GridY));
        Assert.NotNull(kick.Toppled);
        Assert.False(StackAt(map, 12, 7));
        Assert.Contains(kick.Toppled, map.Absent);

        // From the point it stopped at it goes on to the end of its track, and that stack falls too
        kick = VeilstoneBags.KickBag(map, bag, Direction.Right);
        Assert.Equal((10, 21, 7), (kick.Distance, bag.GridX, bag.GridY));
        Assert.False(StackAt(map, 22, 7));

        // A bag on a tile that lets it go only one way along its track (3: never west or east; 5: never west)
        Assert.Equal(VeilstoneBags.Flags.Blocked, VeilstoneBags.FlagsAt(3, 12, Direction.Right, (_, _) => false));
        Assert.Equal(VeilstoneBags.Flags.Blocked, VeilstoneBags.FlagsAt(4, 12, Direction.Left, (_, _) => false));
        Assert.Equal(VeilstoneBags.Flags.None, VeilstoneBags.FlagsAt(4, 12, Direction.Right, (_, _) => false));

        // Coming in again puts every bag and stack back where it was
        map.Puzzle.Arrive(map, story, new System.Random(0));
        Assert.Equal((8, 7), (bag.GridX, bag.GridY));
        Assert.True(StackAt(map, 12, 7) && StackAt(map, 22, 7));
    }

    /// <summary>
    /// Kicks bags until the Leader can be walked up to: every bag the player can stand beside, kicked every way it can
    /// be from there, breadth first. Returns the kicks (the bag's tile and the way) or null.
    /// </summary>
    internal static List<(int X, int Y, Direction Way)>? SolveVeilstone(Map map, StoryState story, (int X, int Y) door, NPC leader)
    {
        var start = new List<(int, int, Direction)>();
        string Key() => string.Join(";", map.NPCs.Where(n => VeilstoneBags.IsBag(n) || VeilstoneBags.IsTireStack(n))
            .Select(n => $"{n.NpcType[0]}{n.GridX},{n.GridY}").OrderBy(k => k));
        void Replay(List<(int X, int Y, Direction Way)> kicks)
        {
            map.Puzzle!.Arrive(map, story, new System.Random(0));
            foreach (var (x, y, way) in kicks) VeilstoneBags.KickBag(map, BagAt(map, x, y), way);
        }
        var seen = new HashSet<string>();
        var queue = new Queue<List<(int X, int Y, Direction Way)>>();
        queue.Enqueue(start);
        while (queue.Count > 0 && seen.Count < 20000)
        {
            var kicks = queue.Dequeue();
            Replay(kicks);
            if (!seen.Add(Key())) continue;
            var reach = Reach(map, door.X, door.Y);
            if (CanTalkTo(reach, leader)) return kicks;
            foreach (var bag in map.NPCs.Where(VeilstoneBags.IsBag).ToList())
                foreach (var way in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
                {
                    var (dx, dy) = FieldMovement.Delta(way);
                    if (!reach.Contains((bag.GridX - dx, bag.GridY - dy))) continue;
                    if (VeilstoneBags.Run(bag.GridX, bag.GridY, way, (x, y) => StackAt(map, x, y)).Distance == 0) continue;
                    queue.Enqueue(new List<(int, int, Direction)>(kicks) { (bag.GridX, bag.GridY, way) });
                }
        }
        return null;
    }

    [Fact]
    public void TheStacksKeepMayleneOutOfReachUntilTheBagsHaveKnockedThemDown()
    {
        var map = Room("VeilstoneGym");
        var story = new StoryState();
        var maylene = map.Everyone.Single(n => n.Key == "maylene");
        map.Puzzle!.Arrive(map, story, new System.Random(0));
        Assert.False(CanTalkTo(Reach(map, 12, 30), maylene));
        var kicks = SolveVeilstone(map, story, (12, 30), maylene);
        Assert.NotNull(kicks);
        Assert.True(CanTalkTo(Reach(map, 12, 30), maylene));
        // Every one of the four black belts can be come to on the way
        foreach (var id in new[] { "black_belt_colby", "black_belt_darren", "black_belt_jeffery", "black_belt_rafael" })
            Assert.True(CanTalkTo(Reach(map, 12, 30), map.NPCs.Single(n => n.Key == id)), id);
    }

    [Fact]
    public void TheVeilstoneGymPlaysThroughToTheCobbleBadge()
    {
        var game = new OpeningTests.Game(0);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 45));
        game.Arrive("VeilstoneGym", 12, 30);

        Assert.Contains(game.Talk("gym_guide").Transcript, l => l.Text.Contains("punching bag"));
        foreach (var id in new[] { "black_belt_colby", "black_belt_darren", "black_belt_jeffery", "black_belt_rafael" })
        {
            Assert.Contains(game.Talk(id).Log, l => l.StartsWith($"battle {id} Won"));
            Assert.DoesNotContain(game.Talk(id).Log, l => l.StartsWith("battle"));
        }

        var gym = game.Talk("maylene");
        Assert.Contains("battle leader_maylene Won", gym.Log);
        Assert.True(game.Story.HasBadge(Badge.Cobble));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM60")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_MAYLENE_TM60"));
        Assert.True(game.Story.Has("FLAG_HIDE_GAME_CORNER_LOOKER"));
        Assert.False(game.Story.Has("FLAG_HIDE_VEILSTONE_COUNTERPART"));
        Assert.Equal(1, game.Story.Var("VAR_VEILSTONE_WAREHOUSE_GUARDS_FIGHTABLE"));
        Assert.Equal(1, game.Story.Var("VAR_VEILSTONE_CITY_COUNTERPART_NEEDS_HELP_STATE"));
        Assert.DoesNotContain(game.Talk("maylene").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM60")!));
    }

    [Fact]
    public void AMayleneBeatenFirstCountsHerBlackBeltsAsBeaten()
    {
        var game = new OpeningTests.Game(0);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 45));
        game.Arrive("VeilstoneGym", 12, 30);
        game.Talk("maylene");
        foreach (var id in new[] { "black_belt_colby", "black_belt_darren", "black_belt_jeffery", "black_belt_rafael" })
        {
            Assert.True(game.Story.HasDefeated(id), id);
            Assert.DoesNotContain(game.Talk(id).Log, l => l.StartsWith("battle"));
        }
    }

    // ------------------------------------------------------------------ the Hearthome Gym

    [Theory]
    [InlineData("HearthomeGymRoom1", 1, "HearthomeGymRoom2")]
    [InlineData("HearthomeGymRoom2", 2, "HearthomeGymLeaderRoom")]
    public void EachDarkRoomChoosesItsDoorAndShowsItsSignAsTheOriginalDoes(string name, int number, string next)
    {
        var map = Room(name);
        var story = new StoryState();
        var rooms = Assert.IsType<HearthomeDoors>(map.Puzzle);
        Assert.Equal(number == 1 ? 3 : 5, rooms.Doors.Count);

        // The original's table of where the sign may lie is the floor and only the floor
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (HearthomeDoors.ClueMayLie(number, x, y)) Assert.False(map.IsSolid(x, y), $"{name}: the sign may lie on ({x}, {y})");

        var chosen = new HashSet<HearthomeDoors.Sign>();
        var clues = new HashSet<(int, int)>();
        for (int seed = 0; seed < 60; seed++)
        {
            rooms.Arrive(map, story, new System.Random(seed));
            var correct = rooms.Correct!.Value;
            chosen.Add(correct);
            clues.Add(rooms.Clue);
            Assert.True(HearthomeDoors.ClueMayLie(number, rooms.Clue.X, rooms.Clue.Y));
            Assert.DoesNotContain(map.NPCs, n => (n.GridX, n.GridY) == rooms.Clue);
            foreach (var (sign, x, y) in rooms.Doors)
            {
                var warp = map.GetWarpAt(x, y)!;
                if (sign == correct) Assert.Equal(next, warp.TargetMap);
                else Assert.Equal((HearthomeDoors.Entrance, 4, 3, Direction.Down), (warp.TargetMap, warp.TargetX, warp.TargetY, warp.TargetFacing));
            }
        }
        // Every door is the way on some of the time, and the sign lies all over the room
        Assert.Equal(rooms.Doors.Count, chosen.Count);
        Assert.True(clues.Count > 20, $"the sign lay on only {clues.Count} tiles");

        // Every door can be walked up to from the way in
        var reach = Reach(map, map.Warps.Single(w => w.TargetMap == HearthomeDoors.Entrance && w.SourceY == map.Height - 1).SourceX, map.Height - 2);
        foreach (var (_, x, y) in rooms.Doors) Assert.Contains((x, y + 1), reach);
    }

    [Fact]
    public void TheDarkIsTheOriginalsFogLiftedRoundEveryLight()
    {
        var map = Room("HearthomeGymRoom2");
        var story = new StoryState();
        var rooms = (HearthomeDoors)map.Puzzle!;
        rooms.Apply(map, story);
        // HearthomeGym_InitFog: 119 of 127 in the second room, 109 in the first, 91 once the Relic Badge is won
        Assert.Equal(119 / 127f, rooms.Fog);
        var first = Room("HearthomeGymRoom1");
        first.Puzzle!.Apply(first, story);
        Assert.Equal(109 / 127f, ((HearthomeDoors)first.Puzzle).Fog);
        story.GiveBadge(Badge.Relic);
        rooms.Apply(map, story);
        Assert.Equal(91 / 127f, rooms.Fog);
        // Clear at a light, thick past its reach; and only the trainers carry one besides the player
        Assert.Equal(0f, HearthomeDoors.FogAt(0f));
        Assert.Equal(1f, HearthomeDoors.FogAt(HearthomeDoors.LightRadius + HearthomeDoors.LightSoft));
        Assert.Equal(4, HearthomeDoors.LightBearers(map).Count());
        // Not a cave's dark: Flash has nothing to light here
        Assert.False(map.IsDark);
    }

    [Fact]
    public void TheHearthomeGymPlaysThroughToTheRelicBadge()
    {
        var game = new OpeningTests.Game(0);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Monferno")!, 38));

        // The first time in, the guide comes over to explain; the second, he stays put
        game.Arrive("HearthomeGym", 4, 8);
        Assert.Contains(game.Said, l => l.Text.Contains("dark"));
        Assert.Equal(1, game.Story.Var("VAR_HAS_ENTERED_HEARTHOME_GYM_BEFORE"));
        int played = game.Played.Count;
        game.Arrive("HearthomeGym", 4, 8);
        Assert.True(game.Played.Count == played || !game.Said.Any());

        // Fantina's room: the bollards keep the pad back to the entrance shut until she is beaten
        game.Arrive("HearthomeGymLeaderRoom", 4, 13);
        var map = game.Map;
        Assert.DoesNotContain((14, 3), Reach(map, 4, 13));
        var gym = game.Talk("fantina");
        Assert.Contains("battle leader_fantina Won", gym.Log);
        Assert.True(game.Story.HasBadge(Badge.Relic));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM65")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_FANTINA_TM65"));
        Assert.True(game.Story.Has("FLAG_HIDE_HEARTHOME_CITY_GYM_BOLLARDS"));
        Assert.True(game.Story.Has("FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_BLOCKADE"));
        Assert.False(game.Story.Has("FLAG_HIDE_HEARTHOME_CITY_ROUTE_209_GATE_RIVAL"));
        Assert.Equal(1, game.Story.Var("VAR_ROUTE_209_GATE_TO_HEARTHOME_CITY_STATE"));
        foreach (var id in new[] { "youngster_donny", "lass_molly", "school_kid_mackenzie", "ace_trainer_allen", "school_kid_chance", "ace_trainer_catherine", "camper_drew", "picnicker_cheyenne" })
            Assert.True(game.Story.HasDefeated(id), id);
        map.ApplyPresence(game.Story.Has);
        Assert.Contains((14, 3), Reach(map, 4, 13));
        Assert.DoesNotContain(game.Talk("fantina").Log, l => l.StartsWith("battle"));
    }

    // ------------------------------------------------------------------ the doors from the cities

    [Theory]
    [InlineData("OreburghGym")]
    [InlineData("PastoriaGym")]
    [InlineData("EternaGym")]
    [InlineData("VeilstoneGym")]
    [InlineData("HearthomeGym")]
    public void EachGymsDoorLeadsInFromItsCityAndBackOut(string gym)
    {
        var world = MapDatabase.Get("Sinnoh");
        var door = world.Warps.Single(w => w.TargetMap == gym);
        var room = Room(gym);
        Assert.True(room.IsWalkable(door.TargetX, door.TargetY));
        var back = Assert.Single(room.Warps, w => w.TargetMap == "Sinnoh");
        Assert.Equal((door.SourceX, door.SourceY + 1), (back.TargetX, back.TargetY));
        // The way out is open ground; only someone a story flag takes off the map may stand on it (Gardenia, at her
        // door until she is spoken to, plan 02 · S6)
        Assert.False(world.IsSolid(back.TargetX, back.TargetY));
        Assert.All(world.NPCs.Where(n => (n.GridX, n.GridY) == (back.TargetX, back.TargetY)), n => Assert.NotNull(n.HiddenBy));
    }
}
