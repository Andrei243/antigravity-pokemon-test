using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Galar's second batch in National Pokédex
// order: Wooloo (831) to Polteageist (855). Their forms are in PokemonModels.Regional.cs and PokemonModels.Gigantamax.cs
// with the other forms. Helpers shared with the earlier batches are in the files of those batches,
// PokemonModels.Sinnoh1.cs to PokemonModels.Galar1.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Wooloo line

    /// <summary>Wooloo: a little sheep that is nearly all wool, a round fleece of white with a dark face peeping out of its front, pink ears, pale eyes, curls of wool by its cheeks and short dark legs.</summary>
    private static PokeBuilder Wooloo()
    {
        var b = new PokeBuilder("Wooloo", 0.5f, BodyPlan.Quadruped, V(0, 0.17f, 0)) { Coat = Fur };
        var wool = Rgb(244, 242, 236);
        var shade = Rgb(212, 210, 206);
        var dark = Rgb(66, 58, 60);
        var pink = Rgb(236, 168, 176);
        foreach (var (z, front) in new[] { (0.06f, true), (-0.07f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s, 0.08f, z), front);
                b.Limb(leg, V(0.07f * s, 0.09f, z), V(0.075f * s, 0.025f, z + 0.01f), 0.022f, 0.02f, dark);
                b.Ell(leg, V(0.075f * s, 0.018f, z + 0.015f), V(0.024f, 0.018f, 0.028f), dark);
            });
        var bc = V(0, 0.17f, -0.02f);
        var br = V(0.15f, 0.12f, 0.15f);
        b.Ell(Body, bc, br, wool);
        Lumps(b, Body, bc, br, 40, 0.045f, wool, shade, 0.1f);
        int head = b.Head(V(0, 0.17f, 0.1f));
        var c = V(0, 0.17f, 0.13f);
        var r = V(0.055f, 0.06f, 0.045f);
        b.Ell(head, c, r, dark);
        b.Ell(head, c + V(0, 0.055f, -0.01f), V(0.045f, 0.03f, 0.035f), wool, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.06f * s, 0.012f, -0.01f), V(0.026f, 0.011f, 0.018f), pink, V(0, 0, -25f * s), blend: 0.008f);
            b.Ell(head, c + V(0.055f * s, -0.045f, 0.0f), V(0.025f, 0.04f, 0.025f), shade, blend: 0.012f);
            var at = On(c, r, 0.022f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, sclera: true, white: Rgb(230, 226, 170), pupil: Rgb(40, 34, 36));
        });
        b.Ell(Body, bc + V(0, 0.02f, -0.15f), V(0.035f, 0.035f, 0.03f), wool, blend: 0.015f);
        return b;
    }

    /// <summary>Dubwool: a sheep grown tall, its fleece white with dark brown patches, a collar of grey wool round its neck and a beard under its dark face, great brown horns curling round its ears and long dark legs.</summary>
    private static PokeBuilder Dubwool()
    {
        var b = new PokeBuilder("Dubwool", 0.78f, BodyPlan.Quadruped, V(0, 0.33f, 0)) { Coat = Fur };
        var wool = Rgb(244, 242, 236);
        var shade = Rgb(212, 210, 206);
        var patch = Rgb(92, 66, 58);
        var grey = Rgb(150, 148, 152);
        var dark = Rgb(62, 54, 58);
        var horn = Rgb(132, 82, 62);
        BeastLegs(b, 0.07f, 0.26f, 0.1f, -0.13f, 0.028f, dark, dark);
        var bc = V(0, 0.34f, -0.03f);
        var br = V(0.17f, 0.14f, 0.2f);
        b.Ell(Body, bc, br, wool);
        Lumps(b, Body, bc, br, 44, 0.05f, wool, shade, 9f);
        foreach (var (dir, k) in new[] { (V(0.85f, 0.2f, -0.3f), 1f), (V(-0.8f, 0.4f, 0.1f), 0.8f), (V(0.3f, 0.9f, -0.5f), 0.7f), (V(-0.6f, 0.2f, -0.7f), 0.9f) })
            b.PaintEll(Body, Out(bc, br, default, dir), V(0.07f, 0.06f, 0.07f) * k, patch);
        // The collar of grey wool round the neck, and the beard below the face
        var collar = V(0, 0.4f, 0.15f);
        b.Ell(Body, collar, V(0.12f, 0.11f, 0.09f), grey, blend: 0.03f);
        Lumps(b, Body, collar, V(0.12f, 0.11f, 0.09f), 22, 0.035f, grey, Rgb(176, 174, 178), 0.42f);
        int head = b.Head(collar + V(0, 0.02f, 0.05f));
        var c = V(0, 0.42f, 0.25f);
        var r = V(0.045f, 0.06f, 0.06f);
        b.Ell(head, c, r, dark, V(20f, 0, 0));
        b.Ell(head, c + V(0, -0.025f, 0.045f), V(0.03f, 0.03f, 0.03f), dark, blend: 0.02f);
        FurTufts(b, head, c + V(0, -0.06f, 0.0f), V(0.04f, 0.03f, 0.04f), 8, 0.04f, 0.014f, grey, 2f, -0.95f, 0.9f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, V(20f, 0, 0), V(0.55f * s, 0.3f, 0.78f));
            b.Eye(head, at, at - c, 0.012f, sclera: true, white: Rgb(230, 226, 170), pupil: Rgb(40, 34, 36));
            // The horns curl back and round past the ears
            Curl(b, head, c + V(0.03f * s, 0.05f, -0.03f), c + V(0.1f * s, 0.01f, -0.07f), V(0, 1f, 0), V(0, 0, -1f), 0.065f, 1.15f, 0.026f, horn);
            b.Ell(head, c + V(0.05f * s, 0.02f, -0.03f), V(0.025f, 0.01f, 0.015f), Rgb(236, 170, 176), V(0, 0, -30f * s), blend: 0.006f);
        });
        int tail = b.Tail(bc + V(0, 0.04f, -0.19f));
        b.Ell(tail, bc + V(0, 0.04f, -0.22f), V(0.045f, 0.045f, 0.04f), grey, blend: 0.015f);
        return b;
    }

    // ------------------------------------------------------------------ Chewtle line

    /// <summary>Chewtle: a little snapping turtle with a big teal head, an orange horn on its crown, its jaws wide open and orange, an angry stare, and a small shell on its back over stubby legs.</summary>
    private static PokeBuilder Chewtle()
    {
        var b = new PokeBuilder("Chewtle", 0.45f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Shell };
        var teal = Rgb(112, 192, 192);
        var orange = Rgb(238, 150, 52);
        var shell = Rgb(80, 128, 116);
        var cream = Rgb(238, 224, 176);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.06f, 0.0f));
            b.Ell(leg, V(0.05f * s, 0.03f, 0.015f), V(0.03f, 0.03f, 0.04f), teal);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.05f * s + 0.012f * i, 0.012f, 0.045f), V(0.05f * s + 0.014f * i, 0.006f, 0.062f), 0.007f, cream, mat: Shell, blend: 0.003f);
        });
        b.Ell(Body, V(0, 0.1f, -0.01f), V(0.06f, 0.06f, 0.055f), cream);
        b.Ell(Body, V(0, 0.12f, -0.045f), V(0.066f, 0.062f, 0.042f), shell, blend: 0.012f);
        b.PaintTorus(Body, V(0, 0.12f, -0.03f), 0.06f, 0.008f, orange, V(90f, 0, 0), sz: 1.05f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.12f, 0.01f));
            b.Limb(arm, V(0.05f * s, 0.12f, 0.01f), V(0.075f * s, 0.08f, 0.04f), 0.016f, 0.014f, teal);
        });
        int head = b.Head(V(0, 0.16f, 0.01f));
        var c = V(0, 0.24f, 0.02f);
        var r = V(0.09f, 0.08f, 0.08f);
        b.Ell(head, c, r, teal);
        // The jaws: an orange lower jaw thrust forward, the mouth wide open above it
        b.Ell(head, c + V(0, -0.05f, 0.035f), V(0.075f, 0.04f, 0.06f), orange, blend: 0.015f);
        Gape(b, head, c + V(0, -0.03f, 0.075f), V(0.05f, 0.028f, 0.03f), Rgb(196, 80, 74), 0.012f);
        b.Spike(head, c + V(0, 0.06f, -0.01f), c + V(0.012f, 0.16f, -0.025f), 0.035f, orange, mat: Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.042f * s, c.Y + 0.03f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(50, 40, 40), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Drednaw and its Gigantamax form. Drednaw is a great snapping turtle on all fours, teal under a brown shell with
    /// a ridge of gold spikes, a gold helmet with a horn over a head of jaws set with white teeth, and a tail spiked
    /// gold. The Gigantamax Drednaw rears up on its hind legs, its shell grown into a cliff of red-brown rock behind it,
    /// Gigantamax's red clouds rising from its top.
    /// </summary>
    private static PokeBuilder DrednawBuild(bool gmax)
    {
        var teal = Rgb(70, 168, 170);
        var shell = Rgb(150, 98, 66);
        var gold = Rgb(222, 162, 62);
        var cream = Rgb(238, 228, 202);
        if (gmax)
        {
            var g = new PokeBuilder("Drednaw-Gmax", 1f, BodyPlan.Biped, V(0, 0.45f, 0)) { Coat = Shell };
            var rock = Rgb(130, 62, 50);
            var stripe = Rgb(214, 104, 60);
            PokeBuilder.Both(s =>
            {
                int leg = g.Leg(s, V(0.11f * s, 0.24f, 0.0f));
                var knee = V(0.15f * s, 0.14f, 0.05f);
                g.Limb(leg, V(0.11f * s, 0.26f, 0.0f), knee, 0.08f, 0.065f, teal);
                g.Limb(leg, knee, V(0.15f * s, 0.04f, 0.04f), 0.065f, 0.06f, teal);
                g.Ell(leg, V(0.15f * s, 0.03f, 0.08f), V(0.07f, 0.03f, 0.08f), teal);
                Claws(g, leg, V(0.15f * s, 0.02f, 0.15f), V(0.03f, 0, 0), V(0, -0.2f, 1f), 0.035f, 0.012f);
            });
            var bc = V(0, 0.46f, 0.02f);
            g.Ell(Body, bc, V(0.17f, 0.24f, 0.14f), teal);
            g.PaintEll(Body, bc + V(0, -0.02f, 0.12f), V(0.12f, 0.2f, 0.06f), cream);
            for (int i = 0; i < 4; i++)
                g.PaintEll(Body, bc + V(0, 0.15f - i * 0.1f, 0.135f), V(0.11f, 0.008f, 0.03f), Rgb(196, 186, 160), soft: 0.005f);
            // The shell: a cliff of rock on its back, striped, its crest broken into crags
            var sc = V(0, 0.52f, -0.15f);
            var sr = V(0.27f, 0.36f, 0.13f);
            g.Ell(Body, sc, sr, rock, V(-10f, 0, 0));
            for (int i = 0; i < 4; i++)
                g.PaintEll(Body, sc + V(0, 0.22f - i * 0.15f, -0.08f), V(0.3f, 0.018f, 0.1f), stripe, V(-10f, 0, 0));
            for (int i = 0; i < 7; i++)
            {
                float a = (i / 6f - 0.5f) * 2.6f;
                var root = sc + V(MathF.Sin(a) * 0.24f, MathF.Cos(a) * 0.3f, -0.03f);
                g.Spike(Body, root, root + V(MathF.Sin(a) * 0.08f, MathF.Cos(a) * 0.1f + 0.03f, -0.02f), 0.05f, PixelCanvas.Mix(rock, Black, 0.2f), 0.6f);
            }
            PokeBuilder.Both(s =>
            {
                int arm = g.Arm(s, V(0.15f * s, 0.58f, 0.04f));
                var hand = V(0.23f * s, 0.36f, 0.14f);
                g.Limb(arm, V(0.15f * s, 0.58f, 0.04f), hand, 0.065f, 0.055f, teal);
                g.Ell(arm, hand, V(0.055f, 0.045f, 0.06f), teal);
                Claws(g, arm, hand + V(0, -0.02f, 0.05f), V(0.025f, 0, 0), V(0, -0.3f, 1f), 0.035f, 0.011f);
            });
            int gh = g.Head(V(0, 0.66f, 0.06f));
            var gc = V(0, 0.75f, 0.12f);
            var gr = V(0.085f, 0.07f, 0.1f);
            g.Limb(gh, V(0, 0.64f, 0.04f), gc, 0.08f, 0.07f, teal);
            DrednawHead(g, gh, gc, gr, teal, Rgb(120, 140, 170), cream);
            MaxClouds(g, Body, sc + V(0.05f, 0.34f, 0.0f), 0.04f, 0.12f, 0.24f, 0.9f, 0.4f);
            return g;
        }
        var b = new PokeBuilder("Drednaw", 0.95f, BodyPlan.Quadruped, V(0, 0.28f, 0)) { Coat = Shell };
        BeastLegs(b, 0.15f, 0.17f, 0.15f, -0.15f, 0.065f, teal, teal);
        var c0 = V(0, 0.25f, -0.02f);
        b.Ell(Body, c0, V(0.17f, 0.1f, 0.25f), cream);
        // The shell, its ridge of gold spikes and a gold rim
        var shc = V(0, 0.32f, -0.04f);
        var shr = V(0.22f, 0.13f, 0.28f);
        b.Ell(Body, shc, shr, shell);
        b.PaintTorus(Body, shc + V(0, -0.06f, 0), 0.2f, 0.02f, gold, sz: 1.3f);
        for (int i = 0; i < 4; i++)
        {
            float z = 0.14f - i * 0.1f;
            var root = Out(shc, shr, default, V(0, 1f, z / shr.Z));
            b.Spike(Body, root - V(0, 0.02f, 0), root + V(0, 0.07f, -0.04f), 0.035f, gold, 0.5f);
        }
        foreach (var dir in new[] { V(0.6f, 0.6f, 0.3f), V(-0.6f, 0.6f, 0.3f), V(0.6f, 0.6f, -0.4f), V(-0.6f, 0.6f, -0.4f) })
            b.PaintEll(Body, Out(shc, shr, default, dir), V(0.06f, 0.04f, 0.06f), PixelCanvas.Mix(shell, Black, 0.18f));
        int head = b.Head(V(0, 0.3f, 0.22f));
        var c = V(0, 0.34f, 0.38f);
        var r = V(0.1f, 0.085f, 0.12f);
        b.Limb(head, V(0, 0.29f, 0.2f), c, 0.085f, 0.075f, teal);
        DrednawHead(b, head, c, r, teal, gold, cream);
        int tail = b.Tail(c0 + V(0, 0.0f, -0.24f));
        var tp = Smooth(3, c0 + V(0, 0.0f, -0.24f), c0 + V(0, -0.04f, -0.34f), c0 + V(0, -0.1f, -0.42f));
        b.Tube(tail, tp, 0.05f, 0.02f, teal, blend: 0f);
        for (int i = 1; i < tp.Length - 1; i += 2)
            b.Spike(tail, tp[i], tp[i] + V(0, 0.05f, -0.02f), 0.02f, gold, 0.6f, blend: 0.006f);
        return b;
    }

    /// <summary>Drednaw's head: a helmet with a horn jutting forward over its brow, jaws set with white teeth and a fierce eye on each side.</summary>
    private static void DrednawHead(PokeBuilder b, int head, Vector3 c, Vector3 r, Color skin, Color helm, Color teeth)
    {
        b.Ell(head, c, r, skin);
        b.Ell(head, c + V(0, 0.045f, -0.01f), V(r.X * 1.02f, r.Y * 0.55f, r.Z * 0.95f), helm, blend: 0.012f);
        b.Spike(head, c + V(0, 0.07f, 0.03f), c + V(0, 0.11f, 0.13f), 0.035f, helm, 0.6f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.07f * s, 0.05f, -0.03f), c + V(0.12f * s, 0.07f, -0.08f), 0.025f, helm, 0.6f));
        // The lower jaw, jutting, its row of teeth pointing up
        b.Ell(head, c + V(0, -0.045f, 0.03f), V(r.X * 0.95f, r.Y * 0.45f, r.Z * 0.95f), skin, blend: 0.012f);
        for (int i = -2; i <= 2; i++)
        {
            float a = i * 0.45f;
            var root = c + V(MathF.Sin(a) * r.X * 0.85f, -0.03f, MathF.Cos(a) * r.Z * 0.95f);
            b.Spike(head, root, root + V(0, 0.03f, 0.005f), 0.011f, teeth, mat: Shell, blend: 0.004f);
        }
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.7f * s, 0.2f, 0.65f));
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(232, 196, 60), glare: true);
        });
    }

    private static PokeBuilder Drednaw() => DrednawBuild(false);

    // ------------------------------------------------------------------ Yamper line

    /// <summary>Yamper: a little corgi pup, cream in front and tan behind, a big yellow ruff on its chest, tall ears green-yellow inside, its tongue out and a little tail like a lightning bolt.</summary>
    private static PokeBuilder Yamper()
    {
        var b = new PokeBuilder("Yamper", 0.5f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Fur };
        var cream = Rgb(246, 238, 216);
        var tan = Rgb(198, 162, 120);
        var yellow = Rgb(250, 222, 72);
        var white = Rgb(250, 250, 246);
        BeastLegs(b, 0.045f, 0.09f, 0.05f, -0.06f, 0.022f, tan, white);
        var bc = V(0, 0.14f, -0.01f);
        b.Ell(Body, bc, V(0.075f, 0.07f, 0.11f), tan);
        b.Ell(Body, bc + V(0, 0.01f, 0.08f), V(0.075f, 0.07f, 0.05f), yellow, blend: 0.02f);
        int head = b.Head(bc + V(0, 0.07f, 0.06f));
        var c = bc + V(0, 0.11f, 0.08f);
        var r = V(0.07f, 0.06f, 0.06f);
        b.Ell(head, c, r, cream);
        b.PaintEll(head, c + V(0, 0.05f, -0.03f), V(0.07f, 0.04f, 0.06f), tan);
        b.Ell(head, c + V(0, -0.018f, 0.05f), V(0.032f, 0.022f, 0.025f), cream, blend: 0.012f);
        b.Ell(head, c + V(0, -0.01f, 0.074f), V(0.009f, 0.007f, 0.006f), Rgb(60, 48, 46), blend: 0.003f);
        b.Limb(head, c + V(0.005f, -0.035f, 0.06f), c + V(0.01f, -0.06f, 0.075f), 0.012f, 0.011f, Rgb(236, 120, 130), blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            FoxEar(b, head, c + V(0.04f * s, 0.035f, -0.01f), c + V(0.1f * s, 0.12f, -0.02f), 0.03f, tan, Rgb(190, 214, 96));
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(150, 170, 50));
        });
        // The tail: a little bolt of yellow
        int tail = b.Tail(bc + V(0, 0.03f, -0.1f));
        b.Tube(tail, new[] { bc + V(0, 0.03f, -0.1f), bc + V(0, 0.09f, -0.13f), bc + V(0.025f, 0.08f, -0.15f), bc + V(0, 0.15f, -0.18f) }, 0.018f, 0.012f, yellow, blend: 0f);
        return b;
    }

    /// <summary>Boltund: a lean racing dog, dark olive, its head yellow with long ears swept back, white muzzle and paws, yellow bands round its legs and a long tail ending in a yellow bolt.</summary>
    private static PokeBuilder Boltund()
    {
        var b = new PokeBuilder("Boltund", 0.8f, BodyPlan.Quadruped, V(0, 0.33f, 0)) { Coat = Fur };
        var dark = Rgb(86, 98, 76);
        var yellow = Rgb(246, 212, 64);
        var white = Rgb(246, 246, 240);
        BeastLegs(b, 0.05f, 0.26f, 0.12f, -0.14f, 0.033f, dark, white, dark, yellow);
        var bc = V(0, 0.3f, -0.01f);
        b.Ell(Body, bc, V(0.075f, 0.08f, 0.18f), dark);
        b.PaintEll(Body, bc + V(0, -0.04f, 0.1f), V(0.05f, 0.04f, 0.07f), PixelCanvas.Mix(dark, White, 0.3f));
        var neck = Smooth(3, bc + V(0, 0.03f, 0.13f), bc + V(0, 0.1f, 0.18f), bc + V(0, 0.13f, 0.2f));
        b.Tube(Body, neck, 0.045f, 0.035f, dark, blend: 0f);
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.025f, 0.035f);
        var r = V(0.045f, 0.045f, 0.065f);
        b.Ell(head, c, r, yellow);
        b.Spike(head, c + V(0, -0.012f, 0.04f), c + V(0, -0.02f, 0.11f), 0.028f, white, 0.8f);
        b.Ell(head, c + V(0, -0.016f, 0.106f), V(0.009f, 0.008f, 0.007f), Rgb(50, 46, 44), blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.025f * s, 0.03f, -0.03f), c + V(0.07f * s, 0.07f, -0.16f), 0.025f, yellow, V(0, 1f, 0.3f));
            var at = Out(c, r, default, V(0.55f * s, 0.25f, 0.8f));
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(150, 170, 50));
        });
        int tail = b.Tail(bc + V(0, 0.03f, -0.16f));
        var tp = Smooth(3, bc + V(0, 0.03f, -0.16f), bc + V(0, 0.08f, -0.26f), bc + V(0, 0.16f, -0.33f));
        b.Tube(tail, tp, 0.02f, 0.012f, dark, blend: 0f);
        b.Tube(tail, new[] { tp[^1], tp[^1] + V(0.03f, 0.05f, -0.03f), tp[^1] + V(-0.01f, 0.06f, -0.06f), tp[^1] + V(0.03f, 0.13f, -0.1f) }, 0.016f, 0.008f, yellow, blend: 0f);
        return b;
    }

    // ------------------------------------------------------------------ Rolycoly line

    /// <summary>Rolycoly: a lump of coal that rolls about, rough with chunks, one great orange eye glowing in a dark ring on its front.</summary>
    private static PokeBuilder Rolycoly()
    {
        var b = new PokeBuilder("Rolycoly", 0.45f, BodyPlan.Floating, V(0, 0.13f, 0)) { Coat = Shell };
        var coal = Rgb(62, 60, 66);
        var light = Rgb(98, 96, 104);
        var c = V(0, 0.13f, -0.02f);
        var r = V(0.13f, 0.12f, 0.12f);
        b.Ell(Body, c, r, coal);
        Lumps(b, Body, c, r, 28, 0.04f, coal, light, 0.06f);
        var face = V(0, 0.14f, 0.09f);
        b.Ell(Body, face, V(0.065f, 0.065f, 0.03f), Rgb(44, 42, 48), blend: 0.012f);
        b.Eye(Body, face + V(0.004f, 0, 0.03f), V(0, 0, 1f), 0.034f, Rgb(246, 120, 40), glare: true);
        return b;
    }

    /// <summary>Carkol: a cart of grey stone heaped with black coal that glows red in its cracks, its face low on the cart's front with orange eyes, four stubby wheels of legs.</summary>
    private static PokeBuilder Carkol()
    {
        var b = new PokeBuilder("Carkol", 0.65f, BodyPlan.Quadruped, V(0, 0.15f, 0)) { Coat = Shell };
        var stone = Rgb(84, 84, 90);
        var coal = Rgb(40, 38, 44);
        var light = Rgb(76, 72, 80);
        var lava = Rgb(236, 74, 44);
        foreach (var (z, front) in new[] { (0.1f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.11f * s, 0.06f, z), front);
                b.Limb(leg, V(0.11f * s, 0.07f, z), V(0.12f * s, 0.035f, z), 0.04f, 0.04f, stone);
            });
        b.Box(Body, V(0, 0.12f, 0), V(0.14f, 0.07f, 0.18f), 0.04f, stone, blend: 0.01f);
        var hc = V(0, 0.24f, -0.02f);
        var hr = V(0.15f, 0.13f, 0.17f);
        b.Ell(Body, hc, hr, coal, blend: 0.02f);
        Lumps(b, Body, hc, hr, 30, 0.04f, coal, light, 9f);
        // Glowing seams between the coals
        foreach (var dir in new[] { V(0.5f, 0.6f, 0.4f), V(-0.6f, 0.5f, 0.2f), V(0.2f, 0.8f, -0.5f), V(-0.4f, 0.7f, -0.4f), V(0.7f, 0.3f, -0.5f) })
            b.PaintEll(Body, Out(hc, hr, default, dir), V(0.05f, 0.012f, 0.05f), lava, Euler(dir), 0.006f);
        int head = b.Head(V(0, 0.12f, 0.15f));
        var c = V(0, 0.13f, 0.17f);
        var r = V(0.085f, 0.045f, 0.035f);
        b.Ell(head, c, r, Rgb(56, 56, 62), blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.45f * s, 0.15f, 0.88f));
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(250, 130, 40), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Coalossal and its Gigantamax form. Coalossal is a giant of black coal blocks, a heap of red-hot coal smouldering on
    /// its crown, its face low in its chest with burning orange eyes and a jagged glowing mouth, arms and legs of
    /// stacked blocks. The Gigantamax Coalossal is a tower of black rock, a crag of spikes for its crown with lava in its
    /// cracks and Gigantamax's red clouds over it.
    /// </summary>
    private static PokeBuilder CoalossalBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Coalossal-Gmax" : "Coalossal", 1f, BodyPlan.Biped, V(0, 0.45f, 0)) { Coat = Shell };
        var coal = Rgb(54, 52, 58);
        var light = Rgb(86, 84, 92);
        var lava = Rgb(242, 96, 40);
        var glow = Rgb(252, 186, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.24f, 0.0f));
            b.Box(leg, V(0.11f * s, 0.18f, 0.0f), V(0.07f, 0.08f, 0.07f), 0.02f, coal, V(0, 20f * s, 0), blend: 0.01f);
            b.Box(leg, V(0.12f * s, 0.05f, 0.02f), V(0.08f, 0.05f, 0.09f), 0.02f, light, V(0, -10f * s, 0), blend: 0.01f);
        });
        var bc = V(0, gmax ? 0.48f : 0.46f, 0);
        var br = gmax ? V(0.22f, 0.26f, 0.17f) : V(0.2f, 0.22f, 0.16f);
        b.Ell(Body, bc, br, coal);
        // Blocks of coal standing out of it
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 18; i++)
        {
            float y = 0.9f - 1.7f * (i + 0.5f) / 18f, ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var dir = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (dir.Z > 0.6f && dir.Y > -0.3f && dir.Y < 0.5f) continue;
            var at = Out(bc, br, default, dir);
            b.Box(Body, at, V(0.05f, 0.045f, 0.05f), 0.012f, i % 3 == 0 ? light : coal, Euler(dir) + V(0, 30f * i, 0), blend: 0.008f);
        }
        // The face low in the chest: burning eyes and a jagged mouth
        var face = bc + V(0, 0.04f, br.Z * 0.92f);
        b.Ell(Body, face, V(0.12f, 0.09f, 0.035f), Rgb(36, 34, 40), blend: 0.012f);
        PokeBuilder.Both(s => b.Eye(Body, face + V(0.055f * s, 0.025f, 0.033f), V(0.15f * s, 0, 1f), 0.02f, lava, glare: true));
        b.Mark(Body, face + V(0, -0.035f, 0.035f), V(0, 0, 1f), 0.055f, 0.015f, glow, MarkShape.Zigzag);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.19f * s, bc.Y + 0.1f, 0.0f));
            b.Box(arm, V(0.24f * s, bc.Y + 0.06f, 0.02f), V(0.07f, 0.08f, 0.07f), 0.02f, coal, V(0, 0, -15f * s), blend: 0.01f);
            b.Box(arm, V(0.28f * s, bc.Y - 0.08f, 0.05f), V(0.06f, 0.07f, 0.06f), 0.02f, light, V(0, 25f, 0), blend: 0.01f);
            b.Box(arm, V(0.29f * s, bc.Y - 0.2f, 0.07f), V(0.065f, 0.055f, 0.065f), 0.02f, coal, V(0, -15f, 10f * s), blend: 0.01f);
        });
        int head = b.Head(bc + V(0, br.Y * 0.8f, -0.02f));
        var top = bc + V(0, br.Y * 0.85f, -0.02f);
        if (gmax)
        {
            // A crag of black spikes, lava running in its cracks
            b.Spike(head, top, top + V(0, 0.38f, -0.03f), 0.16f, coal);
            foreach (var (x, h, z) in new[] { (0.12f, 0.24f, 0.02f), (-0.12f, 0.22f, -0.04f), (0.05f, 0.3f, -0.1f), (-0.06f, 0.18f, 0.08f), (0.15f, 0.14f, -0.08f) })
                b.Spike(head, top + V(x * 0.6f, 0.05f, z * 0.6f), top + V(x * 1.4f, h, z * 1.4f), 0.05f, PixelCanvas.Mix(coal, White, 0.08f), 0.7f);
            for (int i = 0; i < 6; i++)
                b.PaintEll(head, top + V(MathF.Sin(i * 2.1f) * 0.07f, 0.06f + i * 0.045f, 0.08f - i * 0.012f), V(0.02f, 0.035f, 0.03f), lava, soft: 0.006f);
            MaxClouds(b, head, top + V(0, 0.3f, -0.04f), 0.04f, 0.12f, 0.2f, 0.9f, 0.5f);
            return b;
        }
        // The heap of red-hot coal on its crown
        b.Spike(head, top, top + V(0, 0.24f, -0.02f), 0.18f, coal);
        Lumps(b, head, top + V(0, 0.07f, -0.01f), V(0.14f, 0.08f, 0.13f), 18, 0.035f, coal, light, 9f);
        b.Ell(head, top + V(0, 0.15f, -0.015f), V(0.1f, 0.075f, 0.09f), lava, mat: Glow, blend: 0.02f);
        b.PaintEll(head, top + V(0, 0.22f, -0.02f), V(0.06f, 0.035f, 0.055f), glow, soft: 0.008f);
        return b;
    }

    private static PokeBuilder Coalossal() => CoalossalBuild(false);

    // ------------------------------------------------------------------ Applin line

    /// <summary>Applin: a little worm living in a red apple, the apple's foot scalloped cream, two green leaves on top that are its head, with an eye on each, and the tip of its green tail poking out low on one side.</summary>
    private static PokeBuilder Applin()
    {
        var b = new PokeBuilder("Applin", 0.35f, BodyPlan.Floating, V(0, 0.13f, 0)) { Coat = Shell };
        var red = Rgb(216, 52, 64);
        var cream = Rgb(242, 222, 164);
        var green = Rgb(156, 204, 92);
        var c = V(0, 0.13f, 0);
        var r = V(0.12f, 0.12f, 0.115f);
        b.Ell(Body, c, r, red);
        b.PaintEll(Body, c + V(0, -0.11f, 0), V(0.13f, 0.05f, 0.13f), cream);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            b.PaintEll(Body, Out(c, r, default, V(MathF.Sin(a), -0.45f, MathF.Cos(a))), V(0.03f, 0.022f, 0.03f), cream, soft: 0.006f);
        }
        // The head: two round leaves standing up in a V, an eye on the front of each
        int head = b.Head(c + V(0, 0.11f, 0));
        b.Limb(head, c + V(0, 0.09f, 0), c + V(0, 0.13f, 0.0f), 0.022f, 0.018f, green);
        PokeBuilder.Both(s =>
        {
            var lc = c + V(0.045f * s, 0.19f, 0.0f);
            var lr = V(0.035f, 0.055f, 0.02f);
            b.Ell(head, lc, lr, green, V(0, 0, -25f * s), Leaf, 0.015f);
            b.Limb(head, c + V(0, 0.12f, 0), lc + V(-0.02f * s, -0.03f, 0), 0.014f, 0.012f, green);
            b.Eye(head, lc + V(-0.004f * s, 0.005f, lr.Z), V(0, 0, 1f), 0.011f, Rgb(30, 30, 30));
        });
        int tail = b.Tail(c + V(0.09f, -0.07f, 0.06f));
        b.Tube(tail, new[] { c + V(0.08f, -0.08f, 0.05f), c + V(0.13f, -0.1f, 0.07f), c + V(0.16f, -0.09f, 0.08f) }, 0.016f, 0.01f, green, blend: 0f);
        return b;
    }

    /// <summary>
    /// Flapple and its Gigantamax form (which the Gigantamax Appletun shares). Flapple is a little green dragon rising
    /// out of a red apple's husk, its wings of apple skin cream and edged red, red petals of apple skin round its head
    /// and fierce yellow eyes. The Gigantamax Flapple is a great apple oozing orange nectar from under a cap that drips,
    /// a wedge of cut apple at its side, a stalk on top with its little dragon's hood and Gigantamax's red clouds.
    /// </summary>
    private static PokeBuilder FlappleBuild(bool gmax)
    {
        var green = Rgb(112, 172, 82);
        var red = Rgb(198, 52, 62);
        var cream = Rgb(234, 208, 152);
        if (gmax)
        {
            var g = new PokeBuilder("Flapple-Gmax", 1f, BodyPlan.Floating, V(0, 0.36f, 0)) { Coat = Shell };
            var maroon = Rgb(150, 34, 56);
            var nectar = Rgb(240, 156, 56);
            var ac = V(0, 0.36f, 0);
            g.Ell(Body, ac, V(0.34f, 0.33f, 0.33f), maroon);
            // The cap of nectar over its top, drips running down from its edge
            g.Ell(Body, ac + V(0, 0.18f, 0), V(0.35f, 0.16f, 0.34f), nectar, mat: Glow, blend: 0.03f);
            g.PaintEll(Body, ac + V(0.06f, 0.3f, 0.12f), V(0.12f, 0.04f, 0.1f), Rgb(252, 214, 120));
            for (int i = 0; i < 11; i++)
            {
                float a = i * MathF.Tau / 11f, drop = 0.06f + 0.05f * ((i * 5) % 3);
                var root = ac + V(MathF.Sin(a) * 0.31f, 0.1f, MathF.Cos(a) * 0.3f);
                g.Limb(Body, root, root + V(MathF.Sin(a) * 0.02f, -drop, MathF.Cos(a) * 0.02f), 0.035f, 0.03f, nectar, Glow, 0.02f);
            }
            // A wedge of cut apple at the front, pale flesh rimmed green
            var wedge = ac + V(0.16f, -0.08f, 0.24f);
            g.Ell(Body, wedge, V(0.08f, 0.16f, 0.06f), Rgb(222, 222, 150), V(10f, 30f, -20f), blend: 0.02f);
            g.PaintEll(Body, wedge + V(0.06f, 0, 0.0f), V(0.04f, 0.17f, 0.07f), green, V(10f, 30f, -20f));
            // The stalk on top, with the little dragon's red hood and a leaf
            var stalk = ac + V(0, 0.33f, 0);
            g.Limb(Body, ac + V(0, 0.28f, 0), stalk + V(0, 0.12f, 0), 0.035f, 0.03f, Rgb(120, 82, 52));
            g.Ell(Body, stalk + V(0, 0.15f, 0), V(0.11f, 0.045f, 0.1f), red, blend: 0.02f);
            Frond(g, Body, stalk + V(0, 0.15f, 0), stalk + V(0.12f, 0.22f, -0.02f), 0.035f, Rgb(246, 210, 80), V(0, 0, 1f), 0.25f, Leaf);
            MaxClouds(g, Body, stalk + V(-0.05f, 0.16f, -0.02f), 0.035f, 0.1f, 0.16f, 0.9f, 1.4f);
            return g;
        }
        var b = new PokeBuilder("Flapple", 0.75f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        // The husk below, a red apple cut open at the top
        var hc = V(0, 0.2f, 0);
        b.Ell(Body, hc, V(0.11f, 0.09f, 0.11f), red, mat: Shell);
        b.Cut(Body, hc + V(0, 0.09f, 0), V(0.085f, 0.06f, 0.085f));
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f;
            b.Spike(Body, hc + V(MathF.Sin(a) * 0.09f, 0.03f, MathF.Cos(a) * 0.09f), hc + V(MathF.Sin(a) * 0.11f, 0.09f, MathF.Cos(a) * 0.11f), 0.03f, red, 0.4f, Shell, 0.01f);
        }
        // The dragon rising out of it
        var bc = V(0, 0.4f, 0.0f);
        b.Tube(Body, Smooth(3, hc + V(0, 0.0f, 0), hc + V(0.03f, 0.1f, 0.0f), bc + V(0, -0.06f, 0)), 0.03f, 0.04f, green, blend: 0.01f);
        b.Ell(Body, bc, V(0.05f, 0.08f, 0.045f), green);
        b.PaintEll(Body, bc + V(0, -0.01f, 0.04f), V(0.03f, 0.06f, 0.02f), Rgb(222, 214, 140));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.04f * s, 0.03f, 0.02f));
            b.Limb(arm, bc + V(0.04f * s, 0.03f, 0.02f), bc + V(0.07f * s, -0.02f, 0.06f), 0.014f, 0.012f, green);
            // The wings of apple skin, cream and edged red
            int wing = b.Wing(s, bc + V(0.025f * s, 0.04f, -0.02f));
            var root = bc + V(0.025f * s, 0.04f, -0.02f);
            Frond(b, wing, root, root + V(0.22f * s, 0.1f, -0.05f), 0.075f, cream, V(0, 0.2f, 1f), 0.15f);
            b.PaintEll(wing, root + V(0.2f * s, 0.09f, -0.045f), V(0.06f, 0.08f, 0.04f), red);
        });
        int head = b.Head(bc + V(0, 0.08f, 0.02f));
        var c = bc + V(0, 0.15f, 0.04f);
        var r = V(0.045f, 0.045f, 0.055f);
        b.Limb(head, bc + V(0, 0.06f, 0.01f), c, 0.03f, 0.03f, green);
        b.Ell(head, c, r, green);
        b.Ell(head, c + V(0, -0.012f, 0.045f), V(0.028f, 0.02f, 0.03f), green, blend: 0.012f);
        // The petals of apple skin round its head
        foreach (var (dx, up, back) in new[] { (-0.05f, 0.06f, 0.0f), (0.05f, 0.06f, 0.0f), (0f, 0.09f, -0.03f), (-0.03f, 0.03f, -0.06f), (0.03f, 0.03f, -0.06f) })
            Frond(b, head, c + V(dx * 0.4f, 0.02f, -0.02f), c + V(dx * 1.5f, 0.02f + up, -0.02f + back - 0.04f), 0.03f, red, V(0, 0.4f, 1f), 0.25f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.6f * s, 0.2f, 0.75f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(246, 214, 60), glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Flapple() => FlappleBuild(false);

    /// <summary>Appletun: an apple pie on legs, its back a dome of golden crust with a lattice on top and a leaf, its body and legs green beneath, and in front a round red head like an apple, sweet and sleepy.</summary>
    private static PokeBuilder Appletun()
    {
        var b = new PokeBuilder("Appletun", 0.85f, BodyPlan.Quadruped, V(0, 0.24f, 0)) { Coat = Fur };
        var green = Rgb(120, 172, 82);
        var crust = Rgb(236, 180, 96);
        var lattice = Rgb(250, 208, 130);
        var red = Rgb(196, 48, 62);
        BeastLegs(b, 0.13f, 0.14f, 0.14f, -0.15f, 0.05f, green, green);
        var bc = V(0, 0.22f, -0.02f);
        b.Ell(Body, bc, V(0.2f, 0.13f, 0.24f), green);
        // The crust: a dome over its back, raised strips laid across it both ways
        var cc = V(0, 0.32f, -0.04f);
        var cr = V(0.22f, 0.15f, 0.25f);
        b.Ell(Body, cc, cr, crust);
        foreach (float k in new[] { -0.55f, 0f, 0.55f })
        {
            var along = new Vector3[9];
            var across = new Vector3[9];
            for (int i = 0; i < 9; i++)
            {
                float t = (i / 8f - 0.5f) * 2.2f;
                along[i] = Out(cc, cr, default, V(k, MathF.Cos(t), MathF.Sin(t)));
                across[i] = Out(cc, cr, default, V(MathF.Sin(t), MathF.Cos(t), k));
            }
            b.Tube(Body, along, 0.014f, 0.014f, lattice, blend: 0.004f);
            b.Tube(Body, across, 0.014f, 0.014f, lattice, blend: 0.004f);
        }
        var crown = Out(cc, cr, default, V(0, 1f, 0));
        b.Limb(Body, crown, crown + V(0, 0.04f, 0), 0.01f, 0.008f, Rgb(120, 82, 52));
        Frond(b, Body, crown + V(0, 0.03f, 0), crown + V(0.06f, 0.07f, 0.0f), 0.025f, green, V(0, 0, 1f), 0.25f, Leaf);
        int head = b.Head(bc + V(0, -0.02f, 0.2f));
        var c = bc + V(0, -0.02f, 0.28f);
        var r = V(0.08f, 0.075f, 0.075f);
        b.Limb(head, bc + V(0, -0.01f, 0.18f), c, 0.06f, 0.055f, green);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, 0.06f, -0.01f), V(0.07f, 0.03f, 0.07f), PixelCanvas.Mix(red, Black, 0.25f));
        b.Mark(head, On(c, r, 0, c.Y - 0.03f), V(0, -0.3f, 1f), 0.016f, 0.009f, Rgb(110, 30, 40), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(60, 30, 30));
        });
        int tail = b.Tail(bc + V(0, -0.02f, -0.23f));
        b.Spike(tail, bc + V(0, -0.02f, -0.22f), bc + V(0, -0.06f, -0.32f), 0.04f, green);
        return b;
    }

    // ------------------------------------------------------------------ Silicobra line

    /// <summary>Silicobra: a little sand snake coiled on the ground, cream with an olive back, a sack of sand like a ring round its neck spotted dark, a pale head with nostrils on its snout and green eyes.</summary>
    private static PokeBuilder Silicobra()
    {
        var olive = Rgb(146, 138, 94);
        var dark = Rgb(92, 86, 58);
        var cream = Rgb(228, 220, 192);
        var coil = Spiral3(0.11f, 0.06f, 0.04f, 0.07f, 2.4f, 1.1f, 14);
        var points = new[] { V(0.12f, 0.03f, -0.13f), V(0.11f, 0.035f, -0.1f) }.Concat(coil).Concat(new[] { V(0, 0.12f, 0.05f), V(0, 0.19f, 0.06f) }).ToArray();
        var b = new PokeBuilder("Silicobra", 0.5f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int neck = Coils(b, points, t => 0.02f + 0.022f * MathF.Min(1f, t * 3f), cream);
        var ring = V(0, 0.19f, 0.06f);
        b.Torus(neck, ring, 0.055f, 0.042f, olive, mat: Fur, blend: 0.012f);
        foreach (float a in new[] { 0.3f, 1.5f, 2.7f, 3.9f, 5.1f })
            b.PaintEll(neck, ring + V(MathF.Sin(a) * 0.08f, 0.03f, MathF.Cos(a) * 0.08f), V(0.018f, 0.014f, 0.018f), dark, soft: 0.006f);
        int head = b.Head(ring + V(0, 0.03f, 0.0f), neck);
        var c = ring + V(0, 0.06f, 0.03f);
        var r = V(0.048f, 0.04f, 0.06f);
        b.Ell(head, c, r, cream);
        PokeBuilder.Both(s =>
        {
            b.Mark(head, On(c, r, 0.012f * s, c.Y - 0.005f) + V(0, 0, 0.0f), V(0.2f * s, 0.3f, 1f), 0.005f, 0.004f, Rgb(70, 60, 50));
            var at = Out(c, r, default, V(0.6f * s, 0.45f, 0.6f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(110, 180, 70));
        });
        return b;
    }

    /// <summary>
    /// Sandaconda and its Gigantamax form. Sandaconda is a great snake coiled on the ground, banded cream and brown, a
    /// swollen sack of sand round its neck in olive marked with dark zigzags, a pale snout like a skull's and green
    /// eyes. The Gigantamax Sandaconda is a whirlwind of sand, a twister of brown winding up from a point to a cloud of
    /// dust with Gigantamax's red clouds.
    /// </summary>
    private static PokeBuilder SandacondaBuild(bool gmax)
    {
        var brown = Rgb(150, 128, 92);
        var cream = Rgb(230, 220, 190);
        var olive = Rgb(140, 132, 90);
        var dark = Rgb(80, 74, 52);
        if (gmax)
        {
            var g = new PokeBuilder("Sandaconda-Gmax", 1f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Fur };
            const int Steps = 60;
            var twister = new Vector3[Steps + 1];
            for (int i = 0; i <= Steps; i++)
            {
                float t = i / (float)Steps, a = t * MathF.Tau * 4.5f, rad = 0.02f + 0.24f * t * t;
                twister[i] = V(MathF.Cos(a) * rad, 0.04f + 0.66f * t, MathF.Sin(a) * rad);
            }
            g.Tube(Body, twister, 0.035f, 0.075f, brown, blend: 0.03f);
            g.Limb(Body, V(0, 0.03f, 0), V(0, 0.5f, 0), 0.03f, 0.1f, PixelCanvas.Mix(brown, White, 0.15f), blend: 0.02f);
            for (int i = 4; i <= Steps; i += 5)
                g.PaintEll(Body, twister[i], V(0.07f, 0.02f, 0.07f), dark, soft: 0.008f);
            // The cloud of dust on top
            var top = V(0, 0.72f, 0);
            foreach (var (o, k) in new[] { (V(0, 0, 0), 0.13f), (V(0.15f, -0.02f, 0.02f), 0.09f), (V(-0.16f, -0.01f, -0.02f), 0.1f), (V(0.05f, 0.06f, -0.08f), 0.08f), (V(-0.05f, 0.03f, 0.1f), 0.08f), (V(0.24f, -0.04f, -0.04f), 0.06f) })
                g.Ell(Body, top + o, V(k * 1.2f, k * 0.8f, k), Rgb(176, 160, 132), blend: 0.025f);
            MaxClouds(g, Body, top + V(0.02f, 0.08f, 0), 0.035f, 0.1f, 0.16f, 0.9f, 0.8f);
            return g;
        }
        var coil = Spiral3(0.24f, 0.13f, 0.07f, 0.15f, 2.0f, 1.25f, 20);
        var points = new[] { V(0.3f, 0.04f, -0.24f), V(0.27f, 0.05f, -0.2f) }.Concat(coil).Concat(new[] { V(0, 0.28f, 0.07f), V(0, 0.4f, 0.08f) }).ToArray();
        var b = new PokeBuilder("Sandaconda", 0.9f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int neck = Coils(b, points, t => 0.03f + 0.05f * MathF.Min(1f, t * 2.5f), brown);
        // Cream bands across the body, painted on each stretch's own bone
        for (int i = 1; i < points.Length - 3; i += 2)
        {
            int bone = i < 2 ? Body : b.Model.Skeleton.Find("seg" + i / 2);
            var d = points[i + 1] - points[i];
            b.PaintEll(bone, points[i] + d * 0.5f, V(0.09f, 0.025f, 0.09f), cream, Euler(d));
        }
        // The sack of sand round its neck, olive, marked with dark zigzags
        var ring = V(0, 0.4f, 0.08f);
        b.Torus(neck, ring, 0.1f, 0.075f, olive, mat: Fur, blend: 0.015f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.3f;
            var at = ring + V(MathF.Sin(a) * 0.15f, 0.045f, MathF.Cos(a) * 0.15f);
            b.Mark(neck, at, Vector3.Normalize(V(MathF.Sin(a), 0.6f, MathF.Cos(a))), 0.035f, 0.012f, dark, MarkShape.Zigzag, -a / Degree);
        }
        int head = b.Head(ring + V(0, 0.04f, 0), neck);
        var c = ring + V(0, 0.1f, 0.05f);
        var r = V(0.075f, 0.06f, 0.095f);
        b.Ell(head, c, r, cream);
        PokeBuilder.Both(s =>
        {
            b.Mark(head, On(c, r, 0.02f * s, c.Y - 0.01f), V(0.2f * s, 0.2f, 1f), 0.008f, 0.006f, Rgb(70, 60, 50));
            var at = Out(c, r, default, V(0.65f * s, 0.45f, 0.55f));
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(110, 180, 70), glare: true);
        });
        return b;
    }

    private static PokeBuilder Sandaconda() => SandacondaBuild(false);

    // ------------------------------------------------------------------ Cramorant

    /// <summary>
    /// Cramorant and the forms it takes with its catch (<paramref name="prey"/>: 0 none, 1 an Arrokuda, 2 a Pikachu). A
    /// blue cormorant standing upright on webbed grey feet, a ruff of white down on its chest, a long neck to a head
    /// with a crest of blue feathers, a great green eye and a long yellow beak. Gulping, an Arrokuda sticks out of its
    /// beak; gorging, a Pikachu, ears, tail and all.
    /// </summary>
    private static PokeBuilder CramorantBuild(int prey)
    {
        var name = prey switch { 1 => "Cramorant-Gulping", 2 => "Cramorant-Gorging", _ => "Cramorant" };
        var b = new PokeBuilder(name, 0.7f, BodyPlan.Bird, V(0, 0.25f, 0)) { Coat = Fur };
        var blue = Rgb(58, 112, 204);
        var dark = Rgb(40, 62, 124);
        var white = Rgb(240, 238, 248);
        var yellow = Rgb(242, 200, 62);
        var grey = Rgb(76, 96, 108);
        BirdLegs(b, 0.05f, 0.1f, 0.0f, 0.015f, grey);
        var bc = V(0, 0.21f, 0);
        b.Ell(Body, bc, V(0.1f, 0.12f, 0.095f), blue, V(-10f, 0, 0));
        b.PaintEll(Body, bc + V(0, 0.0f, 0.06f), V(0.06f, 0.09f, 0.04f), white);
        FurTufts(b, Body, bc + V(0, 0.01f, 0.05f), V(0.06f, 0.08f, 0.05f), 14, 0.03f, 0.014f, white, 2f, -0.95f, 0.7f);
        PokeBuilder.Both(s => FoldedWing(b, s, bc + V(0.07f * s, 0.06f, 0.0f), bc + V(0.09f * s, -0.07f, -0.1f), 0.06f, blue, dark));
        int tail = b.Tail(bc + V(0, -0.06f, -0.07f));
        TailFan(b, tail, bc + V(0, -0.06f, -0.07f), 3, 30f, 0.08f, 0.02f, dark, blue, 0.6f);
        var neck = Smooth(3, bc + V(0, 0.09f, 0.03f), bc + V(0, 0.15f, 0.07f), bc + V(0, 0.2f, 0.03f), bc + V(0, 0.24f, 0.05f));
        b.Tube(Body, neck, 0.04f, 0.028f, blue, blend: 0f);
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.03f, 0.02f);
        var r = V(0.04f, 0.04f, 0.05f);
        b.Ell(head, c, r, blue);
        foreach (var (x, up, back) in new[] { (0f, 0.08f, 0.04f), (-0.012f, 0.06f, 0.07f), (0.012f, 0.06f, 0.07f) })
            Frond(b, head, c + V(x, 0.03f, -0.02f), c + V(x * 2f, 0.03f + up, -0.02f - back), 0.015f, dark, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.75f * s, 0.25f, 0.6f));
            b.Eye(head, at, Outward(c, r, at), 0.013f, sclera: true, white: Rgb(196, 226, 110), pupil: Rgb(30, 30, 34));
        });
        // The beak: shut, or held open round what it caught
        var root = c + V(0, -0.005f, 0.04f);
        if (prey == 0)
        {
            b.Spike(head, root, root + V(0, -0.03f, 0.17f), 0.022f, yellow, 0.7f, Shell, 0.008f);
            b.Ell(head, root + V(0, -0.02f, 0.05f), V(0.016f, 0.012f, 0.05f), PixelCanvas.Mix(yellow, White, 0.3f), mat: Fur, blend: 0.01f);
            return b;
        }
        b.Spike(head, root + V(0, 0.01f, 0), root + V(0, 0.05f, 0.16f), 0.022f, yellow, 0.7f, Shell, 0.008f);
        b.Spike(head, root + V(0, -0.015f, 0), root + V(0, -0.06f, 0.15f), 0.022f, yellow, 0.7f, Shell, 0.008f);
        var catchAt = root + V(0, -0.005f, 0.09f);
        if (prey == 1)
        {
            // An Arrokuda, head first, its snout poking out between the jaws and its tail fin at their root
            b.Ell(head, catchAt, V(0.03f, 0.035f, 0.07f), Rgb(150, 120, 90), blend: 0.008f);
            b.PaintEll(head, catchAt + V(0, -0.025f, 0), V(0.035f, 0.02f, 0.07f), Rgb(236, 220, 170));
            b.Spike(head, catchAt + V(0, 0, 0.05f), catchAt + V(0, -0.01f, 0.14f), 0.022f, Rgb(236, 220, 170), 0.7f, Shell, 0.006f);
            Frond(b, head, catchAt + V(0, 0.02f, -0.05f), catchAt + V(0, 0.08f, -0.07f), 0.022f, Rgb(120, 96, 72), V(1f, 0, 0), 0.25f);
        }
        else
        {
            // A Pikachu, its yellow head in the beak, ears tipped black and its tail like a bolt out to the side
            var pc = catchAt + V(0, 0.005f, 0.02f);
            b.Ell(head, pc, V(0.045f, 0.04f, 0.045f), Rgb(250, 214, 64), blend: 0.008f);
            PokeBuilder.Both(s =>
            {
                b.Spike(head, pc + V(0.025f * s, 0.03f, -0.01f), pc + V(0.07f * s, 0.09f, -0.02f), 0.014f, Rgb(250, 214, 64), 0.6f);
                b.PaintEll(head, pc + V(0.065f * s, 0.083f, -0.02f), V(0.014f, 0.016f, 0.014f), Rgb(40, 34, 30), soft: 0.005f);
            });
            b.Tube(head, new[] { pc + V(-0.03f, -0.02f, -0.01f), pc + V(-0.09f, -0.0f, -0.02f), pc + V(-0.1f, -0.05f, -0.02f), pc + V(-0.16f, -0.02f, -0.03f) }, 0.012f, 0.016f, Rgb(250, 214, 64), blend: 0f);
        }
        return b;
    }

    private static PokeBuilder Cramorant() => CramorantBuild(0);

    // ------------------------------------------------------------------ Arrokuda line

    /// <summary>Arrokuda: a slim barracuda, brown above and cream below, the bones of a fish drawn pale along its sides, a long pointed jaw jutting forward, an orange gill and brown fins.</summary>
    private static PokeBuilder Arrokuda()
    {
        var b = new PokeBuilder("Arrokuda", 0.5f, BodyPlan.Fish, V(0, 0.3f, 0)) { Coat = Scales }.Hover();
        var brown = Rgb(150, 120, 90);
        var cream = Rgb(236, 222, 172);
        var dark = Rgb(84, 64, 48);
        var bc = V(0, 0.3f, -0.02f);
        b.Ell(Body, bc, V(0.045f, 0.055f, 0.18f), brown);
        b.PaintEll(Body, bc + V(0, -0.04f, 0), V(0.05f, 0.03f, 0.18f), cream);
        // The fishbone along each side: a spine and ribs, pale
        b.PaintEll(Body, bc + V(0, 0.005f, -0.02f), V(0.05f, 0.006f, 0.13f), cream, soft: 0.004f);
        for (int i = 0; i < 5; i++)
            b.PaintEll(Body, bc + V(0, 0.005f, 0.07f - i * 0.04f), V(0.05f, 0.03f, 0.005f), cream, V(-25f, 0, 0), 0.004f);
        b.PaintTorus(Body, bc + V(0, 0, 0.12f), 0.045f, 0.008f, Rgb(232, 112, 62), V(90f, 0, 0), 1f, 1.2f);
        int dorsal = b.Part("dorsal", Body, bc + V(0, 0.05f, -0.06f), PokeRole.Fin);
        Blade(b, dorsal, bc + V(0, 0.045f, -0.04f), bc + V(0, 0.1f, -0.1f), 0.025f, brown, V(1f, 0, 0));
        int tail = b.Tail(bc + V(0, 0, -0.17f));
        b.Limb(tail, bc + V(0, 0, -0.16f), bc + V(0, 0, -0.22f), 0.025f, 0.015f, brown);
        Frond(b, tail, bc + V(0, 0, -0.21f), bc + V(0, 0.07f, -0.27f), 0.03f, dark, V(1f, 0, 0), 0.2f);
        Frond(b, tail, bc + V(0, 0, -0.21f), bc + V(0, -0.06f, -0.26f), 0.028f, dark, V(1f, 0, 0), 0.2f);
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, bc + V(0.04f * s, -0.02f, 0.06f), PokeRole.Fin, s, s);
            Frond(b, fin, bc + V(0.035f * s, -0.02f, 0.06f), bc + V(0.08f * s, -0.04f, 0.0f), 0.018f, brown, V(0, 1f, 0), 0.25f);
        });
        int head = b.Head(bc + V(0, 0, 0.15f));
        var c = bc + V(0, 0.005f, 0.16f);
        var r = V(0.04f, 0.045f, 0.06f);
        b.Ell(head, c, r, brown);
        b.Spike(head, c + V(0, -0.005f, 0.03f), c + V(0, -0.012f, 0.16f), 0.03f, cream, 0.8f);
        b.Spike(head, c + V(0, -0.025f, 0.02f), c + V(0, -0.03f, 0.13f), 0.018f, cream, 0.8f, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.8f * s, 0.3f, 0.5f));
            b.Eye(head, at, Outward(c, r, at), 0.013f, sclera: true, white: Rgb(244, 230, 150), pupil: Rgb(30, 26, 24));
        });
        return Lift(b);
    }

    /// <summary>Barraskewda: a long grey barracuda like a spear, its jaw a long sharp point with teeth, the bones of a fish drawn pale along it, and fins of flame red down its back, belly and tail.</summary>
    private static PokeBuilder Barraskewda()
    {
        var b = new PokeBuilder("Barraskewda", 0.85f, BodyPlan.Fish, V(0, 0.32f, 0)) { Coat = Scales }.Hover();
        var grey = Rgb(124, 112, 102);
        var cream = Rgb(236, 222, 180);
        var red = Rgb(228, 84, 54);
        var bc = V(0, 0.32f, -0.04f);
        b.Ell(Body, bc, V(0.06f, 0.07f, 0.26f), grey);
        b.PaintEll(Body, bc + V(0, -0.05f, 0), V(0.065f, 0.035f, 0.26f), cream);
        b.PaintEll(Body, bc + V(0, 0.01f, -0.03f), V(0.065f, 0.007f, 0.2f), cream, soft: 0.004f);
        for (int i = 0; i < 7; i++)
            b.PaintEll(Body, bc + V(0, 0.01f, 0.13f - i * 0.045f), V(0.065f, 0.035f, 0.005f), cream, V(-25f, 0, 0), 0.004f);
        // Fins of flame red: a row down its back and under its belly, and the tail
        for (int i = 0; i < 4; i++)
        {
            int fin = b.Part("dorsal" + i, Body, bc + V(0, 0.06f, 0.08f - i * 0.08f), PokeRole.Fin, i * 0.5f);
            var root = bc + V(0, 0.06f, 0.08f - i * 0.08f);
            Blade(b, fin, root, root + V(0, 0.07f, -0.07f), 0.03f, red, V(1f, 0, 0));
        }
        for (int i = 0; i < 3; i++)
        {
            var root = bc + V(0, -0.06f, 0.05f - i * 0.1f);
            Blade(b, Body, root, root + V(0, -0.06f, -0.06f), 0.025f, red, V(1f, 0, 0));
        }
        int tail = b.Tail(bc + V(0, 0, -0.25f));
        b.Limb(tail, bc + V(0, 0, -0.24f), bc + V(0, 0, -0.32f), 0.03f, 0.02f, grey);
        Blade(b, tail, bc + V(0, 0, -0.31f), bc + V(0, 0.1f, -0.4f), 0.04f, red, V(1f, 0, 0));
        Blade(b, tail, bc + V(0, 0, -0.31f), bc + V(0, -0.08f, -0.39f), 0.035f, red, V(1f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, bc + V(0.05f * s, -0.03f, 0.12f), PokeRole.Fin, s, s);
            Blade(b, fin, bc + V(0.05f * s, -0.03f, 0.12f), bc + V(0.12f * s, -0.07f, 0.04f), 0.025f, red, V(0, 1f, 0));
        });
        int head = b.Head(bc + V(0, 0, 0.22f));
        var c = bc + V(0, 0.005f, 0.24f);
        var r = V(0.05f, 0.055f, 0.07f);
        b.Ell(head, c, r, grey);
        b.Spike(head, c + V(0, 0.0f, 0.04f), c + V(0, -0.005f, 0.26f), 0.035f, PixelCanvas.Mix(grey, White, 0.25f), 0.8f);
        b.Spike(head, c + V(0, -0.03f, 0.03f), c + V(0, -0.035f, 0.18f), 0.02f, cream, 0.8f, blend: 0.008f);
        for (int i = 0; i < 4; i++)
            PokeBuilder.Both(s => b.Spike(head, c + V(0.012f * s, -0.018f, 0.07f + i * 0.03f), c + V(0.012f * s, -0.032f, 0.075f + i * 0.03f), 0.005f, White, mat: Shell, blend: 0.002f));
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.8f * s, 0.35f, 0.45f));
            b.Eye(head, at, Outward(c, r, at), 0.014f, sclera: true, white: Rgb(244, 230, 150), pupil: Rgb(30, 26, 24));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Toxel line

    /// <summary>Toxel: a purple baby lizard sitting up, its belly pale, a white crest running back from its brow, sullen eyes ringed dark and a stubby tail.</summary>
    private static PokeBuilder Toxel()
    {
        var b = new PokeBuilder("Toxel", 0.45f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Scales };
        var purple = Rgb(160, 112, 192);
        var pale = Rgb(220, 204, 232);
        var white = Rgb(246, 244, 250);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.06f, 0.02f));
            b.Ell(leg, V(0.055f * s, 0.03f, 0.05f), V(0.03f, 0.03f, 0.045f), purple);
        });
        b.Ell(Body, V(0, 0.11f, -0.01f), V(0.075f, 0.075f, 0.08f), purple);
        b.PaintEll(Body, V(0, 0.1f, 0.06f), V(0.05f, 0.055f, 0.03f), pale);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.13f, 0.03f));
            b.Limb(arm, V(0.06f * s, 0.13f, 0.03f), V(0.07f * s, 0.07f, 0.07f), 0.017f, 0.015f, purple);
        });
        int head = b.Head(V(0, 0.18f, 0.03f));
        var c = V(0, 0.23f, 0.04f);
        var r = V(0.085f, 0.07f, 0.075f);
        b.Ell(head, c, r, purple);
        // The crest: a white ridge from the brow back over the crown
        Frond(b, head, c + V(0, 0.04f, 0.05f), c + V(0, 0.09f, -0.06f), 0.03f, white, V(1f, 0, 0), 0.4f);
        b.Spike(head, c + V(0, 0.07f, 0.03f), c + V(0, 0.12f, 0.02f), 0.02f, white, 0.5f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.008f);
            b.PaintEll(head, at + V(0, -0.012f, 0), V(0.024f, 0.012f, 0.02f), Rgb(110, 70, 140), soft: 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(214, 90, 120), glare: true);
        });
        int tail = b.Tail(V(0, 0.07f, -0.08f));
        b.Spike(tail, V(0, 0.07f, -0.07f), V(0, 0.04f, -0.16f), 0.035f, purple);
        return b;
    }

    /// <summary>
    /// Toxtricity in its Amped or Low Key Form, and the Gigantamax form both share. Toxtricity is a lanky punk of a
    /// lizard, purple striped dark, rows of studs down its chest like a guitar's frets, a crest of spikes on its head
    /// and spikes down its arms: yellow and wild when Amped, pale blue and swept back when Low Key. The Gigantamax
    /// Toxtricity crouches like a beast, a tower of pink spines rising from its back like a guitar's neck, spikes of
    /// yellow and blue along its back and Gigantamax's red clouds over it.
    /// </summary>
    private static PokeBuilder ToxtricityBuild(bool lowKey, bool gmax)
    {
        var purple = lowKey ? Rgb(150, 124, 196) : Rgb(140, 98, 182);
        var dark = Rgb(76, 54, 108);
        var spike = lowKey ? Rgb(170, 222, 244) : Rgb(250, 220, 70);
        if (gmax)
        {
            var g = new PokeBuilder("Toxtricity-Amped-Gmax", 1f, BodyPlan.Quadruped, V(0, 0.3f, 0)) { Coat = Scales };
            var navy = Rgb(70, 50, 110);
            var pink = Rgb(236, 120, 190);
            var blueSpike = Rgb(170, 222, 244);
            BeastLegs(g, 0.14f, 0.22f, 0.16f, -0.16f, 0.06f, navy, Rgb(90, 64, 130));
            var gbc = V(0, 0.3f, -0.02f);
            g.Ell(Body, gbc, V(0.18f, 0.13f, 0.26f), navy);
            g.PaintEll(Body, gbc + V(0, -0.08f, 0.05f), V(0.15f, 0.06f, 0.2f), PixelCanvas.Mix(navy, pink, 0.35f));
            // The tower of pink spines rising from the back, like the neck of a guitar
            var tower = gbc + V(0, 0.1f, -0.08f);
            g.Spike(Body, tower, tower + V(0, 0.58f, -0.03f), 0.06f, pink, 0.7f, Glow);
            for (int i = 0; i < 4; i++)
                PokeBuilder.Both(s => g.Spike(Body, tower + V(0, 0.15f + i * 0.1f, 0), tower + V(0.1f * s, 0.2f + i * 0.1f, -0.02f), 0.02f, pink, 0.6f, Glow, 0.006f));
            for (int i = 0; i < 6; i++)
            {
                float z = 0.18f - i * 0.07f;
                PokeBuilder.Both(s => g.Spike(Body, gbc + V(0.08f * s, 0.08f, z), gbc + V(0.16f * s, 0.2f + 0.04f * (i % 2), z - 0.04f), 0.03f, i % 2 == 0 ? Rgb(250, 220, 70) : blueSpike, 0.6f));
            }
            int gh = g.Head(gbc + V(0, 0.04f, 0.24f));
            var gc = gbc + V(0, 0.06f, 0.34f);
            var gr = V(0.11f, 0.075f, 0.1f);
            g.Limb(gh, gbc + V(0, 0.03f, 0.2f), gc, 0.09f, 0.08f, navy);
            g.Ell(gh, gc, gr, navy);
            g.PaintEll(gh, gc + V(0, -0.035f, 0.06f), V(0.1f, 0.012f, 0.06f), pink, soft: 0.006f);
            for (int i = -1; i <= 1; i++)
                g.Spike(gh, gc + V(i * 0.04f, 0.05f, -0.02f), gc + V(i * 0.08f, 0.15f, -0.08f), 0.025f, Rgb(250, 220, 70), 0.6f);
            PokeBuilder.Both(s =>
            {
                var at = Out(gc, gr, default, V(0.6f * s, 0.35f, 0.7f));
                g.Mark(gh, at, Outward(gc, gr, at), 0.02f, 0.02f, Rgb(250, 220, 70), MarkShape.Star5);
                g.Eye(gh, at, Outward(gc, gr, at), 0.011f, Rgb(214, 60, 90), glare: true);
            });
            MaxClouds(g, Body, tower + V(0, 0.56f, -0.03f), 0.035f, 0.1f, 0.16f, 0.9f, 0.6f);
            return g;
        }
        var b = new PokeBuilder(lowKey ? "Toxtricity-Low-Key" : "Toxtricity", 0.95f, BodyPlan.Biped, V(0, 0.55f, 0)) { Coat = Scales };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.43f, 0));
            var knee = V(0.065f * s, 0.24f, 0.03f);
            b.Limb(leg, V(0.05f * s, 0.44f, 0), knee, 0.03f, 0.022f, purple);
            b.Limb(leg, knee, V(0.065f * s, 0.05f, 0.0f), 0.022f, 0.02f, purple);
            b.PaintEll(leg, knee + V(0, 0.07f, 0.0f), V(0.03f, 0.012f, 0.03f), dark, soft: 0.005f);
            b.Ell(leg, V(0.065f * s, 0.022f, 0.03f), V(0.028f, 0.022f, 0.05f), dark);
        });
        b.Ell(Body, V(0, 0.45f, 0), V(0.055f, 0.05f, 0.045f), dark);
        var bc = V(0, 0.56f, 0);
        b.Ell(Body, bc, V(0.065f, 0.11f, 0.05f), purple);
        // The frets down its chest: two rows of studs on a plate
        b.PaintEll(Body, bc + V(0, 0.01f, 0.04f), V(0.035f, 0.09f, 0.02f), spike);
        for (int i = 0; i < 3; i++)
            PokeBuilder.Both(s => b.Spike(Body, bc + V(0.016f * s, 0.06f - i * 0.04f, 0.045f), bc + V(0.022f * s, 0.06f - i * 0.04f, 0.07f), 0.009f, spike, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.055f * s, 0.08f, 0));
            var elbow = bc + V(0.11f * s, -0.02f, 0.0f);
            var hand = bc + V(0.1f * s, -0.12f, 0.05f);
            b.Limb(arm, bc + V(0.05f * s, 0.08f, 0), elbow, 0.02f, 0.017f, purple);
            b.Limb(arm, elbow, hand, 0.017f, 0.015f, purple);
            b.Spike(arm, elbow + V(0, 0.02f, -0.01f), elbow + V(0.04f * s, 0.06f, -0.04f), 0.014f, spike, 0.6f);
            Digits(b, arm, hand, V(0.2f * s, -1f, 0.3f), V(0.3f, 0, 0.2f), 0.035f, 0.007f, dark);
        });
        int head = b.Head(bc + V(0, 0.1f, 0.01f));
        var c = bc + V(0, 0.18f, 0.03f);
        var r = V(0.048f, 0.052f, 0.058f);
        b.Limb(head, bc + V(0, 0.09f, 0.0f), c, 0.028f, 0.028f, purple);
        b.Ell(head, c, r, purple);
        b.PaintEll(head, c + V(0, -0.025f, 0.04f), V(0.035f, 0.012f, 0.03f), dark, soft: 0.005f);
        // The crest: wild and upright when Amped, swept back when Low Key
        var crest = lowKey
            ? new[] { (V(0, 0.04f, 0.02f), V(0, 0.07f, -0.14f)), (V(-0.025f, 0.035f, 0.0f), V(-0.05f, 0.04f, -0.13f)), (V(0.025f, 0.035f, 0.0f), V(0.05f, 0.04f, -0.13f)) }
            : new[] { (V(0, 0.045f, 0.01f), V(0, 0.17f, -0.03f)), (V(-0.02f, 0.04f, 0.0f), V(-0.06f, 0.15f, -0.02f)), (V(0.02f, 0.04f, 0.0f), V(0.06f, 0.15f, -0.02f)), (V(-0.03f, 0.03f, -0.02f), V(-0.09f, 0.1f, -0.05f)), (V(0.03f, 0.03f, -0.02f), V(0.09f, 0.1f, -0.05f)) };
        foreach (var (from, to) in crest)
            b.Spike(head, c + from, c + to, 0.022f, spike, 0.6f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.6f * s, 0.25f, 0.75f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(214, 60, 90), glare: true);
        });
        int tail = b.Tail(V(0, 0.44f, -0.04f));
        b.Spike(tail, V(0, 0.44f, -0.03f), V(0, 0.36f, -0.13f), 0.03f, purple);
        return b;
    }

    private static PokeBuilder Toxtricity() => ToxtricityBuild(false, false);

    // ------------------------------------------------------------------ Sizzlipede line

    /// <summary>
    /// A centipede's body: segments from the tail along <paramref name="points"/> to the neck, each ringed with a darker
    /// band and a pair of little black legs, a bone for every two segments. Returns the last bone.
    /// </summary>
    private static int Centipede(PokeBuilder b, Vector3[] points, float radius, Color color, Color band, Color mark, float legLength)
    {
        int bone = Coils(b, points, t => radius * (0.7f + 0.3f * MathF.Min(1f, t * 3f)), color);
        for (int i = 0; i < points.Length - 1; i++)
        {
            int seg = i < 2 ? Body : b.Model.Skeleton.Find("seg" + i / 2);
            var d = Vector3.Normalize(points[i + 1] - points[i]);
            var side = Vector3.Normalize(Vector3.Cross(d, V(0, 1f, 0)) + V(0, 0.0001f, 0));
            float rad = radius * (0.7f + 0.3f * MathF.Min(1f, i / (float)(points.Length - 1) * 3f));
            b.PaintTorus(seg, points[i], rad * 1.02f, rad * 0.18f, band, Euler(d));
            b.PaintEll(seg, points[i] + (points[i + 1] - points[i]) * 0.5f + V(0, rad * 0.9f, 0), V(rad * 0.35f, rad * 0.3f, rad * 0.35f), mark, soft: 0.006f);
            if (i % 2 == 0 && points[i].Y < radius * 3f)
                PokeBuilder.Both(s =>
                {
                    var root = points[i] + side * s * rad * 0.8f;
                    b.Spike(seg, root, root + side * s * legLength + V(0, -rad, 0), rad * 0.22f, Rgb(40, 34, 40), mat: Shell, blend: 0.004f);
                });
        }
        return bone;
    }

    /// <summary>Sizzlipede: a little centipede of red-orange, each segment banded dark red and marked with a glowing yellow spot, black legs, and a head with orange fangs.</summary>
    private static PokeBuilder Sizzlipede()
    {
        var points = Smooth(2, V(0.08f, 0.035f, -0.22f), V(0.1f, 0.035f, -0.08f), V(0.0f, 0.04f, 0.04f), V(0, 0.08f, 0.13f));
        var b = new PokeBuilder("Sizzlipede", 0.5f, BodyPlan.Serpent, points[0]) { Coat = Shell };
        var red = Rgb(226, 92, 52);
        var band = Rgb(150, 44, 40);
        var yellow = Rgb(250, 200, 70);
        int neck = Centipede(b, points, 0.04f, red, band, yellow, 0.035f);
        int head = b.Head(points[^1], neck);
        var c = points[^1] + V(0, 0.02f, 0.035f);
        var r = V(0.05f, 0.045f, 0.045f);
        b.Ell(head, c, r, red);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, new[] { c + V(0.03f * s, -0.02f, 0.03f), c + V(0.05f * s, -0.03f, 0.07f), c + V(0.02f * s, -0.035f, 0.09f) }, 0.01f, 0.005f, Rgb(250, 160, 60), Shell, 0f);
            var at = Out(c, r, default, V(0.55f * s, 0.35f, 0.75f));
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(250, 220, 90), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Centiskorch and its Gigantamax form. Centiskorch is a long centipede of red-orange, the front of its body reared
    /// up tall, each segment banded dark and marked with a yellow ring, many black legs along the part on the ground,
    /// whiskers of flame curling from its head and flames at its tail's end. The Gigantamax Centiskorch is a great
    /// serpent of fire looping up into the air, wound with golden spirals, spikes along it and flames at its head.
    /// </summary>
    private static PokeBuilder CentiskorchBuild(bool gmax)
    {
        var red = Rgb(222, 84, 46);
        var band = Rgb(140, 40, 36);
        var yellow = Rgb(250, 196, 70);
        var flame = Rgb(250, 160, 50);
        Vector3[] points = gmax
            ? Smooth(3, V(0.3f, 0.06f, -0.2f), V(0.15f, 0.04f, 0.0f), V(-0.12f, 0.14f, 0.06f), V(-0.2f, 0.36f, -0.06f), V(0.0f, 0.5f, -0.12f), V(0.2f, 0.6f, -0.02f), V(0.14f, 0.8f, 0.08f), V(0.0f, 0.86f, 0.12f))
            : Smooth(3, V(0.22f, 0.05f, -0.3f), V(0.2f, 0.05f, -0.12f), V(0.05f, 0.05f, -0.02f), V(-0.02f, 0.12f, 0.06f), V(0, 0.3f, 0.06f), V(0, 0.48f, 0.04f), V(0, 0.6f, 0.08f));
        var b = new PokeBuilder(gmax ? "Centiskorch-Gmax" : "Centiskorch", gmax ? 1f : 0.95f, BodyPlan.Serpent, points[0]) { Coat = Shell };
        int neck = Centipede(b, points, gmax ? 0.07f : 0.055f, red, band, yellow, gmax ? 0.05f : 0.045f);
        if (gmax)
            for (int i = 1; i < points.Length - 1; i += 2)
            {
                int seg = i < 2 ? Body : b.Model.Skeleton.Find("seg" + i / 2);
                var d = Vector3.Normalize(points[i + 1] - points[i]);
                b.PaintTorus(seg, points[i], 0.045f, 0.012f, Rgb(252, 214, 90), Euler(d));
                b.Spike(seg, points[i] + V(0, 0.06f, 0), points[i] + V(0, 0.12f, 0) - d * 0.03f, 0.015f, Rgb(250, 214, 120), 0.6f, blend: 0.004f);
            }
        int tail = b.Tail(points[0]);
        foreach (var d in new[] { V(0.05f, 0.08f, -0.02f), V(0.0f, 0.1f, -0.05f), V(0.07f, 0.05f, -0.06f) })
            b.Spike(tail, points[0], points[0] + d, 0.025f, flame, 0.6f, Glow);
        int head = b.Head(points[^1], neck);
        var c = points[^1] + V(0, 0.03f, 0.05f);
        var r = gmax ? V(0.08f, 0.065f, 0.075f) : V(0.065f, 0.055f, 0.06f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, 0.03f, 0.03f), V(r.X * 0.9f, 0.02f, r.Z * 0.8f), band);
        PokeBuilder.Both(s =>
        {
            // Whiskers of flame curling out and up from each side of the head
            var root = c + V(r.X * 0.8f * s, 0.0f, 0.02f);
            b.Tube(head, Smooth(2, root, root + V(0.06f * s, 0.04f, 0.0f), root + V(0.1f * s, 0.12f, -0.02f), root + V(0.08f * s, 0.18f, -0.04f)), 0.02f, 0.01f, flame, Glow, 0f);
            b.Spike(head, root + V(0.1f * s, 0.12f, -0.02f), root + V(0.15f * s, 0.15f, -0.03f), 0.015f, Rgb(252, 214, 90), 0.6f, Glow);
            b.Tube(head, new[] { c + V(0.03f * s, -0.03f, r.Z * 0.7f), c + V(0.05f * s, -0.05f, r.Z * 1.2f), c + V(0.02f * s, -0.055f, r.Z * 1.4f) }, 0.012f, 0.006f, flame, Shell, 0f);
            var at = Out(c, r, default, V(0.55f * s, 0.35f, 0.75f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(250, 220, 90), glare: true);
        });
        if (gmax)
            MaxClouds(b, head, c + V(0, 0.05f, -0.05f), 0.03f, 0.08f, 0.14f, 0.9f, 0.3f);
        return b;
    }

    private static PokeBuilder Centiskorch() => CentiskorchBuild(false);

    // ------------------------------------------------------------------ Clobbopus line

    /// <summary>Clobbopus: a little octopus, its round head cream spotted orange, big eyes ringed blue, two arms ending in dark round fists like boxing gloves and short orange tentacles beneath.</summary>
    private static PokeBuilder Clobbopus()
    {
        var b = new PokeBuilder("Clobbopus", 0.45f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Fur };
        var cream = Rgb(242, 230, 220);
        var orange = Rgb(236, 120, 70);
        var navy = Rgb(48, 54, 108);
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.04f, -0.04f })
            {
                int leg = b.Leg(s, V(0.04f * s, 0.05f, z), z > 0);
                b.Tube(leg, new[] { V(0.035f * s, 0.06f, z), V(0.07f * s, 0.02f, z * 1.6f), V(0.1f * s, 0.015f, z * 2f) }, 0.022f, 0.012f, orange, blend: 0.006f);
            }
        });
        var c = V(0, 0.16f, 0);
        var r = V(0.1f, 0.1f, 0.09f);
        b.Ell(Body, c, r, cream);
        foreach (var dir in new[] { V(0, 1f, 0.1f), V(0.5f, 0.8f, -0.2f), V(-0.5f, 0.8f, -0.2f), V(0.8f, 0.4f, 0.1f), V(-0.8f, 0.4f, 0.1f), V(0.3f, 0.6f, -0.7f), V(-0.3f, 0.6f, -0.7f) })
            b.PaintEll(Body, Out(c, r, default, dir), V(0.02f, 0.02f, 0.02f), orange, soft: 0.006f);
        b.PaintEll(Body, c + V(0, -0.08f, 0), V(0.1f, 0.04f, 0.1f), orange);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.037f * s, c.Y - 0.01f);
            b.Mark(Body, at, Outward(c, r, at), 0.028f, 0.028f, Rgb(80, 140, 220), MarkShape.Ring);
            b.Eye(Body, at, Outward(c, r, at), 0.017f, Rgb(40, 60, 110));
            // The arms and their round fists
            int arm = b.Arm(s, c + V(0.08f * s, -0.04f, 0.02f));
            var fist = c + V(0.17f * s, -0.04f, 0.05f);
            b.Tube(arm, new[] { c + V(0.08f * s, -0.04f, 0.02f), c + V(0.12f * s, -0.02f, 0.03f), fist }, 0.02f, 0.018f, orange, blend: 0f);
            b.Ell(arm, fist + V(0.02f * s, 0, 0), V(0.035f, 0.035f, 0.035f), navy);
        });
        return b;
    }

    /// <summary>Grapploct: a great dark octopus standing like a wrestler, a yellow mask like a cross round its eyes, thick arms with yellow suckers on their fists and legs of curled tentacles.</summary>
    private static PokeBuilder Grapploct()
    {
        var b = new PokeBuilder("Grapploct", 0.9f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var navy = Rgb(50, 56, 112);
        var yellow = Rgb(242, 214, 70);
        var light = Rgb(78, 86, 150);
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.05f, -0.05f })
            {
                int leg = b.Leg(s, V(0.08f * s, 0.24f, z), z > 0);
                var path = new[] { V(0.07f * s, 0.26f, z), V(0.12f * s, 0.14f, z * 1.4f), V(0.15f * s, 0.04f, z * 1.8f), V(0.2f * s, 0.03f, z * 2.4f), V(0.22f * s, 0.07f, z * 2.6f) };
                b.Tube(leg, path, 0.045f, 0.02f, navy, blend: 0.006f);
                b.PaintEll(leg, path[3] + V(0, -0.02f, 0), V(0.02f, 0.015f, 0.02f), yellow, soft: 0.005f);
            }
        });
        var bc = V(0, 0.34f, 0);
        b.Ell(Body, bc, V(0.12f, 0.1f, 0.1f), navy);
        var c = V(0, 0.52f, 0.01f);
        var r = V(0.14f, 0.13f, 0.12f);
        b.Ell(Body, c, r, navy);
        // The mask: a yellow cross over the eyes
        b.PaintEll(Body, c + V(0, 0.0f, 0.1f), V(0.13f, 0.025f, 0.05f), yellow);
        b.PaintEll(Body, c + V(0, 0.01f, 0.1f), V(0.025f, 0.09f, 0.05f), yellow);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.05f * s, c.Y + 0.0f);
            b.Eye(Body, at, Outward(c, r, at), 0.016f, Rgb(140, 200, 240), glare: true);
            int arm = b.Arm(s, c + V(0.12f * s, -0.08f, 0.02f));
            var elbow = c + V(0.22f * s, -0.14f, 0.06f);
            var fist = c + V(0.2f * s, -0.28f, 0.12f);
            b.Limb(arm, c + V(0.11f * s, -0.08f, 0.02f), elbow, 0.06f, 0.05f, navy);
            b.Limb(arm, elbow, fist, 0.05f, 0.05f, navy);
            b.Ell(arm, fist, V(0.065f, 0.06f, 0.065f), navy);
            foreach (var o in new[] { V(0.0f, -0.03f, 0.055f), V(0.03f * s, 0.0f, 0.055f), V(-0.03f * s, -0.01f, 0.05f) })
                b.PaintEll(arm, fist + o, V(0.016f, 0.016f, 0.016f), yellow, soft: 0.005f);
            b.PaintEll(arm, elbow + V(0, -0.01f, 0.04f), V(0.03f, 0.03f, 0.03f), light);
        });
        return b;
    }

    // ------------------------------------------------------------------ Sinistea line

    private static readonly Color Porcelain = Rgb(214, 238, 232);
    private static readonly Color TeaPurple = Rgb(150, 100, 190);
    private static readonly Color TeaGold = Rgb(230, 196, 96);

    /// <summary>Sinistea: a ghost living in a teacup of pale porcelain, swirls painted round it and a gold rim, a purple handle and purple tea inside; its two eyes look out through the cup's side.</summary>
    private static PokeBuilder Sinistea()
    {
        var b = new PokeBuilder("Sinistea", 0.35f, BodyPlan.Floating, V(0, 0.12f, 0)) { Coat = Shell };
        // The cup: a round bowl cut off flat at the rim and hollowed, the tea filling it to the bottom of the hollow
        // (or the hollow under the tea would be a closed pocket), a gold rim and foot and a purple handle
        var c = V(0, 0.14f, 0);
        var r = V(0.105f, 0.09f, 0.105f);
        b.Ell(Body, c, r, Porcelain);
        b.CutBox(Body, V(0, 0.27f, 0), V(0.2f, 0.08f, 0.2f), Quaternion.Identity);
        b.Cut(Body, V(0, 0.18f, 0), V(0.085f, 0.075f, 0.085f));
        b.Ell(Body, V(0, 0.12f, 0), V(0.075f, 0.025f, 0.075f), TeaPurple, mat: Glow, blend: 0.01f);
        b.Torus(Body, V(0, 0.19f, 0), 0.087f, 0.008f, TeaGold, mat: Metal, blend: 0.004f);
        b.Ell(Body, V(0, 0.022f, 0), V(0.05f, 0.018f, 0.05f), TeaGold, mat: Metal, blend: 0.006f);
        b.Limb(Body, V(0, 0.03f, 0), V(0, 0.07f, 0), 0.025f, 0.03f, TeaGold, Metal, 0.01f);
        b.Torus(Body, V(0.115f, 0.14f, 0), 0.03f, 0.009f, TeaPurple, V(90f, 0, 0), mat: Shell, blend: 0.006f);
        foreach (float a in new[] { 2.2f, 3.4f, 4.2f, 5.6f })
        {
            var at = Out(c, r, default, V(MathF.Sin(a), -0.1f, MathF.Cos(a)));
            b.PaintTorus(Body, at, 0.018f, 0.003f, Rgb(120, 190, 190), Euler(Outward(c, r, at)));
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.032f * s, c.Y - 0.005f);
            b.Eye(Body, at, Outward(c, r, at), 0.016f, sclera: true, white: Rgb(240, 250, 248), pupil: TeaPurple);
        });
        return b;
    }

    /// <summary>Polteageist: a ghost living in a teapot of pale porcelain, swirls painted round it, gold at its foot and handle, a white spout, its lid lifted by the purple tea rising out of it with two eyes peering from under it, and tea dripping down its side.</summary>
    private static PokeBuilder Polteageist()
    {
        var b = new PokeBuilder("Polteageist", 0.55f, BodyPlan.Floating, V(0, 0.17f, 0)) { Coat = Shell };
        var white = Rgb(244, 246, 246);
        var c = V(0, 0.16f, 0);
        var r = V(0.13f, 0.12f, 0.13f);
        b.Ell(Body, c, r, Porcelain);
        b.Torus(Body, V(0, 0.05f, 0), 0.09f, 0.016f, TeaGold, mat: Metal, blend: 0.008f);
        b.Limb(Body, V(0, 0.045f, 0), V(0, 0.06f, 0), 0.045f, 0.06f, TeaGold, Metal, 0.01f);
        foreach (float a in new[] { 0.5f, 1.6f, 2.7f, 3.8f, 4.9f, 6.0f })
            b.PaintTorus(Body, Out(c, r, default, V(MathF.Sin(a), 0.05f, MathF.Cos(a))), 0.025f, 0.004f, Rgb(120, 190, 190), Euler(V(MathF.Sin(a), 0, MathF.Cos(a))));
        // The spout and the handle
        b.Tube(Body, Smooth(2, c + V(-0.1f, -0.03f, 0.03f), c + V(-0.17f, 0.0f, 0.05f), c + V(-0.22f, 0.07f, 0.06f)), 0.03f, 0.018f, white, blend: 0.01f);
        b.Torus(Body, c + V(0.14f, 0.0f, -0.01f), 0.05f, 0.012f, TeaGold, V(90f, 0, 0), mat: Metal, blend: 0.008f);
        // The tea: a purple ghost rising out of the pot's mouth, its lid raised on it, its eyes under the lid, and a drip down its side
        var gc = c + V(0, 0.13f, 0.0f);
        var gr = V(0.075f, 0.06f, 0.075f);
        b.Ell(Body, gc, gr, TeaPurple, mat: Glow, blend: 0.02f);
        b.Ell(Body, gc + V(0, 0.055f, -0.01f), V(0.08f, 0.02f, 0.08f), Porcelain, V(-10f, 0, 0), blend: 0.008f);
        b.Ell(Body, gc + V(0, 0.085f, -0.012f), V(0.02f, 0.02f, 0.02f), TeaGold, mat: Metal, blend: 0.008f);
        b.Tube(Body, new[] { c + V(0.06f, 0.1f, 0.08f), c + V(0.09f, 0.05f, 0.1f), c + V(0.1f, -0.02f, 0.09f) }, 0.016f, 0.02f, TeaPurple, Glow, 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = Out(gc, gr, default, V(0.4f * s, -0.05f, 0.9f));
            b.Eye(Body, at, Outward(gc, gr, at), 0.016f, sclera: true, white: Rgb(240, 250, 248), pupil: Rgb(60, 30, 80));
        });
        return b;
    }
}
