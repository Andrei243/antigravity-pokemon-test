using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// The first scenario tests of plan 06: one rule each, on Pokémon whose stats are known, with the rolls the rule
/// depends on fixed. The numbers are worked out by hand from Platinum's own code (the decompilation's
/// <c>BattleSystem_CalcMoveDamage</c>, <c>CalcDamageVariance</c>, <c>CalcCriticalMulti</c> and
/// <c>ApplyTypeChart</c>): the stat times the power times (2 × level / 5 + 2), over the defending stat, over 50,
/// plus 2; then the critical multiplier; then the roll, 100 hundredths down to 85; then × 15 / 10 for a move of the
/// user's own type and × 20 / 10 or × 5 / 10 for each type of the target. Every division rounds down.
/// </summary>
public class BattleScenarioTests
{
    // At level 50 with no IVs, no EVs and a neutral nature a stat is its base + 5:
    //   Machamp   Attack 135            Blastoise  Defense 105, HP 139
    //   Garchomp  Attack 135            Empoleon   Defense 93
    //   Rhydon    Attack 135            Snorlax    Sp. Def 115
    //   Alakazam  Sp. Atk 140

    [Fact]
    public void DamageIsWhatPlatinumsFormulaGives()
    {
        // Strength, 80 power, neither side's type: 135 × 80 × 22 = 237,600; / 105 = 2,262; / 50 = 45; + 2 = 47
        var machamp = Mon("Machamp", 50, "Strength");
        var blastoise = Mon("Blastoise", 50);
        Assert.Equal(135, machamp.Attack);
        Assert.Equal(105, blastoise.Defense);
        Assert.Equal(47, Damage(machamp, blastoise, "Strength"));
        // The weakest roll: 47 × 85 / 100 = 39
        Assert.Equal(39, Damage(machamp, blastoise, "Strength", Steady().Force(RollKind.Damage, 15)));
        // And every step between is one of sixteen
        var spread = Enumerable.Range(0, 16).Select(roll => Damage(machamp, blastoise, "Strength", Steady().Force(RollKind.Damage, roll))).ToList();
        Assert.Equal(Enumerable.Range(0, 16).Select(roll => 47 * (100 - roll) / 100), spread);
    }

    [Fact]
    public void AMoveOfTheUsersTypeAgainstAWeaknessIsRaisedInThatOrder()
    {
        // Earthquake, 100 power: 135 × 100 × 22 = 297,000; / 93 = 3,193; / 50 = 63; + 2 = 65.
        // Garchomp is a Ground type: 65 × 15 / 10 = 97. Empoleon is Water (× 1) and Steel (× 2): 194
        var garchomp = Mon("Garchomp", 50, "Earthquake");
        var empoleon = Mon("Empoleon", 50);
        Assert.Equal(93, empoleon.Defense);
        Assert.Equal(194, Damage(garchomp, empoleon, "Earthquake"));
        // The weakest roll comes before the bonuses: 65 × 85 / 100 = 55; × 15 / 10 = 82; × 2 = 164
        Assert.Equal(164, Damage(garchomp, empoleon, "Earthquake", Steady().Force(RollKind.Damage, 15)));
    }

    [Fact]
    public void StatStagesMultiplyTheStatBeforeAnythingElse()
    {
        // Psychic, 90 power, special, at +2 Sp. Atk (× 20 / 10): 280 × 90 × 22 = 554,400; / 115 = 4,820; / 50 = 96;
        // + 2 = 98; Alakazam's own type: 98 × 15 / 10 = 147
        var alakazam = Mon("Alakazam", 50, "Psychic");
        var snorlax = Mon("Snorlax", 50);
        Assert.Equal(140, alakazam.SpAttack);
        Assert.Equal(115, snorlax.SpDefense);
        alakazam.StatStages[StatType.SpAttack] = 2;
        Assert.Equal(147, Damage(alakazam, snorlax, "Psychic"));

        // At −1 (× 10 / 15): 93 × 90 × 22 = 184,140; / 115 = 1,601; / 50 = 32; + 2 = 34; × 15 / 10 = 51
        alakazam.StatStages[StatType.SpAttack] = -1;
        Assert.Equal(51, Damage(alakazam, snorlax, "Psychic"));
    }

    [Fact]
    public void ACriticalHitDoublesTheDamageAndIgnoresTheTargetsBoosts()
    {
        var machamp = Mon("Machamp", 50, "Strength");
        var blastoise = Mon("Blastoise", 50);
        var critical = Steady().Force(RollKind.Critical, 0);
        Assert.Equal(94, Damage(machamp, blastoise, "Strength", critical));

        // At +2 Defense (210): 237,600 / 210 = 1,131; / 50 = 22; + 2 = 24. A critical hit sees 105: 47 × 2 = 94
        blastoise.StatStages[StatType.Defense] = 2;
        Assert.Equal(24, Damage(machamp, blastoise, "Strength"));
        Assert.Equal(94, Damage(machamp, blastoise, "Strength", critical));

        // It keeps the target's drops, and ignores the attacker's own
        blastoise.StatStages[StatType.Defense] = 0;
        machamp.StatStages[StatType.Attack] = -2;
        Assert.Equal(94, Damage(machamp, blastoise, "Strength", critical));
    }

    [Fact]
    public void ABurnedAttackersPhysicalMoveDoesHalfDamage()
    {
        // Rhydon's Strength on Blastoise is Machamp's: 45 before the + 2. Burned: 45 / 2 = 22; + 2 = 24
        var rhydon = Mon("Rhydon", 50, "Strength");
        var blastoise = Mon("Blastoise", 50);
        Assert.Equal(47, Damage(rhydon, blastoise, "Strength"));
        rhydon.Status = StatusCondition.Burn;
        Assert.Equal(24, Damage(rhydon, blastoise, "Strength"));

        // A special move is not weakened by a burn
        var alakazam = Mon("Alakazam", 50, "Psychic");
        var snorlax = Mon("Snorlax", 50);
        int healthy = Damage(alakazam, snorlax, "Psychic");
        alakazam.Status = StatusCondition.Burn;
        Assert.Equal(healthy, Damage(alakazam, snorlax, "Psychic"));
    }

    [Fact]
    public void ATurnTakesFromTheTargetWhatTheFormulaSays()
    {
        var machamp = Mon("Machamp", 50, "Strength");
        var blastoise = Mon("Blastoise", 50);
        Assert.Equal(139, blastoise.MaxHP);
        var battle = Battle(machamp, blastoise);

        var said = Turn(battle);

        Assert.Contains("Machamp used Strength!", said);
        Assert.Equal(139 - 47, blastoise.CurrentHP);
        Assert.DoesNotContain("A critical hit!", said);
    }

    [Fact]
    public void AMoveThatCanMissHitsOrMissesByItsRoll()
    {
        // Platinum's Tackle is 95% accurate: it hits on a roll below 95
        List<string> With(int roll)
        {
            var battle = Battle(Mon("Bidoof", 20, "Tackle"), Mon("Bidoof", 20), Steady().Force(RollKind.Accuracy, roll));
            return Turn(battle);
        }
        Assert.Equal(95, MoveDatabase.Get("Tackle").Under(Ruleset.Platinum).Accuracy);
        Assert.DoesNotContain("Bidoof's attack missed!", With(94));
        Assert.Contains("Bidoof's attack missed!", With(95));
    }

    [Fact]
    public void ASideEffectHappensByItsRoll()
    {
        // Ember burns one time in ten
        Pokemon After(int roll)
        {
            var foe = Mon("Bidoof", 30);
            Turn(Battle(Mon("Chimchar", 30, "Ember"), foe, Steady().Force(RollKind.SideEffect, roll)));
            return foe;
        }
        Assert.Equal(StatusCondition.Burn, After(9).Status);
        Assert.Equal(StatusCondition.None, After(10).Status);
    }

    [Fact]
    public void AParalysedPokemonLosesItsTurnOneTimeInFour()
    {
        List<string> With(int roll)
        {
            var mine = Mon("Bidoof", 20, "Tackle");
            mine.Status = StatusCondition.Paralyze;
            return Turn(Battle(mine, Mon("Bidoof", 20), Steady().Force(RollKind.FullParalysis, roll)));
        }
        // The die has four faces, and the first is the one that holds it
        Assert.Contains("Bidoof is paralyzed! It can't move!", With(0));
        Assert.Contains("Bidoof used Tackle!", With(1));
        Assert.Contains("Bidoof used Tackle!", With(3));
    }

    [Fact]
    public void TheSameSeedAndChoicesPlayTheSameBattle()
    {
        // Nothing fixed: the whole battle hangs on the seed
        List<string> Play(uint seed)
        {
            var battle = Battle(Mon("Bidoof", 20, "Tackle", "Growl"), Mon("Starly", 20, "Tackle", "Growl", "Quick Attack"), new BattleRandom(seed));
            var said = new List<string>();
            for (int turn = 0; turn < 12 && !battle.IsBattleOver; turn++) said.AddRange(Turn(battle, turn % 2));
            said.Add($"{battle.PlayerPokemon.CurrentHP}/{battle.EnemyPokemon.CurrentHP}");
            return said;
        }

        var first = Play(20261004);
        Assert.Equal(first, Play(20261004));
        Assert.True(first.Count > 12);
        Assert.Contains(Enumerable.Range(1, 8), seed => !Play((uint)seed).SequenceEqual(first));
    }
}
