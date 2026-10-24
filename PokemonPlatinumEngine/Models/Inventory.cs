using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>How a bag pocket is kept (plan 12 · Q13): the order things were taken in, by name (a TM pocket by number), or the most first.</summary>
public enum BagOrder { Taken, Name, Quantity }

public class Inventory
{
    private readonly List<ItemStack> items = new();
    private readonly Dictionary<ItemPocket, BagOrder> orders = new();

    public IReadOnlyList<ItemStack> AllItems => items;

    /// <summary>The order a pocket is kept in; <see cref="BagOrder.Taken"/> unless chosen.</summary>
    public BagOrder OrderOf(ItemPocket pocket) => orders.TryGetValue(pocket, out var o) ? o : BagOrder.Taken;

    public void SetOrder(ItemPocket pocket, BagOrder order)
    {
        if (order == BagOrder.Taken) orders.Remove(pocket);
        else orders[pocket] = order;
    }

    /// <summary>The pockets with an order chosen, for the save.</summary>
    public IReadOnlyDictionary<ItemPocket, BagOrder> Orders => orders;

    /// <summary>The next order a pocket turns to: as taken, A to Z, most first, and round.</summary>
    public BagOrder NextOrder(ItemPocket pocket) => (BagOrder)(((int)OrderOf(pocket) + 1) % 3);

    /// <summary>The key a TM or an HM is ordered by: its number, the TMs before the HMs, as in the games.</summary>
    internal static (int Kind, int Number, string Name) MachineKey(ItemData item)
    {
        string digits = new(item.Name.Where(char.IsDigit).ToArray());
        int number = int.TryParse(digits, out var n) ? n : int.MaxValue;
        return (item.Name.StartsWith("HM", System.StringComparison.OrdinalIgnoreCase) ? 1 : 0, number, item.Name);
    }

    public List<ItemStack> GetPocketItems(ItemPocket pocket)
    {
        var list = items.Where(i => i.Pocket == pocket);
        return OrderOf(pocket) switch
        {
            BagOrder.Name when pocket == ItemPocket.TMsAndHMs => list.OrderBy(i => MachineKey(i.Data)).ToList(),
            BagOrder.Name => list.OrderBy(i => i.Data.DisplayName, System.StringComparer.CurrentCulture).ThenBy(i => i.Data.Name, System.StringComparer.Ordinal).ToList(),
            BagOrder.Quantity => list.OrderByDescending(i => i.Quantity).ThenBy(i => i.Data.DisplayName, System.StringComparer.CurrentCulture).ThenBy(i => i.Data.Name, System.StringComparer.Ordinal).ToList(),
            _ => list.ToList()
        };
    }

    public void AddItem(ItemData item, int quantity = 1)
    {
        if (quantity <= 0) return;
        var existing = items.FirstOrDefault(i => i.Data.Id == item.Id);
        if (existing != null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            items.Add(new ItemStack(item, quantity));
        }
    }

    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        var existing = items.FirstOrDefault(i => i.Data.Id == item.Id);
        if (existing == null || existing.Quantity < quantity) return false;

        existing.Quantity -= quantity;
        if (existing.Quantity <= 0)
        {
            items.Remove(existing);
        }
        return true;
    }

    public int GetQuantity(ItemData item)
    {
        var existing = items.FirstOrDefault(i => i.Data.Id == item.Id);
        return existing?.Quantity ?? 0;
    }

    public void Clear()
    {
        items.Clear();
        orders.Clear();
    }
}
