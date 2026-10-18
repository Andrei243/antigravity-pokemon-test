using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// The later abilities' bonuses in the damage formula (plan 06 · R27), by name, each in the chain Pokémon Showdown puts
// it in (data/abilities.ts at the pinned commit: onBasePower, onModifyAtk and onModifySpA, onModifyDef and onModifySpD,
// onSourceModifyDamage and onModifyDamage). They are read by the modern formula alone: no Pokémon of Platinum's rules has
// one, and Platinum's own order has no place for them.
public static partial class DamageCalculator
{
    /// <summary>Everyone standing on the field (an aura, Battery, Steely Spirit and the Ruin abilities reach past their holder).</summary>
    private static IEnumerable<Battler> Standing(Battler b) => b.Field?.Battlers().Where(o => o.IsActive) ?? Enumerable.Empty<Battler>();

    private static bool AllyHas(Battler b, string ability) => Standing(b).Any(o => o != b && o.Side == b.Side && o.Ability?.Name == ability);

    /// <summary>Someone other than <paramref name="b"/> has a Ruin ability on the field (which never adds to another of its kind).</summary>
    private static bool RuinedBy(Battler b, string ability) => Standing(b).Any(o => o != b && o.Ability?.Name == ability) && b.Ability?.Name != ability;

    /// <summary>
    /// A damaging move has an effect Sheer Force gives up (Showdown's <c>secondaries</c>): a chance of a condition, a flinch,
    /// confusion or a stat change on its target, or a stat change of its user's that isn't sure.
    /// </summary>
    internal static bool HasSecondaries(MoveData move) =>
        move.Category != MoveCategory.Status && (move.StatusChancePercent > 0 && move.InflictStatus != StatusCondition.None
            || move.FlinchChancePercent > 0 || move.ConfuseChancePercent > 0
            || (move.TargetStatChange != null && move.StatChangeChancePercent < 100 && move.StatChangeChancePercent > 0)
            || (move.TargetStatChange != null && !move.StatChangeTargetSelf)
            // Its user's rise, sure or not, is a secondary of Showdown's (Flame Charge, Power-Up Punch), where a drop is
            // its self (Close Combat); and a chance its own code rolls (Snore's flinch): plan 06 · R27b, by the oracle
            || (move.TargetStatChange != null && move.StatChangeTargetSelf && move.StatStageAmount > 0)
            || move.EffectChance > 0);

    /// <summary>The power's bonuses (onBasePower, onAllyBasePower, onAnyBasePower).</summary>
    private static void LaterPower(List<int> bp, Battler attacker, Battler defender, Move move, string? mine, bool typed, BattleWeather weather)
    {
        var data = move.Data;
        var type = move.Type;
        var p = attacker.Pokemon!;
        bool physical = move.Category == MoveCategory.Physical;
        if (attacker.Turn.SheerForced) bp.Add(5325);
        if (mine == "Tough Claws" && (data.MakesContact || attacker.Turn.PaddedTouch)) bp.Add(5325);
        if (mine == "Strong Jaw" && (data.Flags & MoveFlags.Bite) != 0) bp.Add(6144);
        if (mine == "Mega Launcher" && (data.Flags & MoveFlags.Pulse) != 0) bp.Add(6144);
        if (mine == "Sharpness" && LaterAbilities.Slicing.Contains(data.Name)) bp.Add(6144);
        if (mine == "Punk Rock" && (data.Flags & MoveFlags.Sound) != 0) bp.Add(5325);
        if (mine == "Toxic Boost" && physical && p.Status is StatusCondition.Poison or StatusCondition.Toxic) bp.Add(6144);
        if (mine == "Flare Boost" && !physical && p.Status == StatusCondition.Burn) bp.Add(6144);
        if (mine == "Analytic" && defender != attacker && defender.MovedThisTurn) bp.Add(5325);
        if (mine == "Sand Force" && typed && weather == BattleWeather.Sandstorm && type is PokemonType.Rock or PokemonType.Ground or PokemonType.Steel) bp.Add(5325);
        if (mine == "Fire Mane" && typed && type == PokemonType.Fire) bp.Add(6144);
        // A move a -ate ability made of a Normal one (generation 7 on: a fifth again)
        if (attacker.Turn.AteBoosted) bp.Add(4915);
        // Parental Bond's second hit is a quarter as strong (generation 7 on)
        if (attacker.Turn.ParentalChild) bp.Add(1024);
        // Supreme Overlord: a tenth again for each of its side that had fainted as it came in, five at most (Showdown's
        // onStart and its table of powers: plan 06 · R27b's audit, where R27 counted at each hit and added 410 a faint)
        if (mine == "Supreme Overlord" && attacker.Volatile.Fallen > 0) bp.Add(OverlordPower[Math.Min(5, attacker.Volatile.Fallen)]);
        // Its allies' (and its own Steely Spirit's)
        if (typed && type == PokemonType.Steel && (mine == "Steely Spirit" || AllyHas(attacker, "Steely Spirit"))) bp.Add(6144);
        if (!physical && AllyHas(attacker, "Battery")) bp.Add(5325);
        if (AllyHas(attacker, "Power Spot")) bp.Add(5325);
        // The auras over the whole field, turned round by Aura Break
        if (typed && (type == PokemonType.Dark && Standing(attacker).Any(o => o.Ability?.Name == "Dark Aura")
                      || type == PokemonType.Fairy && Standing(attacker).Any(o => o.Ability?.Name == "Fairy Aura")))
            bp.Add(Standing(attacker).Any(o => o.Ability?.Name == "Aura Break") ? 3072 : 5448);
    }

    /// <summary>The attacking stat's bonuses (onModifyAtk and onModifySpA, the defender's onSourceModifyAtk, the Ruin abilities' onAnyModify).</summary>
    private static void LaterAttack(List<int> at, Battler attacker, Battler defender, Move move, string? mine, string? theirs, bool physical, bool typed, BattleWeather weather)
    {
        var type = move.Type;
        var p = attacker.Pokemon!;
        if (mine == "Defeatist" && p.CurrentHP * 2 <= p.MaxHP) at.Add(2048);
        if (typed && mine == "Water Bubble" && type == PokemonType.Water) at.Add(8192);
        if (typed && theirs == "Water Bubble" && type == PokemonType.Fire) at.Add(2048);
        if (typed && theirs == "Purifying Salt" && type == PokemonType.Ghost) at.Add(2048);
        if (typed && mine == "Steelworker" && type == PokemonType.Steel) at.Add(6144);
        if (typed && mine == "Transistor" && type == PokemonType.Electric) at.Add(5325);
        if (typed && mine == "Dragon's Maw" && type == PokemonType.Dragon) at.Add(6144);
        if (typed && mine == "Rocky Payload" && type == PokemonType.Rock) at.Add(6144);
        if (physical && mine == "Gorilla Tactics" && !attacker.Volatile.Dynamaxed) at.Add(6144);
        if (physical && mine == "Orichalcum Pulse" && weather is BattleWeather.Sun or BattleWeather.HarshSun) at.Add(5461);
        if (!physical && mine == "Hadron Engine" && attacker.Field?.Has(FieldCondition.ElectricTerrain) == true) at.Add(5461);
        if (LaterAbilities.ParadoxStat(attacker) == (physical ? StatType.Attack : StatType.SpAttack)) at.Add(5325);
        // Stakeout: twice as strong against a Pokémon that came in this turn
        if (mine == "Stakeout" && defender.Field is { } field && defender.Volatile.FirstTurn > field.Turn()) at.Add(8192);
        if (physical ? RuinedBy(attacker, "Tablets of Ruin") : RuinedBy(attacker, "Vessel of Ruin")) at.Add(3072);
    }

    /// <summary>The defending stat's bonuses (onModifyDef and onModifySpD, the Ruin abilities').</summary>
    private static void LaterDefense(List<int> df, Battler defender, string? theirs, bool defensePhysical)
    {
        if (defensePhysical && theirs == "Fur Coat") df.Add(8192);
        if (defensePhysical && theirs == "Grass Pelt" && defender.Field?.Has(FieldCondition.GrassyTerrain) == true) df.Add(6144);
        if (LaterAbilities.ParadoxStat(defender) == (defensePhysical ? StatType.Defense : StatType.SpDefense) && theirs != null) df.Add(5325);
        if (defensePhysical ? RuinedBy(defender, "Sword of Ruin") : RuinedBy(defender, "Beads of Ruin")) df.Add(3072);
    }

    /// <summary>The final modifiers (onSourceModifyDamage, onModifyDamage, Friend Guard's onAnyModifyDamage).</summary>
    private static void LaterFinal(List<int> final, Battler attacker, Battler defender, Move move, string? mine, string? theirs, float effectiveness)
    {
        var p = defender.Pokemon!;
        bool contact = move.Data.MakesContact;
        if ((theirs == "Multiscale" || defender.Ability?.Name == "Shadow Shield") && p.CurrentHP >= p.MaxHP) final.Add(2048);
        if (theirs == "Ice Scales" && move.Category == MoveCategory.Special) final.Add(2048);
        if (theirs == "Fluffy")
        {
            if (move.Type == PokemonType.Fire) final.Add(8192);
            if (contact) final.Add(2048);
        }
        if (theirs == "Punk Rock" && (move.Data.Flags & MoveFlags.Sound) != 0) final.Add(2048);
        if (theirs == "Aura Guard" && contact) final.Add(2048);
        if (mine == "Neuroforce" && effectiveness > 1f) final.Add(5120);
        if (defender != attacker && AllyHas(defender, "Friend Guard")) final.Add(3072);
    }

    /// <summary>Supreme Overlord's power by the fallen it counted (Showdown's <c>powMod</c>).</summary>
    private static readonly int[] OverlordPower = { 4096, 4506, 4915, 5325, 5734, 6144 };

    /// <summary>
    /// The weather the damage formula reads for a hit (plan 06 · R27b): Mega Sol's holder hits as under the sun, whatever
    /// the sky (Showdown's megasol onWeatherModifyDamage, which runs the sun's and returns).
    /// </summary>
    internal static BattleWeather SkyFor(string? mine, BattleWeather weather) => mine == "Mega Sol" ? BattleWeather.Sun : weather;

    /// <summary>Infiltrator passes the screens (and a Substitute: <c>BattleCore.Infiltrates</c>).</summary>
    internal static bool Infiltrates(Battler attacker) => attacker.Ability?.Name == "Infiltrator";

    /// <summary>Merciless: every hit on a poisoned target is critical (onModifyCritRatio's 5).</summary>
    internal static bool Merciless(Battler attacker, Battler defender) =>
        attacker.Ability?.Name == "Merciless" && defender.Pokemon?.Status is StatusCondition.Poison or StatusCondition.Toxic;
}
