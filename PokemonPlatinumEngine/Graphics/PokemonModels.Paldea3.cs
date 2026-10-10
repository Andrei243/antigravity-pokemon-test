using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Paldea's third batch in National Pokédex
// order: Capsakid (951) to Dondozo (977). Their forms are in PokemonModels.Regional.cs and PokemonModels.Megas.cs with
// the other forms. Helpers shared with the earlier batches are in the files of those batches, PokemonModels.Sinnoh1.cs
// to PokemonModels.Paldea2.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Capsakid and Scovillain

    private static readonly Color PepperGreen = Rgb(104, 178, 106);
    private static readonly Color PepperDark = Rgb(50, 110, 66);
    private static readonly Color PepperPale = Rgb(214, 234, 204);
    private static readonly Color PepperRed = Rgb(222, 70, 46);

    /// <summary>Capsakid: a little pepper on two stubby legs, its great green head crowned behind with a pale calyx of spikes, a pointed orange beak, sly eyes and leaves for arms.</summary>
    private static PokeBuilder Capsakid()
    {
        var b = new PokeBuilder("Capsakid", 0.45f, BodyPlan.Biped, V(0, 0.13f, 0)) { Coat = Leaf };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.09f, 0));
            b.Limb(leg, V(0.035f * s, 0.1f, 0), V(0.045f * s, 0.025f, 0.01f), 0.022f, 0.02f, PepperGreen);
            b.Ell(leg, V(0.045f * s, 0.018f, 0.02f), V(0.022f, 0.018f, 0.03f), PepperGreen);
        });
        var bc = V(0, 0.13f, 0);
        b.Ell(Body, bc, V(0.05f, 0.06f, 0.045f), PepperGreen);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.045f * s, 0.02f, 0));
            Blade(b, arm, bc + V(0.04f * s, 0.02f, 0), bc + V(0.11f * s, -0.01f, 0.02f), 0.022f, PepperGreen, V(0, 1f, 0.4f), 0.3f);
        });
        int tail = b.Tail(bc + V(0, -0.03f, -0.04f));
        b.Spike(tail, bc + V(0, -0.03f, -0.03f), bc + V(0, -0.02f, -0.1f), 0.02f, PepperGreen, 0.5f);
        int head = b.Head(bc + V(0, 0.06f, 0.01f));
        var c = bc + V(0, 0.14f, 0.02f);
        var r = V(0.1f, 0.09f, 0.09f);
        b.Ell(head, c, r, PepperGreen);
        // The pale calyx behind its crown, spiking out
        b.Ell(head, c + V(0, 0.04f, -0.05f), V(0.08f, 0.06f, 0.06f), PepperPale, blend: 0.012f);
        foreach (var d in new[] { V(0, 1f, -0.4f), V(0.7f, 0.6f, -0.5f), V(-0.7f, 0.6f, -0.5f), V(0.9f, 0.1f, -0.6f), V(-0.9f, 0.1f, -0.6f) })
        {
            var n = Vector3.Normalize(d);
            b.Spike(head, c + n * 0.07f + V(0, 0.02f, -0.03f), c + n * 0.15f + V(0, 0.02f, -0.03f), 0.03f, PepperPale, 0.4f);
        }
        b.Spike(head, c + V(0, -0.02f, 0.07f), c + V(0, -0.05f, 0.14f), 0.03f, Rgb(242, 150, 60), 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.04f * s, c.Y + 0.015f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(40, 60, 40), glare: true);
        });
        return b;
    }

    /// <summary>A pepper of a head at <paramref name="c"/>, <paramref name="k"/> times its size: lumpy at the crown like a pepper's shoulders, a wide mouth gaping with fangs, angry eyes, and in the Mega white horns standing up behind.</summary>
    private static void PepperHead(PokeBuilder b, int head, Vector3 c, float k, Color color, bool horns)
    {
        var r = V(0.085f, 0.08f, 0.08f) * k;
        b.Ell(head, c, r, color);
        foreach (float x in new[] { -0.04f, 0f, 0.04f })
            b.Ell(head, c + V(x, 0.06f, -0.01f) * k, V(0.035f, 0.03f, 0.035f) * k, color, blend: 0.015f * k);
        Gape(b, head, c + V(0, -0.028f, 0.07f) * k, V(0.05f, 0.024f, 0.03f) * k, Rgb(90, 30, 30), 0.018f * k);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.034f * s * k, c.Y + 0.018f * k);
            b.Eye(head, at, Outward(c, r, at), 0.013f * k, Rgb(40, 30, 30), glare: true);
        });
        if (!horns) return;
        foreach (var (x, lean) in new[] { (-0.035f, -0.4f), (0f, 0f), (0.035f, 0.4f) })
            b.Spike(head, c + V(x, 0.07f, -0.02f) * k, c + V(x + lean * 0.05f, 0.17f, -0.06f) * k, 0.02f * k, Rgb(244, 240, 228), 0.6f, Shell);
    }

    /// <summary>
    /// Scovillain: a pepper plant of a Pokémon, its dark green body on thick legs with white toes, a collar of leaves
    /// where two necks rise, and on them two pepper heads, a red one of fire and a green one of grass, each gaping with
    /// fangs. The Mega grows a tail spiked white, a ragged black ruff, white horns on both heads, a long red tongue of
    /// flame from the red head and a yellow one from the green.
    /// </summary>
    private static PokeBuilder ScovillainBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Scovillain-Mega" : "Scovillain", 0.9f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Leaf };
        var white = Rgb(242, 240, 232);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.26f, 0));
            b.Limb(leg, V(0.07f * s, 0.27f, 0), V(0.09f * s, 0.06f, 0.02f), 0.06f, 0.05f, PepperDark);
            b.Ell(leg, V(0.09f * s, 0.035f, 0.035f), V(0.055f, 0.035f, 0.065f), PepperDark);
            b.PaintEll(leg, V(0.09f * s, 0.03f, 0.09f), V(0.045f, 0.03f, 0.025f), white, soft: 0.006f);
        });
        var bc = V(0, 0.32f, 0);
        b.Ell(Body, bc, V(0.12f, 0.12f, 0.1f), PepperDark);
        // The collar of leaves falling from the top of its body
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, bc + V(0, 0.11f, 0) + dir * 0.03f, bc + dir * 0.12f + V(0, 0.03f, 0), 0.04f, PepperGreen, dir + V(0, 0.8f, 0), 0.22f, Leaf, 0.01f);
        }
        if (mega)
        {
            int tail = b.Tail(bc + V(0, -0.06f, -0.08f));
            var tp = Smooth(3, bc + V(0, -0.06f, -0.08f), bc + V(0, -0.15f, -0.22f), bc + V(0.02f, -0.26f, -0.38f));
            b.Tube(tail, tp, 0.055f, 0.02f, PepperDark, blend: 0f);
            for (int i = 1; i < tp.Length - 1; i += 2)
                b.Spike(tail, tp[i] + V(0, 0.03f, 0), tp[i] + V(0, 0.08f, -0.03f), 0.018f, white, 0.6f, Shell);
            // The ragged black ruff about the root of its necks
            for (int i = 0; i < 8; i++)
            {
                float a = i * MathF.Tau / 8f + 0.2f;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                Blade(b, Body, bc + V(0, 0.1f, 0) + dir * 0.05f, bc + dir * 0.2f + V(0, 0.04f * (i % 2), 0), 0.04f, Rgb(36, 34, 42), V(0, 1f, 0), 0.2f);
            }
        }
        foreach (var (s, color) in new[] { (-1f, PepperRed), (1f, PepperGreen) })
        {
            float k = mega && s < 0 ? 1.15f : 1f;
            int head = b.Part(s < 0 ? "headL" : "headR", Body, bc + V(0.03f * s, 0.14f, 0), PokeRole.Head, s < 0 ? 0.3f : 2.1f, s);
            var c = bc + V(0.14f * s, 0.33f, 0.03f);
            b.Tube(head, Smooth(3, bc + V(0.02f * s, 0.1f, 0), bc + V(0.09f * s, 0.2f, 0.0f), c + V(-0.02f * s, -0.07f, -0.02f)), 0.024f, 0.02f, PepperDark, blend: 0.006f);
            PepperHead(b, head, c, k, color, mega);
            if (!mega) continue;
            var mouth = c + V(0, -0.045f, 0.075f) * k;
            Lightning(b, head, s < 0
                    ? new[] { mouth, mouth + V(-0.02f, -0.08f, 0.03f), mouth + V(0.02f, -0.15f, 0.04f), mouth + V(-0.01f, -0.26f, 0.05f) }
                    : new[] { mouth, mouth + V(0.02f, -0.05f, 0.02f), mouth + V(-0.01f, -0.1f, 0.03f), mouth + V(0.015f, -0.16f, 0.03f) },
                new[] { 0.024f, 0.022f, 0.02f }, 0.009f, s < 0 ? Rgb(236, 64, 40) : Rgb(244, 200, 60), V(0, 0, 1f), mat: Glow);
        }
        return b;
    }

    private static PokeBuilder Scovillain() => ScovillainBuild(false);

    // ------------------------------------------------------------------ Rellor and Rabsca

    /// <summary>Rellor: a little dung beetle with a ball of mud streaked orange as big as itself, a tan shell banded like a bowl on its back, a pink face and thin grey legs, two of them braced against the ball.</summary>
    private static PokeBuilder Rellor()
    {
        var b = new PokeBuilder("Rellor", 0.42f, BodyPlan.Quadruped, V(0.08f, 0.08f, -0.03f)) { Coat = Shell };
        var mud = Rgb(134, 102, 66);
        var streak = Rgb(232, 152, 52);
        var shell = Rgb(222, 202, 152);
        var grey = Rgb(112, 102, 92);
        var face = Rgb(206, 96, 126);
        var ball = V(-0.06f, 0.12f, 0.03f);
        var br = V(0.12f, 0.12f, 0.12f);
        b.Ell(Body, ball, br, mud);
        foreach (var (d, turn) in new[] { (V(-0.3f, 0.6f, 0.8f), 30f), (V(0.4f, 0.2f, 0.9f), -50f), (V(-0.8f, 0.1f, 0.5f), 80f), (V(0.1f, 0.9f, 0.3f), 10f), (V(-0.5f, -0.3f, 0.8f), -20f), (V(0.6f, 0.6f, -0.4f), 60f) })
            b.PaintEll(Body, Out(ball, br, default, d), V(0.012f, 0.035f, 0.012f), streak, V(turn, 0, turn * 0.7f), 0.006f);
        var bc = V(0.09f, 0.085f, -0.03f);
        b.Ell(Body, bc, V(0.06f, 0.055f, 0.06f), grey, blend: 0.012f);
        var sc = bc + V(0.015f, 0.02f, -0.015f);
        b.Ell(Body, sc, V(0.058f, 0.05f, 0.055f), shell, blend: 0.01f);
        foreach (float y in new[] { -0.01f, 0.02f })
            b.PaintTorus(Body, sc + V(0, y, 0), 0.055f - MathF.Abs(y) * 0.6f, 0.006f, Rgb(176, 150, 100), V(0, 0, -20f));
        int head = b.Head(bc + V(-0.01f, -0.01f, 0.04f));
        var c = bc + V(-0.005f, -0.01f, 0.05f);
        var r = V(0.035f, 0.03f, 0.028f);
        b.Ell(head, c, r, face);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.013f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, Rgb(40, 30, 36));
        });
        // Legs: two braced against the ball, two standing
        foreach (var (s, front, knee, foot) in new[]
        {
            (-1f, true, V(0.03f, 0.17f, 0.05f), V(0.0f, 0.19f, 0.05f)),
            (-1f, false, V(0.04f, 0.15f, -0.07f), V(0.01f, 0.18f, -0.04f)),
            (1f, true, V(0.17f, 0.08f, 0.03f), V(0.19f, 0.005f, 0.04f)),
            (1f, false, V(0.16f, 0.08f, -0.08f), V(0.18f, 0.005f, -0.1f)),
        })
        {
            var hip = bc + V(0.03f * s, -0.02f, front ? 0.02f : -0.03f);
            int leg = b.Leg(s, hip, front);
            b.Limb(leg, hip, knee, 0.01f, 0.009f, grey);
            b.Limb(leg, knee, foot, 0.009f, 0.007f, grey);
        }
        return b;
    }

    /// <summary>Rabsca: a teal beetle that lies on its back and holds up a lumpy purple ball with a glossy magenta orb swelling from it, its white legs fanned out like feathers below, a red bow-like mark on its face.</summary>
    private static PokeBuilder Rabsca()
    {
        var b = new PokeBuilder("Rabsca", 0.5f, BodyPlan.Floating, V(0, 0.32f, 0)) { Coat = Shell }.Hover();
        var purple = Rgb(112, 62, 142);
        var orb = Rgb(236, 72, 112);
        var teal = Rgb(72, 170, 182);
        var white = Rgb(240, 240, 244);
        var bc = V(0, 0.32f, 0);
        var br = V(0.12f, 0.12f, 0.11f);
        b.Ell(Body, bc, br, purple);
        foreach (var d in new[] { V(1f, 0.3f, 0), V(-1f, 0.2f, 0.1f), V(0.3f, 1f, 0), V(-0.4f, 0.8f, -0.4f), V(0.6f, -0.5f, 0.2f), V(-0.5f, -0.6f, 0.1f), V(0, 0.2f, -1f), V(0.6f, 0.4f, -0.7f) })
            b.Ell(Body, Out(bc, br, default, d), V(0.05f, 0.05f, 0.05f), purple, blend: 0.02f);
        b.Ell(Body, bc + V(0.0f, 0.01f, 0.055f), V(0.085f, 0.085f, 0.075f), orb, blend: 0.01f);
        b.PaintEll(Body, bc + V(-0.03f, 0.05f, 0.115f), V(0.02f, 0.015f, 0.015f), Rgb(255, 210, 226), soft: 0.006f);
        // The beetle beneath, on its back, holding the ball up
        int head = b.Head(bc + V(0, -0.15f, 0));
        var c = bc + V(0, -0.2f, 0.02f);
        var r = V(0.042f, 0.036f, 0.036f);
        b.Ell(head, c, r, teal);
        PokeBuilder.Both(s => b.Tube(head, new[] { c + V(0.02f * s, 0.02f, 0), c + V(0.05f * s, 0.07f, 0.01f), bc + V(0.05f * s, -0.09f, 0.02f) }, 0.011f, 0.01f, teal, blend: 0.006f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.015f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, Rgb(30, 40, 50));
        });
        var bow = On(c, r, c.X, c.Y - 0.013f);
        b.Mark(head, bow, Outward(c, r, bow), 0.016f, 0.008f, Rgb(220, 40, 52), MarkShape.Diamond);
        foreach (var (s, z) in new[] { (-1f, 0.03f), (-1f, -0.03f), (1f, 0.03f), (1f, -0.03f) })
        {
            int leg = b.Leg(s, c + V(0.03f * s, -0.01f, z), z > 0);
            Frond(b, leg, c + V(0.025f * s, -0.01f, z * 0.5f), c + V(0.14f * s, -0.07f, z * 2f), 0.026f, white, V(0, 1f, 0), 0.2f, Fur);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Flittle and Espathra

    /// <summary>Flittle: a little round yellow seed of a bird on tiny purple feet, a frilled lilac skirt orange beneath, two white petals standing up on its head, a purple mark on its brow, teal eyes and a small purple beak.</summary>
    private static PokeBuilder Flittle()
    {
        var b = new PokeBuilder("Flittle", 0.4f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 232, 112);
        var lilac = Rgb(196, 160, 214);
        var purple = Rgb(150, 112, 190);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.04f, 0));
            b.Ell(leg, V(0.032f * s, 0.02f, 0.01f), V(0.018f, 0.02f, 0.024f), purple);
        });
        var bc = V(0, 0.115f, 0);
        var br = V(0.085f, 0.082f, 0.08f);
        b.Ell(Body, bc, br, yellow);
        // The frilled skirt round its foot
        b.Ell(Body, bc + V(0, -0.06f, 0), V(0.095f, 0.03f, 0.09f), lilac, blend: 0.012f);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            b.Ell(Body, bc + V(MathF.Sin(a) * 0.09f, -0.065f, MathF.Cos(a) * 0.085f), V(0.035f, 0.022f, 0.035f), lilac, blend: 0.012f);
        }
        b.PaintEll(Body, bc + V(0, -0.1f, 0), V(0.12f, 0.025f, 0.12f), Rgb(240, 158, 82), soft: 0.01f);
        int head = b.Head(bc + V(0, 0.06f, 0));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, bc + V(0.04f * s, 0.07f, 0));
            b.Ell(ear, bc + V(0.055f * s, 0.12f, -0.005f), V(0.026f, 0.045f, 0.014f), Rgb(246, 244, 240), V(0, 0, -20f * s));
        });
        foreach (float x in new[] { -0.015f, 0.015f })
            b.Spike(head, bc + V(x, 0.07f, 0), bc + V(x * 1.6f, 0.1f, 0.01f), 0.012f, yellow, 0.5f);
        var mark = On(bc, br, bc.X, bc.Y + 0.045f);
        b.Mark(Body, mark, Outward(bc, br, mark), 0.016f, 0.012f, purple, MarkShape.Triangle, 180f);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, bc.X + 0.033f * s, bc.Y + 0.01f);
            b.Eye(Body, at, Outward(bc, br, at), 0.016f, Rgb(110, 200, 196));
        });
        b.Ell(Body, On(bc, br, bc.X, bc.Y - 0.018f), V(0.012f, 0.008f, 0.008f), purple, blend: 0.004f);
        return b;
    }

    /// <summary>Espathra: a tall psychic ostrich, its round orange body fringed with frills of yellow and white feathers, white triangles on its front, a long neck banded purple and pale, a small purple head under a white hood of feathers like an eggshell, and long thin legs banded purple.</summary>
    private static PokeBuilder Espathra()
    {
        var b = new PokeBuilder("Espathra", 1.0f, BodyPlan.Biped, V(0, 0.5f, -0.02f)) { Coat = Fur };
        var orange = Rgb(246, 182, 82);
        var frill = Rgb(250, 232, 124);
        var white = Rgb(246, 244, 236);
        var purple = Rgb(150, 112, 190);
        var pale = Rgb(238, 228, 244);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.42f, 0));
            var knee = V(0.07f * s, 0.25f, -0.03f);
            var ankle = V(0.07f * s, 0.05f, 0.02f);
            b.Limb(leg, V(0.05f * s, 0.43f, 0), knee, 0.018f, 0.014f, white);
            b.Limb(leg, knee, ankle, 0.014f, 0.012f, white);
            foreach (float t in new[] { 0.3f, 0.7f })
                b.PaintEll(leg, Vector3.Lerp(knee, ankle, t), V(0.02f, 0.012f, 0.02f), purple, soft: 0.004f);
            Digits(b, leg, ankle + V(0, -0.02f, 0), V(0, -0.3f, 1f), V(0.4f * s, 0, 0), 0.04f, 0.01f, purple);
        });
        var bc = V(0, 0.54f, -0.02f);
        var br = V(0.17f, 0.14f, 0.17f);
        b.Ell(Body, bc, br, orange);
        // Frills of feathers hanging round it, yellow over white
        for (int i = 0; i < 13; i++)
        {
            float a = (i - 6) * 0.38f;
            var dir = V(MathF.Sin(a), 0, -MathF.Cos(a));
            var root = bc + dir * 0.14f + V(0, -0.03f, 0);
            Frond(b, Body, root, root + dir * 0.07f + V(0, -0.17f, 0), 0.055f, i % 2 == 0 ? frill : white, dir, 0.22f, Fur, 0.01f);
        }
        PokeBuilder.Both(s =>
        {
            var at = Out(bc, br, default, V(0.4f * s, 0.1f, 1f));
            b.Mark(Body, at, Outward(bc, br, at), 0.022f, 0.02f, white, MarkShape.Triangle, 180f);
        });
        int head = b.Head(bc + V(0, 0.08f, 0.09f));
        var neck = Smooth(3, bc + V(0, 0.08f, 0.11f), bc + V(0, 0.2f, 0.16f), bc + V(0, 0.32f, 0.11f), bc + V(0, 0.4f, 0.14f));
        b.Tube(head, neck, 0.03f, 0.024f, purple, blend: 0f);
        for (int i = 1; i < neck.Length - 1; i += 2)
            b.PaintEll(head, neck[i], V(0.03f, 0.014f, 0.03f), pale, soft: 0.004f);
        var c = neck[^1] + V(0, 0.04f, 0.01f);
        var r = V(0.055f, 0.055f, 0.06f);
        b.Ell(head, c, r, purple);
        b.Spike(head, c + V(0, -0.015f, 0.05f), c + V(0, -0.025f, 0.1f), 0.02f, purple, 0.7f);
        // The white hood of feathers, its rim jagged like a broken eggshell
        b.Ell(head, c + V(0, 0.03f, -0.03f), V(0.072f, 0.065f, 0.06f), white, blend: 0.01f);
        foreach (var (x, z) in new[] { (-0.04f, 0.0f), (0f, 0.015f), (0.04f, 0.0f), (0f, -0.05f) })
            b.Spike(head, c + V(x, 0.08f, z - 0.025f), c + V(x * 1.3f, 0.12f, z - 0.025f), 0.016f, white, 0.5f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.025f * s, c.Y + 0.003f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(110, 210, 222));
        });
        return b;
    }

    // ------------------------------------------------------------------ Tinkatink line

    private static readonly Color TinkPink = Rgb(232, 112, 152);
    private static readonly Color TinkPale = Rgb(246, 204, 216);
    private static readonly Color TinkPurple = Rgb(108, 90, 168);
    private static readonly Color TinkSteel = Rgb(160, 150, 182);

    /// <summary>A Tinkatink's face on the head at <paramref name="c"/>: worried purple eyes and a little frown.</summary>
    private static void TinkFace(PokeBuilder b, int head, Vector3 c, Vector3 r)
    {
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + r.X * 0.38f * s, c.Y + r.Y * 0.02f);
            b.Eye(head, at, Outward(c, r, at), r.Y * 0.19f, Rgb(120, 92, 170));
        });
        var mouth = On(c, r, c.X, c.Y - r.Y * 0.35f);
        b.Mark(head, mouth, Outward(c, r, mouth), r.X * 0.16f, r.Y * 0.08f, Rgb(120, 40, 70), MarkShape.Smile, 180f);
    }

    /// <summary>Tinkatink: a small pink imp with a round head and a pale topknot, a grey gem on its chest, a worried face, and a little mallet of purple metal in one hand.</summary>
    private static PokeBuilder Tinkatink()
    {
        var b = new PokeBuilder("Tinkatink", 0.45f, BodyPlan.Biped, V(0, 0.11f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.06f, 0));
            b.Ell(leg, V(0.04f * s, 0.03f, 0.01f), V(0.026f, 0.03f, 0.032f), TinkPink);
        });
        var bc = V(0, 0.11f, 0);
        b.Ell(Body, bc, V(0.065f, 0.06f, 0.055f), TinkPink);
        b.Box(Body, bc + V(0, 0.0f, 0.055f), V(0.014f, 0.014f, 0.01f), 0.004f, Rgb(130, 120, 150), V(0, 0, 45f), Metal, 0.004f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.055f * s, 0.02f, 0));
            var hand = bc + V(0.1f * s, -0.01f, 0.03f);
            b.Limb(arm, bc + V(0.05f * s, 0.02f, 0), hand, 0.018f, 0.02f, TinkPink);
            b.Ell(arm, hand, V(0.022f, 0.02f, 0.02f), TinkPink);
            if (s < 0) return;
            // The little mallet: a purple handle and a knob of steel
            var top = hand + V(0.07f, 0.08f, 0.0f);
            b.Limb(arm, hand + V(-0.01f, -0.02f, 0), top, 0.01f, 0.012f, TinkPurple, Metal, 0.004f);
            b.Ell(arm, top + V(0.015f, 0.02f, 0), V(0.032f, 0.04f, 0.032f), TinkPurple, Euler(top - hand), Metal, 0.006f);
            b.PaintEll(arm, top + V(0.025f, 0.04f, 0), V(0.025f, 0.02f, 0.025f), TinkSteel, soft: 0.006f);
        });
        int head = b.Head(bc + V(0, 0.06f, 0));
        var c = bc + V(0, 0.12f, 0.005f);
        var r = V(0.08f, 0.072f, 0.072f);
        b.Ell(head, c, r, TinkPink);
        b.PaintEll(head, c + V(0, 0.06f, -0.01f), V(0.07f, 0.03f, 0.07f), Rgb(214, 90, 132), soft: 0.01f);
        b.Ell(head, c + V(0, 0.08f, -0.01f), V(0.032f, 0.034f, 0.03f), TinkPale, blend: 0.012f);
        b.Spike(head, c + V(0, 0.1f, -0.015f), c + V(-0.02f, 0.13f, -0.04f), 0.018f, TinkPale, 0.6f);
        TinkFace(b, head, c, r);
        return b;
    }

    /// <summary>Tinkatuff: Tinkatink grown, two great pale lobes of hair falling behind its head, a zigzag at its hairline, carrying a heavy hammer of grey steel on a purple handle across its body.</summary>
    private static PokeBuilder Tinkatuff()
    {
        var b = new PokeBuilder("Tinkatuff", 0.65f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.1f, 0));
            b.Limb(leg, V(0.04f * s, 0.1f, 0), V(0.045f * s, 0.03f, 0.01f), 0.024f, 0.022f, TinkPink);
            b.Ell(leg, V(0.047f * s, 0.022f, 0.025f), V(0.026f, 0.022f, 0.036f), TinkPink);
        });
        var bc = V(0, 0.15f, 0);
        b.Ell(Body, bc, V(0.06f, 0.065f, 0.05f), TinkPink);
        var grip = bc + V(0.02f, 0.0f, 0.08f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.05f * s, 0.03f, 0));
            var hand = grip + V(0.035f * s, 0, 0);
            b.Limb(arm, bc + V(0.05f * s, 0.03f, 0), hand, 0.018f, 0.02f, TinkPink);
            b.Ell(arm, hand, V(0.022f, 0.022f, 0.022f), TinkPink);
            if (s < 0) return;
            // The hammer, held across it, its steel head out to the right
            var end = grip + V(0.2f, 0.02f, -0.02f);
            b.Limb(arm, grip + V(-0.08f, -0.01f, 0.01f), end, 0.012f, 0.012f, TinkPurple, Metal, 0.004f);
            b.Ell(arm, grip + V(-0.085f, -0.01f, 0.01f), V(0.018f, 0.018f, 0.018f), TinkPurple, mat: Metal, blend: 0.004f);
            b.Box(arm, end + V(0.04f, 0.0f, 0), V(0.05f, 0.075f, 0.06f), 0.02f, TinkSteel, mat: Metal, blend: 0.006f);
            b.Torus(arm, end + V(0.04f, 0.0f, 0), 0.06f, 0.008f, TinkPurple, V(0, 0, 90f), 1f, 1f, Metal, 0.004f);
        });
        int head = b.Head(bc + V(0, 0.07f, 0));
        var c = bc + V(0, 0.14f, 0.005f);
        var r = V(0.075f, 0.07f, 0.068f);
        b.Ell(head, c, r, TinkPink);
        b.PaintEll(head, c + V(0, 0.055f, 0), V(0.08f, 0.032f, 0.075f), TinkPale, soft: 0.01f);
        var zig = On(c, r, c.X, c.Y + 0.03f);
        b.Mark(head, zig, Outward(c, r, zig), 0.05f, 0.01f, TinkPale, MarkShape.Zigzag);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.04f, -0.04f));
            b.Ell(ear, c + V(0.075f * s, 0.03f, -0.07f), V(0.04f, 0.07f, 0.028f), TinkPale, V(25f, 0, -25f * s));
        });
        TinkFace(b, head, c, r);
        return b;
    }

    /// <summary>Tinkaton: a small pink smith with long pale lobes of hair hanging to its knees, a zigzag at its hairline, and an enormous hammer of steel on a purple handle swung up over its shoulder, a pale crown of spikes on its top.</summary>
    private static PokeBuilder Tinkaton()
    {
        var b = new PokeBuilder("Tinkaton", 0.95f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.1f, 0));
            b.Limb(leg, V(0.04f * s, 0.1f, 0), V(0.045f * s, 0.03f, 0.01f), 0.024f, 0.022f, TinkPink);
            b.Ell(leg, V(0.047f * s, 0.022f, 0.025f), V(0.026f, 0.022f, 0.036f), TinkPink);
        });
        var bc = V(0, 0.16f, 0);
        b.Ell(Body, bc, V(0.06f, 0.07f, 0.05f), TinkPale);
        b.PaintEll(Body, bc + V(0, -0.04f, 0), V(0.07f, 0.04f, 0.06f), TinkPink, soft: 0.01f);
        var grip = bc + V(0.07f, 0.1f, 0.04f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.05f * s, 0.04f, 0));
            var hand = s > 0 ? grip : grip + V(-0.04f, -0.05f, 0.01f);
            b.Limb(arm, bc + V(0.05f * s, 0.04f, 0), hand, 0.018f, 0.02f, TinkPink);
            b.Ell(arm, hand, V(0.022f, 0.022f, 0.022f), TinkPink);
            if (s < 0) return;
            // The great hammer: up over its shoulder, the head behind and above it
            var head = grip + V(-0.22f, 0.32f, -0.12f);
            b.Limb(arm, grip + V(0.02f, -0.04f, 0.02f), head, 0.016f, 0.016f, TinkPurple, Metal, 0.004f);
            var axis = Vector3.Normalize(V(1f, 0.1f, 0.3f));
            b.Limb(arm, head - axis * 0.16f, head + axis * 0.16f, 0.12f, 0.12f, TinkSteel, Metal, 0.008f);
            foreach (float t in new[] { -1f, 1f })
                b.Ell(arm, head + axis * 0.17f * t, V(0.11f, 0.04f, 0.11f), TinkPurple, Euler(axis), Metal, 0.008f);
            b.PaintTorus(arm, head, 0.122f, 0.012f, TinkPurple, Euler(axis));
            foreach (var (dx, h) in new[] { (-0.07f, 0.12f), (0f, 0.16f), (0.07f, 0.12f) })
                b.Spike(arm, head + axis * dx + V(0, 0.1f, 0), head + axis * dx * 1.4f + V(0, 0.1f + h, -0.01f), 0.03f, Rgb(244, 232, 176), 0.4f, Metal);
        });
        int hd = b.Head(bc + V(0, 0.07f, 0));
        var c = bc + V(0, 0.14f, 0.005f);
        var r = V(0.07f, 0.065f, 0.064f);
        b.Ell(hd, c, r, TinkPink);
        b.PaintEll(hd, c + V(0, 0.05f, 0), V(0.075f, 0.03f, 0.07f), TinkPale, soft: 0.01f);
        var zig = On(c, r, c.X, c.Y + 0.028f);
        b.Mark(hd, zig, Outward(c, r, zig), 0.045f, 0.01f, TinkPale, MarkShape.Zigzag);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(hd, s, c + V(0.05f * s, 0.03f, -0.03f));
            b.Ell(ear, c + V(0.085f * s, -0.05f, -0.035f), V(0.04f, 0.1f, 0.03f), TinkPale, V(10f, 0, 12f * s));
        });
        TinkFace(b, hd, c, r);
        return b;
    }

    // ------------------------------------------------------------------ Wiglett and Wugtrio

    /// <summary>One garden eel of the Wiglett line from <paramref name="root"/>, rising <paramref name="height"/> and bending forward at its top: a smooth tube with a small face, a round nose and bead eyes, or a white patch of a mouth for Wugtrio.</summary>
    private static void GardenEel(PokeBuilder b, int body, int head, Vector3 root, float height, float r, Color skin, Color nose, bool wug)
    {
        var top = root + V(0, height, 0);
        b.Tube(body, Smooth(3, root, root + V(0, height * 0.45f, -height * 0.05f), root + V(0, height * 0.82f, -height * 0.04f), top + V(0, -height * 0.1f, height * 0.02f)), r, r, skin, blend: 0f);
        var c = top + V(0, -height * 0.06f, height * 0.05f + r * 0.6f);
        var hr = V(r * 1.15f, r * 1.3f, r * 1.2f);
        b.Ell(head, c, hr, skin);
        b.Limb(head, top + V(0, -height * 0.15f, 0), c, r, r * 1.1f, skin);
        PokeBuilder.Both(s =>
        {
            var at = On(c, hr, c.X + hr.X * 0.42f * s, c.Y + hr.Y * 0.18f);
            b.Eye(head, at, Outward(c, hr, at), r * 0.2f, Rgb(30, 30, 36));
        });
        if (wug)
        {
            var mouth = On(c, hr, c.X, c.Y - hr.Y * 0.3f);
            b.PaintEll(head, mouth, V(hr.X * 0.45f, hr.Y * 0.28f, hr.Z * 0.3f), Rgb(246, 244, 240), soft: r * 0.15f);
            b.PaintEll(head, mouth, V(hr.X * 0.25f, hr.Y * 0.04f, hr.Z * 0.3f), Rgb(80, 40, 50), soft: r * 0.05f);
        }
        else
            b.Ell(head, On(c, hr, c.X, c.Y - hr.Y * 0.25f) + V(0, 0, r * 0.25f), V(r * 0.65f, r * 0.55f, r * 0.55f), nose, blend: r * 0.15f);
    }

    /// <summary>Wiglett: a white garden eel standing up out of a mound of sand, bending forward at its top, with a small face, bead eyes and a round red nose.</summary>
    private static PokeBuilder Wiglett()
    {
        var b = new PokeBuilder("Wiglett", 0.65f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Scales };
        b.Ell(Body, V(0, 0.02f, 0), V(0.13f, 0.03f, 0.12f), Rgb(150, 170, 150));
        int head = b.Head(V(0, 0.42f, 0.02f));
        GardenEel(b, Body, head, V(0, 0.02f, 0), 0.5f, 0.038f, Rgb(244, 242, 236), Rgb(236, 92, 104), false);
        return b;
    }

    /// <summary>Wugtrio: three red garden eels rising together through a heap of grey rock, the middle one tallest, each with bead eyes and a white patch of a mouth.</summary>
    private static PokeBuilder Wugtrio()
    {
        var b = new PokeBuilder("Wugtrio", 0.8f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Scales };
        var red = Rgb(228, 84, 96);
        var rock = Rgb(104, 106, 116);
        foreach (var (at, half, turn) in new[]
        {
            (V(0, 0.07f, 0.02f), V(0.1f, 0.07f, 0.08f), V(5f, 20f, 8f)), (V(-0.07f, 0.05f, 0.04f), V(0.06f, 0.05f, 0.06f), V(-10f, -25f, 15f)),
            (V(0.08f, 0.05f, 0.03f), V(0.06f, 0.05f, 0.06f), V(10f, 30f, -12f)), (V(0.0f, 0.13f, 0.0f), V(0.05f, 0.04f, 0.05f), V(20f, 45f, 10f)),
        })
            b.Box(Body, at, half, 0.015f, rock, turn, Shell, 0.012f);
        foreach (var (name, x, z, height, phase) in new[] { ("headM", 0f, -0.02f, 0.55f, 0f), ("headL", -0.16f, 0.0f, 0.38f, 1.7f), ("headR", 0.16f, 0.01f, 0.32f, 3.1f) })
        {
            int head = name == "headM" ? b.Head(V(x, height * 0.8f, z)) : b.Part(name, Body, V(x, height * 0.8f, z), PokeRole.Head, phase, MathF.Sign(x));
            var root = V(x, 0.04f, z);
            if (x != 0) b.Tube(Body, Smooth(2, V(x * 0.4f, 0.04f, z), V(x * 0.85f, 0.03f, z), root + V(0, 0.08f, 0)), 0.036f, 0.036f, red, blend: 0f);
            GardenEel(b, Body, head, root, height, 0.036f, red, red, true);
        }
        return b;
    }

    // ------------------------------------------------------------------ Bombirdier

    /// <summary>Bombirdier: a stork with a smug look, white with a long neck and grey wings tipped black, one spread wide, a long red bill, a little crest, red legs and a short white tail.</summary>
    private static PokeBuilder Bombirdier()
    {
        var b = new PokeBuilder("Bombirdier", 1.0f, BodyPlan.Bird, V(0, 0.38f, -0.02f)) { Coat = Fur };
        var white = Rgb(244, 242, 240);
        var grey = Rgb(200, 196, 208);
        var black = Rgb(50, 48, 58);
        var red = Rgb(210, 72, 82);
        BirdLegs(b, 0.045f, 0.28f, 0.0f, 0.016f, red);
        var bc = V(0, 0.38f, -0.02f);
        b.Ell(Body, bc, V(0.1f, 0.12f, 0.12f), white, V(-15f, 0, 0));
        FoldedWing(b, -1f, bc + V(-0.08f, 0.06f, 0.02f), bc + V(-0.1f, -0.1f, -0.16f), 0.07f, grey, black);
        SpreadWing(b, 1f, bc + V(0.08f, 0.06f, 0.01f), bc + V(0.24f, 0.1f, -0.02f), 6, 15f, 75f, 0.24f, 0.034f, grey, black, 0.3f, tipFrom: 0);
        int tail = b.Tail(bc + V(0, -0.06f, -0.1f));
        TailFan(b, tail, bc + V(0, -0.06f, -0.1f), 3, 30f, 0.12f, 0.03f, white, white, 0.5f);
        int head = b.Head(bc + V(0, 0.1f, 0.05f));
        var neck = Smooth(3, bc + V(0, 0.08f, 0.06f), bc + V(0, 0.22f, 0.1f), bc + V(0, 0.32f, 0.02f), bc + V(0, 0.42f, 0.05f));
        b.Tube(head, neck, 0.04f, 0.032f, white, blend: 0f);
        var c = neck[^1] + V(0, 0.02f, 0.01f);
        var r = V(0.045f, 0.045f, 0.05f);
        b.Ell(head, c, r, white);
        b.Spike(head, c + V(0, 0.03f, -0.03f), c + V(0, 0.06f, -0.09f), 0.02f, white, 0.4f);
        b.Spike(head, c + V(0, -0.01f, 0.04f), c + V(0, -0.04f, 0.18f), 0.02f, Rgb(230, 112, 112), 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.025f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 34, 40), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Finizen and Palafin

    private static readonly Color DolphinBlue = Rgb(92, 192, 216);
    private static readonly Color DolphinDark = Rgb(56, 140, 172);
    private static readonly Color DolphinBelly = Rgb(236, 242, 242);

    /// <summary>
    /// Finizen, and Palafin in its Zero Form: a dolphin, light blue with a white belly and a white wavy line along its
    /// sides, big eyes, a dark dorsal fin, a teal ring round the root of its tail; Palafin a little bigger with a pink
    /// mark on its belly.
    /// </summary>
    private static PokeBuilder DolphinBuild(bool palafin)
    {
        float k = palafin ? 1.1f : 1f;
        var b = new PokeBuilder(palafin ? "Palafin" : "Finizen", 0.8f, BodyPlan.Fish, V(0, 0.25f, 0)) { Coat = Scales }.Hover();
        var bc = V(0, 0.25f, 0);
        var br = V(0.1f, 0.1f, 0.2f) * k;
        b.Ell(Body, bc, br, DolphinBlue);
        b.PaintEll(Body, bc + V(0, -0.07f, 0.05f) * k, V(0.09f, 0.05f, 0.17f) * k, DolphinBelly);
        b.Ell(Body, bc + V(0, -0.03f, 0.2f) * k, V(0.045f, 0.035f, 0.07f) * k, DolphinBlue, blend: 0.02f);
        b.PaintEll(Body, bc + V(0, -0.05f, 0.24f) * k, V(0.04f, 0.02f, 0.05f) * k, DolphinBelly, soft: 0.008f);
        if (palafin) b.PaintEll(Body, bc + V(0, -0.095f, 0.06f) * k, V(0.02f, 0.012f, 0.02f) * k, Rgb(232, 100, 130), soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            var side = Out(bc, br, default, V(s, 0.25f, -0.1f));
            b.Mark(Body, side, Outward(bc, br, side), 0.08f * k, 0.014f * k, DolphinBelly, MarkShape.Wave, 0f);
            var at = Out(bc, br, default, V(0.55f * s, 0.05f, 0.85f));
            b.Eye(Body, at, Outward(bc, br, at), 0.026f * k, Rgb(220, 96, 116));
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, bc + V(0.08f * s, -0.04f, 0.06f) * k, PokeRole.Fin, 0.5f, s);
            Blade(b, fin, bc + V(0.07f * s, -0.04f, 0.06f) * k, bc + V(0.17f * s, -0.1f, 0.0f) * k, 0.045f * k, DolphinBlue, V(0, 1f, 0), 0.3f);
        });
        Blade(b, Body, bc + V(0, 0.08f, 0.0f) * k, bc + V(0, 0.18f, -0.08f) * k, 0.04f * k, DolphinDark, V(1f, 0, 0), 0.3f);
        int tail = b.Tail(bc + V(0, 0, -0.18f) * k);
        var root = bc + V(0, 0.01f, -0.3f) * k;
        b.Limb(tail, bc + V(0, 0, -0.16f) * k, root, 0.06f * k, 0.03f * k, DolphinBlue);
        b.Torus(tail, root + V(0, 0, 0.025f) * k, 0.032f * k, 0.008f * k, Rgb(70, 178, 172), V(90f, 0, 0), mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s => Blade(b, tail, root + V(0, 0, 0.02f) * k, root + V(0.11f * s, 0.01f, -0.07f) * k, 0.055f * k, DolphinBlue, V(0, 1f, 0), 0.25f));
        return Lift(b);
    }

    private static PokeBuilder Finizen() => DolphinBuild(false);

    private static PokeBuilder Palafin() => DolphinBuild(true);

    /// <summary>Palafin's Hero Form: a dolphin standing tall as a hero, its navy body on long legs with flippers for feet, a white chest with a red heart on it, great light blue arms, one flexed, a light blue mask over its eyes and two fins sweeping up from its head like a cape in the wind.</summary>
    private static PokeBuilder PalafinHeroBuild()
    {
        var b = new PokeBuilder("Palafin-Hero", 1.0f, BodyPlan.Biped, V(0, 0.52f, 0)) { Coat = Scales };
        var navy = Rgb(42, 82, 172);
        var light = Rgb(112, 192, 238);
        var white = Rgb(238, 242, 246);
        var red = Rgb(204, 52, 76);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.4f, 0));
            var knee = V(0.06f * s, 0.22f, 0.01f);
            b.Limb(leg, V(0.05f * s, 0.42f, 0), knee, 0.04f, 0.032f, navy);
            b.Limb(leg, knee, V(0.07f * s, 0.04f, 0.0f), 0.032f, 0.03f, navy);
            b.Ell(leg, V(0.08f * s, 0.022f, 0.05f), V(0.05f, 0.022f, 0.09f), navy, V(0, 15f * s, 0));
        });
        var bc = V(0, 0.55f, 0);
        b.Ell(Body, bc, V(0.1f, 0.13f, 0.08f), navy);
        b.PaintEll(Body, bc + V(0, 0.0f, 0.06f), V(0.075f, 0.11f, 0.04f), white, soft: 0.01f);
        b.PaintEll(Body, bc + V(-0.02f, -0.01f, 0.07f), V(0.03f, 0.045f, 0.03f), red, V(0, 0, 25f), 0.006f);
        b.PaintEll(Body, bc + V(0.02f, -0.01f, 0.07f), V(0.03f, 0.045f, 0.03f), red, V(0, 0, -25f), 0.006f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.1f * s, 0.08f, 0));
            var elbow = s > 0 ? bc + V(0.2f * s, 0.12f, 0.02f) : bc + V(0.17f * s, -0.04f, 0.04f);
            var fist = s > 0 ? bc + V(0.15f * s, 0.24f, 0.05f) : bc + V(0.15f * s, -0.15f, 0.1f);
            b.Ell(arm, bc + V(0.11f * s, 0.08f, 0), V(0.05f, 0.05f, 0.05f), light, blend: 0.015f);
            b.Limb(arm, bc + V(0.11f * s, 0.08f, 0), elbow, 0.045f, 0.038f, light);
            b.Limb(arm, elbow, fist, 0.038f, 0.034f, light);
            b.Ell(arm, fist, V(0.045f, 0.042f, 0.042f), light);
        });
        int head = b.Head(bc + V(0, 0.13f, 0.01f));
        var c = bc + V(0, 0.21f, 0.02f);
        var r = V(0.078f, 0.074f, 0.078f);
        b.Ell(head, c, r, navy);
        b.Ell(head, c + V(0, -0.02f, 0.06f), V(0.035f, 0.025f, 0.04f), navy, blend: 0.015f);
        b.PaintEll(head, c + V(0, 0.012f, 0.03f), V(0.075f, 0.02f, 0.06f), light, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.026f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, sclera: true, pupil: Rgb(30, 40, 70), glare: true);
            // The fins sweeping up from its head
            var fin = new[] { c + V(0.03f * s, 0.05f, -0.03f), c + V(0.09f * s, 0.14f, -0.06f), c + V(0.07f * s, 0.23f, -0.04f), c + V(0.14f * s, 0.3f, -0.06f) };
            Ribbon(b, head, fin, 0.055f, light, V(0, 0, 1f), 0.014f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Varoom and Revavroom

    private static readonly Color EngineGrey = Rgb(198, 200, 208);
    private static readonly Color EngineDark = Rgb(62, 60, 72);
    private static readonly Color EngineRock = Rgb(78, 66, 92);
    private static readonly Color EnginePurple = Rgb(152, 94, 172);
    private static readonly Color EngineYellow = Rgb(246, 214, 56);

    /// <summary>An exhaust pipe from <paramref name="from"/> through <paramref name="mid"/> to <paramref name="end"/>, its open end spattered purple.</summary>
    private static void ExhaustPipe(PokeBuilder b, int bone, Vector3 from, Vector3 mid, Vector3 end, float r)
    {
        b.Tube(bone, Smooth(3, from, mid, end), r, r * 0.9f, EngineGrey, Metal, 0.006f);
        b.Ell(bone, end, V(r * 1.3f, r * 1.3f, r * 1.3f), EngineGrey, mat: Metal, blend: 0.006f);
        b.PaintEll(bone, end, V(r * 1.6f, r * 1.6f, r * 1.6f), EnginePurple, soft: r * 0.4f);
    }

    /// <summary>Varoom: a little engine sitting on dark purple rocks, its grey block topped by a cylinder banded black and white, a pipe for a face with a dark intake at its end, a yellow eye on each side and an exhaust pipe for a tail, its end spattered purple.</summary>
    private static PokeBuilder Varoom()
    {
        var b = new PokeBuilder("Varoom", 0.75f, BodyPlan.Quadruped, V(0, 0.17f, 0)) { Coat = Metal };
        foreach (var (z, front) in new[] { (0.06f, true), (-0.07f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.1f, z), front);
                b.Ell(leg, V(0.065f * s, 0.06f, z), V(0.065f, 0.06f, 0.065f), EngineRock, V(10f * s, 20f * s, 0), Shell);
            });
        var bc = V(0, 0.17f, 0);
        var br = V(0.1f, 0.075f, 0.1f);
        b.Ell(Body, bc, br, EngineGrey);
        var top = bc + V(0, 0.16f, -0.09f);
        b.Limb(Body, bc + V(0, 0.03f, -0.02f), top, 0.06f, 0.06f, EngineDark);
        for (int i = 0; i < 3; i++)
            b.PaintTorus(Body, Vector3.Lerp(bc + V(0, 0.06f, -0.035f), top, 0.25f + i * 0.3f), 0.062f, 0.011f, EngineGrey, Euler(top - bc));
        PokeBuilder.Both(s =>
        {
            var at = Out(bc, br, default, V(s, 0.25f, 0.35f));
            b.Eye(Body, at, Outward(bc, br, at), 0.024f, EngineYellow);
        });
        int head = b.Head(bc + V(0, 0.02f, 0.08f));
        var mouth = bc + V(0, 0.04f, 0.2f);
        b.Limb(head, bc + V(0, 0.01f, 0.07f), mouth, 0.052f, 0.046f, EngineGrey);
        b.Ell(head, mouth, V(0.05f, 0.05f, 0.02f), EngineGrey, blend: 0.006f);
        b.Cut(head, mouth + V(0, 0, 0.02f), V(0.028f, 0.022f, 0.03f));
        b.PaintEll(head, mouth + V(0, 0, 0.01f), V(0.034f, 0.028f, 0.03f), EngineDark, soft: 0.004f);
        int tail = b.Tail(bc + V(0, -0.01f, -0.09f));
        ExhaustPipe(b, tail, bc + V(0, -0.01f, -0.08f), bc + V(0.02f, -0.02f, -0.2f), bc + V(0.05f, 0.03f, -0.28f), 0.025f);
        return b;
    }

    /// <summary>Revavroom: a great engine on rocks crusted with dark purple crystals, a drum on top banded black with a white zigzag, a grey face with two silver fangs gripping forward and a dark intake between, a yellow eye on each side and two long exhaust pipes for arms, their ends spattered purple.</summary>
    private static PokeBuilder Revavroom()
    {
        var b = new PokeBuilder("Revavroom", 1.0f, BodyPlan.Quadruped, V(0, 0.26f, 0)) { Coat = Metal };
        var crystal = Rgb(96, 72, 128);
        foreach (var (z, front) in new[] { (0.09f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.09f * s, 0.15f, z), front);
                b.Ell(leg, V(0.1f * s, 0.11f, z), V(0.09f, 0.11f, 0.09f), EngineRock, V(10f * s, 25f * s, 0), Shell);
            });
        var bc = V(0, 0.27f, 0);
        var br = V(0.15f, 0.1f, 0.15f);
        b.Ell(Body, bc, br, EngineGrey);
        // The crystals crusting its sides and back
        foreach (var (d, len) in new[] { (V(1f, 0.6f, -0.3f), 0.2f), (V(-1f, 0.6f, -0.3f), 0.2f), (V(0.7f, 0.9f, -0.8f), 0.18f), (V(-0.7f, 0.9f, -0.8f), 0.18f), (V(1f, 0.2f, 0.4f), 0.15f), (V(-1f, 0.2f, 0.4f), 0.15f), (V(0, 0.5f, -1f), 0.16f) })
        {
            var n = Vector3.Normalize(d);
            var root = Out(bc, br, default, d) - n * 0.03f;
            Blade(b, Body, root, root + n * len, 0.06f, crystal, V(-n.Z, 0.2f, n.X) + V(0.01f, 0, 0), 0.5f, Shell);
        }
        var drum = bc + V(0, 0.13f, -0.03f);
        b.Limb(Body, drum - V(0, 0.04f, 0), drum + V(0, 0.04f, 0), 0.1f, 0.1f, EngineDark);
        b.Torus(Body, drum + V(0, 0.045f, 0), 0.085f, 0.014f, EngineGrey, mat: Metal, blend: 0.004f);
        foreach (var dir in new[] { V(0, 0, 1f), V(0, 0, -1f), V(1f, 0, 0), V(-1f, 0, 0) })
            b.Mark(Body, drum + dir * 0.1f, dir, 0.06f, 0.016f, Rgb(240, 240, 244), MarkShape.Zigzag);
        PokeBuilder.Both(s =>
        {
            var at = Out(bc, br, default, V(s, 0.15f, 0.55f));
            b.Eye(Body, at, Outward(bc, br, at), 0.026f, EngineYellow);
            int arm = b.Arm(s, bc + V(0.13f * s, 0.02f, -0.05f));
            ExhaustPipe(b, arm, bc + V(0.12f * s, 0.02f, -0.06f), bc + V(0.28f * s, 0.08f, -0.12f), bc + V(0.36f * s, 0.02f, -0.02f), 0.03f);
        });
        int head = b.Head(bc + V(0, 0.0f, 0.12f));
        var face = bc + V(0, 0.0f, 0.2f);
        b.Ell(head, face, V(0.08f, 0.07f, 0.06f), EngineGrey, blend: 0.015f);
        b.Cut(head, face + V(0, 0, 0.06f), V(0.035f, 0.028f, 0.03f));
        b.PaintEll(head, face + V(0, 0, 0.05f), V(0.042f, 0.034f, 0.03f), EngineDark, soft: 0.004f);
        PokeBuilder.Both(s =>
            b.Tube(head, Smooth(3, face + V(0.06f * s, 0.0f, 0.02f), face + V(0.09f * s, 0.0f, 0.12f), face + V(0.03f * s, -0.02f, 0.2f)), 0.03f, 0.008f, Rgb(226, 228, 236), Metal, 0.006f));
        return b;
    }

    // ------------------------------------------------------------------ Cyclizar

    /// <summary>Cyclizar: a lizard built to be ridden, standing up on strong legs, green with a pale belly, a black tyre round its chest, a dark green helmet of a head with an orange round on each side, and a long tail pale at its end.</summary>
    private static PokeBuilder Cyclizar()
    {
        var b = new PokeBuilder("Cyclizar", 1.0f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Scales };
        var green = Rgb(128, 180, 120);
        var dark = Rgb(60, 104, 72);
        var pale = Rgb(206, 228, 186);
        var black = Rgb(40, 40, 46);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.42f, 0));
            var knee = V(0.09f * s, 0.27f, 0.07f);
            var ankle = V(0.08f * s, 0.08f, -0.03f);
            b.Limb(leg, V(0.06f * s, 0.43f, 0), knee, 0.055f, 0.04f, green);
            b.Limb(leg, knee, ankle, 0.035f, 0.028f, green);
            b.Limb(leg, ankle, V(0.08f * s, 0.025f, 0.06f), 0.026f, 0.024f, green);
            Claws(b, leg, V(0.08f * s, 0.02f, 0.08f), V(0.015f, 0, 0), V(0, -0.2f, 1f), 0.025f, 0.008f);
        });
        var bc = V(0, 0.53f, 0.02f);
        b.Ell(Body, bc, V(0.08f, 0.14f, 0.08f), green, V(-15f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.02f, 0.06f), V(0.05f, 0.11f, 0.03f), pale, V(-15f, 0, 0));
        b.Torus(Body, bc + V(0, 0.04f, 0.0f), 0.085f, 0.028f, black, V(-15f, 0, 0), 1f, 1f, Shell, 0.006f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.07f * s, 0.06f, 0.03f));
            var hand = bc + V(0.11f * s, -0.07f, 0.1f);
            b.Limb(arm, bc + V(0.07f * s, 0.06f, 0.03f), hand, 0.024f, 0.018f, green);
            Claws(b, arm, hand + V(0, -0.01f, 0.01f), V(0.008f, 0, 0), V(0, -0.5f, 1f), 0.02f, 0.006f);
        });
        int tail = b.Tail(bc + V(0, -0.12f, -0.06f));
        var tp = Smooth(3, bc + V(0, -0.12f, -0.06f), bc + V(0, -0.22f, -0.22f), bc + V(0, -0.16f, -0.38f), bc + V(0, -0.05f, -0.45f));
        b.Tube(tail, tp, 0.045f, 0.012f, green, blend: 0f);
        b.PaintEll(tail, tp[^1], V(0.05f, 0.08f, 0.05f), pale, soft: 0.02f);
        int head = b.Head(bc + V(0, 0.14f, 0.05f));
        var c = bc + V(0, 0.21f, 0.09f);
        var r = V(0.072f, 0.066f, 0.096f);
        b.Limb(head, bc + V(0, 0.12f, 0.03f), c + V(0, -0.02f, -0.04f), 0.045f, 0.045f, green);
        b.Ell(head, c, r, green);
        // The helmet: a dark green hood swept back over its head, an orange round on each side
        b.Ell(head, c + V(0, 0.042f, -0.04f), V(0.08f, 0.052f, 0.12f), dark, blend: 0.01f);
        b.Spike(head, c + V(0, 0.06f, -0.12f), c + V(0, 0.08f, -0.21f), 0.048f, dark, 0.4f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.078f * s, 0.042f, -0.025f), V(0.014f, 0.024f, 0.024f), Rgb(240, 124, 60), soft: 0.004f);
            var at = On(c, r, c.X + 0.038f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, sclera: true, pupil: Rgb(30, 36, 30));
        });
        return b;
    }

    // ------------------------------------------------------------------ Orthworm

    /// <summary>Orthworm: a great earthworm of steel, its body in red segments ringed pale with a band of silver in its middle and blue ports along its sides, its head a broad hood rearing up with a wide open mouth.</summary>
    private static PokeBuilder Orthworm()
    {
        var b = new PokeBuilder("Orthworm", 1.0f, BodyPlan.Serpent, V(0, 0.2f, 0)) { Coat = Shell };
        var red = Rgb(232, 98, 82);
        var pale = Rgb(246, 196, 176);
        var silver = Rgb(230, 226, 226);
        var blue = Rgb(92, 172, 224);
        var path = Smooth(2, V(0.44f, 0.07f, -0.02f), V(0.3f, 0.1f, -0.05f), V(0.14f, 0.11f, -0.02f), V(-0.02f, 0.13f, 0.02f), V(-0.12f, 0.24f, 0.06f), V(-0.15f, 0.38f, 0.1f));
        for (int i = 0; i < path.Length; i++)
        {
            float t = (float)i / (path.Length - 1);
            float rr = 0.065f + 0.035f * t;
            bool metal = i >= path.Length / 2 - 1 && i <= path.Length / 2;
            int bone = i < 2 ? Body : b.Part("seg" + i, Body, path[i], PokeRole.Segment, i * 0.5f);
            b.Ell(bone, path[i], V(rr, rr, rr), metal ? silver : red, mat: metal ? Metal : Shell, blend: 0.01f);
            if (i > 0) b.Limb(bone, path[i - 1], path[i], rr * 0.85f, rr * 0.85f, metal ? silver : red, metal ? Metal : Shell, 0.01f);
            if (!metal && i % 2 == 0)
                b.PaintTorus(bone, path[i], rr, rr * 0.25f, pale, Euler(path[Math.Min(i + 1, path.Length - 1)] - path[Math.Max(i - 1, 0)]));
            if (i % 2 == 1 && i < path.Length - 1)
                PokeBuilder.Both(s =>
                {
                    // A blue port in a grey rim on each side
                    var port = path[i] + Vector3.Normalize(V(s, 0.1f, 0.05f)) * rr;
                    b.PaintEll(bone, port, V(0.026f, 0.026f, 0.026f), Rgb(150, 156, 170), soft: 0.004f);
                    b.PaintEll(bone, port, V(0.019f, 0.019f, 0.019f), blue, soft: 0.004f);
                });
        }
        int head = b.Head(path[^1]);
        var c = path[^1] + V(-0.01f, 0.08f, 0.03f);
        var r = V(0.12f, 0.1f, 0.12f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, 0.01f, -0.02f), V(0.13f, 0.03f, 0.13f), pale, soft: 0.01f);
        Grin(b, head, c + V(0, -0.02f, 0.1f), V(0.075f, 0.045f, 0.05f), Rgb(84, 32, 42));
        b.PaintEll(head, c + V(0, -0.02f, 0.11f), V(0.09f, 0.06f, 0.04f), Rgb(250, 204, 196), soft: 0.006f);
        b.PaintEll(head, c + V(0, -0.02f, 0.1f), V(0.07f, 0.04f, 0.05f), Rgb(84, 32, 42), soft: 0.004f);
        return b;
    }

    // ------------------------------------------------------------------ Glimmet and Glimmora

    private static readonly Color CrystalBlue = Rgb(52, 98, 196);
    private static readonly Color CrystalTeal = Rgb(110, 200, 220);
    private static readonly Color CrystalLilac = Rgb(176, 162, 218);
    private static readonly Color CrystalGrey = Rgb(150, 176, 204);

    /// <summary>The cone of crystal a Glimmet or a Glimmora points forward, from a round base at <paramref name="at"/> with a yellow eye on each side of it.</summary>
    private static void CrystalCone(PokeBuilder b, int bone, Vector3 at, Vector3 dir, float r, float length)
    {
        dir = Vector3.Normalize(dir);
        var br = V(r, r * 0.85f, r * 1.1f);
        b.Ell(bone, at, br, CrystalGrey, mat: Shell, blend: 0.008f);
        b.Spike(bone, at + dir * r * 0.5f, at + dir * length, r * 0.8f, CrystalGrey, mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            var e = Out(at, br, default, V(0.75f * s, 0.25f, 0.6f));
            b.Eye(bone, e, Outward(at, br, e), r * 0.28f, EngineYellow);
        });
    }

    /// <summary>Glimmet: a bud of blue crystal petals with a cone pointing forward from its heart and a yellow eye on each side of it, a lilac bulb hanging beneath.</summary>
    private static PokeBuilder Glimmet()
    {
        var b = new PokeBuilder("Glimmet", 0.6f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var c = V(0, 0.36f, 0.0f);
        b.Ell(Body, V(0, 0.2f, -0.01f), V(0.065f, 0.11f, 0.065f), CrystalLilac);
        b.Spike(Body, V(0, 0.13f, -0.01f), V(0, 0.05f, -0.01f), 0.04f, CrystalLilac, 0.8f);
        int head = b.Head(c + V(0, -0.04f, 0));
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.26f;
            var dir = V(MathF.Cos(a), MathF.Sin(a), -0.25f);
            Blade(b, head, c + dir * 0.02f, c + dir * 0.13f, 0.045f, i % 2 == 0 ? CrystalBlue : Rgb(70, 130, 220), V(0, 0, 1f), 0.3f, Shell);
        }
        CrystalCone(b, head, c + V(0, 0, 0.025f), V(0, 0, 1f), 0.04f, 0.12f);
        return Lift(b);
    }

    /// <summary>Glimmora: a great closed bud of dark blue crystal petals, edged teal where they overlap, with a cone pointing forward from its front and a yellow eye on each side of it.</summary>
    private static PokeBuilder Glimmora()
    {
        var b = new PokeBuilder("Glimmora", 1.0f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Shell }.Hover();
        var bc = V(0, 0.4f, -0.03f);
        var br = V(0.2f, 0.16f, 0.22f);
        b.Ell(Body, bc, br, CrystalBlue);
        // Its petals, overlapping round it from front to back
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f + 0.3f;
            var n = V(MathF.Cos(a), MathF.Sin(a), 0);
            var pc = bc + n * 0.07f + V(0, 0, -0.02f);
            b.Ell(Body, pc, V(0.15f, 0.12f, 0.22f), CrystalBlue, V(0, 0, a * 180f / MathF.PI), blend: 0.006f);
            b.PaintEll(Body, bc + n * 0.18f + V(0, 0, 0.03f), V(0.06f, 0.06f, 0.2f), CrystalTeal, V(0, 0, a * 180f / MathF.PI), 0.03f);
        }
        int head = b.Head(bc + V(0, 0, 0.18f));
        CrystalCone(b, head, bc + V(0, -0.01f, 0.2f), V(0, -0.05f, 1f), 0.08f, 0.28f);
        return Lift(b);
    }

    /// <summary>Mega Glimmora, from its picture in Legends: Z-A: its bud burst open, six great blue petals standing round it on stalks of crystal, a star of lilac crystal blades at its heart with a teal centre and a long lance of crystal thrust forward from it.</summary>
    private static PokeBuilder GlimmoraMegaBuild()
    {
        var b = new PokeBuilder("Glimmora-Mega", 1.1f, BodyPlan.Floating, V(0, 0.45f, 0)) { Coat = Shell }.Hover();
        var c = V(0, 0.45f, 0);
        b.Ell(Body, c, V(0.07f, 0.07f, 0.06f), CrystalLilac);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            var dir = V(MathF.Cos(a), MathF.Sin(a), -0.15f);
            Blade(b, Body, c + dir * 0.03f, c + dir * 0.17f, 0.05f, CrystalLilac, V(0, 0, 1f), 0.25f, Shell);
        }
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.5f;
            var n = V(MathF.Cos(a), MathF.Sin(a), 0);
            int petal = b.Part("petal" + i, Body, c + n * 0.15f, PokeRole.Leaf, i * 0.9f);
            var pc = c + n * 0.34f + V(0, 0, -0.05f);
            b.Tube(petal, new[] { c + n * 0.06f, c + n * 0.18f + V(0, 0, -0.03f), pc - n * 0.1f }, 0.016f, 0.014f, CrystalGrey, Shell, 0.006f);
            b.Ell(petal, pc, V(0.09f, 0.15f, 0.045f), CrystalBlue, V(0, 0, a * 180f / MathF.PI - 90f), Shell, 0.01f);
            b.PaintEll(petal, pc + n * 0.11f, V(0.07f, 0.07f, 0.06f), CrystalTeal, soft: 0.025f);
        }
        int head = b.Head(c + V(0, 0, 0.05f));
        CrystalCone(b, head, c + V(0, 0, 0.06f), V(0, 0.05f, 1f), 0.05f, 0.42f);
        b.PaintEll(head, c + V(0, 0, 0.1f), V(0.03f, 0.03f, 0.03f), CrystalTeal, soft: 0.008f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Greavard and Houndstone

    private static readonly Color GhostDogWhite = Rgb(234, 234, 242);
    private static readonly Color GhostDogShade = Rgb(186, 186, 214);
    private static readonly Color GhostFlame = Rgb(196, 176, 246);

    /// <summary>Greavard: a little ghost dog of shaggy white fur hanging over its eyes, a black nose over a wide grin of jagged teeth, floppy ears, a candle on its head burning with a pale purple flame and a curled tail.</summary>
    private static PokeBuilder Greavard()
    {
        var b = new PokeBuilder("Greavard", 0.6f, BodyPlan.Quadruped, V(0, 0.14f, -0.03f)) { Coat = Fur };
        StubbyLegs(b, 0.07f, 0.08f, 0.06f, -0.09f, 0.035f, GhostDogWhite, GhostDogShade);
        var bc = V(0, 0.15f, -0.03f);
        var br = V(0.12f, 0.1f, 0.13f);
        b.Ell(Body, bc, br, GhostDogWhite);
        Shag(b, Body, bc, br, bc.Y - 0.01f, 16, 0.07f, 0.03f, GhostDogWhite, 0.7f);
        int tail = b.Tail(bc + V(0, 0.04f, -0.12f));
        CurledTail(b, tail, bc + V(0, 0.04f, -0.12f), Vector3.Normalize(V(0, 0.6f, -0.8f)), 0.08f, 0.018f, GhostDogWhite, 0.03f);
        int head = b.Head(bc + V(0, 0.05f, 0.1f));
        var c = bc + V(0, 0.08f, 0.13f);
        var r = V(0.11f, 0.095f, 0.095f);
        b.Ell(head, c, r, GhostDogWhite);
        Gape(b, head, c + V(0, -0.035f, 0.08f), V(0.07f, 0.03f, 0.035f), Rgb(184, 62, 74), 0.016f);
        b.Ell(head, c + V(0, 0.015f, 0.1f), V(0.022f, 0.016f, 0.016f), Rgb(52, 52, 62), blend: 0.008f);
        // Fur falling over its eyes
        foreach (float x in new[] { -0.06f, -0.025f, 0.025f, 0.06f })
            Frond(b, head, c + V(x * 0.8f, 0.07f, 0.04f), c + V(x * 1.3f, 0.0f, 0.1f), 0.032f, GhostDogWhite, V(0, 0.3f, 1f), 0.3f, Fur);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.09f * s, 0.03f, -0.02f));
            b.Ell(ear, c + V(0.115f * s, -0.02f, -0.02f), V(0.03f, 0.07f, 0.03f), GhostDogShade, V(0, 0, 12f * s));
        });
        // The candle on its head and its flame
        b.Limb(head, c + V(0, 0.08f, -0.03f), c + V(0, 0.17f, -0.03f), 0.022f, 0.02f, Rgb(246, 244, 240), Shell);
        b.Ell(head, c + V(0.012f, 0.15f, -0.012f), V(0.008f, 0.02f, 0.008f), Rgb(246, 244, 240), blend: 0.006f);
        int flame = b.Part("flame", head, c + V(0, 0.18f, -0.03f), PokeRole.Flame, 0.5f);
        b.Ell(flame, c + V(0, 0.2f, -0.03f), V(0.022f, 0.035f, 0.022f), GhostFlame, mat: Glow, blend: 0.008f);
        b.Spike(flame, c + V(0, 0.22f, -0.03f), c + V(0.005f, 0.27f, -0.03f), 0.018f, GhostFlame, mat: Glow, blend: 0.008f);
        return b;
    }

    /// <summary>Houndstone: a great ghost dog wrapped in a long shaggy coat of pale lilac fur, its head a grey skull of stone with its jaws open and a gravestone standing on it, on bare bony legs with claws and a spined tail of bone.</summary>
    private static PokeBuilder Houndstone()
    {
        var b = new PokeBuilder("Houndstone", 1.0f, BodyPlan.Quadruped, V(0, 0.32f, -0.05f)) { Coat = Fur };
        var lilac = Rgb(172, 170, 216);
        var stone = Rgb(146, 146, 158);
        var bone = Rgb(214, 214, 228);
        BeastLegs(b, 0.1f, 0.24f, 0.13f, -0.2f, 0.035f, bone, bone);
        var bc = V(0, 0.33f, -0.05f);
        var br = V(0.15f, 0.13f, 0.22f);
        b.Ell(Body, bc, br, lilac);
        Shag(b, Body, bc, br, bc.Y - 0.02f, 22, 0.12f, 0.035f, GhostDogWhite, 0.85f);
        FurTufts(b, Body, bc + V(0, 0.05f, 0), br * 0.95f, 14, 0.05f, 0.03f, lilac, 0.6f, -0.1f, 0.6f);
        int tail = b.Tail(bc + V(0, 0.03f, -0.21f));
        var tp = Smooth(3, bc + V(0, 0.03f, -0.21f), bc + V(0, 0.08f, -0.32f), bc + V(0.02f, 0.16f, -0.38f));
        b.Tube(tail, tp, 0.022f, 0.012f, bone, blend: 0f);
        for (int i = 1; i < tp.Length; i += 2)
            b.Spike(tail, tp[i], tp[i] + V(0, 0.04f, -0.02f), 0.012f, bone, 0.6f);
        int head = b.Head(bc + V(0, 0.04f, 0.2f));
        var c = bc + V(0, 0.07f, 0.28f);
        var r = V(0.08f, 0.07f, 0.1f);
        b.Limb(head, bc + V(0, 0.04f, 0.15f), c + V(0, -0.01f, -0.05f), 0.06f, 0.06f, lilac);
        b.Ell(head, c, r, stone, mat: Shell);
        b.Ell(head, c + V(0, -0.07f, 0.03f), V(0.065f, 0.025f, 0.08f), stone, mat: Shell, blend: 0.006f);
        b.Cut(head, c + V(0, -0.04f, 0.1f), V(0.06f, 0.025f, 0.06f));
        b.PaintEll(head, c + V(0, -0.04f, 0.08f), V(0.065f, 0.03f, 0.05f), Rgb(60, 56, 70), soft: 0.004f);
        foreach (float x in new[] { -0.04f, -0.013f, 0.013f, 0.04f })
            b.Spike(head, c + V(x, -0.025f, 0.075f + MathF.Abs(x) * -0.3f), c + V(x, -0.045f, 0.08f + MathF.Abs(x) * -0.3f), 0.008f, bone, mat: Shell, blend: 0.003f);
        PokeBuilder.Both(s => b.PaintEll(head, c + V(0.035f * s, 0.02f, 0.07f), V(0.018f, 0.015f, 0.03f), Rgb(60, 56, 70), soft: 0.004f));
        // The gravestone standing on its head
        b.Box(head, c + V(0, 0.11f, -0.04f), V(0.05f, 0.07f, 0.02f), 0.02f, stone, V(-10f, 0, 0), Shell, 0.006f);
        b.PaintEll(head, c + V(0, 0.13f, -0.015f), V(0.025f, 0.004f, 0.01f), Rgb(96, 96, 108), soft: 0.002f);
        b.PaintEll(head, c + V(0, 0.105f, -0.015f), V(0.025f, 0.004f, 0.01f), Rgb(96, 96, 108), soft: 0.002f);
        return b;
    }

    // ------------------------------------------------------------------ Flamigo

    /// <summary>Flamigo: a flamingo standing on one leg, the other tucked up, its pink body and long neck curving up to a pink head with a yellow eye and a pale bent bill tipped black, its wings folded and darker pink.</summary>
    private static PokeBuilder Flamigo()
    {
        var b = new PokeBuilder("Flamigo", 1.0f, BodyPlan.Bird, V(0, 0.56f, -0.02f)) { Coat = Fur };
        var pink = Rgb(234, 106, 136);
        var deep = Rgb(192, 62, 98);
        var leg = Rgb(158, 148, 160);
        int standing = b.Leg(1f, V(0.025f, 0.5f, 0));
        b.Limb(standing, V(0.025f, 0.5f, 0), V(0.03f, 0.27f, 0.02f), 0.013f, 0.011f, leg);
        b.Limb(standing, V(0.03f, 0.27f, 0.02f), V(0.03f, 0.03f, 0.0f), 0.011f, 0.01f, leg);
        Digits(b, standing, V(0.03f, 0.015f, 0.0f), V(0, 0, 1f), V(0.5f, 0, 0), 0.05f, 0.008f, leg);
        int tucked = b.Leg(-1f, V(-0.025f, 0.5f, 0));
        b.Limb(tucked, V(-0.025f, 0.5f, 0), V(-0.035f, 0.32f, 0.02f), 0.013f, 0.011f, leg);
        b.Limb(tucked, V(-0.035f, 0.32f, 0.02f), V(0.02f, 0.38f, 0.0f), 0.011f, 0.01f, leg);
        b.Ell(tucked, V(0.03f, 0.38f, 0.0f), V(0.018f, 0.01f, 0.02f), leg, blend: 0.006f);
        var bc = V(0, 0.58f, -0.03f);
        b.Ell(Body, bc, V(0.08f, 0.075f, 0.12f), pink, V(10f, 0, 0));
        PokeBuilder.Both(s => FoldedWing(b, s, bc + V(0.06f * s, 0.03f, 0.04f), bc + V(0.07f * s, 0.0f, -0.14f), 0.06f, deep, deep));
        int tail = b.Tail(bc + V(0, 0.01f, -0.11f));
        b.Spike(tail, bc + V(0, 0.01f, -0.1f), bc + V(0, 0.0f, -0.18f), 0.03f, deep, 0.4f);
        int head = b.Head(bc + V(0, 0.06f, 0.09f));
        var neck = Smooth(3, bc + V(0, 0.04f, 0.1f), bc + V(0, 0.18f, 0.16f), bc + V(0, 0.3f, 0.05f), bc + V(0, 0.4f, 0.03f));
        b.Tube(head, neck, 0.026f, 0.022f, pink, blend: 0f);
        var c = neck[^1] + V(0, 0.025f, 0.01f);
        var r = V(0.035f, 0.035f, 0.04f);
        b.Ell(head, c, r, pink);
        var bill = Smooth(3, c + V(0, -0.005f, 0.03f), c + V(0, -0.01f, 0.07f), c + V(0, -0.05f, 0.09f));
        b.Tube(head, bill, 0.016f, 0.008f, Rgb(246, 232, 222), Shell, 0f);
        b.PaintEll(head, bill[^1], V(0.016f, 0.025f, 0.016f), Rgb(40, 36, 44), soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.02f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, white: Rgb(246, 214, 70), pupil: Rgb(30, 30, 34));
        });
        return b;
    }

    // ------------------------------------------------------------------ Cetoddle and Cetitan

    private static readonly Color IceWhite = Rgb(242, 242, 246);
    private static readonly Color IceGrey = Rgb(196, 200, 212);
    private static readonly Color IcePink = Rgb(232, 142, 182);

    /// <summary>Cetoddle: a round white calf of a whale standing up on stubby legs, its mouth wide open, pink rings round its small eyes, pink spots, two little horns of ice, flippers raised and a tail fluke behind.</summary>
    private static PokeBuilder Cetoddle()
    {
        var b = new PokeBuilder("Cetoddle", 0.8f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.1f, 0.01f));
            b.Limb(leg, V(0.07f * s, 0.11f, 0.01f), V(0.08f * s, 0.035f, 0.02f), 0.04f, 0.035f, IceGrey);
            b.Ell(leg, V(0.08f * s, 0.025f, 0.035f), V(0.04f, 0.025f, 0.05f), IceGrey);
            b.PaintEll(leg, V(0.08f * s, 0.01f, 0.06f), V(0.025f, 0.02f, 0.02f), IcePink, soft: 0.006f);
        });
        var bc = V(0, 0.25f, 0);
        var br = V(0.15f, 0.15f, 0.14f);
        b.Ell(Body, bc, br, IceWhite);
        b.PaintEll(Body, bc + V(0, -0.12f, 0), V(0.16f, 0.06f, 0.15f), IceGrey, soft: 0.02f);
        foreach (var d in new[] { V(0.9f, 0.3f, 0.2f), V(-0.9f, -0.2f, 0.3f), V(0.6f, -0.4f, -0.6f), V(-0.4f, 0.6f, -0.7f) })
            b.PaintEll(Body, Out(bc, br, default, d), V(0.025f, 0.025f, 0.025f), IcePink, soft: 0.006f);
        Grin(b, Body, bc + V(0, -0.02f, 0.12f), V(0.075f, 0.06f, 0.045f), Rgb(232, 140, 150));
        b.PaintEll(Body, bc + V(0, -0.02f, 0.11f), V(0.05f, 0.04f, 0.04f), Rgb(80, 70, 84), soft: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = Out(bc, br, default, V(0.42f * s, 0.45f, 0.8f));
            b.PaintEll(Body, at, V(0.026f, 0.022f, 0.02f), IcePink, soft: 0.006f);
            b.Eye(Body, at, Outward(bc, br, at), 0.012f, Rgb(40, 36, 44));
            b.Spike(Body, Out(bc, br, default, V(0.25f * s, 0.95f, 0.2f)), Out(bc, br, default, V(0.25f * s, 0.95f, 0.2f)) + V(0.02f * s, 0.05f, 0.01f), 0.02f, IceWhite, 0.7f);
            int arm = b.Arm(s, bc + V(0.13f * s, 0.03f, 0.02f));
            var tip = s > 0 ? bc + V(0.2f, 0.2f, 0.02f) : bc + V(-0.24f, 0.0f, 0.04f);
            Blade(b, arm, bc + V(0.12f * s, 0.03f, 0.02f), tip, 0.045f, IceGrey, V(0, 0, 1f), 0.4f);
            b.PaintEll(arm, tip, V(0.03f, 0.03f, 0.03f), IcePink, soft: 0.01f);
        });
        int tail = b.Tail(bc + V(0, -0.06f, -0.12f));
        PokeBuilder.Both(s => Blade(b, tail, bc + V(0, -0.06f, -0.12f), bc + V(0.1f * s, 0.0f, -0.22f), 0.05f, IceGrey, V(0, 0, 1f), 0.3f));
        return b;
    }

    /// <summary>Cetitan: a great white whale walking on four legs, a long horn of ice jutting from its brow, pink spots, small eyes ringed pink, two spikes of ice on its back and a broad tail fluke.</summary>
    private static PokeBuilder Cetitan()
    {
        var b = new PokeBuilder("Cetitan", 1.0f, BodyPlan.Quadruped, V(0, 0.3f, -0.02f)) { Coat = Fur };
        var ice = Rgb(226, 234, 246);
        StubbyLegs(b, 0.14f, 0.2f, 0.13f, -0.12f, 0.06f, IceGrey, IcePink);
        var bc = V(0, 0.3f, -0.02f);
        var br = V(0.2f, 0.17f, 0.22f);
        b.Ell(Body, bc, br, IceWhite);
        b.PaintEll(Body, bc + V(0, -0.14f, 0), V(0.21f, 0.06f, 0.23f), IceGrey, soft: 0.03f);
        foreach (var d in new[] { V(1f, 0.2f, 0.2f), V(-1f, 0.1f, -0.2f), V(0.7f, 0.5f, -0.5f), V(-0.6f, 0.4f, 0.4f), V(0.3f, -0.3f, 0.9f) })
            b.PaintEll(Body, Out(bc, br, default, d), V(0.035f, 0.035f, 0.035f), IcePink, soft: 0.008f);
        PokeBuilder.Both(s => b.Spike(Body, Out(bc, br, default, V(0.4f * s, 1f, -0.3f)), Out(bc, br, default, V(0.4f * s, 1f, -0.3f)) + V(0.06f * s, 0.08f, -0.03f), 0.04f, ice, 0.6f, Shell));
        int tail = b.Tail(bc + V(0, 0.0f, -0.2f));
        b.Limb(tail, bc + V(0, 0.0f, -0.18f), bc + V(0, 0.04f, -0.3f), 0.08f, 0.05f, IceWhite);
        PokeBuilder.Both(s => Blade(b, tail, bc + V(0, 0.04f, -0.3f), bc + V(0.16f * s, 0.07f, -0.38f), 0.07f, IceGrey, V(0, 1f, 0), 0.3f));
        int head = b.Head(bc + V(0, 0.03f, 0.16f));
        var c = bc + V(0, 0.04f, 0.2f);
        var r = V(0.14f, 0.12f, 0.11f);
        b.Ell(head, c, r, IceWhite);
        b.Spike(head, c + V(0, 0.06f, 0.07f), c + V(0, 0.16f, 0.32f), 0.06f, ice, mat: Shell);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.7f * s, 0.25f, 0.7f));
            b.PaintEll(head, at, V(0.022f, 0.02f, 0.02f), IcePink, soft: 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 36, 44), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Veluza and Dondozo

    /// <summary>Veluza: a long silver fish like a barracuda, its body cased in plates with dark seams, sharp pink fins above and below, a jaw open on rows of teeth and a pink eye.</summary>
    private static PokeBuilder Veluza()
    {
        var b = new PokeBuilder("Veluza", 1.0f, BodyPlan.Fish, V(0, 0.3f, 0)) { Coat = Metal }.Hover();
        var silver = Rgb(182, 186, 198);
        var seam = Rgb(110, 114, 128);
        var pink = Rgb(222, 92, 162);
        var bc = V(0, 0.3f, 0);
        var br = V(0.065f, 0.08f, 0.3f);
        b.Ell(Body, bc, br, silver);
        foreach (float z in new[] { -0.15f, -0.05f, 0.05f })
            b.PaintTorus(Body, bc + V(0, 0, z), 0.075f, 0.005f, seam, V(90f, 0, 0), 0.9f, 1.1f);
        b.PaintEll(Body, bc + V(0, -0.05f, 0.05f), V(0.06f, 0.035f, 0.24f), Rgb(222, 224, 232), soft: 0.01f);
        Gape(b, Body, bc + V(0, -0.02f, 0.27f), V(0.04f, 0.022f, 0.05f), Rgb(204, 112, 132), 0.012f);
        PokeBuilder.Both(s =>
        {
            var at = Out(bc, br, default, V(0.6f * s, 0.35f, 0.75f));
            b.Eye(Body, at, Outward(bc, br, at), 0.016f, Rgb(212, 82, 140));
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, bc + V(0.05f * s, -0.03f, 0.1f), PokeRole.Fin, 0.4f, s);
            Blade(b, fin, bc + V(0.05f * s, -0.03f, 0.1f), bc + V(0.12f * s, -0.08f, 0.02f), 0.025f, pink, V(0, 1f, 0), 0.3f);
        });
        foreach (var (z, up, len) in new[] { (0.05f, 1f, 0.2f), (-0.12f, 1f, 0.12f), (0.0f, -1f, 0.18f), (-0.14f, -1f, 0.1f) })
            Blade(b, Body, bc + V(0, 0.06f * up, z), bc + V(0, (0.06f + len) * up, z - 0.06f), 0.04f, pink, V(1f, 0, 0), 0.25f);
        int tail = b.Tail(bc + V(0, 0, -0.26f));
        b.Limb(tail, bc + V(0, 0, -0.24f), bc + V(0, 0, -0.32f), 0.03f, 0.02f, silver);
        foreach (float up in new[] { 1f, -1f })
            Blade(b, tail, bc + V(0, 0, -0.31f), bc + V(0, 0.1f * up, -0.4f), 0.04f, pink, V(1f, 0, 0), 0.25f);
        return Lift(b);
    }

    /// <summary>Dondozo: a vast blue catfish, paler beneath with darker plates along its back, a huge mouth, small eyes set high, long whiskers trailing in curls, a white fin on its back and its tail curled down and under, ending in a broad pale fin.</summary>
    private static PokeBuilder Dondozo()
    {
        var b = new PokeBuilder("Dondozo", 1.0f, BodyPlan.Fish, V(0, 0.3f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(42, 124, 204);
        var dark = Rgb(30, 84, 152);
        var belly = Rgb(196, 212, 228);
        var fin = Rgb(232, 236, 242);
        var bc = V(0, 0.3f, 0.04f);
        var br = V(0.2f, 0.17f, 0.3f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, bc + V(0, -0.13f, 0.05f), V(0.18f, 0.08f, 0.28f), belly, soft: 0.02f);
        foreach (float z in new[] { -0.1f, 0.0f, 0.1f })
            b.PaintEll(Body, bc + V(0, 0.15f, z), V(0.09f, 0.04f, 0.04f), dark, soft: 0.01f);
        Grin(b, Body, bc + V(0, -0.05f, 0.27f), V(0.13f, 0.05f, 0.06f), Rgb(150, 60, 80));
        PokeBuilder.Both(s =>
        {
            var at = Out(bc, br, default, V(0.55f * s, 0.5f, 0.7f));
            b.Eye(Body, at, Outward(bc, br, at), 0.016f, Rgb(40, 40, 50), glare: true);
            // Whiskers trailing from the corners of its mouth and curling
            var root = bc + V(0.1f * s, -0.05f, 0.22f);
            b.Tube(Body, Smooth(3, root, root + V(0.08f * s, -0.04f, 0.04f), root + V(0.16f * s, -0.15f, -0.02f), root + V(0.12f * s, -0.22f, -0.06f), root + V(0.08f * s, -0.18f, -0.04f)), 0.008f, 0.005f, Rgb(230, 230, 236), blend: 0.004f);
            int pec = b.Part(s < 0 ? "finL" : "finR", Body, bc + V(0.17f * s, -0.07f, 0.05f), PokeRole.Fin, 0.4f, s);
            Blade(b, pec, bc + V(0.17f * s, -0.07f, 0.05f), bc + V(0.27f * s, -0.14f, -0.02f), 0.05f, fin, V(0, 1f, 0), 0.25f);
        });
        Blade(b, Body, bc + V(0, 0.14f, -0.05f), bc + V(0, 0.27f, -0.14f), 0.07f, fin, V(1f, 0, 0), 0.25f);
        int tail = b.Tail(bc + V(0, 0, -0.26f));
        var tp = Smooth(3, bc + V(0, 0.02f, -0.24f), bc + V(0, -0.06f, -0.4f), bc + V(0, -0.2f, -0.38f), bc + V(0, -0.24f, -0.24f));
        b.Tube(tail, tp, 0.1f, 0.05f, blue, blend: 0f);
        b.PaintEll(tail, tp[2], V(0.08f, 0.08f, 0.08f), belly, soft: 0.03f);
        PokeBuilder.Both(s => Blade(b, tail, tp[^1], tp[^1] + V(0.12f * s, -0.01f, 0.12f), 0.07f, fin, V(0, 1f, 0), 0.25f));
        return Lift(b);
    }
}
