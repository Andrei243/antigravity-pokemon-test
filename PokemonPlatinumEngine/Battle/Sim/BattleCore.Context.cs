using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

// What abilities, held items and moves change a battle through (IBattleContext): HP, stat stages, conditions. Each
// change is made at once and written into the log with the line that tells of it.
public sealed partial class BattleCore
{
    public void Announce(string text) => Say(text);

    public IEnumerable<Battler> ActiveFoes(Battler battler) => SlotsOf(Other(battler.Side)).Where(b => b.IsActive);

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
                if (announceFailure) Say($"{target.Name}'s {p.Ability.Name} prevents its {BattleText.StatName(stat)} from being lowered!");
                return false;
            }
        }

        foreach (var e in BattleEffects.Of(target)) amount = e.ScaleStatChange(amount);
        int current = p.StatStages.GetValueOrDefault(stat);
        int next = Math.Clamp(current + amount, -6, 6);
        if (next == current)
        {
            if (announceFailure) Say($"{target.Name}'s {BattleText.StatName(stat)} won't go any {(amount > 0 ? "higher" : "lower")}!");
            return false;
        }

        p.StatStages[stat] = next;
        Say($"{target.Name}'s {BattleText.StatName(stat)} {BattleText.StageChange(next - current)}")
            .With(new StageChanged(target.Place, stat, next, next > current));
        return true;
    }

    public bool TryInflictStatus(Battler target, StatusCondition status, Battler? source, bool announceFailure = false)
    {
        if (!target.IsActive) return false;
        var p = target.Pokemon!;

        if (p.Status != StatusCondition.None)
        {
            if (announceFailure) Say(p.Status == status || (status == StatusCondition.Toxic && p.Status == StatusCondition.Poison)
                ? BattleText.AlreadyHas(target.Name, p.Status) : "But it failed!");
            return false;
        }

        bool typeImmune = status switch
        {
            StatusCondition.Burn => target.HasType(PokemonType.Fire),
            StatusCondition.Freeze => target.HasType(PokemonType.Ice),
            StatusCondition.Poison or StatusCondition.Toxic => target.HasType(PokemonType.Poison) || target.HasType(PokemonType.Steel),
            StatusCondition.Paralyze => Rules.ElectricTypesCantBeParalyzed && target.HasType(PokemonType.Electric),
            _ => false
        };
        if (typeImmune)
        {
            if (announceFailure) Say($"It doesn't affect {target.Name}...");
            return false;
        }

        bool breaks = source != null && source != target && BattleEffects.Of(source).Any(e => e.IgnoresTargetAbility);
        if (!breaks && p.Ability?.Effect is { } guard && guard.BlocksStatus(target, status))
        {
            if (announceFailure) Say($"{target.Name}'s {p.Ability.Name} prevents {BattleText.StatusName(status)}!");
            return false;
        }

        p.Status = status;
        if (status == StatusCondition.Sleep) p.SleepTurns = 1 + rng.Roll(RollKind.SleepTurns, Rules.SleepLengths);
        if (status == StatusCondition.Toxic) p.ToxicCounter = 0;
        Say(BattleText.Inflicted(target.Name, status)).With(new StatusChanged(target.Place, status));
        CheckConditionHooks(target, source);
        return true;
    }

    public void RestoreHp(Battler target, int amount, string message)
    {
        if (!target.IsActive) return;
        var p = target.Pokemon!;
        p.CurrentHP = Math.Min(p.MaxHP, p.CurrentHP + amount);
        Say(message).With(new HpChanged(target.Place, p.CurrentHP, Healed: true));
    }

    public void LoseHp(Battler target, int amount, string message) => LoseHp(target, amount, message, direct: false);

    /// <param name="direct">Damage Magic Guard doesn't stop (Struggle's recoil).</param>
    private void LoseHp(Battler target, int amount, string message, bool direct)
    {
        if (!target.IsActive) return;
        if (!direct && BattleEffects.Of(target).Any(e => e.PreventsIndirectDamage)) return;
        var p = target.Pokemon!;
        p.CurrentHP = Math.Max(0, p.CurrentHP - amount);
        Say(message).With(new HpChanged(target.Place, p.CurrentHP, Healed: false));
        CheckConditionHooks(target, null);
    }

    public void CureStatus(Battler target, string message)
    {
        var p = target.Pokemon!;
        p.Status = StatusCondition.None;
        p.SleepTurns = 0;
        p.ToxicCounter = 0;
        Say(message).With(new StatusChanged(target.Place, StatusCondition.None));
    }

    public void ConsumeItem(Battler holder)
    {
        if (holder.Pokemon != null) holder.Pokemon.HeldItem = null;
    }
}
