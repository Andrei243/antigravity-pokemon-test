using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

/// <summary>
/// The bag as a battle opens it (plan 06 · R11): Platinum's four battle pockets, filled by each item's battle
/// pocket (<see cref="ItemData.BattlePocket"/>, the original's mask: an item restoring HP and a condition is in
/// both of the first two), and the item used last. An item that heals or restores goes on to the Pokémon it is for,
/// and one that restores one move's PP to that move. No drawing or input.
/// </summary>
public static class BattleBag
{
    /// <summary>The pockets in their order, and the fifth button: the item used last.</summary>
    public static readonly string[] Pockets = { "HP/PP RESTORE", "STATUS RESTORE", "POKé BALLS", "BATTLE ITEMS" };

    public const int LastUsed = 4;

    /// <summary>Whether the item shows in that pocket.</summary>
    public static bool InPocket(ItemData item, int pocket) => pocket switch
    {
        0 => item.BattlePocket is "RecoverHp" or "RecoverPp" or "RecoverHpStatus",
        1 => item.BattlePocket is "RecoverStatus" or "RecoverHpStatus",
        2 => item.BattlePocket == "PokeBalls",
        3 => item.BattlePocket == "BattleItems",
        _ => false
    };

    /// <summary>The bag's items in a pocket, in the bag's own order.</summary>
    public static List<ItemStack> Items(Inventory bag, int pocket) =>
        bag.AllItems.Where(s => s.Quantity > 0 && InPocket(s.Data, pocket)).ToList();

    /// <summary>Whether it is used on a Pokémon of the player's choosing: medicine and berries (the first two pockets).</summary>
    public static bool ForAPartyMember(ItemData item) => item.BattlePocket is "RecoverHp" or "RecoverPp" or "RecoverStatus" or "RecoverHpStatus";

    /// <summary>Whether it then needs one of that Pokémon's moves: an Ether, a Leppa Berry.</summary>
    public static bool ForAMove(ItemData item) => ForAPartyMember(item) && ItemUse.NeedsMove(item);
}
