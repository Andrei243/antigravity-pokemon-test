using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using Xunit;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 46 · CP15, the cap to 300 (decision 3 of 2026-11-09; docs/mechanics/rulings.md, "The level cap past 100 and a
/// region's levels"): the modern rules' cap and the highest level are 300, a Pokémon at 300 is saved, copied and
/// recorded as it was, the curves rise to it, the damage formula holds at it, a staged foe may stand at it, and every
/// region before Carpathia keeps the levels and ceilings it had at 260. Ruleset.Current stays Platinum's in every test.
/// </summary>
public class LevelThreeHundredTests
{
    private static Pokemon Made(string species, int level) =>
        new(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);

    [Fact]
    public void TheModernRulesCapIsThreeHundredAndTheFormerTopIsKept()
    {
        Assert.Equal(300, Ruleset.HighestLevel);
        Assert.Equal(300, Ruleset.Modern.LevelCap);
        Assert.Equal(260, Ruleset.FormerHighestLevel);
        Assert.Equal(100, Ruleset.Platinum.LevelCap);
        Assert.Equal(300, Made("Garchomp", 300).Level);
        Assert.Equal(300, Made("Garchomp", 301).Level);
    }

    [Fact]
    public void ErraticAndFluctuatingAtThreeHundred()
    {
        // 600,000 × 3³ and 1,640,000 × 3³: the rule of our own past 100 (plan 18 · W4) carries on unchanged
        Assert.Equal(16_200_000, Pokemon.GetExpForLevel(300, GrowthRate.Erratic));
        Assert.Equal(44_280_000, Pokemon.GetExpForLevel(300, GrowthRate.Fluctuating));
        // The others' own formulas at 300: n³, 0.8 n³, 1.25 n³ and 1.2 n³ − 15 n² + 100 n − 140
        Assert.Equal(27_000_000, Pokemon.GetExpForLevel(300, GrowthRate.MediumFast));
        Assert.Equal(21_600_000, Pokemon.GetExpForLevel(300, GrowthRate.Fast));
        Assert.Equal(33_750_000, Pokemon.GetExpForLevel(300, GrowthRate.Slow));
        Assert.Equal(31_050_000 + 29_860, Pokemon.GetExpForLevel(300, GrowthRate.MediumSlow));
    }

    [Theory]
    [InlineData(GrowthRate.Fast)]
    [InlineData(GrowthRate.MediumFast)]
    [InlineData(GrowthRate.MediumSlow)]
    [InlineData(GrowthRate.Slow)]
    [InlineData(GrowthRate.Erratic)]
    [InlineData(GrowthRate.Fluctuating)]
    public void EveryCurveRisesToThreeHundredAndAPokemonMadeThereHasItsExp(GrowthRate rate)
    {
        for (int level = 2; level <= 300; level++)
            Assert.True(Pokemon.GetExpForLevel(level, rate) > Pokemon.GetExpForLevel(level - 1, rate), $"{rate} at {level}");
        var p = PokemonDatabase.GetAll().First(s => s.GrowthRate == rate);
        var made = new Pokemon(p, 300, Gender.Male, Nature.Hardy, false);
        Assert.Equal(Pokemon.GetExpForLevel(300, rate), made.CurrentExp);
    }

    [Fact]
    public void APokemonGrowsToThreeHundredAndNoFurther()
    {
        var p = Made("Machop", 299);
        p.GainExp(int.MaxValue / 2, out _, out _, Ruleset.Modern.LevelCap);
        Assert.Equal(300, p.Level);
        Assert.False(p.GainExp(1000, out _, out _, Ruleset.Modern.LevelCap));
        Assert.True(Made("Machop", 300).AtTheCap || Pokemon.Cap < 300);
    }

    [Fact]
    public void APokemonAtThreeHundredIsSavedAndCopiedAsItWas()
    {
        var p = Made("Blissey", 300);
        Assert.Equal(300, p.Level);
        Assert.InRange(p.MaxHP, 1800, 2200);

        var back = SavedPokemonData.FromPokemon(p).ToPokemon();
        Assert.Equal((300, p.CurrentExp, p.MaxHP, p.Attack, p.Speed), (back.Level, back.CurrentExp, back.MaxHP, back.Attack, back.Speed));

        var clone = p.Clone();
        Assert.Equal((300, p.CurrentExp, p.MaxHP), (clone.Level, clone.CurrentExp, clone.MaxHP));
        var other = Made("Blissey", 50);
        other.CopyStateFrom(p);
        Assert.Equal(300, other.Level);
    }

    [Fact]
    public void TheDamageFormulaHoldsAtThreeHundredWithoutOverflow()
    {
        // A whole hit through the calculator under both rules: positive, and stronger than the same at 260
        foreach (var rules in new[] { Ruleset.Platinum, Ruleset.Modern })
        {
            var strong = Mon("Rampardos", 300, "Head Smash");
            var weak = Mon("Shuckle", 300);
            int at300 = Damage(strong, weak, "Head Smash", rules: rules);
            int at260 = Damage(Mon("Rampardos", 260, "Head Smash"), weak, "Head Smash", rules: rules);
            Assert.True(at300 > at260 && at260 > 0, $"{rules}: {at260} at 260, {at300} at 300");
        }

        // The extreme of every step: the formula is held under its most, never wrapping to a negative number
        Assert.Equal(Formulas.MostDamage + 2, Formulas.ModernBaseDamage(300, 999, int.MaxValue, 1));
        Assert.Equal(Formulas.MostDamage + 2, Formulas.BaseDamage(300, 999, int.MaxValue, 1, burned: false, spread: false));
        Assert.Equal(Formulas.MostDamage, Formulas.ModernFinalDamage(Formulas.MostDamage, 0, 8192, 4f, false, 131072));
    }

    [Fact]
    public void AStagedFoeMayStandAtThreeHundred()
    {
        // CP25b's ??? fight: `wildbattle "Furtunaur" 300 hidelevel canlose` (the species is CP3's, so the line is read alone)
        var line = ScriptParser.Parse("test", "script S\n wildbattle \"Furtunaur\" 300 hidelevel canlose")[0].Everything().Single(i => i.Op == Op.WildBattle);
        Assert.Equal(300, line.Number);
        Assert.True(line.CanLose);
        Assert.True(line.Staged!.HideLevel);
        Assert.ThrowsAny<Exception>(() => ScriptParser.Parse("test", "script S\n wildbattle \"Zacian\" 301 hidelevel canlose"));
        Assert.ThrowsAny<Exception>(() => ScriptParser.Parse("test", "script S\n wildbattle \"Zacian\" 280"));
    }

    [Fact]
    public void ABattleAtThreeHundredIsRecordedAndReplayedAsItWas()
    {
        var mine = new Party();
        mine.Add(Mon("Garchomp", 300, "Earthquake", "Dragon Claw"));
        var trainer = new Trainer { Id = "cap_test", Name = "Mara", TrainerClass = "Ace Trainer", PrizeMoney = 64 };
        trainer.Party.Add(Mon("Blissey", 300, "Pound"));
        trainer.Party.Add(Mon("Shuckle", 280, "Wrap"));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = mine, Inventory = new Inventory(), Pokedex = new Pokedex(), Trainers = { trainer },
            Random = new BattleRandom(20261112), Rules = Ruleset.Modern, PlayerName = "Lucas"
        });
        Settle(battle);
        for (int turn = 0; turn < 60 && !battle.IsBattleOver; turn++)
        {
            battle.SelectMove(0);
            Settle(battle);
        }
        Assert.True(battle.IsBattleOver);

        var file = battle.RecordFile(new DateTime(2026, 11, 12))!;
        Assert.Equal(300, file.Player.Single().Pokemon.Level);
        Assert.Equal(new[] { 300, 280 }, file.Trainers.Single().Party.Select(p => p.Pokemon.Level));
        var read = BattleRecordFile.Read(file.Serialize());
        Assert.Equal(ReplayRefusal.None, read.Refused);
        Assert.Equal(300, read.File!.Player.Single().Pokemon.ToPokemon(Ruleset.Modern).Level);
        var again = read.File.Replay();
        Assert.Equal(battle.Result, again.Result);
        Assert.Equal(Golden.BattleText.Written(battle.Core), Golden.BattleText.Written(again));
    }

    [Fact]
    public void ATrainerRelevelledPastTwoHundredAndSixtyStopsAtThreeHundred()
    {
        var trainer = new Trainer { Id = "relevel_test", Name = "Mara", TrainerClass = "Ace Trainer", PrizeMoney = 64 };
        trainer.Party.Add(Mon("Bidoof", 50, "Tackle"));
        trainer.Party.Add(Mon("Bibarel", 60, "Tackle"));
        var lifted = trainer.Relevelled(level => level + 230);
        Assert.Equal(new[] { 280, 290 }, lifted.Party.Members.Select(p => p.Level));
        Assert.Equal(new[] { 300, 300 }, trainer.Relevelled(level => level + 400).Party.Members.Select(p => p.Level));
        // The trainer of the map keeps their own
        Assert.Equal(new[] { 50, 60 }, trainer.Party.Members.Select(p => p.Level));
    }

    [Fact]
    public void TheRegionsBeforeCarpathiaKeepTheTopTheyHad()
    {
        // Every region's mapping ends at or under 260, so it holds there, as it did when 260 was the cap
        foreach (var region in RegionDatabase.All)
            if (region.Levels is { } levels)
            {
                Assert.True(levels.Points[^1].To <= Ruleset.FormerHighestLevel, region.Id);
                Assert.Equal(Ruleset.FormerHighestLevel, levels.Top);
                Assert.True(levels.Map(500) <= Ruleset.FormerHighestLevel, region.Id);
                Assert.Equal(Ruleset.FormerHighestLevel, LevelCaps.TopOf(region.Id));
            }

        // A mapping that climbs past 260 (Carpathia's, written in a later session) reaches 300 and no further
        var climbing = LevelMapping.From(new List<int[]> { new[] { 2, 262 }, new[] { 60, 286 }, new[] { 70, 296 } })!;
        Assert.Equal(300, climbing.Top);
        Assert.Equal(286, climbing.Map(60));
        Assert.Equal(300, climbing.Map(90));
        Assert.Throws<FormatException>(() => LevelMapping.From(new List<int[]> { new[] { 2, 301 } }));
    }

    [Fact]
    public void AGalarCeilingAfterItsHallOfFameStaysAtTwoHundredAndSixty()
    {
        // Galar's post-game top is 242 as met: 110% is 266, held at 260 as before the cap was raised
        var galar = RegionDatabase.Get("Galar")!;
        var story = new StoryState();
        story.Set(galar.StoryCompleteFlag);
        Assert.Equal(242, LevelCaps.MilestoneOf(galar.Id, story));
        Assert.Equal(Ruleset.FormerHighestLevel, LevelCaps.Ceiling(galar.Id, story));
    }
}
