using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Paldea's first batch in National Pokédex
// order: Sprigatito (906) to Pawmot (923). Their forms are in PokemonModels.Regional.cs with the other forms. Helpers
// shared with the earlier batches are in the files of those batches, PokemonModels.Sinnoh1.cs to PokemonModels.Hisui.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Sprigatito line

    private static readonly Color CatLeafLight = Rgb(176, 216, 146);
    private static readonly Color CatLeafDark = Rgb(58, 128, 62);
    private static readonly Color CatLeafPale = Rgb(228, 242, 212);
    private static readonly Color CatLeafPink = Rgb(226, 104, 134);

    /// <summary>The leaf-shaped mask of the Sprigatito line: dark green over the brow and between the eyes, coming to a point at the nose.</summary>
    private static void LeafMask(PokeBuilder b, int head, Vector3 c, Vector3 r, Color dark)
    {
        b.PaintEll(head, c + V(0, r.Y * 0.6f, r.Z * 0.5f), V(r.X * 0.8f, r.Y * 0.4f, r.Z * 0.65f), dark);
        b.PaintEll(head, c + V(0, r.Y * 0.15f, r.Z * 0.9f), V(r.X * 0.18f, r.Y * 0.45f, r.Z * 0.3f), dark, soft: 0.006f);
    }

    /// <summary>Sprigatito: a little green cat of grass, pale below, a dark leaf-shaped mask over its brow, big dark ears, great pink eyes, fluffy cheeks, a collar of dark leaves and a fluffy tail held up.</summary>
    private static PokeBuilder Sprigatito()
    {
        var b = new PokeBuilder("Sprigatito", 0.45f, BodyPlan.Quadruped, V(0, 0.15f, -0.01f)) { Coat = Fur };
        foreach (var (z, front) in new[] { (0.05f, true), (-0.07f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.045f * s, 0.12f, z), front);
                b.Limb(leg, V(0.045f * s, 0.12f, z), V(0.05f * s, 0.03f, z + 0.01f), 0.026f, 0.024f, CatLeafLight);
                b.Ell(leg, V(0.05f * s, 0.02f, z + 0.02f), V(0.026f, 0.02f, 0.03f), CatLeafPale);
            });
        var bc = V(0, 0.15f, -0.01f);
        b.Ell(Body, bc, V(0.07f, 0.065f, 0.1f), CatLeafLight);
        b.PaintEll(Body, bc + V(0, -0.04f, 0.04f), V(0.05f, 0.04f, 0.07f), CatLeafPale);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            Frond(b, Body, bc + V(x, 0.05f, 0.07f), bc + V(x * 1.5f, 0.0f, 0.11f), 0.022f, CatLeafDark, V(0, 0.3f, 1f), 0.25f, Leaf);
        int tail = b.Tail(bc + V(0, 0.03f, -0.09f));
        var tp = Smooth(3, bc + V(0, 0.03f, -0.09f), bc + V(0, 0.08f, -0.15f), bc + V(0, 0.15f, -0.16f));
        b.Tube(tail, tp, 0.02f, 0.025f, CatLeafLight, blend: 0f);
        b.Ell(tail, tp[^1] + V(0, 0.02f, 0), V(0.03f, 0.035f, 0.03f), CatLeafLight);
        int head = b.Head(bc + V(0, 0.08f, 0.05f));
        var c = bc + V(0, 0.13f, 0.06f);
        var r = V(0.1f, 0.085f, 0.085f);
        b.Ell(head, c, r, CatLeafLight);
        LeafMask(b, head, c, r, CatLeafDark);
        b.Ell(head, c + V(0, -0.035f, 0.065f), V(0.04f, 0.028f, 0.03f), CatLeafPale, blend: 0.015f);
        b.Ell(head, c + V(0, -0.02f, 0.09f), V(0.009f, 0.007f, 0.006f), CatLeafPink, blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.08f * s, -0.02f, 0.02f), c + V(0.13f * s, -0.04f, 0.02f), 0.025f, CatLeafLight, 0.5f);
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.06f, -0.01f));
            CatEar(b, ear, c + V(0.06f * s, 0.05f, -0.01f), c + V(0.11f * s, 0.15f, -0.02f), 0.045f, CatLeafDark, CatLeafLight);
            var at = On(c, r, 0.042f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.022f, CatLeafPink);
        });
        return b;
    }

    /// <summary>Floragato: a cat of grass standing on its dark green legs, pale green above, a dark leaf mask on its brow and leaf marks down its chest, its arms dark from the elbow, and in one paw a vine ending in a pink bud.</summary>
    private static PokeBuilder Floragato()
    {
        var b = new PokeBuilder("Floragato", 0.7f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.24f, 0));
            b.Limb(leg, V(0.05f * s, 0.25f, 0), V(0.06f * s, 0.05f, 0.02f), 0.038f, 0.03f, CatLeafDark);
            b.Ell(leg, V(0.06f * s, 0.03f, 0.04f), V(0.032f, 0.03f, 0.045f), CatLeafDark);
        });
        var bc = V(0, 0.32f, 0);
        b.Ell(Body, bc, V(0.07f, 0.1f, 0.06f), CatLeafLight);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.05f), V(0.03f, 0.07f, 0.03f), CatLeafDark, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, bc + V(0.04f * s, -0.02f, 0.05f), V(0.02f, 0.04f, 0.02f), CatLeafDark, V(0, 0, 30f * s), 0.006f);
            Frond(b, Body, bc + V(0.06f * s, 0.07f, -0.01f), bc + V(0.12f * s, -0.01f, -0.04f), 0.035f, CatLeafLight, V(s, 0.2f, -0.3f), 0.25f, Leaf);
            int arm = b.Arm(s, bc + V(0.065f * s, 0.06f, 0.01f));
            var elbow = bc + V(0.11f * s, 0.0f, 0.04f);
            var hand = s > 0 ? bc + V(0.12f * s, 0.06f, 0.1f) : bc + V(0.11f * s, -0.07f, 0.06f);
            b.Limb(arm, bc + V(0.06f * s, 0.06f, 0.01f), elbow, 0.025f, 0.022f, CatLeafLight);
            b.Limb(arm, elbow, hand, 0.022f, 0.024f, CatLeafDark);
            b.Ell(arm, hand, V(0.026f, 0.026f, 0.026f), CatLeafDark);
            if (s > 0)
            {
                // A vine from its paw to a pink bud
                var vine = Smooth(3, hand + V(0, 0.015f, 0.01f), hand + V(0.03f, 0.06f, 0.03f), hand + V(0.0f, 0.1f, 0.05f));
                b.Tube(arm, vine, 0.006f, 0.005f, CatLeafDark, Leaf, 0.003f);
                b.Ell(arm, vine[^1] + V(0, 0.02f, 0), V(0.025f, 0.03f, 0.025f), CatLeafPink);
            }
        });
        int tail = b.Tail(bc + V(0, -0.07f, -0.05f));
        var tp = Smooth(3, bc + V(0, -0.07f, -0.05f), bc + V(-0.03f, -0.1f, -0.15f), bc + V(-0.06f, 0.0f, -0.2f));
        b.Tube(tail, tp, 0.022f, 0.028f, CatLeafLight, blend: 0f);
        b.Ell(tail, tp[^1] + V(0, 0.02f, 0), V(0.03f, 0.04f, 0.03f), CatLeafDark);
        int head = b.Head(bc + V(0, 0.1f, 0.01f));
        var c = bc + V(0, 0.18f, 0.02f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Ell(head, c, r, CatLeafLight);
        LeafMask(b, head, c, r, CatLeafDark);
        b.Ell(head, c + V(0, -0.03f, 0.055f), V(0.03f, 0.022f, 0.025f), CatLeafPale, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.06f * s, -0.02f, 0.0f), c + V(0.1f * s, -0.045f, -0.01f), 0.022f, CatLeafLight, 0.5f);
            int ear = b.Ear(head, s, c + V(0.045f * s, 0.05f, -0.01f));
            CatEar(b, ear, c + V(0.045f * s, 0.04f, -0.01f), c + V(0.09f * s, 0.13f, -0.02f), 0.035f, CatLeafLight, CatLeafDark);
            var at = On(c, r, 0.032f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(200, 50, 90), glare: true);
        });
        return b;
    }

    /// <summary>Meowscarada: a slender magician of a cat, its legs dark green, its body and face white, a great dark cape of leaves falling from its shoulders, a ruff of pink petals at its throat, a dark green hood drawn up into two long points with a dark mask over its red eyes, and a flower-bud bomb held up in one paw.</summary>
    private static PokeBuilder Meowscarada()
    {
        var b = new PokeBuilder("Meowscarada", 0.95f, BodyPlan.Biped, V(0, 0.55f, 0)) { Coat = Fur };
        var white = Rgb(236, 240, 228);
        var dark = Rgb(40, 92, 54);
        var green = Rgb(76, 152, 84);
        var pink = Rgb(228, 84, 124);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.43f, 0));
            var knee = V(0.055f * s, 0.23f, 0.03f);
            b.Limb(leg, V(0.045f * s, 0.44f, 0), knee, 0.035f, 0.024f, dark);
            b.Limb(leg, knee, V(0.055f * s, 0.05f, 0.0f), 0.024f, 0.018f, dark);
            b.Spike(leg, V(0.055f * s, 0.035f, 0.0f), V(0.055f * s, 0.01f, 0.06f), 0.022f, dark, 0.6f);
        });
        var bc = V(0, 0.55f, 0);
        b.Ell(Body, bc, V(0.07f, 0.12f, 0.055f), white);
        b.PaintTorus(Body, bc + V(0, -0.08f, 0), 0.065f, 0.018f, dark);
        // The cape of leaves from its shoulders
        PokeBuilder.Both(s =>
        {
            Frond(b, Body, bc + V(0.06f * s, 0.09f, -0.03f), bc + V(0.2f * s, -0.2f, -0.08f), 0.08f, dark, V(s * 0.6f, 0, -1f), 0.15f, Leaf);
            Frond(b, Body, bc + V(0.04f * s, 0.08f, -0.05f), bc + V(0.1f * s, -0.26f, -0.12f), 0.07f, green, V(s * 0.3f, 0, -1f), 0.15f, Leaf);
            foreach (float k in new[] { 0f, 1f })
                Frond(b, Body, bc + V(0.015f * s, 0.11f, 0.04f), bc + V((0.07f + 0.02f * k) * s, 0.06f + 0.04f * k, 0.09f), 0.03f, pink, V(0, 0.3f, 1f), 0.25f, Leaf);
            int arm = b.Arm(s, bc + V(0.06f * s, 0.08f, 0));
            var elbow = bc + V(0.12f * s, s > 0 ? 0.12f : -0.02f, 0.03f);
            var hand = s > 0 ? bc + V(0.15f * s, 0.24f, 0.05f) : bc + V(0.14f * s, -0.12f, 0.05f);
            b.Limb(arm, bc + V(0.055f * s, 0.08f, 0), elbow, 0.02f, 0.018f, white);
            b.Limb(arm, elbow, hand, 0.018f, 0.018f, white);
            b.Ell(arm, hand, V(0.022f, 0.022f, 0.022f), dark);
            if (s > 0)
            {
                var bomb = hand + V(0.0f, 0.05f, 0.01f);
                b.Ell(arm, bomb, V(0.03f, 0.03f, 0.03f), green, mat: Leaf);
                foreach (float a in new[] { 0f, 1.6f, 3.2f, 4.8f })
                    b.Ell(arm, bomb + V(MathF.Cos(a) * 0.015f, 0.03f, MathF.Sin(a) * 0.015f), V(0.012f, 0.008f, 0.012f), pink, blend: 0.004f);
            }
        });
        // The hooded head, its two long points, the mask
        int head = b.Head(bc + V(0, 0.13f, 0.01f));
        var c = bc + V(0, 0.21f, 0.02f);
        var r = V(0.065f, 0.06f, 0.06f);
        b.Limb(head, bc + V(0, 0.1f, 0), c + V(0, -0.04f, -0.01f), 0.025f, 0.025f, white);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, 0.02f, -0.02f), V(0.07f, 0.06f, 0.06f), dark, blend: 0.01f);
        b.Ell(head, c + V(0, -0.025f, 0.045f), V(0.03f, 0.02f, 0.022f), white, blend: 0.012f);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.035f * s, 0.05f, -0.03f), c + V(0.1f * s, 0.2f, -0.09f), 0.035f, dark, 0.45f));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.026f * s, c.Y + 0.002f);
            b.PaintEll(head, at, V(0.026f, 0.014f, 0.02f), dark, soft: 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(214, 46, 80), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Fuecoco line

    private static readonly Color CrocRed = Rgb(222, 86, 58);
    private static readonly Color CrocCream = Rgb(250, 238, 214);
    private static readonly Color CrocYellow = Rgb(250, 214, 92);
    private static readonly Color CrocClaw = Rgb(82, 82, 96);

    /// <summary>Two dark claws at the front of a foot.</summary>
    private static void CrocClaws(PokeBuilder b, int bone, Vector3 at, float r)
    {
        foreach (float t in new[] { -1f, 1f })
            b.Spike(bone, at + V(t * r, 0, 0), at + V(t * r * 1.2f, -r * 0.5f, r * 1.4f), r * 0.6f, CrocClaw, mat: Shell, blend: 0.004f);
    }

    /// <summary>Fuecoco: a little red crocodile that walks upright, its great round head cream, its snout broad and its mouth open, a tuft of yellow flame on its crown, a cream belly with a yellow mark, stubby arms and a short tail.</summary>
    private static PokeBuilder Fuecoco()
    {
        var b = new PokeBuilder("Fuecoco", 0.45f, BodyPlan.Biped, V(0, 0.13f, 0)) { Coat = Scales };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.08f, 0));
            b.Limb(leg, V(0.06f * s, 0.09f, 0), V(0.065f * s, 0.035f, 0.01f), 0.035f, 0.032f, CrocRed);
            b.Ell(leg, V(0.065f * s, 0.025f, 0.03f), V(0.035f, 0.025f, 0.045f), CrocRed);
            CrocClaws(b, leg, V(0.065f * s, 0.02f, 0.07f), 0.012f);
        });
        var bc = V(0, 0.13f, 0);
        b.Ell(Body, bc, V(0.09f, 0.085f, 0.08f), CrocRed);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.06f), V(0.06f, 0.06f, 0.04f), CrocCream);
        b.PaintEll(Body, bc + V(0.02f, -0.03f, 0.08f), V(0.018f, 0.014f, 0.02f), CrocYellow, soft: 0.005f);
        int tail = b.Tail(bc + V(0, -0.04f, -0.06f));
        b.Tube(tail, Smooth(3, bc + V(0, -0.04f, -0.06f), bc + V(0, -0.09f, -0.13f), bc + V(0, -0.1f, -0.2f)), 0.04f, 0.012f, CrocRed, blend: 0f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.08f * s, 0.03f, 0.02f));
            b.Limb(arm, bc + V(0.07f * s, 0.03f, 0.02f), bc + V(0.11f * s, 0.0f, 0.06f), 0.022f, 0.02f, CrocRed);
        });
        int head = b.Head(bc + V(0, 0.08f, 0.02f));
        var c = bc + V(0, 0.15f, 0.04f);
        var r = V(0.11f, 0.085f, 0.1f);
        b.Ell(head, c, r, CrocCream);
        var sc = c + V(0, -0.015f, 0.07f);
        b.Ell(head, sc, V(0.09f, 0.055f, 0.075f), CrocCream, blend: 0.02f);
        Gape(b, head, sc + V(0, -0.03f, 0.04f), V(0.065f, 0.025f, 0.04f), Rgb(222, 120, 120), 0.012f);
        PokeBuilder.Both(s => b.PaintEll(head, sc + V(0.025f * s, 0.035f, 0.06f), V(0.008f, 0.006f, 0.008f), Rgb(70, 60, 60), soft: 0.003f));
        // The tuft of flame on its crown
        foreach (var (x, z, h) in new[] { (0f, 0f, 0.12f), (-0.02f, -0.02f, 0.08f), (0.02f, -0.015f, 0.09f) })
            Blade(b, head, c + V(x, 0.07f, z), c + V(x * 2f, 0.07f + h, z - 0.02f), 0.025f, CrocYellow, V(1f, 0, 0.2f), 0.3f, Glow);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.065f * s, c.Y + 0.03f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(50, 40, 40));
        });
        return b;
    }

    /// <summary>Crocalor: a red crocodile grown longer, a yellow stripe down its back and tail, its cream head grinning with teeth, and on its crown a cap of fire set with yellow blocks of flame and a yellow egg of fire on top.</summary>
    private static PokeBuilder Crocalor()
    {
        var b = new PokeBuilder("Crocalor", 0.7f, BodyPlan.Biped, V(0, 0.22f, -0.02f)) { Coat = Scales };
        var red = Rgb(196, 64, 54);
        var orange = Rgb(244, 128, 50);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.15f, -0.02f));
            b.Limb(leg, V(0.07f * s, 0.16f, -0.02f), V(0.08f * s, 0.04f, 0.0f), 0.045f, 0.04f, red);
            b.Ell(leg, V(0.08f * s, 0.03f, 0.03f), V(0.045f, 0.03f, 0.055f), red);
            CrocClaws(b, leg, V(0.08f * s, 0.025f, 0.08f), 0.014f);
        });
        var bc = V(0, 0.22f, -0.02f);
        b.Ell(Body, bc, V(0.1f, 0.1f, 0.13f), red, V(-15f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.03f, 0.08f), V(0.07f, 0.08f, 0.06f), CrocCream);
        b.PaintEll(Body, bc + V(0, 0.09f, -0.03f), V(0.03f, 0.04f, 0.12f), CrocYellow, soft: 0.008f);
        int tail = b.Tail(bc + V(0, -0.03f, -0.12f));
        var tp = Smooth(3, bc + V(0, -0.03f, -0.12f), bc + V(0, -0.12f, -0.24f), bc + V(0, -0.17f, -0.36f));
        b.Tube(tail, tp, 0.05f, 0.015f, red, blend: 0f);
        b.PaintEll(tail, tp[tp.Length / 3] + V(0, 0.04f, 0), V(0.02f, 0.025f, 0.08f), CrocYellow, Euler(tp[tp.Length / 3 + 1] - tp[tp.Length / 3]), 0.008f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.08f * s, 0.05f, 0.06f));
            var hand = bc + V(0.13f * s, -0.01f, 0.12f);
            b.Limb(arm, bc + V(0.07f * s, 0.05f, 0.06f), hand, 0.024f, 0.02f, red);
            Claws(b, arm, hand + V(0, -0.01f, 0.015f), V(0.008f, 0, 0), V(0, -0.6f, 0.8f), 0.02f, 0.007f);
        });
        int head = b.Head(bc + V(0, 0.1f, 0.08f));
        var c = bc + V(0, 0.15f, 0.12f);
        var r = V(0.1f, 0.07f, 0.1f);
        b.Ell(head, c, r, CrocCream);
        b.Ell(head, c + V(0, -0.02f, 0.08f), V(0.08f, 0.045f, 0.07f), CrocCream, blend: 0.02f);
        Grin(b, head, c + V(0, -0.035f, 0.13f), V(0.06f, 0.016f, 0.03f), Rgb(170, 60, 60));
        for (int i = -2; i <= 2; i++)
            b.Spike(head, c + V(i * 0.022f, -0.025f, 0.135f - MathF.Abs(i) * 0.01f), c + V(i * 0.022f, -0.045f, 0.14f - MathF.Abs(i) * 0.01f), 0.007f, White, mat: Shell, blend: 0.003f);
        // The cap of fire, blocks of yellow flame and the egg on top
        var cap = c + V(0, 0.06f, -0.02f);
        b.Ell(head, cap, V(0.1f, 0.05f, 0.09f), orange, mat: Glow, blend: 0.01f);
        foreach (var (x, z) in new[] { (-0.05f, 0.03f), (0.04f, 0.04f), (0.0f, -0.03f), (-0.06f, -0.04f), (0.06f, -0.03f) })
            b.Box(head, cap + V(x, 0.035f, z), V(0.02f, 0.015f, 0.02f), 0.005f, CrocYellow, V(0, 30f * x * 20f, 0), Glow, 0.004f);
        b.Ell(head, cap + V(0, 0.07f, 0), V(0.035f, 0.045f, 0.035f), CrocYellow, mat: Glow);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.06f * s, c.Y + 0.025f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(50, 40, 40), glare: true);
        });
        return b;
    }

    /// <summary>Skeledirge: a great red crocodile of fire and ghosts, low on four legs, a cream belly banded dark, a long tail with a pale wisp at its end, its long white head painted like a skull with purple teeth along its jaw, flames on its crown and a small bird of flame resting on its snout.</summary>
    private static PokeBuilder Skeledirge()
    {
        var b = new PokeBuilder("Skeledirge", 1f, BodyPlan.Quadruped, V(0, 0.24f, -0.06f)) { Coat = Scales };
        var red = Rgb(182, 56, 46);
        var dark = Rgb(110, 34, 36);
        var bone = Rgb(240, 234, 226);
        var purple = Rgb(108, 50, 112);
        var orange = Rgb(244, 132, 42);
        foreach (var (z, front) in new[] { (0.08f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.12f * s, 0.2f, z), front);
                var knee = V(0.19f * s, 0.15f, z + 0.02f);
                b.Limb(leg, V(0.11f * s, 0.21f, z), knee, 0.05f, 0.04f, red);
                b.Limb(leg, knee, V(0.2f * s, 0.04f, z + 0.03f), 0.04f, 0.035f, red);
                b.Ell(leg, V(0.2f * s, 0.025f, z + 0.06f), V(0.04f, 0.025f, 0.055f), red);
                CrocClaws(b, leg, V(0.2f * s, 0.02f, z + 0.1f), 0.014f);
            });
        var bc = V(0, 0.24f, -0.06f);
        var br = V(0.15f, 0.11f, 0.27f);
        b.Ell(Body, bc, br, red);
        b.PaintEll(Body, bc + V(0, -0.07f, 0.02f), V(0.12f, 0.06f, 0.24f), CrocCream);
        for (int i = -2; i <= 2; i++)
            b.PaintEll(Body, bc + V(0, -0.09f, i * 0.08f), V(0.13f, 0.03f, 0.012f), dark, soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            foreach (float z in new[] { -0.12f, 0.0f, 0.12f })
                b.PaintEll(Body, bc + V(0.13f * s, 0.02f, z), V(0.03f, 0.06f, 0.012f), bone, V(-15f, 0, 0), 0.006f);
        });
        int tail = b.Tail(bc + V(0, 0, -0.26f));
        var tp = Smooth(3, bc + V(0, 0, -0.26f), bc + V(0.04f, -0.06f, -0.4f), bc + V(0.1f, -0.1f, -0.52f), bc + V(0.18f, -0.07f, -0.58f));
        b.Tube(tail, tp, 0.07f, 0.02f, red, blend: 0f);
        b.Ell(tail, tp[^1], V(0.035f, 0.04f, 0.04f), bone, mat: Glow);
        // The long white head painted like a skull
        int head = b.Head(bc + V(0, 0.04f, 0.24f));
        var c = bc + V(0, 0.1f, 0.36f);
        var r = V(0.11f, 0.085f, 0.15f);
        b.Limb(head, bc + V(0, 0.03f, 0.22f), c + V(0, -0.02f, -0.08f), 0.09f, 0.08f, red);
        b.Ell(head, c, r, bone);
        b.Ell(head, c + V(0, -0.06f, 0.03f), V(0.095f, 0.035f, 0.13f), bone, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 5; i++)
            {
                var at = Out(c, r, default, V(s, -0.25f, 0.9f - i * 0.35f));
                b.PaintEll(head, at, V(0.02f, 0.018f, 0.01f), purple, V(45f * (i % 2 == 0 ? 1f : -1f), 0, 0), 0.004f);
            }
            var eye = Out(c, r, default, V(0.75f * s, 0.45f, 0.35f));
            b.PaintEll(head, eye, V(0.035f, 0.025f, 0.03f), purple, soft: 0.006f);
            b.Eye(head, eye, Outward(c, r, eye), 0.014f, Rgb(250, 210, 80), glare: true);
        });
        // Flames on its crown, a small bird of flame on its snout
        foreach (var (x, z, k) in new[] { (0f, -0.06f, 1f), (-0.06f, -0.08f, 0.8f), (0.06f, -0.08f, 0.8f), (0f, -0.12f, 0.7f) })
        {
            b.Ell(head, c + V(x, 0.07f, z), V(0.045f * k, 0.04f * k, 0.045f * k), orange, mat: Glow, blend: 0.015f);
            b.Ell(head, c + V(x, 0.1f * k + 0.02f, z - 0.01f), V(0.025f * k, 0.03f * k, 0.025f * k), CrocYellow, mat: Glow, blend: 0.012f);
        }
        var bird = c + V(0, 0.07f, 0.11f);
        b.Ell(head, bird, V(0.03f, 0.03f, 0.03f), orange, mat: Glow, blend: 0.01f);
        b.Spike(head, bird + V(0, 0.02f, -0.01f), bird + V(0, 0.07f, -0.03f), 0.015f, CrocYellow, 0.6f, Glow);
        b.PaintEll(head, bird + V(0, 0.005f, 0.025f), V(0.015f, 0.012f, 0.008f), CrocYellow, soft: 0.004f);
        return b;
    }

    // ------------------------------------------------------------------ Quaxly line

    private static readonly Color DuckWhite = Rgb(246, 246, 250);
    private static readonly Color DuckBeak = Rgb(244, 204, 70);

    /// <summary>Quaxly: a white duckling with a great rounded cap of sky-blue hair waved white across its front, a broad yellow beak, blue eyes, small white wings and webbed blue feet.</summary>
    private static PokeBuilder Quaxly()
    {
        var b = new PokeBuilder("Quaxly", 0.45f, BodyPlan.Biped, V(0, 0.14f, 0)) { Coat = Fur };
        var blue = Rgb(72, 190, 222);
        var feet = Rgb(100, 204, 226);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.08f, 0));
            b.Limb(leg, V(0.035f * s, 0.08f, 0), V(0.04f * s, 0.03f, 0.01f), 0.02f, 0.016f, DuckWhite);
            b.Ell(leg, V(0.045f * s, 0.015f, 0.04f), V(0.035f, 0.015f, 0.045f), feet);
        });
        var bc = V(0, 0.14f, 0);
        b.Ell(Body, bc, V(0.08f, 0.09f, 0.075f), DuckWhite);
        b.Ell(Body, bc + V(0, -0.02f, -0.07f), V(0.03f, 0.025f, 0.03f), DuckWhite);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.07f * s, 0.02f, 0));
            Frond(b, wing, bc + V(0.07f * s, 0.03f, 0), bc + V(0.12f * s, -0.04f, 0.02f), 0.03f, DuckWhite, V(s, 0, 0.2f), 0.3f);
        });
        int head = b.Head(bc + V(0, 0.09f, 0.01f));
        var c = bc + V(0, 0.15f, 0.02f);
        var r = V(0.08f, 0.075f, 0.075f);
        b.Ell(head, c, r, DuckWhite);
        var hc = c + V(0, 0.045f, -0.03f);
        var hr = V(0.094f, 0.062f, 0.088f);
        b.Ell(head, hc, hr, blue, blend: 0.01f);
        var wave = Out(hc, hr, default, V(0, 0.35f, 1f));
        b.Mark(head, wave, Outward(hc, hr, wave), 0.04f, 0.012f, DuckWhite, MarkShape.Wave);
        b.Ell(head, c + V(0, -0.025f, 0.075f), V(0.045f, 0.016f, 0.04f), DuckBeak, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.033f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(40, 110, 190));
        });
        return b;
    }

    /// <summary>Quaxwell: a young dancer of a duck, white below like a leotard and blue above, a swept crest of dark blue hair, blue wings fringed with feathers, a ruffle of blue feathers at its hips, and yellow feet banded blue at the ankle.</summary>
    private static PokeBuilder Quaxwell()
    {
        var b = new PokeBuilder("Quaxwell", 0.75f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Fur };
        var blue = Rgb(64, 136, 214);
        var deep = Rgb(36, 82, 172);
        var leg = Rgb(160, 176, 160);
        PokeBuilder.Both(s =>
        {
            int l = b.Leg(s, V(0.04f * s, 0.24f, 0));
            b.Limb(l, V(0.04f * s, 0.25f, 0), V(0.05f * s, 0.05f, 0.01f), 0.024f, 0.018f, leg);
            b.PaintTorus(l, V(0.05f * s, 0.07f, 0.01f), 0.019f, 0.006f, deep);
            b.Ell(l, V(0.055f * s, 0.018f, 0.04f), V(0.035f, 0.018f, 0.05f), DuckBeak);
        });
        var bc = V(0, 0.32f, 0);
        b.Ell(Body, bc, V(0.075f, 0.1f, 0.065f), DuckWhite);
        b.PaintEll(Body, bc + V(0, 0.09f, 0), V(0.08f, 0.05f, 0.07f), blue);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            var root = bc + V(MathF.Sin(a) * 0.065f, -0.06f, MathF.Cos(a) * 0.055f);
            Frond(b, Body, root, root + V(MathF.Sin(a) * 0.04f, -0.06f, MathF.Cos(a) * 0.035f), 0.022f, i % 2 == 0 ? blue : deep, V(MathF.Sin(a), 0, MathF.Cos(a)), 0.25f);
        }
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.07f * s, 0.07f, 0));
            var tip = s > 0 ? bc + V(0.16f * s, 0.2f, 0.04f) : bc + V(0.15f * s, -0.04f, 0.03f);
            b.Limb(wing, bc + V(0.065f * s, 0.07f, 0), tip, 0.025f, 0.018f, blue);
            foreach (float k in new[] { 0.4f, 0.7f, 1f })
                Frond(b, wing, Vector3.Lerp(bc + V(0.065f * s, 0.07f, 0), tip, k), Vector3.Lerp(bc + V(0.065f * s, 0.07f, 0), tip, k) + V(0.02f * s, -0.06f, -0.01f), 0.02f, deep, V(s, 0, 0.3f), 0.25f);
        });
        int head = b.Head(bc + V(0, 0.11f, 0.01f));
        var c = bc + V(0, 0.18f, 0.02f);
        var r = V(0.06f, 0.06f, 0.06f);
        b.Limb(head, bc + V(0, 0.08f, 0), c + V(0, -0.04f, -0.01f), 0.03f, 0.03f, blue);
        b.Ell(head, c, r, blue);
        b.PaintEll(head, c + V(0, -0.015f, 0.04f), V(0.045f, 0.035f, 0.03f), DuckWhite);
        b.Tube(head, Smooth(3, c + V(0, 0.04f, 0.03f), c + V(0, 0.09f, -0.01f), c + V(0, 0.08f, -0.08f), c + V(0, 0.03f, -0.1f)), 0.035f, 0.015f, deep, blend: 0f);
        b.Ell(head, c + V(0, -0.02f, 0.06f), V(0.03f, 0.012f, 0.03f), DuckBeak, mat: Shell, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.025f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 100, 180), glare: true);
        });
        return b;
    }

    /// <summary>Quaquaval: a tall dancer of a bird, blue with a white breast, long legs to orange feet, a great crest of blue plumes with an orange flash, an orange mask round its eyes, a yellow beak, and a boa of pale blue water rings hanging from its wings.</summary>
    private static PokeBuilder Quaquaval()
    {
        var b = new PokeBuilder("Quaquaval", 1f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Fur };
        var blue = Rgb(52, 84, 190);
        var light = Rgb(150, 212, 244);
        var orange = Rgb(242, 112, 52);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.4f, 0));
            var knee = V(0.06f * s, 0.22f, 0.03f);
            b.Limb(leg, V(0.04f * s, 0.41f, 0), knee, 0.03f, 0.02f, blue);
            b.Limb(leg, knee, V(0.06f * s, 0.04f, 0.0f), 0.02f, 0.016f, blue);
            Digits(b, leg, V(0.06f * s, 0.02f, 0.0f), V(0, -0.2f, 1f), V(0.02f, 0, 0), 0.05f, 0.01f, orange);
        });
        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc, V(0.07f, 0.11f, 0.06f), blue);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.05f), V(0.045f, 0.08f, 0.03f), DuckWhite);
        for (int i = 0; i < 7; i++)
        {
            float a = (i - 3) * 0.45f;
            var root = bc + V(MathF.Sin(a) * 0.05f, -0.07f, -MathF.Cos(a) * 0.05f);
            Frond(b, Body, root, root + V(MathF.Sin(a) * 0.05f, -0.1f, -MathF.Cos(a) * 0.05f), 0.025f, blue, V(MathF.Sin(a), 0, -MathF.Cos(a)), 0.25f);
        }
        // Wings with a boa of water rings hanging between them behind
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.06f * s, 0.07f, 0));
            var tip = s > 0 ? bc + V(0.2f * s, 0.18f, 0.02f) : bc + V(0.19f * s, -0.04f, 0.04f);
            b.Limb(wing, bc + V(0.06f * s, 0.07f, 0), tip, 0.026f, 0.018f, blue);
            foreach (float k in new[] { 0.35f, 0.6f, 0.85f, 1f })
                Frond(b, wing, Vector3.Lerp(bc + V(0.06f * s, 0.07f, 0), tip, k), Vector3.Lerp(bc + V(0.06f * s, 0.07f, 0), tip, k) + V(0.03f * s, -0.07f, -0.01f), 0.022f, blue, V(s, 0, 0.3f), 0.25f);
            var end = bc + V(0.07f * s, -0.05f, -0.05f);
            int beads = (int)MathF.Ceiling(Vector3.Distance(tip, end) / 0.028f);
            for (int i = 0; i <= beads; i++)
            {
                float k = i / (float)beads;
                var at = Vector3.Lerp(tip, end, k) + V(0, -0.04f * MathF.Sin(k * MathF.PI), -0.03f * MathF.Sin(k * MathF.PI));
                b.Ell(wing, at, V(0.02f, 0.02f, 0.02f), light, mat: Glow, blend: 0.008f);
            }
        });
        int head = b.Head(bc + V(0, 0.12f, 0.01f));
        var c = bc + V(0, 0.19f, 0.02f);
        var r = V(0.055f, 0.055f, 0.055f);
        b.Limb(head, bc + V(0, 0.1f, 0), c + V(0, -0.03f, -0.01f), 0.03f, 0.03f, blue);
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, -0.015f, 0.055f), V(0.022f, 0.012f, 0.035f), DuckBeak, mat: Shell, blend: 0.006f);
        foreach (var (x, y, z, k) in new[] { (0f, 0.16f, -0.06f, 1f), (-0.04f, 0.13f, -0.09f, 0.85f), (0.04f, 0.13f, -0.09f, 0.85f), (0f, 0.1f, -0.13f, 0.7f) })
            Frond(b, head, c + V(x * 0.3f, 0.04f, -0.01f), c + V(x, y, z), 0.03f * k, blue, V(x * 10f, 0.2f, 1f), 0.25f);
        b.PaintEll(head, c + V(0, 0.06f, -0.01f), V(0.03f, 0.02f, 0.03f), orange, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.024f * s, c.Y + 0.006f);
            b.PaintEll(head, at, V(0.022f, 0.015f, 0.02f), orange, soft: 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(30, 60, 120), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Lechonk and Oinkologne

    /// <summary>Lechonk: a little round pig, dark grey with a band of brown across its face, a big pink snout, yellow lids over sleepy eyes, floppy ears, tiny pink trotters and a curly tail.</summary>
    private static PokeBuilder Lechonk()
    {
        var b = new PokeBuilder("Lechonk", 0.45f, BodyPlan.Quadruped, V(0, 0.14f, 0)) { Coat = Fur };
        var grey = Rgb(70, 70, 82);
        var brown = Rgb(112, 98, 88);
        var pink = Rgb(232, 140, 152);
        var yellow = Rgb(242, 202, 82);
        StubbyLegs(b, 0.05f, 0.08f, 0.06f, -0.06f, 0.026f, grey, pink);
        var bc = V(0, 0.14f, 0);
        b.Ell(Body, bc, V(0.1f, 0.09f, 0.11f), grey);
        int tail = b.Tail(bc + V(0, 0.03f, -0.1f));
        CurledTail(b, tail, bc + V(0, 0.03f, -0.1f), V(0, 0.4f, -1f), 0.03f, 0.008f, grey, 0.02f);
        int head = b.Head(bc + V(0, 0.02f, 0.07f));
        var c = bc + V(0, 0.03f, 0.09f);
        var r = V(0.08f, 0.07f, 0.06f);
        b.Ell(head, c, r, grey);
        b.PaintEll(head, c + V(0, 0.005f, 0.04f), V(0.085f, 0.025f, 0.04f), brown);
        b.Ell(head, c + V(0, -0.015f, 0.06f), V(0.035f, 0.028f, 0.02f), pink);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.012f * s, -0.015f, 0.078f), V(0.006f, 0.01f, 0.006f), Rgb(150, 70, 90), soft: 0.003f);
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.05f, -0.01f));
            b.Ell(ear, c + V(0.08f * s, 0.04f, 0.0f), V(0.02f, 0.035f, 0.03f), brown, V(0, 0, -40f * s));
            var at = On(c, r, 0.042f * s, c.Y + 0.012f);
            b.PaintEll(head, at + V(0, -0.006f, 0.002f), V(0.016f, 0.008f, 0.012f), yellow, soft: 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(40, 34, 40));
        });
        return b;
    }

    /// <summary>
    /// Oinkologne, male and female: a big pig standing on dainty trotters, a pink snout, yellow lines under its
    /// half-shut eyes and great floppy ears. The male is dark grey with a darker mane falling over his brow, magenta
    /// trotters and a curled tail tipped with a magenta ball; the female is brown with a paler face and pink tufts at
    /// the end of her curled tail.
    /// </summary>
    private static PokeBuilder OinkologneBuild(bool female)
    {
        var b = new PokeBuilder(female ? "Oinkologne-Female" : "Oinkologne", 0.75f, BodyPlan.Quadruped, V(0, 0.3f, -0.02f)) { Coat = Fur };
        var coat = female ? Rgb(120, 84, 68) : Rgb(88, 82, 102);
        var mane = female ? Rgb(92, 62, 52) : Rgb(58, 52, 72);
        var face = female ? Rgb(176, 124, 100) : coat;
        var pink = Rgb(236, 140, 156);
        var hoof = female ? Rgb(232, 150, 160) : Rgb(204, 64, 124);
        var yellow = Rgb(242, 202, 82);
        StubbyLegs(b, 0.09f, 0.2f, 0.12f, -0.14f, 0.045f, coat, hoof);
        var bc = V(0, 0.3f, -0.02f);
        b.Ell(Body, bc, V(0.15f, 0.14f, 0.22f), coat);
        b.PaintEll(Body, bc + V(0, 0.06f, 0.08f), V(0.15f, 0.1f, 0.16f), mane, soft: 0.02f);
        int tail = b.Tail(bc + V(0, 0.06f, -0.21f));
        CurledTail(b, tail, bc + V(0, 0.06f, -0.21f), V(0, 0.5f, -1f), 0.06f, 0.012f, coat, 0.03f);
        if (female)
            foreach (var d in new[] { V(0.03f, 0.08f, -0.02f), V(0.0f, 0.1f, -0.03f), V(-0.03f, 0.08f, -0.02f) })
                b.Spike(tail, bc + V(0, 0.13f, -0.26f), bc + V(0, 0.13f, -0.26f) + d, 0.01f, pink, 0.6f);
        else
            b.Ell(tail, bc + V(0, 0.14f, -0.26f), V(0.025f, 0.025f, 0.025f), hoof);
        int head = b.Head(bc + V(0, 0.05f, 0.18f));
        var c = bc + V(0, 0.07f, 0.22f);
        var r = V(0.1f, 0.09f, 0.085f);
        b.Ell(head, c, r, face);
        b.Ell(head, c + V(0, -0.02f, 0.08f), V(0.045f, 0.035f, 0.025f), pink);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.016f * s, -0.02f, 0.105f), V(0.008f, 0.013f, 0.007f), Rgb(150, 70, 90), soft: 0.003f);
            int ear = b.Ear(head, s, c + V(0.07f * s, 0.07f, -0.01f));
            b.Ell(ear, c + V(0.1f * s, 0.06f, 0.02f), V(0.025f, 0.06f, 0.05f), mane, V(30f, 0, -50f * s));
            var at = On(c, r, 0.05f * s, c.Y + 0.012f);
            b.PaintEll(head, at + V(0, -0.008f, 0.003f), V(0.02f, 0.008f, 0.015f), yellow, soft: 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 34, 40));
        });
        if (!female)
        {
            // The male's mane falling over his brow
            b.Ell(head, c + V(0, 0.06f, 0.02f), V(0.09f, 0.04f, 0.07f), mane, blend: 0.012f);
            foreach (float x in new[] { -0.04f, 0f, 0.04f })
                b.Spike(head, c + V(x, 0.06f, 0.05f), c + V(x * 1.2f, 0.035f, 0.1f), 0.022f, mane, 0.5f);
        }
        return b;
    }

    private static PokeBuilder Oinkologne() => OinkologneBuild(false);

    // ------------------------------------------------------------------ Tarountula and Spidops

    /// <summary>Tarountula: a little yellow spider carrying a great ball of white thread on its back, wound round and round, a loose end curling from it, big eyes and six green legs banded yellow.</summary>
    private static PokeBuilder Tarountula()
    {
        var b = new PokeBuilder("Tarountula", 0.4f, BodyPlan.Quadruped, V(0, 0.18f, -0.03f)) { Coat = Shell };
        var white = Rgb(244, 244, 240);
        var thread = Rgb(210, 210, 214);
        var yellow = Rgb(232, 224, 92);
        var green = Rgb(90, 150, 62);
        var bc = V(0, 0.2f, -0.05f);
        b.Ell(Body, bc, V(0.12f, 0.12f, 0.12f), white);
        foreach (var turn in new[] { V(0, 0, 30f), V(60f, 0, -20f), V(-50f, 40f, 10f), V(20f, 90f, 60f) })
            b.PaintTorus(Body, bc, 0.12f, 0.005f, thread, turn);
        b.Tube(Body, Smooth(3, bc + V(0.02f, 0.11f, -0.02f), bc + V(0.05f, 0.17f, -0.04f), bc + V(0.1f, 0.18f, -0.02f), bc + V(0.1f, 0.15f, 0.01f)), 0.008f, 0.006f, white, blend: 0.004f);
        int head = b.Head(V(0, 0.12f, 0.06f));
        var c = V(0, 0.11f, 0.08f);
        var r = V(0.065f, 0.05f, 0.055f);
        b.Ell(head, c, r, yellow);
        b.Limb(head, c + V(0, 0, -0.03f), bc + V(0, -0.06f, 0.07f), 0.04f, 0.04f, yellow);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.02f * s, -0.035f, 0.04f), c + V(0.015f * s, -0.06f, 0.06f), 0.01f, Rgb(250, 248, 230), mat: Shell, blend: 0.004f);
            var at = On(c, r, 0.025f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, sclera: true, pupil: Rgb(40, 60, 110));
        });
        foreach (var (z, front) in new[] { (0.09f, true), (0.05f, true), (0.01f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.04f * s, 0.11f, z), front);
                var knee = V(0.11f * s, 0.12f, z + (z - 0.05f) * 0.6f);
                var foot = V(0.13f * s, 0.01f, z + (z - 0.05f) * 1.2f);
                b.Limb(leg, V(0.04f * s, 0.11f, z), knee, 0.012f, 0.011f, green);
                b.Limb(leg, knee, foot, 0.011f, 0.009f, green);
                b.PaintEll(leg, Vector3.Lerp(knee, foot, 0.4f), V(0.014f, 0.012f, 0.014f), yellow, soft: 0.004f);
            });
        return b;
    }

    /// <summary>Spidops: a guard of a spider standing tall on four long jointed legs, olive green with tan joints, its round body banded, a small head with two dark round eyes like goggles, and a triangle of white thread held between its front legs.</summary>
    private static PokeBuilder Spidops()
    {
        var b = new PokeBuilder("Spidops", 0.85f, BodyPlan.Quadruped, V(0, 0.44f, -0.02f)) { Coat = Shell };
        var green = Rgb(100, 130, 62);
        var olive = Rgb(140, 150, 80);
        var tan = Rgb(200, 172, 112);
        var white = Rgb(244, 244, 240);
        var bc = V(0, 0.44f, -0.02f);
        b.Ell(Body, bc, V(0.08f, 0.1f, 0.08f), green);
        foreach (float y in new[] { -0.04f, 0.0f, 0.04f })
            b.PaintTorus(Body, bc + V(0, y, 0), 0.078f - MathF.Abs(y) * 0.4f, 0.008f, tan);
        b.Ell(Body, bc + V(0, -0.1f, -0.03f), V(0.06f, 0.06f, 0.06f), olive);
        var knees = new Dictionary<float, Vector3>();
        foreach (var (z, front) in new[] { (0.04f, true), (-0.06f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, bc + V(0.06f * s, 0.0f, z), front);
                var knee = bc + V(0.17f * s, 0.12f, z * 2.5f);
                var foot = V(0.27f * s, 0.02f, bc.Z + z * 4f);
                b.Limb(leg, bc + V(0.06f * s, 0.0f, z), knee, 0.02f, 0.016f, green);
                b.Ell(leg, knee, V(0.022f, 0.022f, 0.022f), tan);
                b.Limb(leg, knee, foot, 0.016f, 0.012f, green);
                b.Ell(leg, foot + V(0, 0.01f, 0), V(0.016f, 0.012f, 0.016f), tan);
                if (front) knees[s] = knee;
            });
        // The triangle of thread held between its front legs
        var low = bc + V(0, -0.32f, 0.2f);
        b.Tube(Body, new[] { knees[-1f], knees[1f] }, 0.006f, 0.006f, white, blend: 0.004f);
        b.Tube(Body, new[] { knees[-1f], low }, 0.006f, 0.006f, white, blend: 0.004f);
        b.Tube(Body, new[] { knees[1f], low }, 0.006f, 0.006f, white, blend: 0.004f);
        b.Limb(Body, bc + V(0, -0.05f, 0.04f), low, 0.006f, 0.006f, white, blend: 0.004f);
        int head = b.Head(bc + V(0, 0.1f, 0.02f));
        var c = bc + V(0, 0.15f, 0.04f);
        var r = V(0.055f, 0.045f, 0.05f);
        b.Ell(head, c, r, olive);
        b.Ell(head, c + V(0, 0.03f, -0.01f), V(0.05f, 0.025f, 0.045f), green, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(40, 40, 46));
            b.Ell(head, c + V(0.05f * s, 0.01f, -0.01f), V(0.018f, 0.018f, 0.014f), Rgb(60, 60, 64));
        });
        return b;
    }

    // ------------------------------------------------------------------ Nymble and Lokix

    /// <summary>Nymble: a little grasshopper, its round head and body grey-blue, two great dark ears rising behind, a yellow four-pointed mark on each side, big dark eyes and thin white legs, the back ones long and bent to spring.</summary>
    private static PokeBuilder Nymble()
    {
        var b = new PokeBuilder("Nymble", 0.35f, BodyPlan.Quadruped, V(0, 0.17f, 0)) { Coat = Shell };
        var grey = Rgb(104, 116, 138);
        var dark = Rgb(62, 68, 90);
        var white = Rgb(236, 236, 240);
        var yellow = Rgb(240, 190, 60);
        var bc = V(0, 0.17f, 0);
        var br = V(0.08f, 0.07f, 0.085f);
        b.Ell(Body, bc, br, grey);
        b.Ell(Body, bc + V(0, -0.01f, -0.09f), V(0.05f, 0.045f, 0.05f), dark);
        PokeBuilder.Both(s =>
        {
            var mark = Out(bc, br, default, V(s, 0, -0.1f));
            b.Mark(Body, mark, V(s, 0, 0), 0.025f, 0.025f, yellow, MarkShape.Star);
            // Two great ears rising behind
            int ear = b.Ear(Body, s, bc + V(0.035f * s, 0.05f, -0.03f));
            b.Ell(ear, bc + V(0.045f * s, 0.12f, -0.05f), V(0.025f, 0.065f, 0.04f), dark, V(-15f, 0, -10f * s));
            b.PaintEll(ear, bc + V(0.06f * s, 0.13f, -0.04f), V(0.012f, 0.03f, 0.022f), grey, V(-15f, 0, -10f * s), 0.006f);
        });
        foreach (var (z, front) in new[] { (0.04f, true), (-0.05f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, bc + V(0.05f * s, -0.04f, z), front);
                if (front)
                    b.Limb(leg, bc + V(0.05f * s, -0.04f, z), V(0.07f * s, 0.01f, z + 0.02f), 0.012f, 0.01f, white);
                else
                {
                    var knee = bc + V(0.1f * s, 0.04f, z - 0.04f);
                    b.Limb(leg, bc + V(0.05f * s, -0.04f, z), knee, 0.016f, 0.012f, white);
                    b.Limb(leg, knee, V(0.11f * s, 0.01f, z - 0.02f), 0.012f, 0.01f, white);
                }
            });
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.03f * s, bc.Y + 0.01f);
            b.Eye(Body, at, Outward(bc, br, at), 0.018f, Rgb(30, 30, 40));
        });
        return b;
    }

    /// <summary>Lokix: a grasshopper standing like a fighter, black armour over a grey body, a white cross on its chest, orange spikes at its elbows and heels, long bent legs, sharp orange eyes in a black helm and two long feelers swept back.</summary>
    private static PokeBuilder Lokix()
    {
        var b = new PokeBuilder("Lokix", 0.9f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Shell };
        var black = Rgb(48, 50, 60);
        var grey = Rgb(96, 102, 122);
        var white = Rgb(232, 232, 238);
        var orange = Rgb(242, 150, 40);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.4f, -0.01f));
            var knee = V(0.09f * s, 0.26f, 0.08f);
            var heel = V(0.09f * s, 0.1f, -0.06f);
            b.Limb(leg, V(0.06f * s, 0.41f, -0.01f), knee, 0.045f, 0.03f, black);
            b.Limb(leg, knee, heel, 0.025f, 0.02f, grey);
            b.Limb(leg, heel, V(0.09f * s, 0.025f, 0.04f), 0.02f, 0.018f, black);
            b.Spike(leg, heel, heel + V(0.01f * s, -0.01f, -0.06f), 0.014f, orange, 0.6f);
            b.Spike(leg, V(0.09f * s, 0.02f, 0.05f), V(0.09f * s, 0.01f, 0.1f), 0.015f, black, 0.6f);
        });
        var bc = V(0, 0.5f, 0);
        b.Ell(Body, bc, V(0.07f, 0.12f, 0.06f), black);
        b.Ell(Body, bc + V(0, -0.09f, -0.02f), V(0.06f, 0.06f, 0.06f), grey);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.055f), V(0.006f, 0.05f, 0.02f), white, V(0, 0, 40f), 0.004f);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.055f), V(0.006f, 0.05f, 0.02f), white, V(0, 0, -40f), 0.004f);
        b.Ell(Body, bc + V(0, 0.0f, -0.07f), V(0.06f, 0.09f, 0.025f), black, V(10f, 0, 0), blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.07f * s, 0.08f, 0));
            var elbow = bc + V(0.14f * s, -0.02f, 0.04f);
            var hand = bc + V(0.1f * s, 0.06f, 0.14f);
            b.Limb(arm, bc + V(0.06f * s, 0.08f, 0), elbow, 0.025f, 0.02f, black);
            b.Limb(arm, elbow, hand, 0.02f, 0.018f, grey);
            b.Spike(arm, elbow, elbow + V(0.03f * s, -0.04f, -0.04f), 0.014f, orange, 0.6f);
            Claws(b, arm, hand + V(0, 0.0f, 0.015f), V(0.008f, 0, 0), V(0, 0.2f, 1f), 0.02f, 0.007f);
        });
        int head = b.Head(bc + V(0, 0.13f, 0.01f));
        var c = bc + V(0, 0.2f, 0.02f);
        var r = V(0.055f, 0.06f, 0.06f);
        b.Limb(head, bc + V(0, 0.1f, 0), c + V(0, -0.04f, -0.01f), 0.028f, 0.028f, black);
        b.Ell(head, c, r, black);
        b.Spike(head, c + V(0, -0.02f, 0.04f), c + V(0, -0.06f, 0.07f), 0.03f, black, 0.6f);
        PokeBuilder.Both(s =>
        {
            b.Tube(head, Smooth(3, c + V(0.02f * s, 0.05f, -0.01f), c + V(0.04f * s, 0.16f, -0.05f), c + V(0.06f * s, 0.2f, -0.14f)), 0.008f, 0.005f, black, blend: 0.004f);
            var at = On(c, r, 0.03f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, orange, glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Pawmi line

    private static readonly Color PawOrange = Rgb(242, 150, 50);
    private static readonly Color PawCream = Rgb(250, 232, 184);
    private static readonly Color PawYellow = Rgb(250, 214, 70);
    private static readonly Color PawTeal = Rgb(40, 110, 100);

    /// <summary>A Pawmi's round ears, teal inside, and its yellow cheeks and dark eyes on the face at <paramref name="c"/>.</summary>
    private static void PawmiFace(PokeBuilder b, int head, Vector3 c, Vector3 r, float ear, Color fur)
    {
        PokeBuilder.Both(s =>
        {
            int e = b.Ear(head, s, c + V(0.06f * s, 0.06f, -0.01f));
            var ec = c + V(r.X * 0.95f * s, r.Y * 1.05f, -0.01f);
            b.Ell(e, ec, V(ear * 0.75f, ear, ear * 0.35f), fur, V(0, 0, -30f * s));
            b.PaintEll(e, ec + V(0, 0, ear * 0.3f), V(ear * 0.45f, ear * 0.65f, ear * 0.2f), PawTeal, V(0, 0, -30f * s), 0.006f);
            b.PaintEll(head, c + V(r.X * 0.6f * s, -r.Y * 0.25f, r.Z * 0.7f), V(r.X * 0.25f, r.Y * 0.22f, r.Z * 0.25f), PawYellow, soft: 0.006f);
            var at = On(c, r, c.X + r.X * 0.38f * s, c.Y + r.Y * 0.1f);
            b.Eye(head, at, Outward(c, r, at), r.Y * 0.2f, Rgb(80, 50, 40));
        });
        b.Ell(head, c + V(0, -r.Y * 0.35f, r.Z * 0.85f), V(r.X * 0.3f, r.Y * 0.22f, r.Z * 0.2f), PawCream, blend: 0.01f);
    }

    /// <summary>Pawmi: a round little orange mouse on all fours, a tuft of fur on its crown, round ears teal inside, yellow cheeks, a cream muzzle and cream paws.</summary>
    private static PokeBuilder Pawmi()
    {
        var b = new PokeBuilder("Pawmi", 0.4f, BodyPlan.Quadruped, V(0, 0.12f, -0.02f)) { Coat = Fur };
        StubbyLegs(b, 0.06f, 0.07f, 0.06f, -0.07f, 0.026f, PawOrange, PawCream);
        var bc = V(0, 0.12f, -0.02f);
        b.Ell(Body, bc, V(0.1f, 0.08f, 0.1f), PawOrange);
        b.PaintEll(Body, bc + V(0, -0.06f, 0.02f), V(0.08f, 0.03f, 0.08f), PawCream);
        int tail = b.Tail(bc + V(0, 0.02f, -0.09f));
        b.Spike(tail, bc + V(0, 0.02f, -0.09f), bc + V(0, 0.08f, -0.15f), 0.022f, PawCream, 0.5f);
        int head = b.Head(bc + V(0, 0.03f, 0.07f));
        var c = bc + V(0, 0.06f, 0.09f);
        var r = V(0.085f, 0.07f, 0.065f);
        b.Ell(head, c, r, PawOrange);
        foreach (var (x, z) in new[] { (0f, 0f), (-0.025f, -0.02f), (0.025f, -0.02f) })
            b.Spike(head, c + V(x, 0.05f, z), c + V(x * 1.5f, 0.11f, z + 0.02f), 0.025f, PawOrange, 0.5f);
        PawmiFace(b, head, c, r, 0.04f, PawOrange);
        return b;
    }

    /// <summary>Pawmo: Pawmi standing up to fight, paler orange with a cream belly, one paw raised, pads on its palms, round ears teal inside, yellow cheeks and a tuft of fur on its crown.</summary>
    private static PokeBuilder Pawmo()
    {
        var b = new PokeBuilder("Pawmo", 0.5f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Fur };
        var fur = Rgb(240, 180, 82);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.1f, 0));
            b.Limb(leg, V(0.04f * s, 0.1f, 0), V(0.045f * s, 0.03f, 0.01f), 0.026f, 0.022f, fur);
            b.Ell(leg, V(0.045f * s, 0.02f, 0.025f), V(0.026f, 0.02f, 0.035f), PawCream);
        });
        var bc = V(0, 0.16f, 0);
        b.Ell(Body, bc, V(0.065f, 0.075f, 0.06f), fur);
        b.PaintEll(Body, bc + V(0, -0.01f, 0.045f), V(0.045f, 0.055f, 0.03f), PawCream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.06f * s, 0.04f, 0.01f));
            var hand = s > 0 ? bc + V(0.1f * s, 0.12f, 0.04f) : bc + V(0.09f * s, -0.02f, 0.06f);
            b.Limb(arm, bc + V(0.055f * s, 0.04f, 0.01f), hand, 0.02f, 0.02f, fur);
            b.Ell(arm, hand, V(0.026f, 0.026f, 0.022f), PawCream);
            b.PaintEll(arm, hand + V(0, 0, 0.02f), V(0.012f, 0.012f, 0.008f), PawYellow, soft: 0.004f);
        });
        int tail = b.Tail(bc + V(0, -0.04f, -0.05f));
        b.Spike(tail, bc + V(0, -0.04f, -0.05f), bc + V(0, 0.02f, -0.12f), 0.022f, PawCream, 0.5f);
        int head = b.Head(bc + V(0, 0.07f, 0.01f));
        var c = bc + V(0, 0.13f, 0.02f);
        var r = V(0.07f, 0.065f, 0.06f);
        b.Ell(head, c, r, fur);
        b.Spike(head, c + V(0, 0.05f, 0), c + V(0.01f, 0.1f, 0.02f), 0.022f, fur, 0.5f);
        PawmiFace(b, head, c, r, 0.035f, fur);
        return b;
    }

    /// <summary>Pawmot: grown into a fighter, orange with a great crest of fluffy fur on its crown and a ruff at its neck, round ears teal inside, yellow cheeks, and great cream paws with yellow pads, one thrust forward.</summary>
    private static PokeBuilder Pawmot()
    {
        var b = new PokeBuilder("Pawmot", 0.8f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.18f, 0));
            b.Limb(leg, V(0.06f * s, 0.19f, 0), V(0.07f * s, 0.05f, 0.01f), 0.042f, 0.035f, PawOrange);
            b.Ell(leg, V(0.07f * s, 0.03f, 0.03f), V(0.04f, 0.03f, 0.05f), PawCream);
        });
        var bc = V(0, 0.28f, 0);
        b.Ell(Body, bc, V(0.1f, 0.11f, 0.085f), PawOrange);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.07f), V(0.06f, 0.07f, 0.03f), PawCream);
        FurTufts(b, Body, bc + V(0, 0.09f, 0.0f), V(0.08f, 0.04f, 0.07f), 12, 0.035f, 0.016f, PawOrange, 1.1f, -0.2f, 0.5f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.09f * s, 0.05f, 0.01f));
            var hand = s > 0 ? bc + V(0.17f * s, 0.06f, 0.12f) : bc + V(0.15f * s, -0.04f, 0.05f);
            b.Limb(arm, bc + V(0.08f * s, 0.05f, 0.01f), hand, 0.03f, 0.035f, PawOrange);
            b.Ell(arm, hand, V(0.05f, 0.045f, 0.04f), PawCream);
            var pad = s > 0 ? hand + V(0, 0, 0.035f) : hand + V(0.02f * s, 0, 0.03f);
            b.PaintEll(arm, pad, V(0.02f, 0.02f, 0.012f), PawYellow, soft: 0.005f);
        });
        int tail = b.Tail(bc + V(0, -0.06f, -0.07f));
        b.Spike(tail, bc + V(0, -0.06f, -0.07f), bc + V(0, 0.02f, -0.16f), 0.03f, PawCream, 0.5f);
        int head = b.Head(bc + V(0, 0.11f, 0.01f));
        var c = bc + V(0, 0.18f, 0.02f);
        var r = V(0.08f, 0.07f, 0.065f);
        b.Ell(head, c, r, PawOrange);
        foreach (var (x, z, h) in new[] { (0f, 0.02f, 0.11f), (-0.035f, 0.0f, 0.08f), (0.035f, 0.0f, 0.08f), (-0.02f, -0.035f, 0.09f), (0.02f, -0.035f, 0.09f) })
            b.Spike(head, c + V(x, 0.05f, z), c + V(x * 1.8f, 0.05f + h, z - 0.02f), 0.028f, PawOrange, 0.5f);
        PawmiFace(b, head, c, r, 0.04f, PawOrange);
        return b;
    }
}
