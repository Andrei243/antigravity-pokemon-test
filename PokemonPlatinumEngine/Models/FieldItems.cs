using System;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What the bag's medicine does to a Pokémon in the field: the kinds of effect the item data marks today
/// (restoring HP, reviving, curing a status, a Full Restore). No drawing or input. Plan 06 · R11 gives every
/// item its use; the battle has its own rules for using items.
/// </summary>
public static class FieldItems
{
    /// <summary>Whether the item is medicine that is used on one Pokémon.</summary>
    public static bool IsMedicine(ItemData item) => item.EffectType is ItemEffectType.HealHP or ItemEffectType.Revive
        or ItemEffectType.HealStatus or ItemEffectType.FullRestore;

    private static bool Cures(ItemData item, StatusCondition has) =>
        has is not (StatusCondition.None or StatusCondition.Faint)
        && (item.HealsStatus == StatusCondition.None || item.HealsStatus == has
            || (item.HealsStatus == StatusCondition.Poison && has == StatusCondition.Toxic));

    /// <summary>Whether using it on this Pokémon now would do anything: the games' ABLE and NOT ABLE.</summary>
    public static bool WouldHelp(ItemData item, Pokemon p) => item.EffectType switch
    {
        ItemEffectType.HealHP => !p.IsFainted && p.CurrentHP < p.MaxHP,
        ItemEffectType.Revive => p.IsFainted,
        ItemEffectType.HealStatus => !p.IsFainted && Cures(item, p.Status),
        ItemEffectType.FullRestore => !p.IsFainted && (p.CurrentHP < p.MaxHP || p.Status != StatusCondition.None),
        _ => false
    };

    /// <summary>
    /// Uses it on the Pokémon and says what happened, or returns null (and changes nothing) if it would have
    /// no effect. A Revive's value is the share of its HP a Pokémon comes round with, in hundredths.
    /// </summary>
    public static string? Use(ItemData item, Pokemon p)
    {
        if (!WouldHelp(item, p)) return null;
        switch (item.EffectType)
        {
            case ItemEffectType.HealHP:
                int before = p.CurrentHP;
                p.CurrentHP = Math.Min(p.MaxHP, p.CurrentHP + item.EffectValue);
                return $"{p.DisplayName} recovered {p.CurrentHP - before} HP.";
            case ItemEffectType.Revive:
                p.Revive(Math.Max(1, p.MaxHP * Math.Clamp(item.EffectValue, 1, 100) / 100));
                return $"{p.DisplayName} came round.";
            case ItemEffectType.HealStatus:
                p.Status = StatusCondition.None;
                p.SleepTurns = 0;
                p.ToxicCounter = 0;
                return $"{p.DisplayName} is healthy again.";
            default:
                p.CurrentHP = p.MaxHP;
                p.Status = StatusCondition.None;
                p.SleepTurns = 0;
                p.ToxicCounter = 0;
                return $"{p.DisplayName} is fully restored.";
        }
    }
}
