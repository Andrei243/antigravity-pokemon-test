using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3): the seven first met in Hisui, in National
// Pokédex order, Wyrdeer (899) to Enamorus (905). Their forms are in PokemonModels.Regional.cs with the other forms.
// Paldea's first batch, from Sprigatito, follows in PokemonModels.Paldea1.cs; helpers shared with the earlier batches
// are in the files of those batches, PokemonModels.Sinnoh1.cs to PokemonModels.Galar4.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Wyrdeer

    /// <summary>Wyrdeer: a grey deer of the old north, a great white beard of fur over its chest and down to its belly, white tufts at its hooves, a fluffy white tail, and golden antlers forking wide, dark orbs set where they branch.</summary>
    private static PokeBuilder Wyrdeer()
    {
        var b = new PokeBuilder("Wyrdeer", 1f, BodyPlan.Quadruped, V(0, 0.46f, -0.02f)) { Coat = Fur };
        var grey = Rgb(196, 198, 208);
        var dark = Rgb(140, 142, 156);
        var white = Rgb(244, 242, 246);
        var hoof = Rgb(58, 54, 68);
        var antler = Rgb(226, 194, 118);
        var orb = Rgb(54, 42, 58);
        foreach (var (z, front) in new[] { (0.16f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.075f * s, 0.4f, z), front);
                var knee = V(0.08f * s, 0.23f, z + (front ? 0.01f : -0.04f));
                b.Limb(leg, V(0.075f * s, 0.42f, z), knee, 0.055f, 0.03f, grey);
                b.Limb(leg, knee, V(0.08f * s, 0.05f, z + 0.01f), 0.028f, 0.024f, dark);
                b.Ell(leg, V(0.08f * s, 0.025f, z + 0.015f), V(0.03f, 0.025f, 0.035f), hoof, mat: Shell);
                b.Ell(leg, V(0.08f * s, 0.07f, z + 0.01f), V(0.036f, 0.022f, 0.036f), white, blend: 0.01f);
            });
        var bc = V(0, 0.46f, -0.02f);
        var br = V(0.12f, 0.11f, 0.25f);
        b.Ell(Body, bc, br, grey);
        b.Ell(Body, bc + V(0, 0.02f, 0.14f), V(0.12f, 0.12f, 0.1f), grey, blend: 0.04f);
        // The great white beard over its chest
        var beard = bc + V(0, -0.02f, 0.17f);
        b.Ell(Body, beard, V(0.12f, 0.14f, 0.09f), white, blend: 0.03f);
        FurTufts(b, Body, beard + V(0, -0.03f, 0.01f), V(0.11f, 0.13f, 0.08f), 16, 0.05f, 0.022f, white, 1.1f, -1f, 0.7f);
        int tail = b.Tail(bc + V(0, 0.05f, -0.24f));
        b.Ell(tail, bc + V(0, 0.06f, -0.27f), V(0.055f, 0.065f, 0.05f), white, blend: 0.02f);
        // A long neck up to its head, the antlers forking from its brow
        int head = b.Head(bc + V(0, 0.12f, 0.19f));
        b.Limb(head, bc + V(0, 0.08f, 0.17f), bc + V(0, 0.28f, 0.23f), 0.06f, 0.048f, grey);
        var c = bc + V(0, 0.31f, 0.25f);
        var r = V(0.068f, 0.066f, 0.085f);
        b.Ell(head, c, r, grey);
        b.Ell(head, c + V(0, -0.03f, 0.08f), V(0.046f, 0.042f, 0.06f), grey, blend: 0.03f);
        b.Ell(head, c + V(0, -0.035f, 0.135f), V(0.032f, 0.028f, 0.02f), Rgb(150, 102, 112), mat: Shell, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.03f, -0.03f));
            CatEar(b, ear, c + V(0.05f * s, 0.02f, -0.03f), c + V(0.13f * s, 0.05f, -0.05f), 0.024f, grey, white);
            var path = Smooth(3, c + V(0.025f * s, 0.05f, -0.01f), c + V(0.07f * s, 0.18f, -0.04f), c + V(0.15f * s, 0.27f, -0.03f), c + V(0.25f * s, 0.33f, 0.02f));
            b.Tube(head, path, 0.02f, 0.012f, antler, Shell, 0f);
            foreach (var (k, d) in new[] { (0.3f, V(-0.05f * s, 0.12f, 0.02f)), (0.55f, V(0.0f, 0.13f, -0.02f)), (0.8f, V(0.03f * s, 0.11f, 0.03f)) })
            {
                var at = path[(int)(k * (path.Length - 1))];
                b.Limb(head, at, at + d, 0.013f, 0.009f, antler, Shell, 0.004f);
            }
            b.Ell(head, path[(int)(0.42f * (path.Length - 1))], V(0.026f, 0.026f, 0.026f), orb, mat: Shell, blend: 0.006f);
            b.Ell(head, path[(int)(0.68f * (path.Length - 1))], V(0.022f, 0.022f, 0.022f), orb, mat: Shell, blend: 0.006f);
            var look = V(0.75f * s, 0.15f, 0.65f);
            b.Eye(head, Out(c, r, default, look), look, 0.016f, Rgb(70, 50, 56));
        });
        return b;
    }

    // ------------------------------------------------------------------ Kleavor

    /// <summary>Kleavor: a mantis of stone, its tan body thin and jointed, its head a faceted helm of dark rock with a pale horn, small tan wings, and for forearms two great axes of dark stone.</summary>
    private static PokeBuilder Kleavor()
    {
        var b = new PokeBuilder("Kleavor", 0.95f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Shell };
        var tan = Rgb(198, 170, 118);
        var stone = Rgb(84, 70, 64);
        var rock = Rgb(122, 106, 94);
        var pale = Rgb(232, 218, 182);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.4f, -0.04f));
            var knee = V(0.1f * s, 0.26f, 0.05f);
            var ankle = V(0.09f * s, 0.08f, -0.04f);
            b.Limb(leg, V(0.06f * s, 0.4f, -0.04f), knee, 0.035f, 0.025f, tan);
            b.Limb(leg, knee, ankle, 0.025f, 0.02f, tan);
            b.Box(leg, V(0.09f * s, 0.04f, 0.0f), V(0.035f, 0.04f, 0.05f), 0.012f, stone, V(-15f, 0, 0), blend: 0.008f);
            b.Spike(leg, V(0.09f * s, 0.02f, 0.03f), V(0.09f * s, 0.01f, 0.1f), 0.02f, stone, 0.6f);
        });
        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc, V(0.08f, 0.1f, 0.07f), tan);
        b.Ell(Body, bc + V(0, -0.1f, -0.07f), V(0.07f, 0.08f, 0.09f), tan, V(30f, 0, 0));
        b.Box(Body, bc + V(0, 0.02f, 0.06f), V(0.06f, 0.06f, 0.02f), 0.015f, rock, V(-10f, 0, 0), blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.04f * s, 0.06f, -0.06f));
            Frond(b, wing, bc + V(0.03f * s, 0.06f, -0.06f), bc + V(0.12f * s, -0.04f, -0.16f), 0.05f, pale, V(s, 0, -0.5f), 0.15f);
            // The arms, each ending in a great axe of stone
            int arm = b.Arm(s, bc + V(0.08f * s, 0.07f, 0.02f));
            var elbow = bc + V(0.15f * s, -0.02f, 0.06f);
            var hand = bc + V(0.2f * s, 0.06f, 0.12f);
            b.Limb(arm, bc + V(0.07f * s, 0.07f, 0.02f), elbow, 0.03f, 0.025f, tan);
            b.Limb(arm, elbow, hand, 0.025f, 0.03f, tan);
            b.Box(arm, hand, V(0.035f, 0.035f, 0.035f), 0.012f, rock, blend: 0.008f);
            Blade(b, arm, hand + V(0.01f * s, -0.05f, 0.0f), hand + V(0.07f * s, 0.24f, 0.05f), 0.08f, stone, V(s, 0, 0.6f), 0.22f, Shell);
            b.PaintEll(arm, hand + V(0.05f * s, 0.11f, 0.06f), V(0.025f, 0.09f, 0.03f), rock, V(0, 0, -15f * s));
        });
        // The helm of rock, faceted, with a pale horn
        int head = b.Head(bc + V(0, 0.11f, 0.02f));
        var hc = bc + V(0, 0.2f, 0.04f);
        b.Limb(head, bc + V(0, 0.08f, 0.01f), hc + V(0, -0.04f, -0.01f), 0.03f, 0.03f, tan);
        b.Box(head, hc, V(0.065f, 0.06f, 0.07f), 0.02f, stone, V(0, 45f, 0), blend: 0.01f);
        b.Box(head, hc + V(0, 0.045f, 0.0f), V(0.05f, 0.03f, 0.05f), 0.015f, rock, V(0, 45f, 0), blend: 0.01f);
        b.Spike(head, hc + V(0, 0.05f, 0.04f), hc + V(0, 0.15f, 0.08f), 0.02f, pale, mat: Shell);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, hc + V(0.05f * s, 0.03f, -0.02f), hc + V(0.11f * s, 0.08f, -0.06f), 0.025f, stone, 0.6f);
            b.Eye(head, hc + V(0.035f * s, 0.0f, 0.063f), V(0.6f * s, 0, 1f), 0.012f, Rgb(250, 232, 140), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Ursaluna

    /// <summary>
    /// Ursaluna and its Bloodmoon. Ursaluna is a great brown bear on all fours, a heavy cloak of grey peat over its
    /// back and head with brown flaps hanging round its edge, a pale ring of a moon on its brow, a tan muzzle and an
    /// open mouth, and great white claws. The Bloodmoon is darker and shaggier, its cloak greyer, and a red moon on
    /// its brow.
    /// </summary>
    private static PokeBuilder UrsalunaBuild(bool bloodmoon)
    {
        var b = new PokeBuilder(bloodmoon ? "Ursaluna-Bloodmoon" : "Ursaluna", 1f, BodyPlan.Quadruped, V(0, 0.38f, -0.02f)) { Coat = Fur };
        var brown = bloodmoon ? Rgb(84, 62, 54) : Rgb(112, 80, 58);
        var peat = bloodmoon ? Rgb(150, 150, 152) : Rgb(146, 140, 132);
        var light = Rgb(186, 180, 172);
        var tan = Rgb(214, 182, 120);
        var moon = bloodmoon ? Rgb(222, 70, 92) : Rgb(234, 194, 96);
        foreach (var (z, front) in new[] { (0.18f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.13f * s, 0.3f, z), front);
                b.Limb(leg, V(0.13f * s, 0.32f, z), V(0.14f * s, 0.08f, z + 0.02f), 0.085f, 0.07f, brown);
                b.Ell(leg, V(0.14f * s, 0.04f, z + 0.05f), V(0.075f, 0.04f, 0.09f), brown);
                for (int i = -1; i <= 1; i++)
                    b.Spike(leg, V(0.14f * s + 0.03f * i, 0.03f, z + 0.12f), V(0.14f * s + 0.035f * i, 0.012f, z + 0.17f), 0.014f, White, mat: Shell, blend: 0.004f);
            });
        var bc = V(0, 0.38f, -0.02f);
        b.Ell(Body, bc, V(0.2f, 0.17f, 0.3f), brown);
        // The cloak of peat over its back, brown flaps hanging round its edge
        var pc = bc + V(0, 0.07f, -0.02f);
        var pr = V(0.22f, 0.14f, 0.3f);
        b.Ell(Body, pc, pr, peat, blend: 0.02f);
        foreach (var dir in new[] { V(0.4f, 0.8f, 0.3f), V(-0.5f, 0.8f, -0.2f), V(0.1f, 0.9f, -0.5f), V(-0.2f, 0.9f, 0.5f) })
            b.PaintEll(Body, Out(pc, pr, default, dir), V(0.05f, 0.03f, 0.06f), light, soft: 0.01f);
        for (int i = 0; i < 18; i++)
        {
            float a = i * MathF.Tau / 18f;
            var root = pc + V(MathF.Sin(a) * pr.X * 0.95f, -0.05f, MathF.Cos(a) * pr.Z * 0.95f);
            b.Spike(Body, root, root + V(MathF.Sin(a) * 0.04f, -0.09f, MathF.Cos(a) * 0.04f), 0.035f, brown, 0.45f);
        }
        int tail = b.Tail(bc + V(0, 0.04f, -0.3f));
        b.Ell(tail, bc + V(0, 0.04f, -0.32f), V(0.04f, 0.04f, 0.035f), brown);
        // The head held low and forward, the moon on its brow
        int head = b.Head(bc + V(0, 0.02f, 0.27f));
        var c = bc + V(0, 0.02f, 0.36f);
        var r = V(0.12f, 0.1f, 0.11f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, 0.05f, -0.03f), V(0.13f, 0.07f, 0.1f), peat, blend: 0.02f);
        var mc = c + V(0, -0.035f, 0.08f);
        b.Ell(head, mc, V(0.06f, 0.045f, 0.055f), tan, blend: 0.02f);
        b.Ell(head, mc + V(0, 0.02f, 0.05f), V(0.022f, 0.016f, 0.012f), Black, blend: 0.004f);
        Gape(b, head, mc + V(0, -0.03f, 0.04f), V(0.04f, 0.018f, 0.025f), Rgb(150, 60, 70), 0.014f);
        var mark = On(c, r, 0, c.Y + 0.035f);
        b.Mark(head, mark, Outward(c, r, mark), 0.03f, 0.03f, moon, bloodmoon ? MarkShape.Disc : MarkShape.Ring);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.09f * s, 0.08f, -0.03f));
            b.Ell(ear, c + V(0.1f * s, 0.09f, -0.03f), V(0.032f, 0.032f, 0.022f), brown);
            var at = On(c, r, 0.06f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, bloodmoon && s < 0 ? Rgb(230, 70, 90) : Rgb(40, 34, 40), glare: true);
        });
        return b;
    }

    private static PokeBuilder Ursaluna() => UrsalunaBuild(false);

    // ------------------------------------------------------------------ Basculegion

    /// <summary>
    /// Basculegion, male and female: a great ghostly fish, pale green with a dark back, a jutting jaw of teeth,
    /// white fins and a crest of white spikes, its tail breaking up into wisps of the souls it carries: red, with red
    /// veins down its sides, for the male; pale blue-white, spotted dark, for the female.
    /// </summary>
    private static PokeBuilder BasculegionBuild(bool female)
    {
        var b = new PokeBuilder(female ? "Basculegion-Female" : "Basculegion", 1f, BodyPlan.Fish, V(0, 0.32f, -0.04f)) { Coat = Scales }.Hover();
        var green = Rgb(116, 154, 116);
        var dark = Rgb(56, 76, 68);
        var fin = Rgb(236, 236, 228);
        var wisp = female ? Rgb(200, 236, 236) : Rgb(232, 82, 120);
        var mc = V(0, 0.32f, -0.06f);
        var mr = V(0.11f, 0.14f, 0.26f);
        var hc = V(0, 0.32f, 0.12f);
        var hr = V(0.12f, 0.15f, 0.15f);
        b.Ell(Body, mc, mr, green);
        b.Ell(Body, hc, hr, green);
        b.PaintEll(Body, V(0, 0.44f, 0.02f), V(0.13f, 0.08f, 0.32f), dark);
        PokeBuilder.Both(s =>
        {
            if (female)
                foreach (var (y, z) in new[] { (0.3f, -0.12f), (0.36f, -0.02f), (0.26f, 0.0f), (0.33f, -0.2f), (0.27f, -0.16f) })
                    b.PaintEll(Body, Out(mc, mr, default, V(s, (y - mc.Y) / mr.Y, (z - mc.Z) / mr.Z)), V(0.016f, 0.016f, 0.016f), dark, soft: 0.005f);
            else
                foreach (var (y, z, tilt) in new[] { (0.32f, -0.08f, 30f), (0.34f, -0.2f, -20f), (0.28f, 0.05f, 50f) })
                    b.PaintEll(Body, Out(mc, mr, default, V(s, (y - mc.Y) / mr.Y, (z - mc.Z) / mr.Z)), V(0.012f, 0.07f, 0.012f), wisp, V(tilt, 0, 0), 0.005f);
            Blade(b, Body, V(0.09f * s, 0.25f, 0.06f), V(0.2f * s, 0.15f, -0.06f), 0.05f, fin, V(0, 1f, 0.3f), 0.2f);
            var at = Out(hc, hr, default, V(0.75f * s, 0.4f, 0.5f));
            b.Eye(Body, at, Outward(hc, hr, at), 0.022f, sclera: true, pupil: female ? Rgb(40, 50, 60) : Rgb(200, 40, 60), glare: true);
        });
        for (int i = 0; i < 4; i++)
        {
            var at = V(0, 0.46f - i * 0.006f, 0.1f - i * 0.06f);
            Blade(b, Body, at - V(0, 0.04f, 0), at + V(0, 0.08f + (i % 2) * 0.04f, -0.03f), 0.03f, fin, V(1f, 0, 0), 0.25f);
        }
        Blade(b, Body, V(0, 0.19f, -0.04f), V(0, 0.09f, -0.14f), 0.05f, fin, V(1f, 0, 0), 0.2f);
        // The great jaw, teeth along it
        int head = b.Head(V(0, 0.26f, 0.12f));
        var jc = V(0, 0.24f, 0.17f);
        b.Ell(head, jc, V(0.1f, 0.06f, 0.12f), green);
        Grin(b, Body, V(0, 0.28f, 0.26f), V(0.07f, 0.025f, 0.03f), Rgb(160, 60, 80));
        for (int i = -3; i <= 3; i++)
            b.Spike(head, V(i * 0.018f, 0.27f, 0.27f - MathF.Abs(i) * 0.012f), V(i * 0.019f, 0.305f, 0.27f - MathF.Abs(i) * 0.012f), 0.007f, White, mat: Shell, blend: 0.003f);
        // The tail breaking up into wisps
        int tail = b.Tail(V(0, 0.32f, -0.3f));
        b.Limb(tail, V(0, 0.32f, -0.28f), V(0, 0.33f, -0.38f), 0.05f, 0.035f, green);
        b.Ell(tail, V(0, 0.34f, -0.42f), V(0.04f, 0.06f, 0.05f), wisp, mat: Glow);
        foreach (var (y, z, k) in new[] { (0.12f, -0.08f, 1f), (0.0f, -0.12f, 1.2f), (-0.1f, -0.07f, 0.9f), (0.06f, -0.15f, 0.8f) })
        {
            var start = V(0, 0.34f, -0.42f);
            var path = Smooth(3, start, start + V(0.02f, y * 0.5f, z * 0.6f), start + V(-0.02f, y, z), start + V(0.01f, y * 1.3f + 0.03f, z * 1.5f));
            b.Tube(tail, path, 0.03f * k, 0.01f, wisp, Glow, 0.006f);
        }
        return Lift(b);
    }

    private static PokeBuilder Basculegion() => BasculegionBuild(false);

    // ------------------------------------------------------------------ Sneasler

    /// <summary>Sneasler: a tall lean weasel of the snowy cliffs, grey-blue, with great curved purple claws, purple feet tipped with white claws, a gold gem on its brow and its chest, red eyes in purple and a long crest that sweeps back from its head, purple turning magenta.</summary>
    private static PokeBuilder Sneasler()
    {
        var b = new PokeBuilder("Sneasler", 0.95f, BodyPlan.Biped, V(0, 0.52f, 0)) { Coat = Fur };
        var grey = Rgb(178, 190, 228);
        var purple = Rgb(108, 70, 152);
        var magenta = Rgb(214, 66, 122);
        var gold = Rgb(244, 204, 64);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.4f, -0.01f));
            var knee = V(0.1f * s, 0.24f, 0.06f);
            var ankle = V(0.1f * s, 0.08f, -0.03f);
            b.Limb(leg, V(0.06f * s, 0.4f, -0.01f), knee, 0.045f, 0.03f, grey);
            b.Limb(leg, knee, ankle, 0.03f, 0.025f, grey);
            b.Limb(leg, ankle, V(0.1f * s, 0.025f, 0.05f), 0.026f, 0.022f, purple);
            Claws(b, leg, V(0.1f * s, 0.02f, 0.075f), V(0.018f, 0, 0), V(0, -0.3f, 1f), 0.03f, 0.009f);
        });
        var bc = V(0, 0.52f, 0);
        b.Ell(Body, bc, V(0.085f, 0.15f, 0.07f), grey);
        b.Ell(Body, bc + V(0, 0.05f, 0.065f), V(0.018f, 0.024f, 0.012f), gold, mat: Shell, blend: 0.006f);
        FurTufts(b, Body, bc + V(0, 0.11f, 0.0f), V(0.08f, 0.04f, 0.065f), 10, 0.035f, 0.016f, grey, 1.1f, -0.2f, 0.5f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.08f * s, 0.11f, 0));
            var elbow = bc + V(0.16f * s, -0.02f, 0.04f);
            var hand = bc + V(0.19f * s, -0.15f, 0.07f);
            b.Limb(arm, bc + V(0.07f * s, 0.11f, 0), elbow, 0.032f, 0.028f, grey);
            b.Limb(arm, elbow, hand, 0.028f, 0.032f, purple);
            for (int i = -1; i <= 1; i++)
                b.Tube(arm, Smooth(2, hand + V(0.012f * i * s, -0.01f, 0.01f * i), hand + V((0.02f + 0.012f * i) * s, -0.09f, 0.04f + 0.012f * i), hand + V((0.0f + 0.015f * i) * s, -0.16f, 0.09f + 0.012f * i)), 0.013f, 0.004f, i == 0 ? magenta : purple, Shell, 0.004f);
        });
        int head = b.Head(bc + V(0, 0.15f, 0.02f));
        var c = bc + V(0, 0.24f, 0.03f);
        var r = V(0.085f, 0.075f, 0.08f);
        b.Ell(head, c, r, grey);
        b.Ell(head, c + V(0, -0.02f, 0.07f), V(0.035f, 0.03f, 0.03f), grey, blend: 0.02f);
        b.Ell(head, c + V(0, -0.005f, 0.098f), V(0.012f, 0.009f, 0.007f), purple, blend: 0.004f);
        b.Ell(head, c + V(0, 0.05f, 0.065f), V(0.016f, 0.02f, 0.01f), gold, mat: Shell, blend: 0.006f);
        b.Spike(head, c + V(-0.05f, 0.05f, -0.01f), c + V(-0.09f, 0.13f, -0.03f), 0.03f, grey, 0.5f);
        // The long crest sweeping back from the right of its head
        var crest = Smooth(3, c + V(0.05f, 0.05f, -0.01f), c + V(0.1f, 0.17f, -0.06f), c + V(0.12f, 0.2f, -0.2f), c + V(0.1f, 0.1f, -0.3f));
        b.Tube(head, crest, 0.035f, 0.014f, purple, blend: 0f);
        b.PaintEll(head, crest[^1], V(0.06f, 0.08f, 0.08f), magenta, soft: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.008f);
            b.PaintEll(head, at, V(0.026f, 0.018f, 0.02f), purple, soft: 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(220, 50, 70), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Overqwil

    /// <summary>Overqwil: Hisui's Qwilfish grown into a great dark ball bristling with spikes, long purple ones on top tipped magenta and long pale ones hanging below like a beard, a white moustache under great eyes, a round purple mouth and a tail fin marked like a Q.</summary>
    private static PokeBuilder Overqwil()
    {
        var b = new PokeBuilder("Overqwil", 0.9f, BodyPlan.Fish, V(0, 0.32f, 0)) { Coat = Scales }.Hover();
        var back = Rgb(44, 44, 78);
        var belly = Rgb(226, 218, 190);
        var spikes = Rgb(110, 64, 160);
        var tip = Rgb(222, 92, 172);
        var c = V(0, 0.32f, 0);
        var r = V(0.2f, 0.19f, 0.19f);
        b.Ell(Body, c, r, back);
        b.PaintEll(Body, c + V(0, -0.1f, 0.06f), V(0.2f, 0.12f, 0.18f), belly);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 24; i++)
        {
            float u = -0.85f + 1.7f * (i + 0.5f) / 24f, ring = MathF.Sqrt(1f - u * u), a = i * golden;
            var dir = V(MathF.Sin(a) * ring, u, MathF.Cos(a) * ring);
            if (dir.Z > 0.5f && dir.Y > -0.5f) continue;
            if (dir.Z < -0.8f && MathF.Abs(dir.Y) < 0.4f) continue;
            var root = c + dir * r * 0.95f;
            float len = dir.Y > 0.35f ? 0.17f : dir.Y < -0.35f ? 0.15f : 0.08f;
            var color = dir.Y < -0.35f ? belly : spikes;
            b.Spike(Body, root, root + dir * len, 0.026f, color, mat: Shell);
            if (dir.Y > 0.35f) b.PaintEll(Body, root + dir * len * 0.85f, V(0.02f, 0.02f, 0.02f), tip, soft: 0.006f);
        }
        // A white moustache under its eyes and a round purple mouth
        var mouth = Out(c, r, default, V(0, -0.25f, 1f));
        b.Torus(Body, mouth, 0.028f, 0.015f, Rgb(170, 110, 200), V(90f, 0, 0));
        PokeBuilder.Both(s =>
        {
            Frond(b, Body, mouth + V(0.02f * s, 0.035f, -0.005f), mouth + V(0.13f * s, 0.06f, -0.04f), 0.025f, belly, V(0, 0.4f, 1f), 0.3f);
            var look = V(0.5f * s, 0.32f, 0.8f);
            b.Eye(Body, Out(c, r, default, look), look, 0.032f, sclera: true, pupil: Black, glare: true);
        });
        // The tail fin, round and marked with a Q
        int tail = b.Tail(c + V(0, 0, -0.17f));
        b.Limb(tail, c + V(0, 0, -0.15f), c + V(0, 0.01f, -0.26f), 0.04f, 0.032f, back);
        var fin = c + V(0, 0.02f, -0.34f);
        b.Ell(tail, fin, V(0.018f, 0.08f, 0.09f), back);
        PokeBuilder.Both(s => b.Mark(tail, fin + V(0.018f * s, 0, 0), V(s, 0, 0), 0.04f, 0.05f, belly, MarkShape.Ring));
        b.Spike(tail, fin + V(0, -0.05f, -0.04f), fin + V(0, -0.1f, -0.1f), 0.02f, back, 0.5f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Enamorus

    /// <summary>A loop of thin white line in the shape of a heart standing over <paramref name="at"/>, its point resting there, <paramref name="size"/> tall.</summary>
    private static void HeartLoop(PokeBuilder b, int bone, Vector3 at, float size, Color color)
    {
        const int Steps = 28;
        var points = new Vector3[Steps + 1];
        for (int i = 0; i <= Steps; i++)
        {
            float t = MathF.PI + i * MathF.Tau / Steps;
            float x = 16f * MathF.Pow(MathF.Sin(t), 3f), y = 13f * MathF.Cos(t) - 5f * MathF.Cos(2f * t) - 2f * MathF.Cos(3f * t) - MathF.Cos(4f * t);
            points[i] = at + V(x, y + 17f, 0) * (size / 29f);
        }
        b.Tube(bone, points, size * 0.035f, size * 0.035f, color, blend: 0.004f);
    }

    /// <summary>A heart painted on the surface at <paramref name="at"/>, facing <paramref name="normal"/>: two lobes leaning together to a point below.</summary>
    private static void HeartPaint(PokeBuilder b, int bone, Vector3 at, Vector3 normal, float size, Color color)
    {
        var n = Vector3.Normalize(normal);
        var up = Vector3.UnitY - n * Vector3.Dot(Vector3.UnitY, n);
        up = up.LengthSquared() < 1e-4f ? Vector3.UnitZ : Vector3.Normalize(up);
        var side = Vector3.Normalize(Vector3.Cross(up, n));
        foreach (float s in new[] { -1f, 1f })
        {
            var dir = Vector3.Normalize(up * MathF.Cos(0.6f) + side * s * MathF.Sin(0.6f));
            b.PaintEll(bone, at + side * s * size * 0.28f + up * size * 0.1f, V(size * 0.3f, size * 0.55f, size * 0.3f), color, Euler(dir), size * 0.12f);
        }
    }

    private static readonly Color EnamorusPink = Rgb(236, 124, 164);
    private static readonly Color EnamorusHeart = Rgb(154, 30, 80);

    /// <summary>Enamorus in its Incarnate Forme: a slender pink genie riding a cloud, its long tail wound round its body, dark hearts on its tail and its sides, yellow eyes under a white heart-shaped loop that stands over its head, and pink arms tipped with claws.</summary>
    private static PokeBuilder Enamorus()
    {
        var b = new PokeBuilder("Enamorus", 1f, BodyPlan.Floating, V(0, 0.44f, 0)) { Coat = Fur }.Hover();
        RideCloud(b, Root, V(0, 0.2f, 0), 0.14f);
        var bc = V(0, 0.44f, 0);
        var br = V(0.08f, 0.12f, 0.07f);
        b.Ell(Body, bc, br, EnamorusPink);
        b.Limb(Body, bc + V(0, -0.08f, 0), V(0, 0.27f, 0), 0.06f, 0.05f, EnamorusPink);
        PokeBuilder.Both(s => HeartPaint(b, Body, Out(bc, br, default, V(s, 0.3f, 0.4f)), V(s, 0.3f, 0.4f), 0.04f, EnamorusHeart));
        // Its tail wound twice round its body, rising behind it
        int tail = b.Tail(bc + V(0, -0.1f, -0.04f));
        var coil = new Vector3[25];
        for (int i = 0; i < coil.Length; i++)
        {
            float t = i / (float)(coil.Length - 1), a = t * MathF.Tau;
            coil[i] = bc + V(MathF.Sin(a) * 0.085f, -0.12f + 0.06f * t, -MathF.Cos(a) * 0.075f);
        }
        b.Tube(tail, coil, 0.035f, 0.03f, EnamorusPink, blend: 0f);
        var rise = Smooth(3, coil[^1], coil[^1] + V(0.06f, 0.0f, -0.06f), coil[^1] + V(0.14f, 0.1f, -0.08f), coil[^1] + V(0.16f, 0.24f, -0.04f));
        b.Tube(tail, rise, 0.03f, 0.035f, EnamorusPink, blend: 0f);
        b.Ell(tail, rise[^1], V(0.04f, 0.035f, 0.035f), EnamorusPink);
        for (int i = 4; i < coil.Length; i += 5)
        {
            var n = coil[i] - V(bc.X, coil[i].Y, bc.Z);
            HeartPaint(b, tail, coil[i] + Vector3.Normalize(n) * 0.035f, n, 0.035f, EnamorusHeart);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.07f * s, 0.07f, 0));
            var hand = bc + V(0.14f * s, -0.04f, 0.06f);
            b.Limb(arm, bc + V(0.06f * s, 0.07f, 0), bc + V(0.13f * s, 0.02f, 0.02f), 0.025f, 0.022f, EnamorusPink);
            b.Limb(arm, bc + V(0.13f * s, 0.02f, 0.02f), hand, 0.022f, 0.022f, EnamorusPink);
            Claws(b, arm, hand + V(0, -0.015f, 0.01f), V(0.008f, 0, 0), V(0, -0.8f, 0.4f), 0.022f, 0.007f);
        });
        int head = b.Head(bc + V(0, 0.13f, 0.01f));
        var c = bc + V(0, 0.21f, 0.02f);
        var r = V(0.065f, 0.06f, 0.07f);
        b.Limb(head, bc + V(0, 0.1f, 0), c + V(0, -0.03f, -0.01f), 0.04f, 0.035f, EnamorusPink);
        b.Ell(head, c, r, EnamorusPink);
        b.Ell(head, c + V(0, -0.02f, 0.06f), V(0.035f, 0.026f, 0.03f), EnamorusPink, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.012f * s, -0.035f, 0.08f), c + V(0.014f * s, -0.055f, 0.082f), 0.006f, White, mat: Shell, blend: 0.003f);
            b.Spike(head, c + V(0.05f * s, 0.02f, -0.03f), c + V(0.11f * s, 0.0f, -0.08f), 0.02f, EnamorusPink, 0.5f);
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(250, 214, 70), glare: true);
        });
        HeartLoop(b, head, c + V(0, 0.055f, -0.01f), 0.17f, Rgb(244, 244, 250));
        return Lift(b);
    }

    /// <summary>Enamorus in its Therian Forme: a pink beast like a long-necked tortoise standing on a bed of cloud, dark hearts across its domed back and its curled tail, a long head with a pointed snout and yellow eyes, and the white heart loop standing over its head.</summary>
    private static PokeBuilder EnamorusTherianBuild()
    {
        var b = new PokeBuilder("Enamorus-Therian", 1f, BodyPlan.Quadruped, V(0, 0.32f, -0.02f)) { Coat = Fur };
        var white = Rgb(240, 242, 248);
        // The bed of cloud it stands on
        foreach (var (x, z, k) in new[] { (0f, 0f, 1f), (-0.14f, 0.08f, 0.7f), (0.14f, 0.08f, 0.7f), (-0.12f, -0.12f, 0.75f), (0.12f, -0.12f, 0.75f), (0f, 0.16f, 0.6f), (0f, -0.2f, 0.6f) })
            b.Ell(Root, V(x, 0.06f, z), V(0.16f * k, 0.06f * k, 0.15f * k), white, blend: 0.03f);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.11f * s, 0.26f, z), front);
                b.Limb(leg, V(0.11f * s, 0.27f, z), V(0.13f * s, 0.12f, z + 0.02f), 0.045f, 0.04f, EnamorusPink);
                b.Ell(leg, V(0.13f * s, 0.11f, z + 0.04f), V(0.045f, 0.03f, 0.055f), EnamorusPink);
                Claws(b, leg, V(0.13f * s, 0.1f, z + 0.09f), V(0.016f, 0, 0), V(0, -0.4f, 1f), 0.025f, 0.008f);
            });
        var bc = V(0, 0.32f, -0.02f);
        var br = V(0.17f, 0.12f, 0.2f);
        b.Ell(Body, bc, br, EnamorusPink);
        b.PaintTorus(Body, bc + V(0, -0.03f, 0), 0.17f, 0.012f, Rgb(214, 96, 140), sz: 1.18f);
        foreach (var dir in new[] { V(0, 1f, 0.1f), V(0.6f, 0.7f, 0.3f), V(-0.6f, 0.7f, 0.3f), V(0.55f, 0.7f, -0.45f), V(-0.55f, 0.7f, -0.45f), V(0, 0.8f, -0.6f) })
        {
            var at = Out(bc, br, default, dir);
            HeartPaint(b, Body, at, Outward(bc, br, at), 0.06f, EnamorusHeart);
        }
        int tail = b.Tail(bc + V(0, 0.0f, -0.19f));
        var tp = Smooth(3, bc + V(0, 0.0f, -0.19f), bc + V(0, -0.02f, -0.27f), bc + V(0.04f, 0.04f, -0.32f), bc + V(0.05f, 0.1f, -0.28f));
        b.Tube(tail, tp, 0.035f, 0.025f, EnamorusPink, blend: 0f);
        // The long neck and head, the heart loop standing over it
        int head = b.Head(bc + V(0, 0.05f, 0.18f));
        var c = bc + V(0, 0.16f, 0.3f);
        var r = V(0.055f, 0.05f, 0.075f);
        b.Tube(head, Smooth(3, bc + V(0, 0.03f, 0.17f), bc + V(0, 0.1f, 0.24f), c + V(0, -0.02f, -0.05f)), 0.05f, 0.04f, EnamorusPink, blend: 0f);
        b.Ell(head, c, r, EnamorusPink);
        b.Spike(head, c + V(0, -0.01f, 0.05f), c + V(0, -0.025f, 0.13f), 0.035f, EnamorusPink, 0.7f);
        b.PaintEll(head, c + V(0, -0.03f, 0.1f), V(0.03f, 0.008f, 0.04f), EnamorusHeart, soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.04f * s, 0.02f, -0.04f), c + V(0.09f * s, 0.0f, -0.1f), 0.018f, EnamorusPink, 0.5f);
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(250, 214, 70), glare: true);
        });
        HeartLoop(b, head, c + V(0, 0.045f, -0.02f), 0.16f, Rgb(244, 244, 250));
        return b;
    }
}
