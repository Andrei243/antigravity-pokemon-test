using System;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// Stoutland's Search (plan 23, the rest of the islands; rulings, "The Battle Tree, the power plant, Zygarde's cells and
/// the rest of the islands"): ridden through the Ride Pager, Stoutland sniffs out what lies hidden near the player and
/// stops with its nose to the ground, as Sun and Moon's does, and only on its back is an item buried in the ground dug up
/// (<see cref="HiddenItem.Buried"/>: a hidden item no one else finds). Sun and Moon mark the place with a "!" on the
/// ground; here the player's bubble says it. GPU-free; the field shows the bubble and plays the sound.
/// </summary>
public static class StoutlandSearch
{
    /// <summary>How far Stoutland's nose reaches, in steps of the grid.</summary>
    public const int Reach = 3;

    /// <summary>Whether a ride is Stoutland, whose nose finds hidden things.</summary>
    public static bool Sniffs(string? steed) => string.Equals(steed, "Stoutland", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The nearest thing hidden and not yet found within <see cref="Reach"/> steps of (<paramref name="x"/>,
    /// <paramref name="y"/>), buried or not, or null. Of two at the same distance the first found row by row wins.
    /// </summary>
    public static (int X, int Y)? Nearest(Map map, int x, int y, StoryState story)
    {
        (int X, int Y)? best = null;
        int bestDistance = int.MaxValue;
        for (int dy = -Reach; dy <= Reach; dy++)
            for (int dx = -Reach; dx <= Reach; dx++)
            {
                int distance = Math.Abs(dx) + Math.Abs(dy);
                if (distance > Reach || distance >= bestDistance) continue;
                if (FieldScripts.HiddenAt(map, x + dx, y + dy, story) == null) continue;
                (best, bestDistance) = ((x + dx, y + dy), distance);
            }
        return best;
    }

    /// <summary>Whether a hidden item can be found by someone riding <paramref name="steed"/> (or on foot, null): a buried one only on Stoutland's back.</summary>
    public static bool CanFind(HiddenItem hidden, string? steed) => !hidden.Buried || Sniffs(steed);
}
