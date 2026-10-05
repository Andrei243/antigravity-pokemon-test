using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// The damage of one hit, in Platinum's own order and whole numbers (<see cref="Formulas"/>; the decompilation's
/// <c>BattleSystem_CalcMoveDamage</c>, <c>BattleScript_CalcMoveDamage</c> and <c>BattleSystem_ApplyTypeChart</c>):
/// <list type="number">
/// <item>the power, with the user's bonuses to it;</item>
/// <item>the attacking and the defending stat, each with its holder's bonuses and then its stage;</item>
/// <item>stat × power × (2 × level / 5 + 2) / defence / 50, a burn's halving, three quarters for a move that hits
/// several, + 2;</item>
/// <item>the critical multiplier, then what the user's item adds before chance (a Life Orb);</item>
/// <item>the roll: all of it down to 85 hundredths;</item>
/// <item>half as much again for a move of the user's own type, then each of the target's types;</item>
/// <item>what the target's and the user's ability and item make of how well the type did (Filter, an Expert Belt).</item>
/// </list>
/// Every step rounds down. Abilities and held items join in through <see cref="BattleEffect"/>, an ability's
/// bonus before an item's where both touch the same number.
/// </summary>
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
        var (primary, secondary) = TypesHit(attacker, defender, move);
        return primary == null ? 1f : TypeChart.GetEffectiveness(move.Type, primary.Value, secondary, rules);
    }

    /// <summary>The target's types as the move meets them: Scrappy takes a Ghost type out of the way of Normal and Fighting moves.</summary>
    private static (PokemonType? Primary, PokemonType? Secondary) TypesHit(Battler attacker, Battler defender, Move move)
    {
        var species = defender.Pokemon!.Species;
        PokemonType? primary = species.PrimaryType, secondary = species.SecondaryType;
        if (move.Type is PokemonType.Normal or PokemonType.Fighting && BattleEffects.Of(attacker).Any(e => e.HitsGhosts))
        {
            if (secondary == PokemonType.Ghost) secondary = null;
            if (primary == PokemonType.Ghost)
            {
                primary = secondary;
                secondary = null;
            }
        }
        return (primary, secondary);
    }

    /// <param name="spread">The move hits more than one Pokémon this time, so each takes 3/4.</param>
    /// <param name="powerOverride">A typeless physical hit of this power, which can't be critical (confusion).</param>
    /// <param name="rules">The rules the battle is fought by; left out, those of the game in progress.</param>
    public static DamageResult Calculate(Battler attacker, Battler defender, Move move, Random rng, bool spread, int? powerOverride = null, Ruleset? rules = null)
    {
        rules ??= Ruleset.Current;
        var result = new DamageResult { TypeMultiplier = 1f };
        int power = powerOverride ?? move.Power;
        // Struggle has no type either, but it is a move like any other and can be a critical hit
        bool typeless = powerOverride.HasValue || move.Data == BattleCore.StruggleData;
        if ((move.Category == MoveCategory.Status && !powerOverride.HasValue) || power <= 0) return result;

        var atkPokemon = attacker.Pokemon!;
        var defPokemon = defender.Pokemon!;
        var attackerEffects = BattleEffects.Of(attacker).ToList();
        bool breaksAbility = attackerEffects.Any(e => e.IgnoresTargetAbility);
        var defenderEffects = BattleEffects.Of(defender, includeAbility: !breaksAbility).ToList();

        result.TypeMultiplier = typeless ? 1f : Effectiveness(attacker, defender, move, rules);
        if (result.TypeMultiplier == 0f) return result;

        // Critical hit: one in the rules' odds for the stage (Platinum's are 1/16, 1/8, 1/4, 1/3, 1/2)
        if (!powerOverride.HasValue)
        {
            int stage = move.Data.CritStage + attackerEffects.Sum(e => e.CritStageBonus);
            bool rolled = rng.Roll(RollKind.Critical, rules.CriticalOdds[Math.Clamp(stage, 0, 4)]) == 0;
            result.IsCritical = rolled && !defenderEffects.Any(e => e.PreventsCriticalHits);
        }

        // The power, then the two stats: each bonus in turn
        foreach (var e in attackerEffects) power = Formulas.Scale(power, e.PowerMultiplier(attacker, defender, move));
        power = Math.Max(1, power);

        bool physical = powerOverride.HasValue || move.Category == MoveCategory.Physical;
        int attack = physical ? atkPokemon.Attack : atkPokemon.SpAttack;
        int defense = physical ? defPokemon.Defense : defPokemon.SpDefense;
        foreach (var e in attackerEffects) attack = Formulas.Scale(attack, e.AttackMultiplier(attacker, move));
        foreach (var e in defenderEffects) defense = Formulas.Scale(defense, e.DefenseMultiplier(defender, move));

        int attackStage = atkPokemon.StatStages.GetValueOrDefault(physical ? StatType.Attack : StatType.SpAttack);
        int defenseStage = defPokemon.StatStages.GetValueOrDefault(physical ? StatType.Defense : StatType.SpDefense);
        if (defenderEffects.Any(e => e.IgnoresOthersStatStages)) attackStage = 0;
        if (attackerEffects.Any(e => e.IgnoresOthersStatStages)) defenseStage = 0;
        if (result.IsCritical)
        {
            // A critical hit ignores the attacker's drops and the defender's boosts
            attackStage = Math.Max(0, attackStage);
            defenseStage = Math.Min(0, defenseStage);
        }
        attack = Math.Max(1, Formulas.Staged(attack, attackStage));
        defense = Math.Max(1, Formulas.Staged(defense, defenseStage));

        bool burned = atkPokemon.Status == StatusCondition.Burn && physical && !attackerEffects.Any(e => e.IgnoresBurnPenalty);
        int damage = Formulas.BaseDamage(atkPokemon.Level, power, attack, defense, burned, spread);

        if (result.IsCritical) damage = Formulas.Scale(damage, rules.CriticalMultiplier * attackerEffects.Aggregate(1f, (m, e) => Math.Max(m, e.CriticalBoost)));
        foreach (var e in attackerEffects) damage = Formulas.Scale(damage, e.DamageBeforeTheRoll(attacker, move));

        damage = Formulas.Variance(damage, rng.Roll(RollKind.Damage, 16));

        if (!typeless)
        {
            // Same-type attack bonus
            if (attacker.HasType(move.Type))
            {
                result.IsSTAB = true;
                float stab = attackerEffects.Select(e => e.StabOverride).Where(s => s.HasValue).Select(s => s!.Value).DefaultIfEmpty(1.5f).Max();
                damage = Formulas.Scale(damage, stab);
            }

            // Each of the target's types in turn; a hit that isn't stopped never rounds down to nothing
            var (primary, secondary) = TypesHit(attacker, defender, move);
            foreach (var type in new[] { primary, secondary == primary ? null : secondary })
            {
                if (type == null) continue;
                float against = TypeChart.GetEffectiveness(move.Type, type.Value, null, rules);
                if (against != 1f) damage = Formulas.Divide(damage * (int)MathF.Round(against * 10f), 10);
            }

            foreach (var e in defenderEffects) damage = Formulas.Scale(damage, e.IncomingDamageMultiplier(defender, attacker, move, result.TypeMultiplier));
            foreach (var e in attackerEffects) damage = Formulas.Scale(damage, e.DamageMultiplier(attacker, defender, move, result.TypeMultiplier));
        }

        result.Damage = Math.Max(1, damage);
        return result;
    }

    /// <summary>What a stat stage multiplies a stat by, as a fraction (the battle itself uses <see cref="Formulas.Staged"/>).</summary>
    public static float StageMultiplier(int stage)
    {
        stage = Math.Clamp(stage, -6, 6);
        return stage >= 0 ? (2f + stage) / 2f : 2f / (2f - stage);
    }

    /// <summary>Accuracy and evasion stages combine into one, with thirds instead of halves (<see cref="Formulas.HitRate"/> has the original's table).</summary>
    public static float AccuracyStageMultiplier(int stage)
    {
        stage = Math.Clamp(stage, -6, 6);
        return stage >= 0 ? (3f + stage) / 3f : 3f / (3f - stage);
    }
}
