using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 4 (plan 46 · CP4 to CP8): Zimbrel to Babut (20 species), the forests and the mountains
// (docs/plans/decisions/CP-dex-2-mountains.md, Nos. 61 to 80). Each is sculpted from nothing, our own design on its
// brief; a line shares its palette and its motifs and grows in size and in detail. Helpers of this batch are named
// C4…, so that no other batch's file meets them.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames4() => new[]
    {
        "Zimbrel", "Zimbrunt", "Acvilet", "Acviltor", "Huhurez", "Ciocbit", "Ciocanor", "Tritonel", "Tritanor", "Viperin",
        "Cornviper", "Radascuta", "Radaskon", "Fluturel", "Fluturapol", "Bradlet", "Bradonor", "Floricolt", "Coltarina", "Babut"
    };

    private static PokeBuilder? CreateCarpathia4(string species) => species switch
    {
        "ZIMBREL" => C4Zimbrel(),
        "ZIMBRUNT" => C4Zimbrunt(),
        "ACVILET" => C4Acvilet(),
        "ACVILTOR" => C4Acviltor(),
        "HUHUREZ" => C4Huhurez(),
        "CIOCBIT" => C4Ciocbit(),
        "CIOCANOR" => C4Ciocanor(),
        "TRITONEL" => C4Tritonel(),
        "TRITANOR" => C4Tritanor(),
        "VIPERIN" => C4Viperin(),
        "CORNVIPER" => C4Cornviper(),
        "RADASCUTA" => C4Radascuta(),
        "RADASKON" => C4Radaskon(),
        "FLUTUREL" => C4Fluturel(),
        "FLUTURAPOL" => C4Fluturapol(),
        "BRADLET" => C4Bradlet(),
        "BRADONOR" => C4Bradonor(),
        "FLORICOLT" => C4Floricolt(),
        "COLTARINA" => C4Coltarina(),
        "BABUT" => C4Babut(),
        _ => null
    };

    // ------------------------------------------------------------------ helpers of the batch

    /// <summary>A serpent's body through <paramref name="points"/> as <c>Coils</c> lays it, a bone every <paramref name="per"/> stretches; returns the bone of each stretch.</summary>
    private static int[] C4Coils(PokeBuilder b, Vector3[] points, Func<float, float> radius, Color color, int per = 2)
    {
        var bones = new int[points.Length - 1];
        int parent = Body, bone = Body;
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (i > 0 && i % per == 0)
            {
                bone = b.Part("seg" + i / per, parent, points[i], PokeRole.Segment, i / per);
                parent = bone;
            }
            float t0 = i / (float)(points.Length - 1), t1 = (i + 1) / (float)(points.Length - 1);
            b.Limb(bone, points[i], points[i + 1], radius(t0), radius(t1), color, blend: 0f);
            bones[i] = bone;
        }
        return bones;
    }

    /// <summary>
    /// A viper's zigzag down its back: on each stretch a dark lozenge painted to one side of the spine and then the other,
    /// each on the stretch's own bone, so they join into a zigzag; the belly under the rising neck painted pale.
    /// </summary>
    private static void C4Zigzag(PokeBuilder b, Vector3[] points, int[] bones, Func<float, float> radius, Color zig, int from, int to)
    {
        for (int i = from; i < Math.Min(to, points.Length - 1); i++)
        {
            var mid = (points[i] + points[i + 1]) / 2f;
            var dir = Vector3.Normalize(points[i + 1] - points[i]);
            var side = Vector3.Cross(dir, Vector3.UnitY);
            if (side.LengthSquared() < 1e-4f) side = Vector3.UnitX;
            side = Vector3.Normalize(side);
            var up = Vector3.Normalize(Vector3.Cross(side, dir));
            float r = radius((i + 0.5f) / (points.Length - 1));
            float way = i % 2 == 0 ? 1f : -1f;
            var at = mid + up * r * 0.9f + side * r * 0.32f * way;
            var along = Vector3.Normalize(dir + side * 0.7f * way);
            b.PaintEll(bones[i], at, V(r * 0.36f, Vector3.Distance(points[i], points[i + 1]) * 0.62f, r * 0.36f), zig, Euler(along), PokeBuilder.PaintEdge * 0.6f);
        }
    }

    /// <summary>
    /// One tier of a fir's boughs: a cone of needles from a round rim at <paramref name="y"/>, cut flat underneath, with a
    /// fringe of drooping needle tufts round its rim. Built from the top tier down: each cut carves only what lies under it.
    /// </summary>
    private static void C4FirTier(PokeBuilder b, float y, float height, float radius, Color green, Color fringe, int tufts, float turn)
    {
        b.Spike(Body, V(0, y, 0), V(0, y + height, 0), radius, green, mat: Leaf, blend: 0.02f);
        b.CutBox(Body, V(0, y - 0.012f - 1f, 0), V(2f, 1f, 2f), Quaternion.Identity);
        for (int i = 0; i < tufts; i++)
        {
            float a = turn + i * MathF.Tau / tufts;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(0, y + 0.004f, 0) + o * radius * 0.88f;
            b.Spike(Body, root, root + o * radius * 0.22f + V(0, -radius * 0.16f, 0), radius * 0.13f, fringe, 0.55f, Leaf, 0.012f);
        }
    }

    /// <summary>A star flower facing <paramref name="facing"/>: <paramref name="count"/> felted petals, long and short in turn, round a woolly heart.</summary>
    private static void C4StarFlower(PokeBuilder b, int bone, Vector3 at, Vector3 facing, int count, float length, float width, Color petal, Color heart, float heartR, SurfaceMaterial mat)
    {
        var n = Vector3.Normalize(facing);
        var u = Vector3.Normalize(Vector3.Cross(n, MathF.Abs(n.Y) > 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
        var w = Vector3.Cross(n, u);
        for (int i = 0; i < count; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / count;
            var o = u * MathF.Sin(a) + w * MathF.Cos(a);
            float l = i % 2 == 0 ? length : length * 0.78f;
            Frond(b, bone, at + o * heartR * 0.4f - n * 0.004f, at + o * l - n * l * 0.1f, width, petal, n, 0.28f, mat);
        }
        b.Ell(bone, at + n * heartR * 0.3f, V(heartR, heartR, heartR * 0.6f), heart, mat: Fur, blend: 0.012f);
    }

    // ------------------------------------------------------------------ Zimbrel line (the wisent)

    /// <summary>Zimbrel: a stocky woolly calf in pale tawny, a short woolly hump, a curly forelock, stub horns and a pale star on its brow.</summary>
    private static PokeBuilder C4Zimbrel()
    {
        var b = new PokeBuilder("Zimbrel", 0.6f, BodyPlan.Quadruped, V(0, 0.34f, -0.02f)) { Coat = Fur };
        var tawny = Rgb(226, 190, 124);
        var wool = Rgb(240, 212, 150);
        var deep = Rgb(196, 154, 98);
        var muzzle = Rgb(168, 126, 96);
        var hoof = Rgb(92, 78, 70);

        foreach (var (z, front) in new[] { (0.18f, true), (-0.24f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.15f * s, 0.27f, z), front);
                b.Limb(leg, V(0.15f * s, 0.27f, z), V(0.16f * s, 0.07f, z + 0.01f), 0.075f, 0.058f, tawny);
                b.Ell(leg, V(0.16f * s, 0.036f, z + 0.02f), V(0.06f, 0.038f, 0.068f), hoof, mat: Shell, blend: 0.012f);
                b.PaintEll(leg, V(0.16f * s, 0.036f, z + 0.085f), V(0.006f, 0.03f, 0.02f), PixelCanvas.Shadow(hoof, 0.4f), soft: 0.005f);
            });
        b.Ell(Body, V(0, 0.34f, -0.05f), V(0.24f, 0.2f, 0.34f), tawny);
        b.PaintEll(Body, V(0, 0.18f, -0.04f), V(0.2f, 0.07f, 0.28f), deep);
        // The short hump of wool over its shoulders
        b.Ell(Body, V(0, 0.43f, 0.08f), V(0.22f, 0.19f, 0.2f), wool, blend: 0.05f);
        FurTufts(b, Body, V(0, 0.43f, 0.08f), V(0.22f, 0.19f, 0.2f), 20, 0.06f, 0.032f, wool, face: 0.8f, below: -0.15f, droop: 0.45f);
        int tail = b.Tail(V(0, 0.38f, -0.37f));
        b.Limb(tail, V(0, 0.38f, -0.37f), V(0, 0.26f, -0.44f), 0.022f, 0.016f, tawny);
        b.Ell(tail, V(0, 0.22f, -0.45f), V(0.036f, 0.058f, 0.036f), deep);

        int head = b.Head(V(0, 0.4f, 0.26f));
        var c = V(0, 0.36f, 0.42f);
        var r = V(0.15f, 0.15f, 0.15f);
        b.Ell(head, c, r, tawny);
        b.Ell(head, V(0, 0.29f, 0.54f), V(0.095f, 0.075f, 0.075f), muzzle, blend: 0.03f);
        PokeBuilder.Both(s => b.Mark(head, V(0.034f * s, 0.3f, 0.612f), V(0.3f * s, 0.1f, 1f), 0.014f, 0.01f, Rgb(84, 58, 50)));
        b.Mark(head, V(0, 0.248f, 0.598f), V(0, -0.5f, 1f), 0.042f, 0.008f, PixelCanvas.Shadow(muzzle, 0.45f), MarkShape.Bar);
        // The curly forelock over its brow
        foreach (var p in new[] { V(0, 0.5f, 0.46f), V(-0.07f, 0.49f, 0.43f), V(0.07f, 0.49f, 0.43f), V(-0.035f, 0.52f, 0.38f), V(0.04f, 0.52f, 0.37f), V(0, 0.505f, 0.33f) })
            b.Ell(head, p, V(0.048f, 0.042f, 0.048f), wool, blend: 0.02f);
        PokeBuilder.Both(s => b.Spike(head, V(0.1f * s, 0.46f, 0.38f), V(0.15f * s, 0.53f, 0.38f), 0.026f, hoof, mat: Shell));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.13f * s, 0.42f, 0.39f));
            b.Ell(ear, V(0.19f * s, 0.41f, 0.39f), V(0.06f, 0.028f, 0.04f), tawny, V(0, 0, -18f * s));
            b.PaintEll(ear, V(0.2f * s, 0.41f, 0.41f), V(0.042f, 0.016f, 0.02f), Rgb(212, 160, 128), V(0, 0, -18f * s));
        });
        var star = On(c, r, 0, 0.415f);
        b.Mark(head, star, Outward(c, r, star), 0.03f, 0.03f, Rgb(250, 238, 204), MarkShape.Star5);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.086f * s, 0.37f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.6f), 0.032f, Rgb(70, 52, 40));
        });
        return b;
    }

    /// <summary>Zimbrunt: the great wisent, a massive dark hump and shaggy mane over tawny haunches, a heavy beard, black horns and a six-pointed star.</summary>
    private static PokeBuilder C4Zimbrunt()
    {
        var b = new PokeBuilder("Zimbrunt", 0.98f, BodyPlan.Quadruped, V(0, 0.62f, -0.05f)) { Coat = Fur };
        var dark = Rgb(82, 58, 40);
        var brown = Rgb(120, 88, 60);
        var tan = Rgb(202, 156, 100);
        var hind = Rgb(206, 148, 88);
        var horn = Rgb(40, 38, 42);
        var hoof = Rgb(48, 44, 46);
        var dirt = Rgb(112, 86, 60);

        foreach (var (z, front) in new[] { (0.34f, true), (-0.42f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.26f * s, 0.52f, z), front);
                b.Limb(leg, V(0.26f * s, 0.54f, z), V(0.27f * s, 0.3f, z + 0.02f), 0.13f, 0.11f, front ? dark : hind);
                b.Limb(leg, V(0.27f * s, 0.3f, z + 0.02f), V(0.27f * s, 0.1f, z + 0.03f), 0.1f, 0.09f, dark);
                b.Ell(leg, V(0.27f * s, 0.052f, z + 0.05f), V(0.1f, 0.054f, 0.12f), hoof, mat: Shell, blend: 0.012f);
                b.PaintEll(leg, V(0.27f * s, 0.05f, z + 0.16f), V(0.008f, 0.04f, 0.03f), Black, soft: 0.006f);
                // Earth churned up and caked round each fetlock
                foreach (var (dx, dz) in new[] { (-0.08f, 0.05f), (0.08f, 0.02f), (0f, 0.1f), (0.05f, -0.07f) })
                    b.Ell(leg, V((0.27f + dx) * s, 0.14f + dz * 0.3f, z + 0.03f + dz), V(0.038f, 0.03f, 0.036f), dirt, blend: 0.012f);
            });
        b.Ell(Body, V(0, 0.62f, -0.12f), V(0.34f, 0.3f, 0.6f), hind);
        b.PaintEll(Body, V(0.0f, 0.62f, -0.44f), V(0.36f, 0.22f, 0.24f), PixelCanvas.Light1(hind, 0.22f));
        // The great hump and the mane of the forequarters, shaggy and dark
        b.Ell(Body, V(0, 0.62f, 0.3f), V(0.37f, 0.34f, 0.3f), dark, blend: 0.06f);
        b.Ell(Body, V(0, 0.88f, 0.2f), V(0.38f, 0.34f, 0.36f), dark, blend: 0.08f);
        FurTufts(b, Body, V(0, 0.86f, 0.18f), V(0.38f, 0.36f, 0.38f), 36, 0.12f, 0.05f, dark, face: 0.88f, below: -0.2f, droop: 0.55f);
        b.PaintEll(Body, V(0, 0.44f, 0.5f), V(0.22f, 0.2f, 0.14f), tan);
        int tail = b.Tail(V(0, 0.78f, -0.7f));
        b.Limb(tail, V(0, 0.78f, -0.7f), V(0, 0.46f, -0.82f), 0.032f, 0.026f, hind);
        b.Ell(tail, V(0, 0.38f, -0.84f), V(0.06f, 0.1f, 0.06f), dark, blend: 0.02f);

        int head = b.Head(V(0, 0.72f, 0.58f));
        var c = V(0, 0.58f, 0.8f);
        var r = V(0.2f, 0.21f, 0.2f);
        b.Ell(head, c, r, brown);
        b.Ell(head, V(0, 0.47f, 0.96f), V(0.13f, 0.1f, 0.1f), Rgb(152, 114, 82), blend: 0.03f);
        PokeBuilder.Both(s => b.Mark(head, V(0.05f * s, 0.49f, 1.055f), V(0.3f * s, 0.1f, 1f), 0.02f, 0.014f, Rgb(70, 48, 40)));
        b.Mark(head, V(0, 0.415f, 1.04f), V(0, -0.5f, 1f), 0.06f, 0.01f, PixelCanvas.Shadow(brown, 0.5f), MarkShape.Bar);
        // A dark shock of mane over the crown, and the heavy beard under the chin
        b.Ell(head, V(0, 0.75f, 0.71f), V(0.23f, 0.13f, 0.18f), dark, blend: 0.03f);
        FurTufts(b, head, V(0, 0.76f, 0.7f), V(0.22f, 0.12f, 0.17f), 12, 0.07f, 0.04f, dark, face: 0.9f, below: 0f, droop: 0.4f);
        foreach (float x in new[] { -0.1f, -0.05f, 0f, 0.05f, 0.1f })
            b.Spike(head, V(x, 0.44f, 0.85f), V(x * 1.25f, 0.2f + MathF.Abs(x) * 0.6f, 0.9f), 0.045f, tan, 0.6f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.16f * s, 0.7f, 0.76f), V(0.3f * s, 0.73f, 0.78f), V(0.37f * s, 0.84f, 0.8f), V(0.33f * s, 0.96f, 0.82f)), 0.046f, 0.012f, horn, Shell));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.19f * s, 0.63f, 0.76f));
            b.Ell(ear, V(0.27f * s, 0.62f, 0.77f), V(0.08f, 0.036f, 0.05f), brown, V(0, 0, -15f * s));
            b.PaintEll(ear, V(0.28f * s, 0.62f, 0.8f), V(0.055f, 0.02f, 0.025f), tan, V(0, 0, -15f * s));
        });
        // The six-pointed star of the old Moldavian lands between its horns: two triangles, one pointing up
        var star = On(c, r, 0, 0.68f);
        b.Mark(head, star, Outward(c, r, star), 0.045f, 0.04f, Rgb(244, 234, 206), MarkShape.Triangle);
        b.Mark(head, star, Outward(c, r, star), 0.045f, 0.04f, Rgb(244, 234, 206), MarkShape.Triangle, 180f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.11f * s, 0.6f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.6f), 0.034f, Rgb(84, 50, 34), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Acvilet line (the golden eagle)

    /// <summary>Acvilet: a fluffy golden eaglet, white down on its breast and thighs, dark wing tips spread for balance, a hooked beak and fierce eyes.</summary>
    private static PokeBuilder C4Acvilet()
    {
        var b = new PokeBuilder("Acvilet", 0.56f, BodyPlan.Bird, V(0, 0.27f, 0)) { Coat = Fur }.Hover();
        var gold = Rgb(234, 184, 70);
        var down = Rgb(246, 242, 228);
        var dark = Rgb(76, 60, 46);
        var beak = Rgb(244, 202, 62);

        BirdLegs(b, 0.07f, 0.14f, 0.03f, 0.024f, Rgb(238, 196, 72));
        b.Ell(Body, V(0, 0.27f, 0), V(0.17f, 0.17f, 0.17f), gold);
        b.PaintEll(Body, V(0, 0.25f, 0.1f), V(0.12f, 0.13f, 0.1f), down);
        PokeBuilder.Both(s => b.Ell(Body, V(0.08f * s, 0.16f, 0.03f), V(0.07f, 0.06f, 0.07f), down, blend: 0.03f));
        FurTufts(b, Body, V(0, 0.27f, 0), V(0.17f, 0.17f, 0.17f), 14, 0.04f, 0.025f, gold, face: 0.6f, below: -0.2f, droop: 0.2f);
        int tail = b.Tail(V(0, 0.24f, -0.15f));
        TailFan(b, tail, V(0, 0.24f, -0.15f), 3, 40f, 0.12f, 0.04f, gold, dark);

        // Short wings spread out for balance, brown at their ends with a white band
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.14f * s, 0.33f, 0));
            Frond(b, w, V(0.12f * s, 0.33f, 0), V(0.36f * s, 0.37f, -0.03f), 0.075f, gold, V(0, 0.3f, 1f), 0.22f);
            for (int k = 0; k < 3; k++)
                Blade(b, w, V((0.28f + 0.02f * k) * s, 0.35f - 0.03f * k, -0.03f), V((0.45f - 0.02f * k) * s, 0.32f - 0.07f * k, -0.04f), 0.034f, dark, V(0, 0.3f, 1f), 0.26f);
            b.PaintEll(w, V(0.27f * s, 0.34f, -0.02f), V(0.05f, 0.04f, 0.05f), down);
        });

        int head = b.Head(V(0, 0.4f, 0.03f));
        var c = V(0, 0.5f, 0.05f);
        var r = V(0.15f, 0.14f, 0.14f);
        b.Ell(head, c, r, gold);
        b.PaintEll(head, V(0, 0.46f, 0.16f), V(0.11f, 0.06f, 0.06f), down);
        // Ruffled feathers on the crown, a hooked yellow beak
        foreach (var (x, z) in new[] { (0f, 0.02f), (-0.06f, -0.02f), (0.06f, -0.02f), (0f, -0.08f) })
            b.Spike(head, V(x, 0.6f, z), V(x * 1.5f, 0.69f, z - 0.05f), 0.035f, gold, 0.6f);
        b.Spike(head, V(0, 0.48f, 0.16f), V(0, 0.44f, 0.27f), 0.042f, beak, 0.8f, Shell);
        b.Spike(head, V(0, 0.455f, 0.25f), V(0, 0.415f, 0.255f), 0.016f, beak, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.07f * s, 0.52f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.4f), 0.038f, Rgb(244, 196, 40), glare: true);
        });
        return b;
    }

    /// <summary>Acviltor: the summit eagle, dark brown with a golden cape over its nape, wings mid-beat with bronze-edged primaries, a great hooked beak.</summary>
    private static PokeBuilder C4Acviltor()
    {
        var b = new PokeBuilder("Acviltor", 0.95f, BodyPlan.Bird, V(0, 0.5f, 0)) { Coat = Fur }.Hover();
        var brown = Rgb(74, 56, 42);
        var gold = Rgb(222, 172, 70);
        var white = Rgb(242, 238, 228);
        var bronze = Rgb(184, 122, 70);
        var beak = Rgb(244, 196, 56);

        BirdLegs(b, 0.11f, 0.24f, 0.04f, 0.04f, Rgb(238, 192, 70));
        PokeBuilder.Both(s => b.Ell(Body, V(0.12f * s, 0.28f, 0.04f), V(0.1f, 0.1f, 0.1f), white, blend: 0.03f));
        b.Ell(Body, V(0, 0.52f, 0), V(0.24f, 0.3f, 0.23f), brown);
        b.PaintEll(Body, V(0, 0.7f, 0.15f), V(0.1f, 0.1f, 0.09f), white);
        // The golden cape over its nape and shoulders, feathered at its edge
        b.Ell(Body, V(0, 0.76f, -0.02f), V(0.22f, 0.14f, 0.18f), gold, blend: 0.03f);
        foreach (float a in new[] { -60f, -30f, 0f, 30f, 60f })
        {
            float t = a * Degree;
            var root = V(MathF.Sin(t) * 0.18f, 0.7f, -MathF.Cos(t) * 0.14f - 0.02f);
            b.Spike(Body, root, root + V(MathF.Sin(t) * 0.05f, -0.1f, -MathF.Cos(t) * 0.05f), 0.04f, gold, 0.5f);
        }
        int tail = b.Tail(V(0, 0.36f, -0.2f));
        TailFan(b, tail, V(0, 0.36f, -0.2f), 5, 50f, 0.3f, 0.055f, brown, gold, 0.4f);

        // Wings raised mid-beat: golden coverts, brown flight feathers tipped in bronze
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.2f * s, 0.66f, 0));
            Frond(b, w, V(0.16f * s, 0.62f, 0), V(0.62f * s, 0.66f, -0.06f), 0.15f, brown, V(0, 0.2f, 1f), 0.16f);
            Frond(b, w, V(0.16f * s, 0.68f, 0), V(0.54f * s, 0.86f, -0.05f), 0.095f, gold, V(0, 0.2f, 1f), 0.24f);
            for (int k = 0; k < 5; k++)
            {
                var root = V((0.42f + 0.04f * k) * s, 0.8f - 0.07f * k, -0.05f);
                var dir = Vector3.Normalize(V((0.25f - 0.03f * k) * s, -0.2f - 0.06f * k, -0.02f));
                var tip = root + dir * 0.3f;
                Blade(b, w, root, tip, 0.045f, brown, V(0, 0.2f, 1f), 0.24f);
                Blade(b, w, tip - dir * 0.09f, tip + dir * 0.03f, 0.032f, bronze, V(0, 0.2f, 1f), 0.3f, Metal);
            }
        });

        int head = b.Head(V(0, 0.86f, 0.1f));
        var c = V(0, 0.97f, 0.15f);
        var r = V(0.15f, 0.14f, 0.16f);
        b.Ell(head, c, r, brown);
        b.Ell(head, V(0, 1.02f, 0.07f), V(0.17f, 0.14f, 0.16f), gold, blend: 0.03f);
        foreach (float x in new[] { -0.08f, 0f, 0.08f })
            b.Spike(head, V(x, 1.08f, 0.02f), V(x * 1.6f, 1.14f, -0.14f), 0.04f, gold, 0.5f);
        b.Spike(head, V(0, 0.96f, 0.27f), V(0, 0.9f, 0.43f), 0.06f, beak, 0.8f, Shell);
        b.Spike(head, V(0, 0.92f, 0.41f), V(0, 0.865f, 0.415f), 0.02f, beak, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.08f * s, 0.99f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.5f), 0.034f, Rgb(240, 120, 40), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Huhurez (the Ural owl)

    /// <summary>Huhurez: a pale grey owl streaked dark, a round flat face ringed in grey with small dark eyes, long barred tail and a crescent moon on its breast.</summary>
    private static PokeBuilder C4Huhurez()
    {
        var b = new PokeBuilder("Huhurez", 0.8f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var grey = Rgb(198, 202, 210);
        var pale = Rgb(232, 234, 240);
        var streak = Rgb(86, 88, 104);
        var rim = Rgb(76, 78, 94);

        BirdLegs(b, 0.07f, 0.14f, 0.05f, 0.026f, Rgb(150, 150, 162));
        var bc = V(0, 0.42f, 0);
        var br = V(0.2f, 0.3f, 0.19f);
        b.Ell(Body, bc, br, grey);
        // A crescent moon on its breast: a dark round with a grey one laid over most of it
        b.PaintEll(Body, V(-0.05f, 0.44f, 0.17f), V(0.075f, 0.075f, 0.06f), streak);
        b.PaintEll(Body, V(-0.022f, 0.455f, 0.18f), V(0.065f, 0.068f, 0.07f), grey);
        foreach (var (x, y) in new[] { (0.06f, 0.52f), (0.1f, 0.4f), (0.05f, 0.3f), (-0.1f, 0.3f), (0.12f, 0.6f), (-0.12f, 0.58f), (0.02f, 0.22f), (-0.03f, 0.6f) })
        {
            var at = On(bc, br, x, y);
            b.Mark(Body, at, Outward(bc, br, at), 0.03f, 0.008f, streak, MarkShape.Bar, 90f);
        }
        // Folded wings, barred dark, and a long tail barred in bands
        PokeBuilder.Both(s =>
        {
            int w = FoldedWing(b, s, V(0.17f * s, 0.6f, 0.02f), V(0.17f * s, 0.22f, -0.13f), 0.1f, grey, streak);
            foreach (float y in new[] { 0.5f, 0.42f, 0.34f })
                b.Mark(w, V(0.2f * s, y, 0.02f + (y - 0.6f) * 0.395f), V(s, 0, 0.1f), 0.04f, 0.009f, streak, MarkShape.Bar, 20f * s);
        });
        int tail = b.Tail(V(0, 0.24f, -0.14f));
        Frond(b, tail, V(0, 0.26f, -0.13f), V(0, 0.05f, -0.32f), 0.08f, grey, V(0, 0.6f, -0.8f), 0.22f);
        foreach (float t in new[] { 0.45f, 0.75f })
            b.PaintEll(tail, Vector3.Lerp(V(0, 0.26f, -0.13f), V(0, 0.05f, -0.32f), t), V(0.09f, 0.012f, 0.03f), streak, V(-40f, 0, 0));

        int head = b.Head(V(0, 0.66f, 0));
        var c = V(0, 0.8f, 0.02f);
        var r = V(0.17f, 0.16f, 0.16f);
        b.Ell(head, c, r, grey);
        // The flat round face disc, pale, ringed in a dark line
        var dc = V(0, 0.79f, 0.1f);
        var dr = V(0.14f, 0.13f, 0.08f);
        b.Ell(head, dc, dr, pale, blend: 0.02f);
        b.PaintTorus(head, V(0, 0.79f, 0.12f), 0.13f, 0.013f, rim, V(90f, 0, 0), sx: 1.05f);
        b.Spike(head, V(0, 0.79f, 0.17f), V(0, 0.735f, 0.205f), 0.024f, Rgb(70, 70, 80), mat: Shell);
        PokeBuilder.Both(s => b.Spike(head, V(0.1f * s, 0.92f, 0.02f), V(0.15f * s, 1.01f, -0.02f), 0.03f, grey, 0.6f));
        foreach (var (x, y) in new[] { (-0.06f, 0.93f), (0.05f, 0.94f), (0f, 0.95f) })
            b.Mark(head, V(x, y, 0.07f), V(x * 3f, 1f, 0.5f), 0.012f, 0.012f, streak);
        PokeBuilder.Both(s =>
        {
            var at = On(dc, dr, 0.055f * s, 0.815f);
            b.Eye(head, at, Outward(dc, dr, at), 0.028f, Rgb(36, 36, 46));
        });
        return b;
    }

    // ------------------------------------------------------------------ Ciocbit line (the woodpecker)

    /// <summary>Ciocbit: a small spotted woodpecker, black and white with a red cap, a long chisel beak, a stiff tail, little drum cups at its ears and a drum on its breast.</summary>
    private static PokeBuilder C4Ciocbit()
    {
        var b = new PokeBuilder("Ciocbit", 0.46f, BodyPlan.Bird, V(0, 0.24f, 0)) { Coat = Fur }.Hover();
        var black = Rgb(52, 52, 62);
        var red = Rgb(230, 64, 40);
        var grey = Rgb(150, 152, 162);
        var beak = Rgb(134, 130, 126);

        BirdLegs(b, 0.05f, 0.11f, 0.0f, 0.016f, Rgb(124, 120, 126));
        var bc = V(0, 0.25f, -0.02f);
        var br = V(0.12f, 0.15f, 0.13f);
        b.Ell(Body, bc, br, black);
        b.PaintEll(Body, V(0, 0.24f, 0.08f), V(0.09f, 0.12f, 0.08f), White);
        var drum = On(bc, br, 0, 0.23f);
        b.Mark(Body, drum, Outward(bc, br, drum), 0.034f, 0.034f, grey, MarkShape.Ring);
        foreach (var d in new[] { V(0.4f, 0.3f, -0.85f), V(-0.4f, 0.3f, -0.85f), V(0.2f, 0.6f, -0.7f), V(-0.2f, 0.6f, -0.7f) })
        {
            var at = Out(bc, br, default, d);
            b.Mark(Body, at, Outward(bc, br, at), 0.013f, 0.013f, White);
        }
        PokeBuilder.Both(s =>
        {
            int w = FoldedWing(b, s, V(0.1f * s, 0.32f, 0.0f), V(0.12f * s, 0.16f, -0.14f), 0.06f, black, grey);
            b.Mark(w, V(0.128f * s, 0.24f, -0.06f), V(s, 0, 0.1f), 0.035f, 0.008f, White, MarkShape.Bar, 30f * s);
        });
        int tail = b.Tail(V(0, 0.17f, -0.12f));
        Blade(b, tail, V(0, 0.17f, -0.12f), V(0, 0.03f, -0.24f), 0.04f, black, V(0, 0.6f, -0.8f), 0.28f);

        int head = b.Head(V(0, 0.36f, 0.0f));
        var c = V(0, 0.44f, 0.02f);
        var r = V(0.1f, 0.1f, 0.1f);
        b.Ell(head, c, r, black);
        b.PaintEll(head, V(0, 0.42f, 0.08f), V(0.09f, 0.035f, 0.05f), White);
        // The red cap, ruffled back into a crest
        b.Ell(head, V(0, 0.5f, -0.005f), V(0.098f, 0.065f, 0.1f), red, blend: 0.02f);
        foreach (var (x, y) in new[] { (0f, 0.53f), (-0.03f, 0.51f), (0.03f, 0.51f) })
            b.Spike(head, V(x, y, -0.03f), V(x * 1.4f, y + 0.07f, -0.11f), 0.03f, red, 0.6f);
        // The chisel of a beak, banded dark
        b.Spike(head, V(0, 0.44f, 0.08f), V(0, 0.43f, 0.26f), 0.028f, beak, 0.75f, Shell);
        foreach (float z in new[] { 0.15f, 0.2f })
            b.PaintEll(head, V(0, 0.437f, z), V(0.03f, 0.03f, 0.008f), Rgb(72, 68, 70), soft: 0.004f);
        PokeBuilder.Both(s => b.Ell(head, V(0.098f * s, 0.44f, -0.005f), V(0.026f, 0.04f, 0.04f), grey, mat: Metal, blend: 0.01f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.058f * s, 0.46f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.3f), 0.026f, Rgb(70, 44, 36));
        });
        return b;
    }

    /// <summary>Ciocanor: the drummer, boldly barred black and white, a scarlet crest swept back, a steel hammer of a beak, a steel brow plate and steel-tipped tail.</summary>
    private static PokeBuilder C4Ciocanor()
    {
        var b = new PokeBuilder("Ciocanor", 0.82f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var black = Rgb(48, 48, 58);
        var red = Rgb(226, 56, 36);
        var steel = Rgb(178, 186, 198);

        BirdLegs(b, 0.08f, 0.18f, 0.0f, 0.026f, Rgb(86, 88, 98));
        var bc = V(0, 0.42f, -0.03f);
        var br = V(0.18f, 0.25f, 0.2f);
        b.Ell(Body, bc, br, black);
        b.PaintEll(Body, V(0, 0.4f, 0.1f), V(0.13f, 0.2f, 0.12f), White);
        foreach (float y in new[] { 0.22f, 0.28f, 0.34f })
        {
            var at = On(bc, br, 0, y);
            b.Mark(Body, at, Outward(bc, br, at), 0.07f, 0.011f, black, MarkShape.Bar);
        }
        // The steel drum on its breast that rings with its strikes
        b.Ell(Body, V(0, 0.47f, 0.16f), V(0.075f, 0.075f, 0.035f), steel, mat: Metal, blend: 0.01f);
        b.Mark(Body, V(0, 0.47f, 0.195f), V(0, 0, 1f), 0.045f, 0.045f, Rgb(92, 98, 112), MarkShape.Ring);
        PokeBuilder.Both(s =>
        {
            int w = FoldedWing(b, s, V(0.15f * s, 0.56f, 0.0f), V(0.18f * s, 0.24f, -0.22f), 0.09f, black, steel);
            foreach (float y in new[] { 0.48f, 0.4f })
                b.Mark(w, V(0.2f * s, y, -0.07f), V(s, 0, 0.1f), 0.05f, 0.01f, White, MarkShape.Bar, 30f * s);
        });
        int tail = b.Tail(V(0, 0.26f, -0.18f));
        Blade(b, tail, V(0, 0.27f, -0.18f), V(0, 0.05f, -0.38f), 0.06f, black, V(0, 0.65f, -0.75f), 0.26f);
        Blade(b, tail, V(0, 0.09f, -0.34f), V(0, 0.02f, -0.41f), 0.04f, steel, V(0, 0.65f, -0.75f), 0.3f, Metal);

        int head = b.Head(V(0, 0.62f, 0.0f));
        var c = V(0, 0.74f, 0.02f);
        var r = V(0.14f, 0.14f, 0.14f);
        b.Ell(head, c, r, black);
        b.PaintEll(head, V(0, 0.71f, 0.11f), V(0.13f, 0.05f, 0.07f), White);
        // The steel plate on its brow, riveted, and the scarlet crest swept back over it
        b.Ell(head, V(0, 0.82f, 0.07f), V(0.12f, 0.05f, 0.1f), steel, mat: Metal, blend: 0.01f);
        PokeBuilder.Both(s => b.Mark(head, V(0.06f * s, 0.84f, 0.155f), V(0.3f * s, 0.6f, 1f), 0.009f, 0.009f, Rgb(92, 98, 112)));
        for (int k = 0; k < 4; k++)
            Blade(b, head, V(0, 0.84f - 0.025f * k, -0.01f - 0.035f * k), V(0, 0.99f - 0.05f * k, -0.24f - 0.05f * k), 0.05f, red, V(1f, 0, 0), 0.3f);
        // The hammer: a steel shaft of a beak ending in a block
        b.Limb(head, V(0, 0.73f, 0.12f), V(0, 0.72f, 0.3f), 0.032f, 0.028f, steel, Metal);
        b.Box(head, V(0, 0.72f, 0.34f), V(0.045f, 0.07f, 0.04f), 0.014f, steel, mat: Metal, blend: 0.006f);
        PokeBuilder.Both(s => b.Ell(head, V(0.135f * s, 0.74f, -0.005f), V(0.036f, 0.056f, 0.056f), steel, mat: Metal, blend: 0.01f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.072f * s, 0.75f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.3f), 0.032f, Rgb(106, 78, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Tritonel line (the crested newt)

    /// <summary>Tritonel: a small olive newt, a bright orange belly spotted dark, a wavy crest along its back and a flat tail; droplets on its skin.</summary>
    private static PokeBuilder C4Tritonel()
    {
        var b = new PokeBuilder("Tritonel", 0.48f, BodyPlan.Quadruped, V(0, 0.14f, -0.02f)) { Coat = Scales };
        var olive = Rgb(124, 116, 62);
        var belly = Rgb(246, 140, 46);
        var spot = Rgb(72, 56, 36);
        var crest = Rgb(200, 142, 76);

        StubbyLegs(b, 0.1f, 0.1f, 0.1f, -0.1f, 0.028f, olive);
        var bc = V(0, 0.14f, 0);
        var br = V(0.11f, 0.09f, 0.17f);
        b.Ell(Body, bc, br, olive);
        b.PaintEll(Body, V(0, 0.08f, 0.04f), V(0.09f, 0.06f, 0.16f), belly);
        foreach (var d in new[] { V(0.5f, -0.5f, 0.6f), V(-0.5f, -0.45f, 0.5f), V(0.65f, -0.4f, 0.1f), V(-0.65f, -0.4f, 0.15f), V(0.6f, -0.45f, -0.3f), V(-0.6f, -0.5f, -0.25f) })
        {
            var at = Out(bc, br, default, d);
            b.Mark(Body, at, Outward(bc, br, at), 0.012f, 0.012f, spot);
        }
        foreach (var (y, z) in new[] { (0.226f, 0.08f), (0.23f, 0.0f), (0.224f, -0.08f) })
            b.Ell(Body, V(0, y, z), V(0.011f, 0.028f, 0.034f), crest, blend: 0.01f);
        foreach (var d in new[] { V(0.4f, 0.8f, 0.2f), V(-0.3f, 0.8f, -0.3f) })
        {
            var at = Out(bc, br, default, d);
            b.Mark(Body, at, Outward(bc, br, at), 0.01f, 0.013f, Rgb(214, 236, 240));
        }
        int tail = b.Tail(V(0, 0.14f, -0.15f));
        Frond(b, tail, V(0, 0.14f, -0.13f), V(0, 0.1f, -0.42f), 0.052f, olive, V(1f, 0, 0), 0.3f);
        foreach (var (y, z) in new[] { (0.185f, -0.2f), (0.17f, -0.28f), (0.152f, -0.36f) })
            b.Ell(tail, V(0, y, z), V(0.01f, 0.024f, 0.03f), crest, blend: 0.01f);

        int head = b.Head(V(0, 0.18f, 0.14f));
        var c = V(0, 0.2f, 0.2f);
        var r = V(0.1f, 0.085f, 0.095f);
        b.Ell(head, c, r, olive);
        b.PaintEll(head, V(0, 0.14f, 0.24f), V(0.085f, 0.04f, 0.06f), belly);
        foreach (var (x, y) in new[] { (0.03f, 0.13f), (-0.035f, 0.135f), (0f, 0.12f) })
            b.Mark(head, V(x, y, 0.255f), V(x * 3f, -0.6f, 1f), 0.009f, 0.009f, spot);
        var mouth = On(c, r, 0, 0.175f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.05f, 0.006f, PixelCanvas.Shadow(olive, 0.5f), MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.06f * s, 0.245f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.3f), 0.03f, Rgb(96, 62, 42));
        });
        return b;
    }

    /// <summary>Tritanor: a great crested newt on its hind legs, dark and warty over a spotted orange belly, a tall jagged crest tipped with violet glands.</summary>
    private static PokeBuilder C4Tritanor()
    {
        var b = new PokeBuilder("Tritanor", 0.9f, BodyPlan.Biped, V(0, 0.52f, 0)) { Coat = Scales };
        var dark = Rgb(72, 74, 46);
        var belly = Rgb(238, 132, 44);
        var spot = Rgb(48, 40, 30);
        var crest = Rgb(150, 116, 56);
        var violet = Rgb(172, 98, 216);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.14f * s, 0.38f, 0.0f));
            b.Limb(leg, V(0.14f * s, 0.38f, 0), V(0.19f * s, 0.2f, 0.06f), 0.1f, 0.08f, dark);
            b.Limb(leg, V(0.19f * s, 0.2f, 0.06f), V(0.18f * s, 0.07f, 0.04f), 0.075f, 0.065f, dark);
            b.Ell(leg, V(0.18f * s, 0.035f, 0.1f), V(0.09f, 0.035f, 0.12f), dark);
            foreach (float k in new[] { -0.05f, 0f, 0.05f })
                b.Spike(leg, V((0.18f + k) * s, 0.025f, 0.18f), V((0.18f + k * 1.4f) * s, 0.015f, 0.24f), 0.02f, Rgb(214, 204, 176), mat: Shell, blend: 0.004f);
        });
        var bc = V(0, 0.56f, 0.02f);
        var br = V(0.24f, 0.3f, 0.2f);
        b.Ell(Body, bc, br, dark);
        b.PaintEll(Body, V(0, 0.52f, 0.12f), V(0.18f, 0.26f, 0.12f), belly);
        foreach (var (x, y) in new[] { (0.06f, 0.62f), (-0.08f, 0.58f), (0.1f, 0.48f), (-0.04f, 0.44f), (0.02f, 0.36f), (-0.11f, 0.4f), (0.0f, 0.68f), (0.12f, 0.6f) })
        {
            var at = On(bc, br, x, y);
            b.Mark(Body, at, Outward(bc, br, at), 0.018f, 0.018f, spot);
        }
        // Warts over its back and shoulders
        foreach (var d in new[] { V(0.8f, 0.4f, -0.2f), V(-0.8f, 0.35f, -0.25f), V(0.7f, 0.1f, -0.5f), V(-0.7f, 0.05f, -0.5f), V(0.6f, 0.6f, 0.1f), V(-0.6f, 0.6f, 0.05f) })
            b.Ell(Body, Out(bc, br, default, d), V(0.022f, 0.022f, 0.022f), PixelCanvas.Shadow(dark, 0.15f), blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.2f * s, 0.7f, 0.06f));
            b.Limb(arm, V(0.2f * s, 0.7f, 0.06f), V(0.29f * s, 0.52f, 0.15f), 0.052f, 0.042f, dark);
            b.Ell(arm, V(0.3f * s, 0.49f, 0.17f), V(0.045f, 0.04f, 0.045f), dark);
            Digits(b, arm, V(0.3f * s, 0.46f, 0.19f), V(0.1f * s, -1f, 0.4f), V(0.6f * s, 0, 0), 0.05f, 0.013f, dark);
        });
        int tail = b.Tail(V(0, 0.4f, -0.16f));
        var tailPath = Smooth(3, V(0, 0.4f, -0.16f), V(0, 0.22f, -0.4f), V(0.06f, 0.09f, -0.62f), V(0.16f, 0.055f, -0.78f));
        b.Tube(tail, tailPath, 0.12f, 0.03f, dark, blend: 0.01f);

        // The jagged crest from its crown along its back to the tail, each point tipped with a violet gland
        void Point(int bone, Vector3 root, Vector3 tip, float w)
        {
            Blade(b, bone, root, tip, w, crest, V(1f, 0, 0), 0.25f);
            b.Ell(bone, tip, V(0.026f, 0.03f, 0.026f), violet, mat: Glow, blend: 0.01f);
        }
        foreach (var (y, z) in new[] { (0.82f, -0.08f), (0.72f, -0.148f), (0.62f, -0.176f), (0.52f, -0.178f) })
            Point(Body, V(0, y, z), V(0, y + 0.16f, z - 0.18f), 0.07f);
        for (int i = 2; i < tailPath.Length - 2; i += 2)
        {
            var at = tailPath[i];
            float t = i / (float)(tailPath.Length - 1);
            float rad = 0.12f + (0.03f - 0.12f) * t;
            Point(tail, at + V(0, rad * 0.8f, 0), at + V(0, rad * 0.8f + 0.12f * (1f - t * 0.6f), -0.08f), 0.05f * (1f - t * 0.5f));
        }

        int head = b.Head(V(0, 0.84f, 0.06f));
        var c = V(0, 0.94f, 0.12f);
        var r = V(0.2f, 0.14f, 0.18f);
        b.Ell(head, c, r, dark);
        b.PaintEll(head, V(0, 0.86f, 0.2f), V(0.17f, 0.07f, 0.12f), belly);
        Point(head, V(0, 1.06f, 0.04f), V(0, 1.22f, -0.08f), 0.06f);
        var mouth = On(c, r, 0, 0.9f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.13f, 0.01f, PixelCanvas.Shadow(dark, 0.5f), MarkShape.Bar);
        foreach (var d in new[] { V(0.5f, 0.7f, 0.3f), V(-0.5f, 0.7f, 0.2f), V(0.1f, 0.9f, -0.1f) })
            b.Ell(head, Out(c, r, default, d), V(0.018f, 0.018f, 0.018f), PixelCanvas.Shadow(dark, 0.15f), blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.12f * s, 0.98f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.5f), 0.034f, Rgb(240, 196, 52), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Viperin line (the horned viper)

    /// <summary>Viperin: a short, thick grey viper coiled ready, a dark zigzag down its back, a broad arrow head, slit eyes and a soft bump on its snout.</summary>
    private static PokeBuilder C4Viperin()
    {
        var grey = Rgb(106, 104, 100);
        var zig = Rgb(40, 38, 44);
        var belly = Rgb(194, 188, 180);
        var coil = Spiral3(0.2f, 0.11f, 0.065f, 0.13f, 2.4f, 1.3f, 18);
        var points = new[] { V(0.14f, 0.13f, -0.3f), V(0.18f, 0.08f, -0.23f) }.Concat(coil)
            .Concat(new[] { V(0, 0.2f, 0.05f), V(0, 0.3f, 0.08f), V(0, 0.37f, 0.1f) }).ToArray();
        var b = new PokeBuilder("Viperin", 0.62f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        Func<float, float> radius = t => 0.026f + 0.046f * MathF.Min(1f, t * 3f);
        var bones = C4Coils(b, points, radius, grey);
        C4Zigzag(b, points, bones, radius, zig, 1, points.Length - 2);
        int neck = bones[^1];
        b.PaintEll(neck, V(0, 0.28f, 0.14f), V(0.04f, 0.1f, 0.035f), belly);

        int head = b.Head(points[^1], neck);
        var c = V(0, 0.41f, 0.14f);
        var r = V(0.08f, 0.05f, 0.1f);
        b.Ell(head, c, r, grey);
        b.Ell(head, V(0, 0.4f, 0.09f), V(0.1f, 0.048f, 0.06f), grey, blend: 0.02f);
        b.Ell(head, V(0, 0.425f, 0.235f), V(0.016f, 0.012f, 0.012f), PixelCanvas.Light1(grey, 0.15f), blend: 0.008f);
        PokeBuilder.Both(s => b.PaintEll(head, V(0.065f * s, 0.41f, 0.1f), V(0.03f, 0.012f, 0.06f), zig));
        b.PaintEll(head, V(0, 0.45f, 0.08f), V(0.03f, 0.02f, 0.05f), zig);
        var mouth = On(c, r, 0, 0.392f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.04f, 0.006f, PixelCanvas.Shadow(grey, 0.5f), MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.052f * s, 0.422f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0.3f, 0.4f), 0.022f, sclera: true, white: Rgb(206, 200, 150), pupil: Rgb(30, 30, 30), glare: true);
        });
        return b;
    }

    /// <summary>Cornviper: the long, heavy viper of the rocky hills, ash grey with a black zigzag, craggy limestone scales on its flanks and a stone horn on its snout; fangs drip violet.</summary>
    private static PokeBuilder C4Cornviper()
    {
        var grey = Rgb(118, 116, 110);
        var zig = Rgb(36, 34, 40);
        var belly = Rgb(198, 192, 182);
        var stone = Rgb(176, 172, 160);
        var violet = Rgb(160, 84, 210);
        var coil = Spiral3(0.3f, 0.16f, 0.095f, 0.18f, 2.2f, 1.35f, 22);
        var points = new[] { V(0.36f, 0.3f, -0.44f), V(0.36f, 0.17f, -0.4f), V(0.32f, 0.1f, -0.33f) }.Concat(coil)
            .Concat(new[] { V(0, 0.3f, 0.06f), V(0, 0.46f, 0.1f), V(0, 0.6f, 0.1f), V(0, 0.7f, 0.12f) }).ToArray();
        var b = new PokeBuilder("Cornviper", 0.96f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        Func<float, float> radius = t => 0.034f + 0.066f * MathF.Min(1f, t * 2.5f);
        var bones = C4Coils(b, points, radius, grey, 3);
        C4Zigzag(b, points, bones, radius, zig, 2, points.Length - 2);
        int neck = bones[^1];
        b.PaintEll(neck, V(0, 0.52f, 0.17f), V(0.06f, 0.18f, 0.05f), belly);
        // Craggy plates of limestone along its flanks and up its tail
        for (int i = 3; i < points.Length - 3; i += 2)
        {
            var dir = Vector3.Normalize(points[i + 1] - points[i]);
            var side = Vector3.Cross(dir, Vector3.UnitY);
            if (side.LengthSquared() < 1e-4f) continue;
            side = Vector3.Normalize(side);
            float rr = radius(i / (float)(points.Length - 1));
            foreach (float w in new[] { -1f, 1f })
            {
                var root = points[i] + side * rr * 0.8f * w + V(0, rr * 0.35f, 0);
                b.Spike(bones[i], root, root + side * rr * 0.45f * w + V(0, rr * 0.35f, 0), rr * 0.32f, stone, 0.55f, Shell, 0.008f);
            }
        }
        int tail = b.Tail(points[0], Body);
        foreach (var (dy, dx) in new[] { (0.0f, 0.02f), (-0.06f, -0.025f), (-0.12f, 0.02f) })
            b.Spike(tail, points[0] + V(dx, dy, 0), points[0] + V(dx * 2.5f, dy + 0.06f, -0.04f), 0.03f, stone, 0.6f, Shell);

        int head = b.Head(points[^1], neck);
        var c = V(0, 0.76f, 0.18f);
        var r = V(0.11f, 0.07f, 0.14f);
        b.Ell(head, c, r, grey);
        b.Ell(head, V(0, 0.75f, 0.11f), V(0.13f, 0.066f, 0.08f), grey, blend: 0.02f);
        PokeBuilder.Both(s => b.PaintEll(head, V(0.09f * s, 0.77f, 0.13f), V(0.04f, 0.016f, 0.08f), zig));
        Gape(b, head, V(0, 0.73f, 0.27f), V(0.065f, 0.028f, 0.05f), Rgb(150, 62, 166), 0.032f);
        b.Spike(head, V(0, 0.79f, 0.29f), V(0, 0.93f, 0.33f), 0.04f, stone, mat: Shell);
        PokeBuilder.Both(s => b.Spike(head, V(0.08f * s, 0.81f, 0.1f), V(0.12f * s, 0.86f, 0.04f), 0.025f, stone, 0.6f, Shell));
        // Violet venom dripping from its fangs
        PokeBuilder.Both(s => b.Limb(head, V(0.027f * s, 0.725f, 0.275f), V(0.03f * s, 0.65f, 0.29f), 0.01f, 0.008f, violet, Glow, 0.004f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.07f * s, 0.79f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0.2f, 0.4f), 0.026f, sclera: true, white: Rgb(214, 210, 176), pupil: Rgb(30, 30, 30), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Radascuta line (the stag beetle)

    /// <summary>Radascuta: a small young stag beetle, a glossy chestnut dome over a cream body, a determined cream face and stubby pincers.</summary>
    private static PokeBuilder C4Radascuta()
    {
        var b = new PokeBuilder("Radascuta", 0.46f, BodyPlan.Quadruped, V(0, 0.17f, -0.03f)) { Coat = Shell };
        var cream = Rgb(242, 226, 184);
        var chestnut = Rgb(150, 72, 44);
        var leg = Rgb(86, 56, 42);

        foreach (var (z, front) in new[] { (0.06f, true), (-0.13f, false) })
            PokeBuilder.Both(s =>
            {
                int l = b.Leg(s, V(0.1f * s, 0.1f, z), front);
                b.Limb(l, V(0.1f * s, 0.1f, z), V(0.16f * s, 0.06f, z + 0.01f), 0.024f, 0.02f, leg);
                b.Limb(l, V(0.16f * s, 0.06f, z + 0.01f), V(0.17f * s, 0.018f, z + 0.03f), 0.02f, 0.018f, leg);
            });
        PokeBuilder.Both(s =>
        {
            b.Limb(Body, V(0.1f * s, 0.1f, -0.035f), V(0.16f * s, 0.055f, -0.035f), 0.022f, 0.018f, leg);
            b.Limb(Body, V(0.16f * s, 0.055f, -0.035f), V(0.17f * s, 0.018f, -0.02f), 0.018f, 0.016f, leg);
        });
        b.Ell(Body, V(0, 0.15f, -0.03f), V(0.14f, 0.11f, 0.16f), cream, mat: Fur);
        foreach (float z in new[] { -0.07f, -0.12f })
            b.PaintTorus(Body, V(0, 0.15f, z), 0.12f, 0.006f, PixelCanvas.Shadow(cream, 0.2f), V(90f, 0, 0));
        // The glossy dome of its wing cases, parted down the middle, and the shield behind its head
        b.Ell(Body, V(0, 0.21f, -0.05f), V(0.155f, 0.1f, 0.16f), chestnut, blend: 0.01f);
        b.PaintEll(Body, V(0, 0.31f, -0.05f), V(0.006f, 0.03f, 0.17f), PixelCanvas.Shadow(chestnut, 0.4f), soft: 0.005f);
        b.Ell(Body, V(0, 0.22f, 0.09f), V(0.12f, 0.075f, 0.06f), chestnut, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.12f, 0.09f));
            b.Limb(arm, V(0.09f * s, 0.12f, 0.09f), V(0.14f * s, 0.08f, 0.15f), 0.022f, 0.019f, chestnut);
            b.Spike(arm, V(0.14f * s, 0.08f, 0.15f), V(0.15f * s, 0.05f, 0.19f), 0.018f, leg, mat: Shell);
        });

        int head = b.Head(V(0, 0.16f, 0.12f));
        var c = V(0, 0.15f, 0.16f);
        var r = V(0.1f, 0.085f, 0.075f);
        b.Ell(head, c, r, cream, mat: Fur);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.045f * s, 0.1f, 0.21f), V(0.08f * s, 0.085f, 0.26f), V(0.05f * s, 0.07f, 0.29f)), 0.017f, 0.007f, chestnut));
        PokeBuilder.Both(s => Antenna(b, head, V(0.045f * s, 0.21f, 0.17f), V(0.075f * s, 0.27f, 0.2f), V(0.095f * s, 0.3f, 0.22f), 0.007f, leg, chestnut, 0.012f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, 0.165f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.5f), 0.025f, Rgb(70, 44, 32), glare: true);
        });
        return b;
    }

    /// <summary>Radaskon: the stag beetle knight upright, its wing cases chestnut with a bronze sheen, an iron helm and great antlered mandibles of iron tipped in bronze.</summary>
    private static PokeBuilder C4Radaskon()
    {
        var b = new PokeBuilder("Radaskon", 0.96f, BodyPlan.Biped, V(0, 0.6f, -0.02f)) { Coat = Shell };
        var cream = Rgb(240, 224, 182);
        var chestnut = Rgb(134, 62, 38);
        var iron = Rgb(76, 74, 82);
        var bronze = Rgb(198, 130, 64);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.17f * s, 0.42f, -0.02f));
            b.Limb(leg, V(0.17f * s, 0.42f, -0.02f), V(0.24f * s, 0.22f, 0.06f), 0.1f, 0.085f, chestnut);
            b.Limb(leg, V(0.24f * s, 0.22f, 0.06f), V(0.22f * s, 0.07f, 0.02f), 0.075f, 0.065f, chestnut);
            b.Ell(leg, V(0.22f * s, 0.04f, 0.08f), V(0.09f, 0.04f, 0.12f), chestnut);
            b.Ell(leg, V(0.245f * s, 0.22f, 0.1f), V(0.07f, 0.07f, 0.05f), iron, mat: Metal, blend: 0.01f);
            foreach (float k in new[] { -0.045f, 0.045f })
                b.Spike(leg, V((0.22f + k) * s, 0.03f, 0.17f), V((0.22f + k * 1.4f) * s, 0.02f, 0.25f), 0.022f, iron, mat: Metal, blend: 0.004f);
        });
        var bc = V(0, 0.62f, 0.04f);
        var br = V(0.27f, 0.33f, 0.22f);
        b.Ell(Body, bc, br, cream, mat: Fur);
        foreach (float y in new[] { 0.44f, 0.53f, 0.62f, 0.71f })
        {
            var at = On(bc, br, 0, y);
            b.Mark(Body, at, Outward(bc, br, at), 0.13f, 0.008f, Rgb(204, 182, 140), MarkShape.Bar);
        }
        // The wing cases on its back like a shield, parted down the middle and rimmed in bronze
        b.Ell(Body, V(0, 0.68f, -0.2f), V(0.3f, 0.42f, 0.2f), chestnut, V(-12f, 0, 0), blend: 0.02f);
        b.PaintEll(Body, V(0, 0.68f, -0.4f), V(0.01f, 0.42f, 0.06f), PixelCanvas.Shadow(chestnut, 0.4f), soft: 0.006f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.29f * s, 0.66f, -0.2f), V(0.03f, 0.38f, 0.16f), bronze, V(-12f, 0, 0)));
        PokeBuilder.Both(s => b.Ell(Body, V(0.24f * s, 0.86f, 0.03f), V(0.12f, 0.09f, 0.11f), iron, mat: Metal, blend: 0.015f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.27f * s, 0.84f, 0.04f));
            b.Limb(arm, V(0.27f * s, 0.84f, 0.04f), V(0.36f * s, 0.66f, 0.12f), 0.065f, 0.058f, chestnut);
            b.Limb(arm, V(0.36f * s, 0.66f, 0.12f), V(0.38f * s, 0.5f, 0.22f), 0.07f, 0.06f, iron, Metal);
            b.Torus(arm, V(0.37f * s, 0.6f, 0.15f), 0.07f, 0.016f, bronze, V(60f, 0, 0), mat: Metal);
            b.Ell(arm, V(0.385f * s, 0.47f, 0.24f), V(0.06f, 0.055f, 0.06f), chestnut);
            Digits(b, arm, V(0.39f * s, 0.44f, 0.27f), V(0.1f * s, -1f, 0.5f), V(0.6f * s, 0, 0), 0.07f, 0.016f, iron, Metal);
        });

        int head = b.Head(V(0, 0.92f, 0.08f));
        var c = V(0, 1.04f, 0.12f);
        var r = V(0.16f, 0.14f, 0.14f);
        b.Ell(head, c, r, cream, mat: Fur);
        // The iron helm, its brow rimmed in bronze
        b.Ell(head, V(0, 1.1f, 0.06f), V(0.18f, 0.14f, 0.16f), iron, mat: Metal, blend: 0.015f);
        b.Ell(head, V(0, 1.14f, 0.18f), V(0.12f, 0.028f, 0.04f), bronze, mat: Metal, blend: 0.01f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.05f * s, 0.96f, 0.22f), V(0.09f * s, 0.94f, 0.27f), V(0.06f * s, 0.92f, 0.31f)), 0.024f, 0.01f, chestnut));
        // The antlered mandibles: iron beams that branch, each tine tipped with bronze
        PokeBuilder.Both(s =>
        {
            var beam = Smooth(3, V(0.08f * s, 1.17f, 0.17f), V(0.16f * s, 1.35f, 0.16f), V(0.22f * s, 1.52f, 0.1f), V(0.24f * s, 1.66f, 0.02f));
            b.Tube(head, beam, 0.04f, 0.022f, iron, Metal);
            b.Spike(head, beam[^1], beam[^1] + V(0.02f * s, 0.07f, -0.04f), 0.022f, bronze, mat: Metal, blend: 0.006f);
            foreach (var (from, to) in new[] { (V(0.15f, 1.33f, 0.16f), V(0.27f, 1.42f, 0.25f)), (V(0.2f, 1.47f, 0.12f), V(0.34f, 1.56f, 0.13f)), (V(0.2f, 1.47f, 0.12f), V(0.14f, 1.6f, 0.17f)) })
            {
                var a = V(from.X * s, from.Y, from.Z);
                var t = V(to.X * s, to.Y, to.Z);
                b.Spike(head, a, t, 0.022f, iron, mat: Metal);
                b.Spike(head, Vector3.Lerp(a, t, 0.7f), t + Vector3.Normalize(t - a) * 0.03f, 0.014f, bronze, mat: Metal, blend: 0.006f);
            }
            Antenna(b, head, V(0.12f * s, 1.18f, -0.02f), V(0.22f * s, 1.32f, -0.06f), V(0.32f * s, 1.4f, -0.1f), 0.009f, chestnut, chestnut, 0.014f);
        });
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.06f * s, 1.03f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.5f), 0.03f, Rgb(70, 44, 32), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Fluturel line (the Apollo butterfly)

    /// <summary>Fluturel: a velvety black caterpillar spotted orange with soft white tufts, curled asleep on a little sedum leaf, smiling.</summary>
    private static PokeBuilder C4Fluturel()
    {
        var path = new[] { V(-0.12f, 0.072f, -0.12f), V(-0.02f, 0.076f, -0.18f), V(0.09f, 0.08f, -0.14f), V(0.15f, 0.085f, -0.04f), V(0.13f, 0.09f, 0.06f), V(0.06f, 0.095f, 0.12f) };
        var b = new PokeBuilder("Fluturel", 0.44f, BodyPlan.Serpent, path[0]) { Coat = Fur };
        var black = Rgb(44, 42, 50);
        var orange = Rgb(244, 128, 40);
        var tuft = Rgb(226, 222, 216);
        var leaf = Rgb(110, 160, 104);

        // The little sedum it sleeps on: plump leaves in a rosette under it, joined at their middle
        b.Ell(Body, V(0.02f, 0.014f, -0.04f), V(0.15f, 0.014f, 0.14f), leaf, mat: Leaf, blend: 0.015f);
        foreach (var (x, z, a) in new[] { (0.0f, -0.03f, 0f), (0.12f, -0.12f, 40f), (-0.1f, -0.14f, -30f), (0.16f, 0.05f, 70f), (0.06f, 0.1f, 20f), (-0.08f, 0.02f, -60f) })
            b.Ell(Body, V(x, 0.016f, z), V(0.07f, 0.016f, 0.05f), leaf, V(0, a, 0), Leaf, 0.015f);
        int neck = Grub(b, path, 0.062f, 0.075f, black, Rgb(64, 60, 70), (i, seg, at, r) =>
        {
            // The last segment is under the head's back: its spots would lie on the head's triangles
            if (i < path.Length - 1) PokeBuilder.Both(s => b.Mark(seg, at + V(r * 0.55f * s, r * 0.8f, 0), V(0.55f * s, 0.8f, 0), r * 0.22f, r * 0.22f, orange));
            if (i % 2 == 1) PokeBuilder.Both(s => b.Spike(seg, at + V(r * 0.85f * s, r * 0.35f, 0), at + V(r * 1.3f * s, r * 0.75f, 0), r * 0.2f, tuft, 0.6f));
        });

        int head = b.Head(path[^1] + V(0, 0.02f, 0.03f), neck);
        var c = V(0.02f, 0.125f, 0.19f);
        var r = V(0.085f, 0.08f, 0.075f);
        b.Ell(head, c, r, black);
        PokeBuilder.Both(s => Antenna(b, head, V(0.02f + 0.035f * s, 0.19f, 0.18f), V(0.02f + 0.05f * s, 0.23f, 0.19f), V(0.02f + 0.07f * s, 0.25f, 0.2f), 0.008f, black, black, 0.012f));
        var smile = On(c, r, 0.02f, 0.098f);
        b.Mark(head, smile, Outward(c, r, smile), 0.022f, 0.012f, Rgb(150, 148, 160), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f + 0.04f * s, 0.13f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.3f), 0.024f, closed: true);
        });
        return b;
    }

    /// <summary>Fluturapol: a great white mountain butterfly, its broad wings spotted black with red eyespots ringed in black, a furry grey body and white-knobbed antennae.</summary>
    private static PokeBuilder C4Fluturapol()
    {
        var b = new PokeBuilder("Fluturapol", 0.84f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var wing = Rgb(242, 242, 238);
        var rim = Rgb(72, 72, 80);
        var spot = Rgb(36, 36, 42);
        var red = Rgb(228, 54, 40);
        var body = Rgb(122, 122, 130);
        var headC = Rgb(64, 64, 74);

        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.05f * s, 0.48f, -0.04f));
            FlatWing(b, w, V(0.27f * s, 0.6f, -0.05f), 0.27f, 0.18f, 26f * s, wing, rim, 0.03f);
            FlatWing(b, w, V(0.19f * s, 0.33f, -0.05f), 0.17f, 0.12f, -40f * s, wing, rim, 0.025f);
            foreach (var (x, y, sz) in new[] { (0.2f, 0.66f, 0.022f), (0.33f, 0.62f, 0.02f), (0.16f, 0.56f, 0.016f), (0.25f, 0.72f, 0.016f) })
                b.Mark(w, V(x * s, y, -0.035f), V(0, 0, 1f), sz, sz, spot);
            foreach (var (x, y, sz) in new[] { (0.36f, 0.71f, 0.038f), (0.2f, 0.3f, 0.034f) })
            {
                b.Mark(w, V(x * s, y, -0.035f), V(0, 0, 1f), sz, sz, spot);
                b.Mark(w, V(x * s, y, -0.035f), V(0, 0, 1f), sz * 0.66f, sz * 0.66f, red);
            }
        });
        // The furry body: a thorax under the head and a banded abdomen hanging below
        b.Ell(Body, V(0, 0.48f, 0), V(0.075f, 0.075f, 0.07f), body);
        b.Ell(Body, V(0, 0.32f, -0.03f), V(0.06f, 0.14f, 0.06f), body, V(12f, 0, 0));
        foreach (float y in new[] { 0.28f, 0.34f, 0.4f })
            b.PaintTorus(Body, V(0, y, -0.03f + (0.32f - y) * 0.2f), 0.058f, 0.007f, PixelCanvas.Shadow(body, 0.3f), V(-12f, 0, 0));
        FurTufts(b, Body, V(0, 0.48f, 0), V(0.075f, 0.075f, 0.07f), 10, 0.03f, 0.016f, PixelCanvas.Light1(body, 0.3f), face: 0.5f, below: -0.5f, droop: 0.3f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.44f, 0.04f));
            b.Limb(leg, V(0.04f * s, 0.44f, 0.04f), V(0.07f * s, 0.36f, 0.07f), 0.016f, 0.013f, headC);
        });

        int head = b.Head(V(0, 0.55f, 0.02f));
        var c = V(0, 0.6f, 0.04f);
        var r = V(0.07f, 0.065f, 0.065f);
        b.Ell(head, c, r, headC);
        PokeBuilder.Both(s => Antenna(b, head, V(0.025f * s, 0.65f, 0.04f), V(0.06f * s, 0.77f, 0.05f), V(0.09f * s, 0.88f, 0.04f), 0.007f, spot, White, 0.019f));
        var smile = On(c, r, 0, 0.578f);
        b.Mark(head, smile, Outward(c, r, smile), 0.016f, 0.008f, Rgb(160, 160, 170), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.032f * s, 0.612f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.3f), 0.022f, Rgb(120, 78, 60));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Bradlet line (the fir)

    /// <summary>Bradlet: a walking fir sapling, tiers of dark needles over a little brown trunk on two legs, a face peeking from under its lowest boughs, a pine cone for a tail.</summary>
    private static PokeBuilder C4Bradlet()
    {
        var b = new PokeBuilder("Bradlet", 0.56f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Leaf };
        var green = Rgb(54, 112, 66);
        var fringe = Rgb(44, 96, 58);
        var deep = Rgb(34, 78, 48);
        var trunk = Rgb(124, 84, 52);
        var cone = Rgb(142, 94, 54);

        // The tiers of boughs, from the top down so each cut carves only its own underside
        foreach (var (y, h, rad, n) in new[] { (0.63f, 0.17f, 0.09f, 6), (0.53f, 0.18f, 0.14f, 8), (0.42f, 0.2f, 0.19f, 9), (0.3f, 0.22f, 0.24f, 11) })
            C4FirTier(b, y, h, rad, green, fringe, n, y * 3f);
        b.PaintEll(Body, V(-0.06f, 0.55f, 0.1f), V(0.03f, 0.08f, 0.05f), PixelCanvas.Light1(green, 0.12f));
        b.Limb(Body, V(0, 0.14f, 0), V(0, 0.32f, 0), 0.07f, 0.06f, trunk, Shell);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.18f, 0));
            b.Limb(leg, V(0.05f * s, 0.2f, 0), V(0.07f * s, 0.05f, 0.02f), 0.05f, 0.044f, trunk, Shell);
            b.Ell(leg, V(0.075f * s, 0.03f, 0.04f), V(0.05f, 0.03f, 0.06f), trunk, mat: Shell);
        });
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.27f, 0.02f));
            b.Spike(arm, V(0.12f * s, 0.27f, 0.02f), V(0.27f * s, 0.21f, 0.05f), 0.04f, fringe, 0.6f, Leaf);
        });
        // The face peering out from the shade under its lowest tier
        int head = b.Head(V(0, 0.26f, 0.04f));
        var c = V(0, 0.25f, 0.08f);
        var r = V(0.09f, 0.06f, 0.07f);
        b.Ell(head, c, r, deep);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.255f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.6f), 0.026f, Rgb(214, 150, 88));
        });
        int tail = b.Tail(V(0, 0.24f, -0.12f));
        b.Limb(tail, V(0, 0.24f, -0.05f), V(0.05f, 0.18f, -0.2f), 0.017f, 0.015f, trunk, Shell);
        b.Ell(tail, V(0.06f, 0.13f, -0.22f), V(0.04f, 0.06f, 0.04f), cone, mat: Shell);
        foreach (float y in new[] { 0.11f, 0.14f, 0.17f })
            b.PaintTorus(tail, V(0.06f, y, -0.22f), 0.035f, 0.005f, PixelCanvas.Shadow(cone, 0.35f));
        return b;
    }

    /// <summary>Bradonor: the stately snow fir, tiers of dark boughs heavy with snow and icicles, a trunk split into two root-legs, two bough arms, frost-blue eyes and a beard of frost.</summary>
    private static PokeBuilder C4Bradonor()
    {
        var b = new PokeBuilder("Bradonor", 1f, BodyPlan.Biped, V(0, 0.7f, 0)) { Coat = Leaf };
        var green = Rgb(50, 104, 66);
        var fringe = Rgb(40, 90, 58);
        var deep = Rgb(30, 70, 46);
        var trunk = Rgb(104, 72, 50);
        var snow = Rgb(232, 240, 246);
        var ice = Rgb(176, 218, 240);
        var cone = Rgb(130, 90, 56);

        var tiers = new[] { (1.46f, 0.26f, 0.12f, 6), (1.28f, 0.26f, 0.2f, 8), (1.08f, 0.28f, 0.29f, 10), (0.86f, 0.32f, 0.38f, 12), (0.62f, 0.36f, 0.46f, 14) };
        foreach (var (y, h, rad, n) in tiers)
        {
            C4FirTier(b, y, h, rad, green, fringe, n, y * 2f);
            // Snow heaped on the slope of each tier, and icicles hanging from its rim
            for (int i = 0; i < n / 2; i++)
            {
                float a = y * 5f + i * MathF.Tau / (n / 2);
                var o = V(MathF.Sin(a), 0, MathF.Cos(a));
                b.Ell(Body, V(0, y + h * 0.18f, 0) + o * rad * 0.74f, V(rad * 0.32f, 0.032f, rad * 0.26f), snow, V(0, a / Degree, 0), Fur, 0.02f);
                var hang = V(0, y + 0.006f, 0) + o * rad * 0.92f;
                b.Spike(Body, hang, hang + V(0, -0.1f - 0.02f * (i % 2), 0), 0.018f, ice, mat: Shell, blend: 0.006f);
            }
        }
        b.Ell(Body, V(0, 1.68f, 0), V(0.05f, 0.06f, 0.05f), snow, blend: 0.02f);
        b.Limb(Body, V(0, 0.5f, 0), V(0, 0.9f, 0), 0.17f, 0.13f, trunk, Shell);
        foreach (var (x, z) in new[] { (0.3f, 0.25f), (-0.32f, 0.18f), (0.05f, -0.36f) })
            b.Ell(Body, V(x, 0.57f, z), V(0.03f, 0.045f, 0.03f), cone, mat: Shell, blend: 0.006f);
        // The trunk splits into two root-legs, their roots spread on the ground
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.58f, 0));
            b.Limb(leg, V(0.08f * s, 0.6f, 0), V(0.26f * s, 0.1f, 0.04f), 0.14f, 0.1f, trunk, Shell);
            foreach (var (dx, dz) in new[] { (0.1f, 0.12f), (-0.02f, 0.16f), (0.14f, -0.06f) })
                b.Tube(leg, new[] { V(0.26f * s, 0.1f, 0.04f), V((0.26f + dx) * s, 0.035f, 0.04f + dz) }, 0.07f, 0.03f, trunk, Shell);
        });
        // Two boughs for arms, snow on them and icicles under them
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.3f * s, 0.95f, 0.02f));
            b.Limb(arm, V(0.25f * s, 0.95f, 0.04f), V(0.62f * s, 0.66f, 0.12f), 0.07f, 0.05f, trunk, Shell);
            b.Ell(arm, V(0.46f * s, 0.84f, 0.08f), V(0.22f, 0.07f, 0.12f), green, V(0, 0, -38f * s), Leaf, 0.03f);
            b.Ell(arm, V(0.46f * s, 0.89f, 0.08f), V(0.17f, 0.04f, 0.1f), snow, V(0, 0, -38f * s), Fur, 0.02f);
            foreach (var (dx, dy) in new[] { (0.06f, -0.06f), (0f, -0.12f), (-0.05f, -0.1f) })
                b.Tube(arm, Smooth(3, V(0.62f * s, 0.66f, 0.12f), V((0.66f + dx) * s, 0.6f + dy * 0.5f, 0.14f), V((0.68f + dx * 1.4f) * s, 0.58f + dy, 0.16f)), 0.03f, 0.014f, trunk, Shell);
            foreach (float t in new[] { 0.35f, 0.65f })
            {
                var under = Vector3.Lerp(V(0.3f * s, 0.88f, 0.05f), V(0.6f * s, 0.7f, 0.12f), t);
                b.Spike(arm, under, under + V(0, -0.12f, 0), 0.018f, ice, mat: Shell, blend: 0.006f);
            }
        });

        // Calm frost-blue eyes deep in the boughs, and a beard of frost
        int head = b.Head(V(0, 1.0f, 0.12f));
        var c = V(0, 1.02f, 0.33f);
        var r = V(0.15f, 0.11f, 0.07f);
        b.Ell(head, c, r, deep);
        foreach (float x in new[] { -0.1f, -0.05f, 0f, 0.05f, 0.1f })
            b.Spike(head, V(x, 0.96f, 0.36f), V(x * 1.3f, 0.72f - MathF.Abs(x) * 0.5f, 0.43f), 0.04f, Rgb(214, 234, 246), 0.6f, Fur);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.06f * s, 1.025f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.6f), 0.028f, Rgb(120, 210, 250));
        });
        return b;
    }

    // ------------------------------------------------------------------ Floricolt line (the edelweiss)

    /// <summary>Floricolt: a tiny edelweiss, a felted white star of petals round a woolly yellow face, on a short grey-green woolly body with two leaf arms.</summary>
    private static PokeBuilder C4Floricolt()
    {
        var b = new PokeBuilder("Floricolt", 0.42f, BodyPlan.Biped, V(0, 0.13f, 0)) { Coat = Fur };
        var petal = Rgb(244, 244, 236);
        var heart = Rgb(242, 232, 150);
        var stem = Rgb(150, 172, 152);
        var leaf = Rgb(122, 150, 126);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.06f, 0));
            b.Ell(leg, V(0.045f * s, 0.025f, 0.01f), V(0.03f, 0.025f, 0.035f), stem);
        });
        b.Ell(Body, V(0, 0.12f, 0), V(0.075f, 0.085f, 0.07f), stem);
        FurTufts(b, Body, V(0, 0.12f, 0), V(0.075f, 0.085f, 0.07f), 10, 0.02f, 0.012f, stem, face: 0.95f, below: -0.4f, droop: 0.5f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.15f, 0));
            Frond(b, arm, V(0.055f * s, 0.15f, 0), V(0.17f * s, 0.12f, 0.03f), 0.038f, leaf, V(0, 1f, 0.2f), 0.25f, Leaf);
        });

        int head = b.Head(V(0, 0.2f, 0));
        var fc = V(0, 0.3f, 0.02f);
        C4StarFlower(b, head, fc, V(0, 0.1f, 1f), 10, 0.15f, 0.045f, petal, heart, 0.07f, Fur);
        var c = V(0, 0.3f, 0.04f);
        var r = V(0.07f, 0.07f, 0.045f);
        b.Ell(head, c, r, heart, blend: 0.012f);
        var mouth = On(c, r, 0, 0.278f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.012f, 0.008f, Rgb(200, 90, 90), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.026f * s, 0.305f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.6f), 0.016f, Rgb(50, 44, 40));
        });
        return b;
    }

    /// <summary>Coltarina: an edelweiss spirit rising out of a crag of limestone, a great felted star for a head ringed by a crown of little stars, woolly leaves for sleeves.</summary>
    private static PokeBuilder C4Coltarina()
    {
        var b = new PokeBuilder("Coltarina", 0.86f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Fur };
        var petal = Rgb(244, 244, 236);
        var heart = Rgb(242, 230, 146);
        var rock = Rgb(200, 198, 190);
        var shade = Rgb(162, 160, 154);
        var leaf = Rgb(130, 154, 132);

        // The crag of limestone that is its lower body
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.22f, 0));
            b.Ell(leg, V(0.11f * s, 0.13f, 0), V(0.14f, 0.13f, 0.15f), rock, V(0, 20f * s, 8f * s), Shell, 0.02f);
            b.Ell(leg, V(0.2f * s, 0.035f, 0.08f), V(0.05f, 0.035f, 0.05f), shade, mat: Shell, blend: 0.01f);
        });
        b.Ell(Body, V(0, 0.26f, -0.02f), V(0.18f, 0.2f, 0.16f), rock, mat: Shell);
        b.Box(Body, V(0.05f, 0.3f, 0.04f), V(0.1f, 0.12f, 0.1f), 0.03f, shade, V(20f, 30f, 10f), Shell, 0.02f);
        b.Box(Body, V(-0.07f, 0.2f, 0.06f), V(0.08f, 0.1f, 0.08f), 0.03f, rock, V(-10f, -25f, 6f), Shell, 0.02f);
        // The woolly torso, its gown of leaves falling over the crag
        b.Ell(Body, V(0, 0.6f, 0), V(0.09f, 0.14f, 0.08f), leaf);
        for (int i = -5; i <= 5; i++)
        {
            float a = i * 30f * Degree;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, V(0, 0.6f, 0) + o * 0.06f, V(0, 0.3f, 0) + o * 0.2f, 0.07f, i % 2 == 0 ? leaf : PixelCanvas.Shadow(leaf, 0.12f), o + V(0, 0.3f, 0), 0.25f, Leaf);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.68f, 0));
            Frond(b, arm, V(0.07f * s, 0.68f, 0), V(0.3f * s, 0.42f, 0.04f), 0.07f, leaf, V(0, 0.3f, 1f), 0.25f, Leaf);
            Frond(b, arm, V(0.08f * s, 0.66f, -0.02f), V(0.26f * s, 0.36f, -0.04f), 0.05f, PixelCanvas.Shadow(leaf, 0.12f), V(0, 0.3f, 1f), 0.25f, Leaf);
        });
        b.Limb(Body, V(0, 0.7f, 0), V(0, 0.82f, 0.01f), 0.032f, 0.026f, leaf);

        int head = b.Head(V(0, 0.8f, 0));
        var fc = V(0, 0.93f, 0.03f);
        C4StarFlower(b, head, fc, V(0, 0.1f, 1f), 10, 0.22f, 0.06f, petal, heart, 0.09f, Fur);
        var c = V(0, 0.93f, 0.055f);
        var r = V(0.085f, 0.085f, 0.05f);
        b.Ell(head, c, r, heart, blend: 0.012f);
        // The crown of little stars on a ring behind its head, held to it by two stalks
        var ring = V(0, 0.98f, -0.04f);
        const float R = 0.25f;
        b.Torus(head, ring, R, 0.013f, leaf, V(90f, 0, 0), mat: Leaf);
        PokeBuilder.Both(s => b.Limb(head, V(0.04f * s, 0.93f, 0.0f), ring + V(R * 0.84f * s, -R * 0.54f, 0), 0.014f, 0.012f, leaf, Leaf));
        for (int k = 0; k < 7; k++)
        {
            float a = (20f + k * 140f / 6f) * Degree;
            var at = ring + V(MathF.Cos(a) * R, MathF.Sin(a) * R, 0.012f);
            C4StarFlower(b, head, at, V(0, 0, 1f), 6, 0.045f, 0.014f, petal, heart, 0.016f, Fur);
        }
        var mouth = On(c, r, 0, 0.905f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.014f, 0.008f, Rgb(170, 110, 90), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, 0.94f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.6f), 0.02f, Rgb(54, 46, 40));
        });
        return b;
    }

    // ------------------------------------------------------------------ Babut (the weathered stone)

    /// <summary>Babut: a small round weather-worn stone with a face the wind wore into it, hollows for eyes and a crease of a mouth, spotted with lichen, on two pebble feet.</summary>
    private static PokeBuilder C4Babut()
    {
        var b = new PokeBuilder("Babut", 0.42f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Shell };
        var stone = Rgb(150, 150, 146);
        var shade = Rgb(120, 120, 118);
        var yellow = Rgb(232, 196, 40);
        var orange = Rgb(230, 134, 40);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.06f, 0.02f));
            b.Ell(leg, V(0.075f * s, 0.028f, 0.03f), V(0.035f, 0.03f, 0.035f), shade);
        });
        var bc = V(0, 0.17f, -0.01f);
        var br = V(0.17f, 0.14f, 0.15f);
        b.Ell(Body, bc, br, stone);
        b.Ell(Body, V(0.06f, 0.23f, -0.06f), V(0.1f, 0.08f, 0.1f), stone, blend: 0.05f);
        b.Ell(Body, V(-0.08f, 0.13f, -0.05f), V(0.1f, 0.08f, 0.09f), stone, blend: 0.05f);
        // The worn face: a lump on the front, the eyes in shaded hollows and a crease of a mouth
        int head = b.Head(V(0, 0.2f, 0));
        var c = V(0, 0.2f, 0.04f);
        var r = V(0.15f, 0.12f, 0.12f);
        b.Ell(head, c, r, stone, blend: 0.04f);
        PokeBuilder.Both(s => b.PaintEll(head, V(0.06f * s, 0.215f, 0.15f), V(0.036f, 0.03f, 0.03f), shade));
        var mouth = On(c, r, 0, 0.15f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.06f, 0.006f, Rgb(92, 92, 94), MarkShape.Bar);
        foreach (var (d, col, sz) in new[] { (V(-0.5f, 0.8f, 0.2f), yellow, 0.026f), (V(0.6f, 0.6f, -0.3f), yellow, 0.022f), (V(0.9f, 0.1f, 0.2f), orange, 0.016f), (V(-0.8f, -0.1f, 0.4f), yellow, 0.018f), (V(0.2f, 0.9f, -0.4f), orange, 0.014f) })
        {
            var at = Out(bc, br, default, d);
            b.Mark(Body, at, Outward(bc, br, at), sz, sz * 0.8f, col);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.06f * s, 0.215f);
            b.Eye(head, at, Outward(c, r, at) + V(0, 0, 0.4f), 0.022f, Rgb(70, 70, 76));
        });
        return b;
    }
}
