using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The Mega Evolutions of species of Platinum's Sinnoh Pokédex, hand-built like the species (after plan 03 · D11). Each
// is our own sculpt after the design: the species' build, changed as its Mega Evolution changes it.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Mega Raichu X and Y

    /// <summary>
    /// A bolt of lightning flat across <paramref name="facing"/>: flat spans zigzagging through
    /// <paramref name="corners"/>, each as wide as given, the last coming to a point (in <paramref name="tip"/>).
    /// </summary>
    private static void Lightning(PokeBuilder b, int bone, Vector3[] corners, float[] widths, float thickness, Color color, Vector3 facing, Color? tip = null, SurfaceMaterial? mat = null)
    {
        for (int i = 1; i < corners.Length; i++)
        {
            var d = corners[i] - corners[i - 1];
            float w = widths[Math.Min(widths.Length - 1, i - 1)];
            var y = Vector3.Normalize(d);
            if (i == corners.Length - 1)
            {
                Blade(b, bone, corners[i - 1] - y * w * 0.4f, corners[i], w, tip ?? color, facing, thickness / w, mat);
                break;
            }
            var z = Vector3.Normalize(facing - y * Vector3.Dot(facing, y));
            var x = Vector3.Cross(y, z);
            b.Box(bone, (corners[i - 1] + corners[i]) / 2f, V(w, d.Length() / 2f + w * 0.5f, thickness), Math.Min(thickness, w) * 0.6f, color, mat: mat, blend: 0.006f).Rotation =
                Quaternion.CreateFromRotationMatrix(new Matrix4x4(x.X, x.Y, x.Z, 0, y.X, y.Y, y.Z, 0, z.X, z.Y, z.Z, 0, 0, 0, 0, 1));
        }
    }

    private static PokeBuilder RaichuMegaX()
    {
        var b = new PokeBuilder("Raichu-Mega-X", 0.92f, BodyPlan.Biped, V(0, 0.62f, 0)) { Coat = Fur }.Hover();
        var yellow = Rgb(250, 214, 56);
        var orange = Rgb(250, 150, 40);
        var brown = Rgb(130, 82, 46);
        var black = Rgb(44, 36, 36);
        var white = Rgb(252, 248, 240);

        // Short brown legs striped in black, and under each a great bolt of lightning that holds it up
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.52f, 0));
            b.Limb(leg, V(0.06f * s, 0.52f, 0), V(0.075f * s, 0.4f, 0.01f), 0.042f, 0.036f, brown);
            b.Ell(leg, V(0.08f * s, 0.375f, 0.03f), V(0.042f, 0.03f, 0.055f), brown);
            b.Mark(leg, V(0.075f * s, 0.46f, 0.04f), V(0.2f * s, 0, 1f), 0.03f, 0.016f, black, MarkShape.Zigzag);
            Lightning(b, leg, new[] { V(0.08f * s, 0.36f, 0.02f), V(0.15f * s, 0.26f, 0.02f), V(0.05f * s, 0.21f, 0.02f), V(0.27f * s, -0.04f, 0.02f) },
                new[] { 0.025f, 0.028f, 0.055f }, 0.018f, yellow, V(0, 0, 1f), orange);
        });
        // A yellow body, white from the chest down
        b.Ell(Body, V(0, 0.64f, 0), V(0.12f, 0.15f, 0.11f), yellow);
        b.PaintEll(Body, V(0, 0.61f, 0.07f), V(0.09f, 0.12f, 0.07f), white);
        // Arms ending in big brown mitts, like a boxer's
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.7f, 0.02f));
            b.Limb(arm, V(0.1f * s, 0.7f, 0.02f), V(0.17f * s, 0.68f, 0.06f), 0.03f, 0.028f, yellow);
            b.Ell(arm, V(0.22f * s, 0.68f, 0.07f), V(0.07f, 0.06f, 0.065f), brown);
        });

        int head = b.Head(V(0, 0.76f, 0));
        b.Ell(head, V(0, 0.85f, 0.01f), V(0.125f, 0.105f, 0.105f), yellow);
        // Tall ears, black up from the root and yellow at the tips, little black curls beside them
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.92f, -0.01f));
            b.Spike(ear, V(0.06f * s, 0.91f, -0.01f), V(0.13f * s, 1.22f, -0.05f), 0.045f, yellow, 0.55f);
            b.PaintEll(ear, V(0.075f * s, 0.98f, -0.02f), V(0.05f, 0.08f, 0.04f), black);
            Curl(b, head, V(0.1f * s, 0.9f, 0.0f), V(0.17f * s, 0.92f, 0.0f), V(s, 0, 0), V(0, 1, 0), 0.025f, 1.2f, 0.008f, black);
        });
        // Red cheeks, a nose, a happy open mouth
        PokeBuilder.Both(s => b.Mark(head, V(0.095f * s, 0.825f, 0.075f), V(0.75f * s, -0.05f, 0.65f), 0.028f, 0.026f, Rgb(232, 52, 52)));
        b.Mark(head, V(0, 0.862f, 0.114f), V(0, 0.1f, 1f), 0.014f, 0.01f, black);
        Grin(b, head, V(0, 0.81f, 0.104f), V(0.026f, 0.016f, 0.014f), Rgb(220, 90, 90));
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.87f, 0.097f), V(0.4f * s, 0.05f, 1f), 0.026f));
        return Lift(b);
    }

    private static PokeBuilder RaichuMegaY()
    {
        var b = new PokeBuilder("Raichu-Mega-Y", 0.9f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur }.Hover();
        var orange = Rgb(244, 156, 46);
        var white = Rgb(252, 248, 240);
        var brown = Rgb(120, 74, 42);
        var black = Rgb(44, 36, 36);
        var yellow = Rgb(250, 218, 56);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.18f, 0));
            b.Limb(leg, V(0.07f * s, 0.18f, 0), V(0.09f * s, 0.06f, 0.02f), 0.05f, 0.04f, orange);
            b.Ell(leg, V(0.095f * s, 0.035f, 0.05f), V(0.05f, 0.028f, 0.072f), orange);
        });
        b.Ell(Body, V(0, 0.3f, 0), V(0.14f, 0.16f, 0.12f), orange);
        b.PaintEll(Body, V(0, 0.27f, 0.07f), V(0.1f, 0.12f, 0.08f), white);
        // A long thin brown tail ending in a great bolt
        int tail = b.Tail(V(0, 0.2f, -0.1f));
        b.Tube(tail, Smooth(3, V(0, 0.2f, -0.1f), V(0.06f, 0.08f, -0.22f), V(0.14f, 0.1f, -0.36f), V(0.2f, 0.2f, -0.44f)), 0.014f, 0.011f, brown);
        Lightning(b, tail, new[] { V(0.2f, 0.2f, -0.44f), V(0.22f, 0.4f, -0.5f), V(0.22f, 0.33f, -0.66f), V(0.24f, 0.7f, -0.78f) }, new[] { 0.032f, 0.036f, 0.075f }, 0.016f, yellow, V(1f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.38f, 0.03f));
            b.Limb(arm, V(0.11f * s, 0.38f, 0.03f), V(0.18f * s, 0.34f, 0.08f), 0.032f, 0.028f, orange);
            b.Ell(arm, V(0.195f * s, 0.33f, 0.09f), V(0.032f, 0.03f, 0.03f), orange);
        });

        int head = b.Head(V(0, 0.44f, 0));
        b.Ell(head, V(0, 0.53f, 0.01f), V(0.13f, 0.11f, 0.11f), orange);
        // Huge ears that are bolts of lightning, black at the root, curled at the tip
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.6f, -0.02f));
            Lightning(b, ear, new[] { V(0.07f * s, 0.6f, -0.03f), V(0.23f * s, 0.8f, -0.03f), V(0.1f * s, 0.85f, -0.03f), V(0.38f * s, 1.12f, -0.03f) },
                new[] { 0.035f, 0.035f, 0.075f }, 0.02f, yellow, V(0, 0, 1f));
            b.PaintEll(ear, V(0.1f * s, 0.63f, -0.03f), V(0.05f, 0.05f, 0.04f), black);
            Curl(b, ear, V(0.37f * s, 1.1f, -0.03f), V(0.41f * s, 1.07f, -0.03f), V(s, 0, 0), V(0, 1, 0), 0.03f, 1.2f, 0.01f, black);
        });
        PokeBuilder.Both(s => b.Mark(head, V(0.1f * s, 0.49f, 0.075f), V(0.75f * s, -0.05f, 0.65f), 0.03f, 0.024f, yellow));
        b.Mark(head, V(0, 0.515f, 0.119f), V(0, 0.1f, 1f), 0.011f, 0.008f, black);
        b.Mark(head, V(0, 0.49f, 0.112f), V(0, -0.2f, 1f), 0.024f, 0.008f, Rgb(90, 40, 30), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.55f, 0.1f), V(0.4f * s, 0.05f, 1f), 0.027f, Rgb(64, 150, 230), glare: true));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Mega Clefable

    private static PokeBuilder ClefableMega()
    {
        var b = new PokeBuilder("Clefable-Mega", 0.9f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur }.Hover();
        var pink = Rgb(252, 214, 222);
        var wingPink = Rgb(246, 150, 176);
        var wingYellow = Rgb(250, 222, 120);
        var dark = Rgb(56, 56, 66);
        var rim = Rgb(196, 196, 208);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.14f, 0.01f));
            b.Limb(leg, V(0.07f * s, 0.14f, 0.01f), V(0.08f * s, 0.06f, 0.03f), 0.05f, 0.044f, pink);
            b.Ell(leg, V(0.085f * s, 0.045f, 0.05f), V(0.05f, 0.032f, 0.065f), pink);
        });
        b.Ell(Body, V(0, 0.36f, 0), V(0.19f, 0.24f, 0.17f), pink);
        // Great wings like a butterfly's, pink above and yellow below
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.08f * s, 0.44f, -0.12f));
            var root = V(0.06f * s, 0.44f, -0.13f);
            Frond(b, wing, root, V(0.52f * s, 0.62f, -0.22f), 0.13f, wingPink, V(0, 0.1f, 1f), 0.12f);
            Frond(b, wing, root + V(0, -0.04f, 0), V(0.4f * s, 0.2f, -0.2f), 0.09f, wingYellow, V(0, 0.1f, 1f), 0.14f);
        });
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.43f, 0.04f));
            var hand = s < 0 ? V(-0.27f, 0.52f, 0.08f) : V(0.25f, 0.4f, 0.1f);
            b.Tube(arm, new[] { V(0.15f * s, 0.43f, 0.04f), hand }, 0.036f, 0.033f, pink);
            b.Ell(arm, hand, V(0.04f, 0.04f, 0.04f), pink);
        });
        // A happy face with its eyes shut
        PokeBuilder.Both(s => b.Mark(Body, V(0.1f * s, 0.4f, 0.15f), V(0.6f * s, 0, 1f), 0.026f, 0.016f, Rgb(240, 120, 140)));
        Grin(b, Body, V(0, 0.4f, 0.165f), V(0.035f, 0.024f, 0.016f), Rgb(226, 100, 120));
        PokeBuilder.Both(s => b.Eye(Body, V(0.055f * s, 0.47f, 0.155f), V(0.35f * s, 0.05f, 1f), 0.024f, closed: true));
        // Its ears are dark horns like speakers, turned out to the sides, and a white curl tops its head
        int head = b.Head(V(0, 0.52f, 0));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.13f * s, 0.56f, 0.0f));
            b.Limb(ear, V(0.12f * s, 0.56f, 0.0f), V(0.28f * s, 0.6f, 0.03f), 0.04f, 0.065f, dark, Shell);
            b.Torus(ear, V(0.285f * s, 0.6f, 0.03f), 0.052f, 0.014f, rim, V(0, 0, 90f), mat: Shell, blend: 0.004f);
            b.Ell(ear, V(0.29f * s, 0.6f, 0.03f), V(0.012f, 0.045f, 0.045f), Rgb(30, 30, 36), mat: Shell, blend: 0.004f);
        });
        b.Ell(head, V(0, 0.62f, -0.01f), V(0.07f, 0.06f, 0.065f), White);
        Curl(b, head, V(0, 0.66f, 0.0f), V(0.02f, 0.7f, 0.0f), V(1f, 0, 0), V(0, 1f, 0), 0.035f, 1.1f, 0.02f, White);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Mega Alakazam

    private static PokeBuilder AlakazamMega()
    {
        var b = new PokeBuilder("Alakazam-Mega", 0.92f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Fur }.Hover();
        var yellow = Rgb(240, 196, 58);
        var robe = Rgb(140, 74, 112);
        var beard = Rgb(246, 244, 240);
        var red = Rgb(214, 44, 52);

        // Sitting in the air with its legs crossed, its feet together in front
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.36f, 0.02f));
            b.Limb(leg, V(0.08f * s, 0.36f, 0.02f), V(0.22f * s, 0.3f, 0.1f), 0.055f, 0.045f, yellow);
            b.Limb(leg, V(0.22f * s, 0.3f, 0.1f), V(0.04f * s, 0.27f, 0.2f), 0.045f, 0.035f, yellow);
            b.Ell(leg, V(0.02f * s, 0.27f, 0.22f), V(0.04f, 0.03f, 0.05f), yellow);
        });
        // A body in a robe of deep red-purple, great pauldrons at its shoulders
        b.Ell(Body, V(0, 0.5f, 0), V(0.14f, 0.17f, 0.11f), robe);
        PokeBuilder.Both(s => b.Ell(Body, V(0.16f * s, 0.62f, -0.01f), V(0.11f, 0.08f, 0.1f), robe, V(0, 0, -25f * s)));
        // Arms raised to the sides, palms up
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.2f * s, 0.6f, 0.0f));
            b.Limb(arm, V(0.22f * s, 0.58f, 0.02f), V(0.34f * s, 0.5f, 0.06f), 0.045f, 0.035f, robe);
            b.Limb(arm, V(0.34f * s, 0.5f, 0.06f), V(0.38f * s, 0.66f, 0.1f), 0.03f, 0.026f, yellow);
            b.Ell(arm, V(0.385f * s, 0.69f, 0.11f), V(0.035f, 0.025f, 0.035f), yellow);
            Digits(b, arm, V(0.385f * s, 0.71f, 0.11f), V(0.2f * s, 1f, 0.1f), V(0.6f, 0, 0.4f), 0.04f, 0.011f, yellow);
        });

        int head = b.Head(V(0, 0.68f, 0));
        float hy = 0.78f;
        b.Ell(head, V(0, hy, 0.0f), V(0.11f, 0.11f, 0.11f), yellow);
        b.Ell(head, V(0, hy - 0.06f, 0.1f), V(0.05f, 0.06f, 0.07f), yellow, V(25f, 0, 0));
        // A crown of points, a red gem on its brow, its eyes shut in thought
        b.Spike(head, V(0, hy + 0.06f, -0.01f), V(0, hy + 0.24f, -0.02f), 0.06f, yellow, 0.5f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.05f * s, hy + 0.06f, -0.01f), V(0.17f * s, hy + 0.17f, -0.03f), 0.045f, yellow, 0.5f);
            b.Spike(head, V(0.08f * s, hy + 0.0f, -0.01f), V(0.2f * s, hy + 0.04f, -0.03f), 0.035f, yellow, 0.5f);
        });
        b.Ell(head, V(0, hy + 0.04f, 0.1f), V(0.018f, 0.024f, 0.012f), red, mat: Glow, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, hy + 0.005f, 0.098f), V(0.45f * s, 0.05f, 1f), 0.02f, closed: true));
        // The great white beard and moustache, falling to its lap and sweeping out to both sides
        b.Ell(head, V(0, hy - 0.14f, 0.1f), V(0.08f, 0.1f, 0.05f), beard, V(15f, 0, 0));
        PokeBuilder.Both(s =>
        {
            foreach (var (tip, r) in new[] { (V(0.18f * s, hy - 0.3f, 0.14f), 0.035f), (V(0.3f * s, hy - 0.32f, 0.08f), 0.03f), (V(0.36f * s, hy - 0.22f, 0.04f), 0.025f), (V(0.08f * s, hy - 0.36f, 0.16f), 0.035f) })
                b.Tube(head, Smooth(3, V(0.03f * s, hy - 0.07f, 0.13f), V(0.08f * s, hy - 0.15f, 0.15f), (V(0.03f * s, hy - 0.07f, 0.13f) + tip) / 2f + V(0.02f * s, -0.04f, 0.02f), tip), r, r * 0.3f, beard, blend: 0f);
        });
        // Five spoons hovering in an arc over its head
        for (int i = 0; i < 5; i++)
        {
            float a = (-50f + 25f * i) * Degree;
            var dir = V(MathF.Sin(a), MathF.Cos(a), 0);
            var bowl = V(0, hy + 0.12f, -0.03f) + dir * 0.36f;
            b.Limb(head, bowl - dir * 0.14f, bowl - dir * 0.03f, 0.011f, 0.01f, Rgb(214, 216, 226), Metal, 0.004f);
            b.Ell(head, bowl, V(0.046f, 0.046f, 0.019f), Rgb(214, 216, 226), Euler(dir), Metal, 0.006f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Mega Gengar

    private static PokeBuilder GengarMega()
    {
        var b = new PokeBuilder("Gengar-Mega", 0.9f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var purple = Rgb(78, 60, 118);
        var flame = Rgb(204, 56, 104);

        // Legs and arms that burn away to red at the ends
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.13f * s, 0.2f, 0));
            b.Limb(leg, V(0.13f * s, 0.2f, 0), V(0.16f * s, 0.06f, 0.04f), 0.08f, 0.07f, purple);
            b.Ell(leg, V(0.16f * s, 0.04f, 0.08f), V(0.08f, 0.045f, 0.1f), purple);
            Digits(b, leg, V(0.16f * s, 0.035f, 0.16f), V(0, 0, 1f), V(0.6f, 0, 0), 0.04f, 0.025f, purple);
            b.PaintEll(leg, V(0.16f * s, 0.03f, 0.1f), V(0.11f, 0.08f, 0.12f), flame, soft: 0.04f);
        });
        // A body all head and spikes, a grin full of teeth
        b.Ell(Body, V(0, 0.44f, 0), V(0.29f, 0.27f, 0.24f), purple);
        for (int i = -2; i <= 2; i++)
            b.Spike(Body, V(0.1f * i, 0.58f - 0.03f * Math.Abs(i), -0.15f), V(0.2f * i, 0.72f - 0.05f * Math.Abs(i), -0.4f), 0.075f, purple, 0.55f);
        PokeBuilder.Both(s =>
        {
            b.Spike(Body, V(0.24f * s, 0.32f, -0.04f), V(0.42f * s, 0.24f, -0.12f), 0.07f, purple, 0.5f);
            b.Spike(Body, V(0.26f * s, 0.46f, -0.06f), V(0.46f * s, 0.5f, -0.16f), 0.07f, purple, 0.5f);
        });
        int tail = b.Tail(V(0, 0.3f, -0.2f));
        b.Spike(tail, V(0, 0.3f, -0.19f), V(0, 0.22f, -0.38f), 0.07f, purple);
        b.Cut(Body, V(0, 0.345f, 0.235f), V(0.19f, 0.06f, 0.045f), blend: 0.01f);
        b.PaintEll(Body, V(0, 0.345f, 0.21f), V(0.2f, 0.07f, 0.06f), Rgb(120, 30, 56));
        b.PaintEll(Body, V(0, 0.38f, 0.21f), V(0.19f, 0.024f, 0.06f), White, soft: 0.008f);
        b.PaintEll(Body, V(0, 0.31f, 0.21f), V(0.18f, 0.022f, 0.06f), White, soft: 0.008f);
        for (int i = -3; i <= 3; i++)
            b.PaintEll(Body, V(0.05f * i, 0.345f, 0.215f), V(0.004f, 0.06f, 0.05f), Rgb(150, 140, 160), soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.26f * s, 0.4f, 0.06f));
            b.Limb(arm, V(0.26f * s, 0.4f, 0.06f), V(0.38f * s, 0.32f, 0.14f), 0.065f, 0.055f, purple);
            Digits(b, arm, V(0.39f * s, 0.31f, 0.15f), V(0.4f * s, -0.4f, 0.6f), V(0, 0.6f, 0), 0.06f, 0.024f, purple);
            b.PaintEll(arm, V(0.41f * s, 0.29f, 0.17f), V(0.07f, 0.06f, 0.07f), flame, soft: 0.03f);
        });

        // Ears like horns, red eyes in a grin, and a third eye on its brow
        int head = b.Head(V(0, 0.6f, 0));
        PokeBuilder.Both(s => b.Spike(head, V(0.15f * s, 0.63f, -0.02f), V(0.3f * s, 0.96f, -0.1f), 0.09f, purple, 0.5f));
        b.Ell(head, V(0, 0.66f, -0.02f), V(0.12f, 0.05f, 0.1f), purple);
        PokeBuilder.Both(s => b.Eye(Body, V(0.11f * s, 0.5f, 0.228f), V(0.45f * s, 0.1f, 1f), 0.042f, pupil: Rgb(50, 20, 34), white: Rgb(232, 62, 72), glare: true));
        b.Eye(Body, V(0, 0.6f, 0.19f), V(0, 0.4f, 1f), 0.034f, pupil: Rgb(50, 20, 34), white: Rgb(250, 214, 64));
        return b;
    }

    // ------------------------------------------------------------------ Mega Gyarados

    private static PokeBuilder GyaradosMega()
    {
        var b = new PokeBuilder("Gyarados-Mega", 1f, BodyPlan.Serpent, V(0.04f, 0.14f, -0.3f)) { Coat = Scales };
        var blue = Rgb(44, 120, 186);
        var belly = Rgb(70, 46, 62);
        var red = Rgb(214, 56, 60);
        var sail = Rgb(240, 228, 150);
        var navy = Rgb(30, 50, 100);
        var white = Rgb(236, 242, 248);

        var points = new[] { V(0.06f, 0.14f, -0.32f), V(-0.18f, 0.15f, -0.14f), V(-0.14f, 0.18f, 0.12f), V(0.1f, 0.26f, 0.17f), V(0.16f, 0.44f, 0.05f), V(0.05f, 0.62f, -0.03f), V(-0.01f, 0.77f, 0.03f), V(0, 0.86f, 0.1f) };
        float Radius(int i) => i switch { 0 => 0.12f, 1 => 0.14f, 2 => 0.15f, 3 => 0.15f, 4 => 0.145f, 5 => 0.13f, 6 => 0.12f, _ => 0.115f };
        int parent = Body;
        for (int i = 1; i < points.Length - 1; i++)
        {
            int seg = b.Part("seg" + i, parent, points[i], PokeRole.Segment, i);
            b.Limb(seg, points[i], points[i + 1], Radius(i), Radius(i + 1), blue);
            var mid = (points[i] + points[i + 1]) / 2f;
            var along = Vector3.Normalize(points[i + 1] - points[i]);
            float len = Vector3.Distance(points[i], points[i + 1]);
            var sideways = Vector3.Cross(along, Vector3.UnitY);
            sideways = sideways.LengthSquared() < 0.05f ? Vector3.UnitX : Vector3.Normalize(sideways);
            var back = Vector3.Normalize(Vector3.Cross(sideways, along));
            if (back.Y < 0f) back = -back;
            var front = V(0, 0, 1f) - along * along.Z;
            bool rearing = i >= 4;
            var under = rearing ? Vector3.Normalize(front) : -back;
            var top = rearing ? -under : back;
            // A dark belly, red spots down its sides, and a great sail of yellow fin along its back
            b.PaintEll(seg, mid + under * Radius(i) * 0.7f, V(Radius(i) * 0.75f, len * 0.62f, Radius(i) * 0.6f), belly, Euler(along));
            PokeBuilder.Both(s => b.Mark(seg, mid + sideways * s * Radius(i) * 0.98f, sideways * s + top * 0.3f, 0.034f, 0.024f, red));
            if (i >= 2)
            {
                var root = mid + top * Radius(i) * 0.8f;
                Blade(b, seg, root, root + (top * 1.2f + along * 0.5f) * (0.12f + 0.025f * (i - 2)), 0.07f, sail, sideways, 0.18f);
                b.Spike(seg, root, root + (top * 1.2f + along * 0.55f) * (0.13f + 0.025f * (i - 2)), 0.014f, navy, mat: Shell, blend: 0.004f);
            }
            parent = seg;
        }
        b.Limb(Body, points[0], points[1], Radius(0), Radius(1), blue);
        int tail = b.Tail(points[0]);
        b.Limb(tail, points[0], V(0.22f, 0.12f, -0.44f), 0.12f, 0.05f, blue);
        int fin = b.Part("tailFin", tail, V(0.22f, 0.12f, -0.44f), PokeRole.Fin);
        foreach (var (dx, dy) in new[] { (0.13f, 0.15f), (0.19f, 0.03f), (0.13f, -0.06f) })
            Blade(b, fin, V(0.22f, 0.12f, -0.44f), V(0.22f + dx * 0.5f, 0.13f + dy, -0.44f - dx), 0.06f, sail, V(1f, 0, 0), 0.2f);

        int head = b.Head(points[^1], parent);
        float hy = 0.94f;
        b.Ell(head, V(0, hy, 0.15f), V(0.16f, 0.13f, 0.18f), blue);
        b.Ell(head, V(0, hy - 0.005f, 0.31f), V(0.115f, 0.08f, 0.1f), blue);
        int jaw = b.Jaw(head, V(0, hy - 0.07f, 0.1f));
        b.Ell(jaw, V(0, hy - 0.17f, 0.25f), V(0.105f, 0.045f, 0.15f), blue, V(30f, 0, 0), blend: 0.006f);
        b.Ell(head, V(0, hy - 0.09f, 0.28f), V(0.085f, 0.065f, 0.11f), Rgb(232, 120, 132), V(16f, 0, 0), blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.07f * s, hy - 0.06f, 0.37f), V(0.07f * s, hy - 0.14f, 0.38f), 0.02f, White, mat: Shell, blend: 0.003f);
            b.Spike(jaw, V(0.065f * s, hy - 0.2f, 0.35f), V(0.065f * s, hy - 0.13f, 0.36f), 0.018f, White, mat: Shell, blend: 0.003f);
        });
        // A longer black crest, yellow fins swept back from the head, long white whiskers
        b.Spike(head, V(0, hy + 0.09f, 0.12f), V(0, hy + 0.36f, -0.1f), 0.06f, navy, 0.5f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.05f * s, hy + 0.08f, 0.12f), V(0.18f * s, hy + 0.3f, -0.04f), 0.045f, navy, 0.5f);
            Blade(b, head, V(0.11f * s, hy + 0.02f, 0.1f), V(0.32f * s, hy + 0.12f, -0.06f), 0.07f, sail, V(0, 1f, 0.2f), 0.2f);
            b.Tube(head, new[] { V(0.09f * s, hy - 0.05f, 0.32f), V(0.15f * s, hy - 0.08f, 0.36f), V(0.22f * s, hy - 0.16f, 0.3f), V(0.3f * s, hy - 0.2f, 0.18f), V(0.36f * s, hy - 0.16f, 0.02f) }, 0.012f, 0.007f, white);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.105f * s, hy + 0.045f, 0.26f), V(0.75f * s, 0.25f, 0.6f), 0.034f, Rgb(214, 54, 54), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Mega Steelix

    private static PokeBuilder SteelixMega()
    {
        var path = ArchPath;
        var b = new PokeBuilder("Steelix-Mega", 1f, BodyPlan.Serpent, path[0]) { Coat = Metal };
        var steel = Rgb(136, 152, 184);
        var dark = Rgb(62, 68, 88);
        var hex = Rgb(60, 170, 236);
        var crystals = new[] { Rgb(246, 206, 230), Rgb(196, 230, 250), Rgb(222, 210, 250) };

        // Cut-steel segments, each with a blue hexagon at its side, and crystals jutting from them in every colour
        float Size(int i) => 0.065f + 0.09f * MathF.Min(1f, i / 6f);
        var bones = Boulders(b, path, Size, i => V(0, i * 41f % 30f - 15f, 0), steel, Metal, 0.25f);
        int neck = bones[^1];
        for (int i = 1; i < path.Length; i++)
        {
            int seg = bones[i];
            float r = Size(i);
            var at = path[i];
            var back = Vector3.Normalize(path[i - 1] - path[Math.Min(path.Length - 1, i + 1)]);
            var side = Vector3.Normalize(Vector3.Cross(back, Vector3.UnitY));
            PokeBuilder.Both(s => HexCell(b, seg, at + side * s * r * 1.02f, side * s, r * 0.45f, 0.01f, hex));
            if (i % 2 == 0)
                PokeBuilder.Both(s => Blade(b, seg, at + V(r * 0.7f * s, r * 0.5f, 0), at + V((r + 0.12f) * s, r + 0.1f, 0) + back * (r + 0.08f), r * 0.4f, crystals[(i / 2 + (s > 0 ? 1 : 0)) % 3], V(0, 0, 1f), 0.3f, Shell));
        }
        int tail = b.Tail(path[0]);
        Blade(b, tail, path[0], path[0] + V(-0.14f, 0.08f, 0.12f), 0.05f, crystals[0], V(0, 1f, 0), 0.3f, Shell);
        Blade(b, tail, path[0], path[0] + V(-0.17f, 0.01f, 0.04f), 0.045f, crystals[1], V(0, 1f, 0), 0.3f, Shell);

        int head = b.Head(V(0.0f, 0.86f, 0.04f), neck);
        // A longer head and a jaw with jagged edges, crystals crowning it
        b.Box(head, V(0, 0.93f, 0.17f), V(0.16f, 0.085f, 0.25f), 0.045f, steel, V(-6f, 0, 0), Metal, 0.015f);
        int jaw = b.Jaw(head, V(0, 0.84f, 0.04f));
        b.Box(jaw, V(0, 0.79f, 0.21f), V(0.14f, 0.065f, 0.22f), 0.04f, steel, V(6f, 0, 0), Metal, 0.008f);
        PokeBuilder.Both(s =>
        {
            for (int k = 0; k < 3; k++)
                b.Spike(jaw, V(0.135f * s, 0.8f, 0.08f + 0.1f * k), V(0.19f * s, 0.76f, 0.06f + 0.1f * k), 0.03f, steel, 0.5f, Metal);
        });
        b.PaintEll(head, V(0, 0.848f, 0.36f), V(0.16f, 0.012f, 0.13f), dark);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, V(0.09f * s, 0.98f, -0.02f), V(0.2f * s, 1.12f, -0.24f), 0.05f, crystals[s > 0 ? 1 : 2], V(1f, 0, 0), 0.3f, Shell);
            b.PaintEll(head, V(0.1f * s, 0.95f, 0.32f), V(0.05f, 0.03f, 0.05f), dark);
        });
        Blade(b, head, V(0, 1.0f, -0.02f), V(0, 1.16f, -0.24f), 0.05f, crystals[0], V(1f, 0, 0), 0.3f, Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.085f * s, 0.95f, 0.4f), V(0.3f * s, 0.15f, 0.95f), 0.024f, Rgb(214, 40, 44), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Mega Scizor

    /// <summary>One of Mega Scizor's great pincers at <paramref name="at"/>: two red jaws edged with teeth, an eye-spot on the outside.</summary>
    private static void MegaPincer(PokeBuilder b, int bone, Vector3 at, float s, Color red, Color teeth, Color eye)
    {
        foreach (float up in new[] { 1f, -1f })
        {
            var jaw = at + V(0.02f * s, 0.075f * up, 0);
            b.Ell(bone, jaw, V(0.13f, 0.05f, 0.075f), red, V(0, 0, -10f * up * s));
            for (int k = 0; k < 4; k++)
            {
                var root = jaw + V((0.1f - 0.055f * k) * s + 0.03f * s, -0.03f * up, 0.0f);
                b.Spike(bone, root, root + V(0.012f * s, -0.05f * up, 0), 0.018f, teeth, 0.6f, Shell, 0.004f);
            }
        }
        b.Ell(bone, at + V(-0.1f * s, 0, 0), V(0.06f, 0.07f, 0.065f), Rgb(60, 60, 70));
        b.Mark(bone, at + V(-0.03f * s, 0.08f, 0.075f), V(0, 0.2f, 1f), 0.026f, 0.026f, eye, MarkShape.Ring);
        b.Mark(bone, at + V(-0.03f * s, 0.08f, 0.0755f), V(0, 0.2f, 1f), 0.012f, 0.012f, Rgb(40, 40, 48));
    }

    private static PokeBuilder ScizorMega()
    {
        var b = new PokeBuilder("Scizor-Mega", 0.92f, BodyPlan.Biped, V(0, 0.46f, 0)) { Coat = Metal };
        var red = Rgb(206, 44, 50);
        var dark = Rgb(62, 62, 72);
        var wing = Rgb(232, 236, 242);
        var teal = Rgb(64, 200, 200);

        // Long legs of red armour coming to points, dark joints
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.34f, -0.02f));
            b.Ell(leg, V(0.08f * s, 0.32f, -0.01f), V(0.05f, 0.05f, 0.05f), dark);
            b.Limb(leg, V(0.08f * s, 0.3f, -0.01f), V(0.13f * s, 0.15f, 0.03f), 0.045f, 0.03f, red);
            b.Spike(leg, V(0.13f * s, 0.16f, 0.03f), V(0.15f * s, 0.0f, 0.01f), 0.03f, red);
            b.PaintEll(leg, V(0.15f * s, 0.01f, 0.01f), V(0.02f, 0.03f, 0.02f), Claw);
        });
        // A dark waist of rings between a red abdomen and a broad red thorax, pale wings behind
        b.Ell(Body, V(0, 0.4f, -0.05f), V(0.075f, 0.08f, 0.09f), red);
        b.Ell(Body, V(0, 0.47f, 0.0f), V(0.055f, 0.045f, 0.055f), dark);
        b.Ell(Body, V(0, 0.56f, 0.02f), V(0.1f, 0.1f, 0.08f), red);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.04f * s, 0.6f, -0.05f));
            RimmedFin(b, w, V(0.1f * s, 0.66f, -0.06f), V(0.04f, 0.12f, 0.006f), V(-15f, 0, -45f * s), wing, Rgb(196, 200, 212), 0.01f);
        });
        // Arms of dark joints ending in pincers bigger than its body
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.62f, 0.03f));
            b.Ell(arm, V(0.11f * s, 0.62f, 0.03f), V(0.05f, 0.05f, 0.05f), dark);
            b.Limb(arm, V(0.12f * s, 0.62f, 0.03f), V(0.2f * s, 0.66f, 0.06f), 0.03f, 0.028f, red);
            MegaPincer(b, arm, V(0.34f * s, 0.68f, 0.08f), s, red, Rgb(232, 232, 236), teal);
        });
        int head = b.Head(V(0, 0.66f, 0.04f));
        var c = V(0, 0.72f, 0.06f);
        var r = V(0.06f, 0.065f, 0.065f);
        b.Ell(head, c, r, red);
        // A crest of red points, teal eyes
        b.Spike(head, V(0, 0.77f, 0.06f), V(0, 0.9f, 0.0f), 0.03f, red, 0.5f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.035f * s, 0.75f, 0.05f), V(0.11f * s, 0.85f, -0.01f), 0.024f, red, 0.5f);
            b.Spike(head, V(0.05f * s, 0.71f, 0.05f), V(0.12f * s, 0.72f, 0.0f), 0.02f, red, 0.5f);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.03f * s, 0.73f), V(0.5f * s, 0.1f, 1f), 0.016f, teal, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Mega Heracross

    private static PokeBuilder HeracrossMega()
    {
        var b = new PokeBuilder("Heracross-Mega", 0.9f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Shell };
        var blue = Rgb(52, 74, 124);
        var seam = Rgb(236, 112, 44);
        var orange = Rgb(236, 100, 40);
        var yellow = Rgb(250, 214, 60);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.24f, 0));
            b.Limb(leg, V(0.11f * s, 0.24f, 0), V(0.15f * s, 0.1f, 0.03f), 0.075f, 0.06f, blue);
            b.Ell(leg, V(0.15f * s, 0.04f, 0.04f), V(0.07f, 0.04f, 0.09f), blue);
            b.Spike(leg, V(0.15f * s, 0.03f, 0.1f), V(0.17f * s, 0.01f, 0.17f), 0.022f, Claw);
            b.Spike(leg, V(0.19f * s, 0.1f, 0.0f), V(0.26f * s, 0.06f, 0.0f), 0.025f, Claw, mat: Shell);
        });
        // A round armoured belly with bands across it, plates outlined in orange
        b.Ell(Body, V(0, 0.4f, 0), V(0.19f, 0.2f, 0.16f), blue);
        for (int i = 0; i < 4; i++)
            b.PaintEll(Body, V(0, 0.32f + i * 0.055f, 0.14f), V(0.13f, 0.007f, 0.04f), PixelCanvas.Mix(blue, Rgb(10, 16, 34), 0.45f), default, 0.008f);
        b.PaintTorus(Body, V(0, 0.4f, 0.0f), 0.185f, 0.006f, seam, V(0, 0, 90f), 1f, 0.86f);
        // Great round armoured arms raised either side of the head
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.52f, 0.02f));
            b.Ell(arm, V(0.22f * s, 0.58f, 0.0f), V(0.13f, 0.15f, 0.13f), blue);
            b.PaintTorus(arm, V(0.22f * s, 0.58f, 0.0f), 0.13f, 0.006f, seam, V(0, 0, 90f), 1f, 1f);
            b.Ell(arm, V(0.2f * s, 0.76f, 0.06f), V(0.08f, 0.07f, 0.08f), blue);
            Digits(b, arm, V(0.18f * s, 0.8f, 0.13f), V(-0.3f * s, 0.3f, 1f), V(0.5f, 0, 0.3f), 0.05f, 0.02f, Claw, Shell);
        });

        int head = b.Head(V(0, 0.56f, 0.04f));
        b.Ell(head, V(0, 0.6f, 0.1f), V(0.085f, 0.08f, 0.08f), blue);
        // An orange plate over its face with a fierce yellow eye in it, and the great horn rising high over everything
        b.PaintEll(head, V(0, 0.6f, 0.17f), V(0.07f, 0.055f, 0.03f), orange);
        PokeBuilder.Both(s => b.Eye(head, V(0.035f * s, 0.6f, 0.17f), V(0.45f * s, 0.05f, 1f), 0.018f, yellow, glare: true));
        b.Tube(head, Smooth(3, V(0, 0.64f, 0.12f), V(0, 0.8f, 0.1f), V(0.02f, 1.0f, 0.04f), V(0.06f, 1.18f, -0.04f)), 0.05f, 0.024f, blue, Shell, 0f);
        b.Spike(head, V(0.06f, 1.16f, -0.03f), V(0.08f, 1.26f, -0.08f), 0.026f, blue);
        b.Spike(head, V(0.01f, 0.86f, 0.08f), V(0.07f, 0.92f, 0.12f), 0.024f, blue, 0.5f);
        return b;
    }

    // ------------------------------------------------------------------ Mega Houndoom

    private static PokeBuilder HoundoomMega()
    {
        var b = new PokeBuilder("Houndoom-Mega", 0.9f, BodyPlan.Quadruped, V(0, 0.32f, -0.03f)) { Coat = Fur };
        var black = Rgb(48, 48, 58);
        var orange = Rgb(236, 128, 52);
        var bone = Rgb(238, 238, 232);
        const float k = 1.35f;
        var (head, c, r) = Hound(b, k, black, orange, bone);
        // Long horns rising high and bending in at their tips, an armour of bone over its chest and shoulders
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, V(0.04f * s, 0.6f, 0.2f), V(0.08f * s, 0.72f, 0.16f), V(0.13f * s, 0.86f, 0.1f), V(0.12f * s, 0.98f, 0.08f), V(0.07f * s, 1.04f, 0.1f)), 0.03f, 0.01f, bone, Shell, 0f);
            int ear = b.Ear(head, s, V(0.055f * s, 0.6f, 0.17f));
            b.Spike(ear, V(0.055f * s, 0.59f, 0.17f), V(0.09f * s, 0.66f, 0.12f), 0.03f, black, 0.5f);
        });
        b.Ell(Body, V(0, 0.42f, 0.17f), V(0.12f, 0.09f, 0.07f), bone, mat: Shell, blend: 0.02f);
        b.Spike(Body, V(0, 0.36f, 0.22f), V(0, 0.26f, 0.27f), 0.04f, bone, 0.5f, Shell);
        PokeBuilder.Both(s =>
        {
            foreach (var (tip, w) in new[] { (V(0.24f * s, 0.5f, 0.12f), 0.045f), (V(0.22f * s, 0.36f, 0.16f), 0.04f), (V(0.2f * s, 0.58f, 0.02f), 0.04f) })
                Blade(b, Body, V(0.08f * s, 0.43f, 0.13f), tip, w, bone, V(0, 0.3f, 1f), 0.3f, Shell);
            Blade(b, Body, V(0.06f * s, 0.4f, -0.12f), V(0.2f * s, 0.46f, -0.2f), 0.035f, bone, V(0, 1f, 0), 0.3f, Shell);
        });
        Gape(b, head, V(0, 0.45f, 0.3f), V(0.035f, 0.02f, 0.02f), Rgb(170, 50, 50), 0.018f);
        // A long tail curling up behind it to an arrowhead
        int tail = b.Tail(V(0, 0.34f, -0.24f));
        var path = Smooth(3, V(0, 0.34f, -0.24f), V(0.02f, 0.46f, -0.4f), V(0.07f, 0.62f, -0.44f), V(0.11f, 0.7f, -0.36f));
        b.Tube(tail, path, 0.016f, 0.012f, black, blend: 0f);
        Blade(b, tail, path[^1], path[^1] + V(0.04f, 0.03f, 0.07f), 0.045f, black, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.55f), V(0.45f * s, 0.1f, 1f), 0.024f, Rgb(220, 40, 50), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Mega Gardevoir

    private static PokeBuilder GardevoirMega()
    {
        var b = new PokeBuilder("Gardevoir-Mega", 0.92f, BodyPlan.Biped, V(0, 0.7f, 0)) { Coat = Fur };
        var green = Rgb(124, 204, 112);
        var red = Rgb(230, 60, 90);
        var fold = Rgb(236, 236, 248);

        // A ball gown belling out to the ground in great pleats, its train swept out behind to one side
        b.Limb(Body, V(0, 0.62f, 0), V(0, 0.02f, -0.03f), 0.05f, 0.26f, White);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f + 0.35f;
            var out_ = V(MathF.Sin(a), 0, MathF.Cos(a));
            var foot = out_ * 0.27f + V(0, 0.06f, -0.03f);
            var waist = out_ * 0.05f + V(0, 0.6f, -0.01f);
            Frond(b, Body, foot - V(0, 0.05f, 0), waist, 0.1f, i % 2 == 0 ? White : fold, out_, 0.7f, blend: 0.03f);
        }
        b.CutBox(Body, V(0, -0.3f, 0), V(0.7f, 0.3f, 0.7f), Quaternion.Identity);
        Frond(b, Body, V(0.15f, 0.05f, -0.15f), V(0.55f, 0.012f, -0.42f), 0.15f, White, V(0, 1f, 0), 0.12f);
        b.Ell(Body, V(0, 0.71f, 0), V(0.06f, 0.1f, 0.05f), White);
        Blade(b, Body, V(0, 0.72f, 0.035f), V(0, 0.8f, 0.15f), 0.045f, red, V(1f, 0, 0), 0.3f);
        Blade(b, Body, V(0, 0.72f, -0.035f), V(0, 0.8f, -0.15f), 0.045f, red, V(1f, 0, 0), 0.3f);
        // Long white arms in gloves to the shoulder
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.78f, 0.0f));
            var hand = s < 0 ? V(-0.28f, 0.66f, 0.1f) : V(0.3f, 0.8f, 0.06f);
            b.Tube(arm, Smooth(3, V(0.06f * s, 0.78f, 0.0f), (V(0.06f * s, 0.78f, 0) + hand) / 2f + V(0, 0.02f, 0.02f), hand), 0.022f, 0.017f, White);
            b.Ell(arm, hand, V(0.022f, 0.026f, 0.018f), White);
        });
        int head = b.Head(V(0, 0.83f, 0));
        var c = V(0, 0.89f, 0.03f);
        var r = V(0.075f, 0.075f, 0.072f);
        b.Ell(head, c, r, White);
        b.Ell(head, V(0, 0.935f, -0.01f), V(0.1f, 0.08f, 0.095f), green);
        PokeBuilder.Both(s => b.Ell(head, V(0.075f * s, 0.865f, 0.0f), V(0.038f, 0.085f, 0.07f), green));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.03f * s, 0.89f), V(0.3f * s, 0.05f, 1f), 0.018f, Rgb(220, 50, 80)));
        return b;
    }

    // ------------------------------------------------------------------ Mega Medicham

    private static PokeBuilder MedichamMega()
    {
        var b = new PokeBuilder("Medicham-Mega", 0.9f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Fur }.Hover();
        var pink = Rgb(214, 54, 98);
        var white = Rgb(236, 236, 242);
        var gold = Rgb(244, 178, 64);

        // Floating, its legs folded up inside great round trousers dotted with gold
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.24f, 0));
            b.Ell(leg, V(0.08f * s, 0.12f, 0.02f), V(0.04f, 0.05f, 0.04f), white);
        });
        b.Ell(Body, V(0, 0.27f, 0), V(0.16f, 0.15f, 0.14f), pink);
        foreach (var dir in new[] { V(-0.6f, 0.2f, 0.75f), V(0.55f, -0.2f, 0.8f), V(0.9f, 0.3f, 0.2f), V(-0.9f, -0.2f, 0.1f), V(0.1f, -0.6f, 0.75f), V(0.2f, 0.4f, -0.9f), V(-0.5f, -0.3f, -0.8f) })
        {
            var n = Vector3.Normalize(dir);
            b.PaintEll(Body, V(0, 0.27f, 0) + n * V(0.16f, 0.15f, 0.14f), V(0.032f, 0.032f, 0.032f), gold);
        }
        b.Ell(Body, V(0, 0.43f, 0.0f), V(0.11f, 0.05f, 0.1f), gold, mat: Shell);
        b.Ell(Body, V(0, 0.52f, 0), V(0.055f, 0.1f, 0.05f), white);
        // Hands pressed together before its chest, gold bands at the wrists
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.035f * s, 0.58f, 0));
            b.Tube(arm, Smooth(3, V(0.03f * s, 0.58f, 0), V(0.12f * s, 0.52f, 0.05f), V(0.03f * s, 0.55f, 0.1f)), 0.022f, 0.018f, white);
            b.Torus(arm, V(0.07f * s, 0.535f, 0.08f), 0.02f, 0.008f, gold, V(0, 0, 90f), mat: Shell, blend: 0.004f);
            b.Ell(arm, V(0.012f * s, 0.58f, 0.11f), V(0.014f, 0.04f, 0.022f), white);
        });
        // Four more arms spread wide as ribbons, frayed at their ends
        foreach (var (y, lift, i) in new[] { (0.6f, 0.18f, 0), (0.54f, -0.06f, 1) })
            PokeBuilder.Both(s =>
            {
                int extra = b.Part((s < 0 ? "ribbonL" : "ribbonR") + i, Body, V(0.03f * s, y, -0.02f), PokeRole.Arm, s + 1.4f * i, s);
                var path = Smooth(3, V(0.02f * s, y, -0.015f), V(0.18f * s, y + lift * 0.4f, -0.03f), V(0.3f * s, y + lift * 0.8f, -0.02f), V(0.42f * s, y + lift, 0.0f));
                b.Tube(extra, path, 0.02f, 0.03f, white, blend: 0f);
                Frond(b, extra, path[^1], path[^1] + V(0.08f * s, 0.01f, 0.01f), 0.04f, white, V(0, 0.2f, 1f), 0.4f);
            });

        int head = b.Head(V(0, 0.64f, 0));
        b.Limb(head, V(0, 0.6f, 0.0f), V(0, 0.68f, 0.015f), 0.03f, 0.03f, white);
        b.Ell(head, V(0, 0.71f, 0.02f), V(0.072f, 0.078f, 0.072f), white);
        // A headdress of white petals with a red bulb on top
        for (int i = 0; i < 5; i++)
        {
            float a = (i - 2) * 0.55f;
            Petal(b, head, V(0, 0.76f, -0.01f), V(MathF.Sin(a) * 0.13f, 0.76f + MathF.Cos(a) * 0.13f, 0.02f), 0.045f, white, Fur);
        }
        b.Ell(head, V(0, 0.92f, -0.01f), V(0.04f, 0.05f, 0.04f), Rgb(214, 54, 80), mat: Shell);
        b.PaintEll(head, V(0, 0.66f, 0.08f), V(0.022f, 0.01f, 0.02f), Rgb(200, 60, 80));
        PokeBuilder.Both(s => b.Eye(head, V(0.032f * s, 0.72f, 0.085f), V(0.4f * s, 0.05f, 1f), 0.02f, Rgb(120, 190, 230)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Mega Altaria

    private static PokeBuilder AltariaMega()
    {
        var b = new PokeBuilder("Altaria-Mega", 0.9f, BodyPlan.Bird, V(0, 0.4f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(176, 222, 240);

        // A slender body all but lost in a great cloud, two long streamers of tail
        b.Ell(Body, V(0, 0.4f, -0.02f), V(0.11f, 0.12f, 0.11f), blue);
        foreach (var (at, size) in new[] { (V(0, 0.38f, 0.08f), 0.13f), (V(0, 0.5f, -0.06f), 0.12f), (V(0, 0.3f, -0.08f), 0.12f), (V(0, 0.56f, 0.06f), 0.09f) })
            Puff(b, Body, at, size);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.1f * s, 0.44f, -0.02f));
            Puff(b, w, V(0.22f * s, 0.48f, -0.03f), 0.13f);
            Puff(b, w, V(0.36f * s, 0.42f, -0.06f), 0.11f);
            Puff(b, w, V(0.27f * s, 0.3f, -0.04f), 0.1f);
        });
        int tail = b.Tail(V(0, 0.28f, -0.12f));
        foreach (float x in new[] { -0.03f, 0.03f })
            b.Tube(tail, Smooth(4, V(x, 0.28f, -0.12f), V(x * 2f, 0.18f, -0.22f), V(x * 3f + 0.05f, 0.06f, -0.3f), V(x * 4f + 0.12f, 0.0f, -0.42f)), 0.025f, 0.032f, blue, blend: 0f);
        b.Tube(Body, Smooth(3, V(0, 0.5f, 0.08f), V(0, 0.62f, 0.12f), V(0, 0.72f, 0.1f)), 0.04f, 0.034f, blue, blend: 0.015f);
        int head = b.Head(V(0, 0.72f, 0.1f));
        var c = V(0, 0.77f, 0.12f);
        var r = V(0.06f, 0.05f, 0.07f);
        b.Ell(head, c, r, blue);
        // A pale beak, a fluffy white crest
        b.Spike(head, V(0, 0.755f, 0.18f), V(0, 0.735f, 0.24f), 0.02f, Rgb(214, 226, 240), mat: Shell);
        Puff(b, head, V(0, 0.84f, 0.08f), 0.05f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.035f * s, 0.785f), V(0.6f * s, 0.1f, 0.8f), 0.017f, sclera: true, pupil: Rgb(30, 30, 40)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Mega Chimecho

    private static PokeBuilder ChimechoMega()
    {
        var b = new PokeBuilder("Chimecho-Mega", 0.86f, BodyPlan.Floating, V(0, 0.55f, 0)) { Coat = Metal }.Hover();
        var gold = Rgb(236, 190, 70);
        var blue = Rgb(196, 228, 246);
        var red = Rgb(226, 62, 80);
        var green = Rgb(90, 180, 110);

        // A great golden ring with knobs at its sides and two small rings set in it
        var c = V(0, 0.55f, 0);
        b.Torus(Body, c, 0.3f, 0.026f, gold, V(90f, 0, 0), mat: Metal, blend: 0.006f);
        b.Limb(Body, c + V(-0.3f, 0, 0), c + V(0.3f, 0, 0), 0.016f, 0.016f, gold, Metal);
        PokeBuilder.Both(s =>
        {
            b.Torus(Body, c + V(0.15f * s, 0, 0), 0.06f, 0.016f, gold, V(90f, 0, 0), mat: Metal, blend: 0.006f);
            b.Limb(Body, c + V(0.29f * s, 0, 0), c + V(0.38f * s, 0, 0), 0.03f, 0.035f, gold, Metal);
            b.Ell(Body, c + V(0.4f * s, 0, 0), V(0.04f, 0.045f, 0.04f), gold, mat: Metal);
        });
        // On top a golden knot, and a bow of two great red and white striped loops
        b.Ell(Body, c + V(0, 0.31f, 0), V(0.045f, 0.035f, 0.04f), gold, mat: Metal);
        PokeBuilder.Both(s =>
        {
            int loop = b.Part(s < 0 ? "bowL" : "bowR", Body, c + V(0.03f * s, 0.32f, 0), PokeRole.Ear, s, s);
            var path = Smooth(3, c + V(0.03f * s, 0.32f, 0), c + V(0.12f * s, 0.38f, 0), c + V(0.22f * s, 0.45f, 0), c + V(0.3f * s, 0.52f, 0));
            b.Tube(loop, path, 0.03f, 0.06f, red, Shell, 0f);
            for (int i = 2; i < path.Length - 1; i += 2)
                b.PaintTorus(loop, path[i], 0.03f + 0.03f * i / path.Length + 0.004f, 0.011f, White, Euler(path[i + 1] - path[i - 1]));
            b.Ell(loop, path[^1] + V(0.01f * s, 0.02f, 0), V(0.06f, 0.06f, 0.055f), red, mat: Shell);
        });
        // Chimecho itself hangs in the middle, its long tail ending in a strip of red paper
        int head = b.Head(c + V(0, 0.26f, 0));
        b.Limb(head, c + V(0, 0.29f, 0), c + V(0, 0.16f, 0), 0.016f, 0.016f, blue);
        b.Ell(head, c + V(0, 0.1f, 0.02f), V(0.075f, 0.065f, 0.07f), blue);
        PokeBuilder.Both(s => b.Mark(head, c + V(0.055f * s, 0.09f, 0.065f), V(0.8f * s, 0, 0.6f), 0.02f, 0.015f, red));
        PokeBuilder.Both(s => b.Eye(head, c + V(0.028f * s, 0.11f, 0.086f), V(0.35f * s, 0.05f, 1f), 0.014f, closed: true));
        int tail = b.Tail(c + V(0, 0.04f, 0.02f));
        b.Tube(tail, new[] { c + V(0, 0.04f, 0.02f), c + V(0, -0.1f, 0.02f), c + V(0, -0.2f, 0.02f) }, 0.03f, 0.042f, blue);
        b.Ell(tail, c + V(0, -0.27f, 0.02f), V(0.05f, 0.08f, 0.02f), red, blend: 0.02f);
        // Little chimes hanging from the lower half of the ring on strips of every colour
        var colors = new[] { red, blue, green, blue, green, red };
        for (int i = 0; i < 6; i++)
        {
            float a = (-55f + 22f * i) * Degree;
            var top = c + V(MathF.Sin(a) * 0.3f, -MathF.Cos(a) * 0.3f, 0);
            b.Limb(Body, top, top + V(0, -0.05f, 0), 0.008f, 0.008f, gold, Metal);
            b.Ell(Body, top + V(0, -0.075f, 0), V(0.024f, 0.028f, 0.024f), blue, mat: Shell);
            b.Ell(Body, top + V(0, -0.14f, 0.0f), V(0.018f, 0.05f, 0.008f), colors[i], blend: 0.012f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Mega Absol, and its Z form

    /// <summary>Absol's build, shared by its two Mega Evolutions: four legs, a long body, a ruff, a head with a crescent horn.</summary>
    private static (int head, Vector3 hc, Vector3 hr) MegaAbsolBody(PokeBuilder b, Color fur, Color face, Color claw, Color horn, float hornWidth)
    {
        foreach (var (z, front) in new[] { (0.12f, true), (-0.18f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.08f * s, 0.32f, z), front);
                b.Limb(leg, V(0.08f * s, 0.34f, z), V(0.09f * s, 0.16f, z + 0.01f), 0.048f, 0.036f, fur);
                b.Limb(leg, V(0.09f * s, 0.16f, z + 0.01f), V(0.09f * s, 0.04f, z + 0.02f), 0.034f, 0.03f, fur);
                // Tufts of fur standing out round each ankle
                for (int i = 0; i < 3; i++)
                    b.Spike(leg, V(0.09f * s, 0.1f, z + 0.01f), V((0.09f + 0.05f * (i - 1) * 0.6f + 0.03f) * s, 0.12f + 0.02f * i, z - 0.04f + 0.03f * i), 0.022f, fur, 0.5f);
                b.Ell(leg, V(0.09f * s, 0.03f, z + 0.04f), V(0.035f, 0.025f, 0.045f), face);
                for (int i = -1; i <= 1; i++)
                    b.Spike(leg, V(0.09f * s + 0.018f * i, 0.03f, z + 0.07f), V(0.09f * s + 0.024f * i, 0.0f, z + 0.11f), 0.011f, claw, mat: Shell, blend: 0.004f);
            });
        b.Ell(Body, V(0, 0.4f, -0.04f), V(0.12f, 0.12f, 0.24f), fur);
        for (int i = 0; i < 9; i++)
        {
            float a = (i - 4f) * 0.36f;
            var root = V(MathF.Sin(a) * 0.1f, 0.44f, 0.17f + MathF.Cos(a) * 0.03f);
            b.Spike(Body, root, root + V(MathF.Sin(a) * 0.06f, -0.16f, 0.05f), 0.045f, fur, 0.6f, blend: 0.02f);
        }
        b.Tube(Body, new[] { V(0, 0.45f, 0.14f), V(0, 0.52f, 0.2f), V(0, 0.56f, 0.22f) }, 0.07f, 0.06f, fur, blend: 0f);
        int head = b.Head(V(0, 0.55f, 0.2f));
        var hc = V(0, 0.58f, 0.25f);
        var hr = V(0.085f, 0.085f, 0.09f);
        b.Ell(head, hc, hr, fur);
        b.Ell(head, V(0, 0.55f, 0.32f), V(0.05f, 0.045f, 0.05f), face, blend: 0.02f);
        b.PaintEll(head, V(0, 0.565f, 0.31f), V(0.075f, 0.05f, 0.06f), face);
        b.Tube(head, Smooth(3, V(0.06f, 0.62f, 0.24f), V(0.14f, 0.73f, 0.2f), V(0.13f, 0.86f, 0.12f), V(0.04f, 0.92f, 0.05f), V(-0.06f, 0.87f, 0.03f)), hornWidth, 0.006f, horn, Shell, 0f);
        return (head, hc, hr);
    }

    private static PokeBuilder AbsolMega()
    {
        var b = new PokeBuilder("Absol-Mega", 0.9f, BodyPlan.Quadruped, V(0, 0.4f, -0.04f)) { Coat = Fur };
        var white = Rgb(240, 242, 250);
        var shade = Rgb(196, 204, 230);
        var blue = Rgb(64, 96, 162);
        var (head, hc, hr) = MegaAbsolBody(b, white, blue, blue, blue, 0.036f);
        // Its fur rises off its back into a pair of great white wings
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.06f * s, 0.5f, 0.0f));
            for (int i = 0; i < 5; i++)
            {
                var root = V(0.06f * s, 0.5f, 0.04f - 0.05f * i);
                var tip = V((0.22f + 0.04f * i) * s, 0.8f - 0.07f * i, -0.04f - 0.07f * i);
                Blade(b, wing, root, tip, 0.06f, i % 2 == 0 ? white : shade, V(0.3f * s, 0.3f, 1f), 0.25f);
            }
        });
        int tail = b.Tail(V(0, 0.44f, -0.26f));
        Sickle(b, tail, V(0, 0.45f, -0.26f), V(0, 0.66f, -0.4f), V(0.0f, 0.84f, -0.34f), 0.06f, blue, V(1f, 0, 0));
        b.Mark(head, On(hc, hr, 0, 0.64f), V(0, 0.5f, 1f), 0.012f, 0.02f, blue);
        PokeBuilder.Both(s => b.Eye(head, V(0.035f * s, 0.585f, 0.322f), V(0.4f * s, 0.1f, 1f), 0.015f, Rgb(220, 40, 50), glare: true));
        return b;
    }

    private static PokeBuilder AbsolMegaZ()
    {
        var b = new PokeBuilder("Absol-Mega-Z", 0.9f, BodyPlan.Quadruped, V(0, 0.4f, -0.04f)) { Coat = Fur };
        var navy = Rgb(30, 42, 66);
        var deep = Rgb(18, 26, 44);
        var claw = Rgb(70, 90, 130);
        var red = Rgb(226, 54, 92);
        var (head, hc, hr) = MegaAbsolBody(b, navy, deep, claw, deep, 0.05f);
        // Plumes of red rising from its back like a crest of fire, a long mane fallen over one side of its face
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.05f * s, 0.5f, 0.0f));
            for (int i = 0; i < 4; i++)
            {
                var root = V(0.05f * s, 0.5f, 0.02f - 0.06f * i);
                var tip = V((0.12f + 0.06f * i) * s, 0.95f - 0.08f * i, -0.12f - 0.08f * i);
                Blade(b, wing, root, tip, 0.05f, red, V(0.4f * s, 0.2f, 1f), 0.25f);
                b.PaintEll(wing, Vector3.Lerp(root, tip, 0.15f), V(0.06f, 0.1f, 0.06f), navy);
            }
        });
        b.Tube(head, Smooth(3, V(-0.02f, 0.66f, 0.24f), V(-0.07f, 0.62f, 0.3f), V(-0.08f, 0.52f, 0.31f), V(-0.06f, 0.44f, 0.28f)), 0.04f, 0.012f, navy, blend: 0f);
        int tail = b.Tail(V(0, 0.44f, -0.26f));
        Sickle(b, tail, V(0, 0.45f, -0.26f), V(0, 0.64f, -0.42f), V(0.0f, 0.8f, -0.36f), 0.06f, deep, V(1f, 0, 0));
        b.Eye(head, V(0.035f, 0.585f, 0.322f), V(0.4f, 0.1f, 1f), 0.017f, sclera: true, pupil: Rgb(20, 20, 30), white: red, glare: true);
        b.Eye(head, V(-0.035f, 0.585f, 0.322f), V(-0.4f, 0.1f, 1f), 0.017f, closed: true);
        return b;
    }

    // ------------------------------------------------------------------ Mega Glalie

    private static PokeBuilder GlalieMega()
    {
        var b = new PokeBuilder("Glalie-Mega", 0.9f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var ice = Rgb(224, 234, 246);
        var black = Rgb(40, 40, 50);
        var crystal = Rgb(140, 214, 240);
        var maw = Rgb(40, 58, 90);
        var cyan = Rgb(70, 200, 240);

        // A great black boulder, rough all round, and a mask of ice over its top half
        var c = V(0, 0.42f, 0);
        var r = V(0.26f, 0.26f, 0.24f);
        b.Ell(Body, c, r, black);
        Rubble(b, Body, c, V(0.25f, 0.25f, 0.22f), 0.06f, black, 20, 79u);
        b.Ell(Body, V(0, 0.55f, 0.12f), V(0.23f, 0.15f, 0.14f), ice);
        // Its jaw has broken open on a great dark maw ringed with teeth
        b.Cut(Body, V(0, 0.3f, 0.24f), V(0.18f, 0.13f, 0.1f), blend: 0.012f);
        b.PaintEll(Body, V(0, 0.3f, 0.19f), V(0.19f, 0.14f, 0.1f), maw);
        Vector3 Rim(float a)
        {
            // From the middle of the maw outward over the boulder's surface until it leaves the cut
            var last = V(0, 0.3f, 0.24f);
            for (float t = 0.1f; t < 1.8f; t += 0.01f)
            {
                float x = MathF.Sin(a) * 0.18f * t, y = 0.3f - MathF.Cos(a) * 0.13f * t;
                float u = x / r.X, v = (y - c.Y) / r.Y;
                if (u * u + v * v >= 1f) break;
                last = V(x, y, c.Z + r.Z * MathF.Sqrt(1f - u * u - v * v));
                float cx = x / 0.18f, cy = (y - 0.3f) / 0.13f, cz = (last.Z - 0.24f) / 0.1f;
                if (cx * cx + cy * cy + cz * cz >= 1f) break;
            }
            return last;
        }
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f;
            var rim = Rim(a);
            var inward = Vector3.Normalize(V(0, 0.3f, rim.Z - 0.03f) - rim);
            b.Spike(Body, rim - inward * 0.012f, rim + inward * 0.045f, 0.02f, White, 0.6f, Shell, 0.004f);
        }
        // Horns of ice crystal, a black brow over angry cyan eyes
        PokeBuilder.Both(s =>
        {
            Blade(b, Body, V(0.12f * s, 0.62f, 0.05f), V(0.24f * s, 0.86f, -0.02f), 0.07f, crystal, V(0, 0.2f, 1f), 0.45f, Shell);
            b.Spike(Body, V(0.22f * s, 0.6f, -0.08f), V(0.36f * s, 0.74f, -0.12f), 0.06f, black, 0.6f, Shell);
            b.Spike(Body, V(0.24f * s, 0.3f, -0.02f), V(0.38f * s, 0.2f, 0.02f), 0.05f, crystal, 0.6f, Shell);
        });
        b.PaintEll(Body, V(0, 0.58f, 0.24f), V(0.19f, 0.03f, 0.06f), black);
        PokeBuilder.Both(s =>
        {
            var at = On(V(0, 0.55f, 0.12f), V(0.23f, 0.15f, 0.14f), 0.08f * s, 0.53f);
            b.Eye(Body, at, Outward(V(0, 0.55f, 0.12f), V(0.23f, 0.15f, 0.14f), at), 0.036f, cyan, glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Mega Staraptor

    private static PokeBuilder StaraptorMega()
    {
        var b = new PokeBuilder("Staraptor-Mega", 0.92f, BodyPlan.Bird, V(0, 0.4f, 0)) { Coat = Fur }.Hover();
        var gray = Rgb(176, 172, 178);
        var dark = Rgb(70, 64, 70);
        var orange = Rgb(242, 98, 52);
        var gold = Rgb(246, 186, 64);

        // Yellow legs with dark talons, gripping
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.24f, 0.02f));
            b.Limb(leg, V(0.07f * s, 0.24f, 0.02f), V(0.08f * s, 0.1f, 0.05f), 0.03f, 0.024f, gold, Scales, 0.008f);
            Digits(b, leg, V(0.08f * s, 0.08f, 0.07f), V(0, -0.6f, 1f), V(0.6f, 0, 0), 0.05f, 0.012f, Rgb(40, 36, 40), Shell);
        });
        b.Ell(Body, V(0, 0.4f, 0), V(0.16f, 0.17f, 0.18f), gray);
        b.PaintEll(Body, V(0, 0.4f, 0.1f), V(0.12f, 0.14f, 0.1f), White);
        int tail = b.Tail(V(0, 0.32f, -0.16f));
        foreach (float a in new[] { -26f, -9f, 9f, 26f })
            b.Ell(tail, V(MathF.Sin(a * Degree) * 0.08f, 0.3f, -0.28f), V(0.05f, 0.025f, 0.15f), MathF.Abs(a) < 10f ? dark : gray, V(-18f, a, 0), blend: 0.012f);
        // Great dark wings spread wide, banded in gold
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.13f * s, 0.48f, 0.0f));
            b.Limb(wing, V(0.13f * s, 0.48f, 0.0f), V(0.3f * s, 0.62f, -0.04f), 0.05f, 0.035f, dark);
            for (int i = 0; i < 6; i++)
            {
                var root = V((0.16f + 0.03f * i) * s, 0.5f + 0.022f * i, -0.02f - 0.005f * i);
                var tip = root + V((0.22f + 0.02f * i) * s, 0.26f - 0.08f * i, -0.06f);
                Blade(b, wing, root, tip, 0.05f, i < 2 ? gray : dark, V(0, 0.1f, 1f), 0.25f);
                b.PaintEll(wing, Vector3.Lerp(root, tip, 0.55f), V(0.02f, 0.02f, 0.05f), gold);
            }
        });

        int head = b.Head(V(0, 0.52f, 0.08f));
        float hy = 0.64f;
        b.Ell(head, V(0, hy, 0.1f), V(0.11f, 0.105f, 0.11f), White);
        b.Ell(head, V(0, hy + 0.04f, 0.04f), V(0.11f, 0.09f, 0.1f), gray, blend: 0.03f);
        // A hooked beak, an orange-red crest sweeping forward, orange marks round the eyes
        b.Spike(head, V(0, hy - 0.02f, 0.19f), V(0, hy - 0.08f, 0.29f), 0.04f, gold, 0.8f, Shell);
        Blade(b, head, V(0, hy + 0.07f, 0.06f), V(0, hy + 0.22f, 0.26f), 0.05f, orange, V(1f, 0, 0), 0.3f);
        Blade(b, head, V(0, hy + 0.08f, 0.02f), V(0, hy + 0.24f, 0.12f), 0.04f, orange, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s =>
        {
            b.Mark(head, V(0.07f * s, hy + 0.0f, 0.17f), V(0.55f * s, 0.05f, 1f), 0.03f, 0.014f, orange, MarkShape.Bar, -20f * s);
            b.Eye(head, V(0.06f * s, hy + 0.03f, 0.18f), V(0.55f * s, 0.05f, 1f), 0.026f, Rgb(246, 186, 64), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Mega Lopunny

    private static PokeBuilder LopunnyMega()
    {
        var b = new PokeBuilder("Lopunny-Mega", 0.88f, BodyPlan.Biped, V(0, 0.56f, 0)) { Coat = Fur };
        var brown = Rgb(150, 100, 70);
        var tights = Rgb(70, 46, 40);
        var cream = Rgb(246, 228, 172);

        // Long legs in dark tights, fluffy cream cuffs at the ankles
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.44f, 0));
            b.Limb(leg, V(0.07f * s, 0.44f, 0), V(0.09f * s, 0.24f, 0.04f), 0.055f, 0.04f, tights);
            b.Limb(leg, V(0.09f * s, 0.24f, 0.04f), V(0.09f * s, 0.08f, 0.0f), 0.04f, 0.035f, tights);
            b.PaintEll(leg, V(0.1f * s, 0.32f, 0.05f), V(0.03f, 0.05f, 0.03f), brown);
            foreach (var (dx, dz) in new[] { (0.045f, 0.0f), (-0.035f, 0.03f), (0.0f, -0.045f) })
                b.Ell(leg, V((0.09f + dx) * s, 0.11f, 0.01f + dz), V(0.045f, 0.04f, 0.045f), cream);
            b.Ell(leg, V(0.095f * s, 0.03f, 0.05f), V(0.05f, 0.03f, 0.08f), cream);
        });
        b.Ell(Body, V(0, 0.56f, 0), V(0.1f, 0.16f, 0.08f), brown);
        b.PaintEll(Body, V(0, 0.46f, 0.0f), V(0.11f, 0.07f, 0.09f), tights);
        int tail = b.Tail(V(0, 0.46f, -0.07f));
        b.Ell(tail, V(0, 0.46f, -0.11f), V(0.045f, 0.045f, 0.045f), cream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.66f, 0.01f));
            var hand = s < 0 ? V(-0.2f, 0.74f, 0.08f) : V(0.15f, 0.52f, 0.07f);
            b.Limb(arm, V(0.09f * s, 0.66f, 0.01f), hand, 0.03f, 0.026f, brown);
            foreach (var (dx, dy) in new[] { (0.035f, 0.0f), (-0.03f, 0.02f), (0.0f, -0.035f) })
                b.Ell(arm, hand + V(dx, dy, 0), V(0.038f, 0.034f, 0.038f), cream);
        });

        int head = b.Head(V(0, 0.72f, 0));
        float hy = 0.82f;
        b.Ell(head, V(0, hy, 0.01f), V(0.11f, 0.1f, 0.1f), brown);
        b.Mark(head, V(0, hy - 0.03f, 0.105f), V(0, 0.1f, 1f), 0.014f, 0.01f, Rgb(232, 130, 150));
        // Its ears tied back behind its head, their cream tips flying, a cream fringe over its brow
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, hy + 0.08f, -0.01f));
            b.Tube(ear, Smooth(3, V(0.06f * s, hy + 0.08f, -0.01f), V(0.08f * s, hy + 0.2f, -0.06f), V(0.06f * s, hy + 0.24f, -0.16f), V(0.12f * s, hy + 0.16f, -0.26f)), 0.04f, 0.03f, brown, blend: 0f);
            Frond(b, ear, V(0.12f * s, hy + 0.16f, -0.26f), V(0.22f * s, hy + 0.06f, -0.3f), 0.05f, cream, V(0, 1f, 0.2f), 0.4f);
            b.Ell(head, V(0.05f * s, hy + 0.07f, 0.06f), V(0.05f, 0.03f, 0.05f), cream, V(0, 0, -20f * s));
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, hy + 0.01f, 0.09f), V(0.45f * s, 0.05f, 1f), 0.03f, Rgb(224, 74, 96), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Mega Garchomp, and its Z form

    /// <summary>Garchomp's build, shared by its two Mega Evolutions: legs, body, tail and head, spikes on the legs.</summary>
    private static int MegaGarchompBody(PokeBuilder b, Color navy, Color belly, Color yellow, Color spikes)
    {
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.46f, -0.02f));
            b.Limb(leg, V(0.12f * s, 0.46f, -0.02f), V(0.16f * s, 0.22f, 0.06f), 0.1f, 0.08f, navy);
            b.Limb(leg, V(0.16f * s, 0.22f, 0.06f), V(0.15f * s, 0.05f, 0.0f), 0.07f, 0.06f, navy);
            b.Ell(leg, V(0.15f * s, 0.035f, 0.05f), V(0.07f, 0.035f, 0.1f), navy);
            b.Spike(leg, V(0.2f * s, 0.28f, 0.0f), V(0.32f * s, 0.3f, -0.1f), 0.04f, spikes, mat: Shell);
            b.Spike(leg, V(0.2f * s, 0.4f, -0.04f), V(0.3f * s, 0.46f, -0.12f), 0.035f, spikes, mat: Shell);
            Claws(b, leg, V(0.15f * s, 0.02f, 0.13f), V(0.03f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.013f);
        });
        b.Ell(Body, V(0, 0.62f, 0), V(0.19f, 0.26f, 0.16f), navy);
        b.PaintEll(Body, V(0, 0.6f, 0.07f), V(0.13f, 0.23f, 0.12f), belly);
        // A chevron of yellow down its belly
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.035f * s, 0.5f, 0.14f), V(0.012f, 0.06f, 0.03f), yellow, V(0, 0, 35f * s), 0.008f));
        int tail = b.Tail(V(0, 0.44f, -0.12f));
        b.Limb(tail, V(0, 0.44f, -0.12f), V(0, 0.2f, -0.46f), 0.09f, 0.05f, navy);
        b.Spike(tail, V(0, 0.34f, -0.26f), V(0, 0.48f, -0.38f), 0.055f, navy, 0.35f);
        PokeBuilder.Both(s => b.Spike(tail, V(0.04f * s, 0.24f, -0.42f), V(0.14f * s, 0.24f, -0.52f), 0.04f, navy, 0.35f));
        int head = b.Head(V(0, 0.86f, 0));
        float hy = 0.98f;
        b.Ell(head, V(0, hy, 0.08f), V(0.13f, 0.1f, 0.22f), navy);
        int jaw = b.Jaw(head, V(0, hy - 0.04f, 0.0f));
        b.Ell(jaw, V(0, hy - 0.06f, 0.14f), V(0.11f, 0.04f, 0.14f), belly, blend: 0.006f);
        PokeBuilder.Both(s => b.Ell(head, V(0.18f * s, hy + 0.02f, 0.0f), V(0.12f, 0.03f, 0.12f), navy, V(0, 30f * s, -20f * s), blend: 0.015f));
        b.Spike(head, V(0, hy + 0.06f, -0.06f), V(0, hy + 0.14f, -0.28f), 0.05f, navy, 0.4f);
        PokeBuilder.Both(s => b.Eye(head, V(0.085f * s, hy + 0.03f, 0.2f), V(0.7f * s, 0.05f, 1f), 0.03f, yellow, glare: true));
        return head;
    }

    private static PokeBuilder GarchompMega()
    {
        var b = new PokeBuilder("Garchomp-Mega", 0.95f, BodyPlan.Biped, V(0, 0.62f, 0)) { Coat = Scales };
        var navy = Rgb(64, 80, 140);
        var red = Rgb(214, 74, 60);
        var yellow = Rgb(250, 206, 70);
        int head = MegaGarchompBody(b, navy, red, yellow, Claw);
        b.Mark(head, V(0, 1.06f, 0.14f), V(0, 1f, 0.35f), 0.045f, 0.045f, yellow, MarkShape.Star);
        // Its arms have become great scythes, red along their edges
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.78f, 0));
            b.Limb(arm, V(0.17f * s, 0.78f, 0), V(0.28f * s, 0.62f, 0.08f), 0.06f, 0.05f, navy);
            Sickle(b, arm, V(0.28f * s, 0.62f, 0.08f), V(0.44f * s, 0.84f, 0.04f), V(0.52f * s, 0.56f, 0.1f), 0.08f, red, V(0.2f, 0.1f, 1f));
            Sickle(b, arm, V(0.28f * s, 0.6f, 0.06f), V(0.42f * s, 0.8f, 0.0f), V(0.48f * s, 0.6f, 0.04f), 0.05f, navy, V(0.2f, 0.1f, 1f));
        });
        return b;
    }

    private static PokeBuilder GarchompMegaZ()
    {
        var b = new PokeBuilder("Garchomp-Mega-Z", 0.95f, BodyPlan.Biped, V(0, 0.62f, 0)) { Coat = Scales };
        var navy = Rgb(52, 46, 86);
        var orange = Rgb(226, 90, 48);
        var yellow = Rgb(250, 200, 64);
        int head = MegaGarchompBody(b, navy, orange, yellow, Claw);
        // A long red lance of a horn, and great red wings edged with white spikes where its arms were
        b.Spike(head, V(0, 1.06f, 0.1f), V(0, 1.24f, 0.46f), 0.025f, orange, mat: Shell);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.78f, 0));
            b.Limb(arm, V(0.17f * s, 0.78f, 0), V(0.3f * s, 0.86f, 0.0f), 0.06f, 0.05f, navy);
            var root = V(0.3f * s, 0.86f, 0.0f);
            var tip = V(0.7f * s, 1.06f, -0.1f);
            var p = Frond(b, arm, root, tip, 0.11f, orange, V(0, 0.2f, 1f), 0.18f);
            var along = Vector3.Normalize(tip - root);
            var across = Vector3.Transform(Vector3.UnitX, p.Rotation);
            if (across.Y < 0) across = -across;
            for (int i = 1; i <= 4; i++)
            {
                var at = Vector3.Lerp(root, tip, i / 5f) + across * 0.1f * MathF.Sin(MathF.PI * i / 5f);
                b.Spike(arm, at, at + across * 0.07f + along * 0.02f, 0.02f, Claw, mat: Shell, blend: 0.004f);
            }
        });
        return b;
    }

    // ------------------------------------------------------------------ Mega Lucario, and its Z form

    private static PokeBuilder LucarioMega()
    {
        var b = new PokeBuilder("Lucario-Mega", 0.92f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Fur };
        var blue = Rgb(70, 128, 206);
        var black = Rgb(40, 42, 58);
        var cream = Rgb(238, 210, 130);
        var red = Rgb(200, 52, 52);

        // Legs black at the thigh, red at the foot, white spikes at the knees
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.46f, 0));
            b.Limb(leg, V(0.1f * s, 0.46f, 0), V(0.12f * s, 0.24f, 0.04f), 0.075f, 0.06f, black);
            b.Limb(leg, V(0.12f * s, 0.24f, 0.04f), V(0.12f * s, 0.06f, 0.0f), 0.055f, 0.05f, blue);
            b.Ell(leg, V(0.12f * s, 0.035f, 0.05f), V(0.065f, 0.035f, 0.1f), red);
            b.PaintEll(leg, V(0.12f * s, 0.1f, 0.01f), V(0.07f, 0.05f, 0.07f), red);
            b.Spike(leg, V(0.15f * s, 0.24f, 0.06f), V(0.2f * s, 0.26f, 0.14f), 0.022f, Claw, mat: Shell);
        });
        // A blue body striped in black, spiky cream fur on its chest
        b.Ell(Body, V(0, 0.62f, 0), V(0.16f, 0.22f, 0.13f), blue);
        b.PaintEll(Body, V(0, 0.66f, 0.06f), V(0.12f, 0.13f, 0.1f), cream);
        for (int i = -2; i <= 2; i++)
            b.Spike(Body, V(i * 0.04f, 0.6f, 0.11f), V(i * 0.05f, 0.5f, 0.14f), 0.03f, cream, 0.5f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.14f * s, 0.6f, 0.0f), V(0.03f, 0.12f, 0.1f), black, V(0, 0, 18f * s)));
        b.Spike(Body, V(0, 0.68f, 0.15f), V(0, 0.7f, 0.27f), 0.035f, Claw, mat: Shell);
        b.PaintEll(Body, V(0, 0.455f, 0), V(0.19f, 0.075f, 0.16f), black);
        // A longer tail, spiked like a flame
        int tail = b.Tail(V(0, 0.44f, -0.1f));
        b.Limb(tail, V(0, 0.46f, -0.06f), V(0, 0.4f, -0.2f), 0.05f, 0.05f, cream);
        b.Ell(tail, V(0, 0.38f, -0.24f), V(0.05f, 0.09f, 0.14f), cream, V(-30f, 0, 0));
        foreach (var (dx, dy) in new[] { (0.06f, 0.04f), (-0.06f, 0.0f), (0.0f, -0.08f) })
            b.Spike(tail, V(0, 0.36f, -0.28f), V(dx, 0.32f + dy, -0.42f), 0.035f, cream, 0.5f);
        // Arms red to the elbow and black above it, spikes on the backs of its paws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.78f, 0));
            b.Limb(arm, V(0.15f * s, 0.78f, 0), V(0.25f * s, 0.56f, 0.06f), 0.05f, 0.045f, black);
            b.Ell(arm, V(0.26f * s, 0.52f, 0.07f), V(0.055f, 0.06f, 0.055f), red);
            b.Spike(arm, V(0.26f * s, 0.54f, 0.02f), V(0.31f * s, 0.57f, -0.09f), 0.026f, Claw, mat: Shell);
            b.Spike(arm, V(0.28f * s, 0.5f, 0.02f), V(0.34f * s, 0.48f, -0.06f), 0.02f, Claw, mat: Shell);
        });

        int head = b.Head(V(0, 0.84f, 0));
        float hy = 0.98f;
        b.Ell(head, V(0, hy, 0.02f), V(0.15f, 0.14f, 0.15f), blue);
        b.Ell(head, V(0, hy - 0.05f, 0.13f), V(0.07f, 0.05f, 0.08f), black);
        b.PaintEll(head, V(0, hy + 0.02f, 0.06f), V(0.19f, 0.05f, 0.16f), black);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, hy + 0.1f, 0));
            b.Spike(ear, V(0.08f * s, hy + 0.08f, 0), V(0.14f * s, hy + 0.3f, -0.03f), 0.05f, blue, 0.5f);
            // Four long black appendages flowing from the back of its head, tipped in red
            foreach (var (dy, reach) in new[] { (0.02f, 0.0f), (-0.06f, 0.06f) })
            {
                var path = Smooth(3, V(0.06f * s, hy + dy, -0.1f), V((0.16f + reach) * s, hy + dy + 0.02f, -0.18f), V((0.28f + reach) * s, hy + dy - 0.04f, -0.2f), V((0.38f + reach) * s, hy + dy - 0.1f, -0.16f));
                b.Tube(ear, path, 0.034f, 0.03f, black, blend: 0f);
                b.PaintEll(ear, path[^1], V(0.05f, 0.05f, 0.05f), red);
            }
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.065f * s, hy + 0.02f, 0.14f), V(0.45f * s, 0.05f, 1f), 0.035f, Rgb(232, 120, 40), glare: true));
        return b;
    }

    private static PokeBuilder LucarioMegaZ()
    {
        var b = new PokeBuilder("Lucario-Mega-Z", 0.92f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Fur };
        var teal = Rgb(36, 134, 148);
        var dark = Rgb(56, 60, 70);
        var yellow = Rgb(232, 224, 120);
        var ring = Rgb(170, 180, 190);

        // Teal legs ringed in grey, grey discs at the knees
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.46f, 0));
            b.Limb(leg, V(0.1f * s, 0.46f, 0), V(0.12f * s, 0.24f, 0.04f), 0.075f, 0.06f, dark);
            b.Limb(leg, V(0.12f * s, 0.24f, 0.04f), V(0.12f * s, 0.06f, 0.0f), 0.058f, 0.052f, teal);
            b.Ell(leg, V(0.12f * s, 0.035f, 0.05f), V(0.065f, 0.035f, 0.1f), teal);
            b.PaintTorus(leg, V(0.12f * s, 0.15f, 0.02f), 0.055f, 0.01f, ring);
            b.Ell(leg, V(0.135f * s, 0.25f, 0.08f), V(0.03f, 0.03f, 0.016f), ring, Toward(V(0.3f * s, 0, 1f)), Shell, 0.006f);
        });
        // A teal body, a great fall of yellow fur from its chest to its hips
        b.Ell(Body, V(0, 0.62f, 0), V(0.16f, 0.22f, 0.13f), teal);
        b.PaintEll(Body, V(0, 0.66f, 0.06f), V(0.12f, 0.13f, 0.1f), yellow);
        for (int i = 0; i < 7; i++)
        {
            float x = (i - 3) * 0.04f;
            b.Spike(Body, V(x, 0.64f, 0.1f), V(x * 1.4f, 0.4f - 0.03f * (i % 2), 0.15f), 0.035f, yellow, 0.5f);
        }
        b.PaintEll(Body, V(0, 0.455f, 0), V(0.19f, 0.075f, 0.16f), dark);
        int tail = b.Tail(V(0, 0.44f, -0.12f));
        Blade(b, tail, V(0, 0.42f, -0.14f), V(0.0f, 0.3f, -0.46f), 0.07f, dark, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.78f, 0));
            b.Limb(arm, V(0.15f * s, 0.78f, 0), V(0.25f * s, 0.56f, 0.06f), 0.05f, 0.048f, teal);
            b.PaintTorus(arm, V(0.22f * s, 0.62f, 0.04f), 0.048f, 0.01f, ring, Euler(V(0.4f * s, -1f, 0.25f)));
            b.Ell(arm, V(0.26f * s, 0.52f, 0.07f), V(0.06f, 0.06f, 0.06f), teal);
        });
        // A long teal scarf streaming from its shoulder
        int scarf = b.Part("scarf", Body, V(0.12f, 0.8f, -0.04f), PokeRole.Leaf);
        Frond(b, scarf, V(0.12f, 0.8f, -0.04f), V(0.5f, 0.66f, -0.18f), 0.08f, teal, V(0, 1f, 0.3f), 0.15f);

        int head = b.Head(V(0, 0.84f, 0));
        float hy = 0.98f;
        b.Ell(head, V(0, hy, 0.02f), V(0.15f, 0.14f, 0.15f), dark);
        b.Ell(head, V(0, hy - 0.05f, 0.13f), V(0.07f, 0.05f, 0.08f), dark);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, hy + 0.1f, 0));
            b.Spike(ear, V(0.08f * s, hy + 0.08f, 0), V(0.14f * s, hy + 0.32f, -0.03f), 0.05f, dark, 0.5f);
            // Ribbons of teal flowing long from the back of its head, wavy at the ends
            var path = Smooth(3, V(0.06f * s, hy, -0.1f), V(0.18f * s, hy + 0.06f, -0.2f), V(0.32f * s, hy - 0.06f, -0.22f), V(0.4f * s, hy - 0.24f, -0.16f), V(0.36f * s, hy - 0.4f, -0.1f));
            b.Tube(ear, path, 0.02f, 0.024f, teal, blend: 0f);
            for (int i = 0; i < 3; i++)
                b.Spike(ear, path[^(1 + i * 2)], path[^(1 + i * 2)] + V(0.04f * s, -0.02f, 0.01f), 0.014f, teal, 0.5f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.065f * s, hy + 0.02f, 0.14f), V(0.45f * s, 0.05f, 1f), 0.032f, Rgb(80, 160, 230), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Mega Abomasnow

    private static PokeBuilder AbomasnowMega()
    {
        var b = new PokeBuilder("Abomasnow-Mega", 1f, BodyPlan.Biped, V(0, 0.46f, 0)) { Coat = Fur };
        var teal = Rgb(66, 138, 132);
        var snow = Rgb(240, 244, 248);
        var shade = Rgb(206, 214, 232);
        var ice = Rgb(150, 206, 236);
        var purple = Rgb(176, 146, 214);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.16f, 0.0f));
            b.Limb(leg, V(0.12f * s, 0.16f, 0.0f), V(0.14f * s, 0.05f, 0.03f), 0.08f, 0.07f, teal);
            Digits(b, leg, V(0.14f * s, 0.03f, 0.07f), V(0, -0.2f, 1f), V(0.6f, 0, 0), 0.07f, 0.03f, teal);
        });
        // A huge body heaped with snow: fringes of snow standing out all over it like laden branches
        var c = V(0, 0.48f, 0);
        var r = V(0.27f, 0.32f, 0.22f);
        b.Ell(Body, c, r, snow);
        for (int row = 0; row < 5; row++)
        {
            float y = -0.75f + 0.36f * row;
            int n = row == 4 ? 7 : 11;
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f * (row % 2)) * MathF.Tau / n;
                float ring = MathF.Sqrt(1f - y * y);
                var dir = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
                if (dir.Z > 0.6f && y > -0.2f && y < 0.75f && MathF.Abs(dir.X) < 0.45f) continue;
                var root = c + dir * r * 0.92f;
                var tip = c + dir * r * 1.32f + V(0, -0.04f, 0);
                Blade(b, Body, root, tip, 0.05f, (i + row) % 3 == 0 ? shade : snow, V(0, 1f, 0) - dir * Vector3.Dot(V(0, 1f, 0), dir) + V(0.001f, 0, 0), 0.3f);
            }
        }
        // Crystals of ice rising off its shoulders and back
        PokeBuilder.Both(s =>
        {
            Blade(b, Body, V(0.18f * s, 0.7f, -0.06f), V(0.26f * s, 0.94f, -0.1f), 0.05f, ice, V(0, 0.1f, 1f), 0.45f, Shell);
            Blade(b, Body, V(0.08f * s, 0.74f, -0.12f), V(0.12f * s, 0.96f, -0.2f), 0.04f, ice, V(0, 0.1f, 1f), 0.45f, Shell);
        });
        // Arms shaggy with snow, teal claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.22f * s, 0.56f, 0.0f));
            b.Limb(arm, V(0.22f * s, 0.56f, 0.0f), V(0.36f * s, 0.42f, 0.06f), 0.09f, 0.08f, snow);
            foreach (var t in new[] { 0.3f, 0.6f, 0.9f })
            {
                var at = Vector3.Lerp(V(0.22f * s, 0.56f, 0.0f), V(0.36f * s, 0.42f, 0.06f), t);
                Blade(b, arm, at, at + V(0.07f * s, -0.07f, -0.02f), 0.04f, snow, V(0, 1f, 0), 0.3f);
                Blade(b, arm, at, at + V(0.02f * s, -0.06f, 0.07f), 0.035f, shade, V(0, 1f, 0), 0.3f);
            }
            Digits(b, arm, V(0.38f * s, 0.36f, 0.09f), V(0.2f * s, -1f, 0.3f), V(0.6f, 0, 0.3f), 0.06f, 0.025f, teal);
        });
        // A pale face in the snow: purple eyes under a frown, a pink beak of a nose
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.075f * s, 0.6f), V(0.35f * s, 0.1f, 1f), 0.026f, pupil: Rgb(90, 50, 130), white: purple, glare: true));
        b.Spike(Body, On(c, r, 0, 0.53f) - V(0, 0, 0.02f), On(c, r, 0, 0.5f) + V(0, -0.03f, 0.07f), 0.035f, Rgb(232, 140, 140), 0.6f, Shell);
        return b;
    }

    // ------------------------------------------------------------------ Mega Gallade

    private static PokeBuilder GalladeMega()
    {
        var b = new PokeBuilder("Gallade-Mega", 0.94f, BodyPlan.Biped, V(0, 0.62f, 0)) { Coat = Fur };
        var green = Rgb(40, 160, 90);
        var teal = Rgb(124, 200, 204);
        var red = Rgb(226, 60, 80);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.52f, 0.0f));
            b.Limb(leg, V(0.05f * s, 0.52f, 0.0f), V(0.12f * s, 0.32f, 0.02f), 0.07f, 0.035f, White);
            b.Limb(leg, V(0.12f * s, 0.32f, 0.02f), V(0.13f * s, 0.05f, 0.0f), 0.026f, 0.02f, Rgb(200, 200, 208));
            b.Spike(leg, V(0.13f * s, 0.05f, 0.0f), V(0.14f * s, 0.01f, 0.08f), 0.026f, Rgb(200, 200, 208));
        });
        b.Ell(Body, V(0, 0.6f, 0), V(0.075f, 0.11f, 0.06f), White);
        b.Ell(Body, V(0, 0.5f, 0), V(0.14f, 0.05f, 0.1f), White);
        b.Ell(Body, V(0, 0.7f, 0), V(0.09f, 0.06f, 0.065f), White);
        Blade(b, Body, V(0, 0.71f, 0.05f), V(0, 0.8f, 0.16f), 0.035f, red, V(1f, 0, 0), 0.3f);
        Blade(b, Body, V(0, 0.71f, -0.05f), V(0, 0.8f, -0.16f), 0.035f, red, V(1f, 0, 0), 0.3f);
        // A long sword of red and white from one elbow, and from the other a great cape of teal
        int left = b.Arm(-1f, V(-0.08f, 0.72f, 0.0f));
        b.Limb(left, V(-0.08f, 0.72f, 0.0f), V(-0.16f, 0.62f, 0.05f), 0.028f, 0.024f, White);
        b.Limb(left, V(-0.16f, 0.62f, 0.05f), V(-0.2f, 0.52f, 0.12f), 0.024f, 0.02f, White);
        Blade(b, left, V(-0.16f, 0.62f, 0.05f), V(-0.48f, 0.9f, 0.1f), 0.05f, red, V(0.3f, 0.3f, 1f), 0.2f);
        Blade(b, left, V(-0.17f, 0.6f, 0.05f), V(-0.44f, 0.82f, 0.11f), 0.025f, White, V(0.3f, 0.3f, 1f), 0.3f);
        int right = b.Arm(1f, V(0.08f, 0.72f, 0.0f));
        b.Limb(right, V(0.08f, 0.72f, 0.0f), V(0.18f, 0.66f, 0.04f), 0.028f, 0.024f, White);
        b.Limb(right, V(0.18f, 0.66f, 0.04f), V(0.28f, 0.68f, 0.08f), 0.024f, 0.02f, White);
        Frond(b, right, V(0.18f, 0.66f, 0.0f), V(0.46f, 0.3f, -0.22f), 0.13f, teal, V(0.5f, 0.3f, 1f), 0.12f);
        int head = b.Head(V(0, 0.76f, 0.01f));
        var c = V(0, 0.82f, 0.03f);
        var r = V(0.072f, 0.076f, 0.075f);
        b.Ell(head, c, r, White);
        // A tall crest like a helmet, green in front and teal behind, red eyes
        Blade(b, head, V(0, 0.86f, 0.05f), V(0, 1.02f, 0.13f), 0.045f, green, V(1f, 0, 0), 0.3f);
        Blade(b, head, V(0, 0.88f, -0.02f), V(0, 1.06f, -0.12f), 0.06f, teal, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.05f * s, 0.82f, 0.0f), V(0.14f * s, 0.87f, -0.05f), 0.028f, White, 0.5f);
            b.Eye(head, On(c, r, 0.026f * s, 0.825f), V(0.35f * s, 0.05f, 1f), 0.016f, Rgb(220, 50, 80), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Mega Froslass

    private static PokeBuilder FroslassMega()
    {
        var b = new PokeBuilder("Froslass-Mega", 0.95f, BodyPlan.Floating, V(0, 0.5f, 0)) { Coat = Fur }.Hover();
        var white = Rgb(244, 244, 250);
        var lilac = Rgb(170, 140, 214);
        var ice = Rgb(176, 220, 244);
        var purple = Rgb(132, 82, 184);
        var sash = Rgb(220, 70, 50);

        // A tall gown of snow fading to purple at its hem, flowing down onto a drift of snow
        b.Limb(Body, V(0, 0.66f, 0), V(0, 0.08f, -0.02f), 0.07f, 0.13f, white);
        b.PaintEll(Body, V(0, 0.08f, -0.02f), V(0.16f, 0.2f, 0.16f), lilac, soft: 0.06f);
        foreach (var (x, y) in new[] { (-0.04f, 0.3f), (0.05f, 0.22f), (0.0f, 0.14f) })
            b.Mark(Body, V(x, y, 0.13f - y * 0.12f), V(x * 2f, 0.1f, 1f), 0.016f, 0.016f, ice, MarkShape.Star);
        foreach (var (at, size) in new[] { (V(0, 0.04f, 0.02f), 0.09f), (V(0.12f, 0.03f, -0.04f), 0.07f), (V(-0.12f, 0.03f, -0.02f), 0.07f) })
            Puff(b, Body, at, size);
        b.Torus(Body, V(0, 0.6f, 0), 0.075f, 0.024f, sash, sz: 0.9f, blend: 0.004f);
        // Sleeves grown into great wings of ice
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.7f, 0.0f));
            b.Limb(arm, V(0.07f * s, 0.7f, 0.0f), V(0.13f * s, 0.62f, 0.04f), 0.03f, 0.03f, white);
            var p = Frond(b, arm, V(0.12f * s, 0.7f, -0.02f), V(0.26f * s, 0.22f, -0.06f), 0.1f, ice, V(1f * s, 0, 0.4f), 0.14f, Shell);
            b.PaintEll(arm, V(0.2f * s, 0.42f, -0.04f), V(0.06f, 0.1f, 0.02f), white).Rotation = p.Rotation;
        });
        int head = b.Head(V(0, 0.76f, 0));
        var hc = V(0, 0.84f, 0.01f);
        var hr = V(0.08f, 0.08f, 0.075f);
        b.Ell(head, hc, hr, white);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, V(0.095f * s, 0.76f, -0.005f), V(0.045f, 0.12f, 0.055f), white, V(0, 0, 10f * s));
            b.PaintEll(head, V(0.1f * s, 0.66f, -0.005f), V(0.05f, 0.04f, 0.06f), ice);
            b.Spike(head, V(0.035f * s, 0.9f, -0.01f), V(0.08f * s, 1.04f, -0.04f), 0.03f, ice, mat: Shell, blend: 0.006f);
            b.Spike(head, V(0.05f * s, 0.9f, -0.02f), V(0.12f * s, 0.97f, -0.06f), 0.02f, ice, mat: Shell, blend: 0.006f);
            var at = On(hc, hr, 0.036f * s, 0.84f);
            b.PaintEll(head, at, V(0.032f, 0.03f, 0.03f), purple);
            b.Eye(head, at, Outward(hc, hr, at), 0.019f, sclera: true, white: Rgb(250, 220, 90), pupil: Rgb(60, 110, 220));
        });
        return Lift(b);
    }
}
