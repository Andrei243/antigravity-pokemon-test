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
/// <item>the critical multiplier, then what the user's item adds before chance (a Life Orb, a Metronome);</item>
/// <item>the roll: all of it down to 85 hundredths;</item>
/// <item>half as much again for a move of the user's own type, then each of the target's types;</item>
/// <item>what the target's and the user's ability and item make of how well the type did (Filter, an Expert Belt).</item>
/// </list>
/// Every step rounds down. An ability's bonus is a line by the ability's name and a held item's a line by its hold
/// effect (<see cref="ItemData.HoldEffect"/>, the original's <c>Battler_HeldItemEffect</c>), each in the
/// original's own place (plan 06 · R7 and R8); what is left of <see cref="BattleEffect"/> here is the hooks
/// abilities still answer (Filter, Tinted Lens, Adaptability, Sniper). The field is read from the target's
/// <see cref="Battler.Field"/>: a battler that stands on none (a test's two Pokémon) is hit as on a bare one.
/// Under rules with <see cref="Ruleset.ModernDamageFormula"/> the same bonuses are put where generation 5 on has them,
/// in 4096ths (<c>DamageCalculator.Modern.cs</c>, plan 06 · R19c).
/// </summary>
public static partial class DamageCalculator
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
    /// can't be hit by Ground moves at all. On the field of an inverse battle (<see cref="FieldState.Inverse"/>,
    /// plan 14 · B1) the chart is asked turned round: no type stops a move there, so none of those immunities is
    /// left to lift, and only magnetism (with the abilities, which are asked elsewhere) still keeps a move off.
    /// </summary>
    /// <param name="rules">The rules whose type chart is used; left out, those of the game in progress.</param>
    public static float Effectiveness(Battler attacker, Battler defender, Move move, Ruleset? rules = null)
    {
        rules ??= Ruleset.Current;
        // Thousand Arrows reaches whatever floats (plan 06 · R25; Showdown's ignoreImmunity), and Telekinesis holds up out of every other Ground move's reach
        if (move.Type == PokemonType.Ground && !HitsWhatFloats(move.Data) && (IsLiftedByMagnetism(defender) || IsHeldUp(defender))) return 0f;
        float total = 1f;
        foreach (var (_, multiplier) in Matchups(attacker, defender, move, rules)) total *= multiplier;
        return total;
    }

    /// <summary>Magnet Rise holds: not for a Pokémon rooted to the ground or weighed down, nor under Gravity.</summary>
    public static bool IsLiftedByMagnetism(Battler defender) =>
        defender.Volatile.MagnetRiseTurns > 0 && !IsHeldDown(defender);

    /// <summary>Telekinesis holds the Pokémon in the air (plan 06 · R25), unless something keeps it on the ground.</summary>
    public static bool IsHeldUp(Battler defender) => defender.Volatile.TelekinesisTurns > 0 && !IsHeldDown(defender);

    /// <summary>Thousand Arrows: the Flying type's immunity and every float counts for nothing against it (Showdown's <c>ignoreImmunity</c> and <c>onEffectiveness</c>).</summary>
    public static bool HitsWhatFloats(MoveData move) => move.Effect == "ThousandArrows";

    /// <summary>
    /// The target's Defense or Sp. Def as it stands before its stage: under Wonder Room (plan 06 · R25) each is the
    /// other's (Showdown's <c>calculateStat</c>: the stat swaps and its stage stays the move's).
    /// </summary>
    public static int DefendingStat(Battler defender, bool physical)
    {
        var p = defender.Pokemon!;
        if (defender.Field?.WonderRoomTurns > 0) physical = !physical;
        return physical ? p.Defense : p.SpDefense;
    }

    /// <summary>Something keeps the Pokémon on the ground whatever its type or ability: Gravity, its own roots, an Iron Ball, Smack Down.</summary>
    public static bool IsHeldDown(Battler b) =>
        b.Field?.Gravity == true || b.Volatile.Ingrained || b.Volatile.SmackedDown || BattleEffects.Of(b).Any(e => e.GroundsHolder);

    /// <summary>Each of the target's types with what the move's type does against it, the immunities that don't hold left out.</summary>
    private static IEnumerable<(PokemonType Type, float Multiplier)> Matchups(Battler attacker, Battler defender, Move move, Ruleset rules)
    {
        bool seesGhosts = defender.Volatile.Identified || BattleEffects.Of(attacker).Any(e => e.HitsGhosts);
        // The types it has right now: a Conversion or a Transform may have changed them for the battle
        var types = defender.Types;
        // An inverse battle's chart, where the target stands on such a battle's field (plan 14 · B1)
        bool inverse = defender.Field?.Inverse == true;

        foreach (var type in types)
        {
            if (type == PokemonType.Flying && defender.Turn.Roosting) continue;
            float multiplier = TypeChart.GetEffectiveness(move.Type, type, null, rules, inverse);
            if (multiplier == 0f)
            {
                if (type == PokemonType.Ghost && move.Type is PokemonType.Normal or PokemonType.Fighting && seesGhosts) continue;
                if (type == PokemonType.Flying && move.Type == PokemonType.Ground && IsHeldDown(defender)) continue;
                if (type == PokemonType.Dark && move.Type == PokemonType.Psychic && defender.Volatile.MiracleEye) continue;
            }
            // The strong winds take the Flying type's weaknesses away (plan 06 · R19b; Showdown's deltastream onEffectiveness)
            if (type == PokemonType.Flying && multiplier > 1f && defender.Field?.SkyInEffect == BattleWeather.StrongWinds) multiplier = 1f;
            // The later moves with a type chart of their own (plan 06 · R25; Showdown's onEffectiveness): Freeze-Dry
            // is twice as strong against Water, Flying Press is the Flying type's matchup too, and Thousand Arrows is
            // neutral to a Flying type it reaches in the air
            if (move.Data.Effect == "FreezeDry" && type == PokemonType.Water) multiplier = inverse ? 0.5f : 2f;
            if (move.Data.Effect == "FlyingPress" && multiplier != 0f) multiplier *= TypeChart.GetEffectiveness(PokemonType.Flying, type, null, rules, inverse);
            if (HitsWhatFloats(move.Data) && type == PokemonType.Flying && move.Type == PokemonType.Ground && multiplier == 0f) multiplier = 1f;
            // Tar Shot makes a Fire move a step more effective, on the target's first type (plan 06 · R26; its condition's onEffectiveness)
            if (defender.Volatile.TarShot && move.Type == PokemonType.Fire && type == types[0]) multiplier *= 2f;
            if (multiplier != 1f) yield return (type, multiplier);
        }
    }

    /// <summary>
    /// On the ground (<c>BattlerIsGrounded</c>): not a Flying type, not floating by Levitate or Magnet Rise; or
    /// weighed down by an Iron Ball or by Gravity all the same. What Spikes, Toxic Spikes, Sticky Web, Arena Trap
    /// and the terrains ask.
    /// </summary>
    public static bool IsGrounded(Battler b)
    {
        var effects = BattleEffects.Of(b).ToList();
        // Smack Down brings a Pokémon down until it leaves the field (plan 06 · R24)
        if (b.Field?.Gravity == true || b.Volatile.SmackedDown || effects.Any(e => e.GroundsHolder)) return true;
        bool floats = !b.Volatile.Ingrained && effects.Any(e => e.Levitates);
        return !floats && b.Volatile.MagnetRiseTurns == 0 && b.Volatile.TelekinesisTurns == 0 && !b.HasType(PokemonType.Flying);
    }

    // ---- The later moves that hit with other numbers (plan 06 · R24; Showdown's data/moves.ts)

    /// <summary>Foul Play: the target's Attack and its stage, with the user's own bonuses (<c>overrideOffensivePokemon: 'target'</c>).</summary>
    public static bool TakesTheTargetsAttack(MoveData move) => move.Effect == "FoulPlay";

    /// <summary>Psyshock, Psystrike and Secret Sword: a special move against the target's Defense (<c>overrideDefensiveStat: 'def'</c>).</summary>
    public static bool HitsTheDefense(MoveData move) => move.Effect == "Psyshock";

    /// <summary>Chip Away and Sacred Sword take no notice of the target's Defense stage, nor of its evasion (<c>ignoreDefensive</c>, <c>ignoreEvasion</c>).</summary>
    public static bool IgnoresDefenseStages(MoveData move) => move.Effect == "ChipAway";

    /// <summary>Storm Throw, Frost Breath and Wicked Blow: every hit is critical, unless the target or its side keeps critical hits off (<c>willCrit</c>).</summary>
    public static bool AlwaysCritical(MoveData move) => move.Effect is "StormThrow" or "SurgingStrikes" or "FlowerTrick";

    /// <summary>
    /// The stat a move hits with: Attack or Sp. Atk by its category, or Body Press's Defense (<c>overrideOffensiveStat:
    /// 'def'</c>; plan 06 · R26), its stage with it.
    /// </summary>
    public static StatType OffensiveStat(MoveData move, bool physical) =>
        move.Effect == "BodyPress" ? StatType.Defense : physical ? StatType.Attack : StatType.SpAttack;

    /// <summary>A Pokémon's stat by its kind, as a hit reads it.</summary>
    private static int StatOf(Pokemon p, StatType stat) => stat switch
    {
        StatType.Attack => p.Attack, StatType.Defense => p.Defense, StatType.SpAttack => p.SpAttack, StatType.SpDefense => p.SpDefense, _ => p.Speed
    };

    /// <summary>Photon Geyser, Sunsteel Strike and Moongeist Beam go through the target's ability as Mold Breaker does (<c>ignoreAbility</c>).</summary>
    public static bool IgnoresAbilities(MoveData move) => move.Effect is "PhotonGeyser" or "SunsteelStrike";

    /// <summary>Photon Geyser is physical when its user's Attack at its stage is higher than its Sp. Atk at its stage (<c>getStat(stat, false, true)</c>).</summary>
    public static bool PhysicalByStats(Battler user, MoveData move)
    {
        if (move.Effect != "PhotonGeyser" || user.Pokemon is not { } p) return false;
        return Formulas.Staged(p.Attack, p.StatStages.GetValueOrDefault(StatType.Attack)) > Formulas.Staged(p.SpAttack, p.StatStages.GetValueOrDefault(StatType.SpAttack));
    }

    /// <summary>A terrain reaches the Pokémon (plan 06 · R19b; Showdown's terrains): it is on the ground and not up in the air, under the ground or the water.</summary>
    public static bool OnTheTerrain(Battler b) => IsGrounded(b) && !b.IsElsewhere;

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
        // A hit of a power of its own (confusion's) is worked out as Struggle's, whatever move was chosen, with
        // no weather and no screens to it (the original's CALC_SELF_HIT)
        bool selfHit = powerOverride.HasValue;
        if (selfHit) move = new Move(BattleCore.StruggleData);
        // Struggle has no type either, but it is a move like any other and can be a critical hit
        typeless |= selfHit || move.Data == BattleCore.StruggleData;
        if (move.Category == MoveCategory.Status || power <= 0) return result;

        var atkPokemon = attacker.Pokemon!;
        var defPokemon = defender.Pokemon!;
        var field = defender.Field;
        var weather = selfHit ? BattleWeather.None : field?.WeatherInEffect ?? BattleWeather.None;
        var attackerEffects = BattleEffects.Of(attacker).ToList();
        bool breaksAbility = attackerEffects.Any(e => e.IgnoresTargetAbility) || IgnoresAbilities(move.Data);
        // Photon Geyser hits as a physical move when its user's Attack at its stage is the higher (plan 06 · R24)
        if (PhysicalByStats(attacker, move.Data) && move.Category == MoveCategory.Special) move = new Move(move.Data.OfCategory(MoveCategory.Physical), move.CurrentPP);
        var defenderEffects = BattleEffects.Of(defender, includeAbility: !breaksAbility).ToList();

        result.TypeMultiplier = typeless ? 1f : Effectiveness(attacker, defender, move, rules);
        if (result.TypeMultiplier == 0f) return result;

        if (!powerOverride.HasValue && !noCrit) result.IsCritical = RollsCritical(attacker, defender, move, rng, rules, critBonus);

        // The power and the stats, each ability's and item's bonus in the original's own place and order
        // (BattleSystem_CalcMoveDamage), every step rounded down. Abilities go by name, items by their hold effect
        // (none under an Embargo or with Klutz); the defender's ability counts for nothing against Mold Breaker.
        string? mine = attacker.Ability?.Name;
        string? theirs = breaksAbility ? null : defender.Ability?.Name;
        string? myHold = BattleEffects.HoldEffectOf(attacker);
        string? theirHold = BattleEffects.HoldEffectOf(defender);
        int myParam = BattleEffects.HoldParamOf(attacker);
        string mySpecies = atkPokemon.Species.Name, theirSpecies = defPokemon.Species.Name;
        bool physical = move.Category == MoveCategory.Physical;
        bool typed = !typeless;
        var type = move.Type;
        // The later moves that take another stat (plan 06 · R24): Foul Play the target's Attack and its stage,
        // Psyshock the target's Defense against a special move
        var offense = TakesTheTargetsAttack(move.Data) ? defPokemon : atkPokemon;
        bool defensePhysical = physical || HitsTheDefense(move.Data);
        int attack = StatOf(offense, OffensiveStat(move.Data, physical));
        int defense = DefendingStat(defender, defensePhysical);

        // The later games put the same bonuses in other places and chain them in 4096ths (plan 06 · R19c)
        if (rules.ModernDamageFormula)
        {
            var hit = new ModernHit(attacker, defender, move, rng, spread, power, powerTenths, pastScreens, noVariance, damageTenths,
                typeless, selfHit, weather, attackerEffects, defenderEffects, breaksAbility);
            result.Damage = hit.Damage(ref result);
            return result;
        }

        // The move's own multiplier (Pursuit's doubling; Reckless sets it from the move's script), a Charge, an
        // ally's Helping Hand, Technician
        power = power * powerTenths / 10;
        if (mine == "Reckless" && (move.Data.RecoilPercent > 0 || move.Data.Effect == "CrashOnMiss")) power = power * 12 / 10;
        if (attacker.Volatile.ChargeTurns > 0 && type == PokemonType.Electric && typed) power *= 2;
        if (attacker.Turn.HelpingHand) power = power * 15 / 10;
        if (mine == "Technician" && move.Data != BattleCore.StruggleData && power <= 60) power = power * 15 / 10;
        if (physical && mine is "Huge Power" or "Pure Power") attack *= 2;
        if (physical && mine == "Slow Start" && field != null && field.Turn() - attacker.Volatile.SlowStartTurn < 5) attack /= 2;
        // The items, in the original's block: a type's boost (the plates with it), a Choice Band or Specs, a
        // Soul Dew on either side, the items of one species (a transformed Pokémon counts as what it became, as the
        // original reads the species in battle), the orbs of the three, a Muscle Band or Wise Glasses
        if (typed && myHold != null && HeldItemEffects.TypeBoostedBy(myHold) == type) power = power * (100 + myParam) / 100;
        if (physical && myHold == "ChoiceAtk") attack = attack * 150 / 100;
        if (!physical && myHold == "ChoiceSpatk") attack = attack * 150 / 100;
        bool frontier = field?.Frontier ?? false;
        if (!physical && myHold == "LatiSpecial" && !frontier && mySpecies is "Latios" or "Latias") attack = attack * 150 / 100;
        if (!defensePhysical && theirHold == "LatiSpecial" && !frontier && theirSpecies is "Latios" or "Latias") defense = defense * 150 / 100;
        if (!physical && myHold == "ClamperlSpatk" && mySpecies == "Clamperl") attack *= 2;
        if (!defensePhysical && theirHold == "ClamperlSpdef" && theirSpecies == "Clamperl") defense *= 2;
        if (myHold == "PikaSpatkUp" && mySpecies == "Pikachu") power *= 2;
        if (defensePhysical && theirHold == "DittoDefUp" && theirSpecies == "Ditto") defense *= 2;
        if (physical && myHold == "CuboneAtkUp" && mySpecies is "Cubone" or "Marowak") attack *= 2;
        if (typed && myHold == "DialgaBoost" && mySpecies == "Dialga" && type is PokemonType.Dragon or PokemonType.Steel) power = power * (100 + myParam) / 100;
        if (typed && myHold == "PalkiaBoost" && mySpecies == "Palkia" && type is PokemonType.Dragon or PokemonType.Water) power = power * (100 + myParam) / 100;
        if (typed && myHold == "GiratinaBoost" && mySpecies == "Giratina" && !attacker.Volatile.Transformed && type is PokemonType.Dragon or PokemonType.Ghost) power = power * (100 + myParam) / 100;
        if (physical && myHold == "PowerUpPhys") power = power * (100 + myParam) / 100;
        if (!physical && myHold == "PowerUpSpec") power = power * (100 + myParam) / 100;
        if (typed && theirs == "Thick Fat" && type is PokemonType.Fire or PokemonType.Ice) power /= 2;
        if (physical && mine == "Hustle") attack = attack * 150 / 100;
        if (physical && mine == "Guts" && atkPokemon.Status != StatusCondition.None) attack = attack * 150 / 100;
        if (defensePhysical && theirs == "Marvel Scale" && defPokemon.Status != StatusCondition.None) defense = defense * 150 / 100;
        if (!physical && ((mine == "Plus" && SideHas(attacker, "Minus")) || (mine == "Minus" && SideHas(attacker, "Plus")))) attack = attack * 150 / 100;
        if (typed && field != null)
        {
            if (type == PokemonType.Electric && field.MudSport()) power /= 2;
            if (type == PokemonType.Fire && field.WaterSport()) power /= 2;
            // The terrains (plan 06 · R19b; Showdown's onBasePower): their own type's moves by a Pokémon on them are
            // 5325/4096 as strong, the quakes against one on Grassy Terrain and Dragon moves against one on Misty
            // Terrain half as strong. Nothing in Platinum's rules lays a terrain, so its numbers never pass here.
            if (field.Terrain is { } terrain && field.Has(terrain))
            {
                bool boosts = terrain switch
                {
                    FieldCondition.ElectricTerrain => type == PokemonType.Electric,
                    FieldCondition.GrassyTerrain => type == PokemonType.Grass,
                    FieldCondition.PsychicTerrain => type == PokemonType.Psychic,
                    _ => false
                };
                if (boosts && OnTheTerrain(attacker)) power = (power * 5325 + 2047) / 4096;
                bool softened = terrain switch
                {
                    FieldCondition.GrassyTerrain => move.Data.Name is "Earthquake" or "Bulldoze" or "Magnitude",
                    FieldCondition.MistyTerrain => type == PokemonType.Dragon,
                    _ => false
                };
                if (softened && OnTheTerrain(defender)) power /= 2;
            }
        }
        if (typed && atkPokemon.CurrentHP * 3 <= atkPokemon.MaxHP && mine switch
            {
                "Overgrow" => type == PokemonType.Grass, "Blaze" => type == PokemonType.Fire, "Torrent" => type == PokemonType.Water, "Swarm" => type == PokemonType.Bug, _ => false
            })
            power = power * 150 / 100;
        if (typed && type == PokemonType.Fire && theirs == "Heatproof") power /= 2;
        if (typed && type == PokemonType.Fire && theirs == "Dry Skin") power = power * 125 / 100;

        // The stages: Simple doubles its holder's, Unaware ignores the other's, a critical hit the ones that would help the target
        int attackStage = offense.StatStages.GetValueOrDefault(OffensiveStat(move.Data, physical));
        int defenseStage = IgnoresDefenseStages(move.Data) ? 0 : defPokemon.StatStages.GetValueOrDefault(defensePhysical ? StatType.Defense : StatType.SpDefense);
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

        // Rivalry asks nothing of the move: it makes even a confused holder's hit on itself a quarter stronger
        if (mine == "Rivalry" && atkPokemon.Gender != Gender.Genderless && defPokemon.Gender != Gender.Genderless)
            power = atkPokemon.Gender == defPokemon.Gender ? power * 125 / 100 : power * 75 / 100;
        if (mine == "Iron Fist" && (move.Data.Flags & MoveFlags.Punch) != 0) power = power * 12 / 10;
        if (!physical && mine == "Solar Power" && weather == BattleWeather.Sun) attack = attack * 15 / 10;
        // A sandstorm hardens Rock types against special moves; Flower Gift's sun for a side's Attack and Sp. Def; Explosion finds half a Defense
        if (!defensePhysical && weather == BattleWeather.Sandstorm && defender.HasType(PokemonType.Rock)) defense = defense * 15 / 10;
        // Snow hardens Ice types against physical moves as a sandstorm hardens Rock types (plan 06 · R19b)
        if (defensePhysical && !selfHit && field?.SkyInEffect == BattleWeather.Snow && defender.HasType(PokemonType.Ice)) defense = defense * 15 / 10;
        if (physical && weather == BattleWeather.Sun && SideHas(attacker, "Flower Gift")) attack = attack * 15 / 10;
        if (!defensePhysical && weather == BattleWeather.Sun && !breaksAbility && SideHas(defender, "Flower Gift")) defense = defense * 15 / 10;
        if (rules.ExplosionHalvesDefense && move.Data.Effect == "HalveDefense") defense /= 2;
        power = Math.Max(1, power);

        attack = Math.Max(1, Formulas.Staged(attack, attackStage));
        defense = Math.Max(1, Formulas.Staged(defense, defenseStage));

        bool burned = atkPokemon.Status == StatusCondition.Burn && physical && !attackerEffects.Any(e => e.IgnoresBurnPenalty);

        // Reflect against physical moves and Light Screen against special ones, unless the hit is critical; two
        // Pokémon behind one screen share it
        var side = field?.Side(defender.Side);
        // (Aurora Veil is both screens at once, and counts once beside either: plan 06 · R19b)
        bool screened = side != null && !result.IsCritical && !pastScreens && !selfHit && ((physical ? side.Reflect : side.LightScreen) || side.AuroraVeil);
        bool shared = screened && field!.Standing(defender.Side) >= 2;

        int damage = Formulas.BaseDamage(atkPokemon.Level, power, attack, defense, burned, spread, screened, shared,
            typeless ? BattleWeather.None : weather, SteamsInTheSun(move.Data, weather) ? PokemonType.Fire : move.Type,
            dimmedBeam: move.Data.Effect == "SkipChargeTurnInSun" && field?.WeatherDimsTheSun == true,
            flashFire: attacker.FlashFire && move.Type == PokemonType.Fire && !typeless);

        if (result.IsCritical) damage = Formulas.Scale(damage, rules.CriticalMultiplier * attackerEffects.Aggregate(1f, (m, e) => Math.Max(m, e.CriticalBoost)));
        // A Life Orb's and a Metronome's bonus are for a move that was used (BattleScript_CalcMoveDamage), not for a hit on oneself
        if (!selfHit)
        {
            if (myHold == "HpDrainOnAtk") damage = damage * (100 + myParam) / 100;
            if (myHold == "BoostRepeated") damage = damage * (10 + attacker.Volatile.MetronomeCount) / 10;
        }
        if (damageTenths != 10) damage = damage * damageTenths / 10;

        if (!noVariance) damage = Formulas.Variance(damage, rng.Roll(RollKind.Damage, 16));

        if (!typeless)
        {
            // Same-type attack bonus
            if (attacker.HasType(move.Type) || move.Data.ForcesStab)
            {
                result.IsSTAB = true;
                float stab = attackerEffects.Select(e => e.StabOverride).Where(s => s.HasValue).Select(s => s!.Value).DefaultIfEmpty(1.5f).Max();
                damage = Formulas.Scale(damage, stab);
            }

            // Each of the target's types in turn; a hit that isn't stopped never rounds down to nothing
            foreach (var (_, against) in Matchups(attacker, defender, move, rules))
                damage = Formulas.Divide(damage * (int)MathF.Round(against * 10f), 10);

            // Filter and Solid Rock, then an Expert Belt on a super-effective hit, then Tinted Lens (BattleSystem_ApplyTypeChart)
            foreach (var e in defenderEffects) damage = Formulas.Scale(damage, e.IncomingDamageMultiplier(defender, attacker, move, result.TypeMultiplier));
            if (result.IsSuperEffective && myHold == "PowerUpSe") damage = damage * (100 + myParam) / 100;
            foreach (var e in attackerEffects) damage = Formulas.Scale(damage, e.DamageMultiplier(attacker, defender, move, result.TypeMultiplier));
            // Glaive Rush leaves its user open to twice the hurt (plan 06 · R26b; its condition's onSourceModifyDamage)
            if (defender.Volatile.GlaiveRush) damage *= 2;
        }

        result.Damage = Math.Max(1, damage);
        return result;
    }

    /// <summary>Anyone standing on a battler's side, itself included, has this ability in force (Plus and Minus, Flower Gift: the original's <c>COUNT_ALIVE_BATTLERS_OUR_SIDE</c>).</summary>
    private static bool SideHas(Battler b, string ability) =>
        b.Field != null && b.Field.Battlers().Any(o => o.Side == b.Side && o.IsActive && o.Ability?.Name == ability);

    /// <summary>
    /// Whether a hit is critical: one in the rules' odds for the stage (Platinum's are 1/16, 1/8, 1/4, 1/3, 1/2),
    /// which the move, its effect, the user's ability and item raise (<see cref="ItemCritStages"/>), and Focus
    /// Energy by two; Battle Armor and a Lucky Chant over the target's side stop it. The roll is made whatever stops it.
    /// </summary>
    public static bool RollsCritical(Battler attacker, Battler defender, Move move, Random rng, Ruleset rules, int critBonus = 0)
    {
        var attackerEffects = BattleEffects.Of(attacker).ToList();
        bool breaks = attackerEffects.Any(e => e.IgnoresTargetAbility);
        var defenderEffects = BattleEffects.Of(defender, includeAbility: !breaks).ToList();
        int stage = move.Data.CritStage + critBonus + attackerEffects.Sum(e => e.CritStageBonus) + ItemCritStages(attacker) + (attacker.Volatile.FocusEnergy ? 2 : 0) + attacker.Volatile.ChiStrike + attacker.Volatile.DragonCheer;
        // Storm Throw, Frost Breath and Wicked Blow are critical with no roll (Showdown's willCrit; plan 06 · R24)
        // Laser Focus makes its user's hits critical with no roll for the turn after it too (plan 06 · R25)
        bool focused = attacker.Volatile.LaserFocusUntil >= (attacker.Field?.Turn() ?? int.MaxValue);
        bool rolled = AlwaysCritical(move.Data) || focused || rng.Roll(RollKind.Critical, rules.CriticalOdds[Math.Clamp(stage, 0, 4)]) == 0;
        return rolled && !defenderEffects.Any(e => e.PreventsCriticalHits) && defender.Field?.Side(defender.Side).LuckyChant != true;
    }

    /// <summary>
    /// The stages a held item adds to the chance of a critical hit (<c>BattleSystem_CalcCriticalMulti</c>): a Scope
    /// Lens or Razor Claw one, a Lucky Punch two for Chansey, a Leek two for Farfetch'd.
    /// </summary>
    public static int ItemCritStages(Battler attacker) => BattleEffects.HoldEffectOf(attacker) switch
    {
        "CritrateUp" => 1,
        "ChanseyCritrateUp" when attacker.Pokemon!.Species.Name == "Chansey" => 2,
        "FarfetchdCritrateUp" when attacker.Pokemon!.Species.Name == "Farfetch'd" => 2,
        _ => 0
    };

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
