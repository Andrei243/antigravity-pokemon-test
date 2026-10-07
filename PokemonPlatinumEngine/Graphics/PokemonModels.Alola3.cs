using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Alola's third and last batch in National
// Pokédex order: Tapu Koko (785) to Melmetal (809). Their forms are in PokemonModels.Regional.cs,
// PokemonModels.Megas.cs and PokemonModels.Gigantamax.cs with the other forms. Helpers shared with the earlier batches
// are in the files of those batches, PokemonModels.Sinnoh1.cs to PokemonModels.Alola2.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ The guardians

    /// <summary>
    /// The small dark figure each of the four guardians keeps inside its shell: a black body and head with eyes of
    /// <paramref name="eye"/>, thin arms (on bones of their own, which carry the shell's halves) and, below, whatever the
    /// guardian has instead of legs. Returns the head's bone, centre and radii.
    /// </summary>
    private static (int Head, Vector3 C, Vector3 R) TapuFigure(PokeBuilder b, Vector3 bc, Color body, Color face, Color eye)
    {
        b.Ell(Body, bc, V(0.045f, 0.07f, 0.04f), body);
        int head = b.Head(bc + V(0, 0.06f, 0));
        var c = bc + V(0, 0.11f, 0.01f);
        var r = V(0.04f, 0.042f, 0.038f);
        b.Limb(head, bc + V(0, 0.05f, 0), c, 0.02f, 0.02f, body);
        b.Ell(head, c, r, face);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.016f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, eye, glare: true);
        });
        return (head, c, r);
    }

    /// <summary>Tapu Koko: the guardian of lightning, a small black figure in a shell whose halves it holds out like the heads of two cockerels, olive gold marked with zigzags, dark beaks and claws, yellow crests; orange feathers crown its head and fall below it.</summary>
    private static PokeBuilder TapuKoko()
    {
        var b = new PokeBuilder("Tapu Koko", 0.85f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Shell }.Hover();
        var black = Rgb(48, 46, 52);
        var olive = Rgb(196, 196, 72);
        var orange = Rgb(244, 140, 50);
        var yellow = Rgb(250, 220, 80);
        var bc = V(0, 0.4f, 0.02f);
        var (head, c, _) = TapuFigure(b, bc, black, black, Rgb(90, 200, 220));
        b.PaintEll(head, c + V(0, 0.01f, 0.03f), V(0.03f, 0.008f, 0.02f), orange);
        // The orange crest and the feathers falling below
        for (int i = 0; i < 5; i++)
        {
            float x = (i - 2) * 0.012f;
            Frond(b, head, c + V(x, 0.03f, -0.01f), c + V(x * 3f, 0.13f - MathF.Abs(x) * 1.5f, -0.06f), 0.018f, orange, V(0, 0.3f, 1f), 0.25f);
        }
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * 0.02f;
            Frond(b, Body, bc + V(x, -0.04f, 0), bc + V(x * 2.2f, -0.2f - (1.5f - MathF.Abs(i - 1.5f)) * 0.03f, 0.02f), 0.02f, orange, V(0, 0, 1f), 0.25f);
        }
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.035f * s, 0.04f, 0);
            int arm = b.Arm(s, shoulder);
            var sc = bc + V(0.15f * s, 0.02f, -0.01f);
            var sr = V(0.055f, 0.17f, 0.11f);
            b.Limb(arm, shoulder, sc + V(-0.04f * s, 0, 0.02f), 0.012f, 0.012f, black);
            // A half of the shell, held out like the head of a cockerel
            b.Ell(arm, sc, sr, olive);
            b.Spike(arm, sc + V(-0.02f * s, 0.09f, 0.06f), sc + V(-0.09f * s, 0.05f, 0.12f), 0.04f, black, 0.5f);
            b.Spike(arm, sc + V(0, -0.14f, 0.02f), sc + V(-0.02f * s, -0.26f, 0.05f), 0.03f, black, 0.5f);
            foreach (var (dx, h) in new[] { (-0.01f, 0.1f), (0.01f, 0.08f) })
                Frond(b, arm, sc + V(dx * s, 0.15f, -0.02f), sc + V((dx + 0.02f) * s, 0.15f + h, -0.06f), 0.016f, yellow, V(s, 0, 0.2f), 0.25f);
            var at = Out(sc, sr, default, V(s, 0, 0.1f));
            b.Mark(arm, at, Outward(sc, sr, at), 0.04f, 0.012f, White, MarkShape.Zigzag, 90f);
        });
        return Lift(b);
    }

    /// <summary>Tapu Lele: the guardian of the mind, a small black figure sitting in the pink bud of its shell, marked with dark diamonds, its head under a pink hood banded white and two long pink locks curling at their ends.</summary>
    private static PokeBuilder TapuLele()
    {
        var b = new PokeBuilder("Tapu Lele", 0.8f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var pink = Rgb(240, 150, 190);
        var line = Rgb(130, 70, 90);
        var black = Rgb(48, 46, 52);
        var sc = V(0, 0.18f, 0);
        var sr = V(0.15f, 0.13f, 0.14f);
        b.Ell(Body, sc, sr, pink);
        foreach (var d in new[] { V(0.75f, 0, 0.6f), V(-0.75f, 0, 0.6f), V(0, -0.2f, 1f) })
        {
            var at = Out(sc, sr, default, d);
            b.Mark(Body, at, Outward(sc, sr, at), 0.03f, 0.045f, line, MarkShape.Diamond);
        }
        var bc = V(0, 0.33f, 0.02f);
        var (head, c, _) = TapuFigure(b, bc, black, black, Rgb(70, 210, 200));
        // The hood and its white band, and the long locks falling from under it
        b.Spike(head, c + V(0, 0.03f, -0.005f), c + V(0, 0.15f, -0.03f), 0.052f, pink, 1f);
        b.Torus(head, c + V(0, 0.035f, -0.005f), 0.045f, 0.008f, White, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            var lock_ = Smooth(3, c + V(0.035f * s, 0.02f, 0), c + V(0.07f * s, -0.06f, 0.02f), c + V(0.08f * s, -0.16f, 0.03f), c + V(0.06f * s, -0.2f, 0.05f));
            b.Tube(head, lock_, 0.016f, 0.012f, pink, blend: 0f);
            b.Torus(head, lock_[^1] + V(0.012f * s, 0, 0), 0.014f, 0.007f, pink, V(0, 0, 90f), blend: 0.003f);
            int arm = b.Arm(s, bc + V(0.03f * s, 0.04f, 0));
            b.Limb(arm, bc + V(0.03f * s, 0.04f, 0), bc + V(0.06f * s, -0.02f, 0.05f), 0.01f, 0.009f, black);
            b.Torus(arm, bc + V(0.055f * s, -0.01f, 0.045f), 0.014f, 0.005f, pink, V(0, 0, 60f * s), blend: 0.003f);
        });
        return Lift(b);
    }

    /// <summary>Tapu Bulu: the guardian of growth, a small black figure under the great red plate of its shell, like a bull's brow marked with zigzags and crowned with two dark horns tipped yellow; red cuffs on its arms and a yellow bell hanging below it.</summary>
    private static PokeBuilder TapuBulu()
    {
        var b = new PokeBuilder("Tapu Bulu", 0.9f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Shell }.Hover();
        var red = Rgb(214, 60, 56);
        var black = Rgb(48, 46, 52);
        var yellow = Rgb(230, 200, 90);
        var bc = V(0, 0.38f, 0.02f);
        var (head, c, _) = TapuFigure(b, bc, black, black, Rgb(240, 200, 80));
        b.PaintEll(Body, bc + V(0, -0.03f, 0), V(0.05f, 0.014f, 0.045f), red);
        PokeBuilder.Both(s => b.Limb(Body, bc + V(0.025f * s, -0.05f, 0), bc + V(0.04f * s, -0.12f, 0.02f), 0.02f, 0.016f, black));
        // The bell
        b.Limb(Body, bc + V(0, -0.06f, 0), V(0, 0.2f, 0.01f), 0.008f, 0.008f, yellow);
        b.Ell(Body, V(0, 0.15f, 0.01f), V(0.04f, 0.05f, 0.04f), yellow, mat: Metal);
        b.PaintEll(Body, V(0, 0.14f, 0.01f), V(0.045f, 0.01f, 0.045f), Rgb(80, 60, 30));
        b.Spike(Body, V(0, 0.11f, 0.01f), V(0, 0.07f, 0.01f), 0.012f, Rgb(80, 60, 30), mat: Metal);
        // The shell: a red plate over the head, its ends bent down, marked white, two horns standing from it
        b.Box(head, c + V(0, 0.07f, -0.01f), V(0.12f, 0.045f, 0.07f), 0.015f, red);
        PokeBuilder.Both(s =>
        {
            b.Box(head, c + V(0.16f * s, 0.05f, -0.01f), V(0.06f, 0.04f, 0.07f), 0.015f, red, V(0, 0, -20f * s));
            b.Mark(head, c + V(0.06f * s, 0.07f, 0.06f), V(0, 0, 1f), 0.045f, 0.012f, White, MarkShape.Zigzag);
            b.Spike(head, c + V(0.05f * s, 0.1f, -0.01f), c + V(0.08f * s, 0.22f, -0.02f), 0.022f, black, mat: Shell);
            b.PaintEll(head, c + V(0.08f * s, 0.21f, -0.02f), V(0.014f, 0.025f, 0.014f), yellow);
            var shoulder = bc + V(0.04f * s, 0.05f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.05f * s, -0.06f, 0.03f);
            var hand = shoulder + V(0.06f * s, -0.12f, 0.06f);
            b.Limb(arm, shoulder, elbow, 0.016f, 0.015f, black);
            b.Limb(arm, elbow, hand, 0.015f, 0.014f, black);
            b.Ell(arm, elbow, V(0.03f, 0.028f, 0.03f), red);
            b.Ell(arm, hand, V(0.022f, 0.022f, 0.022f), black);
            b.Torus(arm, Vector3.Lerp(elbow, hand, 0.75f), 0.018f, 0.005f, yellow, V(0, 0, 40f * s), mat: Metal, blend: 0.003f);
        });
        return Lift(b);
    }

    /// <summary>Tapu Fini: the guardian of water, a slender dark figure with a pale face, frills of pale blue fins at its waist and arms, under a purple hood of shell drawn back to a point, the other half of its shell curving below it like a crescent.</summary>
    private static PokeBuilder TapuFini()
    {
        var b = new PokeBuilder("Tapu Fini", 0.85f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Scales }.Hover();
        var purple = Rgb(120, 90, 170);
        var navy = Rgb(56, 60, 90);
        var face = Rgb(170, 180, 230);
        var fin = Rgb(200, 220, 250);
        var bc = V(0, 0.36f, 0.03f);
        var (head, c, _) = TapuFigure(b, bc, navy, face, Rgb(60, 200, 200));
        b.Limb(Body, bc + V(0, -0.05f, 0), bc + V(0, -0.2f, 0.02f), 0.032f, 0.01f, navy);
        PokeBuilder.Both(s =>
        {
            Frond(b, Body, bc + V(0.025f * s, -0.04f, 0), bc + V(0.1f * s, -0.11f, 0.02f), 0.03f, fin, V(0, 0.2f, 1f), 0.2f);
            Frond(b, Body, bc + V(0.02f * s, -0.07f, 0.01f), bc + V(0.07f * s, -0.18f, 0.03f), 0.025f, fin, V(0, 0, 1f), 0.2f);
            var shoulder = bc + V(0.035f * s, 0.04f, 0);
            int arm = b.Arm(s, shoulder);
            var hand = shoulder + V(0.09f * s, -0.07f, 0.05f);
            b.Limb(arm, shoulder, hand, 0.012f, 0.01f, navy);
            Frond(b, arm, Vector3.Lerp(shoulder, hand, 0.5f), Vector3.Lerp(shoulder, hand, 0.5f) + V(0.04f * s, -0.05f, -0.02f), 0.018f, fin, V(0, 0.3f, 1f), 0.2f);
            foreach (float t in new[] { -1f, 1f })
                b.Spike(arm, hand, hand + V(0.02f * s, -0.02f, 0.01f + 0.01f * t), 0.006f, Rgb(40, 40, 60), mat: Shell);
        });
        // The hood, drawn back to a point, and the crescent of shell below
        b.Tube(head, Smooth(3, c + V(0, 0.02f, -0.005f), c + V(0, 0.09f, -0.025f), c + V(0, 0.16f, -0.09f), c + V(0, 0.19f, -0.18f)), 0.064f, 0.01f, purple, blend: 0f);
        b.Tube(Body, Smooth(3, bc + V(0, 0.07f, -0.06f), bc + V(0, -0.14f, -0.11f), bc + V(0, -0.3f, -0.03f), bc + V(0, -0.34f, 0.08f)), 0.075f, 0.03f, purple, blend: 0f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Cosmog line

    /// <summary>Cosmog: a wisp of nebula, dark blue gas with a black face (two cyan eyes, two yellow dots, a pink mouth) and two arms of gas rising to blue puffs that glitter.</summary>
    private static PokeBuilder Cosmog()
    {
        var b = new PokeBuilder("Cosmog", 0.45f, BodyPlan.Floating, V(0, 0.18f, 0)) { Coat = Fur }.Hover();
        var navy = Rgb(44, 44, 104);
        var purple = Rgb(126, 74, 170);
        var blue = Rgb(80, 160, 246);
        var black = Rgb(24, 22, 30);
        var c = V(0, 0.18f, 0);
        var r = V(0.1f, 0.1f, 0.09f);
        b.Ell(Body, c, r, navy);
        b.PaintEll(Body, c + V(0, -0.08f, 0), V(0.12f, 0.06f, 0.11f), purple);
        for (int i = 0; i < 5; i++)
        {
            float a = (i * 72f + 36f) * Degree;
            b.Ell(Body, c + V(MathF.Sin(a) * 0.07f, -0.07f, MathF.Cos(a) * 0.06f), V(0.04f, 0.035f, 0.04f), purple, blend: 0.02f);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, c + V(0.06f * s, 0.06f, 0));
            var top = c + V(0.12f * s, 0.18f, 0);
            b.Tube(arm, Smooth(3, c + V(0.06f * s, 0.05f, 0), c + V(0.11f * s, 0.11f, 0), top), 0.036f, 0.04f, navy, blend: 0f);
            b.Ell(arm, top + V(0, 0.02f, 0), V(0.058f, 0.048f, 0.048f), blue, mat: Glow);
            foreach (var d in new[] { V(0.035f, 0.01f, 0), V(-0.035f, 0.015f, 0), V(0, 0.045f, 0) })
                b.Ell(arm, top + V(0, 0.02f, 0) + d, V(0.028f, 0.026f, 0.028f), blue, mat: Glow, blend: 0.015f);
            b.Mark(arm, top + V(0.012f * s, 0.03f, 0.045f), V(0, 0, 1f), 0.008f, 0.008f, White, MarkShape.Star);
        });
        // The face: a black disc
        var fc = c + V(0, 0, 0.07f);
        var fr = V(0.065f, 0.06f, 0.03f);
        b.Ell(Body, fc, fr, black, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(fc, fr, 0.034f * s, fc.Y - 0.004f);
            b.Eye(Body, at, Outward(fc, fr, at), 0.011f, Rgb(80, 210, 246));
            var dot = On(fc, fr, 0.015f * s, fc.Y + 0.024f);
            b.Mark(Body, dot, Outward(fc, fr, dot), 0.008f, 0.009f, Rgb(250, 220, 90));
        });
        var mouth = On(fc, fr, 0, fc.Y - 0.026f);
        b.Mark(Body, mouth, Outward(fc, fr, mouth), 0.009f, 0.007f, Rgb(244, 140, 170));
        return Lift(b);
    }

    /// <summary>Cosmoem: a cocoon of stars, a dark ball full of the night sky held in a hard golden shell of petals, widest to each side, its eyes shut and a smile on it.</summary>
    private static PokeBuilder Cosmoem()
    {
        var b = new PokeBuilder("Cosmoem", 0.45f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Shell }.Hover();
        var gold = Rgb(214, 190, 120);
        var navy = Rgb(36, 40, 104);
        var c = V(0, 0.2f, 0);
        var r = V(0.075f, 0.075f, 0.075f);
        // The shell's petals round the dark ball, longest to the sides
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Degree;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            float len = i % 3 == 0 ? 0.22f : 0.16f;
            Frond(b, Body, c + d * 0.04f + V(0, 0, -0.02f), c + d * len + V(0, 0, -0.03f), 0.06f, gold, V(0, 0, 1f), 0.35f);
        }
        b.Ell(Body, c, r, navy, mat: Scales);
        foreach (var p in new[] { V(-0.45f, 0.5f, 0.75f), V(0.5f, -0.4f, 0.75f), V(0.25f, 0.55f, 0.8f), V(-0.4f, -0.5f, 0.75f) })
        {
            var at = Out(c, r, default, p);
            b.Mark(Body, at, Outward(c, r, at), 0.006f, 0.006f, White, MarkShape.Star);
        }
        b.Ell(Body, Out(c, r, default, V(-0.55f, 0.6f, 0.55f)), V(0.012f, 0.012f, 0.012f), White, mat: Glow, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.01f);
            b.Eye(Body, at, Outward(c, r, at), 0.01f, Rgb(30, 30, 50), closed: true);
        });
        var smile = On(c, r, 0, c.Y - 0.015f);
        b.Mark(Body, smile, Outward(c, r, smile), 0.012f, 0.006f, Rgb(20, 20, 40), MarkShape.Smile);
        return Lift(b);
    }

    /// <summary>
    /// Solgaleo, and Necrozma fused with it in the Dusk Mane: a great lion, white with grey legs banded orange, a radiant
    /// mane of white and gold rays tipped with orange, a dark lower face, a blue sun set in gold on its brow and a thin
    /// tail tufted dark. Dusk Mane's body glows gold under black prisms of armour over its head, back and legs, a long
    /// black horn of prism sweeping up from its brow.
    /// </summary>
    private static PokeBuilder SolgaleoBuild(bool dusk)
    {
        var b = new PokeBuilder(dusk ? "Necrozma-Dusk" : "Solgaleo", 1f, BodyPlan.Quadruped, V(0, 0.44f, -0.02f)) { Coat = Fur };
        var coat = dusk ? Rgb(246, 214, 130) : Rgb(240, 240, 236);
        var grey = Rgb(84, 88, 98);
        var gold = Rgb(244, 200, 80);
        var orange = Rgb(236, 110, 60);
        var prism = Rgb(34, 32, 42);
        BeastLegs(b, 0.08f, 0.3f, 0.17f, -0.2f, 0.07f, coat, grey, grey, orange);
        var bc = V(0, 0.4f, -0.02f);
        b.Ell(Body, bc, V(0.12f, 0.12f, 0.24f), coat);
        b.PaintEll(Body, bc + V(0, -0.07f, 0), V(0.09f, 0.04f, 0.2f), grey);
        int tail = b.Tail(bc + V(0, 0.03f, -0.21f));
        var tp = Smooth(3, bc + V(0, 0.03f, -0.21f), bc + V(0, 0.12f, -0.32f), bc + V(0, 0.24f, -0.34f));
        b.Tube(tail, tp, 0.018f, 0.014f, coat, blend: 0f);
        b.Ell(tail, tp[^1] + V(0, 0.02f, 0), V(0.028f, 0.035f, 0.028f), grey);
        int head = b.Head(bc + V(0, 0.1f, 0.18f));
        var c = bc + V(0, 0.17f, 0.24f);
        var r = V(0.065f, 0.065f, 0.08f);
        b.Limb(head, bc + V(0, 0.06f, 0.16f), c + V(0, -0.02f, -0.03f), 0.07f, 0.055f, coat);
        // The mane: rays round the head, white and gold, tipped orange
        for (int i = 0; i < 12; i++)
        {
            float a = (i * 30f + 15f) * Degree;
            var d = V(MathF.Cos(a), MathF.Sin(a) * 0.9f + 0.15f, 0);
            var root = c + d * 0.04f + V(0, 0, -0.04f);
            var tip = c + d * (i % 2 == 0 ? 0.22f : 0.17f) + V(0, 0, -0.12f);
            b.Spike(head, root, tip, 0.04f, i % 2 == 0 ? White : gold, 0.35f);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.55f), V(0.014f, 0.014f, 0.014f), orange);
        }
        b.Ell(head, c, r, coat);
        b.Ell(head, c + V(0, -0.03f, 0.035f), V(0.05f, 0.03f, 0.06f), grey, blend: 0.012f);
        // The sun on its brow, set in gold
        b.Ell(head, c + V(0, 0.045f, 0.045f), V(0.032f, 0.02f, 0.03f), gold, mat: Metal, blend: 0.008f);
        Gem(b, head, c + V(0, 0.05f, 0.07f), V(0, 0.3f, 1f), 0.018f, Rgb(70, 130, 240));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(70, 150, 240), glare: true);
        });
        if (dusk)
        {
            // Black prisms of armour: a helm with a long horn, plates down the back and over the shoulders
            b.Box(head, c + V(0, 0.04f, -0.01f), V(0.06f, 0.03f, 0.07f), 0.008f, prism, V(-10f, 0, 0), Shell);
            b.Spike(head, c + V(0, 0.06f, 0.0f), c + V(0, 0.24f, 0.14f), 0.025f, prism, 0.5f, Shell);
            for (int i = 0; i < 4; i++)
                b.Spike(Body, bc + V(0, 0.08f, 0.12f - i * 0.09f), bc + V(0, 0.17f, 0.06f - i * 0.09f), 0.04f, prism, 0.4f, Shell);
            PokeBuilder.Both(s =>
            {
                b.Spike(Body, bc + V(0.09f * s, 0.05f, 0.05f), bc + V(0.17f * s, 0.12f, 0.0f), 0.03f, prism, 0.4f, Shell);
            });
        }
        return b;
    }

    private static PokeBuilder Solgaleo() => SolgaleoBuild(false);

    /// <summary>
    /// Lunala, and Necrozma fused with it in the Dawn Wings: a bat of the moon, its body slender and pale, a dark face
    /// with red eyes framed by a gold crescent, great wings of purple skin opening into a crescent rimmed gold. Dawn Wings'
    /// wings are pale blue, its body black prisms, prisms at its wings' wrists and tips.
    /// </summary>
    private static PokeBuilder LunalaBuild(bool dawn)
    {
        var b = new PokeBuilder(dawn ? "Necrozma-Dawn" : "Lunala", 1f, BodyPlan.Bird, V(0, 0.45f, 0)) { Coat = Fur }.Hover();
        var membrane = dawn ? Rgb(170, 210, 240) : Rgb(86, 64, 156);
        var gold = Rgb(230, 214, 140);
        var pale = dawn ? Rgb(40, 38, 50) : Rgb(226, 226, 240);
        var dark = Rgb(40, 36, 60);
        var prism = Rgb(34, 32, 42);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.05f * s, 0.55f, -0.02f);
            int wing = b.Wing(s, shoulder);
            var wrist = V(0.3f * s, 0.75f, -0.04f);
            var tips = new[] { V(0.42f * s, 0.5f, -0.05f), V(0.38f * s, 0.25f, -0.05f), V(0.22f * s, 0.12f, -0.04f) };
            DragonWing(b, wing, shoulder, wrist, tips, V(0.05f * s, 0.35f, -0.03f), membrane, membrane, 0.016f);
            if (dawn)
            {
                b.Spike(wing, wrist, wrist + V(0.06f * s, 0.06f, 0), 0.035f, prism, 0.4f, Shell);
                foreach (var t in tips)
                    b.Spike(wing, Vector3.Lerp(wrist, t, 0.8f), t + V(0.02f * s, -0.04f, 0), 0.025f, prism, 0.4f, Shell);
            }
            else
            {
                // The gold rim of the crescent, over the wrist and down the leading edge
                b.Tube(wing, Smooth(3, V(0.08f * s, 0.62f, -0.03f), V(0.24f * s, 0.8f, -0.045f), V(0.4f * s, 0.72f, -0.055f), V(0.45f * s, 0.46f, -0.055f)), 0.022f, 0.01f, gold, Shell, 0f);
                foreach (var t in tips)
                    b.PaintEll(wing, Vector3.Lerp(wrist, t, 0.9f), V(0.025f, 0.025f, 0.03f), gold);
            }
        });
        var bc = V(0, 0.45f, 0);
        b.Ell(Body, bc, V(0.045f, 0.1f, 0.04f), pale);
        DanglingLegs(b, 0.025f, 0.37f, 0, 0.14f, 0.012f, pale);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.035f * s, 0.06f, 0.01f));
            var hand = bc + V(0.08f * s, 0.0f, 0.06f);
            b.Limb(arm, bc + V(0.035f * s, 0.06f, 0.01f), hand, 0.011f, 0.009f, pale);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand, hand + V(0.02f * s, -0.02f + t * 0.01f, 0.02f), 0.005f, pale, mat: Shell);
            if (dawn) b.Spike(Body, bc + V(0.03f * s, 0.0f, 0.02f), bc + V(0.09f * s, -0.06f, 0.04f), 0.025f, prism, 0.4f, Shell);
        });
        int head = b.Head(bc + V(0, 0.1f, 0.01f));
        var c = bc + V(0, 0.16f, 0.02f);
        var r = V(0.045f, 0.045f, 0.045f);
        b.Limb(head, bc + V(0, 0.07f, 0), c, 0.022f, 0.022f, pale);
        b.Ell(head, c, r, pale);
        b.PaintEll(head, c + V(0, -0.005f, 0.03f), V(0.035f, 0.03f, 0.025f), dark);
        if (dawn)
            b.Spike(head, c + V(0, 0.03f, 0), c + V(0, 0.12f, -0.04f), 0.03f, prism, 0.4f, Shell);
        else
            b.Torus(head, c + V(0, 0.0f, -0.02f), 0.058f, 0.01f, gold, V(90f, 0, 0), mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.015f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(232, 80, 120), glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Lunala() => LunalaBuild(false);

    // ------------------------------------------------------------------ The Ultra Beasts

    /// <summary>Nihilego: a parasite like a jellyfish of glass, a clear white bell rimmed with a wavy blue frill, dark stars inside it, a body hanging below in a lilac glow and lobed tentacles; no face at all.</summary>
    private static PokeBuilder Nihilego()
    {
        var b = new PokeBuilder("Nihilego", 0.85f, BodyPlan.Floating, V(0, 0.45f, 0)) { Coat = Scales }.Hover();
        var white = Rgb(236, 238, 246);
        var lilac = Rgb(186, 172, 222);
        var blue = Rgb(110, 186, 240);
        var c = V(0, 0.55f, 0);
        var r = V(0.13f, 0.1f, 0.12f);
        b.Ell(Body, c, r, white);
        // The frill round the rim, wavy
        b.Torus(Body, c + V(0, -0.05f, 0), 0.13f, 0.012f, blue, blend: 0.006f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Degree;
            b.Ell(Body, c + V(MathF.Sin(a) * 0.135f, -0.05f + (i % 2 == 0 ? 0.012f : -0.012f), MathF.Cos(a) * 0.135f), V(0.022f, 0.012f, 0.022f), blue, blend: 0.008f);
        }
        foreach (var p in new[] { V(-0.35f, 0.7f, 0.6f), V(0.35f, 0.7f, 0.6f), V(0, 0.8f, 0.55f) })
        {
            var at = Out(c, r, default, p);
            b.Mark(Body, at, Outward(c, r, at), 0.025f, 0.025f, Rgb(130, 130, 160), MarkShape.Star5);
        }
        b.Limb(Body, c + V(0, -0.05f, 0), V(0, 0.22f, 0), 0.08f, 0.03f, white);
        b.PaintEll(Body, V(0, 0.36f, 0.02f), V(0.08f, 0.09f, 0.08f), lilac);
        PokeBuilder.Both(s =>
        {
            foreach (var (x, z, low) in new[] { (0.07f, 0.03f, 0.25f), (0.06f, -0.05f, 0.3f) })
            {
                var t = Smooth(3, c + V(x * s, -0.06f, z), V((x + 0.05f) * s, 0.36f, z + 0.01f), V((x + 0.03f) * s, low, z));
                b.Tube(Body, t, 0.026f, 0.016f, white, blend: 0f);
                b.Ell(Body, t[^1], V(0.02f, 0.02f, 0.02f), white);
            }
        });
        return Lift(b);
    }

    /// <summary>Buzzwole: a mosquito swollen with muscle, its great red muscles shining orange, black fists, a little dark head with orange eyes, a long striped proboscis held up like a lance, thin orange wings and dark spikes at its shoulders.</summary>
    private static PokeBuilder Buzzwole()
    {
        var b = new PokeBuilder("Buzzwole", 1f, BodyPlan.Biped, V(0, 0.46f, 0)) { Coat = Shell };
        var red = Rgb(214, 62, 50);
        var orange = Rgb(246, 140, 64);
        var dark = Rgb(90, 34, 34);
        var black = Rgb(40, 30, 34);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.32f, 0));
            var knee = V(0.09f * s, 0.18f, 0.03f);
            b.Limb(leg, V(0.05f * s, 0.4f, 0), knee, 0.032f, 0.025f, dark);
            b.Ell(leg, V(0.08f * s, 0.25f, 0.015f), V(0.05f, 0.075f, 0.05f), red);
            b.Limb(leg, knee, V(0.09f * s, 0.03f, 0), 0.025f, 0.02f, dark);
            b.Ell(leg, V(0.09f * s, 0.12f, 0.02f), V(0.035f, 0.045f, 0.035f), red);
            b.Ell(leg, V(0.09f * s, 0.025f, 0.02f), V(0.025f, 0.025f, 0.04f), black);
            b.Spike(leg, V(0.09f * s, 0.02f, 0.05f), V(0.1f * s, 0.005f, 0.09f), 0.01f, black, mat: Shell);
        });
        var bc = V(0, 0.48f, 0);
        b.Ell(Body, bc, V(0.11f, 0.13f, 0.09f), dark);
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, bc + V(0.05f * s, 0.04f, 0.055f), V(0.055f, 0.045f, 0.04f), orange);
            b.Ell(Body, bc + V(0.035f * s, -0.06f, 0.06f), V(0.035f, 0.035f, 0.03f), red);
            b.Spike(Body, bc + V(0.1f * s, 0.11f, -0.02f), bc + V(0.16f * s, 0.18f, -0.05f), 0.025f, black, mat: Shell);
            // The arms: great muscles and black fists
            var shoulder = bc + V(0.12f * s, 0.08f, 0);
            int arm = b.Arm(s, shoulder);
            var fist = shoulder + V(0.1f * s, -0.18f, 0.08f);
            b.Ell(arm, shoulder + V(0.07f * s, -0.02f, 0.02f), V(0.08f, 0.065f, 0.065f), red);
            b.PaintEll(arm, shoulder + V(0.08f * s, 0.01f, 0.07f), V(0.04f, 0.03f, 0.03f), orange);
            b.Limb(arm, shoulder + V(0.09f * s, -0.06f, 0.03f), fist, 0.04f, 0.035f, dark);
            b.Ell(arm, fist, V(0.05f, 0.05f, 0.05f), black);
            // The wings
            Frond(b, Body, bc + V(0.04f * s, 0.1f, -0.08f), bc + V(0.15f * s, 0.0f, -0.18f), 0.05f, Rgb(250, 170, 100), V(0, 0.3f, 1f), 0.2f);
        });
        int head = b.Head(bc + V(0, 0.12f, 0.03f));
        var c = bc + V(0, 0.17f, 0.04f);
        var r = V(0.035f, 0.035f, 0.035f);
        b.Ell(head, c, r, dark);
        // The proboscis, held up like a lance, striped
        var root = c + V(0, 0.0f, 0.03f);
        var tip = c + V(-0.1f, 0.22f, 0.08f);
        b.Limb(head, root, tip, 0.012f, 0.005f, White, Shell);
        foreach (var t in new[] { 0.3f, 0.55f, 0.8f })
            b.PaintEll(head, Vector3.Lerp(root, tip, t), V(0.014f, 0.014f, 0.014f), red);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.016f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, Rgb(250, 160, 60), glare: true);
        });
        return b;
    }

    /// <summary>Pheromosa: a slender insect like a dancer, white and pale cream, legs and arms long and thin, a narrow waist over layered plates like a skirt, a little head with a cream crown, lashes over shut eyes, and two long antennae arching to the ground, tipped gold.</summary>
    private static PokeBuilder Pheromosa()
    {
        var b = new PokeBuilder("Pheromosa", 0.9f, BodyPlan.Biped, V(0, 0.55f, 0)) { Coat = Shell };
        var white = Rgb(244, 242, 236);
        var cream = Rgb(232, 222, 196);
        var gold = Rgb(220, 190, 110);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.46f, 0));
            var knee = V(0.03f * s, 0.23f, 0.02f);
            b.Limb(leg, V(0.025f * s, 0.47f, 0), knee, 0.016f, 0.012f, white);
            b.Limb(leg, knee, V(0.03f * s, 0.004f, 0.0f), 0.012f, 0.004f, white);
            b.PaintEll(leg, knee, V(0.014f, 0.014f, 0.014f), gold);
        });
        b.Ell(Body, V(0, 0.48f, 0), V(0.05f, 0.03f, 0.04f), white);
        b.Ell(Body, V(0, 0.52f, 0), V(0.04f, 0.024f, 0.034f), cream);
        b.Limb(Body, V(0, 0.5f, 0), V(0, 0.6f, 0), 0.02f, 0.016f, white);
        b.Ell(Body, V(0, 0.64f, 0), V(0.035f, 0.05f, 0.03f), white);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.03f * s, 0.67f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.065f * s, 0.58f, 0.02f);
            b.Limb(arm, shoulder, elbow, 0.01f, 0.009f, white);
            b.Limb(arm, elbow, V(0.055f * s, 0.5f, 0.05f), 0.009f, 0.007f, white);
        });
        int head = b.Head(V(0, 0.68f, 0));
        var c = V(0, 0.745f, 0.01f);
        var r = V(0.036f, 0.045f, 0.036f);
        b.Limb(head, V(0, 0.67f, 0), c, 0.012f, 0.012f, white);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, 0.035f, -0.008f), V(0.036f, 0.02f, 0.032f), cream, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            var arc = Smooth(3, c + V(0.012f * s, 0.04f, -0.01f), c + V(0.1f * s, 0.12f, -0.02f), c + V(0.22f * s, 0.0f, -0.02f), V(0.24f * s, 0.03f, 0));
            b.Tube(head, arc, 0.008f, 0.007f, white, blend: 0f);
            b.Ell(head, arc[^1], V(0.012f, 0.016f, 0.012f), gold);
            var at = On(c, r, 0.014f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, gold, closed: true);
        });
        return b;
    }

    /// <summary>Xurkitree: a creature of cables, black cords twisted into legs, a body and arms, banded with white, copper wires splayed for hands and plugs for feet, and for a head a white star bristling with spikes; no face.</summary>
    private static PokeBuilder Xurkitree()
    {
        var b = new PokeBuilder("Xurkitree", 1f, BodyPlan.Biped, V(0, 0.55f, 0)) { Coat = Scales };
        var black = Rgb(40, 40, 46);
        var white = Rgb(220, 230, 236);
        var copper = Rgb(220, 140, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.44f, 0));
            b.Tube(leg, Smooth(3, V(0.03f * s, 0.46f, 0), V(0.07f * s, 0.25f, 0.02f), V(0.06f * s, 0.06f, 0)), 0.026f, 0.024f, black, blend: 0f);
            b.Ell(leg, V(0.06f * s, 0.03f, 0.02f), V(0.032f, 0.03f, 0.045f), black);
            foreach (float t in new[] { -1f, 1f })
                b.Limb(leg, V(0.06f * s + 0.012f * t, 0.025f, 0.05f), V(0.06f * s + 0.012f * t, 0.012f, 0.085f), 0.007f, 0.006f, copper, Metal);
            b.Torus(leg, V(0.07f * s, 0.25f, 0.02f), 0.03f, 0.008f, white, blend: 0.004f);
        });
        // Two cords twisted together into the body
        b.Tube(Body, Smooth(3, V(-0.03f, 0.44f, 0), V(0.025f, 0.6f, 0.01f), V(-0.01f, 0.78f, 0)), 0.03f, 0.028f, black, blend: 0f);
        b.Tube(Body, Smooth(3, V(0.03f, 0.44f, 0), V(-0.025f, 0.6f, -0.01f), V(0.01f, 0.78f, 0)), 0.03f, 0.028f, black, blend: 0f);
        b.Torus(Body, V(0, 0.52f, 0), 0.045f, 0.009f, white, blend: 0.004f);
        b.Torus(Body, V(0, 0.68f, 0), 0.045f, 0.009f, white, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.02f * s, 0.75f, 0);
            int arm = b.Arm(s, shoulder);
            var hand = V(0.26f * s, 0.62f, 0.04f);
            b.Tube(arm, Smooth(3, shoulder, V(0.15f * s, 0.73f, 0.02f), hand), 0.024f, 0.02f, black, blend: 0f);
            b.Torus(arm, V(0.15f * s, 0.73f, 0.02f), 0.028f, 0.007f, white, V(0, 0, 80f * s), blend: 0.004f);
            for (int i = 0; i < 5; i++)
            {
                float a = (i * 30f - 60f) * Degree;
                var d = V(MathF.Cos(a) * s, MathF.Sin(a), 0.2f);
                b.Limb(arm, hand, hand + Vector3.Normalize(d) * 0.08f, 0.009f, 0.006f, copper, Metal);
            }
        });
        // The head: a white star of spikes
        var c = V(0, 0.86f, 0);
        int head = b.Head(V(0, 0.8f, 0));
        b.Limb(head, V(0, 0.78f, 0), c, 0.024f, 0.03f, black);
        b.Ell(head, c, V(0.045f, 0.045f, 0.045f), white);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 18; i++)
        {
            float u = -0.6f + 1.55f * (i + 0.5f) / 18f, ring = MathF.Sqrt(1f - u * u), a = i * golden;
            var d = V(MathF.Sin(a) * ring, u, MathF.Cos(a) * ring);
            b.Spike(head, c + d * 0.03f, c + d * 0.11f, 0.02f, white, mat: Shell);
        }
        return b;
    }

    /// <summary>Celesteela: a rocket of bamboo, a tall pale green body like a gown patterned with white triangles, standing on its nozzles, a small head with a pale visor and a tall aerial, and two great arms like bamboo stalks, segmented and capped with gold, their hands nozzles at the bottom.</summary>
    private static PokeBuilder Celesteela()
    {
        var b = new PokeBuilder("Celesteela", 1f, BodyPlan.Biped, V(0, 0.45f, 0)) { Coat = Metal };
        var green = Rgb(140, 182, 162);
        var pale = Rgb(204, 228, 204);
        var dark = Rgb(56, 76, 72);
        var gold = Rgb(232, 214, 120);
        b.Limb(Body, V(0, 0.12f, 0), V(0, 0.62f, 0), 0.13f, 0.045f, pale);
        foreach (var (y, x) in new[] { (0.22f, -0.05f), (0.22f, 0.05f), (0.3f, 0f), (0.38f, -0.03f), (0.38f, 0.03f) })
        {
            float rad = 0.13f - (y - 0.12f) * 0.17f;
            var n = Vector3.Normalize(V(x, 0.15f, MathF.Sqrt(MathF.Max(rad * rad - x * x, 0.0001f))));
            b.Mark(Body, V(x, y, MathF.Sqrt(MathF.Max(rad * rad - x * x, 0.0001f))), n, 0.018f, 0.018f, White, MarkShape.Triangle);
        }
        for (int i = 0; i < 5; i++)
        {
            float a = i * 72f * Degree;
            var at = V(MathF.Sin(a) * 0.08f, 0.13f, MathF.Cos(a) * 0.08f);
            b.Limb(Body, at, at + V(0, -0.11f, 0), 0.026f, 0.022f, dark);
        }
        int head = b.Head(V(0, 0.62f, 0.01f));
        var c = V(0, 0.71f, 0.02f);
        var r = V(0.04f, 0.06f, 0.04f);
        b.Ell(head, c, r, green);
        b.PaintEll(head, c + V(0, 0.005f, 0.03f), V(0.03f, 0.03f, 0.02f), pale);
        b.Spike(head, c + V(0, 0.05f, 0), c + V(0, 0.2f, 0), 0.012f, pale);
        PokeBuilder.Both(s =>
        {
            // An arm of bamboo, segmented, capped with gold, nozzles for fingers at its foot
            var shoulder = V(0.04f * s, 0.6f, 0);
            int arm = b.Arm(s, shoulder);
            var top = V(0.2f * s, 0.92f, -0.03f);
            var bottom = V(0.27f * s, 0.26f, 0.02f);
            b.Limb(arm, shoulder, Vector3.Lerp(top, bottom, 0.45f), 0.03f, 0.03f, dark);
            b.Limb(arm, top, bottom, 0.05f, 0.056f, green);
            foreach (var t in new[] { 0.2f, 0.4f, 0.6f, 0.8f })
                b.Torus(arm, Vector3.Lerp(top, bottom, t), 0.054f, 0.007f, pale, Euler(bottom - top), blend: 0.004f);
            b.Ell(arm, top, V(0.04f, 0.015f, 0.04f), gold, Euler(top - bottom), Glow);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f * Degree;
                var at = bottom + V(MathF.Sin(a) * 0.035f, -0.03f, MathF.Cos(a) * 0.035f);
                b.Limb(arm, at, at + V(0, -0.08f, 0.01f), 0.014f, 0.012f, dark);
            }
        });
        return b;
    }

    /// <summary>Kartana: a warrior of folded paper, flat white planes edged with red and gold, a small diamond of a head with a red fold at its heart and a great blade curving up from it like a crescent, arms that are long blades striped gold near the shoulder, and two pointed legs.</summary>
    private static PokeBuilder Kartana()
    {
        var b = new PokeBuilder("Kartana", 0.7f, BodyPlan.Biped, V(0, 0.48f, 0)) { Coat = Fur };
        var white = Rgb(246, 244, 240);
        var red = Rgb(220, 70, 50);
        var gold = Rgb(236, 180, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.43f, 0));
            b.Spike(leg, V(0.025f * s, 0.44f, 0), V(0.07f * s, 0.004f, 0.01f), 0.03f, white, 0.35f);
            b.PaintEll(leg, V(0.035f * s, 0.36f, 0.005f), V(0.02f, 0.03f, 0.02f), red);
        });
        b.Box(Body, V(0, 0.49f, 0), V(0.045f, 0.045f, 0.012f), 0.004f, white, V(0, 0, 45f));
        b.PaintEll(Body, V(0, 0.49f, 0.012f), V(0.02f, 0.02f, 0.008f), gold);
        int head = b.Head(V(0, 0.54f, 0));
        var c = V(0, 0.61f, 0);
        b.Limb(head, V(0, 0.53f, 0), c, 0.01f, 0.01f, white);
        b.Box(head, c, V(0.035f, 0.035f, 0.012f), 0.004f, white, V(0, 0, 45f));
        b.PaintEll(head, c + V(0, 0, 0.012f), V(0.016f, 0.03f, 0.008f), red);
        // The great blade curving up from its head
        b.Tube(head, Smooth(3, c + V(0, 0.02f, 0), c + V(-0.05f, 0.14f, 0), c + V(-0.03f, 0.26f, 0), c + V(0.03f, 0.32f, 0)), 0.02f, 0.006f, white, blend: 0f);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.03f * s, 0.52f, 0);
            int arm = b.Arm(s, shoulder);
            var tip = V(0.36f * s, 0.44f, 0.02f);
            var mid = (shoulder + tip) / 2f;
            float angle = MathF.Atan2(tip.Y - shoulder.Y, (tip.X - shoulder.X) * s) / Degree * s;
            b.Box(arm, mid, V(Vector3.Distance(shoulder, tip) / 2f, 0.016f, 0.008f), 0.003f, white, V(0, 0, angle));
            foreach (var t in new[] { 0.12f, 0.22f, 0.32f })
                b.PaintEll(arm, Vector3.Lerp(shoulder, tip, t) + V(0, 0, 0.006f), V(0.008f, 0.02f, 0.006f), gold);
        });
        return b;
    }

    /// <summary>Guzzlord: a glutton as wide as it is tall, black, a blue belly between jagged bands of yellow, long arms each ending in a great jaw of white teeth, a small head crowned with yellow points and cyan eyes, stubby legs and a spiked ball of a tail.</summary>
    private static PokeBuilder Guzzlord()
    {
        var b = new PokeBuilder("Guzzlord", 1f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Scales };
        var black = Rgb(50, 50, 62);
        var yellow = Rgb(240, 200, 60);
        var blue = Rgb(70, 120, 170);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.12f, 0));
            b.Limb(leg, V(0.12f * s, 0.13f, 0), V(0.14f * s, 0.04f, 0.02f), 0.06f, 0.05f, black);
            b.Ell(leg, V(0.14f * s, 0.03f, 0.04f), V(0.055f, 0.03f, 0.06f), black);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.14f * s + 0.025f * t, 0.02f, 0.09f), V(0.14f * s + 0.03f * t, 0.005f, 0.12f), 0.012f, yellow, mat: Shell);
        });
        var bc = V(0, 0.3f, 0);
        var br = V(0.24f, 0.2f, 0.18f);
        b.Ell(Body, bc, br, black);
        b.PaintEll(Body, bc + V(0, -0.01f, 0.12f), V(0.16f, 0.13f, 0.08f), blue);
        foreach (float y in new[] { 0.14f, -0.15f })
            foreach (float x in new[] { -0.6f, 0f, 0.6f })
            {
                var at = Out(bc, br, default, V(x, y * 5f, 0.8f));
                b.Mark(Body, at, Outward(bc, br, at), 0.05f, 0.016f, yellow, MarkShape.Zigzag, x * -25f);
            }
        PokeBuilder.Both(s =>
        {
            // The arms, each ending in a great jaw
            var shoulder = bc + V(0.2f * s, 0.1f, 0);
            int arm = b.Arm(s, shoulder);
            var wrist = shoulder + V(0.12f * s, 0.18f, 0.06f);
            b.Tube(arm, Smooth(2, shoulder, shoulder + V(0.14f * s, 0.05f, 0.02f), wrist), 0.045f, 0.04f, black, blend: 0f);
            var jaw = wrist + V(0.02f * s, 0.05f, 0.04f);
            b.Ell(arm, jaw + V(0, 0.04f, 0), V(0.07f, 0.035f, 0.07f), black, V(-20f, 0, 0));
            b.Ell(arm, jaw + V(0, -0.04f, 0), V(0.065f, 0.03f, 0.065f), black, V(20f, 0, 0));
            for (int i = 0; i < 4; i++)
            {
                float x = (i - 1.5f) * 0.025f;
                b.Spike(arm, jaw + V(x, 0.02f, 0.05f), jaw + V(x, -0.01f, 0.06f), 0.009f, White, mat: Shell, blend: 0.003f);
                b.Spike(arm, jaw + V(x, -0.02f, 0.05f), jaw + V(x, 0.01f, 0.06f), 0.009f, White, mat: Shell, blend: 0.003f);
            }
        });
        int head = b.Head(bc + V(0, 0.18f, 0.04f));
        var c = bc + V(0, 0.22f, 0.06f);
        var r = V(0.06f, 0.05f, 0.05f);
        b.Ell(head, c, r, black);
        foreach (float x in new[] { -0.035f, 0f, 0.035f })
            b.Spike(head, c + V(x, 0.03f, -0.01f), c + V(x * 1.6f, 0.08f, -0.02f), 0.012f, yellow, mat: Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(80, 220, 240), glare: true);
        });
        int tail = b.Tail(bc + V(0, -0.1f, -0.15f));
        b.Limb(tail, bc + V(0, -0.1f, -0.15f), V(0, 0.12f, -0.3f), 0.05f, 0.03f, black);
        b.Ell(tail, V(0, 0.12f, -0.32f), V(0.04f, 0.04f, 0.04f), black);
        foreach (var d in new[] { V(0, 1f, 0), V(1f, 0, -0.3f), V(-1f, 0, -0.3f), V(0, -0.3f, -1f), V(0, 0.6f, -0.8f) })
            b.Spike(tail, V(0, 0.12f, -0.32f) + Vector3.Normalize(d) * 0.03f, V(0, 0.12f, -0.32f) + Vector3.Normalize(d) * 0.075f, 0.016f, black, mat: Shell);
        return b;
    }

    // ------------------------------------------------------------------ Necrozma

    /// <summary>
    /// Necrozma and its Ultra form. Necrozma is a creature of black prisms edged with white light: a faceted body on thin
    /// legs, two great prisms for arms with claws below them, prisms jutting from its back, and a pointed head with cyan
    /// eyes. Ultra Necrozma is a dragon of gold light, the same shape glowing, with great spiked wings.
    /// </summary>
    private static PokeBuilder NecrozmaBuild(bool ultra)
    {
        var b = new PokeBuilder(ultra ? "Necrozma-Ultra" : "Necrozma", 1f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Shell };
        var body = ultra ? Rgb(250, 222, 110) : Rgb(34, 32, 42);
        var edge = ultra ? Rgb(255, 246, 200) : Rgb(214, 216, 230);
        SurfaceMaterial mat = ultra ? Glow : Shell;
        if (ultra)
            PokeBuilder.Both(s =>
            {
                var shoulder = V(0.05f * s, 0.62f, -0.04f);
                int wing = b.Wing(s, shoulder);
                var wrist = V(0.3f * s, 0.86f, -0.06f);
                var tips = new[] { V(0.5f * s, 0.78f, -0.07f), V(0.48f * s, 0.6f, -0.07f), V(0.36f * s, 0.44f, -0.06f) };
                DragonWing(b, wing, shoulder, wrist, tips, V(0.05f * s, 0.46f, -0.05f), Rgb(255, 238, 160), Rgb(250, 222, 110), 0.016f);
                foreach (var t in tips)
                    b.Spike(wing, Vector3.Lerp(wrist, t, 0.85f), t + V(0.03f * s, 0.02f, 0), 0.02f, body, 0.5f, mat);
            });
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.42f, 0));
            var knee = V(0.08f * s, 0.24f, 0.05f);
            var foot = V(0.06f * s, 0.03f, 0);
            b.Limb(leg, V(0.05f * s, 0.43f, 0), knee, 0.03f, 0.022f, body, mat);
            b.Limb(leg, knee, foot, 0.022f, 0.015f, body, mat);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, foot, foot + V(0.02f * t, -0.025f, 0.05f), 0.01f, body, mat: mat);
            b.PaintEll(leg, knee, V(0.025f, 0.02f, 0.03f), edge);
        });
        var bc = V(0, 0.5f, 0);
        b.Box(Body, bc, V(0.065f, 0.09f, 0.05f), 0.01f, body, V(0, 45f, 0), mat);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.06f), V(0.02f, 0.06f, 0.02f), edge);
        foreach (var (x, y) in new[] { (0.05f, 0.06f), (-0.05f, 0.06f), (0f, 0.02f) })
            b.Spike(Body, bc + V(x, y, -0.03f), bc + V(x * 2f, y + 0.08f, -0.14f), 0.03f, body, 0.4f, mat);
        PokeBuilder.Both(s =>
        {
            // The great prisms of its arms, edged white, claws below
            var shoulder = bc + V(0.06f * s, 0.08f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.12f * s, 0.12f, 0.02f);
            b.Limb(arm, shoulder, elbow, 0.025f, 0.022f, body, mat);
            var prism = elbow + V(0.03f * s, -0.1f, 0.02f);
            b.Limb(arm, elbow, prism, 0.022f, 0.022f, body, mat);
            b.Box(arm, prism, V(0.035f, 0.13f, 0.05f), 0.008f, body, V(0, 0, 15f * s), mat);
            b.PaintEll(arm, prism + V(0.032f * s, 0, 0.045f), V(0.01f, 0.1f, 0.01f), edge);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, prism + V(0, -0.11f, 0.02f * t), prism + V(0.02f * s, -0.18f, 0.03f * t + 0.02f), 0.012f, body, mat: mat);
        });
        int head = b.Head(bc + V(0, 0.11f, 0.02f));
        var c = bc + V(0, 0.18f, 0.04f);
        var r = V(0.04f, 0.045f, 0.05f);
        b.Limb(head, bc + V(0, 0.07f, 0), c, 0.022f, 0.02f, body, mat);
        b.Ell(head, c, r, body, mat: mat);
        b.Spike(head, c + V(0, 0.03f, -0.01f), c + V(0, 0.14f, -0.06f), 0.025f, body, 0.4f, mat);
        if (ultra)
            PokeBuilder.Both(s => b.Spike(head, c + V(0.025f * s, 0.03f, -0.01f), c + V(0.09f * s, 0.11f, -0.04f), 0.016f, body, 0.4f, mat));
        b.PaintEll(head, c + V(0, 0.005f, 0.04f), V(0.008f, 0.03f, 0.012f), edge);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.018f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, ultra ? Rgb(60, 140, 240) : Rgb(70, 220, 240), glare: true);
        });
        return ultra ? Lift(b.Hover()) : b;
    }

    private static PokeBuilder Necrozma() => NecrozmaBuild(false);

    // ------------------------------------------------------------------ Magearna

    /// <summary>
    /// Magearna in its colours and its Mega Evolutions: a mechanical doll, a body round as a Poké Ball for a skirt (its
    /// top half scalloped with gold, its bottom half white, a gold band and a buckle between), a heart like a little Poké
    /// Ball on its chest, thin gold arms with pointed hands, two pointed gold legs, a face with lenses for eyes, a cog
    /// behind its head and two fan-shaped ears. Magearna is grey and white, its Original Color red. Its Megas are red,
    /// with ears like crescents, a crown of silver points and two great cogs at its sides, dark for Mega Magearna and
    /// white for the Original Color's.
    /// </summary>
    private static PokeBuilder MagearnaBuild(bool original, bool mega)
    {
        var name = "Magearna" + (original ? "-Original" : "") + (mega ? "-Mega" : "");
        var b = new PokeBuilder(name, 0.85f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Metal };
        var white = Rgb(244, 244, 248);
        var silver = Rgb(212, 214, 224);
        var red = Rgb(204, 52, 52);
        var gold = Rgb(232, 196, 92);
        var top = original || mega ? red : silver;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.14f, 0));
            b.Spike(leg, V(0.05f * s, 0.15f, 0), V(0.035f * s, 0.0f, 0.01f), 0.026f, gold);
        });
        // The round body: its top half scalloped gold, a gold band and a buckle, white below
        var sc = V(0, 0.25f, 0);
        var sr = V(0.15f, 0.14f, 0.14f);
        b.Ell(Body, sc, sr, top);
        b.PaintEll(Body, sc + V(0, -0.12f, 0), V(0.17f, 0.12f, 0.16f), white);
        b.Torus(Body, sc + V(0, -0.01f, 0), 0.146f, 0.01f, gold, blend: 0.004f);
        b.Box(Body, sc + V(0, -0.01f, 0.14f), V(0.028f, 0.028f, 0.012f), 0.006f, gold);
        b.PaintEll(Body, sc + V(0, -0.01f, 0.152f), V(0.014f, 0.014f, 0.006f), Rgb(40, 40, 50));
        foreach (var p in new[] { V(-0.6f, 0.55f, 0.6f), V(0.6f, 0.55f, 0.6f), V(0, 0.45f, 0.9f), V(-0.9f, 0.4f, 0f), V(0.9f, 0.4f, 0f), V(-0.4f, 0.75f, -0.5f), V(0.4f, 0.75f, -0.5f) })
        {
            var at = Out(sc, sr, default, p);
            b.Mark(Body, at, Outward(sc, sr, at), 0.03f, 0.01f, gold, MarkShape.Smile);
        }
        b.Ell(Body, V(0, 0.42f, 0), V(0.05f, 0.06f, 0.045f), mega || original ? gold : white);
        // The heart on its chest, a little Poké Ball in a gold ring
        var heart = V(0, 0.43f, 0.046f);
        b.Torus(Body, heart, 0.017f, 0.004f, gold, V(90f, 0, 0), blend: 0.003f);
        b.PaintEll(Body, heart + V(-0.008f, 0, 0), V(0.009f, 0.015f, 0.008f), Rgb(90, 140, 220));
        b.PaintEll(Body, heart + V(0.008f, 0, 0), V(0.009f, 0.015f, 0.008f), Rgb(220, 70, 70));
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.045f * s, 0.45f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = V(0.09f * s, 0.4f, 0.03f);
            var hand = V(0.13f * s, 0.36f, 0.06f);
            b.Limb(arm, shoulder, elbow, 0.014f, 0.012f, gold);
            b.Limb(arm, elbow, hand, 0.012f, 0.011f, gold);
            b.Spike(arm, hand, hand + V(0.05f * s, -0.01f, 0.03f), 0.016f, mega || original ? gold : white, 0.6f);
        });
        int head = b.Head(V(0, 0.47f, 0));
        var c = V(0, 0.54f, 0.01f);
        var r = V(0.05f, 0.05f, 0.045f);
        b.Limb(head, V(0, 0.46f, 0), c, 0.02f, 0.02f, gold);
        b.Ell(head, c, r, mega || original ? gold : white);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, c.Y + 0.004f);
            var n = Outward(c, r, at);
            b.PaintTorus(head, at, 0.016f, 0.004f, gold, Euler(n));
            b.Eye(head, at, n, 0.011f, Rgb(230, 80, 110), sclera: true);
            if (mega)
            {
                // Ears like crescents
                b.Tube(head, Smooth(3, c + V(0.03f * s, 0.04f, -0.01f), c + V(0.08f * s, 0.14f, -0.02f), c + V(0.15f * s, 0.12f, -0.02f), c + V(0.16f * s, 0.06f, -0.02f)), 0.024f, 0.01f, red, blend: 0f);
            }
            else
            {
                int ear = b.Ear(head, s, c + V(0.03f * s, 0.04f, -0.01f));
                b.Ell(ear, c + V(0.08f * s, 0.09f, -0.02f), V(0.075f, 0.04f, 0.012f), original ? red : white, V(0, 0, 25f * s));
                b.PaintEll(ear, c + V(0.12f * s, 0.11f, -0.02f), V(0.03f, 0.025f, 0.02f), gold);
            }
        });
        if (mega)
        {
            // A crown of silver points, and the two great cogs at its sides
            foreach (float x in new[] { -0.03f, -0.015f, 0f, 0.015f, 0.03f })
                b.Spike(head, c + V(x, 0.04f, -0.01f), c + V(x * 1.5f, 0.1f - MathF.Abs(x), -0.02f), 0.008f, silver, mat: Metal);
            var cog = original ? Rgb(226, 228, 234) : Rgb(70, 72, 80);
            PokeBuilder.Both(s =>
            {
                var gc = V(0.22f * s, 0.28f, -0.08f);
                b.Torus(Body, gc, 0.11f, 0.03f, cog, V(90f, 0, 0), mat: Metal, blend: 0.004f);
                for (int i = 0; i < 8; i++)
                {
                    float a = (i * 45f + 22.5f) * Degree;
                    var d = V(MathF.Cos(a), MathF.Sin(a), 0);
                    b.Box(Body, gc + d * 0.15f, V(0.022f, 0.022f, 0.026f), 0.004f, cog, V(0, 0, i * 45f + 22.5f), Metal);
                }
            });
        }
        else
        {
            // The cog behind its head
            var gc = c + V(0, 0, -0.035f);
            b.Torus(head, gc, 0.055f, 0.012f, white, V(90f, 0, 0), blend: 0.004f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Degree;
                b.Box(head, gc + V(MathF.Cos(a), MathF.Sin(a), 0) * 0.07f, V(0.008f, 0.008f, 0.01f), 0.002f, white, V(0, 0, i * 36f));
            }
        }
        return b;
    }

    private static PokeBuilder Magearna() => MagearnaBuild(false, false);

    // ------------------------------------------------------------------ Marshadow

    /// <summary>Marshadow: a little shadow, grey with a darker mask round its great eyes (red in rings of gold), wisps of smoke curling from its head like a hood, a dark ruff of smoke at its neck and a thin wisp trailing behind it.</summary>
    private static PokeBuilder Marshadow()
    {
        var b = new PokeBuilder("Marshadow", 0.5f, BodyPlan.Biped, V(0, 0.13f, 0)) { Coat = Fur };
        var grey = Rgb(120, 122, 132);
        var dark = Rgb(60, 60, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.08f, 0));
            b.Limb(leg, V(0.03f * s, 0.09f, 0), V(0.035f * s, 0.02f, 0.01f), 0.02f, 0.018f, grey);
            b.Ell(leg, V(0.035f * s, 0.015f, 0.015f), V(0.02f, 0.015f, 0.025f), grey);
            int arm = b.Arm(s, V(0.04f * s, 0.17f, 0));
            b.Limb(arm, V(0.04f * s, 0.17f, 0), V(0.07f * s, 0.12f, 0.03f), 0.014f, 0.012f, grey);
            b.Ell(arm, V(0.072f * s, 0.115f, 0.032f), V(0.016f, 0.016f, 0.016f), grey);
        });
        b.Ell(Body, V(0, 0.13f, 0), V(0.05f, 0.065f, 0.045f), grey);
        // The ruff of smoke at its neck
        b.Torus(Body, V(0, 0.19f, 0), 0.045f, 0.018f, dark, blend: 0.008f);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f * Degree;
            b.Ell(Body, V(MathF.Sin(a) * 0.05f, 0.18f, MathF.Cos(a) * 0.05f), V(0.025f, 0.02f, 0.025f), dark, blend: 0.012f);
        }
        int tail = b.Tail(V(0, 0.08f, -0.04f));
        b.Tube(tail, Smooth(3, V(0, 0.08f, -0.04f), V(0, 0.02f, -0.1f), V(0.04f, 0.008f, -0.18f), V(0.08f, 0.01f, -0.2f)), 0.012f, 0.005f, dark, blend: 0f);
        int head = b.Head(V(0, 0.2f, 0));
        var c = V(0, 0.27f, 0.01f);
        var r = V(0.075f, 0.065f, 0.06f);
        b.Ell(head, c, r, grey);
        // The hood of smoke: lobes at the sides and a curl on top
        PokeBuilder.Both(s => b.Ell(head, c + V(0.065f * s, 0.03f, -0.015f), V(0.04f, 0.04f, 0.035f), grey, blend: 0.02f));
        Curl(b, head, c + V(0, 0.06f, -0.01f), c + V(0, 0.1f, -0.01f), V(0, 1f, 0), V(1f, 0, 0), 0.03f, 1.1f, 0.013f, grey);
        b.PaintEll(head, c + V(0, 0.0f, 0.03f), V(0.08f, 0.03f, 0.05f), dark);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, pupil: Rgb(220, 70, 40), white: Rgb(250, 196, 70), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Poipole and Naganadel

    /// <summary>Poipole: a little purple insect of poison floating, its head like a bud striped magenta with three points below, a pale spike standing from its crown, a magenta mask with cyan eyes, thin legs and a curled tail.</summary>
    private static PokeBuilder Poipole()
    {
        var b = new PokeBuilder("Poipole", 0.45f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Scales }.Hover();
        var purple = Rgb(130, 80, 180);
        var magenta = Rgb(220, 70, 150);
        var pale = Rgb(200, 180, 230);
        b.Ell(Body, V(0, 0.2f, 0), V(0.025f, 0.045f, 0.025f), purple);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.015f * s, 0.17f, 0));
            b.Tube(leg, Smooth(2, V(0.015f * s, 0.17f, 0), V(0.03f * s, 0.11f, 0.01f), V(0.025f * s, 0.06f, 0)), 0.009f, 0.007f, purple, blend: 0f);
            b.Ell(leg, V(0.025f * s, 0.055f, 0), V(0.01f, 0.012f, 0.01f), magenta);
        });
        int tail = b.Tail(V(0, 0.17f, -0.02f));
        Curl(b, tail, V(0, 0.17f, -0.02f), V(0, 0.13f, -0.06f), V(0, 1f, 0), V(0, 0, -1f), 0.025f, 1f, 0.008f, purple);
        int head = b.Head(V(0, 0.24f, 0));
        var c = V(0, 0.3f, 0);
        var r = V(0.065f, 0.07f, 0.06f);
        b.Ell(head, c, r, purple);
        foreach (float y in new[] { 0.03f, 0.05f })
            b.PaintEll(head, c + V(0, y, 0), V(0.075f, 0.006f, 0.07f), magenta);
        b.Spike(head, c + V(0, 0.05f, 0), c + V(0, 0.17f, -0.01f), 0.018f, pale, mat: Shell);
        foreach (var (x, z) in new[] { (-0.045f, 0.02f), (0.045f, 0.02f), (0f, -0.04f) })
            b.Spike(head, c + V(x, -0.04f, z), c + V(x * 1.9f, -0.075f, z * 1.5f + 0.01f), 0.022f, magenta);
        b.PaintEll(head, c + V(0, -0.012f, 0.05f), V(0.05f, 0.025f, 0.02f), magenta);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y - 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(80, 220, 240), glare: true);
        });
        return Lift(b);
    }

    /// <summary>Naganadel: a great dragon of a wasp in flight, its purple abdomen swollen and striped magenta, ending in a long pale stinger behind it, a slender body rising in front, a small fierce head with a long snout, and wide purple wings edged with magenta points.</summary>
    private static PokeBuilder Naganadel()
    {
        var b = new PokeBuilder("Naganadel", 1f, BodyPlan.Bird, V(0, 0.35f, 0)) { Coat = Scales }.Hover();
        var purple = Rgb(120, 76, 170);
        var magenta = Rgb(216, 70, 150);
        var pale = Rgb(200, 180, 230);
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.02f * s, 0.47f, 0.09f);
            int wing = b.Wing(s, shoulder);
            var wrist = V(0.2f * s, 0.62f, 0.03f);
            var tips = new[] { V(0.38f * s, 0.6f, 0.0f), V(0.34f * s, 0.46f, 0.0f), V(0.22f * s, 0.38f, 0.03f) };
            DragonWing(b, wing, shoulder, wrist, tips, V(0.02f * s, 0.4f, 0.09f), purple, purple, 0.012f);
            foreach (var t in tips)
                b.Spike(wing, Vector3.Lerp(wrist, t, 0.85f), t + V(0.02f * s, -0.03f, 0), 0.014f, magenta, mat: Shell);
        });
        // The abdomen, striped, and the stinger behind
        var ac = V(0, 0.3f, -0.06f);
        b.Ell(Body, ac, V(0.08f, 0.075f, 0.16f), purple);
        foreach (float z in new[] { -0.08f, -0.02f, 0.04f })
            b.PaintEll(Body, ac + V(0, 0, z), V(0.09f, 0.085f, 0.008f), magenta);
        b.Spike(Body, ac + V(0, 0, -0.14f), ac + V(0, -0.02f, -0.5f), 0.035f, pale, mat: Shell);
        b.Limb(Body, ac + V(0, 0.02f, 0.11f), V(0, 0.44f, 0.1f), 0.04f, 0.035f, purple);
        b.Ell(Body, V(0, 0.42f, 0.1f), V(0.04f, 0.07f, 0.04f), purple, V(20f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.03f * s, 0.44f, 0.12f));
            b.Limb(arm, V(0.03f * s, 0.44f, 0.12f), V(0.06f * s, 0.38f, 0.17f), 0.012f, 0.01f, purple);
            b.Spike(arm, V(0.06f * s, 0.38f, 0.17f), V(0.07f * s, 0.35f, 0.21f), 0.01f, magenta, mat: Shell);
        });
        int head = b.Head(V(0, 0.48f, 0.12f));
        var c = V(0, 0.53f, 0.15f);
        var r = V(0.035f, 0.035f, 0.045f);
        b.Limb(head, V(0, 0.46f, 0.11f), c, 0.022f, 0.02f, purple);
        b.Ell(head, c, r, purple);
        b.Spike(head, c + V(0, -0.005f, 0.035f), c + V(0, -0.015f, 0.1f), 0.016f, purple);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.015f * s, 0.025f, -0.02f), c + V(0.04f * s, 0.07f, -0.07f), 0.012f, magenta, 0.5f, Shell));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.018f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, Rgb(80, 220, 240), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Stakataka

    /// <summary>Stakataka: a tower of living stones, courses of grey blocks stacked a little out of true, two cyan eyes in dark slots in its face, a cyan eye glinting from many a block, and four legs of stacked blocks.</summary>
    private static PokeBuilder Stakataka()
    {
        var b = new PokeBuilder("Stakataka", 1f, BodyPlan.Quadruped, V(0, 0.5f, 0)) { Coat = Shell };
        var stone = Rgb(120, 120, 126);
        var cyan = Rgb(80, 220, 240);
        // The courses of the tower, each set a little off the one below
        float Front(int i) => 0.13f + (i == 5 ? 0.01f : 0) + ((i * 5) % 3 - 1) * 0.006f;
        float Course(int i) => 0.29f + i * 0.085f;
        for (int i = 0; i < 6; i++)
        {
            float dx = ((i * 7) % 3 - 1) * 0.006f, dz = ((i * 5) % 3 - 1) * 0.006f;
            b.Box(Body, V(dx, Course(i), dz), V(0.15f + (i == 5 ? 0.01f : 0), 0.044f, 0.13f + (i == 5 ? 0.01f : 0)), 0.01f, stone, blend: 0.002f);
        }
        foreach (var (x, i) in new[] { (-0.09f, 0), (0.08f, 1), (-0.06f, 2), (0.1f, 5) })
            b.Mark(Body, V(x, Course(i), Front(i)), V(0, 0, 1f), 0.008f, 0.008f, cyan);
        PokeBuilder.Both(s =>
        {
            var at = V(0.05f * s, Course(4), Front(4));
            b.PaintEll(Body, at, V(0.026f, 0.016f, 0.01f), Rgb(30, 30, 36));
            b.Eye(Body, at, V(0, 0, 1f), 0.012f, cyan, glare: true);
            foreach (var (z, front) in new[] { (0.07f, true), (-0.07f, false) })
            {
                int leg = b.Leg(s, V(0.11f * s, 0.26f, z), front);
                var blocks = new[] { V(0.12f * s, 0.21f, z), V(0.145f * s, 0.125f, z + (front ? 0.02f : -0.02f)), V(0.16f * s, 0.042f, z + (front ? 0.03f : -0.03f)) };
                foreach (var p in blocks)
                {
                    b.Box(leg, p, V(0.044f, 0.044f, 0.044f), 0.006f, stone, blend: 0.002f);
                    b.Mark(leg, p + V(0.044f * s, 0, 0), V(s, 0, 0), 0.008f, 0.008f, cyan);
                }
            }
        });
        return b;
    }

    // ------------------------------------------------------------------ Blacephalon

    /// <summary>Blacephalon: a clown from beyond, its head a great white ball dotted pink and blue with stars of both, sitting on a white cloud of a collar, a thin magenta neck, a body round and striped yellow, blue and pink, puffy white sleeves and thin white legs, a ball of each colour hanging at its feet; no face.</summary>
    private static PokeBuilder Blacephalon()
    {
        var b = new PokeBuilder("Blacephalon", 0.9f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var white = Rgb(244, 242, 240);
        var pink = Rgb(236, 90, 160);
        var blue = Rgb(80, 180, 230);
        var yellow = Rgb(246, 220, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.26f, 0));
            b.Limb(leg, V(0.03f * s, 0.27f, 0), V(0.04f * s, 0.02f, 0.01f), 0.012f, 0.01f, white);
            b.Ell(leg, V(0.04f * s, 0.012f, 0.02f), V(0.016f, 0.012f, 0.024f), white);
            b.Limb(leg, V(0.04f * s, 0.12f, 0.01f), V(0.06f * s, 0.07f, 0.02f), 0.004f, 0.004f, white);
            b.Ell(leg, V(0.065f * s, 0.05f, 0.02f), V(0.022f, 0.022f, 0.022f), s < 0 ? blue : pink, mat: Shell);
        });
        var bc = V(0, 0.32f, 0);
        b.Ell(Body, bc, V(0.06f, 0.075f, 0.055f), yellow);
        foreach (var (y, col) in new[] { (0.04f, blue), (0.0f, pink), (-0.04f, blue) })
            b.PaintEll(Body, bc + V(0, y, 0), V(0.07f, 0.01f, 0.065f), col);
        b.Limb(Body, bc + V(0, 0.06f, 0), V(0, 0.5f, 0), 0.016f, 0.014f, pink);
        // The cloud of a collar
        var cc = V(0, 0.5f, 0);
        b.Ell(Body, cc, V(0.12f, 0.035f, 0.1f), white);
        Lumps(b, Body, cc, V(0.12f, 0.035f, 0.1f), 8, 0.035f, white, white, 10f);
        b.PaintEll(Body, cc + V(0, 0.01f, 0.09f), V(0.025f, 0.015f, 0.02f), Rgb(60, 50, 60));
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.05f * s, 0.06f, 0);
            int arm = b.Arm(s, shoulder);
            for (int i = 0; i < 3; i++)
                b.Ell(arm, shoulder + V((0.03f + i * 0.04f) * s, -0.01f - i * 0.02f, 0.01f + i * 0.01f), V(0.03f, 0.026f, 0.026f), white, blend: 0.015f);
            var hand = shoulder + V(0.15f * s, -0.07f, 0.04f);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Limb(arm, hand, hand + V(0.03f * s, -0.02f + t * 0.015f, 0.01f), 0.008f, 0.007f, white);
        });
        int head = b.Head(cc + V(0, 0.03f, 0));
        var c = V(0, 0.62f, 0);
        var r = V(0.09f, 0.085f, 0.085f);
        b.Limb(head, cc, c, 0.03f, 0.03f, white);
        b.Ell(head, c, r, white);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 14; i++)
        {
            float u = -0.5f + 1.4f * (i + 0.5f) / 14f, ring = MathF.Sqrt(1f - u * u), a = i * golden;
            var at = Out(c, r, default, V(MathF.Sin(a) * ring, u, MathF.Cos(a) * ring));
            b.Mark(head, at, Outward(c, r, at), 0.01f, 0.01f, i % 2 == 0 ? pink : blue);
        }
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.9f * s, 0.1f, 0.3f));
            b.Mark(head, at, Outward(c, r, at), 0.022f, 0.022f, s < 0 ? blue : pink, MarkShape.Star);
        });
        return b;
    }

    // ------------------------------------------------------------------ Zeraora

    /// <summary>
    /// Zeraora and its Mega Evolution: a cat of lightning on two legs, yellow with charcoal limbs, great paws padded blue,
    /// a black mask with blue marks round its eyes, tall ears, a ruff of blue at its chest, zigzags on its thighs and a
    /// long tail ending in a bolt. Mega Zeraora is charcoal all over, cyan on its arms and legs, yellow streaming from its
    /// head and back like lightning.
    /// </summary>
    private static PokeBuilder ZeraoraBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Zeraora-Mega" : "Zeraora", 0.9f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 210, 60);
        var dark = Rgb(56, 58, 70);
        var cyan = Rgb(80, 196, 240);
        var coat = mega ? dark : yellow;
        var limb = mega ? cyan : dark;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.3f, -0.01f));
            var knee = V(0.08f * s, 0.17f, 0.04f);
            var ankle = V(0.07f * s, 0.04f, -0.02f);
            b.Limb(leg, V(0.05f * s, 0.31f, -0.01f), knee, 0.045f, 0.032f, coat);
            b.Limb(leg, knee, ankle, 0.03f, 0.022f, limb);
            b.Ell(leg, ankle + V(0, -0.015f, 0.03f), V(0.03f, 0.022f, 0.05f), dark);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, ankle + V(0.015f * t, -0.02f, 0.075f), ankle + V(0.018f * t, -0.03f, 0.095f), 0.007f, White, mat: Shell);
            if (!mega) b.Mark(leg, V(0.105f * s, 0.24f, 0.015f), V(s, 0, 0.3f), 0.02f, 0.012f, dark, MarkShape.Zigzag, 70f);
        });
        var bc = V(0, 0.4f, 0);
        b.Ell(Body, bc, V(0.07f, 0.11f, 0.06f), coat);
        b.Ell(Body, bc + V(0, 0.06f, 0.035f), V(0.05f, 0.04f, 0.035f), cyan, blend: 0.015f);
        FurTufts(b, Body, bc + V(0, 0.05f, 0.04f), V(0.05f, 0.04f, 0.03f), 8, 0.03f, 0.012f, cyan, 1.2f, -0.6f, 0.7f);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.07f * s, 0.08f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.06f * s, -0.1f, 0.04f);
            var paw = shoulder + V(0.07f * s, -0.21f, 0.09f);
            b.Limb(arm, shoulder, elbow, 0.03f, 0.028f, coat);
            b.Limb(arm, elbow, paw, 0.03f, 0.036f, limb);
            b.Ell(arm, paw, V(0.042f, 0.038f, 0.042f), dark);
            b.PaintEll(arm, paw + V(0, -0.01f, 0.035f), V(0.022f, 0.018f, 0.015f), cyan);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, paw + V(0.018f * t, -0.01f, 0.035f), paw + V(0.022f * t, -0.03f, 0.06f), 0.007f, White, mat: Shell);
        });
        int tail = b.Tail(bc + V(0, -0.08f, -0.05f));
        var tp = Smooth(3, bc + V(0, -0.08f, -0.05f), bc + V(0, -0.12f, -0.18f), bc + V(0, 0.0f, -0.3f), bc + V(0, 0.12f, -0.34f));
        b.Tube(tail, tp, 0.022f, 0.014f, coat, blend: 0f);
        Frond(b, tail, tp[^1], tp[^1] + V(0, 0.1f, 0.04f), 0.03f, yellow, V(1f, 0, 0), 0.25f);
        Frond(b, tail, tp[^1], tp[^1] + V(0, 0.04f, -0.08f), 0.025f, yellow, V(1f, 0, 0), 0.25f);
        int head = b.Head(bc + V(0, 0.12f, 0.03f));
        var c = bc + V(0, 0.18f, 0.05f);
        var r = V(0.055f, 0.05f, 0.05f);
        b.Limb(head, bc + V(0, 0.09f, 0.01f), c, 0.03f, 0.028f, coat);
        b.Ell(head, c, r, coat);
        b.Ell(head, c + V(0, -0.02f, 0.04f), V(0.03f, 0.022f, 0.025f), dark, blend: 0.01f);
        b.PaintEll(head, c + V(0, 0.005f, 0.035f), V(0.06f, 0.02f, 0.03f), dark);
        PokeBuilder.Both(s =>
        {
            FoxEar(b, head, c + V(0.03f * s, 0.035f, -0.01f), c + V(0.07f * s, 0.13f, -0.03f), 0.022f, coat, cyan);
            Frond(b, head, c + V(0.045f * s, -0.015f, 0.0f), c + V(0.1f * s, -0.03f, -0.02f), 0.018f, coat, V(0, 1f, 0.2f), 0.25f);
            var at = On(c, r, 0.022f * s, c.Y + 0.008f);
            b.Mark(head, On(c, r, 0.042f * s, c.Y + 0.012f), Outward(c, r, On(c, r, 0.042f * s, c.Y + 0.012f)), 0.01f, 0.004f, cyan, MarkShape.Bar, 30f * s);
            b.Eye(head, at, Outward(c, r, at), 0.01f, cyan, glare: true);
        });
        if (mega)
            // Lightning streaming from its head and back
            foreach (var (x, up, back) in new[] { (-0.03f, 0.06f, 0.2f), (0.03f, 0.06f, 0.2f), (0f, 0.1f, 0.26f) })
                Frond(b, head, c + V(x, 0.03f, -0.03f), c + V(x * 3f, 0.03f + up, -0.03f - back), 0.03f, yellow, V(1f, 0.3f, 0), 0.22f);
        return b;
    }

    private static PokeBuilder Zeraora() => ZeraoraBuild(false);

    // ------------------------------------------------------------------ Meltan and Melmetal

    /// <summary>A hex nut of gold, its six sides slabs round <paramref name="c"/>, facing along z, <paramref name="apothem"/> from its middle to each side.</summary>
    private static void HexNut(PokeBuilder b, int bone, Vector3 c, float apothem, float wall, float depth, Color color)
    {
        // Each side reaches exactly to the corners, where the next one takes over
        float half = apothem * MathF.Tan(30f * Degree);
        for (int i = 0; i < 6; i++)
        {
            float a = i * 60f + 30f;
            var d = V(MathF.Cos(a * Degree), MathF.Sin(a * Degree), 0);
            b.Box(bone, c + d * (apothem - wall / 2f), V(half, wall / 2f, depth), 0.004f, color, V(0, 0, a - 90f), Metal, 0.002f);
        }
    }

    /// <summary>Meltan: a hex nut of gold for a head with its one dark eye looking out of the hole, on a puddle of liquid silver that drips, a thin tail tipped red.</summary>
    private static PokeBuilder Meltan()
    {
        var b = new PokeBuilder("Meltan", 0.4f, BodyPlan.Floating, V(0, 0.08f, 0)) { Coat = Metal };
        var gold = Rgb(230, 190, 80);
        var silver = Rgb(212, 216, 224);
        b.Ell(Body, V(0, 0.05f, 0), V(0.07f, 0.045f, 0.06f), silver);
        foreach (var (x, z) in new[] { (-0.06f, 0.03f), (0.05f, 0.04f), (0.07f, -0.02f), (-0.04f, -0.05f) })
            b.Ell(Body, V(x, 0.015f, z), V(0.03f, 0.015f, 0.03f), silver, blend: 0.015f);
        int tail = b.Tail(V(0, 0.04f, -0.05f));
        var tp = Smooth(3, V(0, 0.04f, -0.05f), V(-0.03f, 0.03f, -0.1f), V(-0.07f, 0.05f, -0.11f));
        b.Tube(tail, tp, 0.006f, 0.005f, Rgb(70, 70, 80), blend: 0f);
        b.Ell(tail, tp[^1], V(0.01f, 0.01f, 0.01f), Rgb(220, 60, 60));
        int head = b.Head(V(0, 0.09f, 0));
        var c = V(0, 0.155f, 0);
        HexNut(b, head, c, 0.075f, 0.03f, 0.03f, gold);
        b.Ell(head, c, V(0.05f, 0.05f, 0.03f), Rgb(60, 60, 70));
        b.Eye(head, c + V(0, 0, 0.03f), V(0, 0, 1f), 0.028f, Rgb(40, 40, 50));
        return b;
    }

    /// <summary>
    /// Melmetal and its Gigantamax form: a giant of liquid metal, a body of silver on short legs, a gold hex nut for a
    /// head with one eye in it, and great arms of dark nuts strung one on another to fists of heavier ones. Gigantamax
    /// Melmetal is molten, darker and heavier still, its fists swollen, Gigantamax's red clouds rising from its head.
    /// </summary>
    private static PokeBuilder MelmetalBuild(bool gmax)
    {
        var b = new PokeBuilder(gmax ? "Melmetal-Gmax" : "Melmetal", 1f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Metal };
        var silver = gmax ? Rgb(170, 172, 182) : Rgb(212, 216, 224);
        var nut = Rgb(80, 82, 92);
        var gold = Rgb(230, 190, 80);
        float k = gmax ? 1.3f : 1f;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.22f, 0));
            b.Limb(leg, V(0.08f * s, 0.24f, 0), V(0.1f * s, 0.06f, 0.02f), 0.06f, 0.05f * k, silver);
            b.Ell(leg, V(0.1f * s, 0.035f, 0.03f), V(0.06f, 0.035f, 0.07f) * k, silver);
        });
        var bc = V(0, 0.36f, 0);
        b.Ell(Body, bc, V(0.13f, 0.15f, 0.11f), silver);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.08f), V(0.07f, 0.09f, 0.04f), gmax ? Rgb(60, 60, 70) : Rgb(236, 238, 244));
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.13f * s, 0.08f, 0);
            int arm = b.Arm(s, shoulder);
            var fist = shoulder + V(0.12f * s, -0.18f, 0.08f);
            b.Limb(arm, shoulder, fist, 0.035f, 0.035f, silver);
            foreach (var t in new[] { 0.25f, 0.55f })
            {
                var at = Vector3.Lerp(shoulder, fist, t);
                b.Box(arm, at, V(0.05f, 0.045f, 0.05f) * k, 0.012f, nut, V(0, 0, 30f * s));
            }
            b.Ell(arm, fist, V(0.08f, 0.075f, 0.08f) * k, nut);
            b.Box(arm, fist + V(0, 0, 0.02f), V(0.075f, 0.065f, 0.065f) * k, 0.02f, nut);
        });
        int head = b.Head(bc + V(0, 0.14f, 0.02f));
        var c = bc + V(0, 0.22f, 0.03f);
        b.Limb(head, bc + V(0, 0.1f, 0.02f), c, 0.05f, 0.04f, silver);
        HexNut(b, head, c, 0.065f, 0.026f, 0.028f, gold);
        b.Ell(head, c, V(0.045f, 0.045f, 0.028f), Rgb(60, 60, 70));
        b.Eye(head, c + V(0, 0, 0.028f), V(0, 0, 1f), 0.024f, Rgb(40, 40, 50));
        if (gmax)
            MaxClouds(b, head, c + V(0, 0.06f, -0.04f), 0.05f, 0.14f, 0.24f, 1.1f, 0.4f);
        return b;
    }

    private static PokeBuilder Melmetal() => MelmetalBuild(false);
}
