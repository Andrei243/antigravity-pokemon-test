using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Unova's first batch in National Pokédex
// order: Victini (494) to Audino (531). Their forms are in PokemonModels.Megas.cs and PokemonModels.Regional.cs with
// the other forms. Helpers shared with the earlier batches are in PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs,
// PokemonModels.Kanto1.cs to PokemonModels.Kanto3.cs, PokemonModels.Johto1.cs, PokemonModels.Johto2.cs and
// PokemonModels.Hoenn1.cs to PokemonModels.Hoenn3.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Victini

    /// <summary>Victini: a small cream Pokémon with two great orange ears in a V, big blue eyes, little feathered wings and orange hands and feet.</summary>
    private static PokeBuilder Victini()
    {
        var b = new PokeBuilder("Victini", 0.5f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var cream = Rgb(250, 232, 180);
        var orange = Rgb(240, 120, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.1f, 0));
            b.Limb(leg, V(0.04f * s, 0.11f, 0), V(0.045f * s, 0.03f, 0.01f), 0.022f, 0.018f, cream);
            b.Ell(leg, V(0.045f * s, 0.02f, 0.025f), V(0.022f, 0.018f, 0.035f), orange);
        });
        var bc = V(0, 0.17f, 0);
        var br = V(0.06f, 0.075f, 0.055f);
        b.Ell(Body, bc, br, cream);
        b.Mark(Body, On(bc, br, 0, 0.2f), V(0, 0.2f, 1f), 0.02f, 0.025f, orange, MarkShape.Triangle, 180f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.2f, 0.01f));
            var hand = V(0.1f * s, 0.24f, 0.04f);
            b.Limb(arm, V(0.05f * s, 0.2f, 0.01f), hand, 0.014f, 0.012f, cream);
            b.Ell(arm, hand, V(0.017f, 0.017f, 0.017f), orange);
            int wing = b.Wing(s, V(0.03f * s, 0.2f, -0.05f));
            foreach (var (dy, len) in new[] { (0.02f, 0.09f), (-0.01f, 0.07f) })
                Frond(b, wing, V(0.03f * s, 0.2f + dy, -0.05f), V(0.03f * s, 0.2f + dy, -0.05f) + Vector3.Normalize(V(0.8f * s, 0.4f, -0.4f)) * len, 0.025f, cream, V(0, 0.2f, 1f), 0.3f);
        });
        // A big head with the two great ears rising in a V, a dark stripe along each
        int head = b.Head(V(0, 0.24f, 0.01f));
        var c = V(0, 0.3f, 0.02f);
        var r = V(0.075f, 0.068f, 0.065f);
        b.Ell(head, c, r, cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.05f, 0));
            var tip = c + V(0.17f * s, 0.24f, -0.03f);
            Blade(b, ear, c + V(0.02f * s, 0.04f, -0.01f), tip, 0.045f, orange, V(0, 0.15f, 1f), 0.3f);
            b.PaintEll(ear, Vector3.Lerp(c + V(0.02f * s, 0.04f, -0.01f), tip, 0.45f) + V(-0.012f * s, 0, 0.01f), V(0.008f, 0.06f, 0.02f), Rgb(60, 40, 40), Euler(tip - c), 0.006f);
            var at = On(c, r, 0.032f * s, 0.305f);
            b.Eye(head, at, Outward(c, r, at), 0.02f, Rgb(70, 150, 230));
        });
        b.Mark(head, On(c, r, 0, 0.275f), V(0, -0.3f, 1f), 0.016f, 0.01f, Rgb(200, 90, 100), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Snivy line

    /// <summary>Snivy: a little green grass snake standing up, a cream front, yellow leaves curling from its shoulders and a broad leaf for a tail.</summary>
    private static PokeBuilder Snivy()
    {
        var b = new PokeBuilder("Snivy", 0.55f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Scales };
        var green = Rgb(90, 170, 90);
        var cream = Rgb(240, 236, 190);
        var yellow = Rgb(240, 210, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.1f, 0));
            b.Limb(leg, V(0.035f * s, 0.11f, 0), V(0.04f * s, 0.02f, 0.02f), 0.018f, 0.014f, cream);
        });
        var bc = V(0, 0.17f, 0);
        var br = V(0.055f, 0.08f, 0.05f);
        b.Ell(Body, bc, br, green);
        b.PaintEll(Body, V(0, 0.16f, 0.04f), V(0.045f, 0.08f, 0.03f), cream);
        PokeBuilder.Both(s =>
        {
            Curl(b, Body, V(0.03f * s, 0.23f, -0.02f), V(0.07f * s, 0.21f, -0.06f), V(0, 0, -1f), V(0, 1f, 0), 0.025f, 0.9f, 0.012f, yellow);
            int arm = b.Arm(s, V(0.045f * s, 0.2f, 0.02f));
            b.Limb(arm, V(0.045f * s, 0.2f, 0.02f), V(0.075f * s, 0.17f, 0.04f), 0.01f, 0.009f, green);
        });
        // A broad three-pointed leaf for a tail
        int tail = b.Tail(V(0, 0.12f, -0.04f));
        b.Tube(tail, Smooth(3, V(0, 0.12f, -0.04f), V(0, 0.08f, -0.1f), V(0, 0.12f, -0.15f)), 0.022f, 0.012f, green, blend: 0f);
        Frond(b, tail, V(0, 0.12f, -0.15f), V(0, 0.28f, -0.2f), 0.06f, Rgb(70, 140, 90), V(1f, 0, 0), 0.15f, Leaf);
        // A long head held up, cream on the snout, eyes with a yellow mark over each
        int head = b.Head(V(0, 0.24f, 0.01f));
        var c = V(0, 0.31f, 0.04f);
        var r = V(0.055f, 0.055f, 0.085f);
        b.Ell(head, c, r, green, V(-10f, 0, 0));
        b.PaintEll(head, c + V(0, -0.02f, 0.04f), V(0.05f, 0.035f, 0.07f), cream);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, 0.32f);
            b.Mark(head, at + V(0, 0.016f, -0.004f), Outward(c, r, at), 0.016f, 0.008f, yellow, MarkShape.Bar, -15f * s);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(160, 70, 50), glare: true);
        });
        return b;
    }

    /// <summary>Servine: a slender green grass snake on thin legs, a yellow collar of leaves swept back, small leaves down its back and a tail of leaves.</summary>
    private static PokeBuilder Servine()
    {
        var b = new PokeBuilder("Servine", 0.75f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Scales };
        var green = Rgb(90, 170, 90);
        var dark = Rgb(60, 130, 70);
        var cream = Rgb(240, 236, 190);
        var yellow = Rgb(240, 210, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.2f, 0));
            b.Limb(leg, V(0.035f * s, 0.21f, 0), V(0.045f * s, 0.02f, 0.02f), 0.016f, 0.01f, cream);
        });
        var bc = V(0, 0.28f, 0);
        var br = V(0.05f, 0.1f, 0.045f);
        b.Ell(Body, bc, br, green, V(10f, 0, 0));
        b.PaintEll(Body, V(0, 0.28f, 0.035f), V(0.04f, 0.1f, 0.03f), cream);
        PokeBuilder.Both(s =>
        {
            Blade(b, Body, V(0.03f * s, 0.36f, 0.0f), V(0.1f * s, 0.42f, -0.1f), 0.025f, yellow, V(0, 1f, 0.2f), 0.25f, Leaf);
            int arm = b.Arm(s, V(0.04f * s, 0.32f, 0.02f));
            b.Limb(arm, V(0.04f * s, 0.32f, 0.02f), V(0.08f * s, 0.26f, 0.04f), 0.01f, 0.008f, green);
            for (int i = 0; i < 2; i++)
                PointedLeaf(b, Body, V(0.025f * s, 0.3f - i * 0.06f, -0.03f), V(0.07f * s, 0.32f - i * 0.06f, -0.07f), 0.022f, dark, V(0, 1f, 0), 0.2f);
        });
        // A long tail of leaves held out behind
        int tail = b.Tail(V(0, 0.2f, -0.04f));
        var tp = Smooth(3, V(0, 0.2f, -0.04f), V(0, 0.14f, -0.16f), V(0, 0.16f, -0.3f));
        b.Tube(tail, tp, 0.018f, 0.01f, green, blend: 0f);
        foreach (int i in new[] { 2, 4 })
            PokeBuilder.Both(s => PointedLeaf(b, tail, tp[i], tp[i] + V(0.05f * s, 0.04f, -0.03f), 0.02f, dark, V(0, 1f, 0), 0.2f));
        Frond(b, tail, tp[^1], tp[^1] + V(0, 0.02f, -0.1f), 0.035f, dark, V(0, 1f, 0), 0.15f, Leaf);
        int head = b.Head(V(0, 0.38f, 0.02f));
        var c = V(0, 0.45f, 0.05f);
        var r = V(0.045f, 0.045f, 0.08f);
        b.Limb(head, V(0, 0.36f, 0.02f), c, 0.025f, 0.025f, green);
        b.Ell(head, c, r, green, V(-10f, 0, 0));
        b.PaintEll(head, c + V(0, -0.02f, 0.04f), V(0.04f, 0.03f, 0.07f), cream);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, 0.46f);
            b.Mark(head, at + V(0, 0.013f, -0.004f), Outward(c, r, at), 0.014f, 0.006f, yellow, MarkShape.Bar, -15f * s);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(160, 70, 50), glare: true);
        });
        return b;
    }

    /// <summary>Serperior: a long regal green serpent rising high from its coils, cream below, yellow curls of leaf at its neck and down its body, a crown of leaves.</summary>
    private static PokeBuilder Serperior()
    {
        var green = Rgb(70, 150, 80);
        var cream = Rgb(236, 234, 196);
        var yellow = Rgb(236, 200, 80);
        var coil = Spiral3(0.24f, 0.12f, 0.06f, 0.14f, 2.0f, 1.3f, 20);
        var points = new[] { V(0.24f, 0.08f, -0.34f), V(0.24f, 0.07f, -0.24f) }.Concat(coil)
            .Concat(new[] { V(0.0f, 0.24f, 0.05f), V(0, 0.38f, 0.07f), V(0, 0.5f, 0.06f) }).ToArray();
        var b = new PokeBuilder("Serperior", 0.95f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int neck = Coils(b, points, t => 0.025f + 0.035f * MathF.Min(1f, t * 3f), green);
        for (int i = 6; i < points.Length - 6; i += 4)
        {
            int bone = 1 + i / 2;
            var mid = Vector3.Lerp(points[i], points[i + 1], 0.5f);
            b.Mark(bone, mid + V(0, 0.055f, 0), V(0, 1f, 0), 0.02f, 0.014f, yellow, MarkShape.Ring);
        }
        // Leaves at the tail's end
        int tail = b.Tail(points[0]);
        foreach (var o in new[] { V(0.04f, 0.05f, -0.06f), V(-0.04f, 0.04f, -0.06f), V(0, 0.07f, -0.02f) })
            PointedLeaf(b, tail, points[0], points[0] + o, 0.02f, Rgb(60, 130, 70), V(0, 1f, 0.3f), 0.2f);
        // Yellow curls flaring at its neck like a collar
        PokeBuilder.Both(s =>
        {
            Curl(b, neck, V(0.03f * s, 0.4f, 0.04f), V(0.1f * s, 0.42f, 0.0f), V(0, 0, -1f), V(0, 1f, 0), 0.035f, 1.1f, 0.012f, yellow);
            b.PaintEll(neck, V(0.0f, 0.38f, 0.1f), V(0.04f, 0.12f, 0.03f), cream);
        });
        int head = b.Head(points[^1], neck);
        var c = V(0, 0.55f, 0.09f);
        var r = V(0.055f, 0.05f, 0.09f);
        b.Ell(head, c, r, green, V(-5f, 0, 0));
        b.PaintEll(head, c + V(0, -0.02f, 0.04f), V(0.05f, 0.03f, 0.07f), cream);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.03f * s, 0.03f, -0.03f), c + V(0.07f * s, 0.09f, -0.12f), 0.02f, yellow, V(0, 0.4f, 1f), 0.25f, Leaf);
            var at = On(c, r, 0.03f * s, 0.56f);
            b.Mark(head, at + V(0, 0.013f, -0.004f), Outward(c, r, at), 0.014f, 0.006f, yellow, MarkShape.Bar, -15f * s);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(180, 50, 50), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Tepig line

    /// <summary>Tepig: a little orange fire pig, black over its hindquarters, a red snout, a yellow stripe by its eyes and a curly tail with a red ball at its end.</summary>
    private static PokeBuilder Tepig()
    {
        var b = new PokeBuilder("Tepig", 0.5f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Fur };
        var orange = Rgb(236, 120, 60);
        var black = Rgb(56, 46, 46);
        var red = Rgb(214, 90, 80);
        StubbyLegs(b, 0.055f, 0.08f, 0.06f, -0.06f, 0.026f, black);
        var bc = V(0, 0.13f, -0.01f);
        var br = V(0.08f, 0.075f, 0.11f);
        b.Ell(Body, bc, br, orange);
        b.PaintEll(Body, V(0, 0.13f, -0.09f), V(0.09f, 0.08f, 0.06f), black);
        int tail = b.Tail(V(0, 0.16f, -0.11f));
        Curl(b, tail, V(0, 0.16f, -0.11f), V(0, 0.2f, -0.15f), V(0, 0, -1f), V(0, 1f, 0), 0.025f, 1.2f, 0.007f, black);
        b.Ell(tail, V(0, 0.24f, -0.16f), V(0.022f, 0.022f, 0.022f), red, blend: 0.006f);
        int head = b.Head(V(0, 0.17f, 0.08f));
        var c = V(0, 0.19f, 0.1f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Ell(head, c, r, orange);
        b.Ell(head, V(0, 0.18f, 0.17f), V(0.03f, 0.025f, 0.015f), red, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Mark(head, V(0.008f * s, 0.18f, 0.186f), V(0, 0, 1f), 0.005f, 0.007f, Rgb(120, 50, 50));
            int ear = b.Ear(head, s, V(0.04f * s, 0.24f, 0.08f));
            Blade(b, ear, V(0.035f * s, 0.24f, 0.08f), V(0.06f * s, 0.29f, 0.05f), 0.024f, black, V(0, 0, 1f), 0.3f);
            var at = On(c, r, 0.04f * s, 0.2f);
            b.PaintEll(head, at + V(0.01f * s, -0.005f, 0.01f), V(0.03f, 0.022f, 0.02f), Rgb(246, 210, 70));
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(40, 30, 30));
        });
        return b;
    }

    /// <summary>Pignite: a round orange fire pig standing up, black below its belt, a yellow belt with gold swirls, black arms with bands and a black ear on each side.</summary>
    private static PokeBuilder Pignite()
    {
        var b = new PokeBuilder("Pignite", 0.8f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var orange = Rgb(236, 120, 60);
        var black = Rgb(56, 46, 46);
        var gold = Rgb(244, 196, 70);
        var red = Rgb(214, 90, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.12f, 0));
            b.Limb(leg, V(0.07f * s, 0.13f, 0), V(0.08f * s, 0.04f, 0.01f), 0.04f, 0.035f, black);
            b.Ell(leg, V(0.08f * s, 0.025f, 0.03f), V(0.04f, 0.025f, 0.05f), black);
        });
        var bc = V(0, 0.27f, 0);
        var br = V(0.14f, 0.16f, 0.12f);
        b.Ell(Body, bc, br, orange);
        b.PaintEll(Body, V(0, 0.15f, 0), V(0.16f, 0.1f, 0.14f), black);
        b.PaintTorus(Body, V(0, 0.22f, 0), 0.14f, 0.02f, gold, sz: 0.86f);
        PokeBuilder.Both(s => b.Mark(Body, On(bc, br, 0.07f * s, 0.21f), V(0.4f * s, -0.1f, 1f), 0.03f, 0.03f, Rgb(200, 150, 40), MarkShape.Ring));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.32f, 0));
            var hand = V(0.2f * s, 0.42f, 0.04f);
            b.Limb(arm, V(0.13f * s, 0.32f, 0), hand, 0.04f, 0.035f, black);
            b.PaintTorus(arm, Vector3.Lerp(V(0.13f * s, 0.32f, 0), hand, 0.6f), 0.038f, 0.008f, gold, Euler(hand - V(0.13f * s, 0.32f, 0)));
            b.Ell(arm, hand, V(0.035f, 0.035f, 0.035f), Rgb(230, 226, 220));
        });
        int tail = b.Tail(V(0, 0.18f, -0.1f));
        b.Limb(tail, V(0, 0.2f, -0.08f), V(0, 0.18f, -0.13f), 0.025f, 0.02f, black);
        GasPuff(b, tail, V(0, 0.18f, -0.14f), V(0, 0.2f, -1f), 0.035f, black);
        // The head grown into the body: a red snout, fangs, black ears
        int head = b.Head(V(0, 0.36f, 0.02f));
        var c = V(0, 0.4f, 0.04f);
        var r = V(0.1f, 0.08f, 0.09f);
        b.Ell(head, c, r, orange);
        b.Ell(head, V(0, 0.39f, 0.13f), V(0.035f, 0.025f, 0.015f), red, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.03f * s, 0.36f, 0.12f), V(0.035f * s, 0.38f, 0.125f), 0.008f, White, mat: Shell, blend: 0.003f);
            int ear = b.Ear(head, s, V(0.06f * s, 0.46f, 0.02f));
            Blade(b, ear, V(0.06f * s, 0.46f, 0.02f), V(0.1f * s, 0.55f, -0.01f), 0.035f, black, V(0, 0, 1f), 0.3f);
            var at = On(c, r, 0.045f * s, 0.42f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(40, 30, 30), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Emboar and its Mega Evolution: a great fire boar standing up, black with an orange chest, a beard of flame
    /// round its face, a gold belt of swirls and red over its legs; the Mega's flames grown into a crown and a
    /// mantle, holding a staff of fire.
    /// </summary>
    private static PokeBuilder EmboarBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Emboar-Mega" : "Emboar", 1f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var orange = Rgb(236, 120, 60);
        var black = Rgb(54, 46, 50);
        var gold = Rgb(244, 196, 70);
        var red = Rgb(200, 60, 50);
        var flame = Rgb(246, 120, 50);
        var heart = Rgb(252, 214, 100);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.24f, 0));
            b.Limb(leg, V(0.09f * s, 0.26f, 0), V(0.1f * s, 0.06f, 0.01f), 0.07f, 0.055f, red);
            b.Ell(leg, V(0.1f * s, 0.035f, 0.03f), V(0.06f, 0.035f, 0.07f), black);
            b.PaintTorus(leg, V(0.1f * s, 0.12f, 0), 0.06f, 0.012f, gold);
        });
        var bc = V(0, 0.44f, 0);
        var br = V(0.17f, 0.2f, 0.14f);
        b.Ell(Body, bc, br, black);
        b.PaintEll(Body, V(0, 0.52f, 0.06f), V(0.15f, 0.1f, 0.1f), orange);
        b.PaintTorus(Body, V(0, 0.36f, 0), 0.17f, 0.035f, gold, sz: 0.85f);
        PokeBuilder.Both(s => b.Mark(Body, On(bc, br, 0.07f * s, 0.36f), V(0.4f * s, -0.1f, 1f), 0.035f, 0.035f, Rgb(170, 120, 30), MarkShape.Ring));
        // Arms: black, gold bands, white fists; the Mega's right hand holds a staff of fire
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, 0.54f, 0));
            var hand = V(0.27f * s, 0.38f, 0.08f);
            b.Limb(arm, V(0.16f * s, 0.54f, 0), hand, 0.06f, 0.05f, black);
            b.PaintTorus(arm, Vector3.Lerp(V(0.16f * s, 0.54f, 0), hand, 0.7f), 0.052f, 0.012f, gold, Euler(hand - V(0.16f * s, 0.54f, 0)));
            b.Ell(arm, hand, V(0.045f, 0.045f, 0.045f), Rgb(230, 226, 220));
            if (mega && s > 0)
            {
                var staff = Smooth(2, hand + V(0, -0.36f, 0), hand + V(0.02f, -0.18f, 0.01f), hand, hand + V(-0.02f, 0.2f, 0.0f), hand + V(0.02f, 0.4f, -0.01f));
                b.Tube(arm, staff, 0.016f, 0.012f, flame, Glow, 0f);
                FlameTongue(b, arm, staff[^1], staff[^1] + V(0, 0.08f, 0), 0.03f, flame, heart);
            }
        });
        int tail = b.Tail(V(0, 0.3f, -0.11f));
        b.Limb(tail, V(0, 0.32f, -0.09f), V(0, 0.3f, -0.15f), 0.03f, 0.025f, black);
        GasPuff(b, tail, V(0, 0.3f, -0.16f), V(0, 0.2f, -1f), 0.04f, black);
        // A head with a black brow and a beard of flames round its face
        int head = b.Head(V(0, 0.62f, 0.04f));
        var c = V(0, 0.68f, 0.07f);
        var r = V(0.09f, 0.08f, 0.085f);
        b.Ell(head, c, r, orange);
        b.PaintEll(head, c + V(0, 0.05f, 0.02f), V(0.09f, 0.04f, 0.08f), black);
        b.Ell(head, c + V(0, -0.01f, 0.08f), V(0.035f, 0.025f, 0.015f), Rgb(220, 120, 110), blend: 0.012f);
        FlameRow(b, head, c + V(-0.09f, -0.03f, 0.03f), c + V(0.09f, -0.03f, 0.03f), V(0, -0.4f, 0.6f), 7, mega ? 0.12f : 0.09f, 0.025f, flame, heart, 2.2f);
        if (mega) FlameRow(b, head, c + V(-0.08f, 0.06f, -0.03f), c + V(0.08f, 0.06f, -0.03f), V(0, 1f, -0.2f), 5, 0.16f, 0.03f, flame, heart, 1.2f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.035f * s, -0.04f, 0.075f), c + V(0.04f * s, -0.01f, 0.085f), 0.01f, White, mat: Shell, blend: 0.003f);
            int ear = b.Ear(head, s, c + V(0.07f * s, 0.05f, -0.01f));
            Blade(b, ear, c + V(0.07f * s, 0.05f, -0.01f), c + V(0.12f * s, 0.12f, -0.04f), 0.03f, black, V(0, 0, 1f), 0.3f);
            var at = On(c, r, 0.04f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 30, 30), glare: true);
        });
        if (mega) FlameRow(b, Body, V(-0.15f, 0.56f, -0.06f), V(0.15f, 0.56f, -0.06f), V(0, 0.6f, -1f), 6, 0.14f, 0.035f, flame, heart, 1.4f);
        return b;
    }

    private static PokeBuilder Emboar() => EmboarBuild(false);

    // ------------------------------------------------------------------ Oshawott line

    /// <summary>Oshawott: a little sea otter, its head white with dark blue ears and a brown nose, a light blue body, a scalchop on its belly and dark blue feet and tail.</summary>
    private static PokeBuilder Oshawott()
    {
        var b = new PokeBuilder("Oshawott", 0.5f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Fur };
        var white = Rgb(246, 246, 248);
        var blue = Rgb(150, 210, 220);
        var navy = Rgb(60, 90, 150);
        var shell = Rgb(240, 220, 160);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.07f, 0));
            b.Ell(leg, V(0.045f * s, 0.025f, 0.02f), V(0.03f, 0.025f, 0.04f), navy);
        });
        var bc = V(0, 0.13f, 0);
        var br = V(0.07f, 0.08f, 0.06f);
        b.Ell(Body, bc, br, blue);
        b.Ell(Body, V(0, 0.13f, 0.055f), V(0.04f, 0.035f, 0.012f), shell, mat: Shell, blend: 0.006f);
        b.Mark(Body, V(0, 0.13f, 0.068f), V(0, 0, 1f), 0.03f, 0.025f, Rgb(210, 180, 120), MarkShape.Wave);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.17f, 0.01f));
            b.Limb(arm, V(0.06f * s, 0.17f, 0.01f), V(0.1f * s, 0.12f, 0.03f), 0.016f, 0.016f, white);
        });
        int tail = b.Tail(V(0, 0.08f, -0.05f));
        Frond(b, tail, V(0, 0.08f, -0.05f), V(0, 0.05f, -0.16f), 0.045f, navy, V(0, 1f, 0.2f), 0.3f);
        int head = b.Head(V(0, 0.2f, 0.01f));
        var c = V(0, 0.27f, 0.02f);
        var r = V(0.085f, 0.075f, 0.075f);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.02f, 0.075f), V(0.022f, 0.016f, 0.012f), Rgb(150, 100, 70), blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.05f, -0.01f));
            b.Ell(ear, c + V(0.075f * s, 0.06f, -0.01f), V(0.025f, 0.022f, 0.015f), navy, blend: 0.008f);
            var at = On(c, r, 0.035f * s, 0.28f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(36, 36, 48));
            b.Mark(head, On(c, r, 0.055f * s, 0.25f), V(0.5f * s, -0.1f, 1f), 0.004f, 0.004f, Rgb(170, 160, 160));
        });
        b.PaintTorus(Body, V(0, 0.2f, 0.0f), 0.055f, 0.012f, blue);
        return b;
    }

    /// <summary>Dewott: an otter standing tall, light blue with a dark blue skirt of fur, a scalchop on each thigh, white whiskers and a dark topknot.</summary>
    private static PokeBuilder Dewott()
    {
        var b = new PokeBuilder("Dewott", 0.75f, BodyPlan.Biped, V(0, 0.26f, 0)) { Coat = Fur };
        var blue = Rgb(130, 200, 220);
        var navy = Rgb(50, 70, 130);
        var shell = Rgb(240, 220, 160);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.16f, 0));
            b.Limb(leg, V(0.05f * s, 0.17f, 0), V(0.06f * s, 0.04f, 0.01f), 0.03f, 0.024f, blue);
            b.Ell(leg, V(0.06f * s, 0.022f, 0.03f), V(0.028f, 0.022f, 0.04f), navy);
            b.Ell(leg, V(0.08f * s, 0.15f, 0.02f), V(0.012f, 0.035f, 0.025f), shell, mat: Shell, blend: 0.006f);
        });
        var bc = V(0, 0.26f, 0);
        var br = V(0.07f, 0.1f, 0.06f);
        b.Ell(Body, bc, br, blue);
        // A skirt of dark fur, jagged at its hem
        for (int i = 0; i < 8; i++)
        {
            float a = MathF.Tau * i / 8f;
            b.Spike(Body, V(MathF.Sin(a) * 0.05f, 0.22f, MathF.Cos(a) * 0.045f), V(MathF.Sin(a) * 0.085f, 0.13f, MathF.Cos(a) * 0.075f), 0.03f, navy, 0.5f, blend: 0.015f);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.32f, 0.01f));
            var hand = V(0.14f * s, 0.28f, 0.04f);
            b.Limb(arm, V(0.06f * s, 0.32f, 0.01f), hand, 0.018f, 0.016f, blue);
            b.Ell(arm, hand, V(0.022f, 0.02f, 0.02f), navy);
        });
        int tail = b.Tail(V(0, 0.18f, -0.05f));
        Frond(b, tail, V(0, 0.18f, -0.05f), V(0, 0.08f, -0.16f), 0.045f, navy, V(0, 1f, 0.2f), 0.3f);
        int head = b.Head(V(0, 0.34f, 0.01f));
        var c = V(0, 0.41f, 0.02f);
        var r = V(0.07f, 0.065f, 0.065f);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, -0.01f, 0.065f), V(0.016f, 0.012f, 0.01f), Rgb(200, 100, 90), blend: 0.006f);
        b.Ell(head, c + V(0, 0.07f, -0.02f), V(0.025f, 0.03f, 0.025f), navy, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.04f * s, -0.015f, 0.04f), c + V(0.13f * s, 0.0f, 0.03f), 0.012f, White, V(0, 1f, 0.2f), 0.3f);
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.05f, -0.01f));
            b.Ell(ear, c + V(0.06f * s, 0.055f, -0.01f), V(0.02f, 0.018f, 0.012f), navy, blend: 0.008f);
            var at = On(c, r, 0.03f * s, 0.42f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(36, 36, 48), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Samurott and its Hisuian form: a great blue sea lion on four legs, armour of cream shells on its forelegs,
    /// a white moustache and a helmet with a horn; the Hisuian dark navy edged red, its helmet horned back and a long
    /// white mane.
    /// </summary>
    private static PokeBuilder SamurottBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Samurott-Hisui" : "Samurott", 1f, BodyPlan.Quadruped, V(0, 0.3f, 0)) { Coat = Fur };
        var blue = hisui ? Rgb(50, 70, 130) : Rgb(70, 110, 180);
        var pale = hisui ? Rgb(160, 200, 220) : Rgb(170, 210, 230);
        var armour = hisui ? Rgb(150, 40, 60) : Rgb(230, 214, 150);
        var helm = hisui ? Rgb(40, 44, 70) : Rgb(230, 214, 150);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.09f * s, 0.24f, z), front);
                var foot = V(0.13f * s, 0.04f, z + (front ? 0.05f : -0.04f));
                b.Limb(leg, V(0.09f * s, 0.26f, z), foot, front ? 0.05f : 0.055f, 0.035f, blue);
                Frond(b, leg, foot, foot + V(0.02f * s, -0.02f, 0.06f), 0.03f, blue, V(0, 1f, 0), 0.4f);
                if (front)
                    for (int i = 0; i < 3; i++)
                        b.Ell(leg, Vector3.Lerp(V(0.09f * s, 0.26f, z), foot, 0.2f + 0.25f * i) + V(0.035f * s, 0, 0.01f), V(0.02f, 0.035f, 0.035f), armour, mat: Shell, blend: 0.008f);
            });
        var bc = V(0, 0.3f, -0.01f);
        var br = V(0.12f, 0.12f, 0.2f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, V(0, 0.24f, 0.06f), V(0.1f, 0.08f, 0.14f), pale);
        int tail = b.Tail(V(0, 0.3f, -0.2f));
        b.Tube(tail, Smooth(3, V(0, 0.3f, -0.2f), V(0, 0.34f, -0.32f), V(0, 0.42f, -0.4f)), 0.05f, 0.02f, blue, blend: 0f);
        PokeBuilder.Both(s => Blade(b, tail, V(0, 0.42f, -0.4f), V(0.06f * s, 0.5f, -0.46f), 0.03f, blue, V(1f, 0, 0), 0.3f));
        // The head under its helmet: a horn rising from its brow, a white moustache flowing back
        int head = b.Head(V(0, 0.38f, 0.17f));
        var c = V(0, 0.43f, 0.23f);
        var r = V(0.085f, 0.075f, 0.09f);
        b.Limb(head, V(0, 0.35f, 0.15f), c, 0.06f, 0.055f, blue);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, 0.03f, -0.015f), V(0.095f, 0.06f, 0.09f), helm, mat: Shell, blend: 0.012f);
        if (hisui)
        {
            PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.04f * s, 0.07f, -0.02f), c + V(0.06f * s, 0.16f, -0.08f), c + V(0.03f * s, 0.24f, -0.14f)), 0.02f, 0.008f, helm, Shell, 0f));
            FurTufts(b, head, c + V(0, -0.04f, -0.04f), V(0.08f, 0.06f, 0.06f), 12, 0.1f, 0.022f, White, -0.2f, -0.9f, 0.7f);
        }
        else
            b.Spike(head, c + V(0, 0.07f, 0.02f), c + V(0, 0.18f, 0.1f), 0.03f, helm, 0.5f, Shell);
        PokeBuilder.Both(s =>
        {
            Ribbon(b, head, Smooth(2, c + V(0.03f * s, -0.03f, 0.08f), c + V(0.09f * s, -0.05f, 0.04f), c + V(0.14f * s, -0.08f, -0.04f)), 0.025f, White, V(0, 1f, 0.2f), 0.008f);
            var at = On(c, r, 0.04f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, hisui ? Rgb(200, 40, 60) : Rgb(40, 40, 60), glare: true);
        });
        return b;
    }

    private static PokeBuilder Samurott() => SamurottBuild(false);

    // ------------------------------------------------------------------ Patrat line

    /// <summary>Patrat: a brown lookout rodent standing up, a cream belly, wide red and yellow eyes ringed black, big front teeth and a cream fan of a tail.</summary>
    private static PokeBuilder Patrat()
    {
        var b = new PokeBuilder("Patrat", 0.55f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Fur };
        var brown = Rgb(160, 110, 76);
        var cream = Rgb(236, 214, 170);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.07f, 0));
            b.Limb(leg, V(0.05f * s, 0.09f, 0), V(0.055f * s, 0.035f, 0.01f), 0.02f, 0.018f, brown);
            b.Ell(leg, V(0.055f * s, 0.025f, 0.02f), V(0.03f, 0.025f, 0.045f), Rgb(80, 60, 50));
        });
        var bc = V(0, 0.16f, 0);
        var br = V(0.085f, 0.11f, 0.07f);
        b.Ell(Body, bc, br, brown);
        b.PaintEll(Body, V(0, 0.15f, 0.05f), V(0.065f, 0.09f, 0.03f), cream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.2f, 0.02f));
            var hand = V(0.12f * s, 0.27f, 0.05f);
            b.Limb(arm, V(0.07f * s, 0.2f, 0.02f), hand, 0.018f, 0.016f, brown);
        });
        int tail = b.Tail(V(0, 0.1f, -0.06f));
        b.Tube(tail, Smooth(3, V(0, 0.1f, -0.06f), V(0.03f, 0.18f, -0.12f), V(0.06f, 0.28f, -0.1f)), 0.014f, 0.012f, brown, blend: 0f);
        FurTufts(b, tail, V(0.06f, 0.3f, -0.1f), V(0.03f, 0.03f, 0.02f), 6, 0.04f, 0.014f, cream, -0.8f, -0.5f, 0.1f);
        int head = b.Head(V(0, 0.25f, 0.01f));
        var c = V(0, 0.3f, 0.02f);
        var r = V(0.08f, 0.065f, 0.07f);
        b.Ell(head, c, r, brown);
        b.PaintEll(head, c + V(0, -0.02f, 0.05f), V(0.05f, 0.03f, 0.03f), cream);
        b.Box(head, c + V(0, -0.04f, 0.065f), V(0.012f, 0.012f, 0.004f), 0.003f, White, mat: Shell, blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.305f);
            b.Mark(head, at, Outward(c, r, at), 0.028f, 0.024f, Black);
            b.Eye(head, at, Outward(c, r, at), 0.016f, sclera: true, white: Rgb(246, 200, 70), pupil: Rgb(200, 50, 50), glare: true);
        });
        return b;
    }

    /// <summary>Watchog: a tall lean lookout rodent, brown with bands of yellow round its body and tail, glowing eyes ringed red and a white fan at its tail's tip.</summary>
    private static PokeBuilder Watchog()
    {
        var b = new PokeBuilder("Watchog", 0.85f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var brown = Rgb(150, 90, 60);
        var yellow = Rgb(240, 200, 80);
        var cream = Rgb(240, 226, 196);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.18f, 0));
            b.Limb(leg, V(0.04f * s, 0.19f, 0), V(0.045f * s, 0.03f, 0.02f), 0.02f, 0.016f, cream);
            b.Ell(leg, V(0.045f * s, 0.018f, 0.035f), V(0.022f, 0.018f, 0.04f), cream);
        });
        var bc = V(0, 0.32f, 0);
        var br = V(0.055f, 0.15f, 0.05f);
        b.Ell(Body, bc, br, brown);
        foreach (float y in new[] { 0.24f, 0.31f, 0.38f })
            b.PaintTorus(Body, V(0, y, 0), 0.05f, 0.016f, yellow, sz: 0.9f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.045f * s, 0.42f, 0.01f));
            var hand = V(0.05f * s, 0.34f, 0.06f);
            b.Limb(arm, V(0.045f * s, 0.42f, 0.01f), hand, 0.015f, 0.013f, brown);
        });
        int tail = b.Tail(V(0, 0.2f, -0.04f));
        var tp = Smooth(3, V(0, 0.2f, -0.04f), V(0.04f, 0.3f, -0.1f), V(0.06f, 0.5f, -0.08f), V(0.07f, 0.62f, -0.04f));
        b.Tube(tail, tp, 0.022f, 0.016f, brown, blend: 0f);
        for (int i = 2; i < tp.Length - 1; i += 2)
            b.PaintTorus(tail, tp[i], 0.02f, 0.008f, yellow, Euler(tp[i + 1] - tp[i]));
        FurTufts(b, tail, tp[^1] + V(0, 0.02f, 0), V(0.025f, 0.025f, 0.015f), 6, 0.04f, 0.012f, White, -0.8f, -0.6f, 0.1f);
        int head = b.Head(V(0, 0.46f, 0.01f));
        var c = V(0, 0.52f, 0.02f);
        var r = V(0.06f, 0.055f, 0.06f);
        b.Ell(head, c, r, brown);
        b.PaintEll(head, c + V(0, -0.02f, 0.04f), V(0.04f, 0.025f, 0.03f), cream);
        b.Box(head, c + V(0, -0.035f, 0.057f), V(0.01f, 0.01f, 0.004f), 0.003f, White, mat: Shell, blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, 0.525f);
            b.Mark(head, at, Outward(c, r, at), 0.022f, 0.018f, Rgb(214, 70, 60));
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, white: Rgb(246, 210, 80), pupil: Rgb(40, 30, 30), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Lillipup line

    /// <summary>Lillipup: a little brown puppy, a great cream mane round its face, a navy patch on its back, a red nose and floppy ears.</summary>
    private static PokeBuilder Lillipup()
    {
        var b = new PokeBuilder("Lillipup", 0.5f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Fur };
        var brown = Rgb(200, 140, 100);
        var cream = Rgb(246, 222, 170);
        var navy = Rgb(70, 80, 120);
        StubbyLegs(b, 0.05f, 0.09f, 0.06f, -0.06f, 0.026f, brown);
        var bc = V(0, 0.13f, -0.02f);
        var br = V(0.065f, 0.06f, 0.1f);
        b.Ell(Body, bc, br, brown);
        b.PaintEll(Body, V(0, 0.19f, -0.04f), V(0.04f, 0.02f, 0.05f), navy);
        int tail = b.Tail(V(0, 0.15f, -0.1f));
        b.Spike(tail, V(0, 0.15f, -0.1f), V(0, 0.21f, -0.15f), 0.022f, brown);
        int head = b.Head(V(0, 0.18f, 0.07f));
        var c = V(0, 0.21f, 0.09f);
        var r = V(0.075f, 0.07f, 0.065f);
        b.Ell(head, c, r, brown);
        FurTufts(b, head, c + V(0, -0.01f, 0.01f), V(0.075f, 0.068f, 0.06f), 14, 0.03f, 0.022f, cream, 0.98f, -0.9f, 0.5f);
        b.PaintEll(head, c + V(0, -0.01f, 0.05f), V(0.07f, 0.06f, 0.035f), cream);
        b.Ell(head, c + V(0, -0.015f, 0.065f), V(0.012f, 0.009f, 0.008f), Rgb(200, 90, 90), mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.05f, -0.01f));
            CatEar(b, ear, c + V(0.05f * s, 0.05f, -0.01f), c + V(0.09f * s, 0.12f, -0.03f), 0.025f, brown, Rgb(230, 170, 140));
            var at = On(c, r, 0.03f * s, 0.22f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(40, 34, 30));
        });
        return b;
    }

    /// <summary>Herdier: a terrier with a dark navy cape of fur over its back, brown legs, a pale moustache and fringe and pointed ears.</summary>
    private static PokeBuilder Herdier()
    {
        var b = new PokeBuilder("Herdier", 0.7f, BodyPlan.Quadruped, V(0, 0.18f, 0)) { Coat = Fur };
        var brown = Rgb(200, 130, 90);
        var navy = Rgb(60, 66, 110);
        var cream = Rgb(240, 216, 170);
        foreach (var (z, front) in new[] { (0.08f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.055f * s, 0.14f, z), front);
                b.Limb(leg, V(0.055f * s, 0.16f, z), V(0.06f * s, 0.035f, z + 0.01f), 0.032f, 0.026f, brown);
                b.Ell(leg, V(0.06f * s, 0.02f, z + 0.025f), V(0.026f, 0.02f, 0.035f), brown);
            });
        var bc = V(0, 0.19f, -0.01f);
        var br = V(0.08f, 0.075f, 0.14f);
        b.Ell(Body, bc, br, brown);
        b.Ell(Body, V(0, 0.23f, -0.02f), V(0.09f, 0.055f, 0.14f), navy, blend: 0.015f);
        FurTufts(b, Body, V(0, 0.21f, -0.02f), V(0.09f, 0.05f, 0.13f), 12, 0.06f, 0.02f, navy, -0.6f, -0.3f, 0.9f);
        int tail = b.Tail(V(0, 0.23f, -0.14f));
        b.Spike(tail, V(0, 0.23f, -0.14f), V(0, 0.32f, -0.2f), 0.022f, navy);
        int head = b.Head(V(0, 0.26f, 0.1f));
        var c = V(0, 0.29f, 0.14f);
        var r = V(0.065f, 0.06f, 0.07f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.02f, 0.06f), V(0.035f, 0.03f, 0.04f), brown, blend: 0.02f);
        b.Ell(head, c + V(0, -0.01f, 0.1f), V(0.012f, 0.01f, 0.008f), Black, mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.015f * s, -0.035f, 0.08f), c + V(0.07f * s, -0.07f, 0.05f), 0.02f, cream, V(0, 0.4f, 1f), 0.3f);
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.05f, -0.01f));
            CatEar(b, ear, c + V(0.04f * s, 0.05f, -0.01f), c + V(0.08f * s, 0.13f, -0.03f), 0.024f, brown, navy);
            var at = On(c, r, 0.03f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 34, 30), glare: true);
        });
        FurTufts(b, head, c + V(0, 0.04f, 0.02f), V(0.04f, 0.02f, 0.03f), 5, 0.04f, 0.012f, cream, 0.2f, -0.2f, 0.6f);
        return b;
    }

    /// <summary>Stoutland: a big old dog, a long cream coat of fur hanging over its navy body like a cape, brown legs, a great moustache and bushy brows.</summary>
    private static PokeBuilder Stoutland()
    {
        var b = new PokeBuilder("Stoutland", 0.9f, BodyPlan.Quadruped, V(0, 0.22f, 0)) { Coat = Fur };
        var brown = Rgb(170, 110, 76);
        var navy = Rgb(60, 66, 100);
        var cream = Rgb(240, 222, 186);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.075f * s, 0.14f, z), front);
                b.Limb(leg, V(0.075f * s, 0.16f, z), V(0.08f * s, 0.035f, z + 0.01f), 0.042f, 0.032f, brown);
                b.Ell(leg, V(0.08f * s, 0.02f, z + 0.03f), V(0.032f, 0.02f, 0.042f), brown);
            });
        var bc = V(0, 0.2f, -0.02f);
        var br = V(0.11f, 0.09f, 0.2f);
        b.Ell(Body, bc, br, navy);
        // A long cream coat over its back, parted down the middle, falling to its knees
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, V(0.06f * s, 0.25f, -0.05f), V(0.08f, 0.06f, 0.19f), cream, V(0, 0, -20f * s), blend: 0.015f);
            FurTufts(b, Body, V(0.08f * s, 0.21f, -0.05f), V(0.06f, 0.05f, 0.17f), 10, 0.08f, 0.02f, cream, -0.9f, -0.6f, 0.9f);
        });
        int tail = b.Tail(V(0, 0.24f, -0.21f));
        b.Spike(tail, V(0, 0.24f, -0.21f), V(0, 0.28f, -0.3f), 0.03f, cream);
        // A big head, a fringe over its eyes and a moustache hanging to the ground
        int head = b.Head(V(0, 0.26f, 0.16f));
        var c = V(0, 0.28f, 0.22f);
        var r = V(0.08f, 0.075f, 0.08f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.015f, 0.07f), V(0.04f, 0.035f, 0.04f), brown, blend: 0.02f);
        b.Ell(head, c + V(0, -0.005f, 0.11f), V(0.015f, 0.012f, 0.01f), Black, mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, c + V(0.015f * s, -0.035f, 0.1f), c + V(0.07f * s, -0.07f, 0.1f), c + V(0.1f * s, -0.14f, 0.06f), c + V(0.1f * s, -0.2f, 0.05f)), 0.025f, 0.012f, cream, blend: 0f);
            Blade(b, head, c + V(0.03f * s, 0.05f, 0.05f), c + V(0.09f * s, 0.04f, 0.1f), 0.02f, cream, V(0, 1f, 0.2f), 0.3f);
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.06f, -0.01f));
            CatEar(b, ear, c + V(0.05f * s, 0.06f, -0.01f), c + V(0.1f * s, 0.12f, -0.04f), 0.026f, brown, cream);
            var at = On(c, r, 0.035f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 34, 30), glare: true);
        });
        FurTufts(b, head, c + V(0, 0.05f, 0.02f), V(0.05f, 0.025f, 0.04f), 6, 0.05f, 0.014f, cream, 0.2f, -0.2f, 0.6f);
        return b;
    }

    // ------------------------------------------------------------------ Purrloin line

    /// <summary>Purrloin: a slinky purple kitten sitting, a cream face and chest, pink ears, green eyes and a long tail with a hooked tip.</summary>
    private static PokeBuilder Purrloin()
    {
        var b = new PokeBuilder("Purrloin", 0.55f, BodyPlan.Quadruped, V(0, 0.16f, 0)) { Coat = Fur };
        var purple = Rgb(130, 90, 170);
        var cream = Rgb(244, 230, 180);
        var pink = Rgb(220, 120, 170);
        foreach (var (z, front) in new[] { (0.05f, true), (-0.04f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.04f * s, front ? 0.13f : 0.08f, z), front);
                b.Limb(leg, V(0.04f * s, front ? 0.14f : 0.09f, z), V(0.045f * s, 0.02f, z + 0.02f), front ? 0.016f : 0.03f, 0.014f, purple);
                b.Ell(leg, V(0.045f * s, 0.014f, z + 0.03f), V(0.018f, 0.014f, 0.028f), cream);
            });
        var bc = V(0, 0.13f, -0.02f);
        var br = V(0.06f, 0.08f, 0.07f);
        b.Ell(Body, bc, br, purple, V(30f, 0, 0));
        b.PaintEll(Body, V(0, 0.16f, 0.04f), V(0.035f, 0.06f, 0.03f), cream);
        int tail = b.Tail(V(0, 0.07f, -0.07f));
        var tp = Smooth(3, V(0, 0.07f, -0.07f), V(0.06f, 0.04f, -0.14f), V(0.14f, 0.1f, -0.16f), V(0.16f, 0.2f, -0.12f));
        b.Tube(tail, tp, 0.014f, 0.012f, purple, blend: 0f);
        Blade(b, tail, tp[^1], tp[^1] + V(0.04f, 0.0f, 0.03f), 0.02f, purple, V(0, 0, 1f), 0.3f);
        int head = b.Head(V(0, 0.21f, 0.02f));
        var c = V(0, 0.26f, 0.03f);
        var r = V(0.07f, 0.06f, 0.06f);
        b.Ell(head, c, r, purple);
        b.PaintEll(head, c + V(0, -0.02f, 0.04f), V(0.05f, 0.035f, 0.03f), cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.04f, -0.01f));
            CatEar(b, ear, c + V(0.04f * s, 0.04f, -0.01f), c + V(0.1f * s, 0.12f, -0.02f), 0.03f, purple, pink);
            var at = On(c, r, 0.03f * s, 0.265f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(90, 190, 120), glare: true);
        });
        b.Mark(head, On(c, r, 0, 0.245f), V(0, -0.3f, 1f), 0.014f, 0.006f, Rgb(120, 80, 90), MarkShape.Smile);
        return b;
    }

    /// <summary>Liepard: a long lithe purple leopard, yellow rosettes along its back, a yellow chest and paws, a pink mask over green eyes and a long curling tail.</summary>
    private static PokeBuilder Liepard()
    {
        var b = new PokeBuilder("Liepard", 0.85f, BodyPlan.Quadruped, V(0, 0.28f, 0)) { Coat = Fur };
        var purple = Rgb(110, 80, 150);
        var yellow = Rgb(244, 210, 100);
        var pink = Rgb(200, 100, 150);
        foreach (var (z, front) in new[] { (0.13f, true), (-0.15f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.24f, z), front);
                var knee = V(0.055f * s, 0.13f, z + (front ? 0.01f : -0.03f));
                b.Limb(leg, V(0.05f * s, 0.26f, z), knee, 0.034f, 0.022f, purple);
                b.Limb(leg, knee, V(0.055f * s, 0.03f, z + 0.01f), 0.02f, 0.016f, yellow);
                b.Ell(leg, V(0.055f * s, 0.016f, z + 0.025f), V(0.02f, 0.016f, 0.03f), yellow);
            });
        var bc = V(0, 0.28f, -0.01f);
        var br = V(0.07f, 0.065f, 0.19f);
        b.Ell(Body, bc, br, purple);
        b.PaintEll(Body, V(0, 0.24f, 0.08f), V(0.05f, 0.04f, 0.1f), yellow);
        foreach (var (x, z) in new[] { (0.05f, 0.08f), (-0.05f, 0.04f), (0.05f, -0.04f), (-0.05f, -0.08f), (0.04f, -0.13f), (-0.03f, 0.12f) })
        {
            var n = V(x / br.X, 0.6f, z / br.Z);
            b.Mark(Body, Out(bc, br, default, n), n, 0.016f, 0.014f, yellow, MarkShape.Ring);
        }
        int tail = b.Tail(V(0, 0.3f, -0.19f));
        var tp = Smooth(3, V(0, 0.3f, -0.19f), V(0, 0.42f, -0.3f), V(0.06f, 0.52f, -0.28f), V(0.04f, 0.5f, -0.2f));
        b.Tube(tail, tp, 0.018f, 0.012f, purple, blend: 0f);
        Blade(b, tail, tp[^1], tp[^1] + V(-0.02f, -0.04f, 0.04f), 0.025f, purple, V(1f, 0, 0), 0.3f);
        int head = b.Head(V(0, 0.34f, 0.17f));
        var c = V(0, 0.39f, 0.22f);
        var r = V(0.05f, 0.045f, 0.06f);
        b.Limb(head, V(0, 0.31f, 0.15f), c, 0.035f, 0.03f, purple);
        b.Ell(head, c, r, purple);
        b.PaintEll(head, c + V(0, -0.025f, 0.03f), V(0.04f, 0.02f, 0.05f), yellow);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.035f, -0.01f));
            CatEar(b, ear, c + V(0.03f * s, 0.035f, -0.01f), c + V(0.07f * s, 0.09f, -0.04f), 0.02f, purple, pink);
            var at = On(c, r, 0.025f * s, c.Y + 0.008f);
            b.Mark(head, at, Outward(c, r, at), 0.02f, 0.012f, pink, rollDeg: -20f * s);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(90, 190, 120), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ the elemental monkeys

    /// <summary>What crowns an elemental monkey's head and tips its tail.</summary>
    private enum Element { Grass, Fire, Water }

    /// <summary>
    /// Pansage, Pansear and Panpour and their evolutions: a monkey with a cream face, hands, belly and feet, big round
    /// ears and a crest and tail tip of its element: a tuft like broccoli and a leaf (Grass), a curl of flame (Fire),
    /// a rounded wave of water (Water); the evolved taller, a white ruff at the chest, the crest grown into a tall
    /// leaf, a mane of fire or long flowing water, and the tail bushy with it.
    /// </summary>
    private static PokeBuilder ElementMonkey(string name, Element element, bool evolved)
    {
        var b = new PokeBuilder(name, evolved ? 0.85f : 0.6f, BodyPlan.Biped, V(0, evolved ? 0.3f : 0.2f, 0)) { Coat = Fur };
        var main = element switch { Element.Grass => Rgb(90, 170, 100), Element.Fire => Rgb(222, 90, 80), _ => Rgb(80, 170, 220) };
        var dark = PixelCanvas.Mix(main, Black, 0.2f);
        var cream = Rgb(244, 222, 160);
        var inner = element switch { Element.Grass => Rgb(150, 210, 150), Element.Fire => Rgb(246, 170, 80), _ => Rgb(150, 210, 236) };
        float k = evolved ? 1.4f : 1f;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s * k, 0.1f * k, 0));
            b.Limb(leg, V(0.04f * s * k, 0.11f * k, 0), V(0.05f * s * k, 0.03f, 0.01f), 0.02f * k, 0.017f * k, element == Element.Fire ? main : main);
            b.Ell(leg, V(0.05f * s * k, 0.02f, 0.03f), V(0.022f * k, 0.02f, 0.035f * k), cream);
        });
        var bc = V(0, 0.17f * k, 0);
        var br = V(0.06f * k, 0.075f * k, 0.05f * k);
        b.Ell(Body, bc, br, cream);
        b.PaintEll(Body, bc + V(0, -0.05f * k, 0), V(0.07f * k, 0.04f * k, 0.06f * k), main);
        if (evolved)
            FurTufts(b, Body, bc + V(0, 0.06f, 0.02f), V(0.05f, 0.03f, 0.04f), 7, 0.04f, 0.014f, White, 0.3f, -0.6f, 0.6f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s * k, 0.21f * k, 0.01f));
            var hand = s > 0 ? V(0.12f * s * k, 0.27f * k, 0.04f) : V(0.12f * s * k, 0.13f * k, 0.05f);
            b.Limb(arm, V(0.05f * s * k, 0.21f * k, 0.01f), hand, 0.014f * k, 0.013f * k, main);
            b.Ell(arm, hand, V(0.02f * k, 0.018f * k, 0.018f * k), cream);
        });
        // A long tail curving up, its tip the element's
        int tail = b.Tail(V(0, 0.1f * k, -0.04f * k));
        var tp = Smooth(3, V(0, 0.13f * k, -0.03f * k), V(0.06f * k, 0.06f * k, -0.1f * k), V(0.12f * k, 0.12f * k, -0.1f * k), V(0.12f * k, 0.2f * k, -0.06f * k));
        b.Tube(tail, tp, 0.011f * k, 0.01f * k, main, blend: 0f);
        var tip = tp[^1];
        switch (element)
        {
            case Element.Grass:
                foreach (var o in new[] { V(0, 0.025f, 0), V(0.024f, 0.008f, 0), V(-0.024f, 0.008f, 0) })
                    b.Ell(tail, tip + o * k, V(0.022f, 0.022f, 0.016f) * k, main, blend: 0.012f);
                break;
            case Element.Fire:
                FlameTongue(b, tail, tip, tip + V(0.01f, 0.07f, -0.01f) * k, 0.025f * k, main, Rgb(250, 180, 90));
                break;
            default:
                foreach (var o in new[] { V(0, 0.03f, 0), V(0.03f, 0.01f, 0), V(-0.03f, 0.01f, 0), V(0, 0.0f, 0.03f) })
                    b.Ell(tail, tip + o * k, V(0.02f, 0.02f, 0.02f) * k, inner, blend: 0.01f);
                break;
        }
        // A round head, a cream face and muzzle, big round ears and the element's crest
        int head = b.Head(V(0, 0.24f * k, 0.01f));
        var c = V(0, 0.31f * k, 0.02f);
        var r = V(0.075f, 0.068f, 0.065f) * k;
        b.Ell(head, c, r, main);
        b.PaintEll(head, c + V(0, -0.015f, 0.04f) * k, V(0.06f, 0.045f, 0.035f) * k, cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.065f * s, 0.01f, 0) * k);
            var ec = c + V(0.082f * s, 0.015f, -0.005f) * k;
            b.Ell(ear, ec, V(0.014f, 0.035f, 0.032f) * k, main, blend: 0.012f);
            b.PaintEll(ear, ec + V(0.012f * s, 0, 0) * k, V(0.008f, 0.024f, 0.022f) * k, inner);
            var at = On(c, r, 0.025f * s * k, c.Y + 0.008f * k);
            b.Eye(head, at, Outward(c, r, at), 0.012f * k, element == Element.Water ? (Color?)null : Rgb(40, 30, 30), sclera: element == Element.Water, pupil: Rgb(40, 30, 30), glare: evolved);
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.035f * k), V(0, -0.3f, 1f), 0.016f * k, 0.007f * k, Rgb(150, 90, 70), MarkShape.Smile);
        var top = c + V(0, 0.06f, -0.01f) * k;
        switch (element)
        {
            case Element.Grass:
                if (evolved)
                {
                    b.Spike(head, top, top + V(0, 0.16f, -0.04f), 0.05f, main, 0.6f, Leaf);
                    b.PaintEll(head, top + V(0, 0.1f, -0.02f), V(0.03f, 0.05f, 0.04f), dark);
                }
                else
                    foreach (var o in new[] { V(0, 0.05f, 0), V(0.035f, 0.035f, 0), V(-0.035f, 0.035f, 0), V(0, 0.04f, -0.03f) })
                        b.Ell(head, top + o, V(0.035f, 0.03f, 0.03f), main, blend: 0.012f);
                break;
            case Element.Fire:
                if (evolved)
                    FlameRow(b, head, top + V(-0.05f, 0, 0), top + V(0.05f, 0, 0), V(0, 1f, -0.3f), 5, 0.1f, 0.025f, main, Rgb(250, 160, 90), 1.6f);
                else
                    Curl(b, head, top, top + V(0, 0.05f, 0.01f), V(0, 0, 1f), V(0, 1f, 0), 0.03f, 0.8f, 0.03f, main);
                break;
            default:
                if (evolved)
                    PokeBuilder.Both(s => b.Tube(head, Smooth(3, top + V(0.03f * s, 0.02f, 0), top + V(0.1f * s, 0.0f, -0.02f), top + V(0.12f * s, -0.12f, -0.02f), top + V(0.1f * s, -0.2f, 0.0f)), 0.03f, 0.024f, main, blend: 0f));
                foreach (var o in new[] { V(0, 0.04f, 0), V(0.04f, 0.025f, 0), V(-0.04f, 0.025f, 0) })
                    b.Ell(head, top + o * k, V(0.035f, 0.03f, 0.03f) * k, main, blend: 0.012f);
                break;
        }
        return b;
    }

    private static PokeBuilder Pansage() => ElementMonkey("Pansage", Element.Grass, false);

    private static PokeBuilder Simisage() => ElementMonkey("Simisage", Element.Grass, true);

    private static PokeBuilder Pansear() => ElementMonkey("Pansear", Element.Fire, false);

    private static PokeBuilder Simisear() => ElementMonkey("Simisear", Element.Fire, true);

    private static PokeBuilder Panpour() => ElementMonkey("Panpour", Element.Water, false);

    private static PokeBuilder Simipour() => ElementMonkey("Simipour", Element.Water, true);

    // ------------------------------------------------------------------ Munna line

    /// <summary>Munna: a round pink dream-eater floating, purple flowers on its sides, a little snout and red eyes on its sides with lashes.</summary>
    private static PokeBuilder Munna()
    {
        var b = new PokeBuilder("Munna", 0.5f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Fur }.Hover();
        var pink = Rgb(246, 200, 214);
        var purple = Rgb(176, 140, 200);
        var c = V(0, 0.2f, 0);
        var r = V(0.13f, 0.12f, 0.12f);
        b.Ell(Body, c, r, pink);
        foreach (var (x, y, z) in new[] { (0.5f, 0.4f, -0.6f), (-0.5f, 0.4f, -0.6f), (0.7f, -0.3f, -0.5f), (-0.7f, -0.3f, -0.5f), (0f, 0.8f, -0.4f) })
        {
            var n = V(x, y, z);
            b.Mark(Body, Out(c, r, default, n), n, 0.04f, 0.04f, purple, MarkShape.Star5);
            b.Mark(Body, Out(c, r, default, n), n, 0.018f, 0.018f, Rgb(240, 160, 190));
        }
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, c + V(0.06f * s, -0.1f, 0.03f));
            b.Ell(leg, c + V(0.06f * s, -0.11f, 0.03f), V(0.022f, 0.02f, 0.022f), pink, blend: 0.012f);
            var look = V(0.75f * s, 0.15f, 0.6f);
            b.Eye(Body, Out(c, r, default, look), look, 0.018f, Rgb(220, 50, 70), glare: false);
        });
        int head = b.Head(c + V(0, 0, 0.1f));
        b.Ell(head, c + V(0, -0.02f, 0.12f), V(0.035f, 0.035f, 0.03f), pink, blend: 0.015f);
        b.Mark(head, c + V(0, -0.02f, 0.15f), V(0, 0, 1f), 0.022f, 0.022f, Rgb(240, 150, 180));
        return Lift(b);
    }

    /// <summary>Musharna: a sleeping capsule of a Pokémon, pink above with purple flowers and dark purple below, a pink mist of dreams curling from its brow.</summary>
    private static PokeBuilder Musharna()
    {
        var b = new PokeBuilder("Musharna", 0.75f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Fur }.Hover();
        var pink = Rgb(246, 196, 214);
        var purple = Rgb(140, 110, 180);
        var mist = Rgb(240, 150, 190);
        var c = V(0, 0.22f, 0);
        var r = V(0.12f, 0.12f, 0.17f);
        b.Ell(Body, c, r, purple);
        b.PaintEll(Body, c + V(0, 0.06f, 0.06f), V(0.13f, 0.1f, 0.14f), pink);
        foreach (var (x, z) in new[] { (0.7f, 0.2f), (-0.7f, 0.2f), (0.4f, -0.5f), (-0.4f, -0.5f) })
        {
            var n = V(x, 0.5f, z);
            b.Mark(Body, Out(c, r, default, n), n, 0.035f, 0.035f, Rgb(200, 160, 210), MarkShape.Star5);
        }
        int head = b.Head(c + V(0, 0.04f, 0.14f));
        b.Ell(head, c + V(0, 0.0f, 0.17f), V(0.05f, 0.05f, 0.03f), pink, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.4f * s, 0.25f, 0.9f));
            b.Eye(Body, at, V(0.4f * s, 0.25f, 0.9f), 0.016f, Rgb(80, 50, 80), closed: true);
            int leg = b.Leg(s, c + V(0.07f * s, -0.1f, 0.05f));
            b.Ell(leg, c + V(0.07f * s, -0.11f, 0.06f), V(0.025f, 0.02f, 0.025f), purple, blend: 0.012f);
        });
        // The dream mist rising from its brow and curling back over it
        int tail = b.Tail(c + V(0, 0.11f, 0.05f));
        var tp = Smooth(3, c + V(0, 0.1f, 0.06f), c + V(0.02f, 0.2f, 0.04f), c + V(-0.04f, 0.26f, -0.06f), c + V(0.02f, 0.3f, -0.16f), c + V(0.08f, 0.26f, -0.22f));
        b.Tube(tail, tp, 0.03f, 0.022f, mist, blend: 0.01f);
        foreach (int i in new[] { 3, 6, 9 })
            b.Ell(tail, tp[i] + V(0.015f, 0.01f, 0), V(0.03f, 0.03f, 0.03f), mist, blend: 0.012f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Pidove line

    /// <summary>Pidove: a plump grey pigeon, a pale chest, a short dark beak, round yellow eyes and black bands on its wings and tail.</summary>
    private static PokeBuilder Pidove()
    {
        var b = new PokeBuilder("Pidove", 0.5f, BodyPlan.Bird, V(0, 0.18f, 0)) { Coat = Fur };
        var gray = Rgb(150, 150, 160);
        var pale = Rgb(214, 214, 220);
        var black = Rgb(60, 60, 70);
        var pink = Rgb(230, 150, 160);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.08f, 0.01f));
            b.Limb(leg, V(0.035f * s, 0.09f, 0.01f), V(0.04f * s, 0.02f, 0.02f), 0.01f, 0.008f, pink);
            Digits(b, leg, V(0.04f * s, 0.012f, 0.025f), V(0, 0, 1f), V(0.7f, 0, 0), 0.03f, 0.007f, pink);
        });
        var bc = V(0, 0.17f, 0);
        var br = V(0.085f, 0.085f, 0.1f);
        b.Ell(Body, bc, br, gray);
        b.PaintEll(Body, V(0, 0.16f, 0.06f), V(0.065f, 0.065f, 0.06f), pale);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.07f * s, 0.19f, 0));
            var wc = V(0.08f * s, 0.16f, -0.03f);
            b.Ell(wing, wc, V(0.025f, 0.055f, 0.075f), gray, V(15f, 0, 0), blend: 0.015f);
            foreach (float z in new[] { -0.05f, -0.08f })
                b.PaintEll(wing, wc + V(0.02f * s, 0, z + 0.03f), V(0.03f, 0.06f, 0.01f), black);
        });
        int tail = b.Tail(V(0, 0.14f, -0.09f));
        Frond(b, tail, V(0, 0.14f, -0.09f), V(0, 0.12f, -0.18f), 0.04f, black, V(0, 1f, 0), 0.3f);
        int head = b.Head(V(0, 0.22f, 0.04f));
        var c = V(0, 0.27f, 0.05f);
        var r = V(0.065f, 0.06f, 0.06f);
        b.Ell(head, c, r, gray);
        b.Ell(head, c + V(0, -0.01f, 0.065f), V(0.016f, 0.012f, 0.014f), pink, blend: 0.008f);
        b.Spike(head, c + V(0, -0.015f, 0.065f), c + V(0, -0.025f, 0.1f), 0.014f, black, 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.008f);
            b.Mark(head, at, Outward(c, r, at), 0.02f, 0.02f, Rgb(246, 196, 70));
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(40, 30, 30));
        });
        return b;
    }

    /// <summary>Tranquill: a grey dove with a black head and crest, a pink patch round its eyes, black-banded wings and a black tail.</summary>
    private static PokeBuilder Tranquill()
    {
        var b = new PokeBuilder("Tranquill", 0.7f, BodyPlan.Bird, V(0, 0.26f, 0)) { Coat = Fur };
        var gray = Rgb(160, 160, 170);
        var pale = Rgb(220, 220, 226);
        var black = Rgb(56, 56, 66);
        var pink = Rgb(220, 120, 140);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.15f, 0.01f));
            b.Limb(leg, V(0.035f * s, 0.16f, 0.01f), V(0.04f * s, 0.02f, 0.02f), 0.011f, 0.008f, pink);
            Digits(b, leg, V(0.04f * s, 0.012f, 0.025f), V(0, 0, 1f), V(0.7f, 0, 0), 0.035f, 0.007f, pink);
        });
        var bc = V(0, 0.25f, 0);
        var br = V(0.08f, 0.09f, 0.11f);
        b.Ell(Body, bc, br, gray, V(-15f, 0, 0));
        b.PaintEll(Body, V(0, 0.25f, 0.07f), V(0.06f, 0.07f, 0.05f), pale);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.07f * s, 0.29f, 0.01f));
            var wc = V(0.08f * s, 0.25f, -0.04f);
            b.Ell(wing, wc, V(0.025f, 0.06f, 0.1f), gray, V(20f, 0, 0), blend: 0.015f);
            b.PaintEll(wing, wc + V(0.02f * s, -0.02f, -0.06f), V(0.03f, 0.06f, 0.04f), black);
        });
        int tail = b.Tail(V(0, 0.21f, -0.1f));
        Frond(b, tail, V(0, 0.21f, -0.1f), V(0, 0.16f, -0.26f), 0.045f, black, V(0, 1f, 0), 0.3f);
        int head = b.Head(V(0, 0.32f, 0.06f));
        var c = V(0, 0.38f, 0.08f);
        var r = V(0.055f, 0.055f, 0.055f);
        b.Limb(head, V(0, 0.31f, 0.05f), c, 0.035f, 0.032f, gray);
        b.Ell(head, c, r, black);
        b.Spike(head, c + V(0, 0.03f, -0.02f), c + V(0, 0.09f, -0.06f), 0.02f, black, 0.5f);
        b.Spike(head, c + V(0, -0.005f, 0.05f), c + V(0, -0.015f, 0.1f), 0.014f, Rgb(240, 200, 80), 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.005f);
            b.Mark(head, at, Outward(c, r, at), 0.02f, 0.016f, pink);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(240, 200, 70), glare: true);
        });
        return b;
    }

    /// <summary>Unfezant: a proud dark-grey pheasant, a green chest, a long pink plume curling down from its head and a broad banded tail.</summary>
    private static PokeBuilder Unfezant()
    {
        var b = new PokeBuilder("Unfezant", 0.9f, BodyPlan.Bird, V(0, 0.36f, 0)) { Coat = Fur };
        var gray = Rgb(90, 90, 100);
        var green = Rgb(80, 150, 100);
        var pink = Rgb(220, 90, 120);
        var pale = Rgb(170, 170, 180);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.24f, 0.01f));
            b.Limb(leg, V(0.04f * s, 0.25f, 0.01f), V(0.045f * s, 0.02f, 0.02f), 0.012f, 0.009f, pale);
            Digits(b, leg, V(0.045f * s, 0.012f, 0.025f), V(0, 0, 1f), V(0.7f, 0, 0), 0.04f, 0.008f, pale);
        });
        var bc = V(0, 0.34f, 0);
        var br = V(0.085f, 0.1f, 0.13f);
        b.Ell(Body, bc, br, gray, V(-20f, 0, 0));
        b.PaintEll(Body, V(0, 0.32f, 0.08f), V(0.07f, 0.07f, 0.05f), green);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.07f * s, 0.38f, 0.01f));
            var wc = V(0.08f * s, 0.34f, -0.05f);
            b.Ell(wing, wc, V(0.025f, 0.07f, 0.12f), gray, V(20f, 0, 0), blend: 0.015f);
            b.PaintEll(wing, wc + V(0.02f * s, -0.03f, -0.06f), V(0.03f, 0.04f, 0.05f), pale);
        });
        int tail = b.Tail(V(0, 0.3f, -0.12f));
        for (int i = -1; i <= 1; i++)
        {
            var tip = V(0.04f * i, 0.3f, -0.38f);
            Frond(b, tail, V(0, 0.3f, -0.12f), tip, 0.04f, gray, V(0, 1f, 0), 0.25f);
            b.PaintEll(tail, Vector3.Lerp(V(0, 0.3f, -0.12f), tip, 0.7f), V(0.05f, 0.03f, 0.02f), pale);
        }
        int head = b.Head(V(0, 0.42f, 0.07f));
        var c = V(0, 0.49f, 0.1f);
        var r = V(0.05f, 0.05f, 0.055f);
        b.Limb(head, V(0, 0.4f, 0.06f), c, 0.035f, 0.03f, gray);
        b.Ell(head, c, r, gray);
        b.Spike(head, c + V(0, -0.005f, 0.045f), c + V(0, -0.02f, 0.1f), 0.014f, Rgb(240, 200, 80), 0.7f, Shell);
        PokeBuilder.Both(s => b.PaintEll(head, c + V(0.035f * s, 0.0f, 0.02f), V(0.025f, 0.02f, 0.02f), pink));
        b.Tube(head, Smooth(3, c + V(0, 0.04f, -0.02f), c + V(0, 0.14f, -0.04f), c + V(0, 0.16f, 0.06f), c + V(0, 0.06f, 0.1f), c + V(0, -0.02f, 0.06f)), 0.016f, 0.01f, pink, blend: 0f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(240, 200, 70), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Blitzle line

    /// <summary>Blitzle: a black foal with white bands round its legs and body, a mane of white lightning, blue in its ears and a round black muzzle.</summary>
    private static PokeBuilder Blitzle()
    {
        var b = new PokeBuilder("Blitzle", 0.6f, BodyPlan.Quadruped, V(0, 0.24f, 0)) { Coat = Fur };
        var black = Rgb(56, 56, 66);
        var white = Rgb(240, 240, 244);
        var blue = Rgb(100, 180, 230);
        foreach (var (z, front) in new[] { (0.07f, true), (-0.08f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.04f * s, 0.2f, z), front);
                b.Limb(leg, V(0.04f * s, 0.22f, z), V(0.045f * s, 0.03f, z + 0.01f), 0.022f, 0.016f, black);
                b.PaintTorus(leg, V(0.045f * s, 0.1f, z), 0.018f, 0.008f, white);
                b.Ell(leg, V(0.045f * s, 0.02f, z + 0.01f), V(0.018f, 0.02f, 0.02f), white);
            });
        var bc = V(0, 0.25f, 0);
        var br = V(0.06f, 0.06f, 0.11f);
        b.Ell(Body, bc, br, black);
        foreach (float z in new[] { 0.04f, -0.03f })
            b.PaintTorus(Body, V(0, 0.25f, z), 0.058f, 0.012f, white, V(90f, 0, 0));
        int tail = b.Tail(V(0, 0.27f, -0.1f));
        b.Spike(tail, V(0, 0.27f, -0.1f), V(0, 0.22f, -0.17f), 0.02f, black);
        int head = b.Head(V(0, 0.32f, 0.08f));
        var c = V(0, 0.36f, 0.11f);
        var r = V(0.055f, 0.055f, 0.06f);
        b.Limb(head, V(0, 0.27f, 0.06f), c, 0.035f, 0.035f, black);
        b.Ell(head, c, r, black);
        b.Ell(head, c + V(0, -0.02f, 0.06f), V(0.035f, 0.03f, 0.03f), black, blend: 0.02f);
        // A mane of white lightning over its head and down its neck
        b.Tube(head, Smooth(1, c + V(0, 0.04f, 0.02f), c + V(0, 0.1f, -0.01f), c + V(0.0f, 0.08f, -0.04f), c + V(0, 0.14f, -0.07f)), 0.016f, 0.008f, white, Shell, 0f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.035f * s, 0.04f, -0.01f));
            CatEar(b, ear, c + V(0.035f * s, 0.04f, -0.01f), c + V(0.06f * s, 0.1f, -0.03f), 0.018f, black, blue);
            var at = On(c, r, 0.03f * s, c.Y + 0.005f);
            b.Mark(head, at, Outward(c, r, at), 0.016f, 0.016f, Rgb(246, 210, 80));
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(80, 160, 220));
        });
        return b;
    }

    /// <summary>Zebstrika: a tall black zebra, white zigzag stripes over its body and legs, a mane of white lightning down its neck and a lightning tail.</summary>
    private static PokeBuilder Zebstrika()
    {
        var b = new PokeBuilder("Zebstrika", 0.9f, BodyPlan.Quadruped, V(0, 0.38f, 0)) { Coat = Fur };
        var black = Rgb(56, 56, 66);
        var white = Rgb(240, 240, 244);
        var blue = Rgb(100, 180, 230);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.34f, z), front);
                var knee = V(0.055f * s, 0.18f, z + (front ? 0.01f : -0.03f));
                b.Limb(leg, V(0.05f * s, 0.36f, z), knee, 0.035f, 0.022f, black);
                b.Limb(leg, knee, V(0.055f * s, 0.03f, z + 0.01f), 0.02f, 0.016f, black);
                foreach (float t in new[] { 0.3f, 0.65f })
                    b.PaintTorus(leg, Vector3.Lerp(knee, V(0.055f * s, 0.03f, z + 0.01f), t), 0.02f, 0.007f, white);
                b.Ell(leg, V(0.055f * s, 0.02f, z + 0.01f), V(0.02f, 0.022f, 0.024f), Rgb(120, 120, 130));
            });
        var bc = V(0, 0.4f, 0);
        var br = V(0.07f, 0.075f, 0.18f);
        b.Ell(Body, bc, br, black);
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.08f, -0.02f, -0.1f })
                b.Mark(Body, Out(bc, br, default, V(s, 0.1f, z / br.Z)), V(s, 0.1f, z / br.Z), 0.03f, 0.05f, white, MarkShape.Zigzag, 90f);
        });
        // A lightning tail and a mane of lightning down its neck
        int tail = b.Tail(V(0, 0.43f, -0.17f));
        b.Tube(tail, Smooth(1, V(0, 0.43f, -0.17f), V(0.01f, 0.45f, -0.24f), V(-0.02f, 0.4f, -0.28f), V(0.01f, 0.42f, -0.34f)), 0.016f, 0.008f, white, Shell, 0f);
        int head = b.Head(V(0, 0.5f, 0.15f));
        b.Limb(head, V(0, 0.45f, 0.12f), V(0, 0.6f, 0.2f), 0.045f, 0.035f, black);
        for (int i = 0; i < 5; i++)
        {
            var root = Vector3.Lerp(V(0, 0.48f, 0.1f), V(0, 0.64f, 0.18f), i / 4f);
            b.Spike(head, root, root + V(0, 0.06f, -0.07f), 0.015f, white, 0.4f, Shell);
        }
        var c = V(0, 0.64f, 0.23f);
        var r = V(0.045f, 0.045f, 0.07f);
        b.Ell(head, c, r, black, V(25f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.04f, -0.02f));
            CatEar(b, ear, c + V(0.03f * s, 0.04f, -0.02f), c + V(0.05f * s, 0.1f, -0.04f), 0.016f, black, blue);
            var at = On(c, r, 0.025f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(80, 160, 220), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Roggenrola line

    /// <summary>Roggenrola: a navy round of stone on two brown stone feet, a brown rock standing on its head and a yellow hexagon of an ear on its front.</summary>
    private static PokeBuilder Roggenrola()
    {
        var b = new PokeBuilder("Roggenrola", 0.5f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Shell };
        var navy = Rgb(60, 70, 120);
        var brown = Rgb(150, 110, 70);
        var yellow = Rgb(230, 180, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.06f, 0));
            b.Box(leg, V(0.06f * s, 0.025f, 0.01f), V(0.035f, 0.022f, 0.03f), 0.008f, brown, V(0, 0, -20f * s), blend: 0.006f);
            b.Limb(leg, V(0.04f * s, 0.08f, 0), V(0.055f * s, 0.035f, 0.01f), 0.016f, 0.016f, brown);
        });
        var bc = V(0, 0.15f, 0);
        b.Box(Body, bc, V(0.09f, 0.09f, 0.08f), 0.035f, navy, V(0, 20f, 0));
        int head = b.Head(bc + V(0, 0.08f, 0));
        b.Box(head, bc + V(0, 0.14f, 0), V(0.025f, 0.06f, 0.02f), 0.01f, brown, V(0, 0, 8f), blend: 0.01f);
        b.Mark(Body, bc + V(0, 0, 0.09f), V(0, 0, 1f), 0.04f, 0.045f, yellow, MarkShape.Diamond);
        b.Mark(Body, bc + V(0, 0, 0.091f), V(0, 0, 1f), 0.02f, 0.022f, Rgb(40, 40, 50), MarkShape.Diamond);
        return b;
    }

    /// <summary>Boldore: a navy body of stone on four short stone legs, orange crystals jutting from it, a yellow hexagon on its front.</summary>
    private static PokeBuilder Boldore()
    {
        var b = new PokeBuilder("Boldore", 0.75f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Shell };
        var navy = Rgb(60, 70, 120);
        var orange = Rgb(230, 110, 70);
        var yellow = Rgb(230, 180, 60);
        foreach (var (z, front) in new[] { (0.06f, true), (-0.06f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.12f * s, 0.12f, z), front);
                b.Box(leg, V(0.15f * s, 0.07f, z), V(0.05f, 0.07f, 0.05f), 0.02f, navy, V(0, 15f * s, -10f * s), blend: 0.01f);
                b.Spike(leg, V(0.17f * s, 0.03f, z), V(0.2f * s, -0.0f, z + 0.02f), 0.02f, orange, mat: Glow, blend: 0.006f);
            });
        var bc = V(0, 0.22f, 0);
        b.Box(Body, bc, V(0.12f, 0.1f, 0.1f), 0.04f, navy);
        b.Spike(Body, bc + V(0, 0.06f, 0.0f), bc + V(0, 0.16f, -0.02f), 0.05f, navy, 0.6f);
        foreach (var (o, d) in new[] { (V(0.08f, 0.08f, 0), V(0.05f, 0.06f, 0)), (V(-0.08f, 0.08f, 0), V(-0.05f, 0.06f, 0)), (V(0.04f, -0.08f, 0.08f), V(0.0f, -0.06f, 0.02f)), (V(-0.04f, -0.08f, 0.08f), V(0.0f, -0.06f, 0.02f)), (V(0.1f, 0.0f, -0.06f), V(0.06f, 0.02f, -0.02f)), (V(-0.1f, 0.0f, -0.06f), V(-0.06f, 0.02f, -0.02f)) })
            b.Spike(Body, bc + o, bc + o + d, 0.022f, orange, mat: Glow, blend: 0.006f);
        int head = b.Head(bc + V(0, 0.1f, 0));
        b.Ell(head, bc + V(0, 0.12f, -0.02f), V(0.04f, 0.04f, 0.04f), navy, blend: 0.02f);
        b.Mark(Body, bc + V(0, 0.02f, 0.101f), V(0, 0, 1f), 0.035f, 0.04f, yellow, MarkShape.Diamond);
        b.Mark(Body, bc + V(0, 0.02f, 0.102f), V(0, 0, 1f), 0.016f, 0.02f, Rgb(40, 40, 50), MarkShape.Diamond);
        return b;
    }

    /// <summary>Gigalith: a great navy golem of stone, red crystals spiking from its back, shoulders and legs, a crest over yellow eyes.</summary>
    private static PokeBuilder Gigalith()
    {
        var b = new PokeBuilder("Gigalith", 1f, BodyPlan.Quadruped, V(0, 0.32f, 0)) { Coat = Shell };
        var navy = Rgb(56, 64, 110);
        var red = Rgb(220, 80, 70);
        foreach (var (z, front) in new[] { (0.1f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.14f * s, 0.2f, z), front);
                b.Box(leg, V(0.17f * s, 0.12f, z + 0.02f), V(0.06f, 0.12f, 0.06f), 0.025f, navy, V(0, 10f * s, -8f * s), blend: 0.01f);
                b.Spike(leg, V(0.21f * s, 0.18f, z), V(0.26f * s, 0.24f, z - 0.02f), 0.025f, red, mat: Glow, blend: 0.006f);
                b.Spike(leg, V(0.19f * s, 0.04f, z + 0.05f), V(0.21f * s, 0.0f, z + 0.1f), 0.022f, red, mat: Glow, blend: 0.006f);
            });
        var bc = V(0, 0.34f, 0);
        b.Box(Body, bc, V(0.15f, 0.13f, 0.13f), 0.05f, navy);
        foreach (var x in new[] { -0.08f, -0.03f, 0.03f, 0.08f })
            b.Spike(Body, bc + V(x, 0.1f, -0.04f), bc + V(x * 1.4f, 0.26f - MathF.Abs(x), -0.08f), 0.03f, red, mat: Glow, blend: 0.006f);
        int head = b.Head(bc + V(0, 0.06f, 0.1f));
        var c = bc + V(0, 0.02f, 0.13f);
        b.Ell(head, c, V(0.05f, 0.05f, 0.04f), navy, blend: 0.02f);
        b.Spike(head, c + V(0, 0.04f, 0.01f), c + V(0, 0.14f, -0.02f), 0.045f, navy, 0.5f);
        PokeBuilder.Both(s =>
        {
            var at = c + V(0.022f * s, -0.005f, 0.037f);
            b.Mark(head, at, V(0.3f * s, 0, 1f), 0.016f, 0.014f, Rgb(30, 30, 40), MarkShape.Diamond);
            b.Eye(head, at, V(0.3f * s, 0, 1f), 0.009f, Rgb(246, 200, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Woobat line

    /// <summary>Woobat: a round ball of pale blue fur flying on black bat wings, a heart-shaped nose, a wide grin with a fang, its eyes shut in its fur.</summary>
    private static PokeBuilder Woobat()
    {
        var b = new PokeBuilder("Woobat", 0.5f, BodyPlan.Bird, V(0, 0.25f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(170, 200, 220);
        var black = Rgb(60, 60, 70);
        var pink = Rgb(230, 150, 160);
        var c = V(0, 0.25f, 0);
        var r = V(0.09f, 0.09f, 0.08f);
        b.Ell(Body, c, r, blue);
        FurTufts(b, Body, c, r, 22, 0.035f, 0.018f, blue, 1.1f, -1f, 0.2f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, c + V(0.07f * s, 0.04f, -0.02f));
            var shoulder = c + V(0.07f * s, 0.04f, -0.02f);
            var wrist = c + V(0.14f * s, 0.1f, -0.03f);
            b.Limb(wing, shoulder, wrist, 0.008f, 0.006f, black);
            Frond(b, wing, shoulder, wrist + V(0.1f * s, 0.02f, 0), 0.05f, black, V(0, 0.2f, 1f), 0.15f);
            Blade(b, wing, wrist, wrist + V(0.12f * s, -0.04f, 0), 0.04f, black, V(0, 0.2f, 1f), 0.2f);
            var at = Out(c, r, default, V(0.4f * s, 0.15f, 0.9f));
            b.Eye(Body, at, V(0.4f * s, 0.15f, 0.9f), 0.01f, Rgb(70, 80, 100), closed: true);
        });
        int head = b.Head(c + V(0, 0.01f, 0.07f));
        b.Ell(head, c + V(0, 0.005f, 0.085f), V(0.035f, 0.03f, 0.02f), pink, blend: 0.01f);
        b.Mark(head, c + V(0, 0.005f, 0.105f), V(0, 0, 1f), 0.018f, 0.016f, Rgb(70, 40, 50), MarkShape.Disc);
        Grin(b, Body, Out(c, r, default, V(0, -0.45f, 0.9f)), V(0.03f, 0.016f, 0.02f), Rgb(230, 120, 140));
        return Lift(b);
    }

    /// <summary>Swoobat: a blue bat with a helmet-shaped head, a heart-shaped nose, a ruff of pale fur, broad black wings and a pink tail coiled like a spring.</summary>
    private static PokeBuilder Swoobat()
    {
        var b = new PokeBuilder("Swoobat", 0.75f, BodyPlan.Bird, V(0, 0.3f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(80, 120, 190);
        var pale = Rgb(170, 200, 220);
        var black = Rgb(60, 60, 70);
        var pink = Rgb(230, 150, 160);
        var bc = V(0, 0.28f, 0);
        var br = V(0.055f, 0.08f, 0.05f);
        b.Ell(Body, bc, br, blue);
        FurTufts(b, Body, bc + V(0, 0.05f, 0), V(0.07f, 0.04f, 0.06f), 14, 0.04f, 0.016f, pale, 1.1f, -0.5f, 0.5f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.05f * s, 0.05f, -0.02f));
            var shoulder = bc + V(0.05f * s, 0.05f, -0.02f);
            var wrist = bc + V(0.16f * s, 0.16f, -0.04f);
            b.Limb(wing, shoulder, wrist, 0.01f, 0.007f, black);
            Frond(b, wing, shoulder, wrist + V(0.12f * s, 0.06f, 0), 0.07f, black, V(0, 0.2f, 1f), 0.12f);
            Blade(b, wing, wrist, wrist + V(0.14f * s, -0.08f, 0), 0.05f, black, V(0, 0.2f, 1f), 0.18f);
            int leg = b.Leg(s, bc + V(0.02f * s, -0.07f, 0));
            b.Limb(leg, bc + V(0.02f * s, -0.07f, 0), bc + V(0.025f * s, -0.12f, 0.01f), 0.01f, 0.008f, black);
        });
        int tail = b.Tail(bc + V(0, -0.08f, -0.01f));
        var coil = Spiral(bc + V(0, -0.14f, -0.02f), V(1f, 0, 0), V(0, 0, 1f), 0.02f, 0.02f, 0f, MathF.Tau * 1.5f, 18).Select((p, i) => p + V(0, -i * 0.002f, 0)).ToArray();
        b.Tube(tail, new[] { bc + V(0, -0.08f, -0.01f) }.Concat(coil).ToArray(), 0.008f, 0.007f, pink, blend: 0f);
        // A head like a helmet, lines down its brow, the heart nose and two little fangs
        int head = b.Head(bc + V(0, 0.08f, 0.01f));
        var c = bc + V(0, 0.13f, 0.02f);
        var r = V(0.055f, 0.065f, 0.055f);
        b.Ell(head, c, r, blue);
        b.Spike(head, c + V(0, 0.04f, -0.01f), c + V(0, 0.1f, -0.03f), 0.035f, blue, 0.7f);
        foreach (var x in new[] { -0.015f, 0.015f })
            b.PaintEll(head, c + V(x, 0.04f, 0.03f), V(0.005f, 0.035f, 0.03f), Rgb(140, 180, 220), soft: 0.005f);
        b.Ell(head, c + V(0, -0.02f, 0.05f), V(0.022f, 0.018f, 0.012f), pink, blend: 0.008f);
        b.Mark(head, c + V(0, -0.02f, 0.063f), V(0, 0, 1f), 0.011f, 0.01f, Rgb(70, 40, 50));
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.012f * s, -0.045f, 0.045f), c + V(0.014f * s, -0.065f, 0.05f), 0.007f, White, mat: Shell, blend: 0.003f);
            var at = On(c, r, 0.03f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(40, 40, 60));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Drilbur line

    /// <summary>Drilbur: a round dark-grey mole, a white face with red streaks, a pink nose and two steel claws on each hand.</summary>
    private static PokeBuilder Drilbur()
    {
        var b = new PokeBuilder("Drilbur", 0.55f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Fur };
        var gray = Rgb(90, 86, 100);
        var white = Rgb(236, 236, 240);
        var steel = Rgb(200, 200, 210);
        var red = Rgb(214, 80, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.05f, 0));
            b.Ell(leg, V(0.055f * s, 0.022f, 0.02f), V(0.03f, 0.022f, 0.04f), Rgb(60, 56, 66));
        });
        var bc = V(0, 0.15f, 0);
        var br = V(0.1f, 0.12f, 0.09f);
        b.Ell(Body, bc, br, gray);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.2f, 0.02f));
            var hand = V(0.15f * s, 0.26f, 0.06f);
            b.Limb(arm, V(0.08f * s, 0.2f, 0.02f), hand, 0.022f, 0.02f, gray);
            foreach (var o in new[] { -0.012f, 0.012f })
                Blade(b, arm, hand + V(0, 0, o), hand + V(0.06f * s, 0.08f, o * 2f + 0.02f), 0.02f, steel, V(0, 0, 1f), 0.4f, Metal);
        });
        // The head is the top of the body: a white mask with red streaks, a pink nose
        int head = b.Head(V(0, 0.22f, 0.02f));
        var c = V(0, 0.25f, 0.03f);
        var r = V(0.075f, 0.06f, 0.07f);
        b.Ell(head, c, r, gray);
        b.Ell(head, c + V(0, 0.0f, 0.04f), V(0.06f, 0.04f, 0.045f), white, blend: 0.015f);
        b.Ell(head, c + V(0, 0.01f, 0.085f), V(0.02f, 0.016f, 0.012f), Rgb(230, 140, 150), blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.045f * s, -0.01f, 0.06f), V(0.015f, 0.004f, 0.02f), red, V(0, 0, 20f * s), 0.004f);
            var at = c + V(0.03f * s, 0.022f, 0.075f);
            b.Eye(head, at, V(0.3f * s, 0.3f, 1f), 0.008f, Rgb(40, 40, 50));
        });
        return b;
    }

    /// <summary>
    /// Excadrill and its Mega Evolution: a dark brown mole with a steel blade over its head and steel claws on its
    /// arms, red streaks on its face and arms; the Mega's claws grown into long drills and its head blade into a
    /// drill banded red.
    /// </summary>
    private static PokeBuilder ExcadrillBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Excadrill-Mega" : "Excadrill", 0.85f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Fur };
        var brown = Rgb(80, 66, 66);
        var steel = Rgb(200, 202, 212);
        var red = Rgb(200, 70, 80);
        var white = Rgb(236, 236, 240);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.1f, 0));
            b.Limb(leg, V(0.06f * s, 0.11f, 0), V(0.07f * s, 0.03f, 0.01f), 0.035f, 0.03f, brown);
            b.Ell(leg, V(0.07f * s, 0.022f, 0.03f), V(0.035f, 0.022f, 0.045f), brown);
        });
        var bc = V(0, 0.22f, 0);
        var br = V(0.12f, 0.15f, 0.11f);
        b.Ell(Body, bc, br, brown);
        foreach (var (x, y) in new[] { (0.06f, 0.26f), (-0.07f, 0.18f) })
            b.PaintEll(Body, V(x, y, 0.09f), V(0.025f, 0.008f, 0.03f), red, V(0, 0, 30f), 0.005f);
        // Arms ending in steel claws; the Mega's a long drill each
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.28f, 0.02f));
            var hand = V(0.19f * s, 0.24f, 0.09f);
            b.Limb(arm, V(0.1f * s, 0.28f, 0.02f), hand, 0.035f, 0.03f, brown);
            if (mega)
            {
                var tip = hand + V(0.06f * s, -0.06f, 0.3f);
                b.Spike(arm, hand, tip, 0.06f, steel, mat: Metal);
                for (int i = 1; i < 5; i++)
                    b.PaintTorus(arm, Vector3.Lerp(hand, tip, i * 0.18f), 0.06f * (1f - i * 0.18f), 0.006f, red, Euler(tip - hand));
            }
            else
                foreach (var o in new[] { -0.02f, 0.02f })
                    Blade(b, arm, hand + V(0, 0, o * 0.5f), hand + V(0.08f * s, -0.04f + o, 0.1f), 0.03f, steel, V(0, 1f, 0), 0.35f, Metal);
        });
        // A head under a steel blade that folds over it like a helmet, a white face streaked red
        int head = b.Head(V(0, 0.34f, 0.04f));
        var c = V(0, 0.38f, 0.07f);
        var r = V(0.07f, 0.06f, 0.07f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.01f, 0.04f), V(0.05f, 0.035f, 0.04f), white, blend: 0.015f);
        b.Ell(head, c + V(0, -0.005f, 0.08f), V(0.015f, 0.012f, 0.01f), Rgb(230, 140, 150), blend: 0.006f);
        if (mega)
        {
            var tip = c + V(0, 0.1f, -0.32f);
            b.Spike(head, c + V(0, 0.04f, 0.0f), tip, 0.08f, steel, mat: Metal);
            for (int i = 1; i < 5; i++)
                b.PaintTorus(head, Vector3.Lerp(c + V(0, 0.04f, 0), tip, i * 0.18f), 0.08f * (1f - i * 0.18f), 0.008f, red, Euler(tip - c));
        }
        else
            PokeBuilder.Both(s => Blade(b, head, c + V(0.035f * s, 0.05f, 0.02f), c + V(0.06f * s, 0.12f, -0.12f), 0.05f, steel, V(s, 0.4f, 0), 0.3f, Metal));
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.04f * s, -0.02f, 0.05f), V(0.012f, 0.004f, 0.02f), red, V(0, 0, 20f * s), 0.004f);
            var at = c + V(0.024f * s, 0.012f, 0.073f);
            b.Eye(head, at, V(0.3f * s, 0.2f, 1f), 0.008f, Rgb(40, 40, 50), glare: true);
        });
        return b;
    }

    private static PokeBuilder Excadrill() => ExcadrillBuild(false);

    // ------------------------------------------------------------------ Audino

    /// <summary>
    /// Audino and its Mega Evolution: a plump pink and cream hearing Pokémon, long curly feelers from its ears, blue
    /// eyes and a fluffy tail; the Mega white with pink edges, its ears grown into frills and its feelers long.
    /// </summary>
    private static PokeBuilder AudinoBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Audino-Mega" : "Audino", 0.75f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Fur };
        var pink = mega ? Rgb(246, 244, 244) : Rgb(240, 160, 180);
        var cream = Rgb(246, 232, 200);
        var edge = Rgb(240, 170, 190);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.1f, 0));
            b.Limb(leg, V(0.05f * s, 0.11f, 0), V(0.06f * s, 0.03f, 0.01f), 0.035f, 0.03f, cream);
            b.Ell(leg, V(0.06f * s, 0.022f, 0.03f), V(0.032f, 0.022f, 0.045f), mega ? edge : cream);
        });
        var bc = V(0, 0.22f, 0);
        var br = V(0.11f, 0.14f, 0.1f);
        b.Ell(Body, bc, br, cream);
        // A coat over its back and shoulders, hemmed round its middle
        b.Ell(Body, V(0, 0.27f, -0.02f), V(0.12f, 0.1f, 0.1f), pink, blend: 0.015f);
        if (mega)
        {
            b.PaintTorus(Body, V(0, 0.19f, 0), 0.1f, 0.012f, edge, sz: 0.9f);
            PokeBuilder.Both(s => b.Ell(Body, V(0.08f * s, 0.12f, -0.02f), V(0.05f, 0.06f, 0.05f), pink, blend: 0.02f));
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.27f, 0.02f));
            var hand = V(0.16f * s, 0.22f, 0.06f);
            b.Limb(arm, V(0.1f * s, 0.27f, 0.02f), hand, 0.025f, 0.02f, pink);
            b.Ell(arm, hand, V(0.022f, 0.022f, 0.022f), cream);
        });
        int tail = b.Tail(V(0, 0.16f, -0.09f));
        b.Ell(tail, V(0, 0.16f, -0.12f), V(0.04f, 0.04f, 0.035f), White, blend: 0.015f);
        // A head with long flopping ears, curly feelers hanging from them, blue eyes
        int head = b.Head(V(0, 0.34f, 0.01f));
        var c = V(0, 0.4f, 0.02f);
        var r = V(0.08f, 0.07f, 0.07f);
        b.Ell(head, c, r, pink);
        b.PaintEll(head, c + V(0, -0.02f, 0.04f), V(0.065f, 0.045f, 0.035f), cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.04f, -0.01f));
            var ec = c + V(0.12f * s, 0.03f, -0.01f);
            b.Ell(ear, ec, V(0.06f, mega ? 0.05f : 0.035f, 0.025f), pink, V(0, 0, -15f * s), blend: 0.02f);
            if (mega) b.PaintEll(ear, ec + V(0.03f * s, 0.02f, 0), V(0.03f, 0.03f, 0.03f), edge);
            var feeler = Smooth(3, c + V(0.06f * s, -0.03f, 0.02f), c + V(0.1f * s, -0.08f, 0.04f), c + V(0.1f * s, mega ? -0.18f : -0.13f, 0.05f), c + V(0.13f * s, mega ? -0.22f : -0.16f, 0.06f));
            b.Tube(ear, feeler, 0.008f, 0.007f, cream, blend: 0f);
            b.Ell(ear, feeler[^1], V(0.018f, 0.018f, 0.018f), cream, blend: 0.005f);
            var at = On(c, r, 0.035f * s, 0.405f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, mega ? Rgb(230, 120, 160) : Rgb(70, 140, 220));
        });
        b.Mark(head, On(c, r, 0, 0.37f), V(0, -0.3f, 1f), 0.016f, 0.008f, Rgb(170, 90, 100), MarkShape.Smile);
        return b;
    }

    private static PokeBuilder Audino() => AudinoBuild(false);
}
