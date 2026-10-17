using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 03 · D12a: every species of Diamond, Pearl and Platinum (the National Pokédex to Arceus) can be had in Sinnoh
/// with nobody else, on the map or by a quest (decision 4). <see cref="SpeciesSources"/> gathers every way the data and
/// the scripts give; what nothing gives is Pal Park's (its Catching Show draws from it), and the test holds that list
/// to the one below, so a way lost or a way given shows here.
/// </summary>
public class SinnohSpeciesTests
{
    private static readonly SpeciesSources Sinnoh = SpeciesSources.Sinnoh;

    /// <summary>
    /// What only Pal Park's Catching Show gives (plan 03 · D12a's proposal, the stand-in the user can overturn): the
    /// species a second game in the console called up in the original, the other regions' first partners, and the
    /// legendary and mythical Pokémon that plan 03 · D12c gives quests of their own, with their lines. A session that
    /// gives one of them another way takes it off this list.
    /// </summary>
    public static readonly string[] OnlyInPalPark =
    {
        // A second game in the console's species (the original's dual slots), with their lines
        "Caterpie", "Metapod", "Butterfree", "Weedle", "Kakuna", "Beedrill", "Ekans", "Arbok", "Sandshrew", "Sandslash",
        "Vulpix", "Ninetales", "Growlithe", "Arcanine", "Pineco", "Forretress", "Shuckle", "Teddiursa", "Ursaring",
        "Lotad", "Lombre", "Ludicolo", "Seedot", "Nuzleaf", "Shiftry", "Sableye", "Mawile", "Zangoose", "Seviper",
        "Lunatone", "Solrock",
        // The first partners of Kanto, Johto and Hoenn
        "Bulbasaur", "Ivysaur", "Venusaur", "Charmander", "Charmeleon", "Charizard", "Squirtle", "Wartortle", "Blastoise",
        "Chikorita", "Bayleef", "Meganium", "Cyndaquil", "Quilava", "Typhlosion", "Totodile", "Croconaw", "Feraligatr",
        "Treecko", "Grovyle", "Sceptile", "Torchic", "Combusken", "Blaziken", "Mudkip", "Marshtomp", "Swampert",
        // The legendary and mythical Pokémon of the older regions and Sinnoh's of an event (plan 03 · D12c's quests)
        "Mewtwo", "Mew", "Raikou", "Entei", "Suicune", "Lugia", "Ho-Oh", "Celebi", "Regirock", "Regice", "Registeel",
        "Latias", "Latios", "Kyogre", "Groudon", "Rayquaza", "Jirachi", "Deoxys", "Phione", "Manaphy", "Darkrai", "Shaymin",
        "Arceus"
    };

    [Fact]
    public void EverySpeciesOfDiamondPearlAndPlatinumCanBeHadInSinnoh()
    {
        var missing = PokemonDatabase.GetAll().Where(s => s.DexNumber is >= 1 and <= SpeciesSources.HighestInSinnoh)
            .Where(s => !Sinnoh.Has(s.Name) && !Sinnoh.PalParkPool.Contains(s.Name)).Select(s => s.Name).ToList();
        Assert.Empty(missing);
        Assert.Equal(SpeciesSources.HighestInSinnoh, Sinnoh.Ways.Count(w => PokemonDatabase.Get(w.Key)!.DexNumber <= SpeciesSources.HighestInSinnoh) + Sinnoh.PalParkPool.Count);
    }

    [Fact]
    public void WhatOnlyPalParkGivesIsTheListAndNothingElse()
    {
        var pool = Sinnoh.PalParkPool.ToList();
        var lost = pool.Except(OnlyInPalPark).ToList();
        var given = OnlyInPalPark.Except(pool).Select(s => $"{s} ({Sinnoh.Ways.GetValueOrDefault(s)})").ToList();
        Assert.True(lost.Count == 0 && given.Count == 0,
            $"Only Pal Park now gives: {string.Join(", ", lost)}. Another way now gives: {string.Join(", ", given)}. The pool: {string.Join(", ", pool.Select(p => $"\"{p}\""))}");
    }

    /// <summary>
    /// Where Diamond's and Pearl's species that Platinum's tables leave out are met with the National Pokédex (the
    /// user's choice of 2026-10-17: their own places in those versions, not the Battle Zone), by area and the grass's
    /// two slots or the Super Rod's.
    /// </summary>
    public static readonly (string Area, string[] Land, string? SuperRod)[] TheirOwnPlaces =
    {
        ("eterna_forest", new[] { "Murkrow", "Misdreavus" }, null),
        ("route_221", new[] { "Stunky", "Skuntank" }, "Clamperl"),
        ("route_222", new[] { "Glameow", "Purugly" }, null),
        ("route_228", new[] { "Trapinch", "Vibrava" }, null),
        ("route_219", Array.Empty<string>(), "Clamperl"),
    };

    [Fact]
    public void TheOtherVersionsSpeciesAreMetInTheirOwnPlacesWithTheNationalPokedex()
    {
        // Diamond's and Pearl's species that Platinum's tables leave out
        string[] exclusives = { "Murkrow", "Misdreavus", "Glameow", "Purugly", "Stunky", "Skuntank", "Trapinch", "Vibrava", "Clamperl" };
        foreach (string s in exclusives)
            Assert.True(Sinnoh.Has(s) && Sinnoh.Ways[s].Contains("National Pokédex"), $"{s}: {Sinnoh.Ways.GetValueOrDefault(s)}");
        // ... and by evolving them
        foreach (string s in new[] { "Honchkrow", "Mismagius", "Flygon", "Huntail", "Gorebyss" }) Assert.True(Sinnoh.Has(s), s);
        // None is left in the Battle Zone's routes but the Trapinch line, whose own place Route 228 is
        foreach (string key in new[] { "route_225", "route_226", "route_229" })
            Assert.True(AreaOf(key).NationalSlots.Count == 0 && AreaOf(key).NationalSuperRod.Count == 0, key);
    }

    /// <summary>An area on the map that holds it (Eterna Forest is a map of its own, the routes the map of Sinnoh).</summary>
    private static MapArea AreaOf(string key) => MapDatabase.Get(SinnohWorld.Value.MapOf(key)!.Name).FindArea(key)!;

    private static readonly Lazy<World> SinnohWorld = new(() => World.LoadAll().First(w => w.Index.Region == "Sinnoh"));

    [Fact]
    public void TheNationalSlotsComeOnlyWithTheNationalPokedexAtTheSlotsOwnLevels()
    {
        foreach (var (key, land, superRod) in TheirOwnPlaces)
        {
            var area = AreaOf(key);
            if (land.Length > 0)
            {
                var before = EncounterSlots.Grass(area, new EncounterMoment());
                var after = EncounterSlots.Grass(area, new EncounterMoment { NationalDex = true });
                Assert.DoesNotContain(before, e => land.Contains(e.SpeciesName));
                Assert.Equal(land, after.Skip(OverlayNational.FirstLandSlot).Take(2).Select(e => e.SpeciesName));
                for (int i = 0; i < 12; i++)
                {
                    Assert.Equal(before[i].MinLevel, after[i].MinLevel);
                    Assert.Equal(before[i].Weight, after[i].Weight);
                }
            }
            var rod = area.RodEncounters[(int)FishingRod.Super];
            if (superRod != null)
            {
                Assert.Equal(superRod, EncounterSlots.SuperRod(area, rod, new EncounterMoment { NationalDex = true })[OverlayNational.SuperRodSlot].SpeciesName);
                Assert.Same(rod, EncounterSlots.SuperRod(area, rod, new EncounterMoment()));
            }
        }
        // The slots' own levels: the forest's 13, Route 221's 31 and 30, Route 222's 40 and 41, Route 228's 49 and 50
        int[] Levels(string key) => EncounterSlots.Grass(AreaOf(key), new EncounterMoment { NationalDex = true })
            .Skip(OverlayNational.FirstLandSlot).Take(2).Select(e => e.MinLevel).ToArray();
        Assert.Equal(new[] { 13, 13 }, Levels("eterna_forest"));
        Assert.Equal(new[] { 31, 30 }, Levels("route_221"));
        Assert.Equal(new[] { 40, 41 }, Levels("route_222"));
        Assert.Equal(new[] { 49, 50 }, Levels("route_228"));
    }

    [Fact]
    public void EveryItemAndPlaceAnEvolutionAsksForIsInSinnoh()
    {
        Assert.True(Sinnoh.Items.ContainsKey(Evolution.LinkingCord), "A Linking Cord stands for every trade");
        foreach (var species in PokemonDatabase.GetAll().Where(s => s.DexNumber <= SpeciesSources.HighestInSinnoh))
            foreach (var e in species.Evolutions ?? new())
            {
                if (e.Region != null || e.FromForm != null || PokemonDatabase.Get(e.TargetSpecies)!.DexNumber > SpeciesSources.HighestInSinnoh) continue;
                if (e.Item != null) Assert.True(Sinnoh.Items.ContainsKey(e.Item), $"{species.Name} into {e.TargetSpecies} wants a {e.Item}");
                if (e.Location != null) Assert.True(Sinnoh.Sites.Contains(e.Location), $"{species.Name} into {e.TargetSpecies} wants the {e.Location}");
            }
        // Both armoured fossils for one game, whatever the trainer's number
        Assert.True(Sinnoh.Items.ContainsKey("Skull Fossil") && Sinnoh.Items.ContainsKey("Armor Fossil"));
    }

    /// <summary>
    /// The forms of Platinum's own that nothing in Sinnoh gives yet, each with whose it is to give. A session that gives
    /// one takes it off; the test fails on one listed that is had.
    /// </summary>
    public static readonly Dictionary<string, string> FormsAwaiting = new()
    {
        ["Unown-Exclamation"] = "the room past Maniac Tunnel, plan 08 · P19",
        ["Unown-Question"] = "the room past Maniac Tunnel, plan 08 · P19",
        ["Rotom-Heat"] = "Rotom's room and its appliances, plan 03 · D12c",
        ["Rotom-Wash"] = "Rotom's room and its appliances, plan 03 · D12c",
        ["Rotom-Frost"] = "Rotom's room and its appliances, plan 03 · D12c",
        ["Rotom-Fan"] = "Rotom's room and its appliances, plan 03 · D12c",
        ["Rotom-Mow"] = "Rotom's room and its appliances, plan 03 · D12c",
        ["Shaymin-Sky"] = "the Gracidea, with Shaymin's quest, plan 03 · D12c",
    };

    [Fact]
    public void ThePlatinumFormsHaveTheirWaysInSinnoh()
    {
        var world = World.LoadAll().First(w => w.Index.Region == "Sinnoh");
        var had = new HashSet<string>();
        foreach (string key in world.Index.Areas)
        {
            var area = world.Area(key);
            if (area == null) continue;
            var met = (area.Land ?? new()).Concat(area.Water ?? new()).Select(e => e.Species).Concat(area.Day ?? new()).Concat(area.Night ?? new()).ToHashSet();
            // Unown in its room's letters, Shellos and Gastrodon in the east sea's colours (FormRules.WildForm)
            if (met.Contains("Unown"))
                foreach (string letter in FormRules.UnownTables[Math.Clamp((area.UnownTable ?? 1) - 1, 0, FormRules.UnownTables.Length - 1)])
                    had.Add(FormRules.UnownForm(letter) ?? "Unown");
            if (area.EastSea == true)
                foreach (string s in new[] { "Shellos", "Gastrodon" })
                    if (met.Contains(s)) had.Add(s + "-East");
        }
        // Giratina's Origin Forme holds the Griseous Orb; Arceus's types are its plates
        if (Sinnoh.Items.ContainsKey("Griseous Orb")) had.Add("Giratina-Origin");
        foreach (var form in PokemonDatabase.Get("Arceus")!.Forms ?? new())
            if (Sinnoh.Items.ContainsKey(form.Name["Arceus-".Length..] switch
                {
                    "Fire" => "Flame Plate", "Water" => "Splash Plate", "Electric" => "Zap Plate", "Grass" => "Meadow Plate", "Ice" => "Icicle Plate",
                    "Fighting" => "Fist Plate", "Poison" => "Toxic Plate", "Ground" => "Earth Plate", "Flying" => "Sky Plate", "Psychic" => "Mind Plate",
                    "Bug" => "Insect Plate", "Rock" => "Stone Plate", "Ghost" => "Spooky Plate", "Dragon" => "Draco Plate", "Dark" => "Dread Plate",
                    "Steel" => "Iron Plate", "Fairy" => "Pixie Plate", _ => "-"
                }))
                had.Add(form.Name);
        // Burmy's and Wormadam's cloaks come of the ground a battle is fought on (FormRules.CloakBurmy), which Sinnoh has every kind of
        foreach (var ground in Enum.GetValues<BattleTerrain>())
            if (FormRules.BurmyCloakFor(ground) is { } cloak) { had.Add(cloak); had.Add(cloak.Replace("Burmy", "Wormadam")); }
        // Castform, Cherrim and Deoxys change in battle or by the meteorites the species comes to (Deoxys from Pal Park)
        had.UnionWith(new[] { "Castform-Sunny", "Castform-Rainy", "Castform-Snowy", "Cherrim-Sunshine", "Deoxys-Attack", "Deoxys-Defense", "Deoxys-Speed" });

        var platinums = PokemonDatabase.GetAll().Where(s => s.DexNumber is 201 or 351 or 386 or 412 or 413 or 421 or 422 or 423 or 479 or 487 or 492 or 493)
            .SelectMany(s => (s.Forms ?? new()).Select(f => f.Name)).Where(f => f != "Arceus-Fairy" && PokemonDatabase.SpeciesOfForm(f) != null).ToList();
        var missing = platinums.Where(f => !had.Contains(f) && !FormsAwaiting.ContainsKey(f)).ToList();
        var given = FormsAwaiting.Keys.Where(had.Contains).ToList();
        Assert.True(missing.Count == 0 && given.Count == 0, $"Had nowhere: {string.Join(", ", missing)}. Listed as awaiting but had: {string.Join(", ", given)}");
    }

    [Fact]
    public void TheProfessorGivesTheOtherTwoStartersToAChampion()
    {
        foreach (string chosen in StoryState.Starters)
        {
            var story = new StoryState();
            story.ChooseStarter(chosen);
            story.Set("FLAG_GAME_COMPLETED");
            story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", 4);
            var host = new HeadlessScriptHost(story) { NationalDex = true };
            Run("Rowan", "RowanLab", host);
            var given = host.Party.Members.Concat(host.Boxes).Select(p => p.Species.Name).ToList();
            Assert.Equal(StoryState.Starters.Where(s => s != chosen).OrderBy(s => s), given.OrderBy(s => s));
            // At level 5, so the player raises and evolves them (the user's choice, 2026-10-17)
            Assert.All(host.Party.Members, p => Assert.Equal(5, p.Level));
            Assert.True(story.Has("FLAG_RECEIVED_SANDGEM_TOWN_LAB_OTHER_STARTERS"));

            // Once only
            var again = new HeadlessScriptHost(story) { NationalDex = true };
            Run("Rowan", "RowanLab", again);
            Assert.Empty(again.Party.Members);
        }
    }

    [Fact]
    public void TheGtsHandsOverALinkingCordWheneverTheBagHasNone()
    {
        var host = Run("Gts", "GlobalTerminal1F", new HeadlessScriptHost());
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get(Evolution.LinkingCord)!));
        Run("Gts", "GlobalTerminal1F", host);
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get(Evolution.LinkingCord)!));
    }

    [Fact]
    public void TheMuseumsCollectorGivesBothArmouredFossilsWithTheNationalPokedex()
    {
        var story = new StoryState();
        var early = Run("Collector", "OreburghMiningMuseum", new HeadlessScriptHost(story));
        Assert.Equal(0, early.Bag.GetQuantity(ItemDatabase.Get("Skull Fossil")!));
        var host = Run("Collector", "OreburghMiningMuseum", new HeadlessScriptHost(story) { NationalDex = true });
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Skull Fossil")!));
        Assert.Equal(1, host.Bag.GetQuantity(ItemDatabase.Get("Armor Fossil")!));
        var again = Run("Collector", "OreburghMiningMuseum", new HeadlessScriptHost(story) { NationalDex = true });
        Assert.Equal(0, again.Bag.GetQuantity(ItemDatabase.Get("Skull Fossil")!));
    }

    [Fact]
    public void PalParksVisitorsGoRoundThePoolSixADay()
    {
        // The user's choice of 2026-10-17: a fixed rotation by the date, no chance in which species come
        var pool = Sinnoh.PalParkPool;
        Assert.True(pool.Count >= 6);
        var day = new DateTime(2026, 10, 17);

        // One date always brings the same six, whatever the dice
        var six = PalParkState.VisitorSpecies(pool, day);
        Assert.Equal(6, six.Distinct().Count());
        Assert.All(six, s => Assert.Contains(s, pool));
        Assert.Equal(six, PalParkState.VisitorSpecies(pool, day));
        Assert.Equal(six, PalParkState.Visitors(pool, day, new Random(7)).Select(p => p.Species.Name));
        Assert.Equal(six, PalParkState.Visitors(pool, day.AddHours(23), new Random(8)).Select(p => p.Species.Name));
        Assert.All(PalParkState.Visitors(pool, day, new Random(7)), p => Assert.InRange(p.Level, PalParkState.VisitorLowest, PalParkState.VisitorHighest));

        // The next day brings the next six of the ring
        var next = PalParkState.VisitorSpecies(pool, day.AddDays(1));
        Assert.NotEqual(six.OrderBy(s => s), next.OrderBy(s => s));
        int at = pool.ToList().IndexOf(six[0]);
        Assert.Equal(pool[(at + 6) % pool.Count], next[0]);

        // Every species of the pool comes round in turn
        var seen = new HashSet<string>();
        for (int d = 0; d < pool.Count; d++) seen.UnionWith(PalParkState.VisitorSpecies(pool, day.AddDays(d)));
        Assert.Equal(pool.OrderBy(s => s), seen.OrderBy(s => s));

        // A pool of fewer than six is made up from Diamond's, Pearl's and Platinum's species in National order
        var few = PalParkState.VisitorSpecies(new[] { "Bulbasaur", "Mew" }, day);
        Assert.Equal(6, few.Distinct().Count());
        Assert.Contains("Mew", few);
        Assert.All(few, s => Assert.InRange(PokemonDatabase.Get(s)!.DexNumber, 1, SpeciesSources.HighestInSinnoh));
    }

    [Fact]
    public void AnotherDaysVisitorsMakeWayForTodays()
    {
        var pool = Sinnoh.PalParkPool;
        var park = new PalParkState();
        var day = new DateTime(2026, 10, 17);
        Assert.True(park.Welcome(pool, day, new Random(1)));
        var first = park.Waiting.ToList();
        // The same day leaves the six waiting as they are
        Assert.False(park.Welcome(pool, day, new Random(2)));
        Assert.Equal(first, park.Waiting);
        // The next day's six take their place, but never in the middle of a show
        park.Begin(new Random(3));
        Assert.False(park.Welcome(pool, day.AddDays(1), new Random(4)));
        park.Abandon();
        Assert.True(park.Welcome(pool, day.AddDays(1), new Random(4)));
        Assert.Equal(PalParkState.VisitorSpecies(pool, day.AddDays(1)), park.Waiting.Select(p => p.Species.Name));
    }

    [Fact]
    public void TheParksAttendantLetsVisitorsComeWhenNothingWaits()
    {
        var host = new HeadlessScriptHost { NationalDex = true };
        Run("Worker", "PalParkLobby", host);
        Assert.True(host.Pastimes.PalPark.Stocked);
        Assert.All(host.Pastimes.PalPark.Waiting, p => Assert.Contains(p.Species.Name, Sinnoh.PalParkPool));
    }

    private static HeadlessScriptHost Run(string script, string place, HeadlessScriptHost host)
    {
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, place)!, new NPC { Name = script });
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }
}
