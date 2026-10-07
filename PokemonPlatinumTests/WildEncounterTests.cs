using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 06 · R13's wild Pokémon, beyond the tables plan 01 brought: the slots the time of day, swarms and the
/// dailies swap in, what wild Pokémon hold, the days that change the odds, honey trees, the Poké Radar and its chain,
/// roaming Pokémon, Feebas's tiles and poison in the field. Each number is the original's (<c>src/overlay006/</c>,
/// <c>src/pokeradar.c</c>, <c>src/roaming_pokemon.c</c>, <c>src/overlay005/honey_tree.c</c>,
/// <c>src/overlay005/field_control.c</c>), named in a comment.
/// </summary>
public class WildEncounterTests
{
    /// <summary>A generator that gives the numbers it was handed, in turn (each kept under the bound asked), then <paramref name="then"/>.</summary>
    private sealed class Rolls(int then, params int[] rolls) : Random
    {
        private readonly Queue<int> left = new(rolls);
        public override int Next(int maxValue) => Math.Min(left.Count > 0 ? left.Dequeue() : then, Math.Max(0, maxValue - 1));
        public override int Next(int minValue, int maxValue) => minValue + Next(maxValue - minValue);
        public override int Next() => Next(int.MaxValue);
        public override long NextInt64(long maxValue) => Next((int)Math.Min(int.MaxValue, maxValue));
    }

    private static WildEncounterEntry Row(string species, int level, int weight) => new() { SpeciesName = species, MinLevel = level, MaxLevel = level, Weight = weight };

    /// <summary>An area like an imported one: twelve grass slots of their own species, and the lists beside them.</summary>
    private static MapArea Area(string key = "route_201")
    {
        var area = new MapArea { Key = key, Open = true };
        string[] species = { "Starly", "Bidoof", "Kricketot", "Shinx", "Budew", "Abra", "Zubat", "Geodude", "Psyduck", "Machop", "Ponyta", "Magikarp" };
        for (int slot = 0; slot < 12; slot++) area.WildEncounters.Add(Row(species[slot], 3 + slot, WorldAreaFile.LandSlotWeights[slot]));
        area.DaySlots.AddRange(new[] { "Pidgey", "Rattata" });
        area.NightSlots.AddRange(new[] { "Hoothoot", "Murkrow" });
        area.SwarmSlots.AddRange(new[] { "Doduo", "Doduo" });
        area.RadarSlots.AddRange(new[] { "Nidoran♂", "Nidoran♀", "Nidoran♂", "Nidoran♀" });
        area.LandRate = 30;
        return area;
    }

    /// <summary>A map of tall grass one chunk across, all of it the one area.</summary>
    private static Map GrassMap(MapArea area)
    {
        var map = new Map(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                map.SetGroundTile(x, y, TileType.TallGrass);
        map.SetArea(0, 0, area);
        return map;
    }

    private static Pokemon Mon(string species, int level) => new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    private static Party PartyOf(params Pokemon[] members)
    {
        var party = new Party();
        foreach (var p in members) party.Add(p);
        return party;
    }

    // ================================================================== the moment's slots

    [Fact]
    public void TheTimeOfDayPutsItsTwoInSlotsTwoAndThree()
    {
        // WildEncounters_ReplaceTimedEncounters: the table's own are the morning's; TIMEOFDAY_DAY and TWILIGHT take
        // dayEncounters[0..1], NIGHT and LATE_NIGHT nightEncounters[0..1], into slots 2 and 3
        var area = Area();
        string[] Species(TimeOfDay time) => EncounterSlots.Grass(area, new EncounterMoment { Time = time }).Select(e => e.SpeciesName).ToArray();
        Assert.Equal(new[] { "Kricketot", "Shinx" }, Species(TimeOfDay.Morning)[2..4]);
        Assert.Equal(new[] { "Pidgey", "Rattata" }, Species(TimeOfDay.Day)[2..4]);
        Assert.Equal(new[] { "Pidgey", "Rattata" }, Species(TimeOfDay.Twilight)[2..4]);
        Assert.Equal(new[] { "Hoothoot", "Murkrow" }, Species(TimeOfDay.Night)[2..4]);
        Assert.Equal(new[] { "Hoothoot", "Murkrow" }, Species(TimeOfDay.LateNight)[2..4]);

        // Each slot keeps its level and its weight, and the area's own table is untouched
        var night = EncounterSlots.Grass(area, new EncounterMoment { Time = TimeOfDay.Night });
        Assert.Equal(5, night[2].MinLevel);
        Assert.Equal(10, night[2].Weight);
        Assert.Equal("Kricketot", area.WildEncounters[2].SpeciesName);
        Assert.Equal(new[] { "Starly", "Bidoof" }, night.Take(2).Select(e => e.SpeciesName));
    }

    [Fact]
    public void TheImportedAreasCarryTheirTimesOfDay()
    {
        // encounters_route_201.json: day STARLY, BIDOOF; night KRICKETOT, BIDOOF; swarms DODUO; radar the Nidoran
        var file = World.LoadAll().Single(w => w.Index.Region == "Sinnoh").Area("route_201")!;
        Assert.Equal(new[] { "Starly", "Bidoof" }, file.Day);
        Assert.Equal(new[] { "Kricketot", "Bidoof" }, file.Night);
        Assert.Equal(new[] { "Doduo", "Doduo" }, file.Swarm);
        Assert.Equal(new[] { "Nidoran♂", "Nidoran♀", "Nidoran♂", "Nidoran♀" }, file.Radar);

        // Every open area with grass has its two lists of two, and none without grass has any
        var world = World.LoadAll().Single(w => w.Index.Region == "Sinnoh");
        foreach (string key in world.Index.Areas)
        {
            var area = world.Area(key)!;
            if (area.Land == null) Assert.True(area.Day == null && area.Night == null && area.Swarm == null && area.Radar == null, key);
            else Assert.True(area.Day?.Count == 2 && area.Night?.Count == 2 && area.Radar?.Count == 4, key);
        }
    }

    [Fact]
    public void AStepInTheGrassMeetsTheMomentsTable()
    {
        // At night slot 2 (ten in a hundred, the rolls 40 to 49) is the night's first, not the table's own
        var area = Area();
        var map = GrassMap(area);
        var night = new EncounterMoment { Time = TimeOfDay.Night };
        Assert.Equal("Hoothoot", WildEncounterRules.Slot(map.WildAt(5, 5, moment: night).Table, false, null, new Rolls(0, 45)).SpeciesName);
        Assert.Equal("Kricketot", WildEncounterRules.Slot(map.WildAt(5, 5).Table, false, null, new Rolls(0, 45)).SpeciesName);

        // Steps in the grass at night meet the night's two and never the morning's
        var steps = new EncounterSteps();
        var met = new HashSet<string>();
        for (int i = 0; i < 6000; i++)
            if (map.RollWildEncounter(5, 5, steps, moment: night) is { } wild) met.Add(wild.SpeciesName);
        Assert.Contains("Hoothoot", met);
        Assert.Contains("Murkrow", met);
        Assert.DoesNotContain("Kricketot", met);
        Assert.DoesNotContain("Shinx", met);
    }

    [Fact]
    public void SomeDaysOfTheYearChangeTheOddsOfAStep()
    {
        // SpecialDates_ModifyEncounterRate: the flat 40 (70 in very tall grass or on a Bicycle) by the date's modifier
        Assert.Equal(30, SpecialDates.ModifyEncounterRate(40, new DateTime(2026, 1, 1)));    // -10
        Assert.Equal(50, SpecialDates.ModifyEncounterRate(40, new DateTime(2026, 1, 12)));   // +10
        Assert.Equal(45, SpecialDates.ModifyEncounterRate(40, new DateTime(2026, 1, 11)));   // +5
        Assert.Equal(40, SpecialDates.ModifyEncounterRate(40, new DateTime(2026, 2, 14)));   // listed, with no change
        Assert.Equal(35, SpecialDates.ModifyEncounterRate(40, new DateTime(2026, 12, 31)));  // -5
        Assert.Equal(80, SpecialDates.ModifyEncounterRate(70, new DateTime(2026, 7, 7)));    // +10
        Assert.Equal(40, SpecialDates.ModifyEncounterRate(40, new DateTime(2026, 10, 7)));   // an ordinary day
        Assert.Equal(0, SpecialDates.ModifyEncounterRate(0, new DateTime(2026, 1, 12)));

        // An attempt past the grace steps: a roll of 44 gets through on the 12th of January (50) and not on the 1st (30)
        EncounterSteps Past()
        {
            var steps = new EncounterSteps();
            for (int i = 0; i < 8; i++) steps.Meets(10, false, new Rolls(99));
            return steps;
        }
        Assert.True(Past().Meets(100, false, new Rolls(0, 44, 0), new DateTime(2026, 1, 12)));
        Assert.False(Past().Meets(100, false, new Rolls(0, 44, 0), new DateTime(2026, 1, 1)));
        Assert.False(Past().Meets(100, false, new Rolls(0, 44, 0)));
    }

    // ================================================================== what wild Pokémon hold

    [Fact]
    public void AWildPokemonHoldsItsSpeciesItemsByThePlatinumOdds()
    {
        // Pokemon_GiveHeldItem: sHeldItemChance { 45, 95 } (none, common up to 94, rare from 95), with Compound Eyes
        // { 20, 80 }; a species whose two items are the same always holds it
        var chansey = PokemonDatabase.Get("Chansey")!;
        Assert.Equal("Oval Stone", chansey.WildItems!.Common);
        Assert.Equal("Lucky Egg", chansey.WildItems.Rare);
        string? Held(PokemonSpecies species, int roll, string? ability = null) =>
            WildEncounterRules.HeldItem(species, ability == null ? null : new WildLead(ability, 20, Nature.Hardy, Gender.Male), new Rolls(0, roll))?.Name;
        Assert.Null(Held(chansey, 44));
        Assert.Equal("Oval Stone", Held(chansey, 45));
        Assert.Equal("Oval Stone", Held(chansey, 94));
        Assert.Equal("Lucky Egg", Held(chansey, 95));
        Assert.Null(Held(chansey, 19, "Compound Eyes"));
        Assert.Equal("Oval Stone", Held(chansey, 20, "Compound Eyes"));
        Assert.Equal("Lucky Egg", Held(chansey, 80, "Compound Eyes"));
        Assert.Equal("Leftovers", Held(PokemonDatabase.Get("Snorlax")!, 0));
        // Butterfree has no common item: the common roll holds nothing
        Assert.Null(Held(PokemonDatabase.Get("Butterfree")!, 50));
        Assert.Equal("Silver Powder", Held(PokemonDatabase.Get("Butterfree")!, 96));
        Assert.Null(Held(PokemonDatabase.Get("Bidoof")!, 99));
    }

    [Fact]
    public void EveryWildItemIsAnItemOfTheGame()
    {
        foreach (var species in PokemonDatabase.GetAll().Where(s => s.WildItems != null))
        {
            Assert.True(species.WildItems!.Common == null || ItemDatabase.Get(species.WildItems.Common) != null, $"{species.Name}: {species.WildItems.Common}");
            Assert.True(species.WildItems.Rare == null || ItemDatabase.Get(species.WildItems.Rare) != null, $"{species.Name}: {species.WildItems.Rare}");
        }
        Assert.True(PokemonDatabase.GetAll().Count(s => s.DexNumber <= 493 && s.WildItems != null) > 150);
    }

    // ================================================================== who keeps a Pokémon away, and from what

    [Fact]
    public void SweetScentAndHoneyAreKeptAwayByNeitherKeenEyeNorARepel()
    {
        // WildEncounters_TrySweetScentEncounter: repelActive FALSE, ignoreAbilityBlock TRUE
        var area = Area();
        var map = GrassMap(area);
        var lead = new WildLead("Keen Eye", 50, Nature.Hardy, Gender.Male, RepelLevel: 50);
        for (int i = 0; i < 40; i++) Assert.NotNull(map.DrawOutWild(5, 5, lead: lead));
        // The same lead keeps every one of them away from a step
        var steps = new EncounterSteps();
        int met = 0;
        for (int i = 0; i < 2000; i++)
            if (map.RollWildEncounter(5, 5, steps, lead: lead) != null) met++;
        Assert.Equal(0, met);
    }

    [Fact]
    public void ARodIsKeptFromNothingByARepelButKeenEyeStillHasItsSay()
    {
        // WildEncounters_TryFishingEncounter: InitEncounterFieldParams leaves repelActive FALSE; TryGenerateWildMon
        // still asks FirstMonAbilityPreventsEncounter
        var area = new MapArea { Key = "pond", Open = true };
        area.RodEncounters[(int)FishingRod.Old].Add(new WildEncounterEntry { SpeciesName = "Magikarp", MinLevel = 3, MaxLevel = 3, Weight = 100 });
        area.RodRates[(int)FishingRod.Old] = 100;
        var map = new Map(32, 32);
        map.SetArea(0, 0, area);
        var repelled = new WildLead("Run Away", 50, Nature.Hardy, Gender.Male, RepelLevel: 50);
        for (int i = 0; i < 40; i++) Assert.NotNull(map.Fish(3, 3, FishingRod.Old, repelled));
        var keen = new WildLead("Keen Eye", 50, Nature.Hardy, Gender.Male);
        int hooked = 0;
        for (int i = 0; i < 400; i++)
            if (map.Fish(3, 3, FishingRod.Old, keen) != null) hooked++;
        Assert.InRange(hooked, 120, 280);   // one time in two
    }

    // ================================================================== the day's number, swarms and the dailies

    [Fact]
    public void TheDaysNumberMovesOnByTheGroupsGenerator()
    {
        // ARNG_Next: × MT19937_F (1812433253) + 1; FieldSystem_HandleDailyEvents advances it once per day and makes the
        // marsh's and the swarm's numbers it
        Assert.Equal(1u, SpecialEncounters.Next(0));
        Assert.Equal(1812433254u, SpecialEncounters.Next(1));
        var state = new SpecialEncounters { DailyNumber = 0, MarshDaily = 7, SwarmDaily = 9 };
        state.DaysPass(0);
        Assert.Equal(7u, state.MarshDaily);
        state.DaysPass(2);
        Assert.Equal(1812433254u, state.DailyNumber);
        Assert.Equal(1812433254u, state.MarshDaily);
        Assert.Equal(1812433254u, state.SwarmDaily);
    }

    [Fact]
    public void ASwarmComesToOnePlaceADayOnceTheyHaveBegun()
    {
        // Swarm_GetMapId: sSwarmMapIdTable[swarm % NUM_SWARMS (22)]
        Assert.Equal(22, Swarms.Areas.Length);
        Assert.Equal("route_201", Swarms.AreaOf(0));
        Assert.Equal("route_201", Swarms.AreaOf(22));
        Assert.Equal("eterna_forest", Swarms.AreaOf(21));
        Assert.Equal("route_206", Swarms.AreaOf(uint.MaxValue));   // 4,294,967,295 = 22 × 195,225,786 + 3
        var state = new SpecialEncounters { SwarmDaily = 22 };
        Assert.Null(Swarms.Today(state));
        state.SwarmsOn = true;
        Assert.Equal("route_201", Swarms.Today(state));

        // There, the table's two swarming species take slots 0 and 1 (WildEncounters_ReplaceSwarmEncounters)
        var area = Area();
        var slots = EncounterSlots.Grass(area, new EncounterMoment { SwarmArea = "route_201" });
        Assert.Equal(new[] { "Doduo", "Doduo", "Kricketot" }, slots.Take(3).Select(e => e.SpeciesName));
        Assert.Equal("Starly", EncounterSlots.Grass(area, new EncounterMoment { SwarmArea = "route_202" })[0].SpeciesName);

        // What is told of each place: Route 201's Doduo (Swarm_GetMapIdAndSpecies), all 22 with a species
        Assert.Equal("Doduo", Swarms.Species("route_201"));
        Assert.Equal("Route 201", Swarms.PlaceName("route_201"));
        Assert.All(Swarms.Areas, key => Assert.NotEmpty(Swarms.Species(key)));
    }

    [Fact]
    public void TheSwarmIsToldAndBegunByItsScript()
    {
        var host = new HeadlessScriptHost();
        host.Encounters.SwarmDaily = 21;
        host.Encounters.SwarmsOn = false;
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.SwarmNews)!);
        runner.RunToEnd();
        Assert.True(host.Encounters.SwarmsOn);
        Assert.True(host.Story.Has("FLAG_TALKED_TO_COUNTERPART_SISTER_WITH_NATIONAL_DEX"));
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.SwarmNews)!);
        runner.RunToEnd();
        string told = host.Transcript.Last(t => t.Text.Contains("crowd")).Text;
        Assert.Contains(Swarms.Species("eterna_forest"), told);
        Assert.Contains("Eterna Forest", told);
    }

    [Fact]
    public void TheGreatMarshDrawsADailySpeciesForEachOfItsAreasDuringASafariGame()
    {
        // ReplaceGreatMarshDailyEncounters: (marshDaily >> 5 × area) & 31 into the 32 of the lookout's list, the
        // National Pokédex's after it is had; slots 6 and 7 both
        var tables = SpecialEncounterTables.Sinnoh.GreatMarsh;
        Assert.Equal(32, tables.Local.Count);
        Assert.Equal(32, tables.National.Count);
        uint daily = (25u << 10) | 31u;
        Assert.Equal("Tangela", GreatMarsh.DailySpecies(tables, daily, "great_marsh_1", false));   // 31
        Assert.Equal("Wooper", GreatMarsh.DailySpecies(tables, daily, "great_marsh_2", false));    // 0
        Assert.Equal("Toxicroak", GreatMarsh.DailySpecies(tables, daily, "great_marsh_2", true));  // 0, the National list
        Assert.Equal("Tropius", GreatMarsh.DailySpecies(tables, daily, "great_marsh_3", false));   // 25
        Assert.Null(GreatMarsh.DailySpecies(tables, daily, "route_201", false));

        var marsh = Area("great_marsh_1");
        var slots = EncounterSlots.Grass(marsh, new EncounterMoment { MarshDaily = daily, Marsh = tables });
        Assert.Equal(new[] { "Tangela", "Tangela" }, slots.Skip(6).Take(2).Select(e => e.SpeciesName));
        Assert.Equal("Zubat", EncounterSlots.Grass(marsh, new EncounterMoment { Marsh = tables })[6].SpeciesName);
    }

    [Fact]
    public void MrBacklotBringsADifferentPokemonEachTimeAndItTakesTheFirstSlot()
    {
        // TrophyGarden_AddNewMon: drawn from 16 until it is neither of the two there; the first moves to the second
        var garden = SpecialEncounterTables.Sinnoh.TrophyGarden;
        Assert.Equal(16, garden.Count);
        var state = new SpecialEncounters();
        TrophyGardenRules.AddNew(state, garden, new Rolls(0, 3));
        Assert.Equal(3, state.TrophyFirst);
        Assert.Null(state.TrophySecond);
        TrophyGardenRules.AddNew(state, garden, new Rolls(0, 3, 3, 9));
        Assert.Equal(9, state.TrophyFirst);
        Assert.Equal(3, state.TrophySecond);
        TrophyGardenRules.AddNew(state, garden, new Rolls(0, 9, 3, 0));
        Assert.Equal((0, 9), (state.TrophyFirst, state.TrophySecond));

        // With the National Pokédex its two are met in the garden's slots 6 and 7 (WildEncounters_ReplaceTrophyGardenEncounters)
        var area = Area(EncounterSlots.TrophyGarden);
        var moment = new EncounterMoment { NationalDex = true, TrophyFirst = garden[0], TrophySecond = garden[9] };
        Assert.Equal(new[] { garden[0], garden[9] }, EncounterSlots.Grass(area, moment).Skip(6).Take(2).Select(e => e.SpeciesName));
        Assert.Equal("Zubat", EncounterSlots.Grass(area, moment with { NationalDex = false })[6].SpeciesName);
    }

    [Fact]
    public void ADailyFlagIsClearedByTheNextDay()
    {
        var story = new StoryState();
        story.Set("FLAG_DAILY_ADDED_TROPHY_GARDEN_MON");
        story.Set("FLAG_GAME_COMPLETED");
        story.ClearDaily();
        Assert.False(story.Has("FLAG_DAILY_ADDED_TROPHY_GARDEN_MON"));
        Assert.True(story.Has("FLAG_GAME_COMPLETED"));
    }

    // ================================================================== honey trees

    [Fact]
    public void AHoneyTreeDrawsItsGroupWhenSlatheredAndIsReadySixHoursLater()
    {
        // HoneyTree_SlatherTree: 24 × 60 minutes; GetTreeEncounterGroup (a normal tree: < 10 nothing, < 30 group B,
        // else A), GetTreeEncounterSlot (< 5 slot 5 ... else 0), GetShakesFromGroup (group A: < 19 two, < 79 one)
        var state = new SpecialEncounters();
        int tree = HoneyTrees.IdOf("route_206")!.Value;
        Assert.Equal(2, tree);
        uint trainer = 0x12345678;   // its Munchlax trees are 18, 10, 2 and 15: tree 3 is an ordinary one
        HoneyTrees.Slather(state, 3, trainer, new Rolls(0, 50, 3, 50));
        var t = state.Trees[3];
        Assert.Equal((1440, 1, 5, 1), (t.MinutesLeft, t.Group, t.Slot, t.Shakes));
        Assert.Equal(3, state.LastSlathered);
        Assert.Equal("Aipom", HoneyTrees.SpeciesAt(t, SpecialEncounterTables.Sinnoh.HoneyTrees));

        // SixHoursSinceSlathered: 0 < minutes ≤ 18 × 60
        Assert.Equal(HoneyTreeStatus.Slathered, HoneyTrees.Status(t));
        HoneyTrees.MinutesPass(state, 359);
        Assert.Equal(HoneyTreeStatus.Slathered, HoneyTrees.Status(t));
        HoneyTrees.MinutesPass(state, 1);
        Assert.Equal(HoneyTreeStatus.Ready, HoneyTrees.Status(t));
        Assert.True(HoneyTrees.Shaking(t));
        HoneyTrees.MinutesPass(state, 2000);
        Assert.Equal(HoneyTreeStatus.Bare, HoneyTrees.Status(t));

        // The same tree again keeps its group nine times in ten, with a new slot and shake
        HoneyTrees.Slather(state, 3, trainer, new Rolls(0, 10, 70, 99));
        Assert.Equal((1440, 1, 0, 3), (t.MinutesLeft, t.Group, t.Slot, t.Shakes));
        // A tree that draws nothing has no honey at once
        HoneyTrees.Slather(state, 4, trainer, new Rolls(0, 5, 50));
        Assert.Equal((0, 0), (state.Trees[4].MinutesLeft, state.Trees[4].Group));
        Assert.Equal(HoneyTreeStatus.Bare, HoneyTrees.Status(state.Trees[4]));
    }

    [Fact]
    public void FourOfTheTreesAreMunchlaxTreesByTheTrainersNumber()
    {
        // IsMunchlaxTree: each byte of the 32-bit number % 21, a repeat moved on to the next tree (wrapping to 0)
        Assert.Equal(new[] { 18, 10, 2, 15 }, HoneyTrees.MunchlaxTrees(0x12345678));
        Assert.Equal(new[] { 0, 1, 2, 3 }, HoneyTrees.MunchlaxTrees(0));
        Assert.Equal(new[] { 20, 0, 1, 2 }, HoneyTrees.MunchlaxTrees(0x14141414));
        // A Munchlax tree's groups: < 1 Munchlax, < 10 nothing, < 30 A, else B
        Assert.Equal(3, HoneyTrees.DrawGroup(true, new Rolls(0, 0)));
        Assert.Equal(0, HoneyTrees.DrawGroup(true, new Rolls(0, 9)));
        Assert.Equal(1, HoneyTrees.DrawGroup(true, new Rolls(0, 29)));
        Assert.Equal(2, HoneyTrees.DrawGroup(true, new Rolls(0, 30)));
        Assert.Equal(2, HoneyTrees.DrawGroup(false, new Rolls(0, 29)));
        var state = new SpecialEncounters();
        HoneyTrees.Slather(state, 18, 0x12345678, new Rolls(0, 0, 0, 50));
        Assert.Equal("Munchlax", HoneyTrees.SpeciesAt(state.Trees[18], SpecialEncounterTables.Sinnoh.HoneyTrees));
        Assert.Equal(3, state.Trees[18].Shakes);   // Munchlax: from 7 three shakes
    }

    [Fact]
    public void WhatComesToAHoneyTreeIsFromLevelFiveToFifteenAndTakesTheHoney()
    {
        // CreateWildMon_HoneyTree: 5 + LCRNG_RandMod(11); Hustle, Vital Spirit, Pressure: 15 one time in two
        var state = new SpecialEncounters();
        state.Trees[0].MinutesLeft = 600;
        state.Trees[0].Group = 2;
        state.Trees[0].Slot = 5;
        var met = HoneyTrees.Meet(state, 0, SpecialEncounterTables.Sinnoh.HoneyTrees, null, new Rolls(0, 4));
        Assert.Equal(("Heracross", 9), (met.SpeciesName, met.MinLevel));
        Assert.Equal(0, state.Trees[0].MinutesLeft);
        state.Trees[0].MinutesLeft = 600;
        var hustle = new WildLead("Hustle", 20, Nature.Hardy, Gender.Male);
        Assert.Equal(15, HoneyTrees.Meet(state, 0, SpecialEncounterTables.Sinnoh.HoneyTrees, hustle, new Rolls(0, 4, 1)).MinLevel);
        Assert.Equal(9, HoneyTrees.Meet(state, 0, SpecialEncounterTables.Sinnoh.HoneyTrees, hustle, new Rolls(0, 4, 0)).MinLevel);
    }

    [Fact]
    public void TheHoneyTreeScriptSlathersAndBattles()
    {
        var bag = new Inventory();
        var host = new HeadlessScriptHost(bag: bag) { HoneyTreeFaced = 5, Rng = new Rolls(0, 50, 3, 50) };
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        var honeyTree = ScriptLibrary.Default.Find(FieldScripts.HoneyTree)!;

        // No Honey: a line, and nothing else
        runner.Start(honeyTree);
        runner.RunToEnd();
        Assert.Empty(host.Asked);
        Assert.Equal(0, host.Encounters.Trees[5].MinutesLeft);

        // With Honey: asked, one is used, the tree slathered
        bag.AddItem(ItemDatabase.Get("Honey")!, 2);
        runner.Start(honeyTree);
        runner.RunToEnd();
        Assert.Single(host.Asked);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Honey")!));
        Assert.Equal(1440, host.Encounters.Trees[5].MinutesLeft);

        // Six hours on, the Pokémon waiting there is battled at once, the honey goes, and the last Honey is offered
        HoneyTrees.MinutesPass(host.Encounters, 400);
        host.Rng = new Rolls(0, 0);
        runner.Start(honeyTree);
        runner.RunToEnd();
        Assert.Contains(host.Log, l => l.StartsWith("wildbattle Aipom 5"));
        Assert.Equal(2, host.Asked.Count);
    }

    [Fact]
    public void AHoneyTreeIsFacedFromTheSouth()
    {
        // HoneyTree_TryInteract: facing north, the tree's model on the tile ahead, in a place with a tree
        var sinnoh = MapDatabase.Get("Sinnoh");
        var area = sinnoh.FindArea("route_205_south")!;
        var tree = sinnoh.Props.First(p => p.Type == PropType.HoneyTree && sinnoh.AreaAt(p.X, p.Y) == area);
        int x = tree.X + tree.Width / 2, y = tree.Y + tree.Depth;
        Assert.Equal(area, sinnoh.AreaAt(x, y));
        Assert.Equal(0, HoneyTrees.Faced(sinnoh, x, y, Direction.Up));
        Assert.Null(HoneyTrees.Faced(sinnoh, x, y, Direction.Left));
        Assert.Null(HoneyTrees.Faced(sinnoh, x, y + 1, Direction.Up));
    }

    [Fact]
    public void EveryHoneyTreesPlaceHasOne()
    {
        // The 21 places of sHoneyTreeMapHeaderIDs each have their tree among the world's models
        var maps = MapDatabase.MapNames.Select(MapDatabase.Get).Distinct().ToList();
        foreach (string key in HoneyTrees.Areas)
        {
            var map = maps.FirstOrDefault(m => m.FindArea(key) is { Open: true });
            if (map == null) continue;   // a place not open yet
            Assert.True(map.Props.Any(p => p.Type == PropType.HoneyTree && map.AreaAt(p.X, p.Y)?.Key == key), key);
        }
    }

    // ================================================================== the Poké Radar

    [Fact]
    public void TheRadarNeedsItsBatteryTallGrassAndTheGroundUnderfoot()
    {
        // CanUsePokeRadar: no partner, not cycling, standing in TILE_BEHAVIOR_TALL_GRASS; RefreshRadarChain: 50 steps
        var map = GrassMap(Area());
        var state = new SpecialEncounters();
        var radar = new RadarChain();
        Assert.Equal(RadarUse.NotCharged, radar.Use(state, map, 10, 10, 0, TravelMode.OnFoot, false, new Rolls(0)));
        for (int i = 0; i < 60; i++) RadarChain.Charge(state);
        Assert.Equal(50, state.RadarCharge);
        Assert.Equal(RadarUse.CantUse, radar.Use(state, map, 10, 10, 0, TravelMode.Cycling, false, new Rolls(0)));
        Assert.Equal(RadarUse.CantUse, radar.Use(state, map, 10, 10, 0, TravelMode.OnFoot, true, new Rolls(0)));
        map.SetGroundTile(10, 10, TileType.Grass);
        Assert.Equal(RadarUse.CantUse, radar.Use(state, map, 10, 10, 0, TravelMode.OnFoot, false, new Rolls(0)));
        map.SetGroundTile(10, 10, TileType.TallGrass);
        Assert.Equal(RadarUse.Shaking, radar.Use(state, map, 10, 10, 0, TravelMode.OnFoot, false, new Rolls(0)));
        Assert.Equal(0, state.RadarCharge);
        Assert.True(radar.Active);
    }

    [Fact]
    public void ThePatchesAreOnFourRingsRoundThePlayer()
    {
        // RadarSpawnPatches: ring r has 32, 24, 16, 8 tiles on a square 9 - 2r across, the roll's band its top row,
        // its bottom row, or its sides; then SetupGrassPatches(BATTLE_RESULT_WIN): 88, 68, 48, 28 to go on
        var map = GrassMap(Area());
        var state = new SpecialEncounters { RadarCharge = 50 };
        var radar = new RadarChain();
        radar.Use(state, map, 10, 10, 0, TravelMode.OnFoot, false, new Rolls(0, 0, 8, 11, 7, 0, 99, 60, 10, 50, 10));
        Assert.Equal(new[] { (6, 6), (8, 13), (12, 9), (11, 10) }, radar.Patches.Select(p => (p.X, p.Y)));
        Assert.All(radar.Patches, p => Assert.True(p.Active));
        Assert.Equal(new[] { true, false, true, false }, radar.Patches.Select(p => p.ContinuesChain));
        Assert.Equal(new[] { PatchShake.Soft, PatchShake.Hard, PatchShake.Soft, PatchShake.Soft }, radar.Patches.Select(p => p.Shake));
        Assert.All(radar.Patches, p => Assert.False(p.Shiny));   // no chain yet: CheckPatchShiny is false at 0

        // A patch only shakes on tall grass of the player's own height and place
        map.SetGroundTile(6, 6, TileType.Grass);
        var other = new RadarChain();
        other.Spawn(map, 10, 10, 0, new Rolls(0, 0, 8, 11, 7));
        Assert.False(other.Patches[0].Active);
        Assert.True(other.Patches[1].Active);
    }

    [Fact]
    public void AChainGrowsWithEachPatchThatGoesOnWithIt()
    {
        var map = GrassMap(Area());
        var state = new SpecialEncounters { RadarCharge = 50 };
        var radar = new RadarChain();
        // Patches as above; every patch's species is Bidoof at level 4 after the first draw
        radar.Use(state, map, 10, 10, 0, TravelMode.OnFoot, false, new Rolls(0, 0, 8, 11, 7, 0, 99, 60, 10, 50, 10));
        var moment = new EncounterMoment { Radar = radar };

        // The first patch walked into: always a Pokémon, whatever the odds; it starts the chain (CreateWildMon_FromRadarNoChain)
        var first = radar.StepOnto(6, 6)!.Value;
        Assert.False(first.KeepsChain);
        var met = radar.Meet(map.WildAt(6, 6, moment: moment).Table, map.AreaAt(6, 6), first, null, map, 6, 6, 0, new Rolls(0, 25));
        Assert.Equal(("Bidoof", 4), (met.SpeciesName, met.MinLevel));   // roll 25: slot 1
        Assert.Equal(1, radar.Count);
        Assert.Equal("Bidoof", radar.Species);

        // Won: the patches the Pokémon's meeting set round the player are set going again: for each, whether it goes
        // on (all four do) and, now that there is a chain, whether it sparkles (none does)
        radar.AfterBattle(won: true, caught: false, new Rolls(0, 0, 8, 11, 7, 0, 1, 0, 1));
        Assert.All(radar.Patches, p => Assert.True(p.ContinuesChain));
        var second = radar.StepOnto(radar.Patches[2].X, radar.Patches[2].Y)!.Value;
        Assert.True(second.KeepsChain);
        Assert.Equal(2, radar.Count);
        met = radar.Meet(map.WildAt(10, 10, moment: moment).Table, map.AreaAt(10, 10), second, null, map, 10, 10, 0, new Rolls(0));
        Assert.Equal(("Bidoof", 4), (met.SpeciesName, met.MinLevel));
    }

    [Fact]
    public void APatchThatDoesntGoOnDrawsAfreshAndAnyOtherSpeciesEndsTheChain()
    {
        var map = GrassMap(Area());
        var radar = new RadarChain();
        var state = new SpecialEncounters { RadarCharge = 50 };
        radar.Use(state, map, 10, 10, 0, TravelMode.OnFoot, false, new Rolls(0, 0, 8, 11, 7, 0, 99, 60, 10, 50, 10));
        var first = radar.StepOnto(6, 6)!.Value;
        // Met, and the patches set round the player again, on the same four tiles as before
        radar.Meet(map.WildAt(6, 6).Table, map.AreaAt(6, 6), first, null, map, 10, 10, 0, new Rolls(0, 25, 0, 8, 11, 7));
        // Won: the first goes on (and doesn't sparkle), the second doesn't and shakes hard, the third goes on, the last doesn't
        radar.AfterBattle(true, false, new Rolls(0, 0, 99, 90, 60, 10, 99, 50, 10));
        // Patch 1 doesn't go on and shakes hard: the radar's species are in slots 4, 5, 10 and 11, and a Nidoran
        // drawn (roll 65: slot 4) ends the chain
        var hard = radar.StepOnto(8, 13)!.Value;
        Assert.Equal((PatchShake.Hard, false), (hard.Shake, hard.KeepsChain));
        var met = radar.Meet(map.WildAt(8, 13).Table, map.AreaAt(8, 13), hard, null, map, 8, 13, 0, new Rolls(0, 65));
        Assert.Equal("Nidoran♂", met.SpeciesName);
        Assert.Equal(0, radar.Count);
        Assert.Null(radar.Species);
    }

    [Fact]
    public void ARunOrAnythingButAPatchEndsTheChainAndAPatchCanSparkle()
    {
        // FieldTask_WildEncounter: a chain goes on only from a win or a catch
        var map = GrassMap(Area());
        var radar = new RadarChain();
        Assert.True(radar.Spawn(map, 10, 10, 0, new Rolls(0)));
        radar.SetUp(false, new Rolls(0));
        radar.StepOnto(radar.Patches[0].X, radar.Patches[0].Y);
        radar.AfterBattle(won: false, caught: false, new Rolls(0));
        Assert.False(radar.Active);

        // CheckPatchShiny: 8200 - 200 × chain, never better than 200; nothing at no chain
        Assert.False(RadarChain.ShinyPatch(0, new Rolls(0)));
        Assert.True(RadarChain.ShinyPatch(1, new Rolls(0)));
        Assert.False(RadarChain.ShinyPatch(1, new Rolls(1)));
        var odds = new List<int>();
        var spy = new SpyRandom(odds);
        RadarChain.ShinyPatch(1, spy);
        RadarChain.ShinyPatch(40, spy);
        RadarChain.ShinyPatch(999, spy);
        Assert.Equal(new[] { 8000, 200, 200 }, odds);

        // Walking off out of sight stops every patch: the chain ends (PokeRadar_ClearIfAllOutOfView)
        Assert.True(radar.Spawn(map, 10, 10, 0, new Rolls(0)));
        Assert.False(radar.KeepInView(12, 10));
        Assert.True(radar.KeepInView(30, 30));
        Assert.False(radar.Active);
    }

    private sealed class SpyRandom(List<int> asked) : Random
    {
        public override int Next(int maxValue)
        {
            asked.Add(maxValue);
            return 1;
        }
    }

    [Fact]
    public void AStepIntoAPatchAlwaysMeetsItsPokemon()
    {
        // PokeRadar_ShouldDoRadarEncounter sets gettingEncounter whatever ShouldGetRandomEncounter said
        var map = GrassMap(Area());
        var radar = new RadarChain();
        radar.Spawn(map, 10, 10, 0, new Rolls(0));
        radar.SetUp(false, new Rolls(0));
        var moment = new EncounterMoment { Radar = radar };
        var met = map.RollWildEncounter(radar.Patches[3].X, radar.Patches[3].Y, new EncounterSteps(), moment: moment);
        Assert.NotNull(met);
        Assert.Equal(1, radar.Count);
    }

    // ================================================================== roaming Pokémon

    [Fact]
    public void ARoamerSetLooseIsMadeAtFullHealthSomewhereNew()
    {
        // RoamingPokemon_ActivateSlot: Mesprit at 50, Cresselia at 50, the birds at 60; HP full, no condition
        var state = new SpecialEncounters { PreviousPlace = "route_202" };
        Roamers.SetLoose(state, Roamers.SlotOf("Mesprit")!.Value, new Random(3));
        var mesprit = state.Roamers[0];
        Assert.True(mesprit.Active);
        Assert.Equal(50, mesprit.Level);
        var made = Roamers.ToBattle(mesprit);
        Assert.Equal(made.MaxHP, made.CurrentHP);
        Assert.NotEqual("route_202", Roamers.PlaceOf(mesprit));
        Assert.NotEqual("route_201", Roamers.PlaceOf(mesprit));   // MoveRoamerRandom never stays where it was (index 0 at first)
        Assert.Equal(new[] { 50, 50, 40, 60, 60, 60 }, Roamers.Slots.Select(s => s.Level));
        Assert.Null(Roamers.SlotOf("Bidoof"));
    }

    [Fact]
    public void ARoamerMovesNearbyNeverToWhereThePlayerJustWas()
    {
        // MoveRoamerNearby: Route 201's neighbours are 202 and 219; the player just left 202
        var state = new SpecialEncounters { PreviousPlace = "route_202" };
        var roamer = state.Roamers[0];
        roamer.Active = true;
        roamer.Route = 0;
        Roamers.MoveNearby(state, 0, new Rolls(0, 0, 1));
        Assert.Equal("route_219", Roamers.PlaceOf(roamer));

        // Route 217 has one neighbour, 216: the player just left it, so anywhere but there and where it is
        roamer.Route = Array.IndexOf(Roamers.Routes, "route_217");
        state.PreviousPlace = "route_216";
        Roamers.MoveNearby(state, 0, new Rolls(0, 20, 21, 5));
        Assert.Equal("route_205_south", Roamers.PlaceOf(roamer));

        // Walking into another place: one time in sixteen anywhere (RoamingPokemon_MoveAllLocations)
        state.CurrentPlace = "route_201";
        Roamers.PlayerWalkedInto(state, "route_202", new Rolls(0, 0, 7));
        Assert.Equal(("route_201", "route_202"), (state.PreviousPlace, state.CurrentPlace));
        Assert.Equal("route_206", Roamers.PlaceOf(roamer));
        Roamers.PlayerWalkedInto(state, "route_203", new Rolls(0, 1, 0));
        Assert.Equal("route_205_north", Roamers.PlaceOf(roamer));   // 206's first neighbour
    }

    [Fact]
    public void ARoamerWhereThePlayerIsIsMetOneTimeInTwoAndARepelKeepsItAway()
    {
        // TryEncounterRoamer: LCRNG_RandMod(2) == 0 is nothing; RepelPreventsEncounter by its level
        var state = new SpecialEncounters();
        state.Roamers[1].Active = true;
        state.Roamers[1].Route = 0;
        Assert.Null(Roamers.MeetHere(state, "route_201", new Rolls(0, 0)));
        Assert.Equal(1, Roamers.MeetHere(state, "route_201", new Rolls(0, 1)));
        Assert.Null(Roamers.MeetHere(state, "route_202", new Rolls(1)));

        var area = Area();
        var map = GrassMap(area);
        var moment = new EncounterMoment { State = state };
        var steps = new EncounterSteps();
        var met = new List<WildEncounterEntry>();
        for (int i = 0; i < 3000; i++)
            if (map.RollWildEncounter(5, 5, steps, moment: moment) is { } wild) met.Add(wild);
        Assert.Contains(met, m => m.Roamer == 1 && m.SpeciesName == "Cresselia");
        Assert.Contains(met, m => m.Roamer == null);
        // A Repel turns it away only from a first Pokémon above its level 50: at 50 the roamer is all that comes, at 51 nothing
        var even = new WildLead("Run Away", 50, Nature.Hardy, Gender.Male, RepelLevel: 50);
        var under = new List<WildEncounterEntry>();
        for (int i = 0; i < 3000; i++)
            if (map.RollWildEncounter(5, 5, steps, lead: even, moment: moment) is { } wild) under.Add(wild);
        Assert.NotEmpty(under);
        Assert.All(under, m => Assert.Equal(1, m.Roamer));
        var above = even with { RepelLevel = 51 };
        for (int i = 0; i < 3000; i++)
            Assert.Null(map.RollWildEncounter(5, 5, steps, lead: above, moment: moment));
        // Beside a partner it isn't met at all
        for (int i = 0; i < 3000; i++)
            Assert.Null(map.RollWildEncounter(5, 5, steps, moment: moment with { Partner = true })?.Roamer);
    }

    [Fact]
    public void ARoamerKeepsWhatABattleLeftItAndIsGoneOnceBeatenOrCaught()
    {
        // RoamerAfterBattle_UpdateRoamers
        var story = new StoryState();
        var state = new SpecialEncounters();
        Roamers.SetLoose(state, 0, new Random(1));
        string here = Roamers.PlaceOf(state.Roamers[0])!;
        var foe = Roamers.ToBattle(state.Roamers[0]);
        foe.CurrentHP = 17;
        foe.Status = StatusCondition.Sleep;
        Roamers.AfterBattle(state, foe, won: false, caught: false, here, story, new Random(2));
        Assert.Equal((17, (int)StatusCondition.Sleep), (state.Roamers[0].Pokemon!.CurrentHP, state.Roamers[0].Pokemon!.Status));
        Assert.NotEqual(here, Roamers.PlaceOf(state.Roamers[0]));   // all roamers there move off

        foe.CurrentHP = 0;
        Roamers.AfterBattle(state, foe, won: true, caught: false, here, story, new Random(2));
        Assert.False(state.Roamers[0].Active);
        Assert.Equal(Roamers.Defeated, story.Var(Roamers.StateVariable("Mesprit")));

        Roamers.SetLoose(state, 1, new Random(1));
        Roamers.AfterBattle(state, Roamers.ToBattle(state.Roamers[1]), won: false, caught: true, null, story, new Random(2));
        Assert.Equal(Roamers.Captured, story.Var("VAR_ROAMING_CRESSELIA_STATE"));

        // Any other wild battle: three times in ten the roamers where the player is move off
        Roamers.SetLoose(state, 3, new Random(1));
        string moltres = Roamers.PlaceOf(state.Roamers[3])!;
        Roamers.AfterBattle(state, Mon("Bidoof", 3), false, false, moltres, story, new Rolls(0, 30));
        Assert.Equal(moltres, Roamers.PlaceOf(state.Roamers[3]));
        Roamers.AfterBattle(state, Mon("Bidoof", 3), false, false, moltres, story, new Rolls(0, 29, 0, 1, 2));
        Assert.NotEqual(moltres, Roamers.PlaceOf(state.Roamers[3]));
    }

    [Fact]
    public void AScriptSetsARoamerLoose()
    {
        var host = new HeadlessScriptHost();
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptParser.Parse("test", """
            script Loose
              roamer start "Mesprit"
              end
            """)[0]);
        runner.RunToEnd();
        Assert.True(host.Encounters.Roamers[0].Active);
        Assert.Throws<ScriptException>(() => ScriptParser.Parse("test", "script Bad\n  roamer start \"Bidoof\"\n"));
    }

    // ================================================================== Feebas

    [Fact]
    public void FeebasIsOnFourTilesOfTheLakeThatTheDaysNumberPicks()
    {
        // PlayerAvatar_IsFacingFeebasTile: 528 tiles in four groups of 132; tile group × i + byte i % 132, the high
        // byte first
        var lake = SpecialEncounterTables.FeebasTiles;
        Assert.Equal(528, lake.Count);
        var today = Feebas.TilesToday(lake, 0x01020304);
        Assert.Equal(new[] { lake[1], lake[134], lake[267], lake[400] }, today);
        // Half the time nothing is checked at all
        Assert.False(Feebas.Bites(lake, 0x01020304, today[0].X, today[0].Y, new Rolls(0, 0)));
        Assert.True(Feebas.Bites(lake, 0x01020304, today[0].X, today[0].Y, new Rolls(0, 1)));
        Assert.False(Feebas.Bites(lake, 0x01020304, lake[2].X, lake[2].Y, new Rolls(0, 1)));
    }

    [Fact]
    public void FeebassTilesAreTheLakesWater()
    {
        var feebas = SpecialEncounterTables.Sinnoh.Feebas;
        Assert.Equal(("mt_coronet_b1f", "Feebas"), (feebas.Area, feebas.Species));
        var map = MapDatabase.Get("MtCoronetB1F");
        Assert.Equal("MtCoronetB1F", map.Name);
        foreach (var (x, y) in SpecialEncounterTables.FeebasTiles)
        {
            Assert.Equal("mt_coronet_b1f", map.AreaAt(x, y)?.Key);
            Assert.True(TileBehaviors.IsSurfable(map.BehaviourAt(x, y)), $"({x},{y}) is not water");
        }

        // A rod cast there hooks Feebas from 10 to 20 (LoadFeebasLevelRange), whatever the rod's own table
        var state = new SpecialEncounters { DailyNumber = 0x01020304 };
        var (fx, fy) = Feebas.TilesToday(SpecialEncounterTables.FeebasTiles, state.DailyNumber)[0];
        var hooked = new HashSet<string>();
        for (int i = 0; i < 400; i++)
            if (map.Fish(fx, fy, FishingRod.Super, null, new EncounterMoment { State = state }) is { } fish)
            {
                hooked.Add(fish.SpeciesName);
                if (fish.SpeciesName == "Feebas") Assert.InRange(fish.MinLevel, 10, 20);
            }
        Assert.Contains("Feebas", hooked);
        Assert.Contains("Gyarados", hooked);
    }

    // ================================================================== poison in the field

    [Fact]
    public void PoisonBitesEveryFourthStepAndLeavesOneHitPoint()
    {
        // Field_UpdatePoison: (steps + 1) % 4; Pokemon_DoPoisonDamage: hp > 1 loses one; at 1, FLDPSN_FAINTED and
        // FRIENDSHIP_EVENT_POISON_SURVIVE (-5 below 200)
        var sick = Mon("Bidoof", 10);
        sick.CurrentHP = 3;
        sick.Status = StatusCondition.Toxic;
        sick.Friendship = 70;
        var well = Mon("Starly", 10);
        var party = PartyOf(sick, well);
        var state = new SpecialEncounters();
        var steps = Enumerable.Range(0, 8).Select(_ => FieldPoison.Step(state, party, Ruleset.Platinum)).ToList();
        Assert.Equal(new[] { PoisonStep.None, PoisonStep.None, PoisonStep.None, PoisonStep.Hurt, PoisonStep.None, PoisonStep.None, PoisonStep.None, PoisonStep.Survived }, steps);
        Assert.Equal(1, sick.CurrentHP);
        Assert.Equal(65, sick.Friendship);
        Assert.Equal(well.MaxHP, well.CurrentHP);

        // Pokemon_TrySurvivePoison cures it; by the modern rules poison does nothing outside battle
        Assert.True(FieldPoison.TrySurvive(sick));
        Assert.Equal(StatusCondition.None, sick.Status);
        well.Status = StatusCondition.Poison;
        for (int i = 0; i < 8; i++) Assert.Equal(PoisonStep.None, FieldPoison.Step(state, party, Ruleset.Modern));
        Assert.Equal(well.MaxHP, well.CurrentHP);
    }

    [Fact]
    public void TheSurvivorsAreNamedAndCured()
    {
        var first = Mon("Bidoof", 10);
        var second = Mon("Starly", 10);
        var third = Mon("Shinx", 10);
        first.CurrentHP = 1;
        first.Status = StatusCondition.Poison;
        third.CurrentHP = 1;
        third.Status = StatusCondition.Toxic;
        var host = new HeadlessScriptHost(party: PartyOf(first, second, third));
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(FieldScripts.PoisonSurvived)!);
        runner.RunToEnd();
        Assert.Equal(StatusCondition.None, first.Status);
        Assert.Equal(StatusCondition.None, third.Status);
        Assert.Equal(2, host.Transcript.Count(t => t.Text.Contains("held on through the poison")));
        Assert.Contains(host.Transcript, t => t.Text.StartsWith("Shinx"));
        Assert.Equal(0, host.Story.Var("VAR_POISON_SURVIVOR"));
    }

    // ================================================================== kept in the save

    [Fact]
    public void TheWildPokemonsStateIsSavedWhole()
    {
        var state = SpecialEncounters.NewGame(new Random(5));
        state.SwarmsOn = true;
        state.TrophyFirst = 4;
        state.Trees[7].MinutesLeft = 900;
        state.Trees[7].Group = 2;
        state.RadarCharge = 33;
        state.PoisonSteps = 2;
        Roamers.SetLoose(state, 0, new Random(6));
        var save = new SaveData { Encounters = state };
        var back = GameDataFiles.Deserialize<SaveData>(GameDataFiles.Serialize(save)).Encounters!;
        Assert.Equal(JsonSerializer.Serialize(state), JsonSerializer.Serialize(back));
        Assert.Equal(state.Roamers[0].Pokemon!.IvSpeed, back.Roamers[0].Pokemon!.IvSpeed);
    }
}
