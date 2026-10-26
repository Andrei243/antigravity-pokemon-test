using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The Megas of our own (plan 06 · R20b): the fifteen final forms of the starters whose lines had no Mega, in National
// Pokédex order. Each is sculpted from its species' own sculpt as the look of docs/plans/R20b-mega-proposals.md and the
// user's reference pictures (docs/art/reference/gemini/megas) say: the species' build is taken whole (renamed,
// stretched, its colours changed, a part taken away) and the Mega's new features are added to it, with the helpers of
// PokemonModels.Ours.Kanto.cs, as the species of our own are built. The species' own sculpts are never touched.
internal static partial class PokemonModels
{
    /// <summary>A Mega of our own with a sculpt of its own, by name in capitals (null for any other).</summary>
    private static PokeBuilder? CreateOurMega(string form) => form switch
    {
        "TYPHLOSION-MEGA" => TyphlosionMega(),
        "TORTERRA-MEGA" => TorterraMega(),
        "INFERNAPE-MEGA" => InfernapeMega(),
        "EMPOLEON-MEGA" => EmpoleonMega(),
        "SERPERIOR-MEGA" => SerperiorMega(),
        "SAMUROTT-MEGA" => SamurottMega(),
        "DECIDUEYE-MEGA" => DecidueyeMega(),
        "INCINEROAR-MEGA" => IncineroarMega(),
        "PRIMARINA-MEGA" => PrimarinaMega(),
        "RILLABOOM-MEGA" => RillaboomMega(),
        "CINDERACE-MEGA" => CinderaceMega(),
        "INTELEON-MEGA" => InteleonMega(),
        "MEOWSCARADA-MEGA" => MeowscaradaMega(),
        "SKELEDIRGE-MEGA" => SkeledirgeMega(),
        "QUAQUAVAL-MEGA" => QuaquavalMega(),
        _ => null
    };

    private static bool SameColour(Color a, Color c) => a.R == c.R && a.G == c.G && a.B == c.B;

    // ------------------------------------------------------------------ Mega Typhlosion

    /// <summary>
    /// Mega Typhlosion: the cream beast hunched lower and broader, the dark blue of its back turned to ash grey cracked
    /// with lava, its collar of flame climbed into a crater's rim of dark rock round its shoulders with fire welling
    /// inside and three tongues of flame standing out of it, and bracers of the same rock on its forearms.
    /// </summary>
    private static PokeBuilder TyphlosionMega()
    {
        var b = Become(TyphlosionBuild(false), "Typhlosion-Mega", 0.95f);
        var ash = Rgb(104, 104, 112);
        var rock = Rgb(58, 54, 60);
        var lava = Rgb(246, 128, 44);
        var rim = Rgb(244, 100, 50);
        var heart = Rgb(255, 222, 120);
        var cream = Rgb(248, 226, 162);
        Recolour(b, (Rgb(48, 86, 106), ash), (cream, Rgb(146, 144, 150)));
        int flame = BoneOf(b, "flame");
        b.Sdf.RemoveAll(s => s.Bone == flame);
        var k = V(1.1f, 0.94f, 1.1f);
        Stretch(b, k);
        // Cream kept on its belly and muzzle only
        b.PaintEll(Body, V(0, 0.36f, 0.1f), V(0.14f, 0.19f, 0.1f), cream);
        int head = BoneOf(b, "head");
        var hc = V(0, 0.71f, 0.07f) * k;
        b.PaintEll(head, hc + V(0, -0.045f, 0.09f), V(0.06f, 0.035f, 0.07f), cream);
        // The crater on its shoulders: a ring of blocks of rock, fire welling inside it
        var crater = V(0, 0.56f, -0.11f);
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f;
            var at = crater + V(MathF.Sin(a) * 0.16f, (i % 3) * 0.01f, MathF.Cos(a) * 0.13f);
            b.Box(Body, at, V(0.034f, 0.052f + 0.01f * ((i * 7) % 3), 0.028f), 0.012f, i % 4 == 1 ? Rgb(76, 70, 76) : rock, V(6f * ((i * 5) % 3 - 1), a / Degree, 0), Shell, 0.01f);
        }
        b.Ell(Body, crater + V(0, 0.02f, 0), V(0.14f, 0.035f, 0.11f), Rgb(250, 160, 60), mat: Glow, blend: 0.01f);
        b.PaintEll(Body, crater + V(0, 0.05f, 0), V(0.08f, 0.03f, 0.07f), heart);
        foreach (var (x, z, h) in new[] { (0f, -0.03f, 0.32f), (-0.07f, 0.01f, 0.24f), (0.07f, 0.0f, 0.26f) })
            FlameTongue(b, flame, crater + V(x, 0.04f, z), crater + V(x * 1.5f, 0.04f + h, z - 0.05f), 0.055f, rim, heart);
        // Lava in thin cracks down its back to its tail
        var bc = V(0, 0.42f, 0) * k;
        var br = V(0.17f, 0.25f, 0.15f) * k;
        foreach (float x0 in new[] { -0.45f, 0.05f, 0.5f })
        {
            var p = Out(bc, br, default, V(x0, 0.45f, -1f));
            for (int j = 0; j < 4; j++)
            {
                var q = Out(bc, br, default, V(x0 + (j % 2 == 0 ? 0.18f : -0.12f), 0.3f - 0.28f * (j + 1), -1f));
                var mid = Vector3.Lerp(p, q, 0.5f);
                var d = q - p;
                b.PaintEll(Body, mid, V(0.009f, d.Length() * 0.55f, 0.02f), lava, V(0, 0, -MathF.Atan2(d.X, -d.Y) / Degree), 0.006f);
                p = q;
            }
        }
        // Bracers of rock on its forearms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = V(0.13f * s, 0.52f, 0.04f) * k;
            var hand = V(0.24f * s, 0.46f, 0.12f) * k;
            var d = hand - shoulder;
            b.Box(arm, Vector3.Lerp(shoulder, hand, 0.62f), V(0.042f, 0.04f, 0.042f), 0.008f, rock, Euler(d), Shell, 0.006f);
            b.Box(arm, Vector3.Lerp(shoulder, hand, 0.8f) + V(0, 0.01f, 0), V(0.036f, 0.026f, 0.04f), 0.006f, Rgb(72, 66, 72), Euler(d) + V(0, 20f * s, 0), Shell, 0.006f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Mega Torterra

    /// <summary>
    /// Mega Torterra: the shell grown into a mountain range of three snowcapped peaks, moss along its rim and over its
    /// slopes, the old tree carried to the back of the shell with two saplings on the slopes, longer tusks of ivory and
    /// a heavier brow plate.
    /// </summary>
    private static PokeBuilder TorterraMega()
    {
        var b = Become(Torterra(), "Torterra-Mega", 0.97f);
        var stone = Rgb(124, 108, 96);
        var snow = Rgb(240, 244, 250);
        var moss = Rgb(58, 110, 52);
        var bark = Rgb(118, 82, 50);
        var leaves = Rgb(70, 150, 76);
        var ivory = Rgb(226, 214, 186);
        // The tree goes to the back of the shell, on a new trunk
        int canopy = BoneOf(b, "canopy");
        b.Sdf.RemoveAll(s => s.Bone == Body && SameColour(s.Color, bark) && s.Shape == SdfShape.RoundCone);
        Move(b, x => x == canopy, V(0.22f, -0.05f, -0.2f));
        b.Limb(Body, V(0.2f, 0.8f, -0.37f), V(0.25f, 1.15f, -0.37f), 0.08f, 0.06f, bark, Shell);
        // The three peaks, capped with snow
        foreach (var (root, tip, r) in new[]
        {
            (V(-0.22f, 0.78f, 0.12f), V(-0.26f, 1.26f, 0.14f), 0.17f),
            (V(0.12f, 0.8f, 0.18f), V(0.14f, 1.12f, 0.22f), 0.13f),
            (V(-0.3f, 0.76f, -0.25f), V(-0.34f, 1.06f, -0.28f), 0.13f)
        })
        {
            b.Spike(Body, root, tip, r, stone, mat: Shell, blend: 0.03f);
            var d = Vector3.Normalize(tip - root);
            b.PaintEll(Body, tip - d * 0.05f, V(r * 0.55f, 0.08f, r * 0.55f), snow, Euler(d), 0.012f);
        }
        // Moss along the shell's rim and over its slopes
        b.PaintTorus(Body, V(0, 0.56f, -0.05f), 0.56f, 0.07f, moss, sz: 1.14f);
        foreach (var (at, size) in new[] { (V(0.3f, 0.82f, 0.25f), 0.12f), (V(-0.08f, 0.86f, -0.1f), 0.14f), (V(0.35f, 0.78f, -0.2f), 0.11f) })
            b.PaintEll(Body, at, V(size, 0.08f, size), moss);
        // Two saplings on the slopes
        foreach (var (x, z) in new[] { (-0.36f, -0.32f), (0.42f, -0.1f) })
        {
            b.Limb(Body, V(x, 0.74f, z), V(x * 1.08f, 0.98f, z), 0.026f, 0.018f, bark, Shell, 0.01f);
            b.Ell(Body, V(x * 1.08f, 1.04f, z), V(0.1f, 0.08f, 0.1f), leaves, mat: Leaf, blend: 0.02f);
            b.Ell(Body, V(x * 1.08f + 0.03f, 1.08f, z - 0.02f), V(0.06f, 0.05f, 0.06f), PixelCanvas.Light1(leaves, 0.15f), mat: Leaf, blend: 0.02f);
        }
        // Longer tusks and a heavier plate over the brow
        int head = BoneOf(b, "head");
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.2f * s, 0.46f, 1.0f), V(0.27f * s, 0.32f, 1.14f), V(0.3f * s, 0.16f, 1.28f), V(0.27f * s, 0.08f, 1.36f)), 0.05f, 0.012f, ivory, Shell, 0.01f));
        b.Ell(head, V(0, 0.72f, 0.84f), V(0.3f, 0.12f, 0.28f), Rgb(128, 90, 56), mat: Shell, blend: 0.01f);
        foreach (float x in new[] { -0.12f, 0f, 0.12f })
            b.Spike(head, V(x, 0.8f, 0.8f), V(x * 1.2f, 0.9f, 0.74f), 0.05f, stone, mat: Shell);
        return b;
    }

    // ------------------------------------------------------------------ Mega Infernape

    /// <summary>
    /// Mega Infernape: taller on its legs, its fur darkened to charcoal striped red, its crown of flame grown into a
    /// mane streaming down its back to a burning tail, its gold markings grown into plates of armour on the shoulders
    /// and knees, and a ring of flame round each wrist like a boxer's wraps set alight.
    /// </summary>
    private static PokeBuilder InfernapeMega()
    {
        var b = Become(Infernape(), "Infernape-Mega", 0.97f);
        var red = Rgb(206, 70, 42);
        var charcoal = Rgb(66, 56, 62);
        var gold = Rgb(236, 190, 64);
        var rim = Rgb(242, 96, 40);
        var heart = Rgb(255, 218, 110);
        Recolour(b, (Rgb(232, 124, 48), red), (Rgb(248, 244, 236), charcoal));
        var k = V(1f, 1.06f, 1f);
        Stretch(b, k);
        int head = BoneOf(b, "head");
        int flame = BoneOf(b, "flame");
        float hy = 1.02f * 1.06f;
        // A tan face under the blue mask, so the eyes read on the dark fur
        b.PaintEll(head, V(0, hy - 0.06f, 0.15f), V(0.12f, 0.05f, 0.09f), Rgb(226, 178, 140));
        // Red flashes down the dark of the chest
        foreach (float y in new[] { 0.62f, 0.72f, 0.82f })
            PokeBuilder.Both(s => b.PaintEll(Body, V(0.1f * s, y, 0.17f), V(0.07f, 0.014f, 0.05f), red, V(0, 0, 25f * s), 0.006f));
        // The mane: tongues of flame from the crown streaming back and down the spine
        // The old crown of fire goes for a mane that streams back from the brow
        b.Sdf.RemoveAll(s => s.Bone == flame);
        foreach (var (x, y, z, up, len) in new[]
        {
            (0f, 0.14f, 0.0f, 0.55f, 0.4f), (-0.07f, 0.12f, -0.01f, 0.45f, 0.36f), (0.07f, 0.12f, -0.01f, 0.45f, 0.36f),
            (-0.13f, 0.06f, -0.04f, 0.2f, 0.32f), (0.13f, 0.06f, -0.04f, 0.2f, 0.32f), (0f, 0.08f, -0.08f, 0.1f, 0.38f),
            (-0.07f, 0.0f, -0.1f, -0.05f, 0.3f), (0.07f, 0.0f, -0.1f, -0.05f, 0.3f)
        })
        {
            var root = V(x, hy + y, z);
            FlameTongue(b, flame, root, root + Vector3.Normalize(V(x * 1.5f, up, -1f)) * len * 1.35f, 0.075f, rim, heart);
        }
        for (int i = 0; i < 5; i++)
        {
            float y = 0.98f - i * 0.1f;
            var root = V(0, y, -0.17f + i * 0.004f);
            FlameTongue(b, Body, root, root + Vector3.Normalize(V(0, -0.15f, -1f)) * (0.22f - i * 0.02f), 0.055f - i * 0.004f, rim, heart);
        }
        int tail = BoneOf(b, "tail");
        var end = V(0.06f, 0.86f * 1.06f, -0.5f);
        FlameTongue(b, tail, end - V(0, 0.03f, 0), end + V(0.05f, 0.24f, -0.06f), 0.06f, rim, heart);
        PokeBuilder.Both(s =>
        {
            // Plates of gold armour on the shoulders and over the knees
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            b.Ell(arm, V(0.25f * s, 0.89f, 0.0f), V(0.15f, 0.1f, 0.13f), gold, V(0, 0, -20f * s), Metal, 0.01f);
            b.Ell(arm, V(0.3f * s, 0.84f, 0.0f), V(0.09f, 0.04f, 0.11f), PixelCanvas.Shadow(gold, 0.15f), V(0, 0, -35f * s), Metal, 0.008f);
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            b.Ell(leg, V(0.215f * s, 0.29f, 0.13f), V(0.07f, 0.085f, 0.045f), gold, mat: Metal, blend: 0.01f);
            // The ring of flame round each wrist
            var elbow = V(0.37f * s, 0.56f * 1.06f, 0.12f);
            var hand = V(0.42f * s, 0.4f * 1.06f, 0.2f);
            var d = hand - elbow;
            var wrist = Vector3.Lerp(elbow, hand, 0.45f);
            b.Torus(arm, wrist, 0.064f, 0.022f, rim, Euler(d), mat: Glow, blend: 0.006f);
            for (int i = 0; i < 4; i++)
            {
                float a = i * MathF.Tau / 4f + 0.4f;
                var side = Vector3.Normalize(Vector3.Cross(d, V(MathF.Cos(a), 0.3f, MathF.Sin(a))));
                var root = wrist + side * 0.07f;
                FlameTongue(b, arm, root, root + side * 0.05f - Vector3.Normalize(d) * 0.06f, 0.022f, rim, heart);
            }
        });
        return b;
    }

    // ------------------------------------------------------------------ Mega Empoleon

    /// <summary>
    /// Mega Empoleon: its trident crest risen into a tall steel crown of three prongs on a gold band, the blades of its
    /// flippers grown long and steel, a cape of navy plumage falling from its shoulders to the ground with a feathered
    /// hem, and gold epaulettes with a fringe where the cape hangs from.
    /// </summary>
    private static PokeBuilder EmpoleonMega()
    {
        var b = Become(Empoleon(), "Empoleon-Mega", 0.97f);
        var steel = Rgb(156, 176, 206);
        var gold = Rgb(246, 204, 64);
        var cape = Rgb(34, 46, 96);
        float hy = 1.1f;
        int head = BoneOf(b, "head");
        // The crown: a gold band and three steel prongs, the middle one tallest
        b.Ell(head, V(0, hy + 0.13f, 0.08f), V(0.1f, 0.03f, 0.07f), gold, mat: Metal, blend: 0.01f);
        b.Spike(head, V(0, hy + 0.14f, 0.09f), V(0, hy + 0.56f, 0.04f), 0.052f, steel, 0.5f, Metal);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.07f * s, hy + 0.13f, 0.08f), V(0.17f * s, hy + 0.44f, 0.05f), 0.042f, steel, 0.5f, Metal);
            b.Spike(head, V(0.06f * s, hy + 0.15f, 0.09f), V(0.1f * s, hy + 0.26f, 0.08f), 0.016f, gold, 0.6f, Metal);
        });
        // Longer blades of steel on its flippers
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            Grow(b, V(1.1f, 1.4f, 1.1f), V(0.26f * s, 0.8f, 0), x => x == wing);
        });
        Recolour(b, (Rgb(110, 170, 226), steel));
        // The cape from the shoulders to the ground, its hem feathered
        for (int i = 0; i < 5; i++)
        {
            float x = -0.2f + 0.1f * i;
            var root = V(x * 0.9f, 0.88f, -0.16f);
            var tip = V(x * 1.7f, 0.05f, -0.34f + MathF.Abs(x) * 0.2f);
            Frond(b, Body, root, tip, 0.13f, cape, V(x * 1.5f, 0.1f, -1f), 0.18f, Fur, 0.02f);
            foreach (float t in new[] { -1f, 0f, 1f })
                Blade(b, Body, tip + V(t * 0.04f, 0.06f, 0.01f), tip + V(t * 0.06f, -0.01f, -0.04f), 0.03f, PixelCanvas.Shadow(cape, 0.15f), V(0, 0.2f, -1f), 0.25f);
        }
        // Gold epaulettes, fringed, where the cape hangs from
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, V(0.24f * s, 0.88f, -0.01f), V(0.1f, 0.045f, 0.1f), gold, V(0, 0, -15f * s), Metal, 0.012f);
            for (int i = 0; i < 5; i++)
            {
                float z = -0.07f + 0.035f * i;
                b.Spike(Body, V(0.31f * s, 0.86f, z), V(0.34f * s, 0.76f, z), 0.012f, gold, mat: Metal, blend: 0.004f);
            }
        });
        return b;
    }

    // ------------------------------------------------------------------ Mega Serperior

    /// <summary>
    /// Mega Serperior: its leaf collar spread into a cobra's hood of leaves framing the face, yellow and green, two
    /// curved horns of leaf sweeping back from its crown, and a pair of leaf-wings standing from the coils behind the
    /// neck.
    /// </summary>
    private static PokeBuilder SerperiorMega()
    {
        var b = Become(Serperior(), "Serperior-Mega", 0.97f);
        var green = Rgb(70, 150, 80);
        var deep = Rgb(46, 112, 60);
        var light = Rgb(110, 184, 90);
        var yellow = Rgb(236, 200, 80);
        int head = BoneOf(b, "head");
        int neck = BoneOf(b, "seg12");
        var c = V(0, 0.55f, 0.09f);
        // The hood: blades of leaf in an arc round the back of the head, flat to the front
        var n = V(0, 0.49f, 0.04f);
        for (int i = 0; i < 11; i++)
        {
            float a = (-115f + 23f * i) * Degree;
            var d = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a) * 0.9f, -0.15f));
            Blade(b, head, n + d * 0.03f, n + d * (0.15f - 0.02f * MathF.Abs(MathF.Sin(a * 0.5f))), 0.04f, i % 2 == 0 ? yellow : light, V(0, 0, 1f), 0.2f, Leaf);
        }
        b.Ell(head, n + V(0, 0, -0.01f), V(0.07f, 0.07f, 0.03f), deep, mat: Leaf, blend: 0.01f);
        // Two horns of leaf sweeping back from the crown
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.025f * s, 0.035f, -0.03f), c + V(0.045f * s, 0.11f, -0.07f), c + V(0.04f * s, 0.17f, -0.15f), c + V(0.03f * s, 0.16f, -0.21f)), 0.017f, 0.005f, deep, Leaf, 0.006f));
        // The leaf-wings behind the neck, each a fan of long leaves flat to the side they face
        PokeBuilder.Both(s =>
        {
            var root = V(0.03f * s, 0.36f, -0.01f);
            var spar = V(0.24f * s, 0.6f, -0.1f);
            b.Limb(neck, root, spar, 0.02f, 0.012f, deep, Leaf, 0.01f);
            for (int i = 0; i < 6; i++)
            {
                float t = 0.25f + 0.15f * i;
                var at = Vector3.Lerp(root, spar, MathF.Min(1f, t));
                var tip = at + V((0.04f + 0.015f * i) * s, -0.08f - 0.02f * i + (i == 5 ? 0.12f : 0f), -0.05f);
                Blade(b, neck, at, tip, 0.03f, i % 2 == 0 ? green : light, V(0, 0, 1f), 0.2f, Leaf);
            }
            Blade(b, neck, spar, spar + V(0.08f * s, 0.06f, -0.03f), 0.03f, green, V(0, 0, 1f), 0.2f, Leaf);
        });
        return b;
    }

    // ------------------------------------------------------------------ Mega Samurott

    /// <summary>
    /// Mega Samurott: the sea lion clad in steel, its shells turned to plates of armour over its shoulders and flanks,
    /// its helmet grown into a kabuto with a tall crescent crest of gold, its moustache longer, and both seamitars half
    /// drawn from their sheaths on the forelegs, longer than the leg.
    /// </summary>
    private static PokeBuilder SamurottMega()
    {
        var b = Become(Samurott(), "Samurott-Mega", 1f);
        var navy = Rgb(64, 84, 122);
        var steel = Rgb(150, 166, 190);
        var gold = Rgb(232, 190, 70);
        var cream = Rgb(236, 226, 190);
        var blade = Rgb(206, 216, 230);
        Recolour(b, (Rgb(70, 110, 180), navy), (Rgb(170, 210, 230), Rgb(150, 172, 200)), (Rgb(230, 214, 150), steel));
        foreach (var s in b.Sdf.Shapes.Where(s => SameColour(s.Color, steel))) s.Material = Metal;
        b.Sdf.Changed();
        int head = BoneOf(b, "head");
        var c = V(0, 0.43f, 0.23f);
        // The kabuto: a neck guard and the crescent crest of gold with a disc at its root
        b.Ell(head, c + V(0, 0.02f, -0.06f), V(0.11f, 0.05f, 0.07f), steel, V(-25f, 0, 0), Metal, 0.01f);
        b.Ell(head, c + V(0, 0.075f, 0.065f), V(0.022f, 0.022f, 0.012f), gold, mat: Metal, blend: 0.006f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.015f * s, 0.075f, 0.065f), c + V(0.1f * s, 0.11f, 0.08f), c + V(0.17f * s, 0.2f, 0.05f), c + V(0.2f * s, 0.31f, -0.01f)), 0.013f, 0.004f, gold, Metal, 0.004f));
        // A longer moustache
        PokeBuilder.Both(s => Ribbon(b, head, Smooth(2, c + V(0.03f * s, -0.03f, 0.08f), c + V(0.12f * s, -0.06f, 0.06f), c + V(0.21f * s, -0.04f, -0.02f), c + V(0.28f * s, 0.04f, -0.08f)), 0.016f, cream, V(0, 1f, 0.2f), 0.007f));
        // Plates of armour over the shoulders and flanks, edged cream
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
            {
                var at = V(0.1f * s, 0.37f - 0.035f * i, 0.09f - 0.075f * i);
                b.Box(Body, at, V(0.04f, 0.06f, 0.05f), 0.012f, steel, V(0, 0, 28f * s), Metal, 0.008f);
                b.PaintEll(Body, at + V(0.03f * s, -0.05f, 0), V(0.04f, 0.012f, 0.055f), cream, V(0, 0, 28f * s), 0.006f);
            }
        });
        // The seamitars, half drawn from their sheaths on the forelegs
        PokeBuilder.Both(s =>
        {
            int leg = BoneOf(b, s < 0 ? "legL" : "legR");
            var root = V(0.14f * s, 0.11f, 0.17f);
            b.Limb(leg, root + V(0, -0.04f, 0.02f), root + V(0.01f * s, 0.1f, -0.03f), 0.022f, 0.018f, Rgb(40, 52, 84), Shell, 0.006f);
            Blade(b, leg, root, V(0.21f * s, 0.56f, -0.03f), 0.034f, blade, V(s, 0, 0.2f), 0.2f, Metal);
            b.Box(leg, root + V(0, -0.012f, 0.004f), V(0.016f, 0.012f, 0.02f), 0.004f, gold, default, Metal, 0.004f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Mega Decidueye

    /// <summary>
    /// Mega Decidueye: its hood deepened into a cowl that shades its face to the eyes, with two leaves for ears, its
    /// eyes lit a ghostly teal, its cloak lengthened into a cape to the ground whose hem is a fan of quills tipped with
    /// a teal glow, and a bow of vine held across its chest, strung with a thread of light.
    /// </summary>
    private static PokeBuilder DecidueyeMega()
    {
        var b = Become(Decidueye(), "Decidueye-Mega", 0.92f);
        var cloak = Rgb(150, 110, 76);
        var hood = Rgb(60, 140, 90);
        var dark = Rgb(46, 104, 70);
        var vine = Rgb(96, 84, 52);
        var teal = Rgb(130, 240, 220);
        Recolour(b, (Rgb(240, 150, 60), teal));
        var bc = V(0, 0.36f, 0);
        var c = bc + V(0, 0.18f, 0.02f);
        int head = BoneOf(b, "head");
        // The cowl, its peak over the brow, and two leaves for ears
        b.Ell(head, c + V(0, 0.025f, -0.03f), V(0.073f, 0.07f, 0.06f), hood, blend: 0.012f);
        b.Spike(head, c + V(0, 0.062f, -0.01f), c + V(0, 0.045f, 0.065f), 0.032f, hood, 0.45f);
        PokeBuilder.Both(s => Blade(b, head, c + V(0.035f * s, 0.07f, -0.02f), c + V(0.07f * s, 0.15f, -0.04f), 0.02f, dark, V(0, 0, 1f), 0.2f, Leaf));
        // The cape to the ground, the hem a fan of quills tipped with light
        foreach (var (x, w) in new[] { (-0.1f, 0.09f), (0f, 0.1f), (0.1f, 0.09f) })
        {
            var root = bc + V(x * 0.7f, 0.11f, -0.045f);
            Frond(b, Body, root, V(x * 1.7f, 0.08f, -0.15f), w, x == 0f ? dark : cloak, V(x * 2f, 0.1f, -1f), 0.2f, Fur, 0.015f);
        }
        for (int i = 0; i < 7; i++)
        {
            float x = -0.15f + 0.05f * i;
            var root = V(x, 0.12f, -0.15f - MathF.Abs(x) * 0.1f);
            var tip = V(x * 1.6f, 0.03f, -0.32f);
            b.Limb(Body, root, tip, 0.009f, 0.006f, vine, Shell, 0.006f);
            b.Spike(Body, tip - Vector3.Normalize(tip - root) * 0.01f, tip + Vector3.Normalize(tip - root) * 0.03f, 0.014f, teal, 0.5f, Glow, 0.004f);
        }
        // The bow of vine across the chest, strung with light
        var bow = new Vector3[9];
        for (int i = 0; i < bow.Length; i++)
        {
            float t = -1f + 2f * i / (bow.Length - 1);
            bow[i] = V(0.2f * t, bc.Y + 0.06f * t * t, 0.068f + 0.11f * t * t);
        }
        b.Tube(Body, bow, 0.01f, 0.01f, vine, Shell, 0.006f);
        b.Limb(Body, bow[0], bow[^1], 0.004f, 0.004f, teal, Glow, 0f);
        PokeBuilder.Both(s => Blade(b, Body, bow[s < 0 ? 0 : ^1], bow[s < 0 ? 0 : ^1] + V(0.03f * s, 0.03f, 0.01f), 0.012f, hood, V(0, 0, 1f), 0.25f, Leaf));
        return b;
    }

    // ------------------------------------------------------------------ Mega Incineroar

    /// <summary>
    /// Mega Incineroar: broader in the chest and shoulders, its belt of fire become a champion's belt, a wide band
    /// with a great gold plate in front bearing a paw print, flames rising from the band behind, red bands on its
    /// forearms and the black mask spread to the brow.
    /// </summary>
    private static PokeBuilder IncineroarMega()
    {
        var b = Become(Incineroar(), "Incineroar-Mega", 1f);
        var black = Rgb(56, 56, 70);
        var rim = Rgb(240, 90, 40);
        var heart = Rgb(250, 170, 60);
        var mix = PixelCanvas.Mix(rim, heart, 0.5f);
        var gold = Rgb(236, 190, 70);
        var band = Rgb(60, 44, 42);
        var bc = V(0, 0.44f, 0);
        // The belt of fire goes
        b.Sdf.RemoveAll(s => s.Bone == Body && (SameColour(s.Color, rim) || SameColour(s.Color, heart) || SameColour(s.Color, mix)));
        // Broader in the chest
        Grow(b, V(1.2f, 1.05f, 1.15f), bc, s => s.Bone == Body && s.Shape == SdfShape.Ellipsoid && SameColour(s.Color, black), _ => false);
        // The champion's belt and its gold plate, flames rising behind
        var waist = bc + V(0, -0.09f, 0);
        b.Torus(Body, waist, 0.12f, 0.032f, band, sz: 0.74f, mat: Shell, blend: 0.006f);
        var plate = waist + V(0, 0, 0.118f);
        b.Box(Body, plate, V(0.058f, 0.046f, 0.014f), 0.008f, gold, default, Metal, 0.004f);
        foreach (float x in new[] { -0.085f, 0.085f })
            b.Box(Body, waist + V(x, 0, 0.098f), V(0.016f, 0.024f, 0.012f), 0.004f, gold, V(0, x * 400f, 0), Metal, 0.004f);
        var dark = Rgb(150, 110, 40);
        var face = plate + V(0, 0, 0.015f);
        // The paw print, painted into the plate
        b.PaintEll(Body, face + V(0, -0.012f, 0), V(0.017f, 0.013f, 0.01f), dark, default, 0.003f);
        foreach (var (x, y) in new[] { (-0.024f, 0.008f), (-0.009f, 0.019f), (0.009f, 0.019f), (0.024f, 0.008f) })
            b.PaintEll(Body, face + V(x, y, 0), V(0.007f, 0.008f, 0.01f), dark, default, 0.003f);
        for (int i = 0; i < 7; i++)
        {
            float a = (110f + 23f * i) * Degree;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = waist + V(d.X * 0.12f, 0.01f, d.Z * 0.09f);
            FlameTongue(b, Body, root, root + V(d.X * 0.04f, 0.13f + 0.03f * (i % 2), d.Z * 0.04f), 0.03f, rim, heart);
        }
        // Red bands on the forearms
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var shoulder = V(0.12f * s, 0.54f, 0);
            var elbow = shoulder + V(0.08f * s, -0.1f, 0.03f);
            var fist = elbow + V(0.02f * s, -0.12f, 0.05f);
            foreach (float t in new[] { 0.3f, 0.62f })
                b.Torus(arm, Vector3.Lerp(elbow, fist, t), 0.042f, 0.014f, Rgb(170, 36, 36), Euler(fist - elbow), mat: Shell, blend: 0.006f);
        });
        // The black mask spread up over the brow
        int head = BoneOf(b, "head");
        var c = bc + V(0, 0.21f, 0.04f);
        b.PaintEll(head, c + V(0, 0.06f, 0.03f), V(0.08f, 0.022f, 0.06f), black);
        return b;
    }

    // ------------------------------------------------------------------ Mega Primarina

    /// <summary>
    /// Mega Primarina: her hair grown into a long train falling to the ground behind her like a gown, teal and pink and
    /// strung with pearls, a tiara of pink coral where her star was, her tail fin spread into a wide fan, and a ring
    /// of floating spheres of water round her, lit pale, held to her on thin strands.
    /// </summary>
    private static PokeBuilder PrimarinaMega()
    {
        var b = Become(Primarina(), "Primarina-Mega", 0.97f);
        var teal = Rgb(150, 220, 226);
        var pink = Rgb(236, 160, 200);
        var coral = Rgb(240, 130, 160);
        var blue = Rgb(80, 140, 226);
        var water = Rgb(210, 240, 252);
        var bc = V(0, 0.18f, -0.02f);
        var c = bc + V(0, 0.15f, 0.05f);
        int head = BoneOf(b, "head");
        // The star goes; a tiara of coral and pearls in its place
        b.Model.Decals.RemoveAll(d => d.Shape == MarkShape.Star5);
        for (int i = -2; i <= 2; i++)
        {
            var root = c + V(0.016f * i, 0.04f - 0.003f * MathF.Abs(i), 0.004f);
            b.Spike(head, root, root + V(0.01f * i, 0.04f - 0.005f * MathF.Abs(i), -0.008f), 0.008f, coral, mat: Shell, blend: 0.004f);
            if (i != 0) b.Spike(head, root + V(0.005f * i, 0.022f, 0), root + V(0.02f * i, 0.035f, 0), 0.005f, coral, mat: Shell, blend: 0.003f);
            b.Ell(head, c + V(0.017f * i, 0.033f - 0.004f * MathF.Abs(i), 0.022f), V(0.006f, 0.006f, 0.006f), White, mat: Glow, blend: 0.003f);
        }
        // The train of hair to the ground behind her, teal with a pink strand, strung with pearls
        PokeBuilder.Both(s =>
        {
            int hair = BoneOf(b, s < 0 ? "earL" : "earR");
            var path = Smooth(3, c + V(0.03f * s, 0.035f, -0.03f), c + V(0.08f * s, 0.02f, -0.12f), V(0.1f * s, 0.14f, -0.24f), V(0.11f * s, 0.06f, -0.36f), V(0.1f * s, 0.035f, -0.44f));
            Ribbon(b, hair, path, 0.06f, teal, V(s, 0.2f, 0), 0.016f);
            var strand = path.Select(p => p + V(0.012f * s, 0.01f, 0)).ToArray();
            Ribbon(b, hair, strand, 0.026f, pink, V(s, 0.2f, 0), 0.012f);
            foreach (var p in path.Where((_, i) => i % 3 == 2))
                b.Ell(hair, p + V(0.016f * s, 0, 0), V(0.01f, 0.01f, 0.01f), White, mat: Glow, blend: 0.004f);
        });
        // The tail fin spread into a wide fan, laced pale
        int tail = BoneOf(b, "tail");
        var end = V(0.04f, 0.04f, -0.26f);
        for (int i = 0; i < 5; i++)
        {
            float a = (-60f + 30f * i) * Degree;
            var tip = end + V(MathF.Sin(a) * 0.14f, 0.025f, -MathF.Cos(a) * 0.1f);
            Frond(b, tail, end, tip, 0.05f, blue, V(0, 1f, 0.2f), 0.2f);
            b.PaintEll(tail, Vector3.Lerp(end, tip, 0.85f), V(0.025f, 0.02f, 0.025f), Rgb(200, 226, 250));
        }
        // The ring of spheres of water round her, on a thread held to her by two strands
        var ring = V(0, 0.22f, -0.03f);
        float rr = 0.24f;
        b.Torus(Body, ring, rr, 0.005f, water, mat: Glow, blend: 0.002f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.2f;
            b.Ell(Body, ring + V(MathF.Cos(a) * rr, 0, MathF.Sin(a) * rr), V(0.028f, 0.028f, 0.028f), water, mat: Glow, blend: 0.004f);
        }
        PokeBuilder.Both(s => b.Limb(Body, ring + V(rr * s, 0, 0), bc + V(0.03f * s, 0.03f, 0), 0.005f, 0.005f, water, Glow, 0.002f));
        return b;
    }

    // ------------------------------------------------------------------ Mega Rillaboom

    /// <summary>
    /// Mega Rillaboom: bigger and more upright, its mane spread over its shoulders like pads of leaves, the stump drum
    /// strapped across its back on a harness of vines, its face a disc ringed dark, and a thick branch with a ball of
    /// leaves at its end in each fist for a drumstick.
    /// </summary>
    private static PokeBuilder RillaboomMega()
    {
        var b = Become(Rillaboom(), "Rillaboom-Mega", 0.97f);
        var green = Rgb(46, 130, 76);
        var light = Rgb(96, 172, 92);
        var wood = Rgb(140, 100, 70);
        var vine = Rgb(52, 100, 50);
        var k = V(1.06f, 1.08f, 1.04f);
        Stretch(b, k);
        // The drum across its back: a log on its side, a face at each end ringed dark, moss over its top
        var drum = V(0, 0.84f, -0.26f);
        b.Ell(Body, drum, V(0.27f, 0.15f, 0.15f), wood, mat: Shell, blend: 0.01f);
        b.PaintEll(Body, drum + V(0.05f, 0.13f, 0), V(0.12f, 0.04f, 0.08f), Rgb(70, 120, 60));
        PokeBuilder.Both(s =>
        {
            var face = drum + V(0.255f * s, 0, 0);
            b.Ell(Body, face, V(0.016f, 0.13f, 0.13f), Rgb(222, 198, 150), mat: Shell, blend: 0.004f);
            b.Torus(Body, face, 0.135f, 0.018f, PixelCanvas.Mix(wood, Black, 0.25f), V(0, 0, 90f), mat: Shell, blend: 0.004f);
            b.PaintTorus(Body, face, 0.07f, 0.008f, PixelCanvas.Shadow(Rgb(222, 198, 150), 0.25f), V(0, 0, 90f));
        });
        // The harness of vines from the drum over the shoulders and across the chest
        PokeBuilder.Both(s =>
        {
            b.Tube(Body, Smooth(3, drum + V(0.12f * s, -0.02f, 0.13f), V(0.14f * s, 0.72f, 0.0f), V(0.06f * s, 0.62f, 0.15f), V(-0.06f * s, 0.5f, 0.15f), V(-0.14f * s, 0.42f, 0.08f)), 0.014f, 0.014f, vine, Leaf, 0.006f);
            // Pads of leaves over the shoulders
            for (int i = 0; i < 5; i++)
            {
                float z = -0.1f + 0.05f * i;
                var root = V(0.15f * s, 0.72f, z);
                Frond(b, Body, root, root + V(0.16f * s, -0.07f, z * 0.6f), 0.05f, i % 2 == 0 ? green : light, V(0.2f * s, 1f, 0), 0.25f, Leaf);
            }
        });
        // A branch in each fist with a ball of leaves at its end
        PokeBuilder.Both(s =>
        {
            int arm = BoneOf(b, s < 0 ? "armL" : "armR");
            var grip = V(0.27f * s, 0.07f, 0.16f) * k;
            var end = grip + V(0.08f * s, 0.26f, 0.22f);
            b.Limb(arm, grip, end, 0.024f, 0.018f, wood, Shell, 0.01f);
            b.Ell(arm, end, V(0.05f, 0.05f, 0.05f), green, mat: Leaf, blend: 0.01f);
            foreach (var d in new[] { V(1f, 0.4f, 0), V(-1f, 0.4f, 0), V(0, 1f, 0.3f), V(0, 0.3f, 1f), V(0, 0.3f, -1f) })
                Frond(b, arm, end, end + Vector3.Normalize(d) * 0.08f, 0.03f, light, V(0, 0.3f, 1f) + d * 0.1f, 0.25f, Leaf);
        });
        return b;
    }

    // ------------------------------------------------------------------ Mega Cinderace

    /// <summary>
    /// Mega Cinderace: its red deepened to crimson, its long ears swept back into a crest of flame, its white ruff
    /// become a scarf trailing behind it, a captain's band of gold round its arm, and a ball of fire held under one
    /// foot, ready to kick.
    /// </summary>
    private static PokeBuilder CinderaceMega()
    {
        var b = Become(Cinderace(), "Cinderace-Mega", 0.95f);
        var crimson = Rgb(176, 40, 48);
        var rim = Rgb(240, 90, 40);
        var heart = Rgb(255, 220, 110);
        var white = Rgb(244, 244, 240);
        var gold = Rgb(236, 190, 64);
        Recolour(b, (Rgb(222, 76, 44), crimson));
        int head = BoneOf(b, "head");
        var c = V(0, 0.73f, 0.01f);
        // The crest of flame streaming back from the ears' tips
        PokeBuilder.Both(s =>
        {
            var tip = c + V(0.07f * s, 0.23f, -0.05f);
            FlameTongue(b, head, tip - V(0.01f * s, 0.03f, -0.01f), tip + V(0.03f * s, 0.05f, -0.26f), 0.03f, rim, heart);
        });
        FlameTongue(b, head, c + V(0, 0.05f, -0.03f), c + V(0, 0.16f, -0.3f), 0.032f, rim, heart);
        // The scarf trailing behind
        PokeBuilder.Both(s => Ribbon(b, Body, Smooth(3, V(0.03f * s, 0.62f, -0.02f), V(0.07f * s, 0.6f, -0.13f), V(0.12f * s, 0.64f, -0.26f), V(0.17f * s, 0.6f, -0.38f)), 0.045f, white, V(0, 1f, 0.2f), 0.01f));
        // A captain's band of gold round the right arm
        int arm = BoneOf(b, "armR");
        var shoulder = V(0.055f, 0.62f, 0);
        var elbow = V(0.1f, 0.53f, 0);
        b.Torus(arm, Vector3.Lerp(shoulder, elbow, 0.55f), 0.024f, 0.008f, gold, Euler(elbow - shoulder), mat: Metal, blend: 0.004f);
        // The ball of fire under the right foot
        int leg = BoneOf(b, "legR");
        var ball = V(0.07f, 0.05f, 0.14f);
        b.Ell(leg, ball, V(0.05f, 0.05f, 0.05f), Rgb(244, 120, 40), mat: Glow, blend: 0.01f);
        b.PaintEll(leg, ball + V(0.01f, 0.01f, 0.03f), V(0.03f, 0.03f, 0.03f), heart);
        foreach (var d in new[] { V(0.4f, 1f, 0.1f), V(-0.5f, 1f, 0.2f), V(0, 1f, 0.6f) })
            FlameTongue(b, leg, ball + Vector3.Normalize(d) * 0.04f, ball + Vector3.Normalize(d) * 0.1f, 0.016f, rim, heart);
        return b;
    }

    // ------------------------------------------------------------------ Mega Inteleon

    /// <summary>
    /// Mega Inteleon: its yellow crest swept back longer into a hood, the tails of a coat falling from its waist to its
    /// knees, and a scope over its right eye, a dark ring round a pale lens lit from within.
    /// </summary>
    private static PokeBuilder InteleonMega()
    {
        var b = Become(Inteleon(), "Inteleon-Mega", 0.95f);
        var yellow = Rgb(240, 214, 90);
        var coat = Rgb(36, 60, 120);
        var navy = Rgb(40, 44, 64);
        int head = BoneOf(b, "head");
        var c = V(0, 0.8f, 0.02f);
        var r = V(0.045f, 0.05f, 0.065f);
        // The hood: the crest longer and broader, flaring at its sides
        Frond(b, head, c + V(0, 0.03f, 0.04f), c + V(0, 0.17f, -0.25f), 0.07f, yellow, V(1f, 0, 0), 0.18f);
        PokeBuilder.Both(s => Frond(b, head, c + V(0.025f * s, 0.03f, 0.0f), c + V(0.06f * s, 0.11f, -0.2f), 0.045f, Rgb(232, 230, 120), V(1f, 0, 0.2f * s), 0.2f));
        // The coat's tails from the waist to the knees
        PokeBuilder.Both(s => Blade(b, Body, V(0.035f * s, 0.52f, -0.035f), V(0.075f * s, 0.2f, -0.13f), 0.05f, coat, V(0.3f * s, 0.1f, -1f), 0.15f));
        b.PaintTorus(Body, V(0, 0.55f, 0), 0.055f, 0.01f, Rgb(40, 150, 150));
        // The scope over the right eye: the eye goes, a ring and a lens take its place
        b.Model.Decals.RemoveAll(d => d.IsEye && d.Center.X > 0f);
        var at = Out(c, r, default, V(0.6f, 0.25f, 0.72f));
        var f = Outward(c, r, at);
        b.Torus(head, at + f * 0.002f, 0.016f, 0.006f, navy, Euler(f), mat: Metal, blend: 0.004f);
        b.Ell(head, at + f * 0.003f, V(0.013f, 0.006f, 0.013f), Rgb(200, 246, 250), Euler(f), Glow, 0.003f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Meowscarada

    /// <summary>
    /// Mega Meowscarada: its white turned dark green, its cape of leaves grown great enough to fall to the ground,
    /// lined pink, the ruff of petals at its throat opened into a full bloom of pink and violet, and a flower bomb
    /// spinning on one fingertip, its petals flat round a disc.
    /// </summary>
    private static PokeBuilder MeowscaradaMega()
    {
        var b = Become(Meowscarada(), "Meowscarada-Mega", 0.95f);
        var dark = Rgb(40, 92, 54);
        var green = Rgb(76, 152, 84);
        var body = Rgb(70, 110, 74);
        var pink = Rgb(236, 110, 160);
        var violet = Rgb(170, 100, 200);
        Recolour(b, (Rgb(236, 240, 228), body));
        var bc = V(0, 0.55f, 0);
        // The great cape, lined pink, to the ground
        foreach (var (x, w) in new[] { (-0.16f, 0.1f), (-0.06f, 0.11f), (0.06f, 0.11f), (0.16f, 0.1f) })
        {
            var root = bc + V(x * 0.45f, 0.09f, -0.045f);
            var tip = V(x * 1.9f, 0.05f, -0.2f - MathF.Abs(x) * 0.15f);
            Frond(b, Body, root, tip, w, MathF.Abs(x) > 0.1f ? dark : green, V(x * 3f, 0.1f, -1f), 0.18f, Leaf, 0.015f);
            Frond(b, Body, root + V(0, -0.02f, 0.012f), tip + V(0, 0.04f, 0.016f), w * 0.8f, pink, V(x * 3f, 0.1f, -1f), 0.15f, Fur, 0.01f);
            foreach (float t in new[] { -1f, 1f })
                Blade(b, Body, tip + V(t * 0.03f, 0.05f, 0), tip + V(t * 0.06f, -0.02f, -0.03f), 0.025f, dark, V(0, 0.2f, -1f), 0.2f, Leaf);
        }
        // The bloom at the throat
        var bloom = bc + V(0, 0.11f, 0.035f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            var d = V(MathF.Cos(a), MathF.Sin(a) * 0.7f - 0.15f, 0.25f);
            Frond(b, Body, bloom + d * 0.015f, bloom + Vector3.Normalize(d) * 0.075f, 0.035f, i % 2 == 0 ? pink : violet, V(0, 0, 1f), 0.22f, Leaf, 0.006f);
        }
        b.Ell(Body, bloom + V(0, -0.01f, 0.02f), V(0.012f, 0.018f, 0.012f), Rgb(200, 60, 90), mat: Shell, blend: 0.004f);
        // The flower bomb spinning on a fingertip of the raised paw
        int arm = BoneOf(b, "armR");
        var hand = bc + V(0.15f, 0.24f, 0.05f);
        var spin = hand + V(0, 0.12f, 0.01f);
        b.Limb(arm, hand + V(0, 0.07f, 0.01f), spin, 0.006f, 0.005f, dark, blend: 0.004f);
        b.Ell(arm, spin, V(0.02f, 0.008f, 0.02f), Rgb(150, 150, 90), mat: Leaf, blend: 0.004f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            var d = V(MathF.Cos(a), 0, MathF.Sin(a));
            Frond(b, arm, spin + d * 0.012f, spin + d * 0.075f, 0.018f, Rgb(176, 184, 124), V(0, 1f, 0), 0.25f, Leaf, 0.004f);
        }
        return b;
    }

    // ------------------------------------------------------------------ Mega Skeledirge

    /// <summary>
    /// Mega Skeledirge: its red darkened to maroon, the bird of flame on its head grown into a great crown of fire
    /// with a wide brim, its flames purple at their edges and the bird's face in the middle of its front, the painted
    /// skull spread down its back as ribs and a spine, and the wisp at its tail become a flame shaped like the head of
    /// a guitar.
    /// </summary>
    private static PokeBuilder SkeledirgeMega()
    {
        var b = Become(Skeledirge(), "Skeledirge-Mega", 1f);
        var maroon = Rgb(150, 40, 44);
        var bone = Rgb(240, 234, 226);
        var orange = Rgb(244, 132, 42);
        var purple = Rgb(160, 96, 226);
        var yellow = Rgb(250, 214, 92);
        Recolour(b, (Rgb(182, 56, 46), maroon));
        var bc = V(0, 0.24f, -0.06f);
        var c = bc + V(0, 0.1f, 0.36f);
        int head = BoneOf(b, "head");
        // The old flames on the crown go; the bird on the snout stays
        b.Sdf.RemoveAll(s => s.Bone == head && s.Material == Glow && s.A.Z < c.Z);
        // The crown of fire: a wide brim, flames purple-edged all round it and the bird's face at its front
        var brim = c + V(0, 0.09f, -0.06f);
        b.Ell(head, brim, V(0.21f, 0.024f, 0.15f), orange, mat: Glow, blend: 0.01f);
        b.Ell(head, brim + V(0, 0.04f, 0), V(0.12f, 0.05f, 0.09f), yellow, mat: Glow, blend: 0.02f);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var root = brim + V(MathF.Cos(a) * 0.14f, 0.01f, MathF.Sin(a) * 0.1f);
            FlameTongue(b, head, root, root + V(MathF.Cos(a) * 0.07f, 0.16f + 0.05f * (i % 2), MathF.Sin(a) * 0.05f), 0.035f, purple, orange);
        }
        foreach (var (x, h) in new[] { (-0.05f, 0.2f), (0.05f, 0.2f), (0f, 0.26f) })
            FlameTongue(b, head, brim + V(x, 0.05f, -0.02f), brim + V(x * 1.4f, 0.05f + h, -0.05f), 0.04f, orange, yellow);
        var bird = brim + V(0, 0.08f, 0.1f);
        b.Ell(head, bird, V(0.042f, 0.046f, 0.03f), orange, mat: Glow, blend: 0.012f);
        b.Spike(head, bird + V(0, -0.005f, 0.02f), bird + V(0, -0.02f, 0.06f), 0.012f, yellow, 0.6f, Glow);
        PokeBuilder.Both(s => b.Spike(head, bird + V(0.025f * s, 0.02f, 0), bird + V(0.08f * s, 0.06f, -0.01f), 0.02f, orange, 0.4f, Glow));
        // Ribs and a spine painted down the back
        for (int i = -2; i <= 2; i++)
            b.PaintEll(Body, bc + V(0, 0.05f, i * 0.085f), V(0.17f, 0.13f, 0.016f), bone, default, 0.006f);
        b.PaintEll(Body, bc + V(0, 0.11f, 0), V(0.022f, 0.03f, 0.27f), bone, default, 0.006f);
        // The flame at the tail's end, shaped like the head of a guitar, with its pegs
        int tail = BoneOf(b, "tail");
        var end = bc + V(0.18f, -0.07f, -0.58f);
        var d = Vector3.Normalize(V(0.6f, 0.5f, -0.6f));
        var lav = Rgb(206, 186, 250);
        b.Ell(tail, end + d * 0.1f, V(0.04f, 0.11f, 0.02f), lav, Euler(d), Glow, 0.012f);
        var side = Vector3.Normalize(Vector3.Cross(d, V(0, 0, 1f)));
        foreach (float t in new[] { 0.06f, 0.11f, 0.16f })
            PokeBuilder.Both(s => b.Ell(tail, end + d * t + side * 0.045f * s, V(0.012f, 0.012f, 0.012f), White, mat: Glow, blend: 0.004f));
        return b;
    }

    // ------------------------------------------------------------------ Mega Quaquaval

    /// <summary>
    /// Mega Quaquaval: its crest of plumes fanned into a crescent headdress, blue with an orange flash at each plume's
    /// end, two long streamers of water beads trailing from its wrists to the ground, and a skirt of blue and teal
    /// feathers fanned all round its hips.
    /// </summary>
    private static PokeBuilder QuaquavalMega()
    {
        var b = Become(Quaquaval(), "Quaquaval-Mega", 1f);
        var blue = Rgb(52, 84, 190);
        var teal = Rgb(70, 170, 200);
        var light = Rgb(150, 212, 244);
        var orange = Rgb(242, 112, 52);
        var bc = V(0, 0.5f, 0);
        var c = bc + V(0, 0.19f, 0.02f);
        int head = BoneOf(b, "head");
        // The crescent headdress
        var root = c + V(0, 0.045f, -0.02f);
        for (int i = 0; i < 9; i++)
        {
            float a = (-80f + 20f * i) * Degree;
            var d = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a), -0.3f));
            float len = 0.22f - 0.05f * MathF.Abs(MathF.Sin(a));
            var tip = root + d * len;
            Frond(b, head, root + d * 0.01f, tip, 0.032f, i % 2 == 0 ? blue : teal, V(0, 0.2f, 1f), 0.2f);
            b.PaintEll(head, root + d * len * 0.82f, V(0.018f, 0.018f, 0.018f), orange, default, 0.005f);
        }
        // Streamers of water beads from the wrists to the ground
        PokeBuilder.Both(s =>
        {
            int wing = BoneOf(b, s < 0 ? "wingL" : "wingR");
            var tip = s > 0 ? bc + V(0.2f * s, 0.18f, 0.02f) : bc + V(0.19f * s, -0.04f, 0.04f);
            var path = Smooth(3, tip, tip + V(0.05f * s, -0.18f, -0.04f), V(0.26f * s, 0.18f, -0.1f), V(0.3f * s, 0.03f, -0.2f));
            var beads = new System.Collections.Generic.List<Vector3> { path[0] };
            for (int i = 1; i < path.Length; i++)
            {
                var from = beads[^1];
                var to = path[i];
                int n = (int)MathF.Ceiling(Vector3.Distance(from, to) / 0.024f);
                for (int j = 1; j <= n; j++) beads.Add(Vector3.Lerp(from, to, j / (float)n));
            }
            for (int i = 0; i < beads.Count; i++)
            {
                float r = 0.018f - 0.006f * i / beads.Count;
                b.Ell(wing, beads[i], V(r, r, r), light, mat: Glow, blend: 0.006f);
            }
        });
        // A skirt of feathers round the hips
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var hip = bc + V(d.X * 0.06f, -0.06f, d.Z * 0.05f);
            Frond(b, Body, hip, hip + V(d.X * 0.09f, -0.13f, d.Z * 0.08f), 0.04f, i % 2 == 0 ? blue : teal, d, 0.22f);
        }
        return b;
    }
}
