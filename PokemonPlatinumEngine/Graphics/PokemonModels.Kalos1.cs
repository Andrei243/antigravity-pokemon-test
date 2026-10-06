using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Kalos's first batch in National Pokédex
// order: Chespin (650) to Slurpuff (685). Their forms are in PokemonModels.Regional.cs and PokemonModels.Megas.cs with
// the other forms. Helpers shared with the earlier batches are in PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs,
// PokemonModels.Kanto1.cs to PokemonModels.Kanto3.cs, PokemonModels.Johto1.cs, PokemonModels.Johto2.cs,
// PokemonModels.Hoenn1.cs to PokemonModels.Hoenn3.cs and PokemonModels.Unova1.cs to PokemonModels.Unova4.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Chespin line

    /// <summary>Chespin: a little tan chestnut of a Pokémon in a green hood that bristles with soft quills, a red nose, dark brown arms and big white feet.</summary>
    private static PokeBuilder Chespin()
    {
        var b = new PokeBuilder("Chespin", 0.5f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Fur };
        var tan = Rgb(214, 170, 120);
        var brown = Rgb(110, 76, 50);
        var green = Rgb(140, 196, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.06f, 0));
            b.Limb(leg, V(0.03f * s, 0.07f, 0), V(0.035f * s, 0.025f, 0.01f), 0.02f, 0.018f, tan);
            b.Ell(leg, V(0.04f * s, 0.016f, 0.03f), V(0.022f, 0.016f, 0.04f), White);
            int arm = b.Arm(s, V(0.045f * s, 0.14f, 0.01f));
            b.Limb(arm, V(0.045f * s, 0.14f, 0.01f), V(0.085f * s, 0.12f, 0.03f), 0.016f, 0.015f, brown);
            b.Ell(arm, V(0.088f * s, 0.118f, 0.032f), V(0.016f, 0.016f, 0.016f), brown);
        });
        b.Ell(Body, V(0, 0.1f, 0), V(0.05f, 0.06f, 0.045f), tan);
        b.PaintEll(Body, V(0, 0.09f, 0.04f), V(0.032f, 0.04f, 0.02f), Rgb(236, 206, 160));
        int tail = b.Tail(V(0, 0.07f, -0.04f));
        b.Tube(tail, Smooth(3, V(0, 0.07f, -0.04f), V(0, 0.05f, -0.08f), V(0.01f, 0.07f, -0.11f)), 0.014f, 0.008f, green, blend: 0f);
        // A green hood open at the face, quills bristling on top
        int head = b.Head(V(0, 0.15f, 0.01f));
        var c = V(0, 0.22f, 0.01f);
        b.Ell(head, c + V(0, 0.01f, -0.01f), V(0.08f, 0.075f, 0.07f), green);
        b.Cut(head, c + V(0, -0.02f, 0.065f), V(0.068f, 0.062f, 0.055f));
        var fc = c + V(0, -0.015f, 0.022f);
        var fr = V(0.068f, 0.062f, 0.06f);
        b.Ell(head, fc, fr, tan);
        foreach (var (x, z, len) in new[] { (-0.03f, 0.02f, 0.07f), (0f, 0.03f, 0.08f), (0.03f, 0.02f, 0.07f), (-0.015f, -0.03f, 0.06f), (0.02f, -0.03f, 0.06f) })
            b.Spike(head, c + V(x, 0.06f, z), c + V(x * 1.8f, 0.06f + len, z - 0.01f), 0.016f, green, 0.6f);
        b.Spike(head, fc + V(0, -0.005f, 0.05f), fc + V(0, -0.012f, 0.075f), 0.012f, Rgb(230, 100, 70), 0.7f);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.03f * s, fc.Y + 0.012f);
            b.Eye(head, at, Outward(fc, fr, at), 0.016f, Rgb(40, 34, 30));
        });
        var m = On(fc, fr, 0, fc.Y - 0.03f);
        b.Mark(head, m, Outward(fc, fr, m), 0.014f, 0.008f, Rgb(120, 60, 60), MarkShape.Smile);
        return b;
    }

    /// <summary>Quilladin: a round green shell of a body with a dark brown spiky belly, a small tan face under a shock of brown quills, a red nose, and thick brown arms with spikes at the shoulders.</summary>
    private static PokeBuilder Quilladin()
    {
        var b = new PokeBuilder("Quilladin", 0.75f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Shell };
        var green = Rgb(140, 190, 90);
        var brown = Rgb(110, 76, 50);
        var tan = Rgb(214, 170, 120);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.08f, 0));
            b.Limb(leg, V(0.06f * s, 0.09f, 0), V(0.07f * s, 0.03f, 0.01f), 0.035f, 0.03f, brown);
            b.Ell(leg, V(0.072f * s, 0.022f, 0.03f), V(0.032f, 0.022f, 0.045f), brown);
            Claws(b, leg, V(0.072f * s, 0.02f, 0.07f), V(0.014f, 0, 0), V(0, -0.2f, 1f), 0.016f, 0.006f);
            var shoulder = V(0.11f * s, 0.25f, 0.02f);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder, V(0.19f * s, 0.2f, 0.06f), 0.035f, 0.03f, brown);
            b.Ell(arm, V(0.2f * s, 0.195f, 0.065f), V(0.03f, 0.03f, 0.03f), brown);
            b.Spike(arm, shoulder + V(0.02f * s, 0.03f, 0), shoulder + V(0.06f * s, 0.07f, -0.01f), 0.014f, White, 0.6f, Shell);
        });
        var bc = V(0, 0.2f, 0);
        var br = V(0.13f, 0.12f, 0.11f);
        b.Ell(Body, bc, br, green);
        b.PaintEll(Body, bc + V(0, -0.06f, 0.07f), V(0.1f, 0.06f, 0.06f), brown);
        b.Mark(Body, On(bc, br, 0, bc.Y - 0.01f), Outward(bc, br, On(bc, br, 0, bc.Y - 0.01f)), 0.06f, 0.02f, brown, MarkShape.Zigzag);
        foreach (var dir in new[] { V(0.9f, 0.3f, -0.3f), V(-0.9f, 0.3f, -0.3f), V(0.5f, 0.2f, -0.8f), V(-0.5f, 0.2f, -0.8f) })
        {
            var at = Out(bc, br, default, dir);
            b.Spike(Body, at - Vector3.Normalize(dir) * 0.01f, at + Vector3.Normalize(dir) * 0.04f, 0.014f, Rgb(240, 150, 80), 0.6f, Shell);
        }
        // A small tan face set into the top of its shell, brown quills above
        int head = b.Head(V(0, 0.29f, 0.03f));
        var c = V(0, 0.33f, 0.05f);
        var r = V(0.065f, 0.055f, 0.055f);
        b.Ell(head, c, r, tan);
        foreach (var (x, len) in new[] { (-0.04f, 0.06f), (-0.015f, 0.08f), (0.015f, 0.08f), (0.04f, 0.06f) })
            b.Spike(head, c + V(x, 0.04f, -0.01f), c + V(x * 1.6f, 0.04f + len, -0.03f), 0.018f, brown, 0.6f);
        b.Ell(head, c + V(0.0f, 0.0f, 0.052f), V(0.012f, 0.01f, 0.01f), Rgb(230, 100, 70), blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.028f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(40, 34, 30));
        });
        var m = On(c, r, 0, c.Y - 0.028f);
        b.Mark(head, m, Outward(c, r, m), 0.016f, 0.009f, Rgb(140, 60, 60), MarkShape.Smile);
        return b;
    }

    /// <summary>
    /// Chesnaught and its Mega Evolution. Chesnaught is a great armoured knight, its back a green shell with two cream
    /// horns, its chest plated in cream, a white face with a shaggy beard and a red nose, and big cream arms with brown
    /// claws. Mega Chesnaught's shell has grown into a great white dome spotted green and spiked at the shoulders, its
    /// face is masked in gold, and a skirt of red-brown fur hangs at its hips.
    /// </summary>
    private static PokeBuilder ChesnaughtBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Chesnaught-Mega" : "Chesnaught", 1f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Shell };
        var cream = Rgb(236, 226, 196);
        var green = Rgb(110, 170, 90);
        var brown = Rgb(120, 80, 54);
        var rust = Rgb(150, 60, 50);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.2f, 0));
            b.Limb(leg, V(0.08f * s, 0.21f, 0), V(0.09f * s, 0.05f, 0.01f), 0.055f, 0.05f, cream);
            foreach (float y in new[] { 0.16f, 0.11f })
                b.PaintTorus(leg, V(0.086f * s, y, 0.005f), 0.052f, 0.006f, brown);
            b.Ell(leg, V(0.092f * s, 0.03f, 0.03f), V(0.05f, 0.03f, 0.06f), cream);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.092f * s + t * 0.02f, 0.025f, 0.08f), V(0.095f * s + t * 0.025f, 0.01f, 0.11f), 0.01f, brown, mat: Shell, blend: 0.004f);
            var shoulder = V(0.15f * s, 0.46f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.26f * s, 0.32f, 0.08f);
            b.Limb(arm, shoulder, hand, 0.055f, 0.048f, cream);
            b.Ell(arm, hand, V(0.05f, 0.045f, 0.05f), cream);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand + V(t * 0.025f, -0.03f, 0.03f), hand + V(t * 0.03f, -0.08f, 0.06f), 0.012f, brown, 0.7f, Shell);
            if (mega) b.Spike(arm, shoulder + V(0.03f * s, 0.04f, 0), shoulder + V(0.13f * s, 0.12f, -0.02f), 0.04f, White, mat: Shell);
        });
        b.Ell(Body, V(0, 0.36f, 0.01f), V(0.14f, 0.17f, 0.11f), cream);
        foreach (float y in new[] { 0.28f, 0.33f, 0.38f })
            b.PaintTorus(Body, V(0, y, 0.01f), 0.135f, 0.005f, brown, sz: 0.8f);
        // The shell on its back, with horns at its top
        var sc = V(0, mega ? 0.46f : 0.42f, -0.07f);
        var sr = mega ? V(0.22f, 0.22f, 0.15f) : V(0.17f, 0.19f, 0.1f);
        b.Ell(Body, sc, sr, mega ? White : green);
        if (mega)
        {
            foreach (var dir in new[] { V(0.6f, 0.5f, -0.4f), V(-0.6f, 0.5f, -0.4f), V(0, 0.8f, -0.5f), V(0.8f, -0.1f, -0.5f), V(-0.8f, -0.1f, -0.5f), V(0.3f, 0.3f, -0.9f) })
                b.PaintEll(Body, Out(sc, sr, default, dir), V(0.035f, 0.035f, 0.035f), green, soft: 0.008f);
            b.Ell(Body, V(0, 0.24f, 0.01f), V(0.15f, 0.05f, 0.12f), rust, blend: 0.02f);
            FurTufts(b, Body, V(0, 0.24f, 0.01f), V(0.14f, 0.045f, 0.11f), 22, 0.06f, 0.025f, rust, 1.1f, -1f, 0.8f);
        }
        else b.PaintTorus(Body, sc + V(0, 0, 0.06f), 0.16f, 0.012f, rust, V(90f, 0, 0), 1f, 1.1f);
        PokeBuilder.Both(s => b.Spike(Body, sc + V(0.12f * s, 0.12f, 0.02f), sc + V(0.22f * s, 0.32f, 0.0f), mega ? 0.05f : 0.04f, cream, 0.7f, Shell));
        // The face, sunk between its shoulders, a shaggy beard below
        int head = b.Head(V(0, 0.5f, 0.06f));
        var c = V(0, 0.55f, 0.08f);
        var r = V(0.07f, 0.065f, 0.06f);
        b.Ell(head, c, r, mega ? Rgb(236, 196, 70) : White);
        FurTufts(b, head, c + V(0, -0.05f, 0.02f), V(0.05f, 0.03f, 0.04f), 10, 0.04f, 0.016f, Rgb(214, 200, 176), 1.1f, -1f, 0.9f);
        foreach (float x in new[] { -0.035f, 0f, 0.035f })
            b.Spike(head, c + V(x, 0.05f, -0.01f), c + V(x * 1.5f, 0.1f, 0.0f), 0.016f, mega ? Rgb(236, 196, 70) : Rgb(150, 110, 80), 0.6f);
        b.Ell(head, c + V(0, -0.008f, 0.058f), V(0.012f, 0.01f, 0.01f), Rgb(220, 80, 70), blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.015f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(40, 34, 30), glare: true);
        });
        return b;
    }

    private static PokeBuilder Chesnaught() => ChesnaughtBuild(false);

    // ------------------------------------------------------------------ Fennekin line

    /// <summary>Fennekin: a little yellow fox with great ears full of orange-red fur, white tufts at its cheeks, orange eyes and a fluffy tail tipped orange.</summary>
    private static PokeBuilder Fennekin()
    {
        var b = new PokeBuilder("Fennekin", 0.5f, BodyPlan.Quadruped, V(0, 0.12f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 222, 120);
        var red = Rgb(232, 90, 50);
        BeastLegs(b, 0.035f, 0.1f, 0.06f, -0.06f, 0.022f, yellow, yellow);
        b.Ell(Body, V(0, 0.12f, 0), V(0.05f, 0.045f, 0.08f), yellow);
        int tail = b.Tail(V(0, 0.13f, -0.07f));
        var tp = Smooth(3, V(0, 0.13f, -0.07f), V(0, 0.17f, -0.13f), V(0, 0.2f, -0.17f));
        b.Tube(tail, tp, 0.025f, 0.035f, yellow, blend: 0.02f);
        b.Ell(tail, tp[^1] + V(0, 0.015f, -0.01f), V(0.03f, 0.035f, 0.03f), red, blend: 0.02f);
        int head = b.Head(V(0, 0.16f, 0.06f));
        var c = V(0, 0.22f, 0.07f);
        var r = V(0.065f, 0.06f, 0.06f);
        b.Ell(head, c, r, yellow);
        b.Ell(head, c + V(0, -0.02f, 0.05f), V(0.025f, 0.02f, 0.03f), yellow);
        b.Ell(head, c + V(0, -0.015f, 0.08f), V(0.008f, 0.007f, 0.006f), Rgb(60, 40, 40), blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.04f, -0.01f));
            FoxEar(b, ear, c + V(0.04f * s, 0.035f, -0.01f), c + V(0.1f * s, 0.11f, -0.03f), 0.038f, yellow, red);
            b.Ell(ear, c + V(0.06f * s, 0.065f, 0.0f), V(0.018f, 0.022f, 0.012f), red, blend: 0.01f);
            b.Spike(head, c + V(0.05f * s, -0.02f, 0.02f), c + V(0.09f * s, -0.03f, 0.03f), 0.015f, White, 0.6f);
            var at = On(c, r, 0.028f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(230, 110, 40));
        });
        return b;
    }

    /// <summary>
    /// Delphox and its Mega Evolution, with Braixen: a fox of the fire that stands upright, its ears full of orange
    /// fur, white fur at its chest and a stick it draws flame from. Braixen is yellow above, a yellow skirt of fur over
    /// thin black legs, the stick in its tail; Delphox wears a long robe of red fur and holds its flaming stick high;
    /// Mega Delphox's robe is black and violet edged with flame, and it holds a flaming stick in each hand.
    /// </summary>
    private static PokeBuilder FoxMageBuild(int stage, bool mega)
    {
        var name = stage == 1 ? "Braixen" : mega ? "Delphox-Mega" : "Delphox";
        float k = stage == 1 ? 0.8f : 1f;
        var b = new PokeBuilder(name, stage == 1 ? 0.8f : 1f, BodyPlan.Biped, V(0, 0.32f * k, 0)) { Coat = Fur };
        var yellow = Rgb(246, 222, 120);
        var red = Rgb(214, 70, 50);
        var robe = mega ? Rgb(60, 40, 70) : red;
        var black = Rgb(50, 44, 50);
        var wood = Rgb(140, 96, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.2f * k, 0));
            b.Limb(leg, V(0.035f * s, 0.21f * k, 0), V(0.045f * s, 0.03f, 0.0f), 0.018f, 0.012f, black);
            b.Ell(leg, V(0.047f * s, 0.016f, 0.02f), V(0.015f, 0.016f, 0.03f), black);
        });
        var bc = V(0, 0.32f * k, 0);
        if (stage == 1)
        {
            b.Ell(Body, bc, V(0.05f, 0.08f, 0.045f), yellow);
            b.Ell(Body, bc + V(0, -0.07f, 0), V(0.07f, 0.05f, 0.06f), yellow, blend: 0.02f);
            FurTufts(b, Body, bc + V(0, -0.08f, 0), V(0.07f, 0.04f, 0.06f), 16, 0.04f, 0.018f, yellow, 1.1f, -1f, 0.7f);
        }
        else
        {
            // A long robe of fur falling to the ground
            b.Ell(Body, bc, V(0.06f, 0.1f, 0.05f), robe);
            var hem = Smooth(3, bc + V(0, -0.04f, 0), bc + V(0, -0.16f, 0), bc + V(0, -0.27f, 0));
            for (int i = 0; i < hem.Length; i++)
            {
                float rr = 0.06f + 0.06f * i / (hem.Length - 1);
                b.Ell(Body, hem[i], V(rr, 0.04f, rr * 0.85f), robe, blend: 0.03f);
            }
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Degree;
                var d = V(MathF.Sin(a), 0, MathF.Cos(a));
                b.Spike(Body, bc + V(0, -0.26f, 0) + d * 0.09f, bc + V(0, -0.3f, 0) + d * 0.13f, 0.03f, robe, 0.5f);
                if (mega) b.PaintEll(Body, bc + V(0, -0.27f, 0) + d * 0.12f, V(0.03f, 0.02f, 0.03f), Rgb(240, 130, 50), soft: 0.008f);
            }
            b.PaintEll(Body, bc + V(0, 0.07f, 0), V(0.07f, 0.04f, 0.06f), yellow);
        }
        b.Ell(Body, bc + V(0, 0.06f, 0.035f), V(0.035f, 0.035f, 0.02f), White, blend: 0.02f);
        FurTufts(b, Body, bc + V(0, 0.06f, 0.035f), V(0.035f, 0.035f, 0.02f), 8, 0.025f, 0.012f, White, 1.1f, -1f, 0.6f);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.05f * s, 0.07f, 0.01f);
            int arm = b.Arm(s, shoulder);
            bool raised = stage > 1 && (s > 0 || mega);
            var hand = raised ? shoulder + V(0.09f * s, 0.06f, 0.06f) : shoulder + V(0.06f * s, -0.08f, 0.04f);
            b.Limb(arm, shoulder, hand, 0.016f, 0.013f, stage == 1 ? yellow : robe);
            if (stage > 1) b.Ell(arm, Vector3.Lerp(shoulder, hand, 0.6f), V(0.03f, 0.03f, 0.03f), robe, Euler(hand - shoulder), blend: 0.015f);
            b.Ell(arm, hand, V(0.013f, 0.013f, 0.013f), black);
            if (raised)
            {
                // The stick, its tip in flame
                var tip = hand + V(0.02f * s, 0.12f, 0.03f);
                b.Limb(arm, hand + V(0, -0.03f, 0), tip, 0.007f, 0.006f, wood);
                FlameTongue(b, arm, tip, tip + V(0, 0.06f, 0), 0.016f, Rgb(250, 110, 40), Rgb(255, 220, 110));
            }
        });
        int tail = b.Tail(bc + V(0, -0.06f, -0.05f));
        var tp = Smooth(3, bc + V(0, -0.06f, -0.05f), bc + V(0.02f, -0.1f, -0.12f), bc + V(0.04f, -0.06f, -0.18f));
        b.Tube(tail, tp, 0.03f, 0.04f, yellow, blend: 0.02f);
        b.Ell(tail, tp[^1] + V(0.01f, 0, -0.02f), V(0.035f, 0.04f, 0.035f), red, blend: 0.02f);
        if (stage == 1) b.Limb(tail, tp[^1] + V(0, -0.02f, 0), tp[^1] + V(0.02f, 0.12f, 0.02f), 0.006f, 0.005f, wood);
        // The head, ears full of fur and a ruff of it round the face
        int head = b.Head(bc + V(0, 0.1f, 0.01f));
        var c = bc + V(0, 0.17f, 0.02f);
        var r = V(0.05f, 0.05f, 0.05f);
        b.Limb(head, bc + V(0, 0.06f, 0.005f), c, 0.025f, 0.025f, yellow);
        b.Ell(head, c, r, yellow);
        b.Ell(head, c + V(0, -0.015f, 0.045f), V(0.02f, 0.016f, 0.03f), yellow);
        b.Ell(head, c + V(0, -0.012f, 0.075f), V(0.006f, 0.005f, 0.005f), Rgb(60, 40, 40), blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.035f, -0.01f));
            FoxEar(b, ear, c + V(0.03f * s, 0.03f, -0.01f), c + V(0.1f * s, 0.12f, -0.02f), 0.035f, yellow, red);
            FurTufts(b, ear, c + V(0.055f * s, 0.06f, 0.0f), V(0.02f, 0.03f, 0.015f), 5, 0.025f, 0.01f, red, 1.1f, -1f, -0.3f);
            b.Spike(head, c + V(0.04f * s, -0.01f, 0.01f), c + V(0.08f * s, -0.03f, 0.0f), 0.015f, stage == 1 ? yellow : red, 0.6f);
            var at = On(c, r, 0.024f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(220, 60, 40), glare: true);
        });
        return mega ? Lift(b.Hover()) : b;
    }

    private static PokeBuilder Braixen() => FoxMageBuild(1, false);

    private static PokeBuilder Delphox() => FoxMageBuild(2, false);

    // ------------------------------------------------------------------ Froakie line

    /// <summary>A collar of white foam bubbles round <paramref name="at"/>, as the Froakie line wears.</summary>
    private static void Frubbles(PokeBuilder b, int bone, Vector3 at, float ring, float size, int count, float sz = 1f)
    {
        for (int i = 0; i < count; i++)
        {
            float a = i * MathF.Tau / count;
            float k = 0.8f + 0.4f * ((i * 5) % 3) / 2f;
            b.Ell(bone, at + V(MathF.Sin(a) * ring, ((i * 3) % 2) * size * 0.4f, MathF.Cos(a) * ring * sz), V(size, size, size) * k, Rgb(244, 246, 250), blend: 0.015f);
        }
    }

    /// <summary>Froakie: a little blue frog crouching, a collar of white foam round its neck, great round yellow eyes bulging from the top of its head and white tips to its fingers.</summary>
    private static PokeBuilder Froakie()
    {
        var b = new PokeBuilder("Froakie", 0.5f, BodyPlan.Biped, V(0, 0.08f, 0)) { Coat = Scales };
        var blue = Rgb(110, 190, 240);
        var dark = Rgb(60, 130, 200);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.05f, -0.01f));
            b.Limb(leg, V(0.04f * s, 0.05f, -0.01f), V(0.07f * s, 0.03f, 0.03f), 0.022f, 0.016f, blue);
            b.Ell(leg, V(0.075f * s, 0.012f, 0.05f), V(0.025f, 0.012f, 0.03f), White);
            int arm = b.Arm(s, V(0.04f * s, 0.1f, 0.02f));
            b.Limb(arm, V(0.04f * s, 0.1f, 0.02f), V(0.05f * s, 0.03f, 0.06f), 0.013f, 0.011f, blue);
            b.Ell(arm, V(0.05f * s, 0.018f, 0.065f), V(0.016f, 0.012f, 0.016f), White);
        });
        b.Ell(Body, V(0, 0.08f, 0), V(0.055f, 0.06f, 0.055f), blue);
        b.PaintEll(Body, V(0, 0.1f, -0.04f), V(0.05f, 0.05f, 0.04f), dark);
        Frubbles(b, Body, V(0, 0.13f, 0.005f), 0.05f, 0.02f, 10);
        int head = b.Head(V(0, 0.14f, 0.01f));
        var c = V(0, 0.19f, 0.01f);
        var r = V(0.07f, 0.05f, 0.06f);
        b.Ell(head, c, r, blue);
        b.PaintEll(head, c + V(0, 0.02f, -0.03f), V(0.07f, 0.04f, 0.05f), dark);
        PokeBuilder.Both(s =>
        {
            // Great eyes bulging up from the top of its head
            var ec = c + V(0.035f * s, 0.035f, 0.02f);
            var er = V(0.035f, 0.035f, 0.032f);
            b.Ell(head, ec, er, blue, blend: 0.01f);
            var at = Out(ec, er, default, V(0.2f * s, 0.15f, 1f));
            b.Eye(head, at, Outward(ec, er, at), 0.026f, Rgb(250, 220, 70), sclera: false, pupil: Rgb(30, 30, 40));
        });
        return b;
    }

    /// <summary>Frogadier: a slender blue frog crouched to spring, a scarf of white foam over its shoulders, a dark blue crest on its head, yellow half-closed eyes and white-tipped fingers.</summary>
    private static PokeBuilder Frogadier()
    {
        var b = new PokeBuilder("Frogadier", 0.75f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Scales };
        var blue = Rgb(100, 170, 230);
        var dark = Rgb(50, 100, 180);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.12f, -0.02f));
            var knee = V(0.09f * s, 0.12f, 0.06f);
            b.Limb(leg, V(0.04f * s, 0.12f, -0.02f), knee, 0.024f, 0.018f, blue);
            b.Limb(leg, knee, V(0.08f * s, 0.02f, -0.01f), 0.017f, 0.014f, blue);
            b.Ell(leg, V(0.085f * s, 0.012f, 0.02f), V(0.022f, 0.012f, 0.035f), White);
            var shoulder = V(0.045f * s, 0.21f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var hand = shoulder + V(0.1f * s, -0.04f, -0.06f);
            b.Limb(arm, shoulder, hand, 0.013f, 0.011f, blue);
            PadDigits(b, arm, hand, V(0.4f * s, -0.4f, -0.5f), V(0, 0.3f, 0), 0.025f, 0.005f, White);
        });
        b.Ell(Body, V(0, 0.17f, 0), V(0.045f, 0.075f, 0.045f), blue, V(25f, 0, 0));
        b.PaintEll(Body, V(0, 0.18f, -0.04f), V(0.04f, 0.06f, 0.03f), dark);
        Frubbles(b, Body, V(0, 0.23f, 0.0f), 0.045f, 0.018f, 9);
        PokeBuilder.Both(s => Frubbles(b, Body, V(0.06f * s, 0.24f, -0.01f), 0.02f, 0.016f, 4));
        int head = b.Head(V(0, 0.25f, 0.03f));
        var c = V(0, 0.29f, 0.05f);
        var r = V(0.05f, 0.04f, 0.06f);
        b.Ell(head, c, r, blue);
        b.Spike(head, c + V(0, 0.02f, -0.02f), c + V(0, 0.05f, -0.1f), 0.03f, dark, 0.5f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(250, 220, 70), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Greninja and its forms: a slender ninja of a frog, dark blue above and pale blue below, its long pink tongue
    /// wound round its neck like a scarf, a cream mask over its face and red eyes. Ash-Greninja has a red and black crest
    /// on its head and a great shuriken of water on its back; Mega Greninja hangs head down from a shuriken of water
    /// spinning above it, its tongue trailing far below.
    /// </summary>
    private static PokeBuilder GreninjaBuild(int form)
    {
        bool ash = form == 1, mega = form == 2;
        var b = new PokeBuilder(ash ? "Greninja-Ash" : mega ? "Greninja-Mega" : "Greninja", 1f, mega ? BodyPlan.Floating : BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Scales };
        if (mega) b.Hover();
        var navy = mega ? Rgb(36, 40, 64) : Rgb(50, 70, 150);
        var pale = Rgb(130, 190, 236);
        var cream = Rgb(240, 230, 180);
        var pink = Rgb(236, 120, 150);
        var water = Rgb(150, 210, 246);
        // Mega Greninja hangs upside down: build it standing, then the flip is a matter of where each part goes
        float flip = mega ? -1f : 1f;
        float top = mega ? 0.96f : 0f;
        Vector3 P(float x, float y, float z) => V(x, top + flip * y, z);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, P(0.045f * s, 0.24f, 0));
            var knee = P(0.09f * s, 0.15f, 0.05f);
            var foot = P(0.08f * s, 0.025f, 0.0f);
            b.Limb(leg, P(0.045f * s, 0.25f, 0), knee, 0.035f, 0.025f, navy);
            b.Limb(leg, knee, foot, 0.022f, 0.018f, pale);
            b.Ell(leg, foot + V(0, 0, 0.02f), V(0.025f, 0.02f, 0.04f), pale);
            PadDigits(b, leg, foot + V(0, 0, 0.04f), V(0, -0.2f * flip, 1f), V(0.4f, 0, 0), 0.03f, 0.006f, cream);
            var shoulder = P(0.06f * s, 0.46f, 0.0f);
            int arm = b.Arm(s, shoulder);
            var hand = mega ? P(0.13f * s, 0.35f, 0.06f) : shoulder + V(0.13f * s, -0.06f, 0.05f);
            b.Limb(arm, shoulder, hand, 0.018f, 0.015f, navy);
            PadDigits(b, arm, hand, Vector3.Normalize(hand - shoulder), V(0, 0.3f, 0.3f), 0.03f, 0.005f, cream);
        });
        b.Ell(Body, P(0, 0.36f, 0), V(0.06f, 0.12f, 0.05f), navy);
        b.PaintEll(Body, P(0, 0.33f, 0.04f), V(0.04f, 0.09f, 0.02f), pale);
        // The tongue wound round its neck, its ends hanging
        var neck = P(0, 0.48f, 0.0f);
        b.Torus(Body, neck, 0.045f, 0.016f, pink, V(-10f * flip, 0, 0), blend: 0.006f);
        var end = mega ? Smooth(3, neck + V(0.03f, 0, 0.04f), P(0.05f, 0.62f, 0.06f), P(0.03f, 0.82f, 0.04f), P(0.0f, 0.92f, 0.05f))
            : Smooth(3, neck + V(0.03f, 0, -0.04f), P(0.08f, 0.44f, -0.1f), P(0.14f, 0.4f, -0.16f));
        b.Tube(Body, end, 0.016f, 0.014f, pink, blend: 0f);
        int head = b.Head(P(0, 0.5f, 0.01f));
        var c = P(0, 0.56f, 0.03f);
        var r = V(0.05f, 0.045f, 0.055f);
        b.Ell(head, c, r, navy);
        b.PaintEll(head, c + V(0, -0.01f * flip, 0.035f), V(0.045f, 0.025f, 0.03f), cream);
        b.Spike(head, c + P(0, 0.03f, -0.02f) - V(0, top, 0), c + P(0, 0.06f, -0.12f) - V(0, top, 0), 0.03f, navy, 0.5f);
        if (ash)
        {
            b.PaintEll(head, c + V(0, 0.03f, 0.0f), V(0.055f, 0.03f, 0.06f), Rgb(200, 40, 40));
            b.PaintEll(head, c + V(0, 0.045f, 0.0f), V(0.03f, 0.02f, 0.04f), Rgb(36, 32, 40));
            // A great shuriken of water on its back
            var back = P(0, 0.4f, -0.07f);
            b.Limb(Body, P(0, 0.4f, -0.03f), back, 0.02f, 0.02f, water);
            for (int k = 0; k < 4; k++)
            {
                float a = (45f + k * 90f) * Degree;
                var d = V(MathF.Cos(a), MathF.Sin(a), 0);
                Blade(b, Body, back, back + d * 0.2f, 0.05f, water, V(0, 0, 1f), 0.2f, Glow);
            }
        }
        if (mega)
        {
            // The shuriken of water it hangs from, spinning above its feet
            var hub = V(0, top + 0.02f, 0.0f);
            b.Ell(Body, hub, V(0.06f, 0.02f, 0.06f), water, mat: Glow, blend: 0.01f);
            for (int k = 0; k < 6; k++)
            {
                float a = k * 60f * Degree;
                var d = V(MathF.Cos(a), 0, MathF.Sin(a));
                Blade(b, Body, hub, hub + d * 0.26f + V(0, 0.01f, 0), 0.06f, water, V(0, 1f, 0), 0.2f, Glow);
            }
            PokeBuilder.Both(s => b.Limb(Body, hub, P(0.08f * s, 0.03f, 0.0f), 0.012f, 0.012f, pale));
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.005f * flip);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(214, 40, 50), glare: true);
        });
        return mega ? Lift(b) : b;
    }

    private static PokeBuilder Greninja() => GreninjaBuild(0);

    // ------------------------------------------------------------------ Bunnelby and Diggersby

    /// <summary>Bunnelby: a gray rabbit, its long ears ending in brown paddles it digs with, a brown collar of fur, two buck teeth and brown feet.</summary>
    private static PokeBuilder Bunnelby()
    {
        var b = new PokeBuilder("Bunnelby", 0.5f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Fur };
        var gray = Rgb(176, 176, 180);
        var brown = Rgb(130, 96, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.06f, 0));
            b.Limb(leg, V(0.03f * s, 0.07f, 0), V(0.035f * s, 0.02f, 0.01f), 0.018f, 0.016f, gray);
            b.Ell(leg, V(0.037f * s, 0.014f, 0.025f), V(0.018f, 0.014f, 0.035f), brown);
            int arm = b.Arm(s, V(0.035f * s, 0.13f, 0.02f));
            b.Limb(arm, V(0.035f * s, 0.13f, 0.02f), V(0.03f * s, 0.1f, 0.05f), 0.011f, 0.01f, gray);
        });
        b.Ell(Body, V(0, 0.1f, 0), V(0.045f, 0.055f, 0.04f), gray);
        b.PaintEll(Body, V(0, 0.09f, 0.035f), V(0.03f, 0.04f, 0.015f), Rgb(214, 214, 216));
        b.Ell(Body, V(0, 0.155f, 0.005f), V(0.045f, 0.018f, 0.04f), brown, blend: 0.012f);
        FurTufts(b, Body, V(0, 0.155f, 0.005f), V(0.045f, 0.018f, 0.04f), 12, 0.02f, 0.01f, brown, 1.1f, -1f, 0.4f);
        int head = b.Head(V(0, 0.17f, 0.01f));
        var c = V(0, 0.21f, 0.015f);
        var r = V(0.055f, 0.05f, 0.05f);
        b.Ell(head, c, r, gray);
        BuckTeeth(b, head, On(c, r, 0, c.Y - 0.03f), 0.008f, 0.012f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.025f * s, 0.04f, -0.01f));
            var tip = c + V(0.06f * s, 0.2f, -0.02f);
            b.Limb(ear, c + V(0.025f * s, 0.04f, -0.01f), tip - V(0, 0.04f, 0), 0.018f, 0.016f, gray);
            b.Ell(ear, tip, V(0.03f, 0.045f, 0.015f), brown, V(0, 0, -10f * s));
            var at = On(c, r, 0.025f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 36, 40));
        });
        b.Ell(head, On(c, r, 0, c.Y - 0.012f), V(0.008f, 0.006f, 0.005f), Rgb(80, 60, 60), blend: 0.004f);
        return b;
    }

    /// <summary>Diggersby: a stout gray rabbit in brown fur trousers with a yellow belly, its great ears like arms ending in brown digging paws, buck teeth and sleepy eyes.</summary>
    private static PokeBuilder Diggersby()
    {
        var b = new PokeBuilder("Diggersby", 0.85f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var gray = Rgb(176, 176, 180);
        var brown = Rgb(122, 90, 66);
        var yellow = Rgb(240, 210, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.1f, 0));
            b.Limb(leg, V(0.06f * s, 0.11f, 0), V(0.07f * s, 0.03f, 0.01f), 0.04f, 0.035f, brown);
            b.Ell(leg, V(0.072f * s, 0.022f, 0.03f), V(0.035f, 0.022f, 0.05f), Rgb(150, 150, 154));
            var shoulder = V(0.08f * s, 0.29f, 0.03f);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder, V(0.05f * s, 0.24f, 0.09f), 0.016f, 0.014f, gray);
        });
        var bc = V(0, 0.22f, 0);
        b.Ell(Body, bc, V(0.12f, 0.13f, 0.1f), brown);
        b.Ell(Body, bc + V(0, 0.08f, 0.01f), V(0.1f, 0.07f, 0.085f), gray, blend: 0.03f);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.09f), V(0.05f, 0.05f, 0.03f), yellow);
        FurTufts(b, Body, bc + V(0, -0.06f, 0), V(0.12f, 0.07f, 0.1f), 18, 0.03f, 0.02f, brown, 1.1f, -1f, 0.6f);
        int head = b.Head(V(0, 0.34f, 0.02f));
        var c = V(0, 0.38f, 0.03f);
        var r = V(0.065f, 0.055f, 0.06f);
        b.Ell(head, c, r, gray);
        BuckTeeth(b, head, On(c, r, 0, c.Y - 0.032f), 0.01f, 0.014f);
        PokeBuilder.Both(s =>
        {
            // Ears thick as arms, hanging down to great brown digging paws
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.04f, -0.01f));
            var path = Smooth(3, c + V(0.04f * s, 0.04f, -0.01f), c + V(0.14f * s, 0.06f, -0.02f), c + V(0.2f * s, -0.06f, 0.0f), c + V(0.2f * s, -0.16f, 0.03f));
            b.Tube(ear, path, 0.03f, 0.035f, gray, blend: 0f);
            b.Ell(ear, path[^1] + V(0, -0.03f, 0.01f), V(0.05f, 0.045f, 0.035f), brown);
            Claws(b, ear, path[^1] + V(0, -0.06f, 0.03f), V(0.016f, 0, 0), V(0, -0.5f, 1f), 0.02f, 0.008f);
            var at = On(c, r, 0.028f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(40, 36, 40), closed: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Fletchling line

    /// <summary>Fletchling: a little robin, its head and breast orange-red, gray-blue beneath, dark gray wings and a black tail barred with white chevrons.</summary>
    private static PokeBuilder Fletchling()
    {
        var b = new PokeBuilder("Fletchling", 0.45f, BodyPlan.Bird, V(0, 0.1f, 0)) { Coat = Fur };
        var red = Rgb(232, 96, 60);
        var blue = Rgb(176, 196, 220);
        var dark = Rgb(70, 70, 80);
        BirdLegs(b, 0.025f, 0.06f, 0, 0.008f, Rgb(50, 46, 50));
        var bc = V(0, 0.11f, 0);
        var br = V(0.06f, 0.06f, 0.07f);
        b.Ell(Body, bc, br, red);
        b.PaintEll(Body, bc + V(0, -0.04f, 0.02f), V(0.06f, 0.035f, 0.06f), blue);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.045f * s, 0.01f, 0));
            Frond(b, wing, bc + V(0.045f * s, 0.01f, 0.02f), bc + V(0.06f * s, -0.005f, -0.08f), 0.035f, dark, V(s, 0.3f, 0), 0.25f);
        });
        int tail = b.Tail(bc + V(0, 0.01f, -0.06f));
        Frond(b, tail, bc + V(0, 0.01f, -0.06f), bc + V(0, 0.07f, -0.14f), 0.03f, Rgb(40, 38, 46), V(0, 1f, 0.3f), 0.25f);
        b.Mark(tail, bc + V(0, 0.06f, -0.12f) + V(0, 0.006f, 0), V(0, 1f, 0.3f), 0.02f, 0.012f, White, MarkShape.Triangle);
        int head = b.Head(bc + V(0, 0.04f, 0.03f));
        var c = bc + V(0, 0.06f, 0.05f);
        var r = V(0.045f, 0.045f, 0.045f);
        b.Ell(head, c, r, red);
        b.Spike(head, c + V(0, -0.005f, 0.04f), c + V(0, -0.01f, 0.09f), 0.01f, Rgb(50, 46, 50), 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 36, 40), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Fletchinder and Talonflame: birds of fire, orange-red heads and breasts, dark gray wings marked with flame, black
    /// tails barred with yellow. Fletchinder stands with its wings folded; Talonflame is a falcon in flight, wings spread,
    /// talons forward.
    /// </summary>
    private static PokeBuilder FlameBirdBuild(bool talonflame)
    {
        var b = new PokeBuilder(talonflame ? "Talonflame" : "Fletchinder", talonflame ? 1f : 0.7f, BodyPlan.Bird, talonflame ? V(0, 0.42f, 0) : V(0, 0.16f, 0)) { Coat = Fur };
        if (talonflame) b.Hover();
        var red = Rgb(226, 88, 56);
        var dark = Rgb(66, 64, 72);
        var yellow = Rgb(240, 196, 70);
        var bc = talonflame ? V(0, 0.42f, 0) : V(0, 0.16f, 0);
        var br = talonflame ? V(0.08f, 0.08f, 0.13f) : V(0.08f, 0.08f, 0.1f);
        b.Ell(Body, bc, br, red, talonflame ? V(-20f, 0, 0) : default);
        b.PaintEll(Body, bc + V(0, -0.05f, 0.03f), V(0.07f, 0.04f, 0.07f), Rgb(240, 150, 110));
        if (talonflame)
        {
            PokeBuilder.Both(s =>
            {
                var shoulder = bc + V(0.06f * s, 0.04f, 0.02f);
                int wing = SpreadWing(b, s, shoulder, shoulder + V(0.22f * s, 0.06f, -0.06f), 7, 40f, -40f, 0.2f, 0.035f, dark, Rgb(40, 38, 46), 0.4f, true, 5);
                b.PaintEll(wing, shoulder + V(0.12f * s, 0.03f, -0.03f), V(0.05f, 0.03f, 0.05f), red);
            });
            DanglingLegs(b, 0.04f, bc.Y - 0.05f, 0.06f, 0.1f, 0.014f, Rgb(70, 60, 60));
        }
        else
        {
            BirdLegs(b, 0.035f, 0.09f, 0, 0.011f, Rgb(60, 56, 60));
            PokeBuilder.Both(s =>
            {
                int wing = b.Wing(s, bc + V(0.06f * s, 0.02f, 0.02f));
                Frond(b, wing, bc + V(0.06f * s, 0.02f, 0.03f), bc + V(0.08f * s, -0.01f, -0.13f), 0.05f, dark, V(s, 0.3f, 0), 0.25f);
                b.PaintEll(wing, bc + V(0.075f * s, -0.005f, -0.09f), V(0.03f, 0.03f, 0.04f), yellow);
            });
        }
        int tail = b.Tail(bc + V(0, 0, -br.Z * 0.9f));
        var tailTip = bc + (talonflame ? V(0, -0.02f, -0.3f) : V(0, 0.08f, -0.22f));
        Frond(b, tail, bc + V(0, 0, -br.Z * 0.9f), tailTip, 0.04f, Rgb(40, 38, 46), V(0, 1f, 0.3f), 0.25f);
        b.PaintEll(tail, Vector3.Lerp(bc + V(0, 0, -br.Z), tailTip, 0.75f), V(0.045f, 0.012f, 0.012f), yellow, Euler(tailTip - bc));
        int head = b.Head(bc + V(0, 0.05f, 0.07f));
        var c = bc + V(0, 0.08f, talonflame ? 0.14f : 0.08f);
        var r = V(0.045f, 0.045f, 0.05f);
        b.Ell(head, c, r, red);
        b.Ell(head, bc + V(0, 0.05f, talonflame ? 0.1f : 0.06f), V(0.05f, 0.04f, 0.05f), red, blend: 0.02f);
        b.Spike(head, c + V(0, 0, 0.045f), c + V(0, -0.02f, 0.1f), 0.014f, Rgb(50, 46, 50), 0.7f, Shell);
        b.Spike(head, c + V(0, 0.03f, -0.02f), c + V(0, 0.05f, -0.07f), 0.02f, red, 0.5f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.035f * s, 0.006f, 0.02f), V(0.012f, 0.01f, 0.012f), yellow, soft: 0.004f);
            var at = On(c, r, 0.03f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 36, 40), glare: true);
        });
        return talonflame ? Lift(b) : b;
    }

    private static PokeBuilder Fletchinder() => FlameBirdBuild(false);

    private static PokeBuilder Talonflame() => FlameBirdBuild(true);

    // ------------------------------------------------------------------ Scatterbug line

    /// <summary>Scatterbug: a little black caterpillar with a big round head, a pale face with two big eyes, a ruff of white bristles round its neck, tan spots down its back and two thin white antennae.</summary>
    private static PokeBuilder Scatterbug()
    {
        var b = new PokeBuilder("Scatterbug", 0.45f, BodyPlan.Serpent, V(0, 0.05f, 0)) { Coat = Fur };
        var black = Rgb(56, 54, 60);
        var tan = Rgb(214, 190, 150);
        var path = new[] { V(0, 0.05f, 0.02f), V(0, 0.045f, -0.02f), V(0, 0.04f, -0.055f), V(0, 0.042f, -0.08f) };
        int last = Grub(b, path, 0.042f, 0.026f, black, black, (i, seg, at, r) =>
        {
            if (i > 0) b.PaintEll(seg, at + V(0.02f, r * 0.6f, 0), V(0.012f, 0.008f, 0.012f), tan, soft: 0.004f);
        });
        PokeBuilder.Both(s => b.Spike(Body, V(0.03f * s, 0.01f, 0.02f), V(0.035f * s, -0.005f, 0.03f), 0.01f, black));
        int head = b.Head(V(0, 0.08f, 0.04f));
        var c = V(0, 0.11f, 0.06f);
        var r = V(0.055f, 0.055f, 0.05f);
        b.Ell(head, c, r, black);
        FurTufts(b, head, c + V(0, -0.05f, -0.01f), V(0.05f, 0.015f, 0.045f), 12, 0.02f, 0.01f, White, 1.1f, -1f, 0.2f);
        var fc = c + V(0, -0.005f, 0.035f);
        var fr = V(0.04f, 0.035f, 0.02f);
        b.PaintEll(head, fc, fr, tan);
        PokeBuilder.Both(s =>
        {
            Antenna(b, head, c + V(0.015f * s, 0.05f, 0), c + V(0.03f * s, 0.1f, -0.01f), c + V(0.05f * s, 0.13f, -0.01f), 0.003f, White, White, 0.004f);
            var at = On(c, r, 0.02f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(60, 50, 40), sclera: true);
        });
        return b;
    }

    /// <summary>Spewpa: a cocoon in a shaggy coat of white fur, gray beneath, its dark round face looking out with two big eyes.</summary>
    private static PokeBuilder Spewpa()
    {
        var b = new PokeBuilder("Spewpa", 0.45f, BodyPlan.Floating, V(0, 0.08f, 0)) { Coat = Fur };
        var white = Rgb(240, 240, 244);
        var gray = Rgb(150, 140, 170);
        var black = Rgb(70, 68, 74);
        var bc = V(0, 0.08f, -0.01f);
        var br = V(0.08f, 0.075f, 0.1f);
        b.Ell(Body, bc, br, white);
        FurTufts(b, Body, bc, br, 26, 0.035f, 0.02f, white, 0.75f, -0.8f, 0.4f);
        b.PaintEll(Body, bc + V(0, -0.02f, -0.07f), V(0.07f, 0.06f, 0.05f), gray);
        int head = b.Head(V(0, 0.09f, 0.06f));
        var c = V(0, 0.1f, 0.07f);
        var r = V(0.05f, 0.05f, 0.04f);
        b.Ell(head, c, r, black);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(60, 50, 40), sclera: true);
        });
        return b;
    }

    /// <summary>
    /// The colours of Vivillon's patterns: the wings' ground, two colours in bands round their roots and spots, and the
    /// colour of the rim; the Meadow pattern is the species' own.
    /// </summary>
    private static readonly Dictionary<string, (Color Ground, Color Band, Color Spot, Color Rim)> VivillonPatterns = new()
    {
        ["Meadow"] = (Rgb(232, 130, 170), Rgb(250, 230, 240), Rgb(90, 200, 200), Rgb(110, 70, 80)),
        ["Icy-Snow"] = (Rgb(236, 238, 244), Rgb(200, 204, 214), Rgb(176, 180, 190), Rgb(150, 154, 166)),
        ["Polar"] = (Rgb(40, 70, 170), Rgb(70, 110, 210), Rgb(240, 244, 250), Rgb(30, 40, 90)),
        ["Tundra"] = (Rgb(176, 214, 244), Rgb(236, 244, 252), Rgb(120, 180, 236), Rgb(70, 110, 170)),
        ["Continental"] = (Rgb(246, 220, 70), Rgb(240, 150, 50), Rgb(200, 50, 40), Rgb(60, 50, 40)),
        ["Garden"] = (Rgb(40, 160, 130), Rgb(70, 200, 160), Rgb(200, 50, 60), Rgb(30, 80, 70)),
        ["Elegant"] = (Rgb(90, 70, 150), Rgb(220, 180, 230), Rgb(240, 150, 200), Rgb(50, 40, 90)),
        ["Modern"] = (Rgb(220, 40, 40), Rgb(60, 110, 220), Rgb(250, 220, 60), Rgb(240, 240, 244)),
        ["Marine"] = (Rgb(40, 140, 230), Rgb(150, 210, 250), Rgb(240, 244, 250), Rgb(20, 70, 140)),
        ["Archipelago"] = (Rgb(200, 110, 40), Rgb(220, 50, 50), Rgb(60, 170, 90), Rgb(240, 236, 220)),
        ["High-Plains"] = (Rgb(244, 170, 80), Rgb(230, 200, 60), Rgb(60, 150, 70), Rgb(130, 80, 40)),
        ["Sandstorm"] = (Rgb(220, 196, 150), Rgb(190, 160, 110), Rgb(140, 110, 70), Rgb(110, 84, 60)),
        ["River"] = (Rgb(200, 160, 90), Rgb(160, 120, 60), Rgb(110, 84, 50), Rgb(40, 110, 200)),
        ["Monsoon"] = (Rgb(220, 222, 228), Rgb(60, 60, 70), Rgb(60, 130, 220), Rgb(150, 154, 166)),
        ["Savanna"] = (Rgb(60, 150, 220), Rgb(246, 220, 70), Rgb(30, 90, 170), Rgb(30, 70, 140)),
        ["Sun"] = (Rgb(240, 70, 60), Rgb(250, 200, 70), Rgb(240, 130, 60), Rgb(200, 40, 40)),
        ["Ocean"] = (Rgb(250, 190, 70), Rgb(240, 90, 60), Rgb(110, 190, 240), Rgb(60, 140, 220)),
        ["Jungle"] = (Rgb(90, 120, 110), Rgb(140, 200, 180), Rgb(170, 230, 210), Rgb(110, 80, 50)),
        ["Fancy"] = (Rgb(248, 220, 232), Rgb(140, 210, 120), Rgb(240, 150, 190), Rgb(110, 180, 90)),
        ["Poke-Ball"] = (Rgb(220, 40, 40), Rgb(240, 240, 244), Rgb(40, 38, 44), Rgb(30, 30, 36)),
    };

    /// <summary>Vivillon, in its Meadow pattern or another: a butterfly with a small dark body, a white face, two antennae and four broad wings in its pattern's colours, banded round their roots, spotted and rimmed.</summary>
    private static PokeBuilder VivillonBuild(string pattern)
    {
        var (ground, band, spot, rim) = VivillonPatterns[pattern];
        var b = new PokeBuilder(pattern == "Meadow" ? "Vivillon" : "Vivillon-" + pattern, 1f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var black = Rgb(56, 54, 62);
        var bc = V(0, 0.42f, 0);
        b.Ell(Body, bc, V(0.035f, 0.07f, 0.035f), black);
        b.Ell(Body, bc + V(0, -0.08f, 0), V(0.025f, 0.05f, 0.025f), black, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.02f * s, 0.01f, -0.02f));
            foreach (var (a, len, wide) in new[] { (55f, 0.3f, 0.12f), (-35f, 0.22f, 0.1f) })
            {
                var d = V(MathF.Cos(a * Degree) * s, MathF.Sin(a * Degree), 0);
                var root = bc + V(0.02f * s, 0.01f, -0.025f);
                var mid = root + d * len * 0.55f;
                var turn = Euler(d);
                b.Ell(wing, mid, V(wide, len * 0.55f, 0.01f), ground, turn, Fur, 0.02f);
                // Bands round the root, a rim at the edge, spots between
                b.PaintEll(wing, root + d * len * 0.15f, V(wide * 0.8f, len * 0.22f, 0.03f), band, turn);
                b.PaintEll(wing, root + d * len * 1.02f, V(wide * 1.05f, len * 0.12f, 0.03f), rim, turn);
                var across = V(-d.Y, d.X, 0);
                foreach (var (t, side) in new[] { (0.55f, 0.45f), (0.7f, -0.3f), (0.45f, -0.55f), (0.8f, 0.2f) })
                    b.PaintEll(wing, root + d * len * t + across * side * wide, V(0.016f, 0.016f, 0.03f), spot, soft: 0.004f);
            }
        });
        int head = b.Head(bc + V(0, 0.07f, 0.01f));
        var c = bc + V(0, 0.1f, 0.02f);
        var r = V(0.035f, 0.032f, 0.032f);
        b.Ell(head, c, r, black);
        b.PaintEll(head, c + V(0, -0.005f, 0.025f), V(0.03f, 0.025f, 0.02f), Rgb(236, 236, 240));
        PokeBuilder.Both(s =>
        {
            Antenna(b, head, c + V(0.012f * s, 0.025f, 0), c + V(0.025f * s, 0.07f, 0.0f), c + V(0.035f * s, 0.11f, -0.01f), 0.003f, black, black, 0.006f);
            var at = On(c, r, 0.014f * s, c.Y - 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(40, 36, 44));
        });
        return Lift(b);
    }

    private static PokeBuilder Vivillon() => VivillonBuild("Meadow");

    // ------------------------------------------------------------------ Litleo and Pyroar

    /// <summary>Litleo: a dark brown lion cub, a tan face framed by two round tan tufts, a tuft of fire-red and yellow hair on its head, tan paws and a tufted tail.</summary>
    private static PokeBuilder Litleo()
    {
        var b = new PokeBuilder("Litleo", 0.6f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Fur };
        var brown = Rgb(96, 70, 56);
        var tan = Rgb(222, 196, 150);
        var red = Rgb(232, 90, 50);
        BeastLegs(b, 0.045f, 0.12f, 0.07f, -0.07f, 0.032f, brown, tan);
        b.Ell(Body, V(0, 0.15f, 0), V(0.065f, 0.06f, 0.09f), brown);
        int tail = b.Tail(V(0, 0.17f, -0.08f));
        var tp = Smooth(3, V(0, 0.17f, -0.08f), V(0, 0.22f, -0.13f), V(0, 0.27f, -0.12f));
        b.Tube(tail, tp, 0.01f, 0.008f, brown, blend: 0f);
        b.Ell(tail, tp[^1] + V(0, 0.015f, 0), V(0.02f, 0.025f, 0.02f), tan, blend: 0.008f);
        int head = b.Head(V(0, 0.2f, 0.07f));
        var c = V(0, 0.24f, 0.09f);
        var r = V(0.065f, 0.06f, 0.06f);
        b.Ell(head, c, r, brown);
        var fc = c + V(0, -0.01f, 0.03f);
        b.PaintEll(head, fc + V(0, -0.005f, 0.03f), V(0.04f, 0.035f, 0.03f), tan);
        b.Ell(head, fc + V(0, -0.02f, 0.035f), V(0.022f, 0.016f, 0.015f), tan, blend: 0.012f);
        b.Ell(head, fc + V(0, -0.012f, 0.05f), V(0.008f, 0.006f, 0.005f), Rgb(230, 100, 80), blend: 0.004f);
        PokeBuilder.Both(s => b.Ell(head, c + V(0.06f * s, 0.03f, -0.02f), V(0.035f, 0.035f, 0.02f), tan));
        foreach (var (x, len, color) in new[] { (-0.012f, 0.06f, red), (0.012f, 0.07f, red), (0f, 0.05f, Rgb(250, 200, 70)) })
            b.Spike(head, c + V(x, 0.05f, 0), c + V(x * 1.5f, 0.05f + len, -0.02f), 0.016f, color, 0.6f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.028f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(40, 34, 30));
        });
        return b;
    }

    /// <summary>
    /// Pyroar and its forms: a lion of fire, tan with a dark brown back, a tufted tail and blue eyes. The male's mane is
    /// a great blaze of red and gold round its head; the female's streams back from her brow in a long red lock striped
    /// gold; Mega Pyroar's mane rises in an enormous flame.
    /// </summary>
    private static PokeBuilder PyroarBuild(int form)
    {
        bool female = form == 1, mega = form == 2;
        var b = new PokeBuilder(female ? "Pyroar-Female" : mega ? "Pyroar-Mega" : "Pyroar", 1f, BodyPlan.Quadruped, V(0, 0.3f, -0.02f)) { Coat = Fur };
        var tan = Rgb(214, 180, 130);
        var brown = Rgb(90, 66, 52);
        var red = Rgb(214, 50, 40);
        var gold = Rgb(250, 200, 60);
        BeastLegs(b, 0.07f, 0.26f, 0.15f, -0.17f, 0.05f, tan, Rgb(200, 170, 120), brown);
        var bc = V(0, 0.3f, -0.02f);
        b.Ell(Body, bc, V(0.1f, 0.09f, 0.19f), tan);
        b.PaintEll(Body, bc + V(0, 0.06f, 0), V(0.1f, 0.05f, 0.2f), brown);
        int tail = b.Tail(bc + V(0, 0.04f, -0.18f));
        var tp = Smooth(3, bc + V(0, 0.04f, -0.18f), bc + V(0, 0.12f, -0.26f), bc + V(0, 0.2f, -0.24f));
        b.Tube(tail, tp, 0.014f, 0.01f, brown, blend: 0f);
        b.Ell(tail, tp[^1] + V(0, 0.02f, 0), V(0.03f, 0.04f, 0.03f), tan, blend: 0.01f);
        int head = b.Head(bc + V(0, 0.08f, 0.16f));
        var c = bc + V(0, 0.16f, 0.23f);
        var r = V(0.055f, 0.055f, 0.06f);
        b.Ell(head, bc + V(0, 0.08f, 0.14f), V(0.06f, 0.07f, 0.06f), tan, blend: 0.02f);
        b.Ell(head, c, r, tan);
        b.Ell(head, c + V(0, -0.02f, 0.05f), V(0.03f, 0.025f, 0.03f), tan);
        b.Ell(head, c + V(0, -0.01f, 0.08f), V(0.009f, 0.007f, 0.006f), Rgb(80, 50, 40), blend: 0.004f);
        if (female)
        {
            // A long lock of mane streaming back from her brow, red striped gold
            var lockPath = Smooth(3, c + V(0, 0.04f, 0.0f), c + V(0, 0.08f, -0.1f), c + V(0, 0.04f, -0.25f), c + V(0, -0.02f, -0.4f));
            Ribbon(b, head, lockPath, 0.07f, red, V(0, 1f, 0.2f), 0.02f);
            for (int i = 1; i < lockPath.Length; i += 2)
                b.PaintEll(head, lockPath[i] + V(0, 0.01f, 0), V(0.012f, 0.02f, 0.03f), gold, soft: 0.006f);
        }
        else
        {
            // A blaze of mane round the head, red outside and gold within; Mega Pyroar's rises high
            int n = mega ? 14 : 12;
            for (int i = 0; i < n; i++)
            {
                float a = (i + 0.5f) * MathF.Tau / n;
                var d = V(MathF.Sin(a), MathF.Cos(a), 0);
                float len = mega ? 0.12f + 0.12f * MathF.Max(0f, d.Y) : 0.13f + 0.03f * MathF.Max(0f, -d.Y);
                var root = c + V(0, 0, -0.02f) + d * 0.04f;
                FlameTongue(b, head, root, root + Vector3.Normalize(d + V(0, mega ? 0.6f : 0.1f, -0.4f)) * len, mega ? 0.04f : 0.05f, red, gold);
            }
            if (mega) FlameTongue(b, head, c + V(0, 0.06f, -0.04f), c + V(0, 0.32f, -0.12f), 0.06f, red, gold);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(70, 180, 220), glare: true);
        });
        return b;
    }

    private static PokeBuilder Pyroar() => PyroarBuild(0);

    // ------------------------------------------------------------------ Flabébé line

    /// <summary>The colour of a flower of the Flabébé line: red is the species' own, then yellow, orange, blue and white.</summary>
    private static Color FlowerColour(string? colour) => colour switch
    {
        "Yellow" => Rgb(250, 220, 90),
        "Orange" => Rgb(246, 150, 60),
        "Blue" => Rgb(70, 120, 220),
        "White" => Rgb(244, 244, 238),
        _ => Rgb(232, 90, 100)
    };

    /// <summary>The fairy of the Flabébé line: a small white face with a ruff of petals for hair, two eyes, and a pair of pale wings like leaves.</summary>
    private static void FlowerFairy(PokeBuilder b, int head, Vector3 c, float k, Color hair, bool wings)
    {
        var r = V(0.04f, 0.038f, 0.036f) * k;
        b.Ell(head, c, r, White);
        for (int i = 0; i < 7; i++)
        {
            float a = (i / 6f - 0.5f) * 200f * Degree;
            var at = c + V(MathF.Sin(a) * 0.035f, 0.02f + MathF.Cos(a) * 0.022f, -0.012f) * k;
            b.Ell(head, at, V(0.016f, 0.016f, 0.016f) * k, hair, blend: 0.008f);
        }
        if (wings)
            PokeBuilder.Both(s => Petal(b, head, c + V(0.03f * s, 0.0f, -0.02f) * k, c + V(0.1f * s, 0.03f, -0.04f) * k, 0.025f * k, Rgb(236, 240, 236)));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.015f * s * k, c.Y);
            b.Eye(head, at, Outward(c, r, at), 0.01f * k, Rgb(40, 40, 50), sclera: true);
        });
    }

    /// <summary>Flabébé in its flower's colour: a tiny white fairy sitting on a five-petalled flower with a yellow heart, its green stem below, stamens curling over it.</summary>
    private static PokeBuilder FlabebeBuild(string? colour)
    {
        var b = new PokeBuilder(colour == null ? "Flabébé" : "Flabébé-" + colour, 0.4f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Leaf }.Hover();
        var petal = FlowerColour(colour);
        var green = Rgb(140, 200, 110);
        // The flower, facing up, its stem and two leaves below
        var heart = V(0, 0.18f, 0);
        FlowerHead(b, Body, heart, V(0, 1f, 0.15f), 5, 0.13f, 0.06f, petal, Rgb(250, 220, 90), 0.035f);
        b.Tube(Body, Smooth(3, heart, heart + V(0, -0.06f, 0.01f), heart + V(0.01f, -0.14f, 0.0f)), 0.012f, 0.01f, green, blend: 0f);
        PokeBuilder.Both(s => Petal(b, Body, heart + V(0, -0.09f, 0), heart + V(0.05f * s, -0.12f, 0.02f), 0.015f, green, Leaf));
        PokeBuilder.Both(s => Antenna(b, Body, heart + V(0.02f * s, 0.01f, -0.01f), heart + V(0.06f * s, 0.06f, -0.01f), heart + V(0.08f * s, 0.05f, 0.02f), 0.003f, green, Rgb(250, 220, 90), 0.008f));
        // The fairy on the flower's heart
        b.Ell(Body, heart + V(0, 0.035f, 0.0f), V(0.02f, 0.025f, 0.018f), White, blend: 0.01f);
        int head = b.Head(heart + V(0, 0.05f, 0.0f));
        FlowerFairy(b, head, heart + V(0, 0.085f, 0.0f), 1f, Rgb(250, 220, 90), true);
        return Lift(b);
    }

    private static PokeBuilder Flabebe() => FlabebeBuild(null);

    /// <summary>
    /// Floette and its forms: a little fairy with a white face and green leaves for arms, legs and a skirt, holding up a
    /// flower bigger than itself in its colour. The Eternal Floette holds a great flower of dark red; Mega Floette floats
    /// in a frame of two great dark loops of petal round a red heart.
    /// </summary>
    private static PokeBuilder FloetteBuild(string? colour, bool eternal = false, bool mega = false)
    {
        var name = mega ? "Floette-Mega" : eternal ? "Floette-Eternal" : colour == null ? "Floette" : "Floette-" + colour;
        var b = new PokeBuilder(name, mega ? 0.9f : 0.5f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Leaf }.Hover();
        var green = Rgb(80, 180, 140);
        var bc = V(0, 0.2f, 0);
        b.Ell(Body, bc, V(0.025f, 0.035f, 0.022f), White);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, bc + V(0.012f * s, -0.03f, 0));
            b.Limb(leg, bc + V(0.012f * s, -0.03f, 0), bc + V(0.02f * s, -0.1f, 0.01f), 0.007f, 0.005f, green);
            b.Spike(leg, bc + V(0.02f * s, -0.1f, 0.01f), bc + V(0.025f * s, -0.13f, 0.02f), 0.012f, green, 0.4f);
            int arm = b.Arm(s, bc + V(0.022f * s, 0.02f, 0));
            b.Limb(arm, bc + V(0.022f * s, 0.02f, 0), bc + V(0.05f * s, s > 0 ? 0.06f : -0.01f, 0.02f), 0.006f, 0.005f, green);
        });
        Frond(b, Body, bc + V(0, -0.01f, 0.01f), bc + V(0, -0.05f, 0.03f), 0.02f, green, V(0, 0, 1f), 0.3f, Leaf);
        int head = b.Head(bc + V(0, 0.035f, 0));
        FlowerFairy(b, head, bc + V(0, 0.07f, 0.0f), 1f, Rgb(246, 236, 180), false);
        // The flower it holds, its stem in its right hand
        var hand = bc + V(0.05f, 0.06f, 0.02f);
        int arm2 = b.Part("stem", Body, hand, PokeRole.Arm, 0f, 1f);
        if (mega)
        {
            // A frame of two great dark loops round a red heart
            var heart = bc + V(0, 0.2f, -0.04f);
            b.Tube(arm2, Smooth(3, hand, hand + V(0.0f, 0.07f, -0.02f), heart + V(0, -0.03f, 0)), 0.006f, 0.006f, green, blend: 0f);
            PokeBuilder.Both(s =>
            {
                b.Torus(arm2, heart + V(0.14f * s, 0.02f, 0), 0.11f, 0.022f, Rgb(36, 34, 46), V(90f, 0, 0), 1.2f, 0.8f, Leaf, 0.01f);
                b.PaintEll(arm2, heart + V(0.26f * s, 0.02f, 0), V(0.06f, 0.1f, 0.05f), Rgb(50, 90, 170), soft: 0.02f);
                b.PaintEll(arm2, heart + V(0.05f * s, 0.02f, 0), V(0.04f, 0.07f, 0.05f), Rgb(200, 40, 50), soft: 0.015f);
                Blade(b, arm2, heart + V(0.24f * s, 0.0f, 0), heart + V(0.38f * s, -0.04f, 0), 0.03f, Rgb(36, 34, 46), V(0, 0, 1f), 0.25f, Leaf);
            });
            b.Ell(arm2, heart, V(0.05f, 0.05f, 0.03f), Rgb(200, 40, 50), mat: Leaf, blend: 0.02f);
        }
        else
        {
            var flower = bc + V(0.08f, 0.16f, 0.0f);
            b.Tube(arm2, Smooth(3, hand, hand + V(0.015f, 0.05f, 0), flower + V(0, -0.02f, 0)), 0.006f, 0.006f, green, blend: 0f);
            var petal = eternal ? Rgb(150, 30, 40) : FlowerColour(colour);
            float size = eternal ? 0.11f : 0.08f;
            FlowerHead(b, arm2, flower, V(0.3f, 0.3f, 1f), 5, size, size * 0.55f, petal, Rgb(150, 210, 100), 0.025f);
            if (eternal) b.PaintEll(arm2, flower, V(0.06f, 0.06f, 0.06f), Rgb(40, 30, 40));
            Petal(b, arm2, flower + V(-0.02f, -0.02f, -0.02f), flower + V(-0.07f, 0.04f, -0.03f), 0.025f, Rgb(140, 200, 110), Leaf);
        }
        return Lift(b);
    }

    private static PokeBuilder Floette() => FloetteBuild(null);

    /// <summary>Florges in its flowers' colour: a white fairy rising from a green stem between two great leaves, crowned with a great round wreath of flowers falling to its shoulders.</summary>
    private static PokeBuilder FlorgesBuild(string? colour)
    {
        var b = new PokeBuilder(colour == null ? "Florges" : "Florges-" + colour, 0.9f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Leaf };
        var green = Rgb(40, 170, 140);
        var crown = FlowerColour(colour);
        var light = PixelCanvas.Mix(crown, White, 0.45f);
        // A green stem with two great leaves, standing on a base of small leaves
        b.Tube(Body, Smooth(3, V(0, 0.02f, 0), V(0, 0.15f, 0.01f), V(0, 0.3f, 0)), 0.02f, 0.03f, green, blend: 0f);
        PokeBuilder.Both(s =>
        {
            Blade(b, Body, V(0.01f * s, 0.12f, 0), V(0.18f * s, 0.3f, 0.02f), 0.06f, green, V(0, 0.2f, 1f), 0.2f, Leaf);
            Blade(b, Body, V(0, 0.02f, 0), V(0.05f * s, 0.0f, 0.03f), 0.02f, green, V(0, 1f, 0), 0.3f, Leaf);
        });
        b.Ell(Body, V(0, 0.33f, 0), V(0.035f, 0.05f, 0.03f), White);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.03f * s, 0.36f, 0));
            b.Limb(arm, V(0.03f * s, 0.36f, 0), V(0.07f * s, 0.32f, 0.04f), 0.008f, 0.007f, White);
        });
        int head = b.Head(V(0, 0.38f, 0.0f));
        var c = V(0, 0.42f, 0.02f);
        var r = V(0.04f, 0.04f, 0.035f);
        // The wreath: rounds of flowers about the face, hanging down to the shoulders
        foreach (var (x, y, z, k) in new[] { (0f, 0.06f, -0.03f, 0.07f), (-0.06f, 0.04f, -0.03f, 0.055f), (0.06f, 0.04f, -0.03f, 0.055f), (-0.08f, -0.01f, -0.03f, 0.045f), (0.08f, -0.01f, -0.03f, 0.045f), (-0.03f, 0.08f, -0.04f, 0.045f), (0.03f, 0.08f, -0.04f, 0.045f), (0f, 0.03f, -0.07f, 0.06f) })
            b.Ell(head, c + V(x, y, z), V(k, k * 0.9f, k * 0.8f), crown, mat: Leaf, blend: 0.02f);
        foreach (var (x, y, z) in new[] { (-0.04f, 0.09f, 0.0f), (0.04f, 0.08f, 0.01f), (-0.08f, 0.03f, 0.01f), (0.08f, 0.04f, 0.0f), (0f, 0.1f, -0.01f) })
            b.PaintEll(head, c + V(x, y, z), V(0.014f, 0.014f, 0.03f), light, soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            var strand = Smooth(3, c + V(0.07f * s, -0.03f, -0.02f), c + V(0.09f * s, -0.09f, -0.01f), c + V(0.08f * s, -0.15f, 0.0f));
            b.Tube(head, strand, 0.008f, 0.007f, light, Leaf, blend: 0f);
            foreach (var at in strand.Where((_, i) => i % 2 == 0))
                b.Ell(head, at, V(0.014f, 0.014f, 0.014f), light, mat: Leaf, blend: 0.01f);
        });
        b.Ell(head, c, r, White);
        b.Tube(head, Smooth(3, c + V(-0.02f, 0.03f, 0.03f), c + V(0, 0.0f, 0.045f), c + V(0.02f, -0.03f, 0.04f)), 0.006f, 0.006f, Rgb(80, 200, 150), blend: 0f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.016f * s, c.Y - 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(40, 40, 50), sclera: true);
        });
        return b;
    }

    private static PokeBuilder Florges() => FlorgesBuild(null);

    // ------------------------------------------------------------------ Skiddo and Gogoat

    /// <summary>
    /// Skiddo and Gogoat: goats with leaves growing along their backs. Skiddo is a kid, dark brown with a white face and
    /// legs and a collar of leaves; Gogoat is a great mount, tan with a ragged coat, dark brown horns curling back, a
    /// mane of leaves down its back, a white beard and orange hooves.
    /// </summary>
    private static PokeBuilder GoatBuild(bool gogoat)
    {
        float k = gogoat ? 1.35f : 1f;
        var b = new PokeBuilder(gogoat ? "Gogoat" : "Skiddo", gogoat ? 1f : 0.75f, BodyPlan.Quadruped, V(0, 0.22f * k, 0)) { Coat = Fur };
        var coat = gogoat ? Rgb(176, 150, 116) : Rgb(96, 76, 66);
        var white = Rgb(236, 236, 232);
        var leaf = Rgb(70, 160, 80);
        var hoof = Rgb(230, 120, 70);
        BeastLegs(b, 0.06f * k, 0.18f * k, 0.12f * k, -0.13f * k, 0.035f * k, gogoat ? coat : white, hoof, white);
        var bc = V(0, 0.22f * k, 0);
        var br = V(0.08f, 0.075f, 0.15f) * k;
        b.Ell(Body, bc, br, coat);
        if (gogoat)
        {
            FurTufts(b, Body, bc + V(0, -0.02f, 0), br, 26, 0.04f, 0.02f, coat, 1.1f, -0.45f, 0.9f);
            b.PaintEll(Body, bc + V(0, -0.06f, 0), V(0.1f, 0.03f, 0.2f), Rgb(110, 90, 70));
        }
        else b.PaintEll(Body, bc + V(0, -0.02f, 0.1f), V(0.07f, 0.06f, 0.06f), white);
        // Leaves along the back
        for (int i = 0; i < (gogoat ? 9 : 6); i++)
        {
            float t = i / (gogoat ? 8f : 5f);
            var root = bc + V(0, br.Y * 0.8f, br.Z * (0.9f - 1.6f * t));
            foreach (float side in new[] { -1f, 1f })
                Frond(b, Body, root, root + V(0.05f * side, 0.04f, -0.03f) * k, 0.025f * k, leaf, V(side * 0.3f, 1f, 0), 0.25f, Leaf);
        }
        int tail = b.Tail(bc + V(0, 0.03f, -br.Z * 0.95f));
        Frond(b, tail, bc + V(0, 0.03f, -br.Z * 0.95f), bc + V(0, 0.08f, -br.Z * 1.3f), 0.025f * k, leaf, V(0, 1f, 0.2f), 0.25f, Leaf);
        int head = b.Head(bc + V(0, 0.05f, 0.12f) * k);
        var c = bc + V(0, 0.15f, 0.17f) * k;
        var r = V(0.045f, 0.045f, 0.055f) * k;
        b.Tube(head, Smooth(3, bc + V(0, 0.03f, 0.1f) * k, bc + V(0, 0.09f, 0.14f) * k, c + V(0, -0.02f, -0.02f) * k), 0.03f * k, 0.028f * k, coat, blend: 0f);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.015f, 0.045f) * k, V(0.025f, 0.022f, 0.03f) * k, white);
        b.Ell(head, c + V(0, -0.005f, 0.075f) * k, V(0.012f, 0.008f, 0.006f) * k, Rgb(50, 46, 50), blend: 0.004f);
        if (gogoat)
        {
            b.Spike(head, c + V(0, -0.04f, 0.03f) * k, c + V(0, -0.09f, 0.04f) * k, 0.02f * k, white, 0.6f);
            PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.025f * s, 0.035f, -0.01f) * k, c + V(0.06f * s, 0.09f, -0.06f) * k, c + V(0.07f * s, 0.06f, -0.13f) * k, c + V(0.06f * s, -0.01f, -0.12f) * k), 0.016f * k, 0.008f * k, Rgb(70, 60, 56), Shell, 0f));
        }
        else PokeBuilder.Both(s => b.Spike(head, c + V(0.02f * s, 0.035f, -0.01f) * k, c + V(0.035f * s, 0.07f, -0.04f) * k, 0.01f, Rgb(70, 60, 56), 0.7f, Shell));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.035f * s, 0.02f, -0.01f) * k);
            b.Ell(ear, c + V(0.06f * s, 0.015f, -0.01f) * k, V(0.025f, 0.01f, 0.012f) * k, coat, V(0, 0, -15f * s));
            var at = On(c, r, 0.03f * s * k, c.Y + 0.01f * k);
            b.Eye(head, at, Outward(c, r, at), 0.012f * k, Rgb(200, 60, 40), glare: gogoat);
        });
        return b;
    }

    private static PokeBuilder Skiddo() => GoatBuild(false);

    private static PokeBuilder Gogoat() => GoatBuild(true);

    // ------------------------------------------------------------------ Pancham and Pangoro

    /// <summary>
    /// Pancham and Pangoro: pandas that glower, a leaf in their mouths. Pancham is a little one, a cream head with black
    /// ears and patches round its eyes and a tuft on top, its body black; Pangoro is a great hulking one, a white mane
    /// round its head and a shaggy white chest, black arms with heavy claws and a coat of black fur falling behind.
    /// </summary>
    private static PokeBuilder PandaBuild(bool pangoro)
    {
        float k = pangoro ? 1f : 0.55f;
        var b = new PokeBuilder(pangoro ? "Pangoro" : "Pancham", pangoro ? 1f : 0.55f, BodyPlan.Biped, V(0, 0.34f * k, 0)) { Coat = Fur };
        var black = Rgb(60, 60, 70);
        var cream = Rgb(244, 236, 214);
        var leaf = Rgb(100, 180, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.2f, 0) * k);
            b.Limb(leg, V(0.09f * s, 0.21f, 0) * k, V(0.1f * s, 0.05f, 0.01f) * k, 0.06f * k, 0.05f * k, black);
            b.Ell(leg, V(0.105f * s, 0.035f, 0.04f) * k, V(0.05f, 0.035f, 0.07f) * k, black);
            var shoulder = V(0.15f * s, 0.46f, 0.02f) * k;
            int arm = b.Arm(s, shoulder);
            var hand = (pangoro ? V(0.27f * s, 0.36f, 0.12f) : V(0.2f * s, 0.32f, 0.06f)) * k;
            b.Limb(arm, shoulder, hand, 0.055f * k, 0.05f * k, black);
            b.Ell(arm, hand, V(0.05f, 0.05f, 0.05f) * k, black);
            if (pangoro) Claws(b, arm, hand + V(0, -0.02f, 0.04f) * k, V(0.02f, 0, 0) * k, V(0, -0.3f, 1f), 0.04f * k, 0.012f * k);
        });
        var bc = V(0, 0.34f, 0) * k;
        b.Ell(Body, bc, V(0.15f, 0.17f, 0.12f) * k, black);
        if (pangoro)
        {
            b.Ell(Body, bc + V(0, 0.06f, 0.05f), V(0.13f, 0.12f, 0.09f), cream, blend: 0.03f);
            FurTufts(b, Body, bc + V(0, 0.04f, 0.07f), V(0.12f, 0.1f, 0.06f), 14, 0.05f, 0.022f, cream, 1.1f, -1f, 0.9f);
            // A coat of black fur falling behind like coat-tails
            PokeBuilder.Both(s => Blade(b, Body, bc + V(0.06f * s, -0.04f, -0.08f), bc + V(0.16f * s, -0.3f, -0.18f), 0.08f, black, V(0, 0, -1f), 0.25f));
        }
        else b.PaintEll(Body, bc + V(0, 0.02f, 0.11f) * k, V(0.09f, 0.1f, 0.05f) * k, Rgb(80, 80, 92));
        int head = b.Head(bc + V(0, 0.15f, 0.02f) * k);
        var c = bc + V(0, pangoro ? 0.26f : 0.3f, 0.04f) * k;
        var r = (pangoro ? V(0.1f, 0.09f, 0.09f) : V(0.15f, 0.14f, 0.13f)) * k;
        b.Ell(head, c, r, cream);
        if (pangoro)
            for (int i = 0; i < 12; i++)
            {
                float a = (i / 11f - 0.5f) * 260f * Degree;
                var d = V(MathF.Sin(a), MathF.Cos(a), -0.4f);
                b.Spike(head, c + d * 0.07f, c + Vector3.Normalize(d) * 0.16f, 0.025f, cream, 0.6f);
            }
        else foreach (var (x, len) in new[] { (-0.02f, 0.05f), (0.0f, 0.07f), (0.02f, 0.05f) })
                b.Spike(head, c + V(x, r.Y * 0.9f, 0), c + V(x * 1.5f, r.Y * 0.9f + len, -0.01f), 0.014f, cream, 0.6f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.6f * r.X * s, 0.7f * r.Y, -0.01f));
            b.Ell(ear, c + V(0.75f * r.X * s, 0.8f * r.Y, -0.01f), r * 0.32f, black);
            var at = On(c, r, 0.4f * r.X * s, c.Y + 0.08f * r.Y);
            b.PaintEll(head, at, r * 0.32f, black, V(0, 0, 25f * s));
            b.Eye(head, at, Outward(c, r, at), 0.17f * r.X, Rgb(40, 40, 50), sclera: true, glare: true);
        });
        b.Ell(head, c + V(0, -0.2f * r.Y, r.Z * 0.95f), r * 0.12f, Rgb(50, 46, 50), blend: 0.006f);
        // The leaf in its mouth
        var mouth = c + V(0.3f * r.X, -0.45f * r.Y, r.Z * 0.8f);
        b.Tube(head, Smooth(3, mouth, mouth + V(0.03f, 0.0f, 0.03f) * k * 2f, mouth + V(0.06f, -0.01f, 0.04f) * k * 2f), 0.004f, 0.004f, Rgb(80, 130, 60), blend: 0f);
        Petal(b, head, mouth + V(0.06f, -0.01f, 0.04f) * k * 2f, mouth + V(0.1f, -0.04f, 0.06f) * k * 2f, 0.018f * k * 2f, leaf, Leaf);
        return b;
    }

    private static PokeBuilder Pancham() => PandaBuild(false);

    private static PokeBuilder Pangoro() => PandaBuild(true);

    // ------------------------------------------------------------------ Furfrou

    /// <summary>
    /// Furfrou, natural or in one of its trims: a slender poodle with dark skin, a long dark muzzle and dark legs, its white
    /// fur grown shaggy all over, or clipped and styled with a trim's colour: a heart, a star or a diamond on its brow and
    /// tail, a Debutante's sun hat, a Matron's bonnet, a Dandy's top hat, La Reine's ribbons, a Kabuki mask or a Pharaoh's
    /// striped headdress, with puffs or cuffs on its legs.
    /// </summary>
    private static PokeBuilder FurfrouBuild(string? trim)
    {
        var b = new PokeBuilder(trim == null ? "Furfrou" : "Furfrou-" + trim, 0.9f, BodyPlan.Quadruped, V(0, 0.3f, 0)) { Coat = Fur };
        var skin = Rgb(80, 72, 70);
        var fur = Rgb(244, 242, 236);
        var colour = trim switch
        {
            "Heart" => Rgb(240, 120, 170), "Star" => Rgb(70, 200, 230), "Diamond" => Rgb(240, 150, 40),
            "Debutante" => Rgb(246, 222, 140), "Matron" => Rgb(230, 140, 180), "Dandy" => Rgb(120, 200, 60),
            "La-Reine" => Rgb(120, 190, 240), "Kabuki" => Rgb(220, 60, 40), "Pharaoh" => Rgb(50, 110, 210),
            _ => fur
        };
        BeastLegs(b, 0.06f, 0.26f, 0.13f, -0.15f, 0.03f, skin, skin);
        var bc = V(0, 0.3f, 0);
        var br = V(0.08f, 0.08f, 0.17f);
        b.Ell(Body, bc, br, skin);
        // White fur over the body: shaggy, or clipped to a smooth coat
        var cc = bc + V(0, 0.01f, 0.04f);
        var cr = V(0.095f, 0.095f, 0.15f);
        b.Ell(Body, cc, cr, fur, blend: 0.02f);
        if (trim == null) FurTufts(b, Body, cc, cr, 34, 0.04f, 0.022f, fur, 1.1f, -0.8f, 0.8f);
        // Puffs or cuffs on the legs, in the trim's colour
        foreach (var z in new[] { 0.13f, -0.15f })
            PokeBuilder.Both(s =>
            {
                var at = V(0.063f * s, 0.09f, z + 0.01f);
                if (trim is "Kabuki" or "La-Reine" or "Pharaoh" or "Dandy")
                    b.Torus(Root, at, 0.032f, 0.012f, colour, blend: 0.006f);
                else b.Ell(Root, at, V(0.04f, 0.04f, 0.04f), trim == null ? fur : colour, blend: 0.02f);
            });
        // The tail: a tuft, or the trim's shape
        int tail = b.Tail(bc + V(0, 0.04f, -br.Z * 0.75f));
        var tp = Smooth(3, bc + V(0, 0.04f, -br.Z * 0.75f), bc + V(0, 0.12f, -br.Z * 1.15f), bc + V(0, 0.18f, -br.Z * 1.1f));
        b.Tube(tail, tp, 0.012f, 0.01f, skin, blend: 0f);
        var tip = tp[^1] + V(0, 0.03f, 0);
        switch (trim)
        {
            case "Heart":
                PokeBuilder.Both(s => b.Ell(tail, tip + V(0.018f * s, 0.01f, 0), V(0.028f, 0.028f, 0.015f), colour, blend: 0.01f));
                b.Spike(tail, tip + V(0, 0.0f, 0), tip + V(0, -0.045f, 0), 0.03f, colour, 0.5f);
                break;
            case "Star":
                StarPoints(b, tail, tip, 0.05f, 0.022f, 0f, 0.5f, colour);
                b.Ell(tail, tip, V(0.025f, 0.025f, 0.015f), colour, blend: 0.01f);
                break;
            case "Diamond":
                b.Box(tail, tip, V(0.03f, 0.03f, 0.012f), 0.008f, colour, V(0, 0, 45f), Fur, 0.01f);
                break;
            case "Kabuki":
                foreach (float a in new[] { -40f, 0f, 40f })
                    Blade(b, tail, tip - V(0, 0.02f, 0), tip + V(MathF.Sin(a * Degree) * 0.06f, 0.04f, 0), 0.025f, colour, V(0, 0, 1f), 0.3f);
                break;
            case "Dandy":
                b.Limb(tail, tip - V(0, 0.02f, 0), tip + V(0, 0.05f, 0), 0.022f, 0.022f, colour);
                break;
            default:
                b.Ell(tail, tip, V(0.035f, 0.04f, 0.035f), trim == null ? fur : colour, blend: 0.01f);
                break;
        }
        // The head: a long dark muzzle, the fur on top styled by the trim
        int head = b.Head(bc + V(0, 0.08f, 0.14f));
        var c = bc + V(0, 0.2f, 0.2f);
        var r = V(0.045f, 0.045f, 0.05f);
        b.Tube(head, Smooth(3, bc + V(0, 0.07f, 0.12f), bc + V(0, 0.14f, 0.17f), c + V(0, -0.02f, -0.02f)), 0.03f, 0.028f, fur, blend: 0f);
        b.Ell(head, c, r, skin);
        b.Ell(head, c + V(0, -0.02f, 0.07f), V(0.022f, 0.02f, 0.05f), skin);
        b.Ell(head, c + V(0, -0.015f, 0.12f), V(0.01f, 0.008f, 0.006f), Rgb(40, 36, 40), blend: 0.004f);
        var crown = c + V(0, 0.04f, -0.01f);
        switch (trim)
        {
            case null:
                b.Ell(head, crown, V(0.06f, 0.05f, 0.06f), fur, blend: 0.02f);
                FurTufts(b, head, crown, V(0.06f, 0.05f, 0.06f), 16, 0.03f, 0.018f, fur, 1.1f, -1f, 0.6f);
                break;
            case "Debutante":
                b.Ell(head, crown, V(0.055f, 0.04f, 0.055f), fur, blend: 0.02f);
                b.Ell(head, crown + V(0, 0.03f, 0), V(0.12f, 0.012f, 0.12f), colour, mat: Fur, blend: 0.01f);
                b.Ell(head, crown + V(0, 0.05f, 0), V(0.045f, 0.03f, 0.045f), colour, blend: 0.01f);
                break;
            case "Matron":
                b.Ell(head, crown + V(0, 0.01f, -0.01f), V(0.065f, 0.05f, 0.06f), colour, blend: 0.02f);
                b.PaintEll(Body, cc + V(0, -0.06f, 0), V(0.11f, 0.05f, 0.16f), colour);
                break;
            case "Dandy":
                b.Ell(head, crown, V(0.05f, 0.03f, 0.05f), fur, blend: 0.02f);
                b.Limb(head, crown + V(0, 0.02f, 0), crown + V(0, 0.1f, 0), 0.04f, 0.04f, fur);
                b.Ell(head, crown + V(0, 0.02f, 0), V(0.07f, 0.01f, 0.07f), fur, blend: 0.006f);
                b.PaintTorus(head, crown + V(0, 0.04f, 0), 0.04f, 0.008f, colour);
                break;
            case "La-Reine":
                b.Ell(head, crown, V(0.055f, 0.045f, 0.055f), fur, blend: 0.02f);
                PokeBuilder.Both(s => b.PaintTorus(head, crown + V(0.04f * s, -0.02f, 0), 0.02f, 0.006f, colour, V(0, 0, 90f)));
                break;
            case "Kabuki":
                b.Ell(head, crown, V(0.06f, 0.05f, 0.06f), fur, blend: 0.02f);
                foreach (float x in new[] { -0.03f, 0f, 0.03f })
                    b.PaintEll(head, crown + V(x, 0.02f, 0.02f), V(0.008f, 0.05f, 0.05f), colour, soft: 0.005f);
                break;
            case "Pharaoh":
                b.Ell(head, crown, V(0.06f, 0.045f, 0.06f), fur, blend: 0.02f);
                PokeBuilder.Both(s => b.Ell(head, c + V(0.05f * s, -0.04f, -0.01f), V(0.022f, 0.06f, 0.025f), fur, blend: 0.015f));
                foreach (float y in new[] { 0.02f, -0.01f, -0.04f, -0.07f })
                    PokeBuilder.Both(s => b.PaintEll(head, c + V(0.05f * s, y, -0.01f), V(0.03f, 0.007f, 0.03f), colour, soft: 0.004f));
                break;
            default:
                b.Ell(head, crown, V(0.055f, 0.045f, 0.055f), fur, blend: 0.02f);
                var brow = crown + V(0, 0.01f, 0.05f);
                if (trim == "Heart") b.Mark(head, brow, V(0, 0.4f, 1f), 0.016f, 0.016f, colour, MarkShape.Disc);
                else if (trim == "Star") b.Mark(head, brow, V(0, 0.4f, 1f), 0.018f, 0.018f, colour, MarkShape.Star5);
                else b.Mark(head, brow, V(0, 0.4f, 1f), 0.015f, 0.018f, colour, MarkShape.Diamond);
                break;
        }
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.01f, -0.02f));
            b.Ell(ear, c + V(0.06f * s, -0.03f, -0.02f), V(0.018f, 0.04f, 0.018f), trim == "La-Reine" ? colour : fur);
            var at = On(c, r, 0.028f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(70, 160, 220), glare: true);
        });
        return b;
    }

    private static PokeBuilder Furfrou() => FurfrouBuild(null);

    // ------------------------------------------------------------------ Espurr and Meowstic

    /// <summary>Espurr: a little gray cat with a big round head, folded ears hiding what they hold, great violet eyes ringed in pink, a white collar and a small curled tail.</summary>
    private static PokeBuilder Espurr()
    {
        var b = new PokeBuilder("Espurr", 0.45f, BodyPlan.Biped, V(0, 0.08f, 0)) { Coat = Fur };
        var gray = Rgb(150, 150, 166);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.05f, 0));
            b.Limb(leg, V(0.025f * s, 0.055f, 0), V(0.028f * s, 0.016f, 0.01f), 0.014f, 0.012f, gray);
            b.Ell(leg, V(0.028f * s, 0.012f, 0.018f), V(0.014f, 0.012f, 0.02f), White);
        });
        b.Ell(Body, V(0, 0.08f, 0), V(0.04f, 0.045f, 0.035f), gray);
        b.Torus(Body, V(0, 0.115f, 0), 0.03f, 0.008f, White, blend: 0.006f);
        int tail = b.Tail(V(0, 0.06f, -0.03f));
        Curl(b, tail, V(0, 0.06f, -0.03f), V(0, 0.08f, -0.06f), V(0, 0, -1f), V(0, 1f, 0), 0.02f, 1f, 0.008f, White);
        int head = b.Head(V(0, 0.12f, 0.01f));
        var c = V(0, 0.18f, 0.015f);
        var r = V(0.07f, 0.065f, 0.06f);
        b.Ell(head, c, r, gray);
        b.Spike(head, c + V(0, 0.05f, 0), c + V(0.02f, 0.09f, -0.01f), 0.015f, gray, 0.6f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.03f, -0.01f));
            b.Ell(ear, c + V(0.075f * s, 0.01f, -0.005f), V(0.025f, 0.045f, 0.02f), gray, V(0, 0, 25f * s));
            var at = On(c, r, 0.03f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.024f, Rgb(160, 120, 220), sclera: true, pupil: Rgb(60, 40, 80));
        });
        return b;
    }

    /// <summary>
    /// Meowstic and its forms: a slender cat standing upright, its ears raised, two tails, a ruff at its neck. The male is
    /// navy marked with white, green-eyed; the female white marked with navy, red-eyed. Mega Meowstic floats with its
    /// ears drawn up over its head like a hood, an eye-ring at each tip, and its long white tail curling below.
    /// </summary>
    private static PokeBuilder MeowsticBuild(bool female, bool mega)
    {
        var name = mega ? (female ? "Meowstic-Female-Mega" : "Meowstic-Male-Mega") : female ? "Meowstic-Female" : "Meowstic";
        var b = new PokeBuilder(name, mega ? 0.9f : 0.7f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        if (mega) b.Hover();
        var navy = Rgb(50, 70, 150);
        var white = Rgb(240, 240, 246);
        var main = female ? white : navy;
        var mark = female ? navy : white;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.15f, 0));
            b.Limb(leg, V(0.025f * s, 0.16f, 0), V(0.03f * s, 0.02f, 0.01f), 0.015f, 0.012f, main);
            b.Ell(leg, V(0.031f * s, 0.014f, 0.02f), V(0.014f, 0.014f, 0.025f), mark);
            var shoulder = V(0.035f * s, 0.27f, 0.01f);
            int arm = b.Arm(s, shoulder);
            b.Limb(arm, shoulder, V(0.07f * s, 0.21f, 0.03f), 0.011f, 0.01f, main);
        });
        b.Ell(Body, V(0, 0.22f, 0), V(0.04f, 0.07f, 0.035f), main);
        b.Ell(Body, V(0, 0.29f, 0.005f), V(0.045f, 0.022f, 0.04f), white, blend: 0.012f);
        FurTufts(b, Body, V(0, 0.28f, 0.005f), V(0.045f, 0.02f, 0.04f), 10, 0.02f, 0.01f, white, 1.1f, -1f, 0.5f);
        // Two tails, banded at their ends
        int tail = b.Tail(V(0, 0.18f, -0.03f));
        PokeBuilder.Both(s =>
        {
            var tp = mega ? Smooth(3, V(0, 0.18f, -0.03f), V(0.03f * s, 0.1f, -0.06f), V(0.06f * s, 0.04f, -0.02f), V(0.05f * s, 0.08f, 0.03f))
                : Smooth(3, V(0, 0.18f, -0.03f), V(0.04f * s, 0.15f, -0.09f), V(0.09f * s, 0.2f, -0.12f), V(0.11f * s, 0.27f, -0.1f));
            b.Tube(tail, tp, 0.012f, 0.02f, mega ? white : main, blend: 0.006f);
            b.PaintEll(tail, tp[^1], V(0.03f, 0.03f, 0.03f), mark);
        });
        int head = b.Head(V(0, 0.3f, 0.01f));
        var c = V(0, 0.35f, 0.015f);
        var r = V(0.05f, 0.045f, 0.045f);
        b.Ell(head, c, r, main);
        b.PaintEll(head, c + V(0, 0.035f, 0.03f), V(0.015f, 0.025f, 0.02f), mark);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.035f * s, 0.03f, -0.01f));
            if (mega)
            {
                // The ears drawn up over its head like a hood, an eye-ring at each tip
                var path = Smooth(3, c + V(0.035f * s, 0.03f, -0.01f), c + V(0.04f * s, 0.1f, 0.0f), c + V(0.025f * s, 0.18f, 0.02f), c + V(0.015f * s, 0.24f, 0.05f));
                b.Tube(ear, path, 0.02f, 0.03f, navy, blend: 0f);
                b.PaintEll(ear, path[^3], V(0.03f, 0.03f, 0.03f), white);
                b.Mark(ear, path[^1] + V(0, 0, 0.028f), V(0, 0.2f, 1f), 0.016f, 0.016f, Rgb(250, 220, 70), MarkShape.Ring);
            }
            else
            {
                var tip = c + V(0.08f * s, 0.09f, -0.02f);
                b.Spike(ear, c + V(0.03f * s, 0.03f, -0.01f), tip, 0.03f, main, 0.45f);
                b.PaintEll(ear, Vector3.Lerp(c, tip, 0.65f) + V(0, 0, 0.01f), V(0.012f, 0.025f, 0.012f), mark, Euler(tip - c));
            }
            var at = On(c, r, 0.022f * s, c.Y);
            b.Eye(head, at, Outward(c, r, at), 0.013f, female ? Rgb(220, 70, 50) : Rgb(70, 190, 120), glare: !female);
        });
        return mega ? Lift(b) : b;
    }

    private static PokeBuilder Meowstic() => MeowsticBuild(false, false);

    // ------------------------------------------------------------------ Honedge line

    /// <summary>A haunted sword: a broad blade of dull bronze with a darker pattern down it, a crossguard with an eye in its middle, and a cloth trailing from the hilt like an arm.</summary>
    private static void HauntedSword(PokeBuilder b, int bone, Vector3 hilt, Vector3 down, Vector3 across, float k, Color cloth, Color iris, bool eye)
    {
        var bronze = Rgb(170, 150, 110);
        var dark = Rgb(110, 92, 70);
        var d = Vector3.Normalize(down);
        var u = Vector3.Normalize(across);
        var n = Vector3.Normalize(Vector3.Cross(d, u));
        if (n.Z < 0) n = -n;
        Blade(b, bone, hilt, hilt + d * 0.36f * k, 0.06f * k, bronze, n, 0.18f, Metal);
        b.PaintEll(bone, hilt + d * 0.16f * k, V(0.02f, 0.12f, 0.03f) * k, dark, Euler(d));
        b.Limb(bone, hilt - u * 0.07f * k, hilt + u * 0.07f * k, 0.022f * k, 0.022f * k, bronze, Metal);
        b.Ell(bone, hilt, V(0.035f, 0.035f, 0.02f) * k, bronze, Euler(d), Metal, 0.008f);
        b.Limb(bone, hilt, hilt - d * 0.08f * k, 0.016f * k, 0.014f * k, dark, Metal);
        var cloth0 = hilt - d * 0.08f * k;
        // The cloth arcs up over the pommel and hangs down beside the blade, frayed at its end
        var fall = Smooth(3, cloth0, cloth0 - d * 0.05f * k + u * 0.06f * k, cloth0 + d * 0.05f * k + u * 0.13f * k, cloth0 + d * 0.2f * k + u * 0.15f * k, cloth0 + d * 0.32f * k + u * 0.14f * k);
        Ribbon(b, bone, fall, 0.04f * k, cloth, n, 0.008f);
        foreach (float f in new[] { -0.012f, 0f, 0.012f })
            b.Spike(bone, fall[^1] + u * f * k, fall[^1] + (d * 0.05f + u * f * 1.5f) * k, 0.008f * k, cloth, 0.5f);
        if (eye) b.Eye(bone, hilt + n * 0.02f * k, n, 0.014f * k, iris, sclera: true);
    }

    /// <summary>Honedge: a sword haunted by a spirit, its one eye on the crossguard, its blue cloth trailing from the hilt like an arm.</summary>
    private static PokeBuilder Honedge()
    {
        var b = new PokeBuilder("Honedge", 0.75f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Metal }.Hover();
        HauntedSword(b, Body, V(0, 0.42f, 0.0f), V(0, -1f, 0.0f), V(1f, 0, 0), 1f, Rgb(60, 120, 200), Rgb(70, 160, 220), true);
        return Lift(b);
    }

    /// <summary>Doublade: two haunted swords crossed, an eye on each crossguard, their pink cloths joined between them.</summary>
    private static PokeBuilder Doublade()
    {
        var b = new PokeBuilder("Doublade", 0.85f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Metal }.Hover();
        PokeBuilder.Both(s =>
        {
            float a = 25f * Degree;
            var down = V(-MathF.Sin(a) * s, -MathF.Cos(a), 0);
            HauntedSword(b, Body, V(0.08f * s, 0.42f, 0.01f * s), down, V(MathF.Cos(a) * s, -MathF.Sin(a), 0), 0.9f, Rgb(200, 80, 150), Rgb(200, 90, 160), true);
        });
        return Lift(b);
    }

    /// <summary>
    /// Aegislash, in its Shield or its Blade Forme: a royal sword with a gold hilt and a purple eye on its crossguard,
    /// and a great round shield of gold and bronze marked with three rounds, two black cloths for arms. Shielded, the
    /// sword stands behind the shield, its arms round it; drawn, the sword stands forward and the shield is held aside.
    /// </summary>
    private static PokeBuilder AegislashBuild(bool blade)
    {
        var b = new PokeBuilder(blade ? "Aegislash-Blade" : "Aegislash", 1f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Metal }.Hover();
        var gold = Rgb(220, 186, 80);
        var steel = Rgb(236, 232, 210);
        var bronze = Rgb(150, 120, 80);
        var black = Rgb(50, 46, 54);
        var hilt = V(0, blade ? 0.68f : 0.66f, blade ? 0.03f : -0.03f);
        // The sword: a long pale blade, the gold hilt above with its crossguard and eye
        Blade(b, Body, hilt, hilt + V(0, -0.6f, 0), 0.07f, steel, V(0, 0, 1f), 0.24f, Metal);
        b.Limb(Body, hilt - V(0.1f, 0, 0), hilt + V(0.1f, 0, 0), 0.025f, 0.025f, gold, Metal);
        b.Ell(Body, hilt, V(0.045f, 0.06f, 0.025f), gold, mat: Metal, blend: 0.008f);
        b.Spike(Body, hilt + V(0, 0.04f, 0), hilt + V(0, 0.2f, 0), 0.035f, gold, 0.4f, Metal);
        PokeBuilder.Both(s => b.Spike(Body, hilt + V(0.09f * s, 0, 0), hilt + V(0.15f * s, 0.06f, 0), 0.02f, gold, 0.5f, Metal));
        b.Eye(Body, hilt + V(0, 0, 0.025f), V(0, 0, 1f), 0.018f, Rgb(150, 80, 180), sclera: true);
        // Two black cloths for arms
        PokeBuilder.Both(s =>
        {
            var from = hilt + V(0.09f * s, -0.02f, 0);
            var path = blade
                ? Smooth(3, from, from + V(0.08f * s, -0.04f, 0), from + V(0.12f * s, -0.14f, 0.02f), from + V(0.1f * s, -0.24f, 0.04f))
                : Smooth(3, from, from + V(0.1f * s, -0.02f, 0.04f), from + V(0.12f * s, -0.1f, 0.09f), from + V(0.03f * s, -0.2f, 0.15f));
            Ribbon(b, Body, path, 0.035f, black, V(0, 0, 1f), 0.008f);
            if (blade && s < 0) b.PaintEll(Body, path[^1], V(0.05f, 0.05f, 0.05f), Rgb(170, 140, 200));
        });
        // The shield: held in front, or aside on the right arm when the sword is drawn
        var sc = blade ? V(0.24f, 0.36f, 0.06f) : V(0, 0.36f, 0.12f);
        var sr = blade ? 0.11f : 0.16f;
        b.Ell(Body, sc, V(sr, sr * (blade ? 1f : 1.25f), 0.03f), gold, mat: Metal, blend: 0.01f);
        b.PaintEll(Body, sc + V(0, 0, 0.02f), V(sr * 0.75f, sr * (blade ? 0.75f : 0.95f), 0.03f), bronze);
        foreach (float a in new[] { 90f, 210f, 330f })
            b.Torus(Body, sc + V(MathF.Cos(a * Degree), MathF.Sin(a * Degree), 0) * sr * 0.35f + V(0, 0, 0.025f), sr * 0.18f, sr * 0.05f, Rgb(60, 50, 40), V(90f, 0, 0), mat: Metal, blend: 0.004f);
        if (blade) b.Limb(Body, sc + V(-0.06f, 0.05f, -0.02f), V(0.1f, 0.48f, 0.03f), 0.015f, 0.015f, black);
        return Lift(b);
    }

    private static PokeBuilder Aegislash() => AegislashBuild(false);

    // ------------------------------------------------------------------ Spritzee and Aromatisse

    /// <summary>Spritzee: a small round pink bird of scent wearing a white mask with a long curved beak, an orange-red eye ringed in black, feathers sweeping up over its head and a tail of feathers.</summary>
    private static PokeBuilder Spritzee()
    {
        var b = new PokeBuilder("Spritzee", 0.45f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Fur }.Hover();
        var pink = Rgb(236, 140, 160);
        var dark = Rgb(200, 90, 120);
        var c = V(0, 0.2f, 0);
        var r = V(0.075f, 0.075f, 0.07f);
        b.Ell(Body, c, r, pink);
        Blade(b, Body, c + V(0, 0.05f, -0.03f), c + V(0.02f, 0.14f, -0.08f), 0.05f, pink, V(1f, 0, 0), 0.25f);
        Curl(b, Body, c + V(0.02f, 0.13f, -0.08f), c + V(0.02f, 0.1f, -0.1f), V(0, 1f, 0), V(0, 0, -1f), 0.03f, 1f, 0.01f, pink);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.Spike(Body, c + V(x, -0.05f, -0.03f), c + V(x * 1.5f, -0.12f, -0.03f), 0.016f, dark, 0.5f);
        // The white mask with its long curved beak
        b.Ell(Body, c + V(0, 0.0f, 0.05f), V(0.06f, 0.05f, 0.03f), White, blend: 0.01f);
        b.Tube(Body, Smooth(3, c + V(0, 0.0f, 0.07f), c + V(0, -0.01f, 0.12f), c + V(0, -0.04f, 0.15f)), 0.03f, 0.008f, White, blend: 0f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, c.Y + 0.02f);
            b.Eye(Body, at, Outward(c, r, at), 0.016f, Rgb(232, 90, 40), glare: true);
        });
        return Lift(b);
    }

    /// <summary>Aromatisse: a lady of perfume, a skirt of pink down below a purple body, a white face like a mask with red eyes ringed in black, purple horns curling up edged in gold and small purple arms.</summary>
    private static PokeBuilder Aromatisse()
    {
        var b = new PokeBuilder("Aromatisse", 0.8f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var pink = Rgb(236, 150, 170);
        var purple = Rgb(160, 90, 170);
        var gold = Rgb(246, 210, 90);
        // A skirt of pink down to the ground
        var sc = V(0, 0.12f, 0);
        b.Ell(Body, sc, V(0.12f, 0.11f, 0.11f), pink);
        FurTufts(b, Body, sc, V(0.12f, 0.11f, 0.11f), 24, 0.04f, 0.022f, pink, 1.1f, -0.7f, 0.7f);
        b.Ell(Body, V(0, 0.25f, 0), V(0.055f, 0.06f, 0.05f), purple, blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.045f * s, 0.27f, 0.02f));
            b.Limb(arm, V(0.045f * s, 0.27f, 0.02f), V(0.08f * s, 0.2f, 0.06f), 0.012f, 0.01f, purple);
        });
        int head = b.Head(V(0, 0.29f, 0.01f));
        var c = V(0, 0.34f, 0.02f);
        var r = V(0.06f, 0.055f, 0.055f);
        b.Ell(head, c, r, purple);
        b.PaintEll(head, c + V(0, -0.01f, 0.04f), V(0.045f, 0.03f, 0.03f), White);
        PokeBuilder.Both(s =>
        {
            var horn = Smooth(3, c + V(0.035f * s, 0.035f, -0.01f), c + V(0.08f * s, 0.09f, -0.02f), c + V(0.06f * s, 0.15f, -0.02f));
            b.Tube(head, horn, 0.018f, 0.006f, purple, blend: 0f);
            Curl(b, head, horn[^1], horn[^1] + V(-0.015f * s, -0.01f, 0), V(s, 0, 0), V(0, 1f, 0), 0.015f, 1f, 0.006f, gold);
            var at = On(c, r, 0.022f * s, c.Y - 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(220, 60, 40), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Swirlix and Slurpuff

    /// <summary>Swirlix: a puff of white candy floss tinged pink, a small face with pink eyes and a smile, little arms and a curling tail.</summary>
    private static PokeBuilder Swirlix()
    {
        var b = new PokeBuilder("Swirlix", 0.45f, BodyPlan.Floating, V(0, 0.1f, 0)) { Coat = Fur };
        var white = Rgb(250, 246, 248);
        var pink = Rgb(240, 190, 210);
        var c = V(0, 0.1f, 0);
        var r = V(0.07f, 0.065f, 0.06f);
        b.Ell(Body, c, r, white);
        foreach (var (x, y, z, k) in new[] { (-0.05f, 0.04f, 0f, 0.04f), (0.05f, 0.04f, 0f, 0.04f), (0f, 0.065f, -0.01f, 0.045f), (-0.06f, -0.01f, -0.01f, 0.035f), (0.06f, -0.01f, -0.01f, 0.035f), (0f, 0.03f, -0.05f, 0.045f) })
            b.Ell(Body, c + V(x, y, z), V(k, k, k), white, blend: 0.02f);
        b.PaintEll(Body, c + V(0.03f, 0.05f, -0.02f), V(0.03f, 0.025f, 0.03f), pink);
        b.PaintEll(Body, c + V(-0.05f, -0.02f, -0.02f), V(0.025f, 0.02f, 0.03f), pink);
        PokeBuilder.Both(s => b.Ell(Body, c + V(0.06f * s, -0.03f, 0.03f), V(0.014f, 0.012f, 0.012f), white, blend: 0.008f));
        int tail = b.Tail(c + V(0, -0.03f, -0.05f));
        Curl(b, tail, c + V(0, -0.03f, -0.05f), c + V(0, 0.0f, -0.09f), V(0, 0, -1f), V(0, 1f, 0), 0.025f, 1.2f, 0.008f, pink);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y + 0.008f);
            b.Eye(Body, at, Outward(c, r, at), 0.012f, Rgb(220, 90, 140), sclera: true);
        });
        var m = On(c, r, 0, c.Y - 0.022f);
        b.Mark(Body, m, Outward(c, r, m), 0.012f, 0.007f, Rgb(200, 80, 110), MarkShape.Smile);
        return b;
    }

    /// <summary>Slurpuff: a round confection, pink and white cream in rounds about its head, a red cherry on top, a broad white face with pink-rimmed eyes and a wide open mouth with its tongue out, and stubby arms.</summary>
    private static PokeBuilder Slurpuff()
    {
        var b = new PokeBuilder("Slurpuff", 0.75f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Fur };
        var white = Rgb(250, 246, 248);
        var pink = Rgb(240, 180, 200);
        var deep = Rgb(220, 100, 140);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.06f, 0));
            b.Limb(leg, V(0.04f * s, 0.07f, 0), V(0.045f * s, 0.02f, 0.01f), 0.025f, 0.022f, white);
            b.Ell(leg, V(0.046f * s, 0.018f, 0.02f), V(0.024f, 0.018f, 0.03f), pink);
            int arm = b.Arm(s, V(0.1f * s, 0.18f, 0.02f));
            b.Limb(arm, V(0.1f * s, 0.18f, 0.02f), V(0.15f * s, 0.15f, 0.05f), 0.02f, 0.018f, white);
        });
        var c = V(0, 0.2f, 0);
        var r = V(0.11f, 0.12f, 0.1f);
        b.Ell(Body, c, r, white);
        PokeBuilder.Both(s =>
        {
            foreach (var (y, k) in new[] { (0.06f, 0.055f), (-0.02f, 0.05f) })
                b.Ell(Body, c + V(0.1f * s, y, -0.01f), V(k, k, k), pink, blend: 0.02f);
        });
        b.Ell(Body, c + V(0, 0.11f, -0.01f), V(0.08f, 0.05f, 0.07f), pink, blend: 0.02f);
        b.Ell(Body, c + V(0, 0.18f, -0.01f), V(0.035f, 0.035f, 0.035f), Rgb(210, 60, 80), mat: Shell, blend: 0.01f);
        b.PaintEll(Body, c + V(0, -0.1f, 0), V(0.12f, 0.03f, 0.11f), pink);
        Grin(b, Body, c + V(0, -0.035f, 0.09f), V(0.045f, 0.022f, 0.03f), Rgb(170, 60, 90));
        b.Ell(Body, c + V(0, -0.05f, 0.09f), V(0.025f, 0.012f, 0.02f), deep, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, c.Y + 0.03f);
            b.Eye(Body, at, Outward(c, r, at), 0.014f, deep, sclera: true);
        });
        return b;
    }
}
