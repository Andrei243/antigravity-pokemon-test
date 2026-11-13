using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 38 · EX11: Unova's shaking grass, rippling water, dust clouds and bridges' shadows. The same day has the same patches and
/// another day others; each table is met on its patches and nowhere else; a cloud's item and Pokémon come from the seed; a
/// bridge's shadow meets the bridge's flying table; no area outside Unova has the tables; and with no day in force
/// (<see cref="EncounterMoment.SpecialDay"/> null) a step is rolled exactly as before.
/// </summary>
public class UnovaSpecialGrassTests
{
    private static readonly Lazy<List<Map>> Maps = new(() => World.LoadAll().Single(w => w.Index.Region == "Unova").BuildMaps().ToList());

    /// <summary>Every tile of every Unova map that is a patch of the kind on the day, with its area.</summary>
    private static List<(Map Map, MapArea Area, int X, int Y)> PatchesOf(SpecialKind kind, uint day, int most = 40)
    {
        var found = new List<(Map, MapArea, int, int)>();
        foreach (var map in Maps.Value)
        {
            if (map.Width * map.Height > 300_000) continue; // the overworld: patches are found on the places that are maps of their own
            for (int y = 0; y < map.Height && found.Count < most; y++)
                for (int x = 0; x < map.Width && found.Count < most; x++)
                    if (map.AreaAt(x, y) is { } area && UnovaSpecialGrass.KindAt(map, x, y, day, kind == SpecialKind.Rippling) == kind)
                        found.Add((map, area, x, y));
        }
        return found;
    }

    [Fact]
    public void TheSameDayHasTheSamePatchesAndTheNextDayOthers()
    {
        var today = Enumerable.Range(0, 2000).Where(i => UnovaSpecialGrass.Patch(SpecialKind.Shaking, "unova_route_8", 5u, i % 50, i / 50)).ToList();
        var again = Enumerable.Range(0, 2000).Where(i => UnovaSpecialGrass.Patch(SpecialKind.Shaking, "unova_route_8", 5u, i % 50, i / 50)).ToList();
        var tomorrow = Enumerable.Range(0, 2000).Where(i => UnovaSpecialGrass.Patch(SpecialKind.Shaking, "unova_route_8", 6u, i % 50, i / 50)).ToList();
        Assert.Equal(today, again);
        Assert.NotEqual(today, tomorrow);
        // About one candidate in Density
        Assert.InRange(today.Count, 2000 / UnovaSpecialGrass.Density / 2, 2000 / UnovaSpecialGrass.Density * 2);
    }

    [Fact]
    public void EachKindIsFoundOnTheTilesItBelongsOn()
    {
        foreach (var (map, area, x, y) in PatchesOf(SpecialKind.Shaking, 7))
        {
            Assert.True(map.IsTallGrass(x, y));
            Assert.NotEmpty(area.ShakingGrassEncounters);
            Assert.False(area.IsCave);
            Assert.DoesNotContain("bridge", area.Key);
        }
        foreach (var (map, area, x, y) in PatchesOf(SpecialKind.Dust, 7))
        {
            Assert.True(area.IsCave);
            Assert.True(map.IsWalkable(x, y));
            Assert.False(map.IsTallGrass(x, y));
        }
        foreach (var (map, area, x, y) in PatchesOf(SpecialKind.Shadow, 7))
            Assert.Contains("bridge", area.Key);
        foreach (var (map, area, x, y) in PatchesOf(SpecialKind.Rippling, 7))
        {
            Assert.True(TileBehaviors.IsSurfable(map.BehaviourAt(x, y)));
            Assert.True(area.RipplingWaterEncounters.Count > 0 || area.RipplingRodEncounters.Count > 0);
        }
    }

    [Fact]
    public void EveryKindHasPatchesInTheGame()
    {
        foreach (var kind in Enum.GetValues<SpecialKind>())
            Assert.NotEmpty(PatchesOf(kind, 11, 1));
    }

    [Fact]
    public void AStepOntoAPatchOfShakingGrassMeetsTheAreasTableEveryTimeAndAStepOntoAnythingElseIsAsBefore()
    {
        const uint day = 3;
        var (map, area, x, y) = PatchesOf(SpecialKind.Shaking, day, 1).First();
        var moment = new EncounterMoment { SpecialDay = day };
        var species = area.ShakingGrassEncounters.Select(e => e.SpeciesName).ToHashSet();
        for (int i = 0; i < 20; i++)
        {
            var met = map.RollWildEncounter(x, y, new EncounterSteps(), false, false, null, moment);
            Assert.NotNull(met);
            Assert.Contains(met!.SpeciesName, species);
        }
        // With no day in force the same tile is rolled as it always was: nobody is sure to meet anything on a first step
        var plain = new EncounterMoment();
        int met2 = Enumerable.Range(0, 20).Count(_ => map.RollWildEncounter(x, y, new EncounterSteps(), false, false, null, plain) != null);
        Assert.True(met2 < 20);
    }

    [Fact]
    public void ARippleMeetsTheRippleTablesOnFootAndOnARod()
    {
        const uint day = 3;
        var (map, area, x, y) = PatchesOf(SpecialKind.Rippling, day, 1).First();
        var moment = new EncounterMoment { SpecialDay = day };
        if (area.RipplingWaterEncounters.Count > 0)
        {
            var names = area.RipplingWaterEncounters.Select(e => e.SpeciesName).ToHashSet();
            for (int i = 0; i < 10; i++)
                Assert.Contains(map.RollWildEncounter(x, y, new EncounterSteps(), true, false, null, moment)!.SpeciesName, names);
        }
        if (area.RipplingRodEncounters.Count > 0)
        {
            var names = area.RipplingRodEncounters.Select(e => e.SpeciesName).ToHashSet();
            var bites = Enumerable.Range(0, 200).Select(_ => map.Fish(x, y, FishingRod.Super, null, moment)).Where(e => e != null).ToList();
            Assert.NotEmpty(bites);
            Assert.All(bites, b => Assert.Contains(b!.SpeciesName, names));
        }
    }

    [Fact]
    public void ACloudsFindAndABridgesShadowComeFromTheSeedAndTheAreasTable()
    {
        var (_, cave, cx, cy) = PatchesOf(SpecialKind.Dust, 9, 1).First();
        var one = UnovaSpecialGrass.DustAt(cave, 9, cx, cy);
        var two = UnovaSpecialGrass.DustAt(cave, 9, cx, cy);
        Assert.Equal(one.Item, two.Item);
        Assert.Equal(one.Pokemon?.SpeciesName, two.Pokemon?.SpeciesName);
        Assert.Equal(one.Pokemon?.MinLevel, two.Pokemon?.MinLevel);
        var finds = Enumerable.Range(0, 300).Select(i => UnovaSpecialGrass.DustAt(cave, (uint)i, cx, cy)).ToList();
        Assert.Contains(finds, f => f.Item != null && ItemDatabase.Get(f.Item) != null);
        Assert.Contains(finds, f => f.Pokemon != null);
        var names = cave.ShakingGrassEncounters.Select(e => e.SpeciesName).ToHashSet();
        foreach (var f in finds.Where(f => f.Pokemon != null))
        {
            Assert.Contains(f.Pokemon!.SpeciesName, names);
            Assert.InRange(f.Pokemon.MinLevel, cave.ShakingGrassEncounters.Min(e => e.MinLevel), cave.ShakingGrassEncounters.Max(e => e.MaxLevel));
        }
        Assert.All(finds.Where(f => f.Item != null), f => Assert.Contains(f.Item!, UnovaSpecialGrass.Gems));
        // The drawbridge's table is the Ducklett line that Black 2 flies over its deck
        var bridge = Maps.Value.SelectMany(m => m.Areas).First(a => a.Key == "driftveil_drawbridge");
        Assert.Equal(SpecialKind.Shadow, UnovaSpecialGrass.LandKindOf(bridge));
        Assert.All(bridge.ShakingGrassEncounters, e => Assert.Equal("Ducklett", e.SpeciesName));
    }

    [Fact]
    public void AnAreaWithNoSpecialTableHasNoKindOfPatch()
    {
        Assert.Null(UnovaSpecialGrass.LandKindOf(new MapArea { Key = "route_201" }));
    }
}
