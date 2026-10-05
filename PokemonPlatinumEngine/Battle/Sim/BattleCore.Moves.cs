using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// Using a move: the checks before it (sleep, freeze, flinching, confusion, paralysis), its targets, accuracy and
// damage, the side effects, and what abilities and items do around it.
public sealed partial class BattleCore
{
    /// <summary>What a Pokémon does when it has no PP left: hurts the target and a quarter of its own HP.</summary>
    public static readonly MoveData StruggleData = new()
    {
        Name = "Struggle",
        Type = PokemonType.Normal,
        Category = MoveCategory.Physical,
        Power = 50,
        Accuracy = 0,
        MaxPP = 1,
        Flags = MoveFlags.Contact,
        Description = "Used only when no other move has PP left. It also hurts the user."
    };

    /// <summary>How one target of a move came out of it.</summary>
    private sealed class MoveHit
    {
        public required Battler Target;
        public bool Missed, Immune, Absorbed, Endured;
        public DamageCalculator.DamageResult Damage;
        public int Dealt;
        public List<BattleEvent> Notes = new();
        public bool Landed => !Missed && !Immune && !Absorbed;
    }

    /// <summary>The moves a Pokémon can pick: any with PP left, or only its locked move while a Choice item holds it.</summary>
    public static List<Move> UsableMoves(Battler b)
    {
        var moves = b.Pokemon!.Moves.Where(m => m.CurrentPP > 0).ToList();
        if (b.ChoiceLock != null && moves.Contains(b.ChoiceLock)) return new List<Move> { b.ChoiceLock };
        return moves;
    }

    private void ExecuteMove(Battler user, Move move, Battler? chosenTarget)
    {
        user.MovedThisTurn = true;
        if (CanMove(user, move)) UseMove(user, move, chosenTarget);
    }

    /// <summary>
    /// Sleep, freeze, flinching, confusion and paralysis can each stop a Pokémon from acting, in that order
    /// (the original's <c>BattleControllerPlayer_CheckStatusDisruption</c>).
    /// </summary>
    private bool CanMove(Battler user, Move move)
    {
        var p = user.Pokemon!;
        var effects = BattleEffects.Of(user).ToList();

        switch (p.Status)
        {
            case StatusCondition.Sleep:
                p.SleepTurns -= effects.Select(e => e.SleepCountdownRate).DefaultIfEmpty(1).Max();
                if (p.SleepTurns > 0)
                {
                    Say($"{user.Name} is fast asleep.");
                    return false;
                }
                p.Status = StatusCondition.None;
                p.SleepTurns = 0;
                Say($"{user.Name} woke up!").With(new StatusChanged(user.Place, StatusCondition.None));
                break;

            case StatusCondition.Freeze:
                // One time in five it thaws; a move that burns thaws its user at once
                if (!move.Data.ThawsUser && rng.Roll(RollKind.Thaw, 5) != 0)
                {
                    Say($"{user.Name} is frozen solid!");
                    return false;
                }
                p.Status = StatusCondition.None;
                Say($"{user.Name} thawed out!").With(new StatusChanged(user.Place, StatusCondition.None));
                break;
        }

        if (user.Flinched)
        {
            user.Flinched = false;
            Say($"{user.Name} flinched!");
            foreach (var e in effects) e.OnFlinch(this, user);
            return false;
        }

        if (user.IsConfused)
        {
            if (--user.ConfusionTurns <= 0)
            {
                Say($"{user.Name} snapped out of confusion!");
            }
            else
            {
                Say($"{user.Name} is confused!");
                // It hurts itself one time in the rules' odds (the die's first face): a typeless 40-power hit
                if (rng.Roll(RollKind.ConfusionSelfHit, Rules.ConfusionSelfHitOdds) == 0)
                {
                    var self = DamageCalculator.Calculate(user, user, move, rng, spread: false, powerOverride: 40, rules: Rules);
                    p.CurrentHP -= Math.Min(p.CurrentHP, self.Damage);
                    Say("It hurt itself in its confusion!")
                        .AtImpact(new Struck(user.Place, p.CurrentHP, Hard: false)).AtImpact(new HitSounded(false));
                    return false;
                }
            }
        }

        // One time in four; a Pokémon with Magic Guard is never held by it, as in Platinum
        if (p.Status == StatusCondition.Paralyze && !effects.Any(e => e.PreventsIndirectDamage) && rng.Roll(RollKind.FullParalysis, 4) == 0)
        {
            Say($"{user.Name} is paralyzed! It can't move!");
            return false;
        }
        return true;
    }

    /// <summary>Who a move hits: its target type, the chosen target (or a stand-in if that one is gone).</summary>
    private List<Battler> ResolveTargets(Battler user, Move move, Battler? chosen)
    {
        var foes = SlotsOf(Other(user.Side)).Where(b => b.IsActive).ToList();
        switch (move.Target)
        {
            case MoveTarget.User:
            case MoveTarget.UserSide:
            case MoveTarget.UserOrAlly:
            case MoveTarget.Field:
                return new List<Battler> { user };
            case MoveTarget.UserAndAllies:
                return SlotsOf(user.Side).Where(b => b.IsActive).ToList();
            case MoveTarget.Ally:
            case MoveTarget.Allies:
                return SlotsOf(user.Side).Where(b => b.IsActive && b != user).ToList();
            case MoveTarget.AllPokemon:
                return AllBattlers.Where(b => b.IsActive).ToList();
            case MoveTarget.AllFoes:
            case MoveTarget.FoeSide:
                return foes;
            case MoveTarget.AllOthers:
                return AllBattlers.Where(b => b.IsActive && b != user).ToList();
            case MoveTarget.RandomFoe:
                return foes.Count == 0 ? foes : new List<Battler> { foes[rng.Roll(RollKind.Target, foes.Count)] };
            default:
                if (chosen != null && chosen.IsActive && chosen != user) return new List<Battler> { chosen };
                // The chosen foe is gone: the move goes to the other one (an ally that fainted leaves nothing to hit)
                if (chosen != null && chosen.Side == user.Side) return new List<Battler>();
                return foes.Count == 0 ? foes : new List<Battler> { foes[rng.Roll(RollKind.Target, foes.Count)] };
        }
    }

    private void UseMove(Battler user, Move move, Battler? chosenTarget)
    {
        var targets = ResolveTargets(user, move, chosenTarget);

        // PP: one, plus one for each foe with Pressure it is aimed at
        int pp = 1 + targets.Where(t => t.Side != user.Side).Sum(t => BattleEffects.Of(t).Sum(e => e.ExtraPpUsed));
        move.CurrentPP = Math.Max(0, move.CurrentPP - pp);
        if (BattleEffects.Of(user).Any(e => e.LocksMoveChoice) && user.Pokemon!.Moves.Contains(move)) user.ChoiceLock = move;
        if (user.IsPlayerSide) Evolution.CountMoveUse(user.Pokemon!, move.Name);

        var used = Say($"{user.Name} used {move.Name}!");
        if (targets.Count == 0)
        {
            Say("But there was no target...");
            return;
        }
        used.With(new Lunged(user.Place, move.Category));

        var hits = targets.Select(t => new MoveHit { Target = t }).ToList();
        // A move whose effect isn't in the engine yet (Support None) does nothing rather than a made-up hit
        bool damaging = move.Category != MoveCategory.Status && move.Power > 0 && move.Data.Support != MoveEffectSupport.None;
        if (!damaging)
        {
            // A status move plays its effect on each target (on itself for moves like Swords Dance)
            foreach (var hit in hits) used.With(Shown(user, hit.Target, move, null));
            ApplyStatusMove(user, move, hits);
            return;
        }

        bool userBreaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
        foreach (var hit in hits)
        {
            var t = hit.Target;
            if (!Hits(user, t, move))
            {
                hit.Missed = true;
                continue;
            }

            float effectiveness = move.Data == StruggleData ? 1f : DamageCalculator.Effectiveness(user, t, move, Rules);
            var guards = BattleEffects.Of(t, includeAbility: !userBreaks).ToList();
            hit.Notes.AddRange(Capture(() => hit.Absorbed = guards.Any(e => e.AbsorbsMove(this, t, user, move, effectiveness))));
            if (hit.Absorbed) continue;
            if (effectiveness == 0f)
            {
                hit.Immune = true;
                continue;
            }

            hit.Damage = DamageCalculator.Calculate(user, t, move, rng, spread: targets.Count > 1, rules: Rules);
            hit.Dealt = Math.Min(t.Pokemon!.CurrentHP, hit.Damage.Damage);
            if (hit.Dealt >= t.Pokemon.CurrentHP)
            {
                hit.Notes.AddRange(Capture(() => hit.Endured = guards.Any(e => e.EnduresHit(this, t, hit.Dealt))));
                if (hit.Endured) hit.Dealt = t.Pokemon.CurrentHP - 1;
            }
        }

        // The move's effect flies to each target and lands with the damage
        foreach (var hit in hits) used.With(Shown(user, hit.Target, move, hit));
        foreach (var hit in hits.Where(h => h.Landed && h.Dealt > 0))
        {
            hit.Target.Pokemon!.CurrentHP -= hit.Dealt;
            used.AtImpact(new Struck(hit.Target.Place, hit.Target.Pokemon.CurrentHP, hit.Damage.IsCritical || hit.Damage.IsSuperEffective));
        }
        if (hits.Any(h => h.Landed)) used.AtImpact(new HitSounded(hits.Any(h => h.Landed && h.Damage.IsSuperEffective)));

        AfterDamagingMove(user, move, hits);
    }

    /// <summary>The effect a move plays on one target: what kind of move it is and, for a damaging move, how it landed.</summary>
    private static MoveShown Shown(Battler user, Battler target, Move move, MoveHit? hit) => new(
        move.Name, move.Type, move.Category, user.Place, target.Place,
        Missed: hit?.Missed ?? false,
        Blocked: hit != null && (hit.Immune || hit.Absorbed),
        Critical: hit is { Landed: true } && hit.Damage.IsCritical,
        SuperEffective: hit is { Landed: true } && hit.Damage.IsSuperEffective);

    /// <summary>
    /// Whether a move reaches its target (<c>BattleControllerPlayer_CheckMoveHitAccuracy</c>): its accuracy by the
    /// stages, then by the user's ability, the target's ability, the target's item and the user's item, each
    /// rounded down in turn; it hits when a roll of 100 comes out below the result.
    /// </summary>
    private bool Hits(Battler user, Battler target, Move move)
    {
        if (move.Accuracy <= 0 || target == user) return true;
        var userEffects = BattleEffects.Of(user).ToList();
        bool breaks = userEffects.Any(e => e.IgnoresTargetAbility);
        var targetEffects = BattleEffects.Of(target, includeAbility: !breaks).ToList();
        if (userEffects.Concat(targetEffects).Any(e => e.MovesNeverMiss)) return true;

        int accuracy = targetEffects.Any(e => e.IgnoresOthersStatStages) ? 0 : user.Pokemon!.StatStages.GetValueOrDefault(StatType.Accuracy);
        int evasion = userEffects.Any(e => e.IgnoresOthersStatStages) ? 0 : target.Pokemon!.StatStages.GetValueOrDefault(StatType.Evasion);
        int rate = Formulas.HitRate(move.Accuracy, accuracy, evasion);

        var userAbility = user.Pokemon!.Ability?.Effect;
        var userItem = HeldItemEffects.For(user.Pokemon.HeldItem);
        var targetAbility = breaks ? null : target.Pokemon!.Ability?.Effect;
        var targetItem = HeldItemEffects.For(target.Pokemon!.HeldItem);
        if (userAbility != null) rate = Formulas.Scale(rate, userAbility.AccuracyMultiplier(user, move));
        if (targetAbility != null) rate = Formulas.Scale(rate, targetAbility.EvasionMultiplier(target));
        if (targetItem != null) rate = Formulas.Scale(rate, targetItem.EvasionMultiplier(target));
        if (userItem != null) rate = Formulas.Scale(rate, userItem.AccuracyMultiplier(user, move));

        return rng.Roll(RollKind.Accuracy, 100) < rate;
    }

    /// <summary>What each target makes of a damaging move, its side effects, then what the attacker gets out of it.</summary>
    private void AfterDamagingMove(Battler user, Move move, List<MoveHit> hits)
    {
        var data = move.Data;
        bool several = hits.Count > 1;
        var userEffects = BattleEffects.Of(user).ToList();
        bool userBreaks = userEffects.Any(e => e.IgnoresTargetAbility);
        int chanceMult = userEffects.Select(e => e.SideEffectChanceMultiplier).DefaultIfEmpty(1).Max();

        foreach (var hit in hits)
        {
            var t = hit.Target;
            if (hit.Missed)
            {
                Say(several || IsDouble ? $"{t.Name} avoided the attack!" : $"{user.Name}'s attack missed!");
                continue;
            }
            if (!hit.Endured) log.AddRange(hit.Notes);
            if (hit.Absorbed) continue;
            if (hit.Immune)
            {
                Say($"It doesn't affect {t.Name}...");
                continue;
            }

            if (hit.Damage.IsCritical) Say(several ? $"A critical hit on {t.Name}!" : "A critical hit!");
            if (hit.Damage.IsSuperEffective) Say(several ? $"It's super effective on {t.Name}!" : "It's super effective!");
            if (hit.Damage.IsNotVeryEffective) Say(several ? $"It's not very effective on {t.Name}..." : "It's not very effective...");
            if (hit.Endured) log.AddRange(hit.Notes);
            if (hit.Damage.IsCritical) t.TookCriticalHit = true;

            bool standing = t.Pokemon!.CurrentHP > 0;

            // A Fire move thaws a frozen target
            if (standing && move.Type == PokemonType.Fire && t.Pokemon.Status == StatusCondition.Freeze)
            {
                t.Pokemon.Status = StatusCondition.None;
                Say($"{t.Name} thawed out!").With(new StatusChanged(t.Place, StatusCondition.None));
            }

            // Side effects on the target
            bool shielded = BattleEffects.Of(t, includeAbility: !userBreaks).Any(e => e.BlocksSideEffects);
            if (standing && !shielded && t != user)
            {
                if (data.InflictStatus != StatusCondition.None && Chance(data.StatusChancePercent * chanceMult))
                    TryInflictStatus(t, data.InflictStatus, user);
                if (data.TargetStatChange is { } stat && !data.StatChangeTargetSelf && Chance(data.StatChangeChancePercent * chanceMult))
                {
                    ChangeStat(t, stat, data.StatStageAmount, user);
                    foreach (var also in data.AlsoChangesStats ?? []) ChangeStat(t, also, data.StatStageAmount, user);
                }
                if (data.ConfuseChancePercent > 0 && Chance(data.ConfuseChancePercent * chanceMult)) Confuse(t, user, announceFailure: false);
                if (data.FlinchChancePercent > 0 && !t.MovedThisTurn && Chance(data.FlinchChancePercent * chanceMult) &&
                    !BattleEffects.Of(t).Any(e => e.BlocksFlinch))
                    t.Flinched = true;
            }

            // The target's ability reacts to the hit (Static, Rough Skin), and berries check its HP
            if (hit.Dealt > 0)
            {
                foreach (var e in BattleEffects.Of(t, includeAbility: !userBreaks).ToList())
                    e.AfterHit(this, t, user, move, hit.Dealt, hit.Damage.IsCritical);
                CheckConditionHooks(t, user);
            }
        }

        // Targets knocked out by the move go down before the attacker's recoil and items
        ResolveFaints();
        if (Result != BattleResult.None) return;

        var landed = hits.Where(h => h.Landed).Select(h => (h.Target, h.Dealt)).ToList();
        int total = landed.Sum(h => h.Dealt);
        bool userStanding = user.IsActive;

        if (userStanding && total > 0)
        {
            // Drain heals the user (or hurts it, against Liquid Ooze)
            if (data.DrainPercent > 0)
            {
                int amount = Math.Max(1, total * data.DrainPercent / 100);
                bool ooze = landed.Any(h => BattleEffects.Of(h.Target).Any(e => e.HurtsDrainers));
                if (ooze) LoseHp(user, amount, $"{user.Name} sucked up the liquid ooze!");
                else RestoreHp(user, amount, $"{landed[0].Target.Name} had its energy drained!");
            }

            // Recoil
            if (data == StruggleData) LoseHp(user, Math.Max(1, user.Pokemon!.MaxHP / 4), $"{user.Name} is hit with recoil!", direct: true);
            else if (data.RecoilPercent > 0 && !userEffects.Any(e => e.PreventsRecoil))
                LoseHp(user, Math.Max(1, total * data.RecoilPercent / 100), $"{user.Name} is hit with recoil!");
        }

        // Changes to the user's own stats (Close Combat)
        if (user.IsActive && landed.Count > 0 && data.TargetStatChange is { } selfStat && data.StatChangeTargetSelf &&
            Chance(data.StatChangeChancePercent * (data.StatChangeChancePercent >= 100 ? 1 : chanceMult)))
        {
            ChangeStat(user, selfStat, data.StatStageAmount, user);
            foreach (var also in data.AlsoChangesStats ?? []) ChangeStat(user, also, data.StatStageAmount, user);
        }

        if (user.IsActive) foreach (var e in userEffects) e.AfterAttacking(this, user, move, landed);
        CheckConditionHooks(user, null);
    }

    private void ApplyStatusMove(Battler user, Move move, List<MoveHit> hits)
    {
        var data = move.Data;
        bool userBreaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);

        foreach (var hit in hits)
        {
            var t = hit.Target;
            if (!t.IsActive) continue;
            if (t != user)
            {
                if (!Hits(user, t, move))
                {
                    Say(hits.Count > 1 || IsDouble ? $"{t.Name} avoided the attack!" : $"{user.Name}'s attack missed!");
                    continue;
                }
                var guards = BattleEffects.Of(t, includeAbility: !userBreaks).ToList();
                if (guards.Any(e => e.AbsorbsMove(this, t, user, move, DamageCalculator.Effectiveness(user, t, move, Rules)))) continue;

                // Thunder Wave is the one status move here that types can be immune to
                if (move.Type == PokemonType.Electric && data.InflictStatus == StatusCondition.Paralyze &&
                    DamageCalculator.Effectiveness(user, t, move, Rules) == 0f)
                {
                    Say($"It doesn't affect {t.Name}...");
                    continue;
                }
            }

            bool did = false;
            if (data.InflictStatus != StatusCondition.None) did |= TryInflictStatus(t, data.InflictStatus, user, announceFailure: true);
            if (data.ConfuseChancePercent > 0) did |= Confuse(t, user, announceFailure: true);
            if (data.TargetStatChange is { } stat)
            {
                var who = data.StatChangeTargetSelf ? user : t;
                did |= ChangeStat(who, stat, data.StatStageAmount, user, announceFailure: true);
                foreach (var also in data.AlsoChangesStats ?? []) did |= ChangeStat(who, also, data.StatStageAmount, user, announceFailure: true);
            }
            if (data.HealPercent > 0)
            {
                var p = t.Pokemon!;
                if (p.CurrentHP >= p.MaxHP) Say($"{t.Name}'s HP is full!");
                else RestoreHp(t, Math.Max(1, p.MaxHP * data.HealPercent / 100), $"{t.Name} regained health!");
                did = true;
            }
            if (!did && data.InflictStatus == StatusCondition.None && data.TargetStatChange == null && data.ConfuseChancePercent == 0)
                Say("But nothing happened!");
        }
    }

    /// <summary>A side effect with this chance in a hundred happens (a roll of 100 below the chance).</summary>
    private bool Chance(int percent) => percent >= 100 || (percent > 0 && rng.Roll(RollKind.SideEffect, 100) < percent);

    /// <summary>Confuses unless already confused or protected (Own Tempo).</summary>
    private bool Confuse(Battler target, Battler? source, bool announceFailure)
    {
        if (!target.IsActive) return false;
        if (target.IsConfused)
        {
            if (announceFailure) Say($"{target.Name} is already confused!");
            return false;
        }
        bool breaks = source != null && source != target && BattleEffects.Of(source).Any(e => e.IgnoresTargetAbility);
        if (BattleEffects.Of(target, includeAbility: !breaks).Any(e => e.BlocksConfusion))
        {
            if (announceFailure) Say($"{target.Name}'s {target.Pokemon!.Ability?.Name} prevents confusion!");
            return false;
        }
        target.ConfusionTurns = 2 + rng.Roll(RollKind.ConfusionTurns, 4);
        Say($"{target.Name} became confused!");
        CheckConditionHooks(target, source);
        return true;
    }

    /// <summary>Lets the Pokémon's ability and item react to a change in its HP or condition (berries, Synchronize).</summary>
    private void CheckConditionHooks(Battler b, Battler? cause)
    {
        if (b.Pokemon == null || b.Pokemon.IsFainted) return;
        foreach (var e in BattleEffects.Of(b).ToList()) e.OnConditionChanged(this, b, cause);
    }
}
