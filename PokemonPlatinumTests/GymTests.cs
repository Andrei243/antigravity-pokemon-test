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
/// the Hearthome Gym's dark rooms and their doors, the Veilstone Gym's punching bags, the Pastoria Gym's water, and of
/// part 2 the Canalave Gym's lifts and the Snowpoint Gym's ice and snowballs.
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
        new object[] { "CanalaveGym", 1, 3, 16, 26, "Sinnoh" },
        new object[] { "SnowpointGym", 1, 3, 11, 28, "Sinnoh" },
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
        new object[] { "CanalaveGym", "byron", 16, 3, Direction.Down },
        new object[] { "CanalaveGym", "gym_guide", 15, 25, Direction.Down },
        new object[] { "CanalaveGym", "worker_gerardo", 8, 3, Direction.Down },
        new object[] { "CanalaveGym", "worker_jackson", 14, 3, Direction.Down },
        new object[] { "CanalaveGym", "worker_gary", 22, 16, Direction.Down },
        new object[] { "CanalaveGym", "black_belt_david", 24, 3, Direction.Down },
        new object[] { "CanalaveGym", "black_belt_ricky", 9, 17, Direction.Left },
        new object[] { "CanalaveGym", "ace_trainer_cesar", 27, 25, Direction.Down },
        new object[] { "CanalaveGym", "ace_trainer_breanna", 27, 5, Direction.Right },
        new object[] { "SnowpointGym", "candice", 11, 3, Direction.Down },
        new object[] { "SnowpointGym", "gym_guide", 12, 27, Direction.Down },
        new object[] { "SnowpointGym", "ace_trainer_anton", 17, 3, Direction.Down },
        new object[] { "SnowpointGym", "ace_trainer_savannah", 21, 9, Direction.Left },
        new object[] { "SnowpointGym", "ace_trainer_alicia", 3, 6, Direction.Right },
        new object[] { "SnowpointGym", "ace_trainer_isaiah", 18, 15, Direction.Down },
        new object[] { "SnowpointGym", "ace_trainer_brenna", 5, 16, Direction.Up },
        new object[] { "SnowpointGym", "ace_trainer_sergio", 2, 13, Direction.Down },
        new object[] { "SnowpointGym", "snowball_1", 12, 18, Direction.Up },
        new object[] { "SnowpointGym", "snowball_6", 11, 11, Direction.Up },
        new object[] { "SnowpointGym", "snowball_19", 20, 23, Direction.Up },
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

    // ------------------------------------------------------------------ the Canalave Gym

    /// <summary>
    /// Every place someone on foot can come to in the Canalave Gym from a tile, riding the platforms as they go: the
    /// tile, the floor's height and which end each platform stands at (a bit each), by the field's own rules with the
    /// lifts'. Each ride ends at the platform's other end with its bit turned over.
    /// </summary>
    private static Dictionary<(int X, int Y, int Height, int Ends), (int X, int Y, int Height, int Ends)?> RideThrough(Map map, int x, int y)
    {
        var lifts = (CanalaveLifts)map.Puzzle!;
        lifts.Reset();
        int ends = 0;
        for (int i = 0; i < CanalaveLifts.Platforms.Length; i++)
            if (lifts.AtB(i)) ends |= 1 << i;
        var start = (x, y, 0, ends);
        var from = new Dictionary<(int X, int Y, int Height, int Ends), (int X, int Y, int Height, int Ends)?> { [start] = null };
        var queue = new Queue<(int X, int Y, int Height, int Ends)>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var here = queue.Dequeue();
            for (int i = 0; i < CanalaveLifts.Platforms.Length; i++) lifts.Set(i, (here.Ends & (1 << i)) != 0);
            foreach (var dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var step = FieldMovement.Step(map, here.X, here.Y, dir, new Walker(TravelMode.OnFoot, here.Height));
                if (!step.Moves) continue;
                var next = (step.X, step.Y, (int)MathF.Round(step.Height), here.Ends);
                if (lifts.PlatformAt(step.X, step.Y, step.Height) is { } platform)
                {
                    lifts.Set(platform, !lifts.AtB(platform));
                    var end = lifts.Where(platform);
                    next = (end.X, end.Y, end.H, here.Ends ^ (1 << platform));
                    lifts.Set(platform, !lifts.AtB(platform));
                }
                if (from.TryAdd(next, here)) queue.Enqueue(next);
            }
        }
        lifts.Reset();
        return from;
    }

    [Fact]
    public void TheCanalaveGymsLiftsLeadFromTheDoorToByronPastEveryTrainer()
    {
        var map = Room("CanalaveGym");
        Assert.IsType<CanalaveLifts>(map.Puzzle);
        var ways = RideThrough(map, 16, 26);
        // Everyone can be walked up to on their own floor
        foreach (var who in map.Everyone.Where(n => n.IsTrainer || n.Key is "byron" or "gym_guide"))
        {
            int level = (int)(who.Level ?? 0f);
            Assert.True(ways.Keys.Any(k => k.Height == level && System.Math.Abs(k.X - who.GridX) + System.Math.Abs(k.Y - who.GridY) == 1),
                $"{who.Key} can't be walked up to");
        }
        // Byron's floor is the top, and the way there takes more than one ride
        var byron = map.Everyone.Single(n => n.Key == "byron");
        Assert.Equal(30f, byron.Level);
        var beside = ways.Keys.First(k => k.Height == 30 && (k.X, k.Y) == (byron.GridX, byron.GridY + 1));
        int rides = 0;
        for (var at = ((int X, int Y, int Height, int Ends)?)beside; at is { } a; at = ways[a])
            if (ways[a] is { } before && before.Ends != a.Ends) rides++;
        Assert.True(rides >= 2, $"Byron is reached in {rides} ride(s)");
        // Nobody walks off a floor's edge onto the one below: a step is refused where the floor's map closes the tile
        Assert.DoesNotContain(ways.Keys, k => k.Height > 0 && CanalaveLifts.Closed(k.Height / CanalaveLifts.FloorSpacing, k.X, k.Y));
    }

    [Fact]
    public void ACanalavePlatformCarriesTheWalkerToItsOtherEndAtTheOriginalsSpeed()
    {
        var lifts = new CanalaveLifts();
        // The red shaft from the ground to the top: 30 tiles at a tile every two frames at thirty a second
        Assert.Equal(0, lifts.PlatformAt(16, 9, 0f));
        Assert.Null(lifts.PlatformAt(16, 9, 30f));
        var ride = lifts.Board(0);
        Assert.Equal(((16, 0, 9), (16, 30, 9)), (ride.From, ride.To));
        Assert.Equal(2f, ride.Duration, 3);
        lifts.Update(1f);
        Assert.Equal(15f, lifts.Moving!.Now.H, 3);
        // On its way it carries whoever is at its height over its own tile, and nobody at a floor's height
        Assert.Equal(15f, lifts.FloorAt(16, 9, 15f));
        Assert.Null(lifts.FloorAt(16, 9, 0f));
        lifts.Update(1f);
        Assert.Null(lifts.Moving);
        Assert.True(lifts.AtB(0));
        Assert.Equal(0, lifts.PlatformAt(16, 9, 30f));
        // Coming in lays every platform out at its first end again
        lifts.Arrive(Room("CanalaveGym"), new StoryState(), new System.Random(0));
        Assert.False(lifts.AtB(0));
        Assert.True(lifts.AtB(6));
    }

    [Fact]
    public void ACanalaveFloorHasAHoleWhereItsPlatformHasGone()
    {
        var lifts = new CanalaveLifts();
        // The lift at (5, 26) between the first floor and the second starts at the first: the second has a hole there
        Assert.Equal(10f, lifts.FloorAt(5, 26, 10f));
        Assert.True(lifts.EmptySlot(2, 5, 26));
        Assert.Null(lifts.FloorAt(5, 26, 20f));
        Assert.True(lifts.Refuses(null!, 5, 26, 20f, false));
        lifts.Set(9, true);
        Assert.False(lifts.EmptySlot(2, 5, 26));
        Assert.True(lifts.EmptySlot(1, 5, 26));
        Assert.Equal(20f, lifts.FloorAt(5, 26, 20f));
        // The ground keeps its own floor everywhere its map is open, whatever stands above
        Assert.False(lifts.EmptySlot(0, 16, 9));
        Assert.Null(lifts.FloorAt(16, 26, 0f));
        Assert.Equal(0, CanalaveLifts.FloorOf(4f));
        Assert.Equal(1, CanalaveLifts.FloorOf(6f));
        Assert.Equal(3, CanalaveLifts.FloorOf(40f));
    }

    [Fact]
    public void TheCanalaveGymShowsOnlyTheFloorsUpToTheViewersAndTrainersLookAlongTheirOwn()
    {
        // A floor shows once the viewer has risen a tile toward it (CanalaveGym_UpdateVisibleProps)
        Assert.True(CanalaveLifts.ShownFrom(0, 0f));
        Assert.False(CanalaveLifts.ShownFrom(1, 0f));
        Assert.True(CanalaveLifts.ShownFrom(1, 1f));
        Assert.False(CanalaveLifts.ShownFrom(2, 10f));
        Assert.True(CanalaveLifts.ShownFrom(2, 11f));
        Assert.True(CanalaveLifts.ShownFrom(3, 30f));
        var map = Room("CanalaveGym");
        Assert.True(map.Puzzle!.Hides(30f, 0f));
        Assert.False(map.Puzzle.Hides(10f, 20f));
        // A trainer of the first floor doesn't see the player on the ground under them
        var jackson = map.Everyone.Single(n => n.Key == "worker_jackson");
        Assert.Equal(10f, jackson.Level);
        Assert.False(TrainerApproach.CanSee(map, jackson, jackson.GridX, jackson.GridY + 1, 0f));
        Assert.True(TrainerApproach.CanSee(map, jackson, jackson.GridX, jackson.GridY + 1, 10f));
    }

    [Fact]
    public void TheCanalaveGymPlaysThroughToTheMineBadge()
    {
        var game = new OpeningTests.Game(0);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Infernape")!, 50));
        game.Arrive("CanalaveGym", 16, 26);
        Assert.Contains(game.Talk("gym_guide").Transcript, l => l.Text.Contains("lifts"));
        Assert.Contains(game.Talk("worker_gary").Log, l => l.StartsWith("battle worker_gary Won"));

        var gym = game.Talk("byron");
        Assert.Contains("battle leader_byron Won", gym.Log);
        Assert.True(game.Story.HasBadge(Badge.Mine));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM91")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_BYRON_TM91"));
        Assert.Equal(2, game.Story.Var("VAR_CANALAVE_CITY_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_ROWAN"));
        Assert.False(game.Story.Has("FLAG_HIDE_CANALAVE_LIBRARY_ROWAN"));
        // His trainers count as beaten, those not yet fought too
        Assert.DoesNotContain(game.Talk("black_belt_ricky").Log, l => l.StartsWith("battle"));
        Assert.DoesNotContain(game.Talk("byron").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM91")!));
    }

    // ------------------------------------------------------------------ the Snowpoint Gym

    /// <summary>
    /// The Snowpoint Gym is the original's bowl of ice (its land data's tiles, behaviours and height plates): the floor by
    /// the door and the outer ring at three, each ring in a tile lower down to the middle at nought, joined by slopes of
    /// ice, and Candice's dais at four. Snow stops a slide; the steps down the middle of each slope (the original's
    /// behaviours 0x4A and 0x49) are snow that can't be left sideways; nineteen snowballs stand on the ice.
    /// </summary>
    [Fact]
    public void TheSnowpointGymIsTheOriginalsBowlOfIce()
    {
        var map = Room("SnowpointGym");
        Assert.True(map.HasRelief);
        Assert.Null(map.Puzzle);
        foreach (var (x, y, height) in new[] { (11, 28, 3f), (1, 24, 3f), (11, 21, 2f), (5, 15, 2f), (11, 18, 1f), (8, 15, 1f), (11, 15, 0f), (11, 4, 4f) })
            Assert.Equal(height, map.HeightAt(x, y), 2);
        // Going in from the outer ring is going down: the slope underfoot falls the way one goes, and rises the way back
        Assert.Equal(IceSlide.Incline.Down, IceSlide.InclineOf(map, 11, 23, Direction.Up));
        Assert.Equal(IceSlide.Incline.Up, IceSlide.InclineOf(map, 11, 23, Direction.Down));
        Assert.Equal(IceSlide.Incline.Down, IceSlide.InclineOf(map, 3, 15, Direction.Right));
        Assert.Equal(IceSlide.Incline.Down, IceSlide.InclineOf(map, 13, 15, Direction.Left));
        Assert.Equal(IceSlide.Incline.Level, IceSlide.InclineOf(map, 11, 24, Direction.Up));
        Assert.Equal(IceSlide.Incline.Level, IceSlide.InclineOf(map, 3, 15, Direction.Up));
        // Candice's dais is climbed from the slopes round it
        Assert.Equal(IceSlide.Incline.Up, IceSlide.InclineOf(map, 8, 3, Direction.Right));
        Assert.Equal(IceSlide.Incline.Up, IceSlide.InclineOf(map, 12, 5, Direction.Up));

        Assert.Equal(TileBehavior.Ice, map.BehaviourAt(11, 24));
        Assert.Equal(TileType.Snow, map.GetGroundTile(17, 6));
        Assert.Equal(TileBehavior.None, map.BehaviourAt(17, 6));
        Assert.Equal(TileBehavior.BlockEastAndWest, map.BehaviourAt(11, 20));
        Assert.Equal(TileBehavior.BlockNorthAndSouth, map.BehaviourAt(16, 11));
        Assert.Equal(TileBehavior.None, map.BehaviourAt(11, 4));
        Assert.True(map.IsSolid(3, 7) && map.IsSolid(9, 13) && map.IsSolid(9, 27));

        var balls = map.Everyone.Where(n => n.IsSnowball).ToList();
        Assert.Equal(19, balls.Count);
        Assert.All(balls, b => Assert.True(b.IsThing && b.GymThing == PropType.Snowball && !map.IsSolid(b.GridX, b.GridY)));
        Assert.Equal(Obstacle.Person, FieldMovement.Step(map, 11, 9, Direction.Up, new Walker(TravelMode.OnFoot, 2f)).Obstacle);
    }

    /// <summary>
    /// The ice's speeds as the original's code keeps them (<c>PlayerAvatar_TileMove_Ice</c> and the two functions it
    /// calls): a tile sloping down the way one goes adds a speed, up to three; one sloping up takes one away, and with
    /// none left the slider slips a tile back; flat ice keeps the speed. Stopped, the speed is gone, and stopped on the
    /// way up, the slider slips back too. Each speed has the original's frames to a tile.
    /// </summary>
    [Fact]
    public void IceGathersSpeedDownItsSlopesAndLosesItUpThem()
    {
        var map = Room("SnowpointGym");
        Walker At(int x, int y) => new(TravelMode.OnFoot, map.HeightAt(x, y));

        // Down the slope from the outer ring at (3, 15): one speed more, and no more than three
        var down = IceSlide.From(map, 3, 15, Direction.Right, 0, At(3, 15));
        Assert.Equal((IceSlide.Outcome.Slide, 4, 15, 1), (down.Outcome, down.Step.X, down.Step.Y, down.Speed));
        Assert.Equal(3, IceSlide.From(map, 3, 15, Direction.Right, 3, At(3, 15)).Speed);
        // Flat ice keeps it
        Assert.Equal(2, IceSlide.From(map, 4, 15, Direction.Right, 2, At(4, 15)).Speed);
        // Up the same slope: a speed less, and with none it slips back a tile, back the way it came
        var climb = IceSlide.From(map, 3, 15, Direction.Left, 2, At(3, 15));
        Assert.Equal((IceSlide.Outcome.Slide, 2, 15, 1), (climb.Outcome, climb.Step.X, climb.Step.Y, climb.Speed));
        var slip = IceSlide.From(map, 3, 15, Direction.Left, 0, At(3, 15));
        Assert.Equal((IceSlide.Outcome.SlipBack, 4, 15, 0), (slip.Outcome, slip.Step.X, slip.Step.Y, slip.Speed));
        // In the way of something on flat ice, it stops, speed gone; on the way up a slope, it slips back instead,
        // whatever its speed (the snowball at (12, 21) above the slope at (12, 20))
        var stop = IceSlide.From(map, 11, 9, Direction.Up, 2, At(11, 9));
        Assert.Equal((IceSlide.Outcome.Stop, 0), (stop.Outcome, stop.Speed));
        var blocked = IceSlide.From(map, 12, 20, Direction.Down, 3, At(12, 20));
        Assert.Equal((IceSlide.Outcome.SlipBack, 12, 19, 0), (blocked.Outcome, blocked.Step.X, blocked.Step.Y, blocked.Speed));

        // The original's frames a tile: four at nought, three at one, two at two and three, sixteen slipping back
        Assert.Equal(new[] { 4, 3, 2, 2 }, Enumerable.Range(0, 4).Select(IceSlide.FramesATile).ToArray());
        Assert.Equal(FieldMovement.TilesPerSecond(Pace.Fast), IceSlide.TilesPerSecond(0));
        Assert.Equal(IceSlide.TilesPerSecond(0) * 2f, IceSlide.TilesPerSecond(3), 3);
        Assert.Equal(IceSlide.TilesPerSecond(0) / 4f, IceSlide.SlipTilesPerSecond, 3);
        // A snowball breaks before a slide of a speed of one or more
        Assert.False(IceSlide.Breaks(0));
        Assert.True(IceSlide.Breaks(1));
    }

    /// <summary>Holds a direction until the player has stopped on a tile again, as the field's walking tests do.</summary>
    private static void Slide(Player player, Map map, Direction dir)
    {
        player.Facing = dir;
        bool started = false;
        for (int frame = 0; frame < 1200; frame++)
        {
            player.Advance(1f / 60f, map, started ? null : dir, false, _ => { }, _ => { });
            started |= player.IsMoving;
            if (started && !player.IsMoving && !player.IsSliding) return;
            if (!started && frame > 2) return;
        }
    }

    /// <summary>
    /// The player plays the slides out: from the snow at (5, 11) a step onto the slope east of it slides down into the
    /// next ring with a speed in hand, smashes the snowball at (11, 11) (<c>ov5_021E06A8</c>) and slides on through
    /// its place to the step at (16, 11). Slid back west at no speed, the player goes down past the snowball's place and
    /// slips back off the slope at the far side, still facing it. Coming in again brings the snowball back, and a slide
    /// with no speed is stopped by it like by anything else.
    /// </summary>
    [Fact]
    public void ASlideWithSpeedSmashesASnowballThatIsBackWhenThePlayerComesIn()
    {
        var map = Room("SnowpointGym");
        var ball = map.Everyone.Single(n => n.Key == "snowball_6");
        var player = new Player(5, 11);
        player.SetHeight(map.HeightAt(5, 11));

        Slide(player, map, Direction.Right);
        Assert.Equal((16, 11), (player.GridX, player.GridY));
        Assert.DoesNotContain(ball, map.NPCs);
        Assert.Same(ball, player.TakeBroken());
        Assert.Null(player.TakeBroken());
        Assert.Equal(0, player.IceSpeed);

        // Back west with no speed: down through the ring, and a slip back off the slope that climbs out of it
        Slide(player, map, Direction.Left);
        Assert.Equal((7, 11), (player.GridX, player.GridY));
        Assert.Equal(Direction.Left, player.Facing);
        Assert.False(player.IsSliding);

        // Coming in again lays the map's things out afresh, as the engine does (ArriveOnMap), and the snowball is back
        map.ForgetForced();
        map.ApplyPresence(_ => false);
        Assert.Contains(ball, map.NPCs);
        Slide(player, map, Direction.Right);
        Assert.Equal((10, 11), (player.GridX, player.GridY));
        Assert.Contains(ball, map.NPCs);
        Assert.Null(player.TakeBroken());
    }

    /// <summary>
    /// Every place the player can come to stand in the Snowpoint Gym from a tile, and which snowballs the way there broke
    /// (a bit each): a step each way from where they stand, and then the ice's own slide as the player plays it, a
    /// snowball in front of a slide with speed breaking (unless <paramref name="breaking"/> is false). The trainers
    /// stand where they are, as beaten.
    /// </summary>
    private static Dictionary<(int X, int Y, int Broken), (int X, int Y, int Broken)?> SlideThrough(Map map, int x, int y, bool breaking = true)
    {
        var balls = map.Everyone.Where(n => n.IsSnowball).ToList();
        void Lay(int broken)
        {
            for (int i = 0; i < balls.Count; i++)
            {
                bool gone = (broken & (1 << i)) != 0, there = map.NPCs.Contains(balls[i]);
                if (gone && there) IceSlide.Break(map, balls[i]);
                else if (!gone && !there)
                {
                    map.Absent.Remove(balls[i]);
                    map.NPCs.Add(balls[i]);
                }
            }
        }
        (int X, int Y, int Broken)? Move(int sx, int sy, int broken, Direction dir)
        {
            Lay(broken);
            var step = FieldMovement.Step(map, sx, sy, dir, new Walker(TravelMode.OnFoot, map.HeightAt(sx, sy)));
            if (!step.Moves || map.GetWarpAt(step.X, step.Y) != null) return null;
            int cx = step.X, cy = step.Y, speed = 0;
            float height = step.Height;
            for (int guard = 0; guard < 100 && map.BehaviourAt(cx, cy) == TileBehavior.Ice; guard++)
            {
                if (breaking && IceSlide.Breaks(speed) && IceSlide.SnowballAhead(map, cx, cy, dir) is { } ball)
                {
                    broken |= 1 << balls.IndexOf(ball);
                    IceSlide.Break(map, ball);
                }
                var next = IceSlide.From(map, cx, cy, dir, speed, new Walker(TravelMode.OnFoot, height));
                speed = next.Speed;
                if (next.Outcome == IceSlide.Outcome.Stop) break;
                (cx, cy, height) = (next.Step.X, next.Step.Y, next.Step.Height);
                if (next.Outcome == IceSlide.Outcome.SlipBack) break;
            }
            return (cx, cy, broken);
        }

        var start = (x, y, 0);
        var from = new Dictionary<(int X, int Y, int Broken), (int X, int Y, int Broken)?> { [start] = null };
        var queue = new Queue<(int X, int Y, int Broken)>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var here = queue.Dequeue();
            foreach (var dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
                if (Move(here.X, here.Y, here.Broken, dir) is { } next && from.TryAdd(next, here)) queue.Enqueue(next);
        }
        Lay(0);
        return from;
    }

    [Fact]
    public void TheSnowpointGymsIceLeadsFromTheDoorToCandiceByBreakingSnowballs()
    {
        var map = Room("SnowpointGym");
        var ways = SlideThrough(map, 11, 28);
        bool Beside(NPC who) => ways.Keys.Any(k => System.Math.Abs(k.X - who.GridX) + System.Math.Abs(k.Y - who.GridY) == 1);
        foreach (var who in map.Everyone.Where(n => n.IsTrainer || n.Key is "candice" or "gym_guide"))
            Assert.True(Beside(who), $"{who.Key} can't be walked up to");
        Assert.DoesNotContain(ways.Keys, k => map.IsSolid(k.X, k.Y));
        // Candice is reached only once snowballs have broken on the way
        var candice = map.Everyone.Single(n => n.Key == "candice");
        Assert.All(ways.Keys.Where(k => (k.X, k.Y) == (candice.GridX, candice.GridY + 1)), k => Assert.NotEqual(0, k.Broken));

        // Without the snowballs ever breaking, nobody gets to her
        var stuck = SlideThrough(map, 11, 28, breaking: false);
        Assert.DoesNotContain(stuck.Keys, k => System.Math.Abs(k.X - candice.GridX) + System.Math.Abs(k.Y - candice.GridY) == 1);
        Assert.Equal(19, map.NPCs.Count(n => n.IsSnowball));
    }

    /// <summary>
    /// The style guide's ice floor: ice lies under everything (no lawn shows where rounded corners meet), the slopes are
    /// streaked, and a snowball's card is a round ball of snow 30 by 30 that stays below white.
    /// </summary>
    [Fact]
    public void TheSnowpointGymsFloorIsIceThroughAndItsSnowballsStayBelowWhite()
    {
        var map = Room("SnowpointGym");
        var floor = PokemonPlatinumEngine.Graphics.GroundBaker.BakeInterior(map);
        var lawn = new[] { new Raylib_cs.Color(104, 190, 98, 255), new Raylib_cs.Color(120, 200, 102, 255) };
        for (int y = 0; y < floor.Height; y++)
            for (int x = 0; x < floor.Width; x++)
                Assert.DoesNotContain(floor.Get(x, y), lawn);
        // A slope of ice has its streaks; flat ice has none
        const int T = PokemonPlatinumEngine.Graphics.GroundBaker.ArtTile;
        int Streaks(int tx, int ty) => Enumerable.Range(0, T * T).Count(i => floor.Get(tx * T + i % T, ty * T + i / T).Equals(new Raylib_cs.Color(206, 232, 250, 255)));
        Assert.True(Streaks(3, 15) >= 20);
        Assert.Equal(0, Streaks(4, 15));

        var ball = PokemonPlatinumEngine.Graphics.ThingCards.Paint(PropType.Snowball);
        Assert.Equal((30, 30), (ball.Width, ball.Height));
        Assert.True(ball.IsOpaque(15, 15) && !ball.IsOpaque(1, 1) && !ball.IsOpaque(28, 1));
        for (int y = 0; y < ball.Height; y++)
            for (int x = 0; x < ball.Width; x++)
                if (ball.IsOpaque(x, y)) Assert.True(ball.Get(x, y).R < 236 && ball.Get(x, y).G < 248, $"({x}, {y}) is too near white");
    }

    [Fact]
    public void TheSnowpointGymPlaysThroughToTheIcicleBadge()
    {
        var game = new OpeningTests.Game(0);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Infernape")!, 50));
        game.Arrive("SnowpointGym", 11, 28);
        Assert.Contains(game.Talk("gym_guide").Transcript, l => l.Text.Contains("snowball"));
        Assert.Contains(game.Talk("ace_trainer_isaiah").Log, l => l.StartsWith("battle ace_trainer_isaiah Won"));

        var gym = game.Talk("candice");
        Assert.Contains("battle leader_candice Won", gym.Log);
        Assert.True(game.Story.HasBadge(Badge.Icicle));
        Assert.Equal(Badge.Icicle, FieldMoveRules.BadgeFor(FieldMove.RockClimb));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM72")!));
        Assert.True(game.Story.Has("FLAG_RECEIVED_CANDICE_TM72"));
        Assert.True(game.Story.Has("FLAG_HIDE_VEILSTONE_GALACTIC_GRUNTS"));
        // Her trainers count as beaten, those not yet fought too
        foreach (var id in new[] { "ace_trainer_sergio", "ace_trainer_isaiah", "ace_trainer_anton", "ace_trainer_savannah", "ace_trainer_alicia", "ace_trainer_brenna" })
            Assert.True(game.Story.HasDefeated(id), id);
        Assert.DoesNotContain(game.Talk("ace_trainer_brenna").Log, l => l.StartsWith("battle"));
        Assert.DoesNotContain(game.Talk("candice").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM72")!));
    }

    // ------------------------------------------------------------------ the doors from the cities

    [Theory]
    [InlineData("OreburghGym")]
    [InlineData("PastoriaGym")]
    [InlineData("EternaGym")]
    [InlineData("VeilstoneGym")]
    [InlineData("HearthomeGym")]
    [InlineData("CanalaveGym")]
    [InlineData("SnowpointGym")]
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
