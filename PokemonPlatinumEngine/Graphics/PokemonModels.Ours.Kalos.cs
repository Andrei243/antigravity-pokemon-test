using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 · D14), batch 6: Kalos's, in our numbers' order
// (2000 + the forebear's National number), with Ecosentinel of its supplement. Each is sculpted from its forebear's own
// sculpt as the look of docs/plans/D14-proposals-kalos.md and the user's reference pictures
// (docs/art/reference/gemini/kalos) say: the forebear's build is taken whole (renamed, stretched, its colours changed,
// a part taken away) and the picture's new features are added to it, with the tools of batches 1 and 5
// (PokemonModels.Ours.Kanto.cs, PokemonModels.Ours.Unova.cs). The forebears' own sculpts are never touched.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ 2660 Excavabbit, from Diggersby

    /// <summary>
    /// Excavabbit: Diggersby grown heavier, its belly a round dark brown like a digger's overalls with straps over the
    /// shoulders, its arms thick, a pale band with a lamp across its brow, and its ears risen into two great arches
    /// whose ends are broad dark scoops with three long iron nails each.
    /// </summary>
    private static PokeBuilder Excavabbit()
    {
        var b = Become(Diggersby(), "Excavabbit", 0.94f);
        int head = BoneOf(b, "head");
        var gray = Rgb(150, 146, 140);
        var dark = Rgb(84, 70, 60);
        var scoop = Rgb(70, 60, 54);
        Recolour(b, (Rgb(176, 176, 180), gray), (Rgb(122, 90, 66), dark), (Rgb(240, 210, 90), Rgb(96, 80, 68)));
        // The ears go: they rise again as arches
        foreach (var ear in new[] { BoneOf(b, "earL"), BoneOf(b, "earR") })
            b.Sdf.RemoveAll(s => s.Bone == ear);
        var bc = V(0, 0.22f, 0);
        var c = V(0, 0.38f, 0.03f);
        // A rounder belly and thick arms
        b.Ell(Body, bc + V(0, -0.02f, 0.03f), V(0.125f, 0.12f, 0.1f), dark, blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var sh = V(0.1f * s, 0.29f, 0.02f);
            var hand = V(0.15f * s, 0.15f, 0.07f);
            b.Limb(arm, sh, Vector3.Lerp(sh, hand, 0.5f) + V(0.01f * s, 0, 0), 0.04f, 0.034f, gray);
            b.Limb(arm, Vector3.Lerp(sh, hand, 0.5f) + V(0.01f * s, 0, 0), hand, 0.034f, 0.03f, gray);
            b.Ell(arm, hand + V(0, -0.015f, 0.01f), V(0.03f, 0.03f, 0.03f), gray);
            Claws(b, arm, hand + V(0, -0.03f, 0.03f), V(0.012f, 0, 0), V(0, -1f, 0.4f), 0.03f, 0.008f);
            // The overalls' straps over each shoulder
            b.PaintEll(Body, bc + V(0.05f * s, 0.09f, 0.07f), V(0.016f, 0.06f, 0.04f), dark);
        });
        // The miner's band across its brow, a round lamp at its middle
        b.PaintTorus(head, c + V(0, 0.022f, -0.004f), 0.062f, 0.012f, Rgb(232, 222, 190), V(-12f, 0, 0), sx: 1.02f, sz: 0.98f);
        b.Ell(head, c + V(0, 0.026f, 0.058f), V(0.014f, 0.014f, 0.008f), Rgb(255, 238, 150), V(-12f, 0, 0), Glow, 0.004f);
        PokeBuilder.Both(s =>
        {
            // Each ear rises high over the head and arches out and down to its scoop
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var path = Smooth(3, c + V(0.03f * s, 0.04f, -0.01f), c + V(0.06f * s, 0.18f, -0.03f), c + V(0.15f * s, 0.27f, -0.04f), c + V(0.25f * s, 0.2f, -0.02f), c + V(0.29f * s, 0.06f, 0.0f));
            b.Tube(ear, path, 0.026f, 0.038f, gray, blend: 0f);
            b.PaintEll(ear, path[^2], V(0.05f, 0.05f, 0.05f), dark);
            var end = path[^1];
            b.Ell(ear, end + V(0, -0.03f, 0.01f), V(0.055f, 0.06f, 0.05f), scoop);
            b.Cut(ear, end + V(-0.01f * s, -0.06f, 0.05f), V(0.04f, 0.045f, 0.03f));
            for (int i = -1; i <= 1; i++)
                b.Spike(ear, end + V(0.02f * i, -0.07f, 0.03f), end + V(0.025f * i, -0.15f, 0.05f), 0.014f, Rgb(80, 84, 92), mat: Metal, blend: 0.006f);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2668 Blazemonarch, from Pyroar

    /// <summary>
    /// Blazemonarch: Pyroar larger and longer-legged, the male's mane grown into a crown of flame points standing up
    /// round its head and running on down its neck and back, its tail's tuft a second small mane of flame and gold
    /// bands round its forelegs. The female keeps her long mane, grown on down her back like a cape.
    /// </summary>
    /// <param name="female">The female's look: Pyroar's female's long lock, grown into a cape of flame.</param>
    private static PokeBuilder Blazemonarch(bool female = false)
    {
        var b = Become(PyroarBuild(female ? 1 : 0), "Blazemonarch", 1f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var tan = Rgb(214, 172, 116);
        var red = Rgb(190, 44, 36);
        var gold = Rgb(250, 196, 60);
        Recolour(b, (Rgb(214, 180, 130), tan), (Rgb(90, 66, 52), Rgb(196, 154, 104)));
        // The tail's tuft goes: a mane of flame takes its place
        b.Sdf.RemoveAll(s => s.Bone == tail && s.Shape == SdfShape.Ellipsoid && s.Op == SdfOp.Add);
        var bc = V(0, 0.3f, -0.02f);
        var c = bc + V(0, 0.16f, 0.23f);
        {
            // The crown: tall points of flame standing up round the top of the head (shorter on the female, under her cape)
            float crown = female ? 0.6f : 1f;
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 6f - 0.5f) * 120f * Degree;
                var d = V(MathF.Sin(a), MathF.Cos(a), 0);
                var root = c + V(0, 0.02f, -0.03f) + d * 0.04f;
                FlameTongue(b, head, root, root + Vector3.Normalize(d * 0.6f + V(0, 1f, -0.15f)) * (0.17f - 0.04f * MathF.Abs(MathF.Sin(a))) * crown, 0.03f, red, gold);
            }
        }
        // The mane runs on down the neck and the back in tongues of flame
        for (int i = 0; i < 6; i++)
        {
            float t = i / 5f;
            var root = bc + V(0, 0.09f - 0.02f * t, 0.12f - 0.26f * t);
            PokeBuilder.Both(s => FlameTongue(b, Body, root + V(0.03f * s, 0, 0), root + V(0.09f * s, 0.06f - 0.03f * t, -0.08f), female ? 0.032f : 0.028f, red, gold));
            FlameTongue(b, Body, root, root + V(0, 0.1f - 0.04f * t, -0.09f), 0.03f, red, gold);
        }
        // The tail's mane
        var tip = bc + V(0, 0.2f, -0.24f);
        foreach (var d in new[] { V(0, 1f, -0.3f), V(0.5f, 0.8f, -0.4f), V(-0.5f, 0.8f, -0.4f), V(0, 0.6f, -0.9f) })
            FlameTongue(b, tail, tip, tip + Vector3.Normalize(d) * 0.1f, 0.026f, red, gold);
        // Gold bands round the forelegs
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Torus(leg, V(0.0735f * s, 0.085f, 0.158f), 0.024f, 0.009f, gold, mat: Metal, blend: 0.004f);
            b.Torus(leg, V(0.0735f * s, 0.06f, 0.16f), 0.023f, 0.008f, gold, mat: Metal, blend: 0.004f);
        });
        Stretch(b, V(1.1f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2673 Alpicorn, from Gogoat

    /// <summary>
    /// Alpicorn: Gogoat grown larger and darker, mossy green over brown, its horns grown into great spiralled ibex horns
    /// ridged and green with moss, the leaves of its back a saddle-cloth hanging down its flanks, a ruff of moss at its
    /// neck and its hooves rimmed with grey stone.
    /// </summary>
    private static PokeBuilder Alpicorn()
    {
        float k = 1.35f;
        var b = Become(Gogoat(), "Alpicorn", 1f);
        int head = BoneOf(b, "head");
        var moss = Rgb(84, 112, 70);
        var brown = Rgb(118, 92, 66);
        var horn = Rgb(92, 92, 82);
        var leaf = Rgb(80, 150, 70);
        var stone = Rgb(136, 136, 128);
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, Rgb(70, 60, 56)));
        Recolour(b, (Rgb(176, 150, 116), moss), (Rgb(110, 90, 70), brown), (Rgb(236, 236, 232), Rgb(150, 160, 140)), (Rgb(230, 120, 70), stone));
        var bc = V(0, 0.22f * k, 0);
        var br = V(0.08f, 0.075f, 0.15f) * k;
        var c = bc + V(0, 0.15f, 0.17f) * k;
        // Brown patches in the coat
        Patches(b, Body, brown, (bc + V(0.09f, 0, 0.05f), 0.05f), (bc + V(-0.09f, 0.0f, -0.08f), 0.05f), (bc + V(0.08f, -0.02f, -0.12f), 0.04f));
        // A saddle-cloth of broad leaves over the back and hanging down the flanks
        for (int i = 0; i < 7; i++)
        {
            float t = i / 6f;
            var root = bc + V(0, br.Y * 0.85f, br.Z * (0.6f - 1.4f * t));
            PokeBuilder.Both(s =>
            {
                Frond(b, Body, root + V(0.02f * s, 0, 0), root + V(0.15f * s, -0.07f, -0.02f), 0.04f, i % 2 == 0 ? leaf : Rgb(64, 132, 60), V(s * 0.6f, 1f, 0), 0.22f, Leaf);
                Frond(b, Body, root + V(0.08f * s, -0.05f, 0.01f), root + V(0.15f * s, -0.17f, 0.0f), 0.03f, Rgb(70, 140, 64), V(s, 0.3f, 0), 0.22f, Leaf);
            });
        }
        // A ruff of moss round the neck
        b.Ell(Body, bc + V(0, 0.05f, 0.12f) * k, V(0.075f, 0.05f, 0.05f) * k, Rgb(110, 150, 70), mat: Leaf, blend: 0.02f);
        // The horns: great spirals rising back over the head and curling round, ridged and mossy
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, c + V(0.022f * s, 0.035f, -0.01f) * k, c + V(0.04f * s, 0.1f, -0.03f) * k, c + V(0.06f * s, 0.16f, -0.1f) * k,
                c + V(0.075f * s, 0.15f, -0.19f) * k, c + V(0.085f * s, 0.07f, -0.22f) * k, c + V(0.095f * s, 0.02f, -0.16f) * k, c + V(0.1f * s, 0.05f, -0.11f) * k);
            b.Tube(head, path, 0.026f * k, 0.008f * k, horn, Shell, 0f);
            for (int i = 2; i < path.Length - 3; i += 2)
                b.PaintTorus(head, path[i], 0.026f * k * (1f - i / (float)path.Length) + 0.002f, 0.004f, Rgb(64, 64, 58), Euler(path[i + 1] - path[i - 1]));
            foreach (int i in new[] { 3, 7, 11 })
                b.PaintEll(head, path[i] + V(0, 0.015f, 0), V(0.02f, 0.012f, 0.02f), Rgb(110, 150, 70));
        });
        // Stone rims round the hooves
        foreach (var z in new[] { 0.12f * k, -0.13f * k })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, (z > 0 ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                b.Torus(leg, V(0.06f * k * 1.05f * s, 0.06f, z + 0.03f), 0.04f, 0.014f, stone, mat: Shell, blend: 0.006f);
            });
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2675 Bambrawler, from Pangoro

    /// <summary>
    /// Bambrawler: Pangoro grown larger, its white mane thicker over its shoulders, its black fur grown into a long coat
    /// falling to its knees, scars over its eye, bandages round its forearms and a whole stalk of bamboo carried across
    /// its shoulders like a staff.
    /// </summary>
    private static PokeBuilder Bambrawler()
    {
        var b = Become(Pangoro(), "Bambrawler", 1f);
        int head = BoneOf(b, "head");
        var black = Rgb(56, 56, 64);
        var cream = Rgb(244, 236, 214);
        var bamboo = Rgb(110, 160, 70);
        var node = Rgb(84, 130, 54);
        var wrap = Rgb(96, 88, 76);
        var bc = V(0, 0.34f, 0);
        var c = bc + V(0, 0.26f, 0.04f);
        // A thicker mane over the shoulders
        for (int i = 0; i < 16; i++)
        {
            float a = (i / 15f - 0.5f) * 300f * Degree;
            var d = V(MathF.Sin(a), 0.2f, MathF.Cos(a) * 0.8f);
            var root = bc + V(0, 0.14f, 0.01f) + d * 0.1f;
            b.Spike(Body, root, root + Vector3.Normalize(d + V(0, -0.5f, 0)) * 0.09f, 0.03f, cream, 0.6f);
        }
        // The long coat: panels falling from the shoulders to the knees, open in front
        PokeBuilder.Both(s =>
        {
            Blade(b, Body, bc + V(0.12f * s, 0.12f, 0.02f), bc + V(0.17f * s, -0.26f, 0.06f), 0.08f, black, V(s, 0, 0.6f), 0.2f);
            Blade(b, Body, bc + V(0.1f * s, 0.12f, -0.07f), bc + V(0.15f * s, -0.28f, -0.13f), 0.09f, black, V(s * 0.4f, 0, -1f), 0.2f);
            // Bandages round each forearm
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var sh = V(0.15f * s, 0.46f, 0.02f);
            var hand = V(0.27f * s, 0.36f, 0.12f);
            foreach (float t in new[] { 0.6f, 0.72f, 0.84f })
                b.Torus(arm, Vector3.Lerp(sh, hand, t), 0.052f, 0.012f, wrap, Euler(hand - sh), blend: 0.006f);
        });
        // Scars over its right eye
        foreach (float dx in new[] { -0.01f, 0.01f })
            b.PaintEll(head, c + V(0.04f + dx, 0.0f, 0.085f), V(0.003f, 0.04f, 0.02f), Rgb(150, 70, 70), V(0, 0, -20f), 0.004f);
        // The bamboo staff across the shoulders, held up by its right hand
        var from = bc + V(-0.3f, 0.12f, 0.1f);
        var to = bc + V(0.42f, 0.36f, -0.12f);
        b.Limb(Body, from, to, 0.024f, 0.024f, bamboo, Leaf, 0.006f);
        for (int i = 1; i < 6; i++)
            b.Torus(Body, Vector3.Lerp(from, to, i / 6f), 0.025f, 0.006f, node, Euler(to - from), mat: Leaf, blend: 0.004f);
        foreach (var (t, d) in new[] { (0.12f, V(-0.05f, -0.06f, 0.03f)), (0.85f, V(0.05f, 0.07f, 0.0f)), (0.95f, V(0.07f, -0.02f, -0.04f)) })
        {
            var at = Vector3.Lerp(from, to, t);
            Frond(b, Body, at, at + d, 0.012f, Rgb(100, 170, 80), V(0, 0, 1f), 0.3f, Leaf);
        }
        Stretch(b, V(1.1f, 1.12f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2676 Majesthound, from Furfrou

    /// <summary>
    /// Majesthound: Furfrou in its natural coat grown tall and long-legged, its skin pale, its fur a mantle over its
    /// shoulders that trails behind it to the ground, a plume of fur standing on its head like a guard's, pale gold eyes
    /// and a long plumed tail.
    /// </summary>
    private static PokeBuilder Majesthound()
    {
        var b = Become(Furfrou(), "Majesthound", 1f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var fur = Rgb(244, 238, 222);
        var cream = Rgb(232, 222, 196);
        Recolour(b, (Rgb(80, 72, 70), Rgb(214, 210, 206)), (Rgb(70, 160, 220), Rgb(226, 200, 110)), (Rgb(244, 242, 236), fur));
        b.Sdf.RemoveAll(s => s.Bone == tail && s.Shape == SdfShape.Ellipsoid && s.Op == SdfOp.Add);
        var bc = V(0, 0.3f, 0);
        var c = bc + V(0, 0.2f, 0.2f);
        // The plume: a crest of fur standing up on its head and sweeping back
        foreach (var (x, h, back) in new[] { (0f, 0.13f, -0.04f), (-0.02f, 0.11f, -0.07f), (0.02f, 0.11f, -0.07f), (0f, 0.1f, -0.1f), (-0.03f, 0.08f, -0.02f), (0.03f, 0.08f, -0.02f) })
            b.Spike(head, c + V(x, 0.05f, -0.01f), c + V(x * 1.5f, 0.05f + h, back), 0.026f, fur, 0.55f, blend: 0.015f);
        // Long locks down the sides of its head
        PokeBuilder.Both(s => b.Spike(head, c + V(0.05f * s, 0.0f, -0.02f), c + V(0.07f * s, -0.1f, -0.04f), 0.025f, fur, 0.5f));
        // The mantle: a mass of fur over the chest and shoulders, falling in long trails down its flanks to the ground behind
        b.Ell(Body, bc + V(0, 0.05f, 0.1f), V(0.09f, 0.1f, 0.08f), fur, blend: 0.03f);
        FurTufts(b, Body, bc + V(0, 0.03f, 0.1f), V(0.09f, 0.1f, 0.08f), 18, 0.05f, 0.022f, cream, 1.1f, -0.9f, 0.9f);
        PokeBuilder.Both(s =>
        {
            foreach (var (z, len) in new[] { (0.02f, 0.2f), (-0.08f, 0.24f), (-0.16f, 0.26f) })
            {
                var root = bc + V(0.07f * s, 0.06f, z);
                Ribbon(b, Body, Smooth(2, root, root + V(0.015f * s, -0.08f, -0.06f), root + V(0.01f * s, -len, -0.16f)), 0.045f, fur, V(s, 0.1f, 0), 0.012f);
            }
        });
        // A long plumed tail
        var tp = Smooth(3, bc + V(0, 0.18f, -0.17f * 1.1f), bc + V(0, 0.22f, -0.28f), bc + V(0, 0.14f, -0.36f), bc + V(0, 0.04f, -0.38f));
        b.Tube(tail, tp, 0.04f, 0.022f, fur, blend: 0.01f);
        FurTufts(b, tail, tp[4], V(0.04f, 0.05f, 0.05f), 10, 0.04f, 0.018f, fur, 1.1f, -1f, 0.6f);
        Stretch(b, V(1f, 1.18f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2678 Occulynx, from Meowstic

    /// <summary>
    /// Occulynx: Meowstic taller and slimmer, its ears grown long and broad into a hood it holds down round itself,
    /// their insides dark and set with glowing blue eyes, a third eye on its brow and its two tails grown into four
    /// long ones, dark at their ends and marked in glowing blue. The male is navy marked white, the female (the
    /// picture's) white marked navy.
    /// </summary>
    /// <param name="female">The female's look: Meowstic's female's white coat, its navy turned to the hood's insides.</param>
    private static PokeBuilder Occulynx(bool female = false)
    {
        var b = Become(MeowsticBuild(female, false), "Occulynx", 0.86f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var navy = Rgb(44, 54, 110);
        var white = Rgb(236, 238, 248);
        var main = female ? white : Rgb(50, 70, 150);
        var inner = female ? navy : Rgb(32, 40, 84);
        var glow = Rgb(96, 220, 250);
        foreach (var ear in new[] { BoneOf(b, "earL"), BoneOf(b, "earR") })
            b.Sdf.RemoveAll(s => s.Bone == ear);
        b.Sdf.RemoveAll(s => s.Bone == tail);
        var c = V(0, 0.35f, 0.015f);
        var r = V(0.05f, 0.045f, 0.045f);
        // The hood: a cap over the head and the two ears falling from it down beside the body to its knees
        b.Ell(head, c + V(0, 0.022f, -0.006f), V(0.056f, 0.044f, 0.05f), main, blend: 0.01f);
        b.Cut(head, c + V(0, -0.005f, 0.06f), V(0.045f, 0.04f, 0.03f));
        b.Ell(head, c, r * 0.97f, main, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var path = Smooth(3, c + V(0.03f * s, 0.05f, -0.01f), c + V(0.065f * s, 0.02f, 0.0f), c + V(0.085f * s, -0.08f, 0.01f), c + V(0.095f * s, -0.2f, 0.02f), c + V(0.09f * s, -0.27f, 0.03f));
            Ribbon(b, ear, path, 0.042f, main, V(s, 0, 0.35f), 0.012f);
            // The dark inside, a little within, and the eyes set in it
            var inside = path.Select(p => p + V(-0.007f * s, 0, 0.002f)).ToArray();
            Ribbon(b, ear, inside, 0.034f, inner, V(s, 0, 0.35f), 0.008f);
            foreach (int i in new[] { 4, 7, 10 })
            {
                var at = inside[Math.Min(i, inside.Length - 1)] + V(-0.005f * s, 0, 0);
                b.Ell(ear, at, V(0.004f, 0.011f, 0.007f), glow, mat: Glow, blend: 0.002f);
                b.Ell(ear, at + V(-0.002f * s, 0, 0), V(0.003f, 0.005f, 0.004f), inner, mat: Glow, blend: 0.001f);
            }
        });
        // Its paws drawn in to the chest, clutching the hood
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Limb(arm, V(0.035f * s, 0.27f, 0.01f), V(0.02f * s, 0.25f, 0.05f), 0.013f, 0.012f, main);
            b.Ell(arm, V(0.018f * s, 0.25f, 0.055f), V(0.013f, 0.012f, 0.012f), main, blend: 0.006f);
        });
        // The third eye on its brow
        var brow = On(c, r, 0, c.Y + 0.01f);
        b.Mark(head, brow, Outward(c, r, brow), 0.006f, 0.008f, glow, MarkShape.Ring);
        // Four long tails, white with dark ends marked in blue
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * 0.04f;
            float lift = 0.05f + 0.05f * (1.5f - MathF.Abs(i - 1.5f));
            var tp = Smooth(3, V(0, 0.18f, -0.03f), V(x * 0.4f, 0.12f, -0.08f), V(x * 0.8f, 0.14f + lift * 0.4f, -0.15f), V(x * 1.1f, 0.2f + lift * 0.8f, -0.18f));
            b.Tube(tail, tp, 0.01f, 0.016f, main, blend: 0.006f);
            b.PaintEll(tail, tp[^1], V(0.03f, 0.04f, 0.03f), inner);
            b.Ell(tail, tp[^2] + V(0, 0.01f, -0.008f), V(0.006f, 0.012f, 0.006f), glow, mat: Glow, blend: 0.003f);
        }
        Stretch(b, V(0.92f, 1.22f, 0.95f));
        return b;
    }

    // ------------------------------------------------------------------ 2683 Perfumelle, from Aromatisse

    /// <summary>
    /// Perfumelle: Aromatisse grown tall, its skirt of down a body of rose feathers, a ruff of violet down at its neck,
    /// a pale curved beak on its masked face, its horns grown into a powdered wig of three tiers with pink feathers in
    /// it, wings at its sides and a long train of violet plumes behind.
    /// </summary>
    private static PokeBuilder Perfumelle()
    {
        var b = Become(Aromatisse(), "Perfumelle", 0.92f);
        int head = BoneOf(b, "head");
        var rose = Rgb(206, 92, 140);
        var violet = Rgb(126, 92, 170);
        var plume = Rgb(176, 130, 210);
        var wig = Rgb(250, 236, 240);
        var pink = Rgb(240, 160, 190);
        Recolour(b, (Rgb(236, 150, 170), rose), (Rgb(160, 90, 170), Rgb(196, 84, 130)));
        // The horns go: the wig is where they were
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone);
        var c = V(0, 0.34f, 0.02f);
        var r = V(0.06f, 0.055f, 0.055f);
        // The wig: three tiers of white curls, each smaller, and pink feathers set in it
        foreach (var (y, k) in new[] { (0.05f, 1f), (0.12f, 0.82f), (0.18f, 0.62f) })
        {
            b.Ell(head, c + V(0, y, -0.015f), V(0.075f, 0.045f, 0.07f) * k, wig, blend: 0.02f);
            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * MathF.Tau;
                b.Ell(head, c + V(MathF.Sin(a) * 0.065f * k, y - 0.005f, MathF.Cos(a) * 0.06f * k - 0.015f), V(0.026f, 0.026f, 0.026f) * k, wig, blend: 0.012f);
            }
        }
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.06f * s, -0.01f, -0.01f), V(0.022f, 0.03f, 0.022f), wig, blend: 0.012f);
            foreach (var y in new[] { 0.06f, 0.14f })
                Frond(b, head, c + V(0.035f * s, y, 0.02f), c + V(0.09f * s, y + 0.04f, 0.04f), 0.012f, pink, V(0, 0, 1f), 0.3f);
        });
        // The beak, long and curving down
        b.Tube(head, Smooth(3, c + V(0, -0.005f, 0.045f), c + V(0, -0.012f, 0.09f), c + V(0, -0.04f, 0.11f)), 0.013f, 0.004f, Rgb(244, 214, 220), Shell, 0.004f);
        // A ruff of violet down at the neck
        FurTufts(b, Body, V(0, 0.29f, 0.0f), V(0.06f, 0.025f, 0.055f), 16, 0.035f, 0.016f, violet, 1.1f, -1f, 0.6f);
        // Wings at its sides
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
                Blade(b, Body, V(0.045f * s, 0.27f - 0.015f * i, -0.01f), V(0.1f * s, 0.12f - 0.02f * i, 0.01f - 0.02f * i), 0.03f, i == 1 ? Rgb(190, 80, 128) : rose, V(s, 0, 0.2f), 0.25f);
        });
        // The train: long violet plumes streaming back and down behind the skirt
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3) * 0.025f;
            var root = V(x * 0.5f, 0.14f, -0.08f);
            Frond(b, Body, root, root + V(x * 2f, -0.08f + 0.01f * MathF.Abs(i - 3), -0.24f + 0.02f * MathF.Abs(i - 3)), 0.03f, i % 2 == 0 ? plume : violet, V(0, 1f, 0.1f), 0.2f);
        }
        Stretch(b, V(1f, 1.15f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2685 Soufflair, from Slurpuff

    /// <summary>
    /// Soufflair: Slurpuff grown larger and paler, its cherry gone for a tall golden souffle rising from its crown, long
    /// cream ears drooping at its sides dipped in cherry red, ribbons of pink and white cream piped round its body and a
    /// curl of cream for a tail.
    /// </summary>
    private static PokeBuilder Soufflair()
    {
        var b = Become(Slurpuff(), "Soufflair", 0.86f);
        var cream = Rgb(250, 236, 206);
        var gold = Rgb(236, 196, 120);
        var crust = Rgb(220, 170, 96);
        var cherry = Rgb(196, 40, 60);
        var pink = Rgb(240, 170, 186);
        b.Sdf.RemoveAll(s => Is(s.Color, Rgb(210, 60, 80)));
        Recolour(b, (Rgb(250, 246, 248), cream));
        var c = V(0, 0.2f, 0);
        // The souffle: a fluted column rising from the crown and a domed golden top spilling over its rim
        b.Limb(Body, c + V(0, 0.12f, -0.01f), c + V(0, 0.22f, -0.01f), 0.055f, 0.065f, gold, blend: 0.012f);
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * MathF.Tau;
            b.PaintEll(Body, c + V(MathF.Sin(a) * 0.06f, 0.18f, MathF.Cos(a) * 0.06f - 0.01f), V(0.008f, 0.05f, 0.008f), crust, soft: 0.006f);
        }
        b.Ell(Body, c + V(0, 0.26f, -0.01f), V(0.1f, 0.05f, 0.09f), gold, blend: 0.015f);
        for (int i = 0; i < 9; i++)
        {
            float a = i / 9f * MathF.Tau;
            b.Ell(Body, c + V(MathF.Sin(a) * 0.08f, 0.25f, MathF.Cos(a) * 0.075f - 0.01f), V(0.035f, 0.03f, 0.035f), gold, blend: 0.012f);
        }
        b.PaintEll(Body, c + V(0, 0.3f, -0.01f), V(0.06f, 0.02f, 0.05f), crust);
        // Long ears drooping from the head, dipped in cherry red
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, c + V(0.07f * s, 0.08f, 0.0f), c + V(0.13f * s, 0.07f, 0.01f), c + V(0.16f * s, -0.02f, 0.02f), c + V(0.16f * s, -0.08f, 0.03f));
            Ribbon(b, Body, path, 0.04f, cream, V(s, 0.1f, 0.4f), 0.014f);
            b.Ell(Body, path[^1] + V(0, 0.01f, 0), V(0.03f, 0.03f, 0.024f), cherry, mat: Shell, blend: 0.012f);
        });
        // Ribbons of cream piped round the body, pink and white in turn
        for (int i = 0; i < 14; i++)
        {
            float a = i / 14f * MathF.Tau;
            var at = c + V(MathF.Sin(a) * 0.105f, -0.075f, MathF.Cos(a) * 0.095f);
            b.Ell(Body, at, V(0.022f, 0.018f, 0.022f), i % 2 == 0 ? pink : cream, blend: 0.01f);
        }
        PokeBuilder.Both(s =>
        {
            foreach (var y in new[] { 0.04f, 0.07f })
                Blade(b, Body, c + V(0.01f * s, y, 0.1f), c + V(0.07f * s, y - 0.06f, 0.09f), 0.016f, pink, V(0, 0, 1f), 0.4f);
        });
        // A curl of cream for a tail
        var tp = Smooth(3, c + V(0, -0.05f, -0.09f), c + V(0.03f, 0.0f, -0.15f), c + V(0.06f, 0.06f, -0.13f), c + V(0.04f, 0.08f, -0.09f));
        b.Tube(Body, tp, 0.02f, 0.012f, cream, blend: 0.008f);
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2687 Mesmerquid, from Malamar

    /// <summary>
    /// Mesmerquid: Malamar grown taller and darker, a flared hood of fin over its head with its yellow eyes glowing from
    /// the shade under it, a spiral of glowing yellow lights winding up its body and ten long tentacles curling down from
    /// it to the ground, pale beneath.
    /// </summary>
    private static PokeBuilder Mesmerquid()
    {
        var b = Become(Malamar(), "Mesmerquid", 0.95f);
        int head = BoneOf(b, "head");
        var dark = Rgb(62, 50, 104);
        var purple = Rgb(94, 64, 126);
        var under = Rgb(170, 110, 160);
        var glow = Rgb(250, 236, 110);
        // The crown of tentacles goes: the hood is where it was
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && Is(s.Color, Rgb(120, 110, 190)));
        Recolour(b, (Rgb(70, 60, 120), dark), (Rgb(120, 110, 190), purple), (Rgb(226, 60, 120), Rgb(150, 80, 150)), (Rgb(236, 232, 246), under));
        var bc = V(0, 0.4f, 0);
        var c = bc + V(0, 0.17f * 0.95f + 0.07f, 0.01f);
        // The hood: a peak over the head and two broad flaring fins sweeping out and down at its sides
        b.Spike(head, c + V(0, 0.02f, -0.01f), c + V(0, 0.13f, 0.02f), 0.05f, purple, 0.55f);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.02f * s, 0.03f, 0.0f), c + V(0.14f * s, 0.07f, 0.02f), 0.05f, purple, V(0, 1f, 0.3f), 0.18f);
            Blade(b, head, c + V(0.03f * s, -0.01f, -0.01f), c + V(0.2f * s, -0.06f, 0.0f), 0.045f, dark, V(0, 1f, 0.2f), 0.18f);
        });
        // The spiral of lights up its body
        var turns = Spiral3(0.07f, 0.062f, bc.Y - 0.13f, bc.Y + 0.12f, 0f, 2.2f, 44);
        b.Tube(Body, turns.Select(p => p + V(0, 0, 0.004f)).ToArray(), 0.007f, 0.006f, glow, Glow, 0.003f);
        for (int i = 3; i < turns.Length; i += 6)
            b.Ell(Body, turns[i] * V(1.05f, 1f, 1.05f) + V(0, 0.015f, 0), V(0.008f, 0.008f, 0.008f), glow, mat: Glow, blend: 0.003f);
        // Ten tentacles curling down to the ground, lit with dots along them
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) / 10f * MathF.Tau;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            float reach = 0.16f + 0.05f * (i % 3);
            var root = bc + V(0, -0.12f, 0) + d * 0.04f;
            var path = Smooth(3, root, root + d * 0.08f + V(0, -0.1f, 0), root + d * reach + V(0, -0.24f, 0), root + d * (reach + 0.05f) + V(0, -0.272f, 0), root + d * (reach + 0.03f) + V(0, -0.255f, 0));
            b.Tube(Body, path, 0.024f, 0.008f, i % 2 == 0 ? dark : purple, blend: 0f);
            b.Ell(Body, path[4] + V(0, 0.022f, 0), V(0.005f, 0.005f, 0.005f), glow, mat: Glow, blend: 0.002f);
        }
        Stretch(b, V(1.05f, 1.15f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2689 Reefcolony, from Barbaracle

    /// <summary>
    /// Reefcolony: Barbaracle grown into a hulk of reef, its rocks crusted with coral (orange brains, pink and violet
    /// tubes, green weed) and small barnacles, and two more heads with faces grown from its shoulders.
    /// </summary>
    private static PokeBuilder Reefcolony()
    {
        var b = Become(Barbaracle(), "Reefcolony", 1f);
        var limb = Rgb(110, 86, 64);
        var nail = Rgb(226, 214, 176);
        var orange = Rgb(236, 120, 70);
        var pink = Rgb(236, 120, 140);
        var violet = Rgb(170, 110, 200);
        var weed = Rgb(80, 150, 100);
        Recolour(b, (Rgb(130, 136, 160), Rgb(150, 138, 100)), (Rgb(130, 96, 70), limb));
        var top = V(0, 0.48f, 0);
        // Two more heads at the shoulders, each a hand with a face
        PokeBuilder.Both(s =>
        {
            b.Limb(Body, top + V(0.08f * s, 0.05f, 0), top + V(0.14f * s, 0.1f, 0.01f), 0.03f, 0.028f, limb);
            BarnacleHand(b, Body, top + V(0.15f * s, 0.15f, 0.02f), 0.85f, true, limb, nail);
        });
        // Coral over the rocks
        void Brain(int bone, Vector3 at, float r) => b.Ell(bone, at, V(r, r * 0.8f, r), orange, mat: Shell, blend: 0.008f);
        void Tubes(int bone, Vector3 at, Color color)
        {
            foreach (var (dx, dz, h) in new[] { (0f, 0f, 0.05f), (0.012f, 0.008f, 0.035f), (-0.012f, 0.004f, 0.04f) })
                b.Limb(bone, at + V(dx, 0, dz), at + V(dx * 1.6f, h, dz * 1.6f), 0.007f, 0.007f, color, Shell, 0.004f);
        }
        void Weed(int bone, Vector3 at)
        {
            foreach (float t in new[] { -1f, 0f, 1f })
                Frond(b, bone, at, at + V(0.015f * t, 0.05f, 0.005f), 0.008f, weed, V(0, 0, 1f), 0.3f, Leaf);
        }
        Brain(Body, top + V(-0.07f, 0.06f, 0.07f), 0.03f);
        Brain(Body, V(0.06f, 0.36f, 0.07f), 0.024f);
        Tubes(Body, top + V(0.05f, 0.07f, 0.04f), violet);
        Tubes(Body, V(-0.07f, 0.35f, 0.06f), pink);
        Weed(Body, top + V(0.0f, 0.075f, -0.05f));
        Weed(Body, top + V(-0.1f, 0.05f, -0.03f));
        foreach (var at in new[] { V(0.03f, 0.55f, 0.08f), V(-0.04f, 0.53f, 0.09f), V(0.1f, 0.5f, 0.06f), V(-0.1f, 0.33f, 0.05f), V(0.02f, 0.3f, 0.08f) })
            b.Spike(Body, at, at + V(0, 0.015f, 0.01f), 0.008f, nail, mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            Brain(leg, V(0.08f * s, 0.15f, 0.04f), 0.02f);
            Tubes(leg, V(0.09f * s, 0.12f, -0.02f), violet);
        });
        Stretch(b, V(1.12f, 1.1f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2691 Kelpdrake, from Dragalge

    /// <summary>
    /// Kelpdrake: Dragalge grown longer and darker, olive and violet, its fins and crest grown into a forest of long
    /// streamers of kelp trailing back from its head and down its back, poisoned spines on its head and fins rimmed in
    /// glowing violet.
    /// </summary>
    private static PokeBuilder Kelpdrake()
    {
        var b = Become(Dragalge(), "Kelpdrake", 1f);
        int head = BoneOf(b, "head");
        var olive = Rgb(70, 72, 50);
        var kelp = Rgb(92, 96, 50);
        var dark = Rgb(60, 50, 70);
        var violet = Rgb(160, 110, 240);
        Recolour(b, (Rgb(110, 80, 140), olive), (Rgb(170, 130, 190), dark), (Rgb(120, 82, 50), kelp), (Rgb(190, 60, 80), Rgb(110, 80, 150)));
        var c = V(0, 0.55f, 0.15f);
        // Poisoned spines on the head, glowing violet at their tips
        foreach (var (x, y, z) in new[] { (0f, 0.06f, -0.03f), (-0.03f, 0.05f, -0.02f), (0.03f, 0.05f, -0.02f), (-0.045f, 0.03f, 0.0f), (0.045f, 0.03f, 0.0f) })
        {
            var root = c + V(x * 0.6f, 0.025f, z);
            var tip = c + V(x * 1.3f, y + 0.06f, z - 0.06f);
            b.Spike(head, root, tip, 0.012f, dark, 0.5f);
            b.Ell(head, Vector3.Lerp(root, tip, 0.85f), V(0.006f, 0.012f, 0.006f), violet, Euler(tip - root), Glow, 0.003f);
        }
        // Streamers of kelp from the head, the neck and down the back, trailing behind
        var spine = Smooth(3, V(0, 0.5f, 0.14f), V(0, 0.42f, 0.04f), V(0, 0.34f, -0.08f), V(0, 0.2f, -0.12f), V(0, 0.12f, -0.04f), V(0, 0.1f, 0.08f));
        for (int i = 0; i < 9; i++)
        {
            var root = i < 3 ? c + V((i - 1) * 0.02f, 0.02f, -0.03f) : spine[(i - 3) * 2] + V(0, 0.03f, -0.02f);
            float len = 0.22f - 0.012f * i;
            var path = Smooth(3, root, root + V(0.03f * MathF.Sin(i), 0.04f, -len * 0.4f), root + V(-0.03f * MathF.Sin(i * 1.7f), -0.02f, -len * 0.75f), root + V(0.02f * MathF.Cos(i), -0.05f, -len));
            int bone = i < 3 ? head : Body;
            Ribbon(b, bone, path, 0.022f, i % 2 == 0 ? kelp : olive, V(0, 1f, 0.1f), 0.008f);
        }
        // Fins rimmed in violet
        PokeBuilder.Both(s =>
        {
            for (int i = 2; i < spine.Length - 1; i += 3)
                b.Ell(Body, Vector3.Lerp(spine[i] + V(0.02f * s, 0, 0), spine[i] + V(0.1f * s, -0.04f, -0.02f), 0.7f), V(0.012f, 0.008f, 0.01f), violet, mat: Glow, blend: 0.006f);
        });
        Stretch(b, V(1.05f, 1.08f, 1.2f));
        return b;
    }

    // ------------------------------------------------------------------ 2693 Salvoclaw, from Clawitzer

    /// <summary>
    /// Salvoclaw: Clawitzer grown larger and armoured, dark plates along its back with glowing amber vents, a crest of
    /// spines on its head, its great claw grown a second barrel beside the first, both banded in glowing amber, and a
    /// small loader's grip.
    /// </summary>
    private static PokeBuilder Salvoclaw()
    {
        float k = 1.35f;
        var b = Become(Clawitzer(), "Salvoclaw", 1f);
        int head = BoneOf(b, "head");
        int arm = BoneOf(b, "armR");
        var teal = Rgb(60, 120, 150);
        var plate = Rgb(30, 40, 52);
        var amber = Rgb(255, 176, 60);
        Recolour(b, (Rgb(110, 180, 230), teal), (Rgb(40, 80, 170), Rgb(34, 64, 120)));
        var bc = V(0, 0.1f, 0) * k;
        var cc = bc + V(0.055f, 0.07f, 0.14f) * k;
        var cr = V(0.085f, 0.075f, 0.1f) * k;
        // The two barrels over the claw, banded in glowing amber
        foreach (var dx in new[] { -0.035f, 0.035f })
        {
            var from = cc + V(dx * k, cr.Y * 0.75f, -cr.Z * 0.4f);
            var to = from + V(0, 0.01f, 0.2f * k);
            b.Limb(arm, from, to, 0.03f * k, 0.028f * k, plate, Metal, 0.006f);
            b.Cut(arm, to + V(0, 0, 0.004f), V(0.017f, 0.017f, 0.03f) * k);
            foreach (float t in new[] { 0.35f, 0.55f, 0.75f })
                b.Torus(arm, Vector3.Lerp(from, to, t), 0.029f * k, 0.004f * k, amber, V(90f, 0, 0), mat: Glow, blend: 0.003f);
        }
        // Armour plates along the back with amber vents
        for (int i = 0; i < 5; i++)
        {
            var at = bc + V(0, 0.05f - 0.004f * i, 0.04f - 0.035f * i) * k;
            b.Ell(Body, at, V(0.06f, 0.025f, 0.03f) * k, plate, V(-15f, 0, 0), Shell, 0.008f);
            b.Ell(Body, at + V(0, 0.022f, 0.0f) * k, V(0.012f, 0.004f, 0.01f) * k, amber, mat: Glow, blend: 0.003f);
        }
        // A crest of spines on its head
        var c = bc + V(-0.045f, 0.035f, 0.06f) * k;
        foreach (var (x, h) in new[] { (-0.02f, 0.05f), (0f, 0.07f), (0.02f, 0.05f) })
            b.Spike(head, c + V(x, 0.025f, -0.01f) * k, c + V(x * 1.4f, 0.03f + h, -0.06f) * k, 0.012f * k, teal, 0.5f, Shell);
        // A small loader's grip on its other side
        var grip = bc + V(-0.09f, 0.0f, 0.1f) * k;
        b.Limb(Body, bc + V(-0.05f, 0.0f, 0.04f) * k, grip, 0.012f * k, 0.01f * k, plate, Shell);
        foreach (var d in new[] { V(-0.02f, 0.02f, 0.03f), V(-0.02f, -0.02f, 0.03f) })
            b.Spike(Body, grip, grip + d * k, 0.008f * k, plate, 0.6f, Shell);
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2695 Coronafrill, from Heliolisk

    /// <summary>
    /// Coronafrill: Heliolisk grown longer and darker, black striped with bright yellow, its frill grown into two great
    /// fans of dark panels standing up at either side of its head, rimmed in glowing gold and orange like the sun's
    /// corona, and its tail curled up at its end into a second small corona.
    /// </summary>
    private static PokeBuilder Coronafrill()
    {
        var b = Become(Heliolisk(), "Coronafrill", 0.95f);
        int tail = BoneOf(b, "tail");
        var dark = Rgb(56, 58, 46);
        var yellow = Rgb(234, 236, 90);
        var orange = Rgb(232, 120, 40);
        var gold = Rgb(255, 210, 80);
        Recolour(b, (Rgb(250, 220, 90), yellow), (Rgb(50, 50, 58), dark));
        var bc = V(0, 0.32f, 0);
        var neck = bc + V(0, 0.1f, 0);
        // Two great fans at the sides of the head, each of eight panels, their rims lit gold
        PokeBuilder.Both(s =>
        {
            var root = neck + V(0.03f * s, 0.06f, -0.02f);
            for (int i = 0; i < 8; i++)
            {
                float a = (-20f + 25f * i) * Degree;
                var d = Vector3.Normalize(V(MathF.Sin(a) * s * 0.9f + 0.25f * s, MathF.Cos(a), -0.25f));
                var tip = root + d * 0.15f;
                Blade(b, Body, root, tip, 0.03f, dark, V(0.3f * s, 0, 1f), 0.15f);
                b.PaintEll(Body, Vector3.Lerp(root, tip, 0.7f), V(0.02f, 0.03f, 0.02f), orange, Euler(d), 0.01f);
                b.Ell(Body, Vector3.Lerp(root, tip, 0.95f), V(0.012f, 0.016f, 0.006f), gold, Euler(d), Glow, 0.004f);
            }
        });
        // Yellow stripes down the back
        foreach (var y in new[] { 0.08f, 0.02f, -0.04f })
            b.PaintEll(Body, bc + V(0, y, -0.04f), V(0.05f, 0.012f, 0.02f), yellow);
        // The tail runs on and curls up into a second corona
        var tp = Smooth(3, V(0, 0.04f, -0.36f), V(0, 0.06f, -0.44f), V(0, 0.16f, -0.48f), V(0, 0.24f, -0.44f), V(0, 0.22f, -0.38f));
        b.Tube(tail, tp, 0.01f, 0.008f, dark, blend: 0f);
        var ring = tp[^1];
        b.Ell(tail, ring, V(0.02f, 0.02f, 0.012f), dark, blend: 0.006f);
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * MathF.Tau;
            var d = V(0, MathF.Cos(a), MathF.Sin(a));
            Blade(b, tail, ring + d * 0.015f, ring + d * 0.07f, 0.01f, i % 2 == 0 ? orange : gold, V(1f, 0, 0), 0.3f, Glow);
        }
        Stretch(b, V(1.05f, 1.12f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2697 Despotitan, from Tyrantrum

    /// <summary>
    /// Despotitan: Tyrantrum grown greater and browner, its white plumes grown into a mane falling from its crown down
    /// its spine past its shoulders, crags of dark rock along its jaw, on its shoulders and knees and in rows of plates
    /// down its heavy tail.
    /// </summary>
    private static PokeBuilder Despotitan()
    {
        var b = Become(Tyrantrum(), "Despotitan", 1f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var brown = Rgb(128, 96, 56);
        var rock = Rgb(84, 76, 62);
        var mane = Rgb(244, 242, 236);
        Recolour(b, (Rgb(170, 70, 60), brown), (Rgb(206, 110, 70), Rgb(104, 88, 60)), (Rgb(236, 140, 60), rock), (Rgb(236, 226, 214), Rgb(196, 160, 100)));
        var bc = V(0, 0.38f, -0.04f);
        var neck = bc + V(0, 0.14f, 0.08f);
        var c = neck + V(0, 0.1f, 0.12f);
        // The mane: long white locks from the crown down the neck and the spine
        for (int i = 0; i < 9; i++)
        {
            float t = i / 8f;
            var root = Vector3.Lerp(c + V(0, 0.05f, -0.06f), bc + V(0, 0.1f, -0.1f), t);
            PokeBuilder.Both(s => b.Spike(i < 3 ? head : Body, root + V(0.03f * s, 0, 0), root + V(0.09f * s, -0.04f - 0.03f * t, -0.12f), 0.035f, mane, 0.35f));
            b.Spike(i < 3 ? head : Body, root, root + V(0, 0.06f - 0.04f * t, -0.14f), 0.035f, mane, 0.35f);
        }
        // Crags of rock along the jaw
        PokeBuilder.Both(s =>
        {
            foreach (var z in new[] { 0.06f, 0.0f, -0.06f })
                b.Spike(head, c + V(0.07f * s, -0.04f, z), c + V(0.1f * s, -0.07f, z - 0.01f), 0.014f, rock, 0.6f, Shell);
            // On the shoulders and the knees
            foreach (var (x, y, z) in new[] { (0.12f, 0.46f, 0.02f), (0.13f, 0.42f, -0.04f) })
                b.Spike(Body, V(x * s, y, z), V((x + 0.05f) * s, y + 0.06f, z - 0.02f), 0.025f, rock, 0.6f, Shell);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Spike(leg, V(0.13f * s, 0.2f, 0.03f), V(0.16f * s, 0.25f, 0.08f), 0.03f, rock, 0.6f, Shell);
        });
        // Rows of plates down the tail
        var tp = Smooth(3, bc + V(0, 0, -0.14f), bc + V(0, -0.08f, -0.3f), bc + V(0, -0.16f, -0.46f), bc + V(0, -0.2f, -0.56f));
        for (int i = 1; i < tp.Length - 1; i++)
        {
            float w = 0.07f * (1f - i / (float)tp.Length);
            var d = Vector3.Normalize(tp[i + 1] - tp[i - 1]);
            b.Spike(tail, tp[i] + V(0, w * 0.6f, 0), tp[i] + V(0, w + 0.05f, 0) - d * 0.03f, w * 0.5f + 0.008f, rock, 0.5f, Shell);
            PokeBuilder.Both(s => b.Spike(tail, tp[i] + V(w * 0.7f * s, w * 0.2f, 0), tp[i] + V((w + 0.03f) * s, w * 0.5f, 0) - d * 0.02f, w * 0.3f + 0.006f, rock, 0.5f, Shell));
        }
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2699 Borealosaur, from Aurorus

    /// <summary>
    /// Borealosaur: Aurorus grown greater and frosted, grey-blue crusted with rime, ice crystals on its shoulders, back
    /// and legs, a crown of ice on its head, and its sail grown into a ribbon of glowing green and pink light that
    /// streams from its head back over its body to its tail.
    /// </summary>
    private static PokeBuilder Borealosaur()
    {
        var b = Become(Aurorus(), "Borealosaur", 1f);
        int head = BoneOf(b, "head");
        int sail = BoneOf(b, "sail");
        var grey = Rgb(110, 128, 150);
        var pale = Rgb(176, 200, 220);
        var ice = Rgb(170, 230, 250);
        var green = Rgb(110, 250, 180);
        var pink = Rgb(240, 150, 230);
        b.Sdf.RemoveAll(s => s.Bone == sail);
        Recolour(b, (Rgb(110, 150, 220), grey), (Rgb(190, 214, 240), pale));
        var bc = V(0, 0.32f, -0.04f);
        var neckTop = bc + V(0, 0.56f, 0.24f);
        var c = neckTop + V(0, 0.04f, 0.03f);
        // A crown of ice crystals on its head
        foreach (var (x, h, z) in new[] { (0f, 0.1f, -0.02f), (-0.025f, 0.08f, -0.01f), (0.025f, 0.08f, -0.01f), (-0.045f, 0.06f, -0.03f), (0.045f, 0.06f, -0.03f), (0f, 0.07f, -0.05f) })
            b.Spike(head, c + V(x, 0.03f, z), c + V(x * 1.5f, 0.03f + h, z - 0.02f), 0.014f, ice, 0.7f, Glow);
        // Crystals on the shoulders, along the back and down the legs
        var br = V(0.12f, 0.11f, 0.18f);
        foreach (var (x, y, z, size) in new[] { (0.7f, 0.6f, 0.6f, 0.03f), (0.7f, 0.6f, -0.6f, 0.026f), (0.35f, 0.9f, 0f, 0.02f), (0.3f, 0.85f, -0.5f, 0.02f) })
            PokeBuilder.Both(s =>
            {
                var at = Out(bc, br, default, V(x * s, y, z));
                var n = Vector3.Normalize(V(x * s, y, z));
                b.Spike(Body, at - n * 0.01f, at + n * size * 1.8f + V(0, size, 0), size * 0.7f, ice, 0.7f, Glow);
            });
        PokeBuilder.Both(s =>
        {
            foreach (var (name, z) in new[] { ("leg", 0.1f), ("hind", -0.16f) })
            {
                int leg = BoneOf(b, name + (s < 0 ? "L" : "R"));
                b.Spike(leg, V(0.09f * s, 0.18f, z + 0.03f), V(0.11f * s, 0.22f, z + 0.06f), 0.014f, ice, 0.7f, Glow);
            }
        });
        Patches(b, Body, pale, (bc + V(0.1f, 0.08f, 0.06f), 0.04f), (bc + V(-0.1f, 0.06f, -0.06f), 0.04f), (bc + V(0.05f, 0.1f, -0.12f), 0.03f));
        // The ribbon of light, from behind the head over the back to the tail in waves
        var ribbon = Smooth(3, c + V(0, 0.02f, -0.05f), c + V(0, 0.08f, -0.18f), bc + V(0, 0.4f, -0.1f), bc + V(0, 0.22f, -0.24f), bc + V(0, 0.3f, -0.4f), bc + V(0, 0.16f, -0.5f), bc + V(0, 0.2f, -0.58f));
        Ribbon(b, sail, ribbon, 0.06f, green, V(1f, 0, 0), 0.012f);
        for (int i = 1; i < ribbon.Length; i += 2)
            b.PaintEll(sail, ribbon[i] + V(0, -0.01f, 0), V(0.03f, 0.025f, 0.04f), pink, soft: 0.012f);
        Make(b, s => s.Bone == sail && Is(s.Color, green), Glow);
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2700 Faerieon, from Sylveon

    /// <summary>
    /// Faerieon: Sylveon grown longer and more slender, its blue deeper, a second bow at its other ear and its feelers
    /// grown to six long ribbons that trail round it like a dancer's, white running to rose and deep blue at their ends,
    /// swirls of rose and gold over its flanks and a long tail plumed pink.
    /// </summary>
    private static PokeBuilder Faerieon()
    {
        var b = Become(Sylveon(), "Faerieon", 0.86f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var cream = Rgb(250, 240, 232);
        var pink = Rgb(240, 160, 190);
        var blue = Rgb(56, 70, 150);
        var gold = Rgb(236, 200, 120);
        Recolour(b, (Rgb(110, 190, 236), blue), (Rgb(70, 170, 230), Rgb(50, 70, 160)));
        b.Sdf.RemoveAll(s => s.Bone == tail);
        var bc = V(0, 0.2f, -0.02f);
        var c = bc + V(0, 0.15f, 0.11f);
        // Swirls over the flanks
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, bc + V(0.05f * s, 0.02f, -0.02f), V(0.02f, 0.035f, 0.05f), pink, V(30f, 0, 0));
            b.PaintEll(Body, bc + V(0.055f * s, 0.0f, 0.03f), V(0.01f, 0.02f, 0.03f), gold, V(-30f, 0, 0));
        });
        // A second bow at the right ear, with its ribbon
        var bow = c + V(0.05f, 0.06f, 0.0f);
        PokeBuilder.Both(s => b.Ell(head, bow + V(0.016f * s, 0, 0), V(0.015f, 0.011f, 0.007f), pink, V(0, 0, 20f * s), blend: 0.006f));
        b.Ell(head, bow, V(0.008f, 0.008f, 0.007f), blue, blend: 0.004f);
        // Four more ribbons from the neck's bow, trailing back round the body and to the ground
        var bowAt = bc + V(0.035f, 0.09f, 0.1f);
        var paths = new[]
        {
            (Smooth(4, bowAt, bowAt + V(-0.03f, 0.07f, -0.05f), bowAt + V(-0.08f, 0.1f, -0.15f), bowAt + V(-0.12f, 0.03f, -0.26f), bowAt + V(-0.13f, 0.08f, -0.36f)), -1f),
            (Smooth(4, bowAt, bowAt + V(0.04f, 0.06f, -0.06f), bowAt + V(0.09f, 0.09f, -0.17f), bowAt + V(0.12f, 0.02f, -0.27f), bowAt + V(0.13f, 0.06f, -0.38f)), 1f),
            (Smooth(4, bowAt, bowAt + V(-0.07f, -0.01f, -0.02f), bowAt + V(-0.11f, -0.05f, -0.12f), bowAt + V(-0.12f, -0.1f, -0.24f), bowAt + V(-0.1f, -0.06f, -0.34f)), -1f),
            (Smooth(4, bowAt, bowAt + V(0.07f, -0.02f, -0.02f), bowAt + V(0.1f, -0.06f, -0.12f), bowAt + V(0.11f, -0.11f, -0.24f), bowAt + V(0.1f, -0.07f, -0.34f)), 1f)
        };
        foreach (var (path, side) in paths)
        {
            Ribbon(b, head, path, 0.026f, cream, V(side, 0.6f, 0), 0.0045f);
            b.PaintEll(head, path[^2], V(0.022f, 0.022f, 0.022f), pink, soft: 0.006f);
            b.PaintEll(head, path[^1], V(0.022f, 0.022f, 0.022f), blue, soft: 0.006f);
        }
        // A long plumed tail
        var tp = Smooth(3, bc + V(0, 0.02f, -0.09f), bc + V(0, 0.06f, -0.18f), bc + V(0, 0.04f, -0.28f), bc + V(0, -0.04f, -0.34f));
        b.Tube(tail, tp, 0.03f, 0.03f, cream, blend: 0.01f);
        FurTufts(b, tail, tp[6], V(0.035f, 0.035f, 0.06f), 14, 0.04f, 0.018f, pink, 1.1f, -1f, 0.4f);
        b.PaintEll(tail, tp[^1], V(0.04f, 0.04f, 0.04f), blue);
        Stretch(b, V(0.95f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2701 Luchacondor, from Hawlucha

    /// <summary>
    /// Luchacondor: Hawlucha grown into a condor of a wrestler, dark green and maroon, its mask grown a headdress of long
    /// red, green and orange plumes round a band of gold, a white ruff at its neck, its wings wider and tipped red, and a
    /// belt of gold feathers round its waist with a gold shield at its front.
    /// </summary>
    private static PokeBuilder Luchacondor()
    {
        var b = Become(Hawlucha(), "Luchacondor", 0.92f);
        int head = BoneOf(b, "head");
        var green = Rgb(40, 110, 70);
        var maroon = Rgb(140, 40, 60);
        var gold = Rgb(246, 196, 60);
        var red = Rgb(220, 70, 40);
        var orange = Rgb(240, 130, 40);
        Recolour(b, (Rgb(60, 170, 120), green), (Rgb(214, 60, 50), maroon), (Rgb(246, 150, 50), red));
        var bc = V(0, 0.26f, 0);
        var c = bc + V(0, 0.12f, 0.02f);
        // The headdress: a band of gold round the brow and long plumes standing up and sweeping back round it
        b.Torus(head, c + V(0, 0.03f, -0.005f), 0.044f, 0.007f, gold, V(-15f, 0, 0), mat: Metal, blend: 0.004f);
        for (int i = 0; i < 11; i++)
        {
            float a = (i / 10f - 0.5f) * 200f * Degree;
            var d = V(MathF.Sin(a), 0.9f + 0.3f * MathF.Cos(a), -0.6f);
            var root = c + V(MathF.Sin(a) * 0.035f, 0.04f, -0.02f);
            Frond(b, head, root, root + Vector3.Normalize(d) * 0.13f, 0.014f, i % 3 == 0 ? red : i % 3 == 1 ? green : orange, V(0, 0.4f, 1f), 0.25f);
        }
        // The belt: a ring of gold feathers round the waist and a shield at its front
        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * MathF.Tau;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(0, -0.04f, 0) + d * V(0.056f, 0, 0.048f);
            Blade(b, Body, root, root + d * 0.03f + V(0, -0.012f, 0), 0.012f, gold, V(0, 1f, 0), 0.3f);
        }
        b.Ell(Body, bc + V(0, -0.04f, 0.055f), V(0.026f, 0.03f, 0.01f), gold, mat: Metal, blend: 0.006f);
        b.PaintEll(Body, bc + V(0, -0.04f, 0.066f), V(0.012f, 0.018f, 0.006f), orange, soft: 0.004f);
        Stretch(b, V(1.15f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2702 Signalmus, from Dedenne

    /// <summary>
    /// Signalmus: Dedenne grown larger and rounder, its cheek pouches full and pink, its whiskers grown into two tall
    /// ribbed aerials rising from its head with a glowing dish at each tip, and its tail a cable coiled in rings behind
    /// it, ending in a plug.
    /// </summary>
    private static PokeBuilder Signalmus()
    {
        var b = Become(Dedenne(), "Signalmus", 0.62f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var black = Rgb(44, 42, 50);
        var cable = Rgb(84, 86, 94);
        var pink = Rgb(240, 140, 160);
        var dish = Rgb(250, 230, 120);
        b.Sdf.RemoveAll(s => (s.Bone == head && Is(s.Color, black) && s.Shape == SdfShape.RoundCone) || s.Bone == tail);
        var c = V(0, 0.17f, 0.015f);
        // Full pink cheeks
        PokeBuilder.Both(s => b.Ell(head, c + V(0.06f * s, -0.025f, 0.02f), V(0.04f, 0.035f, 0.035f), pink, blend: 0.012f));
        // Two tall ribbed aerials with a dish at each tip
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, c + V(0.025f * s, 0.05f, -0.01f), c + V(0.035f * s, 0.12f, -0.015f), c + V(0.055f * s, 0.2f, -0.01f));
            b.Tube(head, path, 0.008f, 0.006f, cable, Metal, 0f);
            for (int i = 1; i < path.Length - 1; i++)
                b.Torus(head, path[i], 0.008f, 0.0025f, Rgb(60, 62, 70), Euler(path[i + 1] - path[i - 1]), mat: Metal, blend: 0.002f);
            var top = path[^1];
            b.Ell(head, top + V(0.006f * s, 0.012f, 0.004f), V(0.03f, 0.008f, 0.03f), dish, V(-15f, 0, -20f * s), Glow, 0.004f);
            b.Spike(head, top + V(0.006f * s, 0.016f, 0.004f), top + V(0.008f * s, 0.036f, 0.008f), 0.004f, Rgb(200, 180, 80), mat: Metal, blend: 0.002f);
        });
        // The cable of a tail, coiled in rings behind, ending in a plug
        var coil = Spiral3(0.04f, 0.03f, 0.03f, 0.14f, 0f, 4f, 64).Select(p => p + V(0.03f, 0, -0.09f)).ToArray();
        var cord = new[] { V(0, 0.05f, -0.05f) }.Concat(coil).Concat(Smooth(3, coil[^1], coil[^1] + V(0.02f, 0.05f, 0.02f), coil[^1] + V(0.04f, 0.08f, 0.0f))).ToArray();
        b.Tube(tail, cord, 0.009f, 0.009f, cable, blend: 0f);
        var plug = cord[^1];
        b.Limb(tail, plug, plug + V(0.015f, 0.01f, 0), 0.008f, 0.008f, black, Metal, 0.003f);
        foreach (float t in new[] { -1f, 1f })
            b.Limb(tail, plug + V(0.015f, 0.01f, t * 0.004f), plug + V(0.03f, 0.02f, t * 0.004f), 0.002f, 0.002f, Rgb(230, 200, 80), Metal, 0.001f);
        Stretch(b, V(1.25f, 1.25f, 1.25f));
        return b;
    }

    // ------------------------------------------------------------------ 2703 Druzarion, from Carbink

    /// <summary>
    /// Druzarion: Carbink grown into a whole geode, its white fluff gone and its rock split open down its front on a
    /// cave of violet crystals glowing within, its crown of blue crystals taller and two arms of blue crystal at its
    /// sides. No pink diamond: that is Diancie's.
    /// </summary>
    private static PokeBuilder Druzarion()
    {
        var b = Become(Carbink(), "Druzarion", 0.7f);
        int head = BoneOf(b, "head");
        var rock = Rgb(120, 118, 116);
        var crystal = Rgb(130, 200, 246);
        var violet = Rgb(170, 110, 240);
        var deep = Rgb(70, 40, 110);
        var fluff = Rgb(240, 238, 244);
        Unlift(b, V(0, 0.16f, 0));
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, fluff));
        Recolour(b, (Rgb(130, 128, 140), rock), (Rgb(206, 202, 216), rock));
        // The rock grown rounder, and split down its front onto the cave
        b.Ell(Body, V(0, 0.14f, 0), V(0.1f, 0.095f, 0.09f), rock, mat: Shell, blend: 0.02f);
        b.Cut(Body, V(0, 0.13f, 0.085f), V(0.055f, 0.06f, 0.05f));
        b.Ell(Body, V(0, 0.13f, 0.03f), V(0.05f, 0.055f, 0.03f), deep, mat: Glow, blend: 0.004f);
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * MathF.Tau;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            var root = V(0, 0.13f, 0.04f) + d * 0.05f;
            b.Spike(Body, root, root - d * 0.03f + V(0, 0, 0.03f), 0.01f, i % 2 == 0 ? violet : Rgb(210, 160, 250), 0.7f, Glow, 0.004f);
        }
        // The crown taller, and two arms of crystal at the sides
        var c = V(0, 0.235f, 0.025f);
        foreach (var (x, h) in new[] { (0f, 0.16f), (-0.025f, 0.12f), (0.025f, 0.13f), (-0.045f, 0.08f), (0.045f, 0.09f) })
            b.Spike(head, c + V(x, 0.02f, -0.01f), c + V(x * 1.6f, 0.02f + h, -0.02f), 0.018f, crystal, 0.6f, Glow);
        PokeBuilder.Both(s =>
        {
            var root = V(0.095f * s, 0.15f, 0.0f);
            foreach (var d in new[] { V(0.07f, 0.04f, 0.01f), V(0.08f, 0.0f, 0.02f), V(0.06f, -0.03f, 0.0f) })
                b.Spike(Body, root, root + V(d.X * s, d.Y, d.Z), 0.016f, crystal, 0.6f, Glow);
        });
        Stretch(b, V(1.3f, 1.3f, 1.3f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2707 Chatelkey, from Klefki

    /// <summary>A key on a ring: a bow of a ring at <paramref name="at"/>, its shaft hanging along <paramref name="down"/>, a square bit at its end.</summary>
    private static void HangingKey(PokeBuilder b, int bone, Vector3 at, Vector3 down, float len, float k, Color color)
    {
        var d = Vector3.Normalize(down);
        var side = Vector3.Normalize(Vector3.Cross(d, V(0, 0, 1f)));
        b.Torus(bone, at + d * 0.012f * k, 0.011f * k, 0.004f * k, color, V(90f, 0, 0), mat: Metal, blend: 0.003f);
        b.Limb(bone, at + d * 0.022f * k, at + d * len, 0.005f * k, 0.005f * k, color, Metal, 0.003f);
        b.Box(bone, at + d * (len - 0.01f * k) + side * 0.01f * k, V(0.009f, 0.007f, 0.003f) * k, 0.002f, color, V(0, 0, MathF.Atan2(d.Y, d.X) / Degree + 90f), Metal, 0.003f);
    }

    /// <summary>
    /// Chatelkey: Klefki grown into the keeper of a castle's keys, its ring a great hoop banded in brass, a crown of
    /// turrets over its face and a keyhole below its eyes, a dozen keys of brass, silver and copper hanging from the
    /// hoop, one great gate key at its foot and two wings of filigree.
    /// </summary>
    private static PokeBuilder Chatelkey()
    {
        var b = Become(Klefki(), "Chatelkey", 0.7f);
        int head = BoneOf(b, "head");
        var steel = Rgb(170, 174, 184);
        var brass = Rgb(206, 170, 90);
        var silver = Rgb(200, 204, 214);
        var copper = Rgb(176, 110, 80);
        Unlift(b, V(0, 0.2f, 0));
        Recolour(b, (Rgb(236, 150, 170), brass), (Rgb(206, 210, 220), steel));
        var ring = V(0, 0.2f, 0);
        // Bands of brass on the hoop
        foreach (var deg in new[] { 90f, 30f, 150f })
        {
            float a = deg * Degree;
            b.Torus(Body, ring + V(MathF.Cos(a), MathF.Sin(a), 0) * 0.1f, 0.011f, 0.004f, brass, V(0, 0, deg), mat: Metal, blend: 0.003f);
        }
        // A dozen keys round the lower half of the hoop
        for (int i = 0; i < 12; i++)
        {
            float a = (195f + 150f * i / 11f) * Degree;
            if (i == 6 || i == 5) continue;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            var color = (i % 3) switch { 0 => brass, 1 => silver, _ => copper };
            HangingKey(b, Body, ring + d * 0.1f, Vector3.Normalize(d + V(0, -0.8f, 0)), 0.08f + 0.01f * (i % 2), 1f, color);
        }
        // The great gate key at its foot
        var foot = ring + V(0, -0.1f, 0);
        b.Torus(Body, foot + V(0, -0.02f, 0), 0.018f, 0.006f, brass, V(90f, 0, 0), mat: Metal, blend: 0.004f);
        b.Limb(Body, foot + V(0, -0.035f, 0), foot + V(0, -0.15f, 0), 0.009f, 0.009f, brass, Metal, 0.004f);
        b.Box(Body, foot + V(0.016f, -0.14f, 0), V(0.016f, 0.018f, 0.005f), 0.003f, brass, default, Metal, 0.004f);
        // Wings of filigree at its upper sides
        PokeBuilder.Both(s =>
        {
            Blade(b, Body, ring + V(0.09f * s, 0.06f, -0.01f), ring + V(0.2f * s, 0.14f, -0.02f), 0.04f, silver, V(0, 0, 1f), 0.15f, Metal);
            Blade(b, Body, ring + V(0.09f * s, 0.03f, -0.01f), ring + V(0.17f * s, 0.0f, -0.02f), 0.03f, silver, V(0, 0, 1f), 0.15f, Metal);
        });
        // A crown of turrets over the face, and a keyhole below its eyes
        var c = V(0, 0.245f, 0.005f);
        var r = V(0.035f, 0.035f, 0.03f);
        foreach (var x in new[] { -0.022f, 0f, 0.022f })
        {
            b.Limb(head, c + V(x, 0.03f, -0.005f), c + V(x, 0.05f, -0.005f), 0.009f, 0.009f, steel, Metal, 0.004f);
            b.Box(head, c + V(x, 0.055f, -0.005f), V(0.01f, 0.005f, 0.01f), 0.001f, steel, default, Metal, 0.002f);
        }
        var hole = On(c, r, 0, c.Y - 0.015f);
        b.Mark(head, hole, Outward(c, r, hole), 0.004f, 0.007f, Rgb(40, 36, 44), MarkShape.Disc);
        Stretch(b, V(1.4f, 1.4f, 1.4f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2709 Sylvanshade, from Trevenant

    /// <summary>
    /// Sylvanshade: Trevenant grown into an old grove of a tree, its leaves gone and its branches grown into a bare
    /// canopy of twisting twigs hung with glowing green moss, three red eyes in the knot of its trunk, its roots trailing
    /// far behind it over the ground.
    /// </summary>
    private static PokeBuilder Sylvanshade()
    {
        var b = Become(Trevenant(), "Sylvanshade", 1f);
        var bark = Rgb(80, 76, 62);
        var moss = Rgb(120, 230, 140);
        var lichen = Rgb(90, 130, 70);
        b.Sdf.RemoveAll(s => Is(s.Color, Rgb(70, 140, 72)) || Is(s.Color, Rgb(110, 176, 96)));
        Recolour(b, (Rgb(122, 98, 72), bark));
        var bc = V(0, 0.38f, 0);
        var br = V(0.1f, 0.21f, 0.09f);
        // Two more red eyes beside the first in the knot
        var eyeAt = Out(bc, br, default, V(-0.35f, 0.38f, 1f));
        foreach (var (dx, dy) in new[] { (0.026f, 0.01f), (0.008f, -0.022f) })
        {
            var at = Out(bc, br, default, V(-0.35f + dx / br.X, 0.38f + dy / br.Y, 1f));
            b.Eye(Body, at, Outward(bc, br, at), 0.011f, Rgb(220, 50, 60), glare: true);
        }
        b.PaintEll(Body, eyeAt + V(0.012f, -0.006f, 0), V(0.05f, 0.04f, 0.03f), Rgb(40, 30, 32));
        Patches(b, Body, lichen, (bc + V(0.08f, -0.08f, 0.05f), 0.03f), (bc + V(-0.07f, 0.1f, -0.05f), 0.03f), (bc + V(0.05f, 0.14f, 0.05f), 0.025f));
        // The canopy: branches forking up and out from the crown and the arms, moss hanging from them
        var tips = new System.Collections.Generic.List<(int bone, Vector3 at)>();
        foreach (var (x, z, lean) in new[] { (-0.04f, 0.0f, -1f), (0.04f, -0.01f, 1f), (0f, -0.04f, 0f), (0.02f, 0.03f, 0.4f), (-0.03f, 0.03f, -0.5f) })
        {
            var root = bc + V(x, br.Y * 0.85f, z);
            var mid = root + V(0.08f * lean, 0.12f, z);
            b.Limb(Body, root, mid, 0.022f, 0.013f, bark);
            foreach (var d in new[] { V(0.08f, 0.08f, 0.02f), V(-0.04f, 0.1f, -0.02f), V(0.02f, 0.07f, 0.06f) })
            {
                var tip = mid + V(d.X + 0.05f * lean, d.Y, d.Z);
                b.Limb(Body, mid, tip, 0.012f, 0.005f, bark);
                tips.Add((Body, Vector3.Lerp(mid, tip, 0.6f)));
            }
        }
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var end = bc + V(0.3f * s, 0.24f, 0.04f);
            foreach (var d in new[] { V(0.06f, 0.08f, 0), V(0.09f, 0.02f, 0.03f), V(0.03f, 0.1f, -0.03f) })
            {
                var tip = end + V(d.X * s, d.Y, d.Z);
                b.Limb(arm, end, tip, 0.01f, 0.004f, bark);
                tips.Add((arm, Vector3.Lerp(end, tip, 0.5f)));
            }
        });
        for (int i = 0; i < tips.Count; i += 2)
        {
            var (bone, at) = tips[i];
            float len = 0.06f + 0.03f * (i % 3);
            b.Tube(bone, Smooth(2, at, at + V(0.005f, -len * 0.5f, 0.003f), at + V(-0.004f, -len, 0)), 0.006f, 0.003f, moss, Glow, 0.003f);
        }
        // Roots trailing behind it over the ground
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            foreach (var (dx, len) in new[] { (0.02f, 0.3f), (0.08f, 0.24f), (-0.03f, 0.34f) })
                b.Tube(leg, Smooth(3, V(0.05f * s, 0.2f, -0.02f), V((0.07f + dx) * s, 0.06f, -0.12f), V((0.08f + dx * 2f) * s, 0.015f, -len)), 0.028f, 0.008f, bark, blend: 0f);
        });
        Stretch(b, V(1.05f, 1.2f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2711 Lanternhusk, from Gourgeist

    /// <summary>
    /// Lanternhusk: Gourgeist grown into a jack-o'-lantern, its pumpkin weathered orange and carved with a great grin
    /// lit from inside, the little face above it under a crooked hat of bark whose point curls over, its hair grown into
    /// a cloak of mossy vines hanging round the pumpkin and its arms gnarled twigs.
    /// </summary>
    private static PokeBuilder Lanternhusk()
    {
        float k = 1f;
        var b = Become(Gourgeist(), "Lanternhusk", 0.92f);
        int head = BoneOf(b, "head");
        var pumpkin = Rgb(190, 116, 56);
        var rib = Rgb(140, 80, 40);
        var bark = Rgb(120, 96, 66);
        var moss = Rgb(96, 110, 66);
        var vine = Rgb(110, 96, 60);
        var glow = Rgb(255, 170, 60);
        Unlift(b, V(0, 0.16f, 0));
        Recolour(b, (Rgb(66, 56, 48), pumpkin), (Rgb(44, 38, 34), rib), (Rgb(236, 150, 124), moss), (Rgb(220, 190, 140), Rgb(150, 120, 80)), (Rgb(250, 210, 70), glow));
        var pc = V(0, 0.16f, 0) * k;
        var pr = V(0.1f, 0.13f, 0.1f) * k;
        // The great grin, carved deeper and lit
        b.Mark(Body, Out(pc, pr, default, V(0, -0.15f, 1f)), V(0, -0.15f, 1f), 0.06f, 0.03f, glow, MarkShape.Smile);
        // The hat of bark on the head: a ragged brim and a point that curls over
        var hc = pc + V(0, pr.Y + 0.035f, 0.02f);
        b.Ell(head, hc + V(0, 0.035f, -0.01f), V(0.085f, 0.012f, 0.08f), bark, blend: 0.01f);
        var hat = Smooth(3, hc + V(0, 0.04f, -0.01f), hc + V(0.0f, 0.11f, -0.02f), hc + V(0.03f, 0.18f, -0.03f), hc + V(0.08f, 0.2f, -0.02f), hc + V(0.09f, 0.16f, -0.01f));
        b.Tube(head, hat, 0.045f, 0.006f, bark, blend: 0f);
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * MathF.Tau;
            b.Spike(head, hc + V(MathF.Sin(a) * 0.07f, 0.035f, MathF.Cos(a) * 0.065f - 0.01f), hc + V(MathF.Sin(a) * 0.11f, 0.02f, MathF.Cos(a) * 0.1f - 0.01f), 0.014f, bark, 0.4f);
        }
        // A cloak of mossy vines hanging from under the hat round the pumpkin
        for (int i = 0; i < 10; i++)
        {
            float a = (i / 9f - 0.5f) * 190f * Degree + MathF.PI;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = hc + V(0, 0.01f, -0.01f) + d * 0.05f;
            var path = Smooth(2, root, root + d * 0.07f + V(0, -0.08f, 0), pc + d * (pr.X + 0.03f) + V(0, -0.02f, 0), pc + d * (pr.X + 0.02f) + V(0, -0.16f, 0));
            Ribbon(b, head, path, 0.03f, i % 2 == 0 ? moss : Rgb(80, 96, 56), d, 0.01f);
        }
        // Gnarled twigs at the ends of its arms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = hc + V(0.24f * s, -0.05f, 0.02f);
            foreach (var d in new[] { V(0.05f, -0.04f, 0.01f), V(0.03f, -0.07f, 0.03f), V(0.06f, 0.0f, 0.02f) })
                b.Tube(arm, Smooth(2, hand, hand + V(d.X * 0.5f * s, d.Y * 0.6f, d.Z), hand + V(d.X * s, d.Y, d.Z)), 0.008f, 0.004f, vine, blend: 0.004f);
        });
        Stretch(b, V(1.25f, 1.25f, 1.25f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2713 Glacitadel, from Avalugg

    /// <summary>
    /// Glacitadel: Avalugg grown into a castle of ice, its slab walled round with towers at its corners and middle, each
    /// crowned with battlements, spires of ice between them, cracks of glowing blue down its dark body and legs, a
    /// wider jaw of icy fangs and spikes down its short tail.
    /// </summary>
    private static PokeBuilder Glacitadel()
    {
        var b = Become(Avalugg(), "Glacitadel", 1f);
        int head = BoneOf(b, "head");
        var ice = Rgb(214, 232, 248);
        var pale = Rgb(190, 226, 236);
        var body = Rgb(70, 100, 140);
        var crack = Rgb(80, 210, 250);
        Recolour(b, (Rgb(160, 190, 226), body), (Rgb(150, 130, 196), Rgb(170, 200, 230)));
        // Towers at the corners and the middle of the slab, each with battlements
        foreach (var (x, z, h) in new[] { (-0.18f, 0.18f, 0.14f), (0.18f, 0.18f, 0.14f), (-0.18f, -0.2f, 0.16f), (0.18f, -0.2f, 0.16f), (0f, -0.02f, 0.22f) })
        {
            var foot = V(x, 0.38f, z);
            var top = foot + V(0, h, 0);
            b.Limb(Body, foot, top, 0.045f, 0.04f, ice, Shell, 0.01f);
            b.Limb(Body, top, top + V(0, 0.025f, 0), 0.05f, 0.05f, ice, Shell, 0.004f);
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * MathF.Tau;
                b.Box(Body, top + V(MathF.Sin(a) * 0.04f, 0.04f, MathF.Cos(a) * 0.04f), V(0.011f, 0.013f, 0.011f), 0.002f, ice, V(0, a / Degree, 0), Shell, 0.002f);
            }
            b.PaintEll(Body, top + V(0, -0.04f, 0.04f), V(0.006f, 0.02f, 0.01f), Rgb(60, 90, 130), soft: 0.004f);
        }
        // Walls between the towers and spires of ice
        foreach (var x in new[] { -0.18f, 0.18f })
            b.Box(Body, V(x, 0.43f, -0.01f), V(0.02f, 0.05f, 0.18f), 0.008f, pale, default, Shell, 0.01f);
        foreach (var (x, z) in new[] { (-0.09f, 0.1f), (0.09f, -0.1f), (0.1f, 0.12f), (-0.1f, -0.14f) })
            b.Spike(Body, V(x, 0.39f, z), V(x, 0.52f, z), 0.025f, pale, 0.7f, Shell);
        // Cracks of glowing blue down the body and the legs
        foreach (var (x, z) in new[] { (0.2f, 0.06f), (0.19f, -0.08f), (-0.2f, 0.02f), (-0.19f, -0.1f) })
            b.Limb(Body, V(x, 0.28f, z), V(x * 1.02f, 0.2f, z + 0.03f), 0.006f, 0.005f, crack, Glow, 0.003f);
        foreach (var (name, z) in new[] { ("leg", 0.12f), ("hind", -0.14f) })
            PokeBuilder.Both(s =>
            {
                int leg = BoneOf(b, name + (s < 0 ? "L" : "R"));
                b.Limb(leg, V(0.16f * s, 0.18f, z + 0.055f), V(0.165f * s, 0.09f, z + 0.05f), 0.005f, 0.004f, crack, Glow, 0.002f);
            });
        // A wider jaw of icy fangs
        var c = V(0, 0.2f, 0.25f);
        for (int i = -3; i <= 3; i++)
        {
            b.Spike(head, c + V(i * 0.02f, -0.025f, 0.035f - 0.004f * Math.Abs(i)), c + V(i * 0.022f, -0.06f, 0.05f - 0.004f * Math.Abs(i)), 0.008f, White, 0.6f, Shell);
            b.Spike(head, c + V(i * 0.02f, 0.02f, 0.04f - 0.004f * Math.Abs(i)), c + V(i * 0.022f, 0.045f, 0.05f - 0.004f * Math.Abs(i)), 0.007f, ice, 0.6f, Shell);
        }
        // A short tail of ice spikes behind
        foreach (var (y, z, h) in new[] { (0.26f, -0.27f, 0.06f), (0.24f, -0.32f, 0.05f), (0.22f, -0.36f, 0.04f) })
            b.Spike(Body, V(0, y, z), V(0, y + h, z - 0.03f), 0.028f, pale, 0.6f, Shell);
        b.Limb(Body, V(0, 0.24f, -0.2f), V(0, 0.2f, -0.38f), 0.05f, 0.02f, body, Shell);
        Stretch(b, V(1.1f, 1.1f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2715 Sonarwing, from Noivern

    /// <summary>
    /// Sonarwing: Noivern grown greater and darker, black-violet with glowing green streaks on its chest, its ears grown
    /// into two great horns of sound ringed in green, the wings wider with rings of green in their membranes like sound
    /// spreading, and a third speaker at its tail's end.
    /// </summary>
    private static PokeBuilder Sonarwing()
    {
        var b = Become(Noivern(), "Sonarwing", 1f);
        int tail = BoneOf(b, "tail");
        var dark = Rgb(56, 40, 70);
        var green = Rgb(110, 250, 120);
        Unlift(b, V(0, 0.44f, 0));
        Recolour(b, (Rgb(56, 52, 66), dark), (Rgb(150, 100, 200), Rgb(96, 60, 120)), (Rgb(70, 210, 150), green), (Rgb(236, 236, 240), Rgb(70, 56, 90)));
        var c = V(0, 0.62f, 0.05f);
        // Rings of green in the wings' membranes
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var wrist = V(0.3f * s, 0.66f, -0.04f);
            foreach (var (t, at) in new[] { (0.5f, V(0.5f * s, 0.46f, -0.05f)), (0.5f, V(0.34f * s, 0.4f, -0.045f)) })
            {
                var mid = Vector3.Lerp(wrist, at, t);
                b.PaintTorus(wing, mid, 0.05f, 0.006f, green, V(90f, 0, 0));
                b.PaintTorus(wing, mid, 0.025f, 0.005f, green, V(90f, 0, 0));
            }
            // The horns of sound: flaring cones on the ears, rings within
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var ec = c + V(0.1f * s, 0.05f, -0.01f);
            var mouth = ec + V(0.06f * s, 0.1f, 0.0f);
            b.Limb(ear, ec, mouth, 0.03f, 0.06f, dark, blend: 0.01f);
            b.Cut(ear, mouth + V(0.012f * s, 0.02f, 0), V(0.05f, 0.025f, 0.05f), V(0, 0, -30f * s));
            b.Torus(ear, mouth, 0.055f, 0.007f, green, V(0, 0, -30f * s), mat: Glow, blend: 0.004f);
            b.Torus(ear, mouth - V(0.01f * s, 0.02f, 0), 0.035f, 0.005f, green, V(0, 0, -30f * s), mat: Glow, blend: 0.003f);
        });
        // Glowing streaks on the chest
        PokeBuilder.Both(s =>
        {
            foreach (var y in new[] { 0.46f, 0.41f })
                b.Ell(Body, V(0.045f * s, y, 0.055f), V(0.022f, 0.005f, 0.006f), green, V(0, 0, 20f * s), Glow, 0.003f);
        });
        // The speaker at the tail's end
        var end = V(0, 0.3f, -0.4f);
        var horn = end + V(0, 0.04f, -0.08f);
        b.Limb(tail, end, horn, 0.012f, 0.05f, dark, blend: 0.006f);
        b.Cut(tail, horn + V(0, 0.01f, -0.015f), V(0.04f, 0.04f, 0.02f), V(30f, 0, 0));
        b.Torus(tail, horn, 0.046f, 0.006f, green, V(90f - 25f, 0, 0), mat: Glow, blend: 0.003f);
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2718 Ecosentinel, from Zygarde

    /// <summary>
    /// Ecosentinel: Zygarde's lean hound of the 10% Forme grown into the guardian of the whole order, heavy and broad,
    /// its black hide a lattice of cells lit green from within, plates of armour on its shoulders and legs, a long tail
    /// curling up behind, a ring of five hexagonal plates standing round its head like a crown and a single red core
    /// glowing on its chest.
    /// </summary>
    private static PokeBuilder Ecosentinel()
    {
        var b = Become(ZygardeBuild(1), "Ecosentinel", 1f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var black = Rgb(54, 58, 48);
        var plate = Rgb(66, 84, 58);
        var green = Rgb(110, 240, 90);
        var red = Rgb(240, 60, 60);
        b.Sdf.RemoveAll(s => (s.Bone == Body && Is(s.Color, Rgb(120, 220, 90)) && s.Op == SdfOp.Add) || s.Bone == tail);
        Recolour(b, (Rgb(40, 44, 42), black));
        var bc = V(0, 0.3f, -0.02f);
        var br = V(0.06f, 0.07f, 0.16f);
        // A heavier body and chest
        b.Ell(Body, bc + V(0, 0.01f, 0.06f), V(0.09f, 0.09f, 0.1f), black, blend: 0.03f);
        b.Ell(Body, bc + V(0, 0, -0.06f), V(0.075f, 0.075f, 0.1f), black, blend: 0.03f);
        // Cells lit green from within over the flanks and back
        foreach (var (x, y, z) in new[] { (0.08f, 0.02f, 0.05f), (0.075f, 0.05f, -0.04f), (0.06f, 0.05f, -0.11f), (0.04f, 0.08f, 0.02f), (0.07f, -0.02f, -0.07f) })
            PokeBuilder.Both(s => HexCell(b, Body, bc + V(x * s, y, z), V(x * s, y, 0.02f), 0.018f, 0.006f, green));
        Make(b, s => Is(s.Color, green), Glow);
        // The red core on its chest
        b.Box(Body, bc + V(0, 0.0f, 0.15f), V(0.014f, 0.022f, 0.008f), 0.004f, red, V(0, 0, 45f), Glow, 0.004f);
        // Plates of armour on the shoulders
        PokeBuilder.Both(s => HexCell(b, Body, bc + V(0.075f * s, 0.06f, 0.1f), V(s, 0.6f, 0.3f), 0.04f, 0.01f, plate));
        // A long tail curling up behind
        var tp = Smooth(3, bc + V(0, 0.02f, -0.15f), bc + V(0, -0.04f, -0.28f), bc + V(0, 0.04f, -0.4f), bc + V(0, 0.2f, -0.42f), bc + V(0, 0.26f, -0.34f), bc + V(0, 0.22f, -0.28f));
        b.Tube(tail, tp, 0.04f, 0.01f, black, blend: 0f);
        for (int i = 2; i < tp.Length - 2; i += 2)
            b.Ell(tail, tp[i] + V(0, 0.028f * (1f - i / (float)tp.Length) + 0.008f, 0), V(0.01f, 0.006f, 0.012f), green, mat: Glow, blend: 0.003f);
        // The crown: five hexagonal plates standing round the head
        var neck = Smooth(3, bc + V(0, 0.03f, 0.13f), bc + V(0, 0.1f, 0.2f), bc + V(0, 0.14f, 0.26f));
        var c = neck[^1] + V(0, 0.02f, 0.04f);
        for (int i = 0; i < 5; i++)
        {
            float a = (i / 4f - 0.5f) * 160f * Degree;
            var d = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a), -0.3f));
            var at = c + V(0, 0, -0.04f) + d * 0.08f;
            b.Limb(head, c + V(0, 0, -0.03f) + d * 0.02f, at, 0.012f, 0.01f, black, Shell, 0.006f);
            HexCell(b, head, at, V(0, 0.1f, 1f), 0.035f, 0.007f, plate, 0f);
            HexCell(b, head, at + V(0, 0, 0.005f), V(0, 0.1f, 1f), 0.015f, 0.004f, green, 0f);
        }
        Make(b, s => s.Bone == head && Is(s.Color, green), Glow);
        Stretch(b, V(1.35f, 1.25f, 1.25f));
        return b;
    }
}
