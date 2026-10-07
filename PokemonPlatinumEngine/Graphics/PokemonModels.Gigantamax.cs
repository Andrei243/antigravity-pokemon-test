using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The Gigantamax forms of the hand-built species (after plan 03 · D11), in National Pokédex order. Each is our own
// sculpt after the design, and each carries the red clouds of Gigantamax energy over it.
internal static partial class PokemonModels
{
    private static readonly Color MaxRed = Rgb(214, 44, 86);

    /// <summary>
    /// The red clouds of Gigantamax energy: puffs strung on a wisp that rises from <paramref name="from"/> in a spiral
    /// <paramref name="turns"/> times round, <paramref name="spread"/> wide at the top, so they hang together.
    /// </summary>
    private static void MaxClouds(PokeBuilder b, int bone, Vector3 from, float puff, float spread, float rise, float turns, float phase = 0f)
    {
        const int Steps = 24;
        var path = new Vector3[Steps + 1];
        for (int i = 0; i <= Steps; i++)
        {
            float t = i / (float)Steps, a = phase + t * MathF.Tau * turns;
            path[i] = from + V(MathF.Cos(a) * spread * t, rise * t, MathF.Sin(a) * spread * t);
        }
        b.Tube(bone, path, puff * 0.45f, puff * 0.4f, MaxRed, Glow, 0f);
        for (int i = 1; i <= Steps; i += 2)
        {
            float size = puff * (1f + 0.35f * MathF.Sin(i * 1.7f));
            b.Ell(bone, path[i], V(size, size * 0.85f, size), MaxRed, mat: Glow, blend: 0.025f);
        }
    }

    // ------------------------------------------------------------------ Gigantamax Venusaur

    private static PokeBuilder VenusaurGmax()
    {
        var b = new PokeBuilder("Venusaur-Gmax", 0.95f, BodyPlan.Quadruped, V(0, 0.38f, -0.04f)) { Coat = Scales };
        var teal = Rgb(88, 162, 168);
        var maroon = Rgb(150, 42, 72);
        var rim = Rgb(236, 128, 156);
        var vine = Rgb(52, 112, 66);
        VenusaurBody(b, teal, Rgb(50, 104, 110), 1.6f);
        // Its flower has grown over all of it: a dome of great dark red petals edged in pink, hanging nearly to the
        // ground, the face peering out under the front; leaves under its rim and two long vines whipping out
        b.Limb(Body, V(0, 0.52f, -0.08f), V(0, 0.68f, -0.08f), 0.14f, 0.12f, Rgb(98, 84, 58), Shell);
        int flower = b.Part("flower", Body, V(0, 0.68f, -0.08f), PokeRole.Leaf);
        var center = V(0, 1.02f, -0.08f);
        b.Limb(flower, V(0, 0.68f, -0.08f), center - V(0, 0.04f, 0), 0.11f, 0.08f, Rgb(98, 84, 58), Shell);
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f + MathF.PI / 7f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            float front = MathF.Max(0f, dir.Z);
            var p0 = center + dir * 0.06f;
            var p1 = center + dir * 0.42f + V(0, -0.04f, 0);
            var p2 = V(0, 0.74f + 0.06f * front, -0.08f) + dir * 0.72f;
            var p3 = V(0, 0.42f + 0.2f * front * front, -0.08f) + dir * 0.82f;
            Frond(b, flower, p0, p1, 0.24f, maroon, V(0, 1f, 0) + dir * 0.2f, 0.12f, Leaf, 0.03f);
            Frond(b, flower, Vector3.Lerp(p0, p1, 0.8f), p2, 0.28f, maroon, dir + V(0, 1f, 0), 0.1f, Leaf, 0.03f);
            Frond(b, flower, Vector3.Lerp(p1, p2, 0.75f), p3, 0.26f, maroon, dir + V(0, 0.3f, 0), 0.11f, Leaf, 0.03f);
            b.PaintEll(flower, Vector3.Lerp(p2, p3, 0.85f), V(0.3f, 0.1f, 0.3f), rim);
            PalmLeaf(b, flower, V(0, 0.66f, -0.08f) + dir * 0.1f, V(0, 0.62f, -0.08f) + dir * 0.4f, V(0, 0.44f, -0.08f) + dir * 0.62f, 0.12f, Rgb(64, 140, 80), V(0, 1f, 0), 5);
        }
        // A pink heart to the flower at the dome's top, the red clouds of Gigantamax energy pouring up from it
        b.Ell(flower, center + V(0, 0.02f, 0), V(0.12f, 0.05f, 0.12f), rim, mat: Leaf, blend: 0.02f);
        MaxClouds(b, flower, center + V(0, 0.04f, 0), 0.085f, 0.3f, 0.42f, 1.1f, 0.5f);
        // Two vines: one curling up beside its face, one trailing out behind
        int whip = b.Part("vine", Body, V(-0.3f, 0.5f, 0.1f), PokeRole.Tail, 0.6f);
        b.Tube(whip, Smooth(4, V(-0.3f, 0.5f, 0.1f), V(-0.62f, 0.42f, 0.36f), V(-0.86f, 0.62f, 0.42f), V(-0.88f, 0.86f, 0.26f), V(-0.76f, 0.9f, 0.2f)), 0.035f, 0.02f, vine, Leaf, 0f);
        int tail = b.Tail(V(0.26f, 0.4f, -0.34f));
        b.Tube(tail, Smooth(4, V(0.26f, 0.4f, -0.34f), V(0.56f, 0.24f, -0.64f), V(0.9f, 0.1f, -0.7f), V(1.1f, 0.06f, -0.5f)), 0.035f, 0.018f, vine, Leaf, 0f);
        return b;
    }

    // ------------------------------------------------------------------ Gigantamax Charizard

    private static PokeBuilder CharizardGmax()
    {
        var b = new PokeBuilder("Charizard-Gmax", 0.95f, BodyPlan.Biped, V(0, 0.52f, 0)) { Coat = Scales };
        var orange = Rgb(242, 140, 64);
        var fire = Rgb(244, 102, 40);
        var heart = Rgb(255, 226, 110);
        var gold = Rgb(250, 206, 64);
        // A heavier build, the pale belly marked with golden diamonds
        var (head, tip) = CharizardBody(b, orange, Rgb(250, 230, 176), Rgb(40, 120, 140), 1.25f, 1.2f);
        foreach (var (x, y) in new[] { (-0.07f, 0.4f), (0.0f, 0.36f), (0.07f, 0.4f), (-0.035f, 0.46f), (0.035f, 0.46f), (0f, 0.53f) })
            b.Mark(Body, On(V(0, 0.52f, 0.0125f), V(0.225f, 0.24f, 0.1875f), x, y), V(x * 2f, 0, 1f), 0.024f, 0.03f, gold, MarkShape.Diamond);
        // Its wings are fire: tongues of flame fanned from its shoulders
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.12f * s, 0.7f, -0.12f));
            var shoulder = V(0.12f * s, 0.7f, -0.13f);
            FlameTongue(b, wing, shoulder, V(0.36f * s, 0.88f, -0.17f), 0.085f, fire, heart);
            foreach (var (x, y, r) in new[] { (0.34f, 1.12f, 0.06f), (0.54f, 1.08f, 0.068f), (0.7f, 0.94f, 0.07f), (0.74f, 0.74f, 0.066f), (0.64f, 0.56f, 0.058f) })
            {
                var tip = V(x * s, y, -0.2f);
                FlameTongue(b, wing, Vector3.Lerp(shoulder, tip, 0.3f), tip, r, fire, heart);
            }
        });
        // A crown of fire round its horns, red clouds of Gigantamax energy at its claws, and a great flame on its tail
        foreach (var (x, z, h) in new[] { (-0.07f, -0.02f, 0.16f), (0.07f, -0.02f, 0.16f), (0f, 0.01f, 0.2f), (-0.04f, -0.08f, 0.14f), (0.04f, -0.08f, 0.14f) })
            FlameTongue(b, head, V(x, 0.99f, z), V(x * 1.6f, 0.99f + h, z - 0.08f), 0.03f, fire, heart);
        PokeBuilder.Both(s =>
        {
            int arm = b.Model.Skeleton.Find(s < 0 ? "armL" : "armR");
            MaxClouds(b, arm, V(0.32f * s, 0.5f, 0.17f), 0.026f, 0.05f, 0.08f, 0.8f, s);
        });
        int flame = b.Part("flame", b.Model.Skeleton.Find("tail"), tip, PokeRole.Flame);
        Flame(b, flame, tip, 1.25f);
        return b;
    }

    // ------------------------------------------------------------------ Gigantamax Blastoise

    private static PokeBuilder BlastoiseGmax()
    {
        var b = new PokeBuilder("Blastoise-Gmax", 0.95f, BodyPlan.Biped, V(0, 0.46f, 0)) { Coat = Scales };
        var blue = Rgb(62, 146, 196);
        var hull = Rgb(72, 40, 56);
        var deck = Rgb(118, 56, 80);
        var trim = Rgb(236, 128, 160);
        var gun = Rgb(70, 72, 84);
        BlastoiseBody(b, blue, Rgb(228, 222, 196), Rgb(150, 140, 120));
        // White marks down its legs and arms
        PokeBuilder.Both(s =>
        {
            b.Mark(b.Model.Skeleton.Find(s < 0 ? "legL" : "legR"), V(0.21f * s, 0.12f, 0.1f), V(0.4f * s, 0, 1f), 0.02f, 0.026f, White, MarkShape.Triangle);
            b.Mark(b.Model.Skeleton.Find(s < 0 ? "armL" : "armR"), V(0.3f * s, 0.5f, 0.15f), V(0.4f * s, 0, 1f), 0.018f, 0.022f, White, MarkShape.Triangle);
        });
        // Its shell has become a warship: a dark hull over its back edged in pink, cannons bristling from its deck
        b.Box(Body, V(0, 0.62f, -0.16f), V(0.28f, 0.2f, 0.26f), 0.1f, hull, V(-12f, 0, 0), Shell, 0.03f);
        b.Box(Body, V(0, 0.82f, -0.2f), V(0.2f, 0.07f, 0.18f), 0.05f, deck, V(-12f, 0, 0), Shell, 0.02f);
        b.Box(Body, V(0, 0.44f, -0.2f), V(0.3f, 0.022f, 0.28f), 0.02f, trim, V(-12f, 0, 0), Shell, 0.01f);
        foreach (var (at, to) in new[]
        {
            (V(-0.1f, 0.88f, -0.12f), V(-0.12f, 0.98f, 0.12f)), (V(0.1f, 0.88f, -0.12f), V(0.12f, 0.98f, 0.12f)),
            (V(-0.14f, 0.86f, -0.3f), V(-0.32f, 1.0f, -0.36f)), (V(0.14f, 0.86f, -0.3f), V(0.32f, 1.0f, -0.36f)),
            (V(-0.22f, 0.74f, -0.06f), V(-0.4f, 0.8f, 0.06f)), (V(0.22f, 0.74f, -0.06f), V(0.4f, 0.8f, 0.06f)),
            (V(0f, 0.9f, -0.26f), V(0f, 1.08f, -0.2f))
        })
            Cannon(b, Body, at, to, 0.036f, gun);
        MaxClouds(b, Body, V(0.04f, 1.04f, -0.24f), 0.07f, 0.22f, 0.32f, 1.1f, 0.4f);
        int tail = b.Tail(V(0, 0.28f, -0.14f));
        b.Spike(tail, V(0, 0.26f, -0.16f), V(0, 0.18f, -0.34f), 0.05f, blue);
        return b;
    }

    // ------------------------------------------------------------------ Gigantamax Butterfree

    private static PokeBuilder ButterfreeGmax()
    {
        var b = new PokeBuilder("Butterfree-Gmax", 0.95f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var mint = Rgb(204, 244, 224);
        var pink = Rgb(242, 150, 182);
        // Its wings have grown huge and shine: pale green rimmed in pink, dotted with white scales
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.04f * s, 0.46f, -0.03f));
            FlatWing(b, w, V(0.34f * s, 0.66f, -0.035f), 0.34f, 0.22f, 26f * s, mint, pink, 0.03f);
            FlatWing(b, w, V(0.22f * s, 0.3f, -0.04f), 0.2f, 0.14f, -44f * s, mint, pink, 0.026f);
            foreach (var (x, y) in new[] { (0.3f, 0.7f), (0.44f, 0.76f), (0.42f, 0.6f), (0.2f, 0.58f), (0.2f, 0.28f), (0.28f, 0.2f) })
                b.Mark(w, V(x * s, y, -0.02f), V(0, 0, 1f), 0.026f, 0.026f, White);
        });
        int head = ButterfreeBody(b, Rgb(86, 78, 142));
        MaxClouds(b, head, V(0, 0.62f, 0.0f), 0.026f, 0.06f, 0.1f, 0.9f, 0.6f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Gigantamax Pikachu

    private static PokeBuilder PikachuGmax()
    {
        var b = new PokeBuilder("Pikachu-Gmax", 0.9f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var yellow = Rgb(250, 212, 52);
        var brown = Rgb(150, 92, 44);
        var black = Rgb(40, 30, 30);
        var spark = Rgb(255, 248, 190);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.1f, 0.04f));
            b.Ell(leg, V(0.13f * s, 0.04f, 0.1f), V(0.07f, 0.04f, 0.09f), yellow);
        });
        // A round, heavy body with its head sunk into it, brown stripes across the back
        b.Ell(Body, V(0, 0.3f, 0), V(0.28f, 0.28f, 0.25f), yellow);
        foreach (float y in new[] { 0.42f, 0.33f })
            b.PaintEll(Body, V(0, y, -0.2f), V(0.18f, 0.018f, 0.1f), brown, soft: 0.012f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.2f * s, 0.36f, 0.14f));
            b.Ell(arm, V(0.18f * s, 0.32f, 0.22f), V(0.05f, 0.05f, 0.05f), yellow);
        });
        // A great tail of lightning rising high behind it, glowing
        int tail = b.Tail(V(0, 0.3f, -0.2f));
        Lightning(b, tail, new[] { V(0.02f, 0.3f, -0.22f), V(0.1f, 0.5f, -0.3f), V(0.02f, 0.56f, -0.3f), V(0.14f, 0.82f, -0.36f), V(0.04f, 0.88f, -0.36f), V(0.18f, 1.26f, -0.42f) },
            new[] { 0.04f, 0.045f, 0.055f, 0.06f, 0.09f }, 0.022f, spark, V(1f, 0, 0), mat: Glow);

        int head = b.Head(V(0, 0.46f, 0.04f));
        b.Ell(head, V(0, 0.54f, 0.08f), V(0.17f, 0.13f, 0.15f), yellow);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.62f, 0.04f));
            MouseEar(b, ear, V(0.08f * s, 0.61f, 0.04f), V(0.2f * s, 0.84f, 0.0f), 0.055f, yellow, black);
        });
        PokeBuilder.Both(s => b.Mark(head, V(0.115f * s, 0.51f, 0.17f), V(0.75f * s, -0.05f, 0.65f), 0.035f, 0.035f, Rgb(232, 52, 52)));
        b.Mark(head, V(0, 0.54f, 0.228f), V(0, 0, 1f), 0.012f, 0.009f, black);
        b.Mark(head, V(0, 0.515f, 0.224f), V(0, -0.2f, 1f), 0.028f, 0.009f, Rgb(90, 36, 30), MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(head, V(0.062f * s, 0.565f, 0.208f), V(0.4f * s, 0.05f, 1f), 0.032f, closed: true));
        MaxClouds(b, head, V(-0.08f, 0.66f, 0.04f), 0.045f, 0.2f, 0.36f, 1.1f, 1f);
        return b;
    }

    // ------------------------------------------------------------------ Gigantamax Meowth

    private static PokeBuilder MeowthGmax()
    {
        var b = new PokeBuilder("Meowth-Gmax", 0.95f, BodyPlan.Biped, V(0, 0.62f, 0)) { Coat = Fur };
        var brown = Rgb(170, 116, 78);
        var cream = Rgb(248, 236, 198);
        var paw = Rgb(150, 96, 62);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.14f, 0));
            b.Limb(leg, V(0.05f * s, 0.15f, 0), V(0.065f * s, 0.045f, 0.015f), 0.042f, 0.032f, cream);
            b.Ell(leg, V(0.068f * s, 0.027f, 0.042f), V(0.043f, 0.027f, 0.062f), paw);
        });
        // Its body stretched up immeasurably tall, pale in front and brown behind, ringed by clouds below its head
        b.Limb(Body, V(0, 0.15f, -0.01f), V(0, 1.18f, 0), 0.09f, 0.07f, brown);
        b.PaintEll(Body, V(0, 0.66f, 0.07f), V(0.08f, 0.58f, 0.06f), cream);
        MaxClouds(b, Body, V(0, 1.08f, 0), 0.062f, 0.17f, 0.1f, 1.6f);
        int tail = b.Tail(V(0, 0.2f, -0.08f));
        CurledTail(b, tail, V(0, 0.2f, -0.08f), Vector3.Normalize(V(0.4f, 0.1f, -1f)), 0.2f, 0.022f, brown, 0.05f);
        // Arms raised high on either side of its head
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 1.12f, 0.01f));
            var elbow = V(0.16f * s, 1.19f, 0.03f);
            var hand = V(0.17f * s, 1.39f, 0.04f);
            b.Limb(arm, V(0.05f * s, 1.12f, 0.01f), elbow, 0.032f, 0.028f, brown);
            b.Limb(arm, elbow, hand, 0.028f, 0.026f, brown);
            b.Ell(arm, hand + V(0, 0.012f, 0), V(0.038f, 0.034f, 0.034f), paw);
        });
        int head = b.Head(V(0, 1.2f, 0.01f));
        var c = V(0, 1.31f, 0.02f);
        var r = V(0.12f, 0.1f, 0.095f);
        PokeBuilder.Both(s =>
        {
            int e = b.Ear(head, s, V(0.07f * s, 1.37f, 0));
            CatEar(b, e, V(0.07f * s, 1.36f, 0), V(0.125f * s, 1.49f, -0.02f), 0.05f, Rgb(54, 46, 52), Rgb(124, 74, 52));
        });
        b.Ell(head, c, r, brown);
        PokeBuilder.Both(s => b.Ell(head, V(0.06f * s, 1.275f, 0.04f), V(0.066f, 0.052f, 0.06f), brown, blend: 0.03f));
        // The coin on its brow shines with Gigantamax energy
        Coin(b, head, On(c, r, 0, 1.385f), 44f, 0.04f, 0.05f, Rgb(255, 222, 96), Rgb(230, 160, 50), 0, Glow);
        var mouth = On(c, r, 0, 1.255f);
        Grin(b, head, mouth, V(0.032f, 0.022f, 0.024f), Rgb(150, 58, 72));
        PokeBuilder.Both(s => b.Spike(head, mouth + V(0.015f * s, 0.016f, -0.003f), mouth + V(0.014f * s, 0.0f, 0.003f), 0.006f, White, mat: Shell, blend: 0.003f));
        Whiskers(b, head, V(0.085f, 1.27f, 0.075f), 0.15f, Rgb(244, 244, 242), 0.009f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 1.32f), V(0.4f * s, 0.05f, 1f), 0.032f, sclera: true, white: Rgb(250, 226, 96), pupil: Black, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Gigantamax Machamp

    private static PokeBuilder MachampGmax()
    {
        var b = new PokeBuilder("Machamp-Gmax", 0.95f, BodyPlan.Biped, V(0, 0.56f, 0)) { Coat = Fur };
        var gray = Rgb(86, 84, 100);
        var pale = Rgb(178, 182, 198);
        var black = Rgb(32, 30, 38);
        var lava = Rgb(250, 150, 40);
        var crack = Rgb(80, 30, 20);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.4f, 0));
            b.Limb(leg, V(0.11f * s, 0.4f, 0), V(0.16f * s, 0.2f, 0.04f), 0.09f, 0.07f, black);
            b.Limb(leg, V(0.16f * s, 0.2f, 0.04f), V(0.15f * s, 0.05f, 0.0f), 0.065f, 0.055f, black);
            b.Ell(leg, V(0.15f * s, 0.03f, 0.05f), V(0.065f, 0.03f, 0.095f), pale);
        });
        // Dark muscle, a pale chest, a black belt with a gold buckle and a ring of red cloud round its waist
        b.Ell(Body, V(0, 0.56f, 0), V(0.19f, 0.22f, 0.15f), gray);
        PokeBuilder.Both(s => b.Ell(Body, V(0.075f * s, 0.66f, 0.07f), V(0.095f, 0.075f, 0.075f), gray));
        b.PaintEll(Body, V(0, 0.5f, 0.1f), V(0.12f, 0.06f, 0.06f), pale);
        b.PaintEll(Body, V(0, 0.4f, 0), V(0.2f, 0.08f, 0.16f), black);
        Belt(b, V(0, 0.46f, 0), 0.185f, 0.145f);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            b.Ell(Body, V(MathF.Sin(a) * 0.21f, 0.47f + 0.015f * MathF.Sin(a * 3f), MathF.Cos(a) * 0.17f), V(0.045f, 0.035f, 0.045f), MaxRed, mat: Glow, blend: 0.015f);
        }
        // Four arms ending in fists of molten rock, cracked and glowing
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.18f * s, 0.72f, 0));
            b.Ell(arm, V(0.2f * s, 0.74f, 0), V(0.1f, 0.09f, 0.09f), gray);
            b.Limb(arm, V(0.22f * s, 0.74f, 0.0f), V(0.36f * s, 0.8f, 0.02f), 0.075f, 0.065f, gray);
            b.Limb(arm, V(0.36f * s, 0.8f, 0.02f), V(0.38f * s, 0.94f, 0.05f), 0.07f, 0.075f, lava, Glow);
            b.Ell(arm, V(0.38f * s, 1.0f, 0.06f), V(0.09f, 0.085f, 0.085f), lava, mat: Glow);
            b.Mark(arm, V(0.38f * s, 1.0f, 0.145f), V(0.2f * s, 0, 1f), 0.045f, 0.007f, crack, MarkShape.Bar, 35f);
            b.Mark(arm, V(0.385f * s, 0.99f, 0.146f), V(0.2f * s, 0, 1f), 0.03f, 0.006f, crack, MarkShape.Bar, -50f);
            int lower = b.Part(s < 0 ? "lowArmL" : "lowArmR", Body, V(0.17f * s, 0.6f, 0.02f), PokeRole.Arm, s + 1.4f, s);
            b.Limb(lower, V(0.17f * s, 0.6f, 0.02f), V(0.3f * s, 0.48f, 0.06f), 0.065f, 0.058f, gray);
            b.Limb(lower, V(0.3f * s, 0.48f, 0.06f), V(0.36f * s, 0.36f, 0.12f), 0.06f, 0.065f, lava, Glow);
            b.Ell(lower, V(0.38f * s, 0.3f, 0.14f), V(0.08f, 0.075f, 0.075f), lava, mat: Glow);
            b.Mark(lower, V(0.38f * s, 0.3f, 0.215f), V(0.2f * s, 0, 1f), 0.04f, 0.007f, crack, MarkShape.Bar, -30f);
            b.Mark(lower, V(0.385f * s, 0.29f, 0.216f), V(0.2f * s, 0, 1f), 0.026f, 0.006f, crack, MarkShape.Bar, 55f);
        });

        int head = b.Head(V(0, 0.77f, 0));
        b.Ell(head, V(0, 0.87f, 0.0f), V(0.11f, 0.11f, 0.11f), gray);
        b.Ell(head, V(0, 0.82f, 0.08f), V(0.08f, 0.055f, 0.06f), black);
        foreach (float x in new[] { -0.04f, 0f, 0.04f })
            b.Ell(head, V(x, 0.97f, -0.01f), V(0.018f, 0.06f, 0.08f), black, V(-20f, 0, 0), Shell, 0.012f);
        PokeBuilder.Both(s => b.Eye(head, V(0.048f * s, 0.89f, 0.1f), V(0.4f * s, 0.05f, 1f), 0.024f, sclera: true, white: Rgb(250, 220, 70), pupil: Rgb(250, 220, 70), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Gigantamax Gengar

    private static PokeBuilder GengarGmax()
    {
        var b = new PokeBuilder("Gengar-Gmax", 0.95f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Fur };
        var purple = Rgb(104, 72, 164);
        var magenta = Rgb(214, 56, 140);
        var deep = Rgb(140, 28, 84);
        var tongue = Rgb(232, 90, 160);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.24f * s, 0.12f, 0.04f));
            b.Ell(leg, V(0.26f * s, 0.05f, 0.1f), V(0.09f, 0.05f, 0.11f), purple);
            Digits(b, leg, V(0.26f * s, 0.04f, 0.2f), V(0, 0, 1f), V(0.6f, 0, 0), 0.04f, 0.025f, purple);
        });
        // A mountain of a body, spiked along its back, nearly all of its front one gaping mouth: a portal of
        // reds deepening to black, a long tongue rolling out of it
        b.Ell(Body, V(0, 0.4f, 0), V(0.38f, 0.42f, 0.3f), purple);
        b.CutBox(Body, V(0, -0.3f, 0), V(0.8f, 0.3f, 0.8f), Quaternion.Identity);
        for (int i = -3; i <= 3; i++)
            b.Spike(Body, V(0.1f * i, 0.66f - 0.04f * Math.Abs(i), -0.18f), V(0.15f * i, 0.78f - 0.06f * Math.Abs(i), -0.38f), 0.08f, purple, 0.55f);
        PokeBuilder.Both(s =>
        {
            b.Spike(Body, V(0.3f * s, 0.3f, -0.1f), V(0.5f * s, 0.2f, -0.18f), 0.07f, purple, 0.5f);
            b.Spike(Body, V(0.32f * s, 0.12f, -0.02f), V(0.5f * s, 0.04f, 0.0f), 0.06f, purple, 0.5f);
        });
        b.Cut(Body, V(0, 0.34f, 0.3f), V(0.27f, 0.27f, 0.13f), blend: 0.012f);
        b.PaintEll(Body, V(0, 0.34f, 0.24f), V(0.29f, 0.29f, 0.12f), magenta);
        b.PaintEll(Body, V(0, 0.33f, 0.15f), V(0.19f, 0.19f, 0.1f), deep);
        b.PaintEll(Body, V(0, 0.32f, 0.08f), V(0.09f, 0.09f, 0.08f), Rgb(30, 10, 30));
        b.Box(Body, V(0, 0.585f, 0.27f), V(0.17f, 0.018f, 0.03f), 0.006f, White, V(-10f, 0, 0), blend: 0.004f);
        int jaw = b.Jaw(Body, V(0, 0.12f, 0.2f));
        b.Tube(jaw, Smooth(3, V(0.02f, 0.12f, 0.22f), V(0.12f, 0.1f, 0.36f), V(0.3f, 0.08f, 0.42f), V(0.44f, 0.12f, 0.34f), V(0.48f, 0.2f, 0.22f)), 0.07f, 0.05f, tongue, blend: 0f);
        // Ears like horns and small fierce eyes high on its head
        int head = b.Head(V(0, 0.66f, 0));
        PokeBuilder.Both(s => b.Spike(head, V(0.2f * s, 0.7f, -0.04f), V(0.36f * s, 0.98f, -0.1f), 0.1f, purple, 0.55f));
        PokeBuilder.Both(s => b.Eye(Body, V(0.12f * s, 0.68f, 0.22f), V(0.4f * s, 0.3f, 1f), 0.04f, pupil: Rgb(140, 30, 40), white: Rgb(250, 214, 64), glare: true));
        MaxClouds(b, head, V(0.06f, 0.76f, 0.0f), 0.045f, 0.18f, 0.32f, 1.1f, 2f);
        return b;
    }

    // ------------------------------------------------------------------ Gigantamax Kingler

    private static PokeBuilder KinglerGmax()
    {
        // Its great claw has grown bigger than all the rest of it, crystals like teeth along its grip, and foam
        // bubbles up round it
        const float Size = 0.21f;
        var b = CrabBuild("Kingler-Gmax", 0.95f, 0.07f, Size, 5, Rgb(214, 100, 56), Rgb(240, 234, 226));
        int arm = b.Model.Skeleton.Find("armR");
        var d = Vector3.Normalize(V(0.15f, 1f, 0.35f));
        var w = Vector3.Normalize(V(-1f, 0, 0) - d * Vector3.Dot(V(-1f, 0, 0), d));
        var face = Vector3.Normalize(Vector3.Cross(d, w));
        var hand = V(0.22f, 0.24f, 0.08f) + d * Size * 0.8f;
        var root = hand + d * Size * 0.55f;
        foreach (var (along, from) in new[] { (0.28f, -0.12f), (0.42f, -0.18f), (0.56f, -0.2f), (0.7f, -0.15f), (0.84f, -0.08f) })
        {
            var at = root + d * Size * along + w * Size * (from - 0.04f);
            b.Spike(arm, at, at + (w * 0.16f + d * 0.03f) * Size, Size * 0.065f, Rgb(244, 244, 236), mat: Shell, blend: 0.004f);
        }
        var foam = Rgb(242, 246, 252);
        foreach (var (at, size) in new[] { (hand + face * Size * 0.36f + d * Size * 0.1f, 0.05f), (hand - face * Size * 0.34f - d * Size * 0.15f, 0.042f), (hand - w * Size * 0.5f + d * Size * 0.2f, 0.036f), (hand + face * Size * 0.3f - d * Size * 0.45f, 0.03f) })
            b.Ell(arm, at, V(size, size, size), foam, mat: Shell, blend: 0.02f);
        foreach (var (x, z, size) in new[] { (-0.12f, 0.08f, 0.035f), (-0.05f, 0.11f, 0.03f), (0.06f, 0.11f, 0.03f), (0.13f, 0.05f, 0.035f) })
            b.Ell(Body, V(x, 0.2f, z), V(size, size, size), foam, mat: Shell, blend: 0.02f);
        MaxClouds(b, Body, V(0, 0.24f, -0.08f), 0.04f, 0.14f, 0.12f, 1.1f, 0.5f);
        return b;
    }

    // ------------------------------------------------------------------ Gigantamax Lapras

    private static PokeBuilder LaprasGmax() => LaprasBuild(true);

    // ------------------------------------------------------------------ Gigantamax Eevee

    private static PokeBuilder EeveeGmax()
    {
        var b = new PokeBuilder("Eevee-Gmax", 0.85f, BodyPlan.Quadruped, V(0, 0.2f, -0.02f)) { Coat = Fur };
        var brown = Rgb(198, 132, 72);
        var cream = Rgb(246, 228, 184);
        var dark = Rgb(112, 72, 42);

        foreach (var (z, front) in new[] { (0.08f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.13f, z), front);
                b.Limb(leg, V(0.06f * s, 0.13f, z), V(0.065f * s, 0.02f, z + 0.01f), 0.03f, 0.026f, brown);
                b.Ell(leg, V(0.065f * s, 0.018f, z + 0.025f), V(0.028f, 0.018f, 0.034f), brown);
            });
        b.Ell(Body, V(0, 0.2f, -0.02f), V(0.09f, 0.085f, 0.14f), brown);
        // An enormous ruff of cream fur that all but swallows it
        foreach (var (at, size) in new[]
        {
            (V(0, 0.24f, 0.12f), 0.14f), (V(0.15f, 0.26f, 0.06f), 0.12f), (V(-0.15f, 0.26f, 0.06f), 0.12f), (V(0.12f, 0.15f, 0.12f), 0.1f),
            (V(-0.12f, 0.15f, 0.12f), 0.1f), (V(0.17f, 0.34f, -0.02f), 0.1f), (V(-0.17f, 0.34f, -0.02f), 0.1f), (V(0, 0.36f, -0.04f), 0.11f)
        })
            b.Ell(Body, at, V(size, size * 0.9f, size * 0.85f), cream, blend: 0.03f);
        int tail = b.Tail(V(0, 0.24f, -0.14f));
        b.Ell(tail, V(0, 0.38f, -0.24f), V(0.1f, 0.18f, 0.1f), brown, V(-25f, 0, 0));
        b.PaintEll(tail, V(0, 0.52f, -0.31f), V(0.1f, 0.07f, 0.1f), cream);

        int head = b.Head(V(0, 0.36f, 0.1f));
        var c = V(0, 0.44f, 0.12f);
        var r = V(0.11f, 0.1f, 0.1f);
        b.Ell(head, c, r, brown);
        b.Ell(head, V(0, 0.41f, 0.2f), V(0.045f, 0.035f, 0.035f), brown);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.52f, 0.09f));
            MouseEar(b, ear, V(0.06f * s, 0.52f, 0.09f), V(0.19f * s, 0.76f, 0.04f), 0.06f, brown, dark, 0.25f);
        });
        b.Mark(head, V(0, 0.42f, 0.233f), V(0, 0.2f, 1f), 0.01f, 0.008f, dark);
        b.Mark(head, V(0, 0.395f, 0.226f), V(0, -0.3f, 1f), 0.018f, 0.007f, dark, MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.45f), V(0.4f * s, 0.05f, 1f), 0.032f, Rgb(110, 60, 40)));
        MaxClouds(b, head, V(0.08f, 0.58f, 0.06f), 0.03f, 0.12f, 0.26f, 1.2f, 0.5f);
        return b;
    }

    // ------------------------------------------------------------------ Gigantamax Snorlax

    private static PokeBuilder SnorlaxGmax()
    {
        var b = new PokeBuilder("Snorlax-Gmax", 1f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var blue = Rgb(40, 104, 132);
        var cream = Rgb(240, 228, 198);
        var pad = Rgb(150, 116, 86);
        var grass = Rgb(118, 176, 82);
        var bush = Rgb(62, 128, 66);
        var path = Rgb(222, 206, 160);
        var bark = Rgb(110, 76, 50);
        var leaves = Rgb(44, 96, 62);

        // Asleep on its back, its feet up at one end, soles to the world
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(-0.3f, 0.22f, 0.12f * s));
            b.Limb(leg, V(-0.3f, 0.22f, 0.12f * s), V(-0.42f, 0.28f, 0.14f * s), 0.09f, 0.085f, blue);
            b.Ell(leg, V(-0.47f, 0.3f, 0.15f * s), V(0.06f, 0.1f, 0.09f), blue);
            b.PaintEll(leg, V(-0.52f, 0.3f, 0.15f * s), V(0.03f, 0.09f, 0.08f), cream);
            b.Mark(leg, V(-0.525f, 0.29f, 0.15f * s), V(-1f, 0, 0.1f * s), 0.045f, 0.05f, pad);
        });
        // A great long body, and on its belly a hill of grass with a path over it, bushes, and a tree in fruit
        b.Ell(Body, V(0, 0.26f, 0), V(0.44f, 0.24f, 0.3f), blue);
        b.CutBox(Body, V(0, -0.3f, 0), V(0.8f, 0.3f, 0.8f), Quaternion.Identity);
        b.Ell(Body, V(-0.02f, 0.44f, 0.02f), V(0.34f, 0.1f, 0.24f), grass, mat: Leaf);
        b.PaintEll(Body, V(0.05f, 0.52f, 0.0f), V(0.3f, 0.06f, 0.035f), path, V(0, 30f, 0));
        foreach (var (at, size) in new[] { (V(-0.22f, 0.5f, 0.12f), 0.06f), (V(-0.12f, 0.53f, -0.14f), 0.07f), (V(0.18f, 0.49f, -0.12f), 0.06f), (V(0.2f, 0.47f, 0.16f), 0.05f), (V(-0.28f, 0.46f, -0.06f), 0.05f) })
            b.Ell(Body, at, V(size, size * 0.8f, size), bush, mat: Leaf, blend: 0.02f);
        b.Limb(Body, V(0.0f, 0.5f, -0.04f), V(0.02f, 0.78f, -0.06f), 0.04f, 0.03f, bark);
        b.Ell(Body, V(0.02f, 0.86f, -0.06f), V(0.15f, 0.11f, 0.13f), leaves, mat: Leaf);
        foreach (var dir in new[] { V(-0.8f, 0.2f, 0.5f), V(0.7f, 0.1f, 0.6f), V(0.2f, 0.6f, 0.7f), V(-0.3f, -0.3f, 0.9f), V(0.9f, -0.2f, -0.2f) })
        {
            var n = Vector3.Normalize(dir);
            b.Ell(Body, V(0.02f, 0.86f, -0.06f) + n * V(0.15f, 0.11f, 0.13f), V(0.022f, 0.022f, 0.022f), Rgb(214, 56, 60), mat: Shell, blend: 0.006f);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.24f, 0.32f, 0.2f * s));
            b.Limb(arm, V(0.24f, 0.32f, 0.2f * s), V(0.14f, 0.4f, 0.27f * s), 0.07f, 0.06f, blue);
            Claws(b, arm, V(0.1f, 0.43f, 0.28f * s), V(0, 0, 0.03f), V(-1f, 0.4f, 0), 0.035f, 0.014f);
        });

        // Its head at the other end, eyes shut, small ears
        int head = b.Head(V(0.34f, 0.3f, 0.06f));
        b.Ell(head, V(0.44f, 0.3f, 0.1f), V(0.16f, 0.15f, 0.15f), blue);
        b.PaintEll(head, V(0.48f, 0.28f, 0.2f), V(0.12f, 0.1f, 0.08f), cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.44f + 0.09f * s, 0.42f, 0.06f));
            b.Spike(ear, V(0.44f + 0.09f * s, 0.41f, 0.06f), V(0.46f + 0.13f * s, 0.5f, 0.05f), 0.045f, blue, 0.6f);
        });
        PokeBuilder.Both(s => b.Eye(head, V(0.44f + 0.05f * s, 0.32f, 0.24f), V(0.3f * s, 0.05f, 1f), 0.024f, closed: true));
        b.Mark(head, V(0.45f, 0.26f, 0.24f), V(0.1f, -0.15f, 1f), 0.035f, 0.01f, Rgb(70, 50, 50), MarkShape.Wave);
        MaxClouds(b, Body, V(0.06f, 0.95f, -0.08f), 0.035f, 0.14f, 0.24f, 1.1f, 0.3f);
        return b;
    }

    // ------------------------------------------------------------------ Gigantamax Garbodor (its build in PokemonModels.Unova2.cs)

    private static PokeBuilder GarbodorGmax() => GarbodorBuild(true);

    // ------------------------------------------------------------------ Gigantamax Melmetal (its build in PokemonModels.Alola3.cs)

    private static PokeBuilder MelmetalGmax() => MelmetalBuild(true);

    // ------------------------------------------------------------------ Galar's: Rillaboom, Cinderace, Inteleon, Corviknight and Orbeetle (their builds in PokemonModels.Galar1.cs)

    private static PokeBuilder RillaboomGmax() => RillaboomBuild(true);

    private static PokeBuilder CinderaceGmax() => CinderaceBuild(true);

    private static PokeBuilder InteleonGmax() => InteleonBuild(true);

    private static PokeBuilder CorviknightGmax() => CorviknightBuild(true);

    private static PokeBuilder OrbeetleGmax() => OrbeetleBuild(true);

    // ------------------------------------------------------------------ Galar's second batch's: Drednaw, Coalossal, Flapple (and Appletun, which looks the same), Sandaconda, Toxtricity (both forms look the same) and Centiskorch (their builds in PokemonModels.Galar2.cs)

    private static PokeBuilder DrednawGmax() => DrednawBuild(true);

    private static PokeBuilder CoalossalGmax() => CoalossalBuild(true);

    private static PokeBuilder FlappleGmax() => FlappleBuild(true);

    private static PokeBuilder SandacondaGmax() => SandacondaBuild(true);

    private static PokeBuilder ToxtricityAmpedGmax() => ToxtricityBuild(false, true);

    private static PokeBuilder CentiskorchGmax() => CentiskorchBuild(true);

    // ------------------------------------------------------------------ Galar's third batch's: Hatterene, Grimmsnarl and Alcremie (their builds in PokemonModels.Galar3.cs)

    private static PokeBuilder HattereneGmax() => HattereneBuild(true);

    private static PokeBuilder GrimmsnarlGmax() => GrimmsnarlBuild(true);
}
