using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 · D14), batch 5: Unova's, in our numbers' order
// (2000 + the forebear's National number). Each is sculpted from its forebear's own sculpt as the look of
// docs/plans/D14-proposals-unova.md and the user's reference pictures (docs/art/reference/gemini/unova) say: the
// forebear's build is taken whole (renamed, stretched, its colours changed, a part taken away) and the picture's new
// features are added to it, with the tools of batch 1 (PokemonModels.Ours.Kanto.cs). The forebears' own sculpts are
// never touched.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ tools of this batch

    /// <summary>Takes a lifted forebear's build back down to where it was sculpted (the body's joint at <paramref name="sculptedAt"/>), so its own numbers can be used; <see cref="Lift"/> lifts it again at the end.</summary>
    private static void Unlift(PokeBuilder b, Vector3 sculptedAt)
    {
        var d = Shifted(b, "body", sculptedAt);
        if (d.LengthSquared() < 1e-12f) return;
        Move(b, _ => true, -d);
        b.Model.Skeleton.Shift(-d, first: 1);
    }

    /// <summary>Gives the shapes <paramref name="which"/> picks another colour (a tail of bark where the body stays green).</summary>
    private static void Tint(PokeBuilder b, Func<SdfPrimitive, bool> which, Color color)
    {
        foreach (var s in b.Sdf.Shapes)
            if (which(s)) s.Color = color;
        b.Sdf.Changed();
    }

    /// <summary>Gives the shapes <paramref name="which"/> picks another material (stripes that glow, plates of steel).</summary>
    private static void Make(PokeBuilder b, Func<SdfPrimitive, bool> which, SurfaceMaterial mat)
    {
        foreach (var s in b.Sdf.Shapes)
            if (which(s)) s.Material = mat;
        b.Sdf.Changed();
    }

    private static bool Is(Color a, Color c) => a.R == c.R && a.G == c.G && a.B == c.B;

    /// <summary>A flat plate lying on the surface at <paramref name="at"/>, facing <paramref name="normal"/> (armour, scales of a shell).</summary>
    private static void Plate(PokeBuilder b, int bone, Vector3 at, Vector3 normal, Vector3 radii, Color color, SurfaceMaterial mat, float blend = 0.008f) =>
        b.Ell(bone, at, radii, color, Euler(normal), mat, blend);

    // ------------------------------------------------------------------ 2505 Beaconwatch, from Watchog

    /// <summary>
    /// Beaconwatch: Watchog taller and leaner, a cream ruff at its neck and down its chest, glowing streaks along its
    /// flanks, and its tail held up as a mast whose end is a lamp of glowing yellow ringed in dark bands, a flame at
    /// its tip.
    /// </summary>
    private static PokeBuilder Beaconwatch()
    {
        var b = Become(Watchog(), "Beaconwatch", 0.9f);
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail && Is(s.Color, White));
        var yellow = Rgb(240, 200, 80);
        Make(b, s => Is(s.Color, yellow) && s.Op == SdfOp.Add, Glow);
        Stretch(b, V(0.95f, 1.15f, 0.95f));
        var cream = Rgb(240, 226, 196);
        var lamp = Rgb(255, 196, 70);
        var band = Rgb(74, 62, 52);
        // The cream chest and a ruff round the neck
        b.PaintEll(Body, V(0, 0.34f, 0.035f), V(0.04f, 0.13f, 0.03f), cream);
        FurTufts(b, Body, V(0, 0.5f, 0.005f), V(0.065f, 0.035f, 0.058f), 16, 0.05f, 0.017f, cream, 1.1f, -0.5f, 0.5f);
        // Glowing streaks along both flanks
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.3f, 0.0f), (0.4f, -0.01f), (0.46f, 0.0f) })
                PaintAs(b, Body, V(0.048f * s, y, z), V(0.012f, 0.034f, 0.03f), Rgb(255, 214, 96), Glow, V(0, 0, 28f * s));
        });
        // The lamp: the tail's end runs on up, glowing, banded with dark rings, a flame at its tip
        var end = V(0.0665f, 0.713f, -0.038f);
        var top = end + V(0.01f, 0.15f, 0.01f);
        var up = top - end;
        b.Limb(tail, end - Vector3.Normalize(up) * 0.01f, top, 0.024f, 0.022f, lamp, Glow, 0.006f);
        b.Torus(tail, end + up * 0.02f, 0.025f, 0.012f, band, Euler(up), mat: Metal, blend: 0.004f);
        for (int i = 1; i <= 3; i++)
            b.Torus(tail, end + up * (0.25f * i), 0.024f, 0.007f, band, Euler(up), mat: Metal, blend: 0.003f);
        FlameTongue(b, tail, top - Vector3.Normalize(up) * 0.006f, top + V(0.006f, 0.08f, 0), 0.02f, Rgb(255, 196, 70), Rgb(255, 248, 210));
        return b;
    }

    // ------------------------------------------------------------------ 2510 Umbrapard, from Liepard

    /// <summary>
    /// Umbrapard: Liepard longer and darker, near black-violet, its rosettes grown into a line of yellow crescents
    /// down its neck and back, its mask a dark red, its ears taller, and a second tail of smoke that curls up beside
    /// the first and ends in a violet glow.
    /// </summary>
    private static PokeBuilder Umbrapard()
    {
        var b = Become(Liepard(), "Umbrapard", 0.88f);
        var dark = Rgb(66, 50, 98);
        var deeper = Rgb(50, 38, 76);
        Recolour(b, (Rgb(110, 80, 150), dark), (Rgb(244, 210, 100), deeper), (Rgb(200, 100, 150), Rgb(150, 30, 50)), (Rgb(90, 190, 120), Rgb(236, 232, 244)));
        b.Model.Decals.RemoveAll(d => d.Shape == MarkShape.Ring);
        Stretch(b, V(1f, 1.08f, 1.15f));
        var bc = V(0, 0.302f, -0.0115f);
        var br = V(0.07f, 0.0702f, 0.2185f);
        var moon = Rgb(250, 204, 70);
        for (int i = 0; i < 9; i++)
        {
            float z = 0.16f - i * 0.04f;
            var n = V(0, 1f, z / br.Z * 1.2f);
            b.Mark(Body, Out(bc, br, default, n), n, 0.026f, 0.02f, moon, MarkShape.Smile, 180f);
        }
        // Taller ears
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            b.Spike(ear, V(0.035f * s, 0.465f, 0.24f), V(0.062f * s, 0.6f, 0.2f), 0.017f, dark, 0.5f);
        });
        // The shadow-tail: a second tail of dark smoke curling up beside the first, glowing violet at its end
        int tail = BoneOf(b, "tail");
        var root = V(0, 0.324f, -0.2185f);
        var path = Smooth(3, root, V(-0.03f, 0.4f, -0.29f), V(-0.09f, 0.52f, -0.3f), V(-0.07f, 0.64f, -0.24f), V(-0.01f, 0.66f, -0.2f), V(0.02f, 0.6f, -0.22f));
        b.Tube(tail, path, 0.022f, 0.016f, Rgb(84, 70, 124), blend: 0f);
        b.Ell(tail, path[^1] + V(0.01f, -0.01f, 0), V(0.03f, 0.026f, 0.026f), Rgb(176, 126, 246), mat: Glow, blend: 0.012f);
        return b;
    }

    // ------------------------------------------------------------------ the three monkeys

    /// <summary>An elemental monkey's evolved build, its crest and its tail's tip taken off and grown taller and leaner, for one of ours.</summary>
    private static PokeBuilder OurMonkey(string name, Element element, Color face)
    {
        var b = Become(ElementMonkey(element switch { Element.Grass => "Simisage", Element.Fire => "Simisear", _ => "Simipour" }, element, true), name, 0.92f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var c = V(0, 0.434f, 0.02f);
        var cream = Rgb(244, 222, 160);
        b.Sdf.RemoveAll(s => s.Bone == head && !(s.Op == SdfOp.Paint && Is(s.Color, cream)) && Vector3.Distance(s.A, c) > 1e-4f);
        b.Sdf.RemoveAll(s => s.Bone == tail && s.A.Y > 0.28f - 0.0005f);
        if (element != Element.Grass) b.Sdf.RemoveAll(s => s.Bone == Body && Is(s.Color, White));
        _ = face;
        Stretch(b, V(0.92f, 1.18f, 0.92f));
        return b;
    }

    /// <summary>Where an evolved monkey's tail runs, stretched as <see cref="OurMonkey"/> stretches it.</summary>
    private static Vector3[] MonkeyTail()
    {
        float k = 1.4f;
        var s = V(0.92f, 1.18f, 0.92f);
        return Smooth(3, V(0, 0.13f * k, -0.03f * k) * s, V(0.06f * k, 0.06f * k, -0.1f * k) * s, V(0.12f * k, 0.12f * k, -0.1f * k) * s, V(0.12f * k, 0.2f * k, -0.06f * k) * s);
    }

    /// <summary>
    /// Simisylvan: Simisage taller and leaner, a crown of branches on its head with leaves at their tips, a mantle of
    /// leaves over its shoulders, bark grown round its forearms, and its tail a bough with leaves and berries.
    /// </summary>
    private static PokeBuilder Simisylvan()
    {
        var b = OurMonkey("Simisylvan", Element.Grass, Rgb(244, 222, 160));
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var bark = Rgb(124, 92, 62);
        var green = Rgb(90, 160, 80);
        var leaf = Rgb(110, 184, 92);
        var c = V(0, 0.512f, 0.026f);
        // The crown of branches, forking twice, a leaf at every twig's end
        PokeBuilder.Both(s =>
        {
            foreach (var (lean, back) in new[] { (0.6f, -0.02f), (0.25f, 0.02f) })
            {
                var root = c + V(0.035f * s * (lean > 0.5f ? 1.2f : 0.5f), 0.085f, back);
                var fork = root + V(0.06f * s * lean + 0.02f * s, 0.11f, back - 0.02f);
                b.Tube(head, Smooth(2, root, root + V(0.02f * s, 0.06f, 0), fork), 0.012f, 0.008f, bark, Shell, 0f);
                foreach (var (dx, dy) in new[] { (0.07f, 0.06f), (-0.01f, 0.09f), (0.04f, 0.1f) })
                {
                    var twig = fork + V(dx * s, dy, -0.01f);
                    b.Limb(head, fork, twig, 0.008f, 0.005f, bark, Shell, 0.003f);
                    Frond(b, head, twig, twig + V(0.02f * s, 0.035f, 0.005f), 0.016f, leaf, V(0, 0, 1f), 0.4f, Leaf, 0.004f);
                }
            }
        });
        // A mantle of leaves over the shoulders and round the neck
        b.Ell(Body, V(0, 0.385f, 0), V(0.1f, 0.05f, 0.085f), green, mat: Leaf, blend: 0.02f);
        for (int ring = 0; ring < 2; ring++)
            for (int i = 0; i < 12; i++)
            {
                float a = (i + ring * 0.5f) / 12f * MathF.Tau;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                var root = V(0, 0.4f - ring * 0.03f, 0) + dir * (0.07f + ring * 0.02f);
                Frond(b, Body, root, root + dir * 0.05f + V(0, -0.05f, 0), 0.026f, ring == 0 ? leaf : green, V(0, 1f, 0) + dir * 0.4f, 0.35f, Leaf, 0.008f);
            }
        // Bark round each forearm
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var sh = V(0.0644f * s, 0.347f, 0.009f);
            var hand = s > 0 ? V(0.1546f, 0.446f, 0.037f) : V(-0.1546f, 0.215f, 0.046f);
            b.Limb(arm, Vector3.Lerp(sh, hand, 0.45f), Vector3.Lerp(sh, hand, 0.82f), 0.021f, 0.02f, bark, Shell, 0.006f);
        });
        // The tail a bough: thick bark, leaves and berries along it and a tuft of leaves at its end
        var tp = MonkeyTail();
        b.Tube(tail, tp, 0.02f, 0.016f, bark, Shell, 0f);
        for (int i = 3; i < tp.Length; i += 2)
        {
            var side = Vector3.Normalize(Vector3.Cross(tp[Math.Min(i + 1, tp.Length - 1)] - tp[i - 1], V(0, 1f, 0)) + V(0, 0.01f, 0));
            float sgn = i % 4 == 1 ? 1f : -1f;
            Frond(b, tail, tp[i] + side * 0.012f * sgn, tp[i] + side * 0.05f * sgn + V(0, 0.02f, 0), 0.018f, leaf, V(0, 1f, 0), 0.4f, Leaf, 0.004f);
            b.Ell(tail, tp[i] - side * 0.018f * sgn + V(0, -0.006f, 0), V(0.012f, 0.012f, 0.012f), i % 3 == 0 ? Rgb(220, 70, 60) : Rgb(240, 140, 60), mat: Shell, blend: 0.004f);
        }
        var tip = tp[^1];
        foreach (var d in new[] { V(0.05f, 0.03f, 0), V(-0.04f, 0.04f, 0.01f), V(0.01f, 0.06f, -0.02f), V(0.02f, 0.04f, 0.04f) })
            Frond(b, tail, tip, tip + d, 0.022f, leaf, V(0, 0, 1f), 0.4f, Leaf, 0.006f);
        b.Ell(tail, tip + V(0.02f, -0.02f, 0.015f), V(0.013f, 0.013f, 0.013f), Rgb(220, 70, 60), mat: Shell, blend: 0.004f);
        return b;
    }

    /// <summary>
    /// Simiscorch: Simisear taller and leaner, rust red, a crown of flames on its head, its forearms and feet sooty
    /// black with glowing embers, a pattern of flame on its chest and its tail a charred torch with a cup of fire.
    /// </summary>
    private static PokeBuilder Simiscorch()
    {
        var b = OurMonkey("Simiscorch", Element.Fire, Rgb(244, 222, 160));
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var rust = Rgb(196, 84, 52);
        var soot = Rgb(62, 56, 58);
        var ember = Rgb(255, 140, 50);
        Recolour(b, (Rgb(222, 90, 80), rust));
        var cream = Rgb(244, 222, 160);
        // Sooty hands and feet
        Tint(b, s => (s.Bone == BoneOf(b, "armL") || s.Bone == BoneOf(b, "armR") || s.Bone == BoneOf(b, "legL") || s.Bone == BoneOf(b, "legR")) && Is(s.Color, cream), soot);
        var c = V(0, 0.512f, 0.026f);
        // A crown of five flames
        FlameRow(b, head, c + V(-0.07f, 0.075f, -0.01f), c + V(0.07f, 0.075f, -0.01f), V(0, 1f, -0.25f), 5, 0.15f, 0.032f, Rgb(236, 70, 40), Rgb(255, 222, 100), 1.4f);
        b.PaintEll(head, c + V(0, 0.03f, 0.06f), V(0.08f, 0.025f, 0.04f), Rgb(70, 52, 50));
        // A flame on its chest
        b.PaintEll(Body, V(0, 0.29f, 0.06f), V(0.05f, 0.06f, 0.03f), Rgb(240, 140, 70));
        // Sooty forearms with embers
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var sh = V(0.0644f * s, 0.347f, 0.009f);
            var hand = s > 0 ? V(0.1546f, 0.446f, 0.037f) : V(-0.1546f, 0.215f, 0.046f);
            b.Limb(arm, Vector3.Lerp(sh, hand, 0.42f), Vector3.Lerp(sh, hand, 0.95f), 0.022f, 0.021f, soot, blend: 0.008f);
            var d = Vector3.Normalize(hand - sh);
            var outward = Vector3.Normalize(Vector3.Cross(d, V(0, 0, 1f))) * -s;
            foreach (float t in new[] { 0.55f, 0.7f, 0.85f })
                b.Ell(arm, Vector3.Lerp(sh, hand, t) + outward * 0.018f + V(0, 0, 0.012f), V(0.006f, 0.009f, 0.006f), ember, mat: Glow, blend: 0.003f);
        });
        // The tail a charred torch, a cup of fire at its end
        var tp = MonkeyTail();
        b.Tube(tail, tp, 0.018f, 0.022f, soot, Shell, 0f);
        var tip = tp[^1];
        b.Torus(tail, tip + V(0, 0.012f, 0), 0.028f, 0.012f, soot, mat: Shell, blend: 0.006f);
        b.Ell(tail, tip + V(0, 0.006f, 0), V(0.028f, 0.012f, 0.028f), soot, mat: Shell, blend: 0.006f);
        FlameTongue(b, tail, tip + V(0, 0.012f, 0), tip + V(0.01f, 0.14f, -0.01f), 0.03f, Rgb(240, 80, 40), Rgb(255, 230, 120));
        return b;
    }

    /// <summary>
    /// Simitorrent: Simipour taller and leaner, marked with teal swirls, a crown of three jets of water, cuffs of foam
    /// round its wrists and its tail a waterfall that arches over and pours down to a pool of foam behind it.
    /// </summary>
    private static PokeBuilder Simitorrent()
    {
        var b = OurMonkey("Simitorrent", Element.Water, Rgb(244, 222, 160));
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var blue = Rgb(40, 140, 200);
        var teal = Rgb(80, 210, 196);
        var water = Rgb(150, 212, 240);
        var foam = Rgb(236, 248, 252);
        Recolour(b, (Rgb(80, 170, 220), blue));
        var c = V(0, 0.512f, 0.026f);
        // Teal swirls over its body
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.04f * s, 0.3f, 0.05f), V(0.012f, 0.05f, 0.03f), teal, V(0, 0, 30f * s));
            b.PaintEll(Body, V(0.06f * s, 0.24f, 0.0f), V(0.012f, 0.04f, 0.04f), teal, V(0, 0, -20f * s));
        });
        // Three jets on its head, rising and spilling over
        foreach (var x in new[] { -1f, 0f, 1f })
        {
            var root = c + V(0.03f * x, 0.08f, -0.005f);
            var path = Smooth(3, root, root + V(0.02f * x, 0.08f, 0), root + V(0.05f * x + 0.01f, 0.15f, -0.01f), root + V(0.09f * x + 0.03f * (x == 0 ? 1 : x), 0.13f, -0.01f));
            b.Tube(head, path, 0.016f, 0.012f, water, Glow, 0f);
            b.Ell(head, path[^1], V(0.02f, 0.014f, 0.016f), foam, mat: Glow, blend: 0.006f);
        }
        // Cuffs of foam round its wrists
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var sh = V(0.0644f * s, 0.347f, 0.009f);
            var hand = s > 0 ? V(0.1546f, 0.446f, 0.037f) : V(-0.1546f, 0.215f, 0.046f);
            var wrist = Vector3.Lerp(sh, hand, 0.82f);
            var d = Vector3.Normalize(hand - sh);
            b.Torus(arm, wrist, 0.02f, 0.012f, foam, Euler(d), blend: 0.006f);
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * MathF.Tau;
                var u = Vector3.Normalize(Vector3.Cross(d, V(0, 0, 1f)));
                var w = Vector3.Cross(d, u);
                b.Ell(arm, wrist + (u * MathF.Cos(a) + w * MathF.Sin(a)) * 0.022f, V(0.012f, 0.012f, 0.012f), foam, blend: 0.006f);
            }
        });
        // The waterfall of a tail: up, over and down to a pool of foam
        var tp = MonkeyTail();
        var tip = tp[^1];
        var fall = Smooth(3, tip - V(0, 0.03f, 0), tip + V(0.01f, 0.08f, -0.03f), tip + V(0.04f, 0.15f, -0.11f), tip + V(0.08f, 0.1f, -0.2f), tip + V(0.1f, -0.06f, -0.24f), tip + V(0.1f, -0.22f, -0.25f), tip + V(0.1f, -0.3f, -0.25f));
        for (int i = 1; i < fall.Length; i++)
        {
            float t = i / (float)(fall.Length - 1);
            float w = 0.016f + 0.05f * t;
            var d = fall[i] - fall[i - 1];
            b.Ell(tail, (fall[i] + fall[i - 1]) / 2f, V(w, d.Length() * 0.7f + 0.012f, 0.016f + 0.006f * t), water, Euler(d), Glow, 0.01f);
        }
        var pool = fall[^1];
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * MathF.Tau;
            b.Ell(tail, pool + V(MathF.Cos(a) * 0.06f, 0.012f, MathF.Sin(a) * 0.04f), V(0.026f, 0.016f, 0.022f), foam, blend: 0.01f);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2518 Somnarna, from Musharna

    /// <summary>
    /// Somnarna: Musharna larger and standing on four short legs, its ears grown broad, its eyes half open and
    /// glowing, a band of night sky with stars across its back and its brow's mist grown into a ring of dream-cloud
    /// that winds round its body.
    /// </summary>
    private static PokeBuilder Somnarna()
    {
        var b = Become(Musharna(), "Somnarna", 0.9f);
        Unlift(b, V(0, 0.22f, 0));
        int tail = BoneOf(b, "tail");
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        var pink = Rgb(246, 196, 214);
        var purple = Rgb(140, 110, 180);
        Recolour(b, (purple, Rgb(214, 160, 206)));
        var c = V(0, 0.22f, 0);
        var r = V(0.12f, 0.12f, 0.17f);
        // Four short legs under it
        foreach (var z in new[] { 0.09f, -0.1f })
            PokeBuilder.Both(s => b.Limb(Body, c + V(0.07f * s, -0.06f, z), c + V(0.075f * s, -0.17f, z + 0.01f), 0.04f, 0.036f, Rgb(214, 160, 206), blend: 0.02f));
        // Broad ears
        PokeBuilder.Both(s => b.Ell(head, c + V(0.1f * s, 0.07f, 0.1f), V(0.05f, 0.045f, 0.015f), pink, V(0, -30f * s, -20f * s), blend: 0.015f));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.Closed = false;
            d.Iris = Rgb(250, 190, 230);
        }
        // A band of night across its back, set with stars
        var night = Rgb(122, 100, 196);
        b.PaintTorus(Body, c, 0.12f, 0.035f, night, V(0, 0, 90f), sx: 1f, sz: 1f);
        foreach (var (y, z) in new[] { (0.9f, 0.1f), (0.75f, -0.15f), (0.98f, -0.05f), (0.6f, 0.05f) })
            PokeBuilder.Both(s =>
            {
                var n = V(0.12f * s, y, z);
                b.Mark(Body, Out(c, r, default, n), n, 0.014f, 0.014f, Rgb(255, 236, 140), MarkShape.Star5);
            });
        // The ring of dream-cloud: puffs of pink and lavender round its body, passing into its side behind
        var lavender = Rgb(200, 170, 236);
        int puffs = 30;
        for (int i = 0; i < puffs; i++)
        {
            float a = i / (float)puffs * MathF.Tau;
            var at = c + V(MathF.Cos(a) * 0.14f, -0.065f + MathF.Sin(a * 2f) * 0.012f, MathF.Sin(a) * 0.19f);
            float rr = 0.038f + 0.006f * MathF.Sin(a * 3f);
            b.Ell(tail, at, V(rr, rr * 0.85f, rr), i % 2 == 0 ? Rgb(244, 176, 220) : lavender, mat: Glow, blend: 0.02f);
        }
        b.Model.Hovers = false;
        b.Ground(0f);
        return b;
    }

    // ------------------------------------------------------------------ 2523 Stormstrika, from Zebstrika

    /// <summary>
    /// Stormstrika: Zebstrika larger, its mane grown into two long forked bolts of white lightning that stream back
    /// from its neck, its tail a bigger forked bolt and rings of sparks round its hooves.
    /// </summary>
    private static PokeBuilder Stormstrika()
    {
        var b = Become(Zebstrika(), "Stormstrika", 0.95f);
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var bolt = Rgb(226, 236, 255);
        var spark = Rgb(150, 200, 255);
        Bolt(b, head, new[] { V(0, 0.7f, 0.2f), V(0.01f, 0.82f, 0.13f), V(0.03f, 0.79f, 0.03f), V(0.04f, 0.92f, -0.07f), V(0.06f, 0.89f, -0.2f), V(0.08f, 1.0f, -0.33f) },
            new[] { 0.05f, 0.045f, 0.04f, 0.032f, 0.022f }, 0.01f, bolt);
        Bolt(b, head, new[] { V(0, 0.62f, 0.12f), V(-0.02f, 0.74f, 0.05f), V(-0.035f, 0.7f, -0.05f), V(-0.05f, 0.81f, -0.16f), V(-0.06f, 0.76f, -0.27f) },
            new[] { 0.045f, 0.04f, 0.03f, 0.02f }, 0.01f, bolt);
        Make(b, s => s.Bone == head && Is(s.Color, bolt), Glow);
        Bolt(b, tail, new[] { V(0, 0.5f, -0.2f), V(0.01f, 0.58f, -0.29f), V(-0.015f, 0.5f, -0.35f), V(0.02f, 0.42f, -0.4f), V(0, 0.3f, -0.42f) },
            new[] { 0.04f, 0.035f, 0.028f, 0.02f }, 0.01f, bolt);
        Bolt(b, tail, new[] { V(-0.015f, 0.5f, -0.35f), V(-0.03f, 0.56f, -0.44f), V(-0.02f, 0.5f, -0.5f) }, new[] { 0.024f, 0.016f }, 0.008f, bolt);
        Make(b, s => s.Bone == tail && Is(s.Color, bolt), Glow);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                var hoof = V(0.0633f * s, 0.06f, (z + 0.01f) * 1.15f);
                b.Torus(leg, hoof, 0.028f, 0.004f, spark, V(8f, 0, 0), mat: Glow, blend: 0.004f);
                b.Torus(leg, hoof + V(0, 0.02f, 0), 0.026f, 0.003f, Rgb(250, 230, 120), V(-6f, 0, 0), mat: Glow, blend: 0.004f);
            });
        return b;
    }

    // ------------------------------------------------------------------ 2528 Amorabat, from Swoobat

    /// <summary>
    /// Amorabat: Swoobat larger and deeper in colour, indigo and violet, great pointed ears pink inside, wings of
    /// maroon membrane, a lilac ruff, its heart nose glowing pink and a long tail of fur like a ribbon, pink at its end.
    /// </summary>
    private static PokeBuilder Amorabat()
    {
        var b = Become(Swoobat(), "Amorabat", 0.86f);
        Unlift(b, V(0, 0.3f, 0));
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var indigo = Rgb(72, 62, 150);
        var navy = Rgb(46, 48, 110);
        var pink = Rgb(230, 150, 160);
        var heart = Rgb(255, 140, 190);
        Recolour(b, (Rgb(80, 120, 190), indigo), (Rgb(170, 200, 220), Rgb(196, 168, 232)), (Rgb(60, 60, 70), navy), (Rgb(140, 180, 220), Rgb(120, 104, 190)));
        b.Sdf.RemoveAll(s => s.Bone == tail);
        b.Model.Decals.RemoveAll(d => d.Bone == head && !d.IsEye && Is(d.Color, Rgb(70, 40, 50)));
        var bc = V(0, 0.28f, 0);
        var c = bc + V(0, 0.13f, 0.02f);
        // Maroon membranes on its wings, the edges left navy
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            b.PaintEll(wing, bc + V(0.22f * s, 0.12f, -0.03f), V(0.1f, 0.075f, 0.06f), Rgb(128, 40, 88));
        });
        // The heart nose glowing, and its heart doubled
        Make(b, s => s.Bone == head && Is(s.Color, pink), Glow);
        Tint(b, s => s.Bone == head && Is(s.Color, pink), heart);
        PokeBuilder.Both(s => b.Ell(head, c + V(0.01f * s, -0.012f, 0.058f), V(0.012f, 0.011f, 0.009f), heart, mat: Glow, blend: 0.004f));
        b.Spike(head, c + V(0, -0.02f, 0.058f), c + V(0, -0.038f, 0.06f), 0.013f, heart, 0.6f, Glow, 0.004f);
        // Great ears, pink inside
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.03f * s, 0.035f, -0.01f);
            int ear = b.Ear(head, s, root);
            CatEar(b, ear, root, c + V(0.085f * s, 0.16f, -0.03f), 0.032f, indigo, Rgb(244, 190, 214));
        });
        // A long tail of fur like a ribbon, pink at its end
        var tp = Smooth(3, bc + V(0, -0.07f, -0.01f), bc + V(0, -0.13f, -0.05f), bc + V(0.05f, -0.2f, -0.03f), bc + V(0.06f, -0.27f, 0.03f), bc + V(-0.01f, -0.3f, 0.07f), bc + V(-0.08f, -0.28f, 0.03f));
        b.Tube(tail, tp, 0.02f, 0.012f, Rgb(110, 80, 170), blend: 0f);
        b.PaintEll(tail, tp[^1], V(0.04f, 0.03f, 0.03f), Rgb(232, 150, 200));
        foreach (var d in new[] { V(-0.05f, 0.005f, -0.02f), V(-0.045f, -0.012f, 0.0f) })
            b.Spike(tail, tp[^1], tp[^1] + d, 0.008f, Rgb(232, 150, 200), blend: 0.004f);
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2530 Excavarch, from Excadrill

    /// <summary>
    /// Excavarch: Excadrill broader and armoured: a miner's helmet of steel with a drill grooved red that points
    /// ahead of it, plates of slate and rust over its back and shoulders, an orange belly and forearm guards, three
    /// steel claws on each hand and a short spiked tail.
    /// </summary>
    private static PokeBuilder Excavarch()
    {
        var b = Become(Excadrill(), "Excavarch", 0.9f);
        int head = BoneOf(b, "head");
        var steel = Rgb(200, 202, 212);
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, steel));
        b.Sdf.RemoveAll(s => (s.Bone == BoneOf(b, "armL") || s.Bone == BoneOf(b, "armR")) && Is(s.Color, steel));
        Stretch(b, V(1.2f, 1.08f, 1.2f));
        var helmet = Rgb(130, 134, 146);
        var slate = Rgb(72, 80, 92);
        var rust = Rgb(152, 76, 56);
        var orange = Rgb(206, 132, 54);
        var red = Rgb(170, 40, 50);
        var brown = Rgb(80, 66, 66);
        var c = V(0, 0.41f, 0.084f);
        var bc = V(0, 0.238f, 0);
        var br = V(0.144f, 0.162f, 0.132f);
        // The helmet and its drill
        b.Ell(head, c + V(0, 0.035f, -0.008f), V(0.086f, 0.052f, 0.086f), helmet, mat: Metal, blend: 0.01f);
        b.Torus(head, c + V(0, 0.022f, 0), 0.084f, 0.008f, helmet, mat: Metal, blend: 0.004f);
        Drill(b, head, c + V(0, 0.07f, 0.02f), c + V(0, 0.12f, 0.3f), 0.052f, Steel, red, 5);
        // Plates over its back and shoulders
        foreach (var (x, y, z, col) in new[] { (0f, 0.6f, -0.8f, slate), (0.5f, 0.5f, -0.7f, rust), (-0.5f, 0.5f, -0.7f, rust), (0f, 0.1f, -1f, rust), (0.6f, 0f, -0.8f, slate), (-0.6f, 0f, -0.8f, slate), (0f, 0.95f, -0.3f, slate), (0f, -0.4f, -0.9f, slate) })
        {
            var n = Vector3.Normalize(V(x, y, z));
            Plate(b, Body, Out(bc, br, default, n), n, V(0.07f, 0.02f, 0.055f), col, Shell);
        }
        PokeBuilder.Both(s => Plate(b, Body, V(0.135f * s, 0.32f, 0.02f), V(s, 0.8f, 0.2f), V(0.06f, 0.024f, 0.06f), rust, Shell));
        b.PaintEll(Body, V(0, 0.19f, 0.11f), V(0.085f, 0.075f, 0.05f), orange);
        b.PaintEll(Body, V(0, 0.29f, 0.11f), V(0.09f, 0.04f, 0.05f), slate);
        // Forearm guards and three claws on each hand
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var sh = V(0.12f * s, 0.302f, 0.024f);
            var hand = V(0.228f * s, 0.259f, 0.108f);
            b.Limb(arm, Vector3.Lerp(sh, hand, 0.5f), hand, 0.043f, 0.04f, orange, Shell, 0.006f);
            foreach (var o in new[] { -0.03f, 0f, 0.03f })
                Blade(b, arm, hand + V(0, o * 0.6f, o * 0.4f), hand + V(0.09f * s, -0.06f + o, 0.12f), 0.03f, Steel, V(0, 1f, 0), 0.35f, Metal);
        });
        // A short tail, spiked along its top
        var tailPath = Smooth(2, V(0, 0.13f, -0.13f), V(0, 0.09f, -0.22f), V(0, 0.05f, -0.3f));
        b.Tube(Body, tailPath, 0.045f, 0.014f, brown, blend: 0.01f);
        foreach (var k in new[] { 1, 2, 3 })
        {
            float rad = 0.045f - 0.031f * k / (tailPath.Length - 1f);
            b.Spike(Body, tailPath[k] + V(0, rad * 0.6f, 0), tailPath[k] + V(0, rad + 0.035f, -0.03f), 0.016f, slate, 0.5f, Shell);
        }
        return b;
    }

    // ------------------------------------------------------------------ 2531 Remedino, from Audino

    /// <summary>
    /// Remedino: Audino taller, a white apron of fur down its front, a red cross on its brow and its ear-feelers grown
    /// long and pink, looping once and ending in the grey disc of a stethoscope.
    /// </summary>
    private static PokeBuilder Remedino()
    {
        var b = Become(Audino(), "Remedino", 0.84f);
        var cream = Rgb(246, 232, 200);
        var pink = Rgb(240, 160, 180);
        int head = BoneOf(b, "head");
        b.Sdf.RemoveAll(s => (s.Bone == BoneOf(b, "earL") || s.Bone == BoneOf(b, "earR")) && Is(s.Color, cream));
        Stretch(b, V(1.1f, 1.15f, 1.1f));
        var c = V(0, 0.46f, 0.022f);
        var r = V(0.088f, 0.0805f, 0.077f);
        b.PaintEll(Body, V(0, 0.245f, 0.085f), V(0.09f, 0.12f, 0.06f), White);
        var red = Rgb(220, 60, 60);
        var at = On(c, r, 0, c.Y + 0.045f);
        b.Mark(head, at, Outward(c, r, at), 0.026f, 0.009f, red, MarkShape.Bar);
        b.Mark(head, at, Outward(c, r, at), 0.026f, 0.009f, red, MarkShape.Bar, 90f);
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var p = Smooth(4, c + V(0.12f * s, 0.02f, -0.005f), c + V(0.17f * s, -0.03f, 0.02f), c + V(0.16f * s, -0.09f, 0.04f), c + V(0.12f * s, -0.07f, 0.045f),
                c + V(0.125f * s, -0.03f, 0.04f), c + V(0.18f * s, -0.07f, 0.035f), c + V(0.2f * s, -0.15f, 0.04f), c + V(0.2f * s, -0.2f, 0.045f));
            b.Tube(ear, p, 0.011f, 0.009f, pink, blend: 0f);
            var end = p[^1] + V(0, -0.018f, 0);
            b.Ell(ear, end, V(0.026f, 0.026f, 0.011f), Rgb(200, 196, 200), V(0, 90f, 0), Metal, 0.004f);
            b.Torus(ear, end, 0.022f, 0.006f, Rgb(150, 146, 156), V(0, 0, 90f), mat: Metal, blend: 0.002f);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2538 Throjutsu, from Throh

    /// <summary>
    /// Throjutsu: Throh taller and heavier, its skin a deep rocky red with knobs on its brow, its gi a dark grey vest
    /// over white trousers, its belt black edged in gold and a second belt tied across its chest.
    /// </summary>
    private static PokeBuilder Throjutsu()
    {
        var b = Become(Throh(), "Throjutsu", 0.98f);
        var red = Rgb(156, 44, 52);
        var gi = Rgb(236, 236, 230);
        var vest = Rgb(76, 76, 80);
        var black = Rgb(36, 34, 38);
        var gold = Rgb(214, 172, 70);
        Recolour(b, (Rgb(210, 80, 70), red));
        Tint(b, s => (s.Bone == BoneOf(b, "armL") || s.Bone == BoneOf(b, "armR")) && Is(s.Color, gi), red);
        Tint(b, s => s.Bone == Body && Is(s.Color, gi), vest);
        Stretch(b, V(1.1f, 1.15f, 1.1f));
        // The vest's white lapels, the gold-edged belt and its knot, and a belt across the chest
        b.PaintEll(Body, V(0.03f, 0.43f, 0.12f), V(0.02f, 0.11f, 0.05f), gi, V(0, 0, -30f));
        b.PaintTorus(Body, V(0, 0.2875f, 0), 0.172f, 0.03f, gold, sz: 0.76f);
        b.PaintTorus(Body, V(0, 0.2875f, 0), 0.172f, 0.021f, black, sz: 0.76f);
        b.PaintTorus(Body, V(0, 0.4f, 0), 0.17f, 0.026f, gold, V(0, 0, 42f), sz: 0.78f);
        b.PaintTorus(Body, V(0, 0.4f, 0), 0.17f, 0.018f, black, V(0, 0, 42f), sz: 0.78f);
        b.Ell(Body, V(-0.02f, 0.44f, 0.13f), V(0.028f, 0.024f, 0.016f), black, blend: 0.006f);
        b.Ell(Body, V(0, 0.2875f, 0.128f), V(0.03f, 0.024f, 0.016f), black, blend: 0.006f);
        PokeBuilder.Both(s => b.Box(Body, V(0.022f * s, 0.21f, 0.128f), V(0.012f, 0.06f, 0.005f), 0.004f, black, V(0, 0, 8f * s), blend: 0.004f));
        // Knobs of rock over its brow and shoulders
        int head = BoneOf(b, "head");
        var c = V(0, 0.575f, 0.055f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.05f * s, 0.06f, 0.0f), c + V(0.075f * s, 0.11f, -0.01f), 0.018f, red, 0.7f);
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            foreach (var o in new[] { V(0.19f, 0.47f, 0.02f), V(0.22f, 0.43f, 0.05f), V(0.24f, 0.36f, 0.06f) })
                b.Ell(arm, V(o.X * s, o.Y, o.Z), V(0.018f, 0.014f, 0.018f), PixelCanvas.Shadow(red, 0.12f), blend: 0.008f);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2539 Sawkendo, from Sawk

    /// <summary>
    /// Sawkendo: Sawk taller and broader, its head rounded under a white headband whose ends fly behind, a white gi
    /// over dark blue trousers, guards of dark hardened skin on its forearms and its black belt tied with long ends.
    /// </summary>
    private static PokeBuilder Sawkendo()
    {
        var b = Become(Sawk(), "Sawkendo", 0.98f);
        var blue = Rgb(66, 124, 204);
        var gi = Rgb(170, 220, 230);
        var white = Rgb(236, 238, 240);
        var navy = Rgb(48, 66, 124);
        var guard = Rgb(56, 58, 66);
        var black = Rgb(40, 38, 42);
        int head = BoneOf(b, "head");
        Recolour(b, (Rgb(60, 110, 190), blue));
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && Is(s.Color, blue));
        Tint(b, s => (s.Bone == BoneOf(b, "armL") || s.Bone == BoneOf(b, "armR")) && Is(s.Color, gi), blue);
        Tint(b, s => (s.Bone == BoneOf(b, "legL") || s.Bone == BoneOf(b, "legR")) && Is(s.Color, gi), navy);
        Tint(b, s => s.Bone == Body && Is(s.Color, gi), white);
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        var c = V(0, 0.661f, 0.045f);
        // The headband and its two ends
        b.Torus(head, c + V(0, 0.035f, -0.004f), 0.062f, 0.011f, white, V(-8f, 0, 0), sx: 1f, sz: 1.08f, mat: SurfaceMaterial.Cloth, blend: 0.004f);
        PokeBuilder.Both(s => Blade(b, head, c + V(0.02f * s, 0.03f, -0.07f), c + V(0.1f * s, -0.01f, -0.13f), 0.022f, white, V(0, 1f, 0.2f), 0.25f, SurfaceMaterial.Cloth));
        // The gi's dark lapels and the belt's long ends
        b.PaintEll(Body, V(0.02f, 0.48f, 0.09f), V(0.014f, 0.1f, 0.04f), navy, V(0, 0, -25f));
        b.Ell(Body, V(0, 0.325f, 0.1f), V(0.03f, 0.026f, 0.016f), black, blend: 0.006f);
        PokeBuilder.Both(s => b.Box(Body, V(0.03f * s, 0.21f, 0.105f), V(0.014f, 0.11f, 0.005f), 0.004f, black, V(4f, 0, 12f * s), SurfaceMaterial.Cloth, 0.004f));
        // Guards of hardened skin on its forearms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var elbow = V(0.19f * s, 0.4256f, 0.056f);
            var hand = s > 0 ? V(0.146f, 0.56f, 0.134f) : V(-0.2f, 0.336f, 0.112f);
            b.Limb(arm, Vector3.Lerp(elbow, hand, 0.1f), Vector3.Lerp(elbow, hand, 0.82f), 0.042f, 0.04f, guard, Shell, 0.006f);
            var d = Vector3.Normalize(hand - elbow);
            var side = Vector3.Normalize(Vector3.Cross(d, V(0, 0, 1f))) * -s;
            foreach (float t in new[] { 0.25f, 0.5f, 0.72f })
                Plate(b, arm, Vector3.Lerp(elbow, hand, t) + side * 0.036f, side, V(0.022f, 0.01f, 0.026f), PixelCanvas.Shadow(guard, 0.15f), Shell);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2547 Cottonimbus, from Whimsicott

    /// <summary>
    /// Cottonimbus: Whimsicott larger, its fur deep green, great ram's horns curled at its temples, floppy green ears,
    /// riding a cloud of cotton that billows round it in cream, its folds tinged pink and green.
    /// </summary>
    private static PokeBuilder Cottonimbus()
    {
        var b = Become(Whimsicott(), "Cottonimbus", 0.82f).Hover();
        var green = Rgb(54, 94, 64);
        var horn = Rgb(110, 150, 84);
        var cream = Rgb(244, 238, 210);
        var pink = Rgb(240, 206, 210);
        var mint = Rgb(196, 214, 160);
        Recolour(b, (Rgb(150, 100, 70), green), (Rgb(80, 170, 90), horn));
        b.Sdf.RemoveAll(s => s.Bone == Body && (Is(s.Color, Rgb(240, 232, 200)) || Is(s.Color, Rgb(250, 246, 226))));
        int head = BoneOf(b, "head");
        var c = V(0, 0.22f, 0.03f);
        // Ram's horns curling at its temples and floppy ears under them
        PokeBuilder.Both(s =>
        {
            Curl(b, head, c + V(0.03f * s, 0.035f, -0.01f), c + V(0.07f * s, 0.07f, -0.02f), V(0, 1f, 0), V(s, 0, -0.3f), 0.04f, 1.2f, 0.018f, horn);
            b.Ell(head, c + V(0.06f * s, -0.005f, 0.0f), V(0.04f, 0.014f, 0.022f), green, V(0, 0, -25f * s), blend: 0.01f);
        });
        b.PaintEll(head, c + V(0, -0.012f, 0.04f), V(0.04f, 0.03f, 0.02f), cream);
        // The cloud it rides: a mass of cotton round its lower half and billowing behind
        var cc = V(0, -0.01f, -0.03f);
        b.Ell(Body, cc, V(0.17f, 0.06f, 0.15f), cream, blend: 0.03f);
        for (int i = 0; i < 14; i++)
        {
            float a = i * MathF.Tau / 14f;
            var at = cc + V(MathF.Sin(a) * 0.16f, 0.01f * MathF.Cos(a * 3f), MathF.Cos(a) * 0.14f - 0.02f);
            b.Ell(Body, at, V(0.07f, 0.06f, 0.07f), i % 4 == 0 ? mint : cream, blend: 0.03f);
        }
        foreach (var o in new[] { V(0.12f, 0.07f, -0.1f), V(-0.13f, 0.07f, -0.08f), V(0.0f, 0.09f, -0.16f), V(0.12f, 0.05f, 0.08f), V(-0.12f, 0.05f, 0.08f) })
            b.Ell(Body, cc + o, V(0.06f, 0.055f, 0.06f), cream, blend: 0.03f);
        b.PaintEll(Body, cc + V(0, -0.06f, 0), V(0.26f, 0.05f, 0.24f), pink);
        b.PaintEll(Body, cc + V(0, -0.02f, 0), V(0.25f, 0.018f, 0.23f), mint);
        Stretch(b, V(1.3f, 1.3f, 1.3f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2549 Ballerilly, from Lilligant

    /// <summary>
    /// Ballerilly: Lilligant grown into a prima ballerina on long pale green legs bound in vines, a tutu of two tiers
    /// (a ruff of leaves over a ring of pink petals), long leafy hair, a crown of three red flowers and long vines
    /// trailing from its arms.
    /// </summary>
    private static PokeBuilder Ballerilly()
    {
        // Built on the dancer's lithe frame of the line (Lilligant's Hisuian build), in the green and red of ours
        var b = Become(LilligantBuild(true), "Ballerilly", 0.9f);
        var pale = Rgb(196, 230, 166);
        var green = Rgb(96, 160, 80);
        var leaf = Rgb(60, 150, 70);
        var pink = Rgb(236, 150, 160);
        var red = Rgb(222, 64, 76);
        Recolour(b, (Rgb(250, 250, 246), pale), (Rgb(230, 120, 170), green), (Rgb(250, 220, 80), green), (Rgb(240, 160, 190), red), (Rgb(200, 70, 120), Rgb(40, 50, 40)));
        Stretch(b, V(1.08f, 1.1f, 1.08f));
        int head = BoneOf(b, "head");
        float neck = 0.49f * 1.1f;
        var c = V(0, neck + 0.055f, 0.0216f);
        // The tutu: a ring of pink petals spread wide, under a ruff of leaves
        var waist = V(0, 0.41f, 0);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, waist + dir * 0.03f - V(0, 0.02f, 0), waist + dir * 0.17f - V(0, 0.07f, 0), 0.07f, pink, V(0, 1f, 0) - dir * 0.3f, 0.12f, Leaf, 0.01f);
        }
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 10f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, waist + dir * 0.03f + V(0, 0.02f, 0), waist + dir * 0.11f - V(0, 0.02f, 0), 0.05f, leaf, V(0, 1f, 0) - dir * 0.4f, 0.15f, Leaf, 0.01f);
        }
        // Vines wound round its legs
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            var hip = V(0.027f * s, 0.4f, 0);
            var foot = V(-0.016f * s, 0.06f, 0);
            Wound(b, leg, new[] { hip - V(0, 0.08f, 0), Vector3.Lerp(hip, foot, 0.5f), foot + V(0, 0.03f, 0) }, 0.02f, 1, 3f, 0.005f, leaf);
        });
        // Long leafy hair falling behind
        foreach (var x in new[] { -0.04f, -0.02f, 0f, 0.02f, 0.04f })
            Frond(b, head, c + V(x, 0.03f, -0.03f), c + V(x * 2.4f, -0.12f, -0.05f), 0.03f, leaf, V(0, 0, -1f), 0.18f, Leaf, 0.006f);
        // Two more flowers beside the one it had: a crown of three
        PokeBuilder.Both(s =>
        {
            var fc = c + V(0.045f * s, 0.045f, -0.01f);
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f;
                var dir = V(MathF.Cos(a), 0.4f, MathF.Sin(a));
                b.Ell(head, fc + dir * 0.022f, V(0.017f, 0.026f, 0.013f), red, Euler(dir), blend: 0.008f);
            }
            b.Ell(head, fc + V(0, 0.01f, 0), V(0.012f, 0.01f, 0.012f), Rgb(250, 220, 80), blend: 0.006f);
        });
        // Long vines trailing from its arms, leaves along them
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = V(0.043f * s, (0.49f - 0.02f) * 1.1f, 0);
            var tip = shoulder + V(0.28f * s, s > 0 ? 0.088f : -0.044f, 0.043f);
            var hang = Smooth(3, tip, tip + V(0.03f * s, -0.08f, 0), tip + V(0.0f, -0.18f, 0.02f), tip + V(0.02f * s, -0.28f, 0));
            b.Tube(arm, hang, 0.007f, 0.005f, leaf, Leaf, 0.004f);
            for (int i = 2; i < hang.Length; i += 3)
                Frond(b, arm, hang[i], hang[i] + V(0.03f * s, -0.01f, 0.01f), 0.014f, leaf, V(0, 0, 1f), 0.3f, Leaf, 0.003f);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2550 Bascurage, from Basculin

    /// <summary>
    /// Bascurage: Basculin longer and darker, all deep green, its stripe split into crossed purple scars, its jaw
    /// heavier and open on rows of teeth, its eye a burning yellow, and a fin of tall jagged rays along its back.
    /// </summary>
    private static PokeBuilder Bascurage()
    {
        var b = Become(Basculin(), "Bascurage", 0.82f);
        Unlift(b, V(0, 0.25f, 0));
        var green = Rgb(54, 96, 66);
        var dark = Rgb(36, 60, 50);
        var fin = Rgb(44, 82, 62);
        Recolour(b, (Rgb(80, 160, 80), green), (Rgb(50, 60, 56), dark), (Rgb(236, 236, 230), fin), (Rgb(220, 60, 50), dark));
        b.Model.Decals.RemoveAll(d => d.Shape == MarkShape.Ring);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.White = Rgb(250, 196, 60);
            d.Pupil = Rgb(170, 40, 30);
        }
        int head = BoneOf(b, "head");
        var mc = V(0, 0.25f, -0.04f);
        var mr = V(0.085f, 0.12f, 0.18f);
        // The jagged fin along its back
        for (int i = 0; i < 7; i++)
        {
            float z = 0.1f - i * 0.045f;
            var at = V(0, 0.34f - i * 0.006f, z);
            Blade(b, Body, at, at + V(0, 0.09f + (i % 2) * 0.05f - i * 0.006f, -0.06f), 0.026f, fin, V(1f, 0, 0), 0.22f);
        }
        // Crossed scars on each flank
        PokeBuilder.Both(s =>
        {
            var n = Vector3.Normalize(V(s, 0.15f, -0.25f));
            var at = Out(mc, mr, default, n);
            b.Mark(Body, at, n, 0.045f, 0.007f, Rgb(120, 40, 76), MarkShape.Bar, 30f);
            b.Mark(Body, at + V(0, 0, 0.012f), n, 0.04f, 0.007f, Rgb(120, 40, 76), MarkShape.Bar, -50f);
        });
        // A heavier jaw and rows of teeth
        b.Ell(head, V(0, 0.17f, 0.15f), V(0.08f, 0.05f, 0.08f), green, blend: 0.02f);
        for (int i = -3; i <= 3; i++)
        {
            float z = 0.215f - MathF.Abs(i) * 0.012f;
            b.Spike(head, V(i * 0.017f, 0.2f, z), V(i * 0.017f, 0.235f, z + 0.004f), 0.007f, Rgb(236, 228, 206), mat: Shell, blend: 0.002f);
            b.Spike(Body, V(i * 0.017f, 0.262f, z - 0.01f), V(i * 0.017f, 0.238f, z - 0.006f), 0.006f, Rgb(236, 228, 206), mat: Shell, blend: 0.002f);
        }
        Stretch(b, V(1.05f, 1.05f, 1.2f));
        return Lift(b);
    }

    /// <summary>Whether bone <paramref name="i"/> of the build has <paramref name="role"/>.</summary>
    private static bool HasRole(PokeBuilder b, int i, PokeRole role) => i >= 0 && i < b.Model.Bones.Count && b.Model.Bones[i].Role == role;

    // ------------------------------------------------------------------ 2555 Darumandala, from Darmanitan

    /// <summary>
    /// Darumandala: Darmanitan larger and seated in meditation, its legs crossed before it and its arms folded over a
    /// round belly marked white and gold like a daruma, its brows of flame swept long and a ring of fire burning
    /// behind its back like a halo.
    /// </summary>
    private static PokeBuilder Darumandala()
    {
        var b = Become(Darmanitan(), "Darumandala", 0.98f);
        var coat = Rgb(186, 50, 56);
        var fur = Rgb(244, 160, 100);
        var flame = Rgb(240, 110, 40);
        var heart = Rgb(255, 214, 110);
        Recolour(b, (Rgb(200, 70, 60), coat), (Rgb(226, 110, 90), Rgb(230, 120, 70)));
        // The arms it walked on are folded now: taken off and laid across its chest
        b.Sdf.RemoveAll(s => s.Bone == BoneOf(b, "legL") || s.Bone == BoneOf(b, "legR"));
        b.Ell(Body, V(0, 0.2f, 0.04f), V(0.18f, 0.17f, 0.16f), coat, blend: 0.03f);
        b.PaintEll(Body, V(0, 0.17f, 0.15f), V(0.13f, 0.12f, 0.07f), Rgb(212, 170, 70));
        b.PaintEll(Body, V(0, 0.17f, 0.155f), V(0.115f, 0.105f, 0.07f), Rgb(244, 240, 228));
        b.Mark(Body, V(0, 0.13f, 0.196f), V(0, -0.1f, 1f), 0.034f, 0.034f, Rgb(212, 170, 70), MarkShape.Ring);
        b.Mark(Body, V(0, 0.13f, 0.197f), V(0, -0.1f, 1f), 0.022f, 0.022f, Rgb(40, 36, 40));
        PokeBuilder.Both(s =>
        {
            // Shins crossed before it
            b.Limb(Body, V(0.16f * s, 0.07f, 0.0f), V(-0.07f * s, 0.055f, 0.19f - 0.02f * s), 0.055f, 0.045f, coat, blend: 0.02f);
            b.Ell(Body, V(-0.09f * s, 0.05f, 0.2f - 0.02f * s), V(0.04f, 0.03f, 0.05f), fur, blend: 0.012f);
            // Arms folded over the chest
            var shoulder = V(0.15f * s, 0.4f, 0.05f);
            var elbow = V(0.18f * s, 0.29f, 0.15f);
            var hand = V(-0.07f * s, 0.32f + 0.025f * s, 0.21f);
            int arm = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Limb(arm, shoulder, elbow, 0.06f, 0.052f, coat, blend: 0.02f);
            b.Limb(arm, elbow, hand, 0.05f, 0.042f, coat, blend: 0.012f);
            b.Ell(arm, hand, V(0.04f, 0.03f, 0.035f), fur, blend: 0.01f);
        });
        // The ring of fire behind its back
        var ring = V(0, 0.45f, -0.19f);
        b.Torus(Body, ring, 0.17f, 0.022f, flame, V(90f, 0, 0), mat: Glow, blend: 0.006f);
        b.Limb(Body, V(0, 0.31f, -0.12f), V(0, 0.285f, -0.19f), 0.024f, 0.022f, flame, Glow, 0.008f);
        for (int i = 0; i < 9; i++)
        {
            float a = (i / 9f) * MathF.Tau + 0.35f;
            var dir = V(MathF.Sin(a), MathF.Cos(a), 0);
            FlameTongue(b, Body, ring + dir * 0.175f, ring + dir * 0.24f + V(0, 0.03f, 0), 0.022f, flame, heart);
        }
        // Brows of flame swept long and back
        int head = BoneOf(b, "head");
        var c = V(0, 0.41f, 0.11f);
        PokeBuilder.Both(s => FlameTongue(b, head, c + V(0.05f * s, 0.06f, 0.05f), c + V(0.21f * s, 0.12f, -0.1f), 0.026f, flame, heart));
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2556 Mariachactus, from Maractus

    /// <summary>
    /// Mariachactus: Maractus grown tall, its trunk brown toward its foot, a sombrero of cactus ringed with flowers
    /// on its head, a second pair of arms with maracas of their own, and the strings of a guitar down its chest
    /// between rows of pale thorns.
    /// </summary>
    private static PokeBuilder Mariachactus()
    {
        var b = Become(Maractus(), "Mariachactus", 0.95f);
        int head = BoneOf(b, "head");
        var green = Rgb(110, 190, 90);
        var olive = Rgb(104, 150, 80);
        var brown = Rgb(170, 116, 74);
        var pink = Rgb(236, 120, 190);
        var thorn = Rgb(246, 210, 80);
        var pale = Rgb(236, 220, 176);
        b.Sdf.RemoveAll(s => s.Bone == head && (Is(s.Color, pink) || (Is(s.Color, thorn) && s.A.Y > 0.47f)));
        Recolour(b, (green, olive));
        Stretch(b, V(1.1f, 1.35f, 1.1f));
        var bc = V(0, 0.3645f, 0);
        var br = V(0.077f, 0.135f, 0.066f);
        var c = V(0, 0.5805f, 0.011f);
        // The trunk browning toward its foot, and its legs
        b.PaintEll(Body, bc + V(0, -0.15f, 0), V(0.1f, 0.08f, 0.1f), brown);
        Tint(b, s => HasRole(b, s.Bone, PokeRole.Leg) && Is(s.Color, olive), brown);
        // The guitar: strings and frets down its chest, a sound hole and rows of pale thorns either side
        foreach (float x in new[] { -0.018f, -0.006f, 0.006f, 0.018f })
            b.Mark(Body, On(bc, br, x, bc.Y + 0.01f), V(x * 3f, 0, 1f), 0.004f, 0.075f, pale, MarkShape.Bar);
        foreach (float y in new[] { 0.06f, 0.03f, 0.0f, -0.03f })
            b.Mark(Body, On(bc, br, 0, bc.Y + y), V(0, 0, 1f), 0.024f, 0.003f, brown, MarkShape.Bar);
        b.Mark(Body, On(bc, br, 0, bc.Y - 0.07f), V(0, -0.2f, 1f), 0.016f, 0.008f, brown, MarkShape.Smile, 180f);
        PokeBuilder.Both(s =>
        {
            foreach (float y in new[] { 0.08f, 0.04f, 0f, -0.04f, -0.08f })
            {
                var at = On(bc, br, 0.035f * s, bc.Y + y);
                var o = Outward(bc, br, at);
                b.Spike(Body, at - o * 0.006f, at + o * 0.03f + V(0.02f * s, 0, 0), 0.008f, pale, mat: Shell, blend: 0.003f);
            }
        });
        // The sombrero, ringed with flowers
        b.Ell(head, c + V(0, 0.075f, -0.005f), V(0.18f, 0.022f, 0.18f), olive, V(-6f, 0, 4f), blend: 0.012f);
        b.Ell(head, c + V(0, 0.115f, -0.01f), V(0.07f, 0.055f, 0.07f), olive, blend: 0.015f);
        var petals = new[] { pink, Rgb(244, 150, 70), Rgb(250, 214, 80), Rgb(240, 110, 120) };
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var at = c + V(MathF.Sin(a) * 0.1f, 0.098f - MathF.Cos(a) * 0.01f, MathF.Cos(a) * 0.1f - 0.01f);
            FlowerHead(b, head, at, V(MathF.Sin(a) * 0.3f, 1f, MathF.Cos(a) * 0.3f), 5, 0.022f, 0.011f, petals[i % 4], thorn, 0.007f);
        }
        // A second pair of arms below the first, each shaking a maraca
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.06f * s, 0.32f, 0.01f);
            var hand = V(0.2f * s, 0.26f, 0.05f);
            b.Limb(Body, shoulder, hand, 0.03f, 0.027f, olive, blend: 0.012f);
            var m = hand + V(0.04f * s, 0.05f, 0.01f);
            b.Ell(Body, hand, V(0.03f, 0.03f, 0.03f), brown, blend: 0.01f);
            b.Ell(Body, m, V(0.042f, 0.052f, 0.042f), olive, V(0, 0, -25f * s), blend: 0.01f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f;
                var d = Vector3.Normalize(V(MathF.Cos(a), 0.6f, MathF.Sin(a)));
                b.Spike(Body, m + d * 0.035f, m + d * 0.062f, 0.008f, pale, mat: Shell, blend: 0.003f);
            }
        });
        return b;
    }

    // ------------------------------------------------------------------ 2558 Crustadel, from Crustle

    /// <summary>
    /// Crustadel: Crustle larger and orange, its stone slab grown into a citadel of three tiers with towers at its
    /// corners, windows in its walls and moss in its cracks, its claws of grey stone and long feelers on its head.
    /// </summary>
    private static PokeBuilder Crustadel()
    {
        var b = Become(Crustle(), "Crustadel", 1f);
        var orange = Rgb(220, 112, 62);
        var leg = Rgb(196, 96, 56);
        var stone = Rgb(140, 132, 116);
        var wall = Rgb(150, 142, 126);
        var moss = Rgb(110, 130, 70);
        var dark = Rgb(70, 50, 46);
        Recolour(b, (Rgb(170, 80, 60), orange));
        Tint(b, s => HasRole(b, s.Bone, PokeRole.Leg) && Is(s.Color, dark), leg);
        Tint(b, s => HasRole(b, s.Bone, PokeRole.Arm) && Is(s.Color, dark), stone);
        Stretch(b, V(1.25f, 1.25f, 1.25f));
        // The citadel: two more tiers on the slab and a tower at each corner
        var rot = V(0, 10f, 0);
        b.Box(Body, V(0, 0.86f, -0.09f), V(0.19f, 0.08f, 0.19f), 0.02f, wall, rot);
        b.Box(Body, V(0, 0.98f, -0.1f), V(0.11f, 0.05f, 0.11f), 0.015f, wall, rot);
        var yaw = Quaternion.CreateFromYawPitchRoll(10f * Degree, 0, 0);
        foreach (var (x, z, h) in new[] { (0.23f, 0.23f, 0.22f), (-0.23f, 0.23f, 0.18f), (0.23f, -0.23f, 0.2f), (-0.23f, -0.23f, 0.24f), (0.1f, -0.1f, 0.28f) })
        {
            var at = V(0, 0, -0.075f) + Vector3.Transform(V(x, 0, z), yaw);
            float bottom = x == 0.1f ? 0.95f : 0.74f;
            b.Limb(Body, V(at.X, bottom, at.Z), V(at.X, 0.8f + h, at.Z), 0.045f, 0.042f, wall, Shell, 0.004f);
            b.Box(Body, V(at.X, 0.82f + h, at.Z), V(0.055f, 0.022f, 0.055f), 0.006f, wall, rot, Shell, 0.004f);
            foreach (var (cx, cz) in new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) })
                b.Box(Body, V(at.X + cx * 0.038f, 0.855f + h, at.Z + cz * 0.038f), V(0.014f, 0.016f, 0.014f), 0.003f, wall, rot, Shell, 0.003f);
        }
        // Windows in its walls and moss in its cracks
        var front = Vector3.Transform(V(0, 0, 1f), yaw);
        var side = Vector3.Transform(V(1f, 0, 0), yaw);
        foreach (var (u, y) in new[] { (-0.14f, 0.55f), (0.0f, 0.62f), (0.14f, 0.5f), (-0.06f, 0.4f), (0.1f, 0.7f) })
            b.Mark(Body, V(0, 0, -0.075f) + front * 0.275f + side * u + V(0, y, 0), front, 0.011f, 0.018f, Rgb(46, 40, 38));
        foreach (var (u, y) in new[] { (-0.1f, 0.6f), (0.12f, 0.45f) })
            PokeBuilder.Both(s => b.Mark(Body, V(0, 0, -0.075f) + side * 0.275f * s + front * u + V(0, y, 0), side * s, 0.011f, 0.018f, Rgb(46, 40, 38)));
        foreach (var at in new[] { V(-0.2f, 0.8f, 0.12f), V(0.18f, 0.66f, 0.18f), V(0.05f, 0.42f, 0.2f), V(-0.24f, 0.5f, -0.1f) })
            b.PaintEll(Body, at, V(0.05f, 0.025f, 0.05f), moss);
        // Long feelers on its head
        int head = BoneOf(b, "head");
        var c = V(0, 0.1875f, 0.2f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.02f * s, 0.02f, 0.02f), c + V(0.06f * s, 0.12f, 0.06f), c + V(0.15f * s, 0.17f, 0.12f)), 0.009f, 0.005f, orange, Shell, 0.004f));
        return b;
    }

    // ------------------------------------------------------------------ 2560 Brawlhood, from Scrafty

    /// <summary>
    /// Brawlhood: Scrafty taller, its crest grown into a dark red pompadour, its shed skin a long pale coat with a
    /// turned-up collar that falls to its ankles open over a grey chest, its arms heavier and its tail longer.
    /// </summary>
    private static PokeBuilder Brawlhood()
    {
        var b = Become(Scrafty(), "Brawlhood", 0.95f);
        int head = BoneOf(b, "head");
        var red = Rgb(196, 70, 60);
        var maroon = Rgb(140, 30, 50);
        var coat = Rgb(232, 214, 168);
        var orange = Rgb(206, 98, 50);
        var gray = Rgb(110, 110, 116);
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, red));
        Recolour(b, (Rgb(226, 186, 100), coat), (Rgb(236, 130, 70), orange));
        Stretch(b, V(1.1f, 1.2f, 1.1f));
        var c = V(0, 0.576f, 0.033f);
        // The pompadour
        b.Ell(head, c + V(0, 0.07f, 0.02f), V(0.07f, 0.045f, 0.075f), maroon, V(-15f, 0, 0), blend: 0.015f);
        foreach (var (x, z) in new[] { (-0.04f, 0.0f), (0f, 0.03f), (0.04f, 0.0f), (0.0f, -0.03f) })
            Blade(b, head, c + V(x, 0.08f, z), c + V(x * 1.4f, 0.13f, z - 0.1f), 0.04f, maroon, V(1f, 0, 0), 0.3f, SurfaceMaterial.Hair);
        Blade(b, head, c + V(0, 0.09f, 0.06f), c + V(0.01f, 0.1f, 0.13f), 0.04f, maroon, V(0, 1f, 0), 0.3f, SurfaceMaterial.Hair);
        // The coat: long, falling to its ankles, open over a grey chest, its collar turned up
        b.Ell(Body, V(0, 0.27f, -0.01f), V(0.12f, 0.23f, 0.1f), coat, mat: SurfaceMaterial.Cloth, blend: 0.03f);
        b.PaintEll(Body, V(0, 0.38f, 0.1f), V(0.03f, 0.1f, 0.04f), gray);
        PokeBuilder.Both(s =>
        {
            b.Box(Body, V(0.05f * s, 0.49f, -0.01f), V(0.04f, 0.05f, 0.006f), 0.004f, coat, V(-20f, 30f * s, 10f * s), SurfaceMaterial.Cloth, 0.008f);
            Blade(b, Body, V(0.1f * s, 0.12f, 0.02f), V(0.14f * s, 0.035f, 0.04f), 0.06f, coat, V(s, 0, 0.3f), 0.25f, SurfaceMaterial.Cloth);
        });
        Blade(b, Body, V(0, 0.12f, -0.1f), V(0, 0.035f, -0.14f), 0.1f, coat, V(0, 0, -1f), 0.25f, SurfaceMaterial.Cloth);
        // Heavier forearms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Limb(arm, V(0.11f * s, 0.33f, 0.03f), V(0.143f * s, 0.264f, 0.055f), 0.032f, 0.03f, orange, blend: 0.01f);
        });
        int tail = BoneOf(b, "tail");
        b.Tube(tail, Smooth(2, V(0, 0.19f, -0.15f), V(0.03f, 0.1f, -0.22f), V(0.06f, 0.08f, -0.3f), V(0.07f, 0.14f, -0.34f)), 0.024f, 0.01f, orange, blend: 0.01f);
        return b;
    }

    // ------------------------------------------------------------------ 2561 Glyphoracle, from Sigilyph

    /// <summary>
    /// Glyphoracle: Sigilyph larger, its patterns grown into rows of golden glyphs, a second closed eye over the
    /// first, its feathers banded gold, teal, magenta and purple, a second pair of wings below the first and its tail
    /// of feathers grown into hanging ribbons.
    /// </summary>
    private static PokeBuilder Glyphoracle()
    {
        var b = Become(Sigilyph(), "Glyphoracle", 0.98f);
        Unlift(b, V(0, 0.42f, 0));
        var black = Rgb(40, 40, 46);
        var gold = Rgb(222, 180, 70);
        var teal = Rgb(40, 168, 150);
        var magenta = Rgb(196, 44, 120);
        var purple = Rgb(110, 60, 160);
        Recolour(b, (Rgb(80, 180, 110), gold), (Rgb(246, 206, 80), gold), (Rgb(110, 170, 230), teal), (Rgb(220, 70, 70), magenta));
        Tint(b, s => s.Bone == BoneOf(b, "tail") && Is(s.Color, gold), purple);
        int head = BoneOf(b, "head");
        var c = V(0, 0.42f, 0);
        var r = V(0.1f, 0.1f, 0.1f);
        var hc = c + V(0, 0.2f, 0.01f);
        var hr = V(0.035f, 0.07f, 0.035f);
        // Rows of glyphs on its body and head
        for (int row = 0; row < 3; row++)
            for (int i = -3; i <= 3; i++)
            {
                var n = Vector3.Normalize(V(i * 0.25f, 0.45f - row * 0.35f, 1f));
                b.Mark(Body, Out(c, r, default, n), n, 0.008f, 0.012f, gold, (i + row) % 3 == 0 ? MarkShape.Diamond : MarkShape.Bar, (i + row) % 2 == 0 ? 0f : 90f);
            }
        b.PaintTorus(head, hc + V(0, -0.045f, 0), 0.03f, 0.005f, gold, sz: 1f);
        // The second eye, shut, above the first
        var second = On(hc, hr, 0, hc.Y + 0.03f);
        b.Mark(head, second, Outward(hc, hr, second), 0.018f, 0.012f, gold, MarkShape.Ring);
        b.Eye(head, second, Outward(hc, hr, second), 0.01f, Rgb(80, 200, 220), closed: true);
        // A second pair of wings below the first
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.075f * s, -0.03f, -0.02f);
            for (int i = 0; i < 3; i++)
            {
                float a = (-28f + i * 16f) * Degree;
                var tip = root + V(MathF.Cos(a) * s, MathF.Sin(a), -0.15f) * (0.22f - i * 0.02f);
                Frond(b, Body, root, tip, 0.02f, gold, V(0, 0, 1f), 0.3f);
                foreach (var (t, col) in new[] { (0.45f, purple), (0.62f, teal), (0.8f, magenta) })
                    b.PaintEll(Body, Vector3.Lerp(root, tip, t), V(0.024f, 0.012f, 0.024f), col, Euler(tip - root), 0.004f);
            }
        });
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2563 Pharaurum, from Cofagrigus

    /// <summary>
    /// Pharaurum: Cofagrigus taller, a pharaoh's golden face under a headdress striped blue and gold with a cobra at
    /// its brow, glowing cyan eyes and a golden beard, its case opened down the front on a glowing inner case, and six
    /// purple shadow hands with claws of silver.
    /// </summary>
    private static PokeBuilder Pharaurum()
    {
        var b = Become(Cofagrigus(), "Pharaurum", 1f);
        Unlift(b, V(0, 0.4f, 0));
        var gold = Rgb(230, 186, 80);
        var blue = Rgb(40, 62, 176);
        var shade = Rgb(96, 72, 160);
        var silver = Rgb(200, 204, 220);
        var cyan = Rgb(110, 240, 220);
        Recolour(b, (Rgb(40, 60, 100), blue), (Rgb(60, 60, 66), shade), (Rgb(40, 36, 44), gold), (Rgb(230, 50, 50), cyan));
        int head = BoneOf(b, "head");
        var c = V(0, 0.4f, 0);
        var hc = c + V(0, 0.23f, 0.03f);
        var hr = V(0.15f, 0.12f, 0.09f);
        // Stripes on the headdress, its two lappets, a cobra at the brow and a beard
        foreach (float y in new[] { -0.02f, 0.015f, 0.055f, 0.095f })
            b.PaintEll(head, hc + V(0, y, 0), V(0.2f, 0.009f, 0.2f), gold, soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            b.Box(head, hc + V(0.11f * s, -0.1f, 0.04f), V(0.035f, 0.07f, 0.02f), 0.012f, blue, V(0, 0, 6f * s), blend: 0.01f);
            foreach (float y in new[] { -0.14f, -0.11f, -0.08f, -0.05f })
                b.PaintEll(head, hc + V(0.11f * s, y, 0.04f), V(0.05f, 0.006f, 0.04f), gold, soft: 0.004f);
        });
        b.Spike(head, hc + V(0, 0.08f, 0.07f), hc + V(0, 0.13f, 0.1f), 0.014f, gold, 0.7f, Metal, 0.004f);
        b.Ell(head, hc + V(0, 0.125f, 0.1f), V(0.014f, 0.012f, 0.01f), gold, mat: Metal, blend: 0.004f);
        b.Limb(head, hc + V(0, -0.09f, 0.08f), hc + V(0, -0.15f, 0.085f), 0.016f, 0.012f, gold, Metal, 0.006f);
        Make(b, s => s.Bone == head && Is(s.Color, cyan), Glow);
        // The case opened down the front on a glowing inner case
        PaintAs(b, Body, c + V(0, -0.05f, 0.1f), V(0.045f, 0.24f, 0.03f), cyan, Glow);
        b.Ell(Body, c + V(0, -0.02f, 0.1f), V(0.03f, 0.15f, 0.018f), gold, mat: Metal, blend: 0.008f);
        b.Ell(Body, c + V(0, 0.13f, 0.1f), V(0.022f, 0.026f, 0.016f), gold, mat: Metal, blend: 0.006f);
        // A third pair of shadow arms, between the others
        PokeBuilder.Both(s =>
        {
            var root = V(0.08f * s, 0.44f, -0.07f);
            int arm = b.Part(s < 0 ? "midArmL" : "midArmR", Body, root, PokeRole.Arm, 0.25f, s);
            var path = Smooth(3, root, root + V(0.13f * s, -0.01f, -0.04f), root + V(0.28f * s, -0.02f, -0.02f), root + V(0.38f * s, 0.02f, 0.02f));
            b.Tube(arm, path, 0.028f, 0.02f, shade, Fur, 0f);
            var d = Vector3.Normalize(path[^1] - path[^2]);
            var palm = path[^1] + d * 0.025f;
            var w = Vector3.Cross(d, Vector3.Normalize(V(0, 0, 1f) - d * d.Z));
            b.Ell(arm, palm, V(0.04f, 0.045f, 0.014f), shade, Euler(d), Fur);
            for (int k = 0; k < 4; k++)
            {
                float a = (-45f + k * 30f) * Degree;
                var f = d * MathF.Cos(a) + w * MathF.Sin(a);
                b.Limb(arm, palm + f * 0.03f, palm + f * 0.09f, 0.011f, 0.008f, shade, Fur, 0.008f);
                b.Spike(arm, palm + f * 0.088f, palm + f * 0.12f, 0.009f, silver, mat: Metal, blend: 0.003f);
            }
        });
        Stretch(b, V(1.12f, 1.15f, 1.12f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2565 Tortugarch, from Carracosta

    /// <summary>
    /// Tortugarch: Carracosta larger and darker, a slate shell ridged with three keels of spikes and spiked round its
    /// rim, its belly a dull tan, its flippers grown into broad paddles studded with plates and its beak hooked.
    /// </summary>
    private static PokeBuilder Tortugarch()
    {
        var b = Become(Carracosta(), "Tortugarch", 0.95f);
        var navy = Rgb(44, 62, 98);
        var blue = Rgb(72, 98, 134);
        var tan = Rgb(188, 178, 148);
        var spike = Rgb(110, 116, 120);
        Recolour(b, (Rgb(56, 80, 140), navy), (Rgb(110, 170, 230), blue), (Rgb(170, 210, 236), tan));
        var sc = V(0, 0.36f, -0.07f);
        var sr = V(0.16f, 0.19f, 0.1f);
        // Three keels of spikes down the shell, and spikes round its rim
        foreach (float x in new[] { -0.55f, 0f, 0.55f })
            for (int i = 0; i < 5; i++)
            {
                var n = Vector3.Normalize(V(x, 0.8f - i * 0.4f, -1f));
                var at = Out(sc, sr, default, n);
                b.Spike(Body, at - n * 0.01f, at + n * 0.05f + V(0, 0.015f, 0), 0.018f, spike, 0.6f, Shell);
            }
        for (int i = 0; i < 12; i++)
        {
            float a = (i / 11f) * MathF.PI;
            var n = Vector3.Normalize(V(MathF.Cos(a), MathF.Sin(a) * 0.9f + 0.1f, -0.2f));
            var at = Out(sc, sr, default, n);
            b.Spike(Body, at - n * 0.01f, at + n * 0.04f, 0.014f, spike, mat: Shell);
        }
        // Broad paddles studded with plates
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var wrist = V(0.24f * s, 0.16f, 0.1f);
            Frond(b, arm, wrist + V(0, 0.02f, 0), V(0.32f * s, 0.0f, 0.26f), 0.1f, blue, V(s, 0, 0.3f), 0.25f, Fur);
            foreach (float t in new[] { 0.3f, 0.55f, 0.8f })
            {
                var at = Vector3.Lerp(V(0.22f * s, 0.3f, 0.06f), wrist, t);
                b.Ell(arm, at + V(0.03f * s, 0.01f, 0.03f), V(0.016f, 0.012f, 0.016f), spike, mat: Shell, blend: 0.006f);
            }
        });
        // A hooked beak
        int head = BoneOf(b, "head");
        var c = V(0, 0.53f, 0.07f);
        b.Spike(head, c + V(0, 0.0f, 0.07f), c + V(0, -0.06f, 0.115f), 0.03f, spike, 0.7f, Shell);
        Stretch(b, V(1.15f, 1.12f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2567 Primavian, from Archeops

    /// <summary>
    /// Primavian: Archeops larger and fully fledged: long blue flight feathers banded red over the yellow of its
    /// wings, a crest of red quills on its head and its tail ending in a broad fan of long blue, red and yellow
    /// feathers.
    /// </summary>
    private static PokeBuilder Primavian()
    {
        var b = Become(Archeops(), "Primavian", 1f);
        Unlift(b, V(0, 0.42f, 0));
        var yellow = Rgb(246, 210, 90);
        var red = Rgb(220, 70, 60);
        var blue = Rgb(46, 92, 184);
        var bc = V(0, 0.42f, 0);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        // Long flight feathers from each wing's bend
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = bc + V(0.06f * s, 0.06f, 0);
            var wrist = bc + V(0.22f * s, 0.16f, 0.01f);
            for (int i = 0; i < 6; i++)
            {
                float a = (40f - i * 16f) * Degree;
                var tip = wrist + V(MathF.Cos(a) * s, MathF.Sin(a) - 0.25f, -0.25f) * (0.3f - i * 0.02f);
                Frond(b, wing, wrist, tip, 0.03f, blue, V(0, 0.2f, 1f), 0.22f);
                b.PaintEll(wing, Vector3.Lerp(wrist, tip, 0.3f), V(0.035f, 0.035f, 0.035f), red, soft: 0.006f);
            }
            for (int i = 0; i < 3; i++)
            {
                var root = Vector3.Lerp(shoulder, wrist, 0.25f + i * 0.25f);
                Frond(b, wing, root, root + V(0.06f * s, -0.2f, -0.1f), 0.03f, blue, V(0, 0.2f, 1f), 0.22f);
                b.PaintEll(wing, root + V(0.02f * s, -0.05f, -0.03f), V(0.03f, 0.04f, 0.03f), red, soft: 0.006f);
            }
        });
        // A crest of red quills
        var c = bc + V(0, 0.23f, 0.16f);
        for (int i = 0; i < 9; i++)
        {
            float x = (i - 4) * 0.012f;
            var root = c + V(x, 0.03f, -0.02f - MathF.Abs(x) * 0.5f);
            b.Spike(head, root, root + V(x * 2f, 0.1f + (i % 3) * 0.02f, -0.09f), 0.01f, red, 0.5f);
        }
        // A fan of long feathers at the tail's end
        var end = bc + V(0.05f, -0.36f, -0.08f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-60f + i * 20f) * Degree;
            var tip = end + V(MathF.Sin(a) * 0.18f, -0.08f, -0.12f - MathF.Cos(a) * 0.08f);
            Frond(b, tail, end, tip, 0.026f, blue, V(0, 1f, 0), 0.25f);
            b.PaintEll(tail, Vector3.Lerp(end, tip, 0.35f), V(0.03f, 0.03f, 0.03f), i % 2 == 0 ? red : yellow, soft: 0.006f);
        }
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2569 Scrapodor, from Garbodor

    /// <summary>
    /// Scrapodor: Garbodor larger and plated in rusted scrap: plates of steel and rust riveted over it, a visor over
    /// glowing green eyes, a crown of pipes letting out green and purple fumes, and its right arm ending in a
    /// digger's toothed scoop.
    /// </summary>
    private static PokeBuilder Scrapodor()
    {
        var b = Become(Garbodor(), "Scrapodor", 1f);
        var steel = Rgb(112, 114, 118);
        var rust = Rgb(152, 92, 58);
        var pipe = Rgb(126, 100, 84);
        var toxic = Rgb(130, 236, 90);
        var fume = Rgb(170, 110, 220);
        Recolour(b, (Rgb(70, 120, 70), steel), (Rgb(150, 136, 116), rust));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.Sclera = false;
            d.White = null;
            d.Iris = toxic;
            d.Glare = true;
        }
        var c = V(0, 0.36f, 0);
        var r = V(0.24f, 0.3f, 0.2f);
        // Plates riveted over it
        foreach (var (x, y, z, col) in new[] { (0.7f, 0.4f, 0.6f, rust), (-0.7f, 0.4f, 0.6f, steel), (0.5f, -0.3f, 0.8f, steel), (-0.5f, -0.3f, 0.8f, rust), (0f, 0.6f, -0.8f, rust), (0.8f, 0f, -0.5f, steel), (-0.8f, 0f, -0.5f, rust), (0f, -0.2f, -1f, steel) })
        {
            var n = Vector3.Normalize(V(x, y, z));
            Plate(b, Body, Out(c, r, default, n), n, V(0.08f, 0.018f, 0.07f), col, Metal);
        }
        PokeBuilder.Both(s => b.Mark(Body, Out(c, r, default, V(0.35f * s, -0.1f, 1f)), V(0.35f * s, -0.1f, 1f), 0.016f, 0.016f, s > 0 ? Rgb(150, 90, 200) : Rgb(220, 70, 70)));
        // The visor over its eyes
        var visor = On(c, r, 0, c.Y + 0.15f);
        b.Box(Body, visor + V(0, 0, -0.01f), V(0.13f, 0.026f, 0.02f), 0.006f, Rgb(70, 66, 80), V(-10f, 0, 0), Metal, 0.006f);
        b.Model.Decals.RemoveAll(d => d.IsEye);
        PokeBuilder.Both(s => b.Eye(Body, visor + V(0.055f * s, 0, 0.012f), V(0.2f * s, 0.1f, 1f), 0.016f, toxic, glare: true));
        // The crown of pipes, two of them fuming
        int head = BoneOf(b, "head");
        foreach (var (x, z, h, fumes) in new[] { (-0.1f, -0.06f, 0.14f, false), (-0.03f, 0.02f, 0.2f, true), (0.05f, -0.04f, 0.17f, false), (0.13f, 0.0f, 0.12f, true), (0.0f, -0.12f, 0.15f, false) })
        {
            var root = V(x, c.Y + r.Y - 0.04f, z);
            var top = root + V(x * 0.3f, h, z * 0.3f);
            b.Limb(head, root, top, 0.02f, 0.018f, pipe, Metal, 0.006f);
            b.Torus(head, top, 0.02f, 0.006f, pipe, mat: Metal, blend: 0.003f);
            if (fumes)
                foreach (var (o, rr, col) in new[] { (V(0, 0.03f, 0), 0.025f, toxic), (V(0.03f, 0.07f, -0.01f), 0.03f, fume), (V(0.0f, 0.11f, -0.02f), 0.026f, fume) })
                    b.Ell(head, top + o, V(rr, rr * 0.85f, rr), col, mat: Glow, blend: 0.015f);
        }
        // The digger's scoop on its right arm, where the pipe was
        int arm = BoneOf(b, "armR");
        var shoulder = c + V(0.2f, 0.08f, 0);
        var end = shoulder + V(0.22f, -0.24f, 0.1f);
        b.Sdf.RemoveAll(s => s.Bone == arm && s.Material == Metal);
        var scoop = end + V(0.04f, -0.08f, 0.04f);
        b.Box(arm, scoop, V(0.07f, 0.06f, 0.05f), 0.015f, Rgb(196, 140, 60), V(0, -20f, 15f), Metal, 0.01f);
        for (int i = -2; i <= 2; i++)
            b.Spike(arm, scoop + V(i * 0.026f, -0.05f, 0.04f), scoop + V(i * 0.026f, -0.1f, 0.06f), 0.012f, Rgb(176, 120, 50), mat: Metal, blend: 0.004f);
        Stretch(b, V(1.1f, 1.1f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2571 Zoromirage, from Zoroark

    /// <summary>
    /// Zoromirage: Zoroark taller, its great mane grown dark and split into nine maroon locks that stream out round
    /// it, each with a teal bead, a teal mark on its brow and teal eyes.
    /// </summary>
    private static PokeBuilder Zoromirage()
    {
        var b = Become(Zoroark(), "Zoromirage", 1f);
        var mane = Rgb(64, 60, 72);
        var maroon = Rgb(124, 30, 52);
        var teal = Rgb(60, 200, 190);
        Recolour(b, (Rgb(170, 40, 56), mane), (Rgb(180, 40, 56), maroon), (Rgb(70, 170, 230), teal));
        int tail = BoneOf(b, "tail");
        int head = BoneOf(b, "head");
        var mc = V(0, 0.69f, -0.05f);
        // Nine locks streaming out of the mane, each with a teal bead near its end
        for (int i = 0; i < 9; i++)
        {
            float a = (-80f + i * 20f) * Degree;
            var dir = Vector3.Normalize(V(MathF.Sin(a), -0.35f - (i % 3) * 0.2f, -MathF.Cos(a) * 0.8f - 0.3f));
            var root = mc + dir * 0.09f;
            var path = Smooth(3, root, root + dir * 0.14f + V(0, 0.04f, 0), root + dir * 0.28f - V(0, 0.04f, 0), root + dir * 0.38f - V(0, 0.1f, 0));
            b.Tube(tail, path, 0.035f, 0.014f, maroon, blend: 0.01f);
            b.Ell(tail, Vector3.Lerp(path[^4], path[^3], 0.5f), V(0.02f, 0.02f, 0.02f), teal, mat: Shell, blend: 0.005f);
        }
        var c = V(0, 0.68f, 0.06f);
        var r = V(0.055f, 0.05f, 0.06f);
        var brow = On(c, r, 0, c.Y + 0.03f);
        b.Mark(head, brow, Outward(c, r, brow), 0.012f, 0.018f, teal, MarkShape.Diamond);
        Stretch(b, V(1f, 1.12f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2573 Chinchillux, from Cinccino

    /// <summary>
    /// Chinchillux: Cinccino longer, its scarves of white fur grown into a long stole that falls from its shoulders
    /// and trails behind it, pale pink tufts at its ears and its tail a great plume with a pale glow at its end.
    /// </summary>
    private static PokeBuilder Chinchillux()
    {
        var b = Become(Cinccino(), "Chinchillux", 0.7f);
        var white = Rgb(246, 246, 250);
        var cream = Rgb(244, 238, 222);
        var pink = Rgb(242, 214, 222);
        int tail = BoneOf(b, "tail");
        // The stole: falls from both shoulders over the front and trails out behind
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var stole = Smooth(3, V(0.03f * s, 0.21f, 0.03f), V(0.06f * s, 0.16f, 0.06f), V(0.08f * s, 0.08f, 0.04f), V(0.1f * s, 0.03f, -0.06f), V(0.08f * s, 0.012f, -0.2f), V(0.03f * s, 0.01f, -0.32f));
            Ribbon(b, arm, stole, 0.05f, cream, V(s, 0.6f, 0.2f), 0.012f);
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var tip = V(0.09f * s, 0.355f, 0.0f);
            foreach (var d in new[] { V(0.04f, 0.05f, -0.01f), V(0.0f, 0.06f, -0.02f), V(0.05f, 0.03f, 0.0f) })
                b.Spike(ear, tip, tip + V(d.X * s, d.Y, d.Z), 0.008f, pink, blend: 0.004f);
        });
        // The plume of a tail, glowing at its end
        var tp = Smooth(3, V(0, 0.1f, -0.04f), V(0.05f, 0.12f, -0.14f), V(0.1f, 0.22f, -0.18f), V(0.09f, 0.33f, -0.14f), V(0.04f, 0.38f, -0.1f));
        b.Tube(tail, tp, 0.022f, 0.05f, white, blend: 0.02f);
        b.Ell(tail, tp[^1], V(0.05f, 0.045f, 0.045f), Rgb(250, 248, 220), mat: Glow, blend: 0.02f);
        Stretch(b, V(1.05f, 1.1f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2581 Cygnelle, from Swanna

    /// <summary>
    /// Cygnelle: Swanna larger, a crown of blue and pale cyan feathers on its head, its wings raised and its trailing
    /// feathers grown long like a gown, white fading through teal to deep blue at their ends.
    /// </summary>
    private static PokeBuilder Cygnelle()
    {
        var b = Become(Swanna(), "Cygnelle", 1f);
        var white = Rgb(244, 246, 250);
        var teal = Rgb(110, 200, 196);
        var blue = Rgb(46, 92, 196);
        var cyan = Rgb(150, 220, 236);
        int head = BoneOf(b, "head");
        var bc = V(0, 0.25f, -0.04f);
        var c = V(0, 0.6f, 0.12f);
        // The crown of feathers
        for (int i = 0; i < 9; i++)
        {
            float a = (-60f + i * 15f) * Degree;
            var root = c + V(MathF.Sin(a) * 0.012f, 0.025f, -0.01f);
            var tip = root + V(MathF.Sin(a) * 0.06f, 0.09f - MathF.Abs(a) * 0.02f, -0.02f);
            Frond(b, head, root, tip, 0.016f, i % 2 == 0 ? blue : cyan, V(0, 0, 1f), 0.3f);
        }
        // Raised wings, and the long gown of feathers
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = bc + V(0.08f * s, 0.05f, 0.04f);
            for (int i = 0; i < 4; i++)
            {
                var tip = shoulder + V((0.12f + i * 0.03f) * s, 0.24f - i * 0.07f, -0.08f - i * 0.04f);
                Frond(b, wing, shoulder, tip, 0.045f, white, V(s, 0.2f, 0.4f), 0.22f);
                b.PaintEll(wing, Vector3.Lerp(shoulder, tip, 0.9f), V(0.04f, 0.04f, 0.04f), teal, soft: 0.008f);
            }
            for (int i = 0; i < 4; i++)
            {
                var root = bc + V((0.06f + 0.012f * i) * s, -0.02f, -0.04f - i * 0.04f);
                var tip = root + V((0.06f + i * 0.03f) * s, -0.21f, -0.12f - i * 0.05f);
                Frond(b, Body, root, tip, 0.05f, white, V(s, 0.3f, -0.3f), 0.2f);
                b.PaintEll(Body, Vector3.Lerp(root, tip, 0.65f), V(0.05f, 0.045f, 0.05f), teal, soft: 0.01f);
                b.PaintEll(Body, Vector3.Lerp(root, tip, 0.95f), V(0.05f, 0.03f, 0.05f), blue, soft: 0.008f);
            }
        });
        for (int i = 0; i < 3; i++)
        {
            var root = bc + V((i - 1) * 0.04f, 0.0f, -0.14f);
            var tip = root + V((i - 1) * 0.06f, -0.2f, -0.24f);
            Frond(b, Body, root, tip, 0.05f, white, V(0, 1f, -0.4f), 0.2f);
            b.PaintEll(Body, Vector3.Lerp(root, tip, 0.65f), V(0.05f, 0.045f, 0.05f), teal, soft: 0.01f);
            b.PaintEll(Body, Vector3.Lerp(root, tip, 0.95f), V(0.05f, 0.03f, 0.05f), blue, soft: 0.008f);
        }
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2586 Seasonstag, from Sawsbuck

    /// <summary>
    /// Seasonstag: Sawsbuck larger, its coat a single deep brown with a white chest, and its antlers grown into four
    /// great branches each in a season of its own: blossom and green leaves on its left, red leaves and snow hung with
    /// icicles on its right.
    /// </summary>
    private static PokeBuilder Seasonstag()
    {
        var b = Become(Sawsbuck(), "Seasonstag", 1f);
        int head = BoneOf(b, "head");
        var blossom = Rgb(240, 160, 190);
        b.Sdf.RemoveAll(s => s.Bone == head && (Is(s.Color, blossom) || Is(s.Color, Rgb(250, 220, 120))));
        var brown = Rgb(108, 72, 50);
        var white = Rgb(242, 238, 230);
        var antler = Rgb(84, 62, 48);
        Recolour(b, (Rgb(130, 90, 60), brown), (Rgb(232, 206, 150), white));
        b.PaintEll(Body, V(0, 0.55f, 0.16f), V(0.07f, 0.09f, 0.06f), white);
        var c = V(0, 0.78f, 0.27f);
        PokeBuilder.Both(s =>
        {
            var a1 = c + V(0.07f * s, 0.14f, -0.05f);
            var a2 = c + V(0.12f * s, 0.25f, -0.06f);
            // The outer branch: spring on the left, winter on the right; the upper branch: summer and autumn
            var outer = new[] { a1, a1 + V(0.1f * s, 0.06f, 0.01f), a1 + V(0.22f * s, 0.07f, 0.02f), a1 + V(0.3f * s, 0.14f, 0.0f) };
            var upper = new[] { a2, a2 + V(0.03f * s, 0.08f, -0.01f), a2 + V(0.06f * s, 0.16f, -0.02f) };
            var ob = Smooth(2, outer);
            var ub = Smooth(2, upper);
            b.Tube(head, ob, 0.013f, 0.007f, s > 0 ? Rgb(60, 56, 60) : antler, Shell, 0.004f);
            b.Tube(head, ub, 0.013f, 0.008f, antler, Shell, 0.004f);
            var tines = new[] { (outer[1], V(0.02f * s, 0.07f, 0.02f)), (outer[2], V(0.0f, 0.08f, -0.02f)), (outer[2], V(0.05f * s, -0.04f, 0.02f)) };
            foreach (var (from, d) in tines)
                b.Limb(head, from, from + d, 0.007f, 0.005f, s > 0 ? Rgb(60, 56, 60) : antler, Shell, 0.003f);
            if (s < 0)
            {
                // Spring: blossom along the branch
                foreach (var at in new[] { outer[1] + V(0, 0.01f, 0.01f), outer[2] + V(0, 0.08f, -0.02f), outer[3], outer[1] + V(0.02f * s, 0.07f, 0.02f), outer[2] + V(0.05f * s, -0.04f, 0.02f) })
                    FlowerHead(b, head, at, V(0.3f * s, 0.6f, 1f), 5, 0.024f, 0.013f, blossom, Rgb(250, 220, 120), 0.007f);
                // Summer: a crown of green leaves
                Lumps(b, head, ub[^1] + V(0.02f * s, 0.02f, 0), V(0.09f, 0.07f, 0.07f), 12, 0.035f, Rgb(54, 128, 60), Rgb(90, 166, 80), 10f);
                b.Ell(head, ub[^1] + V(0.02f * s, 0.02f, 0), V(0.07f, 0.055f, 0.055f), Rgb(54, 128, 60), mat: Leaf);
            }
            else
            {
                // Winter: snow along the branch, icicles hanging under it
                foreach (var at in new[] { outer[1], outer[2], outer[3] })
                {
                    b.Ell(head, at + V(0, 0.008f, 0), V(0.03f, 0.01f, 0.016f), Rgb(246, 248, 252), blend: 0.006f);
                    b.Spike(head, at - V(0, 0.004f, 0), at + V(0, -0.045f, 0), 0.006f, Rgb(200, 230, 246), mat: Glow, blend: 0.003f);
                }
                // Autumn: red and orange leaves
                Lumps(b, head, ub[^1] + V(0.02f * s, 0.02f, 0), V(0.08f, 0.08f, 0.06f), 12, 0.033f, Rgb(212, 84, 50), Rgb(236, 150, 60), 10f);
                b.Ell(head, ub[^1] + V(0.02f * s, 0.02f, 0), V(0.06f, 0.06f, 0.05f), Rgb(212, 84, 50), mat: Leaf);
            }
        });
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2587 Nimbolga, from Emolga

    /// <summary>
    /// Nimbolga: Emolga larger and gliding, its membrane stretched into a kite's diamond quartered yellow and white in
    /// a black frame, its cheeks grown round and yellow, and its tail trailing a string of sparks strung with small
    /// clouds.
    /// </summary>
    private static PokeBuilder Nimbolga()
    {
        var b = Become(Emolga(), "Nimbolga", 0.82f).Hover();
        var yellow = Rgb(250, 214, 80);
        var white = Rgb(246, 246, 248);
        var black = Rgb(50, 50, 56);
        var cloud = Rgb(226, 230, 238);
        b.Sdf.RemoveAll(s => HasRole(b, s.Bone, PokeRole.Arm) && Is(s.Color, yellow));
        // The kite: a diamond on each side, quartered, in a black frame
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var top = V(0.115f * s, 0.205f, -0.01f);
            var outer = V(0.2f * s, 0.11f, -0.01f);
            var bottom = V(0.055f * s, 0.03f, -0.01f);
            var inner = V(0.045f * s, 0.13f, -0.01f);
            var m = (top + outer + bottom + inner) / 4f;
            Panel(b, arm, top, outer, m, -0.01f, yellow);
            Panel(b, arm, outer, bottom, m, -0.01f, white);
            Panel(b, arm, bottom, inner, m, -0.01f, yellow);
            Panel(b, arm, inner, top, m, -0.01f, white);
            b.Tube(arm, new[] { top, outer, bottom }, 0.008f, 0.008f, black, blend: 0.004f);
        });
        // Round cheeks
        int head = BoneOf(b, "head");
        var c = V(0, 0.245f, 0.015f);
        PokeBuilder.Both(s => b.Ell(head, c + V(0.06f * s, -0.02f, 0.02f), V(0.032f, 0.03f, 0.028f), yellow, mat: Shell, blend: 0.01f));
        // The string of sparks and clouds from its tail
        int tail = BoneOf(b, "tail");
        var path = Smooth(3, V(0, 0.17f, -0.14f), V(0.06f, 0.1f, -0.2f), V(0.14f, 0.04f, -0.24f), V(0.22f, 0.08f, -0.26f), V(0.24f, 0.18f, -0.28f), V(0.3f, 0.24f, -0.3f));
        b.Tube(tail, path, 0.005f, 0.004f, Rgb(255, 230, 90), Glow, 0.003f);
        for (int i = 3; i < path.Length; i += 3)
            foreach (var o in new[] { V(0, 0.008f, 0), V(0.016f, 0, 0.004f), V(-0.016f, 0, -0.004f) })
                b.Ell(tail, path[i] + o, V(0.018f, 0.014f, 0.016f), cloud, blend: 0.01f);
        Stretch(b, V(1.2f, 1.2f, 1.2f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2589 Lancelier, from Escavalier

    /// <summary>
    /// Lancelier: Escavalier taller, its lance longer and fluted, two horns rising from its helmet beside a great red
    /// plume, spikes on its shoulders, a shield of shell on its left arm and a red cape falling behind.
    /// </summary>
    private static PokeBuilder Lancelier()
    {
        var b = Become(Escavalier(), "Lancelier", 0.95f);
        Unlift(b, V(0, 0.36f, 0));
        var steel = Rgb(170, 172, 180);
        var red = Rgb(186, 40, 52);
        var shell = Rgb(156, 132, 98);
        int head = BoneOf(b, "head");
        var bc = V(0, 0.36f, 0);
        var c = bc + V(0, 0.17f, 0.03f);
        Recolour(b, (Rgb(210, 70, 80), red));
        // The plume, fuller, and two horns
        Ribbon(b, head, Smooth(3, c + V(0.02f, 0.05f, 0.0f), c + V(0.03f, 0.14f, -0.04f), c + V(0.03f, 0.15f, -0.14f), c + V(0.02f, 0.08f, -0.22f)), 0.07f, red, V(1f, 0, 0), 0.026f);
        Ribbon(b, head, Smooth(3, c + V(-0.02f, 0.05f, 0.0f), c + V(-0.03f, 0.13f, -0.05f), c + V(-0.03f, 0.12f, -0.15f), c + V(-0.02f, 0.04f, -0.22f)), 0.07f, red, V(1f, 0, 0), 0.026f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(2, c + V(0.03f * s, 0.04f, 0.0f), c + V(0.05f * s, 0.1f, 0.0f), c + V(0.06f * s, 0.16f, -0.02f)), 0.01f, 0.004f, Rgb(200, 196, 180), Shell, 0.004f));
        // Spikes on its shoulders
        PokeBuilder.Both(s =>
        {
            var sc = bc + V(0.11f * s, 0.07f, -0.01f);
            foreach (var d in new[] { V(0.6f * s, 0.8f, 0), V(1f * s, 0.2f, 0.2f) })
                b.Spike(Body, sc + Vector3.Normalize(d) * 0.05f, sc + Vector3.Normalize(d) * 0.09f, 0.012f, steel, mat: Metal, blend: 0.003f);
        });
        // A longer, fluted lance
        int lanceArm = BoneOf(b, "armR");
        var hand = bc + V(0.14f, -0.02f, 0.1f);
        var tip = hand + V(0.04f, -0.12f, 0.3f);
        var far = hand + (tip - hand) * 1.4f;
        b.Spike(lanceArm, Vector3.Lerp(hand, tip, 0.6f), far, 0.022f, steel, mat: Metal, blend: 0.006f);
        // The shield on its left arm
        int shieldArm = BoneOf(b, "armL");
        var sh = bc + V(-0.19f, -0.01f, 0.1f);
        b.Ell(shieldArm, sh, V(0.018f, 0.12f, 0.085f), steel, mat: Metal, blend: 0.008f);
        b.Ell(shieldArm, sh + V(-0.008f, 0, 0), V(0.014f, 0.11f, 0.076f), shell, mat: Shell, blend: 0.004f);
        b.PaintEll(shieldArm, sh + V(-0.02f, 0.02f, 0), V(0.02f, 0.07f, 0.012f), red);
        b.PaintEll(shieldArm, sh + V(-0.02f, -0.02f, 0.03f), V(0.02f, 0.012f, 0.04f), red);
        b.Spike(shieldArm, sh + V(-0.012f, 0.1f, 0), sh + V(-0.012f, 0.15f, 0), 0.012f, steel, mat: Metal, blend: 0.003f);
        // A red cape falling behind
        PokeBuilder.Both(s => Frond(b, Body, bc + V(0.06f * s, 0.08f, -0.07f), bc + V(0.12f * s, -0.28f, -0.14f), 0.09f, red, V(0, 0.2f, -1f), 0.15f, SurfaceMaterial.Cloth, 0.01f));
        Frond(b, Body, bc + V(0, 0.08f, -0.08f), bc + V(0, -0.3f, -0.17f), 0.1f, red, V(0, 0.2f, -1f), 0.15f, SurfaceMaterial.Cloth, 0.01f);
        Stretch(b, V(1.1f, 1.15f, 1.1f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2591 Sporemonarch, from Amoonguss

    /// <summary>
    /// Sporemonarch: Amoonguss larger, its cap grown broad and red with pale spots and purple gills under its rim, a
    /// frilled collar round its neck, glowing yellow eyes, its arms let fall, and a ring of small pink toadstools
    /// round its foot on a ring of root.
    /// </summary>
    private static PokeBuilder Sporemonarch()
    {
        var b = Become(Amoonguss(), "Sporemonarch", 0.92f);
        int head = BoneOf(b, "head");
        var stalk = Rgb(196, 210, 180);
        var red = Rgb(150, 34, 52);
        var spot = Rgb(240, 190, 200);
        var purple = Rgb(130, 60, 140);
        var pink = Rgb(236, 120, 210);
        // Its arms let fall, the caps they held gone
        b.Sdf.RemoveAll(s => HasRole(b, s.Bone, PokeRole.Arm) && !Is(s.Color, Rgb(150, 170, 150)));
        b.Model.Decals.RemoveAll(d => HasRole(b, d.Bone, PokeRole.Arm));
        Recolour(b, (Rgb(150, 170, 150), stalk));
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = Rgb(250, 230, 90);
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Ell(arm, V(0.21f * s, 0.14f, 0.03f), V(0.03f, 0.035f, 0.03f), stalk, blend: 0.01f);
        });
        // The broad cap, spotted, purple under its rim
        var c = V(0, 0.34f, 0);
        b.Ell(head, c + V(0, 0.02f, 0), V(0.25f, 0.085f, 0.23f), red, blend: 0.02f);
        b.PaintEll(head, c + V(0, -0.055f, 0), V(0.27f, 0.035f, 0.25f), purple);
        for (int i = 0; i < 14; i++)
        {
            float a = i * 2.39996f;
            float rr = 0.35f + 0.55f * ((i * 37) % 10) / 10f;
            var n = Vector3.Normalize(V(MathF.Sin(a) * rr, 1f, MathF.Cos(a) * rr));
            b.Mark(head, Out(c + V(0, 0.02f, 0), V(0.25f, 0.085f, 0.23f), default, n), n, 0.022f, 0.016f, spot, MarkShape.Disc, i * 40f);
        }
        // A frilled collar
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, V(0, 0.27f, 0) + dir * 0.06f, V(0, 0.25f, 0) + dir * 0.13f, 0.045f, Rgb(200, 160, 176), V(0, 1f, 0) - dir * 0.3f, 0.2f, blend: 0.008f);
        }
        // The ring of toadstools on a ring of root round its foot
        b.Torus(Body, V(0, 0.012f, 0), 0.22f, 0.009f, Rgb(110, 90, 70), mat: Shell, blend: 0.004f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.Tau / 4f + 0.4f;
            b.Limb(Body, V(MathF.Sin(a) * 0.07f, 0.04f, MathF.Cos(a) * 0.06f), V(MathF.Sin(a) * 0.22f, 0.012f, MathF.Cos(a) * 0.22f), 0.012f, 0.008f, Rgb(110, 90, 70), Shell, 0.006f);
        }
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f + 0.15f;
            var at = V(MathF.Sin(a) * 0.22f, 0.012f, MathF.Cos(a) * 0.22f);
            float h = 0.03f + 0.015f * (i % 3);
            b.Limb(Body, at, at + V(0, h, 0), 0.006f, 0.005f, Rgb(240, 230, 236), blend: 0.003f);
            b.Ell(Body, at + V(0, h + 0.006f, 0), V(0.018f, 0.011f, 0.018f), pink, mat: Glow, blend: 0.004f);
        }
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2593 Jellicrown, from Jellicent

    /// <summary>
    /// Jellicrown: Jellicent larger and violet-blue (a female pink), its crown grown into five tall glowing points,
    /// its ruff a beard of white foam, a cape of membrane trailing behind it in violet and teal, and more and longer
    /// tentacles below.
    /// </summary>
    /// <param name="female">A female keeps Jellicent's: pink, her ruff white, her lips red.</param>
    private static PokeBuilder Jellicrown(bool female = false)
    {
        var b = Become(JellicentBuild(female), "Jellicrown", 1.05f);
        Unlift(b, V(0, 0.5f, 0));
        int head = BoneOf(b, "head");
        var coat = female ? Rgb(244, 170, 200) : Rgb(100, 170, 230);
        var ruff = female ? Rgb(250, 240, 246) : Rgb(200, 226, 246);
        var deep = female ? Rgb(220, 120, 176) : Rgb(86, 90, 200);
        var teal = female ? Rgb(250, 190, 220) : Rgb(80, 196, 200);
        var crown = female ? Rgb(255, 210, 230) : Rgb(150, 236, 236);
        b.Sdf.RemoveAll(s => s.Bone == head);
        if (!female) Recolour(b, (coat, deep));
        var c = V(0, 0.52f, 0);
        // The crown of five tall points
        b.Torus(head, c + V(0, 0.14f, -0.005f), 0.05f, 0.012f, crown, mat: Glow, blend: 0.006f);
        for (int i = 0; i < 5; i++)
        {
            float a = (i - 2) * 0.55f;
            var root = c + V(MathF.Sin(a) * 0.05f, 0.14f, MathF.Cos(a) * 0.03f - 0.01f);
            b.Spike(head, root, root + V(MathF.Sin(a) * 0.03f, 0.1f - MathF.Abs(i - 2) * 0.02f, 0), 0.016f, crown, mat: Glow, blend: 0.006f);
        }
        // A beard of foam under its ruff
        Lumps(b, Body, c + V(0, -0.13f, 0.1f), V(0.1f, 0.06f, 0.05f), 14, 0.03f, ruff, White, 10f);
        // A cape of membrane trailing behind
        PokeBuilder.Both(s => Frond(b, Body, c + V(0.12f * s, 0.02f, -0.1f), c + V(0.22f * s, -0.5f, -0.3f), 0.16f, deep, V(0.3f * s, 0.2f, -1f), 0.08f, Fur, 0.02f));
        PokeBuilder.Both(s => b.PaintEll(Body, c + V(0.2f * s, -0.4f, -0.27f), V(0.1f, 0.1f, 0.06f), teal));
        // More and longer tentacles
        for (int i = 0; i < 6; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 6f;
            var d = V(MathF.Cos(a), 0, MathF.Sin(a));
            var root = c + V(0, -0.15f, 0) + d * 0.08f;
            b.Tube(Body, Smooth(3, root, root + d * 0.06f + V(0, -0.15f, 0), root + d * 0.03f + V(0, -0.32f, 0), root + d * 0.09f + V(0, -0.45f, 0)), 0.02f, 0.008f, i % 2 == 0 ? teal : deep, blend: 0.008f);
        }
        Stretch(b, V(1.1f, 1.1f, 1.1f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2594 Alomatron, from Alomomola

    /// <summary>
    /// Alomatron: Alomomola larger, its membrane grown into a wide veil of pink paling to aqua that sweeps behind it,
    /// its side fins grown into fringed hands, small fins below, and a ring of pink light round it.
    /// </summary>
    private static PokeBuilder Alomatron()
    {
        var b = Become(Alomomola(), "Alomatron", 1f);
        Unlift(b, V(0, 0.38f, 0));
        var pink = Rgb(236, 160, 180);
        var veil = Rgb(244, 190, 206);
        var aqua = Rgb(170, 230, 226);
        var c = V(0, 0.4f, 0);
        // The veil sweeping out behind
        for (int i = 0; i < 7; i++)
        {
            float a = (-70f + i * 23f) * Degree;
            var root = c + V(MathF.Sin(a) * 0.05f, 0.04f, -0.06f);
            var tip = c + V(MathF.Sin(a) * 0.3f, 0.12f - MathF.Abs(a) * 0.15f, -0.32f + MathF.Abs(a) * 0.05f);
            Frond(b, Body, root, tip, 0.1f, veil, V(0, 1f, 0.2f), 0.06f, Fur, 0.012f);
            b.PaintEll(Body, Vector3.Lerp(root, tip, 0.85f), V(0.08f, 0.08f, 0.08f), aqua, soft: 0.02f);
        }
        // Fringed fins like open hands
        PokeBuilder.Both(s =>
        {
            int fin = BoneOf(b, s < 0 ? "finL" : "finR");
            var root = c + V(0.11f * s, -0.04f, 0);
            for (int k = 0; k < 5; k++)
                Frond(b, fin, root, root + V((0.12f + k * 0.01f) * s, -0.04f - k * 0.025f, 0.04f - k * 0.02f), 0.022f, pink, V(0.3f, 1f, 0.3f), 0.25f);
            b.Mark(Body, On(c, V(0.13f, 0.15f, 0.09f), 0.07f * s, c.Y + 0.08f), V(0.5f * s, 0.4f, 1f), 0.014f, 0.01f, aqua);
        });
        // The ring of light round it
        b.Torus(Body, c + V(0, 0.07f, 0), 0.185f, 0.009f, Rgb(255, 150, 210), V(8f, 0, 0), mat: Glow, blend: 0.004f);
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2596 Voltarantula, from Galvantula

    /// <summary>
    /// Voltarantula: Galvantula larger, eight legs now and longer, banded yellow and navy, its abdomen spiked and
    /// marked in blue, two more eyes on its brow, and a web of glowing sparks strung on the ground between its legs.
    /// </summary>
    private static PokeBuilder Voltarantula()
    {
        var b = Become(Galvantula(), "Voltarantula", 0.9f);
        var yellow = Rgb(246, 214, 90);
        var navy = Rgb(40, 46, 96);
        var blue = Rgb(60, 96, 170);
        var spark = Rgb(255, 238, 120);
        Recolour(b, (Rgb(160, 150, 210), blue), (Rgb(70, 80, 170), navy));
        var ac = V(0, 0.2f, -0.12f);
        var ar = V(0.12f, 0.1f, 0.13f);
        // Bands round the legs it had, and two more pairs between them
        foreach (var (z, front) in new[] { (0.06f, true), (-0.06f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                var knee = V(0.2f * s, 0.24f, z * 2f);
                var foot = V(0.27f * s, 0.02f, z * 2.6f);
                foreach (float t in new[] { 0.3f, 0.7f })
                {
                    b.PaintTorus(leg, Vector3.Lerp(V(0.06f * s, 0.14f, z), knee, t), 0.03f, 0.012f, navy, Euler(knee - V(0.06f * s, 0.14f, z)));
                    b.PaintTorus(leg, Vector3.Lerp(knee, foot, t), 0.03f, 0.012f, navy, Euler(foot - knee));
                }
            });
        foreach (var z in new[] { 0.0f, -0.13f })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.07f * s, 0.15f, z);
                var knee = V(0.22f * s, 0.25f, z * 1.6f);
                var foot = V(0.31f * s, 0.02f, z * 2f + (z == 0 ? 0.0f : -0.04f));
                b.Limb(Body, hip, knee, 0.026f, 0.023f, yellow, blend: 0.01f);
                b.Limb(Body, knee, foot, 0.023f, 0.018f, yellow, blend: 0.006f);
                foreach (float t in new[] { 0.35f, 0.7f })
                    b.PaintTorus(Body, Vector3.Lerp(knee, foot, t), 0.026f, 0.011f, navy, Euler(foot - knee));
                b.Spike(Body, foot + V(0, 0.01f, 0), foot + V(0.02f * s, -0.02f, 0.01f), 0.012f, navy, mat: Shell, blend: 0.003f);
            });
        // Spikes and blue marks on the abdomen
        for (int i = 0; i < 7; i++)
        {
            float a = i * 2.4f;
            var n = Vector3.Normalize(V(MathF.Sin(a) * 0.7f, 1f, MathF.Cos(a) * 0.7f - 0.2f));
            var at = Out(ac, ar, default, n);
            b.Spike(Body, at - n * 0.01f, at + n * 0.045f, 0.012f, yellow, mat: Shell, blend: 0.004f);
        }
        // Two more eyes
        int head = BoneOf(b, "head");
        var c = V(0, 0.15f, 0.07f);
        var r = V(0.09f, 0.07f, 0.08f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.03f);
            b.Mark(head, at, Outward(c, r, at), 0.008f, 0.008f, Rgb(60, 110, 220));
        });
        // The web of sparks on the ground between its legs
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 8f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var from = V(0, 0.09f, -0.03f) + d * 0.05f;
            var foot = V(0, 0.008f, -0.03f) + d * 0.12f;
            var to = V(0, 0.008f, -0.03f) + d * 0.28f;
            b.Limb(Body, from, foot, 0.004f, 0.004f, spark, Glow, 0.002f);
            b.Limb(Body, foot, to, 0.004f, 0.004f, spark, Glow, 0.002f);
        }
        b.Torus(Body, V(0, 0.008f, -0.03f), 0.18f, 0.004f, spark, mat: Glow, blend: 0.002f);
        b.Torus(Body, V(0, 0.008f, -0.03f), 0.27f, 0.004f, spark, mat: Glow, blend: 0.002f);
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2598 Ferrospire, from Ferrothorn

    /// <summary>
    /// Ferrospire: Ferrothorn larger, five vines now, each ending in its pod of spikes, tall spires of iron rising
    /// round the top of its dome, green leaves bunched round its foot and roots straggling out under it.
    /// </summary>
    private static PokeBuilder Ferrospire()
    {
        var b = Become(Ferrothorn(), "Ferrospire", 1f);
        var gray = Rgb(118, 126, 116);
        var green = Rgb(70, 140, 80);
        var root = Rgb(110, 90, 64);
        Recolour(b, (Rgb(150, 152, 158), gray), (Rgb(70, 160, 100), green));
        int head = BoneOf(b, "head");
        var c = V(0, 0.12f, 0);
        var r = V(0.2f, 0.12f, 0.18f);
        // Two more vines, between the others
        for (int i = 0; i < 2; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 3f + MathF.PI + 0.4f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            int vine = b.Part("vine" + (3 + i), head, c + V(0, 0.12f, 0), PokeRole.Tail, 1.5f + i * 0.5f);
            var pod = c + d * 0.34f + V(0, 0.1f, 0);
            b.Tube(vine, Smooth(3, c + V(0, 0.12f, 0), c + d * 0.1f + V(0, 0.24f, 0), c + d * 0.26f + V(0, 0.22f, 0), pod), 0.014f, 0.012f, green, Leaf, 0f);
            b.Ell(vine, pod, V(0.045f, 0.035f, 0.045f), green, mat: Leaf);
            for (int k = 0; k < 4; k++)
            {
                float t = k * MathF.Tau / 4f;
                var sd = Vector3.Normalize(V(MathF.Cos(t), -0.2f, MathF.Sin(t)));
                b.Spike(vine, pod + sd * 0.03f, pod + sd * 0.07f, 0.015f, Rgb(200, 202, 210), mat: Metal);
            }
        }
        // Spires of iron round the top of the dome, the tallest in the middle
        foreach (var (x, z, h) in new[] { (0f, -0.02f, 0.2f), (0.1f, 0.02f, 0.12f), (-0.1f, 0.02f, 0.12f), (0.06f, -0.1f, 0.14f), (-0.06f, -0.1f, 0.14f) })
        {
            var at = V(x, c.Y + r.Y * MathF.Sqrt(MathF.Max(0.05f, 1f - (x * x) / (r.X * r.X) - (z * z) / (r.Z * r.Z))) - 0.02f, z);
            b.Limb(Body, at, at + V(0, h * 0.55f, 0), 0.03f, 0.022f, gray, Metal, 0.008f);
            b.Spike(Body, at + V(0, h * 0.5f, 0), at + V(0, h, 0), 0.022f, gray, mat: Metal, blend: 0.004f);
        }
        // Leaves round its foot and roots under it
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, c + d * 0.16f + V(0, -0.04f, 0), c + d * 0.26f + V(0, -0.09f, 0), 0.05f, i % 2 == 0 ? green : Rgb(96, 160, 80), V(0, 1f, 0) + d * 0.4f, 0.2f, Leaf, 0.01f);
            if (i % 2 == 0)
                b.Tube(Body, Smooth(2, c + d * 0.1f + V(0, -0.08f, 0), c + d * 0.2f + V(0, -0.11f, 0), c + d * 0.3f + V(0, -0.115f, 0.02f)), 0.008f, 0.004f, root, Shell, 0.004f);
        }
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2606 Cosmogrey, from Beheeyem

    /// <summary>
    /// Cosmogrey: Beheeyem taller, its body a dark violet-grey and its head larger, pale silver with a ridge across
    /// it and great glowing eyes, a brown cape falling from its shoulders and its three lights grown into a ring of
    /// lamps, red, yellow and green, round its shoulders.
    /// </summary>
    private static PokeBuilder Cosmogrey()
    {
        var b = Become(Beheeyem(), "Cosmogrey", 0.92f);
        int head = BoneOf(b, "head");
        var body = Rgb(104, 98, 120);
        var silver = Rgb(170, 172, 180);
        var cape = Rgb(126, 100, 70);
        var ring = Rgb(110, 116, 80);
        Recolour(b, (Rgb(176, 130, 110), body));
        Tint(b, s => s.Bone == head, silver);
        var c = V(0, 0.46f, 0.01f);
        Grow(b, V(1.2f, 1.1f, 1.2f), c + V(0, -0.08f, 0), s => s.Bone == head, i => i == head);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye))
        {
            d.Iris = Rgb(200, 250, 250);
            d.Size *= 1.3f;
            d.Half *= 1.3f;
        }
        b.Model.Decals.RemoveAll(d => d.Bone == head && !d.IsEye && d.Shape != MarkShape.Bar);
        b.Ell(head, c + V(0, 0.07f, -0.01f), V(0.02f, 0.035f, 0.08f), silver, blend: 0.02f);
        // The ring of lamps round its shoulders
        var rc = V(0, 0.335f, 0.0f);
        b.Torus(Body, rc, 0.13f, 0.011f, ring, V(-8f, 0, 0), mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s => b.Limb(Body, V(0.06f * s, 0.335f, 0), V(0.125f * s, 0.336f, 0), 0.012f, 0.01f, ring, Shell, 0.006f));
        var lamps = new[] { Rgb(240, 70, 70), Rgb(250, 220, 70), Rgb(90, 230, 100) };
        for (int i = 0; i < 6; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 6f;
            var at = rc + V(MathF.Sin(a) * 0.13f, -MathF.Cos(a) * 0.13f * MathF.Sin(8f * Degree), MathF.Cos(a) * 0.13f);
            b.Ell(Body, at, V(0.017f, 0.017f, 0.017f), lamps[i % 3], mat: Glow, blend: 0.004f);
        }
        // The cape
        PokeBuilder.Both(s => Frond(b, Body, V(0.03f * s, 0.31f, -0.03f), V(0.14f * s, 0.02f, -0.1f), 0.1f, cape, V(0.3f * s, 0.2f, -1f), 0.12f, SurfaceMaterial.Cloth, 0.012f));
        Frond(b, Body, V(0, 0.31f, -0.03f), V(0, 0.0f, -0.12f), 0.1f, cape, V(0, 0.2f, -1f), 0.12f, SurfaceMaterial.Cloth, 0.012f);
        Stretch(b, V(1f, 1.15f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2614 Beartundra, from Beartic

    /// <summary>
    /// Beartundra: Beartic larger, its beard of icicles longer, a ridge of glacier ice in jagged crystals along its
    /// back and shoulders, ice grown over its forearms and its claws icy blue.
    /// </summary>
    private static PokeBuilder Beartundra()
    {
        var b = Become(Beartic(), "Beartundra", 1f);
        var ice = Rgb(150, 200, 236);
        var glacier = Rgb(176, 216, 240);
        int head = BoneOf(b, "head");
        var c = V(0, 0.62f, 0.05f);
        // The beard longer
        foreach (var (x, len, z) in new[] { (-0.04f, 0.16f, 0.06f), (-0.012f, 0.24f, 0.075f), (0.012f, 0.22f, 0.075f), (0.04f, 0.15f, 0.06f) })
            b.Spike(head, c + V(x, -0.1f, z), c + V(x * 1.2f, -0.1f - len, z + 0.01f), 0.014f, ice, 0.6f, Shell);
        // The ridge of glacier ice along its back and over its shoulders
        var bc = V(0, 0.36f, 0);
        var br = V(0.15f, 0.2f, 0.12f);
        for (int i = 0; i < 9; i++)
        {
            float t = i / 8f;
            var n = Vector3.Normalize(V(((i % 3) - 1) * 0.35f, 1.1f - t * 1.5f, -1f));
            var at = Out(bc, br, default, n);
            float h = 0.08f + 0.06f * MathF.Sin(t * MathF.PI);
            b.Spike(Body, at - n * 0.02f, at + Vector3.Normalize(n + V(0, 0.8f, 0)) * h, 0.026f, i % 2 == 0 ? glacier : ice, 0.55f, Shell, 0.006f);
        }
        PokeBuilder.Both(s =>
        {
            foreach (var o in new[] { V(0.1f, 0.54f, -0.04f), V(0.13f, 0.5f, -0.07f) })
                b.Spike(Body, V(o.X * s, o.Y, o.Z), V(o.X * s * 1.3f, o.Y + 0.09f, o.Z - 0.04f), 0.022f, glacier, 0.55f, Shell, 0.006f);
            // Ice over the forearm, icy claws on the paw
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var elbow = V(0.24f * s, 0.52f, 0.03f);
            var paw = V(0.32f * s, 0.56f, 0.06f);
            b.Limb(arm, Vector3.Lerp(elbow, paw, 0.1f), Vector3.Lerp(elbow, paw, 0.75f), 0.05f, 0.046f, glacier, Shell, 0.008f);
            foreach (float t in new[] { 0.25f, 0.5f })
                b.Spike(arm, Vector3.Lerp(elbow, paw, t) + V(0, 0.03f, -0.02f), Vector3.Lerp(elbow, paw, t) + V(0.01f * s, 0.09f, -0.04f), 0.016f, ice, 0.6f, Shell, 0.004f);
            foreach (float x in new[] { -0.015f, 0f, 0.015f })
                b.Spike(arm, paw + V(x, 0.045f, 0.02f), paw + V(x, 0.09f, 0.05f), 0.008f, ice, mat: Glow, blend: 0.003f);
        });
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2615 Cryohedron, from Cryogonal

    /// <summary>
    /// Cryohedron: Cryogonal larger, its snowflake grown into a lattice of three set at angles to one another round its
    /// face, its eyes a glowing violet, and long chains of ice hanging in loops from its points.
    /// </summary>
    private static PokeBuilder Cryohedron()
    {
        var b = Become(Cryogonal(), "Cryohedron", 0.95f);
        Unlift(b, V(0, 0.42f, 0));
        var ice = Rgb(110, 170, 200);
        var violet = Rgb(170, 140, 230);
        var chain = Rgb(160, 160, 220);
        var c = V(0, 0.42f, 0);
        foreach (var d in b.Model.Decals.Where(d => d.IsEye)) d.Iris = Rgb(220, 120, 255);
        // Two more snowflakes, turned to either side
        foreach (float yaw in new[] { 55f, -55f })
        {
            var q = Quaternion.CreateFromYawPitchRoll(yaw * Degree, 0, 0);
            for (int k = 0; k < 6; k++)
            {
                float a = (90f + k * 60f) * Degree;
                float deg = k * 60f;
                var d = Vector3.Transform(V(MathF.Cos(a), MathF.Sin(a), 0), q);
                var corner = c + d * 0.08f;
                b.Box(Body, c + d * 0.12f, V(0.014f, 0.115f, 0.016f), 0.004f, k % 2 == 0 ? ice : violet, V(0, yaw, deg), Shell, 0.004f);
                foreach (float t in new[] { -1f, 1f })
                    b.Box(Body, corner + d * 0.11f, V(0.012f, 0.045f, 0.014f), 0.004f, ice, V(0, yaw, deg + 40f * t), Shell, 0.004f);
            }
        }
        // Chains of ice hanging in loops from the side points
        PokeBuilder.Both(s =>
        {
            var from = c + V(0.866f * s, -0.5f, 0) * 0.19f;
            var to = c + V(0, -0.2f, 0.01f);
            for (int i = 0; i <= 30; i++)
            {
                float t = i / 30f;
                var at = Vector3.Lerp(from, to, t) + V(0, -MathF.Sin(t * MathF.PI) * 0.22f, 0);
                b.Torus(Body, at, 0.012f, 0.004f, chain, i % 2 == 0 ? V(0, 0, 90f) : V(0, 90f, 90f), mat: Shell, blend: 0.002f);
            }
        });
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2617 Velocigor, from Accelgor

    /// <summary>
    /// Velocigor: Accelgor taller, wrapped now in crimson over a black body, a pale mask over its glowing eyes, two
    /// feelers from its hood, a red abdomen behind and long scarves streaming from its neck.
    /// </summary>
    private static PokeBuilder Velocigor()
    {
        var b = Become(Accelgor(), "Velocigor", 0.86f);
        var black = Rgb(50, 46, 50);
        var red = Rgb(150, 30, 46);
        var pale = Rgb(232, 222, 210);
        Recolour(b, (Rgb(92, 106, 150), black), (Rgb(130, 146, 190), red), (Rgb(236, 120, 150), red), (Rgb(214, 70, 80), Rgb(110, 20, 36)),
            (Rgb(120, 200, 110), Rgb(120, 22, 38)), (Rgb(40, 38, 48), pale), (Rgb(236, 236, 220), Rgb(200, 255, 200)));
        b.Model.Decals.RemoveAll(d => d.Shape == MarkShape.Star);
        int head = BoneOf(b, "head");
        var c = V(0, 0.42f, 0.015f);
        // Two feelers from its hood
        PokeBuilder.Both(s => b.Tube(head, Smooth(2, c + V(0.025f * s, 0.06f, 0.0f), c + V(0.04f * s, 0.12f, 0.01f), c + V(0.07f * s, 0.16f, -0.02f)), 0.006f, 0.003f, black, Shell, 0.003f));
        // A red abdomen behind, wrapped
        b.Ell(Body, V(0, 0.2f, -0.08f), V(0.05f, 0.11f, 0.05f), red, V(-25f, 0, 0), SurfaceMaterial.Cloth, 0.02f);
        // Scarves streaming from its neck
        foreach (var (y, x) in new[] { (0.36f, 0.05f), (0.34f, -0.04f), (0.3f, 0.07f) })
            Ribbon(b, Body, Smooth(3, V(x * 0.5f, y, -0.03f), V(x, y + 0.03f, -0.14f), V(x * 2f, y - 0.03f, -0.28f), V(x * 2.5f, y + 0.02f, -0.42f)), 0.035f, red, V(0, 1f, 0.2f), 0.008f);
        Stretch(b, V(1f, 1.15f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2618 Fluxflounder, from Stunfisk

    /// <summary>
    /// Fluxflounder: Stunfisk larger and wider, olive brown, a fringe of fins round its whole body tipped in yellow,
    /// glowing yellow spots on its back and its grin broader.
    /// </summary>
    private static PokeBuilder Fluxflounder()
    {
        var b = Become(Stunfisk(), "Fluxflounder", 0.8f);
        var skin = Rgb(124, 106, 70);
        var yellow = Rgb(250, 230, 70);
        Recolour(b, (Rgb(140, 92, 60), skin));
        var c = V(0, 0.05f, 0);
        var r = V(0.17f, 0.05f, 0.14f);
        // The fringe of fins round its body
        for (int i = 0; i < 24; i++)
        {
            float a = i * MathF.Tau / 24f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = c + V(d.X * r.X * 0.9f, -0.02f, d.Z * r.Z * 0.9f);
            var tip = root + d * 0.07f + V(0, -0.01f, 0);
            Blade(b, Body, root, tip, 0.03f, skin, V(0, 1f, 0), 0.2f);
            b.PaintEll(Body, tip - d * 0.01f, V(0.02f, 0.02f, 0.02f), yellow, soft: 0.006f);
        }
        // Glowing spots on its back
        foreach (var (x, z, rr) in new[] { (-0.06f, -0.02f, 0.02f), (0.04f, -0.05f, 0.024f), (0.09f, 0.02f, 0.016f), (-0.1f, 0.04f, 0.015f), (0.0f, 0.03f, 0.018f), (-0.03f, -0.08f, 0.016f), (0.1f, -0.08f, 0.014f) })
        {
            var n = Vector3.Normalize(V(x / r.X * 0.6f, 1f, z / r.Z * 0.6f));
            var at = Out(c, r, default, n);
            b.Ell(Body, at, V(rr, 0.008f, rr), yellow, mat: Glow, blend: 0.004f);
        }
        // A broader grin
        b.PaintEll(Body, c + V(0, -0.005f, 0.13f), V(0.16f, 0.022f, 0.035f), Rgb(240, 170, 60), soft: 0.01f);
        Stretch(b, V(1.25f, 1.1f, 1.25f));
        return b;
    }

    // ------------------------------------------------------------------ 2620 Mienshifu, from Mienshao

    /// <summary>
    /// Mienshifu: Mienshao taller, cream and deep purple, a long white beard falling from its chin and whiskers
    /// beside it, its sleeves of fur longer and fringed at their ends, and a sash tied at its waist with tassels.
    /// </summary>
    private static PokeBuilder Mienshifu()
    {
        var b = Become(Mienshao(), "Mienshifu", 0.95f);
        var cream = Rgb(240, 232, 206);
        var purple = Rgb(96, 70, 112);
        var sash = Rgb(120, 40, 56);
        var beard = Rgb(246, 246, 250);
        Recolour(b, (Rgb(228, 224, 236), cream), (Rgb(158, 136, 196), purple));
        int head = BoneOf(b, "head");
        var c = V(0, 0.52f, 0.02f);
        // The beard and whiskers
        b.Ell(head, c + V(0, -0.04f, 0.05f), V(0.03f, 0.03f, 0.025f), beard, blend: 0.01f);
        Frond(b, Body, V(0, 0.47f, 0.06f), V(0, 0.3f, 0.07f), 0.045f, beard, V(0, 0, 1f), 0.4f, SurfaceMaterial.Hair, 0.012f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(2, c + V(0.015f * s, -0.01f, 0.07f), c + V(0.06f * s, 0.0f, 0.07f), c + V(0.1f * s, -0.02f, 0.05f)), 0.003f, 0.002f, beard, blend: 0.002f));
        // Sleeves of cream over the arms, fringed at their ends
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = V(0.05f * s, 0.44f, 0.01f);
            var hand = V(0.11f * s, 0.36f, 0.06f);
            var hem = hand + V(0.03f * s, -0.1f, -0.03f);
            Frond(b, arm, shoulder, hem, 0.05f, cream, V(s, 0, 0.4f), 0.2f, Fur, 0.012f);
            for (int k = -2; k <= 2; k++)
                b.Limb(arm, hem + V(k * 0.01f, 0.02f, k * 0.004f), hem + V(k * 0.012f, -0.08f, k * 0.006f), 0.005f, 0.003f, Rgb(210, 200, 176), blend: 0.003f);
        });
        // The sash and its tassels
        b.PaintTorus(Body, V(0, 0.32f, 0), 0.054f, 0.016f, sash, sz: 0.85f);
        b.Ell(Body, V(0.02f, 0.32f, 0.045f), V(0.016f, 0.014f, 0.01f), sash, blend: 0.004f);
        foreach (float x in new[] { 0.015f, 0.03f })
        {
            b.Limb(Body, V(x, 0.31f, 0.048f), V(x + 0.005f, 0.22f, 0.05f), 0.004f, 0.004f, Rgb(70, 50, 80), blend: 0.003f);
            b.Ell(Body, V(x + 0.005f, 0.21f, 0.05f), V(0.008f, 0.016f, 0.008f), Rgb(70, 50, 80), blend: 0.003f);
        }
        Stretch(b, V(1f, 1.12f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2621 Drudrakon, from Druddigon

    /// <summary>
    /// Drudrakon: Druddigon larger, its stubby wings grown out into great spread wings with a brown membrane, its red
    /// face plated, ridges of dark rock down its back and tail, and a club of rock at its tail's end.
    /// </summary>
    private static PokeBuilder Drudrakon()
    {
        var b = Become(Druddigon(), "Drudrakon", 1f);
        var blue = Rgb(50, 96, 140);
        var membrane = Rgb(116, 96, 96);
        var rock = Rgb(92, 80, 76);
        var red = Rgb(170, 36, 56);
        Recolour(b, (Rgb(50, 108, 146), blue), (Rgb(196, 50, 60), red));
        // The great wings
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var shoulder = V(0.07f * s, 0.42f, -0.05f);
            var wrist = V(0.26f * s, 0.78f, -0.18f);
            DragonWing(b, wing, shoulder, wrist, new[] { V(0.5f * s, 0.62f, -0.22f), V(0.46f * s, 0.44f, -0.22f), V(0.34f * s, 0.34f, -0.18f) }, V(0.12f * s, 0.36f, -0.08f), membrane, blue, 0.018f);
            b.Spike(wing, wrist, wrist + V(0.05f * s, 0.06f, -0.01f), 0.014f, Rgb(170, 160, 140), mat: Shell, blend: 0.004f);
        });
        // Ridges of rock down its back and tail, a club at its end
        foreach (var (y, z) in new[] { (0.5f, -0.02f), (0.47f, -0.07f), (0.42f, -0.1f), (0.36f, -0.12f), (0.3f, -0.12f) })
            PokeBuilder.Both(s => b.Spike(Body, V(0.035f * s, y, z), V(0.06f * s, y + 0.07f, z - 0.05f), 0.02f, rock, 0.5f, Shell, 0.006f));
        int tail = BoneOf(b, "tail");
        var end = V(0.03f, 0.06f, -0.48f);
        Lumps(b, tail, end + V(0.01f, 0.03f, -0.04f), V(0.05f, 0.05f, 0.05f), 9, 0.03f, rock, Rgb(150, 140, 126), 10f);
        b.Ell(tail, end + V(0.01f, 0.03f, -0.04f), V(0.045f, 0.045f, 0.045f), rock, mat: Shell, blend: 0.012f);
        // Plates over its face
        int head = BoneOf(b, "head");
        var c = V(0, 0.55f, 0.12f);
        foreach (var (x, y, z) in new[] { (0f, 0.04f, 0.08f), (-0.04f, 0.02f, 0.08f), (0.04f, 0.02f, 0.08f), (0f, 0.0f, 0.12f) })
            Plate(b, head, c + V(x, y, z), V(x * 3f, 0.6f, 1f), V(0.03f, 0.008f, 0.025f), PixelCanvas.Shadow(red, 0.1f), Shell);
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2623 Colossurk, from Golurk

    /// <summary>
    /// Colossurk: Golurk larger and of mossy stone, a second seal bound round its head like a crown, great plates of
    /// stone on its shoulders, its rocket feet wider and a glow at its chest under the crossed seal.
    /// </summary>
    private static PokeBuilder Colossurk()
    {
        var b = Become(Golurk(), "Colossurk", 1f);
        var stone = Rgb(104, 122, 92);
        var dark = Rgb(72, 82, 66);
        var strap = Rgb(196, 168, 118);
        var glow = Rgb(252, 226, 100);
        Recolour(b, (Rgb(88, 160, 178), stone), (Rgb(46, 104, 138), dark), (Rgb(140, 100, 64), strap));
        int head = BoneOf(b, "head");
        var c = V(0, 0.68f, 0.03f);
        // The second seal, a crown round its head
        b.Torus(head, c + V(0, 0.045f, 0), 0.085f, 0.018f, strap, sx: 1f, sz: 0.95f, mat: Shell, blend: 0.004f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.3f;
            b.Box(head, c + V(MathF.Sin(a) * 0.085f, 0.068f + (i % 2) * 0.006f, MathF.Cos(a) * 0.08f), V(0.018f, 0.02f, 0.008f), 0.003f, strap, V(0, a / Degree, 0), Shell, 0.004f);
        }
        b.Mark(head, c + V(0, 0.045f, 0.1f), V(0, 0, 1f), 0.03f, 0.008f, dark, MarkShape.Zigzag);
        // Plates of stone on its shoulders
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = V(0.16f * s, 0.56f, 0);
            b.Box(arm, shoulder + V(0.04f * s, 0.07f, 0), V(0.1f, 0.03f, 0.1f), 0.015f, stone, V(0, 0, -15f * s), Shell, 0.01f);
            b.Box(arm, shoulder + V(0.09f * s, 0.03f, 0), V(0.03f, 0.06f, 0.09f), 0.012f, stone, V(0, 0, -15f * s), Shell, 0.01f);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Box(leg, V(0.092f * s, 0.04f, 0.03f), V(0.08f, 0.04f, 0.09f), 0.015f, stone, mat: Shell, blend: 0.01f);
            b.Torus(leg, V(0.092f * s, 0.05f, 0.03f), 0.075f, 0.014f, Rgb(200, 170, 70), V(0, 0, 0), sz: 1.1f, mat: Metal, blend: 0.004f);
        });
        // A glow at its chest
        var bc = V(0, 0.46f, 0);
        b.Ell(Body, bc + V(0, -0.04f, 0.12f), V(0.045f, 0.035f, 0.012f), glow, mat: Glow, blend: 0.006f);
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2626 Bouffalanx, from Bouffalant

    /// <summary>
    /// Bouffalanx: Bouffalant larger, its afro grown into a great mane that covers its head and shoulders, its horns
    /// dark grey, longer and curved forward, and shaggy fur round its legs.
    /// </summary>
    private static PokeBuilder Bouffalanx()
    {
        var b = Become(Bouffalant(), "Bouffalanx", 1f);
        var afro = Rgb(72, 56, 48);
        var horn = Rgb(100, 96, 92);
        var brown = Rgb(140, 108, 80);
        Recolour(b, (Rgb(232, 220, 178), horn), (Rgb(226, 184, 72), horn), (Rgb(150, 104, 66), brown));
        int head = BoneOf(b, "head");
        // The mane over its shoulders
        var bc = V(0, 0.38f, -0.02f);
        var mc = bc + V(0, 0.12f, 0.08f);
        var mr = V(0.2f, 0.17f, 0.17f);
        b.Ell(Body, mc, mr, afro, blend: 0.03f);
        Lumps(b, Body, mc, mr, 30, 0.05f, afro, PixelCanvas.Mix(afro, White, 0.08f), 10f);
        // Longer horns, curving forward
        var c = V(0, 0.4f, 0.3f);
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.06f * s, 0.03f, -0.02f);
            b.Tube(head, Smooth(3, root + V(0.15f * s, 0.09f, 0.035f), root + V(0.2f * s, 0.16f, 0.08f), root + V(0.17f * s, 0.21f, 0.15f), root + V(0.11f * s, 0.21f, 0.2f)), 0.016f, 0.005f, horn, Shell, 0.004f);
        });
        // Shaggy fur round its legs
        foreach (int i in Enumerable.Range(0, b.Model.Bones.Count).Where(i => HasRole(b, i, PokeRole.Leg)))
        {
            var j = b.Model.Skeleton[i].Joint;
            FurTufts(b, i, V(j.X, 0.1f, j.Z + 0.02f), V(0.05f, 0.03f, 0.05f), 8, 0.04f, 0.016f, afro, 1.1f, -1f, 0.4f);
        }
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2628 Imperavia, from Braviary

    /// <summary>
    /// Imperavia: Braviary larger, its plumage a deep maroon over a white breast marked with old scars, its crest
    /// grown into a plume of long feathers of red, white and blue, and its flight feathers longer, tipped blue.
    /// </summary>
    private static PokeBuilder Imperavia()
    {
        var b = Become(Braviary(), "Imperavia", 1f);
        Unlift(b, V(0, 0.42f, 0));
        var maroon = Rgb(120, 42, 44);
        var cover = Rgb(136, 50, 46);
        var red = Rgb(196, 40, 52);
        var blue = Rgb(46, 76, 160);
        Recolour(b, (Rgb(80, 82, 150), maroon), (Rgb(200, 44, 50), cover), (Rgb(60, 110, 200), blue));
        int head = BoneOf(b, "head");
        var bc = V(0, 0.42f, 0);
        var c = bc + V(0, 0.17f, 0.17f);
        // A white breast with old scars
        b.PaintEll(Body, bc + V(0, 0.04f, 0.1f), V(0.08f, 0.1f, 0.06f), White);
        b.Mark(Body, Out(bc, V(0.1f, 0.12f, 0.14f), default, V(0.2f, 0.3f, 1f)), V(0.2f, 0.3f, 1f), 0.03f, 0.005f, Rgb(200, 150, 140), MarkShape.Bar, 35f);
        b.Mark(Body, Out(bc, V(0.1f, 0.12f, 0.14f), default, V(-0.15f, 0.1f, 1f)), V(-0.15f, 0.1f, 1f), 0.025f, 0.005f, Rgb(200, 150, 140), MarkShape.Bar, -30f);
        // The plume of long feathers
        var plume = new[] { red, White, blue, red, White };
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 0.02f;
            var root = c + V(x, 0.05f, -0.03f);
            var end = c + V(x * 2.4f, 0.17f + 0.03f * (2 - MathF.Abs(i - 2)), -0.26f);
            Frond(b, head, root, end, 0.03f, plume[i], V(0, 1f, 0.2f), 0.25f);
        }
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2630 Carrionqueen, from Mandibuzz

    /// <summary>
    /// Carrionqueen: Mandibuzz larger and darker, a ruff of long violet-black feathers round its neck, its wings
    /// spread wide and tattered at their tips, its head dark grey under a crown of bone, and the bones round its
    /// middle grown into a skirt of ribs.
    /// </summary>
    private static PokeBuilder Carrionqueen()
    {
        var b = Become(Mandibuzz(), "Carrionqueen", 1f);
        var brown = Rgb(62, 54, 52);
        var ruff = Rgb(70, 56, 86);
        var bone = Rgb(214, 204, 172);
        var skin = Rgb(96, 90, 100);
        Recolour(b, (Rgb(110, 92, 82), brown), (Rgb(156, 136, 120), Rgb(84, 74, 70)), (Rgb(222, 140, 160), skin), (Rgb(232, 214, 170), bone), (Rgb(76, 62, 56), ruff));
        int head = BoneOf(b, "head");
        var bc = V(0, 0.32f, 0);
        var c = bc + V(0, 0.27f, 0.04f);
        // The ruff of long feathers
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, bc + V(0, 0.15f, 0.01f) + d * 0.05f, bc + V(0, 0.08f, 0.01f) + d * 0.13f, 0.03f, ruff, V(0, 1f, 0) - d * 0.4f, 0.22f);
        }
        // Wings spread wide, tattered at their tips
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.08f * s, 0.1f, -0.02f);
            for (int i = 0; i < 6; i++)
            {
                float a = (70f - i * 22f) * Degree;
                var tip = root + V(MathF.Cos(a) * s * 0.9f, MathF.Sin(a), -0.25f) * (0.34f - i * 0.02f);
                Frond(b, Body, root, tip, 0.04f, brown, V(0, 0.2f, 1f), 0.22f);
            }
        });
        // The crown of bone
        b.Torus(head, c + V(0, 0.045f, -0.01f), 0.04f, 0.008f, bone, V(-10f, 0, 0), mat: Shell, blend: 0.004f);
        for (int i = 0; i < 7; i++)
        {
            float a = (i - 3) * 0.5f;
            var root = c + V(MathF.Sin(a) * 0.04f, 0.05f, MathF.Cos(a) * 0.035f - 0.01f);
            b.Spike(head, root, root + V(MathF.Sin(a) * 0.015f, 0.045f + (i == 3 ? 0.04f : (i % 2) * 0.02f), 0), 0.009f, bone, mat: Shell, blend: 0.003f);
        }
        // A skirt of ribs round its middle
        for (int i = -5; i <= 5; i++)
        {
            float a = i * 0.28f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Tube(Body, Smooth(2, bc + V(0, -0.02f, 0) + d * 0.09f, bc + V(0, -0.08f, 0) + d * 0.12f, bc + V(0, -0.16f, 0) + d * 0.11f), 0.008f, 0.006f, bone, Shell, 0.004f);
        }
        b.Torus(Body, bc + V(0, -0.02f, 0.0f), 0.09f, 0.012f, bone, mat: Shell, blend: 0.004f);
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2631 Pyrovore, from Heatmor

    /// <summary>
    /// Pyrovore: Heatmor larger, its fur banded charcoal and rust like soot, its snout a longer pipe glowing at its
    /// end, glowing vents on its shoulders and a seam of fire on its belly, and its tail grown into a banded chimney
    /// burning at its top.
    /// </summary>
    private static PokeBuilder Pyrovore()
    {
        var b = Become(Heatmor(), "Pyrovore", 1f);
        int tail = BoneOf(b, "tail");
        int head = BoneOf(b, "head");
        var char_ = Rgb(70, 62, 60);
        var rust = Rgb(176, 84, 40);
        var iron = Rgb(96, 70, 60);
        var ember = Rgb(255, 130, 40);
        Recolour(b, (Rgb(188, 60, 50), char_), (Rgb(240, 196, 70), rust), (Rgb(170, 150, 132), Rgb(84, 76, 74)));
        b.Sdf.RemoveAll(s => s.Bone == tail);
        var bc = V(0, 0.28f, -0.01f);
        // Bands of rust round its body
        foreach (float y in new[] { 0.2f, 0.3f, 0.38f })
            b.PaintTorus(Body, bc + V(0, y - 0.28f, 0), 0.13f, 0.014f, rust, V(15f, 0, 0), sz: 0.9f);
        // Glowing vents and a seam of fire on its belly
        PokeBuilder.Both(s => b.Box(Body, bc + V(0.11f * s, 0.07f, 0.0f), V(0.012f, 0.025f, 0.03f), 0.004f, ember, V(0, 0, -15f * s), Glow, 0.004f));
        b.Mark(Body, bc + V(0, -0.03f, 0.13f), V(0, -0.1f, 1f), 0.03f, 0.03f, ember, MarkShape.Star);
        // A longer snout, glowing at its end
        var c = V(0, 0.46f, 0.08f);
        var end = c + V(0, -0.1f, 0.24f);
        b.Limb(head, end, end + V(0, -0.03f, 0.08f), 0.028f, 0.03f, iron, Metal, 0.006f);
        b.Ell(head, end + V(0, -0.035f, 0.09f), V(0.024f, 0.012f, 0.024f), ember, Euler(V(0, -0.3f, 1f)), Glow, 0.004f);
        // The chimney of a tail, burning at its top
        var t0 = bc + V(0, -0.06f, -0.1f);
        var t1 = bc + V(0, 0.04f, -0.26f);
        var t2 = bc + V(0, 0.22f, -0.3f);
        b.Limb(tail, t0, t1, 0.05f, 0.055f, iron, Metal, 0.01f);
        b.Limb(tail, t1, t2, 0.055f, 0.06f, iron, Metal, 0.01f);
        foreach (var at in new[] { Vector3.Lerp(t0, t1, 0.6f), Vector3.Lerp(t1, t2, 0.3f), Vector3.Lerp(t1, t2, 0.85f) })
            b.Torus(tail, at, 0.058f, 0.009f, Rgb(70, 52, 46), Euler(t2 - t1), mat: Metal, blend: 0.003f);
        FlameTongue(b, tail, t2, t2 + V(0, 0.15f, -0.02f), 0.04f, Rgb(250, 110, 40), Rgb(255, 230, 120));
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2632 Formicarch, from Durant

    /// <summary>
    /// Formicarch: Durant larger, its abdomen grown long and banded dark and pale steel, wings of grey glass folded
    /// over its back, a crown of steel horns ringed in bronze and four red eyes.
    /// </summary>
    private static PokeBuilder Formicarch()
    {
        var b = Become(Durant(), "Formicarch", 0.82f);
        var steel = Rgb(150, 154, 166);
        var dark = Rgb(66, 66, 76);
        var pale = Rgb(206, 210, 216);
        var bronze = Rgb(170, 140, 80);
        var glass = Rgb(196, 204, 214);
        int head = BoneOf(b, "head");
        // The long banded abdomen, past the old one
        var a0 = V(0, 0.11f, -0.1f);
        var a1 = V(0, 0.12f, -0.28f);
        b.Ell(Body, (a0 + a1) / 2f, V(0.08f, 0.07f, 0.16f), steel, blend: 0.03f);
        for (int i = 0; i < 6; i++)
            b.PaintTorus(Body, Vector3.Lerp(a0, a1, 0.1f + i * 0.16f), 0.075f, 0.012f, i % 2 == 0 ? dark : pale, V(90f, 0, 0));
        b.Spike(Body, a1 + V(0, 0, -0.08f), a1 + V(0, -0.02f, -0.16f), 0.03f, steel, 0.6f, Metal);
        // Wings of grey glass folded over its back
        PokeBuilder.Both(s =>
        {
            Frond(b, Body, V(0.03f * s, 0.15f, -0.04f), V(0.09f * s, 0.24f, -0.28f), 0.05f, glass, V(0.2f * s, 1f, 0), 0.12f, Metal, 0.006f);
            Frond(b, Body, V(0.03f * s, 0.15f, -0.06f), V(0.12f * s, 0.2f, -0.24f), 0.04f, glass, V(0.3f * s, 1f, 0), 0.12f, Metal, 0.006f);
        });
        // The crown of horns
        var c = V(0, 0.11f, 0.1f);
        b.Torus(head, c + V(0, 0.04f, -0.01f), 0.05f, 0.01f, bronze, mat: Metal, blend: 0.004f);
        for (int i = 0; i < 5; i++)
        {
            float a = (i - 2) * 0.55f;
            var root = c + V(MathF.Sin(a) * 0.045f, 0.045f, MathF.Cos(a) * 0.035f - 0.015f);
            b.Spike(head, root, root + V(MathF.Sin(a) * 0.02f, 0.08f - MathF.Abs(i - 2) * 0.012f, -0.01f), 0.012f, steel, mat: Metal, blend: 0.004f);
        }
        var r = V(0.065f, 0.055f, 0.06f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, c.Y + 0.02f);
            b.Mark(head, at, Outward(c, r, at), 0.007f, 0.007f, Rgb(220, 40, 40));
        });
        Stretch(b, V(1.2f, 1.2f, 1.2f));
        return b;
    }

    // ------------------------------------------------------------------ 2637 Helioptera, from Volcarona

    /// <summary>
    /// Helioptera: Volcarona larger, its six wings grown into a ring of eight round its body like the sun's rays,
    /// glowing orange with red eyespots, its corona of white fur fuller, its abdomen a deep red and its eyes a brighter
    /// blue, feathered antennae of flame.
    /// </summary>
    private static PokeBuilder Helioptera()
    {
        var b = Become(Volcarona(), "Helioptera", 1f);
        Unlift(b, V(0, 0.46f, 0));
        var orange = Rgb(250, 130, 50);
        var red = Rgb(200, 50, 40);
        var white = Rgb(244, 242, 236);
        Recolour(b, (Rgb(120, 150, 180), Rgb(150, 40, 40)), (Rgb(90, 190, 240), Rgb(70, 170, 255)));
        Make(b, s => Is(s.Color, Rgb(240, 110, 50)), Glow);
        var bc = V(0, 0.46f, 0);
        // An extra pair of wings between the others: eight round it
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var root = bc + V(0.03f * s, 0.02f, -0.06f);
            foreach (var (a, len, wide) in new[] { (42f, 0.3f, 0.07f), (-18f, 0.3f, 0.07f) })
            {
                var d = V(MathF.Cos(a * Degree) * s, MathF.Sin(a * Degree), 0);
                var mid = root + d * len * 0.55f + V(0, 0, -0.03f);
                b.Ell(wing, mid, V(wide, len * 0.55f, 0.012f), orange, Euler(d), Glow, 0.02f);
                b.PaintEll(wing, root + d * len * 0.98f, V(wide * 0.9f, len * 0.15f, 0.03f), red, Euler(d));
                b.PaintEll(wing, root + d * len * 0.6f + V(0, 0, -0.03f), V(0.03f, 0.03f, 0.03f), red, soft: 0.006f);
            }
        });
        // A fuller corona of white fur
        FurTufts(b, Body, bc + V(0, 0.03f, 0), V(0.11f, 0.075f, 0.1f), 24, 0.045f, 0.02f, white, 0.8f, -1f, 0.3f);
        // Feathered antennae of flame
        int head = BoneOf(b, "head");
        var c = bc + V(0, 0.1f, 0.05f);
        PokeBuilder.Both(s => FlameTongue(b, head, c + V(0.04f * s, 0.15f, -0.03f), c + V(0.06f * s, 0.24f, -0.05f), 0.016f, orange, Rgb(255, 220, 110)));
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2638 Cobaltarch, from Cobalion

    /// <summary>
    /// Cobaltarch: Cobalion larger and armoured, its crest grown into a great curved blade of steel sweeping back, a
    /// mane of deep blue fur over its neck and chest, plates of navy edged in gold on its shoulders and legs, and a
    /// tail ending in a hooked blade.
    /// </summary>
    private static PokeBuilder Cobaltarch()
    {
        var b = Become(Cobalion(), "Cobaltarch", 1f);
        int head = BoneOf(b, "head");
        var tan = Rgb(208, 170, 110);
        var steel = Rgb(170, 180, 196);
        var blue = Rgb(56, 82, 170);
        var navy = Rgb(44, 58, 110);
        var gold = Rgb(200, 164, 70);
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, tan));
        Recolour(b, (Rgb(40, 150, 190), Rgb(70, 90, 150)), (Rgb(238, 238, 242), blue), (tan, steel));
        var bc = V(0, 0.5f, -0.02f);
        var c = bc + V(0, 0.3f, 0.24f);
        // The blade of its crest
        Sickle(b, head, c + V(0, 0.03f, 0.03f), c + V(0, 0.16f, -0.05f), c + V(0, 0.22f, -0.32f), 0.05f, steel, V(1f, 0, 0));
        b.Spike(head, c + V(0, 0.07f, 0.03f), c + V(0, 0.12f, 0.14f), 0.02f, steel, 0.4f, Metal);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.02f * s, 0.025f, -0.02f), c + V(0.07f * s, 0.1f, -0.1f), 0.012f, steel, 0.6f, Metal));
        Make(b, s => s.Bone == head && Is(s.Color, steel), Metal);
        // The mane over its neck and chest
        b.Ell(Body, bc + V(0, 0.1f, 0.15f), V(0.095f, 0.13f, 0.085f), blue, blend: 0.03f);
        FurTufts(b, Body, bc + V(0, 0.12f, 0.15f), V(0.1f, 0.14f, 0.09f), 22, 0.06f, 0.022f, blue, 1.1f, -1f, 0.6f);
        // Plates on its shoulders, edged in gold
        PokeBuilder.Both(s =>
        {
            foreach (var z in new[] { 0.1f, -0.14f })
            {
                var at = bc + V(0.08f * s, 0.02f, z);
                b.Ell(Body, at + V(0, 0.04f, 0), V(0.045f, 0.06f, 0.075f), navy, V(0, 0, -30f * s), Metal, 0.01f);
                b.PaintEll(Body, at + V(0.04f * s, 0.0f, 0), V(0.03f, 0.03f, 0.09f), gold);
            }
        });
        // A hooked blade at its tail's end
        int tail = BoneOf(b, "tail");
        var tip = bc + V(0, 0.02f, -0.32f);
        Sickle(b, tail, tip, tip + V(0, 0.06f, -0.08f), tip + V(0, 0.16f, -0.05f), 0.03f, steel, V(1f, 0, 0));
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2639 Terrakhan, from Terrakion

    /// <summary>
    /// Terrakhan: Terrakion larger, its face fur dark with burning orange eyes, its horns grown into a crown of four,
    /// and plates of stone in red, ochre and slate stacked on its back like a fortress.
    /// </summary>
    private static PokeBuilder Terrakhan()
    {
        var b = Become(Terrakion(), "Terrakhan", 1f);
        int head = BoneOf(b, "head");
        var dark = Rgb(56, 52, 54);
        var horn = Rgb(134, 124, 110);
        Recolour(b, (Rgb(206, 186, 146), Rgb(120, 104, 84)), (Rgb(110, 90, 72), horn));
        Tint(b, s => s.Bone == head && Is(s.Color, Rgb(120, 104, 84)), dark);
        var bc = V(0, 0.42f, 0);
        var c = bc + V(0, 0.1f, 0.3f);
        // A second pair of horns, inside the first
        PokeBuilder.Both(s => b.Tube(head, Smooth(2, c + V(0.04f * s, 0.07f, -0.03f), c + V(0.08f * s, 0.15f, -0.06f), c + V(0.07f * s, 0.25f, -0.06f)), 0.026f, 0.008f, horn, Shell, 0.006f));
        // The fortress of plates on its back
        var colours = new[] { Rgb(150, 70, 52), Rgb(196, 146, 64), Rgb(86, 84, 88) };
        for (int i = 0; i < 12; i++)
        {
            float x = ((i % 3) - 1) * 0.08f;
            float z = 0.12f - (i / 3) * 0.08f;
            var at = bc + V(x, 0.14f + (i / 3 == 1 ? 0.02f : 0f), z);
            b.Box(Body, at, V(0.06f, 0.016f, 0.045f), 0.006f, colours[i % 3], V(-12f, i * 23f % 30f - 15f, x * 120f), Shell, 0.006f);
            if (i % 2 == 0)
                b.Spike(Body, at + V(0, 0.01f, 0), at + V(x * 0.4f, 0.09f + (i % 3) * 0.02f, -0.02f), 0.025f, colours[(i + 1) % 3], 0.5f, Shell, 0.004f);
        }
        b.Spike(Body, bc + V(0, 0.15f, 0), bc + V(0, 0.32f, -0.03f), 0.04f, Rgb(86, 84, 88), 0.6f, Shell, 0.006f);
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2640 Virizenith, from Virizion

    /// <summary>
    /// Virizenith: Virizion larger and a deeper green, its horns grown into two long blades of leaf curving back, a
    /// mane of leaves down its neck and chest, leaves on its legs and its tail a plume of leaves with buds.
    /// </summary>
    private static PokeBuilder Virizenith()
    {
        var b = Become(Virizion(), "Virizenith", 1f);
        int head = BoneOf(b, "head");
        var green = Rgb(70, 140, 70);
        var leaf = Rgb(96, 170, 70);
        var dark = Rgb(56, 100, 50);
        Recolour(b, (Rgb(110, 200, 80), green));
        var bc = V(0, 0.44f, -0.02f);
        var c = bc + V(0, 0.3f, 0.22f);
        // Two long blades of leaf
        PokeBuilder.Both(s => Sickle(b, head, c + V(0.025f * s, 0.035f, 0.0f), c + V(0.07f * s, 0.17f, -0.04f), c + V(0.09f * s, 0.24f, -0.22f), 0.03f, leaf, V(1f, 0, 0)));
        // The mane of leaves
        for (int i = 0; i < 16; i++)
        {
            float t = i / 15f;
            var at = Vector3.Lerp(bc + V(0, 0.06f, 0.16f), c + V(0, -0.02f, -0.04f), t);
            foreach (float s in new[] { -1f, 1f })
                Frond(b, Body, at, at + V(0.05f * s, -0.04f, -0.03f), 0.025f, i % 2 == 0 ? leaf : green, V(s, 0.3f, 0), 0.2f, Leaf, 0.008f);
        }
        b.Ell(Body, bc + V(0, 0.04f, 0.18f), V(0.07f, 0.09f, 0.06f), leaf, mat: Leaf, blend: 0.03f);
        // Leaves on its legs
        foreach (int i in Enumerable.Range(0, b.Model.Bones.Count).Where(i => HasRole(b, i, PokeRole.Leg)))
        {
            var j = b.Model.Skeleton[i].Joint;
            float s = MathF.Sign(j.X);
            Frond(b, i, V(j.X, 0.22f, j.Z), V(j.X + 0.04f * s, 0.14f, j.Z - 0.03f), 0.022f, leaf, V(s, 0.2f, 0), 0.22f, Leaf, 0.006f);
        }
        // The plume of a tail with buds
        int tail = BoneOf(b, "tail");
        var root = bc + V(0, 0.03f, -0.19f);
        for (int i = 0; i < 6; i++)
        {
            float a = (i - 2.5f) * 0.3f;
            var tip = root + V(MathF.Sin(a) * 0.08f, 0.04f + MathF.Cos(a) * 0.04f, -0.2f);
            Frond(b, tail, root, tip, 0.03f, i % 2 == 0 ? leaf : dark, V(0, 1f, 0.2f), 0.25f, Leaf, 0.006f);
            if (i % 2 == 1) b.Ell(tail, tip, V(0.014f, 0.02f, 0.014f), Rgb(170, 180, 110), mat: Leaf, blend: 0.004f);
        }
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }
}
