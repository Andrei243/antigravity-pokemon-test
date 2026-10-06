using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Unova's second batch in National Pokédex
// order: Timburr (532) to Zoroark (571). Their forms are in PokemonModels.Megas.cs, PokemonModels.Regional.cs and
// PokemonModels.Gigantamax.cs with the other forms. Helpers shared with the earlier batches are in
// PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs, PokemonModels.Kanto1.cs to PokemonModels.Kanto3.cs,
// PokemonModels.Johto1.cs, PokemonModels.Johto2.cs, PokemonModels.Hoenn1.cs to PokemonModels.Hoenn3.cs and
// PokemonModels.Unova1.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Timburr line

    /// <summary>Timburr: a small gray fighter, pink veins bulging on its shoulders and legs, a dark bulb of a nose, a crest swept back and a wooden log carried in both hands.</summary>
    private static PokeBuilder Timburr()
    {
        var b = new PokeBuilder("Timburr", 0.55f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        var gray = Rgb(180, 172, 160);
        var pink = Rgb(236, 124, 146);
        var wood = Rgb(128, 82, 52);
        var cut = Rgb(214, 172, 120);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.08f, 0));
            b.Limb(leg, V(0.04f * s, 0.09f, 0), V(0.05f * s, 0.025f, 0.01f), 0.022f, 0.018f, gray);
            b.Ell(leg, V(0.05f * s, 0.016f, 0.022f), V(0.026f, 0.016f, 0.036f), gray);
            b.PaintEll(leg, V(0.046f * s, 0.06f, 0.012f), V(0.024f, 0.012f, 0.024f), pink);
        });
        var bc = V(0, 0.15f, 0);
        var br = V(0.055f, 0.07f, 0.048f);
        b.Ell(Body, bc, br, gray);
        // Arms held low in front, both hands on the log across them
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.05f * s, 0.19f, 0);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.075f * s, 0.13f, 0.07f);
            b.Limb(arm, shoulder, hand, 0.02f, 0.016f, gray);
            b.Ell(arm, hand, V(0.019f, 0.019f, 0.019f), gray);
            b.PaintEll(arm, shoulder + V(0.012f * s, 0.008f, 0), V(0.022f, 0.016f, 0.022f), pink);
            if (s > 0)
            {
                b.Limb(arm, V(-0.16f, 0.13f, 0.088f), V(0.15f, 0.13f, 0.088f), 0.026f, 0.026f, wood, Shell);
                foreach (float x in new[] { -0.18f, 0.17f })
                    b.PaintEll(arm, V(x, 0.13f, 0.088f), V(0.012f, 0.03f, 0.03f), cut, soft: 0.006f);
            }
        });
        int head = b.Head(V(0, 0.21f, 0.01f));
        var c = V(0, 0.26f, 0.015f);
        var r = V(0.062f, 0.056f, 0.052f);
        b.Ell(head, c, r, gray);
        b.Spike(head, c + V(0, 0.03f, -0.005f), c + V(0, 0.06f, -0.125f), 0.045f, gray, 0.8f, blend: 0.025f);
        PokeBuilder.Both(s => b.PaintEll(head, c + V(0.05f * s, 0.025f, -0.005f), V(0.014f, 0.026f, 0.02f), pink));
        b.Ell(head, c + V(0, -0.018f, 0.052f), V(0.022f, 0.018f, 0.016f), Rgb(70, 66, 72), blend: 0.01f);
        b.Mark(head, On(c, r, 0.01f, c.Y - 0.04f), V(0.1f, -0.5f, 1f), 0.018f, 0.008f, White, MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.027f * s, c.Y + 0.014f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, sclera: true, pupil: Rgb(40, 36, 40));
        });
        return b;
    }

    /// <summary>Gurdurr: a muscled gray fighter, pink muscle bulging on its chest, arms and thighs, a red bulb of a nose and a steel beam held over its head.</summary>
    private static PokeBuilder Gurdurr()
    {
        var b = new PokeBuilder("Gurdurr", 0.85f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var gray = Rgb(184, 176, 166);
        var pink = Rgb(230, 130, 176);
        var beam = Rgb(130, 40, 56);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.17f, 0));
            b.Limb(leg, V(0.065f * s, 0.18f, 0), V(0.08f * s, 0.04f, 0.01f), 0.055f, 0.042f, gray);
            b.Ell(leg, V(0.085f * s, 0.024f, 0.03f), V(0.042f, 0.024f, 0.056f), gray);
            b.PaintEll(leg, V(0.068f * s, 0.12f, 0.02f), V(0.04f, 0.035f, 0.035f), pink);
        });
        // An hourglass body: a narrow waist under a broad chest of pink muscle
        b.Ell(Body, V(0, 0.2f, 0), V(0.075f, 0.06f, 0.06f), gray);
        b.Ell(Body, V(0, 0.32f, 0), V(0.11f, 0.1f, 0.08f), gray);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.05f * s, 0.33f, 0.06f), V(0.045f, 0.04f, 0.035f), pink));
        // Both arms up, a steel I-beam held over its head
        int armR = b.Arm(1f, V(0.1f, 0.38f, 0));
        int armL = b.Arm(-1f, V(-0.1f, 0.38f, 0));
        b.Box(armR, V(0, 0.68f, 0.02f), V(0.3f, 0.035f, 0.04f), 0.004f, beam, mat: Metal);
        foreach (float z in new[] { -0.02f, 0.06f })
            b.CutBox(armR, V(0, 0.68f, z), V(0.31f, 0.02f, 0.018f), Quaternion.Identity);
        foreach (var (arm, s) in new[] { (armL, -1f), (armR, 1f) })
        {
            var shoulder = V(0.1f * s, 0.38f, 0);
            var elbow = V(0.19f * s, 0.5f, 0);
            var hand = V(0.14f * s, 0.625f, 0.02f);
            b.Limb(arm, shoulder, elbow, 0.045f, 0.035f, gray);
            b.PaintEll(arm, Vector3.Lerp(shoulder, elbow, 0.45f) + V(0, 0, 0.02f), V(0.04f, 0.045f, 0.035f), pink, Euler(elbow - shoulder));
            b.Limb(arm, elbow, hand, 0.033f, 0.028f, gray);
            b.Ell(arm, hand, V(0.032f, 0.03f, 0.032f), gray);
        }
        int head = b.Head(V(0, 0.42f, 0.03f));
        var c = V(0, 0.475f, 0.04f);
        var r = V(0.075f, 0.068f, 0.068f);
        b.Ell(head, c, r, gray);
        b.Ell(head, c + V(0, -0.02f, 0.06f), V(0.026f, 0.024f, 0.02f), Rgb(200, 50, 60), blend: 0.01f);
        PokeBuilder.Both(s => b.PaintEll(head, c + V(0.05f * s, 0.03f, 0), V(0.015f, 0.028f, 0.02f), pink));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.027f * s, c.Y + 0.016f);
            b.Mark(head, at + V(0, 0.016f, 0), Outward(c, r, at), 0.016f, 0.005f, Rgb(80, 74, 70), MarkShape.Bar, -20f * s);
            b.Eye(head, at, Outward(c, r, at), 0.013f, sclera: true, pupil: Rgb(40, 36, 40), glare: true);
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.042f), V(0, -0.5f, 1f), 0.016f, 0.007f, White, MarkShape.Smile);
        return b;
    }

    /// <summary>Conkeldurr: a hunched old brown fighter, a red bulb of a nose over a gray beard, pink veins coiled round its great arms and a concrete pillar in each hand for a cane.</summary>
    private static PokeBuilder Conkeldurr()
    {
        var b = new PokeBuilder("Conkeldurr", 1f, BodyPlan.Biped, V(0, 0.34f, 0)) { Coat = Fur };
        var brown = Rgb(150, 112, 84);
        var pink = Rgb(216, 120, 170);
        var stone = Rgb(170, 170, 176);
        var gray = Rgb(184, 184, 188);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.18f, 0));
            b.Limb(leg, V(0.08f * s, 0.19f, 0), V(0.1f * s, 0.04f, 0.02f), 0.05f, 0.035f, brown);
            b.Ell(leg, V(0.105f * s, 0.024f, 0.04f), V(0.04f, 0.024f, 0.055f), brown);
            b.PaintTorus(leg, V(0.09f * s, 0.12f, 0.01f), 0.045f, 0.008f, pink);
        });
        b.Ell(Body, V(0, 0.34f, 0.02f), V(0.14f, 0.15f, 0.11f), brown, V(15f, 0, 0));
        b.PaintEll(Body, V(0, 0.3f, 0.09f), V(0.09f, 0.08f, 0.05f), Rgb(172, 134, 102));
        // Long arms out to the tops of the two concrete pillars standing beside it
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.13f * s, 0.42f, 0.04f);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.24f * s, 0.34f, 0.08f);
            var hand = V(0.29f * s, 0.43f, 0.12f);
            b.Limb(arm, shoulder, elbow, 0.06f, 0.05f, brown);
            b.Limb(arm, elbow, hand, 0.05f, 0.042f, brown);
            foreach (float t in new[] { 0.3f, 0.65f })
                b.PaintTorus(arm, Vector3.Lerp(shoulder, elbow, t), 0.058f, 0.008f, pink, Euler(elbow - shoulder));
            b.PaintTorus(arm, Vector3.Lerp(elbow, hand, 0.5f), 0.047f, 0.008f, pink, Euler(hand - elbow));
            b.Ell(arm, hand, V(0.045f, 0.035f, 0.045f), brown);
            b.Box(arm, V(0.29f * s, 0.2f, 0.12f), V(0.05f, 0.2f, 0.055f), 0.01f, stone, mat: Shell);
            b.PaintEll(arm, V(0.29f * s, 0.24f, 0.175f), V(0.01f, 0.06f, 0.01f), Rgb(140, 140, 146), soft: 0.006f);
        });
        int head = b.Head(V(0, 0.46f, 0.08f));
        var c = V(0, 0.52f, 0.1f);
        var r = V(0.075f, 0.065f, 0.07f);
        b.Ell(head, c, r, brown);
        b.Spike(head, c + V(0, 0.04f, -0.02f), c + V(0, 0.1f, -0.1f), 0.04f, brown, 0.6f);
        b.Ell(head, c + V(0, -0.015f, 0.075f), V(0.03f, 0.028f, 0.024f), Rgb(204, 56, 64), blend: 0.01f);
        // A gray moustache and beard under the nose
        PokeBuilder.Both(s => b.Ell(head, c + V(0.03f * s, -0.042f, 0.06f), V(0.035f, 0.018f, 0.02f), gray, V(0, 0, 20f * s), blend: 0.008f));
        b.Ell(head, c + V(0, -0.062f, 0.05f), V(0.03f, 0.03f, 0.022f), gray, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.06f * s, 0.03f, 0.01f), V(0.015f, 0.03f, 0.025f), pink);
            var at = On(c, r, 0.034f * s, c.Y + 0.026f);
            b.Mark(head, at + V(0, 0.016f, 0), Outward(c, r, at), 0.02f, 0.006f, Rgb(70, 52, 44), MarkShape.Bar, -15f * s);
            b.Eye(head, at, Outward(c, r, at), 0.013f, sclera: true, pupil: Rgb(40, 36, 40), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Tympole line

    /// <summary>Tympole: a round tadpole, a cream face in a black hood, a blue ball on each side like a headphone and a broad blue tail.</summary>
    private static PokeBuilder Tympole()
    {
        var b = new PokeBuilder("Tympole", 0.5f, BodyPlan.Fish, V(0, 0.22f, 0)) { Coat = Fur }.Hover();
        var black = Rgb(50, 50, 58);
        var cream = Rgb(244, 220, 170);
        var blue = Rgb(90, 180, 230);
        var c = V(0, 0.22f, 0);
        var r = V(0.085f, 0.08f, 0.075f);
        b.Ell(Body, c, r, black);
        b.PaintEll(Body, c + V(0, -0.012f, 0.05f), V(0.075f, 0.068f, 0.05f), cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(Body, s, c + V(0.08f * s, 0, 0));
            b.Ell(ear, c + V(0.085f * s, 0, -0.005f), V(0.025f, 0.04f, 0.04f), black, blend: 0.01f);
            b.Ell(ear, c + V(0.105f * s, 0, -0.005f), V(0.016f, 0.034f, 0.034f), blue, mat: Shell, blend: 0.006f);
        });
        // A broad paddle of a tail hanging down behind
        int tail = b.Tail(c + V(0, -0.06f, -0.03f));
        var root = c + V(0, -0.06f, -0.03f);
        var tip = c + V(0, -0.2f, -0.12f);
        b.Limb(tail, root, Vector3.Lerp(root, tip, 0.3f), 0.022f, 0.016f, black);
        b.Ell(tail, Vector3.Lerp(root, tip, 0.62f), V(0.04f, 0.07f, 0.014f), blue, Euler(tip - root), Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(Body, at, Outward(c, r, at), 0.02f, Rgb(50, 40, 40));
            b.Mark(Body, at + V(0.002f * s, 0.032f, -0.006f), Outward(c, r, at), 0.012f, 0.004f, Rgb(70, 60, 50), MarkShape.Bar, 20f * s);
        });
        b.Mark(Body, On(c, r, 0, c.Y - 0.035f), V(0, -0.4f, 1f), 0.016f, 0.008f, Rgb(80, 60, 50), MarkShape.Smile);
        return Lift(b);
    }

    /// <summary>Palpitoad: a round blue tadpole-frog, a cream belly ringed in black, a black cap topped by a teal dome, teal bumps on its sides and a pale stub of a tail.</summary>
    private static PokeBuilder Palpitoad()
    {
        var b = new PokeBuilder("Palpitoad", 0.6f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Fur };
        var blue = Rgb(50, 110, 180);
        var black = Rgb(48, 50, 60);
        var teal = Rgb(110, 210, 214);
        var cream = Rgb(232, 210, 170);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.06f, 0.01f));
            b.Limb(leg, V(0.06f * s, 0.06f, 0.01f), V(0.07f * s, 0.02f, 0.03f), 0.025f, 0.02f, blue);
            b.Ell(leg, V(0.075f * s, 0.014f, 0.045f), V(0.03f, 0.014f, 0.035f), blue);
            Digits(b, leg, V(0.075f * s, 0.012f, 0.07f), V(0, 0, 1f), V(0.012f, 0, 0), 0.012f, 0.008f, blue);
        });
        var bc = V(0, 0.18f, 0);
        var br = V(0.11f, 0.15f, 0.09f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, V(0, 0.15f, 0.06f), V(0.08f, 0.11f, 0.05f), black);
        b.PaintEll(Body, V(0, 0.145f, 0.07f), V(0.065f, 0.095f, 0.05f), cream);
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, V(0.1f * s, 0.25f, 0), V(0.03f, 0.034f, 0.034f), black, blend: 0.01f);
            b.Ell(Body, V(0.122f * s, 0.25f, 0), V(0.016f, 0.028f, 0.028f), teal, mat: Shell, blend: 0.006f);
            b.Ell(Body, V(0.075f * s, 0.1f, 0.06f), V(0.022f, 0.02f, 0.02f), teal, mat: Shell, blend: 0.008f);
        });
        int tail = b.Tail(V(0, 0.1f, -0.07f));
        b.Limb(tail, V(0, 0.1f, -0.06f), V(0, 0.06f, -0.15f), 0.03f, 0.018f, Rgb(204, 204, 214));
        int head = b.Head(V(0, 0.28f, 0));
        b.Ell(head, V(0, 0.31f, -0.005f), V(0.085f, 0.05f, 0.075f), black);
        b.Ell(head, V(0, 0.36f, 0), V(0.045f, 0.032f, 0.045f), teal, mat: Shell, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.038f * s, 0.255f);
            b.Eye(Body, at, Outward(bc, br, at), 0.018f, sclera: true, pupil: Rgb(30, 30, 36), glare: true);
        });
        b.Mark(Body, On(bc, br, 0, 0.215f), V(0, -0.2f, 1f), 0.022f, 0.01f, Rgb(70, 40, 50), MarkShape.Smile);
        return b;
    }

    /// <summary>Seismitoad: a great blue toad, a teal belly ringed in black, teal bumps ringed in black over its eyes and on its shoulders and arms, red eyes and broad hands and feet.</summary>
    private static PokeBuilder Seismitoad()
    {
        var b = new PokeBuilder("Seismitoad", 1f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var blue = Rgb(50, 120, 190);
        var dark = Rgb(36, 80, 140);
        var black = Rgb(48, 50, 60);
        var teal = Rgb(110, 210, 214);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.16f, 0));
            b.Limb(leg, V(0.1f * s, 0.17f, 0), V(0.13f * s, 0.05f, 0.03f), 0.06f, 0.045f, blue);
            b.Ell(leg, V(0.14f * s, 0.022f, 0.06f), V(0.05f, 0.022f, 0.06f), dark);
            Digits(b, leg, V(0.14f * s, 0.02f, 0.11f), V(0, 0, 1f), V(0.022f, 0, 0), 0.025f, 0.013f, dark);
        });
        b.Ell(Body, V(0, 0.32f, 0), V(0.17f, 0.19f, 0.14f), blue);
        b.PaintEll(Body, V(0, 0.3f, 0.09f), V(0.12f, 0.15f, 0.08f), black);
        b.PaintEll(Body, V(0, 0.3f, 0.1f), V(0.105f, 0.135f, 0.075f), teal);
        // Teal bumps ringed in black on its shoulders and forearms
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.15f * s, 0.42f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.25f * s, 0.3f, 0.04f);
            var hand = V(0.27f * s, 0.12f, 0.08f);
            b.Limb(arm, shoulder, elbow, 0.06f, 0.05f, blue);
            b.Limb(arm, elbow, hand, 0.05f, 0.045f, blue);
            b.Ell(arm, hand, V(0.05f, 0.04f, 0.05f), dark);
            Digits(b, arm, hand + V(0, -0.02f, 0.03f), V(0, -0.5f, 1f), V(0.022f, 0, 0), 0.03f, 0.014f, dark);
            foreach (var (at, size) in new[] { (shoulder + V(0.04f * s, 0.04f, 0), 0.045f), (Vector3.Lerp(elbow, hand, 0.55f) + V(0.04f * s, 0, 0), 0.035f) })
            {
                b.Ell(arm, at, V(size, size, size), black, blend: 0.01f);
                b.Ell(arm, at + V(0.022f * s, 0.005f, 0), V(size * 0.7f, size * 0.8f, size * 0.8f), teal, mat: Shell, blend: 0.006f);
            }
        });
        // A broad head, two great bumps over the eyes and a teal lip
        int head = b.Head(V(0, 0.46f, 0.05f));
        var c = V(0, 0.52f, 0.07f);
        var r = V(0.13f, 0.07f, 0.1f);
        b.Ell(head, c, r, blue);
        b.PaintEll(head, c + V(0, -0.045f, 0.06f), V(0.1f, 0.03f, 0.05f), teal);
        PokeBuilder.Both(s =>
        {
            var bump = c + V(0.065f * s, 0.065f, -0.005f);
            b.Ell(head, bump, V(0.05f, 0.04f, 0.05f), black, blend: 0.012f);
            b.Ell(head, bump + V(0, 0.025f, 0), V(0.04f, 0.025f, 0.04f), teal, mat: Shell, blend: 0.006f);
            var at = On(c, r, 0.07f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.021f, Rgb(200, 50, 50), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Throh and Sawk

    /// <summary>Throh: a stout red judo fighter in a white gi with a black belt, a square red head with black brows and a frown, red hands and feet.</summary>
    private static PokeBuilder Throh()
    {
        var b = new PokeBuilder("Throh", 0.95f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var red = Rgb(210, 80, 70);
        var gi = Rgb(236, 236, 230);
        var black = Rgb(50, 48, 52);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.2f, 0));
            b.Limb(leg, V(0.08f * s, 0.21f, 0), V(0.1f * s, 0.06f, 0.01f), 0.06f, 0.055f, gi);
            b.PaintEll(leg, V(0.095f * s, 0.11f, 0.05f), V(0.03f, 0.03f, 0.02f), Rgb(110, 110, 112));
            b.Ell(leg, V(0.105f * s, 0.022f, 0.035f), V(0.04f, 0.022f, 0.055f), red, blend: 0.006f);
        });
        b.Ell(Body, V(0, 0.32f, 0), V(0.16f, 0.16f, 0.12f), gi);
        b.PaintTorus(Body, V(0, 0.25f, 0), 0.155f, 0.018f, black, sz: 0.76f);
        b.Ell(Body, V(0, 0.25f, 0.115f), V(0.022f, 0.018f, 0.012f), black, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            b.Limb(Body, V(0.01f * s, 0.24f, 0.118f), V(0.03f * s, 0.18f, 0.11f), 0.008f, 0.007f, black, blend: 0.004f);
            b.PaintEll(Body, V(0.035f * s, 0.36f, 0.1f), V(0.012f, 0.09f, 0.04f), black, V(0, 0, 28f * s), 0.006f);
        });
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.14f * s, 0.4f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.22f * s, 0.32f, 0.04f);
            var hand = V(0.25f * s, 0.24f, 0.1f);
            b.Limb(arm, shoulder, elbow, 0.06f, 0.05f, gi);
            b.Limb(arm, elbow, hand, 0.04f, 0.035f, red, blend: 0.006f);
            b.Ell(arm, hand, V(0.04f, 0.04f, 0.04f), red);
            Digits(b, arm, hand + V(0.01f * s, -0.01f, 0.03f), V(0, -0.3f, 1f), V(0.018f, 0, 0), 0.025f, 0.012f, red);
        });
        // A square head sunk into the collar
        int head = b.Head(V(0, 0.44f, 0.03f));
        var c = V(0, 0.5f, 0.05f);
        b.Box(head, c, V(0.07f, 0.065f, 0.065f), 0.035f, red, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = c + V(0.028f * s, 0.004f, 0.065f);
            b.Mark(head, at + V(0, 0.02f, 0), V(0, 0, 1f), 0.022f, 0.007f, black, MarkShape.Bar, -18f * s);
            b.Eye(head, at, V(0.15f * s, 0, 1f), 0.012f, sclera: true, pupil: Rgb(30, 30, 30), glare: true);
        });
        b.Mark(head, c + V(0, -0.035f, 0.065f), V(0, 0, 1f), 0.02f, 0.008f, black, MarkShape.Smile, 180f);
        return b;
    }

    /// <summary>Sawk: a lean blue karate fighter in a pale blue gi with a black belt, a tall head ridged to a point behind, black brows and blue fists raised.</summary>
    private static PokeBuilder Sawk()
    {
        var b = new PokeBuilder("Sawk", 0.95f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var blue = Rgb(60, 110, 190);
        var gi = Rgb(170, 220, 230);
        var black = Rgb(50, 48, 52);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.24f, 0));
            b.Limb(leg, V(0.06f * s, 0.25f, 0), V(0.09f * s, 0.06f, 0.02f), 0.045f, 0.04f, gi);
            b.PaintEll(leg, V(0.08f * s, 0.12f, 0.045f), V(0.025f, 0.035f, 0.02f), Rgb(60, 60, 70));
            b.Ell(leg, V(0.095f * s, 0.022f, 0.04f), V(0.03f, 0.022f, 0.05f), blue, blend: 0.006f);
        });
        b.Ell(Body, V(0, 0.38f, 0), V(0.11f, 0.15f, 0.085f), gi);
        b.PaintTorus(Body, V(0, 0.29f, 0), 0.1f, 0.016f, black, sz: 0.78f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.03f * s, 0.42f, 0.075f), V(0.01f, 0.09f, 0.03f), black, V(0, 0, 25f * s), 0.006f));
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.1f * s, 0.46f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.17f * s, 0.38f, 0.05f);
            var hand = s > 0 ? V(0.13f * s, 0.5f, 0.12f) : V(0.18f * s, 0.3f, 0.1f);
            b.Limb(arm, shoulder, elbow, 0.045f, 0.035f, gi);
            b.Limb(arm, elbow, hand, 0.03f, 0.028f, blue, blend: 0.006f);
            b.Ell(arm, hand, V(0.035f, 0.035f, 0.035f), blue);
        });
        int head = b.Head(V(0, 0.52f, 0.02f));
        var c = V(0, 0.59f, 0.04f);
        var r = V(0.055f, 0.075f, 0.06f);
        b.Ell(head, c, r, blue);
        b.Spike(head, c + V(0, 0.03f, -0.02f), c + V(0, 0.12f, -0.06f), 0.04f, blue, 0.7f);
        b.PaintEll(head, c + V(0, 0.02f, 0.06f), V(0.005f, 0.08f, 0.03f), Rgb(40, 70, 130), soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.024f * s, c.Y);
            b.Mark(head, at + V(0, 0.018f, 0), Outward(c, r, at), 0.02f, 0.006f, black, MarkShape.Bar, -20f * s);
            b.Eye(head, at, Outward(c, r, at), 0.011f, sclera: true, pupil: Rgb(30, 30, 30), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Sewaddle line

    /// <summary>Sewaddle: a little yellow caterpillar in a hood made of a bitten leaf, an orange beak over a pale chin, round black eyes and a green body on orange feet.</summary>
    private static PokeBuilder Sewaddle()
    {
        var path = new[] { V(0, 0.045f, -0.15f), V(0, 0.05f, -0.09f), V(0, 0.065f, -0.035f), V(0, 0.1f, 0.0f) };
        var b = new PokeBuilder("Sewaddle", 0.45f, BodyPlan.Serpent, path[0]) { Coat = Fur };
        var green = Rgb(140, 190, 70);
        var light = Rgb(190, 220, 110);
        var orange = Rgb(240, 150, 60);
        var yellow = Rgb(246, 210, 100);
        var leaf = Rgb(100, 170, 70);
        int neck = Grub(b, path, 0.04f, 0.05f, green, light, (i, seg, at, r) =>
            PokeBuilder.Both(s => b.Ell(seg, at + V(r * 0.7f * s, -r * 0.65f, r * 0.2f), V(r * 0.3f, r * 0.3f, r * 0.3f), orange, blend: 0.008f)));
        int head = b.Head(path[^1] + V(0, 0.03f, 0.01f), neck);
        var c = V(0, 0.17f, 0.03f);
        var r = V(0.065f, 0.06f, 0.06f);
        // The leaf hood wraps the head from the brow back, open at the face and bitten at one edge
        b.Ell(head, c + V(0, 0.02f, -0.025f), V(0.085f, 0.08f, 0.075f), leaf, mat: Leaf);
        b.Cut(head, c + V(0, -0.01f, 0.07f), V(0.07f, 0.065f, 0.06f));
        b.Cut(head, c + V(0.06f, 0.09f, -0.04f), V(0.025f, 0.025f, 0.03f));
        b.Ell(head, c, r, yellow);
        b.Ell(head, c + V(0, 0.05f, 0.05f), V(0.02f, 0.018f, 0.018f), yellow, blend: 0.01f);
        b.Ell(head, c + V(0, -0.025f, 0.055f), V(0.026f, 0.016f, 0.02f), orange, blend: 0.008f);
        b.Ell(head, c + V(0, -0.045f, 0.04f), V(0.035f, 0.022f, 0.025f), Rgb(200, 200, 196), blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.008f);
            b.Mark(head, at, Outward(c, r, at), 0.016f, 0.016f, White);
            b.Eye(head, at, Outward(c, r, at), 0.012f);
        });
        return b;
    }

    /// <summary>Swadloon: a sulky yellow face peeping out of a round cloak of green leaves it has wrapped about itself, two leaves standing up like ears and leaves spread round its foot.</summary>
    private static PokeBuilder Swadloon()
    {
        var b = new PokeBuilder("Swadloon", 0.5f, BodyPlan.Floating, V(0, 0.14f, 0)) { Coat = Leaf };
        var green = Rgb(120, 180, 70);
        var dark = Rgb(80, 140, 60);
        var yellow = Rgb(246, 210, 100);
        var c = V(0, 0.14f, 0);
        var r = V(0.1f, 0.12f, 0.09f);
        b.Ell(Body, c, r, green);
        b.Cut(Body, c + V(0, 0.04f, 0.09f), V(0.05f, 0.04f, 0.035f));
        foreach (float x in new[] { -0.04f, 0.04f })
            b.PaintEll(Body, c + V(x, -0.03f, 0.08f), V(0.006f, 0.09f, 0.03f), dark, V(0, 0, -x * 300f), 0.006f);
        int head = b.Head(c + V(0, 0.04f, 0.04f));
        var fc = c + V(0, 0.035f, 0.05f);
        var fr = V(0.05f, 0.04f, 0.035f);
        b.Ell(head, fc, fr, yellow, mat: Fur);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.022f * s, fc.Y + 0.006f);
            b.Eye(head, at, Outward(fc, fr, at), 0.01f, glare: true);
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.11f, 0));
            Frond(b, ear, c + V(0.025f * s, 0.1f, -0.01f), c + V(0.06f * s, 0.22f, 0), 0.04f, green, V(0, 0, 1f), 0.2f, Leaf);
        });
        b.Mark(head, On(fc, fr, 0, fc.Y - 0.022f), V(0, -0.3f, 1f), 0.012f, 0.005f, Rgb(80, 60, 40), MarkShape.Smile, 180f);
        for (int i = 0; i < 5; i++)
        {
            float a = (i - 2) * 0.75f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, c + V(0, -0.1f, 0) + dir * 0.06f, c + V(0, -0.135f, 0) + dir * 0.17f, 0.045f, dark, V(0, 1f, 0), 0.15f, Leaf);
        }
        return b;
    }

    /// <summary>Leavanny: a slender yellow and green mantis that sews leaves, its yellow face framed in a hood of leaves with two yellow points, red eyes, leaf blades on its arms and a skirt of leaves over thin legs.</summary>
    private static PokeBuilder Leavanny()
    {
        var b = new PokeBuilder("Leavanny", 0.9f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Leaf };
        var yellow = Rgb(240, 220, 90);
        var green = Rgb(130, 190, 70);
        var dark = Rgb(80, 140, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.22f, 0));
            b.Limb(leg, V(0.03f * s, 0.23f, 0), V(0.04f * s, 0.12f, 0.02f), 0.016f, 0.012f, green);
            b.Limb(leg, V(0.04f * s, 0.12f, 0.02f), V(0.035f * s, 0.02f, 0.01f), 0.012f, 0.01f, yellow);
            b.Spike(leg, V(0.035f * s, 0.015f, 0), V(0.04f * s, 0.008f, 0.07f), 0.012f, yellow);
        });
        var bc = V(0, 0.33f, 0);
        b.Ell(Body, bc, V(0.04f, 0.08f, 0.035f), green);
        b.Ell(Body, V(0, 0.42f, 0), V(0.035f, 0.04f, 0.03f), yellow, blend: 0.02f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.3f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            PointedLeaf(b, Body, bc + dir * 0.025f + V(0, 0.02f, 0), bc + dir * 0.06f + V(0, -0.12f, 0), 0.035f, i % 2 == 0 ? dark : green, dir, 0.18f);
        }
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.035f * s, 0.4f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.09f * s, 0.34f, 0.03f);
            var hand = V(0.1f * s, 0.42f, 0.08f);
            b.Limb(arm, shoulder, elbow, 0.013f, 0.011f, green);
            b.Limb(arm, elbow, hand, 0.011f, 0.009f, yellow);
            Blade(b, arm, elbow, elbow + V(0.06f * s, 0.1f, 0.03f), 0.03f, green, V(s, 0, 0.3f), 0.2f, Leaf);
        });
        int head = b.Head(V(0, 0.45f, 0.01f));
        var c = V(0, 0.51f, 0.02f);
        var r = V(0.047f, 0.046f, 0.045f);
        b.Limb(head, V(0, 0.43f, 0), c, 0.018f, 0.02f, yellow);
        b.Ell(head, c, r, yellow, mat: Fur);
        PokeBuilder.Both(s =>
        {
            Frond(b, head, c + V(0.035f * s, 0, -0.02f), c + V(0.085f * s, -0.08f, 0), 0.05f, green, V(s, 0, 0.4f), 0.2f, Leaf);
            Frond(b, head, c + V(0.03f * s, 0.025f, -0.02f), c + V(0.075f * s, 0.075f, -0.03f), 0.045f, dark, V(s, 0, 0.4f), 0.2f, Leaf);
            b.Spike(head, c + V(0.015f * s, 0.03f, 0), c + V(0.03f * s, 0.11f, 0.01f), 0.012f, yellow, 0.5f);
            var at = On(c, r, 0.016f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(200, 40, 50));
            var cheek = On(c, r, 0.025f * s, c.Y - 0.014f);
            b.Mark(head, cheek, Outward(c, r, cheek), 0.007f, 0.005f, Rgb(240, 150, 150));
        });
        return b;
    }

    // ------------------------------------------------------------------ Venipede line

    /// <summary>Venipede: a magenta centipede banded in dark green, short black spikes along its back, two antennae swept back and two more at its tail, yellow eyes under heavy lids.</summary>
    private static PokeBuilder Venipede()
    {
        var path = new[] { V(0, 0.07f, -0.17f), V(0, 0.075f, -0.1f), V(0, 0.08f, -0.03f), V(0, 0.085f, 0.04f) };
        var b = new PokeBuilder("Venipede", 0.5f, BodyPlan.Serpent, path[0]) { Coat = Shell };
        var red = Rgb(196, 50, 90);
        var green = Rgb(50, 90, 60);
        var black = Rgb(48, 44, 52);
        int neck = Grub(b, path, 0.055f, 0.065f, red, red, (i, seg, at, r) =>
        {
            b.PaintTorus(seg, at + V(0, 0, -r * 0.55f), r * 0.85f, 0.012f, green, V(90f, 0, 0));
            PokeBuilder.Both(s =>
            {
                b.Spike(seg, at + V(r * 0.6f * s, -r * 0.5f, 0), V(r * 1.2f * s, 0, at.Z + 0.01f), 0.012f, black, mat: Shell);
            });
        });
        int head = b.Head(path[^1] + V(0, 0.01f, 0.04f), neck);
        var c = V(0, 0.1f, 0.11f);
        var r = V(0.07f, 0.065f, 0.06f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, 0.01f, 0.04f), V(0.06f, 0.035f, 0.03f), black);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, c + V(0.03f * s, 0.05f, -0.01f), c + V(0.05f * s, 0.1f, -0.05f), c + V(0.06f * s, 0.12f, -0.12f)), 0.012f, 0.006f, black, blend: 0f);
            var at = On(c, r, 0.035f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, sclera: true, white: Rgb(250, 190, 60), pupil: Rgb(40, 30, 30), glare: true);
            b.Spike(head, c + V(0.03f * s, -0.04f, 0.04f), c + V(0.02f * s, -0.07f, 0.07f), 0.01f, black);
        });
        int tail = b.Tail(path[0]);
        PokeBuilder.Both(s => b.Tube(tail, Smooth(3, path[0] + V(0.02f * s, 0.03f, -0.02f), path[0] + V(0.04f * s, 0.09f, -0.07f), path[0] + V(0.06f * s, 0.12f, -0.14f)), 0.012f, 0.006f, black, blend: 0f));
        return b;
    }

    /// <summary>Whirlipede: a centipede curled into a wheel, its lavender shell ringed in red on every segment, spikes round its rim, two striped horns behind and two yellow eyes looking out of the open middle.</summary>
    private static PokeBuilder Whirlipede()
    {
        var b = new PokeBuilder("Whirlipede", 0.85f, BodyPlan.Floating, V(0, 0.29f, 0)) { Coat = Shell };
        var lavender = Rgb(172, 156, 196);
        var gray = Rgb(90, 86, 100);
        var red = Rgb(200, 50, 80);
        var c = V(0, 0.29f, 0);
        var r = V(0.27f, 0.29f, 0.22f);
        b.Ell(Body, c, r, lavender);
        foreach (float a in new[] { 0f, 60f, 120f })
            b.PaintTorus(Body, c, 0.28f, 0.01f, gray, V(0, 0, a), 1f, 0.786f);
        // The open middle: a dark rim round a hollow, and the face inside it
        b.PaintEll(Body, c + V(0, 0, 0.18f), V(0.15f, 0.15f, 0.08f), gray);
        b.Cut(Body, c + V(0, 0, 0.23f), V(0.08f, 0.09f, 0.06f));
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + MathF.PI / 8f;
            var at = Out(c, r, default, V(MathF.Cos(a), MathF.Sin(a), 0.35f));
            b.Mark(Body, at, Outward(c, r, at), 0.035f, 0.035f, red, MarkShape.Ring);
            float sa = a + MathF.PI / 8f;
            if (MathF.Sin(sa) < -0.3f) continue;
            var sd = Vector3.Normalize(V(MathF.Cos(sa), MathF.Sin(sa), 0.1f));
            var sp = Out(c, r, default, sd);
            b.Spike(Body, sp - sd * 0.02f, sp + sd * 0.07f, 0.03f, lavender);
        }
        int head = b.Head(c + V(0, 0, 0.1f));
        var hc = c + V(0, 0, 0.14f);
        var hr = V(0.08f, 0.085f, 0.06f);
        b.Ell(head, hc, hr, Rgb(60, 56, 66));
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.032f * s, hc.Y + 0.005f);
            b.Eye(head, at, Outward(hc, hr, at), 0.016f, sclera: true, white: Rgb(250, 200, 60), pupil: Rgb(30, 30, 30), glare: true);
        });
        // Two striped horns behind, one rising and one trailing
        int tail = b.Tail(c + V(0, 0.1f, -0.18f));
        foreach (var (from, to) in new[] { (c + V(0, 0.18f, -0.15f), c + V(0.03f, 0.42f, -0.36f)), (c + V(0, -0.08f, -0.2f), c + V(0.02f, -0.1f, -0.44f)) })
        {
            b.Spike(tail, from, to, 0.04f, Rgb(120, 116, 124), mat: Shell);
            for (int k = 1; k <= 3; k++)
                b.PaintTorus(tail, Vector3.Lerp(from, to, k * 0.22f), 0.04f * (1f - k * 0.2f), 0.008f, Rgb(40, 38, 44), Euler(to - from));
        }
        return b;
    }

    /// <summary>
    /// Scolipede and its Mega Evolution: a great magenta centipede rearing up, its chest dark, purple rings down its
    /// segments, two hooked horns banded in pink on its head and two at its tail; the Mega armoured in lavender with
    /// seams of green light, a broad hood and horns grown into long blades of light.
    /// </summary>
    private static PokeBuilder ScolipedeBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Scolipede-Mega" : "Scolipede", 1.1f, BodyPlan.Quadruped, V(0, 0.32f, -0.1f)) { Coat = Shell };
        var red = mega ? Rgb(170, 60, 96) : Rgb(200, 40, 80);
        var plate = mega ? Rgb(176, 164, 190) : red;
        var dark = Rgb(56, 52, 64);
        var purple = Rgb(150, 70, 160);
        var pink = Rgb(220, 120, 170);
        var glow = Rgb(90, 230, 190);
        var horn = mega ? dark : purple;
        var band = mega ? glow : pink;
        // Segments from the tail, through the great belly, up the reared neck
        var path = new[] { V(0, 0.28f, -0.5f), V(0, 0.27f, -0.38f), V(0, 0.28f, -0.25f), V(0, 0.32f, -0.08f), V(0, 0.45f, 0.06f), V(0, 0.6f, 0.11f), V(0, 0.73f, 0.1f) };
        float[] radius = { 0.065f, 0.08f, 0.1f, 0.155f, 0.135f, 0.105f, 0.09f };
        var bone = new int[path.Length];
        bone[3] = Body;
        for (int i = 2; i >= 0; i--) bone[i] = b.Part("rear" + i, bone[i + 1], path[i], PokeRole.Segment, 3 - i);
        for (int i = 4; i < path.Length; i++) bone[i] = b.Part("front" + i, bone[i - 1], path[i], PokeRole.Segment, i - 3);
        for (int i = 0; i < path.Length; i++)
        {
            float rr = radius[i];
            b.Ell(bone[i], path[i], V(rr, rr * 0.95f, rr), i >= 3 ? plate : red, blend: 0.03f);
            if (i != 3)
                PokeBuilder.Both(s => b.Mark(bone[i], path[i] + V(rr * 0.92f * s, rr * 0.3f, 0), V(s, 0.3f, 0), rr * 0.32f, rr * 0.32f, purple, MarkShape.Ring));
            if (mega && i >= 2)
                b.PaintTorus(bone[i], path[i] + Vector3.Normalize(path[Math.Min(i + 1, path.Length - 1)] - path[i - 1]) * rr * 0.55f, rr * 0.82f, 0.008f, glow,
                    Euler(path[Math.Min(i + 1, path.Length - 1)] - path[i - 1]));
        }
        b.PaintEll(bone[3], path[3] + V(0, -0.04f, 0.07f), V(0.12f, 0.11f, 0.09f), dark);
        b.PaintEll(bone[4], path[4] + V(0, -0.02f, 0.07f), V(0.1f, 0.1f, 0.07f), dark);
        // Three pairs of thin legs with dark joints and pointed feet
        foreach (var (z, front, hip) in new[] { (0.02f, true, 0.24f), (-0.23f, false, 0.22f), (-0.38f, false, 0.22f) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, hip, z), front);
                var knee = V(0.17f * s, hip - 0.02f, z + 0.03f);
                var foot = V(0.19f * s, 0, z + 0.07f);
                b.Limb(leg, V(0.06f * s, hip, z), knee, 0.038f, 0.03f, red);
                b.Ell(leg, knee, V(0.034f, 0.034f, 0.034f), dark);
                b.Spike(leg, knee, foot, 0.03f, red);
                b.PaintEll(leg, foot + V(0, 0.04f, 0), V(0.025f, 0.05f, 0.025f), dark);
            });
        // A head with hooked horns curving up and forward, yellow eyes and small mandibles
        int head = b.Head(path[^1] + V(0, 0.04f, 0.02f), bone[^1]);
        var c = V(0, 0.8f, 0.17f);
        var r = mega ? V(0.1f, 0.075f, 0.1f) : V(0.08f, 0.068f, 0.09f);
        b.Ell(head, c, r, plate);
        PokeBuilder.Both(s =>
        {
            var hp = Smooth(3, c + V(0.04f * s, 0.04f, -0.02f), c + V(0.07f * s, 0.16f, -0.02f), c + V(0.09f * s, 0.26f, 0.06f), c + V(0.08f * s, mega ? 0.36f : 0.3f, mega ? 0.2f : 0.14f));
            b.Tube(head, hp, 0.03f, 0.008f, horn, blend: 0f);
            for (int k = 2; k < hp.Length - 2; k += 2)
                b.PaintTorus(head, hp[k], 0.03f - 0.022f * k / (hp.Length - 1), 0.006f, band, Euler(hp[k + 1] - hp[k]));
            if (mega)
            {
                b.Spike(head, hp[^2], hp[^1] + V(0.03f * s, 0.25f, -0.2f), 0.022f, glow, mat: Glow);
                Blade(b, head, c + V(0.05f * s, 0.02f, -0.04f), c + V(0.17f * s, -0.07f, -0.1f), 0.07f, plate, V(s, 0.3f, 0.3f), 0.2f, Shell);
            }
            var at = On(c, r, (mega ? 0.06f : 0.045f) * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, white: Rgb(250, 200, 60), pupil: Rgb(30, 30, 30), glare: true);
            b.Spike(head, c + V(0.03f * s, -0.04f, 0.06f), c + V(0.02f * s, -0.08f, 0.1f), 0.014f, dark);
        });
        // Two more hooked horns at the tail
        int tail = b.Tail(path[0], bone[0]);
        PokeBuilder.Both(s =>
        {
            var tp = Smooth(3, path[0] + V(0.03f * s, 0.02f, -0.02f), path[0] + V(0.06f * s, 0.12f, -0.1f), path[0] + V(0.08f * s, 0.28f, -0.14f), path[0] + V(0.09f * s, mega ? 0.42f : 0.36f, -0.08f));
            b.Tube(tail, tp, 0.03f, 0.008f, horn, blend: 0f);
            for (int k = 2; k < tp.Length - 2; k += 2)
                b.PaintTorus(tail, tp[k], 0.03f - 0.022f * k / (tp.Length - 1), 0.006f, band, Euler(tp[k + 1] - tp[k]));
            if (mega) b.Spike(tail, tp[^2], tp[^1] + V(0.02f * s, 0.14f, -0.12f), 0.02f, glow, mat: Glow);
        });
        return b;
    }

    private static PokeBuilder Scolipede() => ScolipedeBuild(false);

    // ------------------------------------------------------------------ Cottonee line

    /// <summary>Cottonee: a puff of cotton drifting on the wind, a round white face under a pale green cloud of fluff, a green leaf out to each side and orange eyes.</summary>
    private static PokeBuilder Cottonee()
    {
        var b = new PokeBuilder("Cottonee", 0.45f, BodyPlan.Floating, V(0, 0.15f, 0)) { Coat = Fur }.Hover();
        var cotton = Rgb(222, 234, 220);
        var light = Rgb(240, 246, 236);
        var green = Rgb(80, 170, 90);
        var c = V(0, 0.15f, 0);
        var r = V(0.065f, 0.055f, 0.06f);
        b.Ell(Body, c, r, White);
        Lumps(b, Body, c + V(0, 0.01f, -0.01f), V(0.075f, 0.065f, 0.065f), 22, 0.035f, cotton, light, c.Y - 0.03f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(Body, s, c + V(0.06f * s, 0.01f, 0));
            Frond(b, ear, c + V(0.05f * s, 0.01f, 0), c + V(0.17f * s, 0.04f, 0), 0.05f, green, V(0, 1f, 0.3f), 0.18f, Leaf);
            var at = On(c, r, 0.025f * s, c.Y + 0.008f);
            b.Eye(Body, at, Outward(c, r, at), 0.012f, Rgb(240, 130, 40));
        });
        return Lift(b);
    }

    /// <summary>Whimsicott: a little brown imp in a great cream cloud of cotton, green horns curled at its cheeks, a tuft of cotton on its head and under its chin and orange eyes.</summary>
    private static PokeBuilder Whimsicott()
    {
        var b = new PokeBuilder("Whimsicott", 0.7f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Fur };
        var brown = Rgb(150, 100, 70);
        var cotton = Rgb(240, 232, 200);
        var light = Rgb(250, 246, 226);
        var green = Rgb(80, 170, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.06f, 0));
            b.Limb(leg, V(0.03f * s, 0.07f, 0), V(0.035f * s, 0.015f, 0.01f), 0.016f, 0.014f, brown);
            b.Ell(leg, V(0.037f * s, 0.012f, 0.02f), V(0.018f, 0.012f, 0.025f), brown);
        });
        b.Ell(Body, V(0, 0.12f, 0), V(0.05f, 0.06f, 0.045f), brown);
        // The cotton: a great cloud behind and round it, lumpy at the rim
        var cc = V(0, 0.24f, -0.1f);
        b.Ell(Body, cc, V(0.2f, 0.18f, 0.1f), cotton);
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            b.Ell(Body, cc + V(MathF.Cos(a) * 0.19f, MathF.Sin(a) * 0.17f, 0), V(0.07f, 0.065f, 0.08f), i % 3 == 0 ? light : cotton, blend: 0.03f);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.04f * s, 0.15f, 0.01f));
            b.Limb(arm, V(0.04f * s, 0.15f, 0.01f), V(0.07f * s, 0.12f, 0.04f), 0.013f, 0.011f, brown);
        });
        int head = b.Head(V(0, 0.18f, 0.02f));
        var c = V(0, 0.22f, 0.03f);
        var r = V(0.05f, 0.045f, 0.045f);
        b.Ell(head, c, r, brown);
        foreach (var o in new[] { V(0, 0.045f, -0.005f), V(-0.025f, 0.035f, 0.01f), V(0.025f, 0.035f, 0.01f) })
            b.Ell(head, c + o, V(0.03f, 0.022f, 0.028f), cotton, blend: 0.015f);
        b.Ell(head, c + V(0, -0.045f, 0.03f), V(0.03f, 0.02f, 0.02f), cotton, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            Curl(b, head, c + V(0.04f * s, 0.01f, 0), c + V(0.068f * s, -0.008f, 0.005f), V(0, 1f, 0), V(s, 0, 0), 0.026f, 1.1f, 0.012f, green);
            var at = On(c, r, 0.02f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(240, 130, 40));
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.02f), V(0, -0.3f, 1f), 0.012f, 0.007f, Rgb(200, 90, 90), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Petilil line

    /// <summary>Petilil: a pale green bulb like an onion with a white face, three leaves sprouting from its top and a frilled leafy skirt round its foot.</summary>
    private static PokeBuilder Petilil()
    {
        var b = new PokeBuilder("Petilil", 0.5f, BodyPlan.Biped, V(0, 0.14f, 0)) { Coat = Leaf };
        var pale = Rgb(200, 230, 140);
        var line = Rgb(150, 190, 100);
        var leaf = Rgb(80, 160, 80);
        var skirt = Rgb(180, 220, 110);
        var c = V(0, 0.15f, 0);
        var r = V(0.075f, 0.085f, 0.07f);
        b.Ell(Body, c, r, pale);
        b.Spike(Body, c + V(0, 0.05f, 0), c + V(0, 0.13f, 0), 0.04f, pale);
        foreach (float a in new[] { 0f, 60f, 120f })
            b.PaintEll(Body, c + V(0, 0.01f, 0), V(0.003f, 0.11f, 0.1f), line, V(0, a, 0), 0.005f);
        b.PaintEll(Body, c + V(0, -0.025f, 0.055f), V(0.045f, 0.04f, 0.03f), White);
        // The skirt: a ring of round leaves under the bulb
        b.Ell(Body, V(0, 0.06f, 0), V(0.05f, 0.035f, 0.045f), skirt);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            b.Ell(Body, V(MathF.Sin(a) * 0.055f, 0.03f, MathF.Cos(a) * 0.05f), V(0.035f, 0.028f, 0.035f), skirt, blend: 0.015f);
        }
        int head = b.Head(c + V(0, 0.1f, 0));
        var top = c + V(0, 0.125f, 0);
        b.Limb(head, c + V(0, 0.1f, 0), top + V(0, 0.01f, 0), 0.012f, 0.01f, leaf);
        PointedLeaf(b, head, top, top + V(0, 0.12f, 0.01f), 0.03f, leaf, V(0, 0, 1f), 0.18f);
        PokeBuilder.Both(s => PointedLeaf(b, head, top, top + V(0.11f * s, 0.08f, 0), 0.035f, leaf, V(0, 0.3f, 1f), 0.18f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y - 0.02f);
            b.Eye(Body, at, Outward(c, r, at), 0.014f, Rgb(170, 60, 50));
        });
        return b;
    }

    /// <summary>
    /// Lilligant and its Hisuian form: a flower lady, her pale green bulb of a skirt edged in dark leaves, a white face
    /// framed in leaves, a great orange flower on her head crowned in yellow, and leaves for arms; the Hisuian a lithe
    /// dancer on long legs that fade from white to pink, yellow pointed feet, a little skirt of leaves, long leaf arms
    /// held out and a pink flower on her head.
    /// </summary>
    private static PokeBuilder LilligantBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Lilligant-Hisui" : "Lilligant", 0.85f, BodyPlan.Biped, V(0, hisui ? 0.42f : 0.2f, 0)) { Coat = Leaf };
        var pale = Rgb(190, 226, 140);
        var leaf = Rgb(60, 150, 70);
        var hair = Rgb(170, 220, 120);
        var white = Rgb(250, 250, 246);
        var yellow = Rgb(250, 220, 80);
        var flower = hisui ? Rgb(240, 160, 190) : Rgb(236, 130, 60);
        float neck;
        if (!hisui)
        {
            var sc = V(0, 0.15f, 0);
            b.Ell(Body, sc, V(0.12f, 0.15f, 0.11f), pale);
            foreach (float a in new[] { 0f, 60f, 120f })
                b.PaintEll(Body, sc, V(0.003f, 0.16f, 0.13f), Rgb(140, 190, 100), V(0, a, 0), 0.005f);
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathF.Tau / 6f + 0.5f;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                Frond(b, Body, sc + dir * 0.08f + V(0, -0.02f, 0), sc + dir * 0.17f + V(0, -0.14f, 0), 0.06f, leaf, dir, 0.15f, Leaf);
            }
            b.Ell(Body, V(0, 0.31f, 0), V(0.05f, 0.04f, 0.045f), pale);
            neck = 0.34f;
        }
        else
        {
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.025f * s, 0.36f, 0));
                var knee = V(0.01f * s, 0.2f, 0.02f);
                var foot = V(-0.015f * s, 0.05f, 0);
                b.Limb(leg, V(0.025f * s, 0.37f, 0), knee, 0.022f, 0.016f, white);
                b.Limb(leg, knee, foot, 0.016f, 0.012f, white);
                b.PaintEll(leg, Vector3.Lerp(knee, foot, 0.6f), V(0.03f, 0.1f, 0.03f), Rgb(230, 120, 170));
                b.Spike(leg, foot + V(0, 0.03f, 0), foot + V(0.02f * s, -0.045f, 0.03f), 0.022f, yellow);
            });
            b.Ell(Body, V(0, 0.42f, 0), V(0.045f, 0.07f, 0.04f), pale);
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathF.Tau / 5f;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                PointedLeaf(b, Body, V(0, 0.38f, 0) + dir * 0.03f, V(0, 0.3f, 0) + dir * 0.08f, 0.04f, leaf, dir, 0.16f);
            }
            neck = 0.49f;
        }
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.04f * s, neck - 0.02f, 0);
            int arm = b.Arm(s, shoulder);
            if (hisui)
            {
                var tip = shoulder + V(0.26f * s, s > 0 ? 0.08f : -0.04f, 0.04f);
                b.Limb(arm, shoulder, Vector3.Lerp(shoulder, tip, 0.3f), 0.012f, 0.01f, leaf);
                Frond(b, arm, Vector3.Lerp(shoulder, tip, 0.2f), tip, 0.045f, leaf, V(0, 1f, 0.3f), 0.15f, Leaf);
            }
            else
                Frond(b, arm, shoulder, shoulder + V(0.15f * s, -0.12f, 0.06f), 0.05f, leaf, V(s, 0.3f, 0.6f), 0.15f, Leaf);
        });
        // A collar of three yellow petals
        for (int i = 0; i < 3; i++)
        {
            float a = (i - 1) * 0.9f;
            Frond(b, Body, V(0, neck - 0.01f, 0), V(MathF.Sin(a) * 0.05f, neck - 0.02f, MathF.Cos(a) * 0.05f), 0.03f, yellow, V(0, 1f, 0), 0.2f);
        }
        int head = b.Head(V(0, neck, 0.01f));
        var c = V(0, neck + 0.05f, 0.02f);
        var r = V(0.045f, 0.045f, 0.042f);
        b.Limb(head, V(0, neck - 0.02f, 0), c, 0.015f, 0.015f, pale);
        b.Ell(head, c, r, white, mat: Fur);
        b.Ell(head, c + V(0, 0.025f, -0.025f), V(0.048f, 0.036f, 0.036f), hair, blend: 0.008f);
        PokeBuilder.Both(s => Frond(b, head, c + V(0.03f * s, 0.03f, -0.01f), c + V(0.06f * s, -0.06f, 0), 0.04f, hair, V(s, 0, 0.5f), 0.15f, Leaf));
        // The flower on its head: petals round a crown of yellow stamens
        var fc = c + V(0.02f, 0.06f, -0.01f);
        int petals = hisui ? 6 : 4;
        float big = hisui ? 0.75f : 1f;
        for (int i = 0; i < petals; i++)
        {
            float a = i * MathF.Tau / petals + 0.4f;
            var dir = V(MathF.Cos(a), 0.35f, MathF.Sin(a));
            b.Ell(head, fc + dir * 0.035f * big, V(0.025f, 0.04f, 0.02f) * big, flower, Euler(dir), blend: 0.012f);
        }
        b.Ell(head, fc + V(0, 0.02f, 0), V(0.025f, 0.02f, 0.025f) * big, flower, blend: 0.01f);
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f;
            var tip = fc + V(MathF.Cos(a) * 0.03f, 0.08f, MathF.Sin(a) * 0.03f) * big;
            b.Limb(head, fc + V(0, 0.03f, 0) * big, tip, 0.005f, 0.004f, yellow);
            b.Ell(head, tip, V(0.01f, 0.01f, 0.01f), hisui ? yellow : Rgb(236, 120, 50));
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.017f * s, c.Y - 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, hisui ? Rgb(200, 70, 120) : Rgb(200, 60, 50));
        });
        return b;
    }

    private static PokeBuilder Lilligant() => LilligantBuild(false);

    // ------------------------------------------------------------------ Basculin

    private enum Stripe { Red, Blue, White }

    /// <summary>
    /// Basculin in its three stripes: a fierce green fish with a great head and jutting jaw, a dark band over its back
    /// edged by a red, blue or white stripe through its eye, jagged white fins and a crest of spikes; the
    /// white-striped darker, its mouth open on its teeth.
    /// </summary>
    private static PokeBuilder BasculinBuild(Stripe stripe)
    {
        string name = stripe switch { Stripe.Blue => "Basculin-Blue-Striped", Stripe.White => "Basculin-White-Striped", _ => "Basculin" };
        var b = new PokeBuilder(name, 0.75f, BodyPlan.Fish, V(0, 0.25f, 0)) { Coat = Scales }.Hover();
        var green = stripe == Stripe.White ? Rgb(90, 140, 96) : Rgb(80, 160, 80);
        var dark = Rgb(50, 60, 56);
        var line = stripe switch { Stripe.Blue => Rgb(40, 140, 220), Stripe.White => Rgb(240, 240, 236), _ => Rgb(220, 60, 50) };
        var fin = Rgb(236, 236, 230);
        var mc = V(0, 0.25f, -0.04f);
        var mr = V(0.085f, 0.12f, 0.18f);
        var hc = V(0, 0.25f, 0.08f);
        var hr = V(0.095f, 0.13f, 0.12f);
        b.Ell(Body, mc, mr, green);
        b.Ell(Body, hc, hr, green);
        // A dark band over the back, the stripe along its edge
        b.PaintEll(Body, V(0, 0.34f, 0.02f), V(0.11f, 0.08f, 0.26f), line);
        b.PaintEll(Body, V(0, 0.36f, 0.02f), V(0.11f, 0.075f, 0.26f), dark);
        PokeBuilder.Both(s =>
        {
            var spot = Out(mc, mr, default, V(s, -0.15f, -0.45f));
            b.Mark(Body, spot, Outward(mc, mr, spot), 0.022f, 0.03f, Rgb(60, 70, 66));
            Blade(b, Body, V(0.075f * s, 0.2f, 0.04f), V(0.15f * s, 0.12f, -0.06f), 0.04f, fin, V(0, 1f, 0.3f), 0.2f);
            var at = Out(hc, hr, default, V(0.08f * s, 0.055f, 0.04f));
            if (stripe == Stripe.Red) b.Mark(Body, at, Outward(hc, hr, at), 0.036f, 0.036f, line, MarkShape.Ring);
            if (stripe == Stripe.Blue)
                b.Eye(Body, at, Outward(hc, hr, at), 0.024f, Rgb(40, 60, 90));
            else
                b.Eye(Body, at, Outward(hc, hr, at), 0.024f, sclera: true, pupil: stripe == Stripe.Red ? Rgb(200, 40, 40) : Rgb(40, 40, 40), glare: stripe == Stripe.Red);
        });
        // A crest of jagged white spikes over the head and down the back
        for (int i = 0; i < 4; i++)
        {
            var at = V(0, 0.38f - i * 0.005f, 0.1f - i * 0.04f);
            Blade(b, Body, at - V(0, 0.03f, 0), at + V(0, 0.06f + (i % 2) * 0.03f, -0.02f), 0.025f, fin, V(1f, 0, 0), 0.25f);
        }
        Blade(b, Body, V(0, 0.15f, -0.02f), V(0, 0.07f, -0.1f), 0.04f, fin, V(1f, 0, 0), 0.2f);
        // The jutting lower jaw, a fang or a row of teeth
        int head = b.Head(V(0, 0.2f, 0.1f));
        b.Ell(head, V(0, 0.18f, 0.12f), V(0.075f, 0.05f, 0.1f), green);
        if (stripe == Stripe.White)
        {
            Grin(b, Body, V(0, 0.22f, 0.19f), V(0.05f, 0.022f, 0.03f), Rgb(200, 80, 90));
            for (int i = -2; i <= 2; i++)
                b.Spike(head, V(i * 0.016f, 0.205f, 0.19f - MathF.Abs(i) * 0.01f), V(i * 0.016f, 0.23f, 0.19f - MathF.Abs(i) * 0.01f), 0.006f, White, mat: Shell, blend: 0.003f);
        }
        else
            PokeBuilder.Both(s => b.Spike(head, V(0.035f * s, 0.21f, 0.2f), V(0.04f * s, 0.25f, 0.2f), 0.009f, White, mat: Shell, blend: 0.003f));
        // A forked tail
        int tail = b.Tail(V(0, 0.25f, -0.2f));
        b.Limb(tail, V(0, 0.25f, -0.18f), V(0, 0.25f, -0.27f), 0.04f, 0.025f, green);
        PokeBuilder.Both(s => Blade(b, tail, V(0, 0.25f, -0.25f), V(0, 0.25f + 0.1f * s, -0.36f), 0.07f, green, V(1f, 0, 0), 0.2f));
        b.PaintEll(tail, V(0, 0.25f, -0.37f), V(0.03f, 0.13f, 0.04f), dark);
        return Lift(b);
    }

    private static PokeBuilder Basculin() => BasculinBuild(Stripe.Red);

    // ------------------------------------------------------------------ Sandile line

    /// <summary>Sandile: a small desert crocodile, sandy tan banded in black, a black mask round its eyes, a long snout with two bumps for nostrils, a pink belly and short splayed legs.</summary>
    private static PokeBuilder Sandile()
    {
        var b = new PokeBuilder("Sandile", 0.6f, BodyPlan.Quadruped, V(0, 0.1f, -0.02f)) { Coat = Scales };
        var tan = Rgb(214, 172, 120);
        var black = Rgb(50, 46, 46);
        var pink = Rgb(220, 150, 160);
        foreach (var (z, front) in new[] { (0.06f, true), (-0.09f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.08f, z), front);
                var foot = V(0.11f * s, 0.016f, z + 0.015f);
                b.Limb(leg, V(0.06f * s, 0.08f, z), foot, 0.022f, 0.018f, tan);
                b.Ell(leg, foot, V(0.022f, 0.016f, 0.028f), tan);
                Claws(b, leg, foot + V(0, -0.004f, 0.025f), V(0.01f, 0, 0), V(0, -0.2f, 1f), 0.016f, 0.005f);
            });
        var bc = V(0, 0.1f, -0.02f);
        b.Ell(Body, bc, V(0.08f, 0.065f, 0.12f), tan);
        foreach (float z in new[] { -0.08f, 0.02f })
            b.PaintTorus(Body, bc + V(0, 0.005f, z), 0.08f * MathF.Sqrt(1f - z * z / 0.0144f), 0.014f, black, V(90f, 0, 0), 1f, 0.81f);
        b.PaintEll(Body, bc + V(0, -0.05f, 0), V(0.07f, 0.03f, 0.11f), pink);
        // A tail banded in black, tapering behind
        int tail = b.Tail(bc + V(0, 0, -0.1f));
        var tp = Smooth(3, bc + V(0, 0, -0.1f), V(0, 0.08f, -0.24f), V(0.02f, 0.09f, -0.34f), V(0.05f, 0.12f, -0.4f));
        b.Tube(tail, tp, 0.045f, 0.012f, tan, blend: 0f);
        for (int i = 2; i < tp.Length - 1; i += 2)
            b.PaintTorus(tail, tp[i], 0.045f - 0.033f * i / (tp.Length - 1), 0.009f, black, Euler(tp[i + 1] - tp[i]));
        // A long snout, nostrils on bumps at its end, and a black mask round the eyes
        int head = b.Head(bc + V(0, 0.02f, 0.1f));
        var c = V(0, 0.13f, 0.13f);
        var r = V(0.06f, 0.055f, 0.06f);
        b.Ell(head, c, r, tan);
        b.Ell(head, V(0, 0.1f, 0.22f), V(0.042f, 0.028f, 0.07f), tan);
        b.PaintEll(head, V(0, 0.095f, 0.21f), V(0.045f, 0.004f, 0.08f), Rgb(110, 70, 60), soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, V(0.022f * s, 0.115f, 0.275f), V(0.014f, 0.012f, 0.014f), tan, blend: 0.008f);
            var at = On(c, r, 0.03f * s, c.Y + 0.018f);
            b.PaintEll(head, at, V(0.032f, 0.026f, 0.03f), black);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(30, 30, 34));
        });
        return b;
    }

    /// <summary>Krokorok: a desert crocodile standing up with its arms folded, sandy tan banded in black, a black mask like dark glasses, a pink belly and a long banded tail.</summary>
    private static PokeBuilder Krokorok()
    {
        var b = new PokeBuilder("Krokorok", 0.8f, BodyPlan.Biped, V(0, 0.26f, 0)) { Coat = Scales };
        var tan = Rgb(210, 168, 116);
        var black = Rgb(50, 46, 46);
        var pink = Rgb(226, 150, 160);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.16f, 0));
            var foot = V(0.07f * s, 0.018f, 0.03f);
            b.Limb(leg, V(0.05f * s, 0.17f, 0), V(0.065f * s, 0.06f, 0), 0.032f, 0.022f, tan);
            b.Limb(leg, V(0.065f * s, 0.06f, 0), foot, 0.022f, 0.018f, tan);
            b.Ell(leg, foot, V(0.025f, 0.018f, 0.04f), tan);
            b.PaintTorus(leg, V(0.062f * s, 0.11f, 0), 0.03f, 0.008f, black);
            Claws(b, leg, foot + V(0, -0.004f, 0.035f), V(0.012f, 0, 0), V(0, -0.2f, 1f), 0.018f, 0.006f);
        });
        b.Ell(Body, V(0, 0.27f, 0), V(0.075f, 0.12f, 0.065f), tan);
        b.PaintTorus(Body, V(0, 0.33f, 0), 0.07f, 0.018f, black, sz: 0.87f);
        b.PaintTorus(Body, V(0, 0.2f, 0), 0.068f, 0.014f, black, sz: 0.87f);
        b.PaintEll(Body, V(0, 0.25f, 0.05f), V(0.05f, 0.09f, 0.03f), pink);
        // Arms folded across its chest
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.065f * s, 0.34f, 0.01f);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.075f * s, 0.27f, 0.07f);
            var hand = V(-0.04f * s, s > 0 ? 0.3f : 0.29f, 0.085f);
            b.Limb(arm, shoulder, elbow, 0.022f, 0.018f, tan);
            b.Limb(arm, elbow, hand, 0.018f, 0.015f, tan);
            b.PaintTorus(arm, Vector3.Lerp(elbow, hand, 0.4f), 0.017f, 0.006f, black, Euler(hand - elbow));
        });
        int tail = b.Tail(V(0, 0.2f, -0.05f));
        var tp = Smooth(3, V(0, 0.2f, -0.05f), V(0, 0.1f, -0.15f), V(0, 0.04f, -0.26f), V(0.03f, 0.04f, -0.36f));
        b.Tube(tail, tp, 0.045f, 0.012f, tan, blend: 0f);
        for (int i = 2; i < tp.Length - 1; i += 2)
            b.PaintTorus(tail, tp[i], 0.045f - 0.033f * i / (tp.Length - 1), 0.01f, black, Euler(tp[i + 1] - tp[i]));
        int head = b.Head(V(0, 0.38f, 0.01f));
        var c = V(0, 0.44f, 0.03f);
        var r = V(0.055f, 0.05f, 0.06f);
        b.Limb(head, V(0, 0.37f, 0), c, 0.035f, 0.035f, tan);
        b.Ell(head, c, r, tan);
        b.Ell(head, c + V(0, -0.02f, 0.09f), V(0.04f, 0.026f, 0.075f), tan);
        b.PaintEll(head, c + V(0, -0.028f, 0.09f), V(0.045f, 0.004f, 0.08f), Rgb(80, 50, 50), soft: 0.004f);
        b.PaintEll(head, c + V(0, 0.012f, 0.03f), V(0.07f, 0.02f, 0.05f), black);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.02f * s, -0.002f, 0.155f), V(0.012f, 0.01f, 0.012f), tan, blend: 0.006f);
            b.Spike(head, c + V(0.035f * s, -0.03f, 0.12f), c + V(0.038f * s, -0.045f, 0.12f), 0.006f, White, mat: Shell, blend: 0.003f);
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(40, 36, 40), glare: true);
        });
        return b;
    }

    /// <summary>Krookodile: a great red crocodile standing tall, a black mask across its eyes, black bands on its arms and its long tail, a pale blue belly, broad jaws and white claws.</summary>
    private static PokeBuilder Krookodile()
    {
        var b = new PokeBuilder("Krookodile", 1f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Scales };
        var red = Rgb(170, 70, 80);
        var black = Rgb(46, 40, 44);
        var belly = Rgb(190, 206, 226);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.2f, 0));
            var knee = V(0.11f * s, 0.11f, 0.03f);
            var foot = V(0.11f * s, 0.022f, 0.04f);
            b.Limb(leg, V(0.08f * s, 0.21f, 0), knee, 0.055f, 0.04f, red);
            b.Limb(leg, knee, foot, 0.04f, 0.03f, red);
            b.Ell(leg, foot, V(0.04f, 0.022f, 0.06f), black);
            Claws(b, leg, foot + V(0, -0.005f, 0.055f), V(0.016f, 0, 0), V(0, -0.2f, 1f), 0.025f, 0.008f);
        });
        b.Ell(Body, V(0, 0.36f, 0), V(0.12f, 0.17f, 0.1f), red);
        b.PaintEll(Body, V(0, 0.32f, 0.07f), V(0.1f, 0.15f, 0.05f), belly);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.07f * s, 0.46f, 0.06f), V(0.05f, 0.012f, 0.06f), black, V(0, 0, 30f * s)));
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.1f * s, 0.46f, 0.01f);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.18f * s, 0.36f, 0.05f);
            var hand = V(0.2f * s, 0.26f, 0.12f);
            b.Limb(arm, shoulder, elbow, 0.04f, 0.032f, red);
            b.Limb(arm, elbow, hand, 0.032f, 0.026f, red);
            b.PaintTorus(arm, Vector3.Lerp(shoulder, elbow, 0.55f), 0.036f, 0.01f, black, Euler(elbow - shoulder));
            b.Ell(arm, hand, V(0.028f, 0.026f, 0.03f), red);
            Claws(b, arm, hand + V(0, -0.01f, 0.02f), V(0.012f, 0, 0), V(0, -0.6f, 1f), 0.03f, 0.007f);
        });
        int tail = b.Tail(V(0, 0.24f, -0.08f));
        var tp = Smooth(3, V(0, 0.24f, -0.08f), V(0, 0.12f, -0.2f), V(0, 0.05f, -0.34f), V(0.04f, 0.05f, -0.48f));
        b.Tube(tail, tp, 0.06f, 0.014f, red, blend: 0f);
        for (int i = 2; i < tp.Length - 1; i += 2)
            b.PaintTorus(tail, tp[i], 0.06f - 0.046f * i / (tp.Length - 1), 0.013f, black, Euler(tp[i + 1] - tp[i]));
        // A broad head with long jaws, a black mask across its eyes rising to two points
        int head = b.Head(V(0, 0.52f, 0.03f));
        var c = V(0, 0.6f, 0.05f);
        var r = V(0.075f, 0.065f, 0.075f);
        b.Ell(head, c, r, red);
        b.Ell(head, c + V(0, -0.025f, 0.12f), V(0.055f, 0.035f, 0.1f), red);
        b.PaintEll(head, c + V(0, -0.035f, 0.12f), V(0.06f, 0.005f, 0.11f), Rgb(80, 30, 40), soft: 0.004f);
        b.PaintEll(head, c + V(0, 0.018f, 0.04f), V(0.09f, 0.026f, 0.06f), black);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.015f * s, 0.035f, 0.03f), c + V(0.045f * s, 0.075f, -0.03f), 0.02f, black, 0.5f);
            b.Spike(head, c + V(0.045f * s, -0.04f, 0.17f), c + V(0.048f * s, -0.06f, 0.17f), 0.008f, White, mat: Shell, blend: 0.003f);
            var at = On(c, r, 0.04f * s, c.Y + 0.016f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(40, 36, 40), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Darumaka line

    /// <summary>
    /// Darumaka and its Galarian form: a round red doll of a Pokémon, yellow brows curled like flames on its head, big
    /// round eyes, a grin with two square teeth, three yellow spots on its belly and little fanned hands and feet; the
    /// Galarian a snowball, white, with brows and spots of blue ice and eyes ringed in blue.
    /// </summary>
    private static PokeBuilder DarumakaBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Darumaka-Galar" : "Darumaka", 0.55f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Fur };
        var coat = galar ? Rgb(242, 244, 248) : Rgb(214, 70, 70);
        var brow = galar ? Rgb(120, 214, 228) : Rgb(250, 210, 100);
        var hand = galar ? Rgb(150, 190, 230) : Rgb(244, 160, 100);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.07f, 0.02f));
            b.Limb(leg, V(0.05f * s, 0.07f, 0.02f), V(0.06f * s, 0.025f, 0.04f), 0.022f, 0.02f, coat);
            b.Ell(leg, V(0.06f * s, 0.022f, 0.045f), V(0.03f, 0.022f, 0.035f), hand);
        });
        var c = V(0, 0.16f, 0);
        var r = V(0.12f, 0.13f, 0.11f);
        b.Ell(Body, c, r, coat);
        foreach (float x in new[] { -0.035f, 0f, 0.035f })
        {
            var at = On(c, r, x, c.Y - 0.06f);
            if (galar) b.Mark(Body, at, Outward(c, r, at), 0.012f, 0.02f, brow, MarkShape.Diamond);
            else b.Mark(Body, at, Outward(c, r, at), 0.012f, 0.024f, brow);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.18f, 0.02f));
            var tip = V(0.145f * s, 0.2f, 0.04f);
            b.Limb(arm, V(0.1f * s, 0.18f, 0.02f), tip, 0.022f, 0.02f, coat);
            Digits(b, arm, tip, V(s, 0.5f, 0.2f), V(0, 0.012f, 0.01f), 0.02f, 0.011f, hand);
        });
        // Brows on top of its head: curls of flame, or blue ice
        int head = b.Head(c + V(0, 0.08f, 0.02f));
        PokeBuilder.Both(s =>
        {
            if (galar)
                Gem(b, head, c + V(0.045f * s, 0.115f, 0.03f), V(0.3f * s, 1f, 0.4f), 0.028f, brow);
            else
                Curl(b, head, c + V(0.03f * s, 0.11f, 0.02f), c + V(0.06f * s, 0.14f, 0.02f), V(-s, 0, 0), V(0, 1f, 0), 0.022f, 1.1f, 0.012f, brow);
            var at = On(c, r, 0.045f * s, c.Y + 0.05f);
            if (galar) b.Mark(Body, at, Outward(c, r, at), 0.032f, 0.032f, Rgb(80, 130, 210), MarkShape.Ring);
            b.Eye(Body, at, Outward(c, r, at), 0.026f, sclera: true, pupil: Rgb(30, 30, 30));
        });
        var m = On(c, r, 0, c.Y + 0.005f);
        b.Mark(Body, m, Outward(c, r, m), 0.03f, 0.016f, galar ? Rgb(80, 130, 210) : Rgb(90, 30, 30), MarkShape.Smile);
        if (!galar)
            b.Box(Body, m + V(0, 0.002f, 0), V(0.014f, 0.008f, 0.006f), 0.003f, White, mat: Shell, blend: 0.003f);
        return b;
    }

    private static PokeBuilder Darumaka() => DarumakaBuild(false);

    /// <summary>
    /// Darmanitan in its four shapes: a great red ape on its knuckles, flames for brows over a grin of teeth, cream fur
    /// at its fists and feet; in Zen Mode a blue stone doll sitting still, its arms folded round it and its brows
    /// curled; the Galarian a white snow ape under a great snowball set with ice, its hands and feet blue; the
    /// Galarian in Zen Mode a snowman, a flame burning on its head over a face of rings, a grinning red face below.
    /// </summary>
    private static PokeBuilder DarmanitanBuild(bool zen, bool galar)
    {
        string name = galar ? (zen ? "Darmanitan-Galar-Zen" : "Darmanitan-Galar-Standard") : zen ? "Darmanitan-Zen" : "Darmanitan";
        var flame = Rgb(236, 90, 50);
        var heart = Rgb(252, 214, 100);
        if (zen && galar)
        {
            var b = new PokeBuilder(name, 1f, BodyPlan.Floating, V(0, 0.24f, 0)) { Coat = Fur };
            var red = Rgb(214, 70, 60);
            var orange = Rgb(236, 130, 70);
            var low = V(0, 0.24f, 0);
            var lr = V(0.22f, 0.24f, 0.2f);
            b.Ell(Body, low, lr, White);
            // The grinning red face on the lower snowball: a red band round white eyes, a red ring below
            b.PaintEll(Body, low + V(0, 0.08f, 0.15f), V(0.16f, 0.045f, 0.09f), red);
            PokeBuilder.Both(s =>
            {
                var at = On(low, lr, 0.065f * s, low.Y + 0.08f);
                b.Eye(Body, at, Outward(low, lr, at), 0.02f, sclera: true, pupil: Rgb(30, 30, 30), glare: true);
                int arm = b.Arm(s, low + V(0.2f * s, 0.06f, 0));
                b.Ell(arm, low + V(0.23f * s, 0.06f, 0.03f), V(0.05f, 0.075f, 0.045f), orange);
                b.Spike(arm, low + V(0.23f * s, 0.1f, 0.04f), low + V(0.24f * s, 0.18f, 0.05f), 0.025f, orange);
            });
            var g = On(low, lr, 0, low.Y);
            Grin(b, Body, g, V(0.09f, 0.03f, 0.04f), Rgb(80, 30, 40));
            b.Mark(Body, g + V(0, 0.002f, 0.004f), Outward(low, lr, g), 0.07f, 0.016f, White, MarkShape.Zigzag);
            var ring = On(low, lr, 0, low.Y - 0.12f);
            b.Mark(Body, ring, Outward(low, lr, ring), 0.03f, 0.02f, Rgb(240, 120, 60), MarkShape.Ring);
            // The upper snowball, a face of rings and a wavy mouth, a flame burning on top
            int head = b.Head(V(0, 0.47f, 0));
            var hc = V(0, 0.59f, 0);
            var hr = V(0.14f, 0.13f, 0.13f);
            b.Ell(head, hc, hr, White);
            PokeBuilder.Both(s =>
            {
                var at = On(hc, hr, 0.06f * s, hc.Y + 0.01f);
                b.Mark(head, at, Outward(hc, hr, at), 0.022f, 0.022f, Rgb(240, 120, 60), MarkShape.Ring);
            });
            var mouth = On(hc, hr, 0, hc.Y - 0.045f);
            b.Mark(head, mouth, Outward(hc, hr, mouth), 0.05f, 0.012f, Rgb(110, 110, 120), MarkShape.Wave);
            FlameTongue(b, head, hc + V(0, 0.1f, 0), hc + V(0, 0.3f, -0.02f), 0.06f, flame, heart);
            PokeBuilder.Both(s => FlameTongue(b, head, hc + V(0.04f * s, 0.1f, 0), hc + V(0.08f * s, 0.22f, -0.02f), 0.035f, flame, heart));
            return b;
        }
        if (zen)
        {
            var b = new PokeBuilder(name, 1f, BodyPlan.Floating, V(0, 0.28f, 0)) { Coat = Shell };
            var blue = Rgb(92, 150, 196);
            var light = Rgb(144, 196, 226);
            var orange = Rgb(240, 160, 60);
            var c = V(0, 0.28f, 0);
            var r = V(0.21f, 0.28f, 0.19f);
            b.Ell(Body, c, r, blue);
            foreach (var (x, y) in new[] { (-0.07f, 0.16f), (0f, 0.15f), (0.07f, 0.16f), (-0.035f, 0.07f), (0.035f, 0.07f) })
            {
                var at = On(c, r, x, y);
                b.Mark(Body, at, Outward(c, r, at), 0.022f, 0.035f, light);
            }
            // Arms folded round its sides, ridged across
            PokeBuilder.Both(s =>
            {
                var path = Smooth(3, V(0.17f * s, 0.44f, -0.02f), V(0.23f * s, 0.3f, 0.04f), V(0.19f * s, 0.13f, 0.12f), V(0.07f * s, 0.07f, 0.18f));
                int arm = b.Arm(s, path[0]);
                b.Tube(arm, path, 0.055f, 0.045f, light, blend: 0.01f);
                for (int i = 2; i < path.Length - 1; i += 2)
                    b.PaintTorus(arm, path[i], 0.052f, 0.006f, blue, Euler(path[i + 1] - path[i]));
            });
            int head = b.Head(c + V(0, 0.16f, 0.06f));
            PokeBuilder.Both(s =>
            {
                Curl(b, head, c + V(0.04f * s, 0.25f, 0.1f), c + V(0.08f * s, 0.27f, 0.11f), V(-s, 0, 0), V(0, 1f, 0), 0.026f, 1.1f, 0.014f, orange);
                var at = On(c, r, 0.065f * s, c.Y + 0.17f);
                b.Eye(Body, at, Outward(c, r, at), 0.024f, sclera: true, pupil: Rgb(30, 30, 30));
            });
            var m = On(c, r, 0, c.Y + 0.11f);
            b.Mark(Body, m, Outward(c, r, m), 0.05f, 0.012f, Rgb(40, 70, 110), MarkShape.Zigzag);
            return b;
        }
        {
            var b = new PokeBuilder(name, 1f, BodyPlan.Quadruped, V(0, 0.32f, 0)) { Coat = Fur };
            var coat = galar ? Rgb(240, 242, 246) : Rgb(200, 70, 60);
            var fur = galar ? Rgb(140, 176, 220) : Rgb(244, 160, 100);
            // Short back legs, and long arms down to the ground on its knuckles
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.1f * s, 0.2f, -0.1f), false);
                var foot = V(0.13f * s, 0.03f, -0.06f);
                b.Limb(leg, V(0.1f * s, 0.21f, -0.1f), foot, 0.06f, 0.045f, coat);
                b.Ell(leg, foot, V(0.05f, 0.03f, 0.065f), fur);
                Digits(b, leg, foot + V(0, -0.005f, 0.06f), V(0, 0, 1f), V(0.018f, 0, 0), 0.02f, 0.012f, fur);
                int arm = b.Leg(s, V(0.15f * s, 0.38f, 0.06f), true);
                var elbow = V(0.22f * s, 0.24f, 0.1f);
                var fist = V(0.21f * s, 0.05f, 0.14f);
                b.Limb(arm, V(0.15f * s, 0.38f, 0.06f), elbow, 0.06f, 0.05f, coat);
                b.Limb(arm, elbow, fist, 0.05f, 0.045f, coat);
                b.Ell(arm, fist, V(0.055f, 0.05f, 0.06f), fur);
            });
            var bc = V(0, 0.32f, 0);
            var br = V(0.17f, 0.15f, 0.15f);
            b.Ell(Body, bc, br, coat);
            b.PaintEll(Body, bc + V(0, -0.06f, 0.08f), V(0.11f, 0.08f, 0.06f), galar ? Rgb(222, 228, 238) : Rgb(226, 110, 90));
            int head = b.Head(V(0, 0.38f, 0.08f));
            var c = V(0, 0.41f, 0.11f);
            var r = V(0.11f, 0.085f, 0.085f);
            b.Ell(head, c, r, coat);
            var g = On(c, r, 0, c.Y - 0.035f);
            Grin(b, head, g, V(0.065f, 0.025f, 0.04f), Rgb(80, 30, 40));
            b.Box(head, g + V(0, 0.012f, -0.012f), V(0.05f, 0.007f, 0.012f), 0.003f, White, mat: Shell, blend: 0.003f);
            b.Box(head, g + V(0, -0.014f, -0.014f), V(0.045f, 0.006f, 0.012f), 0.003f, White, mat: Shell, blend: 0.003f);
            if (galar)
            {
                // A great snowball on its head set with three crystals of ice, a white beard and a crystal on its chest
                var sb = c + V(0, 0.13f, -0.04f);
                var sr = V(0.13f, 0.11f, 0.12f);
                b.Ell(head, sb, sr, White);
                foreach (var d in new[] { V(-0.5f, 0.3f, 1f), V(0, 0.55f, 1f), V(0.5f, 0.3f, 1f) })
                    Gem(b, head, Out(sb, sr, default, d), d, 0.022f, Rgb(120, 214, 228));
                FurTufts(b, head, c + V(0, -0.06f, 0.03f), V(0.08f, 0.03f, 0.05f), 8, 0.035f, 0.016f, White, 1.1f, -1f, 0.4f);
                Gem(b, Body, Out(bc, br, default, V(0, 0.1f, 1f)), V(0, 0.1f, 1f), 0.026f, Rgb(120, 214, 228));
            }
            else
                PokeBuilder.Both(s => FlameRow(b, head, c + V(0.02f * s, 0.055f, 0.05f), c + V(0.085f * s, 0.06f, 0.03f), V(0.6f * s, 1f, -0.2f), 3, 0.15f, 0.026f, flame, heart, 0.8f));
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.045f * s, c.Y + 0.025f);
                b.Mark(head, at, Outward(c, r, at), 0.026f, 0.026f, galar ? Rgb(80, 130, 210) : Rgb(250, 200, 80), MarkShape.Ring);
                b.Eye(head, at, Outward(c, r, at), 0.017f, sclera: true, pupil: Rgb(30, 30, 30), glare: !galar);
            });
            return b;
        }
    }

    private static PokeBuilder Darmanitan() => DarmanitanBuild(false, false);

    // ------------------------------------------------------------------ Maractus

    /// <summary>Maractus: a dancing cactus, green with yellow thorns, ruffs of dark green leaves at its wrists and foot, pink flowers on its head and in one hand, and round yellow eyes.</summary>
    private static PokeBuilder Maractus()
    {
        var b = new PokeBuilder("Maractus", 0.85f, BodyPlan.Biped, V(0, 0.27f, 0)) { Coat = Leaf };
        var green = Rgb(110, 190, 90);
        var dark = Rgb(40, 110, 80);
        var thorn = Rgb(246, 210, 80);
        var pink = Rgb(236, 120, 190);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.16f, 0));
            var foot = V(0.05f * s, 0.04f, 0.01f);
            b.Limb(leg, V(0.035f * s, 0.17f, 0), foot, 0.035f, 0.03f, green);
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 0.9f - 1.35f) + (s > 0 ? 0.5f : -0.5f);
                var d = V(MathF.Sin(a), 0, MathF.Cos(a));
                Blade(b, leg, foot, foot + d * 0.07f + V(0, -0.025f, 0), 0.03f, dark, V(0, 1f, 0), 0.25f, Leaf);
            }
        });
        var bc = V(0, 0.27f, 0);
        var br = V(0.07f, 0.1f, 0.06f);
        b.Ell(Body, bc, br, green);
        foreach (var d in new[] { V(0.7f, 0.2f, 0.7f), V(-0.8f, -0.3f, 0.6f) })
        {
            var at = Out(bc, br, default, d);
            b.Spike(Body, at - Vector3.Normalize(d) * 0.01f, at + Vector3.Normalize(d) * 0.035f, 0.01f, thorn, mat: Shell);
        }
        // Arms flung up and out, a spiked green ball in one hand and a flower in the other
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.06f * s, 0.32f, 0);
            int arm = b.Arm(s, shoulder);
            var hand = s > 0 ? V(0.17f * s, 0.46f, 0.02f) : V(0.18f * s, 0.32f, 0.03f);
            b.Limb(arm, shoulder, hand, 0.028f, 0.026f, green);
            b.Ell(arm, hand, V(0.04f, 0.04f, 0.04f), green);
            var away = Vector3.Normalize(hand - shoulder);
            for (int i = 0; i < 4; i++)
            {
                float a = i * MathF.Tau / 4f;
                var d = Vector3.Normalize(V(MathF.Cos(a), MathF.Sin(a) * 0.4f, MathF.Sin(a)) - away * 0.8f);
                Blade(b, arm, hand - away * 0.03f, hand - away * 0.03f + d * 0.05f, 0.025f, dark, away, 0.25f, Leaf);
            }
            foreach (var d in new[] { V(0.3f * s, 0.9f, 0.3f), V(s, 0.1f, 0.4f) })
                b.Spike(arm, hand + Vector3.Normalize(d) * 0.03f, hand + Vector3.Normalize(d) * 0.065f, 0.01f, thorn, mat: Shell);
            if (s > 0) FlowerHead(b, arm, hand + away * 0.04f, away, 5, 0.06f, 0.03f, pink, thorn, 0.018f);
        });
        int head = b.Head(V(0, 0.36f, 0));
        var c = V(0, 0.43f, 0.01f);
        var r = V(0.065f, 0.06f, 0.055f);
        b.Ell(head, c, r, green);
        FlowerHead(b, head, c + V(-0.03f, 0.07f, -0.01f), V(-0.4f, 1f, 0.2f), 5, 0.06f, 0.03f, pink, thorn, 0.018f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.05f * s, 0.02f, 0), c + V(0.09f * s, 0.04f, 0), 0.01f, thorn, mat: Shell));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, sclera: true, white: Rgb(244, 220, 90), pupil: Rgb(190, 150, 40));
        });
        var m = On(c, r, 0, c.Y - 0.03f);
        b.Mark(head, m, Outward(c, r, m), 0.022f, 0.008f, dark, MarkShape.Zigzag);
        return b;
    }

    // ------------------------------------------------------------------ Dwebble line

    /// <summary>Dwebble: a little orange hermit crab carrying a gray stone on its back, two big black eyes on top and two great pointed claws.</summary>
    private static PokeBuilder Dwebble()
    {
        var b = new PokeBuilder("Dwebble", 0.45f, BodyPlan.Quadruped, V(0, 0.08f, 0.03f)) { Coat = Shell };
        var orange = Rgb(236, 130, 70);
        var stone = Rgb(130, 126, 124);
        var bc = V(0, 0.08f, 0.03f);
        b.Ell(Body, bc, V(0.06f, 0.05f, 0.05f), orange);
        b.Box(Body, V(0, 0.15f, -0.07f), V(0.11f, 0.1f, 0.095f), 0.055f, stone, V(10f, 15f, 5f));
        b.PaintEll(Body, V(0.05f, 0.21f, -0.02f), V(0.014f, 0.01f, 0.014f), Rgb(204, 176, 136));
        foreach (float z in new[] { 0.0f, -0.05f })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.04f * s, 0.06f, z), z > -0.01f);
                b.Limb(leg, V(0.04f * s, 0.06f, z), V(0.09f * s, 0.005f, z + 0.01f), 0.012f, 0.009f, orange);
            });
        // Two great claws held out in front
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.04f * s, 0.08f, 0.06f);
            int arm = b.Arm(s, shoulder);
            var wrist = V(0.08f * s, 0.08f, 0.11f);
            b.Limb(arm, shoulder, wrist, 0.018f, 0.022f, orange);
            b.Ell(arm, wrist, V(0.03f, 0.03f, 0.03f), orange);
            b.Spike(arm, wrist + V(0, -0.01f, 0.01f), wrist + V(0.01f * s, -0.075f, 0.05f), 0.026f, orange);
        });
        int head = b.Head(V(0, 0.11f, 0.05f));
        PokeBuilder.Both(s =>
        {
            var ec = V(0.022f * s, 0.15f, 0.06f);
            var er = V(0.018f, 0.026f, 0.018f);
            b.Limb(head, V(0.015f * s, 0.11f, 0.05f), ec, 0.012f, 0.012f, orange);
            b.Ell(head, ec, er, Rgb(30, 30, 34), mat: Shell, blend: 0.008f);
            var at = Out(ec, er, default, V(0.3f * s, 0.15f, 1f));
            b.Eye(head, at, Outward(ec, er, at), 0.012f, Rgb(20, 20, 24));
        });
        return b;
    }

    /// <summary>Crustle: a red-brown crab carrying a great slab of layered rock on its back, two great dark claws and small eyes ringed in yellow.</summary>
    private static PokeBuilder Crustle()
    {
        var b = new PokeBuilder("Crustle", 1f, BodyPlan.Quadruped, V(0, 0.14f, 0.08f)) { Coat = Shell };
        var red = Rgb(170, 80, 60);
        var dark = Rgb(70, 50, 46);
        var bc = V(0, 0.13f, 0.1f);
        b.Ell(Body, bc, V(0.12f, 0.07f, 0.08f), red);
        b.Ell(Body, V(0, 0.13f, -0.04f), V(0.13f, 0.06f, 0.13f), red);
        // The slab: a block of rock in layers
        b.Box(Body, V(0, 0.4f, -0.06f), V(0.22f, 0.24f, 0.22f), 0.04f, Rgb(130, 100, 80), V(0, 10f, 0));
        foreach (var (y, col) in new[] { (0.24f, Rgb(210, 160, 90)), (0.33f, Rgb(96, 72, 60)), (0.45f, Rgb(220, 172, 100)), (0.56f, Rgb(92, 70, 58)) })
            b.PaintEll(Body, V(0, y, -0.06f), V(0.4f, 0.014f, 0.4f), col, soft: 0.006f);
        foreach (float z in new[] { 0.06f, -0.02f, -0.1f })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.09f * s, 0.12f, z), z > 0);
                b.Limb(leg, V(0.09f * s, 0.12f, z), V(0.19f * s, 0.07f, z + 0.01f), 0.02f, 0.017f, dark);
                b.Spike(leg, V(0.19f * s, 0.07f, z + 0.01f), V(0.21f * s, 0, z + 0.02f), 0.017f, dark);
            });
        // Two great dark claws
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.1f * s, 0.13f, 0.14f);
            int arm = b.Arm(s, shoulder);
            var wrist = V(0.16f * s, 0.12f, 0.24f);
            b.Limb(arm, shoulder, wrist, 0.035f, 0.03f, red);
            b.Ell(arm, wrist + V(0.01f * s, -0.01f, 0.04f), V(0.05f, 0.045f, 0.055f), dark);
            b.Spike(arm, wrist + V(0.015f * s, -0.02f, 0.07f), wrist + V(0.01f * s, -0.12f, 0.12f), 0.035f, dark);
            b.Spike(arm, wrist + V(-0.02f * s, -0.02f, 0.07f), wrist + V(-0.05f * s, -0.08f, 0.12f), 0.022f, dark);
        });
        int head = b.Head(V(0, 0.15f, 0.15f));
        var c = V(0, 0.15f, 0.16f);
        var r = V(0.06f, 0.035f, 0.035f);
        b.Ell(head, c, r, red);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, white: Rgb(244, 200, 70), pupil: Rgb(30, 26, 26));
        });
        return b;
    }

    // ------------------------------------------------------------------ Scraggy line

    /// <summary>Scraggy: a yellow lizard in loose skin like baggy trousers pulled up to its neck and trailing behind, a red crest on its big head, big eyes, a grimace of teeth and a red shirt of scales.</summary>
    private static PokeBuilder Scraggy()
    {
        var b = new PokeBuilder("Scraggy", 0.6f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 220, 120);
        var pants = Rgb(222, 182, 104);
        var red = Rgb(220, 100, 90);
        var orange = Rgb(240, 160, 80);
        b.Ell(Body, V(0, 0.1f, 0), V(0.095f, 0.08f, 0.08f), pants);
        b.Ell(Body, V(0, 0.18f, 0), V(0.06f, 0.07f, 0.05f), yellow);
        b.PaintEll(Body, V(0, 0.17f, 0.04f), V(0.026f, 0.065f, 0.02f), red);
        foreach (float y in new[] { 0.14f, 0.165f, 0.19f, 0.215f })
            b.PaintEll(Body, V(0, y, 0.05f), V(0.03f, 0.0025f, 0.02f), Rgb(150, 60, 60), soft: 0.003f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.05f, 0.02f));
            b.Limb(leg, V(0.045f * s, 0.05f, 0.02f), V(0.05f * s, 0.025f, 0.04f), 0.02f, 0.018f, pants);
            b.Ell(leg, V(0.05f * s, 0.018f, 0.05f), V(0.025f, 0.018f, 0.035f), orange);
            var shoulder = V(0.055f * s, 0.2f, 0.01f);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.085f * s, 0.14f, 0.05f);
            b.Limb(arm, shoulder, hand, 0.015f, 0.014f, yellow);
            b.Ell(arm, hand, V(0.017f, 0.017f, 0.017f), yellow);
        });
        int tail = b.Tail(V(0, 0.08f, -0.06f));
        b.Limb(tail, V(0, 0.08f, -0.06f), V(0.03f, 0.018f, -0.17f), 0.035f, 0.016f, pants);
        b.Spike(tail, V(0.03f, 0.02f, -0.16f), V(0.04f, 0.04f, -0.22f), 0.012f, red);
        int head = b.Head(V(0, 0.23f, 0.01f));
        var c = V(0, 0.29f, 0.02f);
        var r = V(0.075f, 0.07f, 0.065f);
        b.Ell(head, c, r, yellow);
        b.Spike(head, c + V(0, 0.05f, -0.01f), c + V(0, 0.11f, 0.01f), 0.022f, red, 0.6f);
        var t = On(c, r, 0, c.Y - 0.035f);
        b.Box(head, t, V(0.035f, 0.012f, 0.008f), 0.004f, White, mat: Shell, blend: 0.004f);
        b.PaintEll(head, t + V(0, 0, 0.004f), V(0.04f, 0.002f, 0.02f), Rgb(80, 60, 50), soft: 0.002f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.015f);
            b.Eye(head, at, Outward(c, r, at), 0.022f, sclera: true, pupil: Rgb(30, 30, 30));
        });
        return b;
    }

    /// <summary>
    /// Scrafty and its Mega Evolution: an orange tough in yellow skin like baggy trousers pulled up into a hood about
    /// its neck, a tall red crest, a gray scaly chest, heavy-lidded eyes and a grimace of teeth; the Mega's skin white
    /// and grown into a great hood over its head and ragged trousers trailing to the ground.
    /// </summary>
    private static PokeBuilder ScraftyBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Scrafty-Mega" : "Scrafty", 0.85f, BodyPlan.Biped, V(0, 0.26f, 0)) { Coat = Fur };
        var orange = Rgb(236, 130, 70);
        var skin = mega ? Rgb(236, 236, 232) : Rgb(226, 186, 100);
        var red = Rgb(196, 70, 60);
        var gray = Rgb(110, 110, 116);
        // Baggy trousers, ragged at the hem, then the legs and feet under them
        b.Ell(Body, V(0, 0.15f, 0), V(0.13f, 0.11f, 0.11f), skin);
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f;
            b.Cut(Body, V(MathF.Sin(a) * 0.12f, 0.04f, MathF.Cos(a) * 0.1f), V(0.03f, 0.04f, 0.03f));
        }
        if (mega)
            PokeBuilder.Both(s => Blade(b, Body, V(0.08f * s, 0.12f, -0.04f), V(0.14f * s, 0.01f, -0.1f), 0.05f, skin, V(s, 0, -0.3f), 0.3f));
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.1f, 0.01f));
            b.Limb(leg, V(0.06f * s, 0.1f, 0.01f), V(0.07f * s, 0.03f, 0.03f), 0.025f, 0.022f, orange);
            b.Ell(leg, V(0.07f * s, 0.022f, 0.045f), V(0.032f, 0.022f, 0.045f), orange);
        });
        b.Ell(Body, V(0, 0.3f, 0), V(0.075f, 0.1f, 0.065f), orange);
        b.PaintEll(Body, V(0, 0.29f, 0.05f), V(0.045f, 0.08f, 0.03f), gray);
        foreach (float y in new[] { 0.25f, 0.28f, 0.31f, 0.34f })
            b.PaintEll(Body, V(0, y, 0.065f), V(0.05f, 0.003f, 0.02f), Rgb(70, 70, 76), soft: 0.003f);
        b.Torus(Body, V(0, 0.38f, 0), 0.065f, 0.035f, skin, sz: 0.9f, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.07f * s, 0.34f, 0.01f);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.13f * s, 0.22f, 0.05f);
            b.Limb(arm, shoulder, hand, 0.025f, 0.022f, orange);
            b.Ell(arm, hand, V(0.027f, 0.027f, 0.027f), orange);
        });
        int tail = b.Tail(V(0, 0.2f, -0.07f));
        b.Limb(tail, V(0, 0.2f, -0.07f), V(0, 0.16f, -0.14f), 0.02f, 0.012f, orange);
        b.Spike(tail, V(0, 0.16f, -0.14f), V(0, 0.18f, -0.19f), 0.014f, red);
        int head = b.Head(V(0, 0.42f, 0.02f));
        var c = V(0, 0.48f, 0.03f);
        var r = V(0.065f, 0.065f, 0.06f);
        if (mega)
        {
            // A great hood over its head, open at the face
            b.Ell(head, c + V(0, 0.025f, -0.03f), V(0.095f, 0.1f, 0.09f), skin);
            b.Cut(head, c + V(0, -0.005f, 0.07f), V(0.065f, 0.07f, 0.06f));
        }
        b.Ell(head, c, r, orange);
        foreach (float z in new[] { 0.02f, -0.02f, -0.06f })
            Blade(b, head, c + V(0, 0.04f, z), c + V(0, 0.15f - MathF.Abs(z) * 0.5f, z - 0.02f), 0.035f, red, V(1f, 0, 0), 0.35f);
        var t = On(c, r, 0, c.Y - 0.035f);
        b.Box(head, t, V(0.03f, 0.011f, 0.008f), 0.004f, White, mat: Shell, blend: 0.004f);
        b.PaintEll(head, t + V(0, 0, 0.004f), V(0.035f, 0.002f, 0.02f), Rgb(80, 60, 50), soft: 0.002f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, sclera: true, pupil: Rgb(30, 30, 30), glare: true);
        });
        return b;
    }

    private static PokeBuilder Scrafty() => ScraftyBuild(false);

    // ------------------------------------------------------------------ Sigilyph

    /// <summary>Sigilyph: a bird like a totem, a black body ringed in a green zigzag with two eyespots, a long black head with one cyan eye, fans of yellow feathers banded red and blue for wings and tail, and two three-toed black claws.</summary>
    private static PokeBuilder Sigilyph()
    {
        var b = new PokeBuilder("Sigilyph", 1f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var black = Rgb(40, 40, 46);
        var green = Rgb(80, 180, 110);
        var yellow = Rgb(246, 206, 80);
        var red = Rgb(220, 70, 70);
        var blue = Rgb(110, 170, 230);
        var c = V(0, 0.42f, 0);
        var r = V(0.1f, 0.1f, 0.1f);
        b.Ell(Body, c, r, black);
        b.PaintTorus(Body, c, 0.1f, 0.024f, green);
        b.PaintTorus(Body, c + V(0, 0.065f, 0), 0.077f, 0.01f, White);
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f;
            var at = Out(c, r, default, V(MathF.Sin(a), 0, MathF.Cos(a)));
            b.Mark(Body, at, Outward(c, r, at), 0.04f, 0.014f, black, MarkShape.Zigzag);
        }
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.4f * s, -0.45f, 1f));
            b.Mark(Body, at, Outward(c, r, at), 0.026f, 0.026f, blue, MarkShape.Ring);
            b.Mark(Body, at, Outward(c, r, at), 0.011f, 0.011f, White);
            Digits(b, Body, c + V(0.09f * s, -0.04f, 0.02f), V(s, -0.2f, 0.2f), V(0, 0.02f, 0.01f), 0.05f, 0.01f, black);
        });
        // A long black head on a short neck, one cyan eye in a white ring
        int head = b.Head(c + V(0, 0.09f, 0));
        b.Limb(head, c + V(0, 0.08f, 0), c + V(0, 0.15f, 0), 0.025f, 0.02f, black);
        var hc = c + V(0, 0.2f, 0.01f);
        var hr = V(0.035f, 0.07f, 0.035f);
        b.Ell(head, hc, hr, black);
        b.Spike(head, hc + V(0, 0.05f, -0.005f), hc + V(0, 0.1f, 0.02f), 0.022f, black);
        var eye = On(hc, hr, 0, hc.Y - 0.01f);
        b.Mark(head, eye, Outward(hc, hr, eye), 0.022f, 0.022f, White, MarkShape.Ring);
        b.Eye(head, eye, Outward(hc, hr, eye), 0.013f, Rgb(80, 200, 220));
        // Wings: fans of four feathers each, banded blue and red toward their tips
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.08f * s, 0.05f, -0.02f);
            int wing = b.Wing(s, root);
            for (int i = 0; i < 4; i++)
            {
                float a = (22f + i * 18f) * Degree;
                var tip = root + V(MathF.Cos(a) * s, MathF.Sin(a), -0.15f) * (0.27f - i * 0.02f);
                Frond(b, wing, root, tip, 0.022f, yellow, V(0, 0, 1f), 0.3f);
                foreach (var (t, col) in new[] { (0.6f, blue), (0.74f, red), (0.88f, blue) })
                    b.PaintEll(wing, Vector3.Lerp(root, tip, t), V(0.026f, 0.012f, 0.026f), col, Euler(tip - root), 0.004f);
            }
        });
        // A tail of four feathers hanging below
        int tail = b.Tail(c + V(0, -0.08f, 0));
        foreach (float x in new[] { -0.045f, -0.015f, 0.015f, 0.045f })
        {
            var root = c + V(x * 0.5f, -0.07f, 0);
            var tip = c + V(x, -0.32f, 0.02f);
            Frond(b, tail, root, tip, 0.017f, yellow, V(0, 0, 1f), 0.3f);
            foreach (var (t, col) in new[] { (0.62f, blue), (0.76f, red), (0.9f, blue) })
                b.PaintEll(tail, Vector3.Lerp(root, tip, t), V(0.02f, 0.01f, 0.02f), col, soft: 0.004f);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Yamask line

    /// <summary>
    /// Yamask and its Galarian form: a small black shade with a hooded head, two big teary red eyes, two long arms
    /// curling to three-fingered fists, and a long thin tail holding the golden mask of its human face; the
    /// Galarian's eyes purple, its tail holding a slab of stone marked with a red sign.
    /// </summary>
    private static PokeBuilder YamaskBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Yamask-Galar" : "Yamask", 0.6f, BodyPlan.Floating, V(0, 0.27f, 0)) { Coat = Fur }.Hover();
        var black = Rgb(56, 54, 62);
        var eye = galar ? Rgb(160, 100, 210) : Rgb(220, 60, 60);
        b.Ell(Body, V(0, 0.27f, 0), V(0.035f, 0.04f, 0.03f), black);
        // A hood of a head, rounded in front and rising to a point behind
        int head = b.Head(V(0, 0.3f, 0));
        var c = V(0, 0.34f, 0.02f);
        var r = V(0.07f, 0.065f, 0.06f);
        b.Ell(head, c, r, black);
        b.Spike(head, c + V(0, 0.03f, -0.02f), c + V(0.02f, 0.12f, -0.06f), 0.05f, black, 0.7f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.008f);
            b.Mark(head, at, Outward(c, r, at), 0.024f, 0.024f, eye);
            b.Eye(head, at, Outward(c, r, at), 0.014f, sclera: true, white: eye, pupil: Rgb(60, 20, 30));
            var tear = On(c, r, 0.04f * s, c.Y - 0.022f);
            b.Mark(head, tear, Outward(c, r, tear), 0.007f, 0.011f, eye);
        });
        // Long arms curling out to three-fingered fists
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.03f * s, 0.27f, 0));
            var path = Smooth(3, V(0.03f * s, 0.27f, 0), V(0.12f * s, 0.28f, 0.01f), V(0.2f * s, 0.25f, 0.02f), V(0.24f * s, 0.22f, 0.03f));
            b.Tube(arm, path, 0.016f, 0.02f, black, blend: 0f);
            var fist = path[^1] + V(0.02f * s, 0, 0);
            b.Ell(arm, fist, V(0.03f, 0.028f, 0.028f), black);
            Digits(b, arm, fist + V(0.015f * s, 0.01f, 0.02f), V(0, -0.3f, 1f), V(0, 0.012f, 0), 0.02f, 0.01f, black);
        });
        // The tail, and what it holds
        int tail = b.Tail(V(0, 0.24f, 0));
        b.Tube(tail, Smooth(2, V(0, 0.25f, 0), V(0.01f, 0.18f, 0.01f), V(0, 0.12f, 0.02f)), 0.012f, 0.01f, black, blend: 0f);
        if (galar)
        {
            b.Box(tail, V(0, 0.07f, 0.03f), V(0.055f, 0.045f, 0.03f), 0.01f, Rgb(176, 172, 168), V(0, 0, -8f), Shell);
            b.Mark(tail, V(0, 0.07f, 0.06f), V(0, 0, 1f), 0.035f, 0.03f, Rgb(200, 50, 50), MarkShape.Wave);
        }
        else
        {
            var mc = V(0, 0.07f, 0.03f);
            var mr = V(0.045f, 0.06f, 0.02f);
            var line = Rgb(160, 120, 50);
            b.Ell(tail, mc, mr, Rgb(230, 190, 90), mat: Metal);
            b.Mark(tail, On(mc, mr, 0, mc.Y + 0.018f), V(0, 0.2f, 1f), 0.03f, 0.005f, line, MarkShape.Bar);
            b.Mark(tail, On(mc, mr, 0, mc.Y - 0.028f), V(0, -0.2f, 1f), 0.014f, 0.004f, line, MarkShape.Bar);
            b.PaintEll(tail, mc + V(0, -0.004f, 0.02f), V(0.004f, 0.02f, 0.01f), Rgb(200, 160, 70), soft: 0.004f);
        }
        return Lift(b);
    }

    private static PokeBuilder Yamask() => YamaskBuild(false);

    /// <summary>Cofagrigus: a golden coffin with a face, its headdress banded blue and gold, red eyes over a grin of teeth, and four black shadow arms with great hands reaching from its back.</summary>
    private static PokeBuilder Cofagrigus()
    {
        var b = new PokeBuilder("Cofagrigus", 1f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Metal }.Hover();
        var gold = Rgb(230, 186, 80);
        var deep = Rgb(176, 130, 46);
        var navy = Rgb(40, 60, 100);
        var shade = Rgb(60, 60, 66);
        var c = V(0, 0.4f, 0);
        b.Box(Body, c, V(0.15f, 0.32f, 0.1f), 0.075f, gold);
        foreach (float x in new[] { -0.045f, 0f, 0.045f })
            b.PaintEll(Body, c + V(x, -0.1f, 0.09f), V(0.004f, 0.2f, 0.03f), deep, soft: 0.005f);
        foreach (float y in new[] { -0.2f, -0.06f })
            b.PaintEll(Body, c + V(0, y, 0), V(0.2f, 0.005f, 0.2f), deep, soft: 0.005f);
        // The head end: a navy headdress banded in gold round a black face
        int head = b.Head(c + V(0, 0.2f, 0.02f));
        var hc = c + V(0, 0.23f, 0.03f);
        var hr = V(0.15f, 0.12f, 0.09f);
        b.Ell(head, hc, hr, navy);
        foreach (float y in new[] { -0.06f, 0.04f, 0.075f })
            b.PaintEll(head, hc + V(0, y, 0), V(0.2f, 0.008f, 0.2f), gold, soft: 0.005f);
        b.PaintEll(head, hc + V(0, -0.03f, 0.08f), V(0.07f, 0.065f, 0.04f), Rgb(40, 36, 44));
        b.Mark(head, On(hc, hr, 0, hc.Y + 0.085f), V(0, 0.6f, 1f), 0.016f, 0.014f, gold);
        var t = On(hc, hr, 0, hc.Y - 0.065f);
        b.Box(head, t, V(0.035f, 0.01f, 0.008f), 0.003f, White, mat: Shell, blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.03f * s, hc.Y - 0.015f);
            b.Eye(head, at, Outward(hc, hr, at), 0.016f, Rgb(230, 50, 50), glare: true);
        });
        // Four shadow arms from its back, each ending in a great flat hand
        foreach (var (y, up) in new[] { (0.62f, true), (0.26f, false) })
            PokeBuilder.Both(s =>
            {
                var root = V(0.08f * s, y, -0.07f);
                int arm = up ? b.Arm(s, root) : b.Part(s < 0 ? "lowArmL" : "lowArmR", Body, root, PokeRole.Arm, 0.5f, s);
                var path = Smooth(3, root, root + V(0.12f * s, up ? 0.06f : -0.02f, -0.05f), root + V(0.26f * s, up ? 0.04f : -0.1f, -0.06f), root + V(0.34f * s, up ? 0.14f : -0.16f, -0.04f));
                b.Tube(arm, path, 0.028f, 0.02f, shade, Fur, 0f);
                var d = Vector3.Normalize(path[^1] - path[^2]);
                var palm = path[^1] + d * 0.025f;
                var u = Vector3.Normalize(V(0, 0, 1f) - d * d.Z);
                var w = Vector3.Cross(d, u);
                b.Ell(arm, palm, V(0.04f, 0.045f, 0.014f), shade, Euler(d), Fur);
                for (int k = 0; k < 4; k++)
                {
                    float a = (-45f + k * 30f) * Degree;
                    var f = d * MathF.Cos(a) + w * MathF.Sin(a);
                    b.Limb(arm, palm + f * 0.03f, palm + f * 0.09f, 0.011f, 0.008f, shade, Fur, 0.008f);
                }
            });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Tirtouga line

    /// <summary>Tirtouga: a little ancient sea turtle, a dark blue shell with round plates and two small horns at its front, a pale blue head under a dark cap, a beak and broad flippers.</summary>
    private static PokeBuilder Tirtouga()
    {
        var b = new PokeBuilder("Tirtouga", 0.65f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Shell };
        var navy = Rgb(60, 80, 130);
        var light = Rgb(110, 170, 220);
        var sc = V(0, 0.15f, -0.02f);
        var sr = V(0.15f, 0.1f, 0.17f);
        b.Ell(Body, sc, sr, navy);
        b.Ell(Body, sc + V(0, -0.04f, 0.02f), V(0.13f, 0.06f, 0.15f), light, mat: Fur);
        foreach (var d in new[] { V(0, 1f, -0.3f), V(0.6f, 0.8f, 0.2f), V(-0.6f, 0.8f, 0.2f), V(0.5f, 0.7f, -0.6f), V(-0.5f, 0.7f, -0.6f) })
        {
            var at = Out(sc, sr, default, d);
            b.Mark(Body, at, Outward(sc, sr, at), 0.03f, 0.03f, Rgb(40, 56, 96), MarkShape.Ring);
        }
        PokeBuilder.Both(s => b.Spike(Body, sc + V(0.09f * s, 0.05f, 0.11f), sc + V(0.13f * s, 0.12f, 0.19f), 0.022f, navy));
        foreach (var (z, front) in new[] { (0.08f, true), (-0.12f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = V(0.1f * s, 0.1f, z);
                int leg = b.Leg(s, hip, front);
                var tip = front ? V(0.3f * s, 0.02f, z - 0.06f) : V(0.18f * s, 0.02f, z - 0.08f);
                Frond(b, leg, hip, tip, front ? 0.05f : 0.035f, light, V(0, 1f, 0.1f), 0.25f, Fur);
            });
        int head = b.Head(V(0, 0.13f, 0.14f));
        var c = V(0, 0.15f, 0.21f);
        var r = V(0.065f, 0.055f, 0.07f);
        b.Ell(head, c, r, light, mat: Fur);
        b.PaintEll(head, c + V(0, 0.035f, -0.01f), V(0.06f, 0.025f, 0.06f), navy);
        b.Spike(head, c + V(0, -0.005f, 0.045f), c + V(0, -0.03f, 0.075f), 0.018f, navy, 0.7f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, pupil: Rgb(30, 30, 30), glare: true);
        });
        int tail = b.Tail(sc + V(0, -0.02f, -0.15f));
        b.Spike(tail, sc + V(0, -0.02f, -0.14f), sc + V(0, -0.05f, -0.24f), 0.025f, light, mat: Fur);
        return b;
    }

    /// <summary>Carracosta: an ancient sea turtle standing up, a dark blue shell over its back and plates over its front round a pale belly, great flipper-arms reaching to the ground, and a pale blue head under a dark helmet with a beak.</summary>
    private static PokeBuilder Carracosta()
    {
        var b = new PokeBuilder("Carracosta", 0.95f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Shell };
        var navy = Rgb(56, 80, 140);
        var blue = Rgb(110, 170, 230);
        var pale = Rgb(170, 210, 236);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.16f, 0));
            var foot = V(0.1f * s, 0.025f, 0.03f);
            b.Limb(leg, V(0.08f * s, 0.17f, 0), foot + V(0, 0.02f, 0), 0.05f, 0.04f, blue, Fur);
            b.Ell(leg, foot, V(0.045f, 0.025f, 0.06f), blue, mat: Fur);
            Claws(b, leg, foot + V(0, -0.005f, 0.055f), V(0.016f, 0, 0), V(0, -0.2f, 1f), 0.02f, 0.008f);
        });
        b.Ell(Body, V(0, 0.32f, 0.02f), V(0.13f, 0.16f, 0.11f), navy);
        b.PaintEll(Body, V(0, 0.3f, 0.1f), V(0.06f, 0.09f, 0.04f), pale);
        b.Ell(Body, V(0, 0.36f, -0.07f), V(0.16f, 0.19f, 0.1f), navy);
        foreach (var d in new[] { V(0, 0.4f, -1f), V(0.6f, 0.2f, -0.8f), V(-0.6f, 0.2f, -0.8f), V(0, -0.3f, -1f) })
        {
            var at = Out(V(0, 0.36f, -0.07f), V(0.16f, 0.19f, 0.1f), default, d);
            b.Mark(Body, at, Vector3.Normalize(d), 0.035f, 0.03f, Rgb(40, 60, 110), MarkShape.Diamond);
        }
        // Great flipper-arms, reaching to the ground in front
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.13f * s, 0.42f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.22f * s, 0.3f, 0.06f);
            var wrist = V(0.24f * s, 0.16f, 0.1f);
            b.Limb(arm, shoulder, elbow, 0.055f, 0.045f, blue, Fur);
            b.Limb(arm, elbow, wrist, 0.045f, 0.04f, blue, Fur);
            Frond(b, arm, wrist + V(0, 0.03f, 0), V(0.25f * s, 0.01f, 0.2f), 0.07f, blue, V(s, 0, 0.3f), 0.3f, Fur);
        });
        int tail = b.Tail(V(0, 0.18f, -0.1f));
        b.Spike(tail, V(0, 0.18f, -0.1f), V(0, 0.07f, -0.26f), 0.04f, blue, 0.6f, Fur);
        // A pale blue head under a dark helmet, and a beak
        int head = b.Head(V(0, 0.46f, 0.04f));
        var c = V(0, 0.53f, 0.07f);
        var r = V(0.078f, 0.072f, 0.08f);
        b.Ell(head, c, r, blue, mat: Fur);
        b.Ell(head, c + V(0, 0.04f, -0.025f), V(0.085f, 0.055f, 0.08f), navy, blend: 0.01f);
        b.Spike(head, c + V(0, 0.05f, -0.04f), c + V(0, 0.07f, -0.12f), 0.03f, navy, 0.5f);
        b.Spike(head, c + V(0, -0.01f, 0.06f), c + V(0, -0.04f, 0.09f), 0.02f, navy, 0.7f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, pupil: Rgb(30, 30, 30), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Archen line

    /// <summary>Archen: a little first bird, yellow with a red chest and a red head, blue behind its eye, wings of yellow feathers tipped blue with claws at the bend, red legs and a long tail ending in a blue diamond.</summary>
    private static PokeBuilder Archen()
    {
        var b = new PokeBuilder("Archen", 0.6f, BodyPlan.Bird, V(0, 0.18f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 210, 90);
        var red = Rgb(220, 80, 70);
        var blue = Rgb(60, 140, 220);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.13f, 0));
            var ankle = V(0.035f * s, 0.025f, 0.01f);
            b.Limb(leg, V(0.03f * s, 0.13f, 0), ankle, 0.014f, 0.01f, red, Scales);
            foreach (float dx in new[] { -0.015f, 0f, 0.015f })
                b.Limb(leg, ankle, ankle + V(dx * s, -0.018f, 0.03f), 0.007f, 0.006f, red, Scales, 0.005f);
        });
        b.Ell(Body, V(0, 0.18f, 0), V(0.06f, 0.06f, 0.075f), yellow, V(-20f, 0, 0));
        b.PaintEll(Body, V(0, 0.2f, 0.05f), V(0.05f, 0.04f, 0.04f), red);
        FurTufts(b, Body, V(0, 0.21f, 0.06f), V(0.04f, 0.03f, 0.02f), 6, 0.025f, 0.01f, red, 1.1f, -0.8f, 0.3f);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.05f * s, 0.21f, 0.02f);
            int wing = b.Wing(s, shoulder);
            var wrist = V(0.12f * s, 0.17f, 0.07f);
            b.Limb(wing, shoulder, wrist, 0.015f, 0.012f, yellow);
            Digits(b, wing, wrist, V(0.4f * s, -0.4f, 1f), V(0, 0.01f, 0), 0.02f, 0.006f, Claw, Shell);
            foreach (var (o, len) in new[] { (0f, 0.13f), (0.025f, 0.11f), (0.05f, 0.09f) })
            {
                var root = Vector3.Lerp(wrist, shoulder, o / 0.06f);
                var tip = root + V(0.06f * s, -0.06f, -0.08f) * (len / 0.12f);
                Frond(b, wing, root, tip, 0.025f, yellow, V(0, 1f, 0.4f), 0.25f);
                b.PaintEll(wing, Vector3.Lerp(root, tip, 0.85f), V(0.03f, 0.03f, 0.03f), blue, soft: 0.006f);
            }
        });
        int tail = b.Tail(V(0, 0.16f, -0.06f));
        var tp = Smooth(3, V(0, 0.16f, -0.06f), V(0, 0.15f, -0.16f), V(0, 0.2f, -0.26f), V(0, 0.27f, -0.32f));
        b.Tube(tail, tp, 0.02f, 0.008f, red, blend: 0f);
        Frond(b, tail, tp[^1], tp[^1] + V(0, 0.08f, -0.05f), 0.035f, blue, V(1f, 0, 0), 0.25f);
        int head = b.Head(V(0, 0.24f, 0.04f));
        var c = V(0, 0.28f, 0.06f);
        var r = V(0.05f, 0.045f, 0.055f);
        b.Ell(head, c, r, red);
        b.Ell(head, c + V(0, -0.02f, 0.05f), V(0.03f, 0.022f, 0.04f), red);
        b.PaintEll(head, c + V(0, -0.03f, 0.04f), V(0.035f, 0.015f, 0.06f), yellow);
        b.PaintEll(head, c + V(0, 0.005f, -0.04f), V(0.055f, 0.05f, 0.03f), blue);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.02f * s, -0.035f, 0.08f), c + V(0.022f * s, -0.05f, 0.08f), 0.005f, White, mat: Shell, blend: 0.003f);
            var at = On(c, r, 0.035f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, sclera: true, pupil: Rgb(30, 30, 30));
        });
        return b;
    }

    /// <summary>Archeops: the first bird in flight, yellow with ruffled feathers, its head red under a teal crown and its beak open on teeth, broad wings of yellow feathers tipped blue with claws at the bend, red legs and a long red tail ending in a fan of blue feathers.</summary>
    private static PokeBuilder Archeops()
    {
        var b = new PokeBuilder("Archeops", 1f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var yellow = Rgb(246, 210, 90);
        var red = Rgb(220, 80, 70);
        var blue = Rgb(60, 140, 220);
        var teal = Rgb(70, 170, 140);
        DanglingLegs(b, 0.04f, 0.36f, 0.0f, 0.1f, 0.014f, red);
        var bc = V(0, 0.42f, 0);
        b.Ell(Body, bc, V(0.08f, 0.09f, 0.1f), yellow, V(-25f, 0, 0));
        FurTufts(b, Body, bc + V(0, 0.02f, 0.04f), V(0.07f, 0.07f, 0.07f), 12, 0.04f, 0.014f, yellow, 0.3f, -0.8f, 0.3f);
        FurTufts(b, Body, bc + V(0, 0.08f, 0.06f), V(0.05f, 0.03f, 0.03f), 8, 0.03f, 0.012f, teal, 1.1f, -0.8f, 0.3f);
        // Broad wings: yellow feathers tipped in blue, claws at the bend
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.06f * s, 0.06f, 0);
            int wing = b.Wing(s, shoulder);
            var wrist = bc + V(0.22f * s, 0.16f, 0.01f);
            b.Limb(wing, shoulder, wrist, 0.022f, 0.016f, yellow);
            Digits(b, wing, wrist, V(0.2f * s, 0.6f, 1f), V(0, 0.012f, 0), 0.03f, 0.008f, Claw, Shell);
            for (int i = 0; i < 5; i++)
            {
                float a = (70f - i * 22f) * Degree;
                var tip = wrist + V(MathF.Cos(a) * s, MathF.Sin(a), -0.15f) * (0.2f - i * 0.012f);
                Frond(b, wing, wrist, tip, 0.032f, yellow, V(0, 0.2f, 1f), 0.25f);
                b.PaintEll(wing, Vector3.Lerp(wrist, tip, 0.85f), V(0.04f, 0.04f, 0.04f), blue, soft: 0.006f);
            }
            for (int i = 0; i < 3; i++)
            {
                var root = Vector3.Lerp(shoulder, wrist, 0.3f + i * 0.25f);
                var tip = root + V(0.04f * s, -0.13f, -0.06f);
                Frond(b, wing, root, tip, 0.028f, yellow, V(0, 0.2f, 1f), 0.25f);
                b.PaintEll(wing, Vector3.Lerp(root, tip, 0.85f), V(0.035f, 0.035f, 0.035f), blue, soft: 0.006f);
            }
        });
        // A long red tail, a fan of blue feathers at its end
        int tail = b.Tail(bc + V(0, -0.05f, -0.07f));
        var tp = Smooth(3, bc + V(0, -0.05f, -0.07f), bc + V(0, -0.15f, -0.16f), bc + V(0.02f, -0.28f, -0.16f), bc + V(0.05f, -0.36f, -0.08f));
        b.Tube(tail, tp, 0.03f, 0.012f, red, blend: 0f);
        foreach (var d in new[] { V(-0.06f, -0.05f, 0.04f), V(0.0f, -0.08f, 0.05f), V(0.06f, -0.05f, 0.04f) })
            Frond(b, tail, tp[^1], tp[^1] + d, 0.025f, blue, V(0, 0, 1f), 0.25f);
        // A red head under a teal crown, the beak open on teeth
        b.Tube(Body, new[] { bc + V(0, 0.06f, 0.06f), bc + V(0, 0.13f, 0.1f), bc + V(0, 0.19f, 0.13f) }, 0.035f, 0.03f, red, blend: 0.01f);
        int head = b.Head(bc + V(0, 0.19f, 0.13f));
        var c = bc + V(0, 0.23f, 0.16f);
        var r = V(0.05f, 0.045f, 0.055f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, 0.03f, -0.02f), V(0.055f, 0.03f, 0.05f), teal);
        b.Ell(head, c + V(0, -0.005f, 0.06f), V(0.028f, 0.018f, 0.05f), red);
        int jaw = b.Jaw(head, c + V(0, -0.02f, 0.02f));
        b.Ell(jaw, c + V(0, -0.04f, 0.055f), V(0.024f, 0.012f, 0.045f), red, V(15f, 0, 0));
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.018f * s, -0.018f, 0.08f), c + V(0.02f * s, -0.034f, 0.08f), 0.005f, White, mat: Shell, blend: 0.003f);
            var at = On(c, r, 0.035f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, sclera: true, pupil: Rgb(30, 30, 30), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Trubbish line

    /// <summary>Trubbish: a green rubbish bag tied at the top, round eyes, a jagged grin, stubby arms of brown rubbish speckled pink and teal, and little feet.</summary>
    private static PokeBuilder Trubbish()
    {
        var b = new PokeBuilder("Trubbish", 0.6f, BodyPlan.Biped, V(0, 0.14f, 0)) { Coat = Shell };
        var green = Rgb(90, 130, 90);
        var dark = Rgb(60, 96, 66);
        var brown = Rgb(150, 126, 100);
        var pink = Rgb(230, 120, 170);
        var teal = Rgb(70, 190, 180);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.05f, 0.01f));
            b.Ell(leg, V(0.06f * s, 0.02f, 0.03f), V(0.03f, 0.02f, 0.035f), dark);
        });
        var c = V(0, 0.14f, 0);
        var r = V(0.13f, 0.11f, 0.11f);
        b.Ell(Body, c, r, green);
        foreach (var (x, a) in new[] { (-0.05f, 25f), (0.06f, -30f), (0.01f, 70f) })
            b.PaintEll(Body, c + V(x, 0.03f, 0.08f), V(0.003f, 0.05f, 0.04f), dark, V(0, 0, a), 0.003f);
        // Tied at the top, the bag's ends sticking up and out
        int head = b.Head(V(0, 0.23f, 0));
        b.Spike(head, V(0, 0.22f, 0), V(-0.01f, 0.36f, -0.02f), 0.04f, green, 0.6f);
        Frond(b, head, V(0.02f, 0.24f, 0), V(0.14f, 0.3f, -0.03f), 0.04f, green, V(0, 0, 1f), 0.25f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.05f * s, c.Y + 0.02f);
            b.Eye(Body, at, Outward(c, r, at), 0.024f, sclera: true, pupil: Rgb(30, 30, 30));
            int arm = b.Arm(s, V(0.11f * s, 0.12f, 0.02f));
            var tip = V(0.21f * s, 0.06f, 0.05f);
            b.Tube(arm, Smooth(2, V(0.1f * s, 0.12f, 0.02f), V(0.16f * s, 0.1f, 0.04f), tip), 0.03f, 0.026f, brown, Fur, 0.01f);
            b.Ell(arm, tip, V(0.035f, 0.03f, 0.033f), brown, mat: Fur);
            b.Mark(arm, tip + V(0.01f * s, 0.022f, 0.01f), V(0.2f * s, 1f, 0.3f), 0.012f, 0.01f, pink);
            b.Ell(arm, tip + V(0.03f * s, -0.005f, 0.01f), V(0.014f, 0.014f, 0.014f), teal, mat: Shell, blend: 0.006f);
        });
        var m = On(c, r, 0, c.Y - 0.035f);
        b.PaintEll(Body, m, V(0.05f, 0.016f, 0.02f), Rgb(50, 60, 50));
        b.Mark(Body, m, Outward(c, r, m), 0.045f, 0.012f, White, MarkShape.Zigzag);
        return b;
    }

    /// <summary>
    /// Garbodor and its Gigantamax form: a heap of rubbish, brown sludge under a green coat that drips down it,
    /// speckled pink and teal, long arms ending in pipes, puffs of rubbish on its head and a gaping mouth of teeth;
    /// the Gigantamax a great dark mound of toxic slime with junk stuck in it, pink eyes over a gaping mouth, and the
    /// red clouds of Gigantamax energy over it.
    /// </summary>
    private static PokeBuilder GarbodorBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Garbodor-Gmax" : "Garbodor", 1f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var brown = gmax ? Rgb(110, 100, 90) : Rgb(150, 136, 116);
        var coat = gmax ? Rgb(50, 70, 56) : Rgb(70, 120, 70);
        var pink = Rgb(230, 120, 170);
        var teal = Rgb(70, 190, 180);
        var pipe = Rgb(170, 170, 176);
        var c = V(0, 0.36f, 0);
        var r = gmax ? V(0.36f, 0.32f, 0.28f) : V(0.24f, 0.3f, 0.2f);
        if (gmax) c = V(0, 0.32f, 0);
        b.Ell(Body, c, r, brown);
        b.PaintEll(Body, c + V(0, r.Y * 0.5f, 0.02f), V(r.X * 1.1f, r.Y * 0.75f, r.Z * 1.1f), coat);
        for (int i = 0; i < 7; i++)
        {
            float a = (i - 3) * 0.5f;
            var at = Out(c, r, default, V(MathF.Sin(a), 0.15f, MathF.Cos(a)));
            b.PaintEll(Body, at + V(0, -0.02f, 0), V(0.03f, 0.1f + (i % 3) * 0.03f, 0.03f), coat);
        }
        foreach (var (d, col) in new[] { (V(0.6f, -0.4f, 0.8f), pink), (V(-0.7f, -0.2f, 0.7f), teal), (V(0.2f, -0.6f, 0.9f), teal), (V(-0.3f, -0.55f, 0.9f), pink), (V(0.9f, 0.1f, 0.3f), pink) })
        {
            var at = Out(c, r, default, d);
            b.Mark(Body, at, Outward(c, r, at), 0.025f, 0.02f, col);
        }
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.08f, 0.02f));
            b.Ell(leg, V(0.1f * s, 0.05f, 0.04f), V(0.07f, 0.05f, 0.08f), brown);
        });
        // Long arms of rubbish ending in pipes
        PokeBuilder.Both(s =>
        {
            var shoulder = c + V((r.X - 0.04f) * s, 0.08f, 0);
            int arm = b.Arm(s, shoulder);
            var path = Smooth(3, shoulder, shoulder + V(0.14f * s, 0.0f, 0.04f), shoulder + V(0.2f * s, -0.12f, 0.08f), shoulder + V(0.22f * s, -0.24f, 0.1f));
            b.Tube(arm, path, gmax ? 0.06f : 0.045f, gmax ? 0.05f : 0.035f, brown, blend: 0.01f);
            b.Limb(arm, path[^1], path[^1] + V(0.02f * s, -0.07f, 0.03f), 0.022f, 0.02f, pipe, Metal, 0.006f);
            b.Torus(arm, path[^1] + V(0.02f * s, -0.07f, 0.03f), 0.02f, 0.007f, pipe, mat: Metal, blend: 0.004f);
            b.Mark(arm, path[2] + V(0.02f * s, 0.035f, 0.02f), V(0.2f * s, 1f, 0.4f), 0.016f, 0.013f, pink);
        });
        // Puffs of rubbish on its head
        int head = b.Head(c + V(0, r.Y - 0.06f, 0));
        PokeBuilder.Both(s =>
        {
            var pc = c + V(0.11f * s, r.Y + 0.02f, -0.02f);
            Lumps(b, head, pc, V(0.05f, 0.045f, 0.045f), 7, 0.035f, brown, Rgb(170, 150, 126), 10f);
            b.Ell(head, pc, V(0.04f, 0.04f, 0.04f), brown);
            b.Mark(head, pc + V(0, 0.01f, 0.05f), V(0, 0.2f, 1f), 0.016f, 0.013f, pink);
        });
        b.Limb(head, c + V(-0.03f, r.Y - 0.02f, 0), c + V(-0.05f, r.Y + 0.07f, 0.02f), 0.016f, 0.016f, pipe, Metal, 0.006f);
        // A gaping mouth of teeth, round eyes over it
        var m = On(c, r, 0, c.Y + 0.06f);
        Grin(b, Body, m, V(0.07f, 0.045f, 0.05f), gmax ? Rgb(120, 30, 60) : Rgb(60, 30, 40));
        for (int i = -2; i <= 2; i++)
        {
            var top = m + V(i * 0.022f, 0.035f, -0.025f);
            b.Spike(Body, top, top + V(0, -0.025f, 0.005f), 0.009f, gmax ? pink : White, mat: Shell, blend: 0.003f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.07f * s, c.Y + 0.15f);
            if (gmax) b.Eye(Body, at, Outward(c, r, at), 0.022f, pink, glare: true);
            else b.Eye(Body, at, Outward(c, r, at), 0.02f, sclera: true, pupil: Rgb(30, 30, 30));
        });
        if (gmax)
        {
            // Junk stuck in the mound, and the red clouds over it
            foreach (var (d, col, size) in new[] { (V(0.8f, 0.3f, 0.5f), Rgb(80, 120, 200), 0.05f), (V(-0.8f, 0.1f, 0.5f), Rgb(230, 200, 80), 0.045f), (V(0.3f, -0.2f, 1f), Rgb(240, 240, 236), 0.055f), (V(-0.5f, -0.4f, 0.8f), Rgb(200, 60, 60), 0.04f) })
            {
                var at = Out(c, r, default, d);
                b.Box(Body, at, V(size, size * 0.8f, size * 0.7f), 0.008f, col, Euler(d), Shell);
            }
            MaxClouds(b, head, c + V(0, r.Y + 0.02f, -0.04f), 0.06f, 0.18f, 0.28f, 1.1f, 0.3f);
        }
        return b;
    }

    private static PokeBuilder Garbodor() => GarbodorBuild(false);

    // ------------------------------------------------------------------ Zorua line

    /// <summary>
    /// Zorua and its Hisuian form: a little dark gray fox kit, a red tuft swept back over its head, red marks over teal
    /// eyes, a black ruff round its neck and red tips to its paws; the Hisuian white tinged with red, its tuft a wisp of
    /// red flame, its eyes yellow.
    /// </summary>
    private static PokeBuilder ZoruaBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Zorua-Hisui" : "Zorua", 0.6f, BodyPlan.Quadruped, V(0, 0.13f, -0.01f)) { Coat = Fur };
        var gray = hisui ? Rgb(232, 230, 234) : Rgb(80, 76, 90);
        var ruff = hisui ? Rgb(220, 214, 222) : Rgb(40, 38, 46);
        var red = hisui ? Rgb(230, 110, 110) : Rgb(170, 40, 50);
        var eye = hisui ? Rgb(240, 180, 40) : Rgb(70, 200, 200);
        foreach (var (z, front) in new[] { (0.05f, true), (-0.07f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.035f * s, 0.1f, z), front);
                b.Limb(leg, V(0.035f * s, 0.11f, z), V(0.04f * s, 0.02f, z + 0.01f), 0.02f, 0.016f, gray);
                b.Ell(leg, V(0.04f * s, 0.016f, z + 0.018f), V(0.018f, 0.016f, 0.024f), red);
            });
        b.Ell(Body, V(0, 0.13f, -0.01f), V(0.055f, 0.055f, 0.09f), gray);
        b.Limb(Body, V(0, 0.15f, 0.03f), V(0, 0.22f, 0.07f), 0.04f, 0.04f, gray);
        FurTufts(b, Body, V(0, 0.17f, 0.05f), V(0.055f, 0.05f, 0.04f), 12, 0.035f, 0.014f, ruff, 1.1f, -0.5f, 0.3f);
        int tail = b.Tail(V(0, 0.15f, -0.09f));
        var tp = Smooth(3, V(0, 0.15f, -0.09f), V(0, 0.2f, -0.15f), V(0.02f, 0.27f, -0.16f));
        b.Tube(tail, tp, 0.03f, 0.022f, gray, blend: 0f);
        b.Ell(tail, tp[^1], V(0.03f, 0.035f, 0.03f), hisui ? red : ruff, blend: 0.012f);
        // A big head with tall ears, the tuft swept back over it
        int head = b.Head(V(0, 0.2f, 0.06f));
        var c = V(0, 0.26f, 0.08f);
        var r = V(0.07f, 0.065f, 0.065f);
        b.Ell(head, c, r, gray);
        b.Ell(head, c + V(0, -0.025f, 0.06f), V(0.03f, 0.025f, 0.035f), gray);
        b.Ell(head, c + V(0, -0.015f, 0.093f), V(0.009f, 0.007f, 0.006f), ruff, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.05f, -0.01f));
            Blade(b, ear, c + V(0.035f * s, 0.04f, -0.01f), c + V(0.1f * s, 0.14f, -0.04f), 0.045f, gray, V(0.2f * s, 0, 1f), 0.3f);
            b.PaintEll(ear, c + V(0.07f * s, 0.1f, -0.02f), V(0.02f, 0.035f, 0.02f), red, Euler(V(0.065f * s, 0.1f, -0.03f)));
            var at = On(c, r, 0.03f * s, c.Y + 0.005f);
            b.Mark(head, On(c, r, 0.022f * s, c.Y + 0.04f), V(0.2f * s, 0.6f, 1f), 0.009f, 0.009f, red);
            b.Eye(head, at, Outward(c, r, at), 0.017f, eye);
        });
        if (hisui)
        {
            var wisp = Smooth(3, c + V(0, 0.055f, 0), c + V(0, 0.1f, -0.06f), c + V(0.02f, 0.1f, -0.14f), c + V(0.05f, 0.14f, -0.2f));
            b.Tube(head, wisp, 0.03f, 0.008f, gray, blend: 0f);
            b.PaintEll(head, wisp[^2], V(0.04f, 0.04f, 0.04f), red);
        }
        else
            b.Spike(head, c + V(0, 0.055f, -0.01f), c + V(0, 0.1f, -0.13f), 0.032f, red, 0.5f);
        return b;
    }

    private static PokeBuilder Zorua() => ZoruaBuild(false);

    /// <summary>
    /// Zoroark and its Hisuian form: a tall dark gray fox standing up, a great red mane falling down its back to a teal
    /// bead, red claws, red marks round blue eyes; the Hisuian pale gray and white, its mane white fading to red at the
    /// tips and drifting in wisps, its claws dark and its eyes yellow.
    /// </summary>
    private static PokeBuilder ZoroarkBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Zoroark-Hisui" : "Zoroark", 1f, BodyPlan.Biped, V(0, 0.44f, 0)) { Coat = Fur };
        var gray = hisui ? Rgb(220, 218, 224) : Rgb(70, 66, 80);
        var dark = hisui ? Rgb(160, 150, 160) : Rgb(40, 38, 46);
        var mane = hisui ? Rgb(244, 240, 244) : Rgb(170, 40, 56);
        var tip = hisui ? Rgb(220, 70, 80) : Rgb(70, 20, 30);
        var claw = hisui ? Rgb(110, 90, 110) : Rgb(220, 60, 60);
        var eye = hisui ? Rgb(240, 190, 50) : Rgb(70, 170, 230);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.36f, -0.02f));
            var knee = V(0.08f * s, 0.22f, 0.06f);
            var ankle = V(0.08f * s, 0.08f, -0.03f);
            var foot = V(0.085f * s, 0.02f, 0.04f);
            b.Limb(leg, V(0.06f * s, 0.37f, -0.02f), knee, 0.04f, 0.03f, gray);
            b.Limb(leg, knee, ankle, 0.03f, 0.022f, gray);
            b.Limb(leg, ankle, foot, 0.022f, 0.02f, dark);
            Claws(b, leg, foot + V(0, -0.004f, 0.02f), V(0.012f, 0, 0), V(0, -0.3f, 1f), 0.02f, 0.006f);
        });
        b.Ell(Body, V(0, 0.48f, 0), V(0.08f, 0.13f, 0.07f), gray);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.08f * s, 0.56f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.15f * s, 0.45f, 0.08f);
            var hand = V(0.18f * s, 0.36f, 0.14f);
            b.Limb(arm, shoulder, elbow, 0.028f, 0.022f, gray);
            b.Limb(arm, elbow, hand, 0.022f, 0.018f, gray);
            b.Ell(arm, hand, V(0.022f, 0.02f, 0.022f), dark);
            for (int k = -1; k <= 1; k++)
                b.Spike(arm, hand + V(k * 0.012f, -0.01f, 0.012f), hand + V(k * 0.02f + 0.01f * s, -0.04f, 0.04f), 0.006f, claw, mat: Shell, blend: 0.003f);
        });
        // The great mane: a mass round the head falling down the back to a bead
        int tail = b.Tail(V(0, 0.64f, -0.06f));
        b.Ell(tail, V(0, 0.69f, -0.05f), V(0.12f, 0.1f, 0.11f), mane);
        var mp = Smooth(3, V(0, 0.68f, -0.07f), V(0, 0.6f, -0.16f), V(0, 0.44f, -0.19f), V(0, 0.3f, -0.16f));
        b.Tube(tail, mp, 0.1f, 0.045f, mane, blend: 0.02f);
        FurTufts(b, tail, V(0, 0.6f, -0.14f), V(0.1f, 0.14f, 0.08f), 18, 0.07f, 0.024f, mane, 0.2f, -1f, 0.4f);
        b.PaintEll(tail, mp[^1] + V(0, 0.03f, 0), V(0.07f, 0.09f, 0.07f), tip);
        if (hisui)
        {
            b.PaintEll(tail, V(0, 0.78f, -0.06f), V(0.12f, 0.05f, 0.1f), tip);
            PokeBuilder.Both(s => b.Tube(tail, Smooth(3, V(0.07f * s, 0.5f, -0.2f), V(0.12f * s, 0.52f, -0.28f), V(0.14f * s, 0.6f, -0.34f), V(0.12f * s, 0.7f, -0.36f)), 0.022f, 0.006f, tip, blend: 0.01f));
        }
        b.Ell(tail, mp[^1] + V(0, -0.04f, 0.01f), V(0.03f, 0.03f, 0.03f), Rgb(60, 180, 180), mat: Shell, blend: 0.008f);
        // A slender fox head: long snout, tall ears, marks round its eyes
        int head = b.Head(V(0, 0.62f, 0.04f));
        var c = V(0, 0.68f, 0.06f);
        var r = V(0.055f, 0.05f, 0.06f);
        b.Ell(head, c, r, gray);
        b.Ell(head, c + V(0, -0.02f, 0.065f), V(0.028f, 0.024f, 0.05f), gray);
        b.Ell(head, c + V(0, -0.012f, 0.112f), V(0.01f, 0.008f, 0.007f), dark, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.035f * s, 0.04f, -0.01f));
            Blade(b, ear, c + V(0.03f * s, 0.035f, -0.01f), c + V(0.08f * s, 0.12f, -0.05f), 0.035f, gray, V(0.2f * s, 0, 1f), 0.3f);
            var at = On(c, r, 0.026f * s, c.Y + 0.008f);
            b.PaintEll(head, at + V(0.01f * s, 0.004f, 0), V(0.03f, 0.014f, 0.03f), hisui ? Rgb(230, 110, 110) : Rgb(180, 40, 56));
            b.Eye(head, at, Outward(c, r, at), 0.013f, eye, glare: true);
        });
        return b;
    }

    private static PokeBuilder Zoroark() => ZoroarkBuild(false);
}
