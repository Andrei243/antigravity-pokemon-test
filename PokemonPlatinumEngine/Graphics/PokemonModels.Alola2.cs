using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Alola's second batch in National Pokédex
// order: Bounsweet (761) to Kommo-o (784). Their forms are in PokemonModels.Regional.cs and PokemonModels.Megas.cs with
// the other forms. Helpers shared with the earlier batches are in the files of those batches, PokemonModels.Sinnoh1.cs
// to PokemonModels.Alola1.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Bounsweet line

    /// <summary>Bounsweet: a round berry, deep pink above and white below, a sprig of green leaves on top, little dark eyes, a happy open mouth and two pink points for feet.</summary>
    private static PokeBuilder Bounsweet()
    {
        var b = new PokeBuilder("Bounsweet", 0.42f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Scales };
        var pink = Rgb(212, 58, 112);
        var white = Rgb(248, 244, 240);
        var green = Rgb(120, 192, 84);
        var c = V(0, 0.112f, 0);
        var r = V(0.11f, 0.1f, 0.1f);
        b.Ell(Body, c, r, pink);
        b.PaintEll(Body, c + V(0, -0.105f, 0), V(0.125f, 0.06f, 0.115f), white);
        PokeBuilder.Both(s => b.Spike(Body, c + V(0.045f * s, -0.075f, 0.02f), V(0.052f * s, 0.0f, 0.032f), 0.016f, pink, mat: Shell));
        // The sprig: two broad leaves falling to the sides, one behind, a sprout standing up between them
        var top = c + V(0, r.Y - 0.01f, 0);
        b.Limb(Body, top, top + V(0, 0.035f, -0.005f), 0.008f, 0.006f, green, Leaf);
        PokeBuilder.Both(s => Frond(b, Body, top + V(0.01f * s, 0.012f, 0), top + V(0.15f * s, -0.05f, 0.01f), 0.055f, green, V(0.4f * s, 1f, 0), 0.2f, Leaf));
        Frond(b, Body, top + V(0, 0.012f, -0.01f), top + V(0, -0.04f, -0.13f), 0.05f, green, V(0, 1f, -0.4f), 0.2f, Leaf);
        Frond(b, Body, top + V(0, 0.03f, 0), top + V(0.015f, 0.075f, 0.01f), 0.015f, green, V(0, 0, 1f), 0.25f, Leaf);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.02f);
            b.Eye(Body, at, Outward(c, r, at), 0.011f, Rgb(70, 30, 40));
        });
        var mouth = On(c, r, 0, c.Y - 0.022f);
        Grin(b, Body, mouth, V(0.022f, 0.016f, 0.014f), Rgb(140, 30, 60));
        b.PaintEll(Body, mouth + V(0, -0.01f, 0), V(0.012f, 0.007f, 0.012f), Rgb(240, 120, 150));
        return b;
    }

    /// <summary>Steenee: a dancing fruit, its white skirt round as two halves of a berry, a slender pink body and legs, thin white arms, a white face with big pink eyes, and two great green leaves for hair, spotted yellow, a sprout on top.</summary>
    private static PokeBuilder Steenee()
    {
        var b = new PokeBuilder("Steenee", 0.6f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Scales };
        var white = Rgb(248, 244, 242);
        var pink = Rgb(196, 42, 96);
        var green = Rgb(140, 202, 96);
        var spot = Rgb(214, 232, 120);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.13f, 0));
            b.Limb(leg, V(0.025f * s, 0.14f, 0), V(0.03f * s, 0.004f, 0.01f), 0.013f, 0.004f, pink);
        });
        var bc = V(0, 0.17f, 0);
        PokeBuilder.Both(s => b.Ell(Body, bc + V(0.032f * s, 0, 0.005f), V(0.048f, 0.048f, 0.05f), white));
        b.Ell(Body, bc + V(0, 0.07f, 0), V(0.026f, 0.05f, 0.024f), pink);
        // A little pink calyx where the skirt meets the body
        PokeBuilder.Both(s => b.Spike(Body, bc + V(0.01f * s, 0.04f, 0.01f), bc + V(0.04f * s, 0.06f, 0.025f), 0.012f, pink, 0.5f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.022f * s, 0.1f, 0));
            b.Limb(arm, bc + V(0.022f * s, 0.1f, 0), bc + V(0.075f * s, 0.07f, 0.02f), 0.008f, 0.007f, white);
        });
        int head = b.Head(bc + V(0, 0.115f, 0));
        var c = bc + V(0, 0.165f, 0.01f);
        var r = V(0.05f, 0.046f, 0.045f);
        b.Ell(head, c, r, white);
        // Two great leaves falling to the sides like pigtails, spotted yellow
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.02f * s, 0.04f, -0.005f);
            var tip = c + V(0.14f * s, -0.04f, -0.01f);
            Frond(b, head, root, tip, 0.05f, green, V(0.2f * s, 1f, 0.3f), 0.2f, Leaf);
            foreach (var t in new[] { 0.45f, 0.65f, 0.82f })
                b.PaintEll(head, Vector3.Lerp(root, tip, t) + V(0, 0.012f, 0.004f), V(0.009f, 0.007f, 0.009f), spot);
        });
        b.Limb(head, c + V(0, r.Y - 0.005f, 0), c + V(0, r.Y + 0.035f, -0.01f), 0.005f, 0.004f, green, Leaf);
        Frond(b, head, c + V(0, r.Y + 0.03f, -0.01f), c + V(0.025f, r.Y + 0.05f, -0.01f), 0.01f, green, V(0, 1f, 0.2f), 0.3f, Leaf);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.019f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(214, 60, 120));
        });
        Grin(b, head, On(c, r, 0, c.Y - 0.026f), V(0.012f, 0.008f, 0.008f), Rgb(150, 40, 70));
        return b;
    }

    /// <summary>Tsareena: a queen of fruit, tall and slender, a white face behind a high pink collar, haughty purple eyes under pink lids, a green crown of leaves falling in two long trains spotted yellow, a pink calyx for a crown, a white bodice round as fruit and long pink legs ending in points, sheathed in leaves.</summary>
    private static PokeBuilder Tsareena()
    {
        var b = new PokeBuilder("Tsareena", 0.85f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Scales };
        var white = Rgb(248, 244, 242);
        var pink = Rgb(186, 34, 86);
        var deep = Rgb(140, 24, 66);
        var green = Rgb(120, 186, 84);
        var spot = Rgb(222, 226, 110);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.36f, 0));
            var knee = V(0.04f * s, 0.18f, 0.012f);
            b.Limb(leg, V(0.035f * s, 0.37f, 0), knee, 0.022f, 0.016f, pink);
            b.Limb(leg, knee, V(0.045f * s, 0.004f, 0.02f), 0.016f, 0.004f, pink);
            // A leaf sheathing the shin like a greave
            Frond(b, leg, knee + V(0, 0.07f, 0.012f), knee + V(0.004f * s, -0.1f, 0.022f), 0.026f, deep, V(0.1f * s, 0, 1f), 0.3f);
        });
        var bc = V(0, 0.42f, 0);
        PokeBuilder.Both(s => b.Ell(Body, bc + V(0.032f * s, 0, 0.008f), V(0.046f, 0.05f, 0.048f), white));
        b.Ell(Body, bc + V(0, -0.035f, 0), V(0.04f, 0.03f, 0.035f), pink);
        b.Ell(Body, bc + V(0, 0.08f, 0), V(0.028f, 0.065f, 0.026f), pink);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.025f * s, 0.12f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = bc + V(0.075f * s, 0.06f, 0);
            b.Limb(arm, shoulder, elbow, 0.01f, 0.008f, white);
            b.Limb(arm, elbow, bc + V(0.1f * s, 0, 0.03f), 0.008f, 0.007f, white);
        });
        int head = b.Head(bc + V(0, 0.15f, 0));
        var c = bc + V(0, 0.205f, 0.008f);
        var r = V(0.042f, 0.046f, 0.042f);
        b.Ell(head, c, r, white);
        // The crown of leaves: green over the head, falling in two long trains to below the knees
        b.PaintEll(head, c + V(0, 0.04f, -0.012f), V(0.06f, 0.035f, 0.055f), green);
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.03f * s, 0.025f, -0.008f);
            var tip = c + V(0.075f * s, -0.33f, -0.03f);
            Frond(b, head, root, tip, 0.048f, green, V(s, 0, 0.35f), 0.2f, Leaf);
            foreach (var t in new[] { 0.35f, 0.55f, 0.75f, 0.9f })
                b.PaintEll(head, Vector3.Lerp(root, tip, t) + V(0.01f * s, 0, 0.004f), V(0.008f, 0.011f, 0.008f), spot);
        });
        // The high pink collar hiding the mouth, its points rising past the cheeks
        b.Ell(head, c + V(0, -0.04f, 0.012f), V(0.034f, 0.026f, 0.032f), pink, blend: 0.012f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.018f * s, -0.035f, 0.02f), c + V(0.05f * s, -0.005f, 0.035f), 0.014f, pink, 0.4f));
        b.Spike(head, c + V(0, -0.03f, 0.035f), c + V(0, -0.008f, 0.06f), 0.012f, pink, 0.4f);
        b.Ell(head, c + V(0, r.Y + 0.008f, -0.005f), V(0.016f, 0.012f, 0.016f), pink, blend: 0.008f);
        b.Torus(head, c + V(0, r.Y + 0.02f, -0.005f), 0.01f, 0.005f, pink, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.017f * s, c.Y + 0.006f);
            b.PaintEll(head, at + V(0, 0.008f, 0), V(0.016f, 0.008f, 0.012f), pink);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(120, 50, 150), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Comfey

    /// <summary>Comfey: a little green fairy at the top of a lei, a ring of vine strung with the flowers it picked (red, yellow, pink and white), its face a golden flower's heart framed by pale green petals.</summary>
    private static PokeBuilder Comfey()
    {
        var b = new PokeBuilder("Comfey", 0.45f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Leaf }.Hover();
        var vine = Rgb(110, 176, 90);
        var c = V(0, 0.2f, 0);
        const float ring = 0.13f;
        b.Torus(Body, c, ring, 0.009f, vine, V(90f, 0, 0), blend: 0.006f);
        var colours = new[] { Rgb(232, 64, 70), Rgb(250, 212, 70), Rgb(240, 118, 172), Rgb(248, 244, 240), Rgb(232, 64, 70), Rgb(250, 212, 70), Rgb(240, 118, 172) };
        var angles = new[] { 170f, 210f, 250f, 290f, 330f, 10f, 50f };
        for (int i = 0; i < angles.Length; i++)
        {
            float a = angles[i] * Degree;
            var at = c + V(MathF.Cos(a), MathF.Sin(a), 0) * ring + V(0, 0, 0.008f);
            FlowerHead(b, Body, at, V(0, 0, 1f), 5, 0.038f, 0.01f, colours[i], Rgb(250, 220, 110), 0.009f, i * 0.37f);
        }
        float ha = 118f * Degree;
        var neck = c + V(MathF.Cos(ha), MathF.Sin(ha), 0) * ring;
        int head = b.Head(neck);
        var hc = neck + V(-0.01f, 0.03f, 0.02f);
        var hr = V(0.032f, 0.03f, 0.028f);
        b.Ell(head, neck + V(-0.004f, 0.012f, 0.008f), V(0.016f, 0.02f, 0.016f), vine);
        // Pale green petals framing the face like hair
        for (int i = 0; i < 7; i++)
        {
            float a = (i * 360f / 7f + 90f) * Degree;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            Frond(b, head, hc + d * 0.015f + V(0, 0, -0.01f), hc + d * 0.058f + V(0, 0, -0.016f), 0.022f, Rgb(196, 230, 150), V(0, 0, 1f), 0.25f, Leaf);
        }
        b.Ell(head, hc, hr, Rgb(250, 196, 80));
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, hc.X + 0.012f * s, hc.Y + 0.004f);
            b.Eye(head, at, Outward(hc, hr, at), 0.007f, Rgb(50, 40, 40));
        });
        var smile = On(hc, hr, hc.X, hc.Y - 0.012f);
        b.Mark(head, smile, Outward(hc, hr, smile), 0.006f, 0.004f, Rgb(120, 60, 40), MarkShape.Smile);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Oranguru and Passimian

    /// <summary>Oranguru: a wise old orangutan, white-furred, its grey face framed by a white hood of hair, a gold star on its brow, a cape of purple hair streaked orange over its back and arms, great white hands, and a fan of leaves raised behind its shoulder.</summary>
    private static PokeBuilder Oranguru()
    {
        var b = new PokeBuilder("Oranguru", 0.9f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var white = Rgb(238, 238, 234);
        var grey = Rgb(132, 134, 144);
        var purple = Rgb(104, 62, 146);
        var orange = Rgb(236, 150, 62);
        var leaf = Rgb(82, 160, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.2f, 0));
            b.Limb(leg, V(0.07f * s, 0.21f, 0), V(0.09f * s, 0.05f, 0.02f), 0.045f, 0.034f, white);
            b.Ell(leg, V(0.09f * s, 0.03f, 0.04f), V(0.042f, 0.03f, 0.055f), grey);
        });
        var bc = V(0, 0.33f, 0);
        b.Ell(Body, bc, V(0.14f, 0.15f, 0.12f), white);
        // The cape: purple hair over the back and down the sides, streaked orange
        b.Ell(Body, bc + V(0, 0.05f, -0.06f), V(0.16f, 0.15f, 0.09f), purple, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            var root = bc + V(0.12f * s, 0.08f, -0.03f);
            var tip = bc + V(0.2f * s, -0.2f, -0.05f);
            Frond(b, Body, root, tip, 0.08f, purple, V(s, 0, 0.25f), 0.3f);
            foreach (var t in new[] { 0.4f, 0.7f })
                b.PaintEll(Body, Vector3.Lerp(root, tip, t) + V(0.024f * s, 0, 0.03f), V(0.008f, 0.04f, 0.008f), orange);
        });
        // The fan of leaves, its handle held behind the shoulder
        var grip = bc + V(0.2f, 0.2f, -0.1f);
        b.Limb(Body, bc + V(0.16f, 0.0f, -0.08f), grip, 0.01f, 0.01f, Rgb(150, 110, 70));
        for (int i = 0; i < 7; i++)
        {
            float a = (40f + i * 16f) * Degree;
            Frond(b, Body, grip, grip + V(MathF.Cos(a), MathF.Sin(a), 0) * 0.16f, 0.035f, leaf, V(0, 0, 1f), 0.2f, Leaf);
        }
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.13f * s, 0.08f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.07f * s, -0.13f, 0.06f);
            var hand = shoulder + V(0.06f * s, -0.27f, 0.11f);
            b.Limb(arm, shoulder, elbow, 0.05f, 0.042f, purple);
            b.Limb(arm, elbow, hand, 0.04f, 0.036f, white);
            b.Ell(arm, hand + V(0, -0.02f, 0.01f), V(0.055f, 0.045f, 0.06f), white);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Limb(arm, hand + V(0.022f * t, -0.03f, 0.04f), hand + V(0.028f * t, -0.06f, 0.06f), 0.014f, 0.012f, white);
        });
        int head = b.Head(bc + V(0, 0.13f, 0.04f));
        var c = bc + V(0, 0.2f, 0.06f);
        var r = V(0.08f, 0.075f, 0.07f);
        b.Ell(head, c, r, white);
        FurTufts(b, head, c, r, 18, 0.035f, 0.016f, white, 0.5f, -0.5f, 0.4f);
        var fc = c + V(0, -0.012f, 0.045f);
        var fr = V(0.052f, 0.058f, 0.035f);
        b.Ell(head, fc, fr, grey, blend: 0.012f);
        b.Ell(head, fc + V(0, -0.03f, 0.02f), V(0.034f, 0.024f, 0.022f), grey, blend: 0.01f);
        var star = On(c, r, 0, c.Y + 0.05f);
        b.Mark(head, star, Outward(c, r, star), 0.012f, 0.012f, Rgb(250, 196, 60), MarkShape.Star);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.02f * s, fc.Y + 0.012f);
            b.Eye(head, at, Outward(fc, fr, at), 0.009f, Rgb(232, 186, 70));
        });
        return b;
    }

    /// <summary>Passimian: a monkey of a team, white-furred with dark grey limbs and face, yellow eyes, a white crest of hair with a green sprout in it, green marks like tape on its shoulders and a long dark tail ringed white; it holds a hard green berry like a ball.</summary>
    private static PokeBuilder Passimian()
    {
        var b = new PokeBuilder("Passimian", 0.85f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var white = Rgb(240, 240, 236);
        var dark = Rgb(66, 68, 78);
        var lime = Rgb(168, 212, 64);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.055f * s, 0.27f, -0.02f));
            var knee = V(0.08f * s, 0.15f, 0.03f);
            b.Limb(leg, V(0.055f * s, 0.28f, -0.02f), knee, 0.04f, 0.03f, white);
            b.Limb(leg, knee, V(0.075f * s, 0.04f, -0.01f), 0.028f, 0.022f, dark);
            b.Ell(leg, V(0.075f * s, 0.022f, 0.02f), V(0.028f, 0.022f, 0.048f), dark);
        });
        var bc = V(0, 0.38f, 0.02f);
        b.Ell(Body, bc, V(0.085f, 0.12f, 0.075f), white, V(15f, 0, 0));
        PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0.06f * s, 0.07f, 0.04f), V(0.03f, 0.01f, 0.04f), lime, V(0, 0, 30f * s)));
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.08f * s, 0.08f, 0.01f);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.06f * s, -0.15f, 0.05f);
            var hand = shoulder + V(0.07f * s, -0.31f, 0.1f);
            b.Limb(arm, shoulder, elbow, 0.032f, 0.026f, white);
            b.Limb(arm, elbow, hand, 0.024f, 0.02f, dark);
            b.Ell(arm, hand, V(0.026f, 0.024f, 0.03f), dark);
            if (s > 0) b.Ell(arm, hand + V(0.0f, -0.045f, 0.02f), V(0.04f, 0.04f, 0.04f), lime, mat: Shell);
        });
        int tail = b.Tail(bc + V(0, -0.08f, -0.06f));
        var path = Smooth(4, bc + V(0, -0.08f, -0.06f), bc + V(0, -0.06f, -0.2f), bc + V(0, 0.1f, -0.3f), bc + V(0, 0.26f, -0.26f), bc + V(0, 0.3f, -0.16f));
        b.Tube(tail, path, 0.022f, 0.018f, dark, blend: 0f);
        for (int i = 4; i < path.Length - 2; i += 4)
            b.PaintEll(tail, path[i], V(0.026f, 0.026f, 0.026f), white);
        b.Ell(tail, path[^1], V(0.03f, 0.03f, 0.03f), white);
        int head = b.Head(bc + V(0, 0.11f, 0.04f));
        var c = bc + V(0, 0.17f, 0.06f);
        var r = V(0.052f, 0.052f, 0.05f);
        b.Ell(head, c, r, white);
        var fc = c + V(0, -0.01f, 0.03f);
        var fr = V(0.04f, 0.042f, 0.03f);
        b.Ell(head, fc, fr, dark, blend: 0.01f);
        b.Ell(head, fc + V(0, -0.025f, 0.018f), V(0.026f, 0.018f, 0.018f), dark, blend: 0.008f);
        // A crest of white hair swept up and back, a green sprout in it
        foreach (var (x, h) in new[] { (-0.03f, 0.07f), (-0.012f, 0.09f), (0.012f, 0.09f), (0.03f, 0.07f) })
            b.Spike(head, c + V(x, 0.03f, 0.01f), c + V(x * 1.6f, 0.03f + h, -0.03f), 0.018f, white, 0.5f);
        PokeBuilder.Both(s => Frond(b, head, c + V(0, 0.05f, 0), c + V(0.03f * s, 0.1f, 0.01f), 0.012f, lime, V(0, 0, 1f), 0.3f, Leaf));
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.016f * s, fc.Y + 0.01f);
            b.Eye(head, at, Outward(fc, fr, at), 0.009f, Rgb(244, 196, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Wimpod and Golisopod

    /// <summary>Wimpod: a little woodlouse in plates of silver armour, its purple face with two great yellow eyes, long purple feelers sweeping back over its shell, two purple tail points and many little legs.</summary>
    private static PokeBuilder Wimpod()
    {
        var b = new PokeBuilder("Wimpod", 0.45f, BodyPlan.Quadruped, V(0, 0.08f, 0)) { Coat = Shell };
        var silver = Rgb(204, 212, 214);
        var purple = Rgb(118, 74, 156);
        var bc = V(0, 0.075f, -0.02f);
        b.Ell(Body, bc, V(0.065f, 0.055f, 0.1f), silver);
        // Plates of armour overlapping down its back
        for (int i = 0; i < 5; i++)
        {
            float k = 1f - MathF.Abs(i - 1.8f) * 0.07f;
            b.Ell(Body, bc + V(0, 0.002f * (2 - MathF.Abs(i - 2)), -0.08f + i * 0.04f), V(0.075f * k, 0.066f * k, 0.03f), silver, blend: 0.004f);
        }
        int head = b.Head(bc + V(0, 0, 0.09f));
        var c = bc + V(0, -0.008f, 0.115f);
        var r = V(0.05f, 0.042f, 0.035f);
        b.Ell(head, c, r, purple);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.026f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, pupil: Rgb(40, 36, 50), white: Rgb(250, 222, 90));
            b.Tube(head, Smooth(3, c + V(0.022f * s, 0.03f, 0.0f), c + V(0.06f * s, 0.07f, -0.06f), c + V(0.09f * s, 0.06f, -0.2f)), 0.006f, 0.004f, purple, blend: 0f);
        });
        PokeBuilder.Both(s =>
        {
            b.Spike(Body, bc + V(0.025f * s, 0.0f, -0.1f), bc + V(0.06f * s, 0.015f, -0.17f), 0.012f, purple, mat: Shell);
            foreach (float z in new[] { -0.05f, -0.005f, 0.04f })
                b.Limb(Body, bc + V(0.05f * s, -0.035f, z), V(0.072f * s, 0.004f, bc.Z + z + 0.012f), 0.008f, 0.005f, purple);
        });
        return b;
    }

    /// <summary>
    /// Golisopod and its Mega Evolution: a hulking knight of a woodlouse risen on two legs, its back and shoulders armoured
    /// in great plates, a purple face behind a dark visor, two great arms with long black claws and two small ones at its
    /// chest. Golisopod's plates are silver marked with dark green; Mega Golisopod's are gunmetal and heavier, with a crest
    /// of purple fins down its head and back and greater claws.
    /// </summary>
    private static PokeBuilder GolisopodBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Golisopod-Mega" : "Golisopod", 1f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Shell };
        var plate = mega ? Rgb(112, 116, 126) : Rgb(212, 218, 214);
        var mark = mega ? Rgb(170, 90, 220) : Rgb(60, 112, 104);
        var body = mega ? Rgb(52, 48, 60) : Rgb(96, 72, 118);
        var claw = Rgb(40, 40, 50);
        var fin = Rgb(156, 86, 210);
        float k = mega ? 1.15f : 1f;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.24f, -0.02f));
            var knee = V(0.12f * s, 0.13f, 0.04f);
            var ankle = V(0.1f * s, 0.035f, 0.0f);
            b.Limb(leg, V(0.08f * s, 0.25f, -0.02f), knee, 0.05f, 0.04f, plate);
            b.Limb(leg, knee, ankle, 0.034f, 0.028f, body);
            b.Ell(leg, ankle + V(0, -0.01f, 0.02f), V(0.035f, 0.025f, 0.045f), body);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(leg, ankle + V(0.015f * t, -0.015f, 0.04f), ankle + V(0.025f * t, -0.03f, 0.09f), 0.012f, claw, mat: Shell);
        });
        var bc = V(0, 0.44f, 0);
        b.Ell(Body, bc, V(0.12f, 0.15f, 0.1f) * k, body);
        // The chest's plate and the great shell of the back, spined at its edges
        b.Ell(Body, bc + V(0, -0.06f, 0.05f), V(0.09f, 0.07f, 0.06f), plate, blend: 0.01f);
        b.Ell(Body, bc + V(0, 0.05f, -0.07f), V(0.17f, 0.16f, 0.1f) * k, plate, blend: 0.015f);
        for (int i = 0; i < 4; i++)
        {
            float y = 0.15f - i * 0.08f;
            PokeBuilder.Both(s => b.Spike(Body, bc + V(0.1f * s, y, -0.1f), bc + V(0.16f * s * k, y + 0.05f, -0.16f), 0.03f, plate, 0.4f));
        }
        PokeBuilder.Both(s =>
        {
            // The pauldrons, marked, and the great arms
            var sc = bc + V(0.14f * s * k, 0.1f, 0.0f);
            var sr = V(0.085f, 0.065f, 0.085f) * k;
            b.Ell(Body, sc, sr, plate, blend: 0.012f);
            var at = On(sc, sr, sc.X, sc.Y + 0.03f);
            b.Mark(Body, at, Outward(sc, sr, at), 0.02f, 0.018f, mark, MarkShape.Triangle);
            var shoulder = bc + V(0.17f * s * k, 0.08f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.08f * s, -0.12f, 0.05f);
            var wrist = elbow + V(-0.01f * s, -0.12f, 0.08f);
            b.Limb(arm, shoulder, elbow, 0.045f * k, 0.04f * k, body);
            b.Limb(arm, elbow, wrist, 0.042f * k, 0.036f * k, plate);
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, wrist + V(0.02f * i, 0, 0.01f), wrist + V(0.03f * i, -0.16f * k, 0.08f), 0.016f * k, claw, mat: Shell);
            // The small arms at its chest
            b.Limb(Body, bc + V(0.06f * s, -0.01f, 0.07f), bc + V(0.09f * s, -0.08f, 0.13f), 0.018f, 0.015f, body);
            b.Spike(Body, bc + V(0.09f * s, -0.08f, 0.13f), bc + V(0.1f * s, -0.13f, 0.16f), 0.012f, claw, mat: Shell);
        });
        int tail = b.Tail(bc + V(0, -0.12f, -0.08f));
        b.Limb(tail, bc + V(0, -0.12f, -0.08f), V(0, 0.08f, -0.2f), 0.05f, 0.03f, body);
        PokeBuilder.Both(s => b.Spike(tail, V(0.01f * s, 0.09f, -0.19f), V(0.07f * s, 0.03f, -0.28f), 0.022f, mega ? fin : body, 0.4f));
        int head = b.Head(bc + V(0, 0.13f, 0.07f));
        var c = bc + V(0, 0.17f, 0.11f);
        var r = V(0.055f, 0.045f, 0.055f);
        b.Ell(head, c, r, body);
        // The helmet over the crown and the dark visor across the face
        b.Ell(head, c + V(0, 0.03f, -0.02f), V(0.065f, 0.035f, 0.06f), plate, blend: 0.01f);
        b.PaintEll(head, c + V(0, 0.0f, 0.04f), V(0.05f, 0.016f, 0.03f), Rgb(36, 30, 44));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, mega ? Rgb(250, 220, 70) : Rgb(240, 240, 220), glare: true);
            b.Tube(head, Smooth(3, c + V(0.03f * s, -0.01f, 0.04f), c + V(0.08f * s, 0.02f, 0.02f), c + V(0.12f * s, 0.0f, -0.12f)), 0.006f, 0.004f, mega ? fin : Rgb(150, 110, 170), blend: 0f);
        });
        if (mega)
        {
            // A crest of purple fins over the head and down the back
            for (int i = 0; i < 4; i++)
                Blade(b, head, c + V(0, 0.05f, -0.01f - i * 0.02f), c + V(0, 0.11f - i * 0.015f, -0.04f - i * 0.03f), 0.02f, fin, V(1f, 0, 0), 0.25f);
            for (int i = 0; i < 5; i++)
                Blade(b, Body, bc + V(0, 0.18f - i * 0.07f, -0.14f), bc + V(0, 0.22f - i * 0.07f, -0.27f), 0.035f, fin, V(1f, 0, 0), 0.25f);
        }
        return b;
    }

    private static PokeBuilder Golisopod() => GolisopodBuild(false);

    // ------------------------------------------------------------------ Sandygast and Palossand

    /// <summary>Sandygast: a ghost in a mound of sand, its mouth a dark hollow, two dark eyes over it, and a child's red spade stuck in its top, the white handle standing up.</summary>
    private static PokeBuilder Sandygast()
    {
        var b = new PokeBuilder("Sandygast", 0.55f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Fur };
        var sand = Rgb(224, 208, 172);
        var hollow = Rgb(44, 36, 32);
        b.Ell(Body, V(0, 0.03f, 0), V(0.2f, 0.04f, 0.17f), sand);
        b.Ell(Body, V(0, 0.1f, 0), V(0.15f, 0.1f, 0.13f), sand);
        var c3 = V(0, 0.19f, -0.005f);
        var r3 = V(0.1f, 0.08f, 0.09f);
        b.Ell(Body, c3, r3, sand);
        b.Ell(Body, V(0, 0.255f, -0.01f), V(0.06f, 0.05f, 0.055f), sand);
        // Sand slumping at its foot
        for (int i = 0; i < 7; i++)
        {
            float a = (i * 52f + 20f) * Degree;
            b.Ell(Body, V(MathF.Sin(a) * 0.19f, 0.02f, MathF.Cos(a) * 0.15f), V(0.04f, 0.02f, 0.04f), sand, blend: 0.02f);
        }
        var mouth = V(0, 0.1f, 0.125f);
        b.Cut(Body, mouth + V(0, 0, 0.03f), V(0.04f, 0.055f, 0.05f));
        b.PaintEll(Body, mouth, V(0.046f, 0.062f, 0.06f), hollow);
        PokeBuilder.Both(s =>
        {
            var at = On(c3, r3, 0.035f * s, 0.195f);
            b.Eye(Body, at, Outward(c3, r3, at), 0.016f, Rgb(36, 30, 28));
        });
        // The spade: its red blade stuck in the top, the white handle standing up
        var top = V(0, 0.3f, -0.01f);
        b.Ell(Body, top + V(0.012f, 0.035f, 0), V(0.036f, 0.055f, 0.012f), Rgb(222, 50, 50), V(0, 0, -15f), Shell);
        b.Limb(Body, top + V(0.025f, 0.08f, 0), top + V(0.045f, 0.16f, 0), 0.007f, 0.007f, White, Shell);
        b.Ell(Body, top + V(0.046f, 0.165f, 0), V(0.014f, 0.009f, 0.009f), White, mat: Shell);
        return b;
    }

    /// <summary>Palossand: a castle of sand on a mound, a round keep in the middle crowned with a red spade like a flag, its windows two dark eyes and its gateway a mouth, two towers for arms, shells set in its walls.</summary>
    private static PokeBuilder Palossand()
    {
        var b = new PokeBuilder("Palossand", 1f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var sand = Rgb(226, 172, 110);
        var dark = Rgb(60, 40, 30);
        var shell = Rgb(190, 222, 240);
        b.Ell(Body, V(0, 0.04f, 0), V(0.32f, 0.06f, 0.24f), sand);
        b.Ell(Body, V(0, 0.1f, 0), V(0.22f, 0.08f, 0.17f), sand);
        // The keep, ringed with two ridges, a red spade on its dome
        b.Limb(Body, V(0, 0.12f, 0), V(0, 0.46f, 0), 0.13f, 0.11f, sand);
        foreach (float y in new[] { 0.25f, 0.4f })
            b.Torus(Body, V(0, y, 0), 0.13f - (y - 0.12f) * 0.06f, 0.008f, sand, blend: 0.004f);
        Frond(b, Body, V(0, 0.55f, 0), V(0, 0.67f, 0), 0.035f, Rgb(222, 50, 50), V(0, 0, 1f), 0.25f, Shell);
        var gate = V(0, 0.19f, 0.135f);
        b.Cut(Body, gate + V(0, 0, 0.03f), V(0.045f, 0.06f, 0.05f));
        b.PaintEll(Body, gate, V(0.05f, 0.066f, 0.06f), dark);
        PokeBuilder.Both(s =>
        {
            const float y = 0.43f, rad = 0.112f;
            var n = Vector3.Normalize(V(0.04f * s, 0, MathF.Sqrt(rad * rad - 0.04f * 0.04f)));
            b.Eye(Body, V(0, y, 0) + n * rad, n, 0.017f, Rgb(40, 30, 28));
            foreach (var (a, h) in new[] { (35f, 0.32f), (70f, 0.3f) })
            {
                var d = V(MathF.Sin(a * Degree * s), 0, MathF.Cos(a * Degree * s));
                b.Mark(Body, V(0, h, 0) + d * (0.13f - (h - 0.12f) * 0.06f), d, 0.009f, 0.009f, shell);
            }
        });
        // The two towers, the arms, crenellated, a window in each
        PokeBuilder.Both(s =>
        {
            var root = V(0.19f * s, 0.08f, -0.01f);
            int arm = b.Arm(s, root);
            var top = V((s > 0 ? 0.27f : 0.25f) * s, s > 0 ? 0.34f : 0.4f, 0);
            b.Limb(arm, root, top, 0.075f, 0.06f, sand);
            for (int i = 0; i < 4; i++)
            {
                float a = (i * 90f + 45f) * Degree;
                b.Box(arm, top + V(MathF.Sin(a) * 0.045f, 0.055f, MathF.Cos(a) * 0.045f), V(0.018f, 0.025f, 0.018f), 0.005f, sand);
            }
            b.PaintEll(arm, top + V(0, -0.03f, 0.06f), V(0.012f, 0.02f, 0.012f), dark);
        });
        return b;
    }

    // ------------------------------------------------------------------ Pyukumuku

    /// <summary>Pyukumuku: a black sea cucumber lying on the shore, two rows of pink spines down its back, a white star of a mouth and little pink eyes at its front, and the white hand of its insides poking out behind.</summary>
    private static PokeBuilder Pyukumuku()
    {
        var b = new PokeBuilder("Pyukumuku", 0.5f, BodyPlan.Biped, V(0, 0.075f, 0)) { Coat = Scales };
        var black = Rgb(48, 44, 52);
        var pink = Rgb(236, 110, 160);
        var bc = V(0, 0.075f, 0);
        var br = V(0.085f, 0.07f, 0.16f);
        b.Ell(Body, bc, br, black);
        var fc = bc + V(0, -0.005f, 0.11f);
        var fr = V(0.07f, 0.06f, 0.07f);
        b.Ell(Body, fc, fr, black);
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { -0.11f, -0.06f, -0.01f, 0.04f, 0.09f })
            {
                float f = MathF.Sqrt(1f - z * z / (br.Z * br.Z));
                var at = bc + V(br.X * f * 0.5f * s, br.Y * f * 0.866f, z);
                var n = Outward(bc, br, at);
                b.Spike(Body, at - n * 0.01f, at + n * 0.04f + V(0, 0.01f, 0), 0.016f, pink, mat: Shell);
            }
        });
        // The white hand of its insides, poking out behind
        var rear = bc + V(0, 0, -br.Z + 0.01f);
        b.Ell(Body, rear + V(0, 0.01f, -0.025f), V(0.04f, 0.035f, 0.035f), White);
        foreach (float x in new[] { -0.025f, 0f, 0.025f })
            b.Limb(Body, rear + V(x * 0.8f, 0.02f, -0.04f), rear + V(x * 1.4f, 0.04f, -0.075f), 0.013f, 0.012f, White);
        var mouth = On(fc, fr, 0, fc.Y - 0.012f);
        b.Mark(Body, mouth, Outward(fc, fr, mouth), 0.016f, 0.016f, White, MarkShape.Star);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.03f * s, fc.Y + 0.02f);
            b.Eye(Body, at, Outward(fc, fr, at), 0.009f, pink);
        });
        return b;
    }

    // ------------------------------------------------------------------ Type: Null and Silvally

    /// <summary>Type: Null: a beast in a heavy mask, its bronze helmet set with green bolts and crowned with a great grey blade like an axe's, its body dark under ragged grey armour, a bird's taloned forelegs, a beast's hind legs and a fish's tail of grey-blue fins.</summary>
    private static PokeBuilder TypeNull()
    {
        var b = new PokeBuilder("Type: Null", 0.85f, BodyPlan.Quadruped, V(0, 0.32f, 0)) { Coat = Fur };
        var dark = Rgb(56, 56, 64);
        var armour = Rgb(140, 138, 130);
        var helm = Rgb(150, 104, 70);
        var blade = Rgb(196, 200, 206);
        var bolt = Rgb(90, 160, 110);
        var talon = Rgb(80, 100, 90);
        var fin = Rgb(110, 130, 160);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.055f * s, 0.3f, 0.1f));
            var knee = V(0.065f * s, 0.16f, 0.13f);
            var ankle = V(0.065f * s, 0.035f, 0.13f);
            b.Limb(leg, V(0.055f * s, 0.31f, 0.1f), knee, 0.04f, 0.026f, dark);
            b.Limb(leg, knee, ankle, 0.02f, 0.018f, talon, Scales);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Limb(leg, ankle, ankle + V(0.02f * t, -0.03f, 0.04f), 0.01f, 0.006f, talon, Scales);
            b.Limb(leg, ankle, ankle + V(0, -0.03f, -0.03f), 0.009f, 0.006f, talon, Scales);
            int hind = b.Leg(s, V(0.06f * s, 0.28f, -0.14f), false);
            var hk = V(0.075f * s, 0.15f, -0.1f);
            var ha = V(0.075f * s, 0.04f, -0.17f);
            b.Limb(hind, V(0.06f * s, 0.29f, -0.14f), hk, 0.055f, 0.035f, dark);
            b.Limb(hind, hk, ha, 0.03f, 0.026f, dark);
            b.Ell(hind, ha + V(0, -0.015f, 0.02f), V(0.03f, 0.025f, 0.045f), dark);
        });
        var bc = V(0, 0.32f, -0.02f);
        b.Ell(Body, bc, V(0.085f, 0.085f, 0.17f), dark);
        // Ragged armour over the back and shoulders, torn into strips at its edges
        b.Ell(Body, bc + V(0, 0.03f, 0.02f), V(0.095f, 0.075f, 0.16f), armour, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 5; i++)
            {
                float z = 0.12f - i * 0.06f;
                b.Spike(Body, bc + V(0.08f * s, 0.0f, z), bc + V(0.1f * s, -0.07f, z - 0.01f), 0.025f, armour, 0.35f);
            }
        });
        int head = b.Head(bc + V(0, 0.06f, 0.15f));
        var c = bc + V(0, 0.13f, 0.22f);
        b.Limb(head, bc + V(0, 0.04f, 0.13f), c, 0.05f, 0.045f, dark);
        b.Box(head, c, V(0.068f, 0.062f, 0.085f), 0.035f, helm, mat: Metal);
        // The crest: a great blade like an axe's head, rising from the back of the helmet and curving over it, and a ridge down its snout
        b.Ell(head, c + V(0, 0.075f, -0.02f), V(0.012f, 0.085f, 0.1f), blade, V(-20f, 0, 0), Metal, 0.008f);
        b.Box(head, c + V(0, 0.03f, 0.1f), V(0.008f, 0.05f, 0.03f), 0.006f, blade, mat: Metal);
        PokeBuilder.Both(s =>
        {
            b.Mark(head, c + V(0.068f * s, 0.0f, 0.01f), V(s, 0, 0), 0.012f, 0.012f, bolt, MarkShape.Ring);
            b.Mark(head, c + V(0.036f * s, 0.012f, 0.085f), V(0, 0, 1f), 0.01f, 0.0035f, Rgb(30, 26, 30), MarkShape.Bar, -15f * s);
        });
        int tail = b.Tail(bc + V(0, 0.0f, -0.16f));
        var end = bc + V(0, 0.06f, -0.3f);
        b.Limb(tail, bc + V(0, 0.0f, -0.16f), end, 0.04f, 0.025f, dark);
        Frond(b, tail, end, end + V(0, 0.1f, -0.12f), 0.045f, fin, V(1f, 0, 0), 0.2f);
        Frond(b, tail, end, end + V(0, -0.05f, -0.13f), 0.04f, fin, V(1f, 0, 0), 0.2f);
        return b;
    }

    /// <summary>The memory each of Silvally's forms carries: its type after "Silvally-" (null for the Normal type), the colour of its tail fin and that of its eyes.</summary>
    private static readonly (string? Type, Color Fin, Color Eye)[] SilvallyMemories =
    {
        (null, Rgb(70, 82, 126), Rgb(220, 48, 60)),
        ("Fighting", Rgb(196, 62, 48), Rgb(196, 62, 48)),
        ("Flying", Rgb(150, 170, 240), Rgb(150, 170, 240)),
        ("Poison", Rgb(160, 70, 170), Rgb(160, 70, 170)),
        ("Ground", Rgb(222, 186, 100), Rgb(222, 186, 100)),
        ("Rock", Rgb(182, 156, 70), Rgb(182, 156, 70)),
        ("Bug", Rgb(160, 190, 40), Rgb(160, 190, 40)),
        ("Ghost", Rgb(112, 86, 160), Rgb(112, 86, 160)),
        ("Steel", Rgb(176, 180, 200), Rgb(176, 180, 200)),
        ("Fire", Rgb(240, 128, 48), Rgb(240, 128, 48)),
        ("Water", Rgb(92, 140, 240), Rgb(92, 140, 240)),
        ("Grass", Rgb(112, 196, 80), Rgb(112, 196, 80)),
        ("Electric", Rgb(248, 210, 48), Rgb(248, 210, 48)),
        ("Psychic", Rgb(248, 90, 140), Rgb(248, 90, 140)),
        ("Ice", Rgb(140, 214, 220), Rgb(140, 214, 220)),
        ("Dragon", Rgb(112, 62, 240), Rgb(112, 62, 240)),
        ("Dark", Rgb(100, 80, 70), Rgb(100, 80, 70)),
        ("Fairy", Rgb(240, 150, 190), Rgb(240, 150, 190))
    };

    /// <summary>
    /// Silvally carrying one of its memories: Type: Null freed of its mask, a crest of white feathers swept back from a
    /// grey head with a dark jaw, a disc on each side of its head where the memory sits, a white-feathered front on a
    /// bird's taloned forelegs, a beast's dark hind legs marked purple, and a fish's tail whose fin, like its eyes, takes
    /// the memory's colour.
    /// </summary>
    private static PokeBuilder SilvallyBuild(int memory)
    {
        var (type, fin, eye) = SilvallyMemories[memory];
        var b = new PokeBuilder(type == null ? "Silvally" : "Silvally-" + type, 0.95f, BodyPlan.Quadruped, V(0, 0.38f, 0)) { Coat = Fur };
        var white = Rgb(236, 238, 242);
        var grey = Rgb(196, 200, 208);
        var dark = Rgb(54, 56, 64);
        var talon = Rgb(84, 108, 96);
        var purple = Rgb(110, 96, 170);
        PokeBuilder.Both(s =>
        {
            // A bird's forelegs, feathered white above the talons
            int leg = b.Leg(s, V(0.055f * s, 0.36f, 0.12f));
            var knee = V(0.065f * s, 0.2f, 0.15f);
            var ankle = V(0.065f * s, 0.04f, 0.14f);
            b.Limb(leg, V(0.055f * s, 0.37f, 0.12f), knee, 0.045f, 0.03f, white);
            b.Limb(leg, knee, ankle, 0.02f, 0.018f, talon, Scales);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Limb(leg, ankle, ankle + V(0.022f * t, -0.035f, 0.045f), 0.01f, 0.006f, talon, Scales);
            b.Limb(leg, ankle, ankle + V(0, -0.035f, -0.035f), 0.009f, 0.006f, talon, Scales);
            // A beast's hind legs, dark, marked purple on the thighs
            int hind = b.Leg(s, V(0.06f * s, 0.34f, -0.16f), false);
            var hk = V(0.075f * s, 0.18f, -0.11f);
            var ha = V(0.075f * s, 0.045f, -0.19f);
            b.Limb(hind, V(0.06f * s, 0.35f, -0.16f), hk, 0.06f, 0.038f, dark);
            b.Limb(hind, hk, ha, 0.032f, 0.026f, dark);
            b.Ell(hind, ha + V(0, -0.02f, 0.02f), V(0.03f, 0.025f, 0.045f), dark);
            b.PaintEll(hind, V(0.1f * s, 0.29f, -0.15f), V(0.03f, 0.05f, 0.04f), purple);
        });
        var bc = V(0, 0.38f, -0.02f);
        b.Ell(Body, bc + V(0, 0, -0.09f), V(0.092f, 0.092f, 0.13f), dark);
        b.Ell(Body, bc + V(0, 0.02f, 0.08f), V(0.102f, 0.112f, 0.11f), white);
        FurTufts(b, Body, bc + V(0, -0.02f, 0.1f), V(0.09f, 0.09f, 0.09f), 12, 0.04f, 0.016f, white, 1.2f, -0.95f, 0.6f);
        int head = b.Head(bc + V(0, 0.12f, 0.14f));
        var c = bc + V(0, 0.18f, 0.22f);
        var r = V(0.052f, 0.055f, 0.072f);
        b.Limb(head, bc + V(0, 0.06f, 0.12f), c + V(0, -0.02f, -0.03f), 0.055f, 0.04f, white);
        b.Ell(head, c, r, grey);
        // The lower jaw, dark, set with white teeth
        b.Ell(head, c + V(0, -0.04f, 0.03f), V(0.035f, 0.02f, 0.055f), dark, blend: 0.01f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.018f * s, -0.028f, 0.075f), c + V(0.019f * s, -0.012f, 0.081f), 0.006f, White, mat: Shell, blend: 0.003f));
        // The crest: broad white feathers swept up and back from the crown
        foreach (var (x, up, back) in new[] { (-0.035f, 0.08f, 0.1f), (-0.015f, 0.11f, 0.14f), (0.015f, 0.11f, 0.14f), (0.035f, 0.08f, 0.1f), (0f, 0.06f, 0.18f) })
            Frond(b, head, c + V(x * 0.6f, 0.035f, -0.01f), c + V(x * 2.2f, 0.035f + up, -0.02f - back), 0.03f, white, V(1f, 0, 0.3f), 0.2f);
        PokeBuilder.Both(s =>
        {
            // The memory's disc on each side of the head, and the eyes in its colour
            var side = Out(c, r, default, V(s, -0.06f, -0.25f));
            b.Mark(head, side, Outward(c, r, side), 0.012f, 0.012f, Rgb(150, 154, 164), MarkShape.Ring);
            b.Mark(head, side, Outward(c, r, side), 0.007f, 0.007f, fin);
            var at = On(c, r, 0.024f * s, c.Y + 0.014f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, eye, glare: true);
        });
        int tail = b.Tail(bc + V(0, 0.01f, -0.19f));
        var end = bc + V(0, 0.09f, -0.36f);
        b.Tube(tail, Smooth(3, bc + V(0, 0.01f, -0.19f), bc + V(0, 0.03f, -0.28f), end), 0.04f, 0.022f, dark, blend: 0f);
        // The fish's tail fin in the memory's colour, ribbed white
        Frond(b, tail, end, end + V(0, 0.16f, -0.12f), 0.06f, fin, V(1f, 0, 0), 0.2f);
        Frond(b, tail, end, end + V(0, -0.04f, -0.17f), 0.05f, fin, V(1f, 0, 0), 0.2f);
        foreach (var d in new[] { V(0, 0.12f, -0.1f), V(0, 0.05f, -0.15f), V(0, -0.02f, -0.15f) })
            b.Limb(tail, end, end + d, 0.014f, 0.008f, white);
        return b;
    }

    private static PokeBuilder Silvally() => SilvallyBuild(0);

    // ------------------------------------------------------------------ Minior

    /// <summary>The colour of each of Minior's cores, after "Minior-".</summary>
    private static readonly (string Name, Color Core)[] MiniorCores =
    {
        ("Red", Rgb(236, 84, 108)), ("Orange", Rgb(246, 150, 70)), ("Yellow", Rgb(246, 216, 80)), ("Green", Rgb(120, 200, 110)),
        ("Blue", Rgb(96, 196, 240)), ("Indigo", Rgb(96, 124, 224)), ("Violet", Rgb(176, 116, 230))
    };

    /// <summary>
    /// Minior in its Meteor Form or one of its cores (an index into <see cref="MiniorCores"/>; below nought, the meteor).
    /// The meteor is a brown ball of rock with seven points tipped grey, dark marks on its shell and two holes through
    /// which its eyes peer; a core is a glossy ball like a star of candy, seven rounded points, a shine on it and white
    /// eyes. The meteors' cores don't show, so all of them look alike.
    /// </summary>
    private static PokeBuilder MiniorBuild(int core)
    {
        bool meteor = core < 0;
        var b = new PokeBuilder(meteor ? "Minior" : "Minior-" + MiniorCores[core].Name, 0.45f, BodyPlan.Floating, V(0, 0.2f, 0))
            { Coat = meteor ? Fur : Shell }.Hover();
        var colour = meteor ? Rgb(150, 106, 86) : MiniorCores[core].Core;
        var c = V(0, 0.2f, 0);
        var r = V(0.11f, 0.11f, 0.11f);
        b.Ell(Body, c, r, colour);
        // Seven points, the front left clear for the face
        var points = new[]
        {
            V(0, 1f, 0.1f), V(0.87f, -0.15f, 0.5f), V(-0.87f, -0.15f, 0.5f), V(0.5f, 0.2f, -0.87f), V(-0.5f, 0.2f, -0.87f),
            V(0, -1f, -0.1f), V(0, 0.45f, -0.9f)
        };
        foreach (var p in points)
        {
            var d = Vector3.Normalize(p);
            if (meteor)
            {
                b.Spike(Body, c + d * 0.07f, c + d * 0.175f, 0.045f, colour);
                b.PaintEll(Body, c + d * 0.17f, V(0.025f, 0.025f, 0.025f), Rgb(170, 170, 176));
            }
            else
                b.Limb(Body, c + d * 0.06f, c + d * 0.15f, 0.045f, 0.02f, colour);
        }
        if (meteor)
        {
            foreach (var p in new[] { V(0.7f, 0.6f, -0.3f), V(-0.6f, 0.65f, -0.2f), V(0.2f, -0.5f, -0.8f), V(-0.75f, -0.55f, -0.2f) })
            {
                var at = Out(c, r, default, p);
                b.Mark(Body, at, Outward(c, r, at), 0.018f, 0.018f, Rgb(96, 64, 52), MarkShape.Triangle, p.X * 40f);
            }
        }
        else
            b.PaintEll(Body, Out(c, r, default, V(-0.5f, 0.6f, 0.6f)), V(0.035f, 0.025f, 0.03f), PixelCanvas.Mix(colour, White, 0.55f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.015f);
            if (meteor)
            {
                b.PaintEll(Body, at, V(0.03f, 0.032f, 0.02f), Rgb(30, 24, 28));
                b.Eye(Body, at, Outward(c, r, at), 0.018f, Rgb(50, 40, 44));
            }
            else
                b.Eye(Body, at, Outward(c, r, at), 0.022f, pupil: PixelCanvas.Mix(colour, Black, 0.45f), white: White);
        });
        return Lift(b);
    }

    private static PokeBuilder Minior() => MiniorBuild(-1);

    // ------------------------------------------------------------------ Komala

    /// <summary>Komala: a koala sound asleep, blue-grey with peach in its great round ears, a broad dark nose, white tufts at its cheeks, hugging the hollow log it never lets go of.</summary>
    private static PokeBuilder Komala()
    {
        var b = new PokeBuilder("Komala", 0.5f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        var blue = Rgb(150, 182, 216);
        var peach = Rgb(244, 200, 168);
        var nose = Rgb(70, 76, 92);
        var wood = Rgb(140, 96, 64);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.08f, 0));
            b.Ell(leg, V(0.055f * s, 0.035f, 0.02f), V(0.035f, 0.035f, 0.045f), blue);
        });
        var bc = V(0, 0.15f, 0);
        b.Ell(Body, bc, V(0.085f, 0.09f, 0.075f), blue);
        // The log, held across its front, its cut end pale
        var la = V(-0.12f, 0.06f, 0.1f);
        var lb = V(0.1f, 0.17f, 0.11f);
        b.Limb(Body, la, lb, 0.05f, 0.048f, wood);
        b.PaintEll(Body, la - Vector3.Normalize(lb - la) * 0.04f, V(0.038f, 0.038f, 0.038f), Rgb(226, 206, 170));
        foreach (var t in new[] { 0.45f, 0.75f })
            b.PaintEll(Body, Vector3.Lerp(la, lb, t) + V(0, -0.02f, 0.045f), V(0.01f, 0.014f, 0.01f), Rgb(90, 60, 42));
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.07f * s, 0.05f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var hand = Vector3.Lerp(la, lb, s > 0 ? 0.85f : 0.3f) + V(0, 0.045f, 0.02f);
            b.Limb(arm, shoulder, hand, 0.026f, 0.022f, blue);
            b.Ell(arm, hand, V(0.025f, 0.022f, 0.025f), blue);
        });
        int head = b.Head(bc + V(0, 0.08f, 0.01f));
        var c = bc + V(0, 0.15f, 0.02f);
        var r = V(0.075f, 0.065f, 0.062f);
        b.Ell(head, c, r, blue);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.04f, -0.01f));
            b.Ell(ear, c + V(0.1f * s, 0.05f, -0.01f), V(0.048f, 0.045f, 0.02f), blue);
            b.PaintEll(ear, c + V(0.1f * s, 0.05f, 0.01f), V(0.032f, 0.03f, 0.012f), peach);
            foreach (var (dx, dy) in new[] { (0f, 0f), (0.012f, 0.012f), (0.014f, -0.01f) })
                b.Ell(head, c + V((0.062f + dx) * s, -0.03f + dy, 0.03f), V(0.014f, 0.012f, 0.012f), White, blend: 0.008f);
            var at = On(c, r, 0.034f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(50, 50, 60), closed: true);
        });
        b.Ell(head, c + V(0, -0.005f, 0.055f), V(0.024f, 0.034f, 0.018f), nose, blend: 0.01f);
        Grin(b, head, On(c, r, 0, c.Y - 0.045f), V(0.012f, 0.007f, 0.008f), Rgb(230, 120, 140));
        return b;
    }

    // ------------------------------------------------------------------ Turtonator

    /// <summary>Turtonator: a turtle risen on its hind legs, grey-green, its back one great shell rimmed red with a yellow star at its heart and dark spikes bursting from it, a helm of yellow edged red on its head, a pale belly and a tail of red spikes.</summary>
    private static PokeBuilder Turtonator()
    {
        var b = new PokeBuilder("Turtonator", 0.9f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Scales };
        var skin = Rgb(112, 122, 98);
        var belly = Rgb(196, 200, 170);
        var red = Rgb(226, 74, 70);
        var yellow = Rgb(246, 210, 70);
        var spike = Rgb(96, 58, 50);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.18f, 0));
            b.Limb(leg, V(0.08f * s, 0.19f, 0), V(0.1f * s, 0.05f, 0.02f), 0.055f, 0.045f, skin);
            b.Ell(leg, V(0.1f * s, 0.03f, 0.04f), V(0.05f, 0.03f, 0.06f), skin);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.1f * s + 0.025f * t, 0.02f, 0.08f), V(0.1f * s + 0.03f * t, 0.01f, 0.11f), 0.01f, Rgb(236, 230, 210), mat: Shell);
        });
        var bc = V(0, 0.3f, 0);
        b.Ell(Body, bc, V(0.13f, 0.15f, 0.11f), skin);
        b.PaintEll(Body, bc + V(0, -0.01f, 0.06f), V(0.1f, 0.13f, 0.07f), belly);
        // The shell: red, a yellow star painted in eight strokes at its heart, dark spikes bursting from it
        var sc = bc + V(0, 0.03f, -0.1f);
        var sr = V(0.19f, 0.2f, 0.1f);
        b.Ell(Body, sc, sr, red, blend: 0.015f);
        var back = Out(sc, sr, default, V(0, 0, -1f));
        foreach (float a in new[] { 0f, 45f, 90f, 135f })
            b.PaintEll(Body, back, V(0.028f, 0.15f, 0.04f), yellow, V(0, 0, a));
        for (int i = 0; i < 10; i++)
        {
            float a = (i * 36f + 18f) * Degree;
            var d = V(MathF.Cos(a) * 0.85f, MathF.Sin(a) * 0.85f, -0.55f);
            var at = Out(sc, sr, default, d);
            var n = Outward(sc, sr, at);
            b.Spike(Body, at - n * 0.02f, at + n * 0.1f, 0.04f, spike, mat: Shell);
        }
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.12f * s, 0.08f, 0.03f);
            int arm = b.Arm(s, shoulder);
            var hand = shoulder + V(0.06f * s, -0.12f, 0.06f);
            b.Limb(arm, shoulder, hand, 0.035f, 0.03f, skin);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand + V(0.012f * t, -0.01f, 0.01f), hand + V(0.015f * t, -0.04f, 0.03f), 0.008f, Rgb(236, 230, 210), mat: Shell);
        });
        int tail = b.Tail(bc + V(0, -0.12f, -0.08f));
        b.Limb(tail, bc + V(0, -0.12f, -0.08f), V(0, 0.05f, -0.26f), 0.05f, 0.02f, skin);
        foreach (float t in new[] { 0.3f, 0.6f, 0.9f })
        {
            var at = Vector3.Lerp(bc + V(0, -0.12f, -0.08f), V(0, 0.05f, -0.26f), t);
            b.Spike(tail, at + V(0, 0.01f, 0), at + V(0, 0.06f, -0.02f), 0.025f, red, mat: Shell);
        }
        int head = b.Head(bc + V(0, 0.14f, 0.04f));
        var c = bc + V(0, 0.2f, 0.07f);
        var r = V(0.055f, 0.05f, 0.06f);
        b.Limb(head, bc + V(0, 0.1f, 0.03f), c + V(0, -0.02f, -0.01f), 0.05f, 0.04f, skin);
        b.Ell(head, c, r, skin);
        b.Ell(head, c + V(0, -0.012f, 0.05f), V(0.035f, 0.03f, 0.035f), skin);
        // The helm: yellow over the crown, edged with red points
        b.Ell(head, c + V(0, 0.035f, -0.01f), V(0.062f, 0.03f, 0.062f), yellow, blend: 0.01f);
        for (int i = 0; i < 5; i++)
        {
            float a = (i * 45f - 90f) * Degree;
            var d = V(MathF.Sin(a), 0.25f, -MathF.Cos(a));
            b.Spike(head, c + V(0, 0.035f, -0.01f) + d * 0.05f, c + V(0, 0.035f, -0.01f) + d * 0.09f + V(0, 0.02f, 0), 0.016f, red, 0.5f, Shell);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(220, 60, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Togedemaru

    /// <summary>Togedemaru: a round little hedgehog, white in front and grey behind, yellow cheeks, round ears, brown points at its sides, and a long grey spine down its back tipped yellow and black that it raises like a lightning rod.</summary>
    private static PokeBuilder Togedemaru()
    {
        var b = new PokeBuilder("Togedemaru", 0.42f, BodyPlan.Biped, V(0, 0.13f, 0)) { Coat = Fur };
        var grey = Rgb(176, 180, 186);
        var white = Rgb(246, 246, 244);
        var yellow = Rgb(248, 226, 90);
        var brown = Rgb(150, 126, 100);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.05f, 0.02f));
            b.Ell(leg, V(0.045f * s, 0.018f, 0.045f), V(0.025f, 0.018f, 0.03f), white);
        });
        var c = V(0, 0.135f, 0);
        var r = V(0.12f, 0.115f, 0.11f);
        b.Ell(Body, c, r, grey);
        b.PaintEll(Body, c + V(0, -0.015f, 0.07f), V(0.1f, 0.11f, 0.08f), white);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, c + V(0.07f * s, -0.03f, 0.07f));
            b.Ell(arm, c + V(0.075f * s, -0.045f, 0.09f), V(0.02f, 0.015f, 0.02f), white);
            int ear = b.Ear(Body, s, c + V(0.06f * s, 0.09f, 0));
            b.Ell(ear, c + V(0.068f * s, 0.11f, 0), V(0.034f, 0.032f, 0.016f), grey);
            b.PaintEll(ear, c + V(0.068f * s, 0.11f, 0.012f), V(0.02f, 0.019f, 0.007f), white);
            var cheek = On(c, r, 0.065f * s, c.Y - 0.015f);
            b.PaintEll(Body, cheek, V(0.024f, 0.022f, 0.02f), yellow);
            foreach (float y in new[] { 0.03f, -0.045f })
                b.Spike(Body, c + V(0.105f * s, y, -0.03f), c + V(0.15f * s, y - 0.01f, -0.05f), 0.026f, brown, 0.4f, Shell);
            var at = On(c, r, 0.03f * s, c.Y + 0.03f);
            b.Eye(Body, at, Outward(c, r, at), 0.012f, Rgb(40, 36, 40));
        });
        var smile = On(c, r, 0, c.Y + 0.002f);
        b.Mark(Body, smile, Outward(c, r, smile), 0.01f, 0.005f, Rgb(60, 50, 50), MarkShape.Smile);
        // The spine down its back, grey, banded yellow, its tip black
        var root = c + V(0, 0.07f, -0.07f);
        var tip = c + V(0, 0.2f, -0.16f);
        b.Limb(Body, root, tip, 0.035f, 0.012f, grey);
        b.PaintEll(Body, Vector3.Lerp(root, tip, 0.7f), V(0.025f, 0.022f, 0.025f), yellow);
        b.PaintEll(Body, tip, V(0.02f, 0.02f, 0.02f), Rgb(50, 46, 50));
        return b;
    }

    // ------------------------------------------------------------------ Mimikyu

    /// <summary>
    /// Mimikyu and its busted form: a small ghost under a rag sewn to look like Pikachu, the cloth pale yellow-brown with
    /// black-tipped ears, a face scrawled on it (circles for eyes, a zigzag mouth, red cheeks), a stick of a tail, its own
    /// dark eyes peering out from under the hem and shadowy claws at its feet. Busted, the rag's head has flopped over to
    /// one side, its neck broken.
    /// </summary>
    private static PokeBuilder MimikyuBuild(bool busted)
    {
        var b = new PokeBuilder(busted ? "Mimikyu-Busted" : "Mimikyu", 0.45f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Fur };
        var cloth = Rgb(222, 206, 150);
        var black = Rgb(40, 36, 40);
        var stick = Rgb(130, 96, 66);
        var cheek = Rgb(214, 110, 70);
        // The rag hanging to the ground, its hem in points
        b.Limb(Body, V(0, 0.19f, 0), V(0, 0.075f, 0), 0.045f, 0.08f, cloth);
        for (int i = 0; i < 6; i++)
        {
            float a = (i * 60f + 30f) * Degree;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Spike(Body, V(0, 0.035f, 0) + d * 0.07f, V(0, 0.002f, 0) + d * 0.125f, 0.028f, cloth, 0.4f);
        }
        PokeBuilder.Both(s =>
        {
            // Its own eyes under the hem, and shadowy claws
            var at = V(0.024f * s, 0.08f, 0.0745f);
            b.PaintEll(Body, at, V(0.016f, 0.02f, 0.01f), Rgb(30, 26, 30));
            b.Eye(Body, at, V(0.3f * s, 0, 1f), 0.01f, Rgb(30, 26, 30));
            b.Spike(Body, V(0.055f * s, 0.02f, 0.075f), V(0.065f * s, 0.002f, 0.12f), 0.01f, black, mat: Shell);
        });
        int tail = b.Tail(V(0, 0.08f, -0.06f));
        var zig = new[] { V(0, 0.08f, -0.06f), V(0, 0.13f, -0.11f), V(0, 0.115f, -0.15f), V(0, 0.2f, -0.2f) };
        for (int i = 0; i < zig.Length - 1; i++)
            b.Limb(tail, zig[i], zig[i + 1], 0.012f + i * 0.005f, 0.014f + i * 0.006f, stick);
        // The head of the disguise, and the neck it hangs from; busted, it has flopped over to one side
        var neck = V(0, 0.22f, 0);
        float flop = busted ? -75f : 0f;
        var turn = Quaternion.CreateFromAxisAngle(V(0, 0, 1f), flop * Degree);
        Vector3 H(Vector3 p) => neck + Vector3.Transform(p - neck, turn);
        Vector3 N(Vector3 n) => Vector3.Transform(n, turn);
        int head = b.Head(neck);
        var hc = V(0, 0.27f, 0.01f);
        var hr = V(0.085f, 0.074f, 0.072f);
        b.Ell(head, H(hc), hr, cloth, V(0, 0, flop));
        // One ear standing, black-tipped; the other bent over
        var left = new[] { hc + V(-0.045f, 0.05f, 0), hc + V(-0.075f, 0.17f, -0.005f) };
        b.Limb(head, H(left[0]), H(left[1]), 0.022f, 0.01f, cloth);
        b.PaintEll(head, H(left[1] + V(0, -0.01f, 0)), V(0.022f, 0.03f, 0.022f), black);
        var right = new[] { hc + V(0.045f, 0.05f, 0), hc + V(0.09f, 0.125f, -0.005f), hc + V(0.17f, 0.1f, -0.01f) };
        b.Limb(head, H(right[0]), H(right[1]), 0.022f, 0.016f, cloth);
        b.Limb(head, H(right[1]), H(right[2]), 0.016f, 0.008f, cloth);
        b.PaintEll(head, H(right[2]), V(0.028f, 0.022f, 0.022f), black);
        // The face scrawled on it
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.034f * s, hc.Y + 0.014f);
            b.Mark(head, H(at), N(Outward(hc, hr, at)), 0.013f, 0.013f, black, MarkShape.Ring);
            var ch = On(hc, hr, 0.056f * s, hc.Y - 0.024f);
            b.Mark(head, H(ch), N(Outward(hc, hr, ch)), 0.01f, 0.008f, cheek);
        });
        var mouth = On(hc, hr, 0, hc.Y - 0.022f);
        b.Mark(head, H(mouth), N(Outward(hc, hr, mouth)), 0.026f, 0.008f, black, MarkShape.Zigzag);
        return b;
    }

    private static PokeBuilder Mimikyu() => MimikyuBuild(false);

    // ------------------------------------------------------------------ Bruxish

    /// <summary>Bruxish: a gaudy fish, pink with patches of yellow and blue and blue spots, thick pink lips over a grin of sharp white teeth, a great psychic eye on each side, a purple knob of a fin on its head, a wavy blue crest and purple fins and tail.</summary>
    private static PokeBuilder Bruxish()
    {
        var b = new PokeBuilder("Bruxish", 0.7f, BodyPlan.Fish, V(0, 0.25f, 0)) { Coat = Scales }.Hover();
        var pink = Rgb(236, 104, 160);
        var yellow = Rgb(248, 226, 90);
        var blue = Rgb(120, 200, 236);
        var purple = Rgb(150, 90, 180);
        var c = V(0, 0.25f, 0);
        var r = V(0.07f, 0.12f, 0.17f);
        b.Ell(Body, c, r, pink);
        b.PaintEll(Body, c + V(0, -0.09f, 0.02f), V(0.09f, 0.05f, 0.16f), yellow);
        b.PaintEll(Body, c + V(0, -0.01f, 0.16f), V(0.09f, 0.11f, 0.04f), blue);
        b.PaintEll(Body, c + V(0, 0.0f, 0.12f), V(0.09f, 0.12f, 0.012f), yellow);
        PokeBuilder.Both(s =>
        {
            foreach (var p in new[] { V(0.8f, 0.4f, -0.2f), V(0.8f, -0.1f, -0.45f), V(0.75f, 0.45f, -0.6f), V(0.8f, -0.35f, 0.05f) })
            {
                var at = Out(c, r, default, V(p.X * s, p.Y, p.Z));
                b.Mark(Body, at, Outward(c, r, at), 0.012f, 0.012f, blue);
            }
            Frond(b, Body, c + V(0.05f * s, -0.05f, 0.02f), c + V(0.11f * s, -0.1f, -0.04f), 0.025f, purple, V(0, 1f, 0.3f), 0.25f);
            var eye = Out(c, r, default, V(0.7f * s, 0.35f, 0.62f));
            b.Eye(Body, eye, Outward(c, r, eye), 0.026f, pupil: Rgb(200, 60, 130), white: Rgb(250, 228, 110));
        });
        // The lips and the teeth between them
        var mouth = c + V(0, -0.03f, r.Z - 0.01f);
        b.Ell(Body, mouth + V(0, 0.022f, 0.005f), V(0.04f, 0.016f, 0.025f), Rgb(240, 120, 170), blend: 0.008f);
        b.Ell(Body, mouth + V(0, -0.022f, 0.0f), V(0.036f, 0.016f, 0.025f), Rgb(240, 120, 170), blend: 0.008f);
        b.PaintEll(Body, mouth + V(0, 0, 0.025f), V(0.03f, 0.01f, 0.012f), Rgb(80, 30, 60));
        foreach (float x in new[] { -0.02f, -0.007f, 0.007f, 0.02f })
        {
            b.Spike(Body, mouth + V(x, 0.008f, 0.025f), mouth + V(x, -0.004f, 0.03f), 0.005f, White, mat: Shell, blend: 0.002f);
            b.Spike(Body, mouth + V(x, -0.008f, 0.023f), mouth + V(x, 0.004f, 0.028f), 0.005f, White, mat: Shell, blend: 0.002f);
        }
        // The purple knob on its head and the wavy crest down its back
        b.Limb(Body, c + V(0, 0.09f, 0.05f), c + V(0, 0.19f, 0.03f), 0.022f, 0.03f, purple);
        b.Ell(Body, c + V(0, 0.2f, 0.03f), V(0.035f, 0.028f, 0.03f), purple);
        for (int i = 0; i < 3; i++)
            Blade(b, Body, c + V(0, 0.1f, -0.02f - i * 0.05f), c + V(0, 0.15f, -0.05f - i * 0.05f), 0.03f, blue, V(1f, 0, 0), 0.25f);
        int tail = b.Tail(c + V(0, 0, -r.Z + 0.02f));
        var end = c + V(0, 0, -r.Z - 0.03f);
        b.Limb(tail, c + V(0, 0, -r.Z + 0.02f), end, 0.04f, 0.02f, pink);
        PokeBuilder.Both(s => Frond(b, tail, end, end + V(0, 0.08f * s, -0.07f), 0.04f, purple, V(1f, 0, 0), 0.22f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Drampa

    /// <summary>
    /// Drampa and its Mega Evolution: a gentle old dragon lying in a bank of cloud, its long green neck arched up to a
    /// green head with curls of yellow over its eyes and a white beard, a green tail with a wavy fin. Drampa's cloud is
    /// white; Mega Drampa's is a dark storm of slate, a white mane runs down its neck and white horns sweep back from its
    /// head.
    /// </summary>
    private static PokeBuilder DrampaBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Drampa-Mega" : "Drampa", 1f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        var cloud = mega ? Rgb(70, 78, 104) : Rgb(244, 242, 232);
        var light = mega ? Rgb(96, 104, 132) : White;
        var green = Rgb(110, 184, 160);
        var yellow = Rgb(248, 220, 110);
        // The bank of cloud it lies in
        foreach (var (z, rad) in new[] { (0.08f, 0.14f), (-0.1f, 0.15f), (-0.26f, 0.12f), (-0.38f, 0.09f) })
        {
            b.Ell(Body, V(0, rad * 0.95f, z), V(rad * 1.2f, rad, rad), cloud);
            Lumps(b, Body, V(0, rad * 0.95f, z), V(rad * 1.2f, rad, rad), 6, rad * 0.45f, cloud, light, 10f);
        }
        // The neck rising in an arch to the head
        var neck = Smooth(3, V(0, 0.22f, 0.08f), V(0, 0.48f, 0.1f), V(0, 0.54f, 0.22f), V(0, 0.46f, 0.32f));
        int head = b.Head(neck[0]);
        b.Tube(head, neck, 0.06f, 0.045f, green, blend: 0f);
        if (mega)
            for (int i = 2; i < neck.Length - 1; i += 2)
                b.Spike(head, neck[i] + V(0, 0.01f, -0.01f), neck[i] + V(0, 0.06f, -0.05f), 0.02f, White, 0.6f);
        var c = V(0, 0.43f, 0.38f);
        var r = V(0.065f, 0.055f, 0.085f);
        b.Ell(head, c, r, green);
        // The white beard and the curls over its eyes (horns, for the Mega)
        b.Ell(head, c + V(0, -0.055f, -0.01f), V(0.05f, 0.035f, 0.05f), White, blend: 0.012f);
        FurTufts(b, head, c + V(0, -0.06f, -0.01f), V(0.05f, 0.035f, 0.05f), 8, 0.03f, 0.014f, White, 1.2f, -0.9f, 0.9f);
        PokeBuilder.Both(s =>
        {
            if (mega)
                b.Tube(head, Smooth(2, c + V(0.04f * s, 0.04f, -0.02f), c + V(0.075f * s, 0.085f, -0.09f), c + V(0.085f * s, 0.075f, -0.17f)), 0.017f, 0.007f, White, Shell, 0f);
            else
                b.Torus(head, c + V(0.048f * s, 0.04f, 0.0f), 0.02f, 0.009f, yellow, V(0, 0, 90f), blend: 0.004f);
            var at = On(c, r, 0.038f * s, c.Y + 0.014f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(60, 50, 70));
            var ch = On(c, r, 0.05f * s, c.Y - 0.012f);
            b.Mark(head, ch, Outward(c, r, ch), 0.009f, 0.007f, Rgb(240, 150, 180));
        });
        // The tail lying out behind, a wavy green fin along it, a curl of cloud at its end
        int tail = b.Tail(V(0, 0.14f, -0.4f));
        var tp = Smooth(3, V(0, 0.14f, -0.4f), V(0, 0.18f, -0.52f), V(0, 0.24f, -0.6f));
        b.Tube(tail, tp, 0.05f, 0.028f, green, blend: 0f);
        foreach (var t in new[] { 0.4f, 0.75f })
        {
            var at = tp[(int)(t * (tp.Length - 1))];
            Blade(b, tail, at, at + V(0, 0.06f, -0.05f), 0.035f, green, V(1f, 0, 0), 0.25f);
        }
        b.Ell(tail, tp[^1] + V(0, 0.02f, -0.01f), V(0.05f, 0.045f, 0.05f), cloud);
        if (mega)
        {
            var bc = V(0, 0.133f, 0.08f);
            var br = V(0.168f, 0.14f, 0.14f);
            var chest = Out(bc, br, default, V(0, 0.75f, 1f));
            b.Mark(Body, chest, Outward(bc, br, chest), 0.022f, 0.022f, green, MarkShape.Diamond);
        }
        return b;
    }

    private static PokeBuilder Drampa() => DrampaBuild(false);

    // ------------------------------------------------------------------ Dhelmise

    /// <summary>Dhelmise: a tangle of dark green seaweed that has taken an old anchor and a ship's wheel for its body: the rusty anchor's arms curving down, the wheel behind its head, the weed's head forked like antlers and its ragged ends hanging below, and one porthole of an eye on the wheel.</summary>
    private static PokeBuilder Dhelmise()
    {
        var b = new PokeBuilder("Dhelmise", 1f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Leaf }.Hover();
        var weed = Rgb(40, 104, 110);
        var rust = Rgb(150, 92, 60);
        var wheel = Rgb(120, 90, 110);
        var brass = Rgb(232, 200, 80);
        // The anchor: a shaft, and arms curving down to points
        b.Limb(Body, V(0, 0.18f, -0.02f), V(0, 0.62f, -0.02f), 0.025f, 0.022f, rust, Metal);
        PokeBuilder.Both(s =>
        {
            var arc = Smooth(3, V(0, 0.42f, -0.02f), V(0.12f * s, 0.45f, -0.02f), V(0.2f * s, 0.36f, -0.02f), V(0.22f * s, 0.22f, -0.02f));
            b.Tube(Body, arc, 0.024f, 0.016f, rust, Metal, 0f);
            b.Spike(Body, arc[^1] + V(0, 0.02f, 0), arc[^1] + V(0.02f * s, -0.05f, 0), 0.03f, rust, 0.4f, Metal);
        });
        // The wheel behind its head: a rim, a ring of brass, spokes ending in handles
        var wc = V(0, 0.64f, -0.05f);
        b.Ell(Body, wc, V(0.03f, 0.03f, 0.02f), wheel, mat: Shell);
        b.Torus(Body, wc, 0.15f, 0.014f, wheel, V(90f, 0, 0), mat: Shell);
        b.Torus(Body, wc, 0.11f, 0.008f, brass, V(90f, 0, 0), mat: Metal);
        for (int k = 0; k < 8; k++)
        {
            float a = (k * 45f + 22.5f) * Degree;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            b.Limb(Body, wc, wc + d * 0.19f, 0.009f, 0.009f, wheel, Shell);
            b.Ell(Body, wc + d * 0.2f, V(0.014f, 0.014f, 0.014f), brass, mat: Metal);
        }
        // The weed: a stalk down the front of the shaft, its head forked like antlers, its ends hanging ragged below
        b.Tube(Body, Smooth(3, V(0, 0.2f, 0.03f), V(0, 0.42f, 0.035f), V(0, 0.64f, 0.04f)), 0.04f, 0.03f, weed, blend: 0f);
        b.Ell(Body, V(0, 0.68f, 0.04f), V(0.055f, 0.06f, 0.04f), weed);
        PokeBuilder.Both(s =>
        {
            b.Tube(Body, Smooth(2, V(0.03f * s, 0.7f, 0.04f), V(0.08f * s, 0.78f, 0.035f), V(0.13f * s, 0.8f, 0.03f)), 0.018f, 0.008f, weed, blend: 0f);
            b.Tube(Body, Smooth(2, V(0.025f * s, 0.62f, 0.05f), V(0.09f * s, 0.6f, 0.05f), V(0.14f * s, 0.65f, 0.04f)), 0.012f, 0.006f, weed, blend: 0f);
        });
        b.Ell(Body, V(0, 0.2f, 0.03f), V(0.09f, 0.045f, 0.045f), weed);
        for (int i = 0; i < 7; i++)
        {
            float x = -0.12f + i * 0.04f;
            float w = (i % 2 == 0 ? 1f : -1f) * 0.012f;
            b.Tube(Body, Smooth(3, V(x * 0.5f, 0.2f, 0.03f), V(x + w, 0.12f, 0.04f), V(x * 1.1f - w, 0.05f, 0.03f)), 0.016f, 0.008f, weed, blend: 0f);
        }
        // Its eye: a porthole on the wheel, ringed in brass
        var eye = wc + V(0.11f, 0.0f, 0.0f);
        b.Torus(Body, eye + V(0, 0, 0.012f), 0.042f, 0.009f, brass, V(90f, 0, 0), mat: Metal);
        b.Ell(Body, eye + V(0, 0, 0.01f), V(0.04f, 0.04f, 0.012f), Rgb(240, 230, 200), mat: Shell);
        b.Eye(Body, eye + V(0, 0, 0.022f), V(0, 0, 1f), 0.024f, Rgb(230, 90, 50), glare: true);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Jangmo-o line

    /// <summary>Jangmo-o: a little grey dragon on all fours, a great yellow scale like a crest over its brow, fierce red eyes, a white tuft at its neck, dark legs, scales edged yellow down its back and a yellow knob at its tail's end.</summary>
    private static PokeBuilder Jangmoo()
    {
        var b = new PokeBuilder("Jangmo-o", 0.5f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Scales };
        var grey = Rgb(186, 186, 190);
        var dark = Rgb(70, 66, 74);
        var yellow = Rgb(246, 210, 84);
        BeastLegs(b, 0.045f, 0.1f, 0.06f, -0.06f, 0.028f, grey, dark, dark);
        var bc = V(0, 0.13f, 0);
        b.Ell(Body, bc, V(0.06f, 0.055f, 0.1f), grey);
        foreach (float z in new[] { 0.04f, -0.02f, -0.08f })
        {
            b.Ell(Body, bc + V(0, 0.045f, z), V(0.042f, 0.016f, 0.035f), grey, blend: 0.006f);
            b.PaintEll(Body, bc + V(0, 0.05f, z - 0.028f), V(0.044f, 0.014f, 0.01f), yellow);
        }
        FurTufts(b, Body, bc + V(0, 0.02f, 0.08f), V(0.04f, 0.04f, 0.03f), 8, 0.02f, 0.01f, White, 1.2f, -0.5f, 0.6f);
        int tail = b.Tail(bc + V(0, 0, -0.09f));
        b.Limb(tail, bc + V(0, 0, -0.09f), bc + V(0, 0.03f, -0.17f), 0.025f, 0.015f, grey);
        b.Ell(tail, bc + V(0, 0.035f, -0.18f), V(0.022f, 0.022f, 0.022f), yellow, mat: Shell);
        int head = b.Head(bc + V(0, 0.05f, 0.08f));
        var c = bc + V(0, 0.1f, 0.12f);
        var r = V(0.06f, 0.055f, 0.058f);
        b.Limb(head, bc + V(0, 0.03f, 0.06f), c + V(0, -0.02f, -0.02f), 0.035f, 0.03f, grey);
        b.Ell(head, c, r, grey);
        b.Ell(head, c + V(0, -0.02f, 0.04f), V(0.035f, 0.028f, 0.035f), grey);
        // The great yellow scale over its brow, swept up at the front
        b.Ell(head, c + V(0, 0.055f, -0.005f), V(0.06f, 0.026f, 0.054f), yellow, V(-15f, 0, 0), Shell, 0.008f);
        b.Ell(head, c + V(0, 0.07f, 0.035f), V(0.05f, 0.022f, 0.03f), yellow, V(-35f, 0, 0), Shell, 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(220, 50, 50), sclera: true, glare: true);
        });
        return b;
    }

    /// <summary>Hakamo-o: a scaled dragon risen on two legs, grey, a crown of yellow scales like a heart on its head, great yellow scales marked red for shields on its forearms, a white tuft at its chest, dark legs with yellow claws and a tail ending in a yellow knob.</summary>
    private static PokeBuilder Hakamoo()
    {
        var b = new PokeBuilder("Hakamo-o", 0.8f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Scales };
        var grey = Rgb(170, 170, 176);
        var dark = Rgb(70, 66, 74);
        var yellow = Rgb(240, 200, 80);
        var red = Rgb(220, 90, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.2f, 0));
            var knee = V(0.08f * s, 0.11f, 0.03f);
            var ankle = V(0.07f * s, 0.03f, 0);
            b.Limb(leg, V(0.06f * s, 0.21f, 0), knee, 0.04f, 0.03f, grey);
            b.Limb(leg, knee, ankle, 0.028f, 0.022f, dark);
            b.Ell(leg, ankle + V(0, -0.008f, 0.02f), V(0.026f, 0.022f, 0.04f), dark);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(leg, ankle + V(0.012f * t, -0.012f, 0.05f), ankle + V(0.016f * t, -0.025f, 0.08f), 0.009f, yellow, mat: Shell);
        });
        var bc = V(0, 0.32f, 0);
        b.Ell(Body, bc, V(0.08f, 0.12f, 0.07f), grey);
        FurTufts(b, Body, bc + V(0, 0.08f, 0.05f), V(0.05f, 0.03f, 0.03f), 8, 0.025f, 0.012f, White, 1.2f, -0.6f, 0.6f);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.08f * s, 0.08f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.04f * s, -0.1f, 0.04f);
            var hand = shoulder + V(0.03f * s, -0.2f, 0.08f);
            b.Limb(arm, shoulder, elbow, 0.03f, 0.026f, grey);
            b.Limb(arm, elbow, hand, 0.026f, 0.024f, grey);
            b.Ell(arm, hand, V(0.03f, 0.028f, 0.03f), dark);
            // The shield scale on the forearm, a red heart at its middle
            var shield = Vector3.Lerp(elbow, hand, 0.45f) + V(0.028f * s, 0, 0.01f);
            b.Ell(arm, shield, V(0.012f, 0.05f, 0.045f), yellow, mat: Shell, blend: 0.006f);
            b.PaintEll(arm, shield + V(0.012f * s, 0, 0), V(0.008f, 0.022f, 0.02f), red);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand + V(0.012f * t, -0.015f, 0.015f), hand + V(0.015f * t, -0.04f, 0.035f), 0.008f, yellow, mat: Shell);
        });
        int tail = b.Tail(bc + V(0, -0.08f, -0.06f));
        b.Limb(tail, bc + V(0, -0.08f, -0.06f), V(0, 0.08f, -0.22f), 0.04f, 0.02f, grey);
        b.Ell(tail, V(0, 0.09f, -0.24f), V(0.028f, 0.028f, 0.028f), yellow, mat: Shell);
        int head = b.Head(bc + V(0, 0.13f, 0.03f));
        var c = bc + V(0, 0.19f, 0.05f);
        var r = V(0.045f, 0.045f, 0.05f);
        b.Limb(head, bc + V(0, 0.09f, 0.02f), c + V(0, -0.02f, -0.01f), 0.035f, 0.03f, grey);
        b.Ell(head, c, r, grey);
        b.Ell(head, c + V(0, -0.015f, 0.04f), V(0.028f, 0.024f, 0.03f), grey);
        // The crown: two yellow scales making a heart over its head
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.025f * s, 0.055f, -0.01f), V(0.03f, 0.036f, 0.012f), yellow, V(0, 0, -25f * s), Shell, 0.006f);
            b.PaintEll(head, c + V(0.025f * s, 0.055f, 0.002f), V(0.012f, 0.014f, 0.006f), red);
            var at = On(c, r, 0.022f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(220, 50, 50), sclera: true, glare: true);
        });
        return b;
    }

    /// <summary>A golden scale of the Jangmo-o line, its face turned along <paramref name="facing"/>, a red heart at its middle.</summary>
    private static void GoldScale(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float size, Color gold, Color heart)
    {
        var n = Vector3.Normalize(facing);
        b.Ell(bone, at, V(size, size * 0.28f, size), gold, Euler(n), Shell, 0.004f);
        b.PaintEll(bone, at + n * size * 0.22f, V(size * 0.45f, size * 0.2f, size * 0.45f), heart, Euler(n));
    }

    /// <summary>Kommo-o: a great dragon in armour of golden scales, each marked red at its middle, over its head, shoulders, arms and back, a grey face under a crest of them, white tufts at its chest, grey legs with dark feet and yellow claws, and a tail ending in a rattle of golden scales.</summary>
    private static PokeBuilder Kommoo()
    {
        var b = new PokeBuilder("Kommo-o", 1f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Scales };
        var grey = Rgb(160, 160, 166);
        var dark = Rgb(66, 62, 70);
        var gold = Rgb(222, 190, 80);
        var red = Rgb(214, 80, 64);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.26f, -0.01f));
            var knee = V(0.1f * s, 0.14f, 0.04f);
            var ankle = V(0.09f * s, 0.035f, 0);
            b.Limb(leg, V(0.08f * s, 0.27f, -0.01f), knee, 0.055f, 0.04f, grey);
            b.Limb(leg, knee, ankle, 0.035f, 0.03f, dark);
            b.Ell(leg, ankle + V(0, -0.01f, 0.02f), V(0.032f, 0.025f, 0.05f), dark);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, ankle + V(0.015f * t, -0.015f, 0.06f), ankle + V(0.02f * t, -0.03f, 0.095f), 0.01f, gold, mat: Shell);
            GoldScale(b, leg, Vector3.Lerp(V(0.08f * s, 0.27f, -0.01f), knee, 0.5f) + V(0.04f * s, 0, 0.01f), V(s, 0, 0.3f), 0.035f, gold, red);
        });
        var bc = V(0, 0.42f, 0);
        var br = V(0.11f, 0.15f, 0.09f);
        b.Ell(Body, bc, br, grey);
        FurTufts(b, Body, bc + V(0, 0.07f, 0.06f), V(0.06f, 0.04f, 0.04f), 10, 0.03f, 0.014f, White, 1.2f, -0.6f, 0.6f);
        // The armour of scales over the shoulders and back
        foreach (var (y, z) in new[] { (0.75f, -0.2f), (0.4f, -0.6f), (0.0f, -0.85f), (-0.4f, -0.7f) })
            foreach (float x in new[] { -0.65f, 0f, 0.65f })
            {
                var at = Out(bc, br, default, V(x, y, z));
                GoldScale(b, Body, at, Outward(bc, br, at), 0.04f, gold, red);
            }
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.1f * s, 0.1f, 0.01f);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.06f * s, -0.13f, 0.04f);
            var hand = shoulder + V(0.05f * s, -0.27f, 0.08f);
            b.Limb(arm, shoulder, elbow, 0.042f, 0.036f, grey);
            b.Limb(arm, elbow, hand, 0.036f, 0.032f, grey);
            b.Ell(arm, hand, V(0.036f, 0.034f, 0.038f), grey);
            GoldScale(b, arm, shoulder + V(0.03f * s, 0.02f, 0), V(s, 0.6f, 0), 0.045f, gold, red);
            GoldScale(b, arm, Vector3.Lerp(elbow, hand, 0.5f) + V(0.032f * s, 0, 0.005f), V(s, 0, 0.2f), 0.04f, gold, red);
            GoldScale(b, arm, hand + V(0.03f * s, 0, 0.01f), V(s, 0, 0.3f), 0.032f, gold, red);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand + V(0.014f * t, -0.02f, 0.02f), hand + V(0.018f * t, -0.05f, 0.045f), 0.009f, gold, mat: Shell);
        });
        int tail = b.Tail(bc + V(0, -0.1f, -0.07f));
        var tp = Smooth(3, bc + V(0, -0.1f, -0.07f), V(0, 0.2f, -0.25f), V(0, 0.32f, -0.34f), V(0, 0.42f, -0.36f));
        b.Tube(tail, tp, 0.045f, 0.02f, grey, blend: 0f);
        var end = tp[^1];
        foreach (var d in new[] { V(0.7f, 0.7f, 0), V(-0.7f, 0.7f, 0), V(0.7f, -0.5f, 0), V(-0.7f, -0.5f, 0), V(0, 1f, -0.2f) })
            GoldScale(b, tail, end + Vector3.Normalize(d) * 0.035f, V(0, 0, -1f) + Vector3.Normalize(d) * 0.4f, 0.035f, gold, red);
        int head = b.Head(bc + V(0, 0.15f, 0.04f));
        var c = bc + V(0, 0.21f, 0.06f);
        var r = V(0.05f, 0.05f, 0.055f);
        b.Limb(head, bc + V(0, 0.1f, 0.03f), c + V(0, -0.02f, -0.01f), 0.045f, 0.038f, grey);
        b.Ell(head, c, r, grey);
        b.Ell(head, c + V(0, -0.02f, 0.045f), V(0.032f, 0.026f, 0.032f), grey);
        // A crest of scales fanning up and back over its head
        foreach (float x in new[] { -0.045f, -0.015f, 0.015f, 0.045f })
            GoldScale(b, head, c + V(x * 1.1f, 0.065f - MathF.Abs(x) * 0.4f, -0.02f), V(x * 6f, 0.5f, 1f), 0.034f, gold, red);
        GoldScale(b, head, c + V(0, 0.035f, -0.05f), V(0, 0.5f, -1f), 0.032f, gold, red);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.024f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(220, 50, 50), sclera: true, glare: true);
        });
        return b;
    }
}
