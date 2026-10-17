using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Core;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>
/// The pad's signs (plan 12 · Q5; style guide, "Key hints"): the face buttons as coloured discs lettered A, B, X and Y,
/// the shoulders and triggers as small slabs, Start and Select as dark pills with their marks, the d-pad as a cross
/// with its pressed arms lit. All our own, drawn from <see cref="UiShapes"/> at the height of a hint's cap (40).
/// </summary>
internal static class PadGlyphArt
{
    /// <summary>The height a sign is designed for: a hint's cap.</summary>
    public const float CapHeight = 40;

    private static readonly Color Slab = new(78, 88, 112, 255);
    private static readonly Color Dark = new(42, 48, 66, 255);

    public static Color FaceColor(PadGlyph glyph) => glyph switch
    {
        PadGlyph.A => new Color(64, 168, 96, 255),
        PadGlyph.B => new Color(222, 76, 72, 255),
        PadGlyph.X => new Color(70, 132, 226, 255),
        PadGlyph.Y => new Color(236, 184, 52, 255),
        _ => Slab
    };

    /// <summary>The width a sign takes in a cap.</summary>
    public static float Width(PadGlyph glyph) => glyph switch
    {
        PadGlyph.L or PadGlyph.R => 58,
        PadGlyph.ZL or PadGlyph.ZR => 64,
        PadGlyph.Start or PadGlyph.Select => 66,
        _ => 44
    };

    /// <summary>Draws a sign centred at a point.</summary>
    public static void Draw(PadGlyph glyph, Vector2 c, float scale = 1f)
    {
        switch (glyph)
        {
            case PadGlyph.A or PadGlyph.B or PadGlyph.X or PadGlyph.Y:
                UiShapes.Circle(c, 17 * scale, FaceColor(glyph));
                UiShapes.Ring(c, 17 * scale, 2, new Color(0, 0, 0, 50));
                Letter(PadGlyphs.Name(glyph), c, 22 * scale, Color.White);
                break;
            case PadGlyph.L or PadGlyph.R:
                UiShapes.Shape(Around(c, 50, 28, scale), 12 * scale, Slab, Dark);
                Letter(PadGlyphs.Name(glyph), c, 20 * scale, Color.White);
                break;
            case PadGlyph.ZL or PadGlyph.ZR:
                UiShapes.Fill(Around(c, 56, 30, scale), 13 * scale, Dark);
                Letter(PadGlyphs.Name(glyph), c, 19 * scale, Color.White);
                break;
            case PadGlyph.Start:
                UiShapes.Fill(Around(c, 58, 26, scale), 13 * scale, Dark);
                for (int i = -1; i <= 1; i++)
                    UiShapes.Fill(new Rectangle(c.X - 9 * scale, c.Y + (i * 6 - 1.5f) * scale, 18 * scale, 3 * scale), 1.5f * scale, Color.White);
                break;
            case PadGlyph.Select:
                UiShapes.Fill(Around(c, 58, 26, scale), 13 * scale, Dark);
                UiShapes.Fill(new Rectangle(c.X - 11 * scale, c.Y - 6 * scale, 11 * scale, 9 * scale), 2 * scale, Color.White);
                UiShapes.Fill(new Rectangle(c.X - 2 * scale, c.Y - 3 * scale, 11 * scale, 9 * scale), 2 * scale, Color.White with { A = 200 });
                break;
            default:
                Cross(glyph, c, scale);
                break;
        }
    }

    private static Rectangle Around(Vector2 c, float w, float h, float scale) =>
        new(c.X - w * scale / 2f, c.Y - h * scale / 2f, w * scale, h * scale);

    private static void Letter(string text, Vector2 c, float size, Color color)
    {
        float w = UiFonts.Measure(text, size, UiWeight.Black);
        UiFonts.DrawCentered(text, c.X - w / 2f, c.Y, size, color, UiWeight.Black);
    }

    private static void Cross(PadGlyph glyph, Vector2 c, float s)
    {
        bool up = glyph is PadGlyph.DPad or PadGlyph.DPadUp or PadGlyph.DPadVertical;
        bool down = glyph is PadGlyph.DPad or PadGlyph.DPadDown or PadGlyph.DPadVertical;
        bool left = glyph is PadGlyph.DPad or PadGlyph.DPadLeft or PadGlyph.DPadHorizontal;
        bool right = glyph is PadGlyph.DPad or PadGlyph.DPadRight or PadGlyph.DPadHorizontal;
        UiShapes.Fill(new Rectangle(c.X - 6 * s, c.Y - 17 * s, 12 * s, 34 * s), 4 * s, Dark);
        UiShapes.Fill(new Rectangle(c.X - 17 * s, c.Y - 6 * s, 34 * s, 12 * s), 4 * s, Dark);
        var lit = new Color(250, 206, 70, 255);
        if (up) UiShapes.Fill(new Rectangle(c.X - 4 * s, c.Y - 15 * s, 8 * s, 10 * s), 3 * s, lit);
        if (down) UiShapes.Fill(new Rectangle(c.X - 4 * s, c.Y + 5 * s, 8 * s, 10 * s), 3 * s, lit);
        if (left) UiShapes.Fill(new Rectangle(c.X - 15 * s, c.Y - 4 * s, 10 * s, 8 * s), 3 * s, lit);
        if (right) UiShapes.Fill(new Rectangle(c.X + 5 * s, c.Y - 4 * s, 10 * s, 8 * s), 3 * s, lit);
    }
}
