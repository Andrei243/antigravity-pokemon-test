using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Galar's third batch in National Pokédex
// order: Hatenna (856) to Morpeko (877). Their forms are in PokemonModels.Regional.cs, PokemonModels.Megas.cs and
// PokemonModels.Gigantamax.cs with the other forms. Helpers shared with the earlier batches are in the files of those
// batches, PokemonModels.Sinnoh1.cs to PokemonModels.Galar2.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Hatenna line

    private static readonly Color HatBlue = Rgb(164, 204, 232);
    private static readonly Color HatPink = Rgb(244, 196, 210);
    private static readonly Color HatLight = Rgb(214, 236, 248);

    /// <summary>Hatenna: a little pink creature under a great mop of pale blue hair that hangs to the ground on both sides, a striped pink cap on top with a blue bobble on its bent tip, and a small face peeping out below the fringe.</summary>
    private static PokeBuilder Hatenna()
    {
        var b = new PokeBuilder("Hatenna", 0.45f, BodyPlan.Biped, V(0, 0.08f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.03f, 0.01f));
            b.Ell(leg, V(0.028f * s, 0.018f, 0.015f), V(0.018f, 0.018f, 0.022f), HatPink);
        });
        b.Ell(Body, V(0, 0.06f, 0), V(0.045f, 0.045f, 0.04f), HatPink);
        int head = b.Head(V(0, 0.1f, 0));
        var hc = V(0, 0.15f, 0);
        b.Ell(head, hc, V(0.1f, 0.075f, 0.09f), HatBlue);
        // The hair hangs to the ground on both sides
        PokeBuilder.Both(s =>
        {
            b.Limb(head, hc + V(0.06f * s, -0.02f, 0.02f), V(0.11f * s, 0.05f, 0.03f), 0.035f, 0.03f, HatBlue);
            b.Ell(head, V(0.12f * s, 0.035f, 0.03f), V(0.055f, 0.028f, 0.05f), HatBlue, V(0, 0, -15f * s));
        });
        b.Cut(head, V(0, 0.1f, 0.095f), V(0.05f, 0.045f, 0.05f));
        var fc = V(0, 0.11f, 0.05f);
        var fr = V(0.045f, 0.04f, 0.035f);
        b.Ell(head, fc, fr, HatPink);
        foreach (var at in new[] { V(-0.05f, 0.2f, 0.05f), V(0.06f, 0.18f, 0.06f), V(0.0f, 0.22f, -0.03f) })
            b.PaintEll(head, at, V(0.015f, 0.012f, 0.015f), HatLight, soft: 0.005f);
        // The cap, striped, its tip bent over to a blue bobble
        b.Spike(head, hc + V(0, 0.05f, -0.01f), hc + V(0.03f, 0.18f, -0.04f), 0.05f, HatPink);
        b.PaintTorus(head, hc + V(0.01f, 0.1f, -0.02f), 0.035f, 0.007f, White, V(0, 0, -12f));
        b.Tube(head, new[] { hc + V(0.03f, 0.17f, -0.04f), hc + V(0.06f, 0.2f, -0.03f), hc + V(0.09f, 0.19f, -0.02f) }, 0.012f, 0.01f, HatPink, blend: 0f);
        b.Ell(head, hc + V(0.1f, 0.185f, -0.02f), V(0.02f, 0.02f, 0.02f), HatBlue, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.016f * s, fc.Y + 0.006f);
            b.Eye(head, at, Outward(fc, fr, at), 0.01f, Rgb(40, 30, 40));
        });
        b.Mark(head, On(fc, fr, 0, fc.Y - 0.017f), V(0, -0.3f, 1f), 0.008f, 0.007f, Rgb(140, 70, 90));
        return b;
    }

    /// <summary>Hattrem: a small pink body wrapped in a great ball of blue hair, its face looking out of a hollow in the front, a curl on its brow, two big mitts of hair resting on the ground and a pink cap broken off at the tip.</summary>
    private static PokeBuilder Hattrem()
    {
        var b = new PokeBuilder("Hattrem", 0.65f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.05f, 0.03f));
            b.Ell(leg, V(0.032f * s, 0.022f, 0.045f), V(0.022f, 0.022f, 0.026f), HatPink);
        });
        b.Ell(Body, V(0, 0.1f, 0.03f), V(0.05f, 0.06f, 0.045f), HatPink);
        int head = b.Head(V(0, 0.2f, 0));
        var hc = V(0, 0.28f, -0.01f);
        var hr = V(0.19f, 0.17f, 0.15f);
        b.Ell(head, hc, hr, HatBlue);
        b.Cut(head, V(0, 0.23f, 0.15f), V(0.07f, 0.09f, 0.08f));
        var fc = V(0, 0.23f, 0.1f);
        var fr = V(0.06f, 0.07f, 0.05f);
        b.Ell(head, fc, fr, HatPink);
        foreach (var dir in new[] { V(0.7f, 0.5f, 0.4f), V(-0.8f, 0.3f, 0.3f), V(0.4f, 0.8f, -0.3f), V(-0.5f, 0.6f, -0.5f), V(0.9f, -0.1f, -0.3f) })
            b.PaintEll(head, Out(hc, hr, default, dir), V(0.025f, 0.02f, 0.025f), HatLight, soft: 0.006f);
        Curl(b, head, hc + V(0.02f, 0.13f, 0.08f), hc + V(0, 0.1f, 0.115f), V(1f, 0, 0), V(0, 1f, 0), 0.035f, 1.2f, 0.012f, HatLight);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.14f * s, 0.18f, 0.04f));
            b.Limb(arm, V(0.13f * s, 0.18f, 0.04f), V(0.2f * s, 0.07f, 0.07f), 0.045f, 0.035f, HatBlue);
            var mitt = V(0.23f * s, 0.035f, 0.08f);
            b.Ell(arm, mitt, V(0.08f, 0.035f, 0.07f), HatBlue);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Ell(arm, mitt + V(0.03f * s + 0.025f * t * s, 0.0f, 0.05f), V(0.022f, 0.024f, 0.025f), HatBlue, blend: 0.012f);
            var at = On(fc, fr, 0.022f * s, fc.Y + 0.01f);
            b.Eye(head, at, Outward(fc, fr, at), 0.011f, Rgb(40, 30, 40), glare: true);
        });
        // The cap, its tip broken off white
        var cap = hc + V(0, 0.14f, -0.03f);
        b.Spike(head, cap, cap + V(0.05f, 0.14f, -0.03f), 0.07f, HatPink);
        b.PaintTorus(head, cap + V(0.015f, 0.05f, -0.01f), 0.05f, 0.008f, White, V(0, 0, -15f));
        b.Spike(head, cap + V(0.04f, 0.11f, -0.025f), cap + V(0.08f, 0.17f, -0.02f), 0.02f, White, 0.6f, blend: 0.006f);
        return b;
    }

    /// <summary>
    /// Hatterene and its Gigantamax form. Hatterene is a tall sorceress, her gown pink above and white below, frilled
    /// at the hem, a white face under a broad blue hat drawn up into a long point, and from it a long tress of hair
    /// that serves as her arm, ending in a hand of three claws. The Gigantamax Hatterene's tresses have grown into great
    /// blades sweeping up round her, her gown is fuller and Gigantamax's red clouds circle her hat.
    /// </summary>
    private static PokeBuilder HattereneBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Hatterene-Gmax" : "Hatterene", 1f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Fur };
        var white = Rgb(248, 246, 250);
        var deep = Rgb(118, 156, 212);
        // The gown, pink above and white below, its hem frilled
        b.Limb(Body, V(0, gmax ? 0.15f : 0.13f, 0), V(0, 0.45f, 0), gmax ? 0.15f : 0.13f, 0.055f, HatPink);
        b.PaintEll(Body, V(0, 0.06f, 0), V(0.16f, 0.16f, 0.16f), white);
        b.Ell(Body, V(0, 0.035f, 0), V(gmax ? 0.2f : 0.15f, 0.035f, gmax ? 0.16f : 0.12f), white);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            b.Ell(Body, V(MathF.Sin(a) * (gmax ? 0.19f : 0.14f), 0.03f, MathF.Cos(a) * (gmax ? 0.15f : 0.11f)), V(0.03f, 0.025f, 0.03f), white, blend: 0.01f);
        }
        b.Ell(Body, V(0, 0.5f, 0), V(0.06f, 0.08f, 0.05f), HatPink);
        b.PaintEll(Body, V(0, 0.48f, 0.05f), V(0.02f, 0.03f, 0.02f), deep, soft: 0.006f);
        int head = b.Head(V(0, 0.58f, 0.01f));
        var c = V(0, 0.64f, 0.03f);
        var r = V(0.04f, 0.045f, 0.04f);
        b.Limb(head, V(0, 0.56f, 0.0f), c, 0.025f, 0.025f, white);
        b.Ell(head, c, r, white);
        // The hat: a broad brim and a long point drawn back
        b.Ell(head, c + V(0, 0.05f, -0.02f), V(0.16f, 0.04f, 0.13f), HatBlue, V(-15f, 0, 0));
        b.Spike(head, c + V(0, 0.06f, -0.04f), c + V(0.02f, 0.3f, -0.27f), 0.11f, HatBlue, 0.8f);
        // The tress that is her arm, ending in a hand of three claws, and another down her back
        var tress = Smooth(3, c + V(0.08f, 0.03f, -0.02f), c + V(0.17f, 0.0f, 0.0f), c + V(0.22f, -0.15f, 0.04f), c + V(0.24f, -0.28f, 0.06f));
        int arm = b.Arm(1f, tress[0]);
        b.Tube(arm, tress, 0.03f, 0.02f, HatBlue, blend: 0f);
        var hand = tress[^1] + V(0, -0.02f, 0.01f);
        b.Ell(arm, hand, V(0.03f, 0.03f, 0.025f), HatBlue);
        foreach (var d in new[] { V(-0.03f, -0.06f, 0.02f), V(0.0f, -0.07f, 0.03f), V(0.03f, -0.06f, 0.02f) })
            b.Spike(arm, hand, hand + d, 0.013f, HatBlue, blend: 0.006f);
        b.Tube(head, Smooth(3, c + V(-0.07f, 0.03f, -0.05f), c + V(-0.12f, -0.1f, -0.08f), c + V(-0.1f, -0.28f, -0.07f)), 0.028f, 0.012f, HatBlue, blend: 0f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.015f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(80, 60, 120), glare: true);
        });
        if (gmax)
        {
            // Great blades of hair sweeping up round her, and the red clouds circling her hat
            PokeBuilder.Both(s =>
            {
                foreach (var (from, to) in new[] { (V(0.08f, 0.08f, -0.02f), V(0.42f, 0.35f, -0.1f)), (V(0.06f, 0.1f, -0.06f), V(0.3f, 0.55f, -0.18f)) })
                    Blade(b, head, c + V(from.X * s, from.Y, from.Z), c + V(to.X * s, to.Y, to.Z), 0.07f, s > 0 ? HatBlue : deep, V(0, 0.3f, 1f));
            });
            MaxClouds(b, head, c + V(0.02f, 0.25f, -0.22f), 0.035f, 0.12f, 0.16f, 1f, 0.5f);
        }
        return b;
    }

    private static PokeBuilder Hatterene() => HattereneBuild(false);

    // ------------------------------------------------------------------ Impidimp line

    /// <summary>Impidimp: a little pink imp with a big round head, a fringe of dark hair slanting across its brow, wide white eyes, pointed ears red inside, small red horns, thin limbs and a dark pointed tail.</summary>
    private static PokeBuilder Impidimp()
    {
        var b = new PokeBuilder("Impidimp", 0.5f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        var pink = Rgb(222, 94, 140);
        var dark = Rgb(60, 50, 92);
        var red = Rgb(214, 52, 72);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.12f, 0));
            b.Limb(leg, V(0.03f * s, 0.12f, 0), V(0.05f * s, 0.03f, 0.02f), 0.018f, 0.015f, pink);
            b.Ell(leg, V(0.052f * s, 0.016f, 0.035f), V(0.02f, 0.016f, 0.03f), pink);
        });
        b.Ell(Body, V(0, 0.15f, 0), V(0.045f, 0.05f, 0.04f), dark);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.04f * s, 0.18f, 0));
            var hand = V(0.12f * s, 0.15f, 0.04f);
            b.Limb(arm, V(0.04f * s, 0.18f, 0), hand, 0.014f, 0.012f, pink);
            Digits(b, arm, hand, V(s, -0.2f, 0.2f), V(0, 0.3f, 0.2f), 0.025f, 0.006f, pink);
        });
        int head = b.Head(V(0, 0.2f, 0.01f));
        var c = V(0, 0.29f, 0.01f);
        var r = V(0.1f, 0.09f, 0.085f);
        b.Ell(head, c, r, pink);
        b.PaintEll(head, c + V(-0.03f, 0.05f, 0.05f), V(0.09f, 0.03f, 0.06f), dark, V(0, 0, 25f));
        PokeBuilder.Both(s =>
        {
            FoxEar(b, head, c + V(0.08f * s, 0.01f, -0.01f), c + V(0.17f * s, 0.05f, -0.02f), 0.035f, pink, red);
            b.Spike(head, c + V(0.05f * s, 0.07f, 0.0f), c + V(0.07f * s, 0.12f, -0.01f), 0.018f, red, blend: 0.006f);
            var at = On(c, r, 0.036f * s, c.Y - 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.022f, sclera: true, pupil: Rgb(30, 24, 36));
        });
        b.Spike(head, c + V(0.02f, -0.055f, 0.07f), c + V(0.022f, -0.075f, 0.072f), 0.006f, White, mat: Shell, blend: 0.002f);
        int tail = b.Tail(V(0, 0.12f, -0.04f));
        b.Tube(tail, new[] { V(0, 0.12f, -0.04f), V(0, 0.08f, -0.1f), V(0, 0.1f, -0.15f) }, 0.01f, 0.008f, dark, blend: 0f);
        b.Spike(tail, V(0, 0.1f, -0.15f), V(0, 0.13f, -0.19f), 0.018f, dark, 0.5f);
        return b;
    }

    /// <summary>Morgrem: a lanky imp, red above and green below, dark hair swept back from its brow, long pointed ears tipped dark, a sly fanged grin, long thin arms ending in long claws and a tail with a spiked tip.</summary>
    private static PokeBuilder Morgrem()
    {
        var b = new PokeBuilder("Morgrem", 0.75f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var red = Rgb(212, 54, 86);
        var dark = Rgb(58, 46, 86);
        var green = Rgb(80, 152, 92);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.22f, 0));
            var knee = V(0.055f * s, 0.12f, 0.03f);
            b.Limb(leg, V(0.04f * s, 0.23f, 0), knee, 0.025f, 0.02f, green);
            b.Limb(leg, knee, V(0.055f * s, 0.03f, 0.0f), 0.02f, 0.017f, green);
            b.Ell(leg, V(0.058f * s, 0.016f, 0.03f), V(0.022f, 0.016f, 0.04f), green);
            Claws(b, leg, V(0.058f * s, 0.012f, 0.065f), V(0.012f, 0, 0), V(0, -0.1f, 1f), 0.018f, 0.006f);
        });
        b.Ell(Body, V(0, 0.24f, 0), V(0.045f, 0.04f, 0.035f), green);
        b.Ell(Body, V(0, 0.32f, 0), V(0.05f, 0.075f, 0.04f), red);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.045f * s, 0.37f, 0));
            var elbow = V(0.1f * s, 0.28f, 0.02f);
            var hand = V(0.13f * s, 0.17f, 0.05f);
            b.Limb(arm, V(0.045f * s, 0.37f, 0), elbow, 0.016f, 0.013f, red);
            b.Limb(arm, elbow, hand, 0.013f, 0.011f, red);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand, hand + V(0.02f * t * s, -0.07f, 0.02f + 0.01f * t), 0.008f, red, blend: 0.004f);
        });
        int head = b.Head(V(0, 0.4f, 0.01f));
        var c = V(0, 0.47f, 0.02f);
        var r = V(0.055f, 0.06f, 0.055f);
        b.Limb(head, V(0, 0.38f, 0), c, 0.025f, 0.025f, red);
        b.Ell(head, c, r, red);
        // Dark hair swept back from the brow, falling behind
        b.Ell(head, c + V(0, 0.03f, -0.02f), V(0.06f, 0.045f, 0.055f), dark, blend: 0.012f);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.Spike(head, c + V(x, 0.02f, -0.04f), c + V(x * 1.5f, -0.1f, -0.1f), 0.022f, dark, 0.5f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.045f * s, 0.02f, -0.01f), c + V(0.14f * s, 0.1f, -0.03f), 0.028f, red, 0.5f);
            b.PaintEll(head, c + V(0.12f * s, 0.085f, -0.028f), V(0.025f, 0.025f, 0.02f), dark, soft: 0.006f);
            var at = Out(c, r, default, V(0.5f * s, 0.15f, 0.85f));
            b.Eye(head, at, Outward(c, r, at), 0.011f, sclera: true, pupil: Rgb(30, 24, 36), glare: true);
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.028f), V(0, -0.3f, 1f), 0.022f, 0.009f, Rgb(70, 20, 40), MarkShape.Smile);
        int tail = b.Tail(V(0, 0.22f, -0.03f));
        b.Tube(tail, Smooth(2, V(0, 0.22f, -0.03f), V(0.03f, 0.12f, -0.1f), V(0.0f, 0.06f, -0.18f)), 0.012f, 0.008f, dark, blend: 0f);
        foreach (var d in new[] { V(0.03f, 0.02f, -0.03f), V(-0.03f, 0.02f, -0.03f), V(0, -0.02f, -0.04f) })
            b.Spike(tail, V(0.0f, 0.06f, -0.18f), V(0.0f, 0.06f, -0.18f) + d, 0.012f, dark, 0.6f);
        return b;
    }

    /// <summary>
    /// Grimmsnarl and its Gigantamax form. Grimmsnarl is a giant whose great muscles are its hair wrapped round its
    /// body, dark purple, a green stripe down its chest, green claws, a face with pointed green ears and a wide fanged
    /// grin, and locks of hair rising off its shoulders. The Gigantamax Grimmsnarl stands tall on legs of hair like
    /// stilts, its arms grown long, a crown of red spikes on its head and Gigantamax's red clouds over it.
    /// </summary>
    private static PokeBuilder GrimmsnarlBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Grimmsnarl-Gmax" : "Grimmsnarl", 1f, BodyPlan.Biped, V(0, gmax ? 0.62f : 0.5f, 0)) { Coat = Fur };
        var dark = Rgb(72, 58, 112);
        var deep = Rgb(48, 40, 82);
        var green = Rgb(84, 172, 104);
        var red = Rgb(214, 52, 84);
        float lift = gmax ? 0.14f : 0f;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.32f + lift, 0));
            var knee = V(0.15f * s, 0.17f + lift * 0.5f, 0.04f);
            b.Limb(leg, V(0.11f * s, 0.34f + lift, 0), knee, gmax ? 0.06f : 0.085f, gmax ? 0.045f : 0.06f, dark);
            b.Limb(leg, knee, V(0.14f * s, 0.05f, 0.03f), gmax ? 0.045f : 0.06f, gmax ? 0.04f : 0.055f, dark);
            b.Ell(leg, V(0.14f * s, 0.03f, 0.06f), V(0.06f, 0.03f, 0.07f), deep);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.14f * s + 0.03f * t, 0.025f, 0.11f), V(0.14f * s + 0.04f * t, 0.015f, 0.16f), 0.014f, green, mat: Shell, blend: 0.005f);
        });
        var bc = V(0, 0.5f + lift, 0);
        b.Ell(Body, bc, V(0.18f, 0.17f, 0.13f), dark);
        // The hair wound round the body, in bands of a deeper shade
        for (int i = 0; i < 3; i++)
            b.PaintEll(Body, bc + V(0, 0.08f - i * 0.08f, 0.0f), V(0.19f, 0.012f, 0.14f), deep, V(0, 0, 10f * (i - 1)), 0.006f);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.12f), V(0.03f, 0.15f, 0.03f), green);
        PokeBuilder.Both(s =>
        {
            foreach (var (x, z) in new[] { (0.12f, 0.0f), (0.08f, -0.08f) })
                b.Tube(Body, Smooth(2, bc + V(x * s, 0.12f, z), bc + V((x + 0.06f) * s, 0.24f, z - 0.02f), bc + V((x + 0.02f) * s, 0.32f, z - 0.06f)), 0.035f, 0.01f, dark, blend: 0.01f);
            var shoulder = bc + V(0.17f * s, 0.1f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = bc + V(0.32f * s, gmax ? -0.12f : -0.08f, 0.08f);
            var fist = bc + V(gmax ? 0.38f * s : 0.33f * s, gmax ? -0.42f : -0.3f, 0.12f);
            b.Limb(arm, shoulder, elbow, 0.09f, 0.07f, dark);
            b.Limb(arm, elbow, fist, 0.07f, 0.075f, dark);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, fist + V(0.03f * t, -0.04f, 0.05f), fist + V(0.04f * t, -0.11f, 0.08f), 0.016f, green, mat: Shell, blend: 0.005f);
        });
        int head = b.Head(bc + V(0, 0.16f, 0.05f));
        var c = bc + V(0, 0.22f, 0.08f);
        var r = V(0.07f, 0.07f, 0.065f);
        b.Ell(head, c, r, dark);
        b.PaintEll(head, c + V(0, 0.0f, 0.05f), V(0.05f, 0.06f, 0.03f), green);
        b.Mark(head, On(c, r, 0, c.Y - 0.03f), V(0, -0.3f, 1f), 0.035f, 0.014f, Rgb(250, 240, 240), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.055f * s, 0.02f, -0.01f), c + V(0.13f * s, 0.15f, -0.04f), 0.03f, green, 0.5f);
            var at = Out(c, r, default, V(0.45f * s, 0.2f, 0.88f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, pupil: red, glare: true);
        });
        if (gmax)
        {
            for (int i = -2; i <= 2; i++)
                b.Spike(head, c + V(i * 0.025f, 0.05f, -0.02f), c + V(i * 0.045f, 0.13f + 0.03f * (2 - Math.Abs(i)), -0.04f), 0.016f, red, 0.6f);
            MaxClouds(b, head, c + V(0, 0.14f, -0.04f), 0.035f, 0.1f, 0.16f, 0.9f, 0.6f);
        }
        return b;
    }

    private static PokeBuilder Grimmsnarl() => GrimmsnarlBuild(false);

    // ------------------------------------------------------------------ Obstagoon

    /// <summary>Obstagoon: a rough-coated badger like a punk, its fur black with white stripes, a great white mane on its chest and shoulders and white hair down its back, arms crossed before it in an X, red eyes in a striped face and a long tongue hanging out.</summary>
    private static PokeBuilder Obstagoon()
    {
        var b = new PokeBuilder("Obstagoon", 0.95f, BodyPlan.Biped, V(0, 0.48f, 0)) { Coat = Fur };
        var black = Rgb(52, 52, 58);
        var white = Rgb(238, 238, 238);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.32f, 0));
            var knee = V(0.1f * s, 0.17f, 0.03f);
            b.Limb(leg, V(0.08f * s, 0.33f, 0), knee, 0.055f, 0.045f, black);
            b.Limb(leg, knee, V(0.1f * s, 0.05f, 0.02f), 0.045f, 0.04f, black);
            b.PaintTorus(leg, knee + V(0, 0.05f, 0), 0.05f, 0.01f, white);
            b.Ell(leg, V(0.1f * s, 0.03f, 0.05f), V(0.045f, 0.03f, 0.06f), black);
            Claws(b, leg, V(0.1f * s, 0.02f, 0.1f), V(0.02f, 0, 0), V(0, -0.2f, 1f), 0.025f, 0.008f);
        });
        var bc = V(0, 0.48f, 0);
        b.Ell(Body, bc, V(0.12f, 0.15f, 0.1f), black);
        b.Ell(Body, bc + V(0, 0.06f, 0.05f), V(0.13f, 0.1f, 0.08f), white, blend: 0.02f);
        FurTufts(b, Body, bc + V(0, 0.04f, 0.04f), V(0.13f, 0.1f, 0.08f), 14, 0.05f, 0.02f, white, 2f, -0.95f, 0.7f);
        PokeBuilder.Both(s =>
        {
            // Arms crossed in front, each striped white
            var shoulder = bc + V(0.13f * s, 0.07f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var elbow = bc + V(0.17f * s, -0.07f, 0.1f);
            var hand = bc + V(-0.07f * s, 0.02f, 0.17f + 0.02f * s);
            b.Limb(arm, shoulder, elbow, 0.05f, 0.042f, black);
            b.Limb(arm, elbow, hand, 0.042f, 0.036f, black);
            b.PaintEll(arm, Vector3.Lerp(elbow, hand, 0.5f), V(0.045f, 0.012f, 0.045f), white, Euler(hand - elbow), 0.006f);
            Claws(b, arm, hand + Vector3.Normalize(hand - elbow) * 0.03f, V(0, 0.015f, 0), hand - elbow, 0.03f, 0.008f);
        });
        int head = b.Head(bc + V(0, 0.16f, 0.03f));
        var c = bc + V(0, 0.21f, 0.06f);
        var r = V(0.065f, 0.06f, 0.075f);
        b.Ell(head, c, r, black);
        b.Spike(head, c + V(0, -0.01f, 0.05f), c + V(0, -0.03f, 0.14f), 0.035f, white, 0.8f);
        b.Ell(head, c + V(0, -0.028f, 0.14f), V(0.012f, 0.01f, 0.01f), black, blend: 0.004f);
        b.PaintEll(head, c + V(0, 0.03f, 0.05f), V(0.015f, 0.05f, 0.06f), white, V(20f, 0, 0), 0.006f);
        b.Tube(head, new[] { c + V(0.012f, -0.035f, 0.11f), c + V(0.02f, -0.08f, 0.13f), c + V(0.03f, -0.12f, 0.12f) }, 0.014f, 0.012f, Rgb(222, 96, 126), blend: 0f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.6f * s, 0.3f, 0.75f));
            b.PaintEll(head, at, V(0.02f, 0.01f, 0.02f), white, soft: 0.005f);
            b.Eye(head, at + V(0, -0.006f, 0), Outward(c, r, at), 0.011f, Rgb(214, 40, 50), glare: true);
        });
        // White hair down the back
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            Frond(b, head, c + V(x, 0.04f, -0.05f), c + V(x * 1.6f, -0.12f, -0.17f), 0.03f, white, V(0, 0.3f, -1f), 0.3f);
        return b;
    }

    // ------------------------------------------------------------------ Perrserker

    /// <summary>Perrserker: a stocky cat of the Viking north, dark brown with a great grey beard of fur over its chest, a metal coin set in its brow between two dark horns, fierce orange eyes and a wide grin of sharp teeth, and a dark bladed tail.</summary>
    private static PokeBuilder Perrserker()
    {
        var b = new PokeBuilder("Perrserker", 0.75f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var dark = Rgb(70, 62, 58);
        var grey = Rgb(200, 198, 194);
        var tan = Rgb(176, 150, 120);
        var metal = Rgb(150, 152, 164);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.18f, 0));
            b.Limb(leg, V(0.07f * s, 0.19f, 0), V(0.08f * s, 0.05f, 0.02f), 0.05f, 0.045f, dark);
            b.Ell(leg, V(0.08f * s, 0.03f, 0.05f), V(0.045f, 0.03f, 0.055f), tan);
        });
        var bc = V(0, 0.3f, 0);
        b.Ell(Body, bc, V(0.13f, 0.13f, 0.11f), dark);
        var mane = bc + V(0, 0.06f, 0.08f);
        b.Ell(Body, mane, V(0.15f, 0.12f, 0.08f), grey, blend: 0.02f);
        FurTufts(b, Body, mane + V(0, -0.02f, 0), V(0.14f, 0.1f, 0.07f), 16, 0.05f, 0.02f, grey, 2f, -0.95f, 0.9f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.13f * s, 0.05f, 0.0f));
            var hand = bc + V(0.2f * s, -0.07f, 0.08f);
            b.Limb(arm, bc + V(0.12f * s, 0.05f, 0.0f), hand, 0.045f, 0.04f, dark);
            b.Ell(arm, hand, V(0.04f, 0.035f, 0.04f), tan);
            Claws(b, arm, hand + V(0, -0.02f, 0.035f), V(0.015f, 0, 0), V(0, -0.5f, 1f), 0.025f, 0.008f);
        });
        int head = b.Head(bc + V(0, 0.14f, 0.04f));
        var c = bc + V(0, 0.18f, 0.05f);
        var r = V(0.1f, 0.075f, 0.08f);
        b.Ell(head, c, r, dark);
        // The coin set in its brow, the horns and the ears
        b.Ell(head, c + V(0, 0.05f, 0.05f), V(0.025f, 0.025f, 0.01f), metal, V(-40f, 0, 0), Metal, 0.006f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.06f * s, 0.05f, -0.01f), c + V(0.14f * s, 0.11f, -0.03f), 0.03f, Rgb(52, 46, 44), 0.6f);
            var at = Out(c, r, default, V(0.42f * s, 0.32f, 0.85f));
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(244, 150, 40), glare: true);
        });
        Gape(b, head, c + V(0, -0.03f, 0.075f), V(0.065f, 0.025f, 0.03f), Rgb(110, 40, 50), 0.016f);
        for (int i = -2; i <= 2; i++)
            b.Spike(head, c + V(i * 0.022f, -0.008f, 0.075f), c + V(i * 0.022f, -0.025f, 0.08f), 0.007f, White, mat: Shell, blend: 0.002f);
        int tail = b.Tail(bc + V(0, -0.05f, -0.1f));
        Blade(b, tail, bc + V(0.02f, -0.05f, -0.1f), bc + V(0.12f, 0.12f, -0.2f), 0.045f, Rgb(52, 46, 44), V(1f, 0, 0.3f));
        return b;
    }

    // ------------------------------------------------------------------ Cursola

    /// <summary>Cursola: the ghost of a coral, branches of bleached white coral rising from a pale round head with a ghostly face, its body trailing down into a wisp on a ring of grey stone, wisps curling from it.</summary>
    private static PokeBuilder Cursola()
    {
        var b = new PokeBuilder("Cursola", 0.75f, BodyPlan.Floating, V(0, 0.36f, 0)) { Coat = Fur }.Hover();
        var white = Rgb(240, 240, 246);
        var grey = Rgb(190, 190, 200);
        var hc = V(0, 0.36f, 0);
        var hr = V(0.1f, 0.1f, 0.1f);
        b.Ell(Body, hc, hr, white, mat: Glow);
        foreach (var (root, tip, twig) in new[]
        {
            (V(0, 0.08f, 0), V(0.02f, 0.38f, -0.02f), V(0.06f, 0.06f, 0)),
            (V(0.06f, 0.06f, 0), V(0.2f, 0.3f, 0.0f), V(0.05f, 0.06f, 0.02f)),
            (V(-0.06f, 0.06f, 0), V(-0.2f, 0.28f, 0.02f), V(-0.05f, 0.07f, 0)),
            (V(0.03f, 0.07f, -0.05f), V(0.1f, 0.33f, -0.12f), V(0.02f, 0.06f, -0.05f)),
            (V(-0.03f, 0.07f, -0.05f), V(-0.09f, 0.34f, -0.1f), V(-0.04f, 0.05f, -0.04f))
        })
            Coral(b, Body, hc + root, hc + tip, 0.022f, white, twig);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.035f * s, hc.Y + 0.005f);
            b.Eye(Body, at, Outward(hc, hr, at), 0.014f, sclera: true, pupil: Rgb(200, 60, 100), glare: true);
        });
        b.Mark(Body, On(hc, hr, 0, hc.Y - 0.04f), V(0, -0.3f, 1f), 0.02f, 0.008f, Rgb(200, 60, 100), MarkShape.Wave);
        // The wisp of its body trailing down to a ring of stone, wisps curling from the ring
        b.Spike(Body, hc + V(0, -0.06f, 0), V(0, 0.14f, 0.0f), 0.06f, white, mat: Glow);
        b.Limb(Body, V(0, 0.2f, 0), V(0, 0.135f, 0), 0.03f, 0.065f, white, Glow, 0.01f);
        b.Torus(Body, V(0, 0.13f, 0), 0.07f, 0.022f, grey, mat: Shell, blend: 0.01f);
        foreach (float a in new[] { 0.6f, 2.4f, 4.2f })
            b.Tube(Body, new[] { V(MathF.Sin(a) * 0.07f, 0.12f, MathF.Cos(a) * 0.07f), V(MathF.Sin(a) * 0.12f, 0.08f, MathF.Cos(a) * 0.12f), V(MathF.Sin(a + 0.4f) * 0.14f, 0.1f, MathF.Cos(a + 0.4f) * 0.14f) }, 0.012f, 0.006f, white, blend: 0f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Sirfetch'd

    /// <summary>Sirfetch'd: a white duck grown into a knight, standing proud on short legs, a great leek held up as a lance and a shield of leek leaves on its other arm, a crest of white feathers, heavy dark brows and a yellow bill.</summary>
    private static PokeBuilder Sirfetchd()
    {
        var b = new PokeBuilder("Sirfetch'd", 0.8f, BodyPlan.Biped, V(0, 0.25f, 0)) { Coat = Fur };
        var white = Rgb(244, 244, 240);
        var brown = Rgb(150, 104, 70);
        var yellow = Rgb(242, 192, 64);
        var green = Rgb(118, 176, 74);
        var dark = Rgb(62, 112, 62);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.13f, 0));
            b.Limb(leg, V(0.04f * s, 0.14f, 0), V(0.045f * s, 0.03f, 0.01f), 0.016f, 0.014f, brown);
            b.Ell(leg, V(0.046f * s, 0.014f, 0.035f), V(0.03f, 0.012f, 0.04f), Rgb(230, 150, 70));
        });
        var bc = V(0, 0.25f, 0);
        b.Ell(Body, bc, V(0.085f, 0.11f, 0.08f), white);
        b.PaintEll(Body, bc + V(0, 0.0f, -0.06f), V(0.09f, 0.1f, 0.04f), PixelCanvas.Mix(white, brown, 0.25f));
        // The lance: a long leek held upright in the right wing, its white root below the grip
        int lanceArm = b.Arm(1f, bc + V(0.07f, 0.05f, 0.02f));
        var grip = bc + V(0.12f, -0.01f, 0.07f);
        b.Limb(lanceArm, bc + V(0.07f, 0.05f, 0.02f), grip, 0.028f, 0.024f, white);
        b.Limb(lanceArm, grip + V(0, -0.1f, 0), grip + V(0, 0.02f, 0), 0.026f, 0.026f, Rgb(236, 236, 214));
        b.Limb(lanceArm, grip + V(0, 0.02f, 0), grip + V(0.01f, 0.56f, 0.01f), 0.024f, 0.018f, green, Leaf);
        b.PaintEll(lanceArm, grip + V(0.01f, 0.5f, 0.01f), V(0.03f, 0.08f, 0.03f), PixelCanvas.Mix(green, White, 0.3f));
        // The shield: a fan of leek leaves on the left wing
        int shieldArm = b.Arm(-1f, bc + V(-0.07f, 0.05f, 0.02f));
        var boss = bc + V(-0.13f, 0.0f, 0.08f);
        b.Limb(shieldArm, bc + V(-0.07f, 0.05f, 0.02f), boss, 0.028f, 0.024f, white);
        foreach (var (dx, up) in new[] { (-0.04f, 0.2f), (0f, 0.23f), (0.04f, 0.2f), (-0.07f, 0.14f), (0.06f, 0.15f) })
            Frond(b, shieldArm, boss + V(0, -0.08f, 0.02f), boss + V(dx, up - 0.08f, 0.03f), 0.04f, dx == 0f ? green : dark, V(0, 0, 1f), 0.2f, Leaf);
        int head = b.Head(bc + V(0, 0.1f, 0.01f));
        var c = bc + V(0, 0.16f, 0.03f);
        var r = V(0.06f, 0.06f, 0.06f);
        b.Ell(head, c, r, white);
        foreach (var (x, up, back) in new[] { (0f, 0.09f, 0.05f), (-0.02f, 0.07f, 0.07f), (0.02f, 0.07f, 0.07f) })
            Frond(b, head, c + V(x, 0.04f, -0.02f), c + V(x * 2f, 0.04f + up, -0.02f - back), 0.022f, white, V(1f, 0, 0), 0.3f);
        b.Spike(head, c + V(0, -0.01f, 0.05f), c + V(0, -0.025f, 0.12f), 0.025f, yellow, 0.6f, Shell, 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.55f * s, 0.2f, 0.8f));
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 34, 30), glare: true);
            b.Mark(head, at + V(0.002f * s, 0.022f, 0.002f), Outward(c, r, at), 0.016f, 0.005f, Rgb(70, 50, 40), MarkShape.Bar, -15f * s);
        });
        int tail = b.Tail(bc + V(0, -0.02f, -0.07f));
        b.Spike(tail, bc + V(0, -0.02f, -0.07f), bc + V(0, 0.02f, -0.15f), 0.035f, white, 0.5f);
        return b;
    }

    // ------------------------------------------------------------------ Mr. Rime

    /// <summary>Mr. Rime: a tap-dancing gentleman, its head a navy top hat flared at the sides, a moustache and yellow ringed eyes, a navy body with a round red jewel on its belly, white gloves, blue legs in pale blue tap shoes and a cane of ice.</summary>
    private static PokeBuilder MrRime()
    {
        var b = new PokeBuilder("Mr. Rime", 0.85f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Fur };
        var navy = Rgb(72, 72, 134);
        var white = Rgb(246, 246, 252);
        var red = Rgb(214, 70, 92);
        var blue = Rgb(112, 152, 222);
        var ice = Rgb(176, 214, 244);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.24f, 0));
            var knee = V(0.075f * s, 0.13f, 0.03f);
            b.Limb(leg, V(0.06f * s, 0.25f, 0), knee, 0.035f, 0.03f, blue);
            b.Limb(leg, knee, V(0.075f * s, 0.05f, 0.01f), 0.03f, 0.027f, blue);
            b.Ell(leg, V(0.078f * s, 0.03f, 0.04f), V(0.045f, 0.03f, 0.07f), ice);
        });
        var bc = V(0, 0.38f, 0);
        b.Ell(Body, bc, V(0.13f, 0.14f, 0.11f), navy);
        var jewel = bc + V(0, -0.01f, 0.1f);
        b.Ell(Body, jewel, V(0.07f, 0.06f, 0.035f), red, mat: Shell, blend: 0.01f);
        b.PaintEll(Body, jewel + V(-0.02f, 0.025f, 0.03f), V(0.02f, 0.012f, 0.01f), Rgb(250, 200, 210), soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.12f * s, 0.06f, 0));
            var hand = bc + V(0.22f * s, -0.04f, 0.07f);
            b.Limb(arm, bc + V(0.11f * s, 0.06f, 0), hand, 0.022f, 0.018f, navy);
            b.Ell(arm, hand, V(0.035f, 0.035f, 0.035f), white);
            if (s > 0)
            {
                // The cane of ice, its hook at the top
                b.Limb(arm, hand + V(0, 0.03f, 0), hand + V(0.02f, -0.3f, 0.02f), 0.014f, 0.012f, ice, Shell, 0.006f);
                b.Tube(arm, new[] { hand + V(0, 0.03f, 0), hand + V(0.0f, 0.07f, 0.01f), hand + V(-0.03f, 0.08f, 0.015f), hand + V(-0.05f, 0.05f, 0.015f) }, 0.014f, 0.012f, ice, Shell, 0f);
            }
        });
        int head = b.Head(bc + V(0, 0.13f, 0.01f));
        var c = bc + V(0, 0.22f, 0.02f);
        var r = V(0.09f, 0.08f, 0.08f);
        b.Ell(head, c, r, navy);
        // The hat: a crown and a brim flared out at the sides
        b.Limb(head, c + V(0, 0.05f, -0.01f), c + V(0, 0.15f, -0.01f), 0.075f, 0.07f, PixelCanvas.Mix(navy, Black, 0.2f), blend: 0.012f);
        b.PaintTorus(head, c + V(0, 0.08f, -0.01f), 0.075f, 0.01f, Rgb(170, 60, 90));
        PokeBuilder.Both(s => b.Ell(head, c + V(0.11f * s, 0.05f, -0.01f), V(0.06f, 0.025f, 0.05f), PixelCanvas.Mix(navy, Black, 0.2f), V(0, 0, 25f * s), blend: 0.015f));
        b.Mark(head, On(c, r, 0, c.Y - 0.03f), V(0, -0.2f, 1f), 0.035f, 0.012f, Rgb(36, 36, 46), MarkShape.Wave);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.008f);
            b.Mark(head, at, Outward(c, r, at), 0.024f, 0.024f, Rgb(246, 214, 90), MarkShape.Ring);
            b.Eye(head, at, Outward(c, r, at), 0.013f, sclera: true, white: Rgb(250, 230, 130), pupil: Rgb(36, 36, 46));
        });
        return b;
    }

    // ------------------------------------------------------------------ Runerigus

    /// <summary>Runerigus: a ghost bound to a stone tablet, the slab bent like a jagged S and cut with red runes, one eye in its top, and the shadow that is its body reaching from the stone in two black arms with great clawed hands and a ragged tail.</summary>
    private static PokeBuilder Runerigus()
    {
        var b = new PokeBuilder("Runerigus", 0.9f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var stone = Rgb(124, 120, 116);
        var dark = Rgb(48, 44, 52);
        var red = Rgb(204, 52, 52);
        // The slab in three pieces bent like a jagged S, runes cut into its front
        var top = V(0, 0.58f, 0);
        var mid = V(0.05f, 0.42f, 0);
        var low = V(-0.02f, 0.26f, 0);
        b.Box(Body, top, V(0.16f, 0.07f, 0.035f), 0.012f, stone, V(0, 0, 10f), blend: 0.006f);
        b.Box(Body, mid, V(0.06f, 0.12f, 0.035f), 0.012f, stone, V(0, 0, -6f), blend: 0.006f);
        b.Box(Body, low, V(0.14f, 0.07f, 0.035f), 0.012f, stone, V(0, 0, -10f), blend: 0.006f);
        foreach (var (at, rad, roll) in new[] { (low + V(-0.06f, 0, 0.035f), V(0.04f, 0.006f, 0.01f), 20f), (low + V(0.06f, 0.02f, 0.035f), V(0.03f, 0.006f, 0.01f), -40f), (mid + V(0, 0.05f, 0.035f), V(0.006f, 0.05f, 0.01f), 0f), (mid + V(0.02f, -0.05f, 0.035f), V(0.03f, 0.006f, 0.01f), 30f), (top + V(0.08f, -0.02f, 0.035f), V(0.04f, 0.006f, 0.01f), -10f) })
            b.PaintEll(Body, at, rad, red, V(0, 0, roll), 0.004f);
        b.Eye(Body, top + V(-0.05f, 0.01f, 0.036f), V(0, 0, 1f), 0.018f, Rgb(170, 110, 220), glare: true);
        b.Mark(Body, mid + V(0, -0.02f, 0.036f), V(0, 0, 1f), 0.03f, 0.01f, Rgb(40, 34, 40), MarkShape.Zigzag);
        // The shadow's arms, reaching from the ends of the slab, and its ragged tail
        foreach (var (from, to, s) in new[] { (top + V(-0.15f, 0.02f, -0.03f), top + V(-0.3f, -0.04f, 0.04f), -1f), (low + V(0.13f, 0.0f, -0.03f), low + V(0.29f, 0.08f, 0.04f), 1f) })
        {
            int arm = b.Arm(s, from);
            b.Tube(arm, Smooth(2, from, Vector3.Lerp(from, to, 0.5f) + V(0, 0.05f, -0.02f), to), 0.025f, 0.02f, dark, blend: 0f);
            b.Ell(arm, to, V(0.04f, 0.04f, 0.03f), dark);
            for (int i = 0; i < 4; i++)
                b.Limb(arm, to, to + V((0.03f + 0.012f * i) * s, -0.06f + 0.012f * i, 0.03f - 0.02f * i), 0.012f, 0.008f, dark, blend: 0.005f);
        }
        int tail = b.Tail(low + V(-0.12f, -0.04f, -0.02f));
        b.Tube(tail, Smooth(3, low + V(-0.12f, -0.04f, -0.02f), low + V(-0.2f, -0.12f, -0.05f), low + V(-0.1f, -0.18f, -0.08f), low + V(-0.22f, -0.22f, -0.06f)), 0.02f, 0.006f, dark, blend: 0f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Milcery and Alcremie

    /// <summary>Milcery: a floating dollop of cream splashed out into soft points, two dark ovals for eyes and a little smile.</summary>
    private static PokeBuilder Milcery()
    {
        var b = new PokeBuilder("Milcery", 0.35f, BodyPlan.Floating, V(0, 0.12f, 0)) { Coat = Fur }.Hover();
        var cream = Rgb(250, 240, 212);
        var c = V(0, 0.12f, 0);
        var r = V(0.11f, 0.075f, 0.09f);
        b.Ell(Body, c, r, cream);
        foreach (var (a, up, len) in new[] { (-1.2f, 0.4f, 0.07f), (-0.6f, 0.8f, 0.06f), (0.0f, 1f, 0.05f), (0.6f, 0.8f, 0.065f), (1.2f, 0.4f, 0.075f), (2.6f, 0.3f, 0.05f), (-2.6f, 0.3f, 0.05f) })
        {
            var d = Vector3.Normalize(V(MathF.Sin(a), up, MathF.Cos(a) * 0.3f - 0.2f));
            var root = c + d * 0.07f;
            b.Limb(Body, root, root + d * len, 0.026f, 0.02f, cream, blend: 0.02f);
            b.Ell(Body, root + d * len, V(0.022f, 0.022f, 0.022f), cream, blend: 0.01f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.008f);
            b.Eye(Body, at, Outward(c, r, at), 0.013f, Rgb(80, 60, 50));
        });
        b.Mark(Body, On(c, r, 0, c.Y - 0.025f), V(0, -0.3f, 1f), 0.012f, 0.007f, Rgb(120, 90, 70), MarkShape.Smile);
        return Lift(b);
    }

    /// <summary>Alcremie's creams, as their forms name them: the cream's colour, the colour swirled through it (or none), and the colour of its eyes.</summary>
    private static readonly (string Name, Color Cream, Color? Swirl, Color Eye)[] AlcremieCreams =
    {
        ("Vanilla-Cream", Rgb(250, 242, 234), null, Rgb(200, 52, 64)),
        ("Ruby-Cream", Rgb(246, 172, 192), null, Rgb(172, 40, 92)),
        ("Matcha-Cream", Rgb(184, 216, 144), null, Rgb(84, 124, 52)),
        ("Mint-Cream", Rgb(172, 228, 214), null, Rgb(40, 132, 140)),
        ("Lemon-Cream", Rgb(250, 232, 140), null, Rgb(200, 140, 40)),
        ("Salted-Cream", Rgb(244, 222, 198), null, Rgb(150, 100, 70)),
        ("Ruby-Swirl", Rgb(250, 236, 190), Rgb(240, 132, 162), Rgb(220, 90, 60)),
        ("Caramel-Swirl", Rgb(250, 240, 226), Rgb(196, 132, 82), Rgb(140, 80, 40)),
        ("Rainbow-Swirl", Rgb(250, 244, 246), Rgb(240, 160, 200), Rgb(150, 90, 200))
    };

    /// <summary>The sweets an Alcremie wears in its cream, as its forms name them.</summary>
    private static readonly string[] AlcremieSweets = { "Strawberry", "Berry", "Love", "Star", "Clover", "Flower", "Ribbon" };

    /// <summary>One of Alcremie's sweets on <paramref name="bone"/> at <paramref name="at"/>, its face toward <paramref name="facing"/>.</summary>
    private static void AlcremieSweet(PokeBuilder b, int bone, Vector3 at, Vector3 facing, string sweet)
    {
        var n = Vector3.Normalize(facing);
        var u = Vector3.Normalize(Vector3.Cross(n, V(0, 1f, 0)));
        var w = Vector3.Cross(u, n);
        switch (sweet)
        {
            case "Strawberry":
                b.Ell(bone, at, V(0.026f, 0.032f, 0.026f), Rgb(222, 52, 62), mat: Shell, blend: 0.008f);
                for (int i = 0; i < 4; i++)
                {
                    float a = i * MathF.Tau / 4f;
                    Frond(b, bone, at + V(0, 0.026f, 0), at + V(MathF.Cos(a) * 0.025f, 0.035f, MathF.Sin(a) * 0.025f), 0.009f, Rgb(90, 160, 70), V(0, 1f, 0), 0.3f, Leaf);
                }
                break;
            case "Berry":
                b.Ell(bone, at, V(0.026f, 0.026f, 0.026f), Rgb(70, 70, 150), mat: Shell, blend: 0.008f);
                Frond(b, bone, at + w * 0.022f, at + w * 0.03f + u * 0.025f, 0.01f, Rgb(90, 160, 70), n, 0.3f, Leaf);
                break;
            case "Love":
                foreach (float t in new[] { -1f, 1f })
                    b.Ell(bone, at + u * 0.012f * t + w * 0.008f, V(0.016f, 0.016f, 0.012f), Rgb(240, 110, 150), Euler(n) + V(90f, 0, 0), Shell, 0.008f);
                b.Spike(bone, at + w * 0.004f, at - w * 0.03f, 0.022f, Rgb(240, 110, 150), 0.5f, Shell, 0.008f);
                break;
            case "Star":
                b.Ell(bone, at, V(0.014f, 0.014f, 0.01f), Rgb(250, 214, 70), Euler(n) + V(90f, 0, 0), Shell, 0.008f);
                for (int i = 0; i < 5; i++)
                {
                    float a = i * MathF.Tau / 5f + MathF.PI / 2f;
                    Blade(b, bone, at, at + (u * MathF.Cos(a) + w * MathF.Sin(a)) * 0.032f, 0.014f, Rgb(250, 214, 70), n, 0.4f, Shell);
                }
                break;
            case "Clover":
                for (int i = 0; i < 4; i++)
                {
                    float a = i * MathF.Tau / 4f + MathF.PI / 4f;
                    var d = u * MathF.Cos(a) + w * MathF.Sin(a);
                    b.Ell(bone, at + d * 0.016f, V(0.014f, 0.014f, 0.006f), Rgb(96, 176, 80), Euler(n) + V(90f, 0, 0), Leaf, 0.008f);
                }
                b.Limb(bone, at, at - w * 0.03f, 0.005f, 0.004f, Rgb(70, 140, 60), Leaf, 0.004f);
                break;
            case "Flower":
                FlowerHead(b, bone, at, n, 5, 0.03f, 0.013f, Rgb(246, 196, 214), Rgb(250, 220, 90), 0.01f, 0.25f);
                break;
            default:
                // A ribbon: two loops and a knot, the ends trailing
                foreach (float t in new[] { -1f, 1f })
                {
                    b.Ell(bone, at + u * 0.022f * t, V(0.02f, 0.014f, 0.008f), Rgb(222, 60, 80), Euler(u * t) + V(0, 0, 0), Shell, 0.006f);
                    b.Spike(bone, at, at - w * 0.03f + u * 0.012f * t, 0.007f, Rgb(222, 60, 80), 0.4f, Shell, 0.004f);
                }
                b.Ell(bone, at, V(0.009f, 0.009f, 0.009f), Rgb(200, 40, 60), mat: Shell, blend: 0.004f);
                break;
        }
    }

    /// <summary>
    /// Alcremie of one cream (an index into <see cref="AlcremieCreams"/>) wearing one sweet (into
    /// <see cref="AlcremieSweets"/>): a little figure made of whipped cream, a skirt of cream scalloped round the
    /// bottom, two great locks of cream hanging by its face and a peak swirled up on top, a sweet tucked into each lock,
    /// its eyes in the cream's own colour. A swirled cream has its second colour wound in stripes round the locks, the
    /// peak and the skirt; the rainbow's stripes run through five colours.
    /// </summary>
    private static PokeBuilder AlcremieBuild(int cream, int sweet)
    {
        var (creamName, colour, swirl, eye) = AlcremieCreams[cream];
        string name = cream == 0 && sweet == 0 ? "Alcremie" : "Alcremie-" + creamName + "-" + AlcremieSweets[sweet] + "-Sweet";
        var b = new PokeBuilder(name, 0.6f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Fur };
        var rainbow = new[] { Rgb(240, 150, 180), Rgb(250, 214, 110), Rgb(170, 220, 140), Rgb(150, 200, 240), Rgb(196, 160, 230) };
        Color Stripe(int i) => creamName == "Rainbow-Swirl" ? rainbow[i % rainbow.Length] : swirl!.Value;
        // The skirt of cream, scalloped round its edge
        b.Ell(Body, V(0, 0.06f, 0), V(0.13f, 0.06f, 0.12f), colour);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            b.Ell(Body, V(MathF.Sin(a) * 0.12f, 0.03f, MathF.Cos(a) * 0.11f), V(0.035f, 0.03f, 0.035f), colour, blend: 0.012f);
        }
        b.Ell(Body, V(0, 0.16f, 0.01f), V(0.06f, 0.08f, 0.05f), colour);
        PokeBuilder.Both(s => b.Ell(Body, V(0.035f * s, 0.15f, 0.055f), V(0.018f, 0.016f, 0.016f), colour, blend: 0.01f));
        int head = b.Head(V(0, 0.24f, 0.02f));
        var c = V(0, 0.28f, 0.03f);
        var r = V(0.06f, 0.055f, 0.05f);
        b.Ell(head, c, r, colour);
        // The locks hanging by its face, the cream over its crown and the swirled peak
        var crown = c + V(0, 0.06f, -0.025f);
        b.Ell(head, crown, V(0.085f, 0.055f, 0.075f), colour, blend: 0.02f);
        var peak = new[] { crown + V(0, 0.03f, 0), crown + V(0.02f, 0.09f, -0.01f), crown + V(-0.01f, 0.15f, -0.02f), crown + V(0.025f, 0.19f, -0.01f) };
        b.Tube(head, peak, 0.05f, 0.012f, colour, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var lockTop = c + V(0.075f * s, 0.03f, -0.005f);
            b.Ell(head, lockTop + V(0.01f * s, -0.07f, 0), V(0.045f, 0.09f, 0.05f), colour, blend: 0.02f);
            b.Ell(head, lockTop + V(0.015f * s, -0.15f, 0.005f), V(0.035f, 0.035f, 0.04f), colour, blend: 0.015f);
            var at = On(c, r, 0.022f * s, c.Y - 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, eye);
            AlcremieSweet(b, head, lockTop + V(0.03f * s, 0.01f, 0.035f), V(0.6f * s, 0.2f, 0.75f), AlcremieSweets[sweet]);
        });
        if (swirl != null)
        {
            int k = 0;
            PokeBuilder.Both(s =>
            {
                for (int i = 0; i < 3; i++)
                    b.PaintEll(head, c + V(0.085f * s, -0.01f - i * 0.05f, 0.0f), V(0.05f, 0.008f, 0.055f), Stripe(k++), V(0, 0, 25f * s), 0.005f);
            });
            for (int i = 0; i < peak.Length - 1; i++)
                b.PaintEll(head, Vector3.Lerp(peak[i], peak[i + 1], 0.5f), V(0.05f, 0.008f, 0.05f), Stripe(k++), V(0, 0, 20f), 0.005f);
            for (int i = 0; i < 9; i++)
            {
                float a = i * MathF.Tau / 9f;
                b.PaintEll(Body, V(MathF.Sin(a) * 0.12f, 0.05f, MathF.Cos(a) * 0.11f), V(0.02f, 0.03f, 0.02f), Stripe(i), soft: 0.005f);
            }
        }
        return b;
    }

    private static PokeBuilder Alcremie() => AlcremieBuild(0, 0);

    /// <summary>The Gigantamax Alcremie: a great cake of three tiers, cream dripping from each, stars round the bottom, a clover on the middle and strawberries on top, Gigantamax's red clouds over it.</summary>
    private static PokeBuilder AlcremieGmax()
    {
        var b = new PokeBuilder("Alcremie-Gmax", 1f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Fur };
        var cream = Rgb(250, 242, 234);
        var pink = Rgb(236, 168, 196);
        var plum = Rgb(176, 110, 160);
        var tiers = new[] { (0.1f, 0.34f, 0.1f, cream), (0.3f, 0.25f, 0.09f, pink), (0.48f, 0.16f, 0.08f, plum), (0.62f, 0.09f, 0.06f, cream) };
        foreach (var (y, rad, h, col) in tiers)
        {
            b.Ell(Body, V(0, y, 0), V(rad, h, rad), col);
            // Cream dripping from the tier's edge
            for (int i = 0; i < 10; i++)
            {
                float a = i * MathF.Tau / 10f + y * 5f;
                var root = V(MathF.Sin(a) * rad * 0.95f, y + h * 0.4f, MathF.Cos(a) * rad * 0.95f);
                b.Limb(Body, root, root + V(0, -h * (0.6f + 0.4f * (i % 3) / 2f), 0), 0.02f, 0.017f, cream, blend: 0.01f);
            }
        }
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            var at = V(MathF.Sin(a) * 0.35f, 0.1f, MathF.Cos(a) * 0.35f);
            AlcremieSweet(b, Body, at, V(MathF.Sin(a), 0, MathF.Cos(a)), "Star");
        }
        AlcremieSweet(b, Body, V(0, 0.32f, 0.26f), V(0, 0, 1f), "Clover");
        foreach (var at in new[] { V(-0.04f, 0.71f, 0.02f), V(0.04f, 0.71f, -0.01f) })
            AlcremieSweet(b, Body, at, V(0, 0.3f, 1f), "Strawberry");
        MaxClouds(b, Body, V(0, 0.72f, -0.02f), 0.035f, 0.1f, 0.16f, 0.9f, 0.4f);
        return b;
    }

    // ------------------------------------------------------------------ Falinks

    /// <summary>One of Falinks's troopers at <paramref name="c"/>: a round body of brass over black, a red horn on its helmet and two cyan eyes; the leader is bigger with a red crest. Each stands on two little black feet.</summary>
    private static void Trooper(PokeBuilder b, int bone, Vector3 c, float r, bool leader)
    {
        var brass = Rgb(240, 198, 64);
        var black = Rgb(40, 40, 46);
        var red = Rgb(214, 46, 46);
        b.Ell(bone, c, V(r, r, r * 1.05f), brass, mat: Metal);
        b.PaintEll(bone, c + V(0, -r * 0.75f, 0), V(r * 1.2f, r * 0.5f, r * 1.25f), black);
        b.PaintEll(bone, c + V(0, r * 0.05f, r * 0.9f), V(r * 0.8f, r * 0.35f, r * 0.3f), black);
        b.Spike(bone, c + V(0, r * 0.7f, r * 0.2f), c + V(0, r * (leader ? 2f : 1.6f), r * 0.7f), r * 0.32f, red, 0.6f, Shell, 0.008f);
        if (leader)
            b.Spike(bone, c + V(0, r * 0.75f, -r * 0.3f), c + V(0, r * 1.5f, -r * 1.2f), r * 0.28f, red, 0.5f, Shell, 0.008f);
        PokeBuilder.Both(s =>
        {
            b.Ell(bone, c + V(r * 0.45f * s, -r * 0.85f, r * 0.2f), V(r * 0.28f, r * 0.28f, r * 0.38f), black, blend: 0.01f);
            var at = Out(c, V(r, r, r * 1.05f), default, V(0.32f * s, 0.05f, 0.95f));
            b.Eye(bone, at, Outward(c, V(r, r, r * 1.05f), at), r * 0.2f, Rgb(110, 214, 236));
        });
    }

    /// <summary>Falinks: a squad of six marching in a line, each a round brass trooper with a red horn, the leader at the front bigger and crested.</summary>
    private static PokeBuilder Falinks()
    {
        var b = new PokeBuilder("Falinks", 0.8f, BodyPlan.Serpent, V(0, 0.09f, 0.2f)) { Coat = Metal };
        for (int i = 0; i < 6; i++)
        {
            float r = i == 0 ? 0.07f : 0.055f;
            var c = V(i * 0.09f - 0.22f + (i > 0 ? 0.015f : 0f), r + 0.022f, 0.18f - i * 0.06f);
            int bone = i == 0 ? Body : b.Part("trooper" + i, Body, c, PokeRole.Segment, i * 0.5f);
            Trooper(b, bone, c, r, i == 0);
        }
        return b;
    }

    /// <summary>Mega Falinks, from its picture in Legends: Z-A: the squad joined into one armoured soldier of brass and black, a long red lance on one arm and a round brass shield rimmed red on the other, a red crest on its helmet and cyan eyes.</summary>
    private static PokeBuilder FalinksMegaBuild()
    {
        var b = new PokeBuilder("Falinks-Mega", 0.95f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Metal };
        var brass = Rgb(240, 198, 64);
        var black = Rgb(40, 40, 46);
        var red = Rgb(214, 46, 46);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.24f, 0));
            b.Limb(leg, V(0.07f * s, 0.25f, 0), V(0.08f * s, 0.06f, 0.01f), 0.04f, 0.035f, black);
            b.Ell(leg, V(0.08f * s, 0.04f, 0.03f), V(0.045f, 0.04f, 0.06f), brass);
            b.Ell(leg, V(0.08f * s, 0.16f, 0.02f), V(0.045f, 0.045f, 0.045f), brass, blend: 0.01f);
        });
        var bc = V(0, 0.4f, 0);
        b.Ell(Body, bc, V(0.12f, 0.14f, 0.1f), brass);
        b.PaintEll(Body, bc + V(0, -0.1f, 0), V(0.13f, 0.06f, 0.11f), black);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.12f * s, 0.06f, 0));
            var hand = bc + V(0.2f * s, -0.06f, 0.08f);
            b.Ell(arm, bc + V(0.13f * s, 0.07f, 0), V(0.05f, 0.05f, 0.05f), brass, blend: 0.01f);
            b.Limb(arm, bc + V(0.13f * s, 0.06f, 0), hand, 0.03f, 0.028f, black);
            if (s > 0)
                b.Spike(arm, hand + V(0, 0.0f, -0.05f), hand + V(0.03f, 0.04f, 0.4f), 0.04f, red, mat: Shell);
            else
            {
                var shield = hand + V(-0.04f, 0.02f, 0.03f);
                b.Ell(arm, shield, V(0.03f, 0.13f, 0.13f), brass, V(0, -20f, 0), Metal, 0.01f);
                b.Torus(arm, shield + V(-0.012f, 0, 0), 0.12f, 0.012f, red, V(0, -20f, 90f), mat: Shell, blend: 0.006f);
                b.Spike(arm, shield + V(-0.02f, 0, 0.01f), shield + V(-0.08f, 0, 0.03f), 0.03f, red, mat: Shell);
            }
        });
        int head = b.Head(bc + V(0, 0.13f, 0.01f));
        var c = bc + V(0, 0.2f, 0.02f);
        var r = V(0.07f, 0.065f, 0.07f);
        b.Ell(head, c, r, brass);
        b.PaintEll(head, c + V(0, 0.0f, 0.06f), V(0.06f, 0.025f, 0.03f), black);
        b.Spike(head, c + V(0, 0.05f, 0.0f), c + V(0, 0.15f, 0.06f), 0.025f, red, 0.6f, Shell);
        b.Spike(head, c + V(0, 0.05f, -0.03f), c + V(0, 0.11f, -0.12f), 0.022f, red, 0.5f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.35f * s, 0.0f, 0.94f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(110, 214, 236));
        });
        return b;
    }

    // ------------------------------------------------------------------ Pincurchin

    /// <summary>Pincurchin: a dark purple sea urchin bristling with long spines tipped brown, two yellow front teeth like a beak and small eyes above them.</summary>
    private static PokeBuilder Pincurchin()
    {
        var b = new PokeBuilder("Pincurchin", 0.45f, BodyPlan.Floating, V(0, 0.1f, 0)) { Coat = Shell };
        var purple = Rgb(72, 58, 92);
        var tip = Rgb(160, 134, 108);
        var yellow = Rgb(242, 192, 52);
        var c = V(0, 0.1f, 0);
        var r = V(0.12f, 0.09f, 0.12f);
        b.Ell(Body, c, r, purple);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 22; i++)
        {
            float y = 1f - 1.5f * (i + 0.5f) / 22f, ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var dir = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (dir.Z > 0.6f && dir.Y < 0.4f) continue;
            var root = Out(c, r, default, dir) - dir * 0.02f;
            var end = root + dir * (0.12f + 0.04f * (i % 3) / 2f);
            b.Spike(Body, root, end, 0.022f, purple, blend: 0.01f);
            b.PaintEll(Body, Vector3.Lerp(root, end, 0.85f), V(0.02f, 0.03f, 0.02f), tip, Euler(dir), 0.006f);
        }
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, c + V(0.025f * s, -0.03f, 0.11f), V(0.03f, 0.025f, 0.02f), yellow, mat: Shell, blend: 0.008f);
            var at = Out(c, r, default, V(0.3f * s, 0.2f, 0.93f));
            b.Eye(Body, at, Outward(c, r, at), 0.011f, Rgb(240, 214, 90), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Snom and Frosmoth

    /// <summary>Snom: a little white larva lying on the ground with its head raised, crystals of ice along its back and small black eyes.</summary>
    private static PokeBuilder Snom()
    {
        var white = Rgb(244, 246, 250);
        var ice = Rgb(190, 222, 246);
        var points = Smooth(2, V(0, 0.04f, -0.13f), V(0, 0.045f, -0.05f), V(0, 0.055f, 0.03f), V(0, 0.08f, 0.08f));
        var b = new PokeBuilder("Snom", 0.35f, BodyPlan.Serpent, points[0]) { Coat = Fur };
        int neck = Coils(b, points, t => 0.032f + 0.016f * MathF.Min(1f, t * 2f), white);
        for (int i = 0; i < points.Length - 1; i++)
        {
            int seg = i < 2 ? Body : b.Model.Skeleton.Find("seg" + i / 2);
            float rad = 0.032f + 0.016f * MathF.Min(1f, i / (float)(points.Length - 1) * 2f);
            b.Spike(seg, points[i] + V(0, rad * 0.6f, 0), points[i] + V((i % 2 == 0 ? 0.015f : -0.015f), rad + 0.035f, -0.01f), 0.016f, ice, mat: Glow, blend: 0.004f);
        }
        int head = b.Head(points[^1], neck);
        var c = points[^1] + V(0, 0.01f, 0.04f);
        var r = V(0.05f, 0.045f, 0.05f);
        b.Ell(head, c, r, white);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.5f * s, 0.15f, 0.85f));
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(30, 30, 40));
        });
        return b;
    }

    /// <summary>Frosmoth: a great moth of ice floating on broad white wings patterned pale blue, a ruff of white down at its neck, feathery white antennae and blue eyes under heavy lids.</summary>
    private static PokeBuilder Frosmoth()
    {
        var b = new PokeBuilder("Frosmoth", 0.9f, BodyPlan.Bird, V(0, 0.4f, 0)) { Coat = Fur }.Hover();
        var white = Rgb(246, 248, 252);
        var ice = Rgb(196, 224, 246);
        var bc = V(0, 0.4f, 0);
        b.Ell(Body, bc, V(0.065f, 0.065f, 0.1f), white);
        b.Spike(Body, bc + V(0, -0.01f, -0.07f), bc + V(0, -0.06f, -0.2f), 0.045f, white);
        FurTufts(b, Body, bc + V(0, 0.03f, 0.06f), V(0.06f, 0.05f, 0.04f), 12, 0.035f, 0.015f, white, 2f, -0.95f, 0.5f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.04f * s, 0.03f, 0.01f));
            var root = bc + V(0.04f * s, 0.03f, 0.01f);
            Frond(b, wing, root, root + V(0.36f * s, 0.16f, -0.02f), 0.16f, white, V(0, 0.35f, 1f), 0.1f);
            Frond(b, wing, root + V(0, -0.02f, -0.03f), root + V(0.26f * s, -0.12f, -0.06f), 0.11f, white, V(0, 0.2f, 1f), 0.1f);
            foreach (var (at, k) in new[] { (root + V(0.22f * s, 0.1f, -0.01f), 1f), (root + V(0.12f * s, 0.05f, 0.0f), 0.7f), (root + V(0.16f * s, -0.07f, -0.045f), 0.8f) })
                b.PaintEll(wing, at, V(0.04f, 0.04f, 0.03f) * k, ice, soft: 0.008f);
        });
        int head = b.Head(bc + V(0, 0.04f, 0.08f));
        var c = bc + V(0, 0.07f, 0.1f);
        var r = V(0.055f, 0.045f, 0.05f);
        b.Ell(head, c, r, white);
        PokeBuilder.Both(s =>
        {
            Frond(b, head, c + V(0.02f * s, 0.035f, -0.01f), c + V(0.1f * s, 0.12f, -0.04f), 0.025f, white, V(0, 0.3f, 1f), 0.2f);
            var at = Out(c, r, default, V(0.5f * s, 0.15f, 0.85f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(70, 110, 220), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Stonjourner

    /// <summary>Stonjourner: a standing stone come to life, two grey pillars dark with earth at their feet and marked with crossed bands, a lintel across them for a head with a small face, a round stone on its top and two stone blocks at its ends.</summary>
    private static PokeBuilder Stonjourner()
    {
        var b = new PokeBuilder("Stonjourner", 1f, BodyPlan.Biped, V(0, 0.66f, 0)) { Coat = Shell };
        var grey = Rgb(150, 150, 156);
        var light = Rgb(186, 186, 192);
        var dirt = Rgb(132, 98, 72);
        var dark = Rgb(84, 82, 88);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.13f * s, 0.55f, 0));
            b.Box(leg, V(0.13f * s, 0.31f, 0), V(0.07f, 0.31f, 0.06f), 0.025f, grey, blend: 0.006f);
            b.PaintEll(leg, V(0.13f * s, 0.06f, 0), V(0.1f, 0.13f, 0.08f), dirt);
            foreach (float t in new[] { -1f, 1f })
                b.Box(leg, V(0.13f * s, 0.12f, 0.062f), V(0.012f, 0.06f, 0.004f), 0.003f, light, V(0, 0, 35f * t), blend: 0.003f);
        });
        var lc = V(0, 0.66f, 0);
        b.Box(Body, lc, V(0.22f, 0.06f, 0.075f), 0.02f, light, blend: 0.01f);
        int head = b.Head(lc + V(0, 0.06f, 0));
        b.Ell(head, lc + V(0, 0.075f, 0), V(0.05f, 0.02f, 0.05f), dark, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var cube = lc + V(0.18f * s, 0.12f, 0);
            b.Limb(head, lc + V(0.17f * s, 0.05f, 0), cube, 0.015f, 0.015f, grey);
            b.Box(head, cube, V(0.035f, 0.035f, 0.035f), 0.006f, dark, V(0, 20f * s, 15f * s), blend: 0.004f);
            b.Eye(Body, lc + V(0.025f * s, 0.008f, 0.075f), V(0, 0, 1f), 0.011f, Rgb(40, 38, 44));
        });
        b.Mark(Body, lc + V(0, -0.022f, 0.075f), V(0, 0, 1f), 0.016f, 0.007f, Rgb(70, 66, 72), MarkShape.Bar);
        return b;
    }

    // ------------------------------------------------------------------ Eiscue

    /// <summary>
    /// Eiscue with its Ice Face or its Noice Face. A penguin, black with a white front, yellow feet and flippers held
    /// out, whose head is a cube of ice with its face seen through the front, a blue beak and a dark feeler rising from
    /// its top; without the ice, its head is small and pale blue, its feeler kinked.
    /// </summary>
    private static PokeBuilder EiscueBuild(bool noice)
    {
        var b = new PokeBuilder(noice ? "Eiscue-Noice" : "Eiscue", 0.7f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Fur };
        var black = Rgb(42, 46, 64);
        var white = Rgb(244, 246, 250);
        var yellow = Rgb(240, 190, 60);
        var blue = Rgb(80, 150, 214);
        var ice = Rgb(176, 216, 240);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.06f, 0.01f));
            b.Ell(leg, V(0.04f * s, 0.012f, 0.04f), V(0.025f, 0.012f, 0.04f), yellow);
            b.Limb(leg, V(0.035f * s, 0.06f, 0.01f), V(0.04f * s, 0.015f, 0.03f), 0.012f, 0.01f, yellow);
        });
        var bc = V(0, 0.18f, 0);
        b.Ell(Body, bc, V(0.075f, 0.13f, 0.065f), black);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.05f), V(0.055f, 0.1f, 0.03f), white);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.06f * s, 0.06f, 0));
            Frond(b, arm, bc + V(0.06f * s, 0.06f, 0), bc + V(0.13f * s, -0.07f, 0.01f), 0.025f, black, V(s, 0, 0.3f), 0.3f);
        });
        int head = b.Head(bc + V(0, 0.12f, 0.01f));
        if (noice)
        {
            var c = bc + V(0, 0.17f, 0.02f);
            var r = V(0.055f, 0.05f, 0.05f);
            b.Ell(head, c, r, Rgb(150, 210, 236));
            b.Spike(head, c + V(0, -0.01f, 0.04f), c + V(0, -0.02f, 0.08f), 0.014f, blue, 0.6f, Shell);
            b.Tube(head, new[] { c + V(0, 0.045f, 0), c + V(0.01f, 0.08f, 0), c + V(-0.015f, 0.1f, 0.0f), c + V(0.01f, 0.14f, -0.01f) }, 0.004f, 0.003f, black, blend: 0f);
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.02f * s, c.Y + 0.008f);
                b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(30, 30, 40));
            });
            return b;
        }
        // The cube of ice, the face seen through its front
        var cc = bc + V(0, 0.22f, 0.02f);
        b.Box(head, cc, V(0.1f, 0.09f, 0.1f), 0.02f, ice, mat: Glow, blend: 0.006f);
        b.PaintEll(head, cc + V(0, 0, 0.1f), V(0.06f, 0.055f, 0.02f), Rgb(150, 200, 230), soft: 0.01f);
        b.Spike(head, cc + V(0, -0.025f, 0.1f), cc + V(0, -0.035f, 0.14f), 0.016f, blue, 0.6f, Shell);
        b.Tube(head, new[] { cc + V(0, 0.09f, 0), cc + V(0.01f, 0.14f, 0), cc + V(0.02f, 0.2f, -0.01f) }, 0.004f, 0.003f, black, blend: 0f);
        PokeBuilder.Both(s => b.Eye(head, cc + V(0.03f * s, 0.01f, 0.101f), V(0, 0, 1f), 0.011f, Rgb(30, 30, 40)));
        return b;
    }

    private static PokeBuilder Eiscue() => EiscueBuild(false);

    // ------------------------------------------------------------------ Indeedee

    /// <summary>
    /// Indeedee, male or female: a butler or a maid, dark purple with a round lower body on small legs, a face of pale
    /// lilac and brown eyes under great curled locks like a ram's horns. The male wears a white front from collar to
    /// belly; the female is white below the waist like an apron, her locks fuller.
    /// </summary>
    private static PokeBuilder IndeedeeBuild(bool female)
    {
        var b = new PokeBuilder(female ? "Indeedee-Female" : "Indeedee", 0.7f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var dark = Rgb(70, 62, 112);
        var white = Rgb(244, 242, 248);
        var lilac = Rgb(214, 206, 230);
        var curl = Rgb(150, 140, 176);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.07f, 0.01f));
            b.Limb(leg, V(0.04f * s, 0.08f, 0.01f), V(0.045f * s, 0.02f, 0.02f), 0.022f, 0.02f, dark);
            b.Ell(leg, V(0.046f * s, 0.014f, 0.035f), V(0.022f, 0.014f, 0.03f), white);
        });
        var bc = V(0, 0.15f, 0);
        b.Ell(Body, bc, V(0.1f, 0.1f, 0.085f), dark);
        b.Ell(Body, bc + V(0, 0.11f, 0), V(0.055f, 0.07f, 0.045f), dark, blend: 0.03f);
        if (female)
            b.PaintEll(Body, bc + V(0, -0.04f, 0.04f), V(0.1f, 0.09f, 0.07f), white);
        else
            b.PaintEll(Body, bc + V(0, 0.08f, 0.045f), V(0.03f, 0.1f, 0.03f), white);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.05f * s, 0.15f, 0));
            var hand = bc + V(0.1f * s, s > 0 ? 0.18f : 0.06f, 0.05f);
            b.Limb(arm, bc + V(0.05f * s, 0.15f, 0), hand, 0.016f, 0.014f, dark);
            b.Ell(arm, hand, V(0.018f, 0.018f, 0.018f), white);
        });
        int head = b.Head(bc + V(0, 0.18f, 0.01f));
        var c = bc + V(0, 0.24f, 0.02f);
        var r = V(0.055f, 0.05f, 0.05f);
        b.Limb(head, bc + V(0, 0.17f, 0), c, 0.025f, 0.025f, dark);
        b.Ell(head, c, r, dark);
        b.PaintEll(head, c + V(0, -0.01f, 0.04f), V(0.045f, 0.04f, 0.03f), lilac);
        PokeBuilder.Both(s =>
        {
            // The locks curling down like a ram's horns
            var root = c + V(0.04f * s, 0.035f, -0.01f);
            var centre = c + V(0.09f * s, 0.0f, -0.01f);
            float size = female ? 0.055f : 0.045f;
            b.Ell(head, centre, V(size * 0.8f, size, size * 0.8f), curl, blend: 0.02f);
            Curl(b, head, root, centre + V(0.01f * s, 0, 0.0f), V(0, 1f, 0), V(s, 0, 0), size, 1.1f, size * 0.45f, curl);
            var at = On(c, r, 0.02f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(130, 90, 60), glare: true);
        });
        int tail = b.Tail(bc + V(0, -0.02f, -0.08f));
        b.Ell(tail, bc + V(0, -0.02f, -0.1f), V(0.025f, 0.025f, 0.025f), dark, blend: 0.01f);
        return b;
    }

    private static PokeBuilder Indeedee() => IndeedeeBuild(false);

    // ------------------------------------------------------------------ Morpeko

    /// <summary>
    /// Morpeko in its Full Belly or Hangry Mode. A little hamster, round as a bean, yellow below with its back and ears
    /// split into brown on one side and grey on the other, pink cheeks and a seed in its paw; hangry, it is grey above
    /// and purple below, red-eyed and scowling.
    /// </summary>
    private static PokeBuilder MorpekoBuild(bool hangry)
    {
        var b = new PokeBuilder(hangry ? "Morpeko-Hangry" : "Morpeko", 0.45f, BodyPlan.Biped, V(0, 0.14f, 0)) { Coat = Fur };
        var lower = hangry ? Rgb(132, 92, 172) : Rgb(242, 216, 92);
        var left = hangry ? Rgb(84, 82, 90) : Rgb(152, 112, 72);
        var right = Rgb(84, 82, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.04f, 0.01f));
            b.Ell(leg, V(0.04f * s, 0.016f, 0.03f), V(0.022f, 0.016f, 0.03f), lower);
        });
        var c = V(0, 0.15f, 0);
        var r = V(0.075f, 0.12f, 0.065f);
        b.Ell(Body, c, r, lower);
        b.PaintEll(Body, c + V(-0.045f, 0.09f, -0.01f), V(0.06f, 0.07f, 0.075f), left);
        b.PaintEll(Body, c + V(0.045f, 0.09f, -0.01f), V(0.06f, 0.07f, 0.075f), right);
        if (hangry)
            b.PaintEll(Body, c + V(0.0f, 0.04f, 0.05f), V(0.02f, 0.03f, 0.03f), Rgb(170, 110, 220), V(0, 0, 30f), 0.006f);
        int head = b.Head(c + V(0, 0.08f, 0));
        PokeBuilder.Both(s =>
        {
            var ear = s < 0 ? left : right;
            b.Ell(head, c + V(0.05f * s, 0.14f, -0.01f), V(0.025f, 0.06f, 0.02f), ear, V(0, 0, -30f * s), blend: 0.015f);
            b.PaintEll(head, c + V(0.075f * s, 0.185f, -0.01f), V(0.02f, 0.02f, 0.025f), PixelCanvas.Mix(ear, Black, 0.3f), soft: 0.006f);
            var cheek = Out(c, r, default, V(0.75f * s, 0.15f, 0.65f));
            b.Mark(Body, cheek, Outward(c, r, cheek), 0.014f, 0.012f, Rgb(244, 140, 160));
            var at = On(c, r, 0.027f * s, c.Y + 0.05f);
            b.Eye(Body, at, Outward(c, r, at), 0.013f, hangry ? Rgb(214, 40, 50) : Rgb(40, 30, 30), glare: hangry);
            int arm = b.Arm(s, c + V(0.06f * s, 0.0f, 0.03f));
            var hand = c + V(0.09f * s, s > 0 && !hangry ? 0.04f : -0.02f, 0.05f);
            b.Limb(arm, c + V(0.06f * s, 0.0f, 0.03f), hand, 0.015f, 0.013f, lower);
            if (s > 0 && !hangry)
            {
                b.Limb(arm, hand, hand + V(0.01f, 0.05f, 0.0f), 0.005f, 0.004f, Rgb(120, 140, 70));
                b.Ell(arm, hand + V(0.012f, 0.06f, 0), V(0.016f, 0.016f, 0.016f), Rgb(150, 190, 90), blend: 0.004f);
            }
        });
        b.Mark(Body, On(c, r, 0, c.Y + 0.015f), V(0, -0.2f, 1f), 0.016f, 0.009f, Rgb(110, 40, 50), hangry ? MarkShape.Zigzag : MarkShape.Smile);
        int tail = b.Tail(c + V(0, -0.07f, -0.04f));
        b.Tube(tail, new[] { c + V(0, -0.07f, -0.04f), c + V(0.03f, -0.09f, -0.11f), c + V(0.05f, -0.06f, -0.15f) }, 0.008f, 0.006f, left, blend: 0f);
        b.Ell(tail, c + V(0.05f, -0.06f, -0.15f), V(0.014f, 0.014f, 0.014f), left, blend: 0.004f);
        return b;
    }

    private static PokeBuilder Morpeko() => MorpekoBuild(false);
}
