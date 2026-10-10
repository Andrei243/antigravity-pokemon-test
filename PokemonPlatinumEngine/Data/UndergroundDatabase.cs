using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>The five kinds of Sphere (<c>generated/sphere_types.txt</c>), in the original's order; 0 is none.</summary>
public enum SphereKind
{
    None,
    Prism,
    Pale,
    Red,
    Blue,
    Green
}

/// <summary>
/// What a hiker asks for something (<c>SpherePrice</c>): a kind of Sphere, or any kind the day draws
/// (<c>RANDOM_SPHERE_TYPE</c>), and the least and most its size is drawn between. No kind at all is not for trade.
/// </summary>
public sealed class SpherePriceRecord
{
    public SphereKind Kind { get; set; }
    public bool Any { get; set; }
    public int Min { get; set; }
    public int Max { get; set; }

    public bool ForTrade => Any || Kind != SphereKind.None;
}

/// <summary>
/// Something a wall can hold (<c>sMiningObjects</c> in <c>src/underground/mining.c</c>): a Sphere, a treasure or a
/// piece of dark rock, with its size in the wall's squares, its shape where it isn't the whole rectangle (rows of
/// <c>x</c> and <c>o</c>, as the original's lookup reads them), and how likely it is for a trainer whose ID number is
/// odd or even, before and after the National Pokédex.
/// </summary>
public sealed class MiningObjectRecord
{
    /// <summary>The original's constant without <c>MINING_</c>: <c>SMALL_PRISM_SPHERE</c>, <c>TREASURE_HEART_SCALE</c>, <c>ROCK_3</c>.</summary>
    public string Id { get; set; } = "";
    public int Width { get; set; }
    public int Height { get; set; }
    public List<string>? Shape { get; set; }
    public int Odd { get; set; }
    public int Even { get; set; }
    public int OddNational { get; set; }
    public int EvenNational { get; set; }
}

/// <summary>
/// One of the things the wall's game gives (<c>enum MiningObjectID</c> up to <c>MINING_TREASURE_MAX</c>): the ten
/// Spheres by size and the forty-nine treasures, with the bag's item a treasure becomes above ground
/// (<c>sMiningItems</c>) and what a hiker gives for it (<c>sTreasurePrices</c>).
/// </summary>
public sealed class TreasureRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Item { get; set; }
    public SpherePriceRecord Price { get; set; } = new();
}

/// <summary>A trap (<c>generated/traps.txt</c>, <c>sTrapPrices</c>); one with no price is a dummy nobody can have.</summary>
public sealed class TrapRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public SpherePriceRecord Price { get; set; } = new();
}

/// <summary>
/// One of the base's goods (<c>sGoodData</c> in <c>src/goods.c</c>): how many tiles it takes, which of them can't
/// be walked (nine numbers, three to a row), what a hiker asks, what the Department Store asks, what it shows of the
/// records when read, whether it waits for the National Pokédex and whether it can't be given away.
/// </summary>
public sealed class GoodRecord
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Width { get; set; }
    public int Depth { get; set; }
    public string Collision { get; set; } = "000000000";
    public SpherePriceRecord Price { get; set; } = new();
    public int Money { get; set; }
    public string? Record { get; set; }
    public bool National { get; set; }
    public bool Ungiftable { get; set; }
}

/// <summary>One of the twenty hikers (<c>sUndergroundVendors</c>): what he trades in and the tile he stands on.</summary>
public sealed class VendorRecord
{
    public string Kind { get; set; } = "";
    public int X { get; set; }
    public int Z { get; set; }
}

/// <summary>What <c>underground.json</c> holds.</summary>
public sealed class UndergroundData
{
    public List<MiningObjectRecord> Objects { get; set; } = new();

    /// <summary>The Spheres and treasures in the original's order: the first is thing 1.</summary>
    public List<TreasureRecord> Treasures { get; set; } = new();

    /// <summary>The traps in the original's order: the first is trap 1.</summary>
    public List<TrapRecord> Traps { get; set; } = new();

    /// <summary>The goods in the original's order: the first is good 1.</summary>
    public List<GoodRecord> Goods { get; set; } = new();
    public List<VendorRecord> Vendors { get; set; } = new();
}

/// <summary>
/// The Underground's tables (plan 06 · R16a), imported by <c>tools/DataImporter</c> (<c>Importer.Underground.cs</c>):
/// names and numbers only. A treasure, a trap and a good are numbers, as the original's are (0 is none), and a save
/// keeps the numbers.
/// </summary>
public static class UndergroundDatabase
{
    public const string FileName = "underground.json";

    private static readonly UndergroundData Data = GameDataFiles.Load<UndergroundData>(FileName);
    private static readonly Dictionary<string, int> TreasureKeys = Keys(Data.Treasures.Select(t => (t.Id, t.Name)));
    private static readonly Dictionary<string, int> TrapKeys = Keys(Data.Traps.Select(t => (t.Id, t.Name)));
    private static readonly Dictionary<string, int> GoodKeys = Keys(Data.Goods.Select(g => (g.Id, g.Name)));

    private static Dictionary<string, int> Keys(IEnumerable<(string Id, string Name)> rows)
    {
        var keys = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int number = 0;
        foreach (var (id, name) in rows)
        {
            number++;
            keys[id] = number;
            // Several goods share a name (the PCs); the first keeps it
            keys.TryAdd(name, number);
        }
        return keys;
    }

    public static IReadOnlyList<MiningObjectRecord> Objects => Data.Objects;
    public static IReadOnlyList<VendorRecord> Vendors => Data.Vendors;

    // ------------------------------------------------------------------ Spheres and treasures

    /// <summary>The first thing that is no Sphere (<c>MINING_SPHERES_MAX</c>): 11.</summary>
    public const int FirstTreasure = 11;

    /// <summary>One past the last treasure (<c>MINING_TREASURE_MAX</c>): the dark rocks' numbers start here.</summary>
    public static int TreasureEnd => Data.Treasures.Count + 1;

    public static TreasureRecord? Treasure(int id) => id >= 1 && id <= Data.Treasures.Count ? Data.Treasures[id - 1] : null;
    public static int TreasureId(string? idOrName) => idOrName != null && TreasureKeys.TryGetValue(idOrName, out int id) ? id : 0;

    /// <summary><c>Spheres_IsMiningItemSphere</c>.</summary>
    public static bool IsSphere(int id) => id >= 1 && id < FirstTreasure;

    /// <summary>The kind of the Sphere a thing of the wall is: the five small ones, then the five large.</summary>
    public static SphereKind SphereOf(int id) => IsSphere(id) ? (SphereKind)((id - 1) % 5 + 1) : SphereKind.None;

    /// <summary>Whether a thing of the wall is one of the five large Spheres.</summary>
    public static bool IsLargeSphere(int id) => id is >= 6 and < FirstTreasure;

    public static bool IsPlate(int id) => Treasure(id)?.Id.EndsWith("_PLATE", StringComparison.Ordinal) ?? false;

    public static string SphereName(SphereKind kind) => kind == SphereKind.None ? "" : $"{kind} Sphere";

    public static string TreasureName(int id) => Treasure(id)?.Name ?? "";

    // ------------------------------------------------------------------ traps

    public static int TrapCount => Data.Traps.Count;
    public static TrapRecord? Trap(int id) => id >= 1 && id <= Data.Traps.Count ? Data.Traps[id - 1] : null;
    public static int TrapId(string? idOrName) => idOrName != null && TrapKeys.TryGetValue(idOrName, out int id) ? id : 0;
    public static string TrapName(int id) => Trap(id)?.Name ?? "";

    // ------------------------------------------------------------------ goods

    public static int GoodCount => Data.Goods.Count;
    public static GoodRecord? Good(int id) => id >= 1 && id <= Data.Goods.Count ? Data.Goods[id - 1] : null;
    public static int GoodId(string? idOrName) => idOrName != null && GoodKeys.TryGetValue(idOrName, out int id) ? id : 0;
    public static string GoodName(int id) => Good(id)?.Name ?? "";
}
