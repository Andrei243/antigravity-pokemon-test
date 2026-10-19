using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>What the Itemfinder found: nothing in range, something under the player's feet, or something a way off.</summary>
public enum ItemfinderAnswer { Nothing, Underfoot, Nearby }

/// <summary>
/// Emerald's Itemfinder (plan 21 · H3; <c>src/itemfinder.c</c>), which the rival gives on Route 110: it looks for
/// hidden items not yet found within the GBA's screen round the player (seven tiles to either side, five up and down,
/// <c>ItemfinderCheckForHiddenItems</c>), takes the nearest, and turns the player to face it, along the way the item lies
/// farther off. GPU-free; the field plays the sound and the turn.
/// </summary>
public static class Itemfinder
{
    /// <summary>How far the search reaches to either side and up and down: the GBA's screen round the player.</summary>
    public const int ReachAcross = 7, ReachUpAndDown = 5;

    /// <summary>
    /// The answer at (<paramref name="x"/>, <paramref name="y"/>), with the way to face for something nearby (null
    /// otherwise). Of two at the same distance the first found row by row wins, as the original's loop has it.
    /// </summary>
    public static (ItemfinderAnswer Answer, Direction? Toward) Search(Map map, int x, int y, StoryState story)
    {
        int bestDx = 0, bestDy = 0, best = int.MaxValue;
        for (int dy = -ReachUpAndDown; dy <= ReachUpAndDown; dy++)
        {
            for (int dx = -ReachAcross; dx <= ReachAcross; dx++)
            {
                if (FieldScripts.HiddenAt(map, x + dx, y + dy, story) == null) continue;
                int distance = Math.Abs(dx) + Math.Abs(dy);
                if (distance >= best) continue;
                (best, bestDx, bestDy) = (distance, dx, dy);
            }
        }
        if (best == int.MaxValue) return (ItemfinderAnswer.Nothing, null);
        if (best == 0) return (ItemfinderAnswer.Underfoot, null);
        Direction toward = Math.Abs(bestDx) >= Math.Abs(bestDy)
            ? (bestDx < 0 ? Direction.Left : Direction.Right)
            : (bestDy < 0 ? Direction.Up : Direction.Down);
        return (ItemfinderAnswer.Nearby, toward);
    }
}
