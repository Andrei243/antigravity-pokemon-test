using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The species of our own that evolve past level 100 (plan 03 · D14), batch 7: Alola's, in our numbers' order
// (2000 + the forebear's National number), with the ten Ultra Beasts' heirs of its supplement. Each is sculpted from
// its forebear's own sculpt as the look of docs/plans/D14-proposals-alola.md, D14-proposals-alola-supplement.md and the
// user's reference pictures (docs/art/reference/gemini/alola) say: the forebear's build is taken whole (renamed,
// stretched, its colours changed, a part taken away) and the picture's new features are added to it, with the tools
// of batches 1 and 5 (PokemonModels.Ours.Kanto.cs, PokemonModels.Ours.Unova.cs). The forebears' own sculpts are never
// touched.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ 2735 Inspectoose, from Gumshoos

    /// <summary>
    /// Inspectoose: Gumshoos grown longer and darker, its pompadour gone for a dark brim over its brow, a cream collar
    /// of fur turned up round its neck like a trench coat's, a pale ring round one eye like a lens and dark bands round
    /// its long tail.
    /// </summary>
    private static PokeBuilder Inspectoose()
    {
        var b = Become(Gumshoos(), "Inspectoose", 0.9f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var brown = Rgb(110, 90, 70);
        var dark = Rgb(66, 52, 42);
        var cream = Rgb(230, 218, 184);
        var yellow = Rgb(232, 200, 110);
        b.Sdf.RemoveAll(s => s.Bone == head && Is(s.Color, yellow));
        Recolour(b, (Rgb(130, 96, 76), brown), (yellow, Rgb(150, 124, 96)));
        var bc = V(0, 0.24f, 0);
        var br = V(0.08f, 0.13f, 0.07f);
        var c = bc + V(0, br.Y + 0.05f, 0.04f);
        var r = V(0.055f, 0.05f, 0.055f);
        // The brim: a dark cap of fur over the brow, jutting forward
        b.Ell(head, c + V(0, 0.03f, -0.005f), V(0.055f, 0.022f, 0.055f), dark, blend: 0.01f);
        b.Ell(head, c + V(0, 0.034f, 0.05f), V(0.045f, 0.008f, 0.04f), dark, V(-8f, 0, 0), blend: 0.008f);
        // The collar, turned up round the neck
        for (int i = 0; i < 14; i++)
        {
            float a = (i / 13f - 0.5f) * 300f * Degree;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(0, br.Y * 0.8f, 0) + d * 0.06f;
            b.Spike(Body, root, root + d * 0.035f + V(0, 0.045f, 0), 0.022f, cream, 0.45f, blend: 0.01f);
        }
        b.PaintEll(Body, bc + V(0, 0.07f, 0.05f), V(0.04f, 0.05f, 0.03f), cream);
        // The lens: a pale ring round its right eye
        var eye = On(c, r, 0.022f, c.Y + 0.012f);
        b.PaintTorus(head, eye, 0.016f, 0.004f, Rgb(232, 224, 170), Euler(Outward(c, r, eye)));
        // Dark bands round the tail
        var tp = Smooth(3, bc + V(0, -0.09f, -0.06f), V(0, 0.06f, -0.16f), V(0, 0.03f, -0.28f));
        for (int i = 2; i < tp.Length; i += 2)
            b.PaintTorus(tail, tp[i], 0.022f - 0.0015f * i, 0.006f, dark, Euler(tp[Math.Min(i + 1, tp.Length - 1)] - tp[i - 1]));
        Stretch(b, V(1f, 1.15f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2740 Glacipincer, from Crabominable

    /// <summary>
    /// Glacipincer: Crabominable grown larger, its fur pale blue-white, its pincers grown into great gauntlets of blue ice
    /// hung with icicles, a shaggy mane falling over its glowing eyes and a ridge of frost spikes along its back.
    /// </summary>
    private static PokeBuilder Glacipincer()
    {
        var b = Become(Crabominable(), "Glacipincer", 1f);
        var ice = Rgb(150, 200, 236);
        var pale = Rgb(206, 230, 246);
        var fur = Rgb(236, 242, 248);
        Recolour(b, (Rgb(244, 244, 248), fur), (Rgb(70, 60, 120), Rgb(110, 130, 170)), (Rgb(80, 150, 230), Rgb(90, 220, 240)));
        var c = V(0, 0.22f, 0);
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var pc = V(0.22f * s, 0.45f, 0.11f);
            // The gauntlet: blocks of ice over the pincer and icicles hanging from it
            b.Ell(arm, pc + V(0, 0, 0.01f), V(0.095f, 0.09f, 0.075f), ice, mat: Shell, blend: 0.01f);
            b.Box(arm, pc + V(0.03f * s, 0.05f, 0.03f), V(0.04f, 0.035f, 0.035f), 0.012f, pale, V(20f, 30f * s, 10f), Shell, 0.01f);
            b.Box(arm, pc + V(-0.03f * s, -0.03f, 0.05f), V(0.035f, 0.03f, 0.03f), 0.01f, pale, V(-10f, -20f * s, 25f), Shell, 0.01f);
            for (int i = 0; i < 5; i++)
            {
                var root = pc + V((i - 2) * 0.03f, -0.07f, 0.02f * MathF.Cos(i));
                b.Spike(arm, root, root + V(0, -0.06f - 0.015f * (i % 2), 0), 0.012f, pale, mat: Glow, blend: 0.006f);
            }
        });
        // A mane over the eyes
        foreach (var x in new[] { -0.05f, -0.025f, 0f, 0.025f, 0.05f })
            b.Spike(Body, On(c, V(0.11f, 0.1f, 0.09f), x, c.Y + 0.05f) - V(0, 0, 0.02f), On(c, V(0.11f, 0.1f, 0.09f), x * 1.2f, c.Y + 0.02f) + V(0, 0, 0.025f), 0.016f, fur, 0.5f);
        // A ridge of frost spikes along the back
        for (int i = 0; i < 7; i++)
        {
            float a = (i / 6f - 0.5f) * 140f * Degree;
            var d = V(MathF.Sin(a), MathF.Cos(a) * 0.8f, -0.6f);
            var root = c + Vector3.Normalize(d) * 0.08f;
            b.Spike(Body, root, root + Vector3.Normalize(d) * 0.09f, 0.02f, ice, 0.6f, Glow);
        }
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2743 Pollenoble, from Ribombee

    /// <summary>
    /// Pollenoble: Ribombee grown larger, its scarf grown into a great ruff of gold pollen puffs like a crown of flowers,
    /// a little circlet of flowers on its head, tufts of pollen at its antennae's ends and on its legs, a striped body
    /// and a second pair of wings.
    /// </summary>
    private static PokeBuilder Pollenoble()
    {
        var b = Become(Ribombee(), "Pollenoble", 0.7f);
        int head = BoneOf(b, "head");
        var gold = Rgb(236, 190, 70);
        var pollen = Rgb(250, 226, 130);
        var pale = Rgb(240, 236, 170);
        var wing = Rgb(240, 238, 226);
        Unlift(b, V(0, 0.2f, 0));
        Recolour(b, (Rgb(150, 110, 70), Rgb(236, 206, 120)));
        var c = V(0, 0.27f, 0.01f);
        // The ruff of pollen puffs round the neck and shoulders
        for (int ring = 0; ring < 2; ring++)
            for (int i = 0; i < 12; i++)
            {
                float a = (i + ring * 0.5f) / 12f * MathF.Tau;
                var at = V(MathF.Sin(a) * (0.042f + 0.012f * ring), 0.225f - 0.014f * ring, MathF.Cos(a) * (0.036f + 0.01f * ring));
                b.Ell(Body, at, V(0.016f, 0.015f, 0.016f), (i + ring) % 3 == 0 ? pollen : gold, blend: 0.008f);
            }
        // A circlet of little flowers on its head
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * MathF.Tau;
            b.Ell(head, c + V(MathF.Sin(a) * 0.025f, 0.035f, MathF.Cos(a) * 0.022f - 0.005f), V(0.006f, 0.006f, 0.006f), i % 2 == 0 ? pollen : White, blend: 0.004f);
        }
        // Tufts of pollen at the antennae's ends
        PokeBuilder.Both(s => b.Ell(head, c + V(0.02f * s, 0.085f, -0.005f), V(0.012f, 0.009f, 0.01f), gold, blend: 0.004f));
        // Stripes round the body and puffs on the legs
        foreach (var y in new[] { 0.19f, 0.17f, 0.155f })
            b.PaintTorus(Body, V(0, y, 0), 0.023f, 0.004f, pale);
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Ell(leg, V(0.016f * s, 0.075f, 0.005f), V(0.008f, 0.008f, 0.008f), gold, blend: 0.003f);
            // A second, larger pair of wings above the first
            int w = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var root = V(0.025f * s, 0.24f, -0.02f);
            Frond(b, w, root, V(0.16f * s, 0.3f, -0.05f), 0.042f, wing, V(0, 0, 1f), 0.08f, Glow, 0.006f);
        });
        Stretch(b, V(1.3f, 1.3f, 1.3f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2745 Lupinacle, from Lycanroc

    /// <summary>
    /// Lupinacle: Lycanroc of the day grown into a great grey wolf, its rocks grown into a mantle of stone slabs over its
    /// shoulders and neck, banded in the day's sand, the night's red and the dusk's gold, flecks of stone in its coat and
    /// a stone spike at its tail's end banded the same.
    /// </summary>
    private static PokeBuilder Lupinacle()
    {
        var b = Become(Lycanroc(), "Lupinacle", 0.95f);
        int tail = BoneOf(b, "tail");
        var grey = Rgb(150, 142, 126);
        var stone = Rgb(206, 190, 160);
        var red = Rgb(140, 40, 44);
        var gold = Rgb(214, 160, 70);
        var dark = Rgb(70, 72, 66);
        Recolour(b, (Rgb(214, 190, 160), grey), (Rgb(244, 242, 238), Rgb(196, 190, 176)), (Rgb(150, 110, 80), stone));
        var bc = V(0, 0.24f, -0.02f);
        var neck = bc + V(0, 0.05f, 0.1f);
        // The mantle: slabs of stone over the neck and shoulders, overlapping, each banded red and gold
        foreach (var (x, y, z, rot) in new[] { (0f, 0.07f, -0.01f, 0f), (0.045f, 0.04f, 0.0f, -25f), (-0.045f, 0.04f, 0.0f, 25f), (0f, 0.045f, -0.06f, 0f), (0.06f, 0.0f, 0.03f, -45f), (-0.06f, 0.0f, 0.03f, 45f) })
        {
            var at = neck + V(x, y, z);
            b.Box(Body, at, V(0.045f, 0.012f, 0.045f), 0.008f, stone, V(-20f, 0, rot), Shell, 0.01f);
            b.PaintEll(Body, at + V(0, 0, 0.035f), V(0.05f, 0.03f, 0.008f), red, V(-20f, 0, rot), 0.004f);
            b.PaintEll(Body, at + V(0, 0, 0.025f), V(0.05f, 0.03f, 0.005f), gold, V(-20f, 0, rot), 0.003f);
        }
        // Dark fur under the mantle and flecks of stone in the coat
        b.PaintEll(Body, bc + V(0, 0.05f, 0.05f), V(0.06f, 0.03f, 0.08f), dark);
        foreach (var (x, y, z) in new[] { (0.05f, 0.02f, -0.02f), (-0.05f, 0.03f, -0.06f), (0.04f, 0.04f, -0.09f), (-0.045f, 0.0f, 0.02f) })
            b.Spike(Body, bc + V(x, y, z), bc + V(x * 1.25f, y + 0.012f, z), 0.008f, stone, 0.6f, Shell);
        // The stone spike at its tail's end
        var tip = bc + V(0, 0.1f, -0.2f);
        b.Spike(tail, tip, tip + V(0, 0.09f, -0.04f), 0.03f, stone, 0.7f, Shell);
        b.PaintTorus(tail, tip + V(0, 0.02f, -0.01f), 0.026f, 0.005f, red, V(-25f, 0, 0));
        b.PaintTorus(tail, tip + V(0, 0.035f, -0.015f), 0.022f, 0.004f, gold, V(-25f, 0, 0));
        Stretch(b, V(1.12f, 1.15f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2748 Venomreef, from Toxapex

    /// <summary>
    /// Venomreef: Toxapex grown into three domes joined at their rims, dark blue crusted with small shells, long dark
    /// magenta spikes standing out of them, a ring of yellow eyes peeping from the gaps between the domes and clawed
    /// legs beneath.
    /// </summary>
    private static PokeBuilder Venomreef()
    {
        var b = Become(Toxapex(), "Venomreef", 0.9f);
        var blue = Rgb(50, 84, 112);
        var dark = Rgb(30, 40, 60);
        var spike = Rgb(150, 50, 100);
        var shell = Rgb(150, 120, 160);
        var glow = Rgb(250, 210, 80);
        Recolour(b, (Rgb(140, 210, 226), blue), (Rgb(110, 80, 160), spike), (Rgb(240, 120, 170), Rgb(190, 70, 120)));
        // Two more domes beside the first, joined at their rims, each spiked
        PokeBuilder.Both(s =>
        {
            var dc = V(0.18f * s, 0.12f, 0.06f);
            var dr = V(0.13f, 0.11f, 0.13f);
            b.Ell(Body, dc, dr, blue, mat: Shell, blend: 0.02f);
            b.PaintTorus(Body, dc + V(0, -0.04f, 0), 0.12f, 0.012f, dark, sx: 1f, sz: 1f);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 2.39996f;
                float y = 1f - 2f * (i + 0.5f) / 24f;
                float ring = MathF.Sqrt(1f - y * y);
                var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
                var at = dc + n * dr * 0.95f;
                b.Spike(Body, at, at + n * 0.08f, 0.014f, spike, 0.6f, Shell);
            }
            foreach (var (x, y, z) in new[] { (0.05f, 0.08f, 0.08f), (-0.06f, 0.04f, 0.1f), (0.08f, -0.02f, 0.08f) })
                b.Ell(Body, dc + V(x * s, y, z), V(0.012f, 0.01f, 0.008f), shell, mat: Shell, blend: 0.004f);
            // Eyes peeping from the gap at the rim
            for (int i = 0; i < 4; i++)
                b.Ell(Body, dc + V((-0.06f + 0.035f * i) * s, -0.045f, 0.12f - 0.012f * MathF.Abs(i - 1.5f)), V(0.008f, 0.006f, 0.004f), glow, mat: Glow, blend: 0.002f);
            // Clawed legs beneath
            b.Tube(Body, Smooth(3, dc + V(0.02f * s, -0.08f, 0.04f), dc + V(0.08f * s, -0.1f, 0.08f), V(0.25f * s, 0.01f, 0.16f)), 0.022f, 0.012f, Rgb(80, 70, 140), Shell, 0f);
        });
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2750 Claysteed, from Mudsdale

    /// <summary>
    /// Claysteed: Mudsdale grown greater, the red mud over its back fired into plates of clay laid like a saddle over its
    /// back and round its chest, its mane and tail in heavy braids, its lower legs caked in dried mud and its hooves wider.
    /// </summary>
    private static PokeBuilder Claysteed()
    {
        float k = 1.6f;
        var b = Become(Mudsdale(), "Claysteed", 1f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var brown = Rgb(90, 66, 52);
        var clay = Rgb(206, 104, 70);
        var mud = Rgb(120, 98, 78);
        var braid = Rgb(60, 58, 50);
        Recolour(b, (Rgb(110, 76, 60), brown), (Rgb(200, 90, 60), mud), (Rgb(50, 46, 50), braid));
        var bc = V(0, 0.18f, 0) * k;
        var br = V(0.07f, 0.065f, 0.13f) * k;
        // The saddle of clay: plates over the back and the flanks, and a breastplate round the chest
        b.Ell(Body, bc + V(0, br.Y * 0.95f, -0.01f), V(0.07f, 0.03f, 0.08f), clay, mat: Shell, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 4; i++)
            {
                var at = Out(bc, br, default, V(0.8f * s, 0.55f, 0.5f - 0.33f * i));
                b.Box(Body, at, V(0.012f, 0.03f, 0.028f), 0.008f, clay, V(0, 0, -50f * s), Shell, 0.008f);
            }
            foreach (var t in new[] { 0.35f, 0.7f })
            {
                var at = Out(bc, br, default, V(0.55f * s, -0.05f, 0.9f)) + V(0.02f * s * t, 0.03f, 0.01f);
                b.Box(Body, at, V(0.03f, 0.022f, 0.012f), 0.008f, clay, V(0, 40f * s, 0), Shell, 0.008f);
            }
        });
        b.Box(Body, Out(bc, br, default, V(0, 0.1f, 1f)), V(0.03f, 0.025f, 0.012f), 0.008f, clay, default, Shell, 0.008f);
        // Braids: beads of hair down each lock of the mane and the tail
        foreach (var s in b.Sdf.Shapes.Where(s => s.Bone == head && Is(s.Color, braid) && s.Shape == SdfShape.RoundCone).ToList())
            b.Ell(head, (s.A + s.B) / 2f, V(0.016f, 0.016f, 0.016f), braid, blend: 0.004f);
        var tp = Smooth(3, bc + V(0, 0.02f, -0.12f) * k, bc + V(0, -0.04f, -0.18f) * k, bc + V(0, -0.1f, -0.2f) * k);
        foreach (var p in tp)
            b.Ell(tail, p, V(0.022f, 0.022f, 0.022f), braid, blend: 0.006f);
        // Wider hooves
        foreach (var z in new[] { 0.08f * k, -0.1f * k })
            PokeBuilder.Both(s => b.Ell(Root, V(0.052f * k * 1.05f * s, 0.022f, z + 0.03f), V(0.05f, 0.022f, 0.06f), Rgb(150, 120, 90), mat: Shell, blend: 0.01f));
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2752 Diveweaver, from Araquanid

    /// <summary>
    /// Diveweaver: Araquanid grown larger and longer-legged, its bubble grown into a great dome of water over its whole
    /// back, its legs brown banded in green, a small bubble held at each leg's tip and its dark face before the dome.
    /// </summary>
    private static PokeBuilder Diveweaver()
    {
        float k = 1.8f;
        var b = Become(Araquanid(), "Diveweaver", 0.95f);
        var brown = Rgb(150, 124, 84);
        var band = Rgb(70, 110, 70);
        var shell = Rgb(40, 90, 90);
        var water = Rgb(150, 210, 230);
        Recolour(b, (Rgb(200, 220, 90), brown), (Rgb(70, 70, 90), shell), (Rgb(60, 100, 170), Rgb(40, 80, 90)));
        var bc = V(0, 0.09f, -0.03f) * k;
        // The dome of water over its back
        b.Ell(Body, bc + V(0, 0.07f, -0.07f) * k, V(0.1f, 0.09f, 0.1f) * k, water, mat: Glow, blend: 0.02f);
        // Bands on the legs and a bubble at each leg's tip
        foreach (var s in b.Sdf.Shapes.Where(s => s.Shape == SdfShape.RoundCone && s.Op == SdfOp.Add && s.Bone != Body && (Is(s.Color, brown) || Is(s.Color, shell))).ToList())
        {
            b.PaintEll(s.Bone, Vector3.Lerp(s.A, s.B, 0.5f), V(0.016f, 0.016f, 0.016f), band, soft: 0.006f);
            if (s.B.Y < 0.01f)
                b.Ell(s.Bone, s.B + V(0, 0.018f, 0), V(0.022f, 0.022f, 0.022f), water, mat: Glow, blend: 0.006f);
        }
        Stretch(b, V(1.12f, 1.2f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2754 Orchimantis, from Lurantis

    /// <summary>
    /// Orchimantis: Lurantis grown tall, its body green, four sickles of pink petal where it had two, the frill on its
    /// head grown into an orchid's lip of white and pink petals and a long train of pink petals falling behind it.
    /// </summary>
    private static PokeBuilder Orchimantis()
    {
        var b = Become(Lurantis(), "Orchimantis", 0.95f);
        int head = BoneOf(b, "head");
        var pink = Rgb(236, 140, 170);
        var pale = Rgb(250, 226, 236);
        var green = Rgb(130, 180, 100);
        Recolour(b, (Rgb(250, 228, 232), green));
        var bc = V(0, 0.32f, 0);
        var c = bc + V(0, 0.16f, 0.02f);
        // The second pair of sickles, lower down
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = V(0.04f * s, 0.36f, 0.02f);
            var elbow = shoulder + V(0.07f * s, -0.02f, 0.03f);
            b.Limb(arm, shoulder, elbow, 0.012f, 0.011f, green);
            Sickle(b, arm, elbow, elbow + V(0.05f * s, -0.06f, 0.02f), elbow + V(0.02f * s, -0.16f, 0.0f), 0.036f, pink, V(s, 0, 0));
        });
        // The orchid's lip: broad white petals flecked pink round the head
        for (int i = 0; i < 5; i++)
        {
            float a = (i / 4f - 0.5f) * 200f * Degree;
            var d = V(MathF.Sin(a), MathF.Cos(a), -0.3f);
            Petal(b, head, c + d * 0.02f, c + Vector3.Normalize(d) * 0.1f, 0.04f, i % 2 == 0 ? pale : Rgb(246, 196, 214), Leaf);
        }
        // The train of petals
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3) * 0.02f;
            var root = bc + V(x * 0.6f, -0.06f, -0.03f);
            Petal(b, Body, root, root + V(x * 1.5f, -0.24f + 0.01f * MathF.Abs(i - 3), -0.18f + 0.015f * MathF.Abs(i - 3)), 0.035f, i % 2 == 0 ? pink : pale, Leaf);
        }
        Stretch(b, V(1f, 1.15f, 1f));
        return b;
    }

    // ------------------------------------------------------------------ 2756 Lumishroom, from Shiinotic

    /// <summary>
    /// Lumishroom: Shiinotic grown taller, its cap a wide parasol of deep purple, its spots glowing green, its gills pale
    /// green, a ruff of leaves at its neck and a fairy ring of small glowing caps round its foot on a ring of moss.
    /// </summary>
    private static PokeBuilder Lumishroom()
    {
        var b = Become(Shiinotic(), "Lumishroom", 0.85f);
        int head = BoneOf(b, "head");
        int leg = BoneOf(b, "legR");
        var purple = Rgb(110, 70, 170);
        var glow = Rgb(130, 250, 170);
        var gill = Rgb(200, 240, 200);
        var moss = Rgb(110, 150, 100);
        Recolour(b, (Rgb(160, 120, 190), purple), (Rgb(240, 230, 120), glow), (Rgb(246, 244, 236), Rgb(214, 200, 236)), (Rgb(246, 196, 214), Rgb(200, 186, 230)), (Rgb(214, 236, 170), gill));
        // The parasol: the cap grown wide
        var cap = V(0, 0.36f, 0);
        Grow(b, V(1.45f, 1f, 1.45f), cap, s => s.Bone == head && s.A.Y > 0.3f, _ => false);
        // Glowing spots raised on the cap
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f + 0.3f;
            var at = cap + V(MathF.Sin(a) * 0.14f, 0.035f, MathF.Cos(a) * 0.13f);
            b.Ell(head, at, V(0.014f, 0.008f, 0.012f), glow, mat: Glow, blend: 0.006f);
        }
        // A ruff of leaves at the neck
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, V(0, 0.21f, 0.0f) + d * 0.02f, V(0, 0.19f, 0) + d * 0.05f, 0.012f, Rgb(120, 170, 110), V(0, 1f, 0) + d, 0.25f, Leaf);
        }
        // The fairy ring round its foot: a ring of moss joined to the foot by roots, small caps along it
        b.Torus(leg, V(0, 0.008f, 0.005f), 0.1f, 0.008f, moss, blend: 0.004f);
        foreach (var a in new[] { 0.5f, 2.6f, 4.7f })
            b.Limb(leg, V(0, 0.01f, 0.005f), V(MathF.Sin(a) * 0.1f, 0.006f, MathF.Cos(a) * 0.1f + 0.005f), 0.006f, 0.005f, moss);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            var at = V(MathF.Sin(a) * 0.1f, 0.008f, MathF.Cos(a) * 0.1f + 0.005f);
            float h = 0.025f + 0.01f * (i % 3);
            b.Limb(leg, at, at + V(0, h, 0), 0.004f, 0.004f, Rgb(214, 200, 236));
            b.Ell(leg, at + V(0, h + 0.004f, 0), V(0.016f, 0.009f, 0.016f), purple, blend: 0.004f);
            b.PaintEll(leg, at + V(0.006f, h + 0.012f, 0), V(0.004f, 0.004f, 0.004f), glow, soft: 0.003f);
        }
        Stretch(b, V(1.1f, 1.2f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2758 Smolderess, from Salazzle

    /// <summary>
    /// Smolderess: Salazzle grown taller, black lined with glowing pink, a crown of pink and orange frills standing up
    /// round the back of her head, a haze of violet smoke trailing from her shoulders and her tail long and forked at
    /// its end into two fins of flame.
    /// </summary>
    private static PokeBuilder Smolderess()
    {
        var b = Become(Salazzle(), "Smolderess", 0.95f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var pink = Rgb(240, 80, 170);
        var orange = Rgb(246, 140, 70);
        var smoke = Rgb(150, 100, 170);
        var bc = V(0, 0.3f, 0);
        var neckTop = bc + V(0, 0.2f, 0.05f);
        var c = neckTop + V(0, 0.015f, 0.03f);
        // Lines of glowing pink down the body and limbs
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, bc + V(0.03f * s, 0.04f, 0.035f), V(0.004f, 0.05f, 0.004f), pink, V(0, 0, 20f * s), Glow, 0.003f);
            b.Ell(Body, bc + V(0.035f * s, -0.04f, 0.032f), V(0.004f, 0.035f, 0.004f), pink, V(0, 0, -15f * s), Glow, 0.003f);
        });
        // The crown of frills round the back of the head
        for (int i = 0; i < 7; i++)
        {
            float a = (i / 6f - 0.5f) * 220f * Degree;
            var d = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a) * 0.9f + 0.3f, -0.5f));
            var root = c + V(0, 0.01f, -0.025f);
            FlameTongue(b, head, root, root + d * 0.09f, 0.016f, pink, orange);
        }
        // Smoke trailing from her shoulders
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.025f * s, 0.07f, -0.015f);
            var path = Smooth(3, root, root + V(0.05f * s, 0.05f, -0.06f), root + V(0.06f * s, 0.02f, -0.14f), root + V(0.08f * s, 0.07f, -0.22f));
            Ribbon(b, Body, path, 0.03f, smoke, V(0, 1f, 0), 0.01f);
        });
        // The tail runs on and forks into two fins of flame
        var end = V(0.14f, 0.04f, -0.28f);
        b.Tube(tail, Smooth(3, end, V(0.2f, 0.06f, -0.3f), V(0.25f, 0.12f, -0.28f)), 0.008f, 0.008f, Rgb(56, 56, 66), blend: 0.004f);
        var fork = V(0.25f, 0.12f, -0.28f);
        foreach (var d in new[] { V(0.02f, 0.1f, 0.02f), V(0.06f, 0.08f, -0.03f) })
            FlameTongue(b, tail, fork, fork + d, 0.02f, pink, orange);
        Stretch(b, V(1f, 1.15f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2760 Huggrizzle, from Bewear

    /// <summary>
    /// Huggrizzle: Bewear grown greater and turned about, its body pink and thick-furred, a white patch on its belly, its
    /// hood grown black into a shaggy cape of fur that falls from its head down its back to its knees, its arms longer.
    /// </summary>
    private static PokeBuilder Huggrizzle()
    {
        var b = Become(Bewear(), "Huggrizzle", 1f);
        int head = BoneOf(b, "head");
        var pink = Rgb(236, 140, 170);
        var black = Rgb(48, 44, 46);
        Recolour(b, (Rgb(50, 46, 52), pink), (Rgb(248, 246, 244), black), (Rgb(240, 160, 176), pink));
        foreach (var ear in new[] { BoneOf(b, "earL"), BoneOf(b, "earR") })
            Tint(b, s => s.Bone == ear, pink);
        var bc = V(0, 0.36f, 0);
        var br = V(0.14f, 0.18f, 0.11f);
        // The white patch on its belly
        b.PaintEll(Body, Out(bc, br, default, V(0, -0.3f, 1f)), V(0.05f, 0.05f, 0.03f), Rgb(244, 236, 220));
        // Thick fur: tufts over the arms' and body's sides
        FurTufts(b, Body, bc, br, 18, 0.03f, 0.016f, pink, 0.8f, -0.8f, 0.7f);
        // The cape: shaggy black fur from the hood over the shoulders and down the back to the knees
        b.Ell(Body, bc + V(0, 0.16f, -0.03f), V(0.16f, 0.06f, 0.12f), black, blend: 0.02f);
        for (int i = 0; i < 9; i++)
        {
            float a = (i / 8f - 0.5f) * 220f * Degree + MathF.PI;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(0, 0.16f, -0.02f) + d * V(0.13f, 0, 0.1f);
            Blade(b, Body, root, root + d * 0.05f + V(0, -0.36f, 0), 0.06f, black, d, 0.2f);
        }
        Stretch(b, V(1.1f, 1.12f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2764 Wreathfey, from Comfey

    /// <summary>
    /// Wreathfey: Comfey grown into a garland, a second, greater ring of braided vine round its first so that the two
    /// make a wreath, both strung with flowers in four colours, its face crowned with leaves and three long tails of vine
    /// trailing down from it with buds along them.
    /// </summary>
    private static PokeBuilder Wreathfey()
    {
        var b = Become(Comfey(), "Wreathfey", 0.7f);
        var vine = Rgb(100, 166, 84);
        Unlift(b, V(0, 0.2f, 0));
        var c = V(0, 0.2f, 0);
        const float outer = 0.22f;
        b.Torus(Body, c, outer, 0.016f, vine, V(90f, 0, 0), blend: 0.006f);
        b.Torus(Body, c, outer, 0.01f, Rgb(80, 140, 70), V(90f, 20f, 0), blend: 0.004f);
        // Links of vine joining the two rings
        foreach (var deg in new[] { 30f, 150f, 270f, 210f, 330f })
        {
            float a = deg * Degree;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            b.Limb(Body, c + d * 0.13f, c + d * outer, 0.006f, 0.006f, vine);
        }
        // Flowers in four colours round the greater ring
        var colours = new[] { Rgb(240, 140, 150), Rgb(250, 214, 110), Rgb(150, 140, 220), Rgb(248, 244, 240) };
        for (int i = 0; i < 12; i++)
        {
            float a = (i * 30f + 15f) * Degree;
            var at = c + V(MathF.Cos(a), MathF.Sin(a), 0) * outer + V(0, 0, 0.014f);
            FlowerHead(b, Body, at, V(0, 0, 1f), 5, 0.032f, 0.009f, colours[i % 4], Rgb(250, 220, 110), 0.008f, i * 0.4f);
        }
        // Three tails of vine trailing down, buds along them
        for (int i = 0; i < 3; i++)
        {
            float a = (-80f + 15f * i) * Degree;
            var root = c + V(MathF.Cos(a), MathF.Sin(a), 0) * outer;
            var path = Smooth(3, root, root + V(0.03f - 0.03f * i, -0.08f, 0.01f), root + V(-0.02f * i, -0.18f, 0.02f), root + V(-0.06f + 0.04f * i, -0.26f, 0.0f));
            b.Tube(Body, path, 0.007f, 0.004f, vine, blend: 0.003f);
            for (int k = 3; k < path.Length; k += 3)
                b.Ell(Body, path[k] + V(0.006f, 0, 0), V(0.007f, 0.009f, 0.007f), Rgb(240, 150, 170), blend: 0.003f);
        }
        Stretch(b, V(1.1f, 1.1f, 1.1f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2765 Arborsage, from Oranguru

    /// <summary>A fan of leaves on a short handle: <paramref name="count"/> fronds spread from <paramref name="grip"/> between two angles in the plane facing <paramref name="facing"/>.</summary>
    private static void LeafFan(PokeBuilder b, int bone, Vector3 grip, Vector3 handle, float from, float to, int count, float length, Color leaf)
    {
        b.Limb(bone, grip - handle, grip, 0.01f, 0.01f, Rgb(150, 110, 70));
        for (int i = 0; i < count; i++)
        {
            float a = (from + (to - from) * i / (count - 1f)) * Degree;
            Frond(b, bone, grip, grip + V(MathF.Cos(a), MathF.Sin(a), 0.1f) * length, 0.032f, leaf, V(0, 0, 1f), 0.2f, Leaf);
        }
    }

    /// <summary>
    /// Arborsage: Oranguru grown into an old sage, its purple cape grown into robes of hair falling to the ground and
    /// hung with leaves, its white mane long and a white beard falling to its knees from its dark face, a crown of leaves
    /// on its brow and a fan of leaves in each hand.
    /// </summary>
    private static PokeBuilder Arborsage()
    {
        var b = Become(Oranguru(), "Arborsage", 1f);
        int head = BoneOf(b, "head");
        var white = Rgb(238, 238, 234);
        var purple = Rgb(96, 64, 130);
        var dark = Rgb(70, 72, 96);
        var leaf = Rgb(82, 150, 84);
        Tint(b, s => s.Bone != head && Is(s.Color, white) && s.A.Y > 0.15f, purple);
        Tint(b, s => s.Bone != head && Is(s.Color, white), dark);
        Recolour(b, (Rgb(132, 134, 144), dark));
        var bc = V(0, 0.33f, 0);
        var c = bc + V(0, 0.2f, 0.06f);
        // The robes: hair falling from the shoulders to the ground all round, leaves caught in it
        b.Ell(Body, bc + V(0, -0.12f, -0.01f), V(0.17f, 0.14f, 0.14f), purple, blend: 0.03f);
        for (int i = 0; i < 12; i++)
        {
            float a = (i + 0.5f) / 12f * MathF.Tau;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(0, 0.06f, 0) + d * V(0.14f, 0, 0.11f);
            var tip = V(0, 0.01f, 0) + d * V(0.25f, 0, 0.2f);
            Blade(b, Body, root, tip, 0.07f, i % 2 == 0 ? purple : Rgb(84, 56, 116), d, 0.2f);
            Frond(b, Body, Vector3.Lerp(root, tip, 0.7f), Vector3.Lerp(root, tip, 0.85f) + d * 0.03f, 0.016f, leaf, d, 0.35f, Leaf, 0.01f);
        }
        // The beard, falling from the chin to the knees and curling at its end
        var beard = Smooth(3, c + V(0, -0.06f, 0.06f), c + V(0, -0.16f, 0.1f), bc + V(0.01f, -0.1f, 0.15f), bc + V(0.05f, -0.22f, 0.16f), bc + V(0.09f, -0.24f, 0.14f));
        Ribbon(b, head, beard, 0.06f, white, V(0, 0.2f, 1f), 0.02f);
        // The mane grown long down its back
        PokeBuilder.Both(s => Blade(b, head, c + V(0.05f * s, 0.03f, -0.04f), c + V(0.12f * s, -0.12f, -0.12f), 0.05f, white, V(s, 0.3f, -0.3f), 0.25f));
        // A crown of leaves on its brow
        foreach (var x in new[] { -0.02f, 0f, 0.02f })
            Frond(b, head, c + V(x, 0.06f, 0.03f), c + V(x * 2f, 0.1f, 0.02f), 0.012f, leaf, V(0, 0, 1f), 0.25f, Leaf);
        // A second fan, in its left hand
        var hand = bc + V(-0.19f, -0.19f, 0.13f);
        LeafFan(b, BoneOf(b, "armL"), hand + V(0, 0.07f, 0.03f), V(0, 0.08f, 0.0f), 30f, 150f, 7, 0.13f, leaf);
        Stretch(b, V(1.1f, 1.12f, 1.1f));
        return b;
    }

    // ------------------------------------------------------------------ 2766 Scrummian, from Passimian

    /// <summary>
    /// Scrummian: Passimian grown into a great ape of a captain, its arms thicker and patched orange and black at the
    /// shoulders and forearms, a striped band round its brow in place of its mask's tape, a wrap round one forearm and a
    /// belt of vine with a second green berry hanging at it.
    /// </summary>
    private static PokeBuilder Scrummian()
    {
        var b = Become(Passimian(), "Scrummian", 1f);
        int head = BoneOf(b, "head");
        var white = Rgb(240, 240, 236);
        var orange = Rgb(206, 104, 50);
        var black = Rgb(60, 58, 60);
        var lime = Rgb(110, 170, 64);
        var vine = Rgb(110, 140, 70);
        Recolour(b, (Rgb(168, 212, 64), lime));
        var bc = V(0, 0.38f, 0.02f);
        var c = bc + V(0, 0.17f, 0.06f);
        // Thicker arms, patched orange and black at the shoulders and the forearms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = bc + V(0.08f * s, 0.08f, 0.01f);
            var elbow = shoulder + V(0.06f * s, -0.15f, 0.05f);
            b.Limb(arm, shoulder, elbow, 0.045f, 0.04f, white);
            b.Limb(arm, elbow, elbow + V(0.005f * s, -0.12f, 0.04f), 0.04f, 0.03f, white);
            b.PaintEll(arm, shoulder + V(0.02f * s, 0.01f, 0), V(0.04f, 0.035f, 0.04f), orange);
            b.PaintEll(arm, shoulder + V(0.035f * s, 0.0f, -0.01f), V(0.02f, 0.025f, 0.03f), black);
            b.PaintEll(arm, elbow + V(0.01f * s, -0.06f, 0.02f), V(0.04f, 0.035f, 0.04f), orange);
            if (s > 0)
            {
                b.Torus(arm, elbow + V(0, -0.09f, 0.03f), 0.034f, 0.008f, orange, Euler(V(0, 1f, -0.3f)), blend: 0.004f);
                b.Torus(arm, elbow + V(0, -0.075f, 0.026f), 0.036f, 0.007f, black, Euler(V(0, 1f, -0.3f)), blend: 0.004f);
            }
        });
        // The captain's band round its brow
        b.Torus(head, c + V(0, 0.025f, -0.004f), 0.052f, 0.01f, Rgb(200, 170, 130), V(-8f, 0, 0), blend: 0.004f);
        foreach (var a in new[] { -40f, 0f, 40f })
            b.PaintEll(head, c + V(MathF.Sin(a * Degree) * 0.055f, 0.025f, MathF.Cos(a * Degree) * 0.055f), V(0.008f, 0.012f, 0.008f), black, soft: 0.004f);
        // A belt of vine round the waist, and a second berry hanging at it
        b.Torus(Body, bc + V(0, -0.07f, 0.0f), 0.08f, 0.01f, vine, V(15f, 0, 0), blend: 0.006f);
        b.Ell(Body, bc + V(0.07f, -0.1f, 0.06f), V(0.04f, 0.04f, 0.04f), lime, mat: Shell, blend: 0.006f);
        Stretch(b, V(1.15f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2768 Shogunpod, from Golisopod

    /// <summary>
    /// Shogunpod: Golisopod grown into a samurai, its plates slate grey layered like armour and edged in gold, its joints
    /// lit teal, a gold crest rising from its helmet, its claws longer and a fan of tail plates behind.
    /// </summary>
    private static PokeBuilder Shogunpod()
    {
        var b = Become(Golisopod(), "Shogunpod", 1f);
        int head = BoneOf(b, "head");
        int tail = BoneOf(b, "tail");
        var slate = Rgb(110, 128, 128);
        var dark = Rgb(56, 72, 80);
        var gold = Rgb(214, 176, 90);
        var teal = Rgb(90, 220, 220);
        Recolour(b, (Rgb(212, 218, 214), slate), (Rgb(96, 72, 118), dark), (Rgb(60, 112, 104), gold));
        var bc = V(0, 0.44f, 0);
        var c = bc + V(0, 0.17f, 0.11f);
        // Gold edges on the shell's layers and on the pauldrons
        for (int i = 0; i < 4; i++)
            b.PaintTorus(Body, bc + V(0, 0.18f - i * 0.07f, -0.07f), 0.15f - 0.012f * i, 0.005f, gold, V(-20f, 0, 0), 1f, 0.6f);
        PokeBuilder.Both(s => b.PaintTorus(Body, bc + V(0.14f * s, 0.07f, 0.0f), 0.07f, 0.005f, gold));
        // The crest: a tall middle blade and two horns curving up from the helmet
        Blade(b, head, c + V(0, 0.05f, 0.0f), c + V(0, 0.2f, 0.02f), 0.03f, gold, V(0, 0, 1f), 0.2f, Metal);
        b.Ell(head, c + V(0, 0.11f, 0.024f), V(0.008f, 0.02f, 0.006f), teal, mat: Glow, blend: 0.003f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.02f * s, 0.05f, 0.01f), c + V(0.07f * s, 0.1f, 0.01f), c + V(0.1f * s, 0.19f, 0.0f)), 0.012f, 0.005f, gold, Metal, 0.004f));
        // Teal light at the joints
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.17f * s, 0.08f, 0.02f);
            var elbow = shoulder + V(0.08f * s, -0.12f, 0.05f);
            var wrist = elbow + V(-0.01f * s, -0.12f, 0.08f);
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Torus(arm, elbow, 0.044f, 0.007f, teal, Euler(wrist - shoulder), mat: Glow, blend: 0.003f);
            // Longer claws
            b.Spike(arm, wrist + V(0, -0.02f, 0.02f), wrist + V(0.02f * s, -0.26f, 0.12f), 0.02f, gold, mat: Metal);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Torus(leg, V(0.12f * s, 0.13f, 0.04f), 0.036f, 0.006f, teal, mat: Glow, blend: 0.003f);
        });
        // A fan of plates at the tail
        for (int i = 0; i < 5; i++)
        {
            float a = (i / 4f - 0.5f) * 120f * Degree;
            Blade(b, tail, V(0, 0.08f, -0.2f), V(MathF.Sin(a) * 0.1f, 0.06f, -0.28f - 0.03f * MathF.Cos(a)), 0.025f, slate, V(0, 1f, 0), 0.25f, Shell);
        }
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2770 Sandcitadel, from Palossand

    /// <summary>A child's spade: a handle with a grip and a blade, from <paramref name="top"/> down to <paramref name="tip"/>.</summary>
    private static void Spade(PokeBuilder b, int bone, Vector3 top, Vector3 tip, Color color)
    {
        var d = Vector3.Normalize(tip - top);
        var blade = Vector3.Lerp(top, tip, 0.65f);
        b.Limb(bone, top, blade, 0.008f, 0.008f, Rgb(150, 110, 70));
        b.Torus(bone, top, 0.016f, 0.005f, color, Euler(Vector3.Cross(d, V(0, 0, 1f))), blend: 0.003f);
        Frond(b, bone, blade - d * 0.01f, tip, 0.035f, color, V(0, 0, 1f), 0.25f, Shell);
    }

    /// <summary>
    /// Sandcitadel: Palossand grown into a castle of three towers, the tallest rising from its keep's top, all
    /// crenellated, its windows and gate lit purple from within, shells and spirals pressed into its walls, a moat of dark
    /// sand swirling round its base and a spade, red and blue, stuck in each of its arms.
    /// </summary>
    private static PokeBuilder Sandcitadel()
    {
        var b = Become(Palossand(), "Sandcitadel", 1f);
        var sand = Rgb(206, 170, 116);
        var moat = Rgb(100, 70, 100);
        var glow = Rgb(190, 110, 250);
        var shell = Rgb(220, 200, 200);
        Recolour(b, (Rgb(226, 172, 110), sand), (Rgb(60, 40, 30), Rgb(70, 40, 90)));
        // The middle tower rising from the keep's top, crenellated
        var top = V(0, 0.62f, -0.01f);
        b.Limb(Body, V(0, 0.44f, -0.01f), top, 0.09f, 0.08f, sand);
        b.Limb(Body, top, top + V(0, 0.03f, 0), 0.095f, 0.095f, sand);
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * MathF.Tau;
            b.Box(Body, top + V(MathF.Sin(a) * 0.08f, 0.05f, MathF.Cos(a) * 0.08f), V(0.02f, 0.025f, 0.02f), 0.005f, sand, V(0, a / Degree, 0));
        }
        // The red spade stuck in the middle tower's top is kept; windows lit purple
        foreach (var (y, x, z) in new[] { (0.56f, -0.03f, 0.08f), (0.56f, 0.03f, 0.08f), (0.3f, -0.06f, 0.11f), (0.3f, 0.06f, 0.11f), (0.3f, 0f, 0.125f) })
            b.Ell(Body, V(x, y, z), V(0.012f, 0.022f, 0.008f), glow, mat: Glow, blend: 0.004f);
        b.Ell(Body, V(0, 0.19f, 0.12f), V(0.04f, 0.055f, 0.02f), glow, mat: Glow, blend: 0.004f);
        // Shells and spirals pressed into the walls
        foreach (var (x, y) in new[] { (-0.09f, 0.22f), (0.08f, 0.16f), (-0.05f, 0.37f) })
            b.Ell(Body, V(x, y, MathF.Sqrt(MathF.Max(0f, 0.13f * 0.13f - x * x)) - 0.003f), V(0.012f, 0.01f, 0.006f), shell, mat: Shell, blend: 0.003f);
        // The moat of dark sand swirling round the base
        b.Torus(Body, V(0, 0.02f, 0), 0.3f, 0.025f, moat, sz: 0.78f, blend: 0.02f);
        b.Torus(Body, V(0, 0.015f, 0), 0.36f, 0.015f, moat, sz: 0.76f, blend: 0.02f);
        // A spade in each arm, red and blue
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var at = V((s > 0 ? 0.27f : 0.25f) * s, s > 0 ? 0.34f : 0.4f, 0.04f);
            Spade(b, arm, at + V(0.05f * s, 0.06f, 0.02f), at + V(0.02f * s, -0.14f, 0.07f), s < 0 ? Rgb(196, 70, 50) : Rgb(70, 110, 200));
        });
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return b;
    }

    // ------------------------------------------------------------------ 2771 Cucumbrawl, from Pyukumuku

    /// <summary>
    /// Cucumbrawl: Pyukumuku grown larger and reared up at its front, its pink spines longer, a thick band of pink round
    /// its middle and two white fists of its insides held up before its face like boxing gloves.
    /// </summary>
    private static PokeBuilder Cucumbrawl()
    {
        var b = Become(Pyukumuku(), "Cucumbrawl", 0.7f);
        var black = Rgb(48, 44, 52);
        var pink = Rgb(236, 90, 140);
        var bc = V(0, 0.075f, 0);
        var br = V(0.085f, 0.07f, 0.16f);
        var fc = bc + V(0, -0.005f, 0.11f);
        // The front reared up
        b.Ell(Body, fc + V(0, 0.06f, 0.0f), V(0.07f, 0.08f, 0.065f), black, blend: 0.03f);
        // Longer spines over the reared front
        for (int i = 0; i < 9; i++)
        {
            float a = (i / 8f - 0.5f) * 150f * Degree;
            var d = V(MathF.Sin(a), MathF.Cos(a), -0.4f);
            var at = fc + V(0, 0.06f, -0.01f) + Vector3.Normalize(d) * 0.065f;
            b.Spike(Body, at, at + Vector3.Normalize(d) * 0.07f, 0.012f, pink, mat: Shell);
        }
        // The band round the middle
        b.Torus(Body, bc + V(0, 0.005f, 0.0f), 0.072f, 0.013f, pink, V(90f, 0, 0), sx: 1.1f, blend: 0.008f);
        // Two white fists held up before the face
        PokeBuilder.Both(s =>
        {
            var fist = fc + V(0.06f * s, 0.04f, 0.08f);
            b.Limb(Body, fc + V(0.04f * s, 0.02f, 0.03f), fist, 0.016f, 0.02f, black);
            b.Ell(Body, fist + V(0, 0, 0.012f), V(0.035f, 0.033f, 0.035f), White, blend: 0.008f);
            b.Ell(Body, fist + V(-0.015f * s, -0.01f, 0.04f), V(0.014f, 0.012f, 0.012f), White, blend: 0.006f);
        });
        Stretch(b, V(1.25f, 1.25f, 1.25f));
        return b;
    }

    // ------------------------------------------------------------------ 2775 Somnikoala, from Komala

    /// <summary>
    /// Somnikoala: Komala grown larger, still fast asleep, its log grown into a long bough with leafy twigs at both ends
    /// and a starred nightcap drooping over one ear to a pompom.
    /// </summary>
    private static PokeBuilder Somnikoala()
    {
        var b = Become(Komala(), "Somnikoala", 0.75f);
        int head = BoneOf(b, "head");
        var bark = Rgb(150, 130, 100);
        var cap = Rgb(80, 86, 150);
        var star = Rgb(220, 230, 250);
        Recolour(b, (Rgb(150, 182, 216), Rgb(150, 156, 196)), (Rgb(140, 96, 64), bark));
        var la = V(-0.12f, 0.06f, 0.1f);
        var lb = V(0.1f, 0.17f, 0.11f);
        var d = Vector3.Normalize(lb - la);
        // The bough runs on past both ends, twigs and leaves at each
        b.Limb(Body, la, la - d * 0.1f + V(0, -0.02f, 0), 0.045f, 0.032f, bark);
        b.Limb(Body, lb, lb + d * 0.1f, 0.045f, 0.03f, bark);
        foreach (var (end, way) in new[] { (la - d * 0.1f + V(0, -0.02f, 0), -1f), (lb + d * 0.1f, 1f) })
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * MathF.Tau;
                var dir = Vector3.Normalize(d * way * 0.8f + V(MathF.Cos(a), MathF.Sin(a), 0.3f * MathF.Sin(a * 2f)));
                Frond(b, Body, end, end + dir * 0.07f, 0.02f, i % 2 == 0 ? Rgb(70, 150, 140) : Rgb(140, 100, 180), V(0, 0, 1f), 0.25f, Leaf);
            }
        // The nightcap, drooping over its left ear to a pompom
        var c = V(0, 0.3f, 0.02f);
        var path = Smooth(3, c + V(0.02f, 0.05f, -0.01f), c + V(-0.03f, 0.1f, -0.01f), c + V(-0.09f, 0.08f, 0.01f), c + V(-0.13f, 0.0f, 0.03f), c + V(-0.14f, -0.07f, 0.04f));
        b.Ell(head, c + V(0, 0.04f, -0.01f), V(0.07f, 0.035f, 0.06f), cap, blend: 0.01f);
        b.Tube(head, path, 0.05f, 0.012f, cap, blend: 0f);
        b.Ell(head, path[^1] + V(0, -0.015f, 0), V(0.024f, 0.024f, 0.024f), Rgb(170, 176, 220), blend: 0.006f);
        foreach (int i in new[] { 1, 4, 7, 10 })
            b.PaintEll(head, path[Math.Min(i, path.Length - 1)] + V(0, 0.02f, 0.01f), V(0.008f, 0.008f, 0.008f), star, soft: 0.004f);
        Stretch(b, V(1.2f, 1.2f, 1.2f));
        b.Ground(0f);
        return b;
    }

    // ------------------------------------------------------------------ 2776 Caldertoise, from Turtonator

    /// <summary>
    /// Caldertoise: Turtonator grown greater and darker, its shell grown into a cone of dark rock rising to a crater at
    /// its peak that glows with lava, cracks of lava running down its sides, more spikes round its rim, plates of rock
    /// on its forelegs and a longer horn on its snout.
    /// </summary>
    private static PokeBuilder Caldertoise()
    {
        var b = Become(Turtonator(), "Caldertoise", 1f);
        int head = BoneOf(b, "head");
        var skin = Rgb(130, 60, 50);
        var rock = Rgb(70, 50, 46);
        var lava = Rgb(255, 150, 50);
        Recolour(b, (Rgb(112, 122, 98), skin), (Rgb(226, 74, 70), Rgb(110, 50, 44)), (Rgb(246, 210, 70), Rgb(150, 70, 40)), (Rgb(196, 200, 170), Rgb(214, 140, 80)));
        var bc = V(0, 0.3f, 0);
        var sc = bc + V(0, 0.03f, -0.1f);
        // The cone: rising back from the shell to a crater
        var peak = sc + V(0, 0.12f, -0.14f);
        b.Limb(Body, sc + V(0, 0, -0.04f), peak, 0.17f, 0.085f, rock, Shell, 0.02f);
        foreach (var (x, y) in new[] { (0.05f, 0.03f), (-0.05f, 0.03f), (0f, 0.06f), (0f, -0.05f) })
            b.Spike(Body, peak + V(x, y, -0.05f), peak + V(x * 1.4f, y * 1.4f, -0.1f), 0.022f, rock, 0.6f, Shell);
        b.Cut(Body, peak + V(0, 0.0f, -0.07f), V(0.055f, 0.03f, 0.055f), V(-55f, 0, 0));
        b.Ell(Body, peak + V(0, -0.01f, -0.05f), V(0.05f, 0.02f, 0.05f), lava, V(-55f, 0, 0), Glow, 0.004f);
        // Cracks of lava down the cone
        for (int i = 0; i < 6; i++)
        {
            float a = (i / 5f - 0.5f) * 200f * Degree;
            var side = V(MathF.Sin(a), MathF.Cos(a) * 0.6f, 0);
            b.Limb(Body, peak + side * 0.07f + V(0, 0, -0.01f), sc + side * 0.15f + V(0, 0.02f, -0.06f), 0.006f, 0.005f, lava, Glow, 0.003f);
        }
        // More spikes round the rim
        for (int i = 0; i < 8; i++)
        {
            float a = (i * 45f) * Degree;
            var at = sc + V(MathF.Cos(a) * 0.17f, MathF.Sin(a) * 0.17f, 0.04f);
            b.Spike(Body, at, at + V(MathF.Cos(a) * 0.06f, MathF.Sin(a) * 0.06f + 0.02f, -0.02f), 0.025f, rock, mat: Shell);
        }
        // Plates of rock on the forelegs and a longer horn
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = bc + V(0.12f * s, 0.08f, 0.03f);
            var hand = shoulder + V(0.06f * s, -0.12f, 0.06f);
            b.Spike(arm, Vector3.Lerp(shoulder, hand, 0.5f), Vector3.Lerp(shoulder, hand, 0.5f) + V(0.04f * s, 0.03f, 0.0f), 0.018f, rock, 0.6f, Shell);
        });
        var c = bc + V(0, 0.2f, 0.07f);
        b.Spike(head, c + V(0, 0.0f, 0.06f), c + V(0, 0.06f, 0.14f), 0.018f, Rgb(170, 120, 80), mat: Shell);
        Stretch(b, V(1.12f, 1.12f, 1.12f));
        return b;
    }

    // ------------------------------------------------------------------ 2777 Rollocupine, from Togedemaru

    /// <summary>
    /// Rollocupine: Togedemaru grown larger and rounder, darker grey, a ring of spines banded yellow round its face, more
    /// banded spines over its back, its back spine grown into a tall ringed lightning rod glowing yellow at its bands and
    /// two short tails behind.
    /// </summary>
    private static PokeBuilder Rollocupine()
    {
        var b = Become(Togedemaru(), "Rollocupine", 0.6f);
        var grey = Rgb(130, 136, 150);
        var yellow = Rgb(250, 230, 70);
        Recolour(b, (Rgb(176, 180, 186), grey), (Rgb(246, 246, 244), Rgb(190, 196, 206)), (Rgb(150, 126, 100), grey));
        var c = V(0, 0.135f, 0);
        var r = V(0.12f, 0.115f, 0.11f);
        // The smile painted, which the coarser mesh of a larger sculpt can't hold as a mark
        b.Model.Decals.RemoveAll(d => !d.IsEye);
        b.PaintEll(Body, On(c, r, 0, c.Y - 0.004f), V(0.012f, 0.004f, 0.01f), Rgb(60, 50, 50), soft: 0.004f);
        // The ring of spines round the face
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * MathF.Tau;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0.15f);
            var root = c + V(0, 0.01f, 0.07f) + d * 0.08f;
            var tip = root + Vector3.Normalize(d) * 0.07f;
            b.Spike(Body, root, tip, 0.018f, grey, mat: Metal);
            b.PaintEll(Body, Vector3.Lerp(root, tip, 0.55f), V(0.017f, 0.006f, 0.017f), yellow, Euler(tip - root), 0.004f);
        }
        // Banded spines over the back
        for (int i = 0; i < 10; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / 10f;
            float ring = MathF.Sqrt(1f - y * y), a = i * 2.4f;
            var n = V(MathF.Sin(a) * ring, y, -MathF.Abs(MathF.Cos(a)) * ring - 0.3f);
            n = Vector3.Normalize(n);
            var root = c + n * r * 0.9f;
            b.Spike(Body, root, root + n * 0.06f, 0.016f, grey, mat: Metal);
            b.PaintEll(Body, root + n * 0.035f, V(0.015f, 0.005f, 0.015f), yellow, Euler(n), 0.004f);
        }
        // The lightning rod, ringed and glowing
        var root2 = c + V(0, 0.07f, -0.07f);
        var top = c + V(0, 0.36f, -0.18f);
        b.Limb(Body, root2, top, 0.03f, 0.01f, grey, Metal, 0.01f);
        for (int i = 1; i < 6; i++)
            b.Torus(Body, Vector3.Lerp(root2, top, 0.2f + 0.13f * i), 0.024f - 0.003f * i, 0.005f, yellow, Euler(top - root2), mat: Glow, blend: 0.002f);
        // Two short tails
        PokeBuilder.Both(s => b.Limb(Body, c + V(0.03f * s, -0.06f, -0.09f), c + V(0.05f * s, -0.08f, -0.17f), 0.016f, 0.012f, grey, Metal));
        Stretch(b, V(1.3f, 1.3f, 1.3f));
        return b;
    }

    // ------------------------------------------------------------------ 2778 Masqueradoll, from Mimikyu

    /// <summary>
    /// Masqueradoll: Mimikyu's costume sewn anew, larger and in pastel lilac, cream, pink and blue patches with stitches
    /// along every seam, a hood over its head and a cream mask with a button eye and a spiral, a dark gap for a mouth,
    /// one ear limp and one striped, a cape of patches trailing to the ground and a shadowy claw reaching from under it.
    /// </summary>
    private static PokeBuilder Masqueradoll()
    {
        var b = Become(Mimikyu(), "Masqueradoll", 0.6f);
        int head = BoneOf(b, "head");
        var lilac = Rgb(200, 186, 226);
        var cream = Rgb(244, 238, 206);
        var pink = Rgb(240, 186, 200);
        var blue = Rgb(170, 196, 230);
        var stitch = Rgb(90, 76, 110);
        var black = Rgb(40, 36, 40);
        Recolour(b, (Rgb(222, 206, 150), lilac), (Rgb(130, 96, 66), lilac));
        // The old scrawled face goes: a mask with a button and a spiral, and a dark gap for a mouth
        b.Model.Decals.RemoveAll(d => !d.IsEye && d.Bone == head);
        var hc = V(0, 0.27f, 0.01f);
        var hr = V(0.085f, 0.074f, 0.072f);
        b.PaintEll(head, hc + V(0, 0.02f, 0.06f), V(0.08f, 0.03f, 0.03f), cream);
        b.PaintEll(head, hc + V(0, -0.025f, 0.065f), V(0.05f, 0.022f, 0.02f), black);
        var left = On(hc, hr, -0.035f, hc.Y + 0.02f);
        b.Mark(head, left, Outward(hc, hr, left), 0.014f, 0.014f, Rgb(150, 160, 190), MarkShape.Disc);
        var right = On(hc, hr, 0.035f, hc.Y + 0.02f);
        b.Mark(head, right, Outward(hc, hr, right), 0.013f, 0.013f, stitch, MarkShape.Ring);
        // Stripes on the standing ear
        foreach (float t in new[] { 0.3f, 0.55f, 0.8f })
            b.PaintEll(head, Vector3.Lerp(hc + V(-0.045f, 0.05f, 0), hc + V(-0.075f, 0.17f, -0.005f), t), V(0.024f, 0.008f, 0.024f), blue, soft: 0.004f);
        // A hood round the head
        b.Torus(head, hc + V(0, -0.01f, 0.02f), 0.08f, 0.012f, lilac, V(80f, 0, 0), blend: 0.01f);
        // The cape of patches trailing to the ground behind
        var colours = new[] { pink, blue, lilac, cream, pink, blue };
        for (int i = 0; i < 6; i++)
        {
            float a = (i / 5f - 0.5f) * 220f * Degree + MathF.PI;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, V(0, 0.19f, 0) + d * 0.06f, V(0, 0.012f, 0) + d * 0.12f, 0.04f, colours[i], d, 0.18f);
        }
        // Stitches down the seams of the body
        foreach (var a in new[] { -0.6f, 0.6f, 2.5f })
            for (int k = 0; k < 4; k++)
            {
                var d = V(MathF.Sin(a), 0, MathF.Cos(a));
                b.PaintEll(Body, V(0, 0.18f - 0.035f * k, 0) + d * (0.05f + 0.016f * k), V(0.006f, 0.002f, 0.006f), stitch, soft: 0.002f);
            }
        // The shadowy claw from under the cape
        foreach (var dx in new[] { -0.02f, 0f, 0.02f })
            b.Tube(Body, Smooth(2, V(0.02f, 0.03f, 0.08f), V(0.03f + dx, 0.01f, 0.13f), V(0.04f + dx * 2f, 0.002f, 0.17f)), 0.01f, 0.004f, black, blend: 0.004f);
        Stretch(b, V(1.25f, 1.25f, 1.25f));
        return b;
    }

    // ------------------------------------------------------------------ 2779 Chromagnash, from Bruxish

    /// <summary>
    /// Chromagnash: Bruxish grown longer, deep pink swirled with blue and gold, its fins grown into tall sails striped
    /// like the rainbow, a crest of spines on its head, a second row of teeth and a long rainbow tail.
    /// </summary>
    private static PokeBuilder Chromagnash()
    {
        var b = Become(Bruxish(), "Chromagnash", 0.95f);
        int tail = BoneOf(b, "tail");
        var pink = Rgb(220, 70, 150);
        var rainbow = new[] { Rgb(236, 60, 60), Rgb(246, 150, 50), Rgb(246, 220, 70), Rgb(80, 190, 90), Rgb(70, 140, 230), Rgb(140, 80, 200) };
        Unlift(b, V(0, 0.25f, 0));
        Recolour(b, (Rgb(236, 104, 160), pink), (Rgb(150, 90, 180), Rgb(140, 80, 200)));
        var c = V(0, 0.25f, 0);
        var r = V(0.07f, 0.12f, 0.17f);
        // A rainbow sail: a fan of fin rays from root to tips, each its colour
        void Sail(int bone, Vector3 root, Vector3 way, Vector3 spread, float length, Vector3 face)
        {
            for (int i = 0; i < rainbow.Length; i++)
            {
                float t = i / (rainbow.Length - 1f) - 0.5f;
                var dir = Vector3.Normalize(way + spread * t);
                Blade(b, bone, root + spread * t * 0.05f, root + dir * length * (1f - 0.15f * MathF.Abs(t)), 0.024f, rainbow[i], face, 0.22f);
            }
        }
        // The dorsal sail, tall and swept back
        Sail(Body, c + V(0, 0.1f, -0.04f), V(0, 1f, -0.5f), V(0, -0.3f, 1.2f), 0.22f, V(1f, 0, 0));
        // Sails at the sides and beneath
        PokeBuilder.Both(s =>
        {
            Sail(Body, c + V(0.05f * s, -0.04f, 0.0f), V(0.6f * s, -0.6f, -0.4f), V(0, 0.4f, 0.8f), 0.13f, V(0, 1f, 0.2f));
            Sail(Body, c + V(0.02f * s, -0.1f, -0.08f), V(0.3f * s, -1f, -0.3f), V(0, 0.2f, 0.8f), 0.12f, V(1f, 0, 0));
        });
        // The rainbow tail
        var end = c + V(0, 0, -r.Z - 0.03f);
        Sail(tail, end, V(0, 0.6f, -1f), V(0, 1.6f, 0.3f), 0.18f, V(1f, 0, 0));
        // A crest of spines on the head and swirls of blue and gold
        foreach (var (z, h) in new[] { (0.12f, 0.06f), (0.09f, 0.08f), (0.06f, 0.06f) })
            b.Spike(Body, c + V(0, 0.1f, z), c + V(0, 0.1f + h, z - 0.03f), 0.016f, pink, 0.5f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, c + V(0.065f * s, 0.03f, -0.05f), V(0.02f, 0.02f, 0.04f), Rgb(80, 60, 190));
            b.PaintEll(Body, c + V(0.065f * s, -0.02f, -0.09f), V(0.012f, 0.012f, 0.02f), Rgb(246, 210, 80));
        });
        // A second row of teeth
        var mouth = c + V(0, -0.03f, r.Z - 0.01f);
        foreach (float x in new[] { -0.028f, -0.014f, 0f, 0.014f, 0.028f })
            b.Spike(Body, mouth + V(x, 0.005f, 0.018f), mouth + V(x, -0.012f, 0.024f), 0.005f, White, mat: Shell, blend: 0.002f);
        Stretch(b, V(1.05f, 1.05f, 1.2f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ 2780 Elderdrake, from Drampa

    /// <summary>
    /// Elderdrake: Drampa grown longer and paler, cream rather than green, its beard grown into a cloud reaching down its
    /// chest, its cloud a mane running from its head down its back to the tail's end, two great wings of cloud rising
    /// from its shoulders and a crown of four branching horns.
    /// </summary>
    private static PokeBuilder Elderdrake()
    {
        var b = Become(Drampa(), "Elderdrake", 1f);
        int head = BoneOf(b, "head");
        var cream = Rgb(240, 232, 206);
        var cloud = Rgb(244, 244, 248);
        var horn = Rgb(190, 160, 100);
        Recolour(b, (Rgb(110, 184, 160), cream), (Rgb(248, 220, 110), cloud));
        var c = V(0, 0.43f, 0.38f);
        // Its pink cheeks go under the beard's cloud
        b.Model.Decals.RemoveAll(d => d.Bone == head && !d.IsEye);
        // A crown of four branching horns
        PokeBuilder.Both(s =>
        {
            foreach (var (x, lean) in new[] { (0.02f, 0.3f), (0.045f, 0.8f) })
            {
                var root = c + V(x * s, 0.04f, -0.03f);
                var tip = root + V(0.05f * lean * s, 0.14f, -0.06f);
                b.Limb(head, root, tip, 0.011f, 0.005f, horn, Shell, 0.004f);
                b.Limb(head, Vector3.Lerp(root, tip, 0.5f), Vector3.Lerp(root, tip, 0.5f) + V(0.04f * s, 0.04f, 0.0f), 0.006f, 0.003f, horn, Shell, 0.003f);
            }
            // Whiskers
            b.Tube(head, Smooth(3, c + V(0.03f * s, -0.02f, 0.07f), c + V(0.09f * s, -0.04f, 0.06f), c + V(0.13f * s, -0.12f, 0.02f)), 0.003f, 0.002f, horn, blend: 0.002f);
        });
        // The beard grown into a cloud down its chest
        foreach (var (y, z, k) in new[] { (-0.08f, 0.0f, 0.06f), (-0.14f, -0.02f, 0.065f), (-0.2f, -0.05f, 0.06f) })
            b.Ell(head, c + V(0, y, z), V(k, k * 0.85f, k), cloud, blend: 0.02f);
        // Two wings of cloud rising from the shoulders
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.06f * s, 0.3f, 0.0f));
            var path = Smooth(2, V(0.06f * s, 0.3f, 0.0f), V(0.14f * s, 0.5f, -0.06f), V(0.24f * s, 0.62f, -0.14f), V(0.32f * s, 0.66f, -0.24f));
            for (int i = 0; i < path.Length; i++)
            {
                float k = 0.06f - 0.004f * i;
                b.Ell(wing, path[i], V(k, k * 0.9f, k), cloud, blend: 0.02f);
                b.Ell(wing, path[i] + V(0.02f * s, -0.05f, -0.02f), V(k * 0.8f, k * 0.7f, k * 0.8f), Rgb(226, 230, 240), blend: 0.02f);
            }
        });
        Stretch(b, V(1f, 1.05f, 1.15f));
        return b;
    }

    // ------------------------------------------------------------------ 2781 Galleonghast, from Dhelmise

    /// <summary>
    /// Galleonghast: Dhelmise grown into a ghost ship's wreck, a second anchor chained crosswise to the first, the wheel
    /// larger and lit green at its hub, its seaweed grown into a ragged sail of kelp hung from two spars, barnacles over
    /// the anchors' flukes and chains looping below.
    /// </summary>
    private static PokeBuilder Galleonghast()
    {
        var b = Become(Dhelmise(), "Galleonghast", 1f);
        var rust = Rgb(140, 96, 72);
        var kelp = Rgb(70, 110, 60);
        var chain = Rgb(130, 130, 130);
        var glow = Rgb(140, 250, 200);
        var barnacle = Rgb(224, 220, 206);
        var spar = Rgb(120, 96, 70);
        Unlift(b, V(0, 0.4f, 0));
        Recolour(b, (Rgb(40, 104, 110), Rgb(56, 100, 70)), (Rgb(150, 92, 60), rust));
        // The wheel larger
        var wc = V(0, 0.64f, -0.05f);
        Grow(b, V(1.2f, 1.2f, 1.2f), wc, s => s.Bone == Body && s.A.Z < -0.04f && Vector3.Distance(s.A, wc) < 0.22f, _ => false);
        b.Ell(Body, wc + V(0, 0, 0.02f), V(0.03f, 0.03f, 0.015f), glow, mat: Glow, blend: 0.006f);
        // A second anchor, crosswise behind the first
        var a0 = V(-0.2f, 0.66f, -0.025f);
        var a1 = V(0.24f, 0.2f, -0.025f);
        b.Limb(Body, a0, a1, 0.022f, 0.02f, rust, Metal, 0.006f);
        b.Torus(Body, a0 - Vector3.Normalize(a1 - a0) * 0.03f, 0.03f, 0.008f, rust, V(90f, 0, 45f), mat: Metal, blend: 0.004f);
        var dn = Vector3.Normalize(a1 - a0);
        var across = Vector3.Normalize(Vector3.Cross(dn, V(0, 0, 1f)));
        foreach (float sgn in new[] { -1f, 1f })
        {
            var tip = a1 + across * 0.1f * sgn - dn * 0.06f;
            b.Limb(Body, a1, tip, 0.018f, 0.014f, rust, Metal, 0.004f);
            b.Spike(Body, tip, tip - dn * 0.05f + across * 0.02f * sgn, 0.024f, rust, 0.4f, Metal);
        }
        // Barnacles on the flukes
        foreach (float sgn in new[] { -1f, 1f })
            b.Ell(Body, a1 + across * 0.05f * sgn - dn * 0.03f, V(0.014f, 0.012f, 0.014f), barnacle, mat: Shell, blend: 0.006f);
        // The sail of kelp hung between two spars at its side
        var top0 = V(0.06f, 0.82f, 0.06f);
        var top1 = V(0.42f, 0.88f, 0.04f);
        var bot0 = V(0.1f, 0.3f, 0.06f);
        var bot1 = V(0.44f, 0.36f, 0.04f);
        b.Limb(Body, top0, top1, 0.008f, 0.008f, spar, blend: 0.004f);
        b.Limb(Body, bot0, bot1, 0.008f, 0.008f, spar, blend: 0.004f);
        b.Limb(Body, V(0.02f, 0.62f, 0.05f), top0, 0.008f, 0.008f, spar, blend: 0.004f);
        for (int i = 0; i < 7; i++)
        {
            float t = i / 6f;
            var from = Vector3.Lerp(top0, top1, t);
            var to = Vector3.Lerp(bot0, bot1, t) + V(0, -0.04f * (i % 2), 0);
            Ribbon(b, Body, Smooth(2, from, Vector3.Lerp(from, to, 0.5f) + V(0.012f * MathF.Sin(i * 1.7f), 0, 0.01f), to), 0.03f, i % 2 == 0 ? kelp : Rgb(90, 120, 60), V(0, 0, 1f), 0.008f);
        }
        // A chain looping below
        var loop = Smooth(4, V(0, 0.3f, 0.0f), V(-0.04f, 0.14f, 0.04f), V(0.1f, 0.12f, 0.04f), a1 + V(-0.04f, 0.04f, 0));
        b.Tube(Body, loop, 0.008f, 0.008f, chain, Metal, 0.004f);
        for (int i = 1; i < loop.Length; i += 2)
            b.Ell(Body, loop[i], V(0.012f, 0.012f, 0.012f), chain, mat: Metal, blend: 0.006f);
        Stretch(b, V(1.05f, 1.05f, 1.05f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ the four guardians' heirs

    /// <summary>The ring of feathers round a guardian's shoulders, one colour for each island, fanned out to both sides and behind.</summary>
    private static void IslandFeathers(PokeBuilder b, int bone, Vector3 neck, float length)
    {
        var colours = new[] { Rgb(240, 200, 60), Rgb(236, 110, 150), Rgb(80, 170, 100), Rgb(90, 120, 230) };
        b.Torus(bone, neck, 0.04f, 0.007f, Rgb(214, 170, 80), mat: Metal, blend: 0.004f);
        for (int i = 0; i < 8; i++)
        {
            float a = (i / 7f - 0.5f) * 220f * Degree + MathF.PI;
            var d = Vector3.Normalize(V(MathF.Sin(a) * 1.4f, 0.5f, MathF.Cos(a) * 0.5f));
            var root = neck + V(MathF.Sin(a) * 0.035f, 0, MathF.Cos(a) * 0.03f);
            Frond(b, bone, root, root + d * length * 1.5f, 0.028f, colours[i % 4], V(0, 0.2f, 1f), 0.2f);
        }
    }

    /// <summary>A totem's mask: a tall plate facing out at <paramref name="at"/>, two glowing eyes and a band painted on it.</summary>
    private static void TotemPlate(PokeBuilder b, int bone, Vector3 at, float s, Color plate, Color rim, Color eye, float k = 1f)
    {
        var face = V(s, 0, 0.6f);
        b.Ell(bone, at, V(0.03f, 0.13f, 0.08f) * k, plate, V(0, 30f * s, 0), Shell, 0.008f);
        b.PaintEll(bone, at + V(0, 0.09f * k, 0), V(0.06f, 0.02f, 0.1f) * k, rim, soft: 0.006f);
        b.PaintEll(bone, at + V(0, -0.08f * k, 0), V(0.06f, 0.02f, 0.1f) * k, rim, soft: 0.006f);
        foreach (float dz in new[] { -0.03f, 0.03f })
            b.Ell(bone, at + V(0.025f * s * k, 0.025f * k, dz * k), V(0.008f, 0.01f, 0.014f) * k, eye, V(0, 30f * s, 0), Glow, 0.003f);
        _ = face;
    }

    /// <summary>
    /// Kokonui: Tapu Koko grown larger, its crest grown tall and lit gold like a flame, its two halves of shell grown into
    /// totem masks with glowing eyes, held out at its sides, and a ring of four feathers round its shoulders, one for
    /// each island.
    /// </summary>
    private static PokeBuilder Kokonui()
    {
        var b = Become(TapuKoko(), "Kokonui", 0.95f);
        int head = BoneOf(b, "head");
        var gold = Rgb(250, 210, 60);
        Unlift(b, V(0, 0.4f, 0));
        Make(b, s => s.Bone == head && Is(s.Color, Rgb(244, 140, 50)), Glow);
        Recolour(b, (Rgb(244, 140, 50), gold), (Rgb(196, 196, 72), Rgb(214, 170, 60)));
        var bc = V(0, 0.4f, 0.02f);
        var c = bc + V(0, 0.11f, 0.01f);
        // The crest taller
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 0.015f;
            FlameTongue(b, head, c + V(x, 0.04f, -0.01f), c + V(x * 3.5f, 0.24f - MathF.Abs(x) * 2f, -0.08f), 0.024f, gold, Rgb(255, 250, 200));
        }
        PokeBuilder.Both(s => TotemPlate(b, BoneOf(b, s < 0 ? "armL" : "armR"), bc + V(0.205f * s, 0.02f, 0.01f), s, Rgb(214, 170, 60), Rgb(60, 60, 70), Rgb(100, 230, 240)));
        IslandFeathers(b, Body, bc + V(0, 0.06f, -0.01f), 0.12f);
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return Lift(b);
    }

    /// <summary>
    /// Lelenui: Tapu Lele grown larger, its bud of shell grown into two tall masks of rose marked gold standing at its
    /// sides, a skirt of glowing pink scales round the figure, curling pink antennae and the ring of four feathers round
    /// its shoulders.
    /// </summary>
    private static PokeBuilder Lelenui()
    {
        var b = Become(TapuLele(), "Lelenui", 0.95f);
        int head = BoneOf(b, "head");
        var rose = Rgb(190, 80, 120);
        var pink = Rgb(250, 170, 220);
        var gold = Rgb(230, 190, 80);
        Unlift(b, V(0, 0.3f, 0));
        var bc = V(0, 0.33f, 0.02f);
        var c = bc + V(0, 0.11f, 0.01f);
        // The two masks at the sides, rising from the bud
        PokeBuilder.Both(s =>
        {
            b.Limb(Body, V(0.1f * s, 0.18f, 0), V(0.17f * s, 0.24f, -0.01f), 0.03f, 0.03f, Rgb(240, 150, 190), Shell, 0.01f);
            TotemPlate(b, Body, V(0.2f * s, 0.32f, 0.0f), s, rose, gold, pink, 1.15f);
        });
        // A skirt of glowing pink scales
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * MathF.Tau;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Ell(Body, bc + V(0, -0.06f, 0) + d * 0.045f, V(0.022f, 0.014f, 0.022f), pink, mat: Glow, blend: 0.008f);
        }
        // Curling antennae
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.015f * s, 0.12f, -0.03f), c + V(0.04f * s, 0.18f, -0.03f), c + V(0.07f * s, 0.19f, -0.02f), c + V(0.06f * s, 0.16f, -0.02f)), 0.006f, 0.004f, Rgb(240, 150, 190), blend: 0.003f));
        IslandFeathers(b, Body, bc + V(0, 0.06f, -0.01f), 0.11f);
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return Lift(b);
    }

    /// <summary>
    /// Bulunui: Tapu Bulu grown into a great bull of the forest, its red plate split into two halves of a totem mask, its
    /// horns grown long and sweeping out with vines hanging from them, moss on its shoulders and arms of bark, and the
    /// ring of four feathers round its shoulders.
    /// </summary>
    private static PokeBuilder Bulunui()
    {
        var b = Become(TapuBulu(), "Bulunui", 1f);
        int head = BoneOf(b, "head");
        var horn = Rgb(110, 84, 60);
        var vine = Rgb(80, 140, 60);
        var moss = Rgb(96, 130, 64);
        Unlift(b, V(0, 0.38f, 0));
        var bc = V(0, 0.38f, 0.02f);
        var c = bc + V(0, 0.11f, 0.01f);
        // The long horns, sweeping out and up, vines hanging from them
        PokeBuilder.Both(s =>
        {
            var path = Smooth(3, c + V(0.08f * s, 0.1f, -0.02f), c + V(0.2f * s, 0.12f, -0.02f), c + V(0.32f * s, 0.14f, -0.02f), c + V(0.38f * s, 0.26f, -0.02f));
            b.Tube(head, path, 0.02f, 0.006f, horn, Shell, 0f);
            foreach (int i in new[] { 2, 5, 8 })
            {
                var at = path[Math.Min(i, path.Length - 1)];
                b.Tube(head, Smooth(2, at, at + V(0.01f * s, -0.06f, 0.01f), at + V(-0.005f * s, -0.12f, 0.0f)), 0.004f, 0.003f, vine, blend: 0.002f);
            }
            // Moss on the shoulders
            b.Ell(Body, bc + V(0.05f * s, 0.06f, 0.0f), V(0.03f, 0.02f, 0.03f), moss, mat: Leaf, blend: 0.01f);
        });
        // The split in the mask
        b.PaintEll(head, c + V(0, 0.08f, 0.07f), V(0.006f, 0.05f, 0.03f), Rgb(60, 30, 30), soft: 0.004f);
        IslandFeathers(b, Body, bc + V(0, 0.06f, -0.01f), 0.13f);
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return Lift(b);
    }

    /// <summary>
    /// Fininui: Tapu Fini grown larger, its crescent of shell split into two great masks carved with faces standing at
    /// its sides, ribbons of mist winding round it, a flowing tail of fins below and the ring of four feathers round its
    /// shoulders.
    /// </summary>
    private static PokeBuilder Fininui()
    {
        var b = Become(TapuFini(), "Fininui", 0.95f);
        var purple = Rgb(110, 84, 160);
        var teal = Rgb(90, 200, 210);
        var mist = Rgb(200, 236, 250);
        Unlift(b, V(0, 0.38f, 0));
        var bc = V(0, 0.36f, 0.03f);
        PokeBuilder.Both(s =>
        {
            b.Limb(Body, bc + V(0.03f * s, -0.04f, -0.04f), V(0.18f * s, 0.26f, -0.02f), 0.02f, 0.02f, purple, Shell, 0.01f);
            TotemPlate(b, Body, V(0.2f * s, 0.28f, 0.0f), s, purple, teal, Rgb(250, 220, 120), 1.2f);
        });
        // Ribbons of mist winding round it
        var ring = Enumerable.Range(0, 13).Select(i =>
        {
            float a = i / 12f * MathF.Tau * 1.2f;
            return bc + V(MathF.Sin(a) * 0.14f, -0.12f + 0.012f * i, MathF.Cos(a) * 0.12f);
        }).ToArray();
        b.Tube(Body, Smooth(2, new[] { bc + V(0, -0.12f, 0.01f) }.Concat(ring).ToArray()), 0.01f, 0.006f, mist, Glow, 0.004f);
        IslandFeathers(b, Body, bc + V(0, 0.06f, -0.01f), 0.12f);
        Stretch(b, V(1.15f, 1.15f, 1.15f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ the Ultra Beasts' heirs (the supplement)

    /// <summary>
    /// Nihiliad: Nihilego grown taller, its bell folded into a crown of three lobes of glass shot blue and teal, rimmed
    /// with dark crystal, a violet glow inside them, and eight tentacles where it had four, studded with violet beads and
    /// ending in clusters of crystal.
    /// </summary>
    private static PokeBuilder Nihiliad()
    {
        var b = Become(Nihilego(), "Nihiliad", 0.95f);
        var glass = Rgb(110, 150, 230);
        var teal = Rgb(110, 220, 220);
        var violet = Rgb(200, 120, 250);
        var tentacle = Rgb(90, 70, 140);
        var crystal = Rgb(120, 110, 160);
        Unlift(b, V(0, 0.45f, 0));
        Recolour(b, (Rgb(236, 238, 246), tentacle), (Rgb(186, 172, 222), Rgb(140, 100, 200)), (Rgb(110, 186, 240), crystal));
        var c = V(0, 0.55f, 0);
        b.Model.Decals.Clear();
        // The crown: three lobes of glass round the bell, a glow within each
        for (int i = 0; i < 3; i++)
        {
            float a = (i * 120f + 90f) * Degree;
            var d = V(MathF.Cos(a), 0, MathF.Sin(a));
            var lc = c + d * 0.07f + V(0, 0.06f, 0);
            b.Ell(Body, lc, V(0.08f, 0.12f, 0.08f), i == 1 ? teal : glass, mat: Glow, blend: 0.02f);
            b.Ell(Body, lc + d * 0.03f, V(0.035f, 0.04f, 0.035f), violet, mat: Glow, blend: 0.01f);
            for (int k = 0; k < 4; k++)
                b.Spike(Body, lc + d * 0.075f + V(0, 0.09f - 0.05f * k, 0), lc + d * 0.1f + V(0, 0.11f - 0.05f * k, 0), 0.012f, crystal, 0.6f, Shell);
        }
        // Eight tentacles, beaded, ending in crystal
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) / 8f * MathF.Tau;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var path = Smooth(3, c + V(0, -0.08f, 0) + d * 0.04f, V(0, 0.35f, 0) + d * 0.14f, V(0, 0.18f, 0) + d * 0.22f, V(0, 0.04f, 0) + d * 0.2f);
            b.Tube(Body, path, 0.022f, 0.014f, tentacle, blend: 0f);
            foreach (int k in new[] { 2, 5 })
                b.Ell(Body, path[k] + V(0, 0.016f, 0) + d * 0.01f, V(0.008f, 0.008f, 0.008f), violet, mat: Glow, blend: 0.003f);
            b.Spike(Body, path[^1] + V(0, 0.03f, 0), path[^1] + V(0, -0.03f, 0) + d * 0.01f, 0.02f, crystal, 0.6f, Glow);
        }
        Stretch(b, V(1f, 1.1f, 1f));
        return Lift(b);
    }

    /// <summary>
    /// Buzzcolossus: Buzzwole grown into a colossus, darker red, its arms grown down to the ground like a gorilla's with
    /// spikes along the forearms and clawed fists, its chest a plate of orange, its proboscis longer and banded and a
    /// second pair of clear wings above the first.
    /// </summary>
    private static PokeBuilder Buzzcolossus()
    {
        var b = Become(Buzzwole(), "Buzzcolossus", 1f);
        int head = BoneOf(b, "head");
        var red = Rgb(170, 44, 40);
        var orange = Rgb(220, 120, 60);
        var black = Rgb(40, 30, 34);
        var wing = Rgb(240, 236, 236);
        Recolour(b, (Rgb(214, 62, 50), red), (Rgb(246, 140, 64), orange), (Rgb(250, 170, 100), wing));
        var bc = V(0, 0.48f, 0);
        // The chest plate
        b.Ell(Body, bc + V(0, -0.01f, 0.06f), V(0.075f, 0.1f, 0.04f), orange, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = bc + V(0.12f * s, 0.08f, 0);
            var elbow = shoulder + V(0.12f * s, -0.18f, 0.06f);
            var fist = V(0.26f * s, 0.06f, 0.08f);
            b.Ell(arm, elbow, V(0.06f, 0.07f, 0.06f), red);
            b.Limb(arm, elbow, fist, 0.055f, 0.06f, red);
            b.Ell(arm, fist, V(0.065f, 0.055f, 0.065f), red);
            Claws(b, arm, fist + V(0, -0.03f, 0.05f), V(0.025f, 0, 0), V(0, -0.6f, 1f), 0.03f, 0.01f);
            foreach (float t in new[] { 0.3f, 0.55f, 0.8f })
            {
                var at = Vector3.Lerp(elbow, fist, t) + V(0.05f * s, 0, -0.02f);
                b.Spike(arm, at, at + V(0.04f * s, 0.03f, -0.02f), 0.016f, Rgb(120, 30, 30), mat: Shell);
            }
            // The second pair of wings
            Frond(b, Body, bc + V(0.03f * s, 0.1f, -0.06f), bc + V(0.24f * s, 0.24f, -0.14f), 0.055f, wing, V(0, 0.3f, 1f), 0.15f, Glow);
        });
        // A longer, banded proboscis
        var c = bc + V(0, 0.17f, 0.04f);
        var root = c + V(-0.095f, 0.21f, 0.076f);
        var tip = c + V(-0.16f, 0.36f, 0.12f);
        b.Limb(head, root, tip, 0.006f, 0.004f, White, Shell, 0.003f);
        Stretch(b, V(1.1f, 1.1f, 1.1f));
        return b;
    }

    /// <summary>
    /// Pheromuse: Pheromosa grown taller, a shell of pale gold over its back from the shoulders, a long veil of white
    /// down trailing from under it to the ground behind like a train, its antennae longer and curling, and gold guards
    /// on its forearms.
    /// </summary>
    private static PokeBuilder Pheromuse()
    {
        var b = Become(Pheromosa(), "Pheromuse", 0.95f);
        int head = BoneOf(b, "head");
        var gold = Rgb(226, 206, 140);
        var down = Rgb(248, 246, 240);
        // The shell over the back
        b.Ell(Body, V(0, 0.63f, -0.03f), V(0.05f, 0.06f, 0.035f), gold, V(-10f, 0, 0), Shell, 0.012f);
        foreach (var y in new[] { 0.66f, 0.62f, 0.58f })
            b.PaintTorus(Body, V(0, y, -0.03f), 0.045f, 0.003f, Rgb(190, 166, 100), V(-10f, 0, 0), 1f, 0.7f);
        // The veil of down from under the shell to the ground behind
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 0.025f;
            var path = Smooth(3, V(x * 0.6f, 0.62f, -0.06f), V(x, 0.42f, -0.12f), V(x * 1.4f, 0.18f, -0.18f), V(x * 1.6f, 0.02f, -0.26f));
            Ribbon(b, Body, path, 0.035f, down, V(0, 0.1f, -1f), 0.012f);
        }
        // Gold guards on the forearms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var elbow = V(0.065f * s, 0.58f, 0.02f);
            var hand = V(0.055f * s, 0.5f, 0.05f);
            Blade(b, arm, elbow, Vector3.Lerp(elbow, hand, 0.9f), 0.014f, gold, V(s, 0, 0), 0.4f, Shell);
            // Antennae run on and curl
            var c = V(0, 0.745f, 0.01f);
            b.Tube(head, Smooth(3, c + V(0.012f * s, 0.04f, -0.01f), c + V(0.03f * s, 0.15f, -0.03f), c + V(0.1f * s, 0.2f, -0.05f), c + V(0.14f * s, 0.15f, -0.06f), c + V(0.12f * s, 0.12f, -0.05f)), 0.006f, 0.004f, down, blend: 0.003f);
        });
        Stretch(b, V(1f, 1.12f, 1f));
        return b;
    }

    /// <summary>
    /// Xurkigrove: Xurkitree grown into a tree of cables, three trunks twisted together, its arms and its head of spikes
    /// grown into branches of wire lit white at their tips, roots of cable spreading round its feet to plugs.
    /// </summary>
    private static PokeBuilder Xurkigrove()
    {
        var b = Become(Xurkitree(), "Xurkigrove", 1f);
        int head = BoneOf(b, "head");
        var black = Rgb(40, 40, 46);
        var light = Rgb(255, 250, 210);
        var copper = Rgb(200, 120, 70);
        // A third trunk twisted round the two
        var twist = Enumerable.Range(0, 10).Select(i => V(MathF.Sin(i * 0.9f) * 0.04f, 0.44f + 0.035f * i, MathF.Cos(i * 0.9f) * 0.04f)).ToArray();
        b.Tube(Body, Smooth(2, twist), 0.024f, 0.022f, Rgb(60, 60, 68), blend: 0f);
        // Branches of wire from the head, lit at their tips
        var c = V(0, 0.86f, 0);
        for (int i = 0; i < 9; i++)
        {
            float a = (i / 8f - 0.5f) * 200f * Degree;
            var d = V(MathF.Sin(a), MathF.Cos(a) * 0.8f + 0.3f, (i % 3 - 1) * 0.3f);
            var mid = c + d * 0.1f;
            var tip = c + d * 0.2f + V(0, 0.04f, 0);
            b.Tube(head, Smooth(2, c, mid + V(0, 0.02f, 0), tip), 0.01f, 0.005f, black, blend: 0.003f);
            b.Ell(head, tip, V(0.012f, 0.012f, 0.012f), light, mat: Glow, blend: 0.003f);
        }
        // Lights at the arms' wires
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var hand = V(0.26f * s, 0.62f, 0.04f);
            for (int i = 0; i < 5; i += 2)
            {
                float a = (i * 30f - 60f) * Degree;
                b.Ell(arm, hand + Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), 0.2f)) * 0.08f, V(0.009f, 0.009f, 0.009f), light, mat: Glow, blend: 0.003f);
            }
            // Roots of cable spreading round the foot
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            foreach (var (dx, dz) in new[] { (0.12f, 0.06f), (0.1f, -0.1f), (0.02f, 0.14f) })
            {
                var path = Smooth(2, V(0.06f * s, 0.04f, 0.0f), V((0.06f + dx * 0.5f) * s, 0.015f, dz * 0.5f), V((0.06f + dx) * s, 0.012f, dz));
                b.Tube(leg, path, 0.012f, 0.01f, black, blend: 0f);
                b.Limb(leg, path[^1], path[^1] + V(0.02f * s, 0, dz * 0.1f), 0.006f, 0.006f, copper, Metal, 0.002f);
            }
        });
        Stretch(b, V(1f, 1.1f, 1f));
        return b;
    }

    /// <summary>
    /// Celestrata: Celesteela grown taller, a ring of white panels round its waist like a station's, held on struts, a
    /// third stage on each arm, a tall collar round its head and blue flames from its nozzles and arms.
    /// </summary>
    private static PokeBuilder Celestrata()
    {
        var b = Become(Celesteela(), "Celestrata", 1f);
        int head = BoneOf(b, "head");
        var green = Rgb(140, 182, 162);
        var pale = Rgb(214, 228, 220);
        var white = Rgb(236, 238, 236);
        var blue = Rgb(120, 190, 255);
        var gold = Rgb(232, 214, 120);
        // The ring of panels round the waist
        b.Torus(Body, V(0, 0.42f, 0), 0.2f, 0.02f, white, sz: 1f, mat: Metal, blend: 0.004f);
        for (int i = 0; i < 4; i++)
        {
            float a = (i * 90f + 45f) * Degree;
            b.Limb(Body, V(MathF.Sin(a) * 0.07f, 0.42f, MathF.Cos(a) * 0.07f), V(MathF.Sin(a) * 0.19f, 0.42f, MathF.Cos(a) * 0.19f), 0.008f, 0.008f, pale, Metal, 0.003f);
        }
        for (int i = 0; i < 12; i++)
        {
            float a = i * 30f * Degree;
            b.PaintEll(Body, V(MathF.Sin(a) * 0.2f, 0.44f, MathF.Cos(a) * 0.2f), V(0.006f, 0.03f, 0.006f), Rgb(150, 160, 160), soft: 0.004f);
        }
        // The collar round the head
        var c = V(0, 0.71f, 0.02f);
        b.Limb(head, c + V(0, -0.05f, -0.01f), c + V(0, 0.06f, -0.03f), 0.05f, 0.075f, green, Metal, 0.006f);
        b.Cut(head, c + V(0, 0.04f, 0.04f), V(0.05f, 0.08f, 0.05f));
        b.Ell(head, c + V(0, 0.005f, 0.03f), V(0.008f, 0.008f, 0.004f), blue, mat: Glow, blend: 0.002f);
        b.Spike(head, c + V(0, 0.1f, -0.02f), c + V(0, 0.18f, -0.02f), 0.01f, gold, mat: Metal);
        // A third stage at each arm's foot, with blue flame
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var bottom = V(0.27f * s, 0.26f, 0.02f);
            var third = bottom + V(0.02f * s, -0.1f, 0.0f);
            b.Limb(arm, bottom + V(0, -0.02f, 0), third, 0.05f, 0.05f, white, Metal, 0.006f);
            b.Torus(arm, third, 0.05f, 0.008f, green, mat: Metal, blend: 0.003f);
            FlameTongue(b, arm, third + V(0, -0.02f, 0), third + V(0, -0.14f, 0), 0.035f, blue, Rgb(230, 246, 255));
        });
        // Blue flame from the body's nozzles
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Degree;
            var at = V(MathF.Sin(a) * 0.08f, 0.02f, MathF.Cos(a) * 0.08f);
            FlameTongue(b, Body, at, at + V(0, -0.06f, 0), 0.02f, blue, Rgb(230, 246, 255));
        }
        Stretch(b, V(1f, 1.1f, 1f));
        b.Ground(0f);
        return b;
    }

    /// <summary>
    /// Origamurai: Kartana folded into a samurai, green and white planes, a crest of two folded horns rising from its
    /// helm, a skirt and plates of folded armour, a second blade on each arm and a long folded scarf zigzagging behind.
    /// </summary>
    private static PokeBuilder Origamurai()
    {
        var b = Become(Kartana(), "Origamurai", 0.85f);
        int head = BoneOf(b, "head");
        var green = Rgb(80, 140, 90);
        var white = Rgb(246, 244, 240);
        var steel = Rgb(200, 204, 214);
        Recolour(b, (Rgb(220, 70, 50), green), (Rgb(236, 180, 60), Rgb(60, 110, 70)));
        var c = V(0, 0.61f, 0);
        // The horns of the helm
        PokeBuilder.Both(s => Blade(b, head, c + V(0.015f * s, 0.03f, 0), c + V(0.07f * s, 0.15f, 0), 0.02f, green, V(0, 0, 1f), 0.25f));
        b.Box(head, c + V(0, 0.01f, -0.005f), V(0.04f, 0.02f, 0.025f), 0.004f, green, V(0, 0, 0));
        // Plates of armour: shoulders and a skirt of folds
        PokeBuilder.Both(s => b.Box(Body, V(0.045f * s, 0.53f, 0), V(0.025f, 0.008f, 0.02f), 0.002f, green, V(0, 0, -25f * s)));
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * 0.022f;
            Blade(b, Body, V(x, 0.47f, 0.012f), V(x * 1.3f, 0.38f, 0.02f), 0.014f, i % 2 == 0 ? green : white, V(0, 0, 1f), 0.3f);
        }
        // A second blade on each arm
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = V(0.03f * s, 0.52f, 0);
            var from = Vector3.Lerp(shoulder, V(0.36f * s, 0.44f, 0.02f), 0.4f);
            b.Box(arm, (from + from + V(0.0f, -0.22f, 0.02f)) / 2f, V(0.008f, 0.11f, 0.004f), 0.002f, steel, V(0, 0, 10f * s), Metal);
        });
        // The folded scarf zigzagging behind
        var path = new[] { V(0, 0.56f, -0.012f), V(-0.06f, 0.6f, -0.015f), V(-0.1f, 0.52f, -0.015f), V(-0.15f, 0.58f, -0.015f), V(-0.19f, 0.48f, -0.015f), V(-0.22f, 0.54f, -0.015f), V(-0.27f, 0.44f, -0.015f) };
        for (int i = 0; i < path.Length - 1; i++)
            Blade(b, Body, path[i], path[i + 1] + (path[i + 1] - path[i]) * 0.2f, 0.016f, i % 2 == 0 ? green : white, V(0, 0, 1f), 0.25f);
        Stretch(b, V(1.2f, 1.2f, 1.2f));
        return b;
    }

    /// <summary>
    /// Devourmaw: Guzzlord grown greater, its mouth grown down into its chest as a second great maw of white fangs over a
    /// red throat, its hands wider jaws, a crest of spines along its back and red scars across its black hide.
    /// </summary>
    private static PokeBuilder Devourmaw()
    {
        var b = Become(Guzzlord(), "Devourmaw", 1f);
        var black = Rgb(40, 38, 56);
        var purple = Rgb(70, 56, 90);
        var red = Rgb(140, 40, 60);
        Recolour(b, (Rgb(50, 50, 62), black), (Rgb(70, 120, 170), purple), (Rgb(240, 200, 60), Rgb(220, 210, 190)));
        b.Model.Decals.RemoveAll(d => !d.IsEye);
        var bc = V(0, 0.3f, 0);
        var br = V(0.24f, 0.2f, 0.18f);
        // The mouth in the chest
        var m = bc + V(0, -0.01f, 0.15f);
        b.Cut(Body, m + V(0, 0, 0.04f), V(0.1f, 0.08f, 0.06f));
        b.Ell(Body, m + V(0, 0, -0.01f), V(0.09f, 0.07f, 0.03f), red, blend: 0.004f);
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3) * 0.026f;
            float top = 0.07f * MathF.Sqrt(1f - (x / 0.1f) * (x / 0.1f));
            b.Spike(Body, m + V(x, top + 0.005f, 0.02f), m + V(x, top - 0.035f, 0.035f), 0.009f, White, mat: Shell, blend: 0.003f);
            b.Spike(Body, m + V(x, -top - 0.005f, 0.02f), m + V(x, -top + 0.035f, 0.035f), 0.009f, White, mat: Shell, blend: 0.003f);
        }
        // A crest of spines along the back
        for (int i = 0; i < 7; i++)
        {
            float a = (i / 6f - 0.5f) * 140f * Degree;
            var n = V(0, MathF.Cos(a), -MathF.Sin(a) - 0.4f);
            var at = Out(bc, br, default, n);
            b.Spike(Body, at - Vector3.Normalize(n) * 0.02f, at + Vector3.Normalize(n) * 0.09f, 0.03f, purple, 0.5f, Shell);
        }
        // Red scars across the hide
        foreach (var (x, y) in new[] { (0.16f, 0.1f), (-0.18f, 0.05f), (0.14f, -0.12f) })
            b.PaintEll(Body, Out(bc, br, default, V(x / 0.24f, y / 0.2f, 0.6f)), V(0.03f, 0.004f, 0.02f), red, V(0, 0, 25f), 0.004f);
        // Wider jaws on the hands
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = bc + V(0.2f * s, 0.1f, 0);
            var jaw = shoulder + V(0.12f * s, 0.18f, 0.06f) + V(0.02f * s, 0.05f, 0.04f);
            Grow(b, V(1.3f, 1.2f, 1.3f), jaw, sh => sh.Bone == arm && Vector3.Distance(sh.A, jaw) < 0.08f, _ => false);
        });
        Stretch(b, V(1.08f, 1.1f, 1.08f));
        return b;
    }

    /// <summary>
    /// Naganapex: Naganadel grown longer, its tail a serpent's curve ending in a lance lit white, a crown of five
    /// stingers on its head where it had three, a second, smaller pair of wings below the first and its body ridged in
    /// magenta.
    /// </summary>
    private static PokeBuilder Naganapex()
    {
        var b = Become(Naganadel(), "Naganapex", 1f);
        int head = BoneOf(b, "head");
        var purple = Rgb(110, 70, 150);
        var magenta = Rgb(190, 70, 140);
        Unlift(b, V(0, 0.35f, 0));
        b.Sdf.RemoveAll(s => s.Bone == Body && Is(s.Color, Rgb(200, 180, 230)));
        var c = V(0, 0.53f, 0.15f);
        // The crown of five stingers
        for (int i = 0; i < 5; i++)
        {
            float a = (i / 4f - 0.5f) * 100f * Degree;
            b.Spike(head, c + V(MathF.Sin(a) * 0.02f, 0.025f, -0.02f), c + V(MathF.Sin(a) * 0.07f, 0.11f + 0.03f * MathF.Cos(a), -0.06f), 0.012f, magenta, 0.5f, Shell);
        }
        // A second pair of wings below the first
        PokeBuilder.Both(s =>
        {
            int wing = b.Part(s < 0 ? "wing2L" : "wing2R", Body, V(0.03f * s, 0.38f, 0.06f), PokeRole.Wing, 0.4f, s);
            var shoulder = V(0.03f * s, 0.38f, 0.06f);
            var wrist = V(0.16f * s, 0.44f, 0.02f);
            DragonWing(b, wing, shoulder, wrist, new[] { V(0.28f * s, 0.36f, 0.0f), V(0.24f * s, 0.28f, 0.0f), V(0.14f * s, 0.26f, 0.03f) }, V(0.03f * s, 0.32f, 0.06f), purple, purple, 0.01f);
        });
        // The tail's serpent curve and the lance at its end
        var ac = V(0, 0.3f, -0.06f);
        var tail = Smooth(3, ac + V(0, 0, -0.12f), ac + V(0.08f, -0.08f, -0.26f), ac + V(0.02f, -0.18f, -0.4f), ac + V(-0.1f, -0.16f, -0.5f), ac + V(-0.2f, -0.06f, -0.52f));
        b.Tube(Body, tail, 0.045f, 0.02f, purple, blend: 0f);
        for (int i = 2; i < tail.Length - 1; i += 2)
            b.Spike(Body, tail[i] + V(0, 0.02f, 0), tail[i] + V(0, 0.05f, 0.02f), 0.012f, magenta, 0.5f, Shell);
        var end = tail[^1];
        b.Spike(Body, end, end + V(-0.16f, 0.06f, 0.0f), 0.025f, Rgb(250, 230, 255), mat: Glow);
        Stretch(b, V(1.05f, 1.05f, 1.1f));
        return Lift(b);
    }

    /// <summary>
    /// Ziggurak: Stakataka grown into a stepped tower, its courses set back tier on tier to a peaked top, bound with
    /// iron bands, a barred gate at its foot, cyan eyes glinting from every tier and its legs banded in iron.
    /// </summary>
    private static PokeBuilder Ziggurak()
    {
        var b = Become(Stakataka(), "Ziggurak", 1f);
        var stone = Rgb(130, 132, 124);
        var iron = Rgb(80, 80, 86);
        var cyan = Rgb(250, 190, 80);
        Recolour(b, (Rgb(120, 120, 126), stone));
        // The glints of the courses and the legs, a little larger, as a taller sculpt is meshed coarser
        foreach (var d in b.Model.Decals.Where(d => !d.IsEye))
            d.Half *= 1.5f;
        // The tiers set back above
        for (int i = 0; i < 3; i++)
        {
            float y = 0.29f + 6 * 0.085f + i * 0.08f;
            float w = 0.13f - 0.035f * i;
            b.Box(Body, V(0, y, 0), V(w, 0.04f, w * 0.85f), 0.008f, stone, blend: 0.002f);
            for (int k = -1; k <= 1; k += 2)
                b.Mark(Body, V(k * w * 0.45f, y, w * 0.85f), V(0, 0, 1f), 0.011f, 0.009f, cyan);
        }
        b.Spike(Body, V(0, 0.29f + 9 * 0.085f - 0.02f, 0), V(0, 0.29f + 9 * 0.085f + 0.08f, 0), 0.04f, Rgb(170, 160, 130), 0.9f, Shell);
        // Iron bands round the courses
        foreach (int i in new[] { 3, 4 })
            b.Box(Body, V(0, 0.29f + i * 0.085f - 0.042f, 0), V(0.155f, 0.008f, 0.135f), 0.002f, iron, mat: Metal, blend: 0.002f);
        // The barred gate at the foot
        b.Box(Body, V(0, 0.29f, 0.135f), V(0.045f, 0.04f, 0.006f), 0.004f, Rgb(40, 40, 46), blend: 0.002f);
        foreach (var x in new[] { -0.025f, 0f, 0.025f })
            b.Limb(Body, V(x, 0.255f, 0.142f), V(x, 0.325f, 0.142f), 0.004f, 0.004f, iron, Metal, 0.001f);
        // Iron bands round the legs
        PokeBuilder.Both(s =>
        {
            foreach (var (z, front) in new[] { (0.07f, true), (-0.07f, false) })
            {
                int leg = BoneOf(b, (front ? "leg" : "hind") + (s < 0 ? "L" : "R"));
                b.Box(leg, V(0.152f * s, 0.083f, z + (front ? 0.02f : -0.02f)), V(0.048f, 0.008f, 0.048f), 0.002f, iron, mat: Metal, blend: 0.002f);
            }
        });
        Stretch(b, V(1f, 1.05f, 1f));
        return b;
    }

    /// <summary>
    /// Pyrocephalon: Blacephalon grown into a tall jester, its head gone into a burst of fireworks at its neck, three
    /// heads of fireworks (pink, orange and violet) juggled over it on trails of sparks, long striped limbs, a black
    /// body and a cape striped black and white.
    /// </summary>
    private static PokeBuilder Pyrocephalon()
    {
        var b = Become(Blacephalon(), "Pyrocephalon", 1f);
        int head = BoneOf(b, "head");
        var black = Rgb(40, 38, 44);
        var white = Rgb(240, 238, 240);
        var spark = Rgb(250, 170, 70);
        b.Sdf.RemoveAll(s => s.Bone == head);
        b.Model.Decals.RemoveAll(d => d.Bone == head);
        Recolour(b, (Rgb(246, 220, 90), black), (Rgb(236, 90, 160), Rgb(70, 66, 74)), (Rgb(80, 180, 230), Rgb(200, 200, 206)));
        var cc = V(0, 0.5f, 0);
        // The burst of fireworks at the neck
        b.Ell(head, cc + V(0, 0.04f, 0), V(0.06f, 0.04f, 0.06f), Rgb(120, 60, 40), blend: 0.01f);
        for (int i = 0; i < 14; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / 14f;
            if (y < -0.2f) continue;
            float ring = MathF.Sqrt(1f - y * y), a = i * 2.4f;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            b.Spike(head, cc + V(0, 0.04f, 0) + n * 0.04f, cc + V(0, 0.04f, 0) + n * 0.09f, 0.012f, i % 3 == 0 ? Rgb(250, 230, 120) : spark, mat: Glow, blend: 0.004f);
        }
        // The three heads juggled overhead, each on a trail of sparks
        var colours = new[] { Rgb(230, 70, 200), Rgb(250, 150, 50), Rgb(170, 130, 240) };
        for (int i = 0; i < 3; i++)
        {
            float a = (i - 1) * 50f * Degree;
            var at = cc + V(MathF.Sin(a) * 0.16f, 0.2f + MathF.Cos(a) * 0.08f, 0);
            b.Tube(head, Smooth(2, cc + V(0, 0.05f, 0), cc + V(MathF.Sin(a) * 0.07f, 0.16f, 0.0f), at), 0.006f, 0.008f, Rgb(250, 200, 140), Glow, 0.004f);
            b.Ell(head, at, V(0.055f, 0.055f, 0.055f), colours[i], mat: Glow, blend: 0.01f);
            b.Mark(head, at + V(0, -0.012f, 0.054f), V(0, 0, 1f), 0.03f, 0.012f, Rgb(255, 240, 160), MarkShape.Smile);
            foreach (float s in new[] { -1f, 1f })
                b.Mark(head, at + V(0.02f * s, 0.016f, 0.051f), V(0.3f * s, 0.2f, 1f), 0.01f, 0.008f, Rgb(255, 240, 160), MarkShape.Triangle);
        }
        // The cape, striped black and white
        for (int i = 0; i < 8; i++)
        {
            float a = (i / 7f - 0.5f) * 200f * Degree + MathF.PI;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            Blade(b, Body, cc + V(0, -0.02f, 0) + d * 0.08f, V(0, 0.04f, 0) + d * 0.2f, 0.045f, i % 2 == 0 ? black : white, d, 0.32f);
        }
        Stretch(b, V(1f, 1.15f, 1f));
        return b;
    }
}
