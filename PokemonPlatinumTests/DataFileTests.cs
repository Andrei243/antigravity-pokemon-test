using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>The game data files in <c>Data/</c>: they load, refer to each other correctly and keep their values.</summary>
public class DataFileTests
{
    [Fact]
    public void TestSpeciesLoadFromFile()
    {
        var all = PokemonDatabase.GetAll().ToList();
        Assert.Equal(23, all.Count);
        Assert.Equal(all.Count, all.Select(s => s.DexNumber).Distinct().Count());

        var turtwig = PokemonDatabase.GetByDex(387)!;
        Assert.Equal("Turtwig", turtwig.Name);
        Assert.Equal("Tiny Leaf", turtwig.Category);
        Assert.Equal(PokemonType.Grass, turtwig.PrimaryType);
        Assert.Null(turtwig.SecondaryType);
        Assert.Equal((55, 68, 64, 45, 55, 31),
            (turtwig.BaseHP, turtwig.BaseAttack, turtwig.BaseDefense, turtwig.BaseSpAttack, turtwig.BaseSpDefense, turtwig.BaseSpeed));
        Assert.Equal(GrowthRate.MediumSlow, turtwig.GrowthRate);
        Assert.Equal(0.4f, turtwig.Height);
        Assert.Equal(6, turtwig.Learnset.Count);
        Assert.Equal((5, "Withdraw"), (turtwig.Learnset[1].Level, turtwig.Learnset[1].MoveName));
        Assert.Equal((18, "Grotle"), (turtwig.Evolution!.Level, turtwig.Evolution.TargetSpecies));

        var torterra = PokemonDatabase.Get("Torterra")!;
        Assert.Equal(PokemonType.Ground, torterra.SecondaryType);
        Assert.Null(torterra.Evolution);

        Assert.Contains("Pokémon", PokemonDatabase.Get("Torterra")!.DexEntry);
    }

    [Fact]
    public void TestMovesLoadFromFile()
    {
        var all = MoveDatabase.GetAll().ToList();
        Assert.Equal(45, all.Count);
        Assert.Equal(all.Count, all.Select(m => m.Id).Distinct().Count());

        var quickAttack = MoveDatabase.Get("Quick Attack");
        Assert.Equal((PokemonType.Normal, MoveCategory.Physical, 40, 100, 30, 1),
            (quickAttack.Type, quickAttack.Category, quickAttack.Power, quickAttack.Accuracy, quickAttack.MaxPP, quickAttack.Priority));

        var ember = MoveDatabase.Get("Ember");
        Assert.Equal((StatusCondition.Burn, 10), (ember.InflictStatus, ember.StatusChancePercent));
        Assert.Null(ember.TargetStatChange);

        var withdraw = MoveDatabase.Get("Withdraw");
        Assert.Equal(MoveCategory.Status, withdraw.Category);
        Assert.Equal((StatType.Defense, 1, true, 100),
            (withdraw.TargetStatChange, withdraw.StatStageAmount, withdraw.StatChangeTargetSelf, withdraw.StatChangeChancePercent));

        // Unknown moves still fall back to Tackle
        Assert.Equal("Tackle", MoveDatabase.Get("No Such Move").Name);
    }

    [Fact]
    public void TestItemsLoadFromFile()
    {
        var all = ItemDatabase.GetAll().ToList();
        Assert.Equal(18, all.Count);
        Assert.Equal(all.Count, all.Select(i => i.Id).Distinct().Count());

        var masterBall = ItemDatabase.Get("Master Ball")!;
        Assert.Equal((ItemPocket.PokeBalls, ItemEffectType.CatchPokemon, 9999, 0),
            (masterBall.Pocket, masterBall.EffectType, masterBall.EffectValue, masterBall.Price));
        Assert.True(masterBall.CanUseInBattle);
        Assert.False(masterBall.CanUseInOverworld);

        var townMap = ItemDatabase.Get("Town Map")!;
        Assert.Equal(ItemPocket.KeyItems, townMap.Pocket);
        Assert.False(townMap.CanUseInBattle);
    }

    [Fact]
    public void TestEveryNameInTheDataRefersToSomethingThatExists()
    {
        // Learnsets name three moves that aren't written yet; MoveDatabase gives Tackle in their place. This list may only shrink.
        var notYetWritten = new[] { "Aqua Jet", "Headbutt", "Synthesis" };
        var moveNames = MoveDatabase.GetAll().Select(m => m.Name).ToHashSet();
        var missing = PokemonDatabase.GetAll().SelectMany(s => s.Learnset).Select(l => l.MoveName)
            .Where(n => !moveNames.Contains(n)).Distinct().OrderBy(n => n).ToArray();
        Assert.Equal(notYetWritten, missing);

        foreach (var species in PokemonDatabase.GetAll())
        {
            if (species.Evolution != null)
                Assert.NotNull(PokemonDatabase.Get(species.Evolution.TargetSpecies));
        }

        foreach (var file in MapFiles())
        {
            foreach (var e in file.WildEncounters)
                Assert.True(PokemonDatabase.Get(e.SpeciesName) != null, $"{file.Name}: unknown wild species {e.SpeciesName}");
            foreach (var t in file.Npcs.Where(n => n.Trainer != null).Select(n => n.Trainer!))
                Assert.All(t.Party, m => Assert.NotNull(PokemonDatabase.Get(m.Species)));
        }
    }

    [Fact]
    public void TestEveryMapFileLoads()
    {
        MapDatabase.Initialize();
        var names = MapDatabase.MapNames.ToList();
        Assert.Equal(10, names.Count);
        Assert.Contains("TwinleafTown", names);
        Assert.Contains("PlayerHouse", names);

        var route201 = MapDatabase.Get("Route201");
        Assert.Equal(("Route 201", 36, 22), (route201.DisplayName, route201.Width, route201.Height));
        Assert.Equal(TileType.TallGrass, route201.GetGroundTile(3, 3));
        Assert.Equal(TileType.LedgeDown, route201.GetGroundTile(18, 12));
        Assert.Equal(3, route201.WildEncounters.Count);

        var tristan = route201.NPCs.Single(n => n.Id == "trainer_tristan");
        Assert.True(tristan.IsTrainer);
        Assert.Equal("Starly", tristan.TrainerData!.Party.Members.Single().Species.Name);
        Assert.Equal(4, tristan.TrainerData.Party.Members[0].Level);
        Assert.NotEmpty(tristan.TrainerData.Party.Members[0].Moves);

        // Furniture blocks the tiles under it, decoration doesn't
        var home = MapDatabase.Get("PlayerHouse");
        Assert.Equal(InteriorStyle.House, home.Interior);
        Assert.Contains(home.Props, p => p.Type == PropType.Fridge && p.X == 1 && p.Y == 2);
        Assert.False(home.IsWalkable(1, 2));
        Assert.True(home.IsWalkable(3, 6)); // on the rug
    }

    [Fact]
    public void TestMapsReloadWithFreshTrainers()
    {
        MapDatabase.Initialize();
        var first = MapDatabase.Get("Route201").NPCs.Single(n => n.Id == "trainer_tristan");
        MapDatabase.Initialize();
        var second = MapDatabase.Get("Route201").NPCs.Single(n => n.Id == "trainer_tristan");
        Assert.NotSame(first, second);
        Assert.NotSame(first.TrainerData!.Party, second.TrainerData!.Party);
    }

    [Fact]
    public void TestMapFilesRoundTripWithoutLosingAnything()
    {
        string folder = GameDataFiles.PathOf(MapDatabase.Folder);
        foreach (string path in Directory.GetFiles(folder, "*.json"))
        {
            string text = File.ReadAllText(path).Replace("\r\n", "\n");
            var file = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, Path.GetFileName(path)));
            Assert.Equal(text, GameDataFiles.Serialize(MapFile.FromMap(file.ToMap())));
        }
    }

    [Fact]
    public void TestEveryTileTypeHasAUniqueCode()
    {
        var codes = System.Enum.GetValues<TileType>().Select(TileCodes.CodeOf).ToList();
        Assert.Equal(codes.Count, codes.Distinct().Count());
        Assert.DoesNotContain(TileCodes.None, codes);
        foreach (var type in System.Enum.GetValues<TileType>())
            Assert.Equal(type, TileCodes.Parse(TileCodes.CodeOf(type), "test"));
    }

    [Fact]
    public void TestBrokenMapFilesAreRejectedWithAClearError()
    {
        MapFile Tiny() => new()
        {
            Name = "Tiny",
            Width = 3,
            Height = 2,
            Ground = new() { "T.T", "::D" },
            Solid = new() { "#.#", "..." }
        };

        var map = Tiny().ToMap();
        Assert.Equal(TileType.Door, map.GetGroundTile(2, 1));
        Assert.True(map.IsSolid(0, 0));
        Assert.False(map.IsSolid(1, 0));

        var shortRow = Tiny();
        shortRow.Ground[1] = "::";
        Assert.Contains("ground row 1", Assert.Throws<InvalidDataException>(() => shortRow.ToMap()).Message);

        var missingRow = Tiny();
        missingRow.Solid.RemoveAt(1);
        Assert.Contains("solid has 1 rows", Assert.Throws<InvalidDataException>(() => missingRow.ToMap()).Message);

        var unknownTile = Tiny();
        unknownTile.Ground[0] = "T?T";
        Assert.Contains("unknown tile code '?'", Assert.Throws<InvalidDataException>(() => unknownTile.ToMap()).Message);

        var unknownSpecies = Tiny();
        unknownSpecies.Npcs.Add(new MapFile.NpcRecord
        {
            Name = "Nobody",
            Trainer = new MapFile.TrainerRecord { Id = "t", Party = new() { new MapFile.PartyMember { Species = "Missingno", Level = 5 } } }
        });
        Assert.Contains("Missingno", Assert.Throws<InvalidDataException>(() => unknownSpecies.ToMap()).Message);
    }

    [Fact]
    public void TestMissingDataFileNamesThePath()
    {
        var e = Assert.Throws<FileNotFoundException>(() => GameDataFiles.Load<List<MoveData>>("does-not-exist.json"));
        Assert.Contains("does-not-exist.json", e.Message);
    }

    private static IEnumerable<MapFile> MapFiles() =>
        Directory.GetFiles(GameDataFiles.PathOf(MapDatabase.Folder), "*.json")
            .Select(p => GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, Path.GetFileName(p))));
}
