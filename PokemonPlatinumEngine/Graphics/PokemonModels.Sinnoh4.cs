using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Plan 03 · D9: hand-built models for the last batch of Platinum's Sinnoh Pokédex: Tangela (181) to Absol (209), and
// the legendaries the earlier batches left for it, Uxie, Mesprit, Azelf, Dialga, Palkia and Manaphy (146–151). With
// it every species of the Sinnoh Pokédex is hand-built. Helpers shared with the earlier batches are in
// PokemonModels.Sinnoh1.cs, PokemonModels.Sinnoh2.cs and PokemonModels.Sinnoh3.cs.
internal static partial class PokemonModels
{
    /// <summary>The turn (degrees, as <see cref="PokeBuilder.Ell"/> takes it) that faces an ellipsoid's local z along <paramref name="dir"/>: a plate lying flat on a surface.</summary>
    private static Vector3 Toward(Vector3 dir)
    {
        var n = Vector3.Normalize(dir);
        return V(-MathF.Asin(Math.Clamp(n.Y, -1f, 1f)) / Degree, MathF.Atan2(n.X, n.Z) / Degree, 0);
    }

    /// <summary>
    /// An oval leaf or frond from <paramref name="root"/> to <paramref name="tip"/>, widest in the middle and flat
    /// across <paramref name="facing"/> (the way its face looks), <paramref name="thin"/> as thick as it is wide.
    /// </summary>
    private static SdfPrimitive Frond(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float width, Color color, Vector3 facing, float thin = 0.2f, SurfaceMaterial? mat = null, float blend = 0.012f)
    {
        var y = Vector3.Normalize(tip - root);
        var z = Vector3.Normalize(facing - y * Vector3.Dot(facing, y));
        var x = Vector3.Cross(y, z);
        var p = b.Ell(bone, (root + tip) / 2f, V(width, Vector3.Distance(root, tip) / 2f, width * thin), color, mat: mat, blend: blend);
        p.Rotation = Quaternion.CreateFromRotationMatrix(new Matrix4x4(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1));
        return p;
    }

    // ------------------------------------------------------------------ Tangela line

    /// <summary>
    /// A tangle of vines over a ball: arcs of tube round its surface, rising off it in loops, spread by the golden
    /// angle and turned by <paramref name="seed"/>. The front within <paramref name="face"/> of straight ahead stays
    /// bare for the face, and nothing grows under its feet.
    /// </summary>
    private static void Vines(PokeBuilder b, int bone, Vector3 center, Vector3 radii, int count, float thick, Color color, float face, uint seed)
    {
        var rng = new GenomeRandom(seed);
        for (int i = 0; i < count; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / count;
            float ring = MathF.Sqrt(1f - y * y);
            float a = i * 2.39996f + rng.Float() * 0.5f;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            float turn = rng.Range(0f, MathF.PI), span = rng.Range(0.32f, 0.55f), lift = rng.Range(0.08f, 0.2f);
            if (n.Z > face || y < -0.8f) continue;
            var t = Vector3.Normalize(Vector3.Cross(n, MathF.Abs(n.Y) < 0.95f ? Vector3.UnitY : Vector3.UnitX));
            var u = t * MathF.Cos(turn) + Vector3.Cross(n, t) * MathF.Sin(turn);
            var points = new Vector3[5];
            for (int k = 0; k < points.Length; k++)
            {
                float s = k / 2f - 1f;
                var dir = Vector3.Normalize(n * MathF.Cos(s * span) + u * MathF.Sin(s * span));
                points[k] = center + dir * radii * (1f + lift * MathF.Cos(s * MathF.PI / 2f));
            }
            b.Tube(bone, Smooth(2, points), thick, thick, color, blend: 0.004f);
        }
    }

    private static PokeBuilder Tangela()
    {
        var b = new PokeBuilder("Tangela", 0.62f, BodyPlan.Biped, V(0, 0.26f, 0)) { Coat = Leaf };
        var blue = Rgb(64, 138, 222);
        var deep = Rgb(34, 82, 156);
        var red = Rgb(232, 64, 78);
        var black = Rgb(22, 22, 30);

        // Big red feet poking out under the tangle
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.1f, 0.02f));
            b.Ell(leg, V(0.1f * s, 0.05f, 0.05f), V(0.065f, 0.05f, 0.085f), red, mat: Fur);
        });
        // A ball of vines: dark in the gaps, the vines over it, a black face looking out of the middle
        var c = V(0, 0.27f, 0);
        var r = V(0.2f, 0.19f, 0.19f);
        b.Ell(Body, c, r, deep);
        b.PaintEll(Body, V(0, 0.28f, 0.16f), V(0.11f, 0.07f, 0.08f), black);
        Vines(b, Body, c, r, 46, 0.02f, blue, 0.72f, 114);
        b.Tube(Body, Smooth(3, V(-0.13f, 0.2f, 0.13f), V(-0.05f, 0.2f, 0.2f), V(0.04f, 0.215f, 0.2f), V(0.13f, 0.2f, 0.13f)), 0.02f, 0.02f, blue, blend: 0.004f);
        // Two loose vines for arms
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.27f, 0.03f));
            b.Tube(arm, Smooth(3, V(0.16f * s, 0.27f, 0.04f), V(0.24f * s, 0.27f, 0.08f), V(0.28f * s, 0.21f, 0.09f), V(0.25f * s, 0.17f, 0.07f)), 0.02f, 0.016f, blue, blend: 0.004f);
        });
        int head = b.Head(V(0, 0.4f, 0));
        b.Tube(head, Smooth(3, V(-0.04f, 0.44f, 0.02f), V(-0.02f, 0.51f, 0.0f), V(0.04f, 0.52f, -0.02f), V(0.06f, 0.47f, -0.01f)), 0.02f, 0.018f, blue, blend: 0.004f);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.045f * s, 0.29f), V(0.25f * s, 0.05f, 1f), 0.028f, sclera: true, pupil: black));
        return b;
    }

    /// <summary>Vines wound round a limb: <paramref name="strands"/> spirals about the path, each making <paramref name="turns"/> turns.</summary>
    private static void Wound(PokeBuilder b, int bone, Vector3[] path, float radius, int strands, float turns, float thick, Color color)
    {
        for (int k = 0; k < strands; k++)
        {
            var points = new Vector3[path.Length];
            for (int i = 0; i < path.Length; i++)
            {
                var along = Vector3.Normalize(path[Math.Min(path.Length - 1, i + 1)] - path[Math.Max(0, i - 1)]);
                var u = Vector3.Normalize(Vector3.Cross(along, MathF.Abs(along.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX));
                var w = Vector3.Cross(along, u);
                float a = MathF.Tau * (turns * i / (path.Length - 1f) + (float)k / strands);
                points[i] = path[i] + (u * MathF.Cos(a) + w * MathF.Sin(a)) * radius;
            }
            b.Tube(bone, points, thick, thick, color, blend: 0.004f);
        }
    }

    private static PokeBuilder Tangrowth()
    {
        var b = new PokeBuilder("Tangrowth", 0.92f, BodyPlan.Biped, V(0, 0.44f, 0)) { Coat = Leaf };
        var blue = Rgb(58, 132, 214);
        var deep = Rgb(30, 74, 146);
        var red = Rgb(228, 58, 74);
        var black = Rgb(22, 22, 30);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.14f, 0.0f));
            b.Limb(leg, V(0.11f * s, 0.16f, 0.0f), V(0.12f * s, 0.05f, 0.02f), 0.05f, 0.045f, black, Fur);
            b.Ell(leg, V(0.12f * s, 0.04f, 0.06f), V(0.06f, 0.04f, 0.075f), red, mat: Fur);
        });
        // A great heap of vines, taller than wide, ends of vine hanging from it all round like a ragged skirt
        var c = V(0, 0.45f, 0);
        var r = V(0.27f, 0.3f, 0.24f);
        b.Ell(Body, c, r, deep);
        b.PaintEll(Body, V(0, 0.57f, 0.2f), V(0.13f, 0.065f, 0.07f), black);
        Vines(b, Body, c, r, 70, 0.026f, blue, 0.8f, 465);
        var rng = new GenomeRandom(4650);
        for (int i = 0; i < 13; i++)
        {
            float a = (i - 6f) * 0.42f + rng.Range(-0.12f, 0.12f), drop = rng.Range(0.08f, 0.17f), curl = rng.Range(-0.04f, 0.04f);
            var top = c + V(MathF.Sin(a) * 0.23f, -0.2f, MathF.Cos(a) * 0.19f);
            var outward = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Tube(Body, Smooth(2, top, top + outward * 0.04f + V(curl, -drop * 0.6f, 0), top + outward * 0.03f + V(curl * 2f, -drop, 0)), 0.026f, 0.018f, blue, blend: 0.004f);
        }
        // Long arms of vine wound round each other, red fingers at their ends
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.24f * s, 0.54f, 0.02f));
            var path = Smooth(3, V(0.22f * s, 0.55f, 0.02f), V(0.36f * s, 0.55f, 0.08f), V(0.44f * s, 0.44f, 0.12f), V(0.46f * s, 0.3f, 0.12f));
            b.Tube(arm, path, 0.04f, 0.034f, deep, blend: 0f);
            Wound(b, arm, path, 0.03f, 3, 1.5f, 0.02f, blue);
            Digits(b, arm, V(0.46f * s, 0.29f, 0.12f), V(0, -1f, 0.15f), V(0.45f * s, 0, 0.2f), 0.07f, 0.026f, red, Fur);
        });
        int head = b.Head(V(0, 0.7f, 0));
        b.Tube(head, Smooth(3, V(-0.06f, 0.72f, 0.02f), V(-0.04f, 0.8f, 0.0f), V(0.04f, 0.81f, -0.03f), V(0.07f, 0.75f, -0.02f)), 0.026f, 0.022f, blue, blend: 0.004f);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.05f * s, 0.575f), V(0.25f * s, 0.05f, 1f), 0.027f, sclera: true, pupil: black));
        return b;
    }

    // ------------------------------------------------------------------ Yanma line

    /// <summary>
    /// A dragonfly's wing out from <paramref name="root"/> on side <paramref name="s"/>: clear, rimmed, its tip
    /// coloured, swept back by <paramref name="sweep"/> degrees, raised by <paramref name="dihedral"/> and tipped
    /// forward by <paramref name="pitch"/> so its face shows to the front.
    /// </summary>
    private static void Dragonfly(PokeBuilder b, int bone, Vector3 root, float s, float length, float width, float sweep, Color clear, Color rim, Color tip, float tipShare,
        float dihedral = 18f, float pitch = 25f)
    {
        var turn = V(pitch, sweep * s, dihedral * s);
        var q = Quaternion.CreateFromYawPitchRoll(turn.Y * Degree, turn.X * Degree, turn.Z * Degree);
        var along = Vector3.Transform(Vector3.UnitX, q) * s;
        var center = root + along * length;
        RimmedFin(b, bone, center, V(length, 0.007f, width), turn, clear, rim, 0.012f);
        b.PaintEll(bone, center + along * length * (1f - tipShare * 0.6f), V(length * tipShare, 0.03f, width * 1.3f), tip, turn);
    }

    private static PokeBuilder Yanma()
    {
        var b = new PokeBuilder("Yanma", 0.7f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var red = Rgb(222, 52, 56);
        var lime = Rgb(160, 214, 40);
        var leg = Rgb(92, 128, 168);
        var clear = Rgb(234, 238, 244);

        // A red thorax, thin legs folded under it and a long thin tail ending in four little vanes
        b.Ell(Body, V(0, 0.42f, 0.03f), V(0.06f, 0.055f, 0.075f), red);
        foreach (var (z, front) in new[] { (0.06f, true), (0.0f, false) })
            PokeBuilder.Both(s =>
            {
                int l = b.Leg(s, V(0.03f * s, 0.39f, z), front);
                b.Limb(l, V(0.03f * s, 0.39f, z), V(0.09f * s, 0.34f, z + 0.03f), 0.011f, 0.01f, leg, Shell, 0.006f);
                b.Limb(l, V(0.09f * s, 0.34f, z + 0.03f), V(0.1f * s, 0.27f, z + 0.07f), 0.01f, 0.008f, leg, Shell, 0.004f);
            });
        int tail = b.Tail(V(0, 0.43f, -0.03f));
        var path = Smooth(3, V(0, 0.43f, -0.03f), V(0, 0.45f, -0.16f), V(0, 0.48f, -0.3f), V(0, 0.5f, -0.4f));
        b.Tube(tail, path, 0.026f, 0.018f, red, blend: 0f);
        for (int i = 1; i < 4; i++) b.PaintTorus(tail, path[i * 2 + 1], 0.024f, 0.006f, Rgb(250, 120, 110), Euler(path[i * 2 + 2] - path[i * 2]));
        var end = path[^1];
        foreach (var d in new[] { V(1f, 0, 0), V(-1f, 0, 0), V(0, 1f, 0), V(0, -1f, 0) })
            b.Ell(tail, end + d * 0.035f, V(0.016f, 0.035f, 0.01f), red, Euler(d), blend: 0.008f);
        b.Ell(tail, end + V(0, 0, -0.01f), V(0.012f, 0.012f, 0.014f), White, blend: 0.004f);
        // Four clear wings with orange tips
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.04f * s, 0.46f, 0.05f));
            Dragonfly(b, wing, V(0.045f * s, 0.455f, 0.06f), s, 0.17f, 0.045f, -8f, clear, Rgb(200, 206, 218), Rgb(244, 140, 44), 0.25f);
            int back = b.Part(s < 0 ? "wing2L" : "wing2R", Body, V(0.04f * s, 0.46f, 0.0f), PokeRole.Wing, 0.4f, s);
            Dragonfly(b, back, V(0.045f * s, 0.45f, 0.0f), s, 0.16f, 0.042f, 12f, clear, Rgb(200, 206, 218), Rgb(244, 140, 44), 0.25f);
        });
        // A great lime head that is all eye, red jaws under it, two red horns
        int head = b.Head(V(0, 0.44f, 0.09f));
        var c = V(0, 0.44f, 0.15f);
        var r = V(0.085f, 0.08f, 0.075f);
        b.Ell(head, c, r, lime);
        b.Ell(head, V(0, 0.385f, 0.2f), V(0.04f, 0.028f, 0.03f), red, blend: 0.012f);
        b.Mark(head, V(0, 0.38f, 0.229f), V(0, -0.2f, 1f), 0.02f, 0.006f, Rgb(140, 30, 40), MarkShape.Bar);
        PokeBuilder.Both(s => b.Spike(head, V(0.025f * s, 0.505f, 0.13f), V(0.05f * s, 0.6f, 0.1f), 0.018f, red, blend: 0.008f));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.45f), V(0.6f * s, 0.05f, 1f), 0.026f));
        return Lift(b);
    }

    private static PokeBuilder Yanmega()
    {
        var b = new PokeBuilder("Yanmega", 0.92f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var green = Rgb(86, 160, 72);
        var dark = Rgb(50, 104, 52);
        var pink = Rgb(236, 82, 112);
        var ring = Rgb(150, 26, 50);
        var black = Rgb(40, 40, 46);
        var clear = Rgb(234, 238, 244);

        // A green thorax with three black spikes, black legs folded under it
        b.Ell(Body, V(0, 0.42f, 0.04f), V(0.1f, 0.085f, 0.12f), green);
        foreach (var (z, h) in new[] { (0.1f, 0.13f), (0.04f, 0.15f), (-0.02f, 0.12f) })
            b.Spike(Body, V(0, 0.48f, z), V(0, 0.48f + h, z - 0.05f), 0.03f, black, mat: Shell, blend: 0.008f);
        foreach (var (z, front) in new[] { (0.1f, true), (0.0f, false) })
            PokeBuilder.Both(s =>
            {
                int l = b.Leg(s, V(0.05f * s, 0.37f, z), front);
                b.Limb(l, V(0.05f * s, 0.37f, z), V(0.13f * s, 0.32f, z + 0.03f), 0.016f, 0.014f, black, Shell, 0.006f);
                b.Limb(l, V(0.13f * s, 0.32f, z + 0.03f), V(0.15f * s, 0.22f, z + 0.08f), 0.014f, 0.01f, black, Shell, 0.004f);
                b.Spike(l, V(0.14f * s, 0.27f, z + 0.055f), V(0.17f * s, 0.27f, z + 0.03f), 0.007f, black, blend: 0.003f);
            });
        // A long tail curving up behind, banded, pink spots along it and two black points at its end
        int tail = b.Tail(V(0, 0.44f, -0.06f));
        var path = Smooth(3, V(0, 0.44f, -0.06f), V(0, 0.46f, -0.2f), V(0, 0.53f, -0.33f), V(0, 0.64f, -0.41f), V(0, 0.74f, -0.42f));
        b.Tube(tail, path, 0.06f, 0.04f, green, blend: 0f);
        for (int i = 1; i < path.Length - 1; i += 2)
        {
            var along = path[i + 1] - path[i - 1];
            float k = 1f - (float)i / path.Length;
            b.PaintTorus(tail, path[i], 0.04f + 0.02f * k, 0.006f, dark, Euler(along));
            var side = Vector3.Normalize(Vector3.Cross(along, Vector3.UnitX));
            b.PaintEll(tail, path[i] - side * (0.04f + 0.02f * k), V(0.022f, 0.022f, 0.022f), pink);
        }
        PokeBuilder.Both(s => b.Spike(tail, path[^1] + V(0.018f * s, 0, 0), path[^1] + V(0.04f * s, 0.1f, 0.02f), 0.022f, black, mat: Shell, blend: 0.006f));
        // Four wide wings, clear with pink tips
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.07f * s, 0.48f, 0.06f));
            Dragonfly(b, wing, V(0.07f * s, 0.47f, 0.08f), s, 0.23f, 0.07f, -10f, clear, Rgb(206, 210, 220), pink, 0.38f);
            int back = b.Part(s < 0 ? "wing2L" : "wing2R", Body, V(0.07f * s, 0.48f, 0.0f), PokeRole.Wing, 0.4f, s);
            Dragonfly(b, back, V(0.07f * s, 0.465f, 0.0f), s, 0.22f, 0.065f, 14f, clear, Rgb(206, 210, 220), pink, 0.38f);
        });
        // A head that is two great red eyes ringed in dark lines, with white fangs below
        int head = b.Head(V(0, 0.42f, 0.14f));
        b.Ell(head, V(0, 0.41f, 0.19f), V(0.075f, 0.07f, 0.07f), green);
        PokeBuilder.Both(s =>
        {
            var eye = V(0.06f * s, 0.42f, 0.22f);
            var outward = Vector3.Normalize(V(0.75f * s, 0.05f, 0.7f));
            b.Ell(head, eye, V(0.07f, 0.07f, 0.07f), pink, blend: 0.01f);
            foreach (float d in new[] { 0.035f, 0.052f })
                b.PaintTorus(head, eye + outward * d, MathF.Sqrt(0.0049f - d * d), 0.005f, ring, Euler(outward));
            b.PaintEll(head, eye + outward * 0.065f, V(0.026f, 0.026f, 0.026f), ring);
            b.Spike(head, V(0.025f * s, 0.355f, 0.23f), V(0.02f * s, 0.3f, 0.26f), 0.013f, White, mat: Shell, blend: 0.004f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Tropius

    private static PokeBuilder Tropius()
    {
        var b = new PokeBuilder("Tropius", 0.96f, BodyPlan.Quadruped, V(0, 0.36f, -0.04f)) { Coat = Scales };
        var tan = Rgb(206, 166, 112);
        var green = Rgb(84, 172, 64);
        var vein = Rgb(52, 120, 46);
        var yellow = Rgb(250, 222, 72);

        // A heavy brown body on four short legs with yellow claws
        foreach (var (z, front) in new[] { (0.12f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.13f * s, 0.22f, z), front);
                b.Limb(leg, V(0.13f * s, 0.24f, z), V(0.14f * s, 0.05f, z + 0.01f), 0.078f, 0.07f, tan);
                Claws(b, leg, V(0.14f * s, 0.025f, z + 0.068f), V(0.035f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.014f);
            });
        b.Ell(Body, V(0, 0.36f, -0.04f), V(0.21f, 0.18f, 0.29f), tan);
        b.PaintEll(Body, V(0, 0.24f, 0.0f), V(0.18f, 0.07f, 0.25f), Rgb(232, 204, 158));
        int tail = b.Tail(V(0, 0.33f, -0.3f));
        b.Spike(tail, V(0, 0.32f, -0.28f), V(0, 0.26f, -0.44f), 0.06f, tan);
        // A thick neck rising in a curve, bananas growing in a bunch under the chin
        var neck = Smooth(3, V(0, 0.44f, 0.14f), V(0, 0.58f, 0.22f), V(0, 0.71f, 0.22f), V(0, 0.81f, 0.27f));
        b.Tube(Body, neck, 0.095f, 0.07f, tan, blend: 0f);
        int head = b.Head(V(0, 0.81f, 0.27f));
        b.Ell(head, V(0, 0.85f, 0.32f), V(0.072f, 0.062f, 0.088f), tan);
        b.Ell(head, V(0, 0.825f, 0.405f), V(0.047f, 0.038f, 0.062f), tan, blend: 0.02f);
        b.Mark(head, V(0, 0.808f, 0.462f), V(0, -0.3f, 1f), 0.016f, 0.006f, Rgb(140, 70, 60), MarkShape.Bar);
        for (int i = 0; i < 5; i++)
        {
            float a = (i - 2f) * 0.5f;
            var stem = V(MathF.Sin(a) * 0.035f, 0.78f, 0.29f + MathF.Cos(a) * 0.02f);
            var tip = stem + V(MathF.Sin(a) * 0.05f, -0.1f, 0.04f);
            b.Tube(head, Smooth(2, stem, (stem + tip) / 2f + V(0, 0, 0.015f), tip), 0.018f, 0.012f, yellow, Leaf, 0.006f);
            b.Ell(head, tip, V(0.008f, 0.008f, 0.008f), Rgb(110, 80, 50), blend: 0.003f);
        }
        // Leaves over its head and down the back of its neck, and four great fronds on its back for wings
        Frond(b, head, V(0, 0.885f, 0.36f), V(0, 0.92f, 0.12f), 0.065f, green, V(0, 1f, 0.1f), 0.3f, Leaf);
        PokeBuilder.Both(s => Frond(b, head, V(0.03f * s, 0.875f, 0.33f), V(0.11f * s, 0.84f, 0.12f), 0.055f, green, V(0.6f * s, 1f, 0), 0.3f, Leaf));
        Frond(b, Body, V(0, 0.78f, 0.16f), V(0, 0.56f, 0.07f), 0.07f, green, V(0, 0.3f, -1f), 0.3f, Leaf);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.09f * s, 0.5f, -0.02f));
            foreach (var (root, tip, w, up) in new[] { (V(0.08f * s, 0.5f, 0.0f), V(0.6f * s, 0.84f, 0.04f), 0.1f, 0.4f), (V(0.08f * s, 0.48f, -0.1f), V(0.6f * s, 0.46f, -0.26f), 0.095f, 0.65f) })
            {
                var facing = V(0.1f * s, up, 0.85f);
                var p = Frond(b, wing, root, tip, w, green, facing, 0.1f, Leaf);
                var n = Vector3.Transform(Vector3.UnitZ, p.Rotation);
                b.Tube(wing, new[] { root + n * 0.012f, Vector3.Lerp(root, tip, 0.5f) + n * 0.02f, Vector3.Lerp(root, tip, 0.9f) + n * 0.008f }, 0.011f, 0.005f, vein, Leaf, 0.004f);
            }
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.054f * s, 0.865f, 0.36f), V(0.8f * s, 0.2f, 0.6f), 0.017f));
        return b;
    }

    // ------------------------------------------------------------------ Rhyhorn line

    /// <summary>A drill of a horn: a cone with grooves round it.</summary>
    private static void Drill(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float r, Color color, Color groove, int grooves = 3)
    {
        b.Spike(bone, root, tip, r, color, mat: Shell, blend: 0.006f);
        var along = tip - root;
        float rt = MathF.Max(0.004f, r * 0.12f);
        for (int k = 1; k <= grooves; k++)
        {
            float t = k / (grooves + 1f);
            b.PaintTorus(bone, root + along * t, r + (rt - r) * t, 0.0045f, groove, Euler(along));
        }
    }

    private static PokeBuilder Rhyhorn()
    {
        var b = new PokeBuilder("Rhyhorn", 0.8f, BodyPlan.Quadruped, V(0, 0.24f, -0.03f)) { Coat = Shell };
        var gray = Rgb(164, 172, 192);
        var pale = Rgb(206, 212, 226);
        var dark = Rgb(132, 140, 160);

        foreach (var (z, front) in new[] { (0.1f, true), (-0.16f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.12f * s, 0.16f, z), front);
                b.Limb(leg, V(0.12f * s, 0.18f, z), V(0.13f * s, 0.05f, z + 0.01f), 0.066f, 0.06f, gray);
                Claws(b, leg, V(0.13f * s, 0.02f, z + 0.06f), V(0.026f, 0, 0), V(0, -0.1f, 1f), 0.022f, 0.012f);
            });
        // A low barrel of a body in rocky plates, a ridge of points down its back
        b.Ell(Body, V(0, 0.24f, -0.03f), V(0.18f, 0.14f, 0.26f), gray);
        foreach (var (z, y) in new[] { (0.1f, 0.35f), (0.0f, 0.37f), (-0.1f, 0.36f), (-0.2f, 0.32f) })
        {
            Blade(b, Body, V(0, y - 0.02f, z), V(0, y + 0.07f, z - 0.05f), 0.05f, pale, V(1f, 0, 0), 0.35f);
            PokeBuilder.Both(s => b.Box(Body, V(0.12f * s, y - 0.07f, z), V(0.055f, 0.045f, 0.02f), 0.012f, dark, Toward(V(0.8f * s, 0.6f, 0)), blend: 0.012f));
        }
        int tail = b.Tail(V(0, 0.23f, -0.27f));
        b.Spike(tail, V(0, 0.23f, -0.25f), V(0, 0.19f, -0.4f), 0.045f, gray);
        // A great wedge of a head, a horn on its nose, two spikes behind like ears
        int head = b.Head(V(0, 0.27f, 0.18f));
        var c = V(0, 0.25f, 0.27f);
        var r = V(0.115f, 0.1f, 0.13f);
        b.Ell(head, c, r, gray);
        b.Ell(head, V(0, 0.22f, 0.37f), V(0.08f, 0.068f, 0.075f), gray, blend: 0.03f);
        b.PaintEll(head, V(0, 0.18f, 0.33f), V(0.085f, 0.04f, 0.11f), pale);
        b.Spike(head, V(0, 0.27f, 0.39f), V(0, 0.41f, 0.43f), 0.042f, pale, mat: Shell);
        PokeBuilder.Both(s => Blade(b, head, V(0.07f * s, 0.3f, 0.2f), V(0.12f * s, 0.41f, 0.12f), 0.04f, pale, V(0.6f * s, 0, 0.8f), 0.35f));
        b.Mark(head, V(0, 0.195f, 0.443f), V(0, -0.1f, 1f), 0.03f, 0.006f, Rgb(80, 84, 100), MarkShape.Bar);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.078f * s, 0.27f), V(0.85f * s, 0.1f, 0.5f), 0.018f, Rgb(220, 40, 50), glare: true));
        return b;
    }

    private static PokeBuilder Rhydon()
    {
        var b = new PokeBuilder("Rhydon", 0.92f, BodyPlan.Biped, V(0, 0.44f, 0)) { Coat = Shell };
        var gray = Rgb(160, 170, 192);
        var cream = Rgb(232, 222, 196);
        var pale = Rgb(214, 220, 232);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.24f, 0.0f));
            b.Ell(leg, V(0.12f * s, 0.2f, 0.0f), V(0.08f, 0.1f, 0.09f), gray);
            b.Limb(leg, V(0.12f * s, 0.15f, 0.0f), V(0.13f * s, 0.05f, 0.02f), 0.06f, 0.055f, gray);
            Claws(b, leg, V(0.13f * s, 0.02f, 0.07f), V(0.025f, 0, 0), V(0, -0.1f, 1f), 0.024f, 0.012f);
        });
        // An upright body, cream down the front, a thick banded tail and a ridge of plates down its back
        b.Ell(Body, V(0, 0.42f, 0), V(0.17f, 0.22f, 0.15f), gray);
        b.PaintEll(Body, V(0, 0.38f, 0.1f), V(0.12f, 0.17f, 0.08f), cream);
        int tail = b.Tail(V(0, 0.3f, -0.12f));
        var path = Smooth(3, V(0, 0.32f, -0.1f), V(0, 0.22f, -0.28f), V(0.03f, 0.1f, -0.42f), V(0.08f, 0.06f, -0.5f));
        b.Tube(tail, path, 0.075f, 0.03f, gray, blend: 0f);
        for (int i = 2; i < path.Length - 1; i += 2) b.PaintTorus(tail, path[i], 0.075f - 0.045f * i / path.Length, 0.008f, pale, Euler(path[i + 1] - path[i - 1]));
        foreach (var (y, z) in new[] { (0.6f, -0.07f), (0.5f, -0.12f), (0.38f, -0.14f) })
            Blade(b, Body, V(0, y, z), V(0, y + 0.06f, z - 0.08f), 0.045f, pale, V(1f, 0, 0), 0.35f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.52f, 0.03f));
            b.Limb(arm, V(0.15f * s, 0.53f, 0.03f), V(0.25f * s, 0.44f, 0.08f), 0.058f, 0.048f, gray);
            b.Ell(arm, V(0.27f * s, 0.42f, 0.1f), V(0.048f, 0.048f, 0.048f), gray);
            Claws(b, arm, V(0.28f * s, 0.39f, 0.13f), V(0.022f * s, 0.008f, -0.01f), V(0.2f * s, -0.4f, 1f), 0.03f, 0.012f);
        });
        // The head: a horn like a drill on its nose, a crest of points behind, an open mouth
        int head = b.Head(V(0, 0.62f, 0.05f));
        var c = V(0, 0.68f, 0.09f);
        var r = V(0.112f, 0.09f, 0.12f);
        b.Ell(head, c, r, gray);
        b.Ell(head, V(0, 0.645f, 0.19f), V(0.078f, 0.06f, 0.08f), gray, blend: 0.03f);
        Grin(b, head, V(0, 0.615f, 0.235f), V(0.055f, 0.024f, 0.03f), Rgb(220, 120, 130));
        Drill(b, head, V(0, 0.69f, 0.22f), V(0, 0.81f, 0.32f), 0.048f, cream, Rgb(150, 140, 120));
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.075f * s, 0.73f, 0.04f), V(0.13f * s, 0.84f, -0.03f), 0.037f, pale, 0.6f);
            b.Spike(head, V(0.055f * s, 0.67f, -0.01f), V(0.11f * s, 0.68f, -0.1f), 0.032f, pale, 0.6f);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.07f * s, 0.7f), V(0.8f * s, 0.15f, 0.6f), 0.017f, Rgb(220, 40, 50), glare: true));
        return b;
    }

    private static PokeBuilder Rhyperior()
    {
        var b = new PokeBuilder("Rhyperior", 1f, BodyPlan.Biped, V(0, 0.46f, 0)) { Coat = Shell };
        var brown = Rgb(116, 100, 98);
        var orange = Rgb(238, 118, 38);
        var horn = Rgb(214, 216, 222);
        var claw = Rgb(232, 232, 236);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.13f * s, 0.24f, 0.0f));
            b.Ell(leg, V(0.14f * s, 0.2f, 0.0f), V(0.1f, 0.12f, 0.11f), brown);
            b.Box(leg, V(0.21f * s, 0.23f, 0.04f), V(0.05f, 0.06f, 0.018f), 0.012f, orange, Toward(V(0.9f * s, 0, 0.4f)), blend: 0.006f);
            b.Limb(leg, V(0.14f * s, 0.12f, 0.0f), V(0.15f * s, 0.05f, 0.02f), 0.07f, 0.065f, brown);
            Claws(b, leg, V(0.15f * s, 0.02f, 0.08f), V(0.03f, 0, 0), V(0, -0.1f, 1f), 0.028f, 0.014f);
        });
        // A bulk of a body belted with flat orange slabs of rock, a great slab standing up on its back, a club of a tail
        var c = V(0, 0.46f, 0);
        var r = V(0.24f, 0.26f, 0.19f);
        b.Ell(Body, c, r, brown);
        foreach (var (y, count, phase) in new[] { (0.36f, 12, 0.5f), (0.46f, 10, 0f) })
        {
            float k = MathF.Sqrt(1f - (y - c.Y) * (y - c.Y) / (r.Y * r.Y));
            for (int i = 0; i < count; i++)
            {
                float a = (i + phase) * MathF.Tau / count;
                if (y > 0.4f && MathF.Cos(a) < 0.2f) continue;
                var n = V(MathF.Sin(a), 0, MathF.Cos(a));
                b.Box(Body, V(MathF.Sin(a) * r.X * k, y, MathF.Cos(a) * r.Z * k), V(0.058f, 0.045f, 0.018f), 0.01f, orange, Toward(n + V(0, 0.15f, 0)), blend: 0.005f);
            }
        }
        Blade(b, Body, V(0.06f, 0.56f, -0.14f), V(0.14f, 0.9f, -0.22f), 0.13f, brown, V(1f, 0, 0.3f), 0.35f);
        int tail = b.Tail(V(0, 0.3f, -0.15f));
        b.Limb(tail, V(0, 0.3f, -0.14f), V(0.04f, 0.2f, -0.32f), 0.09f, 0.075f, brown);
        b.Ell(tail, V(0.05f, 0.18f, -0.38f), V(0.11f, 0.1f, 0.11f), brown);
        // Arms with orange slabs on the shoulders, three claws round a hole in each palm
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.22f * s, 0.6f, 0.03f));
            b.Box(arm, V(0.25f * s, 0.67f, 0.02f), V(0.075f, 0.022f, 0.07f), 0.014f, orange, V(0, 0, -22f * s), blend: 0.006f);
            b.Limb(arm, V(0.24f * s, 0.6f, 0.04f), V(0.36f * s, 0.5f, 0.14f), 0.075f, 0.065f, brown);
            var palm = V(0.39f * s, 0.49f, 0.2f);
            b.Ell(arm, palm, V(0.07f, 0.065f, 0.06f), brown);
            b.Cut(arm, palm + V(0, 0, 0.06f), V(0.028f, 0.028f, 0.03f), blend: 0.006f);
            b.PaintEll(arm, palm + V(0, 0, 0.045f), V(0.035f, 0.035f, 0.03f), Rgb(30, 26, 26));
            foreach (var d in new[] { V(0.04f * s, 0.04f, 0), V(0.045f * s, -0.035f, 0), V(-0.035f * s, -0.045f, 0) })
                b.Spike(arm, palm + d * 1.2f + V(0, 0, 0.02f), palm + d * 1.6f + V(0, 0, 0.07f), 0.02f, claw, mat: Shell, blend: 0.005f);
        });
        // The head sunk between the shoulders: a drill on its nose, a horn above it, red eyes and a jaw of white teeth
        int head = b.Head(V(0, 0.66f, 0.1f));
        var hc = V(0, 0.7f, 0.14f);
        var hr = V(0.11f, 0.09f, 0.11f);
        b.Ell(head, hc, hr, brown);
        b.Ell(head, V(0, 0.66f, 0.23f), V(0.08f, 0.06f, 0.07f), brown, blend: 0.03f);
        b.PaintEll(head, V(0, 0.625f, 0.24f), V(0.07f, 0.03f, 0.06f), Rgb(196, 192, 196));
        foreach (float x in new[] { -0.04f, 0f, 0.04f })
            b.Spike(head, V(x, 0.635f, 0.29f), V(x, 0.615f, 0.3f), 0.01f, White, mat: Shell, blend: 0.003f);
        Drill(b, head, V(0, 0.71f, 0.25f), V(0, 0.82f, 0.39f), 0.05f, horn, Rgb(150, 150, 160));
        b.Spike(head, V(0, 0.78f, 0.16f), V(0, 0.88f, 0.17f), 0.03f, horn, mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.07f * s, 0.725f), V(0.8f * s, 0.15f, 0.6f), 0.017f, Rgb(220, 40, 50), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Duskull line

    private static PokeBuilder Duskull()
    {
        var b = new PokeBuilder("Duskull", 0.62f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Fur }.Hover();
        var cloak = Rgb(88, 88, 96);
        var bone = Rgb(238, 232, 208);
        var patch = Rgb(150, 150, 156);

        // A hood of dark cloak round a skull, a wisp rising off the top and a tail of cloak trailing below
        b.Ell(Body, V(0, 0.4f, -0.03f), V(0.15f, 0.15f, 0.15f), cloak);
        foreach (var (x, y, z) in new[] { (0.08f, 0.48f, -0.1f), (-0.1f, 0.42f, -0.11f), (0.02f, 0.36f, -0.16f) })
            b.PaintEll(Body, V(x, y, z), V(0.05f, 0.04f, 0.04f), patch);
        int tail = b.Tail(V(0, 0.3f, -0.05f));
        b.Tube(tail, Smooth(3, V(0, 0.32f, -0.04f), V(0.02f, 0.22f, -0.08f), V(0.06f, 0.16f, -0.14f), V(0.1f, 0.15f, -0.2f)), 0.08f, 0.012f, cloak, blend: 0f);
        int head = b.Head(V(0, 0.5f, 0));
        b.Tube(head, Smooth(3, V(0, 0.53f, -0.03f), V(0.03f, 0.6f, -0.05f), V(-0.02f, 0.65f, -0.07f), V(0.03f, 0.7f, -0.06f)), 0.04f, 0.01f, cloak, blend: 0f);
        var c = V(0, 0.4f, 0.06f);
        var r = V(0.115f, 0.12f, 0.1f);
        b.Ell(Body, c, r, bone, blend: 0.015f);
        b.PaintEll(Body, V(0, 0.33f, 0.14f), V(0.06f, 0.02f, 0.03f), Rgb(120, 116, 104));
        b.Eye(Body, On(c, r, 0, 0.42f), V(0, 0.08f, 1f), 0.05f, sclera: true, pupil: Rgb(176, 24, 56), white: Rgb(236, 110, 146));
        return Lift(b);
    }

    private static PokeBuilder Dusclops()
    {
        var b = new PokeBuilder("Dusclops", 0.82f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Fur };
        var gray = Rgb(142, 144, 152);
        var band = Rgb(106, 108, 118);
        var cloth = Rgb(222, 216, 206);

        // Short legs wrapped in bands
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.075f * s, 0.13f, 0.0f));
            b.Limb(leg, V(0.075f * s, 0.15f, 0.0f), V(0.08f * s, 0.045f, 0.01f), 0.066f, 0.06f, gray);
            b.PaintTorus(leg, V(0.08f * s, 0.08f, 0.005f), 0.064f, 0.006f, band);
        });
        // A body wrapped like a mummy, bands round it, a fold like a collar, one great eye
        var c = V(0, 0.38f, 0);
        var r = V(0.17f, 0.24f, 0.16f);
        b.Ell(Body, c, r, gray);
        foreach (float y in new[] { 0.2f, 0.25f, 0.49f, 0.54f })
        {
            float k = MathF.Sqrt(MathF.Max(0f, 1f - (y - c.Y) * (y - c.Y) / (r.Y * r.Y)));
            b.PaintTorus(Body, V(0, y, 0), r.X * k, 0.006f, band, sz: r.Z / r.X);
        }
        b.Torus(Body, V(0, 0.32f, 0), 0.165f, 0.026f, gray, sz: 0.95f, blend: 0.01f);
        foreach (float a in new[] { -0.9f, -0.3f, 0.3f, 0.9f })
            b.Spike(Body, V(MathF.Sin(a) * 0.18f, 0.33f, MathF.Cos(a) * 0.17f), V(MathF.Sin(a) * 0.2f, 0.27f, MathF.Cos(a) * 0.19f), 0.03f, gray, 0.5f, blend: 0.01f);
        int head = b.Head(V(0, 0.58f, 0));
        b.Ell(head, V(0, 0.61f, -0.01f), V(0.07f, 0.04f, 0.07f), cloth, blend: 0.03f);
        b.Tube(head, Smooth(3, V(0, 0.64f, -0.01f), V(0.04f, 0.68f, 0.0f), V(0.1f, 0.67f, -0.02f), V(0.15f, 0.71f, -0.03f)), 0.03f, 0.01f, cloth, blend: 0f);
        // Big bandaged hands, and a cloth streaming behind it like a tail
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.38f, 0.05f));
            b.Limb(arm, V(0.15f * s, 0.39f, 0.05f), V(0.24f * s, 0.32f, 0.12f), 0.045f, 0.04f, gray);
            var palm = V(0.27f * s, 0.29f, 0.15f);
            b.Ell(arm, palm, V(0.06f, 0.05f, 0.055f), cloth);
            for (int i = 0; i < 4; i++)
                b.Limb(arm, palm + V(0.02f * s, -0.01f, -0.03f + 0.022f * i), palm + V(0.06f * s, -0.06f, -0.03f + 0.026f * i), 0.016f, 0.014f, cloth, blend: 0.006f);
        });
        int tail = b.Tail(V(0.06f, 0.44f, -0.12f));
        Blade(b, tail, V(0.06f, 0.46f, -0.12f), V(0.26f, 0.16f, -0.34f), 0.09f, cloth, V(1f, 0.3f, -0.4f), 0.2f);
        b.Eye(Body, On(c, r, 0, 0.46f), V(0, 0.05f, 1f), 0.055f, sclera: true, pupil: Rgb(236, 92, 150));
        return b;
    }

    private static PokeBuilder Dusknoir()
    {
        var b = new PokeBuilder("Dusknoir", 0.96f, BodyPlan.Floating, V(0, 0.48f, 0)) { Coat = Fur }.Hover();
        var gray = Rgb(88, 90, 84);
        var pale = Rgb(196, 206, 168);
        var yellow = Rgb(246, 210, 50);
        var cuff = Rgb(58, 46, 42);

        // A round body tapering into a ghost's tail, a face on its belly: one eye and a zigzag mouth
        var c = V(0, 0.48f, 0);
        var r = V(0.2f, 0.2f, 0.16f);
        b.Ell(Body, c, r, gray);
        int tail = b.Tail(V(0, 0.32f, 0));
        b.Tube(tail, Smooth(3, V(0, 0.36f, 0.0f), V(0.0f, 0.22f, -0.03f), V(0.04f, 0.12f, -0.08f), V(0.09f, 0.08f, -0.14f)), 0.13f, 0.015f, gray, blend: 0f);
        b.Mark(Body, On(c, r, 0, 0.48f), V(0, 0.05f, 1f), 0.032f, 0.022f, yellow, MarkShape.Diamond);
        b.Mark(Body, On(c, r, 0, 0.39f), V(0, -0.35f, 1f), 0.11f, 0.03f, yellow, MarkShape.Zigzag);
        // Pale jagged flaps rising round its head, a crown with a yellow band and a yellow aerial
        PokeBuilder.Both(s =>
        {
            Blade(b, Body, V(0.1f * s, 0.6f, 0.03f), V(0.22f * s, 0.8f, 0.0f), 0.08f, pale, V(0.3f * s, 0.2f, 1f), 0.3f);
            Blade(b, Body, V(0.08f * s, 0.6f, -0.04f), V(0.17f * s, 0.76f, -0.1f), 0.07f, pale, V(0.5f * s, 0.2f, -1f), 0.3f);
        });
        Blade(b, Body, V(0, 0.6f, 0.08f), V(0, 0.68f, 0.17f), 0.08f, pale, V(0, 0.4f, 1f), 0.3f);
        int head = b.Head(V(0, 0.62f, 0));
        var hc = V(0, 0.69f, 0.0f);
        var hr = V(0.075f, 0.09f, 0.07f);
        b.Ell(head, hc, hr, pale);
        b.PaintEll(head, V(0, 0.745f, 0.0f), V(0.09f, 0.014f, 0.09f), yellow);
        b.Limb(head, V(0, 0.76f, 0), V(0, 0.83f, 0), 0.02f, 0.02f, yellow, Shell);
        b.Ell(head, V(0, 0.84f, 0), V(0.05f, 0.015f, 0.05f), yellow, mat: Shell, blend: 0.006f);
        b.PaintEll(head, V(0, 0.66f, 0.06f), V(0.05f, 0.035f, 0.03f), Rgb(26, 24, 26));
        b.Eye(head, On(hc, hr, 0, 0.705f), V(0, 0, 1f), 0.022f, Rgb(232, 50, 50), glare: true);
        // Arms in dark sleeves banded with yellow, great pale hands with long fingers
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.18f * s, 0.55f, 0.02f));
            b.Limb(arm, V(0.18f * s, 0.56f, 0.02f), V(0.33f * s, 0.48f, 0.08f), 0.06f, 0.05f, gray);
            foreach (float t in new[] { 0.35f, 0.55f })
                b.PaintTorus(arm, Vector3.Lerp(V(0.18f * s, 0.56f, 0.02f), V(0.33f * s, 0.48f, 0.08f), t), 0.056f, 0.01f, yellow, Euler(V(0.15f * s, -0.08f, 0.06f)));
            b.Torus(arm, V(0.34f * s, 0.475f, 0.085f), 0.045f, 0.02f, cuff, Euler(V(0.15f * s, -0.08f, 0.06f)), blend: 0.006f);
            var palm = V(0.4f * s, 0.43f, 0.12f);
            b.Ell(arm, palm, V(0.06f, 0.05f, 0.06f), pale);
            for (int i = 0; i < 3; i++)
                b.Tube(arm, Smooth(2, palm + V(0.02f * s, -0.02f, -0.03f + 0.03f * i), palm + V(0.07f * s, -0.07f, -0.03f + 0.04f * i), palm + V(0.07f * s, -0.12f, 0.0f + 0.04f * i)), 0.016f, 0.01f, pale, blend: 0.004f);
            b.Tube(arm, Smooth(2, palm + V(-0.01f * s, -0.02f, 0.04f), palm + V(0.0f, -0.06f, 0.09f), palm + V(0.02f * s, -0.09f, 0.12f)), 0.016f, 0.01f, pale, blend: 0.004f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Porygon line

    private static PokeBuilder Porygon()
    {
        var b = new PokeBuilder("Porygon", 0.62f, BodyPlan.Biped, V(0, 0.24f, -0.02f)) { Coat = Shell };
        var pink = Rgb(240, 98, 116);
        var blue = Rgb(118, 212, 222);

        // Made of flat faces: blocks with hardly a rounded edge, joined without blending
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.14f, 0.06f));
            b.Box(leg, V(0.085f * s, 0.07f, 0.1f), V(0.045f, 0.07f, 0.065f), 0.006f, blue, V(-12f, 0, 0));
        });
        b.Box(Body, V(0, 0.24f, -0.02f), V(0.11f, 0.1f, 0.12f), 0.008f, pink, V(12f, 0, 0));
        PokeBuilder.Both(s => b.Box(Body, V(0.1f * s, 0.25f, 0.0f), V(0.04f, 0.075f, 0.09f), 0.006f, pink, V(10f, 0, 18f * s)));
        int tail = b.Tail(V(0, 0.28f, -0.13f));
        b.Box(tail, V(0, 0.36f, -0.17f), V(0.035f, 0.13f, 0.055f), 0.006f, blue, V(-22f, 0, 0));
        int head = b.Head(V(0, 0.32f, 0.06f));
        b.Box(head, V(0, 0.41f, 0.07f), V(0.1f, 0.09f, 0.1f), 0.008f, pink, V(-8f, 0, 0));
        b.Box(head, V(0, 0.48f, 0.04f), V(0.075f, 0.04f, 0.08f), 0.006f, pink, V(-20f, 0, 0));
        PokeBuilder.Both(s => b.Box(head, V(0.062f * s, 0.4f, 0.15f), V(0.04f, 0.075f, 0.05f), 0.006f, pink, V(-8f, 32f * s, 0)));
        b.Box(head, V(0, 0.345f, 0.2f), V(0.07f, 0.042f, 0.1f), 0.006f, blue, V(30f, 0, 0));
        PokeBuilder.Both(s => b.Box(head, V(0.045f * s, 0.355f, 0.2f), V(0.03f, 0.04f, 0.09f), 0.006f, blue, V(30f, 25f * s, 0)));
        PokeBuilder.Both(s => b.Eye(head, V(0.1f * s, 0.425f, 0.06f), V(s, 0.1f, 0.1f), 0.032f, sclera: true, pupil: Rgb(30, 30, 40)));
        return b;
    }

    private static PokeBuilder Porygon2()
    {
        var b = new PokeBuilder("Porygon2", 0.66f, BodyPlan.Biped, V(0, 0.2f, -0.02f)) { Coat = Shell };
        var magenta = Rgb(232, 70, 122);
        var blue = Rgb(92, 180, 236);

        // Smooth now: a round body on two big blue feet, blue fins for arms, a blue tail
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.1f, 0.05f));
            b.Ell(leg, V(0.085f * s, 0.06f, 0.07f), V(0.075f, 0.06f, 0.1f), blue);
        });
        b.Ell(Body, V(0, 0.2f, -0.02f), V(0.13f, 0.12f, 0.15f), magenta);
        int tail = b.Tail(V(0, 0.16f, -0.15f));
        b.Ell(tail, V(0, 0.15f, -0.19f), V(0.07f, 0.055f, 0.07f), blue);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.25f, -0.04f));
            b.Ell(arm, V(0.15f * s, 0.34f, -0.07f), V(0.03f, 0.11f, 0.036f), blue, V(-20f, 0, -25f * s));
        });
        // A long neck to a round head with a long blue bill
        b.Limb(Body, V(0, 0.24f, 0.06f), V(0, 0.34f, 0.08f), 0.06f, 0.05f, magenta, blend: 0.03f);
        int head = b.Head(V(0, 0.33f, 0.07f));
        var c = V(0, 0.41f, 0.08f);
        var r = V(0.09f, 0.085f, 0.09f);
        b.Ell(head, c, r, magenta);
        b.Ell(head, V(0, 0.385f, 0.19f), V(0.045f, 0.034f, 0.085f), blue, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.06f * s, 0.43f);
            b.Eye(head, at, Outward(c, r, at), 0.028f, sclera: true, pupil: Rgb(30, 30, 40));
        });
        return b;
    }

    private static PokeBuilder PorygonZ()
    {
        var b = new PokeBuilder("Porygon-Z", 0.7f, BodyPlan.Floating, V(0, 0.38f, 0)) { Coat = Shell }.Hover();
        var magenta = Rgb(232, 64, 116);
        var blue = Rgb(92, 182, 236);
        var yellow = Rgb(250, 214, 40);

        // Its parts no longer hold together: a head, a body, two arms and a tail, each floating apart
        b.Ell(Body, V(0, 0.38f, 0), V(0.075f, 0.08f, 0.072f), magenta);
        b.PaintEll(Body, V(0, 0.32f, 0), V(0.09f, 0.055f, 0.09f), blue);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.44f, 0.02f));
            b.Ell(arm, V(0.17f * s, 0.47f, 0.02f), V(0.036f, 0.11f, 0.034f), blue, V(0, 0, -50f * s));
        });
        int tail = b.Tail(V(0, 0.27f, -0.02f));
        b.Ell(tail, V(0.03f, 0.18f, -0.02f), V(0.032f, 0.085f, 0.03f), blue, V(0, 0, 15f));
        int head = b.Head(V(0, 0.48f, 0.02f));
        var c = V(0, 0.56f, 0.03f);
        var r = V(0.085f, 0.072f, 0.085f);
        b.Ell(head, c, r, magenta);
        b.Ell(head, V(0, 0.535f, 0.135f), V(0.045f, 0.032f, 0.07f), blue, V(-10f, 0, 0), blend: 0.012f);
        b.Spike(head, V(0, 0.62f, -0.01f), V(0.03f, 0.73f, -0.05f), 0.022f, magenta, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.06f * s, 0.565f);
            b.Eye(head, at, Outward(c, r, at), 0.03f, sclera: true, white: yellow, pupil: Rgb(30, 26, 30));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Scyther line

    /// <summary>A curved blade from <paramref name="root"/> through <paramref name="mid"/> to a point at <paramref name="tip"/>, flat across <paramref name="facing"/>.</summary>
    private static void Sickle(PokeBuilder b, int bone, Vector3 root, Vector3 mid, Vector3 tip, float width, Color color, Vector3 facing)
    {
        Frond(b, bone, root, mid + (mid - root) * 0.12f, width, color, facing, 0.22f, Metal, 0.006f);
        Blade(b, bone, mid - (mid - root) * 0.12f, tip, width * 0.95f, color, facing, 0.22f, Metal);
    }

    private static PokeBuilder Scyther()
    {
        var b = new PokeBuilder("Scyther", 0.9f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Shell };
        var green = Rgb(112, 192, 88);
        var cream = Rgb(240, 228, 176);
        var steel = Rgb(226, 230, 238);
        var wing = Rgb(246, 238, 206);

        // Long thin legs bent at the knee, claws on the feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.3f, -0.03f));
            b.Limb(leg, V(0.07f * s, 0.32f, -0.03f), V(0.12f * s, 0.17f, 0.05f), 0.045f, 0.03f, green);
            b.Limb(leg, V(0.12f * s, 0.17f, 0.05f), V(0.1f * s, 0.04f, 0.0f), 0.03f, 0.024f, green);
            Claws(b, leg, V(0.1f * s, 0.02f, 0.03f), V(0.02f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.012f);
            b.Spike(leg, V(0.1f * s, 0.03f, -0.02f), V(0.1f * s, 0.01f, -0.07f), 0.012f, Claw, mat: Shell, blend: 0.004f);
        });
        // A cream-banded abdomen, a green thorax with a cream chest
        b.Ell(Body, V(0, 0.36f, -0.05f), V(0.075f, 0.075f, 0.095f), green);
        foreach (float y in new[] { 0.33f, 0.37f }) b.PaintEll(Body, V(0, y, 0.0f), V(0.07f, 0.012f, 0.08f), cream);
        b.Ell(Body, V(0, 0.48f, 0.02f), V(0.08f, 0.1f, 0.075f), green);
        b.PaintEll(Body, V(0, 0.47f, 0.08f), V(0.055f, 0.07f, 0.03f), cream);
        // Two pairs of pale wings on its back
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.04f * s, 0.52f, -0.05f));
            RimmedFin(b, w, V(0.11f * s, 0.6f, -0.055f), V(0.045f, 0.14f, 0.006f), V(-15f, 0, -40f * s), wing, Rgb(220, 206, 150), 0.01f);
            RimmedFin(b, w, V(0.1f * s, 0.5f, -0.065f), V(0.04f, 0.12f, 0.006f), V(-15f, 0, -65f * s), wing, Rgb(220, 206, 150), 0.01f);
        });
        // Arms that end in great scythes
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.54f, 0.04f));
            b.Limb(arm, V(0.07f * s, 0.55f, 0.04f), V(0.15f * s, 0.48f, 0.09f), 0.03f, 0.026f, green);
            b.Limb(arm, V(0.15f * s, 0.48f, 0.09f), V(0.2f * s, 0.58f, 0.15f), 0.026f, 0.03f, green);
            Sickle(b, arm, V(0.2f * s, 0.6f, 0.15f), V(0.26f * s, 0.5f, 0.24f), V(0.25f * s, 0.32f, 0.22f), 0.045f, steel, V(1f, 0, -0.2f * s));
        });
        // A head with a long crest swept back, narrow eyes
        int head = b.Head(V(0, 0.58f, 0.04f));
        var c = V(0, 0.64f, 0.07f);
        var r = V(0.065f, 0.06f, 0.07f);
        b.Ell(head, c, r, green);
        b.Ell(head, V(0, 0.615f, 0.13f), V(0.04f, 0.035f, 0.04f), green, blend: 0.02f);
        Blade(b, head, V(0, 0.68f, 0.06f), V(0, 0.8f, -0.06f), 0.035f, green, V(1f, 0, 0), 0.4f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.035f * s, 0.65f), V(0.5f * s, 0.1f, 1f), 0.015f, glare: true));
        return b;
    }

    private static PokeBuilder Scizor()
    {
        var b = new PokeBuilder("Scizor", 0.92f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Metal };
        var red = Rgb(214, 40, 48);
        var dark = Rgb(72, 72, 82);
        var wing = Rgb(232, 236, 242);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.3f, -0.02f));
            b.Limb(leg, V(0.07f * s, 0.32f, -0.02f), V(0.11f * s, 0.17f, 0.04f), 0.04f, 0.028f, red);
            b.Limb(leg, V(0.11f * s, 0.17f, 0.04f), V(0.1f * s, 0.04f, 0.0f), 0.028f, 0.024f, red);
            b.Spike(leg, V(0.105f * s, 0.12f, 0.02f), V(0.12f * s, 0.12f, -0.05f), 0.014f, red, blend: 0.004f);
            Claws(b, leg, V(0.1f * s, 0.02f, 0.03f), V(0.02f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.012f);
        });
        // A dark segmented waist between a red abdomen and a red thorax
        b.Ell(Body, V(0, 0.36f, -0.05f), V(0.07f, 0.075f, 0.09f), dark);
        b.Ell(Body, V(0, 0.43f, 0.0f), V(0.05f, 0.04f, 0.05f), dark);
        b.Ell(Body, V(0, 0.51f, 0.02f), V(0.085f, 0.09f, 0.075f), red);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.04f * s, 0.54f, -0.05f));
            RimmedFin(b, w, V(0.09f * s, 0.6f, -0.055f), V(0.035f, 0.1f, 0.006f), V(-15f, 0, -45f * s), wing, Rgb(196, 200, 212), 0.01f);
        });
        // Arms ending in great round pincers with eye-like spots, to frighten
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.56f, 0.03f));
            b.Limb(arm, V(0.08f * s, 0.57f, 0.03f), V(0.17f * s, 0.49f, 0.07f), 0.032f, 0.028f, red);
            b.Limb(arm, V(0.17f * s, 0.49f, 0.07f), V(0.23f * s, 0.6f, 0.12f), 0.028f, 0.034f, red);
            var claw = V(0.26f * s, 0.67f, 0.15f);
            b.Ell(arm, claw, V(0.075f, 0.075f, 0.085f), red);
            b.Cut(arm, claw + V(0.0f, -0.005f, 0.07f), V(0.09f, 0.016f, 0.055f), blend: 0.008f);
            b.PaintTorus(arm, claw + V(0, 0, -0.045f), 0.066f, 0.013f, dark, V(90f, 0, 0));
            var spot = claw + V(0.06f * s, 0.035f, 0.02f);
            b.Mark(arm, spot, V(0.9f * s, 0.4f, 0.2f), 0.017f, 0.017f, dark);
        });
        int head = b.Head(V(0, 0.6f, 0.04f));
        var c = V(0, 0.66f, 0.06f);
        var r = V(0.06f, 0.06f, 0.065f);
        b.Ell(head, c, r, red);
        b.Spike(head, V(0, 0.7f, 0.06f), V(0, 0.8f, 0.02f), 0.026f, red, 0.5f);
        PokeBuilder.Both(s => b.Spike(head, V(0.035f * s, 0.69f, 0.05f), V(0.09f * s, 0.77f, 0.0f), 0.022f, red, 0.5f));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.03f * s, 0.67f), V(0.5f * s, 0.1f, 1f), 0.015f, Rgb(250, 214, 40), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Elekid line

    private static PokeBuilder Elekid()
    {
        var b = new PokeBuilder("Elekid", 0.56f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Fur };
        var yellow = Rgb(250, 212, 44);
        var black = Rgb(44, 40, 44);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.08f, 0.0f));
            b.Limb(leg, V(0.05f * s, 0.1f, 0.0f), V(0.055f * s, 0.03f, 0.02f), 0.04f, 0.035f, yellow);
            b.Ell(leg, V(0.055f * s, 0.025f, 0.035f), V(0.04f, 0.025f, 0.05f), yellow);
        });
        var c = V(0, 0.18f, 0);
        var r = V(0.1f, 0.1f, 0.09f);
        b.Ell(Body, c, r, yellow);
        b.Mark(Body, On(c, r, 0, 0.17f), V(0, 0, 1f), 0.05f, 0.022f, black, MarkShape.Zigzag, 90f);
        // Round arms striped black at the end
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.22f, 0.02f));
            b.Limb(arm, V(0.09f * s, 0.22f, 0.02f), V(0.2f * s, 0.2f, 0.04f), 0.032f, 0.036f, yellow);
            b.Ell(arm, V(0.22f * s, 0.2f, 0.045f), V(0.045f, 0.045f, 0.045f), yellow);
            foreach (float x in new[] { 0.16f, 0.21f }) b.PaintEll(arm, V(x * s, 0.2f, 0.04f), V(0.011f, 0.06f, 0.06f), black);
        });
        // A head like a plug, two prongs on top and a black V on the brow
        int head = b.Head(V(0, 0.27f, 0.0f));
        var hc = V(0, 0.36f, 0.01f);
        var hr = V(0.1f, 0.09f, 0.09f);
        b.Ell(head, hc, hr, yellow);
        PokeBuilder.Both(s =>
        {
            b.Limb(head, V(0.045f * s, 0.42f, 0.0f), V(0.05f * s, 0.54f, -0.01f), 0.022f, 0.022f, yellow, blend: 0.01f);
            b.PaintEll(head, V(0.05f * s, 0.5f, -0.01f), V(0.03f, 0.011f, 0.03f), black);
            var at = On(hc, hr, 0.035f * s, 0.41f);
            b.Mark(head, at, Outward(hc, hr, at), 0.035f, 0.009f, black, MarkShape.Bar, -35f * s);
        });
        b.Mark(head, On(hc, hr, 0, 0.32f), V(0, -0.2f, 1f), 0.02f, 0.008f, Rgb(120, 60, 50), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.04f * s, 0.365f), V(0.4f * s, 0.05f, 1f), 0.018f, glare: true));
        return b;
    }

    /// <summary>Black stripes across a body: thin paint bands round it at the given heights, tilted alternately.</summary>
    private static void Stripes(PokeBuilder b, int bone, Vector3 center, Vector3 radii, float[] heights, Color color, float tilt)
    {
        for (int i = 0; i < heights.Length; i++)
        {
            float y = heights[i];
            float k = MathF.Sqrt(MathF.Max(0.05f, 1f - (y - center.Y) * (y - center.Y) / (radii.Y * radii.Y)));
            b.PaintEll(bone, V(center.X, y, center.Z), V(radii.X * k * 1.08f, 0.013f, radii.Z * k * 1.08f), color, V(0, 0, (i % 2 == 0 ? 1 : -1) * tilt));
        }
    }

    /// <summary>
    /// A tiger's stripe painted across a body: a thin band laid along the surface round <paramref name="a"/>
    /// (radians from the front) at height <paramref name="y"/>, <paramref name="length"/> long, tilted by <paramref name="tilt"/> degrees.
    /// </summary>
    private static void Tiger(PokeBuilder b, int bone, Vector3 center, Vector3 radii, float a, float y, float length, float tilt, Color color, float width = 0.013f)
    {
        float k = MathF.Sqrt(MathF.Max(0.05f, 1f - (y - center.Y) * (y - center.Y) / (radii.Y * radii.Y)));
        var at = center + V(MathF.Sin(a) * radii.X * k, y - center.Y, MathF.Cos(a) * radii.Z * k);
        b.PaintEll(bone, at, V(length, width, 0.04f), color, V(0, a / Degree, tilt));
    }

    private static PokeBuilder Electabuzz()
    {
        var b = new PokeBuilder("Electabuzz", 0.88f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var yellow = Rgb(250, 212, 44);
        var black = Rgb(44, 40, 44);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.26f, 0.0f));
            b.Ell(leg, V(0.1f * s, 0.21f, 0.0f), V(0.075f, 0.1f, 0.08f), yellow);
            b.Limb(leg, V(0.1f * s, 0.14f, 0.0f), V(0.11f * s, 0.04f, 0.02f), 0.05f, 0.042f, yellow);
            b.Ell(leg, V(0.11f * s, 0.03f, 0.05f), V(0.05f, 0.03f, 0.07f), yellow);
            Claws(b, leg, V(0.11f * s, 0.02f, 0.11f), V(0.02f, 0, 0), V(0, -0.2f, 1f), 0.025f, 0.011f);
            Stripes(b, leg, V(0.1f * s, 0.18f, 0.0f), V(0.075f, 0.12f, 0.08f), new[] { 0.14f, 0.21f }, black, 0f);
        });
        var c = V(0, 0.42f, 0);
        var r = V(0.14f, 0.17f, 0.12f);
        b.Ell(Body, c, r, yellow);
        foreach (var (y, len) in new[] { (0.35f, 0.07f), (0.43f, 0.08f), (0.51f, 0.07f) })
            PokeBuilder.Both(s =>
            {
                Tiger(b, Body, c, r, 0.62f * s, y, len, 28f * s, black);
                Tiger(b, Body, c, r, MathF.PI - 0.62f * s, y, len, 28f * s, black);
                Tiger(b, Body, c, r, MathF.PI / 2f * s, y + 0.02f, 0.05f, 0f, black);
            });
        int tail = b.Tail(V(0, 0.32f, -0.1f));
        Striped(b, tail, Smooth(3, V(0, 0.32f, -0.1f), V(0.02f, 0.2f, -0.2f), V(0.08f, 0.12f, -0.28f), V(0.16f, 0.1f, -0.3f)), 0.035f, 0.02f, yellow, black, 4);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.52f, 0.02f));
            b.Limb(arm, V(0.13f * s, 0.53f, 0.02f), V(0.24f * s, 0.46f, 0.07f), 0.05f, 0.042f, yellow);
            b.Ell(arm, V(0.27f * s, 0.44f, 0.09f), V(0.05f, 0.05f, 0.05f), yellow);
            foreach (float t in new[] { 0.45f, 0.75f })
                b.PaintEll(arm, Vector3.Lerp(V(0.13f * s, 0.53f, 0.02f), V(0.24f * s, 0.46f, 0.07f), t), V(0.012f, 0.06f, 0.06f), black, V(0, 0, -30f * s));
        });
        // A cat's head with two coiled feelers, a black stripe down the brow, fangs
        int head = b.Head(V(0, 0.58f, 0.03f));
        var hc = V(0, 0.65f, 0.05f);
        var hr = V(0.09f, 0.08f, 0.08f);
        b.Ell(head, hc, hr, yellow);
        b.Ell(head, V(0, 0.62f, 0.11f), V(0.05f, 0.04f, 0.04f), yellow, blend: 0.02f);
        Gape(b, head, V(0, 0.6f, 0.14f), V(0.035f, 0.016f, 0.02f), Rgb(200, 80, 80), 0.012f);
        b.PaintEll(head, V(0, 0.71f, 0.1f), V(0.016f, 0.05f, 0.04f), black);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, V(0.03f * s, 0.72f, 0.03f), V(0.05f * s, 0.8f, 0.0f), V(0.1f * s, 0.82f, 0.0f), V(0.1f * s, 0.77f, 0.01f)), 0.012f, 0.008f, black, blend: 0.004f);
            var at = On(hc, hr, 0.055f * s, 0.7f);
            b.Mark(head, at, Outward(hc, hr, at), 0.03f, 0.008f, black, MarkShape.Bar, -30f * s);
        });
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.04f * s, 0.66f), V(0.4f * s, 0.05f, 1f), 0.016f, glare: true));
        return b;
    }

    private static PokeBuilder Electivire()
    {
        var b = new PokeBuilder("Electivire", 0.96f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var yellow = Rgb(250, 212, 44);
        var black = Rgb(48, 46, 50);
        var red = Rgb(232, 60, 50);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.22f, 0.0f));
            b.Ell(leg, V(0.12f * s, 0.18f, 0.0f), V(0.085f, 0.1f, 0.09f), yellow);
            b.Limb(leg, V(0.12f * s, 0.11f, 0.01f), V(0.13f * s, 0.04f, 0.02f), 0.06f, 0.055f, black);
            Claws(b, leg, V(0.13f * s, 0.02f, 0.08f), V(0.025f, 0, 0), V(0, -0.2f, 1f), 0.025f, 0.012f);
            Stripes(b, leg, V(0.12f * s, 0.18f, 0.0f), V(0.085f, 0.1f, 0.09f), new[] { 0.16f, 0.22f }, black, 0f);
        });
        // A heavy striped body, a shaggy ruff of yellow fur round the shoulders
        var c = V(0, 0.42f, 0);
        var r = V(0.19f, 0.2f, 0.15f);
        b.Ell(Body, c, r, yellow);
        foreach (var (y, len) in new[] { (0.3f, 0.09f), (0.38f, 0.1f), (0.46f, 0.09f) })
            PokeBuilder.Both(s =>
            {
                Tiger(b, Body, c, r, 0.75f * s, y, len, 24f * s, black, 0.016f);
                Tiger(b, Body, c, r, MathF.PI - 0.6f * s, y, len, 24f * s, black, 0.016f);
            });
        b.Mark(Body, On(c, r, 0, 0.4f), V(0, 0, 1f), 0.12f, 0.028f, black, MarkShape.Zigzag);
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(0, 0.56f, 0.01f) + n * V(0.15f, 0, 0.12f);
            b.Spike(Body, root, root + n * 0.07f + V(0, -0.07f, 0), 0.04f, yellow, 0.6f, blend: 0.02f);
        }
        // Two long black tails like cables, red at their ends
        PokeBuilder.Both(s =>
        {
            int tail = b.Part(s < 0 ? "cableL" : "cableR", Body, V(0.06f * s, 0.5f, -0.12f), PokeRole.Tail, 0.6f * s, s);
            var path = Smooth(3, V(0.06f * s, 0.5f, -0.12f), V(0.16f * s, 0.62f, -0.2f), V(0.3f * s, 0.72f, -0.16f), V(0.4f * s, 0.76f, -0.08f));
            b.Tube(tail, path, 0.022f, 0.016f, black, blend: 0f);
            b.Ell(tail, path[^1], V(0.022f, 0.022f, 0.022f), red, blend: 0.004f);
        });
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.5f, 0.02f));
            b.Limb(arm, V(0.17f * s, 0.51f, 0.02f), V(0.3f * s, 0.42f, 0.08f), 0.065f, 0.055f, yellow);
            foreach (float t in new[] { 0.45f, 0.75f })
                b.PaintEll(arm, Vector3.Lerp(V(0.17f * s, 0.51f, 0.02f), V(0.3f * s, 0.42f, 0.08f), t), V(0.013f, 0.075f, 0.075f), black, V(0, 0, -35f * s));
            var palm = V(0.34f * s, 0.39f, 0.11f);
            b.Ell(arm, palm, V(0.05f, 0.045f, 0.05f), black);
            Digits(b, arm, palm + V(0.02f * s, -0.02f, 0.02f), V(0.3f * s, -1f, 0.4f), V(0.5f * s, 0, -0.4f), 0.06f, 0.018f, black);
        });
        // A small head sunk in the ruff, two feelers tipped with balls, red eyes
        int head = b.Head(V(0, 0.58f, 0.06f));
        var hc = V(0, 0.63f, 0.09f);
        var hr = V(0.085f, 0.07f, 0.075f);
        b.Ell(head, hc, hr, yellow);
        b.PaintEll(head, V(0, 0.6f, 0.15f), V(0.05f, 0.012f, 0.03f), black);
        PokeBuilder.Both(s =>
        {
            b.Limb(head, V(0.04f * s, 0.69f, 0.06f), V(0.05f * s, 0.74f, 0.05f), 0.01f, 0.01f, black);
            b.Ell(head, V(0.05f * s, 0.75f, 0.05f), V(0.02f, 0.02f, 0.02f), yellow);
        });
        b.Mark(head, On(hc, hr, 0, 0.67f), V(0, 0.3f, 1f), 0.014f, 0.014f, black);
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.035f * s, 0.635f), V(0.4f * s, 0.05f, 1f), 0.015f, red, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Magby line

    private static PokeBuilder Magby()
    {
        var b = new PokeBuilder("Magby", 0.58f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var red = Rgb(240, 86, 82);
        var flame = Rgb(250, 118, 96);
        var yellow = Rgb(252, 222, 112);
        var black = Rgb(40, 34, 36);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.09f, 0.0f));
            b.Limb(leg, V(0.05f * s, 0.11f, 0.0f), V(0.055f * s, 0.03f, 0.02f), 0.04f, 0.035f, red);
            Claws(b, leg, V(0.055f * s, 0.015f, 0.055f), V(0.015f, 0, 0), V(0, -0.2f, 1f), 0.018f, 0.008f);
        });
        var c = V(0, 0.2f, 0);
        var r = V(0.09f, 0.1f, 0.085f);
        b.Ell(Body, c, r, red);
        b.PaintEll(Body, V(0, 0.17f, 0.06f), V(0.06f, 0.075f, 0.04f), yellow);
        int tail = b.Tail(V(0, 0.14f, -0.07f));
        b.Spike(tail, V(0, 0.14f, -0.06f), V(0, 0.1f, -0.16f), 0.03f, red);
        b.Torus(Body, V(0, 0.29f, 0.0f), 0.06f, 0.012f, black, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.24f, 0.02f));
            b.Limb(arm, V(0.08f * s, 0.24f, 0.02f), V(0.14f * s, 0.2f, 0.05f), 0.025f, 0.022f, red);
            Claws(b, arm, V(0.15f * s, 0.19f, 0.06f), V(0.0f, 0.012f, 0.0f), V(0.6f * s, -0.4f, 0.4f), 0.02f, 0.008f);
        });
        // A round head crowned with two great puffs of flame, a yellow beak of a mouth
        int head = b.Head(V(0, 0.3f, 0.01f));
        var hc = V(0, 0.37f, 0.02f);
        var hr = V(0.085f, 0.08f, 0.08f);
        b.Ell(head, hc, hr, red);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, V(0.042f * s, 0.455f, -0.01f), V(0.064f, 0.062f, 0.062f), flame, blend: 0.02f);
            b.PaintEll(head, V(0.045f * s, 0.495f, -0.005f), V(0.045f, 0.03f, 0.05f), Rgb(255, 150, 120));
        });
        b.Ell(head, V(0, 0.345f, 0.09f), V(0.045f, 0.026f, 0.04f), yellow, blend: 0.01f);
        b.Mark(head, V(0, 0.343f, 0.13f), V(0, 0, 1f), 0.025f, 0.004f, Rgb(170, 110, 60), MarkShape.Bar);
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.04f * s, 0.38f), V(0.4f * s, 0.05f, 1f), 0.017f));
        return b;
    }

    /// <summary>Flame patterns over a body: tongues of paint rising from below, side by side round it.</summary>
    private static void FlamePattern(PokeBuilder b, int bone, Vector3 center, Vector3 radii, float bottom, float top, int tongues, Color color, uint seed)
    {
        var rng = new GenomeRandom(seed);
        for (int i = 0; i < tongues; i++)
        {
            float a = (i + rng.Range(-0.2f, 0.2f)) * MathF.Tau / tongues;
            float height = rng.Range(0.55f, 1f) * (top - bottom);
            float y = bottom + height * 0.5f;
            float k = MathF.Sqrt(MathF.Max(0.1f, 1f - (y - center.Y) * (y - center.Y) / (radii.Y * radii.Y)));
            var at = center + V(MathF.Sin(a) * radii.X * k, y - center.Y, MathF.Cos(a) * radii.Z * k);
            b.PaintEll(bone, at, V(radii.X * 0.22f, height * 0.5f, radii.Z * 0.22f), color, V(0, a / Degree, rng.Range(-12f, 12f)));
        }
    }

    private static PokeBuilder Magmar()
    {
        var b = new PokeBuilder("Magmar", 0.9f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var red = Rgb(236, 52, 44);
        var yellow = Rgb(250, 214, 60);
        var black = Rgb(40, 34, 36);

        // Fat yellow thighs, red legs below them with white claws
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.24f, 0.0f));
            b.Ell(leg, V(0.11f * s, 0.21f, 0.0f), V(0.085f, 0.09f, 0.085f), yellow);
            b.Limb(leg, V(0.11f * s, 0.14f, 0.01f), V(0.12f * s, 0.04f, 0.02f), 0.045f, 0.04f, red);
            b.Torus(leg, V(0.115f * s, 0.13f, 0.01f), 0.042f, 0.01f, black, blend: 0.004f);
            Claws(b, leg, V(0.12f * s, 0.02f, 0.06f), V(0.02f, 0, 0), V(0, -0.2f, 1f), 0.028f, 0.011f);
        });
        // A body like a flame: red, yellow tongues rising up the front
        var c = V(0, 0.42f, 0);
        var r = V(0.14f, 0.17f, 0.12f);
        b.Ell(Body, c, r, red);
        b.PaintEll(Body, V(0, 0.33f, 0.07f), V(0.1f, 0.09f, 0.07f), yellow);
        FlamePattern(b, Body, c, r, 0.3f, 0.52f, 9, yellow, 126);
        int tail = b.Tail(V(0, 0.3f, -0.1f));
        var path = Smooth(3, V(0, 0.3f, -0.1f), V(0.0f, 0.18f, -0.2f), V(0.05f, 0.1f, -0.3f), V(0.12f, 0.12f, -0.38f));
        b.Tube(tail, path, 0.04f, 0.022f, red, blend: 0f);
        Blaze(b, tail, path[^1], V(0.6f, 0.8f, -0.2f), 0.14f, 0.045f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.52f, 0.02f));
            b.Limb(arm, V(0.13f * s, 0.53f, 0.02f), V(0.25f * s, 0.47f, 0.07f), 0.04f, 0.036f, red);
            foreach (float t in new[] { 0.35f, 0.65f })
            {
                var at = Vector3.Lerp(V(0.13f * s, 0.53f, 0.02f), V(0.25f * s, 0.47f, 0.07f), t);
                b.Spike(arm, at, at + V(0.03f * s, -0.07f, -0.02f), 0.022f, red, 0.5f);
            }
            Claws(b, arm, V(0.27f * s, 0.46f, 0.08f), V(0, 0.014f, -0.006f), V(0.8f * s, -0.3f, 0.3f), 0.03f, 0.01f);
        });
        // A head with a yellow duck's bill, two horns of flame, narrow eyes
        int head = b.Head(V(0, 0.58f, 0.03f));
        var hc = V(0, 0.65f, 0.05f);
        var hr = V(0.075f, 0.075f, 0.075f);
        b.Ell(head, hc, hr, red);
        b.Torus(Body, V(0, 0.57f, 0.02f), 0.06f, 0.012f, black, blend: 0.004f);
        b.Ell(head, V(0, 0.62f, 0.13f), V(0.05f, 0.026f, 0.05f), yellow, blend: 0.01f);
        b.Mark(head, V(0, 0.618f, 0.18f), V(0, 0, 1f), 0.03f, 0.004f, Rgb(170, 110, 60), MarkShape.Bar);
        PokeBuilder.Both(s => Blaze(b, head, V(0.04f * s, 0.71f, 0.03f), V(0.3f * s, 1f, -0.1f), 0.11f, 0.03f));
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.04f * s, 0.665f), V(0.4f * s, 0.05f, 1f), 0.016f, glare: true));
        return b;
    }

    private static PokeBuilder Magmortar()
    {
        var b = new PokeBuilder("Magmortar", 0.98f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var red = Rgb(232, 50, 44);
        var yellow = Rgb(250, 208, 56);
        var pink = Rgb(244, 178, 196);
        var dark = Rgb(70, 60, 64);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.2f, 0.0f));
            b.Ell(leg, V(0.11f * s, 0.17f, 0.0f), V(0.09f, 0.09f, 0.09f), pink);
            b.Limb(leg, V(0.12f * s, 0.1f, 0.01f), V(0.13f * s, 0.04f, 0.02f), 0.05f, 0.045f, dark);
            Claws(b, leg, V(0.13f * s, 0.02f, 0.07f), V(0.022f, 0, 0), V(0, -0.2f, 1f), 0.028f, 0.012f);
        });
        // A great round body, red with yellow flames, pink below, puffs of flame on the shoulders
        var c = V(0, 0.42f, 0);
        var r = V(0.2f, 0.2f, 0.16f);
        b.Ell(Body, c, r, red);
        b.PaintEll(Body, V(0, 0.24f, 0.0f), V(0.21f, 0.09f, 0.17f), pink);
        FlamePattern(b, Body, c, r, 0.28f, 0.56f, 11, yellow, 467);
        int tail = b.Tail(V(0, 0.32f, -0.13f));
        b.Limb(tail, V(0, 0.32f, -0.13f), V(0.02f, 0.2f, -0.26f), 0.05f, 0.04f, red);
        Blaze(b, tail, V(0.02f, 0.2f, -0.28f), V(0.2f, 0.3f, -1f), 0.16f, 0.05f);
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, V(0.17f * s, 0.6f, -0.02f), V(0.075f, 0.065f, 0.07f), red);
            Blaze(b, Body, V(0.18f * s, 0.64f, -0.03f), V(0.3f * s, 1f, -0.2f), 0.14f, 0.045f);
        });
        // Arms that are cannons: yellow barrels with a black bore and a rim of flame-coloured petals
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.18f * s, 0.52f, 0.02f));
            b.Limb(arm, V(0.18f * s, 0.53f, 0.02f), V(0.26f * s, 0.46f, 0.09f), 0.055f, 0.05f, red);
            var bore = V(0.31f * s, 0.43f, 0.19f);
            var along = Vector3.Normalize(bore - V(0.26f * s, 0.46f, 0.09f));
            b.Limb(arm, V(0.26f * s, 0.46f, 0.09f), bore, 0.055f, 0.058f, yellow);
            b.Cut(arm, bore + along * 0.055f, V(0.035f, 0.035f, 0.035f), blend: 0.004f);
            b.PaintEll(arm, bore + along * 0.035f, V(0.042f, 0.042f, 0.045f), Rgb(30, 26, 26));
            var u = Vector3.Normalize(Vector3.Cross(along, Vector3.UnitY));
            var w = Vector3.Cross(along, u);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f;
                var outward = u * MathF.Cos(a) + w * MathF.Sin(a);
                Blade(b, arm, bore - along * 0.02f + outward * 0.05f, bore + along * 0.02f + outward * 0.1f, 0.03f, yellow, along, 0.3f);
            }
        });
        // A head with pink fish-lips, a crest of flame, narrow eyes
        int head = b.Head(V(0, 0.6f, 0.05f));
        var hc = V(0, 0.66f, 0.07f);
        var hr = V(0.075f, 0.07f, 0.075f);
        b.Ell(head, hc, hr, red);
        b.Ell(head, V(0, 0.625f, 0.14f), V(0.045f, 0.024f, 0.035f), pink, blend: 0.01f);
        b.Mark(head, V(0, 0.625f, 0.177f), V(0, 0, 1f), 0.028f, 0.004f, Rgb(160, 80, 100), MarkShape.Bar);
        Blaze(b, head, V(0, 0.72f, 0.05f), V(0, 1f, -0.2f), 0.15f, 0.045f);
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.035f * s, 0.67f), V(0.4f * s, 0.05f, 1f), 0.015f, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Swinub line

    private static PokeBuilder Swinub()
    {
        var b = new PokeBuilder("Swinub", 0.54f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Fur };
        var brown = Rgb(178, 128, 86);
        var stripe = Rgb(110, 70, 46);
        var pink = Rgb(240, 172, 172);

        foreach (var (z, front) in new[] { (0.07f, true), (-0.08f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.08f * s, 0.06f, z), front);
                b.Ell(leg, V(0.085f * s, 0.025f, z), V(0.035f, 0.025f, 0.04f), stripe);
            });
        // A round pig of a body, its brown fur striped dark across the back, eyes shut under its fringe
        var c = V(0, 0.15f, 0);
        var r = V(0.16f, 0.13f, 0.17f);
        b.Ell(Body, c, r, brown);
        foreach (float z in new[] { 0.06f, -0.01f, -0.08f })
            b.PaintEll(Body, V(0, 0.27f, z), V(0.12f, 0.06f, 0.013f), stripe, V(-20f, 0, 0));
        int tail = b.Tail(V(0, 0.15f, -0.16f));
        b.Ell(tail, V(0, 0.15f, -0.17f), V(0.02f, 0.02f, 0.02f), brown);
        int head = b.Head(V(0, 0.16f, 0.12f));
        b.Ell(head, V(0, 0.11f, 0.17f), V(0.045f, 0.032f, 0.03f), pink, blend: 0.01f);
        PokeBuilder.Both(s => b.Mark(head, V(0.017f * s, 0.11f, 0.2f), V(0, 0, 1f), 0.007f, 0.01f, Rgb(120, 70, 70)));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.065f * s, 0.18f), V(0.45f * s, 0.1f, 1f), 0.018f, closed: true));
        return b;
    }

    /// <summary>Shaggy fur hanging round a body: strands of points pointing down from just under its widest.</summary>
    private static void Shag(PokeBuilder b, int bone, Vector3 center, Vector3 radii, float y, int strands, float length, float r, Color color, float skipFront = 2f)
    {
        float k = MathF.Sqrt(MathF.Max(0.05f, 1f - (y - center.Y) * (y - center.Y) / (radii.Y * radii.Y)));
        for (int i = 0; i < strands; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / strands;
            if (MathF.Cos(a) > skipFront) continue;
            var n = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(center.X + n.X * radii.X * k * 0.96f, y, center.Z + n.Z * radii.Z * k * 0.96f);
            b.Spike(bone, root, root + n * length * 0.25f + V(0, -length, 0), r, color, 0.6f, blend: 0.015f);
        }
    }

    private static PokeBuilder Piloswine()
    {
        var b = new PokeBuilder("Piloswine", 0.84f, BodyPlan.Quadruped, V(0, 0.3f, -0.02f)) { Coat = Fur };
        var brown = Rgb(176, 124, 86);
        var shade = Rgb(146, 100, 70);
        var pink = Rgb(240, 166, 170);
        var hoof = Rgb(108, 104, 110);

        foreach (var (z, front) in new[] { (0.1f, true), (-0.12f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.13f * s, 0.1f, z), front);
                b.Limb(leg, V(0.13f * s, 0.12f, z), V(0.135f * s, 0.035f, z), 0.05f, 0.045f, shade);
                b.Ell(leg, V(0.135f * s, 0.02f, z + 0.02f), V(0.045f, 0.02f, 0.05f), hoof);
            });
        // A great mound of shaggy fur, hanging over its eyes, falling in points all round its foot
        var c = V(0, 0.32f, -0.02f);
        var r = V(0.24f, 0.26f, 0.24f);
        b.Ell(Body, c, r, brown);
        Shag(b, Body, c, r, 0.18f, 22, 0.13f, 0.045f, shade);
        Shag(b, Body, c, r, 0.3f, 18, 0.1f, 0.04f, brown);
        int tail = b.Tail(V(0, 0.28f, -0.25f));
        b.Ell(tail, V(0, 0.27f, -0.26f), V(0.03f, 0.03f, 0.03f), shade);
        // A pink snout poking out under the fringe, a pair of white tusks curving from it
        int head = b.Head(V(0, 0.2f, 0.18f));
        b.Ell(head, V(0, 0.15f, 0.16f), V(0.08f, 0.07f, 0.07f), brown, blend: 0.03f);
        b.Ell(head, V(0, 0.12f, 0.215f), V(0.055f, 0.04f, 0.035f), pink, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Mark(head, V(0.02f * s, 0.12f, 0.25f), V(0, 0, 1f), 0.009f, 0.013f, Rgb(120, 70, 70));
            b.Tube(head, Smooth(3, V(0.06f * s, 0.12f, 0.22f), V(0.12f * s, 0.06f, 0.26f), V(0.15f * s, 0.06f, 0.32f), V(0.14f * s, 0.11f, 0.34f)), 0.024f, 0.01f, White, Shell, 0f);
        });
        return b;
    }

    private static PokeBuilder Mamoswine()
    {
        var b = new PokeBuilder("Mamoswine", 1f, BodyPlan.Quadruped, V(0, 0.42f, -0.04f)) { Coat = Fur };
        var brown = Rgb(130, 82, 50);
        var tan = Rgb(220, 186, 138);
        var blue = Rgb(110, 196, 236);
        var pink = Rgb(238, 162, 166);

        foreach (var (z, front) in new[] { (0.14f, true), (-0.22f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.17f * s, 0.18f, z), front);
                b.Limb(leg, V(0.17f * s, 0.2f, z), V(0.18f * s, 0.05f, z), 0.08f, 0.075f, brown);
                b.Ell(leg, V(0.18f * s, 0.03f, z + 0.03f), V(0.075f, 0.03f, 0.08f), tan);
                Shag(b, leg, V(0.18f * s, 0.16f, z), V(0.08f, 0.1f, 0.08f), 0.17f, 8, 0.07f, 0.03f, brown);
            });
        // A huge shaggy body with a hump, dark fringes hanging down its sides
        var c = V(0, 0.44f, -0.04f);
        var r = V(0.28f, 0.27f, 0.32f);
        b.Ell(Body, c, r, brown);
        b.Ell(Body, V(0, 0.6f, 0.08f), V(0.2f, 0.16f, 0.18f), brown);
        Shag(b, Body, c, r, 0.32f, 24, 0.12f, 0.05f, Rgb(108, 66, 40));
        int tail = b.Tail(V(0, 0.42f, -0.35f));
        b.Spike(tail, V(0, 0.42f, -0.34f), V(0, 0.34f, -0.42f), 0.03f, brown);
        // A face in pale fur, a blue mask round the eyes, a pink snout and two great tusks
        int head = b.Head(V(0, 0.5f, 0.22f));
        var hc = V(0, 0.5f, 0.27f);
        var hr = V(0.16f, 0.14f, 0.1f);
        b.Ell(head, hc, hr, tan);
        Shag(b, head, hc, hr, 0.4f, 14, 0.1f, 0.04f, tan, 0.3f);
        b.PaintEll(head, V(0, 0.54f, 0.34f), V(0.14f, 0.045f, 0.08f), White);
        b.PaintEll(head, V(0, 0.54f, 0.35f), V(0.12f, 0.03f, 0.08f), blue);
        b.Ell(head, V(0, 0.45f, 0.36f), V(0.055f, 0.042f, 0.035f), pink, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Mark(head, V(0.02f * s, 0.45f, 0.394f), V(0, 0, 1f), 0.01f, 0.014f, Rgb(120, 70, 70));
            b.Tube(head, Smooth(3, V(0.09f * s, 0.43f, 0.32f), V(0.17f * s, 0.3f, 0.36f), V(0.22f * s, 0.24f, 0.46f), V(0.21f * s, 0.32f, 0.54f)), 0.045f, 0.015f, White, Shell, 0f);
        });
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.06f * s, 0.545f), V(0.45f * s, 0.05f, 1f), 0.018f));
        return b;
    }

    // ------------------------------------------------------------------ Snorunt line

    private static PokeBuilder Snorunt()
    {
        var b = new PokeBuilder("Snorunt", 0.6f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var straw = Rgb(240, 202, 112);
        var rim = Rgb(242, 126, 62);
        var dark = Rgb(70, 74, 86);
        var ice = Rgb(120, 214, 244);

        // A hood like a cone of straw, flat at its hem and lined there with orange
        b.Spike(Body, V(0, 0.12f, 0), V(0, 0.7f, -0.02f), 0.2f, straw, blend: 0f);
        b.CutBox(Body, V(0, -0.2f, 0), V(0.4f, 0.235f, 0.4f), Quaternion.Identity);
        b.PaintTorus(Body, V(0, 0.045f, 0), 0.18f, 0.02f, rim);
        // At its front an opening coming to a point, edged in orange, with a dark face in it: pale blue eyes and a grin of white teeth
        b.Ell(Body, V(0, 0.24f, 0.09f), V(0.13f, 0.165f, 0.09f), rim, blend: 0.006f);
        b.Spike(Body, V(0, 0.35f, 0.07f), V(0, 0.52f, 0.083f), 0.07f, rim, blend: 0.006f);
        int head = b.Head(V(0, 0.3f, 0.06f));
        var c = V(0, 0.24f, 0.1f);
        var r = V(0.112f, 0.148f, 0.088f);
        b.Ell(head, c, r, dark, blend: 0.004f);
        b.Spike(head, V(0, 0.34f, 0.093f), V(0, 0.48f, 0.1f), 0.055f, dark, blend: 0.004f);
        b.Ell(head, V(0, 0.19f, 0.175f), V(0.066f, 0.026f, 0.018f), White, blend: 0.006f);
        foreach (float x in new[] { -0.033f, 0f, 0.033f }) b.PaintEll(head, V(x, 0.19f, 0.19f), V(0.004f, 0.028f, 0.012f), dark, soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.06f, 0.04f));
            b.Ell(leg, V(0.065f * s, 0.03f, 0.12f), V(0.045f, 0.03f, 0.05f), dark);
            int arm = b.Arm(s, V(0.06f * s, 0.16f, 0.12f));
            b.Ell(arm, V(0.045f * s, 0.1f, 0.205f), V(0.04f, 0.04f, 0.04f), dark);
            b.Eye(head, On(c, r, 0.045f * s, 0.27f), V(0.3f * s, 0.05f, 1f), 0.024f, sclera: true, white: ice, pupil: Rgb(40, 140, 196));
        });
        return b;
    }

    private static PokeBuilder Glalie()
    {
        var b = new PokeBuilder("Glalie", 0.84f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var ice = Rgb(232, 238, 246);
        var black = Rgb(40, 40, 50);
        var cyan = Rgb(70, 200, 240);

        // A great round face of ice, black rock showing through it in patches, two black horns
        var c = V(0, 0.42f, 0);
        var r = V(0.24f, 0.22f, 0.22f);
        b.Ell(Body, c, r, ice);
        for (int i = 0; i < 22; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / 22f;
            float ring = MathF.Sqrt(1f - y * y);
            float a = i * 2.39996f;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (n.Z > 0.35f && y < 0.75f) continue;
            b.PaintEll(Body, c + n * r, V(0.055f, 0.05f, 0.055f), black, Toward(n));
        }
        PokeBuilder.Both(s => b.Spike(Body, V(0.13f * s, 0.57f, -0.02f), V(0.27f * s, 0.8f, -0.07f), 0.08f, black, 0.6f, Shell));
        // A black mask across angry cyan eyes, a wide grin full of teeth
        b.PaintEll(Body, V(0, 0.48f, 0.17f), V(0.2f, 0.05f, 0.08f), black);
        b.PaintEll(Body, V(0, 0.53f, 0.15f), V(0.035f, 0.07f, 0.08f), black);
        Grin(b, Body, V(0, 0.33f, 0.2f), V(0.13f, 0.042f, 0.05f), Rgb(60, 50, 70));
        b.Box(Body, V(0, 0.357f, 0.18f), V(0.11f, 0.014f, 0.03f), 0.006f, White, V(-10f, 0, 0), blend: 0.004f);
        b.Box(Body, V(0, 0.305f, 0.18f), V(0.09f, 0.012f, 0.03f), 0.006f, White, V(10f, 0, 0), blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.085f * s, 0.475f);
            b.Eye(Body, at, Outward(c, r, at), 0.034f, cyan, glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Froslass()
    {
        var b = new PokeBuilder("Froslass", 0.82f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var white = Rgb(244, 244, 250);
        var ice = Rgb(150, 206, 240);
        var purple = Rgb(132, 82, 184);
        var sash = Rgb(240, 84, 40);

        // A body like a kimono of snow, pale blue at its hem, a red-orange sash with a bow behind
        b.Ell(Body, V(0, 0.42f, 0), V(0.095f, 0.17f, 0.085f), white);
        int tail = b.Tail(V(0, 0.28f, -0.01f));
        var path = Smooth(3, V(0, 0.3f, 0.0f), V(0.02f, 0.18f, -0.04f), V(0.08f, 0.1f, -0.1f), V(0.16f, 0.1f, -0.16f));
        b.Tube(tail, path, 0.085f, 0.012f, white, blend: 0f);
        b.PaintEll(tail, V(0.06f, 0.12f, -0.09f), V(0.1f, 0.06f, 0.1f), ice);
        b.Torus(Body, V(0, 0.4f, 0), 0.093f, 0.022f, sash, sz: 0.9f, blend: 0.004f);
        Frond(b, Body, V(0.04f, 0.4f, -0.07f), V(0.17f, 0.36f, -0.18f), 0.05f, sash, V(0.3f, 1f, 0.2f), 0.3f);
        foreach (var (x, y) in new[] { (-0.05f, 0.35f), (0.05f, 0.37f) })
        {
            var at = On(V(0, 0.42f, 0), V(0.095f, 0.17f, 0.085f), x, y);
            b.Mark(Body, at, Outward(V(0, 0.42f, 0), V(0.095f, 0.17f, 0.085f), at), 0.015f, 0.015f, ice, MarkShape.Diamond);
        }
        // Sleeves folded at the chest, pale blue at the cuffs
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.5f, 0.02f));
            b.Limb(arm, V(0.08f * s, 0.51f, 0.02f), V(0.05f * s, 0.45f, 0.1f), 0.035f, 0.042f, white);
            b.PaintEll(arm, V(0.05f * s, 0.45f, 0.12f), V(0.045f, 0.045f, 0.025f), ice);
            b.Ell(arm, V(0.025f * s, 0.46f, 0.13f), V(0.02f, 0.02f, 0.02f), white);
        });
        // A head with a hood's lobes hanging at its sides, two ice crystals on top, a purple mask round its eyes
        int head = b.Head(V(0, 0.58f, 0));
        var hc = V(0, 0.665f, 0.01f);
        var hr = V(0.085f, 0.085f, 0.08f);
        b.Ell(head, hc, hr, white);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, V(0.1f * s, 0.6f, -0.005f), V(0.05f, 0.1f, 0.06f), white, V(0, 0, 15f * s));
            b.PaintEll(head, V(0.11f * s, 0.53f, -0.005f), V(0.055f, 0.035f, 0.065f), ice);
            b.Spike(head, V(0.04f * s, 0.73f, -0.01f), V(0.07f * s, 0.84f, -0.03f), 0.03f, ice, mat: Shell, blend: 0.006f);
            b.Spike(head, V(0.055f * s, 0.73f, -0.02f), V(0.1f * s, 0.79f, -0.05f), 0.02f, ice, mat: Shell, blend: 0.006f);
            var at = On(hc, hr, 0.038f * s, 0.665f);
            b.PaintEll(head, at, V(0.034f, 0.032f, 0.03f), purple);
            b.Eye(head, at, Outward(hc, hr, at), 0.02f, sclera: true, white: Rgb(250, 220, 90), pupil: Rgb(60, 110, 220));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Absol

    private static PokeBuilder Absol()
    {
        var b = new PokeBuilder("Absol", 0.86f, BodyPlan.Quadruped, V(0, 0.4f, -0.04f)) { Coat = Fur };
        var white = Rgb(240, 242, 248);
        var blue = Rgb(56, 100, 170);
        var face = Rgb(70, 92, 150);

        foreach (var (z, front) in new[] { (0.12f, true), (-0.18f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.08f * s, 0.32f, z), front);
                b.Limb(leg, V(0.08f * s, 0.34f, z), V(0.09f * s, 0.16f, z + 0.01f), 0.045f, 0.035f, white);
                b.Limb(leg, V(0.09f * s, 0.16f, z + 0.01f), V(0.09f * s, 0.04f, z + 0.02f), 0.033f, 0.03f, white);
                b.Ell(leg, V(0.09f * s, 0.03f, z + 0.04f), V(0.035f, 0.025f, 0.045f), blue);
                for (int i = -1; i <= 1; i++)
                    b.Spike(leg, V(0.09f * s + 0.018f * i, 0.03f, z + 0.07f), V(0.09f * s + 0.022f * i, 0.005f, z + 0.1f), 0.01f, blue, mat: Shell, blend: 0.004f);
            });
        // A white body, a shaggy ruff of fur down its chest, a tail like a curved blade
        b.Ell(Body, V(0, 0.4f, -0.04f), V(0.12f, 0.12f, 0.24f), white);
        for (int i = 0; i < 7; i++)
        {
            float a = (i - 3f) * 0.42f;
            var root = V(MathF.Sin(a) * 0.09f, 0.42f, 0.17f + MathF.Cos(a) * 0.03f);
            b.Spike(Body, root, root + V(MathF.Sin(a) * 0.04f, -0.12f, 0.04f), 0.04f, white, 0.6f, blend: 0.02f);
        }
        int tail = b.Tail(V(0, 0.44f, -0.26f));
        Sickle(b, tail, V(0, 0.45f, -0.26f), V(0, 0.64f, -0.38f), V(0.0f, 0.8f, -0.32f), 0.055f, blue, V(1f, 0, 0));
        // A dark blue face on a white head, red eyes, a horn curving off one side like a crescent moon
        b.Tube(Body, new[] { V(0, 0.45f, 0.14f), V(0, 0.52f, 0.2f), V(0, 0.56f, 0.22f) }, 0.07f, 0.06f, white, blend: 0f);
        int head = b.Head(V(0, 0.55f, 0.2f));
        var hc = V(0, 0.58f, 0.25f);
        var hr = V(0.085f, 0.085f, 0.09f);
        b.Ell(head, hc, hr, white);
        b.Ell(head, V(0, 0.55f, 0.32f), V(0.05f, 0.045f, 0.05f), face, blend: 0.02f);
        b.PaintEll(head, V(0, 0.565f, 0.31f), V(0.075f, 0.05f, 0.06f), face);
        b.Tube(head, Smooth(3, V(0.06f, 0.62f, 0.24f), V(0.12f, 0.71f, 0.2f), V(0.11f, 0.82f, 0.12f), V(0.03f, 0.86f, 0.06f), V(-0.05f, 0.82f, 0.04f)), 0.032f, 0.006f, blue, Shell, 0f);
        b.Mark(head, On(hc, hr, 0, 0.64f), V(0, 0.5f, 1f), 0.012f, 0.02f, face);
        PokeBuilder.Both(s => b.Eye(head, V(0.035f * s, 0.585f, 0.322f), V(0.4f * s, 0.1f, 1f), 0.015f, Rgb(220, 40, 50), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ the lake guardians

    /// <summary>
    /// The body the three spirits of the lakes share: slim and pale blue, thin arms, two long tails curving round
    /// below it, each ending in three points round a red gem.
    /// </summary>
    private static void LakeSpirit(PokeBuilder b, Color skin, Color gem)
    {
        b.Ell(Body, V(0, 0.44f, 0), V(0.045f, 0.075f, 0.04f), skin);
        b.Ell(Body, V(0, 0.37f, -0.005f), V(0.04f, 0.035f, 0.035f), skin);
        b.Limb(Body, V(0, 0.49f, 0), V(0, 0.56f, 0.008f), 0.022f, 0.02f, skin);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.04f * s, 0.48f, 0.01f));
            b.Limb(arm, V(0.04f * s, 0.49f, 0.01f), V(0.11f * s, 0.44f, 0.05f), 0.014f, 0.011f, skin);
            b.Ell(arm, V(0.115f * s, 0.435f, 0.055f), V(0.017f, 0.017f, 0.017f), skin);
            int tail = b.Part(s < 0 ? "tailL" : "tailR", Body, V(0.02f * s, 0.36f, -0.01f), PokeRole.Tail, 0.5f * s, s);
            var path = Smooth(3, V(0.02f * s, 0.37f, -0.01f), V(0.06f * s, 0.24f, -0.04f), V(0.12f * s, 0.14f, -0.04f), V(0.2f * s, 0.12f, 0.0f), V(0.24f * s, 0.18f, 0.02f));
            b.Tube(tail, path, 0.02f, 0.012f, skin, blend: 0f);
            var tip = path[^1];
            foreach (var d in new[] { V(0.0f, 1f, 0), V(0.8f * s, 0.4f, 0), V(0.6f * s, -0.6f, 0) })
                Blade(b, tail, tip, tip + d * 0.055f, 0.022f, skin, V(0, 0, 1f), 0.35f);
            b.Ell(tail, tip + V(0, 0, 0.012f), V(0.016f, 0.02f, 0.012f), gem, mat: Glow, blend: 0.004f);
        });
    }

    private static PokeBuilder Uxie()
    {
        var b = new PokeBuilder("Uxie", 0.5f, BodyPlan.Floating, V(0, 0.44f, 0)) { Coat = Fur }.Hover();
        var skin = Rgb(190, 214, 238);
        var yellow = Rgb(246, 206, 60);
        var gem = Rgb(230, 40, 60);
        LakeSpirit(b, skin, gem);
        // A head under a great yellow helmet, a red gem on its brow, its eyes always shut
        int head = b.Head(V(0, 0.52f, 0));
        var c = V(0, 0.6f, 0.015f);
        var r = V(0.065f, 0.065f, 0.06f);
        b.Ell(head, c, r, skin);
        b.Ell(head, V(0, 0.655f, -0.01f), V(0.088f, 0.06f, 0.082f), yellow);
        PokeBuilder.Both(s => b.Ell(head, V(0.075f * s, 0.6f, -0.01f), V(0.045f, 0.065f, 0.06f), yellow));
        b.Ell(head, V(0, 0.665f, 0.07f), V(0.016f, 0.02f, 0.012f), gem, mat: Glow, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.026f * s, 0.595f), V(0.35f * s, 0, 1f), 0.016f, closed: true));
        return Lift(b);
    }

    private static PokeBuilder Mesprit()
    {
        var b = new PokeBuilder("Mesprit", 0.5f, BodyPlan.Floating, V(0, 0.44f, 0)) { Coat = Fur }.Hover();
        var skin = Rgb(190, 214, 238);
        var pink = Rgb(244, 150, 190);
        var gem = Rgb(230, 40, 60);
        LakeSpirit(b, skin, gem);
        // A head under pink hair that falls in two long locks at its sides, a red gem on its brow
        int head = b.Head(V(0, 0.52f, 0));
        var c = V(0, 0.6f, 0.015f);
        var r = V(0.065f, 0.065f, 0.06f);
        b.Ell(head, c, r, skin);
        b.Ell(head, V(0, 0.65f, -0.01f), V(0.08f, 0.055f, 0.075f), pink);
        PokeBuilder.Both(s => b.Ell(head, V(0.11f * s, 0.57f, -0.01f), V(0.04f, 0.1f, 0.036f), pink, V(0, 0, -28f * s)));
        b.Ell(head, V(0, 0.665f, 0.065f), V(0.016f, 0.02f, 0.012f), gem, mat: Glow, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.027f * s, 0.595f), V(0.35f * s, 0, 1f), 0.017f, sclera: true, white: Rgb(250, 220, 80), pupil: Rgb(36, 32, 40)));
        return Lift(b);
    }

    private static PokeBuilder Azelf()
    {
        var b = new PokeBuilder("Azelf", 0.5f, BodyPlan.Floating, V(0, 0.44f, 0)) { Coat = Fur }.Hover();
        var skin = Rgb(190, 214, 238);
        var blue = Rgb(60, 168, 230);
        var gem = Rgb(230, 40, 60);
        LakeSpirit(b, skin, gem);
        // A head under a blue cap swept back to points, a red gem on its brow, a determined look
        int head = b.Head(V(0, 0.52f, 0));
        var c = V(0, 0.6f, 0.015f);
        var r = V(0.065f, 0.065f, 0.06f);
        b.Ell(head, c, r, skin);
        b.Ell(head, V(0, 0.655f, -0.01f), V(0.078f, 0.055f, 0.078f), blue);
        b.Spike(head, V(0, 0.68f, -0.03f), V(0, 0.75f, -0.13f), 0.045f, blue, 0.6f);
        PokeBuilder.Both(s => b.Spike(head, V(0.06f * s, 0.62f, -0.01f), V(0.13f * s, 0.53f, -0.05f), 0.04f, blue, 0.6f));
        b.Ell(head, V(0, 0.665f, 0.066f), V(0.016f, 0.02f, 0.012f), gem, mat: Glow, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.027f * s, 0.595f), V(0.35f * s, 0, 1f), 0.017f, sclera: true, white: Rgb(250, 220, 80), pupil: Rgb(36, 32, 40), glare: true));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Dialga and Palkia

    private static PokeBuilder Dialga()
    {
        var b = new PokeBuilder("Dialga", 1f, BodyPlan.Quadruped, V(0, 0.46f, -0.04f)) { Coat = Metal };
        var blue = Rgb(64, 98, 172);
        var steel = Rgb(206, 216, 234);
        var cyan = Rgb(96, 206, 242);

        // Four strong legs, steel plates over the shoulders, the hips and down the shins
        foreach (var (z, front) in new[] { (0.15f, true), (-0.22f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.14f * s, 0.4f, z), front);
                var knee = V(0.16f * s, 0.22f, z + 0.03f);
                b.Ell(leg, V(0.15f * s, 0.38f, z), V(0.09f, 0.11f, 0.1f), blue);
                b.Limb(leg, V(0.15f * s, 0.36f, z), knee, 0.08f, 0.062f, blue);
                b.Limb(leg, knee, V(0.16f * s, 0.05f, z + 0.03f), 0.062f, 0.055f, blue);
                b.PaintEll(leg, V(0.22f * s, 0.3f, z + 0.01f), V(0.012f, 0.1f, 0.035f), cyan, V(0, 0, 8f * s));
                Blade(b, leg, knee + V(0, 0.03f, 0.05f), knee + V(0, -0.15f, 0.08f), 0.05f, steel, V(0, 0, 1f), 0.3f, Metal);
                Blade(b, leg, V(0.2f * s, 0.46f, z + 0.02f), V(0.27f * s, 0.32f, z + 0.03f), 0.07f, steel, V(1f * s, 0.3f, 0), 0.25f, Metal);
                Claws(b, leg, V(0.16f * s, 0.02f, z + 0.09f), V(0.03f, 0, 0), V(0, -0.2f, 1f), 0.035f, 0.015f);
            });
        // A deep blue body, a steel breastplate with a diamond in it, a fan of steel fins rising off its back
        b.Ell(Body, V(0, 0.47f, -0.04f), V(0.19f, 0.17f, 0.3f), blue);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.16f * s, 0.5f, -0.06f), V(0.04f, 0.015f, 0.22f), cyan, V(8f, 0, 0)));
        b.Ell(Body, V(0, 0.5f, 0.18f), V(0.14f, 0.15f, 0.09f), steel, mat: Metal, blend: 0.02f);
        b.Box(Body, V(0, 0.53f, 0.26f), V(0.034f, 0.034f, 0.012f), 0.004f, cyan, V(0, 0, 45f), Glow, 0.004f);
        for (int k = 0; k < 3; k++)
            PokeBuilder.Both(s =>
            {
                var root = V(0.05f * s, 0.6f, 0.06f - 0.09f * k);
                Blade(b, Body, root, root + V(0.13f * s + 0.05f * s * k, 0.3f - 0.05f * k, -0.17f), 0.06f, steel, V(1f, 0, 0.6f * s), 0.22f, Metal);
            });
        int tail = b.Tail(V(0, 0.44f, -0.32f));
        var path = Smooth(3, V(0, 0.46f, -0.3f), V(0, 0.38f, -0.5f), V(0, 0.28f, -0.66f), V(0, 0.25f, -0.8f));
        b.Tube(tail, path, 0.085f, 0.015f, blue, blend: 0f);
        Blade(b, tail, path[^3], path[^1] + V(0, 0.06f, -0.04f), 0.045f, steel, V(1f, 0, 0), 0.25f, Metal);
        // A thick neck up to a head crowned with steel crests, red eyes
        var neck = Smooth(3, V(0, 0.55f, 0.16f), V(0, 0.7f, 0.25f), V(0, 0.84f, 0.28f), V(0, 0.93f, 0.3f));
        b.Tube(Body, neck, 0.1f, 0.06f, blue, blend: 0f);
        b.PaintEll(Body, V(0, 0.73f, 0.33f), V(0.05f, 0.14f, 0.025f), steel, V(-15f, 0, 0));
        int head = b.Head(V(0, 0.93f, 0.3f));
        b.Ell(head, V(0, 0.96f, 0.35f), V(0.065f, 0.062f, 0.08f), blue);
        b.Ell(head, V(0, 0.94f, 0.44f), V(0.045f, 0.04f, 0.065f), blue, blend: 0.02f);
        b.PaintEll(head, V(0, 0.925f, 0.46f), V(0.04f, 0.018f, 0.055f), steel);
        Blade(b, head, V(0, 1.0f, 0.34f), V(0, 1.16f, 0.18f), 0.05f, steel, V(1f, 0, 0), 0.3f, Metal);
        PokeBuilder.Both(s => Blade(b, head, V(0.045f * s, 0.99f, 0.33f), V(0.12f * s, 1.1f, 0.2f), 0.04f, steel, V(1f, 0, 0.4f * s), 0.3f, Metal));
        PokeBuilder.Both(s => b.Eye(head, V(0.054f * s, 0.975f, 0.39f), V(0.8f * s, 0.15f, 0.6f), 0.018f, Rgb(220, 40, 50), glare: true));
        return b;
    }

    private static PokeBuilder Palkia()
    {
        var b = new PokeBuilder("Palkia", 1f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Scales };
        var white = Rgb(236, 230, 244);
        var pink = Rgb(190, 92, 150);
        var gray = Rgb(150, 146, 162);
        var pearl = Rgb(250, 176, 164);
        var fin = Rgb(226, 212, 240);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.3f, 0.0f));
            b.Ell(leg, V(0.13f * s, 0.26f, -0.01f), V(0.09f, 0.11f, 0.1f), white);
            b.PaintTorus(leg, V(0.13f * s, 0.28f, -0.01f), 0.095f, 0.012f, pink, V(0, 0, 15f * s));
            b.Limb(leg, V(0.14f * s, 0.18f, 0.0f), V(0.15f * s, 0.05f, 0.03f), 0.055f, 0.05f, white);
            b.Ell(leg, V(0.15f * s, 0.03f, 0.06f), V(0.06f, 0.03f, 0.065f), white);
            Claws(b, leg, V(0.15f * s, 0.02f, 0.11f), V(0.028f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.012f);
        });
        // A pale body with stripes of deep pink, grey beneath, a pearl set in armour on each shoulder
        b.Ell(Body, V(0, 0.5f, 0), V(0.15f, 0.2f, 0.13f), white);
        b.PaintEll(Body, V(0, 0.48f, 0.09f), V(0.09f, 0.15f, 0.06f), gray);
        foreach (float y in new[] { 0.38f, 0.46f })
            PokeBuilder.Both(s => Tiger(b, Body, V(0, 0.5f, 0), V(0.15f, 0.2f, 0.13f), 1.2f * s, y, 0.05f, 0f, pink, 0.014f));
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.16f * s, 0.63f, 0.02f);
            b.Ell(Body, shoulder, V(0.085f, 0.085f, 0.085f), white);
            b.PaintTorus(Body, shoulder, 0.08f, 0.012f, pink, Euler(V(s, 0.2f, 0.3f)));
            b.Ell(Body, shoulder + V(0.06f * s, 0.01f, 0.03f), V(0.045f, 0.045f, 0.045f), pearl, mat: Glow, blend: 0.008f);
        });
        // Great pale fins sweeping back from its shoulders like wings
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.12f * s, 0.66f, -0.06f));
            var root = V(0.1f * s, 0.66f, -0.06f);
            b.Ell(wing, root, V(0.035f, 0.035f, 0.035f), fin);
            var tip = V(0.42f * s, 0.86f, -0.36f);
            var p = Frond(b, wing, root, tip, 0.11f, fin, V(0.3f * s, 0.4f, -1f), 0.08f);
            var across = Vector3.Transform(Vector3.UnitX, p.Rotation);
            foreach (float o in new[] { -0.035f, 0.035f })
                b.PaintEll(wing, Vector3.Lerp(root, tip, 0.5f) + across * o, V(0.006f, 0.19f, 0.03f), pink).Rotation = p.Rotation;
        });
        // Grey arms with white claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.6f, 0.06f));
            b.Limb(arm, V(0.15f * s, 0.6f, 0.06f), V(0.24f * s, 0.47f, 0.15f), 0.05f, 0.04f, gray);
            b.Ell(arm, V(0.25f * s, 0.45f, 0.17f), V(0.04f, 0.04f, 0.04f), gray);
            Claws(b, arm, V(0.26f * s, 0.43f, 0.2f), V(0.018f * s, 0.01f, 0), V(0.2f * s, -0.5f, 1f), 0.035f, 0.011f);
        });
        // A neck up to a head swept back in a crest, red eyes, a pink mouth
        b.Tube(Body, new[] { V(0, 0.66f, 0.04f), V(0, 0.76f, 0.07f), V(0, 0.84f, 0.11f) }, 0.065f, 0.05f, white, blend: 0f);
        int head = b.Head(V(0, 0.84f, 0.11f));
        var hc = V(0, 0.88f, 0.15f);
        var hr = V(0.06f, 0.055f, 0.08f);
        b.Ell(head, hc, hr, white);
        b.Ell(head, V(0, 0.86f, 0.23f), V(0.042f, 0.035f, 0.055f), white, blend: 0.02f);
        Grin(b, head, V(0, 0.84f, 0.26f), V(0.032f, 0.016f, 0.025f), Rgb(220, 110, 140));
        Blade(b, head, V(0, 0.92f, 0.13f), V(0, 0.96f, -0.06f), 0.05f, white, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s => b.Eye(head, V(0.048f * s, 0.895f, 0.2f), V(0.8f * s, 0.15f, 0.6f), 0.016f, Rgb(220, 40, 50), glare: true));
        int tail = b.Tail(V(0, 0.38f, -0.11f));
        var path = Smooth(3, V(0, 0.38f, -0.1f), V(0.02f, 0.24f, -0.28f), V(0.08f, 0.16f, -0.44f), V(0.16f, 0.2f, -0.56f));
        b.Tube(tail, path, 0.07f, 0.015f, white, blend: 0f);
        for (int i = 2; i < path.Length - 2; i += 3) b.PaintTorus(tail, path[i], 0.07f - 0.055f * i / path.Length, 0.01f, pink, Euler(path[i + 1] - path[i - 1]));
        return b;
    }

    // ------------------------------------------------------------------ Manaphy

    private static PokeBuilder Manaphy()
    {
        var b = new PokeBuilder("Manaphy", 0.52f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Fur };
        var blue = Rgb(80, 182, 240);
        var yellow = Rgb(250, 224, 72);
        var red = Rgb(232, 64, 70);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.07f, 0.01f));
            b.Ell(leg, V(0.05f * s, 0.045f, 0.03f), V(0.04f, 0.045f, 0.05f), blue);
        });
        // A small round body with a red jewel in its chest, flippers for arms
        b.Ell(Body, V(0, 0.15f, 0), V(0.075f, 0.08f, 0.07f), blue);
        b.Ell(Body, V(0, 0.165f, 0.064f), V(0.024f, 0.024f, 0.012f), red, mat: Glow, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.18f, 0.01f));
            b.Ell(arm, V(0.13f * s, 0.165f, 0.02f), V(0.075f, 0.024f, 0.04f), blue, V(0, 0, -12f * s));
        });
        // A big round head, yellow dots on it, a long feeler curling off the top to a little ball
        int head = b.Head(V(0, 0.22f, 0));
        var c = V(0, 0.32f, 0.01f);
        var r = V(0.1f, 0.095f, 0.09f);
        b.Ell(head, c, r, blue);
        var feeler = Smooth(3, V(0, 0.4f, -0.02f), V(0.05f, 0.47f, -0.06f), V(0.14f, 0.48f, -0.1f), V(0.22f, 0.43f, -0.12f));
        b.Tube(head, feeler, 0.017f, 0.011f, blue, blend: 0f);
        b.Ell(head, feeler[^1] + V(0.012f, -0.005f, 0), V(0.022f, 0.022f, 0.022f), blue, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var dot = On(c, r, 0.055f * s, 0.385f);
            b.Mark(head, dot, Outward(c, r, dot), 0.014f, 0.014f, yellow);
            var cheek = On(c, r, 0.075f * s, 0.29f);
            b.Mark(head, cheek, Outward(c, r, cheek), 0.012f, 0.012f, yellow);
        });
        b.Mark(head, On(c, r, 0, 0.275f), V(0, -0.1f, 1f), 0.015f, 0.007f, Rgb(40, 60, 120), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.04f * s, 0.32f), V(0.35f * s, 0.05f, 1f), 0.03f, Rgb(40, 90, 200)));
        return b;
    }
}
