using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Plan 03 · D7: hand-built models for the second batch of Platinum's Sinnoh Pokédex, Gastly (69) to Hippowdon (123).
// The Gible and Riolu lines in that range were hand-built before. Helpers shared with the first batch (bat wings,
// fins, digits, petals, lifting a flier off its platform) are in PokemonModels.Sinnoh1.cs.
internal static partial class PokemonModels
{
    /// <summary>
    /// A smooth curve through <paramref name="points"/> (Catmull-Rom), as a polyline <paramref name="steps"/> points
    /// to a span, for tubes that bend without corners: whiskers, antennae, tails.
    /// </summary>
    private static Vector3[] Smooth(int steps, params Vector3[] points)
    {
        var curve = new System.Collections.Generic.List<Vector3>();
        for (int i = 0; i < points.Length - 1; i++)
        {
            var p0 = points[Math.Max(0, i - 1)];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = points[Math.Min(points.Length - 1, i + 2)];
            for (int k = 0; k < steps; k++)
            {
                float t = (float)k / steps, t2 = t * t, t3 = t2 * t;
                curve.Add(0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3));
            }
        }
        curve.Add(points[^1]);
        return curve.ToArray();
    }

    // ------------------------------------------------------------------ Gastly line

    private static PokeBuilder Gastly()
    {
        var b = new PokeBuilder("Gastly", 0.72f, BodyPlan.Floating, V(0, 0.5f, 0)) { Coat = Fur }.Hover();
        var black = Rgb(46, 40, 58);
        var gas = Rgb(140, 102, 168);

        // A dark ball in a cloud of the gas it is made of, puffing out round its rim and behind it
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f + 0.2f;
            float r = 0.25f + 0.03f * MathF.Sin(i * 2.7f);
            float k = 0.85f + 0.3f * (i * 7 % 5) / 5f;
            b.Ell(Body, V(MathF.Cos(a) * r, 0.5f + MathF.Sin(a) * r, -0.06f), V(0.09f, 0.09f, 0.07f) * k, gas, blend: 0.03f);
        }
        b.Ell(Body, V(0, 0.5f, -0.1f), V(0.27f, 0.27f, 0.12f), gas, blend: 0.03f);
        b.Ell(Body, V(0, 0.5f, 0.02f), V(0.22f, 0.22f, 0.21f), black);
        Gape(b, Body, V(0, 0.42f, 0.19f), V(0.09f, 0.035f, 0.035f), Rgb(200, 60, 80), 0.035f);
        PokeBuilder.Both(s => b.Eye(Body, V(0.085f * s, 0.54f, 0.2f), V(0.4f * s, 0.05f, 1f), 0.055f, sclera: true, glare: true));
        return Lift(b);
    }

    private static PokeBuilder Haunter()
    {
        var b = new PokeBuilder("Haunter", 0.86f, BodyPlan.Floating, V(0, 0.55f, 0)) { Coat = Fur }.Hover();
        var purple = Rgb(146, 108, 196);

        // A head ringed with spikes, trailing off into a wisp
        b.Ell(Body, V(0, 0.56f, 0), V(0.23f, 0.19f, 0.17f), purple);
        foreach (var (at, tip, r) in new[]
                 {
                     (V(0, 0.7f, -0.02f), V(0.0f, 0.98f, -0.08f), 0.09f), (V(-0.1f, 0.68f, -0.02f), V(-0.22f, 0.88f, -0.06f), 0.07f),
                     (V(0.1f, 0.68f, -0.02f), V(0.24f, 0.9f, -0.06f), 0.07f), (V(-0.2f, 0.58f, -0.02f), V(-0.4f, 0.64f, -0.06f), 0.07f),
                     (V(0.2f, 0.58f, -0.02f), V(0.42f, 0.66f, -0.06f), 0.07f), (V(-0.2f, 0.48f, -0.02f), V(-0.34f, 0.42f, -0.06f), 0.06f),
                     (V(0.2f, 0.48f, -0.02f), V(0.36f, 0.44f, -0.06f), 0.06f)
                 })
            b.Spike(Body, at, tip, r, purple, 0.55f);
        int tail = b.Tail(V(0, 0.42f, -0.04f));
        b.Tube(tail, new[] { V(0, 0.42f, -0.04f), V(0.02f, 0.32f, -0.06f), V(0.07f, 0.24f, -0.08f), V(0.13f, 0.2f, -0.1f) }, 0.08f, 0.02f, purple, blend: 0.02f);
        Gape(b, Body, V(0, 0.5f, 0.15f), V(0.13f, 0.055f, 0.04f), Rgb(226, 112, 140), 0.035f);
        PokeBuilder.Both(s => b.Eye(Body, V(0.085f * s, 0.605f, 0.16f), V(0.45f * s, 0.1f, 1f), 0.045f, sclera: true, glare: true));
        // Two hands that float free of it, three claws on each
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.36f * s, 0.46f, 0.1f));
            b.Ell(arm, V(0.36f * s, 0.46f, 0.1f), V(0.06f, 0.05f, 0.05f), purple);
            Digits(b, arm, V(0.38f * s, 0.44f, 0.14f), V(0.3f * s, -0.4f, 1f), V(0.6f, 0, 0), 0.08f, 0.02f, purple);
        });
        return Lift(b);
    }

    private static PokeBuilder Gengar()
    {
        var b = new PokeBuilder("Gengar", 0.86f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var purple = Rgb(118, 92, 176);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.2f, 0));
            b.Limb(leg, V(0.12f * s, 0.2f, 0), V(0.14f * s, 0.06f, 0.03f), 0.07f, 0.06f, purple);
            b.Ell(leg, V(0.14f * s, 0.04f, 0.07f), V(0.07f, 0.04f, 0.09f), purple);
            Digits(b, leg, V(0.14f * s, 0.035f, 0.14f), V(0, 0, 1f), V(0.6f, 0, 0), 0.03f, 0.022f, purple);
        });
        // A round body that is all head: a grin full of teeth, spikes down its back, a stub of a tail
        b.Ell(Body, V(0, 0.44f, 0), V(0.28f, 0.27f, 0.24f), purple);
        for (int i = -2; i <= 2; i++)
            b.Spike(Body, V(0.1f * i, 0.58f - 0.03f * Math.Abs(i), -0.15f), V(0.16f * i, 0.66f - 0.05f * Math.Abs(i), -0.32f), 0.065f, purple, 0.6f);
        int tail = b.Tail(V(0, 0.3f, -0.2f));
        b.Spike(tail, V(0, 0.3f, -0.19f), V(0, 0.24f, -0.33f), 0.06f, purple);
        b.Cut(Body, V(0, 0.355f, 0.235f), V(0.165f, 0.05f, 0.04f), blend: 0.01f);
        b.PaintEll(Body, V(0, 0.355f, 0.21f), V(0.18f, 0.06f, 0.06f), Rgb(120, 30, 56));
        b.PaintEll(Body, V(0, 0.385f, 0.21f), V(0.17f, 0.022f, 0.06f), White, soft: 0.008f);
        b.PaintEll(Body, V(0, 0.325f, 0.21f), V(0.16f, 0.02f, 0.06f), White, soft: 0.008f);
        for (int i = -3; i <= 3; i++)
            b.PaintEll(Body, V(0.045f * i, 0.355f, 0.215f), V(0.004f, 0.05f, 0.05f), Rgb(150, 140, 160), soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.24f * s, 0.42f, 0.04f));
            b.Limb(arm, V(0.24f * s, 0.42f, 0.04f), V(0.34f * s, 0.35f, 0.1f), 0.06f, 0.05f, purple);
            Digits(b, arm, V(0.35f * s, 0.34f, 0.11f), V(0.4f * s, -0.3f, 0.6f), V(0, 0.6f, 0), 0.05f, 0.022f, purple);
        });

        // Ears like horns, and red eyes narrowed in a grin
        int head = b.Head(V(0, 0.6f, 0));
        PokeBuilder.Both(s => b.Spike(head, V(0.15f * s, 0.63f, -0.02f), V(0.27f * s, 0.86f, -0.06f), 0.085f, purple, 0.55f));
        b.Ell(head, V(0, 0.66f, -0.02f), V(0.12f, 0.05f, 0.1f), purple);
        PokeBuilder.Both(s => b.Eye(Body, V(0.1f * s, 0.52f, 0.225f), V(0.45f * s, 0.1f, 1f), 0.042f, pupil: Rgb(50, 20, 34), white: Rgb(232, 62, 72), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Misdreavus line

    /// <summary>A lock of hair or a ragged hem: a flattened point, its tip coloured.</summary>
    private static void Lock(PokeBuilder b, int bone, Vector3 at, Vector3 tip, float r, Color color, Color tipColor)
    {
        b.Spike(bone, at, tip, r, color, 0.5f);
        b.PaintEll(bone, tip + (at - tip) * 0.14f, V(r * 0.7f, r * 0.7f, r * 0.7f), tipColor);
    }

    private static PokeBuilder Misdreavus()
    {
        var b = new PokeBuilder("Misdreavus", 0.64f, BodyPlan.Floating, V(0, 0.48f, 0)) { Coat = Fur }.Hover();
        var teal = Rgb(64, 150, 172);
        var pink = Rgb(228, 120, 150);
        var red = Rgb(210, 50, 70);

        // A head whose hair hangs in waves to a ragged hem, every lock tipped in pink
        b.Ell(Body, V(0, 0.5f, 0), V(0.17f, 0.17f, 0.16f), teal);
        b.Ell(Body, V(0, 0.36f, -0.01f), V(0.15f, 0.1f, 0.14f), teal);
        foreach (var (at, tip) in new[]
                 {
                     (V(-0.08f, 0.64f, -0.02f), V(-0.14f, 0.78f, -0.06f)), (V(0.06f, 0.65f, -0.03f), V(0.1f, 0.8f, -0.08f)),
                     (V(-0.17f, 0.52f, -0.02f), V(-0.3f, 0.6f, -0.04f)), (V(0.17f, 0.52f, -0.02f), V(0.3f, 0.58f, -0.04f)),
                     (V(-0.14f, 0.32f, 0.0f), V(-0.24f, 0.22f, 0.02f)), (V(0.14f, 0.32f, 0.0f), V(0.24f, 0.22f, 0.02f)),
                     (V(-0.06f, 0.28f, 0.04f), V(-0.08f, 0.16f, 0.06f)), (V(0.06f, 0.28f, 0.04f), V(0.08f, 0.16f, 0.06f)),
                     (V(0, 0.3f, -0.1f), V(0, 0.18f, -0.18f))
                 })
            Lock(b, Body, at, tip, 0.06f, teal, pink);
        // The red pearls round its neck
        for (int i = 0; i < 7; i++)
        {
            float a = (i - 3) * 0.32f;
            b.Ell(Body, V(MathF.Sin(a) * 0.15f, 0.4f - MathF.Abs(MathF.Sin(a)) * 0.02f, MathF.Cos(a) * 0.14f), V(0.028f, 0.028f, 0.028f), red, mat: Shell, blend: 0.006f);
        }
        b.Mark(Body, V(0.02f, 0.45f, 0.165f), V(0, -0.1f, 1f), 0.035f, 0.008f, Rgb(40, 30, 50), MarkShape.Bar, -10f);
        PokeBuilder.Both(s => b.Eye(Body, V(0.065f * s, 0.53f, 0.15f), V(0.4f * s, 0.05f, 1f), 0.04f, pupil: red, white: Rgb(250, 214, 60)));
        return Lift(b);
    }

    private static PokeBuilder Mismagius()
    {
        var b = new PokeBuilder("Mismagius", 0.72f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var purple = Rgb(118, 72, 156);
        var hat = Rgb(92, 54, 128);
        var pink = Rgb(236, 120, 170);
        var red = Rgb(220, 46, 66);

        // A robe that frays into points tipped in pink, three red jewels down its front
        b.Ell(Body, V(0, 0.42f, 0), V(0.11f, 0.17f, 0.1f), purple);
        foreach (var (at, tip) in new[]
                 {
                     (V(-0.07f, 0.3f, 0.02f), V(-0.16f, 0.12f, 0.03f)), (V(0.07f, 0.3f, 0.02f), V(0.15f, 0.1f, 0.03f)), (V(0, 0.28f, 0.03f), V(0.01f, 0.06f, 0.05f)),
                     (V(-0.1f, 0.42f, -0.02f), V(-0.24f, 0.32f, -0.02f)), (V(0.1f, 0.42f, -0.02f), V(0.24f, 0.32f, -0.02f)), (V(0, 0.3f, -0.07f), V(0, 0.12f, -0.12f))
                 })
            Lock(b, Body, at, tip, 0.06f, purple, pink);
        foreach (var (y, r) in new[] { (0.5f, 0.03f), (0.42f, 0.036f), (0.34f, 0.026f) })
            b.Ell(Body, V(0, y, 0.098f), V(r, r * 1.15f, r * 0.8f), red, mat: Shell, blend: 0.006f);

        int head = b.Head(V(0, 0.56f, 0));
        b.Ell(head, V(0, 0.63f, 0.01f), V(0.11f, 0.095f, 0.1f), purple);
        // A wide witch's hat, its crown bent back and its brim frayed into points
        b.Ell(head, V(0, 0.71f, -0.01f), V(0.25f, 0.035f, 0.22f), hat);
        b.Tube(head, new[] { V(0, 0.72f, -0.02f), V(0, 0.84f, -0.06f), V(0.02f, 0.94f, -0.14f), V(0.06f, 0.98f, -0.24f) }, 0.12f, 0.02f, hat, blend: 0.02f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.4f;
            var edge = V(MathF.Sin(a) * 0.23f, 0.7f, MathF.Cos(a) * 0.2f - 0.01f);
            Lock(b, head, edge, edge + V(MathF.Sin(a) * 0.07f, -0.03f, MathF.Cos(a) * 0.06f), 0.035f, hat, pink);
        }
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, 0.635f, 0.095f), V(0.4f * s, 0.05f, 1f), 0.032f, pupil: red, white: Rgb(250, 214, 60)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Murkrow line

    private static PokeBuilder Murkrow()
    {
        var b = new PokeBuilder("Murkrow", 0.56f, BodyPlan.Bird, V(0, 0.3f, 0)) { Coat = Fur };
        var navy = Rgb(44, 54, 112);
        var yellow = Rgb(250, 208, 64);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.18f, 0.0f));
            b.Limb(leg, V(0.05f * s, 0.18f, 0.0f), V(0.055f * s, 0.04f, 0.02f), 0.018f, 0.015f, yellow);
            Digits(b, leg, V(0.055f * s, 0.025f, 0.03f), V(0, -0.2f, 1f), V(0.6f, 0, 0), 0.04f, 0.011f, yellow);
        });
        b.Ell(Body, V(0, 0.31f, 0), V(0.12f, 0.13f, 0.15f), navy, V(-15f, 0, 0));
        // A tail like a broom of feathers
        int tail = b.Tail(V(0, 0.27f, -0.13f));
        for (int i = -2; i <= 2; i++)
            b.Spike(tail, V(0.02f * i, 0.27f, -0.12f), V(0.07f * i, 0.36f + 0.03f * (2 - Math.Abs(i)), -0.34f), 0.045f, navy, 0.5f);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.1f * s, 0.36f, 0.0f));
            b.Ell(w, V(0.12f * s, 0.31f, -0.04f), V(0.035f, 0.1f, 0.15f), navy, V(-20f, 8f * s, 0));
            for (int i = 0; i < 3; i++)
                b.Spike(w, V(0.13f * s, 0.27f - i * 0.03f, -0.12f), V(0.15f * s, 0.24f - i * 0.04f, -0.24f), 0.03f, navy, 0.4f);
        });

        int head = b.Head(V(0, 0.42f, 0.06f));
        b.Ell(head, V(0, 0.48f, 0.07f), V(0.1f, 0.095f, 0.095f), navy);
        // The brim of a witch's hat of feathers, its crown swept back, and a yellow beak
        b.Ell(head, V(0, 0.55f, 0.04f), V(0.16f, 0.025f, 0.14f), navy, V(-12f, 0, 0));
        b.Spike(head, V(0, 0.56f, 0.03f), V(0, 0.62f, -0.16f), 0.085f, navy, 0.6f);
        b.Spike(head, V(0, 0.47f, 0.15f), V(0, 0.43f, 0.25f), 0.042f, yellow, mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.055f * s, 0.49f, 0.145f), V(0.6f * s, 0.05f, 0.8f), 0.025f, sclera: true, pupil: Rgb(200, 36, 50)));
        return b;
    }

    private static PokeBuilder Honchkrow()
    {
        var b = new PokeBuilder("Honchkrow", 0.74f, BodyPlan.Bird, V(0, 0.38f, 0)) { Coat = Fur };
        var navy = Rgb(44, 54, 112);
        var white = Rgb(244, 244, 248);
        var red = Rgb(214, 40, 60);
        var yellow = Rgb(250, 208, 64);
        var gray = Rgb(80, 80, 90);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.22f, 0.0f));
            b.Limb(leg, V(0.07f * s, 0.22f, 0.0f), V(0.075f * s, 0.04f, 0.02f), 0.025f, 0.02f, gray);
            Digits(b, leg, V(0.075f * s, 0.025f, 0.03f), V(0, -0.2f, 1f), V(0.6f, 0, 0), 0.055f, 0.014f, gray);
        });
        b.Ell(Body, V(0, 0.38f, -0.02f), V(0.16f, 0.17f, 0.19f), navy, V(-12f, 0, 0));
        // The great white mane on its chest
        b.Ell(Body, V(0, 0.4f, 0.1f), V(0.14f, 0.15f, 0.1f), white);
        foreach (var at in new[] { V(-0.08f, 0.3f, 0.13f), V(0.08f, 0.3f, 0.13f), V(0, 0.27f, 0.14f), V(-0.1f, 0.42f, 0.12f), V(0.1f, 0.42f, 0.12f) })
            b.Ell(Body, at, V(0.055f, 0.055f, 0.05f), white, blend: 0.02f);
        // A fan of a tail tipped in red
        int tail = b.Tail(V(0, 0.33f, -0.18f));
        for (int i = -3; i <= 3; i++)
            Lock(b, tail, V(0.02f * i, 0.32f, -0.17f), V(0.08f * i, 0.42f + 0.02f * (3 - Math.Abs(i)), -0.42f), 0.045f, navy, red);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.13f * s, 0.46f, 0.0f));
            b.Ell(w, V(0.17f * s, 0.38f, -0.05f), V(0.045f, 0.13f, 0.19f), navy, V(-18f, 10f * s, 0));
            b.PaintEll(w, V(0.2f * s, 0.36f, -0.02f), V(0.03f, 0.08f, 0.12f), red, V(-18f, 10f * s, 0));
            for (int i = 0; i < 3; i++)
                b.Spike(w, V(0.18f * s, 0.32f - i * 0.04f, -0.16f), V(0.21f * s, 0.28f - i * 0.05f, -0.3f), 0.035f, navy, 0.4f);
        });

        int head = b.Head(V(0, 0.52f, 0.08f));
        b.Ell(head, V(0, 0.6f, 0.1f), V(0.11f, 0.1f, 0.1f), navy);
        // A gangster's hat of feathers, and a hooked beak
        b.Ell(head, V(0, 0.68f, 0.07f), V(0.2f, 0.028f, 0.17f), navy, V(-10f, 0, 0));
        b.Spike(head, V(0, 0.7f, 0.05f), V(0, 0.76f, -0.16f), 0.1f, navy, 0.6f);
        b.Tube(head, new[] { V(0, 0.59f, 0.18f), V(0, 0.57f, 0.25f), V(0, 0.53f, 0.28f) }, 0.04f, 0.012f, yellow, Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.06f * s, 0.62f, 0.18f), V(0.6f * s, 0.05f, 0.8f), 0.025f, sclera: true, pupil: red, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Glameow line

    /// <summary>A cat's whiskers: two thin lines out to each side of the muzzle.</summary>
    private static void Whiskers(PokeBuilder b, int bone, Vector3 muzzle, float reach, float droop, Color color)
    {
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 2; i++)
                b.Tube(bone, new[] { muzzle + V(0.03f * s, -0.012f * i, 0), muzzle + V(reach * 0.55f * s, -0.012f * i - droop * 0.3f, 0.005f), muzzle + V(reach * s, -0.024f * i - droop, -0.005f) },
                    0.0055f, 0.0045f, color);
        });
    }

    private static PokeBuilder Glameow()
    {
        var b = new PokeBuilder("Glameow", 0.58f, BodyPlan.Quadruped, V(0, 0.26f, -0.02f)) { Coat = Fur };
        var gray = Rgb(140, 160, 192);
        var light = Rgb(214, 224, 236);
        var pink = Rgb(236, 150, 180);

        foreach (var (z, front) in new[] { (0.09f, true), (-0.13f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.22f, z), front);
                b.Limb(leg, V(0.05f * s, 0.22f, z), V(0.05f * s, 0.03f, z + 0.01f), 0.03f, 0.022f, gray);
                b.Ell(leg, V(0.05f * s, 0.018f, z + 0.025f), V(0.024f, 0.018f, 0.035f), gray);
            });
        b.Ell(Body, V(0, 0.26f, -0.02f), V(0.075f, 0.075f, 0.15f), gray);
        b.PaintEll(Body, V(0, 0.22f, 0.04f), V(0.06f, 0.05f, 0.1f), light);
        // A long tail that rises and coils like a spring, a white puff at its tip
        int tail = b.Tail(V(0, 0.28f, -0.16f));
        var coil = new[] { V(0, 0.28f, -0.16f), V(0, 0.38f, -0.24f), V(0, 0.5f, -0.28f), V(0, 0.6f, -0.22f), V(0, 0.62f, -0.14f), V(0, 0.56f, -0.1f), V(0, 0.5f, -0.14f), V(0, 0.52f, -0.2f) };
        b.Tube(tail, coil, 0.022f, 0.017f, gray);
        b.Ell(tail, V(0, 0.56f, -0.2f), V(0.04f, 0.04f, 0.035f), light);

        int head = b.Head(V(0, 0.32f, 0.11f));
        b.Ell(head, V(0, 0.38f, 0.14f), V(0.085f, 0.075f, 0.075f), gray);
        b.Ell(head, V(0, 0.355f, 0.2f), V(0.04f, 0.028f, 0.03f), light);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.43f, 0.12f));
            b.Spike(ear, V(0.05f * s, 0.42f, 0.12f), V(0.09f * s, 0.53f, 0.1f), 0.04f, gray, 0.45f);
            b.Spike(ear, V(0.052f * s, 0.43f, 0.13f), V(0.085f * s, 0.505f, 0.115f), 0.024f, pink, 0.35f);
        });
        Whiskers(b, head, V(0, 0.355f, 0.215f), 0.12f, 0.02f, Rgb(70, 72, 90));
        PokeBuilder.Both(s => b.Eye(head, V(0.035f * s, 0.39f, 0.2f), V(0.5f * s, 0.05f, 1f), 0.022f, s < 0 ? Rgb(90, 190, 220) : Rgb(236, 120, 190)));
        return b;
    }

    private static PokeBuilder Purugly()
    {
        var b = new PokeBuilder("Purugly", 0.78f, BodyPlan.Quadruped, V(0, 0.32f, -0.02f)) { Coat = Fur };
        var gray = Rgb(148, 160, 196);
        var white = Rgb(236, 238, 244);
        var lilac = Rgb(196, 160, 226);

        foreach (var (z, front) in new[] { (0.1f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.13f * s, 0.14f, z), front);
                b.Limb(leg, V(0.13f * s, 0.14f, z), V(0.14f * s, 0.04f, z + 0.02f), 0.06f, 0.05f, gray);
                b.Ell(leg, V(0.14f * s, 0.03f, z + 0.05f), V(0.055f, 0.03f, 0.065f), white);
            });
        // A barrel of a body, its tail tied round its waist like a belt
        b.Ell(Body, V(0, 0.32f, -0.02f), V(0.22f, 0.2f, 0.22f), gray);
        b.PaintEll(Body, V(0, 0.28f, 0.12f), V(0.15f, 0.15f, 0.12f), white);
        b.Torus(Body, V(0, 0.3f, -0.02f), 0.205f, 0.03f, gray, blend: 0.012f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.2f * s, 0.42f, -0.04f), V(0.04f, 0.04f, 0.12f), PixelCanvas.Mix(gray, white, 0.5f)));

        int head = b.Head(V(0, 0.46f, 0.12f));
        b.Ell(head, V(0, 0.52f, 0.14f), V(0.13f, 0.11f, 0.11f), gray);
        b.Ell(head, V(0, 0.48f, 0.22f), V(0.07f, 0.045f, 0.05f), white);
        // Big ears tufted in lilac, and long whiskers that curl at the ends
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.6f, 0.1f));
            b.Spike(ear, V(0.08f * s, 0.59f, 0.1f), V(0.2f * s, 0.78f, 0.06f), 0.07f, gray, 0.4f);
            b.Spike(ear, V(0.09f * s, 0.62f, 0.11f), V(0.19f * s, 0.77f, 0.075f), 0.045f, lilac, 0.3f);
            var curl = Spiral(V(0.27f * s, 0.47f, 0.25f), V(-s, 0, 0), V(0, 1, 0), 0.03f, 0.012f, 0f, MathF.PI * 1.6f, 8);
            b.Tube(head, new[] { V(0.05f * s, 0.48f, 0.25f), V(0.14f * s, 0.49f, 0.26f) }.Concat(curl).ToArray(), 0.0065f, 0.0055f, Rgb(60, 62, 80));
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.54f, 0.235f), V(0.45f * s, 0.05f, 1f), 0.022f, Rgb(250, 214, 60), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Goldeen line

    private static PokeBuilder Goldeen()
    {
        var b = new PokeBuilder("Goldeen", 0.6f, BodyPlan.Fish, V(0, 0.44f, 0)) { Coat = Scales }.Hover();
        var white = Rgb(244, 242, 240);
        var orange = Rgb(240, 110, 60);

        b.Ell(Body, V(0, 0.44f, -0.02f), V(0.1f, 0.15f, 0.22f), white);
        b.PaintEll(Body, V(0, 0.54f, -0.04f), V(0.11f, 0.08f, 0.16f), orange);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.08f * s, 0.42f, -0.1f), V(0.05f, 0.05f, 0.06f), orange));
        // Long flowing fins, white streaked with orange
        int dorsal = b.Part("dorsal", Body, V(0, 0.58f, -0.04f), PokeRole.Fin);
        RimmedFin(b, dorsal, V(0, 0.66f, -0.1f), V(0.015f, 0.1f, 0.12f), V(-35f, 0, 0), white, orange, 0.02f);
        int tail = b.Tail(V(0, 0.44f, -0.2f));
        b.Limb(tail, V(0, 0.44f, -0.2f), V(0, 0.44f, -0.28f), 0.06f, 0.04f, white);
        int fin = b.Part("tailFin", tail, V(0, 0.44f, -0.28f), PokeRole.Fin);
        RimmedFin(b, fin, V(0, 0.54f, -0.4f), V(0.016f, 0.15f, 0.1f), V(-40f, 0, 0), white, orange, 0.025f);
        RimmedFin(b, fin, V(0, 0.34f, -0.4f), V(0.016f, 0.15f, 0.1f), V(40f, 0, 0), white, orange, 0.025f);
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.08f * s, 0.38f, 0.06f), PokeRole.Fin, s, s);
            RimmedFin(b, side, V(0.115f * s, 0.34f, 0.0f), V(0.014f, 0.07f, 0.1f), V(20f, 20f * s, 0), white, orange, 0.018f);
        });

        int head = b.Head(V(0, 0.44f, 0.12f));
        b.Ell(head, V(0, 0.46f, 0.15f), V(0.095f, 0.13f, 0.1f), white);
        b.PaintEll(head, V(0, 0.55f, 0.12f), V(0.1f, 0.06f, 0.08f), orange);
        // The horn it drills with, and pouting pink lips
        b.Spike(head, V(0, 0.56f, 0.2f), V(0, 0.68f, 0.3f), 0.028f, Rgb(250, 236, 200), mat: Shell);
        b.Torus(head, V(0, 0.42f, 0.25f), 0.022f, 0.016f, Rgb(244, 140, 160), V(90f, 0, 0), mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s => b.Eye(head, V(0.065f * s, 0.49f, 0.21f), V(0.8f * s, 0.05f, 0.6f), 0.03f, sclera: true, pupil: Rgb(70, 150, 210)));
        return Lift(b);
    }

    private static PokeBuilder Seaking()
    {
        var b = new PokeBuilder("Seaking", 0.84f, BodyPlan.Fish, V(0, 0.45f, 0)) { Coat = Scales }.Hover();
        var orange = Rgb(238, 100, 52);
        var white = Rgb(244, 242, 240);
        var black = Rgb(50, 44, 50);

        b.Ell(Body, V(0, 0.45f, -0.02f), V(0.13f, 0.2f, 0.26f), orange);
        b.PaintEll(Body, V(0, 0.32f, 0.0f), V(0.13f, 0.09f, 0.25f), white);
        // Dark bars across its back
        foreach (float z in new[] { 0.06f, -0.06f, -0.17f })
            b.PaintEll(Body, V(0, 0.58f, z), V(0.14f, 0.07f, 0.022f), black, V(25f, 0, 0));
        // Great white fins with dark spots
        int dorsal = b.Part("dorsal", Body, V(0, 0.62f, -0.04f), PokeRole.Fin);
        b.Ell(dorsal, V(0, 0.72f, -0.1f), V(0.016f, 0.12f, 0.13f), white, V(-30f, 0, 0), blend: 0.01f);
        b.PaintEll(dorsal, V(0, 0.76f, -0.12f), V(0.03f, 0.02f, 0.02f), black);
        int tail = b.Tail(V(0, 0.45f, -0.24f));
        b.Limb(tail, V(0, 0.45f, -0.24f), V(0, 0.45f, -0.33f), 0.08f, 0.05f, orange);
        int fin = b.Part("tailFin", tail, V(0, 0.45f, -0.33f), PokeRole.Fin);
        foreach (float k in new[] { 1f, -1f })
        {
            b.Ell(fin, V(0, 0.45f + 0.12f * k, -0.47f), V(0.016f, 0.17f, 0.11f), white, V(-40f * k, 0, 0), blend: 0.01f);
            b.PaintEll(fin, V(0, 0.45f + 0.18f * k, -0.52f), V(0.03f, 0.022f, 0.022f), black);
        }
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.1f * s, 0.36f, 0.06f), PokeRole.Fin, s, s);
            b.Ell(side, V(0.145f * s, 0.32f, -0.02f), V(0.016f, 0.09f, 0.13f), white, V(20f, 20f * s, 0), blend: 0.01f);
            b.PaintEll(side, V(0.155f * s, 0.29f, -0.06f), V(0.025f, 0.02f, 0.02f), black);
        });

        int head = b.Head(V(0, 0.45f, 0.15f));
        b.Ell(head, V(0, 0.47f, 0.18f), V(0.12f, 0.16f, 0.12f), orange);
        b.PaintEll(head, V(0, 0.36f, 0.22f), V(0.1f, 0.07f, 0.09f), white);
        b.Spike(head, V(0, 0.6f, 0.22f), V(0, 0.78f, 0.36f), 0.04f, Rgb(250, 236, 200), mat: Shell);
        b.Torus(head, V(0, 0.42f, 0.3f), 0.026f, 0.02f, Rgb(244, 140, 160), V(90f, 0, 0), mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s => b.Eye(head, V(0.08f * s, 0.52f, 0.25f), V(0.8f * s, 0.05f, 0.6f), 0.034f, sclera: true));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Barboach line

    private static PokeBuilder Barboach()
    {
        var b = new PokeBuilder("Barboach", 0.52f, BodyPlan.Fish, V(0, 0.3f, -0.04f)) { Coat = Scales }.Hover();
        var silver = Rgb(196, 202, 212);
        var blue = Rgb(110, 196, 236);
        var black = Rgb(40, 40, 48);

        // A long slippery body with a black zigzag along each side
        b.Limb(Body, V(0, 0.3f, 0.12f), V(0, 0.3f, -0.18f), 0.085f, 0.07f, silver);
        PokeBuilder.Both(s =>
        {
            b.Mark(Body, V(0.085f * s, 0.305f, 0.045f), V(s, 0.1f, 0), 0.075f, 0.014f, black, MarkShape.Zigzag);
            b.Mark(Body, V(0.08f * s, 0.305f, -0.1f), V(s, 0.1f, 0), 0.075f, 0.014f, black, MarkShape.Zigzag);
        });
        int dorsal = b.Part("dorsal", Body, V(0, 0.38f, 0.0f), PokeRole.Fin);
        b.Ell(dorsal, V(0, 0.42f, -0.03f), V(0.018f, 0.055f, 0.055f), blue, blend: 0.01f);
        int tail = b.Tail(V(0, 0.3f, -0.18f));
        b.Limb(tail, V(0, 0.3f, -0.18f), V(0, 0.3f, -0.25f), 0.065f, 0.045f, silver);
        int fin = b.Part("tailFin", tail, V(0, 0.3f, -0.25f), PokeRole.Fin);
        b.Ell(fin, V(0, 0.3f, -0.29f), V(0.02f, 0.075f, 0.05f), blue, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.05f * s, 0.23f, 0.04f), PokeRole.Fin, s, s);
            b.Ell(side, V(0.055f * s, 0.2f, 0.06f), V(0.024f, 0.03f, 0.03f), blue, blend: 0.01f);
            b.Ell(side, V(0.05f * s, 0.21f, -0.1f), V(0.022f, 0.028f, 0.028f), blue, blend: 0.01f);
        });

        int head = b.Head(V(0, 0.3f, 0.1f));
        b.Ell(head, V(0, 0.31f, 0.16f), V(0.08f, 0.075f, 0.09f), silver);
        // Two whiskers that feel the mud, each ending in a blue drop
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(4, V(0.04f * s, 0.29f, 0.23f), V(0.08f * s, 0.32f, 0.28f), V(0.13f * s, 0.36f, 0.3f)), 0.012f, 0.01f, blue);
            b.Ell(head, V(0.14f * s, 0.365f, 0.3f), V(0.024f, 0.024f, 0.024f), blue);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.055f * s, 0.34f, 0.2f), V(0.6f * s, 0.1f, 0.8f), 0.017f));
        return Lift(b);
    }

    private static PokeBuilder Whiscash()
    {
        var b = new PokeBuilder("Whiscash", 0.74f, BodyPlan.Fish, V(0, 0.34f, -0.04f)) { Coat = Scales }.Hover();
        var blue = Rgb(52, 74, 168);
        var light = Rgb(96, 186, 236);
        var cream = Rgb(246, 226, 150);
        var yellow = Rgb(250, 222, 80);

        // A heavy blue body on a pale belly
        b.Ell(Body, V(0, 0.34f, -0.04f), V(0.2f, 0.17f, 0.27f), blue);
        b.PaintEll(Body, V(0, 0.22f, -0.02f), V(0.18f, 0.07f, 0.25f), cream);
        int dorsal = b.Part("dorsal", Body, V(0, 0.48f, -0.1f), PokeRole.Fin);
        b.Ell(dorsal, V(0, 0.56f, -0.14f), V(0.02f, 0.08f, 0.07f), light, V(-20f, 0, 0), blend: 0.01f);
        foreach (var at in new[] { V(0, 0.58f, -0.12f), V(0, 0.54f, -0.17f) })
            b.PaintEll(dorsal, at, V(0.03f, 0.016f, 0.012f), Rgb(30, 40, 90));
        int tail = b.Tail(V(0, 0.34f, -0.28f));
        b.Limb(tail, V(0, 0.34f, -0.28f), V(0, 0.36f, -0.38f), 0.09f, 0.05f, blue);
        int fin = b.Part("tailFin", tail, V(0, 0.36f, -0.38f), PokeRole.Fin);
        // Its tail fin is a heart
        PokeBuilder.Both(s => b.Ell(fin, V(0.06f * s, 0.44f, -0.44f), V(0.07f, 0.07f, 0.02f), light, V(0, 0, -30f * s), blend: 0.01f));
        b.Spike(fin, V(0, 0.42f, -0.43f), V(0, 0.32f, -0.44f), 0.06f, light, 0.3f);
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.15f * s, 0.24f, 0.06f), PokeRole.Fin, s, s);
            b.Ell(side, V(0.18f * s, 0.22f, 0.04f), V(0.05f, 0.025f, 0.06f), light, V(0, 0, 20f * s), blend: 0.01f);
        });

        int head = b.Head(V(0, 0.34f, 0.14f));
        b.Ell(head, V(0, 0.35f, 0.18f), V(0.19f, 0.15f, 0.14f), blue);
        // A wide pale lip round its mouth, the yellow W on its brow, and whiskers sweeping back
        b.Ell(head, V(0, 0.28f, 0.27f), V(0.17f, 0.05f, 0.07f), light);
        b.Mark(head, V(0, 0.28f, 0.34f), V(0, 0, 1f), 0.09f, 0.008f, Rgb(30, 40, 90), MarkShape.Bar);
        b.Mark(head, V(0, 0.45f, 0.25f), V(0, 0.6f, 1f), 0.055f, 0.028f, yellow, MarkShape.Zigzag);
        PokeBuilder.Both(s => b.Tube(head, Smooth(4, V(0.1f * s, 0.32f, 0.3f), V(0.18f * s, 0.4f, 0.32f), V(0.24f * s, 0.54f, 0.26f), V(0.25f * s, 0.63f, 0.14f), V(0.21f * s, 0.63f, 0.04f)),
            0.014f, 0.009f, yellow));
        PokeBuilder.Both(s => b.Eye(head, V(0.1f * s, 0.38f, 0.29f), V(0.6f * s, 0.05f, 0.8f), 0.024f, sclera: true));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Chingling line

    /// <summary>A rope striped in two colours: a tube with bands of <paramref name="band"/> painted along it.</summary>
    private static void Striped(PokeBuilder b, int bone, Vector3[] path, float ra, float rb, Color color, Color band, int stripes)
    {
        b.Tube(bone, path, ra, rb, color);
        for (int i = 0; i < stripes; i++)
        {
            float t = (i + 0.5f) / stripes;
            int k = Math.Min(path.Length - 2, (int)(t * (path.Length - 1)));
            float f = t * (path.Length - 1) - k;
            var at = Vector3.Lerp(path[k], path[k + 1], f);
            float r = ra + (rb - ra) * t;
            b.PaintEll(bone, at, V(r * 1.3f, r * 0.45f, r * 1.3f), band, Euler(path[k + 1] - path[k]));
        }
    }

    private static PokeBuilder Chingling()
    {
        var b = new PokeBuilder("Chingling", 0.46f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var yellow = Rgb(250, 214, 52);
        var red = Rgb(232, 64, 80);

        // A little bell: the dark mouth of the bell in front, the striped cord of its tassels on top
        b.Ell(Body, V(0, 0.3f, 0), V(0.16f, 0.15f, 0.15f), yellow);
        b.Mark(Body, V(0, 0.235f, 0.135f), V(0, -0.35f, 1f), 0.05f, 0.035f, Rgb(70, 62, 66));
        PokeBuilder.Both(s => Striped(b, Body, Smooth(3, V(0.02f * s, 0.43f, -0.03f), V(0.06f * s, 0.52f, -0.08f), V(0.11f * s, 0.55f, -0.15f), V(0.13f * s, 0.51f, -0.21f)),
            0.032f, 0.028f, red, White, 3));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.29f, 0.04f));
            b.Ell(arm, V(0.16f * s, 0.27f, 0.06f), V(0.045f, 0.035f, 0.045f), yellow);
            int leg = b.Leg(s, V(0.06f * s, 0.18f, 0.06f));
            b.Ell(leg, V(0.07f * s, 0.16f, 0.09f), V(0.04f, 0.03f, 0.05f), yellow);
        });
        PokeBuilder.Both(s => b.Eye(Body, V(0.06f * s, 0.33f, 0.14f), V(0.35f * s, 0.05f, 1f), 0.024f, closed: true));
        return Lift(b);
    }

    private static PokeBuilder Chimecho()
    {
        var b = new PokeBuilder("Chimecho", 0.6f, BodyPlan.Floating, V(0, 0.6f, 0)) { Coat = Shell }.Hover();
        var blue = Rgb(196, 228, 246);
        var red = Rgb(226, 72, 86);

        // A wind chime: a round head hung from a suction cup, a long tail ending in a strip of red paper
        b.Ell(Body, V(0, 0.62f, 0), V(0.12f, 0.1f, 0.11f), blue);
        b.Limb(Body, V(0, 0.7f, 0), V(0, 0.77f, 0), 0.018f, 0.018f, blue);
        b.Ell(Body, V(0, 0.79f, 0), V(0.05f, 0.028f, 0.05f), Rgb(250, 224, 110));
        PokeBuilder.Both(s => b.Mark(Body, V(0.09f * s, 0.63f, 0.07f), V(0.8f * s, 0, 0.6f), 0.03f, 0.022f, red));
        int tail = b.Tail(V(0, 0.54f, 0));
        b.Tube(tail, new[] { V(0, 0.54f, 0), V(0, 0.4f, 0.01f), V(0, 0.26f, 0.02f) }, 0.035f, 0.05f, blue);
        b.Ell(tail, V(0, 0.18f, 0.02f), V(0.065f, 0.09f, 0.022f), red, blend: 0.02f);
        foreach (float x in new[] { -0.045f, -0.015f, 0.015f, 0.045f })
            b.Spike(tail, V(x, 0.12f, 0.02f), V(x * 1.2f, 0.07f, 0.02f), 0.018f, red, 0.5f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.57f, 0.04f));
            b.Ell(arm, V(0.13f * s, 0.55f, 0.05f), V(0.04f, 0.022f, 0.025f), blue);
        });
        b.Mark(Body, V(0, 0.585f, 0.108f), V(0, -0.2f, 1f), 0.018f, 0.018f, Rgb(80, 50, 70), MarkShape.Ring);
        PokeBuilder.Both(s => b.Eye(Body, V(0.045f * s, 0.635f, 0.1f), V(0.35f * s, 0.05f, 1f), 0.02f, closed: true));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Stunky line

    /// <summary>
    /// A bushy tail along <paramref name="path"/>: flattened from side to side, its fur standing out in points along
    /// its outer edge, a stripe of <paramref name="stripe"/> down the middle of that edge.
    /// </summary>
    private static void BushyTail(PokeBuilder b, int bone, Vector3[] path, float ra, float rb, Color color, Color stripe, Vector3 outward)
    {
        var curve = Smooth(3, path);
        for (int i = 1; i < curve.Length; i++)
        {
            float t = (float)i / (curve.Length - 1);
            float r = ra + (rb - ra) * t;
            var along = Vector3.Normalize(curve[i] - curve[i - 1]);
            var outside = outward - along * Vector3.Dot(outward, along);
            outside = outside.LengthSquared() < 1e-4f ? Vector3.Cross(along, Vector3.UnitX) : Vector3.Normalize(outside);
            // The body of the tail, wider across its curl than from side to side
            b.Ell(bone, (curve[i - 1] + curve[i]) / 2f, V(r * 0.75f, Vector3.Distance(curve[i - 1], curve[i]) * 0.5f + r * 0.8f, r), color, Euler(along), blend: 0.03f);
            if (i % 2 == 0)
                b.Spike(bone, curve[i] + outside * r * 0.5f, curve[i] + outside * r * 1.55f - along * r * 0.7f, r * 0.5f, color, 0.45f);
            b.PaintEll(bone, curve[i] + outside * r * 0.8f, V(r * 0.6f, r * 0.75f, r * 0.6f), stripe, Euler(along));
        }
    }

    private static PokeBuilder Skunk(bool grown)
    {
        var b = new PokeBuilder(grown ? "Skuntank" : "Stunky", grown ? 0.78f : 0.56f, BodyPlan.Quadruped, V(0, 0.2f, -0.02f)) { Coat = Fur };
        var purple = grown ? Rgb(110, 70, 140) : Rgb(100, 60, 124);
        var cream = Rgb(242, 224, 170);
        float k = grown ? 1.35f : 1f;

        foreach (var (z, front) in new[] { (0.09f, true), (-0.12f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s * k, 0.13f * k, z * k), front);
                b.Limb(leg, V(0.07f * s * k, 0.13f * k, z * k), V(0.075f * s * k, 0.03f, (z + 0.01f) * k), 0.035f * k, 0.03f * k, purple);
                b.Ell(leg, V(0.075f * s * k, 0.022f, (z + 0.035f) * k), V(0.03f * k, 0.022f, 0.04f * k), purple);
                b.Spike(leg, V(0.075f * s * k, 0.02f, (z + 0.06f) * k), V(0.075f * s * k, 0.005f, (z + 0.09f) * k), 0.012f, Claw, mat: Shell);
            });
        b.Ell(Body, V(0, 0.2f * k, -0.02f * k), V(0.1f * k, 0.09f * k, 0.15f * k), purple);
        b.PaintEll(Body, V(0, 0.28f * k, -0.04f * k), V(0.03f * k, 0.03f * k, 0.15f * k), cream);
        if (grown) b.PaintEll(Body, V(0, 0.14f * k, 0.04f * k), V(0.09f * k, 0.04f * k, 0.12f * k), cream);
        // A great bushy tail raised over its back, its fur in points, a cream stripe along it
        int tail = b.Tail(V(0, 0.24f * k, -0.15f * k));
        var path = grown
            ? new[] { V(0, 0.24f, -0.15f), V(0, 0.42f, -0.22f), V(0, 0.58f, -0.12f), V(0, 0.6f, 0.02f), V(0, 0.52f, 0.12f) }
            : new[] { V(0, 0.24f, -0.15f), V(0, 0.42f, -0.22f), V(0, 0.56f, -0.14f), V(0, 0.6f, -0.02f) };
        BushyTail(b, tail, path.Select(p => p * k).ToArray(), 0.07f * k, 0.12f * k, purple, cream, V(0, 1f, -0.5f));

        int head = b.Head(V(0, 0.24f * k, 0.12f * k));
        b.Ell(head, V(0, 0.27f * k, 0.15f * k), V(0.085f * k, 0.075f * k, 0.075f * k), purple);
        b.PaintEll(head, V(0, 0.3f * k, 0.19f * k), V(0.045f * k, 0.06f * k, 0.05f * k), cream);
        b.Ell(head, V(0, 0.25f * k, 0.21f * k), V(0.035f * k, 0.03f * k, 0.035f * k), cream);
        b.Ell(head, V(0, 0.26f * k, 0.245f * k), V(0.012f * k, 0.01f * k, 0.01f * k), Rgb(60, 40, 60), blend: 0.005f);
        if (grown) PokeBuilder.Both(s => b.Spike(head, V(0.07f * s * k, 0.25f * k, 0.15f * k), V(0.16f * s * k, 0.24f * k, 0.1f * k), 0.035f * k, cream, 0.4f));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s * k, 0.32f * k, 0.12f * k));
            b.Spike(ear, V(0.05f * s * k, 0.32f * k, 0.12f * k), V(0.08f * s * k, 0.38f * k, 0.1f * k), 0.025f * k, purple, 0.5f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.04f * s * k, 0.29f * k, 0.205f * k), V(0.5f * s, 0.05f, 1f), 0.02f * k, Rgb(244, 156, 40), glare: grown));
        return b;
    }

    // ------------------------------------------------------------------ Meditite line

    private static PokeBuilder Meditite()
    {
        var b = new PokeBuilder("Meditite", 0.6f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Fur }.Hover();
        var white = Rgb(226, 228, 236);
        var blue = Rgb(64, 154, 232);

        // Sitting cross-legged in the air, hands resting out to the sides
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.3f, 0.02f));
            b.Limb(leg, V(0.06f * s, 0.3f, 0.02f), V(0.16f * s, 0.25f, 0.12f), 0.05f, 0.042f, white);
            b.Limb(leg, V(0.16f * s, 0.25f, 0.12f), V(-0.03f * s, 0.22f, 0.16f), 0.042f, 0.036f, white);
            b.Ell(leg, V(-0.06f * s, 0.22f, 0.17f), V(0.04f, 0.03f, 0.05f), white);
        });
        b.Ell(Body, V(0, 0.38f, 0), V(0.1f, 0.1f, 0.085f), blue);
        b.Ell(Body, V(0, 0.3f, 0.0f), V(0.11f, 0.05f, 0.09f), white);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.42f, 0.0f));
            b.Limb(arm, V(0.08f * s, 0.42f, 0.0f), V(0.2f * s, 0.36f, 0.05f), 0.03f, 0.028f, blue);
            b.Limb(arm, V(0.2f * s, 0.36f, 0.05f), V(0.26f * s, 0.3f, 0.1f), 0.028f, 0.026f, blue);
            b.Ell(arm, V(0.28f * s, 0.29f, 0.11f), V(0.035f, 0.03f, 0.035f), white);
        });

        int head = b.Head(V(0, 0.46f, 0));
        b.Ell(head, V(0, 0.6f, 0.0f), V(0.15f, 0.14f, 0.14f), white);
        // A point on top of its head, and round puffs either side like headphones
        b.Spike(head, V(0, 0.7f, -0.01f), V(0, 0.86f, -0.03f), 0.08f, white);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, V(0.16f * s, 0.64f, -0.02f), V(0.06f, 0.06f, 0.055f), white);
            b.Mark(head, V(0.2f * s, 0.64f, -0.02f), V(s, 0, 0.1f), 0.03f, 0.03f, Rgb(170, 172, 186), MarkShape.Ring);
            b.Mark(head, V(0.085f * s, 0.55f, 0.125f), V(0.4f * s, 0, 1f), 0.025f, 0.016f, Rgb(244, 140, 160));
        });
        b.Mark(head, V(0, 0.52f, 0.14f), V(0, -0.1f, 1f), 0.02f, 0.016f, Rgb(160, 50, 60));
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.6f, 0.135f), V(0.35f * s, 0.05f, 1f), 0.035f));
        return Lift(b);
    }

    private static PokeBuilder Medicham()
    {
        var b = new PokeBuilder("Medicham", 0.86f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Fur };
        var pink = Rgb(236, 62, 120);
        var white = Rgb(228, 228, 236);
        var cream = Rgb(250, 226, 150);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.26f, 0));
            b.Limb(leg, V(0.07f * s, 0.26f, 0), V(0.08f * s, 0.05f, 0.02f), 0.025f, 0.022f, white);
            b.Ell(leg, V(0.08f * s, 0.025f, 0.04f), V(0.03f, 0.025f, 0.05f), white);
        });
        // Billowing pink trousers dotted in cream, a slender white body above
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, V(0.08f * s, 0.32f, 0), V(0.11f, 0.12f, 0.1f), pink);
            foreach (var (dx, dy, dz) in new[] { (0.12f, 0.36f, 0.06f), (0.06f, 0.26f, 0.09f), (0.16f, 0.28f, -0.02f) })
                b.PaintEll(Body, V(dx * s, dy, dz), V(0.025f, 0.025f, 0.025f), cream);
        });
        b.Ell(Body, V(0, 0.5f, 0), V(0.055f, 0.11f, 0.05f), white);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.58f, 0));
            var hand = s < 0 ? V(-0.2f, 0.74f, 0.06f) : V(0.24f, 0.5f, 0.08f);
            b.Tube(arm, new[] { V(0.05f * s, 0.58f, 0), (V(0.05f * s, 0.58f, 0) + hand) / 2f + V(0.04f * s, 0.02f, 0), hand }, 0.022f, 0.018f, white);
            b.Ell(arm, hand, V(0.03f, 0.03f, 0.025f), white);
        });

        int head = b.Head(V(0, 0.64f, 0));
        b.Ell(head, V(0, 0.71f, 0.02f), V(0.075f, 0.08f, 0.075f), white);
        b.Ell(head, V(0, 0.62f, 0.005f), V(0.03f, 0.035f, 0.03f), white);
        // A pink headdress rising into a bulb
        b.Ell(head, V(0, 0.77f, -0.01f), V(0.085f, 0.065f, 0.085f), pink);
        b.Tube(head, new[] { V(0, 0.8f, -0.02f), V(0.01f, 0.9f, -0.02f), V(0.03f, 0.98f, -0.02f) }, 0.03f, 0.022f, pink);
        b.Ell(head, V(0.04f, 1.02f, -0.02f), V(0.045f, 0.055f, 0.035f), pink);
        b.PaintEll(head, V(0, 0.66f, 0.08f), V(0.025f, 0.01f, 0.02f), Rgb(200, 60, 80));
        PokeBuilder.Both(s => b.Eye(head, V(0.032f * s, 0.72f, 0.085f), V(0.4f * s, 0.05f, 1f), 0.02f, Rgb(214, 56, 80)));
        return b;
    }

    // ------------------------------------------------------------------ Bronzor line

    private static PokeBuilder Bronzor()
    {
        var b = new PokeBuilder("Bronzor", 0.56f, BodyPlan.Floating, V(0, 0.45f, 0)) { Coat = Metal }.Hover();
        var face = Rgb(90, 186, 220);
        var rim = Rgb(56, 140, 186);
        var knob = Rgb(38, 104, 148);

        // An ancient mirror of bronze: a disc in a heavy rim set with six knobs, a face on its front
        b.Ell(Body, V(0, 0.45f, 0), V(0.22f, 0.22f, 0.055f), face);
        b.Torus(Body, V(0, 0.45f, 0), 0.215f, 0.045f, rim, V(90f, 0, 0), mat: Metal, blend: 0.01f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + MathF.PI / 6f;
            b.Ell(Body, V(MathF.Cos(a) * 0.255f, 0.45f + MathF.Sin(a) * 0.255f, 0), V(0.05f, 0.05f, 0.05f), knob, mat: Metal, blend: 0.01f);
        }
        b.Ell(Body, V(0, 0.42f, 0.05f), V(0.03f, 0.03f, 0.02f), rim, mat: Metal, blend: 0.008f);
        b.Mark(Body, V(0, 0.45f, 0.056f), V(0, 0, 1f), 0.16f, 0.16f, rim, MarkShape.Ring);
        PokeBuilder.Both(s => b.Eye(Body, V(0.085f * s, 0.47f, 0.05f), V(0.15f * s, 0, 1f), 0.045f, pupil: Rgb(30, 30, 40), white: Rgb(250, 220, 60)));
        return Lift(b);
    }

    private static PokeBuilder Bronzong()
    {
        var b = new PokeBuilder("Bronzong", 0.86f, BodyPlan.Floating, V(0, 0.45f, 0)) { Coat = Metal }.Hover();
        var green = Rgb(96, 158, 160);
        var dark = Rgb(56, 100, 106);
        var red = Rgb(214, 70, 60);

        // A great bell, its mouth dark, a loop on top, and arms like flat bars
        b.Ell(Body, V(0, 0.5f, 0), V(0.19f, 0.3f, 0.19f), green);
        b.Ell(Body, V(0, 0.28f, 0), V(0.23f, 0.12f, 0.23f), green);
        b.Torus(Body, V(0, 0.18f, 0), 0.21f, 0.04f, green, mat: Metal, blend: 0.015f);
        b.PaintEll(Body, V(0, 0.14f, 0), V(0.18f, 0.05f, 0.18f), Rgb(30, 40, 46));
        foreach (float y in new[] { 0.62f, 0.36f })
            b.PaintTorus(Body, V(0, y, 0), y > 0.5f ? 0.18f : 0.215f, 0.012f, dark);
        b.Torus(Body, V(0, 0.84f, 0), 0.06f, 0.022f, green, V(90f, 0, 0), mat: Metal, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.62f, 0));
            b.Box(arm, V(0.3f * s, 0.63f, 0), V(0.13f, 0.028f, 0.045f), 0.015f, green, mat: Metal, blend: 0.01f);
            b.Box(arm, V(0.44f * s, 0.62f, 0), V(0.035f, 0.06f, 0.055f), 0.02f, green, mat: Metal, blend: 0.01f);
        });
        // Its face: two great red eyes and the lesser ones round them
        foreach (var (x, y) in new[] { (-0.12f, 0.38f), (0.12f, 0.38f), (0f, 0.66f) })
            b.Mark(Body, V(x, y, 0.2f), V(x * 2f, 0, 1f), 0.025f, 0.025f, red, MarkShape.Ring);
        PokeBuilder.Both(s => b.Eye(Body, V(0.08f * s, 0.5f, 0.18f), V(0.4f * s, 0, 1f), 0.045f, pupil: Rgb(40, 20, 20), white: red));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Ponyta line

    /// <summary>A tongue of fire from <paramref name="at"/> along <paramref name="dir"/>: orange round a yellow heart, glowing.</summary>
    private static void Blaze(PokeBuilder b, int bone, Vector3 at, Vector3 dir, float length, float width)
    {
        var d = Vector3.Normalize(dir);
        var orange = Rgb(250, 126, 42);
        b.Ell(bone, at + d * length * 0.32f, V(width, length * 0.36f, width * 0.8f), orange, Euler(d), Glow, 0.02f);
        b.Spike(bone, at + d * length * 0.45f, at + d * length, width * 0.75f, Rgb(242, 90, 40), 0.7f, Glow, 0.02f);
        b.PaintEll(bone, at + d * length * 0.3f, V(width * 0.7f, length * 0.3f, width * 0.7f), Rgb(255, 224, 96), Euler(d));
    }

    private static PokeBuilder Horse(bool grown)
    {
        var b = new PokeBuilder(grown ? "Rapidash" : "Ponyta", grown ? 0.9f : 0.78f, BodyPlan.Quadruped, V(0, 0.5f, -0.02f)) { Coat = Fur };
        var cream = Rgb(252, 240, 192);
        var hoof = Rgb(140, 130, 170);
        float k = grown ? 1.12f : 1f;

        foreach (var (z, front) in new[] { (0.16f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s, 0.42f, z * k), front);
                b.Limb(leg, V(0.07f * s, 0.42f, z * k), V(0.075f * s, 0.2f, (z + 0.01f) * k), 0.045f, 0.03f, cream);
                b.Limb(leg, V(0.075f * s, 0.2f, (z + 0.01f) * k), V(0.075f * s, 0.05f, (z + 0.02f) * k), 0.03f, 0.028f, cream);
                b.Ell(leg, V(0.075f * s, 0.03f, (z + 0.03f) * k), V(0.034f, 0.03f, 0.04f), hoof, mat: Shell);
                // Flames burn at the backs of its legs
                Blaze(b, leg, V(0.075f * s, 0.16f, (z - 0.01f) * k), V(0, 0.4f, -1f), grown ? 0.12f : 0.07f, 0.028f);
            });
        b.Ell(Body, V(0, 0.5f, -0.02f), V(0.12f, 0.12f, 0.25f * k), cream);
        int tail = b.Tail(V(0, 0.56f, -0.24f * k));
        foreach (var (dir, len) in new[] { (V(0, 0.5f, -1f), 0.3f), (V(0.3f, 0.9f, -1f), 0.24f), (V(-0.3f, 0.7f, -1f), 0.22f) })
            Blaze(b, tail, V(0, 0.56f, -0.24f * k), dir, len * k, 0.06f * k);

        int head = b.Head(V(0, 0.6f, 0.18f * k));
        b.Limb(head, V(0, 0.56f, 0.16f * k), V(0, 0.76f, 0.26f * k), 0.075f, 0.06f, cream);
        b.Ell(head, V(0, 0.8f, 0.31f * k), V(0.07f, 0.07f, 0.11f), cream);
        b.Ell(head, V(0, 0.76f, 0.39f * k), V(0.05f, 0.05f, 0.06f), cream);
        PokeBuilder.Both(s => b.Mark(head, V(0.025f * s, 0.77f, 0.445f * k), V(0.3f * s, 0, 1f), 0.008f, 0.006f, Rgb(120, 100, 90)));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.04f * s, 0.86f, 0.27f * k));
            b.Spike(ear, V(0.04f * s, 0.85f, 0.27f * k), V(0.06f * s, 0.94f, 0.24f * k), 0.025f, cream, 0.5f);
        });
        // A mane of fire down its neck, and on the grown one a horn
        int mane = b.Part("mane", head, V(0, 0.82f, 0.24f * k), PokeRole.Flame);
        foreach (var (at, dir, len) in new[] { (V(0, 0.87f, 0.27f), V(0, 1f, -0.5f), 0.22f), (V(0, 0.78f, 0.22f), V(0, 0.8f, -1f), 0.24f), (V(0, 0.68f, 0.17f), V(0, 0.6f, -1f), 0.2f) })
            Blaze(b, mane, at * V(1, 1, k), dir, len * k, 0.06f * k);
        if (grown) b.Spike(head, V(0, 0.85f, 0.34f * k), V(0, 0.98f, 0.42f * k), 0.025f, Rgb(250, 246, 236), mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.82f, 0.37f * k), V(0.8f * s, 0.1f, 0.6f), 0.022f, Rgb(170, 60, 50)));
        return b;
    }

    // ------------------------------------------------------------------ Bonsly line

    /// <summary>A round crown of leaves on a short stem.</summary>
    private static void Foliage(PokeBuilder b, int bone, Vector3 from, Vector3 at, float r, Color stem, Color green)
    {
        b.Limb(bone, from, at, r * 0.22f, r * 0.2f, stem);
        b.Ell(bone, at, V(r, r * 0.95f, r), green, mat: Leaf, blend: 0.012f);
    }

    private static PokeBuilder Bonsly()
    {
        var b = new PokeBuilder("Bonsly", 0.56f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Leaf };
        var brown = Rgb(186, 128, 72);
        var rim = Rgb(150, 98, 54);
        var green = Rgb(76, 176, 70);
        var yellow = Rgb(250, 220, 80);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.1f, 0.02f));
            b.Ell(leg, V(0.075f * s, 0.095f, 0.03f), V(0.035f, 0.03f, 0.035f), rim);
            b.Ell(leg, V(0.08f * s, 0.04f, 0.04f), V(0.05f, 0.04f, 0.06f), rim);
        });
        // A little tree in its pot: a tapering trunk with yellow spots, the pot's rim, three round crowns of leaves
        b.Ell(Body, V(0, 0.22f, 0), V(0.15f, 0.1f, 0.14f), brown);
        b.Spike(Body, V(0, 0.22f, 0), V(0, 0.52f, -0.01f), 0.13f, brown);
        b.Torus(Body, V(0, 0.15f, 0), 0.14f, 0.035f, rim, blend: 0.012f);
        foreach (var (x, y) in new[] { (0f, 0.33f), (-0.06f, 0.24f), (0.06f, 0.24f) })
            b.Mark(Body, V(x, y, 0.12f - Math.Abs(x) * 0.25f), V(x * 3f, 0.3f, 1f), 0.018f, 0.025f, yellow);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.28f, 0.02f));
            b.Limb(arm, V(0.1f * s, 0.28f, 0.02f), V(0.16f * s, 0.24f, 0.05f), 0.022f, 0.02f, brown);
        });
        int head = b.Head(V(0, 0.48f, -0.01f));
        Foliage(b, head, V(0, 0.48f, -0.01f), V(0, 0.64f, -0.02f), 0.08f, brown, green);
        PokeBuilder.Both(s => Foliage(b, head, V(0.02f * s, 0.46f, -0.01f), V(0.13f * s, 0.55f, 0.0f), 0.072f, brown, green));
        PokeBuilder.Both(s => b.Eye(Body, V(0.045f * s, 0.3f, 0.11f), V(0.35f * s, 0.1f, 1f), 0.026f));
        return b;
    }

    private static PokeBuilder Sudowoodo()
    {
        var b = new PokeBuilder("Sudowoodo", 0.82f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Leaf };
        var brown = Rgb(150, 108, 70);
        var green = Rgb(64, 172, 64);
        var yellow = Rgb(214, 220, 80);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.18f, 0));
            b.Limb(leg, V(0.06f * s, 0.18f, 0), V(0.1f * s, 0.05f, 0.02f), 0.055f, 0.045f, brown);
            b.Ell(leg, V(0.1f * s, 0.03f, 0.04f), V(0.05f, 0.03f, 0.065f), brown);
        });
        // A trunk of a body, yellow-green buds down its front, branches for arms ending in balls of leaves
        b.Tube(Body, new[] { V(0, 0.16f, 0), V(0.01f, 0.4f, 0.0f), V(0, 0.66f, -0.01f) }, 0.12f, 0.1f, brown, blend: 0.02f);
        foreach (var (x, y) in new[] { (0.02f, 0.5f), (-0.04f, 0.4f), (0.05f, 0.32f), (-0.03f, 0.24f) })
            b.Mark(Body, V(x, y, 0.11f), V(x * 3f, 0, 1f), 0.016f, 0.024f, yellow);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.54f, 0));
            b.Tube(arm, new[] { V(0.08f * s, 0.54f, 0), V(0.18f * s, 0.6f, 0.02f), V(0.28f * s, 0.68f, 0.03f) }, 0.04f, 0.03f, brown);
            Foliage(b, arm, V(0.28f * s, 0.68f, 0.03f), V(0.31f * s, 0.78f, 0.03f), 0.055f, brown, green);
            Foliage(b, arm, V(0.28f * s, 0.68f, 0.03f), V(0.38f * s, 0.69f, 0.03f), 0.055f, brown, green);
            Foliage(b, arm, V(0.28f * s, 0.68f, 0.03f), V(0.33f * s, 0.6f, 0.05f), 0.05f, brown, green);
        });
        int head = b.Head(V(0, 0.64f, 0));
        b.Ell(head, V(0, 0.68f, 0.0f), V(0.1f, 0.08f, 0.09f), brown);
        b.Tube(head, new[] { V(0, 0.72f, 0), V(0, 0.82f, -0.01f) }, 0.035f, 0.03f, brown);
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0, 0.82f, -0.01f), V(0.05f * s, 0.88f, -0.01f), V(0.08f * s, 0.92f, -0.01f) }, 0.028f, 0.022f, brown));
        b.Mark(Body, V(0, 0.62f, 0.098f), V(0, 0, 1f), 0.035f, 0.007f, Rgb(70, 40, 30), MarkShape.Bar);
        PokeBuilder.Both(s => b.Eye(head, V(0.04f * s, 0.685f, 0.09f), V(0.35f * s, 0.05f, 1f), 0.018f));
        return b;
    }

    // ------------------------------------------------------------------ Mr. Mime line

    private static PokeBuilder MimeJr()
    {
        var b = new PokeBuilder("Mime Jr.", 0.6f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var skin = Rgb(250, 214, 226);
        var pink = Rgb(240, 150, 180);
        var blue = Rgb(44, 92, 172);
        var red = Rgb(230, 52, 72);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.18f, 0));
            b.Limb(leg, V(0.05f * s, 0.18f, 0), V(0.06f * s, 0.06f, 0.01f), 0.03f, 0.026f, skin);
            b.Ell(leg, V(0.065f * s, 0.035f, 0.04f), V(0.04f, 0.035f, 0.06f), blue);
        });
        b.Ell(Body, V(0, 0.28f, 0), V(0.09f, 0.1f, 0.08f), skin);
        b.Mark(Body, V(0, 0.27f, 0.08f), V(0, 0, 1f), 0.04f, 0.04f, pink);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.34f, 0.01f));
            var hand = s > 0 ? V(0.2f, 0.46f, 0.05f) : V(-0.18f, 0.26f, 0.06f);
            b.Tube(arm, new[] { V(0.07f * s, 0.34f, 0.01f), hand }, 0.022f, 0.02f, skin);
            b.Ell(arm, hand, V(0.035f, 0.035f, 0.03f), White);
        });

        int head = b.Head(V(0, 0.38f, 0));
        b.Ell(head, V(0, 0.5f, 0.01f), V(0.12f, 0.11f, 0.11f), skin);
        b.Ell(head, V(0, 0.48f, 0.12f), V(0.035f, 0.033f, 0.033f), red, mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s => b.Mark(head, V(0.075f * s, 0.46f, 0.09f), V(0.6f * s, 0, 1f), 0.022f, 0.016f, pink));
        b.Mark(head, V(0, 0.425f, 0.105f), V(0, -0.3f, 1f), 0.02f, 0.014f, Rgb(160, 40, 60));
        // A jester's cap: blue, a lobe at each side, its point topped by a white bobble
        b.Ell(head, V(0, 0.59f, -0.01f), V(0.13f, 0.07f, 0.12f), blue);
        PokeBuilder.Both(s => b.Ell(head, V(0.12f * s, 0.52f, -0.01f), V(0.05f, 0.075f, 0.055f), blue));
        b.Tube(head, Smooth(3, V(0, 0.64f, -0.01f), V(0.01f, 0.72f, 0.0f), V(0.04f, 0.78f, 0.03f)), 0.05f, 0.02f, blue);
        b.Ell(head, V(0.045f, 0.8f, 0.04f), V(0.03f, 0.03f, 0.03f), White);
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, 0.51f, 0.105f), V(0.35f * s, 0.05f, 1f), 0.024f));
        return b;
    }

    private static PokeBuilder MrMime()
    {
        var b = new PokeBuilder("Mr. Mime", 0.86f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Fur };
        var skin = Rgb(250, 206, 214);
        var pink = Rgb(238, 98, 120);
        var white = Rgb(244, 242, 246);
        var blue = Rgb(44, 92, 172);

        // Thin legs in curled blue shoes
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.46f, 0));
            b.Limb(leg, V(0.09f * s, 0.46f, 0), V(0.14f * s, 0.24f, 0.03f), 0.028f, 0.024f, skin);
            b.Limb(leg, V(0.14f * s, 0.24f, 0.03f), V(0.15f * s, 0.06f, 0.0f), 0.024f, 0.022f, skin);
            b.Ell(leg, V(0.16f * s, 0.035f, 0.05f), V(0.05f, 0.035f, 0.08f), blue);
            b.Tube(leg, new[] { V(0.17f * s, 0.04f, 0.11f), V(0.18f * s, 0.06f, 0.15f), V(0.17f * s, 0.09f, 0.15f) }, 0.022f, 0.014f, blue);
        });
        // A white body with round pink pads at the shoulders and hips, and a pink disc on its belly
        b.Ell(Body, V(0, 0.62f, 0), V(0.13f, 0.15f, 0.11f), white);
        b.Mark(Body, V(0, 0.58f, 0.11f), V(0, 0, 1f), 0.055f, 0.055f, pink);
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, V(0.11f * s, 0.48f, 0.0f), V(0.07f, 0.065f, 0.07f), pink);
            int arm = b.Arm(s, V(0.15f * s, 0.73f, 0));
            b.Ell(arm, V(0.16f * s, 0.73f, 0), V(0.07f, 0.065f, 0.07f), pink);
            // Big white hands, fingers spread against an invisible wall
            var elbow = s < 0 ? V(-0.28f, 0.82f, 0.06f) : V(0.3f, 0.64f, 0.06f);
            var hand = s < 0 ? V(-0.32f, 0.97f, 0.1f) : V(0.42f, 0.66f, 0.12f);
            b.Limb(arm, V(0.16f * s, 0.73f, 0), elbow, 0.025f, 0.022f, skin);
            b.Limb(arm, elbow, hand, 0.022f, 0.02f, skin);
            b.Ell(arm, hand, V(0.05f, 0.05f, 0.025f), white, Euler(hand - elbow));
            var up = Vector3.Normalize(hand - elbow);
            var side = Vector3.Normalize(Vector3.Cross(up, Vector3.UnitZ));
            for (int f = -2; f <= 2; f++)
            {
                var tip = hand + (up + side * f * 0.45f) * 0.09f;
                b.Limb(arm, hand, tip, 0.014f, 0.012f, white, blend: 0.006f);
                b.Ell(arm, tip, V(0.016f, 0.016f, 0.016f), pink, blend: 0.004f);
            }
        });

        int head = b.Head(V(0, 0.78f, 0));
        b.Ell(head, V(0, 0.88f, 0.02f), V(0.11f, 0.1f, 0.1f), skin);
        PokeBuilder.Both(s => b.Mark(head, V(0.07f * s, 0.84f, 0.09f), V(0.6f * s, 0, 1f), 0.026f, 0.022f, pink));
        b.Mark(head, V(0, 0.82f, 0.105f), V(0, -0.3f, 1f), 0.03f, 0.018f, Rgb(160, 40, 60));
        // Blue hair swept up into two horns
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, V(0.06f * s, 0.94f, -0.02f), V(0.13f * s, 1.0f, -0.03f), V(0.2f * s, 1.02f, -0.04f), V(0.26f * s, 1.0f, -0.04f)), 0.05f, 0.016f, blue));
        PokeBuilder.Both(s => b.Eye(head, V(0.04f * s, 0.9f, 0.095f), V(0.35f * s, 0.05f, 1f), 0.024f));
        return b;
    }

    // ------------------------------------------------------------------ Chansey line

    private static PokeBuilder Happiny()
    {
        var b = new PokeBuilder("Happiny", 0.6f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var pink = Rgb(250, 190, 210);
        var deep = Rgb(240, 140, 170);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.1f, 0.02f));
            b.Ell(leg, V(0.075f * s, 0.11f, 0.03f), V(0.035f, 0.035f, 0.035f), deep);
            b.Ell(leg, V(0.075f * s, 0.06f, 0.04f), V(0.045f, 0.045f, 0.055f), deep);
        });
        // An egg of a body in a white skirt, holding the round white stone it thinks is an egg
        b.Ell(Body, V(0, 0.28f, 0), V(0.15f, 0.17f, 0.14f), pink);
        b.Torus(Body, V(0, 0.16f, 0), 0.14f, 0.045f, White, blend: 0.015f);
        b.Ell(Body, V(0, 0.22f, 0.13f), V(0.055f, 0.05f, 0.045f), White, mat: Shell);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.27f, 0.06f));
            b.Ell(arm, V(0.08f * s, 0.24f, 0.13f), V(0.035f, 0.03f, 0.035f), pink);
        });
        // Its face is the front of its body
        PokeBuilder.Both(s => b.Mark(Body, V(0.085f * s, 0.32f, 0.115f), V(0.6f * s, 0, 1f), 0.024f, 0.016f, Rgb(240, 110, 140)));
        b.Mark(Body, V(0, 0.3f, 0.137f), V(0, -0.2f, 1f), 0.016f, 0.012f, Rgb(170, 50, 80));
        PokeBuilder.Both(s => b.Eye(Body, V(0.045f * s, 0.36f, 0.13f), V(0.35f * s, 0.05f, 1f), 0.025f));
        // Curls: a twist of hair on top, one rolled up at each side
        int head = b.Head(V(0, 0.4f, 0));
        b.Tube(head, Smooth(3, V(0, 0.43f, -0.01f), V(0.02f, 0.5f, -0.03f), V(0.07f, 0.53f, -0.05f), V(0.09f, 0.49f, -0.06f)), 0.05f, 0.03f, pink);
        PokeBuilder.Both(s => b.Torus(head, V(0.13f * s, 0.4f, 0.0f), 0.03f, 0.022f, pink, V(0, 0, 90f), blend: 0.01f));
        return b;
    }

    private static PokeBuilder Chansey()
    {
        var b = new PokeBuilder("Chansey", 0.8f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Fur };
        var pink = Rgb(250, 180, 200);
        var deep = Rgb(240, 140, 170);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.12f, 0.02f));
            b.Ell(leg, V(0.12f * s, 0.06f, 0.04f), V(0.06f, 0.045f, 0.08f), deep);
        });
        b.Ell(Body, V(0, 0.38f, 0), V(0.25f, 0.3f, 0.22f), pink);
        // The pouch on its belly with an egg in it
        b.PaintEll(Body, V(0, 0.24f, 0.17f), V(0.13f, 0.1f, 0.08f), PixelCanvas.Light1(pink, 0.25f));
        b.Ell(Body, V(0, 0.28f, 0.19f), V(0.07f, 0.085f, 0.05f), White, mat: Shell);
        b.PaintEll(Body, V(0, 0.22f, 0.21f), V(0.11f, 0.05f, 0.06f), deep);
        int tail = b.Tail(V(0, 0.2f, -0.2f));
        b.Spike(tail, V(0, 0.2f, -0.19f), V(0, 0.14f, -0.3f), 0.04f, pink);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.34f, 0.12f));
            b.Ell(arm, V(0.12f * s, 0.3f, 0.2f), V(0.04f, 0.035f, 0.04f), pink);
        });
        // A small happy face high on its front
        b.Mark(Body, V(0, 0.47f, 0.205f), V(0, 0.15f, 1f), 0.03f, 0.012f, Rgb(170, 70, 100), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(Body, V(0.055f * s, 0.54f, 0.19f), V(0.35f * s, 0.2f, 1f), 0.018f));
        // Three tendrils either side of the top of its body
        int head = b.Head(V(0, 0.52f, 0));
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
                b.Spike(head, V(0.21f * s, 0.56f - i * 0.05f, 0.0f), V(0.34f * s, 0.6f - i * 0.08f, -0.02f), 0.035f, pink, 0.6f);
        });
        b.Ell(head, V(0, 0.62f, -0.02f), V(0.18f, 0.06f, 0.14f), pink);
        return b;
    }

    private static PokeBuilder Blissey()
    {
        var b = new PokeBuilder("Blissey", 0.88f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Fur };
        var pink = Rgb(250, 190, 210);
        var deep = Rgb(240, 140, 170);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.14f, 0.02f));
            b.Ell(leg, V(0.1f * s, 0.07f, 0.04f), V(0.055f, 0.05f, 0.07f), deep);
        });
        // Round and pink above, white below like a dress, with white frills at its sides
        b.Ell(Body, V(0, 0.4f, 0), V(0.25f, 0.29f, 0.22f), pink);
        b.PaintEll(Body, V(0, 0.22f, 0), V(0.3f, 0.17f, 0.27f), White);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 4; i++)
                b.Spike(Body, V(0.22f * s, 0.34f - i * 0.05f, -0.02f), V(0.36f * s, 0.38f - i * 0.08f, -0.05f), 0.04f, White, 0.45f);
        });
        // The egg in its pouch, pink above and white below
        b.Ell(Body, V(0, 0.32f, 0.19f), V(0.08f, 0.095f, 0.055f), White, mat: Shell);
        b.PaintEll(Body, V(0, 0.4f, 0.2f), V(0.09f, 0.05f, 0.08f), deep);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.38f, 0.12f));
            b.Ell(arm, V(0.12f * s, 0.34f, 0.2f), V(0.04f, 0.035f, 0.04f), pink);
        });
        b.Mark(Body, V(0, 0.5f, 0.205f), V(0, -0.1f, 1f), 0.016f, 0.012f, Rgb(170, 50, 80));
        PokeBuilder.Both(s => b.Eye(Body, V(0.06f * s, 0.56f, 0.19f), V(0.35f * s, 0.15f, 1f), 0.02f));
        // Curls of hair rolled up either side of its head, like buns
        int head = b.Head(V(0, 0.56f, 0));
        PokeBuilder.Both(s =>
        {
            b.Ell(head, V(0.14f * s, 0.64f, -0.02f), V(0.09f, 0.08f, 0.08f), pink);
            b.Mark(head, V(0.19f * s, 0.66f, 0.03f), V(0.7f * s, 0.3f, 0.6f), 0.035f, 0.035f, deep, MarkShape.Ring);
        });
        b.Ell(head, V(0, 0.68f, -0.03f), V(0.08f, 0.05f, 0.07f), pink);
        return b;
    }

    // ------------------------------------------------------------------ Clefairy line

    /// <summary>A curl of hair or tail: a tube winding into a spiral round <paramref name="center"/>, in the plane of u and v.</summary>
    private static void Curl(PokeBuilder b, int bone, Vector3 from, Vector3 center, Vector3 u, Vector3 v, float r, float turns, float thick, Color color) =>
        b.Tube(bone, new[] { from }.Concat(Spiral(center, u, v, r, r * 0.3f, 0f, MathF.Tau * turns, (int)(12 * turns))).ToArray(), thick, thick * 0.7f, color);

    private static PokeBuilder Cleffa()
    {
        var b = new PokeBuilder("Cleffa", 0.5f, BodyPlan.Biped, V(0, 0.26f, 0)) { Coat = Fur };
        var pink = Rgb(244, 182, 182);
        var brown = Rgb(116, 74, 52);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.1f, 0.02f));
            b.Ell(leg, V(0.08f * s, 0.05f, 0.05f), V(0.05f, 0.045f, 0.055f), pink);
        });
        // A star of a body: a big round head-and-body, broad ears dark at their points, a curl on top
        b.Ell(Body, V(0, 0.27f, 0), V(0.17f, 0.19f, 0.15f), pink);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.22f, 0.07f));
            b.Ell(arm, V(0.12f * s, 0.2f, 0.12f), V(0.035f, 0.035f, 0.035f), pink);
        });
        PokeBuilder.Both(s => b.Mark(Body, V(0.09f * s, 0.27f, 0.125f), V(0.6f * s, 0, 1f), 0.024f, 0.014f, Rgb(240, 120, 130)));
        b.Mark(Body, V(0, 0.25f, 0.148f), V(0, -0.1f, 1f), 0.025f, 0.009f, Rgb(80, 40, 40), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(Body, V(0.05f * s, 0.31f, 0.138f), V(0.35f * s, 0.05f, 1f), 0.02f));
        int head = b.Head(V(0, 0.38f, 0));
        // Thick brown ears out to the sides, drooping to round points
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.1f * s, 0.4f, -0.01f));
            b.Spike(ear, V(0.09f * s, 0.41f, -0.01f), V(0.3f * s, 0.34f, -0.04f), 0.085f, brown, 0.6f);
            b.Ell(ear, V(0.275f * s, 0.35f, -0.035f), V(0.03f, 0.03f, 0.022f), brown);
        });
        // A lock rising from the crown and winding into a curl, facing forward
        Curl(b, head, V(-0.05f, 0.42f, 0.03f), V(0.01f, 0.46f, 0.045f), V(-1f, 0, 0), V(0, 1f, 0), 0.058f, 1.15f, 0.036f, pink);
        return b;
    }

    private static PokeBuilder Fairy(bool grown)
    {
        var b = new PokeBuilder(grown ? "Clefable" : "Clefairy", grown ? 0.84f : 0.62f, BodyPlan.Biped, V(0, grown ? 0.36f : 0.3f, 0)) { Coat = Fur };
        var pink = Rgb(250, 196, 208);
        var brown = Rgb(116, 74, 52);
        float k = grown ? 1.25f : 1f;
        float by = 0.3f * k;

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s * k, 0.1f * k, 0.01f));
            b.Limb(leg, V(0.07f * s * k, 0.1f * k, 0.01f), V(0.08f * s * k, 0.04f, 0.03f), 0.045f * k, 0.04f * k, pink);
            b.Ell(leg, V(0.085f * s * k, 0.03f, 0.05f), V(0.045f * k, 0.03f, 0.06f * k), pink);
        });
        b.Ell(Body, V(0, by, 0), V(0.16f * k, (grown ? 0.22f : 0.2f) * k, 0.14f * k), pink);
        // Little wings on its back, and a tail that curls round
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.06f * s * k, by + 0.08f * k, -0.1f * k));
            int n = grown ? 3 : 1;
            for (int i = 0; i < n; i++)
                Petal(b, wing, V(0.06f * s * k, by + 0.08f * k, -0.11f * k), V((0.2f + 0.04f * i) * s * k, by + (0.22f - 0.09f * i) * k, -0.16f * k), 0.045f * k, pink, Fur);
        });
        int tail = b.Tail(V(0, by - 0.1f * k, -0.12f * k));
        Curl(b, tail, V(0, by - 0.1f * k, -0.12f * k), V(0, by - 0.08f * k, -0.2f * k), V(0, 0, -1f), V(0, 1f, 0), 0.045f * k, 1.1f, 0.03f * k, pink);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s * k, by + 0.07f * k, 0.03f));
            var hand = s < 0 ? V(-0.24f * k, by + 0.18f * k, 0.06f) : V(0.2f * k, by - 0.02f * k, 0.08f);
            b.Tube(arm, new[] { V(0.12f * s * k, by + 0.07f * k, 0.03f), hand }, 0.032f * k, 0.03f * k, pink);
            b.Ell(arm, hand, V(0.035f * k, 0.035f * k, 0.035f * k), pink);
        });
        // A face on its round front: a little fang in a smile, rosy cheeks
        PokeBuilder.Both(s => b.Mark(Body, V(0.085f * s * k, by + 0.03f * k, 0.12f * k), V(0.6f * s, 0, 1f), 0.024f * k, 0.014f * k, Rgb(240, 120, 140)));
        b.Mark(Body, V(0, by + 0.01f * k, 0.138f * k), V(0, -0.1f, 1f), 0.024f * k, 0.01f * k, Rgb(90, 40, 50), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(Body, V(0.045f * s * k, by + 0.07f * k, 0.13f * k), V(0.35f * s, 0.05f, 1f), 0.02f * k));
        int head = b.Head(V(0, by + 0.12f * k, 0));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s * k, by + 0.15f * k, -0.01f));
            var tip = V((grown ? 0.24f : 0.2f) * s * k, by + (grown ? 0.38f : 0.34f) * k, -0.05f * k);
            b.Spike(ear, V(0.08f * s * k, by + 0.15f * k, -0.01f), tip, 0.05f * k, pink, 0.5f);
            b.PaintEll(ear, tip - (tip - V(0.08f * s * k, by + 0.15f * k, 0)) * 0.12f, V(0.035f * k, 0.04f * k, 0.03f * k), brown);
        });
        // A lock from the crown wound flat on its forehead, like a snail's shell
        float ry = grown ? 0.22f : 0.2f, fy = 0.17f, fz = 0.14f * MathF.Sqrt(1f - fy * fy / (ry * ry));
        var n = Vector3.Normalize(V(0, fy / (ry * ry), fz / (0.14f * 0.14f)));
        Curl(b, head, V(0, by + 0.2f * k, 0), V(0, by + fy * k, fz * k) + n * 0.012f * k, Vector3.Cross(n, V(1f, 0, 0)), V(1f, 0, 0),
            0.045f * k, 1.3f, 0.026f * k, pink);
        return b;
    }

    // ------------------------------------------------------------------ Chatot

    private static PokeBuilder Chatot()
    {
        var b = new PokeBuilder("Chatot", 0.56f, BodyPlan.Bird, V(0, 0.22f, 0)) { Coat = Fur };
        var black = Rgb(56, 58, 68);
        var blue = Rgb(60, 162, 232);
        var green = Rgb(56, 178, 96);
        var yellow = Rgb(250, 222, 64);
        var pink = Rgb(242, 120, 146);

        // Short legs gripping as on a perch
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.12f, 0.0f));
            b.Limb(leg, V(0.04f * s, 0.12f, 0.0f), V(0.045f * s, 0.035f, 0.02f), 0.018f, 0.015f, yellow);
            Digits(b, leg, V(0.045f * s, 0.022f, 0.03f), V(0, -0.2f, 1f), V(0.6f, 0, 0), 0.035f, 0.011f, yellow);
        });
        // A blue body over a green belly, blue wings with yellow shoulders, a tail like a metronome
        b.Ell(Body, V(0, 0.22f, 0), V(0.1f, 0.12f, 0.13f), blue, V(-15f, 0, 0));
        b.PaintEll(Body, V(0, 0.15f, 0.03f), V(0.1f, 0.06f, 0.11f), green);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.08f * s, 0.26f, -0.01f));
            b.Ell(w, V(0.1f * s, 0.21f, -0.05f), V(0.035f, 0.09f, 0.13f), blue, V(-20f, 8f * s, 0));
            b.PaintEll(w, V(0.1f * s, 0.27f, 0.02f), V(0.05f, 0.045f, 0.06f), yellow);
            b.Spike(w, V(0.11f * s, 0.16f, -0.12f), V(0.12f * s, 0.14f, -0.22f), 0.03f, blue, 0.4f);
        });
        int tail = b.Tail(V(0, 0.18f, -0.12f));
        PokeBuilder.Both(s => b.Spike(tail, V(0.02f * s, 0.18f, -0.12f), V(0.05f * s, 0.15f, -0.26f), 0.035f, green, 0.45f));
        b.Tube(tail, new[] { V(0, 0.19f, -0.12f), V(0, 0.26f, -0.24f), V(0, 0.34f, -0.34f) }, 0.012f, 0.01f, black);
        b.Ell(tail, V(0, 0.35f, -0.36f), V(0.032f, 0.032f, 0.032f), black);

        int head = b.Head(V(0, 0.3f, 0.05f));
        // A frill of white round its neck, a black head crowned by a note, and a pink beak
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var at = V(MathF.Sin(a) * 0.085f, 0.3f, MathF.Cos(a) * 0.08f + 0.03f);
            b.Spike(head, at, at + V(MathF.Sin(a) * 0.05f, -0.025f, MathF.Cos(a) * 0.045f), 0.03f, White, 0.45f);
        }
        b.Ell(head, V(0, 0.38f, 0.06f), V(0.085f, 0.08f, 0.08f), black);
        b.Ell(head, V(0, 0.36f, 0.14f), V(0.035f, 0.032f, 0.035f), pink, mat: Shell);
        b.Spike(head, V(0, 0.35f, 0.16f), V(0, 0.32f, 0.19f), 0.02f, pink, mat: Shell);
        b.Tube(head, new[] { V(0, 0.44f, 0.04f), V(0, 0.52f, 0.02f), V(0, 0.58f, 0.0f) }, 0.014f, 0.012f, black);
        b.Ell(head, V(0, 0.56f, -0.05f), V(0.016f, 0.045f, 0.055f), black, V(-30f, 0, 0));
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.395f, 0.115f), V(0.6f * s, 0.05f, 0.8f), 0.022f, sclera: true, pupil: Rgb(40, 30, 40)));
        return b;
    }

    // ------------------------------------------------------------------ Pichu line

    /// <summary>A lightning bolt: a flat zigzag through <paramref name="corners"/>, each span a thin box as wide as given.</summary>
    private static void Bolt(PokeBuilder b, int bone, Vector3[] corners, float[] widths, float thickness, Color color, Color? first = null)
    {
        for (int i = 1; i < corners.Length; i++)
        {
            var d = corners[i] - corners[i - 1];
            float w = widths[Math.Min(widths.Length - 1, i - 1)];
            b.Box(bone, (corners[i - 1] + corners[i]) / 2f, V(thickness, d.Length() / 2f + w * 0.5f, w), Math.Min(thickness, w) * 0.6f,
                i == 1 && first is Color c ? c : color, Euler(d), blend: 0.006f);
        }
    }

    /// <summary>A mouse's long ear from <paramref name="root"/> to <paramref name="tip"/>, its point dipped in <paramref name="tipColor"/>.</summary>
    private static void MouseEar(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float r, Color color, Color tipColor, float dipped = 0.3f)
    {
        b.Spike(bone, root, tip, r, color, 0.55f);
        var d = tip - root;
        b.PaintEll(bone, tip - d * dipped * 0.45f, V(r * 1.4f, d.Length() * dipped * 0.6f, r * 1.4f), tipColor, Euler(d));
    }

    private static PokeBuilder Pichu(bool spikyEared = false)
    {
        var b = new PokeBuilder(spikyEared ? "Pichu-Spiky-Eared" : "Pichu", 0.5f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var yellow = Rgb(250, 220, 72);
        var black = Rgb(40, 34, 36);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.08f, 0.01f));
            b.Limb(leg, V(0.05f * s, 0.1f, 0.01f), V(0.058f * s, 0.035f, 0.025f), 0.034f, 0.026f, yellow);
            b.Ell(leg, V(0.06f * s, 0.025f, 0.04f), V(0.038f, 0.025f, 0.055f), yellow);
        });
        b.Ell(Body, V(0, 0.17f, 0), V(0.09f, 0.1f, 0.08f), yellow);
        // The black collar of fur round its neck, a short dark tail like a little bolt
        b.Mark(Body, V(0, 0.235f, 0.07f), V(0, 0.4f, 1f), 0.05f, 0.016f, black, MarkShape.Zigzag);
        int tail = b.Tail(V(0, 0.14f, -0.07f));
        Bolt(b, tail, new[] { V(0, 0.14f, -0.07f), V(0, 0.2f, -0.12f), V(0, 0.19f, -0.17f), V(0, 0.25f, -0.21f) }, new[] { 0.016f, 0.02f, 0.024f }, 0.012f, black);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.2f, 0.03f));
            b.Limb(arm, V(0.07f * s, 0.2f, 0.03f), V(0.12f * s, 0.17f, 0.06f), 0.022f, 0.02f, yellow);
        });

        int head = b.Head(V(0, 0.26f, 0));
        b.Ell(head, V(0, 0.35f, 0.01f), V(0.14f, 0.12f, 0.12f), yellow);
        // Huge ears rimmed in black, rosy cheeks
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.43f, -0.01f));
            Petal(b, ear, V(0.07f * s, 0.42f, -0.01f), V(0.25f * s, 0.6f, -0.04f), 0.07f, yellow, Fur, 0.015f);
            b.PaintEll(ear, V(0.22f * s, 0.57f, -0.035f), V(0.06f, 0.06f, 0.05f), black, V(0, 0, -45f * s));
            b.PaintEll(ear, V(0.19f * s, 0.6f, -0.035f), V(0.045f, 0.03f, 0.05f), black, V(0, 0, -45f * s));
        });
        PokeBuilder.Both(s => b.Mark(head, V(0.1f * s, 0.31f, 0.08f), V(0.75f * s, -0.05f, 0.65f), 0.026f, 0.022f, Rgb(244, 140, 160)));
        b.Mark(head, V(0, 0.335f, 0.124f), V(0, 0, 1f), 0.008f, 0.006f, black);
        b.Mark(head, V(0, 0.31f, 0.12f), V(0, -0.2f, 1f), 0.018f, 0.007f, Rgb(80, 30, 30), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(head, V(0.055f * s, 0.365f, 0.112f), V(0.4f * s, 0.05f, 1f), 0.03f));
        if (spikyEared) SpikyEar(b);
        return b;
    }

    /// <param name="form">One of Pikachu's caps or costumes (PokemonModels.Pikachu.cs), or none for Pikachu itself.</param>
    private static PokeBuilder Pikachu(string? form = null)
    {
        var b = new PokeBuilder(form ?? "Pikachu", 0.56f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Fur };
        var yellow = Rgb(250, 212, 52);
        var brown = Rgb(150, 92, 44);
        var black = Rgb(40, 30, 30);
        var red = Rgb(232, 52, 52);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.1f, 0.0f));
            b.Limb(leg, V(0.06f * s, 0.1f, 0.0f), V(0.07f * s, 0.04f, 0.02f), 0.045f, 0.035f, yellow);
            b.Ell(leg, V(0.072f * s, 0.025f, 0.045f), V(0.045f, 0.026f, 0.065f), yellow);
        });
        // A round little body, two brown stripes across its back
        b.Ell(Body, V(0, 0.19f, -0.01f), V(0.115f, 0.13f, 0.1f), yellow);
        b.Ell(Body, V(0, 0.13f, 0.0f), V(0.12f, 0.08f, 0.1f), yellow);
        foreach (float y in new[] { 0.245f, 0.19f })
            b.PaintEll(Body, V(0, y, -0.08f), V(0.085f, 0.013f, 0.05f), brown, soft: 0.009f);
        // The tail shaped like a lightning bolt, brown where it joins the body, leaning out to one side as it rises
        int tail = b.Tail(V(0, 0.15f, -0.08f));
        Bolt(b, tail, new[] { V(0, 0.15f, -0.08f), V(0.03f, 0.22f, -0.15f), V(0.07f, 0.31f, -0.12f), V(0.09f, 0.35f, -0.24f), V(0.15f, 0.47f, -0.2f), V(0.18f, 0.52f, -0.34f) },
            new[] { 0.02f, 0.03f, 0.036f, 0.046f, 0.062f }, 0.014f, yellow, brown);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.085f * s, 0.24f, 0.03f));
            b.Limb(arm, V(0.085f * s, 0.24f, 0.03f), V(0.14f * s, 0.2f, 0.08f), 0.028f, 0.024f, yellow);
            b.Ell(arm, V(0.145f * s, 0.195f, 0.09f), V(0.026f, 0.024f, 0.026f), yellow);
        });

        int head = b.Head(V(0, 0.28f, 0));
        b.Ell(head, V(0, 0.38f, 0.01f), V(0.15f, 0.125f, 0.13f), yellow);
        // Long ears with black tips, red cheeks that spark, a little nose and a smile
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, 0.47f, -0.01f));
            MouseEar(b, ear, V(0.07f * s, 0.46f, -0.01f), V(0.16f * s, 0.73f, -0.05f), 0.05f, yellow, black);
        });
        PokeBuilder.Both(s => b.Mark(head, V(0.105f * s, 0.35f, 0.085f), V(0.75f * s, -0.05f, 0.65f), 0.032f, 0.032f, red));
        b.Mark(head, V(0, 0.38f, 0.138f), V(0, 0, 1f), 0.011f, 0.008f, black);
        b.Mark(head, V(0, 0.355f, 0.132f), V(0, -0.2f, 1f), 0.024f, 0.008f, Rgb(90, 36, 30), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(head, V(0.058f * s, 0.4f, 0.118f), V(0.4f * s, 0.05f, 1f), 0.032f));
        if (form != null) DressPikachu(b, form, head, tail);
        return b;
    }

    private static PokeBuilder Raichu()
    {
        var b = new PokeBuilder("Raichu", 0.72f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var orange = Rgb(242, 150, 52);
        var cream = Rgb(250, 238, 210);
        var brown = Rgb(130, 82, 46);
        var black = Rgb(50, 40, 40);
        var yellow = Rgb(250, 214, 60);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.18f, 0));
            b.Limb(leg, V(0.07f * s, 0.18f, 0), V(0.09f * s, 0.05f, 0.02f), 0.05f, 0.04f, orange);
            b.Ell(leg, V(0.095f * s, 0.028f, 0.05f), V(0.05f, 0.028f, 0.075f), brown);
        });
        b.Ell(Body, V(0, 0.3f, 0), V(0.14f, 0.17f, 0.12f), orange);
        b.PaintEll(Body, V(0, 0.27f, 0.07f), V(0.1f, 0.13f, 0.08f), cream);
        // A long thin black tail ending in a yellow bolt
        int tail = b.Tail(V(0, 0.2f, -0.1f));
        b.Tube(tail, Smooth(3, V(0, 0.2f, -0.1f), V(0.03f, 0.1f, -0.24f), V(0.08f, 0.08f, -0.4f), V(0.12f, 0.14f, -0.52f)), 0.014f, 0.01f, black);
        Bolt(b, tail, new[] { V(0.12f, 0.14f, -0.52f), V(0.12f, 0.27f, -0.57f), V(0.12f, 0.24f, -0.68f), V(0.12f, 0.42f, -0.74f) }, new[] { 0.04f, 0.05f, 0.075f }, 0.014f, yellow);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.38f, 0.03f));
            b.Limb(arm, V(0.11f * s, 0.38f, 0.03f), V(0.18f * s, 0.32f, 0.08f), 0.032f, 0.028f, orange);
            b.Ell(arm, V(0.19f * s, 0.31f, 0.09f), V(0.03f, 0.028f, 0.03f), cream);
        });

        int head = b.Head(V(0, 0.44f, 0));
        b.Ell(head, V(0, 0.53f, 0.01f), V(0.13f, 0.11f, 0.11f), orange);
        b.Ell(head, V(0, 0.5f, 0.09f), V(0.06f, 0.045f, 0.04f), cream);
        // Ears that curl at the tips, yellow cheeks
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.6f, -0.01f));
            Petal(b, ear, V(0.08f * s, 0.6f, -0.01f), V(0.2f * s, 0.76f, -0.04f), 0.055f, brown, Fur, 0.015f);
            b.PaintEll(ear, V(0.12f * s, 0.65f, 0.0f), V(0.04f, 0.05f, 0.04f), orange);
            Curl(b, ear, V(0.2f * s, 0.76f, -0.04f), V(0.22f * s, 0.72f, -0.04f), V(s, 0, 0), V(0, 1, 0), 0.025f, 1f, 0.016f, brown);
        });
        PokeBuilder.Both(s => b.Mark(head, V(0.1f * s, 0.49f, 0.075f), V(0.75f * s, -0.05f, 0.65f), 0.03f, 0.022f, yellow));
        b.Mark(head, V(0, 0.515f, 0.127f), V(0, 0.2f, 1f), 0.011f, 0.008f, black);
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.55f, 0.1f), V(0.4f * s, 0.05f, 1f), 0.027f));
        return b;
    }

    // ------------------------------------------------------------------ Hoothoot line

    private static PokeBuilder Hoothoot()
    {
        var b = new PokeBuilder("Hoothoot", 0.64f, BodyPlan.Bird, V(0, 0.34f, 0)) { Coat = Fur };
        var brown = Rgb(178, 128, 88);
        var tan = Rgb(226, 196, 150);
        var black = Rgb(40, 36, 40);
        var red = Rgb(234, 96, 80);

        // It stands on one leg, keeping time
        int leg = b.Leg(1f, V(0, 0.18f, 0.02f));
        b.Limb(leg, V(0, 0.18f, 0.02f), V(0, 0.04f, 0.03f), 0.022f, 0.02f, Rgb(240, 214, 214));
        Digits(b, leg, V(0, 0.025f, 0.04f), V(0, -0.2f, 1f), V(0.6f, 0, 0), 0.04f, 0.012f, Rgb(240, 214, 214));
        b.Ell(Body, V(0, 0.34f, 0), V(0.17f, 0.17f, 0.15f), brown);
        b.PaintEll(Body, V(0, 0.28f, 0.08f), V(0.12f, 0.1f, 0.1f), tan);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.14f * s, 0.34f, 0));
            Petal(b, w, V(0.14f * s, 0.33f, 0.0f), V(0.28f * s, 0.27f, 0.02f), 0.05f, brown, Fur);
        });
        int head = b.Head(V(0, 0.42f, 0.02f));
        // Eyes like the face of a clock in black rings, two hands of black on top, a small beak
        PokeBuilder.Both(s =>
        {
            b.Ell(head, V(0.065f * s, 0.42f, 0.13f), V(0.062f, 0.062f, 0.025f), black);
            b.Spike(head, V(0.03f * s, 0.49f, 0.06f), V(0.13f * s, 0.62f, 0.02f), 0.035f, black, 0.45f);
        });
        b.Spike(head, V(0, 0.4f, 0.15f), V(0, 0.35f, 0.17f), 0.025f, Rgb(232, 206, 196), mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.065f * s, 0.42f, 0.155f), V(0.25f * s, 0, 1f), 0.046f, pupil: black, white: red));
        return b;
    }

    private static PokeBuilder Noctowl()
    {
        var b = new PokeBuilder("Noctowl", 0.88f, BodyPlan.Bird, V(0, 0.46f, 0)) { Coat = Fur };
        var brown = Rgb(150, 108, 76);
        var dark = Rgb(84, 58, 44);
        var tan = Rgb(232, 210, 170);
        var crest = Rgb(236, 220, 178);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.22f, 0.02f));
            b.Limb(leg, V(0.06f * s, 0.22f, 0.02f), V(0.07f * s, 0.05f, 0.04f), 0.04f, 0.03f, tan);
            Digits(b, leg, V(0.07f * s, 0.03f, 0.06f), V(0, -0.3f, 1f), V(0.6f, 0, 0), 0.05f, 0.012f, Claw, Shell);
        });
        // A tall owl: a lighter front marked with dark arrowheads, wings folded like a cloak
        b.Ell(Body, V(0, 0.46f, 0), V(0.15f, 0.24f, 0.14f), brown);
        b.PaintEll(Body, V(0, 0.42f, 0.08f), V(0.1f, 0.2f, 0.08f), tan);
        foreach (var (x, y) in new[] { (-0.05f, 0.5f), (0.05f, 0.5f), (-0.065f, 0.4f), (0f, 0.42f), (0.065f, 0.4f) })
            b.Mark(Body, V(x, y, 0.13f), V(x * 3f, 0, 1f), 0.03f, 0.028f, dark, MarkShape.Triangle);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.12f * s, 0.6f, -0.02f));
            b.Ell(w, V(0.15f * s, 0.44f, -0.03f), V(0.05f, 0.22f, 0.11f), dark, V(0, 0, 4f * s));
        });
        int tail = b.Tail(V(0, 0.28f, -0.12f));
        b.Spike(tail, V(0, 0.28f, -0.12f), V(0, 0.18f, -0.22f), 0.07f, dark, 0.4f);

        int head = b.Head(V(0, 0.66f, 0.02f));
        b.Ell(head, V(0, 0.75f, 0.03f), V(0.14f, 0.12f, 0.12f), brown);
        b.PaintEll(head, V(0, 0.72f, 0.11f), V(0.1f, 0.075f, 0.06f), tan);
        // A crest of pale feathers like a book held open, jagged at the top, a point of brown between them
        PokeBuilder.Both(s =>
        {
            var root = V(0.04f * s, 0.83f, 0.02f);
            Petal(b, head, root, V(0.15f * s, 0.97f, -0.01f), 0.07f, crest, Fur, 0.015f);
            foreach (var tip in new[] { V(0.1f * s, 1.03f, 0.0f), V(0.16f * s, 1.01f, -0.01f), V(0.22f * s, 0.96f, -0.02f) })
                b.Spike(head, root + (tip - root) * 0.45f, tip, 0.035f, crest, 0.35f);
        });
        b.Mark(head, V(0, 0.82f, 0.1f), V(0, 0.4f, 1f), 0.03f, 0.03f, dark, MarkShape.Triangle);
        b.Spike(head, V(0, 0.73f, 0.14f), V(0, 0.68f, 0.17f), 0.022f, Rgb(232, 196, 170), mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.765f, 0.135f), V(0.4f * s, 0.05f, 1f), 0.028f, pupil: Rgb(40, 30, 30), white: Rgb(220, 70, 60), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Spiritomb

    private static PokeBuilder Spiritomb()
    {
        var b = new PokeBuilder("Spiritomb", 0.78f, BodyPlan.Floating, V(0, 0.48f, 0)) { Coat = Fur };
        var mist = Rgb(198, 150, 200);
        var orb = Rgb(96, 168, 52);
        var core = Rgb(236, 222, 76);
        var lime = Rgb(150, 214, 60);
        var stone = Rgb(176, 176, 170);

        // The keystone it is bound to, and the swirl of a hundred and eight spirits over it, ragged at the edge
        b.Box(Body, V(0, 0.06f, 0.0f), V(0.12f, 0.06f, 0.1f), 0.04f, stone, mat: Shell);
        b.Ell(Body, V(0, 0.48f, -0.02f), V(0.27f, 0.33f, 0.13f), mist);
        float[] reach = { 0.11f, 0.07f, 0.13f, 0.08f, 0.1f, 0.06f, 0.12f, 0.09f, 0.07f, 0.12f, 0.08f, 0.1f, 0.06f };
        for (int i = 0; i < reach.Length; i++)
        {
            float a = i * MathF.Tau / reach.Length + 0.2f;
            if (MathF.Sin(a) < -0.9f) continue;
            // Each wisp leans round the way the swirl turns
            var rim = V(MathF.Cos(a) * 0.25f, 0.48f + MathF.Sin(a) * 0.31f, -0.03f);
            var outward = Vector3.Normalize(V(MathF.Cos(a) - MathF.Sin(a) * 0.8f, MathF.Sin(a) + MathF.Cos(a) * 0.8f, 0));
            b.Spike(Body, rim - outward * 0.03f, rim + outward * reach[i], 0.05f, mist, 0.35f);
        }
        b.Tube(Body, new[] { V(0, 0.16f, -0.02f), V(0.02f, 0.12f, 0.0f), V(0, 0.1f, 0.0f) }, 0.06f, 0.05f, mist);
        // A ring of green orbs with yellow hearts round a green face, smaller ones between them
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + MathF.PI / 8f, between = a + MathF.PI / 8f;
            var at = V(MathF.Cos(a) * 0.19f, 0.5f + MathF.Sin(a) * 0.24f, 0.09f);
            b.Ell(Body, at, V(0.036f, 0.036f, 0.025f), orb, mat: Shell, blend: 0.006f);
            b.Mark(Body, at + V(0, 0, 0.024f), V(0, 0, 1f), 0.018f, 0.018f, core);
            b.Ell(Body, V(MathF.Cos(between) * 0.215f, 0.5f + MathF.Sin(between) * 0.27f, 0.07f), V(0.018f, 0.018f, 0.014f), orb, mat: Shell, blend: 0.006f);
        }
        b.Mark(Body, V(0, 0.4f, 0.123f), V(0, 0, 1f), 0.085f, 0.03f, lime, MarkShape.Zigzag);
        PokeBuilder.Both(s => b.Eye(Body, V(0.06f * s, 0.53f, 0.125f), V(0.15f * s, 0, 1f), 0.036f, pupil: Rgb(40, 70, 30), white: lime, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Munchlax line

    /// <summary>Three claws in a row across <paramref name="across"/>, each a short point along <paramref name="dir"/>.</summary>
    private static void Claws(PokeBuilder b, int bone, Vector3 at, Vector3 across, Vector3 dir, float length, float r)
    {
        var d = Vector3.Normalize(dir) * length;
        for (int i = -1; i <= 1; i++)
            b.Spike(bone, at + across * i, at + across * i + d, r, Claw, mat: Shell, blend: 0.004f);
    }

    private static PokeBuilder Munchlax()
    {
        var b = new PokeBuilder("Munchlax", 0.62f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var teal = Rgb(52, 112, 124);
        var cream = Rgb(244, 230, 150);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.1f, 0.01f));
            b.Limb(leg, V(0.07f * s, 0.1f, 0.01f), V(0.08f * s, 0.04f, 0.03f), 0.05f, 0.045f, teal);
            b.Ell(leg, V(0.085f * s, 0.035f, 0.06f), V(0.05f, 0.035f, 0.055f), teal);
            b.PaintEll(leg, V(0.085f * s, 0.035f, 0.105f), V(0.045f, 0.035f, 0.025f), cream);
            Claws(b, leg, V(0.085f * s, 0.055f, 0.1f), V(0.022f, 0, 0), V(0, 0.3f, 1f), 0.02f, 0.008f);
        });
        // A round body in a shaggy coat, ragged at the hem, and a yellow belly
        b.Ell(Body, V(0, 0.22f, 0), V(0.17f, 0.18f, 0.15f), teal);
        for (int i = 0; i < 14; i++)
        {
            float a = i * MathF.Tau / 14f + 0.11f;
            b.Spike(Body, V(MathF.Sin(a) * 0.15f, 0.12f, MathF.Cos(a) * 0.13f), V(MathF.Sin(a) * 0.19f, 0.045f + 0.012f * (i % 3), MathF.Cos(a) * 0.17f), 0.03f, teal);
        }
        b.PaintEll(Body, V(0, 0.2f, 0.12f), V(0.075f, 0.075f, 0.06f), cream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.28f, 0.02f));
            b.Limb(arm, V(0.15f * s, 0.28f, 0.02f), V(0.22f * s, 0.2f, 0.06f), 0.042f, 0.036f, teal);
            b.Spike(arm, V(0.19f * s, 0.23f, 0.03f), V(0.21f * s, 0.17f, 0.02f), 0.022f, teal);
            b.Spike(arm, V(0.16f * s, 0.25f, 0.02f), V(0.17f * s, 0.19f, 0.0f), 0.02f, teal);
        });

        int head = b.Head(V(0, 0.36f, 0));
        b.Ell(head, V(0, 0.45f, 0.01f), V(0.15f, 0.12f, 0.13f), teal);
        // Two pointed ears, round eyes, and a wide grin in a pale jaw
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, 0.53f, -0.01f));
            b.Spike(ear, V(0.07f * s, 0.52f, -0.01f), V(0.11f * s, 0.67f, -0.03f), 0.05f, teal, 0.5f);
        });
        b.PaintEll(head, V(0, 0.385f, 0.09f), V(0.12f, 0.04f, 0.07f), cream);
        Gape(b, head, V(0, 0.405f, 0.12f), V(0.07f, 0.02f, 0.025f), Rgb(190, 70, 80), 0.016f);
        PokeBuilder.Both(s => b.Eye(head, V(0.06f * s, 0.47f, 0.12f), V(0.45f * s, 0.05f, 1f), 0.028f, sclera: true, pupil: Rgb(30, 30, 34)));
        return b;
    }

    private static PokeBuilder Snorlax()
    {
        var b = new PokeBuilder("Snorlax", 1f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var blue = Rgb(40, 104, 132);
        var cream = Rgb(240, 228, 198);
        var pad = Rgb(150, 116, 86);

        // It sits with its feet out in front, soles to the world
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.17f * s, 0.16f, 0.06f));
            b.Limb(leg, V(0.17f * s, 0.16f, 0.06f), V(0.22f * s, 0.12f, 0.22f), 0.1f, 0.095f, blue);
            b.Ell(leg, V(0.23f * s, 0.12f, 0.27f), V(0.1f, 0.12f, 0.07f), blue);
            b.PaintEll(leg, V(0.23f * s, 0.12f, 0.33f), V(0.09f, 0.11f, 0.035f), cream);
            b.Mark(leg, V(0.23f * s, 0.1f, 0.34f), V(0.1f * s, 0, 1f), 0.05f, 0.055f, pad);
            Claws(b, leg, V(0.23f * s, 0.21f, 0.31f), V(0.035f, 0, 0), V(0, 0.5f, 1f), 0.04f, 0.016f);
        });
        b.Ell(Body, V(0, 0.36f, -0.02f), V(0.34f, 0.34f, 0.28f), blue);
        b.PaintEll(Body, V(0, 0.32f, 0.12f), V(0.26f, 0.28f, 0.2f), cream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.28f * s, 0.48f, 0.0f));
            b.Limb(arm, V(0.28f * s, 0.48f, 0.0f), V(0.37f * s, 0.3f, 0.08f), 0.08f, 0.07f, blue);
            b.Ell(arm, V(0.38f * s, 0.27f, 0.1f), V(0.07f, 0.06f, 0.07f), blue);
            Claws(b, arm, V(0.4f * s, 0.23f, 0.12f), V(0, 0, 0.03f), V(0.3f * s, -1f, 0.2f), 0.035f, 0.014f);
        });

        int head = b.Head(V(0, 0.64f, 0.02f));
        b.Ell(head, V(0, 0.75f, 0.02f), V(0.21f, 0.15f, 0.18f), blue);
        b.PaintEll(head, V(0, 0.71f, 0.13f), V(0.16f, 0.1f, 0.1f), cream);
        // Small ears, eyes shut in a doze, a contented mouth
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.13f * s, 0.86f, 0.0f));
            b.Spike(ear, V(0.13f * s, 0.85f, -0.01f), V(0.17f * s, 0.96f, -0.02f), 0.055f, blue, 0.6f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.07f * s, 0.765f, 0.185f), V(0.35f * s, 0.05f, 1f), 0.03f, closed: true));
        b.Mark(head, V(0, 0.705f, 0.19f), V(0, -0.15f, 1f), 0.045f, 0.012f, Rgb(70, 50, 50), MarkShape.Wave);
        return b;
    }

    // ------------------------------------------------------------------ Unown

    private static PokeBuilder Unown()
    {
        // The A of the Unown: one great eye in a ring, a point on top, two legs and a loop below
        var b = new PokeBuilder("Unown", 0.55f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Fur }.Hover();
        var black = Rgb(40, 40, 46);

        b.Torus(Body, V(0, 0.42f, 0), 0.095f, 0.035f, black, V(90f, 0, 0), blend: 0.01f);
        b.Ell(Body, V(0, 0.42f, -0.012f), V(0.1f, 0.1f, 0.02f), black, blend: 0.006f);
        b.Ell(Body, V(0, 0.42f, 0.006f), V(0.1f, 0.1f, 0.018f), White, blend: 0.006f);
        b.Spike(Body, V(0, 0.5f, 0), V(0, 0.61f, -0.01f), 0.032f, black);
        b.Tube(Body, new[] { V(-0.055f, 0.34f, 0), V(-0.085f, 0.24f, 0), V(-0.115f, 0.12f, 0) }, 0.03f, 0.024f, black);
        b.Tube(Body, new[] { V(0.055f, 0.34f, 0), V(0.075f, 0.24f, 0), V(0.085f, 0.12f, 0) }, 0.03f, 0.024f, black);
        b.Torus(Body, V(0, 0.255f, 0), 0.04f, 0.017f, black, V(90f, 0, 0), blend: 0.006f);
        b.Eye(Body, V(0, 0.42f, 0.023f), V(0, 0, 1f), 0.05f, sclera: true, pupil: black);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Wooper line

    /// <summary>A feathery gill: a stalk out from the head with short branches up and down along it.</summary>
    private static void Gill(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float r, Color color)
    {
        b.Tube(bone, new[] { root, (root + tip) / 2f + V(0, 0.01f, 0), tip }, r, r * 0.75f, color);
        for (int i = 0; i < 3; i++)
        {
            var at = Vector3.Lerp(root, tip, 0.4f + 0.25f * i);
            float len = r * (3.2f - 0.5f * i), lean = (tip.X - root.X) * 0.15f;
            b.Tube(bone, new[] { at, at + V(lean, len, 0) }, r * 0.7f, r * 0.5f, color);
            b.Tube(bone, new[] { at, at + V(lean, -len * 0.9f, 0) }, r * 0.7f, r * 0.5f, color);
        }
    }

    private static PokeBuilder Wooper()
    {
        var b = new PokeBuilder("Wooper", 0.52f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        var blue = Rgb(86, 182, 236);
        var navy = Rgb(44, 92, 168);
        var lilac = Rgb(196, 166, 232);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.08f, 0.0f));
            b.Limb(leg, V(0.045f * s, 0.08f, 0.0f), V(0.05f * s, 0.035f, 0.01f), 0.03f, 0.028f, blue);
            b.Ell(leg, V(0.052f * s, 0.025f, 0.025f), V(0.035f, 0.025f, 0.045f), blue);
        });
        // A small body striped across the belly, and a broad flat tail
        b.Ell(Body, V(0, 0.14f, -0.01f), V(0.075f, 0.085f, 0.07f), blue);
        foreach (var (y, z, w) in new[] { (0.155f, 0.058f, 0.034f), (0.125f, 0.058f, 0.03f), (0.095f, 0.049f, 0.024f) })
            b.Mark(Body, V(0, y, z), V(0, (y - 0.14f) * 6f, 1f), w, 0.007f, navy, MarkShape.Bar);
        int tail = b.Tail(V(0, 0.1f, -0.07f));
        b.Limb(tail, V(0, 0.1f, -0.07f), V(0.02f, 0.1f, -0.12f), 0.035f, 0.03f, blue);
        b.Ell(tail, V(0.04f, 0.11f, -0.19f), V(0.085f, 0.024f, 0.1f), blue, V(20f, -20f, 0));

        int head = b.Head(V(0, 0.22f, 0));
        b.Ell(head, V(0, 0.3f, 0.01f), V(0.14f, 0.11f, 0.11f), blue);
        // Feathery lilac gills either side, small eyes, a mouth open in a little O
        PokeBuilder.Both(s =>
        {
            int gill = b.Ear(head, s, V(0.12f * s, 0.31f, -0.01f));
            Gill(b, gill, V(0.12f * s, 0.31f, -0.01f), V(0.27f * s, 0.35f, -0.03f), 0.013f, lilac);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.06f * s, 0.325f, 0.103f), V(0.4f * s, 0.05f, 1f), 0.016f));
        b.Mark(head, V(0, 0.27f, 0.114f), V(0, -0.2f, 1f), 0.016f, 0.02f, Rgb(196, 76, 96));
        return b;
    }

    /// <summary>A wide open mouth: a hollow carved into the face and painted inside, with no teeth.</summary>
    private static void Grin(PokeBuilder b, int bone, Vector3 at, Vector3 radii, Color inside)
    {
        b.Cut(bone, at + V(0, 0, radii.Z * 0.6f), radii, blend: 0.01f);
        b.PaintEll(bone, at, radii * 1.08f, inside);
    }

    private static PokeBuilder Quagsire()
    {
        var b = new PokeBuilder("Quagsire", 0.86f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Fur };
        var blue = Rgb(112, 192, 232);
        var navy = Rgb(40, 74, 128);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.14f, 0.0f));
            b.Limb(leg, V(0.09f * s, 0.14f, 0.0f), V(0.1f * s, 0.045f, 0.02f), 0.065f, 0.055f, blue);
            b.Ell(leg, V(0.105f * s, 0.028f, 0.06f), V(0.06f, 0.028f, 0.08f), blue);
            Digits(b, leg, V(0.105f * s, 0.022f, 0.1f), V(0, 0, 1f), V(0.6f, 0, 0), 0.05f, 0.017f, blue);
        });
        // A tall smooth body all of a piece with its head, and a thick tail finned along the top
        b.Ell(Body, V(0, 0.36f, 0), V(0.19f, 0.28f, 0.16f), blue);
        int tail = b.Tail(V(0, 0.2f, -0.12f));
        b.Tube(tail, Smooth(3, V(0, 0.2f, -0.12f), V(0.03f, 0.1f, -0.28f), V(0.08f, 0.05f, -0.4f)), 0.075f, 0.035f, blue);
        b.Tube(tail, Smooth(3, V(0, 0.285f, -0.13f), V(0.03f, 0.165f, -0.3f), V(0.08f, 0.09f, -0.41f)), 0.02f, 0.012f, navy);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, 0.46f, 0.05f));
            b.Limb(arm, V(0.16f * s, 0.46f, 0.05f), V(0.24f * s, 0.37f, 0.11f), 0.045f, 0.04f, blue);
            Digits(b, arm, V(0.245f * s, 0.36f, 0.12f), V(0.4f * s, -0.5f, 0.6f), V(0, 0.4f, 0.3f), 0.04f, 0.014f, blue);
        });

        int head = b.Head(V(0, 0.56f, 0.01f));
        b.Ell(head, V(0, 0.68f, 0.02f), V(0.17f, 0.14f, 0.15f), blue);
        // A wide, carefree grin and small eyes far apart
        Grin(b, head, V(0, 0.64f, 0.15f), V(0.11f, 0.04f, 0.05f), Rgb(196, 70, 80));
        PokeBuilder.Both(s => b.Eye(head, V(0.075f * s, 0.74f, 0.135f), V(0.45f * s, 0.15f, 1f), 0.016f));
        return b;
    }

    // ------------------------------------------------------------------ Wingull line

    private static PokeBuilder Wingull()
    {
        var b = new PokeBuilder("Wingull", 0.62f, BodyPlan.Bird, V(0, 0.3f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(84, 168, 232);
        var pale = Rgb(176, 220, 246);
        var yellow = Rgb(250, 200, 70);
        var grey = Rgb(80, 82, 92);

        b.Ell(Body, V(0, 0.3f, -0.02f), V(0.085f, 0.08f, 0.15f), White);
        int tail = b.Tail(V(0, 0.3f, -0.14f));
        b.Ell(tail, V(0, 0.31f, -0.2f), V(0.055f, 0.012f, 0.065f), White);
        b.PaintEll(tail, V(0, 0.31f, -0.255f), V(0.07f, 0.03f, 0.03f), blue);
        // Long narrow wings, a band of blue across each and pale forked tips
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.06f * s, 0.32f, 0.0f));
            // Raised at the elbow and angled down at the tip, as a gull glides
            b.Ell(w, V(0.18f * s, 0.345f, -0.01f), V(0.14f, 0.013f, 0.05f), White, V(0, 0, 14f * s));
            b.Ell(w, V(0.387f * s, 0.363f, -0.029f), V(0.075f, 0.011f, 0.04f), White, V(0, 15f * s, -12f * s));
            b.Spike(w, V(0.45f * s, 0.349f, -0.046f), V(0.51f * s, 0.342f, -0.03f), 0.012f, White);
            b.Spike(w, V(0.45f * s, 0.349f, -0.05f), V(0.5f * s, 0.342f, -0.09f), 0.012f, White);
            b.PaintEll(w, V(0.22f * s, 0.355f, -0.01f), V(0.022f, 0.035f, 0.06f), blue);
            b.PaintEll(w, V(0.45f * s, 0.35f, -0.05f), V(0.05f, 0.035f, 0.06f), pale);
        });

        int head = b.Head(V(0, 0.34f, 0.1f));
        b.Ell(head, V(0, 0.365f, 0.13f), V(0.068f, 0.064f, 0.07f), White);
        // A long yellow beak, dark at its point, and eyes narrowed against the sea wind
        b.Spike(head, V(0, 0.35f, 0.18f), V(0, 0.335f, 0.29f), 0.024f, yellow, mat: Shell);
        b.PaintEll(head, V(0, 0.338f, 0.27f), V(0.024f, 0.024f, 0.035f), grey);
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, 0.38f, 0.178f), V(0.75f * s, 0.25f, 0.63f), 0.015f, closed: true));
        return Lift(b);
    }

    private static PokeBuilder Pelipper()
    {
        var b = new PokeBuilder("Pelipper", 0.84f, BodyPlan.Bird, V(0, 0.4f, -0.08f)) { Coat = Fur }.Hover();
        var blue = Rgb(64, 150, 230);
        var yellow = Rgb(250, 212, 50);
        var throat = Rgb(170, 52, 62);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.28f, -0.06f));
            b.Limb(leg, V(0.06f * s, 0.28f, -0.06f), V(0.07f * s, 0.17f, -0.02f), 0.022f, 0.018f, blue);
            b.Ell(leg, V(0.07f * s, 0.16f, 0.03f), V(0.045f, 0.012f, 0.06f), blue);
        });
        b.Ell(Body, V(0, 0.38f, -0.1f), V(0.13f, 0.14f, 0.17f), White);
        int tail = b.Tail(V(0, 0.36f, -0.25f));
        b.Spike(tail, V(0, 0.36f, -0.25f), V(0, 0.33f, -0.36f), 0.06f, White);
        // White wings hanging like arms, tipped with blue feathers
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.11f * s, 0.46f, -0.08f));
            b.Ell(w, V(0.15f * s, 0.34f, -0.1f), V(0.045f, 0.16f, 0.11f), White, V(-15f, 0, 12f * s));
            b.PaintEll(w, V(0.19f * s, 0.21f, -0.11f), V(0.07f, 0.06f, 0.12f), blue);
            Digits(b, w, V(0.19f * s, 0.21f, -0.1f), V(0.25f * s, -1f, 0), V(0, 0, 0.5f), 0.07f, 0.02f, blue);
        });

        int head = b.Head(V(0, 0.5f, 0.0f));
        b.Ell(head, V(0, 0.58f, -0.01f), V(0.1f, 0.1f, 0.1f), White);
        b.PaintEll(head, V(0, 0.65f, -0.03f), V(0.11f, 0.06f, 0.11f), blue);
        b.Spike(head, V(0, 0.64f, -0.06f), V(0, 0.665f, -0.15f), 0.035f, blue);
        // A long flat bill over a great yellow pouch, open on the red of its throat
        b.Ell(head, V(0, 0.44f, 0.16f), V(0.12f, 0.1f, 0.16f), yellow, blend: 0.006f);
        b.PaintEll(head, V(0, 0.53f, 0.17f), V(0.09f, 0.025f, 0.13f), throat);
        b.Ell(head, V(0, 0.575f, 0.17f), V(0.075f, 0.022f, 0.15f), yellow, mat: Shell, blend: 0.006f);
        b.Spike(head, V(0, 0.57f, 0.3f), V(0, 0.54f, 0.33f), 0.02f, Rgb(240, 176, 40), mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s => b.Eye(head, V(0.075f * s, 0.6f, 0.055f), V(0.75f * s, 0.2f, 0.63f), 0.024f, sclera: true, pupil: Rgb(30, 30, 40)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Girafarig

    private static PokeBuilder Girafarig()
    {
        var b = new PokeBuilder("Girafarig", 0.9f, BodyPlan.Quadruped, V(0, 0.5f, -0.02f)) { Coat = Fur };
        var yellow = Rgb(246, 210, 64);
        var brown = Rgb(98, 60, 40);
        var hoof = Rgb(112, 112, 122);
        var pink = Rgb(240, 150, 172);
        var dark = Rgb(50, 36, 32);

        // Yellow at the front with brown spots, dark brown behind
        foreach (var (z, front, color) in new[] { (0.15f, true, yellow), (-0.17f, false, brown) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.45f, z), front);
                b.Limb(leg, V(0.06f * s, 0.45f, z), V(0.065f * s, 0.24f, z + 0.01f), 0.04f, 0.03f, color);
                b.Limb(leg, V(0.065f * s, 0.24f, z + 0.01f), V(0.065f * s, 0.05f, z + 0.02f), 0.03f, 0.026f, color);
                b.Ell(leg, V(0.065f * s, 0.03f, z + 0.03f), V(0.03f, 0.03f, 0.036f), hoof, mat: Shell);
            });
        b.Ell(Body, V(0, 0.5f, -0.01f), V(0.11f, 0.11f, 0.22f), yellow);
        b.PaintEll(Body, V(0, 0.5f, -0.2f), V(0.13f, 0.14f, 0.13f), brown);
        // A long neck crested with pink spines
        b.Tube(Body, new[] { V(0, 0.56f, 0.13f), V(0, 0.72f, 0.18f), V(0, 0.86f, 0.21f) }, 0.05f, 0.035f, yellow);
        foreach (var at in new[] { V(0, 0.81f, 0.172f), V(0, 0.75f, 0.15f), V(0, 0.69f, 0.13f), V(0, 0.62f, 0.09f), V(0, 0.6f, 0.03f), V(0, 0.6f, -0.04f) })
            b.Spike(Body, at, at + V(0, 0.035f, -0.02f), 0.014f, pink);
        PokeBuilder.Both(s =>
        {
            b.Mark(Body, V(0.099f * s, 0.53f, 0.06f), V(s, 0.15f, 0), 0.026f, 0.02f, brown);
            b.Mark(Body, V(0.083f * s, 0.47f, 0.12f), V(0.88f * s, -0.32f, 0.35f), 0.02f, 0.018f, brown);
            b.Mark(Body, V(0.04f * s, 0.68f, 0.168f), V(s, 0, 0.1f), 0.018f, 0.016f, brown);
            b.Mark(Body, V(0.036f * s, 0.79f, 0.195f), V(s, 0, 0.1f), 0.014f, 0.013f, brown);
        });

        int head = b.Head(V(0, 0.86f, 0.21f));
        b.Ell(head, V(0, 0.92f, 0.24f), V(0.062f, 0.06f, 0.088f), yellow);
        b.Ell(head, V(0, 0.9f, 0.32f), V(0.045f, 0.042f, 0.048f), pink);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, new[] { V(0.022f * s, 0.96f, 0.22f), V(0.028f * s, 1.03f, 0.215f) }, 0.009f, 0.008f, Rgb(236, 226, 214));
            b.Ell(head, V(0.028f * s, 1.04f, 0.215f), V(0.016f, 0.016f, 0.016f), White);
            int ear = b.Ear(head, s, V(0.045f * s, 0.95f, 0.21f));
            b.Spike(ear, V(0.045f * s, 0.95f, 0.21f), V(0.11f * s, 0.975f, 0.19f), 0.022f, yellow, 0.5f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, 0.945f, 0.286f), V(0.8f * s, 0.4f, 0.45f), 0.015f));
        // The head on its tail, with a mind of its own: yellow eyes and a row of teeth, looking back
        int tail = b.Tail(V(0, 0.55f, -0.21f));
        b.Tube(tail, new[] { V(0, 0.55f, -0.21f), V(0, 0.63f, -0.27f), V(0, 0.69f, -0.26f) }, 0.025f, 0.02f, brown);
        b.Ell(tail, V(0, 0.72f, -0.26f), V(0.045f, 0.042f, 0.048f), dark);
        PokeBuilder.Both(s => b.Mark(tail, V(0.024f * s, 0.735f, -0.297f), V(0.45f * s, 0.2f, -1f), 0.013f, 0.013f, Rgb(250, 214, 60), MarkShape.Ring));
        b.Mark(tail, V(0, 0.705f, -0.303f), V(0, -0.2f, -1f), 0.022f, 0.007f, White, MarkShape.Zigzag);
        return b;
    }

    // ------------------------------------------------------------------ Hippopotas line

    private static PokeBuilder Hippopotas()
    {
        var b = new PokeBuilder("Hippopotas", 0.72f, BodyPlan.Quadruped, V(0, 0.2f, -0.04f)) { Coat = Fur };
        var tan = Rgb(232, 206, 146);
        var patch = Rgb(196, 156, 98);
        var brown = Rgb(150, 100, 62);
        var dark = Rgb(56, 38, 30);

        foreach (var (z, front) in new[] { (0.06f, true), (-0.16f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.09f * s, 0.12f, z), front);
                b.Limb(leg, V(0.09f * s, 0.12f, z), V(0.095f * s, 0.04f, z + 0.01f), 0.045f, 0.04f, tan);
                b.Ell(leg, V(0.095f * s, 0.03f, z + 0.02f), V(0.045f, 0.03f, 0.05f), tan);
            });
        // A barrel of a body in sandy camouflage
        b.Ell(Body, V(0, 0.2f, -0.06f), V(0.14f, 0.13f, 0.18f), tan);
        foreach (var (at, r) in new[] { (V(0.06f, 0.3f, -0.1f), V(0.07f, 0.04f, 0.06f)), (V(-0.07f, 0.28f, -0.02f), V(0.06f, 0.04f, 0.05f)),
                     (V(-0.03f, 0.27f, -0.18f), V(0.06f, 0.05f, 0.05f)), (V(0.11f, 0.22f, -0.15f), V(0.04f, 0.05f, 0.05f)) })
            b.PaintEll(Body, at, r, patch);
        int tail = b.Tail(V(0, 0.2f, -0.23f));
        b.Spike(tail, V(0, 0.2f, -0.23f), V(0, 0.18f, -0.29f), 0.025f, tan);

        int head = b.Head(V(0, 0.22f, 0.08f));
        b.Ell(head, V(0, 0.22f, 0.17f), V(0.13f, 0.12f, 0.12f), tan);
        // A great round snout with a dark hole in front, and eyes peering out of a brown cap on top
        b.Cut(head, V(0, 0.2f, 0.3f), V(0.035f, 0.035f, 0.04f), blend: 0.008f);
        b.PaintEll(head, V(0, 0.2f, 0.28f), V(0.042f, 0.042f, 0.04f), dark);
        b.Torus(head, V(0, 0.2f, 0.283f), 0.046f, 0.014f, brown, V(90f, 0, 0), blend: 0.008f);
        b.Ell(head, V(0, 0.33f, 0.12f), V(0.075f, 0.05f, 0.065f), brown);
        PokeBuilder.Both(s => b.Eye(head, V(0.03f * s, 0.35f, 0.172f), V(0.34f * s, 0.51f, 0.8f), 0.02f, sclera: true, pupil: Rgb(30, 22, 20)));
        return b;
    }

    private static PokeBuilder Hippowdon()
    {
        var b = new PokeBuilder("Hippowdon", 1f, BodyPlan.Quadruped, V(0, 0.32f, -0.06f)) { Coat = Fur };
        var yellow = Rgb(214, 176, 74);
        var grey = Rgb(92, 92, 98);
        var mouth = Rgb(168, 52, 62);
        var dark = Rgb(40, 40, 44);

        foreach (var (z, front) in new[] { (0.12f, true), (-0.24f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.17f * s, 0.22f, z), front);
                b.Limb(leg, V(0.17f * s, 0.22f, z), V(0.19f * s, 0.07f, z + 0.01f), 0.08f, 0.075f, yellow);
                b.Ell(leg, V(0.19f * s, 0.045f, z + 0.03f), V(0.085f, 0.045f, 0.09f), grey);
                Claws(b, leg, V(0.19f * s, 0.035f, z + 0.11f), V(0.035f, 0, 0), V(0, -0.2f, 1f), 0.035f, 0.014f);
            });
        // A huge body plated in grey along the back, with vents that blow out sand
        b.Ell(Body, V(0, 0.32f, -0.06f), V(0.26f, 0.2f, 0.33f), yellow);
        b.PaintEll(Body, V(0, 0.5f, -0.12f), V(0.2f, 0.08f, 0.26f), grey);
        foreach (var (x, z) in new[] { (0.12f, -0.02f), (-0.12f, -0.02f), (0.12f, -0.2f), (-0.12f, -0.2f) })
        {
            float y = 0.32f + 0.2f * MathF.Sqrt(1f - x * x / (0.26f * 0.26f) - (z + 0.06f) * (z + 0.06f) / (0.33f * 0.33f));
            var n = Vector3.Normalize(V(x / (0.26f * 0.26f), (y - 0.32f) / (0.2f * 0.2f), (z + 0.06f) / (0.33f * 0.33f)));
            var at = V(x, y, z);
            b.Torus(Body, at, 0.035f, 0.016f, grey, Euler(n), blend: 0.01f);
            b.Mark(Body, at + n * 0.004f, n, 0.03f, 0.03f, dark);
        }
        int tail = b.Tail(V(0, 0.3f, -0.38f));
        b.Spike(tail, V(0, 0.3f, -0.38f), V(0, 0.26f, -0.46f), 0.04f, yellow);

        // Jaws thrown wide open: a grey head over a yellow jaw, red inside and ringed with teeth
        int head = b.Head(V(0, 0.4f, 0.22f));
        b.Ell(head, V(0, 0.52f, 0.3f), V(0.19f, 0.12f, 0.17f), grey, V(-25f, 0, 0));
        b.Ell(head, V(0, 0.4f, 0.3f), V(0.15f, 0.1f, 0.12f), mouth, blend: 0.01f);
        b.PaintEll(head, V(0, 0.45f, 0.4f), V(0.15f, 0.04f, 0.1f), mouth);
        int jaw = b.Jaw(head, V(0, 0.36f, 0.2f));
        b.Ell(jaw, V(0, 0.28f, 0.36f), V(0.18f, 0.08f, 0.17f), yellow, V(10f, 0, 0));
        b.PaintEll(jaw, V(0, 0.35f, 0.39f), V(0.15f, 0.03f, 0.13f), mouth);
        PokeBuilder.Both(s =>
        {
            foreach (float x in new[] { 0.12f, 0.06f })
                b.Spike(head, V(x * s, 0.48f, 0.42f - x * 0.1f), V(x * s, 0.43f, 0.43f - x * 0.1f), 0.017f, White, mat: Shell, blend: 0.004f);
            b.Spike(jaw, V(0.11f * s, 0.31f, 0.45f), V(0.11f * s, 0.38f, 0.46f), 0.022f, White, mat: Shell, blend: 0.004f);
            b.Spike(jaw, V(0.05f * s, 0.32f, 0.48f), V(0.05f * s, 0.36f, 0.49f), 0.014f, White, mat: Shell, blend: 0.004f);
            b.Eye(head, V(0.17f * s, 0.53f, 0.24f), V(0.9f * s, 0.2f, 0.3f), 0.02f, Rgb(220, 40, 50));
        });
        return b;
    }
}
