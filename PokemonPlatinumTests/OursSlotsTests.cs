using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D15, design B: our own species past level 100 in the rarest slots of a place (an overlay's <c>ours</c>), at
/// their own levels, never mapped, once the region's Hall of Fame is entered (Sinnoh's once Kanto's is). The engine's
/// part; the placing is later sessions', each adding its region to <see cref="Placed"/>.
/// </summary>
public class OursSlotsTests
{
    /// <summary>
    /// The regions whose batch has been placed (plan 03 · D15's placement sessions): every placeable species of ours of
    /// such a region's batch lives somewhere in its overlays. A session that places a region adds it here.
    /// </summary>
    private static readonly string[] Placed =
    {
        RegionDatabase.Sinnoh,
        RegionDatabase.Kanto,
        RegionDatabase.Johto,
        RegionDatabase.Hoenn,
        RegionDatabase.Unova,
    };

    private static MapArea AreaWithTables()
    {
        var area = new MapArea { Key = "test_area", Open = true };
        foreach (int weight in WorldAreaFile.LandSlotWeights)
            area.WildEncounters.Add(new WildEncounterEntry { SpeciesName = "Raticate", MinLevel = 60, MaxLevel = 60, Weight = weight });
        foreach (int weight in WorldAreaFile.WaterSlotWeights)
            area.WaterEncounters.Add(new WildEncounterEntry { SpeciesName = "Tentacruel", MinLevel = 50, MaxLevel = 60, Weight = weight });
        foreach (int weight in WorldAreaFile.RodSlotWeights)
            area.RodEncounters[(int)FishingRod.Super].Add(new WildEncounterEntry { SpeciesName = "Gyarados", MinLevel = 50, MaxLevel = 60, Weight = weight });
        area.OursLand.Add(new OursSlot("Ratchisel", 105, 109));
        area.OursLand.Add(new OursSlot("Najarok", 106, 110));
        area.OursSurf.Add(new OursSlot("Ratchisel", 105, 105));
        area.OursSuperRod.Add(new OursSlot("Najarok", 106, 108));
        return area;
    }

    [Fact]
    public void SinnohsWaitForKantosHallOfFameAndEveryOtherRegionsForItsOwn()
    {
        var sinnoh = RegionDatabase.Get(RegionDatabase.Sinnoh)!;
        var johto = RegionDatabase.Get(RegionDatabase.Johto)!;
        Assert.Equal("KantoHallOfFame", OursSlots.GateFlag(sinnoh));
        Assert.Equal("JohtoHallOfFame", OursSlots.GateFlag(johto));

        var story = new StoryState();
        story.Set(sinnoh.StoryCompleteFlag);
        Assert.False(OursSlots.Open(sinnoh, story));
        Assert.False(OursSlots.Open(johto, story));
        story.Set("KantoHallOfFame");
        Assert.True(OursSlots.Open(sinnoh, story));
        Assert.False(OursSlots.Open(johto, story));
        Assert.False(OursSlots.Open(null, story));
    }

    [Fact]
    public void TheSlotsAreLaidOnlyOnceOpenInTheRarestSlotsAtTheirOwnLevels()
    {
        var area = AreaWithTables();
        var shut = new EncounterMoment();
        var open = new EncounterMoment { OursOpen = true };

        // Shut: the tables are the area's own
        Assert.All(EncounterSlots.Grass(area, shut), e => Assert.Equal("Raticate", e.SpeciesName));
        Assert.Same(area.WaterEncounters, EncounterSlots.Surf(area, area.WaterEncounters, shut));
        Assert.Same(area.RodEncounters[2], EncounterSlots.SuperRod(area, area.RodEncounters[2], shut));

        var grass = EncounterSlots.Grass(area, open);
        Assert.Equal(new[] { "Ratchisel", "Najarok" }, grass.Skip(OverlayOurs.FirstLandSlot).Select(e => e.SpeciesName));
        Assert.All(grass.Take(OverlayOurs.FirstLandSlot), e => Assert.Equal("Raticate", e.SpeciesName));
        Assert.Equal((105, 109, true), (grass[10].MinLevel, grass[10].MaxLevel, grass[10].Unmapped));
        Assert.Equal((1, 1), (grass[10].Weight, grass[11].Weight));
        Assert.False(area.WildEncounters[10].Unmapped); // the area's own table is untouched

        var surf = EncounterSlots.Surf(area, area.WaterEncounters, open);
        Assert.Equal(("Ratchisel", 105, true, 1), (surf[4].SpeciesName, surf[4].MinLevel, surf[4].Unmapped, surf[4].Weight));
        Assert.All(surf.Take(4), e => Assert.Equal("Tentacruel", e.SpeciesName));

        var rod = EncounterSlots.SuperRod(area, area.RodEncounters[2], open);
        Assert.Equal(("Najarok", 106, 108, true), (rod[4].SpeciesName, rod[4].MinLevel, rod[4].MaxLevel, rod[4].Unmapped));
    }

    [Fact]
    public void WhatIsMetFromASlotOfOursIsUnmappedAndTheRadarGivesItsSlotBack()
    {
        var area = AreaWithTables();
        var grass = EncounterSlots.Grass(area, new EncounterMoment { OursOpen = true });
        var onlyOurs = new List<WildEncounterEntry> { grass[10] };
        var met = WildEncounterRules.Meet(onlyOurs, water: false, lead: null, new Random(7))!;
        Assert.Equal("Ratchisel", met.SpeciesName);
        Assert.True(met.Unmapped);
        Assert.InRange(met.MinLevel, 105, 109);
        Assert.False(WildEncounterRules.Meet(grass.Take(1).ToList(), water: false, lead: null, new Random(7))!.Unmapped);

        // A hard shake's species takes slots 10 and 11 at the table's own levels
        area.RadarSlots.AddRange(new[] { "Pidgey", "Pidgey", "Rattata", "Rattata" });
        EncounterSlots.PutRadarSpecies(grass, area);
        Assert.Equal(("Rattata", 60, false), (grass[10].SpeciesName, grass[10].MinLevel, grass[10].Unmapped));

        // A swarm on the Super Rod leaves the slot of ours as it is
        area.FishSwarm = "Remoraid";
        var moment = new EncounterMoment { OursOpen = true, SwarmArea = area.Key, SwarmWay = SwarmWay.Fish };
        var rod = EncounterSlots.Rod(area, FishingRod.Super, EncounterSlots.SuperRod(area, area.RodEncounters[2], moment), moment);
        Assert.Equal("Najarok", rod[4].SpeciesName);
        Assert.All(rod.Take(4), e => Assert.Equal("Remoraid", e.SpeciesName));
    }

    [Fact]
    public void AnOverlaysOursIsWrittenAsArraysOfASpeciesAndItsTwoLevels()
    {
        var overlay = GameDataFiles.Deserialize<WorldOverlayFile>("""
            { "area": "x", "ours": { "land": [ ["Ratchisel", 105, 109], ["Najarok", 106, 110] ], "surf": [ ["Ratchisel", 105, 109] ], "superRod": [ ["Najarok", 106, 108] ] } }
            """);
        Assert.Equal(new OursSlot("Ratchisel", 105, 109), overlay.Ours!.Land![0]);
        Assert.Equal(new OursSlot("Najarok", 106, 108), overlay.Ours.SuperRod![0]);
        string written = GameDataFiles.Serialize(overlay.Ours);
        Assert.Contains("\"Ratchisel\"", written);
        Assert.Equal(overlay.Ours.Land, GameDataFiles.Deserialize<OverlayOurs>(written).Land);
    }

    [Fact]
    public void EachSpeciesOfOursHasItsBatchsRegionAndTheLegendarySetsAreNeverWild()
    {
        var placeable = PokemonDatabase.Ours().Where(OursSlots.Placeable).ToList();
        Assert.Equal(352, placeable.Count); // plan 03 · D15: 400 species of ours, 48 the ends of legendary sets
        Assert.DoesNotContain(PokemonDatabase.GetAll(), OursSlots.Placeable);
        string[] regions = { "Kanto", "Johto", "Hoenn", "Sinnoh", "Unova", "Kalos", "Alola", "Galar", "Hisui", "Paldea" };
        Assert.All(placeable, s => Assert.Contains(OursSlots.RegionOf(s), regions));
        Assert.All(placeable, s => Assert.InRange(OursSlots.EvolutionLevel(s), 101, Ruleset.HighestLevel));

        var ratchisel = PokemonDatabase.Get("Ratchisel")!;
        Assert.Equal(("Kanto", 105), (OursSlots.RegionOf(ratchisel), OursSlots.EvolutionLevel(ratchisel)));
        Assert.Equal("Johto", OursSlots.RegionOf(PokemonDatabase.Get("Pastorine")!));
        Assert.Null(OursSlots.RegionOf(PokemonDatabase.Get("Raticate")!));
    }

    [Fact]
    public void ThePokedexShowsThemAsAWayOfTheirOwn()
    {
        var file = new WorldHabitatsFile
        {
            Map = new List<string> { "..", ".." },
            Areas = new List<HabitatArea> { new() { Key = "test_area", Name = "Test Route", Cells = "1,1", Morning = new() { "Raticate" } } }
        };
        var habitats = new Habitats(file, ours: new[] { ("test_area", new OverlayOurs { Land = new() { new OursSlot("Ratchisel", 105, 109) }, SuperRod = new() { new OursSlot("Ratchisel", 105, 105) } }) });
        var place = Assert.Single(habitats.Of("Ratchisel"));
        Assert.Equal("Test Route", place.Name);
        Assert.Equal(HabitatWays.Grass | HabitatWays.SuperRod | HabitatWays.Ours, place.Ways);
        Assert.Equal(HabitatWays.Morning, Assert.Single(habitats.Of("Raticate")).Ways);
    }

    /// <summary>Every overlay's <c>ours</c> of the built regions, by region and area key.</summary>
    private static IEnumerable<(string Region, string Key, OverlayOurs Ours)> EveryOverlaysOurs()
    {
        string root = Path.Combine(AppContext.BaseDirectory, "Data", "world");
        Assert.True(Directory.Exists(Path.Combine(root, "sinnoh", "overlays")), root);
        foreach (string folder in Directory.GetDirectories(root))
        {
            string overlays = Path.Combine(folder, "overlays");
            if (!Directory.Exists(overlays)) continue;
            foreach (string file in Directory.GetFiles(overlays, "*.json"))
            {
                string text = File.ReadAllText(file);
                if (!text.Contains("\"ours\"")) continue;
                if (GameDataFiles.Deserialize<WorldOverlayFile>(text).Ours is { } ours)
                    yield return (Path.GetFileName(folder), Path.GetFileNameWithoutExtension(file), ours);
            }
        }
    }

    [Fact]
    public void EverySpeciesPlacedIsOfItsRegionsBatchAndEveryPlacedRegionHasItsWholeBatch()
    {
        var all = EveryOverlaysOurs().ToList();
        foreach (var (region, key, ours) in all)
            foreach (var slot in (ours.Land ?? new()).Concat(ours.Surf ?? new()).Concat(ours.SuperRod ?? new()))
            {
                var species = PokemonDatabase.Get(slot.Species);
                Assert.True(species != null && OursSlots.Placeable(species), $"{region}/{key}: {slot.Species}");
                Assert.True(string.Equals(OursSlots.RegionOf(species!), region, StringComparison.OrdinalIgnoreCase), $"{region}/{key}: {slot.Species} is of {OursSlots.RegionOf(species!)}'s batch");
            }
        foreach (string region in Placed)
        {
            var placed = all.Where(o => string.Equals(o.Region, region, StringComparison.OrdinalIgnoreCase))
                .SelectMany(o => (o.Ours.Land ?? new()).Concat(o.Ours.Surf ?? new()).Concat(o.Ours.SuperRod ?? new())).Select(s => s.Species).ToHashSet();
            var missing = PokemonDatabase.Ours().Where(OursSlots.Placeable).Where(s => OursSlots.RegionOf(s) == region && !placed.Contains(s.Name)).Select(s => s.Name).ToList();
            Assert.True(missing.Count == 0, $"{region}'s batch has no place for {string.Join(", ", missing)}");
        }
    }
}
