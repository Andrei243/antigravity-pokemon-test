using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Galar's last batch in National Pokédex
// order: Cufant (878) to Calyrex (898). Their forms are in PokemonModels.Regional.cs and PokemonModels.Gigantamax.cs
// with the other forms. Helpers shared with the earlier batches are in the files of those batches,
// PokemonModels.Sinnoh1.cs to PokemonModels.Galar3.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Cufant and Copperajah

    /// <summary>Cufant: a small round orange elephant, its copper turning green: a patch of teal over the top of its head with a pale gleam in it, teal spots on its back and toes, a short trunk curled up to a teal tip, little round ears and big dark eyes.</summary>
    private static PokeBuilder Cufant()
    {
        var b = new PokeBuilder("Cufant", 0.6f, BodyPlan.Quadruped, V(0, 0.2f, -0.01f)) { Coat = Fur };
        var orange = Rgb(242, 164, 104);
        var teal = Rgb(44, 122, 124);
        var cream = Rgb(250, 232, 196);
        foreach (var (z, front) in new[] { (0.08f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.075f * s, 0.14f, z), front);
                b.Limb(leg, V(0.075f * s, 0.14f, z), V(0.08f * s, 0.05f, z + 0.01f), 0.045f, 0.042f, orange);
                b.Ell(leg, V(0.08f * s, 0.026f, z + 0.015f), V(0.046f, 0.026f, 0.05f), orange);
                b.PaintEll(leg, V(0.08f * s, 0.022f, z + 0.062f), V(0.04f, 0.026f, 0.018f), teal, soft: 0.006f);
            });
        var bc = V(0, 0.2f, -0.01f);
        var br = V(0.13f, 0.12f, 0.15f);
        b.Ell(Body, bc, br, orange);
        foreach (var (x, y, z) in new[] { (0.1f, 0.06f, -0.05f), (-0.09f, 0.07f, 0.02f), (0.03f, 0.11f, -0.1f), (-0.11f, 0.0f, -0.1f) })
            b.PaintEll(Body, bc + V(x, y, z), V(0.03f, 0.025f, 0.035f), teal, soft: 0.006f);
        b.PaintEll(Body, bc + V(0, -0.06f, 0.07f), V(0.08f, 0.05f, 0.07f), cream);
        int tail = b.Tail(bc + V(0, 0.03f, -0.14f));
        b.Limb(tail, bc + V(0, 0.03f, -0.14f), bc + V(0, -0.02f, -0.2f), 0.012f, 0.01f, orange);
        b.Ell(tail, bc + V(0, -0.03f, -0.21f), V(0.018f, 0.022f, 0.018f), teal);
        // The head, its trunk curled up to a teal tip
        int head = b.Head(bc + V(0, 0.07f, 0.11f));
        var c = bc + V(0, 0.11f, 0.14f);
        var r = V(0.11f, 0.1f, 0.095f);
        b.Ell(head, c, r, orange);
        b.PaintEll(head, c + V(0, 0.085f, -0.015f), V(0.1f, 0.045f, 0.09f), teal);
        foreach (var (x, z) in new[] { (-0.06f, 0.05f), (0.0f, 0.075f), (0.06f, 0.045f) })
            b.PaintEll(head, c + V(x, 0.05f, z), V(0.018f, 0.03f, 0.015f), teal, soft: 0.006f);
        b.PaintEll(head, c + V(0.025f, 0.095f, 0.01f), V(0.03f, 0.015f, 0.03f), cream, soft: 0.006f);
        b.Tube(head, Smooth(3, c + V(0, -0.035f, 0.085f), c + V(0, -0.05f, 0.15f), c + V(0, -0.01f, 0.2f), c + V(0, 0.06f, 0.21f), c + V(0, 0.1f, 0.19f)), 0.03f, 0.022f, orange, blend: 0f);
        b.Ell(head, c + V(0, 0.11f, 0.185f), V(0.03f, 0.024f, 0.028f), teal, blend: 0.008f);
        b.PaintEll(head, c + V(0, -0.075f, 0.065f), V(0.03f, 0.014f, 0.02f), Rgb(204, 96, 96), soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.09f * s, 0.01f, -0.03f));
            b.Ell(ear, c + V(0.105f * s, 0.0f, -0.035f), V(0.022f, 0.05f, 0.045f), orange, V(0, -20f * s, 10f * s));
            b.PaintEll(ear, c + V(0.125f * s, 0.0f, -0.025f), V(0.01f, 0.035f, 0.03f), cream, soft: 0.006f);
            var at = On(c, r, 0.055f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.017f, Rgb(70, 46, 40));
        });
        return b;
    }

    /// <summary>
    /// Copperajah and its Gigantamax form. Copperajah is a great elephant of copper gone green, teal all over with
    /// swirls of bright copper on its flanks, broad ears, a heavy trunk curled down and up again to a copper end, long
    /// white tusks hanging beside it and copper feet with white nails. The Gigantamax Copperajah is darker still, its
    /// trunk grown into a column that reaches the ground, great plates of teal and white rising from its back in tiers
    /// and Gigantamax's red clouds over its head.
    /// </summary>
    private static PokeBuilder CopperajahBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Copperajah-Gmax" : "Copperajah", 1f, BodyPlan.Quadruped, V(0, 0.42f, -0.02f)) { Coat = Shell };
        var teal = gmax ? Rgb(40, 74, 70) : Rgb(52, 148, 128);
        var dark = gmax ? Rgb(26, 44, 44) : Rgb(34, 92, 80);
        var copper = Rgb(226, 124, 52);
        var white = Rgb(240, 240, 234);
        foreach (var (z, front) in new[] { (0.17f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.12f * s, 0.28f, z), front);
                b.Limb(leg, V(0.12f * s, 0.3f, z), V(0.125f * s, 0.08f, z + 0.01f), 0.085f, 0.08f, teal);
                b.Ell(leg, V(0.125f * s, 0.042f, z + 0.02f), V(0.088f, 0.042f, 0.09f), copper);
                foreach (float t in new[] { -1f, 0f, 1f })
                    b.Ell(leg, V(0.125f * s + t * 0.042f, 0.032f, z + 0.095f), V(0.02f, 0.026f, 0.016f), white, mat: Shell, blend: 0.006f);
            });
        var bc = V(0, 0.42f, -0.02f);
        var br = V(0.2f, 0.18f, 0.28f);
        b.Ell(Body, bc, br, teal);
        PokeBuilder.Both(s =>
        {
            b.PaintTorus(Body, bc + V(0.19f * s, 0.02f, -0.08f), 0.06f, 0.012f, copper, V(0, 0, 90f));
            b.PaintEll(Body, bc + V(0.18f * s, 0.06f, -0.08f), V(0.04f, 0.012f, 0.03f), copper, soft: 0.006f);
            b.PaintTorus(Body, bc + V(0.17f * s, -0.04f, 0.13f), 0.04f, 0.01f, copper, V(0, 0, 90f));
        });
        b.PaintEll(Body, bc + V(0, 0.17f, -0.02f), V(0.08f, 0.03f, 0.2f), dark);
        int tail = b.Tail(bc + V(0, 0.05f, -0.27f));
        b.Limb(tail, bc + V(0, 0.05f, -0.27f), bc + V(0, -0.06f, -0.33f), 0.02f, 0.015f, teal);
        b.Ell(tail, bc + V(0, -0.08f, -0.34f), V(0.025f, 0.035f, 0.025f), copper);
        int head = b.Head(bc + V(0, 0.06f, 0.24f));
        var c = bc + V(0, 0.1f, 0.33f);
        var r = V(0.14f, 0.13f, 0.12f);
        b.Ell(head, c, r, teal);
        b.PaintEll(head, c + V(0, 0.08f, 0.03f), V(0.13f, 0.035f, 0.1f), dark);
        b.PaintTorus(head, c + V(0, 0.03f, 0.1f), 0.03f, 0.008f, copper, V(90f, 0, 0));
        PokeBuilder.Both(s =>
        {
            // Broad ears, copper inside, and white tusks hanging beside the trunk
            int ear = b.Ear(head, s, c + V(0.11f * s, 0.02f, -0.04f));
            var ec = c + V(0.16f * s, -0.02f, -0.06f);
            b.Ell(ear, ec, V(0.025f, 0.13f, 0.1f), teal, V(0, -25f * s, 8f * s));
            b.PaintEll(ear, ec + V(0.02f * s, -0.02f, 0.03f), V(0.02f, 0.09f, 0.06f), copper);
            for (int i = 0; i < 3; i++)
            {
                var root = c + V((0.07f + 0.025f * i) * s, -0.06f - 0.015f * i, 0.07f - 0.035f * i);
                b.Spike(head, root, root + V(0.03f * s, -0.17f + 0.02f * i, 0.05f), 0.024f, white, 0.6f, Shell);
            }
            var at = On(c, r, 0.065f * s, c.Y + 0.025f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(250, 186, 60), glare: true);
        });
        if (!gmax)
        {
            var trunk = Smooth(3, c + V(0, -0.03f, 0.09f), c + V(0, -0.14f, 0.17f), c + V(0, -0.28f, 0.2f), c + V(0, -0.36f, 0.27f), c + V(0, -0.32f, 0.36f));
            b.Tube(head, trunk, 0.075f, 0.05f, teal, blend: 0f);
            b.PaintEll(head, trunk[^1], V(0.07f, 0.07f, 0.07f), copper);
            return b;
        }
        // The Gigantamax Copperajah's trunk is a column to the ground, its back rises in tiers of plates
        var column = Smooth(3, c + V(0, -0.03f, 0.09f), c + V(0, -0.16f, 0.2f), c + V(0, -0.32f, 0.27f), c + V(0, -0.43f, 0.33f));
        b.Tube(head, column, 0.1f, 0.085f, teal, blend: 0f);
        b.Ell(head, column[^1] + V(0, -0.005f, 0.01f), V(0.1f, 0.05f, 0.1f), copper);
        for (int i = 0; i < 5; i++)
        {
            var root = bc + V(0, 0.12f - 0.02f * i, 0.18f - 0.11f * i);
            Blade(b, Body, root, root + V(0, 0.2f - 0.015f * i, -0.2f), 0.13f - 0.012f * i, i % 2 == 0 ? Rgb(176, 226, 214) : white, V(1f, 0, 0), 0.22f, Shell);
        }
        MaxClouds(b, head, c + V(0, 0.1f, -0.04f), 0.04f, 0.12f, 0.25f, 1.2f);
        return b;
    }

    private static PokeBuilder Copperajah() => CopperajahBuild(false);

    // ------------------------------------------------------------------ The fossils of Galar

    private static readonly Color FossilGreen = Rgb(44, 116, 82);
    private static readonly Color FossilRed = Rgb(210, 62, 72);
    private static readonly Color FossilYellow = Rgb(250, 212, 64);

    /// <summary>A fossil's legs: big green thighs, shins down to broad feet with pink claws.</summary>
    private static void FossilLegs(PokeBuilder b, Vector3 bc, float x, Color green)
    {
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, bc + V(x * s, -0.04f, 0));
            b.Ell(leg, bc + V((x + 0.02f) * s, -0.08f, 0.01f), V(0.07f, 0.09f, 0.09f), green);
            var ankle = V((x + 0.03f) * s, 0.06f, bc.Z + 0.04f);
            b.Limb(leg, bc + V((x + 0.03f) * s, -0.13f, 0.02f), ankle, 0.045f, 0.035f, green);
            b.Ell(leg, V((x + 0.03f) * s, 0.03f, bc.Z + 0.08f), V(0.045f, 0.03f, 0.07f), green);
            Claws(b, leg, V((x + 0.03f) * s, 0.02f, bc.Z + 0.14f), V(0.02f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.01f);
        });
    }

    /// <summary>The bird's head of the Electric fossils: a yellow head with a short beak and a crest of spiky feathers.</summary>
    private static void BirdCrest(PokeBuilder b, int head, Vector3 c, Vector3 r, Color yellow)
    {
        b.Ell(head, c, r, yellow);
        b.Spike(head, c + V(0, -0.012f, 0.04f), c + V(0, -0.025f, 0.105f), 0.028f, yellow, 0.7f);
        b.PaintEll(head, c + V(0, -0.024f, 0.07f), V(0.025f, 0.004f, 0.04f), Rgb(150, 110, 40), soft: 0.003f);
        foreach (var (x, y, z, len) in new[] { (0f, 0.03f, -0.01f, 0.09f), (-0.025f, 0.025f, -0.02f, 0.08f), (0.025f, 0.025f, -0.02f, 0.08f), (-0.04f, 0.01f, -0.03f, 0.07f), (0.04f, 0.01f, -0.03f, 0.07f), (0f, 0.02f, -0.045f, 0.08f) })
            b.Spike(head, c + V(x, y, z), c + V(x * 2.2f, y + len, z - len * 0.6f), 0.015f, yellow, 0.6f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.028f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(50, 40, 30));
        });
    }

    /// <summary>Dracozolt: the front half of a yellow bird on the back half of a green dinosaur: a crested yellow head, a small yellow chest and arms, then a heavy green body with a pale belly, red bands, red spikes down its back and a long tail held out behind.</summary>
    private static PokeBuilder Dracozolt()
    {
        var b = new PokeBuilder("Dracozolt", 0.85f, BodyPlan.Biped, V(0, 0.34f, -0.06f)) { Coat = Scales };
        var belly = Rgb(244, 226, 150);
        var bc = V(0, 0.34f, -0.06f);
        FossilLegs(b, bc, 0.1f, FossilGreen);
        b.Ell(Body, bc, V(0.14f, 0.13f, 0.2f), FossilGreen, V(-10f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.06f, 0.1f), V(0.1f, 0.08f, 0.12f), belly);
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { -0.1f, 0.0f, 0.1f })
                b.PaintEll(Body, bc + V(0.12f * s, 0.03f, z), V(0.04f, 0.09f, 0.016f), FossilRed, V(-20f, 0, 0), 0.006f);
        });
        int tail = b.Tail(bc + V(0, 0, -0.18f));
        var tp = Smooth(3, bc + V(0, 0, -0.18f), bc + V(0, -0.03f, -0.34f), bc + V(0, -0.07f, -0.5f), bc + V(0, -0.07f, -0.63f));
        b.Tube(tail, tp, 0.1f, 0.02f, FossilGreen, blend: 0f);
        for (int i = 3; i < tp.Length - 2; i += 3)
            b.PaintTorus(tail, tp[i], 0.1f - 0.08f * i / tp.Length, 0.01f, FossilRed, Euler(tp[i + 1] - tp[i]));
        // Red spikes down the back and the tail
        for (int i = 0; i < 5; i++)
        {
            var at = bc + V(0, 0.12f - 0.01f * i, 0.08f - 0.07f * i);
            b.Spike(Body, at, at + V(0, 0.08f, -0.05f), 0.03f, FossilRed, 0.4f);
        }
        for (int i = 3; i < tp.Length - 2; i += 2)
        {
            float k = 1f - (float)i / tp.Length;
            b.Spike(tail, tp[i] + V(0, 0.08f * k, 0), tp[i] + V(0, 0.08f * k + 0.06f * k, -0.05f), 0.025f * k + 0.008f, FossilRed, 0.4f);
        }
        // The yellow bird's chest, arms and crested head
        b.Ell(Body, bc + V(0, 0.13f, 0.14f), V(0.065f, 0.08f, 0.06f), FossilYellow, blend: 0.02f);
        foreach (var (x, y) in new[] { (0.04f, 0.15f), (-0.04f, 0.15f), (0.0f, 0.18f) })
            b.Spike(Body, bc + V(x, y, 0.12f), bc + V(x * 1.8f, y + 0.04f, 0.06f), 0.02f, FossilYellow, 0.5f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.05f * s, 0.14f, 0.17f));
            var hand = bc + V(0.08f * s, 0.08f, 0.25f);
            b.Limb(arm, bc + V(0.05f * s, 0.14f, 0.17f), hand, 0.02f, 0.016f, FossilYellow);
            Claws(b, arm, hand + V(0, -0.01f, 0.015f), V(0.008f, 0, 0), V(0, -0.6f, 1f), 0.02f, 0.006f);
        });
        int head = b.Head(bc + V(0, 0.2f, 0.16f));
        var c = bc + V(0, 0.29f, 0.2f);
        b.Limb(head, bc + V(0, 0.17f, 0.15f), c + V(0, -0.02f, -0.01f), 0.038f, 0.034f, FossilYellow);
        BirdCrest(b, head, c, V(0.045f, 0.04f, 0.055f), FossilYellow);
        return b;
    }

    /// <summary>Arctozolt: the front half of a yellow bird on the back half of a frozen fish: a crested yellow head on a thin neck and small clawed arms, set in a great round body of ice under a cap of snow, standing on two blue fins tipped white, with a blue tail fin behind.</summary>
    private static PokeBuilder Arctozolt()
    {
        var b = new PokeBuilder("Arctozolt", 0.9f, BodyPlan.Biped, V(0, 0.33f, 0)) { Coat = Scales };
        var ice = Rgb(196, 226, 246);
        var snow = Rgb(244, 248, 252);
        var blue = Rgb(56, 112, 196);
        var bc = V(0, 0.33f, 0);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, bc + V(0.09f * s, -0.13f, 0));
            b.Limb(leg, bc + V(0.09f * s, -0.12f, 0.0f), V(0.11f * s, 0.06f, 0.04f), 0.05f, 0.03f, blue);
            b.Ell(leg, V(0.11f * s, 0.03f, 0.08f), V(0.04f, 0.03f, 0.08f), blue);
            b.PaintEll(leg, V(0.11f * s, 0.03f, 0.15f), V(0.045f, 0.035f, 0.025f), snow, soft: 0.006f);
        });
        b.Ell(Body, bc, V(0.17f, 0.17f, 0.16f), ice);
        b.Ell(Body, bc + V(0, 0.09f, 0), V(0.18f, 0.095f, 0.17f), snow, blend: 0.012f);
        foreach (float a in new[] { 0.4f, 1.3f, 2.2f, 3.4f, 4.4f, 5.4f })
            b.Ell(Body, bc + V(MathF.Sin(a) * 0.165f, 0.035f, MathF.Cos(a) * 0.155f), V(0.03f, 0.04f, 0.03f), snow, blend: 0.012f);
        int tail = b.Tail(bc + V(0, -0.06f, -0.14f));
        var tp = Smooth(3, bc + V(0, -0.08f, -0.13f), bc + V(0, -0.15f, -0.25f), bc + V(0, -0.17f, -0.34f));
        b.Tube(tail, tp, 0.05f, 0.02f, blue, blend: 0f);
        Blade(b, tail, tp[^1], tp[^1] + V(0, 0.08f, -0.1f), 0.05f, blue, V(1f, 0, 0), 0.25f);
        Blade(b, tail, tp[^1], tp[^1] + V(0, -0.07f, -0.08f), 0.04f, blue, V(1f, 0, 0), 0.25f);
        b.PaintEll(tail, tp[^1] + V(0, 0.07f, -0.09f), V(0.03f, 0.03f, 0.03f), snow, soft: 0.006f);
        // The yellow bird rising from the ice, its arms reaching out of the front
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.1f * s, 0.03f, 0.12f));
            var hand = bc + V(0.15f * s, -0.03f, 0.19f);
            b.Limb(arm, bc + V(0.09f * s, 0.03f, 0.12f), hand, 0.022f, 0.018f, FossilYellow);
            Claws(b, arm, hand + V(0, -0.01f, 0.015f), V(0.008f, 0, 0), V(0, -0.6f, 1f), 0.022f, 0.007f);
        });
        int head = b.Head(bc + V(0, 0.2f, 0.04f));
        var c = bc + V(0, 0.32f, 0.07f);
        b.Limb(head, bc + V(0, 0.15f, 0.02f), c + V(0, -0.02f, -0.01f), 0.04f, 0.032f, FossilYellow);
        BirdCrest(b, head, c, V(0.048f, 0.042f, 0.058f), FossilYellow);
        b.Spike(head, c + V(0.01f, -0.035f, 0.07f), c + V(0.015f, -0.09f, 0.07f), 0.01f, snow, mat: Shell);
        return b;
    }

    /// <summary>Dracovish: the head of a great fish on the back half of a green dinosaur: a long pale blue head, red on top and banded dark across the eyes, its huge jaw gaping full of teeth, on a green body with red spikes down its back and a heavy tail.</summary>
    private static PokeBuilder Dracovish()
    {
        var b = new PokeBuilder("Dracovish", 0.9f, BodyPlan.Biped, V(0, 0.36f, -0.04f)) { Coat = Scales };
        var pale = Rgb(172, 202, 230);
        var band = Rgb(64, 92, 140);
        var bc = V(0, 0.36f, -0.04f);
        FossilLegs(b, bc + V(0, -0.02f, 0), 0.08f, FossilGreen);
        b.Ell(Body, bc, V(0.12f, 0.15f, 0.13f), FossilGreen);
        PokeBuilder.Both(s =>
        {
            foreach (float y in new[] { -0.06f, 0.04f })
                b.PaintEll(Body, bc + V(0.1f * s, y, 0.02f), V(0.03f, 0.016f, 0.08f), FossilRed, V(0, 0, 20f * s), 0.006f);
        });
        int tail = b.Tail(bc + V(0, -0.04f, -0.12f));
        var tp = Smooth(3, bc + V(0, -0.04f, -0.12f), bc + V(0, -0.14f, -0.24f), bc + V(0, -0.21f, -0.35f), bc + V(0, -0.24f, -0.45f));
        b.Tube(tail, tp, 0.085f, 0.02f, FossilGreen, blend: 0f);
        for (int i = 0; i < 4; i++)
        {
            var at = bc + V(0, 0.12f - 0.05f * i, -0.07f - 0.035f * i);
            b.Spike(Body, at, at + V(0, 0.04f, -0.08f), 0.03f, FossilRed, 0.4f);
        }
        for (int i = 2; i < tp.Length - 2; i += 2)
        {
            float k = 1f - (float)i / tp.Length;
            b.Spike(tail, tp[i] + V(0, 0.07f * k, 0), tp[i] + V(0, 0.07f * k + 0.05f * k, -0.05f), 0.022f * k + 0.008f, FossilRed, 0.4f);
        }
        // The fish's head: a long upper head, a great lower jaw, teeth along both
        int head = b.Head(bc + V(0, 0.13f, 0.04f));
        var hc = bc + V(0, 0.29f, 0.09f);
        var hr = V(0.1f, 0.07f, 0.16f);
        b.Ell(head, hc, hr, pale, V(-12f, 0, 0));
        b.PaintEll(head, hc + V(0, 0.06f, -0.04f), V(0.11f, 0.05f, 0.15f), FossilRed);
        b.PaintEll(head, hc + V(0, 0.02f, 0.02f), V(0.11f, 0.014f, 0.05f), band, V(-12f, 0, 0), 0.006f);
        b.Ell(head, hc + V(0, -0.06f, 0.01f), V(0.07f, 0.03f, 0.1f), Rgb(214, 96, 110), blend: 0.01f);
        int jaw = b.Jaw(head, hc + V(0, -0.06f, -0.08f));
        var jc = hc + V(0, -0.11f, 0.06f);
        b.Ell(jaw, jc, V(0.095f, 0.035f, 0.15f), pale, V(8f, 0, 0), blend: 0.015f);
        b.Ell(jaw, jc + V(0, -0.02f, -0.06f), V(0.07f, 0.04f, 0.07f), pale, blend: 0.02f);
        for (int i = -2; i <= 2; i++)
        {
            float t = i / 2f;
            b.Spike(jaw, jc + V(t * 0.06f, 0.02f, 0.12f - MathF.Abs(t) * 0.04f), jc + V(t * 0.065f, 0.055f, 0.13f - MathF.Abs(t) * 0.04f), 0.008f, White, mat: Shell, blend: 0.003f);
            b.Spike(head, hc + V(t * 0.055f, -0.05f, 0.12f - MathF.Abs(t) * 0.04f), hc + V(t * 0.06f, -0.08f, 0.125f - MathF.Abs(t) * 0.04f), 0.007f, White, mat: Shell, blend: 0.003f);
        }
        PokeBuilder.Both(s =>
        {
            Blade(b, head, hc + V(0.08f * s, 0.0f, -0.08f), hc + V(0.14f * s, -0.02f, -0.15f), 0.035f, pale, V(s, 0.3f, 0), 0.25f);
            var at = Out(hc, hr, V(-12f, 0, 0), V(0.75f * s, 0.35f, 0.55f));
            b.Eye(head, at, Outward(hc, hr, at) + V(0, 0, 0.2f), 0.012f, Rgb(40, 50, 70));
        });
        return b;
    }

    /// <summary>Arctovish: the head of a fish on the back half of a frozen creature: a great pale blue head like a block of ice, dark blue across its brow, its small eyes high on it, two pale flippers below, and a short blue fish's body behind with white spots and a forked tail.</summary>
    private static PokeBuilder Arctovish()
    {
        var b = new PokeBuilder("Arctovish", 0.85f, BodyPlan.Fish, V(0, 0.3f, -0.04f)) { Coat = Scales }.Hover();
        var pale = Rgb(192, 220, 242);
        var blue = Rgb(70, 118, 198);
        var deep = Rgb(46, 84, 160);
        var snow = Rgb(244, 248, 252);
        // The short body and the forked tail behind
        b.Limb(Body, V(0, 0.28f, -0.02f), V(0, 0.22f, -0.2f), 0.08f, 0.045f, blue);
        foreach (float z in new[] { -0.08f, -0.14f })
            PokeBuilder.Both(s => b.PaintEll(Body, V(0.06f * s, 0.25f, z), V(0.014f, 0.014f, 0.014f), snow, soft: 0.004f));
        int tail = b.Tail(V(0, 0.22f, -0.2f));
        Blade(b, tail, V(0, 0.22f, -0.2f), V(0, 0.32f, -0.33f), 0.06f, blue, V(1f, 0, 0), 0.25f);
        Blade(b, tail, V(0, 0.22f, -0.2f), V(0, 0.13f, -0.31f), 0.05f, blue, V(1f, 0, 0), 0.25f);
        // The great head
        int head = b.Head(V(0, 0.36f, 0.02f));
        var hc = V(0, 0.42f, 0.06f);
        var hr = V(0.16f, 0.2f, 0.17f);
        b.Ell(head, hc, hr, pale, V(-12f, 0, 0));
        b.PaintEll(head, hc + V(0, 0.07f, 0.12f), V(0.18f, 0.035f, 0.1f), blue, V(-12f, 0, 0));
        b.PaintEll(head, hc + V(0, -0.02f, 0.14f), V(0.012f, 0.06f, 0.04f), deep, soft: 0.006f);
        b.PaintEll(head, hc + V(0, -0.1f, 0.12f), V(0.1f, 0.04f, 0.08f), snow);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, hc + V(0.08f * s, 0.0f, 0.13f), V(0.01f, 0.05f, 0.04f), deep, V(0, 0, -20f * s), 0.006f);
            var at = Out(hc, hr, V(-12f, 0, 0), V(0.42f * s, 0.62f, 0.66f));
            b.Eye(head, at, Outward(hc, hr, at), 0.014f, Rgb(40, 40, 60), sclera: true);
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, hc + V(0.13f * s, -0.12f, 0.02f), PokeRole.Fin, side: s);
            Frond(b, fin, hc + V(0.12f * s, -0.12f, 0.02f), hc + V(0.2f * s, -0.24f, 0.08f), 0.05f, pale, V(s, 0, 0.4f), 0.25f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Duraludon

    /// <summary>
    /// Duraludon and its Gigantamax form. Duraludon is a dragon of light white metal, blocky like a building: short
    /// thick legs, a squared body, a tall head drawn up to a point with a dark seam down its face and small yellow
    /// eyes, great arms that end in dark star-shaped hands and a heavy tail. The Gigantamax Duraludon is a tower: its
    /// head a spire, a wall of blue glass down its front, its arms building wings with dark stars at their ends and
    /// Gigantamax's red clouds round its top.
    /// </summary>
    private static PokeBuilder DuraludonBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Duraludon-Gmax" : "Duraludon", 1f, BodyPlan.Biped, V(0, gmax ? 0.32f : 0.42f, 0)) { Coat = Metal };
        var white = Rgb(226, 228, 236);
        var grey = Rgb(176, 182, 196);
        var navy = Rgb(48, 56, 84);
        var eye = Rgb(250, 222, 60);
        float legY = gmax ? 0.14f : 0.17f;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, legY + 0.08f, 0));
            b.Box(leg, V(0.1f * s, legY, 0.0f), V(0.07f, 0.1f, 0.07f), 0.03f, white, V(0, 10f * s, 0), blend: 0.01f);
            b.Box(leg, V(0.1f * s, 0.04f, 0.03f), V(0.07f, 0.04f, 0.08f), 0.02f, grey, blend: 0.01f);
            b.PaintEll(leg, V(0.1f * s, 0.03f, 0.11f), V(0.07f, 0.035f, 0.02f), navy, soft: 0.006f);
        });
        // A star-shaped hand at the end of each arm
        void Star(int bone, Vector3 at, float s, float size)
        {
            b.Ell(bone, at, V(size * 0.45f, size * 0.45f, size * 0.45f), navy, mat: Metal);
            foreach (var d in new[] { V(0, 1f, 0), V(0, -1f, 0), V(0, 0, 1f), V(0, 0, -1f), V(0, 0.7f, 0.7f), V(0, -0.7f, -0.7f) })
                b.Spike(bone, at, at + d * size + V(0.02f * s, 0, 0), size * 0.36f, navy, 0.5f, Metal, 0.006f);
        }
        if (!gmax)
        {
            var bc = V(0, 0.42f, 0);
            b.Box(Body, bc, V(0.15f, 0.17f, 0.12f), 0.08f, white, blend: 0.01f);
            b.Ell(Body, bc + V(0, 0.11f, 0.01f), V(0.13f, 0.08f, 0.1f), white, blend: 0.03f);
            for (int i = 0; i < 5; i++)
                b.PaintEll(Body, bc + V(i % 2 == 0 ? -0.012f : 0.012f, 0.12f - i * 0.05f, 0.12f), V(0.016f, 0.03f, 0.02f), navy, V(0, 0, i % 2 == 0 ? 35f : -35f), 0.005f);
            int tail = b.Tail(bc + V(0, -0.1f, -0.08f));
            var tp = Smooth(3, bc + V(0, -0.1f, -0.08f), bc + V(0, -0.22f, -0.2f), bc + V(0, -0.31f, -0.33f));
            b.Tube(tail, tp, 0.07f, 0.05f, white, blend: 0f);
            b.Box(tail, tp[^1] + V(0, 0, -0.03f), V(0.05f, 0.045f, 0.06f), 0.02f, navy, blend: 0.01f);
            PokeBuilder.Both(s =>
            {
                int arm = b.Arm(s, bc + V(0.15f * s, 0.1f, 0));
                b.Box(arm, bc + V(0.19f * s, 0.07f, 0.0f), V(0.06f, 0.08f, 0.06f), 0.025f, white, V(0, 0, -15f * s), blend: 0.01f);
                b.Box(arm, bc + V(0.24f * s, -0.06f, 0.04f), V(0.08f, 0.085f, 0.08f), 0.03f, grey, blend: 0.01f);
                Star(arm, bc + V(0.31f * s, -0.08f, 0.06f), s, 0.085f);
            });
            int head = b.Head(bc + V(0, 0.15f, 0.02f));
            var hc = bc + V(0, 0.3f, 0.04f);
            b.Box(head, hc, V(0.08f, 0.13f, 0.08f), 0.04f, white, blend: 0.01f);
            b.Spike(head, hc + V(0, 0.08f, -0.02f), hc + V(0, 0.23f, -0.08f), 0.075f, white, 0.8f, Metal);
            for (int i = 0; i < 4; i++)
                b.PaintEll(head, hc + V(i % 2 == 0 ? -0.01f : 0.01f, 0.06f - i * 0.04f, 0.08f), V(0.012f, 0.025f, 0.02f), navy, V(0, 0, i % 2 == 0 ? 35f : -35f), 0.005f);
            PokeBuilder.Both(s => b.Eye(head, hc + V(0.045f * s, 0.035f, 0.079f), V(0.3f * s, 0, 1f), 0.013f, eye, glare: true));
            return b;
        }
        // The tower: a body of white walls, a spire of a head, a wall of blue glass down the front
        var tc = V(0, 0.32f, 0);
        b.Box(Body, tc, V(0.16f, 0.2f, 0.12f), 0.04f, white, blend: 0.01f);
        var glass = Rgb(96, 166, 224);
        b.Spike(Body, V(0, 0.13f, 0.12f), V(0, 0.62f, 0.1f), 0.13f, glass, 0.22f, Glow, 0.006f);
        int top = b.Head(V(0, 0.5f, 0));
        b.Spike(top, V(0, 0.46f, -0.02f), V(0, 1.06f, -0.05f), 0.15f, white, 0.85f, Metal);
        b.PaintEll(top, V(0, 0.9f, 0.03f), V(0.06f, 0.012f, 0.03f), navy, soft: 0.004f);
        PokeBuilder.Both(s => b.Eye(top, V(0.026f * s, 0.8f, 0.02f), V(0.35f * s, 0.25f, 1f), 0.012f, eye, glare: true));
        int gtail = b.Tail(V(0, 0.2f, -0.1f));
        b.Tube(gtail, Smooth(3, V(0, 0.2f, -0.1f), V(0, 0.1f, -0.25f), V(0, 0.06f, -0.38f)), 0.07f, 0.05f, navy, blend: 0f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, 0.46f, 0));
            b.Box(arm, V(0.22f * s, 0.44f, 0.0f), V(0.07f, 0.05f, 0.06f), 0.02f, white, V(0, 0, -20f * s), blend: 0.01f);
            b.Limb(arm, V(0.26f * s, 0.42f, 0.0f), V(0.33f * s, 0.32f, 0.04f), 0.05f, 0.045f, grey);
            Star(arm, V(0.37f * s, 0.3f, 0.05f), s, 0.1f);
        });
        MaxClouds(b, top, V(0.04f, 0.7f, 0.0f), 0.035f, 0.12f, 0.22f, 1.1f);
        return b;
    }

    private static PokeBuilder Duraludon() => DuraludonBuild(false);

    // ------------------------------------------------------------------ Dreepy line

    private static readonly Color DreepyTeal = Rgb(110, 178, 150);
    private static readonly Color DreepyYellow = Rgb(250, 228, 72);

    /// <summary>A Dreepy: a flat-headed little green ghost lizard with great yellow eyes, red-tipped fins at the back of its head and a body tapering behind; <paramref name="scale"/> 1 is Dreepy itself, less a Dreepy riding on its elders.</summary>
    private static void DreepyBody(PokeBuilder b, int head, int tail, Vector3 hc, float scale, Vector3 back)
    {
        var dark = Rgb(62, 128, 116);
        var pink = Rgb(238, 110, 140);
        var hr = V(0.08f, 0.05f, 0.09f) * scale;
        b.Ell(head, hc, hr, DreepyTeal);
        b.PaintEll(head, hc + V(0, -0.03f, 0.02f) * scale, V(0.07f, 0.03f, 0.08f) * scale, Rgb(176, 220, 196));
        b.Tube(tail, Smooth(3, hc + V(0, -0.01f, -0.05f) * scale, hc + V(0, -0.02f, -0.15f) * scale, hc + back * scale), 0.045f * scale, 0.012f * scale, DreepyTeal, blend: 0f);
        PokeBuilder.Both(s =>
        {
            var root = hc + V(0.05f * s, 0.025f, -0.04f) * scale;
            var tip = hc + V(0.12f * s, 0.05f, -0.13f) * scale;
            Blade(b, head, root, tip, 0.035f * scale, dark, V(0, 1f, 0), 0.25f);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.85f), V(0.025f, 0.025f, 0.025f) * scale, pink, soft: 0.004f);
            bool rider = scale < 1f;
            var at = On(hc, hr, hc.X + (rider ? 0.032f : 0.045f) * s * scale, hc.Y + (rider ? 0.006f : 0.015f) * scale);
            b.Eye(head, at, Outward(hc, hr, at), (rider ? 0.015f : 0.02f) * scale, sclera: true, white: DreepyYellow, pupil: Rgb(30, 30, 40));
        });
    }

    /// <summary>Dreepy: a little ghost of a lizard floating flat, its head broad with great yellow eyes, red-tipped fins swept back from it, four tiny legs hanging and a body tapering to a point behind.</summary>
    private static PokeBuilder Dreepy()
    {
        var b = new PokeBuilder("Dreepy", 0.4f, BodyPlan.Floating, V(0, 0.22f, -0.04f)) { Coat = Scales }.Hover();
        int head = b.Head(V(0, 0.22f, 0.04f));
        int tail = b.Tail(V(0, 0.22f, -0.06f));
        DreepyBody(b, head, tail, V(0, 0.24f, 0.1f), 1f, V(0, 0.04f, -0.36f));
        b.Ell(Body, V(0, 0.214f, -0.04f), V(0.05f, 0.04f, 0.075f), DreepyTeal);
        Blade(b, tail, V(0, 0.25f, -0.1f), V(0, 0.3f, -0.18f), 0.025f, Rgb(62, 128, 116), V(1f, 0, 0), 0.25f);
        foreach (var (z, front) in new[] { (0.02f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.03f * s, 0.2f, z), front);
                b.Limb(leg, V(0.03f * s, 0.2f, z), V(0.045f * s, 0.14f, z + 0.02f), 0.013f, 0.009f, DreepyTeal);
            });
        return Lift(b);
    }

    /// <summary>The stealth flier's hat of Drakloak and Dragapult: a flat dark delta pointing forward over the face, its wings swept back, striped pink.</summary>
    private static void DeltaHat(PokeBuilder b, int head, Vector3 hc, float span, Color dark, Color pink)
    {
        b.Ell(head, hc, V(0.09f, 0.03f, 0.09f), dark);
        Blade(b, head, hc + V(0, 0, 0.02f), hc + V(0, -0.01f, 0.15f), 0.07f, dark, V(0, 1f, 0), 0.3f);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, hc + V(0.05f * s, 0, 0.0f), hc + V(span * s, -0.02f, -0.07f), 0.08f, dark, V(0, 1f, 0), 0.3f);
            b.PaintEll(head, hc + V(span * 0.65f * s, 0.02f, -0.04f), V(0.05f, 0.03f, 0.015f), pink, V(0, -20f * s, 0), 0.006f);
        });
    }

    /// <summary>Drakloak: a teal ghost dragon carrying a Dreepy on its head: a flat dark hat like a delta of a wing over its face, striped pink, yellow eyes beneath it, a cream belly, small arms and a long tail where its legs would trail.</summary>
    private static PokeBuilder Drakloak()
    {
        var b = new PokeBuilder("Drakloak", 0.75f, BodyPlan.Floating, V(0, 0.36f, 0)) { Coat = Scales }.Hover();
        var teal = Rgb(78, 150, 140);
        var dark = Rgb(50, 70, 86);
        var cream = Rgb(234, 224, 166);
        var pink = Rgb(232, 70, 110);
        var bc = V(0, 0.36f, 0);
        b.Ell(Body, bc, V(0.08f, 0.13f, 0.07f), teal);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.05f), V(0.05f, 0.1f, 0.04f), cream);
        int tail = b.Tail(bc + V(0, -0.1f, -0.03f));
        b.Tube(tail, Smooth(3, bc + V(0, -0.1f, -0.03f), bc + V(0, -0.22f, -0.08f), bc + V(0, -0.28f, -0.16f), bc + V(0, -0.27f, -0.26f)), 0.06f, 0.015f, teal, blend: 0f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.07f * s, 0.04f, 0.02f));
            var hand = bc + V(0.12f * s, -0.04f, 0.07f);
            b.Limb(arm, bc + V(0.06f * s, 0.04f, 0.02f), hand, 0.022f, 0.016f, teal);
            Claws(b, arm, hand + V(0, -0.01f, 0.01f), V(0.008f, 0, 0), V(0, -0.8f, 0.6f), 0.018f, 0.006f);
            int leg = b.Leg(s, bc + V(0.05f * s, -0.1f, 0.0f));
            b.Limb(leg, bc + V(0.04f * s, -0.09f, 0.0f), bc + V(0.07f * s, -0.17f, 0.03f), 0.025f, 0.02f, teal);
        });
        int head = b.Head(bc + V(0, 0.12f, 0.02f));
        var fc = bc + V(0, 0.16f, 0.06f);
        var fr = V(0.055f, 0.045f, 0.05f);
        b.Ell(head, fc, fr, teal);
        b.Limb(head, bc + V(0, 0.1f, 0.02f), fc, 0.04f, 0.04f, teal);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.024f * s, fc.Y - 0.005f);
            b.Eye(head, at, Outward(fc, fr, at), 0.013f, sclera: true, white: DreepyYellow, pupil: Rgb(30, 30, 40), glare: true);
        });
        var hat = fc + V(0, 0.05f, -0.01f);
        DeltaHat(b, head, hat, 0.22f, dark, pink);
        // The Dreepy riding in the middle of its hat
        DreepyBody(b, head, head, hat + V(0, 0.035f, 0.03f), 0.45f, V(0, 0.02f, -0.3f));
        return Lift(b);
    }

    /// <summary>Dragapult: a slender ghost dragon like a stealth flier, its head a long dark delta with a horn reaching out to each side, a Dreepy riding in each horn, yellow eyes under it, a cream belly, small arms and a long tail fading pale.</summary>
    private static PokeBuilder Dragapult()
    {
        var b = new PokeBuilder("Dragapult", 1f, BodyPlan.Floating, V(0, 0.44f, 0)) { Coat = Scales }.Hover();
        var teal = Rgb(70, 140, 132);
        var dark = Rgb(46, 64, 80);
        var cream = Rgb(236, 226, 168);
        var pink = Rgb(232, 64, 106);
        var pale = Rgb(176, 230, 220);
        var bc = V(0, 0.44f, 0);
        b.Ell(Body, bc, V(0.065f, 0.14f, 0.06f), teal);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.045f), V(0.04f, 0.11f, 0.035f), cream);
        int tail = b.Tail(bc + V(0, -0.12f, -0.02f));
        var tp = Smooth(3, bc + V(0, -0.12f, -0.02f), bc + V(0, -0.26f, -0.08f), bc + V(0, -0.36f, 0.0f), bc + V(0, -0.38f, 0.12f), bc + V(0, -0.33f, 0.22f));
        b.Tube(tail, tp, 0.055f, 0.012f, teal, blend: 0f);
        for (int i = tp.Length / 3; i < tp.Length; i++)
            b.PaintEll(tail, tp[i], V(0.06f, 0.06f, 0.06f), pale, soft: 0.01f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.06f * s, 0.06f, 0.02f));
            var hand = bc + V(0.11f * s, -0.03f, 0.07f);
            b.Limb(arm, bc + V(0.05f * s, 0.06f, 0.02f), hand, 0.02f, 0.015f, teal);
            Claws(b, arm, hand + V(0, -0.01f, 0.01f), V(0.008f, 0, 0), V(0, -0.8f, 0.6f), 0.018f, 0.006f);
            int leg = b.Leg(s, bc + V(0.04f * s, -0.11f, 0.0f));
            b.Limb(leg, bc + V(0.035f * s, -0.1f, 0.0f), bc + V(0.06f * s, -0.18f, 0.04f), 0.022f, 0.016f, teal);
        });
        int head = b.Head(bc + V(0, 0.14f, 0.02f));
        var fc = bc + V(0, 0.18f, 0.06f);
        var fr = V(0.045f, 0.04f, 0.05f);
        b.Limb(head, bc + V(0, 0.12f, 0.02f), fc, 0.035f, 0.035f, teal);
        b.Ell(head, fc, fr, teal);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.02f * s, fc.Y - 0.006f);
            b.Eye(head, at, Outward(fc, fr, at), 0.012f, sclera: true, white: DreepyYellow, pupil: Rgb(30, 30, 40), glare: true);
        });
        var hat = fc + V(0, 0.045f, -0.01f);
        b.Ell(head, hat, V(0.07f, 0.028f, 0.08f), dark);
        Blade(b, head, hat + V(0, 0, 0.02f), hat + V(0, -0.01f, 0.18f), 0.06f, dark, V(0, 1f, 0), 0.3f);
        b.PaintEll(head, hat + V(0, 0.0f, 0.17f), V(0.03f, 0.03f, 0.025f), pink, soft: 0.006f);
        // The two horns reaching out to the sides, a Dreepy riding in each
        PokeBuilder.Both(s =>
        {
            var root = hat + V(0.04f * s, 0.01f, 0.0f);
            var end = hat + V(0.32f * s, 0.07f, -0.12f);
            b.Limb(head, root, end, 0.045f, 0.022f, dark);
            Blade(b, head, Vector3.Lerp(root, end, 0.78f), Vector3.Lerp(root, end, 0.78f) + V(0.04f * s, 0.1f, -0.06f), 0.035f, dark, V(0, 0.3f, 1f), 0.25f);
            b.PaintEll(head, end, V(0.04f, 0.04f, 0.04f), pink, soft: 0.006f);
            DreepyBody(b, head, head, Vector3.Lerp(root, end, 0.45f) + V(0, 0.062f, 0.045f), 0.65f, V(0.2f * s, 0.04f, -0.25f));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Zacian and Zamazenta

    /// <summary>
    /// Zacian and its Crowned Sword. Zacian is a wolf of the old tales, blue with fur in long points along its back
    /// and flanks, a red braid of fur round its neck and down its sides, cream legs over dark paws, a long rust-red
    /// tail, a cream muzzle and golden eyes. Crowned, it wears gold armour: a helm with long ears, great gold plates
    /// rising from its back like wings, and a sword held crosswise in its jaws, its blade glowing rose.
    /// </summary>
    private static PokeBuilder ZacianBuild(bool crowned)
    {
        var b = new PokeBuilder(crowned ? "Zacian-Crowned" : "Zacian", 1f, BodyPlan.Quadruped, V(0, 0.47f, -0.02f)) { Coat = Fur };
        var blue = Rgb(64, 128, 204);
        var deep = Rgb(44, 82, 162);
        var red = Rgb(214, 96, 92);
        var rust = Rgb(182, 88, 78);
        var cream = Rgb(236, 216, 190);
        var paw = Rgb(52, 50, 64);
        var gold = Rgb(238, 200, 92);
        BeastLegs(b, 0.07f, 0.42f, 0.19f, -0.2f, 0.05f, blue, paw, cream);
        var bc = V(0, 0.47f, -0.02f);
        var br = V(0.1f, 0.1f, 0.24f);
        b.Ell(Body, bc, br, blue);
        b.Ell(Body, bc + V(0, 0.02f, 0.15f), V(0.1f, 0.11f, 0.1f), blue, blend: 0.04f);
        // Fur in long points along its back and down its flanks
        for (int i = 0; i < 6; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            var at = bc + V(0.03f * side, 0.08f, 0.15f - i * 0.065f);
            b.Spike(Body, at, at + V(0.05f * side, 0.1f, -0.07f), 0.03f, i % 3 == 2 ? deep : blue, 0.5f);
        }
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.1f, 0.0f, -0.1f, -0.18f })
                b.Spike(Body, bc + V(0.08f * s, -0.05f, z), bc + V(0.1f * s, -0.15f, z - 0.03f), 0.03f, deep, 0.5f);
            // The red braid down each side
            var braid = Smooth(3, bc + V(0.075f * s, 0.06f, 0.2f), bc + V(0.1f * s, -0.01f, 0.08f), bc + V(0.1f * s, -0.02f, -0.06f), bc + V(0.085f * s, 0.03f, -0.19f), bc + V(0.03f * s, 0.09f, -0.24f));
            for (int i = 0; i < braid.Length; i++)
                b.Ell(Body, braid[i] + V(0.006f * s * (i % 2 == 0 ? 1f : -1f), 0, 0), V(0.02f, 0.02f, 0.024f), red, blend: 0.006f);
        });
        b.PaintTorus(Body, bc + V(0, 0.08f, 0.2f), 0.08f, 0.018f, red, V(60f, 0, 0));
        int tail = b.Tail(bc + V(0, 0.04f, -0.23f));
        var tp = Smooth(3, bc + V(0, 0.04f, -0.23f), bc + V(0, 0.0f, -0.36f), bc + V(0, -0.1f, -0.46f), bc + V(0, -0.22f, -0.5f));
        b.Tube(tail, tp, 0.055f, 0.03f, rust, blend: 0f);
        for (int i = 2; i < tp.Length; i += 2)
            PokeBuilder.Both(s => Frond(b, tail, tp[i], tp[i] + V(0.05f * s, -0.07f, -0.04f), 0.035f, rust, V(s, 0, -0.5f), 0.3f));
        // A long neck up to its head
        int head = b.Head(bc + V(0, 0.08f, 0.18f));
        var c = bc + V(0, 0.27f, 0.27f);
        var r = V(0.055f, 0.055f, 0.075f);
        b.Tube(head, Smooth(3, bc + V(0, 0.05f, 0.17f), bc + V(0, 0.17f, 0.22f), c + V(0, -0.03f, -0.04f)), 0.06f, 0.045f, blue, blend: 0f);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, -0.02f, 0.07f), V(0.03f, 0.028f, 0.045f), cream, blend: 0.015f);
        b.Ell(head, c + V(0, -0.005f, 0.115f), V(0.012f, 0.01f, 0.008f), paw, blend: 0.004f);
        foreach (var (x, y) in new[] { (0f, 0.04f), (-0.03f, 0.02f), (0.03f, 0.02f), (0f, -0.01f) })
            b.Spike(head, c + V(x, y, -0.05f), c + V(x * 1.6f, y + 0.03f, -0.15f), 0.025f, blue, 0.5f);
        PokeBuilder.Both(s =>
        {
            if (crowned) return;
            b.Spike(head, c + V(0.03f * s, 0.04f, -0.02f), c + V(0.05f * s, 0.11f, -0.05f), 0.02f, blue, 0.55f);
            var at = On(c, r, 0.036f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(232, 176, 60), glare: true);
        });
        if (!crowned) return b;
        // The Crowned Sword: a gold helm with long ears, gold plates on its back, a sword in its jaws
        var hc = c + V(0, 0.018f, 0.0f);
        var hr = V(0.062f, 0.055f, 0.08f);
        b.Ell(head, hc, hr, gold, mat: Metal);
        b.Spike(head, hc + V(0, 0.03f, 0.05f), hc + V(0, 0.01f, 0.1f), 0.025f, gold, 0.5f, Metal);
        b.PaintEll(head, hc + V(0, 0.045f, 0.03f), V(0.012f, 0.02f, 0.025f), Rgb(214, 60, 90), soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, hc + V(0.035f * s, 0.035f, -0.02f), hc + V(0.07f * s, 0.15f, -0.07f), 0.025f, gold, 0.5f, Metal);
            var at = On(hc, hr, 0.04f * s, hc.Y - 0.005f);
            b.Eye(head, at, Outward(hc, hr, at), 0.012f, Rgb(232, 176, 60), glare: true);
            for (int i = 0; i < 3; i++)
            {
                var root = bc + V(0.07f * s, 0.08f, 0.14f - i * 0.1f);
                Blade(b, Body, root, root + V((0.2f - i * 0.04f) * s, 0.28f - i * 0.06f, -0.12f), 0.08f - i * 0.012f, gold, V(s, 0.2f, 0.3f), 0.2f, Metal);
            }
        });
        var hilt = c + V(-0.05f, -0.035f, 0.06f);
        b.Limb(head, hilt, hilt + V(0.1f, 0, 0), 0.012f, 0.012f, gold, Metal);
        b.Ell(head, hilt + V(0.1f, 0, 0), V(0.012f, 0.035f, 0.02f), gold, mat: Metal, blend: 0.004f);
        Blade(b, head, hilt + V(0.1f, 0, 0), hilt + V(0.42f, -0.02f, -0.02f), 0.035f, Rgb(236, 120, 132), V(0, 0, 1f), 0.25f, Glow);
        return b;
    }

    private static PokeBuilder Zacian() => ZacianBuild(false);

    /// <summary>
    /// Zamazenta and its Crowned Shield. Zamazenta is a wolf of the old tales, deep red with white scars across its
    /// flanks, a great blue mane swept up and back from its brow, grey legs over blue paws with blue fur at the
    /// ankles, a blue brush of a tail and golden eyes. Crowned, a gold and red shield covers its chest, a gold helm
    /// its face, and gold plates its shoulders.
    /// </summary>
    private static PokeBuilder ZamazentaBuild(bool crowned)
    {
        var b = new PokeBuilder(crowned ? "Zamazenta-Crowned" : "Zamazenta", 1f, BodyPlan.Quadruped, V(0, 0.48f, -0.02f)) { Coat = Fur };
        var red = Rgb(194, 58, 64);
        var dark = Rgb(140, 38, 50);
        var blue = Rgb(52, 72, 170);
        var grey = Rgb(212, 212, 222);
        var gold = Rgb(238, 196, 84);
        BeastLegs(b, 0.085f, 0.42f, 0.19f, -0.2f, 0.058f, red, blue, grey);
        var bc = V(0, 0.48f, -0.02f);
        b.Ell(Body, bc, V(0.12f, 0.12f, 0.24f), red);
        b.Ell(Body, bc + V(0, 0.03f, 0.15f), V(0.12f, 0.13f, 0.11f), red, blend: 0.04f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, bc + V(0.115f * s, 0.0f, -0.06f), V(0.02f, 0.008f, 0.06f), White, V(40f, 0, 0), 0.005f);
            b.PaintEll(Body, bc + V(0.115f * s, 0.0f, -0.06f), V(0.02f, 0.008f, 0.06f), White, V(-40f, 0, 0), 0.005f);
            foreach (float z in new[] { 0.1f, 0.0f, -0.1f, -0.18f })
                b.Spike(Body, bc + V(0.1f * s, -0.06f, z), bc + V(0.11f * s, -0.15f, z - 0.03f), 0.032f, dark, 0.5f);
            foreach (float z in new[] { 0.19f, -0.2f })
                b.Spike(Body, V(0.09f * s, 0.2f, z), V(0.1f * s, 0.12f, z - 0.06f), 0.03f, blue, 0.5f);
        });
        for (int i = 0; i < 5; i++)
        {
            float side = i % 2 == 0 ? 1f : -1f;
            var at = bc + V(0.03f * side, 0.1f, 0.12f - i * 0.07f);
            b.Spike(Body, at, at + V(0.04f * side, 0.08f, -0.07f), 0.03f, dark, 0.5f);
        }
        int tail = b.Tail(bc + V(0, 0.04f, -0.23f));
        var tp = Smooth(3, bc + V(0, 0.04f, -0.23f), bc + V(0, 0.0f, -0.34f), bc + V(0, -0.1f, -0.42f), bc + V(0, -0.2f, -0.44f));
        b.Tube(tail, tp, 0.06f, 0.035f, blue, blend: 0f);
        for (int i = 2; i < tp.Length; i += 2)
            PokeBuilder.Both(s => Frond(b, tail, tp[i], tp[i] + V(0.05f * s, -0.06f, -0.05f), 0.035f, blue, V(s, 0, -0.5f), 0.3f));
        int head = b.Head(bc + V(0, 0.09f, 0.18f));
        var c = bc + V(0, 0.27f, 0.28f);
        var r = V(0.06f, 0.058f, 0.075f);
        b.Tube(head, Smooth(3, bc + V(0, 0.06f, 0.17f), bc + V(0, 0.17f, 0.23f), c + V(0, -0.03f, -0.04f)), 0.07f, 0.05f, red, blend: 0f);
        b.Ell(head, c, r, red);
        b.Ell(head, c + V(0, -0.025f, 0.07f), V(0.032f, 0.028f, 0.045f), grey, blend: 0.015f);
        b.Ell(head, c + V(0, -0.01f, 0.115f), V(0.012f, 0.01f, 0.008f), Rgb(52, 50, 64), blend: 0.004f);
        // The great blue mane swept up and back from its brow and down its neck
        var mane = Smooth(3, c + V(0, 0.04f, 0.03f), c + V(0, 0.14f, -0.02f), c + V(0, 0.17f, -0.13f), c + V(0, 0.1f, -0.24f));
        b.Tube(head, mane, 0.05f, 0.025f, blue, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            Frond(b, head, c + V(0.02f * s, 0.12f, -0.04f), c + V(0.1f * s, 0.12f, -0.18f), 0.04f, blue, V(s, 0.3f, 0), 0.3f);
            Frond(b, head, c + V(0.03f * s, 0.05f, -0.04f), c + V(0.07f * s, -0.12f, -0.1f), 0.035f, blue, V(s, 0.2f, -0.3f), 0.3f);
            b.Spike(head, c + V(0.035f * s, 0.035f, -0.02f), c + V(0.06f * s, 0.1f, -0.05f), 0.02f, red, 0.55f);
        });
        if (!crowned)
        {
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.04f * s, c.Y + 0.012f);
                b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(236, 180, 60), glare: true);
            });
            return b;
        }
        // The Crowned Shield: a gold helm over its face, a shield across its chest, gold plates on its shoulders
        var hc = c + V(0, 0.012f, 0.005f);
        var hr = V(0.068f, 0.06f, 0.082f);
        b.Ell(head, hc, hr, gold, mat: Metal);
        b.PaintEll(head, hc + V(0, 0.05f, 0.04f), V(0.015f, 0.025f, 0.03f), blue, soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.042f * s, hc.Y - 0.004f);
            b.Eye(head, at, Outward(hc, hr, at), 0.012f, Rgb(236, 180, 60), glare: true);
            Blade(b, Body, bc + V(0.1f * s, 0.08f, 0.14f), bc + V(0.2f * s, 0.2f, 0.0f), 0.09f, gold, V(s, 0.4f, 0.3f), 0.22f, Metal);
        });
        var sc = bc + V(0, 0.0f, 0.27f);
        b.Ell(Body, sc, V(0.17f, 0.16f, 0.05f), gold, mat: Metal, blend: 0.01f);
        b.PaintEll(Body, sc + V(0, 0, 0.045f), V(0.12f, 0.11f, 0.03f), red);
        b.PaintEll(Body, sc + V(0, 0, 0.05f), V(0.03f, 0.08f, 0.02f), gold, soft: 0.006f);
        return b;
    }

    private static PokeBuilder Zamazenta() => ZamazentaBuild(false);

    // ------------------------------------------------------------------ Eternatus

    private static readonly Color EternaDark = Rgb(54, 40, 92);
    private static readonly Color EternaRed = Rgb(214, 44, 76);
    private static readonly Color EternaPink = Rgb(242, 110, 170);

    /// <summary>Eternatus: a vast dragon like a skeleton floating in the air, its spine dark purple with red spikes along it, red ribs curving from it, a glowing pink core in its chest, a long red head like a beak drawn out forward, great jagged arms raised to the sides and claws hanging below.</summary>
    private static PokeBuilder Eternatus()
    {
        var b = new PokeBuilder("Eternatus", 1f, BodyPlan.Floating, V(0, 0.5f, 0)) { Coat = Shell }.Hover();
        var spine = Smooth(3, V(0, 0.6f, 0.12f), V(0, 0.5f, 0.0f), V(0, 0.46f, -0.12f));
        b.Tube(Body, spine, 0.06f, 0.05f, EternaDark, blend: 0f);
        b.Ell(Body, V(0, 0.5f, 0.07f), V(0.075f, 0.075f, 0.06f), EternaPink, mat: Glow);
        PokeBuilder.Both(s =>
        {
            foreach (var at in new[] { V(0, 0.57f, 0.08f), V(0, 0.51f, 0.0f), V(0, 0.47f, -0.08f) })
                b.Tube(Body, Smooth(2, at + V(0.04f * s, 0, 0), at + V(0.13f * s, -0.03f, 0.02f), at + V(0.14f * s, -0.12f, 0.04f)), 0.016f, 0.008f, EternaRed, blend: 0.004f);
            // Great jagged arms raised to the sides, red claws at their ends
            int arm = b.Arm(s, V(0.06f * s, 0.58f, 0.1f));
            var path = new[] { V(0.05f * s, 0.58f, 0.1f), V(0.22f * s, 0.7f, 0.08f), V(0.3f * s, 0.62f, 0.02f), V(0.42f * s, 0.82f, -0.02f) };
            b.Tube(arm, path, 0.035f, 0.022f, EternaDark, blend: 0.004f);
            foreach (var d in new[] { V(0.02f * s, -0.12f, 0.04f), V(0.06f * s, -0.1f, 0.0f), V(0.0f, -0.1f, -0.03f) })
                b.Spike(arm, path[^1], path[^1] + d, 0.016f, EternaRed, 0.6f);
            b.Spike(arm, path[1], path[1] + V(0.02f * s, 0.1f, -0.02f), 0.02f, EternaRed, 0.5f);
            int leg = b.Leg(s, V(0.05f * s, 0.46f, -0.06f));
            b.Limb(leg, V(0.04f * s, 0.46f, -0.06f), V(0.1f * s, 0.32f, -0.02f), 0.025f, 0.018f, EternaDark);
            foreach (var d in new[] { V(0.0f, -0.08f, 0.04f), V(0.03f * s, -0.08f, 0.0f) })
                b.Spike(leg, V(0.1f * s, 0.32f, -0.02f), V(0.1f * s, 0.32f, -0.02f) + d, 0.014f, EternaRed, 0.6f);
        });
        int tail = b.Tail(V(0, 0.46f, -0.12f));
        var tp = Smooth(3, V(0, 0.46f, -0.12f), V(0, 0.42f, -0.28f), V(0, 0.32f, -0.42f), V(0, 0.2f, -0.48f));
        b.Tube(tail, tp, 0.045f, 0.012f, EternaDark, blend: 0f);
        for (int i = 1; i < tp.Length - 1; i += 2)
            b.Spike(tail, tp[i], tp[i] + V(0, 0.07f, -0.04f), 0.02f, EternaRed, 0.4f);
        for (int i = 0; i < spine.Length; i += 2)
            b.Spike(Body, spine[i], spine[i] + V(0, 0.1f, -0.04f), 0.025f, EternaRed, 0.4f);
        // The long head drawn out forward like a beak
        int head = b.Head(V(0, 0.6f, 0.13f));
        var neck = Smooth(3, V(0, 0.6f, 0.12f), V(0, 0.7f, 0.2f), V(0, 0.76f, 0.3f));
        b.Tube(head, neck, 0.045f, 0.04f, EternaDark, blend: 0f);
        b.Ell(head, V(0, 0.77f, 0.32f), V(0.05f, 0.04f, 0.06f), EternaDark);
        b.Spike(head, V(0, 0.78f, 0.34f), V(0, 0.82f, 0.58f), 0.05f, EternaRed, 0.6f);
        b.Spike(head, V(0, 0.75f, 0.34f), V(0, 0.68f, 0.5f), 0.03f, EternaRed, 0.6f);
        b.Spike(head, V(0, 0.8f, 0.3f), V(0, 0.9f, 0.22f), 0.03f, EternaDark, 0.5f);
        b.PaintEll(head, V(0, 0.79f, 0.42f), V(0.03f, 0.01f, 0.08f), EternaDark, soft: 0.005f);
        return Lift(b);
    }

    /// <summary>Eternatus Eternamax: Eternatus swollen with the power of Galar into a vast pink mass, dark spikes bristling round its rim, its core a white star, its head drawn out to one side and jagged arms rising above.</summary>
    private static PokeBuilder EternatusEternamax()
    {
        var b = new PokeBuilder("Eternatus-Eternamax", 1f, BodyPlan.Floating, V(0, 0.45f, 0)) { Coat = Shell }.Hover();
        var bc = V(0, 0.45f, 0);
        var br = V(0.32f, 0.18f, 0.24f);
        b.Ell(Body, bc, br, EternaPink);
        b.PaintEll(Body, bc + V(0, 0.08f, 0), V(0.3f, 0.12f, 0.22f), Rgb(224, 84, 150));
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f + 0.15f;
            var dir = V(MathF.Cos(a), 0.15f * MathF.Sin(i * 1.9f), MathF.Sin(a));
            var root = bc + dir * br * 0.85f;
            b.Spike(Body, root, root + Vector3.Normalize(dir) * (0.14f + 0.05f * (i % 3)), 0.05f, EternaDark, 0.5f);
        }
        foreach (var (x, z) in new[] { (-0.12f, -0.05f), (0.05f, -0.12f), (0.0f, 0.08f) })
            b.Spike(Body, bc + V(x, 0.12f, z), bc + V(x * 1.4f, 0.3f, z - 0.05f), 0.04f, EternaDark, 0.5f);
        b.Mark(Body, On(bc, br, 0, bc.Y), V(0, 0, 1f), 0.08f, 0.08f, White, MarkShape.Star);
        // Its head drawn out to one side, jagged arms above
        int head = b.Head(bc + V(0.25f, 0.05f, 0.05f));
        var hp = Smooth(3, bc + V(0.24f, 0.05f, 0.05f), bc + V(0.38f, 0.1f, 0.08f), bc + V(0.5f, 0.06f, 0.1f));
        b.Tube(head, hp, 0.06f, 0.04f, EternaDark, blend: 0f);
        b.Spike(head, hp[^1], hp[^1] + V(0.14f, 0.0f, 0.02f), 0.04f, EternaRed, 0.6f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.1f * s, 0.12f, -0.05f));
            var path = new[] { bc + V(0.1f * s, 0.12f, -0.05f), bc + V(0.18f * s, 0.3f, -0.08f), bc + V(0.26f * s, 0.24f, -0.1f), bc + V(0.34f * s, 0.42f, -0.12f) };
            b.Tube(arm, path, 0.04f, 0.022f, EternaDark, blend: 0.004f);
            b.Spike(arm, path[^1], path[^1] + V(0.03f * s, 0.08f, 0), 0.02f, EternaRed, 0.5f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Kubfu and Urshifu

    /// <summary>Kubfu: a little grey bear cub in a fighting stance, its fists up before it, a great fluffy grey head with a pale face set in a fierce frown, a white topknot on its brow and a white streamer of fur trailing from the back of its head.</summary>
    private static PokeBuilder Kubfu()
    {
        var b = new PokeBuilder("Kubfu", 0.5f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var grey = Rgb(170, 170, 178);
        var light = Rgb(216, 216, 222);
        var white = Rgb(246, 246, 248);
        var dark = Rgb(62, 62, 72);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.13f, 0));
            b.Limb(leg, V(0.05f * s, 0.13f, 0), V(0.06f * s, 0.045f, 0.01f), 0.036f, 0.03f, light);
            b.Ell(leg, V(0.06f * s, 0.025f, 0.025f), V(0.036f, 0.025f, 0.045f), light);
            b.PaintEll(leg, V(0.06f * s, 0.02f, 0.062f), V(0.032f, 0.022f, 0.015f), dark, soft: 0.005f);
        });
        var bc = V(0, 0.2f, 0);
        b.Ell(Body, bc, V(0.085f, 0.1f, 0.075f), grey);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.05f), V(0.06f, 0.07f, 0.04f), light);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.075f * s, 0.05f, 0.01f));
            var elbow = bc + V(0.11f * s, 0.0f, 0.05f);
            var fist = bc + V(0.075f * s, 0.05f, 0.11f);
            b.Limb(arm, bc + V(0.07f * s, 0.05f, 0.01f), elbow, 0.03f, 0.026f, grey);
            b.Limb(arm, elbow, fist, 0.026f, 0.024f, grey);
            b.Ell(arm, fist, V(0.032f, 0.03f, 0.03f), light);
            b.PaintEll(arm, fist + V(0, 0, 0.026f), V(0.026f, 0.02f, 0.012f), dark, soft: 0.005f);
        });
        int head = b.Head(bc + V(0, 0.1f, 0));
        var c = bc + V(0, 0.2f, 0.0f);
        var r = V(0.11f, 0.095f, 0.095f);
        b.Ell(head, c, r, grey);
        FurTufts(b, head, c + V(0, -0.01f, -0.01f), V(0.1f, 0.085f, 0.085f), 18, 0.045f, 0.022f, grey, 0.45f, -0.75f, 0.2f);
        var fc = c + V(0, -0.02f, 0.05f);
        var fr = V(0.07f, 0.06f, 0.05f);
        b.Ell(head, fc, fr, light);
        // The white topknot over its brow and the streamer of fur behind
        b.Ell(head, c + V(0, 0.075f, 0.03f), V(0.06f, 0.035f, 0.05f), white);
        foreach (var (x, z) in new[] { (0f, 0.04f), (-0.03f, 0.02f), (0.03f, 0.02f) })
            b.Spike(head, c + V(x, 0.09f, z), c + V(x * 1.6f, 0.15f, z - 0.03f), 0.025f, white, 0.6f);
        Frond(b, head, c + V(0, 0.01f, -0.08f), c + V(0.07f, -0.11f, -0.17f), 0.035f, white, V(0.3f, 0, -1f), 0.3f);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.08f * s, 0.07f, -0.02f), V(0.025f, 0.025f, 0.016f), grey);
            var at = On(fc, fr, 0.026f * s, fc.Y + 0.012f);
            b.Eye(head, at, Outward(fc, fr, at), 0.012f, Rgb(50, 46, 56), glare: true);
            b.PaintEll(head, at + V(0.002f * s, 0.016f, 0.004f), V(0.016f, 0.005f, 0.01f), dark, V(0, 0, -20f * s), 0.004f);
        });
        b.Ell(head, fc + V(0, -0.02f, 0.045f), V(0.014f, 0.01f, 0.008f), dark, blend: 0.004f);
        return b;
    }

    /// <summary>
    /// Urshifu's two styles and their Gigantamax forms. Urshifu is a great bear in a martial artist's robe of fur:
    /// dark grey, black tatters hanging round its waist, a white collar and a white stripe down its front, fists
    /// raised with yellow claws and a pale muzzle under fierce eyes. The Single Strike Style's grey topknot rises like
    /// a flame; the Rapid Strike Style is a lighter grey, its long white hair streaming back. Gigantamax, the Single
    /// Strike Style is red, its robe and topknot flaring into flames, and the Rapid Strike Style white, wrapped in
    /// flowing blue like water; both under Gigantamax's red clouds.
    /// </summary>
    private static PokeBuilder UrshifuBuild(bool rapid, bool gmax)
    {
        string name = (rapid, gmax) switch
        {
            (false, false) => "Urshifu",
            (true, false) => "Urshifu-Rapid-Strike",
            (false, true) => "Urshifu-Single-Strike-Gmax",
            _ => "Urshifu-Rapid-Strike-Gmax"
        };
        var b = new PokeBuilder(name, 0.95f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Fur };
        Color body, robe, hair, limb, trim, muzzle;
        if (!gmax)
        {
            body = rapid ? Rgb(100, 100, 110) : Rgb(64, 64, 72);
            robe = Rgb(36, 36, 44);
            hair = rapid ? Rgb(242, 242, 246) : Rgb(150, 150, 160);
            limb = body;
            trim = Rgb(238, 238, 242);
            muzzle = Rgb(140, 140, 150);
        }
        else if (!rapid)
        {
            body = Rgb(150, 40, 54);
            robe = Rgb(222, 52, 62);
            hair = Rgb(236, 70, 64);
            limb = Rgb(236, 230, 234);
            trim = Rgb(250, 214, 208);
            muzzle = Rgb(236, 220, 220);
        }
        else
        {
            body = Rgb(226, 226, 236);
            robe = Rgb(72, 70, 184);
            hair = Rgb(100, 96, 210);
            limb = Rgb(236, 236, 244);
            trim = Rgb(150, 148, 232);
            muzzle = Rgb(240, 240, 246);
        }
        var claw = Rgb(234, 214, 96);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.32f, 0));
            b.Ell(leg, V(0.1f * s, 0.27f, 0.0f), V(0.08f, 0.1f, 0.08f), limb);
            b.Limb(leg, V(0.11f * s, 0.2f, 0.01f), V(0.11f * s, 0.06f, 0.02f), 0.055f, 0.045f, limb);
            b.Ell(leg, V(0.11f * s, 0.035f, 0.05f), V(0.055f, 0.035f, 0.07f), limb);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.11f * s + t * 0.025f, 0.03f, 0.1f), V(0.11f * s + t * 0.03f, 0.012f, 0.135f), 0.012f, claw, mat: Shell, blend: 0.004f);
            if (gmax) b.PaintTorus(leg, V(0.11f * s, 0.14f, 0.015f), 0.05f, 0.012f, robe);
        });
        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc, V(0.17f, 0.2f, 0.13f), body);
        b.PaintEll(Body, bc + V(0, 0.13f, 0.08f), V(0.11f, 0.06f, 0.07f), trim);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.12f), V(0.03f, 0.16f, 0.03f), trim);
        // The robe's tatters round its waist, and down its shoulders
        for (int i = 0; i < 14; i++)
        {
            float a = i * MathF.Tau / 14f;
            var root = bc + V(MathF.Sin(a) * 0.15f, -0.11f, MathF.Cos(a) * 0.11f);
            if (MathF.Cos(a) > 0.85f) continue;
            float flare = gmax ? 0.08f : 0.05f;
            b.Spike(Body, root, root + V(MathF.Sin(a) * flare, -0.15f - 0.03f * (i % 2), MathF.Cos(a) * flare * 0.7f), 0.04f, robe, 0.45f);
        }
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
            {
                var root = bc + V((0.12f + 0.02f * i) * s, 0.12f - 0.05f * i, -0.05f);
                var tip = gmax && !rapid ? root + V(0.1f * s, 0.18f, -0.06f) : root + V(0.06f * s, -0.12f, -0.05f);
                b.Spike(Body, root, tip, 0.035f, robe, 0.45f);
            }
            int arm = b.Arm(s, bc + V(0.16f * s, 0.12f, 0));
            var elbow = bc + V(0.23f * s, -0.02f, 0.06f);
            var fist = bc + V(0.12f * s, 0.05f, 0.2f);
            b.Limb(arm, bc + V(0.15f * s, 0.12f, 0.0f), elbow, 0.06f, 0.05f, body);
            b.Limb(arm, elbow, fist, 0.05f, 0.045f, gmax ? limb : body);
            b.Ell(arm, fist, V(0.05f, 0.045f, 0.05f), gmax ? limb : body);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, fist + V(t * 0.025f, 0.02f, 0.035f), fist + V(t * 0.03f, 0.0f, 0.075f), 0.012f, claw, mat: Shell, blend: 0.004f);
        });
        int head = b.Head(bc + V(0, 0.2f, 0.03f));
        var c = bc + V(0, 0.29f, 0.06f);
        var r = V(0.09f, 0.085f, 0.09f);
        b.Limb(head, bc + V(0, 0.14f, 0.02f), c + V(0, -0.04f, -0.01f), 0.075f, 0.065f, body);
        b.Ell(head, c, r, body);
        b.Ell(head, c + V(0, -0.03f, 0.07f), V(0.045f, 0.035f, 0.04f), muzzle, blend: 0.015f);
        b.Ell(head, c + V(0, -0.015f, 0.105f), V(0.014f, 0.01f, 0.008f), Rgb(40, 40, 46), blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.07f * s, 0.07f, -0.02f), V(0.025f, 0.025f, 0.016f), body);
            var at = On(c, r, 0.04f * s, c.Y + 0.02f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(250, 228, 120), glare: true);
            b.PaintEll(head, at + V(0, 0.005f, 0.002f), V(0.03f, 0.022f, 0.02f), robe, soft: 0.006f);
        });
        if (!rapid)
        {
            // The Single Strike Style's topknot, rising like a flame
            float k = gmax ? 1.5f : 1f;
            b.Spike(head, c + V(0, 0.07f, -0.02f), c + V(0, 0.07f + 0.16f * k, -0.08f), 0.05f, hair, 0.6f);
            PokeBuilder.Both(s => b.Spike(head, c + V(0.03f * s, 0.07f, -0.03f), c + V(0.07f * s, 0.07f + 0.11f * k, -0.09f), 0.035f, hair, 0.6f));
            if (gmax)
                PokeBuilder.Both(s =>
                {
                    for (int i = 0; i < 3; i++)
                        b.Spike(Body, bc + V((0.06f + 0.04f * i) * s, 0.16f, -0.08f), bc + V((0.1f + 0.08f * i) * s, 0.36f - 0.06f * i, -0.16f), 0.045f, i % 2 == 0 ? robe : hair, 0.5f);
                });
        }
        else
        {
            // The Rapid Strike Style's long hair, streaming back
            float k = gmax ? 1.4f : 1f;
            b.Tube(head, Smooth(3, c + V(0, 0.08f, -0.02f), c + V(0, 0.13f, -0.12f) * 1f, c + V(0, 0.03f, -0.22f * k), c + V(0, -0.12f * k, -0.26f * k)), 0.04f, 0.015f, hair, blend: 0f);
            PokeBuilder.Both(s => Frond(b, head, c + V(0.02f * s, 0.09f, -0.05f), c + V(0.08f * s, 0.0f, -0.2f * k), 0.035f, hair, V(s, 0.3f, 0), 0.3f));
            if (gmax)
                PokeBuilder.Both(s =>
                {
                    var wave = Smooth(3, bc + V(0.14f * s, 0.1f, -0.04f), bc + V(0.26f * s, 0.0f, -0.1f), bc + V(0.24f * s, -0.18f, -0.04f), bc + V(0.3f * s, -0.32f, 0.02f));
                    b.Tube(Body, wave, 0.045f, 0.015f, robe, blend: 0.01f);
                });
        }
        if (gmax) MaxClouds(b, head, c + V(0, 0.08f, -0.06f), 0.035f, 0.12f, 0.22f, 1.1f);
        return b;
    }

    private static PokeBuilder Urshifu() => UrshifuBuild(false, false);

    // ------------------------------------------------------------------ Zarude

    /// <summary>
    /// Zarude and its Dada. Zarude is a black ape of the jungle crouched to spring: long arms, one raised high with
    /// great claws, green vines wound round that forearm and round one shin, a grey mask of a face with red eyes and
    /// white fangs, spiky dark hair swept back like ears, grey stripes across its chest and a long tail ending in a
    /// leaf-like spade. Dada wears a pink cape tied at its neck.
    /// </summary>
    private static PokeBuilder ZarudeBuild(bool dada)
    {
        var b = new PokeBuilder(dada ? "Zarude-Dada" : "Zarude", 0.95f, BodyPlan.Biped, V(0, 0.42f, -0.02f)) { Coat = Fur };
        var black = Rgb(44, 44, 52);
        var grey = Rgb(92, 94, 102);
        var vine = Rgb(76, 138, 62);
        var nail = Rgb(70, 70, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.36f, -0.04f));
            var knee = V(0.16f * s, 0.25f, 0.08f);
            var ankle = V(0.15f * s, 0.07f, -0.03f);
            b.Limb(leg, V(0.09f * s, 0.36f, -0.04f), knee, 0.06f, 0.045f, black);
            b.Limb(leg, knee, ankle, 0.045f, 0.035f, black);
            b.Ell(leg, V(0.15f * s, 0.03f, 0.02f), V(0.045f, 0.03f, 0.08f), black);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.15f * s + t * 0.025f, 0.025f, 0.09f), V(0.15f * s + t * 0.03f, 0.01f, 0.125f), 0.011f, nail, mat: Shell, blend: 0.004f);
            if (s < 0) Coils(b, leg, Vector3.Lerp(knee, ankle, 0.2f), Vector3.Lerp(knee, ankle, 0.85f), 4, 0.04f, 0.014f, vine);
        });
        var bc = V(0, 0.42f, -0.02f);
        b.Ell(Body, bc, V(0.12f, 0.15f, 0.1f), black, V(20f, 0, 0));
        for (int i = 0; i < 3; i++)
            b.PaintEll(Body, bc + V(0, 0.07f - i * 0.05f, 0.1f), V(0.07f - 0.01f * i, 0.008f, 0.03f), grey, soft: 0.005f);
        int tail = b.Tail(bc + V(0, -0.08f, -0.09f));
        var tp = Smooth(3, bc + V(0, -0.08f, -0.09f), bc + V(0, -0.2f, -0.2f), bc + V(0, -0.22f, -0.34f), bc + V(0, -0.12f, -0.44f));
        b.Tube(tail, tp, 0.028f, 0.016f, black, blend: 0f);
        Blade(b, tail, tp[^1], tp[^1] + V(0, 0.08f, -0.04f), 0.045f, black, V(1f, 0, 0), 0.25f);
        PokeBuilder.Both(s =>
        {
            // The right arm raised high, vines round its forearm; the left down on its knuckles
            int arm = b.Arm(s, bc + V(0.11f * s, 0.1f, 0.04f));
            var elbow = s > 0 ? bc + V(0.22f, 0.18f, 0.1f) : bc + V(-0.19f, -0.06f, 0.1f);
            var hand = s > 0 ? bc + V(0.2f, 0.38f, 0.1f) : bc + V(-0.21f, -0.33f, 0.16f);
            b.Limb(arm, bc + V(0.1f * s, 0.1f, 0.04f), elbow, 0.045f, 0.035f, black);
            b.Limb(arm, elbow, hand, 0.035f, 0.03f, black);
            b.Ell(arm, hand, V(0.04f, 0.04f, 0.035f), grey);
            var reach = Vector3.Normalize(hand - elbow);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand + reach * 0.02f + V(t * 0.025f, 0, 0.01f), hand + reach * 0.075f + V(t * 0.035f, 0, 0.02f), 0.012f, nail, mat: Shell, blend: 0.004f);
            if (s > 0) Coils(b, arm, Vector3.Lerp(elbow, hand, 0.25f), Vector3.Lerp(elbow, hand, 0.9f), 4, 0.038f, 0.014f, vine);
        });
        int head = b.Head(bc + V(0, 0.14f, 0.08f));
        var c = bc + V(0, 0.22f, 0.14f);
        var r = V(0.08f, 0.075f, 0.075f);
        b.Limb(head, bc + V(0, 0.09f, 0.05f), c + V(0, -0.03f, -0.03f), 0.055f, 0.05f, black);
        b.Ell(head, c, r, black);
        b.PaintEll(head, c + V(0, -0.01f, 0.05f), V(0.06f, 0.05f, 0.04f), grey);
        b.Ell(head, c + V(0, -0.035f, 0.06f), V(0.035f, 0.025f, 0.03f), grey, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.015f * s, -0.045f, 0.075f), c + V(0.02f * s, -0.075f, 0.08f), 0.008f, White, mat: Shell, blend: 0.003f);
            var at = On(c, r, 0.035f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(226, 64, 40), glare: true);
            // Spiky dark hair swept back like ears
            b.Spike(head, c + V(0.05f * s, 0.04f, -0.02f), c + V(0.14f * s, 0.12f, -0.07f), 0.03f, black, 0.5f);
            b.Spike(head, c + V(0.06f * s, 0.0f, -0.03f), c + V(0.15f * s, 0.02f, -0.1f), 0.025f, black, 0.5f);
        });
        b.Spike(head, c + V(0, 0.06f, -0.02f), c + V(0, 0.13f, -0.1f), 0.03f, black, 0.5f);
        if (dada)
        {
            // Dada's pink cape tied at its neck
            var pink = Rgb(222, 92, 170);
            var neck = bc + V(0, 0.14f, 0.04f);
            b.Torus(Body, neck, 0.06f, 0.018f, pink, V(30f, 0, 0), mat: Fur);
            Frond(b, Body, neck + V(0, 0.0f, -0.06f), neck + V(0.03f, -0.22f, -0.18f), 0.09f, pink, V(0, 0.3f, -1f), 0.15f);
            b.Ell(Body, neck + V(0.03f, -0.01f, 0.06f), V(0.025f, 0.02f, 0.015f), pink);
        }
        return b;
    }

    private static PokeBuilder Zarude() => ZarudeBuild(false);

    // ------------------------------------------------------------------ Regieleki and Regidrago

    /// <summary>Regieleki: a giant of electricity floating in the air: a lime-yellow body banded with blue rings, a coiled spring for its lower half, a pattern of pink dots on its front, and to each side a great wing of crackling lightning.</summary>
    private static PokeBuilder Regieleki()
    {
        var b = new PokeBuilder("Regieleki", 1f, BodyPlan.Floating, V(0, 0.45f, 0)) { Coat = Metal }.Hover();
        var yellow = Rgb(232, 238, 92);
        var blue = Rgb(52, 70, 172);
        var grey = Rgb(110, 116, 144);
        var pink = Rgb(232, 110, 150);
        var spark = Rgb(248, 250, 176);
        var bc = V(0, 0.45f, 0);
        var br = V(0.08f, 0.14f, 0.075f);
        b.Ell(Body, bc, br, yellow);
        b.Torus(Body, bc + V(0, 0.05f, 0), 0.075f, 0.018f, blue, mat: Metal);
        b.Torus(Body, bc + V(0, 0.13f, 0), 0.04f, 0.014f, blue, mat: Metal);
        // The coiled spring below
        b.Spike(Body, bc + V(0, -0.11f, 0), bc + V(0, -0.27f, 0), 0.046f, grey, mat: Metal);
        for (int i = 0; i < 4; i++)
            b.Torus(Body, bc + V(0, -0.14f - i * 0.025f, 0), 0.05f - i * 0.007f, 0.012f, blue, V(8f, 0, 0), mat: Metal);
        // The pink dots on its front
        foreach (var (x, y) in new[] { (0f, 0.0f), (-0.025f, -0.03f), (0.025f, -0.03f), (-0.025f, 0.03f), (0.025f, 0.03f) })
        {
            var at = On(bc, br, x, bc.Y - 0.04f + y);
            b.Mark(Body, at, Outward(bc, br, at), 0.009f, 0.009f, pink);
        }
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, bc + V(0.07f * s, 0.0f, 0), V(0.03f, 0.055f, 0.045f), blue, mat: Metal);
            int wing = b.Wing(s, bc + V(0.08f * s, 0.0f, 0));
            var root = bc + V(0.06f * s, 0.0f, 0);
            var tip = bc + V(0.46f * s, 0.06f, -0.03f);
            Frond(b, wing, root, tip, 0.12f, yellow, V(0, 0, 1f), 0.15f, Glow);
            foreach (float off in new[] { -0.05f, 0.0f, 0.05f })
                Frond(b, wing, root + V(0.06f * s, off, 0.008f), tip + V(-0.08f * s, off * 0.6f, 0.008f), 0.02f, spark, V(0, 0, 1f), 0.3f, Glow, 0.006f);
            foreach (var (t, up) in new[] { (0.45f, 1f), (0.7f, -1f), (0.85f, 1f) })
            {
                var at = Vector3.Lerp(root, tip, t) + V(0, up * 0.07f * (1f - t * 0.5f), 0);
                b.Spike(wing, at, at + V(0.04f * s, up * 0.06f, 0), 0.016f, spark, 0.5f, Glow);
            }
        });
        return Lift(b);
    }

    /// <summary>Regidrago: a giant made in the shape of a dragon: a round pink-red body spotted with blue dots, a dark green crest on top shaped like a dragon's head, and for arms two great dark green dragon heads, their jaws open on jagged red.</summary>
    private static PokeBuilder Regidrago()
    {
        var b = new PokeBuilder("Regidrago", 1f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Shell };
        var red = Rgb(214, 42, 92);
        var green = Rgb(40, 76, 66);
        var maw = Rgb(150, 30, 62);
        var blue = Rgb(84, 156, 228);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.24f, 0));
            b.Limb(leg, V(0.08f * s, 0.25f, 0), V(0.09f * s, 0.07f, 0.02f), 0.05f, 0.045f, green);
            b.Ell(leg, V(0.09f * s, 0.04f, 0.04f), V(0.055f, 0.04f, 0.07f), green);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(leg, V(0.09f * s + t * 0.025f, 0.03f, 0.09f), V(0.09f * s + t * 0.03f, 0.012f, 0.125f), 0.013f, green, mat: Shell, blend: 0.004f);
        });
        var bc = V(0, 0.42f, 0);
        var br = V(0.16f, 0.17f, 0.14f);
        b.Ell(Body, bc, br, red);
        foreach (var (x, y) in new[] { (0f, 0.03f), (-0.04f, 0.0f), (0.04f, 0.0f), (-0.04f, 0.06f), (0.04f, 0.06f), (0f, -0.03f), (0f, 0.09f) })
        {
            var at = On(bc, br, x, bc.Y + y);
            b.Mark(Body, at, Outward(bc, br, at), 0.012f, 0.012f, blue);
        }
        // The crest on top, shaped like a dragon's head
        int head = b.Head(bc + V(0, 0.14f, 0));
        var hc = bc + V(0, 0.19f, -0.02f);
        b.Ell(head, hc, V(0.12f, 0.08f, 0.12f), green, V(-15f, 0, 0));
        b.Spike(head, hc + V(0, 0.02f, 0.05f), hc + V(0, 0.12f, 0.2f), 0.07f, green, 0.6f);
        PokeBuilder.Both(s => b.Spike(head, hc + V(0.06f * s, 0.04f, -0.06f), hc + V(0.11f * s, 0.15f, -0.18f), 0.035f, green, 0.5f));
        // The arms, each a dragon's head with its jaws open
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.15f * s, 0.06f, 0));
            b.Limb(arm, bc + V(0.13f * s, 0.06f, 0), bc + V(0.24f * s, 0.0f, 0.02f), 0.05f, 0.05f, green);
            var jc = bc + V(0.33f * s, -0.01f, 0.03f);
            b.Ell(arm, jc + V(0, 0.05f, 0), V(0.1f, 0.05f, 0.08f), green, V(0, 0, -15f * s));
            b.Ell(arm, jc + V(0, -0.05f, 0), V(0.1f, 0.035f, 0.07f), green, V(0, 0, 10f * s));
            b.Ell(arm, jc, V(0.085f, 0.035f, 0.055f), maw, blend: 0.01f);
            for (int t = 0; t < 4; t++)
            {
                var at = jc + V((0.04f - 0.03f * t) * s + 0.02f * s, 0, 0.0f);
                b.Spike(arm, at + V(0, 0.03f, 0), at + V(0.01f * s, -0.005f, 0), 0.013f, red, 0.5f, Shell, 0.004f);
            }
            b.Spike(arm, jc + V(0, 0.08f, -0.02f), jc + V(0.06f * s, 0.16f, -0.06f), 0.03f, green, 0.5f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Glastrier, Spectrier and Calyrex

    /// <summary>A horse's four long legs: hips <paramref name="x"/> apart at <paramref name="hip"/>, front and back pairs at <paramref name="front"/> and <paramref name="back"/>, <paramref name="thigh"/> thick, its hooves <paramref name="hoof"/>; each leg's bone is handed to <paramref name="more"/> to add to.</summary>
    private static void HorseLegs(PokeBuilder b, float x, float hip, float front, float back, float thigh, Color coat, Color low, Color hoof, Action<int, float, float, Vector3>? more = null)
    {
        foreach (var (z, isFront) in new[] { (front, true), (back, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(x * s, hip, z), isFront);
                var knee = V(x * 1.05f * s, hip * 0.52f, z + (isFront ? 0.02f : -0.04f));
                var ankle = V(x * 1.05f * s, 0.08f, z + 0.01f);
                b.Limb(leg, V(x * s, hip + 0.01f, z), knee, thigh, thigh * 0.72f, coat);
                b.Limb(leg, knee, ankle, thigh * 0.7f, thigh * 0.6f, low);
                b.Ell(leg, V(x * 1.05f * s, 0.035f, z + 0.015f), V(thigh * 0.8f, 0.035f, thigh * 0.9f), hoof);
                more?.Invoke(leg, s, z, ankle);
            });
    }

    /// <summary>
    /// Calyrex as it rides, smaller and on the horse's back at <paramref name="seat"/>: its great bulb of a head,
    /// a pale face under a crown of pale leaves, a collar of round beads with pale leaves hanging from it, and its
    /// thin pale legs astride; it holds reins to the horse's muzzle at <paramref name="mouth"/>.
    /// </summary>
    private static void CalyrexRider(PokeBuilder b, int bone, Vector3 seat, Color bulb, Vector3 mouth)
    {
        var pale = Rgb(234, 230, 218);
        var bead = Rgb(56, 124, 126);
        b.Ell(bone, seat + V(0, 0.05f, 0), V(0.04f, 0.065f, 0.035f), pale);
        PokeBuilder.Both(s => b.Limb(bone, seat + V(0.02f * s, 0.01f, 0.0f), seat + V(0.1f * s, -0.08f, 0.04f), 0.014f, 0.01f, pale));
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            b.Ell(bone, seat + V(MathF.Sin(a) * 0.045f, 0.11f, MathF.Cos(a) * 0.04f), V(0.016f, 0.016f, 0.016f), i % 2 == 0 ? bead : Rgb(36, 80, 84), blend: 0.006f);
        }
        foreach (float a in new[] { -1.2f, -0.4f, 0.4f, 1.2f })
            Frond(b, bone, seat + V(MathF.Sin(a) * 0.04f, 0.1f, MathF.Cos(a) * 0.04f), seat + V(MathF.Sin(a) * 0.07f, 0.04f, MathF.Cos(a) * 0.06f), 0.016f, pale, V(MathF.Sin(a), 0, MathF.Cos(a)), 0.3f);
        var hc = seat + V(0, 0.2f, 0.0f);
        var hr = V(0.085f, 0.075f, 0.075f);
        b.Ell(bone, hc, hr, bulb);
        b.PaintEll(bone, hc + V(-0.03f, 0.04f, 0.03f), V(0.03f, 0.02f, 0.03f), PixelCanvas.Mix(bulb, White, 0.4f), soft: 0.006f);
        var fc = hc + V(0, -0.06f, 0.035f);
        var fr = V(0.05f, 0.043f, 0.04f);
        b.Ell(bone, fc, fr, pale);
        foreach (float x in new[] { -0.03f, -0.01f, 0.01f, 0.03f })
            b.Spike(bone, fc + V(x, 0.02f, 0.012f), fc + V(x * 1.4f, 0.06f, 0.025f), 0.012f, pale, 0.5f, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, fc.X + 0.019f * s, fc.Y - 0.004f);
            b.Eye(bone, at, Outward(fc, fr, at), 0.0095f, Rgb(70, 130, 190));
            var hand = seat + V(0.04f * s, 0.06f, 0.08f);
            b.Limb(bone, seat + V(0.03f * s, 0.09f, 0.01f), hand, 0.01f, 0.009f, pale);
            b.Tube(bone, Smooth(2, hand, Vector3.Lerp(hand, mouth + V(0.05f * s, 0, 0), 0.5f) + V(0, -0.04f, 0), mouth + V(0.05f * s, 0, 0)), 0.006f, 0.006f, Rgb(70, 90, 190), blend: 0.004f);
        });
    }

    /// <summary>
    /// Glastrier, and Calyrex riding it. Glastrier is a heavy white war horse of ice, its face behind a mask of pale
    /// blue ice with crystals rising from it, crystals for a mane and a tail, and great clusters of ice crystals
    /// round its hooves. The Ice Rider sits on its back, its head a bulb of pale teal.
    /// </summary>
    private static PokeBuilder GlastrierBuild(bool rider)
    {
        var b = new PokeBuilder(rider ? "Calyrex-Ice" : "Glastrier", 1f, BodyPlan.Quadruped, V(0, 0.55f, -0.02f)) { Coat = Fur };
        var white = Rgb(240, 242, 248);
        var ice = Rgb(172, 202, 240);
        var deep = Rgb(116, 152, 222);
        var shade = Rgb(206, 214, 242);
        HorseLegs(b, 0.085f, 0.48f, 0.2f, -0.22f, 0.075f, white, shade, ice, (leg, s, z, ankle) =>
        {
            for (int k = 0; k < 6; k++)
            {
                float a = k * MathF.Tau / 6f;
                var root = V(0.089f * s + MathF.Cos(a) * 0.04f, 0.06f, z + 0.015f + MathF.Sin(a) * 0.04f);
                b.Spike(leg, root, root + V(MathF.Cos(a) * 0.045f, 0.08f + 0.03f * (k % 2), MathF.Sin(a) * 0.045f), 0.022f, k % 2 == 0 ? ice : deep, 0.6f, Shell);
            }
        });
        var bc = V(0, 0.55f, -0.02f);
        var br = V(0.15f, 0.15f, 0.28f);
        b.Ell(Body, bc, br, white);
        b.Ell(Body, bc + V(0, 0.02f, 0.17f), V(0.15f, 0.16f, 0.12f), white, blend: 0.04f);
        b.PaintEll(Body, bc + V(0, -0.1f, 0), V(0.1f, 0.05f, 0.22f), shade);
        int tail = b.Tail(bc + V(0, 0.06f, -0.26f));
        foreach (var d in new[] { V(0, -0.1f, -0.1f), V(0.04f, -0.16f, -0.06f), V(-0.04f, -0.16f, -0.06f), V(0, -0.2f, -0.03f) })
            b.Spike(tail, bc + V(0, 0.06f, -0.26f), bc + V(0, 0.06f, -0.26f) + d, 0.03f, ice, 0.6f, Shell);
        int head = b.Head(bc + V(0, 0.1f, 0.2f));
        var c = bc + V(0, 0.33f, 0.33f);
        var r = V(0.065f, 0.065f, 0.1f);
        b.Tube(head, Smooth(3, bc + V(0, 0.06f, 0.19f), bc + V(0, 0.2f, 0.25f), c + V(0, -0.03f, -0.07f)), 0.085f, 0.06f, white, blend: 0f);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.035f, 0.09f), V(0.045f, 0.04f, 0.055f), white, blend: 0.02f);
        // The mask of ice over its face, crystals rising from it and down its neck
        var mc = c + V(0, 0.02f, 0.03f);
        var mr = V(0.072f, 0.055f, 0.11f);
        b.Ell(head, mc, mr, ice, mat: Shell, blend: 0.008f);
        b.Spike(head, mc + V(0, 0.04f, 0.04f), mc + V(0, 0.16f, 0.1f), 0.035f, ice, 0.6f, Shell);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, mc + V(0.04f * s, 0.04f, -0.02f), mc + V(0.08f * s, 0.15f, -0.03f), 0.03f, ice, 0.6f, Shell);
            b.Spike(head, mc + V(0.05f * s, 0.02f, -0.05f), mc + V(0.12f * s, 0.07f, -0.1f), 0.025f, deep, 0.6f, Shell);
            var at = Out(mc, mr, default, V(0.75f * s, 0.15f, 0.6f));
            b.Eye(head, at, Outward(mc, mr, at), 0.013f, Rgb(60, 56, 96), glare: true);
        });
        for (int i = 0; i < 4; i++)
        {
            var at = Vector3.Lerp(c + V(0, 0.02f, -0.08f), bc + V(0, 0.12f, 0.17f), i / 3f);
            b.Spike(head, at, at + V(0, 0.09f, -0.06f), 0.03f, i % 2 == 0 ? ice : deep, 0.6f, Shell);
        }
        if (rider) CalyrexRider(b, Body, bc + V(0, br.Y + 0.01f, 0.03f), Rgb(122, 204, 184), c + V(0, -0.04f, 0.12f));
        return b;
    }

    private static PokeBuilder Glastrier() => GlastrierBuild(false);

    /// <summary>
    /// Spectrier, and Calyrex riding it. Spectrier is a slender black horse of the dead, its long mane and tail
    /// streaming like purple smoke, its legs fading purple to its hooves and its eyes glowing white. The Shadow Rider
    /// sits on its back, its head a bulb of pale teal.
    /// </summary>
    private static PokeBuilder SpectrierBuild(bool rider)
    {
        var b = new PokeBuilder(rider ? "Calyrex-Shadow" : "Spectrier", 1f, BodyPlan.Quadruped, V(0, 0.55f, -0.02f)) { Coat = Fur };
        var black = Rgb(40, 40, 54);
        var purple = Rgb(104, 76, 182);
        var lilac = Rgb(196, 180, 234);
        HorseLegs(b, 0.065f, 0.48f, 0.2f, -0.22f, 0.045f, black, Rgb(66, 54, 110), purple);
        var bc = V(0, 0.55f, -0.02f);
        var br = V(0.11f, 0.11f, 0.26f);
        b.Ell(Body, bc, br, black);
        b.Ell(Body, bc + V(0, 0.02f, 0.16f), V(0.11f, 0.12f, 0.1f), black, blend: 0.04f);
        int tail = b.Tail(bc + V(0, 0.05f, -0.25f));
        foreach (var (x, k) in new[] { (0f, 1f), (0.03f, 0.85f), (-0.03f, 0.9f) })
            b.Tube(tail, Smooth(3, bc + V(x, 0.05f, -0.25f), bc + V(x * 2f, 0.0f, -0.36f), bc + V(x * 3f, -0.14f * k, -0.38f), bc + V(x * 4f, -0.26f * k, -0.48f), bc + V(x * 4f, -0.36f * k, -0.44f)), 0.03f, 0.01f, x == 0 ? lilac : purple, blend: 0.004f);
        int head = b.Head(bc + V(0, 0.1f, 0.2f));
        var c = bc + V(0, 0.32f, 0.32f);
        var r = V(0.055f, 0.06f, 0.095f);
        b.Tube(head, Smooth(3, bc + V(0, 0.06f, 0.19f), bc + V(0, 0.19f, 0.24f), c + V(0, -0.03f, -0.06f)), 0.07f, 0.05f, black, blend: 0f);
        b.Ell(head, c, r, black);
        b.Ell(head, c + V(0, -0.035f, 0.085f), V(0.04f, 0.035f, 0.05f), black, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.03f * s, 0.05f, -0.04f), c + V(0.045f * s, 0.1f, -0.07f), 0.016f, black, 0.6f);
            var at = Out(c, r, default, V(0.7f * s, 0.25f, 0.65f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, white: Rgb(236, 236, 252), pupil: Rgb(120, 96, 200));
            // The mane streaming down its neck like smoke
            for (int i = 0; i < 3; i++)
            {
                var root = c + V(0.025f * s, 0.05f - 0.03f * i, -0.06f - 0.05f * i);
                b.Tube(head, Smooth(3, root, root + V(0.06f * s, -0.08f, -0.06f), root + V(0.05f * s, -0.18f, -0.04f), root + V(0.09f * s, -0.28f, -0.08f)), 0.022f, 0.008f, i == 1 ? lilac : purple, blend: 0.004f);
            }
        });
        b.Tube(head, Smooth(3, c + V(0, 0.06f, -0.02f), c + V(0, 0.08f, 0.04f), c + V(0.03f, 0.02f, 0.09f), c + V(0.05f, -0.06f, 0.1f)), 0.018f, 0.008f, purple, blend: 0.004f);
        if (rider) CalyrexRider(b, Body, bc + V(0, br.Y + 0.01f, 0.03f), Rgb(112, 194, 180), c + V(0, -0.04f, 0.12f));
        return b;
    }

    private static PokeBuilder Spectrier() => SpectrierBuild(false);

    /// <summary>Calyrex: a small king of the fields floating in the air, its head a great dark green bulb, a pale face beneath it under a crown of pale leaves, a collar of round beads with pale leaves hanging from it, a pale gown, thin pale arms and two long thin legs.</summary>
    private static PokeBuilder Calyrex()
    {
        var b = new PokeBuilder("Calyrex", 0.6f, BodyPlan.Floating, V(0, 0.45f, 0)) { Coat = Leaf }.Hover();
        var bulb = Rgb(36, 92, 70);
        var pale = Rgb(234, 230, 218);
        var bead = Rgb(56, 124, 126);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.36f, 0));
            b.Limb(leg, V(0.025f * s, 0.38f, 0), V(0.03f * s, 0.04f, 0.0f), 0.016f, 0.008f, pale);
        });
        var bc = V(0, 0.45f, 0);
        b.Ell(Body, bc, V(0.06f, 0.09f, 0.05f), pale);
        b.Spike(Body, bc + V(0, -0.02f, 0), bc + V(0, -0.12f, 0), 0.055f, pale, 0.85f);
        // The collar of beads with pale leaves hanging from it
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            b.Ell(Body, bc + V(MathF.Sin(a) * 0.065f, 0.08f, MathF.Cos(a) * 0.055f), V(0.022f, 0.022f, 0.022f), i % 2 == 0 ? bead : Rgb(36, 80, 84), blend: 0.006f);
        }
        foreach (float a in new[] { -2.2f, -1.4f, -0.6f, 0.0f, 0.6f, 1.4f, 2.2f })
            Frond(b, Body, bc + V(MathF.Sin(a) * 0.06f, 0.06f, MathF.Cos(a) * 0.05f), bc + V(MathF.Sin(a) * 0.1f, -0.06f, MathF.Cos(a) * 0.085f), 0.025f, pale, V(MathF.Sin(a), 0, MathF.Cos(a)), 0.3f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.05f * s, 0.05f, 0.0f));
            var hand = bc + V(0.11f * s, -0.04f, 0.05f);
            b.Limb(arm, bc + V(0.045f * s, 0.05f, 0.0f), hand, 0.014f, 0.012f, pale);
            Frond(b, arm, hand, hand + V(0.02f * s, -0.04f, 0.02f), 0.018f, pale, V(s, 0, 0.3f), 0.3f);
        });
        // The great bulb of a head, the pale face beneath it under its crown of leaves
        int head = b.Head(bc + V(0, 0.1f, 0));
        var hc = bc + V(0, 0.28f, -0.01f);
        b.Ell(head, hc, V(0.17f, 0.15f, 0.15f), bulb);
        b.Spike(head, hc + V(0, 0.1f, -0.02f), hc + V(0, 0.19f, -0.04f), 0.05f, bulb, 0.8f);
        b.PaintEll(head, hc + V(-0.06f, 0.07f, 0.06f), V(0.05f, 0.04f, 0.05f), Rgb(70, 140, 106));
        b.PaintEll(head, hc + V(0, 0.0f, 0.14f), V(0.02f, 0.14f, 0.03f), Rgb(26, 70, 54), soft: 0.008f);
        var fc = bc + V(0, 0.17f, 0.07f);
        var fr = V(0.06f, 0.05f, 0.05f);
        b.Limb(head, bc + V(0, 0.05f, 0.0f), fc + V(0, -0.02f, -0.03f), 0.032f, 0.032f, pale);
        b.Ell(head, fc, fr, pale);
        foreach (float x in new[] { -0.045f, -0.015f, 0.015f, 0.045f })
            b.Spike(head, fc + V(x, 0.03f, 0.015f), fc + V(x * 1.3f, 0.09f, 0.035f), 0.017f, pale, 0.5f, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.022f * s, fc.Y - 0.004f);
            b.Eye(head, at, Outward(fc, fr, at), 0.011f, Rgb(70, 130, 190));
        });
        b.Ell(head, fc + V(0, 0.01f, 0.048f), V(0.008f, 0.008f, 0.005f), Rgb(214, 70, 90), blend: 0.003f);
        return Lift(b);
    }
}
