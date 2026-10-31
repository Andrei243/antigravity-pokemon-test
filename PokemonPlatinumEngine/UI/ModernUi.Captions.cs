using System;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

// Captions and the hold's ring (plan 12 · Q9; style guide, "Field menus and notices", "Captions" and "Holding to confirm").
internal static partial class ModernUi
{
    /// <summary>The hold's ring on a dangerous answer: its radius and its width.</summary>
    public const float HoldRingRadius = 18f, HoldRingWidth = 5f;

    /// <summary>A caption's pill: its height, the gap between two, and where the column stands (its left edge, and the newest's foot).</summary>
    public const float CaptionHeight = 48f, CaptionGap = 10f, CaptionLeft = 96f, CaptionFoot = 730f;

    /// <summary>
    /// The hold's ring at <paramref name="centre"/>: a track of white at 70 and over it the fill in white, growing
    /// clockwise from twelve o'clock as <paramref name="fill"/> goes from 0 to 1, in short strokes as the skip's ring is.
    /// </summary>
    public static void HoldRing(Vector2 centre, float fill)
    {
        UiShapes.Ring(centre, HoldRingRadius, HoldRingWidth, new Color(255, 255, 255, 70));
        float turn = Math.Clamp(fill, 0f, 1f) * MathF.Tau;
        if (turn <= 0f) return;
        float middle = HoldRingRadius - HoldRingWidth / 2f;
        Vector2 At(float angle) => centre + new Vector2(MathF.Sin(angle), -MathF.Cos(angle)) * middle;
        float step = MathF.Tau / 48;
        for (float from = 0f; from < turn; from += step)
            UiShapes.Line(At(from), At(MathF.Min(turn, from + step)), HoldRingWidth, Color.White);
    }

    /// <summary>The captions standing, a pill each in a column at the left, the newest at the bottom (the oldest first in <paramref name="lines"/>).</summary>
    public static void Captions(IReadOnlyList<CaptionLine> lines)
    {
        float foot = CaptionFoot;
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            var line = lines[i];
            float alpha = CaptionQueue.Strength(line.Age);
            if (alpha > 0f) CaptionPill(line, new Vector2(CaptionLeft, foot - CaptionHeight), alpha);
            foot -= CaptionHeight + CaptionGap;
        }
    }

    private static void CaptionPill(CaptionLine line, Vector2 at, float alpha)
    {
        string times = line.Times > 1 ? $"×{line.Times}" : "";
        float tw = UiFonts.Measure(line.Text, 26, UiWeight.Bold);
        float timesWidth = times.Length > 0 ? 10 + UiFonts.Measure(times, 22, UiWeight.Bold) : 0f;
        float w = Math.Max(72f, 52 + tw + timesWidth + 24);
        var r = new Rectangle(at.X, at.Y, w, CaptionHeight);
        byte A(int a) => (byte)Math.Clamp(a * alpha, 0f, 255f);
        var dark = new Color(36, 44, 68, (int)A(235));
        UiShapes.Shape(r, CaptionHeight / 2f, dark, dark, new Color(255, 255, 255, (int)A(60)), 2);

        // The sound's sign: a little box and its cone, and one arc of a wave beyond it; Gold for a cry
        var ink = line.Kind == CaptionKind.Cry ? Gold : Selection;
        ink = ink with { A = A(255) };
        float cy = r.Y + CaptionHeight / 2f, x = r.X + 20;
        UiShapes.Fill(new Rectangle(x - 6, cy - 4, 5, 8), 1, ink);
        UiShapes.Triangle(new Vector2(x - 2, cy - 4), new Vector2(x + 5, cy - 9), new Vector2(x + 5, cy + 9), ink);
        UiShapes.Triangle(new Vector2(x - 2, cy - 4), new Vector2(x + 5, cy + 9), new Vector2(x - 2, cy + 4), ink);
        for (int k = -3; k < 3; k++)
        {
            float a0 = k * 0.25f, a1 = (k + 1) * 0.25f;
            UiShapes.Line(new Vector2(x + 9 + MathF.Cos(a0) * 5, cy + MathF.Sin(a0) * 7), new Vector2(x + 9 + MathF.Cos(a1) * 5, cy + MathF.Sin(a1) * 7), 2.5f, ink);
        }

        UiFonts.DrawCentered(line.Text, r.X + 52, cy, 26, Color.White with { A = A(255) }, UiWeight.Bold);
        if (times.Length > 0) UiFonts.DrawCentered(times, r.X + 52 + tw + 10, cy, 22, Selection with { A = A(255) }, UiWeight.Bold);
    }
}
