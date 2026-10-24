using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 18 · W4, the level cap past 100 (the user's option B of 2026-10-21): 100 by Platinum's rules and 260 by the
/// modern ones, the EXP curves past 100 (Erratic's and Fluctuating's by a rule of our own), what stopped at 100, and
/// each later region's levels mapped for a team that crossed into it. Ruleset.Current stays Platinum's in every test.
/// </summary>
public class LevelCapTests
{
    private static Pokemon Made(string species, int level) =>
        new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    private static Pokemon Weak(Pokemon p)
    {
        p.CurrentHP = 1;
        return p;
    }

    // ================================================================== the cap

    [Fact]
    public void TheCapIsAHundredByPlatinumsRulesAndTwoHundredAndSixtyByTheModernOnes()
    {
        Assert.Equal(100, Ruleset.Platinum.LevelCap);
        Assert.Equal(260, Ruleset.Modern.LevelCap);
        Assert.Equal(Ruleset.HighestLevel, Ruleset.Modern.LevelCap);
        // The game in progress is Platinum's in a test, as Ruleset.Current always is
        Assert.Equal(100, Pokemon.Cap);
    }

    [Fact]
    public void APokemonPastAHundredKeepsItsLevelAndASaveLoadsItAsItWas()
    {
        var p = Made("Garchomp", 187);
        Assert.Equal(187, p.Level);
        Assert.Equal(Pokemon.GetExpForLevel(187, p.Species.GrowthRate), p.CurrentExp);
        Assert.True(p.MaxHP > Made("Garchomp", 100).MaxHP);

        var back = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal((187, p.CurrentExp, p.MaxHP, p.Attack), (back.Level, back.CurrentExp, back.MaxHP, back.Attack));

        // Nothing is made past the highest level any rules allow
        Assert.Equal(Ruleset.HighestLevel, Made("Garchomp", 999).Level);
    }

    // ================================================================== the curves

    [Theory]
    [InlineData(GrowthRate.Fast)]
    [InlineData(GrowthRate.MediumFast)]
    [InlineData(GrowthRate.MediumSlow)]
    [InlineData(GrowthRate.Slow)]
    [InlineData(GrowthRate.Erratic)]
    [InlineData(GrowthRate.Fluctuating)]
    public void EveryCurveRisesLevelByLevelToTheHighestLevel(GrowthRate rate)
    {
        for (int level = 2; level <= Ruleset.HighestLevel; level++)
            Assert.True(Pokemon.GetExpForLevel(level, rate) > Pokemon.GetExpForLevel(level - 1, rate), $"{rate} at {level}");
    }

    [Fact]
    public void ErraticAndFluctuatingKeepTheirShareOfMediumFastPastAHundred()
    {
        // Platinum's own at 100: Erratic 100³ × (160 − 100) / 100, Fluctuating 100³ × (50 + 32) / 50
        Assert.Equal(600_000, Pokemon.GetExpForLevel(100, GrowthRate.Erratic));
        Assert.Equal(1_640_000, Pokemon.GetExpForLevel(100, GrowthRate.Fluctuating));
        // Past it 600,000 × (level / 100)³ and 1,640,000 × (level / 100)³, rounded down: 101³ is 1,030,301
        Assert.Equal(618_180, Pokemon.GetExpForLevel(101, GrowthRate.Erratic));
        Assert.Equal(1_689_693, Pokemon.GetExpForLevel(101, GrowthRate.Fluctuating));
        // Where Erratic's own last branch would have turned to nothing (160) and below it (200)
        Assert.Equal(2_457_600, Pokemon.GetExpForLevel(160, GrowthRate.Erratic));
        Assert.Equal(4_800_000, Pokemon.GetExpForLevel(200, GrowthRate.Erratic));
        Assert.Equal(13_120_000, Pokemon.GetExpForLevel(200, GrowthRate.Fluctuating));
    }

    [Fact]
    public void PlatinumsCurvesUpToAHundredAreAsTheyWere()
    {
        // Pokemon_GetExpToLevel's tables, read from its formulas
        Assert.Equal(125_000, Pokemon.GetExpForLevel(50, GrowthRate.Erratic));
        Assert.Equal(591_882, Pokemon.GetExpForLevel(99, GrowthRate.Erratic));
        Assert.Equal(46_656, Pokemon.GetExpForLevel(36, GrowthRate.Fluctuating));
        Assert.Equal(1_059_860, Pokemon.GetExpForLevel(100, GrowthRate.MediumSlow));
        Assert.Equal(1_250_000, Pokemon.GetExpForLevel(100, GrowthRate.Slow));
    }

    [Fact]
    public void TheCurvesAtTheHighestLevelFitTheirNumbers()
    {
        foreach (var rate in Enum.GetValues<GrowthRate>())
            Assert.InRange(Pokemon.GetExpForLevel(Ruleset.HighestLevel, rate), 1, 40_000_000);
    }

    // ================================================================== EXP and what stopped at 100

    [Fact]
    public void ExpStopsAtTheCapItIsGiven()
    {
        // The game's own cap, Platinum's in a test: nothing past 100
        var platinum = Made("Machop", 100);
        Assert.False(platinum.GainExp(5_000_000, out _, out _));
        Assert.Equal(100, platinum.Level);

        // A battle's modern rules hand it their cap
        var modern = Made("Machop", 100);
        Assert.True(modern.GainExp(Pokemon.GetExpForLevel(150, modern.Species.GrowthRate) - modern.CurrentExp, out _, out _, Ruleset.Modern.LevelCap));
        Assert.Equal(150, modern.Level);

        // And no further than it
        var top = Made("Machop", 259);
        top.GainExp(int.MaxValue / 2, out _, out _, Ruleset.Modern.LevelCap);
        Assert.Equal(260, top.Level);
        Assert.False(top.GainExp(1000, out _, out _, Ruleset.Modern.LevelCap));
    }

    [Fact]
    public void AModernBattleGivesALevel100PokemonItsExpAndPlatinumsGivesNone()
    {
        // ScaledExp(255, 260, 100): 255 × 260 / 5 × (530 / 370)^2.5 = 32,563, + 1; Fast's 100 to 101 is 0.8 × 30,301 = 24,240
        var modern = Mon("Clefairy", 100, "Pound");
        Turn(Wild(modern, Weak(Mon("Blissey", 260)), rules: Ruleset.Modern));
        Assert.Equal(101, modern.Level);

        var platinum = Mon("Clefairy", 100, "Pound");
        int exp = platinum.CurrentExp;
        Turn(Wild(platinum, Weak(Mon("Blissey", 260))));
        Assert.Equal((100, exp), (platinum.Level, platinum.CurrentExp));
    }

    [Fact]
    public void ARareCandyStopsAtTheGamesCap()
    {
        // Platinum's cap in a test: refused at 100, taken at 99
        Assert.False(ItemUse.WouldHelp(ItemDatabase.Get("Rare Candy")!, Made("Bidoof", 100)));
        Assert.True(ItemUse.WouldHelp(ItemDatabase.Get("Rare Candy")!, Made("Bidoof", 99)));
        Assert.True(Made("Bidoof", 100).AtTheCap);
    }

    [Fact]
    public void TheDamageFormulaHoldsAtTheHighestLevel()
    {
        // (2 × 260 / 5 + 2) = 106: 6,000 × 250 × 106 is past an int, so the formula works in a long
        Assert.Equal(6_000L * 250 * 106 / 1 / 50 + 2, Formulas.BaseDamage(260, 250, 6_000, 1, burned: false, spread: false));
        Assert.Equal(6_000L * 250 * 106 / 1 / 50 + 2, Formulas.ModernBaseDamage(260, 250, 6_000, 1));
        // And under 100 it is what it always was
        Assert.Equal(400 * 80 * 42 / 300 / 50 + 2, Formulas.BaseDamage(100, 80, 400, 300, burned: false, spread: false));
    }

    // ================================================================== a region's levels

    [Fact]
    public void AMappingIsAStraightLineBetweenItsPairsAndGoesOnBeyondThem()
    {
        var kanto = LevelMapping.From(new List<int[]> { new[] { 2, 62 }, new[] { 63, 104 } })!;
        Assert.Equal(62, kanto.Map(2));
        Assert.Equal(104, kanto.Map(63));
        // 62 + 30 × 42 / 61 = 82.66…, rounded half up
        Assert.Equal(83, kanto.Map(32));
        // Beyond the last pair on the same line: 62 + 73 × 42 / 61 = 112.26…
        Assert.Equal(112, kanto.Map(75));
        // And below the first: 62 − 42 / 61
        Assert.Equal(61, kanto.Map(1));

        // A single pair moves every level by the same amount
        Assert.Equal(12, LevelMapping.From(new List<int[]> { new[] { 5, 10 } })!.Map(7));
        // Held to the highest level
        Assert.Equal(Ruleset.HighestLevel, LevelMapping.From(new List<int[]> { new[] { 2, 250 }, new[] { 10, 260 } })!.Map(100));
        // None is no mapping
        Assert.Null(LevelMapping.From(null));
        Assert.Null(LevelMapping.From(new List<int[]>()));
    }

    [Fact]
    public void AMappingOutOfOrderOrThatBringsALevelLowerIsRefused()
    {
        Assert.Throws<FormatException>(() => LevelMapping.From(new List<int[]> { new[] { 10, 50 }, new[] { 5, 60 } }));
        Assert.Throws<FormatException>(() => LevelMapping.From(new List<int[]> { new[] { 5, 60 }, new[] { 10, 50 } }));
        Assert.Throws<FormatException>(() => LevelMapping.From(new List<int[]> { new[] { 5, 60, 7 } }));
        Assert.Throws<FormatException>(() => LevelMapping.From(new List<int[]> { new[] { 5, 300 } }));
    }

    [Fact]
    public void EveryRegionAfterSinnohClimbsFromTheChampionBeforeIt()
    {
        // Sinnoh's levels are Platinum's own, where every game begins
        Assert.Null(RegionDatabase.Get(RegionDatabase.Sinnoh)!.Levels);

        // The study's fair climb (W4-level-curve-study.md, section 4): each region's Champion, by its own game's level
        var champions = new (string Region, int Own, int Met)[]
        {
            (RegionDatabase.Kanto, 63, 104),
            (RegionDatabase.Johto, 50, 136),
            (RegionDatabase.Hoenn, 58, 162),
            (RegionDatabase.Unova, 59, 185)
        };
        int before = 62; // Cynthia's strongest
        foreach (var (region, own, met) in champions)
        {
            var levels = RegionDatabase.Get(region)!.Levels;
            Assert.NotNull(levels);
            Assert.Equal(met, levels!.Map(own));
            // The region's first route is met at the Champion's before it
            Assert.Equal(before, levels.Map(2));
            // And nothing of its own game's, up to 100, past the highest level
            Assert.All(Enumerable.Range(1, 100), l => Assert.InRange(levels.Map(l), 1, Ruleset.HighestLevel));
            before = met;
        }
    }

    [Fact]
    public void ATrainerRelevelledIsACopyAtTheNewLevels()
    {
        var trainer = new Trainer { Id = "kanto_leader_brock", Name = "Brock", TrainerClass = "Leader", Items = new() { "Full Restore" } };
        var geodude = Mon("Geodude", 12, "Tackle", "Defense Curl");
        var onix = Made("Onix", 14);
        trainer.Party.Add(geodude);
        trainer.Party.Add(onix);

        var met = trainer.Relevelled(l => l + 50);
        Assert.Equal(new[] { 62, 64 }, met.Party.Members.Select(p => p.Level));
        // The trainer of the map keeps their own
        Assert.Equal(new[] { 12, 14 }, trainer.Party.Members.Select(p => p.Level));
        Assert.DoesNotContain(met.Party.Members, p => trainer.Party.Members.Contains(p));
        Assert.NotSame(trainer.Items, met.Items);
        Assert.Equal(("Brock", "Leader"), (met.Name, met.TrainerClass));

        // Moves chosen for it are kept; what its level taught it is the new level's
        Assert.Equal(new[] { "Tackle", "Defense Curl" }, met.Party.Members[0].Moves.Select(m => m.Name));
        Assert.Equal(Made("Onix", 64).Moves.Select(m => m.Name), met.Party.Members[1].Moves.Select(m => m.Name));
        Assert.Equal((Made("Onix", 64).MaxHP, Made("Onix", 64).MaxHP), (met.Party.Members[1].MaxHP, met.Party.Members[1].CurrentHP));
    }

    [Fact]
    public void AnOlderSaveBeganWhereItStands()
    {
        Assert.Equal(RegionDatabase.Sinnoh, new SaveData { CurrentMapName = "Sinnoh" }.BeganInOrHere());
        Assert.Equal(RegionDatabase.Kanto, new SaveData { CurrentMapName = "Kanto", BeganIn = "Kanto" }.BeganInOrHere());
        Assert.Equal(RegionDatabase.Sinnoh, new SaveData { CurrentMapName = "Kanto", BeganIn = RegionDatabase.Sinnoh }.BeganInOrHere());
    }
}
