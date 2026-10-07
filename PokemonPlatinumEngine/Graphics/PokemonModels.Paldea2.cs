using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Paldea's second batch in National Pokédex
// order: Tandemaus (924) to Klawf (950). Their forms are in PokemonModels.Regional.cs with the other forms. Helpers
// shared with the earlier batches are in the files of those batches, PokemonModels.Sinnoh1.cs to PokemonModels.Paldea1.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Tandemaus and Maushold

    private static readonly Color MouseWhite = Rgb(246, 244, 240);
    private static readonly Color MouseShorts = Rgb(196, 204, 224);
    private static readonly Color MouseEarIn = Rgb(222, 220, 224);

    /// <summary>
    /// One mouse of the Tandemaus line standing at <paramref name="foot"/>, <paramref name="k"/> times a Tandemaus's
    /// size: a great round white head with big round ears, black bead eyes and a dark nose, a small body pale blue-grey
    /// below and little feet. Each arm holds another's paw where one is given (<paramref name="left"/>,
    /// <paramref name="right"/>), or is raised to its chest. Its head is on <paramref name="head"/>, the rest on
    /// <paramref name="body"/>.
    /// </summary>
    private static void Mouse(PokeBuilder b, int body, int head, Vector3 foot, float k, Vector3? left, Vector3? right, bool tail)
    {
        var bc = foot + V(0, 0.075f, 0) * k;
        PokeBuilder.Both(s => b.Ell(body, foot + V(0.022f * s, 0.016f, 0.008f) * k, V(0.018f, 0.016f, 0.024f) * k, MouseWhite));
        b.Ell(body, bc, V(0.04f, 0.055f, 0.036f) * k, MouseWhite);
        b.PaintEll(body, bc + V(0, -0.04f, 0) * k, V(0.05f, 0.032f, 0.046f) * k, MouseShorts, soft: 0.008f * k);
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.03f * s, 0.022f, 0.005f) * k;
            var paw = (s < 0 ? left : right) ?? bc + V(0.028f * s, 0.035f, 0.036f) * k;
            b.Limb(body, shoulder, paw, 0.013f * k, 0.013f * k, MouseWhite);
        });
        if (tail)
        {
            var root = bc + V(0, -0.03f, -0.03f) * k;
            b.Tube(body, Smooth(3, root, root + V(0.02f, -0.02f, -0.04f) * k, root + V(0.05f, 0.0f, -0.05f) * k, root + V(0.04f, 0.03f, -0.045f) * k,
                root + V(0.02f, 0.015f, -0.05f) * k), 0.005f * k, 0.004f * k, MouseWhite, blend: 0.003f);
        }
        var hc = foot + V(0, 0.185f, 0.005f) * k;
        var hr = V(0.075f, 0.065f, 0.062f) * k;
        b.Ell(head, hc, hr, MouseWhite);
        PokeBuilder.Both(s =>
        {
            var ec = hc + V(0.068f * s, 0.058f, -0.012f) * k;
            b.Ell(head, ec, V(0.05f, 0.05f, 0.016f) * k, MouseWhite, V(0, 0, -15f * s), blend: 0.01f * k);
            b.PaintEll(head, ec + V(0, 0, 0.012f) * k, V(0.033f, 0.033f, 0.008f) * k, MouseEarIn, V(0, 0, -15f * s), 0.005f * k);
            var at = On(hc, hr, hc.X + 0.026f * s * k, hc.Y - 0.004f * k);
            // A little one's eyes are a little bigger for its size, as a child's are
            b.Eye(head, at, Outward(hc, hr, at), 0.01f * MathF.Sqrt(k), Rgb(30, 30, 34));
        });
        b.Ell(head, On(hc, hr, hc.X, hc.Y - 0.02f * k), V(0.006f, 0.005f, 0.004f) * k, Rgb(80, 74, 78), blend: 0.002f);
    }

    /// <summary>Tandemaus: two little white mice that are always together, holding paws, each a great round head with big round ears over a small body pale blue-grey below.</summary>
    private static PokeBuilder Tandemaus()
    {
        var b = new PokeBuilder("Tandemaus", 0.42f, BodyPlan.Biped, V(0, 0.075f, 0)) { Coat = Fur };
        // One a little before the other, so their ears pass one in front of the other as in its pictures
        var left = V(-0.095f, 0, 0.025f);
        var right = V(0.095f, 0, -0.025f);
        var paws = V(0, 0.075f, 0.035f);
        int l = b.Head(left + V(0, 0.13f, 0));
        int r = b.Part("head2", Body, right + V(0, 0.13f, 0), PokeRole.Head, 1.9f, 1f);
        Mouse(b, Body, l, left, 1f, null, paws, false);
        Mouse(b, Body, r, right, 1f, paws, null, true);
        b.Ell(Body, paws, V(0.017f, 0.015f, 0.015f), MouseWhite, blend: 0.006f);
        return b;
    }

    /// <summary>
    /// Maushold: Tandemaus grown into a family, the two grown mice holding paws and their little ones beside them:
    /// two of them, one holding each parent's other paw, or (the family of three) one between them, held by both.
    /// </summary>
    private static PokeBuilder MausholdBuild(bool three)
    {
        var b = new PokeBuilder(three ? "Maushold-Family-Of-Three" : "Maushold", 0.55f, BodyPlan.Biped, V(0, 0.085f, 0)) { Coat = Fur };
        const float big = 1.15f, small = 0.5f;
        var left = V(-0.11f, 0, 0.025f);
        var right = V(0.11f, 0, -0.03f);
        var paws = V(0, 0.085f, 0.03f);
        int l = b.Head(left + V(0, 0.15f, 0));
        int r = b.Part("head2", Body, right + V(0, 0.15f, 0), PokeRole.Head, 1.9f, 1f);
        if (three)
        {
            // The little one stands between its parents, in front of their joined paws, which rest on its head
            var kid = V(0, 0, 0.075f);
            int k = b.Part("head3", Body, kid + V(0, 0.07f, 0), PokeRole.Head, 3.3f);
            Mouse(b, Body, l, left, big, null, paws, false);
            Mouse(b, Body, r, right, big, paws, null, true);
            Mouse(b, Body, k, kid, small, null, null, true);
            b.Ell(Body, paws, V(0.019f, 0.017f, 0.017f), MouseWhite, blend: 0.006f);
            b.Limb(Body, paws, kid + V(0, 0.15f, -0.012f) * small, 0.012f, 0.012f, MouseWhite, blend: 0.006f);
            return b;
        }
        // A little one at each side, holding a parent's outer paw
        var kidL = V(-0.245f, 0, 0.06f);
        var kidR = V(0.245f, 0, 0.02f);
        var handL = V(-0.19f, 0.07f, 0.05f);
        var handR = V(0.19f, 0.07f, 0.01f);
        int kl = b.Part("head3", Body, kidL + V(0, 0.07f, 0), PokeRole.Head, 3.3f, -1f);
        int kr = b.Part("head4", Body, kidR + V(0, 0.07f, 0), PokeRole.Head, 4.6f, 1f);
        Mouse(b, Body, l, left, big, handL, paws, false);
        Mouse(b, Body, r, right, big, paws, handR, true);
        Mouse(b, Body, kl, kidL, small, null, handL, true);
        Mouse(b, Body, kr, kidR, small, handR, null, false);
        b.Ell(Body, paws, V(0.019f, 0.017f, 0.017f), MouseWhite, blend: 0.006f);
        b.Ell(Body, handL, V(0.012f, 0.011f, 0.011f), MouseWhite, blend: 0.005f);
        b.Ell(Body, handR, V(0.012f, 0.011f, 0.011f), MouseWhite, blend: 0.005f);
        return b;
    }

    private static PokeBuilder Maushold() => MausholdBuild(false);

    // ------------------------------------------------------------------ Fidough and Dachsbun

    private static readonly Color DoughCream = Rgb(250, 240, 212);
    private static readonly Color DoughYellow = Rgb(242, 216, 106);

    /// <summary>An ear rolled up like a bun on the side <paramref name="s"/> of a head: a thick ring standing on its edge, its hole facing out, filled with a softer roll.</summary>
    private static void BunEar(PokeBuilder b, int ear, Vector3 at, float s, float r, Color roll, Color inside)
    {
        b.Torus(ear, at, r, r * 0.66f, roll, V(0, 0, 90f), blend: 0.01f);
        b.Ell(ear, at + V(r * 0.15f * s, 0, 0), V(r * 0.5f, r * 0.75f, r * 0.75f), inside, blend: 0.006f);
    }

    /// <summary>Fidough: a puppy of dough, a round cream loaf of a body on stubby legs, golden patches on its back and crown spotted like a crust, ears rolled up like buns, a pale muzzle and black eyes.</summary>
    private static PokeBuilder Fidough()
    {
        var b = new PokeBuilder("Fidough", 0.45f, BodyPlan.Quadruped, V(0, 0.12f, -0.05f)) { Coat = Fur };
        StubbyLegs(b, 0.06f, 0.07f, 0.02f, -0.12f, 0.03f, DoughCream);
        var bc = V(0, 0.12f, -0.05f);
        b.Ell(Body, bc, V(0.09f, 0.075f, 0.12f), DoughCream);
        b.PaintEll(Body, bc + V(0.02f, 0.065f, -0.04f), V(0.07f, 0.035f, 0.07f), DoughYellow);
        int tail = b.Tail(bc + V(0, 0.03f, -0.11f));
        b.Ell(tail, bc + V(0, 0.055f, -0.125f), V(0.028f, 0.034f, 0.03f), DoughYellow);
        int head = b.Head(bc + V(0, 0.05f, 0.08f));
        var c = bc + V(0, 0.095f, 0.11f);
        var r = V(0.09f, 0.08f, 0.08f);
        b.Ell(head, c, r, DoughCream);
        b.PaintEll(head, c + V(0, 0.065f, -0.01f), V(0.08f, 0.035f, 0.08f), DoughYellow);
        foreach (var (x, z) in new[] { (-0.025f, 0.03f), (0.03f, 0.015f), (0.0f, -0.025f) })
            b.PaintEll(head, c + V(x, 0.075f, z), V(0.011f, 0.008f, 0.011f), Rgb(210, 164, 70), soft: 0.005f);
        b.Ell(head, c + V(0, -0.028f, 0.068f), V(0.034f, 0.024f, 0.026f), Rgb(248, 228, 180), blend: 0.012f);
        b.Ell(head, c + V(0, -0.016f, 0.092f), V(0.008f, 0.006f, 0.005f), Rgb(84, 58, 48), blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.07f * s, 0.02f, 0));
            BunEar(b, ear, c + V(0.092f * s, 0.005f, -0.005f), s, 0.03f, DoughYellow, DoughCream);
            var at = On(c, r, c.X + 0.034f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(40, 34, 30));
        });
        return b;
    }

    /// <summary>Dachsbun: a long-bodied dog baked brown, a darker bun on its back scored like a loaf, a ruff of pale fluff at its throat, ears rolled up like buns, a tan muzzle and a tail ending in a flame of pale fluff.</summary>
    private static PokeBuilder Dachsbun()
    {
        var b = new PokeBuilder("Dachsbun", 0.6f, BodyPlan.Quadruped, V(0, 0.15f, -0.05f)) { Coat = Fur };
        var brown = Rgb(198, 98, 44);
        var crust = Rgb(140, 62, 30);
        var tan = Rgb(236, 160, 72);
        var fluff = Rgb(248, 226, 142);
        StubbyLegs(b, 0.065f, 0.1f, 0.06f, -0.15f, 0.032f, brown, tan);
        var bc = V(0, 0.15f, -0.05f);
        b.Ell(Body, bc, V(0.085f, 0.075f, 0.16f), brown);
        b.PaintEll(Body, bc + V(0, -0.065f, 0), V(0.07f, 0.03f, 0.15f), tan);
        // The bun on its back, scored across like a loaf
        var bun = bc + V(0, 0.06f, -0.02f);
        b.Ell(Body, bun, V(0.08f, 0.05f, 0.11f), crust, blend: 0.015f);
        foreach (float z in new[] { -0.05f, 0f, 0.05f })
            b.PaintEll(Body, bun + V(0, 0.045f, z), V(0.07f, 0.012f, 0.008f), brown, soft: 0.005f);
        FurTufts(b, Body, bc + V(0, 0.01f, 0.13f), V(0.075f, 0.07f, 0.04f), 14, 0.04f, 0.018f, fluff, 1.1f, -0.7f, 0.4f);
        int tail = b.Tail(bc + V(0, 0.03f, -0.15f));
        var tp = Smooth(3, bc + V(0, 0.03f, -0.15f), bc + V(0, 0.07f, -0.19f), bc + V(0, 0.12f, -0.18f));
        b.Tube(tail, tp, 0.018f, 0.022f, brown, blend: 0f);
        foreach (var d in new[] { V(0, 1f, 0.2f), V(0.5f, 0.8f, -0.1f), V(-0.5f, 0.8f, -0.1f), V(0, 0.7f, -0.6f) })
            b.Spike(tail, tp[^1], tp[^1] + Vector3.Normalize(d) * 0.055f, 0.022f, fluff, 0.6f);
        int head = b.Head(bc + V(0, 0.05f, 0.13f));
        var c = bc + V(0, 0.1f, 0.17f);
        var r = V(0.075f, 0.07f, 0.07f);
        b.Limb(head, bc + V(0, 0.03f, 0.1f), c + V(0, -0.03f, -0.03f), 0.045f, 0.045f, brown);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.025f, 0.065f), V(0.045f, 0.035f, 0.04f), tan, blend: 0.015f);
        b.Ell(head, c + V(0, -0.01f, 0.105f), V(0.014f, 0.01f, 0.008f), Rgb(50, 34, 30), blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.03f, -0.01f));
            BunEar(b, ear, c + V(0.078f * s, 0.01f, -0.01f), s, 0.03f, crust, brown);
            var at = On(c, r, c.X + 0.032f * s, c.Y + 0.015f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(40, 30, 30));
            b.PaintEll(head, at + V(0.002f * s, 0.016f, 0), V(0.016f, 0.005f, 0.01f), crust, V(0, 0, -15f * s), 0.004f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Smoliv line

    private static readonly Color OliveGreen = Rgb(152, 196, 66);
    private static readonly Color OliveLight = Rgb(212, 216, 96);
    private static readonly Color OliveLeaf = Rgb(70, 150, 62);
    private static readonly Color OliveSocket = Rgb(120, 126, 46);

    /// <summary>Smoliv: a little green olive of a Pokémon on two tiny feet, a lighter olive on its head with two leaves under it, round white eyes and a wavy mouth.</summary>
    private static PokeBuilder Smoliv()
    {
        var b = new PokeBuilder("Smoliv", 0.42f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Leaf };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.04f, 0));
            b.Ell(leg, V(0.045f * s, 0.018f, 0.012f), V(0.022f, 0.018f, 0.026f), OliveGreen);
        });
        var bc = V(0, 0.12f, 0);
        var br = V(0.095f, 0.1f, 0.09f);
        b.Ell(Body, bc, br, OliveGreen);
        b.Ell(Body, bc + V(0, 0.07f, -0.005f), V(0.065f, 0.08f, 0.06f), OliveGreen);
        int head = b.Head(bc + V(0, 0.15f, 0));
        var oc = bc + V(0, 0.19f, -0.005f);
        b.Ell(head, oc, V(0.055f, 0.05f, 0.055f), OliveLight);
        b.PaintEll(head, oc + V(0, 0.05f, 0), V(0.018f, 0.01f, 0.018f), OliveSocket, soft: 0.005f);
        PokeBuilder.Both(s =>
            Frond(b, head, oc + V(0.02f * s, -0.04f, 0.01f), oc + V(0.12f * s, -0.075f, 0.03f), 0.034f, OliveLeaf, V(0, 1f, 0.3f), 0.25f, Leaf));
        Frond(b, head, oc + V(0, -0.04f, 0.03f), oc + V(0.015f, -0.085f, 0.085f), 0.026f, OliveLeaf, V(0, 0.5f, 1f), 0.25f, Leaf);
        PokeBuilder.Both(s =>
        {
            var at = On(bc, br, 0.035f * s, bc.Y + 0.005f);
            b.Eye(Body, at, Outward(bc, br, at), 0.016f, sclera: true, pupil: Rgb(30, 30, 30));
        });
        var mouth = On(bc, br, 0, bc.Y - 0.04f);
        b.Mark(Body, mouth, Outward(bc, br, mouth), 0.024f, 0.008f, Rgb(150, 60, 72), MarkShape.Wave);
        return b;
    }

    /// <summary>Dolliv: a little olive doll, a pale green face under a crown of leaves with a yellow-green olive at each side like bunches of hair, a skirt of leaves pale beneath, leaf arms and thin pale legs.</summary>
    private static PokeBuilder Dolliv()
    {
        var b = new PokeBuilder("Dolliv", 0.62f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Leaf };
        var pale = Rgb(232, 238, 204);
        var face = Rgb(216, 234, 166);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.025f * s, 0.14f, 0));
            b.Limb(leg, V(0.025f * s, 0.15f, 0), V(0.03f * s, 0.025f, 0.01f), 0.014f, 0.012f, pale);
            b.Ell(leg, V(0.03f * s, 0.014f, 0.02f), V(0.016f, 0.014f, 0.024f), pale);
        });
        var bc = V(0, 0.2f, 0);
        b.Ell(Body, bc + V(0, 0.03f, 0), V(0.04f, 0.07f, 0.035f), OliveLeaf);
        // The skirt of leaves: pale under, green above
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.2f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = bc + V(0, 0.02f, 0) + dir * 0.02f;
            var tip = bc + dir * 0.12f + V(0, -0.11f, 0);
            Frond(b, Body, root, tip, 0.045f, OliveLeaf, dir + V(0, 0.5f, 0), 0.22f, Leaf, 0.01f);
            b.PaintEll(Body, Vector3.Lerp(root, tip, 0.85f), V(0.03f, 0.02f, 0.03f), pale, soft: 0.008f);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.035f * s, 0.07f, 0));
            Frond(b, arm, bc + V(0.03f * s, 0.07f, 0), bc + V(0.13f * s, 0.05f, 0.03f), 0.022f, OliveLeaf, V(0, 1f, 0.3f), 0.25f, Leaf);
        });
        int head = b.Head(bc + V(0, 0.1f, 0));
        var c = bc + V(0, 0.17f, 0.01f);
        var r = V(0.07f, 0.07f, 0.065f);
        b.Ell(head, c, r, face);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.09f * s, 0.05f, -0.01f), V(0.06f, 0.04f, 0.045f), OliveLight, V(0, 0, -12f * s), blend: 0.015f);
            b.PaintEll(head, c + V(0.15f * s, 0.06f, -0.01f), V(0.012f, 0.016f, 0.016f), OliveSocket, soft: 0.005f);
            var at = On(c, r, c.X + 0.028f * s, c.Y - 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, sclera: true, pupil: Rgb(30, 30, 30));
        });
        // A crown of four leaves standing up
        foreach (var d in new[] { V(0.5f, 1f, 0.2f), V(-0.5f, 1f, 0.2f), V(0.3f, 1f, -0.5f), V(-0.3f, 1f, -0.5f) })
            Frond(b, head, c + V(0, 0.06f, 0), c + V(0, 0.06f, 0) + Vector3.Normalize(d) * 0.08f, 0.026f, OliveLeaf, V(d.X, 0, d.Z), 0.25f, Leaf);
        var mouth = On(c, r, c.X, c.Y - 0.04f);
        b.Mark(head, mouth, Outward(c, r, mouth), 0.012f, 0.004f, Rgb(70, 90, 50), MarkShape.Bar);
        return b;
    }

    /// <summary>Arboliva: a tall olive tree of a lady, her trunk rising from spreading roots, long green leafy hair falling down her front from a calm pale face with closed eyes, and two great boughs spread out like arms, leafy above, dark olives hanging under them.</summary>
    private static PokeBuilder Arboliva()
    {
        var b = new PokeBuilder("Arboliva", 1.0f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Leaf };
        var trunk = Rgb(176, 124, 66);
        var bark = Rgb(124, 84, 46);
        var green = Rgb(64, 140, 64);
        var light = Rgb(110, 176, 80);
        var face = Rgb(242, 228, 196);
        var olive = Rgb(84, 50, 92);
        // Roots spreading out over the ground, two of them its legs
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.26f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            int bone = i == 0 ? b.Leg(1f, V(0.03f, 0.2f, 0.03f)) : i == 5 ? b.Leg(-1f, V(-0.03f, 0.2f, 0.03f)) : Body;
            b.Tube(bone, Smooth(3, V(0, 0.22f, 0) + dir * 0.03f, V(0, 0.09f, 0) + dir * 0.08f, dir * 0.19f + V(0, 0.016f, 0)), 0.034f, 0.016f, trunk, blend: 0.01f);
        }
        b.Limb(Body, V(0, 0.18f, 0), V(0, 0.55f, 0), 0.07f, 0.045f, trunk);
        b.PaintEll(Body, V(0, 0.34f, 0.055f), V(0.014f, 0.12f, 0.03f), bark, soft: 0.006f);
        b.Ell(Body, V(0, 0.56f, 0), V(0.065f, 0.075f, 0.055f), green);
        b.Ell(Body, V(0, 0.63f, 0.005f), V(0.05f, 0.02f, 0.045f), face, blend: 0.01f);
        // The boughs: up and out from her shoulders, leaves standing up along them and olives hanging beneath
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.6f, 0));
            var path = Smooth(3, V(0.04f * s, 0.6f, 0), V(0.2f * s, 0.56f, 0.02f), V(0.36f * s, 0.58f, 0.0f), V(0.48f * s, 0.68f, -0.02f));
            b.Tube(arm, path, 0.028f, 0.015f, trunk, blend: 0.01f);
            for (int i = 1; i < path.Length; i++)
            {
                var p = path[i];
                float t = (float)i / (path.Length - 1);
                Frond(b, arm, p + V(0, 0.01f, 0), p + V(0.04f * s * t, 0.09f - 0.02f * t, -0.01f), 0.032f, i % 2 == 0 ? green : light, V(0, 0, 1f), 0.22f, Leaf, 0.01f);
                if (i % 2 == 1 && i < path.Length - 1)
                {
                    b.Limb(arm, p, p + V(0, -0.04f, 0.01f), 0.005f, 0.005f, bark, blend: 0.004f);
                    b.Ell(arm, p + V(0, -0.065f, 0.01f), V(0.024f, 0.03f, 0.024f), olive, mat: Shell, blend: 0.008f);
                }
            }
        });
        int head = b.Head(V(0, 0.65f, 0));
        var c = V(0, 0.74f, 0.015f);
        var r = V(0.075f, 0.085f, 0.07f);
        b.Ell(head, c, r, face);
        // Leafy hair over her crown and the back of her head, falling in two long locks down her front
        b.Ell(head, c + V(0, 0.035f, -0.032f), V(0.084f, 0.076f, 0.066f), green, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            Frond(b, head, c + V(0.064f * s, -0.01f, 0.02f), V(0.016f * s, 0.46f, 0.075f), 0.038f, green, V(0, 0, 1f), 0.22f, Leaf, 0.01f);
            Frond(b, head, c + V(0.058f * s, 0.07f, -0.01f), c + V(0.11f * s, 0.145f, -0.03f), 0.028f, light, V(s, 0, 0.3f), 0.25f, Leaf);
            var at = On(c, r, c.X + 0.028f * s, c.Y - 0.014f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, closed: true);
        });
        Frond(b, head, c + V(0, 0.09f, -0.01f), c + V(0, 0.19f, 0.0f), 0.03f, light, V(0, 0, 1f), 0.25f, Leaf);
        return b;
    }

    // ------------------------------------------------------------------ Squawkabilly

    /// <summary>
    /// Squawkabilly in its <paramref name="plumage"/> (Green, Blue, Yellow or White): a scowling parrot with a white
    /// breast, a great orange beak, a black crest teased up like a pompadour, heavy-lidded eyes, one wing raised with
    /// pale flight feathers and long tail feathers.
    /// </summary>
    private static PokeBuilder SquawkabillyBuild(string plumage)
    {
        var (coat, feather) = plumage switch
        {
            "Blue" => (Rgb(56, 152, 222), Rgb(236, 242, 250)),
            "Yellow" => (Rgb(238, 210, 64), Rgb(250, 244, 214)),
            "White" => (Rgb(222, 228, 238), Rgb(246, 248, 252)),
            _ => (Rgb(98, 178, 72), Rgb(240, 240, 232)),
        };
        string name = plumage == "Green" ? "Squawkabilly" : $"Squawkabilly-{plumage}-Plumage";
        var b = new PokeBuilder(name, 0.62f, BodyPlan.Bird, V(0, 0.2f, 0)) { Coat = Fur };
        var breast = Rgb(244, 244, 240);
        var beak = Rgb(246, 168, 40);
        var crest = Rgb(66, 66, 74);
        BirdLegs(b, 0.035f, 0.11f, 0.0f, 0.011f, Rgb(232, 180, 80));
        var bc = V(0, 0.2f, 0);
        var br = V(0.075f, 0.1f, 0.07f);
        b.Ell(Body, bc, br, coat);
        b.PaintEll(Body, bc + V(0, 0.01f, 0.055f), V(0.055f, 0.08f, 0.035f), breast);
        // The left wing folded at its side, the right raised as if to make a point
        FoldedWing(b, -1f, bc + V(-0.06f, 0.05f, 0), bc + V(-0.08f, -0.08f, -0.08f), 0.045f, coat, feather);
        SpreadWing(b, 1f, bc + V(0.06f, 0.05f, 0), bc + V(0.13f, 0.14f, 0.02f), 5, 60f, 105f, 0.13f, 0.024f, coat, feather, 0.15f, tipFrom: 0);
        int tail = b.Tail(bc + V(0, -0.06f, -0.06f));
        TailFan(b, tail, bc + V(0, -0.06f, -0.06f), 3, 30f, 0.15f, 0.025f, coat, coat, 0.6f);
        int head = b.Head(bc + V(0, 0.09f, 0.01f));
        var c = bc + V(0, 0.14f, 0.02f);
        var r = V(0.06f, 0.058f, 0.058f);
        b.Ell(head, c, r, coat);
        // The great hooked beak
        b.Ell(head, c + V(0, -0.015f, 0.06f), V(0.034f, 0.03f, 0.03f), beak, blend: 0.01f);
        b.Spike(head, c + V(0, -0.01f, 0.08f), c + V(0, -0.05f, 0.1f), 0.024f, beak, 0.7f, Shell, 0.008f);
        b.Ell(head, c + V(0, -0.045f, 0.055f), V(0.022f, 0.014f, 0.022f), Rgb(228, 140, 40), blend: 0.006f);
        // The teased-up crest
        foreach (var (x, y, z, rr) in new[] { (0f, 0.065f, 0f, 0.03f), (-0.025f, 0.075f, -0.02f, 0.026f), (0.025f, 0.075f, -0.02f, 0.026f),
                     (0f, 0.09f, -0.03f, 0.028f), (0f, 0.07f, 0.025f, 0.024f) })
            b.Ell(head, c + V(x, y, z), V(rr, rr, rr), crest, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.032f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(90, 110, 50), sclera: true, glare: true);
        });
        return b;
    }

    private static PokeBuilder Squawkabilly() => SquawkabillyBuild("Green");

    // ------------------------------------------------------------------ Nacli line

    private static readonly Color SaltWhite = Rgb(244, 242, 240);
    private static readonly Color SaltRock = Rgb(168, 118, 96);
    private static readonly Color SaltRockLight = Rgb(208, 174, 156);
    private static readonly Color SaltRockDark = Rgb(112, 74, 62);
    private static readonly Color SaltEye = Rgb(250, 152, 40);

    /// <summary>The two glowing orange slits that are the Nacli line's eyes, on a face looking along <paramref name="facing"/>.</summary>
    private static void SaltEyes(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float apart, float size)
    {
        PokeBuilder.Both(s => b.Mark(bone, at + V(apart * s, 0, 0), facing, size * 0.5f, size, SaltEye, MarkShape.Bar));
    }

    /// <summary>Nacli: a lump of rock salt, brown and banded pale, a cube of white salt standing on its back, and below a white salt face with two orange slits for eyes.</summary>
    private static PokeBuilder Nacli()
    {
        var b = new PokeBuilder("Nacli", 0.42f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Shell };
        var bc = V(0, 0.17f, -0.01f);
        // The salt block of its face, under the rock
        b.Box(Body, V(0, 0.065f, 0.06f), V(0.06f, 0.06f, 0.05f), 0.012f, SaltWhite);
        b.Box(Body, bc, V(0.12f, 0.075f, 0.1f), 0.035f, SaltRock, V(5f, 20f, -6f), blend: 0.015f);
        b.Box(Body, bc + V(-0.07f, -0.02f, 0.02f), V(0.07f, 0.07f, 0.075f), 0.03f, SaltRock, V(-10f, -15f, 12f), blend: 0.015f);
        b.Box(Body, bc + V(0.08f, -0.02f, -0.01f), V(0.065f, 0.065f, 0.07f), 0.03f, SaltRock, V(8f, 30f, -10f), blend: 0.015f);
        foreach (float y in new[] { -0.03f, 0.03f })
            b.PaintEll(Body, bc + V(0, y, 0), V(0.18f, 0.014f, 0.16f), SaltRockLight, V(0, 0, -6f), 0.01f);
        b.PaintEll(Body, bc + V(0, 0.085f, 0), V(0.14f, 0.03f, 0.13f), SaltRockDark, V(0, 0, -6f), 0.012f);
        int head = b.Head(V(0.02f, 0.25f, -0.04f));
        b.Box(head, V(0.025f, 0.29f, -0.045f), V(0.045f, 0.045f, 0.045f), 0.008f, SaltWhite, V(12f, 25f, 10f));
        SaltEyes(b, Body, V(0, 0.07f, 0.11f), V(0, 0, 1f), 0.024f, 0.017f);
        return b;
    }

    /// <summary>Naclstack: a long block of banded rock salt, darker behind, on four legs of white salt, a white salt block for a head with two orange slits for eyes.</summary>
    private static PokeBuilder Naclstack()
    {
        var b = new PokeBuilder("Naclstack", 0.62f, BodyPlan.Quadruped, V(0, 0.2f, -0.04f)) { Coat = Shell };
        foreach (var (z, front) in new[] { (0.07f, true), (-0.15f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.08f * s, 0.12f, z), front);
                b.Box(leg, V(0.085f * s, 0.07f, z), V(0.045f, 0.07f, 0.045f), 0.01f, SaltWhite, V(0, 8f * s, 0));
            });
        var bc = V(0, 0.2f, -0.05f);
        b.Box(Body, bc, V(0.1f, 0.07f, 0.15f), 0.03f, SaltRock);
        b.Box(Body, bc + V(0, 0.01f, -0.1f), V(0.095f, 0.065f, 0.07f), 0.03f, SaltRockDark, blend: 0.01f);
        foreach (float y in new[] { -0.03f, 0.02f })
            b.PaintEll(Body, bc + V(0, y, 0.04f), V(0.12f, 0.01f, 0.14f), SaltRockLight, soft: 0.008f);
        int head = b.Head(bc + V(0, 0.03f, 0.14f));
        var hc = bc + V(0, 0.05f, 0.17f);
        b.Box(head, hc, V(0.075f, 0.07f, 0.055f), 0.012f, SaltWhite);
        b.Box(head, hc + V(-0.03f, 0.08f, -0.01f), V(0.035f, 0.03f, 0.035f), 0.008f, SaltWhite, V(0, 20f, 8f), blend: 0.004f);
        SaltEyes(b, head, hc + V(0, 0.015f, 0.055f), V(0, 0, 1f), 0.026f, 0.02f);
        return b;
    }

    /// <summary>A pillar of Garganacl's from <paramref name="top"/> down to <paramref name="foot"/>, layered brown and white like strata of salt.</summary>
    private static void SaltPillar(PokeBuilder b, int bone, Vector3 top, Vector3 foot, float half)
    {
        var mid = (top + foot) / 2f;
        float h = (top - foot).Length() / 2f;
        var turn = Euler(top - foot);
        b.Box(bone, mid, V(half, h, half), half * 0.3f, SaltRock, turn);
        for (int i = 0; i < 3; i++)
            b.PaintEll(bone, Vector3.Lerp(foot, top, 0.15f + i * 0.28f), V(half * 1.6f, h * 0.06f, half * 1.6f), SaltWhite, turn, 0.008f);
        b.PaintEll(bone, foot, V(half * 1.6f, h * 0.35f, half * 1.6f), SaltWhite, turn, 0.01f);
    }

    /// <summary>Garganacl: a giant built of blocks of salt, white blocks heaped over its broad shoulders and a tall one above, a small brown rock face with two orange eyes between them, thick legs and arms of salt layered brown and white, its fists white blocks.</summary>
    private static PokeBuilder Garganacl()
    {
        var b = new PokeBuilder("Garganacl", 1.0f, BodyPlan.Biped, V(0, 0.46f, 0)) { Coat = Shell };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.38f, 0));
            SaltPillar(b, leg, V(0.08f * s, 0.4f, 0), V(0.085f * s, 0.0f, 0.01f), 0.065f);
        });
        b.Box(Body, V(0, 0.46f, 0), V(0.13f, 0.09f, 0.08f), 0.025f, SaltRock);
        // The heap of white blocks over its shoulders
        foreach (var (x, y, z, hx, hy, hz, turn) in new[]
        {
            (0.17f, 0.6f, 0f, 0.13f, 0.065f, 0.1f, 4f), (-0.17f, 0.6f, 0f, 0.13f, 0.065f, 0.1f, -4f),
            (0.1f, 0.72f, -0.01f, 0.085f, 0.06f, 0.085f, -6f), (-0.1f, 0.72f, -0.01f, 0.085f, 0.06f, 0.085f, 6f),
            (0f, 0.84f, -0.03f, 0.05f, 0.1f, 0.05f, 0f),
        })
            b.Box(Body, V(x, y, z), V(hx, hy, hz), 0.012f, SaltWhite, V(0, 0, turn));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.27f * s, 0.56f, 0));
            var fist = s > 0 ? V(0.33f * s, 0.1f, 0.03f) : V(0.32f * s, 0.18f, 0.14f);
            SaltPillar(b, arm, V(0.28f * s, 0.58f, 0), fist + V(0, 0.07f, -0.01f), 0.055f);
            b.Box(arm, fist, V(0.075f, 0.075f, 0.075f), 0.014f, SaltWhite, V(s > 0 ? 0f : 20f, 15f * s, 0));
        });
        int head = b.Head(V(0, 0.6f, 0.04f));
        var hc = V(0, 0.62f, 0.1f);
        b.Box(head, hc, V(0.05f, 0.07f, 0.045f), 0.014f, SaltRock);
        b.Box(head, hc + V(0, 0.085f, -0.01f), V(0.03f, 0.025f, 0.03f), 0.008f, SaltRockDark, V(0, 0, 10f), blend: 0.006f);
        SaltEyes(b, head, hc + V(0, 0.015f, 0.045f), V(0, 0, 1f), 0.022f, 0.02f);
        return b;
    }

    // ------------------------------------------------------------------ Charcadet line

    private static readonly Color CoalBlack = Rgb(50, 42, 46);
    private static readonly Color EmberRed = Rgb(224, 60, 42);
    private static readonly Color EmberOrange = Rgb(246, 142, 44);
    private static readonly Color EmberYellow = Rgb(252, 214, 72);

    /// <summary>Charcadet: a little fire spirit with a great round head of charcoal, open at the front on a red face with one yellow eye, a flame burning on top, a thin black body marked with red flame, red arms and black legs ending in red feet, and flames at its back.</summary>
    private static PokeBuilder Charcadet()
    {
        var b = new PokeBuilder("Charcadet", 0.62f, BodyPlan.Biped, V(0, 0.27f, 0)) { Coat = Fur };
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.21f, 0));
            var knee = V(0.05f * s, 0.12f, 0.01f);
            b.Limb(leg, V(0.035f * s, 0.22f, 0), knee, 0.022f, 0.018f, CoalBlack);
            b.Limb(leg, knee, V(0.05f * s, 0.035f, 0.0f), 0.018f, 0.02f, CoalBlack);
            b.Ell(leg, V(0.05f * s, 0.022f, 0.015f), V(0.024f, 0.022f, 0.032f), EmberRed);
        });
        var bc = V(0, 0.27f, 0);
        b.Ell(Body, bc, V(0.045f, 0.065f, 0.035f), CoalBlack);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.03f), V(0.025f, 0.04f, 0.02f), EmberRed, soft: 0.006f);
        foreach (var d in new[] { V(0.6f, 0.6f, 0.4f), V(-0.6f, 0.6f, 0.4f), V(0, 0.4f, 1f) })
            b.Spike(Body, bc + V(0, 0.05f, 0.01f), bc + V(0, 0.05f, 0.01f) + Vector3.Normalize(d) * 0.05f, 0.016f, EmberRed, 0.5f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.04f * s, 0.04f, 0));
            var hand = bc + V(0.1f * s, -0.03f, 0.03f);
            b.Limb(arm, bc + V(0.04f * s, 0.04f, 0), hand, 0.016f, 0.015f, EmberRed);
            b.Ell(arm, hand, V(0.022f, 0.02f, 0.02f), EmberRed);
        });
        int back = b.Part("flameBack", Body, bc + V(0.02f, -0.02f, -0.03f), PokeRole.Flame, 1.3f);
        Flame(b, back, bc + V(0.02f, -0.04f, -0.03f), 0.22f);
        int head = b.Head(bc + V(0, 0.07f, 0));
        var c = bc + V(0, 0.17f, 0.01f);
        var r = V(0.1f, 0.1f, 0.095f);
        b.Limb(head, bc + V(0, 0.05f, 0), c + V(0, -0.06f, 0), 0.025f, 0.03f, CoalBlack);
        b.Ell(head, c, r, CoalBlack);
        // The face showing where the charcoal shell opens, toward its right
        b.PaintEll(head, c + V(0.045f, -0.03f, 0.075f), V(0.075f, 0.075f, 0.06f), EmberRed);
        b.PaintEll(head, c + V(0.085f, -0.03f, 0.04f), V(0.03f, 0.07f, 0.06f), EmberOrange, soft: 0.01f);
        var at = On(c, r, c.X + 0.045f, c.Y - 0.035f);
        b.Eye(head, at, Outward(c, r, at), 0.017f, white: EmberYellow, pupil: Rgb(200, 40, 30));
        int top = b.Part("flame", head, c + V(0, 0.09f, 0), PokeRole.Flame, 0.4f);
        Flame(b, top, c + V(0, 0.06f, -0.01f), 0.45f);
        b.PaintEll(top, c + V(0.01f, 0.21f, -0.02f), V(0.045f, 0.04f, 0.045f), Rgb(200, 90, 200), soft: 0.015f);
        return b;
    }

    /// <summary>Armarouge: a knight of fire in yellow armour, great round pauldrons layered over its shoulders, a red stripe down its breastplate, a black helm with yellow eyes and a flame for a crest, dark red arms and long red legs black at the hips.</summary>
    private static PokeBuilder Armarouge()
    {
        var b = new PokeBuilder("Armarouge", 1.0f, BodyPlan.Biped, V(0, 0.64f, 0)) { Coat = Metal };
        var gold = Rgb(246, 214, 46);
        var darkRed = Rgb(140, 38, 40);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.055f * s, 0.52f, 0));
            var knee = V(0.065f * s, 0.29f, 0.02f);
            b.Limb(leg, V(0.055f * s, 0.53f, 0), knee, 0.042f, 0.034f, CoalBlack, Fur);
            b.Limb(leg, knee, V(0.07f * s, 0.05f, 0.0f), 0.034f, 0.038f, EmberRed, Fur);
            b.PaintEll(leg, V(0.068f * s, 0.18f, 0.01f), V(0.045f, 0.12f, 0.045f), EmberOrange, soft: 0.03f);
            b.Ell(leg, V(0.07f * s, 0.03f, 0.02f), V(0.04f, 0.03f, 0.055f), EmberRed);
        });
        var bc = V(0, 0.66f, 0);
        b.Ell(Body, bc, V(0.08f, 0.12f, 0.06f), gold);
        b.Ell(Body, bc + V(0, -0.11f, 0), V(0.06f, 0.04f, 0.045f), CoalBlack, blend: 0.02f);
        PokeBuilder.Both(s => b.PaintEll(Body, bc + V(0.03f * s, 0.01f, 0.05f), V(0.045f, 0.01f, 0.03f), EmberRed, V(0, 0, 25f * s), 0.005f));
        b.PaintEll(Body, bc + V(0, -0.06f, 0.05f), V(0.008f, 0.04f, 0.03f), EmberRed, soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.1f * s, 0.08f, 0));
            b.Ell(arm, bc + V(0.16f * s, 0.11f, 0), V(0.1f, 0.075f, 0.09f), gold, V(0, 0, -15f * s));
            b.Ell(arm, bc + V(0.19f * s, 0.04f, 0.01f), V(0.085f, 0.06f, 0.08f), gold, V(0, 0, -10f * s), blend: 0.008f);
            var hand = bc + V(0.2f * s, -0.15f, 0.06f);
            b.Limb(arm, bc + V(0.19f * s, 0.0f, 0.02f), hand, 0.036f, 0.042f, darkRed, Fur);
            b.Ell(arm, hand + V(0, -0.02f, 0.01f), V(0.04f, 0.04f, 0.04f), Rgb(110, 30, 34));
        });
        int head = b.Head(bc + V(0, 0.13f, 0.01f));
        var c = bc + V(0, 0.21f, 0.01f);
        var r = V(0.055f, 0.07f, 0.06f);
        b.Limb(head, bc + V(0, 0.1f, 0), c + V(0, -0.05f, 0), 0.03f, 0.03f, CoalBlack);
        b.Ell(head, c, r, CoalBlack);
        b.Spike(head, c + V(0, -0.03f, 0.04f), c + V(0, -0.075f, 0.05f), 0.035f, CoalBlack, 0.6f);
        b.PaintEll(head, c + V(0, 0.04f, 0.04f), V(0.008f, 0.04f, 0.03f), EmberRed, soft: 0.005f);
        PokeBuilder.Both(s =>
        {
            b.Torus(head, c + V(0.055f * s, -0.02f, 0), 0.016f, 0.005f, gold, V(0, 0, 90f), mat: Metal);
            var at = On(c, r, c.X + 0.022f * s, c.Y + 0.0f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, EmberYellow, glare: true);
        });
        int top = b.Part("flame", head, c + V(0, 0.07f, -0.02f), PokeRole.Flame, 0.4f);
        Flame(b, top, c + V(0, 0.05f, -0.02f), 0.5f);
        return b;
    }

    /// <summary>Ceruledge: a slender swordsman in dark violet armour, two long blades of ghostly blue fire running down from its forearms, a narrow helm drawn to a point with pink eyes and a plume of blue fire.</summary>
    private static PokeBuilder Ceruledge()
    {
        var b = new PokeBuilder("Ceruledge", 1.0f, BodyPlan.Biped, V(0, 0.68f, 0)) { Coat = Metal };
        var navy = Rgb(48, 44, 104);
        var violet = Rgb(92, 80, 168);
        var dark = Rgb(30, 28, 62);
        var blade = Rgb(150, 170, 246);
        var bladeTip = Rgb(200, 150, 236);
        var fire = Rgb(96, 128, 246);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.55f, 0));
            var knee = V(0.07f * s, 0.32f, 0.03f);
            b.Limb(leg, V(0.05f * s, 0.56f, 0), knee, 0.04f, 0.028f, navy);
            b.Spike(leg, knee + V(0, 0.02f, 0), knee + V(0.01f * s, 0.07f, 0.05f), 0.026f, violet, 0.5f);
            b.Limb(leg, knee, V(0.075f * s, 0.06f, 0.0f), 0.03f, 0.022f, navy);
            b.Spike(leg, V(0.075f * s, 0.05f, -0.02f), V(0.075f * s, 0.01f, 0.08f), 0.03f, dark, 0.5f);
        });
        var bc = V(0, 0.7f, 0);
        b.Ell(Body, bc, V(0.06f, 0.11f, 0.048f), navy);
        b.PaintEll(Body, bc + V(0, 0.04f, 0.04f), V(0.045f, 0.05f, 0.02f), violet, soft: 0.008f);
        b.Ell(Body, bc + V(0, -0.12f, 0), V(0.055f, 0.045f, 0.045f), dark, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            b.Spike(Body, bc + V(0.06f * s, 0.08f, 0), bc + V(0.12f * s, 0.14f, -0.01f), 0.035f, violet, 0.45f);
            int arm = b.Arm(s, bc + V(0.07f * s, 0.07f, 0));
            var elbow = bc + V(0.11f * s, -0.02f, 0.02f);
            var hand = bc + V(0.12f * s, -0.12f, 0.06f);
            b.Limb(arm, bc + V(0.07f * s, 0.07f, 0), elbow, 0.026f, 0.022f, navy);
            b.Limb(arm, elbow, hand, 0.026f, 0.024f, violet);
            // A ghostly guard of blue fire, and the blade running down from it
            foreach (var d in new[] { V(0.5f * s, 0.9f, 0.2f), V(0.9f * s, 0.3f, 0.1f) })
                b.Spike(arm, hand, hand + Vector3.Normalize(d) * 0.06f, 0.014f, fire, 0.5f, Glow, 0.006f);
            var tip = hand + V(0.2f * s, -0.42f, 0.06f);
            Blade(b, arm, hand, tip, 0.05f, blade, V(0, 0, 1f), 0.16f, Glow);
            // A ridge down the blade, which keeps its thin point whole
            b.Limb(arm, hand, tip, 0.008f, 0.006f, blade, Glow, 0.006f);
            b.PaintEll(arm, Vector3.Lerp(hand, tip, 0.85f), V(0.04f, 0.08f, 0.04f), bladeTip, Euler(tip - hand), 0.03f);
        });
        int head = b.Head(bc + V(0, 0.13f, 0.01f));
        var c = bc + V(0, 0.2f, 0.01f);
        var r = V(0.045f, 0.06f, 0.05f);
        b.Limb(head, bc + V(0, 0.1f, 0), c + V(0, -0.04f, 0), 0.024f, 0.024f, dark);
        b.Ell(head, c, r, navy);
        b.Spike(head, c + V(0, -0.02f, 0.03f), c + V(0, -0.06f, 0.06f), 0.03f, navy, 0.5f);
        b.Spike(head, c + V(0, 0.03f, -0.02f), c + V(0, 0.06f, -0.1f), 0.03f, violet, 0.35f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.018f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(246, 110, 210), glare: true);
        });
        // A plume of blue fire on its helm
        int top = b.Part("flame", head, c + V(0, 0.06f, -0.01f), PokeRole.Flame, 0.4f);
        b.Ell(top, c + V(0, 0.08f, -0.01f), V(0.03f, 0.04f, 0.03f), fire, mat: Glow, blend: 0.012f);
        b.Spike(top, c + V(0, 0.1f, -0.01f), c + V(0.02f, 0.18f, -0.03f), 0.026f, fire, mat: Glow, blend: 0.012f);
        b.PaintEll(top, c + V(0, 0.08f, 0.015f), V(0.02f, 0.03f, 0.02f), Rgb(190, 210, 255), soft: 0.012f);
        return b;
    }

    // ------------------------------------------------------------------ Tadbulb and Bellibolt

    /// <summary>Tadbulb: a tadpole that is a light bulb, its round yellow head glowing, a dark face on its front with yellow eyes and a smile, a brown socket below and a dark tail ending in an orange paddle.</summary>
    private static PokeBuilder Tadbulb()
    {
        var b = new PokeBuilder("Tadbulb", 0.42f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Shell }.Hover();
        var yellow = Rgb(250, 222, 62);
        var face = Rgb(96, 84, 76);
        var socket = Rgb(196, 122, 62);
        var c = V(0, 0.22f, 0);
        var r = V(0.1f, 0.1f, 0.095f);
        b.Ell(Body, c, r, yellow, mat: Glow);
        b.PaintEll(Body, c + V(0, -0.035f, 0.07f), V(0.08f, 0.06f, 0.05f), face, soft: 0.01f);
        b.PaintEll(Body, c + V(0.045f, 0.06f, 0.04f), V(0.025f, 0.02f, 0.02f), Rgb(255, 252, 230), V(0, 0, -30f), 0.008f);
        b.Ell(Body, c + V(0, -0.1f, -0.01f), V(0.05f, 0.03f, 0.05f), socket, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.03f * s, c.Y - 0.025f);
            b.Eye(Body, at, Outward(c, r, at), 0.012f, Rgb(252, 214, 80));
        });
        var mouth = On(c, r, c.X, c.Y - 0.06f);
        b.Mark(Body, mouth, Outward(c, r, mouth), 0.02f, 0.01f, Rgb(252, 214, 80), MarkShape.Smile);
        int tail = b.Tail(c + V(0, -0.11f, -0.02f));
        var tp = Smooth(3, c + V(0, -0.11f, -0.02f), c + V(0, -0.16f, -0.05f), c + V(0, -0.17f, -0.11f));
        b.Tube(tail, tp, 0.022f, 0.02f, face, blend: 0f);
        b.Ell(tail, tp[^1] + V(0, 0.0f, -0.035f), V(0.04f, 0.014f, 0.05f), Rgb(240, 150, 64), V(10f, 0, 0), blend: 0.01f);
        return Lift(b);
    }

    /// <summary>Bellibolt: a great round green frog of a Pokémon, a dark bulb filling its belly with a yellow light at its heart, googly white eyes on top, a wide smile, orange spots, stubby arms and little orange feet.</summary>
    private static PokeBuilder Bellibolt()
    {
        var b = new PokeBuilder("Bellibolt", 0.9f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Scales };
        var green = Rgb(36, 172, 142);
        var bulb = Rgb(84, 74, 68);
        var orange = Rgb(232, 142, 62);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.07f, 0.02f));
            b.Ell(leg, V(0.1f * s, 0.035f, 0.05f), V(0.05f, 0.035f, 0.06f), orange);
        });
        var bc = V(0, 0.2f, 0);
        var br = V(0.2f, 0.18f, 0.17f);
        b.Ell(Body, bc, br, green);
        b.Ell(Body, bc + V(0, -0.02f, 0.11f), V(0.13f, 0.12f, 0.08f), bulb, blend: 0.02f);
        b.Ell(Body, bc + V(0, -0.01f, 0.185f), V(0.028f, 0.036f, 0.012f), Rgb(252, 232, 96), mat: Glow, blend: 0.008f);
        foreach (var (x, y) in new[] { (0.17f, 0.04f), (-0.17f, 0.0f), (0.15f, -0.09f), (-0.1f, 0.13f) })
            b.PaintEll(Body, Out(bc, br, default, V(x, y, 0.02f)), V(0.025f, 0.02f, 0.025f), orange, soft: 0.006f);
        b.Mark(Body, Out(bc, br, default, V(0, 0.4f, 1f)), Outward(bc, br, Out(bc, br, default, V(0, 0.4f, 1f))), 0.07f, 0.025f, Rgb(18, 96, 78), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.18f * s, -0.02f, 0.04f));
            b.Limb(arm, bc + V(0.17f * s, -0.02f, 0.04f), bc + V(0.25f * s, -0.04f, 0.08f), 0.03f, 0.028f, green);
            b.Ell(arm, bc + V(0.26f * s, -0.045f, 0.09f), V(0.03f, 0.028f, 0.03f), orange);
        });
        int head = b.Head(bc + V(0, 0.15f, 0.02f));
        PokeBuilder.Both(s =>
        {
            var ec = bc + V(0.085f * s, 0.17f, 0.07f);
            var er = V(0.05f, 0.048f, 0.045f);
            b.Ell(head, ec, er, Rgb(250, 250, 246), blend: 0.015f);
            var at = Out(ec, er, default, V(0.15f * s, 0.15f, 1f));
            b.Eye(head, at, Outward(ec, er, at), 0.02f, sclera: true, pupil: Rgb(30, 30, 34));
        });
        return b;
    }

    // ------------------------------------------------------------------ Wattrel and Kilowattrel

    private static readonly Color StormBlack = Rgb(50, 50, 62);
    private static readonly Color StormYellow = Rgb(246, 200, 62);

    /// <summary>Wattrel: a little storm petrel, black above, a band of yellow across its breast and pale grey ragged feathers below, great white eyes, an orange beak, wings marked yellow and a long forked tail.</summary>
    private static PokeBuilder Wattrel()
    {
        var b = new PokeBuilder("Wattrel", 0.5f, BodyPlan.Bird, V(0, 0.17f, 0)) { Coat = Fur };
        var grey = Rgb(154, 160, 186);
        var orange = Rgb(240, 158, 76);
        BirdLegs(b, 0.03f, 0.1f, 0.0f, 0.009f, orange);
        var bc = V(0, 0.17f, -0.01f);
        var br = V(0.06f, 0.065f, 0.08f);
        b.Ell(Body, bc, br, StormBlack, V(-15f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.03f, 0.04f), V(0.06f, 0.045f, 0.06f), grey);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.05f), V(0.065f, 0.025f, 0.05f), StormYellow, V(-20f, 0, 0), 0.008f);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.Spike(Body, bc + V(x, -0.04f, 0.04f), bc + V(x * 1.2f, -0.075f, 0.05f), 0.014f, grey, 0.4f);
        PokeBuilder.Both(s =>
        {
            int wing = FoldedWing(b, s, bc + V(0.055f * s, 0.03f, 0.02f), bc + V(0.06f * s, -0.01f, -0.13f), 0.045f, StormBlack, StormBlack);
            b.PaintEll(wing, bc + V(0.06f * s, 0.02f, 0.0f), V(0.03f, 0.025f, 0.035f), StormYellow, soft: 0.008f);
        });
        int tail = b.Tail(bc + V(0, 0.0f, -0.08f));
        PokeBuilder.Both(s => Blade(b, tail, bc + V(0.01f * s, 0.0f, -0.07f), bc + V(0.05f * s, -0.01f, -0.22f), 0.022f, StormBlack, V(0, 1f, 0), 0.3f));
        int head = b.Head(bc + V(0, 0.06f, 0.04f));
        var c = bc + V(0, 0.1f, 0.06f);
        var r = V(0.05f, 0.05f, 0.05f);
        b.Ell(head, c, r, StormBlack);
        b.Spike(head, c + V(0, -0.012f, 0.04f), c + V(0, -0.02f, 0.1f), 0.02f, orange, 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.028f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.018f, sclera: true, pupil: Rgb(30, 30, 36));
        });
        return b;
    }

    /// <summary>Kilowattrel: a great frigatebird soaring on long narrow wings, yellow along their fronts and black behind and ragged at the edge, a black body with a yellow pouch swelling at its throat, a long hooked beak, red feet and a long forked tail.</summary>
    private static PokeBuilder Kilowattrel()
    {
        var b = new PokeBuilder("Kilowattrel", 1.0f, BodyPlan.Bird, V(0, 0.36f, 0)) { Coat = Fur }.Hover();
        var red = Rgb(170, 60, 60);
        var bc = V(0, 0.36f, 0);
        b.Ell(Body, bc, V(0.075f, 0.075f, 0.16f), StormBlack);
        b.Ell(Body, bc + V(0, -0.045f, 0.09f), V(0.065f, 0.065f, 0.08f), StormYellow, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, bc + V(0.05f * s, 0.03f, 0.03f));
            var shoulder = bc + V(0.05f * s, 0.03f, 0.03f);
            var wrist = bc + V(0.3f * s, 0.15f, 0.0f);
            var tip = bc + V(0.6f * s, 0.09f, -0.12f);
            var face = V(-0.3f * s, 1f, 0.35f);
            b.Limb(wing, shoulder, wrist, 0.024f, 0.015f, StormYellow);
            b.Limb(wing, wrist, tip, 0.015f, 0.007f, StormYellow);
            Blade(b, wing, shoulder + V(0, 0, -0.04f), wrist + V(0, -0.01f, -0.07f), 0.095f, StormBlack, face, 0.15f);
            Blade(b, wing, wrist + V(0, 0, -0.015f), tip + V(0, -0.005f, -0.03f), 0.075f, StormBlack, face, 0.17f);
            b.PaintEll(wing, Vector3.Lerp(shoulder, wrist, 0.5f) + V(0, 0.01f, 0.0f), V(0.04f, 0.16f, 0.04f), StormYellow, Euler(wrist - shoulder), 0.01f);
            b.PaintEll(wing, Vector3.Lerp(wrist, tip, 0.4f) + V(0, 0.005f, 0.0f), V(0.03f, 0.13f, 0.03f), StormYellow, Euler(tip - wrist), 0.01f);
            foreach (float t in new[] { 0.3f, 0.6f })
            {
                var edge = Vector3.Lerp(shoulder, wrist, t) + V(0, -0.01f, -0.12f);
                b.Spike(wing, edge + V(0, 0, 0.04f), edge + V(0.02f * s, -0.01f, -0.04f), 0.016f, StormBlack, 0.35f);
            }
        });
        int tail = b.Tail(bc + V(0, 0, -0.14f));
        PokeBuilder.Both(s => Blade(b, tail, bc + V(0.01f * s, 0.0f, -0.13f), bc + V(0.06f * s, -0.02f, -0.38f), 0.028f, StormBlack, V(0, 1f, 0), 0.3f));
        DanglingLegs(b, 0.03f, bc.Y - 0.05f, -0.06f, 0.05f, 0.011f, red);
        int head = b.Head(bc + V(0, 0.03f, 0.13f));
        var c = bc + V(0, 0.04f, 0.19f);
        var r = V(0.045f, 0.045f, 0.05f);
        b.Ell(head, c, r, StormBlack);
        b.Spike(head, c + V(0, -0.005f, 0.04f), c + V(0, -0.02f, 0.15f), 0.016f, Rgb(232, 170, 80), 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.024f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, sclera: true, pupil: Rgb(30, 30, 36));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Maschiff and Mabosstiff

    /// <summary>Maschiff: a scowling bulldog pup, reddish pink, its jowls dark round a mouth clenched on white teeth, a small orange nose, a crest of yellow hair swept back over its head, floppy ears, a dark band under its belly and a yellow plume of a tail.</summary>
    private static PokeBuilder Maschiff()
    {
        var b = new PokeBuilder("Maschiff", 0.55f, BodyPlan.Quadruped, V(0, 0.15f, -0.04f)) { Coat = Fur };
        var coat = Rgb(210, 102, 98);
        var jowl = Rgb(72, 62, 78);
        var gold = Rgb(244, 202, 66);
        StubbyLegs(b, 0.07f, 0.1f, 0.06f, -0.13f, 0.034f, coat);
        var bc = V(0, 0.15f, -0.04f);
        b.Ell(Body, bc, V(0.09f, 0.08f, 0.14f), coat);
        b.PaintEll(Body, bc + V(0, -0.07f, 0), V(0.07f, 0.03f, 0.12f), jowl);
        int tail = b.Tail(bc + V(0, 0.04f, -0.13f));
        Frond(b, tail, bc + V(0, 0.04f, -0.13f), bc + V(0, 0.11f, -0.27f), 0.03f, gold, V(1f, 0, 0), 0.3f, Fur);
        int head = b.Head(bc + V(0, 0.05f, 0.11f));
        var c = bc + V(0, 0.08f, 0.15f);
        var r = V(0.085f, 0.075f, 0.075f);
        b.Limb(head, bc + V(0, 0.03f, 0.09f), c + V(0, -0.02f, -0.03f), 0.05f, 0.05f, coat);
        b.Ell(head, c, r, coat);
        // The dark jowls, and the white teeth clenched between them
        b.Ell(head, c + V(0, -0.03f, 0.05f), V(0.075f, 0.045f, 0.05f), jowl, blend: 0.015f);
        b.Box(head, c + V(0, -0.025f, 0.1f), V(0.042f, 0.013f, 0.007f), 0.005f, White, mat: Shell, blend: 0.004f);
        b.PaintEll(head, c + V(0, -0.025f, 0.105f), V(0.05f, 0.0025f, 0.012f), jowl, soft: 0.002f);
        b.Ell(head, c + V(0, 0.005f, 0.085f), V(0.016f, 0.012f, 0.012f), Rgb(242, 142, 72), blend: 0.006f);
        // The crest of yellow hair swept back over its head
        foreach (var (x, from, to) in new[] { (0f, V(0, 0.06f, 0.04f), V(0, 0.1f, -0.1f)), (-0.03f, V(-0.03f, 0.05f, 0.02f), V(-0.06f, 0.07f, -0.1f)), (0.03f, V(0.03f, 0.05f, 0.02f), V(0.06f, 0.07f, -0.1f)) })
            Frond(b, head, c + from, c + to, 0.035f, gold, V(x * 10f, 1f, 0.1f), 0.25f, Fur);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.07f * s, 0.03f, -0.01f));
            b.Ell(ear, c + V(0.085f * s, 0.0f, -0.01f), V(0.022f, 0.045f, 0.035f), coat, V(0, 0, 20f * s));
            var at = On(c, r, c.X + 0.034f * s, c.Y + 0.02f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(40, 30, 34), glare: true);
        });
        return b;
    }

    /// <summary>Mabosstiff: a great old guard dog, grey-brown and heavy, a long black beard falling from its dark face over its chest, a silver mane combed back from its brow down its back, bushy silver brows and a small orange nose.</summary>
    private static PokeBuilder Mabosstiff()
    {
        var b = new PokeBuilder("Mabosstiff", 0.95f, BodyPlan.Quadruped, V(0, 0.3f, -0.05f)) { Coat = Fur };
        var coat = Rgb(126, 112, 100);
        var black = Rgb(54, 48, 52);
        var silver = Rgb(198, 198, 206);
        var face = Rgb(84, 70, 64);
        BeastLegs(b, 0.1f, 0.24f, 0.12f, -0.2f, 0.06f, coat, coat);
        var bc = V(0, 0.3f, -0.05f);
        b.Ell(Body, bc, V(0.13f, 0.12f, 0.24f), coat);
        b.Ell(Body, bc + V(0, -0.01f, 0.17f), V(0.12f, 0.13f, 0.09f), black, blend: 0.03f);
        Shag(b, Body, bc + V(0, 0, 0.05f), V(0.13f, 0.12f, 0.2f), bc.Y - 0.02f, 16, 0.06f, 0.025f, coat, 0.6f);
        // The silver mane down its back, from the crown of its head
        b.Ell(Body, bc + V(0, 0.115f, 0.02f), V(0.07f, 0.03f, 0.22f), silver, blend: 0.015f);
        foreach (float x in new[] { -0.04f, 0f, 0.04f })
            b.Spike(Body, bc + V(x, 0.1f, -0.18f), bc + V(x * 2f, 0.06f, -0.3f), 0.03f, silver, 0.4f);
        int tail = b.Tail(bc + V(0, 0.05f, -0.23f));
        b.Spike(tail, bc + V(0, 0.05f, -0.23f), bc + V(0, 0.02f, -0.32f), 0.03f, coat, 0.6f);
        int head = b.Head(bc + V(0, 0.08f, 0.2f));
        var c = bc + V(0, 0.13f, 0.27f);
        var r = V(0.1f, 0.1f, 0.09f);
        b.Limb(head, bc + V(0, 0.06f, 0.18f), c + V(0, -0.02f, -0.04f), 0.08f, 0.08f, black);
        b.Ell(head, c, r, face);
        b.Ell(head, c + V(0, -0.03f, 0.07f), V(0.06f, 0.05f, 0.05f), face, blend: 0.02f);
        b.Ell(head, c + V(0, -0.005f, 0.12f), V(0.016f, 0.012f, 0.01f), Rgb(240, 140, 66), blend: 0.006f);
        // The silver hair combed back from its brow, over its neck to meet the mane down its back
        b.Ell(head, c + V(0, 0.07f, -0.05f), V(0.09f, 0.04f, 0.13f), silver, V(-12f, 0, 0), blend: 0.015f);
        b.Ell(head, c + V(0, 0.04f, -0.16f), V(0.075f, 0.035f, 0.1f), silver, V(-20f, 0, 0), blend: 0.015f);
        // The long black beard
        foreach (float x in new[] { -0.05f, -0.017f, 0.017f, 0.05f })
            Frond(b, head, c + V(x, -0.06f, 0.07f), c + V(x * 1.2f, -0.2f, 0.07f), 0.03f, black, V(0, 0.2f, 1f), 0.3f, Fur);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.04f * s, c.Y + 0.015f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(40, 30, 30));
            b.Ell(head, at + V(0.01f * s, 0.025f, 0.0f), V(0.04f, 0.014f, 0.022f), silver, V(0, 0, -15f * s), blend: 0.008f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Shroodle and Grafaiai

    private static readonly Color PaintMint = Rgb(152, 222, 184);
    private static readonly Color PaintPink = Rgb(228, 62, 142);
    private static readonly Color PaintBlue = Rgb(64, 174, 214);

    /// <summary>Shroodle: a little grey-blue rodent with a great dark head, a big glassy green eye on each side, a long snout with a purple tongue at its tip, a cream tuft curling on its crown and round dark ears.</summary>
    private static PokeBuilder Shroodle()
    {
        var b = new PokeBuilder("Shroodle", 0.4f, BodyPlan.Quadruped, V(0, 0.08f, -0.04f)) { Coat = Fur };
        var grey = Rgb(98, 106, 140);
        var lilac = Rgb(184, 178, 214);
        var dark = Rgb(64, 48, 44);
        StubbyLegs(b, 0.045f, 0.05f, 0.02f, -0.08f, 0.02f, lilac);
        var bc = V(0, 0.08f, -0.04f);
        b.Ell(Body, bc, V(0.07f, 0.055f, 0.09f), grey);
        int tail = b.Tail(bc + V(0, 0.01f, -0.08f));
        b.Tube(tail, Smooth(3, bc + V(0, 0.01f, -0.08f), bc + V(0, 0.0f, -0.14f), bc + V(0, 0.03f, -0.18f)), 0.012f, 0.008f, grey, blend: 0f);
        int head = b.Head(bc + V(0, 0.03f, 0.06f));
        var c = bc + V(0, 0.05f, 0.08f);
        var r = V(0.08f, 0.072f, 0.075f);
        b.Ell(head, c, r, dark);
        b.Ell(head, c + V(0, -0.025f, 0.08f), V(0.034f, 0.027f, 0.05f), dark, blend: 0.015f);
        b.Spike(head, c + V(0, -0.035f, 0.12f), c + V(0, -0.06f, 0.14f), 0.013f, Rgb(140, 100, 200), 0.6f, blend: 0.005f);
        b.Spike(head, c + V(0, 0.06f, 0), c + V(0.015f, 0.12f, -0.02f), 0.026f, Rgb(236, 232, 202), 0.6f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.04f, -0.05f));
            b.Ell(ear, c + V(0.055f * s, 0.045f, -0.06f), V(0.028f, 0.028f, 0.012f), dark, V(0, 30f * s, 0));
            var at = Out(c, r, default, V(0.75f * s, 0.15f, 0.62f));
            b.Eye(head, at, Outward(c, r, at), 0.028f, white: PaintMint, pupil: Rgb(30, 40, 36));
        });
        return b;
    }

    /// <summary>Grafaiai: a lean dark monkey with a ringed tail, a crown of grey spikes about its head, great round ears, a white mask striped purple round big green eyes, a dark snout dripping blue paint, and fingers and feet smeared with pink.</summary>
    private static PokeBuilder Grafaiai()
    {
        var b = new PokeBuilder("Grafaiai", 0.7f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Fur };
        var dark = Rgb(62, 62, 80);
        var grey = Rgb(152, 150, 162);
        var white = Rgb(236, 236, 240);
        var purple = Rgb(122, 82, 182);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.18f, 0));
            var knee = V(0.05f * s, 0.11f, 0.03f);
            b.Limb(leg, V(0.035f * s, 0.19f, 0), knee, 0.024f, 0.018f, dark);
            b.Limb(leg, knee, V(0.05f * s, 0.035f, 0.0f), 0.018f, 0.016f, dark);
            b.PaintEll(leg, V(0.05f * s, 0.07f, 0.01f), V(0.025f, 0.012f, 0.025f), grey, soft: 0.005f);
            b.Ell(leg, V(0.052f * s, 0.018f, 0.02f), V(0.024f, 0.018f, 0.034f), white);
            if (s > 0) b.PaintEll(leg, V(0.06f * s, 0.03f, 0.04f), V(0.015f, 0.012f, 0.015f), PaintPink, soft: 0.005f);
        });
        var bc = V(0, 0.25f, 0);
        b.Ell(Body, bc, V(0.05f, 0.075f, 0.042f), dark);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.045f * s, 0.05f, 0));
            var elbow = bc + V(0.08f * s, -0.02f, 0.03f);
            var hand = s > 0 ? bc + V(0.03f, 0.08f, 0.09f) : bc + V(-0.09f, -0.09f, 0.03f);
            b.Limb(arm, bc + V(0.045f * s, 0.05f, 0), elbow, 0.016f, 0.014f, dark);
            b.Limb(arm, elbow, hand, 0.014f, 0.013f, dark);
            b.Ell(arm, hand, V(0.018f, 0.02f, 0.016f), PaintPink);
        });
        int tail = b.Tail(bc + V(0, -0.06f, -0.04f));
        var tp = Smooth(3, bc + V(0, -0.06f, -0.04f), bc + V(0.05f, -0.1f, -0.14f), bc + V(0.12f, 0.0f, -0.18f), bc + V(0.14f, 0.12f, -0.14f));
        b.Tube(tail, tp, 0.022f, 0.026f, dark, blend: 0f);
        for (int i = 2; i < tp.Length; i += 2)
            b.PaintEll(tail, tp[i], V(0.032f, 0.032f, 0.032f) * 0.75f, grey, Euler(tp[i] - tp[i - 1]), 0.004f);
        foreach (float t in new[] { 0.5f, 0.85f })
        {
            int i = (int)(t * (tp.Length - 1));
            b.PaintTorus(tail, tp[i], 0.026f, 0.008f, grey, Euler(tp[Math.Min(i + 1, tp.Length - 1)] - tp[i - 1]));
        }
        int head = b.Head(bc + V(0, 0.08f, 0.01f));
        var c = bc + V(0, 0.15f, 0.01f);
        var r = V(0.072f, 0.068f, 0.066f);
        b.Ell(head, c, r, dark);
        // The crown of grey spikes about its head
        foreach (var d in new[] { V(0, 1f, -0.3f), V(0.6f, 0.9f, -0.3f), V(-0.6f, 0.9f, -0.3f), V(0.3f, 1f, -0.6f), V(-0.3f, 1f, -0.6f), V(0.9f, 0.5f, -0.4f), V(-0.9f, 0.5f, -0.4f) })
        {
            var n = Vector3.Normalize(d);
            b.Spike(head, c + n * 0.05f, c + n * 0.14f, 0.024f, grey, 0.45f);
        }
        // The white mask striped purple, and the snout dripping blue
        b.PaintEll(head, c + V(0, 0.01f, 0.05f), V(0.06f, 0.05f, 0.035f), white, soft: 0.008f);
        PokeBuilder.Both(s => b.PaintEll(head, c + V(0.035f * s, 0.025f, 0.055f), V(0.012f, 0.04f, 0.02f), purple, V(0, 0, -25f * s), 0.005f));
        b.Ell(head, c + V(0, -0.03f, 0.065f), V(0.03f, 0.025f, 0.035f), dark, blend: 0.012f);
        b.Spike(head, c + V(0.01f, -0.045f, 0.085f), c + V(0.012f, -0.09f, 0.09f), 0.012f, PaintBlue, blend: 0.005f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.01f, -0.01f));
            b.Ell(ear, c + V(0.12f * s, 0.02f, -0.015f), V(0.065f, 0.058f, 0.015f), dark, V(0, 15f * s, -10f * s));
            b.PaintEll(ear, c + V(0.13f * s, 0.02f, 0.0f), V(0.045f, 0.04f, 0.01f), Rgb(96, 94, 116), V(0, 15f * s, -10f * s), 0.006f);
            var at = Out(c, r, default, V(0.42f * s, 0.12f, 0.9f));
            b.Eye(head, at, Outward(c, r, at), 0.02f, white: PaintMint, pupil: Rgb(30, 40, 36));
        });
        return b;
    }

    // ------------------------------------------------------------------ Bramblin and Brambleghast

    private static readonly Color BrambleTan = Rgb(214, 184, 130);
    private static readonly Color BrambleBrown = Rgb(178, 106, 64);
    private static readonly Color BrambleDark = Rgb(96, 50, 40);
    private static readonly Color BrambleRed = Rgb(228, 76, 52);

    /// <summary>A tumbleweed's eye at <paramref name="at"/>: a dark spindle with a red slit at its heart, leaning <paramref name="lean"/> degrees.</summary>
    private static void BrambleEye(PokeBuilder b, int bone, Vector3 at, float size, float lean)
    {
        b.Ell(bone, at, V(size * 0.45f, size, size * 0.3f), BrambleDark, V(0, 0, lean), Shell, 0.006f);
        b.PaintEll(bone, at + V(0, 0, size * 0.25f), V(size * 0.16f, size * 0.62f, size * 0.12f), BrambleRed, V(0, 0, lean), size * 0.08f);
    }

    /// <summary>Bramblin: a ball of dry branches, tan above and browner below, curving up from a knot at its foot like the bars of a cage, a thorn here and there, and two dark eyes with red slits held up inside it.</summary>
    private static PokeBuilder Bramblin()
    {
        var b = new PokeBuilder("Bramblin", 0.6f, BodyPlan.Floating, V(0, 0.28f, 0)) { Coat = Shell }.Hover();
        var c = V(0, 0.28f, 0);
        const float R = 0.25f;
        b.Ell(Body, c + V(0, -R + 0.02f, 0), V(0.065f, 0.035f, 0.065f), BrambleBrown);
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8f + 0.2f;
            var pts = new Vector3[7];
            for (int j = 0; j < pts.Length; j++)
            {
                float phi = 0.25f + j * 0.38f, wob = 0.1f * MathF.Sin(j * 1.7f + i);
                pts[j] = c + V(MathF.Sin(phi) * MathF.Cos(a + wob) * R, -MathF.Cos(phi) * R, MathF.Sin(phi) * MathF.Sin(a + wob) * R);
            }
            b.Tube(Body, Smooth(2, pts), 0.017f, 0.007f, BrambleTan, blend: 0.006f);
            var mid = pts[2 + i % 3];
            b.Spike(Body, mid, mid + Vector3.Normalize(mid - c) * 0.05f + V(0, 0.015f, 0), 0.011f, BrambleTan, blend: 0.004f);
        }
        b.PaintEll(Body, c + V(0, -R, 0), V(R * 1.3f, R * 0.75f, R * 1.3f), BrambleBrown, soft: 0.05f);
        // The eyes, held up on a stem from the knot
        int head = b.Head(c + V(0, -0.1f, 0));
        var fork = c + V(0, -0.05f, 0.02f);
        b.Tube(head, Smooth(2, c + V(0, -R + 0.03f, 0), c + V(0, -0.14f, 0.01f), fork), 0.013f, 0.01f, BrambleBrown, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var eye = c + V(0.05f * s, 0.01f, 0.03f);
            b.Limb(head, fork, eye + V(-0.01f * s, -0.03f, 0), 0.009f, 0.008f, BrambleBrown, blend: 0.005f);
            BrambleEye(b, head, eye, 0.05f, -15f * s);
        });
        return Lift(b);
    }

    /// <summary>Brambleghast: a great wreath of tangled thorny branches standing on its edge, tan above and brown below, its topmost thorns tipped purple, and within it a branching stem holding up two dark eyes with red slits, each ringed with thorns.</summary>
    private static PokeBuilder Brambleghast()
    {
        var b = new PokeBuilder("Brambleghast", 0.95f, BodyPlan.Floating, V(0, 0.45f, 0)) { Coat = Shell }.Hover();
        var purple = Rgb(150, 122, 206);
        var c = V(0, 0.45f, 0);
        const float R = 0.36f;
        foreach (var (tilt, roll, ring) in new[] { (90f, 0f, R), (83f, 12f, R * 0.96f), (97f, -10f, R * 1.03f) })
            b.Torus(Body, c, ring, 0.024f, BrambleTan, V(tilt, roll, 0), blend: 0.012f);
        for (int i = 0; i < 14; i++)
        {
            float a = i * MathF.Tau / 14f + 0.1f;
            var n = V(MathF.Cos(a), MathF.Sin(a), 0);
            var p = c + n * R;
            b.Spike(Body, p, p + n * 0.085f + V(0, 0, (i % 3 - 1) * 0.03f), 0.018f, n.Y > 0.75f ? purple : BrambleTan, 0.6f, blend: 0.006f);
        }
        b.PaintEll(Body, c + V(0, -R, 0), V(R * 1.2f, R * 0.85f, 0.2f), BrambleBrown, soft: 0.08f);
        // The branching stem within, and the eyes it holds
        int head = b.Head(c);
        var fork = c + V(0, -0.03f, 0.03f);
        b.Tube(head, Smooth(2, c + V(0, -R + 0.01f, 0), c + V(0.02f, -0.18f, 0.03f), fork), 0.032f, 0.022f, BrambleBrown, blend: 0.008f);
        b.Tube(head, Smooth(2, fork, c + V(-0.03f, 0.12f, 0.02f), c + V(0, R - 0.01f, 0)), 0.02f, 0.016f, BrambleBrown, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            var eye = c + V(0.12f * s, 0.04f, 0.04f);
            b.Tube(head, Smooth(2, fork, c + V(0.07f * s, -0.04f, 0.04f), eye + V(-0.02f * s, -0.06f, 0)), 0.018f, 0.014f, BrambleBrown, blend: 0.008f);
            b.Tube(head, Smooth(2, eye + V(0.03f * s, 0.05f, -0.01f), c + V(0.2f * s, 0.18f, 0.0f), c + V(R * 0.86f * s, R * 0.5f, 0)), 0.014f, 0.012f, BrambleBrown, blend: 0.008f);
            b.Ell(head, eye, V(0.045f, 0.075f, 0.03f), BrambleBrown, V(0, 0, -15f * s), blend: 0.01f);
            BrambleEye(b, head, eye + V(0, 0, 0.012f), 0.065f, -15f * s);
            foreach (float a in new[] { 0.3f, 1.6f, 2.9f, 4.4f })
            {
                var n = V(MathF.Cos(a) * 0.9f, MathF.Sin(a), 0);
                b.Spike(head, eye + n * 0.05f, eye + n * 0.1f, 0.012f, BrambleBrown, 0.6f, blend: 0.005f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Toedscool and Toedscruel

    /// <summary>Toedscool: a pink mushroom-jellyfish of a head, lumpy on top with a yellow spot on one side, a grumpy face below with drooping eyes, walking on two long pale tentacles that end in flat pads.</summary>
    private static PokeBuilder Toedscool()
    {
        var b = new PokeBuilder("Toedscool", 0.85f, BodyPlan.Biped, V(0, 0.56f, 0)) { Coat = Scales };
        var pink = Rgb(242, 190, 174);
        var deep = Rgb(206, 140, 132);
        var stem = Rgb(236, 232, 222);
        var c = V(0, 0.56f, 0);
        var r = V(0.11f, 0.12f, 0.1f);
        b.Ell(Body, c, r, pink);
        PokeBuilder.Both(s => b.Ell(Body, c + V(0.045f * s, 0.07f, -0.01f), V(0.065f, 0.06f, 0.08f), pink, blend: 0.02f));
        var spot = Out(c, r, default, V(1f, 0.25f, -0.15f));
        b.PaintEll(Body, spot, V(0.045f, 0.05f, 0.05f), Rgb(226, 184, 44), soft: 0.006f);
        b.PaintEll(Body, spot, V(0.03f, 0.034f, 0.034f), Rgb(250, 222, 80), soft: 0.006f);
        b.Ell(Body, c + V(0, -0.1f, 0.05f), V(0.035f, 0.03f, 0.035f), pink, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.038f * s, c.Y - 0.04f);
            b.Eye(Body, at, Outward(c, r, at), 0.015f, sclera: true, pupil: Rgb(40, 30, 30), glare: true);
        });
        var mouth = On(c, r, c.X, c.Y - 0.075f);
        b.Mark(Body, mouth, Outward(c, r, mouth), 0.018f, 0.007f, deep, MarkShape.Smile, 180f);
        // The two tentacles it walks on, the left one long and bowed out
        foreach (var (s, path) in new[]
        {
            (-1f, new[] { V(-0.03f, 0.47f, 0.01f), V(-0.07f, 0.32f, 0.05f), V(-0.19f, 0.13f, 0.06f), V(-0.29f, 0.03f, 0.05f) }),
            (1f, new[] { V(0.03f, 0.47f, 0.0f), V(0.06f, 0.33f, -0.02f), V(0.1f, 0.15f, 0.0f), V(0.13f, 0.03f, 0.02f) }),
        })
        {
            int leg = b.Leg(s, path[0]);
            b.Tube(leg, Smooth(3, path), 0.017f, 0.012f, stem, blend: 0.006f);
            var foot = path[^1] + V(0.03f * s, -0.017f, 0.005f);
            b.Ell(leg, foot, V(0.05f, 0.013f, 0.034f), stem, V(0, 20f * s, 0), blend: 0.008f);
        }
        return b;
    }

    /// <summary>Toedscruel: a great mushroom-jellyfish under a black cap with a wavy yellow-edged brim and two pink domes on top, a pale face below with a black spout for a mouth, many long tentacles fading to yellow standing it up, and two long arms ending in yellow pads.</summary>
    private static PokeBuilder Toedscruel()
    {
        var b = new PokeBuilder("Toedscruel", 1.0f, BodyPlan.Biped, V(0, 0.64f, 0)) { Coat = Scales };
        var black = Rgb(46, 42, 50);
        var pink = Rgb(240, 186, 168);
        var cream = Rgb(244, 238, 208);
        var yellow = Rgb(238, 216, 82);
        var c = V(0, 0.64f, 0);
        var r = V(0.075f, 0.085f, 0.07f);
        b.Ell(Body, c, r, cream);
        b.Limb(Body, c + V(0, -0.02f, 0.05f), c + V(0, -0.08f, 0.11f), 0.02f, 0.026f, black);
        b.PaintEll(Body, c + V(0, -0.085f, 0.12f), V(0.024f, 0.012f, 0.024f), yellow, soft: 0.004f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, c.X + 0.04f * s, c.Y + 0.008f);
            b.Eye(Body, at, Outward(c, r, at), 0.013f, sclera: true, pupil: Rgb(30, 30, 34), glare: true);
        });
        // The cap: a black brim, wavy and edged yellow, and two pink domes on top
        int head = b.Head(c + V(0, 0.08f, 0));
        var cap = c + V(0, 0.11f, 0);
        b.Ell(head, cap, V(0.2f, 0.05f, 0.18f), black);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            b.Ell(head, cap + V(MathF.Cos(a) * 0.19f, -0.02f, MathF.Sin(a) * 0.17f), V(0.05f, 0.022f, 0.05f), black, blend: 0.02f);
        }
        b.PaintTorus(head, cap + V(0, -0.02f, 0), 0.235f, 0.016f, yellow, default, 1f, 0.9f);
        PokeBuilder.Both(s => b.Ell(head, cap + V(0.07f * s, 0.035f, -0.01f), V(0.075f, 0.045f, 0.08f), pink, blend: 0.015f));
        b.Ell(head, cap + V(0, 0.06f, 0), V(0.024f, 0.03f, 0.024f), black, blend: 0.01f);
        // The tentacles it stands on, pale fading to yellow
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f + 0.3f;
            var n = V(MathF.Cos(a), 0, MathF.Sin(a));
            var root = c + n * 0.05f + V(0, -0.06f, 0);
            int leg = b.Leg(n.X < 0 ? -1f : 1f, root, n.Z > 0);
            var path = Smooth(3, root, root + n * 0.05f + V(0, -0.2f, 0), n * 0.12f + V(0, 0.16f, 0), n * 0.13f + V(0, 0.03f, 0));
            b.Tube(leg, path, 0.017f, 0.014f, cream, blend: 0.006f);
            b.Ell(leg, path[^1] + V(0, -0.01f, 0.005f), V(0.022f, 0.02f, 0.03f), yellow, blend: 0.006f);
            b.PaintEll(leg, n * 0.12f + V(0, 0.08f, 0), V(0.05f, 0.12f, 0.05f), yellow, soft: 0.05f);
        }
        // The two long arms, one curled up, one reaching forward
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, c + V(0.06f * s, -0.05f, 0.02f));
            var path = s < 0
                ? Smooth(3, c + V(-0.06f, -0.05f, 0.03f), c + V(-0.2f, 0.02f, 0.08f), c + V(-0.32f, 0.0f, 0.1f), c + V(-0.34f, -0.12f, 0.08f))
                : Smooth(3, c + V(0.06f, -0.05f, 0.03f), c + V(0.14f, -0.1f, 0.12f), c + V(0.2f, -0.2f, 0.16f), c + V(0.22f, -0.26f, 0.14f));
            b.Tube(arm, path, 0.017f, 0.014f, cream, blend: 0.006f);
            b.Ell(arm, path[^1] + V(0, -0.035f, 0), V(0.032f, 0.05f, 0.022f), yellow, blend: 0.008f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Klawf

    /// <summary>A clump of mossy rock on a crab's shell: a dark green knob bristling with short points.</summary>
    private static void MossClump(PokeBuilder b, int bone, Vector3 at, float r, Color color)
    {
        b.Ell(bone, at, V(r, r * 0.8f, r), color, blend: 0.01f);
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f;
            var n = Vector3.Normalize(V(MathF.Cos(a), 0.6f + 0.3f * (i % 2), MathF.Sin(a)));
            b.Spike(bone, at + n * r * 0.6f, at + n * r * 1.6f, r * 0.4f, color, 0.6f, blend: 0.004f);
        }
    }

    /// <summary>Klawf: a crab that hangs from cliffs, its broad tan shell a scowling face with a long dark frown and pink cheeks, eyes on stalks, clumps of mossy rock on its shell and joints, two great orange-red pincers raised and three thin legs a side.</summary>
    private static PokeBuilder Klawf()
    {
        var b = new PokeBuilder("Klawf", 0.85f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Shell };
        var red = Rgb(232, 100, 56);
        var tan = Rgb(238, 210, 168);
        var mouth = Rgb(124, 58, 56);
        var moss = Rgb(96, 102, 54);
        var bc = V(0, 0.2f, 0);
        var br = V(0.17f, 0.11f, 0.12f);
        b.Ell(Body, bc, br, tan);
        b.PaintEll(Body, bc + V(0, -0.005f, 0.11f), V(0.1f, 0.02f, 0.04f), mouth, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, bc + V(0.1f * s, -0.025f, 0.1f), V(0.022f, 0.026f, 0.04f), mouth, soft: 0.006f);
            b.PaintEll(Body, bc + V(0.13f * s, 0.025f, 0.075f), V(0.025f, 0.02f, 0.03f), Rgb(242, 152, 142), soft: 0.008f);
        });
        foreach (var (x, z) in new[] { (-0.1f, 0.0f), (0f, 0.02f), (0.1f, 0.0f) })
            MossClump(b, Body, bc + V(x, 0.095f, z), 0.03f, moss);
        int head = b.Head(bc + V(0, 0.08f, 0.05f));
        PokeBuilder.Both(s =>
        {
            b.Limb(head, bc + V(0.05f * s, 0.08f, 0.06f), bc + V(0.06f * s, 0.15f, 0.07f), 0.012f, 0.01f, red);
            var eye = bc + V(0.06f * s, 0.17f, 0.07f);
            b.Ell(head, eye, V(0.03f, 0.03f, 0.03f), White);
            var at = eye + Vector3.Normalize(V(0.2f * s, 0.1f, 1f)) * 0.03f;
            b.Eye(head, at, at - eye, 0.014f, sclera: true, pupil: Rgb(30, 30, 34));
        });
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.15f * s, 0.02f, 0.04f));
            var elbow = bc + V(0.27f * s, 0.04f, 0.08f);
            var wrist = bc + V(0.3f * s, 0.16f, 0.08f);
            b.Limb(arm, bc + V(0.15f * s, 0.02f, 0.04f), elbow, 0.03f, 0.028f, red);
            b.Limb(arm, elbow, wrist, 0.03f, 0.036f, red);
            MossClump(b, arm, elbow + V(0, 0.015f, 0), 0.032f, moss);
            // The pincer held open: a broad palm and two blades parting
            var up = Vector3.Normalize(V(0.1f * s, 1f, 0.15f));
            var palm = wrist + up * 0.04f;
            b.Ell(arm, palm, V(0.05f, 0.065f, 0.04f), red, Euler(up));
            b.Spike(arm, palm + up * 0.03f + V(0.022f * s, 0, 0), palm + up * 0.16f + V(0.05f * s, 0, 0), 0.03f, red, 0.55f);
            b.Spike(arm, palm + up * 0.03f - V(0.022f * s, 0, 0), palm + up * 0.12f - V(0.035f * s, 0, 0), 0.026f, red, 0.55f);
        });
        foreach (var (z, front) in new[] { (0.06f, true), (-0.02f, false), (-0.09f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, bc + V(0.13f * s, -0.04f, z), front);
                var knee = bc + V(0.25f * s, 0.0f, z * 1.3f);
                var foot = V(0.31f * s, 0.006f, bc.Z + z * 1.6f);
                b.Limb(leg, bc + V(0.13f * s, -0.04f, z), knee, 0.022f, 0.018f, red);
                b.Limb(leg, knee, foot, 0.018f, 0.007f, red);
            });
        return b;
    }
}
