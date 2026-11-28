using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The Floccesy Reserve (plan 38 · EX9; <c>docs/mechanics/rulings.md</c>, "The Floccesy Reserve"): its grounds and their
/// tables, the objects' slots laid in, the way in for a Champion, the game from the counter to its last step with a
/// ground arranged, and the way back.
/// </summary>
public class UnovaReserveTests
{
    private static Map Load(string name) => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json"), strict: true).ToMap();

    private static Map Zone() => Load(UnovaReserve.ZoneMap);

    private static PlacedSafariObject Placed(string key, int day, int x = 0, int y = 0) =>
        new() { Type = SafariObjects.Catalogue.Single(t => t.Key == key).Id, X = x, Y = y, Day = day };

    [Fact]
    public void EveryGroundHasATableOfTenAndSlotsItsObjectsCanOpen()
    {
        Assert.Equal(3, UnovaReserve.Grounds.Count);
        Assert.Equal(15, SafariObjects.Arrangeable.Count);
        foreach (var ground in UnovaReserve.Grounds)
        {
            Assert.Equal(UnovaReserve.TableSlots, ground.Table.Count);
            Assert.InRange(ground.Slots.Count, 1, UnovaReserve.TableSlots);
            Assert.All(ground.Table, s => { Assert.NotNull(PokemonDatabase.Get(s.Species)); Assert.InRange(s.Min, 55, s.Max); Assert.InRange(s.Max, s.Min, 61); });
            Assert.All(ground.Slots, s =>
            {
                Assert.NotNull(PokemonDatabase.Get(s.Species));
                Assert.Equal(SafariWay.Land, s.Way);
                // Each count can be met with the store's three of each once they have stood ten days
                Assert.All(s.Needs, n => Assert.InRange(n.Count, 1, 2 * SafariObjects.Catalogue.Sum(t => t.PointsOf(n.Kind)) * SafariObjects.CopiesOfEach));
            });
        }
    }

    [Fact]
    public void TheTallGrassLiesOnTheGroundsAndMeetsEachGroundsOwn()
    {
        var map = Zone();
        var met = new HashSet<string>();
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                if (!map.IsTallGrass(x, y)) continue;
                var ground = UnovaReserve.GroundAt(x, y);
                Assert.NotNull(ground);
                var (table, rate) = map.WildAt(x, y);
                Assert.True(rate > 0);
                Assert.Equal(ground!.Table.Select(s => s.Species), table.Select(e => e.SpeciesName));
                met.Add(ground.Key);
            }
        Assert.Equal(3, met.Count);
        // No water encounters
        Assert.Empty(map.WildAt(10, 22, water: true).Table);
    }

    [Fact]
    public void AnObjectsSlotsTakeTheFirstOfItsGroundsTableAndNoOtherGrounds()
    {
        var meadow = UnovaReserve.Grounds[0];
        var woods = UnovaReserve.Grounds[1];
        var placed = new SafariObjects();
        // Two haystacks are four of plains: Kangaskhan's three, not Tauros's six, from the day after
        placed.Set(meadow.Key, new[] { Placed("haystack", 100), Placed("haystack", 100) });
        List<WildEncounterEntry> At(ReserveGround g, int day) =>
            UnovaReserve.Table(g.X + 3, g.Y + 3, new EncounterMoment { SafariObjects = placed, SafariDay = day });
        Assert.Equal(meadow.Table[0].Species, At(meadow, 100)[0].SpeciesName);
        var open = At(meadow, 101);
        Assert.Equal(("Kangaskhan", 58), (open[0].SpeciesName, open[0].MaxLevel));
        Assert.Equal(meadow.Table.Skip(1).Select(s => s.Species), open.Skip(1).Select(e => e.SpeciesName));
        Assert.Equal(woods.Table.Select(s => s.Species), At(woods, 101).Select(e => e.SpeciesName));
        // Ten days on they count twice: eight of plains open Tauros too
        Assert.Equal("Tauros", At(meadow, 110)[1].SpeciesName);
    }

    [Fact]
    public void MeetingsAreDrawnFromEveryRowOfAGroundAndFromAnOpenedSlot()
    {
        var meadow = UnovaReserve.Grounds[0];
        var placed = new SafariObjects();
        placed.Set(meadow.Key, new[] { Placed("haystack", 1), Placed("haystack", 1) });
        var rng = new Random(38);
        HashSet<string> Met(EncounterMoment? moment)
        {
            var table = UnovaReserve.Table(meadow.X + 3, meadow.Y + 3, moment);
            var met = new HashSet<string>();
            for (int i = 0; i < 600; i++) met.Add(WildEncounterRules.Meet(table, false, null, rng)!.SpeciesName);
            return met;
        }
        Assert.Equal(meadow.Table.Select(s => s.Species).ToHashSet(), Met(null));
        var opened = Met(new EncounterMoment { SafariObjects = placed, SafariDay = 2 });
        Assert.Contains("Kangaskhan", opened);
        Assert.DoesNotContain(meadow.Table[0].Species, opened);
    }

    [Fact]
    public void EachGroundIsARoomOfOpenFloorForTheObjects()
    {
        var map = Zone();
        foreach (var ground in UnovaReserve.Grounds)
        {
            var room = SafariObjects.Room(map, ground.Bounds, Array.Empty<PlacedSafariObject>(), SafariObjects.StoreOf(Array.Empty<PlacedSafariObject>()),
                UnovaReserve.ArrivalX, UnovaReserve.ArrivalY);
            Assert.Equal(RoomTile.Floor, room.TileAt!(room.Start.X, room.Start.Y));
            int floor = Enumerable.Range(ground.Y, ground.Height).Sum(y => Enumerable.Range(ground.X, ground.Width).Count(x => room.TileAt(x, y) == RoomTile.Floor));
            Assert.True(floor > 60, $"{ground.Key} has {floor} tiles of floor");
            Assert.Equal(RoomTile.Rock, room.TileAt(UnovaReserve.ArrivalX, UnovaReserve.ArrivalY));
        }
    }

    [Fact]
    public void EveryPatchOfGrassCanBeWalkedToFromTheWayIn()
    {
        var map = Zone();
        var seen = new HashSet<(int, int)> { (UnovaReserve.ArrivalX, UnovaReserve.ArrivalY) };
        var queue = new Queue<(int X, int Y)>(seen);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                if (map.InBounds(nx, ny) && map.IsWalkable(nx, ny) && seen.Add((nx, ny))) queue.Enqueue((nx, ny));
        }
        for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
                if (map.IsTallGrass(x, y)) Assert.Contains((x, y), seen);
        // The way out leads to the gate's back tile, which is open floor
        var gate = Load(UnovaReserve.GateMap);
        var back = map.Warps.First(w => seen.Contains((w.SourceX, w.SourceY)));
        Assert.Equal(UnovaReserve.GateMap, back.TargetMap);
        Assert.True(gate.IsWalkable(back.TargetX, back.TargetY));
        Assert.Equal("FloccesyRanch", gate.Warps.Single().TargetMap);
    }

    private static HeadlessScriptHost Run(StoryState story, string script, string file, params int[] answers)
    {
        var scripts = ScriptLibrary.Default;
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Money = 3000 };
        foreach (int answer in answers) host.Answers.Enqueue(answer);
        var runner = new ScriptRunner(scripts, host);
        runner.Start(scripts.Find(script, file)!);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }

    [Fact]
    public void TheWardenTakesOnlyAChampion()
    {
        var story = new StoryState();
        var before = Run(story, "ReserveWarden", "floccesy_ranch_2", 0);
        Assert.Empty(before.Asked);
        Assert.DoesNotContain(before.Log, l => l.Contains(UnovaReserve.GateMap));
        story.Set("UnovaHallOfFame");
        var after = Run(story, "ReserveWarden", "floccesy_ranch_2", 0);
        Assert.Contains(after.Log, l => l.Contains(UnovaReserve.GateMap));
        Assert.DoesNotContain(Run(story, "ReserveWarden", "floccesy_ranch_2", 1).Log, l => l.Contains(UnovaReserve.GateMap));
    }

    [Fact]
    public void AGameIsPlayedToItsLastStepWithAGroundArranged()
    {
        var story = new StoryState();
        // The ranger arranges the woods, chosen second
        var arranged = Run(story, "Ranger", UnovaReserve.GateMap, 0, 1);
        Assert.Equal(new[] { 13 }, arranged.Arranged);
        Assert.Equal(UnovaReserve.Grounds[1].Key, SafariObjects.Arrangeable[13]);
        Assert.Empty(Run(story, "Ranger", UnovaReserve.GateMap, 0, 3).Arranged);
        Assert.Empty(Run(story, "Ranger", UnovaReserve.GateMap, 1).Arranged);

        // The counter takes $500 and begins a game of thirty balls and five hundred steps
        var game = Run(story, "Counter", UnovaReserve.GateMap, 0);
        Assert.True(game.Safari.Active);
        Assert.Equal((SafariGame.StartBalls, 500), (game.Safari.Balls, game.Safari.Steps));
        Assert.Equal(2500, game.Money);
        for (int step = 1; step < 500; step++) Assert.False(game.Safari.Step());
        Assert.True(game.Safari.Step());
    }

    [Theory]
    [InlineData("SafariTimeUp")]
    [InlineData("SafariOutOfBalls")]
    public void TheGamesEndBringsAPlayerOfUnovaBackToTheGate(string script)
    {
        var story = new StoryState();
        var scripts = ScriptLibrary.Default;
        var host = new HeadlessScriptHost(story, new Party(), new Inventory()) { Region = RegionDatabase.Unova };
        host.Safari.Start(500);
        var runner = new ScriptRunner(scripts, host);
        runner.Start(scripts.Find(script, "common")!);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.False(host.Safari.Active);
        Assert.Equal(2, story.Var("VAR_UNOVA_RESERVE"));
        Assert.Contains(host.Log, l => l.Contains(UnovaReserve.GateMap));
        // The gate's arrival sees them off once
        Run(story, "OnEnter", UnovaReserve.GateMap);
        Assert.Equal(0, story.Var("VAR_UNOVA_RESERVE"));
    }

    [Fact]
    public void TheWardenStandsOnOpenGroundOfTheRanchBesideTheTileTheGateLeadsBackTo()
    {
        var ranch = World.LoadAll().Single(w => w.Index.Region == RegionDatabase.Unova).BuildMaps().Single(m => m.Name == "FloccesyRanch");
        var way = Load(UnovaReserve.GateMap).Warps.Single();
        var warden = ranch.Everyone.Single(n => n.Script == "ReserveWarden");
        Assert.Equal((way.TargetX + 1, way.TargetY), (warden.GridX, warden.GridY));
        // He stands in the open: the tiles round him are walked, so nobody is shut out by him
        foreach (var (x, y) in new[] { (50, 39), (50, 41), (49, 40), (51, 40), (49, 39), (51, 39), (49, 41), (51, 41) })
            Assert.True(ranch.IsWalkable(x, y), $"{x},{y}");
    }

    [Fact]
    public void NobodyFliesOutOfTheReserve()
    {
        var map = Zone();
        var walker = new Walker();
        Assert.False(FieldMoveRules.SpotOf(map, UnovaReserve.ArrivalX, UnovaReserve.ArrivalY, Direction.Up, walker).FlyAllowed);
    }
}
