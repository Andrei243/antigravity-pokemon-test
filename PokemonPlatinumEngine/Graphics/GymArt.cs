using System;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Pixel art for the things of the Gyms' puzzles that stand in the way like people (plan 01 · M9; style guide,
/// "Gyms"), each drawn as a card as the field's obstacles are: the Veilstone Gym's punching bags and stacks of tyres,
/// and the Hearthome Gym's bollards. Outlined like the field's other sprites. No GPU calls.
/// </summary>
internal static class GymArt
{
    private static Color Rgb(int r, int g, int b) => new(r, g, b, 255);

    /// <summary>A punching bag of red leather hanging from its chain, banded at top and foot, 24 by 44.</summary>
    public static void PaintPunchingBag(PixelCanvas c)
    {
        var leather = Tone.Of(206, 62, 58, 234, 104, 92, 150, 38, 48);
        var band = Tone.Of(250, 246, 238, 255, 255, 255, 196, 192, 196);
        var chain = Rgb(150, 154, 170);
        // The chain runs up out of the top of the card
        for (int y = 0; y < 8; y++) c.Set(12, y, y % 2 == 0 ? chain : Rgb(96, 100, 118));
        c.Rect(9, 7, 7, 2, Rgb(80, 84, 100));
        for (int y = 9; y < 42; y++)
        {
            // A bag rounded at its top and foot, a little fuller in the middle
            int half = y < 12 ? 6 + (y - 9) : y > 38 ? 9 - (y - 38) : 9;
            for (int x = 12 - half; x < 12 + half; x++)
            {
                float u = (x + 0.5f - 12f) / half;
                var tone = y is >= 13 and <= 15 || y is >= 34 and <= 36 ? band : leather;
                c.Set(x, y, u < -0.45f ? tone.Light : u > 0.5f ? tone.Dark : tone.Base);
            }
        }
        // Stitches down the middle and a glint on the leather
        for (int y = 17; y < 33; y += 3) c.Set(12, y, leather.Dark);
        c.Rect(6, 18, 2, 8, Rgb(246, 156, 140));
        Pix.Outline(c);
    }

    /// <summary>Three tyres stacked one on another, 30 by 30.</summary>
    public static void PaintTireStack(PixelCanvas c)
    {
        var rubber = Tone.Of(58, 58, 72, 96, 98, 116, 34, 34, 46);
        for (int tyre = 0; tyre < 3; tyre++)
        {
            int top = 20 - tyre * 8;
            for (int y = top; y < top + 9; y++)
                for (int x = 2; x < 28; x++)
                {
                    float u = (x + 0.5f - 15f) / 13f, v = (y + 0.5f - (top + 4.5f)) / 4.5f;
                    if (u * u + v * v * 0.35f > 1f) continue;
                    var col = v < -0.5f ? rubber.Light : v > 0.5f ? rubber.Dark : rubber.Base;
                    // The tread: a dark notch every four texels across its face
                    if (MathF.Abs(v) < 0.5f && x % 4 == 0) col = rubber.Dark;
                    c.Set(x, y, col);
                }
        }
        // The hole of the top tyre, seen from above
        for (int x = 10; x < 20; x++) c.Set(x, 5, Rgb(24, 22, 32));
        for (int x = 8; x < 22; x++) c.Set(x, 6, Rgb(24, 22, 32));
        Pix.Outline(c);
    }

    /// <summary>A bollard of dark polished stone with a gilded cap, 16 by 28.</summary>
    public static void PaintBollard(PixelCanvas c)
    {
        var stone = Tone.Of(84, 70, 108, 124, 106, 150, 54, 44, 72);
        var gold = Tone.Of(222, 182, 82, 250, 222, 132, 160, 120, 52);
        for (int y = 6; y < 27; y++)
            for (int x = 3; x < 13; x++)
            {
                float u = (x + 0.5f - 8f) / 5f;
                c.Set(x, y, u < -0.4f ? stone.Light : u > 0.5f ? stone.Dark : stone.Base);
            }
        for (int y = 1; y < 7; y++)
        {
            int half = y < 3 ? 2 + y : 6;
            for (int x = 8 - half; x < 8 + half; x++) c.Set(x, y, x < 7 ? gold.Light : x > 9 ? gold.Dark : gold.Base);
        }
        c.Rect(2, 25, 12, 2, stone.Dark);
        Pix.Outline(c);
    }

    // ------------------------------------------------------------------ the Pastoria Gym (plan 01 · M9 1b)

    /// <summary>Texels of a float's edge left round it on its tile, so the water shows between neighbouring floats.</summary>
    public const int FloatInset = 2;

    /// <summary>
    /// The mask the field's water shader draws the Pastoria Gym's pool with when its surface is drawn at
    /// <paramref name="level"/> (a drawn height): over the water's plate, at 32 texels a tile, every texel of a tile
    /// whose floor lies under the surface somewhere, except where a float rides on it; red is the distance to the
    /// shore in texels times <see cref="PixelGround.MaskScale"/>, as <see cref="PixelGround"/> bakes the field's.
    /// </summary>
    public static PixelCanvas PoolMask(Map map, float level)
    {
        const int T = GroundBaker.ArtTile;
        int w = PastoriaWater.PlateWidth * T, h = PastoriaWater.PlateDepth * T;
        var surface = new bool[w * h];
        for (int ty = 0; ty < PastoriaWater.PlateDepth; ty++)
            for (int tx = 0; tx < PastoriaWater.PlateWidth; tx++)
            {
                int mx = PastoriaWater.PlateX + tx, my = PastoriaWater.PlateZ + ty;
                if (map.GetGroundTile(mx, my) == TileType.Wall) continue;
                var (nw, ne, sw, se) = Relief.Corners(map, mx, my);
                if (MathF.Min(MathF.Min(nw, ne), MathF.Min(sw, se)) >= level - 0.01f) continue;
                bool raft = PastoriaWater.IsFloat(map, mx, my);
                for (int y = 0; y < T; y++)
                    for (int x = 0; x < T; x++)
                    {
                        bool under = raft && x >= FloatInset && x < T - FloatInset && y >= FloatInset && y < T - FloatInset;
                        if (!under) surface[(ty * T + y) * w + tx * T + x] = true;
                    }
            }
        var distance = PixelGround.ShoreDistance(surface, w, h);
        var mask = new PixelCanvas(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (surface[i]) mask.SetRaw(x, y, new Color(Math.Min(252, (int)MathF.Round(distance[i] * PixelGround.MaskScale)), 0, 0, 255));
            }
        return mask;
    }

    private static readonly Tone FloatPlank = Tone.Of(178, 134, 92, 192, 150, 104, 126, 90, 62);
    private static readonly Color FloatRim = Rgb(104, 74, 54);

    /// <summary>The top of a float: a raft of planks laid across it, a groove between boards and a dark rim round it.</summary>
    public static void PaintFloatTop(PixelCanvas c)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                int board = y / 8;
                var col = y % 8 == 7 ? FloatPlank.Dark : board % 3 == 1 ? FloatPlank.Light : FloatPlank.Base;
                if (x == 0 || y == 0 || x == c.Width - 1 || y == c.Height - 1) col = FloatRim;
                // A nail texel at each end of a board
                if (y % 8 == 3 && (x == 2 || x == c.Width - 3)) col = FloatRim;
                c.SetRaw(x, y, col);
            }
    }

    /// <summary>A float's side, the depth of its raft: the boards' ends over its rim.</summary>
    public static void PaintFloatSide(PixelCanvas c)
    {
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                c.SetRaw(x, y, y == c.Height - 1 ? FloatRim : y == 0 ? FloatPlank.Light : FloatPlank.Dark);
    }

    private static readonly Tone ButtonPlate = Tone.Of(196, 204, 216, 226, 232, 240, 140, 148, 166);

    /// <summary>The steel plate a button sits on, 26 texels square with a dark edge.</summary>
    public static void PaintButtonPlate(PixelCanvas c) => Pix.Raised(c, 0, 0, c.Width, c.Height, ButtonPlate);

    /// <summary>A button's colours: its face and its light rim (style guide, "Gyms": the buttons).</summary>
    public static (Color Face, Color Rim) ButtonColours(PastoriaWater.Button button) => button switch
    {
        PastoriaWater.Button.Blue => (Rgb(66, 120, 222), Rgb(130, 176, 246)),
        PastoriaWater.Button.Green => (Rgb(70, 178, 96), Rgb(140, 222, 150)),
        _ => (Rgb(236, 140, 48), Rgb(250, 196, 120))
    };

    /// <summary>A button's top, 20 texels square with its corners rounded off: its colour inside a light rim.</summary>
    public static void PaintButtonTop(PixelCanvas c, PastoriaWater.Button button)
    {
        var (face, rim) = ButtonColours(button);
        int w = c.Width, h = c.Height;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // Corners cut off by two texels: the button reads as round from the field's camera
                int cx = Math.Min(x, w - 1 - x), cy = Math.Min(y, h - 1 - y);
                if (cx + cy < 2) continue;
                bool edge = cx == 0 || cy == 0 || cx + cy == 2;
                c.SetRaw(x, y, edge ? rim : face);
            }
    }

    /// <summary>A button's side: its colour, darker.</summary>
    public static void PaintButtonSide(PixelCanvas c, PastoriaWater.Button button)
    {
        var (face, _) = ButtonColours(button);
        c.Fill(PixelCanvas.Shadow(face, 0.3f));
    }
}
