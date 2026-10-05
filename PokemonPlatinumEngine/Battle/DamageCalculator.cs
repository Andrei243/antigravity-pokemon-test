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
        bool seesGhosts = defender.Volatile.Identified || BattleEffects.Of(attacker).Any(e => e.HitsGhosts);
        // The types it has right now: a Conversion or a Transform may have changed them for the battle
        var types = defender.Types;

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
    /// <param name="basePower">A power worked out for this hit in place of the move's own (Flail, Gyro Ball).</param>
    /// <param name="noVariance">The hit takes the formula's whole number, with no roll off it (Spit Up).</param>
    /// <param name="damageTenths">What the damage is multiplied by before the roll, in tenths (Me First: 15), where the original has a Life Orb's.</param>
    public static DamageResult Calculate(Battler attacker, Battler defender, Move move, Random rng, bool spread, int? powerOverride = null,
        Ruleset? rules = null, int powerTenths = 10, int critBonus = 0, bool pastScreens = false, bool noCrit = false, bool typeless = false,
        int? basePower = null, bool noVariance = false, int damageTenths = 10)
    {
        rules ??= Ruleset.Current;
        var result = new DamageResult { TypeMultiplier = 1f };
        int power = powerOverride ?? basePower ?? move.Power;
        // A power worked out as nothing falls back on the move's own (BattleSystem_CalcMoveDamage), which the
        // original's table gives as 1 for every move of variable power: Return at no friendship still deals its 2
        if (power == 0 && basePower.HasValue) power = 1;
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

        if (!powerOverride.HasValue && !noCrit) result.IsCritical = RollsCritical(attacker, defender, move, rng, rules, critBonus);

        // The power and the stats, each ability's and item's bonus in the original's own place and order
        // (BattleSystem_CalcMoveDamage), every step rounded down. Abilities go by name; the defender's count for
        // nothing against Mold Breaker. Items keep their hooks, called at their slot (plan 06 · R8 places each).
        string? mine = attacker.Ability?.Name;
        string? theirs = breaksAbility ? null : defender.Ability?.Name;
        var myItem = BattleEffects.ItemOf(attacker);
        var theirItem = BattleEffects.ItemOf(defender);
        bool physical = powerOverride.HasValue || move.Category == MoveCategory.Physical;
        bool typed = !typeless;
        var type = move.Type;
        int attack = physical ? atkPokemon.Attack : atkPokemon.SpAttack;
        int defense = physical ? defPokemon.Defense : defPokemon.SpDefense;

        // The move's own multiplier (Pursuit, a Helping Hand; Reckless sets it from the move's script), a Charge, Technician
        power = power * powerTenths / 10;
        if (mine == "Reckless" && (move.Data.RecoilPercent > 0 || move.Data.Effect == "CrashOnMiss")) power = power * 12 / 10;
        if (attacker.Volatile.ChargeTurns > 0 && type == PokemonType.Electric && typed) power *= 2;
        if (mine == "Technician" && move.Data != BattleCore.StruggleData && power <= 60) power = power * 15 / 10;
        if (physical && mine is "Huge Power" or "Pure Power") attack *= 2;
        if (physical && mine == "Slow Start" && field != null && field.Turn() - attacker.Volatile.SlowStartTurn < 5) attack /= 2;
        // The items: a type's boost, a Choice Band, a Soul Dew and the rest, in the original's block
        if (myItem != null) power = Formulas.Scale(power, myItem.PowerMultiplier(attacker, defender, move));
        if (myItem != null) attack = Formulas.Scale(attack, myItem.AttackMultiplier(attacker, move));
        if (theirItem != null) defense = Formulas.Scale(defense, theirItem.DefenseMultiplier(defender, move));
        if (typed && theirs == "Thick Fat" && type is PokemonType.Fire or PokemonType.Ice) power /= 2;
        if (physical && mine == "Hustle") attack = attack * 150 / 100;
        if (physical && mine == "Guts" && atkPokemon.Status != StatusCondition.None) attack = attack * 150 / 100;
        if (physical && theirs == "Marvel Scale" && defPokemon.Status != StatusCondition.None) defense = defense * 150 / 100;
        if (!physical && ((mine == "Plus" && SideHas(attacker, "Minus")) || (mine == "Minus" && SideHas(attacker, "Plus")))) attack = attack * 150 / 100;
        if (typed && field != null)
        {
            if (type == PokemonType.Electric && field.MudSport()) power /= 2;
            if (type == PokemonType.Fire && field.WaterSport()) power /= 2;
        }
        if (typed && atkPokemon.CurrentHP * 3 <= atkPokemon.MaxHP && mine switch
            {
                "Overgrow" => type == PokemonType.Grass, "Blaze" => type == PokemonType.Fire, "Torrent" => type == PokemonType.Water, "Swarm" => type == PokemonType.Bug, _ => false
            })
            power = power * 150 / 100;
        if (typed && type == PokemonType.Fire && theirs == "Heatproof") power /= 2;
        if (typed && type == PokemonType.Fire && theirs == "Dry Skin") power = power * 125 / 100;

        // The stages: Simple doubles its holder's, Unaware ignores the other's, a critical hit the ones that would help the target
        int attackStage = atkPokemon.StatStages.GetValueOrDefault(physical ? StatType.Attack : StatType.SpAttack);
        int defenseStage = defPokemon.StatStages.GetValueOrDefault(physical ? StatType.Defense : StatType.SpDefense);
        if (!rules.SimpleDoublesChanges)
        {
            if (mine == "Simple") attackStage = Math.Clamp(attackStage * 2, -6, 6);
            if (theirs == "Simple") defenseStage = Math.Clamp(defenseStage * 2, -6, 6);
        }
        if (theirs == "Unaware") attackStage = 0;
        if (mine == "Unaware") defenseStage = 0;
        if (result.IsCritical)
        {
            attackStage = Math.Max(0, attackStage);
            defenseStage = Math.Min(0, defenseStage);
        }

        if (typed && mine == "Rivalry" && atkPokemon.Gender != Gender.Genderless && defPokemon.Gender != Gender.Genderless)
            power = atkPokemon.Gender == defPokemon.Gender ? power * 125 / 100 : power * 75 / 100;
        if (mine == "Iron Fist" && (move.Data.Flags & MoveFlags.Punch) != 0) power = power * 12 / 10;
        if (!physical && mine == "Solar Power" && weather == BattleWeather.Sun) attack = attack * 15 / 10;
        // A sandstorm hardens Rock types against special moves; Flower Gift's sun for a side's Attack and Sp. Def; Explosion finds half a Defense
        if (!physical && weather == BattleWeather.Sandstorm && defender.HasType(PokemonType.Rock)) defense = defense * 15 / 10;
        if (physical && weather == BattleWeather.Sun && SideHas(attacker, "Flower Gift")) attack = attack * 15 / 10;
        if (!physical && weather == BattleWeather.Sun && !breaksAbility && SideHas(defender, "Flower Gift")) defense = defense * 15 / 10;
        if (rules.ExplosionHalvesDefense && move.Data.Effect == "HalveDefense") defense /= 2;
        power = Math.Max(1, power);

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
        if (damageTenths != 10) damage = damage * damageTenths / 10;

        if (!noVariance) damage = Formulas.Variance(damage, rng.Roll(RollKind.Damage, 16));

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

    /// <summary>Anyone standing on a battler's side, itself included, has this ability in force (Plus and Minus, Flower Gift: the original's <c>COUNT_ALIVE_BATTLERS_OUR_SIDE</c>).</summary>
    private static bool SideHas(Battler b, string ability) =>
        b.Field != null && b.Field.Battlers().Any(o => o.Side == b.Side && o.IsActive && o.Ability?.Name == ability);

    /// <summary>
    /// Whether a hit is critical: one in the rules' odds for the stage (Platinum's are 1/16, 1/8, 1/4, 1/3, 1/2),
    /// which the move, its effect, the user's ability and item raise, and Focus Energy by two; Battle Armor and a
    /// Lucky Chant over the target's side stop it. The roll is made whatever stops it.
    /// </summary>
    public static bool RollsCritical(Battler attacker, Battler defender, Move move, Random rng, Ruleset rules, int critBonus = 0)
    {
        var attackerEffects = BattleEffects.Of(attacker).ToList();
        bool breaks = attackerEffects.Any(e => e.IgnoresTargetAbility);
        var defenderEffects = BattleEffects.Of(defender, includeAbility: !breaks).ToList();
        int stage = move.Data.CritStage + critBonus + attackerEffects.Sum(e => e.CritStageBonus) + (attacker.Volatile.FocusEnergy ? 2 : 0);
        bool rolled = rng.Roll(RollKind.Critical, rules.CriticalOdds[Math.Clamp(stage, 0, 4)]) == 0;
        return rolled && !defenderEffects.Any(e => e.PreventsCriticalHits) && defender.Field?.Side(defender.Side).LuckyChant != true;
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
