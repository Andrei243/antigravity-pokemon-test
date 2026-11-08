using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Kalos's port, the places reached only by Surf or Waterfall, the HMs and Ambrette's fossil laboratory (plan 23, after
/// Kalos's Gyms; docs/mechanics/rulings.md, "Kalos's port, the bay's islets, Waterfall and the other HMs"): Coumarine
/// City's quay and harbour and the islets of the bay, Victory Road's lake and the chamber past it, the pool under
/// Couriway's falls, HM01, HM02, HM04 and HM07 given once each, the small tree and the boulder of Route 6's hedge, the
/// three places of evolution, and the Old Amber revived.
/// </summary>
public class KalosPortAndHmsTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Kalos"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Kalos)!.Start!));

    private static World Kalos => LoadedWorld.Value;
    private static Map Overworld => BuiltMaps.Value["Kalos"];
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    [Fact]
    public void CoumarinesHarbourLeadsBySurfToTheBaysIsletsAndTheirPokemon()
    {
        var map = Overworld;
        // The quay south of the garden, the harbour's water below it, and the bay's islets beyond
        Assert.Equal("Coumarine City", map.DisplayNameAt(250, 506));
        Assert.True(map.IsDeepWater(250, 508));
        Assert.Equal("Coumarine Bay", map.DisplayNameAt(191, 524));
        Assert.True(map.IsTallGrass(191, 524));
        Assert.True(map.IsDeepWater(200, 530));
        // Nobody walks there from the quay; a surfer does, and the walk of the whole game finds both islets
        var onFoot = OnFoot(map, (250, 506), (170, 450, 300, 560));
        Assert.DoesNotContain((191, 524), onFoot);
        Assert.Contains((191, 524), Walked.Value[map]);
        Assert.Contains((224, 540), Walked.Value[map]);
        // The islets hold X and Y's Route 12 Pokémon that its drawn table has no room for
        var bay = Kalos.Area("coumarine_bay")!;
        foreach (string s in new[] { "Tauros", "Miltank", "Mareep", "Pinsir", "Heracross", "Exeggcute", "Chatot", "Pachirisu" })
            Assert.Contains(bay.Land!, e => e.Species == s);
        Assert.Contains(bay.GoodRod!, e => e.Species == "Clamperl");
        Assert.Contains(bay.SuperRod!, e => e.Species == "Corsola");
        Assert.NotEmpty(Kalos.Area("coumarine_city")!.Water!);
        Assert.NotEmpty(map.FindArea("coumarine_bay")!.RodEncounters[(int)FishingRod.Super]);
        // Two balls on the islets
        Assert.Contains(map.Everyone, n => n.IsItemBall && n.Item == "Big Pearl" && (n.GridX, n.GridY) == (191, 524));
        Assert.Contains(map.Everyone, n => n.IsItemBall && n.Item == "Shell Bell" && (n.GridX, n.GridY) == (224, 540));
    }

    [Fact]
    public void VictoryRoadsLakeAndCouriwaysPoolAreReachedOnlyOnTheWater()
    {
        var road = MapNamed("KalosVictoryRoad");
        Assert.True(road.IsDeepWater(14, 48));
        Assert.Equal(TileType.CaveFloor, road.GetGroundTile(12, 56));
        var onFoot = OnFoot(road, (30, 50), (0, 0, road.Width, road.Height));
        Assert.DoesNotContain((12, 56), onFoot);
        Assert.Contains((12, 56), Walked.Value[road]);
        Assert.Contains(road.Everyone, n => n.IsItemBall && n.Item == "Dragon Fang");
        var lake = Kalos.Area("kalos_victory_road")!;
        Assert.Contains(lake.GoodRod!, e => e.Species == "Dratini");
        Assert.NotEmpty(lake.Water!);

        // Couriway's falls: taken north or south only with Waterfall, into the pool below them
        var map = Overworld;
        for (int z = 372; z <= 374; z++)
            foreach (int x in new[] { 650, 651 })
                Assert.Equal(TileBehavior.Waterfall, map.BehaviourAt(x, z));
        Assert.True(map.IsDeepWater(650, 376));
        var surfer = new Walker(TravelMode.Surfing, map.HeightAt(650, 371));
        Assert.Equal(Obstacle.Waterfall, FieldMovement.Step(map, 650, 371, Direction.Down, surfer).Obstacle);
        var down = FieldMovement.Step(map, 650, 371, Direction.Down, surfer with { Moves = FieldMoves.Waterfall });
        Assert.True(down.Moves);
        Assert.Equal((650, 375), (down.X, down.Y));
        Assert.Contains(map.Everyone, n => n.IsItemBall && n.Item == "Splash Plate" && (n.GridX, n.GridY) == (654, 377));
        Assert.Contains((654, 377), Walked.Value[map]);
    }

    [Fact]
    public void TheHmsOfKalosAreGivenOnceEach()
    {
        var map = Overworld;
        var story = new StoryState();
        var bag = new Inventory();
        // HM01 with the palace's flute, from the butler on Route 6
        story.SetVar("VAR_KALOS_STORY", 14);
        Play(story, "kalos_route_6", "Butler", map, map.Everyone.First(n => n.Key == "butler"), bag);
        // HM04 from Grant with his TM
        story.GiveBadge(RegionDatabase.Kalos, "Cliff");
        Play(story, "CyllageCity_Gym", "Grant", Rooms.Value("CyllageCity_Gym"), Rooms.Value("CyllageCity_Gym").Everyone.First(n => n.Key == "grant"), bag);
        // HM02 from the professor on Coumarine's quay, there once the Plant Badge is won
        var sycamore = map.Everyone.First(n => n.Key == "sycamore_port");
        Assert.Equal("FLAG_KALOS_DEFEATED_COUMARINE_GYM", sycamore.ShownBy);
        Play(story, "coumarine_city", "Sycamore", map, sycamore, bag);
        // HM07 (Waterfall) from Shauna on Route 19
        Play(story, "kalos_route_19", "Shauna", map, map.Everyone.First(n => n.Key == "shauna_waterfall"), bag);
        foreach (string hm in new[] { "HM01", "HM02", "HM04", "HM07", "Poké Flute", "TM39" })
            Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(hm)!));
        // Spoken to again, the butler and Grant give nothing more; the professor and Shauna are gone
        Play(story, "kalos_route_6", "Butler", map, map.Everyone.First(n => n.Key == "butler"), bag);
        Play(story, "CyllageCity_Gym", "Grant", Rooms.Value("CyllageCity_Gym"), Rooms.Value("CyllageCity_Gym").Everyone.First(n => n.Key == "grant"), bag);
        foreach (string hm in new[] { "HM01", "HM04" })
            Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(hm)!));
        Assert.True(story.Has("FLAG_KALOS_RECEIVED_HM02") && story.Has("FLAG_KALOS_RECEIVED_HM07"));

        // A save already past the flute is given HM01 by the butler when spoken to again
        var later = new StoryState();
        var laterBag = new Inventory();
        laterBag.AddItem(ItemDatabase.Get("Poké Flute")!, 1);
        Play(later, "kalos_route_6", "Butler", map, map.Everyone.First(n => n.Key == "butler"), laterBag);
        Assert.Equal(1, laterBag.GetQuantity(ItemDatabase.Get("HM01")!));
    }

    [Fact]
    public void RouteSixsHedgeHasASmallTreeAndABoulderBeforeWhatTheyGuard()
    {
        var map = Overworld;
        Assert.Contains(map.Everyone, n => n.Obstacle == PropType.CutTree && (n.GridX, n.GridY) == (212, 267));
        Assert.Contains(map.Everyone, n => n.Obstacle == PropType.StrengthBoulder && (n.GridX, n.GridY) == (190, 266));
        var onFoot = OnFoot(map, (201, 272), (184, 262, 220, 300));
        Assert.DoesNotContain((212, 266), onFoot);
        Assert.DoesNotContain((191, 266), onFoot);
        Assert.Contains(map.Everyone, n => n.IsItemBall && (n.GridX, n.GridY) == (212, 266));
        Assert.Contains(map.Everyone, n => n.IsItemBall && (n.GridX, n.GridY) == (191, 266));
    }

    [Fact]
    public void KalosNamesItsThreePlacesOfEvolutionAndAmbrettesLaboratoryRevivesTheOldAmber()
    {
        var kalos = SpeciesSources.Of(RegionDatabase.Kalos);
        foreach (string site in new[] { "Moss Rock", "Ice Rock", "Magnetic Field" }) Assert.Contains(site, kalos.Sites);
        foreach (string s in new[] { "Leafeon", "Glaceon", "Magnezone", "Probopass", "Aerodactyl" }) Assert.True(kalos.Ways.ContainsKey(s), s);
        Assert.Equal(new[] { "Moss Rock" }, Kalos.Overlay("kalos_route_20")!.EvolutionSites);

        // The laboratory's assistant gives an Old Amber once
        var lab = Rooms.Value("AmbretteTown_FossilLab");
        var story = new StoryState();
        var bag = new Inventory();
        Play(story, "AmbretteTown_FossilLab", "Assistant", lab, lab.Everyone.First(n => n.Key == "assistant"), bag);
        Play(story, "AmbretteTown_FossilLab", "Assistant", lab, lab.Everyone.First(n => n.Key == "assistant"), bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Old Amber")!));
        Assert.Equal("AmbretteTown_FossilLab", Overworld.GetWarpAt(89, 490)?.TargetMap);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>The tiles walked to on foot, with no field move, inside a box (x0, z0, x1, z1).</summary>
    private static HashSet<(int X, int Y)> OnFoot(Map map, (int X, int Y) from, (int X0, int Z0, int X1, int Z1) box)
    {
        var seen = new HashSet<(int X, int Y)> { from };
        var queue = new Queue<(int X, int Y)>(seen);
        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();
            foreach (var dir in new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right })
            {
                var step = FieldMovement.Step(map, x, y, dir, new Walker(TravelMode.OnFoot, map.HeightAt(x, y)));
                if (!step.Moves || step.X < box.X0 || step.Y < box.Z0 || step.X >= box.X1 || step.Y >= box.Z1) continue;
                if (seen.Add((step.X, step.Y))) queue.Enqueue((step.X, step.Y));
            }
        }
        return seen;
    }

    private static HeadlessScriptHost Play(StoryState story, string file, string script, Map at, NPC? who, Inventory bag)
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 80));
        var host = new HeadlessScriptHost(story, party, bag) { Map = at, Region = RegionDatabase.Kalos, MapNamed = n => MapNamed(n) };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }
}
