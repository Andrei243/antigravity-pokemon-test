using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 11 (plan 46 · CP4 to CP8): Turnulet to Cobzar (20 species), the towns' things come alive: a turret and its
// castle, a cabbage roll, a sweet bread, Baba Dochia and her coats, a jug and its kiln pitcher, a rug and its flying carpet, a
// polenta block, a doughnut, a pocket watch and its clock tower, two pigeons, two street lamps and two lutes. Our own designs
// from the briefs of docs/plans/decisions/CP-dex-4-folk-and-legends.md and CP-dex-4b-towns-more.md; nothing has a forebear,
// so each is sculpted from nothing. Helpers of this batch are named C11… so the other batches' files can't clash with them.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames11() => new[]
    {
        "Turnulet", "Cetatoare", "Sarmaluta", "Cozonacel", "Dochita", "Babadochia", "Ulcel", "Ulcior", "Covoras", "Covoron",
        "Mamaligel", "Papanas", "Ceasulet", "Orologiu", "Porumbel", "Porumbor", "Felinarel", "Felinaroi", "Cobzel", "Cobzar"
    };

    private static PokeBuilder? CreateCarpathia11(string species) => species switch
    {
        "TURNULET" => Turnulet(),
        "CETATOARE" => Cetatoare(),
        "SARMALUTA" => Sarmaluta(),
        "COZONACEL" => Cozonacel(),
        "DOCHITA" => C11Dochia(false),
        "BABADOCHIA" => C11Dochia(true),
        "ULCEL" => Ulcel(),
        "ULCIOR" => Ulcior(),
        "COVORAS" => Covoras(),
        "COVORON" => Covoron(),
        "MAMALIGEL" => Mamaligel(),
        "PAPANAS" => Papanas(),
        "CEASULET" => Ceasulet(),
        "OROLOGIU" => Orologiu(),
        "PORUMBEL" => C11Pigeon(false),
        "PORUMBOR" => C11Pigeon(true),
        "FELINAREL" => Felinarel(),
        "FELINAROI" => Felinaroi(),
        "COBZEL" => Cobzel(),
        "COBZAR" => Cobzar(),
        _ => null
    };

    // ------------------------------------------------------------------ shared pieces

    // The towns' palette: whitewash and grey stone, red roof tiles, cream glaze with brown and green brushwork, brass and black iron
    private static readonly Color C11Stone = Rgb(172, 168, 158), C11StoneDark = Rgb(128, 122, 114), C11StoneTan = Rgb(188, 170, 134);
    private static readonly Color C11Tile = Rgb(186, 74, 56), C11TileDark = Rgb(136, 50, 40);
    private static readonly Color C11Glaze = Rgb(240, 228, 200), C11Brush = Rgb(150, 90, 56), C11BrushGreen = Rgb(108, 148, 82), C11Clay = Rgb(160, 108, 76);
    private static readonly Color C11Brass = Rgb(196, 156, 74), C11BrassDark = Rgb(142, 106, 46), C11Iron = Rgb(52, 48, 50), C11IronRust = Rgb(108, 76, 58);
    private static readonly Color C11Window = Rgb(255, 214, 124), C11IvyGreen = Rgb(74, 124, 64);

    /// <summary>
    /// A cone of roof tiles standing on <paramref name="eave"/>: a round cone cut flat at its eave (so it hangs no bulb below
    /// it), rows of tiles painted round it and a lip at the eave. It is cut, so it must be built before what stands under it.
    /// </summary>
    private static void C11Roof(PokeBuilder b, int bone, Vector3 eave, float r, float h)
    {
        b.Spike(bone, eave, eave + V(0, h, 0), r, C11Tile, mat: Shell, blend: 0.004f);
        b.CutBox(bone, eave - V(0, r * 0.75f, 0), V(r * 1.15f, r * 0.75f, r * 1.15f), Quaternion.Identity);
        for (int k = 1; k <= 3; k++)
        {
            float t = k / 4f;
            b.PaintTorus(bone, eave + V(0, h * t, 0), r * (1f - t) * 1.02f, Math.Max(0.004f, r * 0.06f), C11TileDark);
        }
        b.Ell(bone, eave + V(0, r * 0.04f, 0), V(r * 1.04f, r * 0.13f, r * 1.04f), C11Tile, mat: Shell, blend: 0.006f);
    }

    /// <summary>An upright round wall of stone (a turret's, a tower's) from <paramref name="foot"/> up <paramref name="height"/>.</summary>
    private static SdfPrimitive C11Wall(PokeBuilder b, int bone, Vector3 foot, float r, float height, Color color, float blend = 0.01f) =>
        b.Sdf.Cylinder(foot + V(0, height / 2f, 0), r, height / 2f, Math.Min(r, height) * 0.15f, color, bone, blend, Shell);

    /// <summary>Stone blocks laid in courses round a round wall: dark joints between courses, a few blocks a shade off.</summary>
    private static void C11Courses(PokeBuilder b, int bone, Vector3 foot, float r, float height, int courses, int seed)
    {
        for (int k = 1; k < courses; k++)
            b.PaintTorus(bone, foot + V(0, height * k / courses, 0), r, Math.Max(0.003f, r * 0.03f), C11StoneDark);
        for (int i = 0; i < courses * 2; i++)
        {
            float a = (i * 2.39996f + seed) % MathF.Tau, y = height * ((i * 0.618f + seed * 0.13f) % 1f);
            var at = foot + V(MathF.Sin(a) * r, y, MathF.Cos(a) * r);
            b.PaintEll(bone, at, V(r * 0.22f, height / courses * 0.38f, r * 0.22f), i % 2 == 0 ? C11StoneTan : Rgb(150, 146, 138));
        }
    }

    /// <summary>Ivy creeping up a wall: small leaves along <paramref name="path"/>, set into the surface so it stays one piece.</summary>
    private static void C11Ivy(PokeBuilder b, int bone, Vector3[] path, float leaf)
    {
        var curve = Smooth(3, path);
        for (int i = 0; i < curve.Length; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            b.Ell(bone, curve[i] + V(leaf * 0.6f * side, 0, 0), V(leaf, leaf * 0.7f, leaf * 0.55f), i % 3 == 0 ? Rgb(96, 150, 78) : C11IvyGreen,
                V(0, 0, 30f * side), SurfaceMaterial.Leaf, 0.006f);
        }
    }

    /// <summary>A lit window that is also an eye: a dark stone frame painted round a glowing pane with a pupil in it.</summary>
    private static void C11WindowEye(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float size)
    {
        b.Mark(bone, at + V(0, size * 0.15f, 0), facing, size * 1.25f, size * 1.45f, C11StoneDark);
        b.Eye(bone, at, facing, size, sclera: true, white: C11Window, pupil: Rgb(96, 52, 30));
    }

    /// <summary>A square lantern of black iron and glowing glass with its middle at <paramref name="c"/>: corner posts, a floor plate, a cap.</summary>
    private static void C11Lantern(PokeBuilder b, int bone, Vector3 c, float half, float tall, bool crowned)
    {
        var glass = Rgb(252, 196, 110);
        b.Box(bone, c, V(half, tall, half), half * 0.12f, glass, mat: Glow, blend: 0.004f);
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                b.Limb(bone, c + V(sx * half, -tall, sz * half), c + V(sx * half, tall, sz * half), half * 0.13f, half * 0.13f, C11Iron, Metal, 0.004f);
        b.Box(bone, c - V(0, tall + half * 0.1f, 0), V(half * 1.08f, half * 0.12f, half * 1.08f), half * 0.05f, C11Iron, mat: Metal, blend: 0.006f);
        b.Box(bone, c + V(0, tall + half * 0.1f, 0), V(half * 1.3f, half * 0.12f, half * 1.3f), half * 0.05f, C11Iron, mat: Metal, blend: 0.006f);
        b.Ell(bone, c + V(0, tall + half * 0.45f, 0), V(half * 1.05f, half * 0.55f, half * 1.05f), C11Iron, mat: Metal, blend: 0.01f);
        b.Spike(bone, c + V(0, tall + half * 0.85f, 0), c + V(0, tall + half * 1.5f, 0), half * 0.22f, C11Iron, mat: Metal);
        // The flame inside, a bright heart in the glass
        b.PaintEll(bone, c + V(0, -tall * 0.1f, 0), V(half * 0.55f, tall * 0.55f, half * 1.2f), Rgb(255, 236, 160));
        if (!crowned) return;
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var at = c + V(MathF.Sin(a) * half * 1.15f, tall + half * 0.2f, MathF.Cos(a) * half * 1.15f);
            b.Spike(bone, at, at + V(MathF.Sin(a) * half * 0.15f, half * 0.45f, MathF.Cos(a) * half * 0.15f), half * 0.12f, C11Iron, mat: Metal);
        }
    }

    /// <summary>A curl of wrought iron: a flat spiral from <paramref name="start"/> turning about an axis, for scrolls and tails.</summary>
    private static Vector3[] C11Scroll(Vector3 center, Vector3 u, Vector3 v, float r0, float r1, float a0, float turns, int steps)
    {
        var points = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps, a = a0 + turns * MathF.Tau * t, rr = r0 + (r1 - r0) * t;
            points[i] = center + (u * MathF.Cos(a) + v * MathF.Sin(a)) * rr;
        }
        return points;
    }

    /// <summary>A happy little face of the food Pokémon: two eyes, rosy cheeks, an open smile.</summary>
    private static void C11Face(PokeBuilder b, int bone, Vector3 c, Vector3 r, float x, float y, float size, float mouthY)
    {
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, x * s, y);
            b.Eye(bone, at, Outward(c, r, at), size, Rgb(70, 40, 30));
            var cheek = On(c, r, x * 1.55f * s, y - size * 1.2f);
            b.Mark(bone, cheek, Outward(c, r, cheek), size * 0.9f, size * 0.55f, Rgb(240, 150, 150));
        });
        var m = On(c, r, 0, mouthY);
        b.Mark(bone, m, Outward(c, r, m), size * 0.9f, size * 0.6f, Rgb(150, 60, 60), MarkShape.Smile);
    }

    // ------------------------------------------------------------------ Turnulet line

    /// <summary>Turnulet: a little round stone turret on two stone feet, a red tiled cone for a hat, lit windows for eyes and a slit mouth, ivy up one side.</summary>
    private static PokeBuilder Turnulet()
    {
        var b = new PokeBuilder("Turnulet", 0.62f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Shell };
        // The roof is its head: cut flat at the eave, so it is built first
        int head = b.Head(V(0, 0.46f, 0));
        C11Roof(b, head, V(0, 0.47f, 0), 0.215f, 0.3f);
        var foot = V(0, 0.1f, 0);
        const float r = 0.165f, h = 0.38f;
        C11Wall(b, Body, foot, r, h, C11Stone);
        C11Courses(b, Body, foot, r, h, 5, 1);
        // A heavier course of stones round its base, and a band under the eave
        b.Torus(Body, foot + V(0, 0.04f, 0), r + 0.005f, 0.035f, C11Stone, mat: Shell, blend: 0.01f);
        b.PaintTorus(Body, foot + V(0, 0.04f, 0), r + 0.04f, 0.012f, C11StoneDark);
        b.Torus(Body, foot + V(0, h - 0.01f, 0), r - 0.005f, 0.022f, C11StoneTan, mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.075f * s, 0.12f, 0.02f));
            b.Ell(leg, V(0.08f * s, 0.06f, 0.02f), V(0.06f, 0.06f, 0.065f), C11StoneDark, mat: Shell);
            b.Ell(leg, V(0.085f * s, 0.035f, 0.05f), V(0.065f, 0.035f, 0.07f), C11StoneDark, mat: Shell);
        });
        PokeBuilder.Both(s => C11WindowEye(b, Body, V(0.062f * s, 0.34f, r * 0.94f), V(0.38f * s, 0, 1f), 0.03f));
        b.Mark(Body, V(0, 0.235f, r), V(0, 0, 1f), 0.055f, 0.012f, Rgb(50, 44, 42), MarkShape.Bar);
        C11Ivy(b, Body, new[] { V(0.15f, 0.12f, 0.07f), V(0.16f, 0.22f, 0.03f), V(0.13f, 0.32f, 0.09f), V(0.15f, 0.42f, 0.05f) }, 0.022f);
        return b;
    }

    /// <summary>
    /// Cetatoare: a whitewashed castle on a crag of rock that walks on four cliff legs: a keep crowned with battlements, turrets
    /// under red cones at its corners, the tallest tower its head, two lit windows for eyes and a drawbridge for a mouth.
    /// </summary>
    private static PokeBuilder Cetatoare()
    {
        var b = new PokeBuilder("Cetatoare", 1f, BodyPlan.Quadruped, V(0, 0.6f, 0)) { Coat = Shell };
        var plaster = Rgb(236, 232, 222);
        var rock = Rgb(150, 142, 128);
        var rockDark = Rgb(112, 104, 94);
        var wood = Rgb(132, 92, 58);
        // The roofs first: each is cut at its eave
        int head = b.Head(V(0, 0.84f, -0.06f));
        C11Roof(b, head, V(0, 1.13f, -0.08f), 0.135f, 0.24f);
        var turrets = new[] { V(-0.31f, 0.4f, 0.17f), V(0.31f, 0.4f, 0.17f), V(-0.27f, 0.4f, -0.17f), V(0.27f, 0.4f, -0.17f) };
        float[] tops = { 0.93f, 0.95f, 1f, 0.98f };
        for (int i = 0; i < 4; i++) C11Roof(b, Body, V(turrets[i].X, tops[i], turrets[i].Z), 0.095f, 0.17f);
        // The tall tower that is its head
        C11Wall(b, head, V(0, 0.78f, -0.08f), 0.1f, 0.35f, plaster);
        b.PaintTorus(head, V(0, 1.06f, -0.08f), 0.1f, 0.012f, C11StoneDark);
        b.Mark(head, V(0, 0.98f, 0.02f), V(0, 0, 1f), 0.018f, 0.03f, C11Window);
        // The keep, its battlements and the crag under it
        b.Box(Body, V(0, 0.64f, 0), V(0.3f, 0.2f, 0.2f), 0.03f, plaster, blend: 0.02f);
        for (int i = 0; i < 7; i++)
            b.Box(Body, V(-0.27f + i * 0.09f, 0.86f, 0.17f), V(0.028f, 0.03f, 0.03f), 0.006f, plaster, blend: 0.004f);
        b.PaintEll(Body, V(0, 0.48f, 0.2f), V(0.3f, 0.05f, 0.04f), C11Stone);
        for (int i = 0; i < 4; i++)
        {
            C11Wall(b, Body, turrets[i], 0.075f, tops[i] - turrets[i].Y, plaster);
            b.PaintTorus(Body, V(turrets[i].X, tops[i] - 0.05f, turrets[i].Z), 0.075f, 0.01f, C11StoneDark);
        }
        b.Ell(Body, V(0, 0.42f, 0), V(0.37f, 0.12f, 0.27f), rock, blend: 0.04f);
        // Ivy over the whitewash
        b.PaintEll(Body, V(-0.2f, 0.56f, 0.2f), V(0.07f, 0.12f, 0.04f), C11IvyGreen);
        b.PaintEll(Body, V(0.2f, 0.72f, 0.2f), V(0.05f, 0.1f, 0.04f), Rgb(96, 140, 76));
        // The drawbridge is its jaw: the dark gate behind it, the planks let down on two chains
        b.PaintEll(Body, V(0, 0.52f, 0.2f), V(0.085f, 0.08f, 0.03f), Rgb(46, 36, 34));
        b.Torus(Body, V(0, 0.52f, 0.2f), 0.09f, 0.016f, C11StoneDark, V(90f, 0, 0), mat: Shell, blend: 0.006f);
        int jaw = b.Jaw(Body, V(0, 0.46f, 0.21f));
        b.Box(jaw, V(0, 0.44f, 0.28f), V(0.08f, 0.013f, 0.075f), 0.004f, wood, V(22f, 0, 0), blend: 0.006f);
        for (int i = -1; i <= 1; i++) b.PaintEll(jaw, V(i * 0.045f, 0.45f, 0.28f), V(0.004f, 0.03f, 0.08f), Rgb(96, 64, 40), V(22f, 0, 0));
        PokeBuilder.Both(s => b.Limb(jaw, V(0.07f * s, 0.6f, 0.21f), V(0.07f * s, 0.43f, 0.34f), 0.01f, 0.01f, C11Iron, Metal, 0.004f));
        PokeBuilder.Both(s => C11WindowEye(b, Body, V(0.13f * s, 0.69f, 0.2f), V(0.1f * s, 0, 1f), 0.035f));
        // Four legs of cliff rock, knuckled with boulders
        foreach (var (z, front) in new[] { (0.13f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.23f * s, 0.38f, z), front);
                b.Limb(leg, V(0.23f * s, 0.38f, z), V(0.27f * s, 0.08f, z + 0.03f), 0.1f, 0.085f, rock, Shell, 0.03f);
                b.Ell(leg, V(0.26f * s, 0.22f, z + 0.05f), V(0.07f, 0.06f, 0.06f), rockDark, mat: Shell, blend: 0.02f);
                b.Ell(leg, V(0.27f * s, 0.05f, z + 0.04f), V(0.11f, 0.05f, 0.12f), rockDark, mat: Shell, blend: 0.02f);
                foreach (float t in new[] { -1f, 0f, 1f })
                    b.Ell(leg, V((0.27f + 0.05f * t) * s, 0.035f, z + 0.13f), V(0.035f, 0.035f, 0.04f), rock, mat: Shell, blend: 0.01f);
            });
        return b;
    }

    // ------------------------------------------------------------------ the kitchen: Sarmaluta, Cozonacel, Mamaligel, Papanas

    /// <summary>Sarmaluta: a rolled pickled cabbage leaf sitting in a little clay pot, sour cream on its head and a sprig of dill stuck in it.</summary>
    private static PokeBuilder Sarmaluta()
    {
        var b = new PokeBuilder("Sarmaluta", 0.4f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = SurfaceMaterial.Leaf };
        var cabbage = Rgb(196, 214, 132);
        var fold = Rgb(214, 230, 156);
        var vein = Rgb(232, 240, 196);
        var pot = Rgb(196, 104, 66);
        var cream = Rgb(250, 248, 236);
        var dill = Rgb(70, 132, 62);
        // The pot stands on the ground with the root, so the roll can rock in it
        b.Sdf.Cylinder(V(0, 0.045f, 0), 0.098f, 0.045f, 0.015f, pot, Root, 0.01f, Shell);
        b.Sdf.Cylinder(V(0, 0.11f, 0), 0.118f, 0.04f, 0.015f, pot, Root, 0.02f, Shell);
        b.Torus(Root, V(0, 0.15f, 0), 0.118f, 0.02f, Rgb(178, 92, 58), mat: Shell, blend: 0.006f);
        b.PaintTorus(Root, V(0, 0.105f, 0), 0.12f, 0.006f, Rgb(160, 80, 50));
        // The rolled leaf: a round heart with its folds wrapped over the front
        var c = V(0, 0.235f, 0);
        var r = V(0.115f, 0.105f, 0.105f);
        b.Ell(Body, c, r, cabbage);
        PokeBuilder.Both(s => b.Ell(Body, V(0.06f * s, 0.18f, 0.07f), V(0.075f, 0.055f, 0.045f), fold, V(0, 25f * s, -20f * s), blend: 0.015f));
        b.Ell(Body, V(0.07f, 0.3f, 0.04f), V(0.07f, 0.045f, 0.05f), fold, V(0, -20f, 30f), blend: 0.015f);
        for (int i = 0; i < 4; i++)
        {
            float a = -0.9f + i * 0.6f;
            var at = V(MathF.Sin(a) * 0.112f, 0.19f + 0.02f * i, MathF.Cos(a) * 0.1f);
            b.PaintEll(Body, at, V(0.005f, 0.05f, 0.005f), vein, V(0, 0, 40f - i * 25f), 0.006f);
        }
        // Leaf hands on the pot's rim
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.19f, 0.06f));
            b.Ell(arm, V(0.1f * s, 0.155f, 0.1f), V(0.028f, 0.022f, 0.025f), Rgb(128, 170, 92), blend: 0.012f);
        });
        // A dollop of sour cream with its peak, and the dill stuck in it like a feather
        b.Ell(Body, V(0, 0.335f, -0.005f), V(0.085f, 0.032f, 0.075f), cream, mat: Shell, blend: 0.02f);
        b.Ell(Body, V(0.01f, 0.36f, 0), V(0.045f, 0.03f, 0.04f), cream, mat: Shell, blend: 0.02f);
        b.Spike(Body, V(0.01f, 0.37f, 0), V(-0.02f, 0.405f, 0.01f), 0.022f, cream, mat: Shell);
        var sprig = Smooth(3, V(0.045f, 0.34f, -0.02f), V(0.07f, 0.39f, -0.03f), V(0.1f, 0.44f, -0.035f));
        b.Tube(Body, sprig, 0.008f, 0.005f, dill);
        for (int i = 2; i < sprig.Length; i += 2)
            PokeBuilder.Both(s => b.Limb(Body, sprig[i], sprig[i] + V(0.02f * s + 0.008f, 0.018f, 0), 0.0045f, 0.0035f, dill, blend: 0.003f));
        C11Face(b, Body, c, r, 0.042f, 0.245f, 0.016f, 0.215f);
        return b;
    }

    /// <summary>Cozonacel: a plump slice of festive sweet bread, its golden crust dusted with sugar, a swirl of cocoa and walnut round the face on its cut side.</summary>
    private static PokeBuilder Cozonacel()
    {
        var b = new PokeBuilder("Cozonacel", 0.5f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Shell };
        var crust = Rgb(206, 132, 62);
        var crumb = Rgb(250, 236, 196);
        var cocoa = Rgb(108, 66, 46);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.08f, 0));
            b.Ell(leg, V(0.085f * s, 0.045f, 0.01f), V(0.045f, 0.05f, 0.05f), crust);
        });
        b.Box(Body, V(0, 0.22f, -0.01f), V(0.165f, 0.135f, 0.125f), 0.075f, crust);
        b.Ell(Body, V(0, 0.31f, -0.02f), V(0.17f, 0.08f, 0.12f), crust, blend: 0.04f);
        b.PaintEll(Body, V(0, 0.36f, -0.03f), V(0.16f, 0.05f, 0.11f), Rgb(222, 156, 84));
        // The cut side: soft crumb ringed by crust, the cocoa swirl winding in to the face
        var face = V(0, 0.22f, 0.115f);
        b.PaintEll(Body, face, V(0.14f, 0.13f, 0.02f), crumb, soft: 0.008f);
        b.PaintTorus(Body, face, 0.1f, 0.012f, cocoa, V(90f, 0, 0), 1f, 0.95f);
        b.PaintTorus(Body, face + V(0.01f, -0.005f, 0), 0.068f, 0.01f, cocoa, V(90f, 0, 0), 1f, 0.95f);
        b.PaintEll(Body, face + V(0.06f, -0.06f, 0), V(0.035f, 0.012f, 0.02f), cocoa, V(0, 0, 30f));
        // Sugar on the top crust
        for (int i = 0; i < 9; i++)
        {
            float a = i * 2.4f, rr = 0.03f + 0.012f * i;
            b.PaintEll(Body, V(MathF.Sin(a) * rr, 0.385f, -0.03f + MathF.Cos(a) * rr * 0.8f), V(0.012f, 0.03f, 0.012f), Rgb(252, 250, 244), soft: 0.006f);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.22f, 0.02f));
            b.Ell(arm, V(0.18f * s, 0.19f, 0.03f), V(0.035f, 0.05f, 0.035f), crust, V(0, 0, 20f * s));
        });
        var fr = V(0.14f, 0.13f, 0.02f);
        C11Face(b, Body, face - V(0, 0, 0.02f), fr, 0.026f, 0.235f, 0.014f, 0.205f);
        return b;
    }

    /// <summary>Mamaligel: a round block of golden polenta with a domed top, a pat of butter melting on its head and the string's line round its middle.</summary>
    private static PokeBuilder Mamaligel()
    {
        var b = new PokeBuilder("Mamaligel", 0.5f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Shell };
        var gold = Rgb(236, 192, 84);
        var butter = Rgb(252, 234, 140);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.07f, 0));
            b.Ell(leg, V(0.075f * s, 0.04f, 0.015f), V(0.045f, 0.045f, 0.05f), gold);
        });
        b.Box(Body, V(0, 0.18f, 0), V(0.16f, 0.13f, 0.145f), 0.1f, gold);
        b.Ell(Body, V(0, 0.27f, 0), V(0.165f, 0.13f, 0.15f), gold, blend: 0.05f);
        // The line the string cut it along, and a grain of cornmeal over it
        b.PaintEll(Body, V(0, 0.16f, 0), V(0.22f, 0.006f, 0.22f), Rgb(196, 146, 60), soft: 0.006f);
        for (int i = 0; i < 10; i++)
        {
            float a = i * 2.4f;
            b.PaintEll(Body, V(MathF.Sin(a) * 0.16f, 0.08f + (i * 0.37f % 1f) * 0.26f, MathF.Cos(a) * 0.15f), V(0.01f, 0.01f, 0.01f), Rgb(250, 212, 110), soft: 0.006f);
        }
        // The butter melting down its dome
        b.Box(Body, V(0, 0.405f, 0), V(0.05f, 0.022f, 0.042f), 0.012f, butter, V(0, 15f, 0), blend: 0.006f);
        b.PaintEll(Body, V(0, 0.39f, 0), V(0.1f, 0.03f, 0.09f), butter);
        foreach (var (x, z, len) in new[] { (0.06f, 0.07f, 0.06f), (-0.05f, 0.08f, 0.04f), (0.09f, -0.02f, 0.05f) })
            b.PaintEll(Body, V(x, 0.36f - len * 0.5f, z), V(0.014f, len, 0.03f), butter);
        var c = V(0, 0.24f, 0);
        var r = V(0.165f, 0.16f, 0.15f);
        C11Face(b, Body, c, r, 0.045f, 0.235f, 0.016f, 0.205f);
        return b;
    }

    /// <summary>Papanas: a golden fried ring doughnut standing on its edge, a doughnut ball for a hat, sour cream and dark berry jam dripping down, arms up for joy.</summary>
    private static PokeBuilder Papanas()
    {
        var b = new PokeBuilder("Papanas", 0.52f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Shell };
        var dough = Rgb(214, 146, 70);
        var cream = Rgb(250, 246, 232);
        var jam = Rgb(102, 34, 72);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.06f, 0));
            b.Ell(leg, V(0.065f * s, 0.035f, 0.015f), V(0.04f, 0.035f, 0.045f), dough);
        });
        var ring = V(0, 0.245f, 0);
        b.Torus(Body, ring, 0.13f, 0.085f, dough, V(90f, 0, 0), blend: 0.01f);
        b.PaintTorus(Body, ring, 0.13f, 0.03f, Rgb(232, 170, 92), V(90f, 0, 0), 1f, 1f);
        // The ball on top, the cream poured over both and the jam over that
        int head = b.Head(V(0, 0.42f, 0));
        b.Ell(head, V(0, 0.49f, -0.01f), V(0.068f, 0.064f, 0.066f), dough, blend: 0.02f);
        b.Ell(Body, V(0, 0.41f, 0), V(0.13f, 0.045f, 0.09f), cream, blend: 0.03f);
        foreach (var (x, len) in new[] { (-0.1f, 0.09f), (0.11f, 0.06f), (-0.03f, 0.04f) })
            b.Spike(Body, V(x, 0.4f, 0.05f), V(x * 1.1f, 0.4f - len, 0.075f), 0.022f, cream, mat: Shell, blend: 0.01f);
        b.PaintEll(head, V(0, 0.54f, 0), V(0.06f, 0.025f, 0.06f), cream);
        b.Ell(head, V(0.01f, 0.535f, 0.01f), V(0.045f, 0.018f, 0.045f), jam, blend: 0.012f);
        b.Ell(Body, V(-0.07f, 0.43f, -0.02f), V(0.06f, 0.025f, 0.05f), jam, blend: 0.012f);
        b.Spike(Body, V(-0.12f, 0.41f, 0.0f), V(-0.15f, 0.33f, 0.02f), 0.018f, jam, mat: Shell, blend: 0.008f);
        b.Spike(Body, V(0.13f, 0.4f, -0.01f), V(0.17f, 0.32f, 0.0f), 0.016f, jam, mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.19f * s, 0.3f, 0));
            b.Limb(arm, V(0.18f * s, 0.29f, 0), V(0.27f * s, 0.39f, 0.02f), 0.032f, 0.026f, dough);
            b.Ell(arm, V(0.28f * s, 0.41f, 0.02f), V(0.032f, 0.036f, 0.028f), dough);
        });
        // The face on the ring's upper bow, under the cream
        PokeBuilder.Both(s => b.Eye(Body, V(0.045f * s, 0.345f, 0.074f), V(0.25f * s, 0.35f, 1f), 0.016f, Rgb(70, 40, 30)));
        PokeBuilder.Both(s => b.Mark(Body, V(0.085f * s, 0.32f, 0.07f), V(0.55f * s, 0.25f, 1f), 0.014f, 0.009f, Rgb(240, 140, 140)));
        b.Mark(Body, V(0, 0.322f, 0.068f), V(0, 0.55f, 1f), 0.016f, 0.01f, Rgb(150, 60, 60), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Dochita line

    /// <summary>
    /// Dochita and Babadochia: Baba Dochia of March and her nine sheepskin coats. Dochita is a round bundle of white fleece with
    /// only a frosty face and two bright eyes showing, icicles on its hem and a little crook; Babadochia is the tall, hunched old
    /// shepherdess herself, her sheep's face framed in fleece, a long white braid, the outer coat slipping and snowdrops at her staff.
    /// </summary>
    private static PokeBuilder C11Dochia(bool grown)
    {
        var b = new PokeBuilder(grown ? "Babadochia" : "Dochita", grown ? 0.85f : 0.55f, BodyPlan.Biped, V(0, grown ? 0.5f : 0.25f, 0)) { Coat = Fur };
        float k = grown ? 2f : 1f;
        var fleece = Rgb(246, 244, 236);
        var cream = Rgb(232, 224, 200);
        var grey = Rgb(196, 198, 204);
        var skin = Rgb(196, 214, 236);
        var ice = Rgb(196, 230, 248);
        var staff = Rgb(140, 124, 104);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s * k, 0.06f * k, 0));
            b.Ell(leg, V(0.075f * s * k, 0.025f * k, 0.03f * k), V(0.04f * k, 0.03f * k, 0.05f * k), skin, mat: SurfaceMaterial.Skin);
        });
        // The coats: a round body under rings of fleece, one over another, each a darker or paler skin
        var bc = grown ? V(0, 0.46f, -0.02f) : V(0, 0.24f, 0);
        var br = grown ? V(0.3f, 0.38f, 0.27f) : V(0.19f, 0.19f, 0.17f);
        b.Ell(Body, bc, br, cream, grown ? V(8f, 0, 0) : default, blend: 0.03f);
        int tiers = grown ? 4 : 3;
        for (int t = 0; t < tiers; t++)
        {
            float y = (grown ? 0.14f : 0.07f) + t * (grown ? 0.15f : 0.075f);
            float ry = br.Y, dy = (y - bc.Y) / ry, ring = MathF.Sqrt(MathF.Max(0.15f, 1f - dy * dy));
            var color = t % 2 == 0 ? fleece : (grown && t == 1 ? grey : cream);
            int lumps = grown ? 11 : 8;
            for (int i = 0; i < lumps; i++)
            {
                float a = (i + t * 0.5f) * MathF.Tau / lumps;
                var at = bc + V(MathF.Sin(a) * br.X * ring, y - bc.Y, MathF.Cos(a) * br.Z * ring);
                b.Ell(Body, at, V(0.05f * k, 0.035f * k, 0.045f * k), color, V(0, a * 180f / MathF.PI, 0), blend: 0.02f * k);
                // Icicles hang from the lowest hem
                if (t == 0 && i % 2 == 0)
                    b.Spike(Body, at - V(0, 0.01f * k, 0), at - V(0, 0.05f * k, 0), 0.011f * k, ice, mat: Shell, blend: 0.004f);
            }
        }
        // The hood of fleece round the face, and the face peeking out
        var fc = grown ? V(0, 0.73f, 0.17f) : V(0, 0.33f, 0.12f);
        int head = b.Head(fc - V(0, 0.04f * k, 0.06f * k));
        b.Torus(head, fc - V(0, 0, 0.01f * k), 0.075f * k, 0.045f * k, fleece, V(90f, 0, 0), blend: 0.02f * k);
        b.Ell(head, fc - V(0, 0, 0.04f * k), V(0.09f * k, 0.08f * k, 0.07f * k), fleece, blend: 0.03f * k);
        if (grown)
        {
            // A sheep's face, long and stern, with ears out at the sides and fleece on her brow
            var c = V(0, 0.72f, 0.2f);
            var r = V(0.075f, 0.09f, 0.08f);
            b.Ell(head, c, r, skin, mat: SurfaceMaterial.Skin, blend: 0.02f);
            b.Ell(head, c + V(0, -0.05f, 0.045f), V(0.05f, 0.045f, 0.045f), skin, mat: SurfaceMaterial.Skin, blend: 0.02f);
            b.Ell(head, c + V(0, 0.075f, 0.02f), V(0.08f, 0.035f, 0.06f), fleece, blend: 0.02f);
            PokeBuilder.Both(s =>
            {
                int ear = b.Ear(head, s, c + V(0.12f * s, 0.03f, 0.0f));
                b.Ell(ear, c + V(0.2f * s, 0.01f, 0.02f), V(0.075f, 0.024f, 0.035f), skin, V(0, 0, -15f * s), SurfaceMaterial.Skin, 0.01f);
            });
            b.Mark(head, c + V(0, -0.055f, 0.09f), V(0, 0.1f, 1f), 0.016f, 0.01f, Rgb(120, 130, 170));
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.036f * s, 0.735f);
                b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(60, 80, 110));
                b.Mark(head, at + V(0.002f * s, 0.022f, 0.004f), Outward(c, r, at), 0.02f, 0.006f, Rgb(230, 232, 236), MarkShape.Bar, -12f * s);
            });
            // The long white braid down her front, and the outer coat slipping off one shoulder
            for (int i = 0; i < 7; i++)
                b.Ell(Body, V(0.015f * (i % 2 == 0 ? 1 : -1), 0.6f - i * 0.045f, 0.255f - i * 0.003f), V(0.03f, 0.03f, 0.022f), fleece, V(0, 0, 30f * (i % 2 == 0 ? 1 : -1)), blend: 0.01f);
            b.Ell(Body, V(-0.02f, 0.285f, 0.25f), V(0.02f, 0.04f, 0.018f), fleece, blend: 0.008f);
            b.Ell(Body, V(-0.22f, 0.5f, 0.1f), V(0.1f, 0.16f, 0.1f), grey, V(0, 0, -25f), blend: 0.04f);
        }
        else
        {
            var c = V(0, 0.33f, 0.135f);
            var r = V(0.055f, 0.045f, 0.035f);
            b.Ell(head, c, r, skin, mat: SurfaceMaterial.Skin, blend: 0.01f);
            b.Ell(head, c + V(0, -0.014f, 0.03f), V(0.012f, 0.009f, 0.009f), Rgb(150, 180, 220), mat: SurfaceMaterial.Skin, blend: 0.004f);
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.024f * s, 0.336f);
                b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(50, 70, 100));
            });
            // A muffler of fleece across its chin
            b.Ell(Body, V(0.01f, 0.28f, 0.14f), V(0.09f, 0.04f, 0.05f), fleece, V(0, 0, -10f), blend: 0.02f);
        }
        // The arm with the crook, the other hidden in the coats
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(br.X * 0.85f * s, 0.02f * k, 0.03f * k));
            var hand = bc + V((br.X + 0.03f * k) * s, -0.02f * k, 0.09f * k);
            b.Ell(arm, hand, V(0.04f * k, 0.045f * k, 0.04f * k), s > 0 ? fleece : cream, blend: 0.02f);
            if (s < 0) return;
            float top = grown ? 1.08f : 0.52f;
            var x = hand.X + 0.02f * k;
            var crook = Smooth(3, V(x, 0.012f, hand.Z), V(x, top * 0.6f, hand.Z - 0.005f), V(x, top - 0.06f * k, hand.Z), V(x - 0.02f * k, top, hand.Z),
                V(x - 0.06f * k, top - 0.01f * k, hand.Z), V(x - 0.07f * k, top - 0.05f * k, hand.Z));
            b.Tube(arm, crook, 0.011f * k, 0.009f * k, staff, Shell, 0.006f);
            b.Spike(arm, V(x - 0.02f * k, top - 0.005f * k, hand.Z), V(x - 0.02f * k, top - 0.05f * k, hand.Z), 0.009f * k, ice, mat: Shell, blend: 0.004f);
            if (!grown) return;
            // Snowdrops come up where she stands
            foreach (float dz in new[] { 0.05f, -0.04f })
            {
                var root = V(x, 0.02f, hand.Z);
                var bend = root + V(0.06f, 0.07f, dz);
                b.Tube(arm, new[] { root, root + V(0.03f, 0.06f, dz * 0.6f), bend }, 0.006f, 0.005f, Rgb(80, 140, 80), SurfaceMaterial.Leaf);
                b.Ell(arm, bend + V(0.008f, -0.012f, 0), V(0.012f, 0.018f, 0.012f), Rgb(250, 252, 250), mat: SurfaceMaterial.Leaf, blend: 0.004f);
            }
        });
        return b;
    }

    // ------------------------------------------------------------------ Ulcel line

    /// <summary>Glaze brushwork round a pot: brown and green swirls and a little rooster on its belly.</summary>
    private static void C11Brushwork(PokeBuilder b, int bone, Vector3 c, Vector3 r, float y, int roosters)
    {
        for (int i = 0; i < 6; i++)
        {
            float a = (i - 2.5f) * 0.42f;
            var dir = V(MathF.Sin(a), (y - c.Y) / r.Y, MathF.Cos(a));
            var at = Out(c, r, default, dir);
            b.Mark(bone, at, Outward(c, r, at), r.X * 0.15f, r.X * 0.07f, i % 2 == 0 ? C11Brush : C11BrushGreen, MarkShape.Wave, 10f * (i % 3 - 1));
        }
        for (int i = 0; i < roosters; i++)
        {
            float a = roosters == 1 ? -0.25f : (i - (roosters - 1) / 2f) * 1.1f;
            var at = Out(c, r, default, V(MathF.Sin(a), (y - c.Y) / r.Y + 0.35f, MathF.Cos(a)));
            var n = Outward(c, r, at);
            b.Mark(bone, at, n, r.X * 0.1f, r.X * 0.08f, Rgb(160, 92, 60));
            b.Mark(bone, at + V(r.X * 0.04f, r.X * 0.1f, 0), n, r.X * 0.04f, r.X * 0.035f, Rgb(210, 70, 50), MarkShape.Triangle);
        }
        b.PaintTorus(bone, V(c.X, y - r.Y * 0.32f, c.Z), r.X * 0.9f, r.X * 0.025f, C11Brush);
    }

    /// <summary>Ulcel: a small round clay jug glazed cream, painted with swirls and a rooster, a narrow neck with its eyes under the lip, one handle and stubby feet.</summary>
    private static PokeBuilder Ulcel()
    {
        var b = new PokeBuilder("Ulcel", 0.48f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Shell };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.07f, 0.01f));
            b.Ell(leg, V(0.075f * s, 0.035f, 0.035f), V(0.045f, 0.035f, 0.055f), C11Clay, V(-10f, 0, 0));
        });
        var c = V(0, 0.17f, 0);
        var r = V(0.15f, 0.14f, 0.14f);
        b.Ell(Body, c, r, C11Glaze);
        int head = b.Head(V(0, 0.28f, 0));
        b.Limb(head, V(0, 0.27f, 0), V(0, 0.35f, 0), 0.058f, 0.044f, C11Glaze);
        b.Torus(head, V(0, 0.365f, 0), 0.05f, 0.018f, C11Glaze, mat: Shell, blend: 0.01f);
        b.PaintEll(head, V(0, 0.385f, 0), V(0.035f, 0.012f, 0.035f), Rgb(110, 78, 58));
        // The handle on its back-right side, its root in the body
        int tail = b.Tail(V(0.12f, 0.25f, -0.04f));
        b.Torus(tail, V(0.135f, 0.235f, -0.03f), 0.058f, 0.017f, C11Glaze, V(90f, 0, 0), mat: Shell, blend: 0.012f);
        C11Brushwork(b, Body, c, r, 0.15f, 1);
        PokeBuilder.Both(s => b.Eye(head, V(0.022f * s, 0.315f, 0.048f), V(0.4f * s, 0.1f, 1f), 0.011f, Rgb(50, 40, 30)));
        return b;
    }

    /// <summary>Ulcior: a big walking clay pitcher, its wide mouth glowing like a kiln with heat rising from it, roosters and swirls on its glaze, handles for arms and thick legs.</summary>
    private static PokeBuilder Ulcior()
    {
        var b = new PokeBuilder("Ulcior", 0.82f, BodyPlan.Biped, V(0, 0.48f, 0)) { Coat = Shell };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.26f, 0));
            b.Limb(leg, V(0.12f * s, 0.26f, 0), V(0.14f * s, 0.08f, 0.01f), 0.075f, 0.07f, C11Clay);
            b.Ell(leg, V(0.15f * s, 0.045f, 0.04f), V(0.08f, 0.045f, 0.1f), C11Clay);
        });
        var c = V(0, 0.5f, 0);
        var r = V(0.29f, 0.26f, 0.26f);
        b.Ell(Body, c, r, C11Glaze);
        int head = b.Head(V(0, 0.72f, 0));
        b.Limb(head, V(0, 0.72f, 0), V(0, 0.86f, 0), 0.11f, 0.09f, C11Glaze);
        b.Ell(head, V(0, 0.89f, 0), V(0.13f, 0.035f, 0.13f), C11Glaze, blend: 0.03f);
        b.Torus(head, V(0, 0.91f, 0), 0.11f, 0.028f, C11Glaze, mat: Shell, blend: 0.012f);
        // The kiln's glow in its mouth and the heat rising out of it
        b.Ell(head, V(0, 0.915f, 0), V(0.095f, 0.025f, 0.095f), Rgb(252, 136, 40), mat: Glow, blend: 0.006f);
        b.PaintEll(head, V(0, 0.925f, 0), V(0.05f, 0.015f, 0.05f), Rgb(255, 222, 110));
        foreach (var (x, z, hgt) in new[] { (0f, 0f, 0.13f), (0.05f, -0.02f, 0.08f), (-0.05f, 0.02f, 0.09f) })
            b.Spike(head, V(x, 0.92f, z), V(x * 1.4f, 0.92f + hgt, z), 0.035f, Rgb(252, 150, 60), 0.6f, Glow, 0.012f);
        PokeBuilder.Both(s =>
        {
            var at = V(0.045f * s, 0.8f, 0.098f);
            b.Eye(head, at, V(0.35f * s, 0.1f, 1f), 0.02f, Rgb(60, 40, 30), glare: true);
        });
        // Handles for arms: a loop from the shoulder out and down to a clay fist
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.24f * s, 0.66f, 0));
            var loop = Smooth(3, V(0.22f * s, 0.68f, 0), V(0.36f * s, 0.74f, 0), V(0.43f * s, 0.62f, 0.02f), V(0.44f * s, 0.46f, 0.05f));
            b.Tube(arm, loop, 0.048f, 0.055f, C11Glaze, Shell, 0.012f);
            b.Ell(arm, V(0.44f * s, 0.38f, 0.06f), V(0.075f, 0.075f, 0.07f), C11Glaze);
            b.Mark(arm, V(0.47f * s, 0.6f, 0.06f), V(0.4f * s, 0.2f, 1f), 0.025f, 0.012f, C11BrushGreen, MarkShape.Wave);
        });
        // Two more handles on its back, as a pitcher of the fairs has
        PokeBuilder.Both(s => b.Torus(Body, V(0.16f * s, 0.68f, -0.17f), 0.06f, 0.022f, C11Glaze, V(90f, 35f * s, 0), mat: Shell, blend: 0.012f));
        C11Brushwork(b, Body, c, r, 0.46f, 3);
        return b;
    }

    // ------------------------------------------------------------------ Covoras line

    /// <summary>
    /// A woven runner laid along <paramref name="path"/> (front first), <paramref name="half"/> wide and two half-thicknesses
    /// thick: flat slats along the path, a black and red border along each edge and a cream field. Returns the slats' bones.
    /// </summary>
    private static int[] C11Weave(PokeBuilder b, Vector3[] path, float half, float thick, int per, Color field, Color border, Color stripe)
    {
        int parent = Body, bone = Body;
        var bones = new int[path.Length - 1];
        for (int i = 0; i < path.Length - 1; i++)
        {
            if (i > 0 && i % per == 0)
            {
                bone = b.Part("seg" + i / per, parent, path[i], PokeRole.Segment, i / per);
                parent = bone;
            }
            var a = path[i];
            var d = path[i + 1] - a;
            float len = d.Length();
            float pitch = -MathF.Atan2(d.Y, d.Z) * 180f / MathF.PI;
            var mid = (a + path[i + 1]) / 2f;
            b.Box(bone, mid, V(half, thick, len / 2f + thick * 0.8f), thick * 0.6f, field, V(pitch, 0, 0), SurfaceMaterial.Cloth, 0.002f);
            PokeBuilder.Both(s =>
            {
                b.PaintEll(bone, mid + V(half * 0.86f * s, 0, 0), V(half * 0.1f, thick * 3f, len * 0.6f), border, V(pitch, 0, 0), 0.004f);
                b.PaintEll(bone, mid + V(half * 0.66f * s, 0, 0), V(half * 0.05f, thick * 3f, len * 0.6f), stripe, V(pitch, 0, 0), 0.004f);
            });
            bones[i] = bone;
        }
        return bones;
    }

    /// <summary>Tassels across an end of a rug: short cords from <paramref name="at"/> (the end's middle) out along <paramref name="dir"/>.</summary>
    private static void C11Fringe(PokeBuilder b, int bone, Vector3 at, Vector3 dir, float half, int count, float length, float r, Color color)
    {
        for (int i = 0; i < count; i++)
        {
            float x = -half * 0.85f + 1.7f * half * i / (count - 1);
            var root = at + V(x, 0, 0);
            b.Limb(bone, root - dir * r, root + dir * length, r, r * 0.8f, color, SurfaceMaterial.Cloth, 0.004f);
            b.Ell(bone, root + dir * length, V(r * 1.5f, r * 1.5f, r * 1.5f), color, mat: SurfaceMaterial.Cloth, blend: 0.004f);
        }
    }

    /// <summary>Covoras: a small woven rug in red, black and white, crawling in a hump like a caterpillar on tassel feet, two round eyes in the pattern at its front end.</summary>
    private static PokeBuilder Covoras()
    {
        var red = Rgb(186, 52, 48);
        var black = Rgb(48, 40, 40);
        var cream = Rgb(236, 226, 204);
        var fringe = Rgb(214, 202, 180);
        const int n = 12;
        var path = new Vector3[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float z = 0.3f - 0.6f * i / n;
            float hump = MathF.Exp(-MathF.Pow((z + 0.02f) / 0.1f, 2f));
            path[i] = V(0, 0.024f + 0.17f * hump, z - 0.04f * hump);
        }
        const float half = 0.12f, thick = 0.013f;
        var b = new PokeBuilder("Covoras", 0.62f, BodyPlan.Serpent, path[n / 2]) { Coat = SurfaceMaterial.Cloth };
        var seg = C11Weave(b, path, half, thick, 4, cream, black, red);
        // Diamonds down its back, and the eyes in the pattern at its front
        for (int i = 1; i < n; i += 2)
        {
            if (i >= 5 && i <= 7) continue;
            var d = path[i + 1] - path[i];
            var up = Vector3.Normalize(Vector3.Cross(Vector3.UnitX, d)) * (d.Z < 0 ? 1f : -1f);
            var at = (path[i] + path[i + 1]) / 2f + up * thick;
            b.Mark(seg[i], at, up, 0.028f, 0.028f, red, MarkShape.Diamond);
            b.Mark(seg[i], at + up * 0.001f, up, 0.012f, 0.012f, black, MarkShape.Diamond);
        }
        PokeBuilder.Both(s => b.Eye(Body, V(0.045f * s, path[1].Y + thick, 0.245f), V(0, 1f, 0.15f), 0.018f, Rgb(70, 30, 30)));
        // Fringes at both ends, tassel feet under both sides
        int head = b.Head(path[0], Body);
        C11Fringe(b, head, path[0] + V(0, 0, 0.005f), Vector3.Normalize(V(0, -0.15f, 1f)), half, 7, 0.045f, 0.008f, fringe);
        int tail = b.Tail(path[n]);
        C11Fringe(b, tail, path[n] - V(0, 0, 0.005f), Vector3.Normalize(V(0, -0.15f, -1f)), half, 7, 0.045f, 0.008f, fringe);
        foreach (int i in new[] { 1, 3, 9, 11 })
            PokeBuilder.Both(s =>
            {
                var at = path[i] + V((half - 0.01f) * s, 0, 0);
                b.Spike(Body, at, at + V(0.025f * s, -at.Y + 0.006f, 0), 0.012f, fringe, mat: SurfaceMaterial.Cloth, blend: 0.004f);
            });
        return b;
    }

    /// <summary>Covoron: a great flying carpet woven with a tree of life, rhombuses and zigzags, rippling as it glides, long fringes hanging like legs and calm eyes at its front.</summary>
    private static PokeBuilder Covoron()
    {
        var red = Rgb(178, 46, 44);
        var black = Rgb(44, 36, 38);
        var cream = Rgb(238, 228, 206);
        var fringe = Rgb(216, 204, 180);
        const int n = 12;
        var path = new Vector3[n + 1];
        for (int i = 0; i <= n; i++)
        {
            float t = i / (float)n, z = 0.42f - 0.84f * t;
            path[i] = V(0, 0.3f + 0.06f * MathF.Sin(t * MathF.Tau * 1.1f + 0.4f), z);
        }
        const float half = 0.3f, thick = 0.016f;
        var b = new PokeBuilder("Covoron", 0.9f, BodyPlan.Floating, path[n / 2]) { Coat = SurfaceMaterial.Cloth }.Hover();
        var seg = C11Weave(b, path, half, thick, 4, cream, black, red);
        // The tree of life in the middle, rhombuses fore and aft, zigzags along the field
        Vector3 Top(int i, float x)
        {
            var d = path[i + 1] - path[i];
            var up = Vector3.Normalize(Vector3.Cross(Vector3.UnitX, d)) * (d.Z < 0 ? 1f : -1f);
            return (path[i] + path[i + 1]) / 2f + V(x, 0, 0) + up * thick;
        }
        Vector3 Up(int i)
        {
            var d = path[i + 1] - path[i];
            return Vector3.Normalize(Vector3.Cross(Vector3.UnitX, d)) * (d.Z < 0 ? 1f : -1f);
        }
        b.Mark(seg[6], Top(6, 0), Up(6), 0.055f, 0.055f, red, MarkShape.Diamond);
        b.Mark(seg[6], Top(6, 0), Up(6), 0.025f, 0.025f, black, MarkShape.Diamond);
        b.Mark(seg[4], Top(4, 0), Up(4), 0.04f, 0.045f, black, MarkShape.Star);
        foreach (int i in new[] { 2, 9 })
        {
            b.Mark(seg[i], Top(i, 0), Up(i), 0.05f, 0.05f, red, MarkShape.Diamond);
            b.Mark(seg[i], Top(i, 0), Up(i), 0.022f, 0.022f, black, MarkShape.Diamond);
        }
        foreach (int i in new[] { 4, 8 })
            PokeBuilder.Both(s => b.Mark(seg[i], Top(i, 0.15f * s), Up(i), 0.03f, 0.045f, black, MarkShape.Zigzag));
        PokeBuilder.Both(s => b.Eye(Body, Top(0, -0.17f + 0.06f * s) + V(0, 0, -0.005f), Up(0) + V(0, 0, 0.25f), 0.02f, Rgb(60, 30, 30)));
        int head = b.Head(path[0], Body);
        C11Fringe(b, head, path[0] + V(0, 0, 0.005f), Vector3.Normalize(V(0, -0.5f, 1f)), half, 11, 0.07f, 0.011f, fringe);
        int tail = b.Tail(path[n]);
        C11Fringe(b, tail, path[n] - V(0, 0, 0.005f), Vector3.Normalize(V(0, -0.5f, -1f)), half, 11, 0.07f, 0.011f, fringe);
        // Long fringes hanging down its sides like legs, curling at their ends
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "fringeL" : "fringeR", Body, path[n / 2] + V(half * s, 0, 0), PokeRole.Fin, 0f, s);
            for (int i = 1; i < n; i += 1)
            {
                var root = path[i] + V((half - 0.01f) * s, 0, 0);
                float len = 0.08f + 0.03f * ((i * 7) % 3);
                var end = root + V(0.03f * s, -len, 0.01f);
                b.Tube(side, new[] { root, root + V(0.02f * s, -len * 0.6f, 0), end }, 0.011f, 0.009f, fringe, SurfaceMaterial.Cloth, 0.004f);
                b.Ell(side, end, V(0.016f, 0.02f, 0.016f), fringe, mat: SurfaceMaterial.Cloth, blend: 0.004f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Ceasulet line

    /// <summary>Ceasulet: a small brass pocket watch on spindly legs, a cream face with no hands and a worried face in place of the numbers, its chain trailing like a tail.</summary>
    private static PokeBuilder Ceasulet()
    {
        var b = new PokeBuilder("Ceasulet", 0.45f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Metal };
        var face = Rgb(244, 234, 206);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.1f, 0));
            b.Limb(leg, V(0.05f * s, 0.1f, 0), V(0.065f * s, 0.055f, 0.01f), 0.012f, 0.011f, C11Brass, Metal, 0.006f);
            b.Ell(leg, V(0.065f * s, 0.055f, 0.01f), V(0.02f, 0.02f, 0.02f), C11BrassDark, mat: Metal, blend: 0.006f);
            b.Limb(leg, V(0.065f * s, 0.055f, 0.01f), V(0.06f * s, 0.015f, 0.015f), 0.011f, 0.01f, C11Brass, Metal, 0.006f);
            b.Ell(leg, V(0.06f * s, 0.012f, 0.025f), V(0.022f, 0.012f, 0.03f), C11BrassDark, mat: Metal, blend: 0.006f);
        });
        var c = V(0, 0.22f, 0);
        b.Ell(Body, c, V(0.13f, 0.13f, 0.05f), C11Brass);
        b.Torus(Body, c, 0.12f, 0.024f, C11BrassDark, V(90f, 0, 0), mat: Metal, blend: 0.006f);
        b.PaintEll(Body, c + V(0, 0, 0.05f), V(0.1f, 0.1f, 0.025f), face, soft: 0.006f);
        for (int i = 0; i < 12; i++)
        {
            if (i == 0) continue;
            float a = i * MathF.Tau / 12f;
            var at = c + V(MathF.Sin(a) * 0.082f, MathF.Cos(a) * 0.082f, 0.041f);
            b.Mark(Body, at, V(MathF.Sin(a) * 0.4f, MathF.Cos(a) * 0.4f, 1f), 0.004f, i % 3 == 0 ? 0.013f : 0.007f, Rgb(120, 96, 70), MarkShape.Bar, -i * 30f);
        }
        PokeBuilder.Both(s =>
        {
            b.Eye(Body, c + V(0.038f * s, 0.008f, 0.047f), V(0.15f * s, 0, 1f), 0.02f, Rgb(70, 50, 30));
            b.Mark(Body, c + V(0.042f * s, 0.05f, 0.046f), V(0.1f * s, 0.2f, 1f), 0.02f, 0.005f, Rgb(80, 60, 40), MarkShape.Bar, 20f * s);
        });
        b.Mark(Body, c + V(0, -0.045f, 0.047f), V(0, -0.15f, 1f), 0.022f, 0.008f, Rgb(80, 60, 40), MarkShape.Wave);
        // The crown and the bow above it
        int head = b.Head(V(0, 0.34f, 0));
        b.Limb(head, V(0, 0.34f, 0), V(0, 0.37f, 0), 0.017f, 0.017f, C11Brass, Metal, 0.006f);
        b.Ell(head, V(0, 0.38f, 0), V(0.026f, 0.02f, 0.026f), C11BrassDark, mat: Metal, blend: 0.006f);
        b.Torus(head, V(0, 0.425f, 0), 0.036f, 0.01f, C11Brass, V(90f, 0, 0), mat: Metal, blend: 0.006f);
        // Its chain trailing behind on the ground, a fob at its end
        int tail = b.Tail(V(-0.06f, 0.14f, -0.03f));
        var chain = Smooth(3, V(-0.07f, 0.13f, -0.03f), V(-0.11f, 0.06f, -0.06f), V(-0.15f, 0.012f, -0.08f), V(-0.24f, 0.012f, -0.06f), V(-0.29f, 0.012f, 0.0f));
        b.Tube(tail, chain, 0.007f, 0.007f, C11Brass, Metal, 0.003f);
        for (int i = 1; i < chain.Length; i += 2) b.Ell(tail, chain[i], V(0.011f, 0.009f, 0.011f), C11BrassDark, mat: Metal, blend: 0.003f);
        b.Ell(tail, chain[^1] + V(-0.01f, 0, 0.01f), V(0.02f, 0.009f, 0.024f), C11Brass, mat: Metal, blend: 0.004f);
        return b;
    }

    /// <summary>
    /// Orologiu: an old clock tower that stands on two legs of stone: a pointed roof of tiles, a clock face with its hands for
    /// eyes, bells and a pendulum in the open arches beneath, brass arms of gears, one hand raised as if to hold the moment still.
    /// </summary>
    private static PokeBuilder Orologiu()
    {
        var b = new PokeBuilder("Orologiu", 0.95f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Shell };
        var stone = Rgb(166, 158, 140);
        var stoneDark = Rgb(118, 110, 96);
        var roof = Rgb(140, 84, 62);
        var face = Rgb(244, 236, 212);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.34f, 0));
            b.Box(leg, V(0.1f * s, 0.18f, 0), V(0.06f, 0.14f, 0.065f), 0.015f, stone, blend: 0.01f);
            b.Box(leg, V(0.1f * s, 0.03f, 0.02f), V(0.08f, 0.03f, 0.09f), 0.01f, stoneDark, blend: 0.01f);
            b.Torus(leg, V(0.165f * s, 0.2f, 0), 0.035f, 0.012f, C11Brass, V(0, 0, 90f), mat: Metal, blend: 0.006f);
            b.PaintEll(leg, V(0.1f * s, 0.2f, 0.065f), V(0.04f, 0.035f, 0.01f), C11Brass);
        });
        // The arcade with its bells and pendulum, then the clock above it
        b.Box(Body, V(0, 0.36f, 0), V(0.19f, 0.035f, 0.15f), 0.01f, C11BrassDark, mat: Metal, blend: 0.01f);
        b.Box(Body, V(0, 0.5f, 0), V(0.16f, 0.11f, 0.13f), 0.02f, stone, blend: 0.01f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.075f * s, 0.49f, 0.13f), V(0.05f, 0.08f, 0.02f), Rgb(52, 44, 40)));
        b.PaintEll(Body, V(0, 0.49f, 0.13f), V(0.025f, 0.08f, 0.02f), Rgb(52, 44, 40));
        PokeBuilder.Both(s => b.Ell(Body, V(0.075f * s, 0.5f, 0.13f), V(0.022f, 0.026f, 0.014f), C11Brass, mat: Metal, blend: 0.006f));
        b.Ell(Body, V(0, 0.46f, 0.135f), V(0.026f, 0.026f, 0.01f), C11Brass, mat: Metal, blend: 0.006f);
        b.Box(Body, V(0, 0.77f, 0), V(0.19f, 0.16f, 0.16f), 0.02f, stone, blend: 0.012f);
        b.Box(Body, V(0, 0.615f, 0), V(0.18f, 0.02f, 0.145f), 0.008f, C11BrassDark, mat: Metal, blend: 0.006f);
        var fc = V(0, 0.77f, 0.16f);
        b.Torus(Body, fc, 0.125f, 0.016f, C11Brass, V(90f, 0, 0), mat: Metal, blend: 0.006f);
        b.PaintEll(Body, fc, V(0.115f, 0.115f, 0.02f), face, soft: 0.006f);
        for (int i = 0; i < 12; i++)
        {
            if (i is 2 or 10) continue;
            float a = i * MathF.Tau / 12f;
            b.PaintEll(Body, fc + V(MathF.Sin(a) * 0.085f, MathF.Cos(a) * 0.085f, 0), V(0.006f, 0.012f, 0.025f), Rgb(70, 56, 44), V(0, 0, -i * 30f), 0.004f);
        }
        // Its hands are its brows over the eyes
        PokeBuilder.Both(s =>
        {
            b.Eye(Body, fc + V(0.045f * s, -0.005f, 0.001f), V(0, 0, 1f), 0.026f, Rgb(60, 44, 30), glare: true);
            b.Mark(Body, fc + V(0.045f * s, 0.04f, 0.001f), V(0, 0, 1f), 0.05f, 0.008f, Rgb(50, 40, 34), MarkShape.Bar, 18f * s);
        });
        b.Mark(Body, fc + V(0, 0.015f, 0.001f), V(0, 0, 1f), 0.012f, 0.012f, C11BrassDark);
        // The roof: tiled steps to a brass finial and its ring
        int head = b.Head(V(0, 0.93f, 0));
        for (int i = 0; i < 4; i++)
        {
            float w = 0.21f - i * 0.05f;
            b.Box(head, V(0, 0.95f + i * 0.045f, 0), V(w, 0.028f, w * 0.88f), 0.012f, i % 2 == 0 ? roof : Rgb(124, 72, 54), blend: 0.012f);
        }
        b.Limb(head, V(0, 1.1f, 0), V(0, 1.15f, 0), 0.016f, 0.012f, C11Brass, Metal, 0.006f);
        b.Ell(head, V(0, 1.155f, 0), V(0.02f, 0.016f, 0.02f), C11Brass, mat: Metal, blend: 0.006f);
        b.Torus(head, V(0, 1.195f, 0), 0.03f, 0.009f, C11Brass, V(90f, 0, 0), mat: Metal, blend: 0.006f);
        // Arms of brass and gears, the right raised, the left down holding a clock's hand
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.19f * s, 0.84f, 0));
            var shoulder = V(0.2f * s, 0.84f, 0);
            var elbow = s > 0 ? V(0.33f * s, 0.86f, 0.03f) : V(0.3f * s, 0.65f, 0.03f);
            var hand = s > 0 ? V(0.38f * s, 1.0f, 0.04f) : V(0.32f * s, 0.47f, 0.05f);
            b.Torus(arm, shoulder + V(0.02f * s, 0, 0), 0.04f, 0.014f, C11Brass, V(0, 0, 90f), mat: Metal, blend: 0.006f);
            b.Limb(arm, shoulder, elbow, 0.028f, 0.024f, C11Brass, Metal, 0.008f);
            b.Ell(arm, elbow, V(0.032f, 0.032f, 0.032f), C11BrassDark, mat: Metal, blend: 0.008f);
            b.Limb(arm, elbow, hand, 0.024f, 0.02f, C11Brass, Metal, 0.008f);
            b.Ell(arm, hand, V(0.032f, 0.032f, 0.026f), C11Brass, mat: Metal, blend: 0.008f);
            var dir = Vector3.Normalize(hand - elbow);
            Digits(b, arm, hand + dir * 0.02f, dir, V(0.5f, 0, 0.3f), 0.04f, 0.009f, C11Brass, Metal);
            if (s < 0) b.Spike(arm, hand + V(0, 0.03f, 0.02f), hand + V(-0.04f, 0.13f, 0.04f), 0.014f, C11BrassDark, 0.4f, Metal);
        });
        return b;
    }

    // ------------------------------------------------------------------ Porumbel line

    /// <summary>
    /// Porumbel and Porumbor: city pigeons. Porumbel is a plump little grey pigeon with a green and violet sheen round its neck and
    /// two dark bars on its wings; Porumbor a big proud bird, its puffed iridescent chest hung with gold medals like a general's
    /// coat, gold epaulettes on its shoulders, a curl of crest and its chin lifted.
    /// </summary>
    private static PokeBuilder C11Pigeon(bool grown)
    {
        var b = new PokeBuilder(grown ? "Porumbor" : "Porumbel", grown ? 0.75f : 0.45f, BodyPlan.Bird, V(0, grown ? 0.36f : 0.17f, 0)) { Coat = Fur };
        float k = grown ? 2.05f : 1f;
        var grey = Rgb(140, 148, 168);
        var pale = Rgb(198, 204, 220);
        var dark = Rgb(78, 82, 98);
        var green = Rgb(76, 176, 122);
        var violet = Rgb(140, 102, 190);
        var pink = Rgb(226, 140, 150);
        var gold = Rgb(236, 192, 74);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s * k, 0.08f * k, 0.01f * k));
            b.Limb(leg, V(0.035f * s * k, 0.09f * k, 0.01f * k), V(0.04f * s * k, 0.018f * k, 0.02f * k), 0.011f * k, 0.008f * k, pink, SurfaceMaterial.Skin);
            Digits(b, leg, V(0.04f * s * k, 0.012f * k, 0.025f * k), V(0, 0, 1f), V(0.7f, 0, 0), 0.032f * k, 0.0075f * k, pink, SurfaceMaterial.Skin);
        });
        var bc = V(0, 0.17f * k, -0.01f * k);
        var br = grown ? V(0.105f * k, 0.1f * k, 0.13f * k) : V(0.1f, 0.095f, 0.12f);
        b.Ell(Body, bc, br, grey, V(-12f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.02f * k, 0.07f * k), V(0.07f * k, 0.065f * k, 0.06f * k), pale);
        if (grown)
        {
            // The puffed chest: a swell of iridescent feathers before the body, scalloped and hung with medals
            var cc = bc + V(0, 0.045f * k, 0.075f * k);
            var cr = V(0.085f * k, 0.075f * k, 0.065f * k);
            b.Ell(Body, cc, cr, green, blend: 0.03f * k);
            b.PaintEll(Body, cc + V(0, -0.04f * k, 0.03f * k), V(0.08f * k, 0.035f * k, 0.06f * k), violet);
            for (int i = 0; i < 5; i++)
            {
                float x = (i - 2) * 0.03f * k;
                var at = On(cc, cr, x, cc.Y - 0.015f * k + MathF.Abs(x) * 0.3f);
                b.Mark(Body, at, Outward(cc, cr, at), 0.012f * k, 0.016f * k, i % 2 == 0 ? violet : Rgb(60, 140, 104), MarkShape.Triangle, 180f);
            }
            foreach (var (x, y) in new[] { (-0.04f, 0.01f), (0.035f, 0.005f), (0.0f, -0.03f) })
            {
                var at = On(cc, cr, x * k, cc.Y + y * k);
                b.Mark(Body, at, Outward(cc, cr, at), 0.014f * k, 0.014f * k, gold, MarkShape.Star5);
                b.Mark(Body, at + V(0, 0.022f * k, 0), Outward(cc, cr, at), 0.006f * k, 0.012f * k, Rgb(196, 60, 60), MarkShape.Bar);
            }
        }
        else
        {
            b.PaintTorus(Body, bc + V(0, 0.07f * k, 0.03f), 0.075f, 0.022f, green, V(-25f, 0, 0));
            b.PaintTorus(Body, bc + V(0, 0.045f * k, 0.035f), 0.088f, 0.016f, violet, V(-25f, 0, 0));
        }
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.075f * s * k, 0.2f * k, 0));
            var wc = V(0.085f * s * k, 0.165f * k, -0.035f * k);
            b.Ell(wing, wc, V(0.026f * k, 0.06f * k, grown ? 0.11f * k : 0.08f), grey, V(15f, 0, 0), blend: 0.015f * k);
            foreach (float z in new[] { -0.03f, -0.065f })
                b.PaintEll(wing, wc + V(0.022f * s * k, -0.005f * k, z * k + 0.02f * k), V(0.03f * k, 0.06f * k, 0.009f * k), dark, V(15f, 0, 0));
            if (!grown) return;
            // Gold epaulettes on its shoulders, fringed
            var ep = V(0.085f * s * k, 0.225f * k, 0.03f * k);
            b.Ell(wing, ep, V(0.032f * k, 0.014f * k, 0.03f * k), gold, mat: Metal, blend: 0.008f);
            for (int i = 0; i < 4; i++)
            {
                float z = (i - 1.5f) * 0.014f * k;
                b.Limb(wing, ep + V(0.018f * s * k, -0.004f * k, z), ep + V(0.03f * s * k, -0.022f * k, z), 0.005f * k, 0.004f * k, gold, Metal, 0.003f);
            }
        });
        int tail = b.Tail(V(0, 0.14f * k, -0.11f * k));
        Frond(b, tail, V(0, 0.14f * k, -0.11f * k), V(0, 0.11f * k, grown ? -0.33f * k : -0.2f), 0.045f * k, dark, V(0, 1f, 0), 0.3f);
        b.PaintEll(tail, V(0, 0.115f * k, grown ? -0.29f * k : -0.19f), V(0.05f * k, 0.03f * k, 0.03f * k), Rgb(56, 58, 70));
        // The head: a round grey head, the white cere over its dark beak, orange eyes; Porumbor's raised, browed and crested
        int head = b.Head(V(0, 0.23f * k, 0.045f * k));
        var c = grown ? V(0, 0.315f * k, 0.065f * k) : V(0, 0.285f, 0.06f);
        var r = V(0.06f * k, 0.06f * k, 0.06f * k) * (grown ? 0.9f : 1f);
        b.Limb(head, V(0, 0.22f * k, 0.04f * k), c, 0.05f * k, 0.042f * k, grey);
        b.Ell(head, c, r, grey);
        b.PaintEll(head, c + V(0, -0.045f * k, 0.015f * k), V(0.055f * k, 0.03f * k, 0.05f * k), grown ? green : green);
        b.Spike(head, c + V(0, -0.012f * k, r.Z * 0.9f), c + V(0, -0.028f * k, r.Z + 0.04f * k), 0.014f * k, dark, 0.7f, Shell);
        b.Ell(head, c + V(0, -0.002f * k, r.Z * 0.95f), V(0.016f * k, 0.011f * k, 0.013f * k), Rgb(236, 226, 228), mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.032f * k * s, c.Y + 0.01f * k);
            var n = Outward(c, r, at);
            b.Mark(head, at, n, 0.019f * k, 0.019f * k, Rgb(244, 140, 48));
            b.Eye(head, at, n, 0.0095f * k, Rgb(40, 30, 30), glare: grown);
            if (grown) b.Mark(head, at + V(0, 0.02f * k, 0.002f), n, 0.022f * k, 0.007f * k, Rgb(228, 230, 236), MarkShape.Bar, -15f * s);
        });
        if (grown)
            b.Tube(head, Smooth(3, c + V(0, r.Y * 0.8f, -0.01f * k), c + V(0, r.Y + 0.03f * k, -0.04f * k), c + V(0, r.Y + 0.04f * k, -0.08f * k),
                c + V(0, r.Y + 0.015f * k, -0.095f * k)), 0.012f * k, 0.006f * k, grey);
        return b;
    }

    // ------------------------------------------------------------------ Felinarel line

    /// <summary>Felinarel: an old gas street lamp hopping on one iron leg, its lantern a head with a dancing flame, cat-ear scrolls on its black cap and an iron curl for a tail.</summary>
    private static PokeBuilder Felinarel()
    {
        var b = new PokeBuilder("Felinarel", 0.6f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Metal };
        int leg = b.Leg(1f, V(0, 0.3f, 0));
        b.Limb(leg, V(0, 0.31f, 0), V(0.01f, 0.14f, 0.0f), 0.016f, 0.013f, C11Iron, Metal, 0.006f);
        b.Limb(leg, V(0.01f, 0.14f, 0.0f), V(0, 0.03f, 0.01f), 0.013f, 0.014f, C11Iron, Metal, 0.006f);
        b.Ell(leg, V(0, 0.022f, 0.02f), V(0.04f, 0.022f, 0.045f), C11Iron, mat: Metal, blend: 0.008f);
        PokeBuilder.Both(s => b.Tube(leg, C11Scroll(V(0.03f * s, 0.035f, 0.04f), V(s, 0, 0), V(0, 1f, 0), 0.02f, 0.006f, -MathF.PI / 2f, 0.8f, 8), 0.008f, 0.006f, C11Iron, Metal, 0.004f));
        // The lantern on its post
        b.Limb(Body, V(0, 0.27f, 0), V(0, 0.33f, 0), 0.022f, 0.03f, C11Iron, Metal, 0.008f);
        var c = V(0, 0.42f, 0);
        C11Lantern(b, Body, c, 0.08f, 0.085f, false);
        PokeBuilder.Both(s => b.Eye(Body, c + V(0.032f * s, 0.01f, 0.081f), V(0.1f * s, 0, 1f), 0.018f, sclera: true, white: Rgb(255, 238, 170), pupil: Rgb(214, 80, 30)));
        b.Mark(Body, c + V(0, -0.04f, 0.081f), V(0, 0, 1f), 0.02f, 0.026f, Rgb(244, 120, 40), MarkShape.Triangle);
        // Cat ears of scrollwork on its cap, and curls on its sides
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(Body, s, c + V(0.07f * s, 0.11f, 0));
            var path = Smooth(3, c + V(0.06f * s, 0.105f, 0), c + V(0.085f * s, 0.16f, 0), c + V(0.07f * s, 0.2f, 0), c + V(0.045f * s, 0.15f, 0));
            b.Tube(ear, path, 0.009f, 0.007f, C11Iron, Metal, 0.004f);
            b.Tube(ear, C11Scroll(c + V(0.068f * s, 0.15f, 0), V(s, 0, 0), V(0, 1f, 0), 0.016f, 0.005f, MathF.PI, 0.9f, 8), 0.006f, 0.005f, C11Iron, Metal, 0.003f);
            b.Tube(Body, C11Scroll(c + V(0.1f * s, 0.0f, 0), V(s, 0, 0), V(0, 1f, 0), 0.018f, 0.006f, MathF.PI, 1f, 10), 0.007f, 0.006f, C11Iron, Metal, 0.003f);
        });
        int tail = b.Tail(V(0, 0.32f, -0.06f));
        var curl = C11Scroll(V(0, 0.33f, -0.16f), V(0, 0, 1f), V(0, 1f, 0), 0.065f, 0.012f, MathF.PI * 1.15f, 1.15f, 18);
        b.Tube(tail, new[] { V(0, 0.33f, -0.02f), curl[0] }, 0.012f, 0.011f, C11Iron, Metal, 0.006f);
        b.Tube(tail, curl, 0.011f, 0.009f, C11Iron, Metal, 0.004f);
        return b;
    }

    /// <summary>Felinaroi: a tall, elegant wrought-iron boulevard lamp on two thin legs, its crowned lantern head glowing gold, flowering iron arms each hung with a lantern, a scroll for a tail.</summary>
    private static PokeBuilder Felinaroi()
    {
        var b = new PokeBuilder("Felinaroi", 0.92f, BodyPlan.Biped, V(0, 0.66f, 0)) { Coat = Metal };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.52f, 0));
            b.Limb(leg, V(0.05f * s, 0.53f, 0), V(0.075f * s, 0.27f, 0.03f), 0.026f, 0.018f, C11Iron, Metal, 0.008f);
            b.Limb(leg, V(0.075f * s, 0.27f, 0.03f), V(0.09f * s, 0.04f, 0.0f), 0.018f, 0.014f, C11Iron, Metal, 0.008f);
            b.Ell(leg, V(0.095f * s, 0.025f, 0.035f), V(0.032f, 0.025f, 0.05f), C11Iron, mat: Metal, blend: 0.01f);
            foreach (float d in new[] { -1f, 0f, 1f })
                b.Ell(leg, V((0.095f + 0.018f * d) * s, 0.014f, 0.08f), V(0.011f, 0.012f, 0.014f), C11IronRust, mat: Metal, blend: 0.004f);
            b.Tube(leg, C11Scroll(V(0.09f * s, 0.075f, 0.03f), V(s, 0, 0), V(0, 1f, 0), 0.025f, 0.007f, MathF.PI * 0.5f, 0.85f, 8), 0.007f, 0.006f, C11Iron, Metal, 0.004f);
        });
        // A slim body of black iron worked with scrolls
        b.Ell(Body, V(0, 0.68f, 0), V(0.065f, 0.17f, 0.055f), C11Iron, mat: Metal);
        b.Ell(Body, V(0, 0.56f, 0), V(0.075f, 0.05f, 0.06f), C11Iron, mat: Metal, blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.025f * s, 0.72f, 0.05f), V(0.012f, 0.06f, 0.02f), C11IronRust, V(0, 0, -20f * s));
            b.Tube(Body, C11Scroll(V(0.045f * s, 0.6f, 0.04f), V(s, 0, 0), V(0, 1f, 0), 0.022f, 0.006f, MathF.PI * 0.6f, 0.9f, 8), 0.007f, 0.006f, C11Iron, Metal, 0.004f);
        });
        // The crowned lantern head on a slender neck
        int head = b.Head(V(0, 0.84f, 0));
        b.Limb(head, V(0, 0.82f, 0), V(0, 0.92f, 0), 0.022f, 0.03f, C11Iron, Metal, 0.008f);
        var c = V(0, 1.02f, 0);
        C11Lantern(b, head, c, 0.085f, 0.09f, true);
        PokeBuilder.Both(s =>
        {
            b.Eye(head, c + V(0.034f * s, 0.015f, 0.086f), V(0.1f * s, 0, 1f), 0.019f, sclera: true, white: Rgb(255, 236, 160), pupil: Rgb(200, 100, 30), glare: true);
            var path = Smooth(3, c + V(0.065f * s, 0.115f, 0), c + V(0.09f * s, 0.175f, 0), c + V(0.075f * s, 0.215f, 0), c + V(0.045f * s, 0.16f, 0));
            b.Tube(head, path, 0.01f, 0.007f, C11Iron, Metal, 0.004f);
            b.Tube(head, C11Scroll(c + V(0.1f * s, 0.0f, 0), V(s, 0, 0), V(0, 1f, 0), 0.02f, 0.006f, MathF.PI, 1f, 10), 0.007f, 0.006f, C11Iron, Metal, 0.003f);
        });
        b.Mark(head, c + V(0, -0.04f, 0.086f), V(0, 0, 1f), 0.022f, 0.028f, Rgb(244, 120, 40), MarkShape.Triangle);
        // Arms of flowering ironwork, a lantern hung from each
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.78f, 0));
            var reach = Smooth(3, V(0.05f * s, 0.78f, 0), V(0.15f * s, 0.84f, 0.01f), V(0.25f * s, 0.8f, 0.02f), V(0.34f * s, 0.86f, 0.02f), V(0.39f * s, 0.82f, 0.02f));
            b.Tube(arm, reach, 0.016f, 0.012f, C11Iron, Metal, 0.006f);
            b.Tube(arm, C11Scroll(V(0.17f * s, 0.88f, 0.01f), V(s, 0, 0), V(0, 1f, 0), 0.04f, 0.008f, -MathF.PI * 0.5f, 1f, 12), 0.008f, 0.006f, C11Iron, Metal, 0.004f);
            b.Tube(arm, C11Scroll(V(0.28f * s, 0.76f, 0.02f), V(s, 0, 0), V(0, 1f, 0), 0.035f, 0.007f, MathF.PI * 0.5f, 1f, 12), 0.008f, 0.006f, C11Iron, Metal, 0.004f);
            b.Torus(arm, V(0.39f * s, 0.79f, 0.02f), 0.018f, 0.006f, C11Iron, V(90f, 0, 0), mat: Metal, blend: 0.003f);
            C11Lantern(b, arm, V(0.39f * s, 0.69f, 0.02f), 0.04f, 0.05f, false);
        });
        int tail = b.Tail(V(0, 0.6f, -0.05f));
        var curl = C11Scroll(V(0.06f, 0.53f, -0.15f), V(0, 0, 1f), V(0, 1f, 0) * -1f, 0.075f, 0.012f, MathF.PI * 0.82f, 1.2f, 20);
        b.Tube(tail, new[] { V(0, 0.6f, -0.03f), curl[0] }, 0.013f, 0.012f, C11Iron, Metal, 0.006f);
        b.Tube(tail, curl, 0.012f, 0.009f, C11Iron, Metal, 0.004f);
        return b;
    }

    // ------------------------------------------------------------------ Cobzel line

    /// <summary>The cobza's face: a pale soundboard, a rosette round its sound hole, strings over it to a bridge.</summary>
    private static void C11Soundboard(PokeBuilder b, int bone, Vector3 c, Vector3 r, float y, float size, int strings)
    {
        var board = Rgb(232, 202, 150);
        var hole = On(c, r, 0, y + size * 0.15f);
        var n = Outward(c, r, hole);
        b.PaintEll(bone, V(c.X, y, c.Z + r.Z * 0.8f), V(r.X * 0.82f, r.Y * 0.62f, r.Z * 0.5f), board);
        b.Mark(bone, hole, n, size * 0.36f, size * 0.36f, Rgb(150, 100, 58), MarkShape.Ring);
        b.Mark(bone, hole, n, size * 0.2f, size * 0.2f, Rgb(70, 44, 30));
        for (int i = 0; i < strings; i++)
        {
            float x = (i - (strings - 1) / 2f) * size * 0.14f;
            var at = On(c, r, x, y + size * 0.15f);
            b.PaintEll(bone, at, V(size * 0.025f, size * 0.42f, size * 0.25f), Rgb(236, 232, 220), soft: 0.004f);
        }
        var bridge = On(c, r, 0, y - size * 0.55f);
        b.Box(bone, bridge, V(size * 0.28f, size * 0.03f, size * 0.03f), size * 0.015f, Rgb(108, 66, 42), default, Shell, 0.004f);
    }

    /// <summary>A cobza's pegbox, bent back from <paramref name="root"/>, its pegs out at both sides.</summary>
    private static void C11Pegbox(PokeBuilder b, int bone, Vector3 root, float size, Color wood)
    {
        var path = Smooth(3, root, root + V(0, size * 0.5f, -size * 0.05f), root + V(0.05f * size, size * 0.85f, -size * 0.2f), root + V(0.25f * size, size, -size * 0.32f));
        b.Tube(bone, path, size * 0.11f, size * 0.08f, wood, Shell, 0.01f);
        foreach (int i in new[] { 3, 5, 7 })
            PokeBuilder.Both(s => b.Ell(bone, path[i] + V(size * 0.12f * s, 0, 0), V(size * 0.07f, size * 0.045f, size * 0.045f), Rgb(214, 160, 96), mat: Shell, blend: 0.006f));
    }

    /// <summary>Cobzel: a little round-backed lute of honey-brown wood, a short pegbox bent back over its head, three strings over its belly that it plucks with tiny hands.</summary>
    private static PokeBuilder Cobzel()
    {
        var b = new PokeBuilder("Cobzel", 0.52f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Shell };
        var wood = Rgb(200, 132, 62);
        var dark = Rgb(160, 98, 46);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.09f, 0));
            b.Limb(leg, V(0.07f * s, 0.09f, 0), V(0.08f * s, 0.03f, 0.01f), 0.03f, 0.026f, wood);
            b.Ell(leg, V(0.08f * s, 0.02f, 0.025f), V(0.032f, 0.02f, 0.04f), wood);
        });
        var c = V(0, 0.2f, 0);
        var r = V(0.15f, 0.15f, 0.12f);
        b.Ell(Body, c, r, wood);
        b.Ell(Body, V(0, 0.33f, 0), V(0.095f, 0.11f, 0.085f), wood, blend: 0.06f);
        // The ribs of its round back
        for (int i = -2; i <= 2; i++)
            b.PaintEll(Body, V(i * 0.05f, 0.2f, -0.1f), V(0.005f, 0.14f, 0.05f), dark, V(0, i * 15f, 0));
        C11Soundboard(b, Body, c, r, 0.17f, 0.15f, 3);
        int head = b.Head(V(0, 0.38f, 0));
        C11Pegbox(b, head, V(0, 0.42f, 0), 0.16f, wood);
        var hc = V(0, 0.33f, 0);
        var hr = V(0.095f, 0.11f, 0.085f);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.035f * s, 0.355f);
            b.Eye(Body, at, Outward(hc, hr, at), 0.016f, Rgb(50, 30, 20));
        });
        var m = On(hc, hr, 0, 0.315f);
        b.Mark(Body, m, Outward(hc, hr, m), 0.008f, 0.01f, Rgb(140, 60, 60));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.27f, 0.04f));
            var hand = s < 0 ? V(-0.04f, 0.2f, 0.135f) : V(0.17f, 0.3f, 0.06f);
            b.Limb(arm, V(0.12f * s, 0.27f, 0.05f), hand, 0.024f, 0.02f, wood);
            b.Ell(arm, hand, V(0.024f, 0.022f, 0.02f), wood);
        });
        return b;
    }

    /// <summary>Cobzar: a lean night musician whose body is a deep pear-shaped cobza, wrapped in a dark velvet coat with a high collar, plucking the strings across its chest under a low brim.</summary>
    private static PokeBuilder Cobzar()
    {
        var b = new PokeBuilder("Cobzar", 0.88f, BodyPlan.Biped, V(0, 0.7f, 0)) { Coat = Shell };
        var wood = Rgb(176, 110, 54);
        var dark = Rgb(128, 78, 40);
        var velvet = Rgb(58, 48, 96);
        var velvetDark = Rgb(40, 32, 66);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.5f, 0));
            b.Limb(leg, V(0.06f * s, 0.52f, 0), V(0.09f * s, 0.27f, 0.04f), 0.03f, 0.022f, dark);
            b.Limb(leg, V(0.09f * s, 0.27f, 0.04f), V(0.08f * s, 0.04f, 0.0f), 0.022f, 0.018f, dark);
            b.Ell(leg, V(0.085f * s, 0.022f, 0.045f), V(0.03f, 0.022f, 0.07f), dark);
        });
        // The lute it is, and the narrow neck that rises from it
        var c = V(0, 0.66f, 0.01f);
        var r = V(0.16f, 0.2f, 0.13f);
        b.Ell(Body, c, r, wood);
        b.Ell(Body, V(0, 0.86f, 0), V(0.075f, 0.1f, 0.07f), wood, blend: 0.06f);
        for (int i = -2; i <= 2; i++)
            b.PaintEll(Body, V(i * 0.055f, 0.6f, -0.11f), V(0.005f, 0.2f, 0.06f), dark, V(0, i * 14f, 0));
        C11Soundboard(b, Body, c, r, 0.63f, 0.2f, 5);
        // The velvet coat: a mantle over the shoulders, a long back and sides open at the front, a high collar
        b.Ell(Body, V(0, 0.9f, -0.03f), V(0.19f, 0.07f, 0.13f), velvet, mat: SurfaceMaterial.Cloth, blend: 0.03f);
        b.Ell(Body, V(0, 0.62f, -0.1f), V(0.22f, 0.32f, 0.08f), velvet, mat: SurfaceMaterial.Cloth, blend: 0.03f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.18f * s, 0.62f, -0.01f), V(0.055f, 0.3f, 0.12f), velvet, V(0, 0, 4f * s), SurfaceMaterial.Cloth, 0.03f));
        b.PaintEll(Body, V(0, 0.36f, -0.08f), V(0.3f, 0.06f, 0.2f), velvetDark);
        PokeBuilder.Both(s => Blade(b, Body, V(0.08f * s, 0.92f, -0.02f), V(0.13f * s, 1.08f, 0.0f), 0.055f, velvetDark, V(-0.2f * s, 0, 1f), 0.3f, SurfaceMaterial.Cloth));
        // The head under its low brim, eyes glinting in the shadow, the pegbox rising through the hat
        int head = b.Head(V(0, 0.94f, 0));
        var hc = V(0, 1.02f, 0.02f);
        var hr = V(0.06f, 0.08f, 0.06f);
        b.Ell(head, hc, hr, wood);
        b.PaintEll(head, hc + V(0, 0.02f, 0.04f), V(0.07f, 0.03f, 0.04f), Rgb(46, 34, 30));
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.024f * s, 1.03f);
            b.Eye(head, at, Outward(hc, hr, at), 0.013f, sclera: true, white: Rgb(226, 236, 190), pupil: Rgb(30, 30, 30), glare: true);
        });
        b.Ell(head, V(0, 1.09f, 0.02f), V(0.15f, 0.012f, 0.13f), dark, blend: 0.01f);
        b.Ell(head, V(0, 1.115f, 0.0f), V(0.065f, 0.035f, 0.06f), dark, blend: 0.02f);
        C11Pegbox(b, head, V(0, 1.12f, -0.01f), 0.2f, wood);
        // Long arms from under the coat, long fingers across the strings
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.86f, 0.02f));
            var elbow = V(0.2f * s, 0.7f, 0.1f);
            var hand = s < 0 ? V(-0.06f, 0.68f, 0.15f) : V(0.07f, 0.6f, 0.15f);
            b.Limb(arm, V(0.15f * s, 0.86f, 0.03f), elbow, 0.022f, 0.017f, dark);
            b.Limb(arm, elbow, hand, 0.017f, 0.015f, dark);
            b.Ell(arm, hand, V(0.022f, 0.02f, 0.016f), dark);
            Digits(b, arm, hand, V(-s, -0.4f, 0.4f), V(0, 0.35f, 0), 0.05f, 0.006f, dark);
        });
        return b;
    }
}
