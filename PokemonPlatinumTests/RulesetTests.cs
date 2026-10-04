using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The two sets of rules an adventure can be played by (plan 06, decision 1): what each says, that a battle
/// follows the one it is given, and that the choice is kept with the save. These tests hand the rules to a battle
/// or ask a move for its values under them; none changes the rules of the game in progress
/// (<see cref="Ruleset.Use"/>), because test classes run side by side.
/// </summary>
public class RulesetTests
{
    [Fact]
    public void PlatinumsRulesAreTheGamesOwn()
    {
        Assert.Same(Ruleset.Platinum, Ruleset.Current);
        Assert.Same(Ruleset.Platinum, Ruleset.Of(RulesPreset.Platinum));
        Assert.Same(Ruleset.Modern, Ruleset.Of(RulesPreset.Modern));
        Assert.Equal(RulesPreset.Platinum, default(RulesPreset));

        // A battle that is given no rules is fought by the game's
        var party = new Party();
        party.Add(Mon("Bidoof", 5));
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(), WildPokemon = new() { Mon("Starly", 5) }
        });
        Assert.Same(Ruleset.Platinum, battle.Rules);
        Assert.Same(Ruleset.Modern, Battle(Mon("Bidoof", 5), Mon("Starly", 5), rules: Ruleset.Modern).Rules);
    }

    [Fact]
    public void WhatEachSetOfRulesSays()
    {
        var p = Ruleset.Platinum;
        Assert.Equal(new[] { 16, 8, 4, 3, 2 }, p.CriticalOdds);
        Assert.Equal(2f, p.CriticalMultiplier);
        Assert.Equal(8, p.BurnDamageDivisor);
        Assert.Equal(0.25f, p.ParalysisSpeed);
        Assert.False(p.ElectricTypesCantBeParalyzed);
        Assert.Equal(4, p.SleepLengths);
        Assert.Equal(2, p.ConfusionSelfHitOdds);
        Assert.True(p.SteelResistsGhostAndDark);
        Assert.False(p.ModernMoveValues);

        var m = Ruleset.Modern;
        Assert.Equal(new[] { 24, 8, 2, 1, 1 }, m.CriticalOdds);
        Assert.Equal(1.5f, m.CriticalMultiplier);
        Assert.Equal(16, m.BurnDamageDivisor);
        Assert.Equal(0.5f, m.ParalysisSpeed);
        Assert.True(m.ElectricTypesCantBeParalyzed);
        Assert.Equal(3, m.SleepLengths);
        Assert.Equal(3, m.ConfusionSelfHitOdds);
        Assert.False(m.SteelResistsGhostAndDark);
        Assert.True(m.ModernMoveValues);

        Assert.NotEqual(p.Name, m.Name);
    }

    [Fact]
    public void ACriticalHitIsWorthTwiceOrHalfAsMuchAgain()
    {
        // Machamp's Strength on Blastoise is 47 (see BattleScenarioTests): × 2 = 94, or × 1.5 = 70
        var machamp = Mon("Machamp", 50, "Strength");
        var blastoise = Mon("Blastoise", 50);
        var critical = Steady().Force(RollKind.Critical, 0);
        Assert.Equal(94, Damage(machamp, blastoise, "Strength", critical, Ruleset.Platinum));
        Assert.Equal(70, Damage(machamp, blastoise, "Strength", critical, Ruleset.Modern));

        // Sniper makes it half as much again under either: × 3, or × 2.25 (47 × 2.25 = 105)
        machamp.AbilityName = "Sniper";
        Assert.Equal(141, Damage(machamp, blastoise, "Strength", critical, Ruleset.Platinum));
        Assert.Equal(105, Damage(machamp, blastoise, "Strength", critical, Ruleset.Modern));
    }

    [Fact]
    public void CriticalHitsComeByTheRulesOdds()
    {
        var machamp = Mon("Machamp", 50, "Strength", "Slash");
        var blastoise = Mon("Blastoise", 50);
        DamageCalculator.DamageResult Hit(string move, BattleRandom rolls, Ruleset rules) =>
            DamageCalculator.CalculateDamage(machamp, blastoise, machamp.Moves.First(m => m.Name == move), rolls, rules);

        // Only the die's first face is a critical hit
        Assert.True(Hit("Strength", Steady().Force(RollKind.Critical, 0), Ruleset.Platinum).IsCritical);
        Assert.False(Hit("Strength", Steady().Force(RollKind.Critical, 1), Ruleset.Platinum).IsCritical);

        // Left to chance over 4,800 hits: one in sixteen is 300 of them, one in twenty-four is 200
        int Count(string move, Ruleset rules)
        {
            var rolls = new BattleRandom(1);
            return Enumerable.Range(0, 4800).Count(_ => Hit(move, rolls, rules).IsCritical);
        }
        Assert.InRange(Count("Strength", Ruleset.Platinum), 260, 340);
        Assert.InRange(Count("Strength", Ruleset.Modern), 165, 235);
        // A move with a high critical-hit ratio: one in eight under both
        Assert.InRange(Count("Slash", Ruleset.Platinum), 540, 660);
        Assert.InRange(Count("Slash", Ruleset.Modern), 540, 660);

        // Three stages up (Slash, Super Luck and a Scope Lens): one in three in Platinum, every time by the
        // modern rules, whatever the die shows
        machamp.AbilityName = "Super Luck";
        machamp.HeldItem = ItemDatabase.Get("Scope Lens");
        var last = Steady().Force(RollKind.Critical, 99);
        Assert.False(Hit("Slash", last, Ruleset.Platinum).IsCritical);
        Assert.True(Hit("Slash", last, Ruleset.Modern).IsCritical);
        Assert.InRange(Count("Slash", Ruleset.Platinum), 1480, 1720);
    }

    [Fact]
    public void ABurnTakesAnEighthOrASixteenth()
    {
        int Lost(Ruleset rules)
        {
            var mine = Mon("Snorlax", 50);
            mine.Status = StatusCondition.Burn;
            Turn(Battle(mine, Mon("Blastoise", 50), rules: rules));
            return mine.MaxHP - mine.CurrentHP;
        }
        // Snorlax has 220 HP at level 50
        Assert.Equal(27, Lost(Ruleset.Platinum));
        Assert.Equal(13, Lost(Ruleset.Modern));
    }

    [Fact]
    public void ParalysisQuartersOrHalvesSpeed()
    {
        int Speed(Ruleset rules)
        {
            var mine = Mon("Alakazam", 50);
            mine.Status = StatusCondition.Paralyze;
            var battle = Battle(mine, Mon("Blastoise", 50), rules: rules);
            return battle.EffectiveSpeed(battle.PlayerSlots[0]);
        }
        // Alakazam's Speed is 125 at level 50
        Assert.Equal(31, Speed(Ruleset.Platinum));
        Assert.Equal(62, Speed(Ruleset.Modern));
    }

    [Fact]
    public void ElectricTypesCanBeParalysedOnlyInPlatinum()
    {
        bool Paralysed(Ruleset rules, string species)
        {
            var foe = Mon(species, 20);
            var battle = Battle(Mon("Bidoof", 20), foe, rules: rules);
            battle.TryInflictStatus(battle.EnemySlots[0], StatusCondition.Paralyze, battle.PlayerSlots[0]);
            return foe.Status == StatusCondition.Paralyze;
        }
        Assert.True(Paralysed(Ruleset.Platinum, "Shinx"));
        Assert.False(Paralysed(Ruleset.Modern, "Shinx"));
        Assert.True(Paralysed(Ruleset.Modern, "Bidoof"));
    }

    [Fact]
    public void ASleepHasFourLengthsOrThree()
    {
        int Longest(Ruleset rules)
        {
            var foe = Mon("Bidoof", 20);
            var battle = Battle(Mon("Bidoof", 20), foe, Steady().Force(RollKind.SleepTurns, 99), rules);
            battle.TryInflictStatus(battle.EnemySlots[0], StatusCondition.Sleep, battle.PlayerSlots[0]);
            return foe.SleepTurns;
        }
        Assert.Equal(4, Longest(Ruleset.Platinum));
        Assert.Equal(3, Longest(Ruleset.Modern));
    }

    [Fact]
    public void AConfusedPokemonHurtsItselfOneTimeInTwoOrThree()
    {
        bool HurtItself(Ruleset rules, int roll)
        {
            var battle = Battle(Mon("Bidoof", 20, "Tackle"), Mon("Bidoof", 20), Steady().Force(RollKind.ConfusionSelfHit, roll), rules);
            battle.PlayerSlots[0].ConfusionTurns = 3;
            return Turn(battle).Contains("It hurt itself in its confusion!");
        }
        // The die's last face is the one that hurts
        Assert.False(HurtItself(Ruleset.Platinum, 0));
        Assert.True(HurtItself(Ruleset.Platinum, 1));
        Assert.False(HurtItself(Ruleset.Modern, 1));
        Assert.True(HurtItself(Ruleset.Modern, 2));
    }

    [Fact]
    public void SteelStoppedResistingGhostAndDark()
    {
        Assert.Equal(0.5f, TypeChart.GetEffectiveness(PokemonType.Ghost, PokemonType.Steel, null, Ruleset.Platinum));
        Assert.Equal(0.5f, TypeChart.GetEffectiveness(PokemonType.Dark, PokemonType.Steel, null, Ruleset.Platinum));
        Assert.Equal(1f, TypeChart.GetEffectiveness(PokemonType.Ghost, PokemonType.Steel, null, Ruleset.Modern));
        Assert.Equal(1f, TypeChart.GetEffectiveness(PokemonType.Dark, PokemonType.Steel, PokemonType.Water, Ruleset.Modern));

        // Everything else Steel resists, it still does; and a Ghost is still hit hard by a Ghost
        Assert.Equal(0.5f, TypeChart.GetEffectiveness(PokemonType.Psychic, PokemonType.Steel, null, Ruleset.Modern));
        Assert.Equal(2f, TypeChart.GetEffectiveness(PokemonType.Ghost, PokemonType.Ghost, PokemonType.Steel, Ruleset.Modern));

        // In a battle: Bite on Bronzor (Steel and Psychic) is × 0.5 × 2 = 1 in Platinum, × 2 by the modern rules
        var bronzor = new Battler(BattleSide.Enemy, 0) { Pokemon = Mon("Bronzor", 20) };
        var biter = new Battler(BattleSide.Player, 0) { Pokemon = Mon("Shinx", 20, "Bite") };
        var bite = biter.Pokemon.Moves[0];
        Assert.Equal(1f, DamageCalculator.Effectiveness(biter, bronzor, bite, Ruleset.Platinum));
        Assert.Equal(2f, DamageCalculator.Effectiveness(biter, bronzor, bite, Ruleset.Modern));
    }

    [Fact]
    public void AMoveHasItsValuesUnderEitherRules()
    {
        // Platinum's Tackle is 35 power and 95% accurate; the newest games' is 40 and 100
        var tackle = MoveDatabase.Get("Tackle");
        Assert.Equal((35, 95, 35), (tackle.Power, tackle.Accuracy, tackle.MaxPP));
        Assert.NotNull(tackle.Modern);

        var modern = tackle.Under(Ruleset.Modern);
        Assert.Equal((40, 100, 35), (modern.Power, modern.Accuracy, modern.MaxPP));
        Assert.Equal("Tackle", modern.Name);
        Assert.Equal(tackle.Flags, modern.Flags);

        // Asking leaves the game's own move alone, and Platinum's rules give it back as it is
        Assert.Equal((35, 95), (tackle.Power, tackle.Accuracy));
        var platinum = modern.Under(Ruleset.Platinum);
        Assert.Equal((35, 95, 35), (platinum.Power, platinum.Accuracy, platinum.MaxPP));

        // A move nothing changed, and one that came after Platinum, are the same move under both
        Assert.Same(MoveDatabase.Get("Earthquake"), MoveDatabase.Get("Earthquake").Under(Ruleset.Modern));
        Assert.Null(MoveDatabase.Get("Moonblast").Modern);
    }

    [Fact]
    public void ModernValuesAreOnlyWhatDiffers()
    {
        var changed = MoveDatabase.GetAll().Where(m => m.Modern != null).ToList();
        Assert.True(changed.Count > 100, $"only {changed.Count} of Platinum's moves have other values in the newest games");
        Assert.All(changed, m =>
        {
            Assert.True(m.Generation is >= 1 and <= 4, $"{m.Name} came after Platinum and has modern values of its own");
            var v = m.Modern!;
            Assert.True(v.Power != null || v.Accuracy != null || v.MaxPP != null || v.Priority != null || v.Type != null || v.Category != null,
                $"{m.Name} has an empty set of modern values");
            Assert.True(v.Power != m.Power && v.Accuracy != m.Accuracy && v.MaxPP != m.MaxPP && v.Priority != m.Priority && v.Type != m.Type && v.Category != m.Category,
                $"{m.Name} repeats a value that didn't change");
        });

        // Well-known changes
        Assert.Equal(90, MoveDatabase.Get("Thunderbolt").Modern!.Power);
        Assert.Equal(PokemonType.Fairy, MoveDatabase.Get("Charm").Modern!.Type);
        Assert.Equal(2, MoveDatabase.Get("Extreme Speed").Modern!.Priority);
    }

    [Fact]
    public void TheRulesAreKeptWithTheSave()
    {
        // Platinum's until chosen otherwise, and for every save made before the choice existed
        Assert.Equal(RulesPreset.Platinum, new SaveData().Rules);
        Assert.Equal(RulesPreset.Platinum, JsonSerializer.Deserialize<SaveData>("""{ "PlayerName": "Lucas", "Badges": 3 }""")!.Rules);

        string json = JsonSerializer.Serialize(new SaveData { Rules = RulesPreset.Modern });
        Assert.Contains("\"Rules\":\"Modern\"", json);
        Assert.Equal(RulesPreset.Modern, JsonSerializer.Deserialize<SaveData>(json)!.Rules);
    }
}
