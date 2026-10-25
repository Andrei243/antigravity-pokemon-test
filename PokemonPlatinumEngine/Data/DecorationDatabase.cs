using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Data;

/// <summary>What a decoration of a secret base is (Emerald's <c>DECORCAT_*</c>): the eight pockets of the decoration PC.</summary>
public enum DecorationCategory { Desk, Chair, Plant, Ornament, Mat, Poster, Doll, Cushion }

/// <summary>
/// How a decoration stands (Emerald's <c>DECORPERM_*</c>): on the floor and in the way, on the floor and walked over, on
/// the floor with its top row free to stand before the wall, on the wall, or a figure on one tile.
/// </summary>
public enum DecorationPlacement { Solid, Mat, Tall, Wall, Doll }

/// <summary>One decoration of <c>decorations.json</c> (plan 21 · H10).</summary>
public sealed class DecorationRecord
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public DecorationCategory Category { get; set; }
    public DecorationPlacement Placement { get; set; }
    public int Width { get; set; } = 1;
    public int Depth { get; set; } = 1;
    public int Price { get; set; }

    /// <summary>
    /// The tiles it takes on the floor, east and south from its corner: its shape, but a doll's or a cushion's one tile
    /// whatever its figure's height (Emerald draws a big doll two tiles tall on the one tile it stands on).
    /// </summary>
    public (int Width, int Depth) Footprint => Placement == DecorationPlacement.Doll ? (Width, 1) : (Width, Depth);

    /// <summary>Whether whoever walks into it is stopped: everything but a mat (and what Emerald lets be walked over: a chair, a balloon).</summary>
    public bool Blocks => Placement != DecorationPlacement.Mat;
}

/// <summary>What <c>decorations.json</c> holds.</summary>
public sealed class DecorationData
{
    public List<DecorationRecord> Decorations { get; set; } = new();
}

/// <summary>
/// Emerald's hundred and twenty decorations for a secret base (plan 21 · H10): numbers, names, kinds, shapes and prices,
/// written from Emerald's table (<c>src/data/decoration/header.h</c>); nothing of its art. A decoration is its number,
/// as Emerald's <c>DECOR_*</c> (0 is none); a script and a save name it by its name or its constant.
/// </summary>
public static class DecorationDatabase
{
    public const string FileName = "decorations.json";

    private static readonly DecorationData Data = GameDataFiles.Load<DecorationData>(FileName);
    private static readonly Dictionary<int, DecorationRecord> ById = Data.Decorations.ToDictionary(d => d.Id);
    private static readonly Dictionary<string, DecorationRecord> ByName = Names();

    private static Dictionary<string, DecorationRecord> Names()
    {
        var names = new Dictionary<string, DecorationRecord>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in Data.Decorations)
        {
            names[d.Key] = d;
            names[d.Name] = d;
        }
        return names;
    }

    public static IReadOnlyList<DecorationRecord> All => Data.Decorations;

    public static DecorationRecord? Get(int id) => ById.GetValueOrDefault(id);

    /// <summary>A decoration by its name (<c>Red Brick</c>) or its constant (<c>DECOR_RED_BRICK</c>); null for neither.</summary>
    public static DecorationRecord? Get(string name) => ByName.GetValueOrDefault(name);

    /// <summary>The number of a decoration by its name or constant; 0 for none.</summary>
    public static int IdOf(string name) => Get(name)?.Id ?? 0;

    /// <summary>
    /// How many decorations of a kind the decoration PC holds (Emerald's save: ten desks, chairs, plants, posters and
    /// cushions, thirty ornaments and mats, forty dolls).
    /// </summary>
    public static int Room(DecorationCategory category) => category switch
    {
        DecorationCategory.Ornament or DecorationCategory.Mat => 30,
        DecorationCategory.Doll => 40,
        _ => 10
    };
}
