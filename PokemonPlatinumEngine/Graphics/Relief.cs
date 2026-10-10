using System;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The heights the field draws its ground at (style guide, "Relief"), worked out from the heights the map is
/// walked at: the lowlands at zero, water level with its banks, small steps turned into slopes, and everything
/// else left as the step it is, for a face to fill. No GPU calls: the scene builders, the renderer and the tests
/// all ask here, so what stands on the ground stands where the ground is drawn.
/// </summary>
internal static class Relief
{
    /// <summary>Neighbouring tiles that differ by less than this are joined by a slope; by this or more, by a face.</summary>
    public const float WeldLimit = 0.75f;

    /// <summary>Whether a map has any height to draw at all: without it everything is at zero, as before there were heights.</summary>
    public static bool Has(Map map) => map.HasRelief;

    // A tile's own ground at one of the grid's vertices, before it meets its neighbours.
    //
    // Water is drawn level with its banks. Sinnoh's land lies at whole tiles and its water half a tile under
    // (less where a beach's bed slopes up to the shore), so the surface is drawn at the whole tile above the
    // water's own height. Where a place breaks that habit the water still meets its bank, by the slope below.
    private static float Own(Map map, int tx, int ty, int vx, int vy)
    {
        float h = map.HeightAt(tx, ty, vx - tx, vy - ty);
        if (map.IsDeepWater(tx, ty)) h = MathF.Ceiling(h - 0.01f);
        return h - map.GroundLevel;
    }

    /// <summary>
    /// The drawn height of a tile at the grid vertex (<paramref name="vx"/>, <paramref name="vy"/>), one of its
    /// four corners: its own, raised to any neighbour's round that vertex that is higher by less than
    /// <see cref="WeldLimit"/> (and from there to the next, so three small steps make one slope).
    /// </summary>
    public static float Corner(Map map, int tx, int ty, int vx, int vy)
    {
        if (!map.HasRelief || !map.InBounds(tx, ty)) return 0f;
        float h = Own(map, tx, ty, vx, vy);

        Span<float> others = stackalloc float[3];
        int n = 0;
        for (int oy = vy - 1; oy <= vy; oy++)
            for (int ox = vx - 1; ox <= vx; ox++)
            {
                if ((ox == tx && oy == ty) || !map.InBounds(ox, oy)) continue;
                others[n++] = Own(map, ox, oy, vx, vy);
            }

        // From the lowest neighbour up, so each step taken can lead to the next
        for (int i = 1; i < n; i++)
            for (int j = i; j > 0 && others[j] < others[j - 1]; j--)
                (others[j], others[j - 1]) = (others[j - 1], others[j]);
        for (int i = 0; i < n; i++)
            if (others[i] > h && others[i] - h < WeldLimit) h = others[i];
        return h;
    }

    /// <summary>The drawn heights of a tile's four corners.</summary>
    public static (float NW, float NE, float SW, float SE) Corners(Map map, int tx, int ty) =>
        (Corner(map, tx, ty, tx, ty), Corner(map, tx, ty, tx + 1, ty), Corner(map, tx, ty, tx, ty + 1), Corner(map, tx, ty, tx + 1, ty + 1));

    /// <summary>The drawn height of the ground at a point (x and z in tiles): where a tree, a sign or a pair of feet stands.</summary>
    public static float At(Map map, float x, float z)
    {
        if (!map.HasRelief) return 0f;
        int tx = (int)MathF.Floor(x), ty = (int)MathF.Floor(z);
        if (!map.InBounds(tx, ty)) return 0f;
        var (nw, ne, sw, se) = Corners(map, tx, ty);
        float fx = x - tx, fz = z - ty;
        float north = nw + (ne - nw) * fx, south = sw + (se - sw) * fx;
        return north + (south - north) * fz;
    }

    /// <summary>The drawn height of a bridge's deck over a tile, or null where there is none.</summary>
    public static float? Deck(Map map, int tx, int ty) => map.DeckAt(tx, ty) is { } deck ? deck - map.GroundLevel : null;

    /// <summary>
    /// The drawn height under someone who stands at a point at a height the map knows them to be at: the deck
    /// of a bridge if that is what they are on, the ground otherwise.
    /// </summary>
    public static float Under(Map map, float x, float z, float standingHeight)
    {
        if (!map.HasRelief) return 0f;
        int tx = (int)MathF.Floor(x), ty = (int)MathF.Floor(z);
        if (map.DeckAt(tx, ty) is { } deck && map.SurfaceAt(tx, ty, standingHeight).OnDeck) return deck - map.GroundLevel;
        // On a floor a Gym's puzzle lays (the Pastoria Gym's water), where it is what they stand at
        if (map.Puzzle?.FloorAt(tx, ty, standingHeight) is { } floor && floor > map.HeightAt(tx, ty) + 0.25f && MathF.Abs(floor - standingHeight) < 0.25f)
            return floor - map.GroundLevel;
        return At(map, x, z);
    }

    /// <summary>
    /// About how low and how high the drawn ground of a rectangle of tiles goes, from each tile's middle: for
    /// how far a view has to look past its own edges. Cheap enough to ask every frame.
    /// </summary>
    public static (float Min, float Max) Range(Map map, int x0, int y0, int x1, int y1)
    {
        if (!map.HasRelief) return (0f, 0f);
        float min = 0f, max = 0f;
        for (int y = Math.Max(0, y0); y <= Math.Min(map.Height - 1, y1); y++)
            for (int x = Math.Max(0, x0); x <= Math.Min(map.Width - 1, x1); x++)
            {
                float h = Own(map, x, y, x, y) + (Own(map, x, y, x + 1, y + 1) - Own(map, x, y, x, y)) / 2f;
                if (map.DeckAt(x, y) is { } deck) h = Math.Max(h, deck - map.GroundLevel);
                min = Math.Min(min, h);
                max = Math.Max(max, h);
            }
        return (min, max);
    }
}
