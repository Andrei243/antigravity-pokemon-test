using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

public class Inventory
{
    private readonly List<ItemStack> items = new();

    public IReadOnlyList<ItemStack> AllItems => items;

    public List<ItemStack> GetPocketItems(ItemPocket pocket)
    {
        return items.Where(i => i.Pocket == pocket).ToList();
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
    }
}
