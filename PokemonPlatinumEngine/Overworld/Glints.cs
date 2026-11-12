using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Glints (plan 38 · EX3; <c>docs/mechanics/rulings.md</c>, "The field beyond the tables"; style guide, "Life", "A hidden
/// item's glint"): a hidden item not yet found within <see cref="Reach"/> tiles of the player glints once every
/// <see cref="Every"/> seconds of the field's clock, behind the options' HIDDEN ITEMS GLINT row (on under the modern rules). It
/// tells nothing a look at the tile wouldn't: the item is found as ever, by looking at it. GPU-free; no input or drawing.
/// </summary>
public static class Glints
{
    /// <summary>How far from the player (in tiles, either way) a hidden item glints.</summary>
    public const int Reach = 3;

    /// <summary>The seconds of the field's clock between one glint and the next.</summary>
    public const float Every = 2f;

    /// <summary>The hidden items not yet found within reach of a tile, nearest first and then by their place.</summary>
    public static List<(int X, int Y)> Near(Map map, int x, int y, StoryState story)
    {
        var found = new List<(int X, int Y)>();
        for (int dy = -Reach; dy <= Reach; dy++)
            for (int dx = -Reach; dx <= Reach; dx++)
            {
                var at = (x + dx, y + dy);
                if (map.HiddenItems.TryGetValue(at, out var item) && !story.Has(item.Flag)) found.Add(at);
            }
        found.Sort((a, b) =>
        {
            int da = Math.Max(Math.Abs(a.X - x), Math.Abs(a.Y - y)), db = Math.Max(Math.Abs(b.X - x), Math.Abs(b.Y - y));
            return da != db ? da.CompareTo(db) : a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.X.CompareTo(b.X);
        });
        return found;
    }

    /// <summary>Whether a beat of the glint's clock falls between two readings of the field's clock (in seconds).</summary>
    public static bool Beats(double before, double now) => Math.Floor(now / Every) > Math.Floor(before / Every);
}
