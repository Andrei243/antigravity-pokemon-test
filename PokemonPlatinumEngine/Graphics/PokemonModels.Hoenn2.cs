using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Hoenn's second batch in National Pokédex
// order: Sableye (302) to Armaldo (348), but for the species of the Sinnoh Pokédex in that range, hand-built before.
// Their forms are in PokemonModels.Megas.cs with the other forms. Helpers shared with the earlier batches are in
// PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs, PokemonModels.Kanto1.cs to PokemonModels.Kanto3.cs,
// PokemonModels.Johto1.cs, PokemonModels.Johto2.cs and PokemonModels.Hoenn1.cs.
internal static partial class PokemonModels
{
    /// <summary>A cut gem: an octahedron of a rounded box turned on its point, faceted by a paler ring on its face.</summary>
    private static void Gem(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float r, Color color)
    {
        var turn = Euler(facing) + V(90f, 0, 0);
        b.Ell(bone, at, V(r, r, r * 0.45f), color, turn, Glow, 0.006f);
        b.PaintTorus(bone, at + Vector3.Normalize(facing) * r * 0.25f, r * 0.6f, r * 0.12f, PixelCanvas.Mix(color, White, 0.5f), turn);
    }

    // ------------------------------------------------------------------ Sableye

    /// <summary>
    /// Sableye and its Mega Evolution: a crouching purple imp with gems for eyes, a red gem on its chest and clawed
    /// hands; the Mega holds up its chest's gem grown into a great red shield.
    /// </summary>
    private static PokeBuilder SableyeBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Sableye-Mega" : "Sableye", 0.65f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Fur };
        var purple = Rgb(110, 84, 168);
        var dark = Rgb(70, 52, 112);
        var gem = Rgb(176, 226, 236);
        var red = Rgb(220, 40, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.16f, -0.01f));
            var knee = V(0.11f * s, 0.14f, 0.05f);
            b.Limb(leg, V(0.06f * s, 0.17f, -0.01f), knee, 0.032f, 0.026f, purple);
            b.Limb(leg, knee, V(0.1f * s, 0.03f, 0.02f), 0.024f, 0.02f, purple);
            Digits(b, leg, V(0.1f * s, 0.018f, 0.04f), V(0.2f * s, 0, 1f), V(0.6f, 0, 0), 0.04f, 0.01f, purple);
        });
        var bc = V(0, 0.23f, 0.01f);
        var br = V(0.075f, 0.09f, 0.065f);
        b.Ell(Body, bc, br, purple, V(15f, 0, 0));
        if (!mega) Gem(b, Body, Out(bc, br, default, V(0, -0.1f, 1f)), V(0, -0.1f, 1f), 0.022f, red);
        // Long arms hanging, three claws on each hand; the Mega's left hand bears its shield
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.29f, 0.02f));
            var hand = mega && s < 0 ? V(0.12f * s, 0.3f, 0.12f) : V(0.15f * s, 0.15f, 0.08f);
            b.Limb(arm, V(0.06f * s, 0.29f, 0.02f), hand, 0.022f, 0.018f, purple);
            Digits(b, arm, hand, V(0.3f * s, -0.6f, 0.6f), V(0.5f, 0, 0.2f), 0.045f, 0.009f, purple);
            if (mega && s < 0)
            {
                var sc = V(-0.13f, 0.24f, 0.16f);
                b.Limb(arm, hand, sc, 0.02f, 0.02f, purple);
                b.Ell(arm, sc, V(0.13f, 0.18f, 0.04f), red, V(0, 30f, 0), Glow, 0.02f);
                b.PaintTorus(arm, sc + V(0.015f, 0, 0.03f), 0.08f, 0.012f, Rgb(250, 140, 160), V(90f, 30f, 0), 1f, 1.3f);
            }
        });
        // A broad head with a jagged mouth, pointed ears and gems for eyes
        int head = b.Head(V(0, 0.31f, 0.03f));
        var c = V(0, 0.38f, 0.05f);
        var r = V(0.09f, 0.07f, 0.075f);
        b.Ell(head, c, r, purple);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.07f * s, 0.42f, 0.03f));
            Blade(b, ear, V(0.06f * s, 0.42f, 0.03f), V(0.15f * s, 0.5f, -0.01f), 0.035f, purple, V(0, 0.2f, 1f), 0.3f);
            var at = On(c, r, 0.04f * s, 0.39f);
            b.Mark(head, at, Outward(c, r, at), 0.026f, 0.026f, gem, MarkShape.Diamond);
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, white: gem, pupil: Rgb(90, 160, 190));
        });
        b.Mark(head, On(c, r, 0, 0.35f), V(0, -0.3f, 1f), 0.05f, 0.012f, dark, MarkShape.Zigzag);
        return b;
    }

    private static PokeBuilder Sableye() => SableyeBuild(false);

    // ------------------------------------------------------------------ Mawile

    /// <summary>
    /// Mawile and its Mega Evolution: a small yellow deceiver in a skirt of fur, a great black jaw growing from the
    /// back of its head and hanging behind it; the Mega's two jaws, tipped purple, and its dress pink.
    /// </summary>
    private static PokeBuilder MawileBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Mawile-Mega" : "Mawile", 0.6f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 220, 140);
        var black = Rgb(56, 54, 62);
        var dress = mega ? Rgb(210, 110, 160) : yellow;
        var gum = Rgb(200, 90, 120);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.08f, 0));
            b.Ell(leg, V(0.045f * s, 0.025f, 0.02f), V(0.03f, 0.025f, 0.045f), black);
        });
        // A skirt of fur flaring to its feet, a slim body above it
        b.Spike(Body, V(0, 0.25f, 0), V(0, 0.03f, 0), 0.11f, dress, blend: 0.02f);
        b.Ell(Body, V(0, 0.08f, 0), V(0.1f, 0.05f, 0.09f), dress, blend: 0.03f);
        b.Ell(Body, V(0, 0.22f, 0), V(0.05f, 0.07f, 0.045f), yellow, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.04f * s, 0.25f, 0));
            var hand = V(0.1f * s, 0.18f, 0.04f);
            b.Limb(arm, V(0.04f * s, 0.25f, 0), hand, 0.016f, 0.014f, yellow);
            b.Ell(arm, hand, V(0.022f, 0.022f, 0.022f), black);
        });
        // A round head with ruby eyes, the black jaw rising behind it and curving over and down at its back
        int head = b.Head(V(0, 0.3f, 0.01f));
        var c = V(0, 0.36f, 0.02f);
        var r = V(0.065f, 0.065f, 0.06f);
        b.Ell(head, c, r, yellow);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, 0.365f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(200, 50, 60));
        });
        int count = mega ? 2 : 1;
        for (int k = 0; k < count; k++)
        {
            float side = count == 1 ? 0f : (k == 0 ? -1f : 1f);
            int jaw = b.Part(k == 0 ? "maw" : "maw2", head, c + V(0.02f * side, 0.04f, -0.04f), PokeRole.Tail, 0.5f * k);
            var path = Smooth(3, c + V(0.02f * side, 0.04f, -0.04f), c + V(0.05f * side, 0.14f, -0.1f), c + V(0.08f * side, 0.1f, -0.22f), c + V(0.1f * side, -0.08f, -0.28f));
            b.Tube(jaw, path, 0.035f, 0.05f, black, blend: 0f);
            var mouth = path[^1] + V(0.01f * side, -0.08f, -0.02f);
            var dir = Vector3.Normalize(mouth - path[^1]);
            b.Ell(jaw, mouth, V(0.08f, 0.1f, 0.07f), black, Euler(dir) + V(90f, 0, 0), blend: 0.02f);
            b.Cut(jaw, mouth + V(0, -0.04f, 0.02f) + dir * 0.06f, V(0.07f, 0.03f, 0.05f), Euler(dir) + V(90f, 0, 0));
            b.PaintEll(jaw, mouth + dir * 0.05f, V(0.07f, 0.04f, 0.06f), gum, Euler(dir) + V(90f, 0, 0));
            if (mega) b.PaintEll(jaw, mouth + dir * 0.08f, V(0.09f, 0.06f, 0.08f), Rgb(120, 70, 150));
            b.Mark(jaw, mouth + V(0, 0.06f, 0.06f), V(0, 0.3f, 1f), 0.03f, 0.04f, Rgb(246, 210, 90));
            PokeBuilder.Both(s => b.Spike(jaw, mouth + dir * 0.06f + V(0.03f * s, 0.02f, 0.04f), mouth + dir * 0.06f + V(0.03f * s, -0.02f, 0.05f), 0.01f, White, mat: Shell, blend: 0.003f));
        }
        return b;
    }

    private static PokeBuilder Mawile() => MawileBuild(false);

    // ------------------------------------------------------------------ Aron line

    /// <summary>Aron: a little iron armadillo, its head and back plated grey with black holes, a dark body and stubby legs.</summary>
    private static PokeBuilder Aron()
    {
        var b = new PokeBuilder("Aron", 0.5f, BodyPlan.Quadruped, V(0, 0.15f, 0)) { Coat = Metal };
        var steel = Rgb(206, 210, 216);
        var dark = Rgb(70, 74, 86);
        StubbyLegs(b, 0.07f, 0.09f, 0.05f, -0.06f, 0.035f, steel);
        var bc = V(0, 0.14f, -0.02f);
        var br = V(0.1f, 0.09f, 0.11f);
        b.Ell(Body, bc, br, dark);
        // A plate over its back with a ridge, and its head a big steel helmet
        b.Ell(Body, V(0, 0.18f, -0.04f), V(0.09f, 0.08f, 0.09f), steel, blend: 0.01f);
        b.Spike(Body, V(0, 0.23f, -0.04f), V(0, 0.3f, -0.08f), 0.045f, steel, 0.4f);
        int head = b.Head(V(0, 0.16f, 0.06f));
        var c = V(0, 0.18f, 0.1f);
        var r = V(0.1f, 0.09f, 0.08f);
        b.Ell(head, c, r, steel);
        foreach (var (x, y) in new[] { (0.07f, 0.22f), (-0.07f, 0.22f), (0f, 0.24f) })
        {
            var n = V(x / r.X, (y - c.Y) / r.Y, 0.6f);
            b.Mark(head, Out(c, r, default, n), n, 0.016f, 0.016f, Black);
        }
        PokeBuilder.Both(s =>
        {
            var look = V(0.5f * s, -0.1f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.018f, Rgb(80, 170, 220));
        });
        return b;
    }

    /// <summary>Lairon: a stockier iron armadillo, its back plates rising in ridges, spotted black, spiked cuffs on its feet.</summary>
    private static PokeBuilder Lairon()
    {
        var b = new PokeBuilder("Lairon", 0.7f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Metal };
        var steel = Rgb(206, 210, 216);
        var dark = Rgb(70, 74, 86);
        StubbyLegs(b, 0.1f, 0.12f, 0.1f, -0.1f, 0.045f, dark, steel);
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.1f, -0.1f })
                b.Torus(Body, V(0.11f * s, 0.06f, z + 0.02f), 0.045f, 0.012f, steel, mat: Metal);
        });
        var bc = V(0, 0.2f, -0.01f);
        var br = V(0.13f, 0.1f, 0.18f);
        b.Ell(Body, bc, br, dark);
        b.Ell(Body, V(0, 0.24f, -0.02f), V(0.135f, 0.09f, 0.18f), steel, blend: 0.01f);
        for (int i = 0; i < 4; i++)
            Blade(b, Body, V(0, 0.3f, 0.08f - i * 0.07f), V(0, 0.38f - i * 0.01f, 0.03f - i * 0.07f), 0.05f, steel, V(1f, 0, 0), 0.3f, Metal);
        foreach (var (x, z) in new[] { (0.1f, 0.05f), (-0.1f, 0.05f), (0.11f, -0.08f), (-0.11f, -0.08f), (0.05f, -0.15f), (-0.05f, -0.15f) })
        {
            var n = V(x / 0.135f, 0.6f, z / 0.18f);
            b.Mark(Body, Out(V(0, 0.24f, -0.02f), V(0.135f, 0.09f, 0.18f), default, n), n, 0.018f, 0.018f, Black);
        }
        int head = b.Head(V(0, 0.2f, 0.15f));
        var c = V(0, 0.2f, 0.2f);
        var r = V(0.09f, 0.075f, 0.08f);
        b.Ell(head, c, r, steel);
        Grin(b, head, On(c, r, 0, 0.17f), V(0.045f, 0.02f, 0.02f), Rgb(170, 80, 100));
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.05f * s, 0.05f, -0.02f), c + V(0.08f * s, 0.11f, -0.08f), 0.025f, steel, mat: Metal);
            var look = V(0.5f * s, 0.15f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.016f, Rgb(80, 170, 220), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Aggron and its Mega Evolution: an upright iron rhino, a steel helmet with two great horns, dark plated body, a
    /// long tail; the Mega all steel, plated head to foot, its horns joined into one blade.
    /// </summary>
    private static PokeBuilder AggronBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Aggron-Mega" : "Aggron", 1f, BodyPlan.Biped, V(0, 0.48f, 0)) { Coat = Metal };
        var steel = Rgb(210, 214, 222);
        var dark = mega ? Rgb(150, 156, 168) : Rgb(70, 74, 86);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.3f, -0.02f));
            b.Limb(leg, V(0.1f * s, 0.32f, -0.02f), V(0.12f * s, 0.06f, 0.0f), 0.075f, 0.06f, dark);
            b.Ell(leg, V(0.12f * s, 0.04f, 0.03f), V(0.07f, 0.04f, 0.08f), steel);
            b.PaintTorus(leg, V(0.12f * s, 0.12f, 0), 0.065f, 0.02f, steel);
            foreach (float x in new[] { -0.03f, 0.03f })
                b.Spike(leg, V(0.12f * s + x, 0.03f, 0.1f), V(0.12f * s + x, 0.015f, 0.13f), 0.014f, White, mat: Shell, blend: 0.004f);
        });
        var bc = V(0, 0.5f, 0);
        var br = V(0.15f, 0.2f, 0.13f);
        b.Ell(Body, bc, br, dark);
        b.PaintEll(Body, V(0, 0.48f, 0.08f), V(0.12f, 0.15f, 0.07f), mega ? steel : Rgb(90, 94, 108));
        foreach (float y in new[] { 0.42f, 0.5f, 0.58f })
            b.PaintTorus(Body, V(0, y, 0.0f), 0.14f, 0.005f, Rgb(40, 42, 52), sz: 0.9f);
        // Steel plates on its shoulders and back
        PokeBuilder.Both(s => b.Ell(Body, V(0.12f * s, 0.64f, -0.02f), V(0.08f, 0.06f, 0.09f), steel, blend: 0.015f));
        for (int i = 0; i < (mega ? 4 : 3); i++)
            Blade(b, Body, V(0, 0.62f - i * 0.08f, -0.1f), V(0, 0.66f - i * 0.08f, -0.2f), 0.05f, steel, V(1f, 0, 0), 0.3f, Metal);
        int tail = b.Tail(V(0, 0.36f, -0.12f));
        b.Tube(tail, Smooth(3, V(0, 0.36f, -0.1f), V(0, 0.22f, -0.28f), V(0, 0.12f, -0.42f)), 0.07f, 0.03f, dark, blend: 0f);
        if (mega)
            for (int i = 0; i < 3; i++)
                b.PaintTorus(tail, V(0, 0.28f - i * 0.06f, -0.2f - i * 0.08f), 0.055f - i * 0.008f, 0.012f, steel, V(50f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.14f * s, 0.6f, 0));
            var hand = V(0.22f * s, 0.42f, 0.12f);
            b.Limb(arm, V(0.14f * s, 0.6f, 0), hand, 0.05f, 0.045f, dark);
            b.PaintTorus(arm, Vector3.Lerp(V(0.14f * s, 0.6f, 0), hand, 0.65f), 0.048f, 0.016f, steel, Euler(hand - V(0.14f * s, 0.6f, 0)));
            b.Ell(arm, hand, V(0.05f, 0.05f, 0.05f), dark);
            Digits(b, arm, hand + V(0, -0.02f, 0.03f), V(0, -0.5f, 1f), V(0.5f, 0, 0), 0.04f, 0.012f, White, Shell);
        });
        // A steel helmet with two horns swept back, blue eyes, a mouth with fangs
        int head = b.Head(V(0, 0.68f, 0.06f));
        var c = V(0, 0.76f, 0.1f);
        var r = V(0.09f, 0.08f, 0.11f);
        b.Ell(head, c, r, steel);
        b.Ell(head, V(0, 0.72f, 0.16f), V(0.06f, 0.04f, 0.06f), dark, blend: 0.02f);
        if (mega)
            Blade(b, head, c + V(0, 0.04f, 0.06f), c + V(0, 0.14f, -0.22f), 0.08f, steel, V(1f, 0, 0), 0.3f, Metal);
        else
            PokeBuilder.Both(s => b.Spike(head, c + V(0.04f * s, 0.05f, 0.04f), c + V(0.08f * s, 0.16f, -0.18f), 0.035f, steel, 0.6f, Metal));
        b.Spike(head, c + V(0, 0.02f, 0.09f), c + V(0, 0.07f, 0.17f), 0.026f, steel, 0.6f, Metal);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.03f * s, 0.7f, 0.2f), V(0.03f * s, 0.67f, 0.205f), 0.01f, White, mat: Shell, blend: 0.003f);
            var at = On(c, r, 0.045f * s, 0.77f);
            b.Mark(head, at, Outward(c, r, at), 0.022f, 0.016f, Black);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(80, 170, 220), glare: true);
        });
        return b;
    }

    private static PokeBuilder Aggron() => AggronBuild(false);

    // ------------------------------------------------------------------ Electrike line

    /// <summary>Electrike: a lean green dog with yellow lightning across its brow, spiky yellow tail and a fierce face.</summary>
    private static PokeBuilder Electrike()
    {
        var b = new PokeBuilder("Electrike", 0.6f, BodyPlan.Quadruped, V(0, 0.18f, 0)) { Coat = Fur };
        var green = Rgb(124, 192, 108);
        var yellow = Rgb(246, 220, 80);
        foreach (var (z, front) in new[] { (0.08f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.055f * s, 0.15f, z), front);
                b.Limb(leg, V(0.055f * s, 0.16f, z), V(0.06f * s, 0.03f, z + 0.01f), 0.03f, 0.022f, green);
                b.Ell(leg, V(0.06f * s, 0.02f, z + 0.025f), V(0.025f, 0.02f, 0.035f), green);
            });
        var bc = V(0, 0.18f, -0.01f);
        var br = V(0.075f, 0.07f, 0.14f);
        b.Ell(Body, bc, br, green);
        int tail = b.Tail(V(0, 0.2f, -0.14f));
        b.Limb(tail, V(0, 0.2f, -0.13f), V(0, 0.24f, -0.2f), 0.02f, 0.015f, green);
        Blade(b, tail, V(0, 0.24f, -0.2f), V(0, 0.34f, -0.24f), 0.04f, yellow, V(1f, 0, 0), 0.3f);
        Blade(b, tail, V(0, 0.27f, -0.21f), V(0, 0.3f, -0.3f), 0.03f, yellow, V(1f, 0, 0), 0.3f);
        int head = b.Head(V(0, 0.22f, 0.1f));
        var c = V(0, 0.24f, 0.15f);
        var r = V(0.075f, 0.06f, 0.08f);
        b.Ell(head, c, r, green);
        b.Ell(head, V(0, 0.22f, 0.22f), V(0.04f, 0.03f, 0.04f), yellow, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.27f, 0.1f));
            Blade(b, ear, V(0.04f * s, 0.27f, 0.11f), V(0.1f * s, 0.29f, -0.02f), 0.04f, green, V(0, 1f, 0.3f), 0.25f);
            b.Mark(head, c + V(0.04f * s, 0.045f, 0.04f), V(0.4f * s, 0.8f, 0.6f), 0.03f, 0.01f, yellow, MarkShape.Zigzag, 20f * s);
            var at = On(c, r, 0.035f * s, 0.255f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(246, 200, 60), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Manectric and its Mega Evolution: a lean blue wolf, yellow crest and mane, a spiky tail; the Mega's crest and
    /// mane grown into great yellow lightning blades sweeping back.
    /// </summary>
    private static PokeBuilder ManectricBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Manectric-Mega" : "Manectric", 0.85f, BodyPlan.Quadruped, V(0, 0.32f, 0)) { Coat = Fur };
        var blue = Rgb(84, 156, 210);
        var yellow = Rgb(248, 226, 100);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.28f, z), front);
                var knee = V(0.065f * s, 0.14f, z + (front ? 0.01f : -0.03f));
                b.Limb(leg, V(0.06f * s, 0.3f, z), knee, 0.04f, 0.026f, blue);
                b.Limb(leg, knee, V(0.065f * s, 0.035f, z + 0.01f), 0.024f, 0.02f, blue);
                b.Ell(leg, V(0.065f * s, 0.02f, z + 0.025f), V(0.026f, 0.02f, 0.035f), yellow);
            });
        var bc = V(0, 0.32f, -0.01f);
        var br = V(0.085f, 0.085f, 0.17f);
        b.Ell(Body, bc, br, blue);
        // A yellow mane over its shoulders, sweeping back in points
        int n = mega ? 6 : 5;
        for (int i = 0; i < n; i++)
        {
            float x = (i - (n - 1) / 2f) / ((n - 1) / 2f);
            var root = V(x * 0.06f, 0.38f, 0.08f);
            Blade(b, Body, root, root + V(x * 0.1f, mega ? 0.14f : 0.06f, mega ? -0.26f : -0.14f), mega ? 0.05f : 0.04f, yellow, V(0, 1f, 0.2f), 0.25f);
        }
        b.PaintEll(Body, V(0, 0.37f, 0.12f), V(0.08f, 0.06f, 0.06f), yellow);
        int tail = b.Tail(V(0, 0.34f, -0.17f));
        Blade(b, tail, V(0, 0.34f, -0.16f), V(0, 0.46f, -0.3f), 0.05f, yellow, V(1f, 0, 0), 0.25f);
        Blade(b, tail, V(0, 0.38f, -0.2f), V(0, 0.36f, -0.36f), 0.04f, blue, V(1f, 0, 0), 0.25f);
        // A long muzzle, red eyes, a yellow crest rising from its brow
        int head = b.Head(V(0, 0.42f, 0.15f));
        var c = V(0, 0.46f, 0.19f);
        var r = V(0.065f, 0.06f, 0.075f);
        b.Limb(head, V(0, 0.36f, 0.12f), c, 0.05f, 0.045f, blue);
        b.Ell(head, c, r, blue);
        b.Ell(head, V(0, 0.43f, 0.27f), V(0.04f, 0.035f, 0.06f), blue, blend: 0.02f);
        b.Ell(head, V(0, 0.44f, 0.325f), V(0.016f, 0.012f, 0.01f), Black, mat: Shell, blend: 0.005f);
        Blade(b, head, c + V(0, 0.04f, 0.04f), c + V(0, mega ? 0.24f : 0.16f, mega ? -0.16f : -0.06f), 0.05f, yellow, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.04f * s, 0.03f, -0.01f), c + V(0.1f * s, mega ? 0.14f : 0.08f, -0.12f), 0.035f, yellow, V(0, 0.3f, 1f), 0.3f);
            var at = On(c, r, 0.035f * s, 0.47f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(220, 50, 60), glare: true);
        });
        return b;
    }

    private static PokeBuilder Manectric() => ManectricBuild(false);

    // ------------------------------------------------------------------ Plusle and Minun

    /// <summary>Plusle and Minun: a cream mouse with long ears and cheeks, a plus or minus on each, red for Plusle and blue for Minun.</summary>
    private static PokeBuilder CheerMouse(string name, Color tint, bool plus)
    {
        var b = new PokeBuilder(name, 0.5f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var cream = Rgb(250, 238, 180);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.1f, 0));
            b.Limb(leg, V(0.045f * s, 0.1f, 0), V(0.05f * s, 0.03f, 0.02f), 0.026f, 0.022f, cream);
            b.Ell(leg, V(0.05f * s, 0.02f, 0.03f), V(0.026f, 0.02f, 0.036f), cream);
        });
        var bc = V(0, 0.15f, 0);
        var br = V(0.065f, 0.075f, 0.06f);
        b.Ell(Body, bc, br, cream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.18f, 0.01f));
            var hand = s > 0 ? V(0.1f * s, 0.27f, 0.04f) : V(0.09f * s, 0.12f, 0.05f);
            b.Limb(arm, V(0.05f * s, 0.18f, 0.01f), hand, 0.018f, 0.016f, tint);
            b.Ell(arm, hand, V(0.02f, 0.02f, 0.02f), tint);
        });
        // A tail ending in the sign, flat
        int tail = b.Tail(V(0, 0.1f, -0.05f));
        b.Tube(tail, Smooth(3, V(0, 0.1f, -0.05f), V(0.03f, 0.08f, -0.12f), V(0.05f, 0.14f, -0.16f)), 0.008f, 0.007f, cream, blend: 0f);
        var sign = V(0.06f, 0.18f, -0.17f);
        b.Limb(tail, V(0.05f, 0.14f, -0.16f), sign, 0.008f, 0.008f, cream);
        b.Box(tail, sign, V(0.04f, 0.012f, 0.008f), 0.004f, tint, mat: Shell, blend: 0.004f);
        if (plus) b.Box(tail, sign, V(0.012f, 0.04f, 0.008f), 0.004f, tint, mat: Shell, blend: 0.004f);
        // A big round head, long ears in the colour, cheeks marked with the sign
        int head = b.Head(V(0, 0.22f, 0.01f));
        var c = V(0, 0.29f, 0.02f);
        var r = V(0.09f, 0.08f, 0.08f);
        b.Ell(head, c, r, cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.35f, 0.0f));
            var tip = V(0.13f * s, 0.5f, -0.02f);
            b.Ell(ear, Vector3.Lerp(V(0.05f * s, 0.35f, 0), tip, 0.5f), V(0.035f, 0.1f, 0.018f), tint, Euler(tip - V(0.05f * s, 0.35f, 0)) + V(0, 0, 0), blend: 0.02f);
            var cheek = On(c, r, 0.06f * s, 0.26f);
            b.Mark(head, cheek, Outward(c, r, cheek), 0.024f, 0.024f, tint);
            b.Mark(head, cheek + Outward(c, r, cheek) * 0.001f, Outward(c, r, cheek), 0.012f, plus ? 0.012f : 0.004f, plus ? cream : cream, plus ? MarkShape.Star : MarkShape.Bar);
            var at = On(c, r, 0.035f * s, 0.3f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(40, 36, 50));
        });
        b.Mark(head, On(c, r, 0, 0.265f), V(0, -0.2f, 1f), 0.015f, 0.007f, Rgb(170, 80, 80), MarkShape.Smile);
        return b;
    }

    private static PokeBuilder Plusle() => CheerMouse("Plusle", Rgb(230, 70, 70), true);

    private static PokeBuilder Minun() => CheerMouse("Minun", Rgb(80, 150, 230), false);

    // ------------------------------------------------------------------ Volbeat and Illumise

    /// <summary>Volbeat: a firefly, black with a red helmet over its head and yellow bands on its round tail, clear wings and two curly feelers.</summary>
    private static PokeBuilder Volbeat()
    {
        var b = new PokeBuilder("Volbeat", 0.62f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Shell };
        var black = Rgb(56, 54, 66);
        var gray = Rgb(130, 136, 160);
        var red = Rgb(220, 60, 60);
        var yellow = Rgb(246, 210, 80);
        var clear = Rgb(196, 214, 240);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.12f, 0));
            b.Limb(leg, V(0.05f * s, 0.13f, 0), V(0.06f * s, 0.03f, 0.02f), 0.024f, 0.02f, black);
            b.Ell(leg, V(0.06f * s, 0.022f, 0.03f), V(0.03f, 0.022f, 0.04f), black);
        });
        // A round tail of grey with yellow bands, glowing at its end
        b.Ell(Body, V(0, 0.16f, -0.06f), V(0.08f, 0.08f, 0.09f), gray);
        foreach (float z in new[] { -0.03f, -0.08f })
            b.PaintTorus(Body, V(0, 0.16f, z), 0.075f, 0.01f, yellow, V(90f, 0, 0));
        b.Ell(Body, V(0, 0.14f, -0.14f), V(0.04f, 0.04f, 0.03f), yellow, mat: Glow, blend: 0.01f);
        b.Ell(Body, V(0, 0.28f, 0.01f), V(0.06f, 0.08f, 0.055f), black, blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.31f, 0.02f));
            var hand = V(0.1f * s, 0.22f, 0.07f);
            b.Limb(arm, V(0.05f * s, 0.31f, 0.02f), hand, 0.016f, 0.014f, black);
            b.Ell(arm, hand, V(0.022f, 0.022f, 0.022f), black);
            int wing = b.Wing(s, V(0.02f * s, 0.31f, -0.03f));
            Frond(b, wing, V(0.02f * s, 0.31f, -0.03f), V(0.15f * s, 0.3f, -0.12f), 0.05f, clear, V(0, 0.3f, 1f), 0.15f);
        });
        // A helmet of red over its head, round yellow eyes and curly feelers
        int head = b.Head(V(0, 0.36f, 0.02f));
        var c = V(0, 0.42f, 0.03f);
        var r = V(0.075f, 0.065f, 0.07f);
        b.Ell(head, c, r, black);
        b.Ell(head, c + V(0, 0.03f, -0.015f), V(0.09f, 0.06f, 0.075f), red, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            int horn = b.Ear(head, s, c + V(0.03f * s, 0.07f, 0));
            var spiral = Spiral(c + V(0.07f * s, 0.18f, 0), V(1f, 0, 0), V(0, 1f, 0), 0.03f, 0.01f, 0f, MathF.Tau * 1.2f, 12);
            b.Tube(horn, new[] { c + V(0.02f * s, 0.07f, 0) }.Concat(spiral).ToArray(), 0.008f, 0.006f, black, blend: 0f);
            var at = On(c, r, 0.035f * s, 0.41f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, sclera: true, white: yellow, pupil: Rgb(40, 36, 50));
        });
        return b;
    }

    /// <summary>Illumise: a firefly, its face pale blue, a purple hood over its head, a black dress and two straight feelers.</summary>
    private static PokeBuilder Illumise()
    {
        var b = new PokeBuilder("Illumise", 0.62f, BodyPlan.Biped, V(0, 0.26f, 0)) { Coat = Shell };
        var black = Rgb(56, 54, 66);
        var blue = Rgb(130, 196, 232);
        var purple = Rgb(170, 140, 210);
        var yellow = Rgb(246, 210, 80);
        var clear = Rgb(196, 214, 240);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.08f, 0));
            b.Limb(leg, V(0.04f * s, 0.09f, 0), V(0.045f * s, 0.03f, 0.01f), 0.022f, 0.02f, black);
            b.Ell(leg, V(0.045f * s, 0.02f, 0.02f), V(0.026f, 0.02f, 0.035f), black);
        });
        // A dress flaring to its feet, a blue front with purple at its hem
        b.Spike(Body, V(0, 0.3f, 0), V(0, 0.06f, 0), 0.11f, black, blend: 0.02f);
        b.PaintEll(Body, V(0, 0.2f, 0.06f), V(0.05f, 0.09f, 0.05f), blue);
        b.PaintTorus(Body, V(0, 0.08f, 0), 0.1f, 0.016f, purple);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.27f, 0.01f));
            var hand = V(0.1f * s, 0.2f, 0.06f);
            b.Limb(arm, V(0.05f * s, 0.27f, 0.01f), hand, 0.016f, 0.014f, black);
            b.Ell(arm, hand, V(0.022f, 0.022f, 0.022f), black);
            int wing = b.Wing(s, V(0.04f * s, 0.3f, -0.04f));
            Frond(b, wing, V(0.04f * s, 0.3f, -0.04f), V(0.14f * s, 0.32f, -0.1f), 0.045f, clear, V(0, 0.3f, 1f), 0.15f);
        });
        // A pale blue face in a purple hood that curls round its cheeks, two feelers tipped yellow
        int head = b.Head(V(0, 0.34f, 0.01f));
        var c = V(0, 0.4f, 0.02f);
        var r = V(0.07f, 0.065f, 0.065f);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, 0.02f, -0.03f), V(0.09f, 0.07f, 0.06f), purple, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            b.Torus(head, c + V(0.07f * s, -0.01f, 0.01f), 0.025f, 0.01f, purple, V(0, 0, 90f), blend: 0.01f);
            int horn = b.Ear(head, s, c + V(0.03f * s, 0.06f, 0));
            var tip = c + V(0.08f * s, 0.17f, 0.0f);
            b.Limb(horn, c + V(0.025f * s, 0.06f, 0), tip, 0.008f, 0.007f, black);
            b.Ell(horn, tip, V(0.012f, 0.025f, 0.012f), yellow, blend: 0.004f);
            var at = On(c, r, 0.03f * s, 0.405f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(80, 150, 210));
        });
        b.Mark(head, On(c, r, 0, 0.375f), V(0, -0.2f, 1f), 0.012f, 0.006f, Rgb(70, 80, 110), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Gulpin line

    /// <summary>Gulpin: a green blob of a stomach, eyes shut, a yellow feather on its head and stubby hands.</summary>
    private static PokeBuilder Gulpin()
    {
        var b = new PokeBuilder("Gulpin", 0.5f, BodyPlan.Floating, V(0, 0.1f, 0)) { Coat = Fur };
        var green = Rgb(160, 210, 140);
        var yellow = Rgb(246, 216, 90);
        var bc = V(0, 0.1f, -0.02f);
        var br = V(0.12f, 0.1f, 0.15f);
        b.Ell(Body, bc, br, green);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.08f, 0.06f));
            b.Ell(arm, V(0.11f * s, 0.05f, 0.08f), V(0.03f, 0.025f, 0.035f), green, blend: 0.015f);
            var at = Out(bc, br, default, V(0.4f * s, 0.6f, 0.8f));
            b.Eye(Body, at, V(0.4f * s, 0.6f, 0.8f), 0.016f, Rgb(70, 90, 60), closed: true);
        });
        b.Mark(Body, Out(bc, br, default, V(0, 0.1f, 1f)), V(0, 0.1f, 1f), 0.02f, 0.006f, Rgb(80, 110, 70), MarkShape.Bar);
        int head = b.Head(V(0, 0.18f, 0));
        b.Ell(head, V(0, 0.18f, -0.02f), V(0.06f, 0.03f, 0.06f), green, blend: 0.02f);
        Blade(b, head, V(0, 0.19f, -0.02f), V(0.04f, 0.3f, -0.1f), 0.04f, yellow, V(1f, 0, 0.3f), 0.25f);
        return b;
    }

    /// <summary>Swalot: a great purple sack of a stomach, black diamonds on its sides, yellow whiskers and a broad mouth.</summary>
    private static PokeBuilder Swalot()
    {
        var b = new PokeBuilder("Swalot", 0.85f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Fur };
        var purple = Rgb(170, 130, 210);
        var yellow = Rgb(246, 210, 100);
        var bc = V(0, 0.22f, 0);
        var br = V(0.18f, 0.22f, 0.15f);
        b.Ell(Body, bc, br, purple);
        // A wavy hem at its foot
        for (int i = 0; i < 10; i++)
        {
            float a = MathF.Tau * i / 10f;
            b.Ell(Body, V(MathF.Sin(a) * 0.17f, 0.04f, MathF.Cos(a) * 0.14f), V(0.05f, 0.04f, 0.05f), purple, blend: 0.03f);
        }
        PokeBuilder.Both(s =>
        {
            var n = V(0.75f * s, -0.2f, 0.6f);
            b.Mark(Body, Out(bc, br, default, n), n, 0.04f, 0.05f, Rgb(50, 40, 60), MarkShape.Diamond);
            int arm = b.Arm(s, V(0.12f * s, 0.3f, 0.1f));
            b.Ell(arm, V(0.1f * s, 0.3f, 0.14f), V(0.035f, 0.03f, 0.03f), purple, blend: 0.015f);
            b.Tube(Body, Smooth(3, V(0.06f * s, 0.32f, 0.14f), V(0.1f * s, 0.36f, 0.17f), V(0.13f * s, 0.32f, 0.2f), V(0.12f * s, 0.26f, 0.21f)), 0.01f, 0.008f, yellow, blend: 0f);
            var at = Out(bc, br, default, V(0.25f * s, 0.65f, 0.6f));
            b.Eye(Body, at, V(0.25f * s, 0.65f, 0.6f), 0.014f, Rgb(40, 36, 50), closed: true);
        });
        Grin(b, Body, Out(bc, br, default, V(0, 0.4f, 1f)), V(0.07f, 0.012f, 0.03f), Rgb(110, 70, 110));
        int head = b.Head(V(0, 0.42f, 0));
        b.Ell(head, V(0, 0.42f, 0.02f), V(0.05f, 0.03f, 0.05f), purple, blend: 0.02f);
        b.Mark(head, V(0, 0.448f, 0.03f), V(0, 1f, 0.2f), 0.012f, 0.012f, Rgb(140, 90, 170));
        return b;
    }

    // ------------------------------------------------------------------ Carvanha line

    /// <summary>Carvanha: a piranha, red below and navy above, yellow fins jagged like stars, fangs and a red eye in a yellow ring.</summary>
    private static PokeBuilder Carvanha()
    {
        var b = new PokeBuilder("Carvanha", 0.55f, BodyPlan.Fish, V(0, 0.25f, 0)) { Coat = Scales }.Hover();
        var navy = Rgb(56, 84, 150);
        var red = Rgb(214, 80, 80);
        var yellow = Rgb(246, 214, 100);
        var c = V(0, 0.25f, 0);
        var r = V(0.11f, 0.12f, 0.12f);
        b.Ell(Body, c, r, navy);
        b.PaintEll(Body, c + V(0, -0.07f, 0.04f), V(0.13f, 0.08f, 0.12f), red);
        b.Mark(Body, Out(c, r, default, V(0, -0.15f, 1f)), V(0, -0.15f, 1f), 0.07f, 0.03f, White, MarkShape.Zigzag);
        // Yellow fins: a crest of three points over its head, others at its sides, below and behind
        for (int i = 0; i < 3; i++)
            Blade(b, Body, c + V(0, 0.08f, 0.02f - i * 0.04f), c + V(0, 0.22f - i * 0.02f, -0.02f - i * 0.06f), 0.04f, yellow, V(1f, 0, 0), 0.25f);
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, c + V(0.1f * s, -0.02f, 0.02f), PokeRole.Fin, s, s);
            Blade(b, fin, c + V(0.1f * s, -0.02f, 0.02f), c + V(0.24f * s, -0.04f, -0.02f), 0.035f, yellow, V(0, 1f, 0.2f), 0.25f);
            var at = Out(c, r, default, V(0.6f * s, 0.2f, 0.75f));
            b.Mark(Body, at, V(0.6f * s, 0.2f, 0.75f), 0.026f, 0.026f, yellow);
            b.Eye(Body, at, V(0.6f * s, 0.2f, 0.75f), 0.014f, Rgb(214, 50, 60), glare: true);
        });
        Blade(b, Body, c + V(0, -0.1f, 0), c + V(0, -0.2f, -0.03f), 0.035f, yellow, V(1f, 0, 0), 0.25f);
        int tail = b.Tail(c + V(0, 0, -0.11f));
        PokeBuilder.Both(s => Blade(b, tail, c + V(0, 0, -0.1f), c + V(0, 0.08f * s, -0.24f), 0.045f, yellow, V(1f, 0, 0), 0.25f));
        return Lift(b);
    }

    /// <summary>
    /// Sharpedo and its Mega Evolution: a torpedo of a shark, navy above and white below, a yellow star on its snout
    /// and a jaw of fangs; the Mega's snout scarred, yellow stripes along it and spikes on its back.
    /// </summary>
    private static PokeBuilder SharpedoBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Sharpedo-Mega" : "Sharpedo", 0.95f, BodyPlan.Fish, V(0, 0.32f, 0)) { Coat = Scales }.Hover();
        var navy = Rgb(52, 92, 160);
        var white = Rgb(236, 238, 246);
        var yellow = Rgb(246, 214, 100);
        var c = V(0, 0.32f, 0.02f);
        var r = V(0.12f, 0.15f, 0.22f);
        b.Ell(Body, c, r, navy, V(-15f, 0, 0));
        b.PaintEll(Body, c + V(0, -0.08f, 0.05f), V(0.12f, 0.1f, 0.22f), white, V(-15f, 0, 0));
        // A wide open jaw at the front, fanged
        var mouth = c + V(0, 0.0f, 0.2f);
        Grin(b, Body, mouth, V(0.08f, 0.07f, 0.05f), Rgb(200, 90, 110));
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
            {
                float x = 0.025f * (i + 0.5f) * s;
                b.Spike(Body, mouth + V(x, 0.085f, -0.02f), mouth + V(x, 0.035f, 0.005f), 0.012f, White, mat: Shell, blend: 0.003f);
                b.Spike(Body, mouth + V(x, -0.085f, -0.02f), mouth + V(x, -0.035f, 0.005f), 0.012f, White, mat: Shell, blend: 0.003f);
            }
        });
        b.Mark(Body, Out(c, r, V(-15f, 0, 0), V(0, 0.8f, 0.6f)), V(0, 0.8f, 0.6f), 0.03f, 0.03f, yellow, MarkShape.Star5);
        // A great dorsal fin, two side fins and a forked tail
        Blade(b, Body, c + V(0, 0.12f, -0.02f), c + V(0, 0.32f, -0.2f), 0.07f, navy, V(1f, 0, 0), 0.22f);
        if (mega)
        {
            for (int i = 0; i < 3; i++)
                b.Spike(Body, c + V(0, 0.12f, 0.12f - i * 0.05f), c + V(0, 0.18f, 0.1f - i * 0.05f), 0.016f, Rgb(200, 200, 210), mat: Shell);
            PokeBuilder.Both(s =>
            {
                b.Mark(Body, Out(c, r, V(-15f, 0, 0), V(0.7f * s, 0.4f, 0.4f)), V(0.7f * s, 0.4f, 0.4f), 0.035f, 0.01f, yellow, MarkShape.Bar, 60f * s);
                b.Mark(Body, Out(c, r, V(-15f, 0, 0), V(0.6f * s, 0.2f, 0.75f)), V(0.6f * s, 0.2f, 0.75f), 0.02f, 0.03f, Rgb(30, 30, 40), MarkShape.Zigzag);
            });
        }
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, c + V(0.1f * s, -0.04f, 0.04f), PokeRole.Fin, s, s);
            Blade(b, fin, c + V(0.1f * s, -0.04f, 0.04f), c + V(0.3f * s, -0.1f, -0.08f), 0.06f, navy, V(0, 1f, 0.2f), 0.22f);
            if (mega) b.PaintEll(fin, c + V(0.24f * s, -0.08f, -0.05f), V(0.05f, 0.02f, 0.04f), yellow);
            var at = Out(c, r, V(-15f, 0, 0), V(0.65f * s, 0.25f, 0.7f));
            b.Eye(Body, at, V(0.65f * s, 0.25f, 0.7f), 0.016f, Rgb(214, 50, 60), sclera: true, white: yellow, pupil: Rgb(214, 50, 60));
        });
        b.Ell(Body, c + V(0, -0.14f, -0.08f), V(0.02f, 0.05f, 0.04f), white, blend: 0.01f);
        int tail = b.Tail(c + V(0, 0.05f, -0.2f));
        foreach (var (y, z) in new[] { (0.16f, -0.36f), (-0.06f, -0.32f) })
            Blade(b, tail, c + V(0, 0.05f, -0.19f), c + V(0, y, z), 0.05f, navy, V(1f, 0, 0), 0.22f);
        return Lift(b);
    }

    private static PokeBuilder Sharpedo() => SharpedoBuild(false);

    // ------------------------------------------------------------------ Wailmer line

    /// <summary>Wailmer: a round blue whale, cream below, a broad tail fin and a grin; it floats.</summary>
    private static PokeBuilder Wailmer()
    {
        var b = new PokeBuilder("Wailmer", 0.85f, BodyPlan.Fish, V(0, 0.25f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(64, 122, 196);
        var cream = Rgb(246, 232, 190);
        var c = V(0, 0.25f, 0);
        var r = V(0.2f, 0.19f, 0.21f);
        b.Ell(Body, c, r, blue);
        b.PaintEll(Body, c + V(0, -0.1f, 0.04f), V(0.2f, 0.14f, 0.21f), cream);
        b.Mark(Body, Out(c, r, default, V(0, 0.05f, 1f)), V(0, 0.05f, 1f), 0.08f, 0.02f, Rgb(40, 50, 80), MarkShape.Smile);
        for (int i = 0; i < 4; i++)
        {
            float x = -0.045f + 0.03f * i;
            b.Mark(Body, Out(c, r, default, V(x / 0.2f, 0.0f, 1f)) + V(0, -0.02f, 0), V(x, 0, 1f), 0.008f, 0.012f, White, MarkShape.Bar, 90f);
        }
        b.Mark(Body, Out(c, r, default, V(0, 1f, 0.2f)), V(0, 1f, 0.2f), 0.012f, 0.012f, Rgb(30, 50, 90));
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, c + V(0.17f * s, -0.06f, 0.05f), PokeRole.Fin, s, s);
            Frond(b, fin, c + V(0.17f * s, -0.06f, 0.05f), c + V(0.28f * s, -0.12f, 0.06f), 0.05f, blue, V(0, 1f, 0.2f), 0.3f);
            var at = Out(c, r, default, V(0.45f * s, 0.35f, 0.8f));
            b.Eye(Body, at, V(0.45f * s, 0.35f, 0.8f), 0.016f, Rgb(36, 36, 48));
        });
        int tail = b.Tail(c + V(0, 0.04f, -0.18f));
        b.Limb(tail, c + V(0, 0.04f, -0.17f), c + V(0, 0.08f, -0.26f), 0.06f, 0.04f, blue);
        PokeBuilder.Both(s => Frond(b, tail, c + V(0, 0.08f, -0.26f), c + V(0.12f * s, 0.14f, -0.32f), 0.06f, blue, V(0, 1f, 0), 0.3f));
        return Lift(b);
    }

    /// <summary>Wailord: a vast blue whale, long and low, its belly pale and grooved, small fins and a broad tail.</summary>
    private static PokeBuilder Wailord()
    {
        var b = new PokeBuilder("Wailord", 1f, BodyPlan.Fish, V(0, 0.22f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(64, 118, 196);
        var pale = Rgb(214, 214, 224);
        var c = V(0, 0.22f, 0.02f);
        var r = V(0.16f, 0.15f, 0.42f);
        b.Ell(Body, c, r, blue);
        b.PaintEll(Body, c + V(0, -0.08f, 0.04f), V(0.17f, 0.1f, 0.42f), pale);
        for (int i = -3; i <= 3; i++)
            b.PaintEll(Body, c + V(i * 0.035f, -0.12f, 0.05f), V(0.004f, 0.06f, 0.36f), Rgb(150, 150, 166), soft: 0.005f);
        b.Mark(Body, Out(c, r, default, V(0, 1f, 0.6f)), V(0, 1f, 0.6f), 0.014f, 0.014f, Rgb(30, 50, 90));
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, c + V(0.13f * s, -0.06f, 0.15f), PokeRole.Fin, s, s);
            Frond(b, fin, c + V(0.13f * s, -0.06f, 0.15f), c + V(0.24f * s, -0.12f, 0.08f), 0.05f, blue, V(0, 1f, 0.2f), 0.3f);
            var at = Out(c, r, default, V(0.6f * s, 0.35f, 0.75f));
            b.Eye(Body, at, V(0.9f * s, 0.35f, 0.3f), 0.014f, Rgb(36, 36, 48));
            Blade(b, Body, c + V(0.05f * s, 0.12f, -0.1f), c + V(0.07f * s, 0.18f, -0.2f), 0.035f, blue, V(1f, 0, 0), 0.25f);
        });
        int tail = b.Tail(c + V(0, 0.02f, -0.38f));
        b.Limb(tail, c + V(0, 0.02f, -0.36f), c + V(0, 0.06f, -0.5f), 0.07f, 0.04f, blue);
        PokeBuilder.Both(s => Frond(b, tail, c + V(0, 0.06f, -0.5f), c + V(0.14f * s, 0.12f, -0.58f), 0.06f, blue, V(0, 1f, 0), 0.3f));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Numel line

    /// <summary>Numel: a yellow camel, a green hump with yellow spots, a cream belly and dull, heavy-lidded eyes.</summary>
    private static PokeBuilder Numel()
    {
        var b = new PokeBuilder("Numel", 0.6f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 222, 130);
        var green = Rgb(140, 196, 110);
        var cream = Rgb(250, 240, 210);
        StubbyLegs(b, 0.07f, 0.12f, 0.08f, -0.09f, 0.035f, yellow);
        var bc = V(0, 0.18f, -0.01f);
        var br = V(0.1f, 0.09f, 0.15f);
        b.Ell(Body, bc, br, yellow);
        b.PaintEll(Body, V(0, 0.12f, 0.02f), V(0.08f, 0.05f, 0.12f), cream);
        b.Ell(Body, V(0, 0.25f, -0.05f), V(0.08f, 0.05f, 0.08f), yellow, blend: 0.03f);
        b.PaintEll(Body, V(0, 0.3f, -0.05f), V(0.09f, 0.04f, 0.09f), green);
        foreach (var (x, z) in new[] { (0.04f, -0.02f), (-0.03f, -0.08f), (0.0f, -0.12f) })
        {
            var n = V(x / 0.08f, 0.8f, (z + 0.05f) / 0.08f);
            b.Mark(Body, Out(V(0, 0.25f, -0.05f), V(0.08f, 0.05f, 0.08f), default, n), n, 0.014f, 0.012f, Rgb(100, 160, 80), MarkShape.Diamond);
        }
        int tail = b.Tail(V(0, 0.2f, -0.15f));
        b.Limb(tail, V(0, 0.2f, -0.15f), V(0, 0.18f, -0.2f), 0.014f, 0.01f, yellow);
        // A long neck and a head with a broad muzzle, heavy-lidded eyes and a little crest
        int head = b.Head(V(0, 0.26f, 0.1f));
        b.Limb(head, V(0, 0.22f, 0.1f), V(0, 0.34f, 0.14f), 0.045f, 0.04f, yellow);
        var c = V(0, 0.38f, 0.16f);
        var r = V(0.07f, 0.06f, 0.08f);
        b.Ell(head, c, r, yellow);
        b.Ell(head, V(0, 0.36f, 0.22f), V(0.05f, 0.04f, 0.045f), cream, blend: 0.02f);
        b.Ell(head, V(0, 0.44f, 0.13f), V(0.025f, 0.025f, 0.02f), yellow, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.42f, 0.13f));
            b.Ell(ear, V(0.07f * s, 0.42f, 0.12f), V(0.025f, 0.012f, 0.018f), yellow, blend: 0.008f);
            var at = On(c, r, 0.04f * s, 0.39f);
            b.Mark(head, at, Outward(c, r, at), 0.02f, 0.02f, Rgb(70, 60, 50), MarkShape.Ring);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(50, 44, 40));
        });
        return b;
    }

    /// <summary>
    /// Camerupt and its Mega Evolution: a squat red camel carrying two grey volcanoes on its back, blue rings on its
    /// sides; the Mega's two cones merged into one great volcano, lava glowing at its top and rim.
    /// </summary>
    private static PokeBuilder CameruptBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Camerupt-Mega" : "Camerupt", 0.9f, BodyPlan.Quadruped, V(0, 0.24f, 0)) { Coat = Fur };
        var red = Rgb(214, 108, 88);
        var gray = Rgb(150, 144, 140);
        var blue = Rgb(110, 180, 220);
        var lava = Rgb(250, 140, 50);
        StubbyLegs(b, 0.11f, 0.12f, 0.11f, -0.13f, 0.05f, red, Rgb(80, 74, 76));
        var bc = V(0, 0.22f, -0.01f);
        var br = V(0.16f, 0.13f, 0.21f);
        b.Ell(Body, bc, br, red);
        FurTufts(b, Body, V(0, 0.16f, -0.01f), V(0.16f, 0.08f, 0.2f), 18, 0.06f, 0.02f, red, 0.6f, -0.9f, 0.9f);
        if (!mega)
            PokeBuilder.Both(s =>
            {
                for (int i = 0; i < 3; i++)
                {
                    var n = V(s, 0.0f, (0.12f - i * 0.12f) / br.Z);
                    b.Mark(Body, Out(bc, br, default, n), n, 0.03f, 0.05f, blue, MarkShape.Ring);
                }
            });
        // Volcanoes on its back
        if (mega)
        {
            var vb = V(0, 0.32f, -0.03f);
            b.Spike(Body, vb, vb + V(0, 0.26f, 0), 0.14f, gray, blend: 0.02f);
            b.CutBox(Body, vb + V(0, 0.29f, 0), V(0.2f, 0.06f, 0.2f), Quaternion.Identity);
            b.Ell(Body, vb + V(0, 0.225f, 0), V(0.04f, 0.012f, 0.04f), lava, mat: Glow, blend: 0.005f);
            foreach (var (x, z) in new[] { (0.1f, 0.08f), (-0.1f, 0.08f), (0.12f, -0.06f), (-0.12f, -0.06f), (0f, -0.13f), (0f, 0.13f) })
                b.Ell(Body, vb + V(x, 0.02f, z), V(0.05f, 0.04f, 0.05f), Rgb(64, 58, 60), mat: Shell, blend: 0.01f);
            b.PaintEll(Body, vb + V(0, 0.2f, 0), V(0.06f, 0.035f, 0.06f), lava);
        }
        else
            foreach (float z in new[] { 0.06f, -0.1f })
            {
                var vb = V(0, 0.32f, z);
                b.Spike(Body, vb, vb + V(0, 0.16f, 0), 0.08f, gray, blend: 0.02f);
                b.CutBox(Body, vb + V(0, 0.19f, 0), V(0.1f, 0.04f, 0.1f), Quaternion.Identity);
                b.Mark(Body, vb + V(0, 0.15f, 0), V(0, 1f, 0), 0.02f, 0.02f, Rgb(70, 60, 60));
            }
        // A head with a grey muzzle, a tuft of fur and a dull look
        int head = b.Head(V(0, 0.24f, 0.18f));
        var c = V(0, 0.24f, 0.24f);
        var r = V(0.08f, 0.07f, 0.08f);
        b.Ell(head, c, r, red);
        b.Ell(head, V(0, 0.21f, 0.3f), V(0.05f, 0.04f, 0.045f), Rgb(170, 160, 156), blend: 0.02f);
        FurTufts(b, head, c + V(0, 0.05f, -0.01f), V(0.05f, 0.03f, 0.04f), 5, 0.05f, 0.016f, red, 0.3f, 0.3f, 0.1f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.25f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(50, 44, 40), glare: mega);
        });
        return b;
    }

    private static PokeBuilder Camerupt() => CameruptBuild(false);

    // ------------------------------------------------------------------ Torkoal

    /// <summary>Torkoal: an orange tortoise under a black shell with red vents, smoke puffing out of its back and nostrils.</summary>
    private static PokeBuilder Torkoal()
    {
        var b = new PokeBuilder("Torkoal", 0.85f, BodyPlan.Quadruped, V(0, 0.22f, 0)) { Coat = Shell };
        var orange = Rgb(226, 120, 70);
        var shell = Rgb(66, 62, 70);
        var vent = Rgb(220, 70, 60);
        var smoke = Rgb(214, 204, 194);
        StubbyLegs(b, 0.12f, 0.13f, 0.1f, -0.12f, 0.045f, orange);
        var bc = V(0, 0.22f, -0.02f);
        var br = V(0.18f, 0.14f, 0.21f);
        b.Ell(Body, bc, br, shell);
        b.Torus(Body, V(0, 0.16f, -0.02f), 0.18f, 0.025f, orange, sz: 1.15f, mat: Fur, blend: 0.01f);
        foreach (var (x, z) in new[] { (0f, 0.0f), (0.1f, 0.08f), (-0.1f, 0.08f), (0.1f, -0.1f), (-0.1f, -0.1f), (0f, -0.15f) })
        {
            var n = V(x / br.X, 0.8f, z / br.Z);
            b.Mark(Body, Out(bc, br, default, n), n, 0.03f, 0.03f, vent, MarkShape.Diamond);
        }
        foreach (var o in new[] { V(0, 0.36f, -0.06f), V(0.05f, 0.42f, -0.08f), V(-0.04f, 0.44f, -0.04f) })
            GasPuff(b, Body, o, V(0, 1f, 0), 0.06f, smoke);
        int tail = b.Tail(V(0, 0.18f, -0.2f));
        b.Spike(tail, V(0, 0.18f, -0.2f), V(0, 0.14f, -0.28f), 0.03f, orange);
        // A head on a long neck, smoke from its nostrils
        int head = b.Head(V(0, 0.24f, 0.18f));
        b.Limb(head, V(0, 0.22f, 0.16f), V(0, 0.3f, 0.26f), 0.05f, 0.045f, orange);
        var c = V(0, 0.33f, 0.28f);
        var r = V(0.06f, 0.055f, 0.07f);
        b.Ell(head, c, r, orange);
        b.PaintTorus(head, V(0, 0.28f, 0.24f), 0.045f, 0.008f, shell, V(60f, 0, 0));
        PokeBuilder.Both(s =>
        {
            GasPuff(b, head, c + V(0.025f * s, 0.0f, 0.075f), V(0.4f * s, 0.2f, 1f), 0.03f, smoke);
            var at = On(c, r, 0.03f * s, 0.345f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(50, 40, 40), closed: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Spoink line

    /// <summary>Spoink: a little grey pig bouncing on a coiled spring, a pink pearl on its head.</summary>
    private static PokeBuilder Spoink()
    {
        var b = new PokeBuilder("Spoink", 0.55f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Fur };
        var gray = Rgb(150, 150, 160);
        var pink = Rgb(240, 160, 180);
        // The spring under it
        var coil = Spiral(V(0, 0.03f, 0), V(1f, 0, 0), V(0, 0, 1f), 0.04f, 0.04f, 0f, MathF.Tau * 3f, 36).Select((p, i) => p + V(0, i * 0.003f, 0)).ToArray();
        b.Tube(Body, coil, 0.008f, 0.008f, Rgb(180, 180, 190), Metal, 0f);
        b.Limb(Body, coil[^1], V(0, 0.16f, 0), 0.01f, 0.012f, gray);
        var bc = V(0, 0.22f, 0);
        var br = V(0.075f, 0.08f, 0.065f);
        b.Ell(Body, bc, br, gray);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.2f, 0.02f));
            b.Ell(arm, V(0.09f * s, 0.18f, 0.03f), V(0.03f, 0.02f, 0.02f), gray, blend: 0.01f);
            int ear = b.Ear(Body, s, V(0.05f * s, 0.28f, 0));
            b.Ell(ear, V(0.06f * s, 0.29f, 0), V(0.02f, 0.02f, 0.01f), gray, blend: 0.008f);
            var at = On(bc, br, 0.03f * s, 0.24f);
            b.Eye(Body, at, Outward(bc, br, at), 0.012f, Rgb(40, 36, 46));
            b.Mark(Body, On(bc, br, 0.055f * s, 0.215f), V(0.6f * s, 0, 1f), 0.01f, 0.007f, pink);
        });
        b.Ell(Body, V(0, 0.21f, 0.065f), V(0.03f, 0.02f, 0.015f), Rgb(170, 160, 170), blend: 0.008f);
        PokeBuilder.Both(s => b.Mark(Body, V(0.01f * s, 0.21f, 0.08f), V(0, 0, 1f), 0.004f, 0.006f, Rgb(80, 70, 80)));
        int head = b.Head(V(0, 0.3f, 0));
        b.Ell(head, V(0, 0.33f, -0.005f), V(0.04f, 0.04f, 0.04f), pink, mat: Glow, blend: 0.006f);
        return b;
    }

    /// <summary>Grumpig: a purple pig standing upright, black pearls on its head and chest, black ruffles over its arms and legs, a curly tail.</summary>
    private static PokeBuilder Grumpig()
    {
        var b = new PokeBuilder("Grumpig", 0.8f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var purple = Rgb(190, 170, 220);
        var black = Rgb(60, 56, 64);
        var pink = Rgb(240, 170, 186);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.2f, 0));
            b.Ell(leg, V(0.08f * s, 0.15f, 0), V(0.06f, 0.08f, 0.065f), black);
            b.Limb(leg, V(0.08f * s, 0.08f, 0), V(0.08f * s, 0.03f, 0.02f), 0.025f, 0.022f, purple);
            b.Ell(leg, V(0.08f * s, 0.02f, 0.03f), V(0.03f, 0.02f, 0.04f), purple);
        });
        var bc = V(0, 0.32f, 0);
        var br = V(0.09f, 0.12f, 0.08f);
        b.Ell(Body, bc, br, purple);
        b.Ell(Body, V(0, 0.38f, 0.075f), V(0.022f, 0.022f, 0.02f), black, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.4f, 0));
            var hand = V(0.18f * s, 0.32f, 0.06f);
            b.Limb(arm, V(0.08f * s, 0.4f, 0), hand, 0.04f, 0.03f, black);
            b.Ell(arm, hand, V(0.026f, 0.026f, 0.026f), purple);
        });
        int tail = b.Tail(V(0, 0.24f, -0.07f));
        Curl(b, tail, V(0, 0.24f, -0.07f), V(0.03f, 0.22f, -0.16f), V(1f, 0, 0), V(0, 1f, 0), 0.04f, 1.2f, 0.01f, pink);
        int head = b.Head(V(0, 0.44f, 0.01f));
        var c = V(0, 0.5f, 0.02f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Ell(head, c, r, purple);
        b.Ell(head, V(0, 0.48f, 0.09f), V(0.04f, 0.026f, 0.02f), pink, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.035f * s, 0.07f, 0), V(0.022f, 0.022f, 0.022f), black, mat: Shell, blend: 0.006f);
            int ear = b.Ear(head, s, V(0.06f * s, 0.54f, 0));
            Blade(b, ear, V(0.06f * s, 0.54f, 0), V(0.16f * s, 0.6f, -0.02f), 0.04f, black, V(0, 0.3f, 1f), 0.25f);
            var at = On(c, r, 0.03f * s, 0.52f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(150, 90, 170), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Spinda

    /// <summary>Spinda: a cream panda-bunny, red ears and spots, spirals for eyes, tottering on its feet.</summary>
    private static PokeBuilder Spinda()
    {
        var b = new PokeBuilder("Spinda", 0.6f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var cream = Rgb(244, 232, 210);
        var red = Rgb(214, 88, 88);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.12f, 0));
            b.Limb(leg, V(0.05f * s, 0.12f, 0), V(0.06f * s, 0.03f, 0.02f), 0.03f, 0.024f, cream);
            b.Ell(leg, V(0.06f * s, 0.02f, 0.03f), V(0.03f, 0.02f, 0.04f), red);
        });
        var bc = V(0, 0.2f, 0);
        var br = V(0.075f, 0.09f, 0.065f);
        b.Ell(Body, bc, br, cream);
        b.PaintTorus(Body, V(0, 0.13f, 0), 0.07f, 0.014f, red);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.24f, 0.01f));
            var hand = s > 0 ? V(0.14f * s, 0.3f, 0.04f) : V(0.12f * s, 0.18f, 0.05f);
            b.Limb(arm, V(0.06f * s, 0.24f, 0.01f), hand, 0.02f, 0.016f, red);
            b.Ell(arm, hand, V(0.02f, 0.02f, 0.02f), cream);
        });
        int head = b.Head(V(0, 0.28f, 0.01f));
        var c = V(0, 0.35f, 0.02f);
        var r = V(0.08f, 0.075f, 0.07f);
        b.Ell(head, c, r, cream);
        foreach (var (x, y) in new[] { (0.05f, 0.4f), (-0.04f, 0.31f) })
            b.PaintEll(head, V(x, y, 0.07f), V(0.035f, 0.035f, 0.04f), red);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.41f, 0));
            var ec = V(0.1f * s, 0.46f, -0.01f);
            b.Limb(ear, V(0.05f * s, 0.41f, 0), ec, 0.016f, 0.014f, cream);
            b.Ell(ear, ec, V(0.045f, 0.05f, 0.018f), cream, V(0, 0, -30f * s), blend: 0.015f);
            b.Mark(ear, ec + V(0, 0, 0.018f), V(0, 0, 1f), 0.03f, 0.03f, red);
            var at = On(c, r, 0.035f * s, 0.355f);
            b.Mark(head, at, Outward(c, r, at), 0.022f, 0.022f, Rgb(50, 40, 40), MarkShape.Ring);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(50, 40, 40));
        });
        b.Mark(head, On(c, r, 0, 0.32f), V(0, -0.2f, 1f), 0.016f, 0.007f, Rgb(150, 70, 70), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Trapinch line

    /// <summary>Trapinch: a little orange antlion, its head a huge round ball with a jagged crack of a mouth, on four short legs.</summary>
    private static PokeBuilder Trapinch()
    {
        var b = new PokeBuilder("Trapinch", 0.55f, BodyPlan.Quadruped, V(0, 0.12f, -0.04f)) { Coat = Shell };
        var orange = Rgb(230, 130, 80);
        var cream = Rgb(236, 222, 200);
        StubbyLegs(b, 0.05f, 0.08f, 0.0f, -0.1f, 0.025f, orange);
        b.Ell(Body, V(0, 0.09f, -0.07f), V(0.06f, 0.05f, 0.07f), orange);
        b.PaintTorus(Body, V(0, 0.07f, -0.07f), 0.06f, 0.012f, cream);
        int head = b.Head(V(0, 0.14f, 0.02f));
        var c = V(0, 0.2f, 0.06f);
        var r = V(0.13f, 0.13f, 0.12f);
        b.Ell(head, c, r, orange);
        b.Mark(head, Out(c, r, default, V(0, 0.1f, 1f)), V(0, 0.1f, 1f), 0.11f, 0.04f, Rgb(120, 60, 40), MarkShape.Zigzag);
        PokeBuilder.Both(s =>
        {
            var look = V(0.85f * s, 0.25f, 0.4f);
            b.Mark(head, Out(c, r, default, look), look, 0.02f, 0.02f, White, MarkShape.Star);
            b.Eye(head, Out(c, r, default, look), look, 0.01f, Rgb(40, 30, 30));
        });
        return b;
    }

    /// <summary>Vibrava: a yellow-green dragonfly with great diamond wings of green rimmed black, a long thin tail and big green goggle eyes.</summary>
    private static PokeBuilder Vibrava()
    {
        var b = new PokeBuilder("Vibrava", 0.75f, BodyPlan.Bird, V(0, 0.22f, 0)) { Coat = Shell }.Hover();
        var olive = Rgb(214, 214, 140);
        var green = Rgb(90, 180, 90);
        var rim = Rgb(50, 56, 50);
        DanglingLegs(b, 0.035f, 0.19f, 0.04f, 0.12f, 0.009f, rim);
        var bc = V(0, 0.22f, 0);
        var br = V(0.05f, 0.05f, 0.08f);
        b.Ell(Body, bc, br, olive);
        int tail = b.Tail(V(0, 0.22f, -0.07f));
        b.Tube(tail, Smooth(3, V(0, 0.22f, -0.07f), V(0, 0.23f, -0.18f), V(0, 0.26f, -0.3f)), 0.026f, 0.014f, olive, blend: 0f);
        foreach (float z in new[] { -0.14f, -0.22f })
            b.PaintTorus(tail, V(0, 0.23f + (z + 0.14f) * -0.3f, z), 0.024f, 0.005f, rim, V(90f, 0, 0));
        // Four diamond wings, green in a dark rim
        foreach (var (z, ang, len) in new[] { (0.02f, 40f, 0.26f), (-0.04f, 15f, 0.2f) })
            PokeBuilder.Both(s =>
            {
                int wing = b.Wing(s, V(0.035f * s, 0.25f, z));
                float a = ang * Degree;
                var root = V(0.035f * s, 0.25f, z);
                var tip = root + V(MathF.Cos(a) * len * s, MathF.Sin(a) * len * 0.6f, -0.04f);
                Frond(b, wing, root, tip, len * 0.24f, rim, V(0, 1f, 0.2f), 0.12f, Shell);
                b.PaintEll(wing, Vector3.Lerp(root, tip, 0.52f), V(len * 0.17f, len * 0.36f, len * 0.17f), green, Euler(tip - root));
                b.Limb(wing, root, Vector3.Lerp(root, tip, 0.15f), 0.01f, 0.008f, olive);
            });
        // A head with big goggle eyes and a pointed snout
        int head = b.Head(V(0, 0.24f, 0.06f));
        var c = V(0, 0.25f, 0.1f);
        var r = V(0.055f, 0.045f, 0.06f);
        b.Ell(head, c, r, olive);
        b.Spike(head, c + V(0, -0.01f, 0.05f), c + V(0, -0.02f, 0.14f), 0.025f, olive, 0.6f);
        PokeBuilder.Both(s =>
        {
            var g = c + V(0.05f * s, 0.01f, 0.01f);
            b.Ell(head, g, V(0.03f, 0.03f, 0.03f), green, mat: Glow, blend: 0.006f);
            b.Eye(head, g + V(0.028f * s, 0, 0.01f), V(s, 0, 0.4f), 0.012f, Rgb(30, 90, 40));
        });
        return Lift(b);
    }

    /// <summary>Flygon: a slim green desert dragon, red goggles over its eyes, broad diamond wings edged red and a tail with three red diamonds at its end.</summary>
    private static PokeBuilder Flygon()
    {
        var b = new PokeBuilder("Flygon", 0.95f, BodyPlan.Bird, V(0, 0.4f, 0)) { Coat = Scales }.Hover();
        var green = Rgb(150, 206, 130);
        var dark = Rgb(76, 150, 90);
        var red = Rgb(220, 90, 90);
        var membrane = Rgb(150, 210, 150);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.32f, -0.02f));
            b.Limb(leg, V(0.05f * s, 0.33f, -0.02f), V(0.07f * s, 0.2f, 0.02f), 0.035f, 0.02f, green);
            b.Limb(leg, V(0.07f * s, 0.2f, 0.02f), V(0.07f * s, 0.12f, 0.0f), 0.018f, 0.016f, green);
        });
        var bc = V(0, 0.4f, 0);
        var br = V(0.07f, 0.11f, 0.065f);
        b.Ell(Body, bc, br, green, V(15f, 0, 0));
        b.PaintEll(Body, V(0, 0.38f, 0.05f), V(0.05f, 0.08f, 0.03f), dark);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.46f, 0.03f));
            b.Limb(arm, V(0.05f * s, 0.46f, 0.03f), V(0.08f * s, 0.38f, 0.09f), 0.016f, 0.012f, green);
            // A broad wing: a diamond of pale green rimmed red
            int wing = b.Wing(s, V(0.05f * s, 0.48f, -0.03f));
            var root = V(0.05f * s, 0.48f, -0.03f);
            var tip = root + V(0.34f * s, 0.16f, -0.06f);
            Frond(b, wing, root, tip, 0.09f, red, V(0, 1f, 0.2f), 0.12f, Shell);
            b.PaintEll(wing, Vector3.Lerp(root, tip, 0.5f), V(0.07f, 0.15f, 0.07f), membrane, Euler(tip - root));
            b.Limb(wing, root, Vector3.Lerp(root, tip, 0.2f), 0.014f, 0.01f, green);
        });
        // A long tail curling down, three red diamonds at its tip
        int tail = b.Tail(V(0, 0.32f, -0.05f));
        var tp = Smooth(3, V(0, 0.32f, -0.05f), V(0, 0.18f, -0.14f), V(0, 0.16f, -0.32f), V(0, 0.26f, -0.42f));
        b.Tube(tail, tp, 0.035f, 0.014f, green, blend: 0f);
        for (int i = 0; i < 3; i++)
            b.PaintTorus(tail, tp[3 + i * 2], 0.024f, 0.006f, dark, Euler(tp[4 + i * 2] - tp[3 + i * 2]));
        foreach (var o in new[] { V(0, 0.05f, -0.01f), V(0.04f, 0.0f, -0.02f), V(-0.04f, 0.0f, -0.02f) })
            Blade(b, tail, tp[^1], tp[^1] + o + V(0, 0.02f, -0.03f), 0.025f, red, V(0, 0, 1f), 0.25f);
        // A head on a slim neck, red goggles over its eyes and two antennae swept back
        int head = b.Head(V(0, 0.5f, 0.04f));
        b.Limb(head, V(0, 0.48f, 0.03f), V(0, 0.58f, 0.08f), 0.03f, 0.028f, green);
        var c = V(0, 0.62f, 0.1f);
        var r = V(0.055f, 0.05f, 0.07f);
        b.Ell(head, c, r, green);
        PokeBuilder.Both(s =>
        {
            var g = c + V(0.04f * s, 0.01f, 0.03f);
            b.Ell(head, g, V(0.03f, 0.025f, 0.025f), red, mat: Glow, blend: 0.006f);
            b.Eye(head, g + V(0.015f * s, 0, 0.02f), V(0.6f * s, 0, 0.8f), 0.011f, Rgb(200, 40, 50));
            b.Tube(head, Smooth(3, c + V(0.02f * s, 0.04f, 0), c + V(0.05f * s, 0.12f, -0.08f), c + V(0.08f * s, 0.14f, -0.2f)), 0.007f, 0.005f, green, blend: 0f);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Cacnea line

    /// <summary>Cacnea: a round green cactus with black holes for eyes and mouth, dark diamonds and spikes, a yellow flower on top.</summary>
    private static PokeBuilder Cacnea()
    {
        var b = new PokeBuilder("Cacnea", 0.55f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Leaf };
        var green = Rgb(130, 190, 110);
        var dark = Rgb(70, 130, 70);
        var yellow = Rgb(246, 210, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.06f, 0));
            b.Ell(leg, V(0.055f * s, 0.03f, 0.01f), V(0.035f, 0.03f, 0.035f), green);
        });
        var bc = V(0, 0.18f, 0);
        var br = V(0.12f, 0.12f, 0.11f);
        b.Ell(Body, bc, br, green);
        foreach (var n in new[] { V(0.6f, -0.5f, 0.6f), V(-0.6f, -0.5f, 0.6f), V(0, -0.7f, 0.7f), V(0.9f, 0.2f, -0.3f), V(-0.9f, 0.2f, -0.3f), V(0, 0.3f, -1f) })
            b.Mark(Body, Out(bc, br, default, n), n, 0.026f, 0.026f, dark, MarkShape.Diamond);
        foreach (var n in new[] { V(0.5f, 0.6f, -0.6f), V(-0.5f, 0.6f, -0.6f), V(0, -0.2f, -1f), V(0.7f, -0.6f, -0.2f), V(-0.7f, -0.6f, -0.2f) })
        {
            var root = Out(bc, br, default, n);
            b.Spike(Body, root - Vector3.Normalize(n) * 0.01f, root + Vector3.Normalize(n) * 0.04f, 0.018f, dark);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.17f, 0));
            var hand = V(0.2f * s, 0.14f, 0.02f);
            b.Limb(arm, V(0.1f * s, 0.17f, 0), hand, 0.026f, 0.03f, green);
            b.Ell(arm, hand, V(0.04f, 0.04f, 0.04f), green);
            b.Spike(arm, hand + V(0.02f * s, 0.03f, 0), hand + V(0.04f * s, 0.07f, 0), 0.014f, dark);
        });
        // A face of black holes
        foreach (var (x, y, rr) in new[] { (0.04f, 0.22f, 0.02f), (-0.04f, 0.22f, 0.02f), (0f, 0.15f, 0.024f), (0.05f, 0.15f, 0.014f), (-0.05f, 0.15f, 0.014f) })
            b.Mark(Body, On(bc, br, x, y), Outward(bc, br, On(bc, br, x, y)), rr, rr, Rgb(30, 30, 30));
        PokeBuilder.Both(s => b.Eye(Body, On(bc, br, 0.04f * s, 0.22f), Outward(bc, br, On(bc, br, 0.04f * s, 0.22f)), 0.008f, sclera: true, pupil: Rgb(30, 30, 30)));
        int head = b.Head(V(0, 0.29f, 0));
        for (int i = 0; i < 5; i++)
        {
            float a = MathF.Tau * i / 5f;
            b.Spike(head, V(0, 0.29f, 0), V(MathF.Sin(a) * 0.04f, 0.33f, MathF.Cos(a) * 0.04f), 0.018f, yellow, 0.5f, blend: 0.006f);
        }
        return b;
    }

    /// <summary>Cacturne: a tall green cactus scarecrow, a dark pointed hat-head, yellow eyes in the shadow, diamonds and spikes down its body.</summary>
    private static PokeBuilder Cacturne()
    {
        var b = new PokeBuilder("Cacturne", 0.9f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Leaf };
        var green = Rgb(124, 180, 110);
        var dark = Rgb(66, 120, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.24f, 0));
            b.Limb(leg, V(0.05f * s, 0.25f, 0), V(0.08f * s, 0.04f, 0.0f), 0.05f, 0.06f, green);
            b.Ell(leg, V(0.085f * s, 0.03f, 0.01f), V(0.06f, 0.03f, 0.06f), green);
        });
        var bc = V(0, 0.38f, 0);
        var br = V(0.085f, 0.15f, 0.07f);
        b.Ell(Body, bc, br, green);
        foreach (var (x, y) in new[] { (0f, 0.42f), (0.03f, 0.34f), (-0.03f, 0.3f) })
            b.Mark(Body, On(bc, br, x, y), Outward(bc, br, On(bc, br, x, y)), 0.02f, 0.02f, dark, MarkShape.Diamond);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
            {
                var n = V(s, (0.3f + i * 0.08f - bc.Y) / br.Y, 0);
                var root = Out(bc, br, default, n);
                b.Spike(Body, root, root + V(0.04f * s, 0.01f, 0), 0.014f, dark);
            }
            int arm = b.Arm(s, V(0.07f * s, 0.46f, 0));
            var hand = V(0.28f * s, 0.46f, 0.04f);
            b.Limb(arm, V(0.07f * s, 0.46f, 0), hand, 0.035f, 0.045f, green);
            b.Ell(arm, hand, V(0.05f, 0.045f, 0.04f), green);
            for (int i = 0; i < 3; i++)
            {
                var root = Vector3.Lerp(V(0.07f * s, 0.46f, 0), hand, 0.4f + 0.25f * i);
                b.Spike(arm, root + V(0, 0.03f, 0), root + V(0, 0.07f, -0.01f), 0.014f, dark);
            }
        });
        // A head of dark green like a pointed hat, its yellow eyes peering out under the brim
        int head = b.Head(V(0, 0.52f, 0.01f));
        var c = V(0, 0.58f, 0.02f);
        var r = V(0.07f, 0.065f, 0.065f);
        b.Ell(head, c, r, green);
        b.Ell(head, c + V(0, 0.06f, 0), V(0.12f, 0.025f, 0.1f), dark, blend: 0.02f);
        b.Spike(head, c + V(0, 0.07f, 0), c + V(0, 0.2f, -0.04f), 0.09f, dark, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, 0.585f);
            b.Mark(head, at, Outward(c, r, at), 0.022f, 0.02f, Rgb(40, 50, 40));
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, white: Rgb(246, 210, 80), pupil: Rgb(40, 30, 30));
        });
        b.Mark(head, On(c, r, 0, 0.555f), V(0, -0.2f, 1f), 0.03f, 0.008f, Rgb(40, 60, 40), MarkShape.Zigzag);
        return b;
    }

    // ------------------------------------------------------------------ Zangoose and Seviper

    /// <summary>Zangoose: a white mongoose standing upright, red zigzags across its chest and face, long black claws and a bushy tail.</summary>
    private static PokeBuilder Zangoose()
    {
        var b = new PokeBuilder("Zangoose", 0.85f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var white = Rgb(244, 244, 246);
        var red = Rgb(214, 60, 70);
        var claw = Rgb(56, 54, 62);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.2f, -0.01f));
            b.Limb(leg, V(0.07f * s, 0.22f, -0.01f), V(0.08f * s, 0.04f, 0.02f), 0.045f, 0.03f, white);
            b.Ell(leg, V(0.08f * s, 0.022f, 0.04f), V(0.032f, 0.022f, 0.05f), white);
            b.PaintEll(leg, V(0.08f * s, 0.02f, 0.08f), V(0.03f, 0.02f, 0.02f), red);
        });
        var bc = V(0, 0.32f, 0);
        var br = V(0.11f, 0.15f, 0.09f);
        b.Ell(Body, bc, br, white);
        FurTufts(b, Body, bc, br, 12, 0.06f, 0.02f, white, 0.6f, -0.6f, 0.4f);
        b.Mark(Body, On(bc, br, 0, 0.36f), V(0, 0.1f, 1f), 0.08f, 0.035f, red, MarkShape.Zigzag, -20f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.4f, 0.01f));
            var hand = V(0.17f * s, 0.32f, 0.12f);
            b.Limb(arm, V(0.1f * s, 0.4f, 0.01f), hand, 0.03f, 0.024f, white);
            b.PaintEll(arm, hand, V(0.03f, 0.03f, 0.03f), red);
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, hand + V(0.01f * i, 0, 0.01f), hand + V(0.03f * i + 0.02f * s, -0.07f, 0.06f), 0.012f, claw, mat: Shell, blend: 0.006f);
        });
        int tail = b.Tail(V(0, 0.24f, -0.08f));
        var tp = Smooth(3, V(0, 0.24f, -0.08f), V(0.08f, 0.3f, -0.2f), V(0.18f, 0.4f, -0.22f));
        b.Tube(tail, tp, 0.04f, 0.05f, white, blend: 0.01f);
        FurTufts(b, tail, tp[^1], V(0.05f, 0.05f, 0.05f), 6, 0.06f, 0.02f, white, -0.5f, -0.9f, 0.2f);
        int head = b.Head(V(0, 0.46f, 0.02f));
        var c = V(0, 0.53f, 0.03f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0.03f, 0.03f, 0.05f), V(0.06f, 0.015f, 0.04f), red, V(0, 0, -25f));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.6f, 0.01f));
            CatEar(b, ear, V(0.045f * s, 0.6f, 0.01f), V(0.08f * s, 0.7f, -0.02f), 0.025f, white, Rgb(220, 200, 210));
            var at = On(c, r, 0.03f * s, 0.54f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(200, 40, 60), glare: true);
        });
        return b;
    }

    /// <summary>Seviper: a black viper with yellow scales down its back, a red fanged mouth and a tail that is a red blade.</summary>
    private static PokeBuilder Seviper()
    {
        var black = Rgb(56, 60, 80);
        var yellow = Rgb(240, 200, 80);
        var red = Rgb(200, 70, 80);
        var purple = Rgb(140, 100, 180);
        var coil = Spiral3(0.22f, 0.12f, 0.06f, 0.15f, 2.4f, 1.3f, 20);
        var points = new[] { V(0.14f, 0.3f, -0.34f), V(0.18f, 0.16f, -0.28f), V(0.2f, 0.07f, -0.2f) }.Concat(coil)
            .Concat(new[] { V(0.0f, 0.24f, 0.05f), V(0, 0.35f, 0.07f), V(0, 0.44f, 0.06f) }).ToArray();
        var b = new PokeBuilder("Seviper", 0.85f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int neck = Coils(b, points, t => 0.03f + 0.03f * MathF.Min(1f, t * 3f), black);
        // Yellow scales down its back, purple bands
        for (int i = 10; i < points.Length - 2; i += 3)
        {
            // Coils gave every second point a bone of its own, in order after the body's
            float t = (i + 0.5f) / (points.Length - 1);
            var mid = Vector3.Lerp(points[i], points[i + 1], 0.5f);
            b.Mark(i < 2 ? Body : 1 + i / 2, mid + V(0, (0.03f + 0.03f * MathF.Min(1f, t * 3f)) * 0.97f, 0), V(0, 1f, 0), 0.016f, 0.012f, yellow, MarkShape.Diamond);
        }
        int tail = b.Tail(points[0]);
        Blade(b, tail, points[0], points[0] + V(-0.02f, 0.18f, -0.06f), 0.06f, red, V(1f, 0, 0), 0.22f);
        Blade(b, tail, points[0] + V(0, 0.04f, 0), points[0] + V(0.02f, 0.12f, -0.1f), 0.04f, purple, V(1f, 0, 0), 0.22f);
        int head = b.Head(points[^1], neck);
        var c = V(0, 0.48f, 0.08f);
        var r = V(0.075f, 0.06f, 0.1f);
        b.Ell(head, c, r, black);
        b.PaintEll(head, c + V(0, 0.04f, -0.02f), V(0.05f, 0.03f, 0.06f), yellow);
        Grin(b, head, V(0, 0.46f, 0.17f), V(0.045f, 0.022f, 0.035f), red);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.025f * s, 0.46f, 0.17f), V(0.025f * s, 0.41f, 0.18f), 0.01f, White, mat: Shell, blend: 0.003f);
            var at = On(c, r, 0.045f * s, 0.5f);
            b.Mark(head, at, V(0.6f * s, 0.4f, 0.7f), 0.024f, 0.016f, purple);
            b.Eye(head, at, V(0.6f * s, 0.4f, 0.7f), 0.013f, sclera: true, white: yellow, pupil: Rgb(200, 40, 50), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Lunatone and Solrock

    /// <summary>Lunatone: a pale crescent moon of stone floating on edge, pocked with craters, one red eye on its inner curve.</summary>
    private static PokeBuilder Lunatone()
    {
        var b = new PokeBuilder("Lunatone", 0.8f, BodyPlan.Floating, V(0, 0.32f, 0)) { Coat = Shell }.Hover();
        var stone = Rgb(232, 222, 180);
        var crater = Rgb(200, 186, 140);
        var c = V(0, 0.32f, 0);
        var r = V(0.22f, 0.22f, 0.075f);
        b.Ell(Body, c, r, stone);
        b.Cut(Body, c + V(0.12f, 0.03f, 0), V(0.18f, 0.18f, 0.12f));
        foreach (var (x, y, s) in new[] { (-0.14f, 0.1f, 0.025f), (-0.1f, -0.1f, 0.02f), (-0.17f, -0.02f, 0.015f), (-0.04f, 0.18f, 0.016f) })
            b.Mark(Body, V(x, c.Y + y, 0.07f), V(0, 0, 1f), s, s, crater, MarkShape.Ring);
        int head = b.Head(c);
        b.Ell(head, c + V(-0.04f, -0.02f, 0), V(0.02f, 0.02f, 0.02f), stone, blend: 0.01f);
        var at = V(-0.085f, 0.3f, 0.06f);
        b.Mark(Body, at, V(0.3f, 0, 1f), 0.035f, 0.03f, Rgb(40, 36, 40));
        b.Eye(Body, at, V(0.3f, 0, 1f), 0.02f, sclera: true, white: Rgb(232, 120, 140), pupil: Rgb(160, 30, 50), glare: true);
        b.Mark(Body, at + V(0.03f, -0.005f, 0.005f), V(0.6f, 0, 1f), 0.035f, 0.004f, Rgb(40, 36, 40), MarkShape.Bar);
        return Lift(b);
    }

    /// <summary>Solrock: a ball of orange rock with spiky rays of yellow round it like the sun's, and two red eyes in yellow.</summary>
    private static PokeBuilder Solrock()
    {
        var b = new PokeBuilder("Solrock", 0.8f, BodyPlan.Floating, V(0, 0.32f, 0)) { Coat = Shell }.Hover();
        var orange = Rgb(214, 116, 60);
        var yellow = Rgb(246, 210, 100);
        var c = V(0, 0.32f, 0);
        var r = V(0.12f, 0.12f, 0.11f);
        b.Ell(Body, c, r, orange);
        foreach (var n in new[] { V(0.5f, 0.6f, 0.6f), V(-0.6f, -0.4f, 0.7f), V(0.3f, -0.7f, 0.6f), V(-0.2f, 0.8f, 0.5f), V(0.8f, -0.1f, 0.5f) })
            b.Mark(Body, Out(c, r, default, n), n, 0.012f, 0.01f, yellow, MarkShape.Diamond);
        // Rays fanned round it in its own plane, a few turned forward and back
        int rays = b.Part("rays", Body, c, PokeRole.Flame);
        for (int i = 0; i < 10; i++)
        {
            float a = MathF.Tau * i / 10f + 0.2f;
            var dir = V(MathF.Cos(a), MathF.Sin(a), (i % 2 == 0 ? 0.25f : -0.25f));
            var root = c + Vector3.Normalize(dir) * 0.1f;
            Blade(b, rays, root, root + Vector3.Normalize(dir) * 0.16f, 0.04f, yellow, V(0, 0, 1f), 0.25f, Shell);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, 0.33f);
            b.Mark(Body, at, Outward(c, r, at), 0.024f, 0.022f, Rgb(40, 34, 30));
            b.Eye(Body, at, Outward(c, r, at), 0.016f, sclera: true, white: yellow, pupil: Rgb(200, 40, 40), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Corphish line

    /// <summary>Corphish: an orange crayfish with a cream belly, big pincers held up and three spikes on its head.</summary>
    private static PokeBuilder Corphish()
    {
        var b = new PokeBuilder("Corphish", 0.55f, BodyPlan.Quadruped, V(0, 0.16f, 0)) { Coat = Shell };
        var orange = Rgb(232, 106, 60);
        var cream = Rgb(240, 226, 196);
        foreach (var (z, front) in new[] { (0.04f, true), (-0.05f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s, 0.1f, z), front);
                b.Limb(leg, V(0.07f * s, 0.1f, z), V(0.12f * s, 0.06f, z * 1.4f), 0.016f, 0.014f, orange);
                b.Limb(leg, V(0.12f * s, 0.06f, z * 1.4f), V(0.13f * s, 0.01f, z * 1.6f), 0.014f, 0.008f, orange);
            });
        var bc = V(0, 0.16f, 0);
        var br = V(0.1f, 0.09f, 0.09f);
        b.Ell(Body, bc, br, orange);
        b.PaintEll(Body, V(0, 0.13f, 0.06f), V(0.075f, 0.06f, 0.04f), cream);
        foreach (float y in new[] { 0.14f, 0.11f })
            b.PaintEll(Body, V(0, y, 0.085f), V(0.06f, 0.003f, 0.02f), Rgb(200, 186, 160), soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.2f, 0.02f));
            var wrist = V(0.15f * s, 0.26f, 0.04f);
            b.Limb(arm, V(0.08f * s, 0.2f, 0.02f), wrist, 0.02f, 0.02f, orange);
            Pincer(b, arm, wrist, V(0.3f * s, 0.6f, 0.4f), 0.07f, orange);
            b.PaintEll(arm, wrist + Vector3.Normalize(V(0.3f * s, 0.6f, 0.4f)) * 0.06f, V(0.045f, 0.035f, 0.04f), cream);
        });
        int tail = b.Tail(V(0, 0.13f, -0.08f));
        b.Ell(tail, V(0, 0.12f, -0.11f), V(0.04f, 0.025f, 0.04f), orange, blend: 0.015f);
        int head = b.Head(V(0, 0.22f, 0.02f));
        foreach (var (x, lean) in new[] { (0f, 0f), (-0.04f, -0.04f), (0.04f, 0.04f) })
            b.Spike(head, V(x, 0.23f, 0.0f), V(x + lean, 0.33f, -0.03f), 0.02f, orange);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.035f * s, 0.2f);
            b.Eye(Body, at, Outward(bc, br, at), 0.018f, sclera: true, pupil: Rgb(30, 30, 40));
        });
        return b;
    }

    /// <summary>Crawdaunt: a red crayfish, a yellow star on its head, a blue band across its face and two great pincers.</summary>
    private static PokeBuilder Crawdaunt()
    {
        var b = new PokeBuilder("Crawdaunt", 0.85f, BodyPlan.Quadruped, V(0, 0.24f, 0)) { Coat = Shell };
        var red = Rgb(206, 64, 60);
        var cream = Rgb(236, 220, 200);
        var blue = Rgb(110, 170, 220);
        var yellow = Rgb(244, 210, 100);
        foreach (var (z, front) in new[] { (0.04f, true), (-0.05f, false), (-0.12f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s, 0.17f, z), front);
                b.Limb(leg, V(0.05f * s, 0.18f, z), V(0.14f * s, 0.08f, z * 1.2f), 0.02f, 0.018f, red);
                b.Limb(leg, V(0.14f * s, 0.08f, z * 1.2f), V(0.15f * s, 0.01f, z * 1.3f), 0.016f, 0.01f, red);
            });
        var bc = V(0, 0.22f, -0.04f);
        var br = V(0.09f, 0.09f, 0.16f);
        b.Ell(Body, bc, br, red, V(-20f, 0, 0));
        b.PaintEll(Body, V(0, 0.18f, 0.04f), V(0.07f, 0.07f, 0.08f), cream);
        int tail = b.Tail(V(0, 0.2f, -0.18f));
        b.Ell(tail, V(0, 0.18f, -0.24f), V(0.06f, 0.03f, 0.06f), red, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.28f, 0.06f));
            var wrist = V(0.18f * s, 0.34f, 0.12f);
            b.Limb(arm, V(0.08f * s, 0.28f, 0.06f), wrist, 0.026f, 0.026f, red);
            Pincer(b, arm, wrist, V(0.4f * s, 0.4f, 0.6f), 0.1f, red);
            b.PaintEll(arm, wrist + Vector3.Normalize(V(0.4f * s, 0.4f, 0.6f)) * 0.1f, V(0.06f, 0.04f, 0.06f), cream);
        });
        // A head with a blue band across its face, eyes over it and a yellow star on top
        int head = b.Head(V(0, 0.32f, 0.07f));
        var c = V(0, 0.36f, 0.1f);
        var r = V(0.07f, 0.07f, 0.075f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, -0.02f, 0.03f), V(0.075f, 0.025f, 0.06f), blue);
        b.PaintEll(head, c + V(0, -0.055f, 0.03f), V(0.06f, 0.02f, 0.05f), cream);
        StarPoints(b, head, c + V(0, 0.09f, -0.01f), 0.07f, 0.03f, 0f, 0.4f, yellow);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, 0.38f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, sclera: true, pupil: Rgb(30, 30, 40), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Baltoy line

    /// <summary>Baltoy: a clay doll spinning on its point, a red band across its middle with two eyes in it, a spike on its head and two thin arms.</summary>
    private static PokeBuilder Baltoy()
    {
        var b = new PokeBuilder("Baltoy", 0.55f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Shell }.Hover();
        var clay = Rgb(214, 198, 150);
        var red = Rgb(196, 90, 80);
        var c = V(0, 0.24f, 0);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Ell(Body, c, r, clay);
        b.PaintTorus(Body, c + V(0, 0.005f, 0), 0.072f, 0.016f, red);
        b.Ell(Body, V(0, 0.12f, 0), V(0.04f, 0.035f, 0.04f), clay, blend: 0.02f);
        b.Limb(Body, V(0, 0.12f, 0), V(0, 0.2f, 0), 0.022f, 0.03f, clay);
        b.PaintTorus(Body, V(0, 0.125f, 0), 0.038f, 0.008f, red);
        b.Spike(Body, V(0, 0.1f, 0), V(0, 0.02f, 0), 0.028f, clay);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.03f * s, 0.13f, 0));
            b.Tube(arm, Smooth(3, V(0.03f * s, 0.13f, 0), V(0.1f * s, 0.12f, 0.0f), V(0.14f * s, 0.06f, 0.02f)), 0.014f, 0.009f, clay, blend: 0f);
            var at = On(c, r, 0.028f * s, 0.245f);
            b.Eye(Body, at, Outward(c, r, at), 0.012f, Rgb(40, 30, 30));
        });
        int head = b.Head(c + V(0, 0.06f, 0));
        b.Ell(head, c + V(0, 0.06f, 0), V(0.03f, 0.025f, 0.03f), clay, blend: 0.01f);
        b.Spike(head, c + V(0, 0.06f, 0), c + V(0, 0.15f, 0), 0.03f, clay);
        return Lift(b);
    }

    /// <summary>Claydol: a black clay idol floating, a ring of pink eyes round its head, white markings and stout arms with round hands.</summary>
    private static PokeBuilder Claydol()
    {
        var b = new PokeBuilder("Claydol", 0.9f, BodyPlan.Floating, V(0, 0.32f, 0)) { Coat = Shell }.Hover();
        var black = Rgb(56, 52, 58);
        var pink = Rgb(236, 140, 150);
        var cream = Rgb(232, 214, 160);
        var bc = V(0, 0.26f, 0);
        var br = V(0.13f, 0.14f, 0.12f);
        b.Ell(Body, bc, br, black);
        b.Spike(Body, V(0, 0.14f, 0), V(0, 0.08f, 0), 0.05f, black, blend: 0.02f);
        b.Mark(Body, Out(bc, br, default, V(0, -0.2f, 1f)), V(0, -0.2f, 1f), 0.05f, 0.05f, Rgb(232, 230, 226), MarkShape.Ring);
        b.Mark(Body, Out(bc, br, default, V(0, -0.2f, 1f)), V(0, -0.2f, 1f), 0.016f, 0.016f, cream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.26f, 0));
            b.Limb(arm, V(0.12f * s, 0.26f, 0), V(0.2f * s, 0.24f, 0.02f), 0.04f, 0.035f, black);
            b.Ell(arm, V(0.23f * s, 0.24f, 0.03f), V(0.045f, 0.045f, 0.045f), black);
            b.PaintTorus(arm, V(0.18f * s, 0.245f, 0.015f), 0.038f, 0.006f, Rgb(232, 230, 226), V(0, 0, 90f));
        });
        // A head of black over the body, a ring of seven pink eyes round its brow, cream knobs between them
        int head = b.Head(V(0, 0.38f, 0));
        var c = V(0, 0.44f, -0.01f);
        var r = V(0.15f, 0.08f, 0.13f);
        b.Ell(head, c, r, black);
        b.PaintTorus(head, c + V(0, 0.01f, 0), 0.145f, 0.012f, Rgb(232, 230, 226), sz: 0.87f);
        for (int i = 0; i < 7; i++)
        {
            float a = (-75f + 25f * i) * Degree;
            var n = V(MathF.Sin(a), 0.1f, MathF.Cos(a));
            var at = Out(c, r, default, n);
            b.Ell(head, at, V(0.03f, 0.028f, 0.03f), pink, blend: 0.006f);
            b.Eye(head, at + Vector3.Normalize(n / r) * 0.022f, n, 0.012f, sclera: true, white: pink, pupil: Rgb(70, 40, 50), closed: true);
            if (i < 6) b.Spike(head, Out(c, r, default, V(MathF.Sin(a + 12.5f * Degree), -0.4f, MathF.Cos(a + 12.5f * Degree))), Out(c, r, default, V(MathF.Sin(a + 12.5f * Degree), -0.4f, MathF.Cos(a + 12.5f * Degree))) + V(0, -0.03f, 0.01f), 0.012f, cream, blend: 0.004f);
        }
        b.Ell(head, c + V(0, 0.07f, -0.01f), V(0.06f, 0.03f, 0.05f), black, blend: 0.02f);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Lileep line

    /// <summary>Lileep: a purple sea lily on a short stalk and a sucker foot, a crown of pink tentacles round a black face with yellow eyes.</summary>
    private static PokeBuilder Lileep()
    {
        var b = new PokeBuilder("Lileep", 0.6f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Shell };
        var purple = Rgb(170, 140, 210);
        var pink = Rgb(244, 170, 170);
        var black = Rgb(46, 42, 54);
        var yellow = Rgb(240, 214, 120);
        b.Ell(Body, V(0, 0.03f, 0), V(0.09f, 0.03f, 0.08f), purple);
        for (int i = 0; i < 4; i++)
        {
            float a = MathF.Tau * i / 4f + 0.4f;
            b.Ell(Body, V(MathF.Sin(a) * 0.08f, 0.025f, MathF.Cos(a) * 0.07f), V(0.035f, 0.025f, 0.035f), purple, blend: 0.02f);
        }
        b.Limb(Body, V(0, 0.04f, 0), V(0, 0.14f, 0), 0.02f, 0.025f, yellow);
        var c = V(0, 0.22f, 0);
        var r = V(0.08f, 0.08f, 0.075f);
        b.Ell(Body, c, r, purple);
        PokeBuilder.Both(s => b.Mark(Body, Out(c, r, default, V(s, -0.3f, 0.3f)), V(s, -0.3f, 0.3f), 0.025f, 0.025f, yellow, MarkShape.Ring));
        int head = b.Head(c + V(0, 0.06f, 0));
        b.Ell(head, c + V(0, 0.06f, 0.02f), V(0.05f, 0.03f, 0.04f), black, blend: 0.01f);
        PokeBuilder.Both(s => b.Eye(head, c + V(0.02f * s, 0.07f, 0.055f), V(0.3f * s, 0.4f, 1f), 0.01f, sclera: true, white: yellow, pupil: black));
        for (int i = 0; i < 8; i++)
        {
            float a = MathF.Tau * i / 8f;
            var root = c + V(MathF.Sin(a) * 0.05f, 0.06f, MathF.Cos(a) * 0.05f);
            var tip = root + V(MathF.Sin(a) * 0.1f, 0.06f, MathF.Cos(a) * 0.1f);
            var mid = Vector3.Lerp(root, tip, 0.5f) + V(0, 0.04f, 0);
            b.Tube(head, Smooth(2, root, mid, tip + V(0, -0.03f, 0)), 0.02f, 0.016f, pink, blend: 0f);
        }
        return b;
    }

    /// <summary>Cradily: a green sea lily standing tall on a sucker foot, a head with yellow ringed eyes and a crown of pink tentacles.</summary>
    private static PokeBuilder Cradily()
    {
        var b = new PokeBuilder("Cradily", 0.9f, BodyPlan.Serpent, V(0, 0.1f, 0)) { Coat = Shell };
        var green = Rgb(150, 200, 110);
        var pink = Rgb(244, 170, 170);
        var black = Rgb(46, 42, 54);
        var yellow = Rgb(240, 214, 120);
        b.Ell(Body, V(0, 0.05f, -0.04f), V(0.12f, 0.05f, 0.12f), green);
        for (int i = 0; i < 4; i++)
        {
            float a = MathF.Tau * i / 4f + 0.4f;
            b.Ell(Body, V(MathF.Sin(a) * 0.11f, 0.04f, -0.04f + MathF.Cos(a) * 0.1f), V(0.05f, 0.04f, 0.05f), green, blend: 0.02f);
        }
        b.Mark(Body, V(0.06f, 0.09f, 0.04f), V(0.4f, 0.6f, 0.7f), 0.03f, 0.03f, yellow, MarkShape.Ring);
        // A stalk rising and bending forward to the head
        int neck = b.Part("stalk", Body, V(0, 0.1f, -0.04f), PokeRole.Segment);
        b.Tube(neck, Smooth(3, V(0, 0.08f, -0.04f), V(0, 0.22f, -0.04f), V(0, 0.36f, 0.02f), V(0, 0.42f, 0.1f)), 0.045f, 0.035f, green, blend: 0f);
        b.PaintTorus(neck, V(0, 0.2f, -0.04f), 0.042f, 0.01f, yellow);
        int head = b.Head(V(0, 0.42f, 0.1f), neck);
        var c = V(0, 0.46f, 0.14f);
        var r = V(0.09f, 0.08f, 0.09f);
        b.Ell(head, c, r, green);
        Grin(b, head, On(c, r, 0, 0.43f), V(0.05f, 0.02f, 0.02f), black);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, 0.48f);
            b.Mark(head, at, Outward(c, r, at), 0.03f, 0.03f, yellow, MarkShape.Ring);
            b.Eye(head, at, Outward(c, r, at), 0.016f, sclera: true, white: Rgb(214, 230, 120), pupil: black);
        });
        for (int i = 0; i < 8; i++)
        {
            float a = MathF.Tau * i / 8f;
            var root = c + V(MathF.Sin(a) * 0.05f, 0.06f, MathF.Cos(a) * 0.05f - 0.02f);
            var tip = root + V(MathF.Sin(a) * 0.12f, 0.08f, MathF.Cos(a) * 0.1f - 0.03f);
            b.Tube(head, Smooth(2, root, Vector3.Lerp(root, tip, 0.5f) + V(0, 0.05f, 0), tip + V(0, -0.04f, 0)), 0.022f, 0.018f, pink, blend: 0f);
        }
        return b;
    }

    // ------------------------------------------------------------------ Anorith line

    /// <summary>Anorith: a green fossil shrimp, a dark shell over its back, a row of white and red fins down each side, eyes on stalks and two clawed arms.</summary>
    private static PokeBuilder Anorith()
    {
        var b = new PokeBuilder("Anorith", 0.6f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Shell };
        var green = Rgb(150, 190, 150);
        var dark = Rgb(70, 76, 80);
        var red = Rgb(214, 80, 90);
        foreach (var (z, front) in new[] { (0.04f, true), (-0.08f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.04f * s, 0.1f, z), front);
                b.Limb(leg, V(0.04f * s, 0.1f, z), V(0.07f * s, 0.01f, z + 0.02f), 0.014f, 0.01f, green);
            });
        var bc = V(0, 0.13f, -0.03f);
        var br = V(0.07f, 0.06f, 0.15f);
        b.Ell(Body, bc, br, green);
        b.Ell(Body, V(0, 0.16f, -0.03f), V(0.07f, 0.05f, 0.15f), dark, blend: 0.01f);
        b.Mark(Body, V(0, 0.21f, 0.06f), V(0, 1f, 0.2f), 0.03f, 0.02f, red, MarkShape.Triangle);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 4; i++)
            {
                var root = V(0.06f * s, 0.15f, 0.06f - i * 0.06f);
                Blade(b, Body, root, root + V(0.12f * s, 0.03f, -0.02f), 0.025f, White, V(0, 1f, 0), 0.25f);
                b.PaintEll(Body, root + V(0.1f * s, 0.025f, -0.02f), V(0.025f, 0.02f, 0.02f), red);
            }
            int arm = b.Arm(s, V(0.04f * s, 0.12f, 0.1f));
            b.Limb(arm, V(0.04f * s, 0.12f, 0.1f), V(0.07f * s, 0.06f, 0.18f), 0.016f, 0.014f, green);
            b.Spike(arm, V(0.07f * s, 0.06f, 0.18f), V(0.06f * s, 0.0f, 0.22f), 0.016f, dark, 0.5f);
        });
        int tail = b.Tail(V(0, 0.15f, -0.17f));
        PokeBuilder.Both(s => Blade(b, tail, V(0, 0.15f, -0.17f), V(0.04f * s, 0.2f, -0.28f), 0.025f, dark, V(1f, 0, 0), 0.25f));
        int head = b.Head(V(0, 0.14f, 0.1f));
        b.Ell(head, V(0, 0.14f, 0.13f), V(0.05f, 0.04f, 0.04f), green, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            var eye = V(0.08f * s, 0.12f, 0.15f);
            b.Limb(head, V(0.03f * s, 0.13f, 0.14f), eye, 0.008f, 0.008f, green);
            b.Ell(head, eye, V(0.025f, 0.025f, 0.025f), White, blend: 0.006f);
            b.Eye(head, eye + V(0.012f * s, 0, 0.02f), V(0.5f * s, 0, 1f), 0.012f, Rgb(40, 40, 60));
        });
        return b;
    }

    /// <summary>Armaldo: an upright blue armoured fossil, a cream plate down its front, great curved blades for arms, a red-and-white fringe on its back and a long tail.</summary>
    private static PokeBuilder Armaldo()
    {
        var b = new PokeBuilder("Armaldo", 0.95f, BodyPlan.Biped, V(0, 0.46f, 0)) { Coat = Shell };
        var blue = Rgb(110, 140, 196);
        var cream = Rgb(236, 210, 130);
        var dark = Rgb(60, 62, 72);
        var red = Rgb(214, 80, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.28f, -0.02f));
            b.Limb(leg, V(0.08f * s, 0.3f, -0.02f), V(0.1f * s, 0.06f, 0.01f), 0.055f, 0.04f, blue);
            b.Ell(leg, V(0.1f * s, 0.2f, 0.0f), V(0.05f, 0.05f, 0.05f), dark, mat: Shell, blend: 0.01f);
            b.Ell(leg, V(0.1f * s, 0.035f, 0.04f), V(0.05f, 0.035f, 0.07f), blue);
        });
        var bc = V(0, 0.46f, 0);
        var br = V(0.11f, 0.17f, 0.09f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, V(0, 0.44f, 0.07f), V(0.08f, 0.14f, 0.04f), cream);
        // A fringe of white fins tipped red down its back
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 4; i++)
            {
                var root = V(0.07f * s, 0.6f - i * 0.06f, -0.06f);
                Blade(b, Body, root, root + V(0.1f * s, 0.03f, -0.06f), 0.025f, White, V(0, 1f, 0), 0.25f);
                b.PaintEll(Body, root + V(0.08f * s, 0.025f, -0.05f), V(0.025f, 0.02f, 0.02f), red);
            }
        });
        int tail = b.Tail(V(0, 0.34f, -0.08f));
        b.Tube(tail, Smooth(3, V(0, 0.34f, -0.07f), V(0, 0.2f, -0.24f), V(0, 0.12f, -0.42f)), 0.05f, 0.02f, blue, blend: 0f);
        PokeBuilder.Both(s => Blade(b, tail, V(0, 0.12f, -0.4f), V(0.06f * s, 0.16f, -0.5f), 0.03f, dark, V(0, 1f, 0), 0.25f));
        // Arms ending in great curved blades
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.56f, 0));
            var elbow = V(0.2f * s, 0.46f, 0.02f);
            b.Limb(arm, V(0.1f * s, 0.56f, 0), elbow, 0.04f, 0.035f, blue);
            b.Ell(arm, elbow, V(0.04f, 0.04f, 0.04f), dark, mat: Shell, blend: 0.01f);
            b.Tube(arm, Smooth(3, elbow, elbow + V(0.04f * s, 0.06f, 0.08f), elbow + V(0.02f * s, 0.12f, 0.16f)), 0.03f, 0.008f, Rgb(214, 220, 230), Shell, 0f);
        });
        // A head with a dark crest and two horns, eyes red
        int head = b.Head(V(0, 0.6f, 0.03f));
        var c = V(0, 0.67f, 0.05f);
        var r = V(0.07f, 0.065f, 0.075f);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, 0.04f, -0.02f), V(0.06f, 0.035f, 0.06f), dark, blend: 0.015f);
        b.PaintEll(head, c + V(0, -0.03f, 0.05f), V(0.04f, 0.025f, 0.03f), cream);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.03f * s, 0.06f, -0.02f), c + V(0.07f * s, 0.16f, -0.05f), 0.02f, dark);
            var at = On(c, r, 0.035f * s, 0.68f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(214, 60, 70), glare: true);
        });
        return b;
    }
}
