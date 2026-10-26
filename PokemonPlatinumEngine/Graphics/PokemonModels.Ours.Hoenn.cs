using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 · D14), batch 3: Hoenn's, in our numbers' order
// (2000 + the forebear's National number). Each is sculpted from its forebear's own sculpt as the look of
// docs/plans/D14-proposals-hoenn.md and the reference pictures (docs/art/reference/gemini/hoenn) say: the forebear's
// build is taken whole (renamed, stretched, its colours changed, a part taken away) and the new features are added to
// it, with the helpers of PokemonModels.Ours.Kanto.cs. The forebears' own sculpts are never touched.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ 2262 Hyaenarch, from Mightyena

    /// <summary>
    /// Hyaenarch: Mightyena grown heavier in the shoulders, its grey darkened to a charcoal blotched in black, a long
    /// crest of black-violet shag from its brow down its back, a pale scar over one eye, the red eyes kept and a
    /// heavier tail.
    /// </summary>
    private static PokeBuilder Hyaenarch()
    {
        var b = Become(Mightyena(), "Hyaenarch", 0.9f);
        var charcoal = Rgb(70, 72, 84);
        var pale = Rgb(112, 114, 126);
        var shag = Rgb(36, 28, 46);
        var blotch = Rgb(30, 30, 38);
        Recolour(b, (Rgb(56, 56, 66), charcoal), (Rgb(176, 178, 188), pale));
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        // The old upright tail goes: a heavier one hangs low behind
        b.Sdf.RemoveAll(s => s.Bone == tail);
        // Heavier in the forequarters: the body broadened at the shoulders
        Grow(b, V(1.18f, 1.14f, 1.05f), V(0, 0.3f, 0.06f), s => s.Bone == Body && s.Shape == SdfShape.Ellipsoid && s.Size.Z > 0.15f, _ => false);
        b.Ell(Body, V(0, 0.36f, 0.08f), V(0.11f, 0.1f, 0.1f), charcoal, blend: 0.04f);
        b.PaintEll(Body, V(0, 0.25f, 0.08f), V(0.08f, 0.06f, 0.1f), pale);
        foreach (var leg in new[] { "legL", "legR", "hindL", "hindR" })
        {
            int bone = BoneOf(b, leg);
            float s = leg.EndsWith('L') ? -1f : 1f;
            float z = leg.StartsWith("leg") ? 0.12f : -0.14f;
            b.PaintEll(bone, V(0.07f * s, 0.08f, z + 0.01f), V(0.05f, 0.11f, 0.06f), blotch);
        }
        // Black blotches and stripes down its flanks
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.34f, 0.08f), (0.3f, 0.0f), (0.34f, -0.08f), (0.28f, -0.14f), (0.36f, 0.16f), (0.29f, 0.12f) })
                b.PaintEll(Body, V(0.1f * s, y, z), V(0.03f, 0.05f, 0.018f), blotch, V(0, 0, 20f * s));
        });
        // The crest: a long shag of black-violet from the brow down the spine to the rump, high over the shoulders
        b.Ell(Body, V(0, 0.42f, 0.02f), V(0.055f, 0.06f, 0.17f), shag, V(-8f, 0, 0), blend: 0.03f);
        for (int i = 0; i < 10; i++)
        {
            float z = 0.16f - i * 0.036f;
            float h = 0.07f + 0.06f * MathF.Sin((i + 1) / 11f * MathF.PI);
            float y = 0.44f - 0.03f * i / 9f;
            b.Spike(Body, V(0, y - 0.03f, z), V(0, y + h, z - 0.08f), 0.045f, shag, 0.4f, blend: 0.018f);
            PokeBuilder.Both(s => b.Spike(Body, V(0.035f * s, y - 0.04f, z), V(0.08f * s, y + h * 0.6f, z - 0.07f), 0.034f, shag, 0.4f, blend: 0.016f));
        }
        foreach (var (y, z, h) in new[] { (0.5f, 0.2f, 0.07f), (0.47f, 0.16f, 0.08f) })
            b.Spike(head, V(0, y, z), V(0, y + h, z - 0.07f), 0.026f, shag, 0.45f, blend: 0.012f);
        // Rounder hyena's ears
        foreach (var ear in new[] { "earL", "earR" })
        {
            int bone = BoneOf(b, ear);
            float s = ear.EndsWith('L') ? -1f : 1f;
            Grow(b, V(1.3f, 0.6f, 1.3f), V(0.045f * s, 0.5f, 0.18f), x => x == bone);
        }
        // The heavier tail, hanging low in a shaggy plume
        var tp = Smooth(3, V(0, 0.36f, -0.17f), V(0, 0.33f, -0.27f), V(0, 0.24f, -0.34f), V(0, 0.13f, -0.37f));
        b.Tube(tail, tp, 0.045f, 0.06f, shag, blend: 0.012f);
        FurTufts(b, tail, tp[^1], V(0.05f, 0.06f, 0.05f), 7, 0.06f, 0.02f, shag, -0.4f, -0.9f, 0.1f);
        FurTufts(b, tail, tp[6], V(0.05f, 0.05f, 0.05f), 6, 0.05f, 0.018f, shag, -0.3f, -0.4f, 0.2f);
        // A pale scar over the right eye
        b.Mark(head, V(0.04f, 0.485f, 0.268f), V(0.6f, 0.35f, 0.75f), 0.006f, 0.022f, Rgb(214, 196, 200), MarkShape.Bar, 25f);
        return b;
    }

    // ------------------------------------------------------------------ 2264 Streakoon, from Linoone

    /// <summary>
    /// Streakoon: Linoone longer and lower and darkened to a burrower's brown, its stripes pulled into straight tan
    /// bands along its back, a black mask across its eyes on a tan face, the tail banded, and broad digging claws on
    /// its forefeet.
    /// </summary>
    private static PokeBuilder Streakoon()
    {
        var b = Become(Linoone(), "Streakoon", 0.82f);
        var brown = Rgb(92, 72, 52);
        var tan = Rgb(186, 156, 108);
        var mask = Rgb(40, 32, 28);
        var claw = Rgb(56, 50, 48);
        Recolour(b, (Rgb(238, 228, 206), brown), (Rgb(150, 116, 88), tan));
        // A badger's build: the body and legs much stouter, the head broader
        Grow(b, V(1.45f, 1.35f, 1.05f), V(0, 0.125f, 0), s => s.Bone == Body, bone => bone == Body);
        foreach (var leg in new[] { "legL", "legR", "hindL", "hindR" })
        {
            int bone = BoneOf(b, leg);
            float s = leg.EndsWith('L') ? -1f : 1f;
            float z = leg.StartsWith("leg") ? 0.15f : -0.17f;
            Grow(b, V(1.6f, 1f, 1.5f), V(0.065f * s, 0f, z), x => x == bone);
            Move(b, x => x == bone, V(0.02f * s, 0, 0));
        }
        int skull = BoneOf(b, "head");
        Grow(b, V(1.25f, 1.2f, 1.1f), V(0, 0.145f, 0.22f), x => x == skull || b.Model.Skeleton[x].Name.StartsWith("ear"));
        Stretch(b, V(1.08f, 0.94f, 1.12f));
        // Broad digging claws on the forefeet: three long hooked claws each
        foreach (var leg in new[] { "legL", "legR" })
        {
            int bone = BoneOf(b, leg);
            float s = leg.EndsWith('L') ? -1f : 1f;
            var foot = V(0.097f * s, 0.018f, 0.15f * 1.12f + 0.045f);
            b.Ell(bone, foot - V(0, 0, 0.02f), V(0.038f, 0.02f, 0.036f), brown, blend: 0.012f);
            for (int i = -1; i <= 1; i++)
            {
                var root = foot + V(0.017f * i, 0.002f, 0.014f);
                b.Tube(bone, Smooth(3, root, root + V(0.005f * i, 0.004f, 0.04f), root + V(0.008f * i, -0.016f, 0.065f)), 0.011f, 0.004f, claw, Shell, 0f);
            }
        }
        // A tan face with a black mask across the eyes and a tan blaze up the brow
        int head = skull;
        var c = V(0, 0.157f, 0.275f) * V(1.08f, 0.94f, 1.12f);
        b.PaintEll(head, c + V(0, -0.02f, 0.06f), V(0.06f, 0.035f, 0.07f), tan);
        b.PaintEll(head, c + V(0, 0.01f, 0.035f), V(0.09f, 0.018f, 0.06f), mask);
        // A banded tail
        int tail = BoneOf(b, "tail");
        for (int i = 0; i < 3; i++)
        {
            var at = V(0, 0.158f + 0.02f * i, -0.27f - 0.06f * i) * V(1.08f, 0.94f, 1.12f);
            b.PaintTorus(tail, at, 0.04f - 0.004f * i, 0.012f, mask, V(65f, 0, 0));
        }
        return b;
    }

    // ------------------------------------------------------------------ 2277 Swiftarrow, from Swellow

    /// <summary>
    /// Swiftarrow: Swellow longer and narrower, its red breast running down to a point and up into the crest, its
    /// wings longer and its two tail streamers twice as long, each ending in a fletching of navy, red and white.
    /// </summary>
    private static PokeBuilder Swiftarrow()
    {
        var b = Become(Swellow(), "Swiftarrow", 0.95f);
        var navy = Rgb(40, 44, 92);
        var red = Rgb(196, 40, 70);
        var white = Rgb(244, 244, 248);
        Recolour(b, (Rgb(44, 54, 100), navy), (Rgb(212, 72, 86), red), (Rgb(214, 76, 90), Rgb(150, 116, 108)));
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        // Longer wings, folded back past the rump
        foreach (var w in new[] { "wingL", "wingR" })
        {
            int bone = BoneOf(b, w);
            float s = w.EndsWith('L') ? -1f : 1f;
            Grow(b, V(1f, 1f, 1.45f), V(0.08f * s, 0.3f, 0.03f), x => x == bone);
        }
        Stretch(b, V(0.9f, 1.05f, 1.1f));
        // The red breast runs down to a point over the white belly
        b.PaintEll(Body, V(0, 0.29f, 0.11f), V(0.07f, 0.07f, 0.06f), red);
        b.PaintEll(Body, V(0, 0.24f, 0.14f), V(0.025f, 0.05f, 0.04f), red);
        // A red tip to the crest
        int head = BoneOf(b, "head");
        var c = V(0, 0.42f, 0.1f) * V(0.9f, 1.05f, 1.1f);
        foreach (var x in new[] { -0.018f, 0.018f })
            b.PaintEll(head, c + V(x * 2.2f, 0.085f, -0.15f), V(0.03f, 0.03f, 0.05f), red);
        // Two streamers twice as long, each ending in a fletching
        var root = V(0, 0.23f, -0.14f);
        PokeBuilder.Both(s =>
        {
            var tip = V(0.15f * s, 0.06f, -0.98f);
            var start = root + V(0.012f * s, 0, 0);
            b.Tube(tail, Smooth(3, start, Vector3.Lerp(start, tip, 0.5f) + V(0, 0.02f, 0), tip), 0.022f, 0.009f, navy, blend: 0.006f);
            var d = Vector3.Normalize(tip - start);
            var side = Vector3.Normalize(Vector3.Cross(V(0, 1f, 0), d));
            b.PaintEll(tail, Vector3.Lerp(start, tip, 0.45f), V(0.025f, 0.025f, 0.06f), red, Euler(d));
            for (int i = 0; i < 3; i++)
            {
                var at = tip - d * (0.02f + 0.045f * i);
                var col = i == 0 ? navy : i == 1 ? red : white;
                foreach (float k in new[] { -1f, 1f })
                    Blade(b, tail, at, at - d * 0.055f + side * k * 0.055f, 0.024f, col, V(0, 1f, 0), 0.2f);
            }
            b.Spike(tail, tip - d * 0.01f, tip + d * 0.03f, 0.014f, navy, 0.4f);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2284 Ocellwing, from Masquerain

    /// <summary>
    /// Ocellwing: Masquerain deepened to a royal blue with a pale ruff, its clear wings grown broad and blue and a
    /// third pair added, each wing staring with a false eye of black ringed in sky blue, as its antennae's discs do.
    /// </summary>
    private static PokeBuilder Ocellwing()
    {
        var b = Become(Masquerain(), "Ocellwing", 0.9f);
        var blue = Rgb(48, 74, 176);
        var wingBlue = Rgb(56, 86, 196);
        var sky = Rgb(110, 206, 240);
        var ruff = Rgb(196, 210, 236);
        var dark = Rgb(20, 22, 44);
        var lift = Shifted(b, "head", V(0, 0.34f, 0.05f));
        Recolour(b, (Rgb(236, 240, 246), blue), (Rgb(150, 70, 90), dark), (Rgb(242, 152, 96), sky));
        // The old clear wings go: six broad blue ones take their place
        b.Sdf.RemoveAll(s => b.Model.Skeleton[s.Bone].Name.StartsWith("wing"));
        int head = BoneOf(b, "head");
        var c = V(0, 0.36f, 0.06f) + lift;
        b.PaintEll(head, c + V(0, 0.01f, 0.01f), V(0.05f, 0.05f, 0.05f), blue);
        // A pale ruff round its neck and a furred, banded abdomen
        b.Ell(Body, V(0, 0.33f, 0.03f) + lift, V(0.06f, 0.035f, 0.05f), ruff, blend: 0.02f);
        b.Ell(Body, V(0, 0.24f, -0.03f) + lift, V(0.04f, 0.07f, 0.04f), blue, V(25f, 0, 0), blend: 0.02f);
        foreach (float y in new[] { 0.2f, 0.24f, 0.28f })
            b.PaintTorus(Body, V(0, y, -0.03f + (0.28f - y) * 0.4f) + lift, 0.04f, 0.006f, dark, V(25f, 0, 0));
        PokeBuilder.Both(s =>
        {
            // The forebear's two wing bones on this side carry the upper two pairs, a new one the third
            var old = Enumerable.Range(0, b.Model.Skeleton.Count).Where(i => b.Model.Skeleton[i].Name == (s < 0 ? "wingL" : "wingR")).ToArray();
            int n = 0;
            foreach (var (z, elev, len, wide) in new[] { (0.015f, 48f, 0.17f, 0.085f), (0f, 8f, 0.17f, 0.08f), (-0.02f, -36f, 0.13f, 0.07f) })
            {
                var pivot = V(0.04f * s, 0.3f, z) + lift;
                int wing = n < old.Length ? old[n] : b.Wing(s, pivot);
                n++;
                float a = elev * Degree;
                var dir = Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), -0.15f));
                var turn = Euler(dir);
                var mid = pivot + dir * len;
                b.Ell(wing, mid, V(wide, len, 0.009f), wingBlue, turn, Shell, 0.01f);
                b.PaintEll(wing, pivot + dir * len * 1.75f, V(wide * 0.9f, len * 0.35f, 0.03f), blue, turn);
                var q = Quaternion.CreateFromYawPitchRoll(turn.Y * Degree, turn.X * Degree, turn.Z * Degree);
                var face = Vector3.Normalize(Vector3.Transform(V(0, 0, 1f), q));
                if (face.Z < 0) face = -face;
                var spot = mid + dir * len * 0.2f + face * 0.008f;
                b.Mark(wing, spot, face, wide * 0.42f, wide * 0.42f, sky, MarkShape.Disc);
                b.Mark(wing, spot + face * 0.001f, face, wide * 0.26f, wide * 0.26f, dark, MarkShape.Disc);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2286 Sporepugil, from Breloom

    /// <summary>
    /// Sporepugil: Breloom taller, olive and leaf-clad, under a broad brown cap whose rim is dotted with glowing spore
    /// pockets, its hands grown into great lumpy gloves of fungus and its tail ending in a knobbed spore pod.
    /// </summary>
    private static PokeBuilder Sporepugil()
    {
        var b = Become(Breloom(), "Sporepugil", 0.92f);
        var olive = Rgb(96, 128, 78);
        var cream = Rgb(214, 206, 170);
        var cap = Rgb(128, 120, 86);
        var gill = Rgb(196, 188, 150);
        var spore = Rgb(180, 244, 170);
        var glove = Rgb(170, 194, 128);
        var pod = Rgb(150, 144, 100);
        Recolour(b, (Rgb(98, 160, 96), olive), (Rgb(240, 228, 196), cream), (Rgb(222, 82, 82), Rgb(110, 92, 70)));
        int tail = BoneOf(b, "tail");
        // The seeds at its tail's end go: a spore pod takes their place
        b.Sdf.RemoveAll(s => s.Bone == tail && s.Shape == SdfShape.Ellipsoid);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == tail)) s.Color = olive;
        b.Sdf.Changed();
        Stretch(b, V(1.05f, 1.16f, 1.05f));
        var k = V(1.05f, 1.16f, 1.05f);
        // A cream chest and belly
        b.PaintEll(Body, V(0, 0.33f, 0.07f) * k, V(0.06f, 0.12f, 0.05f), cream);
        // The broad cap, its gills under its rim and glowing spore pockets round it
        int head = BoneOf(b, "head");
        var top = V(0, 0.57f, 0.0f) * k;
        b.Ell(head, top, V(0.19f, 0.045f, 0.17f), cap, mat: Leaf, blend: 0.02f);
        b.Ell(head, top + V(0, 0.035f, -0.01f), V(0.12f, 0.04f, 0.11f), cap, mat: Leaf, blend: 0.03f);
        b.PaintEll(head, top - V(0, 0.04f, 0), V(0.18f, 0.02f, 0.16f), gill);
        for (int i = 0; i < 10; i++)
        {
            float a = MathF.Tau * i / 10f;
            b.Ell(head, top + V(MathF.Sin(a) * 0.16f, 0.012f, MathF.Cos(a) * 0.14f), V(0.014f, 0.012f, 0.014f), spore, mat: Glow, blend: 0.005f);
        }
        foreach (var (x, z) in new[] { (0.06f, 0.05f), (-0.07f, 0.0f), (0.02f, -0.08f), (-0.03f, 0.08f), (0.09f, -0.04f) })
            b.Ell(head, top + V(x, 0.065f, z), V(0.012f, 0.008f, 0.012f), spore, mat: Glow, blend: 0.004f);
        // Gloves of fungus: lumpy clusters over its hands
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = V(0.14f * s, 0.3f, 0.12f) * k + V(0.01f * s, 0, 0.01f);
            b.Ell(arm, hand, V(0.06f, 0.055f, 0.06f), glove, mat: Leaf, blend: 0.02f);
            foreach (var o in new[] { V(0.04f, 0.03f, 0.03f), V(-0.03f, 0.04f, 0.02f), V(0.03f, -0.03f, 0.04f), V(-0.04f, -0.02f, 0.03f), V(0, 0.01f, 0.06f), V(0.01f, 0.05f, -0.02f) })
                b.Ell(arm, hand + V(o.X * s, o.Y, o.Z), V(0.032f, 0.03f, 0.032f), PixelCanvas.Shadow(glove, 0.08f), mat: Leaf, blend: 0.012f);
            b.Limb(arm, V(0.1f * s, 0.32f, 0.07f) * k, hand, 0.03f, 0.04f, glove, Leaf, 0.02f);
        });
        // The spore pod at its tail's end, knobbed all over
        var end = V(0, 0.34f, -0.36f) * k + V(0, 0.04f, -0.02f);
        b.Limb(tail, V(0, 0.34f, -0.36f) * k, end, 0.02f, 0.03f, olive, blend: 0.01f);
        b.Ell(tail, end + V(0, 0.05f, 0), V(0.075f, 0.085f, 0.07f), pod, mat: Leaf, blend: 0.015f);
        foreach (var o in new[] { V(0.05f, 0.1f, 0.02f), V(-0.05f, 0.08f, 0.03f), V(0.02f, 0.13f, -0.03f), V(-0.03f, 0.03f, -0.05f), V(0.06f, 0.03f, -0.02f), V(0f, 0.06f, 0.06f) })
            b.Ell(tail, end + o, V(0.03f, 0.03f, 0.03f), PixelCanvas.Shadow(pod, 0.08f), mat: Leaf, blend: 0.012f);
        return b;
    }

    // ------------------------------------------------------------------ 2279 Pelicaptain, from Pelipper

    /// <summary>
    /// Pelicaptain: Pelipper in a captain's dress, its head white under a peaked cap of navy feathers with a black
    /// visor and a gold badge, a navy coat with gold epaulettes, its bill rimmed in blue, its wings as wide again and
    /// tipped navy, its feet orange.
    /// </summary>
    private static PokeBuilder Pelicaptain()
    {
        var b = Become(Pelipper(), "Pelicaptain", 0.92f);
        var navy = Rgb(34, 46, 92);
        var gold = Rgb(240, 186, 58);
        var orange = Rgb(232, 132, 60);
        var lift = Shifted(b, "head", V(0, 0.5f, 0.0f));
        Recolour(b, (Rgb(64, 150, 230), navy));
        foreach (var s in b.Sdf.Shapes.Where(s => b.Model.Skeleton[s.Bone].Name is "legL" or "legR")) s.Color = orange;
        b.Sdf.Changed();
        int head = BoneOf(b, "head");
        // The head white again under the cap
        b.PaintEll(head, V(0, 0.65f, -0.03f) + lift, V(0.115f, 0.065f, 0.115f), White);
        // Wings as wide again, navy over half their length
        foreach (var w in new[] { "wingL", "wingR" })
        {
            int bone = BoneOf(b, w);
            float s = w.EndsWith('L') ? -1f : 1f;
            Grow(b, V(1.35f, 1.3f, 1.45f), V(0.11f * s, 0.46f, -0.08f) + lift, x => x == bone);
            b.PaintEll(bone, V(0.2f * s, 0.24f, -0.12f) + lift, V(0.09f, 0.12f, 0.17f), navy);
        }
        // The cap: a crown of navy feathers, a black band and visor, a gold badge
        var cap = V(0, 0.69f, -0.02f) + lift;
        b.Ell(head, cap, V(0.115f, 0.05f, 0.12f), navy, V(-8f, 0, 0), blend: 0.012f);
        b.Ell(head, cap + V(0, 0.03f, -0.02f), V(0.095f, 0.035f, 0.1f), navy, blend: 0.02f);
        b.PaintTorus(head, cap - V(0, 0.03f, 0), 0.112f, 0.014f, Rgb(30, 30, 38), V(-8f, 0, 0), 1f, 1.05f);
        b.Ell(head, V(0, 0.665f, 0.09f) + lift, V(0.08f, 0.012f, 0.055f), Rgb(30, 30, 38), V(-12f, 0, 0), Shell, 0.006f);
        b.Ell(head, V(0, 0.705f, 0.1f) + lift, V(0.018f, 0.02f, 0.01f), gold, mat: Shell, blend: 0.004f);
        // A blue rim round the bill
        b.PaintTorus(head, V(0, 0.565f, 0.17f) + lift, 0.08f, 0.009f, Rgb(40, 86, 170), default, 1f, 1.95f);
        // The coat: navy lapels down the breast, gold epaulettes fringed at the shoulders
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.09f * s, 0.42f, 0.0f) + lift, V(0.05f, 0.1f, 0.07f), navy, V(0, 0, 18f * s));
            var shoulder = V(0.12f * s, 0.49f, -0.07f) + lift;
            b.Ell(Body, shoulder, V(0.05f, 0.02f, 0.05f), gold, mat: Shell, blend: 0.01f);
            for (int i = -1; i <= 1; i++)
                b.Spike(Body, shoulder + V(0.035f * s, -0.005f, 0.025f * i), shoulder + V(0.05f * s, -0.045f, 0.03f * i), 0.008f, gold, mat: Shell, blend: 0.004f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2291 Shinobuzz, from Ninjask

    /// <summary>
    /// Shinobuzz: Ninjask darkened nearly to black with its yellow kept as a mask across its eyes and thin stripes on a
    /// longer abdomen, its antennae long and curved, a third, smaller pair of clear wings, and a curved blade on each
    /// foreleg, edged in gold.
    /// </summary>
    private static PokeBuilder Shinobuzz()
    {
        var b = Become(Ninjask(), "Shinobuzz", 0.82f);
        var black = Rgb(36, 36, 48);
        var yellow = Rgb(230, 196, 60);
        var clear = Rgb(226, 230, 242);
        var lift = Shifted(b, "head", V(0, 0.34f, 0.06f));
        Recolour(b, (Rgb(48, 46, 54), black), (Rgb(214, 60, 70), Rgb(206, 210, 228)), (Rgb(120, 120, 130), black));
        // The yellow band round its thorax goes thin
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Shape == SdfShape.Torus);
        // A longer abdomen behind, banded thinly in yellow
        var ab = V(0, 0.22f, -0.15f) + lift;
        b.Ell(Body, ab, V(0.065f, 0.06f, 0.12f), black, V(18f, 0, 0), Shell, 0.03f);
        foreach (float z in new[] { -0.11f, -0.15f, -0.19f })
            b.PaintTorus(Body, V(0, 0.22f + (z + 0.15f) * 0.3f, z) + lift, 0.06f, 0.005f, yellow, V(72f, 0, 0));
        // A third, smaller pair of wings
        PokeBuilder.Both(s =>
        {
            var root = V(0.05f * s, 0.27f, -0.04f) + lift;
            int wing = b.Wing(s, root);
            var tip = root + Vector3.Normalize(V(0.9f * s, 0.15f, -0.5f)) * 0.14f;
            Blade(b, wing, root, tip, 0.035f, clear, V(0, 0.3f, 1f), 0.1f);
        });
        // Long curved antennae
        int head = BoneOf(b, "head");
        var c = V(0, 0.38f, 0.09f) + lift;
        b.Sdf.RemoveAll(s => s.Bone == head && s.Shape == SdfShape.RoundCone && s.A.Y > c.Y + 0.02f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.022f * s, 0.035f, 0.01f), c + V(0.05f * s, 0.12f, 0.03f), c + V(0.1f * s, 0.18f, -0.04f), c + V(0.13f * s, 0.19f, -0.1f)), 0.009f, 0.005f, black, blend: 0.004f));
        // A curved blade on each foreleg, black edged in gold
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            var foot = V(0.06f * s, 0.14f, 0.06f) + lift;
            b.Limb(leg, V(0.04f * s, 0.22f, 0.02f) + lift, foot, 0.016f, 0.014f, black, Shell);
            var path = Smooth(3, foot, foot + V(0.02f * s, 0.06f, 0.08f), foot + V(0.03f * s, 0.0f, 0.15f), foot + V(0.03f * s, -0.1f, 0.13f));
            for (int i = 1; i < path.Length; i++)
            {
                float t = i / (float)(path.Length - 1);
                Blade(b, leg, path[i - 1], path[i], 0.022f * (1f - 0.7f * t) + 0.005f, black, V(s, 0, 0), 0.3f, Shell);
            }
            b.Tube(leg, path.Select(p => p + V(0, -0.006f, -0.01f)).ToArray(), 0.006f, 0.003f, yellow, Shell, 0.003f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2297 Yokozan, from Hariyama

    /// <summary>
    /// Yokozan: Hariyama grown into a grand champion, its hide a deep red over a cream belly, a boar's snout with
    /// tusks, its hair tied up in a topknot, and round its waist the white rope belt of a yokozuna with zigzag paper
    /// streamers hanging from its front.
    /// </summary>
    private static PokeBuilder Yokozan()
    {
        var b = Become(Hariyama(), "Yokozan", 1f);
        var red = Rgb(150, 44, 56);
        var dark = Rgb(120, 34, 46);
        var navy = Rgb(40, 42, 76);
        var cream = Rgb(236, 226, 204);
        var hair = Rgb(92, 72, 54);
        var rope = Rgb(246, 244, 238);
        var gold = Rgb(214, 170, 72);
        Recolour(b, (Rgb(234, 226, 214), red), (Rgb(60, 66, 100), navy), (Rgb(244, 210, 92), navy), (Rgb(236, 140, 104), dark));
        Stretch(b, V(1.08f, 1.08f, 1.08f));
        var k = V(1.08f, 1.08f, 1.08f);
        // A cream belly
        b.PaintEll(Body, V(0, 0.4f, 0.1f) * k, V(0.13f, 0.13f, 0.08f), cream);
        // The rope belt, its strands twisted, and the apron's gold edge
        var belt = V(0, 0.3f, 0) * k;
        b.Torus(Body, belt, 0.19f * 1.08f, 0.026f, rope, default, 1f, 0.86f, Fur, 0.01f);
        for (int i = 0; i < 18; i++)
        {
            float a = MathF.Tau * i / 18f;
            var at = belt + V(MathF.Sin(a) * 0.205f, 0, MathF.Cos(a) * 0.205f * 0.86f);
            b.Ell(Body, at, V(0.02f, 0.03f, 0.02f), rope, V(0, -a / Degree, 35f), blend: 0.008f);
        }
        foreach (var x in new[] { -0.07f, 0f, 0.07f })
            b.PaintEll(Body, V(x * 1.2f, 0.18f, 0.16f) * k, V(0.03f, 0.08f, 0.04f), gold);
        // The paper streamers: three zigzags hanging from the front of the rope
        foreach (var x in new[] { -0.08f, 0f, 0.08f })
        {
            var top = V(x, 0.29f, 0.175f) * k;
            for (int i = 0; i < 4; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                var from = top + V(0.008f * side, -0.035f * i, 0.002f * i);
                Blade(b, Body, from, from + V(-0.016f * side, -0.04f, 0.004f), 0.018f, rope, V(0, 0, 1f), 0.2f);
            }
        }
        // A boar's snout with tusks, a fierce brow, pointed ears and a topknot of brown hair
        int head = BoneOf(b, "head");
        var c = V(0, 0.6f, 0.06f) * k;
        b.Model.Decals.RemoveAll(d => !d.IsEye && d.Bone == head);
        b.Ell(head, c + V(0, -0.03f, 0.085f), V(0.036f, 0.028f, 0.035f), Rgb(170, 110, 112), blend: 0.015f);
        b.PaintEll(head, c + V(0, -0.03f, 0.12f), V(0.03f, 0.022f, 0.01f), Rgb(80, 50, 52));
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.028f * s, -0.05f, 0.1f), c + V(0.045f * s, -0.015f, 0.12f), 0.009f, rope, mat: Shell, blend: 0.004f);
            b.Spike(head, c + V(0.07f * s, 0.05f, -0.01f), c + V(0.11f * s, 0.11f, -0.03f), 0.022f, red, 0.5f);
        });
        b.Ell(head, c + V(0, 0.07f, -0.02f), V(0.085f, 0.04f, 0.075f), hair, blend: 0.02f);
        b.Ell(head, c + V(0, 0.115f, -0.05f), V(0.04f, 0.04f, 0.04f), hair, blend: 0.012f);
        b.Ell(head, c + V(0, 0.155f, -0.06f), V(0.03f, 0.035f, 0.03f), hair, blend: 0.01f);
        b.PaintTorus(head, c + V(0, 0.13f, -0.055f), 0.028f, 0.006f, Rgb(200, 180, 120));
        return b;
    }

    // ------------------------------------------------------------------ 2301 Felinelle, from Delcatty

    /// <summary>
    /// Felinelle: Delcatty longer and sleeker, its coat lilac and its collar grown into a great cream ruff, its ears
    /// tall and upright, cream paws, a pale crescent on its brow and its tail ending in two plumes.
    /// </summary>
    private static PokeBuilder Felinelle()
    {
        var b = Become(Delcatty(), "Felinelle", 0.86f);
        var lilac = Rgb(200, 182, 216);
        var cream = Rgb(244, 234, 200);
        var deep = Rgb(168, 146, 194);
        Recolour(b, (Rgb(248, 230, 176), lilac), (Rgb(160, 110, 186), lilac));
        int ruff = BoneOf(b, "ruff");
        int tail = BoneOf(b, "tail");
        int head = BoneOf(b, "head");
        // The old ruff with its beads, the cap's swept points and the tail's leaf go
        b.Sdf.RemoveAll(s => s.Bone == ruff || (s.Bone == tail && s.Shape == SdfShape.Ellipsoid) || b.Model.Skeleton[s.Bone].Name.StartsWith("ear"));
        // A great cream ruff round its neck and down its breast
        b.Ell(ruff, V(0, 0.33f, 0.08f), V(0.11f, 0.08f, 0.08f), cream, blend: 0.02f);
        FurTufts(b, ruff, V(0, 0.32f, 0.08f), V(0.1f, 0.08f, 0.08f), 14, 0.06f, 0.02f, cream, -0.2f, -0.5f, 0.4f);
        // Cream paws
        foreach (var leg in new[] { "legL", "legR", "hindL", "hindR" })
        {
            int bone = BoneOf(b, leg);
            float s = leg.EndsWith('L') ? -1f : 1f;
            float z = leg.StartsWith("leg") ? 0.08f : -0.1f;
            b.PaintEll(bone, V(0.04f * s, 0.02f, z + 0.015f), V(0.03f, 0.05f, 0.04f), cream);
        }
        // Tall upright ears
        var c = V(0, 0.44f, 0.12f);
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            CatEar(b, ear, c + V(0.045f * s, 0.045f, -0.015f), c + V(0.085f * s, 0.2f, -0.03f), 0.036f, lilac, cream);
        });
        // A pale crescent on its brow
        b.Mark(head, On(c, V(0.07f, 0.065f, 0.065f), 0, 0.475f), V(0, 0.45f, 1f), 0.016f, 0.01f, Rgb(250, 244, 220), MarkShape.Smile);
        // Two plumes at its tail's end
        var end = V(0, 0.52f, -0.14f);
        PokeBuilder.Both(s =>
        {
            var tip = end + V(0.07f * s, 0.12f, -0.04f);
            b.Ell(tail, Vector3.Lerp(end, tip, 0.55f), V(0.03f, 0.075f, 0.022f), cream, Euler(tip - end), blend: 0.01f);
            b.PaintEll(tail, Vector3.Lerp(end, tip, 0.5f), V(0.012f, 0.06f, 0.03f), deep, Euler(tip - end));
            FurTufts(b, tail, Vector3.Lerp(end, tip, 0.75f), V(0.025f, 0.04f, 0.02f), 4, 0.035f, 0.01f, cream, -0.3f, 0.2f, 0.2f);
        });
        Stretch(b, V(0.95f, 1.1f, 1.18f));
        return b;
    }

    // ------------------------------------------------------------------ 2302 Gemwraith, from Sableye

    /// <summary>
    /// Gemwraith: Sableye grown larger and darker, a cluster of glowing crystals, blue and red, grown out of its back
    /// and shoulders, the gem on its chest bigger and its claws longer.
    /// </summary>
    private static PokeBuilder Gemwraith()
    {
        var b = Become(Sableye(), "Gemwraith", 0.8f);
        var purple = Rgb(72, 56, 114);
        var blue = Rgb(80, 150, 240);
        var red = Rgb(228, 56, 84);
        Recolour(b, (Rgb(110, 84, 168), purple));
        Stretch(b, V(1.1f, 1.15f, 1.1f));
        var k = V(1.1f, 1.15f, 1.1f);
        // A bigger gem on its chest
        var bc = V(0, 0.23f, 0.01f) * k;
        var br = V(0.075f, 0.09f, 0.065f) * k;
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Material == Glow);
        Gem(b, Body, Out(bc, br, default, V(0, 0.1f, 1f)) + V(0, 0, 0.004f), V(0, 0.1f, 1f), 0.036f, red);
        // Crystals out of its back and shoulders, tall in the middle and short at the edges
        var rnd = new GenomeRandom(2302);
        for (int i = 0; i < 16; i++)
        {
            float x = (i % 8 - 3.5f) / 3.5f;
            float row = i < 8 ? 0f : 1f;
            var root = V(x * 0.07f, 0.3f - row * 0.06f - MathF.Abs(x) * 0.03f, -0.035f - row * 0.01f) * k;
            float h = (0.18f - MathF.Abs(x) * 0.08f - row * 0.05f) * (0.8f + 0.4f * rnd.Float());
            var tip = root + Vector3.Normalize(V(x * 0.6f, 1f, -0.5f - 0.3f * rnd.Float())) * h;
            b.Spike(Body, root, tip, 0.022f + 0.008f * rnd.Float(), (i % 3 == 1) ? red : blue, 0.7f, Glow, 0.006f);
        }
        // Longer claws on its hands
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = V(0.15f * s, 0.15f, 0.08f) * k;
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, hand + V(0.012f * i, -0.01f, 0.01f), hand + V((0.03f + 0.012f * i) * s, -0.09f, 0.05f), 0.01f, Rgb(40, 34, 60), mat: Shell, blend: 0.004f);
            var shoulder = V(0.07f * s, 0.32f, 0.0f) * k;
            b.Spike(Body, shoulder, shoulder + V(0.04f * s, 0.06f, -0.01f), 0.016f, blue, 0.7f, Glow, 0.005f);
        });
        return b;
    }

    // ------------------------------------------------------------------ 2303 Deceivore, from Mawile

    /// <summary>
    /// Deceivore: Mawile taller, its dress longer and striped down the front in black, the great jaw behind its head
    /// longer, fanged along its edge, and two curved horns at the jaw's root.
    /// </summary>
    private static PokeBuilder Deceivore()
    {
        var b = Become(Mawile(), "Deceivore", 0.88f);
        var black = Rgb(56, 54, 62);
        int maw = BoneOf(b, "maw");
        int head = BoneOf(b, "head");
        var c = V(0, 0.36f, 0.02f);
        var root = c + V(0, 0.04f, -0.04f);
        // The jaw grown longer
        Grow(b, V(1.25f, 1.45f, 1.45f), root, x => x == maw);
        var end = b.Sdf.Shapes.Last(s => s.Bone == maw && s.Shape == SdfShape.RoundCone).B;
        var mouth = b.Sdf.Shapes.Where(s => s.Bone == maw && s.Shape == SdfShape.Ellipsoid && s.Op == SdfOp.Add).OrderByDescending(s => s.Size.Length()).First().A;
        var dir = Vector3.Normalize(mouth - end);
        // A ring of steel fangs round the jaw's opening
        var across = Vector3.Normalize(Vector3.Cross(dir, V(1f, 0, 0)));
        for (int i = 0; i < 12; i++)
        {
            float a = MathF.Tau * i / 12f;
            var off = V(MathF.Cos(a) * 0.075f, 0, 0) + across * MathF.Sin(a) * 0.06f;
            var rim = mouth + dir * 0.07f + off;
            b.Spike(maw, rim, rim + dir * 0.045f - off * 0.35f, 0.011f, Rgb(214, 216, 224), mat: Metal, blend: 0.003f);
        }
        // Two horns curving back from the jaw's root
        PokeBuilder.Both(s => b.Tube(maw, Smooth(3, root + V(0.03f * s, 0.02f, 0.0f), root + V(0.07f * s, 0.1f, 0.0f), root + V(0.08f * s, 0.18f, -0.06f), root + V(0.05f * s, 0.2f, -0.12f)), 0.022f, 0.006f, black, Shell, 0.008f));
        // Small pointed ears
        PokeBuilder.Both(s => b.Spike(head, c + V(0.055f * s, 0.0f, 0.0f), c + V(0.1f * s, 0.02f, -0.01f), 0.014f, Rgb(246, 220, 140), 0.5f));
        // A longer dress, striped down its front in black
        Grow(b, V(1.1f, 1.25f, 1.1f), V(0, 0.25f, 0), s => s.Bone == Body && s.A.Y < 0.2f, _ => false);
        b.PaintEll(Body, V(0, 0.1f, 0.09f), V(0.03f, 0.12f, 0.05f), black);
        // Its feet showing in front of the longer hem
        PokeBuilder.Both(s => b.Ell(BoneOf(b, s < 0 ? "legL" : "legR"), V(0.05f * s, 0.022f, 0.1f), V(0.035f, 0.024f, 0.045f), black));
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.075f * s, 0.12f, 0.06f), V(0.012f, 0.12f, 0.05f), black, V(0, 0, 18f * s)));
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2308 Ascetrance, from Medicham

    /// <summary>
    /// Ascetrance: Medicham taller and leaner, grey, floating cross-legged in crimson trousers whose sash drifts below
    /// it, a red ball at each side of its head, a third, lit ball on its brow and a ring of light behind its head.
    /// </summary>
    /// <param name="female">A female keeps Medicham's: larger balls at her head.</param>
    private static PokeBuilder Ascetrance(bool female = false)
    {
        var b = Become(Medicham(female), "Ascetrance", 0.9f).Hover();
        var grey = Rgb(124, 136, 152);
        var crimson = Rgb(172, 34, 52);
        var gold = Rgb(222, 180, 90);
        var light = Rgb(250, 236, 170);
        Recolour(b, (Rgb(228, 228, 236), grey), (Rgb(236, 62, 120), crimson), (Rgb(250, 226, 150), gold), (Rgb(200, 60, 80), Rgb(100, 100, 116)));
        int head = BoneOf(b, "head");
        // The headdress's stalk and bulb go: a ball at each side of the head takes its place
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(crimson) && s.A.Y > 0.84f);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Color.Equals(crimson))) s.Color = grey;
        b.Sdf.Changed();
        float ball = female ? 0.06f : 0.05f;
        PokeBuilder.Both(s =>
        {
            var at = V((0.1f + ball * 0.6f) * s, 0.76f, 0.0f);
            b.Ell(head, at, V(ball * 0.8f, ball, ball), crimson, mat: Shell, blend: 0.012f);
            b.PaintTorus(head, at + V(0.02f * s, 0, 0), ball * 0.6f, 0.006f, gold, V(0, 0, 90f));
        });
        b.Ell(head, V(0, 0.8f, 0.1f), V(0.016f, 0.016f, 0.012f), light, mat: Glow, blend: 0.004f);
        // The ring of light behind its head, on a thin thread down its back
        var ring = V(0, 0.76f, -0.12f);
        b.Torus(head, ring, 0.16f, 0.01f, light, V(90f, 0, 0), mat: Glow, blend: 0.004f);
        b.Limb(head, V(0, 0.62f, -0.05f), ring - V(0, 0.16f, 0), 0.008f, 0.006f, light, Glow, 0.004f);
        // Crossed legs: the standing legs go, the shins fold across in front under the trousers
        foreach (var leg in new[] { "legL", "legR" })
        {
            int bone = BoneOf(b, leg);
            float s = leg.EndsWith('L') ? -1f : 1f;
            b.Sdf.RemoveAll(x => x.Bone == bone);
            var knee = V(0.17f * s, 0.3f, 0.08f);
            var foot = V(-0.06f * s, 0.25f + 0.02f * s, 0.15f);
            b.Limb(bone, knee, foot, 0.04f, 0.03f, crimson, blend: 0.02f);
            b.Ell(bone, foot + V(-0.03f * s, 0, 0.0f), V(0.05f, 0.025f, 0.03f), grey, blend: 0.012f);
        }
        // The sash drifting below, in long tails edged in gold
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 0.07f;
            var from = V(x, 0.24f, -0.02f + 0.03f * (i % 2));
            var to = from + V(x * 0.8f, -0.2f - 0.04f * (i % 2), -0.04f);
            Blade(b, Body, from, to, 0.045f, crimson, V(0, 0.2f, 1f), 0.25f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2310 Fulmenhound, from Manectric

    /// <summary>
    /// Fulmenhound: Manectric larger and a deeper blue, its mane grown into a great yellow cloud over its neck and
    /// shoulders that sparks, yellow bolts down its body and legs, and its tail split in three jagged points.
    /// </summary>
    private static PokeBuilder Fulmenhound()
    {
        var b = Become(Manectric(), "Fulmenhound", 0.94f);
        var blue = Rgb(46, 86, 164);
        var yellow = Rgb(244, 222, 120);
        var cloud = Rgb(236, 220, 150);
        Recolour(b, (Rgb(84, 156, 210), blue));
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        // The cloud of its mane, heaped over the neck and shoulders
        var rnd = new GenomeRandom(2310);
        var mc = V(0, 0.43f, 0.08f);
        b.Ell(Body, mc, V(0.13f, 0.11f, 0.12f), cloud, mat: Glow, blend: 0.03f);
        for (int i = 0; i < 16; i++)
        {
            float a = MathF.Tau * i / 16f;
            float up = 0.2f + 0.8f * rnd.Float();
            var dir = Vector3.Normalize(V(MathF.Cos(a), up, MathF.Sin(a) * 1.1f));
            var at = mc + dir * V(0.13f, 0.11f, 0.13f);
            b.Ell(Body, at, V(0.05f, 0.045f, 0.05f) * (0.8f + 0.5f * rnd.Float()), cloud, mat: Glow, blend: 0.02f);
        }
        // Bolts down its flanks and legs
        PokeBuilder.Both(s =>
        {
            foreach (var z in new[] { -0.02f, -0.1f })
            {
                var n = V(s, 0.1f, 0);
                b.Mark(Body, Out(V(0, 0.32f, -0.01f), V(0.085f, 0.085f, 0.17f), default, V(s, 0.1f, z * 3f)), n, 0.04f, 0.012f, yellow, MarkShape.Zigzag, 70f);
            }
        });
        // A tail of three jagged points
        var root = V(0, 0.34f, -0.17f);
        foreach (var (dy, dx) in new[] { (0.12f, 0f), (0.0f, 0.07f), (0.0f, -0.07f) })
        {
            var tip = root + V(dx, 0.08f + dy, -0.3f);
            Blade(b, tail, root, Vector3.Lerp(root, tip, 0.55f) + V(0, 0.02f, 0), 0.04f, blue, V(1f, 0, 0), 0.25f);
            Blade(b, tail, Vector3.Lerp(root, tip, 0.45f) + V(0, -0.015f, 0), tip, 0.035f, blue, V(1f, 0, 0), 0.25f);
        }
        Stretch(b, V(1.05f, 1.05f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2311 Anodion and 2312 Cathodion, from Plusle and Minun

    /// <summary>
    /// Anodion and Cathodion: Plusle and Minun grown into tall cream hares, their ears long and tipped in their
    /// colour, their cheeks' signs grown into glowing pompoms and the sign on the tail a pompom of its shape.
    /// </summary>
    private static PokeBuilder CheerHare(PokeBuilder b, string name, Color tint, bool plus)
    {
        Become(b, name, 0.75f);
        var cream = Rgb(250, 238, 184);
        var white = Rgb(252, 248, 230);
        var glow = PixelCanvas.Mix(tint, White, 0.25f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        // The sign on the tail and the cheeks' marks go
        b.Sdf.RemoveAll(s => s.Bone == tail && s.Shape == SdfShape.RoundBox);
        b.Model.Decals.RemoveAll(d => !d.IsEye && d.Bone == head);
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(tint))) s.Color = cream;
        b.Sdf.Changed();
        // Long ears, tipped in the colour
        var c = V(0, 0.29f, 0.02f);
        var r = V(0.09f, 0.08f, 0.08f);
        PokeBuilder.Both(s =>
        {
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            var from = V(0.05f * s, 0.35f, 0);
            Grow(b, V(1.1f, 1.7f, 1.1f), from, x => x == ear);
            var tip = from + (V(0.13f * s, 0.5f, -0.02f) - from) * V(1.1f, 1.7f, 1.1f);
            b.PaintEll(ear, Vector3.Lerp(from, tip, 1.05f), V(0.05f, 0.09f, 0.04f), tint, Euler(tip - from));
            // A glowing pompom on each cheek
            var cheek = On(c, r, 0.075f * s, 0.26f) + V(0.015f * s, 0, -0.01f);
            b.Ell(head, cheek, V(0.032f, 0.032f, 0.03f), glow, mat: Glow, blend: 0.01f);
            FurTufts(b, head, cheek, V(0.03f, 0.03f, 0.028f), 6, 0.018f, 0.009f, glow, -0.5f, -0.6f, 0.1f);
        });
        b.Mark(head, On(c, r, 0, 0.265f), V(0, -0.2f, 1f), 0.015f, 0.007f, Rgb(170, 80, 80), MarkShape.Smile);
        // A pale belly
        b.PaintEll(Body, V(0, 0.15f, 0.05f), V(0.045f, 0.065f, 0.03f), white);
        // The sign at the tail's end, a pompom of its shape
        var sign = V(0.06f, 0.18f, -0.17f);
        var arms = plus ? new[] { V(0.035f, 0, 0), V(-0.035f, 0, 0), V(0, 0.035f, 0), V(0, -0.035f, 0) } : new[] { V(0.035f, 0, 0), V(-0.035f, 0, 0), V(0, 0, 0) };
        foreach (var o in arms)
            b.Ell(tail, sign + o, V(0.026f, 0.026f, 0.022f), glow, mat: Glow, blend: 0.012f);
        FurTufts(b, tail, V(0.03f, 0.1f, -0.1f), V(0.03f, 0.03f, 0.03f), 5, 0.03f, 0.012f, cream, -0.3f, -0.5f, 0.2f);
        Stretch(b, V(0.95f, 1.25f, 0.95f));
        return b;
    }

    private static PokeBuilder Anodion() => CheerHare(Plusle(), "Anodion", Rgb(230, 70, 70), true);

    private static PokeBuilder Cathodion() => CheerHare(Minun(), "Cathodion", Rgb(80, 150, 230), false);

    // ------------------------------------------------------------------ 2313 Photurion, from Volbeat

    /// <summary>
    /// Photurion: Volbeat taller, dark navy plated in sea green with a pale ruff at its neck, its wings longer, its
    /// feelers lit at their tips and the light of its tail grown into a great glowing lantern ringed in dark bands.
    /// </summary>
    private static PokeBuilder Photurion()
    {
        var b = Become(Volbeat(), "Photurion", 0.82f);
        var navy = Rgb(40, 44, 76);
        var green = Rgb(76, 128, 112);
        var pale = Rgb(214, 232, 176);
        var light = Rgb(250, 230, 110);
        Recolour(b, (Rgb(56, 54, 66), navy), (Rgb(220, 60, 60), green), (Rgb(130, 136, 160), light), (Rgb(246, 210, 80), navy));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Color.Equals(light))) s.Material = Glow;
        b.Sdf.Changed();
        // The lantern grown and ringed
        Grow(b, V(1.3f, 1.3f, 1.35f), V(0, 0.16f, -0.02f), s => s.Bone == Body && s.A.Y < 0.2f && s.A.Z < -0.03f, _ => false);
        foreach (float z in new[] { -0.05f, -0.1f, -0.15f })
            b.Torus(Body, V(0, 0.16f, z), 0.1f - MathF.Abs(z + 0.08f) * 0.5f, 0.009f, navy, V(90f, 0, 0), mat: Shell, blend: 0.004f);
        // Sea-green plates on its chest and legs, and a pale ruff
        b.PaintEll(Body, V(0, 0.27f, 0.05f), V(0.04f, 0.06f, 0.03f), green);
        foreach (var leg in new[] { "legL", "legR" })
        {
            int bone = BoneOf(b, leg);
            float s = leg.EndsWith('L') ? -1f : 1f;
            b.PaintEll(bone, V(0.055f * s, 0.1f, 0.03f), V(0.02f, 0.04f, 0.02f), green);
        }
        FurTufts(b, Body, V(0, 0.34f, 0.02f), V(0.06f, 0.03f, 0.05f), 10, 0.03f, 0.012f, pale, -0.2f, -0.6f, 0.4f);
        // Longer wings
        foreach (var w in new[] { "wingL", "wingR" })
        {
            int bone = BoneOf(b, w);
            float s = w.EndsWith('L') ? -1f : 1f;
            Grow(b, V(1.5f, 1.3f, 1.4f), V(0.02f * s, 0.31f, -0.03f), x => x == bone);
        }
        // The feelers lit at their tips
        foreach (var e in new[] { "earL", "earR" })
        {
            int bone = BoneOf(b, e);
            var tip = b.Sdf.Shapes.Last(s => s.Bone == bone).B;
            b.Ell(bone, tip, V(0.02f, 0.016f, 0.016f), light, mat: Glow, blend: 0.006f);
        }
        Stretch(b, V(1f, 1.2f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2314 Lampyria, from Illumise

    /// <summary>
    /// Lampyria: Illumise in deep violet, its dress grown into long wing-cases that fall about it, its feelers curled
    /// into spirals lit at their curls, golden eyes, and a trail of light motes drifting from behind it.
    /// </summary>
    private static PokeBuilder Lampyria()
    {
        var b = Become(Illumise(), "Lampyria", 0.84f);
        var violet = Rgb(78, 56, 140);
        var face = Rgb(112, 84, 176);
        var sheen = Rgb(96, 84, 186);
        var light = Rgb(250, 236, 160);
        Recolour(b, (Rgb(56, 54, 66), violet), (Rgb(130, 196, 232), face), (Rgb(170, 140, 210), face), (Rgb(80, 150, 210), Rgb(230, 170, 50)));
        int head = BoneOf(b, "head");
        // The straight feelers go: spirals lit at their curls take their place
        var c = V(0, 0.4f, 0.02f);
        foreach (var e in new[] { "earL", "earR" })
        {
            int bone = BoneOf(b, e);
            float s = e.EndsWith('L') ? -1f : 1f;
            b.Sdf.RemoveAll(x => x.Bone == bone);
            var spiral = Spiral(c + V(0.1f * s, 0.2f, 0), V(s, 0, 0), V(0, 1f, 0), 0.04f, 0.012f, 0f, MathF.Tau * 1.3f, 14);
            b.Tube(bone, new[] { c + V(0.025f * s, 0.06f, 0), c + V(0.06f * s, 0.2f, 0.0f) }.Concat(spiral).ToArray(), 0.007f, 0.006f, violet, blend: 0f);
            b.Ell(bone, c + V(0.1f * s, 0.2f, 0), V(0.03f, 0.03f, 0.012f), light, mat: Glow, blend: 0.01f);
        }
        // Long wing-cases falling about her like a gown
        for (int i = 0; i < 6; i++)
        {
            float a = (-150f + i * 60f) * Degree;
            var root = V(MathF.Sin(a) * 0.05f, 0.27f, MathF.Cos(a) * 0.04f);
            var tip = root + V(MathF.Sin(a) * 0.1f, -0.24f, MathF.Cos(a) * 0.08f);
            Blade(b, Body, root, tip, 0.06f, i % 2 == 0 ? violet : sheen, V(MathF.Sin(a), 0.1f, MathF.Cos(a)), 0.25f, Shell);
        }
        // A trail of light from behind her, motes strung along it
        var trail = Smooth(3, V(0, 0.12f, -0.08f), V(0.05f, 0.08f, -0.2f), V(-0.02f, 0.1f, -0.32f), V(0.04f, 0.06f, -0.44f));
        b.Tube(Body, trail, 0.02f, 0.006f, light, Glow, 0.006f);
        for (int i = 2; i < trail.Length - 1; i += 2)
            b.Ell(Body, trail[i] + V(0, 0.01f, 0), V(0.012f, 0.012f, 0.012f), light, mat: Glow, blend: 0.006f);
        Stretch(b, V(1f, 1.15f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2317 Gulpgorge, from Swalot

    /// <summary>
    /// Gulpgorge: Swalot larger and heavier and a deeper purple, its black diamonds grown into bands round its body,
    /// its lips full and its whiskers long and drooping. A female keeps Swalot's shorter whiskers.
    /// </summary>
    private static PokeBuilder Gulpgorge(bool female = false)
    {
        var b = Become(Swalot(female), "Gulpgorge", 0.95f);
        var purple = Rgb(118, 78, 152);
        var band = Rgb(44, 30, 60);
        var lip = Rgb(170, 110, 196);
        Recolour(b, (Rgb(170, 130, 210), purple), (Rgb(246, 210, 100), purple));
        b.Model.Decals.RemoveAll(d => d.Shape == MarkShape.Diamond);
        // The yellow whiskers go
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Shape == SdfShape.RoundCone && s.Size.X < 0.012f && s.A.Y > 0.25f);
        var bc = V(0, 0.22f, 0);
        var br = V(0.18f, 0.22f, 0.15f);
        // Bands round its body where the diamonds were
        foreach (var (y, tilt) in new[] { (0.12f, -10f), (0.2f, -14f), (0.28f, -18f) })
        {
            float k = MathF.Sqrt(1f - MathF.Pow((y - bc.Y) / br.Y, 2f));
            b.PaintTorus(Body, V(0, y, -0.02f), br.X * k, 0.02f, band, V(tilt, 0, 0), 1f, br.Z / br.X);
        }
        // Full lips
        b.Ell(Body, V(0, 0.33f, 0.135f), V(0.08f, 0.028f, 0.035f), lip, blend: 0.015f);
        b.Ell(Body, V(0, 0.29f, 0.13f), V(0.07f, 0.03f, 0.035f), lip, blend: 0.015f);
        // Long drooping whiskers
        PokeBuilder.Both(s =>
        {
            var root = V(0.06f * s, 0.31f, 0.15f);
            var path = female
                ? Smooth(3, root, root + V(0.06f * s, 0.01f, 0.03f), root + V(0.1f * s, -0.06f, 0.04f))
                : Smooth(3, root, root + V(0.08f * s, 0.02f, 0.04f), root + V(0.14f * s, -0.08f, 0.06f), root + V(0.15f * s, -0.2f, 0.06f), root + V(0.18f * s, -0.25f, 0.04f));
            b.Tube(Body, path, 0.01f, 0.006f, purple, blend: 0.004f);
        });
        Stretch(b, V(1.12f, 1.05f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2319 Megalodax, from Sharpedo

    /// <summary>
    /// Megalodax: Sharpedo longer and heavier, navy above and a dark red below, its fins larger and ragged, a second
    /// row of teeth behind the first, the yellow star on its snout faded into a scar and gill slits on its sides.
    /// </summary>
    private static PokeBuilder Megalodax()
    {
        var b = Become(Sharpedo(), "Megalodax", 1f);
        var navy = Rgb(36, 50, 88);
        var red = Rgb(128, 36, 50);
        var lift = Shifted(b, "body", V(0, 0.32f, 0));
        Recolour(b, (Rgb(52, 92, 160), navy), (Rgb(236, 238, 246), red), (Rgb(246, 214, 100), Rgb(208, 190, 128)));
        var c = V(0, 0.32f, 0.02f) + lift;
        var mouth = c + V(0, 0.0f, 0.2f);
        // A second row of teeth behind the first
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 2; i++)
            {
                float x = 0.025f * (i + 1f) * s;
                b.Spike(Body, mouth + V(x, 0.085f, -0.04f), mouth + V(x, 0.045f, -0.02f), 0.011f, White, mat: Shell, blend: 0.003f);
                b.Spike(Body, mouth + V(x, -0.085f, -0.04f), mouth + V(x, -0.045f, -0.02f), 0.011f, White, mat: Shell, blend: 0.003f);
            }
        });
        // Larger fins: the dorsal, the sides and the tail
        foreach (var f in new[] { "finL", "finR" })
        {
            int bone = BoneOf(b, f);
            float s = f.EndsWith('L') ? -1f : 1f;
            Grow(b, V(1.35f, 1.35f, 1.35f), c + V(0.1f * s, -0.04f, 0.04f), x => x == bone);
        }
        int tail = BoneOf(b, "tail");
        Grow(b, V(1.3f, 1.3f, 1.3f), c + V(0, 0.05f, -0.2f), x => x == tail);
        Grow(b, V(1.25f, 1.25f, 1.25f), c + V(0, 0.12f, -0.02f), s => s.Bone == Body && s.A.Y > c.Y + 0.1f && s.B.Y > c.Y + 0.25f, _ => false);
        // Small fins along its back behind the great one
        for (int i = 0; i < 3; i++)
            Blade(b, Body, c + V(0, 0.09f - i * 0.02f, -0.1f - i * 0.05f), c + V(0, 0.15f - i * 0.02f, -0.15f - i * 0.05f), 0.02f, navy, V(1f, 0, 0), 0.3f);
        Stretch(b, V(1.05f, 1f, 1.22f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2321 Leviawhale, from Wailord

    /// <summary>
    /// Leviawhale: Wailord longer and a deep sea teal, barnacles crusted on its head and flippers, pale scars on its
    /// back, a ridge of fins along its spine, and a glowing mist rising from its blowhole.
    /// </summary>
    private static PokeBuilder Leviawhale()
    {
        var b = Become(Wailord(), "Leviawhale", 1f);
        var teal = Rgb(40, 92, 112);
        var pale = Rgb(196, 220, 214);
        var fin = Rgb(64, 146, 134);
        var barnacle = Rgb(186, 176, 150);
        var mist = Rgb(210, 240, 236);
        var lift = Shifted(b, "body", V(0, 0.22f, 0));
        Recolour(b, (Rgb(64, 118, 196), teal), (Rgb(214, 214, 224), pale), (Rgb(150, 150, 166), Rgb(150, 176, 172)));
        var c = V(0, 0.22f, 0.02f) + lift;
        var r = V(0.16f, 0.15f, 0.42f);
        // A ridge of fins along its spine
        for (int i = 0; i < 9; i++)
        {
            float z = 0.22f - i * 0.065f;
            var at = Out(c, r, default, V(0, 1f, z / 0.42f * 1.2f));
            Blade(b, Body, at - V(0, 0.01f, 0), at + V(0, 0.07f - MathF.Abs(i - 4) * 0.008f, -0.06f), 0.03f, fin, V(1f, 0, 0), 0.25f);
        }
        // Barnacles on its head
        var rnd = new GenomeRandom(2321);
        for (int i = 0; i < 12; i++)
        {
            var dir = V(rnd.Range(-0.7f, 0.7f), rnd.Range(-0.2f, 0.5f), 1f);
            var at = Out(c, r, default, dir);
            b.Ell(Body, at, V(0.014f, 0.012f, 0.014f) * rnd.Range(0.8f, 1.4f), barnacle, mat: Shell, blend: 0.004f);
        }
        // Pale scars on its back
        PokeBuilder.Both(s =>
        {
            foreach (var (z, roll) in new[] { (0.0f, 40f), (-0.06f, -30f), (-0.15f, 20f) })
            {
                var n = V(0.4f * s, 1f, z * 2f);
                b.Mark(Body, Out(c, r, default, n), n, 0.006f, 0.035f, pale, MarkShape.Bar, roll * s);
            }
        });
        // A glowing mist rising from its blowhole
        var hole = Out(c, r, default, V(0, 1f, 0.6f));
        b.Tube(Body, Smooth(3, hole, hole + V(0.01f, 0.08f, 0.0f), hole + V(-0.02f, 0.16f, -0.01f), hole + V(0.01f, 0.22f, 0.0f)), 0.02f, 0.034f, mist, Glow, 0.008f);
        b.Ell(Body, hole + V(0.01f, 0.24f, 0), V(0.04f, 0.035f, 0.04f), mist, mat: Glow, blend: 0.012f);
        Stretch(b, V(1f, 1f, 1.2f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2323 Volcamel, from Camerupt

    /// <summary>
    /// Volcamel: Camerupt in a shaggy maroon coat, a third and taller cone risen between its two, lava glowing in the
    /// craters of all three, plates of rock on its legs and flanks and a grey muzzle. A female keeps Camerupt's larger
    /// cones.
    /// </summary>
    private static PokeBuilder Volcamel(bool female = false)
    {
        var b = Become(Camerupt(female), "Volcamel", 0.96f);
        var maroon = Rgb(132, 44, 44);
        var rock = Rgb(78, 66, 62);
        var lava = Rgb(252, 128, 40);
        Recolour(b, (Rgb(214, 108, 88), maroon), (Rgb(150, 144, 140), rock), (Rgb(170, 160, 156), Rgb(120, 104, 98)), (Rgb(80, 74, 76), rock));
        // The rings on its sides and the dark craters go: lava glows in the craters
        b.Model.Decals.RemoveAll(d => !d.IsEye);
        float k = female ? 1.2f : 1f;
        foreach (float z in new[] { 0.06f, -0.1f })
            b.Ell(Body, V(0, 0.32f + 0.145f * k, z), V(0.035f, 0.01f, 0.035f) * k, lava, mat: Glow, blend: 0.004f);
        // The third cone, taller, between the two
        var mid = V(0, 0.31f, -0.02f);
        b.Spike(Body, mid, mid + V(0, 0.33f, 0), 0.075f, rock, blend: 0.015f);
        b.CutBox(Body, mid + V(0, 0.32f, 0), V(0.15f, 0.04f, 0.15f), Quaternion.Identity);
        b.Ell(Body, mid + V(0, 0.275f, 0), V(0.03f, 0.01f, 0.03f), lava, mat: Glow, blend: 0.004f);
        foreach (var (x, z) in new[] { (0.03f, 0.03f), (-0.035f, 0.01f), (0.0f, -0.035f) })
            b.PaintEll(Body, mid + V(x * 1.6f, 0.19f, z * 1.6f), V(0.012f, 0.05f, 0.012f), lava);
        // Plates of rock on its legs and flank
        foreach (var leg in new[] { "legL", "legR", "hindL", "hindR" })
        {
            int bone = BoneOf(b, leg);
            float s = leg.EndsWith('L') ? -1f : 1f;
            float z = leg.StartsWith("leg") ? 0.11f : -0.13f;
            b.Ell(bone, V(0.115f * s, 0.07f, z + 0.03f), V(0.042f, 0.035f, 0.03f), rock, mat: Shell, blend: 0.008f);
        }
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.24f, 0.05f), (0.2f, 0.0f), (0.27f, -0.02f) })
                b.Ell(Body, V(0.16f * s, y, z), V(0.02f, 0.035f, 0.03f), rock, mat: Shell, blend: 0.008f);
        });
        Stretch(b, V(1.05f, 1.1f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2324 Smeltortoise, from Torkoal

    /// <summary>
    /// Smeltortoise: Torkoal with its coal shell banded in riveted iron straps, the coal glowing orange between them,
    /// short iron chimneys smoking from its back and iron plates on its head and legs.
    /// </summary>
    private static PokeBuilder Smeltortoise()
    {
        var b = Become(Torkoal(), "Smeltortoise", 0.9f);
        var coal = Rgb(46, 42, 48);
        var iron = Rgb(86, 84, 90);
        var glow = Rgb(250, 116, 40);
        Recolour(b, (Rgb(66, 62, 70), coal), (Rgb(220, 70, 60), glow));
        var bc = V(0, 0.22f, -0.02f);
        var br = V(0.18f, 0.14f, 0.21f);
        // Glowing seams across the coal
        foreach (var n in new[] { V(0.5f, 0.7f, 0.3f), V(-0.6f, 0.6f, -0.2f), V(0.2f, 0.6f, -0.7f), V(-0.3f, 0.8f, 0.5f) })
            b.PaintEll(Body, Out(bc, br, default, n), V(0.03f, 0.03f, 0.03f), glow);
        // Iron straps over the shell, round and crossing
        b.Torus(Body, V(0, 0.2f, -0.02f), 0.185f, 0.012f, iron, default, 1f, 1.15f, Metal, 0.004f);
        foreach (var yaw in new[] { 0f, 50f, -50f })
            b.Torus(Body, bc, 0.2f, 0.012f, iron, V(0, yaw, 90f), 0.73f, 1f, Metal, 0.004f);
        b.Torus(Body, bc, 0.185f, 0.012f, iron, V(90f, 0, 0), 1f, 0.76f, Metal, 0.004f);
        // Short chimneys out of its back
        foreach (var n in new[] { V(0.4f, 1f, 0.2f), V(-0.45f, 1f, -0.1f), V(0.1f, 1f, -0.5f), V(0.6f, 0.8f, -0.4f) })
        {
            var at = Out(bc, br, default, n);
            var up = Vector3.Normalize(V(n.X * 0.4f, 1f, n.Z * 0.4f));
            b.Limb(Body, at - up * 0.02f, at + up * 0.07f, 0.026f, 0.024f, iron, Metal, 0.006f);
            b.Torus(Body, at + up * 0.07f, 0.024f, 0.008f, iron, Euler(up), mat: Metal, blend: 0.003f);
        }
        // Iron plates on its brow and on its legs
        int head = BoneOf(b, "head");
        var c = V(0, 0.33f, 0.28f);
        b.Ell(head, c + V(0, 0.035f, -0.01f), V(0.055f, 0.025f, 0.06f), iron, mat: Metal, blend: 0.008f);
        foreach (var leg in new[] { "legL", "legR", "hindL", "hindR" })
        {
            int bone = BoneOf(b, leg);
            float s = leg.EndsWith('L') ? -1f : 1f;
            float z = leg.StartsWith("leg") ? 0.1f : -0.12f;
            b.Ell(bone, V(0.13f * s, 0.07f, z + 0.03f), V(0.035f, 0.03f, 0.025f), iron, mat: Metal, blend: 0.006f);
        }
        Stretch(b, V(1.08f, 1.1f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2326 Boarcane, from Grumpig

    /// <summary>
    /// Boarcane: Grumpig grown into a tall dark boar, swirls of violet over its shoulders and chest, tusks curving up
    /// from its snout, a third pearl lit on its brow and the pearls in its hands grown great and glowing violet.
    /// </summary>
    private static PokeBuilder Boarcane()
    {
        var b = Become(Grumpig(), "Boarcane", 0.9f);
        var dark = Rgb(72, 60, 86);
        var black = Rgb(42, 36, 50);
        var violet = Rgb(170, 120, 236);
        var swirl = Rgb(140, 96, 180);
        Recolour(b, (Rgb(190, 170, 220), dark), (Rgb(60, 56, 64), black), (Rgb(240, 170, 186), Rgb(104, 80, 96)));
        int head = BoneOf(b, "head");
        var c = V(0, 0.5f, 0.02f);
        // Tusks curving up from its snout and a third pearl on its brow
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.022f * s, 0.475f, 0.09f), V(0.06f * s, 0.46f, 0.11f), V(0.08f * s, 0.5f, 0.11f), V(0.075f * s, 0.54f, 0.1f)), 0.009f, 0.003f, Rgb(232, 228, 236), Shell, 0.004f));
        b.Ell(head, V(0, 0.548f, 0.07f), V(0.018f, 0.018f, 0.014f), violet, mat: Glow, blend: 0.004f);
        // A shaggy mane down the back of its neck
        FurTufts(b, Body, V(0, 0.41f, -0.03f), V(0.06f, 0.035f, 0.04f), 9, 0.05f, 0.016f, black, -0.5f, 0.2f, 0.4f);
        // Swirls over its shoulders and chest
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.06f * s, 0.38f, 0.06f), V(0.02f, 0.03f, 0.02f), swirl, V(0, 0, 30f * s));
            b.PaintEll(Body, V(0.08f * s, 0.32f, 0.05f), V(0.015f, 0.025f, 0.02f), swirl, V(0, 0, -20f * s));
        });
        // The great glowing pearls in its hands
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = V(0.18f * s, 0.32f, 0.06f);
            b.Ell(arm, hand + V(0.005f * s, 0.035f, 0.025f), V(0.042f, 0.042f, 0.042f), violet, mat: Glow, blend: 0.006f);
        });
        Stretch(b, V(1.06f, 1.15f, 1.06f));
        return b;
    }

    // ------------------------------------------------------------------ 2327 Whirlpanda, from Spinda

    /// <summary>
    /// Whirlpanda: Spinda grown into a stout bear of a drunken master, its spots and spiral eyes kept, its round ears
    /// red, a red sash knotted at its waist and a gourd hanging at its hip.
    /// </summary>
    private static PokeBuilder Whirlpanda()
    {
        var b = Become(Spinda(), "Whirlpanda", 0.88f);
        var cream = Rgb(246, 234, 210);
        var spot = Rgb(222, 112, 72);
        var sash = Rgb(150, 42, 44);
        var gourd = Rgb(170, 126, 76);
        Recolour(b, (Rgb(214, 88, 88), spot), (Rgb(244, 232, 210), cream));
        // A stout body
        Grow(b, V(1.35f, 1.12f, 1.35f), V(0, 0.2f, 0), s => s.Bone == Body, bone => bone == Body);
        foreach (var e in new[] { "earL", "earR" })
        {
            int bone = BoneOf(b, e);
            foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == bone && s.Shape == SdfShape.Ellipsoid)) s.Color = spot;
        }
        b.Sdf.Changed();
        // More spots on its body and arms
        foreach (var (x, y, z) in new[] { (0.07f, 0.26f, 0.05f), (-0.09f, 0.2f, 0.02f), (0.05f, 0.14f, -0.07f), (-0.04f, 0.27f, -0.07f) })
            b.PaintEll(Body, V(x, y, z), V(0.025f, 0.025f, 0.025f), spot);
        // The sash, knotted in front with its two ends hanging
        var waist = V(0, 0.16f, 0);
        b.Torus(Body, waist, 0.1f, 0.018f, sash, default, 1f, 0.88f, SurfaceMaterial.Cloth, 0.008f);
        b.Ell(Body, waist + V(0, 0, 0.09f), V(0.025f, 0.022f, 0.016f), sash, mat: SurfaceMaterial.Cloth, blend: 0.006f);
        foreach (var x in new[] { -0.012f, 0.014f })
            Blade(b, Body, waist + V(x, -0.01f, 0.095f), waist + V(x * 2.2f, -0.1f, 0.1f), 0.018f, sash, V(0, 0, 1f), 0.25f, SurfaceMaterial.Cloth);
        // The gourd at its hip on a cord
        var hip = V(0.125f, 0.11f, 0.0f);
        b.Limb(Body, waist + V(0.095f, 0, 0), hip + V(0, 0.05f, 0), 0.006f, 0.005f, Rgb(200, 180, 130));
        b.Ell(Body, hip, V(0.034f, 0.036f, 0.034f), gourd, mat: Shell, blend: 0.006f);
        b.Ell(Body, hip + V(0, 0.048f, 0), V(0.022f, 0.022f, 0.022f), gourd, mat: Shell, blend: 0.01f);
        Stretch(b, V(1.12f, 1.25f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2332 Saguarox, from Cacturne

    /// <summary>
    /// Saguarox: Cacturne grown tall and dark green, ribbed and spined all over, its arms branching upward like a
    /// saguaro's with white flowers open on them, its hat broad and spined along its brim. A female keeps Cacturne's
    /// pale diamonds.
    /// </summary>
    private static PokeBuilder Saguarox(bool female = false)
    {
        var b = Become(Cacturne(female), "Saguarox", 0.98f);
        var green = Rgb(76, 122, 76);
        var dark = Rgb(44, 82, 52);
        var spine = Rgb(226, 206, 160);
        Recolour(b, (Rgb(124, 180, 110), green), (Rgb(66, 120, 70), dark));
        int head = BoneOf(b, "head");
        var c = V(0, 0.58f, 0.02f);
        // A broader brim to the hat, spined round its edge
        Grow(b, V(1.45f, 1f, 1.45f), c + V(0, 0.06f, 0), s => s.Bone == head && s.Shape == SdfShape.Ellipsoid && s.Size.X > 0.11f, _ => false);
        for (int i = 0; i < 12; i++)
        {
            float a = MathF.Tau * i / 12f;
            var rim = c + V(MathF.Sin(a) * 0.165f, 0.06f, MathF.Cos(a) * 0.14f);
            b.Spike(head, rim, rim + V(MathF.Sin(a) * 0.03f, 0.012f, MathF.Cos(a) * 0.03f), 0.006f, spine, mat: Shell, blend: 0.002f);
        }
        // Ribs down its body and spines over it
        var bc = V(0, 0.38f, 0);
        var br = V(0.085f, 0.15f, 0.07f);
        foreach (var x in new[] { -0.06f, 0.06f })
            b.PaintEll(Body, V(x, 0.36f, 0.03f), V(0.006f, 0.14f, 0.06f), dark, soft: 0.008f);
        foreach (var n in new[] { V(0.5f, 0.5f, 0.7f), V(-0.6f, 0.2f, 0.7f), V(0.7f, -0.3f, 0.6f), V(-0.4f, -0.5f, 0.75f), V(0.9f, 0.3f, -0.3f), V(-0.9f, -0.1f, -0.3f), V(0.2f, 0.6f, -0.8f), V(-0.3f, -0.4f, -0.85f) })
        {
            var at = Out(bc, br, default, n);
            b.Spike(Body, at, at + Vector3.Normalize(n) * 0.03f, 0.006f, spine, mat: Shell, blend: 0.002f);
        }
        // Branches rising from each arm, with a white flower open on each
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var root = Vector3.Lerp(V(0.07f * s, 0.46f, 0), V(0.28f * s, 0.46f, 0.04f), 0.6f);
            var top = root + V(0.04f * s, 0.2f, -0.01f);
            b.Tube(arm, Smooth(3, root, root + V(0.03f * s, 0.06f, 0), top), 0.034f, 0.03f, green, blend: 0.012f);
            b.Ell(arm, top, V(0.03f, 0.03f, 0.03f), green, blend: 0.01f);
            for (int i = 0; i < 2; i++)
            {
                var at = Vector3.Lerp(root, top, 0.4f + 0.4f * i) + V(0.03f * s, 0, 0);
                b.Spike(arm, at, at + V(0.03f * s, 0.01f, 0), 0.006f, spine, mat: Shell, blend: 0.002f);
            }
            var flower = top + V(0.03f * s, -0.04f, 0.0f);
            b.Ell(arm, flower, V(0.012f, 0.012f, 0.012f), Rgb(240, 200, 80), blend: 0.004f);
            for (int p = 0; p < 6; p++)
            {
                float a = MathF.Tau * p / 6f;
                var way = V(0.3f * s, MathF.Sin(a), MathF.Cos(a));
                b.Spike(arm, flower, flower + Vector3.Normalize(way) * 0.035f, 0.012f, Rgb(248, 246, 232), 0.4f, Leaf, 0.003f);
            }
        });
        Stretch(b, V(1.1f, 1.22f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2334 Nimbusong, from Altaria

    /// <summary>
    /// Nimbusong: Altaria grown larger and a deeper blue, its cloud wings grown into a mantle of cloud round its whole
    /// body, a crest of three feathers, and long plumes tipped in gold from its head and its tail.
    /// </summary>
    private static PokeBuilder Nimbusong()
    {
        var b = Become(Altaria(), "Nimbusong", 1f);
        var blue = Rgb(64, 132, 214);
        var deep = Rgb(40, 74, 160);
        var gold = Rgb(222, 184, 92);
        var lift = Shifted(b, "head", V(0, 0.66f, 0.08f));
        Recolour(b, (Rgb(84, 184, 238), blue));
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        // The mantle of cloud round its whole body, heaped at its neck
        foreach (var (at, r) in new[] { (V(0.08f, 0.44f, 0.04f), 0.09f), (V(-0.08f, 0.44f, 0.04f), 0.09f), (V(0, 0.36f, 0.14f), 0.1f), (V(0.12f, 0.3f, -0.04f), 0.1f),
                     (V(-0.12f, 0.3f, -0.04f), 0.1f), (V(0, 0.22f, 0.02f), 0.1f), (V(0, 0.3f, -0.18f), 0.1f), (V(0.04f, 0.24f, -0.3f), 0.08f), (V(-0.03f, 0.27f, -0.4f), 0.06f) })
            Puff(b, Body, at + lift, r);
        // Plumes from the tail, long and swept, tipped in gold
        var root = V(0, 0.25f, -0.16f) + lift;
        foreach (var (x, y) in new[] { (-0.06f, -0.12f), (0.06f, -0.14f), (0f, -0.06f) })
        {
            var path = Smooth(3, root, root + V(x, y * 0.5f, -0.14f), root + V(x * 2f, y, -0.3f), root + V(x * 2.8f, y * 0.6f, -0.45f));
            b.Tube(tail, path, 0.016f, 0.01f, deep, blend: 0.006f);
            Blade(b, tail, path[^2], path[^1] + (path[^1] - path[^2]) * 2f, 0.03f, deep, V(0, 1f, 0), 0.25f);
            b.PaintEll(tail, path[^1] + (path[^1] - path[^2]) * 1.8f, V(0.025f, 0.025f, 0.04f), gold, Euler(path[^1] - path[^2]));
        }
        // A crest of three feathers, and its head's plumes longer, tipped in gold
        int head = BoneOf(b, "head");
        var c = V(0, 0.71f, 0.1f) + lift;
        foreach (var (x, h) in new[] { (-0.025f, 0.08f), (0f, 0.1f), (0.025f, 0.08f) })
            Blade(b, head, c + V(x, 0.03f, -0.01f), c + V(x * 2.2f, 0.03f + h, -0.06f), 0.018f, deep, V(1f, 0, 0.2f), 0.3f);
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, c + V(0.18f * s, 0.05f, -0.29f), c + V(0.24f * s, 0.0f, -0.4f), c + V(0.28f * s, -0.08f, -0.5f));
            b.Tube(head, new[] { c + V(0.2f * s, 0.05f, -0.3f) }.Concat(path).ToArray(), 0.008f, 0.007f, blue, blend: 0.004f);
            Blade(b, head, path[^1], path[^1] + V(0.03f * s, -0.04f, -0.06f), 0.024f, gold, V(0, 1f, 0), 0.25f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2335 Ravagoose, from Zangoose

    /// <summary>
    /// Ravagoose: Zangoose grown larger and more muscled, its red scars spread into jagged stripes along its back,
    /// arms, legs and tail, its claws long and dark red, and its ears tipped with black tufts.
    /// </summary>
    private static PokeBuilder Ravagoose()
    {
        var b = Become(Zangoose(), "Ravagoose", 0.93f);
        var red = Rgb(186, 44, 58);
        var claw = Rgb(88, 30, 40);
        Recolour(b, (Rgb(214, 60, 70), red), (Rgb(56, 54, 62), claw));
        // Heavier shoulders
        b.Ell(Body, V(0, 0.4f, -0.02f), V(0.12f, 0.08f, 0.08f), Rgb(244, 244, 246), blend: 0.04f);
        // Jagged red stripes along its back and sides
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z, roll) in new[] { (0.42f, -0.05f, 30f), (0.36f, -0.07f, 20f), (0.3f, -0.07f, 15f), (0.24f, -0.05f, 10f), (0.38f, 0.02f, 40f) })
                b.PaintEll(Body, V(0.08f * s, y, z), V(0.05f, 0.008f, 0.04f), red, V(0, 0, roll * s), 0.006f);
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            foreach (var t in new[] { 0.35f, 0.6f })
                b.PaintTorus(arm, Vector3.Lerp(V(0.1f * s, 0.4f, 0.01f), V(0.17f * s, 0.32f, 0.12f), t), 0.03f, 0.006f, red, Euler(V(0.07f * s, -0.08f, 0.11f)));
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.PaintTorus(leg, V(0.075f * s, 0.15f, 0.0f), 0.04f, 0.006f, red, V(15f, 0, 0));
            // Longer claws
            var hand = V(0.17f * s, 0.32f, 0.12f);
            for (int i = -1; i <= 1; i++)
                b.Tube(arm, Smooth(3, hand + V(0.01f * i, 0, 0.01f), hand + V(0.03f * i + 0.025f * s, -0.07f, 0.08f), hand + V(0.035f * i + 0.03f * s, -0.14f, 0.07f)), 0.012f, 0.003f, claw, Shell, 0.004f);
            // Black tufts at the ears' tips
            int ear = BoneOf(b, s < 0 ? "earL" : "earR");
            b.Spike(ear, V(0.08f * s, 0.69f, -0.02f), V(0.09f * s, 0.75f, -0.03f), 0.01f, Rgb(50, 40, 44));
        });
        // Red chevrons down its tail
        int tail = BoneOf(b, "tail");
        var tp = Smooth(3, V(0, 0.24f, -0.08f), V(0.08f, 0.3f, -0.2f), V(0.18f, 0.4f, -0.22f));
        for (int i = 2; i < tp.Length - 1; i += 2)
            b.PaintTorus(tail, tp[i], 0.045f, 0.008f, red, Euler(tp[i + 1] - tp[i - 1]));
        Stretch(b, V(1.08f, 1.12f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2336 Scimitail, from Seviper

    /// <summary>
    /// Scimitail: Seviper longer and black, its gold scales grown into bands edged in red, its fangs longer, a crest
    /// of dark spines on its head, and the blade of its tail grown into a great curved scimitar of steel.
    /// </summary>
    private static PokeBuilder Scimitail()
    {
        var b = Become(Seviper(), "Scimitail", 0.9f);
        var black = Rgb(40, 40, 50);
        var gold = Rgb(214, 172, 80);
        var red = Rgb(186, 40, 56);
        var steel = Rgb(196, 200, 210);
        Recolour(b, (Rgb(56, 60, 80), black), (Rgb(240, 200, 80), gold), (Rgb(140, 100, 180), red));
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        // Gold bands edged in red round its coils
        var coil = Spiral3(0.22f, 0.12f, 0.06f, 0.15f, 2.4f, 1.3f, 20);
        var points = new[] { V(0.14f, 0.3f, -0.34f), V(0.18f, 0.16f, -0.28f), V(0.2f, 0.07f, -0.2f) }.Concat(coil)
            .Concat(new[] { V(0.0f, 0.24f, 0.05f), V(0, 0.35f, 0.07f), V(0, 0.44f, 0.06f) }).ToArray();
        for (int i = 2; i < points.Length - 2; i += 2)
        {
            float t = (i + 0.5f) / (points.Length - 1);
            float rad = 0.03f + 0.03f * MathF.Min(1f, t * 3f);
            var mid = Vector3.Lerp(points[i], points[i + 1], 0.5f);
            var turn = Euler(points[i + 1] - points[i]);
            int bone = 1 + i / 2;
            b.PaintTorus(bone, mid, rad, 0.006f, gold, turn);
            b.PaintTorus(bone, mid + Vector3.Normalize(points[i + 1] - points[i]) * 0.012f, rad, 0.003f, red, turn);
        }
        // The scimitar: a broad curved blade of steel rising from the tail's end, gilded at its hilt
        var hilt = points[0];
        b.Ell(tail, hilt, V(0.03f, 0.03f, 0.03f), gold, mat: Metal, blend: 0.008f);
        var arc = Smooth(3, hilt, hilt + V(0.0f, 0.12f, -0.04f), hilt + V(0.02f, 0.26f, 0.0f), hilt + V(0.03f, 0.34f, 0.1f));
        for (int i = 1; i < arc.Length; i++)
        {
            float k = i / (float)(arc.Length - 1);
            Blade(b, tail, arc[i - 1], arc[i] + (arc[i] - arc[i - 1]) * 0.15f, 0.05f * (1f - 0.75f * k) + 0.012f, steel, V(1f, 0, 0), 0.18f, Metal);
        }
        // Longer fangs and a crest of spines
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Spike(head, V(0.028f * s, 0.465f, 0.165f), V(0.03f * s, 0.39f, 0.18f), 0.012f, White, mat: Shell, blend: 0.003f));
        foreach (var (x, z) in new[] { (0f, 0.06f), (0.02f, 0.03f), (-0.02f, 0.03f), (0f, 0.0f) })
            b.Spike(head, V(x, 0.52f, 0.06f + z - 0.04f), V(x * 2f, 0.58f, z - 0.06f), 0.012f, Rgb(70, 60, 70), 0.5f);
        Stretch(b, V(1.08f, 1.05f, 1.08f));
        return b;
    }

    // ------------------------------------------------------------------ 2337 Selenolith, from Lunatone

    /// <summary>
    /// Selenolith: Lunatone's crescent closed into a full moon, the crescent kept as a raised pale rim round its left
    /// side, its grey face pocked with craters and two red eyes glaring from it.
    /// </summary>
    private static PokeBuilder Selenolith()
    {
        var b = Become(Lunatone(), "Selenolith", 0.88f);
        var grey = Rgb(150, 148, 140);
        var pale = Rgb(226, 222, 196);
        var crater = Rgb(116, 114, 108);
        var lift = Shifted(b, "head", V(0, 0.32f, 0));
        Recolour(b, (Rgb(232, 222, 180), pale), (Rgb(200, 186, 140), Rgb(180, 172, 140)));
        // Its single eye and its brow go: the face is on the full disc
        b.Model.Decals.RemoveAll(d => d.IsEye || d.Center.X > -0.12f);
        var dc = V(0.015f, 0.32f, 0) + lift;
        var dr = V(0.2f, 0.205f, 0.066f);
        b.Ell(Body, dc, dr, grey, mat: Shell, blend: 0.012f);
        // A heavy brow of rock over its eyes, carried on the head's own bone
        int brow = BoneOf(b, "head");
        b.Ell(brow, dc + V(0.035f, 0.075f, dr.Z * 0.75f), V(0.1f, 0.022f, 0.03f), grey, mat: Shell, blend: 0.01f);
        // Its far side rough
        foreach (var n in new[] { V(0.6f, 0.3f, -0.6f), V(-0.2f, -0.5f, -0.8f), V(0.3f, -0.6f, -0.7f), V(-0.5f, 0.4f, -0.7f) })
            b.Ell(Body, Out(dc, dr, default, n), V(0.03f, 0.03f, 0.02f), grey, mat: Shell, blend: 0.01f);
        foreach (var (x, y, r) in new[] { (0.13f, 0.12f, 0.022f), (0.12f, -0.08f, 0.025f), (-0.02f, -0.11f, 0.02f), (0.03f, 0.14f, 0.016f) })
        {
            var at = On(dc, dr, dc.X + x, dc.Y + y);
            b.Mark(Body, at, Outward(dc, dr, at), r, r, crater, MarkShape.Ring);
        }
        foreach (var x in new[] { -0.02f, 0.09f })
        {
            var at = On(dc, dr, dc.X + x, dc.Y + 0.03f);
            b.Mark(Body, at, Outward(dc, dr, at), 0.03f, 0.022f, Rgb(50, 46, 46));
            b.Eye(Body, at, Outward(dc, dr, at), 0.016f, sclera: true, white: Rgb(240, 110, 110), pupil: Rgb(190, 30, 40), glare: true);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2338 Heliolith, from Solrock

    /// <summary>
    /// Heliolith: Solrock with twice as many points, longer, a band of dark rock across its face where its eyes glow,
    /// and a corona of light burning round its rays.
    /// </summary>
    private static PokeBuilder Heliolith()
    {
        var b = Become(Solrock(), "Heliolith", 0.9f);
        var orange = Rgb(200, 112, 56);
        var ray = Rgb(214, 132, 68);
        var dark = Rgb(56, 40, 34);
        var corona = Rgb(252, 170, 70);
        var lift = Shifted(b, "rays", V(0, 0.32f, 0));
        Recolour(b, (Rgb(214, 116, 60), orange), (Rgb(246, 210, 100), ray));
        int rays = BoneOf(b, "rays");
        var c = V(0, 0.32f, 0) + lift;
        Grow(b, V(1.2f, 1.2f, 1.2f), c, x => x == rays);
        // Ten more points between the first, and a few turned toward and away
        for (int i = 0; i < 10; i++)
        {
            float a = MathF.Tau * (i + 0.5f) / 10f + 0.2f;
            var dir = Vector3.Normalize(V(MathF.Cos(a), MathF.Sin(a), i % 2 == 0 ? -0.15f : 0.15f));
            var root = c + dir * 0.1f;
            Blade(b, rays, root, root + dir * 0.17f, 0.04f, ray, V(0, 0, 1f), 0.3f, Shell);
        }
        // The corona: a ring of light through the rays
        b.Torus(rays, c, 0.22f, 0.022f, corona, V(90f, 0, 0), mat: Glow, blend: 0.01f);
        // A band of dark rock across its face, through its eyes
        b.PaintTorus(Body, c + V(0, 0.012f, 0), 0.12f, 0.028f, dark, default, 1f, 0.92f);
        b.PaintTorus(Body, c + V(0, -0.06f, 0), 0.1f, 0.012f, dark, V(15f, 0, 0), 1f, 0.92f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2340 Namazuro, from Whiscash

    /// <summary>
    /// Namazuro: Whiscash grown larger and darker, navy over a mud-brown belly and fins, a rocky plate heaped on its
    /// head, four long barbels drooping from its mouth and cracks of pale light along its flanks.
    /// </summary>
    private static PokeBuilder Namazuro()
    {
        var b = Become(Whiscash(), "Namazuro", 0.92f);
        var navy = Rgb(44, 52, 100);
        var mud = Rgb(120, 104, 80);
        var fin = Rgb(96, 84, 70);
        var rock = Rgb(92, 86, 74);
        var crack = Rgb(246, 240, 170);
        var lift = Shifted(b, "head", V(0, 0.34f, 0.14f));
        Recolour(b, (Rgb(52, 74, 168), navy), (Rgb(246, 226, 150), mud), (Rgb(96, 186, 236), fin));
        int head = BoneOf(b, "head");
        // Its yellow whiskers and the W on its brow go
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(Rgb(250, 222, 80)));
        b.Model.Decals.RemoveAll(d => d.Shape == MarkShape.Zigzag);
        // The rocky plate heaped on its head
        var top = V(0, 0.48f, 0.17f) + lift;
        b.Ell(head, top, V(0.15f, 0.04f, 0.11f), rock, mat: Shell, blend: 0.02f);
        foreach (var (x, z, h) in new[] { (-0.09f, 0.04f, 0.07f), (-0.03f, 0.07f, 0.1f), (0.04f, 0.05f, 0.09f), (0.1f, 0.01f, 0.06f), (-0.06f, -0.04f, 0.08f), (0.02f, -0.05f, 0.11f), (0.08f, -0.06f, 0.07f) })
            b.Spike(head, top + V(x, 0.0f, z), top + V(x * 1.2f, h, z - 0.03f), 0.04f, rock, 0.6f, Shell, 0.012f);
        // Four long barbels drooping from its mouth, pale at their ends
        PokeBuilder.Both(s =>
        {
            foreach (var (x, len) in new[] { (0.12f, 0.24f), (0.06f, 0.18f) })
            {
                var root = V(x * s, 0.29f, 0.3f) + lift;
                var path = Smooth(3, root, root + V(0.03f * s, -0.02f, 0.06f), root + V(0.05f * s, -len * 0.6f, 0.08f), root + V(0.06f * s, -len, 0.05f));
                b.Tube(head, path, 0.012f, 0.006f, navy, blend: 0.004f);
                b.PaintEll(head, path[^1], V(0.012f, 0.03f, 0.012f), Rgb(230, 222, 190));
            }
        });
        // Cracks of light along its flanks
        PokeBuilder.Both(s =>
        {
            foreach (var z in new[] { 0.0f, -0.12f })
            {
                var n = V(s, 0.15f, z * 2f);
                b.Mark(Body, Out(V(0, 0.34f, -0.04f) + lift, V(0.2f, 0.17f, 0.27f), default, n), n, 0.06f, 0.02f, crack, MarkShape.Zigzag, 10f * s);
            }
        });
        Stretch(b, V(1.05f, 1.02f, 1.12f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2342 Crayronin, from Crawdaunt

    /// <summary>
    /// Crayronin: Crawdaunt darkened to a ronin's crimson and navy, its pincers grown long and flat into blades, its
    /// star grown into a crest of spikes, and a straw hat on its back with strands of weed hanging from its brim.
    /// </summary>
    private static PokeBuilder Crayronin()
    {
        var b = Become(Crawdaunt(), "Crayronin", 0.92f);
        var red = Rgb(132, 40, 52);
        var navy = Rgb(42, 44, 72);
        var straw = Rgb(186, 164, 112);
        var weed = Rgb(96, 110, 70);
        var edge = Rgb(220, 224, 232);
        Recolour(b, (Rgb(206, 64, 60), red), (Rgb(236, 220, 200), Rgb(170, 110, 104)), (Rgb(110, 170, 220), navy), (Rgb(244, 210, 100), red));
        // Navy plates down its back
        foreach (var z in new[] { 0.02f, -0.06f, -0.14f })
            b.PaintEll(Body, V(0, 0.29f, z), V(0.08f, 0.025f, 0.03f), navy, V(-20f, 0, 0));
        // Blades out of its pincers
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var wrist = V(0.18f * s, 0.34f, 0.12f);
            var dir = Vector3.Normalize(V(0.4f * s, 0.4f, 0.6f));
            var down = Vector3.Normalize(V(0.3f * s, -0.6f, 0.7f));
            var root = wrist + dir * 0.12f;
            Blade(b, arm, root, root + down * 0.26f, 0.05f, navy, V(s, 0.2f, 0), 0.2f, Shell);
            Blade(b, arm, root + V(0, -0.012f, 0.004f), root + down * 0.24f + V(0, -0.012f, 0.004f), 0.022f, edge, V(s, 0.2f, 0), 0.25f, Metal);
        });
        // A crest of spikes where its star was
        int head = BoneOf(b, "head");
        var c = V(0, 0.36f, 0.1f);
        foreach (var (x, h, z) in new[] { (0f, 0.16f, -0.02f), (-0.03f, 0.12f, -0.03f), (0.03f, 0.12f, -0.03f), (-0.05f, 0.08f, -0.01f), (0.05f, 0.08f, -0.01f) })
            b.Spike(head, c + V(x * 0.6f, 0.06f, 0), c + V(x * 1.4f, 0.06f + h, z - 0.04f), 0.018f, Math.Abs(x) > 0.04f ? navy : red, 0.5f);
        // The straw hat over its back, strands of weed hanging from its brim
        var hat = V(0, 0.33f, -0.06f);
        b.Ell(Body, hat, V(0.23f, 0.02f, 0.21f), straw, mat: Leaf, blend: 0.01f);
        b.Spike(Body, hat, hat + V(0, 0.09f, 0), 0.1f, straw, mat: Leaf, blend: 0.01f);
        for (int i = 0; i < 9; i++)
        {
            float a = MathF.Tau * (i + 0.3f) / 9f;
            var rim = hat + V(MathF.Sin(a) * 0.2f, -0.01f, MathF.Cos(a) * 0.18f);
            b.Tube(Body, Smooth(2, rim, rim + V(0, -0.04f, 0), rim + V(0.005f, -0.07f, 0.005f)), 0.006f, 0.004f, weed, Leaf, 0.003f);
        }
        Stretch(b, V(1.08f, 1.08f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2344 Dogunarch, from Claydol

    /// <summary>
    /// Dogunarch: Claydol taller, its black clay carved with bands of ochre and red, a crown of clay on its head,
    /// and four hands in place of two: the first pair raised beside its head, the second held low.
    /// </summary>
    private static PokeBuilder Dogunarch()
    {
        var b = Become(Claydol(), "Dogunarch", 0.96f);
        var clay = Rgb(70, 66, 62);
        var ochre = Rgb(196, 146, 70);
        var red = Rgb(160, 70, 56);
        var lift = Shifted(b, "head", V(0, 0.38f, 0));
        Recolour(b, (Rgb(56, 52, 58), clay));
        var bc = V(0, 0.26f, 0) + lift;
        // Bands of ochre and red round its body
        b.PaintTorus(Body, bc + V(0, 0.07f, 0), 0.125f, 0.012f, ochre, default, 1f, 0.92f);
        b.PaintTorus(Body, bc + V(0, -0.06f, 0), 0.12f, 0.01f, red, default, 1f, 0.92f);
        b.PaintTorus(Body, bc + V(0, -0.1f, 0), 0.1f, 0.008f, ochre, default, 1f, 0.92f);
        // The first pair of arms raised beside its head
        foreach (var a in new[] { "armL", "armR" })
        {
            int bone = BoneOf(b, a);
            float s = a.EndsWith('L') ? -1f : 1f;
            b.Sdf.RemoveAll(x => x.Bone == bone);
            var shoulder = V(0.12f * s, 0.3f, 0) + lift;
            var hand = V(0.25f * s, 0.42f, 0.03f) + lift;
            b.Limb(bone, shoulder, hand, 0.035f, 0.03f, clay);
            b.Ell(bone, hand, V(0.04f, 0.045f, 0.035f), clay);
            foreach (var f in new[] { -1f, 0f, 1f })
                b.Limb(bone, hand + V(0.01f * f * s, 0.03f, 0.015f * f), hand + V(0.015f * f * s, 0.075f, 0.02f * f), 0.011f, 0.009f, clay, blend: 0.008f);
            b.PaintTorus(bone, Vector3.Lerp(shoulder, hand, 0.65f), 0.033f, 0.006f, ochre, Euler(hand - shoulder));
        }
        // The second pair held low
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.11f * s, 0.2f, 0.01f) + lift;
            int arm = b.Part(s < 0 ? "arm2L" : "arm2R", Body, shoulder, PokeRole.Arm, s, s);
            var hand = V(0.25f * s, 0.1f, 0.05f) + lift;
            b.Limb(arm, shoulder, hand, 0.035f, 0.03f, clay);
            b.Ell(arm, hand, V(0.04f, 0.045f, 0.035f), clay);
            foreach (var f in new[] { -1f, 0f, 1f })
                b.Limb(arm, hand + V(0.01f * f * s, -0.03f, 0.015f * f), hand + V(0.015f * f * s, -0.075f, 0.02f * f), 0.011f, 0.009f, clay, blend: 0.008f);
            b.PaintTorus(arm, Vector3.Lerp(shoulder, hand, 0.65f), 0.033f, 0.006f, red, Euler(hand - shoulder));
        });
        // A crown of clay on its head
        int head = BoneOf(b, "head");
        var crown = V(0, 0.52f, -0.02f) + lift;
        b.Limb(head, crown - V(0, 0.02f, 0), crown + V(0, 0.03f, 0), 0.055f, 0.06f, clay, blend: 0.01f);
        for (int i = 0; i < 7; i++)
        {
            float a = MathF.Tau * i / 7f;
            var root = crown + V(MathF.Sin(a) * 0.045f, 0.03f, MathF.Cos(a) * 0.045f);
            b.Tube(head, Smooth(3, root, root + V(MathF.Sin(a) * 0.02f, 0.05f, MathF.Cos(a) * 0.02f), root + V(MathF.Sin(a) * 0.035f, 0.08f, MathF.Cos(a) * 0.035f)), 0.016f, 0.01f, clay, blend: 0.006f);
            b.Ell(head, root + V(MathF.Sin(a) * 0.035f, 0.085f, MathF.Cos(a) * 0.035f), V(0.014f, 0.014f, 0.014f), clay, blend: 0.006f);
        }
        b.PaintTorus(head, crown + V(0, 0.01f, 0), 0.058f, 0.008f, ochre);
        Stretch(b, V(1f, 1.15f, 1f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2346 Crinolith, from Cradily

    /// <summary>
    /// Crinolith: Cradily grown tall, its stalk ringed and mossed, its head a stony bulb with glowing yellow eyes, a
    /// crown of eight long green fronds drooping from its top, and roots spread round its base.
    /// </summary>
    private static PokeBuilder Crinolith()
    {
        var b = Become(Cradily(), "Crinolith", 1f);
        var stalk = Rgb(118, 132, 98);
        var stone = Rgb(92, 98, 86);
        var frond = Rgb(70, 140, 70);
        var rib = Rgb(170, 210, 110);
        var root = Rgb(116, 104, 84);
        var glow = Rgb(240, 236, 90);
        Recolour(b, (Rgb(150, 200, 110), stalk), (Rgb(240, 214, 120), glow));
        int head = BoneOf(b, "head");
        int neck = BoneOf(b, "stalk");
        // The pink tentacles go: fronds take their place
        b.Sdf.RemoveAll(s => s.Bone == head && s.Color.Equals(Rgb(244, 170, 170)));
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && s.Color.Equals(stalk))) s.Color = stone;
        b.Sdf.Changed();
        // Rings up its stalk
        foreach (var y in new[] { 0.14f, 0.26f, 0.34f })
            b.PaintTorus(neck, V(0, y, y > 0.3f ? -0.02f : -0.04f), 0.043f, 0.006f, Rgb(80, 96, 66));
        var c = V(0, 0.46f, 0.14f);
        // Extra glowing spots round its eyes
        foreach (var (x, y) in new[] { (0.07f, 0.44f), (-0.07f, 0.44f), (0.02f, 0.52f), (-0.02f, 0.52f) })
        {
            var at = On(c, V(0.09f, 0.08f, 0.09f), x, y);
            b.Mark(head, at, Outward(c, V(0.09f, 0.08f, 0.09f), at), 0.012f, 0.012f, glow);
        }
        // The crown of eight fronds, arching up and drooping
        for (int i = 0; i < 8; i++)
        {
            float a = MathF.Tau * (i + 0.5f) / 8f;
            var way = V(MathF.Sin(a), 0, MathF.Cos(a));
            var r0 = c + V(0, 0.06f, -0.02f) + way * 0.03f;
            var mid = r0 + way * 0.12f + V(0, 0.12f, 0);
            var tip = mid + way * 0.16f + V(0, -0.2f, 0);
            b.Tube(head, Smooth(3, r0, r0 + V(0, 0.06f, 0) + way * 0.04f, mid), 0.016f, 0.012f, frond, Leaf, 0.006f);
            Frond(b, head, mid - way * 0.02f, tip, 0.05f, frond, V(0, 1f, 0) - way * 0.4f, 0.2f, Leaf);
            b.PaintEll(head, Vector3.Lerp(mid, tip, 0.45f), V(0.008f, 0.08f, 0.008f), rib, Euler(tip - mid));
        }
        // Roots spreading over the ground from its base
        for (int i = 0; i < 7; i++)
        {
            float a = MathF.Tau * i / 7f + 0.3f;
            var from = V(MathF.Sin(a) * 0.08f, 0.05f, -0.04f + MathF.Cos(a) * 0.08f);
            var to = V(MathF.Sin(a) * 0.24f, 0.012f, -0.04f + MathF.Cos(a) * 0.22f);
            b.Tube(Body, Smooth(3, from, Vector3.Lerp(from, to, 0.5f) + V(0, 0.02f, 0), to), 0.03f, 0.012f, root, blend: 0.01f);
        }
        Stretch(b, V(1f, 1.2f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2348 Anomalorex, from Armaldo

    /// <summary>
    /// Anomalorex: Armaldo in brown armour plated grey and edged in red, its arm blades grown into the curled,
    /// spined grasping limbs of the anomalocaris, and a fan of plates at the end of its tail.
    /// </summary>
    private static PokeBuilder Anomalorex()
    {
        var b = Become(Armaldo(), "Anomalorex", 0.98f);
        var brown = Rgb(132, 104, 78);
        var tan = Rgb(196, 156, 100);
        var plate = Rgb(122, 116, 108);
        var red = Rgb(178, 60, 46);
        var tooth = Rgb(236, 230, 214);
        Recolour(b, (Rgb(110, 140, 196), brown), (Rgb(236, 210, 130), tan), (Rgb(60, 62, 72), plate), (Rgb(214, 80, 90), red), (White, plate));
        // Grey plates over its back and shoulders, edged red
        foreach (var (y, z) in new[] { (0.6f, -0.05f), (0.52f, -0.08f), (0.44f, -0.08f), (0.36f, -0.06f) })
        {
            b.Ell(Body, V(0, y, z), V(0.11f, 0.035f, 0.07f), plate, V(30f, 0, 0), Shell, 0.01f);
            b.PaintTorus(Body, V(0, y - 0.01f, z), 0.1f, 0.006f, red, V(30f, 0, 0), 1f, 0.65f);
        }
        // The arm blades go: curled grasping limbs, spined on their inner edge
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Sdf.RemoveAll(x => x.Bone == arm && x.Color.Equals(Rgb(214, 220, 230)));
            var elbow = V(0.2f * s, 0.46f, 0.02f);
            var curl = Smooth(3, elbow, elbow + V(0.04f * s, 0.06f, 0.1f), elbow + V(0.05f * s, 0.0f, 0.2f), elbow + V(0.05f * s, -0.1f, 0.18f), elbow + V(0.04f * s, -0.12f, 0.1f), elbow + V(0.03f * s, -0.07f, 0.06f));
            b.Tube(arm, curl, 0.038f, 0.012f, brown, Shell, 0.004f);
            for (int i = 2; i < curl.Length - 2; i += 2)
            {
                b.PaintTorus(arm, curl[i], 0.036f - i * 0.0015f, 0.004f, red, Euler(curl[i + 1] - curl[i - 1]));
                var inward = Vector3.Normalize(elbow + V(0.045f * s, -0.04f, 0.12f) - curl[i]);
                b.Spike(arm, curl[i] + inward * 0.02f, curl[i] + inward * 0.05f, 0.007f, tooth, mat: Shell, blend: 0.002f);
            }
        });
        // A fan of plates at the tail's end
        int tail = BoneOf(b, "tail");
        var end = V(0, 0.12f, -0.42f);
        for (int i = 0; i < 5; i++)
        {
            float a = (-50f + 25f * i) * Degree;
            var tip = end + V(MathF.Sin(a) * 0.12f, 0.1f + MathF.Cos(a) * 0.04f, -0.1f);
            Blade(b, tail, end, tip, 0.04f, plate, V(0, 0.3f, 1f), 0.2f, Shell);
        }
        Stretch(b, V(1.05f, 1.1f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2350 Prismaria, from Milotic

    /// <summary>
    /// Prismaria: Milotic longer, its fanned tail doubled into a great fan of scales that shift from sky blue through
    /// lilac to pink, a fin of the same running along its back, and two long ribbons more from its head. A female keeps
    /// Milotic's longer brows.
    /// </summary>
    private static PokeBuilder Prismaria(bool female = false)
    {
        var b = Become(Milotic(female), "Prismaria", 1f);
        var sky = Rgb(150, 186, 244);
        var lilac = Rgb(200, 170, 236);
        var pink = Rgb(246, 176, 210);
        var gold = Rgb(244, 214, 150);
        Recolour(b, (Rgb(66, 156, 226), sky), (Rgb(238, 98, 128), pink), (Rgb(30, 60, 120), Rgb(120, 140, 200)));
        var points = new[] { V(0.24f, 0.08f, -0.28f), V(0.04f, 0.09f, -0.33f), V(-0.17f, 0.1f, -0.16f), V(-0.12f, 0.11f, 0.08f), V(0.09f, 0.13f, 0.12f), V(0.13f, 0.28f, 0.02f), V(0.05f, 0.47f, -0.02f), V(0, 0.62f, 0.02f), V(0, 0.72f, 0.06f) };
        // The second fan, behind the first and larger, in the shifting colours
        int fan = BoneOf(b, "tailFin");
        var cols = new[] { sky, lilac, pink, gold, lilac, sky, pink, lilac };
        int n = 0;
        foreach (float a in new[] { -62f, -37f, -12f, 13f, 38f, 63f, 88f, 113f })
        {
            var d = V(MathF.Sin(a * Degree), MathF.Cos(a * Degree), 0);
            var mid = points[0] + d * 0.15f + V(0, 0, -0.012f);
            b.Ell(fan, mid, V(0.042f, 0.15f, 0.01f), cols[n], Euler(d), blend: 0.008f);
            b.PaintEll(fan, mid + d * 0.07f, V(0.03f, 0.05f, 0.02f), cols[(n + 3) % cols.Length], Euler(d));
            n++;
        }
        // A fin along its back, from the coils up the neck
        for (int i = 3; i < 7; i++)
        {
            int seg = BoneOf(b, "seg" + i);
            for (int k = 0; k < 2; k++)
            {
                var at = Vector3.Lerp(points[i], points[i + 1], 0.25f + 0.5f * k);
                var along = Vector3.Normalize(points[i + 1] - points[i]);
                var up = Vector3.Normalize(V(0, 1f, 0) - along * along.Y);
                if (i >= 5) up = Vector3.Normalize(V(0, 0, -1f) - along * along.Z);
                float rad = i >= 5 ? 0.06f : 0.085f;
                Blade(b, seg, at + up * (rad - 0.01f), at + up * (rad + 0.05f) - along * 0.04f, 0.03f, (i + k) % 2 == 0 ? sky : lilac, Vector3.Cross(along, up), 0.25f);
            }
        }
        // Two long ribbons more from its head
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s =>
        {
            Petal(b, head, V(0.05f * s, 0.8f, 0.0f), V(0.22f * s, 0.62f, -0.2f), 0.03f, sky, Fur, 0.01f);
            Petal(b, head, V(0.04f * s, 0.78f, 0.03f), V(0.1f * s, 0.36f, 0.06f), 0.028f, lilac, Fur, 0.01f);
        });
        Stretch(b, V(1.08f, 1.08f, 1.08f));
        b.Ground(0f);
        return b;
    }

    // ------------------------------------------------------------------ 2352 Chromeleon, from Kecleon

    /// <summary>
    /// Chromeleon: Kecleon taller and paled to a ghostly mint, its red band grown into a frill round its neck striped
    /// in shifting violet and blue, its curled tail longer.
    /// </summary>
    private static PokeBuilder Chromeleon()
    {
        var b = Become(Kecleon(), "Chromeleon", 0.86f);
        var mint = Rgb(176, 220, 196);
        var pale = Rgb(206, 236, 218);
        var violet = Rgb(196, 92, 206);
        var blue = Rgb(110, 144, 232);
        Recolour(b, (Rgb(140, 200, 100), mint), (Rgb(200, 60, 90), mint), (Rgb(240, 214, 110), pale));
        b.Model.Decals.RemoveAll(d => d.Shape == MarkShape.Zigzag);
        // Faint wavy stripes over its body
        foreach (var y in new[] { 0.24f, 0.32f })
            b.PaintTorus(Body, V(0, y, 0), 0.084f, 0.006f, pale, default, 1f, 0.95f);
        // The frill round its neck, open at the front, striped violet and blue
        var neck = V(0, 0.42f, 0.0f);
        int i = 0;
        for (float a = -130f; a <= 130.1f; a += 26f, i++)
        {
            var d = Vector3.Normalize(V(MathF.Sin(a * Degree), 0.3f, -MathF.Cos(a * Degree)));
            Blade(b, Body, neck + d * 0.05f, neck + d * 0.17f + V(0, 0.03f, 0), 0.05f, i % 2 == 0 ? violet : blue, V(0, 1f, 0) - d * 0.3f, 0.15f);
        }
        // A longer tail
        int tail = BoneOf(b, "tail");
        Grow(b, V(1.3f, 1.3f, 1.3f), V(0, 0.22f, -0.07f), x => x == tail);
        Stretch(b, V(0.95f, 1.18f, 0.95f));
        return b;
    }

    // ------------------------------------------------------------------ 2354 Effigrim, from Banette

    /// <summary>
    /// Effigrim: Banette grown tall and gangling, patched in pieces of other cloth, a second zipper down its chest half
    /// open on a glowing light inside, and long ribbons of cloth hanging from its head.
    /// </summary>
    private static PokeBuilder Effigrim()
    {
        var b = Become(Banette(), "Effigrim", 0.9f);
        var grey = Rgb(76, 76, 88);
        var purple = Rgb(96, 82, 130);
        var maroon = Rgb(110, 56, 70);
        var navy = Rgb(52, 56, 88);
        var light = Rgb(230, 240, 110);
        var metal = Rgb(176, 178, 184);
        Recolour(b, (Rgb(90, 90, 100), grey));
        // Patches of other cloth
        foreach (var (bone, at, r, col) in new[] { (Body, V(0.04f, 0.28f, 0.05f), V(0.03f, 0.03f, 0.03f), purple), (Body, V(-0.05f, 0.2f, 0.05f), V(0.025f, 0.03f, 0.03f), maroon), (Body, V(0.0f, 0.22f, -0.06f), V(0.04f, 0.04f, 0.03f), purple) })
            b.PaintEll(bone, at, r, col);
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.PaintEll(leg, V(0.055f * s, 0.09f, 0.0f), V(0.035f, 0.05f, 0.04f), s < 0 ? navy : maroon);
        });
        int head = BoneOf(b, "head");
        var c = V(0, 0.4f, 0.02f);
        b.PaintEll(head, c + V(-0.05f, 0.04f, 0.0f), V(0.04f, 0.04f, 0.07f), purple);
        // The zipper down its chest, half open on the light inside
        var top = V(0, 0.31f, 0.065f);
        b.Ell(Body, V(0, 0.25f, 0.07f), V(0.014f, 0.045f, 0.008f), light, mat: Glow, blend: 0.004f);
        PokeBuilder.Both(s => b.Tube(Body, Smooth(3, top, V(0.018f * s, 0.25f, 0.075f), V(0.004f * s, 0.18f, 0.07f)), 0.005f, 0.005f, metal, Metal, 0.002f));
        b.Box(Body, V(0, 0.175f, 0.072f), V(0.008f, 0.012f, 0.004f), 0.002f, metal, mat: Metal, blend: 0.002f);
        // Long ribbons of cloth from the back of its head
        for (int i = 0; i < 6; i++)
        {
            float x = (i - 2.5f) * 0.03f;
            var from = c + V(x, 0.03f, -0.06f);
            var to = from + V(x * 2f, -0.24f - 0.04f * (i % 2), -0.06f);
            Blade(b, head, from, Vector3.Lerp(from, to, 0.5f) + V(0, 0, -0.02f), 0.02f, grey, V(0, 0.2f, -1f), 0.2f);
            Blade(b, head, Vector3.Lerp(from, to, 0.45f) + V(0, 0, -0.02f), to, 0.018f, i % 3 == 0 ? purple : grey, V(0, 0.2f, -1f), 0.2f);
        }
        Stretch(b, V(0.95f, 1.25f, 0.95f));
        return b;
    }

    // ------------------------------------------------------------------ 2357 Musadon, from Tropius

    /// <summary>
    /// Musadon: Tropius grown into a green dragon of the orchard, leaf-scaled, its leaf wings grown into a canopy, a
    /// crest of young leaves on its head, a bunch of fruit hanging at each side of its throat and a leafy tail.
    /// </summary>
    private static PokeBuilder Musadon()
    {
        var b = Become(Tropius(), "Musadon", 1f);
        var green = Rgb(92, 134, 80);
        var belly = Rgb(150, 180, 122);
        var leaf = Rgb(84, 160, 70);
        var young = Rgb(150, 206, 100);
        var fruit = Rgb(232, 170, 60);
        Recolour(b, (Rgb(206, 166, 112), green), (Rgb(232, 204, 158), belly), (Rgb(250, 222, 72), fruit));
        int head = BoneOf(b, "head");
        // Its mouth painted, not a mark: the wider canopy meshes the head coarser
        b.Model.Decals.RemoveAll(d => !d.IsEye && d.Bone == head);
        b.PaintEll(head, V(0, 0.808f, 0.462f), V(0.02f, 0.006f, 0.01f), Rgb(60, 70, 50));
        // The one bunch under its chin becomes two, one at each side of its throat
        var bunch = b.Sdf.Shapes.Where(s => s.Bone == head && s.Color.Equals(fruit) || (s.Bone == head && s.Color.Equals(Rgb(110, 80, 50)))).ToList();
        foreach (var s in bunch) { s.A += V(0.06f, 0, -0.02f); s.B += V(0.06f, 0, -0.02f); }
        foreach (var s in bunch)
        {
            var shift = V(-0.12f, 0, 0);
            if (s.Shape == SdfShape.Ellipsoid) b.Ell(head, s.A + shift, s.Size, s.Color, blend: s.Blend);
            else b.Sdf.Capsule(s.A + shift, s.B + shift, s.Size.X, s.Size.Y, s.Color, head, s.Blend, s.Material);
        }
        b.Sdf.Changed();
        // A crest of young leaves on its head
        foreach (var (x, h) in new[] { (-0.04f, 0.1f), (-0.015f, 0.13f), (0.015f, 0.13f), (0.04f, 0.1f) })
            Blade(b, head, V(x, 0.89f, 0.31f), V(x * 2.2f, 0.89f + h, 0.25f), 0.022f, young, V(1f, 0, 0.3f), 0.25f, Leaf);
        // Leaf scales down its neck and back
        foreach (var (y, z) in new[] { (0.72f, 0.19f), (0.62f, 0.18f), (0.54f, 0.12f), (0.5f, -0.04f), (0.5f, -0.16f) })
            PokeBuilder.Both(s => Blade(b, Body, V(0.04f * s, y, z), V(0.09f * s, y - 0.05f, z - 0.06f), 0.04f, leaf, V(s, 1f, 0), 0.25f, Leaf));
        // The canopy: its leaf wings broader
        foreach (var w in new[] { "wingL", "wingR" })
        {
            int bone = BoneOf(b, w);
            float s = w.EndsWith('L') ? -1f : 1f;
            Grow(b, V(1.2f, 1.25f, 1.35f), V(0.09f * s, 0.5f, -0.02f), x => x == bone);
        }
        // A leafy tail
        int tail = BoneOf(b, "tail");
        foreach (var a in new[] { -30f, 0f, 30f })
        {
            var d = V(MathF.Sin(a * Degree) * 0.6f, -0.2f, -1f);
            Frond(b, tail, V(0, 0.27f, -0.42f), V(0, 0.27f, -0.42f) + Vector3.Normalize(d) * 0.18f, 0.045f, leaf, V(0, 1f, 0), 0.25f, Leaf);
        }
        // Dark green legs
        foreach (var leg in new[] { "legL", "legR", "hindL", "hindR" })
        {
            int bone = BoneOf(b, leg);
            foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == bone && s.Color.Equals(green))) s.Color = Rgb(78, 100, 64);
        }
        b.Sdf.Changed();
        Stretch(b, V(1.05f, 1.05f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2358 Carillone, from Chimecho

    /// <summary>
    /// Carillone: Chimecho painted like porcelain in blue scrolls, a gold crown on its head, three bells hung one
    /// below another from its red cord and two long red streamers trailing below.
    /// </summary>
    private static PokeBuilder Carillone()
    {
        var b = Become(Chimecho(), "Carillone", 0.9f);
        var white = Rgb(236, 242, 250);
        var blue = Rgb(60, 96, 196);
        var red = Rgb(214, 54, 66);
        var gold = Rgb(230, 196, 70);
        var lift = Shifted(b, "body", V(0, 0.6f, 0));
        Recolour(b, (Rgb(196, 228, 246), white), (Rgb(250, 224, 110), gold));
        int tail = BoneOf(b, "tail");
        b.Sdf.RemoveAll(s => s.Bone == tail);
        b.Model.Decals.RemoveAll(d => !d.IsEye && d.Shape != MarkShape.Ring);
        // Blue scrolls round its head like porcelain's
        var c = V(0, 0.62f, 0) + lift;
        b.PaintTorus(Body, c - V(0, 0.07f, 0), 0.1f, 0.01f, blue, default, 1f, 0.92f);
        PokeBuilder.Both(s =>
        {
            b.Mark(Body, c + V(0.09f * s, 0.01f, 0.07f), V(0.8f * s, 0, 0.6f), 0.03f, 0.016f, blue, MarkShape.Wave);
            b.Mark(Body, c + V(0.06f * s, 0.04f, -0.09f), V(0.5f * s, 0.2f, -0.8f), 0.03f, 0.016f, blue, MarkShape.Wave);
        });
        // A gold crown over the cup it hangs from
        var crown = V(0, 0.8f, 0) + lift;
        for (int i = 0; i < 6; i++)
        {
            float a = MathF.Tau * i / 6f;
            var root = crown + V(MathF.Sin(a) * 0.04f, 0.0f, MathF.Cos(a) * 0.04f);
            b.Spike(Body, root,root + V(MathF.Sin(a) * 0.02f, 0.06f + 0.02f * (i % 2), MathF.Cos(a) * 0.02f), 0.016f, gold);
        }
        // The cord down through three bells, each smaller than the last
        var top = V(0, 0.54f, 0) + lift;
        float y = top.Y;
        foreach (var size in new[] { 0.06f, 0.05f, 0.042f })
        {
            var knot = V(0, y - 0.03f, 0.005f);
            b.Limb(tail, V(0, y + 0.01f, 0), knot, 0.012f, 0.012f, red, blend: 0.006f);
            b.Ell(tail, knot, V(0.016f, 0.014f, 0.016f), red, blend: 0.006f);
            var mouth = V(0, y - 0.03f - size * 2f, 0);
            b.Spike(tail, mouth, knot + V(0, -0.005f, 0), size, white, mat: Shell, blend: 0.01f);
            b.PaintTorus(tail, mouth + V(0, size * 0.55f, 0), size * 0.8f, 0.006f, blue);
            b.Ell(tail, mouth + V(0, -size * 0.8f, 0), V(0.012f, 0.012f, 0.012f), Rgb(170, 176, 196), mat: Metal, blend: 0.004f);
            y = mouth.Y - size * 0.8f - 0.01f;
        }
        // Two long red streamers trailing from its sides, gold at their ends
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var root = V(0.13f * s, 0.55f, 0.05f) + lift;
            var path = Smooth(3, root, root + V(0.06f * s, -0.12f, 0), root + V(0.0f, -0.28f, -0.03f), root + V(0.08f * s, -0.42f, 0.02f), root + V(0.03f * s, -0.52f, 0.0f));
            b.Tube(arm, path, 0.012f, 0.01f, red, SurfaceMaterial.Cloth, 0.004f);
            b.Ell(arm, path[^1], V(0.018f, 0.025f, 0.018f), gold, blend: 0.006f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2359 Omenblade, from Absol

    /// <summary>
    /// Omenblade: Absol grown larger and dark as a storm, its white mane grown into a great ruff and wings it can't
    /// fly with, a second crescent horn on its other side and its tail's blade sharper.
    /// </summary>
    private static PokeBuilder Omenblade()
    {
        var b = Become(Absol(), "Omenblade", 0.98f);
        var dark = Rgb(52, 56, 74);
        var mane = Rgb(220, 224, 232);
        var grey = Rgb(170, 174, 188);
        var horn = Rgb(70, 74, 92);
        Recolour(b, (Rgb(240, 242, 248), dark), (Rgb(70, 92, 150), Rgb(40, 42, 56)), (Rgb(56, 100, 170), horn));
        // The ruff over its neck and chest
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == Body && s.Shape == SdfShape.RoundCone && s.B.Y < 0.35f && s.A.Z > 0.12f)) s.Color = mane;
        b.Sdf.Changed();
        FurTufts(b, Body, V(0, 0.46f, 0.15f), V(0.09f, 0.06f, 0.06f), 14, 0.07f, 0.024f, mane, 0.2f, -0.6f, 0.4f);
        int head = BoneOf(b, "head");
        FurTufts(b, head, V(0, 0.57f, 0.21f), V(0.075f, 0.06f, 0.05f), 10, 0.06f, 0.02f, mane, -0.6f, -0.3f, 0.3f);
        // Wings of fur and feather from its shoulders
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 6; i++)
            {
                var root = V(0.08f * s, 0.5f, 0.06f - i * 0.04f);
                var tip = root + V((0.16f + 0.02f * i) * s, 0.12f + 0.02f * (3 - Math.Abs(i - 2)), -0.12f - 0.03f * i);
                Blade(b, Body, root, tip, 0.04f, i % 2 == 0 ? mane : grey, V(0, 1f, 0.2f), 0.2f);
            }
        });
        // A second crescent horn on the other side
        b.Tube(head, Smooth(3, V(-0.06f, 0.62f, 0.24f), V(-0.12f, 0.71f, 0.2f), V(-0.11f, 0.82f, 0.12f), V(-0.03f, 0.86f, 0.06f), V(0.04f, 0.82f, 0.05f)), 0.03f, 0.006f, horn, Shell, 0f);
        Stretch(b, V(1.06f, 1.08f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2362 Bergmaw, from Glalie

    /// <summary>
    /// Bergmaw: Glalie's face of ice grown into a jagged iceberg on four stout legs, crags of ice heaped round its face
    /// and over its back, four horns, its dark rock showing beneath and its cyan eyes glaring from the black.
    /// </summary>
    private static PokeBuilder Bergmaw()
    {
        var b = Become(Glalie(), "Bergmaw", 0.95f);
        var ice = Rgb(196, 220, 240);
        var deep = Rgb(150, 190, 226);
        var dark = Rgb(34, 44, 66);
        var lift = Shifted(b, "body", V(0, 0.42f, 0));
        Recolour(b, (Rgb(232, 238, 246), ice), (Rgb(40, 40, 50), dark));
        var c = V(0, 0.42f, 0) + lift;
        var r = V(0.24f, 0.22f, 0.22f);
        // Crags of ice round its face and over its back
        var rnd = new GenomeRandom(2362);
        for (int i = 0; i < 18; i++)
        {
            float a = (-100f + 200f * i / 17f) * Degree;
            var n = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a) * 0.9f + 0.1f, 0.35f));
            var at = Out(c, r, default, n);
            b.Spike(Body, at - n * 0.03f, at + n * rnd.Range(0.06f, 0.12f), 0.04f, i % 3 == 0 ? deep : ice, 0.6f, Shell, 0.012f);
        }
        foreach (var n in new[] { V(0.3f, 1f, -0.4f), V(-0.4f, 1f, -0.5f), V(0.1f, 0.8f, -0.9f), V(0.7f, 0.6f, -0.6f), V(-0.7f, 0.5f, -0.6f) })
        {
            var d = Vector3.Normalize(n);
            var at = Out(c, r, default, d);
            b.Spike(Body, at - d * 0.03f, at + d * 0.12f, 0.06f, ice, 0.6f, Shell, 0.015f);
        }
        // Two more horns, lower and forward
        PokeBuilder.Both(s => b.Spike(Body, c + V(0.18f * s, 0.06f, 0.05f), c + V(0.33f * s, 0.2f, 0.02f), 0.06f, ice, 0.6f, Shell));
        // Four stout legs of ice under it
        foreach (var (z, front) in new[] { (0.1f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.15f * s, c.Y - 0.12f, z), front);
                var foot = V(0.19f * s, 0.04f, z + 0.02f);
                b.Limb(leg, V(0.14f * s, c.Y - 0.1f, z), foot + V(0, 0.04f, 0), 0.07f, 0.075f, ice, Shell);
                b.Ell(leg, foot, V(0.08f, 0.045f, 0.09f), ice, mat: Shell);
                b.Spike(leg, foot + V(0.03f * s, 0.08f, 0), foot + V(0.07f * s, 0.18f, -0.01f), 0.03f, deep, 0.6f, Shell);
            });
        b.Model.Hovers = false;
        b.Ground(0f);
        return b;
    }

    // ------------------------------------------------------------------ 2367 Gulpereel, from Huntail

    /// <summary>
    /// Gulpereel: Huntail grown longer and dark as the deep, ragged fins along its back, glowing spots down its sides,
    /// a wide jaw of glowing fangs, a lure on a stalk over its head and the lure at its tail a little glowing fish.
    /// </summary>
    private static PokeBuilder Gulpereel()
    {
        var b = Become(Huntail(), "Gulpereel", 0.9f);
        var navy = Rgb(36, 38, 82);
        var teal = Rgb(44, 92, 96);
        var light = Rgb(150, 230, 240);
        var lift = Shifted(b, "head", Smooth(2, V(0.04f, 0.42f, -0.34f), V(0.06f, 0.3f, -0.24f), V(0.0f, 0.36f, -0.08f), V(-0.04f, 0.3f, 0.06f), V(0, 0.26f, 0.14f))[^1]);
        Recolour(b, (Rgb(90, 150, 210), navy), (Rgb(236, 150, 70), teal));
        foreach (var d in b.Model.Decals.Where(d => d.Shape == MarkShape.Ring)) d.Color = light;
        var points = Smooth(2, V(0.04f, 0.42f, -0.34f), V(0.06f, 0.3f, -0.24f), V(0.0f, 0.36f, -0.08f), V(-0.04f, 0.3f, 0.06f), V(0, 0.26f, 0.14f)).Select(p => p + lift).ToArray();
        // Glowing spots down its sides
        for (int i = 1; i < points.Length - 1; i++)
        {
            int bone = i < 2 ? Body : 1 + i / 2;
            float t = (i + 0.5f) / (points.Length - 1);
            float rad = 0.03f + 0.03f * t;
            var mid = Vector3.Lerp(points[i], points[i + 1], 0.5f);
            PokeBuilder.Both(s => b.Ell(bone, mid + V(rad * 0.85f * s, rad * 0.4f, 0), V(0.008f, 0.008f, 0.008f), light, mat: Glow, blend: 0.003f));
        }
        // The lure at its tail a little glowing fish
        int tail = BoneOf(b, "tail");
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == tail)) { s.Color = light; s.Material = Glow; }
        b.Sdf.Changed();
        // A wider jaw of glowing fangs, and a lure on a stalk over its head
        int head = BoneOf(b, "head");
        var c = points[^1] + V(0, 0.02f, 0.06f);
        var mouth = c + V(0, -0.025f, 0.06f);
        PokeBuilder.Both(s =>
        {
            foreach (var x in new[] { 0.012f, 0.038f })
            {
                b.Spike(head, mouth + V(x * s, 0.03f, -0.01f), mouth + V(x * s, 0.0f, 0.005f), 0.007f, light, mat: Glow, blend: 0.002f);
                b.Spike(head, mouth + V(x * s, -0.03f, -0.01f), mouth + V(x * s, 0.0f, 0.005f), 0.007f, light, mat: Glow, blend: 0.002f);
            }
        });
        var stalk = Smooth(3, c + V(0, 0.06f, -0.01f), c + V(0, 0.14f, 0.02f), c + V(0, 0.16f, 0.1f), c + V(0, 0.1f, 0.16f));
        b.Tube(head, stalk, 0.007f, 0.005f, navy, blend: 0.003f);
        b.Ell(head, stalk[^1], V(0.018f, 0.022f, 0.018f), light, mat: Glow, blend: 0.004f);
        Stretch(b, V(1f, 1.05f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2368 Sirenyss, from Gorebyss

    /// <summary>
    /// Sirenyss: Gorebyss longer, ribbon fins of lilac and sky blue flowing along its whole length, frilled fins at
    /// its head and its tail's fan doubled.
    /// </summary>
    private static PokeBuilder Sirenyss()
    {
        var b = Become(Gorebyss(), "Sirenyss", 0.9f);
        var lilac = Rgb(192, 170, 232);
        var sky = Rgb(170, 200, 240);
        var shell = Rgb(240, 222, 214);
        var raw = Smooth(2, V(0.06f, 0.22f, -0.32f), V(0.04f, 0.32f, -0.22f), V(0.0f, 0.4f, -0.08f), V(0, 0.42f, 0.04f));
        var lift = Shifted(b, "head", raw[^1]);
        var points = raw.Select(p => p + lift).ToArray();
        Recolour(b, (Rgb(176, 130, 200), lilac));
        // Ribbon fins along its length, both sides and over its back
        for (int i = 0; i < points.Length - 1; i++)
        {
            int bone = i < 2 ? Body : 1 + i / 2;
            var mid = Vector3.Lerp(points[i], points[i + 1], 0.5f);
            var along = Vector3.Normalize(points[i + 1] - points[i]);
            var side = Vector3.Normalize(Vector3.Cross(along, V(0, 1f, 0)));
            PokeBuilder.Both(s => Blade(b, bone, mid + side * s * 0.02f, mid + side * s * 0.08f + V(0, -0.05f, 0) - along * 0.03f, 0.022f, i % 2 == 0 ? lilac : sky, V(0, 1f, 0), 0.2f));
            Blade(b, bone, mid + V(0, 0.02f, 0), mid + V(0, 0.07f, 0) - along * 0.04f, 0.022f, sky, side, 0.2f);
        }
        // Frills at its head
        int head = BoneOf(b, "head");
        var c = points[^1] + V(0, 0.01f, 0.04f);
        foreach (var (x, h) in new[] { (-0.02f, 0.07f), (0f, 0.09f), (0.02f, 0.07f) })
            Blade(b, head, c + V(x, 0.03f, -0.01f), c + V(x * 2f, 0.03f + h, -0.04f), 0.016f, Rgb(232, 150, 180), V(1f, 0, 0.2f), 0.25f);
        // A second, larger fan at its tail
        int tail = BoneOf(b, "tail");
        b.Ell(tail, points[0] + V(0f, -0.06f, -0.03f), V(0.07f, 0.08f, 0.012f), shell, V(0, 60f, 20f), blend: 0.012f);
        b.PaintEll(tail, points[0] + V(0f, -0.09f, -0.04f), V(0.06f, 0.05f, 0.03f), lilac, V(0, 60f, 20f));
        Stretch(b, V(1f, 1.05f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2369 Aeonfin, from Relicanth

    /// <summary>
    /// Aeonfin: Relicanth grown larger and walking on its four lobed fins like limbs, its rocky scales grown into grey
    /// plates over a slate body, a spiral mark of age on its flank and a red mouth. A female keeps Relicanth's smaller
    /// patches.
    /// </summary>
    private static PokeBuilder Aeonfin(bool female = false)
    {
        var b = Become(Relicanth(female), "Aeonfin", 0.95f);
        var stone = Rgb(130, 122, 108);
        var slate = Rgb(60, 72, 86);
        var plate = Rgb(150, 146, 132);
        var lift = Shifted(b, "body", V(0, 0.28f, 0));
        Recolour(b, (Rgb(170, 150, 120), stone), (Rgb(100, 86, 74), slate));
        var c = V(0, 0.28f, 0) + lift;
        var r = V(0.1f, 0.13f, 0.22f);
        // Plates of rock over its head and back
        foreach (var n in new[] { V(0, 0.8f, 0.6f), V(0.5f, 0.6f, 0.7f), V(-0.5f, 0.6f, 0.7f), V(0, 1f, 0.1f), V(0.4f, 0.8f, -0.2f), V(-0.4f, 0.8f, -0.2f), V(0, 0.9f, -0.4f) })
        {
            var d = Vector3.Normalize(n);
            b.Ell(Body, Out(c, r, default, d), V(0.04f, 0.022f, 0.045f), plate, Euler(d), Shell, 0.008f);
        }
        // A spiral mark of age on each flank, and a red mouth
        PokeBuilder.Both(s => b.Mark(Body, Out(c, r, default, V(s, 0.05f, -0.15f)), V(s, 0.05f, -0.15f), female ? 0.035f : 0.045f, female ? 0.035f : 0.045f, Rgb(214, 196, 160), MarkShape.Ring));
        b.PaintEll(Body, Out(c, r, default, V(0, -0.3f, 1f)), V(0.04f, 0.012f, 0.03f), Rgb(196, 64, 54));
        // Four lobed fins grown into limbs, the front pair the old fins carried on down
        PokeBuilder.Both(s =>
        {
            int front = BoneOf(b, s < 0 ? "finL" : "finR");
            var knee = c + V(0.13f * s, -0.14f, 0.04f);
            var foot = c + V(0.15f * s, -0.27f, 0.08f);
            b.Limb(front, knee, foot, 0.022f, 0.02f, stone);
            Frond(b, front, foot, foot + V(0.02f * s, -0.01f, 0.07f), 0.035f, slate, V(0, 1f, 0), 0.3f);
            var hip = c + V(0.07f * s, -0.08f, -0.12f);
            int hind = b.Part(s < 0 ? "hindFinL" : "hindFinR", Body, hip, PokeRole.Leg, s, s);
            var hk = hip + V(0.07f * s, -0.07f, -0.02f);
            var hf = hip + V(0.09f * s, -0.19f, 0.01f);
            b.Limb(hind, hip, hk, 0.025f, 0.022f, stone);
            b.Limb(hind, hk, hf, 0.022f, 0.02f, stone);
            Frond(b, hind, hf, hf + V(0.02f * s, -0.01f, 0.07f), 0.035f, slate, V(0, 1f, 0), 0.3f);
        });
        Stretch(b, V(1.05f, 1.05f, 1.1f));
        b.Model.Hovers = false;
        b.Ground(0f);
        return b;
    }

    // ------------------------------------------------------------------ 2370 Heartreef, from Luvdisc

    /// <summary>
    /// Heartreef: Luvdisc grown into a round pink fish with full lips, a reef of little coral branches tipped with
    /// hearts grown over its back, and a long flowing tail fin.
    /// </summary>
    private static PokeBuilder Heartreef()
    {
        var b = Become(Luvdisc(), "Heartreef", 0.75f);
        var pink = Rgb(244, 160, 186);
        var coral = Rgb(206, 80, 146);
        var pale = Rgb(250, 206, 222);
        var lip = Rgb(236, 92, 140);
        var lift = Shifted(b, "body", V(0, 0.25f, 0));
        var c = V(0, 0.25f, 0) + lift;
        int tail = BoneOf(b, "tail");
        // Full lips
        b.Ell(Body, c + V(0, 0.005f, 0.05f), V(0.03f, 0.014f, 0.016f), lip, blend: 0.006f);
        b.Ell(Body, c + V(0, -0.02f, 0.048f), V(0.026f, 0.013f, 0.015f), lip, blend: 0.006f);
        // A long flowing tail fin, its two lobes curling
        PokeBuilder.Both(s =>
        {
            var root = c + V(0, 0.03f, -0.06f);
            var path = Smooth(3, root, root + V(0.0f, 0.03f * s, -0.07f), root + V(0.0f, 0.08f * s, -0.14f), root + V(0.0f, 0.06f * s, -0.19f));
            for (int i = 1; i < path.Length; i++)
                Blade(b, tail, path[i - 1], path[i] + (path[i] - path[i - 1]) * 0.2f, 0.022f - 0.0012f * i, pale, V(1f, 0, 0), 0.2f);
        });
        // The reef of coral over its back, each branch ending in a little heart
        var rnd = new GenomeRandom(2370);
        for (int i = 0; i < 9; i++)
        {
            float x = rnd.Range(-0.08f, 0.08f);
            float z = rnd.Range(-0.03f, 0.03f);
            var root = c + V(x, 0.07f - MathF.Abs(x) * 0.3f, z);
            var tip = root + V(x * 0.4f, 0.05f + 0.03f * rnd.Float(), rnd.Range(-0.02f, 0.02f));
            b.Limb(Body, root - V(0, 0.02f, 0), tip, 0.009f, 0.006f, coral, blend: 0.004f);
            PokeBuilder.Both(s => b.Ell(Body, tip + V(0.006f * s, 0.008f, 0), V(0.008f, 0.008f, 0.006f), coral, V(0, 0, 30f * s), blend: 0.003f));
            b.Spike(Body, tip + V(0, 0.006f, 0), tip - V(0, 0.008f, 0), 0.009f, coral, 0.6f, blend: 0.003f);
        }
        Recolour(b, (Rgb(244, 150, 170), pink));
        Stretch(b, V(1.1f, 1.1f, 1.1f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2377 to 2379, the Regis grown

    /// <summary>Seven dots in a ring on a face, where the Regis' patterns were.</summary>
    private static void BrailleRing(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float radius, float dot, Color color, bool glow = false)
    {
        b.Model.Decals.RemoveAll(d => d.Bone == bone && !d.IsEye && d.Shape == MarkShape.Disc && Vector3.Distance(d.Center, at) < radius * 3f);
        for (int i = 0; i < 7; i++)
        {
            float a = MathF.Tau * i / 7f;
            var p = at + V(MathF.Sin(a) * radius, MathF.Cos(a) * radius, 0);
            if (glow) b.Ell(bone, p - facing * 0.004f, V(dot, dot, dot * 0.6f), color, mat: Glow, blend: 0.003f);
            else b.Mark(bone, p, facing, dot, dot, color);
        }
    }

    /// <summary>
    /// Regimonolith: Regirock taller and darker, built of heavier boulders, more of them heaped on its shoulders and
    /// arms, a lintel of stone laid over its head like a crown, and its seven dots grown into a ring.
    /// </summary>
    private static PokeBuilder Regimonolith()
    {
        var b = Become(Regirock(), "Regimonolith", 1f);
        var pale = Rgb(150, 132, 108);
        var brown = Rgb(110, 92, 74);
        var dark = Rgb(84, 74, 64);
        Recolour(b, (Rgb(210, 196, 182), pale), (Rgb(196, 120, 80), brown));
        var bc = V(0, 0.48f, 0);
        BrailleRing(b, Body, bc + V(0, 0.08f, 0.091f), V(0, 0, 1f), 0.04f, 0.012f, Rgb(232, 150, 60), glow: true);
        // Boulders heaped on its shoulders and arms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Box(arm, V(0.19f * s, 0.65f, -0.01f), V(0.07f, 0.06f, 0.07f), 0.03f, dark, V(10f, 25f, -10f * s), blend: 0.01f);
            b.Box(arm, V(0.25f * s, 0.42f, 0.03f), V(0.045f, 0.045f, 0.05f), 0.02f, brown, V(20f, 0, 15f * s), blend: 0.01f);
            b.Box(arm, V(0.24f * s, 0.2f, 0.05f), V(0.07f, 0.05f, 0.065f), 0.025f, pale, V(0, -20f, 0), blend: 0.01f);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Box(leg, V(0.11f * s, 0.18f, 0.04f), V(0.04f, 0.035f, 0.035f), 0.015f, dark, V(0, 30f, 0), blend: 0.008f);
        });
        // The lintel over its head, on two posts
        int head = BoneOf(b, "head");
        var top = bc + V(0, 0.3f, -0.01f);
        PokeBuilder.Both(s => b.Box(head, top + V(0.07f * s, -0.04f, 0), V(0.025f, 0.05f, 0.03f), 0.012f, pale, blend: 0.008f));
        b.Box(head, top + V(0, 0.02f, 0), V(0.19f, 0.03f, 0.05f), 0.015f, pale, V(0, 0, 3f), blend: 0.006f);
        Stretch(b, V(1.06f, 1.12f, 1.06f));
        return b;
    }

    /// <summary>
    /// Regiglacier: Regice grown larger, its body faceted into a crystal of many faces, great spires of ice risen at
    /// its shoulders and its dots grown into a ring.
    /// </summary>
    private static PokeBuilder Regiglacier()
    {
        var b = Become(Regice(), "Regiglacier", 1f);
        var ice = Rgb(170, 216, 236);
        var deep = Rgb(120, 180, 220);
        var bc = V(0, 0.5f, 0);
        BrailleRing(b, Body, bc + V(0, 0.07f, 0.091f), V(0, 0, 1f), 0.045f, 0.012f, Rgb(250, 240, 170), glow: true);
        // Facets over its body
        foreach (var (x, y, rz) in new[] { (0.07f, 0.1f, 20f), (-0.08f, -0.05f, -25f), (0.06f, -0.1f, 40f), (-0.05f, 0.12f, -35f) })
            b.Box(Body, bc + V(x, y, 0.06f), V(0.04f, 0.04f, 0.035f), 0.006f, ice, V(15f, 20f, rz), Glow, 0.006f);
        // Spires of ice at its shoulders
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            foreach (var (dx, h, lean, w) in new[] { (0.0f, 0.26f, 0.06f, 0.04f), (0.04f, 0.2f, 0.12f, 0.032f), (-0.03f, 0.18f, -0.02f, 0.03f), (0.06f, 0.13f, 0.16f, 0.026f) })
            {
                var root = V((0.19f + dx) * s, 0.6f, -0.01f);
                b.Spike(arm, root, root + V(lean * s, h, -0.03f), w, dx == 0 ? ice : deep, 0.7f, Glow, 0.005f);
            }
        });
        Stretch(b, V(1.08f, 1.1f, 1.08f));
        return b;
    }

    /// <summary>
    /// Regiferrum: Registeel heavier and dark iron, a vault's round door with a wheel and hinges on its chest, rivets
    /// along its seams, a lit ring of red bolts on its face and plated forearms.
    /// </summary>
    private static PokeBuilder Regiferrum()
    {
        var b = Become(Registeel(), "Regiferrum", 1f);
        var iron = Rgb(96, 100, 112);
        var dark = Rgb(56, 58, 66);
        var red = Rgb(240, 70, 70);
        Recolour(b, (Rgb(170, 172, 182), iron), (Rgb(56, 56, 64), dark));
        var bc = V(0, 0.44f, 0);
        var br = V(0.14f, 0.18f, 0.11f);
        // The dark plate down its face goes; a lit ring of bolts on its face
        b.Sdf.RemoveAll(s => s.Bone == Body && s.Color.Equals(dark) && s.Op == SdfOp.Paint);
        BrailleRing(b, Body, bc + V(0, 0.12f, 0.098f), V(0, 0.3f, 1f), 0.028f, 0.008f, red, glow: true);
        // The vault door on its chest
        var door = Out(bc, br, default, V(0, -0.15f, 1f));
        b.Torus(Body, door - V(0, 0, 0.006f), 0.065f, 0.012f, dark, V(90f, 0, 0), mat: Metal, blend: 0.004f);
        b.Ell(Body, door - V(0, 0, 0.004f), V(0.055f, 0.055f, 0.012f), iron, mat: Metal, blend: 0.004f);
        b.Torus(Body, door + V(0, 0, 0.012f), 0.022f, 0.005f, dark, V(90f, 0, 0), mat: Metal, blend: 0.002f);
        for (int i = 0; i < 3; i++)
        {
            float a = MathF.Tau * i / 3f;
            b.Limb(Body, door + V(0, 0, 0.012f), door + V(MathF.Sin(a) * 0.022f, MathF.Cos(a) * 0.022f, 0.012f), 0.004f, 0.004f, dark, Metal, 0.002f);
        }
        foreach (var y in new[] { 0.03f, -0.03f })
            b.Box(Body, door + V(0.068f, y, -0.005f), V(0.014f, 0.01f, 0.012f), 0.003f, dark, mat: Metal, blend: 0.003f);
        // Rivets along its seams
        for (int i = 0; i < 12; i++)
        {
            float a = MathF.Tau * i / 12f;
            var n = V(MathF.Sin(a), 0.35f, MathF.Cos(a));
            b.Ell(Body, Out(bc, br, default, n), V(0.007f, 0.007f, 0.007f), dark, mat: Metal, blend: 0.002f);
        }
        // Plated forearms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = V(0.26f * s, 0.28f, 0.04f);
            b.Limb(arm, Vector3.Lerp(V(0.18f * s, 0.5f, 0), hand, 0.3f), Vector3.Lerp(V(0.18f * s, 0.5f, 0), hand, 0.85f), 0.045f, 0.05f, iron, Metal, 0.01f);
        });
        Stretch(b, V(1.1f, 1.08f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2380 Aurelias and 2381 Aurelios, from Latias and Latios

    /// <summary>
    /// Aurelias and Aurelios: Latias and Latios longer, their wings swept back further and edged in gold, the triangle
    /// on the chest grown into a lit star, horns swept back from the head, and a long tail fin edged in gold.
    /// </summary>
    private static PokeBuilder AureliBuild(PokeBuilder b, string name, bool latios)
    {
        Become(b, name, 1f);
        var gold = Rgb(232, 196, 92);
        var main = latios ? Rgb(52, 84, 176) : Rgb(222, 96, 84);
        var star = latios ? Rgb(200, 226, 250) : Rgb(252, 236, 180);
        Recolour(b, (latios ? Rgb(90, 140, 210) : Rgb(222, 104, 104), main));
        float tilt = latios ? -20f : -35f;
        var lift = Shifted(b, "body", V(0, 0.38f, 0));
        var c = V(0, 0.38f, 0) + lift;
        var r = V(0.085f, 0.095f, 0.22f);
        var turn = V(tilt, 0, 0);
        // The triangle goes: a lit star on its chest
        b.Model.Decals.RemoveAll(d => d.Shape == MarkShape.Triangle);
        var chest = Out(c, r, turn, V(0, -0.3f, 1f));
        var face = Vector3.Normalize(Vector3.Transform(V(0, -0.3f, 1f), Quaternion.CreateFromYawPitchRoll(0, tilt * Degree, 0)));
        b.Mark(Body, chest, face, 0.03f, 0.03f, star, MarkShape.Star);
        // Wings longer and swept back, with gold edges and a second feather behind
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var root = c + V(0.05f * s, 0.06f, 0.02f);
            Grow(b, V(1.35f, 1.2f, 1.35f), root, x => x == wing);
            var tip = root + V(0.34f * s, 0.04f, -0.16f) * 1.35f;
            b.Tube(wing, Smooth(3, root + V(0, 0.012f, 0.03f), Vector3.Lerp(root, tip, 0.5f) + V(0, 0.016f, 0.03f), tip + V(0, 0.005f, 0)), 0.01f, 0.004f, gold, Metal, 0.003f);
            Blade(b, wing, root + V(0, 0.0f, -0.06f), root + V(0.3f * s, 0.1f, -0.36f), 0.07f, main, V(0, 1f, 0.15f), 0.12f);
            b.PaintEll(wing, root + V(0.27f * s, 0.09f, -0.33f), V(0.04f, 0.03f, 0.04f), gold);
        });
        // Horns swept back from the head
        int head = BoneOf(b, "head");
        var front = Out(c, r, turn, V(0, 0, 1f));
        var hc = front + V(0, 0.1f, 0.03f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, hc + V(0.025f * s, 0.04f, -0.02f), hc + V(0.04f * s, 0.09f, -0.06f), hc + V(0.05f * s, 0.12f, -0.13f)), 0.012f, 0.004f, latios ? gold : main, Shell, 0.004f));
        // A long tail fin, edged in gold
        int tail = BoneOf(b, "tail");
        var back = c + V(0, -0.08f, -0.2f);
        var end = back + V(0, -0.06f, -0.3f);
        b.Tube(tail, Smooth(3, back, back + V(0, -0.04f, -0.15f), end), 0.03f, 0.014f, main, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            Blade(b, tail, end, end + V(0.1f * s, 0.02f, -0.12f), 0.04f, main, V(0, 1f, 0.2f), 0.18f);
            b.PaintEll(tail, end + V(0.08f * s, 0.016f, -0.1f), V(0.025f, 0.02f, 0.03f), gold);
        });
        return Lift(b);
    }

    private static PokeBuilder Aurelias() => AureliBuild(Latias(), "Aurelias", false);

    private static PokeBuilder Aurelios() => AureliBuild(Latios(), "Aurelios", true);
}
