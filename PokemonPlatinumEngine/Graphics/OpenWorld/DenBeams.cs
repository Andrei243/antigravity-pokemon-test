using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>
/// A Max Raid den whose beam stands today: the middle of its ring on the ground, in metres, and its boss's stars; or a Tera
/// crystal of Paldea's (plan 34 · PD7), whose <see cref="Tera"/> colours a shorter glow over a crystal at its foot.
/// </summary>
internal readonly record struct DenBeam(Vector3 Foot, int Stars, Models.TeraType? Tera = null);

/// <summary>
/// The beams of light over Galar's lit Max Raid dens (plan 33 · GL3, its second session; style guide, "Max Raid dens"):
/// each a column of soft red light rising from its den's ring, about two metres wide at the foot and fading out at
/// thirty metres, breathing over two seconds. Our own. Made as ribbons that face the camera (the weather's own
/// <see cref="WeatherPiece"/>), stacked a few metres at a time so the light fades as it climbs, in three widths: a hot
/// core, the column and a faint halo. A pure function of the beams, the eye and the field's time; GPU-free.
/// </summary>
internal static class DenBeams
{
    /// <summary>How high a beam reaches, how wide its column stands at its foot, and how long one breath takes, in metres and seconds.</summary>
    public const float Height = 30f, Width = 2f, Breath = 2f;

    /// <summary>How many stacked lengths a beam is drawn in, and how far off one is still drawn.</summary>
    public const int Lengths = 6;
    public const float Reach = 420f;

    /// <summary>The beam's red, deepening toward its top.</summary>
    public static readonly Color Foot = new(255, 92, 70, 255), Top = new(214, 36, 52, 255);

    /// <summary>The three widths a beam is laid in (a share of <see cref="Width"/>) and how strong each is.</summary>
    private static readonly (float Share, float Strength)[] Layers = { (0.35f, 0.85f), (1f, 0.45f), (2.4f, 0.14f) };

    /// <summary>The ribbons of every beam within <see cref="Reach"/> of the eye, added to <paramref name="into"/>.</summary>
    public static void Pieces(IReadOnlyList<DenBeam> beams, Vector3 eye, double time, List<WeatherPiece> into)
    {
        into.Clear();
        if (beams.Count == 0) return;
        // A two-second breath: between 80 % and 100 % of its light
        float breath = 0.9f + 0.1f * MathF.Sin((float)(time % 3600.0) * MathF.Tau / Breath);
        foreach (var beam in beams)
        {
            if (Vector3.DistanceSquared(beam.Foot, eye) > Reach * Reach) continue;
            if (beam.Tera is { } tera)
            {
                Crystal(beam, tera, breath, into);
                continue;
            }
            // A boss of more stars stands a little brighter in its column (four and five stars the brightest)
            float stars = 0.85f + 0.05f * Math.Clamp(beam.Stars, 1, 5);
            for (int i = 0; i < Lengths; i++)
            {
                float y0 = Height * i / Lengths, y1 = Height * (i + 1) / Lengths;
                // Full at the foot, gone at the top: the strength of a length is that of its middle
                float fade = 1f - (i + 0.5f) / Lengths;
                fade *= fade;
                var colour = Mix(Foot, Top, (i + 0.5f) / Lengths);
                foreach (var (share, strength) in Layers)
                {
                    int alpha = (int)MathF.Round(255f * strength * fade * breath * stars);
                    if (alpha <= 0) continue;
                    into.Add(new WeatherPiece(beam.Foot + new Vector3(0, y1, 0), beam.Foot + new Vector3(0, y0, 0), Width * share,
                        new Color(colour.R, colour.G, colour.B, (byte)Math.Min(255, alpha))));
                }
            }
        }
    }

    /// <summary>How high a Tera crystal's glow reaches, and how tall and wide the crystal at its foot stands, in metres (plan 34 · PD7).</summary>
    public const float CrystalGlow = 14f, CrystalHeight = 1.6f, CrystalWidth = 1.1f;

    /// <summary>The colour of a Tera Type's crystal: its type's own light (a move's colours), a Stellar crystal white.</summary>
    public static Color CrystalColour(Models.TeraType tera) =>
        tera.IsStellar ? new Color(250, 250, 255, 255) : MoveFx.ColorsOf(tera.Type).Main;

    /// <summary>
    /// A Tera crystal's pieces (plan 34 · PD7; style guide, "Tera crystals"): the crystal itself, two crossed bright ribbons
    /// narrowing to a point at its top, and over it a glow of its type's colour, half as wide as a den's beam and under half
    /// as high, fading to nothing as it climbs and breathing as a den's does.
    /// </summary>
    private static void Crystal(DenBeam beam, Models.TeraType tera, float breath, List<WeatherPiece> into)
    {
        var colour = CrystalColour(tera);
        var light = Mix(colour, new Color(255, 255, 255, 255), 0.55f);
        float stars = 0.85f + 0.03f * Math.Clamp(beam.Stars, 1, 7);
        // A black crystal (plan 34 · PD9, six and seven stars): a body near black, a fifth taller, lit from within
        bool black = IsBlack(beam.Stars);
        var body = black ? BlackBody : colour;
        float height = black ? CrystalHeight * 1.2f : CrystalHeight;
        // The crystal: a bright core and its body, each in four lengths narrowing toward the point
        for (int i = 0; i < 4; i++)
        {
            float y0 = height * i / 4, y1 = height * (i + 1) / 4;
            float width = CrystalWidth * (1f - (i + 0.5f) / 4f * 0.85f);
            into.Add(new WeatherPiece(beam.Foot + new Vector3(0, y1, 0), beam.Foot + new Vector3(0, y0, 0), width,
                new Color(body.R, body.G, body.B, (byte)Math.Min(255, (int)(225 * breath * stars)))));
            into.Add(new WeatherPiece(beam.Foot + new Vector3(0, y1, 0), beam.Foot + new Vector3(0, y0, 0), width * 0.45f,
                new Color(light.R, light.G, light.B, (byte)Math.Min(255, (int)(235 * breath)))));
        }
        // The glow over it
        for (int i = 0; i < Lengths; i++)
        {
            float y0 = height + (CrystalGlow - height) * i / Lengths, y1 = height + (CrystalGlow - height) * (i + 1) / Lengths;
            float fade = 1f - (i + 0.5f) / Lengths;
            fade *= fade;
            foreach (var (share, strength) in Layers)
            {
                int alpha = (int)MathF.Round(255f * strength * 0.8f * fade * breath * stars);
                if (alpha <= 0) continue;
                into.Add(new WeatherPiece(beam.Foot + new Vector3(0, y1, 0), beam.Foot + new Vector3(0, y0, 0), Width * 0.5f * share,
                    new Color(colour.R, colour.G, colour.B, (byte)Math.Min(255, alpha))));
            }
        }
    }

    /// <summary>The body of a black crystal (plan 34 · PD9; style guide, "Tera crystals"): near black, lit from within by its core.</summary>
    public static readonly Color BlackBody = new(28, 22, 34, 255);

    /// <summary>Whether a crystal of so many stars is a black one: six and seven, after Paldea's Hall of Fame.</summary>
    public static bool IsBlack(int stars) => stars >= 6;

    private static Color Mix(Color a, Color b, float t) =>
        new((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t), (byte)255);
}
