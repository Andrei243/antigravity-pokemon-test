using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// Using a move: the checks before it (sleep, freeze, confusion, flinching, paralysis), its targets, accuracy and
// damage, the side effects, and what abilities and items do around it.
public partial class BattleEngine
{
    /// <summary>What a Pokémon does when it has no PP left: hurts the target and a quarter of its own HP.</summary>
    internal static readonly MoveData StruggleData = new()
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
        public readonly List<(string Text, Action? OnShow)> Notes = new();
        public bool Landed => !Missed && !Immune && !Absorbed;
    }

    private void ExecuteMove(Battler user, Move move, Battler? chosenTarget, Action onComplete)
    {
        user.MovedThisTurn = true;
        CheckCanMove(user, move, canMove =>
        {
            if (!canMove) onComplete();
            else UseMove(user, move, chosenTarget, onComplete);
        });
    }

    /// <summary>Sleep, freeze, flinching, confusion and paralysis can each stop a Pokémon from acting.</summary>
    private void CheckCanMove(Battler user, Move move, Action<bool> then)
    {
        var p = user.Pokemon!;
        var effects = BattleEffects.Of(user).ToList();

        void Paralysis()
        {
            if (p.Status == StatusCondition.Paralyze && rng.Next(100) < 25)
            {
                QueueMessage($"{user.Name} is paralyzed! It can't move!", () => then(false));
                return;
            }
            then(true);
        }

        void Confusion()
        {
            if (!user.IsConfused)
            {
                Paralysis();
                return;
            }
            if (--user.ConfusionTurns <= 0)
            {
                QueueMessage($"{user.Name} snapped out of confusion!", Paralysis);
                return;
            }
            QueueMessage($"{user.Name} is confused!", () =>
            {
                if (rng.Next(2) == 0)
                {
                    Paralysis();
                    return;
                }
                // A typeless 40-power hit on itself
                var self = DamageCalculator.Calculate(user, user, move, rng, spread: false, powerOverride: 40);
                int dealt = Math.Min(p.CurrentHP, self.Damage);
                QueueMessage("It hurt itself in its confusion!", () => then(false), onShow: () => After(HitDelay, () =>
                {
                    p.CurrentHP -= dealt;
                    Anim.Hit(user.Side, user.Slot);
                    AudioManager.PlaySound("hit_normal");
                }));
            });
        }

        void Flinch()
        {
            if (!user.Flinched)
            {
                Confusion();
                return;
            }
            user.Flinched = false;
            QueueMessage($"{user.Name} flinched!", () =>
            {
                foreach (var e in effects) e.OnFlinch(this, user);
                RunAnnouncements(() => then(false));
            });
        }

        switch (p.Status)
        {
            case StatusCondition.Sleep:
                p.SleepTurns -= effects.Select(e => e.SleepCountdownRate).DefaultIfEmpty(1).Max();
                if (p.SleepTurns > 0)
                {
                    QueueMessage($"{user.Name} is fast asleep.", () => then(false));
                    return;
                }
                p.Status = StatusCondition.None;
                p.SleepTurns = 0;
                QueueMessage($"{user.Name} woke up!", Flinch);
                return;

            case StatusCondition.Freeze:
                if (move.Data.ThawsUser || rng.Next(100) < 20)
                {
                    p.Status = StatusCondition.None;
                    QueueMessage($"{user.Name} thawed out!", Flinch);
                    return;
                }
                QueueMessage($"{user.Name} is frozen solid!", () => then(false));
                return;
        }
        Flinch();
    }

    /// <summary>Who a move hits: its target type, the chosen target (or a stand-in if that one is gone).</summary>
    private List<Battler> ResolveTargets(Battler user, Move move, Battler? chosen)
    {
        var foes = SlotsOf(user.IsPlayerSide ? BattleSide.Enemy : BattleSide.Player).Where(b => b.IsActive).ToList();
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
                return foes.Count == 0 ? foes : new List<Battler> { foes[rng.Next(foes.Count)] };
            default:
                if (chosen != null && chosen.IsActive && chosen != user) return new List<Battler> { chosen };
                // The chosen foe is gone: the move goes to the other one (an ally that fainted leaves nothing to hit)
                if (chosen != null && chosen.Side == user.Side) return new List<Battler>();
                return foes.Count == 0 ? foes : new List<Battler> { foes[rng.Next(foes.Count)] };
        }
    }

    private void UseMove(Battler user, Move move, Battler? chosenTarget, Action onComplete)
    {
        var targets = ResolveTargets(user, move, chosenTarget);

        // PP: one, plus one for each foe with Pressure it is aimed at
        int pp = 1 + targets.Where(t => t.Side != user.Side).Sum(t => BattleEffects.Of(t).Sum(e => e.ExtraPpUsed));
        move.CurrentPP = Math.Max(0, move.CurrentPP - pp);
        if (BattleEffects.Of(user).Any(e => e.LocksMoveChoice) && user.Pokemon!.Moves.Contains(move)) user.ChoiceLock = move;
        if (user.IsPlayerSide) Evolution.CountMoveUse(user.Pokemon!, move.Name);

        if (targets.Count == 0)
        {
            QueueMessage($"{user.Name} used {move.Name}!", () => QueueMessage("But there was no target...", onComplete));
            return;
        }

        var hits = targets.Select(t => new MoveHit { Target = t }).ToList();
        // A move whose effect isn't in the engine yet (Support None) does nothing rather than a made-up hit
        bool damaging = move.Category != MoveCategory.Status && move.Power > 0 && move.Data.Support != MoveEffectSupport.None;

        // The move resolves as its message appears: the attacker lunges, the effect flies across and the hit lands
        // a moment later. The follow-up messages come once the player dismisses it.
        void PlayAttack()
        {
            Anim.Attack(user.Side, user.Slot, move.Category);
            if (!damaging)
            {
                // A status move plays its effect on each target (on itself for moves like Swords Dance)
                foreach (var hit in hits) Anim.Cue(MoveCue(user, hit.Target, move));
                return;
            }

            bool userBreaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);
            foreach (var hit in hits)
            {
                var t = hit.Target;
                if (!AccuracyCheck(user, t, move))
                {
                    hit.Missed = true;
                    continue;
                }

                float effectiveness = move.Data == StruggleData ? 1f : DamageCalculator.Effectiveness(user, t, move);
                var guards = BattleEffects.Of(t, includeAbility: !userBreaks).ToList();
                hit.Notes.AddRange(Capture(() => hit.Absorbed = guards.Any(e => e.AbsorbsMove(this, t, user, move, effectiveness))));
                if (hit.Absorbed) continue;
                if (effectiveness == 0f)
                {
                    hit.Immune = true;
                    continue;
                }

                hit.Damage = DamageCalculator.Calculate(user, t, move, rng, spread: targets.Count > 1,
                    powerOverride: move.Data == StruggleData ? move.Power : null);
                hit.Dealt = Math.Min(t.Pokemon!.CurrentHP, hit.Damage.Damage);
                if (hit.Dealt >= t.Pokemon.CurrentHP)
                {
                    hit.Notes.AddRange(Capture(() => hit.Endured = guards.Any(e => e.EnduresHit(this, t, hit.Dealt))));
                    if (hit.Endured) hit.Dealt = t.Pokemon.CurrentHP - 1;
                }
            }

            // The move's effect flies to each target and lands with the damage
            foreach (var hit in hits) Anim.Cue(MoveCue(user, hit.Target, move, hit));

            // Apply damage: each target flinches and its HP bar drains
            After(HitDelay, () =>
            {
                foreach (var hit in hits.Where(h => h.Landed && h.Dealt > 0))
                {
                    hit.Target.Pokemon!.CurrentHP -= hit.Dealt;
                    bool strong = hit.Damage.IsCritical || hit.Damage.IsSuperEffective;
                    Anim.Hit(hit.Target.Side, hit.Target.Slot, strong ? 1.8f : 1f);
                }
                if (hits.Any(h => h.Landed && h.Damage.IsSuperEffective)) AudioManager.PlaySound("hit_super");
                else if (hits.Any(h => h.Landed)) AudioManager.PlaySound("hit_normal");
            });
        }

        QueueMessage($"{user.Name} used {move.Name}!", () =>
        {
            if (damaging) AfterDamagingMove(user, move, hits, onComplete);
            else ApplyStatusMove(user, move, hits, onComplete);
        }, onShow: PlayAttack);
    }

    /// <summary>The effect a move plays on one target: what kind of move it is and, for a damaging move, how it landed.</summary>
    private static EffectCue MoveCue(Battler user, Battler target, Move move, MoveHit? hit = null) => new()
    {
        Kind = CueKind.Move,
        Move = move.Name,
        Type = move.Type,
        Category = move.Category,
        FromSide = user.Side,
        FromSlot = user.Slot,
        ToSide = target.Side,
        ToSlot = target.Slot,
        Missed = hit?.Missed ?? false,
        Blocked = hit != null && (hit.Immune || hit.Absorbed),
        Critical = hit is { Landed: true } && hit.Damage.IsCritical,
        SuperEffective = hit is { Landed: true } && hit.Damage.IsSuperEffective
    };

    private bool AccuracyCheck(Battler user, Battler target, Move move)
    {
        if (move.Accuracy <= 0 || target == user) return true;
        var userEffects = BattleEffects.Of(user).ToList();
        var targetEffects = BattleEffects.Of(target, includeAbility: !userEffects.Any(e => e.IgnoresTargetAbility)).ToList();
        if (userEffects.Concat(targetEffects).Any(e => e.MovesNeverMiss)) return true;

        int accStage = targetEffects.Any(e => e.IgnoresOthersStatStages) ? 0 : user.Pokemon!.StatStages.GetValueOrDefault(StatType.Accuracy);
        int evaStage = userEffects.Any(e => e.IgnoresOthersStatStages) ? 0 : target.Pokemon!.StatStages.GetValueOrDefault(StatType.Evasion);
        float chance = move.Accuracy * DamageCalculator.AccuracyStageMultiplier(accStage - evaStage);
        chance *= userEffects.Aggregate(1f, (m, e) => m * e.AccuracyMultiplier(user, move));
        chance *= targetEffects.Aggregate(1f, (m, e) => m * e.EvasionMultiplier(target));
        return rng.Next(100) < chance;
    }

    /// <summary>What each target makes of a damaging move, its side effects, then what the attacker gets out of it.</summary>
    private void AfterDamagingMove(Battler user, Move move, List<MoveHit> hits, Action onComplete)
    {
        var data = move.Data;
        bool several = hits.Count > 1;
        var userEffects = BattleEffects.Of(user).ToList();
        int chanceMult = userEffects.Select(e => e.SideEffectChanceMultiplier).DefaultIfEmpty(1).Max();

        foreach (var hit in hits)
        {
            var t = hit.Target;
            if (hit.Missed)
            {
                Announce(several || IsDouble ? $"{t.Name} avoided the attack!" : $"{user.Name}'s attack missed!");
                continue;
            }
            foreach (var (text, onShow) in hit.Notes.Where(_ => !hit.Endured)) Announce(text, onShow);
            if (hit.Absorbed) continue;
            if (hit.Immune)
            {
                Announce($"It doesn't affect {t.Name}...");
                continue;
            }

            if (hit.Damage.IsCritical) Announce(several ? $"A critical hit on {t.Name}!" : "A critical hit!");
            if (hit.Damage.IsSuperEffective) Announce(several ? $"It's super effective on {t.Name}!" : "It's super effective!");
            if (hit.Damage.IsNotVeryEffective) Announce(several ? $"It's not very effective on {t.Name}..." : "It's not very effective...");
            if (hit.Endured) foreach (var (text, onShow) in hit.Notes) Announce(text, onShow);
            if (hit.Damage.IsCritical) t.TookCriticalHit = true;

            bool standing = t.Pokemon!.CurrentHP > 0;

            // A Fire move thaws a frozen target
            if (standing && move.Type == PokemonType.Fire && t.Pokemon.Status == StatusCondition.Freeze)
            {
                t.Pokemon.Status = StatusCondition.None;
                Announce($"{t.Name} thawed out!");
            }

            // Side effects on the target
            bool shielded = BattleEffects.Of(t, includeAbility: !userEffects.Any(e => e.IgnoresTargetAbility)).Any(e => e.BlocksSideEffects);
            if (standing && !shielded && t != user)
            {
                if (data.InflictStatus != StatusCondition.None && Roll(data.StatusChancePercent * chanceMult))
                    TryInflictStatus(t, data.InflictStatus, user);
                if (data.TargetStatChange is { } stat && !data.StatChangeTargetSelf && Roll(data.StatChangeChancePercent * chanceMult))
                {
                    ChangeStat(t, stat, data.StatStageAmount, user);
                    foreach (var also in data.AlsoChangesStats ?? []) ChangeStat(t, also, data.StatStageAmount, user);
                }
                if (data.ConfuseChancePercent > 0 && Roll(data.ConfuseChancePercent * chanceMult)) Confuse(t, user, announceFailure: false);
                if (data.FlinchChancePercent > 0 && !t.MovedThisTurn && Roll(data.FlinchChancePercent * chanceMult) &&
                    !BattleEffects.Of(t).Any(e => e.BlocksFlinch))
                    t.Flinched = true;
            }

            // The target's ability reacts to the hit (Static, Rough Skin), and berries check its HP
            if (hit.Dealt > 0)
            {
                foreach (var e in BattleEffects.Of(t, includeAbility: !userEffects.Any(e => e.IgnoresTargetAbility)))
                    e.AfterHit(this, t, user, move, hit.Dealt, hit.Damage.IsCritical);
                CheckConditionHooks(t, user);
            }
        }

        RunAnnouncements(() => ResolveFaintsMidMove(() =>
        {
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
            if (userStanding && landed.Count > 0 && data.TargetStatChange is { } selfStat && data.StatChangeTargetSelf &&
                Roll(data.StatChangeChancePercent * (data.StatChangeChancePercent >= 100 ? 1 : chanceMult)))
            {
                ChangeStat(user, selfStat, data.StatStageAmount, user);
                foreach (var also in data.AlsoChangesStats ?? []) ChangeStat(user, also, data.StatStageAmount, user);
            }

            if (userStanding) foreach (var e in userEffects) e.AfterAttacking(this, user, move, landed);
            CheckConditionHooks(user, null);
            RunAnnouncements(onComplete);
        }));
    }

    /// <summary>Faint messages for targets knocked out by a move, before the attacker's recoil and items.</summary>
    private void ResolveFaintsMidMove(Action next) => ResolveFaints(next);

    private void ApplyStatusMove(Battler user, Move move, List<MoveHit> hits, Action onComplete)
    {
        var data = move.Data;
        bool userBreaks = BattleEffects.Of(user).Any(e => e.IgnoresTargetAbility);

        foreach (var hit in hits)
        {
            var t = hit.Target;
            if (!t.IsActive) continue;
            if (t != user)
            {
                if (!AccuracyCheck(user, t, move))
                {
                    Announce(hits.Count > 1 || IsDouble ? $"{t.Name} avoided the attack!" : $"{user.Name}'s attack missed!");
                    continue;
                }
                var guards = BattleEffects.Of(t, includeAbility: !userBreaks).ToList();
                if (guards.Any(e => e.AbsorbsMove(this, t, user, move, DamageCalculator.Effectiveness(user, t, move)))) continue;

                // Thunder Wave is the one status move here that types can be immune to
                if (move.Type == PokemonType.Electric && data.InflictStatus == StatusCondition.Paralyze &&
                    DamageCalculator.Effectiveness(user, t, move) == 0f)
                {
                    Announce($"It doesn't affect {t.Name}...");
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
                if (p.CurrentHP >= p.MaxHP) Announce($"{t.Name}'s HP is full!");
                else RestoreHp(t, Math.Max(1, p.MaxHP * data.HealPercent / 100), $"{t.Name} regained health!");
                did = true;
            }
            if (!did && data.InflictStatus == StatusCondition.None && data.TargetStatChange == null && data.ConfuseChancePercent == 0)
                Announce("But nothing happened!");
        }

        RunAnnouncements(onComplete);
    }

    private bool Roll(int percent) => percent >= 100 || (percent > 0 && rng.Next(100) < percent);

    /// <summary>Confuses for 1–4 turns unless already confused or protected (Own Tempo).</summary>
    private bool Confuse(Battler target, Battler? source, bool announceFailure)
    {
        if (!target.IsActive) return false;
        if (target.IsConfused)
        {
            if (announceFailure) Announce($"{target.Name} is already confused!");
            return false;
        }
        bool breaks = source != null && source != target && BattleEffects.Of(source).Any(e => e.IgnoresTargetAbility);
        if (BattleEffects.Of(target, includeAbility: !breaks).Any(e => e.BlocksConfusion))
        {
            if (announceFailure) Announce($"{target.Name}'s {target.Pokemon!.Ability?.Name} prevents confusion!");
            return false;
        }
        target.ConfusionTurns = rng.Next(2, 6);
        Announce($"{target.Name} became confused!");
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
