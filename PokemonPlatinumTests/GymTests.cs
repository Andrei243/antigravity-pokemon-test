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
        new object[] { "EternaGym", 1, 3, 11, 27, "Sinnoh" },
        new object[] { "VeilstoneGym", 1, 3, 12, 30, "Sinnoh" },
        new object[] { "HearthomeGym", 1, 3, 4, 8, "Sinnoh" },
        new object[] { "HearthomeGymRoom1", 1, 3, 8, 10, "HearthomeGym" },
        new object[] { "HearthomeGymRoom2", 1, 3, 14, 22, "HearthomeGym" },
        new object[] { "HearthomeGymLeaderRoom", 1, 3, 4, 13, "HearthomeGym" },
        // Plan 01 · M9 1b
        new object[] { "PastoriaGym", 1, 3, 13, 42, "Sinnoh" },
        new object[] { "OreburghGym", 1, 3, 5, 24, "Sinnoh" }
    };

    // The original's people (res/field/events/events_<map>.json): local id, tile, facing
    public static IEnumerable<object[]> People => new[]
    {
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
        new object[] { "HearthomeGymLeaderRoom", "bollard_2", 8, 9, Direction.Up },
        // Plan 01 · M9 1b (events_pastoria_city_gym.json, events_oreburgh_city_gym.json)
        new object[] { "PastoriaGym", "crasher_wake", 13, 4, Direction.Down },
        new object[] { "PastoriaGym", "gym_guide", 15, 40, Direction.Down },
        new object[] { "PastoriaGym", "tuber_jacky", 11, 33, Direction.Down },
        new object[] { "PastoriaGym", "sailor_damian", 7, 22, Direction.Down },
        new object[] { "PastoriaGym", "tuber_caitlyn", 21, 33, Direction.Left },
        new object[] { "PastoriaGym", "sailor_samson", 5, 8, Direction.Left },
        new object[] { "PastoriaGym", "fisherman_erick", 19, 18, Direction.Left },
        new object[] { "PastoriaGym", "fisherman_walter", 9, 11, Direction.Down },
        new object[] { "OreburghGym", "roark", 5, 3, Direction.Down },
        new object[] { "OreburghGym", "gym_guide", 6, 23, Direction.Down },
        new object[] { "OreburghGym", "youngster_jonathon", 4, 18, Direction.Right },
        new object[] { "OreburghGym", "youngster_darius", 7, 11, Direction.Left }
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
    [InlineData("EternaGym")]
    [InlineData("VeilstoneGym")]
    [InlineData("HearthomeGym")]
    [InlineData("PastoriaGym")]
    [InlineData("OreburghGym")]
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

    // ------------------------------------------------------------------ the Pastoria Gym (plan 01 · M9 1b)

    private static PastoriaWater WaterAt(Map map, PastoriaWater.Button button)
    {
        var water = (PastoriaWater)map.Puzzle!;
        water.Press(button);
        water.Settle();
        return water;
    }

    private static int Count(Map map, TileBehavior behaviour)
    {
        int n = 0;
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map.BehaviourAt(x, y) == behaviour) n++;
        return n;
    }

    [Fact]
    public void ThePoolsWaterItsGatesAndItsFloatsAreTheOriginals()
    {
        // PastoriaGym_DynamicMapFeaturesInit: one plate from (1, 2), 25 wide and 38 deep; PASTORIA_WATER_HEIGHT_*: 0, 2 and 4
        Assert.Equal((1, 2, 25, 38), (PastoriaWater.PlateX, PastoriaWater.PlateZ, PastoriaWater.PlateWidth, PastoriaWater.PlateDepth));
        Assert.Equal((0f, 2f, 4f), (PastoriaWater.LevelOf(PastoriaWater.Button.Orange), PastoriaWater.LevelOf(PastoriaWater.Button.Green),
            PastoriaWater.LevelOf(PastoriaWater.Button.Blue)));
        var map = Room("PastoriaGym");
        var water = Assert.IsType<PastoriaWater>(map.Puzzle);
        // PersistedMapFeatures_InitForPastoriaGym: the green button pressed, the water at its middle
        Assert.Equal((PastoriaWater.Button.Green, 2f, 2f), (water.Pressed, water.Height, water.Level));
        // The original's behaviours: ten tiles of 0x56 and of 0x58, and the four of 0x57 inside the room (its other two
        // are in the back wall); the decks at the door's level, four tiles over the pool's bed
        Assert.Equal((10, 4, 10), (Count(map, TileBehavior.PastoriaGymHigh), Count(map, TileBehavior.PastoriaGymMiddle), Count(map, TileBehavior.PastoriaGymLow)));
        Assert.True(Count(map, TileBehavior.MovingFloor) > 400);
        Assert.Equal(4f, map.GroundLevel);
        Assert.Equal((4f, 4f, 0f, 2f), (map.HeightAt(13, 42), map.HeightAt(13, 9), map.HeightAt(13, 7), map.HeightAt(13, 13)));

        // PastoriaGym_DynamicMapFeaturesCheckCollision: each let on only from its own height, whatever the water does
        Assert.Equal(false, water.Collides(map, 13, 8, 4f));
        Assert.Equal(true, water.Collides(map, 13, 8, 2f));
        Assert.Equal(false, water.Collides(map, 6, 8, 0f));
        Assert.Equal(true, water.Collides(map, 6, 8, 4f));
        Assert.Equal(false, water.Collides(map, 13, 33, 2f));
        Assert.Equal(true, water.Collides(map, 13, 33, 4f));
        Assert.Null(water.Collides(map, 13, 9, 4f));

        // The floats: twenty, on the tiles where the original lets the player stand on the water
        var floats = new List<(int, int)>();
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (PastoriaWater.IsFloat(map, x, y)) floats.Add((x, y));
        Assert.Equal(20, floats.Count);
        Assert.Contains((13, 7), floats);
        Assert.Contains((13, 8), floats);
        Assert.Contains((1, 36), floats);
        Assert.DoesNotContain((6, 8), floats);

        // With the water at the decks a float is stood on at its height, and the pool round it can't be
        WaterAt(map, PastoriaWater.Button.Blue);
        Assert.Equal((4f, false, true), map.StandAt(13, 7, 4f));
        var step = FieldMovement.Step(map, 13, 9, Direction.Up, new Walker(Height: 4f));
        Assert.Equal((StepKind.Walk, 13, 8, 4f), (step.Kind, step.X, step.Y, step.Height));
        Assert.Equal(StepKind.Walk, FieldMovement.Step(map, 13, 8, Direction.Up, new Walker(Height: 4f)).Kind);
        Assert.Equal(Obstacle.Water, FieldMovement.Step(map, 13, 7, Direction.Left, new Walker(Height: 4f)).Obstacle);
        // Half way down, the floats ride two tiles under the decks: a drop from them
        WaterAt(map, PastoriaWater.Button.Green);
        Assert.Equal(Obstacle.Cliff, FieldMovement.Step(map, 13, 9, Direction.Up, new Walker(Height: 4f)).Obstacle);
        Assert.Equal((2f, false, true), map.StandAt(13, 34, 2f));
        // Let out: a float rests on the bed and the bed is walked where the original lets it be
        WaterAt(map, PastoriaWater.Button.Orange);
        Assert.Equal((0f, false, false), map.StandAt(13, 7, 4f));
        Assert.Equal(StepKind.Walk, FieldMovement.Step(map, 7, 9, Direction.Up, new Walker(Height: 0f)).Kind);
        Assert.Equal(StepKind.Walk, FieldMovement.Step(map, 7, 8, Direction.Up, new Walker(Height: 0f)).Kind);
    }

    [Fact]
    public void TheWaterMovesASixteenthOfATileAFrameAndIsWalkedOnlyOnceItIsThere()
    {
        var water = new PastoriaWater();
        void Frames(PastoriaWater.Rise rise, int n)
        {
            for (int i = 0; i < n; i++) rise.Update(PastoriaWater.FrameSeconds + 1e-5f);
        }

        // Blue: the buttons, then two tiles up in 32 frames; the field walks the old height until it gets there
        var rise = water.Press(PastoriaWater.Button.Blue);
        Frames(rise, PastoriaWater.ButtonFrames);
        Assert.Equal((2f, false), (water.Level, rise.Flowing));
        Frames(rise, 16);
        Assert.Equal((3f, 2f, true), (water.Level, water.Height, rise.Flowing));
        Frames(rise, 16);
        Assert.Equal(4f, water.Level);
        Assert.Equal(2f, water.Height);
        Frames(rise, 1);
        Assert.True(rise.IsDone);
        Assert.Equal((4f, 4f), (water.Level, water.Height));
        Assert.Null(water.Rising);

        // Green from the top lets it down to the middle; green at the middle moves nothing
        rise = water.Press(PastoriaWater.Button.Green);
        Frames(rise, PastoriaWater.ButtonFrames + 33);
        Assert.True(rise.IsDone);
        Assert.Equal(2f, water.Height);
        rise = water.Press(PastoriaWater.Button.Green);
        Frames(rise, PastoriaWater.ButtonFrames + 1);
        Assert.True(rise.IsDone);
        Assert.Equal(2f, water.Level);

        // Orange lets it all out; coming in again by the door, the green is pressed and the water at its middle
        rise = water.Press(PastoriaWater.Button.Orange);
        Frames(rise, PastoriaWater.ButtonFrames + 33);
        Assert.Equal((0f, PastoriaWater.Button.Orange), (water.Height, water.Pressed));
        water.Arrive(Room("PastoriaGym"), new StoryState(), new System.Random(0));
        Assert.Equal((2f, PastoriaWater.Button.Green), (water.Height, water.Pressed));
    }

    [Fact]
    public void TheButtonsAreTheOriginalsCoordinateEvents()
    {
        // events_pastoria_city_gym.json: scripts 2 (blue), 3 (green) and 4 (orange), each while its own variable is 0
        var map = Room("PastoriaGym");
        Assert.Equal(PastoriaWater.Buttons.Length, map.Triggers.Count);
        Assert.Equal((2, 4, 4), (PastoriaWater.Buttons.Count(b => b.Button == PastoriaWater.Button.Blue),
            PastoriaWater.Buttons.Count(b => b.Button == PastoriaWater.Button.Green), PastoriaWater.Buttons.Count(b => b.Button == PastoriaWater.Button.Orange)));
        foreach (var (x, y, button) in PastoriaWater.Buttons)
        {
            var trigger = Assert.Single(map.Triggers, t => t.X == x && t.Y == y);
            Assert.Equal((button + "Button", PastoriaWater.VarOf(button), 0), (trigger.Script, trigger.Variable, trigger.Value));
            Assert.NotNull(Scripts.Find(trigger.Script, "PastoriaGym"));
        }
    }

    /// <summary>
    /// The fewest buttons pressed on a walk from the Pastoria Gym's door to beside Crasher Wake, by the field's own
    /// rules: steps cost nothing, a press one (each button's trigger runs while its colour isn't the one pressed
    /// last). Null where there is no way; <paramref name="buttons"/> false keeps the water where it is.
    /// </summary>
    internal static int? FewestPresses(Map map, (int X, int Y) goal, bool buttons = true, HashSet<(int X, int Y)>? reached = null)
    {
        var water = (PastoriaWater)map.Puzzle!;
        var start = (X: 13, Y: 42, H: 4f, Last: PastoriaWater.Button.Green);
        var best = new Dictionary<(int, int, float, PastoriaWater.Button), int> { [start] = 0 };
        var queue = new LinkedList<((int X, int Y, float H, PastoriaWater.Button Last) State, int Cost)>();
        queue.AddFirst((start, 0));
        int? found = null;
        while (queue.Count > 0)
        {
            var (s, cost) = queue.First!.Value;
            queue.RemoveFirst();
            if (best[s] < cost) continue;
            reached?.Add((s.X, s.Y));
            if ((s.X, s.Y) == goal) { found ??= cost; continue; }
            WaterAt(map, s.Last);
            foreach (var dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var step = FieldMovement.Step(map, s.X, s.Y, dir, new Walker(Height: s.H));
                if (!step.Moves) continue;
                var last = s.Last;
                if (buttons && PastoriaWater.Buttons.FirstOrDefault(b => (b.X, b.Y) == (step.X, step.Y)) is { Y: > 0 } button && button.Button != last)
                    last = button.Button;
                var next = (step.X, step.Y, step.Height, last);
                int c = cost + (last != s.Last ? 1 : 0);
                if (best.TryGetValue(next, out int known) && known <= c) continue;
                best[next] = c;
                if (c == cost) queue.AddFirst((next, c)); else queue.AddLast((next, c));
            }
        }
        return found;
    }

    [Fact]
    public void CrasherWakeIsReachedOnlyByTheButtonsInTheRightOrder()
    {
        // The original's rules on the original's floor (its plates, behaviours and buttons, checked against a search of
        // the decompilation's own data as this room was rebuilt) take six presses at the least: orange, green, blue,
        // green, orange, blue
        var map = Room("PastoriaGym");
        Assert.Equal(6, FewestPresses(map, (13, 5)));
        // With the water left as it is at the door, the way to him is shut
        Assert.Null(FewestPresses(map, (13, 5), buttons: false));

        // And every trainer of the Gym can be walked up to on the way
        var reached = new HashSet<(int X, int Y)>();
        FewestPresses(map, (-1, -1), reached: reached);
        foreach (var id in new[] { "tuber_jacky", "sailor_damian", "tuber_caitlyn", "sailor_samson", "fisherman_erick", "fisherman_walter", "crasher_wake" })
            Assert.True(CanTalkTo(reached, map.Everyone.Single(n => n.Key == id)), id);
    }

    [Fact]
    public void TheButtonsMoveTheWaterThroughTheRoomsScripts()
    {
        var game = new OpeningTests.Game(0);
        game.Arrive("PastoriaGym", 13, 42);
        var water = (PastoriaWater)game.Map.Puzzle!;
        // InitPersistedMapFeaturesForPastoriaGym and the room's own variables: the green is pressed
        Assert.Equal((0, 1, 0), (game.Story.Var("VAR_MAP_LOCAL_0x01"), game.Story.Var("VAR_MAP_LOCAL_0x02"), game.Story.Var("VAR_MAP_LOCAL_0x03")));
        Assert.False(game.Fires("GreenButton"));
        Assert.True(game.Fires("BlueButton") && game.Fires("OrangeButton"));

        var host = game.Step("BlueButton");
        Assert.Contains("waterbutton blue", host.Log);
        Assert.Equal((PastoriaWater.Button.Blue, 4f), (water.Pressed, water.Height));
        Assert.False(game.Fires("BlueButton"));
        Assert.True(game.Fires("GreenButton"));
        game.Step("OrangeButton");
        Assert.Equal(0f, water.Height);
        Assert.Equal((0, 0, 1), (game.Story.Var("VAR_MAP_LOCAL_0x01"), game.Story.Var("VAR_MAP_LOCAL_0x02"), game.Story.Var("VAR_MAP_LOCAL_0x03")));
    }

    [Fact]
    public void ThePastoriaGymPlaysThroughToTheFenBadge()
    {
        var game = new OpeningTests.Game(0);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Luxray")!, 45));
        game.Arrive("PastoriaGym", 13, 42);

        Assert.Contains(game.Talk("gym_guide").Transcript, l => l.Text.Contains("button"));
        Assert.Contains(game.Talk("gym_guide").Transcript, l => l.Text.Contains("Water types"));
        foreach (var id in new[] { "tuber_jacky", "sailor_damian", "tuber_caitlyn", "sailor_samson", "fisherman_erick", "fisherman_walter" })
        {
            Assert.Contains(game.Talk(id).Log, l => l.StartsWith($"battle {id} Won"));
            Assert.DoesNotContain(game.Talk(id).Log, l => l.StartsWith("battle"));
        }

        // Crasher Wake: the battle, the Fen Badge, TM55, his trainers counted as beaten, and what his script sets
        var gym = game.Talk("crasher_wake");
        Assert.Contains("battle leader_wake Won", gym.Log);
        Assert.True(game.Story.HasBadge(Badge.Fen));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM55")!));
        Assert.Equal("Brine", ItemDatabase.Get("TM55")!.TeachesMove);
        Assert.True(game.Story.Has("FLAG_RECEIVED_WAKE_TM55"));
        Assert.Equal(3, game.Story.Var("VAR_PASTORIA_CITY_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_PASTORIA_CITY_GRUNT_M"));
        Assert.True(game.Story.Has("FLAG_BLOCK_PASTORIA_CITY_CROAGUNK_EVENT"));
        Assert.DoesNotContain(game.Talk("crasher_wake").Log, l => l.StartsWith("battle"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("TM55")!));
        Assert.Contains(game.Talk("gym_guide").Transcript, l => l.Text.Contains("beat Wake"));
    }

    [Fact]
    public void AWakeBeatenFirstCountsHisTrainersAsBeaten()
    {
        var game = new OpeningTests.Game(0);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Luxray")!, 45));
        game.Arrive("PastoriaGym", 13, 42);
        game.Talk("crasher_wake");
        foreach (var id in new[] { "tuber_jacky", "sailor_damian", "tuber_caitlyn", "sailor_samson", "fisherman_erick", "fisherman_walter" })
        {
            Assert.True(game.Story.HasDefeated(id), id);
            Assert.DoesNotContain(game.Talk(id).Log, l => l.StartsWith("battle"));
        }
    }

    // ------------------------------------------------------------------ the Oreburgh Gym on the original's plan (M9 1b)

    [Fact]
    public void TheOreburghGymIsTheOriginalsQuarryInTiers()
    {
        var map = Room("OreburghGym");
        Assert.Equal(InteriorStyle.Gym, map.Interior);
        // The door's level, the middle tier two tiles up, the ledges three, Roark's four; stairs between them
        Assert.Equal((0f, 0f, 2f, 3f, 4f), (map.HeightAt(5, 24), map.HeightAt(5, 16), map.HeightAt(5, 12), map.HeightAt(1, 7), map.HeightAt(5, 3)));
        Assert.Equal((0f, -1f), map.SlopeAt(5, 6));
        Assert.Equal(TileType.Stairs, map.GetGroundTile(5, 13));
        // A bridge over the lowest floor, walked over and under
        Assert.Equal(2f, map.DeckAt(5, 17));
        Assert.Equal(0f, map.HeightAt(5, 17));
        // The ledges' rails, where the dais and the pocket below it are only a tile from them
        Assert.Equal((TileBehavior.BlockWest, TileBehavior.BlockEast), (map.BehaviourAt(3, 4), map.BehaviourAt(7, 4)));
        Assert.Equal((TileBehavior.BlockEast, TileBehavior.BlockWest), (map.BehaviourAt(2, 9), map.BehaviourAt(8, 9)));
        Assert.Equal(StepKind.Blocked, FieldMovement.Step(map, 2, 4, Direction.Right, new Walker(Height: 3f)).Kind);
        // The statues by the door, read from the south; the trainers' sight is the original's
        Assert.Equal("GymStatue", map.SignScripts[(3, 23)]);
        Assert.Equal("GymStatue", map.SignScripts[(7, 23)]);
        Assert.Equal(3, map.Everyone.Single(n => n.Key == "youngster_jonathon").TrainerData!.SightRange);
        Assert.Equal(4, map.Everyone.Single(n => n.Key == "youngster_darius").TrainerData!.SightRange);

        // From the door everyone can be walked up to, over the bridge and under it
        var reach = Reach(map, 5, 24);
        foreach (var id in new[] { "roark", "gym_guide", "youngster_jonathon", "youngster_darius" })
            Assert.True(CanTalkTo(reach, map.Everyone.Single(n => n.Key == id)), id);
        var over = FieldMovement.Step(map, 3, 17, Direction.Right, new Walker(Height: 2f));
        Assert.Equal((4, 17, 2f), (over.X, over.Y, over.Height));
        var under = FieldMovement.Step(map, 5, 16, Direction.Down, new Walker(Height: 0f));
        Assert.Equal((5, 17, 0f), (under.X, under.Y, under.Height));
    }

    [Fact]
    public void RoarksScriptSetsWhatTheOriginalsDoes()
    {
        var game = new OpeningTests.Game(0);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Prinplup")!, 20));
        game.Arrive("OreburghGym", 5, 24);
        Assert.Contains(game.Talk("roark").Log, l => l.StartsWith("battle leader_roark Won"));
        Assert.True(game.Story.HasBadge(Badge.Coal));
        // Beside what plan 02 · S5's script set: the basement, the Global Terminal's greeter, Looker's Pal Pad
        foreach (var flag in StoryMigration.RoarkFlags) Assert.True(game.Story.Has(flag), flag);
        foreach (var variable in StoryMigration.RoarkVariables) Assert.Equal(1, game.Story.Var(variable));
        Assert.True(game.Story.HasDefeated("youngster_jonathon") && game.Story.HasDefeated("youngster_darius"));
        // The statue lists the rival, and the player too once the Badge is won
        Assert.Contains(game.Talk("gym_guide").Transcript, l => l.Text.Contains("beat Roark"));
    }

    [Fact]
    public void ASaveThatWonTheCoalBadgeBeforeIsGivenWhatRoarksScriptAlsoSets()
    {
        // Version 7 (plan 01 · M9 1b)
        var story = new StoryState();
        story.GiveBadge(Badge.Coal);
        story.SetVar("VAR_JUBILIFE_LOOKER_PAL_PAD_STATE", 2);
        StoryMigration.Upgrade(story, 6, System.Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.True(story.Has("FLAG_HIDE_POKECENTER_BASEMENT_BLOCKADE"));
        Assert.Equal(1, story.Var("VAR_GTS_ACCESS_STATE"));
        // A scene that moved a variable on is left where it is
        Assert.Equal(2, story.Var("VAR_JUBILIFE_LOOKER_PAL_PAD_STATE"));

        // Without the Badge nothing is set; a save of today is left as it is
        var early = new StoryState();
        StoryMigration.Upgrade(early, 6, System.Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.False(early.Has("FLAG_HIDE_POKECENTER_BASEMENT_BLOCKADE"));
        var today = new StoryState();
        today.GiveBadge(Badge.Coal);
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, System.Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.Equal(0, today.Var("VAR_GTS_ACCESS_STATE"));
        Assert.True(StoryState.CurrentVersion >= 8);
    }
}
