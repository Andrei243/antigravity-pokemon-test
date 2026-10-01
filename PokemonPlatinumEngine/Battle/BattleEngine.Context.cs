using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// The context abilities and held items work through (IBattleContext): announcements shown one after another,
// HP and stat changes, conditions.
public partial class BattleEngine
{
    private readonly List<(string Text, Action? OnShow)> announcements = new();

    public void Announce(string text, Action? onShow = null) => announcements.Add((text, onShow));

    /// <summary>Shows the pending announcements one by one (more can be added while they show), then runs <paramref name="next"/>.</summary>
    private void RunAnnouncements(Action next)
    {
        if (announcements.Count == 0)
        {
            next();
            return;
        }
        var (text, onShow) = announcements[0];
        announcements.RemoveAt(0);
        QueueMessage(text, () => RunAnnouncements(next), onShow);
    }

    /// <summary>Runs <paramref name="action"/> and takes back the announcements it made, to show them later.</summary>
    private List<(string Text, Action? OnShow)> Capture(Action action)
    {
        int before = announcements.Count;
        action();
        var made = announcements.GetRange(before, announcements.Count - before);
        announcements.RemoveRange(before, made.Count);
        return made;
    }

    public IEnumerable<Battler> ActiveFoes(Battler battler) =>
        SlotsOf(battler.IsPlayerSide ? BattleSide.Enemy : BattleSide.Player).Where(b => b.IsActive);

    public bool ChangeStat(Battler target, StatType stat, int amount, Battler? source, bool announceFailure = false)
    {
        if (!target.IsActive || amount == 0) return false;
        var p = target.Pokemon!;

        // Lowered by someone else: abilities like Clear Body stop it
        if (amount < 0 && source != null && source != target)
        {
            bool breaks = BattleEffects.Of(source).Any(e => e.IgnoresTargetAbility);
            if (!breaks && p.Ability?.Effect is { } guard && guard.BlocksStatDrop(target, stat))
            {
                if (announceFailure) Announce($"{target.Name}'s {p.Ability.Name} prevents its {BattleText.StatName(stat)} from being lowered!");
                return false;
            }
        }

        foreach (var e in BattleEffects.Of(target)) amount = e.ScaleStatChange(amount);
        int current = p.StatStages.GetValueOrDefault(stat);
        int next = Math.Clamp(current + amount, -6, 6);
        if (next == current)
        {
            if (announceFailure) Announce($"{target.Name}'s {BattleText.StatName(stat)} won't go any {(amount > 0 ? "higher" : "lower")}!");
            return false;
        }

        p.StatStages[stat] = next;
        Announce($"{target.Name}'s {BattleText.StatName(stat)} {BattleText.StageChange(next - current)}");
        return true;
    }

    public bool TryInflictStatus(Battler target, StatusCondition status, Battler? source, bool announceFailure = false)
    {
        if (!target.IsActive) return false;
        var p = target.Pokemon!;

        if (p.Status != StatusCondition.None)
        {
            if (announceFailure) Announce(p.Status == status || (status == StatusCondition.Toxic && p.Status == StatusCondition.Poison)
                ? BattleText.AlreadyHas(target.Name, p.Status) : "But it failed!");
            return false;
        }

        bool typeImmune = status switch
        {
            StatusCondition.Burn => target.HasType(PokemonType.Fire),
            StatusCondition.Freeze => target.HasType(PokemonType.Ice),
            StatusCondition.Poison or StatusCondition.Toxic => target.HasType(PokemonType.Poison) || target.HasType(PokemonType.Steel),
            _ => false
        };
        if (typeImmune)
        {
            if (announceFailure) Announce($"It doesn't affect {target.Name}...");
            return false;
        }

        bool breaks = source != null && source != target && BattleEffects.Of(source).Any(e => e.IgnoresTargetAbility);
        if (!breaks && p.Ability?.Effect is { } guard && guard.BlocksStatus(target, status))
        {
            if (announceFailure) Announce($"{target.Name}'s {p.Ability.Name} prevents {BattleText.StatusName(status)}!");
            return false;
        }

        p.Status = status;
        if (status == StatusCondition.Sleep) p.SleepTurns = rng.Next(1, 5);
        if (status == StatusCondition.Toxic) p.ToxicCounter = 0;
        Announce(BattleText.Inflicted(target.Name, status));
        CheckConditionHooks(target, source);
        return true;
    }

    public void RestoreHp(Battler target, int amount, string message)
    {
        Announce(message, () =>
        {
            var p = target.Pokemon;
            if (p == null || p.IsFainted) return;
            p.CurrentHP = Math.Min(p.MaxHP, p.CurrentHP + amount);
            AudioManager.PlaySound("heal");
        });
    }

    public void LoseHp(Battler target, int amount, string message) => LoseHp(target, amount, message, direct: false);

    /// <param name="direct">Damage Magic Guard doesn't stop (Struggle's recoil).</param>
    private void LoseHp(Battler target, int amount, string message, bool direct)
    {
        if (!direct && BattleEffects.Of(target).Any(e => e.PreventsIndirectDamage)) return;
        Announce(message, () =>
        {
            var p = target.Pokemon;
            if (p == null || p.IsFainted) return;
            p.CurrentHP = Math.Max(0, p.CurrentHP - amount);
            Anim.Hit(target.Side, target.Slot);
            AudioManager.PlaySound("hit_normal");
            CheckConditionHooks(target, null);
        });
    }

    public void CureStatus(Battler target, string message)
    {
        var p = target.Pokemon!;
        p.Status = StatusCondition.None;
        p.SleepTurns = 0;
        p.ToxicCounter = 0;
        Announce(message);
    }

    public void ConsumeItem(Battler holder)
    {
        if (holder.Pokemon != null) holder.Pokemon.HeldItem = null;
    }
}
