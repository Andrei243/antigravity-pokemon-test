using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Effects;

// What each ability does in battle, following Generation 4 (Platinum). AbilityDatabase pairs them with names and
// descriptions. Small building blocks first, then the abilities that need their own code.

/// <summary>A move of one type gets 1.5× power while the user is at a third of its HP or less (Overgrow, Blaze, Torrent, Swarm).</summary>
internal sealed class PinchTypeBoost(PokemonType type) : BattleEffect
{
    public override float PowerMultiplier(Battler self, Battler target, Move move) =>
        move.Type == type && self.Pokemon!.CurrentHP * 3 <= self.Pokemon.MaxHP ? 1.5f : 1f;
}

/// <summary>A stat multiplier on the attacking side, optionally only for one category or while it has a status.</summary>
internal sealed class AttackBoost(float factor, MoveCategory? category = MoveCategory.Physical, bool needsStatus = false) : BattleEffect
{
    public override float AttackMultiplier(Battler self, Move move) =>
        (category == null || move.Category == category) && (!needsStatus || self.Pokemon!.Status != StatusCondition.None) ? factor : 1f;
}

/// <summary>Can't be given one or more major status conditions (Immunity, Insomnia, Limber, Water Veil...).</summary>
internal sealed class StatusImmunity(params StatusCondition[] blocked) : BattleEffect
{
    public override bool BlocksStatus(Battler self, StatusCondition status) => blocked.Contains(status);
}

/// <summary>Moves of one type heal a quarter of its HP instead of hurting it (Volt Absorb, Water Absorb).</summary>
internal sealed class AbsorbType(PokemonType type, string abilityName) : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Type != type || attacker == self) return false;
        var p = self.Pokemon!;
        if (p.CurrentHP >= p.MaxHP) ctx.Announce($"{self.Name}'s {abilityName} made {move.Name} useless!");
        else ctx.RestoreHp(self, p.MaxHP / 4, $"{self.Name}'s {abilityName} restored its HP!");
        return true;
    }
}

/// <summary>Takes less damage from some moves (Thick Fat, Heatproof, Filter, Solid Rock).</summary>
internal sealed class DamageShield(Func<Move, float, bool> applies, float factor) : BattleEffect
{
    public override float IncomingDamageMultiplier(Battler self, Battler attacker, Move move, float effectiveness) =>
        applies(move, effectiveness) ? factor : 1f;
}

/// <summary>A contact move against it may give the attacker a status (Static, Flame Body, Poison Point, Effect Spore).</summary>
internal sealed class ContactStatus(int chancePercent, params StatusCondition[] statuses) : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!move.Data.MakesContact || !attacker.IsActive || attacker.Pokemon!.Status != StatusCondition.None) return;
        if (ctx.Random.Next(100) >= chancePercent) return;
        ctx.TryInflictStatus(attacker, statuses[ctx.Random.Next(statuses.Length)], self);
    }
}

/// <summary>Its stats can't be lowered by other Pokémon (all of them, or one).</summary>
internal sealed class StatGuard(StatType? only = null) : BattleEffect
{
    public override bool BlocksStatDrop(Battler self, StatType stat) => only == null || only == stat;
}

/// <summary>An ability with no effect beyond a flag the engine reads.</summary>
internal sealed class FlagEffect : BattleEffect
{
    public bool NoCrits, NoRecoil, NoConfusion, NoFlinch, BreakAbilities, Unaware, Adaptability;
    public int CritBonus, SideEffectMultiplier = 1, PpPressure, SleepRate = 1;
    public float Accuracy = 1f;

    public override bool PreventsCriticalHits => NoCrits;
    public override bool PreventsRecoil => NoRecoil;
    public override bool BlocksConfusion => NoConfusion;
    public override bool BlocksFlinch => NoFlinch;
    public override bool IgnoresTargetAbility => BreakAbilities;
    public override bool IgnoresOthersStatStages => Unaware;
    public override float? StabOverride => Adaptability ? 2f : null;
    public override int CritStageBonus => CritBonus;
    public override int SideEffectChanceMultiplier => SideEffectMultiplier;
    public override int ExtraPpUsed => PpPressure;
    public override int SleepCountdownRate => SleepRate;
    public override float AccuracyMultiplier(Battler self, Move move) => Accuracy;
}

// ---------------------------------------------------------------- abilities with their own code

internal sealed class Intimidate : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        foreach (var foe in ctx.ActiveFoes(self).ToList())
        {
            ctx.Announce($"{self.Name}'s Intimidate cuts {foe.Name}'s Attack!");
            ctx.ChangeStat(foe, StatType.Attack, -1, self, announceFailure: true);
        }
    }
}

internal sealed class Download : BattleEffect
{
    public override void OnEntry(IBattleContext ctx, Battler self)
    {
        var foes = ctx.ActiveFoes(self).ToList();
        if (foes.Count == 0) return;
        int def = foes.Sum(f => f.Pokemon!.GetEffectiveStat(StatType.Defense));
        int spDef = foes.Sum(f => f.Pokemon!.GetEffectiveStat(StatType.SpDefense));
        ctx.ChangeStat(self, def < spDef ? StatType.Attack : StatType.SpAttack, 1, self);
    }
}

internal sealed class Levitate : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Type != PokemonType.Ground || move.Category == MoveCategory.Status) return false;
        ctx.Announce($"{self.Name} makes Ground moves miss with Levitate!");
        return true;
    }
}

internal sealed class FlashFire : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Type != PokemonType.Fire || attacker == self || self.Pokemon!.Status == StatusCondition.Freeze) return false;
        if (!self.FlashFire)
        {
            self.FlashFire = true;
            ctx.Announce($"{self.Name}'s Flash Fire raised its Fire power!");
        }
        else ctx.Announce($"{self.Name}'s Flash Fire made {move.Name} useless!");
        return true;
    }

    public override float PowerMultiplier(Battler self, Battler target, Move move) =>
        self.FlashFire && move.Type == PokemonType.Fire ? 1.5f : 1f;
}

internal sealed class MotorDrive : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Type != PokemonType.Electric || attacker == self) return false;
        ctx.Announce($"{self.Name}'s Motor Drive took in the electricity!");
        ctx.ChangeStat(self, StatType.Speed, 1, self, announceFailure: true);
        return true;
    }
}

internal sealed class WonderGuard : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if (move.Category == MoveCategory.Status || effectiveness > 1f || attacker == self) return false;
        ctx.Announce($"{self.Name} avoided damage with Wonder Guard!");
        return true;
    }
}

internal sealed class Soundproof : BattleEffect
{
    public override bool AbsorbsMove(IBattleContext ctx, Battler self, Battler attacker, Move move, float effectiveness)
    {
        if ((move.Data.Flags & MoveFlags.Sound) == 0 || attacker == self) return false;
        ctx.Announce($"{self.Name}'s Soundproof blocks {move.Name}!");
        return true;
    }
}

internal sealed class Guts : BattleEffect
{
    public override bool IgnoresBurnPenalty => true;
    public override float AttackMultiplier(Battler self, Move move) =>
        move.Category == MoveCategory.Physical && self.Pokemon!.Status != StatusCondition.None ? 1.5f : 1f;
}

internal sealed class TechnicianEffect : BattleEffect
{
    public override float PowerMultiplier(Battler self, Battler target, Move move) => move.Power <= 60 ? 1.5f : 1f;
}

internal sealed class IronFist : BattleEffect
{
    public override float PowerMultiplier(Battler self, Battler target, Move move) =>
        (move.Data.Flags & MoveFlags.Punch) != 0 ? 1.2f : 1f;
}

internal sealed class Reckless : BattleEffect
{
    public override float PowerMultiplier(Battler self, Battler target, Move move) => move.Data.RecoilPercent > 0 ? 1.2f : 1f;
}

internal sealed class Rivalry : BattleEffect
{
    public override float PowerMultiplier(Battler self, Battler target, Move move)
    {
        var a = self.Pokemon!.Gender;
        var b = target.Pokemon!.Gender;
        if (a == Gender.Genderless || b == Gender.Genderless) return 1f;
        return a == b ? 1.25f : 0.75f;
    }
}

internal sealed class Hustle : BattleEffect
{
    public override float AttackMultiplier(Battler self, Move move) => move.Category == MoveCategory.Physical ? 1.5f : 1f;
    public override float AccuracyMultiplier(Battler self, Move move) => move.Category == MoveCategory.Physical ? 0.8f : 1f;
}

internal sealed class TintedLens : BattleEffect
{
    public override float DamageMultiplier(Battler self, Battler target, Move move, float effectiveness) =>
        effectiveness > 0f && effectiveness < 1f ? 2f : 1f;
}

internal sealed class MarvelScale : BattleEffect
{
    public override float DefenseMultiplier(Battler self, Move move) =>
        move.Category == MoveCategory.Physical && self.Pokemon!.Status != StatusCondition.None ? 1.5f : 1f;
}

internal sealed class QuickFeet : BattleEffect
{
    public override bool IgnoresParalysisSlowdown => true;
    public override float SpeedMultiplier(Battler self) => self.Pokemon!.Status != StatusCondition.None ? 1.5f : 1f;
}

internal sealed class TangledFeet : BattleEffect
{
    public override float EvasionMultiplier(Battler self) => self.IsConfused ? 0.5f : 1f;
}

internal sealed class SimpleEffect : BattleEffect
{
    public override int ScaleStatChange(int amount) => amount * 2;
}

internal sealed class RoughSkin : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!move.Data.MakesContact || !attacker.IsActive || attacker == self) return;
        ctx.LoseHp(attacker, Math.Max(1, attacker.Pokemon!.MaxHP / 8), $"{self.Name}'s Rough Skin hurt {attacker.Name}!");
    }
}

internal sealed class AngerPoint : BattleEffect
{
    public override void AfterHit(IBattleContext ctx, Battler self, Battler attacker, Move move, int damage, bool critical)
    {
        if (!critical || !self.IsActive) return;
        self.Pokemon!.StatStages[StatType.Attack] = 6;
        ctx.Announce($"{self.Name}'s Anger Point maxed its Attack!");
    }
}

internal sealed class Steadfast : BattleEffect
{
    public override void OnFlinch(IBattleContext ctx, Battler self) => ctx.ChangeStat(self, StatType.Speed, 1, self);
}

internal sealed class SpeedBoost : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        if (self.Pokemon!.StatStages.GetValueOrDefault(StatType.Speed) < 6) ctx.ChangeStat(self, StatType.Speed, 1, self);
    }
}

internal sealed class ShedSkin : BattleEffect
{
    public override void AtEndOfTurn(IBattleContext ctx, Battler self)
    {
        if (self.Pokemon!.Status != StatusCondition.None && ctx.Random.Next(100) < 30)
            ctx.CureStatus(self, $"{self.Name}'s Shed Skin cured its {BattleText.StatusName(self.Pokemon.Status)}!");
    }
}

internal sealed class NaturalCure : BattleEffect
{
    public override void OnWithdraw(Battler self)
    {
        if (self.Pokemon!.Status != StatusCondition.Faint) self.Pokemon.Status = StatusCondition.None;
    }
}

internal sealed class Synchronize : BattleEffect
{
    public override void OnConditionChanged(IBattleContext ctx, Battler self, Battler? cause)
    {
        var status = self.Pokemon!.Status;
        if (cause == null || cause == self || !cause.IsActive || cause.Pokemon!.Status != StatusCondition.None) return;
        if (status is StatusCondition.Burn or StatusCondition.Paralyze or StatusCondition.Poison or StatusCondition.Toxic)
        {
            ctx.Announce($"{self.Name}'s Synchronize shares its condition!");
            ctx.TryInflictStatus(cause, status == StatusCondition.Toxic ? StatusCondition.Toxic : status, self, announceFailure: true);
        }
    }
}

internal sealed class NoGuard : BattleEffect
{
    public override bool MovesNeverMiss => true;
}

internal sealed class Scrappy : BattleEffect
{
    public override bool HitsGhosts => true;
}

internal sealed class ShieldDust : BattleEffect
{
    public override bool BlocksSideEffects => true;
}

internal sealed class LiquidOoze : BattleEffect
{
    public override bool HurtsDrainers => true;
}

internal sealed class MagicGuard : BattleEffect
{
    public override bool PreventsIndirectDamage => true;
}

internal sealed class Sniper : BattleEffect
{
    public override float CriticalMultiplier => 3f;
}
