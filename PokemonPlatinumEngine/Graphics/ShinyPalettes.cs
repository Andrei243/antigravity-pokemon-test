using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// One rule of a hand-picked shiny palette: a band of hues (<paramref name="From"/> to <paramref name="To"/> degrees,
/// going round past 360 when <paramref name="From"/> is the larger) turned to <paramref name="Hue"/>, its saturation
/// and value scaled. Only colours saturated enough to have a hue are touched (<see cref="ShinyPalette.HasHue"/>).
/// </summary>
internal readonly record struct HueRule(float From, float To, float Hue, float Saturation = 1f, float Value = 1f)
{
    public bool Holds(float hue) => From <= To ? hue >= From && hue <= To : hue >= From || hue <= To;
}

/// <summary>A hand-picked palette's word for a grey body: the hue and saturation its greys take, their value scaled.</summary>
internal readonly record struct GreyRule(float Hue, float Saturation, float Value = 1f);

/// <summary>
/// A model's shiny colours (plan 09 · L1; style guide, "Shiny Pokémon"): a function from each of its own colours to
/// its shiny one, worked out from the model's species and its main colour, never from anything that changes from run
/// to run. GPU-free: what is mapped is plain colours, which the upload and the decal painter hand it.
/// </summary>
internal sealed class ShinyPalette
{
    /// <summary>The saturation and value under which a colour has no hue worth turning: a grey, a white, a dark.</summary>
    public const float GreySaturation = 0.15f, DarkValue = 0.16f;

    /// <summary>How far, in each channel, a colour may be from the main colour and still be tinted with it (an eyelid painted in it).</summary>
    public const int MainReach = 24;

    /// <summary>The saturation a grey main colour is tinted to by the rule.</summary>
    public const float GreyTint = 0.3f;

    /// <summary>The value a black main colour is lifted to as it is tinted.</summary>
    public const float DarkLift = 0.22f;

    /// <summary>The species the palette is for (its National number's angle, or its row of the table).</summary>
    public string Species { get; }

    /// <summary>The angle the rule turns by, in degrees.</summary>
    public float Angle { get; }

    /// <summary>The body's main colour: the colour of the most vertices of its mesh.</summary>
    public Color Main { get; }

    /// <summary>The table's rules for this species, or none (the rule alone).</summary>
    public IReadOnlyList<HueRule> Rules { get; }

    public GreyRule? Greys { get; }

    public ShinyPalette(string species, float angle, Color main, IReadOnlyList<HueRule>? rules = null, GreyRule? greys = null)
    {
        Species = species;
        Angle = angle;
        Main = main;
        Rules = rules ?? Array.Empty<HueRule>();
        Greys = greys;
    }

    /// <summary>Whether the palette is the table's.</summary>
    public bool Picked => Rules.Count > 0 || Greys != null;

    /// <summary>Whether a colour is saturated and bright enough to have a hue that turns.</summary>
    public static bool HasHue(Color c)
    {
        var (_, s, v) = ToHsv(c);
        return s >= GreySaturation && v >= DarkValue;
    }

    /// <summary>A colour of the body (a vertex's, a mark's, the eyelid's) in the shiny's colours.</summary>
    public Color Map(Color c)
    {
        var (h, s, v) = ToHsv(c);
        bool nearMain = Math.Abs(c.R - Main.R) <= MainReach && Math.Abs(c.G - Main.G) <= MainReach && Math.Abs(c.B - Main.B) <= MainReach;
        if (Picked)
        {
            if (s >= GreySaturation && v >= DarkValue)
                foreach (var rule in Rules)
                    if (rule.Holds(h))
                        return FromHsv(rule.Hue, Math.Clamp(s * rule.Saturation, 0f, 1f), Math.Clamp(v * rule.Value, 0f, 1f), c.A);
            if (Greys is { } grey && s < GreySaturation && v >= DarkValue)
                return FromHsv(grey.Hue, grey.Saturation, Math.Clamp(v * grey.Value, 0f, 1f), c.A);
            // A main colour the table left alone takes the rule, so the main colour always changes
            if (!nearMain || !Unchanged(Main)) return c;
        }
        if (s >= GreySaturation && v >= DarkValue) return FromHsv(h + Angle, s, v, c.A);
        // A grey or black body is tinted in the angle's hue, with whatever is painted in its colour (a black one
        // lifted a little, or the tint wouldn't show)
        if (nearMain && !HasHue(Main)) return FromHsv(Angle, Math.Max(s, GreyTint), Math.Max(v, DarkLift), c.A);
        return c;
    }

    /// <summary>Whether the table's rules would leave a colour as it is.</summary>
    private bool Unchanged(Color c)
    {
        var (h, s, v) = ToHsv(c);
        if (s >= GreySaturation && v >= DarkValue) return !Rules.Any(r => r.Holds(h));
        return Greys == null || v < DarkValue;
    }

    /// <summary>An eye's colours (iris, pupil, sclera, glint): they stay, as the creature's eyes do.</summary>
    public Color MapEye(Color c) => c;

    /// <summary>The palette as text: what a signature hashes, and what tells two palettes apart.</summary>
    public string Describe()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(Species).Append('|').Append(Angle.ToString("R", CultureInfo.InvariantCulture)).Append('|').Append(Main.R).Append(',').Append(Main.G).Append(',').Append(Main.B);
        foreach (var r in Rules) sb.Append('|').Append(r.From).Append('-').Append(r.To).Append('>').Append(r.Hue).Append('x').Append(r.Saturation.ToString("R", CultureInfo.InvariantCulture)).Append('x').Append(r.Value.ToString("R", CultureInfo.InvariantCulture));
        if (Greys is { } g) sb.Append("|grey>").Append(g.Hue).Append('x').Append(g.Saturation.ToString("R", CultureInfo.InvariantCulture)).Append('x').Append(g.Value.ToString("R", CultureInfo.InvariantCulture));
        return sb.ToString();
    }

    // ------------------------------------------------------------------ colour arithmetic

    /// <summary>A colour's hue (degrees), saturation and value (0..1).</summary>
    public static (float H, float S, float V) ToHsv(Color c)
    {
        float r = c.R / 255f, g = c.G / 255f, b = c.B / 255f;
        float max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b)), d = max - min;
        float h = 0f;
        if (d > 0f)
        {
            if (max == r) h = 60f * (((g - b) / d) % 6f);
            else if (max == g) h = 60f * ((b - r) / d + 2f);
            else h = 60f * ((r - g) / d + 4f);
        }
        if (h < 0f) h += 360f;
        return (h, max <= 0f ? 0f : d / max, max);
    }

    /// <summary>A colour from a hue (any number of degrees), a saturation and a value.</summary>
    public static Color FromHsv(float h, float s, float v, byte alpha = 255)
    {
        h %= 360f;
        if (h < 0f) h += 360f;
        float c = v * s, x = c * (1f - Math.Abs(h / 60f % 2f - 1f)), m = v - c;
        (float r, float g, float b) = (h / 60f) switch
        {
            < 1f => (c, x, 0f),
            < 2f => (x, c, 0f),
            < 3f => (0f, c, x),
            < 4f => (0f, x, c),
            < 5f => (x, 0f, c),
            _ => (c, 0f, x)
        };
        static byte B(float f) => (byte)Math.Clamp((int)MathF.Round(f * 255f), 0, 255);
        return new Color(B(r + m), B(g + m), B(b + m), alpha);
    }
}

/// <summary>
/// The shiny palettes (plan 09 · L1; style guide, "Shiny Pokémon"): the table of hand-picked palettes for the designs
/// whose shiny is best known, our own colours after each, and the rule for every other species and form.
/// </summary>
internal static class ShinyPalettes
{
    /// <summary>Raise when a palette changes, so the shiny sprites baked are baked again (the plain ones never are).</summary>
    public const int Version = 1;

    /// <summary>The angle the rule turns a species' colours by: 60° + (47 × its National number mod 240)°.</summary>
    public static float AngleOf(int nationalNumber) => 60f + (47 * Math.Max(0, nationalNumber)) % 240;

    /// <summary>The National number a model's species has: a form takes its species'; a name the data doesn't know, nought.</summary>
    public static int NumberOf(string model)
    {
        if (PokemonDatabase.Get(model) is { } species) return species.DexNumber;
        if (PokemonDatabase.SpeciesOfForm(model) is { } owner) return owner.DexNumber;
        return 0;
    }

    /// <summary>The species a model is of (a form's species), for the table.</summary>
    private static string SpeciesOf(string model) =>
        PokemonDatabase.Get(model)?.Name ?? PokemonDatabase.SpeciesOfForm(model)?.Name ?? model;

    /// <summary>A model's shiny palette, from its name (a species or a form, never a look name) and the colours of its mesh.</summary>
    public static ShinyPalette For(string model, IReadOnlyList<Color> colors)
    {
        var main = MainColor(colors);
        string species = SpeciesOf(model);
        float angle = AngleOf(NumberOf(model));
        return Table.TryGetValue(species, out var picked)
            ? new ShinyPalette(species, angle, main, picked.Rules, picked.Greys)
            : new ShinyPalette(species, angle, main);
    }

    /// <summary>
    /// What a model's palette is made by, as text, without its mesh (its main colour is the sculpt's, which the
    /// model's own signature already covers): the table's row, or the rule's angle. What a shiny signature hashes.
    /// </summary>
    public static string Describe(string model)
    {
        string species = SpeciesOf(model);
        var palette = Table.TryGetValue(species, out var picked)
            ? new ShinyPalette(species, AngleOf(NumberOf(model)), default, picked.Rules, picked.Greys)
            : new ShinyPalette(species, AngleOf(NumberOf(model)), default);
        return palette.Describe();
    }

    /// <summary>The colour of the most vertices (ties to the lowest colour as a number, so it is the same every run).</summary>
    public static Color MainColor(IReadOnlyList<Color> colors)
    {
        if (colors.Count == 0) return new Color(128, 128, 128, 255);
        var counts = new Dictionary<int, int>();
        foreach (var c in colors)
        {
            int key = (c.R << 16) | (c.G << 8) | c.B;
            counts[key] = counts.TryGetValue(key, out int n) ? n + 1 : 1;
        }
        var best = counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First().Key;
        return new Color((byte)(best >> 16), (byte)(best >> 8), (byte)best, (byte)255);
    }

    private static HueRule R(float from, float to, float hue, float sat = 1f, float val = 1f) => new(from, to, hue, sat, val);

    /// <summary>
    /// The hand-picked palettes, by species (decision 2: the best-known designs by hand, the rest by rule). Each step of
    /// a line turns a few degrees further, so no two of one line are the same.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (HueRule[] Rules, GreyRule? Greys)> Table =
        new Dictionary<string, (HueRule[], GreyRule?)>(StringComparer.OrdinalIgnoreCase)
        {
            // A red Gyarados, a gold Magikarp
            ["Gyarados"] = (new[] { R(180, 260, 355, 1f, 0.95f) }, null),
            ["Magikarp"] = (new[] { R(340, 35, 46, 0.9f, 1.05f) }, null),
            // Charmander's line golden, and a black Charizard with crimson in its wings
            ["Charmander"] = (new[] { R(5, 45, 48, 0.95f, 1.04f) }, null),
            ["Charmeleon"] = (new[] { R(5, 45, 42, 0.95f, 1.0f) }, null),
            ["Charizard"] = (new[] { R(5, 50, 0, 0.18f, 0.34f), R(160, 260, 352, 0.9f, 0.85f) }, null),
            // The Sinnoh starters, lighter and yellower
            ["Turtwig"] = (new[] { R(70, 170, 62, 0.9f, 1.08f), R(15, 50, 34, 0.8f, 1.1f) }, null),
            ["Grotle"] = (new[] { R(70, 170, 66, 0.9f, 1.06f), R(15, 50, 36, 0.8f, 1.1f) }, null),
            ["Torterra"] = (new[] { R(70, 170, 70, 0.9f, 1.04f), R(15, 50, 38, 0.8f, 1.1f) }, null),
            ["Chimchar"] = (new[] { R(5, 40, 45, 0.95f, 1.05f) }, null),
            ["Monferno"] = (new[] { R(5, 40, 48, 0.95f, 1.05f) }, null),
            ["Infernape"] = (new[] { R(5, 40, 51, 0.95f, 1.05f) }, null),
            ["Piplup"] = (new[] { R(185, 250, 256, 0.8f, 1.08f) }, null),
            ["Prinplup"] = (new[] { R(185, 250, 262, 0.8f, 1.06f) }, null),
            ["Empoleon"] = (new[] { R(185, 250, 268, 0.8f, 1.04f) }, null),
            // Pikachu deeper, Raichu redder
            ["Pikachu"] = (new[] { R(40, 70, 36, 1.05f, 0.95f) }, null),
            ["Raichu"] = (new[] { R(15, 40, 12, 1f, 0.92f) }, null),
            // Eevee silver, and its evolutions
            ["Eevee"] = (new[] { R(10, 50, 30, 0.22f, 1.15f) }, null),
            ["Umbreon"] = (new[] { R(40, 70, 205, 1f, 1f) }, null),
            ["Espeon"] = (new[] { R(260, 345, 100, 0.8f, 1f) }, null),
            ["Vaporeon"] = (new[] { R(175, 245, 282, 0.8f, 1f) }, null),
            ["Jolteon"] = (new[] { R(40, 70, 88, 0.9f, 1f) }, null),
            ["Flareon"] = (new[] { R(0, 35, 40, 0.8f, 1.02f) }, null),
            // Riolu and Lucario, and the Shinx line, yellow
            ["Riolu"] = (new[] { R(185, 250, 48, 0.9f, 1.05f) }, null),
            ["Lucario"] = (new[] { R(185, 250, 52, 0.9f, 1.05f) }, null),
            ["Shinx"] = (new[] { R(185, 250, 46, 0.85f, 1.05f) }, null),
            ["Luxio"] = (new[] { R(185, 250, 50, 0.85f, 1.05f) }, null),
            ["Luxray"] = (new[] { R(185, 250, 54, 0.85f, 1.05f) }, null),
            // Gible's line rose
            ["Gible"] = (new[] { R(195, 265, 318, 0.8f, 1.02f) }, null),
            ["Gabite"] = (new[] { R(195, 265, 322, 0.8f, 1.02f) }, null),
            ["Garchomp"] = (new[] { R(195, 265, 326, 0.8f, 1.02f) }, null),
            // The legends of Sinnoh
            ["Giratina"] = (new[] { R(30, 70, 205, 0.35f, 1.05f), R(330, 20, 210, 0.8f, 0.9f) }, new GreyRule(212, 0.12f, 1.05f)),
            ["Dialga"] = (new[] { R(185, 250, 162, 0.9f, 1f) }, null),
            ["Palkia"] = (new[] { R(290, 355, 268, 0.9f, 1f) }, null),
            // A black Rayquaza, a grey Gengar, a silver Metagross, a green-tailed Mewtwo, a blue Gardevoir
            ["Rayquaza"] = (new[] { R(75, 175, 0, 0.05f, 0.3f) }, null),
            ["Gengar"] = (new[] { R(245, 305, 250, 0.3f, 0.75f) }, null),
            ["Metagross"] = (new[] { R(185, 250, 210, 0.08f, 1.2f) }, null),
            ["Mewtwo"] = (new[] { R(255, 325, 118, 0.8f, 1f) }, null),
            ["Gardevoir"] = (new[] { R(75, 175, 200, 0.9f, 1f) }, null),
        };
}
