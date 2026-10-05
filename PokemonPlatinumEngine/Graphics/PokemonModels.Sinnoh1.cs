using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Plan 03 · D6: hand-built models for the first batch of Platinum's Sinnoh Pokédex, Kricketot (15) to Lopunny (68),
// in the order the story meets them. Every other species of the batch's range was hand-built before.
internal static partial class PokemonModels
{
    /// <summary>Points round an arc in the plane through <paramref name="center"/> spanned by <paramref name="u"/> and <paramref name="v"/>, from angle a0 to a1 (radians).</summary>
    private static Vector3[] Arc(Vector3 center, Vector3 u, Vector3 v, float radius, float a0, float a1, int steps)
    {
        var points = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float a = a0 + (a1 - a0) * i / steps;
            points[i] = center + (u * MathF.Cos(a) + v * MathF.Sin(a)) * radius;
        }
        return points;
    }

    /// <summary>A spiral in the plane of <paramref name="u"/> and <paramref name="v"/>, its radius shrinking from r0 to r1 (curled antennae, mustaches).</summary>
    private static Vector3[] Spiral(Vector3 center, Vector3 u, Vector3 v, float r0, float r1, float a0, float a1, int steps)
    {
        var points = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps, a = a0 + (a1 - a0) * t;
            points[i] = center + (u * MathF.Cos(a) + v * MathF.Sin(a)) * (r0 + (r1 - r0) * t);
        }
        return points;
    }

    /// <summary>Raises a hovering model so its lowest point floats above its platform as the generated fliers' do.</summary>
    private static PokeBuilder Lift(PokeBuilder b)
    {
        b.Ground(PokemonSculptor.HoverGap);
        return b;
    }

    // ------------------------------------------------------------------ Kricketot line

    private static PokeBuilder Kricketot()
    {
        var b = new PokeBuilder("Kricketot", 0.5f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Shell };
        var red = Rgb(230, 86, 46);
        var nose = Rgb(172, 72, 46);
        var cream = Rgb(250, 238, 176);
        var pleat = Rgb(226, 206, 140);
        var gray = Rgb(128, 128, 138);
        var band = Rgb(122, 52, 34);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.12f, 0));
            b.Ell(leg, V(0.1f * s, 0.045f, 0.02f), V(0.07f, 0.05f, 0.08f), gray);
        });
        // A tall egg of a body, a dark band round its foot and a pleated cream front like a coat's
        b.Ell(Body, V(0, 0.33f, 0), V(0.22f, 0.28f, 0.2f), red);
        b.PaintEll(Body, V(0, 0.08f, 0), V(0.25f, 0.06f, 0.23f), band);
        b.PaintEll(Body, V(0, 0.28f, 0.16f), V(0.11f, 0.18f, 0.08f), cream);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.038f * s, 0.27f, 0.19f), V(0.007f, 0.15f, 0.04f), pleat, soft: 0.008f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.3f, 0.08f));
            b.Limb(arm, V(0.17f * s, 0.3f, 0.08f), V(0.21f * s, 0.24f, 0.12f), 0.04f, 0.04f, gray);
            b.Ell(arm, V(0.215f * s, 0.23f, 0.125f), V(0.05f, 0.05f, 0.05f), gray);
        });

        int head = b.Head(V(0, 0.46f, 0));
        // The red face sits inside a big cream collar, high behind the head and flared at the sides
        b.Ell(head, V(0, 0.52f, 0.06f), V(0.17f, 0.15f, 0.14f), red);
        b.Torus(head, V(0, 0.53f, 0.0f), 0.2f, 0.06f, cream, V(68f, 0, 0), 1.15f, mat: Shell, blend: 0.02f);
        b.Ell(head, V(0, 0.57f, -0.07f), V(0.2f, 0.16f, 0.11f), cream);
        b.Ell(head, V(0, 0.655f, 0.07f), V(0.06f, 0.035f, 0.05f), nose);
        b.Ell(head, V(0, 0.465f, 0.19f), V(0.068f, 0.052f, 0.05f), nose);
        PokeBuilder.Both(s => b.Eye(head, V(0.08f * s, 0.55f, 0.18f), V(0.45f * s, 0.05f, 1f), 0.042f));
        // Thick antennae that curl outward like a ram's horns
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.66f, 0.02f));
            var curl = Spiral(V(0.16f * s, 0.86f, 0.0f), V(-s, 0, 0), V(0, 1, 0), 0.09f, 0.035f, 0.35f, 0.35f + 1.7f * MathF.PI, 16);
            b.Tube(ear, new[] { V(0.05f * s, 0.64f, 0.02f) }.Concat(curl).ToArray(), 0.036f, 0.022f, gray);
        });
        return b;
    }

    private static PokeBuilder Kricketune()
    {
        var b = new PokeBuilder("Kricketune", 0.8f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Shell };
        var red = Rgb(226, 80, 44);
        var cream = Rgb(244, 226, 170);
        var wing = Rgb(118, 118, 132);
        var gray = Rgb(132, 132, 144);
        var band = Rgb(128, 56, 34);
        var nose = Rgb(164, 80, 50);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.2f, 0));
            b.Limb(leg, V(0.06f * s, 0.2f, 0), V(0.07f * s, 0.05f, 0.01f), 0.026f, 0.02f, red);
            b.Ell(leg, V(0.075f * s, 0.03f, 0.03f), V(0.04f, 0.03f, 0.05f), red);
        });
        b.Ell(Body, V(0, 0.38f, 0), V(0.135f, 0.2f, 0.115f), red);
        b.PaintEll(Body, V(0, 0.2f, 0), V(0.12f, 0.05f, 0.11f), cream);
        b.PaintEll(Body, V(0, 0.25f, 0), V(0.125f, 0.022f, 0.11f), band);
        b.PaintEll(Body, V(0, 0.43f, 0.09f), V(0.028f, 0.15f, 0.04f), cream);
        // Small grey forelegs folded against the sides
        PokeBuilder.Both(s => b.Ell(Body, V(0.105f * s, 0.35f, 0.04f), V(0.025f, 0.07f, 0.035f), gray, V(0, 0, -8f * s)));
        // Wings that hang behind like the tails of a coat, flaring and pointed at the bottom
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.06f * s, 0.5f, -0.06f));
            b.Ell(w, V(0.15f * s, 0.31f, -0.07f), V(0.07f, 0.21f, 0.03f), wing, V(0, 15f * s, 14f * s));
            b.Spike(w, V(0.19f * s, 0.17f, -0.07f), V(0.25f * s, 0.06f, -0.08f), 0.05f, wing, 0.45f);
        });
        // Arms like the bows of a violin: one raised, one lowered, a round joint at each shoulder
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.52f, 0.02f));
            var knob = s > 0 ? V(0.14f, 0.52f, 0.04f) : V(-0.13f, 0.6f, 0.03f);
            b.Limb(arm, V(0.1f * s, 0.52f, 0.02f), knob, 0.024f, 0.022f, red);
            b.Ell(arm, knob, V(0.03f, 0.03f, 0.03f), red);
            var bow = s > 0
                ? new[] { knob, V(0.19f, 0.44f, 0.06f), V(0.23f, 0.32f, 0.07f), V(0.24f, 0.2f, 0.07f), V(0.23f, 0.1f, 0.06f) }
                : new[] { knob, V(-0.17f, 0.72f, 0.04f), V(-0.2f, 0.84f, 0.04f), V(-0.21f, 0.96f, 0.03f), V(-0.2f, 1.06f, 0.02f) };
            b.Tube(arm, bow, 0.018f, 0.008f, red);
        });

        int head = b.Head(V(0, 0.58f, 0));
        b.Ell(head, V(0, 0.72f, 0.01f), V(0.105f, 0.13f, 0.1f), red);
        b.Spike(head, V(0, 0.8f, 0.0f), V(0, 0.9f, -0.01f), 0.065f, red);
        PokeBuilder.Both(s => b.Eye(head, V(0.066f * s, 0.765f, 0.07f), V(0.75f * s, 0.1f, 0.65f), 0.044f));
        b.Ell(head, V(0, 0.675f, 0.1f), V(0.034f, 0.028f, 0.028f), nose);
        // The grand mustache, its ends rolled into curls beside the chest
        PokeBuilder.Both(s =>
        {
            var curl = Spiral(V(0.15f * s, 0.56f, 0.09f), V(0, 1, 0), V(s, 0, 0), 0.075f, 0.03f, -0.9f, 1.5f * MathF.PI, 16);
            b.Tube(head, new[] { V(0.01f * s, 0.655f, 0.11f), V(0.06f * s, 0.645f, 0.11f) }.Concat(curl).ToArray(), 0.036f, 0.024f, gray);
        });
        // Long antennae angled out, a knob at each tip
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.02f * s, 0.84f, 0));
            b.Tube(ear, new[] { V(0.02f * s, 0.84f, 0), V(0.06f * s, 0.95f, -0.01f), V(0.11f * s, 1.05f, -0.02f), V(0.16f * s, 1.12f, -0.02f) }, 0.016f, 0.012f, red);
            b.Ell(ear, V(0.165f * s, 1.13f, -0.02f), V(0.03f, 0.03f, 0.03f), red);
        });
        return b;
    }

    // ------------------------------------------------------------------ Abra line

    /// <summary>A silver spoon held at <paramref name="grip"/>, its bowl at <paramref name="bowl"/>, the handle running into the bowl.</summary>
    private static void Spoon(PokeBuilder b, int bone, Vector3 grip, Vector3 bowl, float size)
    {
        var silver = Rgb(214, 216, 226);
        var along = Vector3.Normalize(bowl - grip);
        b.Limb(bone, grip - along * size * 0.6f, bowl - along * size * 0.3f, size * 0.12f, size * 0.1f, silver, Metal, 0.004f);
        b.Ell(bone, bowl, V(size * 0.42f, size * 0.42f, size * 0.17f), silver, Euler(along), Metal, 0.006f);
    }

    /// <summary>The turn (degrees, as <see cref="PokeBuilder.Ell"/> takes it) that stands an ellipsoid's long axis along <paramref name="dir"/>.</summary>
    private static Vector3 Euler(Vector3 dir)
    {
        var d = Vector3.Normalize(dir);
        // The roll about z tips +y over to x, then the pitch about x carries what is left of it toward z
        float roll = -MathF.Asin(Math.Clamp(d.X, -1f, 1f)) * 180f / MathF.PI;
        float pitch = MathF.Atan2(d.Z, d.Y) * 180f / MathF.PI;
        return V(pitch, 0, roll);
    }

    /// <summary>An oval petal or leaf from <paramref name="from"/> to <paramref name="to"/>, <paramref name="width"/> across and a third as thick.</summary>
    private static void Petal(PokeBuilder b, int bone, Vector3 from, Vector3 to, float width, Color color, SurfaceMaterial? mat = null, float blend = 0.012f)
    {
        float length = Vector3.Distance(from, to);
        b.Ell(bone, (from + to) / 2f, V(width, length / 2f, width * 0.32f), color, Euler(to - from), mat, blend);
    }

    /// <summary>Three fingers or toes spread from <paramref name="at"/> along <paramref name="dir"/>.</summary>
    private static void Digits(PokeBuilder b, int bone, Vector3 at, Vector3 dir, Vector3 spread, float length, float radius, Color color, SurfaceMaterial? mat = null)
    {
        var d = Vector3.Normalize(dir);
        for (int i = -1; i <= 1; i++)
        {
            var tip = at + (d + spread * i) * length;
            b.Limb(bone, at, tip, radius, radius * 0.7f, color, mat, 0.008f);
        }
    }

    private static PokeBuilder Abra()
    {
        var b = new PokeBuilder("Abra", 0.68f, BodyPlan.Biped, V(0, 0.44f, 0)) { Coat = Fur }.Hover();
        var yellow = Rgb(240, 196, 52);
        var brown = Rgb(146, 98, 74);

        // Sitting cross-legged in the air, hands on its knees
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.32f, 0.02f));
            b.Limb(leg, V(0.08f * s, 0.32f, 0.02f), V(0.2f * s, 0.26f, 0.15f), 0.075f, 0.06f, yellow);
            b.Limb(leg, V(0.2f * s, 0.26f, 0.15f), V(-0.04f * s, 0.21f, 0.2f), 0.055f, 0.045f, yellow);
            b.Ell(leg, V(-0.08f * s, 0.21f, 0.21f), V(0.05f, 0.04f, 0.07f), yellow, V(0, 30f * s, 0));
            Digits(b, leg, V(-0.1f * s, 0.21f, 0.25f), V(-0.2f * s, 0, 1f), V(0.5f * s, 0, 0), 0.05f, 0.016f, yellow);
        });
        b.Ell(Body, V(0, 0.44f, 0), V(0.15f, 0.17f, 0.13f), yellow);
        // The brown plates over its chest and shoulders
        b.Ell(Body, V(0, 0.52f, 0.0f), V(0.17f, 0.1f, 0.14f), brown);
        b.PaintEll(Body, V(0, 0.47f, 0.08f), V(0.11f, 0.07f, 0.08f), brown);
        int tail = b.Tail(V(0, 0.32f, -0.1f));
        var tailPath = new[] { V(0, 0.3f, -0.12f), V(0.12f, 0.24f, -0.26f), V(0.28f, 0.27f, -0.32f), V(0.38f, 0.4f, -0.28f), V(0.4f, 0.56f, -0.2f), V(0.36f, 0.66f, -0.14f) };
        b.Tube(tail, tailPath, 0.07f, 0.035f, yellow, blend: 0.02f);
        b.PaintEll(tail, V(0.4f, 0.5f, -0.24f), V(0.08f, 0.035f, 0.08f), brown, V(0, 0, -20f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.52f, 0.0f));
            b.Ell(arm, V(0.16f * s, 0.53f, 0.0f), V(0.08f, 0.07f, 0.08f), brown);
            b.Limb(arm, V(0.17f * s, 0.5f, 0.02f), V(0.22f * s, 0.36f, 0.1f), 0.04f, 0.033f, yellow);
            b.Limb(arm, V(0.22f * s, 0.36f, 0.1f), V(0.2f * s, 0.3f, 0.17f), 0.033f, 0.03f, yellow);
            Digits(b, arm, V(0.2f * s, 0.3f, 0.18f), V(0, -0.5f, 1f), V(0.45f * s, 0, 0), 0.045f, 0.014f, yellow);
        });

        int head = b.Head(V(0, 0.6f, 0));
        b.Ell(head, V(0, 0.71f, 0.01f), V(0.15f, 0.13f, 0.13f), yellow);
        b.Ell(head, V(0, 0.665f, 0.12f), V(0.07f, 0.05f, 0.065f), yellow);
        b.Ell(head, V(0, 0.678f, 0.18f), V(0.013f, 0.01f, 0.01f), brown, blend: 0.01f);
        // Big pointed ears, and eyes always shut: it sleeps eighteen hours a day
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.09f * s, 0.8f, -0.01f));
            b.Spike(ear, V(0.09f * s, 0.78f, -0.01f), V(0.17f * s, 0.98f, -0.04f), 0.07f, yellow, 0.42f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.065f * s, 0.73f, 0.12f), V(0.45f * s, 0.1f, 1f), 0.03f, closed: true));
        return Lift(b);
    }

    private static PokeBuilder Kadabra()
    {
        var b = new PokeBuilder("Kadabra", 0.86f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Fur };
        var yellow = Rgb(236, 190, 48);
        var brown = Rgb(146, 98, 74);
        var red = Rgb(214, 54, 50);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.46f, 0));
            b.Limb(leg, V(0.1f * s, 0.46f, 0), V(0.15f * s, 0.26f, 0.07f), 0.075f, 0.055f, yellow);
            b.Limb(leg, V(0.15f * s, 0.26f, 0.07f), V(0.14f * s, 0.06f, -0.02f), 0.05f, 0.035f, yellow);
            b.Ell(leg, V(0.14f * s, 0.035f, 0.04f), V(0.05f, 0.032f, 0.085f), yellow);
            Digits(b, leg, V(0.14f * s, 0.03f, 0.1f), V(0, 0, 1f), V(0.5f, 0, 0), 0.05f, 0.016f, Claw, Shell);
        });
        b.Ell(Body, V(0, 0.6f, 0), V(0.16f, 0.21f, 0.13f), yellow);
        b.PaintEll(Body, V(0, 0.62f, 0.04f), V(0.15f, 0.16f, 0.12f), brown);
        // The three red waves on its belly
        for (int i = 0; i < 3; i++)
            b.Mark(Body, V(0, 0.57f + i * 0.045f, 0.126f), V(0, 0.15f, 1f), 0.045f, 0.016f, red, MarkShape.Wave);
        // A thick tail like a fox's, with a brown band
        int tail = b.Tail(V(0, 0.46f, -0.1f));
        b.Tube(tail, new[] { V(0, 0.46f, -0.12f), V(0.05f, 0.34f, -0.24f), V(0.14f, 0.24f, -0.3f), V(0.25f, 0.2f, -0.28f) }, 0.1f, 0.1f, yellow, blend: 0.03f);
        b.Ell(tail, V(0.3f, 0.21f, -0.26f), V(0.11f, 0.1f, 0.1f), yellow);
        b.PaintEll(tail, V(0.1f, 0.29f, -0.27f), V(0.12f, 0.12f, 0.035f), brown, V(0, 40f, 40f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, 0.74f, 0.0f));
            b.Ell(arm, V(0.17f * s, 0.75f, 0.0f), V(0.075f, 0.07f, 0.075f), brown);
            if (s > 0)
            {
                // The right hand holds its spoon up
                b.Limb(arm, V(0.17f * s, 0.72f, 0.02f), V(0.27f, 0.62f, 0.08f), 0.04f, 0.034f, yellow);
                b.Limb(arm, V(0.27f, 0.62f, 0.08f), V(0.3f, 0.8f, 0.12f), 0.034f, 0.03f, yellow);
                b.Ell(arm, V(0.3f, 0.83f, 0.12f), V(0.04f, 0.04f, 0.04f), yellow);
                Spoon(b, arm, V(0.3f, 0.84f, 0.13f), V(0.35f, 1.08f, 0.15f), 0.15f);
            }
            else
            {
                b.Limb(arm, V(0.17f * s, 0.72f, 0.02f), V(-0.25f, 0.56f, 0.06f), 0.04f, 0.034f, yellow);
                b.Limb(arm, V(-0.25f, 0.56f, 0.06f), V(-0.27f, 0.42f, 0.12f), 0.034f, 0.03f, yellow);
                Digits(b, arm, V(-0.27f, 0.4f, 0.13f), V(-0.1f, -1f, 0.5f), V(0.5f, 0, 0), 0.05f, 0.014f, yellow);
            }
        });

        int head = b.Head(V(0, 0.8f, 0));
        float hy = 0.92f;
        b.Ell(head, V(0, hy, 0.01f), V(0.13f, 0.12f, 0.12f), yellow);
        b.Ell(head, V(0, hy - 0.05f, 0.12f), V(0.065f, 0.048f, 0.07f), yellow);
        b.Ell(head, V(0, hy - 0.035f, 0.185f), V(0.016f, 0.012f, 0.012f), brown, blend: 0.01f);
        b.Mark(head, V(0, hy + 0.06f, 0.105f), V(0, 0.5f, 1f), 0.034f, 0.034f, red, MarkShape.Star5);
        // Long whiskers drooping from the snout
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0.045f * s, hy - 0.06f, 0.15f), V(0.1f * s, hy - 0.065f, 0.18f), V(0.16f * s, hy - 0.1f, 0.2f), V(0.2f * s, hy - 0.17f, 0.2f), V(0.21f * s, hy - 0.24f, 0.19f) },
            0.013f, 0.006f, Rgb(214, 164, 52)));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, hy + 0.08f, -0.02f));
            b.Spike(ear, V(0.07f * s, hy + 0.06f, -0.02f), V(0.15f * s, hy + 0.3f, -0.07f), 0.065f, yellow, 0.42f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.06f * s, hy + 0.01f, 0.105f), V(0.5f * s, 0.05f, 1f), 0.026f, red));
        return b;
    }

    private static PokeBuilder Alakazam()
    {
        var b = new PokeBuilder("Alakazam", 0.9f, BodyPlan.Biped, V(0, 0.66f, 0)) { Coat = Fur };
        var yellow = Rgb(236, 190, 48);
        var brown = Rgb(146, 98, 74);
        var moustache = Rgb(222, 172, 50);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.52f, 0));
            b.Limb(leg, V(0.09f * s, 0.52f, 0), V(0.14f * s, 0.3f, 0.05f), 0.06f, 0.045f, yellow);
            b.Ell(leg, V(0.14f * s, 0.3f, 0.05f), V(0.055f, 0.05f, 0.055f), brown);
            b.Limb(leg, V(0.14f * s, 0.3f, 0.05f), V(0.14f * s, 0.07f, -0.01f), 0.04f, 0.03f, yellow);
            b.Ell(leg, V(0.14f * s, 0.035f, 0.04f), V(0.05f, 0.032f, 0.085f), yellow);
            Digits(b, leg, V(0.14f * s, 0.03f, 0.1f), V(0, 0, 1f), V(0.5f, 0, 0), 0.05f, 0.015f, Claw, Shell);
        });
        b.Ell(Body, V(0, 0.68f, 0), V(0.14f, 0.18f, 0.11f), yellow);
        b.PaintEll(Body, V(0, 0.72f, 0.03f), V(0.15f, 0.14f, 0.11f), brown);
        // Arms out to the sides, a spoon in each hand
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.8f, 0.0f));
            b.Ell(arm, V(0.17f * s, 0.81f, 0.0f), V(0.08f, 0.065f, 0.075f), brown, V(0, 0, -20f * s));
            b.Limb(arm, V(0.18f * s, 0.78f, 0.02f), V(0.3f * s, 0.66f, 0.06f), 0.035f, 0.03f, yellow);
            b.Limb(arm, V(0.3f * s, 0.66f, 0.06f), V(0.42f * s, 0.62f, 0.12f), 0.03f, 0.026f, yellow);
            b.Ell(arm, V(0.44f * s, 0.62f, 0.13f), V(0.035f, 0.035f, 0.035f), yellow);
            Spoon(b, arm, V(0.45f * s, 0.62f, 0.14f), V(0.68f * s, 0.68f, 0.2f), 0.14f);
        });

        int head = b.Head(V(0, 0.86f, 0));
        float hy = 0.98f;
        b.Ell(head, V(0, hy, 0.0f), V(0.14f, 0.135f, 0.13f), yellow);
        b.Ell(head, V(0, hy - 0.05f, 0.11f), V(0.065f, 0.055f, 0.08f), yellow);
        b.Ell(head, V(0, hy - 0.03f, 0.188f), V(0.015f, 0.012f, 0.012f), brown, blend: 0.01f);
        // The long moustache that hangs to its chest
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0.03f * s, hy - 0.075f, 0.16f), V(0.08f * s, hy - 0.1f, 0.18f), V(0.11f * s, hy - 0.18f, 0.18f), V(0.12f * s, hy - 0.3f, 0.16f), V(0.11f * s, hy - 0.42f, 0.14f) },
            0.024f, 0.01f, moustache));
        // Ears like horns, swept up and out
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, hy + 0.08f, -0.02f));
            b.Spike(ear, V(0.08f * s, hy + 0.07f, -0.03f), V(0.24f * s, hy + 0.32f, -0.1f), 0.065f, yellow, 0.5f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.06f * s, hy + 0.015f, 0.125f), V(0.5f * s, 0.05f, 1f), 0.027f));
        return b;
    }

    // ------------------------------------------------------------------ Magikarp line

    /// <summary>
    /// A thin fin, flattened across <paramref name="flat"/>: an outer lobe in <paramref name="rim"/> with a thicker,
    /// smaller one inside in <paramref name="fill"/>, which shows through on both faces and leaves the rim round it.
    /// </summary>
    private static void RimmedFin(PokeBuilder b, int bone, Vector3 center, Vector3 radii, Vector3 rotation, Color fill, Color rim, float border = 0.022f)
    {
        b.Ell(bone, center, radii, rim, rotation, Scales, 0.008f);
        var inner = radii - V(border, border, border);
        // Only the thin axis grows, so the inside pokes through the faces
        if (radii.X <= radii.Y && radii.X <= radii.Z) inner.X = radii.X + 0.006f;
        else if (radii.Z <= radii.Y) inner.Z = radii.Z + 0.006f;
        else inner.Y = radii.Y + 0.006f;
        b.Ell(bone, center, inner, fill, rotation, Scales, 0.004f);
    }

    private static PokeBuilder Magikarp()
    {
        var b = new PokeBuilder("Magikarp", 0.72f, BodyPlan.Fish, V(0, 0.45f, 0)) { Coat = Scales }.Hover();
        var orange = Rgb(234, 92, 46);
        var white = Rgb(244, 240, 236);
        var yellow = Rgb(250, 222, 84);
        var lips = Rgb(240, 150, 168);

        // A deep body, flat from side to side
        b.Ell(Body, V(0, 0.45f, -0.02f), V(0.17f, 0.29f, 0.34f), orange);
        // The yellow crown of a dorsal fin and the little fins underneath
        int dorsal = b.Part("dorsal", Body, V(0, 0.72f, -0.02f), PokeRole.Fin);
        for (int i = 0; i < 4; i++)
        {
            float z = 0.08f - i * 0.09f;
            b.Spike(dorsal, V(0, 0.68f - i * 0.01f, z), V(0, 0.88f - i * 0.03f, z - 0.07f), 0.05f, yellow, 0.4f);
        }
        PokeBuilder.Both(s => b.Spike(Body, V(0.06f * s, 0.2f, -0.02f), V(0.1f * s, 0.08f, -0.08f), 0.045f, yellow, 0.4f));
        // The tail: two white lobes rimmed in orange
        int tail = b.Tail(V(0, 0.45f, -0.3f));
        b.Limb(tail, V(0, 0.45f, -0.3f), V(0, 0.45f, -0.42f), 0.1f, 0.06f, orange);
        int fin = b.Part("tailFin", tail, V(0, 0.45f, -0.42f), PokeRole.Fin);
        RimmedFin(b, fin, V(0, 0.58f, -0.55f), V(0.02f, 0.17f, 0.09f), V(-38f, 0, 0), white, orange);
        RimmedFin(b, fin, V(0, 0.32f, -0.55f), V(0.02f, 0.17f, 0.09f), V(38f, 0, 0), white, orange);
        // White fins on the sides, swept back
        PokeBuilder.Both(s =>
        {
            int side = b.Part(s < 0 ? "finL" : "finR", Body, V(0.15f * s, 0.42f, 0.08f), PokeRole.Fin, s, s);
            RimmedFin(b, side, V(0.18f * s, 0.4f, -0.03f), V(0.018f, 0.1f, 0.15f), V(15f, 12f * s, 0), white, Rgb(210, 206, 204), 0.012f);
        });

        int head = b.Head(V(0, 0.45f, 0.18f));
        b.Ell(head, V(0, 0.47f, 0.2f), V(0.16f, 0.24f, 0.17f), orange);
        // Pink lips gaping in an O, and the long whiskers that droop from them
        b.Torus(head, V(0, 0.45f, 0.36f), 0.05f, 0.036f, lips, V(90f, 0, 0), mat: Shell, blend: 0.012f);
        b.Ell(head, V(0, 0.45f, 0.35f), V(0.045f, 0.045f, 0.02f), Rgb(150, 50, 70), blend: 0.006f);
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0.07f * s, 0.43f, 0.33f), V(0.1f * s, 0.36f, 0.32f), V(0.12f * s, 0.24f, 0.28f), V(0.11f * s, 0.12f, 0.24f), V(0.08f * s, 0.04f, 0.22f) },
            0.022f, 0.014f, yellow));
        PokeBuilder.Both(s => b.Eye(head, V(0.13f * s, 0.56f, 0.24f), V(1f * s, 0.15f, 0.55f), 0.055f, sclera: true));
        return Lift(b);
    }

    private static PokeBuilder Gyarados()
    {
        var b = new PokeBuilder("Gyarados", 1f, BodyPlan.Serpent, V(0.04f, 0.14f, -0.3f)) { Coat = Scales };
        var blue = Rgb(58, 150, 226);
        var cream = Rgb(244, 226, 156);
        var white = Rgb(236, 242, 248);
        var navy = Rgb(36, 70, 160);
        var yellow = Rgb(250, 214, 74);

        // An S of a body coiled on the ground and rearing up, thickest in the middle
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
            // Where it rears up the cream belly faces forward, the white fins run down its back
            var front = V(0, 0, 1f) - along * along.Z;
            bool rearing = i >= 4;
            var belly = rearing ? Vector3.Normalize(front) : -back;
            var fins = rearing ? -belly : back;
            b.PaintEll(seg, mid + belly * Radius(i) * 0.7f, V(Radius(i) * 0.75f, len * 0.62f, Radius(i) * 0.6f), cream, Euler(along));
            b.Spike(seg, mid + fins * Radius(i) * 0.8f, mid + (fins + along * 0.3f) * (Radius(i) + 0.1f), 0.05f, white, 0.3f);
            PokeBuilder.Both(s => b.Mark(seg, mid + sideways * s * Radius(i) * 0.98f, sideways * s + back * 0.3f, 0.028f, 0.022f, yellow));
            parent = seg;
        }
        b.Limb(Body, points[0], points[1], Radius(0), Radius(1), blue);
        // The tail ends in a pale fin like a trident
        int tail = b.Tail(points[0]);
        b.Limb(tail, points[0], V(0.22f, 0.12f, -0.44f), 0.12f, 0.05f, blue);
        int fin = b.Part("tailFin", tail, V(0.22f, 0.12f, -0.44f), PokeRole.Fin);
        foreach (var (dx, dy) in new[] { (0.13f, 0.13f), (0.17f, 0.02f), (0.13f, -0.05f) })
            b.Spike(fin, V(0.22f, 0.12f, -0.44f), V(0.22f + dx * 0.5f, 0.13f + dy, -0.44f - dx), 0.055f, Rgb(170, 216, 244), 0.35f);

        int head = b.Head(points[^1], parent);
        float hy = 0.94f;
        b.Ell(head, V(0, hy, 0.15f), V(0.16f, 0.13f, 0.18f), blue);
        b.Ell(head, V(0, hy - 0.005f, 0.31f), V(0.115f, 0.08f, 0.1f), blue);
        // The gaping jaw, pink inside, with white fangs
        int jaw = b.Jaw(head, V(0, hy - 0.07f, 0.1f));
        b.Ell(jaw, V(0, hy - 0.17f, 0.25f), V(0.105f, 0.045f, 0.15f), blue, V(30f, 0, 0), blend: 0.006f);
        b.Ell(head, V(0, hy - 0.09f, 0.28f), V(0.085f, 0.065f, 0.11f), Rgb(232, 120, 132), V(16f, 0, 0), blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.07f * s, hy - 0.06f, 0.37f), V(0.07f * s, hy - 0.14f, 0.38f), 0.02f, White, mat: Shell, blend: 0.003f);
            b.Spike(jaw, V(0.065f * s, hy - 0.2f, 0.35f), V(0.065f * s, hy - 0.13f, 0.36f), 0.018f, White, mat: Shell, blend: 0.003f);
        });
        // The tall dark crest, white fins at the sides of the head, and white whiskers
        b.Spike(head, V(0, hy + 0.09f, 0.12f), V(0, hy + 0.34f, -0.02f), 0.055f, navy, 0.5f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.05f * s, hy + 0.08f, 0.12f), V(0.16f * s, hy + 0.29f, 0.01f), 0.042f, navy, 0.5f);
            b.Spike(head, V(0.11f * s, hy + 0.02f, 0.12f), V(0.27f * s, hy + 0.1f, 0.02f), 0.06f, white, 0.35f);
            b.Tube(head, new[] { V(0.09f * s, hy - 0.05f, 0.32f), V(0.14f * s, hy - 0.09f, 0.36f), V(0.16f * s, hy - 0.18f, 0.35f), V(0.14f * s, hy - 0.26f, 0.31f) }, 0.015f, 0.009f, white);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.105f * s, hy + 0.045f, 0.26f), V(0.75f * s, 0.25f, 0.6f), 0.034f, Rgb(214, 54, 54)));
        return b;
    }

    // ------------------------------------------------------------------ Budew line

    /// <summary>A rose: a ball of petals with the lines of their edges painted darker, cupped by two leaves.</summary>
    private static void Rose(PokeBuilder b, int bone, Vector3 at, float r, Color color, Vector3? leavesToward = null)
    {
        var dark = PixelCanvas.Mix(color, Rgb(40, 20, 40), 0.35f);
        b.Ell(bone, at, V(r, r * 0.92f, r), color, mat: Leaf, blend: 0.01f);
        b.PaintTorus(bone, at + V(0, r * 0.62f, 0), r * 0.42f, r * 0.1f, dark);
        b.PaintTorus(bone, at + V(0, r * 0.15f, 0), r * 0.88f, r * 0.1f, dark, V(14f, 0, 10f));
        b.PaintTorus(bone, at + V(0, -r * 0.35f, 0), r * 0.85f, r * 0.1f, dark, V(-12f, 0, -8f));
        if (leavesToward is Vector3 toward)
        {
            var leaf = Rgb(52, 136, 64);
            var d = Vector3.Normalize(toward);
            var side = Vector3.Normalize(Vector3.Cross(d, Vector3.UnitY));
            foreach (float s in new[] { -1f, 1f })
                b.Spike(bone, at + d * r * 0.6f, at + (d * 0.7f + side * s * 0.8f - Vector3.UnitY * 0.3f) * r * 1.8f, r * 0.42f, leaf, 0.35f, Leaf);
        }
    }

    private static PokeBuilder Budew()
    {
        var b = new PokeBuilder("Budew", 0.48f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Leaf };
        var green = Rgb(156, 214, 104);
        var dark = Rgb(66, 146, 70);
        var yellow = Rgb(250, 226, 84);
        var feet = Rgb(236, 214, 112);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.08f, 0));
            b.Ell(leg, V(0.075f * s, 0.035f, 0.03f), V(0.05f, 0.035f, 0.06f), feet);
        });
        // A skirt of dark sepals round the foot of the bud
        b.Ell(Body, V(0, 0.15f, 0), V(0.15f, 0.08f, 0.14f), dark);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.3f;
            b.Ell(Body, V(MathF.Sin(a) * 0.11f, 0.14f, MathF.Cos(a) * 0.1f), V(0.075f, 0.06f, 0.075f), dark);
        }

        int head = b.Head(V(0, 0.22f, 0));
        // The bud, its leaf wrapped round from behind and curling forward over the top, the yellow face showing
        b.Ell(head, V(0, 0.33f, -0.03f), V(0.16f, 0.17f, 0.14f), green);
        b.Ell(head, V(0, 0.3f, 0.07f), V(0.125f, 0.115f, 0.1f), yellow, blend: 0.012f);
        b.Tube(head, new[] { V(0, 0.4f, -0.1f), V(0, 0.52f, -0.06f), V(0, 0.57f, 0.02f), V(0, 0.52f, 0.09f) }, 0.105f, 0.07f, green, blend: 0.02f);
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.32f, 0.163f), V(0.35f * s, 0.05f, 1f), 0.03f, closed: true));
        return b;
    }

    private static PokeBuilder Roselia()
    {
        var b = new PokeBuilder("Roselia", 0.52f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Leaf };
        var green = Rgb(128, 202, 86);
        var pale = Rgb(214, 236, 160);
        var dark = Rgb(52, 136, 64);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.12f, 0));
            b.Limb(leg, V(0.04f * s, 0.12f, 0), V(0.05f * s, 0.035f, 0.01f), 0.03f, 0.026f, pale);
            b.Ell(leg, V(0.05f * s, 0.022f, 0.025f), V(0.035f, 0.022f, 0.045f), pale);
        });
        b.Ell(Body, V(0, 0.22f, 0), V(0.085f, 0.12f, 0.075f), pale);
        b.PaintEll(Body, V(0, 0.31f, 0), V(0.095f, 0.06f, 0.085f), green);
        // The dark leaf down its chest
        b.Ell(Body, V(0, 0.25f, 0.068f), V(0.05f, 0.08f, 0.015f), dark, V(8f, 0, 0), Leaf, 0.008f);
        b.Spike(Body, V(0, 0.19f, 0.07f), V(0, 0.12f, 0.06f), 0.04f, dark, 0.35f);
        // A red rose in one hand, a blue one in the other
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.3f, 0));
            b.Limb(arm, V(0.07f * s, 0.3f, 0), V(0.15f * s, 0.27f, 0.05f), 0.02f, 0.018f, green);
            Rose(b, arm, V(0.215f * s, 0.28f, 0.07f), 0.072f, s < 0 ? Rgb(232, 56, 78) : Rgb(42, 116, 222), V(-s, -0.3f, -0.4f));
        });

        int head = b.Head(V(0, 0.36f, 0));
        b.Ell(head, V(0, 0.44f, 0.01f), V(0.1f, 0.085f, 0.085f), green);
        b.Ell(head, V(0, 0.355f, 0.005f), V(0.045f, 0.03f, 0.04f), green);
        // A crown of three thorny leaves
        b.Spike(head, V(0, 0.5f, -0.01f), V(0, 0.67f, -0.05f), 0.048f, dark, 0.45f);
        PokeBuilder.Both(s => b.Spike(head, V(0.05f * s, 0.48f, 0), V(0.18f * s, 0.59f, -0.03f), 0.042f, dark, 0.45f));
        PokeBuilder.Both(s => b.Eye(head, V(0.04f * s, 0.45f, 0.087f), V(0.4f * s, 0.05f, 1f), 0.021f, closed: true));
        return b;
    }

    private static PokeBuilder Roserade()
    {
        var b = new PokeBuilder("Roserade", 0.74f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Leaf };
        var green = Rgb(76, 172, 84);
        var pale = Rgb(222, 238, 206);
        var cape = Rgb(46, 128, 64);
        var white = Rgb(248, 248, 242);
        var yellow = Rgb(244, 212, 74);
        var mask = Rgb(34, 92, 50);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.3f, 0));
            b.Limb(leg, V(0.05f * s, 0.3f, 0), V(0.06f * s, 0.05f, 0.01f), 0.032f, 0.026f, pale);
            b.Ell(leg, V(0.06f * s, 0.028f, 0.03f), V(0.038f, 0.028f, 0.055f), pale);
        });
        b.Ell(Body, V(0, 0.44f, 0), V(0.095f, 0.15f, 0.08f), pale);
        b.Ell(Body, V(0, 0.33f, 0), V(0.12f, 0.07f, 0.1f), pale);
        b.PaintEll(Body, V(0, 0.54f, 0.01f), V(0.105f, 0.07f, 0.09f), green);
        b.Torus(Body, V(0, 0.585f, 0), 0.055f, 0.022f, yellow, mat: Leaf, blend: 0.01f);
        // The leafy cape that hangs from its shoulders to the ground
        int capeBone = b.Part("cape", Body, V(0, 0.58f, -0.08f), PokeRole.Tail);
        b.Ell(capeBone, V(0, 0.36f, -0.12f), V(0.15f, 0.26f, 0.03f), cape, V(-8f, 0, 0), Leaf, 0.02f);
        PokeBuilder.Both(s => b.Spike(capeBone, V(0.09f * s, 0.16f, -0.14f), V(0.13f * s, 0.04f, -0.16f), 0.06f, cape, 0.3f, Leaf));
        b.Spike(capeBone, V(0, 0.14f, -0.15f), V(0, 0.03f, -0.17f), 0.06f, cape, 0.3f, Leaf);
        // A bouquet in each hand: three red roses, three blue
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.56f, 0));
            b.Limb(arm, V(0.09f * s, 0.56f, 0), V(0.17f * s, 0.49f, 0.08f), 0.026f, 0.022f, green);
            var rose = s < 0 ? Rgb(232, 58, 80) : Rgb(42, 112, 222);
            Rose(b, arm, V(0.24f * s, 0.53f, 0.1f), 0.06f, rose);
            Rose(b, arm, V(0.29f * s, 0.46f, 0.12f), 0.058f, rose);
            Rose(b, arm, V(0.21f * s, 0.43f, 0.14f), 0.056f, rose, V(-s, -0.5f, -0.6f));
        });

        int head = b.Head(V(0, 0.62f, 0));
        b.Ell(head, V(0, 0.7f, 0.01f), V(0.085f, 0.08f, 0.08f), green);
        b.Ell(head, V(0, 0.61f, 0.005f), V(0.035f, 0.035f, 0.035f), green);
        // A dark mask over the eyes, and the white petals of its hair swept back
        b.PaintEll(head, V(0, 0.71f, 0.05f), V(0.1f, 0.033f, 0.07f), mask);
        b.Ell(head, V(0, 0.77f, -0.02f), V(0.09f, 0.05f, 0.08f), white, mat: Leaf);
        foreach (var (tip, r) in new[] { (V(-0.25f, 0.86f, -0.06f), 0.07f), (V(-0.13f, 0.95f, -0.04f), 0.066f), (V(0.03f, 0.93f, -0.11f), 0.062f), (V(0.14f, 0.84f, -0.09f), 0.058f), (V(-0.07f, 0.8f, -0.21f), 0.058f) })
            Petal(b, head, V(0, 0.78f, -0.03f), tip, r, white, Leaf, 0.02f);
        PokeBuilder.Both(s => b.Eye(head, V(0.04f * s, 0.712f, 0.077f), V(0.5f * s, 0.05f, 1f), 0.022f, yellow, pupil: Rgb(196, 40, 44)));
        return b;
    }

    // ------------------------------------------------------------------ Zubat line

    /// <summary>
    /// The skin of a bat's wing, made before the struts and the body (it is carved): a fan of triangles from the
    /// wrist over the outline from the shoulder through the finger tips back to <paramref name="root"/> at the
    /// body (convex, lying about level in z), each filled by its inscribed ellipse grown a little, so the skin never
    /// reaches past the outline; then scallops cut between the fingers, small enough to leave other wings alone.
    /// </summary>
    private static void BatMembrane(PokeBuilder b, int bone, Vector3 shoulder, Vector3 wrist, Vector3[] tips, Vector3 root, Color membrane)
    {
        var rim = tips.Append(root).ToArray();
        float z = new[] { shoulder, wrist, root }.Concat(tips).Average(p => p.Z);
        Panel(b, bone, wrist, root, shoulder, z, membrane);
        for (int i = 0; i < rim.Length - 1; i++) Panel(b, bone, wrist, rim[i], rim[i + 1], z, membrane);
        var centroid = (rim.Aggregate(Vector3.Zero, (sum, p) => sum + p) + wrist + shoulder) / (rim.Length + 2);
        for (int i = 0; i < rim.Length - 1; i++)
        {
            var a = rim[i];
            var c = rim[i + 1];
            var dir = V(c.X - a.X, c.Y - a.Y, 0);
            float e = dir.Length();
            dir /= e;
            var n = V(-dir.Y, dir.X, 0);
            var mid = V((a.X + c.X) / 2f, (a.Y + c.Y) / 2f, z);
            if (Vector3.Dot(n, mid - centroid) < 0f) n = -n;
            b.Cut(bone, mid + n * 0.1f * e, V(0.3f * e, 0.3f * e, 0.05f));
        }
    }

    /// <summary>
    /// A flat panel filling the triangle p, q, r: its Steiner inellipse (centred on the triangle's middle, touching
    /// each side at its midpoint) grown by a fifth, lying in the plane z.
    /// </summary>
    private static void Panel(PokeBuilder b, int bone, Vector3 p, Vector3 q, Vector3 r, float z, Color color)
    {
        var g = (p + q + r) / 3f;
        // Two conjugate half-diameters, then the ellipse's own axes from the 2x2 singular value decomposition
        var f1 = (r - g) / 2f;
        var f2 = (p - q) / (2f * MathF.Sqrt(3f));
        float e = (f1.X + f2.Y) / 2f, f = (f1.X - f2.Y) / 2f, gg = (f1.Y + f2.X) / 2f, h = (f1.Y - f2.X) / 2f;
        float qq = MathF.Sqrt(e * e + h * h), rr = MathF.Sqrt(f * f + gg * gg);
        float a1 = MathF.Atan2(gg, f), a2 = MathF.Atan2(h, e);
        float phi = (a2 + a1) / 2f;
        float grow = 1.2f;
        b.Ell(bone, V(g.X, g.Y, z), V((qq + rr) * grow, MathF.Abs(qq - rr) * grow + 0.004f, 0.012f), color, V(0, 0, phi * 180f / MathF.PI), blend: 0.006f);
    }

    /// <summary>A bat wing's arm and finger bones, laid over its membrane.</summary>
    private static void BatStruts(PokeBuilder b, int bone, Vector3 shoulder, Vector3 wrist, Vector3[] tips, Color color, float strut)
    {
        b.Limb(bone, shoulder, wrist, strut, strut * 0.85f, color);
        foreach (var tip in tips) b.Limb(bone, wrist, tip, strut * 0.75f, strut * 0.4f, color);
    }

    /// <summary>A whole bat wing: its membrane, then its struts.</summary>
    private static void BatWing(PokeBuilder b, int bone, Vector3 shoulder, Vector3 wrist, Vector3[] tips, Vector3 root, Color membrane, Color struts, float strut)
    {
        BatMembrane(b, bone, shoulder, wrist, tips, root, membrane);
        BatStruts(b, bone, shoulder, wrist, tips, struts, strut);
    }

    /// <summary>A mouth gaping open on the front of <paramref name="bone"/>: carved, dark inside, with two fangs above and two below.</summary>
    private static void Gape(PokeBuilder b, int bone, Vector3 at, Vector3 radii, Color inside, float fang)
    {
        b.Cut(bone, at + V(0, 0, radii.Z * 0.6f), radii, blend: 0.01f);
        b.PaintEll(bone, at, radii * 1.08f, inside);
        PokeBuilder.Both(s =>
        {
            float x = radii.X * 0.42f * s;
            b.Spike(bone, at + V(x, radii.Y * 0.95f, -radii.Z * 0.1f), at + V(x, radii.Y * 0.95f - fang, radii.Z * 0.05f), fang * 0.4f, White, mat: Shell, blend: 0.003f);
            b.Spike(bone, at + V(x * 0.9f, -radii.Y * 0.95f, -radii.Z * 0.1f), at + V(x * 0.9f, -radii.Y * 0.95f + fang * 0.8f, radii.Z * 0.05f), fang * 0.38f, White, mat: Shell, blend: 0.003f);
        });
    }

    private static PokeBuilder Zubat()
    {
        var b = new PokeBuilder("Zubat", 0.7f, BodyPlan.Bird, V(0, 0.5f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(44, 152, 232);
        var purple = Rgb(180, 152, 232);

        // Wide wings of purple skin on blue bones, made first because they are carved
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.08f * s, 0.53f, 0));
            BatWing(b, wing, V(0.08f * s, 0.53f, 0), V(0.3f * s, 0.72f, -0.03f),
                new[] { V(0.52f * s, 0.64f, -0.04f), V(0.48f * s, 0.43f, -0.04f), V(0.3f * s, 0.34f, -0.03f) }, V(0.06f * s, 0.44f, -0.01f), purple, blue, 0.022f);
        });
        // No eyes: a round body that is mostly mouth, tall ears, and two long streamers for legs
        b.Ell(Body, V(0, 0.5f, 0), V(0.1f, 0.11f, 0.09f), blue);
        Gape(b, Body, V(0, 0.48f, 0.075f), V(0.05f, 0.048f, 0.03f), Rgb(40, 18, 46), 0.03f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.42f, -0.01f));
            b.Tube(leg, new[] { V(0.03f * s, 0.42f, -0.01f), V(0.05f * s, 0.26f, 0.02f), V(0.07f * s, 0.1f, 0.04f), V(0.08f * s, 0.01f, 0.05f) }, 0.022f, 0.012f, blue);
        });
        int head = b.Head(V(0, 0.56f, 0));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.58f, 0));
            b.Spike(ear, V(0.05f * s, 0.56f, -0.01f), V(0.11f * s, 0.73f, -0.03f), 0.05f, blue, 0.5f);
            b.Spike(ear, V(0.052f * s, 0.58f, 0.008f), V(0.105f * s, 0.69f, -0.01f), 0.03f, purple, 0.35f);
        });
        b.Ell(head, V(0, 0.585f, 0.0f), V(0.07f, 0.04f, 0.06f), blue);
        return Lift(b);
    }

    private static PokeBuilder Golbat()
    {
        var b = new PokeBuilder("Golbat", 0.9f, BodyPlan.Bird, V(0, 0.52f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(44, 152, 232);
        var purple = Rgb(180, 152, 232);

        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.14f * s, 0.6f, 0));
            BatWing(b, wing, V(0.14f * s, 0.6f, -0.01f), V(0.42f * s, 0.86f, -0.05f),
                new[] { V(0.7f * s, 0.84f, -0.06f), V(0.68f * s, 0.55f, -0.06f), V(0.46f * s, 0.42f, -0.05f) }, V(0.12f * s, 0.44f, -0.02f), purple, blue, 0.028f);
        });
        // A round body that is nearly all mouth, small fierce eyes over it, stubby legs
        b.Ell(Body, V(0, 0.5f, 0), V(0.18f, 0.21f, 0.15f), blue);
        Gape(b, Body, V(0, 0.44f, 0.12f), V(0.115f, 0.14f, 0.06f), Rgb(48, 16, 30), 0.07f);
        b.PaintEll(Body, V(0, 0.36f, 0.1f), V(0.07f, 0.04f, 0.05f), Rgb(208, 72, 96));
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.32f, 0));
            b.Limb(leg, V(0.08f * s, 0.32f, 0), V(0.1f * s, 0.12f, 0.02f), 0.035f, 0.03f, blue);
            b.Ell(leg, V(0.1f * s, 0.09f, 0.05f), V(0.04f, 0.03f, 0.06f), blue);
        });
        int head = b.Head(V(0, 0.62f, 0));
        b.Ell(head, V(0, 0.66f, 0.0f), V(0.13f, 0.07f, 0.1f), blue);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.7f, 0));
            b.Spike(ear, V(0.08f * s, 0.68f, -0.01f), V(0.15f * s, 0.82f, -0.03f), 0.045f, blue, 0.5f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.06f * s, 0.665f, 0.093f), V(0.4f * s, 0.15f, 1f), 0.022f, Rgb(160, 40, 60)));
        return Lift(b);
    }

    private static PokeBuilder Crobat()
    {
        var b = new PokeBuilder("Crobat", 0.92f, BodyPlan.Bird, V(0, 0.5f, 0)) { Coat = Fur }.Hover();
        var purple = Rgb(156, 82, 196);
        var membrane = Rgb(110, 186, 220);

        // Four wings: the arms' great ones and the smaller pair its legs became, every membrane before any strut
        var wings = new (int Bone, Vector3 Shoulder, Vector3 Wrist, Vector3[] Tips, float Strut)[4];
        PokeBuilder.Both(s =>
        {
            int k = s < 0 ? 0 : 2;
            wings[k] = (b.Wing(s, V(0.09f * s, 0.55f, 0)), V(0.09f * s, 0.55f, -0.01f), V(0.32f * s, 0.74f, -0.04f),
                new[] { V(0.62f * s, 0.72f, -0.05f), V(0.56f * s, 0.57f, -0.05f), V(0.36f * s, 0.5f, -0.04f) }, 0.024f);
            BatMembrane(b, wings[k].Bone, wings[k].Shoulder, wings[k].Wrist, wings[k].Tips, V(0.08f * s, 0.48f, -0.02f), membrane);
            wings[k + 1] = (b.Part(s < 0 ? "lowerWingL" : "lowerWingR", Body, V(0.07f * s, 0.43f, 0), PokeRole.Wing, 0.8f, s), V(0.07f * s, 0.44f, -0.01f),
                V(0.24f * s, 0.42f, -0.03f), new[] { V(0.44f * s, 0.36f, -0.04f), V(0.38f * s, 0.22f, -0.04f) }, 0.02f);
            BatMembrane(b, wings[k + 1].Bone, wings[k + 1].Shoulder, wings[k + 1].Wrist, wings[k + 1].Tips, V(0.07f * s, 0.36f, -0.02f), membrane);
        });
        foreach (var w in wings) BatStruts(b, w.Bone, w.Shoulder, w.Wrist, w.Tips, purple, w.Strut);
        b.Ell(Body, V(0, 0.48f, 0), V(0.12f, 0.12f, 0.11f), purple);
        Gape(b, Body, V(0, 0.43f, 0.09f), V(0.04f, 0.02f, 0.02f), Rgb(60, 20, 50), 0.025f);
        int head = b.Head(V(0, 0.54f, 0));
        b.Ell(head, V(0, 0.56f, 0.0f), V(0.1f, 0.06f, 0.09f), purple);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, 0.6f, -0.01f));
            b.Spike(ear, V(0.07f * s, 0.59f, -0.02f), V(0.2f * s, 0.68f, -0.06f), 0.045f, purple, 0.45f);
        });
        PokeBuilder.Both(s => b.Eye(Body, V(0.05f * s, 0.515f, 0.108f), V(0.45f * s, 0.15f, 1f), 0.024f, Rgb(250, 214, 60), pupil: Rgb(200, 40, 50)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Geodude line

    /// <summary>Lumps of rock over a body: stones of about <paramref name="size"/> at points round the ellipsoid it fills.</summary>
    private static void Rubble(PokeBuilder b, int bone, Vector3 center, Vector3 radii, float size, Color color, int count, uint seed)
    {
        var rng = new GenomeRandom(seed);
        for (int i = 0; i < count; i++)
        {
            // Points spread over the sphere by the golden angle, nudged so the lumps don't line up
            float y = 1f - 2f * (i + 0.5f) / count;
            float r = MathF.Sqrt(1f - y * y);
            float a = i * 2.39996f + rng.Float() * 0.5f;
            var dir = V(MathF.Cos(a) * r, y, MathF.Sin(a) * r);
            var at = center + dir * radii * 0.88f;
            float k = size * rng.Range(0.8f, 1.2f);
            var tone = PixelCanvas.Mix(color, (i % 3) switch { 0 => Rgb(255, 255, 250), 1 => Rgb(60, 60, 56), _ => color }, 0.07f);
            b.Ell(bone, at, V(k, k * 0.85f, k), tone, V(rng.Range(0f, 60f), rng.Range(0f, 90f), 0), Shell, 0.03f);
        }
    }

    private static PokeBuilder Geodude()
    {
        var b = new PokeBuilder("Geodude", 0.56f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var gray = Rgb(172, 170, 160);

        b.Ell(Body, V(0, 0.42f, 0), V(0.17f, 0.15f, 0.15f), gray);
        Rubble(b, Body, V(0, 0.42f, -0.02f), V(0.16f, 0.14f, 0.13f), 0.06f, gray, 10, 74u);
        // A heavy brow over a scowl
        b.Ell(Body, V(0, 0.475f, 0.12f), V(0.12f, 0.035f, 0.05f), gray, V(-12f, 0, 0));
        b.Mark(Body, V(0, 0.36f, 0.143f), V(0, -0.25f, 1f), 0.04f, 0.009f, Rgb(70, 68, 62), MarkShape.Bar);
        PokeBuilder.Both(s => b.Eye(Body, V(0.055f * s, 0.43f, 0.142f), V(0.3f * s, -0.05f, 1f), 0.024f));
        // Two brawny arms, fists raised
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.42f, 0));
            b.Limb(arm, V(0.15f * s, 0.42f, 0), V(0.3f * s, 0.38f, 0.04f), 0.05f, 0.045f, gray);
            b.Ell(arm, V(0.22f * s, 0.425f, 0.02f), V(0.065f, 0.048f, 0.048f), gray);
            b.Limb(arm, V(0.3f * s, 0.38f, 0.04f), V(0.33f * s, 0.52f, 0.07f), 0.045f, 0.042f, gray);
            b.Ell(arm, V(0.335f * s, 0.575f, 0.08f), V(0.062f, 0.066f, 0.06f), gray);
            for (int k = -1; k <= 1; k++)
                b.Ell(arm, V((0.335f + 0.022f * k) * s, 0.6f, 0.13f), V(0.02f, 0.02f, 0.016f), gray, blend: 0.012f);
        });
        return Lift(b);
    }

    private static PokeBuilder Graveler()
    {
        var b = new PokeBuilder("Graveler", 0.78f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Shell };
        var gray = Rgb(156, 154, 144);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.18f, 0));
            b.Limb(leg, V(0.12f * s, 0.2f, 0), V(0.13f * s, 0.08f, 0.03f), 0.07f, 0.065f, gray);
            b.Ell(leg, V(0.13f * s, 0.05f, 0.04f), V(0.08f, 0.05f, 0.09f), gray);
        });
        b.Ell(Body, V(0, 0.4f, 0), V(0.27f, 0.26f, 0.23f), gray);
        Rubble(b, Body, V(0, 0.4f, -0.02f), V(0.26f, 0.25f, 0.21f), 0.09f, gray, 18, 75u);
        // A face in the rocks: brow, eyes, and a mouth gaping open
        b.Ell(Body, V(0, 0.5f, 0.2f), V(0.16f, 0.04f, 0.05f), gray, V(-12f, 0, 0));
        PokeBuilder.Both(s => b.Eye(Body, V(0.075f * s, 0.455f, 0.225f), V(0.3f * s, -0.05f, 1f), 0.03f));
        b.Cut(Body, V(0, 0.34f, 0.25f), V(0.1f, 0.045f, 0.05f), blend: 0.01f);
        b.PaintEll(Body, V(0, 0.34f, 0.21f), V(0.11f, 0.05f, 0.06f), Rgb(120, 54, 60));
        b.PaintEll(Body, V(0, 0.32f, 0.21f), V(0.07f, 0.025f, 0.05f), Rgb(222, 124, 132));
        // Four arms: a pair high, a pair low, three fingers on each hand
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.22f * s, 0.48f, 0.05f));
            b.Limb(arm, V(0.22f * s, 0.48f, 0.05f), V(0.35f * s, 0.5f, 0.1f), 0.075f, 0.065f, gray);
            b.Ell(arm, V(0.4f * s, 0.49f, 0.13f), V(0.065f, 0.06f, 0.065f), gray);
            Digits(b, arm, V(0.44f * s, 0.48f, 0.15f), V(s, -0.3f, 0.6f), V(0, 0.6f, 0), 0.065f, 0.025f, gray);
            int lower = b.Part(s < 0 ? "lowArmL" : "lowArmR", Body, V(0.22f * s, 0.3f, 0.08f), PokeRole.Arm, s + 1.4f, s);
            b.Limb(lower, V(0.22f * s, 0.3f, 0.08f), V(0.34f * s, 0.24f, 0.14f), 0.07f, 0.06f, gray);
            b.Ell(lower, V(0.38f * s, 0.21f, 0.17f), V(0.06f, 0.055f, 0.06f), gray);
            Digits(b, lower, V(0.41f * s, 0.19f, 0.19f), V(s, -0.6f, 0.6f), V(0, 0.6f, 0), 0.06f, 0.023f, gray);
        });
        return b;
    }

    private static PokeBuilder Golem()
    {
        var b = new PokeBuilder("Golem", 0.88f, BodyPlan.Biped, V(0, 0.5f, -0.04f)) { Coat = Fur };
        var shell = Rgb(128, 128, 120);
        var seam = Rgb(92, 92, 86);
        var tan = Rgb(214, 174, 136);
        var red = Rgb(204, 64, 62);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.18f * s, 0.22f, 0.08f));
            b.Limb(leg, V(0.18f * s, 0.22f, 0.08f), V(0.2f * s, 0.07f, 0.14f), 0.085f, 0.075f, tan);
            b.Ell(leg, V(0.2f * s, 0.045f, 0.18f), V(0.085f, 0.045f, 0.1f), tan);
            Digits(b, leg, V(0.2f * s, 0.04f, 0.26f), V(0, 0, 1f), V(0.6f, 0, 0), 0.05f, 0.018f, Claw, Shell);
        });
        // The great shell of rock plates, and the two white horns at its shoulders
        b.Ell(Body, V(0, 0.52f, -0.06f), V(0.36f, 0.35f, 0.34f), shell, mat: Shell);
        foreach (var turn in new[] { V(0, 0, 0), V(90f, 0, 0), V(0, 0, 90f), V(90f, 50f, 0), V(90f, -50f, 0), V(35f, 0, 60f) })
            b.PaintTorus(Body, V(0, 0.52f, -0.06f), 0.355f, 0.008f, seam, turn, 1f, 0.95f);
        PokeBuilder.Both(s => b.Spike(Body, V(0.19f * s, 0.74f, 0.17f), V(0.27f * s, 0.9f, 0.25f), 0.05f, Claw, mat: Shell));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.3f * s, 0.52f, 0.12f));
            b.Limb(arm, V(0.3f * s, 0.52f, 0.12f), V(0.42f * s, 0.44f, 0.24f), 0.075f, 0.062f, tan);
            b.Ell(arm, V(0.44f * s, 0.42f, 0.28f), V(0.07f, 0.065f, 0.07f), tan);
            Digits(b, arm, V(0.45f * s, 0.41f, 0.33f), V(0.2f * s, -0.2f, 1f), V(0.6f, 0, 0), 0.05f, 0.018f, Claw, Shell);
        });

        int head = b.Head(V(0, 0.42f, 0.22f));
        b.Ell(head, V(0, 0.45f, 0.33f), V(0.13f, 0.11f, 0.12f), tan);
        int jaw = b.Jaw(head, V(0, 0.4f, 0.3f));
        b.Ell(jaw, V(0, 0.355f, 0.39f), V(0.1f, 0.045f, 0.09f), tan, blend: 0.008f);
        b.Ell(head, V(0, 0.395f, 0.4f), V(0.075f, 0.035f, 0.07f), red, blend: 0.006f);
        PokeBuilder.Both(s => b.Ell(head, V(0.06f * s, 0.53f, 0.38f), V(0.06f, 0.025f, 0.04f), tan, V(0, 0, -15f * s)));
        PokeBuilder.Both(s => b.Eye(head, V(0.062f * s, 0.49f, 0.43f), V(0.4f * s, 0.05f, 1f), 0.03f, red));
        return b;
    }

    // ------------------------------------------------------------------ Onix line

    /// <summary>
    /// A rock-snake: a chain of boulders along <paramref name="path"/> (tail first), the first on the body bone and
    /// each of the others on a segment of its own. Returns the bone of each boulder.
    /// </summary>
    private static int[] Boulders(PokeBuilder b, Vector3[] path, Func<int, float> size, Func<int, Vector3> turn, Color color, SurfaceMaterial mat, float rounding, float blend = 0.02f)
    {
        var bones = new int[path.Length];
        bones[0] = Body;
        b.Box(Body, path[0], V(size(0), size(0), size(0)), size(0) * rounding, color, turn(0), mat, blend);
        for (int i = 1; i < path.Length; i++)
        {
            bones[i] = b.Part("seg" + i, bones[i - 1], (path[i - 1] + path[i]) / 2f, PokeRole.Segment, i);
            float r = size(i);
            b.Box(bones[i], path[i], V(r, r, r), r * rounding, color, turn(i), mat, blend);
        }
        return bones;
    }

    /// <summary>The arch an Onix or a Steelix stands in: from the tail curled on the ground, up behind its right shoulder and over to the head.</summary>
    private static readonly Vector3[] ArchPath =
    {
        V(0.3f, 0.05f, 0.08f), V(0.36f, 0.07f, -0.06f), V(0.35f, 0.1f, -0.19f), V(0.3f, 0.18f, -0.28f), V(0.25f, 0.32f, -0.31f),
        V(0.2f, 0.48f, -0.27f), V(0.15f, 0.64f, -0.2f), V(0.09f, 0.77f, -0.11f), V(0.03f, 0.85f, -0.02f)
    };

    private static PokeBuilder Onix()
    {
        var path = ArchPath;
        var b = new PokeBuilder("Onix", 1f, BodyPlan.Serpent, path[0]) { Coat = Shell };
        var gray = Rgb(156, 156, 158);
        // Boulders from the tail's pebbles to the great stones of its neck, each turned its own way
        // Each a stone of its own, touching the next with a crease between
        int neck = Boulders(b, path, i => 0.07f + 0.055f * MathF.Min(1f, i / 5f), i => V(i * 37f % 50f - 25f, i * 53f % 70f - 35f, i * 29f % 40f - 20f), gray, Shell, 0.65f, 0.008f)[^1];
        int tail = b.Tail(path[0]);
        b.Box(tail, path[0] + V(-0.06f, -0.015f, 0.08f), V(0.045f, 0.045f, 0.045f), 0.022f, gray, V(20f, 30f, 0), Shell, 0.02f);

        int head = b.Head(V(0.0f, 0.86f, 0.06f), neck);
        b.Box(head, V(0, 0.87f, 0.17f), V(0.135f, 0.105f, 0.19f), 0.075f, gray, V(-8f, 0, 0), Shell, 0.02f);
        b.Box(head, V(0, 0.83f, 0.32f), V(0.1f, 0.07f, 0.075f), 0.045f, gray, V(-8f, 0, 0), Shell, 0.02f);
        // The ridge of a horn along its crown, a heavy brow, and a long grim mouth
        b.Spike(head, V(0, 0.95f, 0.14f), V(0, 1.14f, -0.05f), 0.055f, gray, 0.45f);
        PokeBuilder.Both(s => b.Ell(head, V(0.08f * s, 0.945f, 0.285f), V(0.065f, 0.027f, 0.05f), gray, V(0, 0, -12f * s), Shell, 0.012f));
        b.Mark(head, V(0, 0.8f, 0.39f), V(0, -0.15f, 1f), 0.075f, 0.009f, Rgb(64, 64, 70), MarkShape.Bar);
        PokeBuilder.Both(s => b.Eye(head, V(0.09f * s, 0.905f, 0.305f), V(0.55f * s, 0.05f, 0.85f), 0.03f));
        return b;
    }

    private static PokeBuilder Steelix()
    {
        var path = ArchPath;
        var b = new PokeBuilder("Steelix", 1f, BodyPlan.Serpent, path[0]) { Coat = Metal };
        var steel = Rgb(150, 168, 196);
        var dark = Rgb(70, 76, 96);

        // Cut-steel segments, a pair of blades jutting from every other one
        float Size(int i) => 0.06f + 0.085f * MathF.Min(1f, i / 6f);
        var bones = Boulders(b, path, Size, i => V(0, i * 41f % 30f - 15f, 0), steel, Metal, 0.25f);
        int neck = bones[^1];
        for (int i = 2; i < path.Length; i += 2)
        {
            int seg = bones[i];
            float r = Size(i);
            var at = path[i];
            var back = Vector3.Normalize(path[i - 1] - path[Math.Min(path.Length - 1, i + 1)]);
            PokeBuilder.Both(s => b.Spike(seg, at + V(r * 0.8f * s, r * 0.3f, 0), at + V((r + 0.08f) * s, r * 0.5f, 0) + back * (r + 0.06f), r * 0.32f, steel, 0.4f, Metal));
        }
        int tail = b.Tail(path[0]);
        b.Spike(tail, path[0], path[0] + V(-0.12f, 0.06f, 0.1f), 0.04f, steel, 0.45f, Metal);
        b.Spike(tail, path[0], path[0] + V(-0.14f, 0.0f, 0.04f), 0.035f, steel, 0.45f, Metal);

        int head = b.Head(V(0.0f, 0.86f, 0.04f), neck);
        // A long flat-topped head over a massive square jaw, ridged at the back
        b.Box(head, V(0, 0.92f, 0.16f), V(0.15f, 0.08f, 0.23f), 0.045f, steel, V(-6f, 0, 0), Metal, 0.015f);
        int jaw = b.Jaw(head, V(0, 0.84f, 0.04f));
        b.Box(jaw, V(0, 0.79f, 0.2f), V(0.13f, 0.06f, 0.2f), 0.04f, steel, V(6f, 0, 0), Metal, 0.008f);
        b.PaintEll(head, V(0, 0.845f, 0.34f), V(0.15f, 0.012f, 0.12f), dark);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.09f * s, 0.97f, -0.02f), V(0.15f * s, 1.05f, -0.22f), 0.045f, steel, 0.4f, Metal);
            b.PaintEll(head, V(0.1f * s, 0.94f, 0.3f), V(0.045f, 0.03f, 0.045f), dark);
        });
        b.Spike(head, V(0, 0.99f, -0.02f), V(0, 1.09f, -0.22f), 0.045f, steel, 0.4f, Metal);
        PokeBuilder.Both(s => b.Eye(head, V(0.1f * s, 0.94f, 0.33f), V(0.5f * s, 0.15f, 0.85f), 0.024f, Rgb(214, 40, 44)));
        return b;
    }

    // ------------------------------------------------------------------ Cranidos line

    private static PokeBuilder HeadButter(bool grown)
    {
        var b = new PokeBuilder(grown ? "Rampardos" : "Cranidos", grown ? 0.9f : 0.72f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Scales };
        var body = grown ? Rgb(70, 74, 88) : Rgb(192, 198, 208);
        var dome = grown ? Rgb(60, 152, 232) : Rgb(118, 186, 236);
        var spike = grown ? Rgb(236, 232, 214) : Rgb(150, 156, 168);
        float k = grown ? 1.15f : 1f;

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s * k, 0.32f, -0.06f));
            b.Limb(leg, V(0.1f * s * k, 0.32f, -0.06f), V(0.13f * s * k, 0.18f, 0.03f), 0.08f * k, 0.055f * k, body);
            b.Limb(leg, V(0.13f * s * k, 0.18f, 0.03f), V(0.13f * s * k, 0.05f, -0.04f), 0.05f * k, 0.035f * k, body);
            b.Ell(leg, V(0.13f * s * k, 0.03f, 0.03f), V(0.05f * k, 0.03f, 0.085f * k), body);
            Digits(b, leg, V(0.13f * s * k, 0.03f, 0.1f * k), V(0, 0, 1f), V(0.6f, 0, 0), 0.045f, 0.015f, Claw, Shell);
        });
        // A body leaning forward, blue along the back (in bands on the grown one), spikes down the spine
        b.Ell(Body, V(0, 0.4f, 0.02f), V(0.15f * k, 0.16f, 0.21f * k), body, V(-22f, 0, 0));
        if (grown)
            foreach (float z in new[] { 0.1f, -0.04f, -0.16f })
                b.PaintEll(Body, V(0, 0.5f - z * 0.4f, z), V(0.19f, 0.035f, 0.05f), dome, V(-22f, 0, 0));
        else
            b.PaintEll(Body, V(0, 0.52f, -0.04f), V(0.13f, 0.07f, 0.17f), dome, V(-22f, 0, 0));
        for (int i = 0; i < 3; i++)
            b.Spike(Body, V(0, 0.53f - i * 0.05f, -0.02f - i * 0.1f), V(0, 0.62f - i * 0.06f, -0.08f - i * 0.12f), 0.035f * k, spike, 0.5f, Shell);
        int tail = b.Tail(V(0, 0.36f, -0.18f));
        b.Tube(tail, new[] { V(0, 0.36f, -0.18f), V(0, 0.31f, -0.36f), V(0, 0.26f, -0.5f), V(0, 0.24f, -0.6f * k) }, 0.085f * k, 0.022f, body, blend: 0.02f);
        if (grown)
            foreach (float z in new[] { -0.32f, -0.44f })
                b.PaintEll(tail, V(0, 0.3f, z), V(0.09f, 0.08f, 0.035f), dome);
        else
            b.PaintEll(tail, V(0, 0.36f, -0.42f), V(0.05f, 0.04f, 0.2f), dome);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s * k, 0.42f, 0.15f));
            b.Limb(arm, V(0.1f * s * k, 0.42f, 0.15f), V(0.14f * s * k, 0.33f, 0.21f), 0.035f * k, 0.03f * k, body);
            Digits(b, arm, V(0.14f * s * k, 0.32f, 0.22f), V(0, -0.7f, 0.6f), V(0.6f, 0, 0), 0.035f, 0.012f, Claw, Shell);
        });

        int head = b.Head(V(0, 0.5f, 0.16f));
        // A big domed skull, hard as iron, over a small face
        b.Ell(head, V(0, 0.52f, 0.28f), V(0.12f * k, 0.1f, 0.13f * k), body);
        b.Ell(head, V(0, 0.61f, 0.21f), V(0.15f * k, 0.12f * k, 0.165f * k), dome, mat: Shell);
        if (grown) b.PaintEll(head, V(0, 0.5f, 0.3f), V(0.16f, 0.04f, 0.12f), Rgb(30, 30, 40));
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.08f * s, 0.65f, 0.1f), V(0.15f * s, 0.74f, -0.02f), 0.035f * k, spike, 0.6f, Shell);
            b.Spike(head, V(0.12f * s, 0.55f, 0.12f), V(0.22f * s, 0.58f, 0.02f), 0.03f * k, spike, 0.6f, Shell);
        });
        b.Mark(head, V(0, 0.46f, 0.37f * k), V(0, -0.2f, 1f), 0.035f, 0.007f, Rgb(50, 50, 60), MarkShape.Bar);
        PokeBuilder.Both(s => b.Eye(head, V(0.08f * s * k, 0.52f, 0.35f * k), V(0.6f * s, 0, 0.8f), 0.036f, sclera: true, pupil: Rgb(200, 36, 40)));
        return b;
    }

    // ------------------------------------------------------------------ Shieldon line

    private static PokeBuilder Shieldon()
    {
        var b = new PokeBuilder("Shieldon", 0.58f, BodyPlan.Quadruped, V(0, 0.22f, -0.08f)) { Coat = Scales };
        var tan = Rgb(234, 202, 122);
        var gray = Rgb(108, 110, 118);
        var cream = Rgb(240, 236, 220);

        foreach (var (z, front) in new[] { (0.06f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.11f * s, 0.14f, z), front);
                b.Limb(leg, V(0.11f * s, 0.14f, z), V(0.12f * s, 0.05f, z + 0.01f), 0.055f, 0.05f, tan);
                b.Ell(leg, V(0.12f * s, 0.03f, z + 0.03f), V(0.05f, 0.03f, 0.06f), tan);
                b.Spike(leg, V(0.12f * s, 0.03f, z + 0.07f), V(0.12f * s, 0.01f, z + 0.11f), 0.018f, Claw, mat: Shell);
            });
        b.Ell(Body, V(0, 0.22f, -0.08f), V(0.15f, 0.12f, 0.2f), tan);
        int tail = b.Tail(V(0, 0.22f, -0.26f));
        b.Spike(tail, V(0, 0.22f, -0.26f), V(0, 0.2f, -0.4f), 0.06f, tan);

        int head = b.Head(V(0, 0.26f, 0.08f));
        // The great shield of its face: a dark plate with a pale band across, the snout below it
        b.Ell(head, V(0, 0.35f, 0.12f), V(0.19f, 0.18f, 0.07f), gray, V(28f, 0, 0), Shell);
        b.PaintEll(head, V(0, 0.26f, 0.2f), V(0.21f, 0.04f, 0.07f), cream, V(28f, 0, 0));
        b.Ell(head, V(0, 0.18f, 0.2f), V(0.11f, 0.075f, 0.075f), gray, mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.058f * s, 0.2f, 0.268f), V(0.35f * s, 0.1f, 1f), 0.03f, sclera: true));
        return b;
    }

    private static PokeBuilder Bastiodon()
    {
        var b = new PokeBuilder("Bastiodon", 0.86f, BodyPlan.Quadruped, V(0, 0.3f, -0.14f)) { Coat = Scales };
        var tan = Rgb(232, 200, 116);
        var wall = Rgb(74, 74, 82);
        var stone = Rgb(150, 150, 156);
        var yellow = Rgb(250, 222, 96);

        foreach (var (z, front) in new[] { (0.02f, true), (-0.3f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.16f * s, 0.18f, z), front);
                b.Limb(leg, V(0.16f * s, 0.18f, z), V(0.17f * s, 0.06f, z + 0.01f), 0.075f, 0.07f, wall);
                b.Ell(leg, V(0.17f * s, 0.035f, z + 0.03f), V(0.075f, 0.035f, 0.085f), wall);
                Digits(b, leg, V(0.17f * s, 0.03f, z + 0.1f), V(0, -0.2f, 1f), V(0.6f, 0, 0), 0.035f, 0.016f, Claw, Shell);
            });
        b.Ell(Body, V(0, 0.3f, -0.14f), V(0.22f, 0.17f, 0.26f), tan);
        foreach (float z in new[] { -0.02f, -0.16f, -0.3f })
            b.PaintEll(Body, V(0, 0.45f, z), V(0.17f, 0.06f, 0.05f), wall);
        int tail = b.Tail(V(0, 0.28f, -0.38f));
        b.Spike(tail, V(0, 0.28f, -0.38f), V(0, 0.24f, -0.52f), 0.07f, wall);

        int head = b.Head(V(0, 0.3f, 0.08f));
        // Its face is a castle wall: battlements and two horns along the top, yellow windows, tusks at the foot
        b.Box(head, V(0, 0.4f, 0.16f), V(0.27f, 0.26f, 0.06f), 0.04f, wall, mat: Shell);
        foreach (float x in new[] { -0.17f, 0f, 0.17f })
            b.Box(head, V(x, 0.68f, 0.16f), V(0.055f, 0.04f, 0.05f), 0.015f, wall, mat: Shell, blend: 0.01f);
        PokeBuilder.Both(s => b.Spike(head, V(0.25f * s, 0.62f, 0.16f), V(0.31f * s, 0.78f, 0.16f), 0.05f, stone, mat: Shell));
        foreach (var (x, y) in new[] { (-0.1f, 0.54f), (0.1f, 0.54f), (-0.17f, 0.4f), (0.17f, 0.4f) })
            b.Mark(head, V(x, y, 0.222f), V(0, 0, 1f), 0.03f, 0.042f, yellow);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.14f * s, 0.2f, 0.2f), V(0.42f * s, 0.24f, 0.26f), 0.045f, Claw, mat: Shell);
            b.Spike(head, V(0.05f * s, 0.16f, 0.2f), V(0.05f * s, 0.1f, 0.22f), 0.022f, Claw, mat: Shell, blend: 0.004f);
        });
        b.Ell(head, V(0, 0.18f, 0.2f), V(0.1f, 0.05f, 0.05f), stone, mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.075f * s, 0.29f, 0.222f), V(0, 0, 1f), 0.034f, yellow));
        return b;
    }

    // ------------------------------------------------------------------ Machop line

    /// <summary>A champion's belt: gold round the waist, a buckle with a red stone in front.</summary>
    private static void Belt(PokeBuilder b, Vector3 waist, float r, float depth)
    {
        var gold = Rgb(236, 190, 64);
        b.PaintTorus(Body, waist, r, 0.022f, gold, default, 1f, depth / r);
        b.Ell(Body, waist + V(0, 0, depth * 0.98f), V(0.05f, 0.035f, 0.02f), gold, mat: Metal, blend: 0.006f);
        b.Ell(Body, waist + V(0, 0, depth * 0.98f + 0.016f), V(0.02f, 0.016f, 0.008f), Rgb(208, 46, 52), mat: Metal, blend: 0.004f);
    }

    private static PokeBuilder Machop()
    {
        var b = new PokeBuilder("Machop", 0.7f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Fur };
        var blue = Rgb(160, 198, 222);
        var crest = Rgb(176, 166, 156);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.28f, 0));
            b.Limb(leg, V(0.08f * s, 0.28f, 0), V(0.1f * s, 0.06f, 0.02f), 0.065f, 0.05f, blue);
            b.Ell(leg, V(0.1f * s, 0.03f, 0.05f), V(0.055f, 0.03f, 0.085f), blue);
        });
        b.Ell(Body, V(0, 0.4f, 0), V(0.13f, 0.15f, 0.11f), blue);
        // Ribs showing along its sides
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
                b.PaintEll(Body, V(0.11f * s, 0.43f - i * 0.04f, 0.04f), V(0.025f, 0.006f, 0.03f), PixelCanvas.Mix(blue, Rgb(60, 80, 110), 0.35f), V(0, 0, -20f * s), 0.008f);
        });
        // Fists up, ready to fight
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.48f, 0));
            b.Limb(arm, V(0.13f * s, 0.48f, 0), V(0.22f * s, 0.4f, 0.05f), 0.048f, 0.042f, blue);
            b.Limb(arm, V(0.22f * s, 0.4f, 0.05f), V(0.24f * s, 0.48f, 0.12f), 0.042f, 0.04f, blue);
            b.Ell(arm, V(0.245f * s, 0.51f, 0.14f), V(0.058f, 0.058f, 0.058f), blue);
        });

        int head = b.Head(V(0, 0.55f, 0));
        b.Ell(head, V(0, 0.66f, 0.01f), V(0.13f, 0.12f, 0.12f), blue);
        b.Ell(head, V(0, 0.61f, 0.08f), V(0.09f, 0.06f, 0.07f), blue);
        b.PaintEll(head, V(0, 0.6f, 0.13f), V(0.06f, 0.016f, 0.04f), Rgb(110, 40, 50));
        // Three bony ridges along its crown
        foreach (float x in new[] { -0.045f, 0f, 0.045f })
            b.Ell(head, V(x, 0.77f, -0.01f), V(0.02f, 0.06f, 0.075f), crest, V(-20f, 0, 0), Shell, 0.012f);
        PokeBuilder.Both(s => b.Eye(head, V(0.055f * s, 0.68f, 0.115f), V(0.4f * s, 0.05f, 1f), 0.03f, Rgb(214, 56, 56)));
        return b;
    }

    private static PokeBuilder Machoke()
    {
        var b = new PokeBuilder("Machoke", 0.88f, BodyPlan.Biped, V(0, 0.56f, 0)) { Coat = Fur };
        var gray = Rgb(178, 184, 206);
        var red = Rgb(210, 52, 56);
        var black = Rgb(38, 36, 44);
        var crest = Rgb(176, 150, 112);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.4f, 0));
            b.Limb(leg, V(0.1f * s, 0.4f, 0), V(0.14f * s, 0.2f, 0.04f), 0.085f, 0.065f, gray);
            b.Limb(leg, V(0.14f * s, 0.2f, 0.04f), V(0.13f * s, 0.05f, 0.0f), 0.06f, 0.05f, gray);
            b.Ell(leg, V(0.13f * s, 0.03f, 0.05f), V(0.06f, 0.03f, 0.09f), gray);
        });
        b.Ell(Body, V(0, 0.56f, 0), V(0.17f, 0.2f, 0.13f), gray);
        // Chest and shoulders, black trunks and the champion's belt
        PokeBuilder.Both(s => b.Ell(Body, V(0.07f * s, 0.64f, 0.06f), V(0.09f, 0.07f, 0.07f), gray));
        b.PaintEll(Body, V(0, 0.4f, 0), V(0.18f, 0.08f, 0.14f), black);
        Belt(b, V(0, 0.46f, 0), 0.165f, 0.13f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.7f, 0));
            b.Ell(arm, V(0.19f * s, 0.7f, 0), V(0.09f, 0.08f, 0.08f), gray);
            b.Limb(arm, V(0.2f * s, 0.67f, 0.01f), V(0.29f * s, 0.52f, 0.06f), 0.07f, 0.06f, gray);
            b.Limb(arm, V(0.29f * s, 0.52f, 0.06f), V(0.27f * s, 0.38f, 0.12f), 0.06f, 0.05f, gray);
            // Red stripes round the forearm
            for (int i = 0; i < 3; i++)
                b.PaintEll(arm, V((0.285f - i * 0.006f) * s, 0.49f - i * 0.04f, 0.08f + i * 0.015f), V(0.07f, 0.008f, 0.07f), red, V(-25f, 0, 10f * s), 0.008f);
            b.Ell(arm, V(0.265f * s, 0.34f, 0.13f), V(0.06f, 0.06f, 0.06f), gray);
        });

        int head = b.Head(V(0, 0.76f, 0));
        b.Ell(head, V(0, 0.86f, 0.0f), V(0.11f, 0.11f, 0.11f), gray);
        b.Ell(head, V(0, 0.82f, 0.08f), V(0.08f, 0.055f, 0.06f), gray);
        b.PaintEll(head, V(0, 0.81f, 0.13f), V(0.05f, 0.012f, 0.03f), Rgb(110, 40, 50));
        b.Ell(head, V(0, 0.97f, 0.0f), V(0.03f, 0.05f, 0.1f), crest, V(-25f, 0, 0), Shell, 0.015f);
        PokeBuilder.Both(s => b.Ell(head, V(0.05f * s, 0.9f, 0.09f), V(0.04f, 0.015f, 0.025f), gray, V(0, 0, -15f * s)));
        PokeBuilder.Both(s => b.Eye(head, V(0.048f * s, 0.88f, 0.1f), V(0.4f * s, 0.05f, 1f), 0.022f, red));
        return b;
    }

    private static PokeBuilder Machamp()
    {
        var b = new PokeBuilder("Machamp", 0.9f, BodyPlan.Biped, V(0, 0.56f, 0)) { Coat = Fur };
        var blue = Rgb(134, 172, 214);
        var black = Rgb(38, 36, 44);
        var crest = Rgb(176, 150, 112);
        var lips = Rgb(214, 70, 80);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.4f, 0));
            b.Limb(leg, V(0.11f * s, 0.4f, 0), V(0.16f * s, 0.2f, 0.04f), 0.09f, 0.07f, blue);
            b.Limb(leg, V(0.16f * s, 0.2f, 0.04f), V(0.15f * s, 0.05f, 0.0f), 0.065f, 0.055f, blue);
            b.Ell(leg, V(0.15f * s, 0.03f, 0.05f), V(0.065f, 0.03f, 0.095f), blue);
        });
        b.Ell(Body, V(0, 0.56f, 0), V(0.18f, 0.21f, 0.14f), blue);
        PokeBuilder.Both(s => b.Ell(Body, V(0.075f * s, 0.65f, 0.07f), V(0.09f, 0.07f, 0.07f), blue));
        b.PaintEll(Body, V(0, 0.4f, 0), V(0.19f, 0.08f, 0.15f), black);
        Belt(b, V(0, 0.46f, 0), 0.175f, 0.135f);
        // Four arms: the upper pair flexing, the lower pair low and wide
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.18f * s, 0.72f, 0));
            b.Ell(arm, V(0.2f * s, 0.73f, 0), V(0.09f, 0.08f, 0.08f), blue);
            b.Limb(arm, V(0.22f * s, 0.74f, 0.0f), V(0.36f * s, 0.78f, 0.02f), 0.068f, 0.058f, blue);
            b.Ell(arm, V(0.3f * s, 0.8f, 0.02f), V(0.07f, 0.06f, 0.06f), blue);
            b.Limb(arm, V(0.36f * s, 0.78f, 0.02f), V(0.37f * s, 0.94f, 0.05f), 0.058f, 0.05f, blue);
            b.Ell(arm, V(0.37f * s, 0.98f, 0.06f), V(0.06f, 0.06f, 0.06f), blue);
            int lower = b.Part(s < 0 ? "lowArmL" : "lowArmR", Body, V(0.17f * s, 0.6f, 0.02f), PokeRole.Arm, s + 1.4f, s);
            b.Limb(lower, V(0.17f * s, 0.6f, 0.02f), V(0.3f * s, 0.48f, 0.06f), 0.06f, 0.052f, blue);
            b.Limb(lower, V(0.3f * s, 0.48f, 0.06f), V(0.36f * s, 0.34f, 0.1f), 0.052f, 0.046f, blue);
            b.Ell(lower, V(0.37f * s, 0.3f, 0.11f), V(0.055f, 0.055f, 0.055f), blue);
        });

        int head = b.Head(V(0, 0.77f, 0));
        b.Ell(head, V(0, 0.87f, 0.0f), V(0.11f, 0.11f, 0.11f), blue);
        b.Ell(head, V(0, 0.82f, 0.08f), V(0.08f, 0.055f, 0.06f), blue);
        b.Ell(head, V(0, 0.81f, 0.13f), V(0.055f, 0.025f, 0.025f), lips, mat: Scales, blend: 0.008f);
        foreach (float x in new[] { -0.04f, 0f, 0.04f })
            b.Ell(head, V(x, 0.97f, -0.01f), V(0.018f, 0.05f, 0.08f), crest, V(-20f, 0, 0), Shell, 0.012f);
        PokeBuilder.Both(s => b.Eye(head, V(0.048f * s, 0.89f, 0.1f), V(0.4f * s, 0.05f, 1f), 0.022f, Rgb(214, 52, 56)));
        return b;
    }

    // ------------------------------------------------------------------ Psyduck line

    private static PokeBuilder Psyduck()
    {
        var b = new PokeBuilder("Psyduck", 0.7f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var yellow = Rgb(250, 210, 62);
        var cream = Rgb(250, 236, 190);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.14f, 0));
            b.Limb(leg, V(0.09f * s, 0.14f, 0), V(0.1f * s, 0.05f, 0.02f), 0.045f, 0.04f, yellow);
            b.Ell(leg, V(0.11f * s, 0.025f, 0.07f), V(0.07f, 0.022f, 0.1f), cream, V(0, 12f * s, 0));
        });
        b.Ell(Body, V(0, 0.3f, 0), V(0.17f, 0.2f, 0.16f), yellow);
        int tail = b.Tail(V(0, 0.18f, -0.14f));
        b.Spike(tail, V(0, 0.2f, -0.13f), V(0, 0.16f, -0.24f), 0.05f, yellow, 0.6f);
        // Clutching its aching head
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.38f, 0.02f));
            b.Limb(arm, V(0.15f * s, 0.38f, 0.02f), V(0.24f * s, 0.48f, 0.06f), 0.045f, 0.04f, yellow);
            b.Limb(arm, V(0.24f * s, 0.48f, 0.06f), V(0.2f * s, 0.6f, 0.08f), 0.04f, 0.038f, yellow);
            b.Ell(arm, V(0.19f * s, 0.62f, 0.08f), V(0.045f, 0.05f, 0.04f), yellow);
        });

        int head = b.Head(V(0, 0.48f, 0));
        b.Ell(head, V(0, 0.62f, 0.0f), V(0.19f, 0.17f, 0.17f), yellow);
        b.Ell(head, V(0, 0.545f, 0.19f), V(0.11f, 0.035f, 0.09f), cream, mat: Shell, blend: 0.012f);
        b.Ell(head, V(0, 0.52f, 0.17f), V(0.09f, 0.025f, 0.07f), cream, mat: Shell, blend: 0.01f);
        // Three black hairs
        foreach (float x in new[] { -0.035f, 0f, 0.035f })
            b.Tube(head, new[] { V(x, 0.77f, 0.0f), V(x * 1.4f, 0.84f, 0.01f), V(x * 2.2f, 0.89f, -0.01f) }, 0.014f, 0.01f, Rgb(40, 40, 44));
        PokeBuilder.Both(s => b.Eye(head, V(0.075f * s, 0.65f, 0.155f), V(0.4f * s, 0.05f, 1f), 0.048f, sclera: true));
        return b;
    }

    private static PokeBuilder Golduck()
    {
        var b = new PokeBuilder("Golduck", 0.9f, BodyPlan.Biped, V(0, 0.52f, 0)) { Coat = Scales };
        var blue = Rgb(92, 152, 222);
        var bill = Rgb(246, 222, 140);
        var red = Rgb(220, 56, 60);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.4f, 0));
            b.Limb(leg, V(0.1f * s, 0.4f, 0), V(0.15f * s, 0.22f, 0.06f), 0.07f, 0.05f, blue);
            b.Limb(leg, V(0.15f * s, 0.22f, 0.06f), V(0.14f * s, 0.05f, -0.02f), 0.045f, 0.035f, blue);
            b.Ell(leg, V(0.14f * s, 0.025f, 0.05f), V(0.06f, 0.025f, 0.1f), blue);
            Digits(b, leg, V(0.14f * s, 0.022f, 0.13f), V(0, 0, 1f), V(0.6f, 0, 0), 0.04f, 0.014f, Claw, Shell);
        });
        b.Ell(Body, V(0, 0.54f, 0), V(0.13f, 0.2f, 0.11f), blue);
        b.PaintEll(Body, V(0, 0.5f, 0.06f), V(0.09f, 0.12f, 0.08f), PixelCanvas.Light1(blue, 0.18f));
        int tail = b.Tail(V(0, 0.4f, -0.1f));
        b.Tube(tail, new[] { V(0, 0.4f, -0.1f), V(0, 0.28f, -0.26f), V(0, 0.2f, -0.38f) }, 0.06f, 0.025f, blue, blend: 0.02f);
        b.Spike(tail, V(0, 0.22f, -0.34f), V(0, 0.3f, -0.48f), 0.04f, blue, 0.4f);
        // Long arms with webbed, clawed hands
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.66f, 0));
            b.Limb(arm, V(0.12f * s, 0.66f, 0), V(0.25f * s, 0.56f, 0.06f), 0.04f, 0.035f, blue);
            b.Limb(arm, V(0.25f * s, 0.56f, 0.06f), V(0.32f * s, 0.62f, 0.16f), 0.035f, 0.03f, blue);
            b.Ell(arm, V(0.34f * s, 0.63f, 0.19f), V(0.05f, 0.022f, 0.05f), blue, V(0, 0, 20f * s));
            Digits(b, arm, V(0.36f * s, 0.63f, 0.21f), V(0.4f * s, 0, 1f), V(0.6f, 0, 0), 0.04f, 0.012f, Claw, Shell);
        });

        int head = b.Head(V(0, 0.72f, 0));
        b.Ell(head, V(0, 0.82f, 0.0f), V(0.11f, 0.1f, 0.11f), blue);
        b.Ell(head, V(0, 0.77f, 0.15f), V(0.06f, 0.03f, 0.11f), bill, mat: Shell, blend: 0.012f);
        b.Ell(head, V(0, 0.745f, 0.13f), V(0.05f, 0.02f, 0.08f), bill, mat: Shell, blend: 0.008f);
        b.Ell(head, V(0, 0.895f, 0.075f), V(0.028f, 0.028f, 0.02f), red, mat: Glow, blend: 0.006f);
        // A crest of blue spikes swept back from its head
        foreach (var (tip, r) in new[] { (V(0, 0.98f, -0.14f), 0.045f), (V(0.08f, 0.92f, -0.16f), 0.04f), (V(-0.08f, 0.92f, -0.16f), 0.04f) })
            b.Spike(head, V(tip.X * 0.4f, 0.86f, -0.04f), tip, r, blue, 0.5f);
        PokeBuilder.Both(s => b.Spike(head, V(0.09f * s, 0.8f, -0.02f), V(0.19f * s, 0.84f, -0.1f), 0.035f, blue, 0.5f));
        PokeBuilder.Both(s => b.Eye(head, V(0.055f * s, 0.84f, 0.09f), V(0.5f * s, 0.05f, 1f), 0.026f, red));
        return b;
    }

    // ------------------------------------------------------------------ Burmy line

    /// <summary>A cloak of leaves: rows of leaves hanging round an ellipsoid, each a little out from the one above.</summary>
    private static void LeafCloak(PokeBuilder b, int bone, Vector3 center, Vector3 radii, Color light, Color dark, int rows, int perRow, float leaf)
    {
        for (int r = 0; r < rows; r++)
        {
            float y = center.Y + radii.Y * (0.7f - 1.5f * r / Math.Max(1, rows - 1));
            float ring = MathF.Sqrt(Math.Max(0.15f, 1f - MathF.Pow((y - center.Y) / radii.Y, 2f)));
            for (int i = 0; i < perRow; i++)
            {
                float a = (i + (r % 2) * 0.5f) * MathF.Tau / perRow;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                var top = center + V(dir.X * radii.X * ring * 0.85f, y - center.Y, dir.Z * radii.Z * ring * 0.85f);
                var tip = top + dir * leaf * 0.45f + V(0, -leaf * 1.3f, 0);
                Petal(b, bone, top, tip, leaf * 0.55f, (i + r) % 3 == 0 ? dark : light, Leaf, 0.01f);
            }
        }
    }

    private static PokeBuilder Burmy()
    {
        var b = new PokeBuilder("Burmy", 0.46f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Leaf }.Hover();
        var green = Rgb(150, 210, 70);
        var dark = Rgb(96, 170, 60);
        var gray = Rgb(98, 100, 112);

        // A bell of leaves it has wrapped round itself, two twigs poking out below
        b.Ell(Body, V(0, 0.3f, 0), V(0.15f, 0.19f, 0.13f), green);
        LeafCloak(b, Body, V(0, 0.3f, 0), V(0.15f, 0.19f, 0.13f), green, dark, 4, 8, 0.1f);
        b.Limb(Body, V(-0.12f, 0.06f, 0.0f), V(0.04f, 0.16f, 0.02f), 0.014f, 0.012f, Rgb(176, 120, 70), Shell);
        b.Limb(Body, V(0.12f, 0.06f, -0.02f), V(-0.04f, 0.16f, 0.0f), 0.014f, 0.012f, Rgb(176, 120, 70), Shell);

        int head = b.Head(V(0, 0.44f, 0.02f));
        b.Ell(head, V(0, 0.47f, 0.06f), V(0.085f, 0.07f, 0.075f), gray, mat: Shell);
        // The spiral shell of its hood
        for (int i = 0; i < 4; i++)
            b.Ell(head, V(0, 0.54f + i * 0.05f, 0.03f - i * 0.006f), V(0.07f - i * 0.013f, 0.032f, 0.07f - i * 0.013f), PixelCanvas.Mix(gray, White, 0.12f * (i % 2)), mat: Shell, blend: 0.012f);
        b.Spike(head, V(0, 0.7f, 0.01f), V(0, 0.76f, 0.0f), 0.02f, gray, mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.038f * s, 0.475f, 0.128f), V(0.35f * s, 0, 1f), 0.022f, Rgb(250, 220, 60)));
        return Lift(b);
    }

    private static PokeBuilder Wormadam()
    {
        var b = new PokeBuilder("Wormadam", 0.58f, BodyPlan.Floating, V(0, 0.28f, 0)) { Coat = Leaf }.Hover();
        var green = Rgb(132, 204, 70);
        var dark = Rgb(48, 150, 90);
        var gray = Rgb(98, 100, 112);

        // A gown of leaves with white flowers on it, a ragged hem
        b.Ell(Body, V(0, 0.28f, 0), V(0.17f, 0.18f, 0.14f), green);
        b.Ell(Body, V(0, 0.42f, 0.01f), V(0.1f, 0.07f, 0.08f), green);
        LeafCloak(b, Body, V(0, 0.24f, 0), V(0.17f, 0.14f, 0.14f), green, Rgb(110, 186, 60), 2, 9, 0.11f);
        foreach (var (x, y) in new[] { (-0.07f, 0.34f), (0.07f, 0.34f), (0f, 0.24f) })
        {
            var at = V(x, y, 0.135f);
            b.Mark(Body, at, V(x * 3f, 0, 1f), 0.05f, 0.05f, White, MarkShape.Star5);
            b.Mark(Body, at + V(0, 0, 0.002f), V(x * 3f, 0, 1f), 0.016f, 0.016f, Rgb(250, 220, 80));
        }

        int head = b.Head(V(0, 0.46f, 0.02f));
        b.Ell(head, V(0, 0.52f, 0.04f), V(0.08f, 0.065f, 0.07f), gray, mat: Shell);
        // A broad leaf hat, and a horn on top tied in a knot
        PokeBuilder.Both(s => Petal(b, head, V(0.03f * s, 0.57f, 0.0f), V(0.3f * s, 0.6f, -0.05f), 0.08f, dark, Leaf, 0.015f));
        b.Tube(head, new[] { V(0, 0.57f, 0.03f), V(0, 0.68f, 0.02f), V(0.02f, 0.74f, 0.01f), V(0, 0.79f, 0.0f), V(0, 0.86f, -0.01f) }, 0.024f, 0.012f, gray, Shell);
        b.Torus(head, V(0.0f, 0.73f, 0.015f), 0.025f, 0.01f, gray, V(90f, 0, 0), mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, V(0.036f * s, 0.52f, 0.105f), V(0.35f * s, 0, 1f), 0.02f, Rgb(250, 220, 60)));
        return Lift(b);
    }

    /// <summary>A flat wing in the plane facing forward: a thin oval of <paramref name="fill"/> rimmed in <paramref name="rim"/>, its long axis turned by <paramref name="roll"/> degrees.</summary>
    private static void FlatWing(PokeBuilder b, int bone, Vector3 center, float length, float width, float roll, Color fill, Color rim, float border = 0.02f) =>
        RimmedFin(b, bone, center, V(length, width, 0.011f), V(0, 0, roll), fill, rim, border);

    private static PokeBuilder Mothim()
    {
        var b = new PokeBuilder("Mothim", 0.72f, BodyPlan.Bird, V(0, 0.45f, 0)) { Coat = Fur }.Hover();
        var wing = Rgb(252, 222, 128);
        var orange = Rgb(244, 140, 42);
        var gray = Rgb(78, 78, 90);

        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.05f * s, 0.48f, -0.02f));
            FlatWing(b, w, V(0.27f * s, 0.55f, -0.03f), 0.23f, 0.11f, 14f * s, wing, PixelCanvas.Mix(wing, orange, 0.5f), 0.016f);
            // Orange patches at the wing's tip
            foreach (var (x, y) in new[] { (0.46f, 0.66f), (0.5f, 0.57f), (0.43f, 0.48f) })
                b.Box(w, V(x * s, y, -0.03f), V(0.022f, 0.016f, 0.016f), 0.006f, orange, V(0, 0, 14f * s), blend: 0.004f);
            FlatWing(b, w, V(0.16f * s, 0.36f, -0.035f), 0.11f, 0.07f, -38f * s, wing, PixelCanvas.Mix(wing, orange, 0.5f), 0.014f);
            b.Box(w, V(0.24f * s, 0.27f, -0.035f), V(0.02f, 0.015f, 0.016f), 0.006f, orange, V(0, 0, -38f * s), blend: 0.004f);
        });
        // A fluffy white collar on a dark body
        b.Ell(Body, V(0, 0.34f, -0.01f), V(0.045f, 0.08f, 0.045f), gray);
        b.Ell(Body, V(0, 0.46f, 0), V(0.075f, 0.065f, 0.065f), White);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.38f, 0.03f));
            b.Limb(leg, V(0.03f * s, 0.38f, 0.03f), V(0.05f * s, 0.27f, 0.06f), 0.012f, 0.01f, gray);
        });

        int head = b.Head(V(0, 0.5f, 0.01f));
        b.Ell(head, V(0, 0.56f, 0.02f), V(0.065f, 0.06f, 0.06f), orange);
        b.PaintEll(head, V(0, 0.61f, 0.0f), V(0.07f, 0.035f, 0.07f), gray);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.025f * s, 0.6f, 0.02f));
            b.Tube(ear, new[] { V(0.025f * s, 0.6f, 0.02f), V(0.05f * s, 0.67f, 0.03f), V(0.09f * s, 0.7f, 0.03f) }, 0.012f, 0.01f, gray);
            b.Ell(ear, V(0.1f * s, 0.7f, 0.03f), V(0.022f, 0.016f, 0.012f), orange);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.03f * s, 0.565f, 0.07f), V(0.5f * s, 0.05f, 1f), 0.02f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Wurmple line

    private static PokeBuilder Wurmple()
    {
        var path = new[] { V(0, 0.07f, -0.24f), V(0, 0.08f, -0.14f), V(0, 0.1f, -0.04f), V(0, 0.16f, 0.04f), V(0, 0.25f, 0.08f) };
        var b = new PokeBuilder("Wurmple", 0.5f, BodyPlan.Serpent, path[0]) { Coat = Fur };
        var red = Rgb(222, 64, 88);
        var cream = Rgb(240, 232, 194);
        var yellow = Rgb(250, 226, 120);

        // Ringed red segments over a pale belly, little white feet, the front half raised
        int parent = Body;
        for (int i = 0; i < path.Length; i++)
        {
            int seg = i == 0 ? Body : b.Part("seg" + i, parent, path[i], PokeRole.Segment, i);
            float r = 0.07f + 0.006f * i;
            b.Ell(seg, path[i], V(r, r * 0.95f, r), red);
            b.PaintEll(seg, path[i] + V(0, -r * 0.55f, r * 0.35f), V(r * 0.9f, r * 0.55f, r * 0.8f), cream);
            b.Spike(seg, path[i] + V(0, r * 0.7f, -r * 0.2f), path[i] + V(0, r * 1.35f, -r * 0.55f), r * 0.32f, red, 0.5f);
            if (i < 3) PokeBuilder.Both(s => b.Ell(seg, path[i] + V(r * 0.6f * s, -r * 0.85f, 0.01f), V(0.02f, 0.02f, 0.02f), White, mat: Shell, blend: 0.008f));
            parent = seg;
        }
        int tail = b.Tail(path[0]);
        PokeBuilder.Both(s => b.Spike(tail, path[0] + V(0.03f * s, 0.04f, -0.03f), path[0] + V(0.06f * s, 0.16f, -0.12f), 0.025f, yellow, mat: Shell));

        int head = b.Head(path[^1] + V(0, 0.04f, 0.0f), parent);
        b.Ell(head, V(0, 0.36f, 0.1f), V(0.095f, 0.09f, 0.085f), cream);
        b.PaintEll(head, V(0, 0.42f, 0.04f), V(0.1f, 0.06f, 0.08f), red);
        // The red horn that oozes poison
        b.Tube(head, new[] { V(0, 0.42f, 0.08f), V(0, 0.5f, 0.06f), V(0, 0.57f, 0.02f), V(0.0f, 0.62f, -0.04f) }, 0.035f, 0.012f, red);
        PokeBuilder.Both(s => b.Spike(head, V(0.06f * s, 0.43f, 0.06f), V(0.1f * s, 0.5f, 0.03f), 0.022f, yellow, mat: Shell));
        PokeBuilder.Both(s => b.Eye(head, V(0.055f * s, 0.375f, 0.168f), V(0.6f * s, 0.1f, 0.8f), 0.032f, Rgb(250, 222, 70)));
        return b;
    }

    private static PokeBuilder Cocoon(bool purple)
    {
        var b = new PokeBuilder(purple ? "Cascoon" : "Silcoon", purple ? 0.64f : 0.62f, BodyPlan.Floating, V(0, 0.32f, 0)) { Coat = Fur }.Hover();
        var silk = purple ? Rgb(214, 186, 226) : Rgb(236, 236, 242);
        var thread = PixelCanvas.Mix(silk, Rgb(90, 80, 110), 0.25f);

        // A cocoon of silk wound round and round, spikes poking out, an eye glaring from a slit
        b.Ell(Body, V(0, 0.32f, 0), V(0.25f, 0.17f, 0.17f), silk);
        foreach (float x in new[] { -0.15f, -0.05f, 0.05f, 0.15f })
            b.PaintTorus(Body, V(x, 0.32f, 0), 0.165f * MathF.Sqrt(1f - x * x / 0.07f), 0.006f, thread, V(0, 0, 90f));
        foreach (var dir in new[] { V(0.4f, 1f, 0.2f), V(-0.5f, 1f, -0.1f), V(1f, 0.1f, 0.4f), V(-1f, 0.2f, 0.3f), V(0.6f, -0.6f, -0.5f), V(-0.5f, -0.7f, 0.4f), V(0.1f, 0.5f, -1f) })
        {
            var d = Vector3.Normalize(dir);
            var on = V(0, 0.32f, 0) + d * V(0.24f, 0.16f, 0.16f);
            b.Spike(Body, on - d * 0.02f, on + d * 0.13f, 0.03f, silk, mat: Shell);
        }
        PokeBuilder.Both(s =>
        {
            var at = V(0.13f * s, 0.33f, 0.135f);
            b.PaintEll(Body, at, V(0.05f, 0.035f, 0.04f), purple ? Rgb(150, 110, 170) : Rgb(90, 70, 80));
            b.Eye(Body, at + V(0, 0, 0.004f), V(0.6f * s, 0.05f, 0.8f), 0.026f, Rgb(220, 50, 70));
        });
        return Lift(b);
    }

    private static PokeBuilder Beautifly()
    {
        var b = new PokeBuilder("Beautifly", 0.78f, BodyPlan.Bird, V(0, 0.44f, 0)) { Coat = Fur }.Hover();
        var yellow = Rgb(250, 202, 46);
        var gray = Rgb(96, 96, 108);
        var red = Rgb(222, 50, 70);
        var blue = Rgb(56, 146, 222);

        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.04f * s, 0.48f, -0.02f));
            FlatWing(b, w, V(0.25f * s, 0.6f, -0.03f), 0.22f, 0.12f, 24f * s, yellow, gray, 0.024f);
            b.Mark(w, V(0.33f * s, 0.69f, -0.016f), V(0, 0, 1f), 0.035f, 0.022f, red, MarkShape.Disc, 24f * s);
            b.Mark(w, V(0.2f * s, 0.6f, -0.016f), V(0, 0, 1f), 0.03f, 0.02f, red, MarkShape.Disc, 24f * s);
            FlatWing(b, w, V(0.15f * s, 0.33f, -0.035f), 0.13f, 0.08f, -50f * s, gray, PixelCanvas.Mix(gray, Black, 0.3f), 0.016f);
            b.Mark(w, V(0.13f * s, 0.36f, -0.02f), V(0, 0, 1f), 0.028f, 0.02f, red, MarkShape.Disc, -50f * s);
            b.Mark(w, V(0.2f * s, 0.28f, -0.02f), V(0, 0, 1f), 0.022f, 0.016f, blue, MarkShape.Disc, -50f * s);
        });
        b.Ell(Body, V(0, 0.38f, -0.01f), V(0.045f, 0.1f, 0.045f), gray);
        b.Ell(Body, V(0, 0.48f, 0), V(0.06f, 0.055f, 0.055f), Rgb(230, 230, 236));
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.44f, 0.04f));
            b.Limb(leg, V(0.03f * s, 0.44f, 0.04f), V(0.05f * s, 0.36f, 0.08f), 0.014f, 0.012f, gray);
        });

        int head = b.Head(V(0, 0.52f, 0.02f));
        b.Ell(head, V(0, 0.57f, 0.03f), V(0.065f, 0.062f, 0.062f), Rgb(232, 232, 238));
        // A long coiled tongue, and antennae curling at the tips
        b.Tube(head, new[] { V(0, 0.53f, 0.08f), V(0, 0.48f, 0.1f) }.Concat(Spiral(V(0, 0.44f, 0.1f), V(0, 1, 0), V(0, 0, 1f), 0.04f, 0.012f, 0f, 1.6f * MathF.PI, 10)).ToArray(),
            0.009f, 0.007f, gray);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.02f * s, 0.62f, 0.03f));
            var curl = Spiral(V(0.16f * s, 0.79f, 0.0f), V(-s, 0, 0), V(0, 1, 0), 0.025f, 0.01f, 0f, 1.5f * MathF.PI, 8);
            b.Tube(ear, new[] { V(0.02f * s, 0.62f, 0.03f), V(0.07f * s, 0.7f, 0.02f), V(0.13f * s, 0.77f, 0.0f) }.Concat(curl).ToArray(), 0.01f, 0.007f, gray);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.038f * s, 0.575f, 0.078f), V(0.7f * s, 0.05f, 0.7f), 0.032f, blue));
        return Lift(b);
    }

    private static PokeBuilder Dustox()
    {
        var b = new PokeBuilder("Dustox", 0.82f, BodyPlan.Bird, V(0, 0.4f, 0)) { Coat = Fur }.Hover();
        var green = Rgb(150, 216, 92);
        var rim = Rgb(56, 150, 70);
        var purple = Rgb(176, 150, 224);
        var pink = Rgb(226, 90, 120);
        var yellow = Rgb(250, 224, 90);

        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.05f * s, 0.42f, -0.02f));
            FlatWing(b, w, V(0.28f * s, 0.5f, -0.03f), 0.26f, 0.15f, 12f * s, green, rim, 0.026f);
            b.Mark(w, V(0.32f * s, 0.54f, -0.016f), V(0, 0, 1f), 0.06f, 0.04f, pink, MarkShape.Ring, 12f * s);
            FlatWing(b, w, V(0.18f * s, 0.32f, -0.035f), 0.15f, 0.09f, -30f * s, green, rim, 0.022f);
            b.Mark(w, V(0.2f * s, 0.3f, -0.02f), V(0, 0, 1f), 0.04f, 0.028f, pink, MarkShape.Ring, -30f * s);
        });
        b.Ell(Body, V(0, 0.37f, -0.01f), V(0.075f, 0.11f, 0.07f), purple);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.3f, 0.05f));
            b.Limb(leg, V(0.04f * s, 0.3f, 0.05f), V(0.06f * s, 0.23f, 0.08f), 0.018f, 0.016f, purple);
            b.Ell(leg, V(0.06f * s, 0.22f, 0.09f), V(0.022f, 0.016f, 0.022f), pink);
        });

        int head = b.Head(V(0, 0.46f, 0.02f));
        b.Ell(head, V(0, 0.51f, 0.04f), V(0.08f, 0.07f, 0.07f), purple);
        b.Mark(head, V(0, 0.47f, 0.105f), V(0, -0.3f, 1f), 0.035f, 0.012f, Rgb(40, 30, 50), MarkShape.Wave);
        // Feathery yellow antennae
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.03f * s, 0.57f, 0.03f));
            for (int i = 0; i < 4; i++)
                b.Ell(ear, V((0.035f + i * 0.02f) * s, 0.59f + i * 0.04f, 0.03f), V(0.022f - i * 0.002f, 0.022f, 0.022f - i * 0.002f), yellow, blend: 0.012f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.04f * s, 0.52f, 0.1f), V(0.5f * s, 0.05f, 1f), 0.024f, yellow));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Combee line

    /// <summary>
    /// A cell of honeycomb: a hexagonal prism <paramref name="depth"/> deep along <paramref name="normal"/>, made of
    /// three rounded boxes, each joining a pair of opposite sides; <paramref name="roll"/> 30 stands it on a point.
    /// </summary>
    private static void HexCell(PokeBuilder b, int bone, Vector3 center, Vector3 normal, float r, float depth, Color color, float roll = 30f, float blend = 0.006f)
    {
        var n = Vector3.Normalize(normal);
        float pitch = -MathF.Asin(Math.Clamp(n.Y, -1f, 1f)) * 180f / MathF.PI;
        float yaw = MathF.Atan2(n.X, n.Z) * 180f / MathF.PI;
        for (int k = 0; k < 3; k++)
            b.Box(bone, center, V(r * 0.5f, r * 0.866f, depth), r * 0.14f, color, V(pitch, yaw, roll + 60f * k), Shell, k == 0 ? blend : 0f);
    }

    private static PokeBuilder Combee()
    {
        var b = new PokeBuilder("Combee", 0.5f, BodyPlan.Bird, V(0, 0.34f, 0)) { Coat = Shell }.Hover();
        var honey = Rgb(250, 182, 44);
        var face = Rgb(252, 214, 84);
        var wing = Rgb(236, 240, 248);

        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.15f * s, 0.38f, -0.04f));
            Petal(b, w, V(0.15f * s, 0.38f, -0.05f), V(0.42f * s, 0.5f, -0.08f), 0.075f, wing, Shell, 0.01f);
            Petal(b, w, V(0.15f * s, 0.34f, -0.05f), V(0.36f * s, 0.28f, -0.08f), 0.055f, wing, Shell, 0.01f);
        });
        // Three cells of honeycomb, each with a face, and a little striped sting behind: the upper two are the
        // head that leads, the lower one rides on the body
        int head = b.Head(V(0, 0.36f, 0));
        var cells = new[] { (head, V(-0.087f, 0.42f, 0f)), (head, V(0.087f, 0.42f, 0f)), (Body, V(0, 0.27f, 0.01f)) };
        foreach (var (bone, at) in cells)
        {
            HexCell(b, bone, at, V(0, 0, 1f), 0.1f, 0.075f, honey);
            b.PaintEll(bone, at + V(0, 0, 0.075f), V(0.085f, 0.085f, 0.02f), face);
        }
        b.Ell(Body, V(0.06f, 0.24f, -0.09f), V(0.06f, 0.055f, 0.07f), honey);
        b.PaintTorus(Body, V(0.06f, 0.24f, -0.1f), 0.058f, 0.012f, Rgb(50, 40, 40), V(90f, 0, 0));
        b.Spike(Body, V(0.06f, 0.24f, -0.15f), V(0.07f, 0.22f, -0.21f), 0.02f, Rgb(50, 40, 40));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.087f * s, 0.5f, 0.0f));
            b.Tube(ear, new[] { V(0.087f * s, 0.5f, 0.0f), V(0.09f * s, 0.57f, 0.01f), V(0.075f * s, 0.62f, 0.02f) }, 0.009f, 0.008f, Rgb(40, 36, 40));
            b.Ell(ear, V(0.073f * s, 0.625f, 0.02f), V(0.014f, 0.014f, 0.014f), Rgb(40, 36, 40));
        });
        foreach (var (bone, at) in cells)
        {
            var front = at + V(0, 0, 0.076f);
            PokeBuilder.Both(s => b.Eye(bone, front + V(0.026f * s, 0.012f, 0), V(0, 0, 1f), 0.016f));
            b.Mark(bone, front + V(0, -0.03f, 0), V(0, 0, 1f), 0.02f, 0.008f, Rgb(70, 40, 30), MarkShape.Bar);
        }
        return Lift(b);
    }

    private static PokeBuilder Vespiquen()
    {
        var b = new PokeBuilder("Vespiquen", 0.82f, BodyPlan.Floating, V(0, 0.34f, 0)) { Coat = Shell }.Hover();
        var honey = Rgb(250, 176, 40);
        var dark = Rgb(66, 64, 74);
        var face = Rgb(250, 196, 60);
        var red = Rgb(214, 46, 50);

        // A gown that is its hive: dark bands above, a skirt of honeycomb below
        b.Ell(Body, V(0, 0.32f, 0), V(0.25f, 0.29f, 0.22f), honey);
        b.PaintTorus(Body, V(0, 0.47f, 0), 0.2f, 0.03f, dark, default, 1f, 0.88f);
        b.PaintTorus(Body, V(0, 0.36f, 0), 0.245f, 0.03f, dark, default, 1f, 0.9f);
        for (int row = 0; row < 2; row++)
            for (int i = 0; i < 10; i++)
            {
                float a = (i + row * 0.5f) * MathF.Tau / 10f;
                float y = 0.2f - row * 0.1f;
                float k = MathF.Sqrt(Math.Max(0.1f, 1f - MathF.Pow((y - 0.32f) / 0.29f, 2f)));
                var n = V(MathF.Sin(a), 0, MathF.Cos(a));
                HexCell(b, Body, V(n.X * 0.24f * k, y, n.Z * 0.21f * k), n + V(0, -0.2f, 0), 0.05f, 0.03f, PixelCanvas.Mix(honey, Rgb(220, 120, 20), 0.35f), 30f, 0.004f);
            }
        b.Ell(Body, V(0, 0.62f, 0), V(0.085f, 0.06f, 0.075f), dark);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.06f * s, 0.66f, -0.06f));
            b.Ell(w, V(0.05f * s, 0.65f, -0.055f), V(0.03f, 0.035f, 0.025f), dark);
            Petal(b, w, V(0.06f * s, 0.67f, -0.07f), V(0.27f * s, 0.8f, -0.12f), 0.07f, Rgb(236, 240, 248), Shell, 0.01f);
            Petal(b, w, V(0.06f * s, 0.63f, -0.07f), V(0.22f * s, 0.6f, -0.12f), 0.05f, Rgb(236, 240, 248), Shell, 0.01f);
        });
        // Thin dark arms ending in orange claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.62f, 0.02f));
            b.Limb(arm, V(0.07f * s, 0.62f, 0.02f), V(0.2f * s, 0.6f, 0.06f), 0.022f, 0.02f, dark);
            b.Limb(arm, V(0.2f * s, 0.6f, 0.06f), V(0.26f * s, 0.66f, 0.1f), 0.02f, 0.018f, dark);
            Digits(b, arm, V(0.27f * s, 0.67f, 0.11f), V(0.5f * s, 0.6f, 0.5f), V(0.5f, 0, 0), 0.035f, 0.012f, honey);
        });

        int head = b.Head(V(0, 0.68f, 0.01f));
        b.Ell(head, V(0, 0.76f, 0.02f), V(0.1f, 0.09f, 0.09f), honey);
        b.PaintEll(head, V(0, 0.75f, 0.08f), V(0.075f, 0.06f, 0.05f), face);
        // A red jewel on its brow, and the crown of its head
        b.Spike(head, V(0, 0.8f, 0.09f), V(0, 0.86f, 0.1f), 0.025f, red, 0.5f, Glow);
        b.Spike(head, V(0, 0.8f, 0.09f), V(0, 0.74f, 0.105f), 0.022f, red, 0.5f, Glow);
        b.Ell(head, V(0, 0.84f, 0.0f), V(0.07f, 0.03f, 0.06f), dark);
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, 0.77f, 0.095f), V(0.45f * s, 0.05f, 1f), 0.022f, red));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Pachirisu

    private static PokeBuilder Pachirisu()
    {
        var b = new PokeBuilder("Pachirisu", 0.56f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var white = Rgb(246, 246, 250);
        var blue = Rgb(110, 200, 236);
        var yellow = Rgb(252, 222, 70);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.12f, 0));
            b.Limb(leg, V(0.06f * s, 0.12f, 0), V(0.065f * s, 0.04f, 0.02f), 0.04f, 0.035f, white);
            b.Ell(leg, V(0.065f * s, 0.022f, 0.045f), V(0.035f, 0.022f, 0.055f), white);
        });
        b.Ell(Body, V(0, 0.22f, 0), V(0.095f, 0.12f, 0.085f), white);
        // A great fluffy tail curled up behind, blue along its spine and its spiky edge
        int tail = b.Tail(V(0, 0.16f, -0.07f));
        var tailPath = new[] { V(0, 0.15f, -0.08f), V(0, 0.24f, -0.2f), V(0.01f, 0.4f, -0.24f), V(0.02f, 0.54f, -0.18f), V(0.02f, 0.6f, -0.08f) };
        b.Tube(tail, tailPath, 0.07f, 0.1f, white, blend: 0.03f);
        for (int i = 1; i < tailPath.Length; i++)
            b.PaintEll(tail, tailPath[i] + V(0, 0.035f, -0.06f), V(0.035f, 0.07f, 0.07f), blue);
        foreach (var (at, tip) in new[] { (V(0, 0.58f, -0.24f), V(0, 0.68f, -0.32f)), (V(0, 0.66f, -0.14f), V(0, 0.78f, -0.18f)), (V(0, 0.46f, -0.3f), V(0, 0.52f, -0.42f)), (V(0, 0.66f, -0.04f), V(0.02f, 0.76f, 0.0f)) })
            b.Spike(tail, at, tip, 0.05f, blue, 0.5f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.27f, 0.02f));
            b.Limb(arm, V(0.07f * s, 0.27f, 0.02f), V(0.09f * s, 0.21f, 0.08f), 0.028f, 0.025f, white);
        });

        int head = b.Head(V(0, 0.32f, 0));
        b.Ell(head, V(0, 0.42f, 0.02f), V(0.12f, 0.1f, 0.1f), white);
        // The blue stripe from its brow down its back, yellow cheeks that spark
        b.PaintEll(head, V(0, 0.47f, 0.0f), V(0.025f, 0.08f, 0.12f), blue);
        b.PaintEll(Body, V(0, 0.26f, -0.07f), V(0.025f, 0.1f, 0.04f), blue);
        PokeBuilder.Both(s => b.PaintEll(head, V(0.088f * s, 0.39f, 0.07f), V(0.036f, 0.028f, 0.035f), yellow));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, 0.5f, 0));
            b.Spike(ear, V(0.07f * s, 0.49f, 0), V(0.16f * s, 0.62f, -0.03f), 0.05f, white, 0.5f);
            b.PaintEll(ear, V(0.15f * s, 0.6f, -0.025f), V(0.04f, 0.05f, 0.04f), blue);
        });
        b.Mark(head, V(0, 0.4f, 0.12f), V(0, 0.1f, 1f), 0.012f, 0.009f, Rgb(60, 50, 60));
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, 0.43f, 0.105f), V(0.4f * s, 0.05f, 1f), 0.028f));
        return b;
    }

    // ------------------------------------------------------------------ Buizel line

    private static PokeBuilder Buizel()
    {
        var b = new PokeBuilder("Buizel", 0.64f, BodyPlan.Quadruped, V(0, 0.22f, 0)) { Coat = Fur };
        var orange = Rgb(242, 140, 52);
        var cream = Rgb(250, 230, 170);
        var yellow = Rgb(252, 216, 86);
        var blue = Rgb(84, 160, 220);

        // A sleek weasel on all fours, fins on its forelegs
        foreach (var (z, front) in new[] { (0.12f, true), (-0.13f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s, 0.18f, z), front);
                b.Limb(leg, V(0.07f * s, 0.18f, z), V(0.075f * s, 0.04f, z + 0.02f), 0.04f, 0.033f, orange);
                b.Ell(leg, V(0.075f * s, 0.022f, z + 0.045f), V(0.035f, 0.022f, 0.05f), orange);
                if (front) b.Spike(leg, V(0.1f * s, 0.12f, z - 0.01f), V(0.16f * s, 0.16f, z - 0.1f), 0.04f, blue, 0.3f);
            });
        b.Ell(Body, V(0, 0.22f, 0), V(0.1f, 0.1f, 0.2f), orange);
        b.PaintEll(Body, V(0, 0.15f, 0.04f), V(0.08f, 0.05f, 0.15f), cream);
        // The float round its neck
        b.Torus(Body, V(0, 0.26f, 0.14f), 0.085f, 0.035f, yellow, V(65f, 0, 0), mat: Shell, blend: 0.015f);
        int tail = b.Tail(V(0, 0.24f, -0.18f));
        PokeBuilder.Both(s =>
        {
            var tip = V(0.09f * s, 0.42f, -0.34f);
            b.Tube(tail, new[] { V(0, 0.24f, -0.18f), V(0.04f * s, 0.3f, -0.28f), tip }, 0.04f, 0.03f, orange, blend: 0.02f);
            b.Spike(tail, tip, tip + V(0.03f * s, 0.08f, -0.02f), 0.03f, cream, 0.6f);
        });

        int head = b.Head(V(0, 0.28f, 0.18f));
        b.Ell(head, V(0, 0.33f, 0.24f), V(0.085f, 0.075f, 0.085f), orange);
        // A pointed cream snout with whiskers, a yellow spot over each eye
        b.Ell(head, V(0, 0.3f, 0.32f), V(0.045f, 0.035f, 0.065f), cream);
        b.Ell(head, V(0, 0.31f, 0.385f), V(0.014f, 0.012f, 0.011f), Rgb(50, 36, 30), blend: 0.005f);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 2; i++)
                b.Mark(head, V(0.035f * s, 0.295f - i * 0.014f, 0.345f), V(0.6f * s, 0, 1f), 0.016f, 0.0035f, Rgb(70, 50, 40), MarkShape.Bar);
            b.Mark(head, V(0.04f * s, 0.385f, 0.285f), V(0.3f * s, 1f, 0.6f), 0.022f, 0.016f, yellow);
            int ear = b.Ear(head, s, V(0.06f * s, 0.38f, 0.21f));
            b.Spike(ear, V(0.06f * s, 0.37f, 0.21f), V(0.09f * s, 0.43f, 0.19f), 0.025f, orange, 0.6f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, 0.34f, 0.31f), V(0.6f * s, 0.1f, 0.8f), 0.02f));
        return b;
    }

    private static PokeBuilder Floatzel()
    {
        var b = new PokeBuilder("Floatzel", 0.8f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var orange = Rgb(242, 140, 52);
        var cream = Rgb(250, 232, 176);
        var yellow = Rgb(252, 214, 70);
        var blue = Rgb(84, 160, 220);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.26f, 0));
            b.Limb(leg, V(0.08f * s, 0.26f, 0), V(0.09f * s, 0.05f, 0.02f), 0.06f, 0.045f, orange);
            b.Ell(leg, V(0.095f * s, 0.025f, 0.06f), V(0.05f, 0.025f, 0.08f), orange);
        });
        b.Ell(Body, V(0, 0.42f, 0), V(0.12f, 0.2f, 0.1f), orange);
        b.PaintEll(Body, V(0, 0.4f, 0.07f), V(0.085f, 0.17f, 0.06f), cream);
        b.Mark(Body, V(0, 0.36f, 0.098f), V(0, 0, 1f), 0.025f, 0.04f, orange);
        // The float: round its neck and down its back like a scarf
        b.Torus(Body, V(0, 0.6f, -0.01f), 0.085f, 0.048f, yellow, V(-10f, 0, 0), mat: Shell, blend: 0.015f);
        b.Tube(Body, new[] { V(0, 0.6f, -0.1f), V(0, 0.48f, -0.14f), V(0, 0.34f, -0.13f), V(0, 0.24f, -0.1f) }, 0.05f, 0.04f, yellow, Shell, 0.015f);
        int tail = b.Tail(V(0, 0.26f, -0.1f));
        PokeBuilder.Both(s =>
        {
            var tip = V(0.18f * s, 0.2f, -0.32f);
            b.Tube(tail, new[] { V(0, 0.26f, -0.1f), V(0.08f * s, 0.22f, -0.22f), tip }, 0.055f, 0.045f, orange, blend: 0.02f);
            b.Spike(tail, tip, tip + V(0.08f * s, 0.05f, -0.06f), 0.045f, cream, 0.6f);
        });
        // Arms hanging, the fins on them like sails
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.54f, 0.02f));
            b.Limb(arm, V(0.11f * s, 0.54f, 0.02f), V(0.16f * s, 0.42f, 0.07f), 0.038f, 0.033f, orange);
            b.Limb(arm, V(0.16f * s, 0.42f, 0.07f), V(0.12f * s, 0.34f, 0.12f), 0.033f, 0.03f, orange);
            b.Ell(arm, V(0.115f * s, 0.32f, 0.13f), V(0.035f, 0.03f, 0.035f), cream);
            b.Spike(arm, V(0.18f * s, 0.44f, 0.05f), V(0.24f * s, 0.32f, 0.0f), 0.04f, blue, 0.3f);
        });

        int head = b.Head(V(0, 0.64f, 0));
        b.Ell(head, V(0, 0.72f, 0.02f), V(0.09f, 0.085f, 0.09f), orange);
        b.Ell(head, V(0, 0.69f, 0.11f), V(0.05f, 0.04f, 0.075f), cream);
        b.Ell(head, V(0, 0.7f, 0.185f), V(0.016f, 0.013f, 0.012f), Rgb(50, 36, 30), blend: 0.005f);
        b.Spike(head, V(0, 0.78f, 0.0f), V(0, 0.92f, -0.06f), 0.05f, orange, 0.5f);
        PokeBuilder.Both(s =>
        {
            b.Mark(head, V(0.065f * s, 0.7f, 0.09f), V(0.7f * s, 0, 0.7f), 0.02f, 0.004f, Rgb(50, 36, 30), MarkShape.Bar, -20f * s);
            int ear = b.Ear(head, s, V(0.06f * s, 0.78f, 0.0f));
            b.Spike(ear, V(0.06f * s, 0.77f, 0.0f), V(0.09f * s, 0.84f, -0.02f), 0.025f, orange, 0.6f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, 0.74f, 0.09f), V(0.5f * s, 0.05f, 1f), 0.022f));
        return b;
    }

    // ------------------------------------------------------------------ Cherubi line

    private static PokeBuilder Cherubi()
    {
        var b = new PokeBuilder("Cherubi", 0.56f, BodyPlan.Floating, V(0, 0.18f, 0)) { Coat = Leaf };
        var cherry = Rgb(240, 104, 128);
        var small = Rgb(214, 74, 108);
        var green = Rgb(70, 180, 80);

        // Two cherries on one stem: it, and the smaller one packed with nutrients
        b.Ell(Body, V(0, 0.18f, 0), V(0.17f, 0.17f, 0.16f), cherry);
        b.PaintEll(Body, V(0, 0.27f, 0.12f), V(0.012f, 0.07f, 0.06f), PixelCanvas.Mix(cherry, Rgb(120, 20, 60), 0.4f), V(-20f, 0, 0), 0.01f);
        int stem = b.Part("stem", Body, V(0, 0.34f, 0), PokeRole.Leaf);
        b.Tube(stem, new[] { V(0, 0.33f, 0), V(0.02f, 0.43f, -0.01f), V(0.08f, 0.47f, -0.01f), V(0.16f, 0.43f, 0.0f), V(0.2f, 0.33f, 0.01f) }, 0.016f, 0.012f, Rgb(120, 140, 60));
        Petal(b, stem, V(0.02f, 0.44f, -0.01f), V(-0.2f, 0.52f, -0.02f), 0.075f, green, Leaf);
        Petal(b, stem, V(0.06f, 0.47f, -0.01f), V(0.16f, 0.6f, -0.02f), 0.06f, green, Leaf);
        b.Ell(stem, V(0.22f, 0.22f, 0.02f), V(0.075f, 0.075f, 0.072f), small);
        PokeBuilder.Both(s => b.Mark(stem, V(0.22f + 0.025f * s, 0.23f, 0.088f), V(0.3f, 0, 1f), 0.012f, 0.004f, Rgb(120, 30, 60), MarkShape.Bar));
        PokeBuilder.Both(s => b.Eye(Body, V(0.06f * s, 0.17f, 0.15f), V(0.4f * s, 0.05f, 1f), 0.028f, Rgb(170, 40, 90)));
        return b;
    }

    private static PokeBuilder Cherrim()
    {
        var b = new PokeBuilder("Cherrim", 0.58f, BodyPlan.Floating, V(0, 0.28f, 0)) { Coat = Leaf };
        var petal = Rgb(128, 76, 128);
        var pink = Rgb(240, 150, 180);
        var green = Rgb(64, 176, 80);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.06f, 0.02f));
            b.Limb(leg, V(0.05f * s, 0.04f, 0.035f), V(0.04f * s, 0.1f, 0.04f), 0.02f, 0.022f, pink);
            b.Ell(leg, V(0.055f * s, 0.025f, 0.04f), V(0.035f, 0.025f, 0.045f), pink);
        });
        // A bud closed against the clouds: four purple petals wrapped round, the pink face peeking out below
        b.Ell(Body, V(0, 0.28f, 0), V(0.16f, 0.2f, 0.15f), petal);
        for (int i = 0; i < 4; i++)
        {
            float a = i * MathF.Tau / 4f + MathF.PI / 4f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            Petal(b, Body, V(0, 0.46f, 0) + dir * 0.04f, V(0, 0.08f, 0) + dir * 0.19f, 0.13f, petal, Leaf, 0.02f);
        }
        b.Ell(Body, V(0, 0.12f, 0.08f), V(0.08f, 0.05f, 0.06f), pink);
        PokeBuilder.Both(s => b.Eye(Body, V(0.035f * s, 0.12f, 0.135f), V(0.3f * s, -0.2f, 1f), 0.018f, Rgb(170, 40, 90)));
        int top = b.Part("sepals", Body, V(0, 0.46f, 0), PokeRole.Leaf);
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f;
            Petal(b, top, V(0, 0.47f, 0), V(MathF.Sin(a) * 0.16f, 0.43f, MathF.Cos(a) * 0.15f), 0.05f, green, Leaf);
        }
        b.Tube(top, new[] { V(0, 0.47f, 0) }.Concat(Spiral(V(0.04f, 0.6f, 0), V(-1, 0, 0), V(0, 1, 0), 0.06f, 0.025f, -0.6f, MathF.PI * 1.6f, 10)).ToArray(), 0.032f, 0.022f, pink);
        return b;
    }

    // ------------------------------------------------------------------ Shellos line

    private static PokeBuilder SeaSlug(bool grown)
    {
        var b = new PokeBuilder(grown ? "Gastrodon" : "Shellos", grown ? 0.72f : 0.5f, BodyPlan.Serpent, V(0, 0.08f, -0.2f)) { Coat = Scales };
        var pink = Rgb(242, 140, 176);
        var foot = grown ? Rgb(244, 160, 186) : Rgb(244, 240, 236);
        var back = grown ? Rgb(150, 98, 70) : Rgb(244, 140, 176);
        var yellow = Rgb(250, 222, 90);
        float k = grown ? 1.3f : 1f;

        // A slug's foot along the ground, its back rimmed in yellow, the front raised
        var path = new[] { V(0, 0.07f * k, -0.24f * k), V(0, 0.08f * k, -0.12f * k), V(0, 0.1f * k, 0.0f), V(0, 0.18f * k, 0.08f * k) };
        int parent = Body;
        for (int i = 0; i < path.Length; i++)
        {
            int seg = i == 0 ? Body : b.Part("seg" + i, parent, path[i], PokeRole.Segment, i);
            float r = (0.08f + 0.012f * i) * k;
            b.Ell(seg, path[i], V(r * 1.1f, r, r * 1.2f), foot);
            b.Ell(seg, path[i] + V(0, r * 0.5f, 0), V(r * 1.04f, r * 0.78f, r * 1.14f), back);
            b.PaintEll(seg, path[i] + V(0, r * 0.15f, 0), V(r * 1.2f, r * 0.08f, r * 1.3f), yellow);
            if (!grown) b.Spike(seg, path[i] + V(0, r * 1.05f, 0), path[i] + V(0, r * 1.45f, -r * 0.3f), r * 0.28f, pink, 0.5f);
            else PokeBuilder.Both(s => b.PaintEll(seg, path[i] + V(r * 0.45f * s, r * 1.2f, 0), V(r * 0.26f, r * 0.2f, r * 0.24f), pink));
            parent = seg;
        }
        int tail = b.Tail(path[0]);
        b.Ell(tail, path[0] + V(0, -0.01f, -0.08f * k), V(0.06f * k, 0.05f * k, 0.07f * k), foot);

        int head = b.Head(path[^1], parent);
        float hy = 0.28f * k;
        b.Ell(head, V(0, hy, 0.12f * k), V(0.085f * k, 0.1f * k, 0.085f * k), grown ? back : pink);
        b.PaintEll(head, V(0, hy - 0.045f * k, 0.17f * k), V(0.075f * k, 0.075f * k, 0.07f * k), grown ? pink : foot);
        b.PaintEll(head, V(0, hy - 0.035f * k, 0.12f * k), V(0.1f * k, 0.012f, 0.1f * k), yellow);
        if (grown)
        {
            // Two broad brown lobes rising from the head
            PokeBuilder.Both(s =>
            {
                b.Limb(head, V(0.05f * s * k, hy + 0.06f * k, 0.1f * k), V(0.1f * s * k, hy + 0.12f * k, 0.08f * k), 0.03f * k, 0.03f * k, back);
                b.Ell(head, V(0.14f * s * k, hy + 0.15f * k, 0.07f * k), V(0.07f * k, 0.042f * k, 0.018f * k), back, V(0, 0, 35f * s));
            });
        }
        else
        {
            // A pink frill on top like a bow
            PokeBuilder.Both(s => b.Ell(head, V(0.04f * s, hy + 0.1f, 0.1f), V(0.05f, 0.045f, 0.05f), Rgb(238, 72, 132)));
        }
        PokeBuilder.Both(s => b.Eye(head, V(0.055f * s * k, hy + 0.025f * k, 0.18f * k), V(0.6f * s, 0.1f, 0.8f), 0.028f * k, sclera: true));
        return b;
    }

    // ------------------------------------------------------------------ Heracross

    private static PokeBuilder Heracross()
    {
        var b = new PokeBuilder("Heracross", 0.88f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Shell };
        var blue = Rgb(82, 132, 210);
        var light = Rgb(120, 168, 230);
        var yellow = Rgb(250, 214, 60);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.36f, 0));
            b.Limb(leg, V(0.1f * s, 0.36f, 0), V(0.15f * s, 0.2f, 0.05f), 0.07f, 0.055f, blue);
            b.Limb(leg, V(0.15f * s, 0.2f, 0.05f), V(0.14f * s, 0.06f, 0.0f), 0.05f, 0.042f, blue);
            b.Ell(leg, V(0.14f * s, 0.035f, 0.04f), V(0.055f, 0.035f, 0.08f), blue);
            PokeBuilder.Both(t => b.Spike(leg, V((0.14f + 0.025f * t) * s, 0.03f, 0.09f), V((0.15f + 0.04f * t) * s, 0.01f, 0.15f), 0.018f, Claw));
        });
        // A beetle's armour: a ridged front, the two halves of its wing case behind
        b.Ell(Body, V(0, 0.5f, 0), V(0.17f, 0.2f, 0.14f), blue);
        for (int i = 0; i < 3; i++)
            b.PaintEll(Body, V(0, 0.42f + i * 0.06f, 0.12f), V(0.12f, 0.008f, 0.04f), PixelCanvas.Mix(blue, Rgb(20, 30, 60), 0.35f), default, 0.01f);
        b.PaintEll(Body, V(0, 0.55f, 0.1f), V(0.1f, 0.06f, 0.05f), light);
        b.PaintEll(Body, V(0, 0.5f, -0.13f), V(0.008f, 0.18f, 0.04f), PixelCanvas.Mix(blue, Rgb(20, 30, 60), 0.4f), default, 0.008f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, 0.62f, 0.02f));
            b.Limb(arm, V(0.16f * s, 0.62f, 0.02f), V(0.27f * s, 0.52f, 0.06f), 0.05f, 0.045f, blue);
            b.Limb(arm, V(0.27f * s, 0.52f, 0.06f), V(0.31f * s, 0.4f, 0.12f), 0.05f, 0.055f, blue);
            Digits(b, arm, V(0.32f * s, 0.36f, 0.14f), V(0.2f * s, -1f, 0.4f), V(0.5f, 0, 0.4f), 0.06f, 0.02f, Claw, Shell);
        });

        int head = b.Head(V(0, 0.68f, 0.02f));
        b.Ell(head, V(0, 0.76f, 0.06f), V(0.1f, 0.09f, 0.1f), blue);
        // The great horn, forked at the tip, that it throws foes with
        b.Tube(head, new[] { V(0, 0.8f, 0.13f), V(0, 0.92f, 0.17f), V(0, 1.04f, 0.17f), V(0, 1.12f, 0.15f) }, 0.04f, 0.026f, blue);
        b.Limb(head, V(-0.06f, 1.12f, 0.15f), V(0.06f, 1.12f, 0.15f), 0.022f, 0.022f, blue);
        PokeBuilder.Both(s => b.Spike(head, V(0.06f * s, 1.11f, 0.15f), V(0.08f * s, 1.2f, 0.15f), 0.022f, blue));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.82f, 0.04f));
            b.Tube(ear, new[] { V(0.06f * s, 0.82f, 0.04f), V(0.12f * s, 0.9f, 0.04f), V(0.17f * s, 0.93f, 0.02f) }, 0.012f, 0.01f, blue);
            b.Ell(ear, V(0.18f * s, 0.93f, 0.02f), V(0.02f, 0.02f, 0.02f), blue);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.055f * s, 0.77f, 0.145f), V(0.55f * s, 0.05f, 0.9f), 0.026f, yellow));
        return b;
    }

    // ------------------------------------------------------------------ Aipom line

    /// <summary>A hand at the end of a tail: a palm with three fat fingers, tipped in <paramref name="tips"/>.</summary>
    private static void TailHand(PokeBuilder b, int bone, Vector3 palm, Vector3 toward, float size, Color color, Color tips)
    {
        var d = Vector3.Normalize(toward);
        var side = Vector3.Normalize(Vector3.Cross(d, Vector3.UnitZ));
        if (side.LengthSquared() < 0.5f) side = Vector3.UnitX;
        b.Ell(bone, palm, V(size, size, size * 0.8f), color);
        for (int i = -1; i <= 1; i++)
        {
            var tip = palm + (d + side * i * 0.55f) * size * 2.1f;
            b.Limb(bone, palm, tip, size * 0.42f, size * 0.38f, color, blend: 0.012f);
            b.Ell(bone, tip, V(size * 0.42f, size * 0.42f, size * 0.42f), tips, blend: 0.008f);
        }
    }

    private static PokeBuilder Aipom()
    {
        var b = new PokeBuilder("Aipom", 0.72f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var purple = Rgb(178, 122, 212);
        var face = Rgb(240, 212, 156);
        var hand = Rgb(250, 222, 120);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.18f, 0));
            b.Limb(leg, V(0.06f * s, 0.18f, 0), V(0.07f * s, 0.05f, 0.02f), 0.035f, 0.03f, purple);
            b.Ell(leg, V(0.075f * s, 0.028f, 0.05f), V(0.045f, 0.028f, 0.07f), face);
        });
        b.Ell(Body, V(0, 0.28f, 0), V(0.1f, 0.13f, 0.09f), purple);
        b.PaintEll(Body, V(0, 0.25f, 0.06f), V(0.065f, 0.09f, 0.05f), face);
        // Its tail curls up behind it and ends in a hand, cleverer than its real ones
        int tail = b.Tail(V(0, 0.2f, -0.08f));
        b.Tube(tail, new[] { V(0, 0.2f, -0.08f), V(0.06f, 0.2f, -0.22f), V(0.2f, 0.3f, -0.28f), V(0.32f, 0.46f, -0.18f), V(0.35f, 0.56f, -0.05f) }, 0.03f, 0.024f, purple);
        TailHand(b, tail, V(0.36f, 0.6f, 0.0f), V(0.2f, 0.6f, 0.6f), 0.05f, hand, hand);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.34f, 0.02f));
            b.Limb(arm, V(0.09f * s, 0.34f, 0.02f), V(0.16f * s, 0.25f, 0.06f), 0.026f, 0.022f, purple);
            b.Ell(arm, V(0.17f * s, 0.23f, 0.07f), V(0.03f, 0.03f, 0.03f), face);
        });

        int head = b.Head(V(0, 0.4f, 0));
        b.Ell(head, V(0, 0.5f, 0.01f), V(0.15f, 0.13f, 0.13f), purple);
        b.Ell(head, V(0, 0.48f, 0.07f), V(0.12f, 0.09f, 0.08f), face);
        // A wide grin full of teeth
        b.Mark(head, V(0, 0.43f, 0.145f), V(0, -0.3f, 1f), 0.06f, 0.016f, Rgb(250, 250, 248), MarkShape.Bar);
        b.Mark(head, V(0, 0.43f, 0.146f), V(0, -0.3f, 1f), 0.065f, 0.022f, Rgb(90, 50, 60), MarkShape.Ring);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.13f * s, 0.54f, -0.01f));
            b.Ell(ear, V(0.18f * s, 0.55f, -0.02f), V(0.07f, 0.075f, 0.03f), purple, V(0, -20f * s, 0));
            b.PaintEll(ear, V(0.185f * s, 0.55f, 0.01f), V(0.045f, 0.05f, 0.025f), hand, V(0, -20f * s, 0));
        });
        b.Spike(head, V(0, 0.61f, 0.0f), V(0.02f, 0.68f, -0.02f), 0.025f, purple);
        b.Spike(head, V(0.01f, 0.61f, 0.02f), V(-0.02f, 0.66f, 0.03f), 0.02f, purple);
        PokeBuilder.Both(s => b.Eye(head, V(0.05f * s, 0.51f, 0.14f), V(0.4f * s, 0.05f, 1f), 0.034f, sclera: true));
        return b;
    }

    private static PokeBuilder Ambipom()
    {
        var b = new PokeBuilder("Ambipom", 0.82f, BodyPlan.Biped, V(0, 0.34f, 0)) { Coat = Fur };
        var purple = Rgb(170, 116, 206);
        var face = Rgb(240, 212, 156);
        var hand = Rgb(250, 224, 132);
        var tips = Rgb(240, 102, 72);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.22f, 0));
            b.Limb(leg, V(0.06f * s, 0.22f, 0), V(0.08f * s, 0.05f, 0.02f), 0.03f, 0.026f, purple);
            b.Ell(leg, V(0.085f * s, 0.028f, 0.05f), V(0.045f, 0.028f, 0.07f), face);
        });
        b.Ell(Body, V(0, 0.34f, 0), V(0.085f, 0.13f, 0.075f), purple);
        b.PaintEll(Body, V(0, 0.32f, 0.05f), V(0.055f, 0.09f, 0.05f), face);
        // Two tails rising either side, each ending in a big hand held high
        int tail = b.Tail(V(0, 0.24f, -0.06f));
        PokeBuilder.Both(s =>
        {
            b.Tube(tail, new[] { V(0, 0.24f, -0.06f), V(0.1f * s, 0.24f, -0.16f), V(0.26f * s, 0.32f, -0.16f), V(0.34f * s, 0.48f, -0.1f), V(0.35f * s, 0.62f, -0.04f) }, 0.028f, 0.024f, purple);
            TailHand(b, tail, V(0.35f * s, 0.68f, -0.02f), V(0.15f * s, 1f, 0.2f), 0.055f, hand, tips);
        });
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.42f, 0.02f));
            b.Limb(arm, V(0.08f * s, 0.42f, 0.02f), V(0.15f * s, 0.32f, 0.06f), 0.022f, 0.02f, purple);
            b.Ell(arm, V(0.16f * s, 0.3f, 0.07f), V(0.026f, 0.026f, 0.026f), face);
        });

        int head = b.Head(V(0, 0.48f, 0));
        b.Ell(head, V(0, 0.57f, 0.01f), V(0.12f, 0.1f, 0.1f), purple);
        b.Ell(head, V(0, 0.55f, 0.06f), V(0.09f, 0.07f, 0.06f), face);
        b.Mark(head, V(0, 0.51f, 0.112f), V(0, -0.3f, 1f), 0.035f, 0.01f, Rgb(90, 50, 60), MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.1f * s, 0.6f, -0.01f));
            b.Ell(ear, V(0.14f * s, 0.61f, -0.02f), V(0.045f, 0.05f, 0.022f), purple, V(0, -20f * s, 0));
            b.PaintEll(ear, V(0.145f * s, 0.61f, 0.0f), V(0.028f, 0.032f, 0.02f), hand, V(0, -20f * s, 0));
        });
        // A long hair curling up from its crown
        b.Tube(head, new[] { V(0, 0.66f, 0.0f), V(0.0f, 0.74f, -0.01f) }.Concat(Spiral(V(0.03f, 0.78f, -0.01f), V(-1, 0, 0), V(0, 1, 0), 0.035f, 0.015f, 0f, MathF.PI * 1.5f, 8)).ToArray(), 0.016f, 0.01f, purple);
        PokeBuilder.Both(s => b.Eye(head, V(0.042f * s, 0.575f, 0.112f), V(0.4f * s, 0.05f, 1f), 0.026f, sclera: true));
        return b;
    }

    // ------------------------------------------------------------------ Drifloon line

    /// <summary>The white puff of cloud on top of a balloon.</summary>
    private static void Puff(PokeBuilder b, int bone, Vector3 at, float r)
    {
        b.Ell(bone, at, V(r, r * 0.8f, r), White, blend: 0.02f);
        b.Ell(bone, at + V(-r * 0.8f, -r * 0.2f, r * 0.2f), V(r * 0.75f, r * 0.65f, r * 0.75f), White, blend: 0.02f);
        b.Ell(bone, at + V(r * 0.8f, -r * 0.15f, -r * 0.2f), V(r * 0.7f, r * 0.6f, r * 0.7f), White, blend: 0.02f);
        b.Ell(bone, at + V(0.1f * r, r * 0.4f, -r * 0.3f), V(r * 0.6f, r * 0.55f, r * 0.6f), White, blend: 0.02f);
    }

    /// <summary>The yellow cross on a balloon's face.</summary>
    private static void Cross(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float size)
    {
        var yellow = Rgb(250, 214, 64);
        b.Mark(bone, at, facing, size, size * 0.3f, yellow, MarkShape.Bar, 45f);
        b.Mark(bone, at + Vector3.Normalize(facing) * 0.001f, facing, size, size * 0.3f, yellow, MarkShape.Bar, -45f);
    }

    private static PokeBuilder Drifloon()
    {
        var b = new PokeBuilder("Drifloon", 0.56f, BodyPlan.Floating, V(0, 0.62f, 0)) { Coat = Fur }.Hover();
        var lilac = Rgb(190, 160, 236);

        // A balloon, a puff of cloud on top, a frill of paper underneath
        b.Ell(Body, V(0, 0.62f, 0), V(0.15f, 0.16f, 0.15f), lilac);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            var at = V(MathF.Sin(a) * 0.05f, 0.48f, MathF.Cos(a) * 0.05f);
            b.Spike(Body, at + V(0, 0.02f, 0), at + V(MathF.Sin(a) * 0.03f, -0.05f, MathF.Cos(a) * 0.03f), 0.03f, PixelCanvas.Light1(lilac, 0.3f), 0.5f);
        }
        Puff(b, Body, V(0, 0.79f, -0.01f), 0.055f);
        Cross(b, Body, V(0, 0.58f, 0.143f), V(0, -0.2f, 1f), 0.04f);
        PokeBuilder.Both(s => b.Eye(Body, V(0.055f * s, 0.65f, 0.135f), V(0.35f * s, 0.05f, 1f), 0.028f));
        // Two strings for arms, a yellow heart at the end of each
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.48f, 0.02f));
            b.Tube(arm, new[] { V(0.04f * s, 0.48f, 0.02f), V(0.16f * s, 0.38f, 0.04f), V(0.2f * s, 0.22f, 0.05f), V(0.17f * s, 0.08f, 0.05f) }, 0.008f, 0.007f, Rgb(40, 36, 46));
            var heart = Rgb(250, 214, 64);
            b.Ell(arm, V(0.155f * s, 0.05f, 0.05f), V(0.025f, 0.022f, 0.016f), heart);
            b.Ell(arm, V(0.19f * s, 0.05f, 0.05f), V(0.025f, 0.022f, 0.016f), heart);
            b.Spike(arm, V(0.172f * s, 0.045f, 0.05f), V(0.172f * s, 0.005f, 0.05f), 0.03f, heart, 0.5f);
        });
        return Lift(b);
    }

    private static PokeBuilder Drifblim()
    {
        var b = new PokeBuilder("Drifblim", 0.84f, BodyPlan.Floating, V(0, 0.6f, 0)) { Coat = Fur }.Hover();
        var purple = Rgb(112, 84, 166);
        var lilac = Rgb(206, 184, 236);
        var ribbon = Rgb(240, 236, 246);
        var yellow = Rgb(250, 214, 64);

        // A great dark balloon, pale underneath, a red frill at its foot
        b.Ell(Body, V(0, 0.6f, 0), V(0.24f, 0.26f, 0.23f), purple);
        b.PaintEll(Body, V(0, 0.36f, 0), V(0.22f, 0.12f, 0.21f), lilac);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var at = V(MathF.Sin(a) * 0.06f, 0.36f, MathF.Cos(a) * 0.06f);
            b.Spike(Body, at, at + V(MathF.Sin(a) * 0.04f, -0.06f, MathF.Cos(a) * 0.04f), 0.03f, Rgb(232, 70, 92), 0.5f);
        }
        Puff(b, Body, V(0, 0.87f, -0.03f), 0.07f);
        Cross(b, Body, V(0, 0.6f, 0.228f), V(0, 0, 1f), 0.06f);
        PokeBuilder.Both(s => b.Eye(Body, V(0.1f * s, 0.69f, 0.2f), V(0.5f * s, 0.1f, 0.9f), 0.03f, Rgb(232, 60, 80)));
        // Four ribbon arms, yellow at the tips
        foreach (var (s, front) in new[] { (-1f, true), (1f, true), (-1f, false), (1f, false) })
        {
            float z = front ? 0.1f : -0.1f;
            int arm = front ? b.Arm(s, V(0.1f * s, 0.38f, z)) : b.Part(s < 0 ? "backArmL" : "backArmR", Body, V(0.1f * s, 0.38f, z), PokeRole.Arm, s + 1.2f, s);
            var elbow = V(0.24f * s, 0.24f, z * 1.8f);
            var end = V(0.32f * s, 0.08f, z * 2.2f);
            Petal(b, arm, V(0.1f * s, 0.38f, z), elbow, 0.035f, ribbon);
            Petal(b, arm, elbow, end, 0.04f, ribbon);
            Petal(b, arm, end, end + V(0.05f * s, -0.07f, 0), 0.04f, yellow);
            Petal(b, arm, end, end + V(-0.02f * s, -0.08f, 0), 0.035f, yellow);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Lopunny

    private static PokeBuilder Lopunny()
    {
        var b = new PokeBuilder("Lopunny", 0.84f, BodyPlan.Biped, V(0, 0.56f, 0)) { Coat = Fur };
        var brown = Rgb(150, 100, 70);
        var cream = Rgb(246, 228, 172);

        // Long legs with fluffy cream cuffs round the ankles
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.44f, 0));
            b.Limb(leg, V(0.07f * s, 0.44f, 0), V(0.09f * s, 0.24f, 0.04f), 0.055f, 0.04f, brown);
            b.Limb(leg, V(0.09f * s, 0.24f, 0.04f), V(0.09f * s, 0.08f, 0.0f), 0.04f, 0.035f, brown);
            b.Ell(leg, V(0.09f * s, 0.11f, 0.01f), V(0.075f, 0.07f, 0.075f), cream);
            b.Ell(leg, V(0.095f * s, 0.03f, 0.05f), V(0.05f, 0.03f, 0.08f), cream);
        });
        b.Ell(Body, V(0, 0.56f, 0), V(0.1f, 0.16f, 0.08f), brown);
        int tail = b.Tail(V(0, 0.46f, -0.07f));
        b.Ell(tail, V(0, 0.46f, -0.11f), V(0.045f, 0.045f, 0.045f), cream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.66f, 0.01f));
            b.Limb(arm, V(0.09f * s, 0.66f, 0.01f), V(0.13f * s, 0.54f, 0.06f), 0.03f, 0.026f, brown);
            b.Ell(arm, V(0.135f * s, 0.51f, 0.07f), V(0.055f, 0.05f, 0.055f), cream);
        });

        int head = b.Head(V(0, 0.72f, 0));
        float hy = 0.82f;
        b.Ell(head, V(0, hy, 0.01f), V(0.11f, 0.1f, 0.1f), brown);
        b.Mark(head, V(0, hy - 0.03f, 0.105f), V(0, 0.1f, 1f), 0.014f, 0.01f, Rgb(232, 130, 150));
        PokeBuilder.Both(s => b.Mark(head, V(0.045f * s, hy + 0.065f, 0.085f), V(0.35f * s, 0.5f, 1f), 0.022f, 0.012f, cream));
        // Tall ears, their tips bent outward, the cream fluff at their roots falling to its waist
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, hy + 0.08f, -0.01f));
            b.Tube(ear, new[] { V(0.06f * s, hy + 0.08f, -0.01f), V(0.08f * s, hy + 0.24f, -0.03f), V(0.13f * s, hy + 0.36f, -0.04f), V(0.2f * s, hy + 0.4f, -0.03f) }, 0.04f, 0.026f, brown);
            b.PaintEll(ear, V(0.08f * s, hy + 0.22f, 0.0f), V(0.022f, 0.09f, 0.02f), Rgb(222, 150, 160));
            b.Ell(head, V(0.15f * s, hy - 0.04f, -0.01f), V(0.08f, 0.1f, 0.075f), cream);
            b.Ell(head, V(0.19f * s, hy - 0.18f, -0.01f), V(0.085f, 0.12f, 0.075f), cream);
            b.Ell(head, V(0.2f * s, hy - 0.32f, 0.0f), V(0.075f, 0.09f, 0.065f), cream);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.045f * s, hy + 0.01f, 0.09f), V(0.45f * s, 0.05f, 1f), 0.03f, Rgb(224, 74, 96)));
        return b;
    }
}
