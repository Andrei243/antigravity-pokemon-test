using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE11: every item the rules run can be had (docs/mechanics/rulings.md, "Every item the rules run can be had";
/// docs/mechanics/where-items-are-found.md). The Mega Stones, the Z-Crystals, Genesect's drives, the Gems, the key items
/// that change a form, every item an evolution asks for and the later mechanics' key items are each held to a way
/// <see cref="ItemSources.Everywhere"/> finds (a sure way, anywhere in the game: a team carried between regions brings its
/// bag; a wild Pokémon's held item and the Underground's walls are chance, which plan 36 · TR3's sources leave out) or to
/// an owed line naming the plan and session that place it. A session that places
/// an owed item takes its line off (the test fails while an owed item can be had); a region's plan that places its stones,
/// crystals and form items runs this beside its species check (<c>SpeciesSources.Of</c>).
/// </summary>
public class ItemSourcesTests
{
    // ------------------------------------------------------------------ what the rules run

    /// <summary>The later mechanics' key items, which the battle asks the bag for by name.</summary>
    private static readonly string[] LaterKeys =
        { BattleCore.KeyStoneItem, BattleCore.ZRingItem, BattleCore.DynamaxBandItem, BattleCore.MegaMaxBandItem, BattleCore.TeraOrbItem };

    /// <summary>Every item the rules run, each with its kind, in the data's order.</summary>
    public static IReadOnlyList<(string Kind, ItemData Item)> RulesRun()
    {
        var asked = new HashSet<string>(StringComparer.Ordinal);
        foreach (var species in PokemonDatabase.Everyone())
            foreach (var e in species.Evolutions ?? new())
                if (!string.IsNullOrEmpty(e.Item)) asked.Add(e.Item);
        var all = new List<(string, ItemData)>();
        foreach (var item in ItemDatabase.GetAll().OrderBy(i => i.Id))
        {
            string? kind = item.MegaStone != null ? "a Mega Stone"
                : item.ZCrystal != null ? "a Z-Crystal"
                : item.HoldEffect?.StartsWith("Drive", StringComparison.Ordinal) == true ? "one of Genesect's drives"
                : item.HoldEffect?.StartsWith("Gem", StringComparison.Ordinal) == true ? "a Gem"
                : item.FieldUse == "FormChange" ? "a key item that changes a form"
                : LaterKeys.Contains(item.Name) ? "a later mechanic's key item"
                : asked.Contains(item.Name) ? "an item an evolution asks for"
                : null;
            if (kind != null) all.Add((kind, item));
        }
        return all;
    }

    // ------------------------------------------------------------------ who owes the rest

    /// <summary>A session named in an owed line: <c>plan 23 · Z4</c>, <c>plan 40 · GE7</c>, <c>plan 29 · LS11c</c>.</summary>
    public static readonly Regex Session = new(@"\bplan \d+ · [A-Z]+\d+[a-z]?\b", RegexOptions.CultureInvariant);

    /// <summary>An owed list from its owners, each with the items it owes.</summary>
    private static Dictionary<string, string> Owing(params (string Owner, string[] Items)[] owners)
    {
        var owed = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (owner, items) in owners)
            foreach (string item in items) owed.Add(item, owner);
        return owed;
    }

    private const string KalosStory = "plan 23 · Z4: Kalos's story, the stones of X and Y that Z4b's post-game (its fourteen, the Absolite and the Mewtwonites) left unplaced";
    private const string WithDiancie = "plan 29 · LS11: Kalos's mythicals by events of ours (KalosSpeciesTests owes Diancie so); its stone with it";
    private const string WithHoopa = "plan 29 · LS11: Kalos's mythicals by events of ours (KalosSpeciesTests owes Hoopa so); its bottle with it";
    private const string Lumiose = "plan 23 · Z22: Lumiose's era, its Wild Zones, Rogue Megas and shops (Legends: Z-A's Mega Stones)";
    private const string MegaDimension = "plan 23 · Z24 (optional): the Mega Dimension (Legends: Z-A's own)";
    private const string Hisui = "plan 23 · Z20 (optional): Hisui, which owes Kleavor and Ursaluna (NationalSpeciesTests)";
    // (Galar's professor gives the Dynamax Band to a bag without one since plan 33 · GL1: the Wishing Stars at her lake)
    private const string GalarCafes = "plan 33 · GL4: the cafés whose battles give the Sweets (or GL5's, the study's finding)";
    private const string Galar = "plan 33 · GL7: Galar's last session, after its Hall of Fame";
    private const string GalarStones = "plan 33 · GL7: Galar's Hall of Fame, its professor's gift once the bag holds a Key Stone (decisions/R20b-mega-proposals.md)";
    private const string GalarBand = "plan 33 · GL7: after Galar's Hall of Fame, the band made of a Key Stone and a Dynamax Band (decisions/R20c-mega-dynamax-proposals.md, 3.2)";
    private const string IsleOfArmor = "plan 33 · GL8 (optional): the Isle of Armor";
    private const string CrownTundra = "plan 33 · GL9 (optional): the Crown Tundra";
    private const string Academy = "plan 34 · PD1: Paldea's academy, the Tera Orb to a bag without one";
    private const string Provinces = "plan 34 · PD3: the provinces' chests and roaming Gimmighoul (and PD6's)";
    private const string Paldea = "plan 34 · PD9: Paldea's last session, after its Hall of Fame";
    private const string PaldeaStones = "plan 34 · PD9: Paldea's Hall of Fame, its professor's gift once the bag holds a Key Stone (decisions/R20b-mega-proposals.md)";
    private const string TealMask = "plan 34 · PD10 (optional): the Teal Mask";
    private const string IndigoDisk = "plan 34 · PD11 (optional): the Indigo Disk";
    private const string Events = "plan 40 · GE7: the gift box; no game gave it but by an event";

    /// <summary>
    /// What no region of the game gives yet by a sure way, and who owes it. Each line names a plan and a session; the
    /// session that places the item takes the line off.
    /// </summary>
    public static readonly Dictionary<string, string> Owed = Owing(
        // X's and Y's Mega Stones that Kalos's post-game (plan 23 · Z4b) didn't place (Omega Ruby's and Alpha Sapphire's are
        // Hoenn's: decision 11; the Lucarionite is the Tower of Mastery's Lucario's, the Blazikenite Hoenn's with its two
        // fellow starters')
        (KalosStory, new[] { "Gengarite", "Aerodactylite", "Ampharosite", "Gardevoirite", "Abomasite" }),
        (WithDiancie, new[] { "Diancite" }),
        (WithHoopa, new[] { "Prison Bottle" }),
        // Legends: Z-A's Mega Stones, the base game's and the Mega Dimension's
        (Lumiose, new[]
        {
            "Clefablite", "Victreebelite", "Starminite", "Dragoninite", "Meganiumite", "Feraligite", "Skarmorite", "Froslassite",
            "Emboarite", "Excadrite", "Scolipite", "Scraftinite", "Eelektrossite", "Chandelurite", "Chesnaughtite", "Delphoxite",
            "Greninjite", "Pyroarite", "Floettite", "Malamarite", "Barbaracite", "Dragalgite", "Hawluchanite", "Zygardite",
            "Drampanite", "Falinksite"
        }),
        (MegaDimension, new[]
        {
            "Heatranite", "Darkranite", "Zeraorite", "Raichunite X", "Raichunite Y", "Chimechite", "Absolite Z", "Staraptite",
            "Garchompite Z", "Lucarionite Z", "Golurkite", "Crabominite", "Golisopite", "Scovillainite", "Baxcalibrite", "Glimmoranite"
        }),
        // (Alola owes nothing since plan 23's last Alola session: the partners' crystals and stones are Kukui's after the Hall
        // of Fame, the species' crystals Heahea's carver's, the sun's and the moon's the Altar keeper's, Necrozma's and its two
        // machines Veyra's, the Marshadium Z Marshadow's, the Meltan Candy the old Megamart's Meltan's)
        (Events, new[] { "Pikashunium Z", "Fairy Gem" }),
        // What the evolutions of our own species ask for that only chance gives (plan 36 · TR3's sources count no chance):
        // Volcamel's, Gastromire's, Behemowdon's and Furnapede's rocks, Ossugard's club
        // Hisui
        (Hisui, new[] { "Black Augurite", "Peat Block" }),
        // Galar, with the Isle of Armor and the Crown Tundra
        (GalarCafes, new[] { "Strawberry Sweet", "Love Sweet", "Berry Sweet", "Clover Sweet", "Flower Sweet", "Star Sweet", "Ribbon Sweet" }),
        (Galar, new[] { "Sweet Apple", "Tart Apple", "Cracked Pot", "Chipped Pot", "Rotom Catalog" }),
        (GalarBand, new[] { BattleCore.MegaMaxBandItem }),
        (GalarStones, new[] { "Rillaboomite", "Cinderacite", "Inteleonite" }),
        (IsleOfArmor, new[] { "Galarica Cuff", "Galarica Wreath", "Scroll of Darkness", "Scroll of Waters" }),
        (CrownTundra, new[] { "Reins of Unity" }),
        // Paldea, with the Teal Mask and the Indigo Disk
        (Academy, new[] { BattleCore.TeraOrbItem }),
        (Provinces, new[] { "Gimmighoul Coin" }),
        (Paldea, new[] { "Malicious Armor", "Auspicious Armor", "Punching Glove" }),
        (PaldeaStones, new[] { "Meowscaradite", "Skeledirgite", "Quaquavalite" }),
        (TealMask, new[] { "Syrupy Apple", "Unremarkable Teacup", "Masterpiece Teacup" }),
        (IndigoDisk, new[] { "Metal Alloy" }));

    /// <summary>What the rules run that nothing gives and nobody owes, and what is owed that something gives already.</summary>
    public static (List<string> Unaccounted, List<string> Stale) Gaps(ItemSources sources, IEnumerable<string> items, IReadOnlyDictionary<string, string> owed)
    {
        var list = items.ToList();
        return (list.Where(i => sources.Of(i).Count == 0 && !owed.ContainsKey(i)).ToList(),
            owed.Keys.Where(i => sources.Of(i).Count > 0).ToList());
    }

    [Fact]
    public void EveryItemTheRulesRunCanBeHadOrIsOwedByAPlansSession()
    {
        var sources = ItemSources.Everywhere;
        var run = RulesRun();
        // The kinds the plan names are all here, the numbers of the data's
        Assert.Equal(104, run.Count(r => r.Kind == "a Mega Stone"));
        Assert.Equal(35, run.Count(r => r.Kind == "a Z-Crystal"));
        Assert.Equal(4, run.Count(r => r.Kind == "one of Genesect's drives"));
        Assert.Equal(18, run.Count(r => r.Kind == "a Gem"));
        Assert.Equal(8, run.Count(r => r.Kind == "a key item that changes a form"));
        Assert.Equal(5, run.Count(r => r.Kind == "a later mechanic's key item"));

        var (unaccounted, stale) = Gaps(sources, run.Select(r => r.Item.Name), Owed);
        Assert.True(unaccounted.Count == 0, "nothing gives, and nobody owes: " +
            string.Join(", ", unaccounted.Select(i => $"{i} ({run.First(r => r.Item.Name == i).Kind})")));
        Assert.True(stale.Count == 0, "owed, and to be had already (take its line off): " +
            string.Join("; ", stale.Select(i => $"{i} ({Owed[i]}): {sources.Of(i).First().Where}")));

        // Every owed line is an item the rules run and names a plan's session
        Assert.All(Owed, owed =>
        {
            Assert.Contains(run, r => r.Item.Name == owed.Key);
            Assert.Matches(Session, owed.Value);
        });
    }

    [Fact]
    public void TheCheckCatchesASourceTakenAwayAndAnOwedLineThatIsHad()
    {
        // A world of the test's own: one script of a Sinnoh room that gives the Burn Drive
        ItemSources Giving(params string[] items) => ItemSources.From(Array.Empty<World>(), Array.Empty<MapFile>(),
            ScriptLibrary.FromSources(("RowanLab", "script Gift\n" + string.Concat(items.Select(i => $"  give \"{i}\"\n")) + "  end\n")).All);
        var owed = new Dictionary<string, string> { ["Chill Drive"] = "plan 23 · Z4: a test's" };
        string[] items = { "Burn Drive", "Chill Drive" };

        // Given and owed: nothing amiss
        var (unaccounted, stale) = Gaps(Giving("Burn Drive"), items, owed);
        Assert.Empty(unaccounted);
        Assert.Empty(stale);
        Assert.Contains(Giving("Burn Drive").Of("Burn Drive"), s => s.Kind == ItemSourceKind.Given && s.Where == "RowanLab.Gift");
        // The source taken away: unaccounted
        (unaccounted, _) = Gaps(Giving(), items, owed);
        Assert.Equal(new[] { "Burn Drive" }, unaccounted);
        // The owed one given: stale
        (_, stale) = Gaps(Giving("Burn Drive", "Chill Drive"), items, owed);
        Assert.Equal(new[] { "Chill Drive" }, stale);
    }

    [Fact]
    public void TheNewKindsOfSourceFindWhatTheyGive()
    {
        // A Pokémon handed over holding an item (the Tower of Mastery's Lucario, Steven's Beldum)
        Assert.True(ItemSources.In(RegionDatabase.Kalos).Has("Lucarionite", ItemSourceKind.Held));
        Assert.True(ItemSources.In(RegionDatabase.Hoenn).Has("Metagrossite", ItemSourceKind.Held));
        Assert.Contains(ItemSources.In(RegionDatabase.Hoenn).Of("Metagrossite"), s => s.Kind == ItemSourceKind.Held && s.Where.Contains("Beldum", StringComparison.Ordinal));
        // What a trade sends (FireRed's Farfetch'd comes with its Leek)
        Assert.True(ItemSources.In(RegionDatabase.Kanto).Has("Leek", ItemSourceKind.Traded));
        // ... and no way by chance, as plan 36 · TR3 ruled: a Pokémon met holding something (Southern Island's eon Pokémon
        // and their Soul Dew) must be caught first
        Assert.DoesNotContain(ItemSources.Everywhere.Of("Soul Dew"), s => s.Kind == ItemSourceKind.Held);
    }

    // ------------------------------------------------------------------ playing the givers

    private static readonly Dictionary<string, Lazy<Dictionary<string, Map>>> Built = RegionDatabase.All
        .Where(r => r.File != null)
        .ToDictionary(r => r.Id, r => new Lazy<Dictionary<string, Map>>(() =>
            (World.LoadAll().FirstOrDefault(w => string.Equals(w.Index.Region, r.Id, StringComparison.OrdinalIgnoreCase))?.BuildMaps() ?? new List<Map>())
                .ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase)), StringComparer.OrdinalIgnoreCase);

    private static World WorldOf(string region) =>
        World.LoadAll().Single(w => string.Equals(w.Index.Region, region, StringComparison.OrdinalIgnoreCase));

    /// <summary>A game of one region with no screen: its maps, a story begun as a new game's, a team and a bag.</summary>
    private sealed class Game
    {
        private readonly string region;
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;

        public Game(string region, params string[] team)
        {
            this.region = region;
            StoryMigration.BeginNewGame(Story, ScriptLibrary.Default);
            foreach (string species in team.DefaultIfEmpty("Garchomp"))
                Party.Add(new Pokemon(PokemonDatabase.Get(species)!, 70, new Random(Party.Members.Count)));
        }

        public Map MapNamed(string name)
        {
            if (Built[region].Value.TryGetValue(name, out var map)) return map;
            // (a room of a map file, Sinnoh's laboratory)
            return GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, name + ".json")).ToMap();
        }

        /// <summary>To an area's map (or a room's), with the story laid on it.</summary>
        public Game At(string areaOrRoom)
        {
            string name = WorldOf(region).MapOf(areaOrRoom)?.Name ?? areaOrRoom;
            Map = MapNamed(name);
            Map.ForgetForced();
            Map.ApplyPresence(Story.Has);
            Map.ApplyDoors(Story.Has);
            return this;
        }

        public NPC Person(string key, string? area = null) =>
            Map.Everyone.First(n => n.Key == key && (area == null || string.Equals(n.ScriptFile, area, StringComparison.OrdinalIgnoreCase)));

        public bool Present(string key, string? area = null) =>
            Map.NPCs.Any(n => n.Key == key && (area == null || string.Equals(n.ScriptFile, area, StringComparison.OrdinalIgnoreCase)));

        /// <summary>Spoken to: the person's script, played to its end.</summary>
        public HeadlessScriptHost Talk(string key, string? area = null)
        {
            var npc = Person(key, area);
            return Play(ScriptLibrary.Default.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc,
                (npc.GridX, npc.GridY + 1));
        }

        public HeadlessScriptHost Play(Script script, NPC? subject = null, (int, int)? tile = null)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = tile ?? (0, 0),
                MapNamed = name => Built[region].Value.GetValueOrDefault(name),
                Fight = _ => BattleOutcome.Won,
                NeedsPokemon = true
            };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(script, subject);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            Map.ApplyPresence(Story.Has);
            return host;
        }

        public int Has(string item) => Bag.GetQuantity(ItemDatabase.Get(item)!);
    }

    /// <summary>The professors who give the stones of our own Megas (decisions/R20b-mega-proposals.md, "Where the stones are found").</summary>
    public static readonly (string Region, string Place, string Key, string HallOfFame, string[] Stones)[] Professors =
    {
        (RegionDatabase.Sinnoh, "RowanLab", "prof_rowan", "SinnohHallOfFame", new[] { "Torterrite", "Infernapite", "Empoleonite" }),
        (RegionDatabase.Johto, "new_bark_elms_lab_1f", "t20r0101_doctor", "JohtoHallOfFame", new[] { "Typhlosionite" }),
        (RegionDatabase.Unova, "nuvema_town_lab", "0", "UnovaHallOfFame", new[] { "Serperiorite", "Samurottite" }),
        // Plan 23, Alola finished: Kukui in his laboratory, after the partners' Z-Crystals
        (RegionDatabase.Alola, "AlolaRoute1_KukuiLab", "kukui", "AlolaHallOfFame", new[] { "Decidueyite", "Incineroarite", "Primarinite" })
    };

    [Fact]
    public void TheProfessorsGiveTheStonesOfOurMegasAfterTheHallOfFameWithAKeyStoneOnce()
    {
        foreach (var (region, place, key, hallOfFame, stones) in Professors)
            foreach (bool champion in new[] { false, true })
                foreach (bool keyStone in new[] { false, true })
                {
                    var game = new Game(region);
                    // (past the opening of Sinnoh's laboratory, where the professor's own talk waits for its scene)
                    game.Story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", 4);
                    // (and past Alola's first chapter, whose laboratory has given its Pokédex)
                    game.Story.SetVar("VAR_ALOLA_STORY", 60);
                    if (champion) game.Story.Set(hallOfFame);
                    if (keyStone) game.Bag.AddItem(ItemDatabase.Get(BattleCore.KeyStoneItem)!, 1);
                    game.At(place).Talk(key);
                    int expected = champion && keyStone ? 1 : 0;
                    Assert.All(stones, stone => Assert.True(game.Has(stone) == expected,
                        $"{region}'s professor, Champion {champion}, Key Stone {keyStone}: {game.Has(stone)} {stone}"));
                    if (expected == 0) continue;
                    // Spoken to again: no second stone, and the talk goes on as it did
                    var again = game.Talk(key);
                    Assert.All(stones, stone => Assert.Equal(1, game.Has(stone)));
                    Assert.NotEmpty(again.Transcript);
                }
    }

    [Fact]
    public void OurFifteenStonesAreTheThreeProfessorsAndTheOtherNineAreOwedToTheirRegions()
    {
        var ours = ItemDatabase.GetAll().Where(i => i.MegaStone != null && i.Id is >= 9400 and <= 9414).Select(i => i.Name).ToList();
        Assert.Equal(15, ours.Count);
        foreach (var (region, _, _, _, stones) in Professors)
            foreach (string stone in stones)
                Assert.True(ItemSources.In(region).Has(stone, ItemSourceKind.Given), $"{region} gives no {stone}");
        // The other six wait for their regions' professors after their Halls of Fame
        var given = Professors.SelectMany(p => p.Stones).ToHashSet();
        Assert.All(ours.Where(s => !given.Contains(s)), stone => Assert.Contains("the bag holds a Key Stone", Owed[stone], StringComparison.Ordinal));
        Assert.Equal(6, ours.Count(s => !given.Contains(s)));
    }

    // ------------------------------------------------------------------ Unova's form items

    [Fact]
    public void UnovasScenesLeaveItsFormItemsInTheBag()
    {
        // The DNA Splicers: left where Ghetsis stood when the Shadow Triad took him away, in the scene of the cavern
        var chasm = new Game(RegionDatabase.Unova);
        chasm.Story.SetVar("VAR_UNOVA_CHASM", 3);
        chasm.At("giant_chasm_6");
        var scene = chasm.Play(ScriptLibrary.Default.Find("Ghetsis", "giant_chasm_6")!, null, (15, 25));
        Assert.Equal(1, chasm.Has("DNA Splicers"));
        Assert.Equal(4, chasm.Story.Var("VAR_UNOVA_CHASM"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "N" && l.Text.Contains("DNA Splicers", StringComparison.Ordinal));

        // The Reveal Glass: the shrine's breeder, to a Trainer with one of the three of the legend, once
        var shrine = new Game(RegionDatabase.Unova, "Garchomp").At("abundant_shrine");
        var told = shrine.Talk("10", "abundant_shrine");
        Assert.Equal(0, shrine.Has("Reveal Glass"));
        Assert.Contains(told.Transcript, l => l.Text.Contains("three great Pokémon", StringComparison.Ordinal));
        foreach (string force in new[] { "Tornadus", "Thundurus", "Landorus" })
        {
            var game = new Game(RegionDatabase.Unova, force).At("abundant_shrine");
            game.Talk("10", "abundant_shrine");
            Assert.Equal(1, game.Has("Reveal Glass"));
            game.Talk("10", "abundant_shrine");
            Assert.Equal(1, game.Has("Reveal Glass"));
        }

        // Three of Genesect's drives: Driftveil's scientist, to a Trainer with a Genesect, once (the Shock Drive is the P2
        // Laboratory's)
        string[] drives = { "Burn Drive", "Chill Drive", "Douse Drive" };
        var flats = new Game(RegionDatabase.Unova).At("driftveil_city_3");
        flats.Talk("3", "driftveil_city_3");
        Assert.All(drives, d => Assert.Equal(0, flats.Has(d)));
        var genesect = new Game(RegionDatabase.Unova, "Genesect").At("driftveil_city_3");
        genesect.Talk("3", "driftveil_city_3");
        Assert.All(drives, d => Assert.Equal(1, genesect.Has(d)));
        genesect.Talk("3", "driftveil_city_3");
        Assert.All(drives, d => Assert.Equal(1, genesect.Has(d)));
        Assert.True(ItemSources.In(RegionDatabase.Unova).Has("Shock Drive"));

        // The Fire Gem in Reversal Mountain, and Unova's held items at the Battle Subway's counter
        var mountain = new Game(RegionDatabase.Unova).At("reversal_mountain_3");
        var gem = mountain.Person("reversal_mountain_3.FireGem");
        Assert.True(gem.IsItemBall);
        Assert.Equal("Fire Gem", gem.Item);
        foreach (string held in new[] { "Red Card", "Binding Band", "Absorb Bulb", "Cell Battery" })
            Assert.True(ItemSources.In(RegionDatabase.Unova).Has(held, ItemSourceKind.Prize), $"Unova's prize counter has no {held}");
    }

    // ------------------------------------------------------------------ Hoenn's post-game: Omega Ruby's and Alpha Sapphire's stones

    /// <summary>Omega Ruby's and Alpha Sapphire's Mega Stones found on the ground of Hoenn after its Hall of Fame (decision 11).</summary>
    public static readonly (string Area, string Stone)[] HoennFound =
    {
        ("granite_cave_1f", "Steelixite"), ("magma_hideout_4f", "Cameruptite"), ("aqua_hideout_b2f", "Sharpedonite"),
        ("abandoned_ship_room_b1f", "Beedrillite"), ("sootopolis_city", "Sablenite"), ("shoal_cave_low_tide_lower_room", "Glalitite")
    };

    [Fact]
    public void HoennsPostGameGivesOmegaRubysAndAlphaSapphiresStonesOnce()
    {
        // Every stone Omega Ruby and Alpha Sapphire brought in is Hoenn's (Diancite comes with Diancie, an event's)
        string[] oras =
        {
            "Latiasite", "Latiosite", "Swampertite", "Sceptilite", "Blazikenite", "Sablenite", "Altarianite", "Galladite", "Audinite",
            "Metagrossite", "Sharpedonite", "Slowbronite", "Steelixite", "Pidgeotite", "Glalitite", "Cameruptite", "Lopunnite",
            "Salamencite", "Beedrillite"
        };
        var hoenn = ItemSources.In(RegionDatabase.Hoenn);
        Assert.All(oras, stone => Assert.True(hoenn.Has(stone), $"Hoenn gives no {stone}"));

        // Found on the ground once the Hall of Fame is entered, and only then
        foreach (var (area, stone) in HoennFound)
        {
            var before = new Game(RegionDatabase.Hoenn).At(area);
            var ball = before.Person($"{area}.{stone}");
            Assert.Equal(stone, ball.Item);
            Assert.False(before.Present(ball.Key!), $"the {stone} lies in {area} before the Hall of Fame");
            var after = new Game(RegionDatabase.Hoenn);
            after.Story.Set("HoennHallOfFame");
            Assert.True(after.At(area).Present(ball.Key!), $"no {stone} in {area} after the Hall of Fame");
        }

        // Handed over by Hoenn's own people, once each
        void GivesOnce(string area, string key, string[] stones, Action<Game>? ready = null, bool needsHallOfFame = true)
        {
            if (needsHallOfFame)
            {
                var early = new Game(RegionDatabase.Hoenn, "Gallade", "Altaria");
                early.Story.SetVar("VAR_LITTLEROOT_INTRO_STATE", 7);
                ready?.Invoke(early);
                early.At(area);
                if (early.Present(key, area)) early.Talk(key, area);
                Assert.All(stones, stone => Assert.Equal(0, early.Has(stone)));
            }
            var game = new Game(RegionDatabase.Hoenn, "Gallade", "Altaria");
            game.Story.Set("HoennHallOfFame");
            game.Story.SetVar("VAR_LITTLEROOT_INTRO_STATE", 7);
            game.Story.SetVar("VAR_HOENN_CHAMPION_HOME", 1);
            ready?.Invoke(game);
            game.At(area).Talk(key, area);
            Assert.All(stones, stone => Assert.True(game.Has(stone) == 1, $"{area} {key}: {game.Has(stone)} {stone}"));
            ready?.Invoke(game);
            game.Talk(key, area);
            Assert.All(stones, stone => Assert.Equal(1, game.Has(stone)));
        }
        GivesOnce("littleroot_town_brendans_house_1f", "players_house_1f_mom", new[] { "Latiasite", "Latiosite" });
        GivesOnce("meteor_falls_stevens_cave", "meteor_falls_steven", new[] { "Sceptilite", "Blazikenite", "Swampertite" }, needsHallOfFame: false);
        GivesOnce("rustboro_city_devon_corp_3f", "gentleman", new[] { "Pidgeotite" });
        GivesOnce("shoal_cave_low_tide_entrance_room", "expert_m", new[] { "Slowbronite" }, g =>
        {
            g.Bag.AddItem(ItemDatabase.Get("Shoal Salt")!, 4);
            g.Bag.AddItem(ItemDatabase.Get("Shoal Shell")!, 4);
        });
        GivesOnce("lilycove_city", "man_2", new[] { "Altarianite" });
        GivesOnce("mauville_city", "maniac", new[] { "Lopunnite" });
        GivesOnce("fallarbor_town_cozmos_house", "scientist_1", new[] { "Galladite" }, g => g.Story.Set("FLAG_RECEIVED_TM_RETURN"));
        GivesOnce("battle_frontier_outside_west", "gentleman", new[] { "Audinite" }, needsHallOfFame: false);
        GivesOnce("meteor_falls_1f_1r", "dragon_elder", new[] { "Salamencite" });

        // The collector shown no Altaria gives nothing
        var noAltaria = new Game(RegionDatabase.Hoenn, "Gallade");
        noAltaria.Story.Set("HoennHallOfFame");
        noAltaria.At("lilycove_city").Talk("man_2", "lilycove_city");
        Assert.Equal(0, noAltaria.Has("Altarianite"));

        // Steven's Beldum comes holding its line's Metagrossite
        var house = new Game(RegionDatabase.Hoenn);
        house.Story.Set("HoennHallOfFame");
        house.At("mossdeep_city_stevens_house").Talk("stevens_house_ball", "mossdeep_city_stevens_house");
        Assert.Equal("Metagrossite", house.Party.Members.Single(p => p.Species.Name == "Beldum").HeldItem?.Name);
    }

    [Fact]
    public void ThePlacedBallsAndTheElderStandWhereTheWholeGameComes()
    {
        foreach (var (region, places) in new[]
                 {
                     (RegionDatabase.Hoenn, HoennFound.Select(f => (f.Area, $"{f.Area}.{f.Stone}")).Append(("meteor_falls_1f_1r", "dragon_elder")).ToArray()),
                     (RegionDatabase.Unova, new[] { ("reversal_mountain_3", "reversal_mountain_3.FireGem") })
                 })
        {
            var world = WorldOf(region);
            var maps = world.BuildMaps().ToDictionary(m => m.Name);
            Map Named(string name) => maps.TryGetValue(name, out var map) ? map : MapDatabase.Get(name);
            var walked = WorldWalk.From(Named, WorldWalk.OpenedAhead.Where(s => maps.ContainsKey(s.Map)).Prepend(RegionDatabase.Get(region)!.Start!).ToArray());
            foreach (var (area, key) in places)
            {
                var map = maps[world.MapOf(area)!.Name];
                var thing = map.Everyone.Single(n => n.Key == key);
                // A ball is walked onto (the walk picks them up); someone who stands there is spoken to from beside them
                var at = thing.IsItemBall ? new[] { (thing.GridX, thing.GridY) }
                    : new[] { (thing.GridX + 1, thing.GridY), (thing.GridX - 1, thing.GridY), (thing.GridX, thing.GridY + 1), (thing.GridX, thing.GridY - 1) };
                Assert.True(walked.TryGetValue(map, out var tiles) && at.Any(tiles.Contains),
                    $"nobody can come to {key} at {thing.GridX},{thing.GridY} of {map.Name}");
            }
        }
    }
}
