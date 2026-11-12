using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>A Max Raid den whose beam stands today: the middle of its ring on the ground, in metres, and its boss's stars.</summary>
internal readonly record struct DenBeam(Vector3 Foot, int Stars);

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

    private static Color Mix(Color a, Color b, float t) =>
        new((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t), (byte)255);
}
