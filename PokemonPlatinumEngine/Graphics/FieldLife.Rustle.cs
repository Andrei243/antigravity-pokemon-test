using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The DexNav's rustle (plan 10 · F12; style guide, "Life", "A rustle"): a Pokémon hiding in the tall grass in sight
/// throws up the grass's leaf bits and shows two shiver marks, one either side of its tile, every
/// <see cref="HidingEvery"/> seconds; scared, it bursts out of the grass as a tree that is cut down does. GPU-free; the
/// grass's own parting is the renderer's (<see cref="WorldRenderer.Stirring"/>).
/// </summary>
internal sealed partial class FieldLife
{
    /// <summary>The atlas's two shiver marks (cells of their own, not marks' blocks): the left one and the right.</summary>
    public const int ShiverLeft = 34, ShiverRight = 35;

    /// <summary>How often a rustle stirs, how long its shiver marks show, and how far out from the tile's middle and up they stand, in tiles.</summary>
    public const float HidingEvery = 0.6f, ShiverTime = 0.36f, ShiverOut = 0.5f, ShiverUp = 0.4f;

    private static readonly Color ShiverTint = Rgb(250, 248, 228);

    /// <summary>
    /// A Pokémon the DexNav found stirs in the grass at a tile: two of the grass's leaf bits thrown up, and the two shiver
    /// marks either side of the tile for <see cref="ShiverTime"/>.
    /// </summary>
    public void Hiding(Map map, int x, int y)
    {
        if (!map.InBounds(x, y) || map.IsIndoors) return;
        float cx = x + 0.5f, cz = y + 0.5f, ground = Relief.At(map, cx, cz);
        foreach (float side in new[] { -0.25f, 0.25f })
            pieces.Add(new Piece(Kind.Leaf, new Vector3(cx, ground, cz + 0.2f), Now, side < 0f ? LeafLight : LeafMid, 0, side, 0.7f));
        pieces.Add(new Piece(Kind.Shiver, new Vector3(cx - ShiverOut, ground + ShiverUp, cz + 0.2f), Now, ShiverTint, 0, -1f, 1f));
        pieces.Add(new Piece(Kind.Shiver, new Vector3(cx + ShiverOut, ground + ShiverUp, cz + 0.2f), Now, ShiverTint, 0, 1f, 1f));
    }

    /// <summary>A rustle is scared off: the Pokémon bursts out of the grass in six leaf bits and a puff of dust, as a tree cut down falls.</summary>
    public void Fled(Map map, int x, int y)
    {
        if (map.IsIndoors) return;
        GiveWay(map, x, y, PropType.CutTree);
    }

    /// <summary>A shiver mark at its age: whole for the first half, then a texel further out and fainter; none past its time.</summary>
    private static LifeQuad? ShiverAt(Piece p, float age)
    {
        const float texel = 1f / 32f;
        if (age >= ShiverTime) return null;
        bool second = age >= ShiverTime / 2f;
        float out_ = second ? p.Seed * texel : 0f;
        return new LifeQuad(p.At + new Vector3(out_, 0f, 0f), 0.5f, 0.5f, p.Seed < 0f ? ShiverLeft : ShiverRight, p.Tint with { A = second ? (byte)150 : (byte)255 });
    }
}

internal static partial class LifeArt
{
    /// <summary>
    /// The DexNav's shiver marks (plan 10 · F12): a bracket of three steps bowed away from the tile, a texel thick: 3 texels, 5 a
    /// texel further out, 3, white rimmed in the leaves' dark; the left mark bows left and the right one right.
    /// </summary>
    internal static void PaintShivers(PixelCanvas c)
    {
        var white = Color.White;
        var dark = Rgb(96, 100, 96);
        foreach (var (cell, way) in new[] { (FieldLife.ShiverLeft, -1), (FieldLife.ShiverRight, 1) })
        {
            int ox = FieldLife.Cell * (cell % FieldLife.Columns), oy = FieldLife.Cell * (cell / FieldLife.Columns);
            // Three strokes upright, end to end, the middle one longest and a texel further out: a bracket "(" or ")"
            int column = way < 0 ? 7 : 8;
            for (int s = 0; s < 3; s++)
            {
                int length = s == 1 ? 5 : 3;
                int top = s == 0 ? 3 : s == 1 ? 6 : 11;
                // The outer stroke stands a texel further out than the two short ones at its ends
                int x = s == 1 ? column + way : column;
                for (int k = 0; k < length; k++) c.SetRaw(ox + x, oy + top + k, white);
            }
            Rim(c, ox, oy, FieldLife.Cell, dark);
        }
    }
}
