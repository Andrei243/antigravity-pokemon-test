using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Effects;

/// <summary>What an ability or held item can do to the battle around it: queue messages and change the field.</summary>
public interface IBattleContext
{
    Random Random { get; }
    BattleFormat Format { get; }

    /// <summary>Says a line, after what has been said so far.</summary>
    void Announce(string text);

    /// <summary>Raises or lowers a stat stage, with the usual messages. Returns false if nothing changed.</summary>
    bool ChangeStat(Battler target, StatType stat, int amount, Battler? source, bool announceFailure = false);

    /// <summary>Gives a major status condition unless types, abilities or another status prevent it.</summary>
    bool TryInflictStatus(Battler target, StatusCondition status, Battler? source, bool announceFailure = false);

    /// <summary>Restores HP as the message appears; does nothing at full HP.</summary>
    void RestoreHp(Battler target, int amount, string message);

    /// <summary>Takes HP away as the message appears (recoil, Rough Skin, poison).</summary>
    void LoseHp(Battler target, int amount, string message);

    /// <summary>Cures the major status condition (and says so).</summary>
    void CureStatus(Battler target, string message);

    /// <summary>Uses up the held item (berries, Focus Sash).</summary>
    void ConsumeItem(Battler holder);

    /// <summary>The foes standing across from <paramref name="battler"/> that can still fight.</summary>
    IEnumerable<Battler> ActiveFoes(Battler battler);

    /// <summary>The weather and the rest of the field.</summary>
    FieldState Field { get; }

    /// <summary>The rules the battle is fought by.</summary>
    Ruleset Rules { get; }

    /// <summary>
    /// Changes the weather and says <paramref name="line"/>. <paramref name="turns"/> of 0 is weather with no end
    /// of its own. Returns false, saying nothing, when that weather is already there to stay.
    /// </summary>
    bool SetWeather(BattleWeather weather, int turns, string line);

    /// <summary>Someone on the field, still standing, has this ability in force (Damp against Aftermath).</summary>
    bool AnyoneHas(string ability);

    /// <summary>Makes <paramref name="target"/> fall in love with <paramref name="with"/>, by Attract's own rules (Cute Charm). Returns false when it couldn't.</summary>
    bool Infatuate(Battler target, Battler with);

    /// <summary>Everyone standing on the field, the fastest first.</summary>
    IEnumerable<Battler> Everyone { get; }

    /// <summary>The turn being played, counted from 0.</summary>
    int Turn { get; }
}

/// <summary>
/// The fixed points where an ability or a held item joins in a battle (plan 03 · D3). Every hook does nothing by
/// default, so an effect overrides only what it changes. Multipliers from the user's ability and item multiply
/// together.
/// </summary>
public abstract class BattleEffect
{
    // ---- Entering and leaving

    /// <summary>The holder has just come onto the field (Intimidate).</summary>
    public virtual void OnEntry(IBattleContext ctx, Battler self) { }

    /// <summary>The holder is about to be withdrawn (Natural Cure).</summary>
    public virtual void OnWithdraw(Battler self) { }

    // ---- Attacking (the holder is the attacker)

    /// <summary>Multiplies a move's base power (Technician, Blaze, type-boosting items).</summary>
    public virtual float PowerMultiplier(Battler self, Battler target, Move move) => 1f;

    /// <summary>Multiplies the attacking stat (Huge Power, Guts, Choice Band).</summary>
    public virtual float AttackMultiplier(Battler self, Move move) => 1f;

    /// <summary>Multiplies the damage after the critical multiplier and before chance and the types (Life Orb).</summary>
    public virtual float DamageBeforeTheRoll(Battler self, Move move) => 1f;

    /// <summary>Multiplies the final damage, once it is known how well the type did (Expert Belt, Tinted Lens).</summary>
    public virtual float DamageMultiplier(Battler self, Battler target, Move move, float effectiveness) => 1f;

    /// <summary>The bonus for using a move of its own type (Adaptability raises it to 2).</summary>
    public virtual float? StabOverride => null;

    public virtual int CritStageBonus => 0;
    public virtual float AccuracyMultiplier(Battler self, Move move) => 1f;

    /// <summary>The chance of a move's side effect is multiplied by this (Serene Grace).</summary>
    public virtual int SideEffectChanceMultiplier => 1;

    /// <summary>The holder takes no recoil damage (Rock Head).</summary>
    public virtual bool PreventsRecoil => false;

    /// <summary>The holder is held to the first move it uses (Choice items).</summary>
    public virtual bool LocksMoveChoice => false;

    /// <summary>Burns don't halve the holder's physical attacks (Guts).</summary>
    public virtual bool IgnoresBurnPenalty => false;

    /// <summary>Paralysis doesn't slow the holder (Quick Feet).</summary>
    public virtual bool IgnoresParalysisSlowdown => false;

    /// <summary>The holder's moves ignore the target's ability (Mold Breaker).</summary>
    public virtual bool IgnoresTargetAbility => false;

    /// <summary>The holder ignores the other Pokémon's stat stages when they matter to it (Unaware).</summary>
    public virtual bool IgnoresOthersStatStages => false;

    /// <summary>
    /// The holder moves first among moves of the same priority this turn (Quick Claw). <paramref name="roll"/> is
    /// the number its place drew for the turn, 0 to 65,535, as the original draws one for every place.
    /// </summary>
    public virtual bool MovesFirstInBracket(int roll) => false;

    /// <summary>The holder always gets away from a wild Pokémon (Run Away, a Smoke Ball).</summary>
    public virtual bool AlwaysEscapes => false;

    /// <summary>Critical hits do this many times their usual damage (Sniper: half as much again).</summary>
    public virtual float CriticalBoost => 1f;

    /// <summary>Normal and Fighting moves hit Ghost types (Scrappy).</summary>
    public virtual bool HitsGhosts => false;

    /// <summary>Moves by or against the holder never miss (No Guard).</summary>
    public virtual bool MovesNeverMiss => false;

    /// <summary>The holder has damaged its targets with a move (Life Orb, Shell Bell, King's Rock).</summary>
    public virtual void AfterAttacking(IBattleContext ctx, Battler self, Move move, IReadOnlyList<(Battler Target, int Damage)> hits) { }

    // ---- Defending (the holder is the target)

    /// <summary>
    /// The holder takes in a move instead of being hurt by it (Levitate, Volt Absorb, Flash Fire). Returns true
    /// when the move is stopped; the effect announces what happened.
    /// </summary>
    public virtual bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness) => false;

    /// <summary>Multiplies damage the holder takes (Filter, Thick Fat, Heatproof).</summary>
    public virtual float IncomingDamageMultiplier(Battler self, Battler attacker, Move move, float effectiveness) => 1f;

    /// <summary>Multiplies the holder's defending stat (Marvel Scale).</summary>
    public virtual float DefenseMultiplier(Battler self, Move move) => 1f;

    /// <summary>Multiplies the accuracy of moves aimed at the holder (Bright Powder, Sand Veil in a sandstorm).</summary>
    public virtual float EvasionMultiplier(Battler self) => 1f;

    public virtual bool PreventsCriticalHits => false;

    /// <summary>
    /// The holder hangs on with 1 HP instead of fainting (Focus Sash at full HP, Focus Band). Returns true when it
    /// does; the effect announces it.
    /// </summary>
    public virtual bool EnduresHit(IBattleContext ctx, Battler self, int damage) => false;

    /// <summary>The holder was hit by a move (Static, Rough Skin, Anger Point).</summary>
    public virtual void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical) { }

    /// <summary>The side effects of moves that hit the holder never happen (Shield Dust).</summary>
    public virtual bool BlocksSideEffects => false;

    /// <summary>Draining moves hurt the user instead of healing it (Liquid Ooze).</summary>
    public virtual bool HurtsDrainers => false;

    /// <summary>Only direct hits hurt the holder: no poison, burn, recoil or Life Orb damage (Magic Guard).</summary>
    public virtual bool PreventsIndirectDamage => false;

    /// <summary>Each attack aimed at the holder uses this many extra PP (Pressure).</summary>
    public virtual int ExtraPpUsed => 0;

    // ---- Conditions

    public virtual bool BlocksStatus(Battler self, StatusCondition status) => false;
    public virtual bool BlocksConfusion => false;
    public virtual bool BlocksFlinch => false;

    /// <summary>The holder's stats can't be lowered by others (Clear Body, Hyper Cutter for Attack, Keen Eye for accuracy).</summary>
    public virtual bool BlocksStatDrop(Battler self, StatType stat) => false;

    /// <summary>Scales every stat stage change on the holder (Simple doubles them).</summary>
    public virtual int ScaleStatChange(int amount) => amount;

    /// <summary>The holder flinched (Steadfast).</summary>
    public virtual void OnFlinch(IBattleContext ctx, Battler self) { }

    /// <summary>The holder's status or HP changed (cure berries, pinch berries, Synchronize).</summary>
    public virtual void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause) { }

    /// <summary>Sleep counts down this many times faster (Early Bird).</summary>
    public virtual int SleepCountdownRate => 1;

    /// <summary>Speed is multiplied by this (Choice Scarf).</summary>
    public virtual float SpeedMultiplier(Battler self) => 1f;

    /// <summary>End of each turn (Leftovers, Speed Boost, Shed Skin).</summary>
    public virtual void AtEndOfTurn(IBattleContext ctx, Battler self) { }

    /// <summary>The last thing of the holder's turn end, after its conditions have run (a Flame Orb, a Toxic Orb).</summary>
    public virtual void AfterTheTurn(IBattleContext ctx, Battler self) { }

    // ---- The field (plan 06 · R3)

    /// <summary>The weather does nothing while the holder is on the field (Cloud Nine, Air Lock).</summary>
    public virtual bool IgnoresWeather => false;

    /// <summary>The weather, as it passes over the holder at a turn's end, after it has said it goes on (Rain Dish, Ice Body, Dry Skin, Solar Power, Hydration).</summary>
    public virtual void UnderTheWeather(IBattleContext ctx, Battler self, BattleWeather weather) { }

    /// <summary>A sandstorm or hail doesn't hurt the holder (Sand Veil, Snow Cloak, Ice Body, Magic Guard).</summary>
    public virtual bool ShelteredFrom(BattleWeather weather) => false;

    /// <summary>Turns the holder adds to what it starts: "Screens" (Light Clay), "Rain", "Sun", "Sandstorm", "Hail" (the weather rocks).</summary>
    public virtual int ExtraTurns(string of) => 0;

    /// <summary>A binding move the holder uses holds for as long as it can (Grip Claw).</summary>
    public virtual bool BindsToTheEnd => false;

    /// <summary>The holder is on the ground whatever its type or ability (Iron Ball).</summary>
    public virtual bool GroundsHolder => false;

    /// <summary>The holder floats: Ground moves, Spikes and Arena Trap don't reach it (Levitate).</summary>
    public virtual bool Levitates => false;

    /// <summary>A foe can't switch out or run while the holder is on the field (Shadow Tag, Arena Trap, Magnet Pull).</summary>
    public virtual bool Traps(Battler self, Battler foe, bool foeOnTheGround) => false;

    /// <summary>The holder can always be switched out (Shed Shell).</summary>
    public virtual bool SlipsAway => false;

    /// <summary>Roar and Whirlwind can't move the holder (Suction Cups).</summary>
    public virtual bool HoldsItsGround => false;

    /// <summary>The holder can't fall in love (Oblivious).</summary>
    public virtual bool BlocksInfatuation => false;

    /// <summary>A move that charges for a turn strikes at once, and the item is used up (Power Herb).</summary>
    public virtual bool SkipsChargeTurn => false;

    /// <summary>What draining moves, Leech Seed, Ingrain and Aqua Ring give the holder, in hundredths of the usual (Big Root: 130).</summary>
    public virtual int DrainHundredths => 100;

    /// <summary>The holder's moves of two to five hits always hit five times (Skill Link).</summary>
    public virtual bool AlwaysHitsFiveTimes => false;

    /// <summary>The holder's item can't be taken, swapped, knocked off or eaten by another Pokémon (Sticky Hold).</summary>
    public virtual bool KeepsItem => false;

    // ---- Abilities (plan 06 · R7)

    /// <summary>A single-target move of this type aimed at anyone else comes to the holder instead (Lightning Rod, Storm Drain).</summary>
    public virtual bool DrawsMovesOf(PokemonType type) => false;

    /// <summary>The holder loafs around every other turn (Truant).</summary>
    public virtual bool LoafsEveryOtherTurn => false;

    /// <summary>The holder moves after everyone else of its priority (Stall).</summary>
    public virtual bool MovesLast => false;

    /// <summary>The holder eats a pinch berry at half its HP instead of a quarter (Gluttony).</summary>
    public virtual bool EatsBerriesEarly => false;

    /// <summary>Poison heals the holder an eighth a turn instead of hurting it (Poison Heal).</summary>
    public virtual bool HealsWithPoison => false;

    /// <summary>Every move the holder uses is Normal (Normalize).</summary>
    public virtual bool NormalizesMoves => false;

    /// <summary>The holder's held item does nothing, and it can't throw it or make a gift of it (Klutz).</summary>
    public virtual bool IgnoresHeldItem => false;

    /// <summary>The holder's Attack and Speed are halved for its first five turns (Slow Start).</summary>
    public virtual bool StartsSlow => false;

    /// <summary>The holder's Speed doubles once it has lost the item it came in with (Unburden).</summary>
    public virtual bool SpeedsUpUnburdened => false;

    /// <summary>The holder takes the shape the weather or its item gives it, on entry and when the weather changes (Forecast, Flower Gift, Multitype). Returns the line to say, or null when nothing changed.</summary>
    public virtual string? ChangeShape(IBattleContext ctx, Battler self) => null;
}

/// <summary>Finds the effects in play for a Pokémon on the field.</summary>
public static class BattleEffects
{
    /// <summary>Its ability's effect (unless ignored, as by Mold Breaker) and its held item's.</summary>
    public static IEnumerable<BattleEffect> Of(Battler battler, bool includeAbility = true)
    {
        if (battler.Pokemon == null) yield break;
        // Gastro Acid takes the ability away and Embargo the item, for as long as each lasts
        if (includeAbility && battler.Ability?.Effect is { } ability) yield return ability;
        if (ItemOf(battler) is { } item) yield return item;
    }

    /// <summary>The held item's effect alone: none under an Embargo or with Klutz (the original's <c>Battler_HeldItem</c> says it holds nothing).</summary>
    public static BattleEffect? ItemOf(Battler battler) =>
        battler.Pokemon == null || battler.Volatile.EmbargoTurns > 0 || battler.Ability?.Effect is { IgnoresHeldItem: true } ? null : HeldItemEffects.For(battler.Pokemon.HeldItem);

    /// <summary>The item in the holder's hands as far as a move can use it: none under an Embargo or with Klutz.</summary>
    public static ItemData? ItemInHand(Battler battler) =>
        battler.Pokemon == null || battler.Volatile.EmbargoTurns > 0 || battler.Ability?.Effect is { IgnoresHeldItem: true } ? null : battler.Pokemon.HeldItem;

    /// <summary>Whether the battler's ability in force is this one; a defender's is not, against an attacker that breaks abilities.</summary>
    public static bool Has(Battler battler, string ability, Battler? against = null)
    {
        if (battler.Ability?.Name != ability) return false;
        return against == null || battler == against || !Of(against).Any(e => e.IgnoresTargetAbility);
    }
}
