using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>Generation 4's damage formula, with abilities and held items joining in through <see cref="BattleEffect"/>.</summary>
public static class DamageCalculator
{
    public struct DamageResult
    {
        public int Damage;
        public float TypeMultiplier;
        public bool IsCritical;
        public bool IsSTAB;

        public bool IsSuperEffective => TypeMultiplier > 1.0f;
        public bool IsNotVeryEffective => TypeMultiplier > 0f && TypeMultiplier < 1.0f;
        public bool IsImmune => TypeMultiplier == 0f;
    }

    /// <summary>Damage between two Pokémon outside a battle's field (no partner, one target).</summary>
    public static DamageResult CalculateDamage(Pokemon attacker, Pokemon defender, Move move, Random? rng = null, Ruleset? rules = null) =>
        Calculate(new Battler(BattleSide.Player, 0) { Pokemon = attacker }, new Battler(BattleSide.Enemy, 0) { Pokemon = defender },
            move, rng ?? Core.Dice.Shared, spread: false, rules: rules);

    /// <summary>How well a move's type hits the target (0, ¼, ½, 1, 2 or 4), counting Scrappy.</summary>
    /// <param name="rules">The rules whose type chart is used; left out, those of the game in progress.</param>
    public static float Effectiveness(Battler attacker, Battler defender, Move move, Ruleset? rules = null)
    {
        var species = defender.Pokemon!.Species;
        PokemonType primary = species.PrimaryType;
        PokemonType? secondary = species.SecondaryType;

        bool scrappy = move.Type is PokemonType.Normal or PokemonType.Fighting && BattleEffects.Of(attacker).Any(e => e.HitsGhosts);
        if (scrappy)
        {
            if (secondary == PokemonType.Ghost) secondary = null;
            if (primary == PokemonType.Ghost)
            {
                if (secondary == null) return 1f;
                primary = secondary.Value;
                secondary = null;
            }
        }
        return TypeChart.GetEffectiveness(move.Type, primary, secondary, rules);
    }

    /// <param name="spread">The move hits more than one Pokémon this time, so each takes 3/4.</param>
    /// <param name="powerOverride">A typeless physical hit of this power, which can't be critical (confusion, Struggle).</param>
    /// <param name="rules">The rules the battle is fought by; left out, those of the game in progress.</param>
    public static DamageResult Calculate(Battler attacker, Battler defender, Move move, Random rng, bool spread, int? powerOverride = null, Ruleset? rules = null)
    {
        rules ??= Ruleset.Current;
        var result = new DamageResult { TypeMultiplier = 1f };
        int power = powerOverride ?? move.Power;
        if ((move.Category == MoveCategory.Status && !powerOverride.HasValue) || power <= 0) return result;

        var atkPokemon = attacker.Pokemon!;
        var defPokemon = defender.Pokemon!;
        var attackerEffects = BattleEffects.Of(attacker).ToList();
        bool breaksAbility = attackerEffects.Any(e => e.IgnoresTargetAbility);
        var defenderEffects = BattleEffects.Of(defender, includeAbility: !breaksAbility).ToList();

        result.TypeMultiplier = powerOverride.HasValue ? 1f : Effectiveness(attacker, defender, move, rules);
        if (result.TypeMultiplier == 0f) return result;

        // Critical hit: one in the rules' odds for the stage (Platinum's are 1/16, 1/8, 1/4, 1/3, 1/2)
        if (!powerOverride.HasValue && !defenderEffects.Any(e => e.PreventsCriticalHits))
        {
            int stage = move.Data.CritStage + attackerEffects.Sum(e => e.CritStageBonus);
            result.IsCritical = rng.Roll(RollKind.Critical, rules.CriticalOdds[Math.Clamp(stage, 0, 4)]) == 0;
        }

        // Base power, then the attacking and defending stats
        float powerMult = attackerEffects.Aggregate(1f, (m, e) => m * e.PowerMultiplier(attacker, defender, move));
        int basePower = Math.Max(1, (int)(power * powerMult));

        bool physical = powerOverride.HasValue || move.Category == MoveCategory.Physical;
        StatType atkStat = physical ? StatType.Attack : StatType.SpAttack;
        StatType defStat = physical ? StatType.Defense : StatType.SpDefense;

        int atkStage = atkPokemon.StatStages.GetValueOrDefault(atkStat);
        int defStage = defPokemon.StatStages.GetValueOrDefault(defStat);
        if (defenderEffects.Any(e => e.IgnoresOthersStatStages)) atkStage = 0;
        if (attackerEffects.Any(e => e.IgnoresOthersStatStages)) defStage = 0;
        if (result.IsCritical)
        {
            // A critical hit ignores the attacker's drops and the defender's boosts
            atkStage = Math.Max(0, atkStage);
            defStage = Math.Min(0, defStage);
        }

        float atk = RawStat(atkPokemon, atkStat) * StageMultiplier(atkStage);
        atk *= attackerEffects.Aggregate(1f, (m, e) => m * e.AttackMultiplier(attacker, move));
        // Thick Fat and Heatproof halve the attacker's stat in Generation 4; as a damage multiplier the result is the same
        float def = RawStat(defPokemon, defStat) * StageMultiplier(defStage);
        def *= defenderEffects.Aggregate(1f, (m, e) => m * e.DefenseMultiplier(defender, move));

        int level = atkPokemon.Level;
        int damage = (2 * level / 5 + 2) * basePower * Math.Max(1, (int)atk) / Math.Max(1, (int)def) / 50;

        if (atkPokemon.Status == StatusCondition.Burn && physical && !attackerEffects.Any(e => e.IgnoresBurnPenalty)) damage /= 2;
        if (spread) damage = damage * 3 / 4;
        damage += 2;

        if (result.IsCritical) damage = (int)(damage * rules.CriticalMultiplier * attackerEffects.Aggregate(1f, (m, e) => Math.Max(m, e.CriticalBoost)));

        // Random factor 85–100%, in sixteen steps
        damage = damage * (85 + rng.Roll(RollKind.Damage, 16)) / 100;

        // Same-type attack bonus
        if (!powerOverride.HasValue && attacker.HasType(move.Type))
        {
            result.IsSTAB = true;
            float stab = attackerEffects.Select(e => e.StabOverride).Where(s => s.HasValue).Select(s => s!.Value).DefaultIfEmpty(1.5f).Max();
            damage = (int)(damage * stab);
        }

        damage = (int)(damage * result.TypeMultiplier);

        float finalMult = attackerEffects.Aggregate(1f, (m, e) => m * e.DamageMultiplier(attacker, defender, move, result.TypeMultiplier));
        finalMult *= defenderEffects.Aggregate(1f, (m, e) => m * e.IncomingDamageMultiplier(defender, attacker, move, result.TypeMultiplier));
        damage = (int)(damage * finalMult);

        result.Damage = Math.Max(1, damage);
        return result;
    }

    /// <summary>The stat before stages (and before paralysis, which only slows).</summary>
    private static int RawStat(Pokemon p, StatType stat) => stat switch
    {
        StatType.Attack => p.Attack,
        StatType.Defense => p.Defense,
        StatType.SpAttack => p.SpAttack,
        StatType.SpDefense => p.SpDefense,
        StatType.Speed => p.Speed,
        _ => 100
    };

    public static float StageMultiplier(int stage)
    {
        stage = Math.Clamp(stage, -6, 6);
        return stage >= 0 ? (2f + stage) / 2f : 2f / (2f - stage);
    }

    /// <summary>Accuracy and evasion stages combine into one, with thirds instead of halves.</summary>
    public static float AccuracyStageMultiplier(int stage)
    {
        stage = Math.Clamp(stage, -6, 6);
        return stage >= 0 ? (3f + stage) / 3f : 3f / (3f - stage);
    }
}
