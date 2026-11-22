using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 2 (plan 46 · CP4 to CP8): Ciobipup to Floarsol (20 species), sculpted from the briefs of
// docs/plans/decisions/CP-dex-1-plateau.md and CP-dex-1b-plateau-more.md and the user's reference pictures. Each line
// shares its palette and its motif (the sheepdogs' red-and-white cord, the sheep's bronze bell, the storks' nest, the
// sprites' knitted cap, the boletes' chestnut cap, the cats' red thread, the roosters' russet and green, the bees'
// amber bands, the hares' black-tipped ears, the sunflowers' petals) and grows in size and detail.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames2() => new[]
    {
        "Ciobipup", "Mioritar", "Lanita", "Turcanica", "Barzlet", "Barzanor", "Spiridel", "Spiriduke", "Hribling", "Hribarde",
        "Pisicut", "Motanor", "Cocosel", "Cocosor", "Albinut", "Stupara", "Iepurel", "Iepuroi", "Soarel", "Floarsol"
    };

    private static PokeBuilder? CreateCarpathia2(string species) => species switch
    {
        "CIOBIPUP" => Ciobipup(),
        "MIORITAR" => Mioritar(),
        "LANITA" => Lanita(),
        "TURCANICA" => Turcanica(),
        "BARZLET" => Barzlet(),
        "BARZANOR" => Barzanor(),
        "SPIRIDEL" => Spiridel(),
        "SPIRIDUKE" => Spiriduke(),
        "HRIBLING" => Hribling(),
        "HRIBARDE" => Hribarde(),
        "PISICUT" => Pisicut(),
        "MOTANOR" => Motanor(),
        "COCOSEL" => Cocosel(),
        "COCOSOR" => Cocosor(),
        "ALBINUT" => Albinut(),
        "STUPARA" => Stupara(),
        "IEPUREL" => Iepurel(),
        "IEPUROI" => Iepuroi(),
        "SOAREL" => Soarel(),
        "FLOARSOL" => Floarsol(),
        _ => null
    };

    // ------------------------------------------------------------------ shared motifs

    private static readonly Color Carp2CordRed = Rgb(204, 52, 56);
    private static readonly Color Carp2CordWhite = Rgb(246, 242, 236);
    private static readonly Color Carp2Bronze = Rgb(186, 136, 70);
    private static readonly Color Carp2Twig = Rgb(146, 116, 74);
    private static readonly Color Carp2TwigDark = Rgb(104, 82, 56);
    private static readonly Color Carp2Cap = Rgb(200, 66, 54);
    private static readonly Color Carp2Chestnut = Rgb(150, 82, 46);
    private static readonly Color Carp2Gills = Rgb(204, 206, 118);
    private static readonly Color Carp2Moss = Rgb(108, 150, 70);

    /// <summary>The point of a circle of radius <paramref name="ring"/> round the local Y axis, turned as a torus with <paramref name="turn"/> is.</summary>
    private static Vector3 Carp2Ring(Vector3 center, float ring, Vector3 turn, float angle, float sx = 1f, float sz = 1f)
    {
        var q = Quaternion.CreateFromYawPitchRoll(turn.Y * Degree, turn.X * Degree, turn.Z * Degree);
        return center + Vector3.Transform(V(MathF.Cos(angle) * ring * sx, 0, MathF.Sin(angle) * ring * sz), q);
    }

    /// <summary>The twisted red-and-white cord of the shepherds (a mărțișor's), a ring round the neck.</summary>
    private static void Carp2Cord(PokeBuilder b, int bone, Vector3 center, float ring, float tube, Vector3 turn)
    {
        b.Torus(bone, center, ring, tube, Carp2CordRed, turn, mat: SurfaceMaterial.Cloth);
        for (int i = 0; i < 10; i++)
            b.PaintEll(bone, Carp2Ring(center, ring, turn, i * MathF.Tau / 10f), V(tube * 1.4f, tube * 1.4f, tube * 1.4f), Carp2CordWhite, soft: tube * 0.6f);
    }

    /// <summary>A tassel hanging from <paramref name="top"/>: a knot and a few threads.</summary>
    private static void Carp2Tassel(PokeBuilder b, int bone, Vector3 top, float length, Color color, Color knot)
    {
        float r = length * 0.22f;
        b.Ell(bone, top, V(r, r, r), knot, mat: SurfaceMaterial.Cloth);
        foreach (float x in new[] { -0.5f, 0f, 0.5f })
            b.Limb(bone, top, top + V(x * r * 1.2f, -length, 0.004f), r * 0.6f, r * 0.45f, color, SurfaceMaterial.Cloth, 0.006f);
    }

    /// <summary>A sheep's bell hanging from <paramref name="top"/>: a flared dome and its clapper.</summary>
    private static void Carp2Bell(PokeBuilder b, int bone, Vector3 top, float r, Color color)
    {
        b.Limb(bone, top, top + V(0, -r * 1.3f, r * 0.15f), r * 0.5f, r, color, Metal, 0.006f);
        b.Ell(bone, top + V(0, -r * 1.45f, r * 0.15f), V(r * 0.35f, r * 0.3f, r * 0.35f), Rgb(90, 64, 40), mat: Metal, blend: 0.004f);
    }

    /// <summary>Tufts of wool over an ellipsoid: round locks spread by the golden angle, the front and the underside left bare.</summary>
    private static void Carp2Locks(PokeBuilder b, int bone, Vector3 c, Vector3 r, int count, float size, Color color, float face = 0.75f, float below = -0.35f)
    {
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < count; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / count;
            if (y < below) continue;
            float ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (n.Z > face) continue;
            b.Ell(bone, c + n * r * 0.95f, V(size, size * 0.85f, size), color, blend: 0.02f);
        }
    }

    /// <summary>A long wavy ringlet hanging from <paramref name="root"/> down to <paramref name="bottom"/>, swinging out along <paramref name="outward"/>.</summary>
    private static Vector3[] Carp2Ringlet(Vector3 root, float bottom, Vector3 outward, float wave)
    {
        var pts = new Vector3[6];
        for (int i = 0; i < pts.Length; i++)
        {
            float t = i / (float)(pts.Length - 1);
            var side = outward * (0.02f + t * 0.04f) + V(0, 0, MathF.Sin(t * 9f) * wave);
            pts[i] = V(root.X, root.Y + (bottom - root.Y) * t, root.Z) + side;
        }
        return pts;
    }

    /// <summary>Long thin bird's legs: a shank to each foot, three toes forward, one back, and a spur if asked.</summary>
    private static void Carp2BirdLegs(PokeBuilder b, float x, float hip, float z, float thick, Color color, bool spur = false)
    {
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(x * s, hip, z));
            var ankle = V(x * 1.1f * s, 0.035f, z + 0.01f);
            b.Limb(leg, V(x * s, hip, z), ankle, thick, thick * 0.85f, color, Scales, 0.008f);
            foreach (var (dx, dz) in new[] { (-0.5f, 1f), (0f, 1.15f), (0.5f, 1f), (0f, -0.75f) })
            {
                var toe = ankle + V(dx * thick * 2.2f * s, -0.02f, dz * thick * 3.2f);
                b.Limb(leg, ankle, toe, thick * 0.7f, thick * 0.5f, color, Scales, 0.006f);
            }
            if (spur) b.Spike(leg, ankle + V(0, 0.06f, -thick * 0.6f), ankle + V(0, 0.08f, -thick * 3.2f), thick * 0.6f, Rgb(232, 226, 200), mat: Shell, blend: 0.004f);
        });
    }

    /// <summary>A point on the face of a flower's disc: the disc at <paramref name="at"/> looking along <paramref name="n"/>, a heart of radius <paramref name="hr"/>, (x, y) across it.</summary>
    private static Vector3 Carp2Disc(Vector3 at, Vector3 n, float hr, float x, float y)
    {
        n = Vector3.Normalize(n);
        var u = V(1f, 0, 0);
        var w = Vector3.Normalize(Vector3.Cross(n, u));
        float k = MathF.Sqrt(MathF.Max(0f, 1f - (x * x + y * y) / (hr * hr)));
        return at + n * (hr * 0.2f + hr * 0.6f * k) + u * x + w * y;
    }

    // ------------------------------------------------------------------ Ciobipup line: the shepherd's dogs

    private static readonly Color Carp2DogWhite = Rgb(240, 234, 220);
    private static readonly Color Carp2DogCream = Rgb(226, 210, 176);
    private static readonly Color Carp2DogPatch = Rgb(70, 68, 74);

    /// <summary>Ciobipup: a fluffy shepherd's puppy in a playful bow, dark patches round its eyes, floppy ears, a big curled tail and a red-and-white cord.</summary>
    private static PokeBuilder Ciobipup()
    {
        var b = new PokeBuilder("Ciobipup", 0.52f, BodyPlan.Quadruped, V(0, 0.25f, -0.03f)) { Coat = Fur };
        var white = Carp2DogWhite;
        var cream = Carp2DogCream;

        // Front legs stretched out low in the bow, hind legs standing
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.17f, 0.1f));
            b.Limb(leg, V(0.07f * s, 0.17f, 0.1f), V(0.078f * s, 0.045f, 0.2f), 0.042f, 0.036f, white);
            b.Ell(leg, V(0.078f * s, 0.03f, 0.23f), V(0.044f, 0.03f, 0.052f), cream);
            int hind = b.Leg(s, V(0.08f * s, 0.26f, -0.14f), false);
            b.Limb(hind, V(0.08f * s, 0.26f, -0.14f), V(0.086f * s, 0.05f, -0.14f), 0.05f, 0.038f, white);
            b.Ell(hind, V(0.086f * s, 0.03f, -0.12f), V(0.042f, 0.03f, 0.05f), cream);
        });
        b.Ell(Body, V(0, 0.25f, -0.03f), V(0.12f, 0.12f, 0.19f), white, V(14f, 0, 0));
        b.Ell(Body, V(0, 0.2f, 0.1f), V(0.1f, 0.09f, 0.08f), white);
        Carp2Locks(b, Body, V(0, 0.26f, -0.05f), V(0.11f, 0.11f, 0.16f), 14, 0.035f, white, 0.6f, -0.1f);

        // The big tail curled up over its back like a snail's shell
        int tail = b.Tail(V(0, 0.33f, -0.19f));
        var curl = new[] { V(0, 0.32f, -0.2f), V(0, 0.45f, -0.25f), V(0, 0.55f, -0.19f), V(0, 0.54f, -0.09f), V(0, 0.47f, -0.08f), V(0, 0.45f, -0.14f) };
        b.Tube(tail, curl, 0.055f, 0.045f, white, blend: 0.03f);
        b.PaintEll(tail, V(0.04f, 0.48f, -0.13f), V(0.025f, 0.04f, 0.04f), cream);
        b.PaintEll(tail, V(-0.04f, 0.48f, -0.13f), V(0.025f, 0.04f, 0.04f), cream);

        int head = b.Head(V(0, 0.24f, 0.14f));
        var hc = V(0, 0.3f, 0.21f);
        var hr = V(0.11f, 0.1f, 0.1f);
        b.Ell(head, hc, hr, white);
        b.Ell(head, V(0, 0.265f, 0.29f), V(0.055f, 0.042f, 0.05f), white);
        foreach (var x in new[] { -0.05f, 0f, 0.05f })
            b.Ell(head, V(x, 0.39f, 0.2f), V(0.04f, 0.03f, 0.04f), white, blend: 0.02f);
        b.Ell(head, V(0, 0.287f, 0.336f), V(0.022f, 0.016f, 0.015f), Rgb(46, 42, 48), mat: Shell, blend: 0.008f);
        b.Mark(head, V(0, 0.247f, 0.326f), V(0, -0.3f, 1f), 0.024f, 0.012f, Rgb(200, 90, 100), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.046f * s, 0.32f);
            b.PaintEll(head, at, V(0.034f, 0.03f, 0.03f), Carp2DogPatch);
            int ear = b.Ear(head, s, V(0.09f * s, 0.36f, 0.18f));
            b.Ell(ear, V(0.115f * s, 0.3f, 0.18f), V(0.03f, 0.075f, 0.055f), cream, V(0, 0, 14f * s));
            b.Eye(head, at, Outward(hc, hr, at), 0.02f, Rgb(70, 48, 36));
        });

        // The cord round its neck and a red tassel at its throat
        Carp2Cord(b, Body, V(0, 0.215f, 0.15f), 0.083f, 0.013f, V(42f, 0, 0));
        Carp2Tassel(b, Body, V(0, 0.16f, 0.21f), 0.045f, Carp2CordRed, Carp2CordWhite);
        b.Ground();
        return b;
    }

    /// <summary>Mioritar: the great flock guardian, its coat in long white and grey cords, a spiked leather collar over the shepherd's cord.</summary>
    private static PokeBuilder Mioritar()
    {
        var b = new PokeBuilder("Mioritar", 0.9f, BodyPlan.Quadruped, V(0, 0.45f, -0.02f)) { Coat = Fur };
        var white = Rgb(232, 226, 212);
        var grey = Rgb(168, 164, 160);
        var leather = Rgb(86, 58, 44);
        BeastLegs(b, 0.12f, 0.32f, 0.2f, -0.22f, 0.07f, white, Rgb(150, 146, 140));

        var bc = V(0, 0.46f, -0.02f);
        var br = V(0.19f, 0.18f, 0.32f);
        b.Ell(Body, bc, br, white);
        // Cords of fleece hanging from its back and sides, white and grey
        int n = 0;
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { -0.26f, -0.16f, -0.06f, 0.04f, 0.14f })
                foreach (float phi in new[] { -20f, 25f, 65f })
                {
                    float k = MathF.Sqrt(MathF.Max(0.2f, 1f - MathF.Pow((z - bc.Z) / br.Z, 2f)));
                    float a = phi * Degree;
                    var root = V(s * br.X * k * MathF.Cos(a), bc.Y + br.Y * k * MathF.Sin(a), z);
                    var tip = root + V(s * 0.03f, -0.13f - (phi < 0 ? 0f : 0.04f), -0.01f);
                    b.Spike(Body, root, tip, 0.04f, (n++ % 3 == 1) ? grey : white, 0.7f, blend: 0.02f);
                }
        });
        foreach (float z in new[] { -0.22f, -0.08f, 0.06f })
            b.Spike(Body, V(0, bc.Y + br.Y * 0.92f, z), V(0, bc.Y + br.Y * 0.92f + 0.04f, z - 0.07f), 0.045f, white, 0.7f, blend: 0.02f);
        // A shaggy chest
        foreach (float x in new[] { -0.08f, 0f, 0.08f })
            b.Spike(Body, V(x, 0.46f, 0.26f), V(x * 1.1f, 0.3f, 0.3f), 0.045f, white, 0.7f, blend: 0.02f);

        // The plume of its tail, curled up over its rump
        int tail = b.Tail(V(0, 0.54f, -0.32f));
        b.Tube(tail, new[] { V(0, 0.54f, -0.32f), V(0, 0.72f, -0.4f), V(0, 0.84f, -0.32f), V(0, 0.8f, -0.21f), V(0, 0.71f, -0.22f) }, 0.07f, 0.05f, white, blend: 0.03f);
        b.PaintEll(tail, V(0, 0.84f, -0.32f), V(0.08f, 0.04f, 0.06f), grey);

        int head = b.Head(V(0, 0.58f, 0.26f));
        var hc = V(0, 0.67f, 0.34f);
        var hr = V(0.13f, 0.12f, 0.13f);
        b.Ell(head, hc, hr, white);
        b.Ell(head, V(0, 0.61f, 0.46f), V(0.07f, 0.062f, 0.08f), white);
        b.Ell(head, V(0, 0.645f, 0.535f), V(0.032f, 0.023f, 0.02f), Rgb(44, 40, 44), mat: Shell, blend: 0.008f);
        // A beard and jowls of long fur under its muzzle, and a shaggy fringe over its brow
        foreach (float x in new[] { -0.045f, 0f, 0.045f })
            b.Spike(head, V(x, 0.57f, 0.46f), V(x * 1.2f, 0.49f, 0.47f), 0.03f, white, 0.7f, blend: 0.015f);
        foreach (float x in new[] { -0.07f, 0f, 0.07f })
            b.Spike(head, V(x, 0.78f, 0.36f), V(x * 1.3f, 0.76f, 0.44f), 0.04f, white, 0.6f, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.055f * s, 0.69f);
            b.PaintEll(head, at, V(0.045f, 0.035f, 0.035f), Rgb(96, 94, 98));
            // Heavy brows over calm eyes
            b.Ell(head, at + V(0.004f * s, 0.035f, -0.01f), V(0.035f, 0.016f, 0.025f), grey, V(0, 0, -12f * s), blend: 0.01f);
            int ear = b.Ear(head, s, V(0.11f * s, 0.74f, 0.3f));
            b.Ell(ear, V(0.14f * s, 0.65f, 0.3f), V(0.035f, 0.09f, 0.06f), grey, V(0, 0, 10f * s));
            b.Eye(head, at, Outward(hc, hr, at), 0.022f, Rgb(96, 70, 52));
        });

        // The spiked leather collar against wolves, the shepherd's cord and its tassel under it
        var collar = V(0, 0.58f, 0.24f);
        var turn = V(55f, 0, 0);
        b.Torus(Body, collar, 0.135f, 0.028f, leather, turn, mat: SurfaceMaterial.Leather);
        for (int i = 0; i < 9; i++)
        {
            float a = MathF.PI * (0.05f + 0.9f * i / 8f);
            var at = Carp2Ring(collar, 0.155f, turn, a);
            var outward = Vector3.Normalize(at - collar);
            b.Spike(Body, at, at + outward * 0.05f, 0.016f, Rgb(176, 180, 190), mat: Metal, blend: 0.004f);
        }
        Carp2Cord(b, Body, collar + V(0, -0.03f, -0.02f), 0.14f, 0.013f, turn);
        Carp2Tassel(b, Body, Carp2Ring(collar + V(0, -0.03f, -0.02f), 0.14f, turn, MathF.PI / 2f) + V(0, -0.01f, 0), 0.06f, Carp2CordRed, Carp2CordWhite);
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Lanita line: the mountain sheep

    private static readonly Color Carp2Wool = Rgb(240, 230, 204);
    private static readonly Color Carp2WoolShade = Rgb(220, 206, 176);
    private static readonly Color Carp2SheepBlack = Rgb(50, 46, 50);
    private static readonly Color Carp2Hoof = Rgb(98, 86, 78);

    /// <summary>Lanita: a mountain lamb in long wavy wool, black face and legs, tiny curled horns and a bronze bell.</summary>
    private static PokeBuilder Lanita()
    {
        var b = new PokeBuilder("Lanita", 0.55f, BodyPlan.Quadruped, V(0, 0.28f, -0.01f)) { Coat = Fur };
        BeastLegs(b, 0.075f, 0.17f, 0.1f, -0.1f, 0.036f, Carp2SheepBlack, Carp2Hoof);
        var bc = V(0, 0.28f, -0.01f);
        var br = V(0.16f, 0.15f, 0.2f);
        b.Ell(Body, bc, br, Carp2Wool);
        Carp2Locks(b, Body, bc, br, 26, 0.05f, Carp2Wool, 0.7f, -0.55f);
        foreach (var (x, z) in new[] { (0.08f, -0.08f), (-0.1f, 0.04f), (0.12f, 0.06f), (-0.04f, -0.14f) })
            b.PaintEll(Body, V(x, 0.25f, z), V(0.06f, 0.05f, 0.06f), Carp2WoolShade);

        int head = b.Head(V(0, 0.32f, 0.15f));
        var fc = V(0, 0.33f, 0.24f);
        var fr = V(0.07f, 0.075f, 0.08f);
        b.Ell(head, fc, fr, Carp2SheepBlack);
        b.Ell(head, V(0, 0.3f, 0.3f), V(0.045f, 0.04f, 0.04f), Carp2SheepBlack);
        // A fringe of wool over its brow, hiding its eyes a little
        b.Ell(head, V(0, 0.395f, 0.21f), V(0.1f, 0.06f, 0.09f), Carp2Wool);
        foreach (float x in new[] { -0.045f, 0f, 0.045f })
            b.Ell(head, V(x, 0.375f, 0.27f), V(0.03f, 0.03f, 0.025f), Carp2Wool, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, 0.34f, 0.2f));
            b.Ell(ear, V(0.1f * s, 0.335f, 0.2f), V(0.045f, 0.018f, 0.026f), Carp2SheepBlack, V(0, 0, -15f * s));
            // Tiny curled horns
            b.Torus(head, V(0.075f * s, 0.385f, 0.17f), 0.022f, 0.011f, Rgb(92, 88, 90), V(0, 0, 90f), mat: Shell);
            var at = On(fc, fr, 0.03f * s, 0.338f);
            b.Eye(head, at, Outward(fc, fr, at), 0.016f, sclera: true, pupil: Rgb(40, 34, 30));
        });
        b.Mark(head, V(0, 0.288f, 0.338f), V(0, -0.2f, 1f), 0.016f, 0.008f, Rgb(126, 110, 110), MarkShape.Smile);

        b.Torus(Body, V(0, 0.28f, 0.16f), 0.07f, 0.012f, Rgb(92, 66, 46), V(50f, 0, 0), mat: SurfaceMaterial.Leather);
        Carp2Bell(b, Body, V(0, 0.23f, 0.22f), 0.03f, Carp2Bronze);
        b.Ground();
        return b;
    }

    /// <summary>Turcanica: a tall ewe of the long-wool breed, ringlets to the ground, embroidered ribbons, spiralled horns and a bigger bell.</summary>
    private static PokeBuilder Turcanica()
    {
        var b = new PokeBuilder("Turcanica", 0.9f, BodyPlan.Quadruped, V(0, 0.5f, -0.02f)) { Coat = Fur };
        var red = Rgb(170, 40, 48);
        var ink = Rgb(44, 30, 34);
        BeastLegs(b, 0.11f, 0.32f, 0.18f, -0.2f, 0.05f, Carp2SheepBlack, Carp2Hoof);
        var bc = V(0, 0.5f, -0.02f);
        var br = V(0.2f, 0.19f, 0.3f);
        b.Ell(Body, bc, br, Carp2Wool);
        Carp2Locks(b, Body, bc, br, 30, 0.055f, Carp2Wool, 0.8f, -0.2f);

        // Ringlets hanging nearly to the ground, the tips of some glowing softly
        int n = 0;
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { -0.26f, -0.15f, -0.04f, 0.07f, 0.17f })
                foreach (float phi in new[] { -10f, 30f })
                {
                    float k = MathF.Sqrt(MathF.Max(0.2f, 1f - MathF.Pow((z - bc.Z) / br.Z, 2f)));
                    float a = phi * Degree;
                    var root = V(s * br.X * k * MathF.Cos(a), bc.Y + br.Y * k * MathF.Sin(a), z);
                    var pts = Carp2Ringlet(root, phi < 0 ? 0.08f : 0.14f, V(s, 0, 0), 0.012f);
                    b.Tube(Body, pts, 0.028f, 0.02f, Carp2Wool, blend: 0.015f);
                    if (n++ % 3 == 0) b.Ell(Body, pts[^1], V(0.022f, 0.022f, 0.022f), Rgb(252, 240, 200), mat: Glow, blend: 0.01f);
                }
        });
        foreach (var (x, z) in new[] { (0f, -0.29f), (-0.08f, -0.25f), (0.08f, -0.25f) })
            b.Tube(Body, Carp2Ringlet(V(x, bc.Y - 0.05f, z), 0.12f, V(0, 0, -1f), 0.01f), 0.028f, 0.02f, Carp2Wool, blend: 0.015f);

        // Embroidered ribbons round its fleece, their ends hanging in tassels
        foreach (float z in new[] { 0.1f, -0.1f, -0.26f })
        {
            float k = MathF.Sqrt(MathF.Max(0.2f, 1f - MathF.Pow((z - bc.Z) / br.Z, 2f)));
            var c = V(0, bc.Y, z);
            b.PaintTorus(Body, c, br.Y * k * 1.12f, 0.032f, red, V(90f, 0, 0), 1.08f, 1f);
            b.PaintTorus(Body, c, br.Y * k * 1.12f, 0.009f, ink, V(90f, 0, 0), 1.08f, 1f);
            PokeBuilder.Both(s =>
            {
                var top = V(s * br.X * k * 0.96f, bc.Y - br.Y * k * 0.35f, z);
                var end = top + V(s * 0.02f, -0.14f, 0);
                b.Limb(Body, top, end, 0.018f, 0.016f, red, SurfaceMaterial.Cloth, 0.008f);
                Carp2Tassel(b, Body, end, 0.05f, red, ink);
            });
        }

        int head = b.Head(V(0, 0.6f, 0.27f));
        b.Ell(head, V(0, 0.62f, 0.28f), V(0.13f, 0.14f, 0.12f), Carp2Wool);
        Carp2Locks(b, head, V(0, 0.64f, 0.28f), V(0.12f, 0.13f, 0.11f), 12, 0.04f, Carp2Wool, 0.6f, -0.3f);
        var fc = V(0, 0.72f, 0.39f);
        var fr = V(0.07f, 0.1f, 0.09f);
        b.Ell(head, fc, fr, Carp2SheepBlack, V(20f, 0, 0));
        b.Ell(head, V(0, 0.8f, 0.34f), V(0.1f, 0.06f, 0.09f), Carp2Wool);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.75f, 0.35f));
            b.Ell(ear, V(0.11f * s, 0.73f, 0.34f), V(0.055f, 0.02f, 0.03f), Carp2SheepBlack, V(0, 0, -18f * s));
            // Long horns spiralling back and down beside its face
            var root = V(0.06f * s, 0.81f, 0.31f);
            var pts = new Vector3[9];
            for (int i = 0; i < pts.Length; i++)
            {
                float t = i / (float)(pts.Length - 1), a = t * 1.7f * MathF.PI, rr = 0.075f - 0.03f * t;
                pts[i] = root + V(0.07f * s * t + 0.02f * s, MathF.Cos(a) * rr - 0.075f, -MathF.Sin(a) * rr);
            }
            b.Tube(head, pts, 0.028f, 0.012f, Rgb(74, 70, 70), Shell, 0.012f);
            var at = On(fc, fr, 0.034f * s, 0.74f);
            b.Eye(head, at, Outward(fc, fr, at) + V(0, 0.2f, 0), 0.017f, sclera: true, pupil: Rgb(60, 40, 30));
        });
        b.Mark(head, V(0, 0.65f, 0.465f), V(0, -0.5f, 1f), 0.016f, 0.008f, Rgb(126, 110, 110), MarkShape.Smile);

        // A bigger bell on an embroidered band at its throat
        b.Torus(Body, V(0, 0.6f, 0.32f), 0.11f, 0.02f, red, V(50f, 0, 0), mat: SurfaceMaterial.Cloth);
        Carp2Bell(b, Body, V(0, 0.52f, 0.42f), 0.045f, Carp2Bronze);
        PokeBuilder.Both(s => Carp2Tassel(b, Body, V(0.06f * s, 0.52f, 0.4f), 0.06f, red, ink));
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Barzlet line: the storks

    private static readonly Color Carp2StorkWhite = Rgb(244, 244, 246);
    private static readonly Color Carp2StorkBlack = Rgb(40, 40, 46);

    /// <summary>A nest of twigs round <paramref name="center"/>: a ring of twigs, a floor and sticks crossing it.</summary>
    private static void Carp2Nest(PokeBuilder b, int bone, Vector3 center, float ring, float tube, int sticks, float stick)
    {
        b.Torus(bone, center, ring, tube, Carp2Twig, blend: 0.01f);
        b.Ell(bone, center + V(0, -tube * 0.6f, 0), V(ring, tube * 0.8f, ring), Carp2TwigDark);
        for (int i = 0; i < sticks; i++)
        {
            float a0 = i * MathF.Tau / sticks, a1 = a0 + 1.1f;
            float y0 = (i % 2 == 0 ? 0.4f : -0.4f) * tube, y1 = -y0;
            var p0 = center + V(MathF.Cos(a0) * (ring + tube * 0.75f), y0, MathF.Sin(a0) * (ring + tube * 0.75f));
            var p1 = center + V(MathF.Cos(a1) * (ring + tube * 0.75f), y1, MathF.Sin(a1) * (ring + tube * 0.75f));
            b.Limb(bone, p0, p1, stick, stick, i % 3 == 0 ? Carp2TwigDark : Carp2Twig, Shell, 0.004f);
        }
    }

    /// <summary>Barzlet: a gangly stork chick, white with grey down and black wingtips, sitting in the nest it carries on long pink legs.</summary>
    private static PokeBuilder Barzlet()
    {
        var b = new PokeBuilder("Barzlet", 0.56f, BodyPlan.Bird, V(0, 0.44f, 0)) { Coat = Fur };
        var grey = Rgb(170, 172, 180);
        Carp2BirdLegs(b, 0.04f, 0.36f, 0.01f, 0.015f, Rgb(236, 164, 164));
        Carp2Nest(b, Body, V(0, 0.38f, 0), 0.13f, 0.045f, 10, 0.011f);
        var bc = V(0, 0.45f, -0.01f);
        b.Ell(Body, bc, V(0.11f, 0.09f, 0.13f), Carp2StorkWhite);
        b.PaintEll(Body, V(0, 0.5f, -0.06f), V(0.08f, 0.04f, 0.08f), grey);
        b.PaintEll(Body, V(0, 0.42f, 0.1f), V(0.06f, 0.05f, 0.04f), grey);
        PokeBuilder.Both(s => FoldedWing(b, s, V(0.09f * s, 0.48f, 0.04f), V(0.12f * s, 0.43f, -0.14f), 0.05f, Carp2StorkWhite, Carp2StorkBlack));

        int head = b.Head(V(0, 0.5f, 0.06f));
        b.Limb(head, V(0, 0.48f, 0.05f), V(0, 0.6f, 0.08f), 0.042f, 0.036f, Carp2StorkWhite);
        var hc = V(0, 0.65f, 0.09f);
        var hr = V(0.065f, 0.065f, 0.07f);
        b.Ell(head, hc, hr, Carp2StorkWhite);
        b.PaintEll(head, V(0, 0.69f, 0.06f), V(0.075f, 0.04f, 0.075f), grey);
        // Wisps of down on its crown
        foreach (var (x, z) in new[] { (-0.02f, 0.06f), (0.015f, 0.08f), (0f, 0.04f) })
            b.Spike(head, V(x, 0.7f, z), V(x * 2f, 0.75f, z - 0.02f), 0.012f, grey, blend: 0.008f);
        b.Spike(head, V(0, 0.64f, 0.15f), V(0, 0.62f, 0.25f), 0.024f, Rgb(244, 150, 112), 0.8f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.75f * s, 0.15f, 0.65f));
            b.Eye(head, at, at - hc, 0.016f, Rgb(50, 40, 36));
        });
        b.Ground();
        return b;
    }

    /// <summary>Barzanor: the great white stork on one red leg, black flight feathers in its half-spread wings, a nest for a crown and its long red beak raised to clatter.</summary>
    private static PokeBuilder Barzanor()
    {
        var b = new PokeBuilder("Barzanor", 0.95f, BodyPlan.Bird, V(0, 0.62f, -0.02f)) { Coat = Fur };
        var red = Rgb(214, 62, 54);
        var greyWhite = Rgb(214, 216, 224);

        // One long red leg standing, the other drawn up
        int stand = b.Leg(-1f, V(-0.05f, 0.54f, 0f));
        b.Limb(stand, V(-0.05f, 0.54f, 0f), V(-0.055f, 0.035f, 0.01f), 0.019f, 0.016f, red, Scales, 0.008f);
        foreach (var (dx, dz) in new[] { (-0.5f, 1f), (0f, 1.15f), (0.5f, 1f), (0f, -0.75f) })
            b.Limb(stand, V(-0.055f, 0.035f, 0.01f), V(-0.055f + dx * 0.04f, 0.012f, 0.01f + dz * 0.055f), 0.013f, 0.009f, red, Scales, 0.006f);
        int raised = b.Leg(1f, V(0.05f, 0.54f, 0f));
        b.Limb(raised, V(0.05f, 0.54f, 0f), V(0.07f, 0.37f, 0.1f), 0.019f, 0.017f, red, Scales, 0.008f);
        b.Limb(raised, V(0.07f, 0.37f, 0.1f), V(0.075f, 0.24f, 0.01f), 0.017f, 0.015f, red, Scales, 0.008f);
        foreach (float dx in new[] { -0.02f, 0f, 0.02f })
            b.Limb(raised, V(0.075f, 0.24f, 0.01f), V(0.075f + dx, 0.19f, 0.04f), 0.011f, 0.008f, red, Scales, 0.006f);

        var bc = V(0, 0.64f, -0.02f);
        b.Ell(Body, bc, V(0.12f, 0.11f, 0.2f), Carp2StorkWhite, V(-12f, 0, 0));
        PokeBuilder.Both(s => b.Ell(Body, V(0.05f * s, 0.56f, 0f), V(0.05f, 0.06f, 0.06f), Carp2StorkWhite));
        int tail = b.Tail(V(0, 0.6f, -0.2f));
        b.Ell(tail, V(0, 0.58f, -0.28f), V(0.07f, 0.03f, 0.1f), Carp2StorkWhite, V(20f, 0, 0));

        // Wings half spread: white coverts over long black flight feathers
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.1f * s, 0.7f, 0.02f);
            var wrist = V(0.38f * s, 0.82f, -0.02f);
            int w = b.Wing(s, shoulder);
            for (int i = 0; i < 6; i++)
            {
                float t = 0.25f + 0.15f * i;
                var root = Vector3.Lerp(shoulder, wrist, t);
                var tip = root + V(s * (0.04f + 0.03f * i), -0.2f - 0.03f * i, -0.03f);
                Frond(b, w, root, tip, 0.045f, i < 2 ? greyWhite : Carp2StorkBlack, V(0, 0, 1f), 0.25f, Fur, 0.01f);
            }
            var d = wrist - shoulder;
            b.Ell(w, shoulder + d * 0.55f + V(0, -0.02f, 0), V(0.06f, d.Length() * 0.58f, 0.03f), Carp2StorkWhite, Euler(d), blend: 0.015f);
        });

        int head = b.Head(V(0, 0.72f, 0.12f));
        b.Tube(head, new[] { V(0, 0.68f, 0.1f), V(0, 0.82f, 0.16f), V(0, 0.95f, 0.15f) }, 0.05f, 0.036f, Carp2StorkWhite, blend: 0.02f);
        var hc = V(0, 1f, 0.16f);
        var hr = V(0.058f, 0.055f, 0.07f);
        b.Ell(head, hc, hr, Carp2StorkWhite);
        b.Spike(head, V(0, 1.0f, 0.215f), V(0, 1.09f, 0.4f), 0.024f, red, 0.85f, Shell);
        int jaw = b.Jaw(head, V(0, 0.99f, 0.21f));
        b.Spike(jaw, V(0, 0.985f, 0.21f), V(0, 1.035f, 0.38f), 0.018f, red, 0.85f, Shell);
        // A crown of woven twigs, like a nest on a chimney
        Carp2Nest(b, head, V(0, 1.05f, 0.14f), 0.055f, 0.024f, 7, 0.009f);
        foreach (float a in new[] { 0.4f, 2.2f, 4.1f })
            b.Spike(head, V(MathF.Cos(a) * 0.07f, 1.06f, 0.14f + MathF.Sin(a) * 0.07f), V(MathF.Cos(a) * 0.11f, 1.09f, 0.14f + MathF.Sin(a) * 0.11f), 0.012f, Rgb(118, 160, 84), 0.4f, Leaf);
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.75f * s, 0.15f, 0.62f));
            b.Eye(head, at, at - hc, 0.014f, Rgb(46, 40, 40));
        });
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Spiridel line: the house sprites

    private static readonly Color Carp2SpriteSkin = Rgb(204, 178, 132);
    private static readonly Color Carp2SpriteEye = Rgb(70, 220, 140);

    /// <summary>A knitted cap on a sprite's head: a crown, a ribbed brim and a long tail flopping back and to its side.</summary>
    private static void Carp2KnitCap(PokeBuilder b, int head, Vector3 crown, float r, Vector3[] flop, float flopR)
    {
        var rib = PixelCanvas.Mix(Carp2Cap, Black, 0.18f);
        b.Ell(head, crown, V(r * 1.05f, r * 0.62f, r * 0.98f), Carp2Cap, mat: SurfaceMaterial.Cloth);
        b.Torus(head, crown + V(0, -r * 0.3f, 0.005f), r * 0.98f, r * 0.2f, rib, mat: SurfaceMaterial.Cloth);
        for (int i = 0; i < 12; i++)
            b.PaintEll(head, Carp2Ring(crown + V(0, -r * 0.3f, 0.005f), r * 1.12f, default, i * MathF.Tau / 12f), V(r * 0.07f, r * 0.3f, r * 0.07f), Carp2Cap, soft: r * 0.05f);
        b.Tube(head, flop, flopR, flopR * 0.45f, Carp2Cap, SurfaceMaterial.Cloth, 0.02f);
    }

    /// <summary>Spiridel: a tiny house sprite in a floppy red knitted cap, big pointed ears and a long nose, clutching a wooden spoon bigger than itself, a candle-flame wisp for a tail.</summary>
    private static PokeBuilder Spiridel()
    {
        var b = new PokeBuilder("Spiridel", 0.5f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = SurfaceMaterial.Skin };
        var skin = Carp2SpriteSkin;
        var belly = Rgb(196, 190, 140);
        var wood = Rgb(170, 124, 76);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.09f, 0));
            b.Limb(leg, V(0.05f * s, 0.09f, 0), V(0.055f * s, 0.03f, 0.01f), 0.032f, 0.028f, skin);
            b.Ell(leg, V(0.058f * s, 0.02f, 0.03f), V(0.034f, 0.02f, 0.045f), skin);
        });
        var bc = V(0, 0.17f, 0);
        var br = V(0.11f, 0.115f, 0.1f);
        b.Ell(Body, bc, br, skin);
        b.PaintEll(Body, V(0, 0.15f, 0.06f), V(0.075f, 0.08f, 0.06f), belly);

        // A thin tail with a candle's flame at its tip
        int tail = b.Tail(V(0, 0.1f, -0.08f));
        var tip = V(-0.12f, 0.19f, -0.16f);
        b.Tube(tail, new[] { V(0, 0.1f, -0.08f), V(-0.05f, 0.09f, -0.14f), V(-0.1f, 0.13f, -0.16f), tip }, 0.014f, 0.011f, skin, blend: 0.008f);
        Flame(b, tail, tip + V(0, -0.012f, 0), 0.14f);

        // The wooden spoon held across its body in both hands, its bowl up by its right shoulder
        var a = V(-0.17f, 0.04f, 0.135f);
        var bTop = V(0.15f, 0.3f, 0.135f);
        var d = Vector3.Normalize(bTop - a);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.22f, 0.02f));
            var hand = Vector3.Lerp(a, bTop, s < 0 ? 0.35f : 0.68f);
            b.Limb(arm, V(0.09f * s, 0.22f, 0.02f), hand + V(0.01f * s, 0.01f, -0.02f), 0.026f, 0.022f, skin);
            b.Ell(arm, hand, V(0.028f, 0.028f, 0.026f), skin);
            if (s > 0)
            {
                b.Limb(arm, a, bTop, 0.014f, 0.016f, wood, Shell, 0.004f);
                var bowl = bTop + d * 0.06f;
                b.Ell(arm, bowl, V(0.045f, 0.065f, 0.02f), wood, Euler(d), Shell, 0.006f);
                b.PaintEll(arm, bowl + V(0, 0, 0.016f), V(0.034f, 0.05f, 0.01f), PixelCanvas.Mix(wood, Black, 0.25f), Euler(d));
            }
        });

        int head = b.Head(V(0, 0.27f, 0.01f));
        var hc = V(0, 0.36f, 0.01f);
        var hr = V(0.12f, 0.11f, 0.105f);
        b.Ell(head, hc, hr, skin);
        b.Spike(head, V(0, 0.34f, 0.1f), V(0.015f, 0.3f, 0.2f), 0.024f, Rgb(214, 160, 132), blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.1f * s, 0.38f, 0));
            b.Spike(ear, V(0.1f * s, 0.38f, 0), V(0.27f * s, 0.45f, -0.02f), 0.042f, skin, 0.35f);
            b.PaintEll(ear, V(0.16f * s, 0.405f, 0.004f), V(0.05f, 0.022f, 0.02f), Rgb(226, 164, 150), V(0, 0, 20f * s));
            var at = On(hc, hr, 0.045f * s, 0.365f);
            b.Eye(head, at, Outward(hc, hr, at), 0.022f, Carp2SpriteEye, glare: true);
            b.PaintEll(head, On(hc, hr, 0.075f * s, 0.325f), V(0.022f, 0.012f, 0.015f), Rgb(222, 150, 132));
        });
        b.Mark(head, On(hc, hr, 0, 0.29f), V(0, -0.3f, 1f), 0.04f, 0.014f, Rgb(110, 70, 60), MarkShape.Smile);
        Carp2KnitCap(b, head, V(0, 0.44f, -0.005f), 0.12f,
            new[] { V(0, 0.48f, -0.02f), V(-0.07f, 0.53f, -0.06f), V(-0.16f, 0.48f, -0.08f), V(-0.21f, 0.39f, -0.06f) }, 0.06f);
        b.Ground();
        return b;
    }

    /// <summary>Spiriduke: the old house sprite of the hearth, floating on a wisp of smoke, a tall capped bell, an embroidered waistcoat, a ring of iron keys and a clay lamp lighting its grin.</summary>
    private static PokeBuilder Spiriduke()
    {
        var b = new PokeBuilder("Spiriduke", 0.74f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = SurfaceMaterial.Skin }.Hover();
        var skin = Rgb(196, 168, 124);
        var vest = Rgb(150, 44, 46);
        var gold = Rgb(222, 178, 82);
        var smoke = Rgb(150, 142, 142);
        var iron = Rgb(96, 84, 78);
        var clay = Rgb(176, 104, 70);

        var bc = V(0, 0.42f, 0);
        var br = V(0.1f, 0.13f, 0.085f);
        b.Ell(Body, bc, br, skin);
        // The waistcoat, open down its front, its hem trimmed in gold
        b.PaintEll(Body, bc + V(0, 0.01f, 0), V(0.115f, 0.135f, 0.1f), vest);
        b.PaintEll(Body, V(0, 0.42f, 0.085f), V(0.035f, 0.15f, 0.035f), skin);
        b.PaintTorus(Body, V(0, 0.33f, 0), 0.085f, 0.008f, gold, default, 1f, 0.85f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.045f * s, 0.44f, 0.075f), V(0.008f, 0.1f, 0.02f), gold));
        // Its lower body fades into a curl of hearth smoke
        int tail = b.Tail(V(0, 0.32f, 0));
        b.Tube(tail, new[] { V(0, 0.33f, 0), V(0, 0.25f, 0.01f), V(0.03f, 0.17f, -0.02f), V(-0.02f, 0.1f, -0.05f), V(0.025f, 0.05f, -0.08f) }, 0.08f, 0.022f, smoke, blend: 0.03f);
        b.PaintEll(tail, V(0, 0.31f, 0.0f), V(0.09f, 0.03f, 0.09f), PixelCanvas.Mix(skin, smoke, 0.5f));

        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.5f, 0));
            if (s > 0)
            {
                // A small clay oil lamp held out in its right hand, its flame lighting its face
                b.Limb(arm, V(0.09f, 0.5f, 0), V(0.17f, 0.45f, 0.06f), 0.026f, 0.022f, skin);
                b.Limb(arm, V(0.17f, 0.45f, 0.06f), V(0.23f, 0.48f, 0.13f), 0.022f, 0.02f, skin);
                b.Ell(arm, V(0.24f, 0.48f, 0.14f), V(0.025f, 0.025f, 0.025f), skin);
                b.Ell(arm, V(0.29f, 0.465f, 0.15f), V(0.05f, 0.024f, 0.036f), clay, mat: Shell);
                b.Spike(arm, V(0.31f, 0.47f, 0.15f), V(0.36f, 0.485f, 0.16f), 0.016f, clay, mat: Shell, blend: 0.006f);
                Flame(b, arm, V(0.355f, 0.475f, 0.16f), 0.12f);
            }
            else
            {
                // A ring of old iron keys hanging from its left hand
                b.Limb(arm, V(-0.09f, 0.5f, 0), V(-0.16f, 0.41f, 0.03f), 0.026f, 0.022f, skin);
                b.Limb(arm, V(-0.16f, 0.41f, 0.03f), V(-0.18f, 0.31f, 0.06f), 0.022f, 0.02f, skin);
                b.Ell(arm, V(-0.18f, 0.3f, 0.06f), V(0.026f, 0.026f, 0.026f), skin);
                b.Torus(arm, V(-0.185f, 0.255f, 0.06f), 0.034f, 0.009f, iron, V(90f, 0, 0), mat: Metal);
                foreach (var (x, len) in new[] { (-0.02f, 0.08f), (0f, 0.1f), (0.02f, 0.075f) })
                {
                    var top = V(-0.185f + x, 0.222f, 0.06f);
                    var end = top + V(x * 0.5f, -len, 0.004f);
                    b.Limb(arm, top, end, 0.009f, 0.009f, iron, Metal, 0.004f);
                    b.Box(arm, end + V(0.008f, 0.004f, 0), V(0.01f, 0.006f, 0.005f), 0.002f, iron, mat: Metal, blend: 0.003f);
                }
            }
        });

        int head = b.Head(V(0, 0.55f, 0));
        var hc = V(0, 0.64f, 0.01f);
        var hr = V(0.12f, 0.11f, 0.105f);
        b.Ell(head, hc, hr, skin);
        b.Spike(head, V(0, 0.62f, 0.1f), V(0.02f, 0.56f, 0.21f), 0.026f, Rgb(206, 150, 120), blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.1f * s, 0.66f, 0));
            b.Spike(ear, V(0.1f * s, 0.66f, 0), V(0.3f * s, 0.75f, -0.03f), 0.045f, skin, 0.35f);
            b.PaintEll(ear, V(0.17f * s, 0.69f, 0.004f), V(0.06f, 0.024f, 0.02f), Rgb(214, 150, 136), V(0, 0, 20f * s));
            var at = On(hc, hr, 0.046f * s, 0.65f);
            b.Eye(head, at, Outward(hc, hr, at), 0.022f, Carp2SpriteEye, glare: true);
            // Wrinkles of an old sprite over its brow
            b.PaintEll(head, On(hc, hr, 0.035f * s, 0.7f), V(0.03f, 0.006f, 0.02f), PixelCanvas.Mix(skin, Black, 0.2f), V(0, 0, -12f * s));
        });
        b.Mark(head, On(hc, hr, 0, 0.57f), V(0, -0.3f, 1f), 0.05f, 0.016f, Rgb(100, 62, 54), MarkShape.Smile);
        Carp2KnitCap(b, head, V(0, 0.72f, -0.01f), 0.125f,
            new[] { V(0, 0.76f, -0.02f), V(-0.02f, 0.87f, -0.06f), V(-0.11f, 0.9f, -0.1f), V(-0.18f, 0.81f, -0.1f), V(-0.2f, 0.71f, -0.08f) }, 0.072f);
        b.Ell(head, V(-0.2f, 0.69f, -0.08f), V(0.024f, 0.024f, 0.024f), gold, mat: Metal, blend: 0.006f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Hribling line: the boletes

    private static readonly Color Carp2Stem = Rgb(236, 224, 194);

    /// <summary>A bolete's cap: a dome cut flat underneath and a pale gilled underside. It must come first, since its cut carves what is there already.</summary>
    private static void Carp2BoleteCap(PokeBuilder b, int bone, Vector3 c, Vector3 r, float under)
    {
        b.Ell(bone, c, r, Carp2Chestnut, mat: Shell);
        b.CutBox(bone, V(c.X, c.Y - r.Y - 0.5f + under, c.Z), V(r.X * 1.5f, 0.5f, r.Z * 1.5f), Quaternion.Identity);
        b.Ell(bone, V(c.X, c.Y - (r.Y - under) - 0.005f, c.Z), V(r.X * 0.97f, r.Y * 0.22f, r.Z * 0.97f), Carp2Gills, mat: Leaf, blend: 0.01f);
    }

    /// <summary>A few tufts of moss on a surface.</summary>
    private static void Carp2MossOn(PokeBuilder b, int bone, Vector3 c, Vector3 r, float size, params Vector3[] dirs)
    {
        foreach (var dir in dirs)
        {
            var at = Out(c, r, default, dir);
            b.Ell(bone, at, V(size, size * 0.9f, size), Carp2Moss, mat: Leaf, blend: 0.015f);
            b.PaintEll(bone, at, V(size * 1.8f, size * 1.6f, size * 1.8f), Carp2Moss, soft: size * 0.4f);
        }
    }

    /// <summary>Hribling: a small fat bolete, a glossy chestnut cap over a stout cream stem, sleepy eyes under the brim and moss at its base.</summary>
    private static PokeBuilder Hribling()
    {
        var b = new PokeBuilder("Hribling", 0.5f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = SurfaceMaterial.Skin };
        int head = b.Head(V(0, 0.27f, 0));
        Carp2BoleteCap(b, head, V(0, 0.36f, 0.005f), V(0.2f, 0.12f, 0.18f), 0.04f);
        b.PaintEll(head, V(-0.08f, 0.45f, 0.08f), V(0.05f, 0.02f, 0.03f), PixelCanvas.Light1(Carp2Chestnut, 0.3f), V(0, 0, 15f));

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.06f, 0));
            b.Ell(leg, V(0.06f * s, 0.028f, 0.02f), V(0.04f, 0.028f, 0.045f), Carp2Stem);
        });
        var bc = V(0, 0.16f, 0);
        var br = V(0.115f, 0.125f, 0.105f);
        b.Ell(Body, bc, br, Carp2Stem);
        // The cap's shadow over its brow
        b.PaintEll(Body, V(0, 0.262f, 0.05f), V(0.12f, 0.022f, 0.08f), Rgb(120, 96, 76));
        Carp2MossOn(b, Body, bc, br, 0.018f, V(0.9f, -0.4f, 0.3f), V(-0.9f, -0.3f, 0.35f), V(0.7f, -0.2f, -0.6f));
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.038f * s, 0.21f);
            b.Eye(Body, at, Outward(bc, br, at), 0.016f, Rgb(60, 50, 44), sclera: true, pupil: Rgb(50, 42, 38));
        });
        b.Ground();
        return b;
    }

    /// <summary>Hribarde: the old forest bolete, a cracked cap as wide as a table with ferns and small mushrooms on its brim, a white beard of threads and roots splayed for feet.</summary>
    private static PokeBuilder Hribarde()
    {
        var b = new PokeBuilder("Hribarde", 0.95f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = SurfaceMaterial.Skin };
        var beard = Rgb(226, 226, 222);
        var brow = Rgb(176, 176, 172);
        var fern = Rgb(96, 150, 72);

        int head = b.Head(V(0, 0.64f, 0));
        var cc = V(0, 0.83f, 0);
        var cr = V(0.5f, 0.2f, 0.44f);
        Carp2BoleteCap(b, head, cc, cr, 0.06f);
        // Cracks across its cap
        foreach (var (x, z, roll) in new[] { (-0.18f, 0.12f, 30f), (0.16f, 0.08f, -40f), (0.02f, 0.24f, 80f) })
        {
            var dir = V(x / cr.X, 0.7f, z / cr.Z);
            b.Mark(head, Out(cc, cr, default, dir), dir, 0.06f, 0.02f, Rgb(96, 50, 30), MarkShape.Zigzag, roll);
        }
        // Small mushrooms growing on its brim
        foreach (var (x, z, k) in new[] { (0.3f, -0.05f, 1f), (0.36f, 0.06f, 0.7f), (-0.28f, -0.12f, 0.85f), (0.1f, -0.25f, 0.75f) })
        {
            var at = Out(cc, cr, default, V(x / cr.X, 0.6f, z / cr.Z));
            b.Limb(head, at - V(0, 0.01f, 0), at + V(0, 0.06f * k, 0), 0.012f * k, 0.01f * k, Carp2Stem, blend: 0.006f);
            b.Ell(head, at + V(0, 0.07f * k, 0), V(0.035f * k, 0.018f * k, 0.035f * k), Carp2Chestnut, mat: Shell, blend: 0.006f);
        }
        // Two ferns on its brim
        foreach (var (x, z, lean) in new[] { (-0.3f, 0.05f, -1f), (0.22f, -0.18f, 1f) })
        {
            var root = Out(cc, cr, default, V(x / cr.X, 0.55f, z / cr.Z)) - V(0, 0.01f, 0);
            var tip = root + V(0.06f * lean, 0.22f, -0.02f);
            b.Limb(head, root, tip, 0.01f, 0.007f, fern, Leaf, 0.006f);
            for (int i = 1; i <= 4; i++)
            {
                var on = Vector3.Lerp(root, tip, i / 5f);
                float len = 0.07f * (1f - i / 6f);
                PokeBuilder.Both(s => Frond(b, head, on, on + V(s * len, len * 0.3f, 0), 0.016f, fern, V(0, 0, 1f), 0.25f, Leaf, 0.004f));
            }
        }
        b.Ell(head, Out(cc, cr, default, V(-0.6f, 0.6f, 0.1f)), V(0.06f, 0.025f, 0.05f), Carp2Moss, mat: Leaf, blend: 0.015f);

        // Roots splayed out for feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.14f * s, 0.14f, 0));
            foreach (var (dx, dz) in new[] { (1f, 0.5f), (0.7f, -0.6f), (0.25f, 1f) })
                b.Limb(leg, V(0.12f * s, 0.16f, 0.02f), V((0.14f + 0.2f * dx) * s, 0.025f, 0.2f * dz), 0.07f, 0.025f, Carp2Stem, blend: 0.02f);
        });
        var bc = V(0, 0.38f, 0);
        var br = V(0.25f, 0.3f, 0.22f);
        b.Ell(Body, bc, br, Carp2Stem);
        b.Limb(Body, V(0, 0.15f, -0.1f), V(0, 0.025f, -0.3f), 0.07f, 0.025f, Carp2Stem, blend: 0.02f);
        Carp2MossOn(b, Body, bc, br, 0.03f, V(0.9f, -0.5f, 0.2f), V(-0.95f, -0.4f, 0.3f), V(0.6f, -0.3f, -0.7f), V(-0.5f, 0.1f, -0.8f));
        // The cap's shadow over its brow, heavy grey brows and drowsy eyes
        b.PaintEll(Body, V(0, 0.665f, 0.08f), V(0.25f, 0.025f, 0.16f), Rgb(140, 116, 92));
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.07f * s, 0.585f);
            b.Ell(Body, at + V(0.006f * s, 0.035f, -0.005f), V(0.05f, 0.018f, 0.025f), brow, V(0, 0, -10f * s), blend: 0.01f);
            b.Eye(Body, at, Outward(bc, br, at), 0.024f, Rgb(70, 56, 48), sclera: true, pupil: Rgb(56, 46, 40));
        });
        // A long beard of white threads hanging from under its cap
        for (int i = 0; i < 9; i++)
        {
            float x = -0.12f + 0.03f * i;
            var root = On(bc, br, x, 0.54f) - V(0, 0, 0.01f);
            float drop = 0.22f - MathF.Abs(x) * 0.6f;
            b.Tube(Body, new[] { root, root + V(x * 0.1f, -drop * 0.5f, 0.03f), root + V(x * 0.15f + (i % 2 == 0 ? 0.01f : -0.01f), -drop, 0.02f) }, 0.022f, 0.012f, beard, blend: 0.01f);
        }
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Pisicut line: the village cats

    private static readonly Color Carp2CatGrey = Rgb(166, 166, 172);
    private static readonly Color Carp2CatStripe = Rgb(110, 110, 120);
    private static readonly Color Carp2CatPale = Rgb(234, 232, 228);
    private static readonly Color Carp2Thread = Rgb(190, 54, 54);

    /// <summary>Tabby stripes across an ellipsoid's back and down its sides, each a thin painted band.</summary>
    private static void Carp2Tabby(PokeBuilder b, int bone, Vector3 c, Vector3 r, Color color, params float[] zs)
    {
        foreach (float z in zs)
            b.PaintEll(bone, V(c.X, c.Y + r.Y * 0.5f, z), V(r.X * 1.2f, r.Y * 0.95f, r.Z * 0.07f), color, V(-18f, 0, 0));
    }

    /// <summary>Pisicut: a round grey tabby kitten curled up as if on a warm stove, paws tucked under, a red thread tied in a bow and sleepy green eyes.</summary>
    private static PokeBuilder Pisicut()
    {
        var b = new PokeBuilder("Pisicut", 0.5f, BodyPlan.Quadruped, V(0, 0.14f, -0.03f)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.07f, 0.13f));
            b.Ell(leg, V(0.05f * s, 0.035f, 0.165f), V(0.042f, 0.032f, 0.05f), Carp2CatPale);
            int hind = b.Leg(s, V(0.12f * s, 0.09f, -0.1f), false);
            b.Ell(hind, V(0.125f * s, 0.085f, -0.1f), V(0.06f, 0.07f, 0.09f), Carp2CatGrey);
        });
        var bc = V(0, 0.14f, -0.03f);
        var br = V(0.16f, 0.13f, 0.19f);
        b.Ell(Body, bc, br, Carp2CatGrey);
        b.PaintEll(Body, V(0, 0.1f, 0.15f), V(0.1f, 0.08f, 0.06f), Carp2CatPale);
        Carp2Tabby(b, Body, bc, br, Carp2CatStripe, -0.16f, -0.08f, 0f, 0.08f);

        // The tail wrapped round its side
        int tail = b.Tail(V(0, 0.06f, -0.2f));
        var path = new[] { V(0.02f, 0.05f, -0.21f), V(0.12f, 0.04f, -0.19f), V(0.18f, 0.04f, -0.07f), V(0.16f, 0.035f, 0.06f) };
        b.Tube(tail, path, 0.036f, 0.03f, Carp2CatGrey, blend: 0.015f);
        foreach (var at in new[] { path[1], path[2] })
            b.PaintEll(tail, at, V(0.04f, 0.045f, 0.012f), Carp2CatStripe, Euler(path[3] - path[0]) + V(0, 0, 90f));

        int head = b.Head(V(0, 0.18f, 0.11f));
        var hc = V(0, 0.22f, 0.15f);
        var hr = V(0.12f, 0.1f, 0.1f);
        b.Ell(head, hc, hr, Carp2CatGrey);
        b.PaintEll(head, V(0, 0.185f, 0.23f), V(0.08f, 0.04f, 0.04f), Carp2CatPale);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.PaintEll(head, V(x, 0.31f, 0.18f), V(0.008f, 0.04f, 0.05f), Carp2CatStripe, V(-30f, 0, x * 300f));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, 0.29f, 0.12f));
            CatEar(b, ear, V(0.07f * s, 0.28f, 0.12f), V(0.11f * s, 0.36f, 0.11f), 0.038f, Carp2CatGrey, Rgb(232, 170, 176));
            var at = On(hc, hr, 0.047f * s, 0.225f);
            b.Eye(head, at, Outward(hc, hr, at), 0.017f, Rgb(120, 170, 96));
        });
        b.Mark(head, On(hc, hr, 0, 0.2f), V(0, 0, 1f), 0.012f, 0.009f, Rgb(232, 150, 160), MarkShape.Triangle, 180f);

        // A red woollen thread tied round its neck in a little bow
        var turn = V(62f, 0, 0);
        b.Torus(Body, V(0, 0.15f, 0.12f), 0.095f, 0.009f, Carp2Thread, turn, mat: SurfaceMaterial.Cloth);
        var knot = V(0, 0.115f, 0.205f);
        b.Ell(Body, knot, V(0.012f, 0.012f, 0.012f), Carp2Thread, mat: SurfaceMaterial.Cloth);
        PokeBuilder.Both(s =>
        {
            b.Torus(Body, knot + V(0.022f * s, 0.004f, 0.004f), 0.017f, 0.007f, Carp2Thread, V(80f, 0, 0), mat: SurfaceMaterial.Cloth);
            b.Limb(Body, knot, knot + V(0.012f * s, -0.035f, 0.012f), 0.007f, 0.006f, Carp2Thread, SurfaceMaterial.Cloth, 0.004f);
        });
        b.Ground();
        return b;
    }

    /// <summary>Motanor: the big old tomcat of the village tales, lean and heavy-shouldered, silver at the muzzle, one ear notched, a long striped tail and sly yellow eyes.</summary>
    private static PokeBuilder Motanor()
    {
        var b = new PokeBuilder("Motanor", 0.85f, BodyPlan.Quadruped, V(0, 0.42f, -0.02f)) { Coat = Fur };
        var grey = Rgb(150, 150, 158);
        var stripe = Rgb(86, 86, 96);
        var silver = Rgb(214, 214, 218);
        var scar = Rgb(196, 140, 140);
        BeastLegs(b, 0.1f, 0.32f, 0.17f, -0.2f, 0.055f, grey, Rgb(172, 170, 176));

        var bc = V(0, 0.43f, -0.03f);
        var br = V(0.14f, 0.14f, 0.27f);
        b.Ell(Body, bc, br, grey);
        b.Ell(Body, V(0, 0.44f, 0.15f), V(0.14f, 0.16f, 0.11f), grey);
        b.PaintEll(Body, V(0, 0.4f, 0.24f), V(0.09f, 0.13f, 0.05f), silver);
        Carp2Tabby(b, Body, bc, br, stripe, -0.24f, -0.14f, -0.04f, 0.06f);
        b.Mark(Body, Out(bc, br, default, V(1f, 0.3f, 0.4f)), V(1f, 0.3f, 0.4f), 0.03f, 0.008f, scar, MarkShape.Bar, 40f);

        // A long striped tail hanging down like a sash
        int tail = b.Tail(V(0, 0.47f, -0.28f));
        var path = new[] { V(0, 0.47f, -0.29f), V(0.01f, 0.32f, -0.38f), V(0.04f, 0.14f, -0.42f), V(0.07f, 0.05f, -0.36f) };
        b.Tube(tail, path, 0.036f, 0.032f, grey, blend: 0.015f);
        for (int i = 0; i < 3; i++)
            b.PaintEll(tail, Vector3.Lerp(path[i], path[i + 1], 0.55f), V(0.045f, 0.045f, 0.045f), stripe);

        int head = b.Head(V(0, 0.56f, 0.22f));
        var hc = V(0, 0.66f, 0.28f);
        var hr = V(0.12f, 0.105f, 0.105f);
        b.Ell(head, hc, hr, grey);
        b.Ell(head, V(0, 0.615f, 0.37f), V(0.06f, 0.042f, 0.04f), silver);
        // Ruffs of grizzled fur at its cheeks
        PokeBuilder.Both(s =>
        {
            foreach (var (y, len) in new[] { (0.64f, 0.07f), (0.6f, 0.06f) })
                b.Spike(head, V(0.09f * s, y, 0.3f), V((0.09f + len) * s, y - 0.035f, 0.29f), 0.03f, silver, 0.55f, blend: 0.012f);
        });
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.PaintEll(head, V(x, 0.76f, 0.3f), V(0.008f, 0.05f, 0.06f), stripe, V(-30f, 0, x * 300f));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, 0.74f, 0.25f));
            CatEar(b, ear, V(0.07f * s, 0.73f, 0.25f), V(0.12f * s, 0.85f, 0.23f), 0.048f, grey, Rgb(220, 160, 166));
            var at = On(hc, hr, 0.05f * s, 0.675f);
            b.Eye(head, at, Outward(hc, hr, at), 0.02f, Rgb(232, 200, 60), glare: true);
        });
        // The notch torn out of its left ear, and a scar over its brow
        b.Cut(head, V(-0.132f, 0.8f, 0.24f), V(0.014f, 0.014f, 0.04f));
        b.Mark(head, On(hc, hr, -0.075f, 0.72f), V(-0.3f, 0.3f, 1f), 0.025f, 0.006f, scar, MarkShape.Bar, 60f);
        b.Mark(head, V(0, 0.64f, 0.376f), V(0, 0.2f, 1f), 0.014f, 0.01f, Rgb(200, 130, 140), MarkShape.Triangle, 180f);
        Whiskers(b, head, V(0.05f, 0.61f, 0.37f), 0.14f, Rgb(240, 240, 240), 0.005f);

        // The old red thread, knotted and frayed
        b.Torus(Body, V(0, 0.55f, 0.21f), 0.115f, 0.011f, Carp2Thread, V(55f, 0, 0), mat: SurfaceMaterial.Cloth);
        var knot = Carp2Ring(V(0, 0.55f, 0.21f), 0.115f, V(55f, 0, 0), MathF.PI / 2f);
        b.Ell(Body, knot, V(0.016f, 0.016f, 0.016f), Carp2Thread, mat: SurfaceMaterial.Cloth);
        PokeBuilder.Both(s => b.Limb(Body, knot, knot + V(0.012f * s, -0.06f, 0.01f), 0.008f, 0.006f, Carp2Thread, SurfaceMaterial.Cloth, 0.004f));
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Cocosel line: the village roosters

    private static readonly Color Carp2Russet = Rgb(186, 76, 42);
    private static readonly Color Carp2RoosterGreen = Rgb(66, 146, 66);
    private static readonly Color Carp2RoosterDeep = Rgb(36, 84, 56);
    private static readonly Color Carp2Comb = Rgb(222, 52, 48);
    private static readonly Color Carp2Shank = Rgb(234, 196, 90);

    /// <summary>Cocosel: a gangly young rooster, russet shading to green on its wings and tail, a tiny red comb, long yellow legs, chest out and head cocked.</summary>
    private static PokeBuilder Cocosel()
    {
        var b = new PokeBuilder("Cocosel", 0.56f, BodyPlan.Bird, V(0, 0.34f, 0)) { Coat = Fur };
        Carp2BirdLegs(b, 0.045f, 0.29f, 0.0f, 0.016f, Carp2Shank);
        var bc = V(0, 0.34f, 0);
        b.Ell(Body, bc, V(0.1f, 0.1f, 0.13f), Carp2Russet, V(-20f, 0, 0));
        b.PaintEll(Body, V(0, 0.27f, -0.03f), V(0.12f, 0.07f, 0.15f), Carp2RoosterGreen);
        b.Ell(Body, V(0, 0.38f, 0.09f), V(0.075f, 0.08f, 0.06f), Carp2Russet);
        PokeBuilder.Both(s => FoldedWing(b, s, V(0.085f * s, 0.39f, 0.04f), V(0.11f * s, 0.31f, -0.12f), 0.055f, Carp2Russet, Carp2RoosterGreen));
        int tail = b.Tail(V(0, 0.37f, -0.11f));
        TailFan(b, tail, V(0, 0.37f, -0.11f), 4, 46f, 0.15f, 0.04f, Carp2RoosterGreen, Carp2RoosterDeep, -1.2f);

        int head = b.Head(V(0, 0.44f, 0.07f));
        b.Limb(head, V(0, 0.42f, 0.07f), V(0, 0.5f, 0.09f), 0.048f, 0.044f, Carp2Russet);
        var hc = V(0.01f, 0.54f, 0.09f);
        var hr = V(0.07f, 0.072f, 0.072f);
        b.Ell(head, hc, hr, Carp2Russet);
        foreach (var (z, r) in new[] { (0.12f, 0.016f), (0.095f, 0.02f), (0.068f, 0.017f) })
            b.Ell(head, V(0.015f, 0.615f, z), V(r * 0.7f, r, r), Carp2Comb, blend: 0.008f);
        b.Spike(head, V(0.01f, 0.535f, 0.15f), V(0.012f, 0.52f, 0.2f), 0.02f, Rgb(178, 160, 140), 0.8f, Shell);
        b.Ell(head, V(0.01f, 0.505f, 0.14f), V(0.012f, 0.014f, 0.01f), Carp2Comb, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.6f * s, 0.15f, 0.78f));
            b.Eye(head, at, at - hc, 0.017f, Rgb(80, 52, 36));
        });
        b.Ground();
        return b;
    }

    /// <summary>Cocosor: the fighting cockerel who crows the dawn, a towering comb like a flame, golden hackles, a sweep of dark green sickle feathers, spurred legs and its beak open mid-crow.</summary>
    private static PokeBuilder Cocosor()
    {
        var b = new PokeBuilder("Cocosor", 0.95f, BodyPlan.Bird, V(0, 0.52f, 0)) { Coat = Fur };
        var gold = Rgb(234, 178, 66);
        var wine = Rgb(150, 44, 40);
        Carp2BirdLegs(b, 0.065f, 0.44f, 0.0f, 0.024f, Carp2Shank, spur: true);
        var bc = V(0, 0.52f, 0);
        b.Ell(Body, bc, V(0.15f, 0.15f, 0.21f), wine, V(-20f, 0, 0));
        b.PaintEll(Body, V(0, 0.42f, -0.02f), V(0.17f, 0.08f, 0.24f), Carp2RoosterDeep);
        b.Ell(Body, V(0, 0.6f, 0.13f), V(0.11f, 0.12f, 0.09f), wine);
        PokeBuilder.Both(s => FoldedWing(b, s, V(0.13f * s, 0.6f, 0.06f), V(0.16f * s, 0.47f, -0.18f), 0.08f, wine, Carp2RoosterDeep));

        // Sickle feathers sweeping up and over behind it, golden saddle feathers at their root
        int tail = b.Tail(V(0, 0.58f, -0.18f));
        foreach (var (x, h, reach) in new[] { (0f, 0.42f, 0.36f), (-0.04f, 0.36f, 0.3f), (0.04f, 0.36f, 0.3f), (-0.07f, 0.28f, 0.24f), (0.07f, 0.28f, 0.24f) })
        {
            var root = V(x * 0.5f, 0.6f, -0.17f);
            b.Tube(tail, new[] { root, root + V(x * 0.6f, h * 0.7f, -reach * 0.35f), root + V(x, h, -reach * 0.8f), root + V(x * 1.2f, h * 0.7f, -reach * 1.15f), root + V(x * 1.3f, h * 0.3f, -reach * 1.2f) },
                0.03f, 0.014f, Carp2RoosterDeep, blend: 0.012f);
        }
        foreach (float x in new[] { -0.06f, 0f, 0.06f })
            b.Spike(tail, V(x, 0.62f, -0.15f), V(x * 1.5f, 0.56f, -0.27f), 0.035f, gold, 0.5f, blend: 0.015f);

        int head = b.Head(V(0, 0.68f, 0.13f));
        b.Limb(head, V(0, 0.66f, 0.12f), V(0, 0.8f, 0.17f), 0.07f, 0.058f, gold);
        var hc = V(0, 0.86f, 0.19f);
        var hr = V(0.075f, 0.075f, 0.08f);
        b.Ell(head, hc, hr, Rgb(198, 64, 44));
        // Golden hackles flaring round its neck
        for (int i = 0; i < 11; i++)
        {
            float a = MathF.PI * (-0.15f + 1.3f * i / 10f);
            var root = V(MathF.Cos(a) * 0.065f, 0.8f, 0.16f - MathF.Sin(a) * 0.06f);
            var outward = Vector3.Normalize(V(MathF.Cos(a), 0, -MathF.Sin(a)));
            b.Spike(head, root, root + outward * 0.08f + V(0, -0.14f, -0.02f), 0.035f, gold, 0.45f, blend: 0.015f);
        }
        // The towering comb like a flame, and wattles under the beak
        foreach (var (z, h, r) in new[] { (0.24f, 0.08f, 0.024f), (0.215f, 0.13f, 0.03f), (0.19f, 0.16f, 0.032f), (0.165f, 0.13f, 0.028f), (0.14f, 0.09f, 0.024f) })
            b.Spike(head, V(0, 0.91f, z), V(0, 0.91f + h, z - 0.03f), r, Carp2Comb, 0.4f, blend: 0.012f);
        PokeBuilder.Both(s => b.Ell(head, V(0.015f * s, 0.815f, 0.235f), V(0.018f, 0.035f, 0.018f), Carp2Comb, blend: 0.008f));
        // Its beak open mid-crow
        b.Spike(head, V(0, 0.875f, 0.255f), V(0, 0.885f, 0.33f), 0.026f, Rgb(240, 200, 90), 0.8f, Shell);
        int jaw = b.Jaw(head, V(0, 0.85f, 0.25f));
        b.Spike(jaw, V(0, 0.85f, 0.25f), V(0, 0.82f, 0.31f), 0.02f, Rgb(232, 186, 80), 0.8f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.62f * s, 0.25f, 0.75f));
            b.Eye(head, at, at - hc, 0.019f, Rgb(232, 150, 40), glare: true);
        });
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Albinut line: the honeybees

    private static readonly Color Carp2Amber = Rgb(232, 176, 66);
    private static readonly Color Carp2BeeBrown = Rgb(98, 62, 36);
    private static readonly Color Carp2Wax = Rgb(244, 232, 196);
    private static readonly Color Carp2WingPale = Rgb(238, 240, 240);
    private static readonly Color Carp2Honey = Rgb(242, 172, 34);

    /// <summary>Albinut: a round, fuzzy little honeybee in amber and brown stripes, a cap of pale wax on its head, small clear wings and stubby legs dangling.</summary>
    private static PokeBuilder Albinut()
    {
        var b = new PokeBuilder("Albinut", 0.46f, BodyPlan.Bird, V(0, 0.2f, -0.02f)) { Coat = Fur }.Hover();
        var face = Rgb(244, 200, 96);
        var bc = V(0, 0.2f, -0.04f);
        var br = V(0.15f, 0.14f, 0.16f);
        b.Ell(Body, bc, br, Carp2Amber);
        foreach (var (z, ring) in new[] { (-0.07f, 0.142f), (-0.14f, 0.11f) })
            b.PaintTorus(Body, V(0, 0.2f, z), ring, 0.018f, Carp2BeeBrown, V(90f, 0, 0), 1.07f, 1f);
        FurTufts(b, Body, bc, br, 22, 0.022f, 0.016f, Carp2Amber, 0.3f, -0.5f, 0f);
        // Six stubby legs dangling under it, a little sting behind
        foreach (float z in new[] { 0.04f, -0.04f, -0.12f })
            PokeBuilder.Both(s => b.Limb(Body, V(0.06f * s, 0.11f, z), V(0.07f * s, 0.055f, z + 0.01f), 0.014f, 0.012f, Carp2BeeBrown, blend: 0.008f));
        b.Spike(Body, V(0, 0.2f, -0.18f), V(0, 0.19f, -0.24f), 0.018f, Carp2BeeBrown, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.06f * s, 0.32f, -0.06f));
            Petal(b, w, V(0.05f * s, 0.32f, -0.06f), V(0.15f * s, 0.47f, -0.1f), 0.05f, Carp2WingPale, Shell, 0.008f);
            Petal(b, w, V(0.06f * s, 0.31f, -0.07f), V(0.2f * s, 0.36f, -0.12f), 0.035f, Carp2WingPale, Shell, 0.008f);
        });

        int head = b.Head(V(0, 0.2f, 0.05f));
        var hc = V(0, 0.21f, 0.08f);
        var hr = V(0.125f, 0.12f, 0.1f);
        b.Ell(head, hc, hr, face);
        // A cap of pale wax dripping a little over its brow, and two little antennae
        b.Ell(head, V(0, 0.32f, 0.07f), V(0.07f, 0.03f, 0.065f), Carp2Wax, mat: Shell, blend: 0.015f);
        foreach (var (x, z) in new[] { (-0.05f, 0.11f), (0.03f, 0.12f), (0.065f, 0.05f) })
            b.Ell(head, V(x, 0.305f, z), V(0.02f, 0.025f, 0.02f), Carp2Wax, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0.05f * s, 0.3f, 0.1f), V(0.09f * s, 0.37f, 0.14f), V(0.13f * s, 0.37f, 0.17f) }, 0.009f, 0.008f, Carp2BeeBrown, blend: 0.006f));
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.042f * s, 0.215f);
            b.Eye(head, at, Outward(hc, hr, at), 0.022f, Rgb(80, 52, 36));
        });
        b.Mark(head, On(hc, hr, 0, 0.17f), V(0, -0.2f, 1f), 0.02f, 0.012f, Rgb(170, 70, 60), MarkShape.Smile);
        return Lift(b);
    }

    /// <summary>Stupara: the hive queen, her abdomen an old straw skep dripping honey, a slim amber thorax in a ruff of pale fur, gauzy wings, a little wax crown and two of her workers on the hive.</summary>
    private static PokeBuilder Stupara()
    {
        var b = new PokeBuilder("Stupara", 0.9f, BodyPlan.Floating, V(0, 0.32f, 0)) { Coat = Fur }.Hover();
        var straw = Rgb(206, 158, 72);
        var strawDark = Rgb(170, 122, 52);
        var face = Rgb(244, 204, 100);

        // The skep: a dome of coiled straw round a core, its door low in front
        var sc = V(0, 0.3f, 0);
        var sr = V(0.23f, 0.26f, 0.22f);
        b.Ell(Body, sc, sr, straw, mat: Shell);
        for (int i = 0; i < 8; i++)
        {
            float y = 0.1f + 0.055f * i;
            float k = MathF.Sqrt(MathF.Max(0.05f, 1f - MathF.Pow((y - sc.Y) / sr.Y, 2f)));
            b.Torus(Body, V(0, y, 0), sr.X * k * 0.98f, 0.026f, i % 2 == 0 ? straw : strawDark, default, 1f, sr.Z / sr.X, Shell, 0.008f);
        }
        b.Mark(Body, Out(sc, sr, default, V(0.25f, -0.45f, 1f)), V(0.25f, -0.45f, 1f), 0.03f, 0.035f, Rgb(70, 44, 26));
        // Honey dripping from its rim
        foreach (var (a, len) in new[] { (0.3f, 0.07f), (1.3f, 0.05f), (2.4f, 0.08f), (3.5f, 0.06f), (4.6f, 0.07f), (5.6f, 0.05f) })
        {
            var top = V(MathF.Cos(a) * 0.13f, 0.1f, MathF.Sin(a) * 0.12f);
            b.Limb(Body, top, top + V(0, -len, 0), 0.02f, 0.012f, Carp2Honey, Shell, 0.01f);
            b.Ell(Body, top + V(0, -len - 0.005f, 0), V(0.017f, 0.02f, 0.017f), Carp2Honey, mat: Shell, blend: 0.008f);
        }
        // Two of her workers clinging to the hive
        foreach (var (at, n) in new[] { (Out(sc, sr, default, V(-0.9f, 0.1f, 0.4f)), V(-0.9f, 0.1f, 0.4f)), (Out(sc, sr, default, V(0.8f, 0.4f, -0.5f)), V(0.8f, 0.4f, -0.5f)) })
        {
            var p = at + Vector3.Normalize(n) * 0.02f;
            b.Ell(Body, p, V(0.028f, 0.024f, 0.035f), Carp2Amber, blend: 0.008f);
            b.PaintTorus(Body, p, 0.025f, 0.006f, Carp2BeeBrown, V(90f, 0, 0));
            b.Ell(Body, p + V(0, 0.025f, -0.005f), V(0.022f, 0.012f, 0.008f), Carp2WingPale, mat: Shell, blend: 0.004f);
        }
        // Her dark legs folded over the hive
        PokeBuilder.Both(s =>
        {
            b.Limb(Body, V(0.06f * s, 0.55f, 0.06f), V(0.17f * s, 0.43f, 0.15f), 0.016f, 0.014f, Carp2BeeBrown, blend: 0.006f);
            b.Limb(Body, V(0.17f * s, 0.43f, 0.15f), V(0.22f * s, 0.22f, 0.15f), 0.014f, 0.011f, Carp2BeeBrown, blend: 0.006f);
        });

        // A slim amber thorax in a ruff of pale fur
        var tc = V(0, 0.6f, 0.02f);
        b.Ell(Body, tc, V(0.085f, 0.1f, 0.075f), Carp2Amber);
        b.PaintTorus(Body, V(0, 0.57f, 0.02f), 0.083f, 0.014f, Carp2BeeBrown, default, 1f, 0.9f);
        b.Torus(Body, V(0, 0.68f, 0.02f), 0.075f, 0.04f, Carp2Wax, default, 1f, 0.95f, blend: 0.015f);
        FurTufts(b, Body, V(0, 0.69f, 0.02f), V(0.1f, 0.04f, 0.09f), 12, 0.03f, 0.018f, Carp2Wax, 1.1f, -0.8f, 0.3f);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.06f * s, 0.65f, -0.05f));
            Petal(b, w, V(0.06f * s, 0.66f, -0.06f), V(0.38f * s, 0.86f, -0.12f), 0.09f, Carp2WingPale, Shell, 0.01f);
            Petal(b, w, V(0.06f * s, 0.62f, -0.06f), V(0.32f * s, 0.54f, -0.12f), 0.065f, Carp2WingPale, Shell, 0.01f);
            // Thin dark arms, one hand raised to her chin
            int arm = b.Arm(s, V(0.07f * s, 0.62f, 0.04f));
            var elbow = V(0.13f * s, 0.55f, 0.1f);
            var hand = s < 0 ? V(-0.04f, 0.72f, 0.12f) : V(0.15f, 0.43f, 0.2f);
            b.Limb(arm, V(0.07f * s, 0.62f, 0.04f), elbow, 0.016f, 0.014f, Carp2BeeBrown, blend: 0.006f);
            b.Limb(arm, elbow, hand, 0.014f, 0.012f, Carp2BeeBrown, blend: 0.006f);
            b.Ell(arm, hand, V(0.02f, 0.02f, 0.02f), Carp2BeeBrown, blend: 0.006f);
        });

        int head = b.Head(V(0, 0.72f, 0.03f));
        var hc = V(0, 0.8f, 0.04f);
        var hr = V(0.085f, 0.085f, 0.078f);
        b.Ell(head, hc, hr, face);
        // A little crown of wax and long curled antennae
        b.Torus(head, V(0, 0.875f, 0.03f), 0.04f, 0.012f, Carp2Wax, mat: Shell);
        for (int i = 0; i < 5; i++)
        {
            float a = MathF.PI * (0.15f + 0.7f * i / 4f);
            var at = V(MathF.Cos(a) * 0.04f, 0.88f, 0.03f + MathF.Sin(a) * 0.04f);
            b.Spike(head, at, at + V(0, i == 2 ? 0.05f : 0.035f, 0), 0.012f, Carp2Wax, mat: Shell, blend: 0.006f);
        }
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0.04f * s, 0.86f, 0.06f), V(0.07f * s, 0.95f, 0.07f), V(0.1f * s, 1.01f, 0.04f), V(0.13f * s, 1.0f, 0.0f), V(0.13f * s, 0.96f, 0.0f) }, 0.009f, 0.008f, Carp2BeeBrown, blend: 0.004f));
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.035f * s, 0.805f);
            b.Eye(head, at, Outward(hc, hr, at), 0.02f, Rgb(90, 56, 34));
        });
        b.Mark(head, On(hc, hr, 0, 0.765f), V(0, -0.2f, 1f), 0.016f, 0.008f, Rgb(150, 70, 50), MarkShape.Smile);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Iepurel line: the field hares

    private static readonly Color Carp2HareBrown = Rgb(176, 128, 80);
    private static readonly Color Carp2HareSand = Rgb(232, 206, 160);
    private static readonly Color Carp2HareTip = Rgb(46, 38, 34);
    private static readonly Color Carp2HareInner = Rgb(206, 150, 138);

    /// <summary>A hare's long ear through <paramref name="path"/>, pink inside and black at its tip.</summary>
    private static void Carp2HareEar(PokeBuilder b, int ear, Vector3[] path, float r)
    {
        b.Tube(ear, path, r, r * 0.75f, Carp2HareBrown, blend: 0.012f);
        for (int i = 1; i < path.Length - 1; i++)
            b.PaintEll(ear, path[i] + V(0, 0, r * 0.7f), V(r * 0.5f, r * 1.4f, r * 0.4f), Carp2HareInner, Euler(path[i + 1] - path[i - 1]));
        b.PaintEll(ear, path[^1], V(r * 1.3f, r * 1.6f, r * 1.3f), Carp2HareTip, Euler(path[^1] - path[^2]));
    }

    /// <summary>Iepurel: a young brown field hare crouched low in the grass, black-tipped ears with one flopped over, big watchful eyes and long hind legs ready to spring.</summary>
    private static PokeBuilder Iepurel()
    {
        var b = new PokeBuilder("Iepurel", 0.54f, BodyPlan.Quadruped, V(0, 0.2f, -0.02f)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.055f * s, 0.2f, 0.09f));
            b.Limb(leg, V(0.055f * s, 0.2f, 0.09f), V(0.06f * s, 0.03f, 0.12f), 0.026f, 0.022f, Carp2HareBrown);
            b.Ell(leg, V(0.06f * s, 0.018f, 0.14f), V(0.026f, 0.018f, 0.035f), Carp2HareBrown);
            int hind = b.Leg(s, V(0.09f * s, 0.17f, -0.1f), false);
            b.Ell(hind, V(0.095f * s, 0.14f, -0.1f), V(0.05f, 0.085f, 0.1f), Carp2HareBrown, V(-20f, 0, 0));
            b.Ell(hind, V(0.1f * s, 0.024f, -0.04f), V(0.034f, 0.024f, 0.1f), Carp2HareBrown);
        });
        var bc = V(0, 0.2f, -0.03f);
        var br = V(0.11f, 0.12f, 0.17f);
        b.Ell(Body, bc, br, Carp2HareBrown, V(-12f, 0, 0));
        b.PaintEll(Body, V(0, 0.18f, 0.1f), V(0.08f, 0.1f, 0.07f), Carp2HareSand);
        b.PaintEll(Body, V(0, 0.11f, -0.04f), V(0.08f, 0.04f, 0.13f), Carp2HareSand);
        int tail = b.Tail(V(0, 0.22f, -0.19f));
        b.Ell(tail, V(0, 0.23f, -0.22f), V(0.035f, 0.04f, 0.035f), Carp2HareSand);

        int head = b.Head(V(0, 0.29f, 0.11f));
        var hc = V(0, 0.33f, 0.14f);
        var hr = V(0.075f, 0.075f, 0.085f);
        b.Ell(head, hc, hr, Carp2HareBrown);
        b.Ell(head, V(0, 0.305f, 0.21f), V(0.038f, 0.032f, 0.032f), Carp2HareSand);
        b.Mark(head, V(0, 0.32f, 0.24f), V(0, 0.2f, 1f), 0.01f, 0.008f, Rgb(120, 80, 70), MarkShape.Triangle, 180f);
        int up = b.Ear(head, 1f, V(0.03f, 0.39f, 0.11f));
        Carp2HareEar(b, up, new[] { V(0.03f, 0.38f, 0.11f), V(0.04f, 0.5f, 0.1f), V(0.05f, 0.62f, 0.09f), V(0.055f, 0.68f, 0.085f) }, 0.032f);
        int flop = b.Ear(head, -1f, V(-0.03f, 0.39f, 0.11f));
        Carp2HareEar(b, flop, new[] { V(-0.03f, 0.38f, 0.11f), V(-0.05f, 0.5f, 0.1f), V(-0.1f, 0.57f, 0.1f), V(-0.17f, 0.55f, 0.11f) }, 0.03f);
        PokeBuilder.Both(s =>
        {
            var at = Out(hc, hr, default, V(0.7f * s, 0.2f, 0.7f));
            b.Eye(head, at, at - hc, 0.024f, Rgb(40, 36, 40));
        });
        b.Ground();
        return b;
    }

    /// <summary>Iepuroi: the boxing hare, upright on powerful hind legs, pale chest, thick forearms and fists raised in a boxer's guard, ears standing tall and a fierce grin.</summary>
    private static PokeBuilder Iepuroi()
    {
        var b = new PokeBuilder("Iepuroi", 0.95f, BodyPlan.Biped, V(0, 0.55f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.44f, 0));
            b.Ell(leg, V(0.1f * s, 0.38f, 0.01f), V(0.075f, 0.11f, 0.095f), Carp2HareBrown);
            b.Limb(leg, V(0.1f * s, 0.32f, -0.03f), V(0.11f * s, 0.07f, -0.06f), 0.045f, 0.035f, Carp2HareBrown);
            b.Ell(leg, V(0.11f * s, 0.03f, 0.04f), V(0.048f, 0.03f, 0.13f), Carp2HareBrown);
        });
        b.Ell(Body, V(0, 0.52f, 0), V(0.13f, 0.15f, 0.11f), Carp2HareBrown);
        b.Ell(Body, V(0, 0.68f, 0.02f), V(0.17f, 0.12f, 0.12f), Carp2HareBrown);
        b.PaintEll(Body, V(0, 0.56f, 0.08f), V(0.08f, 0.17f, 0.06f), Carp2HareSand);
        b.PaintEll(Body, V(0, 0.7f, 0.1f), V(0.07f, 0.06f, 0.05f), Carp2HareSand);
        int tail = b.Tail(V(0, 0.46f, -0.1f));
        b.Ell(tail, V(0, 0.46f, -0.13f), V(0.045f, 0.05f, 0.045f), Carp2HareSand);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.73f, 0.02f));
            var elbow = V(0.2f * s, 0.6f, 0.08f);
            var fist = s < 0 ? V(-0.07f, 0.73f, 0.2f) : V(0.16f, 0.8f, 0.17f);
            b.Ell(arm, V(0.16f * s, 0.69f, 0.03f), V(0.065f, 0.07f, 0.065f), Carp2HareBrown);
            b.Limb(arm, V(0.16f * s, 0.7f, 0.02f), elbow, 0.05f, 0.045f, Carp2HareBrown);
            b.Limb(arm, elbow, fist, 0.05f, 0.045f, Carp2HareBrown);
            b.Ell(arm, fist, V(0.06f, 0.06f, 0.058f), Carp2HareBrown);
            b.PaintEll(arm, fist + V(0, 0.01f, 0.045f), V(0.045f, 0.02f, 0.02f), PixelCanvas.Mix(Carp2HareBrown, Black, 0.2f));
        });

        int head = b.Head(V(0, 0.78f, 0.03f));
        var hc = V(0, 0.87f, 0.05f);
        var hr = V(0.1f, 0.095f, 0.1f);
        b.Ell(head, hc, hr, Carp2HareBrown);
        b.Ell(head, V(0, 0.835f, 0.13f), V(0.055f, 0.042f, 0.045f), Carp2HareSand);
        b.Mark(head, V(0, 0.86f, 0.175f), V(0, 0.2f, 1f), 0.014f, 0.01f, Rgb(120, 80, 70), MarkShape.Triangle, 180f);
        b.Mark(head, V(0, 0.81f, 0.165f), V(0, -0.4f, 1f), 0.035f, 0.014f, Rgb(70, 40, 34), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.04f * s, 0.95f, 0.03f));
            Carp2HareEar(b, ear, new[] { V(0.04f * s, 0.94f, 0.03f), V(0.055f * s, 1.06f, 0.02f), V(0.07f * s, 1.18f, 0.0f), V(0.08f * s, 1.25f, -0.01f) }, 0.04f);
            var at = On(hc, hr, 0.045f * s, 0.895f);
            b.Eye(head, at, Outward(hc, hr, at), 0.02f, Rgb(60, 50, 40), glare: true);
        });
        b.Ground();
        return b;
    }

    // ------------------------------------------------------------------ Soarel line: the sunflowers

    private static readonly Color Carp2Stalk = Rgb(104, 170, 84);
    private static readonly Color Carp2StalkDeep = Rgb(74, 132, 64);
    private static readonly Color Carp2Petal = Rgb(250, 202, 52);
    private static readonly Color Carp2PetalOrange = Rgb(244, 140, 44);
    private static readonly Color Carp2SunFace = Rgb(250, 214, 86);

    /// <summary>Soarel: a round green bud with a small yellow flower for a face tilted up at the sun, two leaf arms, stubby root feet and a blissful smile.</summary>
    private static PokeBuilder Soarel()
    {
        var b = new PokeBuilder("Soarel", 0.46f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Leaf };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.05f, 0));
            b.Ell(leg, V(0.06f * s, 0.022f, 0.04f), V(0.035f, 0.022f, 0.04f), Carp2StalkDeep);
            b.Ell(leg, V(0.1f * s, 0.022f, -0.01f), V(0.03f, 0.02f, 0.035f), Carp2StalkDeep);
        });
        var bc = V(0, 0.17f, 0);
        var br = V(0.15f, 0.15f, 0.14f);
        b.Ell(Body, bc, br, Carp2Stalk);
        // The bud's sepals folded round it, and its point behind the flower
        Petal(b, Body, V(-0.05f, 0.04f, 0.11f), V(0.03f, 0.26f, 0.1f), 0.075f, PixelCanvas.Mix(Carp2Stalk, Carp2StalkDeep, 0.4f), Leaf, 0.01f);
        b.Spike(Body, V(0.04f, 0.27f, -0.06f), V(0.06f, 0.37f, -0.07f), 0.05f, Carp2Stalk, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.17f, 0.02f));
            Petal(b, arm, V(0.13f * s, 0.17f, 0.02f), V(0.22f * s, 0.12f, 0.05f), 0.035f, Carp2Stalk, Leaf, 0.01f);
        });

        int head = b.Head(V(0, 0.26f, 0.05f));
        var at = V(0, 0.3f, 0.1f);
        var n = Vector3.Normalize(V(0, 0.45f, 1f));
        float hr = 0.08f;
        FlowerHead(b, head, at, n, 14, 0.13f, 0.035f, Carp2Petal, Carp2SunFace, hr);
        PokeBuilder.Both(s => b.Eye(head, Carp2Disc(at, n, hr, 0.03f * s, 0.012f), n, 0.016f, closed: true));
        b.Mark(head, Carp2Disc(at, n, hr, 0, -0.025f), n, 0.022f, 0.012f, Rgb(170, 90, 50), MarkShape.Smile);
        b.Ground();
        return b;
    }

    /// <summary>Floarsol: the sunflower sun, a twisted green stalk on splayed roots, broad leaves for arms with sun-scorched edges, and a great golden face of petals freckled with seeds.</summary>
    private static PokeBuilder Floarsol()
    {
        var b = new PokeBuilder("Floarsol", 0.95f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Leaf };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.12f, 0));
            foreach (var (dx, dz) in new[] { (1f, 0.4f), (0.5f, -0.8f), (0.3f, 1f) })
                b.Limb(leg, V(0.05f * s, 0.14f, 0), V((0.06f + 0.16f * dx) * s, 0.022f, 0.16f * dz), 0.045f, 0.02f, Carp2StalkDeep, blend: 0.015f);
        });
        // A stalk of two twisted strands
        b.Limb(Body, V(0, 0.1f, 0), V(0, 0.72f, 0), 0.05f, 0.04f, Carp2Stalk);
        for (int strand = 0; strand < 2; strand++)
        {
            var pts = new Vector3[8];
            for (int i = 0; i < pts.Length; i++)
            {
                float t = i / (float)(pts.Length - 1), a = strand * MathF.PI + t * 2.4f * MathF.PI;
                pts[i] = V(MathF.Cos(a) * 0.04f, 0.1f + 0.6f * t, MathF.Sin(a) * 0.035f);
            }
            b.Tube(Body, pts, 0.036f, 0.03f, strand == 0 ? Carp2Stalk : Carp2StalkDeep, blend: 0.01f);
        }
        // Broad leaves for arms, their edges scorched orange
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.04f * s, 0.55f, 0));
            var tip = s < 0 ? V(-0.4f, 0.62f, 0.05f) : V(0.38f, 0.42f, 0.06f);
            Frond(b, arm, V(0.04f * s, 0.55f, 0), tip, 0.09f, Carp2Stalk, V(0, 1f, 0.4f), 0.22f, Leaf);
            b.PaintEll(arm, tip + (V(0.04f * s, 0.55f, 0) - tip) * 0.12f, V(0.07f, 0.07f, 0.07f), Carp2PetalOrange, soft: 0.03f);
        });

        int head = b.Head(V(0, 0.7f, 0));
        var at = V(0, 0.84f, 0.05f);
        var n = Vector3.Normalize(V(0, 0.15f, 1f));
        float hr = 0.15f;
        // Green sepals behind, a ring of orange petals, a ring of golden ones and the face
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 8f;
            var o = V(MathF.Cos(a), MathF.Sin(a), 0);
            Frond(b, head, at - n * 0.03f + o * 0.08f, at - n * 0.05f + o * 0.24f, 0.05f, Carp2StalkDeep, n, 0.25f, Leaf);
        }
        FlowerHead(b, head, at - n * 0.015f, n, 16, 0.32f, 0.045f, Carp2PetalOrange, Carp2SunFace, hr, 0.5f);
        FlowerHead(b, head, at, n, 14, 0.26f, 0.042f, Carp2Petal, Carp2SunFace, hr);
        PokeBuilder.Both(s =>
        {
            b.Eye(head, Carp2Disc(at, n, hr, 0.05f * s, 0.018f), n, 0.026f, Rgb(140, 84, 44));
            b.PaintEll(head, Carp2Disc(at, n, hr, 0.08f * s, -0.025f), V(0.022f, 0.014f, 0.02f), Rgb(240, 150, 80));
        });
        b.Mark(head, Carp2Disc(at, n, hr, 0, -0.045f), n, 0.04f, 0.016f, Rgb(150, 80, 40), MarkShape.Smile);
        // Seeds freckled over its face
        foreach (var (x, y) in new[] { (-0.07f, 0.075f), (0.06f, 0.08f), (0f, 0.1f), (-0.1f, -0.01f), (0.1f, -0.005f) })
            b.PaintEll(head, Carp2Disc(at, n, hr, x, y), V(0.008f, 0.011f, 0.01f), Rgb(96, 60, 34), soft: 0.004f);
        b.Ground();
        return b;
    }
}
