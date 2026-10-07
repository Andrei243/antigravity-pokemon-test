using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Alola's first batch in National Pokédex
// order: Rowlet (722) to Bewear (760). Their forms are in PokemonModels.Regional.cs and PokemonModels.Megas.cs with the
// other forms. Helpers shared with the earlier batches are in PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs,
// PokemonModels.Kanto1.cs to PokemonModels.Kanto3.cs, PokemonModels.Johto1.cs, PokemonModels.Johto2.cs,
// PokemonModels.Hoenn1.cs to PokemonModels.Hoenn3.cs, PokemonModels.Unova1.cs to PokemonModels.Unova4.cs,
// PokemonModels.Kalos1.cs and PokemonModels.Kalos2.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Rowlet line

    /// <summary>Rowlet: a round little owl, tan above and white below, its face two white discs round great dark eyes, a cream beak, a bow tie of two green leaves and orange feet.</summary>
    private static PokeBuilder Rowlet()
    {
        var b = new PokeBuilder("Rowlet", 0.45f, BodyPlan.Bird, V(0, 0.12f, 0)) { Coat = Fur };
        var tan = Rgb(214, 182, 150);
        var white = Rgb(246, 244, 238);
        var green = Rgb(80, 180, 120);
        var orange = Rgb(236, 150, 70);
        BirdLegs(b, 0.04f, 0.05f, 0.01f, 0.008f, orange);
        var c = V(0, 0.12f, 0);
        var r = V(0.1f, 0.1f, 0.09f);
        b.Ell(Body, c, r, tan);
        b.PaintEll(Body, c + V(0, -0.05f, 0.04f), V(0.09f, 0.06f, 0.07f), white);
        PokeBuilder.Both(s =>
        {
            FoldedWing(b, s, c + V(0.08f * s, 0.02f, -0.01f), c + V(0.1f * s, -0.05f, -0.03f), 0.03f, tan, PixelCanvas.Mix(tan, Black, 0.2f));
            var at = On(c, r, 0.035f * s, c.Y + 0.025f);
            b.PaintEll(Body, at, V(0.034f, 0.036f, 0.03f), white);
            b.Eye(Body, at, Outward(c, r, at), 0.022f, Rgb(40, 36, 40));
            Petal(b, Body, c + V(0.005f * s, -0.035f, 0.085f), c + V(0.045f * s, -0.04f, 0.085f), 0.018f, green, Leaf);
        });
        b.Spike(Body, On(c, r, 0, c.Y + 0.005f) - V(0, 0, 0.01f), On(c, r, 0, c.Y + 0.005f) + V(0, -0.02f, 0.015f), 0.013f, Rgb(240, 232, 214), 0.6f, Shell);
        return b;
    }

    /// <summary>Dartrix: a dapper owl, brown above with a white breast, a green hood of two leaves over its head like a heart, its eyes shut in disdain, a leaf at its breast and white-tipped wings.</summary>
    private static PokeBuilder Dartrix()
    {
        var b = new PokeBuilder("Dartrix", 0.6f, BodyPlan.Bird, V(0, 0.16f, 0)) { Coat = Fur };
        var brown = Rgb(120, 86, 62);
        var white = Rgb(246, 244, 238);
        var green = Rgb(80, 170, 110);
        var orange = Rgb(236, 150, 70);
        BirdLegs(b, 0.035f, 0.08f, 0.01f, 0.009f, orange);
        var bc = V(0, 0.16f, 0);
        b.Ell(Body, bc, V(0.065f, 0.085f, 0.06f), brown);
        b.PaintEll(Body, bc + V(0, -0.01f, 0.04f), V(0.05f, 0.07f, 0.03f), white);
        Petal(b, Body, bc + V(-0.02f, 0.05f, 0.055f), bc + V(0.02f, 0.03f, 0.06f), 0.014f, green, Leaf);
        PokeBuilder.Both(s => FoldedWing(b, s, bc + V(0.05f * s, 0.04f, 0), bc + V(0.08f * s, -0.06f, -0.04f), 0.035f, brown, White));
        int head = b.Head(bc + V(0, 0.08f, 0.0f));
        var c = bc + V(0, 0.13f, 0.01f);
        var r = V(0.055f, 0.05f, 0.05f);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0, 0.02f, -0.03f), V(0.065f, 0.055f, 0.045f), green);
        // The hood: two green leaves meeting over the brow, falling to the cheeks
        PokeBuilder.Both(s => Frond(b, head, c + V(0.005f * s, 0.055f, 0.005f), c + V(0.065f * s, -0.025f, 0.015f), 0.04f, green, V(s, 0.4f, 0.6f), 0.3f, Leaf));
        b.Spike(head, On(c, r, 0, c.Y - 0.005f) - V(0, 0, 0.01f), On(c, r, 0, c.Y - 0.005f) + V(0, -0.02f, 0.015f), 0.011f, orange, 0.6f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 36, 40), closed: true);
        });
        return b;
    }

    /// <summary>
    /// Decidueye and its Hisuian form: a tall archer of an owl, wrapped in its wings like a cloak, a hood over its head.
    /// Decidueye's cloak is brown and its hood green with a dark mask, a leaf lacing its chest; the Hisuian Decidueye's
    /// cloak is dark brown, its hood red under a white crest, a red sash across its white breast and a golden beak.
    /// </summary>
    private static PokeBuilder DecidueyeBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Decidueye-Hisui" : "Decidueye", 0.9f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var cloak = hisui ? Rgb(96, 70, 54) : Rgb(150, 110, 76);
        var hood = hisui ? Rgb(186, 46, 40) : Rgb(60, 140, 90);
        var white = Rgb(244, 240, 232);
        var beak = hisui ? Rgb(240, 196, 70) : Rgb(236, 120, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.2f, 0));
            b.Limb(leg, V(0.04f * s, 0.22f, 0), V(0.05f * s, 0.05f, 0.01f), 0.03f, 0.018f, white);
            foreach (var (dx, dz) in new[] { (-0.4f, 1f), (0.4f, 1f), (0f, -0.6f) })
            {
                var toe = V(0.05f * s + dx * 0.02f, 0.01f, dz * 0.04f);
                b.Limb(leg, V(0.05f * s, 0.04f, 0.0f), toe, 0.008f, 0.006f, hisui ? Rgb(60, 50, 44) : Rgb(236, 150, 70));
            }
        });
        var bc = V(0, 0.36f, 0);
        b.Ell(Body, bc, V(0.08f, 0.13f, 0.07f), white);
        // The cloak of its wings, falling from the shoulders round its sides
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.06f * s, 0.1f, 0));
            Frond(b, wing, bc + V(0.06f * s, 0.11f, -0.01f), bc + V(0.12f * s, -0.18f, -0.04f), 0.06f, cloak, V(s, 0, 0.6f), 0.22f);
            if (!hisui)
                foreach (var t in new[] { 0.3f, 0.55f, 0.8f })
                    b.PaintEll(wing, Vector3.Lerp(bc + V(0.06f * s, 0.11f, -0.01f), bc + V(0.12f * s, -0.18f, -0.04f), t) + V(0.02f * s, 0, 0.04f), V(0.01f, 0.012f, 0.012f), Rgb(230, 130, 60));
        });
        b.Ell(Body, bc + V(0, 0.04f, -0.04f), V(0.085f, 0.11f, 0.05f), cloak, blend: 0.02f);
        if (hisui)
        {
            b.PaintEll(Body, bc + V(0, 0.06f, 0.05f), V(0.09f, 0.025f, 0.04f), hood, V(0, 0, 25f));
            FurTufts(b, Body, bc + V(0, -0.06f, 0.02f), V(0.075f, 0.06f, 0.06f), 12, 0.03f, 0.016f, white, 1.1f, -0.9f, 0.7f);
        }
        else PokeBuilder.Both(s => Frond(b, Body, bc + V(0, 0.08f, 0.065f), bc + V(0.06f * s, 0.03f, 0.065f), 0.016f, hood, V(0, 0, 1f), 0.25f, Leaf));
        int head = b.Head(bc + V(0, 0.13f, 0.01f));
        var c = bc + V(0, 0.18f, 0.02f);
        var r = V(0.055f, 0.055f, 0.055f);
        b.Ell(head, c, r, white);
        // The hood over the head and down the back of the neck, the face masked dark below the eyes
        b.Ell(head, c + V(0, 0.015f, -0.02f), V(0.062f, 0.058f, 0.05f), hood, blend: 0.012f);
        b.Spike(head, c + V(0, 0.0f, -0.04f), c + V(0, -0.1f, -0.08f), 0.035f, hood, 0.5f);
        if (hisui)
            foreach (float x in new[] { -0.015f, 0f, 0.015f })
                b.Spike(head, c + V(x, 0.05f, -0.01f), c + V(x * 2.5f, 0.1f, -0.02f), 0.012f, white, 0.5f);
        else b.Ell(head, c + V(0, 0.06f, -0.01f), V(0.012f, 0.012f, 0.012f), White, blend: 0.006f);
        b.PaintEll(head, c + V(0, -0.02f, 0.04f), V(0.05f, 0.022f, 0.03f), hisui ? Rgb(60, 40, 34) : Rgb(30, 60, 50));
        b.Spike(head, On(c, r, 0, c.Y - 0.015f) - V(0, 0, 0.01f), On(c, r, 0, c.Y - 0.015f) + V(0, -0.03f, 0.02f), 0.013f, beak, 0.6f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, hisui ? Rgb(240, 200, 70) : Rgb(240, 150, 60), glare: true);
        });
        return b;
    }

    private static PokeBuilder Decidueye() => DecidueyeBuild(false);

    // ------------------------------------------------------------------ Litten line

    /// <summary>Litten: a little black kitten striped red across its brow and cheeks, red on its paws, yellow eyes rimmed red and a long thin tail.</summary>
    private static PokeBuilder Litten()
    {
        var b = new PokeBuilder("Litten", 0.45f, BodyPlan.Quadruped, V(0, 0.1f, 0)) { Coat = Fur };
        var black = Rgb(56, 56, 70);
        var red = Rgb(230, 70, 60);
        BeastLegs(b, 0.03f, 0.08f, 0.05f, -0.05f, 0.016f, black, red);
        b.Ell(Body, V(0, 0.1f, 0), V(0.04f, 0.04f, 0.065f), black);
        int tail = b.Tail(V(0, 0.11f, -0.06f));
        b.Tube(tail, Smooth(3, V(0, 0.11f, -0.06f), V(0, 0.18f, -0.1f), V(0, 0.26f, -0.09f)), 0.008f, 0.006f, black, blend: 0f);
        b.Ell(tail, V(0, 0.27f, -0.09f), V(0.012f, 0.016f, 0.012f), black, blend: 0.006f);
        int head = b.Head(V(0, 0.14f, 0.05f));
        var c = V(0, 0.17f, 0.07f);
        var r = V(0.055f, 0.048f, 0.048f);
        b.Ell(head, c, r, black);
        b.PaintEll(head, c + V(0, 0.03f, 0.03f), V(0.008f, 0.02f, 0.02f), red);
        PokeBuilder.Both(s =>
        {
            CatEar(b, head, c + V(0.03f * s, 0.03f, -0.01f), c + V(0.05f * s, 0.08f, -0.02f), 0.02f, black, Rgb(110, 110, 130));
            b.PaintEll(head, c + V(0.045f * s, -0.01f, 0.025f), V(0.016f, 0.006f, 0.02f), red, V(0, 0, -15f * s));
            var at = On(c, r, 0.022f * s, c.Y + 0.004f);
            b.PaintEll(head, at, V(0.017f, 0.015f, 0.012f), red);
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, white: Rgb(250, 216, 60), pupil: Rgb(30, 28, 34), glare: true);
        });
        return b;
    }

    /// <summary>Torracat: a fiery cat, red with black stripes, black about its head, two red horns of ears, a yellow bell of fire at its throat and yellow eyes.</summary>
    private static PokeBuilder Torracat()
    {
        var b = new PokeBuilder("Torracat", 0.7f, BodyPlan.Quadruped, V(0, 0.17f, 0)) { Coat = Fur };
        var red = Rgb(226, 80, 56);
        var black = Rgb(56, 56, 70);
        var yellow = Rgb(250, 200, 70);
        BeastLegs(b, 0.05f, 0.14f, 0.08f, -0.08f, 0.026f, red, black, black);
        var bc = V(0, 0.17f, 0);
        var br = V(0.06f, 0.055f, 0.1f);
        b.Ell(Body, bc, br, red);
        PokeBuilder.Both(s =>
        {
            foreach (var z in new[] { -0.06f, -0.02f, 0.02f })
                b.PaintEll(Body, Out(bc, br, default, V(s, 0.4f, z / br.Z)), V(0.012f, 0.04f, 0.01f), black, V(0, 0, 20f * s), 0.004f);
        });
        int tail = b.Tail(bc + V(0, 0.02f, -0.09f));
        b.Tube(tail, Smooth(3, bc + V(0, 0.02f, -0.09f), bc + V(0, 0.08f, -0.15f), bc + V(0, 0.15f, -0.14f)), 0.014f, 0.009f, black, blend: 0f);
        b.Ell(Body, bc + V(0, 0.02f, 0.11f), V(0.028f, 0.028f, 0.024f), yellow, mat: Glow, blend: 0.008f);
        int head = b.Head(bc + V(0, 0.06f, 0.09f));
        var c = bc + V(0, 0.1f, 0.12f);
        var r = V(0.055f, 0.05f, 0.055f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, 0.03f, -0.02f), V(0.05f, 0.03f, 0.05f), black);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.03f * s, 0.035f, -0.01f), c + V(0.07f * s, 0.09f, -0.03f), 0.02f, red, 0.5f);
            var at = On(c, r, 0.024f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, white: Rgb(250, 216, 60), pupil: Rgb(30, 28, 34), glare: true);
        });
        return b;
    }

    /// <summary>Incineroar: a heel of a tiger wrestling on its hind legs, black and banded red, a belt of fire round its waist, broad red arms striped black, a fierce face masked black and yellow eyes.</summary>
    private static PokeBuilder Incineroar()
    {
        var b = new PokeBuilder("Incineroar", 1f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var black = Rgb(56, 56, 70);
        var red = Rgb(214, 60, 50);
        var flame = Rgb(250, 170, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.28f, 0));
            var knee = V(0.1f * s, 0.15f, 0.03f);
            b.Limb(leg, V(0.07f * s, 0.29f, 0), knee, 0.05f, 0.04f, red);
            b.Limb(leg, knee, V(0.1f * s, 0.04f, 0.0f), 0.04f, 0.035f, black);
            b.Ell(leg, V(0.1f * s, 0.03f, 0.02f), V(0.04f, 0.03f, 0.055f), black);
            foreach (var y in new[] { 0.22f, 0.12f })
                b.PaintEll(leg, V(0.09f * s, y, 0.03f), V(0.05f, 0.012f, 0.05f), y > 0.2f ? black : red);
            var shoulder = V(0.12f * s, 0.54f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.08f * s, -0.1f, 0.03f);
            var fist = elbow + V(0.02f * s, -0.12f, 0.05f);
            b.Limb(arm, shoulder, elbow, 0.045f, 0.04f, red);
            b.Limb(arm, elbow, fist, 0.04f, 0.035f, red);
            foreach (var t in new[] { 0.35f, 0.75f })
                b.PaintEll(arm, Vector3.Lerp(shoulder, fist, t), V(0.05f, 0.012f, 0.05f), black, Euler(fist - shoulder));
            b.Ell(arm, fist, V(0.04f, 0.04f, 0.04f), black);
            Claws(b, arm, fist + V(0, 0.01f, 0.035f), V(0.014f, 0, 0), V(0, 0.3f, 1f), 0.02f, 0.008f);
        });
        var bc = V(0, 0.44f, 0);
        var br = V(0.12f, 0.14f, 0.09f);
        b.Ell(Body, bc, br, black);
        b.PaintEll(Body, bc + V(0, -0.01f, 0.07f), V(0.05f, 0.07f, 0.03f), red, V(0, 0, 45f));
        b.PaintEll(Body, bc + V(0, -0.01f, 0.07f), V(0.05f, 0.07f, 0.03f), red, V(0, 0, -45f));
        // The belt of fire round its waist
        for (int i = 0; i < 12; i++)
        {
            float a = i * MathF.Tau / 12f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            FlameTongue(b, Body, bc + V(0, -0.1f, 0) + d * 0.09f, bc + V(0, -0.06f, 0) + d * 0.15f, 0.025f, Rgb(240, 90, 40), flame);
        }
        int tail = b.Tail(bc + V(0, -0.1f, -0.08f));
        b.Tube(tail, Smooth(3, bc + V(0, -0.1f, -0.08f), bc + V(0, -0.2f, -0.18f), bc + V(0, -0.24f, -0.28f)), 0.03f, 0.012f, black, blend: 0f);
        int head = b.Head(bc + V(0, br.Y, 0.02f));
        var c = bc + V(0, br.Y + 0.07f, 0.04f);
        var r = V(0.09f, 0.08f, 0.078f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, 0.03f, 0.025f), V(0.095f, 0.035f, 0.07f), black);
        b.PaintEll(head, c + V(0, -0.03f, 0.04f), V(0.05f, 0.025f, 0.03f), Rgb(240, 236, 226));
        Grin(b, head, On(c, r, 0, c.Y - 0.03f) - V(0, 0, 0.01f), V(0.03f, 0.01f, 0.015f), Rgb(120, 30, 40));
        PokeBuilder.Both(s =>
        {
            CatEar(b, head, c + V(0.05f * s, 0.04f, -0.02f), c + V(0.09f * s, 0.1f, -0.04f), 0.022f, black, red);
            b.Spike(head, c + V(0.065f * s, -0.01f, 0.0f), c + V(0.11f * s, -0.03f, -0.01f), 0.016f, red, 0.5f);
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, sclera: true, white: Rgb(250, 216, 60), pupil: Rgb(30, 28, 34), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Popplio line

    /// <summary>Popplio: a little blue sea lion, a pale blue ruff round its neck, a round pink nose, big dark eyes, navy flippers in front and a tail of two flippers behind.</summary>
    private static PokeBuilder Popplio()
    {
        var b = new PokeBuilder("Popplio", 0.5f, BodyPlan.Quadruped, V(0, 0.1f, 0)) { Coat = Scales };
        var blue = Rgb(70, 120, 200);
        var navy = Rgb(40, 70, 140);
        var pale = Rgb(170, 220, 240);
        var pink = Rgb(240, 150, 180);
        var bc = V(0, 0.09f, -0.01f);
        b.Ell(Body, bc, V(0.06f, 0.06f, 0.08f), blue);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, bc + V(0.05f * s, 0.0f, 0.04f));
            Frond(b, leg, bc + V(0.05f * s, -0.01f, 0.04f), V(0.11f * s, 0.008f, 0.08f), 0.03f, navy, V(0, 1f, 0), 0.25f);
            Frond(b, Body, bc + V(0.02f * s, -0.03f, -0.07f), V(0.06f * s, 0.008f, -0.16f), 0.025f, navy, V(0, 1f, 0), 0.25f);
        });
        b.Ell(Body, bc + V(0, 0.05f, 0.04f), V(0.055f, 0.015f, 0.05f), pale, blend: 0.01f);
        int head = b.Head(bc + V(0, 0.06f, 0.04f));
        var c = bc + V(0, 0.1f, 0.06f);
        var r = V(0.055f, 0.05f, 0.05f);
        b.Ell(head, c, r, blue);
        b.Ell(head, On(c, r, 0, c.Y - 0.01f) + V(0, 0, 0.01f), V(0.018f, 0.016f, 0.016f), pink, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(30, 40, 70));
        });
        return b;
    }

    /// <summary>Brionne: a pale blue sea lion dancing, a white ruff and frilled flippers, a pink nose, a tuft of blue hair beaded with white bubbles, and big blue eyes.</summary>
    private static PokeBuilder Brionne()
    {
        var b = new PokeBuilder("Brionne", 0.65f, BodyPlan.Quadruped, V(0, 0.12f, 0)) { Coat = Scales };
        var blue = Rgb(110, 180, 236);
        var pale = Rgb(214, 236, 248);
        var pink = Rgb(240, 150, 180);
        var bc = V(0, 0.11f, -0.02f);
        b.Ell(Body, bc, V(0.055f, 0.06f, 0.09f), blue);
        b.PaintEll(Body, bc + V(0, -0.03f, 0.02f), V(0.05f, 0.04f, 0.08f), pale);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, bc + V(0.045f * s, 0.0f, 0.05f));
            Frond(b, leg, bc + V(0.045f * s, -0.01f, 0.05f), V(0.12f * s, 0.01f, 0.1f), 0.035f, pale, V(0, 1f, 0), 0.25f);
            Frond(b, Body, bc + V(0.02f * s, -0.03f, -0.08f), V(0.07f * s, 0.01f, -0.18f), 0.03f, pale, V(0, 1f, 0), 0.25f);
        });
        b.Ell(Body, bc + V(0, 0.06f, 0.05f), V(0.05f, 0.016f, 0.045f), White, blend: 0.01f);
        int head = b.Head(bc + V(0, 0.07f, 0.05f));
        var c = bc + V(0, 0.12f, 0.07f);
        var r = V(0.05f, 0.048f, 0.048f);
        b.Ell(head, c, r, blue);
        b.Ell(head, On(c, r, 0, c.Y - 0.012f) + V(0, 0, 0.008f), V(0.016f, 0.014f, 0.014f), pink, blend: 0.006f);
        // A tuft of hair beaded with bubbles
        var tuft = Smooth(3, c + V(0, 0.04f, -0.01f), c + V(0.0f, 0.08f, -0.05f), c + V(0.0f, 0.06f, -0.11f));
        b.Tube(head, tuft, 0.016f, 0.01f, blue, blend: 0f);
        foreach (var at in new[] { tuft[2], tuft[^1] + V(0, 0.0f, -0.015f), c + V(0.04f, 0.03f, -0.01f), c + V(-0.04f, 0.03f, -0.01f) })
            b.Ell(head, at, V(0.014f, 0.014f, 0.014f), White, mat: Glow, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(50, 110, 200));
        });
        return b;
    }

    /// <summary>Primarina: a sea-lion soloist, white with a blue tail fin, long pale teal hair flowing behind her strung with pearls, a pink star on her brow, a white ruff and blue eyes.</summary>
    private static PokeBuilder Primarina()
    {
        var b = new PokeBuilder("Primarina", 0.95f, BodyPlan.Quadruped, V(0, 0.2f, -0.02f)) { Coat = Scales };
        var white = Rgb(240, 244, 250);
        var blue = Rgb(60, 120, 210);
        var teal = Rgb(150, 220, 226);
        var pink = Rgb(240, 150, 180);
        var bc = V(0, 0.18f, -0.02f);
        b.Ell(Body, bc, V(0.07f, 0.1f, 0.08f), white, V(-15f, 0, 0));
        // The tail sweeping back to a fin of blue
        int tail = b.Tail(bc + V(0, -0.06f, -0.04f));
        var tp = Smooth(3, bc + V(0, -0.06f, -0.04f), V(0, 0.05f, -0.14f), V(0.04f, 0.04f, -0.26f));
        b.Tube(tail, tp, 0.05f, 0.02f, white, blend: 0f);
        PokeBuilder.Both(s => Frond(b, tail, tp[^1], tp[^1] + V(0.06f * s, 0.05f, -0.06f), 0.04f, blue, V(0, 1f, 0.3f), 0.25f));
        b.PaintEll(tail, tp[^1], V(0.03f, 0.03f, 0.03f), blue);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, bc + V(0.05f * s, -0.06f, 0.04f));
            Frond(b, leg, bc + V(0.05f * s, -0.06f, 0.04f), V(0.14f * s, 0.01f, 0.1f), 0.04f, white, V(0, 1f, 0), 0.25f);
        });
        b.Ell(Body, bc + V(0, 0.08f, 0.03f), V(0.06f, 0.02f, 0.055f), White, blend: 0.012f);
        int head = b.Head(bc + V(0, 0.1f, 0.03f));
        var c = bc + V(0, 0.15f, 0.05f);
        var r = V(0.045f, 0.045f, 0.05f);
        b.Ell(head, c, r, white);
        b.Spike(head, On(c, r, 0, c.Y - 0.01f) - V(0, 0, 0.01f), On(c, r, 0, c.Y - 0.01f) + V(0, -0.005f, 0.02f), 0.012f, white, 0.7f);
        b.Ell(head, On(c, r, 0, c.Y - 0.01f) + V(0, -0.004f, 0.018f), V(0.008f, 0.007f, 0.007f), pink, blend: 0.004f);
        // Long flowing hair strung with pearls, a pink star on her brow
        PokeBuilder.Both(s =>
        {
            int hair = b.Ear(head, s, c + V(0.02f * s, 0.03f, -0.02f));
            var path = Smooth(3, c + V(0.02f * s, 0.04f, -0.01f), c + V(0.07f * s, 0.06f, -0.08f), c + V(0.1f * s, -0.04f, -0.14f), c + V(0.08f * s, -0.14f, -0.12f));
            Ribbon(b, hair, path, 0.05f, teal, V(s, 0.2f, 0), 0.014f);
            foreach (var p in path.Where((_, i) => i % 3 == 1))
                b.Ell(hair, p + V(0.012f * s, 0, 0), V(0.01f, 0.01f, 0.01f), White, mat: Glow, blend: 0.004f);
            var at = On(c, r, 0.02f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(60, 130, 220));
        });
        b.Mark(head, On(c, r, 0, c.Y + 0.03f), V(0, 0.6f, 1f), 0.014f, 0.014f, pink, MarkShape.Star5);
        return b;
    }

    // ------------------------------------------------------------------ Pikipek line

    /// <summary>
    /// The Pikipek line: woodpeckers black above and white below with white wings, their beaks growing longer and
    /// brighter. Pikipek's head is striped red and white over a gray beak; Trumbeak's beak is long and tipped orange;
    /// Toucannon's is a great bill banded yellow, orange and red.
    /// </summary>
    private static PokeBuilder PeckerBuild(int stage)
    {
        float k = new[] { 1f, 1.25f, 1.6f }[stage];
        var b = new PokeBuilder(new[] { "Pikipek", "Trumbeak", "Toucannon" }[stage], new[] { 0.45f, 0.6f, 0.9f }[stage], BodyPlan.Bird, V(0, 0.12f * k, 0)) { Coat = Fur };
        var black = Rgb(50, 50, 60);
        var white = Rgb(244, 244, 240);
        var red = Rgb(220, 60, 50);
        BirdLegs(b, 0.025f * k, 0.06f * k, 0.0f, 0.007f * k, Rgb(120, 140, 170));
        var bc = V(0, 0.12f, 0) * k;
        var br = V(0.05f, 0.055f, 0.07f) * k;
        b.Ell(Body, bc, br, black);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.04f) * k, V(0.04f, 0.04f, 0.04f) * k, white);
        PokeBuilder.Both(s => FoldedWing(b, s, bc + V(0.035f * s, 0.02f, 0.01f) * k, bc + V(0.045f * s, -0.01f, -0.09f) * k, 0.02f * k, white, black));
        int tail = b.Tail(bc + V(0, 0, -0.06f) * k);
        Frond(b, tail, bc + V(0, 0, -0.06f) * k, bc + V(0, -0.04f, -0.14f) * k, 0.025f * k, black, V(0, 1f, 0.2f), 0.25f);
        if (stage == 2) b.PaintEll(tail, bc + V(0, -0.04f, -0.14f) * k, V(0.02f, 0.02f, 0.02f) * k, red);
        int head = b.Head(bc + V(0, 0.04f, 0.03f) * k);
        var c = bc + V(0, 0.07f, 0.04f) * k;
        var r = V(0.04f, 0.038f, 0.04f) * k;
        b.Ell(head, c, r, black);
        if (stage == 0)
        {
            b.PaintEll(head, c + V(0, 0.03f, 0.0f) * k, V(0.03f, 0.015f, 0.04f) * k, red);
            b.PaintEll(head, c + V(0, 0.012f, 0.005f) * k, V(0.03f, 0.008f, 0.04f) * k, white);
            foreach (float x in new[] { -0.01f, 0.01f })
                b.Spike(head, c + V(x, 0.03f, -0.01f) * k, c + V(x * 2f, 0.06f, -0.03f) * k, 0.012f * k, black, 0.5f);
        }
        else b.PaintEll(head, c + V(0, -0.02f, 0.01f) * k, V(0.03f, 0.02f, 0.03f) * k, white);
        // The beak: short and gray, long and tipped orange, or a great banded bill
        var root = On(c, r, 0, c.Y - 0.004f * k) - V(0, 0, 0.008f * k);
        if (stage == 2)
        {
            var tip = root + V(0, -0.02f, 0.15f) * k;
            b.Spike(head, root, tip, 0.03f * k, Rgb(250, 200, 50), 0.75f, Shell);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.45f), V(0.03f, 0.03f, 0.022f) * k, Rgb(240, 140, 50), soft: 0.006f);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.8f), V(0.025f, 0.025f, 0.025f) * k, red, soft: 0.006f);
            b.PaintEll(head, tip, V(0.015f, 0.015f, 0.02f) * k, black, soft: 0.004f);
        }
        else
        {
            var tip = root + V(0, -0.005f, stage == 0 ? 0.05f : 0.09f) * k;
            b.Spike(head, root, tip, (stage == 0 ? 0.01f : 0.012f) * k, Rgb(150, 150, 160), 0.8f, Shell);
            if (stage == 1) b.PaintEll(head, tip, V(0.012f, 0.012f, 0.025f) * k, Rgb(240, 150, 60), soft: 0.004f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s * k, c.Y + 0.004f * k);
            b.Eye(head, at, Outward(c, r, at), 0.01f * k, Rgb(70, 140, 220), sclera: true);
        });
        return b;
    }

    private static PokeBuilder Pikipek() => PeckerBuild(0);

    private static PokeBuilder Trumbeak() => PeckerBuild(1);

    private static PokeBuilder Toucannon() => PeckerBuild(2);

    // ------------------------------------------------------------------ Yungoos and Gumshoos

    /// <summary>Yungoos: a long low mongoose, pinkish brown with a cream belly, its head topped yellow, a great mouth of teeth, little eyes and a tail tufted yellow.</summary>
    private static PokeBuilder Yungoos()
    {
        var b = new PokeBuilder("Yungoos", 0.55f, BodyPlan.Quadruped, V(0, 0.07f, 0)) { Coat = Fur };
        var brown = Rgb(176, 120, 100);
        var yellow = Rgb(240, 210, 110);
        BeastLegs(b, 0.03f, 0.05f, 0.06f, -0.07f, 0.014f, brown, brown);
        var bc = V(0, 0.07f, 0);
        b.Ell(Body, bc, V(0.04f, 0.035f, 0.11f), brown);
        b.PaintEll(Body, bc + V(0, -0.02f, 0), V(0.035f, 0.02f, 0.1f), yellow);
        int tail = b.Tail(bc + V(0, 0.01f, -0.1f));
        b.Tube(tail, Smooth(3, bc + V(0, 0.01f, -0.1f), bc + V(0, 0.02f, -0.18f), bc + V(0, 0.06f, -0.24f)), 0.016f, 0.01f, brown, blend: 0f);
        foreach (float a in new[] { -30f, 0f, 30f })
            b.Spike(tail, bc + V(0, 0.06f, -0.24f), bc + V(MathF.Sin(a * Degree) * 0.03f, 0.1f, -0.27f), 0.012f, yellow, 0.5f);
        int head = b.Head(bc + V(0, 0.02f, 0.1f));
        var c = bc + V(0, 0.045f, 0.13f);
        var r = V(0.045f, 0.04f, 0.045f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, 0.025f, -0.01f), V(0.045f, 0.022f, 0.04f), yellow, blend: 0.012f);
        Grin(b, head, On(c, r, 0, c.Y - 0.012f) - V(0, 0, 0.008f), V(0.028f, 0.012f, 0.014f), Rgb(150, 50, 60));
        foreach (float x in new[] { -0.018f, -0.006f, 0.006f, 0.018f })
            b.Spike(head, On(c, r, x, c.Y - 0.002f) - V(0, 0, 0.004f), On(c, r, x, c.Y - 0.002f) + V(0, -0.01f, 0.002f), 0.004f, White, mat: Shell, blend: 0.002f);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.035f * s, 0.025f, -0.015f), V(0.012f, 0.012f, 0.008f), brown, blend: 0.006f);
            var at = On(c, r, 0.02f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.007f, Rgb(40, 30, 30));
        });
        return b;
    }

    /// <summary>Gumshoos: a mongoose standing upright like a detective on a stakeout, brown with a yellow front and a yellow pompadour, a mouthful of teeth and a heavy-lidded glare.</summary>
    private static PokeBuilder Gumshoos()
    {
        var b = new PokeBuilder("Gumshoos", 0.85f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Fur };
        var brown = Rgb(130, 96, 76);
        var yellow = Rgb(232, 200, 110);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.12f, 0));
            b.Limb(leg, V(0.05f * s, 0.13f, 0), V(0.06f * s, 0.03f, 0.02f), 0.03f, 0.025f, brown);
            b.Ell(leg, V(0.06f * s, 0.02f, 0.04f), V(0.028f, 0.018f, 0.045f), brown);
            int arm = b.Arm(s, V(0.06f * s, 0.32f, 0.03f));
            b.Limb(arm, V(0.06f * s, 0.32f, 0.03f), V(0.07f * s, 0.24f, 0.07f), 0.016f, 0.014f, brown);
            Claws(b, arm, V(0.07f * s, 0.23f, 0.08f), V(0.006f, 0, 0), V(0, -0.5f, 1f), 0.012f, 0.004f);
        });
        var bc = V(0, 0.24f, 0);
        var br = V(0.08f, 0.13f, 0.07f);
        b.Ell(Body, bc, br, brown);
        b.PaintEll(Body, bc + V(0, 0.0f, 0.05f), V(0.05f, 0.11f, 0.03f), yellow);
        FurTufts(b, Body, bc + V(0, -0.08f, 0), V(0.07f, 0.04f, 0.06f), 10, 0.025f, 0.012f, brown, 1.1f, -0.9f, 0.6f);
        int tail = b.Tail(bc + V(0, -0.09f, -0.06f));
        b.Tube(tail, Smooth(3, bc + V(0, -0.09f, -0.06f), V(0, 0.06f, -0.16f), V(0, 0.03f, -0.28f)), 0.026f, 0.01f, brown, blend: 0f);
        b.PaintEll(tail, V(0, 0.03f, -0.27f), V(0.02f, 0.02f, 0.03f), yellow);
        int head = b.Head(bc + V(0, br.Y, 0.02f));
        var c = bc + V(0, br.Y + 0.05f, 0.04f);
        var r = V(0.055f, 0.05f, 0.055f);
        b.Ell(head, c, r, brown);
        // A yellow pompadour swept forward over the brow
        b.Ell(head, c + V(0, 0.035f, -0.005f), V(0.05f, 0.03f, 0.055f), yellow, blend: 0.012f);
        b.Spike(head, c + V(0, 0.045f, 0.02f), c + V(0, 0.05f, 0.07f), 0.03f, yellow, 0.5f);
        Grin(b, head, On(c, r, 0, c.Y - 0.018f) - V(0, 0, 0.01f), V(0.032f, 0.012f, 0.015f), Rgb(150, 50, 60));
        foreach (float x in new[] { -0.02f, -0.007f, 0.007f, 0.02f })
            b.Spike(head, On(c, r, x, c.Y - 0.008f) - V(0, 0, 0.004f), On(c, r, x, c.Y - 0.008f) + V(0, -0.012f, 0.002f), 0.005f, White, mat: Shell, blend: 0.002f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(170, 60, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Grubbin line

    /// <summary>Grubbin: a white grub in segments, its head brown under an orange cap, with great orange jaws reaching forward and a dark little face between them.</summary>
    private static PokeBuilder Grubbin()
    {
        var path = new[] { V(0, 0.035f, -0.12f), V(0, 0.038f, -0.07f), V(0, 0.042f, -0.02f), V(0, 0.048f, 0.03f) };
        var b = new PokeBuilder("Grubbin", 0.45f, BodyPlan.Serpent, path[0]) { Coat = Shell };
        var white = Rgb(236, 236, 240);
        var orange = Rgb(240, 150, 60);
        var brown = Rgb(130, 86, 60);
        var yellow = Rgb(250, 210, 120);
        int neck = Grub(b, path, 0.03f, 0.04f, white, Rgb(214, 218, 226));
        int head = b.Head(path[^1] + V(0, 0, 0.03f), neck);
        var c = V(0, 0.055f, 0.08f);
        var r = V(0.045f, 0.045f, 0.04f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, 0.02f, -0.005f), V(0.047f, 0.03f, 0.042f), orange, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.025f * s, 0.0f, 0.03f), c + V(0.03f * s, 0.01f, 0.12f), 0.018f, orange, 0.6f, Shell);
            b.PaintEll(head, c + V(0.03f * s, 0.008f, 0.1f), V(0.012f, 0.012f, 0.02f), yellow);
            var at = On(c, r, 0.016f * s, c.Y - 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, Rgb(30, 28, 30), sclera: true);
        });
        return b;
    }

    /// <summary>Charjabug: a battery of a bug, a green box rounded at its corners with dark seams, blue windows at its sides, a white zigzag on its face and two stubby yellow jaws.</summary>
    private static PokeBuilder Charjabug()
    {
        var b = new PokeBuilder("Charjabug", 0.55f, BodyPlan.Biped, V(0, 0.07f, 0)) { Coat = Shell };
        var green = Rgb(80, 140, 80);
        var dark = Rgb(50, 90, 54);
        var blue = Rgb(70, 140, 230);
        b.Box(Body, V(0, 0.07f, -0.01f), V(0.07f, 0.065f, 0.1f), 0.03f, green, mat: Shell);
        foreach (var z in new[] { -0.05f, 0.0f, 0.05f })
            b.PaintEll(Body, V(0, 0.07f, z), V(0.09f, 0.08f, 0.004f), dark, soft: 0.003f);
        PokeBuilder.Both(s => b.Mark(Body, V(0.07f * s, 0.08f, 0.04f), V(s, 0, 0), 0.02f, 0.016f, blue, MarkShape.Bar));
        int head = b.Head(V(0, 0.07f, 0.08f));
        b.Mark(Body, V(0, 0.08f, 0.09f), V(0, 0, 1f), 0.03f, 0.02f, White, MarkShape.Zigzag, 90f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, V(0.035f * s, 0.03f, 0.08f), V(0.04f * s, 0.02f, 0.12f), 0.016f, Rgb(250, 200, 70), 0.6f, Shell);
            b.Eye(Body, V(0.04f * s, 0.1f, 0.088f), V(0.2f * s, 0.1f, 1f), 0.008f, Rgb(30, 28, 30));
        });
        return b;
    }

    /// <summary>Vikavolt: a stag beetle flying like a fighter, dark blue, its great jaws reaching forward edged in yellow and set with teeth, clear wings humming above its back and little legs folded below.</summary>
    private static PokeBuilder Vikavolt()
    {
        var b = new PokeBuilder("Vikavolt", 0.85f, BodyPlan.Bird, V(0, 0.4f, 0)) { Coat = Shell }.Hover();
        var navy = Rgb(40, 70, 120);
        var yellow = Rgb(250, 210, 60);
        var wing = Rgb(220, 236, 246);
        var bc = V(0, 0.4f, -0.04f);
        b.Ell(Body, bc, V(0.06f, 0.05f, 0.12f), navy, mat: Shell);
        b.PaintEll(Body, bc + V(0, 0.04f, -0.02f), V(0.05f, 0.02f, 0.1f), Rgb(180, 190, 200));
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, bc + V(0.03f * s, 0.04f, 0.0f));
            foreach (var (z, len) in new[] { (0.02f, 0.18f), (-0.04f, 0.14f) })
                Frond(b, w, bc + V(0.03f * s, 0.045f, z), bc + V(len * s, 0.07f, z - 0.03f), 0.035f, wing, V(0, 1f, 0), 0.08f, Glow, 0.006f);
            foreach (var z in new[] { 0.04f, -0.02f, -0.07f })
                b.Limb(Body, bc + V(0.04f * s, -0.03f, z), bc + V(0.06f * s, -0.08f, z + 0.02f), 0.006f, 0.005f, Rgb(30, 40, 60));
        });
        int head = b.Head(bc + V(0, 0.0f, 0.1f));
        var c = bc + V(0, 0.0f, 0.13f);
        var r = V(0.04f, 0.03f, 0.04f);
        b.Ell(head, c, r, navy, mat: Shell);
        // The great jaws, edged yellow and toothed on their inner side
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.028f * s, -0.008f, 0.02f);
            var tip = c + V(0.02f * s, -0.01f, 0.24f);
            Blade(b, head, root, tip, 0.03f, navy, V(0, 1f, 0), 0.35f, Shell);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.5f) + V(0.02f * s, 0, 0), V(0.008f, 0.02f, 0.09f), yellow, soft: 0.004f);
            for (int i = 1; i <= 3; i++)
            {
                var at = Vector3.Lerp(root, tip, 0.25f + i * 0.17f) - V(0.012f * s, 0, 0);
                b.Spike(head, at, at + V(-0.012f * s, 0, 0.004f), 0.005f, White, mat: Shell, blend: 0.002f);
            }
            var e = On(c, r, 0.017f * s, c.Y + 0.017f);
            b.Eye(head, e, Outward(c, r, e), 0.008f, Rgb(220, 60, 60), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Crabrawler and Crabominable

    /// <summary>Crabrawler: a purple crab of a boxer, two great teal pincers like gloves, two yellow horns and small pale claws for legs.</summary>
    private static PokeBuilder Crabrawler()
    {
        var b = new PokeBuilder("Crabrawler", 0.55f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Shell };
        var purple = Rgb(110, 80, 160);
        var teal = Rgb(60, 170, 190);
        var cream = Rgb(240, 226, 160);
        PokeBuilder.Both(s =>
        {
            foreach (var (z, front) in new[] { (0.02f, true), (-0.03f, false) })
            {
                int leg = b.Leg(s, V(0.05f * s, 0.07f, z), front);
                b.Limb(leg, V(0.05f * s, 0.07f, z), V(0.09f * s, 0.03f, z + 0.01f), 0.012f, 0.01f, purple);
                b.Spike(leg, V(0.09f * s, 0.03f, z + 0.01f), V(0.1f * s, 0.0f, z + 0.02f), 0.01f, cream, 0.6f, Shell);
            }
            int arm = b.Arm(s, V(0.06f * s, 0.12f, 0.02f));
            var wrist = V(0.12f * s, 0.15f, 0.07f);
            b.Limb(arm, V(0.06f * s, 0.12f, 0.02f), wrist, 0.018f, 0.016f, purple);
            b.Ell(arm, wrist + V(0.02f * s, 0.03f, 0.03f), V(0.05f, 0.05f, 0.05f), teal, mat: Shell);
            b.Spike(arm, wrist + V(0.0f, -0.0f, 0.03f), wrist + V(0.01f * s, -0.02f, 0.08f), 0.016f, cream, 0.6f, Shell);
        });
        var c = V(0, 0.1f, 0);
        var r = V(0.065f, 0.055f, 0.06f);
        b.Ell(Body, c, r, purple, mat: Shell);
        b.PaintEll(Body, c + V(0, -0.02f, 0.04f), V(0.05f, 0.03f, 0.03f), Rgb(150, 120, 200));
        PokeBuilder.Both(s =>
        {
            b.Spike(Body, c + V(0.03f * s, 0.04f, 0.0f), c + V(0.05f * s, 0.11f, -0.01f), 0.016f, cream, 0.6f, Shell);
            var at = On(c, r, 0.02f * s, c.Y + 0.01f);
            b.Eye(Body, at, Outward(c, r, at), 0.01f, Rgb(250, 220, 90), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Crabominable and its Mega Evolution: a great crab of the snow, shaggy white with purple bands, two huge pincers
    /// ringed in white fur with dark purple pads, a yellow crest between its eyes. Mega Crabominable's pincers are crowned
    /// with great blue crystals of ice, and icicles hang below it.
    /// </summary>
    private static PokeBuilder CrabominableBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Crabominable-Mega" : "Crabominable", 1f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var white = Rgb(244, 244, 248);
        var purple = Rgb(70, 60, 120);
        var pad = Rgb(110, 100, 170);
        var ice = Rgb(150, 210, 246);
        PokeBuilder.Both(s =>
        {
            foreach (var (z, front) in new[] { (0.04f, true), (-0.04f, false) })
            {
                int leg = b.Leg(s, V(0.09f * s, 0.16f, z), front);
                var foot = V(0.2f * s, 0.01f, z * 2f);
                b.Limb(leg, V(0.09f * s, 0.16f, z), V(0.17f * s, 0.12f, z * 1.5f), 0.02f, 0.017f, white);
                b.Limb(leg, V(0.17f * s, 0.12f, z * 1.5f), foot, 0.017f, 0.012f, white);
                b.PaintEll(leg, V(0.17f * s, 0.12f, z * 1.5f), V(0.025f, 0.012f, 0.025f), purple);
                if (mega) b.Spike(leg, V(0.17f * s, 0.12f, z * 1.5f), V(0.2f * s, 0.2f, z * 1.5f), 0.014f, ice, 0.6f, Glow);
            }
            // A huge pincer ringed in white fur, its pad dark purple
            int arm = b.Arm(s, V(0.1f * s, 0.28f, 0.02f));
            var wrist = V(0.18f * s, 0.38f, 0.08f);
            b.Limb(arm, V(0.1f * s, 0.28f, 0.02f), wrist, 0.03f, 0.028f, white);
            b.PaintEll(arm, Vector3.Lerp(V(0.1f * s, 0.28f, 0.02f), wrist, 0.5f), V(0.035f, 0.012f, 0.035f), purple, Euler(wrist - V(0.1f * s, 0.28f, 0.02f)));
            var pc = wrist + V(0.04f * s, 0.07f, 0.03f);
            b.Ell(arm, pc, V(0.08f, 0.08f, 0.06f), mega ? purple : white);
            FurTufts(b, arm, pc, V(0.08f, 0.08f, 0.06f), 18, 0.03f, 0.016f, white, 0.6f, -1f, 0.1f);
            b.PaintEll(arm, pc + V(0, 0, 0.05f), V(0.05f, 0.05f, 0.02f), pad);
            foreach (var (x, y) in new[] { (-0.025f, 0.03f), (0.025f, 0.03f), (0f, 0.04f) })
                b.PaintEll(arm, pc + V(x, y, 0.055f), V(0.01f, 0.01f, 0.01f), purple);
            if (mega)
                for (int i = 0; i < 5; i++)
                {
                    float a = (i / 4f - 0.5f) * 140f * Degree;
                    var d = V(MathF.Sin(a), MathF.Cos(a), -0.1f);
                    b.Spike(arm, pc + d * 0.05f, pc + d * (0.13f + 0.03f * MathF.Cos(a)), 0.03f, ice, 0.6f, Glow);
                }
        });
        var c = V(0, 0.22f, 0);
        var r = V(0.11f, 0.1f, 0.09f);
        b.Ell(Body, c, r, white);
        FurTufts(b, Body, c, r, 26, 0.04f, 0.02f, white, 0.75f, -0.9f, 0.7f);
        foreach (var y in new[] { -0.03f, -0.065f })
            b.PaintEll(Body, c + V(0, y, 0), V(0.115f, 0.012f, 0.1f), purple);
        b.PaintEll(Body, On(c, r, 0, c.Y + 0.01f), V(0.05f, 0.03f, 0.03f), purple);
        b.Spike(Body, On(c, r, 0, c.Y + 0.05f) - V(0, 0, 0.02f), On(c, r, 0, c.Y + 0.05f) + V(0, 0.07f, 0.01f), 0.02f, Rgb(246, 220, 90), 0.5f, Shell);
        if (mega)
            foreach (float x in new[] { -0.06f, 0f, 0.06f })
                b.Spike(Body, c + V(x, -0.06f, 0.02f), c + V(x, -0.2f, 0.03f), 0.02f, ice, 0.6f, Glow);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.02f);
            b.Eye(Body, at, Outward(c, r, at), 0.012f, Rgb(80, 150, 230), sclera: true);
        });
        return b;
    }

    private static PokeBuilder Crabominable() => CrabominableBuild(false);

    // ------------------------------------------------------------------ Oricorio

    /// <summary>The look of each of Oricorio's styles: the main colour, the colour at the ends of its feathers, and the style's name after "Oricorio-", or null for the Baile Style.</summary>
    private static readonly (string? Name, Color Main, Color Tips)[] OricorioStyles =
    {
        (null, Rgb(220, 50, 56), Rgb(40, 36, 44)),
        ("Pom-Pom", Rgb(250, 236, 140), Rgb(220, 230, 90)),
        ("Pau", Rgb(246, 170, 190), Rgb(250, 240, 236)),
        ("Sensu", Rgb(170, 150, 220), Rgb(150, 220, 226))
    };

    /// <summary>
    /// Oricorio in each of its four styles, a little bird of the dance: the Baile Style red with black-tipped fans of
    /// wings and a red crest; the Pom-Pom Style yellow, pom-poms for wings and a tuft on its head; the Pa'u Style pink in
    /// a skirt of white grass, a flower of curls on its head; the Sensu Style lilac, its wings fans tipped pale blue and
    /// its hair in a bun.
    /// </summary>
    private static PokeBuilder OricorioBuild(int style)
    {
        var (name, main, tips) = OricorioStyles[style];
        var b = new PokeBuilder(name == null ? "Oricorio" : "Oricorio-" + name, 0.55f, BodyPlan.Bird, V(0, 0.16f, 0)) { Coat = Fur };
        BirdLegs(b, 0.02f, 0.09f, 0.0f, 0.006f, Rgb(240, 160, 170));
        var bc = V(0, 0.16f, 0);
        var br = V(0.05f, 0.06f, 0.05f);
        b.Ell(Body, bc, br, main);
        // The skirt of feathers, or Pa'u's of grass
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(0, -0.03f, 0) + d * 0.035f;
            var tip = bc + V(0, -0.09f, 0) + d * 0.075f;
            Frond(b, Body, root, tip, 0.022f, style == 2 ? Rgb(244, 236, 214) : main, V(d.X, 0.3f, d.Z), 0.25f);
            if (style != 2) b.PaintEll(Body, tip, V(0.02f, 0.02f, 0.02f), tips);
        }
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.04f * s, 0.02f, 0));
            var hand = bc + V(0.12f * s, 0.05f, 0.02f);
            b.Limb(wing, bc + V(0.04f * s, 0.02f, 0), hand, 0.015f, 0.012f, main);
            if (style == 1)
            {
                // A pom-pom on each wing
                b.Ell(wing, hand + V(0.03f * s, 0, 0), V(0.035f, 0.035f, 0.03f), tips);
                FurTufts(b, wing, hand + V(0.03f * s, 0, 0), V(0.035f, 0.035f, 0.03f), 14, 0.02f, 0.012f, tips, 1.1f, -1f, 0f);
            }
            else
                // A fan of feathers
                for (int i = 0; i < 4; i++)
                {
                    float a = (60f - i * 30f) * Degree;
                    var d = V(MathF.Cos(a) * s, MathF.Sin(a), -0.15f);
                    Frond(b, wing, hand, hand + d * 0.07f, 0.016f, main, V(0, 0, 1f), 0.25f);
                    b.PaintEll(wing, hand + d * 0.065f, V(0.016f, 0.016f, 0.016f), tips);
                }
        });
        int head = b.Head(bc + V(0, 0.05f, 0.01f));
        var c = bc + V(0, 0.09f, 0.015f);
        var r = V(0.035f, 0.035f, 0.035f);
        b.Ell(head, c, r, main);
        b.Spike(head, On(c, r, 0, c.Y - 0.004f) - V(0, 0, 0.008f), On(c, r, 0, c.Y - 0.004f) + V(0, -0.012f, 0.014f), 0.008f, Rgb(240, 170, 180), 0.6f, Shell);
        // The crest: a red plume, a tuft, a flower of curls or a bun
        switch (style)
        {
            case 0:
                foreach (var (x, len) in new[] { (-0.01f, 0.06f), (0.01f, 0.07f) })
                    Frond(b, head, c + V(x, 0.03f, -0.01f), c + V(x * 3f, 0.03f + len, -0.04f), 0.012f, main, V(0, 0.3f, 1f), 0.25f);
                break;
            case 1:
                b.Ell(head, c + V(0, 0.045f, -0.005f), V(0.016f, 0.016f, 0.016f), tips, blend: 0.006f);
                break;
            case 2:
                foreach (float x in new[] { -0.02f, 0f, 0.02f })
                    b.Torus(head, c + V(x, 0.04f, -0.005f), 0.01f, 0.005f, main, V(0, 0, 90f), blend: 0.005f);
                break;
            default:
                b.Ell(head, c + V(0, 0.035f, -0.025f), V(0.018f, 0.018f, 0.018f), Rgb(110, 90, 170), blend: 0.008f);
                break;
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.016f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, Rgb(60, 60, 150), closed: style == 3);
        });
        return b;
    }

    private static PokeBuilder Oricorio() => OricorioBuild(0);

    // ------------------------------------------------------------------ Cutiefly and Ribombee

    /// <summary>Cutiefly: a round little yellow fly with great dark eyes, a white ruff, a short proboscis, four thin black legs with round tips and clear wings rimmed brown.</summary>
    private static PokeBuilder Cutiefly()
    {
        var b = new PokeBuilder("Cutiefly", 0.4f, BodyPlan.Bird, V(0, 0.2f, 0)) { Coat = Fur }.Hover();
        var yellow = Rgb(250, 230, 120);
        var brown = Rgb(170, 130, 90);
        var wing = Rgb(232, 240, 246);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.03f * s, 0.24f, -0.02f));
            foreach (var (to, k) in new[] { (V(0.11f * s, 0.32f, -0.04f), 1f), (V(0.1f * s, 0.2f, -0.05f), 0.7f) })
            {
                Frond(b, w, V(0.03f * s, 0.24f, -0.02f), to, 0.035f * k, wing, V(0, 0, 1f), 0.08f, Glow, 0.006f);
                b.PaintEll(w, Vector3.Lerp(V(0.03f * s, 0.24f, -0.02f), to, 0.6f), V(0.03f, 0.03f, 0.03f) * k, brown);
            }
            foreach (var x in new[] { 0.012f, 0.03f })
                Antenna(b, Body, V(x * s, 0.19f, 0.0f), V(x * 1.3f * s, 0.14f, 0.01f), V(x * 1.4f * s, 0.1f, 0.0f), 0.003f, Rgb(40, 36, 44), Rgb(40, 36, 44), 0.007f);
        });
        var c = V(0, 0.22f, 0);
        var r = V(0.05f, 0.048f, 0.045f);
        b.Ell(Body, c, r, yellow);
        b.Ell(Body, c + V(0, -0.035f, 0), V(0.035f, 0.016f, 0.03f), White, blend: 0.012f);
        b.Spike(Body, On(c, r, 0, c.Y - 0.005f) - V(0, 0, 0.005f), On(c, r, 0, c.Y - 0.005f) + V(0, 0, 0.03f), 0.005f, brown, mat: Shell, blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            Antenna(b, Body, c + V(0.01f * s, 0.04f, 0.0f), c + V(0.015f * s, 0.06f, 0.0f), c + V(0.02f * s, 0.07f, -0.005f), 0.003f, brown, brown, 0.005f);
            var at = On(c, r, 0.02f * s, c.Y + 0.004f);
            b.Eye(Body, at, Outward(c, r, at), 0.016f, Rgb(40, 36, 44));
        });
        return Lift(b);
    }

    /// <summary>Ribombee: a little bee-fly upright, a yellow head with great dark eyes and two black feelers, a fluffy brown and white scarf, a yellow body, two thin black legs and clear wings patterned brown.</summary>
    private static PokeBuilder Ribombee()
    {
        var b = new PokeBuilder("Ribombee", 0.5f, BodyPlan.Bird, V(0, 0.2f, 0)) { Coat = Fur }.Hover();
        var yellow = Rgb(250, 220, 90);
        var brown = Rgb(150, 110, 70);
        var wing = Rgb(232, 240, 246);
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.025f * s, 0.24f, -0.02f));
            foreach (var (to, k) in new[] { (V(0.13f * s, 0.32f, -0.04f), 1f), (V(0.11f * s, 0.18f, -0.05f), 0.75f) })
            {
                Frond(b, w, V(0.025f * s, 0.24f, -0.02f), to, 0.04f * k, wing, V(0, 0, 1f), 0.08f, Glow, 0.006f);
                b.PaintEll(w, Vector3.Lerp(V(0.025f * s, 0.24f, -0.02f), to, 0.65f), V(0.03f, 0.03f, 0.03f) * k, brown);
            }
            int leg = b.Leg(s, V(0.012f * s, 0.15f, 0));
            b.Limb(leg, V(0.012f * s, 0.15f, 0), V(0.016f * s, 0.07f, 0.005f), 0.005f, 0.004f, Rgb(40, 36, 44));
            b.Ell(leg, V(0.016f * s, 0.065f, 0.005f), V(0.008f, 0.008f, 0.008f), Rgb(40, 36, 44), blend: 0.004f);
            int arm = b.Arm(s, V(0.02f * s, 0.21f, 0.01f));
            b.Limb(arm, V(0.02f * s, 0.21f, 0.01f), V(0.04f * s, 0.18f, 0.03f), 0.005f, 0.004f, Rgb(40, 36, 44));
        });
        b.Ell(Body, V(0, 0.18f, 0), V(0.025f, 0.035f, 0.022f), yellow);
        // A fluffy scarf of brown with a white edge
        b.Ell(Body, V(0, 0.22f, 0), V(0.035f, 0.016f, 0.03f), brown, blend: 0.01f);
        FurTufts(b, Body, V(0, 0.22f, 0), V(0.035f, 0.016f, 0.03f), 10, 0.016f, 0.01f, White, 1.1f, -1f, 0.5f);
        int head = b.Head(V(0, 0.24f, 0.005f));
        var c = V(0, 0.27f, 0.01f);
        var r = V(0.042f, 0.038f, 0.036f);
        b.Ell(head, c, r, yellow);
        PokeBuilder.Both(s =>
        {
            Antenna(b, head, c + V(0.01f * s, 0.03f, 0.0f), c + V(0.015f * s, 0.06f, 0.0f), c + V(0.02f * s, 0.08f, -0.005f), 0.004f, Rgb(40, 36, 44), Rgb(40, 36, 44), 0.006f);
            var at = On(c, r, 0.018f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(40, 36, 44));
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.018f), V(0, -0.2f, 1f), 0.008f, 0.004f, Rgb(80, 50, 40), MarkShape.Smile);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Rockruff and Lycanroc

    /// <summary>Rockruff: a tan puppy with darker ears and paws, a collar of gray rocks round its neck, a fluffy cream tail, a dark muzzle and blue eyes in dark patches.</summary>
    private static PokeBuilder Rockruff()
    {
        var b = new PokeBuilder("Rockruff", 0.5f, BodyPlan.Quadruped, V(0, 0.11f, 0)) { Coat = Fur };
        var tan = Rgb(200, 160, 130);
        var brown = Rgb(130, 96, 76);
        var cream = Rgb(240, 230, 214);
        var rock = Rgb(150, 146, 140);
        BeastLegs(b, 0.035f, 0.09f, 0.05f, -0.05f, 0.02f, tan, brown, brown);
        var bc = V(0, 0.11f, 0);
        b.Ell(Body, bc, V(0.045f, 0.045f, 0.07f), tan);
        int tail = b.Tail(bc + V(0, 0.02f, -0.06f));
        b.Limb(tail, bc + V(0, 0.02f, -0.06f), bc + V(0, 0.06f, -0.09f), 0.014f, 0.016f, cream);
        b.Ell(tail, bc + V(0, 0.07f, -0.1f), V(0.03f, 0.05f, 0.03f), cream, V(-30f, 0, 0), blend: 0.02f);
        for (int i = 0; i < 7; i++)
        {
            float a = (i / 6f - 0.5f) * 220f * Degree;
            var d = V(MathF.Sin(a), 0.2f, MathF.Cos(a));
            b.Spike(Body, bc + V(0, 0.04f, 0.05f) + d * 0.03f, bc + V(0, 0.04f, 0.05f) + d * 0.065f, 0.014f, rock, 0.6f, Shell);
        }
        int head = b.Head(bc + V(0, 0.05f, 0.05f));
        var c = bc + V(0, 0.09f, 0.07f);
        var r = V(0.05f, 0.045f, 0.045f);
        b.Ell(head, c, r, tan);
        b.Ell(head, c + V(0, -0.015f, 0.035f), V(0.022f, 0.018f, 0.02f), brown, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.03f, -0.01f));
            b.Ell(ear, c + V(0.05f * s, 0.035f, -0.01f), V(0.02f, 0.025f, 0.01f), brown, V(0, 0, -50f * s));
            var at = On(c, r, 0.02f * s, c.Y + 0.008f);
            b.PaintEll(head, at, V(0.018f, 0.016f, 0.014f), brown);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(80, 160, 230), sclera: true);
        });
        return b;
    }

    /// <summary>
    /// Lycanroc in each of its forms: the Midday Form a lean tan wolf with a white mane set with brown rocks and a white
    /// tail; the Midnight Form a red wolf risen on its hind legs, a long white mane falling over its face and red eyes;
    /// the Dusk Form an orange wolf with a white mane set with dark rocks, a white tail and green eyes.
    /// </summary>
    private static PokeBuilder LycanrocBuild(int form)
    {
        var name = form switch { 1 => "Lycanroc-Midnight", 2 => "Lycanroc-Dusk", _ => "Lycanroc" };
        var coat = form switch { 1 => Rgb(170, 50, 50), 2 => Rgb(210, 120, 70), _ => Rgb(214, 190, 160) };
        var white = Rgb(244, 242, 238);
        var rock = form == 0 ? Rgb(150, 110, 80) : Rgb(80, 66, 60);
        var eye = form switch { 1 => Rgb(220, 50, 60), 2 => Rgb(80, 190, 110), _ => Rgb(80, 150, 230) };
        if (form == 1)
        {
            // The Midnight Form: risen on its hind legs, hunched, long arms
            var b = new PokeBuilder(name, 0.9f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.22f, -0.02f));
                var knee = V(0.08f * s, 0.12f, 0.04f);
                b.Limb(leg, V(0.05f * s, 0.23f, -0.02f), knee, 0.035f, 0.025f, coat);
                b.Limb(leg, knee, V(0.08f * s, 0.03f, -0.02f), 0.024f, 0.02f, white);
                b.Ell(leg, V(0.08f * s, 0.02f, 0.01f), V(0.025f, 0.02f, 0.04f), white);
                var shoulder = V(0.07f * s, 0.4f, 0.04f);
                int arm = b.Arm(s, shoulder);
                var hand = shoulder + V(0.06f * s, -0.22f, 0.06f);
                b.Limb(arm, shoulder, hand, 0.024f, 0.02f, coat);
                b.Spike(arm, Vector3.Lerp(shoulder, hand, 0.5f), Vector3.Lerp(shoulder, hand, 0.5f) + V(0.03f * s, 0.03f, -0.03f), 0.012f, rock, 0.6f, Shell);
                Claws(b, arm, hand + V(0, -0.01f, 0.01f), V(0.008f, 0, 0), V(0, -1f, 0.3f), 0.022f, 0.006f);
            });
            var bc = V(0, 0.32f, 0.02f);
            b.Ell(Body, bc, V(0.07f, 0.11f, 0.06f), coat, V(20f, 0, 0));
            b.PaintEll(Body, bc + V(0, 0, 0.05f), V(0.04f, 0.08f, 0.03f), white);
            int tail = b.Tail(bc + V(0, -0.08f, -0.05f));
            b.Tube(tail, Smooth(3, bc + V(0, -0.08f, -0.05f), V(0, 0.16f, -0.14f), V(0, 0.08f, -0.2f)), 0.03f, 0.02f, white, blend: 0f);
            int head = b.Head(bc + V(0, 0.1f, 0.05f));
            var c = bc + V(0, 0.15f, 0.08f);
            var r = V(0.05f, 0.05f, 0.06f);
            b.Ell(head, c, r, coat);
            // A long white mane from the crown down the back, falling over one eye
            b.Ell(head, c + V(0, 0.03f, -0.03f), V(0.065f, 0.05f, 0.06f), white, blend: 0.015f);
            FurTufts(b, head, c + V(0, 0.01f, -0.04f), V(0.07f, 0.06f, 0.06f), 18, 0.06f, 0.02f, white, 0.5f, -0.6f, 1.2f);
            Frond(b, head, c + V(0.01f, 0.04f, 0.02f), c + V(0.04f, -0.04f, 0.06f), 0.03f, white, V(0.3f, 0, 1f), 0.3f);
            PokeBuilder.Both(s =>
            {
                CatEar(b, head, c + V(0.03f * s, 0.04f, -0.02f), c + V(0.05f * s, 0.09f, -0.03f), 0.018f, coat, white);
                var at = On(c, r, 0.02f * s, c.Y + 0.008f);
                b.Eye(head, at, Outward(c, r, at), 0.01f, eye, glare: true);
            });
            return b;
        }
        else
        {
            var b = new PokeBuilder(name, 0.85f, BodyPlan.Quadruped, V(0, 0.24f, -0.02f)) { Coat = Fur };
            BeastLegs(b, 0.045f, 0.2f, 0.1f, -0.1f, 0.028f, coat, Rgb(90, 80, 76), form == 2 ? Rgb(60, 50, 46) : white);
            var bc = V(0, 0.24f, -0.02f);
            b.Ell(Body, bc, V(0.055f, 0.06f, 0.13f), coat);
            int tail = b.Tail(bc + V(0, 0.03f, -0.12f));
            b.Limb(tail, bc + V(0, 0.03f, -0.11f), bc + V(0, 0.08f, -0.17f), 0.016f, 0.02f, white);
            b.Ell(tail, bc + V(0, 0.1f, -0.2f), V(0.035f, 0.07f, 0.035f), white, V(-35f, 0, 0), blend: 0.02f);
            // A white mane round the neck, rocks standing out of it
            var neck = bc + V(0, 0.05f, 0.1f);
            b.Ell(Body, neck, V(0.06f, 0.06f, 0.05f), white, blend: 0.02f);
            FurTufts(b, Body, neck, V(0.06f, 0.06f, 0.05f), 16, 0.04f, 0.018f, white, 1.1f, -0.9f, 0.5f);
            foreach (var (x, y) in new[] { (-0.04f, 0.06f), (0.04f, 0.06f), (-0.05f, 0.0f), (0.05f, 0.0f), (0f, 0.07f) })
                b.Spike(Body, neck + V(x * 0.6f, y * 0.6f, -0.02f), neck + V(x * 1.6f, y * 1.4f + 0.02f, -0.05f), 0.016f, rock, 0.6f, Shell);
            int head = b.Head(neck + V(0, 0.03f, 0.02f));
            var c = neck + V(0, 0.07f, 0.05f);
            var r = V(0.045f, 0.045f, 0.055f);
            b.Ell(head, c, r, coat);
            b.Ell(head, c + V(0, -0.015f, 0.05f), V(0.022f, 0.02f, 0.035f), coat);
            b.Ell(head, c + V(0, -0.008f, 0.085f), V(0.008f, 0.006f, 0.005f), Rgb(50, 46, 50), blend: 0.004f);
            if (form == 2) b.Ell(head, c + V(0, 0.035f, -0.01f), V(0.05f, 0.025f, 0.05f), Rgb(80, 66, 60), blend: 0.012f);
            PokeBuilder.Both(s =>
            {
                CatEar(b, head, c + V(0.025f * s, 0.035f, -0.02f), c + V(0.045f * s, 0.09f, -0.04f), 0.018f, coat, white);
                var at = On(c, r, 0.022f * s, c.Y + 0.01f);
                b.Eye(head, at, Outward(c, r, at), 0.01f, eye, glare: true);
            });
            return b;
        }
    }

    private static PokeBuilder Lycanroc() => LycanrocBuild(0);

    // ------------------------------------------------------------------ Wishiwashi

    /// <summary>
    /// Wishiwashi alone and in its School Form. Alone, a tiny blue fish, white below, with one great teary eye on each
    /// side. Schooled, a great dark blue fish as big as a whale, made of hundreds, its gaping mouth set with white teeth
    /// and its sides lit with the shining eyes of the fish that make it.
    /// </summary>
    private static PokeBuilder WishiwashiBuild(bool school)
    {
        var b = new PokeBuilder(school ? "Wishiwashi-School" : "Wishiwashi", school ? 1f : 0.35f, BodyPlan.Fish, V(0, school ? 0.4f : 0.15f, 0)) { Coat = Scales }.Hover();
        if (!school)
        {
            var blue = Rgb(80, 140, 220);
            var c = V(0, 0.15f, 0);
            var r = V(0.04f, 0.04f, 0.06f);
            b.Ell(Body, c, r, White);
            b.PaintEll(Body, c + V(0, 0.025f, -0.01f), V(0.045f, 0.03f, 0.07f), blue);
            int tail = b.Tail(c + V(0, 0, -0.05f));
            PokeBuilder.Both(s => Frond(b, tail, c + V(0, 0, -0.05f), c + V(0, 0.03f * s, -0.1f), 0.018f, White, V(1f, 0, 0), 0.25f));
            Blade(b, Body, c + V(0, 0.03f, -0.01f), c + V(0, 0.055f, -0.04f), 0.015f, blue, V(1f, 0, 0), 0.3f);
            PokeBuilder.Both(s =>
            {
                var at = c + V(0.036f * s, 0.005f, 0.025f);
                b.Eye(Body, at, V(s, 0, 0.4f), 0.017f, Rgb(70, 140, 220), sclera: true);
                b.Mark(Body, at + V(0.002f * s, -0.022f, 0), V(s, -0.3f, 0.3f), 0.004f, 0.007f, Rgb(140, 200, 250));
            });
            return Lift(b);
        }
        var navy = Rgb(40, 60, 120);
        var deep = Rgb(24, 36, 80);
        var glow = Rgb(120, 220, 240);
        var bc = V(0, 0.4f, -0.04f);
        var br = V(0.17f, 0.17f, 0.3f);
        b.Ell(Body, bc, br, navy);
        b.PaintEll(Body, bc + V(0, -0.1f, 0.04f), V(0.16f, 0.08f, 0.26f), deep);
        // The great mouth gaping, white teeth top and bottom
        var mouth = bc + V(0, -0.02f, br.Z * 0.92f);
        b.Cut(Body, mouth + V(0, 0, 0.03f), V(0.12f, 0.06f, 0.06f));
        b.PaintEll(Body, mouth, V(0.13f, 0.07f, 0.06f), Rgb(20, 20, 40));
        foreach (float x in new[] { -0.08f, -0.04f, 0f, 0.04f, 0.08f })
        {
            b.Spike(Body, mouth + V(x, 0.055f, -0.01f), mouth + V(x, 0.02f, 0.005f), 0.012f, White, mat: Shell, blend: 0.004f);
            b.Spike(Body, mouth + V(x * 0.9f, -0.055f, -0.01f), mouth + V(x * 0.9f, -0.025f, 0.005f), 0.011f, White, mat: Shell, blend: 0.004f);
        }
        // The shining eyes of the fish that make it, all over its sides
        for (int i = 0; i < 22; i++)
        {
            float t = (i + 0.5f) / 22f, a = i * 2.4f;
            var n = V(MathF.Sin(a) * 0.9f, MathF.Cos(a) * 0.6f, 1.6f * t - 0.9f);
            if (n.Z > 0.6f && MathF.Abs(n.Y) < 0.4f) continue;
            b.PaintEll(Body, Out(bc, br, default, n), V(0.012f, 0.012f, 0.012f), glow);
        }
        // Fins and a tail, ragged with little fish breaking away
        int tail2 = b.Tail(bc + V(0, 0, -br.Z * 0.9f));
        PokeBuilder.Both(s => Frond(b, tail2, bc + V(0, 0, -br.Z * 0.9f), bc + V(0, 0.16f * s, -br.Z - 0.18f), 0.07f, navy, V(1f, 0, 0), 0.22f));
        Blade(b, Body, bc + V(0, br.Y * 0.8f, -0.05f), bc + V(0, br.Y + 0.12f, -0.18f), 0.08f, navy, V(1f, 0, 0), 0.22f);
        PokeBuilder.Both(s =>
        {
            Frond(b, Body, bc + V(br.X * 0.8f * s, -0.06f, 0.06f), bc + V((br.X + 0.12f) * s, -0.16f, 0.0f), 0.05f, navy, V(0, 1f, 0), 0.22f);
            foreach (var (y, z) in new[] { (-0.1f, -0.02f), (0.08f, -0.2f) })
            {
                var n = V(s, y / br.Y, z / br.Z);
                var on = Out(bc, br, default, n);
                var fish = on + Outward(bc, br, on) * 0.035f;
                b.Ell(Body, fish, V(0.012f, 0.014f, 0.025f), navy, blend: 0.006f);
                b.Limb(Body, on - Outward(bc, br, on) * 0.01f, fish, 0.006f, 0.006f, navy);
            }
            var at = Out(bc, br, default, V(0.75f * s, 0.35f, 0.9f));
            b.Eye(Body, at, Outward(bc, br, at), 0.026f, Rgb(250, 230, 120), sclera: true, white: Rgb(240, 250, 250), glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Wishiwashi() => WishiwashiBuild(false);

    // ------------------------------------------------------------------ Mareanie and Toxapex

    /// <summary>Mareanie: a little purple starfish of poison, a crown of yellow on its head, long pale blue tentacles hanging round it like hair, each set with pink-tipped purple spines.</summary>
    private static PokeBuilder Mareanie()
    {
        var b = new PokeBuilder("Mareanie", 0.5f, BodyPlan.Biped, V(0, 0.08f, 0)) { Coat = Scales };
        var purple = Rgb(150, 120, 190);
        var teal = Rgb(150, 220, 230);
        var spine = Rgb(130, 90, 170);
        var pink = Rgb(240, 120, 170);
        var c = V(0, 0.12f, 0.01f);
        var r = V(0.045f, 0.045f, 0.04f);
        b.Ell(Body, V(0, 0.05f, 0.01f), V(0.035f, 0.045f, 0.03f), purple);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.02f * s, 0.03f, 0.01f));
            b.Ell(leg, V(0.025f * s, 0.012f, 0.02f), V(0.016f, 0.012f, 0.018f), purple);
        });
        int head = b.Head(c - V(0, 0.03f, 0));
        b.Ell(head, c, r, purple);
        b.Spike(head, c + V(0, 0.04f, 0), c + V(0, 0.07f, -0.01f), 0.016f, Rgb(250, 220, 90), 0.6f, Shell);
        // Tentacles like hair, hanging round the head and spiny
        for (int i = 0; i < 6; i++)
        {
            float a = ((i + 0.5f) / 6f - 0.5f) * 280f * Degree;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a) * 0.6f - 0.4f);
            var top = c + V(0, 0.03f, 0) + d * 0.03f;
            var path = Smooth(3, top, top + d * 0.06f + V(0, 0.01f, 0), top + d * 0.09f + V(0, -0.06f, 0), top + d * 0.09f + V(0, -0.12f, 0));
            b.Tube(head, path, 0.016f, 0.011f, teal, blend: 0f);
            var at = path[path.Length / 2];
            b.Spike(head, at, at + d * 0.03f + V(0, 0.01f, 0), 0.008f, spine, 0.6f, Shell);
            b.PaintEll(head, at + d * 0.028f + V(0, 0.01f, 0), V(0.006f, 0.006f, 0.006f), pink, soft: 0.003f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.018f * s, c.Y - 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(250, 220, 90), glare: true);
        });
        return b;
    }

    /// <summary>Toxapex: a cage of pale blue legs arched into a dome over its little purple body, set all over with purple spines tipped pink, its face peering out between them.</summary>
    private static PokeBuilder Toxapex()
    {
        var b = new PokeBuilder("Toxapex", 0.75f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Scales };
        var purple = Rgb(130, 100, 180);
        var teal = Rgb(140, 210, 226);
        var spine = Rgb(110, 80, 160);
        var pink = Rgb(240, 120, 170);
        var c = V(0, 0.1f, 0.04f);
        var r = V(0.045f, 0.045f, 0.04f);
        b.Ell(Body, c, r, purple);
        b.Ell(Body, V(0, 0.32f, -0.02f), V(0.04f, 0.03f, 0.04f), teal, blend: 0.02f);
        // The dome: legs arching from the crown down to the ground, the front ones parted to show the face
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 10f;
            if (MathF.Cos(a) > 0.85f) continue;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var path = Smooth(3, V(0, 0.31f, -0.02f), V(0, 0.28f, -0.02f) + d * 0.1f, V(0, 0.16f, -0.02f) + d * 0.16f, V(0, 0.015f, -0.02f) + d * 0.17f);
            int leg = b.Part("leg" + i, Body, path[0], PokeRole.Leg, i * 0.3f);
            b.Tube(leg, path, 0.03f, 0.022f, teal, blend: 0f);
            for (int k = 3; k < path.Length - 1; k += 3)
            {
                b.Spike(leg, path[k], path[k] + d * 0.05f + V(0, 0.02f, 0), 0.014f, spine, 0.6f, Shell);
                b.PaintEll(leg, path[k] + d * 0.048f + V(0, 0.02f, 0), V(0.008f, 0.008f, 0.008f), pink, soft: 0.003f);
            }
        }
        b.Limb(Body, c, V(0, 0.3f, -0.02f), 0.02f, 0.02f, purple);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.018f * s, c.Y + 0.004f);
            b.Eye(Body, at, Outward(c, r, at), 0.01f, Rgb(250, 200, 70), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Mudbray and Mudsdale

    /// <summary>
    /// Mudbray and Mudsdale: horses of mud. Mudbray is a little brown donkey with cream patches, long ears, a black mane
    /// and great muddy hooves; Mudsdale a heavy dark horse with a mane of black locks, red mud caked over its shoulders
    /// and haunches and great boots of red mud on its legs.
    /// </summary>
    private static PokeBuilder MudHorseBuild(bool mudsdale)
    {
        float k = mudsdale ? 1.6f : 1f;
        var b = new PokeBuilder(mudsdale ? "Mudsdale" : "Mudbray", mudsdale ? 1f : 0.6f, BodyPlan.Quadruped, V(0, 0.18f * k, 0)) { Coat = Fur };
        var brown = mudsdale ? Rgb(110, 76, 60) : Rgb(150, 110, 84);
        var black = Rgb(50, 46, 50);
        var mud = mudsdale ? Rgb(200, 90, 60) : Rgb(236, 210, 190);
        BeastLegs(b, 0.05f * k, 0.14f * k, 0.08f * k, -0.1f * k, 0.026f * k, brown, mud, brown);
        foreach (var z in new[] { 0.08f * k, -0.1f * k })
            PokeBuilder.Both(s => b.Ell(Root, V(0.052f * k * s, 0.05f * k, z + 0.01f), V(0.035f, 0.04f, 0.04f) * k, mud, blend: 0.012f));
        var bc = V(0, 0.18f, 0) * k;
        var br = V(0.07f, 0.065f, 0.13f) * k;
        b.Ell(Body, bc, br, brown);
        if (mudsdale)
            foreach (var z in new[] { 0.09f, -0.09f })
                PokeBuilder.Both(s => b.PaintEll(Body, Out(bc, br, default, V(s, 0.2f, z / 0.13f)), V(0.05f, 0.05f, 0.05f) * k, mud));
        else
            foreach (var (x, z) in new[] { (0.06f, 0.04f), (-0.06f, -0.06f) })
                b.PaintEll(Body, Out(bc, br, default, V(x / 0.06f, 0.2f, z / 0.13f)), V(0.03f, 0.03f, 0.03f), mud);
        int tail = b.Tail(bc + V(0, 0.02f, -0.12f) * k);
        b.Tube(tail, Smooth(3, bc + V(0, 0.02f, -0.12f) * k, bc + V(0, -0.04f, -0.18f) * k, bc + V(0, -0.1f, -0.2f) * k), 0.016f * k, 0.01f * k, black, blend: 0f);
        var neck = Smooth(3, bc + V(0, 0.03f, 0.1f) * k, bc + V(0, 0.09f, 0.14f) * k, bc + V(0, 0.13f, 0.15f) * k);
        b.Tube(Body, neck, 0.035f * k, 0.03f * k, brown, blend: 0f);
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.02f, 0.04f) * k;
        var r = V(0.035f, 0.035f, 0.055f) * k;
        b.Ell(head, c, r, brown, V(20f, 0, 0));
        b.PaintEll(head, c + V(0, -0.02f, 0.045f) * k, V(0.025f, 0.02f, 0.02f) * k, mudsdale ? black : mud);
        // The mane: black, falling in locks
        int locks = mudsdale ? 7 : 4;
        for (int i = 0; i < locks; i++)
        {
            float t = i / (locks - 1f);
            var at = Vector3.Lerp(c + V(0, 0.03f, -0.03f) * k, neck[0] + V(0, 0.04f, 0) * k, t);
            PokeBuilder.Both(s => b.Tube(head, Smooth(2, at, at + V(0.03f * s, -0.01f, -0.01f) * k, at + V(0.04f * s, -0.06f * (mudsdale ? 1.4f : 0.6f), -0.02f) * k), 0.012f * k, 0.008f * k, black, blend: 0f));
        }
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.02f * s, 0.03f, -0.03f) * k);
            b.Ell(ear, c + V(0.035f * s, 0.06f, -0.04f) * k, V(0.012f, 0.03f, 0.008f) * k, brown, V(0, 0, -20f * s));
            var at = Out(c, r, V(20f, 0, 0), V(0.7f * s, 0.3f, 0.6f));
            b.Eye(head, at, V(0.7f * s, 0.3f, 0.6f), 0.009f * k, mudsdale ? Rgb(200, 50, 40) : Rgb(40, 36, 40), glare: mudsdale);
        });
        return b;
    }

    private static PokeBuilder Mudbray() => MudHorseBuild(false);

    private static PokeBuilder Mudsdale() => MudHorseBuild(true);

    // ------------------------------------------------------------------ Dewpider and Araquanid

    /// <summary>
    /// Dewpider and Araquanid: water spiders that carry a bubble of water over their heads. Dewpider's dark blue head is
    /// inside its bubble, six yellow-green legs below; Araquanid stands on long yellow-green legs tipped gray, its blue
    /// face inside a great bubble and more bubbles held on its legs.
    /// </summary>
    private static PokeBuilder WaterSpiderBuild(bool araquanid)
    {
        float k = araquanid ? 1.8f : 1f;
        var b = new PokeBuilder(araquanid ? "Araquanid" : "Dewpider", araquanid ? 0.85f : 0.45f, BodyPlan.Quadruped, V(0, 0.1f * k, 0)) { Coat = Shell };
        var green = Rgb(200, 220, 90);
        var gray = Rgb(70, 70, 90);
        var blue = Rgb(60, 100, 170);
        var water = Rgb(190, 226, 246);
        var bc = V(0, 0.09f, -0.03f) * k;
        b.Ell(Body, bc, V(0.035f, 0.03f, 0.045f) * k, gray);
        b.Spike(Body, bc + V(0, 0, -0.03f) * k, bc + V(0, -0.02f, -0.08f) * k, 0.025f * k, gray, 0.6f);
        foreach (var (z, front) in new[] { (0.02f, true), (-0.01f, true), (-0.04f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, bc + V(0.02f * s, 0.0f, z) * k, front);
                var knee = bc + V(0.09f * s, araquanid ? 0.09f : 0.05f, z * 2f) * k;
                var foot = V(0.12f * s, 0.0f, z * 2.5f + 0.01f) * k;
                b.Limb(leg, bc + V(0.02f * s, 0, z) * k, knee, 0.008f * k, 0.007f * k, green);
                b.Limb(leg, knee, foot, 0.007f * k, 0.004f * k, araquanid ? gray : green);
                if (araquanid && front && z > 0.01f) b.Ell(leg, knee + V(0, 0.03f, 0.0f) * k, V(0.03f, 0.03f, 0.03f) * k, water, mat: Glow, blend: 0.008f);
            });
        int head = b.Head(bc + V(0, 0.03f, 0.03f) * k);
        var c = bc + V(0, 0.06f, 0.05f) * k;
        var r = V(0.035f, 0.03f, 0.035f) * k;
        b.Ell(head, c, r, blue);
        // The bubble of water round the head
        b.Ell(head, c + V(0, 0.01f, 0), V(0.06f, 0.055f, 0.06f) * k, water, mat: Glow, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var bubble = V(0.06f, 0.055f, 0.06f) * k;
            var at = Out(c + V(0, 0.01f, 0), bubble, default, V(0.4f * s, 0.05f, 1f));
            b.Eye(head, at, Outward(c + V(0, 0.01f, 0), bubble, at), 0.011f * k, Rgb(30, 50, 100));
        });
        return b;
    }

    private static PokeBuilder Dewpider() => WaterSpiderBuild(false);

    private static PokeBuilder Araquanid() => WaterSpiderBuild(true);

    // ------------------------------------------------------------------ Fomantis and Lurantis

    /// <summary>Fomantis: a little pink mantis like a bud, a cap of green leaves over its head, two leaves for arms and red eyes.</summary>
    private static PokeBuilder Fomantis()
    {
        var b = new PokeBuilder("Fomantis", 0.45f, BodyPlan.Biped, V(0, 0.08f, 0)) { Coat = Leaf };
        var pink = Rgb(240, 170, 180);
        var green = Rgb(100, 190, 140);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.02f * s, 0.04f, 0));
            b.Limb(leg, V(0.02f * s, 0.05f, 0), V(0.025f * s, 0.01f, 0.01f), 0.01f, 0.008f, pink);
            int arm = b.Arm(s, V(0.03f * s, 0.11f, 0.01f));
            Frond(b, arm, V(0.03f * s, 0.11f, 0.01f), V(0.08f * s, 0.08f, 0.04f), 0.02f, green, V(0, 1f, 0.2f), 0.25f, Leaf);
        });
        b.Ell(Body, V(0, 0.08f, 0), V(0.03f, 0.05f, 0.028f), pink);
        b.PaintEll(Body, V(0, 0.07f, 0.02f), V(0.012f, 0.04f, 0.015f), Rgb(250, 230, 230));
        int head = b.Head(V(0, 0.12f, 0.01f));
        var c = V(0, 0.15f, 0.015f);
        var r = V(0.04f, 0.035f, 0.035f);
        b.Ell(head, c, r, pink);
        // A cap of leaves over the head, like a bud's
        for (int i = 0; i < 5; i++)
        {
            float a = ((i + 0.5f) / 5f - 0.5f) * 240f * Degree;
            var d = Vector3.Normalize(V(MathF.Sin(a), 0, 0.3f * MathF.Cos(a) - 0.4f));
            Frond(b, head, c + V(0, 0.032f, 0), c + V(0, 0.035f, 0) + d * 0.06f + V(0, 0.008f, 0), 0.03f, green, V(0, 1f, 0), 0.35f, Leaf);
        }
        b.Ell(head, c + V(0, 0.035f, -0.01f), V(0.04f, 0.022f, 0.035f), green, mat: Leaf, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.017f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(200, 50, 70), sclera: true, white: Rgb(250, 200, 210));
        });
        return b;
    }

    /// <summary>Lurantis: a tall elegant mantis like an orchid, pink and white, petals for a robe and a crest, two great pink sickles for arms edged in green, and sharp red eyes.</summary>
    private static PokeBuilder Lurantis()
    {
        var b = new PokeBuilder("Lurantis", 0.9f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Leaf };
        var pink = Rgb(236, 120, 140);
        var pale = Rgb(250, 228, 232);
        var green = Rgb(100, 190, 150);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.18f, 0));
            b.Limb(leg, V(0.03f * s, 0.19f, 0), V(0.04f * s, 0.02f, 0.01f), 0.014f, 0.01f, pale);
            var shoulder = V(0.05f * s, 0.42f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.06f * s, -0.04f, 0.04f);
            b.Limb(arm, shoulder, elbow, 0.014f, 0.012f, pink);
            Sickle(b, arm, elbow, elbow + V(0.04f * s, 0.08f, 0.02f), elbow + V(0.0f, 0.18f, 0.0f), 0.04f, pink, V(s, 0, 0));
            b.PaintEll(arm, elbow + V(0.03f * s, 0.1f, 0.01f), V(0.012f, 0.06f, 0.03f), green, soft: 0.004f);
        });
        var bc = V(0, 0.32f, 0);
        b.Ell(Body, bc, V(0.04f, 0.12f, 0.035f), pale);
        // A robe of long petals, pink striped pale, hanging from the shoulders to the shins
        for (int i = 0; i < 6; i++)
        {
            float a = ((i + 0.5f) / 6f - 0.5f) * 300f * Degree;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            if (MathF.Abs(MathF.Sin(a)) < 0.3f && MathF.Cos(a) > 0) continue;
            Petal(b, Body, bc + V(0, 0.08f, 0) + d * 0.035f, bc + V(0, -0.14f, 0) + d * 0.06f, 0.03f, pink, Leaf);
        }
        int head = b.Head(bc + V(0, 0.12f, 0.01f));
        var c = bc + V(0, 0.16f, 0.02f);
        var r = V(0.035f, 0.032f, 0.035f);
        b.Ell(head, c, r, pale);
        foreach (var (x, len) in new[] { (-0.02f, 0.08f), (0.02f, 0.08f), (0f, 0.1f) })
            Petal(b, head, c + V(x, 0.02f, -0.01f), c + V(x * 3f, 0.02f + len, -0.04f), 0.02f, pink, Leaf);
        PokeBuilder.Both(s => Frond(b, head, c + V(0.02f * s, 0.0f, -0.02f), c + V(0.07f * s, 0.03f, -0.06f), 0.012f, green, V(0, 1f, 0), 0.25f, Leaf));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.015f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(200, 50, 70), sclera: true, white: Rgb(250, 200, 210), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Morelull and Shiinotic

    /// <summary>Morelull: a little white mushroom spirit under caps of pale purple banded with deeper purple, two big dark eyes and roots for feet.</summary>
    private static PokeBuilder Morelull()
    {
        var b = new PokeBuilder("Morelull", 0.4f, BodyPlan.Biped, V(0, 0.08f, 0)) { Coat = Leaf };
        var white = Rgb(246, 244, 236);
        var lilac = Rgb(220, 200, 236);
        var band = Rgb(170, 130, 200);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.012f * s, 0.04f, 0));
            b.Tube(leg, Smooth(2, V(0.012f * s, 0.05f, 0), V(0.02f * s, 0.02f, 0.0f), V(0.035f * s, 0.005f, 0.01f)), 0.006f, 0.004f, Rgb(200, 220, 180), blend: 0f);
        });
        var c = V(0, 0.08f, 0.01f);
        var r = V(0.035f, 0.035f, 0.032f);
        b.Ell(Body, c, r, white);
        // Caps: a tall one in the middle, two smaller ones at its sides
        foreach (var (x, y, k) in new[] { (0f, 0.16f, 1f), (-0.04f, 0.12f, 0.7f), (0.04f, 0.12f, 0.7f) })
        {
            b.Limb(Body, c + V(x * 0.5f, 0.02f, -0.01f), V(x, y - 0.02f, -0.005f), 0.008f, 0.008f, white);
            b.Ell(Body, V(x, y, -0.005f), V(0.03f, 0.04f, 0.03f) * k, lilac, blend: 0.006f);
            b.PaintEll(Body, V(x, y - 0.02f * k, -0.005f), V(0.032f, 0.008f, 0.032f) * k, band);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.014f * s, c.Y);
            b.Eye(Body, at, Outward(c, r, at), 0.01f, Rgb(40, 36, 44));
        });
        return b;
    }

    /// <summary>Shiinotic: a tall mushroom spirit under a great purple cap spotted yellow and frilled pink and green beneath, a pale slender body with long thin arms, a smiling face and a pink foot.</summary>
    private static PokeBuilder Shiinotic()
    {
        var b = new PokeBuilder("Shiinotic", 0.75f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Leaf };
        var white = Rgb(246, 244, 236);
        var purple = Rgb(160, 120, 190);
        var pink = Rgb(246, 196, 214);
        var green = Rgb(214, 236, 170);
        int leg = b.Leg(1f, V(0, 0.05f, 0));
        b.Ell(leg, V(0, 0.03f, 0.005f), V(0.04f, 0.03f, 0.035f), pink);
        b.Limb(Body, V(0, 0.05f, 0), V(0, 0.22f, 0), 0.016f, 0.02f, white);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.015f * s, 0.18f, 0.0f));
            b.Tube(arm, Smooth(3, V(0.015f * s, 0.18f, 0.0f), V(0.05f * s, 0.12f, 0.02f), V(0.06f * s, 0.06f, 0.03f)), 0.005f, 0.004f, white, blend: 0f);
            foreach (var t in new[] { -1f, 1f })
                b.Limb(arm, V(0.06f * s, 0.06f, 0.03f), V((0.06f + t * 0.008f) * s, 0.04f, 0.035f), 0.004f, 0.003f, white);
        });
        var c = V(0, 0.24f, 0.01f);
        var r = V(0.035f, 0.035f, 0.03f);
        b.Ell(Body, c, r, white);
        b.Mark(Body, On(c, r, 0, c.Y - 0.012f), V(0, -0.2f, 1f), 0.012f, 0.006f, Rgb(40, 36, 44), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.013f * s, c.Y + 0.004f);
            b.Eye(Body, at, Outward(c, r, at), 0.009f, Rgb(40, 36, 44));
        });
        // The great cap: purple, spotted yellow, frilled pink and green beneath
        int head = b.Head(V(0, 0.3f, 0));
        var cap = V(0, 0.36f, 0);
        b.Ell(head, cap, V(0.13f, 0.07f, 0.12f), purple, mat: Leaf);
        b.Limb(head, V(0, 0.27f, 0), cap, 0.02f, 0.03f, white);
        b.Torus(head, cap + V(0, -0.045f, 0), 0.1f, 0.016f, pink, sz: 0.92f, blend: 0.01f);
        b.Torus(head, cap + V(0, -0.06f, 0), 0.08f, 0.012f, green, sz: 0.92f, blend: 0.01f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f;
            b.PaintEll(head, cap + V(MathF.Sin(a) * 0.09f, 0.035f, MathF.Cos(a) * 0.08f), V(0.016f, 0.012f, 0.016f), Rgb(240, 230, 120));
        }
        return b;
    }

    // ------------------------------------------------------------------ Salandit and Salazzle

    /// <summary>Salandit: a black salamander creeping low, a red stripe of flame down its back and tail, purple eyes.</summary>
    private static PokeBuilder Salandit()
    {
        var b = new PokeBuilder("Salandit", 0.5f, BodyPlan.Quadruped, V(0, 0.05f, 0)) { Coat = Scales };
        var black = Rgb(60, 60, 70);
        var red = Rgb(226, 80, 50);
        var purple = Rgb(170, 130, 220);
        PokeBuilder.Both(s =>
        {
            foreach (var (z, front) in new[] { (0.06f, true), (-0.06f, false) })
            {
                int leg = b.Leg(s, V(0.03f * s, 0.05f, z), front);
                var foot = V(0.07f * s, 0.008f, z + 0.02f);
                b.Limb(leg, V(0.03f * s, 0.05f, z), foot, 0.012f, 0.009f, black);
                PadDigits(b, leg, foot, V(0.4f * s, 0, 1f), V(0.6f, 0, 0), 0.02f, 0.004f, black);
            }
        });
        var bc = V(0, 0.05f, 0);
        b.Ell(Body, bc, V(0.035f, 0.03f, 0.1f), black);
        b.PaintEll(Body, bc + V(0, 0.03f, 0), V(0.012f, 0.01f, 0.1f), red);
        int tail = b.Tail(bc + V(0, 0, -0.09f));
        var tp = Smooth(3, bc + V(0, 0, -0.09f), V(0, 0.06f, -0.18f), V(0, 0.12f, -0.2f), V(0.02f, 0.16f, -0.16f));
        b.Tube(tail, tp, 0.018f, 0.006f, black, blend: 0f);
        foreach (var p in tp.Where((_, i) => i % 2 == 1))
            b.PaintEll(tail, p + V(0, 0.012f, -0.005f), V(0.008f, 0.01f, 0.012f), red);
        int head = b.Head(bc + V(0, 0.01f, 0.09f));
        var c = bc + V(0, 0.025f, 0.13f);
        var r = V(0.035f, 0.025f, 0.045f);
        b.Ell(head, c, r, black);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, purple, glare: true);
        });
        return b;
    }

    /// <summary>Salazzle: a sleek black salamander standing tall, flames of pink and purple painted down its body, a crested head, a long tail and long clawed fingers, purple eyes.</summary>
    private static PokeBuilder Salazzle()
    {
        var b = new PokeBuilder("Salazzle", 0.85f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Scales };
        var black = Rgb(56, 56, 66);
        var pink = Rgb(236, 90, 160);
        var purple = Rgb(170, 110, 220);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.2f, -0.02f));
            var knee = V(0.08f * s, 0.11f, 0.04f);
            b.Limb(leg, V(0.04f * s, 0.21f, -0.02f), knee, 0.024f, 0.016f, black);
            b.Limb(leg, knee, V(0.07f * s, 0.015f, 0.0f), 0.015f, 0.012f, black);
            PadDigits(b, leg, V(0.07f * s, 0.01f, 0.01f), V(0, 0, 1f), V(0.5f, 0, 0), 0.03f, 0.005f, black);
            var shoulder = V(0.04f * s, 0.4f, 0.02f);
            int arm = b.Arm(s, shoulder);
            var hand = shoulder + V(0.08f * s, -0.06f, 0.06f);
            b.Limb(arm, shoulder, hand, 0.012f, 0.01f, black);
            PadDigits(b, arm, hand, V(0.4f * s, -0.3f, 0.6f), V(0.3f, 0.3f, 0), 0.03f, 0.004f, black);
        });
        var bc = V(0, 0.3f, 0);
        var br = V(0.045f, 0.11f, 0.04f);
        b.Ell(Body, bc, br, black);
        PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0.03f * s, 0.0f, 0.02f), V(0.012f, 0.08f, 0.02f), pink, V(0, 0, 15f * s)));
        b.PaintEll(Body, bc + V(0, 0.02f, 0.035f), V(0.012f, 0.05f, 0.012f), purple);
        int tail = b.Tail(bc + V(0, -0.08f, -0.03f));
        var tp = Smooth(3, bc + V(0, -0.08f, -0.03f), V(0, 0.12f, -0.12f), V(0.06f, 0.05f, -0.24f), V(0.14f, 0.04f, -0.28f));
        b.Tube(tail, tp, 0.022f, 0.006f, black, blend: 0f);
        b.PaintEll(tail, tp[^2], V(0.016f, 0.016f, 0.016f), pink);
        var neck = Smooth(3, bc + V(0, br.Y * 0.8f, 0.0f), bc + V(0, 0.16f, 0.02f), bc + V(0, 0.2f, 0.05f));
        b.Tube(Body, neck, 0.02f, 0.017f, black, blend: 0f);
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.015f, 0.03f);
        var r = V(0.03f, 0.025f, 0.05f);
        b.Ell(head, c, r, black);
        b.Spike(head, c + V(0, 0.02f, -0.02f), c + V(0, 0.05f, -0.08f), 0.018f, black, 0.4f);
        b.PaintEll(head, c + V(0, 0.03f, -0.04f), V(0.01f, 0.015f, 0.03f), pink);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.017f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, purple, glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Stufful and Bewear

    /// <summary>Stufful: a plush bear cub, pink with a white hood rimmed round its face and white ear flaps, dark brown limbs with pink pads and little black eyes.</summary>
    private static PokeBuilder Stufful()
    {
        var b = new PokeBuilder("Stufful", 0.5f, BodyPlan.Quadruped, V(0, 0.09f, 0)) { Coat = Fur };
        var pink = Rgb(240, 160, 176);
        var white = Rgb(248, 246, 244);
        var brown = Rgb(100, 70, 60);
        BeastLegs(b, 0.04f, 0.07f, 0.04f, -0.05f, 0.028f, brown, brown);
        var bc = V(0, 0.09f, 0);
        b.Ell(Body, bc, V(0.06f, 0.055f, 0.07f), pink);
        int head = b.Head(bc + V(0, 0.04f, 0.05f));
        var c = bc + V(0, 0.07f, 0.07f);
        var r = V(0.055f, 0.05f, 0.05f);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0, 0, 0.03f), V(0.042f, 0.04f, 0.03f), Rgb(250, 230, 220));
        b.Ell(head, c + V(0, -0.01f, 0.045f), V(0.016f, 0.012f, 0.012f), Rgb(250, 236, 226), blend: 0.008f);
        b.Ell(head, c + V(0, -0.006f, 0.057f), V(0.006f, 0.005f, 0.004f), Rgb(50, 40, 40), blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.02f, -0.01f));
            b.Ell(ear, c + V(0.07f * s, 0.03f, -0.01f), V(0.03f, 0.022f, 0.012f), white, V(0, 0, -20f * s));
            var at = On(c, r, 0.02f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, Rgb(40, 36, 40));
        });
        return b;
    }

    /// <summary>Bewear: a towering plush bear, black from the shoulders down, pink about its head and chest under a white hood with ear flaps, pink pads on its great paws and little black eyes, one arm raised in welcome.</summary>
    private static PokeBuilder Bewear()
    {
        var b = new PokeBuilder("Bewear", 1f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var pink = Rgb(240, 160, 176);
        var white = Rgb(248, 246, 244);
        var black = Rgb(50, 46, 52);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.2f, 0));
            b.Limb(leg, V(0.08f * s, 0.22f, 0), V(0.09f * s, 0.05f, 0.01f), 0.06f, 0.05f, black);
            b.Ell(leg, V(0.09f * s, 0.03f, 0.03f), V(0.05f, 0.03f, 0.06f), black);
            var shoulder = V(0.14f * s, 0.5f, 0.0f);
            int arm = b.Arm(s, shoulder);
            var paw = s > 0 ? shoulder + V(0.1f, 0.18f, 0.02f) : shoulder + V(-0.06f, -0.2f, 0.06f);
            b.Limb(arm, shoulder, paw, 0.05f, 0.045f, black);
            b.Ell(arm, paw, V(0.05f, 0.05f, 0.045f), black);
            b.PaintEll(arm, paw + V(0, 0, 0.04f), V(0.025f, 0.025f, 0.012f), pink);
        });
        var bc = V(0, 0.36f, 0);
        var br = V(0.14f, 0.18f, 0.11f);
        b.Ell(Body, bc, br, black);
        b.PaintEll(Body, bc + V(0, 0.14f, 0.02f), V(0.15f, 0.08f, 0.12f), pink);
        int head = b.Head(bc + V(0, br.Y, 0.02f));
        var c = bc + V(0, br.Y + 0.07f, 0.04f);
        var r = V(0.08f, 0.07f, 0.07f);
        b.Ell(head, c, r, pink);
        b.Ell(head, c + V(0, 0.02f, -0.02f), V(0.085f, 0.07f, 0.065f), white, blend: 0.012f);
        b.Cut(head, c + V(0, -0.005f, 0.06f), V(0.06f, 0.055f, 0.04f));
        b.Ell(head, c + V(0, -0.005f, 0.035f), V(0.062f, 0.058f, 0.04f), pink, blend: 0.01f);
        b.Ell(head, c + V(0, -0.02f, 0.07f), V(0.022f, 0.016f, 0.016f), Rgb(250, 236, 226), blend: 0.008f);
        b.Ell(head, c + V(0, -0.012f, 0.085f), V(0.008f, 0.006f, 0.005f), Rgb(50, 40, 40), blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.04f, -0.02f));
            b.Ell(ear, c + V(0.1f * s, 0.06f, -0.02f), V(0.04f, 0.03f, 0.015f), white, V(0, 0, -20f * s));
            var at = c + V(0.025f * s, 0.008f, 0.07f);
            b.Eye(head, at, V(0.3f * s, 0.1f, 1f), 0.009f, Rgb(40, 36, 40));
        });
        return b;
    }
}
