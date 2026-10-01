using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>Every item, read from <c>Data/items.json</c> the first time it is used.</summary>
public static class ItemDatabase
{
    public const string FileName = "items.json";

    private static readonly Dictionary<string, ItemData> Items = new(StringComparer.OrdinalIgnoreCase);

    public static void Initialize() { }

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
    }

    public static ItemData? Get(string name)
    {
        Items.TryGetValue(name, out var item);
        return item;
    }

    public static IEnumerable<ItemData> GetAll() => Items.Values;
}
