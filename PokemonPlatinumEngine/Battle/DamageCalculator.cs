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
/// <item>the power, with what the move's own effect and the user's bonuses make of it;</item>
/// <item>the attacking and the defending stat, each with its holder's bonuses and then its stage;</item>
/// <item>stat × power × (2 × level / 5 + 2) / defence / 50, a burn's halving, a screen's, three quarters for a
/// move that hits several, the weather's part, + 2;</item>
/// <item>the critical multiplier, then what the user's item adds before chance (a Life Orb);</item>
/// <item>the roll: all of it down to 85 hundredths;</item>
/// <item>half as much again for a move of the user's own type, then each of the target's types;</item>
/// <item>what the target's and the user's ability and item make of how well the type did (Filter, an Expert Belt).</item>
/// </list>
/// Every step rounds down. Abilities and held items join in through <see cref="BattleEffect"/>, an ability's
/// bonus before an item's where both touch the same number. The field is read from the target's
/// <see cref="Battler.Field"/>: a battler that stands on none (a test's two Pokémon) is hit as on a bare one.
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

    /// <summary>
    /// How well a move's type hits the target (0, ¼, ½, 1, 2 or 4), as the battle has it at this moment
    /// (<c>BattleSystem_ApplyTypeChart</c>): Scrappy and Foresight let Normal and Fighting moves hit a Ghost,
    /// Miracle Eye lets Psychic moves hit a Dark type, Gravity, Ingrain and an Iron Ball let Ground moves hit a
    /// Flying type, a Pokémon that roosted has no Flying type until the turn ends, and one risen on magnetism
    /// can't be hit by Ground moves at all.
    /// </summary>
    /// <param name="rules">The rules whose type chart is used; left out, those of the game in progress.</param>
    public static float Effectiveness(Battler attacker, Battler defender, Move move, Ruleset? rules = null)
    {
        rules ??= Ruleset.Current;
        if (move.Type == PokemonType.Ground && IsLiftedByMagnetism(defender)) return 0f;
        float total = 1f;
        foreach (var (_, multiplier) in Matchups(attacker, defender, move, rules)) total *= multiplier;
        return total;
    }

    /// <summary>Magnet Rise holds: not for a Pokémon rooted to the ground or weighed down, nor under Gravity.</summary>
    public static bool IsLiftedByMagnetism(Battler defender) =>
        defender.Volatile.MagnetRiseTurns > 0 && !IsHeldDown(defender);

    /// <summary>Something keeps the Pokémon on the ground whatever its type or ability: Gravity, its own roots, an Iron Ball.</summary>
    public static bool IsHeldDown(Battler b) =>
        b.Field?.Gravity == true || b.Volatile.Ingrained || BattleEffects.Of(b).Any(e => e.GroundsHolder);

    /// <summary>Each of the target's types with what the move's type does against it, the immunities that don't hold left out.</summary>
    private static IEnumerable<(PokemonType Type, float Multiplier)> Matchups(Battler attacker, Battler defender, Move move, Ruleset rules)
    {
        var target = defender.Pokemon!;
        bool seesGhosts = defender.Volatile.Identified || BattleEffects.Of(attacker).Any(e => e.HitsGhosts);
        var types = target.SecondaryType is { } second && second != target.PrimaryType
            ? new[] { target.PrimaryType, second }
            : new[] { target.PrimaryType };

        foreach (var type in types)
        {
            if (type == PokemonType.Flying && defender.Turn.Roosting) continue;
            float multiplier = TypeChart.GetEffectiveness(move.Type, type, null, rules);
            if (multiplier == 0f)
            {
                if (type == PokemonType.Ghost && move.Type is PokemonType.Normal or PokemonType.Fighting && seesGhosts) continue;
                if (type == PokemonType.Flying && move.Type == PokemonType.Ground && IsHeldDown(defender)) continue;
                if (type == PokemonType.Dark && move.Type == PokemonType.Psychic && defender.Volatile.MiracleEye) continue;
            }
            if (multiplier != 1f) yield return (type, multiplier);
        }
    }

    /// <param name="spread">The move hits more than one Pokémon this time, so each takes 3/4.</param>
    /// <param name="powerOverride">A typeless physical hit of this power, which can't be critical (confusion).</param>
    /// <param name="rules">The rules the battle is fought by; left out, those of the game in progress.</param>
    /// <param name="powerTenths">What the move's own effect makes of its power, in tenths (20 doubles it).</param>
    /// <param name="critBonus">Stages the move's own effect adds to its chance of a critical hit.</param>
    /// <param name="pastScreens">The hit takes no notice of Reflect and Light Screen (Brick Break breaks them first).</param>
    /// <param name="noCrit">The hit can't be critical (one worked out ahead, like Future Sight's).</param>
    /// <param name="typeless">The move's type counts for nothing: no bonus for the user's own type, no matchup, no weather.</param>
    public static DamageResult Calculate(Battler attacker, Battler defender, Move move, Random rng, bool spread, int? powerOverride = null,
        Ruleset? rules = null, int powerTenths = 10, int critBonus = 0, bool pastScreens = false, bool noCrit = false, bool typeless = false)
    {
        rules ??= Ruleset.Current;
        var result = new DamageResult { TypeMultiplier = 1f };
        int power = powerOverride ?? move.Power;
        // Struggle has no type either, but it is a move like any other and can be a critical hit
        typeless |= powerOverride.HasValue || move.Data == BattleCore.StruggleData;
        if ((move.Category == MoveCategory.Status && !powerOverride.HasValue) || power <= 0) return result;

        var atkPokemon = attacker.Pokemon!;
        var defPokemon = defender.Pokemon!;
        var field = defender.Field;
        var weather = field?.WeatherInEffect ?? BattleWeather.None;
        var attackerEffects = BattleEffects.Of(attacker).ToList();
        bool breaksAbility = attackerEffects.Any(e => e.IgnoresTargetAbility);
        var defenderEffects = BattleEffects.Of(defender, includeAbility: !breaksAbility).ToList();

        result.TypeMultiplier = typeless ? 1f : Effectiveness(attacker, defender, move, rules);
        if (result.TypeMultiplier == 0f) return result;

        // Critical hit: one in the rules' odds for the stage (Platinum's are 1/16, 1/8, 1/4, 1/3, 1/2). Focus
        // Energy is worth two stages; a Lucky Chant over the target's side stops it
        if (!powerOverride.HasValue && !noCrit)
        {
            int stage = move.Data.CritStage + critBonus + attackerEffects.Sum(e => e.CritStageBonus) + (attacker.Volatile.FocusEnergy ? 2 : 0);
            bool rolled = rng.Roll(RollKind.Critical, rules.CriticalOdds[Math.Clamp(stage, 0, 4)]) == 0;
            result.IsCritical = rolled && !defenderEffects.Any(e => e.PreventsCriticalHits) && field?.Side(defender.Side).LuckyChant != true;
        }

        // The power: the move's own doubling, a Charge behind an Electric move, then each bonus in turn
        power = power * powerTenths / 10;
        if (attacker.Volatile.ChargeTurns > 0 && move.Type == PokemonType.Electric && !typeless) power *= 2;
        foreach (var e in attackerEffects) power = Formulas.Scale(power, e.PowerMultiplier(attacker, defender, move));
        if (!typeless && field != null)
        {
            if (move.Type == PokemonType.Electric && field.MudSport()) power /= 2;
            if (move.Type == PokemonType.Fire && field.WaterSport()) power /= 2;
        }
        power = Math.Max(1, power);

        bool physical = powerOverride.HasValue || move.Category == MoveCategory.Physical;
        int attack = physical ? atkPokemon.Attack : atkPokemon.SpAttack;
        int defense = physical ? defPokemon.Defense : defPokemon.SpDefense;
        foreach (var e in attackerEffects) attack = Formulas.Scale(attack, e.AttackMultiplier(attacker, move));
        foreach (var e in defenderEffects) defense = Formulas.Scale(defense, e.DefenseMultiplier(defender, move));
        // A sandstorm hardens Rock types against special moves
        if (!physical && weather == BattleWeather.Sandstorm && defender.HasType(PokemonType.Rock)) defense = defense * 15 / 10;

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

        // Reflect against physical moves and Light Screen against special ones, unless the hit is critical; two
        // Pokémon behind one screen share it
        var side = field?.Side(defender.Side);
        bool screened = side != null && !result.IsCritical && !pastScreens && !powerOverride.HasValue && (physical ? side.Reflect : side.LightScreen);
        bool shared = screened && field!.Standing(defender.Side) >= 2;

        int damage = Formulas.BaseDamage(atkPokemon.Level, power, attack, defense, burned, spread, screened, shared,
            typeless ? BattleWeather.None : weather, move.Type,
            dimmedBeam: move.Data.Effect == "SkipChargeTurnInSun" && field?.WeatherDimsTheSun == true,
            flashFire: attacker.FlashFire && move.Type == PokemonType.Fire && !typeless);

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
            foreach (var (_, against) in Matchups(attacker, defender, move, rules))
                damage = Formulas.Divide(damage * (int)MathF.Round(against * 10f), 10);

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
