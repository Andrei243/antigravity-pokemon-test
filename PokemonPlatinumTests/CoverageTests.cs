using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataImporter;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>
/// The catalogue of plan 06 · R1: that everything the plan counts is in the data, that the report of how much of
/// it runs (docs/mechanics/coverage.md) is the one the data and the engine give today, and that it never gets
/// worse. Also the importer's reader of Pokémon Showdown's tables, on small texts written here.
/// </summary>
public class CoverageTests
{
    private static List<MoveData> Moves => MoveDatabase.GetAll().OrderBy(m => m.Id).ToList();
    private static List<ItemData> Items => ItemDatabase.GetAll().OrderBy(i => i.Id).ToList();
    private static List<AbilityDatabase.AbilityRecord> Abilities => GameDataFiles.Load<List<AbilityDatabase.AbilityRecord>>(AbilityDatabase.FileName);

    private static string Repo()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("The tests don't run from inside the repository");
    }

    // ------------------------------------------------------------------ the report

    [Fact]
    public void TheReportIsTheOneTheDataAndTheEngineGive()
    {
        // Whoever gives an ability or an item its effect, or changes the data, runs the importer, which rewrites
        // the report: dotnet run --project tools/DataImporter
        string report = Coverage.Report(PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList(), Moves, Abilities, Items);
        string onDisk = File.ReadAllText(Coverage.ReportPath(Repo()));
        Assert.True(report.ReplaceLineEndings("\n") == onDisk.ReplaceLineEndings("\n"),
            "docs/mechanics/coverage.md is out of date: run the data importer (dotnet run --project tools/DataImporter)");
    }

    [Fact]
    public void CoverageNeverGetsWorse()
    {
        var n = Coverage.Tally(Moves, Abilities, Items);
        var fallen = new List<string>();
        void AtLeast(int floor, int now, string what)
        {
            if (now < floor) fallen.Add($"{what}: {now}, was {floor}");
        }

        // The floors of 2026-10-05 (plan 06 · R2). A session that writes rules raises them to what the report
        // then says; nothing ever lowers one.
        AtLeast(467, n.PlatinumMovesFully, "Platinum's moves fully run");
        AtLeast(467, n.PlatinumMovesFully + n.PlatinumMovesPartly, "Platinum's moves that at least hit");
        AtLeast(119, n.LaterMovesFully, "later moves fully run");
        AtLeast(291, n.LaterMovesFully + n.LaterMovesPartly, "later moves that at least hit");
        // All 123 since plan 06 · R7
        AtLeast(123, n.PlatinumAbilitiesRun, "Platinum's abilities with an effect");
        AtLeast(0, n.LaterAbilitiesRun, "later abilities with an effect");
        AtLeast(302, n.PlatinumItemsFully, "Platinum's items that work");
        AtLeast(355, n.PlatinumItemsFully + n.PlatinumItemsPartly, "Platinum's items that work at least partly");
        AtLeast(158, n.HoldEffectsRun, "hold effects run");
        AtLeast(0, n.SpecialMovesRun, "Z-Moves and Max Moves run");

        Assert.True(fallen.Count == 0, "The coverage report got worse:\n" + string.Join("\n", fallen));
    }

    // ------------------------------------------------------------------ what the catalogue holds

    [Fact]
    public void EveryMoveThePlanCountsIsInTheData()
    {
        var n = Coverage.Tally(Moves, Abilities, Items);
        Assert.Equal(467, n.PlatinumMoves);
        Assert.Equal(380, n.LaterMoves);
        // 35 Z-Moves (one for each of the 18 types, 17 of single species), 19 Max Moves (18 types and Max Guard)
        // and the 33 G-Max Moves: with the 847 ordinary moves, every move of the main games
        Assert.Equal((35, 19, 33), (n.ZMoves, n.MaxMoves, n.GMaxMoves));
        Assert.Equal(934, Moves.Count);
        Assert.Equal(Moves.Count, Moves.Select(m => m.Name).Distinct().Count());

        var special = Moves.Where(m => m.Kind != MoveKind.Standard).ToList();
        Assert.All(special, m =>
        {
            Assert.Equal(MoveEffectSupport.None, m.Support);
            Assert.NotNull(m.Effect);
            Assert.True(m.Description.Length > 0, m.Name);
        });
        Assert.Equal(18, special.Count(m => m.Kind == MoveKind.ZMove && Items.Any(i => i.ZCrystal?.Type == m.Type) && !Items.Any(i => i.ZCrystal?.Move == m.Name)));
        Assert.Equal(33, special.Where(m => m.Kind == MoveKind.GMaxMove).Select(m => m.GigantamaxOf).Distinct().Count());
        Assert.All(special.Where(m => m.Kind == MoveKind.GMaxMove), m => Assert.True(m.Id >= Importer.FirstGMaxMoveId, m.Name));

        // No Pokémon learns one of them
        var learned = PokemonDatabase.GetAll().SelectMany(s => s.Learnset).Select(l => l.MoveName).ToHashSet();
        Assert.DoesNotContain(special, m => learned.Contains(m.Name));
    }

    [Fact]
    public void EveryDamagingMoveHasItsPowerAsAZMoveAndAsAMaxMove()
    {
        var standard = Moves.Where(m => m.Kind == MoveKind.Standard).ToList();
        var missing = standard.Where(m => m.Name != "Struggle" && (m.Modern?.Category ?? m.Category) != MoveCategory.Status && (m.ZPower == 0 || m.MaxPower == 0)).ToList();
        Assert.True(missing.Count == 0, "No Z-Move or Max Move power: " + string.Join(", ", missing.Select(m => m.Name)));
        Assert.All(standard.Where(m => m.ZBonus != null), m => Assert.Equal(MoveCategory.Status, m.Modern?.Category ?? m.Category));
        Assert.Equal(0, MoveDatabase.Get("Struggle").ZPower);

        (int, int) Powers(string move) => (MoveDatabase.Get(move).ZPower, MoveDatabase.Get(move).MaxPower);
        // By the rule: from the newest games' power (Tackle is 40 there), with Fighting and Poison moves lower as Max Moves
        Assert.Equal((100, 90), Powers("Tackle"));
        Assert.Equal((175, 130), Powers("Thunderbolt"));
        Assert.Equal((180, 130), Powers("Earthquake"));
        Assert.Equal((190, 95), Powers("Close Combat"));
        Assert.Equal((200, 150), Powers("Hyper Beam"));
        // A move that hits several times counts three of its hits: Bullet Seed's 25 is 75
        Assert.Equal(140, MoveDatabase.Get("Bullet Seed").ZPower);
        // Moves whose power isn't a number have powers of their own
        Assert.Equal((100, 75), Powers("Seismic Toss"));
        Assert.Equal((160, 130), Powers("Gyro Ball"));

        // Z-Power on status moves: an effect, or stats raised
        Assert.Equal("ClearNegativeBoost", MoveDatabase.Get("Swords Dance").ZBonus!.Effect);
        Assert.Equal("Heal", MoveDatabase.Get("Belly Drum").ZBonus!.Effect);
        var splash = MoveDatabase.Get("Splash").ZBonus!;
        Assert.Equal(new[] { StatType.Attack }, splash.Stats);
        Assert.Equal(3, splash.Stages);
    }

    [Theory]
    [InlineData(0, false, 100)]
    [InlineData(40, false, 100)]
    [InlineData(60, false, 120)]
    [InlineData(75, false, 140)]
    [InlineData(85, false, 160)]
    [InlineData(95, false, 175)]
    [InlineData(100, false, 180)]
    [InlineData(110, false, 185)]
    [InlineData(120, false, 190)]
    [InlineData(130, false, 195)]
    [InlineData(250, false, 200)]
    [InlineData(25, true, 140)]
    public void AMovesZPowerFollowsItsPower(int power, bool hitsSeveralTimes, int expected) =>
        Assert.Equal(expected, Importer.ZPowerOf(power, hitsSeveralTimes));

    [Theory]
    [InlineData(0, "Normal", 100)]
    [InlineData(40, "Normal", 90)]
    [InlineData(50, "Water", 100)]
    [InlineData(60, "Fire", 110)]
    [InlineData(70, "Grass", 120)]
    [InlineData(90, "Electric", 130)]
    [InlineData(120, "Ice", 140)]
    [InlineData(150, "Normal", 150)]
    [InlineData(40, "Fighting", 70)]
    [InlineData(50, "Poison", 75)]
    [InlineData(60, "Fighting", 80)]
    [InlineData(70, "Poison", 85)]
    [InlineData(90, "Fighting", 90)]
    [InlineData(120, "Fighting", 95)]
    [InlineData(150, "Poison", 100)]
    public void AMovesMaxPowerFollowsItsPowerAndType(int power, string type, int expected) =>
        Assert.Equal(expected, Importer.MaxPowerOf(power, type));

    [Fact]
    public void MegaStonesAndZCrystalsKnowWhoseTheyAre()
    {
        var stones = Items.Where(i => i.MegaStone != null).ToList();
        var crystals = Items.Where(i => i.ZCrystal != null).ToList();
        Assert.True(stones.Count >= 48, $"{stones.Count} Mega Stones");
        Assert.Equal(35, crystals.Count);

        var venusaurite = ItemDatabase.Get("Venusaurite")!.MegaStone!;
        Assert.Equal(("Venusaur", "Venusaur-Mega"), (venusaurite.Species, venusaurite.Form));
        Assert.Equal("Charizard-Mega-X", ItemDatabase.Get("Charizardite X")!.MegaStone!.Form);
        Assert.Equal("Charizard-Mega-Y", ItemDatabase.Get("Charizardite Y")!.MegaStone!.Form);
        Assert.All(stones, i =>
        {
            Assert.True(PokemonDatabase.Get(i.MegaStone!.Species) != null, $"{i.Name} is for {i.MegaStone.Species}, which is no species");
            Assert.StartsWith(i.MegaStone.Species, i.MegaStone.Form);
            Assert.Null(i.ZCrystal);
        });
        Assert.Equal(stones.Count, stones.Select(i => i.MegaStone!.Form).Distinct().Count());

        // A crystal for each type, and each of the others turns one species' move into a Z-Move the data has
        Assert.Equal(PokemonType.Normal, ItemDatabase.Get("Normalium Z")!.ZCrystal!.Type);
        Assert.Equal(18, crystals.Where(i => i.ZCrystal!.Type != null).Select(i => i.ZCrystal!.Type).Distinct().Count());
        var pikanium = ItemDatabase.Get("Pikanium Z")!.ZCrystal!;
        Assert.Equal(("Catastropika", "Volt Tackle", "Pikachu"), (pikanium.Move, pikanium.From, pikanium.Users!.Single()));
        Assert.All(crystals.Where(i => i.ZCrystal!.Type == null), i =>
        {
            var z = i.ZCrystal!;
            Assert.Equal(MoveKind.ZMove, MoveDatabase.GetAll().Single(m => m.Name == z.Move).Kind);
            Assert.Equal(MoveKind.Standard, MoveDatabase.GetAll().Single(m => m.Name == z.From).Kind);
            Assert.NotEmpty(z.Users!);
            // A species, or one of its forms (Kommo-o-Totem)
            Assert.All(z.Users!, user => Assert.True(PokemonDatabase.GetAll().Any(s => user == s.Name || user.StartsWith(s.Name + "-")),
                $"{i.Name} is for {user}, which is no species"));
        });

        // Every Z-Move is made by exactly one crystal
        Assert.All(Moves.Where(m => m.Kind == MoveKind.ZMove), m =>
            Assert.Single(crystals, i => i.ZCrystal!.Move == m.Name || (i.ZCrystal.Type == m.Type && !crystals.Any(c => c.ZCrystal!.Move == m.Name))));

        Assert.Equal(Items.Count, Items.Select(i => i.Id).Distinct().Count());
        Assert.Equal(Items.Count, Items.Select(i => i.Name).Distinct().Count());
    }

    [Fact]
    public void PlatinumsItemTableIsWhole()
    {
        var own = Items.Where(Coverage.IsPlatinums).ToList();
        // The decompilation has 446 item files; one of them is "no item"
        Assert.Equal(445, own.Count);
        Assert.Equal(100, own.Count(i => i.FieldUse == "TmHm"));
        Assert.Equal(64, own.Count(i => i.Pocket == ItemPocket.Berries));

        var potion = ItemDatabase.Get("Potion")!;
        Assert.Equal(("Healing", "Healing", 20, 30), (potion.FieldUse, potion.BattleUse, potion.Use!["hpRestored"], potion.FlingPower));
        Assert.Equal(Works.Fully, Coverage.Of(potion));

        // A berry: what it does held, thrown, plucked and as Natural Gift
        var sitrus = ItemDatabase.Get("Sitrus Berry")!;
        Assert.Equal(("HpPctRestore", 25, 10, "HpPctRestore", "HpPctRestore"), (sitrus.HoldEffect, sitrus.HoldParam, sitrus.FlingPower, sitrus.FlingEffect, sitrus.PluckEffect));
        Assert.Equal((60, PokemonType.Psychic), (sitrus.NaturalGiftPower, sitrus.NaturalGiftType));
        Assert.All(own.Where(i => i.Pocket == ItemPocket.Berries), i => Assert.True(i.NaturalGiftPower > 0 && i.NaturalGiftType != null, i.Name));

        Assert.Equal(20, ItemDatabase.Get("Charcoal")!.HoldParam);
        Assert.Equal(10, ItemDatabase.Get("Ether")!.Use!["ppRestored"]);
        // Platinum's Rare Candy also brings a fainted Pokémon back
        Assert.Equal((1, 1), (ItemDatabase.Get("Rare Candy")!.Use!["levelUp"], ItemDatabase.Get("Rare Candy")!.Use!["revive"]));

        var bicycle = ItemDatabase.Get("Bicycle")!;
        Assert.True(bicycle.CantBeTossed && bicycle.CanBeRegistered);
        Assert.Equal("Bicycle", bicycle.FieldUse);
        Assert.All(own.Where(i => i.Pocket == ItemPocket.KeyItems), i => Assert.True(i.CantBeTossed, i.Name));

        // What works is judged job by job
        Assert.Equal(Works.Fully, Coverage.Of(ItemDatabase.Get("Net Ball")!));       // catches by its own condition
        Assert.Equal(Works.Partly, Coverage.Of(ItemDatabase.Get("Luxury Ball")!));   // catches, without the friendship after
        Assert.Equal(Works.Fully, Coverage.Of(ItemDatabase.Get("Full Restore")!));   // from the bag and in battle (plan 06 · R8)
        Assert.Equal(Works.Fully, Coverage.Of(ItemDatabase.Get("X Attack")!));       // in battle, which is all it is for
        Assert.Equal(Works.Fully, Coverage.Of(ItemDatabase.Get("Poké Doll")!));
        Assert.Equal(Works.Partly, Coverage.Of(ItemDatabase.Get("Occa Berry")!));    // held; planted in soil is R14's
        Assert.Equal(Works.Fully, Coverage.Of(ItemDatabase.Get("Macho Brace")!));    // its Speed in battle (the EVs it doubles are R10's)
        Assert.Equal(200, ItemDatabase.Get("Hyper Potion")!.EffectValue);            // the table's signed byte read right
        Assert.Equal(200, ItemDatabase.Get("Hyper Potion")!.Use!["hpRestored"]);
        Assert.Equal(Works.Fully, Coverage.Of(ItemDatabase.Get("TM01")!));          // teaches from the bag (plan 06 · R11)
        Assert.Equal(Works.Fully, Coverage.Of(ItemDatabase.Get("Ether")!));         // in battle and from the bag (R11)
        Assert.Equal(Works.Fully, Coverage.Of(ItemDatabase.Get("Repel")!));         // keeps weaker Pokémon away (R11)
        Assert.Equal(Works.Partly, Coverage.Of(ItemDatabase.Get("Oran Berry")!));   // eaten and held; planted is R14's
        Assert.Equal(Works.Fully, Coverage.Of(ItemDatabase.Get("Leftovers")!));
        Assert.Equal(Works.NothingToRun, Coverage.Of(ItemDatabase.Get("Nugget")!));
    }

    // ------------------------------------------------------------------ reading Pokémon Showdown's tables

    [Fact]
    public void ShowdownsTablesAreReadEntryByEntry()
    {
        const string text = """
            export const Moves: import('../sim/dex-moves').MoveDataTable = {
            	"10000000voltthunderbolt": {
            		num: 719,
            		accuracy: true,
            		basePower: 195,
            		category: "Special",
            		isNonstandard: "Past",
            		name: "10,000,000 Volt Thunderbolt",
            		pp: 1,
            		priority: 0,
            		flags: {},
            		isZ: "pikashuniumz",
            		critRatio: 3,
            		secondary: null,
            		target: "normal",
            		type: "Electric",
            	},
            	bulletseed: {
            		num: 331,
            		basePower: 25,
            		category: "Physical",
            		name: "Bullet Seed",
            		flags: { protect: 1, mirror: 1, metronome: 1, bullet: 1 },
            		multihit: [2, 5],
            		onHit(target) {
            			this.add('-anim', target, "Bullet Seed");
            		},
            		zMove: { basePower: 140 },
            		maxMove: { basePower: 130 },
            		type: "Grass",
            	},
            	gmaxwildfire: {
            		num: 1000,
            		isNonstandard: "Gmax", // G-Max moves are not hackable
            		name: "G-Max Wildfire",
            		isMax: "Charizard",
            	},
            	geomancy: {
            		name: "Geomancy",
            		zMove: { boost: { atk: 1, def: 1, spa: 1, spd: 1, spe: 1 } },
            	},
            	swordsdance: {
            		name: "Swords Dance",
            		zMove: { effect: 'clearnegativeboost' },
            	},
            };
            """;
        var entries = Showdown.Parse(text.Replace("\r", "").Split('\n'));

        Assert.Equal(new[] { "10000000voltthunderbolt", "bulletseed", "gmaxwildfire", "geomancy", "swordsdance" }, entries.Select(e => e.Id));
        var volt = entries[0];
        Assert.Equal(("10,000,000 Volt Thunderbolt", 719, 195, "pikashuniumz"), (volt.Name, volt.Number("num"), volt.Number("basePower"), volt.Text("isZ")));
        Assert.True(volt.Is("accuracy", "true"));

        // A function over several lines is passed over; the fields after it are still read
        var seed = entries[1];
        Assert.True(seed.Fields["multihit"].StartsWith('['));
        Assert.Equal((140, 130, "Grass"), (Showdown.Inner(seed.Fields["zMove"], "basePower"), Showdown.Inner(seed.Fields["maxMove"], "basePower"), seed.Text("type")));
        Assert.False(seed.Has("add"));

        // A comment after a value is not part of it
        Assert.Equal(("Gmax", "Charizard"), (entries[2].Text("isNonstandard"), entries[2].Text("isMax")));

        Assert.Equal(new[] { ("atk", 1), ("def", 1), ("spa", 1), ("spd", 1), ("spe", 1) }, Showdown.InnerTable(entries[3].Fields["zMove"], "boost"));
        Assert.Equal("clearnegativeboost", Showdown.InnerText(entries[4].Fields["zMove"], "effect"));
        Assert.Empty(Showdown.InnerTable(entries[4].Fields["zMove"], "boost"));

        Assert.Equal(new[] { ("Charizard", "Charizard-Mega-X") }, Showdown.Pairs("{ \"Charizard\": \"Charizard-Mega-X\" }"));
        Assert.Equal(new[] { "Pikachu", "Raichu" }, Showdown.List("[\"Pikachu\", \"Raichu\"]"));
    }
}
