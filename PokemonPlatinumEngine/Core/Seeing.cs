using System;
using System.Collections.Generic;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>Which colours a player tells apart (plan 12 · Q8, the options' COLOUR VISION): Normal unless chosen.</summary>
public enum ColourVision { Normal, Protan, Deutan, Tritan }

/// <summary>How large the lines that are read are written (plan 12 · Q8, the options' TEXT SIZE): Normal unless chosen.</summary>
public enum TextSize { Normal, Large }

/// <summary>
/// Seeing (plan 12 · Q8; style guide, "Typography" and "Shapes and components", "Seeing"): the three options' rows as
/// the whole process holds them, set by <c>GameEngine.ApplySettings</c> alone (like <see cref="Accessibility"/>), and
/// the remap every colour of the interface kit goes through on its way out (<see cref="Seen"/>). GPU-free: a colour
/// in, a colour out. A test that changes it runs in the collection "Shared clocks and dice" and puts it back.
/// </summary>
public static class Seeing
{
    /// <summary>The remap in force: <see cref="ColourVision.Normal"/> leaves every colour as it is, to the bit.</summary>
    public static ColourVision Vision { get; private set; }

    /// <summary>Whether the lines that are read are written large (the field's text box, the battle's message box).</summary>
    public static bool LargeText { get; private set; }

    /// <summary>Whether the text box and the message box are drawn opaque, white on black.</summary>
    public static bool HighContrast { get; private set; }

    /// <summary>Whether a type pill carries its type's sign and an HP bar at half or under is hatched: under any remap or high contrast.</summary>
    public static bool Signs => Vision != ColourVision.Normal || HighContrast;

    /// <summary>The three rows as the options have them.</summary>
    public static void Use(ColourVision vision, TextSize size, bool highContrast)
    {
        if (vision != Vision) cache.Clear();
        Vision = vision;
        LargeText = size == TextSize.Large;
        HighContrast = highContrast;
    }

    /// <summary>All three back as Platinum draws (for tests).</summary>
    public static void Reset() => Use(ColourVision.Normal, TextSize.Normal, false);

    private static readonly Dictionary<uint, Color> cache = new();

    /// <summary>A colour as the remap in force draws it; under Normal the colour itself.</summary>
    public static Color Seen(Color c)
    {
        if (Vision == ColourVision.Normal) return c;
        uint key = (uint)(c.R | c.G << 8 | c.B << 16);
        if (!cache.TryGetValue(key, out var shown))
        {
            shown = Daltonise(c, Vision);
            if (cache.Count > 4096) cache.Clear();
            cache[key] = shown;
        }
        return shown with { A = c.A };
    }

    // Machado, Oliveira and Fernandes (2009), the full-strength matrices, in linear light
    private static readonly float[] Protan = { 0.152286f, 1.052583f, -0.204868f, 0.114503f, 0.786281f, 0.099216f, -0.003882f, -0.048116f, 1.051998f };
    private static readonly float[] Deutan = { 0.367322f, 0.860646f, -0.227968f, 0.280085f, 0.672501f, 0.047413f, -0.011820f, 0.042940f, 0.968881f };
    private static readonly float[] Tritan = { 1.255528f, -0.076749f, -0.178779f, -0.078411f, 0.930809f, 0.147602f, 0.004733f, 0.691367f, 0.303900f };

    private static float[] MatrixOf(ColourVision v) => v switch
    {
        ColourVision.Protan => Protan,
        ColourVision.Deutan => Deutan,
        _ => Tritan
    };

    /// <summary>A colour as an eye of the given kind sees it (Machado's matrix in linear light); Normal is the colour itself.</summary>
    public static Color Simulate(Color c, ColourVision v)
    {
        if (v == ColourVision.Normal) return c;
        var (r, g, b) = (Linear(c.R), Linear(c.G), Linear(c.B));
        var m = MatrixOf(v);
        return new Color(Gamma(m[0] * r + m[1] * g + m[2] * b), Gamma(m[3] * r + m[4] * g + m[5] * b), Gamma(m[6] * r + m[7] * g + m[8] * b), c.A);
    }

    /// <summary>
    /// The daltonising shift (Fidaner): what the eye loses is the colour less its simulation; red's loss goes into
    /// green and blue for Protan and Deutan, blue's into red and green for Tritan. A grey loses nothing and stays itself.
    /// </summary>
    public static Color Daltonise(Color c, ColourVision v)
    {
        if (v == ColourVision.Normal) return c;
        var (r, g, b) = (Linear(c.R), Linear(c.G), Linear(c.B));
        var m = MatrixOf(v);
        float sr = m[0] * r + m[1] * g + m[2] * b, sg = m[3] * r + m[4] * g + m[5] * b, sb = m[6] * r + m[7] * g + m[8] * b;
        float er = r - sr, eg = g - sg, eb = b - sb;
        float nr, ng, nb;
        if (v == ColourVision.Tritan)
        {
            nr = r + er + 0.7f * eb;
            ng = g + eg + 0.7f * eb;
            nb = b;
        }
        else
        {
            nr = r;
            ng = g + 0.7f * er + eg;
            nb = b + 0.7f * er + eb;
        }
        // Tuned for text and panels: the shift moves hue and must not move lightness, so the remapped colour is scaled
        // until the eye it is for sees it as light as an ordinary eye sees the colour, and every contrast holds
        float wanted = 0.2126f * r + 0.7152f * g + 0.0722f * b;
        float seen = 0.2126f * (m[0] * nr + m[1] * ng + m[2] * nb) + 0.7152f * (m[3] * nr + m[4] * ng + m[5] * nb) + 0.0722f * (m[6] * nr + m[7] * ng + m[8] * nb);
        if (seen > 1e-5f && wanted > 1e-5f)
        {
            float k = wanted / seen;
            nr *= k;
            ng *= k;
            nb *= k;
        }
        return new Color(Gamma(nr), Gamma(ng), Gamma(nb), c.A);
    }

    /// <summary>The WCAG contrast ratio of two colours, 1 to 21.</summary>
    public static float Contrast(Color a, Color b)
    {
        float la = Luminance(a), lb = Luminance(b);
        return (MathF.Max(la, lb) + 0.05f) / (MathF.Min(la, lb) + 0.05f);
    }

    /// <summary>A colour's relative luminance (WCAG), 0 to 1.</summary>
    public static float Luminance(Color c) => 0.2126f * Linear(c.R) + 0.7152f * Linear(c.G) + 0.0722f * Linear(c.B);

    private static float Linear(byte v)
    {
        float s = v / 255f;
        return s <= 0.04045f ? s / 12.92f : MathF.Pow((s + 0.055f) / 1.055f, 2.4f);
    }

    private static byte Gamma(float l)
    {
        l = Math.Clamp(l, 0f, 1f);
        float s = l <= 0.0031308f ? l * 12.92f : 1.055f * MathF.Pow(l, 1f / 2.4f) - 0.055f;
        return (byte)Math.Clamp((int)MathF.Round(s * 255f), 0, 255);
    }
}
