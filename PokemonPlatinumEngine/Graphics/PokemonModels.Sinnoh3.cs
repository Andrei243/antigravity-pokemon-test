using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Plan 03 · D8: hand-built models for the third batch of Platinum's Sinnoh Pokédex, Azurill (124) to Magnezone (180).
// The legendaries in that range (Uxie, Mesprit, Azelf, Dialga, Palkia and Manaphy) wait for the last batch, D9.
// Helpers shared with the earlier batches are in PokemonModels.Sinnoh1.cs and PokemonModels.Sinnoh2.cs.
internal static partial class PokemonModels
{
    private const float Degree = MathF.PI / 180f;

    /// <summary>The point on the front of an upright ellipsoid at (x, y): where an eye or a marking on it goes.</summary>
    private static Vector3 On(Vector3 center, Vector3 radii, float x, float y)
    {
        float u = (x - center.X) / radii.X, v = (y - center.Y) / radii.Y;
        return V(x, y, center.Z + radii.Z * MathF.Sqrt(MathF.Max(0f, 1f - u * u - v * v)));
    }

    /// <summary>The way an upright ellipsoid's surface faces at a point on it.</summary>
    private static Vector3 Outward(Vector3 center, Vector3 radii, Vector3 at) => Vector3.Normalize((at - center) / (radii * radii));

    /// <summary>
    /// A pointed blade from <paramref name="root"/> to <paramref name="tip"/>, flat across <paramref name="facing"/>
    /// (the way its face looks): a swept-back fin, a pointed leaf. A spike's own squash can't choose that way.
    /// </summary>
    private static void Blade(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float width, Color color, Vector3 facing, float thin = 0.22f, SurfaceMaterial? mat = null)
    {
        var p = b.Spike(bone, root, tip, width, color, mat: mat);
        var y = Vector3.Normalize(tip - root);
        var z = Vector3.Normalize(facing - y * Vector3.Dot(facing, y));
        var x = Vector3.Cross(y, z);
        p.Rotation = Quaternion.CreateFromRotationMatrix(new Matrix4x4(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1));
        p.Stretch = V(1f, 1f, thin);
    }

    // ------------------------------------------------------------------ Azurill line

    private static PokeBuilder Azurill()
    {
        var b = new PokeBuilder("Azurill", 0.45f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var blue = Rgb(56, 164, 238);
        var pink = Rgb(240, 112, 132);
        var black = Rgb(36, 36, 44);

        // It sits on the great round ball at the end of its tail, which bounces it along
        b.Ell(Body, V(0, 0.105f, -0.01f), V(0.125f, 0.105f, 0.12f), blue);
        var c = V(0, 0.3f, 0);
        var r = V(0.1f, 0.105f, 0.095f);
        b.Ell(Body, c, r, blue);
        int tail = b.Tail(V(0, 0.24f, -0.08f));
        Bolt(b, tail, new[] { V(0, 0.24f, -0.08f), V(0.05f, 0.27f, -0.14f), V(0.08f, 0.2f, -0.15f), V(0.13f, 0.22f, -0.2f) }, new[] { 0.012f, 0.012f, 0.012f }, 0.01f, black);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.2f, 0.06f));
            b.Ell(leg, V(0.05f * s, 0.205f, 0.075f), V(0.028f, 0.022f, 0.032f), blue);
            int arm = b.Arm(s, V(0.08f * s, 0.31f, 0.04f));
            b.Limb(arm, V(0.08f * s, 0.31f, 0.04f), V(0.125f * s, 0.36f, 0.07f), 0.022f, 0.02f, blue);
        });
        int head = b.Head(V(0, 0.36f, 0));
        b.Ell(head, V(0, 0.38f, -0.005f), V(0.085f, 0.035f, 0.08f), blue);
        // Round ears pink inside, a worried little mouth, a white spot on each cheek
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.38f, -0.01f));
            b.Ell(ear, V(0.085f * s, 0.425f, -0.01f), V(0.045f, 0.045f, 0.02f), blue, V(0, 0, -15f * s));
            b.PaintEll(ear, V(0.085f * s, 0.425f, 0.006f), V(0.03f, 0.03f, 0.02f), pink);
            var cheek = On(c, r, 0.068f * s, 0.285f);
            b.Mark(Body, cheek, Outward(c, r, cheek), 0.015f, 0.015f, White);
        });
        b.Mark(Body, On(c, r, 0, 0.27f), V(0, -0.1f, 1f), 0.016f, 0.007f, Rgb(200, 90, 100), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.04f * s, 0.315f), V(0.35f * s, 0.05f, 1f), 0.021f));
        return b;
    }

    private static PokeBuilder Marill()
    {
        var b = new PokeBuilder("Marill", 0.52f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = Fur };
        var blue = Rgb(56, 164, 238);
        var pink = Rgb(240, 112, 132);
        var black = Rgb(36, 36, 44);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.06f, 0.02f));
            b.Ell(leg, V(0.065f * s, 0.025f, 0.05f), V(0.035f, 0.025f, 0.045f), blue);
        });
        // A ball of a body, white below, and a zigzag tail ending in a ball that floats it in water
        var c = V(0, 0.17f, 0);
        var r = V(0.15f, 0.145f, 0.14f);
        b.Ell(Body, c, r, blue);
        b.PaintEll(Body, V(0, 0.08f, 0.03f), V(0.16f, 0.08f, 0.15f), White);
        int tail = b.Tail(V(0, 0.15f, -0.13f));
        Bolt(b, tail, new[] { V(0, 0.15f, -0.13f), V(0.02f, 0.22f, -0.2f), V(0.03f, 0.17f, -0.25f), V(0.05f, 0.27f, -0.31f) }, new[] { 0.012f, 0.012f, 0.012f }, 0.012f, black);
        b.Ell(tail, V(0.06f, 0.32f, -0.34f), V(0.055f, 0.055f, 0.055f), blue);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.17f, 0.05f));
            b.Limb(arm, V(0.12f * s, 0.17f, 0.05f), V(0.17f * s, 0.12f, 0.08f), 0.028f, 0.025f, blue);
        });
        int head = b.Head(V(0, 0.25f, 0));
        b.Ell(head, V(0, 0.29f, -0.01f), V(0.11f, 0.035f, 0.1f), blue);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.27f, -0.01f));
            b.Ell(ear, V(0.1f * s, 0.325f, -0.01f), V(0.055f, 0.055f, 0.022f), blue, V(0, 0, -20f * s));
            b.PaintEll(ear, V(0.1f * s, 0.325f, 0.008f), V(0.036f, 0.036f, 0.02f), pink);
        });
        b.Mark(Body, On(c, r, 0, 0.16f), V(0, -0.1f, 1f), 0.017f, 0.011f, Rgb(200, 60, 80));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.048f * s, 0.205f), V(0.35f * s, 0.1f, 1f), 0.024f));
        return b;
    }

    private static PokeBuilder Azumarill()
    {
        var b = new PokeBuilder("Azumarill", 0.74f, BodyPlan.Biped, V(0, 0.25f, 0)) { Coat = Fur };
        var blue = Rgb(56, 164, 238);
        var pink = Rgb(240, 112, 132);
        var black = Rgb(36, 36, 44);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.07f, 0.02f));
            b.Ell(leg, V(0.08f * s, 0.03f, 0.05f), V(0.045f, 0.03f, 0.06f), blue);
        });
        // An egg of a body, white below a wavy line like water, with white bubbles above it
        var c = V(0, 0.25f, 0);
        var r = V(0.17f, 0.22f, 0.16f);
        b.Ell(Body, c, r, blue);
        b.PaintEll(Body, V(0, 0.09f, 0.0f), V(0.18f, 0.1f, 0.17f), White);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            b.PaintEll(Body, V(MathF.Sin(a) * 0.15f, 0.175f, MathF.Cos(a) * 0.14f), V(0.032f, 0.026f, 0.032f), White);
        }
        foreach (var (x, y, size) in new[] { (-0.12f, 0.25f, 0.024f), (-0.065f, 0.215f, 0.016f), (0.09f, 0.25f, 0.02f), (0.135f, 0.21f, 0.017f), (0.12f, 0.31f, 0.013f) })
        {
            var at = On(c, r, x, y);
            b.Mark(Body, at, Outward(c, r, at), size, size, White);
        }
        int tail = b.Tail(V(0, 0.15f, -0.15f));
        Bolt(b, tail, new[] { V(0, 0.15f, -0.15f), V(0.03f, 0.2f, -0.22f), V(0.04f, 0.12f, -0.27f), V(0.06f, 0.16f, -0.33f) }, new[] { 0.013f, 0.013f, 0.013f }, 0.012f, black);
        b.Ell(tail, V(0.07f, 0.15f, -0.38f), V(0.05f, 0.05f, 0.05f), blue);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.28f, 0.04f));
            b.Limb(arm, V(0.15f * s, 0.28f, 0.04f), V(0.23f * s, 0.25f, 0.08f), 0.033f, 0.03f, blue);
        });
        int head = b.Head(V(0, 0.4f, 0));
        b.Ell(head, V(0, 0.445f, -0.005f), V(0.12f, 0.035f, 0.11f), blue);
        // Long ears like a rabbit's, pink inside
        PokeBuilder.Both(s =>
        {
            var root = V(0.06f * s, 0.43f, -0.01f);
            var tip = V(0.15f * s, 0.74f, -0.04f);
            int ear = b.Ear(head, s, root);
            Petal(b, ear, root, tip, 0.05f, blue, Fur, 0.015f);
            b.PaintEll(ear, (root + tip) / 2f + V(0, 0.02f, 0.012f), V(0.028f, 0.11f, 0.016f), pink, Euler(tip - root));
        });
        b.Mark(Body, On(c, r, 0, 0.33f), V(0, 0, 1f), 0.024f, 0.013f, Rgb(200, 60, 80));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.052f * s, 0.375f), V(0.35f * s, 0.1f, 1f), 0.024f));
        return b;
    }

    // ------------------------------------------------------------------ Skorupi line

    /// <summary>A scorpion's claw: a ball on the end of an arm with a white hook curving off its top.</summary>
    private static void Pincer(PokeBuilder b, int bone, Vector3 at, float r, Color color, Vector3 hook)
    {
        b.Ell(bone, at, V(r, r * 0.92f, r), color);
        b.Tube(bone, Smooth(3, at + V(0, r * 0.6f, 0), at + V(0, r * 0.6f, 0) + hook * 0.6f + V(0, r * 0.4f, 0), at + V(0, r * 0.6f, 0) + hook), r * 0.32f, r * 0.08f, Claw, Shell);
    }

    private static PokeBuilder Skorupi()
    {
        var b = new PokeBuilder("Skorupi", 0.66f, BodyPlan.Quadruped, V(0, 0.16f, 0)) { Coat = Shell };
        var lilac = Rgb(150, 160, 226);
        var navy = Rgb(70, 72, 122);
        var pale = Rgb(190, 200, 244);

        foreach (var (z, front) in new[] { (0.05f, true), (-0.06f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.08f * s, 0.1f, z), front);
                b.Limb(leg, V(0.08f * s, 0.1f, z), V(0.12f * s, 0.06f, z + 0.02f), 0.022f, 0.02f, pale);
                b.Limb(leg, V(0.12f * s, 0.06f, z + 0.02f), V(0.135f * s, 0.012f, z + 0.035f), 0.02f, 0.012f, pale);
            });
        // A domed shell, dark beneath, arms ending in hooked claws and a tail that curls up over its back
        b.Ell(Body, V(0, 0.15f, -0.01f), V(0.13f, 0.085f, 0.12f), lilac);
        b.PaintEll(Body, V(0, 0.08f, -0.01f), V(0.14f, 0.035f, 0.13f), navy);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.17f, 0.05f));
            b.Limb(arm, V(0.1f * s, 0.17f, 0.05f), V(0.19f * s, 0.2f, 0.1f), 0.03f, 0.028f, lilac);
            Pincer(b, arm, V(0.22f * s, 0.22f, 0.12f), 0.06f, lilac, V(-0.02f * s, 0.07f, 0.03f));
        });
        int tail = b.Tail(V(0, 0.2f, -0.1f));
        var curve = Smooth(4, V(0, 0.19f, -0.11f), V(0, 0.3f, -0.18f), V(0, 0.42f, -0.16f), V(0, 0.5f, -0.08f), V(0, 0.52f, 0.0f));
        const int Segments = 9;
        for (int i = 0; i < Segments; i++)
        {
            var at = curve[i * (curve.Length - 1) / Segments];
            float rr = 0.052f - 0.0025f * i;
            b.Ell(tail, at, V(rr, rr * 0.9f, rr), lilac, blend: 0.012f);
            b.PaintEll(tail, at + V(0, -rr * 0.45f, -rr * 0.45f), V(rr * 1.1f, rr * 0.6f, rr * 0.75f), navy);
        }
        var sting = curve[^1];
        b.Ell(tail, sting, V(0.058f, 0.058f, 0.058f), navy);
        PokeBuilder.Both(s => b.Tube(tail, Smooth(3, sting + V(0.03f * s, 0.03f, 0), sting + V(0.08f * s, 0.07f, 0.01f), sting + V(0.08f * s, 0.13f, 0.03f)), 0.02f, 0.005f, Claw, Shell));

        int head = b.Head(V(0, 0.17f, 0.07f));
        var c = V(0, 0.16f, 0.09f);
        var r = V(0.1f, 0.075f, 0.07f);
        b.Ell(head, c, r, lilac);
        // Blue eyes rimmed in dark, and small white hooks for mandibles below them
        PokeBuilder.Both(s =>
        {
            b.Eye(head, On(c, r, 0.042f * s, 0.18f), V(0.45f * s, 0.25f, 1f), 0.03f, pupil: Rgb(22, 62, 120), white: Rgb(40, 150, 226));
            b.Spike(head, V(0.03f * s, 0.12f, 0.145f), V(0.012f * s, 0.085f, 0.175f), 0.014f, Claw, mat: Shell, blend: 0.004f);
        });
        return b;
    }

    private static PokeBuilder Drapion()
    {
        var b = new PokeBuilder("Drapion", 0.96f, BodyPlan.Quadruped, V(0, 0.27f, -0.04f)) { Coat = Shell };
        var mauve = Rgb(196, 112, 164);
        var light = Rgb(226, 160, 198);
        var plum = Rgb(140, 46, 98);
        var blue = Rgb(40, 150, 226);

        foreach (var (z, front) in new[] { (0.08f, true), (-0.17f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.12f * s, 0.18f, z), front);
                b.Limb(leg, V(0.12f * s, 0.18f, z), V(0.17f * s, 0.1f, z + 0.02f), 0.055f, 0.048f, mauve);
                b.Limb(leg, V(0.17f * s, 0.1f, z + 0.02f), V(0.17f * s, 0.02f, z + 0.04f), 0.045f, 0.03f, light);
                b.Spike(leg, V(0.17f * s, 0.11f, z), V(0.24f * s, 0.15f, z - 0.03f), 0.026f, plum);
            });
        // A long armoured body, plated in bands, thick with spines down its back
        b.Ell(Body, V(0, 0.27f, -0.05f), V(0.16f, 0.12f, 0.23f), mauve);
        for (int i = 0; i < 4; i++)
        {
            float z = 0.08f - i * 0.09f, k = MathF.Sqrt(1f - (z + 0.05f) * (z + 0.05f) / (0.23f * 0.23f));
            b.PaintTorus(Body, V(0, 0.27f, z), 0.16f * k, 0.015f, light, V(90f, 0, 0), 1f, 0.75f);
        }
        foreach (float z in new[] { 0.04f, -0.07f, -0.18f })
            PokeBuilder.Both(s => b.Spike(Body, V(0.06f * s, 0.37f, z), V(0.11f * s, 0.47f, z - 0.04f), 0.038f, plum));
        int tail = b.Tail(V(0, 0.27f, -0.26f));
        b.Limb(tail, V(0, 0.27f, -0.26f), V(0, 0.23f, -0.35f), 0.06f, 0.035f, mauve);
        b.Spike(tail, V(0, 0.23f, -0.35f), V(0, 0.21f, -0.43f), 0.03f, plum);
        // Thick arms that arch up off its back and bend forward to great hooked claws, spined along the way
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.34f, -0.02f));
            var arc = Smooth(4, V(0.11f * s, 0.34f, -0.02f), V(0.22f * s, 0.48f, -0.07f), V(0.31f * s, 0.5f, 0.05f), V(0.32f * s, 0.38f, 0.17f));
            b.Tube(arm, arc, 0.06f, 0.05f, mauve);
            foreach (int k in new[] { 4, 7, 10 })
            {
                var at = arc[k];
                var up = Vector3.Normalize(V(0.4f * s, 1f, -0.2f));
                b.Spike(arm, at + up * 0.03f, at + up * 0.11f, 0.035f, plum);
            }
            Pincer(b, arm, V(0.31f * s, 0.34f, 0.22f), 0.075f, plum, V(-0.03f * s, 0.07f, 0.08f));
            b.Mark(arm, V(0.31f * s, 0.34f, 0.293f), V(0.2f * s, 0, 1f), 0.024f, 0.017f, blue);
        });

        int head = b.Head(V(0, 0.33f, 0.15f));
        var c = V(0, 0.33f, 0.24f);
        var r = V(0.15f, 0.12f, 0.13f);
        b.Ell(head, c, r, mauve);
        // A broad head with a mouth full of fangs, blue eyes and white horns over them
        Gape(b, head, V(0, 0.29f, 0.34f), V(0.1f, 0.035f, 0.035f), Rgb(120, 30, 70), 0.04f);
        PokeBuilder.Both(s =>
        {
            b.Eye(head, On(c, r, 0.06f * s, 0.38f), V(0.45f * s, 0.35f, 1f), 0.026f, pupil: Rgb(22, 62, 120), white: blue, glare: true);
            b.Spike(head, V(0.09f * s, 0.42f, 0.28f), V(0.14f * s, 0.5f, 0.32f), 0.02f, Claw, mat: Shell);
            b.Spike(head, V(0.11f * s, 0.27f, 0.32f), V(0.14f * s, 0.21f, 0.38f), 0.022f, Claw, mat: Shell);
        });
        return b;
    }

    // ------------------------------------------------------------------ Croagunk line

    private static PokeBuilder Croagunk()
    {
        var b = new PokeBuilder("Croagunk", 0.64f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Scales };
        var blue = Rgb(64, 112, 204);
        var black = Rgb(40, 40, 52);
        var orange = Rgb(244, 140, 52);
        var yellow = Rgb(250, 214, 60);

        // It squats on bent legs with black feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.14f, -0.02f));
            b.Limb(leg, V(0.08f * s, 0.14f, -0.02f), V(0.14f * s, 0.11f, 0.06f), 0.045f, 0.04f, blue);
            b.Limb(leg, V(0.14f * s, 0.11f, 0.06f), V(0.12f * s, 0.035f, 0.04f), 0.035f, 0.03f, black);
            b.Ell(leg, V(0.12f * s, 0.022f, 0.08f), V(0.04f, 0.022f, 0.06f), black);
        });
        // A round hunched body with a white band across the chest and a black line beneath it
        b.Ell(Body, V(0, 0.25f, 0), V(0.13f, 0.14f, 0.12f), blue);
        b.PaintEll(Body, V(0, 0.3f, 0.07f), V(0.13f, 0.032f, 0.09f), White);
        b.PaintEll(Body, V(0, 0.263f, 0.08f), V(0.12f, 0.011f, 0.08f), black, soft: 0.008f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.29f, 0.03f));
            b.Limb(arm, V(0.11f * s, 0.29f, 0.03f), V(0.17f * s, 0.21f, 0.07f), 0.032f, 0.028f, blue);
            b.Limb(arm, V(0.17f * s, 0.21f, 0.07f), V(0.15f * s, 0.13f, 0.12f), 0.03f, 0.026f, black);
            b.PaintEll(arm, V(0.17f * s, 0.21f, 0.07f), V(0.032f, 0.016f, 0.032f), orange, Euler(V(0.1f * s, -1f, 0.3f)));
            Digits(b, arm, V(0.15f * s, 0.12f, 0.125f), V(0, -0.6f, 1f), V(0.5f, 0, 0), 0.03f, 0.011f, black);
        });

        int head = b.Head(V(0, 0.34f, 0.02f));
        var c = V(0, 0.4f, 0.04f);
        var r = V(0.13f, 0.1f, 0.11f);
        b.Ell(head, c, r, blue);
        // Orange poison sacs on its cheeks, heavy-lidded yellow eyes and a wide mouth
        PokeBuilder.Both(s => b.Ell(head, V(0.115f * s, 0.355f, 0.07f), V(0.045f, 0.045f, 0.04f), orange));
        b.Mark(head, On(c, r, 0, 0.355f), V(0, -0.2f, 1f), 0.06f, 0.006f, black, MarkShape.Bar);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.43f), V(0.4f * s, 0.2f, 1f), 0.026f, pupil: black, white: yellow, glare: true));
        return b;
    }

    private static PokeBuilder Toxicroak()
    {
        var b = new PokeBuilder("Toxicroak", 0.92f, BodyPlan.Biped, V(0, 0.52f, 0)) { Coat = Scales };
        var blue = Rgb(64, 132, 214);
        var black = Rgb(36, 40, 56);
        var red = Rgb(232, 48, 86);
        var yellow = Rgb(250, 214, 60);

        // Long legs bent at the knee, banded in black, standing on three long toes
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.42f, -0.02f));
            b.Limb(leg, V(0.07f * s, 0.42f, -0.02f), V(0.13f * s, 0.25f, 0.06f), 0.05f, 0.04f, blue);
            b.Limb(leg, V(0.13f * s, 0.25f, 0.06f), V(0.12f * s, 0.05f, -0.01f), 0.038f, 0.03f, blue);
            b.PaintEll(leg, V(0.13f * s, 0.25f, 0.06f), V(0.045f, 0.015f, 0.045f), black, Euler(V(0.2f * s, 0.6f, 0.4f)));
            Digits(b, leg, V(0.12f * s, 0.025f, 0.0f), V(0, -0.1f, 1f), V(0.55f, 0, 0), 0.07f, 0.016f, blue);
        });
        b.Ell(Body, V(0, 0.55f, 0), V(0.12f, 0.16f, 0.1f), blue);
        b.PaintEll(Body, V(0, 0.45f, 0.06f), V(0.08f, 0.06f, 0.06f), White);
        b.PaintTorus(Body, V(0, 0.5f, 0), 0.115f, 0.012f, black, default, 1f, 0.85f);
        // The red poison sac swelling at its throat
        b.Ell(Body, V(0, 0.6f, 0.08f), V(0.085f, 0.085f, 0.06f), red, mat: Shell);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.64f, 0.0f));
            b.Limb(arm, V(0.1f * s, 0.64f, 0.0f), V(0.21f * s, 0.56f, 0.06f), 0.038f, 0.032f, blue);
            b.Limb(arm, V(0.21f * s, 0.56f, 0.06f), V(0.24f * s, 0.44f, 0.14f), 0.032f, 0.028f, blue);
            b.PaintEll(arm, V(0.21f * s, 0.56f, 0.06f), V(0.04f, 0.04f, 0.014f), black, Euler(V(0.9f * s, -0.6f, 0.3f)));
            Digits(b, arm, V(0.245f * s, 0.425f, 0.15f), V(0.2f * s, -1f, 0.5f), V(0.5f, 0, 0), 0.04f, 0.013f, black);
            // The poison claw on the back of each hand
            b.Tube(arm, Smooth(3, V(0.25f * s, 0.46f, 0.12f), V(0.31f * s, 0.46f, 0.1f), V(0.34f * s, 0.41f, 0.12f)), 0.016f, 0.005f, red, Shell);
        });

        int head = b.Head(V(0, 0.7f, 0.02f));
        var c = V(0, 0.77f, 0.05f);
        var r = V(0.115f, 0.09f, 0.125f);
        b.Ell(head, c, r, blue);
        // A crest swept back off its head, a wide frog's mouth with black lines running back from it, fierce yellow eyes
        b.Spike(head, V(0, 0.82f, -0.01f), V(0, 0.93f, -0.16f), 0.05f, blue, 0.45f);
        b.Mark(head, On(c, r, 0, 0.73f), V(0, -0.2f, 1f), 0.065f, 0.006f, black, MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            var cheek = On(c, r, 0.085f * s, 0.745f);
            b.Mark(head, cheek, Outward(c, r, cheek), 0.03f, 0.005f, black, MarkShape.Bar, -12f * s);
            b.Eye(head, On(c, r, 0.052f * s, 0.8f), V(0.45f * s, 0.2f, 1f), 0.024f, pupil: black, white: yellow, glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Carnivine

    private static PokeBuilder Carnivine()
    {
        var b = new PokeBuilder("Carnivine", 0.86f, BodyPlan.Floating, V(0, 0.62f, 0)) { Coat = Leaf }.Hover();
        var green = Rgb(144, 204, 82);
        var light = Rgb(196, 232, 124);
        var leaf = Rgb(60, 150, 70);
        var pink = Rgb(232, 104, 146);
        var yellow = Rgb(220, 206, 80);

        // A flytrap of a head: a ring of fangs round its mouth, little spines round its crown
        var c = V(0, 0.62f, 0);
        var r = V(0.17f, 0.14f, 0.15f);
        b.Ell(Body, c, r, green);
        b.PaintEll(Body, V(0, 0.73f, 0), V(0.16f, 0.05f, 0.14f), light);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f + 0.3f;
            var at = V(MathF.Sin(a) * 0.13f, 0.72f, MathF.Cos(a) * 0.115f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.03f, 0.04f, MathF.Cos(a) * 0.03f), 0.016f, light);
        }
        b.Cut(Body, V(0, 0.585f, 0.16f), V(0.14f, 0.03f, 0.09f), blend: 0.01f);
        b.PaintEll(Body, V(0, 0.585f, 0.12f), V(0.15f, 0.04f, 0.1f), pink);
        for (int i = -3; i <= 3; i++)
        {
            float x = i * 0.034f, z = 0.135f - MathF.Abs(i) * 0.012f;
            b.Spike(Body, V(x, 0.612f, z), V(x, 0.587f, z + 0.006f), 0.01f, White, mat: Shell, blend: 0.003f);
            b.Spike(Body, V(x + 0.017f, 0.558f, z), V(x + 0.017f, 0.583f, z + 0.006f), 0.01f, White, mat: Shell, blend: 0.003f);
        }
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.06f * s, 0.67f), V(0.35f * s, 0.15f, 1f), 0.024f, sclera: true, pupil: Rgb(30, 36, 30)));
        // A yellow collar, a leaf for each arm, and a tangle of vines hanging below
        b.Torus(Body, V(0, 0.47f, 0), 0.045f, 0.024f, yellow, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.04f * s, 0.46f, 0));
            b.Tube(arm, new[] { V(0.04f * s, 0.46f, 0), V(0.11f * s, 0.45f, 0.02f) }, 0.016f, 0.014f, leaf);
            var root = V(0.11f * s, 0.45f, 0.02f);
            var tip = V(0.38f * s, 0.53f, 0.0f);
            Blade(b, arm, root, tip, 0.085f, leaf, V(0, 1f, 0.3f), 0.2f, Leaf);
            for (int k = 1; k <= 3; k++)
            {
                var at = Vector3.Lerp(root, tip, 0.22f * k) + V(0, 0.062f - 0.016f * k, 0);
                Blade(b, arm, at - V(0.012f * s, 0.012f, 0), at + V(0.035f * s, 0.035f, 0), 0.022f, leaf, V(0, 0.3f, 1f), 0.4f, Leaf);
            }
        });
        int vines = b.Part("vines", Body, V(0, 0.45f, 0), PokeRole.Leaf);
        foreach (var (x, z, bend, color) in new[] { (-0.04f, 0.02f, -1f, leaf), (0.03f, 0.03f, 1f, leaf), (0.0f, -0.03f, 0.4f, green), (-0.03f, -0.02f, -0.6f, pink), (0.04f, -0.01f, 0.8f, pink) })
            b.Tube(vines, Smooth(3, V(x * 0.5f, 0.45f, z * 0.5f), V(x, 0.36f, z), V(x + 0.04f * bend, 0.27f, z + 0.02f), V(x + 0.01f * bend, 0.2f, z), V(x + 0.05f * bend, 0.14f, z - 0.01f)), 0.016f, 0.008f, color);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Remoraid line

    private static PokeBuilder Remoraid()
    {
        var b = new PokeBuilder("Remoraid", 0.58f, BodyPlan.Fish, V(0, 0.4f, 0)) { Coat = Scales }.Hover();
        var pale = Rgb(190, 214, 204);
        var dark = Rgb(96, 142, 128);
        var fin = Rgb(226, 238, 232);

        b.Ell(Body, V(0, 0.4f, -0.03f), V(0.095f, 0.11f, 0.19f), pale);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.07f * s, 0.46f, 0.0f), V(0.05f, 0.026f, 0.08f), dark);
            b.PaintEll(Body, V(0.075f * s, 0.43f, -0.13f), V(0.04f, 0.022f, 0.05f), dark);
        });
        // A tall fin swept back over it, a forked tail, a mouth like the muzzle of a water gun
        int dorsal = b.Part("dorsal", Body, V(0, 0.5f, -0.02f), PokeRole.Fin);
        Blade(b, dorsal, V(0, 0.47f, 0.0f), V(0, 0.68f, -0.17f), 0.075f, fin, V(1f, 0, 0));
        int tail = b.Tail(V(0, 0.4f, -0.2f));
        b.Limb(tail, V(0, 0.4f, -0.2f), V(0, 0.4f, -0.27f), 0.05f, 0.035f, pale);
        int tailFin = b.Part("tailFin", tail, V(0, 0.4f, -0.27f), PokeRole.Fin);
        Blade(b, tailFin, V(0, 0.41f, -0.25f), V(0, 0.55f, -0.41f), 0.055f, fin, V(1f, 0, 0));
        Blade(b, tailFin, V(0, 0.39f, -0.25f), V(0, 0.25f, -0.41f), 0.055f, fin, V(1f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.08f * s, 0.34f, 0.04f), PokeRole.Fin, s, s);
            Blade(b, side, V(0.07f * s, 0.34f, 0.04f), V(0.17f * s, 0.27f, -0.06f), 0.04f, fin, V(0.3f * s, 1f, 0.2f));
        });

        int head = b.Head(V(0, 0.4f, 0.1f));
        var c = V(0, 0.4f, 0.12f);
        var r = V(0.088f, 0.095f, 0.1f);
        b.Ell(head, c, r, pale);
        b.Torus(head, V(0, 0.38f, 0.22f), 0.026f, 0.013f, pale, V(90f, 0, 0), blend: 0.008f);
        b.Mark(head, V(0, 0.38f, 0.225f), V(0, 0, 1f), 0.018f, 0.018f, Rgb(60, 80, 76));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.06f * s, 0.43f), V(0.8f * s, 0.1f, 0.6f), 0.028f, sclera: true, pupil: Rgb(30, 36, 36)));
        return Lift(b);
    }

    private static PokeBuilder Octillery()
    {
        var b = new PokeBuilder("Octillery", 0.76f, BodyPlan.Floating, V(0, 0.34f, 0)) { Coat = Scales };
        var red = Rgb(228, 56, 50);
        var yellow = Rgb(250, 208, 62);

        // An octopus with a gun-barrel snout, standing on tentacles that curl out round it, yellow suckers at their tips
        var c = V(0, 0.36f, -0.02f);
        var r = V(0.16f, 0.19f, 0.16f);
        b.Ell(Body, c, r, red);
        var muzzle = V(0, 0.57f, 0.21f);
        var aim = Vector3.Normalize(V(0, 0.07f, 0.04f));
        b.Tube(Body, new[] { V(0, 0.42f, 0.1f), V(0, 0.5f, 0.17f), muzzle }, 0.055f, 0.048f, red);
        b.Torus(Body, muzzle + aim * 0.04f, 0.03f, 0.014f, yellow, Euler(aim), blend: 0.006f);
        b.Mark(Body, muzzle + aim * 0.048f, aim, 0.022f, 0.022f, Rgb(70, 20, 20));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.085f * s, 0.4f), V(0.5f * s, 0.1f, 1f), 0.034f, sclera: true, pupil: Rgb(40, 20, 20), white: Rgb(250, 240, 214), glare: true));
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + MathF.PI / 8f;
            var out1 = V(MathF.Sin(a), 0, MathF.Cos(a));
            int arm = b.Part("tentacle" + i, Body, V(0, 0.2f, -0.02f) + out1 * 0.08f, PokeRole.Leaf, i * 0.7f);
            var root = V(0, 0.22f, -0.02f) + out1 * 0.08f;
            var path = Smooth(3, root, V(0, 0.08f, -0.02f) + out1 * 0.17f, V(0, 0.035f, -0.02f) + out1 * 0.27f, V(0, 0.07f, -0.02f) + out1 * 0.34f, V(0, 0.11f, -0.02f) + out1 * 0.31f);
            b.Tube(arm, path, 0.05f, 0.022f, red);
            foreach (float t in new[] { 0.62f, 0.8f })
            {
                var at = path[(int)(t * (path.Length - 1))];
                b.Ell(arm, at + out1 * 0.022f + V(0, -0.012f, 0), V(0.018f, 0.018f, 0.018f), yellow, blend: 0.006f);
            }
        }
        return b;
    }

    // ------------------------------------------------------------------ Finneon line

    private static PokeBuilder Finneon()
    {
        var b = new PokeBuilder("Finneon", 0.5f, BodyPlan.Fish, V(0, 0.36f, 0)) { Coat = Scales }.Hover();
        var sky = Rgb(150, 210, 244);
        var navy = Rgb(28, 54, 96);
        var pink = Rgb(240, 110, 166);

        b.Ell(Body, V(0, 0.36f, -0.02f), V(0.07f, 0.09f, 0.14f), sky);
        b.PaintEll(Body, V(0, 0.43f, -0.02f), V(0.08f, 0.055f, 0.15f), navy);
        b.PaintEll(Body, V(0, 0.375f, 0.0f), V(0.1f, 0.009f, 0.16f), pink, soft: 0.008f);
        int dorsal = b.Part("dorsal", Body, V(0, 0.44f, -0.04f), PokeRole.Fin);
        RimmedFin(b, dorsal, V(0, 0.47f, -0.09f), V(0.01f, 0.05f, 0.05f), V(-50f, 0, 0), navy, navy, 0.01f);
        // A tail like a butterfly's wings, a pink eye-spot on each
        int tail = b.Tail(V(0, 0.36f, -0.14f));
        b.Limb(tail, V(0, 0.36f, -0.14f), V(0, 0.36f, -0.21f), 0.035f, 0.025f, sky);
        int tailFin = b.Part("tailFin", tail, V(0, 0.36f, -0.21f), PokeRole.Fin);
        b.Ell(tailFin, V(0, 0.44f, -0.26f), V(0.012f, 0.09f, 0.07f), sky, V(-35f, 0, 0), blend: 0.01f);
        b.Ell(tailFin, V(0, 0.29f, -0.25f), V(0.012f, 0.07f, 0.055f), sky, V(35f, 0, 0), blend: 0.01f);
        b.PaintEll(tailFin, V(0, 0.46f, -0.27f), V(0.02f, 0.045f, 0.035f), pink, V(-35f, 0, 0));
        b.PaintEll(tailFin, V(0, 0.28f, -0.26f), V(0.02f, 0.035f, 0.028f), pink, V(35f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.06f * s, 0.32f, 0.04f), PokeRole.Fin, s, s);
            b.Ell(side, V(0.09f * s, 0.3f, 0.0f), V(0.01f, 0.035f, 0.05f), sky, V(30f, 30f * s, 0));
        });

        int head = b.Head(V(0, 0.36f, 0.08f));
        var c = V(0, 0.36f, 0.08f);
        var r = V(0.066f, 0.085f, 0.075f);
        b.Ell(head, c, r, sky);
        b.PaintEll(head, V(0, 0.425f, 0.08f), V(0.07f, 0.05f, 0.08f), navy);
        b.PaintEll(head, V(0, 0.375f, 0.1f), V(0.08f, 0.009f, 0.08f), pink, soft: 0.008f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.375f), V(0.8f * s, 0.1f, 0.6f), 0.02f, pupil: Rgb(70, 30, 50), white: pink));
        return Lift(b);
    }

    private static PokeBuilder Lumineon()
    {
        var b = new PokeBuilder("Lumineon", 0.8f, BodyPlan.Fish, V(0, 0.42f, 0)) { Coat = Scales }.Hover();
        var sky = Rgb(150, 210, 244);
        var navy = Rgb(28, 50, 90);
        var pink = Rgb(240, 110, 166);

        // A tall, flat body, dark over the top in wavy patches, two great fins spread like a butterfly's wings
        b.Ell(Body, V(0, 0.42f, -0.02f), V(0.065f, 0.16f, 0.17f), sky);
        b.PaintEll(Body, V(0, 0.52f, -0.04f), V(0.07f, 0.08f, 0.15f), navy);
        foreach (var (y, z) in new[] { (0.47f, 0.06f), (0.45f, -0.06f), (0.46f, -0.15f) })
            b.PaintEll(Body, V(0, y, z), V(0.07f, 0.04f, 0.035f), navy);
        PokeBuilder.Both(s =>
        {
            int wing = b.Part(s < 0 ? "finL" : "finR", Body, V(0.05f * s, 0.5f, -0.02f), PokeRole.Fin, s, s);
            RimmedFin(b, wing, V(0.17f * s, 0.6f, -0.09f), V(0.012f, 0.16f, 0.09f), V(-30f, 0, -55f * s), navy, sky, 0.024f);
            int lower = b.Part(s < 0 ? "fin2L" : "fin2R", Body, V(0.05f * s, 0.34f, 0.0f), PokeRole.Fin, s, s);
            RimmedFin(b, lower, V(0.075f * s, 0.29f, -0.04f), V(0.012f, 0.08f, 0.06f), V(30f, 0, 25f * s), navy, sky, 0.018f);
        });
        int tail = b.Tail(V(0, 0.42f, -0.18f));
        b.Limb(tail, V(0, 0.42f, -0.18f), V(0, 0.42f, -0.24f), 0.045f, 0.03f, sky);
        int tailFin = b.Part("tailFin", tail, V(0, 0.42f, -0.24f), PokeRole.Fin);
        b.Ell(tailFin, V(0, 0.5f, -0.32f), V(0.012f, 0.09f, 0.06f), sky, V(-35f, 0, 0), blend: 0.01f);
        b.Ell(tailFin, V(0, 0.34f, -0.32f), V(0.012f, 0.09f, 0.06f), sky, V(35f, 0, 0), blend: 0.01f);
        b.PaintEll(tailFin, V(0, 0.53f, -0.34f), V(0.02f, 0.03f, 0.025f), pink);
        b.PaintEll(tailFin, V(0, 0.31f, -0.34f), V(0.02f, 0.03f, 0.025f), pink);

        int head = b.Head(V(0, 0.42f, 0.1f));
        var c = V(0, 0.41f, 0.11f);
        var r = V(0.06f, 0.11f, 0.09f);
        b.Ell(head, c, r, sky);
        b.PaintEll(head, V(0, 0.5f, 0.08f), V(0.07f, 0.05f, 0.09f), navy);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 0.43f), V(0.8f * s, 0.1f, 0.6f), 0.022f, pupil: Rgb(70, 30, 50), white: pink, glare: true));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Tentacool line

    private static PokeBuilder Tentacool()
    {
        var b = new PokeBuilder("Tentacool", 0.68f, BodyPlan.Floating, V(0, 0.55f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(72, 172, 232);
        var red = Rgb(228, 36, 60);
        var grey = Rgb(132, 120, 110);

        // A clear blue bell with two red crystals, a skirt round its rim and two long stinging tentacles
        var c = V(0, 0.56f, -0.02f);
        var r = V(0.13f, 0.15f, 0.13f);
        b.Ell(Body, c, r, blue);
        b.Ell(Body, V(0, 0.43f, -0.02f), V(0.15f, 0.035f, 0.15f), blue);
        PokeBuilder.Both(s => b.Ell(Body, V(0.075f * s, 0.6f, 0.07f), V(0.055f, 0.06f, 0.05f), red, mat: Shell, blend: 0.01f));
        b.Spike(Body, V(0, 0.45f, 0.09f), V(0, 0.37f, 0.12f), 0.03f, Rgb(170, 220, 244), mat: Shell);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.05f * s, 0.49f), V(0.35f * s, 0, 1f), 0.017f, glare: true));
        PokeBuilder.Both(s =>
        {
            int arm = b.Part(s < 0 ? "tentacleL" : "tentacleR", Body, V(0.06f * s, 0.42f, 0.0f), PokeRole.Leaf, s, s);
            b.Tube(arm, Smooth(3, V(0.06f * s, 0.42f, 0.0f), V(0.13f * s, 0.3f, 0.05f), V(0.1f * s, 0.18f, 0.02f), V(0.17f * s, 0.07f, -0.02f)), 0.018f, 0.011f, grey);
        });
        return Lift(b);
    }

    private static PokeBuilder Tentacruel()
    {
        var b = new PokeBuilder("Tentacruel", 0.96f, BodyPlan.Floating, V(0, 0.66f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(72, 172, 232);
        var red = Rgb(228, 36, 60);
        var grey = Rgb(132, 120, 110);
        var pale = Rgb(170, 220, 244);

        // A wide bell with three red crystals, two pale beaks hanging in front and a mass of tentacles
        var c = V(0, 0.68f, -0.02f);
        var r = V(0.2f, 0.14f, 0.17f);
        b.Ell(Body, c, r, blue);
        b.Ell(Body, V(0, 0.57f, -0.02f), V(0.24f, 0.04f, 0.2f), blue);
        PokeBuilder.Both(s => b.Ell(Body, V(0.11f * s, 0.72f, 0.07f), V(0.075f, 0.065f, 0.065f), red, mat: Shell, blend: 0.01f));
        b.Ell(Body, V(0, 0.8f, 0.05f), V(0.03f, 0.03f, 0.03f), red, mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            b.Eye(Body, On(c, r, 0.07f * s, 0.6f), V(0.3f * s, 0, 1f), 0.022f, glare: true);
            b.Spike(Body, V(0.03f * s, 0.57f, 0.13f), V(0.035f * s, 0.36f, 0.16f), 0.032f, pale, 0.6f, Shell);
        });
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f + 0.31f;
            var out1 = V(MathF.Sin(a), 0, MathF.Cos(a));
            int arm = b.Part("tentacle" + i, Body, V(0, 0.55f, -0.02f) + out1 * 0.12f, PokeRole.Leaf, i * 0.6f);
            var root = V(0, 0.55f, -0.02f) + out1 * 0.12f;
            float sag = 0.04f * (i % 3);
            b.Tube(arm, Smooth(3, root, root + out1 * 0.08f + V(0, -0.14f, 0), root + out1 * 0.18f + V(0, -0.26f - sag, 0), root + out1 * 0.3f + V(0, -0.34f - sag, 0)), 0.02f, 0.013f, grey);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Feebas line

    private static PokeBuilder Feebas()
    {
        var b = new PokeBuilder("Feebas", 0.56f, BodyPlan.Fish, V(0, 0.36f, 0)) { Coat = Scales }.Hover();
        var tan = Rgb(200, 172, 118);
        var brown = Rgb(150, 116, 74);
        var fin = Rgb(176, 206, 236);
        var pink = Rgb(236, 126, 150);

        // A drab fish, mottled brown, with ragged pale fins
        b.Ell(Body, V(0, 0.36f, -0.03f), V(0.1f, 0.14f, 0.14f), tan);
        foreach (var (x, y, z) in new[] { (0.07f, 0.44f, -0.06f), (0.09f, 0.33f, -0.1f), (0.06f, 0.26f, 0.0f), (0.08f, 0.4f, 0.04f) })
            PokeBuilder.Both(s => b.PaintEll(Body, V(x * s, y, z), V(0.04f, 0.03f, 0.035f), brown));
        int dorsal = b.Part("dorsal", Body, V(0, 0.48f, -0.04f), PokeRole.Fin);
        PokeBuilder.Both(s => b.Tube(dorsal, new[] { V(0.012f * s, 0.47f, -0.04f), V(0.022f * s, 0.56f, -0.05f), V(0.03f * s, 0.64f, -0.04f) }, 0.012f, 0.009f, fin));
        int tail = b.Tail(V(0, 0.36f, -0.15f));
        b.Limb(tail, V(0, 0.36f, -0.15f), V(0, 0.36f, -0.2f), 0.045f, 0.03f, tan);
        int tailFin = b.Part("tailFin", tail, V(0, 0.36f, -0.2f), PokeRole.Fin);
        foreach (float a in new[] { -50f, -25f, 0f, 25f, 50f })
        {
            var d = V(0, MathF.Sin(a * Degree), -MathF.Cos(a * Degree));
            b.Ell(tailFin, V(0, 0.36f, -0.2f) + d * 0.075f, V(0.01f, 0.075f, 0.022f), fin, Euler(d), blend: 0.008f);
        }
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.08f * s, 0.28f, 0.04f), PokeRole.Fin, s, s);
            foreach (float a in new[] { -20f, 15f })
            {
                var d = Vector3.Normalize(V(0.7f * s, MathF.Sin(a * Degree) - 0.6f, 0.2f));
                b.Ell(side, V(0.08f * s, 0.28f, 0.04f) + d * 0.05f, V(0.05f, 0.012f, 0.025f), fin, V(0, 0, MathF.Atan2(d.Y, d.X) / Degree), blend: 0.008f);
            }
        });

        int head = b.Head(V(0, 0.36f, 0.06f));
        var c = V(0, 0.36f, 0.06f);
        var r = V(0.095f, 0.13f, 0.1f);
        b.Ell(head, c, r, tan);
        // Big blank eyes on either side and thick pink lips
        b.Ell(head, V(0, 0.3f, 0.155f), V(0.045f, 0.024f, 0.028f), pink, mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.07f * s, 0.38f), V(0.9f * s, 0.05f, 0.45f), 0.04f, sclera: true, pupil: Rgb(30, 30, 30)));
        return Lift(b);
    }

    private static PokeBuilder Milotic()
    {
        var b = new PokeBuilder("Milotic", 1f, BodyPlan.Serpent, V(0.05f, 0.1f, -0.28f)) { Coat = Scales };
        var cream = Rgb(246, 232, 196);
        var blue = Rgb(66, 156, 226);
        var navy = Rgb(30, 60, 120);
        var pink = Rgb(238, 98, 128);

        // A long body coiled on the ground and rising into a slender neck; the tail end is blue, scaled in dark lines
        var points = new[] { V(0.24f, 0.08f, -0.28f), V(0.04f, 0.09f, -0.33f), V(-0.17f, 0.1f, -0.16f), V(-0.12f, 0.11f, 0.08f), V(0.09f, 0.13f, 0.12f), V(0.13f, 0.28f, 0.02f), V(0.05f, 0.47f, -0.02f), V(0, 0.62f, 0.02f), V(0, 0.72f, 0.06f) };
        float Radius(int i) => i switch { 0 => 0.05f, 1 => 0.065f, 2 => 0.08f, 3 => 0.09f, 4 => 0.09f, 5 => 0.08f, 6 => 0.065f, 7 => 0.055f, _ => 0.05f };
        int parent = Body;
        for (int i = 1; i < points.Length - 1; i++)
        {
            int seg = b.Part("seg" + i, parent, points[i], PokeRole.Segment, i);
            b.Limb(seg, points[i], points[i + 1], Radius(i), Radius(i + 1), i < 3 ? blue : cream);
            if (i < 3)
            {
                var mid = (points[i] + points[i + 1]) / 2f;
                var along = Vector3.Normalize(points[i + 1] - points[i]);
                for (int k = -1; k <= 1; k++)
                    b.PaintTorus(seg, mid + along * (k * 0.06f), Radius(i) * 1.02f, 0.006f, navy, Euler(along));
            }
            parent = seg;
        }
        b.Limb(Body, points[0], points[1], Radius(0), Radius(1), blue);
        // A fan of a tail fin, blue and pink like a peacock's
        int tail = b.Tail(points[0]);
        b.Ell(tail, points[0] + V(0.02f, 0, 0.01f), V(0.048f, 0.048f, 0.048f), blue);
        int fan = b.Part("tailFin", tail, points[0], PokeRole.Fin);
        foreach (float a in new[] { -50f, -25f, 0f, 25f, 50f, 75f, 100f })
        {
            var d = V(MathF.Sin(a * Degree), MathF.Cos(a * Degree), 0);
            var mid = points[0] + d * 0.11f;
            b.Ell(fan, mid, V(0.038f, 0.11f, 0.012f), blue, Euler(d), blend: 0.008f);
            b.PaintEll(fan, mid + d * 0.03f, V(0.02f, 0.055f, 0.02f), pink, Euler(d));
        }

        int head = b.Head(points[^1], parent);
        var c = V(0, 0.77f, 0.08f);
        var r = V(0.075f, 0.068f, 0.1f);
        b.Ell(head, c, r, cream);
        // A pointed crest, long red brows that curl up and back, and long pink fins flowing down like hair
        b.Spike(head, V(0, 0.81f, 0.05f), V(0, 0.98f, -0.06f), 0.03f, cream, 0.5f);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(4, V(0.03f * s, 0.81f, 0.12f), V(0.06f * s, 0.9f, 0.1f), V(0.1f * s, 0.95f, 0.02f), V(0.14f * s, 0.9f, -0.04f)), 0.006f, 0.004f, Rgb(214, 54, 78));
            var root = V(0.045f * s, 0.79f, 0.04f);
            Petal(b, head, root, V(0.13f * s, 0.46f, -0.04f), 0.045f, pink, Fur, 0.01f);
            Petal(b, head, V(0.045f * s, 0.78f, 0.01f), V(0.11f * s, 0.52f, -0.12f), 0.038f, pink, Fur, 0.01f);
            b.Eye(head, On(c, r, 0.04f * s, 0.785f), V(0.6f * s, 0.1f, 0.8f), 0.02f, Rgb(214, 54, 78));
        });
        return b;
    }

    // ------------------------------------------------------------------ Mantyke line

    private static PokeBuilder Mantyke()
    {
        var b = new PokeBuilder("Mantyke", 0.56f, BodyPlan.Fish, V(0, 0.32f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(72, 160, 232);
        var pale = Rgb(206, 232, 250);

        // A small round ray, blue above and pale below, a face on its front and two feelers arched over it
        var c = V(0, 0.32f, 0);
        var r = V(0.15f, 0.085f, 0.14f);
        b.Ell(Body, c, r, blue);
        b.PaintEll(Body, V(0, 0.26f, 0.02f), V(0.16f, 0.04f, 0.15f), pale);
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, V(0.12f * s, 0.32f, -0.01f), PokeRole.Fin, s, s);
            b.Ell(fin, V(0.21f * s, 0.33f, -0.04f), V(0.1f, 0.025f, 0.075f), blue, V(0, 25f * s, -8f * s));
            b.PaintEll(fin, V(0.21f * s, 0.315f, -0.04f), V(0.1f, 0.015f, 0.075f), pale, V(0, 25f * s, -8f * s));
        });
        int tail = b.Tail(V(0, 0.31f, -0.13f));
        b.Tube(tail, new[] { V(0, 0.31f, -0.13f), V(0, 0.3f, -0.21f), V(0, 0.31f, -0.27f) }, 0.022f, 0.012f, blue);
        int head = b.Head(V(0, 0.36f, 0.06f));
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.05f * s, 0.38f, 0.06f), V(0.07f * s, 0.48f, 0.04f), V(0.09f * s, 0.53f, -0.03f), V(0.11f * s, 0.5f, -0.1f)), 0.022f, 0.016f, blue));
        b.Mark(Body, On(c, r, 0, 0.295f), V(0, -0.2f, 1f), 0.028f, 0.014f, Rgb(150, 40, 70));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.06f * s, 0.335f), V(0.35f * s, 0.1f, 1f), 0.03f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Mantine

    private static PokeBuilder Mantine()
    {
        var b = new PokeBuilder("Mantine", 1f, BodyPlan.Fish, V(0, 0.36f, 0)) { Coat = Scales }.Hover();
        var navy = Rgb(40, 56, 100);
        var lilac = Rgb(216, 208, 240);
        var pale = Rgb(196, 224, 244);

        // A great ray, dark above with a white leading edge cut into teeth, lilac beneath
        var c = V(0, 0.36f, 0);
        var r = V(0.16f, 0.08f, 0.17f);
        b.Ell(Body, c, r, navy);
        b.PaintEll(Body, V(0, 0.3f, 0.03f), V(0.17f, 0.05f, 0.18f), lilac);
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, V(0.12f * s, 0.36f, 0), PokeRole.Fin, s, s);
            var turn = V(0, 18f * s, 8f * s);
            b.Ell(fin, V(0.27f * s, 0.38f, -0.04f), V(0.19f, 0.026f, 0.11f), navy, turn);
            b.PaintEll(fin, V(0.27f * s, 0.355f, -0.04f), V(0.19f, 0.015f, 0.11f), lilac, turn);
            b.PaintEll(fin, V(0.27f * s, 0.385f, 0.035f), V(0.18f, 0.04f, 0.045f), White, turn);
            for (int k = 0; k < 3; k++)
                b.PaintEll(fin, V((0.18f + 0.07f * k) * s, 0.39f, 0.03f - 0.028f * k), V(0.02f, 0.04f, 0.03f), navy, turn);
        });
        int tail = b.Tail(V(0, 0.34f, -0.16f));
        b.Tube(tail, Smooth(3, V(0, 0.34f, -0.16f), V(0, 0.32f, -0.3f), V(0.03f, 0.3f, -0.44f), V(0.08f, 0.31f, -0.54f)), 0.018f, 0.006f, pale);
        int head = b.Head(V(0, 0.38f, 0.1f));
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.07f * s, 0.38f, 0.14f), V(0.09f * s, 0.46f, 0.17f), V(0.11f * s, 0.52f, 0.12f)), 0.026f, 0.018f, navy));
        b.Mark(Body, On(c, r, 0, 0.315f), V(0, -0.3f, 1f), 0.05f, 0.012f, Rgb(60, 50, 90), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.065f * s, 0.34f), V(0.4f * s, 0, 1f), 0.022f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Snover line

    private static PokeBuilder Snover()
    {
        var b = new PokeBuilder("Snover", 0.68f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var teal = Rgb(66, 138, 132);
        var brown = Rgb(186, 136, 104);
        var bark = Rgb(128, 88, 66);
        var snow = Rgb(240, 244, 248);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.1f, 0.0f));
            b.Limb(leg, V(0.06f * s, 0.1f, 0.0f), V(0.08f * s, 0.035f, 0.02f), 0.045f, 0.04f, teal);
            b.Ell(leg, V(0.085f * s, 0.03f, 0.05f), V(0.045f, 0.03f, 0.06f), teal);
        });
        // A trunk of bark, a coat of snow over it with a ragged hem, and a cap of snow like a little pine
        var low = V(0, 0.19f, 0);
        var lowR = V(0.13f, 0.12f, 0.12f);
        b.Ell(Body, low, lowR, brown);
        b.Mark(Body, On(low, lowR, 0, 0.16f), V(0, -0.1f, 1f), 0.08f, 0.024f, bark, MarkShape.Zigzag);
        var c = V(0, 0.36f, 0);
        var r = V(0.15f, 0.1f, 0.13f);
        b.Ell(Body, c, r, snow);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var at = V(MathF.Sin(a) * 0.13f, 0.29f, MathF.Cos(a) * 0.115f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.025f, -0.055f, MathF.Cos(a) * 0.025f), 0.035f, snow);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.35f, 0.02f));
            b.Limb(arm, V(0.12f * s, 0.35f, 0.02f), V(0.22f * s, 0.4f, 0.06f), 0.042f, 0.038f, teal);
            Digits(b, arm, V(0.23f * s, 0.41f, 0.07f), V(0.6f * s, 0.6f, 0.3f), V(0, 0.6f, 0), 0.055f, 0.02f, teal);
        });
        int head = b.Head(V(0, 0.44f, 0));
        b.Spike(head, V(0, 0.5f, 0), V(0, 0.76f, -0.01f), 0.17f, snow);
        for (int i = 0; i < 11; i++)
        {
            float a = i * MathF.Tau / 11f + 0.3f;
            var at = V(MathF.Sin(a) * 0.16f, 0.5f, MathF.Cos(a) * 0.16f);
            b.Spike(head, at, at + V(MathF.Sin(a) * 0.025f, -0.06f, MathF.Cos(a) * 0.025f), 0.03f, snow);
        }
        // Eyes peeking out from under the cap
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.045f * s, 0.362f), V(0.35f * s, 0.05f, 1f), 0.02f, Rgb(110, 196, 196)));
        return b;
    }

    private static PokeBuilder Abomasnow()
    {
        var b = new PokeBuilder("Abomasnow", 1f, BodyPlan.Biped, V(0, 0.45f, 0)) { Coat = Fur };
        var teal = Rgb(66, 138, 132);
        var snow = Rgb(240, 244, 248);
        var purple = Rgb(176, 146, 214);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.16f, 0.0f));
            b.Limb(leg, V(0.1f * s, 0.16f, 0.0f), V(0.12f * s, 0.05f, 0.03f), 0.07f, 0.06f, teal);
            Digits(b, leg, V(0.12f * s, 0.03f, 0.06f), V(0, -0.2f, 1f), V(0.6f, 0, 0), 0.06f, 0.025f, teal);
        });
        // A great shaggy coat of snow, ragged at the hem and down the front
        var c = V(0, 0.45f, 0);
        var r = V(0.24f, 0.3f, 0.2f);
        b.Ell(Body, c, r, snow);
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f;
            var at = V(MathF.Sin(a) * 0.2f, 0.24f, MathF.Cos(a) * 0.165f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.03f, -0.09f - 0.025f * (i % 2), MathF.Cos(a) * 0.03f), 0.045f, snow);
        }
        foreach (var (x, y) in new[] { (-0.13f, 0.4f), (0.13f, 0.4f), (-0.07f, 0.32f), (0.07f, 0.32f) })
        {
            var at = On(c, r, x, y);
            b.Spike(Body, at - V(0, -0.02f, 0.02f), at + V(x * 0.15f, -0.07f, 0.02f), 0.035f, snow);
        }
        // Arms raised: white fur at the shoulder, branches of teal beyond with needles for fingers
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.2f * s, 0.6f, 0.0f));
            b.Limb(arm, V(0.2f * s, 0.6f, 0.0f), V(0.3f * s, 0.7f, 0.04f), 0.08f, 0.07f, snow);
            b.Limb(arm, V(0.3f * s, 0.7f, 0.04f), V(0.34f * s, 0.86f, 0.06f), 0.07f, 0.06f, teal);
            foreach (var tip in new[] { V(0.29f * s, 1.0f, 0.07f), V(0.37f * s, 1.0f, 0.06f), V(0.44f * s, 0.94f, 0.05f) })
                b.Spike(arm, V(0.34f * s, 0.87f, 0.06f), tip, 0.035f, teal);
        });
        int head = b.Head(V(0, 0.68f, 0.02f));
        for (int i = -2; i <= 2; i++)
            b.Spike(head, V(i * 0.05f, 0.7f, 0.0f), V(i * 0.08f, 0.83f - MathF.Abs(i) * 0.02f, -0.03f), 0.045f, snow);
        // Pale purple eyes under a frown of fur, and a mouth with fangs set in a shaggy beard
        Gape(b, Body, V(0, 0.52f, 0.19f), V(0.07f, 0.032f, 0.03f), Rgb(180, 60, 70), 0.025f);
        foreach (float x in new[] { -0.06f, 0f, 0.06f })
        {
            var at = On(c, r, x, 0.47f);
            b.Spike(Body, at - V(0, 0, 0.02f), at + V(x * 0.2f, -0.07f, 0.02f), 0.03f, snow);
        }
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.07f * s, 0.62f), V(0.35f * s, 0.1f, 1f), 0.024f, pupil: Rgb(90, 50, 130), white: purple, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Sneasel line

    private static PokeBuilder Sneasel()
    {
        var b = new PokeBuilder("Sneasel", 0.7f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Fur };
        var dark = Rgb(46, 66, 98);
        var red = Rgb(232, 52, 84);
        var gold = Rgb(244, 204, 64);

        // Lean legs that bend back at the heel, white claws on the toes
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.32f, -0.01f));
            b.Limb(leg, V(0.05f * s, 0.32f, -0.01f), V(0.08f * s, 0.19f, 0.05f), 0.04f, 0.03f, dark);
            b.Limb(leg, V(0.08f * s, 0.19f, 0.05f), V(0.08f * s, 0.06f, -0.02f), 0.028f, 0.024f, dark);
            b.Limb(leg, V(0.08f * s, 0.06f, -0.02f), V(0.085f * s, 0.025f, 0.05f), 0.024f, 0.02f, dark);
            Claws(b, leg, V(0.085f * s, 0.02f, 0.07f), V(0.018f, 0, 0), V(0, -0.3f, 1f), 0.03f, 0.009f);
        });
        b.Ell(Body, V(0, 0.42f, 0), V(0.075f, 0.12f, 0.065f), dark);
        b.Ell(Body, V(0, 0.46f, 0.06f), V(0.02f, 0.026f, 0.014f), gold, mat: Shell, blend: 0.006f);
        // Red feathers off its tail
        int tail = b.Tail(V(0, 0.33f, -0.05f));
        foreach (var tip in new[] { V(0.07f, 0.4f, -0.22f), V(0, 0.44f, -0.24f), V(-0.07f, 0.37f, -0.2f) })
            Blade(b, tail, V(0, 0.33f, -0.05f), tip, 0.03f, red, V(0, 1f, 0.3f), 0.35f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.5f, 0.02f));
            b.Limb(arm, V(0.07f * s, 0.5f, 0.02f), V(0.16f * s, 0.46f, 0.07f), 0.028f, 0.024f, dark);
            b.Limb(arm, V(0.16f * s, 0.46f, 0.07f), V(0.22f * s, 0.43f, 0.12f), 0.024f, 0.022f, dark);
            Claws(b, arm, V(0.235f * s, 0.43f, 0.14f), V(0, 0.018f, 0), V(0.4f * s, -0.4f, 1f), 0.05f, 0.011f);
        });

        int head = b.Head(V(0, 0.52f, 0.02f));
        var c = V(0, 0.61f, 0.03f);
        var r = V(0.1f, 0.085f, 0.09f);
        b.Ell(head, c, r, dark);
        // Pointed ears, a single long red feather off one of them, a gold gem on its brow and fierce red eyes
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.67f, 0.0f));
            b.Spike(ear, V(0.06f * s, 0.66f, 0.0f), V(0.11f * s, 0.77f, -0.03f), 0.04f, dark, 0.5f);
        });
        Blade(b, head, V(0.07f, 0.68f, -0.02f), V(0.16f, 0.9f, -0.06f), 0.04f, red, V(0, 0, 1f), 0.3f);
        b.Ell(head, V(0, 0.665f, 0.1f), V(0.016f, 0.02f, 0.012f), gold, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.6f), V(0.4f * s, 0.1f, 1f), 0.022f, Rgb(220, 50, 70), glare: true));
        return b;
    }

    private static PokeBuilder Weavile()
    {
        var b = new PokeBuilder("Weavile", 0.8f, BodyPlan.Biped, V(0, 0.44f, 0)) { Coat = Fur };
        var dark = Rgb(46, 66, 98);
        var red = Rgb(214, 40, 70);
        var gold = Rgb(244, 204, 64);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.055f * s, 0.36f, -0.01f));
            b.Limb(leg, V(0.055f * s, 0.36f, -0.01f), V(0.09f * s, 0.21f, 0.06f), 0.045f, 0.034f, dark);
            b.Limb(leg, V(0.09f * s, 0.21f, 0.06f), V(0.09f * s, 0.07f, -0.02f), 0.032f, 0.026f, dark);
            b.Limb(leg, V(0.09f * s, 0.07f, -0.02f), V(0.095f * s, 0.025f, 0.05f), 0.026f, 0.022f, dark);
            Claws(b, leg, V(0.095f * s, 0.02f, 0.07f), V(0.02f, 0, 0), V(0, -0.3f, 1f), 0.035f, 0.01f);
        });
        b.Ell(Body, V(0, 0.47f, 0), V(0.085f, 0.13f, 0.07f), dark);
        // Long red feathers off its tail
        int tail = b.Tail(V(0, 0.37f, -0.05f));
        foreach (var tip in new[] { V(0.08f, 0.44f, -0.28f), V(-0.06f, 0.4f, -0.27f) })
            Blade(b, tail, V(0, 0.37f, -0.05f), tip, 0.035f, red, V(0, 1f, 0.3f), 0.35f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.56f, 0.02f));
            b.Limb(arm, V(0.08f * s, 0.56f, 0.02f), V(0.18f * s, 0.5f, 0.07f), 0.032f, 0.027f, dark);
            b.Limb(arm, V(0.18f * s, 0.5f, 0.07f), V(0.25f * s, 0.46f, 0.13f), 0.027f, 0.024f, dark);
            Claws(b, arm, V(0.265f * s, 0.455f, 0.15f), V(0, 0.02f, 0), V(0.4f * s, -0.4f, 1f), 0.06f, 0.012f);
        });
        // A collar of red feathers
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            var at = V(MathF.Sin(a) * 0.065f, 0.585f, MathF.Cos(a) * 0.055f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.04f, -0.035f, MathF.Cos(a) * 0.04f), 0.026f, red, 0.5f);
        }

        int head = b.Head(V(0, 0.6f, 0.02f));
        var c = V(0, 0.68f, 0.03f);
        var r = V(0.1f, 0.085f, 0.09f);
        b.Ell(head, c, r, dark);
        // A crown of red feathers fanned out behind its head, a gold gem on its brow, fierce red eyes
        foreach (float a in new[] { -70f, -40f, -12f, 12f, 40f, 70f })
        {
            var d = Vector3.Normalize(V(MathF.Sin(a * Degree), MathF.Cos(a * Degree), -0.25f));
            var root = V(0, 0.7f, -0.05f);
            Blade(b, head, root, root + d * (0.22f - MathF.Abs(a) * 0.0008f), 0.05f, red, V(0, 0, 1f), 0.25f);
        }
        b.Ell(head, V(0, 0.735f, 0.1f), V(0.016f, 0.02f, 0.012f), gold, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.67f), V(0.4f * s, 0.1f, 1f), 0.022f, Rgb(220, 50, 70), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Rotom

    /// <summary>A lightning bolt flat to the front: <see cref="Bolt"/>'s zigzag, wide across and thin front to back.</summary>
    private static void FlatBolt(PokeBuilder b, int bone, Vector3[] corners, float[] widths, float thickness, Color color)
    {
        for (int i = 1; i < corners.Length; i++)
        {
            var d = corners[i] - corners[i - 1];
            float w = widths[Math.Min(widths.Length - 1, i - 1)];
            b.Box(bone, (corners[i - 1] + corners[i]) / 2f, V(w, d.Length() / 2f + w * 0.5f, thickness), Math.Min(thickness, w) * 0.6f, color, Euler(d), blend: 0.006f);
        }
    }

    private static PokeBuilder Rotom()
    {
        var b = new PokeBuilder("Rotom", 0.45f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Glow }.Hover();
        var orange = Rgb(244, 114, 42);
        var spark = Rgb(170, 230, 252);

        // A body of plasma drawn up into a point, and arms of lightning
        var c = V(0, 0.3f, 0);
        var r = V(0.1f, 0.1f, 0.09f);
        b.Ell(Body, c, r, orange);
        b.Spike(Body, V(-0.03f, 0.36f, -0.01f), V(-0.1f, 0.5f, -0.03f), 0.055f, orange);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.28f, 0));
            FlatBolt(b, arm, new[] { V(0.08f * s, 0.29f, 0), V(0.17f * s, 0.25f, 0), V(0.15f * s, 0.19f, 0), V(0.27f * s, 0.12f, 0) }, new[] { 0.016f, 0.02f, 0.026f }, 0.012f, spark);
        });
        b.Mark(Body, On(c, r, 0, 0.27f), V(0, -0.1f, 1f), 0.018f, 0.007f, Rgb(120, 40, 20), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.04f * s, 0.315f), V(0.35f * s, 0.05f, 1f), 0.026f, sclera: true, pupil: Rgb(40, 110, 220)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Gligar line

    private static PokeBuilder Gligar()
    {
        var b = new PokeBuilder("Gligar", 0.76f, BodyPlan.Bird, V(0, 0.38f, 0)) { Coat = Fur }.Hover();
        var pink = Rgb(222, 122, 222);
        var blue = Rgb(104, 152, 228);
        var tongue = Rgb(246, 150, 124);

        // A small body hanging under a big head, a tail curling down to a hooked tip
        b.Ell(Body, V(0, 0.38f, -0.02f), V(0.085f, 0.11f, 0.08f), pink);
        int tail = b.Tail(V(0, 0.3f, -0.06f));
        b.Tube(tail, Smooth(3, V(0, 0.3f, -0.06f), V(0, 0.2f, -0.12f), V(0.02f, 0.1f, -0.1f), V(0.04f, 0.05f, -0.02f)), 0.03f, 0.02f, pink);
        b.Spike(tail, V(0.04f, 0.05f, -0.02f), V(0.08f, 0.03f, 0.04f), 0.022f, pink);
        b.Spike(tail, V(0.04f, 0.05f, -0.02f), V(0.0f, 0.02f, 0.05f), 0.022f, pink);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.3f, 0.0f));
            b.Limb(leg, V(0.05f * s, 0.3f, 0.0f), V(0.09f * s, 0.21f, 0.04f), 0.03f, 0.025f, pink);
            b.Spike(leg, V(0.09f * s, 0.21f, 0.04f), V(0.12f * s, 0.15f, 0.07f), 0.02f, pink);
            b.Spike(leg, V(0.09f * s, 0.21f, 0.04f), V(0.07f * s, 0.15f, 0.08f), 0.02f, pink);
        });
        // Arms ending in crab's pincers, with blue skin stretched from them to its flanks
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.07f * s, 0.46f, 0.0f));
            b.Limb(w, V(0.07f * s, 0.46f, 0.0f), V(0.21f * s, 0.55f, 0.02f), 0.03f, 0.028f, pink);
            b.Ell(w, V(0.235f * s, 0.6f, 0.03f), V(0.05f, 0.05f, 0.04f), pink);
            b.Tube(w, Smooth(3, V(0.25f * s, 0.63f, 0.03f), V(0.29f * s, 0.69f, 0.03f), V(0.26f * s, 0.74f, 0.03f)), 0.022f, 0.008f, pink);
            b.Tube(w, Smooth(3, V(0.22f * s, 0.63f, 0.03f), V(0.19f * s, 0.69f, 0.03f), V(0.21f * s, 0.73f, 0.03f)), 0.02f, 0.008f, pink);
            b.Ell(w, V(0.15f * s, 0.42f, -0.03f), V(0.1f, 0.1f, 0.012f), blue, V(0, 0, 25f * s));
        });
        int head = b.Head(V(0, 0.5f, 0.02f));
        var c = V(0, 0.58f, 0.03f);
        var r = V(0.13f, 0.1f, 0.1f);
        b.Ell(head, c, r, pink);
        // Long pointed ears, big round eyes, fangs and a tongue lolling out
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, 0.65f, 0.0f));
            b.Spike(ear, V(0.07f * s, 0.64f, 0.0f), V(0.12f * s, 0.84f, -0.03f), 0.04f, pink, 0.5f);
        });
        Gape(b, head, V(0, 0.545f, 0.115f), V(0.06f, 0.025f, 0.025f), Rgb(160, 40, 60), 0.025f);
        b.Ell(head, V(0, 0.515f, 0.13f), V(0.035f, 0.03f, 0.018f), tongue, V(35f, 0, 0), blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.61f), V(0.35f * s, 0.1f, 1f), 0.032f, sclera: true, pupil: Rgb(30, 30, 30)));
        return Lift(b);
    }

    private static PokeBuilder Gliscor()
    {
        var b = new PokeBuilder("Gliscor", 0.96f, BodyPlan.Bird, V(0, 0.46f, 0)) { Coat = Fur }.Hover();
        var violet = Rgb(144, 134, 194);
        var dark = Rgb(58, 54, 66);
        var red = Rgb(214, 40, 56);
        var yellow = Rgb(250, 220, 60);

        b.Ell(Body, V(0, 0.46f, -0.02f), V(0.09f, 0.13f, 0.08f), violet);
        b.Spike(Body, V(0, 0.56f, 0.06f), V(0, 0.47f, 0.09f), 0.035f, red, 0.4f);
        // A long tail of segments that hooks up at the end into a pincer
        int tail = b.Tail(V(0, 0.36f, -0.06f));
        var curve = Smooth(4, V(0, 0.36f, -0.06f), V(0, 0.24f, -0.1f), V(0.02f, 0.12f, -0.07f), V(0.05f, 0.06f, 0.02f));
        for (int i = 0; i <= 8; i++)
        {
            float rr = 0.04f - 0.0022f * i;
            b.Ell(tail, curve[i * (curve.Length - 1) / 8], V(rr, rr * 0.9f, rr), violet, blend: 0.01f);
        }
        b.Tube(tail, Smooth(3, curve[^1], curve[^1] + V(0.05f, -0.01f, 0.04f), curve[^1] + V(0.06f, 0.04f, 0.07f)), 0.022f, 0.006f, violet);
        b.Tube(tail, Smooth(3, curve[^1], curve[^1] + V(-0.03f, -0.01f, 0.05f), curve[^1] + V(-0.02f, 0.04f, 0.08f)), 0.022f, 0.006f, violet);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.36f, 0.0f));
            b.Limb(leg, V(0.05f * s, 0.36f, 0.0f), V(0.08f * s, 0.27f, 0.04f), 0.032f, 0.026f, violet);
            Digits(b, leg, V(0.08f * s, 0.26f, 0.05f), V(0, -1f, 0.4f), V(0.5f, 0, 0), 0.035f, 0.01f, Claw, Shell);
        });
        // Great pincers held up, black wings with red linings stretched from them to its legs
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.08f * s, 0.55f, 0.0f));
            b.Limb(w, V(0.08f * s, 0.55f, 0.0f), V(0.22f * s, 0.66f, 0.02f), 0.035f, 0.03f, violet);
            b.Ell(w, V(0.25f * s, 0.72f, 0.03f), V(0.06f, 0.06f, 0.05f), violet);
            b.Tube(w, Smooth(3, V(0.27f * s, 0.76f, 0.03f), V(0.33f * s, 0.83f, 0.03f), V(0.29f * s, 0.89f, 0.03f)), 0.026f, 0.008f, violet);
            b.Tube(w, Smooth(3, V(0.23f * s, 0.77f, 0.03f), V(0.19f * s, 0.84f, 0.03f), V(0.22f * s, 0.88f, 0.03f)), 0.024f, 0.008f, violet);
            b.Ell(w, V(0.2f * s, 0.5f, -0.04f), V(0.16f, 0.16f, 0.012f), dark, V(0, 0, 28f * s));
            b.PaintEll(w, V(0.13f * s, 0.5f, -0.04f), V(0.05f, 0.14f, 0.03f), red, V(0, 0, 28f * s));
        });
        int head = b.Head(V(0, 0.58f, 0.02f));
        var c = V(0, 0.66f, 0.03f);
        var r = V(0.12f, 0.1f, 0.1f);
        b.Ell(head, c, r, violet);
        // Pointed ears, a wide grin of fangs, yellow eyes in a red mask
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, 0.73f, 0.0f));
            b.Spike(ear, V(0.07f * s, 0.72f, 0.0f), V(0.14f * s, 0.86f, -0.03f), 0.04f, violet, 0.5f);
        });
        b.PaintEll(head, V(0, 0.69f, 0.1f), V(0.1f, 0.035f, 0.05f), red);
        Gape(b, head, V(0, 0.62f, 0.115f), V(0.07f, 0.022f, 0.025f), Rgb(150, 30, 50), 0.022f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 0.69f), V(0.35f * s, 0.1f, 1f), 0.026f, pupil: Rgb(30, 20, 20), white: yellow, glare: true));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Nosepass line

    private static PokeBuilder Nosepass()
    {
        var b = new PokeBuilder("Nosepass", 0.7f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Shell };
        var stone = Rgb(114, 162, 216);
        var red = Rgb(232, 72, 72);
        var black = Rgb(36, 36, 44);

        // A block of blue stone with its big red nose pointing north, a black band across eyes kept shut
        b.Box(Body, V(0, 0.3f, 0), V(0.14f, 0.28f, 0.13f), 0.1f, stone);
        b.PaintEll(Body, V(0, 0.46f, 0.1f), V(0.17f, 0.035f, 0.07f), black);
        PokeBuilder.Both(s => b.Mark(Body, V(0.065f * s, 0.46f, 0.13f), V(0.2f * s, 0, 1f), 0.024f, 0.008f, Rgb(176, 206, 236), MarkShape.Wave));
        b.Spike(Body, V(0, 0.41f, 0.1f), V(0, 0.39f, 0.29f), 0.075f, red);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.28f, 0.0f));
            b.Box(arm, V(0.19f * s, 0.26f, 0.0f), V(0.07f, 0.05f, 0.05f), 0.03f, stone, V(0, 0, -15f * s));
            b.Mark(arm, V(0.257f * s, 0.242f, 0.0f), V(s, -0.25f, 0), 0.02f, 0.02f, black);
            int leg = b.Leg(s, V(0.07f * s, 0.06f, 0.0f));
            b.Box(leg, V(0.075f * s, 0.03f, 0.02f), V(0.05f, 0.03f, 0.06f), 0.025f, stone);
        });
        return b;
    }

    private static PokeBuilder Probopass()
    {
        var b = new PokeBuilder("Probopass", 0.86f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Shell };
        var stone = Rgb(102, 152, 208);
        var red = Rgb(226, 68, 68);
        var iron = Rgb(62, 62, 68);

        // A great stone head in a red cap, its nose bigger than ever over a moustache of iron filings
        b.Box(Body, V(0, 0.28f, 0), V(0.17f, 0.28f, 0.15f), 0.11f, stone);
        b.Ell(Body, V(0, 0.57f, 0), V(0.16f, 0.07f, 0.15f), red, mat: Shell);
        b.Spike(Body, V(0, 0.42f, 0.12f), V(0, 0.36f, 0.33f), 0.09f, red);
        Rubble(b, Body, V(0, 0.27f, 0.16f), V(0.11f, 0.05f, 0.04f), 0.026f, iron, 14, 7u);
        PokeBuilder.Both(s => b.Eye(Body, V(0.085f * s, 0.46f, 0.15f), V(0.3f * s, 0.1f, 1f), 0.026f, sclera: true, pupil: Rgb(30, 30, 30)));
        // Its little noses, each a stone in a red cap of its own
        PokeBuilder.Both(s =>
        {
            int unit = b.Arm(s, V(0.2f * s, 0.32f, 0.02f));
            b.Box(unit, V(0.28f * s, 0.3f, 0.02f), V(0.05f, 0.06f, 0.05f), 0.025f, stone);
            b.Ell(unit, V(0.28f * s, 0.37f, 0.02f), V(0.05f, 0.025f, 0.05f), red, mat: Shell);
            b.Spike(unit, V(0.28f * s, 0.3f, 0.06f), V(0.28f * s, 0.28f, 0.12f), 0.025f, red);
        });
        return b;
    }

    // ------------------------------------------------------------------ Ralts line

    private static PokeBuilder Ralts()
    {
        var b = new PokeBuilder("Ralts", 0.45f, BodyPlan.Biped, V(0, 0.14f, 0)) { Coat = Fur };
        var green = Rgb(124, 204, 112);
        var pink = Rgb(240, 120, 160);

        // A little white gown down to the ground, a green helmet of hair over its eyes, two pink horns
        b.Limb(Body, V(0, 0.17f, 0), V(0, 0.0f, 0), 0.04f, 0.09f, White);
        b.CutBox(Body, V(0, -0.2f, 0), V(0.4f, 0.2f, 0.4f), Quaternion.Identity);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.04f * s, 0.16f, 0.02f));
            b.Ell(arm, V(0.05f * s, 0.14f, 0.05f), V(0.018f, 0.028f, 0.018f), White);
        });
        int head = b.Head(V(0, 0.21f, 0));
        var c = V(0, 0.25f, 0.02f);
        var r = V(0.075f, 0.065f, 0.068f);
        b.Ell(head, c, r, White);
        b.Ell(head, V(0, 0.29f, 0.0f), V(0.11f, 0.085f, 0.1f), green);
        Blade(b, head, V(0.015f, 0.35f, 0.05f), V(0.04f, 0.45f, 0.12f), 0.036f, pink, V(1f, 0, 0), 0.35f);
        Blade(b, head, V(-0.015f, 0.35f, -0.05f), V(-0.03f, 0.42f, -0.12f), 0.03f, pink, V(1f, 0, 0), 0.35f);
        b.Mark(head, On(c, r, 0, 0.205f), V(0, -0.3f, 1f), 0.008f, 0.006f, Rgb(220, 90, 110));
        return b;
    }

    private static PokeBuilder Kirlia()
    {
        var b = new PokeBuilder("Kirlia", 0.66f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var green = Rgb(124, 204, 112);
        var pink = Rgb(240, 120, 160);

        // Thin green legs on points, like a dancer's, under a skirt that flares like a tutu
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.36f, 0.0f));
            b.Limb(leg, V(0.025f * s, 0.36f, 0.0f), V(0.035f * s, 0.04f, 0.01f), 0.02f, 0.012f, green);
            b.Spike(leg, V(0.035f * s, 0.045f, 0.01f), V(0.04f * s, 0.0f, 0.02f), 0.013f, green);
        });
        b.Ell(Body, V(0, 0.38f, 0), V(0.15f, 0.04f, 0.13f), White);
        b.Ell(Body, V(0, 0.45f, 0), V(0.05f, 0.08f, 0.045f), White);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.04f * s, 0.5f, 0.0f));
            b.Tube(arm, new[] { V(0.04f * s, 0.5f, 0.0f), V(0.07f * s, 0.45f, 0.05f), V(0.018f * s, 0.48f, 0.08f) }, 0.016f, 0.014f, White);
        });
        int head = b.Head(V(0, 0.53f, 0));
        var c = V(0, 0.575f, 0.03f);
        var r = V(0.055f, 0.055f, 0.055f);
        b.Ell(head, c, r, White);
        // A bob of green hair, one lock down over its right eye, and two pink horns like ribbons
        b.Ell(head, V(0, 0.61f, -0.01f), V(0.09f, 0.075f, 0.085f), green);
        PokeBuilder.Both(s => b.Ell(head, V(0.065f * s, 0.56f, 0.0f), V(0.035f, 0.07f, 0.06f), green));
        b.Ell(head, V(-0.025f, 0.585f, 0.06f), V(0.035f, 0.035f, 0.022f), green, blend: 0.01f);
        PokeBuilder.Both(s => Blade(b, head, V(0.05f * s, 0.64f, -0.02f), V(0.12f * s, 0.73f, -0.05f), 0.03f, pink, V(0, 0, 1f), 0.3f));
        b.Eye(head, On(c, r, 0.022f, 0.58f), V(0.3f, 0.05f, 1f), 0.017f, Rgb(220, 60, 90));
        return b;
    }

    private static PokeBuilder Gardevoir()
    {
        var b = new PokeBuilder("Gardevoir", 0.92f, BodyPlan.Biped, V(0, 0.7f, 0)) { Coat = Fur };
        var green = Rgb(124, 204, 112);
        var red = Rgb(230, 60, 90);

        // A white gown that falls to the ground and opens at the front on green
        b.Limb(Body, V(0, 0.62f, 0), V(0, 0.0f, -0.02f), 0.05f, 0.24f, White);
        b.CutBox(Body, V(0, -0.3f, 0), V(0.6f, 0.3f, 0.6f), Quaternion.Identity);
        var seam = V(0, -0.62f, 0.2f);
        b.PaintEll(Body, V(0, 0.33f, 0.13f), V(0.022f, 0.3f, 0.03f), green, Euler(seam));
        b.PaintEll(Body, V(0, 0.08f, 0.21f), V(0.06f, 0.09f, 0.05f), green, Euler(seam));
        b.Ell(Body, V(0, 0.71f, 0), V(0.06f, 0.1f, 0.05f), White);
        // The red fin through its chest, before and behind
        Blade(b, Body, V(0, 0.72f, 0.035f), V(0, 0.8f, 0.15f), 0.045f, red, V(1f, 0, 0), 0.3f);
        Blade(b, Body, V(0, 0.72f, -0.035f), V(0, 0.8f, -0.15f), 0.045f, red, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.78f, 0.0f));
            b.Tube(arm, Smooth(3, V(0.06f * s, 0.78f, 0.0f), V(0.15f * s, 0.7f, 0.04f), V(0.24f * s, 0.62f, 0.08f)), 0.022f, 0.017f, green);
            b.Ell(arm, V(0.255f * s, 0.6f, 0.09f), V(0.022f, 0.026f, 0.018f), green);
        });
        int head = b.Head(V(0, 0.83f, 0));
        var c = V(0, 0.89f, 0.03f);
        var r = V(0.075f, 0.075f, 0.072f);
        b.Ell(head, c, r, White);
        // A bob of green hair falling past its cheeks, and red eyes
        b.Ell(head, V(0, 0.935f, -0.01f), V(0.1f, 0.08f, 0.095f), green);
        PokeBuilder.Both(s => b.Ell(head, V(0.075f * s, 0.865f, 0.0f), V(0.038f, 0.085f, 0.07f), green));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.03f * s, 0.89f), V(0.3f * s, 0.05f, 1f), 0.018f, Rgb(220, 50, 80)));
        return b;
    }

    private static PokeBuilder Gallade()
    {
        var b = new PokeBuilder("Gallade", 0.92f, BodyPlan.Biped, V(0, 0.62f, 0)) { Coat = Fur };
        var green = Rgb(40, 160, 90);
        var steel = Rgb(150, 190, 214);
        var red = Rgb(230, 60, 90);

        // Long white legs flaring at the thigh, a white body under a green chest
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.52f, 0.0f));
            b.Limb(leg, V(0.05f * s, 0.52f, 0.0f), V(0.11f * s, 0.32f, 0.02f), 0.06f, 0.035f, White);
            b.Limb(leg, V(0.11f * s, 0.32f, 0.02f), V(0.12f * s, 0.05f, 0.0f), 0.024f, 0.018f, White);
            b.Spike(leg, V(0.12f * s, 0.05f, 0.0f), V(0.13f * s, 0.01f, 0.08f), 0.025f, White);
        });
        b.Ell(Body, V(0, 0.6f, 0), V(0.075f, 0.11f, 0.06f), White);
        b.Ell(Body, V(0, 0.5f, 0), V(0.13f, 0.05f, 0.09f), White);
        b.Ell(Body, V(0, 0.7f, 0), V(0.09f, 0.06f, 0.065f), green);
        Blade(b, Body, V(0, 0.71f, 0.05f), V(0, 0.77f, 0.13f), 0.03f, red, V(1f, 0, 0), 0.3f);
        Blade(b, Body, V(0, 0.71f, -0.05f), V(0, 0.77f, -0.13f), 0.03f, red, V(1f, 0, 0), 0.3f);
        // Arms with a long green blade out from each elbow
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.72f, 0.0f));
            b.Limb(arm, V(0.08f * s, 0.72f, 0.0f), V(0.16f * s, 0.62f, 0.05f), 0.028f, 0.024f, green);
            b.Limb(arm, V(0.16f * s, 0.62f, 0.05f), V(0.18f * s, 0.52f, 0.12f), 0.024f, 0.02f, White);
            Blade(b, arm, V(0.16f * s, 0.62f, 0.05f), V(0.24f * s, 0.74f, 0.25f), 0.045f, green, V(s, 0, -0.4f), 0.22f);
        });
        int head = b.Head(V(0, 0.76f, 0.01f));
        var c = V(0, 0.82f, 0.03f);
        var r = V(0.072f, 0.076f, 0.075f);
        b.Ell(head, c, r, White);
        // A crest like a blade, green before and steel-blue behind, white horns at its cheeks, red eyes
        Blade(b, head, V(0, 0.86f, 0.06f), V(0, 0.98f, 0.12f), 0.035f, green, V(1f, 0, 0), 0.3f);
        Blade(b, head, V(0, 0.88f, -0.02f), V(0, 1.0f, -0.1f), 0.045f, steel, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.05f * s, 0.82f, 0.0f), V(0.12f * s, 0.86f, -0.04f), 0.025f, White, 0.5f);
            b.Eye(head, On(c, r, 0.026f * s, 0.825f), V(0.35f * s, 0.05f, 1f), 0.016f, Rgb(220, 50, 80), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Lickitung line

    private static PokeBuilder Lickitung()
    {
        var b = new PokeBuilder("Lickitung", 0.84f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var pink = Rgb(236, 140, 160);
        var cream = Rgb(250, 230, 160);
        var tongue = Rgb(250, 190, 196);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.12f, 0.02f));
            b.Limb(leg, V(0.1f * s, 0.12f, 0.02f), V(0.12f * s, 0.04f, 0.05f), 0.07f, 0.06f, pink);
            b.Ell(leg, V(0.12f * s, 0.025f, 0.08f), V(0.06f, 0.025f, 0.07f), pink);
            b.Mark(leg, V(0.135f * s, 0.11f, 0.085f), V(0.4f * s, 0, 1f), 0.03f, 0.03f, cream, MarkShape.Ring);
        });
        // A stout body, its cream belly banded in pink, and a thick tail
        var c = V(0, 0.32f, -0.02f);
        var r = V(0.19f, 0.24f, 0.17f);
        b.Ell(Body, c, r, pink);
        b.PaintEll(Body, V(0, 0.26f, 0.08f), V(0.14f, 0.16f, 0.12f), cream);
        b.PaintEll(Body, V(0, 0.29f, 0.13f), V(0.16f, 0.022f, 0.08f), pink);
        b.PaintEll(Body, V(0, 0.2f, 0.12f), V(0.16f, 0.018f, 0.08f), pink);
        int tail = b.Tail(V(0, 0.18f, -0.17f));
        b.Limb(tail, V(0, 0.18f, -0.17f), V(0.04f, 0.08f, -0.32f), 0.09f, 0.06f, pink);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, 0.42f, 0.04f));
            b.Limb(arm, V(0.16f * s, 0.42f, 0.04f), V(0.26f * s, 0.37f, 0.1f), 0.05f, 0.045f, pink);
            Digits(b, arm, V(0.27f * s, 0.36f, 0.12f), V(0.3f * s, -0.4f, 1f), V(0.5f, 0, 0), 0.03f, 0.016f, pink);
        });
        int head = b.Head(V(0, 0.5f, 0.04f));
        var hc = V(0, 0.56f, 0.06f);
        var hr = V(0.13f, 0.1f, 0.12f);
        b.Ell(head, hc, hr, pink);
        // A wide mouth and its tongue, longer than it is tall, hanging down to the ground
        Grin(b, head, V(0, 0.52f, 0.16f), V(0.08f, 0.035f, 0.03f), Rgb(170, 60, 80));
        b.Tube(head, Smooth(3, V(0, 0.51f, 0.15f), V(0, 0.47f, 0.25f), V(0.02f, 0.34f, 0.31f), V(0.03f, 0.2f, 0.3f)), 0.042f, 0.038f, tongue, blend: 0f);
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.06f * s, 0.6f), V(0.4f * s, 0.1f, 1f), 0.02f));
        return b;
    }

    private static PokeBuilder Lickilicky()
    {
        var b = new PokeBuilder("Lickilicky", 0.96f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Fur };
        var pink = Rgb(236, 140, 160);
        var yellow = Rgb(250, 214, 80);
        var tongue = Rgb(250, 190, 196);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.14f, 0.03f));
            b.Limb(leg, V(0.11f * s, 0.15f, 0.03f), V(0.12f * s, 0.05f, 0.06f), 0.06f, 0.055f, pink);
            b.Ell(leg, V(0.12f * s, 0.04f, 0.07f), V(0.07f, 0.04f, 0.08f), pink);
        });
        // A round body with a white bib scalloped at its edge, and yellow bands across its belly
        var c = V(0, 0.38f, -0.02f);
        var r = V(0.25f, 0.3f, 0.22f);
        b.Ell(Body, c, r, pink);
        b.PaintEll(Body, V(0, 0.52f, 0.15f), V(0.17f, 0.08f, 0.1f), White);
        foreach (float x in new[] { -0.09f, 0f, 0.09f })
            b.PaintEll(Body, V(x, 0.44f, 0.18f), V(0.03f, 0.03f, 0.05f), White);
        foreach (float y in new[] { 0.34f, 0.27f, 0.2f })
            b.PaintEll(Body, V(0, y, 0.16f), V(0.13f, 0.016f, 0.1f), yellow, soft: 0.01f);
        int tail = b.Tail(V(0, 0.2f, -0.22f));
        b.Limb(tail, V(0, 0.2f, -0.22f), V(0.05f, 0.08f, -0.36f), 0.09f, 0.06f, pink);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.22f * s, 0.46f, 0.04f));
            b.Limb(arm, V(0.22f * s, 0.46f, 0.04f), V(0.3f * s, 0.36f, 0.12f), 0.07f, 0.06f, pink);
        });
        int head = b.Head(V(0, 0.6f, 0.04f));
        var hc = V(0, 0.66f, 0.04f);
        var hr = V(0.14f, 0.11f, 0.12f);
        b.Ell(head, hc, hr, pink);
        // A big curl on top, and a broad tongue lolling out across its front
        Curl(b, head, V(-0.05f, 0.74f, 0.0f), V(0.01f, 0.8f, 0.0f), V(-1f, 0, 0), V(0, 1f, 0), 0.065f, 1.05f, 0.052f, pink);
        Grin(b, head, V(0, 0.63f, 0.15f), V(0.07f, 0.03f, 0.03f), Rgb(170, 60, 80));
        b.Tube(head, Smooth(3, V(0, 0.62f, 0.15f), V(-0.06f, 0.6f, 0.23f), V(-0.15f, 0.55f, 0.24f), V(-0.2f, 0.47f, 0.2f)), 0.045f, 0.04f, tongue, blend: 0f);
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.055f * s, 0.7f), V(0.4f * s, 0.1f, 1f), 0.018f));
        return b;
    }

    // ------------------------------------------------------------------ Eevee and its evolutions

    private static PokeBuilder Eevee()
    {
        var b = new PokeBuilder("Eevee", 0.5f, BodyPlan.Quadruped, V(0, 0.17f, -0.02f)) { Coat = Fur };
        var brown = Rgb(198, 132, 72);
        var cream = Rgb(246, 228, 184);
        var dark = Rgb(112, 72, 42);

        foreach (var (z, front) in new[] { (0.06f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.13f, z), front);
                b.Limb(leg, V(0.05f * s, 0.13f, z), V(0.055f * s, 0.02f, z + 0.01f), 0.026f, 0.022f, brown);
                b.Ell(leg, V(0.055f * s, 0.018f, z + 0.025f), V(0.024f, 0.018f, 0.03f), brown);
            });
        // A fluffy cream ruff round its neck and a bushy tail tipped with cream
        b.Ell(Body, V(0, 0.17f, -0.02f), V(0.075f, 0.07f, 0.13f), brown);
        b.Ell(Body, V(0, 0.22f, 0.08f), V(0.08f, 0.065f, 0.06f), cream);
        for (int i = 0; i < 9; i++)
        {
            float a = -1.6f + i * 0.4f;
            var at = V(MathF.Sin(a) * 0.075f, 0.21f, 0.08f + MathF.Cos(a) * 0.05f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.035f, -0.04f, MathF.Cos(a) * 0.035f), 0.028f, cream);
        }
        int tail = b.Tail(V(0, 0.2f, -0.14f));
        b.Ell(tail, V(0, 0.29f, -0.2f), V(0.065f, 0.12f, 0.065f), brown, V(-25f, 0, 0));
        b.PaintEll(tail, V(0, 0.38f, -0.25f), V(0.07f, 0.05f, 0.07f), cream);

        int head = b.Head(V(0, 0.27f, 0.08f));
        var c = V(0, 0.33f, 0.1f);
        var r = V(0.1f, 0.09f, 0.09f);
        b.Ell(head, c, r, brown);
        b.Ell(head, V(0, 0.3f, 0.17f), V(0.04f, 0.03f, 0.03f), brown);
        // Big ears edged in dark brown, big dark eyes
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.4f, 0.07f));
            MouseEar(b, ear, V(0.05f * s, 0.4f, 0.07f), V(0.17f * s, 0.6f, 0.02f), 0.05f, brown, dark, 0.25f);
        });
        b.Mark(head, V(0, 0.31f, 0.2f), V(0, 0.2f, 1f), 0.008f, 0.006f, dark);
        b.Mark(head, V(0, 0.288f, 0.193f), V(0, -0.3f, 1f), 0.016f, 0.006f, dark, MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 0.34f), V(0.4f * s, 0.05f, 1f), 0.028f, Rgb(110, 60, 40)));
        return b;
    }

    /// <summary>
    /// What Eevee's evolutions share: a slim fox standing <paramref name="h"/> tall at the hip, a neck rising to a head
    /// with a small muzzle and nose. Returns the head bone and the head's centre and radii, to place eyes on.
    /// </summary>
    private static (int head, Vector3 c, Vector3 r) EeveeShape(PokeBuilder b, Color coat, Color paws, float h, Color nose)
    {
        foreach (var (z, front) in new[] { (0.08f, true), (-0.13f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, h, z), front);
                var knee = V(0.055f * s, h * 0.45f, z + (front ? 0.01f : -0.015f));
                b.Limb(leg, V(0.05f * s, h, z), knee, 0.032f, 0.024f, coat);
                b.Limb(leg, knee, V(0.055f * s, 0.022f, z + 0.01f), 0.022f, 0.02f, coat);
                b.Ell(leg, V(0.055f * s, 0.018f, z + 0.025f), V(0.024f, 0.018f, 0.032f), paws);
            });
        b.Ell(Body, V(0, h + 0.02f, -0.025f), V(0.075f, 0.07f, 0.15f), coat);
        b.Tube(Body, new[] { V(0, h + 0.04f, 0.09f), V(0, h + 0.12f, 0.12f), V(0, h + 0.18f, 0.13f) }, 0.05f, 0.042f, coat, blend: 0.02f);
        int head = b.Head(V(0, h + 0.17f, 0.13f));
        var c = V(0, h + 0.22f, 0.15f);
        var r = V(0.09f, 0.085f, 0.09f);
        b.Ell(head, c, r, coat);
        b.Ell(head, V(0, h + 0.195f, 0.225f), V(0.038f, 0.03f, 0.036f), coat);
        b.Mark(head, V(0, h + 0.207f, 0.257f), V(0, 0.2f, 1f), 0.011f, 0.008f, nose);
        return (head, c, r);
    }

    /// <summary>A pointed ear with its inside painted a darker shade, from <paramref name="root"/> to <paramref name="tip"/>.</summary>
    private static void FoxEar(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float r, Color color, Color inside)
    {
        b.Spike(bone, root, tip, r, color, 0.5f);
        var d = tip - root;
        b.PaintEll(bone, root + d * 0.45f + V(0, 0, r * 0.4f), V(r * 0.55f, d.Length() * 0.3f, r * 0.3f), inside, Euler(d));
    }

    private static PokeBuilder Vaporeon()
    {
        var b = new PokeBuilder("Vaporeon", 0.74f, BodyPlan.Quadruped, V(0, 0.19f, -0.03f)) { Coat = Scales };
        var blue = Rgb(92, 190, 232);
        var navy = Rgb(40, 96, 168);
        var cream = Rgb(248, 240, 200);
        const float h = 0.17f;
        var (head, c, r) = EeveeShape(b, blue, blue, h, navy);
        // A frill of fins round its neck, a dark crest down its back and a tail that ends like a mermaid's
        for (int i = 0; i < 11; i++)
        {
            float a = -1.9f + i * 0.38f;
            var at = V(MathF.Sin(a) * 0.05f, h + 0.13f, 0.12f + MathF.Cos(a) * 0.04f);
            Blade(b, Body, at, at + V(MathF.Sin(a) * 0.05f, -0.02f, MathF.Cos(a) * 0.045f), 0.026f, cream, V(0, 1f, 0), 0.35f);
        }
        int crest = b.Part("crest", Body, V(0, h + 0.1f, 0.0f), PokeRole.Fin);
        foreach (var (y, z) in new[] { (h + 0.17f, 0.085f), (h + 0.1f, 0.03f), (h + 0.09f, -0.06f), (h + 0.075f, -0.13f) })
            Blade(b, crest, V(0, y, z), V(0, y + 0.05f, z - 0.05f), 0.024f, navy, V(1f, 0, 0), 0.3f);
        int tail = b.Tail(V(0, h + 0.03f, -0.17f));
        var tailPath = Smooth(3, V(0, h + 0.03f, -0.17f), V(0.03f, h - 0.02f, -0.28f), V(0.09f, h - 0.05f, -0.36f), V(0.15f, h - 0.03f, -0.4f));
        b.Tube(tail, tailPath, 0.055f, 0.025f, blue, blend: 0f);
        var end = tailPath[^1];
        Blade(b, tail, end, end + V(0.11f, 0.06f, -0.05f), 0.05f, blue, V(0, 0.4f, 1f), 0.25f);
        Blade(b, tail, end, end + V(0.09f, -0.01f, 0.08f), 0.05f, blue, V(0, 0.4f, 1f), 0.25f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, h + 0.26f, 0.13f));
            Blade(b, ear, V(0.055f * s, h + 0.26f, 0.13f), V(0.19f * s, h + 0.32f, 0.08f), 0.05f, cream, V(0, 0.3f, 1f), 0.25f);
        });
        Blade(b, head, V(0, h + 0.28f, 0.12f), V(0, h + 0.4f, 0.07f), 0.03f, navy, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, h + 0.235f), V(0.45f * s, 0.05f, 1f), 0.024f, Rgb(40, 60, 110)));
        return b;
    }

    private static PokeBuilder Jolteon()
    {
        var b = new PokeBuilder("Jolteon", 0.7f, BodyPlan.Quadruped, V(0, 0.26f, -0.03f)) { Coat = Fur };
        var yellow = Rgb(250, 214, 56);
        var dark = Rgb(40, 40, 50);
        const float h = 0.24f;
        var (head, c, r) = EeveeShape(b, yellow, yellow, h, dark);
        // A ruff of white spikes, and its fur standing out in spikes over its back and rump
        for (int i = 0; i < 11; i++)
        {
            float a = -1.9f + i * 0.38f;
            var at = V(MathF.Sin(a) * 0.05f, h + 0.11f, 0.11f + MathF.Cos(a) * 0.04f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.05f, -0.05f, MathF.Cos(a) * 0.045f), 0.026f, White);
        }
        PokeBuilder.Both(s => b.Spike(Body, V(0.04f * s, h + 0.07f, -0.06f), V(0.07f * s, h + 0.13f, -0.1f), 0.025f, yellow));
        int tail = b.Tail(V(0, h + 0.04f, -0.15f));
        foreach (var (dx, dy, dz) in new[] { (0f, 0.11f, -0.1f), (0.06f, 0.07f, -0.11f), (-0.06f, 0.07f, -0.11f), (0.04f, 0.12f, -0.03f), (-0.04f, 0.12f, -0.03f), (0f, 0.02f, -0.14f), (0.08f, 0.0f, -0.08f), (-0.08f, 0.0f, -0.08f) })
            b.Spike(tail, V(0, h + 0.04f, -0.15f), V(dx, h + 0.04f + dy, -0.15f + dz), 0.03f, yellow);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, h + 0.28f, 0.13f));
            FoxEar(b, ear, V(0.05f * s, h + 0.27f, 0.13f), V(0.13f * s, h + 0.44f, 0.08f), 0.042f, yellow, dark);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, h + 0.235f), V(0.45f * s, 0.05f, 1f), 0.022f, glare: true));
        return b;
    }

    private static PokeBuilder Flareon()
    {
        var b = new PokeBuilder("Flareon", 0.72f, BodyPlan.Quadruped, V(0, 0.22f, -0.03f)) { Coat = Fur };
        var orange = Rgb(242, 128, 58);
        var cream = Rgb(250, 230, 150);
        var dark = Rgb(90, 50, 30);
        const float h = 0.2f;
        var (head, c, r) = EeveeShape(b, orange, orange, h, dark);
        // A great fluffy collar, a tuft on its brow and a big bushy tail, all pale gold
        b.Ell(Body, V(0, h + 0.11f, 0.11f), V(0.085f, 0.075f, 0.065f), cream, blend: 0.03f);
        for (int i = 0; i < 9; i++)
        {
            float a = -1.6f + i * 0.4f;
            var at = V(MathF.Sin(a) * 0.08f, h + 0.1f, 0.11f + MathF.Cos(a) * 0.055f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.04f, -0.035f, MathF.Cos(a) * 0.035f), 0.03f, cream);
        }
        b.Ell(head, V(0, h + 0.29f, 0.15f), V(0.04f, 0.035f, 0.04f), cream);
        b.Spike(head, V(0, h + 0.3f, 0.16f), V(0.01f, h + 0.35f, 0.2f), 0.025f, cream);
        int tail = b.Tail(V(0, h + 0.04f, -0.16f));
        b.Ell(tail, V(0, h + 0.13f, -0.24f), V(0.085f, 0.13f, 0.085f), cream, V(-30f, 0, 0), blend: 0.03f);
        b.Spike(tail, V(0, h + 0.2f, -0.27f), V(0.02f, h + 0.29f, -0.29f), 0.04f, cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, h + 0.28f, 0.13f));
            FoxEar(b, ear, V(0.05f * s, h + 0.27f, 0.13f), V(0.15f * s, h + 0.41f, 0.08f), 0.045f, orange, dark);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, h + 0.235f), V(0.45f * s, 0.05f, 1f), 0.024f, Rgb(110, 60, 30)));
        return b;
    }

    private static PokeBuilder Espeon()
    {
        var b = new PokeBuilder("Espeon", 0.72f, BodyPlan.Quadruped, V(0, 0.26f, -0.03f)) { Coat = Fur };
        var lilac = Rgb(232, 194, 232);
        var violet = Rgb(130, 120, 200);
        var red = Rgb(226, 40, 60);
        const float h = 0.24f;
        var (head, c, r) = EeveeShape(b, lilac, lilac, h, Rgb(150, 100, 150));
        // Great ears, a red jewel on its brow, a tail that forks at the end
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.055f * s, h + 0.27f, 0.13f));
            FoxEar(b, ear, V(0.055f * s, h + 0.26f, 0.13f), V(0.19f * s, h + 0.39f, 0.07f), 0.055f, lilac, violet);
            b.Spike(head, V(0.065f * s, h + 0.2f, 0.14f), V(0.11f * s, h + 0.16f, 0.12f), 0.02f, lilac, 0.6f);
        });
        b.Ell(head, V(0, h + 0.27f, 0.212f), V(0.015f, 0.015f, 0.012f), red, mat: Shell, blend: 0.006f);
        int tail = b.Tail(V(0, h + 0.04f, -0.16f));
        var path = Smooth(3, V(0, h + 0.04f, -0.16f), V(0, h + 0.0f, -0.26f), V(0.02f, h + 0.08f, -0.34f), V(0.03f, h + 0.18f, -0.33f));
        b.Tube(tail, path, 0.024f, 0.018f, lilac, blend: 0f);
        PokeBuilder.Both(s => b.Tube(tail, Smooth(3, path[^1], path[^1] + V(0.03f * s, 0.05f, 0.0f), path[^1] + V(0.03f * s, 0.08f, 0.04f)), 0.016f, 0.008f, lilac));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, h + 0.235f), V(0.45f * s, 0.05f, 1f), 0.024f, Rgb(140, 60, 170)));
        return b;
    }

    private static PokeBuilder Umbreon()
    {
        var b = new PokeBuilder("Umbreon", 0.74f, BodyPlan.Quadruped, V(0, 0.28f, -0.03f)) { Coat = Fur };
        var black = Rgb(46, 50, 60);
        var gold = Rgb(246, 210, 70);
        const float h = 0.26f;
        var (head, c, r) = EeveeShape(b, black, black, h, Rgb(20, 20, 26));
        // Gold rings that glow by moonlight: on its ears, brow, tail and legs
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, h + 0.28f, 0.13f));
            var root = V(0.05f * s, h + 0.27f, 0.13f);
            var tip = V(0.13f * s, h + 0.46f, 0.08f);
            b.Spike(ear, root, tip, 0.045f, black, 0.5f);
            b.PaintEll(ear, Vector3.Lerp(root, tip, 0.5f), V(0.06f, 0.012f, 0.06f), gold, Euler(tip - root));
        });
        b.Mark(head, On(c, r, 0, h + 0.27f), V(0, 0.6f, 1f), 0.022f, 0.022f, gold, MarkShape.Ring);
        foreach (var (z, front) in new[] { (0.08f, true), (-0.13f, false) })
            PokeBuilder.Both(s => b.PaintEll(Body, V(0.055f * s, h * 0.72f, z + (front ? 0.004f : -0.006f)), V(0.045f, 0.012f, 0.045f), gold));
        int tail = b.Tail(V(0, h + 0.04f, -0.16f));
        var path = Smooth(3, V(0, h + 0.04f, -0.16f), V(0, h + 0.1f, -0.25f), V(0, h + 0.2f, -0.29f));
        b.Tube(tail, path, 0.035f, 0.022f, black, blend: 0f);
        b.PaintEll(tail, path[path.Length / 2], V(0.05f, 0.014f, 0.05f), gold, Euler(path[^1] - path[0]));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, h + 0.235f), V(0.45f * s, 0.05f, 1f), 0.024f, Rgb(220, 40, 50), glare: true));
        return b;
    }

    private static PokeBuilder Leafeon()
    {
        var b = new PokeBuilder("Leafeon", 0.72f, BodyPlan.Quadruped, V(0, 0.26f, -0.03f)) { Coat = Fur };
        var cream = Rgb(242, 228, 172);
        var brown = Rgb(150, 100, 60);
        var green = Rgb(76, 172, 96);
        const float h = 0.24f;
        var (head, c, r) = EeveeShape(b, cream, brown, h, brown);
        // Leaves for ears, a leaf curling up off its brow, leaves sprouting from its chest and legs, and a leaf for a tail
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.055f * s, h + 0.27f, 0.13f));
            Blade(b, ear, V(0.055f * s, h + 0.26f, 0.13f), V(0.19f * s, h + 0.38f, 0.07f), 0.05f, green, V(0, 0.2f, 1f), 0.2f, Leaf);
            b.PaintEll(ear, V(0.12f * s, h + 0.32f, 0.11f), V(0.035f, 0.05f, 0.04f), cream, Euler(V(0.14f * s, 0.12f, -0.06f)));
        });
        Blade(b, head, V(0, h + 0.28f, 0.15f), V(0.02f, h + 0.38f, 0.1f), 0.03f, green, V(1f, 0, 0), 0.25f, Leaf);
        Blade(b, Body, V(0, h + 0.1f, 0.13f), V(0.0f, h + 0.06f, 0.2f), 0.025f, green, V(1f, 0, 0), 0.3f, Leaf);
        PokeBuilder.Both(s => Blade(b, Body, V(0.06f * s, h - 0.02f, -0.12f), V(0.1f * s, h + 0.03f, -0.17f), 0.02f, green, V(s, 0, 0), 0.3f, Leaf));
        int tail = b.Tail(V(0, h + 0.04f, -0.16f));
        Blade(b, tail, V(0, h + 0.04f, -0.16f), V(0.02f, h + 0.3f, -0.3f), 0.07f, green, V(1f, 0, 0.2f), 0.18f, Leaf);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, h + 0.235f), V(0.45f * s, 0.05f, 1f), 0.024f, Rgb(120, 70, 40)));
        return b;
    }

    private static PokeBuilder Glaceon()
    {
        var b = new PokeBuilder("Glaceon", 0.7f, BodyPlan.Quadruped, V(0, 0.24f, -0.03f)) { Coat = Fur };
        var ice = Rgb(176, 222, 244);
        var navy = Rgb(40, 110, 170);
        const float h = 0.22f;
        var (head, c, r) = EeveeShape(b, ice, navy, h, navy);
        // A cap of dark blue on its head with two locks hanging from it, diamonds of ice down its back and tail
        b.PaintEll(head, V(0, h + 0.28f, 0.14f), V(0.085f, 0.04f, 0.085f), navy);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, V(0.07f * s, h + 0.25f, 0.15f), V(0.1f * s, h + 0.18f, 0.16f), V(0.11f * s, h + 0.1f, 0.15f)), 0.012f, 0.01f, ice);
            Blade(b, head, V(0.11f * s, h + 0.11f, 0.15f), V(0.11f * s, h + 0.03f, 0.15f), 0.028f, navy, V(0, 0, 1f), 0.3f);
            int ear = b.Ear(head, s, V(0.055f * s, h + 0.27f, 0.13f));
            FoxEar(b, ear, V(0.055f * s, h + 0.26f, 0.13f), V(0.15f * s, h + 0.37f, 0.08f), 0.045f, ice, navy);
        });
        foreach (float z in new[] { 0.03f, -0.07f })
            b.Mark(Body, V(0, h + 0.088f, z), V(0, 1f, 0.1f), 0.022f, 0.028f, navy, MarkShape.Diamond);
        int tail = b.Tail(V(0, h + 0.04f, -0.16f));
        var tip = V(0, h + 0.24f, -0.3f);
        b.Tube(tail, new[] { V(0, h + 0.04f, -0.16f), V(0, h + 0.12f, -0.25f), tip }, 0.03f, 0.02f, ice, blend: 0f);
        Blade(b, tail, tip - V(0, 0.03f, -0.02f), tip + V(0, 0.07f, -0.04f), 0.035f, navy, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, h + 0.235f), V(0.45f * s, 0.05f, 1f), 0.024f, Rgb(40, 90, 150)));
        return b;
    }

    // ------------------------------------------------------------------ Swablu line

    private static PokeBuilder Swablu()
    {
        var b = new PokeBuilder("Swablu", 0.5f, BodyPlan.Bird, V(0, 0.3f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(84, 184, 238);

        // A round blue bird with wings of cotton cloud, two fine feathers on its crown
        var c = V(0, 0.3f, 0);
        var r = V(0.09f, 0.085f, 0.09f);
        b.Ell(Body, c, r, blue);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.08f * s, 0.32f, 0));
            Puff(b, w, V(0.19f * s, 0.33f, -0.01f), 0.065f);
            Puff(b, w, V(0.28f * s, 0.32f, -0.03f), 0.05f);
            int leg = b.Leg(s, V(0.03f * s, 0.23f, 0.0f));
            b.Limb(leg, V(0.03f * s, 0.23f, 0.0f), V(0.035f * s, 0.19f, 0.01f), 0.01f, 0.009f, blue);
        });
        int tail = b.Tail(V(0, 0.28f, -0.08f));
        PokeBuilder.Both(s => b.Tube(tail, new[] { V(0.012f * s, 0.28f, -0.08f), V(0.02f * s, 0.24f, -0.15f), V(0.03f * s, 0.22f, -0.2f) }, 0.012f, 0.006f, blue));
        int head = b.Head(V(0, 0.34f, 0.03f));
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.015f * s, 0.38f, 0.02f), V(0.03f * s, 0.46f, -0.01f), V(0.05f * s, 0.5f, -0.06f)), 0.01f, 0.005f, blue));
        b.Spike(head, V(0, 0.29f, 0.08f), V(0, 0.265f, 0.16f), 0.02f, blue);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.042f * s, 0.325f), V(0.45f * s, 0.1f, 1f), 0.025f, sclera: true, pupil: Rgb(30, 30, 40)));
        return Lift(b);
    }

    private static PokeBuilder Altaria()
    {
        var b = new PokeBuilder("Altaria", 0.86f, BodyPlan.Bird, V(0, 0.36f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(84, 184, 238);

        // A body wrapped in cloud, wings of cloud, a slender neck rising out of them
        b.Ell(Body, V(0, 0.34f, -0.02f), V(0.13f, 0.12f, 0.13f), blue);
        Puff(b, Body, V(0, 0.3f, 0.06f), 0.12f);
        Puff(b, Body, V(0, 0.42f, -0.06f), 0.1f);
        Puff(b, Body, V(0, 0.26f, -0.08f), 0.1f);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.1f * s, 0.38f, -0.02f));
            Puff(b, w, V(0.2f * s, 0.4f, -0.03f), 0.1f);
            Puff(b, w, V(0.31f * s, 0.36f, -0.06f), 0.075f);
            int leg = b.Leg(s, V(0.05f * s, 0.2f, 0.0f));
            b.Limb(leg, V(0.05f * s, 0.2f, 0.0f), V(0.06f * s, 0.1f, 0.03f), 0.014f, 0.012f, blue);
            Digits(b, leg, V(0.06f * s, 0.095f, 0.035f), V(0, -0.3f, 1f), V(0.6f, 0, 0), 0.03f, 0.008f, blue);
        });
        int tail = b.Tail(V(0, 0.25f, -0.16f));
        foreach (float x in new[] { -0.04f, 0f, 0.04f })
            Blade(b, tail, V(x * 0.5f, 0.25f, -0.16f), V(x, 0.17f, -0.3f), 0.025f, blue, V(0, 1f, 0.2f), 0.3f);
        b.Tube(Body, Smooth(3, V(0, 0.42f, 0.06f), V(0, 0.55f, 0.1f), V(0, 0.66f, 0.08f)), 0.042f, 0.036f, blue, blend: 0.015f);
        int head = b.Head(V(0, 0.66f, 0.08f));
        var c = V(0, 0.71f, 0.1f);
        var r = V(0.06f, 0.05f, 0.07f);
        b.Ell(head, c, r, blue);
        // A pale beak, and two long fine feathers trailing back from its head
        b.Spike(head, V(0, 0.695f, 0.16f), V(0, 0.675f, 0.22f), 0.02f, Rgb(214, 226, 240), mat: Shell);
        PokeBuilder.Both(s => b.Tube(head, Smooth(4, V(0.02f * s, 0.75f, 0.07f), V(0.05f * s, 0.82f, 0.0f), V(0.12f * s, 0.82f, -0.12f), V(0.2f * s, 0.76f, -0.2f)), 0.012f, 0.006f, blue));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.035f * s, 0.725f), V(0.6f * s, 0.1f, 0.8f), 0.017f, sclera: true, pupil: Rgb(30, 30, 40)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Togepi line

    private static PokeBuilder Togepi()
    {
        var b = new PokeBuilder("Togepi", 0.42f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Fur };
        var yellow = Rgb(250, 236, 150);
        var red = Rgb(228, 60, 60);
        var blue = Rgb(70, 140, 228);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.04f, 0.02f));
            b.Ell(leg, V(0.055f * s, 0.02f, 0.06f), V(0.032f, 0.02f, 0.042f), yellow);
        });
        // Still in the shell it hatched from, marked with red and blue, broken jaggedly round the top
        var c = V(0, 0.16f, 0);
        var r = V(0.12f, 0.14f, 0.11f);
        b.Ell(Body, c, r, White);
        foreach (var (x, y, color, roll) in new[] { (-0.07f, 0.17f, red, 15f), (0.065f, 0.12f, blue, -20f), (0.0f, 0.075f, red, 180f), (0.06f, 0.21f, red, 30f), (-0.05f, 0.08f, blue, 160f) })
        {
            var at = On(c, r, x, y);
            b.Mark(Body, at, Outward(c, r, at), 0.026f, 0.024f, color, MarkShape.Triangle, roll);
        }
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var at = V(MathF.Sin(a) * 0.1f, 0.255f, MathF.Cos(a) * 0.09f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.01f, 0.035f + 0.012f * (i % 2), MathF.Cos(a) * 0.01f), 0.024f, White);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.2f, 0.02f));
            b.Ell(arm, V(0.135f * s, 0.21f, 0.03f), V(0.035f, 0.022f, 0.025f), yellow, V(0, 0, 20f * s));
        });
        // A head of yellow spikes peeking out of the top
        int head = b.Head(V(0, 0.26f, 0));
        var hc = V(0, 0.3f, 0.01f);
        var hr = V(0.1f, 0.075f, 0.09f);
        b.Ell(head, hc, hr, yellow);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.3f;
            var at = V(MathF.Sin(a) * 0.06f, 0.34f, MathF.Cos(a) * 0.05f);
            b.Spike(head, at, at + V(MathF.Sin(a) * 0.05f, 0.06f, MathF.Cos(a) * 0.04f), 0.03f, yellow);
        }
        b.Mark(head, On(hc, hr, 0, 0.275f), V(0, -0.2f, 1f), 0.013f, 0.01f, Rgb(200, 70, 80));
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.035f * s, 0.31f), V(0.35f * s, 0.05f, 1f), 0.02f));
        return b;
    }

    private static PokeBuilder Togetic()
    {
        var b = new PokeBuilder("Togetic", 0.6f, BodyPlan.Bird, V(0, 0.3f, 0)) { Coat = Fur }.Hover();
        var red = Rgb(228, 60, 60);
        var blue = Rgb(70, 140, 228);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.18f, 0.0f));
            b.Limb(leg, V(0.04f * s, 0.18f, 0.0f), V(0.05f * s, 0.12f, 0.02f), 0.03f, 0.026f, White);
            b.Ell(leg, V(0.05f * s, 0.11f, 0.04f), V(0.028f, 0.02f, 0.04f), White);
        });
        // A white body marked with red and blue, little wings on its back
        var c = V(0, 0.3f, 0);
        var r = V(0.1f, 0.14f, 0.09f);
        b.Ell(Body, c, r, White);
        foreach (var (x, y, color, roll) in new[] { (0.0f, 0.26f, red, 180f), (0.06f, 0.33f, blue, -15f), (-0.06f, 0.33f, red, 15f) })
        {
            var at = On(c, r, x, y);
            b.Mark(Body, at, Outward(c, r, at), 0.025f, 0.024f, color, MarkShape.Triangle, roll);
        }
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.06f * s, 0.38f, -0.05f));
            Blade(b, w, V(0.06f * s, 0.37f, -0.06f), V(0.2f * s, 0.48f, -0.12f), 0.055f, White, V(0.2f * s, 0.2f, 1f), 0.25f);
            Blade(b, w, V(0.06f * s, 0.35f, -0.06f), V(0.17f * s, 0.38f, -0.13f), 0.04f, White, V(0.2f * s, 0.2f, 1f), 0.25f);
            int arm = b.Arm(s, V(0.08f * s, 0.34f, 0.04f));
            b.Ell(arm, V(0.12f * s, 0.32f, 0.06f), V(0.03f, 0.022f, 0.025f), White, V(0, 0, -25f * s));
        });
        int head = b.Head(V(0, 0.42f, 0.01f));
        var hc = V(0, 0.49f, 0.02f);
        var hr = V(0.085f, 0.08f, 0.08f);
        b.Ell(head, hc, hr, White);
        // A crown of points on its head
        foreach (var (x, z) in new[] { (-0.05f, 0.0f), (0.05f, 0.0f), (0f, -0.04f) })
            b.Spike(head, V(x, 0.54f, z), V(x * 1.5f, 0.62f, z - 0.03f), 0.035f, White);
        b.Mark(head, On(hc, hr, 0, 0.455f), V(0, -0.2f, 1f), 0.014f, 0.01f, Rgb(200, 70, 80));
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.033f * s, 0.49f), V(0.35f * s, 0.05f, 1f), 0.019f));
        return Lift(b);
    }

    private static PokeBuilder Togekiss()
    {
        var b = new PokeBuilder("Togekiss", 0.92f, BodyPlan.Bird, V(0, 0.36f, 0)) { Coat = Fur }.Hover();
        var red = Rgb(228, 60, 60);
        var blue = Rgb(70, 140, 228);

        // A smooth white body made for gliding, its wings one with it, red and blue on their fronts
        b.Ell(Body, V(0, 0.36f, -0.02f), V(0.1f, 0.095f, 0.19f), White);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.08f * s, 0.38f, -0.02f));
            b.Ell(w, V(0.24f * s, 0.39f, -0.1f), V(0.2f, 0.024f, 0.11f), White, V(0, 25f * s, 6f * s));
            b.PaintEll(w, V(0.14f * s, 0.41f, 0.0f), V(0.03f, 0.03f, 0.025f), red);
            b.PaintEll(w, V(0.22f * s, 0.42f, -0.04f), V(0.03f, 0.03f, 0.025f), blue);
            int leg = b.Leg(s, V(0.04f * s, 0.28f, 0.0f));
            b.Ell(leg, V(0.045f * s, 0.26f, 0.02f), V(0.025f, 0.02f, 0.035f), White);
        });
        int tail = b.Tail(V(0, 0.36f, -0.2f));
        foreach (float x in new[] { -0.05f, 0f, 0.05f })
            Blade(b, tail, V(x * 0.5f, 0.37f, -0.19f), V(x, 0.38f, -0.31f), 0.03f, White, V(0, 1f, 0), 0.3f);
        int head = b.Head(V(0, 0.4f, 0.14f));
        var hc = V(0, 0.43f, 0.17f);
        var hr = V(0.07f, 0.065f, 0.07f);
        b.Ell(head, hc, hr, White);
        // A crown of five points tipped with red and blue
        for (int i = -2; i <= 2; i++)
        {
            var root = V(i * 0.025f, 0.48f, 0.15f);
            var tip = V(i * 0.045f, 0.56f - MathF.Abs(i) * 0.015f, 0.09f);
            b.Spike(head, root, tip, 0.022f, White);
            b.PaintEll(head, tip - (tip - root) * 0.2f, V(0.02f, 0.025f, 0.02f), i % 2 == 0 ? red : blue);
        }
        b.Mark(head, On(hc, hr, 0, 0.405f), V(0, -0.2f, 1f), 0.012f, 0.008f, Rgb(200, 70, 80));
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.03f * s, 0.44f), V(0.4f * s, 0.05f, 1f), 0.016f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Houndour line

    /// <summary>A hound of shadow and fire: black, its muzzle and belly orange, white bones over its back and round its ankles.</summary>
    private static (int head, Vector3 c, Vector3 r) Hound(PokeBuilder b, float k, Color black, Color orange, Color bone)
    {
        float h = 0.2f * k;
        foreach (var (z, front) in new[] { (0.08f * k, true), (-0.13f * k, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s * k, h, z), front);
                b.Limb(leg, V(0.06f * s * k, h, z), V(0.065f * s * k, h * 0.45f, z), 0.034f * k, 0.026f * k, black);
                b.Limb(leg, V(0.065f * s * k, h * 0.45f, z), V(0.065f * s * k, 0.02f, z + 0.01f), 0.026f * k, 0.022f * k, black);
                b.Torus(leg, V(0.065f * s * k, 0.05f * k, z + 0.005f), 0.026f * k, 0.012f * k, bone, blend: 0.006f);
                Digits(b, leg, V(0.065f * s * k, 0.016f, z + 0.03f * k), V(0, -0.2f, 1f), V(0.6f, 0, 0), 0.025f * k, 0.01f * k, black);
            });
        b.Ell(Body, V(0, h + 0.03f * k, -0.025f * k), V(0.08f * k, 0.075f * k, 0.16f * k), black);
        b.PaintEll(Body, V(0, h - 0.04f * k, 0.0f), V(0.07f * k, 0.03f * k, 0.13f * k), orange);
        foreach (float z in new[] { 0.05f, -0.025f, -0.1f })
            b.PaintEll(Body, V(0, h + 0.1f * k, z * k), V(0.09f * k, 0.03f * k, 0.016f * k), bone);
        b.Tube(Body, new[] { V(0, h + 0.05f * k, 0.09f * k), V(0, h + 0.12f * k, 0.12f * k), V(0, h + 0.17f * k, 0.14f * k) }, 0.05f * k, 0.045f * k, black, blend: 0.02f);
        int head = b.Head(V(0, h + 0.17f * k, 0.14f * k));
        var c = V(0, h + 0.21f * k, 0.16f * k);
        var r = V(0.075f * k, 0.07f * k, 0.08f * k);
        b.Ell(head, c, r, black);
        b.Ell(head, V(0, h + 0.18f * k, 0.23f * k), V(0.04f * k, 0.035f * k, 0.05f * k), orange);
        b.Mark(head, V(0, h + 0.195f * k, 0.279f * k), V(0, 0.3f, 1f), 0.012f * k, 0.009f * k, Rgb(30, 26, 26));
        PokeBuilder.Both(s => b.Spike(head, V(0.022f * s * k, h + 0.165f * k, 0.25f * k), V(0.022f * s * k, h + 0.135f * k, 0.255f * k), 0.009f * k, White, mat: Shell, blend: 0.003f));
        return (head, c, r);
    }

    private static PokeBuilder Houndour()
    {
        var b = new PokeBuilder("Houndour", 0.6f, BodyPlan.Quadruped, V(0, 0.23f, -0.02f)) { Coat = Fur };
        var black = Rgb(54, 54, 64);
        var orange = Rgb(236, 128, 52);
        var bone = Rgb(236, 236, 228);
        var (head, c, r) = Hound(b, 1f, black, orange, bone);
        // A white skull-cap on its head, pointed ears, a thin tail ending in a point
        b.PaintEll(head, V(0, 0.46f, 0.15f), V(0.075f, 0.03f, 0.075f), bone);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.46f, 0.14f));
            b.Spike(ear, V(0.05f * s, 0.45f, 0.14f), V(0.08f * s, 0.55f, 0.1f), 0.035f, black, 0.5f);
        });
        int tail = b.Tail(V(0, 0.25f, -0.18f));
        var path = Smooth(3, V(0, 0.25f, -0.18f), V(0, 0.3f, -0.24f), V(0, 0.34f, -0.24f));
        b.Tube(tail, path, 0.016f, 0.012f, black, blend: 0f);
        Blade(b, tail, path[^1], path[^1] + V(0, 0.05f, 0.01f), 0.022f, black, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.04f * s, 0.425f), V(0.45f * s, 0.1f, 1f), 0.02f, glare: true));
        return b;
    }

    private static PokeBuilder Houndoom()
    {
        var b = new PokeBuilder("Houndoom", 0.88f, BodyPlan.Quadruped, V(0, 0.32f, -0.03f)) { Coat = Fur };
        var black = Rgb(50, 50, 60);
        var orange = Rgb(236, 128, 52);
        var bone = Rgb(236, 236, 228);
        const float k = 1.35f;
        var (head, c, r) = Hound(b, k, black, orange, bone);
        // Ram's horns curling back off its head, a skull hanging at its throat, a long tail ending in an arrowhead
        PokeBuilder.Both(s =>
        {
            var root = V(0.04f * s, 0.6f, 0.2f);
            var coil = Spiral(V(0.08f * s, 0.6f, 0.14f), V(0, 1f, 0), V(0, 0, -1f), 0.065f, 0.025f, 0.2f, MathF.PI * 1.75f, 14)
                .Select((p, i) => p + V(0.005f * i * s, 0, 0)).ToArray();
            b.Tube(head, new[] { root }.Concat(coil).ToArray(), 0.028f, 0.014f, Rgb(214, 214, 214), Shell, 0.004f);
            int ear = b.Ear(head, s, V(0.055f * s, 0.6f, 0.17f));
            b.Spike(ear, V(0.055f * s, 0.59f, 0.17f), V(0.09f * s, 0.66f, 0.12f), 0.03f, black, 0.5f);
        });
        b.Ell(Body, V(0, 0.41f, 0.2f), V(0.03f, 0.032f, 0.022f), bone, mat: Shell, blend: 0.008f);
        Gape(b, head, V(0, 0.45f, 0.3f), V(0.035f, 0.02f, 0.02f), Rgb(170, 50, 50), 0.018f);
        int tail = b.Tail(V(0, 0.34f, -0.24f));
        var path = Smooth(3, V(0, 0.34f, -0.24f), V(0.02f, 0.44f, -0.36f), V(0.06f, 0.5f, -0.3f), V(0.07f, 0.47f, -0.24f));
        b.Tube(tail, path, 0.016f, 0.012f, black, blend: 0f);
        Blade(b, tail, path[^1], path[^1] + V(0.0f, -0.05f, 0.03f), 0.03f, black, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.55f), V(0.45f * s, 0.1f, 1f), 0.024f, Rgb(220, 40, 50), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Magnemite line

    /// <summary>
    /// A horseshoe magnet: a U of steel bent at <paramref name="bend"/> and opening along <paramref name="open"/>, its
    /// two prongs apart along <paramref name="across"/>, one end red and the other blue.
    /// </summary>
    private static void Magnet(PokeBuilder b, int bone, Vector3 bend, Vector3 open, Vector3 across, float size)
    {
        var u = Vector3.Normalize(open);
        var x = Vector3.Normalize(across - u * Vector3.Dot(across, u));
        float bendR = size * 0.5f, bar = size * 0.2f, prong = size * 0.8f;
        var center = bend + u * bendR;
        var a = center + x * bendR + u * prong;
        var z = center - x * bendR + u * prong;
        b.Tube(bone, new[] { a }.Concat(Arc(center, x, -u, bendR, 0f, MathF.PI, 8)).Append(z).ToArray(), bar, bar, Rgb(204, 208, 218), Metal, 0f);
        b.PaintEll(bone, a, V(prong * 0.32f, prong * 0.32f, prong * 0.32f), Rgb(228, 56, 60));
        b.PaintEll(bone, z, V(prong * 0.32f, prong * 0.32f, prong * 0.32f), Rgb(60, 150, 228));
    }

    /// <summary>A screw driven into a steel body: a short shank and a flat head with a cross cut in it.</summary>
    private static void Screw(PokeBuilder b, int bone, Vector3 at, Vector3 outward, float r, Color steel)
    {
        var n = Vector3.Normalize(outward);
        b.Limb(bone, at - n * r, at + n * r, r * 0.45f, r * 0.45f, steel, Metal);
        b.Ell(bone, at + n * r * 1.2f, V(r, r * 0.35f, r), steel, Euler(n), Metal, 0.004f);
        b.Mark(bone, at + n * r * 1.55f, n, r * 0.6f, r * 0.6f, Rgb(80, 84, 96), MarkShape.Star);
    }

    /// <summary>One of the Magnemite: a ball of steel with a single eye, a magnet on each side and screws in it.</summary>
    private static void SteelBall(PokeBuilder b, Vector3 at, float r, Color steel, int left, int right, bool topScrew = true)
    {
        b.Ell(Body, at, V(r, r, r), steel);
        if (topScrew) Screw(b, Body, at + V(0, r, 0), V(0, 1f, 0), r * 0.25f, steel);
        PokeBuilder.Both(s => Screw(b, Body, at + V(0.6f * r * s, -0.7f * r, -0.4f * r), V(0.6f * s, -0.7f, -0.4f), r * 0.2f, steel));
        foreach (var (bone, s) in new[] { (left, -1f), (right, 1f) })
        {
            if (bone < 0) continue;
            b.Limb(bone, at + V(0.9f * r * s, 0, 0), at + V(1.3f * r * s, 0, 0), r * 0.15f, r * 0.15f, steel, Metal);
            Magnet(b, bone, at + V(1.3f * r * s, 0, 0), V(s, 0, 0.35f), V(0, 1f, 0), r * 0.95f);
        }
        b.Eye(Body, at + V(0, 0, r), V(0, 0, 1f), r * 0.5f, sclera: true, pupil: Rgb(30, 30, 30));
    }

    private static PokeBuilder Magnemite()
    {
        var b = new PokeBuilder("Magnemite", 0.45f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Metal }.Hover();
        int left = b.Arm(-1f, V(-0.1f, 0.3f, 0));
        int right = b.Arm(1f, V(0.1f, 0.3f, 0));
        SteelBall(b, V(0, 0.3f, 0), 0.1f, Rgb(192, 198, 210), left, right);
        return Lift(b);
    }

    private static PokeBuilder Magneton()
    {
        var b = new PokeBuilder("Magneton", 0.8f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Metal }.Hover();
        var steel = Rgb(192, 198, 210);
        // Three Magnemite stuck together in a triangle
        int left = b.Arm(-1f, V(-0.1f, 0.48f, 0));
        int right = b.Arm(1f, V(0.1f, 0.48f, 0));
        SteelBall(b, V(0, 0.48f, 0), 0.09f, steel, left, right);
        int lowLeft = b.Part("magnetL2", Body, V(-0.18f, 0.33f, 0.02f), PokeRole.Arm, 1.3f, -1f);
        int lowRight = b.Part("magnetR2", Body, V(0.18f, 0.33f, 0.02f), PokeRole.Arm, 1.3f, 1f);
        SteelBall(b, V(-0.08f, 0.34f, 0.02f), 0.085f, steel, lowLeft, -1, false);
        SteelBall(b, V(0.08f, 0.34f, 0.02f), 0.085f, steel, -1, lowRight, false);
        PokeBuilder.Both(s =>
        {
            int under = b.Part(s < 0 ? "magnetL3" : "magnetR3", Body, V(0.08f * s, 0.26f, 0.02f), PokeRole.Leg, 0.5f, s);
            b.Limb(under, V(0.08f * s, 0.27f, 0.02f), V(0.08f * s, 0.22f, 0.02f), 0.013f, 0.013f, steel, Metal);
            Magnet(b, under, V(0.08f * s, 0.22f, 0.02f), V(0, -1f, 0.3f), V(1f, 0, 0), 0.08f);
        });
        return Lift(b);
    }

    private static PokeBuilder Magnezone()
    {
        var b = new PokeBuilder("Magnezone", 0.92f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Metal }.Hover();
        var steel = Rgb(184, 190, 206);

        // A body like a flying saucer: a dome over a wide rim, a great eye, two little bulbs with eyes of their own
        var c = V(0, 0.44f, 0);
        var r = V(0.17f, 0.13f, 0.15f);
        b.Ell(Body, c, r, steel);
        b.Ell(Body, V(0, 0.38f, 0), V(0.3f, 0.035f, 0.26f), steel, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            var bulb = V(0.17f * s, 0.44f, 0.04f);
            b.Ell(Body, bulb, V(0.055f, 0.055f, 0.055f), steel);
            b.Eye(Body, bulb + V(0.012f * s, 0, 0.053f), V(0.2f * s, 0, 1f), 0.022f, sclera: true, pupil: Rgb(30, 30, 30));
            Screw(b, Body, bulb + V(0.04f * s, 0.035f, -0.02f), V(0.7f * s, 0.6f, -0.3f), 0.018f, steel);
        });
        b.Eye(Body, On(c, r, 0, 0.47f), V(0, 0.15f, 1f), 0.045f, sclera: true, pupil: Rgb(216, 40, 50));
        // An aerial on top, and two great magnets hanging under it
        b.Limb(Body, V(0, 0.56f, 0), V(0, 0.68f, 0), 0.012f, 0.01f, Rgb(214, 190, 60), Metal);
        b.Ell(Body, V(0, 0.69f, 0), V(0.022f, 0.022f, 0.022f), Rgb(250, 214, 60), mat: Shell);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.34f, 0.02f));
            b.Limb(arm, V(0.12f * s, 0.36f, 0.02f), V(0.14f * s, 0.27f, 0.04f), 0.02f, 0.02f, steel, Metal);
            Magnet(b, arm, V(0.14f * s, 0.27f, 0.04f), V(0.15f * s, -1f, 0.4f), V(1f, 0, 0), 0.13f);
        });
        return Lift(b);
    }
}
