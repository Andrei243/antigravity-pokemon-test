using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Unova's third batch in National Pokédex
// order: Minccino (572) to Chandelure (609). Their forms are in PokemonModels.Regional.cs and PokemonModels.Megas.cs
// with the other forms. Helpers shared with the earlier batches are in PokemonModels.Sinnoh1.cs to
// PokemonModels.Sinnoh4.cs, PokemonModels.Kanto1.cs to PokemonModels.Kanto3.cs, PokemonModels.Johto1.cs,
// PokemonModels.Johto2.cs, PokemonModels.Hoenn1.cs to PokemonModels.Hoenn3.cs, PokemonModels.Unova1.cs and
// PokemonModels.Unova2.cs.
internal static partial class PokemonModels
{
    /// <summary>A bow of ribbon at <paramref name="at"/>: two loops out to the sides of a knot, flat across <paramref name="facing"/>.</summary>
    private static void Bow(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float size, Color color)
    {
        var n = Vector3.Normalize(facing);
        var side = Vector3.Cross(V(0, 1f, 0), n);
        side = side.LengthSquared() < 0.01f ? V(1f, 0, 0) : Vector3.Normalize(side);
        foreach (float s in new[] { -1f, 1f })
            Frond(b, bone, at, at + side * (s * size * 2f), size * 0.8f, color, n, 0.35f);
        b.Ell(bone, at, V(size * 0.45f, size * 0.45f, size * 0.45f), color, blend: 0.006f);
    }

    // ------------------------------------------------------------------ Minccino line

    /// <summary>Minccino: a little gray chinchilla, great round ears pink inside, a tuft on its head, big dark eyes, small paws held at its chest and a great fluffy tail it sweeps with.</summary>
    private static PokeBuilder Minccino()
    {
        var b = new PokeBuilder("Minccino", 0.5f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Fur };
        var gray = Rgb(206, 204, 200);
        var light = Rgb(238, 236, 232);
        var pink = Rgb(236, 150, 160);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.06f, 0));
            b.Limb(leg, V(0.03f * s, 0.07f, 0), V(0.035f * s, 0.02f, 0.01f), 0.018f, 0.016f, gray);
            b.Ell(leg, V(0.036f * s, 0.014f, 0.02f), V(0.018f, 0.014f, 0.026f), gray);
            int arm = b.Arm(s, V(0.04f * s, 0.15f, 0.02f));
            b.Limb(arm, V(0.04f * s, 0.15f, 0.02f), V(0.022f * s, 0.12f, 0.055f), 0.012f, 0.011f, gray);
        });
        b.Ell(Body, V(0, 0.12f, 0), V(0.05f, 0.065f, 0.045f), gray);
        b.PaintEll(Body, V(0, 0.13f, 0.04f), V(0.03f, 0.05f, 0.02f), light);
        // A great fluffy tail sweeping out behind, pale at its end
        int tail = b.Tail(V(0, 0.08f, -0.04f));
        var tp = Smooth(3, V(0, 0.08f, -0.04f), V(0.06f, 0.06f, -0.12f), V(0.13f, 0.1f, -0.16f), V(0.15f, 0.2f, -0.12f));
        b.Tube(tail, tp, 0.018f, 0.045f, gray, blend: 0.02f);
        b.PaintEll(tail, tp[^1] + V(0, 0.01f, 0), V(0.05f, 0.06f, 0.05f), light);
        int head = b.Head(V(0, 0.18f, 0.01f));
        var c = V(0, 0.235f, 0.015f);
        var r = V(0.075f, 0.065f, 0.065f);
        b.Ell(head, c, r, gray);
        foreach (var o in new[] { V(-0.02f, 0.06f, 0.02f), V(0, 0.07f, 0.01f), V(0.02f, 0.06f, 0.02f) })
            b.Ell(head, c + o, V(0.018f, 0.018f, 0.018f), gray, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.04f, -0.01f));
            var ec = c + V(0.1f * s, 0.08f, -0.015f);
            b.Limb(ear, c + V(0.045f * s, 0.035f, -0.01f), ec, 0.02f, 0.02f, gray);
            b.Ell(ear, ec, V(0.055f, 0.06f, 0.016f), gray, V(0, 0, -30f * s));
            b.PaintEll(ear, ec + V(0, 0, 0.012f), V(0.038f, 0.044f, 0.012f), pink, V(0, 0, -30f * s));
            var at = On(c, r, 0.03f * s, c.Y - 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.02f, Rgb(80, 60, 50));
        });
        var m = On(c, r, 0, c.Y - 0.035f);
        b.Mark(head, m, Outward(c, r, m), 0.012f, 0.006f, Rgb(120, 90, 90), MarkShape.Smile);
        return b;
    }

    /// <summary>Cinccino: a slender pale gray chinchilla wrapped in long scarves of white silky fur that curl out to either side, great round ears and a fluffy white tail.</summary>
    private static PokeBuilder Cinccino()
    {
        var b = new PokeBuilder("Cinccino", 0.6f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        var gray = Rgb(214, 212, 210);
        var white = Rgb(246, 246, 250);
        var pink = Rgb(236, 150, 160);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.09f, 0));
            b.Limb(leg, V(0.03f * s, 0.1f, 0), V(0.035f * s, 0.02f, 0.01f), 0.017f, 0.014f, gray);
            b.Ell(leg, V(0.036f * s, 0.014f, 0.02f), V(0.018f, 0.014f, 0.026f), gray);
        });
        b.Ell(Body, V(0, 0.15f, 0), V(0.045f, 0.075f, 0.04f), gray);
        // Scarves of white fur round its shoulders, curling out to each side and down to the ground
        b.Torus(Body, V(0, 0.2f, 0), 0.045f, 0.022f, white, sz: 0.9f, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.04f * s, 0.2f, 0.02f);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder, V(0.03f * s, 0.15f, 0.05f), 0.012f, 0.011f, gray);
            var scarf = Smooth(3, shoulder, V(0.13f * s, 0.23f, 0), V(0.21f * s, 0.17f, -0.02f), V(0.21f * s, 0.07f, 0.02f), V(0.15f * s, 0.02f, 0.05f));
            Ribbon(b, arm, scarf, 0.055f, white, V(0, 0.3f, 1f), 0.014f);
        });
        int tail = b.Tail(V(0, 0.1f, -0.04f));
        var tp = Smooth(3, V(0, 0.1f, -0.04f), V(0.04f, 0.1f, -0.12f), V(0.08f, 0.18f, -0.15f), V(0.07f, 0.27f, -0.12f));
        b.Tube(tail, tp, 0.018f, 0.04f, white, blend: 0.02f);
        int head = b.Head(V(0, 0.22f, 0.01f));
        var c = V(0, 0.28f, 0.015f);
        var r = V(0.065f, 0.058f, 0.058f);
        b.Ell(head, c, r, gray);
        foreach (var o in new[] { V(-0.018f, 0.055f, 0.02f), V(0, 0.064f, 0.01f), V(0.018f, 0.055f, 0.02f) })
            b.Ell(head, c + o, V(0.016f, 0.016f, 0.016f), white, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.045f * s, 0.035f, -0.01f));
            var ec = c + V(0.09f * s, 0.075f, -0.015f);
            b.Limb(ear, c + V(0.04f * s, 0.03f, -0.01f), ec, 0.018f, 0.018f, gray);
            b.Ell(ear, ec, V(0.05f, 0.055f, 0.015f), gray, V(0, 0, -30f * s));
            b.PaintEll(ear, ec + V(0, 0, 0.011f), V(0.034f, 0.04f, 0.011f), pink, V(0, 0, -30f * s));
            var at = On(c, r, 0.027f * s, c.Y - 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.017f, Rgb(80, 60, 50));
        });
        var m = On(c, r, 0, c.Y - 0.032f);
        b.Mark(head, m, Outward(c, r, m), 0.011f, 0.006f, Rgb(120, 90, 90), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Gothita line

    /// <summary>Gothita: a little girl of a Pokémon, a big lilac face under black hair tied with two white bows and a topknot, blue eyes, small red lips, a black dress with a white bow at the collar and a zigzag hem.</summary>
    private static PokeBuilder Gothita()
    {
        var b = new PokeBuilder("Gothita", 0.5f, BodyPlan.Biped, V(0, 0.11f, 0)) { Coat = Fur };
        var lilac = Rgb(176, 146, 206);
        var black = Rgb(50, 46, 56);
        var white = Rgb(244, 244, 248);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.06f, 0));
            b.Limb(leg, V(0.025f * s, 0.07f, 0), V(0.03f * s, 0.02f, 0.01f), 0.016f, 0.015f, black);
            b.Ell(leg, V(0.031f * s, 0.014f, 0.018f), V(0.017f, 0.014f, 0.024f), black);
            int arm = b.Arm(s, V(0.045f * s, 0.15f, 0));
            b.Limb(arm, V(0.045f * s, 0.15f, 0), V(0.075f * s, 0.11f, 0.02f), 0.012f, 0.011f, black);
        });
        b.Ell(Body, V(0, 0.12f, 0), V(0.05f, 0.06f, 0.045f), black);
        b.PaintTorus(Body, V(0, 0.08f, 0), 0.046f, 0.012f, white, sz: 0.9f);
        var hem = V(0, 0.085f, 0.04f);
        b.Mark(Body, hem, V(0, -0.2f, 1f), 0.04f, 0.012f, black, MarkShape.Zigzag);
        Bow(b, Body, V(0, 0.165f, 0.04f), V(0, 0.2f, 1f), 0.018f, white);
        int head = b.Head(V(0, 0.18f, 0.01f));
        var c = V(0, 0.25f, 0.01f);
        var r = V(0.08f, 0.075f, 0.07f);
        b.Ell(head, c, r, lilac);
        b.PaintEll(head, c + V(0, 0.05f, -0.03f), V(0.088f, 0.055f, 0.07f), black);
        b.Limb(head, c + V(0, 0.07f, 0), c + V(0, 0.1f, 0), 0.012f, 0.012f, black);
        b.Ell(head, c + V(0, 0.115f, 0), V(0.022f, 0.025f, 0.022f), black);
        PokeBuilder.Both(s =>
        {
            Bow(b, head, c + V(0.075f * s, 0.035f, -0.01f), V(s, 0.2f, 0), 0.022f, white);
            var at = On(c, r, 0.033f * s, c.Y - 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.022f, Rgb(70, 170, 230));
        });
        var lips = On(c, r, 0, c.Y - 0.045f);
        b.Mark(head, lips, Outward(c, r, lips), 0.01f, 0.007f, Rgb(232, 96, 110));
        return b;
    }

    /// <summary>Gothorita: a slim girl in black, a lilac face, black hair in two great round buns tied with white bows, white bows at the collar and the waist, long black legs and blue eyes under heavy lids.</summary>
    private static PokeBuilder Gothorita()
    {
        var b = new PokeBuilder("Gothorita", 0.75f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var lilac = Rgb(176, 146, 206);
        var black = Rgb(50, 46, 56);
        var white = Rgb(244, 244, 248);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.21f, 0));
            b.Limb(leg, V(0.025f * s, 0.22f, 0), V(0.02f * s, 0.025f, 0.01f), 0.016f, 0.013f, black);
            b.Ell(leg, V(0.02f * s, 0.018f, 0.02f), V(0.022f, 0.018f, 0.03f), black);
        });
        b.Ell(Body, V(0, 0.23f, 0), V(0.065f, 0.035f, 0.055f), black);
        b.Ell(Body, V(0, 0.3f, 0), V(0.038f, 0.06f, 0.034f), black);
        Bow(b, Body, V(0, 0.265f, 0.04f), V(0, 0, 1f), 0.017f, white);
        Bow(b, Body, V(0, 0.35f, 0.03f), V(0, 0.2f, 1f), 0.015f, white);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.035f * s, 0.34f, 0);
            int arm = b.Arm(s, shoulder);
            var hand = s > 0 ? V(0.1f * s, 0.4f, 0.03f) : V(0.08f * s, 0.26f, 0.03f);
            b.Limb(arm, shoulder, hand, 0.011f, 0.009f, black);
            b.Ell(arm, hand, V(0.014f, 0.014f, 0.014f), white);
        });
        int head = b.Head(V(0, 0.36f, 0.01f));
        var c = V(0, 0.41f, 0.015f);
        var r = V(0.058f, 0.058f, 0.052f);
        b.Ell(head, c, r, lilac);
        b.PaintEll(head, c + V(0, 0.045f, -0.02f), V(0.065f, 0.04f, 0.06f), black);
        PokeBuilder.Both(s =>
        {
            // Hair in a great round bun at each side, a bow over it and a lock hanging under it
            var bun = c + V(0.085f * s, 0.05f, -0.02f);
            b.Limb(head, c + V(0.04f * s, 0.04f, -0.02f), bun, 0.02f, 0.02f, black);
            b.Ell(head, bun, V(0.042f, 0.042f, 0.04f), black);
            Bow(b, head, bun + V(0.01f * s, 0.045f, 0), V(0.4f * s, 1f, 0.3f), 0.018f, white);
            b.Limb(head, bun + V(0, -0.03f, 0), bun + V(0.005f * s, -0.07f, 0), 0.01f, 0.008f, black);
            b.Ell(head, bun + V(0.005f * s, -0.08f, 0), V(0.014f, 0.018f, 0.014f), black);
            var at = On(c, r, 0.025f * s, c.Y - 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(70, 170, 230), glare: true);
        });
        var lips = On(c, r, 0, c.Y - 0.037f);
        b.Mark(head, lips, Outward(c, r, lips), 0.009f, 0.007f, Rgb(220, 70, 80));
        return b;
    }

    /// <summary>Gothitelle: a tall lady in a long black gown in tiers with white bows down its front, a lilac face under a crown of long black spikes of hair, blue eyes, red lips and thin black arms with white cuffs.</summary>
    private static PokeBuilder Gothitelle()
    {
        var b = new PokeBuilder("Gothitelle", 1f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var lilac = Rgb(176, 146, 206);
        var black = Rgb(46, 42, 52);
        var white = Rgb(244, 244, 248);
        // A gown in three tiers down to the ground, a white bow at the front of each
        foreach (var (y0, y1, r0, r1) in new[] { (0.03f, 0.2f, 0.15f, 0.1f), (0.2f, 0.32f, 0.11f, 0.07f), (0.32f, 0.43f, 0.075f, 0.045f) })
        {
            b.Limb(Body, V(0, y0, 0), V(0, y1, 0), r0, r1, black);
            Bow(b, Body, V(0, y0 + 0.03f, r0 * 0.93f), V(0, 0.15f, 1f), 0.022f, white);
        }
        b.CutBox(Body, V(0, -0.1f, 0), V(0.3f, 0.1f, 0.3f), Quaternion.Identity);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.05f * s, 0.44f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.13f * s, 0.38f, 0.05f);
            var hand = s > 0 ? V(0.17f * s, 0.46f, 0.1f) : V(0.15f * s, 0.3f, 0.08f);
            b.Limb(arm, shoulder, elbow, 0.012f, 0.01f, black);
            b.Limb(arm, elbow, hand, 0.01f, 0.009f, black);
            b.Torus(arm, Vector3.Lerp(elbow, hand, 0.8f), 0.016f, 0.008f, white, Euler(hand - elbow), blend: 0.004f);
            b.Ell(arm, hand, V(0.014f, 0.016f, 0.012f), lilac);
        });
        int head = b.Head(V(0, 0.5f, 0.01f));
        var c = V(0, 0.56f, 0.02f);
        var r = V(0.058f, 0.065f, 0.055f);
        b.Limb(head, V(0, 0.44f, 0), c, 0.022f, 0.025f, black);
        b.Ell(head, c, r, lilac);
        b.PaintEll(head, c + V(0, 0.05f, -0.025f), V(0.065f, 0.045f, 0.06f), black);
        // A crown of long flat spikes of hair spread round its head
        for (int i = 0; i < 9; i++)
        {
            float a = (-30f + i * 30f) * Degree;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            Blade(b, head, c + d * 0.035f + V(0, 0, -0.03f), c + d * 0.18f + V(0, 0, -0.04f), 0.028f, black, V(0, 0, 1f), 0.3f);
        }
        PokeBuilder.Both(s => Bow(b, head, c + V(0.045f * s, 0.07f, -0.01f), V(0.3f * s, 0.6f, 1f), 0.016f, white));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.024f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(70, 170, 230), glare: true);
        });
        var lips = On(c, r, 0, c.Y - 0.04f);
        b.Mark(head, lips, Outward(c, r, lips), 0.009f, 0.007f, Rgb(220, 70, 80));
        return b;
    }

    // ------------------------------------------------------------------ Solosis line

    /// <summary>Solosis: a little cell in a ball of green gel, its pale face looking out with two dark eyes and a red diamond, a yellow horn curling on top.</summary>
    private static PokeBuilder Solosis()
    {
        var b = new PokeBuilder("Solosis", 0.45f, BodyPlan.Floating, V(0, 0.18f, 0)) { Coat = Shell }.Hover();
        var gel = Rgb(120, 200, 140);
        var cell = Rgb(222, 236, 190);
        var c = V(0, 0.18f, 0);
        b.Ell(Body, c, V(0.13f, 0.13f, 0.12f), gel);
        int head = b.Head(c);
        var hc = c + V(0, 0, 0.07f);
        var hr = V(0.095f, 0.085f, 0.08f);
        b.Ell(head, hc, hr, cell, mat: Fur);
        Curl(b, head, hc + V(0.04f, 0.06f, 0.02f), hc + V(0.06f, 0.1f, 0.02f), V(1f, 0, 0), V(0, 1f, 0), 0.022f, 0.8f, 0.012f, Rgb(240, 210, 110));
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.045f * s, hc.Y);
            b.Eye(head, at, Outward(hc, hr, at), 0.016f, Rgb(50, 70, 60));
        });
        var mark = On(hc, hr, 0, hc.Y + 0.004f);
        b.Mark(head, mark, Outward(hc, hr, mark), 0.02f, 0.026f, Rgb(220, 90, 90), MarkShape.Diamond);
        return Lift(b);
    }

    /// <summary>Duosion: two cells in a lumpy blob of green gel, the upper one's pale face looking out with two dark eyes and a red diamond, the lower one curled beneath it.</summary>
    private static PokeBuilder Duosion()
    {
        var b = new PokeBuilder("Duosion", 0.65f, BodyPlan.Floating, V(0, 0.26f, 0)) { Coat = Shell }.Hover();
        var gel = Rgb(120, 200, 140);
        var cell = Rgb(214, 230, 182);
        var c = V(0, 0.26f, 0);
        b.Ell(Body, c, V(0.17f, 0.2f, 0.15f), gel);
        foreach (var o in new[] { V(-0.12f, 0.12f, 0), V(0.13f, -0.08f, 0.02f), V(-0.1f, -0.14f, 0.03f), V(0.06f, 0.18f, -0.02f) })
            b.Ell(Body, c + o, V(0.05f, 0.045f, 0.05f), gel, blend: 0.04f);
        b.Ell(Body, c + V(0, -0.1f, 0.09f), V(0.07f, 0.06f, 0.06f), cell, mat: Fur);
        int head = b.Head(c + V(0, 0.05f, 0.04f));
        var hc = c + V(0, 0.05f, 0.1f);
        var hr = V(0.08f, 0.075f, 0.07f);
        b.Ell(head, hc, hr, cell, mat: Fur);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.038f * s, hc.Y);
            b.Eye(head, at, Outward(hc, hr, at), 0.015f, Rgb(50, 70, 60));
        });
        var mark = On(hc, hr, 0, hc.Y + 0.004f);
        b.Mark(head, mark, Outward(hc, hr, mark), 0.018f, 0.024f, Rgb(220, 90, 90), MarkShape.Diamond);
        return Lift(b);
    }

    /// <summary>Reuniclus: a body of green gel with two round lobes on its head and great gel arms ending in three fingers, a red nucleus held in each hand, and a small pale body in its middle with an open mouth.</summary>
    private static PokeBuilder Reuniclus()
    {
        var b = new PokeBuilder("Reuniclus", 0.9f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var gel = Rgb(120, 200, 140);
        var cell = Rgb(214, 230, 182);
        var red = Rgb(214, 80, 80);
        var c = V(0, 0.3f, 0);
        b.Ell(Body, c, V(0.13f, 0.14f, 0.11f), gel);
        PokeBuilder.Both(s => b.Ell(Body, V(0.07f * s, 0.43f, 0), V(0.04f, 0.045f, 0.035f), gel));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.3f, 0));
            var hand = V(0.32f * s, 0.36f, 0);
            b.Tube(arm, Smooth(2, V(0.1f * s, 0.3f, 0), V(0.2f * s, 0.32f, 0.01f), hand), 0.045f, 0.05f, gel, blend: 0.02f);
            b.Ell(arm, hand, V(0.085f, 0.085f, 0.06f), gel);
            foreach (var d in new[] { V(0.4f * s, 1f, 0), V(s, 0.5f, 0), V(s, -0.3f, 0) })
                b.Limb(arm, hand, hand + Vector3.Normalize(d) * 0.1f, 0.025f, 0.02f, gel);
            b.Ell(arm, hand + V(0, 0, 0.045f), V(0.04f, 0.04f, 0.03f), red, blend: 0.006f);
            foreach (float t in new[] { 0.35f, 0.6f })
                b.Mark(arm, Vector3.Lerp(V(0.1f * s, 0.3f, 0), hand, t) + V(0, 0.01f, 0.045f), V(0, 0.2f, 1f), 0.014f, 0.01f, Rgb(214, 180, 110));
        });
        int head = b.Head(c + V(0, 0, 0.04f));
        var hc = c + V(0, -0.01f, 0.08f);
        var hr = V(0.07f, 0.08f, 0.06f);
        b.Ell(head, hc, hr, cell, mat: Fur);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.028f * s, hc.Y + 0.02f);
            b.Eye(head, at, Outward(hc, hr, at), 0.012f, Rgb(50, 70, 60));
        });
        Grin(b, head, On(hc, hr, 0, hc.Y - 0.02f), V(0.028f, 0.018f, 0.02f), Rgb(200, 90, 90));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Ducklett line

    /// <summary>Ducklett: a little blue water bird, a yellow bill, a heart-shaped tuft on its head, scalloped feathers on its chest, one wing raised and orange feet.</summary>
    private static PokeBuilder Ducklett()
    {
        var b = new PokeBuilder("Ducklett", 0.55f, BodyPlan.Bird, V(0, 0.14f, 0)) { Coat = Fur };
        var teal = Rgb(140, 206, 220);
        var blue = Rgb(70, 150, 210);
        var yellow = Rgb(246, 196, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.08f, 0));
            b.Limb(leg, V(0.03f * s, 0.08f, 0), V(0.035f * s, 0.015f, 0.01f), 0.012f, 0.01f, yellow, Scales);
            b.Ell(leg, V(0.04f * s, 0.008f, 0.035f), V(0.03f, 0.008f, 0.035f), yellow, mat: Scales);
        });
        var bc = V(0, 0.13f, 0);
        var br = V(0.07f, 0.07f, 0.075f);
        b.Ell(Body, bc, br, teal);
        b.PaintEll(Body, bc + V(0, -0.045f, 0.01f), V(0.08f, 0.045f, 0.085f), blue);
        foreach (var (x, y) in new[] { (-0.03f, 0.12f), (0.03f, 0.12f), (0f, 0.09f) })
        {
            var at = On(bc, br, x, y);
            b.Mark(Body, at, Outward(bc, br, at), 0.016f, 0.012f, Rgb(100, 180, 220), MarkShape.Smile, 180f);
        }
        // One wing raised high, the other folded
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.06f * s, 0.03f, -0.01f);
            int wing = b.Wing(s, root);
            var tip = s < 0 ? root + V(0.07f * s, 0.16f, -0.03f) : root + V(0.03f * s, -0.06f, -0.06f);
            Frond(b, wing, root, tip, 0.04f, teal, V(s, 0, 0.3f), 0.25f);
            Frond(b, wing, root + V(0, -0.01f, 0), Vector3.Lerp(root, tip, 0.75f) + V(0.02f * s, 0, -0.01f), 0.03f, teal, V(s, 0, 0.3f), 0.25f);
        });
        int tail = b.Tail(bc + V(0, 0.01f, -0.07f));
        b.Spike(tail, bc + V(0, 0.01f, -0.06f), bc + V(0, 0.04f, -0.12f), 0.025f, teal, 0.5f);
        int head = b.Head(V(0, 0.19f, 0.02f));
        var c = V(0, 0.245f, 0.025f);
        var r = V(0.058f, 0.058f, 0.055f);
        b.Ell(head, c, r, teal);
        b.Ell(head, c + V(0, -0.015f, 0.065f), V(0.042f, 0.016f, 0.04f), yellow, mat: Shell, blend: 0.01f);
        PokeBuilder.Both(s => Frond(b, head, c + V(0.005f * s, 0.05f, 0), c + V(0.028f * s, 0.1f, -0.005f), 0.018f, teal, V(0, 0, 1f), 0.3f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, sclera: true, pupil: Rgb(30, 30, 30));
        });
        return b;
    }

    /// <summary>Swanna: a white swan, its long neck curved in an S, a yellow beak, a black mask round its eyes, a crest of feathers, blue feathers ruffled round its lower body and gray legs.</summary>
    private static PokeBuilder Swanna()
    {
        var b = new PokeBuilder("Swanna", 1f, BodyPlan.Bird, V(0, 0.24f, -0.04f)) { Coat = Fur };
        var white = Rgb(244, 246, 250);
        var blue = Rgb(110, 160, 220);
        var gray = Rgb(100, 100, 110);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.18f, -0.02f));
            var ankle = V(0.045f * s, 0.03f, 0);
            b.Limb(leg, V(0.04f * s, 0.18f, -0.02f), ankle, 0.014f, 0.011f, gray, Scales);
            b.Ell(leg, ankle + V(0, -0.02f, 0.03f), V(0.03f, 0.008f, 0.035f), gray, mat: Scales);
        });
        var bc = V(0, 0.25f, -0.04f);
        var br = V(0.11f, 0.09f, 0.15f);
        b.Ell(Body, bc, br, white);
        // Wings folded along its back; ruffled blue feathers round its lower body
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.08f * s, 0.04f, 0.06f));
            Frond(b, wing, bc + V(0.08f * s, 0.04f, 0.06f), bc + V(0.11f * s, 0.06f, -0.2f), 0.07f, white, V(s, 0.4f, 0), 0.22f);
            Frond(b, wing, bc + V(0.06f * s, 0.04f, -0.1f), bc + V(0.06f * s, 0.12f, -0.26f), 0.04f, white, V(s, 0.4f, 0), 0.22f);
            for (int i = 0; i < 3; i++)
            {
                var root = bc + V(0.07f * s, -0.04f, 0.06f - i * 0.08f);
                PointedLeaf(b, Body, root, root + V(0.04f * s, -0.07f, -0.03f), 0.03f, blue, V(s, -0.3f, 0.2f), 0.2f);
            }
        });
        b.PaintEll(Body, bc + V(0, -0.07f, 0), V(0.12f, 0.04f, 0.16f), blue);
        // A long neck in an S up to a small head with a yellow beak
        b.Tube(Body, Smooth(3, bc + V(0, 0.04f, 0.12f), V(0, 0.38f, 0.12f), V(0, 0.48f, 0.07f), V(0, 0.57f, 0.1f)), 0.035f, 0.024f, white, blend: 0.01f);
        int head = b.Head(V(0, 0.57f, 0.1f));
        var c = V(0, 0.6f, 0.12f);
        var r = V(0.035f, 0.035f, 0.045f);
        b.Ell(head, c, r, white);
        b.Spike(head, c + V(0, -0.005f, 0.035f), c + V(0, -0.015f, 0.13f), 0.014f, Rgb(246, 210, 80), 0.6f, Shell);
        b.PaintEll(head, c + V(0, 0, 0.02f), V(0.04f, 0.016f, 0.03f), Rgb(40, 40, 46));
        PokeBuilder.Both(s => Blade(b, head, c + V(0.012f * s, 0.025f, -0.01f), c + V(0.03f * s, 0.07f, -0.05f), 0.016f, white, V(s, 0, 0.3f), 0.3f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.028f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, sclera: true, pupil: Rgb(40, 40, 50), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Vanillite line

    /// <summary>The swirl of soft snow on top of the Vanillite line: rounds narrowing to a curled point.</summary>
    private static void SnowSwirl(PokeBuilder b, int bone, Vector3 at, float size)
    {
        var snow = Rgb(246, 248, 252);
        b.Ell(bone, at, V(size, size * 0.45f, size * 0.85f), snow);
        b.Ell(bone, at + V(0, size * 0.4f, 0), V(size * 0.68f, size * 0.38f, size * 0.6f), snow);
        b.Spike(bone, at + V(0, size * 0.7f, 0), at + V(size * 0.15f, size * 1.25f, -size * 0.15f), size * 0.4f, snow);
    }

    /// <summary>Vanillite: a floating scoop of ice cream, a white swirl on top of a pale icy body, blue eyes, a blue smile, crystals of ice at its sides and a cone of blue ice beneath.</summary>
    private static PokeBuilder Vanillite()
    {
        var b = new PokeBuilder("Vanillite", 0.5f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Fur }.Hover();
        var ice = Rgb(236, 242, 250);
        var cyan = Rgb(150, 200, 240);
        var c = V(0, 0.2f, 0);
        var r = V(0.08f, 0.075f, 0.07f);
        b.Ell(Body, c, r, ice);
        int head = b.Head(c + V(0, 0.06f, 0));
        SnowSwirl(b, head, c + V(0, 0.07f, 0), 0.075f);
        PokeBuilder.Both(s =>
        {
            var side = Out(c, r, default, V(s, 0, 0.3f));
            Gem(b, Body, side, V(s, 0, 0.3f), 0.022f, cyan);
            var at = On(c, r, 0.03f * s, c.Y + 0.01f);
            b.Eye(Body, at, Outward(c, r, at), 0.02f, Rgb(70, 110, 210));
        });
        var m = On(c, r, 0, c.Y - 0.03f);
        b.Mark(Body, m, Outward(c, r, m), 0.024f, 0.012f, Rgb(70, 100, 200), MarkShape.Smile);
        int tail = b.Tail(c + V(0, -0.06f, 0));
        b.Spike(tail, c + V(0, -0.05f, 0), c + V(0, -0.16f, 0.01f), 0.05f, cyan, mat: Glow);
        return Lift(b);
    }

    /// <summary>Vanillish: a taller ice cream, a swirl of snow on top, a ring of ice crystals round its middle, big blue eyes, a wide smile and a long cone of blue ice beneath.</summary>
    private static PokeBuilder Vanillish()
    {
        var b = new PokeBuilder("Vanillish", 0.8f, BodyPlan.Floating, V(0, 0.28f, 0)) { Coat = Fur }.Hover();
        var ice = Rgb(236, 242, 250);
        var cyan = Rgb(150, 200, 240);
        var c = V(0, 0.3f, 0);
        var r = V(0.09f, 0.11f, 0.08f);
        b.Ell(Body, c, r, ice);
        int head = b.Head(c + V(0, 0.09f, 0));
        SnowSwirl(b, head, c + V(0, 0.1f, 0), 0.085f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.5f;
            var d = V(MathF.Sin(a), -0.35f, MathF.Cos(a));
            var at = Out(c, r, default, d);
            Gem(b, Body, at, d, 0.024f, cyan);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.032f * s, c.Y + 0.035f);
            b.Eye(Body, at, Outward(c, r, at), 0.022f, Rgb(70, 110, 210));
        });
        var m = On(c, r, 0, c.Y - 0.01f);
        b.Mark(Body, m, Outward(c, r, m), 0.035f, 0.016f, Rgb(70, 100, 200), MarkShape.Smile);
        int tail = b.Tail(c + V(0, -0.09f, 0));
        b.Spike(tail, c + V(0, -0.08f, 0), c + V(0.01f, -0.3f, 0.02f), 0.06f, cyan, mat: Glow);
        return Lift(b);
    }

    /// <summary>Vanilluxe: two scoops of ice cream side by side on one great cone of blue ice, a cloud of snow over them, two faces smiling, crystals of ice round them and an icicle held out.</summary>
    private static PokeBuilder Vanilluxe()
    {
        var b = new PokeBuilder("Vanilluxe", 1f, BodyPlan.Floating, V(0, 0.36f, 0)) { Coat = Fur }.Hover();
        var ice = Rgb(236, 242, 250);
        var cyan = Rgb(150, 200, 240);
        var face = Rgb(120, 90, 200);
        var cone = V(0, 0.3f, 0);
        b.Ell(Body, cone, V(0.15f, 0.07f, 0.1f), ice);
        int tail = b.Tail(cone + V(0, -0.05f, 0));
        b.Spike(tail, cone + V(-0.03f, -0.03f, 0), cone + V(-0.05f, -0.28f, 0.02f), 0.07f, cyan, mat: Glow);
        b.Spike(tail, cone + V(0.06f, -0.03f, 0), cone + V(0.08f, -0.2f, 0.03f), 0.04f, cyan, mat: Glow);
        // Two heads, each with its own face
        foreach (var (x, name) in new[] { (-0.08f, "head"), (0.08f, "head2") })
        {
            var c = V(x, 0.42f, 0.01f);
            var r = V(0.08f, 0.085f, 0.075f);
            int head = name == "head" ? b.Head(c + V(0, -0.06f, 0)) : b.Part(name, Body, c + V(0, -0.06f, 0), PokeRole.Head, 0.5f);
            b.Ell(head, c, r, ice);
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, x + 0.028f * s, c.Y + 0.02f);
                b.Mark(head, at, Outward(c, r, at), 0.018f, 0.022f, face, MarkShape.Ring);
                b.Eye(head, at, Outward(c, r, at), 0.012f, face);
            });
            var m = On(c, r, x, c.Y - 0.025f);
            b.Mark(head, m, Outward(c, r, m), 0.03f, 0.012f, face, MarkShape.Smile);
            Gem(b, head, Out(c, r, default, V(x * 10f, -0.3f, 0.6f)), V(x * 10f, -0.3f, 0.6f), 0.022f, cyan);
        }
        // A cloud of snow over both heads, and an icicle held out
        Lumps(b, Body, V(0, 0.52f, -0.02f), V(0.16f, 0.04f, 0.08f), 12, 0.04f, ice, Rgb(250, 252, 255), 10f);
        b.Ell(Body, V(0, 0.52f, -0.02f), V(0.14f, 0.035f, 0.07f), ice);
        int arm = b.Arm(1f, V(0.15f, 0.4f, 0));
        b.Spike(arm, V(0.14f, 0.4f, 0.01f), V(0.28f, 0.48f, 0.04f), 0.022f, cyan, mat: Glow);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Deerling line

    private enum Season { Spring, Summer, Autumn, Winter }

    /// <summary>
    /// Deerling in its four seasons: a fawn, its back and hood coloured by the season (pink in spring, green in summer,
    /// orange in autumn, brown in winter) over a cream belly and face, edged in yellow and spotted yellow, big eyes,
    /// tall ears and a yellow flower on its head.
    /// </summary>
    private static PokeBuilder DeerlingBuild(Season season)
    {
        var b = new PokeBuilder(season == Season.Spring ? "Deerling" : "Deerling-" + season, 0.6f, BodyPlan.Quadruped, V(0, 0.16f, 0)) { Coat = Fur };
        var coat = season switch { Season.Summer => Rgb(80, 170, 70), Season.Autumn => Rgb(236, 140, 70), Season.Winter => Rgb(156, 126, 116), _ => Rgb(236, 150, 170) };
        var cream = Rgb(246, 236, 206);
        var yellow = Rgb(246, 206, 70);
        var hoof = Rgb(70, 60, 60);
        foreach (var (z, front) in new[] { (0.06f, true), (-0.08f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.035f * s, 0.13f, z), front);
                b.Limb(leg, V(0.035f * s, 0.14f, z), V(0.04f * s, 0.02f, z + 0.01f), 0.018f, 0.013f, cream);
                b.Ell(leg, V(0.04f * s, 0.012f, z + 0.015f), V(0.014f, 0.012f, 0.018f), hoof);
            });
        var bc = V(0, 0.16f, -0.01f);
        b.Ell(Body, bc, V(0.05f, 0.055f, 0.1f), cream);
        b.PaintEll(Body, bc + V(0, 0.035f, 0), V(0.075f, 0.06f, 0.125f), yellow);
        b.PaintEll(Body, bc + V(0, 0.045f, 0), V(0.07f, 0.055f, 0.12f), coat);
        foreach (var d in new[] { V(0.6f, 0.6f, 0.4f), V(-0.6f, 0.6f, -0.3f), V(0.5f, 0.7f, -0.5f), V(-0.5f, 0.75f, 0.45f) })
        {
            var at = Out(bc, V(0.05f, 0.055f, 0.1f), default, d);
            b.Mark(Body, at, Outward(bc, V(0.05f, 0.055f, 0.1f), at), 0.008f, 0.008f, yellow);
        }
        int tail = b.Tail(bc + V(0, 0.02f, -0.09f));
        b.Spike(tail, bc + V(0, 0.02f, -0.09f), bc + V(0, 0.07f, -0.13f), 0.02f, coat, 0.6f);
        b.Limb(Body, bc + V(0, 0.03f, 0.07f), V(0, 0.27f, 0.09f), 0.03f, 0.026f, cream);
        int head = b.Head(V(0, 0.27f, 0.09f));
        var c = V(0, 0.3f, 0.1f);
        var r = V(0.058f, 0.055f, 0.056f);
        b.Ell(head, c, r, cream);
        b.PaintEll(head, c + V(0, 0.028f, -0.018f), V(0.058f, 0.04f, 0.052f), coat);
        b.Ell(head, c + V(0, -0.015f, 0.045f), V(0.025f, 0.022f, 0.03f), cream);
        b.Ell(head, c + V(0, -0.008f, 0.075f), V(0.007f, 0.006f, 0.005f), hoof, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.03f, -0.01f));
            var root = c + V(0.03f * s, 0.03f, -0.01f);
            var tip = c + V(0.11f * s, 0.075f, -0.03f);
            Frond(b, ear, root, tip, 0.026f, coat, V(0, 0.3f, 1f), 0.3f);
            b.PaintEll(ear, Vector3.Lerp(root, tip, 0.55f) + V(0, 0, 0.006f), V(0.03f, 0.014f, 0.01f), yellow, Euler(tip - root));
            var at = On(c, r, 0.027f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.017f, Rgb(70, 50, 40));
        });
        FlowerHead(b, head, c + V(0.02f, 0.055f, 0), V(0.3f, 1f, 0.3f), 5, 0.035f, 0.018f, yellow, Rgb(250, 240, 170), 0.01f);
        return b;
    }

    private static PokeBuilder Deerling() => DeerlingBuild(Season.Spring);

    /// <summary>
    /// Sawsbuck in its four seasons: a stag, brown with a cream belly and a tuft at its chest, long legs and a sharp
    /// face, its branching antlers wearing the season: pink blossom in spring, a great crown of green leaves in summer,
    /// red leaves hanging in autumn, and bare white antlers with a white ruff and snowy legs in winter.
    /// </summary>
    private static PokeBuilder SawsbuckBuild(Season season)
    {
        var b = new PokeBuilder(season == Season.Spring ? "Sawsbuck" : "Sawsbuck-" + season, 1f, BodyPlan.Quadruped, V(0, 0.5f, -0.02f)) { Coat = Fur };
        var brown = Rgb(130, 90, 60);
        var cream = Rgb(232, 206, 150);
        var hoof = Rgb(60, 46, 40);
        var antler = season == Season.Winter ? Rgb(236, 236, 230) : Rgb(80, 60, 50);
        var snow = Rgb(246, 248, 252);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.15f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.46f, z), front);
                var knee = V(0.065f * s, 0.26f, z + (front ? 0.01f : -0.03f));
                var foot = V(0.065f * s, 0.03f, z + 0.01f);
                b.Limb(leg, V(0.06f * s, 0.47f, z), knee, 0.035f, 0.022f, brown);
                b.Limb(leg, knee, foot, 0.02f, 0.017f, season == Season.Winter ? snow : cream);
                b.Ell(leg, foot + V(0, -0.012f, 0.008f), V(0.02f, 0.018f, 0.024f), hoof);
            });
        var bc = V(0, 0.5f, -0.02f);
        b.Ell(Body, bc, V(0.1f, 0.1f, 0.2f), brown);
        b.PaintEll(Body, bc + V(0, -0.07f, 0), V(0.09f, 0.05f, 0.18f), cream);
        if (season == Season.Winter)
            b.Torus(Body, V(0, 0.6f, 0.15f), 0.07f, 0.04f, snow, V(-40f, 0, 0), blend: 0.015f);
        else
            FurTufts(b, Body, V(0, 0.54f, 0.16f), V(0.05f, 0.05f, 0.04f), 8, 0.04f, 0.016f, cream, 1.1f, -0.8f, 0.5f);
        int tail = b.Tail(bc + V(0, 0.04f, -0.19f));
        b.Spike(tail, bc + V(0, 0.04f, -0.18f), bc + V(0, 0.1f, -0.24f), 0.025f, brown, 0.6f);
        b.Tube(Body, new[] { bc + V(0, 0.04f, 0.14f), V(0, 0.66f, 0.2f), V(0, 0.74f, 0.24f) }, 0.05f, 0.035f, brown, blend: 0.012f);
        int head = b.Head(V(0, 0.74f, 0.24f));
        var c = V(0, 0.78f, 0.27f);
        var r = V(0.045f, 0.045f, 0.06f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.02f, 0.06f), V(0.026f, 0.024f, 0.04f), brown);
        b.PaintEll(head, c + V(0, -0.025f, 0.08f), V(0.025f, 0.018f, 0.03f), cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.035f * s, 0.02f, -0.02f));
            Blade(b, ear, c + V(0.035f * s, 0.02f, -0.02f), c + V(0.1f * s, 0.05f, -0.04f), 0.025f, brown, V(0, 0.3f, 1f), 0.3f);
            var at = On(c, r, 0.03f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(60, 40, 30), glare: true);
            // Branching antlers, and what the season hangs on them
            var a0 = c + V(0.02f * s, 0.035f, -0.02f);
            var a1 = c + V(0.07f * s, 0.14f, -0.05f);
            var a2 = c + V(0.12f * s, 0.25f, -0.06f);
            b.Tube(head, Smooth(2, a0, a1, a2), 0.014f, 0.009f, antler, Shell, 0f);
            b.Limb(head, a1, a1 + V(0.0f, 0.08f, 0.05f), 0.009f, 0.006f, antler, Shell, 0.006f);
            b.Limb(head, Vector3.Lerp(a1, a2, 0.6f), Vector3.Lerp(a1, a2, 0.6f) + V(0.08f * s, 0.04f, 0.02f), 0.008f, 0.006f, antler, Shell, 0.006f);
            switch (season)
            {
                case Season.Spring:
                    foreach (var at2 in new[] { a2, a1 + V(0, 0.08f, 0.05f), Vector3.Lerp(a1, a2, 0.6f) + V(0.08f * s, 0.04f, 0.02f) })
                        FlowerHead(b, head, at2, V(0.3f * s, 0.6f, 1f), 5, 0.03f, 0.016f, Rgb(240, 160, 190), Rgb(250, 220, 120), 0.008f);
                    break;
                case Season.Summer:
                    var crown = a2 + V(0.06f * s, 0.02f, 0);
                    b.Ell(head, crown, V(0.15f, 0.05f, 0.12f), Rgb(60, 140, 70), mat: Leaf);
                    Lumps(b, head, crown, V(0.15f, 0.05f, 0.12f), 14, 0.045f, Rgb(60, 140, 70), Rgb(90, 170, 80), 10f);
                    break;
                case Season.Autumn:
                    foreach (var o in new[] { V(0.04f * s, -0.02f, 0), V(0.12f * s, -0.06f, 0.02f), V(0.08f * s, -0.12f, -0.02f), V(0.14f * s, -0.14f, 0.02f) })
                        b.Ell(head, a2 + o, V(0.05f, 0.06f, 0.04f), Rgb(200, 90, 80), mat: Leaf, blend: 0.03f);
                    Lumps(b, head, a2 + V(0.08f * s, -0.07f, 0), V(0.08f, 0.1f, 0.05f), 10, 0.035f, Rgb(200, 90, 80), Rgb(220, 120, 90), 10f);
                    break;
            }
        });
        return b;
    }

    private static PokeBuilder Sawsbuck() => SawsbuckBuild(Season.Spring);

    // ------------------------------------------------------------------ Emolga

    /// <summary>Emolga: a little flying squirrel, white with a black head and ears lined in yellow, yellow cheeks, a yellow cape of skin from its arms to its legs and a black forked tail.</summary>
    private static PokeBuilder Emolga()
    {
        var b = new PokeBuilder("Emolga", 0.5f, BodyPlan.Biped, V(0, 0.13f, 0)) { Coat = Fur };
        var white = Rgb(246, 246, 248);
        var black = Rgb(50, 50, 56);
        var yellow = Rgb(250, 214, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.06f, 0));
            b.Limb(leg, V(0.03f * s, 0.07f, 0), V(0.035f * s, 0.02f, 0.01f), 0.016f, 0.014f, white);
            b.Ell(leg, V(0.036f * s, 0.013f, 0.018f), V(0.016f, 0.013f, 0.024f), white);
            var shoulder = V(0.04f * s, 0.17f, 0);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.11f * s, 0.2f, 0.02f);
            b.Limb(arm, shoulder, hand, 0.012f, 0.011f, white);
            Frond(b, arm, V(0.05f * s, 0.17f, -0.01f), V(0.12f * s, 0.07f, -0.01f), 0.055f, yellow, V(0, 0, 1f), 0.15f);
        });
        b.Ell(Body, V(0, 0.13f, 0), V(0.05f, 0.06f, 0.045f), white);
        int tail = b.Tail(V(0, 0.1f, -0.04f));
        var tp = Smooth(2, V(0, 0.1f, -0.04f), V(0, 0.12f, -0.1f), V(0, 0.17f, -0.14f));
        b.Tube(tail, tp, 0.016f, 0.02f, black, blend: 0f);
        PokeBuilder.Both(s => Blade(b, tail, tp[^1], tp[^1] + V(0.06f * s, 0.06f, -0.06f), 0.03f, black, V(0, 0.3f, 1f), 0.3f));
        int head = b.Head(V(0, 0.19f, 0.01f));
        var c = V(0, 0.245f, 0.015f);
        var r = V(0.068f, 0.06f, 0.06f);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0, 0.035f, -0.02f), V(0.075f, 0.045f, 0.065f), black);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.045f * s, 0.045f, -0.01f));
            var ec = c + V(0.07f * s, 0.085f, -0.01f);
            b.Ell(ear, ec, V(0.035f, 0.04f, 0.014f), black, V(0, 0, -25f * s));
            b.PaintEll(ear, ec + V(0, 0, 0.01f), V(0.022f, 0.026f, 0.01f), yellow, V(0, 0, -25f * s));
            var cheek = On(c, r, 0.05f * s, c.Y - 0.02f);
            b.PaintEll(head, cheek, V(0.02f, 0.016f, 0.02f), yellow);
            var at = On(c, r, 0.028f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(40, 30, 30));
        });
        var m = On(c, r, 0, c.Y - 0.026f);
        b.Mark(head, m, Outward(c, r, m), 0.014f, 0.01f, Rgb(220, 110, 120), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Karrablast and Escavalier

    /// <summary>Karrablast: a little blue beetle standing up, a yellow belly in bands, a black face with yellow eyes and a gaping mouth, a horn forked at its tip and small dark claws.</summary>
    private static PokeBuilder Karrablast()
    {
        var b = new PokeBuilder("Karrablast", 0.5f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Shell };
        var blue = Rgb(60, 110, 200);
        var black = Rgb(40, 40, 50);
        var yellow = Rgb(240, 196, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.06f, 0));
            b.Limb(leg, V(0.04f * s, 0.07f, 0), V(0.045f * s, 0.025f, 0.01f), 0.02f, 0.018f, black);
            b.Ell(leg, V(0.046f * s, 0.018f, 0.02f), V(0.024f, 0.018f, 0.03f), black);
            int arm = b.Arm(s, V(0.07f * s, 0.15f, 0));
            b.Limb(arm, V(0.07f * s, 0.15f, 0), V(0.1f * s, 0.12f, 0.02f), 0.018f, 0.016f, black);
            b.Ell(arm, V(0.105f * s, 0.115f, 0.025f), V(0.02f, 0.018f, 0.02f), black);
        });
        var bc = V(0, 0.15f, 0);
        var br = V(0.08f, 0.1f, 0.07f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, bc + V(0, -0.04f, 0.04f), V(0.06f, 0.06f, 0.04f), yellow);
        foreach (float y in new[] { 0.09f, 0.115f })
            b.PaintEll(Body, V(0, y, 0.05f), V(0.07f, 0.003f, 0.04f), Rgb(170, 130, 40), soft: 0.003f);
        b.PaintEll(Body, bc + V(0, 0.045f, 0.05f), V(0.065f, 0.04f, 0.03f), black);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.032f * s, bc.Y + 0.055f);
            b.Eye(Body, at, Outward(bc, br, at), 0.016f, sclera: true, white: yellow, pupil: Rgb(30, 30, 30), glare: true);
        });
        var g = On(bc, br, 0, bc.Y + 0.015f);
        Grin(b, Body, g, V(0.03f, 0.02f, 0.02f), Rgb(220, 120, 130));
        PokeBuilder.Both(s => b.Spike(Body, g + V(0.018f * s, 0.016f, -0.006f), g + V(0.016f * s, 0.003f, -0.004f), 0.005f, White, mat: Shell, blend: 0.003f));
        // A horn rising from its head, forked at the top
        int head = b.Head(bc + V(0, 0.09f, 0));
        b.Limb(head, bc + V(0, 0.08f, -0.01f), bc + V(0, 0.16f, -0.01f), 0.022f, 0.016f, blue);
        PokeBuilder.Both(s => b.Spike(head, bc + V(0, 0.16f, -0.01f), bc + V(0.035f * s, 0.2f, 0), 0.014f, blue));
        return b;
    }

    /// <summary>Escavalier: a knight floating in armour, a helmet with a visor and a great red plume curling back, yellow eyes behind the visor, two lances banded red for arms and shells coiled like a snail's at its shoulders.</summary>
    private static PokeBuilder Escavalier()
    {
        var b = new PokeBuilder("Escavalier", 0.9f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Metal }.Hover();
        var steel = Rgb(170, 172, 180);
        var dark = Rgb(80, 82, 92);
        var red = Rgb(210, 70, 80);
        var bc = V(0, 0.36f, 0);
        var br = V(0.09f, 0.12f, 0.08f);
        b.Ell(Body, bc, br, steel);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.06f), V(0.05f, 0.07f, 0.03f), Rgb(70, 90, 150));
        b.PaintEll(Body, bc + V(0, -0.06f, 0.06f), V(0.05f, 0.025f, 0.03f), Rgb(240, 196, 70));
        b.Spike(Body, bc + V(0, -0.08f, 0), bc + V(0, -0.22f, 0.02f), 0.05f, steel);
        // Shells coiled like a snail's at its shoulders, and lances for arms
        PokeBuilder.Both(s =>
        {
            var sc = bc + V(0.11f * s, 0.07f, -0.01f);
            b.Ell(Body, sc, V(0.065f, 0.07f, 0.06f), steel);
            b.PaintTorus(Body, sc + V(0.03f * s, 0, 0), 0.045f, 0.006f, dark, V(0, 0, 90f));
            b.PaintTorus(Body, sc + V(0.05f * s, 0, 0), 0.025f, 0.006f, dark, V(0, 0, 90f));
            var shoulder = bc + V(0.09f * s, 0.0f, 0.04f);
            int arm = b.Arm(s, shoulder);
            var hand = bc + V(0.14f * s, -0.02f, 0.1f);
            b.Limb(arm, shoulder, hand, 0.03f, 0.03f, steel);
            b.Torus(arm, hand, 0.035f, 0.012f, steel, V(70f, 0, 0), mat: Metal, blend: 0.006f);
            var tip = hand + V(0.04f * s, -0.12f, 0.3f);
            b.Spike(arm, hand, tip, 0.035f, steel);
            for (int k = 1; k <= 3; k++)
                b.PaintTorus(arm, Vector3.Lerp(hand, tip, 0.2f * k), 0.035f * (1f - 0.2f * k * 0.9f), 0.008f, red, Euler(tip - hand));
        });
        // A helmet with a visor, and a great red plume curling back over it
        int head = b.Head(bc + V(0, 0.12f, 0.02f));
        var c = bc + V(0, 0.17f, 0.03f);
        var r = V(0.055f, 0.06f, 0.06f);
        b.Ell(head, c, r, steel);
        b.PaintEll(head, c + V(0, -0.005f, 0.045f), V(0.045f, 0.028f, 0.03f), Rgb(40, 40, 50));
        b.PaintEll(head, c + V(0, -0.005f, 0.06f), V(0.004f, 0.03f, 0.02f), steel, soft: 0.003f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, c.Y - 0.003f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(250, 210, 60), glare: true);
        });
        Ribbon(b, head, Smooth(3, c + V(0, 0.05f, 0.02f), c + V(0, 0.11f, -0.02f), c + V(0, 0.13f, -0.1f), c + V(0, 0.07f, -0.17f), c + V(0, -0.01f, -0.16f)), 0.07f, red, V(1f, 0, 0), 0.028f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Foongus line

    /// <summary>Foongus: a little mushroom whose cap is patterned like a Poké Ball, white with red at its sides and a dark band, over a white body with two small eyes, a pink mouth and stubby arms.</summary>
    private static PokeBuilder Foongus()
    {
        var b = new PokeBuilder("Foongus", 0.4f, BodyPlan.Biped, V(0, 0.07f, 0)) { Coat = Fur };
        var white = Rgb(240, 234, 226);
        var red = Rgb(214, 80, 90);
        var band = Rgb(80, 60, 60);
        var bc = V(0, 0.07f, 0);
        var br = V(0.06f, 0.07f, 0.055f);
        b.Ell(Body, bc, br, white);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.06f, 0.01f));
            b.Ell(arm, V(0.065f * s, 0.05f, 0.015f), V(0.02f, 0.016f, 0.018f), white);
            var at = On(bc, br, 0.02f * s, bc.Y + 0.02f);
            b.Eye(Body, at, Outward(bc, br, at), 0.009f, Rgb(30, 30, 30));
        });
        b.Ell(Body, On(bc, br, 0, bc.Y - 0.012f), V(0.015f, 0.013f, 0.01f), Rgb(236, 150, 170), blend: 0.006f);
        int head = b.Head(bc + V(0, 0.06f, 0));
        var c = V(0, 0.17f, 0);
        b.Ell(head, c, V(0.12f, 0.065f, 0.11f), white);
        PokeBuilder.Both(s => b.PaintEll(head, c + V(0.1f * s, 0, 0), V(0.06f, 0.08f, 0.12f), red));
        b.PaintTorus(head, c + V(0, 0.01f, 0), 0.108f, 0.008f, band, sz: 0.92f);
        b.Mark(head, c + V(0, 0.065f, 0), V(0, 1f, 0), 0.035f, 0.035f, band, MarkShape.Ring);
        return b;
    }

    /// <summary>Amoonguss: a stout mushroom, a broad cap red on top over a gray-green body, sleepy eyes, a round pink mouth, and two arms holding up caps patterned like Poké Balls.</summary>
    private static PokeBuilder Amoonguss()
    {
        var b = new PokeBuilder("Amoonguss", 0.75f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = Fur };
        var stalk = Rgb(150, 170, 150);
        var cap = Rgb(170, 176, 170);
        var red = Rgb(214, 80, 100);
        var band = Rgb(70, 60, 60);
        var bc = V(0, 0.17f, 0);
        var br = V(0.1f, 0.15f, 0.09f);
        b.Ell(Body, bc, br, stalk);
        b.PaintTorus(Body, V(0, 0.06f, 0), 0.085f, 0.014f, band, sz: 0.9f);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.035f * s, bc.Y + 0.06f);
            b.Eye(Body, at, Outward(bc, br, at), 0.012f, Rgb(40, 40, 40), glare: true);
            // Arms out to each side, a cap like a Poké Ball in each hand
            int arm = b.Arm(s, V(0.08f * s, 0.18f, 0));
            var ball = V(0.21f * s, 0.14f, 0.03f);
            b.Limb(arm, V(0.08f * s, 0.18f, 0), ball, 0.022f, 0.02f, stalk);
            var bcap = V(0.085f, 0.08f, 0.08f);
            b.Ell(arm, ball, bcap, cap);
            b.PaintEll(arm, ball + V(0, 0.05f, 0), V(0.1f, 0.06f, 0.1f), red);
            b.PaintTorus(arm, ball, 0.082f, 0.007f, band, V(0, 0, 90f));
            var face = Out(ball, bcap, default, V(s, 0, 0.3f));
            b.Mark(arm, face, Outward(ball, bcap, face), 0.022f, 0.022f, band, MarkShape.Ring);
        });
        b.Ell(Body, On(bc, br, 0, bc.Y + 0.025f), V(0.02f, 0.018f, 0.012f), Rgb(236, 150, 170), blend: 0.006f);
        int head = b.Head(bc + V(0, 0.13f, 0));
        var c = V(0, 0.34f, 0);
        b.Ell(head, c, V(0.18f, 0.07f, 0.16f), cap);
        b.PaintEll(head, c + V(0, 0.06f, 0.02f), V(0.16f, 0.05f, 0.14f), red);
        b.PaintTorus(head, c + V(0, -0.005f, 0), 0.172f, 0.009f, band, sz: 0.9f);
        return b;
    }

    // ------------------------------------------------------------------ Frillish line

    /// <summary>Five broad tentacles hanging from <paramref name="root"/>, curling out as they fall, each on a bone of its own.</summary>
    private static void Tentacles(PokeBuilder b, Vector3 root, float reach, float drop, float width, Color color, int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / count + MathF.PI / 2f;
            var d = V(MathF.Cos(a), 0, MathF.Sin(a));
            int bone = b.Part("tentacle" + i, Body, root + d * reach * 0.2f, PokeRole.Tail, i * 0.4f);
            var path = Smooth(3, root + d * reach * 0.15f, root + d * reach * 0.6f + V(0, -drop * 0.3f, 0), root + d * reach + V(0, -drop * 0.7f, 0), root + d * reach * 0.85f + V(0, -drop, 0));
            Ribbon(b, bone, path, width, color, d, width * 0.25f);
        }
    }

    /// <summary>
    /// Frillish, male and female: a jellyfish with a round head under a little crown, eyes ringed in white, a collar of
    /// frills and five broad tentacles; the male blue and frowning, the female pink and smiling.
    /// </summary>
    private static PokeBuilder FrillishBuild(bool female)
    {
        var b = new PokeBuilder(female ? "Frillish-Female" : "Frillish", 0.85f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var coat = female ? Rgb(244, 180, 206) : Rgb(150, 206, 236);
        var light = female ? Rgb(250, 220, 232) : Rgb(214, 236, 246);
        var c = V(0, 0.42f, 0);
        var r = V(0.12f, 0.12f, 0.11f);
        b.Ell(Body, c, r, coat);
        b.Torus(Body, c + V(0, -0.11f, 0), 0.065f, 0.024f, light, blend: 0.012f);
        Tentacles(b, c + V(0, -0.12f, 0), 0.2f, 0.26f, 0.075f, light);
        int head = b.Head(c + V(0, 0.1f, 0));
        foreach (float x in new[] { -0.022f, 0, 0.022f })
            b.Spike(head, c + V(x, 0.1f, -0.01f), c + V(x * 1.6f, 0.155f, -0.01f), 0.013f, light);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, c.Y + 0.005f);
            b.Mark(Body, at, Outward(c, r, at), 0.022f, 0.022f, female ? Rgb(90, 120, 220) : Rgb(220, 60, 80), MarkShape.Ring);
            b.Eye(Body, at, Outward(c, r, at), 0.012f, sclera: true, pupil: female ? Rgb(60, 80, 200) : Rgb(200, 40, 60));
        });
        var m = On(c, r, 0, c.Y - 0.04f);
        b.Mark(Body, m, Outward(c, r, m), 0.018f, 0.008f, Rgb(80, 60, 80), MarkShape.Smile, female ? 0f : 180f);
        return Lift(b);
    }

    private static PokeBuilder Frillish() => FrillishBuild(false);

    /// <summary>
    /// Jellicent, male and female: a great jellyfish, its domed head under a little crown, a thick frilled ruff round
    /// its face like a moustache, eyes ringed in white and broad tentacles below; the male blue with a pale ruff, the
    /// female pink with a white ruff, red lips and lashes.
    /// </summary>
    private static PokeBuilder JellicentBuild(bool female)
    {
        var b = new PokeBuilder(female ? "Jellicent-Female" : "Jellicent", 1.1f, BodyPlan.Floating, V(0, 0.5f, 0)) { Coat = Fur }.Hover();
        var coat = female ? Rgb(244, 170, 200) : Rgb(100, 170, 230);
        var ruff = female ? Rgb(250, 240, 246) : Rgb(200, 226, 246);
        var c = V(0, 0.52f, 0);
        var r = V(0.2f, 0.15f, 0.17f);
        b.Ell(Body, c, r, coat);
        b.Torus(Body, c + V(0, -0.1f, 0.02f), 0.17f, 0.065f, ruff, sz: 0.88f, blend: 0.02f);
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            b.Ell(Body, c + V(MathF.Cos(a) * 0.2f, -0.12f, MathF.Sin(a) * 0.17f + 0.02f), V(0.06f, 0.05f, 0.05f), ruff, blend: 0.03f);
        }
        Tentacles(b, c + V(0, -0.15f, 0), 0.3f, 0.3f, 0.1f, coat);
        int head = b.Head(c + V(0, 0.12f, 0));
        foreach (float x in new[] { -0.025f, 0, 0.025f })
            b.Spike(head, c + V(x, 0.13f, -0.01f), c + V(x * 1.6f, 0.19f, -0.01f), 0.014f, ruff);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.065f * s, c.Y + 0.02f);
            b.Mark(Body, at, Outward(c, r, at), 0.024f, 0.024f, Rgb(220, 60, 80), MarkShape.Ring);
            b.Eye(Body, at, Outward(c, r, at), 0.015f, sclera: true, pupil: female ? Rgb(60, 80, 200) : Rgb(200, 40, 60));
            if (female) b.Mark(Body, at + V(0.022f * s, 0.016f, 0), Outward(c, r, at), 0.01f, 0.003f, Rgb(80, 40, 60), MarkShape.Bar, 30f * s);
        });
        if (female)
        {
            var lips = On(c, r, 0, c.Y - 0.02f);
            b.Mark(Body, lips, Outward(c, r, lips), 0.022f, 0.016f, Rgb(220, 50, 80), MarkShape.Diamond);
        }
        return Lift(b);
    }

    private static PokeBuilder Jellicent() => JellicentBuild(false);

    // ------------------------------------------------------------------ Alomomola

    /// <summary>Alomomola: a pink fish shaped like a heart, upright in the water, a fin like a ribbon swept back from its head, broad fins at its sides and below and gentle eyes with lashes.</summary>
    private static PokeBuilder Alomomola()
    {
        var b = new PokeBuilder("Alomomola", 0.95f, BodyPlan.Fish, V(0, 0.38f, 0)) { Coat = Scales }.Hover();
        var pink = Rgb(236, 160, 180);
        var deep = Rgb(210, 110, 140);
        var c = V(0, 0.4f, 0);
        var r = V(0.13f, 0.15f, 0.09f);
        b.Ell(Body, c, r, pink);
        PokeBuilder.Both(s => b.Ell(Body, c + V(0.07f * s, 0.07f, 0), V(0.1f, 0.1f, 0.085f), pink));
        b.Spike(Body, c + V(0, -0.08f, 0), c + V(0, -0.24f, 0.02f), 0.08f, pink);
        int head = b.Head(c + V(0, 0.15f, 0));
        Ribbon(b, head, Smooth(3, c + V(0, 0.13f, 0.02f), c + V(0, 0.22f, -0.01f), c + V(0, 0.23f, -0.1f), c + V(0, 0.17f, -0.17f)), 0.07f, deep, V(1f, 0, 0), 0.02f);
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, c + V(0.11f * s, -0.04f, 0), PokeRole.Fin, 0f, s);
            Frond(b, fin, c + V(0.11f * s, -0.04f, 0), c + V(0.22f * s, -0.12f, -0.03f), 0.05f, deep, V(0.3f, 1f, 0.3f), 0.2f);
            var at = On(c, r, 0.045f * s, c.Y + 0.03f);
            b.Eye(Body, at, Outward(c, r, at), 0.016f, Rgb(120, 90, 60));
            b.Mark(Body, at + V(0, 0.022f, 0), Outward(c, r, at), 0.016f, 0.004f, Rgb(80, 50, 60), MarkShape.Bar);
        });
        int tail = b.Tail(c + V(0, -0.2f, 0));
        Frond(b, tail, c + V(0, -0.22f, 0.01f), c + V(0, -0.34f, -0.06f), 0.06f, deep, V(0, 0, 1f), 0.2f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Joltik line

    /// <summary>Joltik: a tiny fuzzy yellow tick, two big blue eyes with two small ones between them that never blink, and four short legs tipped in blue.</summary>
    private static PokeBuilder Joltik()
    {
        var b = new PokeBuilder("Joltik", 0.35f, BodyPlan.Quadruped, V(0, 0.07f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 214, 90);
        var blue = Rgb(60, 90, 180);
        var c = V(0, 0.07f, 0);
        var r = V(0.07f, 0.055f, 0.06f);
        b.Ell(Body, c, r, yellow);
        FurTufts(b, Body, c, r, 26, 0.025f, 0.012f, yellow, 0.6f, -0.5f, 0.2f);
        foreach (var (z, front) in new[] { (0.03f, true), (-0.03f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, c + V(0.05f * s, -0.01f, z), front);
                var foot = c + V(0.1f * s, -0.065f, z * 1.6f);
                b.Limb(leg, c + V(0.05f * s, -0.01f, z), foot, 0.009f, 0.008f, yellow);
                b.Ell(leg, foot, V(0.01f, 0.007f, 0.01f), blue);
            });
        int head = b.Head(c + V(0, 0.01f, 0.04f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.005f);
            b.Eye(Body, at, Outward(c, r, at), 0.016f, blue);
            var small = On(c, r, 0.012f * s, c.Y + 0.012f);
            b.Mark(Body, small, Outward(c, r, small), 0.007f, 0.007f, blue);
        });
        b.Ell(head, c + V(0, -0.015f, 0.05f), V(0.02f, 0.012f, 0.012f), yellow);
        return b;
    }

    /// <summary>Galvantula: a yellow spider, its fuzzy head yellow over lavender, two big blue eyes and four small ones on its brow that never blink, an abdomen striped lavender and yellow and four long legs with blue-tipped tufts.</summary>
    private static PokeBuilder Galvantula()
    {
        var b = new PokeBuilder("Galvantula", 0.85f, BodyPlan.Quadruped, V(0, 0.16f, -0.04f)) { Coat = Fur };
        var yellow = Rgb(246, 214, 90);
        var lavender = Rgb(160, 150, 210);
        var blue = Rgb(70, 80, 170);
        // The abdomen behind, striped lavender and yellow, fuzz at its sides
        var ac = V(0, 0.2f, -0.12f);
        var ar = V(0.12f, 0.1f, 0.13f);
        b.Ell(Body, ac, ar, lavender);
        foreach (float z in new[] { -0.17f, -0.1f, -0.03f })
            b.PaintEll(Body, V(0, 0.3f, z), V(0.13f, 0.06f, 0.018f), yellow);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
            {
                var root = ac + V(0.1f * s, 0.03f, -0.06f + i * 0.05f);
                Blade(b, Body, root, root + V(0.06f * s, 0.07f, -0.02f), 0.025f, blue, V(s, 0, 0.3f), 0.3f);
            }
        });
        // Four long legs, bent high and tufted at their feet
        foreach (var (z, front) in new[] { (0.06f, true), (-0.06f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.14f, z), front);
                var knee = V(0.2f * s, 0.24f, z * 2f);
                var foot = V(0.27f * s, 0.02f, z * 2.6f);
                b.Limb(leg, V(0.06f * s, 0.14f, z), knee, 0.03f, 0.026f, yellow);
                b.Limb(leg, knee, foot, 0.026f, 0.03f, yellow);
                FurTufts(b, leg, foot + V(0, 0.02f, 0), V(0.03f, 0.025f, 0.03f), 8, 0.03f, 0.012f, blue, 1.1f, -0.7f, 0.3f);
            });
        // The head in front: two big eyes and four small ones in a square on its brow
        int head = b.Head(V(0, 0.15f, 0.04f));
        var c = V(0, 0.15f, 0.07f);
        var r = V(0.09f, 0.07f, 0.08f);
        b.Ell(head, c, r, yellow);
        b.PaintEll(head, c + V(0, -0.045f, 0), V(0.1f, 0.04f, 0.09f), lavender);
        FurTufts(b, head, c + V(0, 0.01f, -0.02f), V(0.08f, 0.06f, 0.06f), 14, 0.035f, 0.014f, yellow, 0.3f, -0.3f, 0.2f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(60, 110, 220));
            foreach (float dy in new[] { 0.025f, 0.045f })
            {
                var small = On(c, r, 0.012f * s, c.Y + dy);
                b.Mark(head, small, Outward(c, r, small), 0.007f, 0.007f, Rgb(60, 110, 220));
            }
            b.Spike(head, c + V(0.02f * s, -0.04f, 0.06f), c + V(0.015f * s, -0.07f, 0.08f), 0.01f, blue);
        });
        return b;
    }

    // ------------------------------------------------------------------ Ferroseed line

    /// <summary>Ferroseed: a seed of iron, a gray ball banded in black, green spikes all round it and two orange eyes in green patches on its front.</summary>
    private static PokeBuilder Ferroseed()
    {
        var b = new PokeBuilder("Ferroseed", 0.6f, BodyPlan.Floating, V(0, 0.16f, 0)) { Coat = Metal };
        var gray = Rgb(150, 152, 158);
        var green = Rgb(70, 160, 100);
        var c = V(0, 0.16f, 0);
        var r = V(0.15f, 0.16f, 0.14f);
        b.Ell(Body, c, r, gray);
        foreach (float y in new[] { -0.06f, 0.07f })
            b.PaintTorus(Body, c + V(0, y, 0), 0.15f * MathF.Sqrt(1f - y * y / 0.0256f), 0.008f, Rgb(50, 50, 56), sz: 0.93f);
        for (int i = 0; i < 12; i++)
        {
            float u = -0.3f + 1.2f * (i + 0.5f) / 12f, ring = MathF.Sqrt(1f - MathF.Min(u * u, 1f)), a = i * 2.4f;
            var d = Vector3.Normalize(V(MathF.Sin(a) * ring, u, MathF.Cos(a) * ring));
            if (d.Z > 0.5f && MathF.Abs(d.Y - 0.05f) < 0.45f) continue;
            var at = Out(c, r, default, d);
            b.Spike(Body, at - d * 0.02f, at + d * 0.06f, 0.028f, green, mat: Shell);
        }
        foreach (var d in new[] { V(0.5f, -0.5f, 0.7f), V(-0.4f, 0.55f, 0.75f), V(-0.6f, -0.4f, 0.7f) })
        {
            var at = Out(c, r, default, d);
            b.Mark(Body, at, Outward(c, r, at), 0.01f, 0.008f, Rgb(50, 50, 56), MarkShape.Diamond);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, c.Y + 0.015f);
            b.PaintEll(Body, at, V(0.035f, 0.025f, 0.03f), green);
            b.Eye(Body, at, Outward(c, r, at), 0.015f, Rgb(244, 150, 50), glare: true);
        });
        return b;
    }

    /// <summary>Ferrothorn: a dome of iron spiked round its rim, two orange eyes in green patches, and three green vines from its top, each ending in a pod of green spikes.</summary>
    private static PokeBuilder Ferrothorn()
    {
        var b = new PokeBuilder("Ferrothorn", 1f, BodyPlan.Floating, V(0, 0.12f, 0)) { Coat = Metal };
        var gray = Rgb(150, 152, 158);
        var green = Rgb(70, 160, 100);
        var c = V(0, 0.12f, 0);
        var r = V(0.2f, 0.12f, 0.18f);
        b.Ell(Body, c, r, gray);
        b.PaintTorus(Body, c + V(0, 0.02f, 0), 0.198f, 0.008f, Rgb(50, 50, 56), sz: 0.9f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.2f;
            var d = Vector3.Normalize(V(MathF.Sin(a), 0.1f, MathF.Cos(a)));
            var at = Out(c, r, default, d);
            b.Spike(Body, at - d * 0.02f, at + d * 0.06f + V(0, -0.02f, 0), 0.026f, gray);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.06f * s, c.Y + 0.05f);
            b.PaintEll(Body, at, V(0.04f, 0.025f, 0.035f), green);
            b.Eye(Body, at, Outward(c, r, at), 0.016f, Rgb(244, 150, 50), glare: true);
        });
        // Three vines arching up from its top, a pod of spikes at the end of each
        int head = b.Head(c + V(0, 0.1f, 0));
        b.Ell(head, c + V(0, 0.13f, 0), V(0.05f, 0.035f, 0.05f), green, mat: Leaf);
        for (int i = 0; i < 3; i++)
        {
            float a = i * MathF.Tau / 3f + MathF.PI;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            int vine = b.Part("vine" + i, head, c + V(0, 0.12f, 0), PokeRole.Tail, i * 0.5f);
            var pod = c + d * 0.3f + V(0, 0.22f, 0);
            b.Tube(vine, Smooth(3, c + V(0, 0.12f, 0), c + d * 0.08f + V(0, 0.3f, 0), c + d * 0.24f + V(0, 0.32f, 0), pod), 0.014f, 0.012f, green, Leaf, 0f);
            b.Ell(vine, pod, V(0.045f, 0.035f, 0.045f), green, mat: Leaf);
            for (int k = 0; k < 4; k++)
            {
                float t = k * MathF.Tau / 4f;
                var sd = Vector3.Normalize(V(MathF.Cos(t), -0.2f, MathF.Sin(t)));
                b.Spike(vine, pod + sd * 0.03f, pod + sd * 0.07f, 0.015f, Rgb(200, 202, 210), mat: Metal);
            }
        }
        return b;
    }

    // ------------------------------------------------------------------ Klink line

    /// <summary>
    /// A gear standing upright and facing forward: a rim <paramref name="r"/> across with <paramref name="teeth"/> square
    /// teeth, a recessed face and a raised core of <paramref name="core"/>, turned <paramref name="turnDeg"/> round.
    /// </summary>
    private static void Gear(PokeBuilder b, int bone, Vector3 c, float r, int teeth, Color core, float turnDeg = 0f)
    {
        var steel = Rgb(180, 184, 194);
        b.Torus(bone, c, r * 0.8f, r * 0.2f, steel, V(90f, 0, 0), mat: Metal, blend: 0.004f);
        b.Ell(bone, c + V(0, 0, r * 0.08f), V(r * 0.72f, r * 0.72f, r * 0.08f), Rgb(110, 112, 124), mat: Metal, blend: 0.004f);
        b.Ell(bone, c + V(0, 0, r * 0.12f), V(r * 0.26f, r * 0.26f, r * 0.16f), core, mat: Shell, blend: 0.004f);
        for (int k = 0; k < teeth; k++)
        {
            float a = turnDeg + k * 360f / teeth;
            var d = V(MathF.Cos(a * Degree), MathF.Sin(a * Degree), 0);
            b.Box(bone, c + d * r * 1.04f, V(r * 0.16f, r * 0.17f, r * 0.17f), r * 0.03f, steel, V(0, 0, a - 90f), Metal, 0.004f);
        }
    }

    /// <summary>The face on a small gear of the Klink line: a round eye with a pupil on one side, a white X for the other, and a small mouth.</summary>
    private static void GearFace(PokeBuilder b, int bone, Vector3 c, float r, float side)
    {
        var face = c + V(0, 0, r * 0.15f);
        b.Eye(bone, face + V(-0.38f * r * side, 0.32f * r, 0), V(0, 0, 1f), r * 0.15f, sclera: true, pupil: Rgb(30, 30, 30));
        foreach (float roll in new[] { 45f, -45f })
            b.Mark(bone, face + V(0.38f * r * side, 0.32f * r, 0), V(0, 0, 1f), r * 0.14f, r * 0.035f, White, MarkShape.Bar, roll);
        b.Mark(bone, face + V(-0.3f * r * side, -0.4f * r, 0), V(0, 0, 1f), r * 0.06f, r * 0.07f, Rgb(40, 40, 46));
    }

    /// <summary>Klink: two gears meshed together and floating, each with a teal core and a face of one round eye, a white X and a small mouth.</summary>
    private static PokeBuilder Klink()
    {
        var b = new PokeBuilder("Klink", 0.55f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Metal }.Hover();
        var teal = Rgb(40, 160, 170);
        var a = V(-0.075f, 0.25f, 0.012f);
        Gear(b, Body, a, 0.1f, 6, teal);
        GearFace(b, Body, a, 0.1f, 1f);
        var g = V(0.085f, 0.16f, -0.012f);
        int second = b.Part("gear2", Body, g, PokeRole.Segment, 0.5f);
        Gear(b, second, g, 0.1f, 6, teal, 30f);
        GearFace(b, second, g, 0.1f, -1f);
        return Lift(b);
    }

    /// <summary>Klang: a great gear meshed with a small one, the small one's face an eye and a white X, the great one's a square and a round mark over a white frown.</summary>
    private static PokeBuilder Klang()
    {
        var b = new PokeBuilder("Klang", 0.85f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Metal }.Hover();
        var teal = Rgb(40, 160, 170);
        var big = V(0.05f, 0.28f, -0.012f);
        Gear(b, Body, big, 0.17f, 8, teal);
        var face = big + V(0, 0, 0.17f * 0.15f);
        b.Mark(Body, face + V(-0.075f, 0.06f, 0), V(0, 0, 1f), 0.014f, 0.014f, Rgb(40, 40, 46), MarkShape.Diamond, 45f);
        b.Mark(Body, face + V(0.075f, 0.06f, 0), V(0, 0, 1f), 0.014f, 0.014f, Rgb(40, 40, 46));
        b.Mark(Body, face + V(0, -0.075f, 0), V(0, 0, 1f), 0.035f, 0.016f, White, MarkShape.Smile, 180f);
        var small = V(-0.15f, 0.42f, 0.012f);
        int head = b.Head(small);
        Gear(b, head, small, 0.095f, 6, teal, 15f);
        GearFace(b, head, small, 0.095f, 1f);
        return Lift(b);
    }

    /// <summary>Klinklang: a great gear and a small one with faces, as Klang's, ringed by a spiked steel band like a halo with a red core at its front.</summary>
    private static PokeBuilder Klinklang()
    {
        var b = new PokeBuilder("Klinklang", 0.9f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Metal }.Hover();
        var teal = Rgb(40, 160, 170);
        var big = V(0.05f, 0.28f, -0.012f);
        Gear(b, Body, big, 0.16f, 8, teal);
        var face = big + V(0, 0, 0.16f * 0.15f);
        b.Mark(Body, face + V(-0.07f, 0.055f, 0), V(0, 0, 1f), 0.013f, 0.013f, Rgb(40, 40, 46), MarkShape.Diamond, 45f);
        b.Mark(Body, face + V(0.07f, 0.055f, 0), V(0, 0, 1f), 0.013f, 0.013f, Rgb(40, 40, 46));
        b.Mark(Body, face + V(0, -0.07f, 0), V(0, 0, 1f), 0.032f, 0.015f, White, MarkShape.Smile, 180f);
        var small = V(-0.14f, 0.41f, 0.012f);
        int head = b.Head(small);
        Gear(b, head, small, 0.09f, 6, teal, 15f);
        GearFace(b, head, small, 0.09f, 1f);
        // The spiked band round both, and its red core
        int ring = b.Part("ring", Body, V(0, 0.22f, 0), PokeRole.Segment, 0.3f);
        b.Torus(ring, V(0, 0.22f, 0.02f), 0.28f, 0.014f, Rgb(200, 204, 214), mat: Metal, blend: 0.006f);
        foreach (float a in new[] { 0.3f, 1.9f, 3.4f, 4.9f })
        {
            var d = V(MathF.Cos(a), 0, MathF.Sin(a));
            b.Spike(ring, V(0, 0.22f, 0.02f) + d * 0.28f, V(0, 0.22f, 0.02f) + d * 0.36f, 0.018f, Rgb(200, 204, 214), mat: Metal);
        }
        b.Ell(ring, V(0, 0.22f, 0.3f), V(0.04f, 0.025f, 0.025f), Rgb(220, 90, 100), mat: Shell, blend: 0.008f);
        b.Limb(ring, V(0, 0.22f, 0.29f), big + V(0, -0.1f, 0.03f), 0.012f, 0.012f, Rgb(200, 204, 214), Metal, 0.006f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Tynamo line

    /// <summary>Tynamo: a tiny pale eel like a larva, see-through white, a red star round its mouth, round black eyes and a yellow zigzag of light along its side.</summary>
    private static PokeBuilder Tynamo()
    {
        var b = new PokeBuilder("Tynamo", 0.4f, BodyPlan.Fish, V(0, 0.15f, 0)) { Coat = Shell }.Hover();
        var pale = Rgb(222, 232, 242);
        var path = Smooth(3, V(0, 0.16f, 0.1f), V(0, 0.15f, 0.02f), V(0, 0.14f, -0.06f), V(0.01f, 0.15f, -0.13f));
        b.Tube(Body, path, 0.048f, 0.014f, pale, blend: 0f);
        int tail = b.Tail(path[^1]);
        Frond(b, tail, path[^2], path[^1] + V(0.01f, 0.01f, -0.08f), 0.035f, pale, V(1f, 0, 0), 0.2f);
        int head = b.Head(path[0]);
        var c = path[0] + V(0, 0, 0.01f);
        var r = V(0.05f, 0.048f, 0.05f);
        b.Ell(head, c, r, pale);
        var mouth = On(c, r, 0, c.Y);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.025f, 0.025f, Rgb(220, 70, 80), MarkShape.Star);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.8f * s, 0.2f, 0.5f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(30, 30, 30));
            var side = path[path.Length / 2] + V(0.03f * s, 0.004f, 0);
            b.Mark(Body, side, V(s, 0.1f, 0), 0.045f, 0.008f, Rgb(246, 210, 70), MarkShape.Zigzag);
        });
        return Lift(b);
    }

    /// <summary>Eelektrik: a dark blue eel coiled in an S, a cream belly and yellow spots, a red sucker of a mouth ringed with teeth, eyes ringed in yellow, a cream fin on its back and a red fin at its tail.</summary>
    private static PokeBuilder Eelektrik()
    {
        var b = new PokeBuilder("Eelektrik", 0.95f, BodyPlan.Fish, V(0, 0.3f, 0)) { Coat = Scales }.Hover();
        var navy = Rgb(50, 70, 120);
        var cream = Rgb(232, 216, 170);
        var red = Rgb(214, 80, 80);
        var yellow = Rgb(246, 210, 70);
        var path = Smooth(3, V(0, 0.44f, 0.06f), V(0, 0.42f, -0.05f), V(0, 0.32f, -0.07f), V(0, 0.22f, 0.02f), V(0, 0.13f, 0.03f), V(0, 0.1f, -0.07f), V(0, 0.13f, -0.17f));
        b.Tube(Body, path, 0.06f, 0.025f, navy, blend: 0f);
        for (int i = 2; i < path.Length - 2; i += 3)
            PokeBuilder.Both(s => b.Mark(Body, path[i] + V(0.05f * s, 0.01f, 0), V(s, 0.2f, 0), 0.012f, 0.012f, yellow));
        b.PaintEll(Body, V(0, 0.22f, 0.05f), V(0.05f, 0.08f, 0.04f), cream);
        int tail = b.Tail(path[^1]);
        Frond(b, tail, path[^2], path[^1] + V(0, 0.01f, -0.09f), 0.05f, red, V(1f, 0, 0), 0.2f);
        int head = b.Head(path[0]);
        var c = V(0, 0.45f, 0.08f);
        var r = V(0.06f, 0.055f, 0.065f);
        b.Ell(head, c, r, navy);
        Frond(b, head, c + V(0, 0.04f, -0.03f), c + V(0, 0.15f, -0.07f), 0.035f, cream, V(1f, 0, 0), 0.2f);
        // A red sucker for a mouth, teeth set round it in a cross
        var m = c + V(0, -0.005f, 0.06f);
        b.Torus(head, m, 0.035f, 0.015f, red, V(90f, 0, 0), blend: 0.01f);
        b.Ell(head, m, V(0.03f, 0.03f, 0.012f), Rgb(60, 30, 40), blend: 0.004f);
        foreach (var d in new[] { V(1f, 0, 0), V(-1f, 0, 0), V(0, 1f, 0), V(0, -1f, 0) })
            b.Spike(head, m + d * 0.022f + V(0, 0, 0.005f), m + d * 0.006f + V(0, 0, 0.01f), 0.006f, White, mat: Shell, blend: 0.002f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.8f * s, 0.35f, 0.3f));
            b.Mark(head, at, Outward(c, r, at), 0.02f, 0.02f, yellow, MarkShape.Ring);
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, pupil: Rgb(30, 30, 30));
        });
        return Lift(b);
    }

    /// <summary>
    /// Eelektross and its Mega Evolution: a great dark blue eel rising from its coiled tail, clawed arms, a red sucker
    /// of a mouth full of fangs, eyes ringed in yellow, cream fins and yellow spots; the Mega longer, great white tubes of
    /// light marked with yellow crosses held at its sides, and pale fins streaming from it like ghosts.
    /// </summary>
    private static PokeBuilder EelektrossBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Eelektross-Mega" : "Eelektross", 1f, BodyPlan.Fish, V(0, 0.4f, 0)) { Coat = Scales }.Hover();
        var navy = Rgb(46, 64, 116);
        var cream = Rgb(232, 216, 170);
        var red = Rgb(214, 80, 80);
        var yellow = Rgb(246, 210, 70);
        var ghost = Rgb(220, 240, 240);
        var path = Smooth(3, V(0, 0.56f, 0.08f), V(0, 0.5f, 0.02f), V(0, 0.4f, 0.0f), V(0, 0.3f, -0.06f), V(0.04f, 0.24f, -0.18f), V(0.02f, 0.28f, -0.3f), V(-0.04f, 0.36f, mega ? -0.42f : -0.36f));
        b.Tube(Body, path, 0.09f, 0.03f, navy, blend: 0f);
        b.PaintEll(Body, V(0, 0.42f, 0.07f), V(0.07f, 0.1f, 0.04f), cream);
        for (int i = 3; i < path.Length - 2; i += 3)
            PokeBuilder.Both(s => b.Mark(Body, path[i] + V(0.07f * s, 0.02f, 0), V(s, 0.3f, 0), 0.016f, 0.016f, yellow));
        int tail = b.Tail(path[^1]);
        Frond(b, tail, path[^2], path[^1] + V(-0.02f, 0.1f, -0.08f), 0.06f, mega ? ghost : cream, V(1f, 0, 0.2f), 0.2f, mega ? Glow : null);
        // Clawed arms, and the Mega's tubes of light
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.07f * s, 0.46f, 0.05f);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.17f * s, 0.4f, 0.14f);
            b.Limb(arm, shoulder, hand, 0.032f, 0.026f, navy);
            Claws(b, arm, hand + V(0.01f * s, -0.01f, 0.02f), V(0.012f, 0, 0), V(0.2f * s, -0.5f, 1f), 0.035f, 0.008f);
            Frond(b, arm, shoulder + V(0.01f * s, 0.02f, -0.02f), shoulder + V(0.1f * s, 0.06f, -0.12f), 0.035f, mega ? ghost : cream, V(s, 0, 0.3f), 0.2f, mega ? Glow : null);
            if (mega)
            {
                var top = V(0.24f * s, 0.46f, 0.0f);
                var bottom = V(0.26f * s, 0.24f, 0.08f);
                b.Limb(arm, top, bottom, 0.055f, 0.055f, ghost, Glow, 0.01f);
                b.Limb(arm, hand, Vector3.Lerp(top, bottom, 0.5f), 0.016f, 0.016f, navy, null, 0.008f);
                foreach (var end in new[] { top, bottom })
                    b.PaintEll(arm, end, V(0.06f, 0.03f, 0.06f), Rgb(240, 130, 130), Euler(bottom - top));
                var mid = Vector3.Lerp(top, bottom, 0.5f) + V(0.04f * s, 0, 0.03f);
                foreach (float roll in new[] { 0f, 90f })
                    b.Mark(arm, mid, V(s, 0, 0.6f), 0.03f, 0.008f, yellow, MarkShape.Bar, roll);
                Ribbon(b, arm, Smooth(3, top + V(0, 0.04f, -0.02f), top + V(0.06f * s, 0.14f, -0.12f), top + V(0.02f * s, 0.24f, -0.22f), top + V(0.08f * s, 0.3f, -0.3f)), 0.05f, ghost, V(s, 0, 0.3f), 0.012f);
            }
        });
        // A great head, a sucker of a mouth full of fangs, eyes ringed in yellow and fins behind
        int head = b.Head(path[0]);
        var c = V(0, 0.58f, 0.12f);
        var r = V(0.09f, 0.075f, 0.1f);
        b.Ell(head, c, r, navy);
        var m = c + V(0, -0.015f, 0.09f);
        b.Torus(head, m, 0.05f, 0.02f, red, V(90f, 0, 0), blend: 0.01f);
        b.Ell(head, m, V(0.04f, 0.04f, 0.015f), Rgb(60, 30, 40), blend: 0.004f);
        for (int k = 0; k < 6; k++)
        {
            float a = k * MathF.Tau / 6f;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            b.Spike(head, m + d * 0.04f + V(0, 0, 0.012f), m + d * 0.015f + V(0, 0, 0.02f), 0.008f, White, mat: Shell, blend: 0.002f);
        }
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.7f * s, 0.6f, 0.3f));
            b.Mark(head, at, Outward(c, r, at), 0.024f, 0.024f, yellow, MarkShape.Ring);
            b.Eye(head, at, Outward(c, r, at), 0.014f, red, glare: true);
            Frond(b, head, c + V(0.05f * s, 0.03f, -0.06f), c + V(0.15f * s, 0.07f, -0.16f), 0.04f, mega ? ghost : cream, V(s, 0.3f, 0), 0.2f, mega ? Glow : null);
        });
        return Lift(b);
    }

    private static PokeBuilder Eelektross() => EelektrossBuild(false);

    // ------------------------------------------------------------------ Elgyem line

    /// <summary>Three stubby fingers at the end of an arm, tipped red, yellow and green, as the Elgyem line's are.</summary>
    private static void SignalFingers(PokeBuilder b, int bone, Vector3 hand, Vector3 dir, Vector3 across, float r, Color skin)
    {
        var colors = new[] { Rgb(220, 70, 70), Rgb(246, 210, 70), Rgb(80, 190, 100) };
        for (int k = 0; k < 3; k++)
        {
            var tip = hand + Vector3.Normalize(dir) * r * 2.2f + across * (k - 1) * r * 1.3f;
            b.Limb(bone, hand, tip, r * 0.55f, r * 0.5f, skin, null, 0.006f);
            b.Ell(bone, tip, V(r * 0.6f, r * 0.6f, r * 0.6f), colors[k], mat: Shell, blend: 0.004f);
        }
    }

    /// <summary>Elgyem: a little teal alien, a great egg of a head marked with a dark pattern like a brain, small green eyes low on its face, a small body and three fingers tipped red, yellow and green.</summary>
    private static PokeBuilder Elgyem()
    {
        var b = new PokeBuilder("Elgyem", 0.5f, BodyPlan.Biped, V(0, 0.11f, 0)) { Coat = Fur };
        var teal = Rgb(150, 214, 210);
        var dark = Rgb(40, 50, 56);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.06f, 0));
            b.Limb(leg, V(0.025f * s, 0.07f, 0), V(0.035f * s, 0.018f, 0.01f), 0.016f, 0.016f, teal);
            int arm = b.Arm(s, V(0.035f * s, 0.12f, 0));
            var hand = V(0.08f * s, 0.07f, 0.03f);
            b.Limb(arm, V(0.035f * s, 0.12f, 0), hand, 0.012f, 0.011f, teal);
            SignalFingers(b, arm, hand, V(0.4f * s, -1f, 0.3f), V(0, 0, 1f), 0.008f, teal);
        });
        b.Ell(Body, V(0, 0.11f, 0), V(0.04f, 0.05f, 0.035f), teal);
        int head = b.Head(V(0, 0.16f, 0.01f));
        var c = V(0, 0.27f, 0.01f);
        var r = V(0.085f, 0.11f, 0.08f);
        b.Ell(head, c, r, teal);
        foreach (var (x, y, roll) in new[] { (0f, 0.31f, 0f), (-0.025f, 0.34f, -30f), (0.025f, 0.34f, 30f) })
        {
            var at = On(c, r, x, y);
            b.Mark(head, at, Outward(c, r, at), 0.006f, 0.03f, dark, MarkShape.Bar, roll);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y - 0.065f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(80, 190, 100));
        });
        return b;
    }

    /// <summary>Beheeyem: a brown alien in a robe, its head like a helmet flaring at its rim and marked with dark lines, green eyes under it, a collar, pale buttons down its front and three fingers tipped red, yellow and green.</summary>
    private static PokeBuilder Beheeyem()
    {
        var b = new PokeBuilder("Beheeyem", 0.85f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var brown = Rgb(176, 130, 110);
        var dark = Rgb(50, 40, 40);
        var pale = Rgb(226, 196, 176);
        b.Limb(Body, V(0, 0.04f, 0), V(0, 0.3f, 0), 0.11f, 0.06f, brown);
        b.CutBox(Body, V(0, -0.1f, 0), V(0.3f, 0.1f, 0.3f), Quaternion.Identity);
        foreach (var (x, y) in new[] { (-0.025f, 0.24f), (0.025f, 0.24f), (-0.025f, 0.19f), (0.025f, 0.19f) })
            b.Mark(Body, V(x, y, 0.08f + (0.24f - y) * 0.4f), V(0, 0.2f, 1f), 0.01f, 0.01f, pale);
        b.Torus(Body, V(0, 0.33f, 0), 0.055f, 0.022f, brown, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.06f * s, 0.3f, 0);
            int arm = b.Arm(s, shoulder);
            var hand = s > 0 ? V(0.15f * s, 0.28f, 0.08f) : V(0.13f * s, 0.18f, 0.06f);
            b.Limb(arm, shoulder, hand, 0.018f, 0.015f, brown);
            SignalFingers(b, arm, hand, s > 0 ? V(0.5f * s, 0.3f, 1f) : V(0.2f * s, -1f, 0.3f), V(0, 0.3f, 0.7f), 0.011f, brown);
        });
        // A head like a helmet, its rim flared over the eyes, dark lines over its crown
        int head = b.Head(V(0, 0.36f, 0.01f));
        var c = V(0, 0.46f, 0.01f);
        var r = V(0.075f, 0.1f, 0.075f);
        b.Ell(head, c, r, brown);
        b.Torus(head, c + V(0, -0.055f, 0), 0.07f, 0.014f, brown, sz: 0.95f, blend: 0.01f);
        foreach (var (x, y, roll) in new[] { (0f, 0.5f, 0f), (-0.03f, 0.52f, -25f), (0.03f, 0.52f, 25f), (-0.05f, 0.5f, -10f), (0.05f, 0.5f, 10f) })
        {
            var at = On(c, r, x, y);
            b.Mark(head, at, Outward(c, r, at), 0.006f, 0.035f, dark, MarkShape.Bar, roll);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.026f * s, c.Y - 0.075f);
            b.Mark(head, at, Outward(c, r, at), 0.017f, 0.012f, Rgb(80, 190, 100));
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(40, 120, 60));
        });
        return b;
    }

    // ------------------------------------------------------------------ Litwick line

    /// <summary>Litwick: a little candle of white wax, a purple flame on its head, one yellow eye (the wax folded over the other), a smile and two stubby arms.</summary>
    private static PokeBuilder Litwick()
    {
        var b = new PokeBuilder("Litwick", 0.45f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Fur };
        var wax = Rgb(236, 236, 246);
        var shade = Rgb(200, 200, 226);
        var c = V(0, 0.12f, 0);
        var r = V(0.07f, 0.11f, 0.065f);
        b.Ell(Body, c, r, wax);
        b.PaintEll(Body, c + V(0, -0.07f, 0), V(0.09f, 0.05f, 0.09f), shade);
        // Wax melted over its brow and one eye, and drips down its sides
        b.Ell(Body, c + V(-0.025f, 0.03f, 0.03f), V(0.05f, 0.045f, 0.04f), wax, blend: 0.02f);
        foreach (var (x, z) in new[] { (0.06f, 0.02f), (-0.055f, -0.03f), (0.03f, -0.055f) })
            b.Ell(Body, c + V(x, -0.06f, z), V(0.022f, 0.04f, 0.022f), wax, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, c + V(0.06f * s, -0.02f, 0.01f));
            b.Ell(arm, c + V(0.075f * s, -0.035f, 0.02f), V(0.02f, 0.025f, 0.02f), wax);
        });
        var at = On(c, r, 0.03f, c.Y + 0.015f);
        b.Eye(Body, at, Outward(c, r, at), 0.016f, Rgb(250, 220, 60));
        var m = On(c, r, 0.005f, c.Y - 0.025f);
        b.Mark(Body, m, Outward(c, r, m), 0.018f, 0.009f, Rgb(90, 70, 110), MarkShape.Smile);
        int head = b.Head(c + V(0, 0.1f, 0));
        FlameTongue(b, head, c + V(0, 0.1f, 0), c + V(0, 0.24f, -0.01f), 0.04f, Rgb(120, 90, 200), Rgb(170, 200, 250));
        return b;
    }

    /// <summary>Lampent: a lantern, a black cap with a broad brim and a curled point over a round glass body, a blue flame inside it with two yellow eyes, and two black arms curling from its sides.</summary>
    private static PokeBuilder Lampent()
    {
        var b = new PokeBuilder("Lampent", 0.7f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var black = Rgb(44, 42, 50);
        var glass = Rgb(196, 214, 244);
        var flame = Rgb(110, 150, 230);
        var c = V(0, 0.3f, 0);
        var r = V(0.09f, 0.1f, 0.09f);
        b.Ell(Body, c, r, glass, mat: Glow);
        b.PaintEll(Body, c + V(0, -0.005f, 0.06f), V(0.055f, 0.075f, 0.04f), flame);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.005f);
            b.Eye(Body, at, Outward(c, r, at), 0.014f, Rgb(250, 220, 60));
            int arm = b.Arm(s, c + V(0.08f * s, -0.02f, 0));
            Curl(b, arm, c + V(0.08f * s, -0.02f, 0), c + V(0.15f * s, -0.14f, 0.02f), V(0, 1f, 0), V(s, 0, 0), 0.04f, 1.1f, 0.012f, black);
        });
        b.Ell(Body, c + V(0, -0.1f, 0), V(0.045f, 0.025f, 0.045f), black);
        // A black cap with a broad brim, its point curling over
        int head = b.Head(c + V(0, 0.08f, 0));
        b.Ell(head, c + V(0, 0.09f, 0), V(0.08f, 0.035f, 0.08f), black);
        b.Torus(head, c + V(0, 0.075f, 0.01f), 0.11f, 0.018f, black, V(-12f, 0, 0), blend: 0.01f);
        b.Ell(head, c + V(0, 0.075f, 0.01f), V(0.11f, 0.012f, 0.11f), black, V(-12f, 0, 0));
        b.Tube(head, Smooth(3, c + V(0, 0.11f, 0), c + V(0.02f, 0.19f, -0.02f), c + V(0.08f, 0.24f, -0.02f), c + V(0.12f, 0.2f, 0)), 0.03f, 0.012f, black, blend: 0f);
        return Lift(b);
    }

    /// <summary>
    /// Chandelure and its Mega Evolution: a chandelier, a round glass head with two yellow eyes joined by a dark stripe, a
    /// purple flame over it, a black frame of curling arms each holding a cup of purple flame, and a point below; the
    /// Mega taller, its frame in two tiers of arms and many more flames.
    /// </summary>
    private static PokeBuilder ChandelureBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Chandelure-Mega" : "Chandelure", 1f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Metal }.Hover();
        var black = Rgb(44, 42, 50);
        var glass = Rgb(190, 206, 240);
        var rim = Rgb(120, 90, 200);
        var heart = Rgb(170, 200, 250);
        var c = V(0, 0.44f, 0);
        var r = V(0.1f, 0.1f, 0.09f);
        b.Ell(Body, c, r, glass, mat: Glow);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.005f);
            b.Eye(Body, at, Outward(c, r, at), 0.013f, Rgb(250, 220, 60));
        });
        var stripe = On(c, r, 0, c.Y + 0.005f);
        b.Mark(Body, stripe, Outward(c, r, stripe), 0.03f, 0.004f, Rgb(50, 50, 70), MarkShape.Bar);
        b.Ell(Body, c + V(0, -0.1f, 0), V(0.06f, 0.03f, 0.06f), black, mat: Metal);
        b.Spike(Body, c + V(0, -0.11f, 0), c + V(0, mega ? -0.32f : -0.24f, 0), 0.035f, black, mat: Metal);
        if (mega) b.Torus(Body, c + V(0, -0.18f, 0), 0.035f, 0.014f, black, mat: Metal, blend: 0.008f);
        // A crown over the glass and a purple flame rising from it
        int head = b.Head(c + V(0, 0.09f, 0));
        b.Torus(head, c + V(0, 0.08f, 0), 0.06f, 0.014f, black, mat: Metal, blend: 0.008f);
        foreach (float a in new[] { 0.4f, 2f, 3.6f, 5.2f })
            b.Spike(head, c + V(MathF.Cos(a) * 0.06f, 0.08f, MathF.Sin(a) * 0.06f), c + V(MathF.Cos(a) * 0.07f, 0.13f, MathF.Sin(a) * 0.07f), 0.012f, black, mat: Metal);
        FlameTongue(b, head, c + V(0, 0.09f, 0), c + V(0, mega ? 0.34f : 0.26f, -0.02f), 0.05f, rim, heart);
        // Arms of black iron curling out to cups of purple flame, one tier or two
        var tiers = mega ? new[] { (0.0f, 6, 0.22f), (-0.14f, 6, 0.3f) } : new[] { (0.0f, 4, 0.22f) };
        int n = 0;
        foreach (var (dy, count, reach) in tiers)
            for (int i = 0; i < count; i++)
            {
                float a = (i + 0.5f) * MathF.Tau / count + (dy < 0 ? 0.3f : 0f);
                var d = V(MathF.Cos(a), 0, MathF.Sin(a));
                var root = c + V(0, -0.04f + dy, 0) + d * (dy < 0 ? 0.035f : 0.07f);
                int arm = n < 2 ? b.Arm(n == 0 ? -1f : 1f, root) : b.Part("arm" + n, Body, root, PokeRole.Arm, n * 0.3f, d.X < 0 ? -1f : 1f);
                n++;
                var elbow = root + d * reach * 0.6f + V(0, -0.06f, 0);
                var cup = root + d * reach + V(0, 0.06f, 0);
                b.Tube(arm, Smooth(2, root, elbow, cup), 0.013f, 0.011f, black, Metal, 0f);
                b.Ell(arm, cup, V(0.025f, 0.02f, 0.025f), black, mat: Metal);
                FlameTongue(b, arm, cup + V(0, 0.01f, 0), cup + V(0, 0.1f, 0), 0.025f, rim, heart);
            }
        return Lift(b);
    }

    private static PokeBuilder Chandelure() => ChandelureBuild(false);
}
