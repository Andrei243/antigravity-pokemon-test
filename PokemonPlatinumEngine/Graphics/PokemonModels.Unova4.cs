using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Unova's fourth and last batch in National
// Pokédex order: Axew (610) to Genesect (649). Their forms are in PokemonModels.Regional.cs and PokemonModels.Megas.cs
// with the other forms. Helpers shared with the earlier batches are in PokemonModels.Sinnoh1.cs to
// PokemonModels.Sinnoh4.cs, PokemonModels.Kanto1.cs to PokemonModels.Kanto3.cs, PokemonModels.Johto1.cs,
// PokemonModels.Johto2.cs, PokemonModels.Hoenn1.cs to PokemonModels.Hoenn3.cs and PokemonModels.Unova1.cs to
// PokemonModels.Unova3.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Axew line

    /// <summary>Axew: a little gray-green dragon with a big head, a dark green crest sweeping up to a point, red eyes and a tusk at each side of its mouth.</summary>
    private static PokeBuilder Axew()
    {
        var b = new PokeBuilder("Axew", 0.55f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Scales };
        var green = Rgb(150, 168, 124);
        var dark = Rgb(80, 102, 64);
        var pale = Rgb(190, 204, 164);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.07f, 0));
            b.Limb(leg, V(0.035f * s, 0.08f, 0), V(0.04f * s, 0.025f, 0.01f), 0.024f, 0.02f, green);
            b.Ell(leg, V(0.042f * s, 0.016f, 0.024f), V(0.024f, 0.016f, 0.032f), green);
            Claws(b, leg, V(0.042f * s, 0.012f, 0.052f), V(0.01f, 0, 0), V(0, -0.2f, 1f), 0.012f, 0.005f);
            int arm = b.Arm(s, V(0.045f * s, 0.15f, 0.02f));
            b.Limb(arm, V(0.045f * s, 0.15f, 0.02f), V(0.062f * s, 0.11f, 0.05f), 0.015f, 0.013f, green);
            b.Ell(arm, V(0.064f * s, 0.105f, 0.055f), V(0.015f, 0.014f, 0.015f), green);
        });
        b.Ell(Body, V(0, 0.12f, 0), V(0.055f, 0.065f, 0.05f), green);
        b.PaintEll(Body, V(0, 0.1f, 0.045f), V(0.035f, 0.04f, 0.02f), pale);
        b.PaintEll(Body, V(0, 0.175f, 0.03f), V(0.05f, 0.02f, 0.035f), dark);
        int tail = b.Tail(V(0, 0.08f, -0.04f));
        b.Tube(tail, Smooth(3, V(0, 0.08f, -0.04f), V(0, 0.05f, -0.09f), V(0, 0.03f, -0.13f)), 0.025f, 0.008f, green, blend: 0f);
        // A big head, dark green over its top and back, its crest rising to a point
        int head = b.Head(V(0, 0.18f, 0.01f));
        var c = V(0, 0.25f, 0.02f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Ell(head, c, r, green);
        b.Spike(head, c + V(0, 0.03f, -0.025f), c + V(0, 0.12f, -0.09f), 0.065f, dark, 0.7f);
        b.PaintEll(head, c + V(0, 0.06f, -0.02f), V(0.09f, 0.05f, 0.09f), dark);
        b.PaintEll(head, c + V(0, -0.035f, 0.04f), V(0.055f, 0.03f, 0.04f), pale);
        PokeBuilder.Both(s =>
        {
            // A tusk out of each corner of its mouth, dark with a pale point
            var root = c + V(0.05f * s, -0.035f, 0.035f);
            var tip = c + V(0.1f * s, -0.025f, 0.08f);
            b.Spike(head, root, tip, 0.014f, dark, mat: Shell, blend: 0.006f);
            b.PaintEll(head, tip - (tip - root) * 0.15f, V(0.01f, 0.01f, 0.01f), pale, soft: 0.004f);
            var at = On(c, r, 0.035f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.024f, Rgb(200, 44, 40), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Fraxure: a dark gray dragon in a green jacket of armour, red spots on its belly, a big head that gapes open,
    /// and a long gray tusk to each side tipped in red like an axe; its tail ends in red.
    /// </summary>
    private static PokeBuilder Fraxure()
    {
        var b = new PokeBuilder("Fraxure", 0.75f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Scales };
        var gray = Rgb(84, 84, 92);
        var green = Rgb(86, 136, 82);
        var red = Rgb(200, 62, 60);
        var steel = Rgb(128, 128, 138);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.14f, 0));
            var knee = V(0.075f * s, 0.08f, 0.02f);
            b.Limb(leg, V(0.06f * s, 0.15f, 0), knee, 0.045f, 0.036f, gray);
            b.Limb(leg, knee, V(0.075f * s, 0.025f, 0.02f), 0.034f, 0.028f, gray);
            b.Ell(leg, V(0.078f * s, 0.018f, 0.04f), V(0.032f, 0.018f, 0.045f), gray);
            Claws(b, leg, V(0.078f * s, 0.014f, 0.08f), V(0.014f, 0, 0), V(0, -0.2f, 1f), 0.02f, 0.007f);
            b.PaintEll(leg, V(0.078f * s, 0.022f, 0.09f), V(0.04f, 0.02f, 0.025f), red);
            var shoulder = V(0.07f * s, 0.29f, 0.02f);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder, V(0.13f * s, 0.25f, 0.06f), 0.022f, 0.018f, green);
            b.Ell(arm, V(0.14f * s, 0.245f, 0.07f), V(0.02f, 0.018f, 0.02f), green);
            Claws(b, arm, V(0.15f * s, 0.24f, 0.085f), V(0, 0.008f, 0), V(0.5f * s, 0, 1f), 0.018f, 0.005f);
        });
        b.Ell(Body, V(0, 0.21f, 0), V(0.085f, 0.11f, 0.075f), gray);
        b.Ell(Body, V(0, 0.29f, 0.01f), V(0.09f, 0.06f, 0.075f), green, blend: 0.03f);
        b.PaintEll(Body, V(0, 0.31f, 0.01f), V(0.11f, 0.06f, 0.1f), green);
        foreach (var (x, y, size) in new[] { (-0.04f, 0.2f, 0.016f), (0.035f, 0.17f, 0.02f), (0.01f, 0.23f, 0.012f), (-0.02f, 0.14f, 0.014f) })
            b.PaintEll(Body, V(x, y, 0.07f), V(size, size * 0.8f, 0.03f), red, soft: 0.006f);
        int tail = b.Tail(V(0, 0.16f, -0.06f));
        var tp = Smooth(3, V(0, 0.16f, -0.06f), V(0, 0.1f, -0.16f), V(0, 0.07f, -0.26f), V(0, 0.08f, -0.34f));
        b.Tube(tail, tp, 0.045f, 0.012f, gray, blend: 0f);
        b.PaintEll(tail, tp[^1], V(0.04f, 0.03f, 0.05f), red);
        foreach (int i in new[] { 4, 6, 8 })
            b.Spike(tail, tp[i] + V(0, 0.02f, 0), tp[i] + V(0, 0.05f, -0.02f), 0.012f, red, 0.5f);
        // A big green head with its jaws open, a tusk to each side like an axe's blade
        int head = b.Head(V(0, 0.34f, 0.03f));
        var c = V(0, 0.41f, 0.04f);
        var r = V(0.07f, 0.065f, 0.07f);
        b.Ell(head, c, r, green);
        b.Ell(head, c + V(0, -0.025f, 0.07f), V(0.05f, 0.04f, 0.06f), green);
        Grin(b, head, c + V(0, -0.04f, 0.11f), V(0.035f, 0.018f, 0.03f), Rgb(220, 120, 130));
        b.Spike(head, c + V(0, 0.05f, -0.02f), c + V(0, 0.09f, -0.09f), 0.035f, green, 0.6f);
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.05f * s, -0.02f, 0.06f);
            var tip = c + V(0.19f * s, -0.015f, 0.11f);
            Blade(b, head, root, tip, 0.045f, steel, V(0, 1f, 0.25f), 0.25f, Metal);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.85f), V(0.03f, 0.02f, 0.03f), red, soft: 0.006f);
            var at = On(c, r, 0.045f * s, c.Y + 0.02f);
            b.Eye(head, at, Outward(c, r, at), 0.018f, Rgb(200, 40, 40), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Haxorus: a tall armoured dragon in olive gold over black, red claws, a long banded tail, and two great tusks
    /// curving up from its jaws like axes, red edged with black.
    /// </summary>
    private static PokeBuilder Haxorus()
    {
        var b = new PokeBuilder("Haxorus", 1f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Shell };
        var gold = Rgb(196, 180, 72);
        var black = Rgb(50, 48, 46);
        var red = Rgb(200, 48, 50);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.27f, -0.01f));
            var knee = V(0.09f * s, 0.16f, 0.04f);
            var ankle = V(0.085f * s, 0.05f, -0.01f);
            b.Limb(leg, V(0.06f * s, 0.28f, -0.01f), knee, 0.05f, 0.032f, gold);
            b.Ell(leg, knee, V(0.03f, 0.03f, 0.03f), black);
            b.Limb(leg, knee, ankle, 0.028f, 0.022f, gold);
            b.Ell(leg, ankle + V(0, -0.025f, 0.04f), V(0.03f, 0.022f, 0.06f), black);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, ankle + V(t * 0.016f * s, -0.035f, 0.09f), ankle + V(t * 0.022f * s, -0.045f, 0.12f), 0.008f, red, mat: Shell, blend: 0.004f);
            var shoulder = V(0.07f * s, 0.52f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.12f * s, 0.44f, 0.06f);
            var hand = V(0.13f * s, 0.37f, 0.12f);
            b.Limb(arm, shoulder, elbow, 0.026f, 0.02f, gold);
            b.Limb(arm, elbow, hand, 0.02f, 0.016f, black);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand + V(t * 0.01f * s, 0, 0), hand + V(t * 0.014f * s, -0.03f, 0.03f), 0.006f, red, mat: Shell, blend: 0.004f);
        });
        // A slim black body under plates of gold, broadest at the chest
        b.Ell(Body, V(0, 0.4f, 0), V(0.075f, 0.15f, 0.065f), black);
        b.Ell(Body, V(0, 0.49f, 0.015f), V(0.085f, 0.07f, 0.07f), gold, blend: 0.012f);
        foreach (float y in new[] { 0.4f, 0.33f })
            b.Ell(Body, V(0, y, 0.02f), V(0.072f, 0.03f, 0.055f), gold, blend: 0.008f);
        b.PaintEll(Body, V(0, 0.42f, 0.07f), V(0.025f, 0.1f, 0.02f), black, soft: 0.006f);
        int tail = b.Tail(V(0, 0.32f, -0.05f));
        var tp = Smooth(3, V(0, 0.32f, -0.05f), V(0, 0.24f, -0.18f), V(0, 0.18f, -0.34f), V(0, 0.2f, -0.5f));
        b.Tube(tail, tp, 0.05f, 0.012f, gold, blend: 0f);
        for (int i = 2; i < tp.Length - 1; i += 2)
            b.PaintTorus(tail, tp[i], 0.05f - 0.04f * i / (tp.Length - 1), 0.009f, black, Euler(tp[i + 1] - tp[i]));
        // A long armoured head, its jaw black, its tusks sweeping up past its crown
        int head = b.Head(V(0, 0.6f, 0.03f));
        var c = V(0, 0.66f, 0.05f);
        var r = V(0.06f, 0.055f, 0.08f);
        b.Limb(head, V(0, 0.53f, 0.02f), c, 0.035f, 0.035f, gold);
        b.Ell(head, c, r, gold);
        b.Ell(head, c + V(0, -0.025f, 0.06f), V(0.045f, 0.03f, 0.06f), black);
        b.Spike(head, c + V(0, 0.03f, -0.04f), c + V(0, 0.06f, -0.12f), 0.035f, gold, 0.5f);
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.045f * s, -0.03f, 0.05f);
            Sickle(b, head, root, root + V(0.08f * s, 0.06f, 0.02f), root + V(0.07f * s, 0.2f, -0.06f), 0.03f, red, V(s, 0, 0.3f));
            b.Spike(head, root, root + V(0.06f * s, 0.05f, 0.015f), 0.016f, black, mat: Shell, blend: 0.006f);
            b.PaintEll(head, c + V(0.04f * s, 0.012f, 0.04f), V(0.03f, 0.006f, 0.04f), black, V(0, 0, -15f * s), 0.004f);
            var at = On(c, r, 0.04f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(200, 40, 40), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Cubchoo and Beartic

    /// <summary>Cubchoo: a little white bear cub with a big round blue head, round blue ears, a big black nose and a drop of icy snot hanging from it.</summary>
    private static PokeBuilder Cubchoo()
    {
        var b = new PokeBuilder("Cubchoo", 0.5f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Fur };
        var white = Rgb(244, 246, 250);
        var blue = Rgb(150, 200, 236);
        var pad = Rgb(70, 72, 86);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.06f, 0));
            b.Limb(leg, V(0.04f * s, 0.07f, 0), V(0.045f * s, 0.025f, 0.01f), 0.026f, 0.024f, white);
            b.Ell(leg, V(0.046f * s, 0.022f, 0.02f), V(0.026f, 0.022f, 0.03f), white);
            b.PaintEll(leg, V(0.046f * s, 0.022f, 0.048f), V(0.016f, 0.014f, 0.01f), pad, soft: 0.005f);
            int arm = b.Arm(s, V(0.045f * s, 0.13f, 0.02f));
            b.Limb(arm, V(0.045f * s, 0.13f, 0.02f), V(0.05f * s, 0.09f, 0.05f), 0.018f, 0.016f, white);
        });
        b.Ell(Body, V(0, 0.11f, 0), V(0.06f, 0.06f, 0.055f), white);
        // A big blue head with a long muzzle, a black nose and round ears
        int head = b.Head(V(0, 0.16f, 0.01f));
        var c = V(0, 0.24f, 0.015f);
        var r = V(0.085f, 0.075f, 0.075f);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, -0.03f, 0.065f), V(0.04f, 0.03f, 0.035f), blue);
        b.Ell(head, c + V(0, -0.02f, 0.1f), V(0.022f, 0.016f, 0.014f), Rgb(46, 44, 52), mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.05f, -0.01f));
            b.Ell(ear, c + V(0.055f * s, 0.06f, -0.015f), V(0.032f, 0.032f, 0.018f), blue);
            var at = On(c, r, 0.04f * s, c.Y + 0.015f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(40, 40, 50));
        });
        // The drop of snot that always hangs from its nose
        var drip = new[] { c + V(0.005f, -0.03f, 0.105f), c + V(0.008f, -0.06f, 0.1f), c + V(0.01f, -0.09f, 0.095f) };
        b.Tube(head, drip, 0.008f, 0.012f, Rgb(170, 214, 240), Shell, 0.004f);
        b.Ell(head, drip[^1] + V(0, -0.012f, 0), V(0.018f, 0.022f, 0.016f), Rgb(170, 214, 240), mat: Shell, blend: 0.008f);
        return b;
    }

    /// <summary>Beartic: a great white polar bear standing tall, arms spread with dark pads and claws, shaggy fur at its belly, and a beard of blue icicles under its open jaws.</summary>
    private static PokeBuilder Beartic()
    {
        var b = new PokeBuilder("Beartic", 1f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var white = Rgb(232, 238, 246);
        var ice = Rgb(150, 200, 236);
        var pad = Rgb(70, 72, 86);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.2f, 0));
            b.Limb(leg, V(0.08f * s, 0.21f, 0), V(0.09f * s, 0.04f, 0.01f), 0.06f, 0.05f, white);
            b.Ell(leg, V(0.092f * s, 0.028f, 0.03f), V(0.05f, 0.028f, 0.065f), white);
            Claws(b, leg, V(0.092f * s, 0.02f, 0.09f), V(0.018f, 0, 0), V(0, -0.2f, 1f), 0.02f, 0.008f);
            // Arms held out wide, the dark pads of the paws to the front
            var shoulder = V(0.13f * s, 0.5f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.24f * s, 0.52f, 0.03f);
            var paw = V(0.32f * s, 0.56f, 0.06f);
            b.Limb(arm, shoulder, elbow, 0.055f, 0.045f, white);
            b.Limb(arm, elbow, paw, 0.045f, 0.04f, white);
            b.Ell(arm, paw, V(0.045f, 0.05f, 0.035f), white);
            b.PaintEll(arm, paw + V(0, 0, 0.03f), V(0.03f, 0.03f, 0.015f), pad, soft: 0.006f);
            Claws(b, arm, paw + V(0, 0.045f, 0.02f), V(0.015f, 0, 0), V(0, 1f, 0.6f), 0.025f, 0.008f);
        });
        b.Ell(Body, V(0, 0.36f, 0), V(0.15f, 0.2f, 0.12f), white);
        FurTufts(b, Body, V(0, 0.24f, 0), V(0.15f, 0.08f, 0.12f), 18, 0.05f, 0.025f, white, 1.1f, -0.9f, 0.6f);
        // A small head on its broad shoulders, its jaws open over a beard of icicles
        int head = b.Head(V(0, 0.54f, 0.03f));
        var c = V(0, 0.62f, 0.05f);
        var r = V(0.075f, 0.065f, 0.075f);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.015f, 0.07f), V(0.045f, 0.035f, 0.05f), white);
        b.Ell(head, c + V(0, -0.005f, 0.115f), V(0.018f, 0.013f, 0.012f), pad, mat: Shell, blend: 0.008f);
        Grin(b, head, c + V(0, -0.045f, 0.09f), V(0.03f, 0.015f, 0.03f), Rgb(200, 90, 110));
        b.Ell(head, c + V(0, -0.075f, 0.06f), V(0.06f, 0.04f, 0.04f), ice, mat: Shell, blend: 0.015f);
        foreach (var (x, len, z) in new[] { (-0.05f, 0.1f, 0.05f), (-0.025f, 0.16f, 0.07f), (0f, 0.2f, 0.08f), (0.025f, 0.15f, 0.07f), (0.05f, 0.09f, 0.05f) })
            b.Spike(head, c + V(x, -0.09f, z), c + V(x * 1.2f, -0.09f - len, z + 0.01f), 0.018f, ice, 0.6f, Shell);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.05f, -0.02f));
            b.Ell(ear, c + V(0.06f * s, 0.065f, -0.02f), V(0.02f, 0.02f, 0.012f), white);
            var at = On(c, r, 0.035f * s, c.Y + 0.02f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 40, 50), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Cryogonal

    /// <summary>Cryogonal: a floating snowflake of blue ice, a pale hexagonal face framed in ice with two glowing eyes and a chain of icy beads for a mouth, six crossed crystals round it.</summary>
    private static PokeBuilder Cryogonal()
    {
        var b = new PokeBuilder("Cryogonal", 0.85f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var ice = Rgb(120, 162, 222);
        var pale = Rgb(198, 222, 246);
        var bead = Rgb(150, 210, 246);
        var c = V(0, 0.42f, 0);
        var r = V(0.11f, 0.11f, 0.035f);
        b.Ell(Body, c, r, pale);
        for (int k = 0; k < 6; k++)
        {
            float a = (90f + k * 60f) * Degree;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            float deg = k * 60f;
            // The frame of the face: one side of the hexagon between this corner and the next
            var mid = c + V(MathF.Cos(a + 30f * Degree), MathF.Sin(a + 30f * Degree), 0) * 0.105f;
            b.Box(Body, mid, V(0.065f, 0.016f, 0.03f), 0.006f, ice, V(0, 0, deg + 30f), Shell, 0.004f);
            // A crystal at each corner: a bar out, and two crossing it near its end
            var corner = c + d * 0.12f;
            b.Box(Body, corner + d * 0.06f, V(0.016f, 0.07f, 0.02f), 0.005f, ice, V(0, 0, deg), Shell, 0.004f);
            foreach (float t in new[] { -1f, 1f })
                b.Box(Body, corner + d * 0.1f, V(0.014f, 0.055f, 0.016f), 0.004f, ice, V(0, 0, deg + 40f * t), Shell, 0.004f);
        }
        // A chain of icy beads hanging below the eyes
        foreach (var x in new[] { -0.04f, -0.02f, 0f, 0.02f, 0.04f })
        {
            float y = c.Y - 0.035f - 0.02f * (1f - MathF.Abs(x) / 0.04f);
            b.Ell(Body, V(x, y, 0.028f), V(0.012f, 0.012f, 0.012f), bead, mat: Glow, blend: 0.004f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.025f);
            b.Eye(Body, at, V(0, 0, 1f), 0.022f, Rgb(150, 220, 255));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Shelmet and Accelgor

    /// <summary>Shelmet: a pink snail of a Pokémon in a pale helmet of shell curled at the back, blue spots across its top, its pink face and sucking mouth looking out of the visor.</summary>
    private static PokeBuilder Shelmet()
    {
        var b = new PokeBuilder("Shelmet", 0.45f, BodyPlan.Floating, V(0, 0.11f, 0)) { Coat = Shell };
        var shell = Rgb(220, 214, 200);
        var spot = Rgb(70, 96, 170);
        var pink = Rgb(236, 120, 150);
        var c = V(0, 0.12f, -0.01f);
        var r = V(0.1f, 0.11f, 0.12f);
        b.Ell(Body, c, r, shell);
        // The helmet curls round at the back
        b.Torus(Body, c + V(0, 0.04f, -0.09f), 0.045f, 0.03f, shell, V(0, 0, 90f), blend: 0.02f);
        b.Ell(Body, c + V(0, 0.04f, -0.09f), V(0.03f, 0.03f, 0.03f), PixelCanvas.Mix(shell, Black, 0.15f), blend: 0.01f);
        // The visor: open across the front, dark inside
        b.Cut(Body, c + V(0, -0.025f, 0.11f), V(0.085f, 0.05f, 0.07f));
        b.PaintEll(Body, c + V(0, -0.025f, 0.06f), V(0.09f, 0.055f, 0.06f), Rgb(60, 50, 60));
        foreach (var (x, z) in new[] { (-0.045f, 0.05f), (0f, 0.065f), (0.045f, 0.05f) })
            b.PaintEll(Body, c + V(x, 0.09f, z), V(0.014f, 0.01f, 0.03f), spot, soft: 0.006f);
        // Its pink face and mouth inside the visor
        int head = b.Head(V(0, 0.09f, 0.03f));
        var fc = V(0, 0.09f, 0.06f);
        var fr = V(0.06f, 0.04f, 0.05f);
        b.Ell(head, fc, fr, pink);
        b.Limb(head, fc + V(0, -0.01f, 0.03f), fc + V(0, -0.015f, 0.09f), 0.02f, 0.016f, pink);
        b.Ell(head, fc + V(0, -0.015f, 0.095f), V(0.022f, 0.02f, 0.012f), PixelCanvas.Mix(pink, Black, 0.1f), blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.035f * s, fc.Y + 0.012f);
            b.Eye(head, at, Outward(fc, fr, at), 0.016f, Rgb(70, 170, 80), sclera: true);
        });
        return b;
    }

    /// <summary>
    /// Accelgor: a ninja of a Pokémon, its pink head a helmet curled at the back with green and red stripes and a black
    /// star on its brow, its eyes peering from the dark beneath, its body wrapped in blue-gray cloth that streams away behind.
    /// </summary>
    private static PokeBuilder Accelgor()
    {
        var b = new PokeBuilder("Accelgor", 0.75f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var wrap = Rgb(92, 106, 150);
        var light = Rgb(130, 146, 190);
        var pink = Rgb(236, 120, 150);
        var green = Rgb(120, 200, 110);
        var red = Rgb(214, 70, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.12f, 0));
            b.Limb(leg, V(0.03f * s, 0.13f, 0), V(0.035f * s, 0.02f, 0.01f), 0.022f, 0.016f, wrap);
            b.Ell(leg, V(0.036f * s, 0.016f, 0.02f), V(0.018f, 0.016f, 0.03f), wrap);
            var shoulder = V(0.05f * s, 0.32f, 0.01f);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder, V(0.08f * s, 0.24f, 0.05f), 0.016f, 0.012f, wrap);
            b.Ell(arm, V(0.082f * s, 0.235f, 0.055f), V(0.014f, 0.014f, 0.014f), wrap);
            // The streamers of its wrappings, flying out behind
            var streamer = Smooth(3, shoulder, V(0.08f * s, 0.37f, -0.1f), V(0.13f * s, 0.42f, -0.2f), V(0.19f * s, 0.39f, -0.3f));
            Ribbon(b, arm, streamer, 0.035f, light, V(0, 1f, 0.3f), 0.008f);
        });
        b.Ell(Body, V(0, 0.22f, 0), V(0.06f, 0.12f, 0.05f), wrap);
        foreach (float y in new[] { 0.16f, 0.22f, 0.28f })
            b.PaintTorus(Body, V(0, y, 0), 0.058f, 0.006f, light, V(8f, 0, 0), sz: 0.85f);
        // A pink head like a helmet, curled at the back, its stripes running over the top
        int head = b.Head(V(0, 0.33f, 0.01f));
        var c = V(0, 0.42f, 0.015f);
        var r = V(0.075f, 0.075f, 0.075f);
        b.Ell(head, c, r, pink);
        b.Torus(head, c + V(0, 0.03f, -0.06f), 0.035f, 0.022f, pink, V(0, 0, 90f), blend: 0.015f);
        b.PaintTorus(head, c, 0.075f, 0.009f, red, V(0, 0, 90f));
        PokeBuilder.Both(s => b.PaintTorus(head, c + V(0.022f * s, 0, 0), 0.072f, 0.008f, green, V(0, 0, 90f)));
        var star = On(c, r, 0, c.Y + 0.01f);
        b.Mark(head, star, Outward(c, r, star), 0.03f, 0.03f, Black, MarkShape.Star);
        // Its eyes in the shadow beneath the helmet's rim
        b.PaintEll(head, c + V(0, -0.055f, 0.05f), V(0.06f, 0.02f, 0.04f), Rgb(40, 38, 48));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y - 0.052f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(236, 236, 220), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Stunfisk

    /// <summary>
    /// Stunfisk and its Galarian form: a flat fish lying in the mud, broad fins at its sides and a flat tail, its eyes on
    /// top. The original is brown with yellow lips and a yellow crest; the Galarian is a trap, gray-green with olive
    /// blotches, a ring of black teeth standing up round its top and a red lure between its eyes.
    /// </summary>
    private static PokeBuilder StunfiskBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Stunfisk-Galar" : "Stunfisk", 0.7f, BodyPlan.Fish, V(0, 0.05f, 0)) { Coat = Scales };
        var skin = galar ? Rgb(96, 100, 86) : Rgb(140, 92, 60);
        var patch = galar ? Rgb(150, 170, 80) : Rgb(236, 206, 96);
        var c = V(0, 0.05f, 0);
        var r = V(0.17f, 0.05f, 0.14f);
        b.Ell(Body, c, r, skin);
        PokeBuilder.Both(s =>
        {
            // Broad flat fins at its sides
            b.Ell(Body, c + V(0.16f * s, -0.02f, -0.02f), V(0.06f, 0.012f, 0.05f), skin, V(0, 20f * s, 0), blend: 0.02f);
        });
        int tail = b.Tail(c + V(0, 0, -0.12f));
        b.Ell(tail, c + V(0, -0.02f, -0.19f), V(0.07f, 0.012f, 0.05f), skin, blend: 0.02f);
        if (galar)
        {
            // Olive blotches over its back and a trap's ring of black teeth standing up round it
            foreach (var (x, z, size) in new[] { (-0.07f, 0.02f, 0.035f), (0.06f, -0.03f, 0.04f), (0f, -0.07f, 0.03f), (0.09f, 0.05f, 0.025f) })
                b.PaintEll(Body, c + V(x, 0.05f, z), V(size, 0.02f, size * 0.8f), patch);
            for (int k = 0; k < 9; k++)
            {
                float a = (22f + k * 17f) * Degree;
                var at = c + V(MathF.Cos(a) * 0.13f, 0.03f, -MathF.Sin(a) * 0.1f);
                b.Spike(Body, at, at + V(-MathF.Cos(a) * 0.01f, 0.038f, MathF.Sin(a) * 0.01f), 0.022f, Rgb(46, 44, 50), 0.45f, Shell);
            }
            var lure = On(c, r, 0, c.Y + 0.012f);
            b.Ell(Body, lure, V(0.02f, 0.016f, 0.012f), Rgb(210, 50, 50), blend: 0.006f);
            b.PaintEll(Body, lure + V(0, 0.003f, 0.01f), V(0.007f, 0.006f, 0.006f), White, soft: 0.003f);
        }
        else
        {
            // Thick yellow lips round its mouth at the front and a yellow crest behind its eyes
            b.PaintEll(Body, c + V(0, -0.005f, 0.13f), V(0.13f, 0.02f, 0.03f), patch, soft: 0.01f);
            b.Spike(Body, c + V(0, 0.04f, -0.04f), c + V(0, 0.085f, -0.06f), 0.04f, patch, 0.3f);
            b.PaintEll(Body, c + V(0.08f, 0.04f, -0.07f), V(0.03f, 0.02f, 0.025f), patch);
        }
        PokeBuilder.Both(s =>
        {
            var dir = Vector3.Normalize(V(0.35f * s, 0.8f, 0.6f));
            var at = Out(c, r, default, dir);
            b.Eye(Body, at, Outward(c, r, at), 0.028f, Rgb(40, 40, 46), sclera: true);
        });
        return b;
    }

    private static PokeBuilder Stunfisk() => StunfiskBuild(false);

    // ------------------------------------------------------------------ Mienfoo and Mienshao

    /// <summary>Mienfoo: a little yellow weasel of the martial arts, red fur like great cuffs on its arms, red legs and tail, a round face with dark rings round its eyes.</summary>
    private static PokeBuilder Mienfoo()
    {
        var b = new PokeBuilder("Mienfoo", 0.7f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var yellow = Rgb(240, 216, 120);
        var red = Rgb(190, 66, 88);
        var ring = Rgb(140, 100, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.12f, 0));
            b.Limb(leg, V(0.04f * s, 0.13f, 0), V(0.05f * s, 0.07f, 0.02f), 0.03f, 0.024f, yellow);
            b.Limb(leg, V(0.05f * s, 0.07f, 0.02f), V(0.05f * s, 0.025f, 0.02f), 0.026f, 0.024f, red);
            b.Ell(leg, V(0.052f * s, 0.018f, 0.04f), V(0.024f, 0.018f, 0.035f), red);
            var shoulder = V(0.06f * s, 0.25f, 0.01f);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder, V(0.1f * s, 0.21f, 0.05f), 0.018f, 0.016f, yellow);
            // Its fists, buried in big cuffs of red fur
            b.Ell(arm, V(0.115f * s, 0.195f, 0.075f), V(0.04f, 0.035f, 0.04f), red);
            FurTufts(b, arm, V(0.115f * s, 0.195f, 0.075f), V(0.035f, 0.03f, 0.035f), 6, 0.02f, 0.012f, red, 1.1f, -1f, 0.2f);
        });
        b.Ell(Body, V(0, 0.2f, 0), V(0.065f, 0.08f, 0.055f), yellow);
        int tail = b.Tail(V(0, 0.16f, -0.05f));
        var tp = Smooth(3, V(0, 0.16f, -0.05f), V(0.02f, 0.14f, -0.13f), V(0.06f, 0.2f, -0.18f));
        b.Tube(tail, tp, 0.012f, 0.016f, red, blend: 0.01f);
        b.Ell(tail, tp[^1], V(0.022f, 0.026f, 0.022f), red);
        int head = b.Head(V(0, 0.28f, 0.01f));
        var c = V(0, 0.34f, 0.015f);
        var r = V(0.075f, 0.07f, 0.065f);
        b.Ell(head, c, r, yellow);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.035f * s, 0.06f, -0.01f));
            b.Ell(ear, c + V(0.045f * s, 0.1f, -0.015f), V(0.02f, 0.04f, 0.012f), yellow, V(0, 0, -15f * s));
            var at = On(c, r, 0.035f * s, c.Y + 0.008f);
            b.PaintEll(head, at, V(0.02f, 0.017f, 0.02f), ring, soft: 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(40, 36, 40));
        });
        var m = On(c, r, 0, c.Y - 0.035f);
        b.Mark(head, m, Outward(c, r, m), 0.009f, 0.005f, Rgb(120, 70, 60), MarkShape.Smile);
        return b;
    }

    /// <summary>Mienshao: a slender lilac-white martial artist, long whips of purple fur hanging from its arms, purple bands at its ankles, long streamers rising from its head and a long thin tail.</summary>
    private static PokeBuilder Mienshao()
    {
        var b = new PokeBuilder("Mienshao", 0.95f, BodyPlan.Biped, V(0, 0.34f, 0)) { Coat = Fur };
        var white = Rgb(228, 224, 236);
        var purple = Rgb(158, 136, 196);
        var pink = Rgb(220, 150, 180);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.24f, 0));
            var knee = V(0.055f * s, 0.13f, 0.03f);
            b.Limb(leg, V(0.04f * s, 0.25f, 0), knee, 0.03f, 0.022f, white);
            b.Limb(leg, knee, V(0.055f * s, 0.03f, 0), 0.02f, 0.016f, white);
            b.Torus(leg, V(0.055f * s, 0.06f, 0.003f), 0.02f, 0.008f, purple, blend: 0.004f);
            b.Ell(leg, V(0.056f * s, 0.016f, 0.02f), V(0.018f, 0.016f, 0.035f), white);
            var shoulder = V(0.05f * s, 0.44f, 0.01f);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.11f * s, 0.36f, 0.06f);
            b.Limb(arm, shoulder, hand, 0.016f, 0.013f, white);
            b.Ell(arm, hand, V(0.015f, 0.015f, 0.015f), white);
            // A long whip of purple fur hanging from each arm
            var whip = Smooth(3, Vector3.Lerp(shoulder, hand, 0.4f), hand + V(0.04f * s, -0.04f, -0.02f), hand + V(0.07f * s, -0.16f, -0.04f), hand + V(0.05f * s, -0.28f, -0.02f));
            Ribbon(b, arm, whip, 0.03f, purple, V(s, 0, 0.4f), 0.008f);
        });
        b.Ell(Body, V(0, 0.36f, 0), V(0.055f, 0.1f, 0.045f), white);
        b.PaintEll(Body, V(0, 0.39f, 0.04f), V(0.03f, 0.05f, 0.02f), pink);
        int tail = b.Tail(V(0, 0.29f, -0.04f));
        var tp = Smooth(3, V(0, 0.29f, -0.04f), V(0, 0.2f, -0.12f), V(0.04f, 0.18f, -0.24f), V(0.08f, 0.24f, -0.32f));
        b.Tube(tail, tp, 0.012f, 0.006f, white, blend: 0f);
        b.Ell(tail, tp[^1], V(0.016f, 0.03f, 0.016f), purple, Euler(tp[^1] - tp[^2]), blend: 0.008f);
        int head = b.Head(V(0, 0.46f, 0.01f));
        var c = V(0, 0.52f, 0.02f);
        var r = V(0.055f, 0.05f, 0.055f);
        b.Limb(head, V(0, 0.44f, 0.005f), c, 0.02f, 0.02f, white);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.015f, 0.045f), V(0.03f, 0.022f, 0.03f), white);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.04f, -0.01f));
            var streamer = Smooth(3, c + V(0.03f * s, 0.04f, -0.01f), c + V(0.06f * s, 0.1f, -0.03f), c + V(0.08f * s, 0.16f, -0.08f), c + V(0.1f * s, 0.2f, -0.14f));
            Ribbon(b, ear, streamer, 0.022f, purple, V(s, 0, 0.4f), 0.008f);
            var at = On(c, r, 0.028f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(120, 80, 160), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Druddigon

    /// <summary>Druddigon: a hunched blue dragon with a rough red head, a belly of tan plates, red spikes down its back and tail, small blue wings edged in red and great claws.</summary>
    private static PokeBuilder Druddigon()
    {
        var b = new PokeBuilder("Druddigon", 1f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Scales };
        var blue = Rgb(50, 108, 146);
        var dark = Rgb(36, 80, 110);
        var red = Rgb(196, 50, 60);
        var tan = Rgb(208, 170, 96);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.07f * s, 0.42f, -0.05f));
            DragonWing(b, wing, V(0.07f * s, 0.42f, -0.05f), V(0.24f * s, 0.56f, -0.12f),
                new[] { V(0.32f * s, 0.46f, -0.14f), V(0.28f * s, 0.38f, -0.14f), V(0.2f * s, 0.36f, -0.12f) },
                V(0.12f * s, 0.38f, -0.08f), dark, blue, 0.014f);
            b.Spike(wing, V(0.24f * s, 0.56f, -0.12f), V(0.27f * s, 0.6f, -0.13f), 0.01f, red, blend: 0.004f);
        });
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.2f, 0));
            var knee = V(0.11f * s, 0.12f, 0.04f);
            b.Limb(leg, V(0.08f * s, 0.21f, 0), knee, 0.06f, 0.045f, blue);
            b.Limb(leg, knee, V(0.11f * s, 0.03f, 0.02f), 0.045f, 0.036f, blue);
            b.Ell(leg, V(0.112f * s, 0.024f, 0.05f), V(0.042f, 0.024f, 0.06f), blue);
            Claws(b, leg, V(0.112f * s, 0.018f, 0.105f), V(0.018f, 0, 0), V(0, -0.2f, 1f), 0.025f, 0.009f);
            var shoulder = V(0.11f * s, 0.42f, 0.04f);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.19f * s, 0.3f, 0.13f);
            b.Limb(arm, shoulder, hand, 0.045f, 0.036f, blue);
            b.Ell(arm, hand, V(0.035f, 0.032f, 0.035f), blue);
            Claws(b, arm, hand + V(0, -0.01f, 0.03f), V(0.016f, 0, 0), V(0, -0.5f, 1f), 0.045f, 0.011f);
        });
        b.Ell(Body, V(0, 0.32f, 0), V(0.14f, 0.16f, 0.12f), blue);
        b.Ell(Body, V(0, 0.42f, 0.04f), V(0.12f, 0.08f, 0.09f), blue, blend: 0.04f);
        foreach (float y in new[] { 0.2f, 0.26f, 0.32f, 0.38f })
            b.Ell(Body, V(0, y, 0.1f), V(0.065f - (y - 0.2f) * 0.05f, 0.026f, 0.03f), tan, mat: Shell, blend: 0.008f);
        int tail = b.Tail(V(0, 0.22f, -0.1f));
        var tp = Smooth(3, V(0, 0.22f, -0.1f), V(0, 0.12f, -0.22f), V(0, 0.07f, -0.36f), V(0.03f, 0.06f, -0.48f));
        b.Tube(tail, tp, 0.07f, 0.015f, blue, blend: 0f);
        for (int i = 1; i < tp.Length - 1; i += 2)
        {
            float rad = 0.07f - 0.055f * i / (tp.Length - 1);
            b.Spike(tail, tp[i] + V(0, rad * 0.8f, 0), tp[i] + V(0, rad * 0.8f + 0.04f, -0.03f), 0.018f, red, 0.5f, Shell);
        }
        foreach (var (y, z) in new[] { (0.46f, -0.05f), (0.42f, -0.085f), (0.36f, -0.11f) })
            b.Spike(Body, V(0, y, z), V(0, y + 0.06f, z - 0.04f), 0.02f, red, 0.5f, Shell);
        // A rough red head on a short neck, jaws full of teeth
        int head = b.Head(V(0, 0.5f, 0.08f));
        var c = V(0, 0.55f, 0.12f);
        var r = V(0.08f, 0.07f, 0.08f);
        b.Ell(head, c, r, red);
        b.Ell(head, c + V(0, -0.03f, 0.07f), V(0.06f, 0.04f, 0.05f), red);
        foreach (var (x, y, z) in new[] { (-0.05f, 0.05f, 0f), (0.05f, 0.05f, 0f), (0f, 0.065f, -0.02f), (-0.065f, 0.02f, -0.03f), (0.065f, 0.02f, -0.03f), (0f, 0.04f, 0.05f) })
            b.Ell(head, c + V(x, y, z), V(0.03f, 0.024f, 0.03f), red, blend: 0.015f);
        Gape(b, head, c + V(0, -0.045f, 0.11f), V(0.045f, 0.018f, 0.03f), Rgb(120, 40, 50), 0.014f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(246, 210, 70), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Golett and Golurk

    /// <summary>A square panel of pale clay with a dark spiral pressed into it, as the Golett line bears on its chest and shoulders; it faces forward, or out to a side.</summary>
    private static void SealPanel(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float size, Color panel, Color line)
    {
        var n = Vector3.Normalize(facing);
        var u = Vector3.Normalize(Vector3.Cross(V(0, 1f, 0), n));
        var v = Vector3.Cross(n, u);
        var turn = MathF.Abs(n.X) > 0.5f ? V(0, 90f, 45f) : V(0, 0, 45f);
        b.Box(bone, at, V(size, size, size * 0.18f), size * 0.15f, panel, turn, Shell, 0.006f);
        var spiral = Spiral(at + n * size * 0.17f, u, v, size * 0.55f, size * 0.08f, 0f, 3.2f * MathF.PI, 24);
        b.Tube(bone, spiral, size * 0.07f, size * 0.07f, line, Shell, 0f);
    }

    /// <summary>Golett: a round teal golem of clay, two brown straps crossed over a pale spiral panel on its chest, big fists, stubby legs and two yellow eyes glowing in a small square head.</summary>
    private static PokeBuilder Golett()
    {
        var b = new PokeBuilder("Golett", 0.8f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Shell };
        var teal = Rgb(88, 160, 178);
        var dark = Rgb(46, 104, 138);
        var light = Rgb(170, 222, 220);
        var strap = Rgb(140, 100, 64);
        var glow = Rgb(252, 232, 120);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.12f, 0));
            b.Limb(leg, V(0.07f * s, 0.13f, 0), V(0.08f * s, 0.04f, 0.01f), 0.045f, 0.045f, teal);
            b.Box(leg, V(0.082f * s, 0.03f, 0.025f), V(0.045f, 0.03f, 0.055f), 0.015f, teal, mat: Shell, blend: 0.01f);
            var shoulder = V(0.13f * s, 0.3f, 0);
            int arm = b.Arm(s, shoulder);
            var fist = V(0.21f * s, 0.2f, 0.04f);
            b.Limb(arm, shoulder, fist, 0.045f, 0.04f, teal);
            b.Box(arm, fist + V(0.01f * s, -0.02f, 0.01f), V(0.05f, 0.055f, 0.05f), 0.02f, teal, mat: Shell, blend: 0.01f);
        });
        var bc = V(0, 0.24f, 0);
        var br = V(0.14f, 0.15f, 0.12f);
        b.Ell(Body, bc, br, teal);
        b.PaintEll(Body, bc + V(0, -0.1f, 0), V(0.16f, 0.07f, 0.14f), dark);
        foreach (float t in new[] { -1f, 1f })
            b.Torus(Body, bc, 0.135f, 0.016f, strap, V(0, 0, 38f * t), 1f, 0.88f, Shell, 0.004f);
        SealPanel(b, Body, On(bc, br, 0, bc.Y + 0.01f) + V(0, 0, 0.003f), V(0, 0, 1f), 0.045f, light, dark);
        // A small square head with two glowing eyes
        int head = b.Head(V(0, 0.37f, 0.01f));
        var c = V(0, 0.42f, 0.02f);
        b.Box(head, c, V(0.065f, 0.055f, 0.06f), 0.02f, teal, mat: Shell, blend: 0.02f);
        b.Box(head, c + V(0, 0.06f, -0.01f), V(0.03f, 0.016f, 0.03f), 0.008f, dark, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, c + V(0.024f * s, 0.005f, 0.06f), V(0, 0, 1f), 0.02f, glow));
        return b;
    }

    /// <summary>
    /// Golurk and its Mega Evolution: a giant teal golem of clay, a seal bound across its chest with brown straps and
    /// cracked with light, spiral panels on great round shoulders, banded wrists and ankles and a pointed helmet of a
    /// head. Mega Golurk stands taller with the seal gone: a cross of yellow light on its chest, light breaking out of
    /// its wrists, ankles, shoulders and left eye, and purple flames rising from its back.
    /// </summary>
    private static PokeBuilder GolurkBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Golurk-Mega" : "Golurk", 1f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Shell };
        var teal = Rgb(88, 160, 178);
        var dark = Rgb(46, 104, 138);
        var light = Rgb(170, 222, 220);
        var strap = Rgb(140, 100, 64);
        var glow = Rgb(252, 226, 100);
        var flame = Rgb(150, 90, 220);
        float k = mega ? 1.08f : 1f;
        PokeBuilder.Both(s =>
        {
            // Pillar legs, a band at each ankle
            int leg = b.Leg(s, V(0.08f * s, 0.24f * k, 0));
            var ankle = V(0.09f * s, 0.07f, 0.01f);
            b.Limb(leg, V(0.08f * s, 0.25f * k, 0), ankle, 0.055f, 0.05f, teal);
            b.Box(leg, V(0.092f * s, 0.035f, 0.03f), V(0.06f, 0.035f, 0.07f), 0.015f, teal, mat: Shell, blend: 0.01f);
            b.Torus(leg, ankle + V(0, 0.01f, 0), 0.05f, 0.016f, mega ? glow : strap, mat: mega ? Glow : Shell, blend: 0.004f);
            // Great round shoulders with a spiral panel on each, thick arms and banded wrists
            var shoulder = V(0.16f * s, 0.56f * k, 0);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder - V(0.04f * s, 0, 0), shoulder + V(0.06f * s, 0, 0), 0.075f, 0.075f, teal);
            b.Spike(arm, shoulder + V(0.03f * s, 0.05f, 0), shoulder + V(0.05f * s, 0.12f, -0.01f), 0.03f, teal, 0.7f);
            var elbow = V(0.24f * s, 0.44f * k, 0.02f);
            var wrist = V(0.26f * s, 0.32f * k, 0.06f);
            b.Limb(arm, shoulder, elbow, 0.055f, 0.05f, teal);
            b.Limb(arm, elbow, wrist, 0.05f, 0.045f, teal);
            b.Torus(arm, wrist, 0.048f, 0.016f, mega ? glow : strap, Euler(wrist - elbow), mat: mega ? Glow : Shell, blend: 0.004f);
            b.Box(arm, wrist + V(0.005f * s, -0.07f, 0.02f), V(0.06f, 0.06f, 0.055f), 0.02f, teal, mat: Shell, blend: 0.01f);
            var side = shoulder + V(0.13f * s, 0, 0);
            if (mega)
            {
                b.Ell(arm, side, V(0.02f, 0.05f, 0.05f), glow, mat: Glow, blend: 0.01f);
                FlameTongue(b, arm, side + V(0, 0.03f, -0.02f), side + V(0.05f * s, 0.16f, -0.06f), 0.03f, flame, Rgb(220, 190, 250));
            }
            else SealPanel(b, arm, side, V(s, 0, 0), 0.04f, light, Rgb(230, 190, 80));
        });
        var bc = V(0, 0.46f * k, 0);
        b.Box(Body, bc, V(0.15f, 0.15f, 0.12f), 0.06f, teal, mat: Shell, blend: 0.03f);
        b.Ell(Body, bc + V(0, -0.16f, 0), V(0.11f, 0.08f, 0.09f), dark, blend: 0.03f);
        b.Torus(Body, bc + V(0, -0.13f, 0), 0.115f, 0.018f, strap, sz: 0.85f, mat: Shell, blend: 0.004f);
        var front = bc + V(0, 0.02f, 0.12f);
        if (mega)
        {
            // A cross of light where the seal was
            b.Box(Body, front + V(0, 0, 0.005f), V(0.022f, 0.09f, 0.012f), 0.008f, glow, mat: Glow, blend: 0.004f);
            b.Box(Body, front + V(0, 0.02f, 0.005f), V(0.08f, 0.022f, 0.012f), 0.008f, glow, mat: Glow, blend: 0.004f);
            foreach (var (from, to) in new[] { (V(-0.08f, 0.6f, 0.06f), V(-0.14f, 0.82f, -0.04f)), (V(0.07f, 0.62f, 0.04f), V(0.12f, 0.86f, -0.06f)), (V(0, 0.6f, -0.06f), V(0, 0.84f, -0.14f)) })
                FlameTongue(b, Body, from * V(1f, k, 1f), to * V(1f, k, 1f), 0.035f, flame, Rgb(220, 190, 250));
        }
        else
        {
            // The seal bound over its chest, cracked with light
            b.Box(Body, front, V(0.1f, 0.065f, 0.014f), 0.012f, strap, V(0, 0, -30f), Shell, 0.004f);
            b.Tube(Body, new[] { front + V(-0.06f, 0.04f, 0.016f), front + V(-0.015f, 0.008f, 0.016f), front + V(0.015f, 0.028f, 0.016f), front + V(0.065f, -0.03f, 0.016f) }, 0.006f, 0.005f, glow, Glow, 0f);
            b.Torus(Body, bc + V(0, 0.04f, 0), 0.14f, 0.016f, strap, V(0, 0, -30f), 1f, 0.9f, Shell, 0.004f);
        }
        // A head like a helmet rising to a point, its eyes two lights
        int head = b.Head(V(0, 0.62f * k, 0.02f));
        var c = V(0, 0.68f * k, 0.03f);
        b.Limb(head, V(0, 0.58f * k, 0.01f), c, 0.04f, 0.04f, teal);
        b.Box(head, c, V(0.07f, 0.06f, 0.065f), 0.025f, teal, mat: Shell, blend: 0.02f);
        b.Spike(head, c + V(0, 0.05f, -0.01f), c + V(0, 0.14f, -0.03f), 0.05f, teal, 0.6f);
        PokeBuilder.Both(s =>
        {
            // Mega Golurk's light breaks out of its left eye only
            bool lit = !mega || s > 0;
            b.Eye(head, c + V(0.028f * s, -0.005f, 0.065f), V(0, 0, 1f), 0.02f, lit ? glow : Rgb(70, 90, 100));
        });
        return b;
    }

    private static PokeBuilder Golurk() => GolurkBuild(false);

    // ------------------------------------------------------------------ Pawniard and Bisharp

    /// <summary>Pawniard: a small blade of a Pokémon, a red helmet with a steel crescent blade rising from it over a black face with yellow eyes, a black body banded in steel, blades for hands and red legs.</summary>
    private static PokeBuilder Pawniard()
    {
        var b = new PokeBuilder("Pawniard", 0.5f, BodyPlan.Biped, V(0, 0.13f, 0)) { Coat = Metal };
        var red = Rgb(200, 56, 60);
        var black = Rgb(50, 48, 56);
        var steel = Rgb(206, 208, 216);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.08f, 0));
            b.Limb(leg, V(0.03f * s, 0.09f, 0), V(0.04f * s, 0.025f, 0.01f), 0.02f, 0.018f, red);
            b.Ell(leg, V(0.042f * s, 0.018f, 0.02f), V(0.022f, 0.018f, 0.032f), red);
            var shoulder = V(0.05f * s, 0.17f, 0.01f);
            int arm = b.Arm(s, shoulder);
            var wrist = V(0.09f * s, 0.13f, 0.04f);
            b.Limb(arm, shoulder, wrist, 0.015f, 0.013f, red);
            Blade(b, arm, wrist, wrist + V(0.07f * s, 0.03f, 0.05f), 0.022f, steel, V(0, 1f, 0), 0.25f, Metal);
        });
        b.Ell(Body, V(0, 0.13f, 0), V(0.045f, 0.06f, 0.04f), black);
        foreach (float y in new[] { 0.11f, 0.15f })
            b.Torus(Body, V(0, y, 0), 0.045f, 0.008f, steel, sz: 0.9f, mat: Metal, blend: 0.004f);
        int head = b.Head(V(0, 0.18f, 0.01f));
        var c = V(0, 0.24f, 0.015f);
        var r = V(0.065f, 0.06f, 0.06f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, -0.012f, 0.045f), V(0.05f, 0.04f, 0.035f), black);
        // A steel crescent blade standing up from its brow
        Sickle(b, head, c + V(0, 0.045f, 0.03f), c + V(0, 0.1f, 0.0f), c + V(0, 0.12f, -0.06f), 0.03f, steel, V(1f, 0, 0));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(250, 196, 50), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Bisharp: a tall warrior of blades, red armour over a black body, a ring of steel blades round its waist and more
    /// across its chest, great steel blades on its forearms, blade-tipped feet, and a gold axe-head crest on its red helmet.
    /// </summary>
    private static PokeBuilder Bisharp()
    {
        var b = new PokeBuilder("Bisharp", 1f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Metal };
        var red = Rgb(196, 50, 56);
        var black = Rgb(48, 46, 54);
        var steel = Rgb(214, 216, 224);
        var gold = Rgb(236, 196, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.27f, 0));
            var knee = V(0.06f * s, 0.15f, 0.02f);
            var ankle = V(0.06f * s, 0.05f, 0);
            b.Limb(leg, V(0.045f * s, 0.28f, 0), knee, 0.04f, 0.026f, red);
            b.Limb(leg, knee, ankle, 0.024f, 0.018f, red);
            b.Ell(leg, ankle + V(0, -0.02f, 0.01f), V(0.02f, 0.03f, 0.025f), red);
            Blade(b, leg, ankle + V(0, -0.03f, 0.02f), ankle + V(0, -0.04f, 0.1f), 0.02f, steel, V(0, 1f, 0), 0.3f, Metal);
            var shoulder = V(0.08f * s, 0.52f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.14f * s, 0.43f, 0.04f);
            var hand = V(0.15f * s, 0.34f, 0.1f);
            b.Ell(arm, shoulder, V(0.045f, 0.04f, 0.045f), red);
            b.Limb(arm, shoulder, elbow, 0.022f, 0.018f, black);
            b.Limb(arm, elbow, hand, 0.024f, 0.02f, red);
            b.Ell(arm, hand, V(0.02f, 0.02f, 0.02f), black);
            Sickle(b, arm, elbow + V(0.01f * s, 0, 0.01f), elbow + V(0.06f * s, 0.06f, 0.0f), elbow + V(0.07f * s, 0.16f, -0.03f), 0.03f, steel, V(s, 0, 0.3f));
        });
        b.Ell(Body, V(0, 0.4f, 0), V(0.065f, 0.13f, 0.055f), black);
        b.Ell(Body, V(0, 0.48f, 0.01f), V(0.075f, 0.05f, 0.06f), red, blend: 0.015f);
        // Steel blades round its waist like a disc, and two across its chest
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Degree;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            Blade(b, Body, V(0, 0.32f, 0) + d * 0.04f, V(0, 0.33f, 0) + d * 0.11f, 0.025f, steel, V(0, 1f, 0), 0.3f, Metal);
        }
        PokeBuilder.Both(s => Blade(b, Body, V(0.01f * s, 0.42f, 0.05f), V(0.075f * s, 0.38f, 0.05f), 0.018f, steel, V(0, 0, 1f), 0.3f, Metal));
        int head = b.Head(V(0, 0.55f, 0.01f));
        var c = V(0, 0.62f, 0.015f);
        var r = V(0.06f, 0.06f, 0.06f);
        b.Limb(head, V(0, 0.52f, 0.005f), c, 0.025f, 0.025f, black);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, -0.012f, 0.045f), V(0.045f, 0.04f, 0.03f), black);
        // The gold axe head of its crest
        b.Limb(head, c + V(0, 0.04f, 0), c + V(0, 0.09f, -0.01f), 0.014f, 0.012f, gold, Metal);
        Blade(b, head, c + V(0, 0.1f, -0.01f), c + V(0.07f, 0.13f, -0.01f), 0.04f, gold, V(0, 0, 1f), 0.25f, Metal);
        Blade(b, head, c + V(0, 0.1f, -0.01f), c + V(-0.07f, 0.13f, -0.01f), 0.04f, gold, V(0, 0, 1f), 0.25f, Metal);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.045f * s, 0.01f, 0.03f), V(0.02f, 0.03f, 0.03f), steel, soft: 0.006f);
            var at = On(c, r, 0.022f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(250, 196, 50), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Bouffalant

    /// <summary>Bouffalant: a brown bison with a great dark afro over its head and shoulders, broad cream horns ringed with gold, dark legs and a thin tail.</summary>
    private static PokeBuilder Bouffalant()
    {
        var b = new PokeBuilder("Bouffalant", 1f, BodyPlan.Quadruped, V(0, 0.36f, -0.02f)) { Coat = Fur };
        var brown = Rgb(150, 104, 66);
        var dark = Rgb(96, 72, 54);
        var afro = Rgb(72, 56, 48);
        var horn = Rgb(232, 220, 178);
        var gold = Rgb(226, 184, 72);
        BeastLegs(b, 0.09f, 0.32f, 0.17f, -0.2f, 0.06f, brown, Rgb(70, 56, 50), dark);
        var bc = V(0, 0.38f, -0.02f);
        b.Ell(Body, bc, V(0.14f, 0.13f, 0.26f), brown);
        b.Ell(Body, bc + V(0, 0.04f, 0.14f), V(0.14f, 0.14f, 0.12f), brown, blend: 0.04f);
        int tail = b.Tail(V(0, 0.42f, -0.27f));
        var tp = Smooth(3, V(0, 0.42f, -0.27f), V(0, 0.36f, -0.33f), V(0, 0.26f, -0.35f));
        b.Tube(tail, tp, 0.012f, 0.008f, brown, blend: 0f);
        b.Ell(tail, tp[^1] + V(0, -0.02f, 0), V(0.02f, 0.035f, 0.02f), afro);
        // A great dark afro over its head and shoulders
        int head = b.Head(V(0, 0.44f, 0.18f));
        var ac = V(0, 0.52f, 0.17f);
        var ar = V(0.16f, 0.14f, 0.13f);
        b.Ell(head, ac, ar, afro);
        Lumps(b, head, ac, ar, 26, 0.05f, afro, PixelCanvas.Mix(afro, White, 0.08f), 2f);
        // Its face beneath, and horns curving out to the sides
        var c = V(0, 0.4f, 0.3f);
        var r = V(0.07f, 0.065f, 0.07f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.03f, 0.05f), V(0.055f, 0.04f, 0.04f), dark);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.022f * s, -0.035f, 0.09f), V(0.01f, 0.008f, 0.006f), Black, soft: 0.004f);
            var root = c + V(0.06f * s, 0.03f, -0.02f);
            var tp2 = Smooth(3, root, root + V(0.08f * s, 0.01f, 0), root + V(0.14f * s, 0.05f, 0.03f), root + V(0.16f * s, 0.11f, 0.04f));
            b.Tube(head, tp2, 0.035f, 0.012f, horn, Shell, 0f);
            foreach (int i in new[] { 3, 5 })
                b.Torus(head, tp2[i], 0.034f - 0.004f * i, 0.008f, gold, Euler(tp2[i + 1] - tp2[i]), mat: Metal, blend: 0.004f);
            var at = On(c, r, 0.045f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(250, 200, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Rufflet and Braviary

    /// <summary>Rufflet: an eaglet in a great cream ruff of down round its head, a navy face with round white eyes, a red and white plume on its brow, a yellow beak, a navy body and yellow feet.</summary>
    private static PokeBuilder Rufflet()
    {
        var b = new PokeBuilder("Rufflet", 0.55f, BodyPlan.Bird, V(0, 0.13f, 0)) { Coat = Fur };
        var navy = Rgb(108, 100, 162);
        var cream = Rgb(242, 234, 200);
        var yellow = Rgb(244, 200, 80);
        var red = Rgb(220, 60, 50);
        BirdLegs(b, 0.035f, 0.09f, 0.01f, 0.012f, yellow);
        b.Ell(Body, V(0, 0.13f, 0), V(0.07f, 0.065f, 0.065f), navy);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.06f * s, 0.15f, -0.01f));
            Frond(b, wing, V(0.06f * s, 0.15f, -0.01f), V(0.1f * s, 0.1f, -0.06f), 0.035f, navy, V(s, 0.2f, 0), 0.25f);
        });
        int tail = b.Tail(V(0, 0.11f, -0.05f));
        Frond(b, tail, V(0, 0.11f, -0.05f), V(0, 0.08f, -0.12f), 0.035f, navy, V(0, 1f, 0.3f), 0.25f);
        // The great ruff of down, its face looking out of the front
        int head = b.Head(V(0, 0.18f, 0.01f));
        var c = V(0, 0.27f, 0);
        var r = V(0.1f, 0.11f, 0.09f);
        b.Ell(head, c, r, cream);
        FurTufts(b, head, c, r, 30, 0.04f, 0.025f, cream, 0.6f, -0.9f, 0.3f);
        var fc = c + V(0, -0.01f, 0.055f);
        var fr = V(0.06f, 0.055f, 0.045f);
        b.Ell(head, fc, fr, navy, blend: 0.015f);
        b.Spike(head, fc + V(0, -0.02f, 0.035f), fc + V(0, -0.045f, 0.075f), 0.016f, yellow, 0.7f, Shell);
        Blade(b, head, c + V(0, 0.06f, 0.06f), c + V(0.03f, 0.17f, 0.03f), 0.026f, red, V(0, 0, 1f), 0.3f);
        Blade(b, head, c + V(0, 0.06f, 0.05f), c + V(-0.04f, 0.15f, 0), 0.022f, White, V(0, 0, 1f), 0.3f);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.026f * s, fc.Y + 0.012f);
            b.Eye(head, at, Outward(fc, fr, at), 0.016f, Rgb(40, 40, 60), sclera: true, glare: true);
        });
        return b;
    }

    /// <summary>
    /// Braviary and its Hisuian form: a great eagle in flight, wings spread, talons forward, a crest of white feathers
    /// swept back from its head and a hooked yellow beak. Braviary is navy with red over its wings, its crest tipped
    /// red and blue and its tail red, yellow and blue; the Hisuian is dark, its wings white above, its face masked in
    /// purple and pink.
    /// </summary>
    private static PokeBuilder BraviaryBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Braviary-Hisui" : "Braviary", 1f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var body = hisui ? Rgb(58, 54, 64) : Rgb(80, 82, 150);
        var cover = hisui ? Rgb(238, 236, 242) : Rgb(200, 44, 50);
        var feather = hisui ? Rgb(64, 60, 70) : Rgb(80, 82, 150);
        var tip = hisui ? Rgb(36, 34, 42) : Rgb(60, 110, 200);
        var yellow = Rgb(244, 200, 80);
        var bc = V(0, 0.42f, 0);
        var br = V(0.1f, 0.12f, 0.14f);
        b.Ell(Body, bc, br, body, V(-25f, 0, 0));
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.07f * s, 0.06f, 0.02f);
            var wrist = shoulder + V(0.2f * s, 0.12f, -0.04f);
            int wing = SpreadWing(b, s, shoulder, wrist, 7, 75f, -15f, 0.22f, 0.04f, feather, tip, 0.3f);
            Frond(b, wing, shoulder, wrist + V(0.02f * s, 0, 0), 0.06f, cover, V(0, 1f, 0.4f), 0.25f, Fur, 0.01f);
        });
        // A tail of three broad feathers, fanned
        int tail = b.Tail(bc + V(0, -0.06f, -0.12f));
        var tails = hisui ? new[] { White, White, White } : new[] { Rgb(200, 44, 50), Rgb(244, 200, 80), Rgb(60, 110, 200) };
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 0.045f;
            Frond(b, tail, bc + V(x * 0.4f, -0.05f, -0.1f), bc + V(x * 1.6f, -0.12f, -0.3f), 0.04f, tails[i], V(0, 1f, 0.3f), 0.22f);
        }
        DanglingLegs(b, 0.05f, bc.Y - 0.08f, 0.06f, 0.12f, 0.018f, yellow);
        // A white head with its crest swept back, a hooked yellow beak
        int head = b.Head(bc + V(0, 0.1f, 0.12f));
        var c = bc + V(0, 0.17f, 0.17f);
        var r = V(0.06f, 0.06f, 0.065f);
        b.Ell(head, c, r, White);
        b.Ell(head, bc + V(0, 0.1f, 0.12f), V(0.07f, 0.06f, 0.07f), hisui ? White : body, blend: 0.03f);
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 0.025f;
            var root = c + V(x, 0.04f, -0.02f);
            var end = c + V(x * 1.8f, 0.09f + 0.02f * (2 - MathF.Abs(i - 2)), -0.16f);
            Frond(b, head, root, end, 0.03f, White, V(0, 1f, 0.2f), 0.25f);
            if (!hisui) b.PaintEll(head, Vector3.Lerp(root, end, 0.82f), V(0.025f, 0.02f, 0.03f), i % 2 == 0 ? Rgb(200, 44, 50) : Rgb(60, 110, 200), soft: 0.006f);
        }
        b.Spike(head, c + V(0, 0, 0.05f), c + V(0, -0.04f, 0.13f), 0.025f, yellow, 0.7f, Shell);
        if (hisui)
        {
            PokeBuilder.Both(s => b.PaintEll(head, c + V(0.035f * s, 0.01f, 0.04f), V(0.03f, 0.025f, 0.03f), Rgb(170, 110, 190)));
            b.PaintEll(head, c + V(0, 0.03f, 0.05f), V(0.02f, 0.015f, 0.02f), Rgb(230, 120, 160));
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, hisui ? Rgb(240, 200, 80) : Rgb(40, 40, 60), glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Braviary() => BraviaryBuild(false);

    // ------------------------------------------------------------------ Vullaby and Mandibuzz

    /// <summary>Vullaby: a vulture chick sitting in the lower half of a pale skull, a heart on its front, a ruff of dark down, little brown wings, a pink head with a dark topknot and pink legs.</summary>
    private static PokeBuilder Vullaby()
    {
        var b = new PokeBuilder("Vullaby", 0.55f, BodyPlan.Bird, V(0, 0.14f, 0)) { Coat = Fur };
        var bone = Rgb(236, 222, 186);
        var brown = Rgb(96, 76, 66);
        var pink = Rgb(226, 150, 170);
        BirdLegs(b, 0.04f, 0.08f, 0, 0.012f, pink);
        var bc = V(0, 0.13f, 0);
        var br = V(0.09f, 0.085f, 0.085f);
        b.Ell(Body, bc, br, bone, mat: Shell);
        PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0.07f * s, -0.04f, 0.02f), V(0.04f, 0.04f, 0.05f), brown));
        // A heart on its front, two rounds over a point
        PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0.012f * s, 0.01f, 0.085f), V(0.014f, 0.013f, 0.01f), brown, soft: 0.004f));
        var point = On(bc, br, 0, bc.Y - 0.006f);
        b.Mark(Body, point, Outward(bc, br, point), 0.024f, 0.016f, brown, MarkShape.Triangle);
        b.Ell(Body, bc + V(0, 0.08f, 0), V(0.075f, 0.035f, 0.07f), brown);
        FurTufts(b, Body, bc + V(0, 0.08f, 0), V(0.075f, 0.035f, 0.07f), 16, 0.03f, 0.016f, brown, 1.1f, -1f, 0.2f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.07f * s, 0.07f, 0));
            Frond(b, wing, bc + V(0.07f * s, 0.07f, 0), bc + V(0.15f * s, 0.05f, -0.02f), 0.035f, brown, V(0, 1f, 0.3f), 0.25f);
            Frond(b, wing, bc + V(0.07f * s, 0.06f, 0), bc + V(0.14f * s, 0.01f, -0.02f), 0.03f, brown, V(0, 1f, 0.3f), 0.25f);
        });
        int head = b.Head(bc + V(0, 0.1f, 0.01f));
        var c = bc + V(0, 0.15f, 0.015f);
        var r = V(0.05f, 0.048f, 0.048f);
        b.Ell(head, c, r, pink);
        foreach (var (dx, dz) in new[] { (-0.02f, 0f), (0.015f, -0.01f), (0f, -0.025f) })
            Frond(b, head, c + V(dx, 0.035f, dz), c + V(dx * 2f, 0.1f, dz - 0.01f), 0.022f, brown, V(0, 0, 1f), 0.25f);
        b.Spike(head, c + V(0, -0.005f, 0.04f), c + V(0, -0.025f, 0.07f), 0.012f, Rgb(150, 150, 160), 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(140, 40, 60), glare: true);
        });
        return b;
    }

    /// <summary>Mandibuzz: a tall vulture, brown with its wings folded, a cape of pale bones hanging down its front with a jagged hem, a bare pink neck and head, a hooked gray beak and a dark topknot with a bone in it.</summary>
    private static PokeBuilder Mandibuzz()
    {
        var b = new PokeBuilder("Mandibuzz", 0.9f, BodyPlan.Bird, V(0, 0.32f, 0)) { Coat = Fur };
        var brown = Rgb(110, 92, 82);
        var light = Rgb(156, 136, 120);
        var beige = Rgb(232, 214, 170);
        var pink = Rgb(222, 140, 160);
        var beak = Rgb(150, 150, 160);
        BirdLegs(b, 0.05f, 0.18f, 0, 0.016f, pink);
        var bc = V(0, 0.32f, 0);
        b.Ell(Body, bc, V(0.1f, 0.15f, 0.09f), brown);
        // A cape of pale bone down its front, its hem jagged
        var cc = bc + V(0, 0.01f, 0.05f);
        b.Ell(Body, cc, V(0.085f, 0.13f, 0.06f), beige, mat: Shell, blend: 0.015f);
        for (int i = -3; i <= 3; i++)
            b.Spike(Body, cc + V(i * 0.018f, -0.09f, 0.03f - 0.002f * i * i), cc + V(i * 0.024f, -0.17f, 0.035f), 0.014f, beige, 0.5f, Shell);
        PokeBuilder.Both(s =>
        {
            int wing = FoldedWing(b, s, bc + V(0.08f * s, 0.1f, 0), bc + V(0.11f * s, -0.15f, -0.08f), 0.07f, brown, light);
            b.PaintEll(wing, bc + V(0.1f * s, 0.02f, 0), V(0.02f, 0.1f, 0.06f), light);
        });
        // A ruff of brown down round the base of its bare pink neck
        b.Ell(Body, bc + V(0, 0.14f, 0.01f), V(0.07f, 0.03f, 0.065f), brown);
        FurTufts(b, Body, bc + V(0, 0.14f, 0.01f), V(0.07f, 0.03f, 0.065f), 14, 0.025f, 0.014f, brown, 1.1f, -1f, 0.2f);
        int head = b.Head(bc + V(0, 0.15f, 0.02f));
        var c = bc + V(0, 0.27f, 0.04f);
        var r = V(0.05f, 0.045f, 0.055f);
        b.Tube(head, Smooth(3, bc + V(0, 0.14f, 0.02f), bc + V(0, 0.2f, 0.01f), c + V(0, -0.02f, -0.01f)), 0.022f, 0.02f, pink, blend: 0f);
        b.Ell(head, c, r, pink);
        b.Spike(head, c + V(0, 0.005f, 0.045f), c + V(0, -0.035f, 0.11f), 0.02f, beak, 0.7f, Shell);
        foreach (var (dx, dy) in new[] { (-0.015f, 0.02f), (0.012f, 0.03f), (0f, 0.01f) })
            Frond(b, head, c + V(dx, dy + 0.02f, -0.04f), c + V(dx * 2f, dy + 0.08f, -0.09f), 0.025f, Rgb(76, 62, 56), V(0, 0, 1f), 0.25f);
        b.Limb(head, c + V(-0.03f, 0.06f, -0.06f), c + V(0.03f, 0.06f, -0.06f), 0.006f, 0.006f, beige, Shell);
        PokeBuilder.Both(s => b.Ell(head, c + V(0.035f * s, 0.06f, -0.06f), V(0.01f, 0.01f, 0.008f), beige, mat: Shell, blend: 0.004f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(160, 40, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Heatmor and Durant

    /// <summary>Heatmor: an anteater of fire, red striped with yellow, a long gray snout curling down with a flame at its end, hands like pierced plates with yellow claws and a tail that is a pipe.</summary>
    private static PokeBuilder Heatmor()
    {
        var b = new PokeBuilder("Heatmor", 0.95f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var red = Rgb(188, 60, 50);
        var yellow = Rgb(240, 196, 70);
        var gray = Rgb(170, 150, 132);
        var dark = Rgb(80, 66, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.14f, 0));
            b.Limb(leg, V(0.07f * s, 0.15f, 0), V(0.08f * s, 0.035f, 0.02f), 0.05f, 0.04f, gray);
            b.Ell(leg, V(0.082f * s, 0.025f, 0.04f), V(0.04f, 0.025f, 0.055f), gray);
            var shoulder = V(0.1f * s, 0.35f, 0.03f);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.17f * s, 0.27f, 0.13f);
            b.Limb(arm, shoulder, hand, 0.035f, 0.03f, gray);
            // A hand like a round plate pierced with holes, three yellow claws below it
            b.Ell(arm, hand, V(0.045f, 0.045f, 0.03f), gray, V(0, 30f * s, 0));
            foreach (var (x, y) in new[] { (-0.018f, 0.012f), (0.018f, 0.012f), (0f, -0.02f), (0f, 0.03f) })
                b.PaintEll(arm, hand + V(x * MathF.Cos(30f * Degree) * s, y, 0.03f), V(0.008f, 0.008f, 0.02f), dark, soft: 0.004f);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand + V(t * 0.02f * s, -0.035f, 0.01f), hand + V(t * 0.025f * s, -0.09f, 0.05f), 0.012f, yellow, 0.6f, Shell);
        });
        var bc = V(0, 0.28f, -0.01f);
        var br = V(0.13f, 0.15f, 0.12f);
        b.Ell(Body, bc, br, red, V(15f, 0, 0));
        foreach (float a in new[] { -70f, -35f, 0f, 35f, 70f })
        {
            var d = V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree));
            var at = bc + V(d.X * br.X, 0, d.Z * br.Z);
            b.PaintEll(Body, at, V(0.012f, 0.13f, 0.012f), yellow, V(0, -a, 6f * MathF.Sign(a)), 0.006f);
        }
        b.Ell(Body, bc + V(0, 0.12f, 0.05f), V(0.09f, 0.05f, 0.08f), gray, blend: 0.03f);
        // A tail that is a pipe, its end open
        int tail = b.Tail(bc + V(0, -0.06f, -0.1f));
        var pipe = bc + V(0, 0.0f, -0.25f);
        b.Limb(tail, bc + V(0, -0.06f, -0.1f), pipe, 0.035f, 0.035f, dark);
        b.Torus(tail, pipe, 0.036f, 0.01f, dark, V(90f, 0, 0), blend: 0.006f);
        b.Cut(tail, pipe + V(0, 0, -0.01f), V(0.026f, 0.026f, 0.03f));
        b.PaintEll(tail, pipe, V(0.03f, 0.03f, 0.03f), Rgb(36, 30, 30));
        // A gray head with a long snout curling down, a flame at its end
        int head = b.Head(V(0, 0.42f, 0.06f));
        var c = V(0, 0.46f, 0.08f);
        var r = V(0.07f, 0.06f, 0.07f);
        b.Ell(head, c, r, gray);
        var snout = Smooth(3, c + V(0, 0, 0.05f), c + V(0, 0.01f, 0.14f), c + V(0, -0.04f, 0.22f), c + V(0, -0.1f, 0.24f));
        b.Tube(head, snout, 0.034f, 0.022f, gray, blend: 0f);
        b.PaintEll(head, snout[^1], V(0.024f, 0.012f, 0.024f), dark, soft: 0.004f);
        FlameTongue(b, head, snout[^1] + V(0, -0.015f, 0), snout[^1] + V(0, -0.1f, 0.03f), 0.022f, Rgb(250, 120, 50), Rgb(255, 230, 120));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.025f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 36, 36), glare: true);
        });
        return b;
    }

    /// <summary>Durant: an ant of steel, six thin dark legs, a gray armoured head with red eyes and great ring-shaped mandibles, two antennae and a bulbous spotted abdomen.</summary>
    private static PokeBuilder Durant()
    {
        var b = new PokeBuilder("Durant", 0.5f, BodyPlan.Quadruped, V(0, 0.09f, 0)) { Coat = Metal };
        var steel = Rgb(174, 176, 186);
        var dark = Rgb(66, 66, 76);
        foreach (var (z, name) in new[] { (0.04f, "leg"), (0f, "mid"), (-0.04f, "hind") })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.03f * s, 0.08f, z);
                int leg = name == "leg" ? b.Leg(s, hip) : name == "hind" ? b.Leg(s, hip, false) : b.Part(s < 0 ? "midL" : "midR", Root, hip, PokeRole.Leg, 0.8f + s * 0.5f, s);
                var knee = V(0.11f * s, 0.13f, z * 1.8f);
                var foot = V(0.15f * s, 0.008f, z * 2.6f);
                b.Limb(leg, hip, knee, 0.01f, 0.008f, dark, Metal);
                b.Limb(leg, knee, foot, 0.008f, 0.006f, dark, Metal);
            });
        // A small thorax and a bulbous abdomen with dark spots
        b.Ell(Body, V(0, 0.09f, 0), V(0.04f, 0.035f, 0.05f), steel);
        var ac = V(0, 0.11f, -0.1f);
        var ar = V(0.07f, 0.06f, 0.08f);
        b.Ell(Body, ac, ar, steel);
        foreach (var dir in new[] { V(0.6f, 0.6f, -0.3f), V(-0.6f, 0.6f, -0.3f), V(0, 0.8f, -0.6f), V(0.8f, 0.1f, -0.5f), V(-0.8f, 0.1f, -0.5f) })
        {
            var at = Out(ac, ar, default, dir);
            b.PaintEll(Body, at, V(0.014f, 0.014f, 0.014f), dark, soft: 0.004f);
        }
        // A big armoured head with ring mandibles and red eyes
        int head = b.Head(V(0, 0.1f, 0.04f));
        var c = V(0, 0.11f, 0.1f);
        var r = V(0.065f, 0.055f, 0.06f);
        b.Ell(head, c, r, steel);
        b.Ell(head, c + V(0, 0.03f, -0.01f), V(0.068f, 0.03f, 0.06f), PixelCanvas.Mix(steel, White, 0.25f), blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            b.Torus(head, c + V(0.045f * s, -0.035f, 0.075f), 0.03f, 0.01f, steel, V(0, 0, 90f), 1f, 1.3f, Metal, 0.006f);
            Antenna(b, head, c + V(0.025f * s, 0.05f, 0.02f), c + V(0.05f * s, 0.11f, 0.03f), c + V(0.09f * s, 0.13f, 0.06f), 0.005f, dark, dark, 0.012f, Metal);
            var at = On(c, r, 0.035f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(220, 40, 40), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Deino line

    /// <summary>A head of the Deino line: a blue jaw open under a hood of dark shaggy fur that hides the eyes, with points like ears or a horn on top.</summary>
    private static void FurHoodHead(PokeBuilder b, int bone, Vector3 c, float k, Color blue, Color fur, bool horn, bool ears)
    {
        var r = V(0.07f, 0.065f, 0.07f) * k;
        b.Ell(bone, c, r, fur);
        FurTufts(b, bone, c + V(0, -0.02f * k, 0), r, 18, 0.025f * k, 0.016f * k, fur, 0.9f, -0.5f, 0.6f);
        var jaw = c + V(0, -0.045f * k, 0.035f * k);
        b.Ell(bone, jaw, V(0.055f, 0.035f, 0.05f) * k, blue, blend: 0.015f);
        Grin(b, bone, jaw + V(0, -0.005f * k, 0.035f * k), V(0.035f, 0.016f, 0.025f) * k, Rgb(220, 100, 120));
        if (horn) b.Spike(bone, c + V(0, 0.05f * k, -0.01f * k), c + V(0, 0.14f * k, -0.05f * k), 0.025f * k, fur, 0.8f);
        if (ears)
            PokeBuilder.Both(s => b.Spike(bone, c + V(0.035f * s * k, 0.045f * k, -0.01f * k), c + V(0.07f * s * k, 0.13f * k, -0.03f * k), 0.022f * k, fur, 0.6f));
    }

    /// <summary>Deino: a small blue dragon on stubby legs, its head buried under dark shaggy fur with a horn on top, its open jaw below, a ruff of dark fur spotted pink round its neck.</summary>
    private static PokeBuilder Deino()
    {
        var b = new PokeBuilder("Deino", 0.6f, BodyPlan.Quadruped, V(0, 0.13f, 0)) { Coat = Fur };
        var blue = Rgb(70, 110, 180);
        var fur = Rgb(70, 60, 64);
        var spot = Rgb(176, 70, 120);
        BeastLegs(b, 0.05f, 0.1f, 0.06f, -0.05f, 0.032f, blue, blue);
        b.Ell(Body, V(0, 0.13f, 0), V(0.07f, 0.06f, 0.09f), blue);
        var rc = V(0, 0.17f, 0.05f);
        var rr = V(0.08f, 0.05f, 0.06f);
        b.Ell(Body, rc, rr, fur);
        FurTufts(b, Body, rc, rr, 16, 0.03f, 0.016f, fur, 1.1f, -1f, 0.4f);
        foreach (var dir in new[] { V(0.7f, 0.2f, 0.6f), V(-0.6f, 0.3f, 0.7f), V(0.2f, 0.4f, 0.9f) })
            b.PaintEll(Body, Out(rc, rr, default, dir), V(0.012f, 0.01f, 0.012f), spot, soft: 0.004f);
        int tail = b.Tail(V(0, 0.12f, -0.08f));
        b.Tube(tail, Smooth(3, V(0, 0.12f, -0.08f), V(0, 0.1f, -0.13f), V(0, 0.07f, -0.17f)), 0.025f, 0.008f, blue, blend: 0f);
        int head = b.Head(V(0, 0.2f, 0.08f));
        FurHoodHead(b, head, V(0, 0.26f, 0.11f), 1f, blue, fur, true, false);
        return b;
    }

    /// <summary>Zweilous: a stocky blue dragon with two heads on short necks, each blind under its dark hood with two points like ears, a shaggy dark coat spotted pink and small dark wings.</summary>
    private static PokeBuilder Zweilous()
    {
        var b = new PokeBuilder("Zweilous", 0.85f, BodyPlan.Quadruped, V(0, 0.22f, 0)) { Coat = Fur };
        var blue = Rgb(70, 110, 180);
        var fur = Rgb(70, 60, 64);
        var spot = Rgb(176, 70, 120);
        BeastLegs(b, 0.08f, 0.17f, 0.09f, -0.09f, 0.05f, blue, blue);
        var bc = V(0, 0.22f, 0);
        var br = V(0.12f, 0.1f, 0.15f);
        b.Ell(Body, bc, br, blue);
        var fc = bc + V(0, 0.03f, 0.02f);
        var fr = V(0.13f, 0.1f, 0.15f);
        b.Ell(Body, fc, fr, fur, blend: 0.02f);
        FurTufts(b, Body, fc, fr, 30, 0.04f, 0.02f, fur, 1.1f, -0.7f, 0.5f);
        foreach (var dir in new[] { V(0.8f, 0.3f, 0.3f), V(-0.8f, 0.2f, 0.4f), V(0.5f, 0.6f, -0.4f), V(-0.4f, 0.5f, -0.6f), V(0, 0.4f, 0.9f) })
            b.PaintEll(Body, Out(fc, fr, default, dir), V(0.018f, 0.014f, 0.018f), spot, soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.08f * s, 0.1f, -0.04f));
            foreach (var (dy, dz) in new[] { (0.1f, -0.05f), (0.06f, -0.1f) })
                Frond(b, wing, bc + V(0.08f * s, 0.1f, -0.04f), bc + V(0.17f * s, 0.1f + dy, -0.04f + dz), 0.035f, fur, V(s, 0.2f, 0), 0.25f);
        });
        int tail = b.Tail(bc + V(0, -0.02f, -0.14f));
        b.Tube(tail, Smooth(3, bc + V(0, -0.02f, -0.14f), bc + V(0, -0.06f, -0.22f), bc + V(0.03f, -0.1f, -0.3f)), 0.04f, 0.01f, blue, blend: 0f);
        PokeBuilder.Both(s =>
        {
            int head = b.Part(s < 0 ? "headL" : "headR", Body, bc + V(0.06f * s, 0.06f, 0.12f), PokeRole.Head, s < 0 ? 0.3f : 1.9f, s);
            var c = bc + V(0.1f * s, 0.18f, 0.22f);
            b.Limb(head, bc + V(0.05f * s, 0.06f, 0.1f), c + V(0, -0.05f, -0.03f), 0.045f, 0.04f, blue);
            FurHoodHead(b, head, c, 1.15f, blue, fur, false, true);
        });
        return b;
    }

    /// <summary>
    /// Hydreigon: a blue three-headed dragon in flight on six ragged black wings, a ruff of black fur striped magenta
    /// round its chest, its main head under a black hood ringed with magenta spikes, red eyes, its arms ending in two
    /// more heads, and a long tail striped purple.
    /// </summary>
    private static PokeBuilder Hydreigon()
    {
        var b = new PokeBuilder("Hydreigon", 1f, BodyPlan.Bird, V(0, 0.5f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(70, 110, 180);
        var fur = Rgb(54, 48, 54);
        var magenta = Rgb(196, 50, 120);
        var purple = Rgb(130, 60, 140);
        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc, V(0.09f, 0.13f, 0.09f), blue);
        var rc = bc + V(0, 0.06f, 0.01f);
        var rr = V(0.11f, 0.07f, 0.1f);
        b.Ell(Body, rc, rr, fur, blend: 0.02f);
        FurTufts(b, Body, rc, rr, 22, 0.045f, 0.022f, fur, 1.1f, -1f, 0.5f);
        b.PaintTorus(Body, rc + V(0, -0.02f, 0), 0.105f, 0.012f, magenta, sz: 0.95f);
        // Six ragged black wings, three to a side
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.05f * s, 0.08f, -0.06f);
            int wing = b.Wing(s, root);
            foreach (var (dx, dy, len) in new[] { (0.9f, 0.9f, 0.3f), (1f, 0.35f, 0.32f), (0.9f, -0.2f, 0.26f) })
            {
                var dir = Vector3.Normalize(V(dx * s, dy, -0.35f));
                Frond(b, wing, root, root + dir * len, 0.05f, fur, V(0, 0, 1f), 0.2f);
                foreach (float t in new[] { 0.6f, 0.85f })
                    b.Spike(wing, root + dir * len * t, root + dir * len * t + Vector3.Normalize(V(0, -1f, -0.2f) + dir * 0.4f) * 0.06f, 0.012f, fur, 0.5f);
            }
        });
        // A long blue tail sweeping down and back, striped purple
        int tail = b.Tail(bc + V(0, -0.1f, -0.03f));
        var tp = Smooth(3, bc + V(0, -0.1f, -0.03f), bc + V(0, -0.25f, -0.06f), bc + V(0.04f, -0.36f, -0.16f), bc + V(0.02f, -0.4f, -0.3f));
        b.Tube(tail, tp, 0.05f, 0.008f, blue, blend: 0f);
        for (int i = 2; i < tp.Length - 1; i += 2)
            b.PaintTorus(tail, tp[i], 0.05f - 0.04f * i / (tp.Length - 1), 0.009f, purple, Euler(tp[i + 1] - tp[i]));
        // Arms ending in heads of their own
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.08f * s, 0.06f, 0.03f);
            int arm = b.Arm(s, shoulder);
            var hand = bc + V(0.2f * s, 0.02f, 0.13f);
            b.Limb(arm, shoulder, hand, 0.035f, 0.03f, blue);
            FurHoodHead(b, arm, hand + V(0, 0.02f, 0.03f), 0.75f, blue, fur, false, false);
            foreach (var d in new[] { V(0.4f * s, 0.9f, -0.2f), V(0.9f * s, 0.4f, -0.2f), V(0.2f * s, 0.7f, -0.7f) })
                b.Spike(arm, hand + V(0, 0.04f, 0.02f) + d * 0.03f, hand + V(0, 0.04f, 0.02f) + d * 0.075f, 0.01f, magenta, 0.6f);
        });
        // The main head on a short neck, under a black hood ringed with magenta spikes
        int head = b.Head(bc + V(0, 0.12f, 0.03f));
        var c = bc + V(0, 0.22f, 0.07f);
        var r = V(0.07f, 0.065f, 0.07f);
        b.Limb(head, bc + V(0, 0.1f, 0.02f), c + V(0, -0.04f, -0.02f), 0.04f, 0.038f, blue);
        b.Ell(head, c + V(0, 0.015f, -0.01f), r, fur);
        var jaw = c + V(0, -0.03f, 0.04f);
        var jr = V(0.055f, 0.04f, 0.055f);
        b.Ell(head, jaw, jr, blue, blend: 0.015f);
        Grin(b, head, jaw + V(0, -0.012f, 0.045f), V(0.035f, 0.015f, 0.025f), Rgb(220, 100, 120));
        foreach (float a in new[] { -80f, -50f, -20f, 20f, 50f, 80f, 0f })
        {
            var d = V(MathF.Sin(a * Degree), MathF.Cos(a * Degree) * 0.9f, -0.35f);
            b.Spike(head, c + V(0, 0.015f, -0.02f) + d * 0.05f, c + V(0, 0.015f, -0.02f) + d * 0.13f, 0.016f, magenta, 0.6f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(jaw, jr, 0.032f * s, jaw.Y + 0.022f);
            b.Eye(head, at, Outward(jaw, jr, at), 0.013f, Rgb(210, 40, 50), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Larvesta and Volcarona

    /// <summary>Larvesta: a fuzzy white caterpillar, five red horns round its head, blue eyes in a black band across its face, a brown segmented body behind and stubby gray legs.</summary>
    private static PokeBuilder Larvesta()
    {
        var b = new PokeBuilder("Larvesta", 0.75f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Fur };
        var white = Rgb(244, 242, 236);
        var red = Rgb(220, 60, 44);
        var brown = Rgb(176, 128, 74);
        var gray = Rgb(80, 80, 90);
        foreach (var (z, name) in new[] { (0.08f, "leg"), (0f, "mid"), (-0.08f, "hind") })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.06f * s, 0.06f, z);
                int leg = name == "leg" ? b.Leg(s, hip) : name == "hind" ? b.Leg(s, hip, false) : b.Part(s < 0 ? "midL" : "midR", Root, hip, PokeRole.Leg, 0.8f + s * 0.5f, s);
                b.Limb(leg, hip, V(0.08f * s, 0.015f, z + 0.01f), 0.022f, 0.018f, gray);
                b.Ell(leg, V(0.082f * s, 0.014f, z + 0.015f), V(0.02f, 0.014f, 0.022f), gray);
            });
        // A brown segmented body behind
        var ac = V(0, 0.12f, -0.12f);
        b.Ell(Body, ac, V(0.09f, 0.08f, 0.13f), brown);
        foreach (float dz in new[] { -0.05f, -0.01f, 0.03f, 0.07f })
            b.PaintTorus(Body, ac + V(0, 0, -dz), 0.085f - MathF.Abs(dz) * 0.2f, 0.006f, PixelCanvas.Mix(brown, Black, 0.3f), V(90f, 0, 0), 1f, 0.95f);
        // The fuzzy white head, a black band across its face
        int head = b.Head(V(0, 0.16f, 0.04f));
        var c = V(0, 0.18f, 0.06f);
        var r = V(0.14f, 0.13f, 0.13f);
        b.Ell(head, c, r, white);
        FurTufts(b, head, c, r, 34, 0.04f, 0.022f, white, 0.75f, -0.9f, 0.3f);
        b.PaintEll(head, c + V(0, -0.005f, 0.11f), V(0.15f, 0.035f, 0.05f), Rgb(36, 34, 40));
        foreach (float a in new[] { 25f, 155f, 205f, 335f, 90f })
        {
            var d = V(MathF.Cos(a * Degree), MathF.Sin(a * Degree), -0.25f);
            var root = c + Vector3.Normalize(d) * 0.1f;
            b.Spike(head, root, root + Vector3.Normalize(d) * 0.14f, 0.032f, red, 0.6f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.055f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.02f, Rgb(70, 170, 240), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Volcarona: a great moth of the sun, six broad orange wings tipped red and spotted black spread round it like
    /// petals, a ruff of white fuzz, a black face with blue eyes and two red horns, and a gray-blue abdomen hanging below.
    /// </summary>
    private static PokeBuilder Volcarona()
    {
        var b = new PokeBuilder("Volcarona", 1f, BodyPlan.Bird, V(0, 0.46f, 0)) { Coat = Fur }.Hover();
        var orange = Rgb(240, 110, 50);
        var red = Rgb(200, 40, 40);
        var black = Rgb(40, 38, 44);
        var white = Rgb(244, 242, 236);
        var blue = Rgb(120, 150, 180);
        var bc = V(0, 0.46f, 0);
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.03f * s, 0.02f, -0.05f);
            int wing = b.Wing(s, root);
            foreach (var (a, len, wide) in new[] { (72f, 0.3f, 0.075f), (12f, 0.32f, 0.08f), (-48f, 0.26f, 0.068f) })
            {
                var d = V(MathF.Cos(a * Degree) * s, MathF.Sin(a * Degree), 0);
                var mid = root + d * len * 0.55f + V(0, 0, -0.02f);
                b.Ell(wing, mid, V(wide, len * 0.55f, 0.012f), orange, Euler(d), Fur, 0.02f);
                b.PaintEll(wing, root + d * len * 0.98f, V(wide * 0.9f, len * 0.15f, 0.03f), red, Euler(d));
                foreach (var (t, side) in new[] { (0.45f, 0.4f), (0.7f, -0.35f), (0.6f, 0.05f) })
                {
                    var across = V(-d.Y, d.X, 0);
                    b.PaintEll(wing, root + d * len * t + across * side * wide + V(0, 0, -0.02f), V(0.018f, 0.018f, 0.03f), black, soft: 0.005f);
                }
            }
        });
        // A black thorax wrapped in white fuzz, the gray-blue abdomen hanging below
        b.Ell(Body, bc, V(0.06f, 0.08f, 0.06f), black);
        var fc = bc + V(0, 0.02f, 0);
        b.Ell(Body, fc, V(0.09f, 0.06f, 0.08f), white, blend: 0.02f);
        FurTufts(b, Body, fc, V(0.09f, 0.06f, 0.08f), 20, 0.035f, 0.018f, white, 0.8f, -1f, 0.3f);
        var ab = Smooth(3, bc + V(0, -0.05f, 0.01f), bc + V(0, -0.15f, 0.05f), bc + V(0, -0.24f, 0.06f));
        b.Tube(Body, ab, 0.05f, 0.012f, blue, blend: 0f);
        for (int i = 1; i < ab.Length - 1; i += 2)
            b.PaintTorus(Body, ab[i], 0.05f - 0.035f * i / (ab.Length - 1), 0.006f, black, Euler(ab[i + 1] - ab[i]));
        int head = b.Head(bc + V(0, 0.06f, 0.03f));
        var c = bc + V(0, 0.1f, 0.05f);
        var r = V(0.055f, 0.05f, 0.05f);
        b.Ell(head, c, r, black);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, c + V(0.025f * s, 0.035f, -0.01f), c + V(0.06f * s, 0.11f, -0.02f), c + V(0.04f * s, 0.18f, -0.03f)), 0.014f, 0.006f, red, blend: 0f);
            var at = On(c, r, 0.025f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(90, 190, 240), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ The Swords of Justice

    /// <summary>Cobalion: a stately teal stag of steel, a white beard of fur at its chest, tan plates swept back from its shoulders, a great tan crest swept back from its brow, navy lower legs and dark hooves.</summary>
    private static PokeBuilder Cobalion()
    {
        var b = new PokeBuilder("Cobalion", 1f, BodyPlan.Quadruped, V(0, 0.5f, -0.02f)) { Coat = Fur };
        var teal = Rgb(40, 150, 190);
        var navy = Rgb(40, 54, 90);
        var pale = Rgb(176, 214, 232);
        var white = Rgb(238, 238, 242);
        var tan = Rgb(208, 170, 110);
        BeastLegs(b, 0.07f, 0.45f, 0.17f, -0.2f, 0.045f, teal, Rgb(46, 46, 56), navy);
        var bc = V(0, 0.5f, -0.02f);
        b.Ell(Body, bc, V(0.1f, 0.1f, 0.22f), teal);
        foreach (var (x, z) in new[] { (0.05f, -0.05f), (-0.04f, -0.12f), (0.03f, 0.04f), (-0.06f, 0.0f) })
            b.PaintEll(Body, bc + V(x, 0.09f, z), V(0.012f, 0.01f, 0.012f), navy, soft: 0.004f);
        // A beard of white fur on its chest
        var fc = bc + V(0, 0.05f, 0.18f);
        b.Ell(Body, fc, V(0.075f, 0.1f, 0.06f), white, blend: 0.03f);
        FurTufts(b, Body, fc, V(0.075f, 0.1f, 0.06f), 14, 0.04f, 0.018f, white, 1.1f, -1f, 0.6f);
        PokeBuilder.Both(s => Blade(b, Body, bc + V(0.07f * s, 0.06f, 0.1f), bc + V(0.15f * s, 0.16f, -0.1f), 0.05f, tan, V(s, 0.3f, 0), 0.25f, Shell));
        int tail = b.Tail(bc + V(0, 0.04f, -0.21f));
        b.Tube(tail, Smooth(3, bc + V(0, 0.04f, -0.21f), bc + V(0, 0.06f, -0.27f), bc + V(0, 0.02f, -0.32f)), 0.02f, 0.008f, teal, blend: 0f);
        // A long neck up to a pale head, its great crest swept back
        int head = b.Head(bc + V(0, 0.08f, 0.17f));
        var c = bc + V(0, 0.3f, 0.24f);
        var r = V(0.05f, 0.05f, 0.07f);
        b.Tube(head, Smooth(3, bc + V(0, 0.06f, 0.16f), bc + V(0, 0.2f, 0.2f), c + V(0, -0.02f, -0.03f)), 0.045f, 0.035f, teal, blend: 0f);
        b.Ell(head, c, r, pale);
        b.Ell(head, c + V(0, -0.015f, 0.06f), V(0.03f, 0.028f, 0.04f), pale);
        b.PaintEll(head, c + V(0, 0.01f, 0.03f), V(0.055f, 0.02f, 0.04f), navy);
        var crest = Smooth(3, c + V(0, 0.03f, 0.03f), c + V(0, 0.1f, -0.01f), c + V(0, 0.16f, -0.1f), c + V(0, 0.17f, -0.2f));
        b.Tube(head, crest, 0.032f, 0.01f, tan, Shell, 0f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.03f * s, 0.035f, -0.03f), c + V(0.08f * s, 0.06f, -0.1f), 0.014f, tan, 0.6f, Shell));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.036f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 40, 60), glare: true);
        });
        return b;
    }

    /// <summary>Terrakion: a heavy gray bull of rock, a great brown plate flaring back from its head over a tan face set in a fierce grimace, orange stones at its shoulders and flanks, a tan band across its chest and tan cuffs on its legs.</summary>
    private static PokeBuilder Terrakion()
    {
        var b = new PokeBuilder("Terrakion", 1f, BodyPlan.Quadruped, V(0, 0.42f, 0)) { Coat = Shell };
        var gray = Rgb(150, 150, 156);
        var dark = Rgb(90, 90, 98);
        var brown = Rgb(110, 90, 72);
        var tan = Rgb(206, 186, 146);
        var orange = Rgb(232, 110, 50);
        BeastLegs(b, 0.11f, 0.36f, 0.17f, -0.18f, 0.07f, gray, dark, gray, tan);
        var bc = V(0, 0.42f, 0);
        b.Ell(Body, bc, V(0.15f, 0.14f, 0.24f), gray);
        b.Ell(Body, bc + V(0, 0.04f, 0.15f), V(0.15f, 0.14f, 0.12f), gray, blend: 0.04f);
        b.PaintTorus(Body, bc + V(0, 0.03f, 0.15f), 0.15f, 0.025f, tan, V(0, 0, 25f), 1f, 0.85f);
        PokeBuilder.Both(s =>
        {
            b.Box(Body, bc + V(0.15f * s, 0.07f, 0.12f), V(0.025f, 0.04f, 0.05f), 0.012f, orange, V(0, 0, -10f * s), Shell, 0.01f);
            b.Box(Body, bc + V(0.14f * s, 0.05f, -0.14f), V(0.025f, 0.035f, 0.045f), 0.012f, orange, V(0, 0, -10f * s), Shell, 0.01f);
        });
        int tail = b.Tail(bc + V(0, 0.02f, -0.24f));
        b.Tube(tail, Smooth(3, bc + V(0, 0.02f, -0.24f), bc + V(0, -0.02f, -0.3f), bc + V(0, -0.08f, -0.32f)), 0.02f, 0.01f, gray, blend: 0f);
        // A tan face under a great brown plate that flares back and out
        int head = b.Head(bc + V(0, 0.06f, 0.22f));
        var c = bc + V(0, 0.1f, 0.3f);
        var r = V(0.085f, 0.075f, 0.07f);
        b.Ell(head, c, r, tan);
        b.Ell(head, c + V(0, 0.07f, -0.03f), V(0.11f, 0.04f, 0.1f), brown, blend: 0.015f);
        PokeBuilder.Both(s => Blade(b, head, c + V(0.07f * s, 0.07f, -0.04f), c + V(0.22f * s, 0.12f, -0.2f), 0.07f, brown, V(0.2f * s, 1f, 0), 0.22f, Shell));
        b.Spike(head, c + V(0, 0.09f, 0.03f), c + V(0, 0.16f, 0.06f), 0.025f, brown, 0.7f, Shell);
        Grin(b, head, c + V(0, -0.04f, 0.06f), V(0.04f, 0.012f, 0.02f), Rgb(60, 50, 50));
        b.PaintEll(head, c + V(0, -0.04f, 0.072f), V(0.04f, 0.004f, 0.01f), White, soft: 0.003f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.02f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(232, 110, 40), glare: true);
        });
        return b;
    }

    /// <summary>Virizion: a slender green doe, pale beneath, leaves swept back from its flanks, long thin legs banded pink above dark hooves, and a great green horn curving back from its brow over two pink points.</summary>
    private static PokeBuilder Virizion()
    {
        var b = new PokeBuilder("Virizion", 1f, BodyPlan.Quadruped, V(0, 0.44f, -0.02f)) { Coat = Leaf };
        var green = Rgb(110, 200, 80);
        var light = Rgb(214, 232, 186);
        var pink = Rgb(222, 90, 120);
        var dark = Rgb(60, 100, 50);
        BeastLegs(b, 0.06f, 0.39f, 0.15f, -0.19f, 0.038f, green, dark, green, pink);
        var bc = V(0, 0.44f, -0.02f);
        b.Ell(Body, bc, V(0.08f, 0.08f, 0.2f), green);
        b.PaintEll(Body, bc + V(0, -0.06f, 0), V(0.07f, 0.04f, 0.18f), light);
        PokeBuilder.Both(s =>
        {
            Blade(b, Body, bc + V(0.06f * s, 0.04f, 0.1f), bc + V(0.14f * s, 0.12f, -0.1f), 0.045f, green, V(s, 0.3f, 0), 0.22f, Leaf);
            Blade(b, Body, bc + V(0.06f * s, 0.0f, 0.04f), bc + V(0.13f * s, 0.05f, -0.15f), 0.035f, green, V(s, 0.3f, 0), 0.22f, Leaf);
        });
        int tail = b.Tail(bc + V(0, 0.03f, -0.19f));
        Frond(b, tail, bc + V(0, 0.03f, -0.19f), bc + V(0, 0.0f, -0.3f), 0.03f, green, V(0, 1f, 0.2f), 0.25f, Leaf);
        int head = b.Head(bc + V(0, 0.06f, 0.16f));
        var c = bc + V(0, 0.3f, 0.22f);
        var r = V(0.045f, 0.045f, 0.065f);
        b.Tube(head, Smooth(3, bc + V(0, 0.04f, 0.13f), bc + V(0, 0.18f, 0.19f), c + V(0, -0.02f, -0.03f)), 0.035f, 0.028f, green, blend: 0f);
        b.Ell(head, c, r, green);
        b.PaintEll(head, c + V(0, -0.03f, 0.04f), V(0.04f, 0.02f, 0.04f), light);
        // A great horn curving back from its brow like a blade of grass
        Sickle(b, head, c + V(0, 0.035f, 0.02f), c + V(0, 0.13f, -0.02f), c + V(0, 0.2f, -0.16f), 0.035f, green, V(1f, 0, 0));
        PokeBuilder.Both(s => b.Spike(head, c + V(0.03f * s, 0.03f, -0.02f), c + V(0.07f * s, 0.08f, -0.06f), 0.012f, pink, 0.6f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.032f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(200, 40, 50), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ The forces of nature

    private enum Force { Tornadus, Thundurus, Landorus }

    /// <summary>A cloud the forces of nature ride: puffs of white heaped round <paramref name="at"/>.</summary>
    private static void RideCloud(PokeBuilder b, int bone, Vector3 at, float r)
    {
        var white = Rgb(240, 242, 248);
        b.Ell(bone, at, V(r, r * 0.6f, r * 0.8f), white);
        foreach (var (x, y, z, k) in new[] { (-0.7f, 0.15f, 0.2f, 0.65f), (0.7f, 0.1f, 0.1f, 0.6f), (0f, 0.3f, 0.4f, 0.55f), (-0.3f, 0.35f, -0.4f, 0.6f), (0.4f, 0.3f, -0.3f, 0.5f), (0f, -0.35f, 0f, 0.6f) })
            b.Ell(bone, at + V(x, y, z) * r, V(k, k * 0.8f, k) * r, white, blend: 0.03f);
        // A wisp trailing below
        b.Spike(bone, at + V(0, -r * 0.4f, 0), at + V(r * 0.15f, -r * 1.3f, -r * 0.1f), r * 0.3f, white);
    }

    /// <summary>
    /// Tornadus, Thundurus and Landorus in their Incarnate Formes: a muscular genie riding a cloud, its arms crossed, a
    /// mane of white cloud, fangs and fierce yellow eyes. Tornadus is green spotted purple, horned, its long purple tail
    /// curling round with yellow spirals; Thundurus is blue spotted purple, horned, its tail a chain of black spiked
    /// balls; Landorus is orange spotted pink under a great brow of white cloud, its heavy brown tail arching overhead.
    /// </summary>
    private static PokeBuilder ForceIncarnate(Force force)
    {
        var b = new PokeBuilder(force.ToString(), 1f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var (skin, spot) = force switch
        {
            Force.Tornadus => (Rgb(80, 150, 60), Rgb(140, 90, 180)),
            Force.Thundurus => (Rgb(110, 170, 230), Rgb(110, 80, 170)),
            _ => (Rgb(236, 130, 50), Rgb(230, 140, 160))
        };
        var white = Rgb(240, 242, 248);
        var purple = Rgb(150, 110, 200);
        RideCloud(b, Root, V(0, 0.2f, 0), 0.13f);
        var bc = V(0, 0.42f, 0);
        b.Ell(Body, bc, V(0.12f, 0.12f, 0.09f), skin);
        b.Limb(Body, bc + V(0, -0.08f, -0.01f), V(0, 0.24f, 0), 0.07f, 0.06f, skin);
        foreach (var dir in new[] { V(0.8f, 0.4f, 0.3f), V(-0.8f, 0.5f, 0.2f), V(0.5f, -0.3f, 0.7f), V(-0.4f, -0.4f, 0.7f) })
            b.PaintEll(Body, Out(bc, V(0.12f, 0.12f, 0.09f), default, dir), V(0.025f, 0.02f, 0.025f), spot, soft: 0.006f);
        // Arms crossed in front of it
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.11f * s, 0.06f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = bc + V(0.13f * s, -0.04f, 0.08f);
            var hand = bc + V(-0.07f * s, -0.01f + 0.02f * s, 0.11f);
            b.Ell(arm, shoulder, V(0.055f, 0.05f, 0.05f), skin);
            b.Limb(arm, shoulder, elbow, 0.04f, 0.035f, skin);
            b.Limb(arm, elbow, hand, 0.035f, 0.03f, skin);
            b.Ell(arm, hand, V(0.03f, 0.028f, 0.03f), skin);
            b.PaintEll(arm, shoulder + V(0.02f * s, 0.02f, 0.02f), V(0.025f, 0.02f, 0.025f), spot, soft: 0.006f);
        });
        // The tail
        int tail = b.Tail(V(0, 0.28f, -0.05f));
        switch (force)
        {
            case Force.Tornadus:
            {
                var tp = Smooth(3, V(0, 0.28f, -0.05f), V(-0.18f, 0.24f, -0.06f), V(-0.24f, 0.4f, -0.02f), V(-0.18f, 0.56f, 0), V(-0.08f, 0.6f, 0.02f));
                b.Tube(tail, tp, 0.05f, 0.03f, purple, blend: 0f);
                foreach (int i in new[] { 4, 8 })
                    b.Ell(tail, tp[i] + V(-0.03f, 0, 0.02f), V(0.03f, 0.04f, 0.02f), Rgb(246, 210, 70), mat: Shell, blend: 0.008f);
                Curl(b, tail, tp[^1], tp[^1] + V(0.04f, 0.02f, 0), V(1f, 0, 0), V(0, 1f, 0), 0.04f, 1.5f, 0.022f, purple);
                break;
            }
            case Force.Thundurus:
            {
                var tp = Smooth(3, V(0, 0.28f, -0.05f), V(-0.12f, 0.36f, -0.08f), V(-0.22f, 0.5f, -0.04f), V(-0.3f, 0.58f, 0.02f));
                b.Tube(tail, tp, 0.03f, 0.022f, Rgb(80, 60, 110), blend: 0f);
                foreach (var (i, size) in new[] { (3, 0.035f), (6, 0.045f), (tp.Length - 1, 0.055f) })
                {
                    b.Ell(tail, tp[i], V(size, size, size), Rgb(56, 54, 62), mat: Metal, blend: 0.006f);
                    foreach (var d in new[] { V(0, 1f, 0), V(1f, 0, 0), V(-1f, 0, 0), V(0, 0, 1f), V(0, -1f, 0) })
                        b.Spike(tail, tp[i] + d * size * 0.8f, tp[i] + d * size * 1.6f, size * 0.3f, Rgb(56, 54, 62), mat: Metal, blend: 0.004f);
                }
                break;
            }
            default:
            {
                var tp = Smooth(3, V(0, 0.28f, -0.05f), V(-0.16f, 0.3f, -0.1f), V(-0.24f, 0.5f, -0.1f), V(-0.18f, 0.72f, -0.06f), V(0.0f, 0.78f, -0.04f));
                b.Tube(tail, tp, 0.06f, 0.05f, Rgb(120, 90, 70), blend: 0f);
                for (int i = 1; i < tp.Length; i += 3)
                    b.PaintEll(tail, tp[i] + V(0, 0, 0.045f), V(0.025f, 0.025f, 0.03f), spot, soft: 0.006f);
                break;
            }
        }
        // The head, its mane of cloud, horns and fangs
        int head = b.Head(bc + V(0, 0.1f, 0.02f));
        var c = bc + V(0, 0.18f, 0.04f);
        var r = V(0.06f, 0.06f, 0.06f);
        b.Ell(head, c, r, skin);
        foreach (var (x, y, z, k) in force == Force.Landorus
            ? new[] { (0f, 0.07f, 0.02f, 0.055f), (-0.06f, 0.06f, 0f, 0.045f), (0.06f, 0.06f, 0f, 0.045f), (-0.1f, 0.05f, -0.01f, 0.035f), (0.1f, 0.05f, -0.01f, 0.035f) }
            : new[] { (0f, 0.06f, -0.04f, 0.05f), (-0.045f, 0.04f, -0.05f, 0.045f), (0.045f, 0.04f, -0.05f, 0.045f), (0f, 0.02f, -0.08f, 0.05f), (0f, 0.09f, -0.06f, 0.035f) })
            b.Ell(head, c + V(x, y, z), V(k, k * 0.8f, k), white, blend: 0.02f);
        if (force != Force.Landorus)
            PokeBuilder.Both(s => b.Spike(head, c + V(0.035f * s, 0.04f, 0), c + V(0.07f * s, 0.12f, -0.02f), 0.016f, purple, 0.7f, Shell));
        // A beard of white cloud and two fangs
        b.Ell(head, c + V(0, -0.055f, 0.03f), V(0.045f, 0.03f, 0.03f), white, blend: 0.02f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.02f * s, -0.035f, 0.05f), c + V(0.022f * s, -0.065f, 0.055f), 0.008f, White, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(246, 210, 60), glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Tornadus() => ForceIncarnate(Force.Tornadus);

    private static PokeBuilder Thundurus() => ForceIncarnate(Force.Thundurus);

    private static PokeBuilder Landorus() => ForceIncarnate(Force.Landorus);

    /// <summary>Tornadus in its Therian Forme: a great green bird, purple at its breast and wingtips, a white crest and a long purple plume curling from its head.</summary>
    private static PokeBuilder TornadusTherianBuild()
    {
        var b = new PokeBuilder("Tornadus-Therian", 1f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var green = Rgb(90, 156, 60);
        var purple = Rgb(140, 90, 180);
        var bc = V(0, 0.42f, 0);
        var br = V(0.09f, 0.14f, 0.08f);
        b.Ell(Body, bc, br, green);
        var chest = On(bc, br, 0, bc.Y + 0.03f);
        b.Mark(Body, chest, Outward(bc, br, chest), 0.04f, 0.05f, purple, MarkShape.Diamond);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.07f * s, 0.08f, 0);
            SpreadWing(b, s, shoulder, shoulder + V(0.2f * s, 0.12f, -0.03f), 7, 80f, -20f, 0.2f, 0.035f, green, purple, 0.25f, false, 4);
        });
        int tail = b.Tail(bc + V(0, -0.12f, -0.03f));
        foreach (float x in new[] { -0.04f, 0f, 0.04f })
            Frond(b, tail, bc + V(x * 0.5f, -0.12f, -0.03f), bc + V(x * 1.5f, -0.28f, -0.08f), 0.035f, green, V(0, 0, 1f), 0.25f);
        DanglingLegs(b, 0.05f, bc.Y - 0.1f, 0.02f, 0.08f, 0.016f, purple);
        int head = b.Head(bc + V(0, 0.13f, 0.02f));
        var c = bc + V(0, 0.21f, 0.04f);
        var r = V(0.05f, 0.05f, 0.055f);
        b.Limb(head, bc + V(0, 0.12f, 0.01f), c, 0.035f, 0.03f, green);
        b.Ell(head, c, r, green);
        b.Spike(head, c + V(0, -0.01f, 0.045f), c + V(0, -0.035f, 0.1f), 0.016f, Rgb(240, 220, 120), 0.7f, Shell);
        foreach (float x in new[] { -0.025f, 0f, 0.025f })
            Frond(b, head, c + V(x, 0.03f, -0.02f), c + V(x * 1.6f, 0.09f, -0.06f), 0.02f, White, V(0, 0, 1f), 0.3f);
        var plume = Smooth(3, c + V(0, 0.04f, -0.03f), c + V(0.04f, 0.14f, -0.06f), c + V(0.12f, 0.2f, -0.06f));
        b.Tube(head, plume, 0.014f, 0.02f, purple, blend: 0f);
        Curl(b, head, plume[^1], plume[^1] + V(0.03f, -0.02f, 0), V(1f, 0, 0), V(0, 1f, 0), 0.035f, 1.4f, 0.016f, purple);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(246, 210, 60), glare: true);
        });
        return Lift(b);
    }

    /// <summary>Thundurus in its Therian Forme: a blue beast spotted purple, flying low with great clawed hands, a mane of white cloud with a purple horn, its tail a chain of black spiked balls.</summary>
    private static PokeBuilder ThundurusTherianBuild()
    {
        var b = new PokeBuilder("Thundurus-Therian", 1f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(110, 170, 230);
        var spot = Rgb(110, 80, 170);
        var white = Rgb(240, 242, 248);
        var bc = V(0, 0.42f, 0);
        var br = V(0.09f, 0.14f, 0.08f);
        b.Ell(Body, bc, br, blue, V(-20f, 0, 0));
        foreach (var dir in new[] { V(0.8f, 0.3f, 0.4f), V(-0.8f, 0.1f, 0.5f), V(0.3f, -0.5f, 0.8f), V(-0.5f, 0.6f, -0.5f) })
            b.PaintEll(Body, Out(bc, br, V(-20f, 0, 0), dir), V(0.025f, 0.02f, 0.025f), spot, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.08f * s, 0.08f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var hand = bc + V(0.2f * s, 0.06f, 0.1f);
            b.Limb(arm, shoulder, hand, 0.035f, 0.03f, blue);
            b.Ell(arm, hand, V(0.04f, 0.035f, 0.035f), blue);
            Claws(b, arm, hand + V(0.02f * s, 0, 0.03f), V(0, 0.015f, 0), V(0.4f * s, -0.2f, 1f), 0.03f, 0.009f);
            int leg = b.Leg(s, bc + V(0.05f * s, -0.1f, 0));
            b.Limb(leg, bc + V(0.05f * s, -0.1f, 0), bc + V(0.07f * s, -0.2f, -0.06f), 0.035f, 0.03f, blue);
            b.Ell(leg, bc + V(0.075f * s, -0.22f, -0.05f), V(0.03f, 0.022f, 0.04f), blue);
        });
        int tail = b.Tail(bc + V(0, -0.12f, -0.05f));
        var tp = Smooth(3, bc + V(0, -0.12f, -0.05f), bc + V(-0.08f, -0.2f, -0.15f), bc + V(-0.2f, -0.18f, -0.22f), bc + V(-0.3f, -0.08f, -0.24f));
        b.Tube(tail, tp, 0.03f, 0.02f, Rgb(80, 60, 110), blend: 0f);
        foreach (var (i, size) in new[] { (3, 0.03f), (6, 0.035f), (tp.Length - 1, 0.045f) })
        {
            b.Ell(tail, tp[i], V(size, size, size), Rgb(56, 54, 62), mat: Metal, blend: 0.006f);
            foreach (var d in new[] { V(0, 1f, 0), V(1f, 0, 0), V(-1f, 0, 0), V(0, -1f, 0) })
                b.Spike(tail, tp[i] + d * size * 0.8f, tp[i] + d * size * 1.6f, size * 0.3f, Rgb(56, 54, 62), mat: Metal, blend: 0.004f);
        }
        int head = b.Head(bc + V(0, 0.12f, 0.05f));
        var c = bc + V(0, 0.19f, 0.08f);
        var r = V(0.05f, 0.05f, 0.06f);
        b.Limb(head, bc + V(0, 0.1f, 0.02f), c, 0.035f, 0.03f, blue);
        b.Ell(head, c, r, blue);
        foreach (var (x, y, z, k) in new[] { (0f, 0.05f, -0.05f, 0.05f), (-0.05f, 0.03f, -0.07f, 0.045f), (0.05f, 0.03f, -0.07f, 0.045f), (0f, 0.02f, -0.11f, 0.05f), (0f, -0.04f, -0.06f, 0.04f) })
            b.Ell(head, c + V(x, y, z), V(k, k * 0.8f, k), white, blend: 0.02f);
        b.Spike(head, c + V(0, 0.04f, 0.01f), c + V(0, 0.14f, -0.02f), 0.018f, Rgb(150, 110, 200), 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(246, 210, 60), glare: true);
        });
        return Lift(b);
    }

    /// <summary>Landorus in its Therian Forme: an orange beast like a lion spotted pink, white fur at its paws, a roll of white cloud across its brow over a spiky pink mane, and a heavy brown tail spotted pink arching up behind.</summary>
    private static PokeBuilder LandorusTherianBuild()
    {
        var b = new PokeBuilder("Landorus-Therian", 1f, BodyPlan.Quadruped, V(0, 0.36f, 0)) { Coat = Fur };
        var orange = Rgb(236, 130, 50);
        var pink = Rgb(230, 140, 160);
        var white = Rgb(240, 242, 248);
        BeastLegs(b, 0.09f, 0.3f, 0.15f, -0.16f, 0.06f, orange, white, white);
        var bc = V(0, 0.36f, 0);
        var br = V(0.12f, 0.12f, 0.2f);
        b.Ell(Body, bc, br, orange);
        foreach (var dir in new[] { V(0.8f, 0.4f, 0.2f), V(-0.8f, 0.3f, -0.3f), V(0.4f, 0.7f, -0.6f), V(-0.3f, 0.8f, 0.3f), V(0.7f, 0.2f, -0.7f) })
            b.PaintEll(Body, Out(bc, br, default, dir), V(0.028f, 0.022f, 0.028f), pink, soft: 0.006f);
        int tail = b.Tail(bc + V(0, 0.04f, -0.19f));
        var tp = Smooth(3, bc + V(0, 0.04f, -0.19f), bc + V(0, 0.16f, -0.28f), bc + V(0.02f, 0.32f, -0.24f), bc + V(0.02f, 0.42f, -0.12f));
        b.Tube(tail, tp, 0.04f, 0.045f, Rgb(120, 90, 70), blend: 0f);
        for (int i = 2; i < tp.Length; i += 3)
            b.PaintEll(tail, tp[i] + V(0, 0.03f, 0), V(0.022f, 0.025f, 0.022f), pink, soft: 0.006f);
        int head = b.Head(bc + V(0, 0.06f, 0.17f));
        var c = bc + V(0, 0.13f, 0.25f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Limb(head, bc + V(0, 0.04f, 0.15f), c, 0.06f, 0.05f, orange);
        b.Ell(head, c, r, orange);
        foreach (var d in new[] { V(0.7f, 0.5f, -0.4f), V(-0.7f, 0.5f, -0.4f), V(0.9f, 0f, -0.4f), V(-0.9f, 0f, -0.4f), V(0.5f, -0.4f, -0.6f), V(-0.5f, -0.4f, -0.6f) })
            b.Spike(head, c + Vector3.Normalize(d) * 0.05f, c + Vector3.Normalize(d) * 0.12f, 0.02f, pink, 0.6f);
        // A roll of white cloud across its brow, its ends cut square
        b.Limb(head, c + V(-0.12f, 0.06f, 0.0f), c + V(0.12f, 0.06f, 0.0f), 0.04f, 0.04f, white);
        b.Ell(head, c + V(0, -0.03f, 0.06f), V(0.04f, 0.03f, 0.03f), white, blend: 0.015f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.025f * s, -0.04f, 0.075f), c + V(0.028f * s, -0.075f, 0.08f), 0.009f, White, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(246, 210, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Reshiram and Zekrom

    /// <summary>A tail with a turbine: a great ring round its middle, seen on Reshiram and the White Kyurem.</summary>
    private static void TurbineTail(PokeBuilder b, int bone, Vector3[] path, int at, float r, Color body, Color ring, Color core)
    {
        var d = Vector3.Normalize(path[at + 1] - path[at - 1]);
        b.Limb(bone, path[at] - d * r * 0.7f, path[at] + d * r * 0.7f, r, r, body);
        foreach (float t in new[] { -0.6f, 0f, 0.6f })
            b.Torus(bone, path[at] + d * r * t, r * 1.02f, r * 0.12f, ring, Euler(d), mat: Shell, blend: 0.004f);
        b.PaintEll(bone, path[at] + d * r * 0.7f, V(r * 0.6f, r * 0.2f, r * 0.6f), core, Euler(d));
    }

    /// <summary>
    /// Reshiram: a white dragon of fire, tall on digitigrade legs, great white wings fringed like feathers, a long neck
    /// to a narrow head with a mane flowing back and blue eyes, and a heavy tail round a turbine.
    /// </summary>
    private static PokeBuilder Reshiram()
    {
        var b = new PokeBuilder("Reshiram", 1f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var white = Rgb(242, 242, 246);
        var gray = Rgb(200, 204, 214);
        var blue = Rgb(80, 150, 220);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.3f, -0.02f));
            var knee = V(0.09f * s, 0.18f, 0.06f);
            var ankle = V(0.08f * s, 0.06f, -0.02f);
            b.Limb(leg, V(0.06f * s, 0.31f, -0.02f), knee, 0.05f, 0.035f, white);
            b.Limb(leg, knee, ankle, 0.03f, 0.024f, white);
            b.Ell(leg, ankle + V(0, -0.03f, 0.04f), V(0.028f, 0.025f, 0.06f), white);
            Claws(b, leg, ankle + V(0, -0.04f, 0.1f), V(0.014f, 0, 0), V(0, -0.2f, 1f), 0.02f, 0.008f);
            FurTufts(b, leg, knee + V(0, 0.04f, -0.02f), V(0.05f, 0.05f, 0.05f), 6, 0.03f, 0.014f, white, 1.1f, -1f, 0.4f);
            var shoulder = V(0.07f * s, 0.55f, 0.03f);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.12f * s, 0.46f, 0.1f);
            b.Limb(arm, shoulder, hand, 0.025f, 0.02f, white);
            Claws(b, arm, hand + V(0, -0.01f, 0.01f), V(0, 0.008f, 0.006f), V(0.3f * s, -0.4f, 1f), 0.02f, 0.006f);
            // A great wing fringed with feathers
            var wshoulder = V(0.05f * s, 0.56f, -0.02f);
            SpreadWing(b, s, wshoulder, wshoulder + V(0.26f * s, 0.12f, -0.1f), 8, 55f, -45f, 0.22f, 0.04f, white, gray, 0.45f, true, 6);
        });
        b.Ell(Body, V(0, 0.45f, 0), V(0.085f, 0.15f, 0.08f), white);
        b.Ell(Body, V(0, 0.54f, 0.02f), V(0.09f, 0.07f, 0.075f), white, blend: 0.03f);
        int tail = b.Tail(V(0, 0.34f, -0.06f));
        var tp = Smooth(3, V(0, 0.34f, -0.06f), V(0, 0.24f, -0.18f), V(0, 0.18f, -0.32f), V(0, 0.2f, -0.46f));
        b.Tube(tail, tp, 0.055f, 0.02f, white, blend: 0f);
        TurbineTail(b, tail, tp, tp.Length / 2, 0.075f, white, gray, Rgb(120, 160, 220));
        foreach (float a in new[] { -40f, 0f, 40f })
            Frond(b, tail, tp[^1], tp[^1] + V(MathF.Sin(a * Degree) * 0.08f, 0.04f, -0.08f), 0.035f, white, V(0, 1f, 0.3f), 0.22f);
        // A long neck to a narrow head, a mane flowing back
        int head = b.Head(V(0, 0.62f, 0.04f));
        var c = V(0, 0.84f, 0.1f);
        var r = V(0.055f, 0.055f, 0.08f);
        b.Tube(head, Smooth(3, V(0, 0.6f, 0.03f), V(0, 0.72f, 0.06f), c + V(0, -0.03f, -0.03f)), 0.04f, 0.03f, white, blend: 0f);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.012f, 0.06f), V(0.028f, 0.025f, 0.04f), white);
        foreach (var (x, y, len) in new[] { (0f, 0.04f, 0.2f), (-0.03f, 0.02f, 0.16f), (0.03f, 0.02f, 0.16f), (0f, 0f, 0.14f) })
            Frond(b, head, c + V(x, y, -0.03f), c + V(x * 2f, y + 0.02f, -0.03f - len), 0.035f, white, V(0, 1f, 0.2f), 0.22f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.028f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, blue, glare: true);
        });
        return b;
    }

    /// <summary>
    /// Zekrom: a black dragon of lightning, broad and muscular, its chest lined with gray, great angular wings, a crest
    /// swept back from its head, red eyes, and a tail like a generator with blue light at its end.
    /// </summary>
    private static PokeBuilder Zekrom()
    {
        var b = new PokeBuilder("Zekrom", 1f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Scales };
        var black = Rgb(44, 44, 52);
        var gray = Rgb(90, 92, 104);
        var blue = Rgb(70, 170, 240);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.06f * s, 0.56f, -0.04f));
            DragonWing(b, wing, V(0.06f * s, 0.56f, -0.04f), V(0.24f * s, 0.74f, -0.14f),
                new[] { V(0.36f * s, 0.6f, -0.16f), V(0.32f * s, 0.48f, -0.16f), V(0.22f * s, 0.44f, -0.14f) },
                V(0.1f * s, 0.48f, -0.1f), gray, black, 0.018f);
        });
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.3f, 0));
            var knee = V(0.12f * s, 0.18f, 0.06f);
            var ankle = V(0.11f * s, 0.06f, -0.01f);
            b.Limb(leg, V(0.08f * s, 0.31f, 0), knee, 0.065f, 0.045f, black);
            b.Limb(leg, knee, ankle, 0.04f, 0.032f, black);
            b.Ell(leg, ankle + V(0, -0.03f, 0.04f), V(0.035f, 0.028f, 0.06f), black);
            Claws(b, leg, ankle + V(0, -0.04f, 0.1f), V(0.016f, 0, 0), V(0, -0.2f, 1f), 0.022f, 0.009f);
            var shoulder = V(0.11f * s, 0.56f, 0.03f);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.18f * s, 0.46f, 0.06f);
            var hand = V(0.18f * s, 0.36f, 0.12f);
            b.Limb(arm, shoulder, elbow, 0.05f, 0.04f, black);
            b.Limb(arm, elbow, hand, 0.04f, 0.035f, black);
            b.Ell(arm, hand, V(0.035f, 0.035f, 0.035f), black);
            Claws(b, arm, hand + V(0, -0.02f, 0.03f), V(0.014f, 0, 0), V(0, -0.5f, 1f), 0.03f, 0.009f);
        });
        b.Ell(Body, V(0, 0.44f, 0), V(0.11f, 0.16f, 0.09f), black);
        b.Ell(Body, V(0, 0.54f, 0.02f), V(0.12f, 0.08f, 0.085f), black, blend: 0.03f);
        foreach (float y in new[] { 0.5f, 0.44f, 0.38f })
            b.PaintEll(Body, V(0, y, 0.09f), V(0.06f - (0.5f - y) * 0.2f, 0.008f, 0.02f), gray, soft: 0.004f);
        b.PaintEll(Body, V(0, 0.58f, 0.08f), V(0.03f, 0.03f, 0.02f), blue);
        // A thick tail ending in a generator, blue light at its end
        int tail = b.Tail(V(0, 0.32f, -0.06f));
        var tp = Smooth(3, V(0, 0.32f, -0.06f), V(0, 0.22f, -0.18f), V(0, 0.16f, -0.3f));
        b.Tube(tail, tp, 0.07f, 0.06f, black, blend: 0f);
        var gen = tp[^1];
        var gd = Vector3.Normalize(tp[^1] - tp[^2]);
        b.Limb(tail, gen, gen + gd * 0.1f, 0.09f, 0.09f, black);
        b.Torus(tail, gen + gd * 0.1f, 0.075f, 0.016f, blue, Euler(gd), mat: Glow, blend: 0.004f);
        b.Ell(tail, gen + gd * 0.11f, V(0.06f, 0.02f, 0.06f), Rgb(60, 70, 90), Euler(gd), blend: 0.006f);
        // A head on a short neck, a crest swept back
        int head = b.Head(V(0, 0.6f, 0.04f));
        var c = V(0, 0.72f, 0.08f);
        var r = V(0.055f, 0.055f, 0.07f);
        b.Limb(head, V(0, 0.6f, 0.03f), c + V(0, -0.03f, -0.02f), 0.05f, 0.04f, black);
        b.Ell(head, c, r, black);
        b.Ell(head, c + V(0, -0.02f, 0.06f), V(0.035f, 0.03f, 0.04f), gray);
        b.Spike(head, c + V(0, 0.03f, -0.02f), c + V(0, 0.08f, -0.24f), 0.035f, black, 0.5f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.04f * s, 0.01f, -0.03f), c + V(0.09f * s, 0.02f, -0.1f), 0.014f, black, 0.6f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(220, 40, 50), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Kyurem

    private enum Fusion { None, White, Black }

    /// <summary>
    /// Kyurem and its fusions. Kyurem is a gray dragon on four legs, its wings and back jagged with pale ice, its jaw
    /// frozen over and its eyes yellow. The White Kyurem stands upright in white plating with Reshiram's feathered wings
    /// and turbine tail, all frosted; the Black Kyurem in black plating with Zekrom's wings and generator tail.
    /// </summary>
    private static PokeBuilder KyuremBuild(Fusion fusion)
    {
        bool fused = fusion != Fusion.None;
        var name = fusion switch { Fusion.White => "Kyurem-White", Fusion.Black => "Kyurem-Black", _ => "Kyurem" };
        var b = new PokeBuilder(name, 1f, fused ? BodyPlan.Biped : BodyPlan.Quadruped, fused ? V(0, 0.44f, 0) : V(0, 0.36f, -0.02f)) { Coat = Shell };
        var gray = Rgb(110, 116, 128);
        var ice = Rgb(190, 222, 242);
        var plate = fusion == Fusion.White ? Rgb(232, 234, 240) : fusion == Fusion.Black ? Rgb(48, 48, 58) : gray;
        var yellow = Rgb(246, 210, 60);
        int head;
        Vector3 c;
        var r = V(0.06f, 0.055f, 0.08f);
        if (!fused)
        {
            BeastLegs(b, 0.1f, 0.32f, 0.16f, -0.18f, 0.06f, gray, gray, gray);
            var bc = V(0, 0.36f, -0.02f);
            b.Ell(Body, bc, V(0.13f, 0.12f, 0.24f), gray);
            b.Ell(Body, bc + V(0, 0.04f, 0.14f), V(0.13f, 0.12f, 0.11f), gray, blend: 0.04f);
            // Wings of jagged ice, and ice along its back
            PokeBuilder.Both(s =>
            {
                int wing = b.Wing(s, bc + V(0.07f * s, 0.08f, 0.04f));
                foreach (var (to, w) in new[] { (V(0.26f * s, 0.32f, -0.04f), 0.06f), (V(0.24f * s, 0.22f, -0.14f), 0.05f), (V(0.18f * s, 0.18f, -0.22f), 0.045f) })
                    Blade(b, wing, bc + V(0.07f * s, 0.08f, 0.04f), bc + to, w, ice, V(0, 0, 1f), 0.25f, Shell);
            });
            foreach (var (z, h) in new[] { (0.08f, 0.1f), (-0.04f, 0.09f), (-0.15f, 0.08f) })
                b.Spike(Body, bc + V(0, 0.1f, z), bc + V(0, 0.1f + h, z - 0.04f), 0.035f, ice, 0.5f, Shell);
            int tail = b.Tail(bc + V(0, 0.02f, -0.23f));
            var tp = Smooth(3, bc + V(0, 0.02f, -0.23f), bc + V(0, -0.06f, -0.34f), bc + V(0, -0.14f, -0.46f));
            b.Tube(tail, tp, 0.045f, 0.015f, gray, blend: 0f);
            b.Spike(tail, tp[^1], tp[^1] + V(0, 0.02f, -0.1f), 0.03f, ice, 0.6f, Shell);
            head = b.Head(bc + V(0, 0.08f, 0.22f));
            c = bc + V(0, 0.16f, 0.3f);
            b.Limb(head, bc + V(0, 0.06f, 0.18f), c, 0.05f, 0.045f, gray);
        }
        else
        {
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s, 0.3f, 0));
                var knee = V(0.11f * s, 0.18f, 0.06f);
                var ankle = V(0.1f * s, 0.06f, -0.01f);
                b.Limb(leg, V(0.07f * s, 0.31f, 0), knee, 0.06f, 0.04f, plate);
                b.Limb(leg, knee, ankle, 0.036f, 0.03f, gray);
                b.Ell(leg, ankle + V(0, -0.03f, 0.04f), V(0.035f, 0.028f, 0.06f), gray);
                Claws(b, leg, ankle + V(0, -0.04f, 0.1f), V(0.016f, 0, 0), V(0, -0.2f, 1f), 0.022f, 0.009f);
                b.Spike(leg, knee + V(0, 0.02f, 0.02f), knee + V(0.02f * s, 0.08f, 0.04f), 0.02f, ice, 0.6f, Shell);
                var shoulder = V(0.11f * s, 0.56f, 0.03f);
                int arm = b.Arm(s, shoulder);
                var hand = V(0.17f * s, 0.4f, 0.12f);
                b.Limb(arm, shoulder, hand, 0.045f, 0.035f, plate);
                b.Ell(arm, hand, V(0.035f, 0.035f, 0.035f), gray);
                Claws(b, arm, hand + V(0, -0.02f, 0.03f), V(0.014f, 0, 0), V(0, -0.5f, 1f), 0.03f, 0.009f);
            });
            PokeBuilder.Both(s =>
            {
                var wshoulder = V(0.06f * s, 0.56f, -0.03f);
                if (fusion == Fusion.White)
                    SpreadWing(b, s, wshoulder, wshoulder + V(0.26f * s, 0.12f, -0.1f), 7, 55f, -45f, 0.2f, 0.04f, plate, ice, 0.45f, true, 4);
                else
                {
                    int wing = b.Wing(s, wshoulder);
                    DragonWing(b, wing, wshoulder, wshoulder + V(0.18f * s, 0.16f, -0.06f),
                        new[] { V(0.32f * s, 0.62f, -0.16f), V(0.28f * s, 0.5f, -0.16f), V(0.18f * s, 0.46f, -0.14f) },
                        V(0.08f * s, 0.48f, -0.1f), gray, plate, 0.018f);
                    Blade(b, wing, wshoulder + V(0.18f * s, 0.16f, -0.06f), wshoulder + V(0.22f * s, 0.3f, -0.08f), 0.05f, ice, V(0, 0, 1f), 0.25f, Shell);
                }
            });
            b.Ell(Body, V(0, 0.45f, 0), V(0.11f, 0.15f, 0.09f), plate);
            b.Ell(Body, V(0, 0.55f, 0.02f), V(0.12f, 0.07f, 0.085f), plate, blend: 0.03f);
            b.PaintEll(Body, V(0, 0.42f, 0.08f), V(0.07f, 0.1f, 0.03f), gray);
            foreach (var (x, y) in new[] { (0.09f, 0.6f), (-0.09f, 0.6f), (0f, 0.62f) })
                b.Spike(Body, V(x, y, -0.04f), V(x * 1.3f, y + 0.08f, -0.08f), 0.03f, ice, 0.5f, Shell);
            int tail = b.Tail(V(0, 0.32f, -0.06f));
            var tp = Smooth(3, V(0, 0.32f, -0.06f), V(0, 0.22f, -0.18f), V(0, 0.16f, -0.32f), V(0, 0.16f, -0.44f));
            b.Tube(tail, tp, 0.06f, 0.03f, plate, blend: 0f);
            if (fusion == Fusion.White) TurbineTail(b, tail, tp, tp.Length / 2, 0.075f, plate, ice, Rgb(150, 200, 240));
            else
            {
                var gd = Vector3.Normalize(tp[^1] - tp[^2]);
                b.Limb(tail, tp[^1], tp[^1] + gd * 0.08f, 0.08f, 0.08f, plate);
                b.Torus(tail, tp[^1] + gd * 0.08f, 0.065f, 0.014f, Rgb(70, 170, 240), Euler(gd), mat: Glow, blend: 0.004f);
            }
            head = b.Head(V(0, 0.6f, 0.04f));
            c = V(0, 0.72f, 0.08f);
            b.Limb(head, V(0, 0.6f, 0.03f), c + V(0, -0.03f, -0.02f), 0.045f, 0.04f, plate);
        }
        // A gray head, its jaw frozen over in pale ice, spikes of ice behind it
        b.Ell(head, c, r, gray);
        b.Ell(head, c + V(0, -0.025f, 0.06f), V(0.045f, 0.03f, 0.05f), ice, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.035f * s, 0.03f, -0.03f), c + V(0.08f * s, 0.1f, -0.12f), 0.022f, fusion == Fusion.Black ? plate : ice, 0.6f, Shell);
            if (fusion == Fusion.White) b.Spike(head, c + V(0.02f * s, 0.04f, 0), c + V(0.03f * s, 0.12f, -0.02f), 0.012f, yellow, 0.6f, Shell);
            var at = On(c, r, 0.035f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, yellow, glare: true);
        });
        return b;
    }

    private static PokeBuilder Kyurem() => KyuremBuild(Fusion.None);

    // ------------------------------------------------------------------ Keldeo

    /// <summary>
    /// Keldeo and its Resolute Form: a cream colt with a bushy red mane tied back with a navy band, a flowing blue tail,
    /// blue tufts at its knees and navy hooves. A little cream horn grows from its brow; resolute, the horn becomes a
    /// long blue sword and its mane is streaked with yellow, green and blue.
    /// </summary>
    private static PokeBuilder KeldeoBuild(bool resolute)
    {
        var b = new PokeBuilder(resolute ? "Keldeo-Resolute" : "Keldeo", 0.85f, BodyPlan.Quadruped, V(0, 0.26f, -0.01f)) { Coat = Fur };
        var cream = Rgb(246, 236, 190);
        var red = Rgb(232, 80, 50);
        var blue = Rgb(110, 180, 236);
        var navy = Rgb(40, 60, 120);
        // Slim legs with blue tufts at the knees and navy hooves
        foreach (var (z, front) in new[] { (0.11f, true), (-0.13f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.22f, z), front);
                var knee = V(0.052f * s, 0.115f, z + (front ? 0.01f : -0.02f));
                var ankle = V(0.052f * s, 0.04f, z + 0.005f);
                b.Limb(leg, V(0.05f * s, 0.23f, z), knee, 0.035f, 0.025f, cream);
                b.Limb(leg, knee, ankle, 0.022f, 0.018f, cream);
                b.Ell(leg, knee + V(0, -0.01f, 0.005f), V(0.03f, 0.026f, 0.03f), blue, blend: 0.012f);
                b.Ell(leg, V(0.052f * s, 0.022f, z + 0.01f), V(0.022f, 0.022f, 0.026f), navy);
            });
        var bc = V(0, 0.26f, -0.01f);
        b.Ell(Body, bc, V(0.075f, 0.072f, 0.14f), cream);
        int tail = b.Tail(bc + V(0, 0.02f, -0.13f));
        var tp = Smooth(3, bc + V(0, 0.02f, -0.13f), bc + V(0, 0.06f, -0.22f), bc + V(0.02f, 0.02f, -0.3f), bc + V(0.03f, 0.08f, -0.38f));
        Ribbon(b, tail, tp, 0.05f, blue, V(0.4f, 1f, 0), 0.012f);
        int head = b.Head(bc + V(0, 0.06f, 0.11f));
        var c = bc + V(0, 0.14f, 0.15f);
        var r = V(0.052f, 0.05f, 0.06f);
        b.Tube(head, Smooth(3, bc + V(0, 0.04f, 0.1f), bc + V(0, 0.1f, 0.13f), c + V(0, -0.02f, -0.02f)), 0.04f, 0.035f, cream, blend: 0f);
        b.Ell(head, c, r, cream);
        b.Ell(head, c + V(0, -0.015f, 0.045f), V(0.03f, 0.028f, 0.03f), cream);
        // A bushy red mane tied back with a navy band
        var mc = c + V(0, 0.04f, -0.04f);
        var mr = V(0.065f, 0.05f, 0.075f);
        b.Ell(head, mc, mr, red);
        FurTufts(b, head, mc, mr, 18, 0.04f, 0.02f, red, 0.8f, -0.8f, 0.5f);
        b.Torus(head, mc + V(0, -0.01f, -0.06f), 0.035f, 0.01f, navy, V(70f, 0, 0), blend: 0.004f);
        Ribbon(b, head, Smooth(3, mc + V(0, -0.01f, -0.07f), mc + V(0.03f, -0.03f, -0.13f), mc + V(0.05f, -0.01f, -0.19f)), 0.025f, navy, V(0, 1f, 0.2f), 0.008f);
        if (resolute)
        {
            foreach (var (dx, color) in new[] { (-0.03f, Rgb(246, 220, 80)), (0f, Rgb(120, 200, 110)), (0.03f, Rgb(110, 180, 236)) })
                Blade(b, head, mc + V(dx, 0.04f, 0.02f), mc + V(dx * 2f, 0.12f, -0.04f), 0.02f, color, V(0, 0, 1f), 0.3f);
            Blade(b, head, c + V(0, 0.04f, 0.03f), c + V(0.02f, 0.22f, -0.04f), 0.026f, Rgb(80, 120, 220), V(1f, 0, 0), 0.45f, Glow);
        }
        else b.Spike(head, c + V(0, 0.035f, 0.035f), c + V(0, 0.07f, 0.06f), 0.012f, Rgb(244, 220, 120), 0.8f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(40, 40, 60), glare: resolute);
        });
        return b;
    }

    private static PokeBuilder Keldeo() => KeldeoBuild(false);

    // ------------------------------------------------------------------ Meloetta

    /// <summary>
    /// Meloetta and its Pirouette Forme: a small slender singer, white with a black curl like a clef on its head. As
    /// Aria it has teal eyes and long green hair falling to the ground lined like a stave; as Pirouette red eyes, its
    /// hair wound into an orange turban, a dark skirt of petals at its waist.
    /// </summary>
    private static PokeBuilder MeloettaBuild(bool pirouette)
    {
        var b = new PokeBuilder(pirouette ? "Meloetta-Pirouette" : "Meloetta", 0.6f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var white = Rgb(244, 244, 248);
        var gray = Rgb(110, 108, 118);
        var black = Rgb(50, 48, 56);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.02f * s, 0.15f, 0));
            b.Limb(leg, V(0.02f * s, 0.16f, 0), V(0.03f * s, 0.02f, 0.01f), 0.012f, 0.008f, white);
            b.Spike(leg, V(0.03f * s, 0.025f, 0.01f), V(0.032f * s, 0f, 0.025f), 0.009f, white);
            var shoulder = V(0.03f * s, 0.26f, 0);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder, V(0.08f * s, 0.2f, 0.03f), 0.01f, 0.008f, white);
            b.Ell(arm, V(0.082f * s, 0.196f, 0.033f), V(0.01f, 0.01f, 0.01f), white);
        });
        b.Ell(Body, V(0, 0.21f, 0), V(0.035f, 0.06f, 0.03f), gray);
        if (pirouette)
            foreach (float a in new[] { 0f, 60f, 120f, 180f, 240f, 300f })
            {
                var d = V(MathF.Sin(a * Degree), 0, MathF.Cos(a * Degree));
                Petal(b, Body, V(0, 0.17f, 0) + d * 0.02f, V(0, 0.15f, 0) + d * 0.08f, 0.03f, black);
            }
        int head = b.Head(V(0, 0.26f, 0.005f));
        var c = V(0, 0.32f, 0.01f);
        var r = V(0.05f, 0.05f, 0.045f);
        b.Ell(head, c, r, white);
        // The clef curling on its head
        b.Tube(head, Smooth(3, c + V(0, 0.04f, 0), c + V(0.015f, 0.08f, -0.005f), c + V(0.035f, 0.09f, -0.005f)), 0.008f, 0.008f, black, blend: 0f);
        Curl(b, head, c + V(0.035f, 0.09f, -0.005f), c + V(0.035f, 0.07f, -0.005f), V(1f, 0, 0), V(0, 1f, 0), 0.02f, 1.2f, 0.007f, black);
        if (pirouette)
        {
            var orange = Rgb(236, 110, 60);
            var stripe = Rgb(250, 160, 100);
            for (int i = 0; i < 4; i++)
            {
                var at = c + V(0, 0.02f + i * 0.022f, -0.02f);
                b.Torus(head, at, 0.05f - i * 0.007f, 0.014f, i % 2 == 0 ? orange : stripe, V(-10f, 0, 0), blend: 0.008f);
            }
            b.Ell(head, c + V(0, 0.06f, -0.02f), V(0.04f, 0.04f, 0.04f), orange, blend: 0.01f);
        }
        else
        {
            // Long green hair falling to the ground, lined like a stave
            var green = Rgb(150, 210, 140);
            var hair = Smooth(3, c + V(0.02f, 0.03f, -0.03f), c + V(0.06f, -0.02f, -0.05f), V(0.07f, 0.14f, -0.04f), V(0.06f, 0.02f, -0.02f));
            b.Tube(head, hair, 0.035f, 0.03f, green, blend: 0f);
            b.Ell(head, c + V(0, 0.02f, -0.02f), V(0.05f, 0.04f, 0.04f), green, blend: 0.01f);
            for (int i = 2; i < hair.Length - 1; i += 2)
                b.PaintTorus(head, hair[i], 0.034f, 0.004f, Rgb(80, 150, 100), Euler(hair[i + 1] - hair[i]));
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, c.Y - 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, pirouette ? Rgb(210, 50, 60) : Rgb(70, 190, 200), sclera: true);
        });
        return b;
    }

    private static PokeBuilder Meloetta() => MeloettaBuild(false);

    // ------------------------------------------------------------------ Genesect

    /// <summary>
    /// Genesect and its drives: a purple insect of war on long legs, a narrow head with a red visor for eyes, thin arms
    /// tipped with claws, and a great cannon on its back pointing over its head, its drive's colour showing in the slot
    /// on top: blue for water, yellow for lightning, red for fire, pale blue for ice.
    /// </summary>
    private static PokeBuilder GenesectBuild(string? drive)
    {
        var b = new PokeBuilder(drive == null ? "Genesect" : "Genesect-" + drive, 0.95f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Metal };
        var purple = Rgb(150, 100, 190);
        var dark = Rgb(96, 70, 130);
        var pink = Rgb(220, 140, 190);
        var slot = drive switch
        {
            "Douse" => Rgb(70, 130, 230),
            "Shock" => Rgb(250, 210, 60),
            "Burn" => Rgb(230, 60, 50),
            "Chill" => Rgb(214, 236, 250),
            _ => Rgb(110, 96, 130)
        };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.26f, 0));
            var knee = V(0.1f * s, 0.16f, 0.07f);
            var ankle = V(0.09f * s, 0.05f, -0.02f);
            b.Limb(leg, V(0.06f * s, 0.27f, 0), knee, 0.04f, 0.03f, purple);
            b.Limb(leg, knee, ankle, 0.026f, 0.02f, dark);
            b.Spike(leg, ankle, ankle + V(0, -0.05f, 0.09f), 0.022f, purple, 0.6f, Metal);
            b.Spike(leg, ankle, ankle + V(0, -0.05f, -0.04f), 0.016f, purple, 0.6f, Metal);
            var shoulder = V(0.08f * s, 0.46f, 0.03f);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.13f * s, 0.38f, 0.06f);
            var hand = V(0.15f * s, 0.3f, 0.12f);
            b.Limb(arm, shoulder, elbow, 0.022f, 0.018f, purple);
            b.Limb(arm, elbow, hand, 0.018f, 0.015f, dark);
            b.Spike(arm, hand, hand + V(0.01f * s, -0.04f, 0.04f), 0.014f, purple, 0.6f, Metal);
        });
        b.Ell(Body, V(0, 0.38f, 0), V(0.075f, 0.12f, 0.065f), purple);
        foreach (float y in new[] { 0.32f, 0.36f })
            b.PaintTorus(Body, V(0, y, 0), 0.07f, 0.007f, pink, sz: 0.9f);
        // The cannon on its back, pointing forward over its head, the drive in its slot
        var breech = V(0, 0.58f, -0.12f);
        var muzzle = V(0, 0.68f, 0.1f);
        b.Limb(Body, V(0, 0.44f, -0.05f), breech, 0.04f, 0.04f, dark);
        b.Limb(Body, breech, muzzle, 0.05f, 0.04f, purple);
        b.Cut(Body, muzzle + V(0, 0.005f, 0.02f), V(0.03f, 0.03f, 0.03f));
        b.PaintEll(Body, muzzle, V(0.035f, 0.035f, 0.03f), Rgb(40, 34, 50), Euler(muzzle - breech));
        b.Box(Body, Vector3.Lerp(breech, muzzle, 0.35f) + V(0, 0.05f, 0), V(0.022f, 0.012f, 0.04f), 0.006f, slot, V(-20f, 0, 0), drive == "Chill" ? Shell : Glow, 0.004f);
        // A narrow head, a red visor across its face
        int head = b.Head(V(0, 0.48f, 0.03f));
        var c = V(0, 0.54f, 0.06f);
        var r = V(0.045f, 0.04f, 0.07f);
        b.Limb(head, V(0, 0.48f, 0.02f), c, 0.03f, 0.03f, purple);
        b.Ell(head, c, r, purple);
        b.Ell(head, c + V(0, -0.012f, 0.06f), V(0.03f, 0.02f, 0.04f), purple);
        b.PaintEll(head, c + V(0, 0.005f, 0.05f), V(0.05f, 0.016f, 0.04f), Rgb(200, 60, 80));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(250, 90, 100), glare: true);
        });
        return b;
    }

    private static PokeBuilder Genesect() => GenesectBuild(null);
}
