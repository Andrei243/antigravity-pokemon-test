using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 14 (plan 46 · CP8): the 25 regional forms, "<Species>-Carpathia". Each is its species' own sculpt
// taken whole (its body, its silhouette, its eyes) and changed as docs/plans/decisions/CP-dex-5-forms.md says: new
// colours, parts taken away and parts of our own added. Every helper of this file is named C14..., since the other
// batches add theirs to the same partial class.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames14() => new[]
    {
        "Jigglypuff-Carpathia", "Wigglytuff-Carpathia", "Igglybuff-Carpathia", "Dunsparce-Carpathia", "Smeargle-Carpathia",
        "Whismur-Carpathia", "Loudred-Carpathia", "Exploud-Carpathia", "Sableye-Carpathia", "Spinda-Carpathia",
        "Chimecho-Carpathia", "Drifloon-Carpathia", "Drifblim-Carpathia", "Chingling-Carpathia", "Bronzor-Carpathia",
        "Bronzong-Carpathia", "Golett-Carpathia", "Golurk-Carpathia", "Skiddo-Carpathia", "Gogoat-Carpathia",
        "Noibat-Carpathia", "Noivern-Carpathia", "Sinistea-Carpathia", "Polteageist-Carpathia", "Dudunsparce-Carpathia"
    };

    private static PokeBuilder? CreateCarpathia14(string species) => species switch
    {
        "JIGGLYPUFF-CARPATHIA" => C14Jigglypuff(),
        "WIGGLYTUFF-CARPATHIA" => C14Wigglytuff(),
        "IGGLYBUFF-CARPATHIA" => C14Igglybuff(),
        "DUNSPARCE-CARPATHIA" => C14Dunsparce(),
        "DUDUNSPARCE-CARPATHIA" => C14Dudunsparce(),
        "SMEARGLE-CARPATHIA" => C14Smeargle(),
        "WHISMUR-CARPATHIA" => C14Whismur(),
        "LOUDRED-CARPATHIA" => C14Loudred(),
        "EXPLOUD-CARPATHIA" => C14Exploud(),
        "SABLEYE-CARPATHIA" => C14Sableye(),
        "SPINDA-CARPATHIA" => C14Spinda(),
        "CHINGLING-CARPATHIA" => C14Chingling(),
        "CHIMECHO-CARPATHIA" => C14Chimecho(),
        "DRIFLOON-CARPATHIA" => C14Drifloon(),
        "DRIFBLIM-CARPATHIA" => C14Drifblim(),
        "BRONZOR-CARPATHIA" => C14Bronzor(),
        "BRONZONG-CARPATHIA" => C14Bronzong(),
        "GOLETT-CARPATHIA" => C14Golett(),
        "GOLURK-CARPATHIA" => C14Golurk(),
        "SKIDDO-CARPATHIA" => C14Goat(false),
        "GOGOAT-CARPATHIA" => C14Goat(true),
        "NOIBAT-CARPATHIA" => C14Noibat(),
        "NOIVERN-CARPATHIA" => C14Noivern(),
        "SINISTEA-CARPATHIA" => C14Sinistea(),
        "POLTEAGEIST-CARPATHIA" => C14Polteageist(),
        _ => null
    };

    // ------------------------------------------------------------------ the colours of the region's crafts

    private static readonly Color C14Wool = Rgb(240, 228, 204);
    private static readonly Color C14FolkRed = Rgb(190, 36, 42);
    private static readonly Color C14FolkBlack = Rgb(44, 36, 36);
    private static readonly Color C14Bark = Rgb(122, 84, 54);
    private static readonly Color C14Wood = Rgb(196, 150, 96);
    private static readonly Color C14Beech = Rgb(224, 198, 152);
    private static readonly Color C14Gold = Rgb(226, 182, 78);
    private static readonly Color C14Patina = Rgb(104, 160, 128);
    private static readonly Color C14Stone = Rgb(142, 140, 132);
    private static readonly Color C14Moss = Rgb(96, 128, 60);
    private static readonly Color C14Linden = Rgb(232, 208, 124);

    // ------------------------------------------------------------------ helpers

    private static bool C14Same(Color a, Color c) => a.R == c.R && a.G == c.G && a.B == c.B;

    /// <summary>
    /// Changes one colour to another on the shapes and the decals of the bones <paramref name="bones"/> picks (all when
    /// null): a decal's colour, iris, white and pupil alike, which Recolour leaves.
    /// </summary>
    private static void C14Swap(PokeBuilder b, Func<int, bool>? bones, params (Color from, Color to)[] map)
    {
        foreach (var s in b.Sdf.Shapes)
        {
            if (bones != null && !bones(s.Bone)) continue;
            foreach (var (from, to) in map)
                if (C14Same(s.Color, from)) { s.Color = to; break; }
        }
        foreach (var d in b.Model.Decals)
        {
            if (bones != null && !bones(d.Bone)) continue;
            foreach (var (from, to) in map)
            {
                if (C14Same(d.Color, from)) d.Color = to;
                if (d.Iris is { } iris && C14Same(iris, from)) d.Iris = to;
                if (d.White is { } white && C14Same(white, from)) d.White = to;
                if (d.Pupil is { } pupil && C14Same(pupil, from)) d.Pupil = to;
            }
        }
        b.Sdf.Changed();
    }

    /// <summary>The same, on the shapes alone (a balloon's yellow cross is a decal of the colour its hearts are made of).</summary>
    private static void C14SwapShapes(PokeBuilder b, params (Color from, Color to)[] map)
    {
        foreach (var s in b.Sdf.Shapes)
            foreach (var (from, to) in map)
                if (C14Same(s.Color, from)) { s.Color = to; break; }
        b.Sdf.Changed();
    }

    /// <summary>Gives the shapes of one colour another material (paper that glows from within).</summary>
    private static void C14Material(PokeBuilder b, Color color, SurfaceMaterial mat)
    {
        foreach (var s in b.Sdf.Shapes)
            if (s.Op == SdfOp.Add && C14Same(s.Color, color)) s.Material = mat;
        b.Sdf.Changed();
    }

    private static Func<int, bool> C14On(PokeBuilder b, params string[] names)
    {
        var set = new System.Collections.Generic.HashSet<int>();
        foreach (var n in names) set.Add(BoneOf(b, n));
        return set.Contains;
    }

    /// <summary>A sash round a body: a band of red edged with two thin bands of black (painted, so the shape stays).</summary>
    private static void C14Sash(PokeBuilder b, int bone, Vector3 center, float ring, float width, Vector3 rotationDeg = default, float sz = 1f)
    {
        b.PaintTorus(bone, center, ring, width, C14FolkRed, rotationDeg, 1f, sz);
        var up = Vector3.Transform(Vector3.UnitY, Quaternion.CreateFromYawPitchRoll(rotationDeg.Y * Degree, rotationDeg.X * Degree, rotationDeg.Z * Degree));
        foreach (float k in new[] { -1f, 1f })
            b.PaintTorus(bone, center + up * k * width * 0.9f, ring * 0.99f, width * 0.28f, C14FolkBlack, rotationDeg, 1f, sz);
    }

    /// <summary>A little flower painted on a surface: five petals round a heart.</summary>
    private static void C14Flower(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float size, Color petals, Color heart)
    {
        b.Mark(bone, at, facing, size, size, petals, MarkShape.Star5);
        b.Mark(bone, at + Vector3.Normalize(facing) * 0.001f, facing, size * 0.35f, size * 0.35f, heart);
    }

    /// <summary>A fir: a short trunk and two cones of needles on it, the lower the broader.</summary>
    private static void C14Fir(PokeBuilder b, int bone, Vector3 foot, float height)
    {
        var needles = Rgb(46, 92, 54);
        b.Limb(bone, foot - V(0, height * 0.15f, 0), foot + V(0, height * 0.2f, 0), height * 0.09f, height * 0.08f, Rgb(100, 70, 46), blend: 0.008f);
        b.Spike(bone, foot + V(0, height * 0.12f, 0), foot + V(0, height * 0.7f, 0), height * 0.36f, needles, mat: Leaf, blend: 0.006f);
        b.Spike(bone, foot + V(0, height * 0.45f, 0), foot + V(0, height, 0), height * 0.26f, needles, mat: Leaf, blend: 0.006f);
    }

    /// <summary>A wooden horn's mouth at <paramref name="at"/>, opening along <paramref name="dir"/>: a flared lip of wood round a dark throat.</summary>
    private static void C14HornMouth(PokeBuilder b, int bone, Vector3 at, Vector3 dir, float r, Color wood)
    {
        var d = Vector3.Normalize(dir);
        b.Spike(bone, at + d * r * 1.4f, at - d * r * 0.4f, r * 1.25f, wood, mat: Shell, blend: 0.01f);
        b.Torus(bone, at + d * r * 1.35f, r * 1.15f, r * 0.22f, PixelCanvas.Shadow(wood, 0.25f), Euler(d), mat: Shell, blend: 0.006f);
        b.PaintEll(bone, at + d * r * 1.5f, V(r * 0.9f, r * 0.3f, r * 0.9f), Rgb(50, 34, 26), Euler(d));
    }

    // ------------------------------------------------------------------ the Jigglypuff line: the doina's singers

    /// <summary>Carpathian Igglybuff: wool-cream, a red and black swaddling sash round its middle.</summary>
    private static PokeBuilder C14Igglybuff()
    {
        var b = Become(Igglybuff(), "Igglybuff-Carpathia", 0.42f);
        C14Swap(b, null, (Rgb(250, 200, 210), C14Wool), (Rgb(220, 140, 160), Rgb(226, 190, 160)));
        // The sash, low round its round middle, under the smile
        C14Sash(b, Body, V(0, 0.085f, 0), 0.118f, 0.02f);
        // A wisp of whiter wool over the curl
        b.PaintEll(Body, V(0, 0.36f, 0.04f), V(0.05f, 0.04f, 0.05f), Rgb(250, 246, 236));
        return b;
    }

    /// <summary>Carpathian Jigglypuff: wool-cream under an embroidered red headscarf knotted behind, its curl a red and white tassel.</summary>
    private static PokeBuilder C14Jigglypuff()
    {
        var b = Become(Jigglypuff(), "Jigglypuff-Carpathia", 0.5f);
        C14Swap(b, null, (Rgb(252, 198, 212), C14Wool), (Rgb(48, 40, 52), Rgb(84, 58, 46)));
        // The scarf over the crown, a white stitched edge to it, its knot and tails at the back
        b.PaintEll(Body, V(0, 0.37f, -0.02f), V(0.22f, 0.085f, 0.22f), C14FolkRed);
        b.PaintTorus(Body, V(0, 0.296f, -0.01f), 0.155f, 0.007f, Rgb(246, 240, 228));
        foreach (float a in new[] { -0.9f, -0.45f, 0f, 0.45f, 0.9f })
        {
            var dir = V(MathF.Sin(a), 0.62f, MathF.Cos(a));
            b.Mark(Body, Out(V(0, 0.21f, 0), V(0.19f, 0.18f, 0.18f), default, dir), dir, 0.014f, 0.014f, C14FolkBlack, MarkShape.Diamond);
        }
        b.Ell(Body, V(0, 0.3f, -0.17f), V(0.032f, 0.026f, 0.024f), C14FolkRed, blend: 0.012f);
        PokeBuilder.Both(s => Blade(b, Body, V(0.012f * s, 0.29f, -0.18f), V(0.05f * s, 0.2f, -0.2f), 0.022f, C14FolkRed, V(0, 0, -1f), 0.3f));
        // The curl a tassel: red, banded white
        b.PaintEll(Body, V(-0.02f, 0.35f, 0.1f), V(0.05f, 0.05f, 0.06f), C14FolkRed);
        b.PaintEll(Body, V(0.0f, 0.33f, 0.13f), V(0.05f, 0.012f, 0.05f), Rgb(246, 240, 228));
        return b;
    }

    /// <summary>Carpathian Wigglytuff: fleece cream and pale gold, a sleeveless sheepskin vest embroidered red, tassels at its ears' tips.</summary>
    private static PokeBuilder C14Wigglytuff()
    {
        var b = Become(Wigglytuff(), "Wigglytuff-Carpathia", 0.74f);
        var sheepskin = Rgb(176, 128, 82);
        C14Swap(b, null, (Rgb(252, 198, 212), C14Wool), (Rgb(252, 246, 248), Rgb(238, 214, 150)), (Rgb(48, 40, 52), Rgb(96, 70, 52)));
        // The vest: sheepskin round its body from hips to shoulders, open down the front over its pale gold belly,
        // a band of white fleece along each edge of the opening
        b.PaintEll(Body, V(0, 0.25f, -0.01f), V(0.24f, 0.15f, 0.22f), sheepskin);
        b.PaintEll(Body, V(0, 0.24f, 0.12f), V(0.07f, 0.2f, 0.1f), Rgb(238, 214, 150));
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.075f * s, 0.25f, 0.15f), V(0.014f, 0.15f, 0.05f), Rgb(250, 244, 232));
            foreach (float y in new[] { 0.3f, 0.22f, 0.14f })
            {
                var at = On(V(0, 0.29f, 0), V(0.2f, 0.26f, 0.18f), 0.13f * s, y);
                b.Mark(Body, at, Outward(V(0, 0.29f, 0), V(0.2f, 0.26f, 0.18f), at), 0.016f, 0.016f, C14FolkRed, MarkShape.Diamond);
            }
        });
        // A tassel hangs from the tip of each ear
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var tip = V(0.155f * s, 0.665f, -0.01f);
            b.Ell(ear, tip, V(0.022f, 0.022f, 0.02f), C14FolkRed, blend: 0.01f);
            b.Spike(ear, tip + V(0.008f * s, -0.01f, 0), tip + V(0.03f * s, -0.08f, 0), 0.018f, C14FolkRed, blend: 0.006f);
            b.PaintEll(ear, tip + V(0.018f * s, -0.045f, 0), V(0.025f, 0.008f, 0.025f), Rgb(246, 240, 228));
        });
        return b;
    }

    // ------------------------------------------------------------------ the Dunsparce line: the village's balaur

    private static readonly Color C14BalaurGreen = Rgb(118, 150, 78);
    private static readonly Color C14BalaurDark = Rgb(62, 92, 50);
    private static readonly Color C14Horn = Rgb(222, 206, 160);
    private static readonly Color C14Leather = Rgb(150, 108, 74);

    /// <summary>Carpathian Dunsparce: moss-green, pale beneath, leathery dragon's wings and a crest of little horns down its back.</summary>
    private static PokeBuilder C14Dunsparce()
    {
        var b = Become(Dunsparce(), "Dunsparce-Carpathia", 0.6f);
        C14Swap(b, null, (Rgb(242, 224, 110), C14BalaurGreen), (Rgb(70, 150, 196), C14BalaurDark), (Rgb(250, 242, 206), Rgb(232, 224, 184)));
        b.PaintEll(Body, V(0, 0.03f, 0.0f), V(0.1f, 0.06f, 0.15f), Rgb(232, 224, 184));
        // The white fins go: bat-like wings of leather in their place
        b.Sdf.RemoveAll(s => b.Model.Skeleton[s.Bone].Name.StartsWith("wing"));
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            BatWing(b, wing, V(0.05f * s, 0.21f, 0), V(0.12f * s, 0.3f, -0.04f),
                new[] { V(0.21f * s, 0.31f, -0.09f), V(0.2f * s, 0.23f, -0.11f), V(0.12f * s, 0.21f, -0.09f) }, V(0.05f * s, 0.2f, -0.04f), C14Leather, C14BalaurDark, 0.008f);
        });
        // A crest of small horns from its crown down its back
        foreach (float z in new[] { 0.06f, 0.0f, -0.06f, -0.12f })
            b.Spike(Body, V(0, 0.215f, z), V(0, 0.26f, z - 0.025f), 0.016f, C14Horn, mat: Shell, blend: 0.006f);
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Spike(head, V(0.045f * s, 0.255f, 0.13f), V(0.065f * s, 0.3f, 0.1f), 0.016f, C14Horn, mat: Shell, blend: 0.006f));
        return b;
    }

    /// <summary>Carpathian Dudunsparce: the old balaur, moss-green and cream beneath, horns along its back, great leathery wings and a forked drill.</summary>
    private static PokeBuilder C14Dudunsparce()
    {
        var b = Become(Dudunsparce(), "Dudunsparce-Carpathia", 0.9f);
        C14Swap(b, null, (Rgb(242, 224, 110), C14BalaurGreen), (Rgb(70, 150, 196), C14BalaurDark), (Rgb(250, 242, 206), Rgb(232, 224, 184)));
        var c0 = V(0, 0.1f, 0.04f);
        var c1 = V(0.05f, 0.1f, -0.17f);
        int seg = BoneOf(b, "seg1");
        b.PaintEll(Body, c0 + V(0, -0.07f, 0), V(0.09f, 0.05f, 0.11f), Rgb(232, 224, 184));
        b.PaintEll(seg, c1 + V(0, -0.07f, 0), V(0.09f, 0.05f, 0.11f), Rgb(232, 224, 184));
        // Its little fins become great leathery wings
        b.Sdf.RemoveAll(s => b.Model.Skeleton[s.Bone].Name.StartsWith("wing"));
        var c = c0 + V(0, 0.12f, 0.2f);
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            BatWing(b, wing, c + V(0.06f * s, 0.07f, -0.05f), c + V(0.2f * s, 0.18f, -0.12f),
                new[] { c + V(0.33f * s, 0.16f, -0.2f), c + V(0.31f * s, 0.06f, -0.22f), c + V(0.18f * s, 0.04f, -0.16f) }, c + V(0.06f * s, 0.03f, -0.08f), C14Leather, C14BalaurDark, 0.011f);
        });
        // Horns along the back of both segments and two on the crown
        foreach (var (bone, at) in new[] { (Body, c0 + V(0, 0.085f, 0.0f)), (Body, c0 + V(0, 0.085f, -0.07f)), (seg, c1 + V(0, 0.085f, 0.04f)), (seg, c1 + V(0, 0.085f, -0.03f)), (seg, c1 + V(0, 0.08f, -0.09f)) })
            b.Spike(bone, at, at + V(0, 0.05f, -0.025f), 0.018f, C14Horn, mat: Shell, blend: 0.006f);
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Spike(head, c + V(0.05f * s, 0.085f, -0.03f), c + V(0.075f * s, 0.14f, -0.07f), 0.018f, C14Horn, mat: Shell, blend: 0.006f));
        // The drill forks: a second point leans out from the first's root
        int tail = BoneOf(b, "tail");
        var root = c1 + V(0, 0.1f, -0.23f);
        b.Spike(tail, root, c1 + V(0.12f, 0.26f, -0.36f), 0.05f, C14BalaurGreen, mat: Shell);
        foreach (float f in new[] { 0.3f, 0.6f })
            b.PaintTorus(tail, Vector3.Lerp(root, c1 + V(0.12f, 0.26f, -0.36f), f), 0.05f * (1f - f), 0.009f, C14BalaurDark, Euler(V(0.12f, 0.16f, -0.13f)));
        return b;
    }

    // ------------------------------------------------------------------ Smeargle: the monasteries' painter

    /// <summary>Carpathian Smeargle: cream in an ochre smock, gold stars and blue clouds on its back, its brush wet with deep blue and a golden disc of a halo behind its beret.</summary>
    private static PokeBuilder C14Smeargle()
    {
        var b = Become(Smeargle(), "Smeargle-Carpathia", 0.75f);
        var ochre = Rgb(206, 152, 64);
        var blue = Rgb(34, 62, 168);
        C14Swap(b, null, (Rgb(108, 178, 82), blue), (Rgb(230, 222, 198), Rgb(168, 58, 50)));
        // The smock over its body, its hem at its hips
        var bc = V(0, 0.35f, 0);
        var br = V(0.085f, 0.125f, 0.075f);
        b.PaintEll(Body, V(0, 0.33f, 0), V(0.1f, 0.1f, 0.09f), ochre);
        // The fresco on its back: gold stars over blue clouds
        foreach (var (x, y, star) in new[] { (-0.03f, 0.34f, false), (0.035f, 0.3f, false), (0.03f, 0.38f, true), (-0.035f, 0.29f, true), (0f, 0.42f, true) })
        {
            var n = V(x / br.X, (y - bc.Y) / br.Y, -1f);
            if (star) b.Mark(Body, Out(bc, br, default, n), n, 0.016f, 0.016f, C14Gold, MarkShape.Star5);
            else b.Mark(Body, Out(bc, br, default, n), n, 0.022f, 0.014f, Rgb(74, 110, 196));
        }
        // The halo: a golden disc set against the back of its head
        int head = BoneOf(b, "head");
        b.Ell(head, V(0, 0.6f, -0.083f), V(0.088f, 0.088f, 0.01f), C14Gold, mat: Metal, blend: 0.012f);
        b.PaintTorus(head, V(0, 0.6f, -0.092f), 0.07f, 0.006f, Rgb(250, 222, 130), V(90f, 0, 0));
        return b;
    }

    // ------------------------------------------------------------------ the Whismur line: shepherds' flutes and horns

    /// <summary>Carpathian Whismur: wool-cream in curls of fleece, its ears the ends of a shepherd's wooden flute with finger holes.</summary>
    private static PokeBuilder C14Whismur()
    {
        var b = Become(Whismur(), "Whismur-Carpathia", 0.5f);
        C14Swap(b, null, (Rgb(226, 188, 220), C14Wool), (Rgb(246, 214, 110), C14Bark));
        var bc = V(0, 0.18f, 0);
        var br = V(0.13f, 0.14f, 0.12f);
        // Curls of fleece over its back and sides
        for (int i = 0; i < 11; i++)
        {
            float a = 1.2f + i * 0.36f;
            float y = (i % 3 - 1) * 0.35f;
            var at = Out(bc, br, default, V(MathF.Sin(a), y, MathF.Cos(a)));
            b.Ell(Body, at, V(0.03f, 0.03f, 0.03f), Rgb(250, 244, 230), blend: 0.012f);
        }
        // The ears go: two flutes of wood take their place, banded with bark and pierced with finger holes
        int left = BoneOf(b, "earL"), right = BoneOf(b, "earR");
        b.Sdf.RemoveAll(s => s.Bone == left || s.Bone == right);
        b.Model.Decals.RemoveAll(d => d.Bone == left || d.Bone == right);
        PokeBuilder.Both(s =>
        {
            int ear = s < 0 ? left : right;
            var from = V(0.06f * s, 0.29f, 0);
            var to = V(0.22f * s, 0.4f, -0.01f);
            b.Limb(ear, from, to, 0.03f, 0.028f, C14Wood, Shell, 0.015f);
            b.Torus(ear, to, 0.026f, 0.008f, C14Bark, Euler(to - from), mat: Shell, blend: 0.004f);
            b.PaintEll(ear, to + Vector3.Normalize(to - from) * 0.01f, V(0.018f, 0.01f, 0.018f), Rgb(50, 34, 26), Euler(to - from));
            foreach (float f in new[] { 0.35f, 0.7f })
                b.PaintTorus(ear, Vector3.Lerp(from, to, f), 0.03f, 0.006f, C14Bark, Euler(to - from));
            var along = Vector3.Normalize(to - from);
            var up = Vector3.Normalize(V(0, 0.2f, 1f) - along * Vector3.Dot(V(0, 0.2f, 1f), along));
            foreach (float f in new[] { 0.5f, 0.6f, 0.82f })
            {
                var axis = Vector3.Lerp(from, to, f);
                b.Mark(ear, axis + up * 0.0285f, up, 0.008f, 0.008f, Rgb(54, 36, 26));
            }
        });
        return b;
    }

    /// <summary>Carpathian Loudred: bark-brown, its belly cream embroidered red and black, its ears long alphorns of wood bound with bark.</summary>
    private static PokeBuilder C14Loudred()
    {
        var b = Become(Loudred(), "Loudred-Carpathia", 0.75f);
        C14Swap(b, null, (Rgb(150, 136, 200), Rgb(136, 96, 64)), (Rgb(242, 210, 100), C14Wool));
        // A cream belly under the mouth, embroidered
        var bc = V(0, 0.28f, 0);
        var br = V(0.14f, 0.16f, 0.12f);
        b.PaintEll(Body, V(0, 0.16f, 0.08f), V(0.11f, 0.06f, 0.07f), C14Wool);
        foreach (float x in new[] { -0.05f, 0f, 0.05f })
        {
            var at = On(bc, br, x, 0.155f);
            b.Mark(Body, at, Outward(bc, br, at), 0.014f, 0.014f, x == 0f ? C14FolkRed : C14FolkBlack, MarkShape.Diamond);
        }
        // The speaker ears go: long alphorns sweep up from its head
        int left = BoneOf(b, "earL"), right = BoneOf(b, "earR");
        b.Sdf.RemoveAll(s => s.Bone == left || s.Bone == right);
        b.Model.Decals.RemoveAll(d => d.Bone == left || d.Bone == right);
        PokeBuilder.Both(s =>
        {
            int ear = s < 0 ? left : right;
            var path = Smooth(3, V(0.08f * s, 0.4f, 0), V(0.16f * s, 0.46f, -0.02f), V(0.22f * s, 0.56f, -0.04f), V(0.25f * s, 0.66f, -0.05f));
            b.Tube(ear, path, 0.03f, 0.024f, C14Wood, Shell, 0.01f);
            foreach (int i in new[] { 2, 5, 8 })
                if (i < path.Length - 1) b.PaintTorus(ear, path[i], 0.03f - 0.0006f * i, 0.007f, C14Bark, Euler(path[i + 1] - path[i - 1]));
            C14HornMouth(b, ear, path[^1], path[^1] - path[^2], 0.03f, C14Wood);
        });
        return b;
    }

    /// <summary>Carpathian Exploud: bark-brown under a sheepskin mantle, its pipes the flared mouths of wooden horns, a red and black belt round its middle.</summary>
    private static PokeBuilder C14Exploud()
    {
        var b = Become(Exploud(), "Exploud-Carpathia", 1f);
        var horn = Rgb(214, 172, 112);
        C14Swap(b, null, (Rgb(136, 124, 196), Rgb(126, 88, 58)), (Rgb(240, 206, 96), horn));
        // Every pipe's rim flares wider, a horn's mouth
        foreach (var s in b.Sdf.Shapes)
            if (s.Shape == SdfShape.Torus && C14Same(s.Color, horn)) s.Size = new Vector3(s.Size.X * 1.4f, s.Size.Y * 1.25f, s.Size.Z);
        b.Sdf.Changed();
        // The sheepskin mantle over its shoulders
        var fleece = Rgb(238, 226, 200);
        b.Ell(Body, V(0, 0.565f, -0.01f), V(0.165f, 0.055f, 0.13f), fleece, blend: 0.02f);
        FurTufts(b, Body, V(0, 0.565f, -0.01f), V(0.165f, 0.055f, 0.13f), 18, 0.03f, 0.014f, fleece, 1.1f, -0.9f, 0.6f);
        // The belt
        C14Sash(b, Body, V(0, 0.33f, -0.01f), 0.125f, 0.022f);
        return b;
    }

    // ------------------------------------------------------------------ Sableye: from the salt mines

    /// <summary>Carpathian Sableye: pale and crusted with white and pink rock salt, its eyes and chest gem amber salt crystal, salt cubes on its shoulders.</summary>
    private static PokeBuilder C14Sableye()
    {
        var b = Become(Sableye(), "Sableye-Carpathia", 0.65f);
        var salt = Rgb(226, 218, 212);
        var pink = Rgb(232, 178, 176);
        var amber = Rgb(236, 168, 62);
        C14Swap(b, null, (Rgb(110, 84, 168), salt), (Rgb(70, 52, 112), Rgb(120, 92, 88)), (Rgb(176, 226, 236), amber), (Rgb(220, 40, 70), amber), (Rgb(90, 160, 190), Rgb(150, 80, 30)));
        // Pink salt in crusts over its body and head
        int head = BoneOf(b, "head");
        b.PaintEll(Body, V(0.04f, 0.27f, -0.05f), V(0.05f, 0.05f, 0.05f), pink);
        b.PaintEll(Body, V(-0.05f, 0.18f, -0.02f), V(0.04f, 0.04f, 0.05f), pink);
        b.PaintEll(head, V(-0.05f, 0.43f, 0.0f), V(0.05f, 0.04f, 0.05f), pink);
        // Cubes of salt crusted on its shoulders and crown
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Box(arm, V(0.07f * s, 0.315f, 0.015f), V(0.022f, 0.022f, 0.022f), 0.004f, Rgb(244, 238, 234), V(15f, 30f * s, 20f), Shell, 0.006f);
            b.Box(arm, V(0.085f * s, 0.3f, -0.02f), V(0.015f, 0.015f, 0.015f), 0.003f, pink, V(-20f, 10f * s, 35f), Shell, 0.006f);
        });
        b.Box(head, V(0.03f, 0.445f, 0.02f), V(0.014f, 0.014f, 0.014f), 0.003f, Rgb(244, 238, 234), V(10f, 40f, 25f), Shell, 0.006f);
        return b;
    }

    // ------------------------------------------------------------------ Spinda: the hora's dancer

    /// <summary>Carpathian Spinda: cream, its spots red and black embroidery, a red sash round its waist and ribbons at its ears.</summary>
    private static PokeBuilder C14Spinda()
    {
        var b = Become(Spinda(), "Spinda-Carpathia", 0.6f);
        C14Swap(b, null, (Rgb(214, 88, 88), C14FolkRed));
        int head = BoneOf(b, "head");
        var c = V(0, 0.35f, 0.02f);
        var r = V(0.08f, 0.075f, 0.07f);
        // The spots embroidered: black stitches round each patch of red
        foreach (var (x, y) in new[] { (0.05f, 0.4f), (-0.04f, 0.31f) })
        {
            var at = On(c, r, x, y);
            b.Mark(head, at, Outward(c, r, at), 0.018f, 0.018f, C14FolkBlack, MarkShape.Diamond);
        }
        // A sash round its waist, tied, its ends flying
        b.Torus(Body, V(0, 0.14f, 0), 0.052f, 0.012f, C14FolkRed, sz: 0.88f, mat: SurfaceMaterial.Cloth, blend: 0.006f);
        Blade(b, Body, V(0.03f, 0.135f, 0.045f), V(0.08f, 0.07f, 0.08f), 0.018f, C14FolkRed, V(0, 0, 1f), 0.3f);
        // Ribbons tied at its ears
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var ec = V(0.1f * s, 0.46f, -0.01f);
            Blade(b, ear, ec + V(0.02f * s, 0.03f, 0), ec + V(0.07f * s, -0.01f, -0.02f), 0.012f, s < 0 ? Rgb(232, 196, 60) : Rgb(70, 140, 70), V(0, 0, 1f), 0.3f);
        });
        return b;
    }

    // ------------------------------------------------------------------ the Chingling line: wood that knocks

    /// <summary>Carpathian Chingling: a little round body of pale beech wood carved round its rim, a wooden striker hanging from its mouth.</summary>
    private static PokeBuilder C14Chingling()
    {
        var b = Become(Chingling(), "Chingling-Carpathia", 0.46f);
        var lift = Shifted(b, "body", V(0, 0.3f, 0));
        C14Swap(b, null, (Rgb(250, 214, 52), C14Beech), (Rgb(232, 64, 80), C14FolkRed), (Rgb(70, 62, 66), Rgb(70, 46, 30)));
        C14Material(b, C14Beech, SurfaceMaterial.Shell);
        // A carved band round its rim, and notches cut into it
        b.PaintTorus(Body, V(0, 0.24f, 0) + lift, 0.137f, 0.012f, C14Wood);
        foreach (float a in new[] { -1.1f, -0.55f, 0.55f, 1.1f })
        {
            var at = Out(V(0, 0.3f, 0) + lift, V(0.16f, 0.15f, 0.15f), default, V(MathF.Sin(a), -0.4f, MathF.Cos(a)));
            b.Mark(Body, at, Outward(V(0, 0.3f, 0) + lift, V(0.16f, 0.15f, 0.15f), at), 0.016f, 0.012f, C14Bark, MarkShape.Zigzag);
        }
        // The striker: a knob of darker wood on a cord, peeping out under its mouth
        b.Limb(Body, V(0, 0.21f, 0.09f) + lift, V(0, 0.155f, 0.105f) + lift, 0.009f, 0.008f, C14FolkRed, blend: 0.006f);
        b.Ell(Body, V(0, 0.145f, 0.108f) + lift, V(0.022f, 0.022f, 0.022f), C14Bark, mat: Shell, blend: 0.008f);
        return Lift(b);
    }

    /// <summary>Carpathian Chimecho: a hollow body of carved pale wood ringed with spirals, its tail a woven strip of red cloth.</summary>
    private static PokeBuilder C14Chimecho()
    {
        var b = Become(Chimecho(), "Chimecho-Carpathia", 0.6f);
        var lift = Shifted(b, "body", V(0, 0.6f, 0));
        C14Swap(b, null, (Rgb(196, 228, 246), C14Beech), (Rgb(226, 72, 86), C14FolkRed), (Rgb(250, 224, 110), C14Bark), (Rgb(80, 50, 70), Rgb(70, 46, 30)));
        // Carved spirals on its sides and a rope twist round its head
        PokeBuilder.Both(s =>
        {
            var at = V(0.105f * s, 0.6f, 0.035f) + lift;
            b.Mark(Body, at, V(0.9f * s, 0, 0.4f), 0.02f, 0.02f, C14Bark, MarkShape.Ring);
            b.Mark(Body, at + V(0.002f * s, 0, 0.001f), V(0.9f * s, 0, 0.4f), 0.008f, 0.008f, C14Bark);
        });
        foreach (float a in new[] { 0f, 0.6f, 1.2f, 1.8f, 2.4f, 3.0f, 3.6f, 4.2f, 4.8f, 5.4f })
            b.PaintEll(Body, V(MathF.Sin(a) * 0.098f, 0.69f, MathF.Cos(a) * 0.09f) + lift, V(0.014f, 0.008f, 0.014f), C14Wood, V(0, 0, 35f));
        // The cloth woven: black threads across the red strip
        int tail = BoneOf(b, "tail");
        foreach (float y in new[] { 0.22f, 0.17f, 0.12f })
            b.PaintEll(tail, V(0, y, 0.02f) + lift, V(0.08f, 0.006f, 0.03f), C14FolkBlack);
        foreach (float y in new[] { 0.48f, 0.38f, 0.3f })
            b.PaintTorus(tail, V(0, y, 0.01f) + lift, 0.04f, 0.006f, C14Bark);
        return Lift(b);
    }

    // ------------------------------------------------------------------ the Drifloon line: midsummer's paper lanterns

    /// <summary>Carpathian Drifloon: a sky lantern of waxed yellow paper glowing from a flame inside, a flower painted on its back, its strings woven grass.</summary>
    private static PokeBuilder C14Drifloon()
    {
        var b = Become(Drifloon(), "Drifloon-Carpathia", 0.56f);
        var lift = Shifted(b, "body", V(0, 0.62f, 0));
        var lilac = Rgb(190, 160, 236);
        var paper = Rgb(246, 214, 132);
        var grass = Rgb(132, 150, 70);
        C14SwapShapes(b, (lilac, paper), (PixelCanvas.Light1(lilac, 0.3f), Rgb(232, 176, 90)), (Rgb(40, 36, 46), grass), (Rgb(250, 214, 64), Rgb(214, 186, 96)));
        C14Material(b, paper, SurfaceMaterial.Glow);
        // The flame inside, seen through its open foot
        b.Ell(Body, V(0, 0.48f, 0.0f) + lift, V(0.03f, 0.04f, 0.03f), Rgb(250, 140, 50), mat: SurfaceMaterial.Glow, blend: 0.01f);
        // A yellow flower painted on its back, two smaller beside it
        var c = V(0, 0.62f, 0) + lift;
        var r = V(0.15f, 0.16f, 0.15f);
        foreach (var (dir, size) in new[] { (V(0, 0.05f, -1f), 0.032f), (V(0.55f, -0.1f, -0.8f), 0.02f), (V(-0.55f, -0.1f, -0.8f), 0.02f) })
            C14Flower(b, Body, Out(c, r, default, dir), dir, size, Rgb(250, 200, 50), Rgb(200, 100, 40));
        return Lift(b);
    }

    /// <summary>Carpathian Drifblim: a great lantern of amber and rose paper, flowers painted on it, a fire within, its arms ribbons of woven grass and red thread.</summary>
    private static PokeBuilder C14Drifblim()
    {
        var b = Become(Drifblim(), "Drifblim-Carpathia", 0.84f);
        var lift = Shifted(b, "body", V(0, 0.6f, 0));
        var amber = Rgb(232, 156, 72);
        var rose = Rgb(238, 172, 160);
        var grass = Rgb(138, 156, 74);
        C14SwapShapes(b, (Rgb(112, 84, 166), amber), (Rgb(206, 184, 236), rose), (Rgb(232, 70, 92), C14FolkRed), (Rgb(240, 236, 246), grass), (Rgb(250, 214, 64), C14FolkRed));
        C14Material(b, amber, SurfaceMaterial.Glow);
        // The fire within, at its open foot
        b.Ell(Body, V(0, 0.36f, 0.0f) + lift, V(0.05f, 0.05f, 0.05f), Rgb(250, 120, 40), mat: SurfaceMaterial.Glow, blend: 0.012f);
        // Flowers painted round its sides and back
        var c = V(0, 0.6f, 0) + lift;
        var r = V(0.24f, 0.26f, 0.23f);
        foreach (var (dir, size) in new[] { (V(0.85f, 0.1f, 0.2f), 0.035f), (V(-0.85f, 0.1f, 0.2f), 0.035f), (V(0, 0.15f, -1f), 0.045f), (V(0.6f, 0.35f, -0.6f), 0.028f), (V(-0.6f, 0.35f, -0.6f), 0.028f) })
            C14Flower(b, Body, Out(c, r, default, dir), dir, size, Rgb(250, 226, 120), C14FolkRed);
        return Lift(b);
    }

    // ------------------------------------------------------------------ the Bronzor line: the Dacian sun discs

    /// <summary>Carpathian Bronzor: a disc of green-patinated bronze, a sun of rays round its edge, stone dust in its grooves.</summary>
    private static PokeBuilder C14Bronzor()
    {
        var b = Become(Bronzor(), "Bronzor-Carpathia", 0.56f);
        var lift = Shifted(b, "body", V(0, 0.45f, 0));
        var bronze = Rgb(150, 118, 66);
        C14Swap(b, null, (Rgb(90, 186, 220), C14Patina), (Rgb(56, 140, 186), bronze), (Rgb(38, 104, 148), C14Stone));
        var c = V(0, 0.45f, 0) + lift;
        // Twelve rays round its rim, between and over the knobs
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            Blade(b, Body, c + d * 0.24f, c + d * (i % 2 == 0 ? 0.33f : 0.3f), 0.026f, bronze, V(0, 0, 1f), 0.4f, Metal);
        }
        // Stone dust in the grooves of the face
        foreach (var (x, y) in new[] { (0.12f, 0.08f), (-0.1f, -0.1f), (0.02f, -0.15f) })
            b.PaintEll(Body, c + V(x, y, 0.05f), V(0.035f, 0.025f, 0.03f), Rgb(170, 166, 150));
        return Lift(b);
    }

    /// <summary>Carpathian Bronzong: a bell of patinated bronze ringed with carved sun rays and panels of andesite, spirals on its arms, two eyes of gold.</summary>
    private static PokeBuilder C14Bronzong()
    {
        var b = Become(Bronzong(), "Bronzong-Carpathia", 0.86f);
        var lift = Shifted(b, "body", V(0, 0.45f, 0));
        var gold = Rgb(240, 196, 70);
        C14Swap(b, null, (Rgb(96, 158, 160), C14Patina), (Rgb(56, 100, 106), C14Stone), (Rgb(214, 70, 60), gold));
        // A sun of rays round the bell's rim
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f;
            var d = V(MathF.Cos(a), 0, MathF.Sin(a));
            Blade(b, Body, V(0, 0.18f, 0) + lift + d * 0.23f, V(0, 0.18f, 0) + lift + d * (i % 2 == 0 ? 0.32f : 0.29f), 0.028f, Rgb(150, 118, 66), V(0, 1f, 0), 0.35f, Metal);
        }
        // Panels of andesite set into its sides
        foreach (float a in new[] { 1.2f, 1.94f, 4.35f, 5.08f })
            b.PaintEll(Body, V(MathF.Cos(a) * 0.19f, 0.5f, MathF.Sin(a) * 0.19f) + lift, V(0.06f, 0.07f, 0.06f), Rgb(122, 118, 110));
        // Spirals carved into its arms' ends
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var at = V(0.44f * s, 0.62f, 0.055f) + lift;
            b.Mark(arm, at, V(0, 0, 1f), 0.022f, 0.022f, Rgb(70, 110, 86), MarkShape.Ring);
            b.Mark(arm, at + V(0, 0, 0.001f), V(0, 0, 1f), 0.009f, 0.009f, Rgb(70, 110, 86));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ the Golett line: the giants of the tales

    private static readonly Color C14GolemStone = Rgb(146, 144, 136);
    private static readonly Color C14GolemDark = Rgb(98, 96, 92);
    private static readonly Color C14GolemEye = Rgb(244, 214, 116);

    private static void C14GolemColours(PokeBuilder b, Color glow) =>
        C14Swap(b, null, (Rgb(88, 160, 178), C14GolemStone), (Rgb(46, 104, 138), C14GolemDark), (Rgb(170, 222, 220), Rgb(186, 182, 170)),
            (Rgb(140, 100, 64), C14Moss), (glow, C14GolemEye), (Rgb(230, 190, 80), Rgb(206, 170, 80)));

    /// <summary>Carpathian Golett: rough grey mountain stone, moss in its seams, a Dacian spiral on its chest and pale gold eyes.</summary>
    private static PokeBuilder C14Golett()
    {
        var b = Become(Golett(), "Golett-Carpathia", 0.8f);
        C14GolemColours(b, Rgb(252, 232, 120));
        var bc = V(0, 0.24f, 0);
        var br = V(0.14f, 0.15f, 0.12f);
        // Moss in the seams of its stone
        b.PaintEll(Body, V(0.08f, 0.32f, 0.04f), V(0.05f, 0.03f, 0.06f), C14Moss);
        b.PaintEll(Body, V(-0.1f, 0.18f, -0.04f), V(0.05f, 0.03f, 0.06f), C14Moss);
        // Rough lumps of rock on its shoulders
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Ell(arm, V(0.14f * s, 0.33f, -0.01f), V(0.035f, 0.025f, 0.035f), C14GolemDark, V(0, 0, 20f * s), Shell, 0.012f);
        });
        // The spiral carved over its chest, in place of the seal's light
        var at = On(bc, br, 0, bc.Y + 0.01f) + V(0, 0, 0.006f);
        b.Mark(Body, at, V(0, 0, 1f), 0.03f, 0.03f, Rgb(214, 184, 96), MarkShape.Ring);
        b.Mark(Body, at + V(0, 0, 0.001f), V(0, 0, 1f), 0.016f, 0.016f, Rgb(214, 184, 96), MarkShape.Ring);
        return b;
    }

    /// <summary>Carpathian Golurk: an uriaș of weathered mountain stone, grass and little firs on its shoulders, a band of carved spirals across its chest, eyes of gold.</summary>
    private static PokeBuilder C14Golurk()
    {
        var b = Become(Golurk(), "Golurk-Carpathia", 1f);
        C14GolemColours(b, Rgb(252, 226, 100));
        // Grass and firs on its shoulders
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.PaintEll(arm, V(0.17f * s, 0.63f, 0), V(0.1f, 0.03f, 0.08f), Rgb(104, 150, 66));
            C14Fir(b, arm, V(0.22f * s, 0.625f, 0.035f), 0.12f);
            C14Fir(b, arm, V(0.13f * s, 0.625f, -0.035f), 0.09f);
        });
        // Spirals carved along the band across its chest
        var front = V(0, 0.48f, 0.135f);
        foreach (float x in new[] { -0.06f, 0f, 0.06f })
        {
            var at = front + V(x, -x * 0.577f, 0);
            b.Mark(BoneOf(b, "body"), at, V(0, 0, 1f), 0.018f, 0.018f, C14GolemEye, MarkShape.Ring);
        }
        return b;
    }

    // ------------------------------------------------------------------ the Skiddo line: the New Year's goat

    /// <summary>
    /// Carpathian Skiddo and Gogoat: the leaves of their backs a blanket of ribbons, red, yellow, green and blue, little
    /// bells at their ends; Gogoat's ribbons longer and hung with beads and mirrors, its horns wound in red cloth and
    /// tinsel and a wooden clacking jaw on its muzzle.
    /// </summary>
    private static PokeBuilder C14Goat(bool gogoat)
    {
        string name = gogoat ? "Gogoat-Carpathia" : "Skiddo-Carpathia";
        var b = Become(gogoat ? Gogoat() : Skiddo(), name, gogoat ? 1f : 0.75f);
        float k = gogoat ? 1.35f : 1f;
        var leaf = Rgb(70, 160, 80);
        b.Sdf.RemoveAll(s => C14Same(s.Color, leaf));
        var ribbons = new[] { C14FolkRed, Rgb(240, 196, 60), Rgb(70, 150, 70), Rgb(54, 96, 180) };
        var bc = V(0, 0.22f * k, 0);
        var br = V(0.08f, 0.075f, 0.15f) * k;
        int n = gogoat ? 9 : 6;
        for (int i = 0; i < n; i++)
        {
            float t = i / (n - 1f);
            var root = bc + V(0, br.Y * 0.85f, br.Z * (0.9f - 1.6f * t));
            foreach (float side in new[] { -1f, 1f })
            {
                var color = ribbons[(i + (side > 0 ? 2 : 0)) % 4];
                var tip = root + V(0.085f * side, gogoat ? -0.1f : -0.07f, -0.01f) * k;
                Frond(b, Body, root, tip, 0.022f * k, color, V(side, 0.3f, 0), 0.25f, SurfaceMaterial.Cloth);
                if (i % 2 == 0) b.Ell(Body, tip, V(0.013f, 0.013f, 0.013f) * k, C14Gold, mat: Metal, blend: 0.006f);
                else if (gogoat) b.Ell(Body, tip, V(0.016f, 0.016f, 0.005f) * k, Rgb(206, 226, 240), V(0, 90f * side, 0), Metal, 0.006f);
            }
        }
        int tail = BoneOf(b, "tail");
        Frond(b, tail, bc + V(0, 0.03f, -br.Z * 0.95f), bc + V(0, 0.08f, -br.Z * 1.3f), 0.025f * k, C14FolkRed, V(0, 1f, 0.2f), 0.25f, SurfaceMaterial.Cloth);
        int head = BoneOf(b, "head");
        var c = bc + V(0, 0.15f, 0.17f) * k;
        if (gogoat)
        {
            // The blanket's stripes over its shaggy back, beads along its spine
            foreach (var (z, color) in new[] { (0.12f, ribbons[0]), (0.04f, ribbons[1]), (-0.04f, ribbons[2]), (-0.12f, ribbons[3]) })
                b.PaintEll(Body, bc + V(0, 0.06f, z) * k, V(0.13f, 0.06f, 0.03f) * k, color);
            foreach (float z in new[] { 0.1f, 0.0f, -0.1f })
                b.Ell(Body, bc + V(0, br.Y * 0.98f, z * k), V(0.012f, 0.012f, 0.012f) * k, Rgb(250, 240, 220), mat: Shell, blend: 0.006f);
            // Horns wound in red cloth, bound with gold tinsel
            C14Swap(b, C14On(b, "head"), (Rgb(70, 60, 56), C14FolkRed));
            PokeBuilder.Both(s =>
            {
                var path = Smooth(3, c + V(0.025f * s, 0.035f, -0.01f) * k, c + V(0.06f * s, 0.09f, -0.06f) * k, c + V(0.07f * s, 0.06f, -0.13f) * k, c + V(0.06f * s, -0.01f, -0.12f) * k);
                for (int i = 2; i < path.Length - 1; i += 3)
                    b.PaintTorus(head, path[i], 0.017f * k, 0.004f, C14Gold, Euler(path[i + 1] - path[i - 1]));
            });
            // The wooden jaw that clacks
            var wood = Rgb(150, 100, 60);
            b.Box(head, c + V(0, -0.04f, 0.05f) * k, V(0.03f, 0.008f, 0.035f) * k, 0.004f, wood, mat: Shell, blend: 0.006f);
            b.PaintTorus(head, c + V(0, -0.012f, 0.045f) * k, 0.03f * k, 0.004f, Rgb(96, 64, 40), sz: 1.2f);
        }
        return b;
    }

    // ------------------------------------------------------------------ the Noibat line: the caves' echo

    /// <summary>Carpathian Noibat: cave-brown, its great ears ridged inside like a horn's bell and glowing faint gold.</summary>
    private static PokeBuilder C14Noibat()
    {
        var b = Become(Noibat(), "Noibat-Carpathia", 0.5f);
        var lift = Shifted(b, "head", V(0, 0.23f, 0.01f));
        var gold = Rgb(222, 176, 82);
        C14Swap(b, null, (Rgb(156, 116, 196), Rgb(116, 82, 56)), (Rgb(76, 66, 90), Rgb(64, 46, 36)), (Rgb(96, 90, 106), Rgb(104, 86, 70)), (Rgb(60, 50, 80), gold));
        var c = V(0, 0.265f, 0.02f) + lift;
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var ec = c + V(0.075f * s, 0.075f, -0.01f);
            PaintAs(b, ear, ec + V(0, 0, 0.012f), V(0.04f, 0.045f, 0.012f), gold, SurfaceMaterial.Glow, V(0, 0, -30f * s));
            foreach (float ring in new[] { 0.016f, 0.03f })
                b.PaintTorus(ear, ec + V(0, 0, 0.014f), ring, 0.0035f, Rgb(150, 100, 44), V(90f, 0, -30f * s), 0.9f, 1f);
        });
        return Lift(b);
    }

    /// <summary>Carpathian Noivern: dark brown and bronze, its speaker ears the flared mouths of alphorns bound with bark, its wings deep amber with folk-patterned edges.</summary>
    private static PokeBuilder C14Noivern()
    {
        var b = Become(Noivern(), "Noivern-Carpathia", 1f);
        var lift = Shifted(b, "head", V(0, 0.56f, 0.02f));
        var amber = Rgb(206, 132, 52);
        var ears = C14On(b, "earL", "earR");
        C14Swap(b, ears, (Rgb(56, 52, 66), C14Wood), (Rgb(70, 210, 150), C14Bark));
        C14Swap(b, null, (Rgb(56, 52, 66), Rgb(80, 56, 40)), (Rgb(150, 100, 200), amber), (Rgb(236, 236, 240), Rgb(232, 216, 184)));
        var c = V(0, 0.62f, 0.05f) + lift;
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var ec = c + V(0.1f * s, 0.05f, -0.01f);
            // A flared lip round the horn's mouth, and bark bound round its stalk
            b.Torus(ear, ec + V(0.03f * s, 0, 0), 0.05f, 0.011f, C14Wood, V(0, 0, 90f + 20f * s), 1.2f, 1f, Shell, 0.008f);
            b.PaintTorus(ear, Vector3.Lerp(c + V(0.04f * s, 0.03f, -0.01f), ec, 0.5f), 0.03f, 0.007f, C14Bark, V(0, 0, 90f + 20f * s));
            // The wings' edges stitched with red and black
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var wrist = V(0.3f * s, 0.66f, -0.04f) + lift;
            var tips = new[] { V(0.56f * s, 0.56f, -0.06f) + lift, V(0.5f * s, 0.36f, -0.06f) + lift, V(0.34f * s, 0.24f, -0.05f) + lift };
            foreach (var t in tips)
            {
                b.PaintEll(wing, Vector3.Lerp(wrist, t, 0.9f), V(0.03f, 0.03f, 0.03f), C14FolkRed);
                b.PaintEll(wing, Vector3.Lerp(wrist, t, 0.72f), V(0.02f, 0.02f, 0.03f), C14FolkBlack);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ the Sinistea line: linden tea in village clay

    private static readonly Color C14Glaze = Rgb(230, 208, 160);
    private static readonly Color C14GlazeBrown = Rgb(126, 76, 42);
    private static readonly Color C14GlazeGreen = Rgb(74, 126, 66);

    /// <summary>A linden flower: five pale petals round a heart, a pale green bract under it.</summary>
    private static void C14LindenFlower(PokeBuilder b, int bone, Vector3 at, Vector3 up, float size)
    {
        var u = Vector3.Normalize(up);
        b.Ell(bone, at - u * size * 0.2f + V(size * 0.6f, 0, 0), V(size * 1.1f, size * 0.25f, size * 0.5f), Rgb(170, 196, 110), mat: Leaf, blend: 0.004f);
        b.Ell(bone, at, V(size, size * 0.3f, size), Rgb(250, 238, 170), mat: Leaf, blend: 0.004f);
        b.Ell(bone, at + u * size * 0.2f, V(size * 0.35f, size * 0.25f, size * 0.35f), Rgb(226, 170, 60), blend: 0.003f);
    }

    /// <summary>Carpathian Sinistea: a glazed earthenware cup painted with brown and green swirls, pale gold linden tea in it with a flower floating.</summary>
    private static PokeBuilder C14Sinistea()
    {
        var b = Become(Sinistea(), "Sinistea-Carpathia", 0.35f);
        // The handle first, while the tea's colour still names it
        foreach (var s in b.Sdf.Shapes)
            if (s.Shape == SdfShape.Torus && C14Same(s.Color, TeaPurple)) s.Color = C14GlazeBrown;
        C14Swap(b, null, (Porcelain, C14Glaze), (TeaPurple, C14Linden), (TeaGold, C14GlazeBrown), (Rgb(120, 190, 190), C14GlazeGreen));
        foreach (var d in b.Model.Decals)
            if (d.IsEye) d.Pupil = Rgb(92, 58, 34);
        // Brown swirls between the green
        var c = V(0, 0.14f, 0);
        var r = V(0.105f, 0.09f, 0.105f);
        foreach (float a in new[] { 2.8f, 3.8f, 4.9f })
        {
            var at = Out(c, r, default, V(MathF.Sin(a), 0.15f, MathF.Cos(a)));
            b.PaintTorus(Body, at, 0.012f, 0.003f, C14GlazeBrown, Euler(Outward(c, r, at)));
        }
        b.PaintTorus(Body, V(0, 0.07f, 0), 0.085f, 0.005f, C14GlazeBrown);
        // A linden flower floating on the tea
        C14LindenFlower(b, Body, V(0.025f, 0.146f, -0.02f), V(0, 1f, 0), 0.016f);
        return b;
    }

    /// <summary>Carpathian Polteageist: a teapot of cream clay glazed with brown and green spirals and a rooster, pale gold linden tea rising out of it, linden flowers in the steam.</summary>
    private static PokeBuilder C14Polteageist()
    {
        var b = Become(Polteageist(), "Polteageist-Carpathia", 0.55f);
        C14Swap(b, null, (Porcelain, C14Glaze), (TeaPurple, C14Linden), (TeaGold, C14GlazeBrown), (Rgb(120, 190, 190), C14GlazeGreen), (Rgb(244, 246, 246), C14Glaze), (Rgb(60, 30, 80), Rgb(92, 58, 34)));
        var c = V(0, 0.16f, 0);
        var r = V(0.13f, 0.12f, 0.13f);
        // Brown spirals between the green, a band round its foot
        foreach (float a in new[] { 1.05f, 2.15f, 3.25f, 5.45f })
        {
            var at = Out(c, r, default, V(MathF.Sin(a), -0.15f, MathF.Cos(a)));
            b.PaintTorus(Body, at, 0.016f, 0.004f, C14GlazeBrown, Euler(Outward(c, r, at)));
        }
        // The rooster on its front: a body, a red comb and a tail of strokes
        var front = Out(c, r, default, V(0.25f, -0.25f, 1f));
        var n = Outward(c, r, front);
        b.Mark(Body, front, n, 0.02f, 0.016f, C14GlazeBrown);
        b.Mark(Body, front + V(0.016f, 0.02f, 0) + n * 0.001f, n, 0.009f, 0.009f, C14FolkRed, MarkShape.Triangle);
        b.Mark(Body, front + V(-0.024f, 0.012f, 0) + n * 0.001f, n, 0.014f, 0.012f, C14GlazeGreen, MarkShape.Star);
        // Linden flowers drifting in the tea that rises out of it
        var gc = c + V(0, 0.13f, 0);
        var gr = V(0.075f, 0.06f, 0.075f);
        foreach (var dir in new[] { V(-0.8f, 0.2f, 0.4f), V(0.7f, -0.1f, -0.6f) })
        {
            var at = Out(gc, gr, default, dir);
            C14LindenFlower(b, Body, at, Outward(gc, gr, at), 0.014f);
        }
        return b;
    }
}
