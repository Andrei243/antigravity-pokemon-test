using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Paldea's last batch in National Pokédex
// order: Wo-Chien (1001) to Pecharunt (1025), so every species of the National Pokédex. Their forms are here beside
// them (Koraidon's builds, Miraidon's modes, Ogerpon's masks, Terapagos's Terastal and Stellar Forms). Helpers shared
// with the earlier batches are in the files of those batches, PokemonModels.Sinnoh1.cs to PokemonModels.Paldea4.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Wo-Chien

    /// <summary>
    /// Wo-Chien: a slug of ruin under a mound of dark moss ragged at its foot, a cone of wooden tablets bound in two
    /// bands on its back, and at its front a head heaped with olive and tan leaves, two orange eyes in the shadow under
    /// a crown of leaves and two pale antlers curling out of its top, a leaf on each.
    /// </summary>
    private static PokeBuilder WoChien()
    {
        var b = new PokeBuilder("Wo-Chien", 0.85f, BodyPlan.Floating, V(0, 0.16f, -0.02f)) { Coat = Leaf };
        var moss = Rgb(58, 64, 48);
        var dark = Rgb(40, 44, 36);
        var olive = Rgb(122, 128, 74);
        var tan = Rgb(198, 170, 112);
        var wood = Rgb(142, 102, 66);
        var band = Rgb(92, 64, 44);
        var bc = V(0, 0.13f, -0.02f);
        var br = V(0.3f, 0.14f, 0.27f);
        b.Ell(Body, bc, br, moss);
        // The ragged foot of the mound
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f + 0.1f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(n.X * br.X * 0.92f, -0.06f, n.Z * br.Z * 0.92f);
            b.Spike(Body, root, root + n * 0.07f + V(0, -0.055f, 0), 0.04f, i % 2 == 0 ? dark : moss, 0.45f, blend: 0.012f);
        }
        foreach (var (d, size) in new[] { (V(0.6f, 0.7f, -0.3f), 0.05f), (V(-0.7f, 0.6f, 0.1f), 0.06f), (V(0.2f, 0.8f, -0.6f), 0.045f), (V(-0.3f, 0.5f, -0.8f), 0.05f) })
            b.PaintEll(Body, Out(bc, br, default, d), V(size, size * 0.5f, size), Rgb(84, 92, 62), soft: 0.02f);
        // The cone of wooden tablets on its back, bound in two dark bands
        var foot = V(0.07f, 0.2f, -0.1f);
        var top = V(0.11f, 0.52f, -0.19f);
        var axis = Vector3.Normalize(top - foot);
        b.Limb(Body, foot, top, 0.16f, 0.07f, wood, Shell, 0.01f);
        b.Ell(Body, top + axis * 0.01f, V(0.075f, 0.03f, 0.075f), wood, Euler(axis), Shell, 0.006f);
        foreach (float t in new[] { 0.35f, 0.72f })
            b.Torus(Body, Vector3.Lerp(foot, top, t), 0.16f + (0.07f - 0.16f) * t + 0.004f, 0.012f, band, Euler(axis), mat: Shell, blend: 0.003f);
        var side = Vector3.Normalize(Vector3.Cross(axis, V(0, 0, 1f)));
        var front = Vector3.Cross(side, axis);
        for (int k = 0; k < 9; k++)
        {
            float a = k * MathF.Tau / 9f;
            var radial = side * MathF.Cos(a) + front * MathF.Sin(a);
            foreach (float t in new[] { 0.18f, 0.55f, 0.88f })
            {
                float rad = 0.16f + (0.07f - 0.16f) * t;
                b.PaintEll(Body, Vector3.Lerp(foot, top, t) + radial * rad, V(0.004f, 0.06f, 0.004f), band, Euler(axis), 0.004f);
            }
        }
        Petal(b, Body, top + V(0.02f, -0.12f, 0.06f), top + V(0.08f, -0.06f, 0.1f), 0.022f, Rgb(84, 130, 70), Leaf);
        // The head at its front, heaped with leaves
        int head = b.Head(V(-0.03f, 0.22f, 0.12f));
        var c = V(-0.03f, 0.36f, 0.11f);
        var r = V(0.12f, 0.13f, 0.1f);
        b.Ell(head, c, r, olive);
        b.Ell(head, c + V(0, -0.1f, -0.01f), V(0.13f, 0.08f, 0.1f), olive);
        for (int i = 0; i < 11; i++)
        {
            float a = (-100f + 20f * i) * Degree;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            var from = c + V(n.X * 0.09f, -0.02f, n.Z * 0.07f);
            Petal(b, head, from, from + n * 0.07f + V(0, -0.16f, 0), 0.05f, i % 2 == 0 ? tan : olive, Leaf);
        }
        for (int i = 0; i < 7; i++)
        {
            float a = (-75f + 25f * i) * Degree;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            var from = c + V(n.X * 0.08f, 0.06f, n.Z * 0.07f);
            Petal(b, head, from, from + n * 0.06f + V(0, -0.1f, 0), 0.04f, i % 2 == 0 ? olive : tan, Leaf);
        }
        // The shadow its eyes look out of, under a crown of little leaves
        b.PaintEll(head, c + V(0, 0.05f, 0.08f), V(0.08f, 0.03f, 0.04f), Rgb(46, 42, 34), soft: 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.035f * s, c.Y + 0.05f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(244, 146, 40), glare: true);
        });
        foreach (var (x, lean) in new[] { (-0.04f, -0.5f), (0f, 0f), (0.04f, 0.5f) })
            Petal(b, head, c + V(x, 0.1f, 0.02f), c + V(x + lean * 0.05f, 0.17f, 0.05f), 0.025f, Rgb(108, 146, 76), Leaf);
        // Two pale antlers curling out of its top, a leaf on each
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.11f, -0.02f));
            var root = c + V(0.05f * s, 0.1f, -0.02f);
            var pts = Smooth(3, root, root + V(0.02f * s, 0.12f, -0.01f), root + V(0.09f * s, 0.22f, -0.03f), root + V(0.16f * s, 0.21f, -0.03f), root + V(0.16f * s, 0.15f, -0.01f));
            b.Tube(ear, pts, 0.013f, 0.008f, Rgb(206, 200, 178), Shell, 0f);
            Petal(b, ear, pts[4], pts[4] + V(0.05f * s, 0.03f, 0.02f), 0.018f, Rgb(108, 146, 76), Leaf);
        });
        return b;
    }

    // ------------------------------------------------------------------ Chien-Pao

    /// <summary>
    /// Chien-Pao: a snow leopard of ruin, white and long, spotted with pale blue diamonds, a spiky white mane over two
    /// red eyes, and from its cheeks two dark green cords hanging to blades of ice like swords; a long spotted tail
    /// curling at its end.
    /// </summary>
    private static PokeBuilder ChienPao()
    {
        var b = new PokeBuilder("Chien-Pao", 0.95f, BodyPlan.Quadruped, V(0, 0.42f, 0)) { Coat = Fur };
        var white = Rgb(238, 242, 248);
        var spot = Rgb(150, 196, 226);
        var cord = Rgb(44, 92, 92);
        var ice = Rgb(170, 220, 244);
        BeastLegs(b, 0.095f, 0.34f, 0.17f, -0.19f, 0.064f, white, white);
        var bc = V(0, 0.41f, -0.02f);
        var br = V(0.135f, 0.125f, 0.25f);
        b.Ell(Body, bc, br, white);
        b.Ell(Body, V(0, 0.46f, 0.15f), V(0.145f, 0.15f, 0.14f), white);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 26; i++)
        {
            float y = 0.9f - 1.3f * (i + 0.5f) / 26f, ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var at = Out(bc, br, default, V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring));
            b.PaintEll(Body, at, V(0.026f, 0.013f, 0.013f), spot, V(0, a * 30f, 45f), 0.006f);
        }
        int head = b.Head(V(0, 0.54f, 0.22f));
        var c = V(0, 0.59f, 0.3f);
        var r = V(0.095f, 0.085f, 0.1f);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.025f, 0.08f), V(0.055f, 0.045f, 0.05f), white);
        b.PaintEll(head, c + V(0, -0.01f, 0.13f), V(0.016f, 0.012f, 0.012f), Rgb(80, 90, 110), soft: 0.004f);
        // The spiky white mane over its brow and down its neck
        for (int i = 0; i < 11; i++)
        {
            float a = (-100f + 20f * i) * Degree;
            var root = c + V(MathF.Sin(a) * 0.08f, 0.05f, MathF.Cos(a) * 0.04f - 0.02f);
            b.Spike(head, root, root + V(MathF.Sin(a) * 0.09f, 0.06f - MathF.Abs(MathF.Sin(a)) * 0.05f, -0.09f), 0.036f, white, 0.5f);
        }
        b.Ell(head, c + V(0, -0.02f, -0.09f), V(0.1f, 0.09f, 0.08f), white);
        b.Spike(head, c + V(0, 0.06f, 0.05f), c + V(0, 0.05f, 0.13f), 0.03f, white, 0.5f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.04f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(214, 52, 62), glare: true);
            // A cord from each cheek down to a blade of ice
            var cheek = c + V(0.06f * s, -0.035f, 0.06f);
            var hang = cheek + V(0.02f * s, -0.18f, 0.03f);
            b.Tube(head, Smooth(3, cheek, cheek + V(0.02f * s, -0.06f, 0.02f), hang), 0.012f, 0.01f, cord, blend: 0.004f);
            b.Torus(head, hang, 0.016f, 0.007f, Rgb(40, 40, 52), mat: Metal, blend: 0.003f);
            Blade(b, head, hang + V(0, -0.005f, 0), hang + V(0, -0.2f, 0.01f), 0.03f, ice, V(1f * s, 0, 0.2f), 0.3f, Glow);
        });
        int tail = b.Tail(bc + V(0, 0.02f, -0.24f));
        var tp = Smooth(3, bc + V(0, 0.03f, -0.22f), bc + V(0, -0.04f, -0.4f), bc + V(0.06f, -0.2f, -0.5f), bc + V(0.12f, -0.28f, -0.42f), bc + V(0.08f, -0.24f, -0.33f));
        b.Tube(tail, tp, 0.045f, 0.03f, white, blend: 0f);
        for (int i = 2; i < tp.Length - 1; i += 2)
            b.PaintEll(tail, tp[i] + V(0, 0.035f, 0), V(0.022f, 0.012f, 0.012f), spot, V(0, 0, 45f), 0.005f);
        return b;
    }

    // ------------------------------------------------------------------ Ting-Lu

    /// <summary>
    /// Ting-Lu: a moose of ruin as heavy as a hill, its blocky brown body banded rust-red on legs like pillars, its
    /// small face low at its front, and on its shoulders a great ritual vessel of green bronze, engraved and rusted red,
    /// with four prongs rising from it like antlers.
    /// </summary>
    private static PokeBuilder TingLu()
    {
        var b = new PokeBuilder("Ting-Lu", 1f, BodyPlan.Quadruped, V(0, 0.42f, -0.02f)) { Coat = Shell };
        var brown = Rgb(122, 96, 76);
        var dark = Rgb(90, 68, 54);
        var rust = Rgb(156, 74, 54);
        var bronze = Rgb(62, 104, 92);
        var shade = Rgb(42, 74, 66);
        foreach (var (z, front) in new[] { (0.18f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.13f * s, 0.32f, z), front);
                b.Box(leg, V(0.135f * s, 0.18f, z), V(0.072f, 0.16f, 0.078f), 0.04f, brown, mat: Shell, blend: 0.01f);
                b.Box(leg, V(0.138f * s, 0.035f, z + 0.005f), V(0.078f, 0.035f, 0.084f), 0.028f, dark, mat: Shell, blend: 0.006f);
                b.PaintEll(leg, V(0.135f * s, 0.2f, z), V(0.09f, 0.022f, 0.1f), rust, soft: 0.008f);
            });
        var bc = V(0, 0.44f, -0.04f);
        b.Box(Body, bc, V(0.2f, 0.15f, 0.29f), 0.1f, brown, mat: Shell, blend: 0.02f);
        b.Ell(Body, V(0, 0.52f, 0.13f), V(0.22f, 0.18f, 0.17f), brown);
        foreach (float z in new[] { -0.22f, -0.1f, 0.02f })
            b.PaintTorus(Body, V(0, 0.45f, z), 0.19f, 0.04f, rust, V(90f, 0, 0));
        // Its small face low at its front
        int head = b.Head(V(0, 0.42f, 0.3f));
        var c = V(0, 0.36f, 0.42f);
        b.Limb(head, V(0, 0.45f, 0.2f), c + V(0, 0.02f, -0.04f), 0.09f, 0.075f, brown, Shell, 0.02f);
        b.Box(head, c, V(0.085f, 0.07f, 0.09f), 0.05f, brown, mat: Shell, blend: 0.02f);
        b.PaintEll(head, c + V(0, 0.03f, 0.09f), V(0.09f, 0.012f, 0.02f), rust, soft: 0.006f);
        b.PaintEll(head, c + V(0, -0.045f, 0.09f), V(0.05f, 0.012f, 0.02f), Rgb(60, 40, 36), soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            b.Eye(head, c + V(0.042f * s, 0.005f, 0.09f), V(0.25f * s, 0.05f, 1f), 0.011f, Rgb(214, 60, 48), glare: true);
            b.Spike(head, c + V(0.05f * s, -0.06f, 0.08f), c + V(0.055f * s, -0.035f, 0.11f), 0.008f, Rgb(236, 232, 220), mat: Shell, blend: 0.003f);
        });
        // The great bronze vessel on its shoulders, tipped forward
        var vc = V(0, 0.78f, 0.12f);
        var vr = V(0.24f, 0.15f, 0.2f);
        var tilt = V(15f, 0, 0);
        var turn = Quaternion.CreateFromYawPitchRoll(0, 15f * Degree, 0);
        Vector3 InVessel(Vector3 p) => vc + Vector3.Transform(p, turn);
        b.Ell(Body, vc, vr, bronze, tilt, Metal, 0.03f);
        b.Cut(Body, InVessel(V(0, 0.09f, 0)), V(0.21f, 0.1f, 0.17f), tilt);
        b.PaintEll(Body, InVessel(V(0, 0.04f, 0)), V(0.2f, 0.05f, 0.16f), shade, tilt, 0.01f);
        b.Torus(Body, InVessel(V(0, 0.05f, 0)), 0.218f, 0.018f, shade, tilt, 1f, 0.87f, Metal, 0.004f);
        foreach (var (x, y) in new[] { (-0.1f, -0.03f), (0.1f, -0.03f), (0f, -0.07f), (-0.05f, -0.09f), (0.05f, -0.09f) })
            b.PaintEll(Body, InVessel(V(x, y, 0.19f)), V(0.035f, 0.02f, 0.03f), shade, tilt, 0.004f);
        foreach (var d in new[] { V(-0.8f, -0.2f, 0.6f), V(0.7f, -0.4f, -0.5f), V(0.2f, -0.6f, 0.8f), V(-0.4f, -0.5f, -0.7f) })
            b.PaintEll(Body, vc + Vector3.Transform(Out(Vector3.Zero, vr, default, d), turn), V(0.03f, 0.025f, 0.03f), rust, soft: 0.008f);
        foreach (var (x, z, h, lean) in new[] { (-0.15f, 0.07f, 0.38f, -0.09f), (0.15f, 0.07f, 0.38f, 0.09f), (-0.14f, -0.08f, 0.22f, -0.08f), (0.14f, -0.08f, 0.22f, 0.08f), (0f, -0.13f, 0.18f, 0f) })
        {
            var root = InVessel(V(x, 0.03f, z));
            b.Spike(Body, root, root + V(lean, h, -0.05f), 0.045f, bronze, 0.7f, Metal, 0.008f);
        }
        int tail = b.Tail(bc + V(0, 0.04f, -0.27f));
        b.Spike(tail, bc + V(0, 0.04f, -0.26f), bc + V(0, -0.02f, -0.36f), 0.035f, dark, 0.7f);
        return b;
    }

    // ------------------------------------------------------------------ Chi-Yu

    /// <summary>
    /// Chi-Yu: a little goldfish of ruin, round and orange with red curls of flame on it, big black eyes in two rings
    /// of dark jade like spectacles, and fins of flame: a pair at its sides, one over its back and three streaming
    /// from its tail.
    /// </summary>
    private static PokeBuilder ChiYu()
    {
        var b = new PokeBuilder("Chi-Yu", 0.45f, BodyPlan.Fish, V(0, 0.2f, 0)) { Coat = Scales }.Hover();
        var orange = Rgb(246, 150, 74);
        var pale = Rgb(252, 214, 150);
        var red = Rgb(214, 58, 40);
        var jade = Rgb(62, 104, 86);
        var bc = V(0, 0.2f, 0);
        var br = V(0.085f, 0.085f, 0.1f);
        b.Ell(Body, bc, br, orange);
        b.PaintEll(Body, bc + V(0, -0.05f, 0.03f), V(0.07f, 0.04f, 0.08f), pale, soft: 0.015f);
        foreach (var (p, turn) in new[] { (V(0.06f, 0.04f, -0.04f), 30f), (V(-0.06f, 0.04f, -0.03f), -30f), (V(0.0f, 0.075f, -0.02f), 0f), (V(0.07f, -0.01f, -0.06f), 60f), (V(-0.07f, -0.01f, -0.06f), -60f) })
            b.PaintEll(Body, bc + p, V(0.012f, 0.03f, 0.012f), red, V(0, 0, turn), 0.006f);
        // Big black eyes in rings of jade, joined over its snout
        PokeBuilder.Both(s =>
        {
            var at = Out(bc, br, default, V(0.75f * s, 0.15f, 0.65f));
            var n = Outward(bc, br, at);
            b.Eye(Body, at, n, 0.017f, Rgb(36, 40, 44), pupil: Rgb(20, 20, 24));
            b.Torus(Body, at + n * 0.004f, 0.026f, 0.008f, jade, Euler(n), mat: Shell, blend: 0.004f);
        });
        b.Tube(Body, Smooth(2, Out(bc, br, default, V(-0.45f, 0.2f, 0.88f)), bc + V(0, 0.03f, 0.105f), Out(bc, br, default, V(0.45f, 0.2f, 0.88f))), 0.006f, 0.006f, jade, Shell, 0.004f);
        b.PaintEll(Body, bc + V(0, -0.025f, 0.1f), V(0.015f, 0.006f, 0.01f), Rgb(150, 50, 40), soft: 0.004f);
        // Fins of flame
        PokeBuilder.Both(s =>
        {
            int fin = b.Wing(s, bc + V(0.07f * s, -0.02f, 0.01f));
            Frond(b, fin, bc + V(0.07f * s, -0.02f, 0.01f), bc + V(0.15f * s, -0.07f, -0.04f), 0.035f, red, V(0, 1f, 0.3f), 0.22f, Glow, 0.01f);
        });
        Frond(b, Body, bc + V(0, 0.07f, -0.02f), bc + V(0, 0.16f, -0.1f), 0.04f, red, V(1f, 0, 0), 0.22f, Glow, 0.012f);
        int tail = b.Tail(bc + V(0, 0, -0.09f));
        foreach (var (x, y, w) in new[] { (0f, 0.02f, 0.05f), (0.06f, 0.07f, 0.035f), (-0.06f, 0.07f, 0.035f), (0.05f, -0.04f, 0.03f), (-0.05f, -0.04f, 0.03f) })
        {
            var tip = bc + V(x * 1.6f, y * 1.4f, -0.3f);
            var mid = bc + V(x, y, -0.18f);
            Frond(b, tail, bc + V(0, 0, -0.08f), mid, w, red, V(0, 0, 1f) + V(x == 0 ? 0 : 0.5f, 1f, 0), 0.2f, Glow, 0.012f);
            Frond(b, tail, mid - (mid - bc) * 0.15f, tip, w * 0.7f, Rgb(232, 96, 50), V(0, 1f, 0), 0.22f, Glow, 0.01f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Roaring Moon

    /// <summary>
    /// Roaring Moon: Salamence's ancient past, a blue dragon with a red chest and a black shaggy skirt of feathers, a
    /// grey crest swept back from its brow, red eyes, clawed arms reaching forward, and two great crimson wings that
    /// rise and bend over it into a crescent, scalloped along their edge in pale blue.
    /// </summary>
    private static PokeBuilder RoaringMoon()
    {
        var b = new PokeBuilder("Roaring Moon", 1f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(60, 140, 204);
        var red = Rgb(206, 38, 66);
        var deep = Rgb(150, 24, 52);
        var pale = Rgb(132, 196, 232);
        var black = Rgb(36, 34, 44);
        var grey = Rgb(206, 214, 224);
        var bc = V(0, 0.42f, 0.02f);
        // The crescent of its wings, made first and joined to its shoulders
        var mid = V(0, 0.66f, -0.06f);
        const float radius = 0.42f;
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.08f * s, 0.06f, -0.03f);
            int wing = b.Wing(s, shoulder);
            var start = mid + V(MathF.Cos(-18f * Degree) * radius * s, MathF.Sin(-18f * Degree) * radius, 0);
            b.Limb(wing, shoulder, start + V(-0.02f * s, 0.02f, 0), 0.04f, 0.03f, deep);
            var arc = new List<Vector3>();
            for (int i = 0; i <= 10; i++)
            {
                float a = (-18f + 9.4f * i) * Degree;
                arc.Add(mid + V(MathF.Cos(a) * radius * s, MathF.Sin(a) * radius, -0.02f * i * 0.3f));
            }
            for (int i = 0; i < arc.Count - 1; i++)
            {
                var step = arc[i + 1] - arc[i];
                Frond(b, wing, arc[i] - step * 0.35f, arc[i + 1] + step * 0.35f, 0.09f, i % 2 == 0 ? red : Rgb(196, 34, 62), V(0, 0, 1f), 0.2f, Fur, 0.01f);
                var outward = Vector3.Normalize(arc[i] - mid);
                b.Spike(wing, arc[i] + outward * 0.05f, arc[i] + outward * 0.13f + V(0, 0, -0.01f), 0.03f, pale, 0.35f, Fur, 0.008f);
            }
            var last = arc[^1];
            b.Spike(wing, last, last + Vector3.Normalize(last - arc[^2]) * 0.08f, 0.04f, pale, 0.35f, Fur, 0.008f);
            b.PaintEll(wing, mid + V(MathF.Cos(30f * Degree) * (radius - 0.06f) * s, MathF.Sin(30f * Degree) * (radius - 0.06f), 0), V(0.05f, 0.18f, 0.05f), deep, V(0, 0, -60f * s));
        });
        b.Ell(Body, bc, V(0.12f, 0.15f, 0.13f), blue);
        b.PaintEll(Body, bc + V(0, -0.01f, 0.08f), V(0.07f, 0.1f, 0.06f), red, soft: 0.012f);
        // The black shaggy skirt under it
        for (int i = 0; i < 11; i++)
        {
            float a = (-150f + 30f * i) * Degree;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(n.X * 0.07f, -0.08f, n.Z * 0.08f - 0.02f);
            Blade(b, Body, root, root + n * 0.08f + V(0, -0.22f, -0.03f), 0.035f, black, n, 0.3f, Fur);
        }
        // Clawed arms reaching forward, and short legs tucked under
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.08f * s, 0.05f, 0.06f));
            var elbow = bc + V(0.13f * s, -0.02f, 0.12f);
            var hand = bc + V(0.11f * s, -0.07f, 0.2f);
            b.Limb(arm, bc + V(0.08f * s, 0.05f, 0.06f), elbow, 0.03f, 0.025f, blue);
            b.Limb(arm, elbow, hand, 0.025f, 0.022f, blue);
            Claws(b, arm, hand, V(0.012f * s, 0, 0), V(0.1f * s, -0.4f, 1f), 0.035f, 0.008f);
        });
        // The head, its grey crest swept back
        int head = b.Head(bc + V(0, 0.11f, 0.06f));
        var c = bc + V(0, 0.18f, 0.12f);
        var r = V(0.055f, 0.05f, 0.07f);
        b.Limb(head, bc + V(0, 0.07f, 0.04f), c + V(0, -0.02f, -0.03f), 0.05f, 0.042f, blue);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, -0.02f, 0.06f), V(0.04f, 0.03f, 0.04f), blue);
        b.PaintEll(head, c + V(0, -0.035f, 0.08f), V(0.035f, 0.01f, 0.03f), red, soft: 0.005f);
        foreach (var (x, y, len) in new[] { (0f, 0.04f, 0.16f), (-0.04f, 0.02f, 0.13f), (0.04f, 0.02f, 0.13f), (-0.05f, -0.01f, 0.1f), (0.05f, -0.01f, 0.1f) })
            b.Spike(head, c + V(x, y, 0.0f), c + V(x * 2.2f, y + 0.07f, -len), 0.026f, grey, 0.45f, Shell, 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.03f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(232, 56, 52), glare: true);
        });
        int tail = b.Tail(bc + V(0, -0.06f, -0.1f));
        var tp = Smooth(3, bc + V(0, -0.06f, -0.1f), bc + V(0, -0.2f, -0.2f), bc + V(0.05f, -0.3f, -0.12f), bc + V(0.09f, -0.31f, 0.0f));
        b.Tube(tail, tp, 0.035f, 0.012f, blue, blend: 0f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Iron Valiant

    /// <summary>
    /// Iron Valiant: Gardevoir and Gallade's future in one, a slim white machine in a gown of long green and white
    /// panels, a green helm with a crest rising from its brow and green locks at its sides tipped pink, a pink diamond
    /// on its chest, red eyes, and in its hand a long pink blade held low.
    /// </summary>
    private static PokeBuilder IronValiant()
    {
        var b = new PokeBuilder("Iron Valiant", 0.95f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Metal };
        var white = Rgb(236, 238, 244);
        var green = Rgb(64, 152, 84);
        var dark = Rgb(36, 40, 50);
        var pink = Rgb(236, 68, 140);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.5f, 0));
            b.Limb(leg, V(0.035f * s, 0.5f, 0), V(0.04f * s, 0.26f, 0.01f), 0.022f, 0.018f, white);
            b.Ell(leg, V(0.04f * s, 0.26f, 0.01f), V(0.022f, 0.022f, 0.022f), dark);
            b.Limb(leg, V(0.04f * s, 0.26f, 0.01f), V(0.045f * s, 0.04f, 0.0f), 0.018f, 0.016f, white);
            b.Ell(leg, V(0.045f * s, 0.022f, 0.012f), V(0.02f, 0.022f, 0.032f), pink);
        });
        // The gown: long panels from its hips nearly to the ground, green without and white within
        var hip = V(0, 0.52f, 0);
        b.Ell(Body, hip, V(0.07f, 0.04f, 0.06f), white);
        foreach (var (a, color) in new[] { (35f, green), (-35f, green), (145f, green), (-145f, green), (90f, white), (-90f, white), (0f, white), (180f, white) })
        {
            float rad = a * Degree;
            var n = V(MathF.Sin(rad), 0, MathF.Cos(rad));
            var root = hip + n * 0.045f + V(0, -0.01f, 0);
            Frond(b, Body, root, root + n * 0.12f + V(0, -0.44f, 0), color == green ? 0.06f : 0.045f, color, n, 0.18f, Metal, 0.012f);
        }
        b.Ell(Body, V(0, 0.6f, 0), V(0.035f, 0.05f, 0.03f), dark);
        var cc = V(0, 0.7f, 0);
        b.Ell(Body, cc, V(0.065f, 0.08f, 0.05f), white);
        b.Ell(Body, cc + V(0, 0.01f, 0.03f), V(0.03f, 0.04f, 0.025f), pink, V(0, 0, 45f), Glow, 0.006f);
        PokeBuilder.Both(s => b.Spike(Body, cc + V(0.05f * s, 0.06f, 0), cc + V(0.09f * s, 0.11f, -0.01f), 0.022f, green, 0.6f));
        // Arms: one at its side, the other holding the long pink blade low and out
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, cc + V(0.06f * s, 0.05f, 0));
            var elbow = cc + V(0.11f * s, -0.04f, 0.02f);
            var hand = s < 0 ? cc + V(0.1f * s, -0.14f, 0.05f) : cc + V(0.17f * s, -0.12f, 0.06f);
            b.Limb(arm, cc + V(0.06f * s, 0.05f, 0), elbow, 0.02f, 0.017f, white);
            b.Limb(arm, elbow, hand, 0.017f, 0.015f, white);
            b.Ell(arm, hand, V(0.022f, 0.022f, 0.022f), dark);
            Blade(b, arm, elbow, elbow + V(0.03f * s, 0.1f, -0.06f), 0.025f, green, V(1f * s, 0, 0), 0.3f, Metal);
            if (s > 0)
                Blade(b, arm, hand, hand + V(0.38f, -0.32f, 0.06f), 0.034f, pink, V(0, 0, 1f), 0.45f, Glow);
        });
        // The helm: a crest rising from its brow, green locks at its sides tipped pink, red eyes in a white face
        int head = b.Head(cc + V(0, 0.08f, 0));
        var c = cc + V(0, 0.17f, 0.01f);
        var r = V(0.055f, 0.06f, 0.055f);
        b.Ell(head, c, r, green);
        b.PaintEll(head, c + V(0, -0.02f, 0.05f), V(0.04f, 0.035f, 0.02f), white, soft: 0.006f);
        b.Spike(head, c + V(0, 0.05f, 0.03f), c + V(0, 0.2f, -0.02f), 0.03f, green, 0.4f, Metal);
        PokeBuilder.Both(s =>
        {
            Frond(b, head, c + V(0.045f * s, 0.01f, 0), c + V(0.07f * s, -0.1f, 0.01f), 0.03f, green, V(1f * s, 0, 0.2f), 0.3f, Metal, 0.01f);
            b.PaintEll(head, c + V(0.07f * s, -0.09f, 0.01f), V(0.03f, 0.025f, 0.03f), pink, soft: 0.006f);
            var at = On(c, r, c.X + 0.02f * s, c.Y - 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(232, 40, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Koraidon

    private enum KoraidonShape { Apex, Limited, Sprinting, Swimming, Gliding }

    private static readonly Color KoraidonRed = Rgb(222, 72, 52);
    private static readonly Color KoraidonDark = Rgb(164, 44, 38);
    private static readonly Color KoraidonTire = Rgb(40, 38, 44);

    /// <summary>A wheel of Koraidon's or Miraidon's at <paramref name="at"/> with its axle along <paramref name="axle"/>: a fat tyre round a hub, and spokes painted on its faces.</summary>
    private static void RideWheel(PokeBuilder b, int bone, Vector3 at, Vector3 axle, float ring, float tube, Color tyre, Color hub, Color spokes, SurfaceMaterial tyreMat, SurfaceMaterial hubMat)
    {
        var turn = Euler(axle);
        b.Torus(bone, at, ring, tube, tyre, turn, mat: tyreMat, blend: 0.006f);
        b.Ell(bone, at, V(ring, tube * 0.6f, ring), hub, turn, hubMat, 0.008f);
        var n = Vector3.Normalize(axle);
        var u = Vector3.Normalize(Vector3.Cross(n, MathF.Abs(n.Y) > 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
        var w = Vector3.Cross(n, u);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            var d = u * MathF.Cos(a) + w * MathF.Sin(a);
            foreach (float side in new[] { -1f, 1f })
                b.PaintEll(bone, at + n * tube * 0.6f * side + d * ring * 0.62f, V(ring * 0.2f, ring * 0.2f, ring * 0.2f), spokes, soft: 0.006f);
        }
    }

    /// <summary>
    /// Koraidon in each of its builds. The Apex Build stands tall: a red dragon on clawed legs, the black segments of
    /// its tyres folded on its chest and back, fans of white feathers tipped purple at its hips, a crest of navy and
    /// pink feathers, a long braided white plume curling from its head, and a red tail frilled along its top. The
    /// Limited Build crouches with its crest and fans folded and its plume curled up. The Sprinting Build lies along
    /// its two tyres, red and white at their hubs, to be ridden; the Swimming Build lays its tyres flat as floats and
    /// spreads its fans as fins; the Gliding Build opens its feathers into two great white wings banded purple.
    /// </summary>
    private static PokeBuilder KoraidonBuild(KoraidonShape shape, string name)
    {
        bool riding = shape is KoraidonShape.Sprinting or KoraidonShape.Swimming or KoraidonShape.Gliding;
        bool limited = shape == KoraidonShape.Limited;
        var b = new PokeBuilder(name, 1f, riding ? BodyPlan.Quadruped : BodyPlan.Biped, riding ? V(0, 0.36f, 0) : V(0, limited ? 0.4f : 0.5f, 0)) { Coat = Scales };
        var red = KoraidonRed;
        var dark = KoraidonDark;
        var tire = KoraidonTire;
        var white = Rgb(242, 240, 236);
        var purple = Rgb(150, 86, 170);
        var navy = Rgb(48, 64, 150);
        var pink = Rgb(232, 92, 150);
        Vector3 neck, headC, tailRoot;
        Vector3[] tail;
        if (!riding)
        {
            float k = limited ? 0.8f : 1f;
            PokeBuilder.Both(s =>
            {
                var hipAt = V(0.1f * s, 0.42f * k, -0.03f);
                int leg = b.Leg(s, hipAt);
                var knee = V(0.13f * s, 0.24f * k, limited ? 0.1f : 0.06f);
                var ankle = V(0.12f * s, 0.07f, -0.04f);
                b.Limb(leg, hipAt, knee, 0.07f, 0.05f, red);
                b.Limb(leg, knee, ankle, 0.045f, 0.035f, red);
                b.Ell(leg, ankle + V(0, -0.03f, 0.05f), V(0.04f, 0.028f, 0.07f), red);
                Claws(b, leg, ankle + V(0, -0.035f, 0.11f), V(0.018f * s, 0, 0), V(0, -0.3f, 1f), 0.035f, 0.01f);
            });
            var bc = V(0, 0.56f * k, 0.02f);
            b.Ell(Body, bc, V(0.13f, 0.17f * k, 0.12f), red);
            var chest = bc + V(0, 0.16f * k, 0.04f);
            b.Ell(Body, chest, V(0.12f, 0.12f, 0.11f), red);
            // The black segments of its folded tyres, on its chest and its back
            PokeBuilder.Both(s =>
            {
                b.Ell(Body, chest + V(0.065f * s, -0.04f, 0.07f), V(0.07f, 0.12f * k, 0.065f), tire, mat: Shell, blend: 0.01f);
                foreach (float y in new[] { -0.08f, 0f, 0.08f })
                    b.PaintEll(Body, chest + V(0.075f * s, -0.04f + y * k, 0.135f), V(0.05f, 0.006f, 0.02f), Rgb(84, 80, 90), soft: 0.004f);
            });
            b.Ell(Body, bc + V(0, 0.08f, -0.09f), V(0.14f, 0.17f * k, 0.09f), tire, mat: Shell, blend: 0.012f);
            // Fans of white feathers tipped purple at its hips, folded close in its Limited Build
            PokeBuilder.Both(s =>
            {
                for (int i = 0; i < 6; i++)
                {
                    float a = (limited ? 60f + 6f * i : 20f + 18f * i) * Degree;
                    var dir = Vector3.Normalize(V(MathF.Cos(a) * s, -MathF.Sin(a) * 0.6f, -0.35f - (limited ? 0.6f : 0f)));
                    var root = bc + V(0.1f * s, -0.06f * k, -0.04f);
                    float len = limited ? 0.17f : 0.27f + 0.03f * (i % 3);
                    Frond(b, Body, root, root + dir * len, 0.04f, white, V(0, 0.3f, 1f), 0.22f, Fur, 0.01f);
                    b.PaintEll(Body, root + dir * len * 0.86f, V(0.04f, len * 0.16f, 0.04f), purple, Euler(dir));
                }
            });
            PokeBuilder.Both(s =>
            {
                int arm = b.Arm(s, chest + V(0.1f * s, 0.02f, 0.02f));
                var elbow = chest + V(0.17f * s, -0.1f, 0.07f);
                var hand = chest + V(0.14f * s, -0.16f, 0.16f);
                b.Limb(arm, chest + V(0.1f * s, 0.02f, 0.02f), elbow, 0.035f, 0.03f, red);
                b.Limb(arm, elbow, hand, 0.03f, 0.026f, red);
                Claws(b, arm, hand, V(0.012f * s, 0, 0), V(0.2f * s, -0.4f, 1f), 0.04f, 0.009f);
            });
            neck = chest + V(0, 0.08f, 0.04f);
            headC = chest + V(0, limited ? 0.19f : 0.25f, 0.12f);
            b.Limb(Body, chest + V(0, 0.04f, 0.02f), headC + V(0, -0.05f, -0.04f), 0.06f, 0.045f, red);
            tailRoot = bc + V(0, -0.08f, -0.1f);
            tail = Smooth(3, tailRoot, bc + V(0, -0.24f * k, -0.3f), V(0.06f, 0.07f, -0.52f), V(0.14f, 0.05f, -0.66f));
        }
        else
        {
            // Lying along its two tyres to be ridden
            var bc = V(0, 0.4f, 0);
            b.Ell(Body, bc, V(0.12f, 0.1f, 0.26f), red);
            b.Ell(Body, bc + V(0, 0.03f, 0.2f), V(0.11f, 0.1f, 0.1f), red);
            bool flat = shape == KoraidonShape.Swimming;
            foreach (float z in new[] { 0.22f, -0.2f })
            {
                if (flat)
                    PokeBuilder.Both(s => RideWheel(b, Body, V(0.15f * s, 0.33f, z * 0.9f), V(0, 1f, 0), 0.11f, 0.05f, tire, Rgb(238, 236, 232), red, Shell, Shell));
                else
                    RideWheel(b, Body, V(0, 0.2f, z), V(1f, 0, 0), 0.13f, 0.06f, tire, red, Rgb(244, 242, 238), Shell, Shell);
            }
            // Its legs stretched out behind along its flanks, its arms gripping the front tyre
            PokeBuilder.Both(s =>
            {
                var hipAt = bc + V(0.1f * s, -0.02f, -0.16f);
                int leg = b.Leg(s, hipAt, false);
                var knee = bc + V(0.14f * s, -0.04f, -0.3f);
                var foot = bc + V(0.13f * s, 0.0f, -0.44f);
                b.Limb(leg, hipAt, knee, 0.055f, 0.04f, red);
                b.Limb(leg, knee, foot, 0.04f, 0.03f, red);
                Claws(b, leg, foot, V(0, 0.012f, 0), V(0.1f * s, 0.1f, -1f), 0.035f, 0.009f);
                int arm = b.Arm(s, bc + V(0.1f * s, 0, 0.2f));
                var hand = bc + V(0.13f * s, -0.1f, 0.3f);
                b.Limb(arm, bc + V(0.1f * s, 0, 0.2f), hand, 0.035f, 0.028f, red);
                Claws(b, arm, hand, V(0.012f * s, 0, 0), V(0, -0.6f, 1f), 0.035f, 0.009f);
            });
            // Its feathers: folded at its hips, spread as fins in water, opened into wings to glide
            if (shape == KoraidonShape.Gliding)
                PokeBuilder.Both(s => SpreadWing(b, s, bc + V(0.09f * s, 0.07f, 0.02f), bc + V(0.42f * s, 0.2f, -0.08f), 8, 5f, 75f, 0.42f, 0.055f, white, purple, 0.4f, false, 2));
            else
                PokeBuilder.Both(s =>
                {
                    for (int i = 0; i < 5; i++)
                    {
                        var dir = flat ? Vector3.Normalize(V(1f * s, 0.1f, -0.25f - 0.25f * i)) : Vector3.Normalize(V(0.6f * s, 0.15f, -1f));
                        var root = bc + V(0.1f * s, 0.02f, -0.05f - 0.03f * i);
                        float len = flat ? 0.2f : 0.16f;
                        Frond(b, Body, root, root + dir * len, 0.03f, white, V(0, 1f, 0), 0.22f, Fur, 0.01f);
                        b.PaintEll(Body, root + dir * len * 0.86f, V(0.04f, len * 0.16f, 0.04f), purple, Euler(dir));
                    }
                });
            neck = bc + V(0, 0.06f, 0.28f);
            headC = bc + V(0, 0.2f, 0.4f);
            b.Limb(Body, bc + V(0, 0.05f, 0.24f), headC + V(0, -0.04f, -0.05f), 0.06f, 0.045f, red);
            tailRoot = bc + V(0, 0.02f, -0.24f);
            tail = Smooth(3, tailRoot, bc + V(0, 0.04f, -0.4f), bc + V(0, 0.0f, -0.56f), bc + V(0, -0.06f, -0.7f));
        }
        // The tail, frilled along its top
        int tb = b.Tail(tailRoot);
        b.Tube(tb, tail, 0.07f, 0.02f, red, blend: 0f);
        for (int i = 1; i < tail.Length - 1; i++)
        {
            var dir = Vector3.Normalize(tail[i + 1] - tail[i - 1]);
            var up = Vector3.Normalize(Vector3.Cross(Vector3.Cross(dir, V(0, 1f, 0)), dir));
            if (up.Y < 0) up = -up;
            float rad = 0.07f + (0.02f - 0.07f) * i / (tail.Length - 1f);
            Blade(b, tb, tail[i] + up * rad * 0.7f, tail[i] + up * (rad + 0.05f) - dir * 0.03f, 0.022f, dark, V(1f, 0, 0), 0.3f, Scales);
        }
        // The head: a dragon's, crested in navy and pink, with a long braided white plume
        int head = b.Head(neck);
        var r = V(0.065f, 0.06f, 0.085f);
        b.Ell(head, headC, r, red);
        b.Ell(head, headC + V(0, -0.02f, 0.08f), V(0.045f, 0.035f, 0.05f), red);
        b.PaintEll(head, headC + V(0, -0.045f, 0.1f), V(0.04f, 0.01f, 0.04f), Rgb(250, 240, 230), soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(headC, r, headC.X + 0.035f * s, headC.Y + 0.012f);
            b.Eye(head, at, Outward(headC, r, at), 0.011f, Rgb(246, 196, 50), glare: true);
        });
        float fold = limited ? 0.55f : 1f;
        foreach (var (x, len, color) in new[] { (0f, 0.16f, navy), (-0.04f, 0.13f, navy), (0.04f, 0.13f, navy), (-0.06f, 0.1f, Rgb(70, 110, 200)), (0.06f, 0.1f, Rgb(70, 110, 200)) })
            Blade(b, head, headC + V(x, 0.03f, -0.02f), headC + V(x * 2.2f, 0.03f + 0.1f * fold, -len * (limited ? 1.2f : 1f)), 0.03f, color, V(1f, 0, 0), 0.25f, Fur);
        Blade(b, head, headC + V(0, 0.05f, 0.02f), headC + V(0, 0.05f + 0.13f * fold, -0.05f), 0.03f, pink, V(1f, 0, 0), 0.25f, Fur);
        int plume = b.Part("plume", head, headC + V(0, 0.06f, -0.01f), PokeRole.Ear);
        var root = headC + V(0, 0.055f, -0.01f);
        var plumePts = limited
            ? Smooth(3, root, root + V(0.03f, 0.14f, -0.04f), root + V(0.08f, 0.2f, -0.16f), root + V(0.07f, 0.1f, -0.24f), root + V(0.02f, 0.05f, -0.18f))
            : riding
                ? Smooth(3, root, root + V(0.02f, 0.1f, -0.1f), root + V(0.05f, 0.14f, -0.35f), root + V(0.09f, 0.12f, -0.6f), root + V(0.12f, 0.06f, -0.8f))
                : Smooth(3, root, root + V(0.03f, 0.2f, -0.06f), root + V(0.12f, 0.3f, -0.26f), root + V(0.24f, 0.24f, -0.44f), root + V(0.3f, 0.1f, -0.5f));
        b.Tube(plume, plumePts, 0.016f, 0.01f, white, Fur, 0f);
        for (int i = 2; i < plumePts.Length - 1; i += 2)
            b.PaintEll(plume, plumePts[i], V(0.018f, 0.006f, 0.018f), Rgb(196, 192, 200), Euler(plumePts[i + 1] - plumePts[i - 1]), 0.004f);
        // Swimming, it floats
        return shape == KoraidonShape.Swimming ? Lift(b.Hover()) : b;
    }

    private static PokeBuilder Koraidon() => KoraidonBuild(KoraidonShape.Apex, "Koraidon");
    private static PokeBuilder KoraidonLimited() => KoraidonBuild(KoraidonShape.Limited, "Koraidon-Limited-Build");
    private static PokeBuilder KoraidonSprinting() => KoraidonBuild(KoraidonShape.Sprinting, "Koraidon-Sprinting-Build");
    private static PokeBuilder KoraidonSwimming() => KoraidonBuild(KoraidonShape.Swimming, "Koraidon-Swimming-Build");
    private static PokeBuilder KoraidonGliding() => KoraidonBuild(KoraidonShape.Gliding, "Koraidon-Gliding-Build");

    // ------------------------------------------------------------------ Miraidon

    private enum MiraidonShape { Ultimate, LowPower, Drive, Aquatic, Glide }

    /// <summary>
    /// Miraidon in each of its modes. The Ultimate Mode rears up: a dark violet machine of a dragon striped with
    /// glowing lilac, its wheels folded at its hips as pale rings, a long neck rising to a helmeted head with a white
    /// visor and two bolts of yellow lightning for antennae, great white claws held forward and a long tail ending in a
    /// yellow fin. The Low-Power Mode crouches small with its antennae laid back. The Drive Mode lies along two great
    /// glowing wheels with a jet of yellow light behind it; the Aquatic Mode lays its wheels flat as floats; the Glide
    /// Mode spreads a pale sail over its back.
    /// </summary>
    private static PokeBuilder MiraidonBuild(MiraidonShape shape, string name)
    {
        bool riding = shape is MiraidonShape.Drive or MiraidonShape.Aquatic or MiraidonShape.Glide;
        bool low = shape == MiraidonShape.LowPower;
        var b = new PokeBuilder(name, 1f, riding ? BodyPlan.Quadruped : BodyPlan.Biped, riding ? V(0, 0.38f, 0) : V(0, low ? 0.36f : 0.5f, 0)) { Coat = Metal };
        var violet = Rgb(76, 62, 168);
        var dark = Rgb(46, 38, 104);
        var lilac = Rgb(206, 208, 248);
        var glow = Rgb(252, 230, 120);
        var white = Rgb(240, 242, 250);
        Vector3 neckFrom, headC;
        Vector3[] neckPts, tail;
        if (!riding)
        {
            float k = low ? 0.72f : 1f;
            PokeBuilder.Both(s =>
            {
                var hipAt = V(0.1f * s, 0.42f * k, -0.04f);
                int leg = b.Leg(s, hipAt);
                var knee = V(0.15f * s, 0.24f * k, 0.08f);
                var ankle = V(0.13f * s, 0.07f, -0.04f);
                b.Limb(leg, hipAt, knee, 0.07f, 0.05f, violet);
                b.Limb(leg, knee, ankle, 0.045f, 0.035f, dark);
                b.Ell(leg, ankle + V(0, -0.03f, 0.05f), V(0.04f, 0.028f, 0.07f), dark);
                Claws(b, leg, ankle + V(0, -0.035f, 0.11f), V(0.018f * s, 0, 0), V(0, -0.3f, 1f), 0.035f, 0.01f);
                // A wheel folded at its hip, a pale ring
                b.Torus(leg, hipAt + V(0.06f * s, -0.04f, 0), 0.09f * k, 0.03f, lilac, V(0, 0, 90f), mat: Glow, blend: 0.006f);
                b.PaintTorus(leg, hipAt + V(0.06f * s, -0.04f, 0), 0.09f * k, 0.012f, glow, V(0, 0, 90f));
            });
            var bc = V(0, 0.56f * k, 0);
            b.Ell(Body, bc, V(0.12f, 0.16f * k, 0.12f), violet);
            foreach (float y in new[] { -0.08f, 0f, 0.08f })
                b.PaintTorus(Body, bc + V(0, y * k, 0), 0.12f, 0.012f, lilac, V(0, 0, 0), 1f, 1f);
            PokeBuilder.Both(s =>
            {
                var shoulder = bc + V(0.1f * s, 0.1f * k, 0.04f);
                int arm = b.Arm(s, shoulder);
                var elbow = shoulder + (low ? V(0.04f * s, -0.08f, 0.06f) : V(0.08f * s, -0.04f, 0.1f));
                var hand = elbow + (low ? V(0.0f, -0.06f, 0.06f) : V(0.02f * s, 0.02f, 0.11f));
                b.Limb(arm, shoulder, elbow, 0.035f, 0.03f, violet);
                b.Limb(arm, elbow, hand, 0.03f, 0.028f, dark);
                Digits(b, arm, hand, V(0.1f * s, -0.2f, 1f), V(0.6f * s, 0, 0) * 0.5f, 0.06f, 0.016f, white, Metal);
            });
            neckFrom = bc + V(0, 0.12f * k, 0.04f);
            neckPts = low
                ? Smooth(3, neckFrom, neckFrom + V(0, 0.1f, 0.04f), neckFrom + V(0, 0.14f, 0.12f))
                : Smooth(3, neckFrom, neckFrom + V(0, 0.18f, 0.06f), neckFrom + V(-0.03f, 0.34f, -0.02f), neckFrom + V(0, 0.46f, 0.04f));
            headC = neckPts[^1] + V(0, 0.03f, 0.06f);
            var tr = bc + V(0, -0.1f * k, -0.1f);
            tail = Smooth(3, tr, bc + V(0, -0.28f * k, -0.28f), V(0.04f, 0.08f, -0.48f), V(0.12f, 0.06f, -0.62f));
        }
        else
        {
            var bc = V(0, 0.42f, 0);
            b.Ell(Body, bc, V(0.11f, 0.09f, 0.27f), violet);
            foreach (float z in new[] { -0.12f, 0f, 0.12f })
                b.PaintTorus(Body, bc + V(0, 0, z), 0.1f, 0.012f, lilac, V(90f, 0, 0));
            bool flat = shape == MiraidonShape.Aquatic;
            foreach (float z in new[] { 0.24f, -0.2f })
            {
                if (flat)
                    PokeBuilder.Both(s => RideWheel(b, Body, V(0.15f * s, 0.35f, z * 0.9f), V(0, 1f, 0), 0.11f, 0.045f, lilac, Rgb(120, 110, 200), glow, Glow, Metal));
                else
                    RideWheel(b, Body, V(0, 0.21f, z), V(1f, 0, 0), 0.15f, 0.05f, lilac, Rgb(120, 110, 200), glow, Glow, Metal);
            }
            PokeBuilder.Both(s =>
            {
                var hipAt = bc + V(0.09f * s, -0.02f, -0.16f);
                int leg = b.Leg(s, hipAt, false);
                var foot = bc + V(0.12f * s, -0.02f, -0.4f);
                b.Limb(leg, hipAt, foot, 0.05f, 0.03f, violet);
                Claws(b, leg, foot, V(0, 0.012f, 0), V(0.1f * s, 0.1f, -1f), 0.035f, 0.009f);
                int arm = b.Arm(s, bc + V(0.09f * s, 0, 0.2f));
                var hand = bc + V(0.12f * s, -0.08f, 0.3f);
                b.Limb(arm, bc + V(0.09f * s, 0, 0.2f), hand, 0.035f, 0.028f, violet);
                Digits(b, arm, hand, V(0, -0.4f, 1f), V(0.5f * s, 0, 0) * 0.5f, 0.05f, 0.014f, white, Metal);
            });
            if (shape == MiraidonShape.Glide)
            {
                // A pale sail spread over its back
                var mast = bc + V(0, 0.08f, 0.05f);
                b.Limb(Body, mast, mast + V(0, 0.12f, -0.02f), 0.02f, 0.015f, dark);
                PokeBuilder.Both(s => Frond(b, Body, mast + V(0, 0.1f, 0.02f), mast + V(0.5f * s, 0.17f, -0.16f), 0.17f, Rgb(244, 240, 200), V(0, 1f, 0.1f), 0.1f, Glow, 0.01f));
            }
            neckFrom = bc + V(0, 0.04f, 0.24f);
            neckPts = Smooth(3, neckFrom, neckFrom + V(0, 0.06f, 0.08f), neckFrom + V(0, 0.08f, 0.16f));
            headC = neckPts[^1] + V(0, 0.02f, 0.06f);
            var tr = bc + V(0, 0, -0.25f);
            tail = Smooth(3, tr, bc + V(0, 0.02f, -0.4f), bc + V(0, 0.02f, -0.56f));
            // The jet of yellow light behind it
            Lightning(b, Body, new[] { bc + V(0, 0.02f, -0.5f), bc + V(0, 0.04f, -0.62f), bc + V(0, 0.0f, -0.72f), bc + V(0, 0.02f, -0.86f) },
                new[] { 0.03f, 0.026f, 0.022f }, 0.01f, glow, V(0, 1f, 0), mat: Glow);
        }
        int nb = b.Part("neck", Body, neckFrom, PokeRole.Head, 0.4f);
        b.Tube(nb, neckPts, 0.065f, 0.05f, violet, blend: 0f);
        for (int i = 1; i < neckPts.Length; i += 2)
            b.PaintEll(nb, neckPts[i] + V(0, 0, 0.045f), V(0.03f, 0.02f, 0.012f), lilac, soft: 0.006f);
        int tb = b.Tail(tail[0]);
        b.Tube(tb, tail, 0.06f, 0.025f, violet, blend: 0f);
        if (!riding)
            Lightning(b, tb, new[] { tail[^1], tail[^1] + V(0.06f, 0.04f, -0.06f), tail[^1] + V(0.04f, -0.02f, -0.12f), tail[^1] + V(0.12f, 0.0f, -0.2f) },
                new[] { 0.03f, 0.026f, 0.022f }, 0.01f, glow, V(0, 1f, 0), mat: Glow);
        // The helmeted head, a white visor and two bolts of yellow lightning
        int head = b.Head(neckPts[^1], nb);
        var r = V(0.085f, 0.07f, 0.13f);
        b.Ell(head, headC, r, dark);
        b.PaintEll(head, headC + V(0, -0.005f, 0.1f), V(0.07f, 0.035f, 0.05f), white, soft: 0.006f);
        b.Spike(head, headC + V(0, 0.02f, -0.06f), headC + V(0, 0.05f, -0.16f), 0.035f, dark, 0.6f, Metal);
        PokeBuilder.Both(s =>
        {
            var at = On(headC, r, headC.X + 0.04f * s, headC.Y - 0.002f);
            b.Eye(head, at, Outward(headC, r, at), 0.017f, Rgb(250, 210, 60), glare: true);
            int ear = b.Ear(head, s, headC + V(0.03f * s, 0.04f, -0.02f));
            var root = headC + V(0.03f * s, 0.04f, -0.02f);
            var corners = low || riding
                ? new[] { root, root + V(0.03f * s, 0.04f, -0.06f), root + V(0.02f * s, 0.05f, -0.11f), root + V(0.05f * s, 0.07f, -0.18f) }
                : new[] { root, root + V(0.03f * s, 0.08f, -0.02f), root + V(0.0f, 0.13f, -0.03f), root + V(0.05f * s, 0.24f, -0.06f) };
            Lightning(b, ear, corners, new[] { 0.024f, 0.02f, 0.018f }, 0.01f, glow, V(1f * s, 0, 0.3f), mat: Glow);
        });
        return shape == MiraidonShape.Aquatic ? Lift(b.Hover()) : b;
    }

    private static PokeBuilder Miraidon() => MiraidonBuild(MiraidonShape.Ultimate, "Miraidon");
    private static PokeBuilder MiraidonLowPower() => MiraidonBuild(MiraidonShape.LowPower, "Miraidon-Low-Power-Mode");
    private static PokeBuilder MiraidonDrive() => MiraidonBuild(MiraidonShape.Drive, "Miraidon-Drive-Mode");
    private static PokeBuilder MiraidonAquatic() => MiraidonBuild(MiraidonShape.Aquatic, "Miraidon-Aquatic-Mode");
    private static PokeBuilder MiraidonGlide() => MiraidonBuild(MiraidonShape.Glide, "Miraidon-Glide-Mode");

    // ------------------------------------------------------------------ Walking Wake

    /// <summary>
    /// Walking Wake: Suicune's ancient past, a teal dinosaur on clawed legs patterned with white diamonds, a great
    /// purple mane flowing like a cloak from its head over its back, orange spikes standing out of it, a dark teal crest
    /// framed over its brow, a pale jaw with fangs, and a long white tail trailing two ribbons.
    /// </summary>
    private static PokeBuilder WalkingWake()
    {
        var b = new PokeBuilder("Walking Wake", 1f, BodyPlan.Quadruped, V(0, 0.44f, 0)) { Coat = Scales };
        var teal = Rgb(70, 168, 188);
        var deep = Rgb(36, 96, 112);
        var purple = Rgb(146, 92, 164);
        var white = Rgb(240, 244, 248);
        var orange = Rgb(238, 120, 62);
        foreach (var (z, front, thick) in new[] { (0.16f, true, 0.045f), (-0.1f, false, 0.065f) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.1f * s, 0.4f, z);
                int leg = b.Leg(s, hip, front);
                var knee = V(0.15f * s, front ? 0.24f : 0.26f, z + (front ? 0.04f : 0.07f));
                var ankle = V(0.14f * s, 0.06f, z - 0.02f);
                b.Limb(leg, hip, knee, thick, thick * 0.8f, teal);
                b.Limb(leg, knee, ankle, thick * 0.75f, thick * 0.6f, teal);
                b.Ell(leg, ankle + V(0, -0.025f, 0.03f), V(thick * 0.9f, 0.026f, thick * 1.3f), teal);
                Claws(b, leg, ankle + V(0, -0.03f, thick * 1.2f + 0.02f), V(0.016f * s, 0, 0), V(0, -0.3f, 1f), 0.04f, 0.011f);
                b.PaintEll(leg, Vector3.Lerp(hip, knee, 0.5f) + V(0.04f * s, 0, 0), V(0.012f, 0.03f, 0.012f), white, V(0, 0, 45f), 0.005f);
            });
        var bc = V(0, 0.44f, 0.02f);
        b.Ell(Body, bc, V(0.14f, 0.14f, 0.21f), teal);
        b.PaintEll(Body, bc + V(0, -0.08f, 0.04f), V(0.11f, 0.06f, 0.16f), white, soft: 0.012f);
        // The purple mane flowing back over it like a cloak, orange spikes standing out of it
        b.Ell(Body, bc + V(0, 0.11f, 0.02f), V(0.16f, 0.11f, 0.2f), purple, blend: 0.02f);
        for (int i = 0; i < 8; i++)
        {
            float a = (-140f + 40f * i) * Degree;
            var n = V(MathF.Sin(a) * 0.9f, 0, -MathF.Cos(a) * 0.3f - 0.7f);
            var root = bc + V(MathF.Sin(a) * 0.12f, 0.08f, -0.12f);
            Blade(b, Body, root, root + Vector3.Normalize(n) * 0.16f + V(0, -0.12f, 0), 0.05f, purple, V(0, 1f, 0.2f), 0.25f, Fur);
        }
        foreach (var (x, z, h) in new[] { (0f, 0.1f, 0.16f), (-0.07f, 0.04f, 0.13f), (0.07f, 0.04f, 0.13f), (-0.05f, -0.06f, 0.11f), (0.05f, -0.06f, 0.11f) })
            b.Spike(Body, bc + V(x, 0.17f, z), bc + V(x * 1.4f, 0.17f + h, z - 0.06f), 0.026f, orange, 0.5f, Shell);
        // Its head low at its front: a pale jaw with fangs, a dark crest framed over its brow
        int head = b.Head(bc + V(0, 0.1f, 0.18f));
        var c = bc + V(0, 0.15f, 0.29f);
        var r = V(0.07f, 0.06f, 0.09f);
        b.Limb(head, bc + V(0, 0.08f, 0.14f), c + V(0, -0.02f, -0.05f), 0.07f, 0.055f, teal);
        b.Ell(head, c, r, teal);
        b.Ell(head, c + V(0, -0.035f, 0.05f), V(0.055f, 0.03f, 0.06f), white);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.03f * s, -0.04f, 0.1f), c + V(0.034f * s, -0.075f, 0.1f), 0.008f, white, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.035f * s, c.Y + 0.015f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(244, 160, 48), glare: true);
            b.Limb(head, c + V(0.04f * s, 0.04f, 0.0f), c + V(0.06f * s, 0.16f, -0.05f), 0.014f, 0.012f, deep, Shell);
            b.Limb(head, c + V(0.06f * s, 0.16f, -0.05f), c + V(0.02f * s, 0.21f, -0.1f), 0.012f, 0.011f, deep, Shell);
        });
        b.Limb(head, c + V(-0.02f, 0.21f, -0.1f), c + V(0.02f, 0.21f, -0.1f), 0.012f, 0.012f, deep, Shell);
        b.Spike(head, c + V(0, 0.05f, 0.02f), c + V(0, 0.09f, 0.1f), 0.02f, deep, 0.5f, Shell);
        // The long white tail, two ribbons trailing from it
        int tail = b.Tail(bc + V(0, 0.02f, -0.2f));
        var tp = Smooth(3, bc + V(0, 0.02f, -0.19f), bc + V(0, -0.06f, -0.38f), bc + V(0.06f, -0.16f, -0.55f), bc + V(0.14f, -0.2f, -0.66f));
        b.Tube(tail, tp, 0.06f, 0.02f, white, blend: 0f);
        for (int i = 1; i < tp.Length - 1; i += 2)
            b.PaintEll(tail, tp[i] + V(0, 0.04f, 0), V(0.02f, 0.012f, 0.02f), teal, V(0, 0, 45f), 0.005f);
        PokeBuilder.Both(s => Ribbon(b, tail, new[] { tp[^2], tp[^1] + V(0.06f * s, 0.06f, -0.08f), tp[^1] + V(0.1f * s, 0.02f, -0.2f) }, 0.03f, white, V(0, 1f, 0), 0.008f));
        return b;
    }

    // ------------------------------------------------------------------ Iron Leaves, Iron Crown

    /// <summary>The four slender legs of a machine of a deer (Iron Leaves, Iron Crown): each a long shank jointed with a ring, on a dark hoof.</summary>
    private static void RoboDeerLegs(PokeBuilder b, float x, float hip, float front, float back, float r, Color coat, Color ring, Color hoof)
    {
        foreach (var (z, isFront) in new[] { (front, true), (back, false) })
            PokeBuilder.Both(s =>
            {
                var top = V(x * s, hip, z);
                int leg = b.Leg(s, top, isFront);
                var knee = V(x * 1.1f * s, hip * 0.5f, z + (isFront ? 0.02f : -0.04f));
                var ankle = V(x * 1.1f * s, 0.06f, z);
                b.Limb(leg, top, knee, r * 1.3f, r, coat);
                b.Torus(leg, knee, r * 1.05f, r * 0.4f, ring, mat: Glow, blend: 0.004f);
                b.Limb(leg, knee, ankle, r, r * 0.85f, coat);
                b.Ell(leg, ankle + V(0, -0.025f, 0.005f), V(r * 1.1f, 0.03f, r * 1.3f), hoof, mat: Shell);
            });
    }

    /// <summary>
    /// Iron Leaves: Virizion's future, a slender green machine of a deer on long legs ringed pink at the knee, grey
    /// plates on its chest, leaf blades swept back from its shoulders and hips, and on its head a great blade of a horn
    /// sweeping forward over its red eyes, its underside pink and dotted white, with pink spotted leaves at its sides.
    /// </summary>
    private static PokeBuilder IronLeaves()
    {
        var b = new PokeBuilder("Iron Leaves", 0.95f, BodyPlan.Quadruped, V(0, 0.5f, 0)) { Coat = Metal };
        var green = Rgb(72, 170, 84);
        var dark = Rgb(40, 110, 56);
        var grey = Rgb(172, 178, 184);
        var pink = Rgb(236, 92, 120);
        RoboDeerLegs(b, 0.08f, 0.46f, 0.14f, -0.14f, 0.029f, green, pink, Rgb(40, 40, 44));
        var bc = V(0, 0.52f, 0);
        b.Ell(Body, bc, V(0.11f, 0.105f, 0.2f), green);
        b.PaintEll(Body, bc + V(0, -0.04f, 0.12f), V(0.08f, 0.07f, 0.09f), grey, soft: 0.01f);
        PokeBuilder.Both(s =>
        {
            Blade(b, Body, bc + V(0.08f * s, 0.04f, 0.1f), bc + V(0.16f * s, 0.2f, -0.04f), 0.045f, dark, V(1f * s, 0, 0), 0.25f, Metal);
            Blade(b, Body, bc + V(0.08f * s, 0.04f, -0.1f), bc + V(0.15f * s, 0.17f, -0.27f), 0.04f, dark, V(1f * s, 0, 0), 0.25f, Metal);
        });
        // A neck of green segments ringed grey
        var neckTop = bc + V(0, 0.27f, 0.2f);
        int neck = b.Part("neck", Body, bc + V(0, 0.05f, 0.16f), PokeRole.Head, 0.4f);
        var np = Smooth(3, bc + V(0, 0.05f, 0.16f), bc + V(0, 0.16f, 0.2f), neckTop);
        b.Tube(neck, np, 0.048f, 0.04f, green, blend: 0f);
        for (int i = 1; i < np.Length; i += 2)
            b.Torus(neck, np[i], 0.036f, 0.008f, grey, Euler(np[Math.Min(np.Length - 1, i + 1)] - np[i - 1]), mat: Metal, blend: 0.003f);
        int head = b.Head(neckTop, neck);
        var c = neckTop + V(0, 0.04f, 0.03f);
        var r = V(0.06f, 0.058f, 0.075f);
        b.Ell(head, c, r, green);
        b.PaintEll(head, c + V(0, -0.01f, 0.06f), V(0.045f, 0.025f, 0.035f), grey, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.022f * s, c.Y - 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(232, 50, 60), glare: true);
            var leaf = c + V(0.035f * s, 0.025f, -0.01f);
            Frond(b, head, leaf, leaf + V(0.1f * s, 0.05f, -0.03f), 0.03f, pink, V(0, 1f, 0.3f), 0.25f, Metal, 0.008f);
            foreach (float t in new[] { 0.4f, 0.6f, 0.8f })
                b.PaintEll(head, Vector3.Lerp(leaf, leaf + V(0.1f * s, 0.05f, -0.03f), t) + V(0, 0.008f, 0), V(0.006f, 0.006f, 0.006f), White, soft: 0.003f);
        });
        // The great horn blade sweeping forward over its brow, pink and dotted beneath
        var hornRoot = c + V(0, 0.04f, -0.02f);
        var hornTip = c + V(0, 0.2f, 0.3f);
        Blade(b, head, hornRoot, hornTip, 0.058f, green, V(1f, 0, 0), 0.3f, Metal);
        b.Spike(head, hornRoot, c + V(0, 0.12f, -0.12f), 0.03f, green, 0.4f, Metal);
        for (int i = 0; i < 4; i++)
            b.PaintEll(head, Vector3.Lerp(hornRoot, hornTip, 0.25f + 0.17f * i) + V(0, -0.012f, 0), V(0.02f, 0.008f, 0.016f), pink, soft: 0.004f);
        int tail = b.Tail(bc + V(0, 0.03f, -0.17f));
        Blade(b, tail, bc + V(0, 0.03f, -0.16f), bc + V(0, 0.12f, -0.3f), 0.03f, dark, V(1f, 0, 0), 0.3f, Metal);
        return b;
    }

    /// <summary>
    /// Iron Crown: Cobalion's future, a slender teal machine of a deer on legs ringed with glowing teal, a grey chest,
    /// gold fins swept back from its shoulders, and a tall crown of a horn rising from its head, two strands of gold
    /// and teal twisted round each other and forked at the top.
    /// </summary>
    private static PokeBuilder IronCrown()
    {
        var b = new PokeBuilder("Iron Crown", 0.95f, BodyPlan.Quadruped, V(0, 0.5f, 0)) { Coat = Metal };
        var teal = Rgb(56, 156, 176);
        var deep = Rgb(36, 104, 124);
        var grey = Rgb(178, 186, 192);
        var gold = Rgb(226, 184, 70);
        var glow = Rgb(130, 236, 226);
        RoboDeerLegs(b, 0.08f, 0.46f, 0.13f, -0.15f, 0.03f, teal, glow, Rgb(110, 116, 124));
        var bc = V(0, 0.52f, 0);
        b.Ell(Body, bc, V(0.11f, 0.11f, 0.2f), teal);
        b.Ell(Body, bc + V(0, 0.0f, 0.13f), V(0.095f, 0.11f, 0.09f), grey);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 4; i++)
                Blade(b, Body, bc + V(0.08f * s, 0.04f - 0.03f * i, 0.07f - 0.03f * i), bc + V(0.22f * s, 0.15f - 0.05f * i, -0.13f - 0.04f * i), 0.036f, gold, V(0, 1f, 0), 0.25f, Metal);
        });
        var neckTop = bc + V(0, 0.26f, 0.19f);
        int neck = b.Part("neck", Body, bc + V(0, 0.06f, 0.15f), PokeRole.Head, 0.4f);
        var np = Smooth(3, bc + V(0, 0.06f, 0.15f), bc + V(0, 0.16f, 0.19f), neckTop);
        b.Tube(neck, np, 0.05f, 0.042f, teal, blend: 0f);
        b.PaintEll(neck, np[2] + V(0, 0, 0.035f), V(0.025f, 0.08f, 0.015f), grey, soft: 0.006f);
        int head = b.Head(neckTop, neck);
        var c = neckTop + V(0, 0.04f, 0.04f);
        var r = V(0.058f, 0.055f, 0.08f);
        b.Ell(head, c, r, teal);
        b.PaintEll(head, c + V(0, -0.01f, 0.055f), V(0.04f, 0.02f, 0.03f), grey, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.024f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(70, 220, 230), glare: true);
            Blade(b, head, c + V(0.04f * s, 0.02f, -0.02f), c + V(0.08f * s, 0.05f, -0.1f), 0.022f, deep, V(1f * s, 0.3f, 0), 0.3f, Metal);
        });
        // The crown: two strands twisted round each other, forked at the top
        var crownRoot = c + V(0, 0.04f, -0.01f);
        foreach (var (phase, color) in new[] { (0f, gold), (MathF.PI, teal) })
        {
            var pts = new Vector3[9];
            for (int i = 0; i < pts.Length; i++)
            {
                float t = i / (pts.Length - 1f), a = phase + t * MathF.Tau * 1.25f;
                pts[i] = crownRoot + V(MathF.Cos(a) * 0.02f, t * 0.24f, MathF.Sin(a) * 0.02f - t * 0.05f);
            }
            b.Tube(head, pts, 0.014f, 0.01f, color, Metal, 0f);
        }
        var crownTop = crownRoot + V(0, 0.24f, -0.05f);
        PokeBuilder.Both(s => b.Spike(head, crownTop, crownTop + V(0.06f * s, 0.06f, -0.01f), 0.012f, gold, mat: Metal, blend: 0.004f));
        int tail = b.Tail(bc + V(0, 0.03f, -0.17f));
        Blade(b, tail, bc + V(0, 0.03f, -0.16f), bc + V(0, 0.1f, -0.3f), 0.03f, gold, V(1f, 0, 0), 0.3f, Metal);
        return b;
    }

    // ------------------------------------------------------------------ Dipplin, Hydrapple

    private static readonly Color CandyRed = Rgb(214, 40, 42);
    private static readonly Color CandyDeep = Rgb(162, 24, 32);

    /// <summary>A candy-coated apple at <paramref name="c"/>: red and glossy, its coat dripping down its sides into a pool round its foot.</summary>
    private static void CandyApple(PokeBuilder b, Vector3 c, Vector3 r)
    {
        b.Ell(Body, c, r, CandyRed, mat: Glow);
        b.PaintEll(Body, c + V(-r.X * 0.4f, r.Y * 0.5f, r.Z * 0.6f), V(r.X * 0.25f, r.Y * 0.18f, r.Z * 0.2f), Rgb(246, 140, 140), soft: 0.01f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f + 0.3f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            float len = 0.35f + 0.25f * ((i * 7) % 3) / 2f;
            var at = c + V(n.X * r.X * 0.97f, -r.Y * 0.2f - len * r.Y * 0.5f, n.Z * r.Z * 0.97f);
            b.Ell(Body, at, V(r.X * 0.13f, r.Y * len * 0.6f, r.Z * 0.13f), CandyRed, mat: Glow, blend: 0.012f);
        }
        b.Ell(Body, V(c.X, r.Y * 0.12f, c.Z), V(r.X * 1.22f, r.Y * 0.14f, r.Z * 1.18f), CandyDeep, mat: Glow, blend: 0.02f);
        b.PaintTorus(Body, c + V(0, -r.Y * 0.1f, 0), MathF.Max(r.X, r.Z) * 0.98f, r.Y * 0.08f, CandyDeep, sx: 1f, sz: r.Z / r.X);
    }

    /// <summary>
    /// Dipplin: a candy apple coated thick and glossy red, dripping into a pool at its foot, a green tail with two
    /// leaves peeping out of its side, and on its top the little dragon's face: two yellow leaves with its eyes in them
    /// either side of a long green stem.
    /// </summary>
    private static PokeBuilder Dipplin()
    {
        var b = new PokeBuilder("Dipplin", 0.5f, BodyPlan.Floating, V(0, 0.13f, 0)) { Coat = Leaf };
        var green = Rgb(122, 168, 92);
        var yellow = Rgb(244, 230, 140);
        var c = V(0, 0.14f, 0);
        var r = V(0.12f, 0.11f, 0.115f);
        CandyApple(b, c, r);
        int head = b.Head(c + V(0, 0.08f, 0));
        b.Ell(head, c + V(0, 0.1f, 0), V(0.04f, 0.025f, 0.04f), Rgb(110, 150, 82));
        b.Tube(head, Smooth(3, c + V(0, 0.1f, 0), c + V(0.01f, 0.22f, -0.01f), c + V(0.03f, 0.36f, 0.0f)), 0.01f, 0.007f, Rgb(150, 182, 110), Leaf, 0f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.02f * s, 0.11f, 0.01f));
            var from = c + V(0.02f * s, 0.11f, 0.02f);
            var to = c + V(0.08f * s, 0.2f, 0.04f);
            Frond(b, ear, from, to, 0.032f, yellow, V(0.2f * s, 0, 1f), 0.3f, Leaf, 0.008f);
            b.Eye(ear, Vector3.Lerp(from, to, 0.55f) + V(0, 0, 0.012f), V(0.15f * s, 0, 1f), 0.01f, Rgb(40, 40, 30), glare: true);
        });
        int tail = b.Tail(c + V(-0.1f, -0.06f, -0.04f));
        var tp = Smooth(3, c + V(-0.09f, -0.07f, -0.03f), c + V(-0.17f, -0.1f, -0.03f), c + V(-0.24f, -0.07f, -0.02f));
        b.Tube(tail, tp, 0.022f, 0.012f, green, blend: 0f);
        Petal(b, tail, tp[^1], tp[^1] + V(-0.03f, 0.05f, 0.01f), 0.014f, Rgb(84, 130, 70), Leaf);
        Petal(b, tail, tp[2], tp[2] + V(-0.01f, 0.05f, 0.02f), 0.013f, Rgb(84, 130, 70), Leaf);
        return b;
    }

    /// <summary>
    /// Hydrapple: a great candy apple dripping into a pool, and rising from its top a long green dragon of a neck
    /// winding up to a head with a pale belly down its throat, a forked tongue and a little red fruit on a twig on its
    /// crown; a green tail with leaves curling out of its foot.
    /// </summary>
    private static PokeBuilder Hydrapple()
    {
        var b = new PokeBuilder("Hydrapple", 1f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Scales };
        var green = Rgb(112, 152, 76);
        var pale = Rgb(206, 214, 140);
        var c = V(0, 0.22f, 0);
        var r = V(0.22f, 0.19f, 0.21f);
        CandyApple(b, c, r);
        int neck = b.Part("neck", Body, c + V(0, 0.17f, 0), PokeRole.Head, 0.4f);
        var np = Smooth(3, c + V(0, 0.15f, 0), c + V(-0.06f, 0.32f, 0.02f), c + V(0.05f, 0.48f, 0.0f), c + V(-0.02f, 0.62f, 0.04f), c + V(0, 0.7f, 0.1f));
        b.Tube(neck, np, 0.06f, 0.045f, green, blend: 0f);
        for (int i = 1; i < np.Length - 1; i++)
        {
            var d = Vector3.Normalize(np[i + 1] - np[i - 1]);
            var front = Vector3.Normalize(Vector3.Cross(d, V(1f, 0, 0)));
            if (front.Z < 0) front = -front;
            b.PaintEll(neck, np[i] + front * 0.045f, V(0.03f, 0.03f, 0.02f), pale, Euler(d), 0.008f);
        }
        int head = b.Head(np[^1], neck);
        var hc = np[^1] + V(0, 0.03f, 0.05f);
        var hr = V(0.06f, 0.045f, 0.08f);
        b.Ell(head, hc, hr, green);
        b.PaintEll(head, hc + V(0, -0.03f, 0.03f), V(0.045f, 0.02f, 0.06f), pale, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, hc.X + 0.03f * s, hc.Y + 0.012f);
            b.Eye(head, at, Outward(hc, hr, at), 0.012f, Rgb(232, 80, 50), glare: true);
        });
        var mouth = hc + V(0, -0.015f, 0.075f);
        b.Tube(head, new[] { mouth, mouth + V(0, -0.03f, 0.04f), mouth + V(0, -0.06f, 0.05f) }, 0.008f, 0.006f, Rgb(214, 70, 80), blend: 0f);
        PokeBuilder.Both(s => b.Spike(head, mouth + V(0, -0.06f, 0.05f), mouth + V(0.015f * s, -0.08f, 0.06f), 0.005f, Rgb(214, 70, 80), blend: 0.002f));
        var twig = hc + V(0, 0.04f, -0.02f);
        b.Limb(head, twig, twig + V(0.05f, 0.04f, 0.0f), 0.008f, 0.006f, Rgb(120, 86, 60));
        b.Ell(head, twig + V(0.06f, 0.05f, 0), V(0.02f, 0.02f, 0.02f), Rgb(220, 60, 40), mat: Glow, blend: 0.004f);
        int tail = b.Tail(c + V(0.18f, -0.14f, -0.06f));
        var tp = Smooth(3, c + V(0.18f, -0.15f, -0.06f), c + V(0.32f, -0.18f, -0.06f), c + V(0.42f, -0.15f, 0.0f));
        b.Tube(tail, tp, 0.04f, 0.02f, green, blend: 0f);
        Petal(b, tail, tp[^1], tp[^1] + V(0.04f, 0.07f, 0.0f), 0.022f, Rgb(84, 130, 70), Leaf);
        Petal(b, tail, tp[2], tp[2] + V(0.0f, 0.07f, 0.02f), 0.02f, Rgb(84, 130, 70), Leaf);
        return b;
    }

    // ------------------------------------------------------------------ Poltchageist, Sinistcha

    private static readonly Color Matcha = Rgb(110, 160, 88);

    /// <summary>
    /// Poltchageist: a little tea caddy haunted by matcha, white glazed and dark at its shoulder, its cream lid tipped
    /// and green spilling from under it, swirls painted on its front for eyes, three dots by them, and a green hand
    /// holding up a bamboo tea scoop. The Artisan's mark is under its foot, so it looks just the same.
    /// </summary>
    private static PokeBuilder Poltchageist()
    {
        var b = new PokeBuilder("Poltchageist", 0.4f, BodyPlan.Floating, V(0, 0.13f, 0)) { Coat = Shell }.Hover();
        var glaze = Rgb(236, 234, 224);
        var black = Rgb(40, 38, 40);
        var cream = Rgb(244, 236, 214);
        var c = V(0, 0.13f, 0);
        var r = V(0.07f, 0.09f, 0.065f);
        b.Ell(Body, c, r, glaze);
        b.PaintEll(Body, c + V(0.03f, 0.06f, -0.02f), V(0.07f, 0.05f, 0.07f), black, soft: 0.01f);
        b.PaintEll(Body, c + V(-0.05f, 0.03f, -0.03f), V(0.03f, 0.06f, 0.05f), black, soft: 0.01f);
        foreach (var (x, y) in new[] { (0.035f, 0.045f), (0.05f, 0.03f), (0.042f, 0.015f) })
        {
            var at = On(c, r, c.X + x, c.Y + y);
            b.Mark(Body, at, Outward(c, r, at), 0.006f, 0.006f, black);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.026f * s, c.Y - 0.015f);
            b.Mark(Body, at, Outward(c, r, at), 0.014f, 0.016f, black, MarkShape.Ring);
        });
        var smile = On(c, r, c.X, c.Y - 0.045f);
        b.Mark(Body, smile, Outward(c, r, smile), 0.014f, 0.007f, black, MarkShape.Smile);
        // The lid tipped on its top, matcha spilling from under it
        int head = b.Head(c + V(0, 0.08f, 0));
        var lid = c + V(-0.005f, 0.09f, 0.0f);
        b.Ell(head, lid, V(0.055f, 0.016f, 0.05f), cream, V(0, 0, 8f));
        b.Ell(head, lid + V(0, 0.022f, 0), V(0.024f, 0.016f, 0.022f), cream);
        foreach (var (x, z, len) in new[] { (0.05f, 0.01f, 0.05f), (-0.03f, 0.04f, 0.03f), (0.02f, -0.045f, 0.04f), (-0.05f, -0.02f, 0.02f) })
            b.Ell(head, lid + V(x, -0.01f - len * 0.4f, z), V(0.012f, len * 0.5f, 0.012f), Matcha, blend: 0.01f);
        // A green hand holding up a bamboo scoop
        int arm = b.Arm(1f, c + V(0.06f, 0.04f, 0));
        b.Ell(arm, c + V(0.08f, 0.06f, 0.0f), V(0.018f, 0.018f, 0.018f), Matcha, blend: 0.01f);
        b.Limb(arm, c + V(0.04f, 0.02f, 0.0f), c + V(0.15f, 0.16f, -0.01f), 0.008f, 0.009f, Rgb(224, 210, 150), Shell);
        return Lift(b);
    }

    /// <summary>
    /// Sinistcha: a dark glazed tea bowl with a cream panel painted with swirls, and in it a ghost of matcha, its
    /// green arms over the rim, swirled eyes in its face and a bamboo tea whisk worn on its head like a hat, its tines
    /// flaring down round it and its handle bound in black. The Masterpiece's mark is under its foot.
    /// </summary>
    private static PokeBuilder Sinistcha()
    {
        var b = new PokeBuilder("Sinistcha", 0.5f, BodyPlan.Floating, V(0, 0.14f, 0)) { Coat = Shell }.Hover();
        var glaze = Rgb(66, 48, 40);
        var rim = Rgb(120, 76, 50);
        var cream = Rgb(240, 232, 212);
        var bamboo = Rgb(224, 210, 146);
        var c = V(0, 0.12f, 0);
        var r = V(0.125f, 0.1f, 0.12f);
        b.Ell(Body, c, r, glaze);
        b.Cut(Body, c + V(0, 0.09f, 0), V(0.11f, 0.09f, 0.105f));
        b.Torus(Body, c + V(0, 0.058f, 0), 0.105f, 0.01f, rim, sz: 0.96f, mat: Shell, blend: 0.004f);
        b.Torus(Body, c + V(0, -0.095f, 0), 0.05f, 0.012f, glaze, mat: Shell, blend: 0.006f);
        b.PaintEll(Body, c + V(0.01f, -0.015f, 0.1f), V(0.085f, 0.06f, 0.05f), cream, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.032f * s, c.Y - 0.01f);
            b.Mark(Body, at, Outward(c, r, at), 0.018f, 0.02f, Rgb(40, 38, 40), MarkShape.Ring);
        });
        // The ghost of matcha in the bowl
        int head = b.Head(c + V(0, 0.06f, 0));
        b.Ell(head, c + V(0, 0.03f, 0), V(0.105f, 0.045f, 0.1f), Matcha, blend: 0.01f);
        var hc = c + V(-0.01f, 0.13f, 0.01f);
        var hr = V(0.048f, 0.058f, 0.045f);
        b.Ell(head, hc, hr, Matcha);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, hc.X + 0.02f * s, hc.Y - 0.002f);
            b.Mark(head, at, Outward(hc, hr, at), 0.01f, 0.011f, Rgb(40, 60, 36), MarkShape.Ring);
            int arm = b.Arm(s, c + V(0.06f * s, 0.06f, 0.02f));
            b.Tube(arm, Smooth(2, c + V(0.05f * s, 0.06f, 0.03f), c + V(0.1f * s, 0.075f, 0.05f), c + V(0.115f * s, 0.04f, 0.06f)), 0.008f, 0.007f, Matcha, blend: 0f);
        });
        // The whisk worn on its head: tines flaring round it, the handle bound in black
        var top = hc + V(0, 0.07f, 0);
        b.Limb(head, top - V(0, 0.01f, 0), top + V(0, 0.09f, 0), 0.016f, 0.015f, bamboo, Shell);
        b.Torus(head, top + V(0, 0.015f, 0), 0.017f, 0.004f, Rgb(36, 34, 36), blend: 0.002f);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Tube(head, Smooth(2, top + n * 0.008f, top + n * 0.045f + V(0, -0.02f, 0), top + n * 0.058f + V(0, -0.06f, 0)), 0.0055f, 0.0045f, bamboo, Shell, 0f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Okidogi, Munkidori, Fezandipiti

    private static readonly Color ToxicChain = Rgb(176, 66, 148);

    /// <summary>A chain of the Loyal Three's toxic links along <paramref name="points"/>, each link turned a quarter from the last.</summary>
    private static void ChainLinks(PokeBuilder b, int bone, Vector3[] points, float ring, float tube)
    {
        for (int i = 0; i < points.Length; i++)
        {
            var d = Vector3.Normalize(points[Math.Min(points.Length - 1, i + 1)] - points[Math.Max(0, i - 1)]);
            var side = Vector3.Normalize(Vector3.Cross(d, MathF.Abs(d.Y) > 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
            var axis = i % 2 == 0 ? side : Vector3.Cross(d, side);
            b.Torus(bone, points[i], ring, tube, ToxicChain, Euler(axis), 1f, 1.4f, Shell, 0.004f);
        }
    }

    /// <summary>
    /// Okidogi: a hulking dog of a brawler, black and shaggy above with a green chest, great green arms and short green
    /// legs, black pointed ears, a green face with a toothy grin, a pink nose, a purple swirl over one yellow eye, and a
    /// toxic chain round its neck trailing up over its shoulder.
    /// </summary>
    private static PokeBuilder Okidogi()
    {
        var b = new PokeBuilder("Okidogi", 0.95f, BodyPlan.Biped, V(0, 0.48f, 0)) { Coat = Fur };
        var black = Rgb(40, 40, 46);
        var green = Rgb(98, 156, 62);
        var deep = Rgb(66, 116, 44);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.32f, 0));
            b.Limb(leg, V(0.09f * s, 0.32f, 0), V(0.11f * s, 0.12f, 0.02f), 0.065f, 0.055f, green);
            b.Limb(leg, V(0.11f * s, 0.12f, 0.02f), V(0.11f * s, 0.05f, 0.02f), 0.05f, 0.045f, black);
            b.Ell(leg, V(0.11f * s, 0.035f, 0.05f), V(0.055f, 0.035f, 0.075f), green);
        });
        var bc = V(0, 0.5f, 0);
        var br = V(0.17f, 0.2f, 0.14f);
        b.Ell(Body, bc, br, black);
        Shag(b, Body, bc, br, bc.Y - 0.14f, 14, 0.09f, 0.04f, black, 2f);
        var chest = bc + V(0, 0.06f, 0.08f);
        b.Ell(Body, chest, V(0.12f, 0.12f, 0.08f), green, blend: 0.015f);
        b.PaintEll(Body, chest + V(0, -0.05f, 0.07f), V(0.035f, 0.05f, 0.03f), black, V(0, 0, 0), 0.006f);
        FurTufts(b, Body, bc + V(0, 0.1f, -0.02f), V(0.17f, 0.12f, 0.12f), 18, 0.07f, 0.035f, black, 0.6f, -0.2f, 0.2f);
        // Great green arms, the right reaching out with its hand open
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.16f * s, 0.12f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var elbow = s > 0 ? shoulder + V(0.12f, -0.06f, 0.08f) : shoulder + V(-0.06f, -0.16f, 0.04f);
            var hand = s > 0 ? elbow + V(0.1f, 0.06f, 0.08f) : elbow + V(-0.02f, -0.14f, 0.04f);
            b.Ell(arm, shoulder, V(0.07f, 0.07f, 0.07f), black, blend: 0.02f);
            b.Limb(arm, shoulder, elbow, 0.065f, 0.06f, green);
            b.Limb(arm, elbow, hand, 0.07f, 0.06f, green);
            b.Ell(arm, hand + Vector3.Normalize(hand - elbow) * 0.03f, V(0.06f, 0.055f, 0.055f), green);
            b.PaintEll(arm, hand + Vector3.Normalize(hand - elbow) * 0.03f + V(0, 0, 0.05f), V(0.03f, 0.03f, 0.02f), ToxicChain, soft: 0.006f);
        });
        // The head: black ears, a green face with a toothy grin
        int head = b.Head(bc + V(0, 0.2f, 0.04f));
        var c = bc + V(0, 0.3f, 0.07f);
        var r = V(0.085f, 0.08f, 0.08f);
        b.Ell(head, c, r, black);
        b.Ell(head, c + V(0, -0.02f, 0.05f), V(0.07f, 0.06f, 0.05f), green, blend: 0.015f);
        b.Ell(head, c + V(0, -0.03f, 0.1f), V(0.03f, 0.024f, 0.02f), Rgb(200, 90, 140), blend: 0.006f);
        Grin(b, head, c + V(0, -0.06f, 0.085f), V(0.05f, 0.016f, 0.02f), Rgb(60, 30, 40));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.06f, -0.01f));
            b.Spike(ear, c + V(0.05f * s, 0.05f, -0.01f), c + V(0.09f * s, 0.15f, -0.02f), 0.035f, black, 0.6f);
            var at = On(c + V(0, -0.02f, 0.05f), V(0.07f, 0.06f, 0.05f), c.X + 0.032f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c + V(0, -0.02f, 0.05f), V(0.07f, 0.06f, 0.05f), at), 0.011f, Rgb(244, 210, 60), glare: true);
        });
        b.PaintEll(head, c + V(-0.035f, 0.03f, 0.08f), V(0.03f, 0.022f, 0.02f), ToxicChain, soft: 0.006f);
        // The toxic chain round its neck, trailing up over its shoulder
        var links = new List<Vector3>();
        for (int i = 0; i < 6; i++)
        {
            float a = (150f + i * 60f) * Degree;
            links.Add(bc + V(MathF.Sin(a) * 0.09f, 0.21f, MathF.Cos(a) * 0.085f + 0.03f));
        }
        links.AddRange(new[] { bc + V(0.13f, 0.25f, 0.0f), bc + V(0.165f, 0.29f, -0.025f), bc + V(0.2f, 0.33f, -0.045f), bc + V(0.235f, 0.37f, -0.055f) });
        ChainLinks(b, Body, links.ToArray(), 0.03f, 0.011f);
        return b;
    }

    /// <summary>
    /// Munkidori: a monkey in a black hood and cloak that falls to its feet in tatters, a tuft standing on its crown,
    /// its pale blue face with heavy-lidded yellow eyes, one blue hand at its chin, blue feet ringed purple, and a toxic
    /// chain wound round its head like a crown.
    /// </summary>
    private static PokeBuilder Munkidori()
    {
        var b = new PokeBuilder("Munkidori", 0.75f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var black = Rgb(40, 40, 50);
        var blue = Rgb(140, 168, 230);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.12f, 0));
            b.Limb(leg, V(0.04f * s, 0.14f, 0), V(0.05f * s, 0.05f, 0.01f), 0.022f, 0.02f, blue);
            b.Torus(leg, V(0.05f * s, 0.06f, 0.01f), 0.022f, 0.008f, Rgb(140, 70, 150), blend: 0.003f);
            b.Ell(leg, V(0.055f * s, 0.025f, 0.03f), V(0.03f, 0.025f, 0.045f), blue);
        });
        var bc = V(0, 0.36f, 0);
        b.Ell(Body, bc, V(0.1f, 0.24f, 0.085f), black);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Spike(Body, bc + V(n.X * 0.075f, -0.18f, n.Z * 0.06f), bc + V(n.X * 0.1f, -0.27f, n.Z * 0.08f), 0.03f, black, 0.5f, blend: 0.012f);
        }
        // Its arm up at its chin
        int arm = b.Arm(1f, bc + V(0.08f, 0.1f, 0.03f));
        b.Limb(arm, bc + V(0.08f, 0.1f, 0.03f), bc + V(0.08f, 0.0f, 0.1f), 0.022f, 0.02f, blue);
        b.Limb(arm, bc + V(0.08f, 0.0f, 0.1f), bc + V(0.03f, 0.16f, 0.12f), 0.02f, 0.018f, blue);
        Digits(b, arm, bc + V(0.03f, 0.16f, 0.12f), V(-0.3f, 0.6f, 0.4f), V(0.3f, 0, 0) * 0.4f, 0.03f, 0.007f, blue);
        // The hood and the pale blue face
        int head = b.Head(bc + V(0, 0.18f, 0));
        var c = bc + V(0, 0.27f, 0.02f);
        b.Ell(head, c, V(0.1f, 0.1f, 0.085f), black);
        foreach (var (x, h) in new[] { (-0.02f, 0.08f), (0.01f, 0.1f), (0.035f, 0.07f) })
            b.Spike(head, c + V(x, 0.07f, -0.01f), c + V(x * 1.5f, 0.07f + h, -0.02f), 0.025f, black, 0.5f);
        var fc = c + V(0, -0.02f, 0.07f);
        var fr = V(0.078f, 0.072f, 0.048f);
        b.Ell(head, fc, fr, blue, blend: 0.012f);
        b.Ell(head, fc + V(0, -0.03f, 0.03f), V(0.035f, 0.025f, 0.02f), Rgb(160, 186, 238), blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, fc.X + 0.03f * s, fc.Y + 0.014f);
            b.Eye(head, at, Outward(fc, fr, at), 0.016f, Rgb(244, 218, 60), glare: true);
        });
        // The chain wound round its head like a crown
        var links = new List<Vector3>();
        for (int i = 0; i < 8; i++)
        {
            float a = (i * 45f + 20f) * Degree;
            links.Add(c + V(MathF.Sin(a) * 0.1f, 0.05f, MathF.Cos(a) * 0.085f));
        }
        links.AddRange(new[] { c + V(0.12f, 0.02f, 0.07f), c + V(0.15f, -0.03f, 0.08f) });
        ChainLinks(b, head, links.ToArray(), 0.026f, 0.009f);
        return b;
    }

    /// <summary>
    /// Fezandipiti: a pheasant with a long white neck, its black body and great brown wings, a black cap with two
    /// curved yellow horns, a red mask round its eyes, pink plumes falling from its crown, a long tail of brown and
    /// white streamers, orange legs, and a toxic chain draped across its breast.
    /// </summary>
    private static PokeBuilder Fezandipiti()
    {
        var b = new PokeBuilder("Fezandipiti", 0.95f, BodyPlan.Bird, V(0, 0.48f, 0)) { Coat = Fur };
        var black = Rgb(40, 38, 46);
        var brown = Rgb(156, 82, 50);
        var white = Rgb(244, 242, 238);
        var yellow = Rgb(222, 178, 60);
        var pink = Rgb(218, 96, 170);
        BirdLegs(b, 0.07f, 0.4f, 0.0f, 0.022f, Rgb(214, 120, 70));
        var bc = V(0, 0.48f, -0.02f);
        b.Ell(Body, bc, V(0.12f, 0.13f, 0.16f), black);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.1f * s, 0.06f, 0.02f));
            var tip = bc + V(0.24f * s, -0.12f, -0.2f);
            b.Ell(wing, Vector3.Lerp(bc + V(0.1f * s, 0.06f, 0.02f), tip, 0.5f), V(0.05f, 0.2f, 0.12f), black, Euler(tip - bc), blend: 0.015f);
            for (int i = 0; i < 4; i++)
            {
                var root = Vector3.Lerp(bc + V(0.12f * s, 0.02f, 0.0f), tip, 0.4f + 0.2f * i);
                Frond(b, wing, root, root + V(0.04f * s, -0.14f, -0.06f), 0.035f, brown, V(1f * s, 0, 0.2f), 0.22f, Fur, 0.01f);
            }
        });
        int tail = b.Tail(bc + V(0, -0.02f, -0.15f));
        foreach (var (x, len, color) in new[] { (0f, 0.42f, brown), (-0.05f, 0.34f, brown), (0.05f, 0.34f, brown), (-0.02f, 0.38f, white), (0.02f, 0.38f, white) })
        {
            var root = bc + V(x, -0.02f, -0.14f);
            Frond(b, tail, root, root + V(x * 2f, -0.3f, -len * 0.6f), 0.03f, color, V(0, 1f, -0.3f), 0.22f, Fur, 0.01f);
        }
        // The long white neck and the head
        int neck = b.Part("neck", Body, bc + V(0, 0.1f, 0.1f), PokeRole.Head, 0.4f);
        var np = Smooth(3, bc + V(0, 0.08f, 0.1f), bc + V(0, 0.22f, 0.12f), bc + V(0, 0.36f, 0.08f));
        b.Tube(neck, np, 0.045f, 0.035f, white, blend: 0f);
        int head = b.Head(np[^1], neck);
        var c = np[^1] + V(0, 0.04f, 0.03f);
        var r = V(0.045f, 0.045f, 0.06f);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0, 0.03f, -0.01f), V(0.05f, 0.025f, 0.06f), black, soft: 0.006f);
        b.PaintEll(head, c + V(0, 0.0f, 0.03f), V(0.05f, 0.016f, 0.04f), Rgb(214, 60, 60), soft: 0.005f);
        b.Spike(head, c + V(0, -0.005f, 0.05f), c + V(0, -0.02f, 0.1f), 0.014f, yellow, mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.025f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(232, 196, 60), glare: true);
            b.Tube(head, Smooth(3, c + V(0.025f * s, 0.04f, -0.01f), c + V(0.06f * s, 0.09f, -0.02f), c + V(0.05f * s, 0.12f, 0.01f)), 0.01f, 0.007f, yellow, Shell, 0f);
            b.Tube(head, Smooth(3, c + V(0.03f * s, 0.0f, -0.03f), c + V(0.06f * s, -0.06f, -0.03f), c + V(0.05f * s, -0.13f, 0.0f)), 0.012f, 0.008f, pink, Fur, 0f);
        });
        // The chain draped across its breast
        var links = new List<Vector3>();
        for (int i = 0; i < 7; i++)
        {
            float t = i / 6f;
            links.Add(bc + V(-0.1f + 0.2f * t, 0.04f - 0.1f * MathF.Sin(t * MathF.PI), 0.12f + 0.03f * MathF.Sin(t * MathF.PI)));
        }
        ChainLinks(b, Body, links.ToArray(), 0.026f, 0.009f);
        return b;
    }

    // ------------------------------------------------------------------ Ogerpon

    private enum OgerponMask { Teal, Wellspring, Hearthflame, Cornerstone }

    /// <summary>
    /// Ogerpon in each of its masks: a little ogre in a green leafy tunic with a yellow button and a white star, long
    /// dark arms and legs sprouting leaves, an orange face with yellow eyes and fangs under a dark hood with one black
    /// horn, great rounded lobes of its mask's colour at either side of its head and the mask itself worn tipped over
    /// one side of its face: a leafy green mask crowned with a star (Teal), a blue one with closed eyes and a crown like
    /// drops of water (Wellspring), a red ogre with golden horns and a blue mouth (Hearthflame) or a grey block of
    /// stone with glowing blue eyes (Cornerstone).
    /// </summary>
    private static PokeBuilder OgerponBuild(OgerponMask mask, string name)
    {
        var b = new PokeBuilder(name, 0.8f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Leaf };
        var dark = Rgb(48, 50, 50);
        var green = Rgb(82, 152, 62);
        var orange = Rgb(240, 152, 56);
        var (color, deep) = mask switch
        {
            OgerponMask.Wellspring => (Rgb(60, 140, 210), Rgb(36, 90, 160)),
            OgerponMask.Hearthflame => (Rgb(196, 54, 44), Rgb(140, 34, 34)),
            OgerponMask.Cornerstone => (Rgb(110, 112, 118), Rgb(70, 72, 78)),
            _ => (Rgb(96, 168, 72), Rgb(58, 120, 50))
        };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.34f, 0));
            b.Limb(leg, V(0.04f * s, 0.34f, 0), V(0.05f * s, 0.06f, 0.0f), 0.026f, 0.022f, dark);
            b.Ell(leg, V(0.05f * s, 0.03f, 0.02f), V(0.03f, 0.03f, 0.045f), dark);
            Petal(b, leg, V(0.05f * s, 0.05f, 0.05f), V(0.07f * s, 0.08f, 0.08f), 0.012f, green, Leaf);
        });
        var bc = V(0, 0.42f, 0);
        b.Ell(Body, bc, V(0.07f, 0.09f, 0.06f), green);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            Petal(b, Body, bc + V(n.X * 0.05f, -0.05f, n.Z * 0.04f), bc + V(n.X * 0.08f, -0.12f, n.Z * 0.07f), 0.03f, green, Leaf);
        }
        b.Ell(Body, bc + V(0.0f, 0.02f, 0.06f), V(0.013f, 0.013f, 0.01f), Rgb(240, 210, 60), blend: 0.004f);
        b.Mark(Body, On(bc, V(0.07f, 0.09f, 0.06f), bc.X - 0.03f, bc.Y - 0.03f), V(-0.3f, 0, 1f), 0.016f, 0.016f, White, MarkShape.Star5);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.06f * s, 0.05f, 0));
            var hand = s < 0 ? bc + V(0.22f * s, -0.02f, 0.06f) : bc + V(0.12f * s, -0.12f, 0.03f);
            b.Limb(arm, bc + V(0.06f * s, 0.05f, 0), hand, 0.018f, 0.016f, dark);
            b.Ell(arm, hand, V(0.022f, 0.022f, 0.022f), dark);
            Petal(b, arm, hand + V(0, 0.01f, 0), hand + V(0.02f * s, 0.04f, 0.0f), 0.01f, green, Leaf);
        });
        // The head: an orange face under a dark hood with one black horn
        int head = b.Head(bc + V(0, 0.09f, 0));
        var c = bc + V(0, 0.2f, 0.0f);
        b.Limb(head, bc + V(0, 0.06f, 0), c + V(0, -0.06f, 0), 0.032f, 0.03f, dark);
        b.Ell(head, c, V(0.085f, 0.085f, 0.08f), dark);
        b.Spike(head, c + V(0.04f, 0.07f, -0.01f), c + V(0.07f, 0.18f, -0.02f), 0.025f, Rgb(36, 36, 38), mat: Shell);
        var fc = c + V(0.02f, -0.01f, 0.05f);
        var fr = V(0.055f, 0.055f, 0.04f);
        b.Ell(head, fc, fr, orange, blend: 0.01f);
        foreach (var (x, y) in new[] { (0.028f, -0.03f), (0.038f, -0.022f), (0.0f, -0.035f) })
            b.PaintEll(head, fc + V(x, y, 0.035f), V(0.004f, 0.004f, 0.004f), Rgb(60, 40, 30), soft: 0.002f);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, fc.X + 0.022f * s, fc.Y + 0.01f);
            b.Eye(head, at, Outward(fc, fr, at), 0.013f, Rgb(70, 50, 20), white: Rgb(252, 220, 60), pupil: Rgb(30, 30, 30));
            b.Spike(head, fc + V(0.014f * s, -0.03f, 0.035f), fc + V(0.016f * s, -0.045f, 0.04f), 0.005f, White, mat: Shell, blend: 0.002f);
            // The great rounded lobes of its mask's colour
            var lobe = c + V(0.12f * s, 0.0f, -0.03f);
            b.Ell(head, lobe, V(0.08f, 0.1f, 0.07f), color, V(0, 0, -25f * s), Leaf, 0.02f);
            b.PaintEll(head, lobe + V(0.03f * s, 0.06f, 0.0f), V(0.04f, 0.03f, 0.06f), deep, soft: 0.01f);
        });
        // The mask worn tipped over the left of its face
        var mc = c + V(-0.07f, 0.0f, 0.07f);
        var face = V(-0.5f, 0.05f, 1f);
        switch (mask)
        {
            case OgerponMask.Teal:
                b.Ell(head, mc, V(0.05f, 0.06f, 0.035f), color, V(0, -25f, 0), Leaf, 0.01f);
                for (int i = 0; i < 7; i++)
                {
                    float a = (i * 50f - 150f) * Degree;
                    var n = V(MathF.Cos(a), MathF.Sin(a), 0);
                    Petal(b, head, mc + n * 0.04f, mc + n * 0.08f + V(0, 0, -0.01f), 0.02f, i % 2 == 0 ? deep : color, Leaf);
                }
                b.Mark(head, mc + V(0, 0.055f, 0.0f) + V(0.01f, 0, 0.02f), V(-0.4f, 0.6f, 0.7f), 0.014f, 0.014f, White, MarkShape.Star5);
                break;
            case OgerponMask.Wellspring:
                b.Ell(head, mc, V(0.05f, 0.065f, 0.035f), color, V(0, -25f, 0), Shell, 0.01f);
                b.PaintEll(head, mc + V(0.01f, -0.01f, 0.025f), V(0.035f, 0.04f, 0.02f), Rgb(232, 238, 246), V(0, -25f, 0), 0.006f);
                foreach (float x in new[] { -0.03f, 0f, 0.03f })
                    b.Ell(head, mc + V(x, 0.07f, -0.01f), V(0.015f, 0.025f, 0.015f), color, mat: Shell, blend: 0.008f);
                break;
            case OgerponMask.Hearthflame:
                b.Ell(head, mc, V(0.055f, 0.06f, 0.035f), color, V(0, -25f, 0), Shell, 0.01f);
                b.PaintEll(head, mc + V(0.01f, -0.03f, 0.03f), V(0.03f, 0.012f, 0.02f), Rgb(70, 180, 210), V(0, -25f, 0), 0.004f);
                PokeBuilder.Both(t => b.Spike(head, mc + V(0.03f * t, 0.04f, -0.01f), mc + V(0.07f * t, 0.1f, -0.02f), 0.016f, Rgb(232, 186, 60), mat: Metal, blend: 0.004f));
                Blade(b, head, mc + V(-0.04f, 0.0f, -0.01f), mc + V(-0.11f, 0.04f, -0.02f), 0.025f, Rgb(232, 186, 60), V(0, 0, 1f), 0.3f, Metal);
                break;
            default:
                b.Box(head, mc, V(0.05f, 0.06f, 0.03f), 0.012f, color, V(0, -25f, 0), Shell, 0.008f);
                b.Box(head, mc + V(0, 0.07f, -0.01f), V(0.03f, 0.02f, 0.025f), 0.008f, deep, V(0, -25f, 0), Shell, 0.006f);
                b.PaintEll(head, mc + V(0.01f, -0.035f, 0.03f), V(0.03f, 0.008f, 0.02f), deep, V(0, -25f, 0), 0.003f);
                break;
        }
        foreach (float x in new[] { -0.018f, 0.018f })
        {
            var hole = mc + V(x, 0.012f, 0.0f) + Vector3.Normalize(face) * 0.035f;
            b.PaintEll(head, hole, V(0.01f, 0.007f, 0.01f), mask == OgerponMask.Cornerstone ? Rgb(90, 200, 240) : Rgb(30, 28, 30), V(0, -25f, 0), 0.003f);
        }
        return b;
    }

    private static PokeBuilder Ogerpon() => OgerponBuild(OgerponMask.Teal, "Ogerpon");
    private static PokeBuilder OgerponWellspring() => OgerponBuild(OgerponMask.Wellspring, "Ogerpon-Wellspring-Mask");
    private static PokeBuilder OgerponHearthflame() => OgerponBuild(OgerponMask.Hearthflame, "Ogerpon-Hearthflame-Mask");
    private static PokeBuilder OgerponCornerstone() => OgerponBuild(OgerponMask.Cornerstone, "Ogerpon-Cornerstone-Mask");

    // ------------------------------------------------------------------ Archaludon

    /// <summary>
    /// Archaludon: a dragon of steel built like a bridge, its tall navy body striped red and laced with white girders,
    /// standing on four long legs braced wide on white blocks, two white pylons rising from its shoulders with tan tips,
    /// and its small head between them with yellow eyes and two navy spires.
    /// </summary>
    private static PokeBuilder Archaludon()
    {
        var b = new PokeBuilder("Archaludon", 1f, BodyPlan.Quadruped, V(0, 0.58f, 0)) { Coat = Metal };
        var navy = Rgb(40, 52, 104);
        var white = Rgb(236, 238, 246);
        var red = Rgb(196, 80, 74);
        var tan = Rgb(220, 186, 140);
        var bc = V(0, 0.6f, 0);
        b.Box(Body, bc, V(0.1f, 0.22f, 0.08f), 0.04f, navy, mat: Metal, blend: 0.01f);
        foreach (float x in new[] { -0.045f, 0f, 0.045f })
            b.PaintEll(Body, bc + V(x, -0.02f, 0.08f), V(0.012f, 0.18f, 0.02f), red, soft: 0.004f);
        foreach (float y in new[] { 0.1f, -0.04f })
            PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0, y, 0.082f), V(0.1f, 0.008f, 0.012f), white, V(0, 0, 38f * s), 0.003f));
        // Four long legs braced wide on white blocks
        foreach (var (z, front) in new[] { (0.05f, true), (-0.05f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = bc + V(0.09f * s, front ? -0.05f : -0.12f, z);
                int leg = b.Leg(s, hip, front);
                var foot = V(0.28f * s, 0.07f, z * 3.2f);
                b.Limb(leg, hip, Vector3.Lerp(hip, foot, 0.55f), 0.045f, 0.04f, navy);
                b.Limb(leg, Vector3.Lerp(hip, foot, 0.55f), foot, 0.04f, 0.035f, white);
                b.Box(leg, foot + V(0, -0.035f, 0), V(0.05f, 0.035f, 0.06f), 0.01f, white, mat: Metal, blend: 0.006f);
                b.PaintEll(leg, foot + V(0, -0.035f, 0.06f), V(0.02f, 0.03f, 0.01f), navy, soft: 0.004f);
            });
        // The two pylons rising from its shoulders
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.1f * s, 0.12f, 0));
            var root = bc + V(0.12f * s, 0.08f, 0);
            var tip = bc + V(0.22f * s, 0.52f, -0.02f);
            b.Spike(arm, root, tip, 0.06f, white, 0.65f, Metal, 0.01f);
            b.PaintEll(arm, Vector3.Lerp(root, tip, 0.92f), V(0.03f, 0.04f, 0.03f), tan, soft: 0.006f);
        });
        int head = b.Head(bc + V(0, 0.2f, 0.02f));
        var c = bc + V(0, 0.28f, 0.04f);
        var r = V(0.05f, 0.05f, 0.055f);
        b.Ell(head, c, r, navy);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.022f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(246, 196, 60), glare: true);
            b.Spike(head, c + V(0.03f * s, 0.03f, -0.01f), c + V(0.05f * s, 0.22f, -0.03f), 0.022f, navy, 0.6f, Metal);
            b.PaintEll(head, c + V(0.05f * s, 0.21f, -0.03f), V(0.012f, 0.02f, 0.012f), tan, soft: 0.004f);
        });
        int tail = b.Tail(bc + V(0, -0.16f, -0.07f));
        b.Spike(tail, bc + V(0, -0.16f, -0.06f), bc + V(0, -0.3f, -0.22f), 0.04f, navy, 0.6f, Metal);
        return b;
    }

    // ------------------------------------------------------------------ Gouging Fire, Raging Bolt

    /// <summary>
    /// Gouging Fire: Entei's ancient past, a hulking shaggy beast of brown fur with a pale belly, black cuffs on its legs
    /// and red claws, curved white tusks either side of a red-masked face, a great crown of golden blades streaked red
    /// and green rising from its head and down its back with grey spikes behind, and a cloud of grey smoke for a tail.
    /// </summary>
    private static PokeBuilder GougingFire()
    {
        var b = new PokeBuilder("Gouging Fire", 1f, BodyPlan.Quadruped, V(0, 0.4f, -0.02f)) { Coat = Fur };
        var brown = Rgb(156, 104, 62);
        var dark = Rgb(112, 72, 46);
        var pale = Rgb(214, 186, 140);
        var gold = Rgb(232, 196, 82);
        var grey = Rgb(170, 170, 176);
        BeastLegs(b, 0.13f, 0.32f, 0.18f, -0.18f, 0.075f, brown, Rgb(150, 170, 196), dark, Rgb(36, 34, 40));
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.18f, -0.18f })
                for (int t = -1; t <= 1; t++)
                    b.Spike(Body, V(0.137f * s + t * 0.02f, 0.03f, z + 0.1f), V(0.14f * s + t * 0.024f, 0.012f, z + 0.14f), 0.01f, Rgb(214, 54, 44), mat: Shell, blend: 0.003f);
        });
        var bc = V(0, 0.42f, -0.02f);
        var br = V(0.19f, 0.16f, 0.27f);
        b.Ell(Body, bc, br, brown);
        b.PaintEll(Body, bc + V(0, -0.1f, 0.05f), V(0.14f, 0.07f, 0.2f), pale, soft: 0.015f);
        Shag(b, Body, bc, br, bc.Y - 0.06f, 16, 0.08f, 0.04f, dark, 0.8f);
        // The crown of golden blades down its back, grey spikes behind
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            var root = bc + V(0, 0.14f - 0.04f * t, 0.18f - 0.36f * t);
            PokeBuilder.Both(s => Blade(b, Body, root + V(0.05f * s, 0, 0), root + V(0.14f * s, 0.2f - 0.06f * t, -0.12f), 0.045f, i < 3 ? gold : grey, V(1f * s, 0, 0.4f), 0.25f, Shell));
        }
        int tail = b.Tail(bc + V(0, 0.04f, -0.26f));
        foreach (var (p, size) in new[] { (V(0, 0.06f, -0.3f), 0.08f), (V(0.05f, 0.12f, -0.38f), 0.07f), (V(-0.04f, 0.17f, -0.44f), 0.065f), (V(0.03f, 0.24f, -0.5f), 0.055f) })
            b.Ell(tail, bc + p, V(size, size * 0.85f, size), grey, blend: 0.03f);
        // The head: a red mask, white tusks and the crown rising from it
        int head = b.Head(bc + V(0, 0.08f, 0.24f));
        var c = bc + V(0, 0.12f, 0.33f);
        var r = V(0.1f, 0.09f, 0.1f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.03f, 0.08f), V(0.07f, 0.05f, 0.05f), pale);
        b.PaintEll(head, c + V(0, 0.02f, 0.08f), V(0.09f, 0.03f, 0.04f), Rgb(196, 46, 40), soft: 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.04f * s, c.Y + 0.02f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(60, 40, 30), white: Rgb(250, 230, 120), glare: true);
            b.Tube(head, Smooth(3, c + V(0.05f * s, -0.04f, 0.1f), c + V(0.08f * s, -0.1f, 0.14f), c + V(0.1f * s, -0.08f, 0.2f)), 0.016f, 0.01f, Rgb(244, 240, 228), Shell, 0f);
        });
        for (int i = 0; i < 7; i++)
        {
            float a = (-60f + 20f * i) * Degree;
            var root = c + V(MathF.Sin(a) * 0.06f, 0.07f, -0.02f);
            Blade(b, head, root, root + V(MathF.Sin(a) * 0.14f, 0.2f - MathF.Abs(MathF.Sin(a)) * 0.08f, -0.08f), 0.04f, gold, V(MathF.Cos(a) * 0.2f, 0, 1f), 0.25f, Shell);
            if (i % 3 == 1) b.PaintEll(head, root + V(MathF.Sin(a) * 0.07f, 0.1f, -0.04f), V(0.02f, 0.04f, 0.02f), i == 1 ? Rgb(200, 50, 40) : Rgb(70, 140, 60), soft: 0.006f);
        }
        return b;
    }

    /// <summary>
    /// Raging Bolt: Raikou's ancient past, a long-necked dinosaur of yellow fur striped with black zigzags and a white
    /// belly, red flames at the tops of its white-toed feet, blue lightning standing along its back and down its long
    /// tail to a star of a tip, and on its small head a great flat purple storm cloud with white fur bristling over it.
    /// </summary>
    private static PokeBuilder RagingBolt()
    {
        var b = new PokeBuilder("Raging Bolt", 1f, BodyPlan.Quadruped, V(0, 0.38f, 0)) { Coat = Fur };
        var yellow = Rgb(244, 206, 66);
        var white = Rgb(244, 244, 238);
        var black = Rgb(44, 42, 48);
        var red = Rgb(232, 74, 48);
        var blue = Rgb(110, 186, 234);
        var cloud = Rgb(152, 136, 196);
        foreach (var (z, front) in new[] { (0.14f, true), (-0.16f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.09f * s, 0.34f, z);
                int leg = b.Leg(s, hip, front);
                var ankle = V(0.1f * s, 0.08f, z + 0.01f);
                b.Limb(leg, hip, ankle, 0.05f, 0.04f, yellow);
                b.Ell(leg, ankle + V(0, -0.04f, 0.02f), V(0.045f, 0.04f, 0.055f), white);
                for (int t = -1; t <= 1; t++)
                    Blade(b, leg, ankle + V(t * 0.02f, 0.0f, 0.03f), ankle + V(t * 0.035f, 0.06f, 0.07f), 0.016f, red, V(0, 0.3f, 1f), 0.3f, Glow);
            });
        var bc = V(0, 0.4f, -0.02f);
        b.Ell(Body, bc, V(0.12f, 0.12f, 0.24f), yellow);
        b.PaintEll(Body, bc + V(0, -0.07f, 0.03f), V(0.09f, 0.06f, 0.2f), white, soft: 0.012f);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 4; i++)
            {
                var at = bc + V(0.11f * s, 0.03f, 0.15f - 0.1f * i);
                b.PaintEll(Body, at, V(0.012f, 0.05f, 0.012f), black, V(0, 0, 20f * s), 0.004f);
                b.PaintEll(Body, at + V(0, 0.04f, -0.02f), V(0.012f, 0.03f, 0.012f), black, V(-40f, 0, 0), 0.004f);
            }
        });
        for (int i = 0; i < 5; i++)
        {
            float dz = 0.12f - 0.07f * i;
            var root = bc + V(0, 0.12f * MathF.Sqrt(MathF.Max(0f, 1f - dz * dz / (0.24f * 0.24f))) - 0.012f, dz);
            Lightning(b, Body, new[] { root, root + V(0, 0.06f, -0.02f), root + V(0, 0.08f, 0.02f), root + V(0, 0.15f, -0.02f) }, new[] { 0.018f, 0.016f, 0.014f }, 0.008f, blue, V(1f, 0, 0), mat: Glow);
        }
        int tail = b.Tail(bc + V(0, 0.02f, -0.22f));
        var tp = Smooth(3, bc + V(0, 0.02f, -0.22f), bc + V(0, 0.0f, -0.42f), bc + V(0.03f, 0.04f, -0.6f), bc + V(0.06f, 0.1f, -0.74f));
        b.Tube(tail, tp, 0.05f, 0.016f, yellow, blend: 0f);
        for (int i = 2; i < tp.Length - 1; i += 3)
            Lightning(b, tail, new[] { tp[i], tp[i] + V(0, 0.05f, -0.02f), tp[i] + V(0, 0.07f, 0.02f), tp[i] + V(0, 0.12f, -0.02f) }, new[] { 0.014f, 0.012f, 0.01f }, 0.007f, blue, V(1f, 0, 0), mat: Glow);
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f;
            b.Spike(tail, tp[^1], tp[^1] + V(MathF.Cos(a) * 0.06f, MathF.Sin(a) * 0.06f, -0.02f), 0.016f, blue, 0.4f, Glow, 0.006f);
        }
        // The long neck up to a small head under its storm cloud
        int neck = b.Part("neck", Body, bc + V(0, 0.08f, 0.18f), PokeRole.Head, 0.4f);
        var np = Smooth(3, bc + V(0, 0.06f, 0.18f), bc + V(0, 0.3f, 0.26f), bc + V(0, 0.56f, 0.26f), bc + V(0, 0.72f, 0.3f));
        b.Tube(neck, np, 0.065f, 0.045f, yellow, blend: 0f);
        for (int i = 1; i < np.Length - 1; i++)
            b.PaintEll(neck, np[i] + V(0, 0, 0.04f), V(0.035f, 0.03f, 0.02f), white, soft: 0.008f);
        for (int i = 1; i < np.Length - 1; i += 2)
            PokeBuilder.Both(s => b.PaintEll(neck, np[i] + V(0.055f * s, 0, 0), V(0.01f, 0.04f, 0.01f), black, V(0, 0, 25f * s), 0.004f));
        int head = b.Head(np[^1], neck);
        var c = np[^1] + V(0, 0.02f, 0.05f);
        var r = V(0.05f, 0.04f, 0.065f);
        b.Ell(head, c, r, yellow);
        b.PaintEll(head, c + V(0, -0.02f, 0.03f), V(0.04f, 0.02f, 0.05f), white, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.025f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(232, 60, 50), glare: true);
        });
        var cc = c + V(0, 0.08f, -0.05f);
        b.Limb(head, c + V(0, 0.02f, -0.02f), cc, 0.03f, 0.03f, yellow);
        foreach (var (p, s) in new[] { (V(0, 0, 0), V(0.25f, 0.045f, 0.2f)), (V(0.18f, 0.015f, 0.03f), V(0.11f, 0.06f, 0.11f)), (V(-0.18f, 0.015f, 0.03f), V(0.11f, 0.06f, 0.11f)),
                     (V(0.08f, 0.02f, -0.14f), V(0.11f, 0.06f, 0.09f)), (V(-0.08f, 0.02f, -0.14f), V(0.11f, 0.06f, 0.09f)), (V(0, 0.015f, 0.16f), V(0.11f, 0.05f, 0.08f)) })
            b.Ell(head, cc + p, s, cloud, blend: 0.03f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-75f + 25f * i) * Degree;
            var root = cc + V(MathF.Sin(a) * 0.1f, 0.03f, MathF.Cos(a) * 0.05f - 0.06f);
            b.Spike(head, root, root + V(MathF.Sin(a) * 0.08f, 0.13f + 0.03f * (i % 2), -0.06f), 0.022f, white, 0.7f);
        }
        return b;
    }

    // ------------------------------------------------------------------ Iron Boulder

    /// <summary>
    /// Iron Boulder: Terrakion's future, a heavy grey machine of a bull with a broad dark chest, dark hooves rimmed with
    /// glowing orange blades, two dark horns curving from its head, orange eyes, and a great frame of glowing orange
    /// blades round its head like a crest.
    /// </summary>
    private static PokeBuilder IronBoulder()
    {
        var b = new PokeBuilder("Iron Boulder", 0.95f, BodyPlan.Quadruped, V(0, 0.42f, 0)) { Coat = Metal };
        var grey = Rgb(170, 176, 186);
        var dark = Rgb(58, 54, 60);
        var orange = Rgb(250, 164, 62);
        foreach (var (z, front) in new[] { (0.13f, true), (-0.16f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.1f * s, 0.32f, z);
                int leg = b.Leg(s, hip, front);
                var knee = V(0.12f * s, 0.18f, z + 0.02f);
                b.Limb(leg, hip, knee, 0.06f, 0.045f, grey);
                b.Ell(leg, knee, V(0.045f, 0.045f, 0.045f), dark);
                b.Limb(leg, knee, V(0.12f * s, 0.06f, z), 0.04f, 0.04f, grey);
                b.Ell(leg, V(0.12f * s, 0.035f, z + 0.01f), V(0.05f, 0.035f, 0.055f), dark, mat: Shell);
                for (int t = -1; t <= 1; t++)
                    Blade(b, leg, V(0.12f * s + t * 0.03f, 0.04f, z + 0.03f), V(0.12f * s + t * 0.06f, 0.11f, z + 0.08f), 0.02f, orange, V(0, 0.3f, 1f), 0.3f, Glow);
            });
        var bc = V(0, 0.44f, -0.04f);
        b.Ell(Body, bc, V(0.16f, 0.15f, 0.22f), grey);
        b.Ell(Body, bc + V(0, 0.04f, 0.15f), V(0.16f, 0.17f, 0.12f), dark, blend: 0.02f);
        b.PaintEll(Body, bc + V(0, 0.12f, -0.05f), V(0.12f, 0.05f, 0.14f), Rgb(140, 146, 156), soft: 0.02f);
        int head = b.Head(bc + V(0, 0.06f, 0.24f));
        var c = bc + V(0, 0.05f, 0.31f);
        var r = V(0.075f, 0.075f, 0.07f);
        b.Ell(head, c, r, grey);
        b.PaintEll(head, c + V(0, 0.0f, 0.06f), V(0.06f, 0.045f, 0.03f), dark, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.028f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(250, 150, 50), glare: true);
            b.Tube(head, Smooth(3, c + V(0.05f * s, 0.05f, 0.0f), c + V(0.1f * s, 0.1f, -0.02f), c + V(0.11f * s, 0.18f, 0.03f)), 0.02f, 0.01f, Rgb(40, 38, 42), Shell, 0f);
            // The frame of glowing blades round its head
            Blade(b, head, c + V(0.06f * s, -0.02f, -0.02f), c + V(0.19f * s, 0.1f, -0.03f), 0.03f, orange, V(0, 0, 1f), 0.25f, Glow);
            Blade(b, head, c + V(0.17f * s, 0.06f, -0.03f), c + V(0.12f * s, 0.26f, -0.04f), 0.028f, orange, V(0, 0, 1f), 0.25f, Glow);
            Blade(b, head, c + V(0.13f * s, 0.23f, -0.04f), c + V(0.01f * s, 0.28f, -0.04f), 0.024f, orange, V(0, 0, 1f), 0.25f, Glow);
        });
        int tail = b.Tail(bc + V(0, 0.04f, -0.21f));
        b.Spike(tail, bc + V(0, 0.04f, -0.2f), bc + V(0, 0.0f, -0.3f), 0.03f, dark, 0.6f, Metal);
        return b;
    }

    // ------------------------------------------------------------------ Terapagos

    /// <summary>A shell of crystal panels: the dome at <paramref name="c"/> with panels of many colours painted over it.</summary>
    private static void CrystalPanels(PokeBuilder b, int bone, Vector3 c, Vector3 r, int count, float size, float seed)
    {
        var colors = new[] { Rgb(250, 220, 90), Rgb(120, 210, 120), Rgb(110, 170, 240), Rgb(236, 120, 180), Rgb(170, 120, 230), Rgb(240, 140, 80), Rgb(120, 220, 220) };
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < count; i++)
        {
            float y = 1f - 1.6f * (i + 0.5f) / count, ring = MathF.Sqrt(MathF.Max(0f, 1f - y * y)), a = i * golden + seed;
            var at = Out(c, r, default, V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring));
            b.PaintEll(bone, at, V(size, size, size), colors[i % colors.Length], soft: size * 0.3f);
        }
    }

    /// <summary>
    /// Terapagos in its Normal Form: a small dark blue tortoise with a shell of pale crystal faceted and ringed teal,
    /// stubby legs banded teal, a long neck wound round with a teal coil, a round head with big blue eyes and a teal
    /// crystal crest tipped lilac, and a tail ending in a teal star.
    /// </summary>
    private static PokeBuilder Terapagos()
    {
        var b = new PokeBuilder("Terapagos", 0.55f, BodyPlan.Quadruped, V(0, 0.12f, 0)) { Coat = Shell };
        var navy = Rgb(48, 44, 124);
        var teal = Rgb(104, 206, 186);
        var crystal = Rgb(218, 230, 178);
        foreach (var (z, front) in new[] { (0.06f, true), (-0.06f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.07f * s, 0.1f, z);
                int leg = b.Leg(s, hip, front);
                b.Limb(leg, hip, V(0.085f * s, 0.03f, z + 0.01f), 0.03f, 0.03f, navy);
                b.Torus(leg, V(0.08f * s, 0.06f, z), 0.031f, 0.009f, teal, mat: Glow, blend: 0.004f);
                b.Ell(leg, V(0.085f * s, 0.022f, z + 0.015f), V(0.032f, 0.022f, 0.036f), navy);
            });
        var bc = V(0, 0.12f, 0);
        b.Ell(Body, bc, V(0.1f, 0.06f, 0.12f), navy);
        var sc = bc + V(0, 0.04f, -0.01f);
        var sr = V(0.095f, 0.06f, 0.11f);
        b.Ell(Body, sc, sr, crystal, mat: Glow, blend: 0.01f);
        b.Torus(Body, bc + V(0, 0.015f, -0.01f), 0.1f, 0.012f, teal, sz: 1.15f, mat: Glow, blend: 0.006f);
        CrystalPanels(b, Body, sc, sr, 9, 0.022f, 0.4f);
        int neck = b.Part("neck", Body, bc + V(0, 0.02f, 0.1f), PokeRole.Head, 0.4f);
        var np = Smooth(3, bc + V(0, 0.02f, 0.1f), bc + V(0, 0.1f, 0.15f), bc + V(0, 0.18f, 0.16f));
        b.Tube(neck, np, 0.03f, 0.028f, navy, blend: 0f);
        for (int i = 0; i < 3; i++)
            b.Torus(neck, Vector3.Lerp(np[0], np[^1], 0.25f + 0.25f * i), 0.03f, 0.008f, teal, V(18f, 0, 18f * (i - 1)), mat: Glow, blend: 0.004f);
        int head = b.Head(np[^1], neck);
        var c = np[^1] + V(0, 0.04f, 0.02f);
        var r = V(0.055f, 0.05f, 0.05f);
        b.Ell(head, c, r, navy);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.024f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(80, 150, 240));
        });
        b.Spike(head, c + V(0, 0.04f, -0.01f), c + V(0.02f, 0.1f, -0.03f), 0.022f, teal, 0.6f, Glow);
        b.Spike(head, c + V(0.02f, 0.09f, -0.03f), c + V(0.06f, 0.13f, -0.06f), 0.016f, Rgb(176, 156, 222), 0.6f, Glow);
        int tail = b.Tail(bc + V(0, 0.0f, -0.11f));
        var tip = bc + V(0.03f, 0.02f, -0.2f);
        b.Limb(tail, bc + V(0, 0.0f, -0.1f), tip, 0.016f, 0.01f, navy);
        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.Tau / 4f;
            b.Spike(tail, tip, tip + V(MathF.Cos(a) * 0.03f, MathF.Sin(a) * 0.03f, -0.01f), 0.01f, teal, 0.5f, Glow, 0.004f);
        }
        return b;
    }

    /// <summary>
    /// Terapagos in its Terastal Form: a great low dome of crystal panels of every colour on short dark legs, wisps of
    /// glowing blue streaming from its sides, and its small dark head peering out at the front with a teal crest.
    /// </summary>
    private static PokeBuilder TerapagosTerastal()
    {
        var b = new PokeBuilder("Terapagos-Terastal", 0.85f, BodyPlan.Quadruped, V(0, 0.18f, 0)) { Coat = Shell };
        var navy = Rgb(48, 44, 124);
        var wisp = Rgb(150, 206, 244);
        StubbyLegs(b, 0.16f, 0.1f, 0.12f, -0.12f, 0.05f, navy);
        var dc = V(0, 0.18f, -0.02f);
        var dr = V(0.3f, 0.15f, 0.32f);
        b.Ell(Body, dc, dr, Rgb(196, 214, 236), mat: Glow);
        CrystalPanels(b, Body, dc, dr, 24, 0.055f, 0.2f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            if (n.Z > 0.85f) continue;
            var root = dc + V(n.X * dr.X * 0.9f, -0.02f, n.Z * dr.Z * 0.9f);
            Blade(b, Body, root, root + n * 0.14f + V(0, 0.04f * (i % 2), -0.05f), 0.05f, wisp, V(0, 1f, 0), 0.25f, Glow);
        }
        int head = b.Head(dc + V(0, -0.02f, 0.28f));
        var c = dc + V(0, -0.02f, 0.33f);
        var r = V(0.06f, 0.05f, 0.05f);
        b.Ell(head, c, r, navy);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.026f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(80, 150, 240));
        });
        b.Spike(head, c + V(0, 0.04f, -0.01f), c + V(0, 0.08f, -0.05f), 0.02f, Rgb(104, 206, 186), 0.6f, Glow);
        return b;
    }

    /// <summary>
    /// Terapagos in its Stellar Form: a great orb of crystal panels shining in every colour, set on a short dark foot,
    /// its small dark head on top at the front under a jagged blue crown like a star.
    /// </summary>
    private static PokeBuilder TerapagosStellar()
    {
        var b = new PokeBuilder("Terapagos-Stellar", 1f, BodyPlan.Floating, V(0, 0.34f, 0)) { Coat = Shell };
        var navy = Rgb(48, 44, 124);
        var star = Rgb(70, 110, 230);
        var oc = V(0, 0.35f, 0);
        var or = V(0.28f, 0.3f, 0.28f);
        b.Ell(Body, V(0, 0.05f, 0), V(0.12f, 0.06f, 0.12f), navy);
        b.Ell(Body, oc, or, Rgb(226, 230, 246), mat: Glow);
        CrystalPanels(b, Body, oc, or, 30, 0.06f, 1.1f);
        int head = b.Head(oc + V(0, 0.25f, 0.1f));
        var c = oc + V(0, 0.29f, 0.12f);
        var r = V(0.055f, 0.045f, 0.05f);
        b.Ell(head, c, r, navy);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.024f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(80, 150, 240));
        });
        for (int i = 0; i < 6; i++)
        {
            float a = (i * 60f + 30f) * Degree;
            b.Spike(head, c + V(0, 0.05f, -0.02f), c + V(MathF.Sin(a) * 0.08f, 0.1f + MathF.Cos(a) * 0.08f, -0.04f), 0.02f, star, 0.4f, Glow, 0.006f);
        }
        return b;
    }

    // ------------------------------------------------------------------ Pecharunt

    /// <summary>
    /// Pecharunt: a little purple ghost between the two halves of a dark mochi peach, each half black with a cat's
    /// ears and a glowing pink face cracked with darker lines, its small body trailing a wisp, heart-pink eyes and a
    /// fang in its smile.
    /// </summary>
    private static PokeBuilder Pecharunt()
    {
        var b = new PokeBuilder("Pecharunt", 0.5f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Shell }.Hover();
        var black = Rgb(42, 32, 48);
        var pink = Rgb(244, 178, 214);
        var crack = Rgb(196, 90, 150);
        var purple = Rgb(134, 66, 146);
        var bc = V(0, 0.18f, 0.03f);
        b.Ell(Body, bc, V(0.06f, 0.07f, 0.055f), purple);
        b.Tube(Body, Smooth(3, bc + V(0, -0.05f, 0), bc + V(0.02f, -0.1f, -0.02f), bc + V(-0.02f, -0.14f, -0.04f)), 0.03f, 0.01f, Rgb(170, 90, 170), blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, V(0.06f, 0.07f, 0.055f), bc.X + 0.022f * s, bc.Y + 0.012f);
            b.Eye(Body, at, Outward(bc, V(0.06f, 0.07f, 0.055f), at), 0.013f, Rgb(236, 90, 160));
        });
        b.Spike(Body, bc + V(0.01f, -0.02f, 0.052f), bc + V(0.012f, -0.035f, 0.056f), 0.005f, White, mat: Shell, blend: 0.002f);
        // The two halves of the peach, held at its sides
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.05f * s, 0.01f, 0));
            var hc = V(0.15f * s, 0.24f, 0);
            var hr = V(0.11f, 0.12f, 0.1f);
            b.Limb(arm, bc + V(0.04f * s, 0.0f, 0), hc + V(-0.06f * s, -0.04f, 0), 0.016f, 0.014f, purple);
            b.Ell(arm, hc, hr, black);
            var face = hc + V(0.0f, 0.0f, 0.06f);
            b.PaintEll(arm, face, V(0.075f, 0.08f, 0.06f), pink, soft: 0.008f);
            foreach (var (d, turn) in new[] { (V(0.03f, 0.04f, 0), 30f), (V(-0.04f, 0.02f, 0), -50f), (V(0.02f, -0.05f, 0), 70f) })
                b.PaintEll(arm, face + d + V(0, 0, 0.035f), V(0.004f, 0.03f, 0.03f), crack, V(0, 0, turn), 0.003f);
            b.Spike(arm, hc + V(-0.04f * s, 0.09f, 0), hc + V(-0.06f * s, 0.18f, -0.01f), 0.035f, black, 0.6f);
            b.Spike(arm, hc + V(0.05f * s, 0.08f, 0), hc + V(0.08f * s, 0.16f, -0.01f), 0.035f, black, 0.6f);
        });
        return Lift(b);
    }
}
