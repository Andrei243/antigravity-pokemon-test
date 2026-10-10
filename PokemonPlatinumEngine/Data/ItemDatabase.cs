using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>Every item, read from <c>Data/items.json</c> the first time it is used.</summary>
public static class ItemDatabase
{
    public const string FileName = "items.json";

    private static readonly Dictionary<string, ItemData> Items = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<int, ItemData> ByNumber = new();

    public static void Initialize() { }

    /// <summary>
    /// An item by its number (Platinum's own for its items): what the original's scripts hand about, the item a bag
    /// opened for a script picked (plan 06 · R14a). Null for a number no item has.
    /// </summary>
    public static ItemData? ById(int id) => ByNumber.GetValueOrDefault(id);

    static ItemDatabase()
    {
        foreach (var item in GameDataFiles.Load<List<ItemData>>(FileName))
        {
            Register(item);
        }
    }

    private static void Register(ItemData item)
    {
        Items[item.Name] = item;
        if (item.Id > 0) ByNumber.TryAdd(item.Id, item);
    }

    public static ItemData? Get(string name)
    {
        Items.TryGetValue(name, out var item);
        return item;
    }

    public static IEnumerable<ItemData> GetAll() => Items.Values;
}
