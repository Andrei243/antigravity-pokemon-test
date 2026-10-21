using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 08 · P23: the species of a game in the console's second slot (the original's dual slot,
/// <c>WildEncounters_ReplaceDualSlotEncounters</c>): the five games' two species in an area file, in the area, and in
/// slots 8 and 9 of its grass once the National Pokédex is had and a game is chosen. Platinum's tables are what they were
/// with no game in the slot.
/// </summary>
public class DualSlotTests
{
    private sealed class Rolls(int then, params int[] rolls) : Random
    {
        private readonly Queue<int> left = new(rolls);
        public override int Next(int maxValue) => Math.Min(left.Count > 0 ? left.Dequeue() : then, Math.Max(0, maxValue - 1));
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
        public override int Next() => Next(int.MaxValue);
        public override long NextInt64(long maxValue) => Next((int)Math.Min(int.MaxValue, maxValue));
    }

    private static readonly Lazy<World> Sinnoh = new(() => World.LoadAll().Single(w => w.Index.Region == "Sinnoh"));

    private static MapArea AreaOf(string key) => MapDatabase.Get(Sinnoh.Value.MapOf(key)!.Name).FindArea(key)!;

    private static readonly SlotTwoGame[] Games = { SlotTwoGame.Ruby, SlotTwoGame.Sapphire, SlotTwoGame.Emerald, SlotTwoGame.FireRed, SlotTwoGame.LeafGreen };

    [Fact]
    public void TheFiveListsGoThroughAnAreaFileAndBack()
    {
        var area = new WorldAreaFile
        {
            Key = "route_201",
            DualSlot = new DualSlotTables
            {
                Ruby = new() { "Starly", "Bidoof" }, Sapphire = new() { "Kricketot", "Bidoof" }, Emerald = new() { "Shinx", "Shinx" },
                FireRed = new() { "Growlithe", "Growlithe" }, LeafGreen = new() { "Abra", "Bidoof" }
            }
        };
        string json = GameDataFiles.Serialize(area);
        var back = GameDataFiles.Deserialize<WorldAreaFile>(json)!;
        Assert.Equal(json, GameDataFiles.Serialize(back));
        Assert.Contains("\"fireRed\"", json);
        Assert.Contains("\"leafGreen\"", json);
        Assert.Equal(new[] { "Growlithe", "Growlithe" }, back.DualSlot!.Of(SlotTwoGame.FireRed));
        Assert.Equal(new[] { "Shinx", "Shinx" }, back.DualSlot.Of(SlotTwoGame.Emerald));
        Assert.Null(back.DualSlot.Of(SlotTwoGame.None));
        // An area with none writes none
        Assert.DoesNotContain("dualSlot", GameDataFiles.Serialize(new WorldAreaFile { Key = "x" }));
    }

    [Fact]
    public void EveryImportedAreaWithGrassHasTheFiveListsOfTwoAndNoneWithoutHasAny()
    {
        var world = Sinnoh.Value;
        foreach (string key in world.Index.Areas)
        {
            var file = world.Area(key)!;
            if (file.Land == null) Assert.Null(file.DualSlot);
            else foreach (var game in Games) Assert.True(file.DualSlot?.Of(game)?.Count == 2, $"{key} {game}");
        }
        // encounters_route_201.json: firered GROWLITHE, GROWLITHE and the rest Platinum's own
        var route = world.Area("route_201")!;
        Assert.Equal(new[] { "Growlithe", "Growlithe" }, route.DualSlot!.FireRed);
        Assert.Equal(new[] { "Starly", "Bidoof" }, route.DualSlot.LeafGreen);
    }

    [Fact]
    public void TheListsReachTheAreaOfTheMap()
    {
        var area = AreaOf("route_201");
        Assert.Equal(new[] { "Growlithe", "Growlithe" }, area.DualSlot!.Of(SlotTwoGame.FireRed));
        Assert.Equal(new[] { "Starly", "Bidoof" }, area.DualSlot.Of(SlotTwoGame.Ruby));
        // A place with no grass has none
        Assert.Null(AreaOf("twinleaf_town").DualSlot);
    }

    [Fact]
    public void WithTheNationalPokedexAndFireRedSlotsEightAndNineAreTheGamesAtTheirOwnLevelsAndWeights()
    {
        var area = AreaOf("route_201");
        var none = EncounterSlots.Grass(area, new EncounterMoment { NationalDex = true });
        var fire = EncounterSlots.Grass(area, new EncounterMoment { NationalDex = true, SlotTwo = SlotTwoGame.FireRed });

        Assert.Equal(new[] { "Starly", "Bidoof" }, none.Skip(8).Take(2).Select(e => e.SpeciesName));
        Assert.Equal(new[] { "Growlithe", "Growlithe" }, fire.Skip(8).Take(2).Select(e => e.SpeciesName));
        // Nothing else moves: the other ten slots, and every slot's level and weight
        for (int i = 0; i < 12; i++)
        {
            if (i is not (8 or 9)) Assert.Equal(none[i].SpeciesName, fire[i].SpeciesName);
            Assert.Equal(none[i].MinLevel, fire[i].MinLevel);
            Assert.Equal(none[i].Weight, fire[i].Weight);
        }
        // ... and the area's own table is untouched
        Assert.Equal("Starly", area.WildEncounters[8].SpeciesName);
    }

    [Fact]
    public void WithoutTheNationalPokedexOrWithNoGameNothingChanges()
    {
        var area = AreaOf("route_201");
        var plain = EncounterSlots.Grass(area, new EncounterMoment()).Select(e => e.SpeciesName).ToArray();
        Assert.Equal(plain, EncounterSlots.Grass(area, new EncounterMoment { SlotTwo = SlotTwoGame.FireRed }).Select(e => e.SpeciesName));
        Assert.Equal(plain, EncounterSlots.Grass(area, new EncounterMoment { NationalDex = true, SlotTwo = SlotTwoGame.None }).Select(e => e.SpeciesName));
        // Without a moment at all the area's own table is what is met
        Assert.Equal(plain, EncounterSlots.Grass(area, null).Select(e => e.SpeciesName));
    }

    [Fact]
    public void ARollOfSlotEightMeetsAGrowlitheWithFireRedAndAStarlyWithNone()
    {
        // Slots 8 and 9 are met 4 in a hundred each: the rolls 90 to 93 are slot 8 (WildEncounters' weights 20 20 10 10 10 10 5 5 4 4 1 1)
        var area = AreaOf("route_201");
        var map = MapDatabase.Get(Sinnoh.Value.MapOf("route_201")!.Name);
        var fire = new EncounterMoment { NationalDex = true, SlotTwo = SlotTwoGame.FireRed };
        var none = new EncounterMoment { NationalDex = true };

        var withGame = WildEncounterRules.Slot(EncounterSlots.Grass(area, fire), false, null, new Rolls(0, 90));
        var without = WildEncounterRules.Slot(EncounterSlots.Grass(area, none), false, null, new Rolls(0, 90));
        Assert.Equal("Growlithe", withGame.SpeciesName);
        Assert.Equal("Starly", without.SpeciesName);
        Assert.Equal(area.WildEncounters[8].MinLevel, withGame.MinLevel);
        Assert.NotNull(map);

        // Every other slot meets what it met: the first slot, say
        Assert.Equal("Starly", WildEncounterRules.Slot(EncounterSlots.Grass(area, fire), false, null, new Rolls(0, 0)).SpeciesName);
    }

    [Fact]
    public void SweetScentAndTheStepsDrawFromTheSameTable()
    {
        // The Sweet Scent's table is the moment's too (Map.DrawOutWild asks WildAt with it), as a step's is
        var imported = AreaOf("route_201");
        var area = new MapArea { Key = imported.Key, Open = true, LandRate = imported.LandRate, DualSlot = imported.DualSlot };
        foreach (var e in imported.WildEncounters) area.WildEncounters.Add(new WildEncounterEntry { SpeciesName = e.SpeciesName, MinLevel = e.MinLevel, MaxLevel = e.MaxLevel, Weight = e.Weight });
        var map = new Map(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++) map.SetGroundTile(x, y, TileType.TallGrass);
        map.SetArea(0, 0, area);

        var fire = new EncounterMoment { NationalDex = true, SlotTwo = SlotTwoGame.FireRed };
        var table = map.WildAt(5, 5, moment: fire).Table;
        Assert.Equal("Growlithe", table[8].SpeciesName);
        Assert.Equal("Growlithe", table[9].SpeciesName);

        var scent = new HashSet<string>();
        for (int i = 0; i < 4000; i++)
            if (map.DrawOutWild(5, 5, moment: fire) is { } wild) scent.Add(wild.SpeciesName);
        Assert.Contains("Growlithe", scent);
        var steps = new EncounterSteps();
        var met = new HashSet<string>();
        for (int i = 0; i < 6000; i++)
            if (map.RollWildEncounter(5, 5, steps, moment: fire) is { } wild) met.Add(wild.SpeciesName);
        Assert.Contains("Growlithe", met);
    }

    [Fact]
    public void AnAreaWithNoListForTheGameKeepsItsTable()
    {
        var area = new MapArea { Key = "somewhere", Open = true };
        for (int i = 0; i < 12; i++) area.WildEncounters.Add(new WildEncounterEntry { SpeciesName = "Starly", MinLevel = 3, MaxLevel = 3, Weight = 1 });
        area.DualSlot = new DualSlotTables { Ruby = new() { "Growlithe", "Growlithe" } };
        var moment = new EncounterMoment { NationalDex = true, SlotTwo = SlotTwoGame.Emerald };
        Assert.All(EncounterSlots.Grass(area, moment), e => Assert.Equal("Starly", e.SpeciesName));
        moment = moment with { SlotTwo = SlotTwoGame.Ruby };
        Assert.Equal("Growlithe", EncounterSlots.Grass(area, moment)[8].SpeciesName);
    }

    [Fact]
    public void ADualSlotGameTakesThePlaceOfOurNationalSpeciesInTheSameSlots()
    {
        // Eterna Forest has our own species in slots 8 and 9 with the National Pokédex (plan 03 · D12a); a game in the slot takes them
        var area = AreaOf("eterna_forest");
        var national = EncounterSlots.Grass(area, new EncounterMoment { NationalDex = true });
        Assert.Equal(new[] { "Murkrow", "Misdreavus" }, national.Skip(8).Take(2).Select(e => e.SpeciesName));
        var game = EncounterSlots.Grass(area, new EncounterMoment { NationalDex = true, SlotTwo = SlotTwoGame.FireRed });
        Assert.Equal(area.DualSlot!.FireRed, game.Skip(8).Take(2).Select(e => e.SpeciesName));
    }

    [Fact]
    public void TheRowCyclesThroughTheSixValuesAndRoundAgain()
    {
                var settings = new GameSettings();
        Assert.Equal(SlotTwoGame.None, settings.SlotTwo);
        var seen = new List<SlotTwoGame> { settings.SlotTwo };
        for (int i = 0; i < 6; i++)
        {
            OptionsScreen.Change(settings, OptionRow.SlotTwo, 1);
            seen.Add(settings.SlotTwo);
        }
        Assert.Equal(new[] { SlotTwoGame.None, SlotTwoGame.Ruby, SlotTwoGame.Sapphire, SlotTwoGame.Emerald, SlotTwoGame.FireRed, SlotTwoGame.LeafGreen, SlotTwoGame.None }, seen);
    }

    [Fact]
    public void TheSettingIsKeptInTheSettingsFileAsItsName()
    {
        var settings = new GameSettings { SlotTwo = SlotTwoGame.LeafGreen };
        string folder = Path.Combine(Path.GetTempPath(), "slot-two-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string path = Path.Combine(folder, GameSettings.FileName);
            File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(settings, new System.Text.Json.JsonSerializerOptions { Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } }));
            Assert.Contains("\"LeafGreen\"", File.ReadAllText(path));
            Assert.Equal(SlotTwoGame.LeafGreen, GameSettings.Load(path).SlotTwo);
        }
        finally
        {
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
