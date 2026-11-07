using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What making one machine takes (plan 39 · CR4): the move's type's common material, its rare one for a strong move, and money.</summary>
public sealed record TmCost(PokemonType Type, string TierId, string CommonId, int Common, string? RareId, int Rare, int Money);

/// <summary>What <see cref="TmMaker.Make"/> did: the machine made, or why it was not (and nothing taken).</summary>
public sealed record TmMade(bool Made, string? Why, int MoneySpent)
{
    public static TmMade Refused(string why) => new(false, why, 0);
}

/// <summary>
/// The TM maker (plan 39 · CR4, decision 4; <c>docs/mechanics/rulings.md</c>, "Materials, crafting and cooking"): a TM is
/// used up in every game, so a PC with the crafting kit in the bag makes again any TM or TR the player has ever had in
/// the bag (<see cref="Inventory.MachinesHad"/>), for what its move's type leaves behind and some money by the move's
/// power (<c>Data/recipes.json</c>, <c>machines</c>). An HM is never made, and neither is a machine the player never
/// held. A made machine is no find and no gift: it is exactly the item, so the randomiser (which maps what the world
/// gives) never sees it, and it teaches and is used up as a found one does (<see cref="MoveTeaching"/>). No drawing or input.
/// </summary>
public static class TmMaker
{
    /// <summary>The most of one machine the bag holds; a made one past it is refused.</summary>
    public const int MostInBag = 99;

    /// <summary>A TM or a TR: a machine that is no HM.</summary>
    public static bool Makeable(ItemData item) => MoveTeaching.IsMachine(item) && !MoveTeaching.IsHm(item);

    /// <summary>The machines the player has had, TMs by number and then TRs by number.</summary>
    public static IReadOnlyList<ItemData> Known(Inventory bag) =>
        bag.MachinesHad.Select(name => ItemDatabase.Get(name))
            .OfType<ItemData>()
            .Where(Makeable)
            .OrderBy(item => item.Name.StartsWith("TR", StringComparison.Ordinal) ? 1 : 0)
            .ThenBy(item => Inventory.MachineKey(item).Number)
            .ThenBy(item => item.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>The tier a move's power puts it in.</summary>
    public static TmTier TierOf(MoveData move)
    {
        int power = move.Category == MoveCategory.Status ? 0 : move.Power > 0 ? move.Power : RecipeTable.Machines.VariablePower;
        var tiers = RecipeTable.Tiers;
        return tiers.FirstOrDefault(t => power <= t.MaxPower) ?? tiers[^1];
    }

    /// <summary>What making the machine takes; null for something that is no machine to make.</summary>
    public static TmCost? CostOf(ItemData machine)
    {
        if (!Makeable(machine) || !MoveDatabase.Has(machine.TeachesMove!)) return null;
        var move = MoveDatabase.Get(machine.TeachesMove!);
        var tier = TierOf(move);
        var common = MaterialTable.OfType(move.Type, rare: false);
        var rare = tier.Rare > 0 ? MaterialTable.OfType(move.Type, rare: true) : null;
        if (common == null) return null;
        return new TmCost(move.Type, tier.Id, common.Id, tier.Common, rare?.Id, rare == null ? 0 : tier.Rare, tier.Money);
    }

    /// <summary>
    /// Why the machine can't be made now, in words, or null when it can: never had in the bag, an HM, a material short,
    /// money short, or the bag holding all it can.
    /// </summary>
    public static string? WhyNot(ItemData machine, Inventory bag, MaterialPouch pouch, int money)
    {
        if (MoveTeaching.IsHm(machine)) return Text.Of("crafting.tm.why.hm");
        if (!Makeable(machine) || !bag.MachinesHad.Contains(machine.Name)) return Text.Of("crafting.tm.why.unknown");
        if (CostOf(machine) is not { } cost) return Text.Of("crafting.tm.why.unknown");
        if (!pouch.Has(cost.CommonId, cost.Common)) return Text.Of("crafting.tm.why.short", ("name", MaterialTable.NameOf(cost.CommonId)));
        if (cost.RareId != null && !pouch.Has(cost.RareId, cost.Rare)) return Text.Of("crafting.tm.why.short", ("name", MaterialTable.NameOf(cost.RareId)));
        if (money < cost.Money) return Text.Of("crafting.tm.why.money");
        if (bag.GetQuantity(machine) >= MostInBag) return Text.Of("crafting.tm.why.full");
        return null;
    }

    /// <summary>
    /// Makes the machine: takes the materials from the pouch and puts one machine in the bag, and says what money it
    /// costs (the caller takes it: the screen hands it to the engine). Nothing changes when it can't be made.
    /// </summary>
    public static TmMade Make(ItemData machine, Inventory bag, MaterialPouch pouch, int money)
    {
        if (WhyNot(machine, bag, pouch, money) is { } why) return TmMade.Refused(why);
        var cost = CostOf(machine)!;
        pouch.Take(cost.CommonId, cost.Common);
        if (cost.RareId != null) pouch.Take(cost.RareId, cost.Rare);
        bag.AddItem(machine, 1);
        return new TmMade(true, null, cost.Money);
    }
}
