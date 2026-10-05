using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>The game data files in <c>Data/</c>: they load, refer to each other correctly and keep their values.</summary>
[Collection("MapDatabase")]
public class DataFileTests
{
    [Fact]
    public void TestSpeciesLoadFromFile()
    {
        // The whole National Pokédex, numbered 1–1025 without gaps
        var all = PokemonDatabase.GetAll().ToList();
        Assert.Equal(1025, all.Count);
        Assert.Equal(Enumerable.Range(1, 1025), all.Select(s => s.DexNumber).Order());
        Assert.Equal(all.Count, all.Select(s => s.Name).Distinct().Count());

        // Platinum's own data for Turtwig
        var turtwig = PokemonDatabase.GetByDex(387)!;
        Assert.Equal("Turtwig", turtwig.Name);
        Assert.Equal("Tiny Leaf", turtwig.Category);
        Assert.Equal(4, turtwig.Generation);
        Assert.Equal(PokemonType.Grass, turtwig.PrimaryType);
        Assert.Null(turtwig.SecondaryType);
        Assert.Equal((55, 68, 64, 45, 55, 31),
            (turtwig.BaseHP, turtwig.BaseAttack, turtwig.BaseDefense, turtwig.BaseSpAttack, turtwig.BaseSpDefense, turtwig.BaseSpeed));
        Assert.Equal(1, turtwig.EvYield!.Attack);
        Assert.Equal((45, 64, GrowthRate.MediumSlow, 1), (turtwig.CatchRate, turtwig.BaseExpYield, turtwig.GrowthRate, turtwig.GenderRatio));
        Assert.Equal(new[] { "Monster", "Grass" }, turtwig.EggGroups);
        Assert.Equal(0.4f, turtwig.Height);
        Assert.Equal(new[] { "Overgrow" }, turtwig.Abilities);
        Assert.Equal((5, "Withdraw"), (turtwig.Learnset[1].Level, turtwig.Learnset[1].MoveName));
        Assert.Equal((45, "Leaf Storm"), (turtwig.Learnset[^1].Level, turtwig.Learnset[^1].MoveName));
        Assert.Equal((18, "Grotle"), (turtwig.LevelEvolution!.Level, turtwig.LevelEvolution.TargetSpecies));
        Assert.Null(PokemonDatabase.Get("Torterra")!.Evolutions);
        Assert.Equal(PokemonType.Ground, PokemonDatabase.Get("Torterra")!.SecondaryType);

        // Platinum's typings stay (Clefairy is Normal); the Fairy type belongs to later species (plan 03, decision 2)
        Assert.Equal(PokemonType.Normal, PokemonDatabase.Get("Clefairy")!.PrimaryType);
        Assert.DoesNotContain(all.Where(s => s.DexNumber <= 493), s => s.PrimaryType == PokemonType.Fairy || s.SecondaryType == PokemonType.Fairy);
        Assert.Equal(PokemonType.Fairy, PokemonDatabase.Get("Sylveon")!.PrimaryType);

        // Later generations from PokeAPI
        var sprigatito = PokemonDatabase.GetByDex(906)!;
        Assert.Equal(("Sprigatito", 9, PokemonType.Grass), (sprigatito.Name, sprigatito.Generation, sprigatito.PrimaryType));
        Assert.Equal((16, "Floragato"), (sprigatito.LevelEvolution!.Level, sprigatito.LevelEvolution.TargetSpecies));
        Assert.Equal(-1, PokemonDatabase.Get("Magnemite")!.GenderRatio);
        Assert.Equal(8, PokemonDatabase.Get("Nidoran♀")!.GenderRatio);
        Assert.Equal("Farfetch'd", PokemonDatabase.GetByDex(83)!.Name);

        // Every species can be met: an ability, moves to use and its own Pokédex text (ours, generated from the data)
        Assert.All(all, s =>
        {
            Assert.NotEmpty(s.Abilities);
            Assert.NotEmpty(s.Learnset);
            Assert.StartsWith(s.Name + " is ", s.DexEntry);
        });
        Assert.Contains("evolves into Grotle", turtwig.DexEntry);
    }

    [Fact]
    public void TestTheSinnohPokedexIsPlatinums()
    {
        // 210 regional numbers without gaps, all of them species of Platinum's own time
        var sinnoh = PokemonDatabase.GetAll().Where(s => s.SinnohNumber != null).ToList();
        Assert.Equal(Enumerable.Range(1, 210), sinnoh.Select(s => s.SinnohNumber!.Value).Order());
        Assert.All(sinnoh, s => Assert.InRange(s.DexNumber, 1, 493));
        Assert.Equal(1, PokemonDatabase.Get("Turtwig")!.SinnohNumber);
        Assert.Equal(4, PokemonDatabase.Get("Chimchar")!.SinnohNumber);
        Assert.Equal(7, PokemonDatabase.Get("Piplup")!.SinnohNumber);
        Assert.Equal(210, PokemonDatabase.Get("Giratina")!.SinnohNumber);
        // Species that came back in Platinum's Pokédex keep their place in it; the mythical ones have none
        Assert.Equal(PokemonDatabase.Get("Absol")!.SinnohNumber, 209);
        Assert.Null(PokemonDatabase.Get("Arceus")!.SinnohNumber);
        Assert.Null(PokemonDatabase.Get("Bulbasaur")!.SinnohNumber);

        // Legendary and mythical species
        var all = PokemonDatabase.GetAll().ToList();
        Assert.Equal(23, all.Count(s => s.Mythical));
        Assert.True(PokemonDatabase.Get("Arceus")!.Mythical);
        Assert.True(PokemonDatabase.Get("Mew")!.Mythical);
        Assert.True(PokemonDatabase.Get("Giratina")!.Legendary);
        Assert.False(PokemonDatabase.Get("Giratina")!.Mythical);
        Assert.DoesNotContain(all, s => s.Legendary && s.Mythical);
    }

    [Fact]
    public void TestEvolutionsKeepTheirMethods()
    {
        var eevee = PokemonDatabase.Get("Eevee")!;
        Assert.Equal(8, eevee.Evolutions!.Count);
        Assert.Null(eevee.LevelEvolution);
        Assert.Contains(eevee.Evolutions, e => e is { Method: EvolutionMethod.UseItem, Item: "Fire Stone", TargetSpecies: "Flareon" });
        Assert.Contains(eevee.Evolutions, e => e is { Method: EvolutionMethod.LevelAtLocation, Location: "Moss Rock", TargetSpecies: "Leafeon" });
        Assert.Contains(eevee.Evolutions, e => e is { Method: EvolutionMethod.LevelKnowsMoveType, Type: PokemonType.Fairy, TargetSpecies: "Sylveon" });

        Assert.Equal(EvolutionMethod.FriendshipDay, PokemonDatabase.Get("Riolu")!.Evolutions!.Single().Method);
        Assert.Equal(("Mantine", "Remoraid"), (PokemonDatabase.Get("Mantyke")!.Evolutions!.Single().TargetSpecies, PokemonDatabase.Get("Mantyke")!.Evolutions!.Single().Species));
        Assert.Equal("Ancient Power", PokemonDatabase.Get("Piloswine")!.Evolutions!.Single(e => e.TargetSpecies == "Mamoswine").Move);

        // Platinum species that gained an evolution later, and the ones that start from a regional form, which say so
        Assert.Contains(PokemonDatabase.Get("Scyther")!.Evolutions!, e => e.TargetSpecies == "Kleavor");
        Assert.Equal("Meowth-Galar", PokemonDatabase.Get("Meowth")!.Evolutions!.Single(e => e.TargetSpecies == "Perrserker").FromForm);

        // Every evolution goes somewhere that exists, with items and moves that exist
        foreach (var s in PokemonDatabase.GetAll())
        {
            foreach (var e in s.Evolutions ?? new())
            {
                Assert.NotNull(PokemonDatabase.Get(e.TargetSpecies));
                if (e.Item != null) Assert.True(ItemDatabase.Get(e.Item) != null, $"{s.Name}: {e.Item}");
                if (e.Move != null) Assert.Equal(e.Move, MoveDatabase.Get(e.Move).Name);
            }
        }
    }

    [Fact]
    public void TestMovesLoadFromFile()
    {
        // Platinum's 467 moves (Struggle included) and the later ones; the Z-Moves and Max Moves are in the data
        // too, as moves of a kind of their own that nothing learns (CoverageTests counts them)
        var all = MoveDatabase.GetAll().ToList();
        Assert.Equal(847, all.Count(m => m.Kind == MoveKind.Standard));
        Assert.Equal(467, all.Count(m => m.Id <= 467));
        Assert.Equal(all.Count, all.Select(m => m.Id).Distinct().Count());
        Assert.DoesNotContain(all, m => m.Kind == MoveKind.Standard && m.Name is "Breakneck Blitz" or "Max Strike" or "Catastropika");

        // Platinum's values: Tackle was 35 power and 95% accurate in Generation 4
        var tackle = MoveDatabase.Get("Tackle");
        Assert.Equal((33, 1, PokemonType.Normal, MoveCategory.Physical, 35, 95, 35), (tackle.Id, tackle.Generation, tackle.Type, tackle.Category, tackle.Power, tackle.Accuracy, tackle.MaxPP));
        Assert.Equal(MoveEffectSupport.Full, tackle.Support);
        Assert.Null(tackle.Effect);

        var quickAttack = MoveDatabase.Get("Quick Attack");
        Assert.Equal((40, 100, 30, 1), (quickAttack.Power, quickAttack.Accuracy, quickAttack.MaxPP, quickAttack.Priority));

        var ember = MoveDatabase.Get("Ember");
        Assert.Equal((StatusCondition.Burn, 10), (ember.InflictStatus, ember.StatusChancePercent));
        Assert.Null(ember.TargetStatChange);

        var withdraw = MoveDatabase.Get("Withdraw");
        Assert.Equal(MoveCategory.Status, withdraw.Category);
        Assert.Equal((StatType.Defense, 1, true, 100),
            (withdraw.TargetStatChange, withdraw.StatStageAmount, withdraw.StatChangeTargetSelf, withdraw.StatChangeChancePercent));

        // Battle details: targets, flags and extra effects
        Assert.Equal(MoveTarget.AllOthers, MoveDatabase.Get("Earthquake").Target);
        Assert.False(MoveDatabase.Get("Earthquake").MakesContact);
        Assert.True(MoveDatabase.Get("Mach Punch").Flags.HasFlag(MoveFlags.Contact | MoveFlags.Punch));
        Assert.True(MoveDatabase.Get("Growl").Flags.HasFlag(MoveFlags.Sound));
        Assert.False(MoveDatabase.Get("Razor Leaf").MakesContact);
        Assert.Equal(new[] { StatType.SpDefense }, MoveDatabase.Get("Close Combat").AlsoChangesStats);
        Assert.Equal(30, MoveDatabase.Get("Bite").FlinchChancePercent);
        Assert.Equal((10, 10), (MoveDatabase.Get("Fire Fang").StatusChancePercent, MoveDatabase.Get("Fire Fang").FlinchChancePercent));
        Assert.Equal(PokemonType.Ghost, MoveDatabase.Get("Curse").Type); // "???" in Platinum

        // Later moves take their side effects from PokeAPI
        var moonblast = MoveDatabase.Get("Moonblast");
        Assert.Equal((PokemonType.Fairy, StatType.SpAttack, -1, 30), (moonblast.Type, moonblast.TargetStatChange, moonblast.StatStageAmount, moonblast.StatChangeChancePercent));
        Assert.Equal(75, MoveDatabase.Get("Draining Kiss").DrainPercent);

        // Unknown moves still fall back to Tackle
        Assert.Equal("Tackle", MoveDatabase.Get("No Such Move").Name);
    }

    [Fact]
    public void TestMovesTheEngineCantRunYetAreFlagged()
    {
        foreach (var m in MoveDatabase.GetAll())
        {
            // Anything not fully run names the effect still to write; a move that is fully run names one only
            // when the engine has code under that name
            if (m.Support != MoveEffectSupport.Full) Assert.True(m.Effect != null, m.Name);
            else if (m.Effect != null) Assert.True(BattleCore.HasMoveEffect(m.Effect), m.Name);
            // A move that does nothing yet carries no half-working side effects
            if (m.Support == MoveEffectSupport.None)
            {
                Assert.Equal(StatusCondition.None, m.InflictStatus);
                Assert.Null(m.TargetStatChange);
                Assert.Equal(0, m.HealPercent + m.ConfuseChancePercent + m.DrainPercent + m.RecoilPercent);
            }
            // Only Partial moves hit
            if (m.Support == MoveEffectSupport.Partial) Assert.True(m.Category != MoveCategory.Status || m.HealPercent > 0 || m.TargetStatChange != null, m.Name);
        }

        // How much runs may only grow (docs/mechanics/coverage.md has the details)
        Assert.True(MoveDatabase.GetAll().Count(m => m.Support == MoveEffectSupport.Full) >= 583);
        Assert.True(AbilityDatabase.GetAll().Count(a => a.IsImplemented) >= 95);

        Assert.Equal(("MultiHit", MoveEffectSupport.Full), (MoveDatabase.Get("Fury Attack").Effect, MoveDatabase.Get("Fury Attack").Support));
        Assert.Equal(("Protect", MoveEffectSupport.Full), (MoveDatabase.Get("Protect").Effect, MoveDatabase.Get("Protect").Support));
        Assert.Equal(("Protect", MoveEffectSupport.Full), (MoveDatabase.Get("Detect").Effect, MoveDatabase.Get("Detect").Support));
        // A move of a fixed amount or a power of its own carries none in its data; the engine's code for its effect is what runs it
        Assert.Equal((0, "LevelDamageFlat", MoveEffectSupport.Full), (MoveDatabase.Get("Seismic Toss").Power, MoveDatabase.Get("Seismic Toss").Effect, MoveDatabase.Get("Seismic Toss").Support));
        Assert.Equal(MoveEffectSupport.Full, MoveDatabase.Get("Fake Out").Support);
        // Every one of Platinum's 467 moves runs in full since plan 06 · R6; what is still to write is among the later games' moves
        Assert.All(MoveDatabase.GetAll().Where(m => m.Id <= 467), m => Assert.Equal(MoveEffectSupport.Full, m.Support));
        Assert.Contains(MoveDatabase.GetAll(), m => m.Id > 467 && m.Support == MoveEffectSupport.None);
        Assert.Equal((50, "HealHalfMoreInSun"), (MoveDatabase.Get("Synthesis").HealPercent, MoveDatabase.Get("Synthesis").Effect));
    }

    [Fact]
    public void TestAbilitiesLoadFromFile()
    {
        var all = AbilityDatabase.GetAll().ToList();
        Assert.Equal(313, all.Count);
        Assert.All(all, a => Assert.NotEmpty(a.Description));

        // Every ability with battle code is one the data knows
        var names = all.Select(a => a.Name).ToHashSet();
        Assert.All(AbilityEffectTable.Names, n => Assert.Contains(n, names));
        Assert.True(AbilityDatabase.Get("Intimidate")!.IsImplemented);
        Assert.False(AbilityDatabase.Get("Protean")!.IsImplemented);
    }

    [Fact]
    public void TestItemsLoadFromFile()
    {
        var all = ItemDatabase.GetAll().ToList();
        Assert.Equal(all.Count, all.Select(i => i.Id).Distinct().Count());
        Assert.True(all.Count(i => i.Id < 1000) > 400, "Platinum's items");

        // Every item with battle code is in the data, and has a hold effect
        foreach (string name in HeldItemEffects.Names)
        {
            var item = ItemDatabase.Get(name);
            Assert.True(item != null, name);
            Assert.True(item!.HoldEffect != null, name);
        }
        Assert.Equal(ItemPocket.Berries, ItemDatabase.Get("Sitrus Berry")!.Pocket);
        Assert.Equal("HpRestoreGradual", ItemDatabase.Get("Leftovers")!.HoldEffect);
        Assert.Equal("Focus Punch", ItemDatabase.Get("TM01")!.TeachesMove);

        var masterBall = ItemDatabase.Get("Master Ball")!;
        Assert.Equal((ItemPocket.PokeBalls, ItemEffectType.CatchPokemon, 9999, 0),
            (masterBall.Pocket, masterBall.EffectType, masterBall.EffectValue, masterBall.Price));
        Assert.True(masterBall.CanUseInBattle);
        Assert.False(masterBall.CanUseInOverworld);

        var potion = ItemDatabase.Get("Potion")!;
        Assert.Equal((ItemEffectType.HealHP, 20, 300), (potion.EffectType, potion.EffectValue, potion.Price));
        Assert.Equal((ItemEffectType.HealStatus, StatusCondition.Poison), (ItemDatabase.Get("Antidote")!.EffectType, ItemDatabase.Get("Antidote")!.HealsStatus));
        Assert.Equal(ItemEffectType.Revive, ItemDatabase.Get("Revive")!.EffectType);
        Assert.Equal(ItemEffectType.LevelUp, ItemDatabase.Get("Rare Candy")!.EffectType);

        var townMap = ItemDatabase.Get("Town Map")!;
        Assert.Equal(ItemPocket.KeyItems, townMap.Pocket);
        Assert.False(townMap.CanUseInBattle);
        Assert.Equal(ItemPocket.KeyItems, ItemDatabase.Get("Running Shoes")!.Pocket);

        // Later games' items, for battles and evolutions
        Assert.NotNull(ItemDatabase.Get("Assault Vest"));
        Assert.NotNull(ItemDatabase.Get("Auspicious Armor"));
    }

    [Fact]
    public void TestEveryMachineIsDescribedByTheMoveItTeaches()
    {
        var all = ItemDatabase.GetAll().ToList();
        var machines = all.Where(i => i.TeachesMove != null).ToList();
        Assert.Equal(100, machines.Count); // Platinum's 92 TMs and 8 HMs

        // Any move's name standing as words of its own; the longest first, so Flash Cannon isn't read as Flash
        var names = MoveDatabase.GetAll().Select(m => m.Name).OrderByDescending(n => n.Length).Select(Regex.Escape);
        var anyMove = new Regex(@"(?<![\p{L}\p{N}])(?:" + string.Join("|", names) + @")(?![\p{L}\p{N}])");
        // "(Gen IV & III: Focus Punch Gen I: Mega Punch)", "(HS: Whirlpool DPP: Defog)": what a machine taught in other games
        var otherGames = new Regex(@"\bGen [IVX]+\b|\b(?:HS|DPP)\b");

        var problems = new List<string>();
        foreach (var item in machines)
        {
            var move = MoveDatabase.Get(item.TeachesMove!);
            Assert.Equal(item.TeachesMove, move.Name); // an unknown name would have given Tackle

            // The move's own text may name another move (Earthquake reaches a Pokémon using Dig); the machine's may not
            string own = move.Description.Length > 0 ? item.Description.Replace(move.Description, "") : item.Description;
            var named = anyMove.Matches(own).Select(m => m.Value).Distinct().ToList();
            if (named.Count != 1 || named[0] != move.Name)
                problems.Add($"{item.Name} teaches {move.Name}, but its text names {(named.Count == 0 ? "no move" : string.Join(", ", named))}: {item.Description}");
            if (otherGames.IsMatch(item.Description))
                problems.Add($"{item.Name} tells of other games: {item.Description}");
        }

        // Nothing else passes for a machine
        problems.AddRange(all.Where(i => i.TeachesMove == null && i.Description.Contains("Teaches "))
            .Select(i => $"{i.Name} teaches no move, but its text says: {i.Description}"));
        Assert.True(problems.Count == 0, string.Join("\n", problems));

        // Platinum's moves, where later games put others: TM01 became Hone Claws and HM05 Waterfall in Generation 5
        Assert.StartsWith("Teaches Focus Punch to a compatible Pokémon.", ItemDatabase.Get("TM01")!.Description);
        Assert.StartsWith("Teaches Defog to a compatible Pokémon.", ItemDatabase.Get("HM05")!.Description);
    }

    [Fact]
    public void TestEveryNameInTheDataRefersToSomethingThatExists()
    {
        var moveNames = MoveDatabase.GetAll().Select(m => m.Name).ToHashSet();
        var missing = PokemonDatabase.GetAll().SelectMany(s => s.Learnset).Select(l => l.MoveName)
            .Where(n => !moveNames.Contains(n)).Distinct().OrderBy(n => n).ToArray();
        Assert.Empty(missing);

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
        Assert.Equal(14, names.Count); // 10 hand-made and 2 from the imported world in Sinnoh, 2 in Kanto
        Assert.Contains("Sinnoh", names);
        Assert.Contains("PlayerHouse", names);

        var city = MapDatabase.Get("JubilifeCity");
        Assert.Equal(("Jubilife City", 40, 34), (city.DisplayName, city.Width, city.Height));
        Assert.False(city.IsStreamed);
        Assert.Null(city.AreaAt(5, 5));
        Assert.Equal("Jubilife City", city.DisplayNameAt(5, 5));

        var tristan = MapDatabase.Get("Sinnoh").NPCs.Single(n => n.Id == "trainer_tristan");
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
    public void TestEveryWarpLandsOnOpenGround()
    {
        MapDatabase.Initialize();
        foreach (string name in MapDatabase.MapNames)
        {
            var map = MapDatabase.Get(name);
            foreach (var warp in map.Warps)
            {
                // Furniture, fences and lamp posts must never stand where somebody arrives
                Assert.Contains(warp.TargetMap, MapDatabase.MapNames);
                var target = MapDatabase.Get(warp.TargetMap);
                Assert.True(target.InBounds(warp.TargetX, warp.TargetY) && !target.IsSolid(warp.TargetX, warp.TargetY),
                    $"{name}: the warp at {warp.SourceX},{warp.SourceY} lands on a blocked tile of {warp.TargetMap} ({warp.TargetX},{warp.TargetY})");
            }
        }
    }

    [Fact]
    public void TestMapsReloadWithFreshTrainers()
    {
        MapDatabase.Initialize();
        var first = MapDatabase.Get("Sinnoh").NPCs.Single(n => n.Id == "trainer_tristan");
        MapDatabase.Initialize();
        var second = MapDatabase.Get("Sinnoh").NPCs.Single(n => n.Id == "trainer_tristan");
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
