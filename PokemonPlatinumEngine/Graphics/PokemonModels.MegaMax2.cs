using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The Mega Dynamax forms of the Megas from Incineroar to Victreebel (plan 06 · R20c, the second half of the sculpts;
// the first half, Abomasnow to Houndoom, is PokemonModels.MegaMax1.cs). Each is its Mega's own build, taken whole and
// renamed, with the look the user's reference pictures (docs/art/reference/gemini/megadynamax) give a Mega Dynamax
// form: the storm of dark red and violet smoke it stands in (a bank of it on the ground with a spiral rising round the
// build), the Mega Stone's emblem glowing on it, and the things of its own that the picture makes larger, lit or
// crystal. The Mega's own sculpt is never touched. Every part is joined to the body, so each form is one piece.
internal static partial class PokemonModels
{
    private static readonly Color MmSmoke = Rgb(126, 30, 54), MmSmokeLit = Rgb(178, 52, 72), MmViolet = Rgb(104, 62, 152),
        MmCyan = Rgb(88, 220, 236), MmBolt = Rgb(190, 246, 255), MmRing = Rgb(246, 238, 255), MmCrystalPink = Rgb(238, 120, 190),
        MmCrystalViolet = Rgb(170, 110, 236), MmCrystalCyan = Rgb(110, 226, 238), MmGold = Rgb(248, 206, 84),
        MmOrange = Rgb(250, 150, 70), MmRed = Rgb(226, 70, 86), MmLeafGlow = Rgb(130, 238, 120);

    /// <summary>A Mega Dynamax form of the second half with a sculpt of its own, by name in capitals (null for any other).</summary>
    private static PokeBuilder? CreateMegaMax2(string form) => form switch
    {
        "INCINEROAR-MEGA-MMAX" => IncineroarMmax(),
        "INFERNAPE-MEGA-MMAX" => InfernapeMmax(),
        "INTELEON-MEGA-MMAX" => InteleonMmax(),
        "KANGASKHAN-MEGA-MMAX" => KangaskhanMmax(),
        "LOPUNNY-MEGA-MMAX" => LopunnyMmax(),
        "LUCARIO-MEGA-MMAX" => LucarioMmax(),
        "MANECTRIC-MEGA-MMAX" => ManectricMmax(),
        "MAWILE-MEGA-MMAX" => MawileMmax(),
        "MEDICHAM-MEGA-MMAX" => MedichamMmax(),
        "MEGANIUM-MEGA-MMAX" => MeganiumMmax(),
        "MEOWSCARADA-MEGA-MMAX" => MeowscaradaMmax(),
        "METAGROSS-MEGA-MMAX" => MetagrossMmax(),
        "PIDGEOT-MEGA-MMAX" => PidgeotMmax(),
        "PINSIR-MEGA-MMAX" => PinsirMmax(),
        "PRIMARINA-MEGA-MMAX" => PrimarinaMmax(),
        "PYROAR-MEGA-MMAX" => PyroarMmax(),
        "QUAQUAVAL-MEGA-MMAX" => QuaquavalMmax(),
        "RAICHU-MEGA-X-MMAX" => RaichuXMmax(),
        "RAICHU-MEGA-Y-MMAX" => RaichuYMmax(),
        "RILLABOOM-MEGA-MMAX" => RillaboomMmax(),
        "SABLEYE-MEGA-MMAX" => SableyeMmax(),
        "SALAMENCE-MEGA-MMAX" => SalamenceMmax(),
        "SAMUROTT-MEGA-MMAX" => SamurottMmax(),
        "SCEPTILE-MEGA-MMAX" => SceptileMmax(),
        "SCIZOR-MEGA-MMAX" => ScizorMmax(),
        "SCOLIPEDE-MEGA-MMAX" => ScolipedeMmax(),
        "SERPERIOR-MEGA-MMAX" => SerperiorMmax(),
        "SHARPEDO-MEGA-MMAX" => SharpedoMmax(),
        "SKARMORY-MEGA-MMAX" => SkarmoryMmax(),
        "SKELEDIRGE-MEGA-MMAX" => SkeledirgeMmax(),
        "SLOWBRO-MEGA-MMAX" => SlowbroMmax(),
        "STARMIE-MEGA-MMAX" => StarmieMmax(),
        "STEELIX-MEGA-MMAX" => SteelixMmax(),
        "SWAMPERT-MEGA-MMAX" => SwampertMmax(),
        "TORTERRA-MEGA-MMAX" => TorterraMmax(),
        "TYPHLOSION-MEGA-MMAX" => TyphlosionMmax(),
        "TYRANITAR-MEGA-MMAX" => TyranitarMmax(),
        "VENUSAUR-MEGA-MMAX" => VenusaurMmax(),
        "VICTREEBEL-MEGA-MMAX" => VictreebelMmax(),
        _ => null
    };

    // ------------------------------------------------------------------ the kit every one of them is built with

    /// <summary>Takes a Mega's finished build as the start of its Mega Dynamax form (renamed, the frame it filled kept).</summary>
    private static PokeBuilder MmBase(string mega)
    {
        var b = Create(mega);
        return Become(b, mega + "-Mmax", b.Model.Fill);
    }

    /// <summary>A bone by name, or the body's if the build has none of that name.</summary>
    private static int MmBone(PokeBuilder b, string name)
    {
        int i = b.Model.Skeleton.Find(name);
        return i < 0 ? Body : i;
    }

    /// <summary>The point of a bone's shapes that lies farthest along <paramref name="dir"/> (the tip of an ear, a horn, a wing).</summary>
    private static Vector3 MmTip(PokeBuilder b, int bone, Vector3 dir)
    {
        var best = Vector3.Zero;
        float far = float.MinValue;
        foreach (var s in b.Sdf.Shapes)
        {
            if (s.Bone != bone || s.Op != SdfOp.Add) continue;
            foreach (var p in new[] { s.A, s.B })
            {
                float d = Vector3.Dot(p, dir);
                if (d > far) { far = d; best = p; }
            }
        }
        return best;
    }

    /// <summary>Where a ray from <paramref name="from"/> along <paramref name="dir"/> first meets the surface (null if it never does).</summary>
    private static Vector3? MmHit(PokeBuilder b, Vector3 from, Vector3 dir, float reach = 2f)
    {
        dir = Vector3.Normalize(dir);
        for (float t = 0f; t < reach; t += 0.006f)
        {
            var p = from + dir * t;
            if (b.Sdf.Distance(p) < 0.003f) return p;
        }
        return null;
    }

    /// <summary>The surface's outward direction at <paramref name="p"/> (the gradient of the distance).</summary>
    private static Vector3 MmNormal(PokeBuilder b, Vector3 p)
    {
        const float e = 0.012f;
        var g = V(b.Sdf.Distance(p + V(e, 0, 0)) - b.Sdf.Distance(p - V(e, 0, 0)), b.Sdf.Distance(p + V(0, e, 0)) - b.Sdf.Distance(p - V(0, e, 0)),
            b.Sdf.Distance(p + V(0, 0, e)) - b.Sdf.Distance(p - V(0, 0, e)));
        return g.LengthSquared() < 1e-9f ? V(0, 0, 1) : Vector3.Normalize(g);
    }

    /// <summary>The bone whose shape the surface at <paramref name="p"/> belongs to (the nearest added shape's).</summary>
    private static int MmBoneAt(PokeBuilder b, Vector3 p)
    {
        b.Sdf.Prepare();
        int bone = Body;
        float best = float.MaxValue;
        foreach (var s in b.Sdf.Shapes)
        {
            if (s.Op != SdfOp.Add) continue;
            float d = s.Distance(p);
            if (d < best) { best = d; bone = s.Bone; }
        }
        return bone;
    }

    /// <summary>
    /// Thickens the thinnest end of a bone's shapes that lies farthest along <paramref name="dir"/> (an ear's tip) by a
    /// shape of its own on the same ends, so that a tip a coarser mesh would break off stays joined.
    /// </summary>
    private static void MmThickenTip(PokeBuilder b, int bone, Vector3 dir, float k = 1.5f)
    {
        SdfPrimitive? best = null;
        float far = float.MinValue;
        foreach (var s in b.Sdf.Shapes)
        {
            if (s.Bone != bone || s.Op != SdfOp.Add || s.Shape != SdfShape.RoundCone) continue;
            float d = MathF.Max(Vector3.Dot(s.A, dir), Vector3.Dot(s.B, dir));
            if (d > far) { far = d; best = s; }
        }
        if (best is { } t) b.Limb(bone, t.A, t.B, t.Size.X * k, t.Size.Y * k, t.Color, t.Material, t.Blend);
    }

    /// <summary>The lowest height of the build's surface, found by dropping rays over its footprint (for a build that hovers).</summary>
    private static float MmFloor(PokeBuilder b)
    {
        var (mn, mx) = b.Sdf.Bounds();
        float low = float.MaxValue;
        for (int i = 0; i <= 8; i++)
            for (int k = 0; k <= 8; k++)
            {
                float x = mn.X + (mx.X - mn.X) * i / 8f, z = mn.Z + (mx.Z - mn.Z) * k / 8f;
                for (float y = MathF.Max(0f, mn.Y); y < low && y < mx.Y; y += 0.01f)
                    if (b.Sdf.Distance(V(x, y, z)) < 0f) { low = y; break; }
            }
        return low == float.MaxValue ? 0f : low;
    }

    /// <summary>The height of the build, which sizes what is laid on it.</summary>
    private static float MmHeight(PokeBuilder b)
    {
        var (mn, mx) = b.Sdf.Bounds();
        return mx.Y - mn.Y;
    }

    /// <summary>
    /// The Mega Stone's emblem laid on the front of the build at <paramref name="x"/> and <paramref name="y"/>: a ring
    /// and the star in it, on whatever the front is there (a head, a chest, a belly). Nothing if the ray finds no front.
    /// </summary>
    private static void MmStone(PokeBuilder b, float x, float y, Color star, float scale = 1f)
    {
        var (_, mx) = b.Sdf.Bounds();
        if (MmHit(b, V(x, y, mx.Z + 0.1f), V(0, 0, -1)) is { } at)
            MmStoneAt(b, at, MmNormal(b, at), Math.Clamp(MmHeight(b) * 0.04f, 0.022f, 0.06f) * scale, star);
    }

    /// <summary>The emblem at a point of the surface facing <paramref name="n"/>.</summary>
    private static void MmStoneAt(PokeBuilder b, Vector3 at, Vector3 n, float size, Color star)
    {
        int bone = MmBoneAt(b, at);
        b.Mark(bone, at, n, size, size, MmRing, MarkShape.Ring);
        b.Mark(bone, at, n, size * 0.72f, size * 0.72f, star, MarkShape.Star);
    }

    /// <summary>A crystal: a blade of faceted light from <paramref name="from"/> to <paramref name="to"/>.</summary>
    private static void MmCrystal(PokeBuilder b, int bone, Vector3 from, Vector3 to, float r, Color color) =>
        b.Spike(bone, from, to, r, color, 0.7f, Glow, 0.006f);

    /// <summary>
    /// The storm Dynamax energy stands in: a bank of dark red smoke on the ground under the build with a violet ring
    /// on its edge, billows along it and a spiral of puffs rising round the build from it. Every part joins the bank,
    /// so the build stays one piece; the bank is on the root, which doesn't move with the body.
    /// </summary>
    private static void MmStorm(PokeBuilder b, float scale = 1f)
    {
        var (mn, mx) = b.Sdf.Bounds();
        float h = mx.Y - mn.Y;
        var c = V((mn.X + mx.X) / 2f, 0f, (mn.Z + mx.Z) / 2f);
        float rx = MathF.Max(0.26f, (mx.X - mn.X) / 2f * 0.82f) * scale, rz = MathF.Max(0.26f, (mx.Z - mn.Z) / 2f * 0.82f) * scale;
        float t = Math.Clamp(h * 0.05f, 0.03f, 0.075f);
        // A thin bank of smoke whose top just meets the feet (so it joins the build without swallowing its legs), ringed with a violet one
        float floor = b.Model.Hovers ? MmFloor(b) : 0f;
        bool hover = floor > 0.04f;
        float y = hover ? floor + 0.2f * t : 0.35f * t;
        b.Ell(Root, V(c.X, y, c.Z), V(rx * 0.92f, t * 0.35f, rz * 0.92f), MmSmoke, mat: Glow, blend: 0.012f);
        b.Torus(Root, V(c.X, y + t * 0.2f, c.Z), rz * 0.98f, t * 0.45f, MmViolet, sx: rx / rz, mat: Glow, blend: 0.012f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f + 0.3f;
            float s = t * (1.1f + 0.25f * MathF.Sin(i * 2.3f));
            b.Ell(Root, V(c.X + MathF.Cos(a) * rx, y + t * 0.65f, c.Z + MathF.Sin(a) * rz), V(s, s * 0.85f, s), i % 3 == 1 ? MmSmokeLit : MmSmoke, mat: Glow, blend: 0.012f);
        }
        // A build that hovers is joined to its bank by a column of smoke up into its middle
        if (hover)
        {
            var inside = V(c.X, floor + 0.4f * (mx.Y - floor), c.Z);
            b.Limb(Root, V(c.X, y, c.Z), inside, t * 0.9f, t * 0.7f, MmSmoke, Glow, 0.01f);
        }
        // A spiral of puffs rising round the build from the bank, kept outside it and its soft edges short so it takes no weight from a limb
        const int Steps = 10;
        var prev = V(c.X + MathF.Cos(0.3f) * rx, y + t * 0.8f, c.Z + MathF.Sin(0.3f) * rz);
        for (int i = 1; i <= Steps; i++)
        {
            float u = i / (float)Steps, a = 0.3f + u * MathF.Tau * 0.9f;
            float k = 1.12f - 0.12f * u;
            float s = t * (1.2f - 0.5f * u);
            var at = V(c.X + MathF.Cos(a) * rx * k, y + t * 0.8f + u * 0.62f * h, c.Z + MathF.Sin(a) * rz * k);
            b.Limb(Root, prev, at, s * 0.55f, s * 0.5f, MmSmoke, Glow, 0.006f);
            b.Ell(Root, at, V(s, s * 0.85f, s), i % 3 == 0 ? MmViolet : (i % 3 == 1 ? MmSmokeLit : MmSmoke), mat: Glow, blend: 0.008f);
            prev = at;
        }
        b.Ell(Root, prev + V(0, t * 0.5f, 0), V(t * 0.5f, t * 0.42f, t * 0.5f), MmCyan, mat: Glow, blend: 0.008f);
    }

    /// <summary>A zigzag of lightning (a thin lit tube) through <paramref name="points"/>.</summary>
    private static void MmZig(PokeBuilder b, int bone, float r, Color color, params Vector3[] points) =>
        b.Tube(bone, points, r, r * 0.8f, color, Glow, 0.004f);

    /// <summary>
    /// A crack of lightning laid over the front of the build, from <paramref name="x"/>, <paramref name="y"/> down and
    /// across by <paramref name="dx"/>, <paramref name="dy"/> in <paramref name="n"/> jagged steps: each point lies on
    /// the surface the ray from the front finds.
    /// </summary>
    private static void MmCrack(PokeBuilder b, float x, float y, float dx, float dy, int n, Color color, float r = 0.007f)
    {
        var (_, mx) = b.Sdf.Bounds();
        var pts = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i <= n; i++)
        {
            float jig = (i % 2 == 0 ? -1f : 1f) * MathF.Abs(dx) * 0.5f;
            if (MmHit(b, V(x + dx * i + (i == 0 ? 0 : jig), y + dy * i, mx.Z + 0.1f), V(0, 0, -1)) is { } p) pts.Add(p + MmNormal(b, p) * 0.004f);
        }
        if (pts.Count >= 2) b.Tube(MmBoneAt(b, pts[0]), pts.ToArray(), r, r * 0.8f, color, Glow, 0.003f);
    }

    /// <summary>
    /// A ring of plates round the build on strands from its middle: one flat gem of each colour given on an ellipse
    /// <paramref name="rx"/> by <paramref name="rz"/> round <paramref name="c"/>.
    /// </summary>
    private static void MmOrbit(PokeBuilder b, Vector3 c, float rx, float rz, float size, params Color[] colors)
    {
        int n = colors.Length;
        for (int i = 0; i < n; i++)
        {
            float a = i * MathF.Tau / n + 0.4f;
            var at = c + V(MathF.Cos(a) * rx, 0.03f * MathF.Sin(i * 2.1f), MathF.Sin(a) * rz);
            b.Limb(Body, c, at, 0.016f, 0.012f, MmBolt, Glow, 0.004f);
            b.Ell(Body, at, V(size, size * 1.1f, size * 0.45f), colors[i], V(0, -a / Degree + 90f, 0), Glow, 0.008f);
        }
    }

    // ------------------------------------------------------------------ Mega Dynamax Incineroar

    /// <summary>
    /// Mega Dynamax Incineroar: the champion's belt ringed with a spinning band of fire and its shards, the plate's
    /// paw print lit, and a great flame on the end of its tail.
    /// </summary>
    private static PokeBuilder IncineroarMmax()
    {
        var b = MmBase("Incineroar-Mega");
        var fire = Rgb(246, 112, 42);
        var heart = Rgb(255, 222, 110);
        b.Torus(Body, V(0, 0.36f, -0.01f), 0.19f, 0.018f, fire, sx: 1f, sz: 0.84f, mat: Glow, blend: 0.012f);
        b.Torus(Body, V(0, 0.37f, -0.01f), 0.2f, 0.01f, heart, sx: 1f, sz: 0.84f, mat: Glow, blend: 0.008f);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var at = V(MathF.Sin(a) * 0.19f, 0.36f, -0.01f + MathF.Cos(a) * 0.16f);
            var tangent = V(MathF.Cos(a), 0, -MathF.Sin(a)) * 0.07f;
            b.Spike(Body, at, at + tangent + V(0, 0.045f + 0.02f * (i % 3), 0), 0.02f, i % 2 == 0 ? fire : heart, 0.7f, Glow, 0.008f);
        }
        MmStone(b, 0f, 0.35f, heart);
        int tail = MmBone(b, "tail");
        var tip = MmTip(b, tail, V(0, 0.1f, -1f));
        FlameTongue(b, tail, tip, tip + V(0, 0.28f, -0.1f), 0.06f, fire, heart);
        FlameTongue(b, tail, tip + V(0, 0, 0.02f), tip + V(0.07f, 0.2f, -0.06f), 0.04f, fire, heart);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Infernape

    /// <summary>Mega Dynamax Infernape: its mane of fire grown out in long tongues streaming back, the emblem on its chest, and rings of flame at both wrists.</summary>
    private static PokeBuilder InfernapeMmax()
    {
        var b = MmBase("Infernape-Mega");
        var fire = Rgb(246, 112, 42);
        var heart = Rgb(255, 224, 112);
        int head = MmBone(b, "head");
        foreach (var (x, y, z, ex, ey, ez) in new[]
        {
            (-0.09f, 1.12f, -0.1f, -0.24f, 1.34f, -0.44f), (0.09f, 1.12f, -0.1f, 0.24f, 1.34f, -0.44f),
            (0f, 1.18f, -0.1f, 0f, 1.46f, -0.5f), (-0.05f, 1.02f, -0.14f, -0.16f, 1.1f, -0.56f), (0.05f, 1.02f, -0.14f, 0.16f, 1.1f, -0.56f)
        })
            FlameTongue(b, head, V(x, y, z), V(ex, ey, ez), 0.055f, fire, heart);
        foreach (float s in new[] { -1f, 1f })
        {
            int arm = MmBone(b, s < 0 ? "armL" : "armR");
            var hand = MmTip(b, arm, V(0.2f * s, -1f, 0.2f));
            b.Torus(arm, hand + V(0, 0.07f, 0), 0.07f, 0.014f, fire, V(0, 0, 70f * s), mat: Glow, blend: 0.008f);
            FlameTongue(b, arm, hand + V(0, 0.07f, 0), hand + V(0.06f * s, 0.22f, 0.02f), 0.035f, fire, heart);
        }
        MmStone(b, 0f, 0.86f, heart);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Inteleon

    /// <summary>Mega Dynamax Inteleon: a ring of light aimed out from its scope with a cross in it, flames of water about its coat, and the emblem on its chest.</summary>
    private static PokeBuilder InteleonMmax()
    {
        var b = MmBase("Inteleon-Mega");
        int head = MmBone(b, "head");
        var eye = V(0.035f, 0.76f, 0.07f);
        if (MmHit(b, V(0.035f, 0.76f, 0.5f), V(0, 0, -1)) is { } hit && hit.Z > 0f) eye = hit;
        var ring = eye + V(0, 0, 0.1f);
        b.Limb(head, eye, ring, 0.012f, 0.012f, MmCyan, Glow, 0.004f);
        b.Torus(head, ring, 0.05f, 0.012f, MmCyan, V(90f, 0, 0), mat: Glow, blend: 0.004f);
        b.Limb(head, ring + V(-0.07f, 0, 0), ring + V(0.07f, 0, 0), 0.01f, 0.01f, MmBolt, Glow, 0.002f);
        b.Limb(head, ring + V(0, -0.07f, 0), ring + V(0, 0.07f, 0), 0.01f, 0.01f, MmBolt, Glow, 0.002f);
        foreach (var (x, z) in new[] { (-0.04f, -0.01f), (0.04f, -0.01f) })
            FlameTongue(b, Body, V(x, 0.48f, z), V(x * 1.7f, 0.68f, z - 0.03f), 0.022f, MmViolet, MmCyan);
        MmStone(b, 0f, 0.7f, MmCyan);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Kangaskhan

    /// <summary>Mega Dynamax Kangaskhan: the baby in its pouch grown crystals of pink light, on its fists and its belly, and the Stone's emblem on its brow.</summary>
    private static PokeBuilder KangaskhanMmax()
    {
        var b = MmBase("Kangaskhan-Mega");
        int baby = MmBone(b, "baby");
        var j = b.Model.Skeleton[baby].Joint;
        foreach (float s in new[] { -1f, 1f })
        {
            var fist = j + V(0.09f * s, 0.2f, 0.07f);
            b.Ell(baby, fist, V(0.05f, 0.05f, 0.05f), MmCrystalPink, mat: Glow, blend: 0.012f);
            MmCrystal(b, baby, fist, fist + V(0.07f * s, 0.07f, 0.05f), 0.03f, MmCrystalPink);
            MmCrystal(b, baby, fist, fist + V(0.03f * s, 0.1f, 0.0f), 0.025f, Rgb(250, 160, 130));
            MmCrystal(b, baby, j + V(0.04f * s, 0.18f, 0.06f), j + V(0.1f * s, 0.14f, 0.18f), 0.02f, MmCrystalPink);
        }
        foreach (var (x, y) in new[] { (0f, 0.14f), (-0.03f, 0.22f), (0.03f, 0.22f) })
            b.Ell(baby, j + V(x, y, 0.09f), V(0.022f, 0.03f, 0.014f), MmCrystalPink, mat: Glow, blend: 0.006f);
        MmStone(b, 0f, 0.9f, MmCrystalCyan, 0.8f);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Lopunny

    /// <summary>Mega Dynamax Lopunny: rings of pink light-fluff at its wrists, ankles and ears, its ears streaming ribbons, and a gem on its chest.</summary>
    private static PokeBuilder LopunnyMmax()
    {
        var b = MmBase("Lopunny-Mega");
        var pink = Rgb(252, 150, 196);
        var cream = Rgb(255, 232, 176);
        foreach (float s in new[] { -1f, 1f })
        {
            int ear = MmBone(b, s < 0 ? "earL" : "earR");
            var tip = MmTip(b, ear, V(0.3f * s, 0.2f, -1f));
            var pts = Smooth(3, tip, tip + V(0.1f * s, 0.02f, -0.18f), tip + V(0.2f * s, -0.04f, -0.34f), tip + V(0.14f * s, 0.06f, -0.5f));
            b.Tube(ear, pts, 0.03f, 0.014f, pink, Glow, 0.006f);
            b.Tube(ear, pts.Select(p => p + V(0, 0.03f, 0)).ToArray(), 0.02f, 0.01f, cream, Glow, 0.006f);
            var top = MmTip(b, ear, V(0, 1, 0));
            b.Torus(ear, top + V(0, -0.02f, 0), 0.035f, 0.022f, pink, V(90f, 0, 0), mat: Glow, blend: 0.01f);
            int leg = MmBone(b, s < 0 ? "legL" : "legR");
            b.Torus(leg, V(0.07f * s, 0.1f, 0.0f), 0.048f, 0.022f, pink, mat: Glow, blend: 0.01f);
            int arm = MmBone(b, s < 0 ? "armL" : "armR");
            var hand = MmTip(b, arm, V(0.2f * s, -1f, 0.1f));
            b.Torus(arm, hand + V(0, 0.03f, 0), 0.032f, 0.018f, pink, mat: Glow, blend: 0.008f);
        }
        MmStone(b, 0f, 0.68f, Rgb(120, 230, 160), 0.8f);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Lucario

    /// <summary>Mega Dynamax Lucario: an aura sphere held before its chest, ribbons of crystal streaming from its tendrils, and the emblem on its chest.</summary>
    private static PokeBuilder LucarioMmax()
    {
        var b = MmBase("Lucario-Mega");
        var blue = Rgb(70, 130, 232);
        var red = Rgb(214, 60, 84);
        foreach (float s in new[] { -1f, 1f })
        {
            int ear = MmBone(b, s < 0 ? "earL" : "earR");
            var tip = MmTip(b, ear, V(s, 0, 0));
            var pts = Smooth(3, tip, tip + V(0.1f * s, 0.12f, -0.1f), tip + V(0.2f * s, 0.26f, -0.04f), tip + V(0.34f * s, 0.2f, -0.2f), tip + V(0.4f * s, 0.34f, -0.1f));
            b.Tube(ear, pts, 0.03f, 0.018f, blue, Glow, 0.008f);
            b.Tube(ear, pts.Skip(3).ToArray(), 0.02f, 0.012f, red, Glow, 0.008f);
        }
        var core = V(0, 0.62f, 0.2f);
        b.Ell(Body, core, V(0.1f, 0.1f, 0.1f), Rgb(90, 200, 250), mat: Glow, blend: 0.02f);
        b.Ell(Body, core, V(0.05f, 0.05f, 0.05f), MmBolt, mat: Glow, blend: 0.02f);
        b.Torus(Body, core, 0.12f, 0.008f, MmCyan, V(60f, 0, 20f), mat: Glow, blend: 0.006f);
        MmStone(b, 0f, 0.84f, MmCyan, 0.8f);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Manectric

    /// <summary>Mega Dynamax Manectric: its mane stood up in great blades of yellow edged with cyan, lightning running through them, and the emblem by its eye.</summary>
    private static PokeBuilder ManectricMmax()
    {
        var b = MmBase("Manectric-Mega");
        var yellow = Rgb(250, 232, 110);
        foreach (var (x, y, z, ex, ey, ez, r) in new[]
        {
            (-0.07f, 0.4f, 0.06f, -0.2f, 1.02f, -0.1f, 0.06f), (0.07f, 0.4f, 0.06f, 0.2f, 1.02f, -0.1f, 0.06f),
            (-0.09f, 0.4f, -0.04f, -0.26f, 0.96f, -0.34f, 0.055f), (0.09f, 0.4f, -0.04f, 0.26f, 0.96f, -0.34f, 0.055f),
            (0f, 0.42f, -0.02f, 0f, 1.1f, -0.22f, 0.06f), (0f, 0.38f, -0.14f, 0f, 0.9f, -0.5f, 0.05f)
        })
        {
            b.Spike(Body, V(x, y, z), V(ex, ey, ez), r, yellow, 0.5f, Fur, 0.01f);
            var d = V(ex, ey, ez) - V(x, y, z);
            b.PaintEll(Body, V(ex, ey, ez) - d * 0.12f, V(0.035f, 0.1f, 0.035f), MmCrystalCyan, Euler(d));
            MmZig(b, Body, 0.012f, MmBolt, V(x, y, z) + d * 0.2f, V(x, y, z) + d * 0.35f + V(0.03f, 0, 0), V(x, y, z) + d * 0.5f + V(-0.02f, 0, 0.03f), V(x, y, z) + d * 0.7f + V(0.03f, 0, 0));
        }
        if (MmHit(b, V(-0.4f, 0.45f, 0.18f), V(1, 0, 0)) is { } side) MmStoneAt(b, side, MmNormal(b, side), 0.04f, MmCyan);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Mawile

    /// <summary>Mega Dynamax Mawile: crystal teeth of violet light round each great jaw, and a chain of light from jaw to jaw across its chest with the emblem hung on it.</summary>
    private static PokeBuilder MawileMmax()
    {
        var b = MmBase("Mawile-Mega");
        var pink = Rgb(236, 120, 244);
        var c = V(0, 0.36f, 0.02f);
        var mouths = new Vector3[2];
        foreach (var (k, side) in new[] { (0, -1f), (1, 1f) })
        {
            int jaw = MmBone(b, k == 0 ? "maw" : "maw2");
            var path = Smooth(3, c + V(0.02f * side, 0.04f, -0.04f), c + V(0.05f * side, 0.14f, -0.1f), c + V(0.08f * side, 0.1f, -0.22f), c + V(0.1f * side, -0.08f, -0.28f));
            var mouth = path[^1] + V(0.01f * side, -0.08f, -0.02f);
            mouths[k] = mouth;
            for (int i = 0; i < 9; i++)
            {
                float a = i * MathF.Tau / 9f;
                var rim = mouth + V(MathF.Cos(a) * 0.075f, 0.01f, MathF.Sin(a) * 0.06f);
                var outward = V(MathF.Cos(a), 0.15f, MathF.Sin(a));
                MmCrystal(b, jaw, rim - outward * 0.02f, rim + outward * 0.07f, 0.014f, i % 2 == 0 ? pink : MmCrystalViolet);
            }
            for (int i = 1; i < path.Length - 2; i += 2)
                MmCrystal(b, jaw, path[i], path[i] + V(0.04f * side, 0.06f, -0.01f), 0.012f, pink);
        }
        var chain = Smooth(3, mouths[0], c + V(-0.12f, 0.02f, -0.02f), V(-0.1f, 0.24f, 0.07f), V(0, 0.19f, 0.085f), V(0.1f, 0.24f, 0.07f), c + V(0.12f, 0.02f, -0.02f), mouths[1]);
        b.Tube(MmBone(b, "head"), chain, 0.009f, 0.009f, pink, Glow, 0.004f);
        for (int i = 4; i < chain.Length - 4; i += 3)
            b.Torus(Body, chain[i], 0.016f, 0.005f, MmRing, V(i * 37f % 90f, i * 53f % 180f, 0), mat: Glow, blend: 0.002f);
        var hang = V(0, 0.19f, 0.09f);
        b.Torus(Body, hang, 0.03f, 0.007f, MmRing, V(90f, 0, 0), mat: Glow, blend: 0.003f);
        b.Ell(Body, hang + V(0, 0, 0.004f), V(0.026f, 0.026f, 0.01f), pink, mat: Glow, blend: 0.005f);
        MmStoneAt(b, hang + V(0, 0, 0.014f), V(0, 0, 1), 0.025f, MmRing);
        MmStorm(b, 1.3f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Medicham

    /// <summary>Mega Dynamax Medicham: four wings of pale prismatic light spread behind it, and the emblem on its brow.</summary>
    private static PokeBuilder MedichamMmax()
    {
        var b = MmBase("Medicham-Mega");
        var glass = Rgb(244, 226, 252);
        var gold = Rgb(252, 226, 150);
        foreach (float s in new[] { -1f, 1f })
        {
            var root = V(0.04f * s, 0.7f, -0.04f);
            Frond(b, Body, root, V(0.62f * s, 1.14f, -0.12f), 0.1f, glass, V(0, 0, 1), 0.22f, Glow, 0.01f);
            Frond(b, Body, root, V(0.6f * s, 0.8f, -0.1f), 0.08f, gold, V(0, 0, 1), 0.22f, Glow, 0.01f);
            Frond(b, Body, root + V(0, -0.06f, 0), V(0.54f * s, 0.52f, -0.08f), 0.07f, glass, V(0, 0, 1), 0.22f, Glow, 0.01f);
        }
        MmStone(b, 0f, 0.8f, gold, 0.9f);
        MmStorm(b, 1.1f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Meganium

    /// <summary>Mega Dynamax Meganium: a great fan of golden petals standing behind its collar like a sun, the antennae's rings lit, and the emblem on its belly.</summary>
    private static PokeBuilder MeganiumMmax()
    {
        var b = MmBase("Meganium-Mega");
        var sun = Rgb(250, 206, 70);
        var light = Rgb(255, 232, 130);
        var centre = V(0, 0.6f, -0.12f);
        for (int i = 0; i < 17; i++)
        {
            float a = Degree * (-95f + i * 11.8f);
            var dir = V(MathF.Sin(a), MathF.Cos(a), 0f);
            Frond(b, Body, centre + dir * 0.18f, centre + dir * (0.52f + 0.04f * (i % 2)) + V(0, 0, -0.1f), 0.05f, i % 2 == 0 ? sun : light, V(0, 0, 1), 0.22f, Glow, 0.01f);
        }
        b.Ell(Body, centre + V(0, 0.02f, -0.02f), V(0.2f, 0.2f, 0.06f), sun, mat: Glow, blend: 0.02f);
        int head = MmBone(b, "head");
        foreach (float s in new[] { -1f, 1f })
        {
            var top = MmTip(b, head, V(0.3f * s, 1f, 0f));
            b.Torus(head, top + V(0, 0.02f, 0), 0.03f, 0.007f, MmGold, V(90f, 0, 0), mat: Glow, blend: 0.004f);
        }
        MmStone(b, 0f, 0.3f, MmCrystalCyan);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Meowscarada

    /// <summary>Mega Dynamax Meowscarada: the flower bomb on its fingertip grown into a great rose of pink crystal with sparks of light, and the emblem in the bloom at its throat.</summary>
    private static PokeBuilder MeowscaradaMmax()
    {
        var b = MmBase("Meowscarada-Mega");
        var pink = Rgb(244, 100, 170);
        var light = Rgb(255, 170, 210);
        // On whichever hand is raised highest
        int arm = MmTip(b, MmBone(b, "armR"), V(0, 1, 0)).Y >= MmTip(b, MmBone(b, "armL"), V(0, 1, 0)).Y ? MmBone(b, "armR") : MmBone(b, "armL");
        var tip = MmTip(b, arm, V(0, 1, 0)) + V(0, 0.02f, 0);
        b.Ell(arm, tip, V(0.045f, 0.04f, 0.045f), pink, mat: Glow, blend: 0.012f);
        for (int ring = 0; ring < 2; ring++)
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f + ring * 0.6f;
                var dir = V(MathF.Cos(a), 0.2f + 0.5f * (1 - ring), MathF.Sin(a));
                Petal(b, arm, tip + dir * 0.01f, tip + dir * (0.09f - 0.025f * ring) + V(0, 0.03f, 0), 0.04f - 0.01f * ring, ring == 0 ? pink : light, Glow, 0.01f);
            }
        foreach (var d in new[] { V(0.9f, 0.7f, 0), V(-0.8f, 0.9f, 0.3f), V(0.2f, 1.2f, -0.6f), V(-0.3f, 0.4f, 0.9f) })
            MmCrystal(b, arm, tip + Vector3.Normalize(d) * 0.06f, tip + Vector3.Normalize(d) * 0.12f, 0.007f, MmRing);
        MmStone(b, 0f, 0.64f, MmCrystalPink, 0.8f);
        MmStorm(b, 1.2f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Metagross

    /// <summary>Mega Dynamax Metagross: a ring of crystal plates in cyan, violet and grey turning round it on strands, crystal claws on its four fists, and the emblem at the cross of its brow.</summary>
    private static PokeBuilder MetagrossMmax()
    {
        var b = MmBase("Metagross-Mega");
        MmOrbit(b, V(0, 0.2f, 0), 0.4f, 0.3f, 0.045f, MmCrystalCyan, MmCrystalViolet, Rgb(196, 206, 220), MmCrystalCyan, MmCrystalViolet, Rgb(196, 206, 220),
            MmCrystalCyan, MmCrystalViolet);
        foreach (var (x, z) in new[] { (0.43f, 0.2f), (-0.43f, 0.2f), (0.4f, -0.2f), (-0.4f, -0.2f) })
            MmCrystal(b, Body, V(x * 0.9f, 0.2f, z), V(x * 1.08f, 0.31f, z * 1.2f), 0.03f, MmCrystalCyan);
        MmStone(b, 0f, 0.26f, MmGold, 0.8f);
        MmStorm(b, 1.15f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Pidgeot

    /// <summary>Mega Dynamax Pidgeot: its crest streamed into long ribbons of orange and pink crystal looping behind it, its tail fanned into spears of rainbow crystal, and the emblem on its chest.</summary>
    private static PokeBuilder PidgeotMmax()
    {
        var b = MmBase("Pidgeot-Mega");
        int head = MmBone(b, "head");
        var top = V(0, 0.7f, 0.1f);
        var r1 = Smooth(3, top, top + V(0.08f, 0.14f, -0.08f), top + V(0.3f, 0.2f, -0.22f), top + V(0.5f, 0.1f, -0.4f), top + V(0.64f, 0.18f, -0.62f));
        var r2 = Smooth(3, top, top + V(-0.06f, 0.16f, -0.06f), top + V(-0.26f, 0.22f, -0.2f), top + V(-0.46f, 0.12f, -0.36f), top + V(-0.6f, 0.2f, -0.56f));
        Ribbon(b, head, r1, 0.045f, MmCrystalPink, V(0, 1f, 0.2f), 0.014f);
        Ribbon(b, head, r2, 0.04f, MmOrange, V(0, 1f, 0.2f), 0.014f);
        int tail = MmBone(b, "tail");
        var root = V(0, 0.34f, -0.2f);
        var cols = new[] { MmCrystalCyan, MmCrystalPink, MmOrange, MmCrystalPink, MmCrystalCyan };
        for (int i = -4; i <= 4; i++)
        {
            float a = i * 9f * Degree;
            var dir = V(MathF.Sin(a) * 0.9f, -0.5f + 0.04f * Math.Abs(i), -MathF.Cos(a));
            MmCrystal(b, tail, root + V(0.01f * i, 0, 0), root + Vector3.Normalize(dir) * 0.62f, 0.028f, cols[Math.Abs(i) % cols.Length]);
        }
        foreach (float s in new[] { -1f, 1f })
        {
            int wing = MmBone(b, s < 0 ? "wingL" : "wingR");
            var wt = MmTip(b, wing, V(s, 0.3f, -0.3f));
            MmCrystal(b, wing, wt + V(-0.08f * s, 0, 0.05f), wt + V(0.1f * s, 0.08f, -0.04f), 0.03f, MmCrystalCyan);
        }
        MmStone(b, 0f, 0.4f, MmCrystalPink);
        MmStorm(b, 0.7f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Pinsir

    /// <summary>Mega Dynamax Pinsir: purple crystals grown along its great horns, two spears of orange standing from its back, and its chest's emblem a flame.</summary>
    private static PokeBuilder PinsirMmax()
    {
        var b = MmBase("Pinsir-Mega");
        int head = MmBone(b, "head");
        foreach (float s in new[] { -1f, 1f })
        {
            var tip = MmTip(b, head, V(0.5f * s, 1f, 0));
            var low = V(0.1f * s, 0.78f, 0.04f);
            for (int i = 0; i < 6; i++)
            {
                float t = 0.25f + i * 0.13f;
                var p = Vector3.Lerp(low, tip, t);
                MmCrystal(b, head, p, p + V(-0.05f * s, 0.03f, 0.04f) + V(0, 0.05f * (1f - t), 0), 0.018f * (1.3f - t), i % 2 == 0 ? MmCrystalViolet : MmCrystalPink);
            }
        }
        foreach (float s in new[] { -1f, 1f })
        {
            int wing = MmBone(b, s < 0 ? "wingL" : "wingR");
            MmCrystal(b, wing, V(0.12f * s, 0.76f, -0.12f), V(0.2f * s, 1.15f, -0.2f), 0.04f, MmOrange);
            MmCrystal(b, wing, V(0.2f * s, 0.74f, -0.14f), V(0.34f * s, 1.05f, -0.2f), 0.035f, MmOrange);
        }
        MmStone(b, 0f, 0.62f, MmOrange, 0.9f);
        MmStorm(b, 1.1f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Primarina

    /// <summary>Mega Dynamax Primarina: a tiara of coral and the emblem on her brow, strands of pearls hung at her throat, gems of violet light in her ring of water, and her train of hair streaming.</summary>
    private static PokeBuilder PrimarinaMmax()
    {
        var b = MmBase("Primarina-Mega");
        var coral = Rgb(246, 124, 150);
        int head = MmBone(b, "head");
        var c = V(0, 0.34f, 0.04f);
        foreach (var (x, h) in new[] { (-0.04f, 0.07f), (-0.015f, 0.09f), (0.015f, 0.1f), (0.04f, 0.07f) })
            MmCrystal(b, head, c + V(x, 0f, -0.02f), c + V(x * 1.6f, h, -0.04f), 0.011f, coral);
        if (MmHit(b, V(0, 0.4f, 0.5f), V(0, -0.2f, -1f)) is { } brow) MmStoneAt(b, brow, MmNormal(b, brow), 0.022f, MmCrystalViolet);
        // A string of pearls across her chest, each laid on the surface the ray from the front finds
        var neck = new System.Collections.Generic.List<Vector3>();
        foreach (var (x, y) in new[] { (-0.08f, 0.27f), (-0.06f, 0.22f), (-0.03f, 0.19f), (0f, 0.18f), (0.03f, 0.19f), (0.06f, 0.22f), (0.08f, 0.27f) })
            if (MmHit(b, V(x, y, 0.6f), V(0, 0, -1)) is { } hit) neck.Add(hit + MmNormal(b, hit) * 0.006f);
        if (neck.Count >= 2) b.Tube(MmBoneAt(b, neck[0]), neck.ToArray(), 0.007f, 0.007f, MmRing, Glow, 0.003f);
        foreach (var p in neck) b.Ell(MmBoneAt(b, p), p, V(0.014f, 0.014f, 0.014f), MmRing, mat: Glow, blend: 0.004f);
        MmOrbit(b, V(0, 0.22f, 0), 0.3f, 0.26f, 0.03f, MmCrystalViolet, MmCrystalCyan, MmCrystalViolet, MmCrystalCyan, MmCrystalViolet, MmCrystalCyan);
        MmStorm(b, 1.1f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Pyroar

    /// <summary>Mega Dynamax Pyroar: its mane of fire blazing wider with blades of pale gold light within, the emblem lit on its brow.</summary>
    private static PokeBuilder PyroarMmax()
    {
        var b = MmBase("Pyroar-Mega");
        var fire = Rgb(238, 84, 40);
        var heart = Rgb(255, 224, 120);
        int head = MmBone(b, "head");
        var c = V(0, 0.42f, 0.12f);
        for (int i = 0; i < 9; i++)
        {
            float a = Degree * (-85f + i * 21f);
            var dir = V(MathF.Sin(a), MathF.Cos(a), 0f);
            FlameTongue(b, head, c + dir * 0.1f, c + dir * (0.4f + 0.06f * (i % 2)) + V(0, 0, -0.14f), 0.05f, fire, heart);
        }
        if (MmHit(b, V(0, 0.52f, 0.6f), V(0, -0.2f, -1f)) is { } brow) MmStoneAt(b, brow, MmNormal(b, brow), 0.03f, MmGold);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Quaquaval

    /// <summary>Mega Dynamax Quaquaval: two long strings of water beads trailing from its wrists in loops, a halo of the emblem over its crest, and the skirt of feathers lit.</summary>
    private static PokeBuilder QuaquavalMmax()
    {
        var b = MmBase("Quaquaval-Mega");
        var bead = Rgb(150, 236, 244);
        foreach (float s in new[] { -1f, 1f })
        {
            int wing = MmBone(b, s < 0 ? "wingL" : "wingR");
            var hand = MmTip(b, wing, V(s, 0f, 0f));
            var path = Smooth(4, hand, hand + V(0.14f * s, 0.14f, 0.1f), hand + V(0.28f * s, 0.2f, 0.18f), hand + V(0.38f * s, 0.06f, 0.14f), hand + V(0.34f * s, -0.08f, 0.04f));
            for (int i = 0; i < path.Length; i++) path[i].Y = MathF.Max(path[i].Y, 0.1f);
            b.Tube(wing, path, 0.012f, 0.012f, MmCyan, Glow, 0.003f);
            for (int i = 0; i < path.Length; i += 2)
                b.Ell(wing, path[i], V(0.026f, 0.026f, 0.026f), bead, mat: Glow, blend: 0.006f);
        }
        int head = MmBone(b, "head");
        var halo = V(0, 0.98f, 0f);
        b.Limb(head, V(0, 0.84f, 0f), halo, 0.012f, 0.01f, MmCyan, Glow, 0.004f);
        b.Mark(head, halo + V(0, 0, 0.02f), V(0, 0.2f, 1f), 0.04f, 0.04f, MmRing, MarkShape.Ring);
        b.Mark(head, halo + V(0, 0, 0.02f), V(0, 0.2f, 1f), 0.028f, 0.028f, MmGold, MarkShape.Star);
        b.Torus(head, halo, 0.04f, 0.007f, MmRing, V(90f, 0, 0), mat: Glow, blend: 0.003f);
        MmStorm(b, 1.3f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Raichu X

    /// <summary>Mega Dynamax Raichu X: crystals of yellow and cyan light grown over its ears and its bolt of a tail, sparks of lightning about it, and the emblem on its belly.</summary>
    private static PokeBuilder RaichuXMmax()
    {
        var b = MmBase("Raichu-Mega-X");
        foreach (float s in new[] { -1f, 1f })
        {
            int ear = MmBone(b, s < 0 ? "earL" : "earR");
            var tip = MmTip(b, ear, V(0, 1, 0));
            MmCrystal(b, ear, tip + V(0, -0.12f, 0), tip + V(0.03f * s, 0.12f, 0), 0.03f, MmCrystalCyan);
            MmCrystal(b, ear, tip + V(0, -0.2f, 0), tip + V(0.09f * s, 0.0f, 0), 0.024f, Rgb(250, 236, 110));
            MmZig(b, ear, 0.01f, MmBolt, tip + V(0, -0.2f, 0), tip + V(0.05f * s, -0.1f, 0), tip + V(0.01f * s, -0.02f, 0.02f), tip + V(0.08f * s, 0.1f, 0));
        }
        foreach (var d in new[] { V(-1f, -0.6f, 0), V(1f, -0.6f, 0) })
            MmCrystal(b, Body, V(0, 0.4f, 0), V(0, 0.4f, 0) + d * 0.2f, 0.03f, MmCrystalCyan);
        MmStone(b, 0f, 0.78f, MmCrystalPink);
        MmStorm(b, 1.2f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Raichu Y

    /// <summary>Mega Dynamax Raichu Y: a ring of glass bubbles with a spark in each held round it on threads of lightning, and the emblem on its belly.</summary>
    private static PokeBuilder RaichuYMmax()
    {
        var b = MmBase("Raichu-Mega-Y");
        // The thin ear tips are thickened a little: the wider form meshes coarser, and they would break off
        foreach (var ear in new[] { "earL", "earR" }) MmThickenTip(b, MmBone(b, ear), V(0, 1, 0), 1.6f);
        MmThickenTip(b, MmBone(b, "tail"), V(0.3f, 0.5f, -1f), 1.6f);
        MmThickenTip(b, MmBone(b, "tail"), V(0.3f, 1f, 0f), 1.6f);
        var glass = Rgb(214, 232, 248);
        MmOrbit(b, V(0, 0.42f, 0), 0.34f, 0.3f, 0.045f, glass, MmGold, glass, glass, MmGold, glass);
        MmStone(b, 0f, 0.4f, MmGold);
        MmStorm(b, 0.85f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Rillaboom

    /// <summary>Mega Dynamax Rillaboom: the drum's faces lit with rings of rainbow light, moss and vines grown thick over its arms, and the emblem on its chest.</summary>
    private static PokeBuilder RillaboomMmax()
    {
        var b = MmBase("Rillaboom-Mega");
        var moss = Rgb(70, 128, 58);
        var drum = V(0, 0.84f, -0.26f);
        var rings = new[] { MmCrystalPink, MmCyan, MmOrange, MmCrystalViolet };
        PokeBuilder.Both(s =>
        {
            var face = drum + V(0.268f * s, 0, 0);
            for (int i = 0; i < rings.Length; i++)
                b.Torus(Body, face, 0.12f - 0.027f * i, 0.007f, rings[i], V(0, 0, 90f), mat: Glow, blend: 0.003f);
            b.Ell(Body, face, V(0.01f, 0.025f, 0.025f), MmRing, mat: Glow, blend: 0.004f);
            int arm = MmBone(b, s < 0 ? "armL" : "armR");
            b.PaintEll(arm, V(0.27f * s, 0.55f, 0.04f), V(0.07f, 0.12f, 0.07f), moss);
        });
        MmStone(b, 0f, 0.66f, MmCrystalCyan);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Sableye

    /// <summary>Mega Dynamax Sableye: the red gem it carries grown into a great shield of crystal, crystals of red and violet standing round its rim and the emblem on its face.</summary>
    private static PokeBuilder SableyeMmax()
    {
        var b = MmBase("Sableye-Mega");
        int arm = MmBone(b, "armL");
        var sc = V(-0.13f, 0.24f, 0.16f);
        var ex = V(MathF.Cos(30f * Degree), 0, -MathF.Sin(30f * Degree));
        var facing = V(MathF.Sin(30f * Degree), 0, MathF.Cos(30f * Degree));
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f + 0.2f;
            if (MathF.Sin(a) < -0.2f) continue;
            var rim = sc + ex * (MathF.Cos(a) * 0.12f) + V(0, MathF.Sin(a) * 0.17f, 0);
            var outward = Vector3.Normalize(ex * MathF.Cos(a) * 0.13f + V(0, MathF.Sin(a) * 0.18f, 0));
            float len = 0.05f + 0.045f * (i % 3);
            MmCrystal(b, arm, rim - outward * 0.02f, rim + outward * len + V(0, 0.01f, 0), 0.016f, i % 2 == 0 ? Rgb(224, 70, 100) : MmCrystalViolet);
        }
        b.Ell(arm, sc + facing * 0.03f, V(0.08f, 0.12f, 0.02f), Rgb(180, 90, 220), V(0, 30f, 0), Glow, 0.015f);
        b.Mark(arm, sc + facing * 0.054f, facing, 0.06f, 0.06f, MmRing, MarkShape.Ring);
        b.Mark(arm, sc + facing * 0.054f, facing, 0.042f, 0.042f, MmRing, MarkShape.Star);
        MmStorm(b, 1.5f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Salamence

    /// <summary>Mega Dynamax Salamence: its crimson wings turned to faceted crystal with streaks of orange fire trailing from their edges, and the emblem on its breast plate.</summary>
    private static PokeBuilder SalamenceMmax()
    {
        var b = MmBase("Salamence-Mega");
        int wing = MmBone(b, "wingR");
        foreach (float s in new[] { -1f, 1f })
        {
            for (int i = 0; i < 6; i++)
            {
                float x = s * (0.1f + 0.065f * i);
                var from = V(x, 0.42f - 0.01f * i, -0.08f - 0.015f * i);
                var to = V(x * 1.12f, 0.36f - 0.02f * i, -0.32f - 0.04f * i);
                MmCrystal(b, wing, from, to, 0.03f - 0.003f * i, i % 2 == 0 ? MmOrange : MmRed);
            }
            MmCrystal(b, wing, V(0.3f * s, 0.4f, 0f), V(0.5f * s, 0.55f, -0.1f), 0.032f, MmCrystalPink);
        }
        MmStone(b, 0f, 0.3f, MmCrystalCyan);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Samurott

    /// <summary>Mega Dynamax Samurott: the seamitars blazing cyan along the forelegs, the emblem on its helmet's brow, and sparks of water about its armour.</summary>
    private static PokeBuilder SamurottMmax()
    {
        var b = MmBase("Samurott-Mega");
        foreach (float s in new[] { -1f, 1f })
        {
            int leg = MmBone(b, s < 0 ? "legL" : "legR");
            var from = V(0.12f * s, 0.34f, 0.2f);
            var to = V(0.14f * s, 0.02f, 0.28f);
            b.Limb(leg, to, from + V(0, 0.2f, -0.06f), 0.03f, 0.016f, MmCyan, Glow, 0.006f);
        }
        if (MmHit(b, V(0, 0.62f, 0.7f), V(0, -0.15f, -1f)) is { } brow) MmStoneAt(b, brow, MmNormal(b, brow), 0.034f, MmGold);
        MmStorm(b);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Sceptile

    /// <summary>Mega Dynamax Sceptile: a whole tree of glowing green leaves grown from the end of its tail, orange gems along its coil, and the emblem on its chest.</summary>
    private static PokeBuilder SceptileMmax()
    {
        var b = MmBase("Sceptile-Mega");
        var bark = Rgb(60, 90, 50);
        int tail = MmBone(b, "tail");
        var tip = MmTip(b, tail, V(0, 0, -1));
        var trunk = Smooth(3, tip + V(0, 0.02f, 0.06f), tip + V(0, 0.1f, -0.02f), tip + V(0.02f, 0.22f, -0.04f), tip + V(0, 0.36f, -0.02f));
        b.Tube(tail, trunk, 0.03f, 0.016f, bark, Leaf, 0.008f);
        var top = trunk[^1];
        foreach (var d in new[] { V(-0.7f, 0.6f, 0.1f), V(0.7f, 0.6f, 0.1f), V(-0.35f, 0.95f, -0.2f), V(0.35f, 0.95f, 0.2f), V(0, 1f, -0.4f), V(0, 0.5f, 0.8f) })
        {
            var end = top + Vector3.Normalize(d) * 0.22f;
            b.Limb(tail, top - V(0, 0.05f, 0), end, 0.012f, 0.006f, bark, Leaf, 0.004f);
            foreach (float t in new[] { 0.45f, 0.75f, 1f })
                Frond(b, tail, Vector3.Lerp(top, end, t) - Vector3.Normalize(d) * 0.01f, Vector3.Lerp(top, end, t) + V(0.025f * (t > 0.7f ? 0 : 1), 0.05f, 0.025f), 0.04f, MmLeafGlow, V(0, 1f, 0), 0.5f, Glow, 0.006f);
        }
        foreach (var (x, y, z, r) in new[] { (0f, 0.34f, -0.22f, 0.04f), (0f, 0.36f, -0.36f, 0.035f) })
            b.Ell(tail, V(x, y, z), V(r, r, r), Rgb(240, 130, 90), mat: Glow, blend: 0.008f);
        MmStone(b, 0f, 0.66f, MmCrystalCyan);
        MmStorm(b, 1.1f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Scizor

    /// <summary>Mega Dynamax Scizor: four wings of prismatic crystal, violet and orange, spread behind it, and the emblem on its chest.</summary>
    private static PokeBuilder ScizorMmax()
    {
        var b = MmBase("Scizor-Mega");
        foreach (float s in new[] { -1f, 1f })
        {
            int wing = MmBone(b, s < 0 ? "wingL" : "wingR");
            var root = V(0.05f * s, 0.62f, -0.06f);
            Frond(b, wing, root, V(0.38f * s, 1.12f, -0.12f), 0.12f, MmCrystalViolet, V(0, 0.2f, 1f), 0.22f, Glow, 0.01f);
            Frond(b, wing, root + V(0, -0.04f, 0), V(0.34f * s, 0.5f, -0.1f), 0.09f, MmCrystalViolet, V(0, 0.2f, 1f), 0.22f, Glow, 0.01f);
            Frond(b, wing, root, V(0.33f * s, 1.0f, -0.12f), 0.045f, MmOrange, V(0, 0.2f, 1f), 0.22f, Glow, 0.01f);
            Frond(b, wing, root, V(0.3f * s, 0.52f, -0.1f), 0.035f, MmOrange, V(0, 0.2f, 1f), 0.22f, Glow, 0.01f);
        }
        MmStone(b, 0f, 0.55f, MmCyan);
        MmStorm(b, 1.1f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Scolipede

    /// <summary>Mega Dynamax Scolipede: crystals of glowing pink thrust up along its back and dripping, a swirl of poison about its tail, and the emblem on its brow.</summary>
    private static PokeBuilder ScolipedeMmax()
    {
        var b = MmBase("Scolipede-Mega");
        var pink = Rgb(250, 130, 232);
        var drip = Rgb(210, 90, 230);
        // Crystals along the ridge of its back, each rooted where the ray from above meets it
        foreach (var (z, h) in new[] { (-0.5f, 0.12f), (-0.38f, 0.16f), (-0.24f, 0.2f), (-0.1f, 0.24f), (0.0f, 0.26f), (0.06f, 0.2f), (0.1f, 0.16f) })
        {
            if (MmHit(b, V(0, 1.6f, z), V(0, -1f, 0)) is not { } p) continue;
            int bone = MmBoneAt(b, p);
            MmCrystal(b, bone, p - V(0, 0.04f, 0), p + V(0, h, -h * 0.5f), 0.036f, pink);
            MmCrystal(b, bone, p - V(0, 0.04f, 0) + V(0.02f, 0, 0), p + V(0.05f, h * 0.6f, -h * 0.3f), 0.024f, drip);
            b.Spike(bone, p + V(-0.03f, 0f, 0f), p + V(-0.05f, -0.08f, 0f), 0.014f, drip, 0.8f, Glow, 0.004f);
        }
        if (MmHit(b, V(0, 0.9f, 0.7f), V(0, -0.3f, -1f)) is { } brow) MmStoneAt(b, brow, MmNormal(b, brow), 0.028f, MmRing);
        MmStorm(b, 1.4f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Serperior

    /// <summary>Mega Dynamax Serperior: its leaf wings tipped with violet crystal and spread wide, a hood of crystal leaves behind its head, and the emblem on its brow.</summary>
    private static PokeBuilder SerperiorMmax()
    {
        var b = MmBase("Serperior-Mega");
        int head = MmBone(b, "head");
        foreach (float s in new[] { -1f, 1f })
        {
            var root = V(0.05f * s, 0.4f, -0.02f);
            for (int i = 0; i < 6; i++)
            {
                float a = Degree * (15f + i * 14f);
                var tip = root + V(MathF.Cos(a) * 0.46f * s, MathF.Sin(a) * 0.42f + 0.06f, -0.06f - 0.015f * i);
                Frond(b, Body, root, tip, 0.05f, i % 2 == 0 ? MmCrystalViolet : Rgb(190, 150, 250), V(0, 0.2f, 1f), 0.22f, Glow, 0.01f);
            }
            for (int i = 0; i < 3; i++)
            {
                float a = Degree * (55f + i * 25f);
                Frond(b, head, V(0.02f * s, 0.55f, 0), V(MathF.Cos(a) * 0.22f * s, 0.55f + MathF.Sin(a) * 0.22f, -0.1f), 0.04f, MmCrystalViolet, V(0, 0.2f, 1f), 0.22f, Glow, 0.008f);
            }
        }
        if (MmHit(b, V(0, 0.6f, 0.7f), V(0, -0.2f, -1f)) is { } brow) MmStoneAt(b, brow, MmNormal(b, brow), 0.028f, MmRing);
        MmStorm(b, 1.3f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Sharpedo

    /// <summary>Mega Dynamax Sharpedo: three golden crystals standing along its brow, blades of clear water at its fins, and the emblem at its eye.</summary>
    private static PokeBuilder SharpedoMmax()
    {
        var b = MmBase("Sharpedo-Mega");
        var gold = Rgb(250, 196, 60);
        var ice = Rgb(196, 232, 248);
        foreach (var (z, h) in new[] { (0.14f, 0.1f), (0.04f, 0.13f), (-0.06f, 0.14f) })
            if (MmHit(b, V(0, 0.8f, z), V(0, -1f, 0)) is { } p)
                MmCrystal(b, MmBoneAt(b, p), p - V(0, 0.03f, 0), p + V(0, h, -h * 0.4f), 0.04f, gold);
        foreach (float s in new[] { -1f, 1f })
        {
            int fin = MmBone(b, s < 0 ? "finL" : "finR");
            var tip = b.Model.Skeleton[fin].Joint;
            b.Spike(fin, tip, tip + V(0.36f * s, 0.05f, -0.04f), 0.05f, ice, 0.6f, Glow, 0.006f);
        }
        if (MmHit(b, V(0.6f, 0.36f, 0.12f), V(-1, 0, 0)) is { } eye) MmStoneAt(b, eye, MmNormal(b, eye), 0.035f, gold);
        MmStorm(b, 1.3f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Skarmory

    /// <summary>Mega Dynamax Skarmory: its wings of steel fanned into blades of red and golden crystal, and the emblem on its breast.</summary>
    private static PokeBuilder SkarmoryMmax()
    {
        var b = MmBase("Skarmory-Mega");
        foreach (float s in new[] { -1f, 1f })
        {
            int wing = MmBone(b, s < 0 ? "wingL" : "wingR");
            var root = V(0.08f * s, 0.5f, 0f);
            var cols = new[] { MmRed, MmGold, MmRed, MmGold, MmRed };
            for (int i = 0; i < 5; i++)
            {
                float a = Degree * (25f + i * 12f);
                var tip = root + V(MathF.Cos(a) * (0.5f - 0.05f * i) * s, MathF.Sin(a) * (0.45f - 0.05f * i) - 0.02f, -0.05f - 0.025f * i);
                MmCrystal(b, wing, root + V(0.04f * s, 0, 0), tip, 0.035f - 0.003f * i, cols[i]);
            }
        }
        MmStone(b, 0f, 0.42f, MmCrystalCyan);
        MmStorm(b, 1.1f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Skeledirge

    /// <summary>Mega Dynamax Skeledirge: its crown of fire made a crown of crystal, gold and lilac, standing round a ring of light, and the emblem on its side.</summary>
    private static PokeBuilder SkeledirgeMmax()
    {
        var b = MmBase("Skeledirge-Mega");
        var fire = Rgb(246, 120, 44);
        var heart = Rgb(255, 222, 120);
        int head = MmBone(b, "head");
        var c = V(0, 0.52f, 0.17f);
        b.Torus(head, c, 0.17f, 0.009f, heart, mat: Glow, blend: 0.004f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var at = c + V(MathF.Cos(a) * 0.12f, 0, MathF.Sin(a) * 0.05f);
            float h = 0.2f + 0.09f * MathF.Sin(i * 1.9f);
            MmCrystal(b, head, at - V(0, 0.03f, 0), at + V(MathF.Cos(a) * 0.06f, h, MathF.Sin(a) * 0.05f), 0.022f, i % 2 == 0 ? MmGold : MmCrystalViolet);
        }
        int tail = MmBone(b, "tail");
        var tip = MmTip(b, tail, V(0, 0.3f, -1f));
        FlameTongue(b, tail, tip, tip + V(0.02f, 0.28f, -0.12f), 0.05f, MmViolet, heart);
        FlameTongue(b, tail, tip + V(0, 0, 0.02f), tip + V(-0.06f, 0.2f, -0.08f), 0.03f, fire, heart);
        if (MmHit(b, V(0.6f, 0.26f, 0.0f), V(-1, 0, 0)) is { } side) MmStoneAt(b, side, MmNormal(b, side), 0.05f, heart);
        MmStorm(b, 1.1f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Slowbro

    /// <summary>Mega Dynamax Slowbro: a stair of cyan light winding round its drill of a shell between crystals of violet, and the emblem on its brow.</summary>
    private static PokeBuilder SlowbroMmax()
    {
        var b = MmBase("Slowbro-Mega");
        Vector3? last = null;
        for (int i = 0; i <= 22; i++)
        {
            float u = i / 22f, a = u * MathF.Tau * 2.2f + 0.5f;
            float y = 0.36f + u * 0.4f;
            var dir = V(MathF.Cos(a), 0, MathF.Sin(a));
            if (MmHit(b, V(0, y, 0) + dir * 0.7f, -dir) is not { } p) { last = null; continue; }
            var at = p + dir * 0.012f;
            if (last is { } l) b.Limb(Body, l, at, 0.012f, 0.012f, MmCyan, Glow, 0.004f);
            last = at;
            if (i % 2 == 0) b.Ell(Body, at, V(0.035f, 0.008f, 0.035f), MmBolt, mat: Glow, blend: 0.004f);
        }
        foreach (var (a, y) in new[] { (0.4f, 0.56f), (2.1f, 0.66f), (3.6f, 0.5f), (5.2f, 0.7f) })
        {
            var dir = V(MathF.Cos(a), 0, MathF.Sin(a));
            if (MmHit(b, V(0, y, 0) + dir * 0.7f, -dir) is { } p) MmCrystal(b, Body, p - dir * 0.03f, p + dir * 0.09f + V(0, 0.03f, 0), 0.03f, MmCrystalViolet);
        }
        MmStone(b, 0f, 0.94f, MmCyan, 0.7f);
        MmStorm(b, 1.2f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Starmie

    /// <summary>Mega Dynamax Starmie: a second star of rainbow light, red, gold, cyan and violet, standing behind its own, and the emblem in its gem.</summary>
    private static PokeBuilder StarmieMmax()
    {
        var b = MmBase("Starmie-Mega");
        var cols = new[] { Rgb(250, 90, 96), Rgb(250, 214, 90), MmCrystalCyan, MmCrystalViolet, Rgb(110, 150, 250) };
        var c = V(0, 0.66f, -0.05f);
        for (int i = 0; i < 5; i++)
        {
            float a = (36f + i * 72f) * Degree;
            var dir = V(MathF.Sin(a), MathF.Cos(a), 0);
            b.Spike(Body, c, c + dir * 0.46f, 0.1f, cols[i], 0.22f, Glow, 0.012f);
        }
        if (MmHit(b, V(0, 0.66f, 0.7f), V(0, 0, -1)) is { } gem) MmStoneAt(b, gem, V(0, 0, 1), 0.042f, MmCyan);
        MmStorm(b, 1.1f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Steelix

    /// <summary>Mega Dynamax Steelix: great crystals of violet and cyan thrust out between its segments, rings of cyan light let into them, and the emblem on its brow.</summary>
    private static PokeBuilder SteelixMmax()
    {
        var b = MmBase("Steelix-Mega");
        var joints = new[] { "body", "seg1", "seg2", "seg3", "seg4", "seg5", "seg6", "seg7" };
        for (int i = 0; i < joints.Length; i++)
        {
            int bone = MmBone(b, joints[i]);
            var j = b.Model.Skeleton[bone].Joint;
            float s = i % 2 == 0 ? 1f : -1f;
            var dir = Vector3.Normalize(V(0.7f * s, 0.55f, i % 3 == 0 ? 0.4f : -0.3f));
            MmCrystal(b, bone, j, j + dir * (0.22f - 0.012f * i), 0.04f - 0.002f * i, i % 2 == 0 ? MmCrystalViolet : MmCrystalCyan);
            MmCrystal(b, bone, j + V(0, 0.01f, 0), j + Vector3.Normalize(V(-0.5f * s, 0.8f, -0.2f)) * 0.12f, 0.026f, i % 2 == 0 ? MmCrystalCyan : MmCrystalViolet);
        }
        if (MmHit(b, V(0, 1.1f, 0.7f), V(0, -0.35f, -1f)) is { } brow) MmStoneAt(b, brow, MmNormal(b, brow), 0.03f, MmCyan);
        MmStorm(b, 1.1f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Swampert

    /// <summary>Mega Dynamax Swampert: a great breaking wave of crystal water standing over its back and curling toward its head, aqua fins lit, and the emblem on its chest.</summary>
    private static PokeBuilder SwampertMmax()
    {
        var b = MmBase("Swampert-Mega");
        var aqua = Rgb(110, 230, 236);
        var deep = Rgb(70, 170, 220);
        for (int i = 0; i < 11; i++)
        {
            float x = (i - 5) * 0.052f;
            float lift = 1f - MathF.Abs(i - 5) / 6.5f;
            var root = V(x, 0.5f, -0.12f);
            var crest = V(x * 1.25f, 0.62f + 0.42f * lift, -0.2f - 0.1f * lift);
            var curl = V(x * 1.3f, 0.7f + 0.46f * lift, 0.02f);
            b.Spike(Body, root, crest, 0.06f, i % 2 == 0 ? aqua : deep, 0.6f, Glow, 0.012f);
            b.Spike(Body, crest - V(0, 0.04f, 0), curl, 0.05f, i % 2 == 0 ? MmBolt : aqua, 0.6f, Glow, 0.01f);
        }
        MmStone(b, 0f, 0.3f, MmCrystalCyan, 1.1f);
        MmStorm(b, 1.15f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Torterra

    /// <summary>Mega Dynamax Torterra: its three peaks turned to crystal of violet and rose, more crystals thrust up among them, and the emblem on its brow.</summary>
    private static PokeBuilder TorterraMmax()
    {
        var b = MmBase("Torterra-Mega");
        Recolour(b, (Rgb(124, 108, 96), Rgb(112, 76, 200)), (Rgb(240, 244, 250), Rgb(250, 196, 230)));
        if (MmHit(b, V(0, 0.64f, 1.6f), V(0, -0.1f, -1f)) is { } brow) MmStoneAt(b, brow, MmNormal(b, brow), 0.045f, MmGold);
        foreach (var (x, z) in new[] { (-0.22f, 0.12f), (0.12f, 0.18f), (-0.3f, -0.25f) })
        {
            var at = V(x, 1.1f, z);
            MmCrystal(b, Body, at - V(0, 0.2f, 0), at + V(0.02f, 0.2f, 0.02f), 0.05f, MmCrystalViolet);
        }
        MmStorm(b, 1.0f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Typhlosion

    /// <summary>Mega Dynamax Typhlosion: three great blades of flame-coloured crystal standing up out of its crater, and the emblem on its brow.</summary>
    private static PokeBuilder TyphlosionMmax()
    {
        var b = MmBase("Typhlosion-Mega");
        var peach = Rgb(255, 176, 110);
        var heart = Rgb(255, 232, 150);
        var crater = V(0, 0.56f, -0.11f);
        foreach (var (x, z, h) in new[] { (0f, -0.02f, 0.5f), (-0.1f, 0.01f, 0.36f), (0.1f, 0f, 0.38f) })
        {
            MmCrystal(b, Body, crater + V(x, 0.03f, z), crater + V(x * 1.5f, h, z - 0.06f), 0.07f, peach);
            MmCrystal(b, Body, crater + V(x * 0.6f, 0.03f, z), crater + V(x * 0.8f, h * 0.7f, z - 0.03f), 0.04f, heart);
        }
        if (MmHit(b, V(0, 0.72f, 0.7f), V(0, -0.1f, -1f)) is { } brow) MmStoneAt(b, brow, MmNormal(b, brow), 0.028f, MmGold);
        MmStorm(b, 1.3f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Tyranitar

    /// <summary>Mega Dynamax Tyranitar: cracks of violet and cyan lightning running over its armour, and the emblem in the middle of its chest.</summary>
    private static PokeBuilder TyranitarMmax()
    {
        var b = MmBase("Tyranitar-Mega");
        MmCrack(b, -0.14f, 0.82f, 0.03f, -0.06f, 6, MmCrystalCyan);
        MmCrack(b, 0.16f, 0.78f, -0.03f, -0.07f, 6, MmCrystalViolet);
        MmCrack(b, -0.2f, 0.5f, 0.035f, -0.05f, 5, MmCrystalViolet);
        MmCrack(b, 0.18f, 0.56f, -0.03f, -0.06f, 5, MmCrystalCyan);
        MmCrack(b, 0.02f, 0.34f, 0.025f, -0.04f, 4, MmBolt);
        MmStone(b, 0f, 0.62f, MmCrystalCyan, 1.1f);
        MmStorm(b, 1.15f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Venusaur

    /// <summary>Mega Dynamax Venusaur: a crown of violet and rose crystals standing from the heart of its flower, and the emblem on its brow.</summary>
    private static PokeBuilder VenusaurMmax()
    {
        var b = MmBase("Venusaur-Mega");
        var rose = Rgb(250, 190, 240);
        var centre = V(0, 0.98f, -0.08f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var at = centre + V(MathF.Cos(a) * 0.05f, 0, MathF.Sin(a) * 0.05f);
            MmCrystal(b, MmBone(b, "flower"), at - V(0, 0.04f, 0), at + V(MathF.Cos(a) * 0.05f, 0.14f + 0.07f * (i % 2), MathF.Sin(a) * 0.05f), 0.02f, i % 2 == 0 ? MmCrystalViolet : rose);
        }
        if (MmHit(b, V(0, 0.52f, 0.9f), V(0, -0.1f, -1f)) is { } brow) MmStoneAt(b, brow, MmNormal(b, brow), 0.035f, MmCrystalPink);
        MmStorm(b, 0.8f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Dynamax Victreebel

    /// <summary>Mega Dynamax Victreebel: a pool of pink and violet light glowing in its great mouth with drops of slime hanging at its rim, and the emblem large on its belly.</summary>
    private static PokeBuilder VictreebelMmax()
    {
        var b = MmBase("Victreebel-Mega");
        var glow = Rgb(238, 90, 230);
        var slime = Rgb(190, 238, 190);
        int head = MmBone(b, "head");
        var mouth = V(0, 0.74f, 0.02f);
        b.Ell(head, mouth, V(0.09f, 0.03f, 0.09f), glow, mat: Glow, blend: 0.01f);
        b.Ell(head, mouth + V(0, 0.012f, 0), V(0.06f, 0.02f, 0.06f), MmCrystalViolet, mat: Glow, blend: 0.01f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var rim = mouth + V(MathF.Cos(a) * 0.09f, -0.02f, MathF.Sin(a) * 0.09f);
            b.Spike(head, rim + V(0, 0.02f, 0), rim + V(0, -0.07f - 0.02f * (i % 3), 0), 0.012f, slime, 0.8f, Glow, 0.004f);
            if (i % 2 == 0) MmCrystal(b, head, rim, rim + V(MathF.Cos(a) * 0.03f, 0.07f, MathF.Sin(a) * 0.03f), 0.012f, MmRing);
        }
        MmStone(b, 0f, 0.3f, MmCrystalPink, 1.7f);
        MmStorm(b, 1.2f);
        return b;
    }
}
