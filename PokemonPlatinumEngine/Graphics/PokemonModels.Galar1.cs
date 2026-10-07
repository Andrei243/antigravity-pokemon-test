using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Galar's first batch in National Pokédex
// order: Grookey (810) to Eldegoss (830). Their Gigantamax forms are in PokemonModels.Gigantamax.cs with the other
// forms. Helpers shared with the earlier batches are in the files of those batches, PokemonModels.Sinnoh1.cs to
// PokemonModels.Alola3.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Grookey line

    private static PokeBuilder Grookey()
    {
        var b = new PokeBuilder("Grookey", 0.5f, BodyPlan.Biped, V(0, 0.14f, 0)) { Coat = Fur };
        var green = Rgb(132, 190, 72);
        var cream = Rgb(242, 234, 150);
        var brown = Rgb(132, 86, 52);
        var orange = Rgb(232, 118, 56);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.08f, 0));
            b.Limb(leg, V(0.045f * s, 0.09f, 0), V(0.05f * s, 0.035f, 0.01f), 0.03f, 0.026f, green);
            b.Ell(leg, V(0.05f * s, 0.022f, 0.025f), V(0.03f, 0.022f, 0.042f), orange);
        });
        b.Ell(Body, V(0, 0.13f, 0), V(0.065f, 0.07f, 0.058f), green);
        b.PaintEll(Body, V(0, 0.12f, 0.05f), V(0.04f, 0.05f, 0.03f), PixelCanvas.Mix(green, cream, 0.45f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.055f * s, 0.17f, 0));
            var hand = V(0.11f * s, 0.1f, 0.04f);
            b.Limb(arm, V(0.05f * s, 0.17f, 0), hand, 0.022f, 0.02f, green);
            b.Ell(arm, hand + V(0.005f * s, -0.01f, 0.005f), V(0.025f, 0.022f, 0.025f), orange);
        });
        int head = b.Head(V(0, 0.2f, 0));
        var c = V(0, 0.29f, 0.01f);
        var r = V(0.1f, 0.09f, 0.085f);
        b.Ell(head, c, r, green);
        // A mask of pale yellow round its eyes, a round orange snout and round brown ears
        b.PaintEll(head, c + V(0, -0.01f, 0.06f), V(0.085f, 0.06f, 0.05f), cream);
        b.Ell(head, c + V(0, -0.038f, 0.08f), V(0.045f, 0.03f, 0.03f), orange, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.1f * s, -0.005f, -0.01f), V(0.018f, 0.035f, 0.03f), brown, blend: 0.01f);
            var at = On(c, r, 0.04f * s, c.Y + 0.014f);
            b.Eye(head, at, Outward(c, r, at), 0.022f, Rgb(60, 40, 30));
        });
        // The stick it keeps in its hair, and a sprout of two leaves on its crown
        b.Limb(head, c + V(-0.12f, 0.07f, -0.01f), c + V(0.08f, 0.085f, -0.01f), 0.013f, 0.012f, brown, Shell, 0.008f);
        PokeBuilder.Both(s => Frond(b, head, c + V(0, 0.085f, 0), c + V(0.05f * s, 0.15f, 0.0f), 0.02f, green, V(0, 0, 1f), 0.25f, Leaf));
        int tail = b.Tail(V(0, 0.09f, -0.05f));
        b.Tube(tail, Smooth(3, V(0, 0.09f, -0.05f), V(0, 0.05f, -0.12f), V(0, 0.08f, -0.19f), V(0, 0.15f, -0.2f), V(0, 0.17f, -0.15f)), 0.016f, 0.012f, brown, blend: 0f);
        return b;
    }

    /// <summary>Thwackey: a lime monkey with a mantle of brown fur over its shoulders and arms, a big orange snout, rings of gold round its eyes and a stick across its crown, a drumstick in each hand.</summary>
    private static PokeBuilder Thwackey()
    {
        var b = new PokeBuilder("Thwackey", 0.62f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var lime = Rgb(198, 214, 86);
        var pale = Rgb(232, 236, 160);
        var brown = Rgb(96, 62, 48);
        var orange = Rgb(226, 96, 52);
        var gold = Rgb(238, 196, 70);
        var wood = Rgb(150, 100, 62);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.15f, 0));
            b.Limb(leg, V(0.05f * s, 0.16f, 0), V(0.06f * s, 0.04f, 0.01f), 0.032f, 0.024f, lime);
            b.Ell(leg, V(0.062f * s, 0.022f, 0.03f), V(0.03f, 0.022f, 0.05f), pale);
        });
        b.Ell(Body, V(0, 0.23f, 0), V(0.07f, 0.1f, 0.06f), lime);
        b.PaintEll(Body, V(0, 0.21f, 0.05f), V(0.045f, 0.07f, 0.03f), pale);
        // The mantle of brown fur over the shoulders and back
        b.Ell(Body, V(0, 0.3f, -0.02f), V(0.08f, 0.05f, 0.065f), brown, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.065f * s, 0.3f, 0));
            var elbow = V(0.1f * s, 0.22f, 0.03f);
            var hand = V(0.1f * s, 0.15f, 0.07f);
            b.Limb(arm, V(0.06f * s, 0.3f, 0), elbow, 0.035f, 0.03f, brown);
            b.Limb(arm, elbow, hand, 0.03f, 0.024f, brown);
            b.Ell(arm, hand, V(0.024f, 0.024f, 0.024f), lime);
            b.Limb(arm, hand + V(0, 0.05f, -0.02f), hand + V(0.02f * s, -0.1f, 0.06f), 0.01f, 0.009f, wood, Shell, 0.004f);
        });
        int head = b.Head(V(0, 0.32f, 0));
        var c = V(0, 0.4f, 0.01f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Limb(head, V(0, 0.32f, 0), c, 0.035f, 0.035f, lime);
        b.Ell(head, c, r, lime);
        b.PaintEll(head, c + V(0, 0.02f, -0.06f), V(0.07f, 0.06f, 0.04f), brown);
        b.Ell(head, c + V(0, -0.03f, 0.065f), V(0.042f, 0.03f, 0.04f), orange, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.075f * s, -0.005f, -0.01f), V(0.014f, 0.025f, 0.022f), brown, blend: 0.008f);
            var at = On(c, r, 0.032f * s, c.Y + 0.018f);
            b.Mark(head, at, Outward(c, r, at), 0.02f, 0.02f, gold, MarkShape.Ring);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(70, 46, 30));
        });
        b.Limb(head, c + V(-0.1f, 0.06f, -0.01f), c + V(0.1f, 0.068f, -0.01f), 0.011f, 0.01f, wood, Shell, 0.008f);
        Frond(b, head, c + V(0, 0.068f, 0), c + V(0.02f, 0.13f, -0.01f), 0.018f, Rgb(80, 160, 70), V(0, 0, 1f), 0.25f, Leaf);
        int tail = b.Tail(V(0, 0.16f, -0.05f));
        b.Tube(tail, Smooth(3, V(0, 0.16f, -0.05f), V(0, 0.09f, -0.13f), V(0, 0.13f, -0.21f), V(0, 0.21f, -0.2f)), 0.016f, 0.012f, brown, blend: 0f);
        return b;
    }

    /// <summary>
    /// Rillaboom and its Gigantamax form. Rillaboom is a great dark gorilla leaning on its knuckles, its hands and feet
    /// grey, a mane of green leaves round its head, leaves on its chest and round its wrists and ankles, an orange snout
    /// and fierce red eyes. The Gigantamax Rillaboom sits behind a drum the size of a tree's stump, its face ringed green,
    /// trees of drums standing at either side and Gigantamax's red clouds over it.
    /// </summary>
    private static PokeBuilder RillaboomBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Rillaboom-Gmax" : "Rillaboom", 0.95f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Fur };
        var brown = Rgb(84, 64, 56);
        var grey = Rgb(150, 138, 120);
        var green = Rgb(46, 130, 76);
        var light = Rgb(96, 172, 92);
        var orange = Rgb(220, 92, 52);
        var wood = Rgb(140, 100, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.3f, -0.06f));
            var knee = V(0.15f * s, 0.17f, -0.02f);
            b.Limb(leg, V(0.11f * s, 0.32f, -0.06f), knee, 0.075f, 0.06f, brown);
            b.Limb(leg, knee, V(0.15f * s, 0.06f, 0.0f), 0.06f, 0.055f, brown);
            b.Ell(leg, V(0.15f * s, 0.035f, 0.025f), V(0.06f, 0.035f, 0.08f), grey);
            foreach (float a in new[] { -40f, 0f, 40f, 90f })
            {
                var d = V(MathF.Sin(a * Degree) * s, 0, MathF.Cos(a * Degree));
                Frond(b, leg, V(0.15f * s, 0.09f, 0.0f) + d * 0.04f, V(0.15f * s, 0.06f, 0.0f) + d * 0.1f, 0.022f, green, V(0, 1f, 0), 0.25f, Leaf);
            }
        });
        var bc = V(0, 0.5f, 0.0f);
        b.Ell(Body, bc, V(0.18f, 0.17f, 0.14f), brown, V(15f, 0, 0));
        b.Ell(Body, V(0, 0.33f, -0.04f), V(0.13f, 0.1f, 0.1f), brown);
        // Leaves on its chest
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, V(0.07f * s, 0.55f, 0.13f), V(0.05f, 0.03f, 0.04f), green, V(0, 0, 30f * s));
            b.PaintEll(Body, V(0.05f * s, 0.46f, 0.15f), V(0.035f, 0.02f, 0.035f), green, V(0, 0, -20f * s));
        });
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.17f * s, 0.6f, 0.03f);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.27f * s, 0.36f, 0.1f);
            var wrist = V(0.27f * s, 0.12f, 0.15f);
            b.Limb(arm, shoulder, elbow, 0.08f, 0.06f, brown);
            b.Limb(arm, elbow, wrist, 0.06f, 0.06f, brown);
            b.Ell(arm, wrist + V(0, -0.06f, 0.01f), V(0.065f, 0.06f, 0.07f), grey);
            // The cuff of leaves round the wrist
            for (int i = 0; i < 7; i++)
            {
                float a = i * MathF.Tau / 7f;
                var d = V(MathF.Cos(a), 0, MathF.Sin(a));
                Frond(b, arm, wrist + V(0, 0.03f, 0) + d * 0.045f, wrist + V(0, 0.0f, 0) + d * 0.11f, 0.024f, i % 2 == 0 ? green : light, V(0, 1f, 0), 0.25f, Leaf);
            }
        });
        int head = b.Head(V(0, 0.64f, 0.08f));
        var c = V(0, 0.69f, 0.13f);
        var r = V(0.075f, 0.075f, 0.075f);
        b.Limb(head, V(0, 0.62f, 0.05f), c, 0.07f, 0.06f, brown);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.03f, 0.065f), V(0.042f, 0.035f, 0.035f), orange, mat: Shell, blend: 0.012f);
        b.Ell(head, c + V(0, -0.004f, 0.098f), V(0.016f, 0.01f, 0.008f), green, mat: Leaf, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.022f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(214, 60, 40), glare: true);
        });
        // The mane: leaves all round the head, longer at the back
        for (int i = 0; i < 16; i++)
        {
            float a = i * MathF.Tau / 16f;
            var d = V(MathF.Cos(a), MathF.Sin(a) * 0.9f + 0.15f, -0.25f);
            var root = c + V(d.X * 0.06f, d.Y * 0.06f, -0.02f);
            Frond(b, head, root, root + Vector3.Normalize(d) * 0.13f, 0.04f, i % 2 == 0 ? green : light, V(0, 0, 1f), 0.25f, Leaf);
        }
        for (int i = 0; i < 6; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 6f;
            var root = c + V(MathF.Cos(a) * 0.04f, MathF.Sin(a) * 0.04f + 0.02f, -0.06f);
            Frond(b, head, root, root + V(MathF.Cos(a) * 0.08f, MathF.Sin(a) * 0.06f, -0.12f), 0.04f, green, V(0, 0, 1f), 0.25f, Leaf);
        }
        if (gmax)
        {
            // The drum: a great stump on its side in front, its face ringed green, and a tree of drums at each side, rooted to it
            var dn = Vector3.Normalize(V(0, 0.55f, 0.84f));
            var dc = V(0, 0.24f, 0.32f);
            b.Ell(Body, dc, V(0.23f, 0.11f, 0.23f), wood, Euler(dn), Shell, 0.01f);
            b.Limb(Body, dc - dn * 0.06f, V(0, 0.33f, 0.08f), 0.12f, 0.1f, wood, Shell, 0.02f);
            var face = dc + dn * 0.1f;
            b.Ell(Body, face, V(0.18f, 0.014f, 0.18f), Rgb(236, 220, 176), Euler(dn), Shell, 0.004f);
            b.Torus(Body, face, 0.19f, 0.025f, PixelCanvas.Mix(wood, Black, 0.2f), Euler(dn), mat: Shell, blend: 0.006f);
            b.PaintEll(Body, face, V(0.06f, 0.03f, 0.06f), green, Euler(dn), 0.006f);
            b.PaintTorus(Body, face, 0.12f, 0.012f, green, Euler(dn));
            PokeBuilder.Both(s =>
            {
                var root = V(0.42f * s, 0.0f, -0.04f);
                b.Limb(Body, root + V(0, 0.06f, 0), V(0.16f * s, 0.07f, 0.22f), 0.06f, 0.07f, wood, Shell, 0.02f);
                b.Limb(Body, root + V(0, 0.08f, 0), root + V(0, 0.5f, 0), 0.08f, 0.06f, wood, Shell, 0.01f);
                foreach (var (dx, dy, dz) in new[] { (0.1f, 0.62f, 0.05f), (-0.05f, 0.66f, -0.02f), (0.02f, 0.58f, 0.1f) })
                {
                    var top = root + V(dx * s, dy, dz);
                    b.Limb(Body, root + V(0, 0.45f, 0), top, 0.03f, 0.025f, wood, Shell, 0.01f);
                    b.Limb(Body, top, top + V(0, 0.04f, 0), 0.06f, 0.06f, Rgb(176, 130, 92), Shell, 0.006f);
                    Frond(b, Body, top + V(0, 0.02f, 0), top + V(0.05f * s, 0.08f, -0.03f), 0.03f, green, V(0, 0, 1f), 0.25f, Leaf);
                }
            });
            MaxClouds(b, head, c + V(0, 0.07f, -0.08f), 0.035f, 0.1f, 0.17f, 0.9f, 0.4f);
        }
        return b;
    }

    private static PokeBuilder Rillaboom() => RillaboomBuild(false);

    // ------------------------------------------------------------------ Scorbunny line

    /// <summary>A rabbit's long ear from <paramref name="root"/> to <paramref name="tip"/>, white, its upper part in <paramref name="tipColor"/> with a stripe of <paramref name="stripe"/> up its inside.</summary>
    private static void RabbitEar(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float width, Color white, Color tipColor, Color stripe)
    {
        var d = tip - root;
        Frond(b, bone, root, tip, width, white, V(0, 0, 1f), 0.35f);
        b.PaintEll(bone, root + d * 0.82f, V(width * 1.3f, d.Length() * 0.3f, width * 1.3f), tipColor, Euler(d));
        b.PaintEll(bone, root + d * 0.55f + V(0, 0, width * 0.3f), V(width * 0.35f, d.Length() * 0.3f, width * 0.4f), stripe, Euler(d), 0.006f);
    }

    /// <summary>Scorbunny: a little white rabbit ready to run, a band of orange across its nose, long ears tipped orange, big feet of orange with yellow pads and a bright red eye.</summary>
    private static PokeBuilder Scorbunny()
    {
        var b = new PokeBuilder("Scorbunny", 0.5f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var white = Rgb(244, 244, 240);
        var orange = Rgb(240, 120, 56);
        var yellow = Rgb(250, 210, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.15f, 0));
            b.Limb(leg, V(0.04f * s, 0.16f, 0), V(0.045f * s, 0.06f, 0.0f), 0.026f, 0.02f, white);
            b.Ell(leg, V(0.045f * s, 0.035f, 0.02f), V(0.03f, 0.035f, 0.055f), orange);
            b.PaintEll(leg, V(0.045f * s, 0.0f, 0.03f), V(0.028f, 0.012f, 0.04f), yellow, soft: 0.006f);
        });
        b.Ell(Body, V(0, 0.2f, 0), V(0.05f, 0.065f, 0.045f), white);
        b.Ell(Body, V(0, 0.255f, 0.01f), V(0.03f, 0.012f, 0.032f), Rgb(214, 70, 52), blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.045f * s, 0.23f, 0));
            b.Limb(arm, V(0.04f * s, 0.23f, 0), V(0.085f * s, 0.18f, 0.03f), 0.018f, 0.016f, white);
        });
        b.Ell(Body, V(0, 0.17f, -0.045f), V(0.02f, 0.02f, 0.02f), white, blend: 0.01f);
        int head = b.Head(V(0, 0.27f, 0));
        var c = V(0, 0.33f, 0.01f);
        var r = V(0.07f, 0.065f, 0.062f);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0, -0.01f, 0.06f), V(0.045f, 0.012f, 0.03f), orange, soft: 0.006f);
        b.Mark(head, On(c, r, 0, c.Y - 0.03f), V(0, -0.3f, 1f), 0.014f, 0.01f, Rgb(200, 70, 70), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            RabbitEar(b, head, c + V(0.025f * s, 0.05f, -0.01f), c + V(0.07f * s, 0.2f, -0.03f), 0.022f, white, orange, yellow);
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(214, 70, 52));
        });
        return b;
    }

    /// <summary>Raboot: a grey rabbit in a hood of orange-red fur that hangs to its shoulders, its hands hidden in it, a yellow band across its brow, dark legs like trousers and big red feet.</summary>
    private static PokeBuilder Raboot()
    {
        var b = new PokeBuilder("Raboot", 0.68f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var grey = Rgb(196, 198, 200);
        var hood = Rgb(226, 86, 46);
        var dark = Rgb(66, 66, 72);
        var yellow = Rgb(246, 200, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.24f, 0));
            var knee = V(0.055f * s, 0.14f, 0.03f);
            b.Limb(leg, V(0.045f * s, 0.25f, 0), knee, 0.035f, 0.026f, dark);
            b.Limb(leg, knee, V(0.055f * s, 0.05f, -0.01f), 0.026f, 0.02f, dark);
            b.Ell(leg, V(0.055f * s, 0.035f, 0.025f), V(0.035f, 0.035f, 0.065f), hood);
            b.PaintEll(leg, V(0.055f * s, 0.0f, 0.04f), V(0.03f, 0.012f, 0.045f), dark, soft: 0.006f);
        });
        b.Ell(Body, V(0, 0.3f, 0), V(0.055f, 0.07f, 0.045f), dark);
        // The hood of fur, hanging over the shoulders and arms
        b.Ell(Body, V(0, 0.36f, 0), V(0.075f, 0.06f, 0.06f), hood, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.37f, 0));
            b.Limb(arm, V(0.06f * s, 0.37f, 0), V(0.07f * s, 0.28f, 0.03f), 0.035f, 0.03f, hood);
            b.Spike(arm, V(0.07f * s, 0.3f, 0.02f), V(0.075f * s, 0.24f, 0.04f), 0.03f, hood, 0.6f, blend: 0.01f);
        });
        int head = b.Head(V(0, 0.39f, 0));
        var c = V(0, 0.46f, 0.015f);
        var r = V(0.06f, 0.06f, 0.058f);
        b.Ell(head, c, r, grey);
        b.Ell(head, c + V(0, 0.005f, -0.02f), V(0.068f, 0.068f, 0.055f), hood, blend: 0.015f);
        b.PaintEll(head, c + V(0, 0.04f, 0.04f), V(0.05f, 0.012f, 0.04f), yellow, soft: 0.006f);
        b.Ell(head, c + V(0, -0.02f, 0.05f), V(0.025f, 0.018f, 0.018f), grey, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            RabbitEar(b, head, c + V(0.03f * s, 0.05f, -0.03f), c + V(0.12f * s, 0.12f, -0.17f), 0.03f, grey, grey, Rgb(220, 200, 196));
            var at = On(c, r, 0.025f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(226, 120, 40), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Cinderace and its Gigantamax form. Cinderace is a tall rabbit standing on one leg's worth of poise: a white head
    /// with long ears tipped red and yellow, a red brow and a dark mask round red eyes, a white ruff on its chest,
    /// red thighs marked yellow over dark shins, red feet padded yellow. The Gigantamax Cinderace stands small on top of a
    /// great ball of fire it has kicked up.
    /// </summary>
    private static PokeBuilder CinderaceBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Cinderace-Gmax" : "Cinderace", gmax ? 1f : 0.92f, BodyPlan.Biped, gmax ? V(0, 0.32f, 0) : V(0, 0.5f, 0)) { Coat = Fur };
        var white = Rgb(244, 244, 240);
        var red = Rgb(222, 76, 44);
        var yellow = Rgb(250, 200, 70);
        var dark = Rgb(44, 44, 64);
        float k = gmax ? 0.42f : 1f;
        var o = gmax ? V(0, 0.6f, 0) : Vector3.Zero;
        Vector3 P(float x, float y, float z) => o + V(x, y, z) * k;
        if (gmax)
        {
            // The ball of fire: yellow at heart, orange outside, flames licking up and back off it
            var fc = V(0, 0.32f, 0);
            b.Ell(Body, fc, V(0.32f, 0.32f, 0.32f), Rgb(250, 168, 50), mat: Glow);
            b.PaintEll(Body, fc + V(0.04f, 0.04f, 0.2f), V(0.18f, 0.18f, 0.18f), Rgb(252, 226, 110));
            b.PaintEll(Body, fc + V(0, -0.2f, -0.1f), V(0.3f, 0.18f, 0.3f), Rgb(236, 92, 40));
            for (int i = 0; i < 10; i++)
            {
                float a = i * MathF.Tau / 10f;
                var d = Vector3.Normalize(V(MathF.Cos(a), 0.35f + 0.25f * (i % 3), MathF.Sin(a) - 0.4f));
                var root = fc + d * 0.29f;
                b.Spike(Body, root, root + Vector3.Normalize(d + V(0, 0.7f, -0.6f)) * (0.17f + 0.05f * (i % 3)), 0.07f, i % 2 == 0 ? Rgb(246, 120, 40) : Rgb(250, 190, 60), 0.55f, Glow, 0.03f);
            }
        }
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, P(0.055f * s, 0.42f, 0));
            var knee = P(0.065f * s, 0.25f, 0.03f);
            var ankle = P(0.065f * s, 0.08f, -0.01f);
            b.Limb(leg, P(0.055f * s, 0.43f, 0), knee, 0.042f * k, 0.03f * k, red);
            b.PaintEll(leg, P(0.075f * s, 0.32f, 0.03f), V(0.025f, 0.03f, 0.025f) * k, yellow);
            b.Limb(leg, knee, ankle, 0.026f * k, 0.022f * k, dark);
            b.Ell(leg, P(0.065f * s, 0.035f, 0.03f), V(0.035f, 0.035f, 0.07f) * k, red);
            b.PaintEll(leg, P(0.065f * s, 0.0f, 0.05f), V(0.03f, 0.014f, 0.045f) * k, yellow);
        });
        b.Ell(Body, P(0, 0.45f, 0), V(0.075f, 0.06f, 0.055f) * k, red);
        b.Ell(Body, P(0, 0.56f, 0), V(0.062f, 0.08f, 0.05f) * k, white);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.Spike(Body, P(x, 0.62f, 0.035f), P(x * 1.4f, 0.55f, 0.07f), 0.022f * k, white, 0.6f, blend: 0.01f * k);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, P(0.06f * s, 0.62f, 0));
            var elbow = P(0.1f * s, 0.53f, 0.0f);
            var hand = P(0.11f * s, 0.45f, 0.04f);
            b.Limb(arm, P(0.055f * s, 0.62f, 0), elbow, 0.022f * k, 0.018f * k, white);
            b.Limb(arm, elbow, hand, 0.018f * k, 0.016f * k, white);
            b.Ell(arm, hand, V(0.02f, 0.02f, 0.02f) * k, white);
        });
        int head = b.Head(P(0, 0.66f, 0));
        var c = P(0, 0.73f, 0.01f);
        var r = V(0.06f, 0.06f, 0.058f) * k;
        b.Limb(head, P(0, 0.64f, 0), c, 0.03f * k, 0.03f * k, white);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0, 0.05f, 0.03f) * k, V(0.05f, 0.03f, 0.04f) * k, red);
        b.Ell(head, c + V(0, -0.02f, 0.05f) * k, V(0.025f, 0.018f, 0.018f) * k, white, blend: 0.01f * k);
        PokeBuilder.Both(s =>
        {
            RabbitEar(b, head, c + V(0.025f * s, 0.05f, -0.01f) * k, c + V(0.07f * s, 0.24f, -0.05f) * k, 0.026f * k, white, red, yellow);
            var at = On(c, r, 0.026f * s * k + c.X, c.Y + 0.008f * k);
            b.PaintEll(head, at + V(0.008f * s, -0.004f, 0) * k, V(0.02f, 0.014f, 0.02f) * k, dark, soft: 0.006f * k);
            // The rider on the Gigantamax fireball is too small for its eyes to show
            if (!gmax) b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(214, 50, 40), glare: true);
        });
        if (gmax) return b;
        int tail = b.Tail(V(0, 0.44f, -0.05f));
        b.Ell(tail, V(0, 0.44f, -0.06f), V(0.022f, 0.022f, 0.02f), white, blend: 0.01f);
        return b;
    }

    private static PokeBuilder Cinderace() => CinderaceBuild(false);

    // ------------------------------------------------------------------ Sobble line

    /// <summary>Sobble: a timid little blue lizard with a big head, a yellow fin on a stalk on its crown, big worried eyes, dark blue spots on its cheeks and a curled tail.</summary>
    private static PokeBuilder Sobble()
    {
        var b = new PokeBuilder("Sobble", 0.5f, BodyPlan.Biped, V(0, 0.11f, 0)) { Coat = Scales };
        var blue = Rgb(150, 206, 240);
        var spot = Rgb(60, 120, 200);
        var yellow = Rgb(246, 210, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.06f, 0.01f));
            b.Limb(leg, V(0.04f * s, 0.07f, 0.01f), V(0.045f * s, 0.025f, 0.02f), 0.02f, 0.017f, blue);
            b.Ell(leg, V(0.047f * s, 0.016f, 0.035f), V(0.025f, 0.016f, 0.035f), blue);
        });
        b.Ell(Body, V(0, 0.11f, 0), V(0.055f, 0.06f, 0.05f), blue);
        b.PaintEll(Body, V(0, 0.1f, 0.045f), V(0.035f, 0.045f, 0.02f), PixelCanvas.Mix(blue, White, 0.5f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.045f * s, 0.13f, 0.01f));
            b.Limb(arm, V(0.045f * s, 0.13f, 0.01f), V(0.07f * s, 0.085f, 0.045f), 0.016f, 0.014f, blue);
        });
        int head = b.Head(V(0, 0.17f, 0));
        var c = V(0, 0.25f, 0.015f);
        var r = V(0.1f, 0.085f, 0.085f);
        b.Ell(head, c, r, blue);
        // The fin on its stalk
        b.Limb(head, c + V(0, 0.07f, -0.01f), c + V(0, 0.12f, -0.02f), 0.012f, 0.01f, blue);
        Frond(b, head, c + V(0, 0.11f, -0.02f), c + V(0.025f, 0.22f, -0.05f), 0.04f, yellow, V(1f, 0, 0.2f), 0.22f);
        PokeBuilder.Both(s =>
        {
            var cheek = Out(c, r, default, V(0.85f * s, -0.25f, 0.45f));
            b.Mark(head, cheek, Outward(c, r, cheek), 0.015f, 0.015f, spot);
            var at = On(c, r, 0.042f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.026f, Rgb(40, 80, 170));
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.045f), V(0, -0.4f, 1f), 0.014f, 0.007f, Rgb(40, 60, 110), MarkShape.Wave);
        int tail = b.Tail(V(0, 0.08f, -0.04f));
        Curl(b, tail, V(0, 0.08f, -0.04f), V(0, 0.12f, -0.14f), V(0, 0, -1f), V(0, 1f, 0), 0.06f, 1.1f, 0.034f, blue);
        foreach (var at in new[] { V(0.025f, 0.08f, -0.16f), V(-0.026f, 0.15f, -0.19f), V(0.02f, 0.18f, -0.12f) })
            b.PaintEll(tail, at, V(0.018f, 0.018f, 0.018f), spot, soft: 0.006f);
        return b;
    }

    /// <summary>Drizzile: a slim blue lizard with a flap of purple fin over its head like a cap, a white belly, green hands and feet, heavy-lidded eyes and a long tail curled at its end.</summary>
    private static PokeBuilder Drizzile()
    {
        var b = new PokeBuilder("Drizzile", 0.68f, BodyPlan.Biped, V(0, 0.27f, 0)) { Coat = Scales };
        var blue = Rgb(70, 140, 206);
        var lime = Rgb(176, 206, 100);
        var purple = Rgb(140, 120, 196);
        var spot = Rgb(40, 84, 156);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.2f, -0.01f));
            var knee = V(0.07f * s, 0.11f, 0.03f);
            b.Limb(leg, V(0.05f * s, 0.21f, -0.01f), knee, 0.028f, 0.022f, blue);
            b.Limb(leg, knee, V(0.07f * s, 0.035f, 0.0f), 0.022f, 0.018f, blue);
            b.Ell(leg, V(0.07f * s, 0.018f, 0.03f), V(0.035f, 0.018f, 0.06f), lime);
        });
        b.Ell(Body, V(0, 0.27f, 0), V(0.06f, 0.09f, 0.05f), blue);
        b.PaintEll(Body, V(0, 0.26f, 0.045f), V(0.04f, 0.07f, 0.02f), Rgb(240, 244, 244));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.055f * s, 0.33f, 0));
            var elbow = V(0.1f * s, 0.27f, 0.03f);
            var hand = V(0.12f * s, 0.21f, 0.06f);
            b.Limb(arm, V(0.05f * s, 0.33f, 0), elbow, 0.02f, 0.017f, blue);
            b.Limb(arm, elbow, hand, 0.017f, 0.015f, blue);
            b.Ell(arm, hand, V(0.022f, 0.012f, 0.03f), lime, V(0, 0, 30f * s));
        });
        int head = b.Head(V(0, 0.35f, 0.01f));
        var c = V(0, 0.42f, 0.03f);
        var r = V(0.06f, 0.055f, 0.075f);
        b.Limb(head, V(0, 0.34f, 0.0f), c, 0.03f, 0.03f, blue);
        b.Ell(head, c, r, blue);
        // The fin lying back over the head like the peak of a cap
        Frond(b, head, c + V(0, 0.05f, 0.05f), c + V(0, 0.02f, -0.13f), 0.055f, purple, V(0, 1f, 0.2f), 0.18f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.55f * s, 0.25f, 0.75f));
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(214, 196, 80), glare: true);
        });
        int tail = b.Tail(V(0, 0.22f, -0.04f));
        var tp = Smooth(3, V(0, 0.22f, -0.04f), V(0, 0.14f, -0.12f), V(0, 0.08f, -0.22f));
        b.Tube(tail, tp, 0.03f, 0.024f, blue, blend: 0f);
        Curl(b, tail, tp[^1], V(0, 0.13f, -0.28f), V(0, -1f, 0), V(0, 0, -1f), 0.055f, 1.1f, 0.026f, blue);
        foreach (var at in new[] { V(0.02f, 0.1f, -0.32f), V(-0.02f, 0.16f, -0.3f) })
            b.PaintEll(tail, at, V(0.016f, 0.016f, 0.016f), spot, soft: 0.006f);
        return b;
    }

    /// <summary>
    /// Inteleon and its Gigantamax form. Inteleon is a tall lizard like a secret agent, its blue body in a suit of black
    /// down its long thin limbs, a long yellow crest swept back from its head, yellow membranes over its eyes and a long
    /// tail curled at its end. The Gigantamax Inteleon stands small on top of a tower of water with a stream winding up
    /// round it.
    /// </summary>
    private static PokeBuilder InteleonBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Inteleon-Gmax" : "Inteleon", gmax ? 1f : 0.95f, BodyPlan.Biped, gmax ? V(0, 0.5f, 0) : V(0, 0.6f, 0)) { Coat = Scales };
        var blue = Rgb(66, 142, 206);
        var navy = Rgb(40, 44, 64);
        var yellow = Rgb(240, 214, 90);
        float k = gmax ? 0.32f : 1f;
        var o = gmax ? V(0, 0.84f, 0) : Vector3.Zero;
        Vector3 P(float x, float y, float z) => o + V(x, y, z) * k;
        if (gmax)
        {
            // The tower of water, a stream winding up round it, a ring of water at its foot and a disc at its top
            var water = Rgb(110, 190, 240);
            b.Limb(Body, V(0, 0.02f, 0), V(0, 0.86f, 0), 0.06f, 0.04f, water, Glow, 0.01f);
            var spiral = new Vector3[40];
            for (int i = 0; i < spiral.Length; i++)
            {
                float t = i / (float)(spiral.Length - 1), a = t * MathF.Tau * 3f;
                spiral[i] = V(MathF.Cos(a) * 0.07f, 0.04f + t * 0.8f, MathF.Sin(a) * 0.07f);
            }
            b.Tube(Body, spiral, 0.025f, 0.018f, blue, Glow, 0.006f);
            b.Torus(Body, V(0, 0.025f, 0), 0.16f, 0.025f, water, mat: Glow, blend: 0.01f);
            PokeBuilder.Both(s => b.Limb(Body, V(0.04f * s, 0.025f, 0), V(0.15f * s, 0.025f, 0), 0.02f, 0.02f, water, Glow, 0.01f));
            b.Ell(Body, V(0, 0.86f, 0), V(0.11f, 0.02f, 0.11f), water, mat: Glow, blend: 0.01f);
            MaxClouds(b, Body, V(0, 0.82f, -0.06f), 0.03f, 0.1f, 0.12f, 0.8f, 1.2f);
        }
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, P(0.05f * s, 0.5f, -0.01f));
            var knee = P(0.06f * s, 0.27f, 0.03f);
            var ankle = P(0.06f * s, 0.05f, -0.01f);
            b.Limb(leg, P(0.05f * s, 0.51f, -0.01f), knee, 0.03f * k, 0.02f * k, navy);
            b.Limb(leg, knee, ankle, 0.02f * k, 0.016f * k, navy);
            b.Ell(leg, P(0.062f * s, 0.016f, 0.03f), V(0.022f, 0.016f, 0.065f) * k, navy);
        });
        b.Ell(Body, P(0, 0.5f, -0.01f), V(0.058f, 0.05f, 0.045f) * k, navy);
        b.Ell(Body, P(0, 0.61f, 0), V(0.055f, 0.11f, 0.045f) * k, blue);
        // The black of its suit down its sides and over its shoulders, a white throat
        PokeBuilder.Both(s => b.PaintEll(Body, P(0.05f * s, 0.6f, -0.01f), V(0.03f, 0.12f, 0.05f) * k, navy));
        b.PaintEll(Body, P(0, 0.69f, 0.035f), V(0.022f, 0.03f, 0.02f) * k, Rgb(236, 240, 240));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, P(0.055f * s, 0.69f, 0));
            var elbow = P(0.11f * s, 0.58f, -0.01f);
            var hand = P(0.09f * s, 0.5f, 0.04f);
            b.Limb(arm, P(0.05f * s, 0.69f, 0), elbow, 0.018f * k, 0.014f * k, navy);
            b.Limb(arm, elbow, hand, 0.014f * k, 0.012f * k, navy);
            b.Ell(arm, hand, V(0.016f, 0.02f, 0.016f) * k, navy);
        });
        int head = b.Head(P(0, 0.72f, 0));
        var c = P(0, 0.8f, 0.02f);
        var r = V(0.045f, 0.05f, 0.065f) * k;
        b.Limb(head, P(0, 0.7f, 0), c, 0.024f * k, 0.024f * k, navy);
        b.Ell(head, c, r, blue);
        Frond(b, head, c + V(0, 0.035f, 0.02f) * k, c + V(0, 0.13f, -0.15f) * k, 0.035f * k, yellow, V(1f, 0, 0), 0.2f);
        PokeBuilder.Both(s =>
        {
            // The rider on the Gigantamax tower is too small for its eyes to show
            var at = Out(c, r, default, V(0.6f * s, 0.25f, 0.72f));
            if (!gmax) b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(232, 200, 64), glare: true);
        });
        int tail = b.Tail(P(0, 0.47f, -0.04f));
        var tp = Smooth(3, P(0, 0.47f, -0.04f), P(0, 0.32f, -0.12f), P(0, 0.14f, -0.18f), P(0.04f, 0.05f, -0.24f));
        b.Tube(tail, tp, 0.022f * k, 0.014f * k, blue, blend: 0f);
        Curl(b, tail, tp[^1], P(0.07f, 0.08f, -0.26f), V(-1f, 0, 0), V(0, 1f, 0), 0.03f * k, 1f, 0.013f * k, blue);
        return b;
    }

    private static PokeBuilder Inteleon() => InteleonBuild(false);

    // ------------------------------------------------------------------ Skwovet line

    /// <summary>Skwovet: a little grey squirrel standing up, its cheeks stuffed and pale, two buck teeth, tufted ears and a great bushy tail curled up over its back.</summary>
    private static PokeBuilder Skwovet()
    {
        var b = new PokeBuilder("Skwovet", 0.5f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Fur };
        var grey = Rgb(152, 142, 132);
        var pale = Rgb(218, 210, 198);
        var dark = Rgb(104, 96, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.07f, 0.0f));
            b.Limb(leg, V(0.04f * s, 0.07f, 0.0f), V(0.045f * s, 0.025f, 0.02f), 0.025f, 0.02f, grey);
            b.Ell(leg, V(0.047f * s, 0.016f, 0.035f), V(0.022f, 0.016f, 0.035f), pale);
        });
        b.Ell(Body, V(0, 0.12f, 0), V(0.068f, 0.075f, 0.06f), grey);
        b.PaintEll(Body, V(0, 0.11f, 0.05f), V(0.045f, 0.055f, 0.03f), pale);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.15f, 0.02f));
            b.Limb(arm, V(0.05f * s, 0.15f, 0.02f), V(0.035f * s, 0.14f, 0.075f), 0.016f, 0.014f, grey);
        });
        int head = b.Head(V(0, 0.18f, 0.01f));
        var c = V(0, 0.24f, 0.02f);
        var r = V(0.07f, 0.06f, 0.06f);
        b.Ell(head, c, r, grey);
        // The stuffed cheeks, pale, and the face between them
        PokeBuilder.Both(s => b.Ell(head, c + V(0.05f * s, -0.025f, 0.03f), V(0.04f, 0.035f, 0.035f), pale, blend: 0.02f));
        b.PaintEll(head, c + V(0, -0.02f, 0.05f), V(0.035f, 0.03f, 0.03f), pale);
        b.Ell(head, c + V(0, -0.005f, 0.062f), V(0.012f, 0.009f, 0.008f), Rgb(232, 140, 110), blend: 0.004f);
        PokeBuilder.Both(s => b.Box(head, c + V(0.006f * s, -0.04f, 0.058f), V(0.0055f, 0.01f, 0.003f), 0.002f, White, mat: Shell, blend: 0.002f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.015f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(50, 40, 36));
            b.Spike(head, c + V(0.035f * s, 0.045f, -0.01f), c + V(0.045f * s, 0.095f, -0.02f), 0.018f, grey, 0.6f);
            b.Spike(head, c + V(0.042f * s, 0.085f, -0.02f), c + V(0.05f * s, 0.115f, -0.025f), 0.008f, dark, blend: 0.004f);
        });
        int tail = b.Tail(V(0, 0.1f, -0.05f));
        var tp = Smooth(3, V(0, 0.1f, -0.05f), V(0, 0.14f, -0.14f), V(0, 0.25f, -0.17f), V(0, 0.34f, -0.12f), V(0, 0.36f, -0.05f));
        b.Tube(tail, tp, 0.045f, 0.06f, dark, blend: 0.01f);
        Lumps(b, tail, V(0, 0.29f, -0.13f), V(0.05f, 0.07f, 0.06f), 10, 0.03f, dark, grey, 9f);
        return b;
    }

    /// <summary>Greedent: a fat brown squirrel, its round body cream-bellied, its cheeks swollen with freckles, two long buck teeth, little arms holding its belly and a tail as tall as itself standing up behind, spiky at its edge.</summary>
    private static PokeBuilder Greedent()
    {
        var b = new PokeBuilder("Greedent", 0.85f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var brown = Rgb(178, 112, 92);
        var cream = Rgb(240, 214, 182);
        var dark = Rgb(138, 82, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.12f, 0.02f));
            b.Limb(leg, V(0.1f * s, 0.12f, 0.02f), V(0.11f * s, 0.04f, 0.06f), 0.05f, 0.04f, brown);
            b.Ell(leg, V(0.11f * s, 0.025f, 0.09f), V(0.045f, 0.025f, 0.06f), cream);
        });
        var bc = V(0, 0.28f, 0);
        b.Ell(Body, bc, V(0.2f, 0.23f, 0.17f), brown);
        b.PaintEll(Body, bc + V(0, -0.04f, 0.13f), V(0.13f, 0.16f, 0.08f), cream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.34f, 0.08f));
            b.Limb(arm, V(0.15f * s, 0.34f, 0.08f), V(0.06f * s, 0.29f, 0.18f), 0.028f, 0.024f, brown);
            b.Ell(arm, V(0.055f * s, 0.29f, 0.185f), V(0.025f, 0.022f, 0.022f), cream);
        });
        int head = b.Head(V(0, 0.42f, 0.04f));
        var c = V(0, 0.47f, 0.07f);
        var r = V(0.1f, 0.085f, 0.085f);
        b.Ell(head, c, r, brown);
        PokeBuilder.Both(s =>
        {
            var cheek = c + V(0.065f * s, -0.05f, 0.05f);
            b.Ell(head, cheek, V(0.065f, 0.055f, 0.055f), cream, blend: 0.02f);
            foreach (var (x, y) in new[] { (0.01f, 0.02f), (0.03f, 0.0f), (0.0f, -0.01f) })
                b.PaintEll(head, cheek + V(x * s, y, 0.05f), V(0.006f, 0.006f, 0.008f), dark, soft: 0.004f);
            var at = On(c, r, 0.035f * s, c.Y + 0.02f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(50, 36, 30));
            b.Spike(head, c + V(0.055f * s, 0.06f, -0.02f), c + V(0.075f * s, 0.12f, -0.03f), 0.025f, brown, 0.6f);
            b.Spike(head, c + V(0.068f * s, 0.105f, -0.03f), c + V(0.08f * s, 0.14f, -0.035f), 0.012f, dark, blend: 0.004f);
        });
        b.Ell(head, c + V(0, -0.02f, 0.085f), V(0.016f, 0.012f, 0.01f), Rgb(226, 120, 100), blend: 0.004f);
        PokeBuilder.Both(s => b.Box(head, c + V(0.009f * s, -0.07f, 0.09f), V(0.008f, 0.022f, 0.005f), 0.003f, White, mat: Shell, blend: 0.003f));
        // The tail: a great plume standing up behind, spiky at its edges, its top curling forward
        int tail = b.Tail(bc + V(0, -0.05f, -0.15f));
        var tc = V(0, 0.45f, -0.22f);
        b.Limb(tail, bc + V(0, -0.08f, -0.13f), tc + V(0, -0.15f, 0.0f), 0.1f, 0.14f, dark);
        b.Ell(tail, tc, V(0.2f, 0.28f, 0.1f), dark, V(-10f, 0, 0));
        b.Ell(tail, tc + V(0, 0.22f, 0.07f), V(0.13f, 0.08f, 0.09f), dark, blend: 0.04f);
        for (int i = 0; i < 9; i++)
        {
            float a = (i / 8f - 0.5f) * 2.6f;
            var root = tc + V(MathF.Sin(a) * 0.18f, MathF.Cos(a) * 0.24f, -0.02f);
            b.Spike(tail, root, root + V(MathF.Sin(a) * 0.08f, MathF.Cos(a) * 0.06f, -0.02f), 0.035f, dark, 0.45f, blend: 0.012f);
        }
        return b;
    }

    // ------------------------------------------------------------------ Rookidee line

    /// <summary>Rookidee: a small round bird, deep blue above and yellow below, its head black with a crest of blue, a white ring round its red eye and a short grey beak.</summary>
    private static PokeBuilder Rookidee()
    {
        var b = new PokeBuilder("Rookidee", 0.45f, BodyPlan.Bird, V(0, 0.14f, 0)) { Coat = Fur };
        var blue = Rgb(52, 76, 146);
        var black = Rgb(40, 40, 54);
        var yellow = Rgb(234, 216, 100);
        var grey = Rgb(150, 156, 168);
        BirdLegs(b, 0.035f, 0.09f, 0.0f, 0.009f, grey);
        var bc = V(0, 0.15f, 0);
        b.Ell(Body, bc, V(0.07f, 0.065f, 0.09f), blue, V(-10f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.03f, 0.05f), V(0.06f, 0.05f, 0.06f), yellow);
        PokeBuilder.Both(s => FoldedWing(b, s, bc + V(0.06f * s, 0.02f, 0.03f), bc + V(0.075f * s, -0.01f, -0.1f), 0.04f, blue, black));
        int tail = b.Tail(bc + V(0, 0.0f, -0.08f));
        TailFan(b, tail, bc + V(0, 0.0f, -0.08f), 3, 30f, 0.09f, 0.02f, blue, black);
        int head = b.Head(bc + V(0, 0.04f, 0.04f));
        var c = bc + V(0, 0.08f, 0.05f);
        var r = V(0.065f, 0.06f, 0.06f);
        b.Ell(head, c, r, black);
        foreach (var (x, h) in new[] { (-0.015f, 0.05f), (0.015f, 0.05f), (0f, 0.065f) })
            b.Spike(head, c + V(x, 0.045f, -0.01f), c + V(x * 1.5f, 0.045f + h, -0.05f), 0.016f, blue, 0.5f);
        b.Spike(head, c + V(0, -0.01f, 0.05f), c + V(0, -0.015f, 0.1f), 0.016f, grey, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.7f * s, 0.15f, 0.7f));
            b.Mark(head, at, Outward(c, r, at), 0.024f, 0.024f, Rgb(244, 244, 240));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(214, 40, 40));
        });
        return b;
    }

    /// <summary>Corvisquire: a lean blue raven, darker on its wings, raising them as if to take off, a black mask through its red eyes, a ragged crest and a strong dark beak.</summary>
    private static PokeBuilder Corvisquire()
    {
        var b = new PokeBuilder("Corvisquire", 0.7f, BodyPlan.Bird, V(0, 0.25f, 0)) { Coat = Fur };
        var blue = Rgb(72, 106, 162);
        var dark = Rgb(38, 46, 74);
        var light = Rgb(124, 152, 198);
        var grey = Rgb(110, 120, 150);
        BirdLegs(b, 0.04f, 0.19f, 0.0f, 0.012f, grey);
        var bc = V(0, 0.26f, 0);
        b.Ell(Body, bc, V(0.08f, 0.09f, 0.12f), blue, V(-20f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.03f, 0.07f), V(0.06f, 0.06f, 0.05f), light);
        PokeBuilder.Both(s =>
        {
            // The wings half raised, a fan of long dark feathers
            var shoulder = bc + V(0.06f * s, 0.05f, 0.02f);
            int wing = b.Wing(s, shoulder);
            var elbow = shoulder + V(0.09f * s, 0.08f, -0.04f);
            b.Limb(wing, shoulder, elbow, 0.03f, 0.022f, blue);
            for (int i = 0; i < 5; i++)
            {
                float a = (20f + i * 16f) * Degree;
                var root = Vector3.Lerp(shoulder, elbow, 0.4f + 0.15f * i);
                var tip = root + V(MathF.Cos(a) * s * 0.18f, MathF.Sin(a) * 0.18f - 0.06f, -0.08f);
                Frond(b, wing, root, tip, 0.028f, i < 2 ? blue : dark, V(0, 0, 1f), 0.25f);
            }
        });
        int tail = b.Tail(bc + V(0, -0.02f, -0.11f));
        TailFan(b, tail, bc + V(0, -0.02f, -0.11f), 4, 40f, 0.14f, 0.025f, dark, blue, 0.35f);
        int head = b.Head(bc + V(0, 0.08f, 0.07f));
        var c = bc + V(0, 0.13f, 0.08f);
        var r = V(0.055f, 0.055f, 0.06f);
        b.Ell(head, c, r, blue);
        b.PaintEll(head, c + V(0, 0.005f, 0.03f), V(0.06f, 0.016f, 0.04f), dark, soft: 0.006f);
        foreach (var (x, h) in new[] { (-0.02f, 0.05f), (0.02f, 0.05f), (0f, 0.07f) })
            b.Spike(head, c + V(x, 0.04f, -0.02f), c + V(x * 1.6f, 0.04f + h * 0.6f, -0.02f - h), 0.018f, dark, 0.5f);
        b.Spike(head, c + V(0, -0.005f, 0.045f), c + V(0, -0.025f, 0.11f), 0.02f, Rgb(54, 56, 70), 0.8f, Shell, 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.7f * s, 0.12f, 0.7f));
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(220, 46, 46), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Corviknight and its Gigantamax form. Corviknight is a great raven in armour of dark steel, standing tall on
    /// clawed feet with its wings folded about it like a cloak, a crested helm of a head, a heavy hooked beak and a red
    /// eye. The Gigantamax Corviknight spreads its wings wide, each long feather streaked with glowing red, and
    /// Gigantamax's red clouds rise round it.
    /// </summary>
    private static PokeBuilder CorviknightBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Corviknight-Gmax" : "Corviknight", 1f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Metal };
        var navy = Rgb(50, 52, 100);
        var dark = Rgb(32, 32, 58);
        var steel = Rgb(92, 98, 146);
        var glow = Rgb(236, 64, 84);
        BirdLegs(b, 0.07f, 0.24f, 0.0f, 0.03f, dark);
        var bc = V(0, 0.42f, 0);
        b.Ell(Body, bc, V(0.16f, 0.21f, 0.15f), navy, V(-10f, 0, 0));
        // The plates of its breast
        for (int i = 0; i < 3; i++)
            b.Ell(Body, bc + V(0, 0.06f - i * 0.07f, 0.1f - i * 0.005f), V(0.1f - i * 0.012f, 0.04f, 0.05f), i % 2 == 0 ? steel : navy, V(-15f, 0, 0), blend: 0.012f);
        int tail = b.Tail(bc + V(0, -0.12f, -0.1f));
        TailFan(b, tail, bc + V(0, -0.12f, -0.1f), 5, 40f, 0.2f, 0.035f, dark, navy, 0.6f);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.12f * s, 0.12f, 0.0f);
            if (!gmax)
            {
                // The wings folded close like a cloak, their feathers in layers of dark steel
                int wing = FoldedWing(b, s, shoulder, bc + V(0.15f * s, -0.24f, -0.12f), 0.11f, navy, dark);
                for (int i = 0; i < 3; i++)
                    b.Spike(wing, shoulder + V(0.04f * s, -0.12f - i * 0.08f, -0.02f - i * 0.03f), shoulder + V(0.06f * s, -0.24f - i * 0.08f, -0.06f - i * 0.03f), 0.04f, i % 2 == 0 ? dark : steel, 0.35f, blend: 0.01f);
                return;
            }
            // Spread wide, each long feather streaked with glowing red
            int spread = b.Wing(s, shoulder);
            var wrist = shoulder + V(0.26f * s, 0.2f, -0.06f);
            b.Limb(spread, shoulder, wrist, 0.05f, 0.03f, navy);
            for (int i = 0; i < 7; i++)
            {
                float t = i / 6f, a = (70f - i * 14f) * Degree;
                var root = Vector3.Lerp(shoulder + V(0.04f * s, -0.02f, 0), wrist, t);
                var tip = root + V(MathF.Cos(a) * s * 0.3f, MathF.Sin(a) * 0.3f - 0.12f, -0.06f);
                var d = tip - root;
                Frond(b, spread, root, tip, 0.04f, dark, V(0, 0, 1f), 0.22f);
                b.PaintEll(spread, root + d * 0.55f, V(0.012f, d.Length() * 0.35f, 0.05f), glow, Euler(d), 0.006f);
            }
        });
        int head = b.Head(bc + V(0, 0.18f, 0.03f));
        var c = bc + V(0, 0.27f, 0.07f);
        var r = V(0.08f, 0.075f, 0.085f);
        b.Limb(head, bc + V(0, 0.15f, 0.02f), c, 0.08f, 0.07f, navy);
        b.Ell(head, c, r, navy);
        // The crest of its helm, a visor over its eyes, and the hooked beak
        foreach (var (x, h, back) in new[] { (0f, 0.09f, 0.06f), (-0.03f, 0.07f, 0.08f), (0.03f, 0.07f, 0.08f), (0f, 0.06f, 0.12f) })
            b.Spike(head, c + V(x, 0.06f, -0.02f), c + V(x * 1.5f, 0.06f + h, -0.02f - back), 0.03f, steel, 0.45f);
        b.Ell(head, c + V(0, 0.035f, 0.04f), V(0.075f, 0.02f, 0.05f), steel, blend: 0.012f);
        var beak = new[] { c + V(0, -0.01f, 0.06f), c + V(0, -0.02f, 0.13f), c + V(0, -0.05f, 0.16f) };
        b.Spike(head, beak[0], beak[1], 0.035f, dark, 0.8f, Shell, 0.01f);
        b.Spike(head, beak[1] + V(0, 0.01f, -0.02f), beak[2], 0.016f, dark, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.75f * s, 0.05f, 0.65f));
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(226, 48, 48), glare: true);
        });
        if (gmax)
        {
            MaxClouds(b, Body, bc + V(0, 0.16f, -0.12f), 0.035f, 0.12f, 0.3f, 0.9f, 0.3f);
        }
        return b;
    }

    private static PokeBuilder Corviknight() => CorviknightBuild(false);

    // ------------------------------------------------------------------ Blipbug line

    /// <summary>Blipbug: a little grub standing up, its big head deep blue with two fins in a V on top and two great round eyes banded dark, its body cream with yellow feet.</summary>
    private static PokeBuilder Blipbug()
    {
        var b = new PokeBuilder("Blipbug", 0.4f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Shell };
        var navy = Rgb(62, 70, 150);
        var cream = Rgb(236, 226, 184);
        var yellow = Rgb(240, 190, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.04f, 0.01f));
            b.Ell(leg, V(0.035f * s, 0.018f, 0.025f), V(0.022f, 0.018f, 0.03f), yellow);
        });
        b.Ell(Body, V(0, 0.09f, 0), V(0.05f, 0.065f, 0.048f), cream);
        b.PaintTorus(Body, V(0, 0.08f, 0), 0.05f, 0.006f, PixelCanvas.Mix(cream, Black, 0.15f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.04f * s, 0.11f, 0.03f));
            b.Ell(arm, V(0.045f * s, 0.1f, 0.05f), V(0.016f, 0.014f, 0.016f), yellow, blend: 0.01f);
        });
        int head = b.Head(V(0, 0.15f, 0));
        var c = V(0, 0.2f, 0.01f);
        var r = V(0.075f, 0.07f, 0.065f);
        b.Ell(head, c, r, navy);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.02f * s, 0.05f, -0.01f), c + V(0.07f * s, 0.14f, -0.02f), 0.025f, navy, V(0, 0, 1f));
            var at = On(c, r, 0.036f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.026f, sclera: true, white: Rgb(220, 220, 236), pupil: Rgb(40, 44, 90));
        });
        int tail = b.Tail(V(0, 0.06f, -0.04f));
        b.Spike(tail, V(0, 0.06f, -0.03f), V(0, 0.03f, -0.09f), 0.02f, navy);
        return b;
    }

    /// <summary>Dottler: a dome of yellow shell cut into facets, a navy dot at every corner, under which it hides, its two cyan eyes peering out from the dark below its rim over little navy legs.</summary>
    private static PokeBuilder Dottler()
    {
        var b = new PokeBuilder("Dottler", 0.55f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Shell };
        var yellow = Rgb(226, 188, 72);
        var navy = Rgb(56, 60, 112);
        var dark = Rgb(56, 54, 70);
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.06f, -0.06f })
            {
                int leg = b.Leg(s, V(0.1f * s, 0.05f, z), z > 0);
                b.Spike(leg, V(0.1f * s, 0.05f, z), V(0.17f * s, 0.005f, z * 1.4f), 0.02f, navy, blend: 0.008f);
            }
        });
        var dc = V(0, 0.15f, 0);
        var dr = V(0.19f, 0.15f, 0.18f);
        b.Ell(Body, V(0, 0.06f, 0.02f), V(0.15f, 0.05f, 0.14f), dark);
        b.Ell(Body, dc, dr, yellow);
        b.Cut(Body, V(0, 0.04f, 0.17f), V(0.12f, 0.055f, 0.06f));
        b.Ell(Body, V(0, 0.07f, 0.1f), V(0.11f, 0.055f, 0.06f), dark, blend: 0.01f);
        // The facets' seams and a navy dot at every corner
        b.PaintTorus(Body, dc + V(0, 0.07f, 0), 0.155f, 0.005f, PixelCanvas.Mix(yellow, Black, 0.25f), sz: 1f);
        foreach (var (y, n, turn) in new[] { (0.62f, 5, 0f), (0.1f, 10, 0.5f) })
            for (int i = 0; i < n; i++)
            {
                float a = (i + turn) * MathF.Tau / n, ring = MathF.Sqrt(1f - y * y);
                var at = Out(dc, dr, default, V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring));
                if (at.Y < 0.09f) continue;
                b.PaintEll(Body, at, V(0.022f, 0.022f, 0.022f), navy, soft: 0.006f);
            }
        b.PaintEll(Body, dc + V(0, dr.Y, 0), V(0.025f, 0.012f, 0.025f), navy, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = V(0.04f * s, 0.07f, 0.155f);
            b.Eye(Body, at, V(0.25f * s, 0, 1f), 0.02f, Rgb(110, 214, 232));
        });
        return b;
    }

    /// <summary>
    /// Orbeetle and its Gigantamax form. Orbeetle floats under a great red dome spotted navy, its dark face below with
    /// glowing cyan eyes and long gold feelers swept out like a moustache, its little dark body hung with four legs
    /// tipped gold. The Gigantamax Orbeetle is a flying saucer: a broad red disc, a dome ringed with cyan on top, lights
    /// of yellow beneath and Orbeetle's face at its front.
    /// </summary>
    private static PokeBuilder OrbeetleBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Orbeetle-Gmax" : "Orbeetle", gmax ? 1f : 0.75f, BodyPlan.Floating, V(0, gmax ? 0.3f : 0.45f, 0)) { Coat = Shell }.Hover();
        var red = Rgb(214, 62, 52);
        var navy = Rgb(52, 58, 112);
        var gold = Rgb(236, 196, 72);
        var black = Rgb(46, 46, 56);
        var cyan = Rgb(104, 222, 232);
        if (gmax)
        {
            var sc = V(0, 0.3f, 0);
            b.Ell(Body, sc, V(0.42f, 0.07f, 0.42f), red);
            b.Torus(Body, sc, 0.4f, 0.03f, PixelCanvas.Mix(red, Black, 0.3f), mat: Shell, blend: 0.01f);
            var dome = sc + V(0, 0.06f, 0);
            var domeR = V(0.18f, 0.1f, 0.18f);
            b.Ell(Body, dome, domeR, PixelCanvas.Mix(red, Black, 0.15f), blend: 0.02f);
            foreach (float ring in new[] { 0.07f, 0.13f })
                b.PaintTorus(Body, dome + V(0, domeR.Y * MathF.Sqrt(1f - ring * ring / (domeR.X * domeR.X)), 0), ring, 0.012f, cyan);
            for (int i = 0; i < 4; i++)
            {
                float a = (i + 0.5f) * MathF.Tau / 4f;
                b.Ell(Body, sc + V(MathF.Sin(a) * 0.2f, -0.06f, MathF.Cos(a) * 0.2f), V(0.06f, 0.02f, 0.06f), Rgb(252, 214, 90), mat: Glow, blend: 0.008f);
            }
            var front = sc + V(0, -0.01f, 0.34f);
            var frontR = V(0.12f, 0.05f, 0.07f);
            b.Ell(Body, front, frontR, black, blend: 0.012f);
            PokeBuilder.Both(s =>
            {
                var at = Out(front, frontR, default, V(0.4f * s, 0.1f, 0.9f));
                b.Eye(Body, at, Outward(front, frontR, at), 0.02f, cyan);
            });
            MaxClouds(b, Body, sc + V(0, 0.06f, -0.3f), 0.03f, 0.12f, 0.16f, 1.1f, 0.6f);
            return Lift(b);
        }
        var hc = V(0, 0.5f, -0.02f);
        var hr = V(0.17f, 0.13f, 0.16f);
        b.Ell(Body, hc, hr, red);
        foreach (var dir in new[] { V(0, 1f, 0), V(0.6f, 0.6f, 0.3f), V(-0.6f, 0.6f, 0.3f), V(0.7f, 0.5f, -0.5f), V(-0.7f, 0.5f, -0.5f), V(0, 0.5f, -0.9f), V(0.9f, 0.1f, 0.1f), V(-0.9f, 0.1f, 0.1f) })
            b.PaintEll(Body, Out(hc, hr, default, dir), V(0.035f, 0.035f, 0.035f), navy, soft: 0.008f);
        var face = V(0, 0.42f, 0.1f);
        var fr = V(0.1f, 0.06f, 0.07f);
        b.Ell(Body, face, fr, black, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            var at = Out(face, fr, default, V(0.42f * s, 0.15f, 0.88f));
            b.Eye(Body, at, Outward(face, fr, at), 0.018f, cyan);
            // The gold feelers swept out like a moustache
            b.Tube(Body, Smooth(3, face + V(0.04f * s, -0.01f, 0.05f), face + V(0.11f * s, 0.0f, 0.07f), face + V(0.2f * s, 0.05f, 0.04f), face + V(0.26f * s, 0.11f, 0.0f)), 0.009f, 0.004f, gold, Shell, 0.004f);
        });
        b.Ell(Body, V(0, 0.33f, 0.0f), V(0.06f, 0.07f, 0.05f), black);
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.03f, -0.03f })
            {
                int leg = b.Part((s < 0 ? "legL" : "legR") + (z > 0 ? "f" : "b"), Body, V(0.04f * s, 0.32f, z), PokeRole.Leg, z > 0 ? 0f : 1.6f, s);
                var knee = V(0.09f * s, 0.27f, z * 2f);
                var foot = V(0.08f * s, 0.15f, z * 2.5f);
                b.Limb(leg, V(0.04f * s, 0.32f, z), knee, 0.014f, 0.012f, black);
                b.Limb(leg, knee, foot, 0.012f, 0.012f, black);
                b.Spike(leg, foot, foot + V(0, -0.06f, 0.0f), 0.014f, gold, mat: Shell, blend: 0.004f);
            }
        });
        return Lift(b);
    }

    private static PokeBuilder Orbeetle() => OrbeetleBuild(false);

    // ------------------------------------------------------------------ Nickit line

    /// <summary>Nickit: a sly little fox of orange-brown, tall dark ears, a cream muzzle, yellow eyes under dark brows and a great round tail, dark and shaggy at its end.</summary>
    private static PokeBuilder Nickit()
    {
        var b = new PokeBuilder("Nickit", 0.5f, BodyPlan.Quadruped, V(0, 0.13f, 0)) { Coat = Fur };
        var orange = Rgb(196, 94, 52);
        var dark = Rgb(82, 54, 46);
        var cream = Rgb(238, 226, 204);
        BeastLegs(b, 0.035f, 0.12f, 0.05f, -0.06f, 0.02f, orange, dark, dark);
        var bc = V(0, 0.13f, 0);
        b.Ell(Body, bc, V(0.05f, 0.05f, 0.09f), orange);
        int head = b.Head(bc + V(0, 0.05f, 0.07f));
        var c = bc + V(0, 0.1f, 0.09f);
        var r = V(0.05f, 0.048f, 0.052f);
        b.Limb(head, bc + V(0, 0.03f, 0.06f), c, 0.03f, 0.03f, orange);
        b.Ell(head, c, r, orange);
        b.Spike(head, c + V(0, -0.012f, 0.03f), c + V(0, -0.02f, 0.1f), 0.026f, cream, 0.8f);
        b.Ell(head, c + V(0, -0.019f, 0.098f), V(0.008f, 0.007f, 0.007f), Rgb(40, 30, 30), mat: Shell, blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            FoxEar(b, head, c + V(0.028f * s, 0.035f, -0.01f), c + V(0.05f * s, 0.12f, -0.02f), 0.022f, dark, PixelCanvas.Mix(dark, cream, 0.3f));
            var at = Out(c, r, default, V(0.55f * s, 0.25f, 0.8f));
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(240, 200, 60), glare: true);
        });
        // The tail: a round mass of fur, dark and shaggy at its end
        int tail = b.Tail(bc + V(0, 0.01f, -0.08f));
        var tc = bc + V(0, 0.03f, -0.19f);
        b.Limb(tail, bc + V(0, 0.01f, -0.07f), tc, 0.035f, 0.06f, orange);
        b.Ell(tail, tc, V(0.08f, 0.08f, 0.085f), orange);
        b.PaintEll(tail, tc + V(0, -0.02f, -0.06f), V(0.1f, 0.09f, 0.07f), dark);
        FurTufts(b, tail, tc + V(0, -0.01f, -0.03f), V(0.075f, 0.07f, 0.07f), 14, 0.035f, 0.02f, dark, 2f, -0.6f, 0.4f);
        return b;
    }

    /// <summary>Thievul: a slender fox of red-brown with a black mask across its yellow eyes like a thief's, tall black ears, a white ruff on its chest, black stockings and a long tail ending in a black flame of fur.</summary>
    private static PokeBuilder Thievul()
    {
        var b = new PokeBuilder("Thievul", 0.8f, BodyPlan.Quadruped, V(0, 0.28f, 0)) { Coat = Fur };
        var red = Rgb(188, 80, 48);
        var black = Rgb(44, 40, 46);
        var white = Rgb(242, 238, 232);
        BeastLegs(b, 0.05f, 0.25f, 0.1f, -0.12f, 0.028f, red, black, black);
        var bc = V(0, 0.28f, -0.01f);
        b.Ell(Body, bc, V(0.06f, 0.065f, 0.15f), red);
        b.Ell(Body, bc + V(0, 0.02f, 0.11f), V(0.065f, 0.075f, 0.06f), white, blend: 0.02f);
        FurTufts(b, Body, bc + V(0, 0.0f, 0.13f), V(0.055f, 0.06f, 0.05f), 10, 0.035f, 0.016f, white, 2f, -0.95f, 0.8f);
        var neck = Smooth(3, bc + V(0, 0.04f, 0.1f), bc + V(0, 0.12f, 0.14f), bc + V(0, 0.16f, 0.16f));
        b.Tube(Body, neck, 0.04f, 0.032f, red, blend: 0f);
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.03f, 0.03f);
        var r = V(0.048f, 0.046f, 0.055f);
        b.Ell(head, c, r, red);
        b.Spike(head, c + V(0, -0.01f, 0.03f), c + V(0, -0.025f, 0.11f), 0.026f, red, 0.8f);
        PokeBuilder.Both(s => b.PaintEll(head, c + V(0.022f * s, -0.025f, 0.06f), V(0.018f, 0.012f, 0.03f), white, soft: 0.006f));
        b.Ell(head, c + V(0, -0.025f, 0.108f), V(0.008f, 0.007f, 0.007f), black, mat: Shell, blend: 0.003f);
        // The mask: a band of black across the eyes
        b.PaintEll(head, c + V(0, 0.012f, 0.03f), V(0.06f, 0.016f, 0.045f), black, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            FoxEar(b, head, c + V(0.026f * s, 0.035f, -0.015f), c + V(0.05f * s, 0.14f, -0.03f), 0.022f, black, Rgb(120, 70, 60));
            var at = Out(c, r, default, V(0.55f * s, 0.25f, 0.8f));
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(246, 210, 60), glare: true);
        });
        int tail = b.Tail(bc + V(0, 0.02f, -0.14f));
        var tp = Smooth(3, bc + V(0, 0.02f, -0.14f), bc + V(0, -0.04f, -0.26f), bc + V(0, -0.13f, -0.33f), bc + V(0, -0.17f, -0.4f));
        b.Tube(tail, tp, 0.03f, 0.045f, red, blend: 0f);
        var end = tp[^1];
        b.Ell(tail, end, V(0.06f, 0.06f, 0.06f), black);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var d = Vector3.Normalize(V(MathF.Cos(a), MathF.Sin(a) * 0.8f + 0.2f, -0.4f));
            b.Spike(tail, end + d * 0.04f, end + d * 0.1f, 0.025f, black, 0.6f, blend: 0.01f);
        }
        return b;
    }

    // ------------------------------------------------------------------ Gossifleur line

    /// <summary>Gossifleur: a flower walking about on its petals, a big yellow bloom over its little green face like a bonnet and a ruff of curling red-orange petals round it.</summary>
    private static PokeBuilder Gossifleur()
    {
        var b = new PokeBuilder("Gossifleur", 0.45f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Leaf };
        var yellow = Rgb(248, 220, 72);
        var green = Rgb(110, 170, 84);
        var face = Rgb(176, 214, 126);
        var orange = Rgb(222, 102, 62);
        // The ruff of red-orange petals spread round it on the ground
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 8f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(0, 0.07f, 0) + d * 0.03f;
            Frond(b, Body, root, root + d * 0.09f + V(0, -0.035f, 0), 0.034f, orange, V(0, 1f, 0) - d * 0.2f, 0.25f);
        }
        b.Ell(Body, V(0, 0.09f, 0), V(0.055f, 0.05f, 0.05f), green);
        int head = b.Head(V(0, 0.13f, 0.01f));
        var c = V(0, 0.16f, 0.03f);
        var r = V(0.055f, 0.048f, 0.048f);
        b.Ell(head, c, r, face);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y - 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(50, 70, 40));
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.025f), V(0, -0.3f, 1f), 0.01f, 0.006f, Rgb(80, 110, 60), MarkShape.Smile);
        // The yellow bloom on top, tipped back like a bonnet so the face shows under its brim
        FlowerHead(b, head, c + V(0, 0.045f, -0.03f), V(0, 1f, -0.3f), 5, 0.1f, 0.036f, yellow, Rgb(242, 180, 64), 0.022f, 0.5f);
        return b;
    }

    /// <summary>Eldegoss: a great white ball of cotton dotted with seeds, a little green body in front of it, its sleepy face framed by a hood of leaves and a skirt of leaves below.</summary>
    private static PokeBuilder Eldegoss()
    {
        var b = new PokeBuilder("Eldegoss", 0.65f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var cotton = Rgb(246, 242, 228);
        var seed = Rgb(150, 110, 72);
        var green = Rgb(134, 166, 72);
        var dark = Rgb(98, 130, 56);
        var cc = V(0, 0.36f, -0.07f);
        var cr = V(0.24f, 0.24f, 0.23f);
        b.Ell(Body, cc, cr, cotton);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 24; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / 24f, ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var dir = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (dir.Z > 0.5f && dir.Y < 0.3f) continue;
            b.PaintEll(Body, Out(cc, cr, default, dir), V(0.012f, 0.008f, 0.012f), seed, soft: 0.005f);
        }
        // The skirt of leaves down to the ground
        for (int i = 0; i < 6; i++)
        {
            float a = (i / 5f - 0.5f) * 2.4f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(0, 0.1f, 0.13f) + d * 0.04f;
            Frond(b, Body, root, root + d * 0.09f + V(0, -0.09f, 0), 0.04f, i % 2 == 0 ? green : dark, V(0, 1f, 0) - d * 0.3f + V(0, 0, 0.3f), 0.25f, Leaf);
        }
        b.Ell(Body, V(0, 0.13f, 0.13f), V(0.07f, 0.065f, 0.06f), green, blend: 0.02f);
        int head = b.Head(V(0, 0.18f, 0.15f));
        var c = V(0, 0.23f, 0.2f);
        var r = V(0.075f, 0.062f, 0.055f);
        b.Ell(head, c, r, Rgb(164, 194, 104));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.028f * s, c.Y - 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(70, 90, 40), glare: true);
        });
        // The hood of leaves round the face
        for (int i = 0; i < 7; i++)
        {
            float a = (i / 6f - 0.5f) * 2.6f;
            var d = V(MathF.Sin(a), MathF.Cos(a), 0);
            var root = c + V(d.X * r.X, d.Y * r.Y, 0) * 0.85f + V(0, 0, -0.035f);
            Frond(b, head, root, root + d * 0.08f + V(0, 0, -0.02f), 0.03f, i % 2 == 0 ? green : dark, V(0, 0, 1f), 0.25f, Leaf);
        }
        return b;
    }
}
