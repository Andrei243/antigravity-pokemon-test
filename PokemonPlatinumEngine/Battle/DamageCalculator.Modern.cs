using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

public static partial class DamageCalculator
{
    /// <summary>
    /// One hit by the damage formula of generation 5 on (plan 06 · R19c, <see cref="Ruleset.ModernDamageFormula"/>;
    /// Pokémon Showdown's <c>getDamage</c> and <c>modifyDamage</c>, held to its calculator by the oracle of plan 24 · X6):
    /// <list type="number">
    /// <item>the power, its bonuses chained in 4096ths (Technician, Rivalry, Iron Fist, a type's item, a Muscle Band,
    /// Helping Hand, the terrains, Heatproof, Dry Skin) and rounded half down;</item>
    /// <item>the attacking stat at its stage, then Hustle, then its bonuses chained (Huge Power, Guts, Blaze and its
    /// kin, a Choice Band, Thick Fat); the defending stat at its stage, a sandstorm's or snow's half, then its bonuses;</item>
    /// <item>the base damage and its + 2; a spread hit's 3072, the weather's 6144 or 2048; a critical hit's half again;</item>
    /// <item>for each roll: the roll, the same-type bonus rounded half down, the type chart, a burn's halving, and the
    /// final modifiers chained (the screens, Sniper, Tinted Lens, Filter and Solid Rock, an Expert Belt, a Life Orb,
    /// a Metronome).</item>
    /// </list>
    /// Platinum's bonuses are the same ones; what moves is where each is applied and how it is rounded.
    /// </summary>
    private sealed class ModernHit(Battler attacker, Battler defender, Move move, Random rng, bool spread, int power, int powerTenths,
        bool pastScreens, bool noVariance, int damageTenths, bool typeless, bool selfHit, BattleWeather weather,
        List<BattleEffect> attackerEffects, List<BattleEffect> defenderEffects, bool breaksAbility)
    {
        public int Damage(ref DamageResult result)
        {
            var atkPokemon = attacker.Pokemon!;
            var defPokemon = defender.Pokemon!;
            var field = defender.Field;
            string? mine = attacker.Ability?.Name;
            string? theirs = breaksAbility ? null : defender.Ability?.Name;
            string? myHold = BattleEffects.HoldEffectOf(attacker);
            string? theirHold = BattleEffects.HoldEffectOf(defender);
            int myParam = BattleEffects.HoldParamOf(attacker);
            string mySpecies = atkPokemon.Species.Name, theirSpecies = defPokemon.Species.Name;
            bool physical = move.Category == MoveCategory.Physical;
            bool defensePhysical = physical || HitsTheDefense(move.Data);
            bool typed = !typeless;
            var type = move.Type;
            bool frontier = field?.Frontier ?? false;

            // ---- The power (Showdown's onBasePower, the calculator's bpMods)
            power = power * powerTenths / 10;
            var bp = new List<int>();
            if (damageTenths != 10) bp.Add(damageTenths * 4096 / 10);
            if (attacker.Turn.HelpingHand) bp.Add(6144);
            if (typed && field != null && field.Terrain is { } terrain && field.Has(terrain))
            {
                bool boosts = terrain switch
                {
                    FieldCondition.ElectricTerrain => type == PokemonType.Electric,
                    FieldCondition.GrassyTerrain => type == PokemonType.Grass,
                    FieldCondition.PsychicTerrain => type == PokemonType.Psychic,
                    _ => false
                };
                if (boosts && OnTheTerrain(attacker)) bp.Add(5325);
                bool softened = terrain switch
                {
                    FieldCondition.GrassyTerrain => move.Data.Name is "Earthquake" or "Bulldoze" or "Magnitude",
                    FieldCondition.MistyTerrain => type == PokemonType.Dragon,
                    _ => false
                };
                if (softened && OnTheTerrain(defender)) bp.Add(2048);
            }
            if (mine == "Technician" && move.Data != BattleCore.StruggleData && power <= 60) bp.Add(6144);
            // Normalize adds a fifth to the moves it makes Normal (generation 7 on; the core has made the move Normal already)
            if (typed && mine == "Normalize") bp.Add(4915);
            if (mine == "Rivalry" && atkPokemon.Gender != Gender.Genderless && defPokemon.Gender != Gender.Genderless)
                bp.Add(atkPokemon.Gender == defPokemon.Gender ? 5120 : 3072);
            if ((mine == "Reckless" && (move.Data.RecoilPercent > 0 || move.Data.Effect == "CrashOnMiss"))
                || (mine == "Iron Fist" && (move.Data.Flags & MoveFlags.Punch) != 0)) bp.Add(4915);
            if (typed && type == PokemonType.Fire && theirs == "Heatproof") bp.Add(2048);
            if (typed && type == PokemonType.Fire && theirs == "Dry Skin") bp.Add(5120);
            if (attacker.Volatile.ChargeTurns > 0 && type == PokemonType.Electric && typed) bp.Add(8192);
            if (typed && field != null && type == PokemonType.Electric && field.MudSport()) bp.Add(1352);
            if (typed && field != null && type == PokemonType.Fire && field.WaterSport()) bp.Add(1352);
            // The items: a type's boost and the plates, the orbs of the three, a Soul Dew for Latios and Latias's own
            // types (generation 7 on), a Muscle Band or Wise Glasses
            if (typed && myHold != null && HeldItemEffects.TypeBoostedBy(myHold) == type) bp.Add(In(100 + myParam, 100));
            else if (typed && myHold == "DialgaBoost" && mySpecies == "Dialga" && type is PokemonType.Dragon or PokemonType.Steel) bp.Add(In(100 + myParam, 100));
            else if (typed && myHold == "PalkiaBoost" && mySpecies == "Palkia" && type is PokemonType.Dragon or PokemonType.Water) bp.Add(In(100 + myParam, 100));
            else if (typed && myHold == "GiratinaBoost" && mySpecies == "Giratina" && !attacker.Volatile.Transformed && type is PokemonType.Dragon or PokemonType.Ghost) bp.Add(In(100 + myParam, 100));
            else if (typed && myHold == "LatiSpecial" && !frontier && mySpecies is "Latios" or "Latias" && type is PokemonType.Dragon or PokemonType.Psychic) bp.Add(4915);
            else if (physical && myHold == "PowerUpPhys") bp.Add(In(100 + myParam, 100));
            else if (!physical && myHold == "PowerUpSpec") bp.Add(In(100 + myParam, 100));
            // Solar Beam under a sky that dims the sun
            if (move.Data.Effect == "SkipChargeTurnInSun" && field?.WeatherDimsTheSun == true) bp.Add(2048);
            // The later abilities' (plan 06 · R27; DamageCalculator.Abilities.cs)
            if (!selfHit) LaterPower(bp, attacker, defender, move, mine, typed, weather);
            // The later items' (plan 06 · R28a; DamageCalculator.Items.cs)
            if (!selfHit) LaterItemPower(bp, attacker, move, myHold);
            power = Math.Max(1, (int)Formulas.RoundHalfDown((long)power * Formulas.Chain(bp, 41, 2097152)));

            // ---- The stats: the stage first, then the bonuses (onModifyAtk and its kin)
            // Foul Play takes the target's Attack and its stage, Psyshock the target's Defense, Chip Away no notice of
            // the target's stages (plan 06 · R24; Showdown's overrideOffensivePokemon, overrideDefensiveStat, ignoreDefensive)
            var offense = TakesTheTargetsAttack(move.Data) ? defPokemon : atkPokemon;
            // Body Press its user's Defense and its stage (plan 06 · R26; overrideOffensiveStat)
            int attackStage = offense.StatStages.GetValueOrDefault(OffensiveStat(move.Data, physical));
            int defenseStage = IgnoresDefenseStages(move.Data) ? 0 : defPokemon.StatStages.GetValueOrDefault(defensePhysical ? StatType.Defense : StatType.SpDefense);
            if (theirs == "Unaware") attackStage = 0;
            if (mine == "Unaware") defenseStage = 0;
            if (result.IsCritical)
            {
                attackStage = Math.Max(0, attackStage);
                defenseStage = Math.Min(0, defenseStage);
            }
            int attack = Formulas.Staged(StatOf(offense, OffensiveStat(move.Data, physical)), attackStage);
            int defense = Formulas.Staged(DefendingStat(defender, defensePhysical), defenseStage);

            // Hustle is applied to the stat itself, before the chain
            if (physical && mine == "Hustle") attack = attack * 3 / 2;
            var at = new List<int>();
            if (physical && mine == "Slow Start" && field != null && field.Turn() - attacker.Volatile.SlowStartTurn < 5) at.Add(2048);
            if (physical && weather == BattleWeather.Sun && SideHas(attacker, "Flower Gift")) at.Add(6144);
            if (physical && mine == "Guts" && atkPokemon.Status != StatusCondition.None) at.Add(6144);
            if (!physical && mine == "Solar Power" && weather == BattleWeather.Sun) at.Add(6144);
            if (attacker.FlashFire && type == PokemonType.Fire && typed) at.Add(6144);
            if (typed && atkPokemon.CurrentHP * 3 <= atkPokemon.MaxHP && mine switch
                {
                    "Overgrow" => type == PokemonType.Grass, "Blaze" => type == PokemonType.Fire, "Torrent" => type == PokemonType.Water, "Swarm" => type == PokemonType.Bug, _ => false
                })
                at.Add(6144);
            if (!physical && ((mine == "Plus" && SideHas(attacker, "Minus")) || (mine == "Minus" && SideHas(attacker, "Plus")))) at.Add(6144);
            if (physical && mine is "Huge Power" or "Pure Power") at.Add(8192);
            if (typed && theirs == "Thick Fat" && type is PokemonType.Fire or PokemonType.Ice) at.Add(2048);
            if (physical && myHold == "CuboneAtkUp" && mySpecies is "Cubone" or "Marowak") at.Add(8192);
            if (!physical && myHold == "ClamperlSpatk" && mySpecies == "Clamperl") at.Add(8192);
            if (myHold == "PikaSpatkUp" && mySpecies == "Pikachu") at.Add(8192);
            // (A Choice item does nothing for a Dynamaxed Pokémon: plan 06 · R22, Showdown's choiceband onModifyAtk)
            if (((physical && myHold == "ChoiceAtk") || (!physical && myHold == "ChoiceSpatk")) && !attacker.Volatile.Dynamaxed) at.Add(6144);
            if (!selfHit) LaterAttack(at, attacker, defender, move, mine, theirs, physical, typed, weather);
            attack = Math.Max(1, (int)Formulas.RoundHalfDown((long)attack * Formulas.Chain(at, 410, 131072)));

            // A sandstorm hardens Rock types against special moves and snow Ice types against physical ones, on the stat itself
            if (!defensePhysical && weather == BattleWeather.Sandstorm && defender.HasType(PokemonType.Rock)) defense = defense * 3 / 2;
            if (defensePhysical && !selfHit && field?.SkyInEffect == BattleWeather.Snow && defender.HasType(PokemonType.Ice)) defense = defense * 3 / 2;
            var df = new List<int>();
            if (defensePhysical && theirs == "Marvel Scale" && defPokemon.Status != StatusCondition.None) df.Add(6144);
            if (!defensePhysical && weather == BattleWeather.Sun && !breaksAbility && SideHas(defender, "Flower Gift")) df.Add(6144);
            if (!defensePhysical && theirHold == "ClamperlSpdef" && theirSpecies == "Clamperl") df.Add(8192);
            if (defensePhysical && theirHold == "DittoDefUp" && theirSpecies == "Ditto" && !defender.Volatile.Transformed) df.Add(8192);
            if (!selfHit) LaterDefense(df, defender, theirs, defensePhysical);
            if (!selfHit) LaterItemDefense(df, defender, theirHold, defensePhysical);
            defense = Math.Max(1, (int)Formulas.RoundHalfDown((long)defense * Formulas.Chain(df, 410, 131072)));

            // ---- The base damage, then a spread hit, the weather and a critical hit
            int damage = Formulas.ModernBaseDamage(atkPokemon.Level, power, attack, defense);
            if (spread) damage = Formulas.Modify(damage, 3072);
            if (typed && !selfHit && !UnderAnUmbrella(defender))
            {
                // (Mega Sol hits as under the sun: plan 06 · R27b)
                var sky = SkyFor(mine, weather);
                bool strong = (sky == BattleWeather.Sun && (type == PokemonType.Fire || SteamsInTheSun(move.Data, sky))) || (sky == BattleWeather.Rain && type == PokemonType.Water);
                bool weak = (sky == BattleWeather.Sun && type == PokemonType.Water && !SteamsInTheSun(move.Data, sky)) || (sky == BattleWeather.Rain && type == PokemonType.Fire);
                if (strong) damage = Formulas.Modify(damage, 6144);
                else if (weak) damage = Formulas.Modify(damage, 2048);
            }
            if (result.IsCritical) damage = damage * 3 / 2;

            // ---- The same-type bonus, the type chart, a burn and the final modifiers
            int stab = 4096;
            // A Terastallized user's bonus goes by its Tera Type and its own types both (plan 06 · R23)
            if (typed && TeraStab(attacker, move, type, attackerEffects) is { } tera)
            {
                stab = tera.Stab;
                result.IsSTAB = tera.IsStab;
            }
            else if (typed && (attacker.HasType(type) || move.Data.ForcesStab))
            {
                result.IsSTAB = true;
                float factor = attackerEffects.Select(e => e.StabOverride).Where(s => s.HasValue).Select(s => s!.Value).DefaultIfEmpty(1.5f).Max();
                stab = Formulas.In4096ths(factor);
            }
            float effectiveness = typed ? result.TypeMultiplier : 1f;
            bool burned = atkPokemon.Status == StatusCondition.Burn && physical && !attackerEffects.Any(e => e.IgnoresBurnPenalty);

            var final = new List<int>();
            var side = field?.Side(defender.Side);
            bool screened = side != null && !result.IsCritical && !pastScreens && !selfHit && !Infiltrates(attacker) && ((physical ? side.Reflect : side.LightScreen) || side.AuroraVeil);
            if (screened) final.Add(field!.Standing(defender.Side) >= 2 ? 2732 : 2048);
            if (result.IsCritical) final.Add(Formulas.In4096ths(attackerEffects.Aggregate(1f, (m, e) => Math.Max(m, e.CriticalBoost))));
            // Glaive Rush leaves its user open to twice the hurt (plan 06 · R26b)
            if (defender.Volatile.GlaiveRush) final.Add(8192);
            if (typed)
            {
                foreach (var e in attackerEffects) final.Add(Formulas.In4096ths(e.DamageMultiplier(attacker, defender, move, result.TypeMultiplier)));
                foreach (var e in defenderEffects) final.Add(Formulas.In4096ths(e.IncomingDamageMultiplier(defender, attacker, move, result.TypeMultiplier)));
            }
            if (!selfHit)
            {
                if (typed && result.IsSuperEffective && myHold == "PowerUpSe") final.Add(In(100 + myParam, 100));
                else if (myHold == "HpDrainOnAtk") final.Add(In(100 + myParam, 100));
                else if (myHold == "BoostRepeated") final.Add(4096 + 819 * Math.Min(5, Math.Max(0, attacker.Volatile.MetronomeCount - 1)));
            }
            if (typed && !selfHit) LaterFinal(final, attacker, defender, move, mine, theirs, result.TypeMultiplier);
            int finalMod = Formulas.Chain(final, 41, 131072);

            int roll = noVariance ? 0 : rng.Roll(RollKind.Damage, 16);
            return Formulas.ModernFinalDamage(damage, roll, stab, effectiveness, burned, finalMod, rolled: !noVariance);
        }

        /// <summary>A bonus the original writes as a fraction, in 4096ths, rounded down (a Life Orb's 130/100 is 5324).</summary>
        private static int In(int numerator, int denominator) => numerator * 4096 / denominator;
    }
}
