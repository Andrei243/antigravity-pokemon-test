using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Hoenn's first batch in National Pokédex
// order: Treecko (252) to Delcatty (301), but for the species of the Sinnoh Pokédex in that range, hand-built before.
// Their forms are in PokemonModels.Megas.cs and PokemonModels.Regional.cs with the other forms. Helpers shared with
// the earlier batches are in PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs, PokemonModels.Kanto1.cs to
// PokemonModels.Kanto3.cs and PokemonModels.Johto1.cs and PokemonModels.Johto2.cs.
internal static partial class PokemonModels
{
    /// <summary>Three fingers or toes from <paramref name="at"/> along <paramref name="dir"/>, each ending in a round pad (a gecko's).</summary>
    private static void PadDigits(PokeBuilder b, int bone, Vector3 at, Vector3 dir, Vector3 spread, float length, float r, Color color)
    {
        var d = Vector3.Normalize(dir);
        for (int i = -1; i <= 1; i++)
        {
            var tip = at + (d + spread * i) * length;
            b.Limb(bone, at, tip, r, r * 0.8f, color, blend: 0.008f);
            b.Ell(bone, tip, V(r * 1.6f, r * 1.6f, r * 1.6f), color, blend: 0.006f);
        }
    }

    // ------------------------------------------------------------------ Treecko line

    /// <summary>Treecko: a little green gecko with a broad head, great yellow eyes, a red throat and a big flat tail curled behind it.</summary>
    private static PokeBuilder Treecko()
    {
        var b = new PokeBuilder("Treecko", 0.55f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Scales };
        var green = Rgb(140, 206, 96);
        var dark = Rgb(72, 146, 82);
        var red = Rgb(226, 112, 118);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.2f, 0));
            b.Limb(leg, V(0.06f * s, 0.21f, 0), V(0.08f * s, 0.05f, 0.01f), 0.036f, 0.028f, green);
            b.Ell(leg, V(0.085f * s, 0.026f, 0.03f), V(0.04f, 0.026f, 0.05f), green);
            PadDigits(b, leg, V(0.085f * s, 0.018f, 0.06f), V(0.15f * s, 0, 1f), V(0.6f, 0, 0), 0.035f, 0.012f, green);
        });
        var bc = V(0, 0.29f, 0);
        var br = V(0.085f, 0.12f, 0.075f);
        b.Ell(Body, bc, br, green);
        b.PaintEll(Body, V(0, 0.29f, 0.06f), V(0.06f, 0.1f, 0.04f), red);
        // Arms held out, three fingers with round pads on each hand
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.36f, 0));
            var hand = V(0.19f * s, 0.43f, 0.03f);
            b.Limb(arm, V(0.07f * s, 0.36f, 0), hand, 0.022f, 0.018f, green);
            PadDigits(b, arm, hand, V(s, 0.4f, 0.2f), V(0, 0.7f, 0), 0.04f, 0.009f, green);
        });
        // A broad flat tail, dark green, curled up behind it
        int tail = b.Tail(V(0, 0.2f, -0.06f));
        b.Limb(tail, V(0, 0.2f, -0.05f), V(0, 0.12f, -0.14f), 0.035f, 0.03f, green);
        b.Ell(tail, V(0, 0.14f, -0.23f), V(0.025f, 0.11f, 0.12f), dark, V(-20f, 0, 0), blend: 0.03f);
        // A broad head: a red throat, a wide mouth and great yellow eyes on its sides
        int head = b.Head(V(0, 0.4f, 0.01f));
        var c = V(0, 0.5f, 0.03f);
        var r = V(0.115f, 0.095f, 0.105f);
        b.Ell(head, c, r, green);
        b.Ell(head, V(0, 0.47f, 0.1f), V(0.08f, 0.055f, 0.06f), green, blend: 0.03f);
        b.PaintEll(head, V(0, 0.43f, 0.08f), V(0.06f, 0.03f, 0.05f), red);
        b.Mark(head, V(0, 0.455f, 0.155f), V(0, -0.2f, 1f), 0.035f, 0.01f, Rgb(60, 90, 50), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var look = V(0.8f * s, 0.3f, 0.5f);
            b.Eye(head, Out(c, r, default, look), look, 0.032f, Rgb(246, 214, 60));
        });
        return b;
    }

    /// <summary>Grovyle: a lean green lizard, a long leaf swept back from its head, leaves along its forearms and a tail of leaves.</summary>
    private static PokeBuilder Grovyle()
    {
        var b = new PokeBuilder("Grovyle", 0.8f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Scales };
        var green = Rgb(124, 196, 102);
        var leaf = Rgb(58, 132, 76);
        var red = Rgb(222, 112, 112);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.32f, -0.02f));
            var knee = V(0.1f * s, 0.2f, 0.06f);
            b.Limb(leg, V(0.07f * s, 0.33f, -0.02f), knee, 0.05f, 0.03f, green);
            b.Limb(leg, knee, V(0.1f * s, 0.04f, -0.01f), 0.028f, 0.022f, green);
            Digits(b, leg, V(0.1f * s, 0.02f, 0.01f), V(0.1f * s, 0, 1f), V(0.6f, 0, 0), 0.05f, 0.014f, green);
        });
        var bc = V(0, 0.42f, 0.02f);
        var br = V(0.075f, 0.12f, 0.07f);
        b.Ell(Body, bc, br, green, V(15f, 0, 0));
        b.PaintEll(Body, V(0, 0.41f, 0.07f), V(0.055f, 0.1f, 0.03f), red);
        b.PaintTorus(Body, V(0, 0.34f, 0.02f), 0.07f, 0.012f, Rgb(108, 168, 86), V(15f, 0, 0), 1f, 0.95f);
        // Arms bent forward, each with leaves growing back from the forearm
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.5f, 0.03f));
            var elbow = V(0.13f * s, 0.42f, 0.0f);
            var hand = V(0.15f * s, 0.4f, 0.12f);
            b.Limb(arm, V(0.07f * s, 0.5f, 0.03f), elbow, 0.022f, 0.018f, green);
            b.Limb(arm, elbow, hand, 0.018f, 0.016f, green);
            Digits(b, arm, hand, V(0.2f * s, -0.4f, 1f), V(0.5f, 0, 0), 0.04f, 0.008f, green);
            for (int i = 0; i < 3; i++)
            {
                var root = Vector3.Lerp(elbow, hand, 0.2f + 0.3f * i);
                Blade(b, arm, root, root + V(0.1f * s, -0.02f - 0.04f * i, -0.18f), 0.03f, leaf, V(s, 0.3f, 0), 0.25f);
            }
        });
        // A tail of long leaves, drooping behind it
        int tail = b.Tail(V(0, 0.34f, -0.06f));
        b.Limb(tail, V(0, 0.34f, -0.05f), V(0, 0.26f, -0.16f), 0.04f, 0.022f, green);
        foreach (var (x, y) in new[] { (0f, -0.04f), (-0.05f, -0.08f), (0.05f, -0.08f), (0f, -0.12f) })
            Blade(b, tail, V(x * 0.3f, 0.26f, -0.16f), V(x, 0.26f + y - 0.05f, -0.4f), 0.035f, leaf, V(0, 1f, 0.2f), 0.22f);
        // A lean head with a long leaf laid back from its brow
        int head = b.Head(V(0, 0.54f, 0.05f));
        var c = V(0, 0.62f, 0.08f);
        var r = V(0.065f, 0.06f, 0.08f);
        b.Ell(head, c, r, green);
        b.Limb(head, V(0, 0.52f, 0.04f), c, 0.035f, 0.035f, green);
        b.Ell(head, V(0, 0.6f, 0.15f), V(0.045f, 0.035f, 0.05f), green, blend: 0.02f);
        b.PaintEll(head, V(0, 0.58f, 0.13f), V(0.04f, 0.02f, 0.05f), red);
        Blade(b, head, c + V(0, 0.03f, 0.03f), c + V(0, 0.08f, -0.32f), 0.045f, leaf, V(0, 1f, 0), 0.4f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.75f * s, 0.25f, 0.6f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f, Rgb(240, 200, 60), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Sceptile and its Mega Evolution: a tall green lizard with yellow seeds down its back, leaf blades on its arms
    /// and a great feathery tail; the Mega's seeds run down its tail, which grows long and red at its tip.
    /// </summary>
    private static PokeBuilder SceptileBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Sceptile-Mega" : "Sceptile", 0.95f, BodyPlan.Biped, V(0, 0.55f, 0)) { Coat = Scales };
        var green = Rgb(112, 186, 98);
        var leaf = Rgb(48, 118, 70);
        var red = Rgb(212, 82, 86);
        var seed = Rgb(236, 196, 72);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.45f, -0.03f));
            var knee = V(0.12f * s, 0.27f, 0.06f);
            b.Limb(leg, V(0.08f * s, 0.46f, -0.03f), knee, 0.06f, 0.035f, green);
            b.Limb(leg, knee, V(0.12f * s, 0.05f, -0.02f), 0.032f, 0.026f, green);
            Digits(b, leg, V(0.12f * s, 0.025f, 0.0f), V(0.1f * s, 0, 1f), V(0.6f, 0, 0), 0.07f, 0.016f, green);
        });
        var bc = V(0, 0.58f, 0.01f);
        var br = V(0.085f, 0.15f, 0.08f);
        b.Ell(Body, bc, br, green, V(8f, 0, 0));
        b.PaintEll(Body, V(0, 0.56f, 0.07f), V(0.06f, 0.11f, 0.03f), Rgb(170, 214, 120));
        b.PaintTorus(Body, V(0, 0.46f, 0.01f), 0.08f, 0.016f, red, V(8f, 0, 0), 1f, 0.95f);
        // Yellow seeds down its back, more and on down the tail for the Mega
        int seeds = mega ? 4 : 3;
        for (int i = 0; i < seeds; i++)
        {
            float y = 0.68f - i * 0.07f;
            b.Ell(Body, V(0, y, -0.075f + i * 0.004f), V(0.03f, 0.03f, 0.028f), seed, mat: Shell, blend: 0.008f);
        }
        // Arms with long leaf blades growing from the forearms
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.67f, 0.02f));
            var elbow = V(0.15f * s, 0.56f, -0.01f);
            var hand = V(0.18f * s, 0.5f, 0.12f);
            b.Limb(arm, V(0.08f * s, 0.67f, 0.02f), elbow, 0.026f, 0.02f, green);
            b.Limb(arm, elbow, hand, 0.02f, 0.018f, green);
            Digits(b, arm, hand, V(0.2f * s, -0.4f, 1f), V(0.5f, 0, 0), 0.05f, 0.009f, green);
            foreach (var (t, len) in new[] { (0.25f, 0.2f), (0.6f, 0.16f) })
            {
                var root = Vector3.Lerp(elbow, hand, t);
                Blade(b, arm, root, root + Vector3.Normalize(V(0.4f * s, 0.15f, -1f)) * len, 0.035f, leaf, V(s, 0.3f, 0), 0.22f);
            }
        });
        // A great tail of leaves fanned like a frond; the Mega's is long, with seeds along it and red at the tip
        int tail = b.Tail(V(0, 0.46f, -0.07f));
        var spine = mega
            ? Smooth(3, V(0, 0.46f, -0.06f), V(0, 0.38f, -0.2f), V(0, 0.32f, -0.42f), V(0, 0.3f, -0.66f))
            : Smooth(3, V(0, 0.46f, -0.06f), V(0, 0.34f, -0.14f), V(0, 0.22f, -0.22f), V(0, 0.16f, -0.3f));
        b.Tube(tail, spine, 0.045f, 0.022f, green, blend: 0f);
        int fronds = mega ? 9 : 7;
        for (int i = 0; i < fronds; i++)
        {
            float t = 0.25f + 0.75f * i / (fronds - 1f);
            var root = spine[(int)(t * (spine.Length - 1))];
            float len = (mega ? 0.16f : 0.2f) * (1f - 0.35f * MathF.Abs(t - 0.6f));
            PokeBuilder.Both(s => Blade(b, tail, root, root + Vector3.Normalize(V(0.9f * s, -0.4f, -0.5f)) * len, 0.035f, leaf, V(0, 1f, 0.3f), 0.2f));
        }
        Blade(b, tail, spine[^1], spine[^1] + Vector3.Normalize(spine[^1] - spine[^2]) * 0.14f, 0.05f, mega ? red : leaf, V(0, 1f, 0), 0.22f);
        if (mega)
        {
            for (int i = 1; i < 4; i++)
            {
                var at = spine[i * (spine.Length - 1) / 4];
                b.Ell(tail, at + V(0, 0.03f, 0), V(0.028f, 0.028f, 0.028f), seed, mat: Shell, blend: 0.008f);
            }
            b.PaintEll(tail, spine[^3], V(0.05f, 0.05f, 0.08f), red);
        }
        // A sharp head with a crest swept back and a red underside to its jaw
        int head = b.Head(V(0, 0.73f, 0.05f));
        var c = V(0, 0.83f, 0.07f);
        var r = V(0.07f, 0.065f, 0.085f);
        b.Limb(head, V(0, 0.72f, 0.04f), c, 0.04f, 0.04f, green);
        b.Ell(head, c, r, green);
        b.Ell(head, V(0, 0.81f, 0.15f), V(0.05f, 0.035f, 0.06f), green, blend: 0.02f);
        b.PaintEll(head, V(0, 0.79f, 0.13f), V(0.045f, 0.02f, 0.06f), red);
        Blade(b, head, c + V(0, 0.04f, 0), c + V(0, mega ? 0.1f : 0.06f, -0.2f), 0.04f, mega ? red : leaf, V(0, 1f, 0), 0.25f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.75f * s, 0.25f, 0.6f);
            b.Eye(head, Out(c, r, default, look), look, 0.018f, Rgb(240, 200, 60), glare: true);
        });
        return b;
    }

    private static PokeBuilder Sceptile() => SceptileBuild(false);

    // ------------------------------------------------------------------ Torchic line

    /// <summary>Torchic: a round orange chick with a crest of three yellow plumes, little yellow wings and a beak.</summary>
    private static PokeBuilder Torchic()
    {
        var b = new PokeBuilder("Torchic", 0.5f, BodyPlan.Bird, V(0, 0.22f, 0)) { Coat = Fur };
        var orange = Rgb(244, 140, 52);
        var yellow = Rgb(250, 206, 76);
        var foot = Rgb(242, 196, 92);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.12f, 0));
            b.Limb(leg, V(0.05f * s, 0.13f, 0), V(0.055f * s, 0.025f, 0.01f), 0.014f, 0.011f, foot);
            Digits(b, leg, V(0.055f * s, 0.016f, 0.015f), V(0, 0, 1f), V(0.7f, 0, 0), 0.04f, 0.011f, foot);
        });
        var bc = V(0, 0.24f, 0);
        var br = V(0.12f, 0.13f, 0.11f);
        b.Ell(Body, bc, br, orange);
        // A ruff of yellow down on its chest, and stubby yellow wings
        FurTufts(b, Body, V(0, 0.24f, 0.02f), V(0.08f, 0.04f, 0.1f), 5, 0.04f, 0.014f, yellow, 0.95f, -0.9f, 0.6f);
        b.PaintEll(Body, V(0, 0.22f, 0.09f), V(0.07f, 0.04f, 0.04f), yellow);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.11f * s, 0.25f, 0));
            b.Ell(wing, V(0.125f * s, 0.23f, 0), V(0.03f, 0.05f, 0.045f), orange, V(0, 0, 20f * s));
            FurTufts(b, wing, V(0.14f * s, 0.2f, 0), V(0.02f, 0.03f, 0.03f), 3, 0.04f, 0.012f, yellow, 0.1f, -0.9f, 0.9f);
        });
        // A head grown into the body: a little beak and a crest of three plumes
        int head = b.Head(V(0, 0.32f, 0.01f));
        var c = V(0, 0.38f, 0.02f);
        var r = V(0.1f, 0.09f, 0.09f);
        b.Ell(head, c, r, orange);
        foreach (var (x, h, lean) in new[] { (0f, 0.13f, -0.02f), (-0.04f, 0.1f, -0.06f), (0.04f, 0.1f, 0.06f) })
            Blade(b, head, c + V(x * 0.5f, 0.07f, -0.01f), c + V(x + lean, 0.07f + h, -0.02f), 0.035f, yellow, V(0, 0, 1f), 0.3f);
        b.Spike(head, On(c, r, 0, 0.355f) - V(0, 0, 0.02f), On(c, r, 0, 0.355f) + V(0, -0.01f, 0.05f), 0.026f, Rgb(250, 210, 80), 0.6f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.05f * s, 0.39f);
            b.Eye(head, at, Outward(c, r, at), 0.018f, Rgb(40, 36, 50));
        });
        return b;
    }

    /// <summary>Combusken: a lanky fighting chick, orange below and yellow above, with a crest of two plumes and grey talons.</summary>
    private static PokeBuilder Combusken()
    {
        var b = new PokeBuilder("Combusken", 0.8f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Fur };
        var orange = Rgb(232, 116, 54);
        var yellow = Rgb(250, 218, 120);
        var talon = Rgb(156, 150, 160);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.3f, 0));
            var knee = V(0.1f * s, 0.18f, 0.05f);
            b.Limb(leg, V(0.07f * s, 0.32f, 0), knee, 0.05f, 0.03f, orange);
            b.Limb(leg, knee, V(0.1f * s, 0.04f, -0.01f), 0.02f, 0.016f, talon);
            Digits(b, leg, V(0.1f * s, 0.018f, 0.0f), V(0.1f * s, 0, 1f), V(0.7f, 0, 0), 0.06f, 0.012f, talon, Shell);
        });
        b.Ell(Body, V(0, 0.34f, -0.01f), V(0.1f, 0.085f, 0.09f), orange);
        var bc = V(0, 0.45f, 0.01f);
        var br = V(0.08f, 0.1f, 0.07f);
        b.Ell(Body, bc, br, yellow, blend: 0.03f);
        FurTufts(b, Body, V(0, 0.36f, 0), V(0.09f, 0.03f, 0.08f), 7, 0.04f, 0.014f, orange, 0.2f, -0.9f, 0.7f);
        // Lanky yellow arms with three grey claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.5f, 0));
            var hand = V(0.17f * s, 0.34f, 0.09f);
            b.Limb(arm, V(0.07f * s, 0.5f, 0), hand, 0.025f, 0.02f, yellow);
            FurTufts(b, arm, Vector3.Lerp(V(0.07f * s, 0.5f, 0), hand, 0.8f), V(0.02f, 0.02f, 0.02f), 3, 0.03f, 0.01f, yellow, 0.1f, -0.9f, 0.8f);
            Digits(b, arm, hand, V(s, -0.3f, 0.4f), V(0, 0.4f, 0.5f), 0.05f, 0.008f, talon, Shell);
        });
        // A head with an orange crest of two plumes and a beak
        int head = b.Head(V(0, 0.55f, 0.02f));
        var c = V(0, 0.62f, 0.03f);
        var r = V(0.075f, 0.07f, 0.075f);
        b.Ell(head, c, r, yellow);
        foreach (var x in new[] { -0.03f, 0.03f })
            Blade(b, head, c + V(x * 0.5f, 0.05f, -0.01f), c + V(x * 2f, 0.17f, -0.05f), 0.035f, orange, V(0, 0, 1f), 0.3f);
        b.Spike(head, On(c, r, 0, 0.6f) - V(0, 0, 0.02f), On(c, r, 0, 0.6f) + V(0, -0.015f, 0.05f), 0.024f, Rgb(244, 196, 72), 0.6f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.635f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(200, 90, 60), glare: true);
        });
        return b;
    }

    /// <summary>
    /// Blaziken and its Mega Evolution: a tall red fighter, a cream mane falling down its back from a red face, cream
    /// chest and arms, yellow fluff at its wrists and shins; the Mega's wrists burn and black runs down its legs.
    /// </summary>
    private static PokeBuilder BlazikenBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Blaziken-Mega" : "Blaziken", 1f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Fur };
        var red = Rgb(212, 66, 52);
        var cream = Rgb(246, 232, 196);
        var yellow = Rgb(250, 206, 92);
        var talon = Rgb(150, 140, 150);
        var black = Rgb(54, 46, 54);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.48f, -0.01f));
            var knee = V(0.11f * s, 0.28f, 0.05f);
            b.Limb(leg, V(0.08f * s, 0.5f, -0.01f), knee, 0.065f, 0.042f, red);
            b.Limb(leg, knee, V(0.11f * s, 0.05f, -0.01f), 0.04f, 0.024f, red);
            if (mega) b.PaintEll(leg, Vector3.Lerp(knee, V(0.11f * s, 0.05f, -0.01f), 0.4f), V(0.05f, 0.1f, 0.05f), black);
            FlameRow(b, leg, V(0.08f * s, 0.12f, 0.02f), V(0.14f * s, 0.12f, -0.02f), V(0, 0.5f, 1f), 4, 0.07f, 0.02f, yellow, Rgb(250, 240, 180));
            Digits(b, leg, V(0.11f * s, 0.02f, 0.0f), V(0.1f * s, 0, 1f), V(0.6f, 0, 0), 0.07f, 0.014f, talon, Shell);
        });
        var bc = V(0, 0.62f, 0.01f);
        var br = V(0.105f, 0.15f, 0.088f);
        b.Ell(Body, bc, br, red);
        b.PaintEll(Body, V(0, 0.69f, 0.06f), V(0.08f, 0.08f, 0.05f), cream);
        foreach (var x in new[] { -0.05f, -0.02f, 0.02f, 0.05f })
            Blade(b, Body, V(x, 0.7f, 0.07f), V(x * 0.5f, 0.6f - MathF.Abs(x), 0.1f), 0.03f, cream, V(0, 0.2f, 1f), 0.3f);
        // Cream arms, the wrists ringed with yellow fluff (burning, for the Mega), grey claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.72f, 0.01f));
            var elbow = V(0.17f * s, 0.6f, -0.01f);
            var hand = V(0.17f * s, 0.6f, 0.15f);
            b.Limb(arm, V(0.09f * s, 0.72f, 0.01f), elbow, 0.04f, 0.032f, cream);
            b.Limb(arm, elbow, hand, 0.034f, 0.03f, cream);
            if (mega)
                FlameRow(b, arm, elbow + V(0, 0.02f, 0.05f), elbow + V(0, -0.02f, 0.05f), V(0.6f * s, 0.6f, -0.6f), 3, 0.22f, 0.03f, Rgb(244, 120, 48), Rgb(252, 210, 90), 0.8f);
            else
                FlameRow(b, arm, elbow + V(0, 0.02f, 0.05f), elbow + V(0, -0.02f, 0.05f), V(0.6f * s, 0.2f, -0.6f), 3, 0.08f, 0.022f, yellow, Rgb(250, 240, 180));
            Digits(b, arm, hand + V(0, 0, 0.02f), V(0, -0.2f, 1f), V(0.5f, 0, 0), 0.04f, 0.012f, talon, Shell);
        });
        // A red face with a sharp beak and two horns, a long cream mane down its back
        int head = b.Head(V(0, 0.76f, 0.03f));
        var c = V(0, 0.84f, 0.04f);
        var r = V(0.08f, 0.082f, 0.082f);
        b.Ell(head, c, r, red);
        b.Spike(head, On(c, r, 0, 0.82f) - V(0, 0, 0.02f), On(c, r, 0, 0.82f) + V(0, -0.02f, 0.06f), 0.026f, Rgb(244, 196, 72), 0.6f, Shell);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.03f * s, 0.05f, 0.02f), c + V(0.07f * s, 0.13f, -0.03f), 0.02f, red, blend: 0.008f));
        var mane = Smooth(3, c + V(0, 0.06f, -0.03f), c + V(0, 0.04f, -0.12f), c + V(0, -0.1f, -0.16f), c + V(0, mega ? -0.36f : -0.3f, -0.14f));
        b.Tube(head, mane, 0.065f, 0.035f, cream, blend: 0.015f);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, c + V(0.04f * s, 0.03f, -0.04f), c + V(0.08f * s, -0.04f, -0.1f), c + V(0.08f * s, -0.2f, -0.12f)), 0.04f, 0.022f, cream, blend: 0.015f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, 0.85f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(230, 190, 60), glare: true);
        });
        return b;
    }

    private static PokeBuilder Blaziken() => BlazikenBuild(false);

    // ------------------------------------------------------------------ Mudkip line

    /// <summary>Mudkip: a little blue mudfish on four stubby legs, a fin on its head, orange gills at its cheeks and a broad pale tail fin.</summary>
    private static PokeBuilder Mudkip()
    {
        var b = new PokeBuilder("Mudkip", 0.5f, BodyPlan.Quadruped, V(0, 0.16f, 0)) { Coat = Scales };
        var blue = Rgb(96, 172, 230);
        var pale = Rgb(214, 236, 248);
        var orange = Rgb(246, 148, 58);
        StubbyLegs(b, 0.07f, 0.08f, 0.06f, -0.07f, 0.035f, blue);
        var bc = V(0, 0.125f, -0.01f);
        var br = V(0.1f, 0.085f, 0.12f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, V(0, 0.075f, 0.03f), V(0.08f, 0.04f, 0.1f), pale);
        // A broad pale fin for a tail, held up behind it
        int tail = b.Tail(V(0, 0.13f, -0.11f));
        b.Limb(tail, V(0, 0.13f, -0.1f), V(0, 0.17f, -0.16f), 0.03f, 0.02f, blue);
        b.Ell(tail, V(0, 0.21f, -0.24f), V(0.012f, 0.08f, 0.1f), pale, V(-35f, 0, 0), blend: 0.02f);
        // A big head with a fin on top, a pale chin and spiky orange gills at its cheeks
        int head = b.Head(V(0, 0.17f, 0.08f));
        var c = V(0, 0.21f, 0.11f);
        var r = V(0.1f, 0.09f, 0.085f);
        b.Ell(head, c, r, blue);
        b.PaintEll(head, V(0, 0.15f, 0.15f), V(0.08f, 0.04f, 0.05f), pale);
        Blade(b, head, c + V(0, 0.06f, -0.01f), c + V(0, 0.2f, -0.04f), 0.05f, blue, V(1f, 0, 0), 0.25f);
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.09f * s, -0.03f, 0.0f);
            foreach (var (dy, dz) in new[] { (0.04f, 0.01f), (0f, 0.03f), (-0.035f, 0.0f) })
                b.Spike(head, root, root + V(0.06f * s, dy, dz), 0.022f, orange, 0.6f, blend: 0.01f);
        });
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.22f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(36, 36, 48));
        });
        b.Mark(head, On(c, r, 0, 0.17f), V(0, -0.2f, 1f), 0.02f, 0.008f, Rgb(40, 60, 90), MarkShape.Smile);
        return b;
    }

    /// <summary>Marshtomp: an upright light-blue mudfish with an orange belly, a dark fin on its head, orange cheek fins and a forked tail fin.</summary>
    private static PokeBuilder Marshtomp()
    {
        var b = new PokeBuilder("Marshtomp", 0.7f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Scales };
        var blue = Rgb(124, 198, 222);
        var orange = Rgb(246, 156, 102);
        var dark = Rgb(62, 82, 112);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.18f, 0));
            b.Limb(leg, V(0.07f * s, 0.2f, 0), V(0.09f * s, 0.05f, 0.01f), 0.045f, 0.035f, blue);
            b.Ell(leg, V(0.095f * s, 0.026f, 0.03f), V(0.045f, 0.026f, 0.055f), blue);
        });
        var bc = V(0, 0.3f, 0);
        var br = V(0.12f, 0.13f, 0.1f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, V(0, 0.28f, 0.08f), V(0.075f, 0.08f, 0.04f), orange);
        // Arms raised wide
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.38f, 0));
            var hand = V(0.22f * s, 0.46f, 0.02f);
            b.Limb(arm, V(0.1f * s, 0.38f, 0), hand, 0.032f, 0.028f, blue);
            b.Ell(arm, hand, V(0.035f, 0.035f, 0.03f), blue);
        });
        // A tail of two dark fins
        int tail = b.Tail(V(0, 0.22f, -0.08f));
        b.Limb(tail, V(0, 0.22f, -0.06f), V(0, 0.2f, -0.12f), 0.03f, 0.025f, blue);
        PokeBuilder.Both(s => b.Ell(tail, V(0.04f * s, 0.18f, -0.15f), V(0.016f, 0.07f, 0.06f), dark, V(-30f, 30f * s, 0), blend: 0.03f));
        // The head is the top of the body: a wide grin, a dark fin and orange spiky fins at its cheeks
        int head = b.Head(V(0, 0.4f, 0.01f));
        var c = V(0, 0.46f, 0.02f);
        var r = V(0.1f, 0.08f, 0.09f);
        b.Ell(head, c, r, blue);
        Blade(b, head, c + V(0, 0.05f, -0.02f), c + V(0, 0.2f, -0.06f), 0.05f, dark, V(1f, 0, 0), 0.25f);
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.09f * s, -0.01f, 0.0f);
            b.Spike(head, root, root + V(0.09f * s, 0.01f, 0.02f), 0.028f, orange, 0.5f, blend: 0.012f);
        });
        Grin(b, head, On(c, r, 0, 0.43f), V(0.05f, 0.025f, 0.02f), Rgb(150, 70, 90));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, 0.49f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(230, 120, 60));
        });
        return b;
    }

    /// <summary>
    /// Swampert and its Mega Evolution: a big blue mudfish on all fours, two dark fins on its head, orange spiky
    /// cheeks and orange pads on its limbs; the Mega's arms swell huge, spotted orange, with dark fins along its back.
    /// </summary>
    private static PokeBuilder SwampertBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Swampert-Mega" : "Swampert", 0.95f, BodyPlan.Quadruped, V(0, 0.34f, 0)) { Coat = Scales };
        var blue = Rgb(90, 160, 220);
        var pale = Rgb(184, 220, 240);
        var orange = Rgb(244, 140, 60);
        var dark = Rgb(48, 62, 92);
        float arm = mega ? 0.1f : 0.065f;
        foreach (var (z, front) in new[] { (0.12f, true), (-0.12f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.14f * s, 0.32f, z), front);
                var foot = V(0.2f * s, 0.05f, z + 0.03f);
                float thick = front ? arm : 0.07f;
                b.Limb(leg, V(0.14f * s, 0.33f, z), foot, thick, thick * 0.75f, blue);
                b.Ell(leg, foot + V(0, -0.015f, 0.02f), V(thick * 0.9f, 0.035f, thick * 1.1f), dark);
                b.Ell(leg, Vector3.Lerp(V(0.14f * s, 0.33f, z), foot, 0.45f) + V(0.05f * s, 0, 0), V(0.035f, 0.05f, 0.04f), orange, mat: Shell, blend: 0.01f);
                if (mega && front)
                    b.Ell(leg, Vector3.Lerp(V(0.14f * s, 0.33f, z), foot, 0.75f) + V(0.06f * s, 0, 0.04f), V(0.04f, 0.04f, 0.045f), orange, mat: Shell, blend: 0.01f);
            });
        var bc = V(0, 0.34f, 0);
        var br = V(0.18f, 0.16f, 0.21f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, V(0, 0.27f, 0.11f), V(0.13f, 0.1f, 0.1f), pale);
        if (mega)
            foreach (var (x, y, z) in new[] { (0.12f, 0.42f, -0.06f), (-0.13f, 0.4f, -0.1f), (0.08f, 0.46f, -0.14f) })
            {
                var n = V(x, y - bc.Y, z) / br;
                b.Mark(Body, Out(bc, br, default, n), n, 0.04f, 0.035f, orange);
            }
        // Dark fins: a tail fin, and for the Mega a row along its back
        int tail = b.Tail(V(0, 0.3f, -0.2f));
        b.Ell(tail, V(0, 0.36f, -0.3f), V(0.015f, 0.12f, 0.09f), dark, V(-35f, 0, 0), blend: 0.025f);
        if (mega)
            for (int i = 0; i < 3; i++)
                Blade(b, Body, V(0, 0.45f, 0.05f - i * 0.1f), V(0, 0.64f - i * 0.04f, -0.05f - i * 0.1f), 0.07f, dark, V(1f, 0, 0), 0.2f);
        // A broad head with two dark fins, orange spiky cheeks and a wide mouth
        int head = b.Head(V(0, 0.4f, 0.17f));
        var c = V(0, 0.44f, 0.24f);
        var r = V(0.12f, 0.09f, 0.1f);
        b.Ell(head, c, r, blue);
        b.PaintEll(head, V(0, 0.39f, 0.29f), V(0.1f, 0.04f, 0.06f), pale);
        PokeBuilder.Both(s =>
        {
            Blade(b, head, c + V(0.05f * s, 0.06f, -0.02f), c + V(0.1f * s, 0.24f, -0.1f), 0.06f, dark, V(1f, 0, 0.3f * s), 0.22f);
            var root = c + V(0.11f * s, -0.02f, 0.01f);
            foreach (var (dy, dz) in new[] { (0.04f, 0.0f), (0f, 0.03f), (-0.04f, 0.0f) })
                b.Spike(head, root, root + V((mega ? 0.12f : 0.08f) * s, dy, dz), 0.026f, mega ? Rgb(224, 84, 60) : orange, 0.5f, blend: 0.01f);
        });
        b.Mark(head, On(c, r, 0, 0.415f), V(0, -0.3f, 1f), 0.06f, 0.012f, Rgb(40, 50, 80), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.055f * s, 0.47f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(240, 140, 50), glare: mega);
        });
        return b;
    }

    private static PokeBuilder Swampert() => SwampertBuild(false);

    // ------------------------------------------------------------------ Poochyena line

    /// <summary>Poochyena: a grey hyena pup, its face, legs and shaggy tail dark, a red nose, yellow eyes and a fang.</summary>
    private static PokeBuilder Poochyena()
    {
        var b = new PokeBuilder("Poochyena", 0.55f, BodyPlan.Quadruped, V(0, 0.22f, 0)) { Coat = Fur };
        var gray = Rgb(176, 178, 186);
        var dark = Rgb(72, 72, 82);
        foreach (var (z, front) in new[] { (0.08f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.14f, z), front);
                b.Limb(leg, V(0.06f * s, 0.16f, z), V(0.065f * s, 0.035f, z + 0.01f), 0.032f, 0.024f, dark);
                b.Ell(leg, V(0.065f * s, 0.022f, z + 0.025f), V(0.026f, 0.022f, 0.035f), dark);
            });
        var bc = V(0, 0.19f, -0.01f);
        var br = V(0.085f, 0.08f, 0.13f);
        b.Ell(Body, bc, br, gray);
        FurTufts(b, Body, V(0, 0.23f, -0.02f), V(0.08f, 0.06f, 0.12f), 11, 0.08f, 0.022f, gray, -0.2f, 0.1f, 0.4f);
        b.PaintEll(Body, V(0, 0.22f, 0.09f), V(0.08f, 0.07f, 0.05f), dark);
        int tail = b.Tail(V(0, 0.22f, -0.12f));
        b.Limb(tail, V(0, 0.22f, -0.12f), V(0, 0.25f, -0.17f), 0.025f, 0.03f, gray);
        b.Ell(tail, V(0, 0.26f, -0.2f), V(0.04f, 0.04f, 0.07f), gray, V(-30f, 0, 0), blend: 0.02f);
        FurTufts(b, tail, V(0, 0.27f, -0.22f), V(0.035f, 0.035f, 0.06f), 5, 0.06f, 0.018f, gray, -0.5f, -0.9f, 0.3f);
        // A dark face, pointed ears, a red nose and a fang over its lip
        int head = b.Head(V(0, 0.26f, 0.1f));
        var c = V(0, 0.29f, 0.14f);
        var r = V(0.075f, 0.068f, 0.075f);
        b.Ell(head, c, r, dark);
        b.Ell(head, V(0, 0.27f, 0.21f), V(0.045f, 0.035f, 0.05f), dark, blend: 0.02f);
        b.Ell(head, V(0, 0.28f, 0.26f), V(0.017f, 0.014f, 0.012f), Rgb(214, 60, 70), mat: Shell, blend: 0.006f);
        b.Spike(head, V(0.025f, 0.25f, 0.24f), V(0.027f, 0.225f, 0.245f), 0.008f, White, mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.34f, 0.12f));
            CatEar(b, ear, V(0.045f * s, 0.34f, 0.12f), V(0.08f * s, 0.42f, 0.1f), 0.022f, dark, gray);
            var at = On(c, r, 0.035f * s, 0.31f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(246, 196, 60), glare: true);
        });
        return b;
    }

    /// <summary>Mightyena: a black wolf with a grey belly and face, a shaggy mane, red eyes and a big bushy tail.</summary>
    private static PokeBuilder Mightyena()
    {
        var b = new PokeBuilder("Mightyena", 0.85f, BodyPlan.Quadruped, V(0, 0.34f, 0)) { Coat = Fur };
        var black = Rgb(56, 56, 66);
        var gray = Rgb(176, 178, 188);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.065f * s, 0.27f, z), front);
                var knee = V(0.07f * s, 0.14f, z + (front ? 0.01f : -0.03f));
                b.Limb(leg, V(0.065f * s, 0.29f, z), knee, 0.05f, 0.03f, front ? gray : black);
                b.Limb(leg, knee, V(0.07f * s, 0.04f, z + 0.01f), 0.028f, 0.022f, black);
                b.Ell(leg, V(0.07f * s, 0.022f, z + 0.025f), V(0.026f, 0.022f, 0.038f), black);
            });
        var bc = V(0, 0.31f, -0.01f);
        var br = V(0.1f, 0.1f, 0.18f);
        b.Ell(Body, bc, br, black);
        b.PaintEll(Body, V(0, 0.26f, 0.05f), V(0.08f, 0.06f, 0.15f), gray);
        FurTufts(b, Body, V(0, 0.37f, 0.1f), V(0.08f, 0.06f, 0.06f), 9, 0.08f, 0.022f, gray, -0.2f, 0.2f, 0.3f);
        FurTufts(b, Body, V(0, 0.35f, -0.06f), V(0.09f, 0.06f, 0.11f), 9, 0.07f, 0.02f, black, -0.3f, 0.2f, 0.3f);
        // A big bushy black tail held up
        int tail = b.Tail(V(0, 0.35f, -0.18f));
        var tp = Smooth(3, V(0, 0.35f, -0.17f), V(0, 0.43f, -0.27f), V(0, 0.52f, -0.32f));
        b.Tube(tail, tp, 0.04f, 0.06f, black, blend: 0.01f);
        FurTufts(b, tail, tp[^1], V(0.04f, 0.05f, 0.04f), 5, 0.05f, 0.018f, black, -0.4f, -0.9f, 0.1f);
        // A long head, grey round the eyes, pointed ears, red eyes and a black nose
        int head = b.Head(V(0, 0.4f, 0.14f));
        var c = V(0, 0.45f, 0.21f);
        var r = V(0.07f, 0.065f, 0.075f);
        b.Limb(head, V(0, 0.35f, 0.12f), c, 0.065f, 0.055f, black);
        FurTufts(b, head, V(0, 0.38f, 0.15f), V(0.07f, 0.05f, 0.05f), 9, 0.08f, 0.02f, gray, 0.5f, -0.3f, 0.4f);
        b.Ell(head, c, r, black);
        b.Ell(head, V(0, 0.42f, 0.29f), V(0.04f, 0.035f, 0.06f), gray, blend: 0.02f);
        b.Ell(head, V(0, 0.43f, 0.345f), V(0.016f, 0.012f, 0.01f), Black, mat: Shell, blend: 0.005f);
        b.PaintEll(head, c + V(0, 0.01f, 0.05f), V(0.06f, 0.025f, 0.04f), gray);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.045f * s, 0.5f, 0.18f));
            CatEar(b, ear, V(0.04f * s, 0.5f, 0.18f), V(0.07f * s, 0.6f, 0.14f), 0.024f, gray, black);
            var at = On(c, r, 0.035f * s, 0.465f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(220, 50, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Zigzagoon line

    /// <summary>
    /// Zigzagoon and its Galarian form: a low raccoon, its fur zigzagged in bands of brown and cream down its back
    /// and tail, a dark mask; the Galarian's black and white, its tongue out.
    /// </summary>
    private static PokeBuilder ZigzagoonBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Zigzagoon-Galar" : "Zigzagoon", 0.55f, BodyPlan.Quadruped, V(0, 0.16f, 0)) { Coat = Fur };
        var light = galar ? Rgb(236, 236, 240) : Rgb(236, 224, 196);
        var dark = galar ? Rgb(52, 52, 60) : Rgb(150, 116, 88);
        var mask = galar ? Rgb(40, 40, 48) : Rgb(70, 54, 50);
        StubbyLegs(b, 0.06f, 0.09f, 0.09f, -0.09f, 0.032f, dark);
        var bc = V(0, 0.14f, -0.01f);
        var br = V(0.1f, 0.085f, 0.17f);
        b.Ell(Body, bc, br, light);
        // Bands of fur across its back, each with a spiky edge of tufts
        for (int i = 0; i < 4; i++)
        {
            float z = 0.1f - i * 0.075f;
            float k = MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow((z - bc.Z) / br.Z, 2f)));
            b.PaintTorus(Body, V(0, 0.17f, z), br.X * k, 0.02f, dark, V(90f, 0, 0), 1f, br.Y / br.X);
            FurTufts(b, Body, V(0, 0.2f, z), V(0.09f * k, 0.06f, 0.02f), 4, 0.035f, 0.013f, i % 2 == 0 ? dark : light, -0.3f, 0.3f, 0.5f);
        }
        // A bushy tail, banded
        int tail = b.Tail(V(0, 0.17f, -0.17f));
        var tc = V(0, 0.2f, -0.26f);
        b.Limb(tail, V(0, 0.16f, -0.16f), tc, 0.035f, 0.045f, light);
        b.Ell(tail, tc, V(0.06f, 0.06f, 0.1f), light, V(-15f, 0, 0), blend: 0.02f);
        b.PaintTorus(tail, tc + V(0, 0, 0.02f), 0.055f, 0.016f, dark, V(75f, 0, 0));
        FurTufts(b, tail, tc, V(0.05f, 0.05f, 0.08f), 6, 0.04f, 0.014f, light, -0.4f, -0.5f, 0.3f);
        // A small head with a dark mask across its eyes and a dark nose; the Galarian's tongue lolls out
        int head = b.Head(V(0, 0.18f, 0.13f));
        var c = V(0, 0.19f, 0.17f);
        var r = V(0.075f, 0.065f, 0.065f);
        b.Ell(head, c, r, light);
        b.Ell(head, V(0, 0.17f, 0.23f), V(0.035f, 0.03f, 0.035f), light, blend: 0.02f);
        b.Ell(head, V(0, 0.18f, 0.262f), V(0.015f, 0.012f, 0.01f), mask, mat: Shell, blend: 0.005f);
        b.PaintEll(head, c + V(0, 0.012f, 0.04f), V(0.07f, 0.025f, 0.04f), mask);
        if (galar)
        {
            b.Spike(head, V(0, 0.155f, 0.235f), V(0, 0.11f, 0.27f), 0.018f, Rgb(232, 120, 160), 0.4f, Shell, 0.006f);
            FurTufts(b, head, c + V(0, 0.04f, -0.01f), V(0.06f, 0.03f, 0.04f), 5, 0.04f, 0.012f, light, -0.6f, 0.4f, 0.1f);
        }
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.04f * s, 0.24f, 0.15f));
            CatEar(b, ear, V(0.04f * s, 0.24f, 0.15f), V(0.06f * s, 0.29f, 0.14f), 0.018f, dark, light);
            var at = On(c, r, 0.035f * s, 0.2f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, galar ? Rgb(220, 60, 90) : Rgb(40, 36, 50), glare: galar);
        });
        return b;
    }

    private static PokeBuilder Zigzagoon() => ZigzagoonBuild(false);

    /// <summary>
    /// Linoone and its Galarian form: a long, low weasel striped straight down its back in brown and cream, a long
    /// snout and short legs with long claws; the Galarian's black and white, a long tongue out.
    /// </summary>
    private static PokeBuilder LinooneBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Linoone-Galar" : "Linoone", 0.75f, BodyPlan.Quadruped, V(0, 0.16f, 0)) { Coat = Fur };
        var light = galar ? Rgb(236, 236, 240) : Rgb(238, 228, 206);
        var dark = galar ? Rgb(50, 50, 58) : Rgb(150, 116, 88);
        foreach (var (z, front) in new[] { (0.15f, true), (-0.17f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.1f, z), front);
                b.Limb(leg, V(0.06f * s, 0.11f, z), V(0.07f * s, 0.03f, z + 0.02f), 0.032f, 0.026f, galar ? dark : light);
                Digits(b, leg, V(0.07f * s, 0.016f, z + 0.03f), V(0, -0.1f, 1f), V(0.5f, 0, 0), 0.04f, 0.007f, Rgb(200, 200, 210), Shell);
            });
        var bc = V(0, 0.125f, 0);
        var br = V(0.075f, 0.07f, 0.24f);
        b.Ell(Body, bc, br, light);
        // Straight stripes from its nose down its back
        b.PaintEll(Body, V(0, 0.175f, 0), V(0.035f, 0.03f, 0.26f), dark);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.065f * s, 0.125f, -0.02f), V(0.02f, 0.025f, 0.2f), dark));
        // A long tail streaming out behind it
        int tail = b.Tail(V(0, 0.145f, -0.23f));
        var tp = Smooth(3, V(0, 0.145f, -0.22f), V(0, 0.17f, -0.32f), V(0, 0.21f, -0.42f));
        b.Tube(tail, tp, 0.045f, 0.03f, light, blend: 0.01f);
        b.PaintEll(tail, V(0, 0.19f, -0.32f), V(0.025f, 0.03f, 0.12f), dark);
        FurTufts(b, tail, tp[^1], V(0.03f, 0.03f, 0.04f), 4, 0.05f, 0.014f, light, -0.5f, -0.9f, 0.2f);
        // A long snout, small ears and the stripes over its brow; the Galarian's tongue hangs out
        int head = b.Head(V(0, 0.145f, 0.22f));
        var c = V(0, 0.155f, 0.27f);
        var r = V(0.06f, 0.055f, 0.07f);
        b.Ell(head, c, r, light);
        b.Ell(head, V(0, 0.135f, 0.34f), V(0.03f, 0.028f, 0.05f), light, blend: 0.02f);
        b.Ell(head, V(0, 0.14f, 0.388f), V(0.013f, 0.011f, 0.009f), Black, mat: Shell, blend: 0.005f);
        b.PaintEll(head, c + V(0, 0.04f, 0.01f), V(0.025f, 0.02f, 0.08f), dark);
        if (galar)
        {
            b.PaintEll(head, c + V(0, 0.01f, 0.03f), V(0.065f, 0.02f, 0.04f), dark);
            b.Spike(head, V(0, 0.115f, 0.35f), V(0, 0.06f, 0.4f), 0.016f, Rgb(232, 120, 160), 0.4f, Shell, 0.006f);
        }
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.04f * s, 0.195f, 0.25f));
            CatEar(b, ear, V(0.04f * s, 0.195f, 0.25f), V(0.055f * s, 0.235f, 0.23f), 0.016f, dark, light);
            var look = V(0.8f * s, 0.3f, 0.5f);
            b.Eye(head, Out(c, r, default, look), look, 0.013f, galar ? Rgb(220, 60, 90) : Rgb(80, 150, 210), glare: galar);
        });
        return b;
    }

    private static PokeBuilder Linoone() => LinooneBuild(false);

    // ------------------------------------------------------------------ Lotad line

    /// <summary>The green rim and dark wedges of a lily pad laid over a Lotad's line, flat or turned up at the brim.</summary>
    private static void LilyPad(PokeBuilder b, int bone, Vector3 c, float r, float thick, Color pad, Color wedge, float brim, int wedges)
    {
        b.Ell(bone, c, V(r, thick, r * 0.92f), pad, mat: Leaf, blend: 0.015f);
        b.Torus(bone, c + V(0, brim * 0.5f, 0), r * 0.95f, thick * 0.6f + brim * 0.3f, pad, sz: 0.92f, mat: Leaf, blend: 0.01f);
        for (int i = 0; i < wedges; i++)
        {
            float a = MathF.Tau * (i + 0.5f) / wedges;
            var at = c + V(MathF.Sin(a) * r * 0.55f, thick * 0.98f, MathF.Cos(a) * r * 0.5f);
            b.Mark(bone, at, V(0, 1f, 0), r * 0.17f, r * 0.17f, wedge, MarkShape.Triangle, a / Degree + 180f);
        }
    }

    /// <summary>Lotad: a little blue water-weed with a duck's yellow bill, under a broad lily pad marked with dark wedges.</summary>
    private static PokeBuilder Lotad()
    {
        var b = new PokeBuilder("Lotad", 0.5f, BodyPlan.Quadruped, V(0, 0.1f, 0)) { Coat = Fur };
        var blue = Rgb(92, 150, 214);
        var pad = Rgb(110, 184, 102);
        var wedge = Rgb(64, 132, 72);
        var bill = Rgb(246, 214, 112);
        StubbyLegs(b, 0.07f, 0.06f, 0.05f, -0.06f, 0.028f, blue);
        var bc = V(0, 0.09f, 0);
        var br = V(0.1f, 0.07f, 0.1f);
        b.Ell(Body, bc, br, blue);
        // A wide yellow bill
        int head = b.Head(V(0, 0.1f, 0.08f));
        b.Ell(head, V(0, 0.065f, 0.11f), V(0.065f, 0.02f, 0.05f), bill, mat: Shell, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.5f, 1f);
            b.Eye(Body, Out(bc, br, default, look), look, 0.017f, Rgb(36, 36, 48));
        });
        // The lily pad, flat over its back
        int leaf = b.Part("pad", Body, V(0, 0.16f, 0), PokeRole.Leaf);
        b.Limb(leaf, V(0, 0.12f, 0), V(0, 0.17f, -0.01f), 0.04f, 0.03f, blue);
        LilyPad(b, leaf, V(0, 0.18f, -0.01f), 0.2f, 0.02f, pad, wedge, 0.02f, 5);
        return b;
    }

    /// <summary>Lombre: a green imp with a broad lily pad for a hat, a red bill, pale limbs and long red-tipped claws.</summary>
    private static PokeBuilder Lombre()
    {
        var b = new PokeBuilder("Lombre", 0.72f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var green = Rgb(116, 182, 90);
        var pale = Rgb(160, 214, 196);
        var red = Rgb(228, 92, 92);
        var pad = Rgb(112, 186, 96);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.2f, 0));
            b.Limb(leg, V(0.06f * s, 0.21f, 0), V(0.08f * s, 0.04f, 0.02f), 0.026f, 0.02f, pale);
            Digits(b, leg, V(0.08f * s, 0.02f, 0.03f), V(0.2f * s, 0, 1f), V(0.6f, 0, 0), 0.045f, 0.01f, red, Shell);
        });
        var bc = V(0, 0.3f, 0);
        var br = V(0.08f, 0.11f, 0.075f);
        b.Ell(Body, bc, br, green);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.36f, 0));
            var hand = V(0.18f * s, 0.18f, 0.06f);
            b.Limb(arm, V(0.07f * s, 0.36f, 0), hand, 0.022f, 0.018f, pale);
            Digits(b, arm, hand, V(0.2f * s, -1f, 0.3f), V(0.6f, 0, 0.3f), 0.05f, 0.008f, red, Shell);
        });
        // A pale face with a red bill turned down, under a hat of a lily pad with its brim turned up
        int head = b.Head(V(0, 0.4f, 0.01f));
        var c = V(0, 0.47f, 0.03f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Ell(head, c, r, pale);
        b.Ell(head, V(0, 0.43f, 0.1f), V(0.05f, 0.018f, 0.045f), red, V(15f, 0, 0), Shell, 0.012f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, 0.48f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, sclera: true, pupil: Rgb(50, 50, 60));
        });
        b.Ell(head, V(0, 0.53f, 0.0f), V(0.13f, 0.03f, 0.13f), pad, mat: Leaf, blend: 0.015f);
        b.Torus(head, V(0, 0.55f, 0.0f), 0.13f, 0.02f, pad, V(10f, 0, 0), mat: Leaf, blend: 0.01f);
        b.Ell(head, V(0, 0.57f, -0.01f), V(0.08f, 0.04f, 0.08f), pad, mat: Leaf, blend: 0.02f);
        return b;
    }

    /// <summary>Ludicolo: a dancing pineapple of a duck, yellow with green zigzags, green limbs and a sombrero of a lily pad.</summary>
    private static PokeBuilder Ludicolo()
    {
        var b = new PokeBuilder("Ludicolo", 0.9f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 226, 140);
        var zig = Rgb(150, 188, 72);
        var green = Rgb(124, 194, 92);
        var pad = Rgb(120, 192, 100);
        var bill = Rgb(246, 200, 120);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.16f, 0));
            b.Limb(leg, V(0.08f * s, 0.17f, 0), V(0.1f * s, 0.04f, 0.02f), 0.04f, 0.034f, green);
            b.Ell(leg, V(0.1f * s, 0.028f, 0.04f), V(0.045f, 0.028f, 0.06f), green);
        });
        var bc = V(0, 0.36f, 0);
        var br = V(0.15f, 0.21f, 0.13f);
        b.Ell(Body, bc, br, yellow);
        // Zigzag bands round its body, like a pineapple's
        foreach (float y in new[] { 0.28f, 0.4f })
            for (int i = 0; i < 10; i++)
            {
                float a = MathF.Tau * i / 10f;
                var n = V(MathF.Sin(a), (y - bc.Y) / br.Y, MathF.Cos(a));
                b.Mark(Body, Out(bc, br, default, n), n, 0.04f, 0.02f, zig, MarkShape.Zigzag);
            }
        // Arms out wide, palms up
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.42f, 0));
            var hand = V(0.27f * s, 0.5f, 0.04f);
            b.Limb(arm, V(0.13f * s, 0.42f, 0), hand, 0.035f, 0.03f, green);
            b.Ell(arm, hand + V(0.02f * s, 0.01f, 0), V(0.04f, 0.035f, 0.035f), green);
        });
        // A beak open wide in its face, and the sombrero with a jagged brim and a brown knob at its top
        int head = b.Head(V(0, 0.48f, 0.02f));
        var mouth = V(0, 0.42f, 0.13f);
        Grin(b, Body, mouth, V(0.065f, 0.05f, 0.03f), Rgb(230, 130, 130));
        b.Ell(head, V(0, 0.48f, 0.125f), V(0.075f, 0.022f, 0.04f), bill, mat: Shell, blend: 0.01f);
        b.Ell(head, V(0, 0.37f, 0.12f), V(0.055f, 0.018f, 0.03f), bill, mat: Shell, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.3f * s, 0.85f, 0.55f);
            b.Eye(Body, Out(bc, br, default, look), look, 0.02f, sclera: true, pupil: Rgb(40, 40, 50));
        });
        b.Ell(head, V(0, 0.58f, -0.01f), V(0.22f, 0.025f, 0.2f), pad, mat: Leaf, blend: 0.02f);
        for (int i = 0; i < 12; i++)
        {
            float a = MathF.Tau * i / 12f;
            var root = V(MathF.Sin(a) * 0.19f, 0.6f, -0.01f + MathF.Cos(a) * 0.17f);
            b.Spike(head, root, root + V(MathF.Sin(a) * 0.05f, 0.04f, MathF.Cos(a) * 0.05f), 0.03f, pad, 0.4f, Leaf, 0.008f);
        }
        b.Spike(head, V(0, 0.59f, -0.02f), V(0, 0.7f, -0.04f), 0.035f, Rgb(170, 130, 80), mat: Shell, blend: 0.012f);
        return b;
    }

    // ------------------------------------------------------------------ Seedot line

    /// <summary>Seedot: an acorn on stubby feet, a grey cap ringed in steps with a stalk on top, and wide pale rings round its eyes.</summary>
    private static PokeBuilder Seedot()
    {
        var b = new PokeBuilder("Seedot", 0.5f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Shell };
        var brown = Rgb(150, 112, 78);
        var cap = Rgb(150, 150, 150);
        var groove = Rgb(98, 96, 100);
        var cream = Rgb(240, 222, 170);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.06f, 0));
            b.Ell(leg, V(0.07f * s, 0.035f, 0.02f), V(0.045f, 0.035f, 0.055f), cream);
        });
        var bc = V(0, 0.18f, 0);
        var br = V(0.11f, 0.13f, 0.1f);
        b.Ell(Body, bc, br, brown);
        b.Spike(Body, V(0, 0.08f, 0), V(0, 0.035f, 0.01f), 0.04f, brown, blend: 0.02f);
        // Wide cream rings round its eyes
        PokeBuilder.Both(s =>
        {
            var n = V(0.45f * s, 0.05f, 1f);
            var at = Out(bc, br, default, n);
            b.Mark(Body, at, n, 0.035f, 0.035f, cream, MarkShape.Ring);
            b.Eye(Body, at, n, 0.016f, Rgb(36, 32, 40));
        });
        // A cap in steps, with a stalk
        int head = b.Head(V(0, 0.26f, 0));
        b.Ell(head, V(0, 0.27f, -0.005f), V(0.12f, 0.06f, 0.11f), cap);
        foreach (var (y, rr) in new[] { (0.29f, 0.11f), (0.31f, 0.085f), (0.326f, 0.055f) })
            b.PaintTorus(head, V(0, y, -0.005f), rr, 0.005f, groove, sz: 0.92f);
        b.Limb(head, V(0, 0.32f, -0.005f), V(0, 0.38f, -0.005f), 0.022f, 0.018f, cap);
        b.Ell(head, V(0, 0.385f, -0.005f), V(0.026f, 0.012f, 0.026f), cap, blend: 0.006f);
        return b;
    }

    /// <summary>Nuzleaf: a brown imp with a long pointed nose, a leaf on its head, pale bulging shorts and a pink mouth.</summary>
    private static PokeBuilder Nuzleaf()
    {
        var b = new PokeBuilder("Nuzleaf", 0.75f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var brown = Rgb(166, 118, 82);
        var pale = Rgb(226, 210, 186);
        var leaf = Rgb(84, 160, 86);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.18f, 0));
            b.Ell(leg, V(0.07f * s, 0.16f, 0.0f), V(0.06f, 0.06f, 0.06f), pale);
            b.Limb(leg, V(0.07f * s, 0.12f, 0), V(0.075f * s, 0.035f, 0.01f), 0.025f, 0.022f, brown);
            b.Ell(leg, V(0.075f * s, 0.02f, 0.03f), V(0.03f, 0.02f, 0.05f), brown);
        });
        var bc = V(0, 0.3f, 0);
        var br = V(0.075f, 0.1f, 0.065f);
        b.Ell(Body, bc, br, brown);
        // Arms flexed up
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.36f, 0));
            var elbow = V(0.15f * s, 0.34f, 0.0f);
            var fist = V(0.14f * s, 0.46f, 0.02f);
            b.Limb(arm, V(0.07f * s, 0.36f, 0), elbow, 0.028f, 0.024f, brown);
            b.Limb(arm, elbow, fist, 0.024f, 0.022f, brown);
            b.Ell(arm, fist, V(0.03f, 0.03f, 0.03f), brown);
        });
        // A head with a long pointed nose, a leaf on top and a pink mouth
        int head = b.Head(V(0, 0.4f, 0.01f));
        var c = V(0, 0.47f, 0.02f);
        var r = V(0.08f, 0.075f, 0.075f);
        b.Ell(head, c, r, brown);
        b.PaintEll(head, c + V(0, 0.015f, 0.04f), V(0.07f, 0.025f, 0.05f), Rgb(110, 76, 56));
        b.Spike(head, c + V(0, 0.01f, 0.05f), c + V(0, 0.02f, 0.17f), 0.03f, pale, blend: 0.01f);
        b.Mark(head, On(c, r, 0, 0.435f), V(0, -0.3f, 1f), 0.014f, 0.014f, Rgb(232, 140, 150));
        Blade(b, head, c + V(0, 0.06f, 0), c + V(0.02f, 0.13f, -0.1f), 0.04f, leaf, V(0, 1f, 0.2f), 0.2f, Leaf);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.49f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, pupil: Rgb(40, 34, 30), glare: true);
        });
        return b;
    }

    /// <summary>Shiftry: a long-nosed tengu with a white mane, a leaf crown, great leaf fans for hands and clogs for feet.</summary>
    private static PokeBuilder Shiftry()
    {
        var b = new PokeBuilder("Shiftry", 0.9f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var brown = Rgb(160, 110, 74);
        var white = Rgb(242, 242, 246);
        var leaf = Rgb(80, 156, 84);
        var clog = Rgb(132, 92, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.3f, 0));
            var knee = V(0.1f * s, 0.18f, 0.04f);
            b.Limb(leg, V(0.07f * s, 0.32f, 0), knee, 0.05f, 0.03f, brown);
            b.Limb(leg, knee, V(0.1f * s, 0.07f, 0.0f), 0.028f, 0.024f, brown);
            b.Box(leg, V(0.1f * s, 0.06f, 0.02f), V(0.035f, 0.01f, 0.07f), 0.006f, clog, mat: Shell, blend: 0.006f);
            b.Box(leg, V(0.1f * s, 0.025f, 0.02f), V(0.03f, 0.025f, 0.012f), 0.004f, clog, mat: Shell, blend: 0.006f);
        });
        var bc = V(0, 0.42f, 0);
        var br = V(0.08f, 0.12f, 0.07f);
        b.Ell(Body, bc, br, brown);
        // A white mane falling down its back from its head
        b.Ell(Body, V(0, 0.48f, -0.06f), V(0.1f, 0.14f, 0.06f), white, blend: 0.02f);
        FurTufts(b, Body, V(0, 0.45f, -0.06f), V(0.09f, 0.12f, 0.05f), 14, 0.09f, 0.025f, white, -0.2f, -0.8f, 0.6f);
        // Arms ending in great fans of leaves
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.5f, 0));
            var hand = V(0.2f * s, 0.42f, 0.06f);
            b.Limb(arm, V(0.07f * s, 0.5f, 0), hand, 0.026f, 0.022f, brown);
            for (int i = 0; i < 4; i++)
            {
                float a = (-40f + 30f * i) * Degree;
                var dir = Vector3.Normalize(V(MathF.Cos(a) * s, MathF.Sin(a), 0.2f));
                PointedLeaf(b, arm, hand, hand + dir * 0.2f, 0.05f, leaf, V(0, 0, 1f), 0.16f);
            }
        });
        // A long pointed nose, a fierce face, a crown of leaves
        int head = b.Head(V(0, 0.55f, 0.02f));
        var c = V(0, 0.62f, 0.03f);
        var r = V(0.075f, 0.075f, 0.07f);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0, -0.02f, 0.05f), V(0.06f, 0.04f, 0.03f), brown);
        b.Spike(head, c + V(0, -0.005f, 0.04f), c + V(0, 0.02f, 0.22f), 0.025f, brown, blend: 0.01f);
        foreach (var x in new[] { -0.04f, 0f, 0.04f })
            PointedLeaf(b, head, c + V(x, 0.06f, -0.01f), c + V(x * 2.5f, 0.17f, -0.05f), 0.035f, leaf, V(0, 0, 1f), 0.16f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, 0.635f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(240, 200, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Taillow line

    /// <summary>Taillow: a little navy swallow, a red face over a white breast, a yellow beak and a forked tail.</summary>
    private static PokeBuilder Taillow()
    {
        var b = new PokeBuilder("Taillow", 0.5f, BodyPlan.Bird, V(0, 0.2f, 0)) { Coat = Fur };
        var navy = Rgb(48, 60, 104);
        var red = Rgb(214, 80, 90);
        var white = Rgb(242, 242, 246);
        var foot = Rgb(232, 180, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.07f, 0.01f));
            b.Limb(leg, V(0.035f * s, 0.08f, 0.01f), V(0.04f * s, 0.02f, 0.02f), 0.01f, 0.008f, foot);
            Digits(b, leg, V(0.04f * s, 0.012f, 0.025f), V(0, 0, 1f), V(0.7f, 0, 0), 0.03f, 0.007f, foot);
        });
        var bc = V(0, 0.17f, 0);
        var br = V(0.085f, 0.09f, 0.12f);
        b.Ell(Body, bc, br, navy, V(-15f, 0, 0));
        b.PaintEll(Body, V(0, 0.14f, 0.06f), V(0.075f, 0.07f, 0.08f), white);
        // Long pointed wings folded back
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.07f * s, 0.2f, 0.03f));
            Blade(b, wing, V(0.075f * s, 0.2f, 0.03f), V(0.1f * s, 0.15f, -0.22f), 0.05f, navy, V(s, 0.3f, 0), 0.2f);
        });
        int tail = b.Tail(V(0, 0.16f, -0.1f));
        PokeBuilder.Both(s => Blade(b, tail, V(0.01f * s, 0.16f, -0.1f), V(0.07f * s, 0.12f, -0.3f), 0.03f, navy, V(0, 1f, 0), 0.2f));
        // The head: navy cap, a red face and a little yellow beak
        int head = b.Head(V(0, 0.24f, 0.05f));
        var c = V(0, 0.29f, 0.07f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Ell(head, c, r, navy);
        b.PaintEll(head, c + V(0, -0.025f, 0.04f), V(0.07f, 0.05f, 0.05f), red);
        b.PaintEll(head, c + V(0, 0.05f, 0.04f), V(0.025f, 0.02f, 0.03f), red);
        b.Spike(head, On(c, r, 0, 0.28f) - V(0, 0, 0.015f), On(c, r, 0, 0.28f) + V(0, -0.005f, 0.05f), 0.02f, Rgb(244, 200, 80), 0.6f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.3f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(36, 36, 48));
        });
        return b;
    }

    /// <summary>Swellow: a sleek navy swallow, red at its face and breast, two long tail streamers tipped red and a blue crest.</summary>
    private static PokeBuilder Swellow()
    {
        var b = new PokeBuilder("Swellow", 0.85f, BodyPlan.Bird, V(0, 0.3f, 0)) { Coat = Fur };
        var navy = Rgb(44, 54, 100);
        var red = Rgb(212, 72, 86);
        var white = Rgb(242, 242, 246);
        var foot = Rgb(214, 76, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.17f, 0.01f));
            b.Limb(leg, V(0.04f * s, 0.19f, 0.01f), V(0.045f * s, 0.025f, 0.02f), 0.013f, 0.01f, foot);
            Digits(b, leg, V(0.045f * s, 0.014f, 0.025f), V(0, 0, 1f), V(0.7f, 0, 0), 0.04f, 0.008f, foot);
        });
        var bc = V(0, 0.26f, 0);
        var br = V(0.09f, 0.11f, 0.15f);
        b.Ell(Body, bc, br, navy, V(-20f, 0, 0));
        b.PaintEll(Body, V(0, 0.22f, 0.08f), V(0.08f, 0.08f, 0.1f), white);
        b.PaintEll(Body, V(0, 0.3f, 0.1f), V(0.08f, 0.045f, 0.06f), red);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.075f * s, 0.3f, 0.03f));
            Blade(b, wing, V(0.08f * s, 0.3f, 0.03f), V(0.12f * s, 0.24f, -0.32f), 0.065f, navy, V(s, 0.3f, 0), 0.2f);
        });
        // Two long tail streamers, red at their tips
        int tail = b.Tail(V(0, 0.22f, -0.13f));
        PokeBuilder.Both(s =>
        {
            var tip = V(0.12f * s, 0.12f, -0.55f);
            Blade(b, tail, V(0.01f * s, 0.22f, -0.13f), tip, 0.03f, navy, V(0, 1f, 0), 0.2f);
            b.PaintEll(tail, Vector3.Lerp(V(0.01f * s, 0.22f, -0.13f), tip, 0.85f), V(0.03f, 0.03f, 0.08f), red, Euler(tip - V(0.01f * s, 0.22f, -0.13f)));
        });
        // A head with a red face, a sharp beak and a crest swept back
        int head = b.Head(V(0, 0.36f, 0.08f));
        var c = V(0, 0.42f, 0.1f);
        var r = V(0.07f, 0.065f, 0.075f);
        b.Ell(head, c, r, navy);
        b.PaintEll(head, c + V(0, -0.02f, 0.05f), V(0.06f, 0.04f, 0.04f), red);
        b.Spike(head, On(c, r, 0, 0.41f) - V(0, 0, 0.015f), On(c, r, 0, 0.41f) + V(0, -0.005f, 0.07f), 0.022f, Rgb(244, 200, 80), 0.6f, Shell);
        foreach (var x in new[] { -0.02f, 0.02f })
            Blade(b, head, c + V(x, 0.05f, -0.02f), c + V(x * 2.5f, 0.09f, -0.16f), 0.025f, navy, V(0, 1f, 0), 0.2f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, 0.43f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(36, 36, 48), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Surskit line

    /// <summary>Surskit: a little blue pond-skater on four long thin legs, a yellow knob on its head and pink cheeks.</summary>
    private static PokeBuilder Surskit()
    {
        var b = new PokeBuilder("Surskit", 0.55f, BodyPlan.Quadruped, V(0, 0.16f, 0)) { Coat = Shell };
        var blue = Rgb(110, 176, 230);
        var yellow = Rgb(246, 214, 100);
        foreach (var (z, front) in new[] { (0.03f, true), (-0.03f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.15f, z), front);
                var knee = V(0.2f * s, 0.2f, z * 4f);
                b.Limb(leg, V(0.05f * s, 0.15f, z), knee, 0.012f, 0.01f, blue);
                b.Limb(leg, knee, V(0.3f * s, 0.012f, z * 6f), 0.01f, 0.008f, blue);
            });
        var bc = V(0, 0.16f, 0);
        var br = V(0.07f, 0.065f, 0.07f);
        b.Ell(Body, bc, br, blue);
        int head = b.Head(V(0, 0.2f, 0));
        b.Spike(head, V(0, 0.2f, -0.01f), V(0, 0.3f, -0.04f), 0.035f, yellow, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.03f * s, 0.165f);
            b.Eye(Body, at, Outward(bc, br, at), 0.012f, Rgb(36, 36, 48));
            b.Mark(Body, On(bc, br, 0.05f * s, 0.14f), V(0.6f * s, -0.2f, 1f), 0.01f, 0.007f, Rgb(240, 130, 150));
        });
        return b;
    }

    /// <summary>Masquerain: a little white flier under four great wing-antennae, each marked with an eye, on four clear wings.</summary>
    private static PokeBuilder Masquerain()
    {
        var b = new PokeBuilder("Masquerain", 0.7f, BodyPlan.Floating, V(0, 0.32f, 0)) { Coat = Shell }.Hover();
        var white = Rgb(236, 240, 246);
        var clear = Rgb(196, 222, 242);
        var orange = Rgb(242, 152, 96);
        var eye = Rgb(150, 70, 90);
        var bc = V(0, 0.3f, 0);
        var br = V(0.05f, 0.05f, 0.065f);
        b.Ell(Body, bc, br, white);
        b.Spike(Body, V(0, 0.29f, -0.04f), V(0, 0.25f, -0.15f), 0.03f, white, blend: 0.01f);
        // Four clear wings, flat
        foreach (var (z, ang) in new[] { (0.01f, 20f), (-0.03f, -10f) })
            PokeBuilder.Both(s =>
            {
                int wing = b.Wing(s, V(0.04f * s, 0.29f, z));
                float a = ang * Degree;
                Blade(b, wing, V(0.04f * s, 0.29f, z), V(0.04f * s, 0.29f, z) + V(MathF.Cos(a) * 0.2f * s, -0.03f, MathF.Sin(a) * 0.2f), 0.05f, clear, V(0, 1f, 0), 0.25f);
            });
        // The head with its four great "antenna" wings fanned over it, each painted with a staring eye
        int head = b.Head(V(0, 0.34f, 0.05f));
        var c = V(0, 0.36f, 0.06f);
        var r = V(0.045f, 0.045f, 0.045f);
        b.Ell(head, c, r, orange);
        PokeBuilder.Both(s =>
        {
            foreach (var (k, lean) in new[] { (0f, 0.4f), (1f, 0.9f) })
            {
                int horn = b.Ear(head, s, c + V(0.03f * s, 0.03f, 0));
                var root = c + V(0.025f * s, 0.035f, -0.01f);
                var tip = root + Vector3.Normalize(V(lean * s, 1f - 0.4f * k, -0.2f - 0.4f * k)) * 0.24f;
                b.Limb(horn, root, Vector3.Lerp(root, tip, 0.5f), 0.01f, 0.008f, white);
                var wc = Vector3.Lerp(root, tip, 0.7f);
                var face = Vector3.Normalize(V(0.2f * s, 0.2f, 1f));
                b.Ell(horn, wc, V(0.075f, 0.075f, 0.01f), white, Euler(face) + V(90f, 0, 0), blend: 0.012f);
                b.PaintTorus(horn, wc, 0.06f, 0.012f, orange, Euler(face) + V(90f, 0, 0));
                b.Mark(horn, wc + face * 0.01f, face, 0.03f, 0.022f, eye);
            }
        });
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, 0.365f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(40, 36, 50));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Shroomish line

    /// <summary>Shroomish: a squat beige mushroom spotted green, a grumpy face and a ring of green nubs under it for feet.</summary>
    private static PokeBuilder Shroomish()
    {
        var b = new PokeBuilder("Shroomish", 0.5f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Fur };
        var beige = Rgb(232, 218, 182);
        var green = Rgb(118, 168, 120);
        foreach (var (z, front) in new[] { (0.05f, true), (-0.05f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.05f, z), front);
                b.Ell(leg, V(0.06f * s, 0.035f, z), V(0.04f, 0.035f, 0.04f), green);
            });
        var bc = V(0, 0.14f, 0);
        var br = V(0.15f, 0.1f, 0.14f);
        b.Ell(Body, bc, br, beige);
        // A frilly rim and a rounded top
        for (int i = 0; i < 10; i++)
        {
            float a = MathF.Tau * i / 10f;
            b.Ell(Body, V(MathF.Sin(a) * 0.15f, 0.1f, MathF.Cos(a) * 0.14f), V(0.04f, 0.03f, 0.04f), beige, blend: 0.02f);
        }
        b.Ell(Body, V(0, 0.22f, -0.01f), V(0.09f, 0.06f, 0.09f), beige, blend: 0.03f);
        foreach (var (x, y, z) in new[] { (0.6f, 0.4f, -0.4f), (-0.7f, 0.3f, -0.2f), (0.1f, 0.7f, -0.6f), (-0.3f, 0.5f, -0.8f), (0.9f, 0.1f, 0.2f), (-0.95f, 0.05f, 0.25f) })
        {
            var n = V(x, y, z);
            b.Mark(Body, Out(bc, br, default, n), n, 0.035f, 0.035f, green);
        }
        // A grumpy face
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.05f * s, 0.16f);
            b.Eye(Body, at, Outward(bc, br, at), 0.014f, Rgb(40, 36, 40), glare: true);
        });
        b.Mark(Body, On(bc, br, 0, 0.12f), V(0, -0.2f, 1f), 0.025f, 0.008f, Rgb(80, 66, 60), MarkShape.Smile, 180f);
        int head = b.Head(V(0, 0.2f, 0));
        b.Ell(head, V(0, 0.26f, -0.02f), V(0.05f, 0.03f, 0.05f), beige, blend: 0.02f);
        return b;
    }

    /// <summary>Breloom: a green kangaroo of a mushroom, a cap with red berries on its head, red claws, and a tail with green seeds at its end.</summary>
    private static PokeBuilder Breloom()
    {
        var b = new PokeBuilder("Breloom", 0.8f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var green = Rgb(98, 160, 96);
        var cream = Rgb(240, 228, 196);
        var red = Rgb(222, 82, 82);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.2f, -0.01f));
            b.Ell(leg, V(0.07f * s, 0.18f, -0.01f), V(0.055f, 0.08f, 0.065f), green);
            b.Limb(leg, V(0.07f * s, 0.12f, 0), V(0.075f * s, 0.04f, 0.02f), 0.025f, 0.02f, cream);
            Digits(b, leg, V(0.075f * s, 0.02f, 0.03f), V(0.1f * s, 0, 1f), V(0.6f, 0, 0), 0.04f, 0.012f, red, Shell);
        });
        var bc = V(0, 0.3f, 0);
        var br = V(0.085f, 0.12f, 0.075f);
        b.Ell(Body, bc, br, green);
        // A collar of green leaves and short cream arms with red claws
        for (int i = 0; i < 8; i++)
        {
            float a = MathF.Tau * i / 8f;
            var root = V(MathF.Sin(a) * 0.06f, 0.4f, MathF.Cos(a) * 0.05f);
            b.Spike(Body, root, root + V(MathF.Sin(a) * 0.06f, -0.03f, MathF.Cos(a) * 0.05f), 0.025f, green, 0.4f, blend: 0.008f);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.35f, 0.02f));
            var hand = V(0.14f * s, 0.3f, 0.12f);
            b.Limb(arm, V(0.07f * s, 0.35f, 0.02f), hand, 0.024f, 0.02f, cream);
            Digits(b, arm, hand, V(0.2f * s, -0.3f, 1f), V(0.5f, 0, 0), 0.04f, 0.01f, red, Shell);
        });
        // A long tail ending in three green seeds
        int tail = b.Tail(V(0, 0.24f, -0.07f));
        var tp = Smooth(3, V(0, 0.24f, -0.06f), V(0, 0.16f, -0.2f), V(0, 0.22f, -0.32f), V(0, 0.34f, -0.36f));
        b.Tube(tail, tp, 0.03f, 0.018f, cream, blend: 0f);
        foreach (var o in new[] { V(0, 0.03f, 0), V(0.03f, -0.01f, 0.01f), V(-0.03f, -0.01f, 0.01f) })
            b.Ell(tail, tp[^1] + o, V(0.03f, 0.03f, 0.03f), green, blend: 0.008f);
        // A cream face with a long snout, under a green cap with red berries on its rim
        int head = b.Head(V(0, 0.42f, 0.02f));
        var c = V(0, 0.48f, 0.04f);
        var r = V(0.065f, 0.065f, 0.07f);
        b.Ell(head, c, r, cream);
        b.Ell(head, V(0, 0.46f, 0.12f), V(0.035f, 0.03f, 0.05f), cream, blend: 0.02f);
        b.Ell(head, V(0, 0.55f, 0.0f), V(0.11f, 0.045f, 0.1f), green, blend: 0.02f);
        foreach (var x in new[] { -0.085f, 0.085f })
            b.Ell(head, V(x, 0.53f, 0.03f), V(0.026f, 0.026f, 0.026f), red, mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, 0.49f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(36, 36, 48), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Slakoth line

    /// <summary>Slakoth: a beige sloth sprawled on its belly, a brown stripe down its back, a tuft on its head and sleepy eyes.</summary>
    private static PokeBuilder Slakoth()
    {
        var b = new PokeBuilder("Slakoth", 0.6f, BodyPlan.Quadruped, V(0, 0.1f, 0)) { Coat = Fur };
        var beige = Rgb(206, 182, 150);
        var brown = Rgb(132, 96, 70);
        var claw = Rgb(240, 236, 226);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.13f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.07f, z), front);
                var paw = V(0.13f * s, 0.03f, z + (front ? 0.08f : -0.08f));
                b.Limb(leg, V(0.06f * s, 0.07f, z), paw, 0.03f, 0.024f, beige);
                Digits(b, leg, paw, V(0.3f * s, -0.2f, front ? 1f : -1f), V(0.5f, 0, 0), 0.035f, 0.008f, claw, Shell);
            });
        var bc = V(0, 0.09f, -0.01f);
        var br = V(0.09f, 0.075f, 0.17f);
        b.Ell(Body, bc, br, beige);
        b.PaintEll(Body, V(0, 0.15f, -0.02f), V(0.03f, 0.03f, 0.17f), brown);
        foreach (float z in new[] { 0.03f, -0.05f })
            b.PaintTorus(Body, V(0, 0.09f, z), 0.08f, 0.012f, brown, V(90f, 0, 0), 1f, 0.85f);
        // A round head laid forward, a tuft on top, droopy eyes, a pink nose
        int head = b.Head(V(0, 0.11f, 0.15f));
        var c = V(0, 0.12f, 0.2f);
        var r = V(0.08f, 0.07f, 0.07f);
        b.Ell(head, c, r, beige);
        b.PaintEll(head, c + V(0, 0.005f, 0.05f), V(0.065f, 0.035f, 0.035f), brown);
        b.Ell(head, V(0, 0.11f, 0.268f), V(0.02f, 0.014f, 0.01f), Rgb(232, 160, 150), mat: Shell, blend: 0.008f);
        foreach (var x in new[] { -0.015f, 0f, 0.015f })
            b.Spike(head, c + V(x, 0.06f, -0.02f), c + V(x * 3f, 0.11f, -0.05f), 0.014f, claw, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, 0.13f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(60, 44, 40), closed: true);
        });
        return b;
    }

    /// <summary>Vigoroth: a white ape that can't keep still, a red crest on its head, a brown band round its middle and long clawed arms flung up.</summary>
    private static PokeBuilder Vigoroth()
    {
        var b = new PokeBuilder("Vigoroth", 0.85f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var white = Rgb(240, 238, 234);
        var brown = Rgb(150, 110, 82);
        var red = Rgb(214, 60, 64);
        var claw = Rgb(110, 110, 120);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.24f, 0));
            b.Limb(leg, V(0.06f * s, 0.25f, 0), V(0.08f * s, 0.05f, 0.02f), 0.04f, 0.03f, white);
            Digits(b, leg, V(0.08f * s, 0.02f, 0.03f), V(0.1f * s, 0, 1f), V(0.6f, 0, 0), 0.04f, 0.012f, claw, Shell);
        });
        var bc = V(0, 0.36f, 0);
        var br = V(0.1f, 0.14f, 0.085f);
        b.Ell(Body, bc, br, white);
        b.PaintTorus(Body, V(0, 0.3f, 0), 0.095f, 0.022f, brown, sz: 0.85f);
        FurTufts(b, Body, V(0, 0.36f, -0.02f), V(0.1f, 0.12f, 0.07f), 10, 0.06f, 0.02f, white, 0.5f, -0.5f, 0.4f);
        // Long arms, one flung up, three grey claws on each hand
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.09f * s, 0.44f, 0));
            var hand = s > 0 ? V(0.16f * s, 0.74f, 0.04f) : V(0.24f * s, 0.32f, 0.08f);
            b.Limb(arm, V(0.09f * s, 0.44f, 0), hand, 0.035f, 0.03f, white);
            Digits(b, arm, hand, s > 0 ? V(0, 1f, 0.2f) : V(0.3f * s, -1f, 0.3f), V(0.5f, 0, 0), 0.06f, 0.012f, claw, Shell);
        });
        // A head with a red crest, a wide mouth open
        int head = b.Head(V(0, 0.48f, 0.02f));
        var c = V(0, 0.55f, 0.04f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Ell(head, c, r, white);
        b.PaintEll(head, c + V(0, 0.01f, 0.05f), V(0.06f, 0.035f, 0.04f), brown);
        Blade(b, head, c + V(0, 0.05f, 0.02f), c + V(0, 0.15f, -0.02f), 0.05f, red, V(0, 0, 1f), 0.3f);
        Grin(b, head, On(c, r, 0, 0.52f), V(0.03f, 0.02f, 0.02f), Rgb(170, 70, 80));
        b.Ell(head, On(c, r, 0, 0.552f) + V(0, 0, 0.005f), V(0.015f, 0.01f, 0.01f), Rgb(110, 76, 66), mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, 0.565f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 36, 40), glare: true);
        });
        return b;
    }

    /// <summary>Slaking: a great brown gorilla lolling on its belly, chin on its fist, a shaggy white face and a pink nose.</summary>
    private static PokeBuilder Slaking()
    {
        var b = new PokeBuilder("Slaking", 1f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Fur };
        var brown = Rgb(150, 112, 82);
        var tan = Rgb(214, 192, 160);
        var white = Rgb(244, 242, 238);
        var claw = Rgb(110, 104, 110);
        // Hind legs sprawled behind, an arm forward under its chin, the other lying along its side
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.12f, -0.2f), false);
            b.Limb(leg, V(0.12f * s, 0.12f, -0.2f), V(0.18f * s, 0.06f, -0.38f), 0.08f, 0.06f, brown);
            b.Ell(leg, V(0.18f * s, 0.05f, -0.43f), V(0.06f, 0.05f, 0.05f), tan);
        });
        PokeBuilder.Both(s =>
        {
            int arm = b.Leg(s, V(0.16f * s, 0.18f, 0.12f), true);
            var elbow = s > 0 ? V(0.12f * s, 0.05f, 0.3f) : V(0.26f * s, 0.06f, 0.0f);
            var fist = s > 0 ? V(0.03f * s, 0.18f, 0.34f) : V(0.24f * s, 0.05f, -0.16f);
            b.Limb(arm, V(0.16f * s, 0.18f, 0.12f), elbow, 0.07f, 0.06f, brown);
            b.Limb(arm, elbow, fist, 0.06f, 0.05f, brown);
            b.Ell(arm, fist, V(0.055f, 0.05f, 0.055f), tan);
            Digits(b, arm, fist + V(0, 0, 0.02f), s > 0 ? V(0, 0.4f, 1f) : V(0, -0.2f, -1f), V(0.5f, 0, 0), 0.04f, 0.012f, claw, Shell);
        });
        var bc = V(0, 0.18f, -0.04f);
        var br = V(0.2f, 0.15f, 0.27f);
        b.Ell(Body, bc, br, brown);
        b.PaintEll(Body, V(0, 0.08f, -0.02f), V(0.16f, 0.06f, 0.22f), tan);
        // A big head on its fist: a shaggy white beard and brows, a pink nose and a face half asleep
        int head = b.Head(V(0, 0.28f, 0.2f));
        var c = V(0, 0.33f, 0.3f);
        var r = V(0.11f, 0.1f, 0.095f);
        b.Ell(head, c, r, tan);
        b.Ell(head, V(0, 0.38f, 0.26f), V(0.12f, 0.08f, 0.1f), brown, blend: 0.03f);
        FurTufts(b, head, c + V(0, -0.04f, 0.02f), V(0.1f, 0.06f, 0.08f), 10, 0.07f, 0.024f, white, 0.75f, -0.9f, 0.6f);
        b.Ell(head, V(0, 0.33f, 0.4f), V(0.045f, 0.03f, 0.025f), Rgb(232, 160, 160), mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.05f * s, 0.05f, 0.07f), V(0.04f, 0.015f, 0.02f), white, blend: 0.01f);
            var at = On(c, r, 0.045f * s, 0.355f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(50, 40, 40), closed: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Nincada line

    /// <summary>Nincada: a pale cicada nymph, low on four thin legs, big grey-brown digging claws and small green wings on its back.</summary>
    private static PokeBuilder Nincada()
    {
        var b = new PokeBuilder("Nincada", 0.5f, BodyPlan.Quadruped, V(0, 0.12f, 0)) { Coat = Shell };
        var white = Rgb(236, 236, 230);
        var gray = Rgb(150, 140, 126);
        var wing = Rgb(196, 226, 170);
        foreach (var (z, front) in new[] { (0.03f, true), (-0.07f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.1f, z), front);
                var knee = V(0.14f * s, 0.11f, z);
                b.Limb(leg, V(0.05f * s, 0.1f, z), knee, 0.014f, 0.012f, white);
                b.Limb(leg, knee, V(0.18f * s, 0.01f, z + 0.01f), 0.012f, 0.008f, white);
            });
        var bc = V(0, 0.12f, -0.02f);
        var br = V(0.07f, 0.06f, 0.12f);
        b.Ell(Body, bc, br, white);
        for (int i = 0; i < 3; i++)
            b.PaintTorus(Body, V(0, 0.12f, -0.07f - i * 0.03f), 0.06f, 0.004f, Rgb(200, 200, 190), V(90f, 0, 0), 1f, 0.85f);
        PokeBuilder.Both(s => Blade(b, Body, V(0.03f * s, 0.17f, -0.02f), V(0.07f * s, 0.2f, -0.12f), 0.035f, wing, V(0, 1f, 0.3f), 0.2f));
        // A head with big digging claws hanging under it, and two feelers
        int head = b.Head(V(0, 0.13f, 0.09f));
        var c = V(0, 0.14f, 0.12f);
        var r = V(0.06f, 0.05f, 0.055f);
        b.Ell(head, c, r, white);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.1f, 0.12f));
            b.Limb(arm, V(0.05f * s, 0.11f, 0.12f), V(0.09f * s, 0.06f, 0.17f), 0.02f, 0.02f, white);
            b.Spike(arm, V(0.09f * s, 0.07f, 0.17f), V(0.1f * s, 0.0f, 0.24f), 0.04f, gray, 0.5f, Shell);
            b.Limb(head, c + V(0.02f * s, 0.04f, 0.02f), c + V(0.06f * s, 0.12f, 0.0f), 0.006f, 0.004f, white);
            var at = On(c, r, 0.03f * s, 0.15f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(40, 40, 40));
        });
        return b;
    }

    /// <summary>Ninjask: a black and yellow ninja wasp hovering on clear wings tipped red, red eyes and a mask over its face.</summary>
    private static PokeBuilder Ninjask()
    {
        var b = new PokeBuilder("Ninjask", 0.6f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var black = Rgb(48, 46, 54);
        var yellow = Rgb(236, 196, 70);
        var clear = Rgb(226, 232, 240);
        var red = Rgb(214, 60, 70);
        var bc = V(0, 0.27f, -0.02f);
        var br = V(0.08f, 0.08f, 0.1f);
        b.Ell(Body, bc, br, black, V(-20f, 0, 0));
        b.PaintTorus(Body, bc + V(0, 0.01f, 0.03f), 0.075f, 0.014f, yellow, V(70f, 0, 0), 1f, 1f);
        // Thin legs tucked under
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.22f, 0.02f));
            b.Limb(leg, V(0.04f * s, 0.22f, 0.02f), V(0.06f * s, 0.14f, 0.06f), 0.01f, 0.008f, Rgb(120, 120, 130));
        });
        // Four clear wings, the larger tipped with red stripes
        foreach (var (y, len, w) in new[] { (0.34f, 0.26f, 0.06f), (0.3f, 0.18f, 0.045f) })
            PokeBuilder.Both(s =>
            {
                int wing = b.Wing(s, V(0.05f * s, y, -0.02f));
                var tip = V(0.05f * s, y, -0.02f) + Vector3.Normalize(V(0.9f * s, 0.6f, -0.3f)) * len;
                Blade(b, wing, V(0.05f * s, y, -0.02f), tip, w, clear, V(0, 0.3f, 1f), 0.1f);
                b.PaintEll(wing, Vector3.Lerp(V(0.05f * s, y, -0.02f), tip, 0.8f), V(w, len * 0.1f, w), red, Euler(tip - V(0.05f * s, y, -0.02f)));
            });
        // A yellow-masked head with red eyes and two horns
        int head = b.Head(V(0, 0.34f, 0.06f));
        var c = V(0, 0.38f, 0.09f);
        var r = V(0.06f, 0.055f, 0.055f);
        b.Ell(head, c, r, black);
        b.PaintEll(head, c + V(0, 0.03f, 0.03f), V(0.06f, 0.025f, 0.04f), yellow);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.025f * s, 0.04f, 0), c + V(0.06f * s, 0.12f, -0.04f), 0.014f, black, blend: 0.006f);
            var at = On(c, r, 0.03f * s, 0.375f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, red, glare: true);
        });
        return Lift(b);
    }

    /// <summary>Shedinja: a brown husk hovering under a halo, a crack down its back, small torn wings and hollow eyes.</summary>
    private static PokeBuilder Shedinja()
    {
        var b = new PokeBuilder("Shedinja", 0.6f, BodyPlan.Floating, V(0, 0.28f, 0)) { Coat = Shell }.Hover();
        var brown = Rgb(170, 140, 90);
        var gray = Rgb(150, 136, 120);
        var halo = Rgb(236, 240, 250);
        var bc = V(0, 0.28f, 0);
        var br = V(0.085f, 0.11f, 0.08f);
        b.Ell(Body, bc, br, brown);
        b.Ell(Body, V(0, 0.18f, 0.0f), V(0.06f, 0.05f, 0.06f), gray, blend: 0.03f);
        for (int i = 0; i < 3; i++)
            b.PaintTorus(Body, V(0, 0.17f + i * 0.025f, 0), 0.055f, 0.004f, Rgb(110, 100, 90), sz: 1f);
        b.PaintEll(Body, V(0, 0.32f, -0.075f), V(0.008f, 0.08f, 0.02f), Rgb(60, 50, 40), soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.06f * s, 0.34f, -0.03f));
            foreach (var (dy, len) in new[] { (0.02f, 0.2f), (-0.04f, 0.16f) })
                Blade(b, wing, V(0.06f * s, 0.34f + dy, -0.03f), V(0.06f * s, 0.34f + dy, -0.03f) + Vector3.Normalize(V(0.9f * s, 0.25f, -0.4f)) * len, 0.04f, brown, V(0, 1f, 0.3f), 0.15f);
            int arm = b.Arm(s, V(0.06f * s, 0.24f, 0.05f));
            b.Spike(arm, V(0.06f * s, 0.24f, 0.05f), V(0.09f * s, 0.15f, 0.1f), 0.022f, brown, 0.5f);
        });
        int head = b.Head(V(0, 0.36f, 0.02f));
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.035f * s, 0.32f);
            b.Eye(Body, at, Outward(bc, br, at), 0.016f, sclera: true, white: Rgb(40, 34, 30), pupil: Rgb(250, 250, 250));
        });
        b.Spike(head, V(0, 0.38f, 0.0f), V(0, 0.42f, -0.02f), 0.02f, brown, blend: 0.006f);
        // The halo over its head, floating free
        b.Torus(head, V(0, 0.48f, -0.02f), 0.08f, 0.012f, halo, V(10f, 0, 0), mat: Glow);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Whismur line

    /// <summary>Whismur: a little pink round body with great floppy ears like loudspeakers, yellow inside, and yellow feet.</summary>
    private static PokeBuilder Whismur()
    {
        var b = new PokeBuilder("Whismur", 0.5f, BodyPlan.Biped, V(0, 0.18f, 0)) { Coat = Fur };
        var pink = Rgb(226, 188, 220);
        var yellow = Rgb(246, 214, 110);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.06f, 0));
            b.Ell(leg, V(0.07f * s, 0.028f, 0.03f), V(0.04f, 0.028f, 0.05f), yellow);
        });
        var bc = V(0, 0.18f, 0);
        var br = V(0.13f, 0.14f, 0.12f);
        b.Ell(Body, bc, br, pink);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.16f, 0.04f));
            b.Ell(arm, V(0.12f * s, 0.14f, 0.07f), V(0.03f, 0.03f, 0.03f), pink);
        });
        // Mouth and eyes like an open face, small dots for whiskers
        b.Mark(Body, On(bc, br, 0, 0.15f), V(0, -0.1f, 1f), 0.035f, 0.02f, Rgb(200, 100, 120), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.045f * s, 0.21f);
            b.Eye(Body, at, Outward(bc, br, at), 0.012f, Rgb(50, 40, 50), closed: true);
        });
        // Great round ears, flopping out, yellow inside
        int head = b.Head(V(0, 0.28f, 0));
        b.Ell(head, V(0, 0.28f, -0.01f), V(0.08f, 0.04f, 0.07f), pink, blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.29f, 0));
            var ec = V(0.15f * s, 0.36f, -0.01f);
            b.Limb(ear, V(0.06f * s, 0.29f, 0), ec, 0.03f, 0.03f, pink);
            b.Ell(ear, ec, V(0.09f, 0.075f, 0.022f), pink, V(0, 0, 25f * s), blend: 0.02f);
            b.Mark(ear, ec + V(0, 0, 0.022f), V(0, 0, 1f), 0.06f, 0.05f, yellow, rollDeg: 25f * s);
        });
        return b;
    }

    /// <summary>Loudred: a purple brute with a huge open mouth rimmed yellow, speaker ears and stubby limbs.</summary>
    private static PokeBuilder Loudred()
    {
        var b = new PokeBuilder("Loudred", 0.75f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var purple = Rgb(150, 136, 200);
        var yellow = Rgb(242, 210, 100);
        var dark = Rgb(80, 60, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.12f, 0));
            b.Limb(leg, V(0.08f * s, 0.13f, 0), V(0.1f * s, 0.04f, 0.02f), 0.045f, 0.04f, purple);
            b.Ell(leg, V(0.1f * s, 0.028f, 0.04f), V(0.045f, 0.028f, 0.06f), purple);
            b.Mark(leg, V(0.1f * s, 0.03f, 0.1f), V(0, 0, 1f), 0.02f, 0.012f, yellow);
        });
        var bc = V(0, 0.28f, 0);
        var br = V(0.14f, 0.16f, 0.12f);
        b.Ell(Body, bc, br, purple);
        // A huge mouth, rimmed yellow, with a white tooth at each corner
        var mouth = V(0, 0.27f, 0.11f);
        Grin(b, Body, mouth, V(0.08f, 0.09f, 0.04f), Rgb(150, 70, 100));
        b.Torus(Body, mouth + V(0, 0, 0.012f), 0.085f, 0.015f, yellow, V(90f, 0, 0), 0.95f, 1.05f, Shell);
        PokeBuilder.Both(s => b.Spike(Body, mouth + V(0.06f * s, 0.07f, 0.02f), mouth + V(0.06f * s, 0.04f, 0.02f), 0.012f, White, mat: Shell, blend: 0.004f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.32f, 0));
            var hand = V(0.2f * s, 0.4f, 0.05f);
            b.Limb(arm, V(0.12f * s, 0.32f, 0), hand, 0.04f, 0.035f, purple);
            b.Ell(arm, hand, V(0.04f, 0.04f, 0.035f), purple);
        });
        int tail = b.Tail(V(0, 0.18f, -0.1f));
        b.Limb(tail, V(0, 0.18f, -0.1f), V(0, 0.1f, -0.18f), 0.025f, 0.018f, purple);
        // Eyes over the mouth, and round speaker ears on stalks
        int head = b.Head(V(0, 0.4f, 0));
        b.Ell(head, V(0, 0.4f, -0.01f), V(0.09f, 0.04f, 0.08f), purple, blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.3f * s, 0.75f, 0.6f);
            b.Eye(Body, Out(bc, br, default, look), look, 0.015f, Rgb(220, 70, 80), glare: true);
            int ear = b.Ear(head, s, V(0.08f * s, 0.4f, 0));
            var ec = V(0.17f * s, 0.5f, 0.0f);
            b.Limb(ear, V(0.08f * s, 0.4f, 0), ec, 0.035f, 0.03f, purple);
            b.Ell(ear, ec, V(0.07f, 0.07f, 0.03f), purple, V(0, 25f * s, 0), blend: 0.015f);
            b.Torus(ear, ec + V(0.012f * s, 0, 0.022f), 0.045f, 0.012f, dark, V(90f, 25f * s, 0), mat: Shell, blend: 0.006f);
            b.Mark(ear, ec + V(0.012f * s, 0, 0.026f), V(0.4f * s, 0, 1f), 0.03f, 0.03f, Rgb(180, 110, 150));
        });
        return b;
    }

    /// <summary>Exploud: a purple beast with a cavernous mouth, pipes along its head, back and arms with yellow rims, and a forked tail.</summary>
    private static PokeBuilder Exploud()
    {
        var b = new PokeBuilder("Exploud", 1f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var purple = Rgb(136, 124, 196);
        var yellow = Rgb(240, 206, 96);
        var dark = Rgb(70, 50, 80);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.24f, -0.03f));
            b.Limb(leg, V(0.1f * s, 0.26f, -0.03f), V(0.12f * s, 0.05f, 0.0f), 0.07f, 0.05f, purple);
            b.Ell(leg, V(0.12f * s, 0.03f, 0.03f), V(0.06f, 0.03f, 0.08f), purple);
            foreach (float x in new[] { -0.03f, 0.03f })
                b.Ell(leg, V(0.12f * s + x, 0.02f, 0.1f), V(0.015f, 0.014f, 0.016f), White, mat: Shell, blend: 0.004f);
        });
        var bc = V(0, 0.42f, -0.01f);
        var br = V(0.15f, 0.18f, 0.14f);
        b.Ell(Body, bc, br, purple);
        // Pipes: a row down its back, two on its shoulders, yellow-rimmed; and a forked tail
        foreach (var (at, dir) in new[] { (V(0, 0.55f, -0.08f), V(0, 0.6f, -1f)), (V(0, 0.44f, -0.13f), V(0, 0.2f, -1f)) })
        {
            var tip = at + Vector3.Normalize(dir) * 0.1f;
            b.Limb(Body, at, tip, 0.03f, 0.03f, purple);
            b.Torus(Body, tip, 0.03f, 0.01f, yellow, Euler(dir), mat: Shell);
        }
        int tail = b.Tail(V(0, 0.3f, -0.13f));
        b.Limb(tail, V(0, 0.3f, -0.12f), V(0, 0.22f, -0.26f), 0.04f, 0.03f, purple);
        PokeBuilder.Both(s => b.Limb(tail, V(0, 0.22f, -0.26f), V(0.06f * s, 0.24f, -0.34f), 0.028f, 0.026f, purple));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.48f, 0.02f));
            var hand = V(0.22f * s, 0.32f, 0.12f);
            b.Limb(arm, V(0.13f * s, 0.48f, 0.02f), hand, 0.05f, 0.04f, purple);
            b.Ell(arm, hand, V(0.045f, 0.045f, 0.045f), purple);
            Digits(b, arm, hand + V(0, -0.02f, 0.03f), V(0, -0.5f, 1f), V(0.5f, 0, 0), 0.03f, 0.012f, White, Shell);
            var pipe = V(0.14f * s, 0.6f, -0.02f);
            b.Limb(arm, V(0.13f * s, 0.5f, 0.0f), pipe, 0.03f, 0.03f, purple);
            b.Torus(arm, pipe, 0.03f, 0.01f, yellow, V(0, 0, -20f * s), mat: Shell);
        });
        // A big head with a cavernous mouth, pipes along its crest and ears, red eyes
        int head = b.Head(V(0, 0.56f, 0.06f));
        var c = V(0, 0.6f, 0.11f);
        var r = V(0.12f, 0.1f, 0.12f);
        b.Ell(head, c, r, purple);
        Grin(b, head, On(c, r, 0, 0.56f), V(0.08f, 0.07f, 0.05f), Rgb(170, 80, 110));
        PokeBuilder.Both(s =>
        {
            b.Spike(head, On(c, r, 0.06f * s, 0.61f) + V(0, 0, 0.01f), On(c, r, 0.06f * s, 0.6f) + V(0, -0.03f, 0.015f), 0.012f, White, mat: Shell, blend: 0.004f);
            foreach (var (x, y, z) in new[] { (0.06f, 0.08f, -0.04f), (0.11f, 0.03f, -0.06f) })
            {
                var root = c + V(x * s, y, z);
                var tip = root + V(0.06f * s, 0.06f, -0.03f);
                b.Limb(head, root, tip, 0.022f, 0.022f, purple);
                b.Torus(head, tip, 0.022f, 0.008f, yellow, Euler(tip - root), mat: Shell);
            }
            var at = On(c, r, 0.07f * s, 0.65f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(220, 60, 80), glare: true);
        });
        b.Limb(head, c + V(0, 0.09f, -0.02f), c + V(0, 0.17f, -0.08f), 0.025f, 0.025f, purple);
        b.Torus(head, c + V(0, 0.17f, -0.08f), 0.025f, 0.009f, yellow, V(-35f, 0, 0), mat: Shell);
        b.Mark(head, On(c, r, 0, 0.615f) + V(0, 0, 0.001f), V(0, 0.1f, 1f), 0.03f, 0.012f, dark);
        return b;
    }

    // ------------------------------------------------------------------ Makuhita line

    /// <summary>Makuhita: a stout yellow sumo, dark below the belt, a black topknot and brown mitts for hands.</summary>
    private static PokeBuilder Makuhita()
    {
        var b = new PokeBuilder("Makuhita", 0.7f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 222, 110);
        var dark = Rgb(70, 66, 80);
        var mitt = Rgb(140, 104, 84);
        var pink = Rgb(236, 170, 170);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.14f, 0));
            b.Limb(leg, V(0.07f * s, 0.15f, 0), V(0.09f * s, 0.05f, 0.01f), 0.045f, 0.04f, dark);
            b.Ell(leg, V(0.09f * s, 0.03f, 0.03f), V(0.045f, 0.03f, 0.055f), yellow);
        });
        var bc = V(0, 0.28f, 0);
        var br = V(0.14f, 0.15f, 0.12f);
        b.Ell(Body, bc, br, yellow);
        b.PaintEll(Body, V(0, 0.15f, 0), V(0.16f, 0.08f, 0.14f), dark);
        foreach (var y in new[] { 0.3f, 0.22f })
            PokeBuilder.Both(s => b.Mark(Body, On(bc, br, 0.08f * s, y), V(0.5f * s, 0, 1f), 0.025f, 0.025f, pink, MarkShape.Ring));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.32f, 0));
            var hand = s > 0 ? V(0.18f * s, 0.46f, 0.06f) : V(0.22f * s, 0.24f, 0.07f);
            b.Limb(arm, V(0.12f * s, 0.32f, 0), hand, 0.04f, 0.035f, yellow);
            b.Ell(arm, hand, V(0.06f, 0.055f, 0.055f), mitt);
        });
        // A head grown into the body, a black topknot, a grim mouth
        int head = b.Head(V(0, 0.38f, 0.01f));
        var c = V(0, 0.42f, 0.03f);
        var r = V(0.09f, 0.075f, 0.085f);
        b.Ell(head, c, r, yellow);
        b.Ell(head, V(0, 0.5f, -0.02f), V(0.05f, 0.03f, 0.04f), dark, blend: 0.015f);
        b.Ell(head, V(0, 0.53f, -0.04f), V(0.025f, 0.03f, 0.03f), dark, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.46f, 0.0f));
            b.Ell(ear, V(0.09f * s, 0.47f, 0.0f), V(0.02f, 0.03f, 0.02f), yellow, blend: 0.01f);
            var at = On(c, r, 0.035f * s, 0.43f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 36, 40), closed: true);
        });
        b.Mark(head, On(c, r, 0, 0.39f), V(0, -0.2f, 1f), 0.025f, 0.008f, Rgb(120, 80, 60), MarkShape.Bar);
        return b;
    }

    /// <summary>Hariyama: a huge sumo, pale with a yellow-fringed apron, dark legs and belt, a topknot and great orange palms.</summary>
    private static PokeBuilder Hariyama()
    {
        var b = new PokeBuilder("Hariyama", 1f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Fur };
        var pale = Rgb(234, 226, 214);
        var navy = Rgb(60, 66, 100);
        var yellow = Rgb(244, 210, 92);
        var palm = Rgb(236, 140, 104);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.2f, 0));
            b.Limb(leg, V(0.1f * s, 0.22f, 0), V(0.14f * s, 0.06f, 0.01f), 0.07f, 0.055f, navy);
            b.Ell(leg, V(0.14f * s, 0.035f, 0.03f), V(0.06f, 0.035f, 0.07f), pale);
        });
        var bc = V(0, 0.38f, 0);
        var br = V(0.18f, 0.2f, 0.15f);
        b.Ell(Body, bc, br, pale);
        b.PaintTorus(Body, V(0, 0.27f, 0), 0.17f, 0.035f, navy, sz: 0.85f);
        // A yellow apron hanging from the belt, fringed
        for (int i = 0; i < 5; i++)
        {
            float x = -0.08f + 0.04f * i;
            Blade(b, Body, V(x, 0.26f, 0.13f), V(x * 1.2f, 0.12f, 0.15f), 0.035f, yellow, V(0, 0, 1f), 0.25f);
        }
        // Great arms, one thrust out, with huge orange palms
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, 0.46f, 0));
            var hand = s > 0 ? V(0.34f * s, 0.62f, 0.1f) : V(0.3f * s, 0.34f, 0.12f);
            b.Limb(arm, V(0.16f * s, 0.46f, 0), hand, 0.06f, 0.05f, pale);
            b.Ell(arm, hand + V(0.03f * s, 0.02f, 0.02f), V(0.08f, 0.1f, 0.03f), palm, V(0, 20f * s, -20f * s), blend: 0.015f);
            for (int f = 0; f < 3; f++)
            {
                var root = hand + V(0.03f * s, 0.08f, 0.02f) + V(0.03f * s * (f - 1), 0, 0);
                b.Limb(arm, root, root + V(0.015f * s * (f - 1), 0.06f, 0), 0.022f, 0.02f, palm, blend: 0.01f);
            }
        });
        // A head sunk in its shoulders, a topknot over a navy brow and a stern mouth
        int head = b.Head(V(0, 0.54f, 0.04f));
        var c = V(0, 0.6f, 0.06f);
        var r = V(0.09f, 0.08f, 0.08f);
        b.Ell(head, c, r, pale);
        b.PaintEll(head, c + V(0, 0.06f, 0.0f), V(0.1f, 0.05f, 0.09f), navy);
        b.Ell(head, V(0, 0.69f, 0.03f), V(0.045f, 0.03f, 0.05f), navy, blend: 0.015f);
        b.Ell(head, V(0, 0.71f, -0.01f), V(0.03f, 0.03f, 0.03f), navy, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.035f * s, 0.6f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 36, 40), glare: true);
        });
        b.Mark(head, On(c, r, 0, 0.56f), V(0, -0.2f, 1f), 0.03f, 0.008f, Rgb(120, 80, 60), MarkShape.Bar);
        return b;
    }

    // ------------------------------------------------------------------ Skitty line

    /// <summary>Skitty: a pink kitten with a cream face and belly, big ears purple inside, and a tail ending in a pink bulb with three beads.</summary>
    private static PokeBuilder Skitty()
    {
        var b = new PokeBuilder("Skitty", 0.5f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Fur };
        var pink = Rgb(244, 168, 176);
        var cream = Rgb(250, 238, 196);
        var purple = Rgb(146, 112, 186);
        StubbyLegs(b, 0.05f, 0.1f, 0.05f, -0.07f, 0.03f, cream);
        var bc = V(0, 0.13f, -0.01f);
        var br = V(0.08f, 0.075f, 0.1f);
        b.Ell(Body, bc, br, pink);
        b.PaintEll(Body, V(0, 0.1f, 0.03f), V(0.06f, 0.05f, 0.08f), cream);
        // A thin tail curving up to a pink bulb with three beads on stalks
        int tail = b.Tail(V(0, 0.14f, -0.1f));
        var tp = Smooth(3, V(0, 0.14f, -0.1f), V(0, 0.2f, -0.17f), V(0, 0.3f, -0.18f), V(0, 0.36f, -0.12f));
        b.Tube(tail, tp, 0.01f, 0.009f, cream, blend: 0f);
        var bulb = tp[^1] + V(0, 0.06f, 0.0f);
        b.Ell(tail, bulb, V(0.026f, 0.055f, 0.026f), pink, blend: 0.01f);
        foreach (var x in new[] { -0.025f, 0f, 0.025f })
        {
            var top = bulb + V(x, 0.11f, 0);
            b.Limb(tail, bulb + V(x * 0.4f, 0.05f, 0), top, 0.005f, 0.004f, Rgb(232, 200, 150));
            b.Ell(tail, top, V(0.012f, 0.012f, 0.012f), cream, blend: 0.004f);
        }
        // A big round head, cream below and pink above, the eyes shut in a smile
        int head = b.Head(V(0, 0.18f, 0.06f));
        var c = V(0, 0.23f, 0.09f);
        var r = V(0.1f, 0.085f, 0.085f);
        b.Ell(head, c, r, pink);
        b.PaintEll(head, c + V(0, -0.035f, 0.04f), V(0.08f, 0.045f, 0.06f), cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.29f, 0.08f));
            CatEar(b, ear, V(0.06f * s, 0.28f, 0.08f), V(0.12f * s, 0.38f, 0.06f), 0.04f, pink, purple);
            var at = On(c, r, 0.04f * s, 0.24f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(70, 50, 60), closed: true);
        });
        b.Mark(head, On(c, r, 0, 0.2f), V(0, -0.2f, 1f), 0.016f, 0.006f, Rgb(140, 90, 90), MarkShape.Smile);
        return b;
    }

    /// <summary>Delcatty: an elegant cream cat, a purple ruff round its neck with beads, a purple cap with swept points and a beaded tail.</summary>
    private static PokeBuilder Delcatty()
    {
        var b = new PokeBuilder("Delcatty", 0.75f, BodyPlan.Quadruped, V(0, 0.28f, 0)) { Coat = Fur };
        var cream = Rgb(248, 230, 176);
        var purple = Rgb(160, 110, 186);
        foreach (var (z, front) in new[] { (0.08f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.045f * s, 0.24f, z), front);
                b.Limb(leg, V(0.045f * s, 0.26f, z), V(0.04f * s, 0.03f, z + 0.01f), 0.03f, 0.016f, cream);
                b.Ell(leg, V(0.04f * s, 0.016f, z + 0.015f), V(0.018f, 0.016f, 0.025f), cream);
            });
        var bc = V(0, 0.28f, -0.01f);
        var br = V(0.07f, 0.07f, 0.12f);
        b.Ell(Body, bc, br, cream);
        // A purple ruff round its neck, beads at its points
        int ruff = b.Part("ruff", Body, V(0, 0.33f, 0.07f), PokeRole.Fin);
        b.Ell(ruff, V(0, 0.33f, 0.09f), V(0.1f, 0.06f, 0.07f), purple, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            var tip = V(0.16f * s, 0.31f, 0.1f);
            b.Limb(ruff, V(0.08f * s, 0.32f, 0.1f), tip, 0.006f, 0.005f, purple);
            b.Ell(ruff, tip, V(0.016f, 0.016f, 0.016f), purple, blend: 0.004f);
        });
        // A long thin tail ending in a purple leaf and beads
        int tail = b.Tail(V(0, 0.3f, -0.12f));
        var tp = Smooth(3, V(0, 0.3f, -0.12f), V(0, 0.38f, -0.2f), V(0, 0.48f, -0.2f), V(0, 0.52f, -0.14f));
        b.Tube(tail, tp, 0.012f, 0.008f, cream, blend: 0f);
        b.Ell(tail, tp[^1] + V(0, 0.02f, 0.01f), V(0.03f, 0.04f, 0.025f), purple, blend: 0.008f);
        // A small cream face under a purple cap whose points sweep out and up
        int head = b.Head(V(0, 0.38f, 0.1f));
        var c = V(0, 0.44f, 0.12f);
        var r = V(0.07f, 0.065f, 0.065f);
        b.Ell(head, c, r, cream);
        b.Ell(head, c + V(0, 0.04f, -0.015f), V(0.09f, 0.05f, 0.065f), purple, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.48f, 0.1f));
            Blade(b, ear, c + V(0.06f * s, 0.05f, -0.01f), c + V(0.19f * s, 0.12f, -0.03f), 0.05f, purple, V(0, 0.2f, 1f), 0.25f);
            var at = On(c, r, 0.03f * s, 0.43f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(40, 34, 40));
        });
        b.Mark(head, On(c, r, 0, 0.405f), V(0, -0.2f, 1f), 0.012f, 0.005f, Rgb(120, 90, 80), MarkShape.Smile);
        return b;
    }
}
