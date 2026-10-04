using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Effects;

/// <summary>What an ability or held item can do to the battle around it: queue messages and change the field.</summary>
public interface IBattleContext
{
    Random Random { get; }
    BattleFormat Format { get; }

    /// <summary>Shows a message once the current step finishes; <paramref name="onShow"/> runs as it appears (HP changes, animations).</summary>
    void Announce(string text, Action? onShow = null);

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

    /// <summary>Multiplies the final damage (Life Orb, Expert Belt, Tinted Lens).</summary>
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

    /// <summary>The holder may move first among moves of the same priority (Quick Claw).</summary>
    public virtual bool MovesFirstInBracket(Random random) => false;

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
}

/// <summary>Finds the effects in play for a Pokémon on the field.</summary>
public static class BattleEffects
{
    /// <summary>Its ability's effect (unless ignored, as by Mold Breaker) and its held item's.</summary>
    public static IEnumerable<BattleEffect> Of(Battler battler, bool includeAbility = true)
    {
        var p = battler.Pokemon;
        if (p == null) yield break;
        if (includeAbility && p.Ability?.Effect is { } ability) yield return ability;
        if (HeldItemEffects.For(p.HeldItem) is { } item) yield return item;
    }
}
