using System;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Delve;

/// <summary>
/// The items of a delve's bag (plan 44 · SD7; <c>docs/mechanics/rulings.md</c>, "The delve"): food is the game's berries,
/// each worth <see cref="BerryHunger"/> of hunger and its own cure or HP; a Potion and a Super Potion heal; an Escape Rope
/// leaves with the bag; a Revive is used at once as the leader falls (<see cref="DelveRun.LeaderFalls"/>) and never by hand.
/// Anything else found is carried home and does nothing below. No drawing or input.
/// </summary>
public static class DelveItems
{
    /// <summary>The hunger a berry gives back.</summary>
    public const int BerryHunger = 30;

    public static bool IsFood(string item) => item.EndsWith(" Berry", StringComparison.Ordinal);

    /// <summary>The condition a berry cures, or none.</summary>
    public static StatusCondition? Cures(string item) => item switch
    {
        "Pecha Berry" => StatusCondition.Poison,
        "Cheri Berry" => StatusCondition.Paralyze,
        "Chesto Berry" => StatusCondition.Sleep,
        "Rawst Berry" => StatusCondition.Burn,
        "Aspear Berry" => StatusCondition.Freeze,
        _ => null
    };

    /// <summary>The HP an item gives back to a Pokémon (nought for none).</summary>
    public static int HealOf(string item, Pokemon p) => item switch
    {
        "Oran Berry" => 10,
        "Sitrus Berry" => Math.Max(1, p.MaxHP / 4),
        "Potion" => 20,
        "Super Potion" => 60,
        _ => 0
    };

    /// <summary>Whether an item would do anything for a teammate now.</summary>
    public static bool Helps(DelveRun run, DelveMon who, string item)
    {
        if (item == "Escape Rope") return true;
        var p = who.Pokemon;
        if (IsFood(item) && run.Hunger < DelveRun.FullHunger) return true;
        if (HealOf(item, p) > 0 && p.CurrentHP < p.MaxHP) return true;
        if (Cures(item) is { } cure && (p.Status == cure || (cure == StatusCondition.Poison && p.Status == StatusCondition.Toxic))) return true;
        return item == "Persim Berry" && who.ConfusedTurns > 0;
    }

    /// <summary>An item from the bag used on a teammate.</summary>
    internal static void Use(DelveRun run, DelveMon who, string item)
    {
        run.Bag.Remove(item);
        if (item == "Escape Rope")
        {
            run.Escape();
            return;
        }
        var p = who.Pokemon;
        if (IsFood(item))
        {
            int before = run.Hunger;
            run.Hunger = Math.Min(DelveRun.FullHunger, run.Hunger + BerryHunger);
            run.Say(DelveEventKind.Ate, who.Name, what: item, amount: run.Hunger - before);
        }
        int heal = Math.Min(HealOf(item, p), p.MaxHP - p.CurrentHP);
        if (heal > 0)
        {
            p.CurrentHP += heal;
            run.Say(DelveEventKind.Healed, who.Name, what: item, amount: heal);
        }
        if (Cures(item) is { } cure && (p.Status == cure || (cure == StatusCondition.Poison && p.Status == StatusCondition.Toxic)))
        {
            p.Status = StatusCondition.None;
            run.Say(DelveEventKind.Cured, who.Name, what: item);
        }
        if (item == "Persim Berry" && who.ConfusedTurns > 0)
        {
            who.ConfusedTurns = 0;
            run.Say(DelveEventKind.Cured, who.Name, what: item);
        }
    }
}
