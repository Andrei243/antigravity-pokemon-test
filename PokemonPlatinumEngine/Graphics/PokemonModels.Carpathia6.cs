using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 6 (plan 46 · CP4 to CP8): Izvoras to Oracul (20 species), sculpted from the briefs of
// docs/plans/decisions/CP-dex-2b-mountains-more.md and CP-dex-3-river-and-caves.md and the user's reference pictures.
// None has a forebear to build on: each is sculpted from nothing, a line's members sharing their colours and motifs.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames6() => new[]
    {
        "Izvoras", "Izvorana", "Nametel", "Nameton", "Zaganul", "Cremenel", "Cremenor", "Aurel", "Aurifer", "Norel",
        "Tunetor", "Grindinel", "Vantulet", "Vantoroi", "Morunel", "Belugrand", "Somnel", "Somnoros", "Brotacel", "Oracul"
    };

    private static PokeBuilder? CreateCarpathia6(string species) => species switch
    {
        "IZVORAS" => Izvoras(),
        "IZVORANA" => Izvorana(),
        "NAMETEL" => Nametel(),
        "NAMETON" => Nameton(),
        "ZAGANUL" => Zaganul(),
        "CREMENEL" => Cremenel(),
        "CREMENOR" => Cremenor(),
        "AUREL" => Aurel(),
        "AURIFER" => Aurifer(),
        "NOREL" => Norel(),
        "TUNETOR" => Tunetor(),
        "GRINDINEL" => Grindinel(),
        "VANTULET" => Vantulet(),
        "VANTOROI" => Vantoroi(),
        "MORUNEL" => Morunel(),
        "BELUGRAND" => Belugrand(),
        "SOMNEL" => Somnel(),
        "SOMNOROS" => Somnoros(),
        "BROTACEL" => Brotacel(),
        "ORACUL" => Oracul(),
        _ => null
    };

    // ------------------------------------------------------------------ helpers of this batch

    /// <summary>
    /// Round lumps over an ellipsoid, spread by the golden angle (a cloud's billows, a heap of snow): none under
    /// <paramref name="below"/> and none within <paramref name="face"/> of straight ahead, where the face goes.
    /// </summary>
    private static void C6Lobes(PokeBuilder b, int bone, Vector3 center, Vector3 radii, int count, float r, Color color, float below = -0.7f, float face = 0.6f,
        float blend = 0.02f, SurfaceMaterial? mat = null)
    {
        for (int i = 0; i < count; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / count;
            float ring = MathF.Sqrt(1f - y * y);
            float a = i * 2.39996f;
            var dir = V(MathF.Cos(a) * ring, y, MathF.Sin(a) * ring);
            if (y < below || dir.Z > face) continue;
            float k = r * (0.82f + 0.36f * ((i * 7) % 5) / 4f);
            b.Ell(bone, center + dir * radii * 0.9f, V(k, k * 0.88f, k), color, mat: mat, blend: blend);
        }
    }

    /// <summary>Flat facets knapped off an ellipsoid: a plane cut <paramref name="depth"/> of the way out along each direction.</summary>
    private static void C6Facets(PokeBuilder b, int bone, Vector3 center, Vector3 radii, Vector3[] dirs, float depth)
    {
        foreach (var d0 in dirs)
        {
            var d = Vector3.Normalize(d0);
            float reach = (d * radii).Length() * depth;
            b.CutBox(bone, center + d * (reach + 0.3f), V(0.6f, 0.3f, 0.6f), PokeBuilder.AlignY(d));
        }
    }

    /// <summary>A zigzag of light from <paramref name="from"/> to <paramref name="to"/>, kinked <paramref name="kink"/> to each side.</summary>
    private static Vector3[] C6Bolt(Vector3 from, Vector3 to, Vector3 kink, int steps)
    {
        var points = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
            points[i] = Vector3.Lerp(from, to, (float)i / steps) + (i == 0 || i == steps ? Vector3.Zero : kink * (i % 2 == 0 ? -1f : 1f));
        return points;
    }

    /// <summary>A spiral lying on the top of an upright ellipsoid (a whirl's crown), from the rim in to the middle.</summary>
    private static Vector3[] C6Crown(Vector3 center, Vector3 radii, float from, float to, float turns, int steps, float lift)
    {
        var points = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps, a = turns * MathF.Tau * t;
            float rr = from + (to - from) * t;
            float x = MathF.Cos(a) * rr, z = MathF.Sin(a) * rr;
            float u = x / radii.X, w = z / radii.Z;
            points[i] = center + V(x, radii.Y * MathF.Sqrt(MathF.Max(0f, 1f - u * u - w * w)) + lift, z);
        }
        return points;
    }

    // ------------------------------------------------------------------ Izvoras line

    private static readonly Color C6Water = Rgb(92, 190, 220);
    private static readonly Color C6WaterDeep = Rgb(54, 142, 194);
    private static readonly Color C6WaterPale = Rgb(178, 230, 240);

    /// <summary>Izvoras: a drop of spring water bulging up from a mossy grey stone, its crown curling over like a ripple, bubbles clinging to it.</summary>
    private static PokeBuilder Izvoras()
    {
        var b = new PokeBuilder("Izvoras", 0.5f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell };
        var stone = Rgb(130, 130, 122);
        var moss = Rgb(104, 142, 66);

        // The stone it wells up from, flecked with moss and a few tufts round its rim
        b.Ell(Root, V(0, 0.085f, -0.01f), V(0.2f, 0.09f, 0.17f), stone);
        Rubble(b, Root, V(0, 0.08f, -0.01f), V(0.17f, 0.07f, 0.14f), 0.045f, stone, 7, 10101u, 0.6f);
        foreach (var (x, z) in new[] { (-0.12f, 0.08f), (0.13f, 0.05f), (0.02f, -0.13f), (-0.15f, -0.06f) })
            b.PaintEll(Root, V(x, 0.15f, z), V(0.05f, 0.05f, 0.045f), moss);
        foreach (var (x, z) in new[] { (-0.1f, 0.1f), (0.1f, 0.09f), (0.15f, -0.04f), (-0.13f, -0.06f), (0.03f, -0.13f) })
            b.Ell(Root, V(x, 0.16f, z), V(0.034f, 0.022f, 0.03f), moss, mat: Leaf, blend: 0.012f);

        // The drop: clear water, deeper below, a pale ripple across its brow
        var c = V(0, 0.29f, 0.01f);
        var r = V(0.15f, 0.14f, 0.14f);
        b.Ell(Body, c, r, C6Water);
        b.PaintEll(Body, c + V(0, -0.1f, 0), V(0.16f, 0.06f, 0.15f), C6WaterDeep);
        b.PaintEll(Body, c + V(-0.04f, 0.08f, 0.06f), V(0.06f, 0.035f, 0.06f), C6WaterPale);
        // Its crown, a wave rising off the top and curling over
        int crest = b.Part("crest", Body, c + V(0.02f, 0.11f, -0.02f), PokeRole.Flame);
        b.Spike(crest, c + V(0.0f, 0.08f, -0.02f), c + V(0.07f, 0.21f, -0.05f), 0.07f, C6Water);
        b.Tube(crest, Smooth(3, c + V(0.06f, 0.19f, -0.05f), c + V(0.11f, 0.21f, -0.04f), c + V(0.14f, 0.18f, -0.02f), c + V(0.13f, 0.155f, 0.0f)),
            0.022f, 0.014f, C6Water);
        // Bubbles welling out of its sides
        foreach (var (x, y, rr) in new[] { (-0.14f, 0.27f, 0.028f), (0.13f, 0.33f, 0.024f), (0.11f, 0.21f, 0.02f), (-0.1f, 0.37f, 0.02f) })
        {
            var at = c + V(x, y - c.Y, 0);
            float u = x / r.X, v = (y - c.Y) / r.Y;
            at.Z = c.Z + r.Z * MathF.Sqrt(MathF.Max(0f, 1f - u * u - v * v)) * 0.55f;
            b.Ell(Body, at + Vector3.Normalize(at - c) * rr * 0.4f, V(rr, rr, rr), C6WaterPale, blend: 0.006f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.052f * s, 0.305f);
            b.Eye(Body, at, Outward(c, r, at), 0.026f, Rgb(36, 60, 120));
        });
        b.Mark(Body, On(c, r, 0, 0.235f), V(0, -0.3f, 1f), 0.05f, 0.012f, C6WaterDeep, MarkShape.Wave);
        b.Ground();
        return b;
    }

    /// <summary>Izvorana: a slim maiden poured from a mountain stream, her gown running to the ground among pebbles, a crown of ferns and watercress, streamers of water from her arms.</summary>
    private static PokeBuilder Izvorana()
    {
        var b = new PokeBuilder("Izvorana", 0.86f, BodyPlan.Biped, V(0, 0.56f, 0)) { Coat = Shell };
        var hair = Rgb(70, 160, 206);
        var fern = Rgb(86, 150, 70);
        var cress = Rgb(124, 176, 84);
        var stone = Rgb(128, 130, 124);

        // A gown of running water flaring to the ground, streaked paler down its folds, with pebbles at its hem
        b.Limb(Root, V(0, 0.17f, 0), V(0, 0.5f, 0), 0.15f, 0.065f, C6Water);
        b.Ell(Root, V(0, 0.08f, 0.0f), V(0.2f, 0.08f, 0.185f), C6Water);
        b.Ell(Root, V(0, 0.03f, 0.0f), V(0.24f, 0.03f, 0.22f), C6Water);
        foreach (float a in new[] { -50f, -20f, 15f, 45f, 160f, 200f })
        {
            float rad = a * Degree;
            var dir = V(MathF.Sin(rad), 0, MathF.Cos(rad));
            b.PaintEll(Root, dir * 0.15f + V(0, 0.22f, 0), V(0.016f, 0.15f, 0.016f), C6WaterPale, Euler(V(0, 1f, 0) - dir * 0.35f));
        }
        b.PaintEll(Root, V(0, 0.04f, 0), V(0.24f, 0.04f, 0.22f), C6WaterDeep);
        foreach (var (x, z, k) in new[] { (-0.2f, 0.14f, 0.05f), (0.21f, 0.1f, 0.045f), (0.12f, 0.2f, 0.035f), (-0.08f, -0.2f, 0.04f) })
            b.Ell(Root, V(x, 0.028f, z), V(k, k * 0.6f, k * 0.85f), stone, blend: 0.01f);

        // A slender body
        b.Ell(Body, V(0, 0.6f, 0), V(0.08f, 0.12f, 0.062f), C6Water);
        b.PaintEll(Body, V(0.02f, 0.64f, 0.05f), V(0.03f, 0.05f, 0.02f), C6WaterPale);
        b.Limb(Body, V(0, 0.68f, 0), V(0, 0.77f, 0.005f), 0.026f, 0.024f, C6Water);

        // Arms held out and down, a streamer of water running from each forearm to the ground
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.075f * s, 0.69f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.16f * s, 0.585f, 0.03f);
            var hand = V(0.235f * s, 0.49f, 0.06f);
            b.Limb(arm, shoulder, elbow, 0.024f, 0.02f, C6Water);
            b.Limb(arm, elbow, hand, 0.02f, 0.016f, C6Water);
            b.Ell(arm, hand + V(0.012f * s, -0.018f, 0.005f), V(0.016f, 0.026f, 0.012f), C6Water, V(0, 0, 20f * s));
            Ribbon(b, arm, Smooth(3, elbow + V(0.005f * s, -0.01f, -0.02f), V(0.22f * s, 0.42f, -0.05f), V(0.27f * s, 0.26f, -0.06f), V(0.25f * s, 0.12f, -0.03f)),
                0.032f, C6Water, V(0, 0, 1f), 0.012f);
        });

        // Her head: long water hair framing a calm face, a crown of fern and watercress
        int head = b.Head(V(0, 0.77f, 0.005f));
        var c = V(0, 0.84f, 0.01f);
        var r = V(0.066f, 0.078f, 0.066f);
        b.Ell(head, c, r, C6Water);
        b.Ell(head, c + V(0, 0.02f, -0.025f), V(0.076f, 0.08f, 0.068f), hair);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, c + V(0.06f * s, 0.03f, -0.0f), c + V(0.08f * s, -0.06f, -0.01f), c + V(0.085f * s, -0.15f, -0.03f), c + V(0.07f * s, -0.22f, -0.04f)),
                0.024f, 0.012f, hair);
            b.Tube(head, Smooth(3, c + V(0.035f * s, 0.03f, -0.07f), c + V(0.07f * s, -0.1f, -0.09f), c + V(0.08f * s, -0.26f, -0.1f), c + V(0.06f * s, -0.38f, -0.08f)),
                0.03f, 0.014f, hair);
        });
        var crown = c + V(0, 0.07f, -0.02f);
        for (int i = 0; i < 9; i++)
        {
            float a = (i / 9f) * MathF.Tau + 0.2f;
            var rim = crown + V(MathF.Sin(a) * 0.06f, 0, MathF.Cos(a) * 0.055f);
            var outward = Vector3.Normalize(V(MathF.Sin(a), 0, MathF.Cos(a)));
            if (i % 2 == 0)
                Frond(b, head, rim, rim + outward * 0.05f + V(0, 0.07f, 0), 0.02f, fern, V(0, 0.4f, 0) - outward, 0.25f, Leaf);
            b.Ell(head, rim + V(0, 0.012f, 0), V(0.022f, 0.012f, 0.022f), cress, mat: Leaf, blend: 0.01f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.027f * s, 0.845f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(120, 200, 240));
        });
        b.Mark(head, On(c, r, 0, 0.81f), V(0, -0.2f, 1f), 0.014f, 0.006f, C6WaterDeep, MarkShape.Smile);
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Nametel line

    private static readonly Color C6Snow = Rgb(232, 238, 246);
    private static readonly Color C6SnowShade = Rgb(184, 204, 226);

    /// <summary>Nametel: a round puff of powdery snow with two twig arms, coal eyes and pink cheeks.</summary>
    private static PokeBuilder Nametel()
    {
        var b = new PokeBuilder("Nametel", 0.52f, BodyPlan.Floating, V(0, 0.25f, 0)) { Coat = Fur };
        var twig = Rgb(110, 76, 52);
        var c = V(0, 0.25f, 0);
        var r = V(0.21f, 0.2f, 0.19f);
        b.Ell(Body, c, r, C6Snow);
        C6Lobes(b, Body, c, r, 26, 0.07f, C6Snow, -0.75f, 0.55f);
        b.PaintEll(Body, c + V(0, -0.17f, 0), V(0.3f, 0.1f, 0.28f), C6SnowShade);
        // Twig arms, each forked at the end
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.22f, 0.02f));
            var tip = V(0.36f * s, 0.16f, 0.05f);
            b.Tube(arm, Smooth(3, V(0.15f * s, 0.22f, 0.02f), V(0.26f * s, 0.19f, 0.04f), tip), 0.014f, 0.009f, twig, Fur);
            var fork = V(0.31f * s, 0.175f, 0.045f);
            b.Limb(arm, fork, V(0.36f * s, 0.215f, 0.06f), 0.009f, 0.007f, twig, Fur);
            b.Limb(arm, tip, V(0.4f * s, 0.14f, 0.07f), 0.009f, 0.007f, twig, Fur);
        });
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.065f * s, 0.29f);
            b.Eye(Body, at, Outward(c, r, at), 0.022f, Rgb(70, 66, 70));
            var cheek = On(c, r, 0.11f * s, 0.235f);
            b.Mark(Body, cheek, Outward(c, r, cheek), 0.026f, 0.016f, Rgb(244, 168, 184));
        });
        b.Mark(Body, On(c, r, 0, 0.215f), V(0, -0.1f, 1f), 0.025f, 0.01f, Rgb(140, 160, 190), MarkShape.Smile);
        b.Ground();
        return b;
    }

    /// <summary>Nameton: a great snowdrift heaped over a grey rocky body, its arms heavy drifts with dark claws, icicles hanging from its brow and chin.</summary>
    private static PokeBuilder Nameton()
    {
        var b = new PokeBuilder("Nameton", 0.98f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Fur };
        var rock = Rgb(126, 130, 134);
        var earth = Rgb(104, 90, 78);
        var claw = Rgb(78, 66, 56);
        var ice = Rgb(170, 214, 236);

        // Short rocky legs with snow on the knees
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.13f * s, 0.3f, 0));
            b.Limb(leg, V(0.13f * s, 0.3f, 0), V(0.15f * s, 0.08f, 0.02f), 0.095f, 0.085f, rock, Shell);
            b.Ell(leg, V(0.15f * s, 0.05f, 0.05f), V(0.105f, 0.05f, 0.12f), rock, mat: Shell);
            b.Ell(leg, V(0.15f * s, 0.18f, 0.06f), V(0.085f, 0.045f, 0.07f), C6Snow);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V((0.15f + 0.05f * t) * s, 0.03f, 0.13f), V((0.15f + 0.06f * t) * s, 0.012f, 0.19f), 0.024f, claw, mat: Shell);
        });
        // A grey rocky body banded with earth
        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc, V(0.25f, 0.24f, 0.2f), rock, mat: Shell);
        Rubble(b, Body, bc, V(0.22f, 0.21f, 0.17f), 0.07f, rock, 10, 10104u, 0.4f);
        b.PaintEll(Body, V(0, 0.36f, 0), V(0.27f, 0.05f, 0.22f), earth);
        // The drift piled over its shoulders and back, rock spurs breaking through
        var mc = V(0, 0.72f, -0.04f);
        var mr = V(0.33f, 0.16f, 0.25f);
        b.Ell(Body, mc, mr, C6Snow);
        C6Lobes(b, Body, mc, mr, 22, 0.085f, C6Snow, -0.3f, 0.8f);
        PokeBuilder.Both(s =>
        {
            b.Spike(Body, V(0.17f * s, 0.8f, -0.14f), V(0.24f * s, 0.98f, -0.2f), 0.06f, rock, 0.6f, Shell);
            b.Spike(Body, V(0.1f * s, 0.82f, -0.2f), V(0.12f * s, 0.95f, -0.28f), 0.045f, rock, 0.6f, Shell);
        });

        // Heavy arms of packed snow, dark claws at their ends
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.27f * s, 0.62f, 0));
            b.Limb(arm, V(0.27f * s, 0.62f, 0), V(0.38f * s, 0.44f, 0.05f), 0.085f, 0.08f, rock, Shell);
            var fc = V(0.42f * s, 0.32f, 0.08f);
            var fr = V(0.13f, 0.17f, 0.13f);
            b.Ell(arm, fc, fr, C6Snow);
            C6Lobes(b, arm, fc, fr, 12, 0.06f, C6Snow, -0.6f, 1f);
            b.PaintEll(arm, fc + V(0, -0.12f, 0), V(0.16f, 0.06f, 0.16f), C6SnowShade);
            foreach (float t in new[] { -1f, 0f, 1f })
            {
                var root = fc + V(0.06f * t * s, -0.14f, 0.07f - 0.02f * MathF.Abs(t));
                b.Spike(arm, root, root + V(0.02f * t * s, -0.11f, 0.06f), 0.028f, claw, mat: Shell);
            }
        });

        // A snow face under a jutting brow, icicles hanging from brow and chin, frost in its open mouth
        int head = b.Head(V(0, 0.66f, 0.12f));
        var c = V(0, 0.66f, 0.2f);
        var r = V(0.13f, 0.11f, 0.11f);
        b.Ell(head, c, r, C6Snow);
        b.Ell(head, c + V(0, 0.085f, 0.04f), V(0.15f, 0.045f, 0.075f), C6Snow, V(-10f, 0, 0));
        b.Ell(head, c + V(0, -0.075f, 0.035f), V(0.11f, 0.05f, 0.08f), C6Snow);
        foreach (float t in new[] { -2f, -1f, 0f, 1f, 2f })
        {
            var brow = c + V(0.055f * t, 0.055f, 0.09f - 0.01f * t * t);
            b.Spike(head, brow, brow + V(0, -0.05f - 0.012f * (2 - MathF.Abs(t)), 0.01f), 0.014f, ice, mat: Shell);
            var chin = c + V(0.04f * t, -0.11f, 0.085f - 0.008f * t * t);
            b.Spike(head, chin, chin + V(0, -0.07f - 0.02f * (2 - MathF.Abs(t)), 0.0f), 0.016f, ice, mat: Shell);
        }
        b.Mark(head, On(c, r, 0, 0.615f), V(0, -0.1f, 1f), 0.05f, 0.028f, Rgb(70, 86, 112));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.052f * s, 0.672f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(150, 210, 240), glare: true);
            var cheek = On(c, r, 0.09f * s, 0.64f);
            b.Mark(head, cheek, Outward(c, r, cheek), 0.02f, 0.012f, Rgb(232, 160, 170));
        });
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Zaganul

    /// <summary>Zaganul: a bearded vulture of the crags, rust-breasted with dark wings half spread, a pale shaggy head, a black mask and beard, perched on a bone.</summary>
    private static PokeBuilder Zaganul()
    {
        var b = new PokeBuilder("Zaganul", 0.88f, BodyPlan.Bird, V(0, 0.34f, 0)) { Coat = Fur };
        var rust = Rgb(178, 84, 48);
        var wing = Rgb(62, 66, 64);
        var wingDark = Rgb(40, 42, 42);
        var cream = Rgb(238, 226, 200);
        var black = Rgb(34, 32, 36);
        var beak = Rgb(128, 124, 120);
        var bone = Rgb(222, 212, 186);
        var slate = Rgb(110, 108, 102);
        var feet = Rgb(150, 86, 66);

        // The bone it carries, under its talons
        b.Limb(Root, V(-0.2f, 0.03f, 0.08f), V(0.22f, 0.03f, 0.02f), 0.028f, 0.026f, bone, Shell);
        foreach (var end in new[] { V(-0.2f, 0.03f, 0.08f), V(0.22f, 0.03f, 0.02f) })
            PokeBuilder.Both(k => b.Ell(Root, end + V(0, 0.005f, 0.022f * k), V(0.028f, 0.03f, 0.026f), bone, mat: Shell, blend: 0.01f));
        BirdLegs(b, 0.06f, 0.2f, 0.03f, 0.022f, feet);

        // A rust breast, the back dark, and a ruff of pale feathers round the neck
        var bc = V(0, 0.34f, 0);
        b.Ell(Body, bc, V(0.12f, 0.17f, 0.11f), rust, V(12f, 0, 0));
        b.PaintEll(Body, bc + V(0, 0.04f, -0.09f), V(0.14f, 0.2f, 0.07f), wing);
        b.Ell(Body, bc + V(0, 0.15f, 0.03f), V(0.085f, 0.07f, 0.08f), cream);
        FurTufts(b, Body, bc + V(0, 0.15f, 0.03f), V(0.085f, 0.07f, 0.08f), 14, 0.03f, 0.014f, cream, 1.1f, -0.6f, 0.4f);

        // Long dark wings held half open, plates of slate on their shoulders
        PokeBuilder.Both(s =>
        {
            int w = SpreadWing(b, s, bc + V(0.09f * s, 0.1f, -0.03f), bc + V(0.27f * s, 0.2f, -0.09f), 8, -5f, -95f, 0.34f, 0.03f, wing, wingDark, 0.35f, true, 5);
            foreach (var (x, y, k) in new[] { (0.12f, 0.12f, 0.035f), (0.17f, 0.15f, 0.03f), (0.15f, 0.08f, 0.028f) })
                b.Ell(w, bc + V(x * s, y, -0.04f), V(k, k * 0.7f, k), slate, V(20f, 0, 30f * s), Shell, 0.01f);
        });
        int tail = b.Tail(bc + V(0, -0.12f, -0.09f));
        TailFan(b, tail, bc + V(0, -0.12f, -0.09f), 5, 36f, 0.22f, 0.03f, wing, wingDark, 1.3f);

        // The head: pale and shaggy, a black mask through red-ringed eyes, a hooked grey beak with a black beard under it
        int head = b.Head(bc + V(0, 0.18f, 0.04f));
        var c = bc + V(0, 0.26f, 0.06f);
        var r = V(0.075f, 0.068f, 0.078f);
        b.Ell(head, c, r, cream);
        FurTufts(b, head, c, r, 12, 0.035f, 0.012f, cream, 0.55f, -0.1f, 0.15f);
        b.PaintEll(head, c + V(0, 0.012f, 0.03f), V(0.075f, 0.013f, 0.06f), black, V(-8f, 0, 0), 0.008f);
        b.Tube(head, Smooth(3, c + V(0, -0.005f, 0.05f), c + V(0, -0.005f, 0.1f), c + V(0, -0.025f, 0.135f), c + V(0, -0.055f, 0.13f)), 0.018f, 0.007f, beak, Shell, 0.01f);
        foreach (float t in new[] { -1f, 0f, 1f })
            b.Spike(head, c + V(0.012f * t, -0.035f, 0.08f), c + V(0.02f * t, -0.11f, 0.085f), 0.011f, black, mat: Fur, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.042f * s, c.Y + 0.014f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, white: Rgb(204, 56, 44), pupil: black);
        });
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Cremenel line

    private static readonly Color C6Flint = Rgb(62, 62, 68);
    private static readonly Color C6FlintLight = Rgb(88, 90, 98);
    private static readonly Color C6Ember = Rgb(248, 142, 44);

    /// <summary>Cremenel: a knapped pebble of grey-black flint on stubby feet, two orange eyes glowing in a dark hollow, sparks on its edges.</summary>
    private static PokeBuilder Cremenel()
    {
        var b = new PokeBuilder("Cremenel", 0.44f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Shell };
        var c = V(0, 0.21f, 0);
        var r = V(0.16f, 0.15f, 0.15f);
        b.Ell(Body, c, r, C6Flint);
        // Chipped facets all over, the face left round
        C6Facets(b, Body, c, r, new[]
        {
            V(0.7f, 0.7f, 0.1f), V(-0.7f, 0.7f, -0.2f), V(0, 1f, -0.3f), V(0.9f, 0.1f, -0.4f), V(-0.9f, -0.1f, -0.3f), V(0.3f, 0.5f, -0.8f),
            V(-0.4f, 0.2f, -0.9f), V(0.6f, -0.4f, -0.6f), V(0.8f, 0.3f, 0.5f), V(-0.8f, 0.35f, 0.45f)
        }, 0.88f);
        foreach (var at in new[] { V(0.08f, 0.33f, 0.02f), V(-0.12f, 0.27f, -0.06f), V(0.1f, 0.18f, -0.12f) })
            b.PaintEll(Body, at, V(0.05f, 0.04f, 0.05f), C6FlintLight);
        // The dark hollow its eyes glow from
        b.PaintEll(Body, c + V(0, 0.015f, 0.13f), V(0.1f, 0.035f, 0.05f), Rgb(30, 30, 34), soft: 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, c.Y + 0.015f);
            b.Eye(Body, at, Outward(c, r, at), 0.02f, white: C6Ember, pupil: Rgb(255, 214, 120));
        });
        // Sparks caught on its edges
        foreach (var (x, y, z) in new[] { (0.15f, 0.25f, 0.02f), (-0.15f, 0.17f, 0.03f), (0.12f, 0.12f, 0.08f), (-0.1f, 0.3f, 0.05f) })
            b.Ell(Body, V(x, y, z), V(0.014f, 0.014f, 0.014f), C6Ember, mat: Glow, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.1f, 0.01f));
            b.Limb(leg, V(0.07f * s, 0.1f, 0.01f), V(0.075f * s, 0.035f, 0.025f), 0.032f, 0.03f, C6Flint);
            b.Box(leg, V(0.076f * s, 0.02f, 0.035f), V(0.028f, 0.02f, 0.034f), 0.012f, C6Flint, V(0, 8f * s, 0));
        });
        b.Ground();
        return b;
    }

    /// <summary>Cremenor: a hulking creature of knapped flint plates with razor edges, orange fire glowing in the cracks between them, its fists clenched.</summary>
    private static PokeBuilder Cremenor()
    {
        var b = new PokeBuilder("Cremenor", 0.88f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Shell };
        var flint = C6Flint;
        var light = C6FlintLight;

        void Crack(int bone, Vector3 at, Vector3 dir, float length) =>
            b.PaintEll(bone, at, V(0.011f, length, 0.03f), C6Ember, Euler(dir), 0.006f);

        // A broad torso of plates: two slabs of chest, a plated belly, a hump of plates behind the head
        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc, V(0.22f, 0.2f, 0.16f), flint);
        PokeBuilder.Both(s => b.Box(Body, V(0.085f * s, 0.56f, 0.12f), V(0.085f, 0.07f, 0.04f), 0.02f, flint, V(-8f, 10f * s, 6f * s)));
        b.Box(Body, V(0, 0.4f, 0.12f), V(0.1f, 0.06f, 0.035f), 0.02f, flint, V(6f, 0, 0));
        b.Ell(Body, V(0, 0.66f, -0.07f), V(0.2f, 0.12f, 0.12f), flint);
        PokeBuilder.Both(s => b.Spike(Body, V(0.08f * s, 0.7f, -0.08f), V(0.1f * s, 0.82f, -0.16f), 0.06f, light, 0.45f));
        Crack(Body, V(0, 0.5f, 0.165f), V(0, 1f, 0), 0.11f);
        PokeBuilder.Both(s =>
        {
            Crack(Body, V(0.08f * s, 0.48f, 0.155f), V(0.9f * s, 0.5f, 0), 0.06f);
            Crack(Body, V(0.1f * s, 0.6f, 0.14f), V(-0.6f * s, 1f, 0), 0.05f);
            Crack(Body, V(0.18f * s, 0.5f, 0.06f), V(0.2f * s, 1f, 0), 0.07f);
        });

        // Heavy legs of plates
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.32f, 0));
            b.Limb(leg, V(0.11f * s, 0.32f, 0), V(0.14f * s, 0.08f, 0.02f), 0.085f, 0.075f, flint);
            b.Box(leg, V(0.14f * s, 0.18f, 0.06f), V(0.065f, 0.05f, 0.035f), 0.015f, light, V(-10f, 0, 0));
            b.Box(leg, V(0.145f * s, 0.035f, 0.06f), V(0.075f, 0.035f, 0.095f), 0.015f, flint);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V((0.145f + 0.045f * t) * s, 0.025f, 0.14f), V((0.145f + 0.05f * t) * s, 0.012f, 0.19f), 0.022f, light);
            Crack(leg, V(0.14f * s, 0.25f, 0.08f), V(0.3f * s, 1f, 0.1f), 0.05f);
        });

        // Great plated arms: a slab on each shoulder, a blade of flint down each forearm, clenched fists
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.24f * s, 0.62f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.33f * s, 0.43f, 0.04f);
            var wrist = V(0.35f * s, 0.25f, 0.09f);
            b.Limb(arm, shoulder, elbow, 0.075f, 0.07f, flint);
            b.Limb(arm, elbow, wrist, 0.075f, 0.085f, flint);
            b.Box(arm, V(0.28f * s, 0.68f, 0), V(0.11f, 0.075f, 0.11f), 0.02f, light, V(10f, 0, 28f * s));
            Blade(b, arm, V(0.38f * s, 0.42f, 0.03f), V(0.42f * s, 0.24f, 0.02f), 0.05f, light, V(s, 0, 0), 0.35f);
            b.Box(arm, V(0.355f * s, 0.19f, 0.11f), V(0.075f, 0.07f, 0.075f), 0.025f, flint, V(0, 15f * s, 0));
            Crack(arm, V(0.35f * s, 0.33f, 0.13f), V(0.2f * s, 1f, 0.2f), 0.06f);
            Crack(arm, V(0.29f * s, 0.55f, 0.07f), V(0.6f * s, 1f, 0), 0.05f);
        });

        // A small head sunk between its shoulders, a heavy brow over glowing orange eyes
        int head = b.Head(V(0, 0.66f, 0.08f));
        var c = V(0, 0.7f, 0.13f);
        var r = V(0.1f, 0.085f, 0.09f);
        b.Ell(head, c, r, flint);
        b.Box(head, c + V(0, 0.045f, 0.06f), V(0.1f, 0.022f, 0.035f), 0.012f, light, V(-15f, 0, 0));
        b.Box(head, c + V(0, -0.055f, 0.05f), V(0.07f, 0.03f, 0.04f), 0.012f, flint);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, white: C6Ember, pupil: Rgb(255, 214, 120), glare: true);
        });
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Aurel line

    private static readonly Color C6Gold = Rgb(224, 178, 58);
    private static readonly Color C6GoldLight = Rgb(246, 214, 112);

    /// <summary>Aurel: a lumpy little gold nugget with big shy eyes, its hands at its chin, peeking from beside a grey pebble, drops of stream water on it.</summary>
    private static PokeBuilder Aurel()
    {
        var b = new PokeBuilder("Aurel", 0.44f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Metal };
        var stone = Rgb(112, 114, 112);
        var drop = Rgb(214, 232, 242);
        // The pebble it hides behind
        b.Ell(Root, V(-0.11f, 0.065f, -0.08f), V(0.12f, 0.07f, 0.09f), stone, V(0, 20f, 8f), Shell);
        var c = V(0, 0.21f, 0);
        var r = V(0.13f, 0.16f, 0.12f);
        b.Ell(Body, c, r, C6Gold, V(0, 0, -6f));
        Rubble(b, Body, c, V(0.12f, 0.14f, 0.11f), 0.05f, C6Gold, 12, 10108u, 0.45f);
        b.PaintEll(Body, c + V(0.05f, 0.11f, 0.04f), V(0.05f, 0.04f, 0.05f), C6GoldLight);
        // Drops of the stream it was found in
        foreach (var (x, y) in new[] { (0.09f, 0.3f), (-0.1f, 0.2f), (0.11f, 0.14f) })
        {
            var at = On(c, r, x, y);
            b.Ell(Body, at - Outward(c, r, at) * 0.004f, V(0.01f, 0.014f, 0.01f), drop, mat: Shell, blend: 0.004f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.05f * s, 0.235f);
            b.Eye(Body, at, Outward(c, r, at), 0.032f, Rgb(96, 66, 34));
        });
        // Little hands at its chin, two stubby feet
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.15f, 0.07f));
            b.Ell(arm, V(0.055f * s, 0.14f, 0.115f), V(0.035f, 0.03f, 0.03f), C6Gold);
            int leg = b.Leg(s, V(0.06f * s, 0.08f, 0.02f));
            b.Ell(leg, V(0.065f * s, 0.04f, 0.04f), V(0.036f, 0.045f, 0.036f), C6Gold);
        });
        b.Ground();
        return b;
    }

    /// <summary>Aurifer: a heavy guardian of pale quartz veined with gold, gold crystals jutting from its shoulders, back and forearms, slate bands on its limbs, amber eyes deep under its brow.</summary>
    private static PokeBuilder Aurifer()
    {
        var b = new PokeBuilder("Aurifer", 0.94f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Shell };
        var quartz = Rgb(234, 226, 204);
        var shade = Rgb(208, 196, 166);
        var slate = Rgb(96, 98, 104);

        void Vein(int bone, Vector3 at, Vector3 dir, float length) =>
            b.PaintEll(bone, at, V(0.012f, length, 0.03f), C6Gold, Euler(dir), 0.006f);
        void Crystal(int bone, Vector3 root, Vector3 tip, float width) => b.Spike(bone, root, tip, width, C6Gold, 0.75f, Metal);

        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc, V(0.24f, 0.21f, 0.17f), quartz);
        PokeBuilder.Both(s => b.Box(Body, V(0.09f * s, 0.55f, 0.12f), V(0.09f, 0.075f, 0.04f), 0.02f, quartz, V(-8f, 10f * s, 6f * s)));
        b.Box(Body, V(0, 0.42f, 0.13f), V(0.1f, 0.05f, 0.03f), 0.02f, quartz);
        b.PaintEll(Body, V(0, 0.33f, 0), V(0.26f, 0.045f, 0.2f), slate);
        b.Ell(Body, V(0, 0.68f, -0.07f), V(0.22f, 0.13f, 0.13f), quartz);
        Crystal(Body, V(0.06f, 0.76f, -0.1f), V(0.1f, 0.93f, -0.18f), 0.04f);
        Crystal(Body, V(-0.07f, 0.75f, -0.09f), V(-0.1f, 0.88f, -0.16f), 0.035f);
        Crystal(Body, V(0f, 0.74f, -0.15f), V(0.0f, 0.86f, -0.24f), 0.03f);
        Vein(Body, V(0, 0.5f, 0.165f), V(0, 1f, 0), 0.1f);
        PokeBuilder.Both(s =>
        {
            Vein(Body, V(0.1f * s, 0.47f, 0.15f), V(1f * s, 0.4f, 0), 0.06f);
            Vein(Body, V(0.19f * s, 0.55f, 0.06f), V(-0.2f * s, 1f, 0), 0.06f);
            Vein(Body, V(0.08f * s, 0.62f, 0.12f), V(0.8f * s, -0.5f, 0), 0.05f);
        });

        // Legs of quartz, a slate plate on each knee
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.32f, 0));
            b.Limb(leg, V(0.12f * s, 0.32f, 0), V(0.145f * s, 0.08f, 0.02f), 0.09f, 0.08f, quartz);
            b.Ell(leg, V(0.145f * s, 0.19f, 0.075f), V(0.05f, 0.045f, 0.03f), slate, blend: 0.006f);
            b.Box(leg, V(0.15f * s, 0.035f, 0.06f), V(0.08f, 0.035f, 0.1f), 0.015f, quartz);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Ell(leg, V((0.15f + 0.045f * t) * s, 0.03f, 0.15f), V(0.024f, 0.022f, 0.03f), shade);
            Vein(leg, V(0.145f * s, 0.26f, 0.08f), V(-0.3f * s, 1f, 0.1f), 0.05f);
        });

        // Arms thick as mine timbers: slate pauldrons crowned with crystals, slate bands at the wrists, gold fists
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.25f * s, 0.62f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.34f * s, 0.43f, 0.03f);
            var wrist = V(0.37f * s, 0.26f, 0.08f);
            b.Limb(arm, shoulder, elbow, 0.08f, 0.075f, quartz);
            b.Limb(arm, elbow, wrist, 0.08f, 0.085f, quartz);
            b.Ell(arm, V(0.29f * s, 0.68f, 0), V(0.12f, 0.085f, 0.11f), slate, V(0, 0, 20f * s));
            Crystal(arm, V(0.3f * s, 0.74f, -0.01f), V(0.36f * s, 0.9f, -0.04f), 0.035f);
            Crystal(arm, V(0.36f * s, 0.71f, 0.0f), V(0.45f * s, 0.82f, -0.02f), 0.03f);
            Crystal(arm, V(0.25f * s, 0.75f, -0.03f), V(0.27f * s, 0.86f, -0.07f), 0.025f);
            Crystal(arm, V(0.39f * s, 0.4f, -0.02f), V(0.46f * s, 0.5f, -0.07f), 0.03f);
            b.Ell(arm, V(0.36f * s, 0.29f, 0.07f), V(0.1f, 0.04f, 0.1f), slate, blend: 0.006f);
            b.Box(arm, V(0.375f * s, 0.2f, 0.1f), V(0.075f, 0.07f, 0.075f), 0.025f, C6Gold, V(0, 15f * s, 0), Metal);
            Vein(arm, V(0.33f * s, 0.45f, 0.1f), V(0.2f * s, 1f, 0.2f), 0.06f);
        });

        // A heavy stone brow over small amber eyes
        int head = b.Head(V(0, 0.66f, 0.09f));
        var c = V(0, 0.69f, 0.14f);
        var r = V(0.11f, 0.09f, 0.09f);
        b.Ell(head, c, r, quartz);
        b.Box(head, c + V(0, 0.05f, 0.055f), V(0.11f, 0.025f, 0.04f), 0.012f, quartz, V(-15f, 0, 0));
        b.Box(head, c + V(0, -0.055f, 0.05f), V(0.075f, 0.03f, 0.04f), 0.012f, shade);
        Vein(head, c + V(0.05f, 0.03f, 0.07f), V(1f, 1f, 0), 0.03f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.042f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(232, 150, 40), glare: true);
        });
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Norel line

    private static readonly Color C6Cloud = Rgb(116, 122, 140);
    private static readonly Color C6CloudDark = Rgb(80, 84, 104);
    private static readonly Color C6Spark = Rgb(252, 222, 80);

    /// <summary>Norel: a small cross storm cloud, its edges bristling with static, glowing yellow eyes and sparks under it.</summary>
    private static PokeBuilder Norel()
    {
        var b = new PokeBuilder("Norel", 0.5f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Fur }.Hover();
        var c = V(0, 0.3f, 0);
        var r = V(0.17f, 0.13f, 0.14f);
        b.Ell(Body, c, r, C6Cloud);
        C6Lobes(b, Body, c, r, 20, 0.068f, C6Cloud, -0.8f, 0.55f);
        b.PaintEll(Body, c + V(0, -0.12f, 0), V(0.24f, 0.07f, 0.2f), C6CloudDark);
        // Static standing its edges on end
        foreach (var d in new[] { V(1f, 0.4f, 0), V(-1f, 0.5f, -0.1f), V(0.5f, 0.9f, -0.2f), V(-0.4f, 0.95f, 0.1f), V(0.9f, -0.5f, 0.1f), V(-0.85f, -0.4f, 0.2f) })
        {
            var n = Vector3.Normalize(d);
            var root = c + n * r * 1.05f;
            b.Spike(Body, root, root + n * 0.08f, 0.024f, C6CloudDark, 0.55f);
        }
        foreach (float x in new[] { -0.07f, 0.02f, 0.09f })
            b.Tube(Body, C6Bolt(c + V(x, -0.1f, 0.04f), c + V(x + 0.01f, -0.155f, 0.05f), V(0.01f, 0, 0), 2), 0.009f, 0.008f, C6Spark, Glow, 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.055f * s, 0.315f);
            b.Eye(Body, at, Outward(c, r, at), 0.024f, white: C6Spark, pupil: Rgb(255, 244, 180), glare: true);
        });
        b.Mark(Body, On(c, r, 0, 0.26f), V(0, -0.2f, 1f), 0.022f, 0.01f, C6CloudDark, MarkShape.Smile, 180f);
        return Lift(b);
    }

    /// <summary>Tunetor: a great thundercloud, dark and billowing, its wings sheets of falling rain, lightning glowing in its belly and forking from its edges.</summary>
    private static PokeBuilder Tunetor()
    {
        var b = new PokeBuilder("Tunetor", 0.96f, BodyPlan.Bird, V(0, 0.52f, 0)) { Coat = Fur }.Hover();
        var dark = Rgb(62, 66, 90);
        var mid = Rgb(92, 98, 124);
        var rain = Rgb(104, 132, 168);
        var glow = Rgb(252, 228, 112);

        var c = V(0, 0.5f, 0);
        var r = V(0.22f, 0.19f, 0.18f);
        b.Ell(Body, c, r, dark);
        C6Lobes(b, Body, c, r, 22, 0.085f, dark, -0.5f, 0.75f);
        // The lightning in its belly
        b.Ell(Body, c + V(0, -0.17f, 0.04f), V(0.16f, 0.1f, 0.13f), glow, mat: Glow);
        foreach (float a in new[] { -0.7f, 0f, 0.7f, 2.4f, 3.14f, 3.9f })
            b.Ell(Body, c + V(MathF.Sin(a) * 0.16f, -0.12f, MathF.Cos(a) * 0.13f + 0.04f), V(0.06f, 0.05f, 0.06f), mid);
        foreach (float x in new[] { -0.08f, 0.03f, 0.1f })
            b.Tube(Body, C6Bolt(c + V(x, -0.2f, 0.09f), c + V(x + 0.02f, -0.36f, 0.08f), V(0.02f, 0, 0.005f), 4), 0.012f, 0.009f, glow, Glow, 0.004f);
        int tail = b.Tail(c + V(0, -0.2f, -0.06f));
        b.Tube(tail, Smooth(3, c + V(0, -0.18f, -0.08f), c + V(0.03f, -0.3f, -0.12f), c + V(0.1f, -0.33f, -0.1f), c + V(0.12f, -0.28f, -0.06f)), 0.05f, 0.02f, dark);

        // Wings: billows of cloud along an arm, sheets of rain falling from them, lightning down their edges
        PokeBuilder.Both(s =>
        {
            var shoulder = c + V(0.18f * s, 0.08f, -0.03f);
            var tip = c + V(0.52f * s, 0.16f, -0.1f);
            int wing = b.Wing(s, shoulder);
            b.Limb(wing, shoulder, tip, 0.08f, 0.06f, dark);
            for (int i = 0; i < 4; i++)
            {
                var at = Vector3.Lerp(shoulder, tip, 0.2f + 0.27f * i) + V(0, 0.04f, 0);
                b.Ell(wing, at, V(0.075f, 0.065f, 0.07f), i % 2 == 0 ? dark : mid);
            }
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                var root = Vector3.Lerp(shoulder + V(0.04f * s, -0.04f, 0), tip + V(0, -0.03f, 0), t);
                var end = root + V(0.05f * s * t, -0.32f - 0.1f * MathF.Sin(MathF.PI * t) - 0.03f * (i % 2), -0.03f);
                Blade(b, wing, root, end, 0.034f, rain, V(0, 0, 1f), 0.3f, Shell);
            }
            b.Tube(wing, C6Bolt(tip + V(0, -0.02f, 0.02f), tip + V(0.04f * s, -0.36f, 0.02f), V(0.025f * s, 0, 0), 4), 0.012f, 0.009f, glow, Glow, 0.004f);
        });

        // A fierce face in the cloud, static bristling over it
        int head = b.Head(c + V(0, 0.1f, 0.08f));
        var hc = c + V(0, 0.15f, 0.12f);
        var hr = V(0.14f, 0.12f, 0.11f);
        b.Ell(head, hc, hr, dark);
        C6Lobes(b, head, hc, hr, 12, 0.06f, dark, -0.2f, 0.55f);
        foreach (var d in new[] { V(0.3f, 1f, -0.2f), V(-0.4f, 1f, -0.1f), V(0.05f, 1f, -0.5f) })
        {
            var n = Vector3.Normalize(d);
            b.Spike(head, hc + n * hr * 1.05f, hc + n * hr * 1.05f + n * 0.12f, 0.03f, dark, 0.5f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.055f * s, hc.Y + 0.01f);
            b.Eye(head, at, Outward(hc, hr, at), 0.026f, white: Rgb(255, 246, 196), pupil: Rgb(255, 255, 236), glare: true);
        });
        b.Mark(head, On(hc, hr, 0, hc.Y - 0.05f), V(0, -0.2f, 1f), 0.026f, 0.012f, Rgb(36, 38, 56), MarkShape.Zigzag);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Grindinel

    /// <summary>Grindinel: a cluster of white hailstones bound by crackling blue sparks, two glowing eyes in its middle stone, smaller stones orbiting it on a ring of static.</summary>
    private static PokeBuilder Grindinel()
    {
        var b = new PokeBuilder("Grindinel", 0.56f, BodyPlan.Floating, V(0, 0.32f, 0)) { Coat = Shell }.Hover();
        var hail = Rgb(222, 232, 244);
        var shade = Rgb(186, 204, 226);
        var spark = Rgb(120, 222, 250);
        var c = V(0, 0.32f, 0.03f);
        var r = V(0.12f, 0.12f, 0.11f);
        b.Ell(Body, c, r, hail);
        // The stones packed round it
        var rng = new GenomeRandom(10112u);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f + 0.3f;
            float k = rng.Range(0.065f, 0.09f);
            var at = c + V(MathF.Cos(a) * 0.15f, MathF.Sin(a) * 0.15f, -0.045f + rng.Range(-0.02f, 0.02f));
            b.Ell(Body, at, V(k, k * 0.92f, k), i % 3 == 0 ? shade : hail, V(rng.Range(0f, 40f), rng.Range(0f, 60f), 0));
        }
        b.Ell(Body, c + V(0, 0, -0.1f), V(0.12f, 0.12f, 0.08f), shade);
        // Sparks running between the stones, and a ring of static with smaller stones on it
        foreach (float a in new[] { 0.9f, 2.4f, 4.0f, 5.5f })
        {
            var from = c + V(MathF.Cos(a) * 0.1f, MathF.Sin(a) * 0.1f, 0.07f);
            var to = c + V(MathF.Cos(a + 0.5f) * 0.19f, MathF.Sin(a + 0.5f) * 0.19f, 0.04f);
            b.Tube(Body, C6Bolt(from, to, V(0, 0, 0.012f), 3), 0.008f, 0.007f, spark, Glow, 0.003f);
        }
        int ring = b.Part("ring", Body, c, PokeRole.Segment);
        b.Torus(ring, c, 0.29f, 0.009f, spark, V(72f, 0, 18f), mat: Glow);
        var tilt = Quaternion.CreateFromYawPitchRoll(0, 72f * Degree, 18f * Degree);
        foreach (float a in new[] { 0.4f, 1.6f, 2.9f, 4.1f, 5.3f })
        {
            var at = c + Vector3.Transform(V(MathF.Cos(a) * 0.29f, 0, MathF.Sin(a) * 0.29f), tilt);
            b.Ell(ring, at, V(0.03f, 0.028f, 0.03f), hail, V(a * 20f, 0, 0), blend: 0.006f);
        }
        foreach (float a in new[] { 0.2f, 3.3f })
        {
            var outer = c + Vector3.Transform(V(MathF.Cos(a) * 0.29f, 0, MathF.Sin(a) * 0.29f), tilt);
            var inner = c + Vector3.Normalize(outer - c) * 0.17f;
            b.Tube(ring, C6Bolt(inner, outer, V(0, 0.012f, 0), 3), 0.008f, 0.007f, spark, Glow, 0.003f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.042f * s, 0.335f);
            b.Eye(Body, at, Outward(c, r, at), 0.024f, white: Rgb(150, 236, 255), pupil: Rgb(250, 240, 150));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Vantulet line

    private static readonly Color C6Air = Rgb(222, 236, 246);
    private static readonly Color C6AirLine = Rgb(150, 184, 214);
    private static readonly Color C6LeafGreen = Rgb(108, 168, 76);

    /// <summary>Vantulet: a little whirl of pale air, a spiral on its round head, two swirls for arms, a green leaf caught in its tail.</summary>
    private static PokeBuilder Vantulet()
    {
        var b = new PokeBuilder("Vantulet", 0.48f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var c = V(0, 0.42f, 0.02f);
        var r = V(0.13f, 0.11f, 0.12f);
        b.Ell(Body, c, r, C6Air);
        b.Tube(Body, C6Crown(c, r, 0.11f, 0.015f, 1.6f, 22, -0.006f), 0.012f, 0.01f, C6AirLine, blend: 0.006f);
        // The whirl winding down into a tail
        int tail = b.Tail(c + V(0, -0.08f, 0));
        var path = new Vector3[17];
        for (int i = 0; i <= 16; i++)
        {
            float t = i / 16f, a = t * 3.2f * MathF.PI;
            float rr = 0.09f * (1f - t) + 0.02f;
            path[i] = c + V(MathF.Cos(a) * rr + 0.04f * t, -0.06f - 0.3f * t, MathF.Sin(a) * rr - 0.02f);
        }
        b.Tube(tail, path, 0.05f, 0.012f, C6Air);
        b.PaintTorus(tail, c + V(0, -0.16f, -0.01f), 0.07f, 0.008f, C6AirLine, V(10f, 0, 8f));
        // The leaf caught where the whirl swings out to the right, its stalk inside the tube so it stays held
        var stalk = path[10];
        var leafTip = stalk + V(0.11f, -0.09f, 0.05f);
        Petal(b, tail, stalk, leafTip, 0.035f, C6LeafGreen, Leaf);
        b.PaintEll(tail, Vector3.Lerp(stalk, leafTip, 0.55f), V(0.004f, 0.045f, 0.02f), Rgb(76, 128, 56), Euler(leafTip - stalk), 0.004f);
        // Two swirls for arms
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, c + V(0.1f * s, -0.04f, 0.04f));
            var curl = Spiral(c + V(0.2f * s, -0.04f, 0.06f), V(s, 0, 0), V(0, 1f, 0), 0.05f, 0.012f, MathF.PI, MathF.PI + 1.6f * MathF.Tau * s, 16);
            var lead = new[] { c + V(0.09f * s, -0.05f, 0.05f), c + V(0.14f * s, -0.045f, 0.06f) };
            b.Tube(arm, lead, 0.022f, 0.018f, C6Air);
            b.Tube(arm, curl, 0.016f, 0.01f, C6Air);
        });
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.43f);
            b.Eye(Body, at, Outward(c, r, at), 0.016f, Rgb(70, 86, 110));
        });
        b.Mark(Body, On(c, r, 0, 0.385f), V(0, -0.2f, 1f), 0.008f, 0.011f, Rgb(70, 86, 110));
        return Lift(b);
    }

    /// <summary>Vantoroi: a tall howling whirlwind, a wide funnel of pale grey air with a spiral crown, streamers of cloud curling from its sides, leaves caught in them, narrow glowing eyes.</summary>
    private static PokeBuilder Vantoroi()
    {
        var b = new PokeBuilder("Vantoroi", 1f, BodyPlan.Floating, V(0, 0.55f, 0)) { Coat = Fur }.Hover();
        var grey = Rgb(178, 190, 204);
        var pale = Rgb(210, 222, 234);
        var dark = Rgb(124, 140, 160);
        // The funnel: bands of wind wound round a core, widening upward and leaning as it twists
        int tail = b.Tail(V(0, 0.42f, 0));
        b.Limb(tail, V(0.04f, 0.1f, 0), V(0, 0.45f, 0), 0.02f, 0.11f, grey);
        b.Limb(Body, V(0, 0.42f, 0), V(0, 0.78f, 0), 0.11f, 0.2f, grey);
        for (int i = 0; i < 11; i++)
        {
            float t = i / 10f, y = 0.12f + 0.64f * t;
            float ring = 0.025f + 0.19f * MathF.Pow(t, 1.2f);
            int bone = y < 0.45f ? tail : Body;
            b.Torus(bone, V(0.04f * (1f - t) + 0.012f * MathF.Sin(i * 1.7f), y, 0.01f * MathF.Cos(i * 1.3f)), ring, 0.02f + 0.022f * t,
                i % 2 == 0 ? pale : grey, V(8f * MathF.Sin(i), 0, 10f * MathF.Cos(i * 0.7f)));
        }
        b.Spike(tail, V(0.04f, 0.12f, 0), V(0.06f, 0.0f, 0.02f), 0.025f, grey);

        // The crown: a wide disc of air with a spiral on top, and the eyes under its rim
        int head = b.Head(V(0, 0.8f, 0));
        var c = V(0, 0.86f, 0);
        var r = V(0.27f, 0.085f, 0.25f);
        b.Ell(head, c, r, grey);
        b.Torus(head, c + V(0, -0.02f, 0), 0.25f, 0.035f, pale);
        b.Tube(head, C6Crown(c, r, 0.22f, 0.02f, 2.2f, 34, -0.004f), 0.016f, 0.012f, dark, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = c + V(0.075f * s, -0.04f, 0);
            at.Z = 0.255f;
            b.Eye(head, at, V(0.25f * s, -0.1f, 1f), 0.018f, white: Rgb(130, 224, 252), pupil: Rgb(230, 250, 255), glare: true);
        });

        // Streamers of cloud curling from its sides, leaves caught in them
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.7f, 0));
            var high = Smooth(3, V(0.15f * s, 0.72f, 0.02f), V(0.32f * s, 0.7f, 0.08f), V(0.45f * s, 0.6f, 0.08f), V(0.5f * s, 0.48f, 0.04f));
            b.Tube(arm, high, 0.04f, 0.024f, pale);
            b.Tube(arm, Spiral(V(0.46f * s, 0.46f, 0.04f), V(s, 0, 0), V(0, 1f, 0), 0.045f, 0.012f, 0f, -1.4f * MathF.Tau, 14), 0.024f, 0.012f, pale);
            var low = Smooth(3, V(0.12f * s, 0.56f, -0.02f), V(0.26f * s, 0.5f, -0.06f), V(0.36f * s, 0.38f, -0.06f), V(0.38f * s, 0.27f, -0.03f));
            b.Tube(arm, low, 0.035f, 0.02f, grey);
            b.Tube(arm, Spiral(V(0.345f * s, 0.25f, -0.03f), V(s, 0, 0), V(0, 1f, 0), 0.035f, 0.01f, 0f, -1.4f * MathF.Tau, 12), 0.02f, 0.01f, grey);
            Petal(b, arm, V(0.33f * s, 0.69f, 0.1f), V(0.4f * s, 0.78f, 0.12f), 0.035f, C6LeafGreen, Leaf);
            Petal(b, arm, V(0.36f * s, 0.36f, -0.04f), V(0.44f * s, 0.32f, 0.02f), 0.03f, C6LeafGreen, Leaf);
        });
        Petal(b, Body, V(0.13f, 0.55f, 0.12f), V(0.2f, 0.5f, 0.2f), 0.035f, C6LeafGreen, Leaf);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Morunel line

    /// <summary>Morunel: a grey sturgeon fry with a long flat snout, four whiskers under it, rows of little bony plates and a shark's tail.</summary>
    private static PokeBuilder Morunel()
    {
        var b = new PokeBuilder("Morunel", 0.52f, BodyPlan.Fish, V(0, 0.26f, -0.02f)) { Coat = Scales }.Hover();
        var grey = Rgb(124, 138, 150);
        var dark = Rgb(90, 102, 116);
        var pale = Rgb(204, 212, 218);
        var plate = Rgb(150, 162, 172);

        b.Limb(Body, V(0, 0.26f, 0.1f), V(0, 0.27f, -0.18f), 0.075f, 0.048f, grey);
        b.PaintEll(Body, V(0, 0.21f, -0.02f), V(0.06f, 0.03f, 0.18f), pale);
        b.PaintEll(Body, V(0, 0.33f, -0.03f), V(0.05f, 0.03f, 0.2f), dark);
        // A row of plates along its back and one along each side
        for (int i = 0; i < 5; i++)
        {
            var at = V(0, 0.335f - 0.012f * i, 0.07f - 0.055f * i);
            b.Spike(Body, at, at + V(0, 0.035f, -0.01f), 0.016f, plate, 0.6f);
        }
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 5; i++)
                b.Ell(Body, V((0.068f - 0.005f * i) * s, 0.265f, 0.06f - 0.052f * i), V(0.012f, 0.009f, 0.014f), plate, blend: 0.006f);
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, V(0.05f * s, 0.21f, 0.05f), PokeRole.Fin, s, s);
            Blade(b, fin, V(0.05f * s, 0.21f, 0.05f), V(0.12f * s, 0.16f, -0.01f), 0.03f, dark, V(0, 1f, 0), 0.3f);
            Blade(b, Body, V(0.035f * s, 0.21f, -0.12f), V(0.07f * s, 0.18f, -0.16f), 0.02f, dark, V(0, 1f, 0), 0.3f);
        });
        Blade(b, Body, V(0, 0.3f, -0.14f), V(0, 0.36f, -0.19f), 0.025f, dark, V(1f, 0, 0), 0.3f);

        // The shark's tail, its upper lobe longer
        int tail = b.Tail(V(0, 0.27f, -0.18f));
        b.Limb(tail, V(0, 0.27f, -0.18f), V(0, 0.29f, -0.26f), 0.045f, 0.028f, grey);
        Blade(b, tail, V(0, 0.29f, -0.25f), V(0, 0.42f, -0.35f), 0.04f, dark, V(1f, 0, 0), 0.28f);
        Blade(b, tail, V(0, 0.27f, -0.25f), V(0, 0.19f, -0.31f), 0.03f, dark, V(1f, 0, 0), 0.28f);

        // A head drawn out into a long flat snout, four whiskers under it
        int head = b.Head(V(0, 0.26f, 0.1f));
        var c = V(0, 0.265f, 0.13f);
        var r = V(0.068f, 0.065f, 0.08f);
        b.Ell(head, c, r, grey);
        b.Spike(head, c + V(0, -0.01f, 0.04f), c + V(0, -0.03f, 0.25f), 0.052f, grey, 0.42f);
        b.PaintEll(head, c + V(0, -0.05f, 0.11f), V(0.05f, 0.016f, 0.15f), pale);
        foreach (float x in new[] { -0.026f, -0.009f, 0.009f, 0.026f })
            b.Tube(head, Smooth(3, c + V(x, -0.035f, 0.16f), c + V(x * 1.1f, -0.06f, 0.165f), c + V(x * 1.2f, -0.085f, 0.155f)), 0.0075f, 0.006f, pale);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, 0.285f);
            b.Eye(head, at, Outward(c, r, at), 0.019f, sclera: true);
        });
        return Lift(b);
    }

    /// <summary>Belugrand: a colossal ancient sturgeon, slate above and pale below, rows of stone shields on its back and sides, a crown of plates, a wide mouth fringed with long barbels.</summary>
    private static PokeBuilder Belugrand()
    {
        var b = new PokeBuilder("Belugrand", 1f, BodyPlan.Fish, V(0, 0.4f, -0.05f)) { Coat = Scales }.Hover();
        var slate = Rgb(74, 88, 106);
        var pale = Rgb(172, 188, 198);
        var rock = Rgb(130, 128, 112);
        var rockDark = Rgb(100, 98, 86);

        void Shield(int bone, Vector3 at, Vector3 dir, float size) =>
            b.Spike(bone, at, at + Vector3.Normalize(dir) * size * 1.4f, size, rock, 0.55f, Shell, 0.012f);

        var bc = V(0, 0.4f, -0.05f);
        b.Ell(Body, bc, V(0.22f, 0.21f, 0.38f), slate);
        b.PaintEll(Body, bc + V(0, -0.15f, 0.05f), V(0.24f, 0.09f, 0.36f), pale);
        // The stone shields: a row down its back, two down each side
        for (int i = 0; i < 7; i++)
        {
            float z = 0.22f - 0.09f * i;
            float u = (z - bc.Z) / 0.38f;
            float y = bc.Y + 0.21f * MathF.Sqrt(MathF.Max(0f, 1f - u * u));
            Shield(Body, V(0, y - 0.02f, z), V(0, 1f, -0.25f), 0.05f - 0.003f * i);
        }
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 6; i++)
            {
                float z = 0.18f - 0.09f * i;
                float u = (z - bc.Z) / 0.38f, w = MathF.Sqrt(MathF.Max(0f, 1f - u * u));
                Shield(Body, V(0.16f * w * s, bc.Y + 0.13f * w, z), V(0.6f * s, 0.8f, -0.2f), 0.036f - 0.002f * i);
                Shield(Body, V(0.21f * w * s, bc.Y + 0.02f, z - 0.03f), V(1f * s, 0.1f, -0.2f), 0.03f - 0.002f * i);
            }
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, V(0.17f * s, 0.27f, 0.12f), PokeRole.Fin, s, s);
            Blade(b, fin, V(0.17f * s, 0.27f, 0.12f), V(0.34f * s, 0.14f, -0.02f), 0.07f, rockDark, V(0, 1f, 0.2f), 0.25f, Shell);
            Blade(b, Body, V(0.1f * s, 0.24f, -0.28f), V(0.2f * s, 0.16f, -0.36f), 0.04f, rockDark, V(0, 1f, 0), 0.25f, Shell);
        });
        Blade(b, Body, V(0, 0.55f, -0.3f), V(0, 0.62f, -0.4f), 0.04f, rockDark, V(1f, 0, 0), 0.25f, Shell);

        // A tail lifting into a long upper lobe of rough fin
        int tail = b.Tail(V(0, 0.42f, -0.4f));
        b.Limb(tail, V(0, 0.42f, -0.38f), V(0, 0.46f, -0.56f), 0.1f, 0.05f, slate);
        Blade(b, tail, V(0, 0.47f, -0.54f), V(0, 0.76f, -0.74f), 0.07f, rockDark, V(1f, 0, 0), 0.22f, Shell);
        Blade(b, tail, V(0, 0.44f, -0.55f), V(0, 0.32f, -0.66f), 0.05f, rockDark, V(1f, 0, 0), 0.22f, Shell);

        // A great blunt head: a crown of shield plates, tiny old eyes, a wide mouth with its barbels hanging under it
        int head = b.Head(V(0, 0.4f, 0.24f));
        var c = V(0, 0.39f, 0.32f);
        var r = V(0.19f, 0.16f, 0.16f);
        b.Ell(head, c, r, slate);
        b.Ell(head, c + V(0, -0.1f, 0.05f), V(0.17f, 0.055f, 0.11f), pale);
        b.Mark(head, On(c, r, 0, 0.33f), V(0, -0.2f, 1f), 0.11f, 0.014f, Rgb(54, 46, 52), MarkShape.Bar);
        foreach (var (x, z, k) in new[] { (0f, 0.34f, 0.055f), (0.08f, 0.31f, 0.045f), (-0.08f, 0.31f, 0.045f), (0.05f, 0.24f, 0.05f), (-0.05f, 0.24f, 0.05f), (0.13f, 0.27f, 0.035f), (-0.13f, 0.27f, 0.035f) })
        {
            float u = x / r.X, w = (z - c.Z) / r.Z;
            float y = c.Y + r.Y * MathF.Sqrt(MathF.Max(0f, 1f - u * u - w * w));
            Shield(head, V(x, y - 0.02f, z), V(x * 3f, 1f, -0.3f), k);
        }
        for (int i = 0; i < 8; i++)
        {
            float x = -0.11f + 0.22f * i / 7f;
            float drop = 0.09f + 0.04f * MathF.Cos(i * 1.3f);
            b.Tube(head, Smooth(3, c + V(x, -0.12f, 0.1f), c + V(x * 1.1f, -0.2f, 0.13f), c + V(x * 1.2f + 0.01f * MathF.Sin(i), -0.12f - drop, 0.12f)), 0.013f, 0.008f, pale);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.12f * s, 0.43f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(90, 80, 60));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Somnel line

    /// <summary>
    /// Sleepy eyes high on a catfish's head: each a bulge with an eye half under a heavy lid, its lid in the skin's
    /// colour laid over the eye's upper half.
    /// </summary>
    private static void C6SleepyEyes(PokeBuilder b, int head, Vector3 at, float size, Color skin)
    {
        PokeBuilder.Both(s =>
        {
            var bulge = at * V(s, 1f, 1f);
            b.Ell(head, bulge, V(size * 1.9f, size * 1.6f, size * 1.7f), skin);
            var eye = bulge + V(0.25f * size * s, -0.15f * size, size * 1.55f);
            b.Eye(head, eye, V(0.35f * s, 0.05f, 1f), size, sclera: true, white: Rgb(232, 226, 190));
            b.Ell(head, bulge + V(0.2f * size * s, size * 0.55f, size * 0.55f), V(size * 1.75f, size * 0.9f, size * 1.25f), skin, V(-15f, 0, 0), blend: 0.006f);
        });
    }

    /// <summary>Somnel: a small smooth olive catfish, its mouth huge and wide, two long whiskers and two short, its eyes half shut, a bubble at its lips like a snore.</summary>
    private static PokeBuilder Somnel()
    {
        var b = new PokeBuilder("Somnel", 0.55f, BodyPlan.Fish, V(0, 0.28f, -0.05f)) { Coat = Scales }.Hover();
        var olive = Rgb(98, 112, 66);
        var dark = Rgb(72, 82, 50);
        var belly = Rgb(186, 192, 132);
        var mouth = Rgb(120, 92, 86);

        var bc = V(0, 0.28f, -0.06f);
        b.Ell(Body, bc, V(0.18f, 0.14f, 0.21f), olive);
        b.PaintEll(Body, bc + V(0, -0.1f, 0.04f), V(0.19f, 0.07f, 0.2f), belly);
        b.Ell(Body, bc + V(0, 0.13f, -0.1f), V(0.02f, 0.045f, 0.06f), dark, V(-20f, 0, 0), blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, V(0.13f * s, 0.2f, 0.04f), PokeRole.Fin, s, s);
            b.Ell(fin, V(0.17f * s, 0.18f, 0.0f), V(0.06f, 0.014f, 0.05f), dark, V(0, 0, 10f * s), blend: 0.01f);
            b.Ell(Body, V(0.09f * s, 0.17f, -0.14f), V(0.035f, 0.012f, 0.035f), dark, V(0, 0, 15f * s), blend: 0.01f);
        });
        int tail = b.Tail(bc + V(0, 0, -0.18f));
        b.Limb(tail, bc + V(0, 0, -0.18f), bc + V(0, 0.01f, -0.28f), 0.07f, 0.045f, olive);
        b.Ell(tail, bc + V(0, 0.02f, -0.34f), V(0.022f, 0.09f, 0.065f), dark, blend: 0.01f);

        // The broad flat head, its mouth from side to side under a pale lip
        int head = b.Head(V(0, 0.28f, 0.1f));
        var c = V(0, 0.29f, 0.14f);
        var r = V(0.19f, 0.11f, 0.12f);
        b.Ell(head, c, r, olive);
        b.Ell(head, c + V(0, -0.06f, 0.06f), V(0.16f, 0.045f, 0.07f), belly);
        b.PaintEll(head, c + V(0, -0.025f, 0.115f), V(0.15f, 0.018f, 0.04f), mouth, soft: 0.008f);
        C6SleepyEyes(b, head, c + V(0.08f, 0.085f, 0.03f), 0.016f, olive);
        // Its whiskers: two long, two short under its chin
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(4, c + V(0.12f * s, -0.01f, 0.1f), c + V(0.22f * s, -0.03f, 0.15f), c + V(0.32f * s, -0.07f, 0.08f), c + V(0.38f * s, -0.1f, -0.08f), c + V(0.4f * s, -0.08f, -0.2f)), 0.012f, 0.007f, dark);
            b.Tube(head, Smooth(3, c + V(0.05f * s, -0.09f, 0.1f), c + V(0.055f * s, -0.14f, 0.11f), c + V(0.065f * s, -0.17f, 0.1f)), 0.009f, 0.007f, dark);
        });
        // A bubble at its lips
        b.Ell(head, c + V(-0.17f, 0.02f, 0.11f), V(0.035f, 0.035f, 0.035f), Rgb(200, 224, 236), mat: Shell, blend: 0.004f);
        return Lift(b);
    }

    /// <summary>Somnoros: an enormous mottled wels catfish, a mouth wide enough for an oar, whiskers trailing like ropes, a nightcap of river weed over its sleepy eyes.</summary>
    private static PokeBuilder Somnoros()
    {
        var b = new PokeBuilder("Somnoros", 1f, BodyPlan.Fish, V(0, 0.32f, -0.04f)) { Coat = Scales }.Hover();
        var olive = Rgb(96, 102, 62);
        var black = Rgb(40, 42, 32);
        var belly = Rgb(152, 150, 104);
        var mouth = Rgb(116, 88, 82);
        var weed = Rgb(72, 120, 56);

        var bc = V(0, 0.32f, -0.04f);
        b.Ell(Body, bc, V(0.24f, 0.2f, 0.3f), olive);
        b.PaintEll(Body, bc + V(0, -0.14f, 0.05f), V(0.25f, 0.08f, 0.28f), belly);
        var rng = new GenomeRandom(10118u);
        for (int i = 0; i < 12; i++)
        {
            float a = rng.Range(0f, MathF.Tau), y = rng.Range(-0.05f, 0.9f);
            var dir = Vector3.Normalize(V(MathF.Cos(a), y, MathF.Sin(a)));
            b.PaintEll(Body, bc + dir * V(0.24f, 0.2f, 0.3f), V(0.05f, 0.04f, 0.06f) * rng.Range(0.7f, 1.3f), black);
        }
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, V(0.18f * s, 0.2f, 0.08f), PokeRole.Fin, s, s);
            b.Ell(fin, V(0.24f * s, 0.17f, 0.04f), V(0.09f, 0.018f, 0.07f), black, V(0, 0, 10f * s), blend: 0.01f);
        });
        // A long tail curling up behind it, a ribbon of fin along it
        int tail = b.Tail(bc + V(0, 0, -0.26f));
        var path = Smooth(4, bc + V(0, 0, -0.24f), bc + V(0.03f, 0.01f, -0.42f), bc + V(0.07f, 0.04f, -0.56f), bc + V(0.13f, 0.1f, -0.66f), bc + V(0.18f, 0.18f, -0.68f));
        b.Tube(tail, path, 0.14f, 0.05f, olive);
        Ribbon(b, tail, Smooth(2, bc + V(0, 0.12f, -0.36f), bc + V(0.04f, 0.12f, -0.48f), bc + V(0.09f, 0.16f, -0.6f), bc + V(0.15f, 0.24f, -0.66f)), 0.05f, black, V(1f, 0, 0), 0.016f);
        PokeBuilder.Both(s => b.PaintEll(tail, bc + V(0.06f * s, 0.06f, -0.52f), V(0.06f, 0.06f, 0.05f), black));

        // A vast head, its mouth across the whole front, river weed draped over it
        int head = b.Head(V(0, 0.32f, 0.2f));
        var c = V(0, 0.32f, 0.27f);
        var r = V(0.25f, 0.15f, 0.16f);
        b.Ell(head, c, r, olive);
        b.Ell(head, c + V(0, -0.08f, 0.07f), V(0.22f, 0.06f, 0.1f), belly);
        b.PaintEll(head, c + V(0, -0.035f, 0.15f), V(0.21f, 0.022f, 0.05f), mouth, soft: 0.008f);
        foreach (var (x, z) in new[] { (0.12f, 0.2f), (-0.16f, 0.17f), (0.04f, 0.32f), (-0.06f, 0.26f) })
            b.PaintEll(head, V(x, 0.44f, z), V(0.05f, 0.04f, 0.04f), black);
        C6SleepyEyes(b, head, c + V(0.1f, 0.12f, 0.05f), 0.02f, olive);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(4, c + V(0.18f * s, -0.02f, 0.12f), c + V(0.32f * s, -0.05f, 0.18f), c + V(0.44f * s, -0.12f, 0.06f), c + V(0.48f * s, -0.16f, -0.16f), c + V(0.56f * s, -0.12f, -0.36f), c + V(0.52f * s, -0.06f, -0.48f)),
                0.022f, 0.015f, black);
            b.Tube(head, Smooth(3, c + V(0.08f * s, -0.12f, 0.12f), c + V(0.09f * s, -0.2f, 0.14f), c + V(0.11f * s, -0.25f, 0.12f)), 0.011f, 0.008f, black);
        });
        // The nightcap of weed: a mat over its crown, strands hanging down the back and sides of its head
        b.Ell(head, c + V(0, 0.13f, -0.05f), V(0.17f, 0.04f, 0.12f), weed, mat: Leaf);
        for (int i = 0; i < 11; i++)
        {
            float a = -2.6f + 5.2f * i / 10f;
            var top = c + V(MathF.Sin(a) * 0.13f, 0.14f, -0.05f + MathF.Cos(a) * 0.08f);
            var outward = Vector3.Normalize(V(MathF.Sin(a), 0, MathF.Cos(a) * 0.6f - 0.4f));
            b.Tube(head, Smooth(3, top, top + outward * 0.08f + V(0, -0.02f, 0), top + outward * 0.12f + V(0, -0.1f - 0.03f * (i % 3), 0)), 0.012f, 0.008f, weed, Leaf, 0.006f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Brotacel line

    private static readonly Color C6FrogGreen = Rgb(126, 196, 72);
    private static readonly Color C6FrogCream = Rgb(240, 232, 196);
    private static readonly Color C6FrogStripe = Rgb(72, 62, 48);
    private static readonly Color C6Sac = Rgb(168, 218, 236);

    /// <summary>A frog's three long toes from <paramref name="at"/>, each ending in a round sticky pad.</summary>
    private static void C6Toes(PokeBuilder b, int bone, Vector3 at, Vector3 dir, Vector3 spread, float length, float radius, Color color)
    {
        var d = Vector3.Normalize(dir);
        for (int i = -1; i <= 1; i++)
        {
            var tip = at + Vector3.Normalize(d + spread * i) * length;
            b.Limb(bone, at, tip, radius, radius * 0.75f, color, blend: 0.006f);
            b.Ell(bone, tip, V(radius * 1.5f, radius * 1.2f, radius * 1.5f), color, blend: 0.004f);
        }
    }

    /// <summary>Brotacel: a tiny bright-green tree frog perched on a reed, cream-bellied, a dark stripe from its eye to its flank, its throat puffed into a bubble.</summary>
    private static PokeBuilder Brotacel()
    {
        var b = new PokeBuilder("Brotacel", 0.4f, BodyPlan.Quadruped, V(0, 0.15f, -0.02f)) { Coat = Scales };
        var reed = Rgb(108, 150, 70);
        // The reed it sits on
        b.Limb(Root, V(-0.24f, 0.024f, -0.14f), V(0.26f, 0.024f, 0.16f), 0.024f, 0.022f, reed, Leaf);
        b.Ell(Root, V(0.06f, 0.024f, 0.04f), V(0.03f, 0.03f, 0.03f), reed, mat: Leaf, blend: 0.01f);

        // A squat seated body, cream underneath, its stripe along each flank
        var bc = V(0, 0.15f, -0.02f);
        b.Ell(Body, bc, V(0.1f, 0.095f, 0.12f), C6FrogGreen, V(-28f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.03f, 0.05f), V(0.08f, 0.07f, 0.07f), C6FrogCream);
        PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0.09f * s, 0.02f, -0.01f), V(0.03f, 0.016f, 0.11f), C6FrogStripe, V(-28f, 0, 0), 0.006f));
        // Folded hind legs and front legs gripping the reed, every toe padded
        PokeBuilder.Both(s =>
        {
            int hind = b.Leg(s, V(0.08f * s, 0.1f, -0.08f), front: false);
            b.Ell(hind, V(0.1f * s, 0.08f, -0.06f), V(0.04f, 0.045f, 0.075f), C6FrogGreen, V(-15f, 0, 0));
            b.Limb(hind, V(0.11f * s, 0.045f, -0.08f), V(0.12f * s, 0.03f, 0.03f), 0.022f, 0.018f, C6FrogGreen);
            C6Toes(b, hind, V(0.12f * s, 0.028f, 0.03f), V(0.3f * s, -0.05f, 1f), V(0.6f, 0, 0), 0.045f, 0.008f, C6FrogGreen);
            int front = b.Leg(s, V(0.05f * s, 0.13f, 0.05f));
            b.Limb(front, V(0.05f * s, 0.13f, 0.05f), V(0.065f * s, 0.04f, 0.09f), 0.018f, 0.015f, C6FrogGreen);
            C6Toes(b, front, V(0.065f * s, 0.035f, 0.09f), V(0.4f * s, -0.1f, 1f), V(0.6f, 0, 0), 0.035f, 0.007f, C6FrogGreen);
        });

        // A wide head with eyes high at its corners, the stripe running back from them, the bubble of its throat sac
        int head = b.Head(bc + V(0, 0.05f, 0.08f));
        var c = V(0, 0.22f, 0.1f);
        var r = V(0.095f, 0.065f, 0.08f);
        b.Ell(head, c, r, C6FrogGreen);
        PokeBuilder.Both(s =>
        {
            var bulge = c + V(0.058f * s, 0.045f, 0.01f);
            b.Ell(head, bulge, V(0.035f, 0.033f, 0.035f), C6FrogGreen);
            b.PaintEll(head, c + V(0.075f * s, 0.02f, -0.03f), V(0.016f, 0.013f, 0.05f), C6FrogStripe, soft: 0.006f);
            var eye = bulge + V(0.012f * s, 0.004f, 0.03f);
            b.Eye(head, eye, V(0.5f * s, 0.15f, 0.9f), 0.017f, white: Rgb(206, 184, 112), pupil: Rgb(30, 28, 30));
        });
        b.PaintEll(head, c + V(0, -0.035f, 0.07f), V(0.08f, 0.009f, 0.02f), C6FrogStripe, soft: 0.005f);
        b.Ell(head, c + V(0, -0.065f, 0.07f), V(0.058f, 0.052f, 0.05f), C6Sac, mat: Shell, blend: 0.006f);
        b.Ground();
        return b;
    }

    /// <summary>Oracul: a big green frog standing upright like a conductor, a glowing resonator at its throat and two at its cheeks, a reed baton in its hand, a ring of sound spreading round its feet.</summary>
    private static PokeBuilder Oracul()
    {
        var b = new PokeBuilder("Oracul", 0.76f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Scales };
        var green = C6FrogGreen;
        var reed = Rgb(196, 176, 120);
        var ring = Rgb(160, 210, 236);
        var glowSac = Rgb(150, 210, 240);

        // The ring of sound round its feet on the water
        b.Torus(Root, V(0, 0.01f, 0.03f), 0.22f, 0.011f, ring, mat: Shell);

        // Long frog's legs bent at the knee, splayed feet with padded toes
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.36f, -0.02f));
            var knee = V(0.16f * s, 0.22f, 0.07f);
            var ankle = V(0.13f * s, 0.05f, -0.02f);
            b.Limb(leg, V(0.08f * s, 0.36f, -0.02f), knee, 0.055f, 0.045f, green);
            b.Limb(leg, knee, ankle, 0.042f, 0.032f, green);
            C6Toes(b, leg, ankle + V(0, -0.02f, 0.01f), V(0.4f * s, -0.12f, 1f), V(0.55f, 0, 0), 0.13f, 0.013f, green);
        });

        // A round cream-bellied body, dark flaps hanging from its sides like the tails of a coat
        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc, V(0.15f, 0.19f, 0.12f), green);
        b.PaintEll(Body, bc + V(0, -0.03f, 0.08f), V(0.12f, 0.15f, 0.07f), C6FrogCream);
        PokeBuilder.Both(s =>
        {
            Frond(b, Body, V(0.12f * s, 0.6f, -0.07f), V(0.17f * s, 0.27f, -0.1f), 0.06f, green, V(s, 0, -0.5f), 0.22f, Scales);
            b.PaintEll(Body, V(0.14f * s, 0.46f, -0.075f), V(0.03f, 0.16f, 0.05f), C6FrogStripe, Euler(V(0.15f * s, 1f, 0)));
        });
        // The great resonator at its throat
        b.Ell(Body, V(0, 0.62f, 0.12f), V(0.1f, 0.09f, 0.08f), glowSac, mat: Glow, blend: 0.008f);

        // Arms: one thrown open, the other holding up its reed baton
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.13f * s, 0.62f, 0.02f);
            int arm = b.Arm(s, shoulder);
            if (s < 0)
            {
                var elbow = V(-0.26f, 0.6f, 0.05f);
                var hand = V(-0.34f, 0.7f, 0.07f);
                b.Limb(arm, shoulder, elbow, 0.03f, 0.026f, green);
                b.Limb(arm, elbow, hand, 0.026f, 0.022f, green);
                C6Toes(b, arm, hand, V(-0.3f, 1f, 0.2f), V(0.6f, 0, 0), 0.06f, 0.009f, green);
            }
            else
            {
                var elbow = V(0.25f, 0.52f, 0.1f);
                var hand = V(0.32f, 0.57f, 0.15f);
                b.Limb(arm, shoulder, elbow, 0.03f, 0.026f, green);
                b.Limb(arm, elbow, hand, 0.026f, 0.022f, green);
                b.Ell(arm, hand, V(0.03f, 0.032f, 0.03f), green);
                b.Limb(arm, hand + V(-0.02f, -0.04f, 0.0f), hand + V(0.12f, 0.24f, -0.03f), 0.01f, 0.007f, reed, Leaf);
            }
        });

        // A broad head, its mouth a little open as it sings, golden eyes with the stripe running back from them, a resonator at each cheek
        int head = b.Head(V(0, 0.68f, 0.03f));
        var c = V(0, 0.74f, 0.06f);
        var r = V(0.12f, 0.08f, 0.1f);
        b.Ell(head, c, r, green);
        b.PaintEll(head, c + V(0, -0.03f, 0.085f), V(0.1f, 0.016f, 0.03f), Rgb(168, 92, 96), soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var bulge = c + V(0.07f * s, 0.055f, 0.02f);
            b.Ell(head, bulge, V(0.042f, 0.04f, 0.042f), green);
            b.PaintEll(head, c + V(0.095f * s, 0.025f, -0.03f), V(0.02f, 0.016f, 0.06f), C6FrogStripe, soft: 0.006f);
            var eye = bulge + V(0.014f * s, 0.005f, 0.036f);
            b.Eye(head, eye, V(0.5f * s, 0.15f, 0.9f), 0.02f, white: Rgb(206, 148, 56), pupil: Rgb(30, 28, 30));
            b.Ell(head, c + V(0.12f * s, -0.04f, 0.04f), V(0.048f, 0.048f, 0.048f), glowSac, mat: Glow, blend: 0.006f);
        });
        b.Ground();
        return b;
    }
}
