using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The maps' answers by tile (plan 01 · M12): who stands on a tile, which way a tile leads and whether it can be
/// stood on are found by where things are, not by going through every person and warp of the map; these hold the
/// found answers equal to the searched ones, on every map of the game, as people move, are hidden and shown, and as
/// ways shut and open.
/// </summary>
public class MapIndexTests
{
    // What the maps answered before they kept anything by place: a search of the whole list, as it was written
    private static NPC? ScanNpcAt(Map map, int x, int y) => map.NPCs.FirstOrDefault(n => n.GridX == x && n.GridY == y);

    private static NPC? ScanNpcIn(Map map, int x, int y, float height) =>
        map.NPCs.FirstOrDefault(n => n.GridX == x && n.GridY == y && MathF.Abs((n.Level ?? map.HeightAt(x, y)) - height) < FieldMovement.StepLimit);

    // Among the open ways of a tile, one that is there from a flag wins (plan 02 · S15, part c); else the first
    private static Warp? ScanWarpAt(Map map, int x, int y)
    {
        var open = map.Warps.Where(w => w.SourceX == x && w.SourceY == y && !w.Shut).ToList();
        return open.FirstOrDefault(w => w.From != null) ?? open.FirstOrDefault();
    }

    private static bool ScanIsWalkable(Map map, int x, int y)
    {
        if (!map.InBounds(x, y) || map.IsSolid(x, y)) return false;
        if (map.NPCs.Any(n => n.GridX == x && n.GridY == y)) return false;
        var behaviour = map.BehaviourAt(x, y);
        if (FieldMovement.LedgeDirection(behaviour) != null) return false;
        return !(TileBehaviors.IsSurfable(behaviour) && map.DeckAt(x, y) == null);
    }

    /// <summary>The tiles worth asking about: where anyone or any warp is, the tiles round them, and a scatter of others.</summary>
    private static List<(int X, int Y)> Tiles(Map map, Random rng)
    {
        var tiles = new HashSet<(int, int)>();
        void Round(int x, int y)
        {
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++) tiles.Add((x + dx, y + dy));
        }
        foreach (var npc in map.Everyone) Round(npc.GridX, npc.GridY);
        foreach (var warp in map.Warps) Round(warp.SourceX, warp.SourceY);
        for (int i = 0; i < 200; i++) tiles.Add((rng.Next(-2, map.Width + 2), rng.Next(-2, map.Height + 2)));
        return tiles.ToList();
    }

    private static void TheFoundAnswersAreTheSearchedOnes(Map map, Random rng, string when)
    {
        foreach (var (x, y) in Tiles(map, rng))
        {
            Assert.True(ReferenceEquals(ScanNpcAt(map, x, y), map.GetNpcAt(x, y)), $"{map.Name} {x},{y}: who is here, {when}");
            Assert.True(ReferenceEquals(ScanWarpAt(map, x, y), map.GetWarpAt(x, y)), $"{map.Name} {x},{y}: which way it leads, {when}");
            Assert.Equal(map.Warps.Where(w => w.SourceX == x && w.SourceY == y), map.WarpsAt(x, y));
            if (!map.InBounds(x, y)) continue;
            Assert.True(ScanIsWalkable(map, x, y) == map.IsWalkable(x, y), $"{map.Name} {x},{y}: whether it is walked, {when}");
            // At the ground's height, at a deck's and at a height nobody stands at
            foreach (float height in new[] { map.HeightAt(x, y), map.DeckAt(x, y) ?? map.HeightAt(x, y) + 2f, map.HeightAt(x, y) + 40f })
                Assert.True(ReferenceEquals(ScanNpcIn(map, x, y, height), map.NpcIn(x, y, height)), $"{map.Name} {x},{y} at {height}: who is in the way, {when}");
        }
    }

    [Fact]
    public void OnEveryMapOfTheGameWhatIsFoundByPlaceIsWhatASearchFinds()
    {
        var maps = MapDatabase.Fresh();
        var rng = new Random(20261011);
        int people = 0, warps = 0;
        foreach (string name in MapDatabase.MapNames)
        {
            var map = maps(name);
            if (map.Name != name) continue;
            people += map.NPCs.Count;
            warps += map.Warps.Count;
            TheFoundAnswersAreTheSearchedOnes(map, rng, "as the game begins");
        }
        // The whole world was asked: thousands of people and ways
        Assert.True(people > 3000, $"{people} people");
        Assert.True(warps > 1000, $"{warps} warps");
    }

    [Fact]
    public void TheAnswersHoldAsPeopleMoveAreHiddenAndShownAndWaysShutAndOpen()
    {
        var maps = MapDatabase.Fresh();
        var rng = new Random(77);
        // The map of Sinnoh, which holds most of the game's people, and a few rooms and caves of each kind
        foreach (string name in new[] { "Sinnoh", "JubilifePokemonCenter", "OreburghMineB1F", "VeilstoneGym", "LakeValor", "ValorLakefrontOutdoors" }.Where(MapDatabase.MapNames.Contains))
        {
            var map = maps(name);
            var order = map.NPCs.ToList();

            // People move: a step, a walk across the map, off its edge and back, and onto one another's tiles
            var movers = map.NPCs.Where((_, i) => i % 3 == 0).ToList();
            foreach (var npc in movers)
            {
                switch (rng.Next(4))
                {
                    case 0: npc.GridX += rng.Next(-1, 2); break;
                    case 1: (npc.GridX, npc.GridY) = (npc.GridX + rng.Next(-70, 71), npc.GridY + rng.Next(-70, 71)); break;
                    case 2: npc.GridY = -1 - rng.Next(3); break;
                    default:
                        var other = map.NPCs[rng.Next(map.NPCs.Count)];
                        (npc.GridX, npc.GridY) = (other.GridX, other.GridY);
                        break;
                }
            }
            TheFoundAnswersAreTheSearchedOnes(map, rng, "after people moved");
            Assert.Equal(order, map.NPCs);

            // Hidden and shown: every flag set takes those it hides off the map and brings on those it shows
            var flags = new HashSet<string>();
            foreach (var npc in map.Everyone)
                if ((npc.HiddenBy ?? npc.ShownBy) is { } flag && rng.Next(2) == 0) flags.Add(flag);
            map.ApplyPresence(flags.Contains);
            map.ApplyDoors(flags.Contains);
            TheFoundAnswersAreTheSearchedOnes(map, rng, "after some were hidden and some shown");
            // Whoever is off the map moves too, and is found where they stand once they are back
            foreach (var npc in map.Absent.Take(40)) npc.GridX += 5;
            map.ApplyPresence(_ => false);
            TheFoundAnswersAreTheSearchedOnes(map, rng, "after everyone a flag hides came back");
            map.ApplyPresence(_ => true);
            TheFoundAnswersAreTheSearchedOnes(map, rng, "with every flag set");

            // Ways shut and open with the story's flags, each way round
            foreach (var set in new Func<string, bool>[] { _ => true, _ => false, f => f.Length % 2 == 0 })
            {
                map.ApplyDoors(set);
                TheFoundAnswersAreTheSearchedOnes(map, rng, "after the story's doors were set");
            }

            // Someone taken off the map, and someone new put on it
            if (map.NPCs.Count > 0)
            {
                var gone = map.NPCs[rng.Next(map.NPCs.Count)];
                map.NPCs.Remove(gone);
                Assert.Null(map.NPCs.FirstOrDefault(n => n == gone));
                gone.GridX += 3;   // and what they do afterwards is no longer the map's business
            }
            var come = new NPC { Name = "Newcomer", GridX = map.Width / 2, GridY = map.Height / 2 };
            map.Add(come);
            come.GridX -= 9;
            TheFoundAnswersAreTheSearchedOnes(map, rng, "after one left and one came");
            Assert.Same(come, map.NPCs.Last());
        }
    }

    [Fact]
    public void APlaceTheStoryRevealsAndChunksItSwapsKeepTheWaysRight()
    {
        var maps = MapDatabase.Fresh();
        var map = maps("Sinnoh");
        var rng = new Random(5);
        var variables = new Dictionary<string, int>();
        int Var(string name) => variables.GetValueOrDefault(name);
        map.ApplyHiddenPlaces(Var);
        map.ApplySwappedPlaces(Var);
        TheFoundAnswersAreTheSearchedOnes(map, rng, "with the hidden places hidden");
        int before = map.Warps.Count;
        foreach (var place in map.HiddenPlaces) variables[place.Var] = place.Value;
        foreach (var place in map.SwappedPlaces) variables[place.Var] = place.Value;
        Assert.NotEmpty(map.ApplyHiddenPlaces(Var));
        map.ApplySwappedPlaces(Var);
        map.ApplyPresence(_ => false);
        TheFoundAnswersAreTheSearchedOnes(map, rng, "with them revealed");
        Assert.True(map.Warps.Count >= before);
        variables.Clear();
        map.ApplyHiddenPlaces(Var);
        map.ApplySwappedPlaces(Var);
        TheFoundAnswersAreTheSearchedOnes(map, rng, "with them hidden again");
    }

    [Fact]
    public void WhoIsNearAPlaceIsFoundInTheMapsOwnOrder()
    {
        var map = MapDatabase.Fresh()("Sinnoh");
        var rng = new Random(11);
        var buffer = new List<NPC>();
        for (int i = 0; i < 60; i++)
        {
            int x0 = rng.Next(-20, map.Width), y0 = rng.Next(-20, map.Height), w = rng.Next(1, 200), h = rng.Next(1, 200);
            var scanned = map.NPCs.Where(n => n.GridX >= x0 && n.GridX <= x0 + w && n.GridY >= y0 && n.GridY <= y0 + h).ToList();
            Assert.Equal(scanned, map.NPCs.In(x0, y0, x0 + w, y0 + h, buffer));
        }
        // The whole map and more, and nothing at all
        Assert.Equal(map.NPCs.ToList(), map.NPCs.In(-5000, -5000, 5000, 5000));
        Assert.Empty(map.NPCs.In(10, 10, 9, 9));
        var someone = map.NPCs[map.NPCs.Count / 2];
        Assert.Contains(someone, map.NPCs.Near(someone.GridX + 3, someone.GridY - 3, 3));
        Assert.DoesNotContain(someone, map.NPCs.Near(someone.GridX + 4, someone.GridY, 3));
    }

    [Fact]
    public void AListOfPeopleIsAListLikeAnyOther()
    {
        var a = new NPC { Name = "A", GridX = 1, GridY = 1 };
        var b = new NPC { Name = "B", GridX = 1, GridY = 1 };
        var c = new NPC { Name = "C", GridX = 40, GridY = 2 };
        var list = new PeopleList { a, b, c };
        Assert.Equal(3, list.Count);
        Assert.Equal(new[] { a, b, c }, list);
        Assert.Same(a, list.At(1, 1));
        Assert.Same(b, list.At(1, 1, a, (n, first) => n != first));
        Assert.Null(list.At(2, 1));

        // Put in before the others, someone is the first on their tile; the order is the list's own
        var first = new NPC { Name = "First", GridX = 1, GridY = 1 };
        list.Insert(0, first);
        Assert.Same(first, list.At(1, 1));
        Assert.Equal(new[] { first, a, b, c }, list);
        list[0] = c;
        Assert.Same(a, list.At(1, 1));
        // The same person twice is on their tile once, and stays while one of the two is left
        Assert.Equal(new[] { c, a, b, c }, list);
        Assert.True(list.Remove(c));
        Assert.Same(c, list.At(40, 2));
        Assert.True(list.Remove(c));
        Assert.Null(list.At(40, 2));
        Assert.False(list.Remove(c));

        Assert.Equal(1, list.RemoveAll(n => n.Name == "A"));
        Assert.Same(b, list.At(1, 1));
        list.AddRange(new[] { a, c });
        Assert.Equal(new[] { b, a, c }, list);
        Assert.Same(b, list.At(1, 1));
        // (The list's own Contains is what is asked here: it answers from its index)
        bool holds = list.Contains(a);
        Assert.True(holds);
        Assert.Equal(1, list.IndexOf(a));
        list.RemoveAt(0);
        Assert.Same(a, list.At(1, 1));
        list.Clear();
        Assert.Empty(list);
        Assert.Null(list.At(1, 1));
        // Nobody of an emptied list tells it where they go
        a.GridX = 5;
        Assert.Null(list.At(5, 1));
    }

    [Fact]
    public void SomeoneOnTwoMapsListsIsFoundOnBothWhereverTheyGo()
    {
        // Whoever travels with the player is, for a moment, on the map left and the map come to
        var one = new PeopleList();
        var two = new PeopleList();
        var walker = new NPC { Name = "Walker", GridX = 3, GridY = 3 };
        one.Add(walker);
        two.Add(walker);
        walker.GridX = 100;
        walker.GridY = 200;
        Assert.Same(walker, one.At(100, 200));
        Assert.Same(walker, two.At(100, 200));
        Assert.Null(one.At(3, 3));
        one.Remove(walker);
        walker.GridX = 7;
        Assert.Same(walker, two.At(7, 200));
        Assert.Null(one.At(7, 200));
        two.Remove(walker);
        walker.GridX = 9;
        Assert.Null(two.At(9, 200));
    }

    [Fact]
    public void AListOfWarpsCountsItsChanges()
    {
        var map = new Map(8, 8);
        Assert.Null(map.GetWarpAt(2, 2));
        var door = new Warp { SourceX = 2, SourceY = 2, TargetMap = "Inside" };
        int before = map.Warps.Version;
        map.Warps.Add(door);
        Assert.NotEqual(before, map.Warps.Version);
        Assert.Same(door, map.GetWarpAt(2, 2));

        // A warp moved after it was put on the map is found where it went (the builder aims some of them afterwards)
        door.SourceX = 5;
        Assert.Null(map.GetWarpAt(2, 2));
        Assert.Same(door, map.GetWarpAt(5, 2));

        // One taken off as another is put on: as many as before, and still right
        var other = new Warp { SourceX = 6, SourceY = 6, TargetMap = "Elsewhere" };
        map.Warps.RemoveAll(w => w == door);
        map.Warps.Add(other);
        Assert.Null(map.GetWarpAt(5, 2));
        Assert.Same(other, map.GetWarpAt(6, 6));

        // Two on one tile: the one the story has open
        var second = new Warp { SourceX = 6, SourceY = 6, TargetMap = "Later", Shut = true };
        map.Warps.Add(second);
        Assert.Same(other, map.GetWarpAt(6, 6));
        other.Shut = true;
        second.Shut = false;
        Assert.Same(second, map.GetWarpAt(6, 6));
        Assert.Equal(new[] { other, second }, map.WarpsAt(6, 6));
        map.Warps.Clear();
        Assert.Null(map.GetWarpAt(6, 6));
    }

    [Fact]
    public void NoTrainerSeesFurtherThanTheTilesTheirSightIsAskedWithin()
    {
        // Only people within SightReach of a tile are asked whether they see it
        var maps = MapDatabase.Fresh();
        int longest = MapDatabase.MapNames.Select(maps).SelectMany(m => m.Everyone).Where(n => n.TrainerData != null).Max(n => n.TrainerData!.SightRange);
        Assert.InRange(longest, 1, TrainerApproach.SightReach);

        // And a trainer at the very end of their sight is still found
        var map = new Map(40, 8);
        var trainer = new NPC { Name = "Lookout", IsTrainer = true, TrainerData = new PokemonPlatinumEngine.Models.Trainer { Id = "lookout", SightRange = longest }, GridX = 2, GridY = 2, Facing = Direction.Right };
        map.Add(trainer);
        Assert.Same(trainer, TrainerApproach.FindSpotter(map, 2 + longest, 2));
        Assert.Null(TrainerApproach.FindSpotter(map, 3 + longest, 2));
    }

    [Fact]
    public void OnlyThoseNearThePlayerMoveAboutAndAStepBegunIsFinished()
    {
        var map = new Map(400, 12);
        NPC Wanderer(string name, int x) => new()
        {
            Name = name, GridX = x, GridY = 5,
            Movement = PersonMovement.Parse("wander_around", 3, 3, x, 5, Direction.Down) ?? throw new InvalidOperationException("no such movement")
        };
        var near = Wanderer("Near", 10);
        var far = Wanderer("Far", 10 + Wandering.Reach + 20);
        map.Add(near);
        map.Add(far);
        var wandering = new Wandering();
        var rng = new Random(3);
        for (int i = 0; i < 60 * 30; i++) wandering.Update(1f / 60f, map, (8, 8), (8, 8), rng);
        Assert.NotEqual((10, 5), (near.GridX, near.GridY));
        Assert.Equal((10 + Wandering.Reach + 20, 5), (far.GridX, far.GridY));

        // The player comes within reach of the other: it moves; and leaves while someone is in the middle of a step
        for (int i = 0; i < 60 * 30 && !wandering.AnyoneStepping; i++) wandering.Update(1f / 60f, map, (far.GridX, 8), (far.GridX, 8), rng);
        Assert.True(wandering.AnyoneStepping);
        for (int i = 0; i < 120; i++) wandering.Update(1f / 60f, map, (200, 8), (200, 8), rng);
        Assert.False(wandering.AnyoneStepping);
    }
}
