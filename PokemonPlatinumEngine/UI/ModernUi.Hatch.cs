using System;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>An Egg (plan 06 · R15): the hatching scene's, drawn large and smooth, and the words an Egg's summary says.</summary>
internal static partial class ModernUi
{
    /// <summary>An Egg's shell, its spots and the ink round it (style guide, "Eggs").</summary>
    public static readonly Color EggShell = new(246, 236, 210, 255), EggShade = new(222, 206, 172, 255), EggSpot = new(104, 178, 136, 255);

    // The spots, in the Egg's own measure: across from its middle and up from its foot, in heights, and their sizes
    private static readonly (float X, float Y, float R)[] EggSpots =
    {
        (-0.17f, 0.30f, 0.075f), (0.12f, 0.22f, 0.06f), (0.02f, 0.46f, 0.085f), (-0.08f, 0.62f, 0.055f), (0.16f, 0.58f, 0.05f), (0.03f, 0.82f, 0.045f)
    };

    /// <summary>
    /// The hatching scene's Egg, standing with its foot at <paramref name="foot"/>, <paramref name="height"/> tall,
    /// leaning <paramref name="lean"/> radians about its foot, with <paramref name="cracks"/> (0 to 1) of its cracks run
    /// across it and <paramref name="white"/> (0 to 1) of the light it bursts in.
    /// </summary>
    public static void HatchEgg(Vector2 foot, float height, float lean, float cracks, float white)
    {
        // Its shadow on the floor stays put as it rocks
        SoftLight(foot + new Vector2(0, 6), height * 0.95f, height * 0.16f, new Color(0, 0, 20, 150));

        Vector2 At(float x, float y)
        {
            // From the Egg's measure (x across, y up from the foot, in heights) to the screen, leaning about the foot
            float px = x * height, py = -y * height;
            float cos = MathF.Cos(lean), sin = MathF.Sin(lean);
            return foot + new Vector2(px * cos - py * sin, px * sin + py * cos);
        }

        const int points = 48;
        var outline = new Vector2[points];
        var shell = new Vector2[points];
        var lit = new Vector2[points];
        for (int i = 0; i < points; i++)
        {
            float a = i * MathF.Tau / points;
            float up = MathF.Sin(a);
            // An ellipse narrowed toward its top: the shape of an egg
            float x = 0.37f * MathF.Cos(a) * (1f - 0.17f * up);
            float y = 0.5f + 0.5f * up;
            shell[i] = At(x, y);
            // The lit part of the shell: the same shape a little smaller and toward the upper left, which leaves a
            // crescent of shade along the lower right inside the outline
            lit[i] = At(x * 0.9f - 0.025f, 0.05f + y * 0.92f);
            outline[i] = At(x * 1.035f + MathF.Cos(a) * 0.012f, 0.5f + (y - 0.5f) * 1.03f);
        }

        Rlgl.DisableBackfaceCulling();
        Fan(At(0, 0.5f), outline, Ink);
        Fan(At(0, 0.5f), shell, EggShade);
        Fan(At(-0.025f, 0.51f), lit, EggShell);
        Rlgl.EnableBackfaceCulling();

        float s = height;
        Raylib.BeginBlendMode(BlendMode.Alpha);
        foreach (var (x, y, r) in EggSpots)
        {
            var c = At(x, y);
            UiShapes.Circle(c, r * s, EggSpot);
        }
        // A shine on its upper left
        UiShapes.Circle(At(-0.16f, 0.78f), 0.06f * s, new Color(255, 255, 255, 170));
        UiShapes.Circle(At(-0.22f, 0.68f), 0.03f * s, new Color(255, 255, 255, 140));
        Raylib.EndBlendMode();

        // The cracks: a zigzag round its upper half, run from left to right, then a second branch
        if (cracks > 0.01f)
        {
            var zig = new[] { (-0.33f, 0.60f), (-0.22f, 0.66f), (-0.12f, 0.58f), (-0.02f, 0.68f), (0.09f, 0.59f), (0.19f, 0.67f), (0.30f, 0.60f) };
            float run = Math.Clamp(cracks * 1.4f, 0f, 1f) * (zig.Length - 1);
            for (int i = 0; i < zig.Length - 1 && i < run; i++)
            {
                float part = Math.Clamp(run - i, 0f, 1f);
                var a = At(zig[i].Item1, zig[i].Item2);
                var b = At(zig[i + 1].Item1, zig[i + 1].Item2);
                UiShapes.Line(a, Vector2.Lerp(a, b, part), 0.016f * s, Ink);
            }
            if (cracks > 0.55f)
            {
                var branch = new[] { (-0.02f, 0.68f), (0.03f, 0.78f), (-0.04f, 0.86f) };
                float more = Math.Clamp((cracks - 0.55f) / 0.45f, 0f, 1f) * (branch.Length - 1);
                for (int i = 0; i < branch.Length - 1 && i < more; i++)
                {
                    float part = Math.Clamp(more - i, 0f, 1f);
                    var a = At(branch[i].Item1, branch[i].Item2);
                    var b = At(branch[i + 1].Item1, branch[i + 1].Item2);
                    UiShapes.Line(a, Vector2.Lerp(a, b, part), 0.013f * s, Ink);
                }
            }
        }

        // The light it bursts in
        if (white > 0.01f)
        {
            Rlgl.DisableBackfaceCulling();
            Fan(At(0, 0.5f), outline, new Color(255, 255, 255, (int)Math.Clamp(white * 255f, 0f, 255f)));
            Rlgl.EnableBackfaceCulling();
            Raylib.BeginBlendMode(BlendMode.Additive);
            SoftLight(At(0, 0.5f), height * 1.6f, height * 1.6f, Lit(Color.White, 0.6f * white));
            Raylib.EndBlendMode();
        }
    }

    private static void Fan(Vector2 centre, Vector2[] rim, Color color)
    {
        for (int i = 0; i < rim.Length; i++) Raylib.DrawTriangle(centre, rim[i], rim[(i + 1) % rim.Length], color);
    }

    /// <summary>What an Egg's summary shows in place of its stats: its watch, by the cycles it has left (<see cref="Breeding.Watch"/>).</summary>
    public static void EggWatch(Rectangle r, Pokemon egg)
    {
        UiFonts.Draw("THE EGG WATCH", r.X + 36, r.Y + 30, 30, Muted, UiWeight.Black);
        float y = r.Y + 84;
        foreach (string line in Wrap(Breeding.Watch(egg), r.Width - 72, 34))
        {
            UiFonts.Draw(line, r.X + 36, y, 34, Ink, UiWeight.Bold);
            y += 46;
        }
    }
}
