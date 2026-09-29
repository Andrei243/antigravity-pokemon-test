using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public static class DamageCalculator
{
    private static readonly Random rng = new();

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

    public static DamageResult CalculateDamage(Pokemon attacker, Pokemon defender, Move move)
    {
        var result = new DamageResult
        {
            Damage = 0,
            TypeMultiplier = 1.0f,
            IsCritical = false,
            IsSTAB = false
        };

        if (move.Category == MoveCategory.Status || move.Power <= 0)
        {
            return result;
        }

        // Type Effectiveness Check
        result.TypeMultiplier = TypeChart.GetEffectiveness(
            move.Type,
            defender.Species.PrimaryType,
            defender.Species.SecondaryType);

        if (result.TypeMultiplier == 0.0f)
        {
            return result;
        }

        // Stats Selection (Physical vs Special Split)
        int atkVal = move.Category == MoveCategory.Physical
            ? attacker.GetEffectiveStat(StatType.Attack)
            : attacker.GetEffectiveStat(StatType.SpAttack);

        int defVal = move.Category == MoveCategory.Physical
            ? defender.GetEffectiveStat(StatType.Defense)
            : defender.GetEffectiveStat(StatType.SpDefense);

        // Gen 4 Critical Hit Check (1/16 base, 1/8 with high crit)
        int critThreshold = move.Data.CritStage > 0 ? 8 : 16;
        result.IsCritical = rng.Next(critThreshold) == 0;
        float critMult = result.IsCritical ? 2.0f : 1.0f;

        // Base Formula: ((2 * Level / 5 + 2) * Power * Atk / Def) / 50 + 2
        float levelFactor = (2.0f * attacker.Level / 5.0f) + 2.0f;
        float baseDmg = ((levelFactor * move.Power * ((float)atkVal / Math.Max(1, defVal))) / 50.0f) + 2.0f;

        // STAB (Same Type Attack Bonus = 1.5x)
        if (attacker.Species.PrimaryType == move.Type || attacker.Species.SecondaryType == move.Type)
        {
            result.IsSTAB = true;
            baseDmg *= 1.5f;
        }

        // Type multiplier
        baseDmg *= result.TypeMultiplier;

        // Critical multiplier
        baseDmg *= critMult;

        // Burn penalty for physical attackers
        if (attacker.Status == StatusCondition.Burn && move.Category == MoveCategory.Physical)
        {
            baseDmg *= 0.5f;
        }

        // Random variance (0.85 to 1.00)
        float randomVariance = (rng.Next(85, 101)) / 100.0f;
        baseDmg *= randomVariance;

        result.Damage = Math.Max(1, (int)Math.Floor(baseDmg));
        return result;
    }
}
