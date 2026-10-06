using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Hoenn's third batch in National Pokédex
// order: Castform (351) to Deoxys (386), but for the species of the Sinnoh Pokédex in that range, hand-built before.
// Their forms are in PokemonModels.Forms.cs (Castform's weathers, Deoxys's formes) and PokemonModels.Megas.cs (the
// Megas and the Primal Reversions). Helpers shared with the earlier batches are in PokemonModels.Sinnoh1.cs to
// PokemonModels.Sinnoh4.cs, PokemonModels.Kanto1.cs to PokemonModels.Kanto3.cs, PokemonModels.Johto1.cs,
// PokemonModels.Johto2.cs, PokemonModels.Hoenn1.cs and PokemonModels.Hoenn2.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Castform

    /// <summary>
    /// Castform in its four weathers: a small grey cloud of a face with a wisp curling over its head and a cloud
    /// below (0); a red sun in a ring of orange balls over a white cloud (1); a blue raindrop over grey drops (2); a
    /// purple face in mint clouds under a ring of ice (3).
    /// </summary>
    private static PokeBuilder CastformBuild(int weather)
    {
        string name = weather switch { 1 => "Castform-Sunny", 2 => "Castform-Rainy", 3 => "Castform-Snowy", _ => "Castform" };
        var b = new PokeBuilder(name, 0.5f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Fur }.Hover();
        var face = weather switch { 1 => Rgb(222, 70, 50), 2 => Rgb(90, 160, 220), 3 => Rgb(150, 120, 190), _ => Rgb(232, 232, 236) };
        var mask = weather switch { 1 => Rgb(246, 222, 110), 2 => Rgb(170, 216, 240), 3 => Rgb(200, 190, 232), _ => Rgb(150, 150, 158) };
        var cloud = weather switch { 1 => Rgb(244, 244, 248), 2 => Rgb(170, 172, 180), 3 => Rgb(190, 228, 214), _ => Rgb(214, 214, 220) };
        var c = V(0, 0.32f, 0);
        var r = V(0.1f, 0.1f, 0.095f);
        // The cloud below: three lumps and a wisp trailing behind
        int tail = b.Tail(V(0, 0.22f, 0));
        foreach (var (x, y, rr) in new[] { (-0.06f, 0.2f, 0.06f), (0.06f, 0.2f, 0.06f), (0f, 0.17f, 0.07f) })
            b.Ell(tail, V(x, y, 0.0f), V(rr, rr * 0.85f, rr), cloud, blend: 0.02f);
        if (weather != 2) Blade(b, tail, V(0.04f, 0.18f, -0.04f), V(0.16f, 0.14f, -0.1f), 0.04f, cloud, V(0, 1f, 0), 0.35f);
        b.Ell(Body, c, r, face);
        // The mask round its eyes and over its lip, the eyes big and round
        b.PaintEll(Body, c + V(0, 0.005f, 0.07f), V(0.08f, 0.04f, 0.04f), mask);
        if (weather == 0) b.PaintEll(Body, c + V(0, -0.05f, 0.075f), V(0.03f, 0.025f, 0.03f), mask);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.33f);
            b.Eye(Body, at, Outward(c, r, at), 0.02f, sclera: true, pupil: Rgb(30, 30, 40));
        });
        b.Mark(Body, On(c, r, 0, 0.27f), V(0, -0.4f, 1f), 0.018f, 0.012f, Rgb(200, 90, 110), MarkShape.Smile);
        int head = b.Head(c + V(0, 0.08f, 0));
        switch (weather)
        {
            case 0:
                Curl(b, head, c + V(0, 0.07f, -0.02f), c + V(0.0f, 0.14f, -0.01f), V(0, 0, 1f), V(0, 1f, 0), 0.035f, 0.7f, 0.03f, face);
                break;
            case 1:
                // A ring of orange balls round its face, in the face's own plane
                for (int i = 0; i < 9; i++)
                {
                    float a = MathF.Tau * i / 9f + 0.35f;
                    b.Ell(head, c + V(MathF.Sin(a) * 0.13f, MathF.Cos(a) * 0.13f, -0.02f), V(0.045f, 0.045f, 0.045f), Rgb(246, 154, 64), blend: 0.015f);
                }
                break;
            case 2:
                b.Spike(head, c + V(0, 0.05f, 0), c + V(0, 0.22f, -0.01f), 0.075f, face, blend: 0.03f);
                break;
            default:
                // Mint puffs round its head, and a ring of ice resting on the two highest
                foreach (var (x, y, rr) in new[] { (0.12f, 0.03f, 0.05f), (-0.12f, 0.03f, 0.05f), (0.09f, -0.06f, 0.045f), (-0.09f, -0.06f, 0.045f), (0f, 0.1f, 0.045f) })
                    b.Ell(head, c + V(x, y, -0.01f), V(rr, rr, rr), cloud, blend: 0.02f);
                b.Torus(head, c + V(0, 0.075f, -0.01f), 0.13f, 0.018f, Rgb(200, 236, 230), mat: Glow, blend: 0.008f);
                break;
        }
        return Lift(b);
    }

    private static PokeBuilder Castform() => CastformBuild(0);

    // ------------------------------------------------------------------ Kecleon

    /// <summary>Kecleon: a green chameleon standing up, a red zigzag band round its belly, eyes on cones, a jagged yellow crest and a curled tail.</summary>
    private static PokeBuilder Kecleon()
    {
        var b = new PokeBuilder("Kecleon", 0.75f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Scales };
        var green = Rgb(140, 200, 100);
        var red = Rgb(200, 60, 90);
        var yellow = Rgb(240, 214, 110);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.2f, 0));
            b.Limb(leg, V(0.06f * s, 0.21f, 0), V(0.08f * s, 0.04f, 0.02f), 0.035f, 0.024f, green);
            Digits(b, leg, V(0.08f * s, 0.02f, 0.03f), V(0.2f * s, 0, 1f), V(0.6f, 0, 0), 0.04f, 0.01f, yellow);
        });
        var bc = V(0, 0.3f, 0);
        var br = V(0.085f, 0.12f, 0.08f);
        b.Ell(Body, bc, br, green);
        b.PaintTorus(Body, V(0, 0.27f, 0), 0.08f, 0.02f, red, sz: 0.95f);
        b.Mark(Body, On(bc, br, 0, 0.3f), V(0, 0, 1f), 0.06f, 0.015f, red, MarkShape.Zigzag);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.37f, 0.02f));
            var hand = V(0.14f * s, 0.32f, 0.14f);
            b.Limb(arm, V(0.07f * s, 0.37f, 0.02f), hand, 0.02f, 0.016f, green);
            Digits(b, arm, hand, V(0.1f * s, -0.3f, 1f), V(0.6f, 0, 0), 0.035f, 0.007f, yellow);
        });
        // A tail curled flat into a spiral behind it
        int tail = b.Tail(V(0, 0.22f, -0.07f));
        Curl(b, tail, V(0, 0.22f, -0.07f), V(0, 0.2f, -0.2f), V(0, 0, -1f), V(0, 1f, 0), 0.09f, 1.6f, 0.03f, green);
        // A head with eyes in cones on its sides, a wide mouth, a jagged crest over the back of it
        int head = b.Head(V(0, 0.42f, 0.02f));
        var c = V(0, 0.49f, 0.04f);
        var r = V(0.08f, 0.07f, 0.09f);
        b.Ell(head, c, r, green);
        b.Mark(head, On(c, r, 0, 0.455f), V(0, -0.3f, 1f), 0.05f, 0.012f, Rgb(60, 90, 50), MarkShape.Smile);
        for (int i = 0; i < 4; i++)
        {
            float x = -0.045f + 0.03f * i;
            b.Spike(head, c + V(x, 0.06f, -0.04f), c + V(x * 1.4f, 0.12f, -0.1f), 0.022f, yellow, 0.5f);
        }
        PokeBuilder.Both(s =>
        {
            var cone = c + V(0.08f * s, 0.01f, 0.03f);
            b.Ell(head, cone, V(0.035f, 0.035f, 0.035f), green, blend: 0.015f);
            b.PaintTorus(head, cone + V(0.015f * s, 0, 0.005f), 0.026f, 0.006f, yellow, V(0, 0, 90f));
            b.Eye(head, cone + V(0.034f * s, 0, 0.005f), V(s, 0, 0.3f), 0.016f, Rgb(200, 60, 80));
        });
        return b;
    }

    // ------------------------------------------------------------------ Shuppet and Banette

    /// <summary>Shuppet: a navy puppet ghost floating, a horn on its head swept back, a ragged hem and big blue eyes rimmed black.</summary>
    private static PokeBuilder Shuppet()
    {
        var b = new PokeBuilder("Shuppet", 0.55f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Fur }.Hover();
        var navy = Rgb(70, 76, 120);
        var c = V(0, 0.3f, 0);
        var r = V(0.11f, 0.11f, 0.1f);
        b.Ell(Body, c, r, navy);
        // A ragged hem below and a cloth tail behind
        for (int i = 0; i < 6; i++)
        {
            float a = MathF.Tau * i / 6f;
            b.Spike(Body, c + V(MathF.Sin(a) * 0.07f, -0.06f, MathF.Cos(a) * 0.06f), c + V(MathF.Sin(a) * 0.1f, -0.16f, MathF.Cos(a) * 0.09f), 0.04f, navy, 0.6f, blend: 0.02f);
        }
        int tail = b.Tail(c + V(0, -0.02f, -0.08f));
        Blade(b, tail, c + V(0, -0.04f, -0.07f), c + V(0.03f, -0.1f, -0.16f), 0.045f, navy, V(0, 1f, 0), 0.3f);
        int head = b.Head(c + V(0, 0.08f, 0));
        b.Spike(head, c + V(0, 0.08f, -0.01f), c + V(0, 0.26f, -0.1f), 0.06f, navy, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, 0.29f);
            b.Mark(Body, at, Outward(c, r, at), 0.04f, 0.03f, Rgb(30, 30, 40), rollDeg: -20f * s);
            b.Eye(Body, at, Outward(c, r, at), 0.02f, sclera: true, white: Rgb(150, 200, 236), pupil: Rgb(246, 210, 60));
        });
        return Lift(b);
    }

    /// <summary>
    /// Banette and its Mega Evolution: a black-grey cursed doll, a zipper for a mouth, red eyes, a horn and a long
    /// cloth tail from its head, a little star at its tail's tip; the Mega's zippers all over and long pink claws of
    /// cloth on its arms.
    /// </summary>
    private static PokeBuilder BanetteBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Banette-Mega" : "Banette", 0.75f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var gray = Rgb(90, 90, 100);
        var zip = Rgb(240, 200, 70);
        var pink = Rgb(220, 90, 130);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.14f, 0));
            b.Limb(leg, V(0.05f * s, 0.15f, 0), V(0.06f * s, 0.03f, 0.01f), 0.03f, 0.026f, gray);
            b.Ell(leg, V(0.06f * s, 0.02f, 0.02f), V(0.03f, 0.02f, 0.04f), gray);
        });
        var bc = V(0, 0.24f, 0);
        var br = V(0.08f, 0.1f, 0.07f);
        b.Ell(Body, bc, br, gray);
        if (mega)
            foreach (float x in new[] { -0.04f, 0.04f })
                b.PaintEll(Body, V(x, 0.24f, 0.07f), V(0.006f, 0.08f, 0.02f), zip, soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.29f, 0));
            var hand = V((mega ? 0.2f : 0.16f) * s, mega ? 0.3f : 0.2f, 0.04f);
            b.Limb(arm, V(0.07f * s, 0.29f, 0), hand, 0.025f, 0.03f, gray);
            if (mega)
                for (int i = -1; i <= 1; i++)
                    Blade(b, arm, hand, hand + V(0.1f * s, -0.12f + 0.04f * i, 0.04f * i), 0.03f, pink, V(0, 0, 1f), 0.3f);
            else
                b.Ell(arm, hand, V(0.03f, 0.03f, 0.03f), gray);
        });
        // A tail with a little star at its end
        int tail = b.Tail(V(0, 0.18f, -0.06f));
        var tp = Smooth(3, V(0, 0.18f, -0.06f), V(-0.06f, 0.12f, -0.12f), V(-0.12f, 0.08f, -0.08f));
        b.Tube(tail, tp, 0.012f, 0.01f, gray, blend: 0f);
        StarPoints(b, tail, tp[^1] + V(-0.02f, 0, 0), 0.035f, 0.016f, 0f, 0.5f, zip);
        // The head: red eyes, a zipped mouth, a horn and a long cloth streamer falling behind
        int head = b.Head(V(0, 0.33f, 0.01f));
        var c = V(0, 0.4f, 0.02f);
        var r = V(0.085f, 0.075f, 0.075f);
        b.Ell(head, c, r, gray);
        b.Mark(head, On(c, r, 0, 0.37f), V(0, -0.2f, 1f), 0.06f, 0.012f, zip, MarkShape.Zigzag);
        b.Spike(head, c + V(0, 0.05f, -0.02f), c + V(0, 0.16f, -0.04f), 0.035f, gray);
        var streamer = mega
            ? Smooth(3, c + V(0, 0.05f, -0.05f), c + V(0.02f, 0.18f, -0.1f), c + V(0.06f, 0.24f, -0.02f), c + V(0.1f, 0.2f, 0.04f))
            : Smooth(3, c + V(0, 0.04f, -0.06f), c + V(0.06f, 0.06f, -0.14f), c + V(0.12f, -0.02f, -0.18f), c + V(0.14f, -0.14f, -0.16f));
        b.Tube(head, streamer, 0.035f, 0.02f, gray, blend: 0f);
        if (mega) b.PaintEll(head, streamer[streamer.Length / 2], V(0.04f, 0.04f, 0.04f), zip);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, 0.41f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, sclera: true, white: Rgb(240, 150, 170), pupil: Rgb(170, 30, 60), glare: true);
        });
        return b;
    }

    private static PokeBuilder Banette() => BanetteBuild(false);

    // ------------------------------------------------------------------ Wynaut

    /// <summary>Wynaut: a cheerful blue blob with long flat ear-arms, eyes shut in a smile, a big open mouth and a tail with a black tip.</summary>
    private static PokeBuilder Wynaut()
    {
        var b = new PokeBuilder("Wynaut", 0.55f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Fur };
        var blue = Rgb(130, 200, 216);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.06f, 0));
            b.Spike(leg, V(0.04f * s, 0.06f, 0), V(0.05f * s, 0.0f, 0.02f), 0.035f, blue, 0.6f);
        });
        var bc = V(0, 0.16f, 0);
        var br = V(0.085f, 0.11f, 0.075f);
        b.Ell(Body, bc, br, blue);
        int tail = b.Tail(V(0, 0.08f, -0.06f));
        var tp = Smooth(3, V(0, 0.08f, -0.06f), V(0.06f, 0.03f, -0.12f), V(0.14f, 0.03f, -0.1f));
        b.Tube(tail, tp, 0.012f, 0.01f, Rgb(40, 40, 50), blend: 0f);
        b.Ell(tail, tp[^1], V(0.03f, 0.015f, 0.03f), Rgb(40, 40, 50), blend: 0.006f);
        b.Mark(tail, tp[^1] + V(0, 0.015f, 0), V(0, 1f, 0), 0.015f, 0.015f, White, MarkShape.Ring);
        // The head is the top of the body: two long flat ears hanging out like arms
        int head = b.Head(V(0, 0.24f, 0));
        var c = V(0, 0.28f, 0.01f);
        var r = V(0.085f, 0.08f, 0.075f);
        b.Ell(head, c, r, blue);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.32f, 0));
            var path = Smooth(3, c + V(0.05f * s, 0.05f, 0), c + V(0.13f * s, 0.06f, 0.0f), c + V(0.17f * s, -0.02f, 0.02f), c + V(0.18f * s, -0.1f, 0.03f));
            b.Tube(ear, path, 0.024f, 0.03f, blue, blend: 0f);
            var at = On(c, r, 0.035f * s, 0.3f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(40, 40, 50), closed: true);
        });
        Grin(b, head, On(c, r, 0, 0.255f), V(0.04f, 0.025f, 0.02f), Rgb(220, 110, 130));
        return b;
    }

    // ------------------------------------------------------------------ Spheal line

    /// <summary>Spheal: a round blue seal, its belly cream, white spots on its back, small flippers, a tail fin and two little fangs.</summary>
    private static PokeBuilder Spheal()
    {
        var b = new PokeBuilder("Spheal", 0.5f, BodyPlan.Quadruped, V(0, 0.15f, 0)) { Coat = Fur };
        var blue = Rgb(110, 160, 220);
        var cream = Rgb(246, 236, 200);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.06f, 0.05f));
            Frond(b, leg, V(0.1f * s, 0.06f, 0.05f), V(0.17f * s, 0.02f, 0.09f), 0.035f, blue, V(0, 1f, 0), 0.35f);
        });
        var bc = V(0, 0.15f, 0);
        var br = V(0.14f, 0.14f, 0.13f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, V(0, 0.08f, 0.06f), V(0.14f, 0.11f, 0.12f), cream);
        foreach (var n in new[] { V(0.6f, 0.4f, -0.6f), V(-0.5f, 0.6f, -0.5f), V(0.3f, 0.2f, -0.9f), V(-0.7f, 0.1f, -0.6f), V(0.1f, 0.8f, -0.5f) })
            b.Mark(Body, Out(bc, br, default, n), n, 0.016f, 0.016f, White);
        int tail = b.Tail(V(0, 0.06f, -0.12f));
        b.Limb(tail, V(0, 0.08f, -0.09f), V(0, 0.05f, -0.13f), 0.03f, 0.02f, blue);
        PokeBuilder.Both(s => Frond(b, tail, V(0, 0.05f, -0.12f), V(0.07f * s, 0.03f, -0.2f), 0.035f, blue, V(0, 1f, 0), 0.35f));
        int head = b.Head(V(0, 0.2f, 0.1f));
        b.Ell(head, V(0, 0.16f, 0.13f), V(0.04f, 0.03f, 0.02f), blue, blend: 0.02f);
        b.Ell(head, V(0, 0.17f, 0.15f), V(0.012f, 0.009f, 0.007f), Rgb(40, 40, 60), mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.02f * s, 0.14f, 0.135f), V(0.022f * s, 0.11f, 0.14f), 0.009f, White, mat: Shell, blend: 0.003f);
            var at = On(bc, br, 0.05f * s, 0.2f);
            b.Eye(Body, at, Outward(bc, br, at), 0.014f, Rgb(36, 36, 48));
        });
        return b;
    }

    /// <summary>Sealeo: a blue sea lion sitting up, a cream belly, a broad white whisker on each side of its nose, flippers and a tail fin.</summary>
    private static PokeBuilder Sealeo()
    {
        var b = new PokeBuilder("Sealeo", 0.75f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Fur };
        var blue = Rgb(110, 160, 220);
        var cream = Rgb(246, 236, 200);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.12f, 0.06f));
            b.Limb(leg, V(0.05f * s, 0.16f, 0.04f), V(0.1f * s, 0.12f, 0.06f), 0.03f, 0.022f, blue);
            Frond(b, leg, V(0.1f * s, 0.12f, 0.06f), V(0.16f * s, 0.02f, 0.12f), 0.04f, blue, V(1f, 0.3f, 0), 0.35f);
        });
        var bc = V(0, 0.18f, -0.03f);
        var br = V(0.12f, 0.14f, 0.16f);
        b.Ell(Body, bc, br, blue, V(-25f, 0, 0));
        b.PaintEll(Body, V(0, 0.16f, 0.06f), V(0.1f, 0.12f, 0.08f), cream);
        int tail = b.Tail(V(0, 0.06f, -0.16f));
        b.Limb(tail, V(0, 0.06f, -0.15f), V(0, 0.04f, -0.24f), 0.04f, 0.025f, blue);
        PokeBuilder.Both(s => Frond(b, tail, V(0, 0.04f, -0.24f), V(0.07f * s, 0.03f, -0.32f), 0.035f, blue, V(0, 1f, 0), 0.35f));
        int head = b.Head(V(0, 0.3f, 0.06f));
        var c = V(0, 0.35f, 0.08f);
        var r = V(0.08f, 0.07f, 0.08f);
        b.Ell(head, c, r, blue);
        b.Ell(head, V(0, 0.33f, 0.15f), V(0.035f, 0.03f, 0.03f), cream, blend: 0.02f);
        b.Ell(head, V(0, 0.345f, 0.18f), V(0.013f, 0.01f, 0.008f), Rgb(40, 40, 60), mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            Frond(b, head, V(0.02f * s, 0.33f, 0.16f), V(0.12f * s, 0.32f, 0.15f), 0.03f, White, V(0, 0.2f, 1f), 0.3f);
            int ear = b.Ear(head, s, V(0.06f * s, 0.4f, 0.06f));
            b.Ell(ear, V(0.065f * s, 0.41f, 0.05f), V(0.015f, 0.02f, 0.012f), blue, blend: 0.008f);
            var at = On(c, r, 0.035f * s, 0.37f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(36, 36, 48));
        });
        return b;
    }

    /// <summary>Walrein: a great blue walrus, a thick cream mane round its neck, long white tusks, a blue collar band and broad flippers.</summary>
    private static PokeBuilder Walrein()
    {
        var b = new PokeBuilder("Walrein", 1f, BodyPlan.Quadruped, V(0, 0.28f, -0.03f)) { Coat = Fur };
        var blue = Rgb(100, 150, 210);
        var mane = Rgb(236, 232, 214);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.14f * s, 0.16f, 0.1f));
            b.Limb(leg, V(0.14f * s, 0.18f, 0.1f), V(0.18f * s, 0.05f, 0.14f), 0.05f, 0.04f, blue);
            Frond(b, leg, V(0.18f * s, 0.04f, 0.14f), V(0.24f * s, 0.02f, 0.24f), 0.05f, blue, V(0, 1f, 0), 0.3f);
        });
        var bc = V(0, 0.25f, -0.06f);
        var br = V(0.17f, 0.17f, 0.24f);
        b.Ell(Body, bc, br, blue, V(-15f, 0, 0));
        b.PaintEll(Body, V(0, 0.15f, 0.0f), V(0.15f, 0.08f, 0.2f), Rgb(180, 210, 236));
        int tail = b.Tail(V(0, 0.1f, -0.26f));
        b.Limb(tail, V(0, 0.1f, -0.26f), V(0, 0.05f, -0.4f), 0.06f, 0.04f, blue);
        PokeBuilder.Both(s => Frond(b, tail, V(0, 0.04f, -0.4f), V(0.1f * s, 0.03f, -0.5f), 0.05f, blue, V(0, 1f, 0), 0.3f));
        // A shaggy mane round its neck, a collar band and a head with long tusks
        b.Torus(Body, V(0, 0.4f, 0.12f), 0.13f, 0.02f, blue, V(-35f, 0, 0), mat: Shell, blend: 0.012f);
        Lumps(b, Body, V(0, 0.42f, 0.1f), V(0.16f, 0.09f, 0.11f), 11, 0.05f, mane, Rgb(250, 248, 240), 0.9f);
        int head = b.Head(V(0, 0.48f, 0.16f));
        var c = V(0, 0.5f, 0.2f);
        var r = V(0.085f, 0.08f, 0.09f);
        b.Ell(head, c, r, blue);
        b.Ell(head, V(0, 0.47f, 0.28f), V(0.055f, 0.04f, 0.04f), blue, blend: 0.02f);
        b.Ell(head, V(0, 0.48f, 0.315f), V(0.015f, 0.012f, 0.01f), Rgb(40, 40, 60), mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, V(0.03f * s, 0.44f, 0.3f), V(0.035f * s, 0.34f, 0.33f), V(0.03f * s, 0.24f, 0.32f)), 0.018f, 0.008f, White, Shell, 0f);
            var at = On(c, r, 0.04f * s, 0.52f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(36, 36, 48), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Clamperl line

    /// <summary>Clamperl: a blue clam gaping open, its inside dark and its rim jagged, a sleeping pink pearl cradled in blue cushions.</summary>
    private static PokeBuilder Clamperl()
    {
        var b = new PokeBuilder("Clamperl", 0.55f, BodyPlan.Floating, V(0, 0.12f, 0)) { Coat = Shell };
        var blue = Rgb(70, 110, 180);
        var dark = Rgb(30, 30, 44);
        var cushion = Rgb(150, 196, 236);
        var pink = Rgb(244, 170, 180);
        // The lower shell hollowed into a bowl, the upper one standing open behind
        b.Ell(Body, V(0, 0.08f, 0), V(0.18f, 0.08f, 0.16f), blue);
        b.Cut(Body, V(0, 0.14f, 0.01f), V(0.16f, 0.08f, 0.14f));
        int lid = b.Part("lid", Body, V(0, 0.08f, -0.12f), PokeRole.Fin);
        var lc = V(0, 0.24f, -0.13f);
        b.Ell(lid, lc, V(0.18f, 0.17f, 0.04f), blue, V(-15f, 0, 0));
        b.PaintEll(lid, lc + V(0, -0.005f, 0.04f), V(0.155f, 0.145f, 0.03f), dark, V(-15f, 0, 0));
        for (int i = 0; i < 9; i++)
        {
            float a = (-80f + 20f * i) * Degree;
            var root = lc + V(MathF.Sin(a) * 0.16f, MathF.Cos(a) * 0.15f, -0.02f);
            b.Spike(lid, root, root + V(MathF.Sin(a) * 0.04f, MathF.Cos(a) * 0.04f, 0), 0.025f, blue, 0.5f);
        }
        b.PaintEll(Body, V(0, 0.08f, 0.02f), V(0.15f, 0.06f, 0.13f), dark);
        // The pearl on its cushions
        foreach (var (x, z) in new[] { (-0.09f, 0.03f), (0.09f, 0.03f), (-0.05f, 0.09f), (0.05f, 0.09f), (0f, -0.02f) })
            b.Ell(Body, V(x, 0.1f, z), V(0.05f, 0.04f, 0.045f), cushion, blend: 0.015f);
        int head = b.Head(V(0, 0.14f, 0.03f));
        var c = V(0, 0.17f, 0.04f);
        var r = V(0.065f, 0.065f, 0.065f);
        b.Ell(head, c, r, pink, mat: Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, 0.18f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(80, 50, 60), closed: true);
        });
        return b;
    }

    /// <summary>Huntail: a blue eel with orange fins, white spots ringed orange down its sides, a mouth of fangs and a tail ending in a little fish-shaped lure.</summary>
    private static PokeBuilder Huntail()
    {
        var blue = Rgb(90, 150, 210);
        var orange = Rgb(236, 150, 70);
        var points = Smooth(2, V(0.04f, 0.42f, -0.34f), V(0.06f, 0.3f, -0.24f), V(0.0f, 0.36f, -0.08f), V(-0.04f, 0.3f, 0.06f), V(0, 0.26f, 0.14f));
        var b = new PokeBuilder("Huntail", 0.85f, BodyPlan.Serpent, points[0]) { Coat = Scales }.Hover();
        int neck = Coils(b, points, t => 0.03f + 0.03f * t, blue);
        for (int i = 2; i < points.Length - 1; i += 2)
        {
            int bone = i < 2 ? Body : 1 + i / 2;
            var mid = Vector3.Lerp(points[i], points[i + 1], 0.5f);
            float t = (i + 0.5f) / (points.Length - 1);
            Blade(b, bone, mid + V(0, 0.02f, 0), mid + V(0, 0.07f + 0.03f * t, -0.03f), 0.03f, orange, V(1f, 0, 0), 0.3f);
        }
        // A lure at the tail's tip: a little fish shape with an eye-spot
        int tail = b.Tail(points[0]);
        b.Ell(tail, points[0] + V(0, 0.03f, -0.03f), V(0.02f, 0.04f, 0.035f), blue, V(-40f, 0, 0), blend: 0.008f);
        PokeBuilder.Both(s => Blade(b, tail, points[0] + V(0, 0.05f, -0.06f), points[0] + V(0.04f * s, 0.1f, -0.09f), 0.02f, blue, V(1f, 0, 0), 0.3f));
        // A big head with a jaw full of fangs, orange fins at its cheeks
        int head = b.Head(points[^1], neck);
        var c = points[^1] + V(0, 0.02f, 0.06f);
        var r = V(0.075f, 0.065f, 0.085f);
        b.Ell(head, c, r, blue);
        var mouth = c + V(0, -0.025f, 0.06f);
        Grin(b, head, mouth, V(0.05f, 0.03f, 0.03f), Rgb(220, 120, 130));
        PokeBuilder.Both(s =>
        {
            b.Spike(head, mouth + V(0.025f * s, 0.035f, 0.0f), mouth + V(0.025f * s, 0.0f, 0.01f), 0.01f, White, mat: Shell, blend: 0.003f);
            Blade(b, head, c + V(0.06f * s, -0.04f, 0.0f), c + V(0.13f * s, -0.08f, -0.04f), 0.035f, orange, V(0, 1f, 0.3f), 0.3f);
            var at = On(c, r, 0.045f * s, c.Y + 0.035f);
            b.Mark(head, at, Outward(c, r, at), 0.016f, 0.016f, orange, MarkShape.Ring);
            b.Eye(head, at, Outward(c, r, at), 0.01f, sclera: true, pupil: Rgb(30, 30, 40));
        });
        return Lift(b);
    }

    /// <summary>Gorebyss: a slender pink fish arched in the water, a long thin snout, long fins from its head and a purple tail fin.</summary>
    private static PokeBuilder Gorebyss()
    {
        var pink = Rgb(240, 160, 196);
        var purple = Rgb(176, 130, 200);
        var points = Smooth(2, V(0.06f, 0.22f, -0.32f), V(0.04f, 0.32f, -0.22f), V(0.0f, 0.4f, -0.08f), V(0, 0.42f, 0.04f));
        var b = new PokeBuilder("Gorebyss", 0.8f, BodyPlan.Serpent, points[0]) { Coat = Scales }.Hover();
        int neck = Coils(b, points, t => 0.022f + 0.02f * t, pink);
        foreach (int i in new[] { 1, 2 })
            b.PaintTorus(i < 2 ? Body : 1 + i / 2, Vector3.Lerp(points[i], points[i + 1], 0.5f), 0.025f, 0.005f, White, Euler(points[i + 1] - points[i]));
        int tail = b.Tail(points[0]);
        b.Ell(tail, points[0] + V(0.01f, -0.05f, -0.02f), V(0.05f, 0.06f, 0.012f), purple, V(0, 60f, 0), blend: 0.01f);
        int head = b.Head(points[^1], neck);
        var c = points[^1] + V(0, 0.01f, 0.04f);
        var r = V(0.04f, 0.045f, 0.06f);
        b.Ell(head, c, r, pink);
        b.Spike(head, c + V(0, -0.01f, 0.04f), c + V(0, -0.04f, 0.2f), 0.02f, pink, 0.7f);
        PokeBuilder.Both(s =>
        {
            Ribbon(b, head, Smooth(2, c + V(0.02f * s, 0.03f, -0.02f), c + V(0.05f * s, 0.07f, -0.14f), c + V(0.06f * s, 0.03f, -0.28f)), 0.03f, pink, V(s, 0.3f, 0), 0.008f);
            var at = On(c, r, 0.025f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, sclera: true, pupil: Rgb(30, 30, 40));
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, points[2], PokeRole.Fin, s, s);
            b.Ell(fin, points[2] + V(0.035f * s, -0.03f, 0), V(0.02f, 0.03f, 0.012f), purple, blend: 0.01f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Relicanth and Luvdisc

    /// <summary>Relicanth: an ancient fish, brown with darker patches like rock, a grumpy mouth, a red spot on its side and stiff lobed fins.</summary>
    private static PokeBuilder Relicanth()
    {
        var b = new PokeBuilder("Relicanth", 0.8f, BodyPlan.Fish, V(0, 0.28f, 0)) { Coat = Scales }.Hover();
        var brown = Rgb(170, 150, 120);
        var dark = Rgb(100, 86, 74);
        var c = V(0, 0.28f, 0);
        var r = V(0.1f, 0.13f, 0.22f);
        b.Ell(Body, c, r, brown);
        foreach (var (x, y, z) in new[] { (0.08f, 0.04f, -0.04f), (-0.08f, 0.0f, -0.08f), (0.06f, -0.04f, 0.06f), (-0.06f, 0.06f, 0.04f), (0f, 0.1f, -0.12f) })
            b.PaintEll(Body, c + V(x, y, z), V(0.05f, 0.05f, 0.06f), dark);
        PokeBuilder.Both(s => b.Mark(Body, Out(c, r, default, V(s, 0.15f, 0.2f)), V(s, 0.15f, 0.2f), 0.012f, 0.012f, Rgb(214, 60, 60)));
        int tail = b.Tail(c + V(0, 0, -0.2f));
        foreach (var (y, z) in new[] { (0.1f, -0.38f), (-0.08f, -0.36f) })
            Frond(b, tail, c + V(0, 0, -0.2f), c + V(0, y, z), 0.05f, dark, V(1f, 0, 0), 0.3f);
        Frond(b, Body, c + V(0, 0.1f, -0.04f), c + V(0, 0.2f, -0.14f), 0.04f, dark, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, c + V(0.08f * s, -0.06f, 0.06f), PokeRole.Fin, s, s);
            b.Limb(fin, c + V(0.08f * s, -0.06f, 0.06f), c + V(0.13f * s, -0.14f, 0.04f), 0.02f, 0.016f, brown);
            Frond(b, fin, c + V(0.13f * s, -0.14f, 0.04f), c + V(0.16f * s, -0.2f, 0.0f), 0.03f, dark, V(1f, 0, 0), 0.3f);
            var at = Out(c, r, default, V(0.55f * s, 0.25f, 0.8f));
            b.Eye(Body, at, V(0.55f * s, 0.25f, 0.8f), 0.012f, Rgb(36, 36, 40));
        });
        b.Mark(Body, Out(c, r, default, V(0, -0.25f, 1f)), V(0, -0.25f, 1f), 0.03f, 0.01f, dark, MarkShape.Smile, 180f);
        return Lift(b);
    }

    /// <summary>Luvdisc: a pink heart-shaped fish with a little puckered mouth.</summary>
    private static PokeBuilder Luvdisc()
    {
        var b = new PokeBuilder("Luvdisc", 0.5f, BodyPlan.Fish, V(0, 0.25f, 0)) { Coat = Scales }.Hover();
        var pink = Rgb(244, 150, 170);
        var c = V(0, 0.25f, 0);
        PokeBuilder.Both(s => b.Ell(Body, c + V(0.055f * s, 0.04f, 0), V(0.075f, 0.075f, 0.04f), pink, V(0, 0, 25f * s), blend: 0.03f));
        b.Spike(Body, c + V(0, 0.0f, 0), c + V(0, -0.14f, 0), 0.07f, pink, 0.6f, blend: 0.03f);
        int tail = b.Tail(c + V(0, 0.02f, -0.03f));
        b.Ell(tail, c + V(0, 0.03f, -0.045f), V(0.03f, 0.03f, 0.02f), pink, blend: 0.01f);
        b.Ell(Body, c + V(0.0f, 0.0f, 0.04f), V(0.022f, 0.016f, 0.014f), Rgb(250, 190, 200), blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var ec = c + V(0.055f * s, 0.04f, 0);
            var at = ec + V(-0.01f * s, 0.005f, 0.038f);
            b.Eye(Body, at, V(0, 0.1f, 1f), 0.012f, Rgb(36, 36, 48));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Bagon line

    /// <summary>Bagon: a little blue dragon with a big round grey helmet of a head, a yellow jaw and belly and two small fangs.</summary>
    private static PokeBuilder Bagon()
    {
        var b = new PokeBuilder("Bagon", 0.55f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Scales };
        var blue = Rgb(100, 170, 220);
        var gray = Rgb(200, 200, 204);
        var yellow = Rgb(244, 214, 110);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.12f, 0));
            b.Limb(leg, V(0.05f * s, 0.13f, 0), V(0.06f * s, 0.035f, 0.02f), 0.035f, 0.03f, blue);
            b.Ell(leg, V(0.06f * s, 0.022f, 0.035f), V(0.032f, 0.022f, 0.045f), blue);
        });
        var bc = V(0, 0.19f, 0);
        var br = V(0.07f, 0.09f, 0.065f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, V(0, 0.17f, 0.05f), V(0.045f, 0.06f, 0.03f), yellow);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.23f, 0.02f));
            b.Limb(arm, V(0.06f * s, 0.23f, 0.02f), V(0.1f * s, 0.18f, 0.06f), 0.018f, 0.016f, blue);
        });
        int tail = b.Tail(V(0, 0.14f, -0.05f));
        b.Spike(tail, V(0, 0.14f, -0.05f), V(0, 0.08f, -0.15f), 0.035f, blue);
        // The head: a round grey helmet over the top and back, a blue face, a yellow jaw with two fangs
        int head = b.Head(V(0, 0.27f, 0.02f));
        var c = V(0, 0.33f, 0.03f);
        var r = V(0.085f, 0.08f, 0.085f);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, 0.035f, -0.025f), V(0.095f, 0.075f, 0.09f), gray, blend: 0.012f);
        for (int i = 0; i < 3; i++)
            b.PaintTorus(head, c + V(0, 0.04f, -0.03f), 0.09f - i * 0.0f, 0.004f, Rgb(160, 160, 166), V(0, 0, 60f - 60f * i), 1f, 1f);
        b.Ell(head, V(0, 0.29f, 0.09f), V(0.055f, 0.025f, 0.045f), yellow, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.03f * s, 0.305f, 0.12f), V(0.032f * s, 0.33f, 0.125f), 0.008f, White, mat: Shell, blend: 0.003f);
            var at = On(c, r, 0.04f * s, 0.32f);
            b.Mark(head, at, Outward(c, r, at), 0.018f, 0.014f, Black);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(246, 210, 60), glare: true);
        });
        return b;
    }

    /// <summary>Shelgon: a hard white shell like a ridged cocoon on four short grey legs, a dark face peering out with yellow eyes.</summary>
    private static PokeBuilder Shelgon()
    {
        var b = new PokeBuilder("Shelgon", 0.65f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Shell };
        var white = Rgb(232, 232, 236);
        var gray = Rgb(100, 100, 108);
        var red = Rgb(200, 70, 80);
        StubbyLegs(b, 0.09f, 0.08f, 0.08f, -0.08f, 0.04f, gray, red);
        var bc = V(0, 0.21f, 0);
        var br = V(0.15f, 0.15f, 0.16f);
        b.Ell(Body, bc, br, white);
        // Ridges round its shell from front to back
        foreach (float x in new[] { -0.1f, -0.05f, 0.05f, 0.1f })
        {
            float k = MathF.Sqrt(1f - x * x / (br.X * br.X));
            b.PaintTorus(Body, bc + V(x, 0, 0), br.Y * k, 0.005f, Rgb(180, 180, 188), V(0, 0, 90f), 1f, br.Z / br.Y);
        }
        b.Ell(Body, bc + V(0, 0.05f, -0.08f), V(0.12f, 0.08f, 0.08f), white, blend: 0.02f);
        int head = b.Head(bc + V(0, 0, 0.12f));
        var fc = bc + V(0, -0.01f, 0.14f);
        b.PaintEll(Body, fc, V(0.06f, 0.06f, 0.04f), gray);
        b.Ell(head, fc + V(0, 0, -0.005f), V(0.05f, 0.05f, 0.025f), gray, blend: 0.006f);
        PokeBuilder.Both(s => b.Eye(head, fc + V(0.024f * s, 0.005f, 0.019f), V(0.2f * s, 0, 1f), 0.012f, sclera: true, white: Rgb(244, 220, 90), pupil: Rgb(40, 40, 40), glare: true));
        return b;
    }

    /// <summary>
    /// Salamence and its Mega Evolution: a blue dragon on four legs, grey plates down its chest, a long tail and two
    /// great red wings; the Mega's wings joined into one red crescent round it.
    /// </summary>
    private static PokeBuilder SalamenceBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Salamence-Mega" : "Salamence", 1f, BodyPlan.Quadruped, V(0, 0.3f, 0)) { Coat = Scales };
        var blue = Rgb(80, 150, 200);
        var gray = Rgb(214, 214, 220);
        var red = Rgb(200, 60, 70);
        var bc = V(0, 0.3f, -0.02f);
        var br = V(0.11f, 0.1f, 0.2f);
        // The Mega's crescent first, its middle carved away before the body goes in
        if (mega)
        {
            int ring = b.Wing(1f, bc + V(0, 0.06f, 0));
            b.Ell(ring, bc + V(0, 0.08f, -0.06f), V(0.44f, 0.025f, 0.38f), red, V(-8f, 0, 0), blend: 0.01f);
            b.Cut(ring, bc + V(0, 0.08f, 0.14f), V(0.36f, 0.08f, 0.36f), V(-8f, 0, 0));
        }
        foreach (var (z, front) in new[] { (0.12f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.08f * s, 0.26f, z), front);
                var knee = V(0.1f * s, 0.14f, z + (front ? 0.02f : -0.03f));
                b.Limb(leg, V(0.08f * s, 0.28f, z), knee, 0.05f, 0.034f, blue);
                b.Limb(leg, knee, V(0.1f * s, 0.035f, z + 0.02f), 0.03f, 0.026f, blue);
                Digits(b, leg, V(0.1f * s, 0.02f, z + 0.05f), V(0, -0.1f, 1f), V(0.6f, 0, 0), 0.035f, 0.01f, White, Shell);
            });
        b.Ell(Body, bc, br, blue);
        for (int i = 0; i < 5; i++)
            b.PaintEll(Body, V(0, 0.22f, 0.12f - i * 0.06f), V(0.08f, 0.03f, 0.025f), gray);
        if (!mega)
            PokeBuilder.Both(s =>
            {
                // A great red wing: a broad membrane swept back, its trailing point a blade
                int wing = b.Wing(s, V(0.07f * s, 0.38f, 0.04f));
                var root = V(0.07f * s, 0.38f, 0.04f);
                var tip = V(0.36f * s, 0.52f, -0.12f);
                b.Limb(wing, root, tip, 0.022f, 0.014f, red);
                Frond(b, wing, root + V(0, 0, -0.02f), tip + V(0, -0.01f, -0.04f), 0.11f, red, V(0.2f * s, 1f, 0.1f), 0.1f, Shell);
                Frond(b, wing, Vector3.Lerp(root, tip, 0.35f) + V(0, 0, -0.04f), V(0.2f * s, 0.42f, -0.36f), 0.07f, red, V(0.2f * s, 1f, 0.1f), 0.12f, Shell);
                Blade(b, wing, tip + V(0, -0.01f, -0.02f), tip + V(0.06f * s, -0.04f, -0.16f), 0.05f, red, V(0.2f * s, 1f, 0.1f), 0.14f, Shell);
            });
        int tail = b.Tail(V(0, 0.3f, -0.2f));
        var tp = Smooth(3, V(0, 0.3f, -0.2f), V(0, 0.22f, -0.38f), V(0.04f, 0.14f, -0.54f));
        b.Tube(tail, tp, 0.06f, 0.02f, blue, blend: 0f);
        if (mega) b.PaintEll(tail, tp[^2], V(0.04f, 0.04f, 0.08f), red);
        // A long neck, a head with a grey jaw, red at the cheeks, a crest swept back
        int head = b.Head(V(0, 0.38f, 0.18f));
        b.Limb(head, V(0, 0.34f, 0.16f), V(0, 0.48f, 0.26f), 0.05f, 0.042f, blue);
        var c = V(0, 0.52f, 0.29f);
        var r = V(0.065f, 0.055f, 0.085f);
        b.Ell(head, c, r, blue);
        b.Ell(head, V(0, 0.49f, 0.33f), V(0.05f, 0.025f, 0.06f), gray, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.05f * s, -0.01f, 0.03f), V(0.02f, 0.02f, 0.03f), red);
            Blade(b, head, c + V(0.03f * s, 0.03f, -0.02f), c + V(0.09f * s, mega ? 0.04f : 0.08f, -0.16f), 0.03f, blue, V(0, 1f, 0.2f), 0.3f);
            var at = On(c, r, 0.035f * s, 0.535f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(214, 60, 70), glare: true);
        });
        b.Spike(head, c + V(0, 0.04f, -0.02f), c + V(0, 0.08f, -0.14f), 0.025f, gray);
        return b;
    }

    private static PokeBuilder Salamence() => SalamenceBuild(false);

    // ------------------------------------------------------------------ Beldum line

    /// <summary>Beldum: a steel blue cylinder floating level, one red eye at its front and grey hooks at its back.</summary>
    private static PokeBuilder Beldum()
    {
        var b = new PokeBuilder("Beldum", 0.6f, BodyPlan.Floating, V(0, 0.28f, 0)) { Coat = Metal }.Hover();
        var blue = Rgb(110, 150, 196);
        var gray = Rgb(214, 214, 222);
        var c = V(0, 0.28f, 0.02f);
        var r = V(0.075f, 0.085f, 0.17f);
        b.Ell(Body, c, r, blue);
        b.PaintTorus(Body, c + V(0, 0, -0.05f), 0.08f, 0.006f, Rgb(70, 96, 130), V(90f, 0, 0), 1f, 1.1f);
        int tail = b.Tail(c + V(0, 0, -0.15f));
        b.Ell(tail, c + V(0, 0, -0.16f), V(0.07f, 0.08f, 0.03f), blue, blend: 0.01f);
        foreach (var o in new[] { V(0, 0.07f, 0), V(0.06f, -0.04f, 0), V(-0.06f, -0.04f, 0) })
            b.Spike(tail, c + V(0, 0, -0.17f) + o * 0.8f, c + V(0, 0, -0.27f) + o * 1.3f, 0.02f, gray, mat: Shell);
        int head = b.Head(c + V(0, 0, 0.15f));
        var front = c + V(0, 0.0f, 0.15f);
        b.Ell(head, front, V(0.06f, 0.07f, 0.04f), blue, blend: 0.015f);
        var at = front + V(0, 0.0f, 0.04f);
        b.Mark(head, at, V(0, 0, 1f), 0.028f, 0.028f, Rgb(40, 50, 70));
        b.Eye(head, at, V(0, 0, 1f), 0.016f, sclera: true, white: Rgb(230, 110, 120), pupil: Rgb(30, 30, 40));
        return Lift(b);
    }

    /// <summary>Metang: two Beldum fused into a steel disc floating with one red eye in its top, white spikes at its sides and two great clawed arms.</summary>
    private static PokeBuilder Metang()
    {
        var b = new PokeBuilder("Metang", 0.85f, BodyPlan.Floating, V(0, 0.32f, 0)) { Coat = Metal }.Hover();
        var blue = Rgb(100, 140, 186);
        var gray = Rgb(226, 226, 232);
        var c = V(0, 0.36f, 0);
        var r = V(0.18f, 0.08f, 0.14f);
        b.Ell(Body, c, r, blue);
        b.Ell(Body, c + V(0, 0.06f, 0.03f), V(0.08f, 0.05f, 0.08f), blue, blend: 0.02f);
        b.PaintTorus(Body, c, 0.17f, 0.006f, Rgb(70, 96, 130), sz: 0.8f);
        PokeBuilder.Both(s =>
        {
            b.Spike(Body, c + V(0.12f * s, 0.05f, -0.02f), c + V(0.24f * s, 0.1f, -0.02f), 0.025f, gray, mat: Shell);
            b.Spike(Body, c + V(0.14f * s, 0.03f, -0.06f), c + V(0.24f * s, 0.05f, -0.12f), 0.02f, gray, mat: Shell);
            // An arm hanging from each side: a ball joint, a thick forearm, three claws
            int arm = b.Arm(s, c + V(0.17f * s, -0.02f, 0.03f));
            var elbow = c + V(0.22f * s, -0.06f, 0.04f);
            var fist = c + V(0.22f * s, -0.24f, 0.08f);
            b.Ell(arm, elbow, V(0.03f, 0.03f, 0.03f), gray);
            b.Limb(arm, c + V(0.16f * s, -0.02f, 0.03f), elbow, 0.02f, 0.02f, gray);
            b.Limb(arm, elbow, fist, 0.05f, 0.065f, blue);
            foreach (var x in new[] { -0.03f, 0f, 0.03f })
                b.Spike(arm, fist + V(x, -0.03f, 0.02f), fist + V(x * 1.3f, -0.1f, 0.05f), 0.016f, gray, mat: Shell);
        });
        int head = b.Head(c + V(0, 0.08f, 0.04f));
        var at = c + V(0, 0.09f, 0.085f);
        b.Ell(head, at + V(0, 0, -0.01f), V(0.03f, 0.025f, 0.02f), Rgb(40, 50, 70), blend: 0.008f);
        b.Eye(head, at + V(0, 0.002f, 0.01f), V(0, 0.4f, 1f), 0.016f, sclera: true, white: Rgb(230, 110, 120), pupil: Rgb(30, 30, 40));
        return Lift(b);
    }

    /// <summary>
    /// Metagross and its Mega Evolution: a heavy steel disc on four legs with grey claws, a grey X across its face and
    /// two red eyes; the Mega hovering, its four legs raised as arms round it and its X gold.
    /// </summary>
    private static PokeBuilder MetagrossBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Metagross-Mega" : "Metagross", 1f, BodyPlan.Quadruped, V(0, 0.3f, 0)) { Coat = Metal };
        if (mega) b.Hover();
        var blue = Rgb(100, 140, 186);
        var gray = Rgb(214, 214, 222);
        var cross = mega ? Rgb(230, 190, 80) : gray;
        var c = V(0, 0.3f, 0);
        var r = V(0.24f, 0.12f, 0.23f);
        b.Ell(Body, c, r, blue);
        b.Ell(Body, c + V(0, 0.08f, 0.0f), V(0.14f, 0.07f, 0.14f), blue, blend: 0.02f);
        foreach (var (x, z) in new[] { (1f, 1f), (-1f, 1f), (1f, -1f), (-1f, -1f) })
        {
            int leg = b.Leg(x, c + V(0.13f * x, -0.03f, 0.11f * z), z > 0);
            var shoulder = c + V(0.17f * x, -0.04f, 0.13f * z);
            var knee = mega ? c + V(0.3f * x, 0.14f, 0.2f * z) : c + V(0.26f * x, 0.02f, 0.21f * z);
            var foot = mega ? c + V(0.38f * x, 0.0f, 0.27f * z) : V(0.29f * x, 0.04f, 0.24f * z);
            b.Ell(leg, shoulder, V(0.035f, 0.035f, 0.035f), gray);
            b.Limb(leg, shoulder, knee, 0.026f, 0.026f, blue);
            b.Ell(leg, knee, V(0.035f, 0.035f, 0.035f), gray);
            b.Limb(leg, knee, foot + V(0, 0.04f, 0), 0.045f, 0.055f, blue);
            foreach (var o in new[] { -0.03f, 0f, 0.03f })
                b.Spike(leg, foot + V(o * x * 0.6f, 0.02f, o + 0.02f), foot + V(o * x, -0.03f, o * 1.3f + 0.05f), 0.014f, gray, mat: Shell);
        }
        int head = b.Head(c + V(0, 0.06f, 0.15f));
        var face = c + V(0, 0.05f, 0.21f);
        b.Ell(head, face, V(0.07f, 0.05f, 0.04f), blue, blend: 0.02f);
        PokeBuilder.Both(s => b.Box(head, face + V(0, 0.005f, 0.035f), V(0.075f, 0.012f, 0.01f), 0.004f, cross, V(0, 0, 35f * s), Shell, 0.004f));
        PokeBuilder.Both(s =>
        {
            var at = face + V(0.035f * s, -0.018f, 0.035f);
            b.Eye(head, at, V(0.3f * s, -0.2f, 1f), 0.011f, Rgb(214, 50, 60), glare: true);
        });
        return mega ? Lift(b) : b;
    }

    private static PokeBuilder Metagross() => MetagrossBuild(false);

    // ------------------------------------------------------------------ the Regis

    /// <summary>Seven dots in a pattern on a face, where a Regi's eyes would be.</summary>
    private static void Braille(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float step, Color color, (float x, float y)[] dots)
    {
        foreach (var (x, y) in dots)
            b.Mark(bone, at + V(x * step, y * step, 0), facing, step * 0.4f, step * 0.4f, color);
    }

    /// <summary>Regirock: a golem of boulders, pale with orange-brown patches, chunky rock arms and legs and seven orange dots on its face.</summary>
    private static PokeBuilder Regirock()
    {
        var b = new PokeBuilder("Regirock", 1f, BodyPlan.Biped, V(0, 0.45f, 0)) { Coat = Shell };
        var pale = Rgb(210, 196, 182);
        var brown = Rgb(196, 120, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.3f, 0));
            b.Box(leg, V(0.09f * s, 0.25f, 0), V(0.05f, 0.075f, 0.05f), 0.02f, pale, V(0, 15f, 5f * s), blend: 0.01f);
            b.Box(leg, V(0.1f * s, 0.13f, 0.01f), V(0.055f, 0.06f, 0.055f), 0.02f, brown, V(0, -10f, 0), blend: 0.01f);
            b.Box(leg, V(0.1f * s, 0.04f, 0.03f), V(0.065f, 0.04f, 0.07f), 0.02f, pale, V(0, 20f, 0), blend: 0.01f);
        });
        var bc = V(0, 0.48f, 0);
        b.Box(Body, bc, V(0.12f, 0.16f, 0.09f), 0.04f, pale, blend: 0.01f);
        b.Box(Body, bc + V(0, 0.12f, -0.01f), V(0.09f, 0.06f, 0.08f), 0.03f, pale, V(0, 0, 8f), blend: 0.01f);
        foreach (var (x, y) in new[] { (0.06f, 0.06f), (-0.07f, -0.06f), (0.05f, -0.1f) })
            b.PaintEll(Body, bc + V(x, y, 0.08f), V(0.04f, 0.035f, 0.03f), brown);
        b.PaintEll(Body, bc + V(-0.06f, 0.08f, -0.08f), V(0.05f, 0.05f, 0.04f), brown);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.56f, 0));
            b.Box(arm, V(0.17f * s, 0.58f, 0), V(0.07f, 0.08f, 0.07f), 0.03f, brown, V(0, 0, -15f * s), blend: 0.01f);
            b.Box(arm, V(0.2f * s, 0.46f, 0.02f), V(0.05f, 0.055f, 0.05f), 0.02f, pale, V(0, 20f, 0), blend: 0.01f);
            b.Box(arm, V(0.21f * s, 0.35f, 0.03f), V(0.055f, 0.05f, 0.055f), 0.02f, pale, V(0, -15f, 10f * s), blend: 0.01f);
            b.Box(arm, V(0.22f * s, 0.25f, 0.04f), V(0.065f, 0.05f, 0.06f), 0.025f, pale, V(0, 10f, 0), blend: 0.01f);
        });
        int head = b.Head(bc + V(0, 0.14f, 0));
        b.Box(head, bc + V(0, 0.2f, 0.0f), V(0.06f, 0.04f, 0.06f), 0.025f, pale, blend: 0.01f);
        Braille(b, Body, bc + V(0, 0.08f, 0.091f), V(0, 0, 1f), 0.032f, brown,
            new[] { (-1f, 1f), (-1f, 0f), (-1f, -1f), (1f, 1f), (1f, 0f), (1f, -1f), (0f, 0f) });
        return b;
    }

    /// <summary>Regice: a golem of clear blue ice, its body and limbs faceted crystals, standing on two crystal spikes, yellow dots in a cross on its face.</summary>
    private static PokeBuilder Regice()
    {
        var b = new PokeBuilder("Regice", 1f, BodyPlan.Biped, V(0, 0.48f, 0)) { Coat = Shell };
        var ice = Rgb(180, 222, 240);
        var deep = Rgb(130, 190, 226);
        var yellow = Rgb(246, 210, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.3f, 0));
            b.Box(leg, V(0.08f * s, 0.3f, 0), V(0.05f, 0.065f, 0.05f), 0.01f, ice, V(0, 45f, 0), blend: 0.008f);
            b.Spike(leg, V(0.08f * s, 0.24f, 0), V(0.09f * s, 0.01f, 0.01f), 0.06f, deep, mat: Glow, blend: 0.01f);
        });
        var bc = V(0, 0.5f, 0);
        b.Box(Body, bc, V(0.13f, 0.16f, 0.09f), 0.015f, ice, mat: Glow, blend: 0.008f);
        b.Spike(Body, bc + V(0, 0.12f, 0), bc + V(0, 0.28f, 0), 0.09f, ice, 0.6f, Glow, 0.01f);
        b.Box(Body, bc + V(0, -0.15f, 0), V(0.16f, 0.02f, 0.1f), 0.008f, deep, mat: Glow, blend: 0.006f);
        b.PaintEll(Body, bc + V(-0.06f, 0.0f, 0.09f), V(0.03f, 0.14f, 0.02f), deep, V(0, 0, 10f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.58f, 0));
            b.Box(arm, V(0.2f * s, 0.54f, 0), V(0.06f, 0.09f, 0.06f), 0.012f, ice, V(0, 0, -20f * s), Glow, 0.008f);
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, V(0.23f * s, 0.46f, 0.02f * i), V(0.26f * s + 0.02f * i, 0.36f, 0.04f * i), 0.02f, deep, mat: Glow, blend: 0.004f);
            b.Spike(arm, V(0.24f * s, 0.6f, 0), V(0.33f * s, 0.66f, -0.02f), 0.03f, ice, 0.6f, Glow);
        });
        int head = b.Head(bc + V(0, 0.18f, 0));
        b.Box(head, bc + V(0, 0.2f, 0.0f), V(0.05f, 0.04f, 0.05f), 0.01f, ice, mat: Glow, blend: 0.008f);
        Braille(b, Body, bc + V(0, 0.07f, 0.091f), V(0, 0, 1f), 0.03f, yellow,
            new[] { (0f, 1f), (0f, 0f), (0f, -1f), (-1f, 0f), (1f, 0f), (-2f, 0f), (2f, 0f) });
        return b;
    }

    /// <summary>Registeel: a golem of dark grey steel, a black plate down its face with seven red dots round a ring, long arms with three-clawed hands.</summary>
    private static PokeBuilder Registeel()
    {
        var b = new PokeBuilder("Registeel", 1f, BodyPlan.Biped, V(0, 0.44f, 0)) { Coat = Metal };
        var steel = Rgb(170, 172, 182);
        var dark = Rgb(56, 56, 64);
        var red = Rgb(230, 80, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.24f, 0));
            b.Limb(leg, V(0.08f * s, 0.26f, 0), V(0.09f * s, 0.06f, 0.01f), 0.06f, 0.055f, steel);
            b.Ell(leg, V(0.09f * s, 0.04f, 0.02f), V(0.065f, 0.04f, 0.07f), steel);
        });
        var bc = V(0, 0.44f, 0);
        var br = V(0.14f, 0.18f, 0.11f);
        b.Ell(Body, bc, br, steel);
        b.Torus(Body, V(0, 0.28f, 0), 0.12f, 0.02f, steel, sz: 0.8f, mat: Metal);
        b.Ell(Body, bc + V(0, 0.14f, 0), V(0.1f, 0.07f, 0.08f), steel, blend: 0.03f);
        b.PaintEll(Body, bc + V(0, 0.04f, 0.09f), V(0.05f, 0.16f, 0.04f), dark);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.52f, 0));
            b.Ell(arm, V(0.16f * s, 0.53f, 0), V(0.06f, 0.06f, 0.06f), steel);
            var hand = V(0.26f * s, 0.28f, 0.04f);
            b.Limb(arm, V(0.18f * s, 0.5f, 0), hand, 0.025f, 0.022f, dark);
            b.Ell(arm, hand, V(0.035f, 0.03f, 0.035f), dark);
            for (int i = -1; i <= 1; i++)
            {
                var tip = hand + V(0.03f * i * s + 0.01f * s, -0.08f, 0.03f * i);
                b.Limb(arm, hand, tip, 0.016f, 0.012f, steel, blend: 0.006f);
                b.PaintEll(arm, Vector3.Lerp(hand, tip, 0.5f), V(0.012f, 0.02f, 0.012f), red);
            }
        });
        int head = b.Head(bc + V(0, 0.18f, 0));
        b.Ell(head, bc + V(0, 0.2f, -0.01f), V(0.06f, 0.04f, 0.06f), steel, blend: 0.02f);
        Braille(b, Body, bc + V(0, 0.07f, 0.112f), V(0, 0, 1f), 0.022f, red,
            new[] { (0f, 0f), (0f, 1.2f), (0f, -1.2f), (1f, 0.6f), (1f, -0.6f), (-1f, 0.6f), (-1f, -0.6f) });
        return b;
    }

    // ------------------------------------------------------------------ Latias and Latios

    /// <summary>
    /// Latias and Latios and their Mega Evolutions: sleek jet-like dragons flying level, white below, red for Latias
    /// and blue for Latios, a triangle on the chest, swept wings and a crest at the back of the head; the Megas
    /// purple, their wings grown long.
    /// </summary>
    private static PokeBuilder LatiBuild(bool latios, bool mega)
    {
        string name = (latios ? "Latios" : "Latias") + (mega ? "-Mega" : "");
        var b = new PokeBuilder(name, 0.95f, BodyPlan.Bird, V(0, 0.38f, 0)) { Coat = Fur }.Hover();
        var main = mega ? Rgb(140, 120, 200) : latios ? Rgb(90, 140, 210) : Rgb(222, 104, 104);
        var white = Rgb(240, 240, 246);
        var mark = latios ? Rgb(220, 70, 80) : Rgb(90, 150, 220);
        var eye = latios ? Rgb(220, 60, 70) : Rgb(240, 200, 60);
        float tilt = latios ? -20f : -35f;
        var c = V(0, 0.38f, 0);
        var r = V(0.085f, 0.095f, 0.22f);
        var turn = V(tilt, 0, 0);
        b.Ell(Body, c, r, main, turn);
        var front = Out(c, r, turn, V(0, 0, 1f));
        var under = Out(c, r, turn, V(0, -1f, 0.2f));
        b.PaintEll(Body, Vector3.Lerp(c, under, 0.6f) + V(0, 0, 0.06f), V(0.09f, 0.07f, 0.16f), white, turn);
        b.Mark(Body, Out(c, r, turn, V(0, -0.3f, 1f)), Vector3.Transform(V(0, -0.3f, 1f), Quaternion.CreateFromYawPitchRoll(0, tilt * Degree, 0)), 0.03f, 0.03f, mark, MarkShape.Triangle, 180f);
        // Swept wings from its back, and short fins at the end of its body
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, c + V(0.06f * s, 0.06f, 0.0f));
            var root = c + V(0.05f * s, 0.06f, 0.02f);
            float span = mega ? 0.44f : 0.34f;
            var tip = root + V(span * s, 0.04f, -0.16f);
            Blade(b, wing, root, tip, mega ? 0.12f : 0.1f, main, V(0, 1f, 0.15f), 0.12f);
            b.PaintEll(wing, Vector3.Lerp(root, tip, 0.25f), V(0.05f, 0.05f, 0.05f), white);
            if (mega) Blade(b, wing, root + V(0, 0, -0.08f), root + V(0.24f * s, 0.12f, -0.3f), 0.07f, main, V(0, 1f, 0.15f), 0.12f);
            int arm = b.Arm(s, c + V(0.06f * s, -0.04f, 0.1f));
            var hand = c + V(0.1f * s, -0.08f, 0.17f);
            b.Limb(arm, c + V(0.06f * s, -0.04f, 0.1f), hand, 0.02f, 0.016f, main);
            Digits(b, arm, hand, V(0, -0.3f, 1f), V(0.5f, 0, 0), 0.03f, 0.008f, white);
        });
        int tail = b.Tail(c + V(0, -0.06f, -0.18f));
        var back = c + V(0, -0.08f, -0.2f);
        PokeBuilder.Both(s => Blade(b, tail, back, back + V(0.08f * s, -0.04f, -0.12f), 0.04f, main, V(0, 1f, 0.2f), 0.2f));
        Blade(b, tail, back, back + V(0, 0.04f, -0.16f), 0.04f, main, V(1f, 0, 0), 0.2f);
        // A head raised in front, white at the jaw, eyes in its sides and a crest of two points behind
        int head = b.Head(front + V(0, 0, -0.04f));
        var hc = front + V(0, 0.1f, 0.03f);
        var hr = V(0.065f, 0.06f, 0.09f);
        b.Limb(head, front + V(0, -0.02f, -0.04f), hc, 0.05f, 0.045f, main);
        b.Ell(head, hc, hr, main);
        b.PaintEll(head, hc + V(0, -0.03f, 0.03f), V(0.055f, 0.03f, 0.07f), white);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, hc + V(0.03f * s, 0.03f, -0.04f), hc + V(0.07f * s, 0.06f, -0.16f), 0.025f, mega ? white : main, V(0, 1f, 0.2f), 0.3f);
            var at = On(hc, hr, 0.035f * s, hc.Y + 0.01f);
            b.Mark(head, at, Outward(hc, hr, at), 0.016f, 0.012f, mark);
            b.Eye(head, at, Outward(hc, hr, at), 0.011f, eye, glare: latios);
        });
        return Lift(b);
    }

    private static PokeBuilder Latias() => LatiBuild(false, false);

    private static PokeBuilder Latios() => LatiBuild(true, false);

    // ------------------------------------------------------------------ Kyogre and Groudon

    /// <summary>
    /// Kyogre and its Primal Reversion: a great blue whale of the deep, white below, red lines ringing marks on its
    /// sides and its huge flat fins, white claws at the fins' edges and a broad fanned tail; the Primal navy, its
    /// lines glowing pale blue and its marks yellow.
    /// </summary>
    private static PokeBuilder KyogreBuild(bool primal)
    {
        var b = new PokeBuilder(primal ? "Kyogre-Primal" : "Kyogre", 1f, BodyPlan.Fish, V(0, 0.3f, 0)) { Coat = Scales }.Hover();
        var blue = primal ? Rgb(50, 70, 150) : Rgb(60, 110, 200);
        var white = Rgb(236, 238, 246);
        var line = primal ? Rgb(170, 230, 250) : Rgb(214, 60, 70);
        var spot = primal ? Rgb(246, 220, 110) : blue;
        var c = V(0, 0.3f, 0);
        var r = V(0.17f, 0.13f, 0.3f);
        b.Ell(Body, c, r, blue);
        b.PaintEll(Body, c + V(0, -0.09f, 0.08f), V(0.14f, 0.06f, 0.22f), white);
        PokeBuilder.Both(s =>
        {
            var n = V(s, 0.2f, 0.1f);
            b.Mark(Body, Out(c, r, default, n), n, 0.06f, 0.05f, line, MarkShape.Ring);
            if (primal) b.Mark(Body, Out(c, r, default, n), n, 0.035f, 0.03f, spot);
            // A huge flat fin, ringed with a line, white claws along its trailing edge
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, c + V(0.14f * s, -0.04f, 0.08f), PokeRole.Fin, s, s);
            var root = c + V(0.12f * s, -0.04f, 0.1f);
            var tip = c + V(0.46f * s, -0.1f, 0.02f);
            Frond(b, fin, root, tip, primal ? 0.14f : 0.12f, blue, V(0, 1f, 0.1f), 0.12f);
            var mid = Vector3.Lerp(root, tip, 0.6f);
            b.Mark(fin, mid + V(0, 0.016f, 0), V(0, 1f, 0.1f), 0.06f, 0.045f, line, MarkShape.Ring);
            if (primal) b.Mark(fin, mid + V(0, 0.016f, 0), V(0, 1f, 0.1f), 0.03f, 0.025f, spot);
            for (int i = 0; i < 4; i++)
            {
                var at = Vector3.Lerp(root, tip, 0.55f + 0.13f * i) + V(0, -0.005f, -0.08f + 0.03f * i);
                b.Spike(fin, at + V(0, 0, 0.03f), at + V(0.01f * s, -0.01f, -0.04f), 0.016f, white, 0.6f, Shell);
            }
        });
        // A broad tail fanned behind it
        int tail = b.Tail(c + V(0, 0.02f, -0.27f));
        for (int i = 0; i < 5; i++)
        {
            float a = (-50f + 25f * i) * Degree;
            Frond(b, tail, c + V(0, 0.02f, -0.26f), c + V(MathF.Sin(a) * 0.2f, 0.04f - MathF.Abs(MathF.Sin(a)) * 0.04f, -0.26f - MathF.Cos(a) * 0.18f), 0.045f, blue, V(0, 1f, 0.1f), 0.2f);
        }
        b.Mark(tail, c + V(0, 0.045f, -0.36f), V(0, 1f, 0), 0.05f, 0.04f, line, MarkShape.Ring);
        // Its head the front of its body: lines over its brow, small red eyes, a mouth along its white chin
        int head = b.Head(c + V(0, 0.06f, 0.22f));
        b.Ell(head, c + V(0, 0.05f, 0.18f), V(0.12f, 0.08f, 0.1f), blue, blend: 0.03f);
        var hc = c + V(0, 0.05f, 0.18f);
        var hr = V(0.12f, 0.08f, 0.1f);
        PokeBuilder.Both(s =>
        {
            b.Mark(head, Out(hc, hr, default, V(0.3f * s, 1f, 0.4f)), V(0.3f * s, 1f, 0.4f), 0.04f, 0.015f, line, MarkShape.Bar, 30f * s);
            var at = Out(hc, hr, default, V(0.55f * s, 0.25f, 0.8f));
            b.Eye(head, at, V(0.55f * s, 0.25f, 0.8f), 0.013f, sclera: true, white: primal ? Rgb(246, 220, 110) : Rgb(240, 240, 250), pupil: Rgb(200, 40, 50), glare: true);
        });
        b.Mark(Body, Out(c, r, default, V(0, -0.15f, 1f)), V(0, -0.15f, 1f), 0.06f, 0.012f, Rgb(40, 50, 80), MarkShape.Smile);
        return Lift(b);
    }

    private static PokeBuilder Kyogre() => KyogreBuild(false);

    /// <summary>
    /// Groudon and its Primal Reversion: a titan of red plate leaning forward on two thick legs, grey beneath, dark
    /// seams between its plates, white claws and a row of white spines down its back and tail; the Primal darker,
    /// its seams glowing yellow and its spines grown.
    /// </summary>
    private static PokeBuilder GroudonBuild(bool primal)
    {
        var b = new PokeBuilder(primal ? "Groudon-Primal" : "Groudon", 1f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Shell };
        var red = primal ? Rgb(196, 50, 50) : Rgb(214, 70, 60);
        var gray = primal ? Rgb(60, 56, 60) : Rgb(120, 116, 116);
        var seam = primal ? Rgb(250, 200, 80) : Rgb(70, 40, 40);
        var white = Rgb(236, 236, 240);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.32f, -0.04f));
            var knee = V(0.15f * s, 0.2f, 0.02f);
            b.Limb(leg, V(0.12f * s, 0.34f, -0.04f), knee, 0.1f, 0.08f, red);
            b.Limb(leg, knee, V(0.15f * s, 0.06f, 0.0f), 0.07f, 0.06f, red);
            b.Ell(leg, V(0.15f * s, 0.04f, 0.04f), V(0.07f, 0.04f, 0.09f), red);
            b.PaintTorus(leg, knee, 0.085f, 0.008f, seam);
            foreach (float x in new[] { -0.04f, 0f, 0.04f })
                b.Spike(leg, V(0.15f * s + x, 0.04f, 0.1f), V(0.15f * s + x * 1.2f, 0.01f, 0.15f), 0.02f, white, mat: Shell, blend: 0.004f);
        });
        var bc = V(0, 0.45f, 0);
        var br = V(0.19f, 0.2f, 0.22f);
        b.Ell(Body, bc, br, red, V(20f, 0, 0));
        b.PaintEll(Body, V(0, 0.38f, 0.12f), V(0.15f, 0.14f, 0.1f), gray, V(20f, 0, 0));
        foreach (float y in new[] { 0.36f, 0.46f, 0.56f })
            b.PaintTorus(Body, V(0, y, 0), 0.18f, 0.007f, seam, V(20f, 0, 0), 1f, 1.1f);
        // Spines down its back and a thick tail
        for (int i = 0; i < (primal ? 6 : 5); i++)
            Blade(b, Body, V(0, 0.62f - i * 0.05f, -0.06f - i * 0.04f), V(0, 0.72f - i * 0.04f, -0.12f - i * 0.05f), 0.04f, primal ? red : white, V(1f, 0, 0), 0.3f, Shell);
        int tail = b.Tail(V(0, 0.36f, -0.18f));
        var tp = Smooth(3, V(0, 0.36f, -0.18f), V(0, 0.2f, -0.34f), V(0.04f, 0.08f, -0.52f));
        b.Tube(tail, tp, 0.1f, 0.03f, red, blend: 0f);
        for (int i = 1; i < tp.Length - 1; i += 2)
            b.Spike(tail, tp[i] + V(0, 0.05f, 0), tp[i] + V(0, 0.12f, -0.04f), 0.025f, primal ? red : white, 0.6f, Shell);
        // Thick arms held forward, plated, with white claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.55f, 0.06f));
            var elbow = V(0.24f * s, 0.46f, 0.08f);
            var hand = V(0.22f * s, 0.34f, 0.18f);
            b.Ell(arm, V(0.19f * s, 0.56f, 0.04f), V(0.08f, 0.08f, 0.08f), red);
            b.Limb(arm, V(0.19f * s, 0.55f, 0.05f), elbow, 0.06f, 0.055f, red);
            b.Limb(arm, elbow, hand, 0.055f, 0.05f, red);
            b.PaintTorus(arm, elbow, 0.058f, 0.008f, seam);
            foreach (float x in new[] { -0.03f, 0f, 0.03f })
                b.Spike(arm, hand + V(x, -0.02f, 0.02f), hand + V(x * 1.4f, -0.08f, 0.06f), 0.016f, white, mat: Shell, blend: 0.004f);
        });
        // A head thrust forward, grey jaw, yellow eyes under a heavy brow
        int head = b.Head(V(0, 0.58f, 0.16f));
        b.Limb(head, V(0, 0.56f, 0.14f), V(0, 0.6f, 0.24f), 0.08f, 0.075f, red);
        var c = V(0, 0.62f, 0.27f);
        var r = V(0.1f, 0.08f, 0.13f);
        b.Ell(head, c, r, red);
        b.Ell(head, c + V(0, -0.04f, 0.03f), V(0.07f, 0.035f, 0.08f), gray, blend: 0.015f);
        b.Mark(head, Out(c, r, default, V(0, 1f, 0.4f)), V(0, 1f, 0.4f), 0.03f, 0.015f, seam, MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.05f * s, 0.03f, -0.04f), c + V(0.09f * s, 0.06f, -0.14f), 0.025f, white, 0.6f, Shell);
            var at = On(c, r, 0.045f * s, c.Y + 0.01f);
            b.Mark(head, at, Outward(c, r, at), 0.018f, 0.012f, Rgb(40, 30, 30));
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(246, 210, 60), glare: true);
        });
        return b;
    }

    private static PokeBuilder Groudon() => GroudonBuild(false);

    // ------------------------------------------------------------------ Rayquaza

    /// <summary>
    /// Rayquaza and its Mega Evolution: a long green sky serpent looped in the air, yellow rings along it, fins down
    /// its length, small clawed arms, a long jaw and long fins swept back from its head; the Mega's green darker, long
    /// yellow streamers flowing from its head along its body.
    /// </summary>
    private static PokeBuilder RayquazaBuild(bool mega)
    {
        var green = mega ? Rgb(40, 110, 80) : Rgb(60, 156, 96);
        var yellow = Rgb(244, 214, 90);
        var dark = mega ? Rgb(40, 44, 50) : Rgb(40, 110, 70);
        var points = Smooth(3, V(0.08f, 0.12f, -0.42f), V(0.16f, 0.24f, -0.28f), V(0.08f, 0.4f, -0.3f), V(-0.1f, 0.46f, -0.16f),
            V(-0.1f, 0.36f, 0.0f), V(0.04f, 0.3f, 0.08f), V(0.06f, 0.44f, 0.16f), V(0, 0.58f, 0.2f));
        var b = new PokeBuilder(mega ? "Rayquaza-Mega" : "Rayquaza", 1f, BodyPlan.Serpent, points[0]) { Coat = Scales }.Hover();
        int neck = Coils(b, points, t => 0.02f + 0.03f * MathF.Min(1f, t * 2.5f), green);
        for (int i = 3; i < points.Length - 2; i += 3)
        {
            int bone = 1 + i / 2;
            var mid = Vector3.Lerp(points[i], points[i + 1], 0.5f);
            float t = (i + 0.5f) / (points.Length - 1);
            b.PaintTorus(bone, mid, 0.02f + 0.03f * MathF.Min(1f, t * 2.5f), 0.006f, yellow, Euler(points[i + 1] - points[i]));
            if (i % 6 == 0)
                PokeBuilder.Both(s => Blade(b, bone, mid, mid + V(0.08f * s, 0.03f, -0.04f), 0.025f, dark, V(0, 1f, 0), 0.3f));
            if (mega && i % 6 == 3)
                b.PaintEll(bone, mid + V(0, 0.04f, 0), V(0.02f, 0.02f, 0.02f), Rgb(240, 120, 70));
        }
        // A tail fin of three points
        int tail = b.Tail(points[0]);
        foreach (var o in new[] { V(0, 0.04f, -0.1f), V(0.06f, -0.03f, -0.08f), V(-0.06f, -0.03f, -0.08f) })
            Blade(b, tail, points[0], points[0] + o, 0.03f, dark, V(0, 1f, 0.3f), 0.3f);
        // Small arms below the neck, three claws on each
        PokeBuilder.Both(s =>
        {
            int arm = b.Part(s < 0 ? "armL" : "armR", neck, points[^4], PokeRole.Arm, s, s);
            var hand = points[^4] + V(0.1f * s, -0.04f, 0.06f);
            b.Limb(arm, points[^4], hand, 0.016f, 0.014f, green);
            Digits(b, arm, hand, V(0.3f * s, -0.4f, 1f), V(0.5f, 0, 0), 0.03f, 0.007f, White, Shell);
        });
        // The head: a long jaw lined yellow, fins swept back, yellow eyes
        int head = b.Head(points[^1], neck);
        var c = points[^1] + V(0, 0.02f, 0.06f);
        var r = V(0.055f, 0.05f, 0.09f);
        b.Ell(head, c, r, green);
        b.Ell(head, c + V(0, -0.02f, 0.08f), V(0.035f, 0.025f, 0.06f), green, blend: 0.02f);
        b.Mark(head, c + V(0, -0.025f, 0.14f), V(0, 0, 1f), 0.02f, 0.006f, yellow, MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.03f * s, 0.03f, -0.02f), c + V(0.06f * s, 0.06f, -0.2f), 0.025f, green, V(0, 1f, 0.2f), 0.3f);
            Blade(b, head, c + V(0.04f * s, -0.01f, -0.02f), c + V(0.12f * s, -0.02f, -0.12f), 0.02f, dark, V(0, 1f, 0.2f), 0.3f);
            var at = On(c, r, 0.035f * s, c.Y + 0.015f);
            b.Mark(head, at, Outward(c, r, at), 0.016f, 0.01f, Rgb(30, 40, 30));
            b.Eye(head, at, Outward(c, r, at), 0.009f, sclera: true, white: yellow, pupil: Rgb(30, 30, 30), glare: true);
            if (mega)
            {
                var start = c + V(0.04f * s, 0.0f, -0.04f);
                Ribbon(b, head, Smooth(2, start, start + V(0.06f * s, 0.02f, -0.12f), start + V(0.1f * s, -0.04f, -0.26f), start + V(0.12f * s, -0.14f, -0.38f)), 0.03f, yellow, V(s, 0.3f, 0), 0.008f);
            }
        });
        return Lift(b);
    }

    private static PokeBuilder Rayquaza() => RayquazaBuild(false);

    // ------------------------------------------------------------------ Jirachi

    /// <summary>Jirachi: a little white star wish-maker floating, a three-pointed yellow star for a head with blue tags at its points, long yellow streamers hanging from its back.</summary>
    private static PokeBuilder Jirachi()
    {
        var b = new PokeBuilder("Jirachi", 0.5f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Fur }.Hover();
        var white = Rgb(244, 244, 248);
        var yellow = Rgb(250, 230, 120);
        var blue = Rgb(110, 196, 200);
        var bc = V(0, 0.2f, 0);
        var br = V(0.06f, 0.07f, 0.055f);
        b.Ell(Body, bc, br, white);
        b.Mark(Body, On(bc, br, 0, 0.18f), V(0, -0.1f, 1f), 0.03f, 0.008f, Rgb(170, 170, 180), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.05f * s, 0.02f, 0));
            b.Limb(arm, bc + V(0.05f * s, 0.02f, 0), bc + V(0.09f * s, -0.02f, 0.03f), 0.016f, 0.014f, white);
            int leg = b.Leg(s, bc + V(0.03f * s, -0.05f, 0));
            b.Ell(leg, bc + V(0.03f * s, -0.07f, 0.01f), V(0.02f, 0.02f, 0.02f), white);
            // A long yellow streamer from its back, hanging down at its side
            int wing = b.Wing(s, bc + V(0.04f * s, 0.03f, -0.04f));
            Ribbon(b, wing, Smooth(2, bc + V(0.04f * s, 0.03f, -0.04f), bc + V(0.11f * s, 0.0f, -0.06f), bc + V(0.15f * s, -0.1f, -0.05f)), 0.04f, yellow, V(0, 0.2f, 1f), 0.008f);
        });
        // The star head: three points, each with a blue tag hanging from it
        int head = b.Head(bc + V(0, 0.07f, 0));
        var c = V(0, 0.36f, 0);
        var r = V(0.09f, 0.08f, 0.075f);
        b.Ell(head, c, r, yellow);
        foreach (float a in new[] { 0f, 120f, -120f })
        {
            var dir = V(MathF.Sin(a * Degree), MathF.Cos(a * Degree), 0);
            var tip = c + dir * 0.16f;
            b.Spike(head, c + dir * 0.04f, tip, 0.05f, yellow, 0.5f);
            b.Box(head, tip - dir * 0.01f + V(0, -0.03f, 0.012f), V(0.012f, 0.03f, 0.005f), 0.004f, blue, V(0, 0, a > 0 ? -20f : a < 0 ? 20f : 0f), blend: 0.004f);
        }
        b.Ell(head, c + V(0, -0.02f, 0.03f), V(0.07f, 0.05f, 0.05f), white, blend: 0.015f);
        var fc = c + V(0, -0.02f, 0.03f);
        var fr = V(0.07f, 0.05f, 0.05f);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.028f * s, fc.Y + 0.005f);
            b.Eye(head, at, Outward(fc, fr, at), 0.013f, Rgb(40, 40, 50));
            b.Mark(head, at + V(0, -0.025f, 0.0f), Outward(fc, fr, at), 0.005f, 0.008f, blue);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Deoxys

    /// <summary>
    /// Deoxys in its four formes: a lean orange alien, a dark core with a purple crystal in its chest, blue stripes
    /// down its legs and a blue visor on its face, its arms two strands of orange and blue twisted together (0); the
    /// Attack Forme's arms split into many thin tentacles and its crest a spike (1); the Defense Forme stout and
    /// rounded, its arms broad bands hanging to the ground (2); the Speed Forme sleek, its arms single strands
    /// trailing back and its crest swept long (3).
    /// </summary>
    private static PokeBuilder DeoxysBuild(int forme)
    {
        string name = forme switch { 1 => "Deoxys-Attack", 2 => "Deoxys-Defense", 3 => "Deoxys-Speed", _ => "Deoxys" };
        var b = new PokeBuilder(name, 1f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Shell };
        var orange = Rgb(226, 110, 70);
        var blue = Rgb(110, 186, 206);
        var dark = Rgb(66, 64, 76);
        var crystal = Rgb(150, 110, 200);
        bool stout = forme == 2;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.42f, 0));
            var knee = V((stout ? 0.1f : 0.09f) * s, 0.24f, 0.04f);
            var foot = V((stout ? 0.11f : 0.1f) * s, 0.02f, 0.0f);
            b.Limb(leg, V(0.06f * s, 0.44f, 0), knee, stout ? 0.06f : 0.045f, stout ? 0.05f : 0.035f, orange);
            b.Limb(leg, knee, foot, stout ? 0.05f : 0.03f, stout ? 0.045f : 0.012f, forme == 3 ? dark : orange);
            b.PaintEll(leg, Vector3.Lerp(knee, foot, 0.4f) + V(0.02f * s, 0, -0.02f), V(0.015f, 0.12f, 0.015f), blue);
            if (stout) b.Ell(leg, foot + V(0, 0.01f, 0.02f), V(0.05f, 0.025f, 0.06f), orange);
            if (forme == 1) Blade(b, leg, Vector3.Lerp(knee, foot, 0.3f), Vector3.Lerp(knee, foot, 0.3f) + V(0.06f * s, -0.12f, -0.02f), 0.025f, blue, V(0, 0, 1f), 0.3f);
        });
        // A narrow waist, a dark core and orange plates over the chest and shoulders, the crystal in the middle
        var bc = V(0, 0.52f, 0);
        b.Ell(Body, bc, stout ? V(0.1f, 0.12f, 0.08f) : V(0.06f, 0.11f, 0.05f), dark);
        b.Ell(Body, V(0, 0.6f, 0.0f), stout ? V(0.13f, 0.08f, 0.1f) : V(0.1f, 0.06f, 0.07f), orange, blend: 0.02f);
        b.Ell(Body, V(0, 0.58f, stout ? 0.09f : 0.065f), V(0.025f, 0.025f, 0.015f), crystal, mat: Glow, blend: 0.005f);
        b.PaintTorus(Body, V(0, 0.58f, stout ? 0.095f : 0.07f), 0.03f, 0.006f, dark, V(90f, 0, 0));
        // The arms, as the forme has them
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V((stout ? 0.12f : 0.09f) * s, 0.62f, 0));
            var sh = V((stout ? 0.12f : 0.09f) * s, 0.62f, 0);
            switch (forme)
            {
                case 1:
                    for (int k = 0; k < 3; k++)
                    {
                        var end = V((0.2f + 0.06f * k) * s, 0.3f - 0.08f * k, 0.06f - 0.06f * k);
                        b.Tube(arm, Smooth(2, sh, sh + V(0.08f * s, -0.02f, 0.02f - 0.03f * k), end), 0.016f, 0.006f, k == 1 ? blue : orange, blend: 0f);
                    }
                    break;
                case 2:
                    var band = Smooth(3, sh, sh + V(0.06f * s, 0.0f, 0.0f), sh + V(0.1f * s, -0.2f, 0.02f), V(0.2f * s, 0.06f, 0.03f));
                    b.Tube(arm, band, 0.04f, 0.05f, orange, blend: 0f);
                    b.PaintEll(arm, band[band.Length / 2] + V(0.02f * s, 0, 0.03f), V(0.02f, 0.2f, 0.02f), blue);
                    b.Mark(arm, band[^3] + V(0, 0, 0.045f), V(0, 0, 1f), 0.016f, 0.016f, blue);
                    break;
                case 3:
                    b.Tube(arm, Smooth(3, sh, sh + V(0.08f * s, -0.04f, -0.02f), sh + V(0.16f * s, -0.2f, -0.14f), sh + V(0.18f * s, -0.36f, -0.26f)), 0.022f, 0.008f, orange, blend: 0f);
                    break;
                default:
                    // Two strands twisted round each other, one orange and one blue
                    var path = Smooth(3, sh, sh + V(0.08f * s, -0.04f, 0.02f), sh + V(0.14f * s, -0.2f, 0.04f), sh + V(0.16f * s, -0.38f, 0.02f), sh + V(0.24f * s, -0.48f, 0.0f));
                    for (int k = 0; k < 2; k++)
                    {
                        var strand = path.Select((p, i) => p + V(MathF.Cos(i * 0.9f + k * MathF.PI) * 0.018f, 0, MathF.Sin(i * 0.9f + k * MathF.PI) * 0.018f)).ToArray();
                        b.Tube(arm, strand, 0.018f, 0.012f, k == 0 ? orange : blue, blend: 0f);
                    }
                    b.Limb(arm, path[0], path[2], 0.02f, 0.016f, orange);
                    break;
            }
        });
        // A head with a blue visor over its eyes and a crest as the forme has it
        int head = b.Head(V(0, 0.66f, 0.01f));
        var c = V(0, 0.73f, 0.02f);
        var r = stout ? V(0.075f, 0.075f, 0.075f) : V(0.06f, 0.065f, 0.065f);
        b.Ell(head, c, r, orange);
        b.Limb(head, V(0, 0.64f, 0), c, 0.025f, 0.03f, dark);
        switch (forme)
        {
            case 1:
                b.Spike(head, c + V(0, 0.03f, -0.02f), c + V(0, 0.2f, -0.06f), 0.035f, orange, 0.5f);
                break;
            case 2:
                break;
            case 3:
                b.Spike(head, c + V(0, 0.03f, -0.03f), c + V(0, 0.1f, -0.24f), 0.04f, orange, 0.5f);
                break;
            default:
                b.Box(head, c + V(0, 0.07f, -0.01f), V(0.07f, 0.02f, 0.03f), 0.012f, orange, blend: 0.01f);
                break;
        }
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.03f * s, 0.0f, 0.05f), V(0.02f, 0.05f, 0.03f), blue);
            var at = On(c, r, 0.03f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(30, 40, 50), glare: true);
        });
        return b;
    }

    private static PokeBuilder Deoxys() => DeoxysBuild(0);
}
