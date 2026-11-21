using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 9 (plan 46 · CP4 to CP8): Rapanor to Stimel (20 species), from the briefs of
// docs/plans/decisions/CP-dex-3b-river-and-caves-more.md and CP-dex-4-folk-and-legends.md. Every sculpt is our own,
// built from nothing; a line shares its palette and its motifs and grows in size and detail. Helpers are C9's own.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames9() => new[]
    {
        "Rapanor", "Guvizel", "Speleurs", "Pesterurs", "Paianjenel", "Paienjenis", "Licurici", "Licuron", "Proteu", "Serpel",
        "Serpoaica", "Dunel", "Dunaroi", "Strigoias", "Strigoion", "Sanzienel", "Sanziana", "Zburel", "Zburator", "Stimel"
    };

    private static PokeBuilder? CreateCarpathia9(string species) => species switch
    {
        "RAPANOR" => Rapanor(),
        "GUVIZEL" => Guvizel(),
        "SPELEURS" => C9Bear(false),
        "PESTERURS" => C9Bear(true),
        "PAIANJENEL" => C9Spider(false),
        "PAIENJENIS" => C9Spider(true),
        "LICURICI" => C9Firefly(false),
        "LICURON" => C9Firefly(true),
        "PROTEU" => Proteu(),
        "SERPEL" => C9DiceSnake(false),
        "SERPOAICA" => C9DiceSnake(true),
        "DUNEL" => C9Dune(false),
        "DUNAROI" => C9Dune(true),
        "STRIGOIAS" => Strigoias(),
        "STRIGOION" => Strigoion(),
        "SANZIENEL" => Sanzienel(),
        "SANZIANA" => Sanziana(),
        "ZBUREL" => Zburel(),
        "ZBURATOR" => Zburator(),
        "STIMEL" => Stimel(),
        _ => null
    };

    // ------------------------------------------------------------------ helpers

    /// <summary>The bone <see cref="Coils"/> (two points a bone) gives the stretch from point i to i + 1.</summary>
    private static int C9SegBone(PokeBuilder b, int i) => i < 2 ? Body : b.Model.Skeleton.Find("seg" + i / 2);

    /// <summary>
    /// Dice snake checks down a coiled body: a dark square on every other stretch, alternately left and right of the
    /// spine, and a pale belly under every stretch, each painted on that stretch's own bone.
    /// </summary>
    private static void C9Checks(PokeBuilder b, Vector3[] points, Func<float, float> radius, Color check, Color belly, int from = 0, int to = int.MaxValue)
    {
        int n = points.Length - 1;
        for (int i = from; i < Math.Min(n, to); i++)
        {
            int bone = C9SegBone(b, i);
            var d = Vector3.Normalize(points[i + 1] - points[i]);
            var up = V(0, 1f, 0) - d * Vector3.Dot(V(0, 1f, 0), d);
            up = up.LengthSquared() < 1e-4f ? V(0, 0, 1f) : Vector3.Normalize(up);
            var side = Vector3.Cross(d, up);
            var mid = (points[i] + points[i + 1]) / 2f;
            float r = radius((i + 0.5f) / n);
            float len = Vector3.Distance(points[i], points[i + 1]);
            if (i % 2 == 0)
            {
                float s = (i / 2) % 2 == 0 ? 1f : -1f;
                b.PaintEll(bone, mid + up * r * 0.8f + side * r * 0.45f * s, V(r * 0.36f, Math.Min(len * 0.32f, r * 0.42f), r * 0.36f), check, Euler(d), r * 0.12f);
            }
            b.PaintEll(bone, mid - up * r * 0.9f, V(r * 0.7f, len * 0.5f + r * 0.3f, r * 0.45f), belly, Euler(d), r * 0.2f);
        }
    }

    /// <summary>The way up from a stretch of a body running along <paramref name="d"/>, square to it.</summary>
    private static Vector3 C9Up(Vector3 d)
    {
        var up = V(0, 1f, 0) - d * Vector3.Dot(V(0, 1f, 0), d);
        return up.LengthSquared() < 1e-4f ? V(0, 0, 1f) : Vector3.Normalize(up);
    }

    /// <summary>Two points leading into a coil from outside, along its first turn's way, the tip raised a little.</summary>
    private static Vector3[] C9TailTo(Vector3[] coil, float length, float rise)
    {
        var d = coil[0] - coil[1];
        d = Vector3.Normalize(V(d.X, 0, d.Z));
        return new[] { coil[0] + d * length + V(0, rise, 0), coil[0] + d * length * 0.5f + V(0, rise * 0.3f, 0) };
    }

    // ------------------------------------------------------------------ Speleurs and Pesterurs

    /// <summary>
    /// Speleurs and Pesterurs: an ancient cave bear, stone-grey fur under a crust of pale mineral; the cub stocky with a
    /// big domed head, the great bear vast, its back grown with calcite like stalagmites.
    /// </summary>
    private static PokeBuilder C9Bear(bool great)
    {
        float k = great ? 1.6f : 1f;
        var b = new PokeBuilder(great ? "Pesterurs" : "Speleurs", great ? 1f : 0.66f, BodyPlan.Quadruped, V(0, 0.3f, 0) * k) { Coat = Fur };
        var fur = great ? Rgb(112, 108, 104) : Rgb(126, 122, 118);
        var paw = Rgb(96, 90, 86);
        var crust = great ? Rgb(232, 226, 210) : Rgb(214, 210, 198);
        var vein = Rgb(186, 176, 160);
        var muzzle = Rgb(156, 146, 136);

        BeastLegs(b, 0.13f * k, 0.22f * k, 0.18f * k, -0.17f * k, 0.075f * k, fur, paw);
        // A heavy barrel of a body, its shoulders a hump
        b.Ell(Body, V(0, 0.32f, -0.02f) * k, V(0.2f, 0.17f, 0.3f) * k, fur);
        b.Ell(Body, V(0, 0.38f, 0.12f) * k, V(0.19f, 0.16f, 0.15f) * k, fur, blend: 0.04f * k);
        // The crust: lumps of pale mineral over the shoulders and down the back, dripping down the flanks
        var lumps = great
            ? new[] { (V(0, 0.5f, 0.12f), 0.1f), (V(0.08f, 0.49f, 0.02f), 0.09f), (V(-0.08f, 0.49f, -0.02f), 0.09f), (V(0, 0.47f, -0.12f), 0.09f), (V(0.06f, 0.44f, -0.22f), 0.07f), (V(-0.1f, 0.45f, 0.18f), 0.07f), (V(0.11f, 0.46f, 0.18f), 0.07f) }
            : new[] { (V(0, 0.5f, 0.12f), 0.085f), (V(0.07f, 0.48f, 0.05f), 0.07f), (V(-0.07f, 0.48f, 0.03f), 0.07f), (V(0, 0.46f, -0.06f), 0.07f), (V(0.03f, 0.44f, -0.16f), 0.055f) };
        foreach (var (at, r) in lumps)
            b.Ell(Body, at * k, V(r, r * 0.55f, r) * k, crust, mat: Shell, blend: 0.03f * k);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.17f * s, 0.4f, 0.1f) * k, V(0.06f, 0.09f, 0.08f) * k, crust);
            b.PaintEll(Body, V(0.18f * s, 0.38f, -0.06f) * k, V(0.05f, 0.08f, 0.06f) * k, crust);
        });
        b.PaintEll(Body, V(0, 0.52f, 0.02f) * k, V(0.03f, 0.02f, 0.14f) * k, vein);
        if (great)
        {
            // Stalagmites of calcite stand up along its spine and shoulders
            foreach (var (at, tip, r) in new[]
                     {
                         (V(0, 0.55f, 0.14f), V(0, 0.72f, 0.12f), 0.05f), (V(0, 0.54f, 0.02f), V(0.01f, 0.76f, -0.01f), 0.055f),
                         (V(0, 0.52f, -0.1f), V(0, 0.68f, -0.14f), 0.048f), (V(0.01f, 0.48f, -0.2f), V(0.02f, 0.6f, -0.26f), 0.04f),
                         (V(0.09f, 0.53f, 0.08f), V(0.14f, 0.66f, 0.07f), 0.035f), (V(-0.09f, 0.53f, 0.08f), V(-0.14f, 0.65f, 0.06f), 0.035f),
                         (V(0.1f, 0.5f, -0.06f), V(0.15f, 0.6f, -0.08f), 0.03f), (V(-0.1f, 0.5f, -0.06f), V(-0.15f, 0.61f, -0.09f), 0.03f)
                     })
                b.Spike(Body, at * k, tip * k, r * k, crust, mat: Shell, blend: 0.02f * k);
        }
        int tail = b.Tail(V(0, 0.34f, -0.3f) * k);
        b.Ell(tail, V(0, 0.34f, -0.32f) * k, V(0.04f, 0.04f, 0.04f) * k, fur);

        // A big domed head, a short muzzle, small round ears; small kind eyes
        int head = b.Head(V(0, 0.4f, 0.24f) * k);
        var c = great ? V(0, 0.47f, 0.33f) * k : V(0, 0.46f, 0.32f) * k;
        var r0 = great ? V(0.12f, 0.12f, 0.11f) * k : V(0.13f, 0.13f, 0.12f) * k;
        b.Ell(head, c, r0, fur);
        // The steep brow: the dome rises above the eyes
        b.Ell(head, c + V(0, 0.05f, -0.02f) * k, r0 * V(0.85f, 0.8f, 0.8f), fur, blend: 0.03f * k);
        var mc = c + V(0, -0.05f, 0.1f) * k;
        var mr = V(0.06f, 0.05f, 0.06f) * k;
        b.Ell(head, mc, mr, muzzle, blend: 0.02f * k);
        b.Ell(head, mc + V(0, 0.02f, 0.055f) * k, V(0.025f, 0.017f, 0.014f) * k, Black, mat: Shell, blend: 0.005f * k);
        b.Mark(head, mc + V(0, -0.025f, 0.055f) * k, V(0, -0.3f, 1f), 0.02f * k, 0.007f * k, Rgb(70, 62, 58), MarkShape.Smile);
        if (!great) b.PaintEll(head, c + V(0, 0.12f, -0.02f) * k, V(0.06f, 0.025f, 0.06f) * k, crust);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.09f * s, 0.09f, -0.03f) * k);
            b.Ell(ear, c + V(0.1f * s, 0.11f, -0.03f) * k, V(0.035f, 0.035f, 0.022f) * k, fur);
            b.PaintEll(ear, c + V(0.1f * s, 0.11f, -0.01f) * k, V(0.02f, 0.02f, 0.012f) * k, paw);
            var look = V(0.42f * s, 0.12f, 1f);
            b.Eye(head, Out(c, r0, default, look), look, (great ? 0.016f : 0.02f) * k, Rgb(80, 66, 54));
        });
        return b;
    }

    // ------------------------------------------------------------------ Serpel and Serpoaica

    /// <summary>
    /// Serpel and Serpoaica: dice snakes of the delta, olive green checked with dark squares, yellowish beneath; the
    /// young coiled low in the shallows, the grown one long, its head reared high, fangs dripping green venom.
    /// </summary>
    private static PokeBuilder C9DiceSnake(bool grown)
    {
        var olive = grown ? Rgb(112, 128, 62) : Rgb(126, 148, 74);
        var check = grown ? Rgb(34, 38, 28) : Rgb(52, 62, 38);
        var belly = grown ? Rgb(222, 206, 112) : Rgb(214, 210, 140);
        Vector3[] points;
        Func<float, float> radius;
        if (!grown)
        {
            var coil = Spiral3(0.2f, 0.11f, 0.035f, 0.06f, 2.6f, 1.25f, 16);
            points = C9TailTo(coil, 0.1f, 0.03f).Concat(coil)
                .Concat(new[] { V(0, 0.1f, 0.06f), V(0, 0.18f, 0.1f), V(0, 0.25f, 0.12f) }).ToArray();
            radius = t => 0.01f + 0.026f * MathF.Min(1f, t * 3.2f) - 0.006f * MathF.Max(0f, t - 0.85f) * 6f;
        }
        else
        {
            var coil = Spiral3(0.3f, 0.16f, 0.05f, 0.09f, 2.4f, 1.4f, 20);
            points = C9TailTo(coil, 0.16f, 0.04f).Concat(coil)
                .Concat(new[] { V(0, 0.16f, 0.08f), V(0, 0.3f, 0.12f), V(0, 0.44f, 0.12f), V(0, 0.56f, 0.1f), V(0, 0.64f, 0.12f) }).ToArray();
            radius = t => 0.012f + 0.04f * MathF.Min(1f, t * 3f) - 0.008f * MathF.Max(0f, t - 0.85f) * 6f;
        }
        var b = new PokeBuilder(grown ? "Serpoaica" : "Serpel", grown ? 0.95f : 0.56f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int neck = Coils(b, points, radius, olive);
        C9Checks(b, points, radius, check, belly);

        int head = b.Head(points[^1], neck);
        float k = grown ? 1.45f : 1f;
        var c = points[^1] + V(0, 0.02f, 0.045f) * k;
        var r = V(0.042f, 0.032f, 0.065f) * k;
        b.Ell(head, c, r, olive);
        b.Ell(head, c + V(0, -0.012f, 0.03f) * k, V(0.032f, 0.02f, 0.045f) * k, olive, blend: 0.015f * k);
        b.PaintEll(head, c + V(0, -0.028f, 0.02f) * k, V(0.04f, 0.012f, 0.07f) * k, belly);
        PokeBuilder.Both(s => b.PaintEll(head, c + V(0.025f * s, 0.024f, -0.03f) * k, V(0.012f, 0.012f, 0.012f) * k, check));
        if (grown)
        {
            // Jaws open: two fangs, a bead of green-yellow venom hanging from each
            var mouth = c + V(0, -0.012f, 0.06f) * k;
            Gape(b, head, mouth, V(0.026f, 0.012f, 0.02f) * k, Rgb(196, 90, 96), 0.02f * k);
            PokeBuilder.Both(s =>
            {
                var fang = mouth + V(0.011f * s, 0.012f * k - 0.02f * k, -0.002f);
                b.Ell(head, fang + V(0, -0.006f, 0.002f) * k, V(0.0065f, 0.009f, 0.0065f) * k, Rgb(170, 214, 70), mat: Glow, blend: 0.004f);
            });
        }
        // A forked tongue flickering out
        var tongue = c + V(0, -0.018f, 0.06f) * k;
        var fork = tongue + V(0, -0.004f, 0.035f) * k;
        b.Tube(head, new[] { tongue, fork }, 0.0045f * k, 0.004f * k, Rgb(206, 70, 86), blend: 0f);
        PokeBuilder.Both(s => b.Tube(head, new[] { fork, fork + V(0.008f * s, -0.003f, 0.014f) * k }, 0.004f * k, 0.0035f * k, Rgb(206, 70, 86), blend: 0f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.026f * s * k, c.Y + 0.012f * k);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.3f), 0.013f * k, Rgb(206, 170, 60), glare: grown);
        });
        return b;
    }

    // ------------------------------------------------------------------ Strigoias and Strigoion

    /// <summary>Strigoias: a little ghost under a rough linen sheet, round glowing eyes and a cheeky grin, a sprig of garlic flower tied round it, its hem tattered.</summary>
    private static PokeBuilder Strigoias()
    {
        var b = new PokeBuilder("Strigoias", 0.5f, BodyPlan.Floating, V(0, 0.26f, 0)) { Coat = Fur }.Hover();
        var linen = Rgb(226, 220, 204);
        var shade = Rgb(188, 180, 164);
        var dark = Rgb(56, 58, 62);
        var glow = Rgb(214, 234, 110);
        var rope = Rgb(170, 140, 96);

        // The sheet falls from a round head to a flared, ragged hem
        b.Ell(Body, V(0, 0.24f, 0), V(0.16f, 0.15f, 0.14f), linen);
        b.Ell(Body, V(0, 0.14f, 0), V(0.19f, 0.07f, 0.16f), linen, blend: 0.04f);
        for (int i = 0; i < 9; i++)
        {
            float a = (i + 0.3f) * MathF.Tau / 9f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            float len = 0.08f + 0.035f * ((i * 7) % 3);
            b.Spike(Body, V(0, 0.13f, 0) + o * V(0.15f, 0, 0.13f), V(0, 0.13f - len, 0) + o * V(0.2f, 0, 0.17f), 0.05f, linen, 0.45f, blend: 0.015f);
        }
        b.PaintEll(Body, V(0, 0.1f, 0), V(0.2f, 0.06f, 0.18f), shade);
        // The head under the sheet, its face a dark opening
        int head = b.Head(V(0, 0.3f, 0));
        var c = V(0, 0.4f, 0.01f);
        var r = V(0.13f, 0.12f, 0.12f);
        b.Ell(head, c, r, linen);
        b.Spike(head, c + V(0, 0.08f, -0.04f), c + V(0.03f, 0.17f, -0.1f), 0.05f, linen, blend: 0.02f);
        b.PaintEll(head, c + V(0, -0.01f, 0.1f), V(0.085f, 0.075f, 0.05f), dark);
        b.Mark(head, Out(c, r, default, V(0, -0.35f, 1f)), V(0, -0.35f, 1f), 0.04f, 0.016f, Rgb(160, 200, 90), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var look = V(0.38f * s, 0.12f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.024f, sclera: true, white: glow, pupil: Rgb(40, 50, 30));
        });
        // A rope tied round its neck, a sprig of garlic flowers knotted into it
        b.Torus(Body, V(0, 0.29f, 0), 0.13f, 0.012f, rope, sz: 0.9f, blend: 0.004f);
        var knot = V(0.1f, 0.29f, 0.08f);
        b.Ell(Body, knot, V(0.022f, 0.02f, 0.018f), rope, blend: 0.004f);
        b.Tube(Body, new[] { knot, knot + V(0.04f, 0.04f, 0.02f), knot + V(0.07f, 0.09f, 0.02f) }, 0.006f, 0.005f, Rgb(110, 150, 80), blend: 0f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f;
            b.Ell(Body, knot + V(0.07f + 0.018f * MathF.Cos(a), 0.1f + 0.012f * MathF.Sin(a), 0.02f + 0.01f * MathF.Sin(a)), V(0.012f, 0.012f, 0.012f), Rgb(244, 242, 232), blend: 0.004f);
        }
        // Two stubby arms of sheet
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.24f, 0.02f));
            b.Spike(arm, V(0.13f * s, 0.24f, 0.02f), V(0.23f * s, 0.18f, 0.05f), 0.045f, linen, 0.55f, blend: 0.02f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Licurici and Licuron

    /// <summary>
    /// Licurici and Licuron: fireflies, dark brown with an orange-edged shield behind the head and a tail that glows; the
    /// little one's bulb soft yellow-green, the great one's a swollen lantern of yellow-white light with blue sparks at its wings.
    /// </summary>
    private static PokeBuilder C9Firefly(bool lantern)
    {
        float k = lantern ? 1.5f : 1f;
        var b = new PokeBuilder(lantern ? "Licuron" : "Licurici", lantern ? 0.72f : 0.46f, BodyPlan.Bird, V(0, 0.3f, 0) * k) { Coat = Shell }.Hover();
        var brown = Rgb(70, 52, 42);
        var dark = Rgb(46, 36, 32);
        var orange = Rgb(232, 124, 46);
        var shield = Rgb(60, 44, 40);
        var light = lantern ? Rgb(252, 244, 170) : Rgb(204, 240, 120);
        var wing = Rgb(220, 226, 232);
        var spark = Rgb(120, 190, 250);

        // Thorax and the long abdomen tilted down behind, ending in the light
        b.Ell(Body, V(0, 0.3f, 0.02f) * k, V(0.06f, 0.055f, 0.07f) * k, brown);
        b.Ell(Body, V(0, 0.27f, -0.08f) * k, V(0.055f, 0.05f, 0.08f) * k, dark, V(-18f, 0, 0), blend: 0.02f * k);
        var lc = lantern ? V(0, 0.2f, -0.2f) : V(0, 0.22f, -0.17f);
        var lr = lantern ? V(0.085f, 0.09f, 0.1f) : V(0.06f, 0.058f, 0.07f);
        b.Ell(Body, lc * k, lr * k, light, V(-22f, 0, 0), Glow, 0.02f * k);
        foreach (float t in lantern ? new[] { -0.05f, 0f, 0.05f } : new[] { -0.03f, 0.02f })
            b.PaintTorus(Body, (lc + V(0, -t * 0.4f, t)) * k, lr.X * 0.98f * k, 0.006f * k, PixelCanvas.Mix(light, Rgb(200, 150, 60), lantern ? 0.45f : 0.3f), V(68f, 0, 0), sz: 1.1f);
        if (lantern)
        {
            // The lantern is held in ribs of dark shell, like a lamp's frame
            foreach (float x in new[] { -0.05f, 0f, 0.05f })
                b.Tube(Body, new[] { V(x * 0.6f, 0.27f, -0.11f) * k, V(x, 0.26f, -0.18f) * k, V(x * 1.1f, 0.2f, -0.27f) * k, V(x * 0.4f, 0.13f, -0.29f) * k }, 0.007f * k, 0.006f * k, dark, blend: 0.004f);
        }
        // Six thin legs held under the body
        for (int i = 0; i < 3; i++)
            PokeBuilder.Both(s =>
            {
                float z = (0.05f - i * 0.04f) * k;
                var hip = V(0.035f * s, 0.26f, z) * k;
                b.Tube(Body, new[] { hip, hip + V(0.04f * s, -0.04f, 0.01f) * k, hip + V(0.05f * s, -0.1f, 0.02f) * k }, 0.007f * k, 0.005f * k, dark, blend: 0.004f);
            });
        // Wings from the shoulders, clear with a faint vein
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.04f * s, 0.34f, 0.0f) * k);
            var root = V(0.03f * s, 0.34f, 0.0f) * k;
            Frond(b, w, root, root + V(0.2f * s, 0.14f, -0.1f) * k, 0.06f * k, wing, V(0, 1f, 0.3f), 0.18f, blend: 0.01f * k);
            Frond(b, w, root + V(0, -0.01f, -0.02f) * k, root + V(0.17f * s, 0.07f, -0.17f) * k, 0.05f * k, wing, V(0, 1f, 0.3f), 0.18f, blend: 0.01f * k);
            b.PaintEll(w, root + V(0.1f * s, 0.07f, -0.05f) * k, V(0.09f, 0.008f, 0.01f) * k, PixelCanvas.Shadow(wing, 0.2f), Euler(V(0.2f * s, 0.14f, -0.1f)));
            if (lantern)
            {
                // Blue sparks crackle at the wing's edge
                var tip = root + V(0.2f * s, 0.14f, -0.1f) * k;
                b.Spike(w, tip + V(-0.01f * s, -0.005f, 0) * k, tip + V(0.04f * s, 0.03f, -0.01f) * k, 0.012f * k, spark, mat: Glow, blend: 0.004f);
                b.Spike(w, tip + V(-0.02f * s, -0.01f, -0.01f) * k, tip + V(0.03f * s, -0.035f, -0.02f) * k, 0.011f * k, spark, mat: Glow, blend: 0.004f);
            }
        });

        // A round head under the orange-edged shield, big black eyes, two feathery feelers
        int head = b.Head(V(0, 0.31f, 0.07f) * k);
        var c = V(0, 0.31f, 0.11f) * k;
        var r = V(0.05f, 0.048f, 0.045f) * k;
        b.Ell(head, c, r, brown);
        var sc = V(0, 0.36f, 0.08f) * k;
        b.Ell(head, sc, V(0.075f, 0.03f, 0.06f) * k, orange, V(-24f, 0, 0), mat: Shell, blend: 0.015f * k);
        b.PaintEll(head, sc + V(0, 0.02f, -0.01f) * k, V(0.05f, 0.03f, 0.045f) * k, shield, V(-24f, 0, 0));
        if (lantern) b.Torus(head, V(0, 0.28f, 0.07f) * k, 0.05f * k, 0.012f * k, orange, V(70f, 0, 0), mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            Antenna(b, head, c + V(0.02f * s, 0.04f, 0.02f) * k, c + V(0.04f * s, 0.11f, 0.05f) * k, c + V(0.07f * s, 0.15f, 0.04f) * k, 0.006f * k, dark, dark, 0.009f * k);
            var look = V(0.6f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.016f * k, Rgb(40, 34, 30));
        });
        return Lift(b);
    }

    /// <summary>Points spread evenly over a unit sphere (a golden spiral), for bumps over a ball.</summary>
    private static Vector3[] C9Sphere(int count)
    {
        var points = new Vector3[count];
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < count; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / count, rad = MathF.Sqrt(1f - y * y), a = golden * i;
            points[i] = V(MathF.Cos(a) * rad, y, MathF.Sin(a) * rad);
        }
        return points;
    }

    // ------------------------------------------------------------------ Rapanor

    /// <summary>Rapanor: a predatory sea snail, its heavy ridged shell crusted with barnacles, a bright orange mouth at the rim, a grey foot gripping a mussel, eyes on stalks.</summary>
    private static PokeBuilder Rapanor()
    {
        var b = new PokeBuilder("Rapanor", 0.86f, BodyPlan.Floating, V(0, 0.12f, 0.04f)) { Coat = Scales };
        var foot = Rgb(112, 128, 142);
        var footLow = Rgb(92, 104, 118);
        var shell = Rgb(196, 176, 148);
        var ridge = Rgb(150, 128, 104);
        var chalk = Rgb(232, 226, 212);
        var orange = Rgb(246, 120, 40);
        var deep = Rgb(206, 70, 34);
        var mussel = Rgb(56, 58, 86);

        // The muscular foot, broad and low
        b.Ell(Body, V(0, 0.07f, 0.04f), V(0.22f, 0.07f, 0.32f), foot);
        b.Ell(Body, V(0, 0.13f, 0.14f), V(0.15f, 0.08f, 0.16f), foot, blend: 0.04f);
        b.PaintEll(Body, V(0, 0.0f, 0.04f), V(0.24f, 0.04f, 0.34f), footLow);
        // The shell: a heavy body whorl and a short spire, each ringed with a ridge, rising up and back
        int sh = b.Part("shell", Body, V(0, 0.28f, -0.08f), PokeRole.Body);
        float y = 0.3f, z = -0.08f, rr = 0.24f;
        for (int i = 0; i < 4; i++)
        {
            b.Ell(sh, V(0, y, z), V(rr * 1.02f, rr * 0.86f, rr), shell, V(-14f, 0, 0), Shell, 0.03f);
            b.Torus(sh, V(0, y - rr * 0.18f, z), rr * 0.96f, Math.Max(0.016f, rr * 0.09f), ridge, V(-14f, 0, 0), mat: Shell, blend: 0.01f);
            y += rr * 0.72f;
            z -= rr * 0.22f;
            rr *= 0.6f;
        }
        b.Spike(sh, V(0, y - 0.02f, z), V(0, y + 0.05f, z - 0.02f), 0.035f, ridge, mat: Shell, blend: 0.01f);
        // Chalky crust and barnacles over the shell
        foreach (var (at, r) in new[] { (V(0.16f, 0.4f, -0.12f), 0.08f), (V(-0.14f, 0.34f, -0.2f), 0.07f), (V(0.02f, 0.52f, -0.22f), 0.06f), (V(-0.18f, 0.44f, 0.02f), 0.06f) })
            b.PaintEll(sh, at, V(r, r, r), chalk);
        foreach (var (at, dir) in new[] { (V(0.2f, 0.36f, -0.1f), V(1f, 0.3f, 0)), (V(-0.19f, 0.4f, -0.14f), V(-1f, 0.5f, -0.2f)), (V(0.06f, 0.5f, -0.24f), V(0.2f, 0.6f, -0.6f)), (V(-0.1f, 0.48f, 0.04f), V(-0.3f, 1f, 0.3f)), (V(0.14f, 0.27f, -0.26f), V(0.5f, 0, -1f)) })
        {
            var d = Vector3.Normalize(dir);
            b.Spike(sh, at - d * 0.02f, at + d * 0.035f, 0.026f, chalk, mat: Shell, blend: 0.008f);
            b.PaintEll(sh, at + d * 0.034f, V(0.01f, 0.01f, 0.01f), Rgb(110, 100, 92), soft: 0.005f);
        }
        // The mouth: a flared lip of bright orange at the shell's front rim
        var lip = V(0.04f, 0.24f, 0.11f);
        b.Ell(sh, lip, V(0.17f, 0.15f, 0.06f), orange, V(-10f, -12f, 0), Shell, 0.03f);
        b.Cut(sh, lip + V(0, 0, 0.05f), V(0.1f, 0.09f, 0.05f), V(-10f, -12f, 0));
        b.PaintEll(sh, lip + V(0, 0, 0.02f), V(0.11f, 0.1f, 0.06f), deep, V(-10f, -12f, 0));

        // The head on the foot's front, two eyes up on stalks, a mussel prised in its grip
        int head = b.Head(V(0, 0.14f, 0.26f));
        var c = V(0, 0.15f, 0.3f);
        var r0 = V(0.09f, 0.07f, 0.09f);
        b.Ell(head, c, r0, foot);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.06f * s, -0.02f, 0.06f), V(0.03f, 0.02f, 0.03f), foot, blend: 0.015f);
            int stalk = b.Ear(head, s, c + V(0.04f * s, 0.05f, 0.02f));
            var tip = c + V(0.1f * s, 0.2f, 0.04f);
            b.Tube(stalk, new[] { c + V(0.04f * s, 0.04f, 0.02f), c + V(0.08f * s, 0.12f, 0.02f), tip }, 0.016f, 0.014f, foot, blend: 0.008f);
            var ball = V(0.034f, 0.034f, 0.034f);
            b.Ell(stalk, tip, ball, foot, blend: 0.008f);
            var look = V(0.25f * s, 0.15f, 1f);
            b.Eye(stalk, Out(tip, ball, default, look), look, 0.015f, sclera: true, white: Rgb(240, 214, 120), pupil: Black);
        });
        // The mussel, its two dark valves pried apart, held to the foot's front
        var mc = V(0.0f, 0.06f, 0.4f);
        b.Ell(Body, mc + V(0, 0.02f, 0), V(0.09f, 0.025f, 0.06f), mussel, V(0, 20f, 12f), Shell, 0.02f);
        b.Ell(Body, mc - V(0, 0.02f, 0), V(0.09f, 0.025f, 0.06f), mussel, V(0, 20f, -6f), Shell, 0.02f);
        b.PaintEll(Body, mc, V(0.08f, 0.012f, 0.05f), Rgb(230, 170, 140), V(0, 20f, 0));
        return b;
    }

    // ------------------------------------------------------------------ Guvizel

    /// <summary>Guvizel: a little round-headed goby of the shore, puffy lips, sandy mottled body matching the pebbles, its pelvic fins a sucker disc.</summary>
    private static PokeBuilder Guvizel()
    {
        var b = new PokeBuilder("Guvizel", 0.42f, BodyPlan.Fish, V(0, 0.36f, 0)) { Coat = Scales }.Hover();
        var sand = Rgb(178, 162, 118);
        var pale = Rgb(226, 214, 178);
        var spot = Rgb(110, 92, 64);
        var blue = Rgb(110, 140, 150);
        var lips = Rgb(206, 156, 128);
        var fin = Rgb(196, 182, 140);

        b.Ell(Body, V(0, 0.34f, -0.06f), V(0.11f, 0.11f, 0.17f), sand);
        b.PaintEll(Body, V(0, 0.25f, -0.04f), V(0.11f, 0.05f, 0.17f), pale);
        foreach (var (at, sr, col) in new[]
                 {
                     (V(0.09f, 0.38f, -0.02f), 0.018f, spot), (V(-0.09f, 0.39f, -0.04f), 0.018f, spot), (V(0.1f, 0.33f, -0.1f), 0.014f, blue),
                     (V(-0.1f, 0.33f, -0.12f), 0.014f, blue), (V(0.06f, 0.43f, -0.12f), 0.016f, spot), (V(-0.05f, 0.44f, -0.08f), 0.016f, spot),
                     (V(0.08f, 0.36f, -0.18f), 0.014f, spot), (V(-0.07f, 0.37f, -0.18f), 0.014f, spot), (V(0, 0.45f, -0.02f), 0.015f, blue)
                 })
            b.PaintEll(Body, at, V(sr, sr, sr), col, soft: 0.008f);
        // The sucker disc under it, two fins fused in a round cup
        b.Ell(Body, V(0, 0.23f, 0.04f), V(0.06f, 0.025f, 0.06f), fin, blend: 0.02f);
        b.Torus(Body, V(0, 0.215f, 0.04f), 0.05f, 0.012f, PixelCanvas.Shadow(fin, 0.12f), blend: 0.006f);
        // Two dorsal fins, the tail fin and broad round pectorals
        int dorsal = b.Part("dorsal", Body, V(0, 0.44f, -0.06f), PokeRole.Fin);
        RimmedFin(b, dorsal, V(0, 0.48f, -0.02f), V(0.012f, 0.05f, 0.05f), V(-20f, 0, 0), fin, spot, 0.012f);
        RimmedFin(b, dorsal, V(0, 0.47f, -0.14f), V(0.012f, 0.05f, 0.07f), V(-10f, 0, 0), fin, spot, 0.012f);
        int tail = b.Tail(V(0, 0.34f, -0.2f));
        b.Limb(tail, V(0, 0.34f, -0.2f), V(0, 0.34f, -0.27f), 0.06f, 0.04f, sand);
        int tf = b.Part("tailFin", tail, V(0, 0.34f, -0.27f), PokeRole.Fin);
        RimmedFin(b, tf, V(0, 0.35f, -0.33f), V(0.012f, 0.07f, 0.06f), default, fin, spot, 0.014f);
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.09f * s, 0.32f, 0.04f), PokeRole.Fin, s, s);
            RimmedFin(b, side, V(0.13f * s, 0.31f, 0.0f), V(0.012f, 0.06f, 0.06f), V(0, 30f * s, 20f * s), fin, PixelCanvas.Shadow(fin, 0.15f), 0.012f);
        });

        // The big round head, its eyes high on top, the puffy lips pouting forward
        int head = b.Head(V(0, 0.36f, 0.08f));
        var c = V(0, 0.37f, 0.12f);
        var r = V(0.13f, 0.12f, 0.12f);
        b.Ell(head, c, r, sand);
        b.PaintEll(head, c + V(0, -0.09f, 0.02f), V(0.12f, 0.05f, 0.11f), pale);
        foreach (var at in new[] { V(0.08f, 0.43f, 0.12f), V(-0.07f, 0.44f, 0.1f), V(0.11f, 0.36f, 0.12f), V(-0.11f, 0.35f, 0.13f) })
            b.PaintEll(head, at, V(0.015f, 0.015f, 0.015f), spot, soft: 0.008f);
        b.Ell(head, V(0, 0.35f, 0.24f), V(0.06f, 0.028f, 0.035f), lips, mat: Shell, blend: 0.012f);
        b.Ell(head, V(0, 0.315f, 0.235f), V(0.055f, 0.026f, 0.033f), lips, mat: Shell, blend: 0.012f);
        b.PaintEll(head, V(0, 0.332f, 0.27f), V(0.045f, 0.006f, 0.02f), PixelCanvas.Shadow(lips, 0.35f), soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            var ec = c + V(0.06f * s, 0.08f, 0.03f);
            var er = V(0.045f, 0.045f, 0.045f);
            b.Ell(head, ec, er, sand, blend: 0.015f);
            var look = V(0.45f * s, 0.35f, 1f);
            b.Eye(head, Out(ec, er, default, look), look, 0.024f, sclera: true, pupil: Rgb(30, 40, 50));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Paianjenel and Paienjenis

    /// <summary>
    /// Paianjenel and Paienjenis: cave spiders. The little one pale cream, round, on very long thin legs; the great one
    /// dark and velvety, its abdomen laced in silver, silk strung between its legs like lace curtains beaded with water,
    /// eight faint blue eyes.
    /// </summary>
    private static PokeBuilder C9Spider(bool lace)
    {
        float k = lace ? 2.2f : 1f;
        var b = new PokeBuilder(lace ? "Paienjenis" : "Paianjenel", lace ? 0.9f : 0.48f, BodyPlan.Quadruped, V(0, 0.2f, -0.04f) * k) { Coat = lace ? Fur : Shell };
        var body = lace ? Rgb(48, 44, 60) : Rgb(234, 226, 202);
        var legC = lace ? Rgb(60, 56, 72) : Rgb(218, 210, 188);
        var mark = lace ? Rgb(206, 206, 216) : Rgb(170, 164, 150);
        var joint = lace ? Rgb(196, 196, 210) : Rgb(196, 188, 166);
        var eyeBlue = Rgb(120, 190, 240);

        // The round abdomen behind, the little cephalothorax before it
        var ac = V(0, 0.22f, -0.1f) * k;
        var ar = (lace ? V(0.13f, 0.11f, 0.15f) : V(0.1f, 0.095f, 0.1f)) * k;
        b.Ell(Body, ac, ar, body);
        if (lace)
        {
            // Silver lace over the abdomen: arcs and dots like a doily
            foreach (var turn in new[] { V(0, 0, 0), V(0, 50f, 0), V(0, -50f, 0) })
                b.PaintEll(Body, ac + V(0, ar.Y * 0.9f, 0), V(ar.X * 0.7f, 0.006f * k, ar.Z * 0.95f), mark, turn + V(0, 0, 0), 0.004f * k);
            foreach (var at in new[] { V(0.07f, 0.95f, 0.2f), V(-0.07f, 0.95f, 0.2f), V(0, 0.98f, -0.3f), V(0.1f, 0.8f, -0.45f), V(-0.1f, 0.8f, -0.45f), V(0.13f, 0.6f, 0.1f), V(-0.13f, 0.6f, 0.1f) })
                b.PaintEll(Body, ac + at * ar, V(0.012f, 0.012f, 0.012f) * k, mark, soft: 0.004f * k);
            b.PaintTorus(Body, ac + V(0, ar.Y * 0.55f, 0), ar.X * 0.82f, 0.006f * k, mark, sz: ar.Z / ar.X);
        }
        else
        {
            // Faint grey chevrons fading back down the abdomen
            for (int i = 0; i < 3; i++)
                b.PaintEll(Body, ac + V(0, ar.Y * 0.9f, -0.02f - 0.03f * i) * V(1, 1, k), V(0.04f - 0.008f * i, 0.008f, 0.008f) * k, PixelCanvas.Mix(body, mark, 0.6f - 0.15f * i), soft: 0.006f * k);
        }
        var cc = V(0, 0.2f, 0.04f) * k;
        var cr = V(0.065f, 0.055f, 0.07f) * k;
        b.Ell(Body, cc, cr, body, blend: 0.02f * k);

        // Eight long jointed legs, four a side, the knees high above the body
        for (int i = 0; i < 4; i++)
            PokeBuilder.Both(s =>
            {
                float z = (0.08f - i * 0.04f) * k;
                var hip = V(0.05f * s * k, 0.2f * k, z);
                int leg = i == 0 ? b.Leg(s, hip, true) : i == 3 ? b.Leg(s, hip, false) : b.Part((s < 0 ? "midL" : "midR") + i, Root, hip, PokeRole.Leg, 0.8f * i, s);
                float spread = i switch { 0 => 0.5f, 1 => 0.15f, 2 => -0.15f, _ => -0.5f };
                var knee = V(0.2f * s, 0.36f, z / k + spread * 0.12f) * k;
                var foot = V(0.34f * s, 0.008f, z / k + spread * 0.32f) * k;
                float lr = (lace ? 0.016f : 0.011f) * k;
                b.Tube(leg, new[] { hip, knee, Vector3.Lerp(knee, foot, 0.55f) + V(0.02f * s, 0.02f, 0) * k, foot }, lr, lr * 0.65f, legC, blend: 0f);
                b.PaintEll(leg, knee, V(lr * 1.6f, lr * 1.6f, lr * 1.6f), joint, soft: lr * 0.5f);
            });
        if (lace)
        {
            // Silk strung between neighbouring legs, sagging, beaded with water
            for (int i = 0; i < 3; i++)
                PokeBuilder.Both(s =>
                {
                    float z0 = (0.08f - i * 0.04f), z1 = (0.08f - (i + 1) * 0.04f);
                    float sp0 = i switch { 0 => 0.5f, 1 => 0.15f, _ => -0.15f }, sp1 = i switch { 0 => 0.15f, 1 => -0.15f, _ => -0.5f };
                    var a = Vector3.Lerp(V(0.2f * s, 0.36f, z0 + sp0 * 0.12f), V(0.34f * s, 0.008f, z0 + sp0 * 0.32f), 0.35f) * k;
                    var e = Vector3.Lerp(V(0.2f * s, 0.36f, z1 + sp1 * 0.12f), V(0.34f * s, 0.008f, z1 + sp1 * 0.32f), 0.35f) * k;
                    var mid = (a + e) / 2f + V(0.01f * s, -0.04f, 0) * k;
                    int bone = b.Model.Skeleton.Find(i == 0 ? (s < 0 ? "legL" : "legR") : ((s < 0 ? "midL" : "midR") + i));
                    b.Tube(bone, Smooth(3, a, mid, e), 0.0045f * k, 0.0045f * k, Rgb(214, 220, 232), blend: 0f);
                    b.Ell(bone, mid, V(0.009f, 0.009f, 0.009f) * k, Rgb(200, 226, 246), mat: Glow, blend: 0.003f);
                });
        }

        // The head: the cephalothorax's front, two eyes and, on the great one, six more glimmering about them
        int head = b.Head(V(0, 0.2f, 0.08f) * k);
        var hc = V(0, 0.21f, 0.1f) * k;
        var hr = V(0.05f, 0.045f, 0.045f) * k;
        b.Ell(head, hc, hr, body, blend: 0.02f * k);
        PokeBuilder.Both(s =>
        {
            var look = V(0.35f * s, 0.2f, 1f);
            if (lace) b.Eye(head, Out(hc, hr, default, look), look, 0.011f * k, eyeBlue, glare: true);
            else b.Eye(head, Out(hc, hr, default, look), look, 0.013f, Rgb(40, 36, 40));
            b.Spike(head, hc + V(0.02f * s, -0.03f, 0.02f) * k, hc + V(0.018f * s, -0.06f, 0.04f) * k, 0.009f * k, lace ? Rgb(90, 84, 104) : Rgb(186, 176, 150), mat: Shell, blend: 0.004f);
            if (lace)
                foreach (var (dx, dy) in new[] { (0.12f, 0.75f), (0.55f, 0.55f), (0.7f, 0.1f) })
                {
                    var d = V(dx * s, dy, 1f);
                    b.Mark(head, Out(hc, hr, default, d), d, 0.006f * k, 0.006f * k, eyeBlue);
                }
        });
        return b;
    }

    // ------------------------------------------------------------------ Proteu

    /// <summary>Proteu: a long, eel-like cave salamander of pale pink, four tiny legs, feathery red gills either side of its head, no eyes, a faint ring of light round its head.</summary>
    private static PokeBuilder Proteu()
    {
        var pink = Rgb(244, 198, 196);
        var belly = Rgb(250, 222, 218);
        var red = Rgb(226, 62, 76);
        var halo = Rgb(252, 220, 236);
        // A long body laid along the ground in a gentle S, the head lifted at the front
        var points = new[]
        {
            V(0.04f, 0.02f, -0.42f), V(0.08f, 0.03f, -0.34f), V(0.08f, 0.04f, -0.25f), V(0.03f, 0.045f, -0.16f), V(-0.04f, 0.045f, -0.08f),
            V(-0.06f, 0.045f, 0.01f), V(-0.03f, 0.045f, 0.09f), V(0.02f, 0.05f, 0.16f), V(0.02f, 0.08f, 0.22f), V(0, 0.12f, 0.26f)
        };
        Func<float, float> radius = t => 0.012f + 0.032f * MathF.Min(1f, t * 2.4f);
        var b = new PokeBuilder("Proteu", 0.5f, BodyPlan.Serpent, points[0]) { Coat = Fur };
        int neck = Coils(b, points, radius, pink);
        for (int i = 0; i < points.Length - 1; i++)
        {
            var d = Vector3.Normalize(points[i + 1] - points[i]);
            b.PaintEll(C9SegBone(b, i), (points[i] + points[i + 1]) / 2f - C9Up(d) * radius((i + 0.5f) / (points.Length - 1)) * 0.9f,
                V(0.03f, Vector3.Distance(points[i], points[i + 1]) * 0.6f, 0.02f), belly, Euler(d));
        }
        // The tail flattened to a paddle
        int tail = b.Tail(points[0]);
        b.Ell(tail, points[0] + V(0, 0.015f, 0.02f), V(0.008f, 0.03f, 0.05f), pink, blend: 0.015f);
        // Four tiny legs, each with a few toes
        foreach (var (i, front) in new[] { (6, true), (2, false) })
            PokeBuilder.Both(s =>
            {
                var at = points[i];
                int leg = b.Leg(s, at + V(0.03f * s, 0, 0), front);
                var foot = at + V(0.07f * s, -at.Y + 0.008f, 0.01f);
                b.Limb(leg, at + V(0.02f * s, 0, 0), foot, 0.011f, 0.008f, pink);
                Digits(b, leg, foot, V(0.4f * s, 0, 1f), V(0.6f, 0, 0), 0.02f, 0.005f, pink);
            });

        int head = b.Head(points[^1], neck);
        var c = points[^1] + V(0, 0.01f, 0.05f);
        var r = V(0.05f, 0.036f, 0.07f);
        b.Ell(head, c, r, pink);
        b.Ell(head, c + V(0, -0.008f, 0.05f), V(0.035f, 0.022f, 0.04f), pink, blend: 0.015f);
        b.Mark(head, Out(c, r, default, V(0, -0.5f, 1f)), V(0, -0.5f, 1f), 0.022f, 0.008f, PixelCanvas.Shadow(pink, 0.3f), MarkShape.Smile);
        // Three feathery red gills each side, each a stem with fronds
        PokeBuilder.Both(s =>
        {
            foreach (var (dy, dz) in new[] { (0.05f, 0.02f), (0.02f, -0.01f), (-0.01f, -0.03f) })
            {
                var root = c + V(0.035f * s, dy * 0.4f, -0.02f + dz * 0.5f);
                var tip = root + V(0.06f * s, dy + 0.025f, dz - 0.02f);
                b.Limb(head, root, tip, 0.008f, 0.006f, red, blend: 0.005f);
                for (int f = 1; f <= 3; f++)
                {
                    var at = Vector3.Lerp(root, tip, f / 3.5f);
                    b.Spike(head, at, at + V(0.008f * s, 0.022f, 0.008f), 0.006f, red, blend: 0.003f);
                    b.Spike(head, at, at + V(0.01f * s, -0.016f, -0.012f), 0.006f, red, blend: 0.003f);
                }
            }
        });
        // The pale ring of light it senses the world with, round its crown and through its gills
        b.Torus(head, c + V(0, 0.02f, -0.01f), 0.075f, 0.007f, halo, V(-12f, 0, 0), mat: Glow, blend: 0.004f);
        return b;
    }

    // ------------------------------------------------------------------ Dunel and Dunaroi

    /// <summary>
    /// Dunel and Dunaroi: dunes of the delta that live. The little one a rounded mound of golden sand rippled by the wind,
    /// a tuft of dune grass on top; the great one long and wide, old twisted oaks and vines on its back and wild horses'
    /// hoofprints down its flanks.
    /// </summary>
    private static PokeBuilder C9Dune(bool great)
    {
        float k = great ? 2.2f : 1f;
        var b = new PokeBuilder(great ? "Dunaroi" : "Dunel", great ? 1f : 0.5f, BodyPlan.Quadruped, V(0, 0.14f, 0) * k) { Coat = Scales };
        var sand = Rgb(226, 196, 136);
        var ripple = Rgb(200, 168, 108);
        var paw = Rgb(214, 182, 122);
        var grass = Rgb(110, 160, 70);
        var oak = Rgb(96, 74, 52);
        var leaves = Rgb(70, 112, 58);

        StubbyLegs(b, 0.14f * k, 0.06f * k, 0.1f * k, -0.1f * k, 0.035f * k, paw);
        var c = great ? V(0, 0.15f, -0.04f) * k : V(0, 0.14f, -0.01f);
        var r = great ? V(0.28f, 0.13f, 0.3f) * k : V(0.2f, 0.15f, 0.18f);
        b.Ell(Body, c, r, sand);
        b.Ell(Body, c + V(0, -0.05f, 0) * k, r * V(1.08f, 0.5f, 1.06f), sand, blend: 0.03f * k);
        // Wind ripples round the mound
        for (int i = 0; i < (great ? 5 : 4); i++)
        {
            float t = 0.25f + 0.17f * i;
            b.PaintTorus(Body, c + V(0, r.Y * (0.85f - t), 0), r.X * MathF.Sqrt(Math.Max(0.05f, 1f - (0.85f - t) * (0.85f - t))) * 1.01f, 0.006f * k, ripple, V(0, 0, 6f * (i % 2 == 0 ? 1 : -1)), sz: r.Z / r.X);
        }
        if (great)
        {
            // Lumps along its flanks, and wild horses' hoofprints pressed into the sand
            PokeBuilder.Both(s =>
            {
                foreach (float z in new[] { 0.12f, 0.0f, -0.12f, -0.24f })
                    b.Ell(Body, V(0.27f * s, 0.12f, z) * k, V(0.03f, 0.022f, 0.03f) * k, sand, blend: 0.02f * k);
                foreach (var (y, z) in new[] { (0.2f, 0.06f), (0.22f, -0.06f), (0.19f, -0.18f) })
                {
                    var at = Out(c, r, default, V(s, (y - 0.15f) * 4f, z * 3f));
                    PokeBuilder.Both(t => b.PaintEll(Body, at + V(0, 0, 0.012f * t) * k, V(0.008f, 0.012f, 0.005f) * k, ripple, soft: 0.004f * k));
                    b.PaintEll(Body, at + V(0, 0.012f, 0) * k, V(0.006f, 0.004f, 0.012f) * k, ripple, soft: 0.004f * k);
                }
            });
            // Old twisted oaks grown on its back, vines trailing down its sides
            int trees = b.Part("trees", Body, V(0, 0.27f, -0.04f) * k, PokeRole.Leaf);
            foreach (var (root, lean, h, crown) in new[]
                     {
                         (V(0.0f, 0.26f, 0.02f), V(0.04f, 0, 0.02f), 0.2f, 0.11f), (V(-0.12f, 0.23f, -0.12f), V(-0.05f, 0, -0.02f), 0.14f, 0.08f),
                         (V(0.12f, 0.23f, -0.14f), V(0.06f, 0, -0.03f), 0.12f, 0.075f)
                     })
            {
                var top = root + lean + V(0, h, 0);
                var bend = root + V(-lean.X * 0.6f, h * 0.5f, -lean.Z * 0.6f);
                b.Tube(trees, Smooth(3, root * k, bend * k, top * k), 0.022f * k, 0.014f * k, oak, Shell, 0.004f);
                b.Tube(trees, new[] { bend * k, (bend + V(lean.X * 1.6f + 0.05f, h * 0.25f, lean.Z)) * k }, 0.01f * k, 0.008f * k, oak, Shell, 0.004f);
                foreach (var (o, sc) in new[] { (V(0, 0.02f, 0), 1f), (V(0.06f, -0.01f, 0.02f), 0.7f), (V(-0.06f, -0.01f, -0.02f), 0.75f), (V(0.02f, 0.0f, -0.06f), 0.65f), (V(-0.02f, -0.01f, 0.06f), 0.65f) })
                    b.Ell(trees, (top + o * crown / 0.11f) * k, V(crown * 0.75f, crown * 0.45f, crown * 0.75f) * sc * k, leaves, mat: Leaf, blend: 0.02f * k);
                b.PaintEll(trees, (root + V(0, 0.01f, 0)) * k, V(0.04f, 0.015f, 0.04f) * k, grass);
            }
            PokeBuilder.Both(s =>
            {
                foreach (float z in new[] { 0.3f, -0.5f })
                    b.Tube(Body, Smooth(3, Out(c, r, default, V(0.6f * s, 0.9f, z)), Out(c, r, default, V(0.85f * s, 0.45f, z + 0.1f)), Out(c, r, default, V(s, 0.05f, z - 0.05f))),
                        0.007f * k, 0.006f * k, grass, blend: 0.003f);
            });
        }
        else
        {
            // A tuft of dune grass growing from its top
            int tuft = b.Part("grass", Body, V(0, 0.28f, -0.02f), PokeRole.Leaf);
            for (int i = 0; i < 9; i++)
            {
                float a = i * MathF.Tau / 9f;
                var root = V(0.02f * MathF.Cos(a), 0.27f, -0.02f + 0.02f * MathF.Sin(a));
                var tip = root + V(0.08f * MathF.Cos(a) * (0.5f + (i % 3) * 0.25f), 0.12f + 0.03f * (i % 3), 0.07f * MathF.Sin(a));
                Blade(b, tuft, root, tip, 0.016f, PixelCanvas.Mix(grass, Rgb(160, 190, 90), (i % 3) * 0.25f), V(-MathF.Sin(a), 0, MathF.Cos(a)), 0.45f);
            }
        }

        // The face: a smooth patch of sand at the front, two small dark eyes; the great one a wide mouth
        int head = b.Head(V(0, 0.12f, 0.12f) * k);
        var hc = great ? V(0, 0.13f, 0.21f) * k : V(0, 0.13f, 0.1f);
        var hr = great ? V(0.18f, 0.1f, 0.12f) * k : V(0.12f, 0.09f, 0.08f);
        b.Ell(head, hc, hr, sand, blend: 0.03f * k);
        if (great)
        {
            b.Mark(head, Out(hc, hr, default, V(0, -0.35f, 1f)), V(0, -0.35f, 1f), 0.09f * k, 0.018f * k, ripple, MarkShape.Smile);
            PokeBuilder.Both(s => b.PaintEll(head, Out(hc, hr, default, V(0.55f * s, 0.45f, 1f)) + V(0, 0.012f, 0) * k, V(0.03f, 0.008f, 0.02f) * k, ripple));
        }
        PokeBuilder.Both(s =>
        {
            var look = V(0.35f * s, 0.25f, 1f);
            b.Eye(head, Out(hc, hr, default, look), look, (great ? 0.016f : 0.018f) * k, Rgb(58, 42, 30));
        });
        return b;
    }

    // ------------------------------------------------------------------ Strigoion

    /// <summary>Strigoion: a tall, gaunt spirit of the folk tales, a ragged cloak of village homespun, a pale long face, red-violet eyes, long thin fingers, white hair; below, a whirl of grave mist and leaves.</summary>
    private static PokeBuilder Strigoion()
    {
        var b = new PokeBuilder("Strigoion", 0.95f, BodyPlan.Floating, V(0, 0.62f, 0)) { Coat = Fur }.Hover();
        var cloak = Rgb(60, 58, 64);
        var weave = Rgb(92, 84, 78);
        var face = Rgb(214, 214, 204);
        var hair = Rgb(236, 236, 240);
        var mist = Rgb(140, 142, 152);
        var rope = Rgb(150, 124, 86);
        var eye = Rgb(206, 90, 210);

        // A whirl of grave mist that tapers away beneath it, fallen leaves caught in it
        var whirl = Spiral(V(0, 0.36f, 0), V(1f, 0, 0), V(0, 0, 1f), 0.1f, 0.02f, 0f, MathF.Tau * 1.6f, 16);
        for (int i = 0; i < whirl.Length; i++) whirl[i].Y = 0.42f - 0.38f * i / (whirl.Length - 1f);
        int tail = b.Tail(V(0, 0.4f, 0));
        b.Tube(tail, new[] { V(0, 0.46f, 0) }.Concat(whirl).ToArray(), 0.09f, 0.012f, mist, blend: 0.03f);
        foreach (var (i, col) in new[] { (4, Rgb(178, 112, 52)), (8, Rgb(150, 96, 52)), (11, Rgb(190, 140, 60)) })
            Petal(b, tail, whirl[i], whirl[i] + V(0.04f, -0.02f, 0.03f), 0.018f, col, Leaf, 0.006f);
        // The cloak: a gaunt body, the homespun flaring to a ragged hem
        b.Ell(Body, V(0, 0.66f, 0), V(0.12f, 0.22f, 0.09f), cloak);
        b.Ell(Body, V(0, 0.5f, 0), V(0.16f, 0.12f, 0.12f), cloak, blend: 0.05f);
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 10f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            float len = 0.1f + 0.05f * ((i * 5) % 3);
            b.Spike(Body, V(0, 0.48f, 0) + o * V(0.13f, 0, 0.1f), V(0, 0.48f - len, 0) + o * V(0.2f, 0, 0.16f), 0.05f, cloak, 0.4f, blend: 0.015f);
        }
        foreach (float y in new[] { 0.46f, 0.58f, 0.72f })
            b.PaintTorus(Body, V(0, y, 0), y < 0.5f ? 0.16f : 0.12f, 0.008f, weave, sz: 0.78f);
        // A rope belt, knotted, its ends hanging
        b.Torus(Body, V(0, 0.56f, 0), 0.125f, 0.011f, rope, sz: 0.78f, blend: 0.004f);
        b.Tube(Body, new[] { V(0.05f, 0.56f, 0.09f), V(0.06f, 0.5f, 0.1f), V(0.05f, 0.44f, 0.1f) }, 0.009f, 0.008f, rope, blend: 0.003f);
        // Long thin arms ending in long thin fingers
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.8f, 0));
            var elbow = V(0.22f * s, 0.66f, 0.04f);
            var hand = V(0.27f * s, 0.5f, 0.08f);
            b.Tube(arm, new[] { V(0.1f * s, 0.8f, 0), elbow, hand }, 0.04f, 0.022f, cloak, blend: 0.01f);
            b.Ell(arm, hand, V(0.022f, 0.026f, 0.016f), face, blend: 0.008f);
            for (int f = -1; f <= 2; f++)
            {
                var knuckle = hand + V(0.006f * f * s, -0.018f, 0.008f * f);
                var mid = knuckle + V(0.02f * s, -0.05f, 0.01f * f);
                b.Tube(arm, new[] { knuckle, mid, mid + V(0.004f * s, -0.04f, 0.012f) }, 0.006f, 0.0045f, face, blend: 0.003f);
            }
        });
        // The hood, and under it a pale long face, glowing eyes and a shock of white hair
        int head = b.Head(V(0, 0.86f, 0));
        var c = V(0, 0.96f, 0.02f);
        var r = V(0.065f, 0.095f, 0.07f);
        b.Ell(head, c + V(0, 0.02f, -0.03f), V(0.1f, 0.12f, 0.09f), cloak, blend: 0.02f);
        b.Spike(head, c + V(0, 0.08f, -0.06f), c + V(0, 0.2f, -0.14f), 0.06f, cloak, blend: 0.02f);
        b.Cut(head, c + V(0, -0.01f, 0.09f), V(0.075f, 0.1f, 0.05f));
        b.Ell(head, c, r, face);
        b.PaintEll(head, c + V(0, -0.04f, 0.06f), V(0.04f, 0.02f, 0.02f), PixelCanvas.Shadow(face, 0.18f));
        b.Mark(head, Out(c, r, default, V(0, -0.55f, 1f)), V(0, -0.55f, 1f), 0.022f, 0.005f, Rgb(80, 70, 80), MarkShape.Bar);
        foreach (var (root, tip) in new[]
                 {
                     (V(0.05f, 1.03f, 0.03f), V(0.12f, 1.08f, 0.04f)), (V(-0.05f, 1.03f, 0.03f), V(-0.13f, 1.06f, 0.05f)), (V(0.02f, 1.05f, 0.04f), V(0.05f, 1.12f, 0.08f)),
                     (V(-0.02f, 1.05f, 0.04f), V(-0.06f, 1.11f, 0.09f)), (V(0.06f, 0.98f, 0.02f), V(0.13f, 0.9f, 0.04f)), (V(-0.06f, 0.98f, 0.02f), V(-0.13f, 0.88f, 0.05f))
                 })
            b.Spike(head, root, tip, 0.022f, hair, 0.5f, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.4f * s, 0.15f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f, sclera: true, white: Rgb(40, 24, 44), pupil: eye, glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Sanzienel and Sanziana

    /// <summary>Sanzienel: a tiny fairy made of a bunch of yellow bedstraw flowers, a round face in the middle, gauzy wings like dandelion seed and a glow like a firefly's.</summary>
    private static PokeBuilder Sanzienel()
    {
        var b = new PokeBuilder("Sanzienel", 0.44f, BodyPlan.Bird, V(0, 0.2f, 0)) { Coat = Leaf }.Hover();
        var gold = Rgb(242, 196, 50);
        var deep = Rgb(214, 160, 36);
        var skin = Rgb(250, 224, 184);
        var leaf = Rgb(110, 156, 72);
        var glow = Rgb(255, 234, 140);
        var gauze = Rgb(236, 238, 230);

        // A little dress of leaves, two thin legs, the glow hanging below like a firefly's
        b.Ell(Body, V(0, 0.2f, 0), V(0.05f, 0.06f, 0.045f), leaf);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            Petal(b, Body, V(0.03f * MathF.Sin(a), 0.2f, 0.03f * MathF.Cos(a)), V(0.07f * MathF.Sin(a), 0.13f, 0.065f * MathF.Cos(a)), 0.03f, leaf, Leaf, 0.008f);
        }
        PokeBuilder.Both(s => b.Tube(Body, new[] { V(0.02f * s, 0.16f, 0), V(0.025f * s, 0.1f, 0.01f), V(0.02f * s, 0.06f, 0.0f) }, 0.009f, 0.008f, leaf, blend: 0.004f));
        b.Ell(Body, V(0, 0.05f, 0), V(0.035f, 0.035f, 0.035f), glow, mat: Glow, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.04f * s, 0.23f, 0.01f));
            b.Tube(arm, new[] { V(0.04f * s, 0.23f, 0.01f), V(0.08f * s, 0.2f, 0.03f), V(0.1f * s, 0.17f, 0.04f) }, 0.009f, 0.008f, leaf, blend: 0.004f);
            b.Ell(arm, V(0.1f * s, 0.165f, 0.04f), V(0.014f, 0.014f, 0.014f), skin, blend: 0.004f);
            // Gauzy wings like dandelion seed
            int w = b.Wing(s, V(0.03f * s, 0.27f, -0.05f));
            var root = V(0.03f * s, 0.27f, -0.05f);
            Frond(b, w, root, root + V(0.2f * s, 0.1f, -0.05f), 0.055f, gauze, V(0, 0.4f, 1f), 0.15f, blend: 0.008f);
            Frond(b, w, root + V(0, -0.02f, 0), root + V(0.17f * s, -0.08f, -0.03f), 0.045f, gauze, V(0, 0.4f, 1f), 0.15f, blend: 0.008f);
            b.PaintEll(w, root + V(0.1f * s, 0.05f, -0.025f), V(0.1f, 0.004f, 0.006f), PixelCanvas.Shadow(gauze, 0.2f), Euler(V(0.2f * s, 0.1f, -0.05f)));
        });

        // The head is a ball of tiny yellow flowers, a round face in the middle of the blossoms
        int head = b.Head(V(0, 0.27f, 0));
        var c = V(0, 0.37f, 0);
        float rr = 0.12f;
        b.Ell(head, c, V(rr, rr, rr), gold);
        foreach (var p in C9Sphere(34))
        {
            if (p.Z > 0.55f && p.Y > -0.6f) continue;
            b.Ell(head, c + p * rr, V(0.03f, 0.03f, 0.03f), (p.X + p.Y) > 0.2f ? gold : deep, mat: Leaf, blend: 0.012f);
            b.PaintEll(head, c + p * (rr + 0.028f), V(0.008f, 0.008f, 0.008f), Rgb(250, 226, 120), soft: 0.004f);
        }
        var fc = c + V(0, -0.01f, 0.06f);
        var fr = V(0.08f, 0.075f, 0.07f);
        b.Ell(head, fc, fr, skin, blend: 0.01f);
        b.Mark(head, Out(fc, fr, default, V(0, -0.45f, 1f)), V(0, -0.45f, 1f), 0.02f, 0.008f, Rgb(160, 90, 70), MarkShape.Smile);
        PokeBuilder.Both(s => b.PaintEll(head, Out(fc, fr, default, V(0.55f * s, -0.2f, 1f)), V(0.016f, 0.01f, 0.01f), Rgb(246, 176, 150)));
        PokeBuilder.Both(s =>
        {
            Antenna(b, head, c + V(0.04f * s, rr * 0.9f, 0), c + V(0.06f * s, rr + 0.06f, 0.02f), c + V(0.08f * s, rr + 0.1f, 0.03f), 0.006f, deep, glow, 0.014f, Glow);
            var look = V(0.35f * s, 0.1f, 1f);
            b.Eye(head, Out(fc, fr, default, look), look, 0.022f, Rgb(150, 96, 40));
        });
        return Lift(b);
    }

    /// <summary>Sanziana: the midsummer fairy, slim and floating in a white folk dress embroidered in gold, a crown of bedstraw, long golden hair trailing into flowers, leaf-shaped ears, petal hands, gauzy wings.</summary>
    private static PokeBuilder Sanziana()
    {
        var b = new PokeBuilder("Sanziana", 0.86f, BodyPlan.Floating, V(0, 0.6f, 0)) { Coat = Leaf }.Hover();
        var dress = Rgb(244, 242, 232);
        var gold = Rgb(230, 182, 52);
        var green = Rgb(100, 148, 70);
        var skin = Rgb(250, 226, 192);
        var hair = Rgb(240, 200, 96);
        var glow = Rgb(255, 234, 140);
        var gauze = Rgb(236, 238, 230);

        // The dress: a slim bodice of leaves and a bell of white skirt, gold flowers worked round its hem and waist
        b.Ell(Body, V(0, 0.64f, 0), V(0.075f, 0.12f, 0.06f), green);
        b.Ell(Body, V(0, 0.42f, 0), V(0.16f, 0.17f, 0.14f), dress, blend: 0.05f);
        b.Ell(Body, V(0, 0.3f, 0), V(0.19f, 0.06f, 0.17f), dress, blend: 0.05f);
        b.PaintTorus(Body, V(0, 0.29f, 0), 0.185f, 0.012f, gold, sz: 0.9f);
        b.PaintTorus(Body, V(0, 0.33f, 0), 0.18f, 0.006f, green, sz: 0.9f);
        b.PaintTorus(Body, V(0, 0.54f, 0), 0.1f, 0.01f, gold, sz: 0.85f);
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 8f;
            var at = V(0, 0.42f, 0) + V(MathF.Sin(a) * 0.15f, 0, MathF.Cos(a) * 0.135f);
            b.PaintEll(Body, at, V(0.018f, 0.018f, 0.018f), gold, soft: 0.006f);
            b.PaintEll(Body, at + V(0, 0.035f, 0), V(0.01f, 0.01f, 0.01f), gold, soft: 0.005f);
        }
        foreach (var (a, len) in new[] { (0.4f, 0.1f), (-0.5f, 0.09f), (2.4f, 0.11f), (-2.6f, 0.1f) })
            Petal(b, Body, V(MathF.Sin(a) * 0.06f, 0.68f, MathF.Cos(a) * 0.05f), V(MathF.Sin(a) * 0.1f, 0.6f - len * 0.2f, MathF.Cos(a) * 0.09f), 0.035f, green, Leaf, 0.008f);
        // Slim legs below the hem, one glow at her feet as the bud's
        PokeBuilder.Both(s => b.Tube(Body, new[] { V(0.04f * s, 0.3f, 0), V(0.045f * s, 0.18f, 0.01f), V(0.04f * s, 0.08f, 0.0f) }, 0.016f, 0.012f, green, blend: 0.004f));
        b.Ell(Body, V(0.055f, 0.06f, 0.02f), V(0.032f, 0.032f, 0.032f), glow, mat: Glow, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            // Thin arms ending in petal hands
            int arm = b.Arm(s, V(0.07f * s, 0.72f, 0));
            var hand = V(0.2f * s, 0.58f, 0.06f);
            b.Tube(arm, new[] { V(0.07f * s, 0.72f, 0), V(0.14f * s, 0.65f, 0.03f), hand }, 0.016f, 0.012f, skin, blend: 0.006f);
            foreach (var d in new[] { V(0.4f * s, -0.6f, 0.4f), V(0.1f * s, -0.9f, 0.3f), V(0.6f * s, -0.4f, 0.1f) })
                Petal(b, arm, hand, hand + Vector3.Normalize(d) * 0.06f, 0.018f, PixelCanvas.Mix(dress, gold, 0.25f), Leaf, 0.006f);
            // Gauzy wings, as the bud's grown wide
            int w = b.Wing(s, V(0.03f * s, 0.72f, -0.05f));
            var root = V(0.03f * s, 0.72f, -0.05f);
            Frond(b, w, root, root + V(0.32f * s, 0.2f, -0.08f), 0.08f, gauze, V(0, 0.4f, 1f), 0.12f, blend: 0.008f);
            Frond(b, w, root + V(0, -0.03f, 0), root + V(0.28f * s, -0.12f, -0.05f), 0.065f, gauze, V(0, 0.4f, 1f), 0.12f, blend: 0.008f);
            b.PaintEll(w, root + V(0.16f * s, 0.1f, -0.04f), V(0.16f, 0.005f, 0.007f), PixelCanvas.Shadow(gauze, 0.2f), Euler(V(0.32f * s, 0.2f, -0.08f)));
        });

        // Her head: a crown of bedstraw, leaf-shaped ears, long golden hair trailing into strands of flowers
        int head = b.Head(V(0, 0.78f, 0));
        var c = V(0, 0.87f, 0.01f);
        var r = V(0.07f, 0.08f, 0.07f);
        b.Limb(head, V(0, 0.74f, 0), V(0, 0.82f, 0.01f), 0.025f, 0.025f, skin);
        b.Ell(head, c, r, skin);
        b.Ell(head, c + V(0, 0.025f, -0.02f), V(0.078f, 0.075f, 0.072f), hair, blend: 0.01f);
        b.Cut(head, c + V(0, -0.02f, 0.06f), V(0.06f, 0.07f, 0.04f));
        b.Ell(head, c + V(0, -0.005f, 0.005f), r * 0.98f, skin, blend: 0.004f);
        foreach (var (x, len) in new[] { (-0.06f, 0.5f), (-0.03f, 0.56f), (0.0f, 0.52f), (0.03f, 0.58f), (0.06f, 0.5f) })
        {
            var top = c + V(x, 0.02f, -0.06f);
            var end = c + V(x * 2.4f, -len, -0.16f - MathF.Abs(x));
            b.Tube(head, Smooth(3, top, c + V(x * 1.8f, -0.12f, -0.1f), end), 0.022f, 0.012f, hair, blend: 0.006f);
            for (int f = 0; f < 3; f++)
                b.Ell(head, end + V(0.01f * (f - 1), 0.016f * f, 0.008f * (f % 2)), V(0.014f, 0.014f, 0.014f), f == 0 ? glow : gold, mat: f == 0 ? Glow : Leaf, blend: 0.004f);
        }
        for (int i = 0; i < 11; i++)
        {
            float a = i * MathF.Tau / 11f;
            b.Ell(head, c + V(MathF.Sin(a) * 0.072f, 0.065f, MathF.Cos(a) * 0.068f), V(0.018f, 0.016f, 0.018f), i % 2 == 0 ? gold : PixelCanvas.Mix(gold, glow, 0.5f), mat: Leaf, blend: 0.005f);
        }
        b.Mark(head, Out(c, r, default, V(0, -0.5f, 1f)), V(0, -0.5f, 1f), 0.014f, 0.006f, Rgb(200, 110, 100), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.06f * s, 0.0f, -0.01f), c + V(0.13f * s, 0.05f, -0.03f), 0.018f, PixelCanvas.Mix(skin, green, 0.35f), V(0, 0, 1f), 0.3f);
            Antenna(b, head, c + V(0.025f * s, 0.075f, 0), c + V(0.04f * s, 0.13f, 0.01f), c + V(0.06f * s, 0.17f, 0.02f), 0.009f, green, glow, 0.016f, Glow);
            var look = V(0.38f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.016f, Rgb(120, 150, 60));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Zburel and Zburator

    /// <summary>Zburel: a little ball of warm fire, orange at the core and gold at the edges, two big eyes and a grin, a falling star's tail of sparks behind.</summary>
    private static PokeBuilder Zburel()
    {
        var b = new PokeBuilder("Zburel", 0.46f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Scales }.Hover();
        var orange = Rgb(246, 128, 40);
        var gold = Rgb(252, 200, 70);
        var core = Rgb(255, 232, 150);

        var c = V(0, 0.3f, 0.02f);
        var r = V(0.13f, 0.12f, 0.12f);
        b.Ell(Body, c, r, orange, mat: Glow);
        b.PaintEll(Body, c + V(0, 0.08f, -0.06f), V(0.11f, 0.06f, 0.1f), gold);
        // Flames licking up off its back
        foreach (var (at, tip) in new[] { (V(0, 0.38f, -0.03f), V(0.02f, 0.52f, -0.1f)), (V(0.07f, 0.36f, -0.05f), V(0.14f, 0.46f, -0.12f)), (V(-0.07f, 0.36f, -0.05f), V(-0.12f, 0.47f, -0.12f)) })
            FlameTongue(b, Body, at, tip, 0.04f, orange, gold);
        // The tail: a falling star's trail of sparks curling away behind
        int tail = b.Tail(V(0, 0.28f, -0.1f));
        var trail = Smooth(3, V(0, 0.28f, -0.08f), V(0.04f, 0.24f, -0.22f), V(0.14f, 0.2f, -0.32f), V(0.26f, 0.2f, -0.36f), V(0.36f, 0.24f, -0.34f));
        b.Tube(tail, trail, 0.075f, 0.012f, orange, Glow, 0.02f);
        for (int i = 2; i < trail.Length - 1; i += 2)
        {
            float t = i / (float)trail.Length;
            b.PaintEll(tail, trail[i] + V(0, 0.02f * (1 - t), 0), V(0.04f, 0.03f, 0.04f) * (1.2f - t), gold);
            b.Spike(tail, trail[i], trail[i] + V(0.02f, 0.05f * (1f - t) + 0.02f, -0.02f), 0.025f * (1.1f - t), gold, mat: Glow, blend: 0.008f);
        }
        b.Ell(tail, trail[^1], V(0.02f, 0.02f, 0.02f), core, mat: Glow, blend: 0.008f);
        // Big bright eyes and a mischievous grin
        Grin(b, Body, Out(c, r, default, V(0, -0.4f, 1f)) - V(0, 0, 0.01f), V(0.05f, 0.022f, 0.03f), Rgb(180, 60, 30));
        PokeBuilder.Both(s =>
        {
            var look = V(0.38f * s, 0.18f, 1f);
            b.Eye(Body, Out(c, r, default, look), look, 0.03f, sclera: true, white: core, pupil: Rgb(60, 30, 20));
        });
        return Lift(b);
    }

    /// <summary>Zburator: the night flyer of the folk tales, a sleek bird of glowing coal, wings of long ribbon flames, a comet's tail of sparks, a crest of white-hot flame and a proud face.</summary>
    private static PokeBuilder Zburator()
    {
        var b = new PokeBuilder("Zburator", 1f, BodyPlan.Bird, V(0, 0.5f, 0)) { Coat = Scales }.Hover();
        var coal = Rgb(56, 42, 40);
        var ember = Rgb(244, 120, 44);
        var gold = Rgb(252, 200, 70);
        var white = Rgb(255, 244, 200);
        var beak = Rgb(236, 176, 64);

        // A sleek body of coal, cracked with glowing seams
        b.Ell(Body, V(0, 0.5f, 0), V(0.11f, 0.13f, 0.17f), coal, V(-20f, 0, 0));
        b.PaintEll(Body, V(0, 0.46f, 0.1f), V(0.08f, 0.09f, 0.06f), PixelCanvas.Mix(coal, ember, 0.5f));
        foreach (var turn in new[] { V(30f, 10f, 60f), V(-20f, -40f, 100f), V(70f, 30f, -20f), V(10f, 80f, 30f) })
            b.PaintEll(Body, V(0, 0.5f, 0), V(0.2f, 0.005f, 0.2f), ember, turn, 0.004f);
        // Two thin legs, talons tucked
        PokeBuilder.Both(s =>
        {
            b.Tube(Body, new[] { V(0.04f * s, 0.42f, 0.0f), V(0.05f * s, 0.32f, -0.02f), V(0.05f * s, 0.26f, 0.02f) }, 0.016f, 0.012f, coal, blend: 0.006f);
            Digits(b, Body, V(0.05f * s, 0.26f, 0.02f), V(0, -0.4f, 1f), V(0.5f, 0, 0), 0.04f, 0.006f, beak, Shell);
        });
        // Wings of long ribbon flames from a dark arm
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.08f * s, 0.58f, 0.02f));
            var wrist = V(0.3f * s, 0.74f, -0.02f);
            b.Tube(w, new[] { V(0.07f * s, 0.58f, 0.02f), V(0.18f * s, 0.7f, 0.0f), wrist }, 0.035f, 0.022f, coal, blend: 0.01f);
            for (int i = 0; i < 5; i++)
            {
                var root = Vector3.Lerp(V(0.12f * s, 0.62f, 0.0f), wrist, i / 4f);
                var tip = root + V((0.12f + 0.04f * i) * s, 0.12f + 0.05f * i, -0.12f - 0.02f * i);
                FlameTongue(b, w, root, tip, 0.03f, ember, i == 4 ? white : gold);
            }
            b.PaintEll(w, wrist, V(0.03f, 0.03f, 0.03f), ember);
        });
        // The comet's tail: a long trail of flame curling away, sparks along it
        int tail = b.Tail(V(0, 0.44f, -0.14f));
        var trail = Smooth(3, V(0, 0.44f, -0.14f), V(0, 0.34f, -0.3f), V(-0.06f, 0.24f, -0.44f), V(-0.18f, 0.18f, -0.52f), V(-0.3f, 0.18f, -0.54f));
        b.Tube(tail, trail, 0.05f, 0.012f, ember, Glow, 0.015f);
        for (int i = 1; i < trail.Length - 1; i += 2)
        {
            float t = i / (float)trail.Length;
            FlameTongue(b, tail, trail[i], trail[i] + V(-0.02f, 0.08f * (1.1f - t), -0.05f), 0.028f * (1.2f - t), ember, gold);
        }
        // A proud head on a long neck, a hooked golden beak, a crest of white-hot flame
        int head = b.Head(V(0, 0.62f, 0.1f));
        b.Limb(head, V(0, 0.6f, 0.1f), V(0, 0.72f, 0.16f), 0.05f, 0.042f, coal);
        var c = V(0, 0.78f, 0.18f);
        var r = V(0.055f, 0.055f, 0.065f);
        b.Ell(head, c, r, coal);
        b.PaintEll(head, c + V(0, -0.03f, 0.03f), V(0.04f, 0.025f, 0.04f), PixelCanvas.Mix(coal, ember, 0.45f));
        b.Spike(head, c + V(0, -0.005f, 0.04f), c + V(0, -0.03f, 0.12f), 0.024f, beak, mat: Shell, blend: 0.008f);
        foreach (var (at, tip) in new[] { (V(0, 0.04f, -0.01f), V(0, 0.16f, -0.08f)), (V(0, 0.03f, -0.04f), V(0, 0.11f, -0.15f)), (V(0, 0.045f, 0.02f), V(0, 0.15f, 0.0f)) })
            FlameTongue(b, head, c + at, c + tip, 0.028f, gold, white);
        PokeBuilder.Both(s =>
        {
            var look = V(0.7f * s, 0.15f, 0.7f);
            b.Eye(head, Out(c, r, default, look), look, 0.016f, sclera: true, white: Rgb(252, 230, 120), pupil: Rgb(60, 20, 10), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Stimel

    /// <summary>Stimel: a small serpentine dragon hatchling in deep lake-blue with a pale belly, two stubby legs, small fin-like ears, big shy eyes, a bit of eggshell still on its head.</summary>
    private static PokeBuilder Stimel()
    {
        var blue = Rgb(52, 106, 146);
        var belly = Rgb(200, 228, 238);
        var fin = Rgb(84, 150, 186);
        var shell = Rgb(240, 234, 214);
        // The tail curls on the ground behind, the body rises upright to the neck
        var points = new[]
        {
            V(-0.24f, 0.04f, -0.22f), V(-0.2f, 0.035f, -0.3f), V(-0.1f, 0.035f, -0.32f), V(0.0f, 0.04f, -0.26f), V(0.06f, 0.05f, -0.16f),
            V(0.05f, 0.08f, -0.06f), V(0.02f, 0.14f, 0.0f), V(0, 0.22f, 0.03f), V(0, 0.3f, 0.04f), V(0, 0.36f, 0.05f)
        };
        Func<float, float> radius = t => 0.014f + 0.06f * MathF.Min(1f, t * 1.6f) - 0.022f * MathF.Max(0f, t - 0.7f) / 0.3f;
        var b = new PokeBuilder("Stimel", 0.52f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int neck = Coils(b, points, radius, blue);
        for (int i = 3; i < points.Length - 1; i++)
        {
            var d = Vector3.Normalize(points[i + 1] - points[i]);
            var front = i >= 5 ? Vector3.Normalize(V(0, 0, 1f) - d * Vector3.Dot(V(0, 0, 1f), d)) : -C9Up(d);
            b.PaintEll(C9SegBone(b, i), (points[i] + points[i + 1]) / 2f + front * radius((i + 0.5f) / (points.Length - 1)) * 0.8f,
                V(0.04f, Vector3.Distance(points[i], points[i + 1]) * 0.6f, 0.03f), belly, Euler(d));
        }
        // A fin at the tail's tip, and small fins down its back
        int tail = b.Tail(points[0]);
        Blade(b, tail, points[0], points[0] + V(-0.08f, 0.05f, 0.02f), 0.03f, fin, V(0, 0, 1f), 0.3f);
        foreach (int i in new[] { 4, 6 })
        {
            var d = Vector3.Normalize(points[i + 1] - points[i]);
            var back = i >= 5 ? -Vector3.Normalize(V(0, 0, 1f) - d * Vector3.Dot(V(0, 0, 1f), d)) : C9Up(d);
            var at = points[i] + back * radius(i / (points.Length - 1f)) * 0.7f;
            Blade(b, C9SegBone(b, i), at, at + back * 0.045f - d * 0.02f, 0.018f, fin, V(1f, 0, 0), 0.3f);
        }
        // Two stubby legs at the front of its body
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.1f, 0.02f), true);
            b.Limb(leg, V(0.05f * s, 0.1f, 0.0f), V(0.07f * s, 0.035f, 0.04f), 0.03f, 0.028f, blue);
            b.Ell(leg, V(0.07f * s, 0.022f, 0.06f), V(0.03f, 0.022f, 0.035f), blue);
            for (int t = -1; t <= 1; t++)
                b.PaintEll(leg, V(0.07f * s + t * 0.012f, 0.02f, 0.093f), V(0.004f, 0.01f, 0.005f), belly, soft: 0.004f);
        });

        // A big round head, fin-like ears, big shy eyes and a cap of eggshell
        int head = b.Head(points[^1], neck);
        var c = V(0, 0.45f, 0.08f);
        var r = V(0.1f, 0.09f, 0.095f);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, -0.035f, 0.07f), V(0.06f, 0.04f, 0.045f), blue, blend: 0.02f);
        b.PaintEll(head, c + V(0, -0.06f, 0.06f), V(0.07f, 0.03f, 0.06f), belly);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.018f * s, -0.025f, 0.115f), V(0.006f, 0.005f, 0.005f), Rgb(30, 50, 70), soft: 0.003f);
            Blade(b, head, c + V(0.08f * s, 0.01f, -0.03f), c + V(0.15f * s, 0.06f, -0.08f), 0.028f, fin, V(0, 0, 1f), 0.3f);
            Blade(b, head, c + V(0.08f * s, -0.01f, -0.03f), c + V(0.14f * s, -0.02f, -0.1f), 0.02f, fin, V(0, 0, 1f), 0.3f);
            var look = V(0.42f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.03f, Rgb(40, 70, 100));
        });
        // The eggshell sitting on its crown, its broken rim jagged
        var cap = c + V(0.01f, 0.07f, -0.01f);
        b.Ell(head, cap, V(0.085f, 0.05f, 0.08f), shell, V(0, 0, -8f), Shell, 0.008f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            var rim = cap + V(MathF.Sin(a) * 0.076f, -0.008f, MathF.Cos(a) * 0.071f);
            b.Spike(head, rim + V(0, 0.01f, 0), rim + V(MathF.Sin(a) * 0.006f, -0.028f, MathF.Cos(a) * 0.006f), 0.016f, shell, 0.4f, Shell, 0.004f);
        }
        return b;
    }
}
