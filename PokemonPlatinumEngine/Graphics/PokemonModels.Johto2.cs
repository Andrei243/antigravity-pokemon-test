using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Johto's second batch in National Pokédex
// order: Delibird (225) to Celebi (251), but for the species of the Sinnoh Pokédex in that range, hand-built before.
// Their forms are in PokemonModels.Megas.cs with the other forms. Helpers shared with the earlier batches are in
// PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs, PokemonModels.Kanto1.cs to PokemonModels.Kanto3.cs and
// PokemonModels.Johto1.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Delibird

    /// <summary>Delibird: a round red bird with a white face and beard, a crest of white plumes, and its tail a sack it carries presents in.</summary>
    private static PokeBuilder Delibird()
    {
        var b = new PokeBuilder("Delibird", 0.62f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var red = Rgb(222, 66, 58);
        var white = Rgb(246, 244, 240);
        var yellow = Rgb(246, 200, 72);
        var ring = Rgb(40, 38, 48);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.13f, 0));
            b.Limb(leg, V(0.08f * s, 0.14f, 0), V(0.09f * s, 0.035f, 0.02f), 0.026f, 0.02f, yellow);
            Digits(b, leg, V(0.09f * s, 0.022f, 0.025f), V(0, 0, 1f), V(0.7f, 0, 0), 0.05f, 0.017f, yellow);
        });
        var bc = V(0, 0.3f, 0);
        var br = V(0.16f, 0.19f, 0.14f);
        b.Ell(Body, bc, br, red);
        // A shaggy white beard down its chest
        b.PaintEll(Body, V(0, 0.36f, 0.1f), V(0.1f, 0.12f, 0.07f), white);
        for (int i = 0; i < 5; i++)
        {
            float x = -0.06f + 0.03f * i;
            var root = Out(bc, br, default, V(x / br.X, 0.35f, 1f));
            Blade(b, Body, root, root + V(x * 0.4f, -0.12f + MathF.Abs(x) * 0.6f, 0.02f), 0.03f, white, V(0, 0, 1f), 0.3f);
        }
        // Wings for arms, one raised to wave
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.36f, 0));
            var tip = s > 0 ? V(0.3f * s, 0.53f, 0.03f) : V(0.27f * s, 0.22f, 0.05f);
            var root = V(0.12f * s, 0.36f, 0.01f);
            Frond(b, arm, root, tip, 0.07f, red, V(0, 0, 1f), 0.3f, Fur, 0.02f);
            b.PaintEll(arm, Vector3.Lerp(root, tip, 0.85f), V(0.06f, 0.04f, 0.04f), white, Euler(tip - root));
        });
        // The head is the top of the body: a white face, black rings round its eyes, a yellow beak
        int head = b.Head(V(0, 0.42f, 0));
        var c = V(0, 0.48f, 0.02f);
        var r = V(0.12f, 0.11f, 0.11f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, V(0, 0.46f, 0.09f), V(0.11f, 0.1f, 0.07f), white);
        // A crest of white plumes, sharp and tousled
        foreach (var (x, h, z) in new[] { (0f, 0.15f, -0.02f), (-0.05f, 0.12f, -0.01f), (0.05f, 0.13f, -0.01f), (-0.09f, 0.08f, 0.01f), (0.09f, 0.09f, 0.01f) })
            Blade(b, head, c + V(x * 0.7f, 0.07f, z), c + V(x * 1.8f, 0.07f + h, z - 0.04f), 0.03f, white, V(0, 0, 1f), 0.3f);
        b.Spike(head, On(c, r, 0, 0.455f) - V(0, 0, 0.02f), On(c, r, 0, 0.455f) + V(0, -0.015f, 0.06f), 0.03f, yellow, 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.048f * s, 0.5f);
            b.PaintEll(head, at, V(0.034f, 0.036f, 0.03f), ring);
            b.Eye(head, at, Outward(c, r, at), 0.02f, sclera: true, pupil: ring);
        });
        // Its tail, a white sack slung behind it
        int tail = b.Tail(V(0, 0.22f, -0.12f));
        b.Tube(tail, Smooth(3, V(0, 0.24f, -0.11f), V(0.07f, 0.21f, -0.19f), V(0.13f, 0.2f, -0.2f)), 0.035f, 0.035f, white);
        b.Ell(tail, V(0.17f, 0.2f, -0.2f), V(0.1f, 0.11f, 0.1f), Rgb(228, 228, 234), blend: 0.02f);
        return b;
    }

    // ------------------------------------------------------------------ Skarmory

    /// <summary>
    /// Skarmory and its Mega Evolution: a lean bird of steel with red blades for flight feathers, a long beak and a
    /// blade of a crest; the Mega's steel gold, its blades navy and grown into long spears from its wings and tail.
    /// </summary>
    private static PokeBuilder SkarmoryBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Skarmory-Mega" : "Skarmory", mega ? 1f : 0.95f, BodyPlan.Bird, V(0, 0.55f, 0)) { Coat = Metal }.Hover();
        var steel = mega ? Rgb(214, 172, 66) : Rgb(198, 206, 222);
        var dark = mega ? Rgb(40, 48, 92) : Rgb(74, 82, 114);
        var blade = mega ? dark : Rgb(196, 54, 64);
        DanglingLegs(b, 0.04f, 0.46f, -0.02f, 0.2f, 0.014f, dark);
        // A slim body leaning into its flight, plated down its front
        var bc = V(0, 0.55f, 0);
        var br = V(0.085f, 0.15f, 0.095f);
        b.Ell(Body, bc, br, steel, V(30f, 0, 0));
        foreach (float y in new[] { -0.06f, 0f, 0.06f })
            b.PaintEll(Body, bc + V(0, y, 0.07f + y * 0.5f), V(0.07f, 0.005f, 0.04f), dark, V(30f, 0, 0), 0.006f);
        // Wings of steel: an arm, a broad plate at its end and blades trailing back from under it
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.06f * s, 0.62f, 0.02f);
            var wrist = V(0.22f * s, 0.78f, -0.05f);
            int wing = b.Wing(s, shoulder);
            b.Limb(wing, shoulder, wrist, 0.032f, 0.022f, steel);
            Blade(b, wing, wrist - V(0.04f * s, 0.03f, 0), wrist + V(0.2f * s, 0.1f, -0.08f), 0.055f, steel, V(0, 0.3f, 1f), 0.25f);
            int count = mega ? 6 : 4;
            for (int i = 0; i < count; i++)
            {
                float t = i / (count - 1f);
                var root = Vector3.Lerp(shoulder + V(0.04f * s, 0.02f, 0), wrist, 0.3f + 0.7f * t);
                float len = mega ? 0.34f + 0.12f * t : 0.24f + 0.1f * t;
                var dir = Vector3.Normalize(V((0.5f + 0.5f * t) * s, mega ? 0.5f - 0.2f * t : -0.1f, -1f));
                Blade(b, wing, root, root + dir * len, 0.045f, blade, V(0, 1f, 0.2f), 0.25f, Shell);
            }
        });
        // A tail of blades fanned down behind it
        int tail = b.Tail(V(0, 0.46f, -0.08f));
        int tails = mega ? 5 : 3;
        for (int i = 0; i < tails; i++)
        {
            float x = (i - (tails - 1) / 2f) / Math.Max(1, tails - 1);
            float len = mega ? 0.38f - MathF.Abs(x) * 0.1f : 0.2f;
            var root = V(x * 0.03f, 0.47f, -0.08f);
            Blade(b, tail, root, root + Vector3.Normalize(V(x * 0.8f, -0.55f, -1f)) * len, 0.03f, i % 2 == 0 || !mega ? steel : dark, V(0, 1f, 0), 0.25f);
        }
        // A neck reaching forward, a long beak a little open and a blade of a crest laid back
        int head = b.Head(V(0, 0.66f, 0.07f));
        var c = V(0, 0.75f, 0.15f);
        var r = V(0.05f, 0.05f, 0.065f);
        b.Limb(head, V(0, 0.64f, 0.06f), c, 0.04f, 0.036f, steel);
        b.Ell(head, c, r, steel);
        b.Spike(head, c + V(0, 0.005f, 0.04f), c + V(0, -0.02f, 0.21f), 0.03f, mega ? steel : Rgb(170, 176, 196), 0.6f);
        b.Spike(head, c + V(0, -0.025f, 0.04f), c + V(0, -0.07f, 0.15f), 0.018f, Rgb(140, 146, 166), 0.6f);
        Blade(b, head, c + V(0, 0.03f, -0.01f), c + V(0, mega ? 0.12f : 0.07f, -0.17f), 0.03f, mega ? dark : steel, V(1f, 0, 0), 0.3f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.7f * s, 0.15f, 0.6f);
            b.Eye(head, Out(c, r, default, look), look, 0.016f, mega ? Rgb(210, 40, 50) : Rgb(240, 200, 60), glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Skarmory() => SkarmoryBuild(false);

    // ------------------------------------------------------------------ Kingdra

    /// <summary>Kingdra: a great blue seahorse with a gold belly, curled tail, white frilled fins and branching spines on its head.</summary>
    private static PokeBuilder Kingdra()
    {
        var b = new PokeBuilder("Kingdra", 0.95f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(98, 170, 222);
        var gold = Rgb(244, 196, 108);
        var plate = PixelCanvas.Mix(gold, Rgb(170, 110, 50), 0.45f);
        var fin = Rgb(232, 234, 238);
        b.Ell(Body, V(0, 0.38f, 0), V(0.12f, 0.15f, 0.11f), blue);
        int tail = b.Tail(V(0, 0.26f, -0.02f));
        SeahorseTail(b, tail, V(0, 0.27f, -0.02f), V(0, 0.12f, 0.03f), 0.11f, 1.6f, 0.068f, blue);
        b.PaintEll(Body, V(0, 0.36f, 0.07f), V(0.075f, 0.15f, 0.05f), gold);
        for (int i = 0; i < 5; i++)
            b.PaintEll(Body, V(0, 0.28f + i * 0.042f, 0.1f), V(0.065f, 0.005f, 0.03f), plate, soft: 0.006f);
        // Frilled fins on its back, pale and ribbed
        int back = b.Part("fin", Body, V(0, 0.42f, -0.08f), PokeRole.Fin, 0.5f);
        foreach (var (from, to) in new[] { (V(0, 0.44f, -0.07f), V(0, 0.58f, -0.3f)), (V(0, 0.36f, -0.08f), V(0, 0.32f, -0.3f)) })
        {
            Frond(b, back, from, to, 0.07f, fin, V(1f, 0, 0), 0.2f, Shell);
            foreach (float t in new[] { 0.45f, 0.75f })
                b.PaintEll(back, Vector3.Lerp(from, to, t), V(0.08f, 0.004f, 0.02f), Rgb(196, 200, 210), Euler(to - from) + V(90f, 0, 0), 0.005f);
        }
        // A long thin snout and branching spines over its head like coral
        int head = b.Head(V(0, 0.52f, 0));
        var c = V(0, 0.62f, 0.02f);
        var r = V(0.1f, 0.095f, 0.11f);
        b.Ell(head, c, r, blue);
        var tip = V(0, 0.6f, 0.31f);
        b.Limb(head, V(0, 0.6f, 0.08f), tip, 0.034f, 0.026f, blue);
        b.Ell(head, tip, V(0.036f, 0.036f, 0.022f), blue, blend: 0.015f);
        BellMouth(b, head, tip + V(0, 0, 0.014f), V(0, 0, 1f), 0.022f, blue, Rgb(40, 50, 70), 0.9f);
        Coral(b, head, c + V(0, 0.07f, 0), c + V(0, 0.22f, -0.04f), 0.02f, blue, V(0.05f, 0.03f, 0.02f));
        PokeBuilder.Both(s =>
        {
            Coral(b, head, c + V(0.05f * s, 0.06f, -0.02f), c + V(0.15f * s, 0.16f, -0.08f), 0.018f, blue, V(0.01f * s, 0.06f, 0.02f));
            Coral(b, head, c + V(0.06f * s, 0.0f, -0.05f), c + V(0.14f * s, 0.02f, -0.18f), 0.016f, blue, V(0.02f * s, 0.05f, 0));
        });
        PokeBuilder.Both(s =>
        {
            var look = V(0.72f * s, 0.2f, 0.65f);
            b.Eye(head, Out(c, r, default, look), look, 0.025f, Rgb(210, 50, 60));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Phanpy line

    /// <summary>Phanpy: a little blue elephant, its great ears tipped red, its trunk raised.</summary>
    private static PokeBuilder Phanpy()
    {
        var b = new PokeBuilder("Phanpy", 0.5f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Fur };
        var blue = Rgb(124, 200, 216);
        var red = Rgb(230, 98, 72);
        StubbyLegs(b, 0.08f, 0.15f, 0.07f, -0.09f, 0.045f, blue);
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { 0.07f, -0.09f })
                b.Ell(Body, V(0.086f * s, 0.022f, z + 0.065f), V(0.02f, 0.016f, 0.012f), White, mat: Shell, blend: 0.006f);
        });
        b.Ell(Body, V(0, 0.21f, -0.01f), V(0.13f, 0.12f, 0.15f), blue);
        int tail = b.Tail(V(0, 0.22f, -0.15f));
        b.Limb(tail, V(0, 0.22f, -0.15f), V(0, 0.2f, -0.21f), 0.014f, 0.01f, blue);
        PointedLeaf(b, tail, V(0, 0.2f, -0.2f), V(0, 0.19f, -0.26f), 0.025f, blue, V(1f, 0, 0), 0.3f);
        // A big head, a trunk curling up at its end
        int head = b.Head(V(0, 0.26f, 0.1f));
        var c = V(0, 0.3f, 0.12f);
        var r = V(0.12f, 0.11f, 0.11f);
        b.Ell(head, c, r, blue);
        var trunk = Smooth(3, V(0, 0.27f, 0.2f), V(0, 0.22f, 0.27f), V(0, 0.22f, 0.32f), V(0, 0.26f, 0.35f));
        b.Tube(head, trunk, 0.04f, 0.03f, blue, blend: 0f);
        b.Ell(head, trunk[^1], V(0.033f, 0.033f, 0.022f), blue, Euler(trunk[^1] - trunk[^2]) + V(90f, 0, 0), blend: 0.01f);
        PokeBuilder.Both(s => b.Mark(head, trunk[^1] + Vector3.Normalize(trunk[^1] - trunk[^2]) * 0.02f + V(0.012f * s, 0, 0), trunk[^1] - trunk[^2], 0.007f, 0.009f, Rgb(50, 70, 80)));
        b.Mark(head, On(c, r, 0, 0.235f), V(0, -0.4f, 1f), 0.025f, 0.012f, Rgb(170, 60, 70), MarkShape.Smile);
        // Great flat ears, red at their edges
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.1f * s, 0.33f, 0.09f));
            var ec = V(0.15f * s, 0.32f, 0.07f);
            b.Ell(ear, ec, V(0.018f, 0.08f, 0.09f), blue, V(0, 25f * s, -20f * s), blend: 0.02f);
            b.PaintEll(ear, ec + V(0.03f * s, 0.06f, -0.04f), V(0.05f, 0.05f, 0.06f), red);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.32f), V(0.45f * s, 0.05f, 1f), 0.016f, Rgb(40, 40, 50)));
        return b;
    }

    /// <summary>Donphan: an armoured elephant, the dark plates on its back ridged like a tyre, white tusks and long ear-flaps.</summary>
    private static PokeBuilder Donphan()
    {
        var b = new PokeBuilder("Donphan", 0.85f, BodyPlan.Quadruped, V(0, 0.3f, -0.03f)) { Coat = Shell };
        var gray = Rgb(148, 162, 186);
        var armour = Rgb(84, 88, 102);
        var groove = Rgb(54, 56, 66);
        var red = Rgb(164, 60, 84);
        StubbyLegs(b, 0.12f, 0.2f, 0.12f, -0.15f, 0.065f, gray, armour);
        var bc = V(0, 0.29f, -0.03f);
        b.Ell(Body, bc, V(0.17f, 0.15f, 0.23f), gray);
        // The armour over its back, from its brow to its tail, ridged across like a tyre's tread
        var ac = V(0, 0.36f, -0.02f);
        var ar = V(0.2f, 0.15f, 0.27f);
        b.Ell(Body, ac, ar, armour, blend: 0.015f);
        for (int i = -3; i <= 3; i++)
        {
            float z = ac.Z + i * 0.07f;
            float k = MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow((z - ac.Z) / ar.Z, 2f)));
            b.PaintTorus(Body, V(0, ac.Y, z), ar.X * k, 0.008f, groove, V(90f, 0, 0), 1f, ar.Y / ar.X);
        }
        // The head under the front of its armour: a trunk, white tusks, flaps of ears hanging at its sides
        int head = b.Head(V(0, 0.3f, 0.2f));
        var c = V(0, 0.27f, 0.25f);
        var r = V(0.1f, 0.09f, 0.09f);
        b.Ell(head, c, r, gray);
        b.Tube(head, Smooth(3, V(0, 0.25f, 0.32f), V(0, 0.18f, 0.37f), V(0, 0.12f, 0.38f)), 0.042f, 0.032f, gray, blend: 0f);
        PokeBuilder.Both(s =>
        {
            var root = V(0.055f * s, 0.22f, 0.31f);
            b.Tube(head, Smooth(3, root, V(0.1f * s, 0.2f, 0.42f), V(0.13f * s, 0.24f, 0.5f)), 0.024f, 0.01f, Claw, Shell, 0f);
            int ear = b.Ear(head, s, V(0.12f * s, 0.34f, 0.2f));
            var ec = V(0.17f * s, 0.28f, 0.17f);
            b.Ell(ear, ec, V(0.02f, 0.09f, 0.13f), armour, V(-10f, 0, 8f * s), blend: 0.02f);
            b.PaintEll(ear, ec + V(-0.01f * s, -0.07f, 0.02f), V(0.03f, 0.03f, 0.12f), red);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.29f), V(0.5f * s, 0.05f, 1f), 0.015f, Rgb(40, 40, 50), glare: true));
        int tail = b.Tail(V(0, 0.3f, -0.27f));
        b.Limb(tail, V(0, 0.3f, -0.27f), V(0, 0.24f, -0.34f), 0.015f, 0.01f, gray);
        Blade(b, tail, V(0, 0.24f, -0.33f), V(0, 0.2f, -0.41f), 0.035f, armour, V(1f, 0, 0), 0.3f);
        return b;
    }

    // ------------------------------------------------------------------ Stantler

    /// <summary>Stantler: a slender deer with a broad dark nose, cream spots on its haunches and antlers that hold black orbs.</summary>
    private static PokeBuilder Stantler()
    {
        var b = new PokeBuilder("Stantler", 0.92f, BodyPlan.Quadruped, V(0, 0.48f, -0.02f)) { Coat = Fur };
        var brown = Rgb(170, 126, 94);
        var cream = Rgb(240, 222, 184);
        var hoof = Rgb(64, 58, 76);
        var nose = Rgb(112, 70, 52);
        var antler = Rgb(226, 198, 98);
        var orb = Rgb(50, 46, 70);
        foreach (var (z, front) in new[] { (0.16f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.075f * s, 0.42f, z), front);
                var knee = V(0.08f * s, 0.24f, z + (front ? 0.01f : -0.04f));
                b.Limb(leg, V(0.075f * s, 0.44f, z), knee, front ? 0.05f : 0.06f, 0.026f, brown);
                b.Limb(leg, knee, V(0.08f * s, 0.05f, z + 0.01f), 0.024f, 0.02f, brown);
                b.Ell(leg, V(0.08f * s, 0.025f, z + 0.015f), V(0.026f, 0.025f, 0.032f), hoof, mat: Shell);
            });
        var bc = V(0, 0.48f, -0.02f);
        var br = V(0.11f, 0.11f, 0.24f);
        b.Ell(Body, bc, br, brown);
        b.Ell(Body, V(0, 0.5f, 0.13f), V(0.11f, 0.12f, 0.1f), brown, blend: 0.04f);
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z, k) in new[] { (0.5f, -0.15f, 1f), (0.45f, -0.08f, 0.7f) })
            {
                var n = V(s, (y - bc.Y) / br.Y, (z - bc.Z) / br.Z * 0.5f);
                b.Mark(Body, Out(bc, br, default, n), n, 0.018f * k, 0.014f * k, cream);
            }
        });
        // A fluffy cream tail
        int tail = b.Tail(V(0, 0.52f, -0.25f));
        b.Ell(tail, V(0, 0.53f, -0.27f), V(0.05f, 0.06f, 0.045f), cream, blend: 0.02f);
        // A long neck, a head with a broad dark nose and antlers that bend out and forward
        int head = b.Head(V(0, 0.6f, 0.17f));
        b.Limb(head, V(0, 0.56f, 0.15f), V(0, 0.74f, 0.21f), 0.055f, 0.045f, brown);
        b.PaintEll(head, V(0, 0.62f, 0.21f), V(0.04f, 0.07f, 0.03f), cream);
        var c = V(0, 0.77f, 0.23f);
        var r = V(0.068f, 0.068f, 0.085f);
        b.Ell(head, c, r, brown);
        b.Ell(head, V(0, 0.74f, 0.31f), V(0.048f, 0.045f, 0.06f), brown, blend: 0.03f);
        b.Ell(head, V(0, 0.735f, 0.36f), V(0.036f, 0.032f, 0.024f), nose, mat: Shell, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.8f, 0.2f));
            CatEar(b, ear, V(0.05f * s, 0.79f, 0.2f), V(0.13f * s, 0.82f, 0.18f), 0.026f, brown, cream);
            int horn = head;
            var path = Smooth(3, V(0.025f * s, 0.82f, 0.22f), V(0.06f * s, 0.96f, 0.19f), V(0.15f * s, 1.06f, 0.2f), V(0.24f * s, 1.06f, 0.29f));
            b.Tube(horn, path, 0.02f, 0.012f, antler, Shell, 0f);
            b.Limb(horn, path[path.Length / 2], path[path.Length / 2] + V(0.02f * s, 0.11f, -0.02f), 0.013f, 0.009f, antler, Shell, 0.004f);
            b.Limb(horn, path[path.Length / 3], path[path.Length / 3] + V(-0.04f * s, 0.09f, 0.03f), 0.012f, 0.008f, antler, Shell, 0.004f);
            b.Ell(horn, path[(path.Length * 2) / 3] + V(0, 0.004f, 0), V(0.03f, 0.03f, 0.03f), orb, mat: Shell, blend: 0.006f);
        });
        PokeBuilder.Both(s =>
        {
            var look = V(0.75f * s, 0.15f, 0.65f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f, Rgb(70, 46, 40), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Smeargle

    /// <summary>Smeargle: a cream painter with floppy brown ears, a beret of fur, paw-prints on its back and a tail tipped like a brush.</summary>
    private static PokeBuilder Smeargle()
    {
        var b = new PokeBuilder("Smeargle", 0.75f, BodyPlan.Biped, V(0, 0.35f, 0)) { Coat = Fur };
        var cream = Rgb(240, 232, 212);
        var cap = Rgb(230, 222, 198);
        var brown = Rgb(150, 98, 66);
        var paint = Rgb(108, 178, 82);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.055f * s, 0.25f, 0));
            b.Limb(leg, V(0.055f * s, 0.26f, 0), V(0.065f * s, 0.05f, 0.01f), 0.032f, 0.026f, cream);
            b.Ell(leg, V(0.066f * s, 0.028f, 0.03f), V(0.035f, 0.028f, 0.05f), cream);
            b.PaintEll(leg, V(0.066f * s, 0.02f, -0.01f), V(0.03f, 0.025f, 0.02f), brown);
        });
        var bc = V(0, 0.35f, 0);
        var br = V(0.085f, 0.125f, 0.075f);
        b.Ell(Body, bc, br, cream);
        b.PaintTorus(Body, V(0, 0.44f, 0), 0.06f, 0.014f, brown, sz: 0.9f);
        foreach (var (x, y) in new[] { (-0.03f, 0.38f), (0.035f, 0.31f) })
        {
            var n = V(x / br.X, (y - bc.Y) / br.Y, -1f);
            b.Mark(Body, Out(bc, br, default, n), n, 0.018f, 0.02f, brown);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.41f, 0));
            var hand = s > 0 ? V(0.16f * s, 0.5f, 0.07f) : V(0.14f * s, 0.3f, 0.05f);
            b.Limb(arm, V(0.07f * s, 0.41f, 0), hand, 0.022f, 0.02f, cream);
            b.Ell(arm, hand, V(0.026f, 0.026f, 0.026f), cream);
        });
        // A long tail curling round from behind to its raised paw, its tip a brush wet with paint
        int tail = b.Tail(V(0, 0.28f, -0.07f));
        var path = Smooth(3, V(0, 0.27f, -0.06f), V(0.1f, 0.2f, -0.16f), V(0.24f, 0.32f, -0.08f), V(0.22f, 0.5f, 0.04f), V(0.18f, 0.6f, 0.08f));
        b.Tube(tail, path, 0.016f, 0.013f, cream, blend: 0f);
        var dir = Vector3.Normalize(path[^1] - path[^2]);
        b.Spike(tail, path[^1] - dir * 0.01f, path[^1] + dir * 0.1f, 0.03f, cream, blend: 0.006f);
        b.PaintEll(tail, path[^1] + dir * 0.07f, V(0.04f, 0.04f, 0.04f), paint);
        // A dog's head under a beret of fur, floppy ears, a ring of brown round one eye
        int head = b.Head(V(0, 0.48f, 0.01f));
        var c = V(0, 0.56f, 0.02f);
        var r = V(0.095f, 0.09f, 0.09f);
        b.Ell(head, c, r, cream);
        b.Ell(head, V(0, 0.54f, 0.1f), V(0.045f, 0.035f, 0.04f), cream, blend: 0.02f);
        b.Ell(head, V(0, 0.55f, 0.137f), V(0.014f, 0.011f, 0.008f), brown, mat: Shell, blend: 0.004f);
        b.Ell(head, V(0, 0.64f, 0.0f), V(0.1f, 0.05f, 0.1f), cap, blend: 0.015f);
        b.Spike(head, V(0, 0.67f, -0.01f), V(0, 0.73f, -0.04f), 0.035f, cap, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.6f, 0));
            b.Ell(ear, V(0.11f * s, 0.55f, -0.005f), V(0.022f, 0.06f, 0.035f), brown, V(0, 0, -15f * s), blend: 0.015f);
        });
        var eyeAt = On(c, r, 0.04f * 1f, 0.57f);
        b.PaintEll(head, eyeAt, V(0.03f, 0.03f, 0.03f), brown);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.57f);
            b.Eye(head, at, Outward(c, r, at), 0.018f, sclera: true, pupil: Rgb(70, 50, 40));
        });
        b.Mark(head, V(0, 0.52f, 0.12f), V(0, -0.3f, 1f), 0.016f, 0.009f, Rgb(230, 110, 130));
        return b;
    }

    // ------------------------------------------------------------------ Tyrogue line

    /// <summary>Tyrogue: a small lilac fighter in brown shorts, three bumps on its head and bands round its wrists.</summary>
    private static PokeBuilder Tyrogue()
    {
        var b = new PokeBuilder("Tyrogue", 0.6f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var skin = Rgb(208, 172, 214);
        var shorts = Rgb(150, 104, 74);
        var wrap = Rgb(240, 236, 226);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.24f, 0));
            b.Limb(leg, V(0.07f * s, 0.25f, 0), V(0.1f * s, 0.06f, 0.01f), 0.03f, 0.025f, skin);
            b.Ell(leg, V(0.1f * s, 0.035f, 0.03f), V(0.042f, 0.035f, 0.06f), shorts);
        });
        b.Ell(Body, V(0, 0.26f, 0), V(0.115f, 0.075f, 0.09f), shorts);
        b.Ell(Body, V(0, 0.36f, 0), V(0.085f, 0.09f, 0.075f), skin, blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.4f, 0));
            var hand = V(0.15f * s, 0.2f, 0.09f);
            b.Limb(arm, V(0.08f * s, 0.4f, 0), hand, 0.026f, 0.022f, skin);
            b.PaintTorus(arm, Vector3.Lerp(V(0.08f * s, 0.4f, 0), hand, 0.75f), 0.024f, 0.009f, wrap, Euler(hand - V(0.08f * s, 0.4f, 0)));
            Digits(b, arm, hand, V(0.2f * s, -1f, 0.4f), V(0.5f, 0, 0.3f), 0.04f, 0.011f, skin);
        });
        b.PaintEll(Body, V(0, 0.25f, 0), V(0.13f, 0.075f, 0.11f), shorts, soft: 0.008f);
        b.PaintEll(Body, V(0, 0.4f, 0), V(0.11f, 0.075f, 0.1f), skin, soft: 0.008f);
        int head = b.Head(V(0, 0.45f, 0.01f));
        var c = V(0, 0.53f, 0.01f);
        var r = V(0.1f, 0.095f, 0.09f);
        b.Ell(head, c, r, skin);
        b.Spike(head, c + V(0, 0.06f, -0.01f), c + V(0, 0.18f, -0.02f), 0.04f, skin, 0.7f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.06f * s, 0.05f, -0.01f), c + V(0.1f * s, 0.13f, -0.02f), 0.03f, skin, 0.7f));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.54f), V(0.4f * s, 0.05f, 1f), 0.022f, Rgb(214, 160, 50), glare: true));
        b.Mark(head, On(c, r, 0, 0.485f), V(0, -0.3f, 1f), 0.014f, 0.004f, Rgb(120, 80, 110), MarkShape.Bar);
        return b;
    }

    /// <summary>Hitmontop: a fighter spinning on the horn of its head, brown and upside down, its legs up in blue spiked boots.</summary>
    private static PokeBuilder Hitmontop()
    {
        var b = new PokeBuilder("Hitmontop", 0.85f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var brown = Rgb(186, 142, 112);
        var blue = Rgb(82, 132, 204);
        var band = Rgb(236, 230, 214);
        // Its head below, a horn under it to spin on
        int head = b.Head(V(0, 0.26f, 0));
        var c = V(0, 0.16f, 0.01f);
        var r = V(0.1f, 0.095f, 0.1f);
        b.Ell(head, c, r, brown);
        b.Spike(head, c - V(0, 0.06f, 0), V(0, 0f, 0.01f), 0.05f, brown);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.04f * s, 0.15f), V(0.4f * s, -0.05f, 1f), 0.02f, Rgb(60, 50, 40), glare: true));
        // Its body above, and a short tail standing up from it
        b.Ell(Body, V(0, 0.36f, 0), V(0.12f, 0.12f, 0.1f), brown);
        int tail = b.Tail(V(0, 0.46f, -0.05f));
        b.Spike(tail, V(0, 0.45f, -0.05f), V(0, 0.56f, -0.1f), 0.035f, brown);
        // Legs kicking up and out, in blue boots with spikes
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.44f, 0));
            var foot = V(0.22f * s, 0.66f, 0.02f);
            b.Limb(leg, V(0.07f * s, 0.43f, 0), foot, 0.032f, 0.026f, brown);
            b.PaintTorus(leg, Vector3.Lerp(V(0.07f * s, 0.43f, 0), foot, 0.7f), 0.028f, 0.01f, band, Euler(foot - V(0.07f * s, 0.43f, 0)));
            b.Ell(leg, foot + V(0.02f * s, 0.03f, 0), V(0.06f, 0.06f, 0.06f), blue);
            b.Spike(leg, foot + V(0.06f * s, 0.06f, 0), foot + V(0.1f * s, 0.12f, 0), 0.016f, band, mat: Shell, blend: 0.004f);
            b.Spike(leg, foot + V(0.0f, 0.09f, 0), foot + V(-0.01f * s, 0.15f, 0), 0.016f, band, mat: Shell, blend: 0.004f);
        });
        // Arms reaching out to steady its spin
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.32f, 0));
            var fist = V(0.28f * s, 0.24f, 0.04f);
            b.Limb(arm, V(0.1f * s, 0.32f, 0), fist, 0.026f, 0.022f, brown);
            b.PaintTorus(arm, Vector3.Lerp(V(0.1f * s, 0.32f, 0), fist, 0.8f), 0.022f, 0.008f, band, Euler(fist - V(0.1f * s, 0.32f, 0)));
            b.Ell(arm, fist + V(0.02f * s, 0, 0), V(0.034f, 0.032f, 0.032f), brown);
        });
        return b;
    }

    // ------------------------------------------------------------------ Smoochum

    /// <summary>Smoochum: a little pink girl with a bob of yellow hair, a sprout on top, big green eyes and a round nose.</summary>
    private static PokeBuilder Smoochum()
    {
        var b = new PokeBuilder("Smoochum", 0.45f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var pink = Rgb(238, 112, 152);
        var hair = Rgb(250, 228, 116);
        var shirt = Rgb(250, 238, 180);
        var nose = Rgb(222, 92, 132);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.1f, 0));
            b.Ell(leg, V(0.055f * s, 0.035f, 0.02f), V(0.04f, 0.035f, 0.05f), shirt);
        });
        b.Ell(Body, V(0, 0.15f, 0), V(0.09f, 0.1f, 0.08f), pink);
        b.Ell(Body, V(0, 0.22f, 0), V(0.105f, 0.05f, 0.09f), shirt, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.22f, 0));
            b.Limb(arm, V(0.08f * s, 0.22f, 0), V(0.13f * s, 0.16f, 0.04f), 0.03f, 0.024f, shirt);
            b.Ell(arm, V(0.135f * s, 0.15f, 0.045f), V(0.022f, 0.022f, 0.022f), pink);
        });
        b.PaintEll(Body, V(0, 0.235f, 0), V(0.12f, 0.045f, 0.11f), shirt, soft: 0.008f);
        b.PaintEll(Body, V(0, 0.1f, 0), V(0.12f, 0.09f, 0.11f), pink, soft: 0.008f);
        // A bob of hair, its fringe cut straight, the face under it
        int head = b.Head(V(0, 0.27f, 0));
        var c = V(0, 0.35f, 0.01f);
        var r = V(0.11f, 0.1f, 0.1f);
        b.Ell(head, V(0, 0.39f, -0.005f), V(0.13f, 0.105f, 0.12f), hair);
        b.CutBox(head, V(0, 0.315f, 0.13f), V(0.1f, 0.075f, 0.09f), Quaternion.Identity);
        b.Ell(head, c, r, pink, blend: 0.01f);
        b.PaintEll(head, V(0, 0.33f, 0.06f), V(0.11f, 0.06f, 0.08f), pink);
        foreach (float s in new[] { -1f, 1f })
            Frond(b, head, V(0.005f * s, 0.48f, -0.01f), V(0.06f * s, 0.55f, -0.02f), 0.03f, hair, V(0, 0, 1f), 0.3f, Leaf);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, 0.36f);
            b.Eye(head, at, Outward(c, r, at), 0.026f, Rgb(70, 170, 130));
        });
        b.Ell(head, On(c, r, 0, 0.31f), V(0.035f, 0.03f, 0.02f), nose, blend: 0.01f);
        return b;
    }

    // ------------------------------------------------------------------ Miltank

    /// <summary>Miltank: a round pink cow standing up, black on its head and hooves, a cream udder spotted pink and a tail tipped black.</summary>
    private static PokeBuilder Miltank()
    {
        var b = new PokeBuilder("Miltank", 0.85f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var pink = Rgb(242, 160, 182);
        var black = Rgb(52, 48, 60);
        var udder = Rgb(250, 232, 170);
        var horn = Rgb(238, 226, 190);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.17f, 0));
            b.Limb(leg, V(0.1f * s, 0.18f, 0), V(0.11f * s, 0.06f, 0.02f), 0.05f, 0.04f, pink);
            b.Ell(leg, V(0.11f * s, 0.035f, 0.03f), V(0.048f, 0.035f, 0.055f), black, mat: Shell);
        });
        var bc = V(0, 0.34f, 0);
        var br = V(0.18f, 0.2f, 0.16f);
        b.Ell(Body, bc, br, pink);
        b.PaintEll(Body, V(0, 0.28f, 0.1f), V(0.13f, 0.12f, 0.1f), udder);
        foreach (var (x, y) in new[] { (-0.05f, 0.31f), (0.05f, 0.31f), (-0.04f, 0.23f), (0.04f, 0.23f) })
        {
            var n = V(x / br.X, (y - bc.Y) / br.Y, 1f);
            b.Mark(Body, Out(bc, br, default, n), n, 0.015f, 0.012f, Rgb(232, 120, 150));
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.43f, 0));
            var hand = s < 0 ? V(0.3f * s, 0.6f, 0.04f) : V(0.25f * s, 0.32f, 0.08f);
            b.Limb(arm, V(0.14f * s, 0.43f, 0), hand, 0.042f, 0.034f, pink);
            b.Ell(arm, hand, V(0.036f, 0.036f, 0.036f), black, mat: Shell, blend: 0.015f);
        });
        // A tail as thin as a whip, ending in a black tuft
        int tail = b.Tail(V(0, 0.22f, -0.15f));
        var path = Smooth(3, V(0, 0.22f, -0.14f), V(0.08f, 0.14f, -0.24f), V(0.2f, 0.08f, -0.22f), V(0.27f, 0.08f, -0.14f));
        b.Tube(tail, path, 0.012f, 0.009f, Rgb(214, 190, 150), blend: 0f);
        b.Ell(tail, path[^1], V(0.024f, 0.024f, 0.024f), black, blend: 0.006f);
        // A head black above, with short horns, ears out to its sides and a happy open mouth
        int head = b.Head(V(0, 0.5f, 0.02f));
        var c = V(0, 0.6f, 0.03f);
        var r = V(0.11f, 0.1f, 0.1f);
        b.Ell(head, c, r, pink);
        b.PaintEll(head, V(0, 0.67f, -0.01f), V(0.12f, 0.06f, 0.12f), black);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.05f * s, 0.07f, -0.01f), c + V(0.08f * s, 0.14f, -0.02f), 0.018f, horn, mat: Shell);
            int ear = b.Ear(head, s, V(0.09f * s, 0.63f, 0.0f));
            b.Ell(ear, V(0.15f * s, 0.63f, 0.0f), V(0.055f, 0.022f, 0.035f), black, V(0, 0, 15f * s), blend: 0.015f);
            b.PaintEll(ear, V(0.16f * s, 0.62f, 0.015f), V(0.035f, 0.012f, 0.02f), pink, V(0, 0, 15f * s));
        });
        Grin(b, head, On(c, r, 0, 0.56f) - V(0, 0, 0.01f), V(0.04f, 0.025f, 0.02f), Rgb(190, 70, 90));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.62f), V(0.4f * s, 0.05f, 1f), 0.02f, Rgb(80, 132, 214)));
        return b;
    }

    // ------------------------------------------------------------------ The legendary beasts

    /// <summary>
    /// A legendary beast's four long legs: hips <paramref name="x"/> apart at <paramref name="hip"/>, the front pair at
    /// <paramref name="front"/> and the back at <paramref name="back"/>, thighs <paramref name="thigh"/> thick, shins
    /// in <paramref name="low"/>, broad paws, and a cuff round each shin if one is given.
    /// </summary>
    private static void BeastLegs(PokeBuilder b, float x, float hip, float front, float back, float thigh, Color coat, Color paw, Color? low = null, Color? cuff = null)
    {
        foreach (var (z, isFront) in new[] { (front, true), (back, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(x * s, hip, z), isFront);
                var knee = V(x * 1.05f * s, hip * 0.5f, z + (isFront ? 0.015f : -0.035f));
                var ankle = V(x * 1.05f * s, 0.06f, z + 0.01f);
                b.Limb(leg, V(x * s, hip + 0.01f, z), knee, thigh, thigh * 0.72f, coat);
                b.Limb(leg, knee, ankle, thigh * 0.7f, thigh * 0.62f, low ?? coat);
                b.Ell(leg, V(x * 1.05f * s, 0.032f, z + 0.03f), V(thigh * 0.78f, 0.032f, thigh * 1.05f), paw);
                foreach (float t in new[] { -1f, 0f, 1f })
                    b.PaintEll(leg, V(x * 1.05f * s + t * thigh * 0.32f, 0.03f, z + 0.03f + thigh * 0.95f), V(0.004f, 0.02f, 0.01f), PixelCanvas.Mix(paw, Black, 0.4f), soft: 0.004f);
                if (cuff is Color band)
                    b.Torus(leg, Vector3.Lerp(knee, ankle, 0.55f), thigh * 0.62f, thigh * 0.17f, band, mat: Metal, blend: 0.006f);
            });
    }

    /// <summary>Raikou: a yellow tiger striped black like lightning, a purple storm cloud on its back, a dark mask, long fangs and a tail of lightning.</summary>
    private static PokeBuilder Raikou()
    {
        var b = new PokeBuilder("Raikou", 1f, BodyPlan.Quadruped, V(0, 0.46f, -0.02f)) { Coat = Fur };
        var yellow = Rgb(246, 200, 66);
        var white = Rgb(240, 240, 246);
        var black = Rgb(48, 46, 60);
        var purple = Rgb(162, 124, 180);
        var light = Rgb(194, 164, 210);
        var bolt = Rgb(124, 202, 238);
        var mask = Rgb(66, 68, 88);
        BeastLegs(b, 0.1f, 0.42f, 0.19f, -0.22f, 0.08f, yellow, white);
        var bc = V(0, 0.46f, -0.02f);
        var br = V(0.15f, 0.14f, 0.28f);
        b.Ell(Body, bc, br, yellow);
        b.Ell(Body, V(0, 0.48f, 0.16f), V(0.14f, 0.14f, 0.12f), yellow, blend: 0.04f);
        b.PaintEll(Body, V(0, 0.38f, 0.14f), V(0.11f, 0.1f, 0.14f), white);
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.5f, -0.18f), (0.47f, 0.0f), (0.52f, 0.12f) })
            {
                var n = V(s, (y - bc.Y) / br.Y, (z - bc.Z) / br.Z * 0.6f);
                b.Mark(Body, Out(bc, br, default, n), n, 0.04f, 0.022f, black, MarkShape.Zigzag, 70f);
            }
        });
        // The storm cloud on its back, billowing out behind
        foreach (var (at, size) in new[] { (V(0, 0.6f, 0.04f), 0.09f), (V(0.05f, 0.63f, -0.08f), 0.09f), (V(-0.05f, 0.62f, -0.16f), 0.085f), (V(0.02f, 0.62f, -0.26f), 0.08f), (V(-0.04f, 0.58f, -0.36f), 0.07f) })
            b.Ell(Body, at + V(0, 0.02f, 0), V(size * 1.45f, size, size * 1.25f), purple, blend: 0.03f);
        foreach (var at in new[] { V(0.06f, 0.72f, -0.06f), V(-0.06f, 0.72f, -0.18f), V(0.04f, 0.7f, -0.3f) })
            b.PaintEll(Body, at, V(0.05f, 0.03f, 0.05f), light);
        // A tail of lightning, a jagged line ending in a star
        int tail = b.Tail(V(0, 0.48f, -0.29f));
        var zig = new[] { V(0, 0.48f, -0.28f), V(0, 0.62f, -0.4f), V(0, 0.56f, -0.47f), V(0, 0.76f, -0.56f), V(0, 0.7f, -0.62f), V(0, 0.86f, -0.66f) };
        b.Tube(tail, zig, 0.015f, 0.011f, bolt, blend: 0f);
        StarPoints(b, tail, zig[^1], 0.04f, 0.016f, 0f, 0.5f, bolt);
        // A broad head under a dark mask, white fangs hanging from its jaw and blue whiskers like sparks
        int head = b.Head(V(0, 0.56f, 0.24f));
        var c = V(0, 0.62f, 0.3f);
        var r = V(0.1f, 0.09f, 0.1f);
        b.Ell(head, c, r, yellow);
        b.Ell(head, V(0, 0.585f, 0.385f), V(0.065f, 0.05f, 0.06f), white, blend: 0.025f);
        b.Ell(head, V(0, 0.69f, 0.28f), V(0.105f, 0.05f, 0.11f), mask, V(-12f, 0, 0), Shell, 0.012f);
        b.Spike(head, V(0, 0.71f, 0.24f), V(0, 0.72f, 0.12f), 0.05f, mask, 0.5f, Shell);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.03f * s, 0.57f, 0.42f), V(0.035f * s, 0.46f, 0.43f), 0.013f, Claw, mat: Shell, blend: 0.004f);
            Blade(b, head, V(0.07f * s, 0.6f, 0.37f), V(0.19f * s, 0.64f, 0.4f), 0.022f, bolt, V(0, 1f, 0), 0.4f);
            Blade(b, head, V(0.07f * s, 0.58f, 0.37f), V(0.17f * s, 0.53f, 0.42f), 0.02f, bolt, V(0, 1f, 0), 0.4f);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.635f), V(0.5f * s, 0.05f, 1f), 0.02f, Rgb(200, 50, 60), glare: true));
        return b;
    }

    /// <summary>Entei: a brown lion of a beast with a shaggy mane, a cream beard, a red face under a gold crown, steel jaws, smoke for a tail and cuffs on its legs.</summary>
    private static PokeBuilder Entei()
    {
        var b = new PokeBuilder("Entei", 1f, BodyPlan.Quadruped, V(0, 0.47f, -0.02f)) { Coat = Fur };
        var brown = Rgb(152, 94, 58);
        var shin = Rgb(84, 56, 44);
        var cream = Rgb(238, 216, 160);
        var steel = Rgb(190, 194, 206);
        var smoke = Rgb(180, 180, 188);
        var red = Rgb(212, 54, 50);
        var gold = Rgb(242, 204, 66);
        BeastLegs(b, 0.11f, 0.42f, 0.2f, -0.22f, 0.088f, brown, Rgb(200, 196, 208), shin, Rgb(56, 54, 64));
        var bc = V(0, 0.47f, -0.02f);
        b.Ell(Body, bc, V(0.16f, 0.15f, 0.28f), brown);
        // A shaggy mane over its shoulders and chest
        var mc = V(0, 0.55f, 0.13f);
        var mr = V(0.18f, 0.17f, 0.15f);
        b.Ell(Body, mc, mr, brown, blend: 0.04f);
        FurTufts(b, Body, mc, mr, 34, 0.07f, 0.04f, brown, 0.8f, -0.5f, 0.5f);
        // Plates of steel rising from its back like a volcano's smoke
        PokeBuilder.Both(s =>
        {
            foreach (var (from, to) in new[] { (V(0.06f, 0.62f, 0.02f), V(0.2f, 0.84f, -0.12f)), (V(0.07f, 0.6f, -0.08f), V(0.24f, 0.74f, -0.26f)), (V(0.05f, 0.58f, -0.16f), V(0.16f, 0.68f, -0.36f)) })
                Blade(b, Body, from * V(s, 1f, 1f), to * V(s, 1f, 1f), 0.05f, steel, V(0.3f * s, 0.2f, 1f), 0.28f, Shell);
        });
        // Smoke for a tail, billowing behind
        int tail = b.Tail(V(0, 0.5f, -0.29f));
        b.Limb(tail, V(0, 0.5f, -0.28f), V(0, 0.56f, -0.36f), 0.05f, 0.06f, smoke);
        GasPuff(b, tail, V(0, 0.56f, -0.36f), V(0, 0.4f, -1f), 0.07f, smoke);
        GasPuff(b, tail, V(0.04f, 0.6f, -0.48f), V(0.3f, 0.8f, -1f), 0.055f, smoke);
        // A heavy head: a red face, steel jaws, a cream beard and a gold crown of three points
        int head = b.Head(V(0, 0.6f, 0.26f));
        var c = V(0, 0.66f, 0.3f);
        var r = V(0.1f, 0.095f, 0.1f);
        b.Ell(head, c, r, brown);
        b.PaintEll(head, V(0, 0.66f, 0.38f), V(0.085f, 0.07f, 0.06f), red);
        b.Ell(head, V(0, 0.61f, 0.4f), V(0.06f, 0.045f, 0.06f), steel, mat: Shell, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.05f * s, 0.61f, 0.4f), V(0.12f * s, 0.52f, 0.42f), 0.03f, steel, 0.6f, Shell, 0.01f);
            Blade(b, head, V(0.035f * s, 0.74f, 0.33f), V(0.09f * s, 0.84f, 0.3f), 0.032f, gold, V(0, 0, 1f), 0.3f, Shell);
        });
        Blade(b, head, V(0, 0.75f, 0.34f), V(0, 0.9f, 0.31f), 0.042f, gold, V(0, 0, 1f), 0.3f, Shell);
        for (int i = 0; i < 5; i++)
        {
            float x = -0.06f + 0.03f * i;
            Blade(b, head, V(x, 0.57f, 0.34f), V(x * 1.3f, 0.43f - MathF.Abs(x), 0.36f), 0.03f, cream, V(0, 0, 1f), 0.3f);
        }
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.048f * s, 0.675f), V(0.5f * s, 0.05f, 1f), 0.019f, Rgb(140, 70, 40), glare: true));
        return b;
    }

    /// <summary>Suicune: a slender blue beast spotted with white diamonds, a purple mane flowing back, a crystal crest and two white ribbons for a tail.</summary>
    private static PokeBuilder Suicune()
    {
        var b = new PokeBuilder("Suicune", 1f, BodyPlan.Quadruped, V(0, 0.48f, -0.02f)) { Coat = Fur };
        var blue = Rgb(122, 206, 228);
        var spot = Rgb(238, 246, 250);
        var purple = Rgb(162, 122, 202);
        var crystal = Rgb(66, 146, 196);
        BeastLegs(b, 0.085f, 0.44f, 0.18f, -0.22f, 0.066f, blue, spot);
        var bc = V(0, 0.48f, -0.02f);
        var br = V(0.12f, 0.11f, 0.27f);
        b.Ell(Body, bc, br, blue);
        b.Ell(Body, V(0, 0.5f, 0.15f), V(0.12f, 0.12f, 0.1f), blue, blend: 0.04f);
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.5f, -0.2f), (0.46f, -0.08f), (0.5f, 0.04f), (0.45f, 0.14f) })
            {
                var n = V(s, (y - bc.Y) / br.Y, (z - bc.Z) / br.Z * 0.6f);
                b.Mark(Body, Out(bc, br, default, n), n, 0.024f, 0.032f, spot, MarkShape.Diamond);
            }
        });
        // A mane flowing back from its head over its back, in two waves
        foreach (float x in new[] { -0.03f, 0.03f })
            Ribbon(b, Body, Smooth(3, V(x, 0.66f, 0.2f), V(x * 2f, 0.66f, 0.04f), V(x * 3f, 0.68f, -0.16f), V(x * 2f, 0.74f, -0.34f), V(x, 0.7f, -0.48f)), 0.12f, purple, V(0, 1f, 0.2f), 0.014f);
        // Two white ribbons for a tail, streaming back and down
        int tail = b.Tail(V(0, 0.52f, -0.28f));
        PokeBuilder.Both(s =>
            Ribbon(b, tail, Smooth(3, V(0.02f * s, 0.54f, -0.27f), V(0.08f * s, 0.5f, -0.42f), V(0.13f * s, 0.34f, -0.46f), V(0.14f * s, 0.2f, -0.38f), V(0.12f * s, 0.18f, -0.26f)), 0.05f, spot, V(1f, 0, 0), 0.016f));
        // A fine head, a white muzzle and a great crest of blue crystal
        int head = b.Head(V(0, 0.6f, 0.24f));
        var c = V(0, 0.65f, 0.29f);
        var r = V(0.082f, 0.075f, 0.09f);
        b.Ell(head, c, r, blue);
        b.Ell(head, V(0, 0.62f, 0.37f), V(0.048f, 0.04f, 0.06f), spot, blend: 0.02f);
        Blade(b, head, c + V(0, 0.05f, 0.04f), c + V(0, 0.26f, -0.07f), 0.07f, crystal, V(1f, 0, 0), 0.18f, Shell);
        PokeBuilder.Both(s => Blade(b, head, c + V(0.05f * s, 0.0f, 0.05f), c + V(0.15f * s, -0.03f, -0.06f), 0.02f, spot, V(0, 1f, 0), 0.35f));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.66f), V(0.5f * s, 0.05f, 1f), 0.018f, Rgb(200, 50, 70), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Larvitar line

    /// <summary>Larvitar: a small green rock-eater with a horn, a red diamond on its belly, spikes at its sides and a stubby tail.</summary>
    private static PokeBuilder Larvitar()
    {
        var b = new PokeBuilder("Larvitar", 0.52f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Scales };
        var green = Rgb(150, 178, 110);
        var red = Rgb(206, 64, 58);
        var hole = Rgb(56, 58, 64);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.1f, 0));
            b.Ell(leg, V(0.075f * s, 0.04f, 0.02f), V(0.045f, 0.04f, 0.06f), green);
        });
        var bc = V(0, 0.22f, 0);
        var br = V(0.11f, 0.15f, 0.1f);
        b.Ell(Body, bc, br, green);
        b.Mark(Body, On(bc, br, -0.01f, 0.18f), V(0, -0.1f, 1f), 0.045f, 0.06f, red, MarkShape.Diamond);
        var holeAt = On(bc, br, 0.06f, 0.2f);
        b.Mark(Body, holeAt, Outward(bc, br, holeAt), 0.016f, 0.022f, hole, MarkShape.Diamond);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.24f, 0.02f));
            b.Limb(arm, V(0.09f * s, 0.24f, 0.02f), V(0.14f * s, 0.2f, 0.05f), 0.026f, 0.02f, green);
            foreach (var (y, z) in new[] { (0.17f, -0.05f), (0.12f, -0.07f) })
                b.Spike(Body, V(0.08f * s, y, z), V(0.15f * s, y - 0.02f, z - 0.04f), 0.03f, green, 0.6f);
        });
        int tail = b.Tail(V(0, 0.12f, -0.08f));
        b.Spike(tail, V(0, 0.12f, -0.08f), V(0, 0.05f, -0.18f), 0.04f, green, 0.7f);
        int head = b.Head(V(0, 0.32f, 0.01f));
        var c = V(0, 0.38f, 0.02f);
        var r = V(0.09f, 0.085f, 0.085f);
        b.Ell(head, c, r, green);
        b.Spike(head, c + V(0, 0.06f, -0.01f), c + V(0, 0.2f, -0.04f), 0.038f, green, 0.75f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.385f);
            b.Mark(head, at + V(0.008f * s, -0.012f, 0), Outward(c, r, at), 0.03f, 0.022f, Rgb(40, 40, 46), MarkShape.Triangle, 180f + 20f * s);
            b.Eye(head, at, Outward(c, r, at), 0.017f, Rgb(200, 50, 50), glare: true);
        });
        return b;
    }

    /// <summary>Pupitar: a hard grey-blue shell, ringed with spikes, its eyes looking out through two slits.</summary>
    private static PokeBuilder Pupitar()
    {
        var b = new PokeBuilder("Pupitar", 0.62f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var gray = Rgb(140, 154, 198);
        var seam = Rgb(88, 94, 124);
        var slit = Rgb(36, 36, 46);
        var bc = V(0, 0.3f, 0);
        var br = V(0.14f, 0.2f, 0.13f);
        b.Ell(Body, bc, br, gray);
        b.Spike(Body, V(0, 0.14f, 0), V(0, 0.06f, 0), 0.05f, gray);
        foreach (float y in new[] { 0.2f, 0.25f })
            b.PaintTorus(Body, V(0, y, 0), br.X * MathF.Sqrt(1f - MathF.Pow((y - bc.Y) / br.Y, 2f)) * 1.02f, 0.006f, seam, sz: br.Z / br.X);
        // Spikes round its shoulders and up over its crown
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 8f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            if (o.Z > 0.7f) continue;
            var root = bc + V(o.X * 0.12f, 0.06f, o.Z * 0.11f);
            Blade(b, Body, root, root + Vector3.Normalize(o + V(0, 0.5f, 0)) * 0.13f, 0.045f, gray, V(0, 1f, 0) - o * 0.5f, 0.35f);
        }
        foreach (var (x, h) in new[] { (0f, 0.16f), (-0.05f, 0.1f), (0.05f, 0.1f) })
            Blade(b, Body, V(x, 0.44f, -0.02f), V(x * 2f, 0.44f + h, -0.06f), 0.045f, gray, V(0, 0, 1f), 0.35f);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.06f * s, 0.34f);
            b.PaintEll(Body, at, V(0.04f, 0.02f, 0.03f), slit, V(0, 0, -20f * s));
            b.Eye(Body, at, Outward(bc, br, at), 0.012f, Rgb(210, 60, 60), glare: true);
        });
        return Lift(b);
    }

    /// <summary>
    /// Tyranitar and its Mega Evolution: a green armoured kaiju, a blue plate down its belly, dark holes in its hide,
    /// spikes down its back and a horn on its head; the Mega's spikes grown long all over, its chest plate red.
    /// </summary>
    private static PokeBuilder TyranitarBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Tyranitar-Mega" : "Tyranitar", 1f, BodyPlan.Biped, V(0, 0.46f, 0)) { Coat = Shell };
        var green = mega ? Rgb(150, 176, 104) : Rgb(142, 172, 108);
        var plate = mega ? Rgb(208, 62, 58) : Rgb(124, 140, 186);
        var hole = Rgb(54, 58, 64);
        float k = mega ? 1.6f : 1f;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.32f, 0));
            b.Ell(leg, V(0.13f * s, 0.25f, 0.01f), V(0.085f, 0.11f, 0.095f), green);
            b.Limb(leg, V(0.13f * s, 0.2f, 0.01f), V(0.13f * s, 0.07f, 0.03f), 0.065f, 0.055f, green);
            b.Ell(leg, V(0.13f * s, 0.035f, 0.06f), V(0.068f, 0.035f, 0.095f), green);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(leg, V(0.13f * s + t * 0.035f, 0.03f, 0.13f), V(0.13f * s + t * 0.045f, 0.015f, 0.17f), 0.014f, Claw, mat: Shell, blend: 0.004f);
            var thigh = V(0.13f * s, 0.27f, 0.01f) + V(0.07f * s, 0, 0.04f);
            b.Mark(leg, thigh, V(s, 0, 0.6f), 0.03f, 0.04f, hole, MarkShape.Triangle, 180f);
        });
        var bc = V(0, 0.48f, 0);
        var br = V(0.165f, 0.22f, 0.15f);
        b.Ell(Body, bc, br, green);
        b.PaintEll(Body, V(0, 0.46f, 0.11f), V(0.09f, 0.17f, 0.07f), plate);
        if (mega)
            b.Mark(Body, On(bc, br, 0, 0.56f), V(0, 0.1f, 1f), 0.035f, 0.035f, hole, MarkShape.Diamond);
        else
            for (int i = 0; i < 4; i++)
                b.PaintEll(Body, V(0, 0.38f + i * 0.06f, 0.15f), V(0.08f, 0.004f, 0.03f), PixelCanvas.Mix(plate, hole, 0.5f), soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            foreach (var (x, y) in new[] { (0.11f, 0.6f), (0.13f, 0.46f) })
            {
                var n = V(x / br.X * s, (y - bc.Y) / br.Y, 0.7f);
                b.Mark(Body, Out(bc, br, default, n), n, 0.03f, 0.04f, hole, MarkShape.Triangle, 180f);
            }
        });
        // Spikes down its back in two rows, and over its shoulders
        PokeBuilder.Both(s =>
        {
            foreach (var (y, len) in new[] { (0.66f, 0.13f), (0.56f, 0.15f), (0.45f, 0.13f), (0.36f, 0.1f) })
            {
                var root = V(0.07f * s, y, -0.11f);
                Blade(b, Body, root, root + Vector3.Normalize(V(0.5f * s, 0.25f, -1f)) * len * k, 0.05f, green, V(0, 1f, 0), 0.32f);
            }
            var sh = V(0.14f * s, 0.62f, -0.02f);
            Blade(b, Body, sh, sh + Vector3.Normalize(V(0.7f * s, 0.6f, -0.2f)) * 0.1f * k, 0.04f, green, V(0, 0, 1f), 0.32f);
        });
        // Short strong arms with claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s, 0.58f, 0.02f));
            var hand = V(0.27f * s, 0.46f, 0.1f);
            b.Limb(arm, V(0.14f * s, 0.58f, 0.02f), hand, 0.05f, 0.04f, green);
            Digits(b, arm, hand, V(0.4f * s, -0.3f, 0.6f), V(0.4f, -0.3f, 0), 0.045f, 0.012f, Claw, Shell);
            if (mega)
                Blade(b, arm, V(0.2f * s, 0.54f, 0.04f), V(0.34f * s, 0.6f, -0.04f), 0.03f, green, V(0, 1f, 0), 0.32f);
        });
        // A thick tail, spiked
        int tail = b.Tail(V(0, 0.34f, -0.12f));
        var path = Smooth(3, V(0, 0.36f, -0.1f), V(0, 0.2f, -0.3f), V(0, 0.12f, -0.48f));
        b.Tube(tail, path, 0.085f, 0.03f, green, blend: 0f);
        foreach (float t in new[] { 0.35f, 0.65f })
        {
            var at = path[(int)(t * (path.Length - 1))];
            Blade(b, tail, at + V(0, 0.03f, 0), at + V(0, 0.1f * k, -0.07f), 0.04f, green, V(1f, 0, 0), 0.32f);
        }
        // A head with a horn and a jaw open in a roar, spikes behind it
        int head = b.Head(V(0, 0.68f, 0.04f));
        var c = V(0, 0.76f, 0.06f);
        var r = V(0.085f, 0.08f, 0.09f);
        b.Ell(head, c, r, green);
        b.Ell(head, V(0, 0.73f, 0.13f), V(0.065f, 0.045f, 0.055f), green, blend: 0.02f);
        Grin(b, head, V(0, 0.715f, 0.16f), V(0.04f, 0.022f, 0.03f), Rgb(170, 60, 70));
        b.Spike(head, c + V(0, 0.07f, 0.01f), c + V(0, 0.19f * (mega ? 1.3f : 1f), -0.03f), 0.035f, green, 0.7f);
        PokeBuilder.Both(s => Blade(b, head, c + V(0.06f * s, 0.02f, -0.05f), c + V(0.14f * s, 0.06f, -0.14f) * V(1f, 1f, 1f), 0.035f, green, V(0, 1f, 0), 0.32f));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 0.77f), V(0.55f * s, 0.05f, 1f), 0.015f, Rgb(200, 50, 50), glare: true));
        return b;
    }

    private static PokeBuilder Tyranitar() => TyranitarBuild(false);

    // ------------------------------------------------------------------ Lugia

    /// <summary>Lugia: a great white diver, its belly blue, dark plates over its eyes and down its back, wings ending in fingers and a long tail with fins.</summary>
    private static PokeBuilder Lugia()
    {
        var b = new PokeBuilder("Lugia", 1f, BodyPlan.Bird, V(0, 0.5f, 0)) { Coat = Fur }.Hover();
        var white = Rgb(240, 242, 250);
        var belly = Rgb(152, 172, 216);
        var navy = Rgb(58, 74, 142);
        DanglingLegs(b, 0.06f, 0.38f, 0.03f, 0.1f, 0.024f, white);
        var bc = V(0, 0.5f, 0);
        var br = V(0.12f, 0.17f, 0.12f);
        b.Ell(Body, bc, br, white, V(-10f, 0, 0));
        b.PaintEll(Body, V(0, 0.44f, 0.08f), V(0.1f, 0.12f, 0.08f), belly);
        foreach (var (y, z) in new[] { (0.64f, -0.06f), (0.56f, -0.1f), (0.48f, -0.12f), (0.4f, -0.12f) })
            Blade(b, Body, V(0, y, z), V(0, y + 0.02f, z - 0.09f), 0.035f, navy, V(1f, 0, 0), 0.3f, Shell);
        // Great wings, their ends spread like fingers
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.09f * s, 0.6f, 0);
            var wrist = V(0.38f * s, 0.78f, -0.04f);
            int wing = b.Wing(s, shoulder);
            b.Limb(wing, shoulder, wrist, 0.065f, 0.04f, white);
            for (int i = 0; i < 5; i++)
            {
                float a = (70f - i * 30f) * Degree;
                var dir = Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), -0.15f));
                Frond(b, wing, wrist - dir * 0.02f, wrist + dir * (0.15f + 0.03f * MathF.Sin(i * 0.8f)), 0.04f, white, V(0, 0, 1f), 0.35f, Fur, 0.012f);
            }
            Frond(b, wing, Vector3.Lerp(shoulder, wrist, 0.3f), Vector3.Lerp(shoulder, wrist, 0.75f) + V(0, -0.12f, -0.04f), 0.07f, white, V(0, 0, 1f), 0.25f, Fur, 0.02f);
        });
        // A long tail, two dark fins at its end
        int tail = b.Tail(V(0, 0.36f, -0.08f));
        var path = Smooth(3, V(0, 0.38f, -0.08f), V(0, 0.26f, -0.26f), V(0, 0.24f, -0.46f), V(0, 0.32f, -0.6f));
        b.Tube(tail, path, 0.05f, 0.018f, white, blend: 0f);
        PokeBuilder.Both(s => Blade(b, tail, path[^2], path[^2] + V(0.1f * s, 0.01f, -0.06f), 0.03f, navy, V(0, 1f, 0), 0.3f, Shell));
        // A long neck, a smooth head with a point behind and dark plates round its eyes
        int head = b.Head(V(0, 0.64f, 0.04f));
        var c = V(0, 0.79f, 0.12f);
        var r = V(0.068f, 0.06f, 0.085f);
        b.Limb(head, V(0, 0.62f, 0.03f), c, 0.055f, 0.045f, white);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.015f, 0.08f), V(0.045f, 0.036f, 0.05f), white, blend: 0.02f);
        b.Spike(head, c + V(0, 0.02f, -0.05f), c + V(0, 0.04f, -0.15f), 0.035f, white, 0.6f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, c.Y + 0.005f);
            b.PaintEll(head, at + V(0.005f * s, -0.008f, -0.01f), V(0.03f, 0.022f, 0.04f), navy, V(0, 0, 15f * s));
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(200, 56, 70));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Ho-Oh

    /// <summary>Ho-Oh: a phoenix of red and gold, white below, its wings tipped green, a gold crest and a great tail fan.</summary>
    private static PokeBuilder HoOh()
    {
        var b = new PokeBuilder("Ho-Oh", 1f, BodyPlan.Bird, V(0, 0.55f, 0)) { Coat = Fur }.Hover();
        var red = Rgb(222, 70, 52);
        var gold = Rgb(250, 196, 66);
        var green = Rgb(112, 192, 92);
        var white = Rgb(246, 240, 228);
        var black = Rgb(52, 50, 62);
        var orange = Rgb(244, 140, 52);
        DanglingLegs(b, 0.045f, 0.44f, 0f, 0.2f, 0.017f, black);
        var bc = V(0, 0.55f, 0);
        b.Ell(Body, bc, V(0.11f, 0.16f, 0.12f), red, V(-12f, 0, 0));
        b.PaintEll(Body, V(0, 0.5f, 0.08f), V(0.09f, 0.11f, 0.07f), white);
        PokeBuilder.Both(s => SpreadWing(b, s, V(0.08f * s, 0.63f, -0.02f), V(0.25f * s, 0.78f, -0.06f), 7, 105f, 15f, 0.4f, 0.045f, red, green, 0.25f));
        int tail = b.Tail(V(0, 0.45f, -0.1f));
        TailFan(b, tail, V(0, 0.45f, -0.1f), 7, 70f, 0.36f, 0.05f, gold, orange, 0.9f);
        int head = b.Head(V(0, 0.7f, 0.04f));
        var c = V(0, 0.8f, 0.07f);
        var r = V(0.065f, 0.06f, 0.07f);
        b.Limb(head, V(0, 0.66f, 0.03f), c, 0.05f, 0.045f, red);
        b.PaintTorus(head, V(0, 0.71f, 0.05f), 0.048f, 0.01f, green, V(15f, 0, 0));
        b.Ell(head, c, r, red);
        b.Spike(head, c + V(0, -0.01f, 0.05f), c + V(0, -0.04f, 0.13f), 0.025f, gold, 0.8f, Shell);
        foreach (var (x, h, z) in new[] { (0f, 0.11f, -0.02f), (-0.03f, 0.08f, -0.03f), (0.03f, 0.08f, -0.03f) })
        {
            var tip = c + V(x * 1.6f, 0.04f + h, z - 0.08f);
            var root = c + V(x, 0.04f, 0.02f);
            Blade(b, head, root, tip, 0.025f, gold, V(1f, 0, 0), 0.4f, Shell);
            b.Ell(head, Vector3.Lerp(root, tip, 0.85f), V(0.018f, 0.018f, 0.018f), gold, blend: 0.008f);
        }
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.05f, 0.8f);
            b.Eye(head, Out(c, r, default, look), look, 0.016f, Rgb(200, 40, 50), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Celebi

    /// <summary>Celebi: a little green fairy with a head like an onion, two feelers, great blue eyes ringed black and clear wings.</summary>
    private static PokeBuilder Celebi()
    {
        var b = new PokeBuilder("Celebi", 0.5f, BodyPlan.Floating, V(0, 0.26f, 0)) { Coat = Fur }.Hover();
        var green = Rgb(198, 234, 144);
        var deep = Rgb(124, 184, 92);
        var tip = Rgb(70, 140, 210);
        var wing = Rgb(222, 240, 238);
        var bc = V(0, 0.25f, 0);
        b.Ell(Body, bc, V(0.06f, 0.08f, 0.055f), green);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.19f, 0));
            b.Limb(leg, V(0.03f * s, 0.19f, 0), V(0.04f * s, 0.12f, 0.01f), 0.022f, 0.02f, green);
            b.Ell(leg, V(0.042f * s, 0.11f, 0.02f), V(0.022f, 0.02f, 0.03f), deep);
            int arm = b.Arm(s, V(0.05f * s, 0.29f, 0));
            b.Limb(arm, V(0.05f * s, 0.29f, 0), V(0.12f * s, 0.27f, 0.03f), 0.018f, 0.015f, green);
            b.Ell(arm, V(0.125f * s, 0.27f, 0.03f), V(0.02f, 0.02f, 0.02f), green);
            int w = b.Wing(s, V(0.03f * s, 0.3f, -0.05f));
            Frond(b, w, V(0.03f * s, 0.3f, -0.04f), V(0.15f * s, 0.4f, -0.1f), 0.05f, wing, V(0, 0.2f, 1f), 0.15f, Shell, 0.01f);
            Frond(b, w, V(0.03f * s, 0.27f, -0.04f), V(0.12f * s, 0.2f, -0.1f), 0.035f, wing, V(0, 0.2f, 1f), 0.15f, Shell, 0.01f);
        });
        // A big head drawn up to a point like an onion's, tipped blue, two feelers
        int head = b.Head(V(0, 0.32f, 0.01f));
        var c = V(0, 0.42f, 0.02f);
        var r = V(0.12f, 0.105f, 0.11f);
        b.Ell(head, c, r, green);
        var top = Smooth(3, c + V(0, 0.06f, -0.02f), c + V(0, 0.15f, -0.05f), c + V(0, 0.22f, -0.12f));
        b.Tube(head, top, 0.08f, 0.012f, green, blend: 0f);
        b.PaintEll(head, top[^1], V(0.04f, 0.04f, 0.05f), tip);
        PokeBuilder.Both(s => Antenna(b, head, c + V(0.05f * s, 0.08f, 0.04f), c + V(0.08f * s, 0.15f, 0.05f), c + V(0.1f * s, 0.2f, 0.02f), 0.007f, deep, tip, 0.012f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.048f * s, 0.41f);
            b.PaintEll(head, at, V(0.04f, 0.044f, 0.03f), Rgb(36, 40, 50));
            b.Eye(head, at, Outward(c, r, at), 0.032f, Rgb(70, 150, 230));
        });
        return Lift(b);
    }
}
