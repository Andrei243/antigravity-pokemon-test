using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Kanto's first in National Pokédex order:
// Bulbasaur (1) to Vileplume (45), but for the Pikachu and Clefairy lines, which are in the Sinnoh Pokédex and were
// hand-built before. Their forms are in PokemonModels.Megas.cs, PokemonModels.Gigantamax.cs and
// PokemonModels.Regional.cs with the other forms. Helpers shared with the Sinnoh batches are in
// PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs.
internal static partial class PokemonModels
{
    /// <summary>
    /// A palm-like leaf along the curve from <paramref name="root"/> by <paramref name="bend"/> to <paramref name="tip"/>:
    /// a rib down its middle and <paramref name="pairs"/> pairs of leaflets swept toward the tip, longest in the middle
    /// and flat across <paramref name="facing"/>, so its edges are cut into points.
    /// </summary>
    private static void PalmLeaf(PokeBuilder b, int bone, Vector3 root, Vector3 bend, Vector3 tip, float width, Color color, Vector3 facing, int pairs)
    {
        Vector3 At(float t) => (1 - t) * (1 - t) * root + 2 * (1 - t) * t * bend + t * t * tip;
        Vector3 Along(float t) => Vector3.Normalize(2 * (1 - t) * (bend - root) + 2 * t * (tip - bend));
        // The blade itself, in two halves along the curve, then leaflets past its edges
        float Thin(float half) => MathF.Max(0.25f, 0.011f / half);
        Frond(b, bone, At(0f), At(0.58f), width * 0.55f, color, facing, Thin(width * 0.55f), Leaf, 0.01f);
        Frond(b, bone, At(0.42f), At(1f), width * 0.45f, color, facing, Thin(width * 0.45f), Leaf, 0.01f);
        for (int i = 0; i < pairs; i++)
        {
            float t = (i + 0.6f) / (pairs + 0.4f);
            var at = At(t);
            var y = Along(t);
            var side = Vector3.Normalize(Vector3.Cross(facing, y));
            float reach = width * (0.6f + 0.4f * MathF.Sin(MathF.PI * MathF.Min(1f, 0.1f + t)));
            foreach (float s in new[] { -1f, 1f })
            {
                var end = at + (side * s * 0.9f + y * 0.6f) * reach;
                float half = reach * 0.36f;
                Frond(b, bone, at - y * reach * 0.2f, end, half, color, facing, Thin(half), Leaf, 0.008f);
            }
        }
    }

    /// <summary>
    /// The seed on a Bulbasaur's back: six cloves leaning together into a bulb round <paramref name="center"/>, its
    /// point along <paramref name="axis"/>.
    /// </summary>
    private static void Bulb(PokeBuilder b, int bone, Vector3 center, Vector3 axis, float r, Color color)
    {
        axis = Vector3.Normalize(axis);
        var u = Vector3.Normalize(Vector3.Cross(axis, Vector3.UnitZ));
        var w = Vector3.Cross(u, axis);
        var top = center + axis * r * 1.15f;
        for (int i = 0; i < 6; i++)
        {
            float a = i * MathF.Tau / 6f + 0.4f;
            var off = (u * MathF.Cos(a) + w * MathF.Sin(a)) * r * 0.55f;
            var foot = center - axis * r * 0.45f + off;
            b.Ell(bone, (foot + top) / 2f + off * 0.35f, V(r * 0.55f, Vector3.Distance(foot, top) / 2f + r * 0.1f, r * 0.55f), color, Euler(top - foot), Leaf, 0.02f);
        }
        b.Spike(bone, center + axis * r * 0.7f, center + axis * r * 1.6f, r * 0.42f, color, mat: Leaf, blend: 0.03f);
    }

    /// <summary>Dark patches painted over a Bulbasaur's skin, each a flattened round on the surface at a point given with its size.</summary>
    private static void Patches(PokeBuilder b, int bone, Color color, params (Vector3 at, float size)[] patches)
    {
        foreach (var (at, size) in patches) b.PaintEll(bone, at, V(size, size * 0.8f, size), color);
    }

    // ------------------------------------------------------------------ Bulbasaur line

    private static PokeBuilder Bulbasaur()
    {
        var b = new PokeBuilder("Bulbasaur", 0.58f, BodyPlan.Quadruped, V(0, 0.21f, -0.04f)) { Coat = Scales };
        var teal = Rgb(132, 206, 174);
        var spot = Rgb(76, 156, 124);
        var bulb = Rgb(100, 180, 98);

        // Short, stubby legs under a low body
        foreach (var (z, front) in new[] { (0.13f, true), (-0.21f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.16f * s, 0.17f, z), front);
                b.Limb(leg, V(0.16f * s, 0.17f, z), V(0.17f * s, 0.07f, z + 0.02f), 0.08f, 0.074f, teal);
                b.Ell(leg, V(0.17f * s, 0.045f, z + 0.04f), V(0.08f, 0.045f, 0.088f), teal);
                Claws(b, leg, V(0.17f * s, 0.03f, z + 0.11f), V(0.03f, 0, 0), V(0, -0.1f, 1f), 0.024f, 0.012f);
                Patches(b, leg, spot, (V(0.225f * s, 0.13f, z + 0.03f), 0.032f));
            });
        b.Ell(Body, V(0, 0.21f, -0.05f), V(0.23f, 0.15f, 0.26f), teal);
        PokeBuilder.Both(s => Patches(b, Body, spot, (V(0.2f * s, 0.25f, 0.0f), 0.04f), (V(0.19f * s, 0.19f, -0.17f), 0.035f), (V(0.12f * s, 0.29f, 0.1f), 0.03f)));
        // The seed on its back, a bulb of cloves that has grown with it since it hatched
        int seed = b.Part("bulb", Body, V(0, 0.35f, -0.12f), PokeRole.Leaf);
        Bulb(b, seed, V(0, 0.43f, -0.14f), V(0, 1f, -0.45f), 0.19f, bulb);

        int head = b.Head(V(0, 0.27f, 0.15f));
        var c = V(0, 0.38f, 0.28f);
        var r = V(0.24f, 0.19f, 0.2f);
        b.Ell(head, c, r, teal);
        b.Ell(head, V(0, 0.32f, 0.31f), V(0.21f, 0.12f, 0.17f), teal);
        // A wide smile, small pointed ears, patches on its brow
        Grin(b, head, V(0, 0.305f, 0.465f), V(0.085f, 0.03f, 0.04f), Rgb(222, 122, 142));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.15f * s, 0.52f, 0.24f));
            b.Spike(ear, V(0.15f * s, 0.5f, 0.24f), V(0.21f * s, 0.64f, 0.22f), 0.055f, teal, 0.45f);
        });
        Patches(b, head, spot, (V(0, 0.565f, 0.3f), 0.05f), (V(-0.12f, 0.53f, 0.2f), 0.035f), (V(0.12f, 0.53f, 0.2f), 0.035f));
        PokeBuilder.Both(s => b.Mark(head, V(0.032f * s, 0.362f, 0.472f), V(0.1f * s, 0.15f, 1f), 0.012f, 0.009f, Rgb(40, 70, 60)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.12f * s, 0.4f), V(0.5f * s, 0.12f, 1f), 0.05f, sclera: true, pupil: Rgb(200, 48, 60)));
        return b;
    }

    private static PokeBuilder Ivysaur()
    {
        var b = new PokeBuilder("Ivysaur", 0.74f, BodyPlan.Quadruped, V(0, 0.3f, -0.05f)) { Coat = Scales };
        var teal = Rgb(112, 184, 194);
        var spot = Rgb(64, 126, 138);
        var leaf = Rgb(74, 158, 88);
        var bud = Rgb(238, 126, 148);
        var trunk = Rgb(142, 100, 66);

        foreach (var (z, front) in new[] { (0.15f, true), (-0.24f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.17f * s, 0.26f, z), front);
                b.Limb(leg, V(0.17f * s, 0.26f, z), V(0.19f * s, 0.08f, z + 0.02f), 0.075f, 0.066f, teal);
                b.Ell(leg, V(0.19f * s, 0.045f, z + 0.04f), V(0.076f, 0.045f, 0.088f), teal);
                Claws(b, leg, V(0.19f * s, 0.03f, z + 0.115f), V(0.032f, 0, 0), V(0, -0.1f, 1f), 0.026f, 0.012f);
                Patches(b, leg, spot, (V(0.245f * s, 0.18f, z + 0.02f), 0.034f));
            });
        b.Ell(Body, V(0, 0.3f, -0.05f), V(0.22f, 0.16f, 0.29f), teal);
        PokeBuilder.Both(s => Patches(b, Body, spot, (V(0.21f * s, 0.33f, 0.02f), 0.042f), (V(0.2f * s, 0.28f, -0.2f), 0.036f), (V(0.13f * s, 0.41f, 0.12f), 0.03f)));
        // The bulb has burst open: a short trunk, four great palm leaves spread round it and a pink bud wrapped tight
        b.Limb(Body, V(0, 0.4f, -0.1f), V(0, 0.5f, -0.12f), 0.07f, 0.06f, trunk, Shell);
        int plant = b.Part("bud", Body, V(0, 0.5f, -0.12f), PokeRole.Leaf);
        foreach (float deg in new[] { 50f, 130f, 230f, 310f })
        {
            float a = deg * Degree;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = V(0, 0.5f, -0.12f) + dir * 0.04f;
            PalmLeaf(b, plant, root, root + dir * 0.25f + V(0, 0.12f, 0), root + dir * 0.46f + V(0, -0.1f, 0), 0.15f, leaf, V(0, 1f, 0), 6);
        }
        b.Ell(plant, V(0, 0.63f, -0.14f), V(0.1f, 0.14f, 0.1f), bud, mat: Leaf);
        foreach (float deg in new[] { 0f, 120f, 240f })
        {
            float a = deg * Degree;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, plant, V(0, 0.53f, -0.14f) + o * 0.05f, V(0, 0.78f, -0.16f) + o * 0.015f, 0.085f, PixelCanvas.Shadow(bud, 0.1f), o, 0.32f, Leaf, 0.01f);
        }
        b.Spike(plant, V(0, 0.72f, -0.15f), V(0, 0.83f, -0.17f), 0.06f, bud, mat: Leaf);

        int head = b.Head(V(0, 0.36f, 0.17f));
        var c = V(0, 0.47f, 0.31f);
        var r = V(0.22f, 0.18f, 0.19f);
        b.Ell(head, c, r, teal);
        b.Ell(head, V(0, 0.41f, 0.35f), V(0.2f, 0.11f, 0.16f), teal);
        // A mouth just open, a fang at each corner, ears pointed and dark inside
        Grin(b, head, V(0, 0.395f, 0.5f), V(0.075f, 0.018f, 0.035f), Rgb(150, 70, 84));
        PokeBuilder.Both(s => b.Spike(head, V(0.055f * s, 0.405f, 0.495f), V(0.056f * s, 0.372f, 0.505f), 0.012f, White, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.13f * s, 0.6f, 0.27f));
            b.Spike(ear, V(0.13f * s, 0.59f, 0.27f), V(0.19f * s, 0.74f, 0.24f), 0.055f, teal, 0.45f);
            b.PaintEll(ear, V(0.165f * s, 0.67f, 0.27f), V(0.025f, 0.05f, 0.03f), spot);
        });
        Patches(b, head, spot, (V(0, 0.645f, 0.31f), 0.045f), (V(-0.11f, 0.61f, 0.22f), 0.03f), (V(0.11f, 0.61f, 0.22f), 0.03f));
        PokeBuilder.Both(s => b.Mark(head, V(0.03f * s, 0.448f, 0.505f), V(0.1f * s, 0.15f, 1f), 0.012f, 0.009f, Rgb(40, 70, 72)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.11f * s, 0.49f), V(0.5f * s, 0.12f, 1f), 0.045f, sclera: true, pupil: Rgb(200, 48, 60)));
        return b;
    }

    /// <summary>
    /// Venusaur's build, shared by its Mega Evolution and its Gigantamax form: four thick legs with claws, a heavy
    /// body, and a wide head with a mouth open, small pointed ears and fierce red eyes. Returns the head's bone.
    /// </summary>
    private static int VenusaurBody(PokeBuilder b, Color teal, Color spot, float nostrils = 1f)
    {
        foreach (var (z, front) in new[] { (0.2f, true), (-0.26f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.26f * s, 0.3f, z), front);
                b.Limb(leg, V(0.26f * s, 0.3f, z), V(0.29f * s, 0.09f, z + 0.03f), 0.11f, 0.1f, teal);
                b.Ell(leg, V(0.29f * s, 0.055f, z + 0.05f), V(0.11f, 0.06f, 0.12f), teal);
                Claws(b, leg, V(0.29f * s, 0.035f, z + 0.16f), V(0.045f, 0, 0), V(0, -0.15f, 1f), 0.035f, 0.016f);
                Patches(b, leg, spot, (V(0.385f * s, 0.2f, z + 0.03f), 0.045f));
            });
        b.Ell(Body, V(0, 0.38f, -0.04f), V(0.33f, 0.21f, 0.37f), teal);
        PokeBuilder.Both(s => Patches(b, Body, spot, (V(0.31f * s, 0.42f, 0.06f), 0.06f), (V(0.3f * s, 0.36f, -0.22f), 0.05f), (V(0.2f * s, 0.5f, 0.16f), 0.045f)));

        int head = b.Head(V(0, 0.38f, 0.28f));
        var c = V(0, 0.45f, 0.44f);
        var r = V(0.25f, 0.16f, 0.19f);
        b.Ell(head, c, r, teal);
        b.Ell(head, V(0, 0.39f, 0.48f), V(0.23f, 0.11f, 0.16f), teal);
        Gape(b, head, V(0, 0.38f, 0.62f), V(0.12f, 0.045f, 0.05f), Rgb(196, 92, 116), 0.028f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.16f * s, 0.57f, 0.38f));
            b.Spike(ear, V(0.16f * s, 0.56f, 0.38f), V(0.24f * s, 0.67f, 0.35f), 0.06f, teal, 0.45f);
            b.PaintEll(ear, V(0.2f * s, 0.62f, 0.39f), V(0.028f, 0.045f, 0.03f), spot);
        });
        Patches(b, head, spot, (V(0, 0.605f, 0.44f), 0.06f));
        PokeBuilder.Both(s => b.Mark(head, V(0.035f * s, 0.452f, 0.62f), V(0.1f * s, 0.15f, 1f), 0.013f * nostrils, 0.01f * nostrils, Rgb(36, 72, 72)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.13f * s, 0.48f), V(0.55f * s, 0.15f, 1f), 0.045f, sclera: true, pupil: Rgb(200, 48, 60), glare: true));
        return head;
    }

    /// <summary>
    /// The flower on a Venusaur's back, on a bone of its own: a trunk ringed like a palm's and crowned in gold, palm
    /// leaves hanging under petals spotted white. <paramref name="k"/> scales it. Returns the flower's bone.
    /// </summary>
    private static int Blossom(PokeBuilder b, Vector3 at, float k, Color petal, Color leaf, Color trunk, Color crown, int petals = 5, int leaves = 6)
    {
        var top = at + V(0, 0.3f, -0.02f) * k;
        b.Limb(Body, at - V(0, 0.08f, 0), at + V(0, 0.06f, 0) * k, 0.13f * k, 0.12f * k, trunk, Shell);
        int flower = b.Part("flower", Body, at + V(0, 0.06f, 0) * k, PokeRole.Leaf);
        b.Limb(flower, at + V(0, 0.06f, 0) * k, top, 0.12f * k, 0.085f * k, trunk, Shell);
        for (int i = 0; i < 3; i++)
            b.PaintTorus(flower, at + V(0, 0.13f + 0.05f * i, -0.005f * i) * k, (0.112f - 0.008f * i) * k, 0.007f, PixelCanvas.Shadow(trunk, 0.35f));
        for (int i = 0; i < 7; i++)
        {
            float a = i * MathF.Tau / 7f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Spike(flower, top + o * 0.06f * k, top + (o * 0.1f + V(0, 0.07f, 0)) * k, 0.025f * k, crown, mat: Shell, blend: 0.006f);
        }
        // Palm leaves first, under the petals, hanging over its sides
        for (int i = 0; i < leaves; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / leaves;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = at + (V(0, 0.07f, 0) + dir * 0.06f) * k;
            PalmLeaf(b, flower, root, root + (dir * 0.3f + V(0, 0.06f, 0)) * k, root + (dir * 0.54f - V(0, 0.2f, 0)) * k, 0.17f * k, leaf, V(0, 1f, 0), 6);
        }
        // Broad petals spread round the trunk, drooping at their rounded tips, two white spots on each
        for (int i = 0; i < petals; i++)
        {
            float a = i * MathF.Tau / petals;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = at + (V(0, 0.2f, 0) + dir * 0.05f) * k;
            var mid = root + (dir * 0.26f + V(0, 0.025f, 0)) * k;
            var tip = root + (dir * 0.5f - V(0, 0.05f, 0)) * k;
            Frond(b, flower, root, mid + dir * 0.04f * k, 0.12f * k, petal, V(0, 1f, 0), 0.2f, Leaf, 0.02f);
            Frond(b, flower, mid - dir * 0.08f * k, tip, 0.16f * k, petal, V(0, 1f, 0) + dir * 0.3f, 0.16f, Leaf, 0.02f);
            var across = Vector3.Cross(V(0, 1f, 0), dir);
            foreach (float s in new[] { -1f, 1f })
                b.PaintEll(flower, mid + (dir * 0.07f + across * s * 0.06f) * k, V(0.032f, 0.05f, 0.032f) * k, White);
        }
        return flower;
    }

    private static PokeBuilder Venusaur()
    {
        var b = new PokeBuilder("Venusaur", 0.95f, BodyPlan.Quadruped, V(0, 0.38f, -0.04f)) { Coat = Scales };
        VenusaurBody(b, Rgb(102, 174, 170), Rgb(58, 116, 116));
        // The flower on its back, opened in full
        Blossom(b, V(0, 0.6f, -0.08f), 1f, Rgb(240, 126, 138), Rgb(74, 152, 86), Rgb(128, 90, 62), Rgb(246, 206, 72));
        return b;
    }

    // ------------------------------------------------------------------ Charmander line

    /// <summary>
    /// A dragon's wing on <paramref name="bone"/>: a bat's membrane in two layers, <paramref name="inside"/> in front
    /// and <paramref name="outside"/> behind, so each face shows its own colour, on struts of <paramref name="outside"/>.
    /// Made before the body, because the membrane's scallops are carved.
    /// </summary>
    private static void DragonWing(PokeBuilder b, int bone, Vector3 shoulder, Vector3 wrist, Vector3[] tips, Vector3 root, Color inside, Color outside, float strut)
    {
        // The membrane's panels meet at the wrist and leave their corners open there, which a wide wing shows: a web
        // of skin round the wrist closes them
        var all = new[] { shoulder, wrist, root }.Concat(tips).ToArray();
        float z = all.Average(p => p.Z);
        var middle = all.Aggregate(Vector3.Zero, (sum, p) => sum + p) / all.Length;
        var inward = Vector3.Normalize(V(middle.X - wrist.X, middle.Y - wrist.Y, 0));
        float web = 0.4f * MathF.Min(Vector3.Distance(wrist, tips[0]), Vector3.Distance(wrist, shoulder));
        var gap = V(0, 0, 0.009f);
        foreach (var (shift, color) in new[] { (-gap, outside), (gap, inside) })
        {
            BatMembrane(b, bone, shoulder + shift, wrist + shift, tips.Select(t => t + shift).ToArray(), root + shift, color);
            b.Ell(bone, V(wrist.X, wrist.Y, z) + inward * web * 0.8f + shift, V(web, web, 0.012f), color, blend: 0.006f);
        }
        // The arm along the leading edge shows on both faces; the fingers lie in the back layer, so the front stays clean
        b.Limb(bone, shoulder, wrist, strut * 1.35f, strut * 1.15f, outside);
        foreach (var tip in tips) b.Limb(bone, wrist - gap, tip - gap * 1.4f, strut * 0.75f, strut * 0.4f, outside);
    }

    private static PokeBuilder Charmander()
    {
        var b = new PokeBuilder("Charmander", 0.6f, BodyPlan.Biped, V(0, 0.27f, 0)) { Coat = Scales };
        var orange = Rgb(246, 150, 76);
        var cream = Rgb(252, 230, 162);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.085f * s, 0.17f, -0.01f));
            b.Limb(leg, V(0.085f * s, 0.17f, -0.01f), V(0.1f * s, 0.07f, 0.01f), 0.07f, 0.055f, orange);
            b.Ell(leg, V(0.105f * s, 0.035f, 0.04f), V(0.058f, 0.035f, 0.085f), orange);
            Claws(b, leg, V(0.105f * s, 0.025f, 0.12f), V(0.024f, 0, 0), V(0, -0.1f, 1f), 0.018f, 0.009f);
        });
        b.Ell(Body, V(0, 0.27f, 0), V(0.14f, 0.17f, 0.12f), orange);
        b.PaintEll(Body, V(0, 0.25f, 0.06f), V(0.1f, 0.14f, 0.09f), cream);
        // A tail sweeping out behind, the flame on its tip that shows how it feels
        int tail = b.Tail(V(0, 0.16f, -0.09f));
        var path = Smooth(3, V(0, 0.16f, -0.09f), V(0.03f, 0.1f, -0.24f), V(0.1f, 0.13f, -0.36f), V(0.15f, 0.24f, -0.42f));
        b.Tube(tail, path, 0.055f, 0.03f, orange, blend: 0f);
        int flame = b.Part("flame", tail, path[^1], PokeRole.Flame);
        Flame(b, flame, path[^1], 0.62f);
        // Arms held out, three small fingers on each hand
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.34f, 0.02f));
            b.Limb(arm, V(0.11f * s, 0.34f, 0.02f), V(0.22f * s, 0.3f, 0.07f), 0.042f, 0.034f, orange);
            Digits(b, arm, V(0.22f * s, 0.3f, 0.07f), V(s, -0.2f, 0.4f), V(0, 0.35f, 0.25f), 0.035f, 0.014f, orange);
        });
        // A big round head with a short snout, a smile with one small fang, big blue eyes
        int head = b.Head(V(0, 0.42f, 0.01f));
        var c = V(0, 0.56f, 0.03f);
        var r = V(0.15f, 0.155f, 0.14f);
        b.Ell(head, c, r, orange);
        b.Ell(head, V(0, 0.5f, 0.09f), V(0.12f, 0.085f, 0.11f), orange);
        Grin(b, head, V(0, 0.48f, 0.19f), V(0.065f, 0.024f, 0.03f), Rgb(204, 72, 82));
        b.Spike(head, V(0.036f, 0.497f, 0.19f), V(0.036f, 0.472f, 0.197f), 0.01f, White, mat: Shell, blend: 0.003f);
        PokeBuilder.Both(s => b.Mark(head, V(0.022f * s, 0.535f, 0.193f), V(0.1f * s, 0.2f, 1f), 0.011f, 0.009f, Rgb(120, 50, 40)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.07f * s, 0.585f), V(0.45f * s, 0.1f, 1f), 0.048f, Rgb(56, 124, 204)));
        return b;
    }

    private static PokeBuilder Charmeleon()
    {
        var b = new PokeBuilder("Charmeleon", 0.76f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Scales };
        var red = Rgb(230, 98, 72);
        var cream = Rgb(248, 224, 178);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.27f, -0.02f));
            b.Ell(leg, V(0.1f * s, 0.24f, 0.0f), V(0.07f, 0.09f, 0.08f), red);
            b.Limb(leg, V(0.105f * s, 0.18f, 0.01f), V(0.11f * s, 0.06f, 0.0f), 0.05f, 0.042f, red);
            b.Ell(leg, V(0.11f * s, 0.03f, 0.04f), V(0.052f, 0.03f, 0.085f), red);
            Claws(b, leg, V(0.11f * s, 0.022f, 0.12f), V(0.022f, 0, 0), V(0, -0.1f, 1f), 0.02f, 0.009f);
        });
        b.Ell(Body, V(0, 0.4f, 0.0f), V(0.12f, 0.18f, 0.105f), red);
        b.PaintEll(Body, V(0, 0.38f, 0.05f), V(0.09f, 0.15f, 0.08f), cream);
        int tail = b.Tail(V(0, 0.27f, -0.08f));
        var path = Smooth(3, V(0, 0.27f, -0.08f), V(0.02f, 0.14f, -0.24f), V(0.1f, 0.12f, -0.42f), V(0.2f, 0.24f, -0.5f));
        b.Tube(tail, path, 0.055f, 0.026f, red, blend: 0f);
        int flame = b.Part("flame", tail, path[^1], PokeRole.Flame);
        Flame(b, flame, path[^1], 0.72f);
        // Arms held forward, ready, with white claws
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.5f, 0.02f));
            b.Limb(arm, V(0.1f * s, 0.5f, 0.02f), V(0.17f * s, 0.4f, 0.1f), 0.036f, 0.03f, red);
            b.Ell(arm, V(0.18f * s, 0.38f, 0.115f), V(0.032f, 0.03f, 0.032f), red);
            Claws(b, arm, V(0.185f * s, 0.36f, 0.14f), V(0.014f, 0, 0), V(0.1f * s, -0.6f, 1f), 0.024f, 0.008f);
        });
        // A long snout, a horn sweeping back from the back of its head, fierce eyes
        int head = b.Head(V(0, 0.55f, 0.02f));
        var c = V(0, 0.66f, 0.03f);
        var r = V(0.11f, 0.1f, 0.11f);
        b.Ell(head, c, r, red);
        b.Ell(head, V(0, 0.625f, 0.12f), V(0.08f, 0.065f, 0.1f), red);
        b.Spike(head, V(0, 0.72f, -0.03f), V(0, 0.8f, -0.19f), 0.055f, red, 0.45f);
        b.Mark(head, V(0, 0.6f, 0.205f), V(0, -0.2f, 1f), 0.045f, 0.01f, Rgb(110, 40, 40), MarkShape.Bar);
        b.Spike(head, V(0.03f, 0.6f, 0.2f), V(0.03f, 0.578f, 0.205f), 0.009f, White, mat: Shell, blend: 0.003f);
        PokeBuilder.Both(s => b.Mark(head, V(0.02f * s, 0.655f, 0.205f), V(0.1f * s, 0.4f, 1f), 0.011f, 0.009f, Rgb(110, 40, 36)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.685f), V(0.5f * s, 0.1f, 1f), 0.03f, Rgb(36, 92, 112), glare: true));
        return b;
    }

    /// <summary>
    /// Charizard's build, shared by its Mega Evolutions and its Gigantamax form: legs, body and tail, arms with claws,
    /// the long neck and the head with its snout and two horns. Returns the head's bone and the tail's tip.
    /// </summary>
    private static (int head, Vector3 tip) CharizardBody(PokeBuilder b, Color skin, Color belly, Color eyes, float girth = 1f, float horn = 1f)
    {
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.36f, -0.02f));
            b.Ell(leg, V(0.14f * s, 0.32f, 0.0f), V(0.1f, 0.13f, 0.11f), skin);
            b.Limb(leg, V(0.15f * s, 0.24f, 0.02f), V(0.15f * s, 0.07f, 0.0f), 0.065f, 0.055f, skin);
            b.Ell(leg, V(0.155f * s, 0.035f, 0.06f), V(0.07f, 0.035f, 0.1f), skin);
            Claws(b, leg, V(0.155f * s, 0.025f, 0.15f), V(0.028f, 0, 0), V(0, -0.1f, 1f), 0.024f, 0.011f);
        });
        b.Ell(Body, V(0, 0.52f, 0.01f * girth), V(0.18f * girth, 0.24f, 0.15f * girth), skin);
        b.PaintEll(Body, V(0, 0.5f, 0.07f * girth), V(0.13f * girth, 0.21f, 0.11f * girth), belly);
        int tail = b.Tail(V(0, 0.36f, -0.12f));
        var path = Smooth(3, V(0, 0.36f, -0.12f), V(0.02f, 0.24f, -0.3f), V(0.12f, 0.2f, -0.46f), V(0.22f, 0.3f, -0.52f));
        b.Tube(tail, path, 0.085f, 0.04f, skin, blend: 0f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.15f * s * girth, 0.66f, 0.04f));
            b.Limb(arm, V(0.15f * s * girth, 0.66f, 0.04f), V(0.25f * s * girth, 0.54f, 0.12f), 0.045f, 0.038f, skin);
            b.Ell(arm, V(0.26f * s * girth, 0.525f, 0.135f), V(0.04f, 0.035f, 0.04f), skin);
            Claws(b, arm, V(0.27f * s * girth, 0.5f, 0.165f), V(0.016f, 0, 0), V(0.1f * s, -0.7f, 1f), 0.03f, 0.009f);
        });
        b.Limb(Body, V(0, 0.7f, 0.02f), V(0, 0.84f, 0.07f), 0.085f, 0.07f, skin);
        int head = b.Head(V(0, 0.84f, 0.07f));
        var c = V(0, 0.95f, 0.08f);
        var r = V(0.1f, 0.085f, 0.11f);
        b.Ell(head, c, r, skin);
        b.Ell(head, V(0, 0.92f, 0.18f), V(0.07f, 0.055f, 0.09f), skin);
        PokeBuilder.Both(s => b.Spike(head, V(0.05f * s, 0.99f, 0.01f), V(0.08f * s, 1.0f + 0.06f * horn, 0.01f - 0.14f * horn), 0.03f, skin));
        b.Mark(head, V(0, 0.885f, 0.262f), V(0, -0.3f, 1f), 0.04f, 0.009f, Rgb(90, 40, 30), MarkShape.Bar);
        PokeBuilder.Both(s => b.Mark(head, V(0.02f * s, 0.95f, 0.255f), V(0.1f * s, 0.5f, 1f), 0.011f, 0.009f, Rgb(90, 40, 30)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.975f), V(0.6f * s, 0.15f, 1f), 0.024f, eyes));
        return (head, path[^1]);
    }

    /// <summary>The wing of a Charizard on side <paramref name="s"/>, its points spread out as given, scaled by <paramref name="k"/>.</summary>
    private static void CharizardWing(PokeBuilder b, float s, Color inside, Color outside, float k = 1f, float strut = 0.026f)
    {
        int wing = b.Wing(s, V(0.1f * s, 0.68f, -0.12f));
        DragonWing(b, wing, V(0.1f * s, 0.68f, -0.12f), V(0.1f * s, 0.68f, -0.2f) + V(0.32f * s, 0.32f, 0) * k,
            new[] { V(0.1f * s, 0.6f, -0.22f) + V(0.62f * s, 0.26f, 0) * k, V(0.1f * s, 0.6f, -0.2f) + V(0.58f * s, 0.02f, 0) * k, V(0.1f * s, 0.56f, -0.17f) + V(0.38f * s, -0.06f, 0) * k },
            V(0.11f * s, 0.54f, -0.12f), inside, outside, strut);
        b.Spike(wing, V(0.1f * s, 0.68f, -0.2f) + V(0.32f * s, 0.32f, 0) * k, V(0.1f * s, 0.74f, -0.2f) + V(0.34f * s, 0.32f, 0) * k, 0.02f, outside);
    }

    /// <summary>
    /// A tongue of flame from <paramref name="from"/> to its point at <paramref name="to"/>, <paramref name="r"/> wide
    /// at its root and lit from within: <paramref name="rim"/> outside, <paramref name="heart"/> toward its root.
    /// </summary>
    private static void FlameTongue(PokeBuilder b, int bone, Vector3 from, Vector3 to, float r, Color rim, Color heart)
    {
        var d = to - from;
        b.Ell(bone, from + d * 0.15f, V(r, r * 1.25f, r), rim, Euler(d), Glow, r * 0.3f);
        b.Spike(bone, from + d * 0.2f, to, r * 0.85f, rim, mat: Glow, blend: r * 0.3f);
        b.PaintEll(bone, from + d * 0.24f, V(r * 0.85f, r * 1.6f, r * 0.85f), PixelCanvas.Mix(rim, heart, 0.5f), Euler(d), r * 0.35f);
        b.PaintEll(bone, from + d * 0.18f, V(r * 0.55f, r * 0.95f, r * 0.55f), heart, Euler(d), r * 0.3f);
    }

    private static PokeBuilder Charizard()
    {
        var b = new PokeBuilder("Charizard", 0.95f, BodyPlan.Biped, V(0, 0.52f, 0)) { Coat = Scales };
        var orange = Rgb(242, 146, 72);
        // Wings first, for their membranes are carved: teal in front, orange behind
        PokeBuilder.Both(s => CharizardWing(b, s, Rgb(46, 140, 160), orange));
        var (_, tip) = CharizardBody(b, orange, Rgb(250, 228, 162), Rgb(40, 120, 140));
        int flame = b.Part("flame", b.Model.Skeleton.Find("tail"), tip, PokeRole.Flame);
        Flame(b, flame, tip, 0.85f);
        return b;
    }

    // ------------------------------------------------------------------ Squirtle line

    /// <summary>
    /// A turtle's shell on an upright body: a dome over its back round <paramref name="center"/>, a rim round its
    /// edge in the plane of its front, and cream plates down its belly parted by dark seams.
    /// </summary>
    private static void UprightShell(PokeBuilder b, Vector3 center, Vector3 radii, Color shell, Color rim, Color belly, Color seam, Vector3 bodyCenter, Vector3 bodyRadii)
    {
        b.Ell(Body, center, radii, shell, mat: Shell);
        b.Torus(Body, V(0, center.Y - radii.Y * 0.06f, center.Z + radii.Z * 0.42f), radii.X * 0.94f, radii.X * 0.15f, rim, V(90f, 0, 0), sz: radii.Y / radii.X, mat: Shell, blend: 0.012f);
        b.PaintEll(Body, bodyCenter + V(0, -0.01f, bodyRadii.Z * 0.6f), V(bodyRadii.X * 0.78f, bodyRadii.Y * 0.86f, bodyRadii.Z * 0.6f), belly);
        foreach (float y in new[] { 0.32f, -0.12f })
        {
            var at = On(bodyCenter, bodyRadii, 0, bodyCenter.Y + bodyRadii.Y * y);
            b.Mark(Body, at, Outward(bodyCenter, bodyRadii, at), bodyRadii.X * 0.55f, 0.008f, seam, MarkShape.Bar);
        }
    }

    private static PokeBuilder Squirtle()
    {
        var b = new PokeBuilder("Squirtle", 0.56f, BodyPlan.Biped, V(0, 0.24f, 0)) { Coat = Scales };
        var blue = Rgb(140, 204, 234);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.14f, 0.0f));
            b.Limb(leg, V(0.08f * s, 0.14f, 0.0f), V(0.095f * s, 0.06f, 0.02f), 0.06f, 0.05f, blue);
            b.Ell(leg, V(0.1f * s, 0.03f, 0.035f), V(0.055f, 0.032f, 0.075f), blue);
        });
        var bc = V(0, 0.24f, 0);
        var br = V(0.125f, 0.145f, 0.105f);
        b.Ell(Body, bc, br, blue);
        UprightShell(b, V(0, 0.26f, -0.045f), V(0.15f, 0.16f, 0.105f), Rgb(180, 124, 76), Rgb(246, 242, 228), Rgb(246, 226, 164), Rgb(170, 130, 80), bc, br);
        // A tail curled up at its tip
        int tail = b.Tail(V(0, 0.13f, -0.08f));
        Curl(b, tail, V(0, 0.13f, -0.08f), V(0, 0.19f, -0.26f), V(0, -1f, 0), V(0, 0, -1f), 0.055f, 0.95f, 0.034f, blue);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.11f * s, 0.3f, 0.03f));
            b.Limb(arm, V(0.11f * s, 0.3f, 0.03f), V(0.2f * s, 0.26f, 0.08f), 0.04f, 0.034f, blue);
            b.Ell(arm, V(0.215f * s, 0.255f, 0.09f), V(0.04f, 0.034f, 0.04f), blue);
        });
        // A big round head with a short muzzle, a smile, large red-brown eyes
        int head = b.Head(V(0, 0.36f, 0.01f));
        var c = V(0, 0.48f, 0.03f);
        var r = V(0.145f, 0.135f, 0.135f);
        b.Ell(head, c, r, blue);
        b.Ell(head, V(0, 0.445f, 0.09f), V(0.1f, 0.07f, 0.08f), blue);
        Grin(b, head, V(0, 0.424f, 0.166f), V(0.05f, 0.013f, 0.025f), Rgb(170, 70, 90));
        PokeBuilder.Both(s => b.Mark(head, V(0.018f * s, 0.457f, 0.168f), V(0.1f * s, 0.3f, 1f), 0.011f, 0.008f, Rgb(50, 80, 100)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.06f * s, 0.5f), V(0.45f * s, 0.12f, 1f), 0.042f, Rgb(150, 44, 60)));
        return b;
    }

    private static PokeBuilder Wartortle()
    {
        var b = new PokeBuilder("Wartortle", 0.74f, BodyPlan.Biped, V(0, 0.33f, 0)) { Coat = Scales };
        var blue = Rgb(136, 158, 224);
        var fluff = Rgb(226, 238, 252);
        var swirl = Rgb(150, 182, 232);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.2f, 0.0f));
            b.Limb(leg, V(0.09f * s, 0.2f, 0.0f), V(0.11f * s, 0.07f, 0.02f), 0.066f, 0.055f, blue);
            b.Ell(leg, V(0.115f * s, 0.033f, 0.045f), V(0.062f, 0.035f, 0.085f), blue);
            Claws(b, leg, V(0.115f * s, 0.025f, 0.125f), V(0.024f, 0, 0), V(0, -0.1f, 1f), 0.018f, 0.009f);
        });
        var bc = V(0, 0.33f, 0);
        var br = V(0.14f, 0.17f, 0.12f);
        b.Ell(Body, bc, br, blue);
        UprightShell(b, V(0, 0.35f, -0.05f), V(0.165f, 0.185f, 0.115f), Rgb(150, 102, 68), Rgb(244, 240, 226), Rgb(244, 226, 168), Rgb(160, 120, 76), bc, br);
        // A great curled tail of fur, white and fluffy, a pale blue swirl through it
        int tail = b.Tail(V(0, 0.2f, -0.1f));
        var center = V(0.04f, 0.36f, -0.33f);
        Curl(b, tail, V(0, 0.2f, -0.1f), center, V(0, -1f, 0), Vector3.Normalize(V(0.25f, 0, -1f)), 0.12f, 1.05f, 0.06f, fluff);
        foreach (float a in new[] { 0.6f, 1.6f, 2.6f, 3.6f })
            b.Ell(tail, center + (V(0, -MathF.Cos(a), 0) + Vector3.Normalize(V(0.25f, 0, -1f)) * MathF.Sin(a)) * 0.13f, V(0.06f, 0.06f, 0.06f), fluff, blend: 0.03f);
        b.PaintTorus(tail, center, 0.07f, 0.012f, swirl, V(0, 14f, 90f), sz: 1.2f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.41f, 0.03f));
            b.Limb(arm, V(0.12f * s, 0.41f, 0.03f), V(0.22f * s, 0.35f, 0.08f), 0.042f, 0.036f, blue);
            b.Ell(arm, V(0.235f * s, 0.34f, 0.09f), V(0.042f, 0.036f, 0.042f), blue);
            Claws(b, arm, V(0.25f * s, 0.33f, 0.12f), V(0.016f, 0, 0), V(0.2f * s, -0.4f, 1f), 0.02f, 0.008f);
        });
        // Feathery ears swept up and back, a fang at each side of the mouth, brown eyes
        int head = b.Head(V(0, 0.48f, 0.02f));
        var c = V(0, 0.6f, 0.03f);
        var r = V(0.13f, 0.12f, 0.12f);
        b.Ell(head, c, r, blue);
        b.Ell(head, V(0, 0.565f, 0.09f), V(0.09f, 0.065f, 0.075f), blue);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.1f * s, 0.66f, 0.0f));
            Frond(b, ear, V(0.09f * s, 0.65f, 0.0f), V(0.24f * s, 0.84f, -0.06f), 0.055f, fluff, V(0.3f * s, 0, 1f), 0.3f);
            Frond(b, ear, V(0.1f * s, 0.63f, 0.0f), V(0.25f * s, 0.7f, -0.06f), 0.042f, fluff, V(0.3f * s, 0, 1f), 0.34f);
            b.PaintEll(ear, V(0.17f * s, 0.75f, -0.02f), V(0.02f, 0.06f, 0.04f), swirl, V(0, 0, -35f * s));
        });
        Grin(b, head, V(0, 0.545f, 0.158f), V(0.048f, 0.013f, 0.024f), Rgb(150, 60, 80));
        PokeBuilder.Both(s => b.Spike(head, V(0.03f * s, 0.553f, 0.157f), V(0.03f * s, 0.532f, 0.162f), 0.008f, White, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s => b.Mark(head, V(0.017f * s, 0.58f, 0.163f), V(0.1f * s, 0.3f, 1f), 0.011f, 0.008f, Rgb(50, 60, 100)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.62f), V(0.45f * s, 0.12f, 1f), 0.034f, Rgb(116, 70, 44), glare: true));
        return b;
    }

    /// <summary>
    /// Blastoise's build, shared by its Mega Evolution and its Gigantamax form: thick legs and arms with claws, a heavy
    /// body with its belly plates, and a small head with round ears. Returns the head's bone.
    /// </summary>
    private static int BlastoiseBody(PokeBuilder b, Color blue, Color belly, Color seam)
    {
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.14f * s, 0.3f, 0.0f));
            b.Ell(leg, V(0.16f * s, 0.25f, 0.02f), V(0.1f, 0.12f, 0.11f), blue);
            b.Limb(leg, V(0.17f * s, 0.16f, 0.03f), V(0.17f * s, 0.06f, 0.02f), 0.08f, 0.075f, blue);
            b.Ell(leg, V(0.175f * s, 0.035f, 0.06f), V(0.085f, 0.04f, 0.11f), blue);
            Claws(b, leg, V(0.175f * s, 0.03f, 0.16f), V(0.03f, 0, 0), V(0, -0.1f, 1f), 0.025f, 0.012f);
        });
        var bc = V(0, 0.46f, 0);
        var br = V(0.21f, 0.25f, 0.17f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, bc + V(0, -0.02f, br.Z * 0.6f), V(br.X * 0.78f, br.Y * 0.86f, br.Z * 0.6f), belly);
        foreach (float y in new[] { 0.3f, -0.1f })
        {
            var at = On(bc, br, 0, bc.Y + br.Y * y);
            b.Mark(Body, at, Outward(bc, br, at), br.X * 0.55f, 0.01f, seam, MarkShape.Bar);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.2f * s, 0.62f, 0.02f));
            b.Ell(arm, V(0.22f * s, 0.6f, 0.03f), V(0.085f, 0.09f, 0.085f), blue);
            b.Limb(arm, V(0.24f * s, 0.56f, 0.05f), V(0.31f * s, 0.45f, 0.11f), 0.07f, 0.065f, blue);
            b.Ell(arm, V(0.32f * s, 0.42f, 0.13f), V(0.06f, 0.055f, 0.06f), blue);
            Claws(b, arm, V(0.33f * s, 0.39f, 0.17f), V(0.022f, 0, 0), V(0.1f * s, -0.5f, 1f), 0.026f, 0.01f);
        });
        int head = b.Head(V(0, 0.68f, 0.05f));
        var c = V(0, 0.775f, 0.08f);
        var r = V(0.12f, 0.1f, 0.11f);
        b.Ell(head, c, r, blue);
        b.Ell(head, V(0, 0.745f, 0.15f), V(0.09f, 0.065f, 0.08f), blue);
        PokeBuilder.Both(s => b.Ell(head, V(0.085f * s, 0.86f, 0.02f), V(0.038f, 0.042f, 0.03f), blue, V(0, 0, -20f * s)));
        b.Mark(head, V(0, 0.722f, 0.226f), V(0, -0.3f, 1f), 0.045f, 0.01f, Rgb(40, 50, 80), MarkShape.Bar);
        PokeBuilder.Both(s => b.Mark(head, V(0.02f * s, 0.775f, 0.226f), V(0.1f * s, 0.4f, 1f), 0.011f, 0.009f, Rgb(40, 50, 80)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.8f), V(0.5f * s, 0.12f, 1f), 0.026f, Rgb(110, 66, 40), glare: true));
        return head;
    }

    /// <summary>
    /// A cannon on <paramref name="bone"/> from <paramref name="breech"/> to its <paramref name="muzzle"/>: a pale
    /// barrel, a band round its mouth, the bore carved into it and dark inside.
    /// </summary>
    private static void Cannon(PokeBuilder b, int bone, Vector3 breech, Vector3 muzzle, float r, Color barrel)
    {
        var d = Vector3.Normalize(muzzle - breech);
        b.Limb(bone, breech, muzzle, r, r, barrel, Metal, 0.012f);
        b.Limb(bone, muzzle - d * r * 0.5f, muzzle, r * 1.12f, r * 1.12f, barrel, Metal, 0.004f);
        b.Cut(bone, muzzle + d * r * 0.2f, V(r * 0.62f, r * 1.2f, r * 0.62f), Euler(d));
        b.PaintEll(bone, muzzle, V(r * 0.7f, r * 0.7f, r * 0.7f), Rgb(40, 42, 52), Euler(d), r * 0.15f);
    }

    private static PokeBuilder Blastoise()
    {
        var b = new PokeBuilder("Blastoise", 0.95f, BodyPlan.Biped, V(0, 0.46f, 0)) { Coat = Scales };
        var blue = Rgb(106, 150, 216);
        var shell = Rgb(132, 96, 68);
        var rim = Rgb(242, 242, 238);
        int head = BlastoiseBody(b, blue, Rgb(238, 222, 164), Rgb(160, 130, 90));
        // A great shell over its back with a white rim, two cannons jutting forward from it past its head
        b.Ell(Body, V(0, 0.5f, -0.09f), V(0.25f, 0.28f, 0.17f), shell, mat: Shell);
        b.Torus(Body, V(0, 0.48f, -0.01f), 0.235f, 0.035f, rim, V(90f, 0, 0), sz: 1.19f, mat: Shell, blend: 0.012f);
        PokeBuilder.Both(s => Cannon(b, Body, V(0.14f * s, 0.74f, -0.16f), V(0.19f * s, 0.79f, 0.1f), 0.055f, Rgb(220, 222, 228)));
        int tail = b.Tail(V(0, 0.28f, -0.14f));
        b.Spike(tail, V(0, 0.26f, -0.16f), V(0, 0.18f, -0.3f), 0.05f, blue);
        return b;
    }

    // ------------------------------------------------------------------ Caterpie line

    /// <summary>
    /// A caterpillar's body: segments along <paramref name="path"/> from the tail to the neck, each on a bone of its
    /// own, growing from <paramref name="r0"/> to <paramref name="r1"/>, with a pale belly. Returns the last segment's
    /// bone, for the head to hang from.
    /// </summary>
    private static int Grub(PokeBuilder b, Vector3[] path, float r0, float r1, Color skin, Color belly, Action<int, int, Vector3, float>? each = null)
    {
        int parent = Body;
        for (int i = 0; i < path.Length; i++)
        {
            int seg = i == 0 ? Body : b.Part("seg" + i, parent, path[i], PokeRole.Segment, i);
            float r = r0 + (r1 - r0) * i / (path.Length - 1);
            b.Ell(seg, path[i], V(r, r * 0.95f, r), skin, blend: 0.03f);
            b.PaintEll(seg, path[i] + V(0, -r * 0.6f, r * 0.3f), V(r * 0.85f, r * 0.5f, r * 0.85f), belly);
            each?.Invoke(i, seg, path[i], r);
            parent = seg;
        }
        return parent;
    }

    private static PokeBuilder Caterpie()
    {
        var path = new[] { V(0, 0.062f, -0.26f), V(0, 0.066f, -0.17f), V(0, 0.076f, -0.08f), V(0, 0.11f, 0.0f), V(0, 0.18f, 0.04f), V(0, 0.255f, 0.05f) };
        var b = new PokeBuilder("Caterpie", 0.5f, BodyPlan.Serpent, path[0]) { Coat = Fur };
        var green = Rgb(132, 202, 92);
        var cream = Rgb(246, 228, 164);
        var yellow = Rgb(248, 222, 110);

        // Green segments ringed in yellow on each side, cream feet on the raised front
        int neck = Grub(b, path, 0.064f, 0.084f, green, cream, (i, seg, at, r) =>
        {
            PokeBuilder.Both(s => b.Mark(seg, at + V(r * 0.97f * s, r * 0.1f, 0), V(s, 0.15f, 0), r * 0.42f, r * 0.42f, yellow, MarkShape.Ring));
            if (i >= 3) PokeBuilder.Both(s => b.Ell(seg, at + V(r * 0.45f * s, -r * 0.2f, r * 0.85f), V(r * 0.32f, r * 0.28f, r * 0.3f), cream, blend: 0.01f));
        });
        // A short stub of a tail
        int tail = b.Tail(path[0]);
        b.Tube(tail, new[] { path[0] + V(0, 0.02f, -0.04f), path[0] + V(0, 0.08f, -0.12f), path[0] + V(0, 0.14f, -0.13f) }, 0.022f, 0.016f, Rgb(214, 184, 112), blend: 0f);
        b.Ell(tail, path[0] + V(0, 0.16f, -0.13f), V(0.03f, 0.04f, 0.03f), Rgb(214, 184, 112), blend: 0.01f);

        int head = b.Head(path[^1] + V(0, 0.04f, 0.01f), neck);
        var c = V(0, 0.36f, 0.06f);
        var r = V(0.105f, 0.1f, 0.1f);
        b.Ell(head, c, r, green);
        b.PaintEll(head, V(0, 0.31f, 0.13f), V(0.075f, 0.055f, 0.05f), cream);
        // The red forked feeler on its head that gives off a stink, and great round eyes ringed in yellow
        b.Tube(head, new[] { V(0, 0.44f, 0.03f), V(0, 0.49f, 0.07f), V(0, 0.5f, 0.1f) }, 0.018f, 0.016f, Rgb(230, 92, 60), blend: 0f);
        PokeBuilder.Both(s => b.Tube(head, new[] { V(0, 0.5f, 0.1f), V(0.035f * s, 0.505f, 0.115f), V(0.07f * s, 0.5f, 0.12f) }, 0.016f, 0.014f, Rgb(230, 92, 60), blend: 0f));
        b.Tube(head, new[] { V(0, 0.5f, 0.1f), V(0, 0.495f, 0.15f) }, 0.016f, 0.014f, Rgb(230, 92, 60), blend: 0f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.07f * s, 0.38f);
            b.PaintEll(head, at, V(0.05f, 0.055f, 0.05f), yellow);
            b.Eye(head, at, Outward(c, r, at), 0.04f, pupil: Rgb(30, 30, 30));
        });
        return b;
    }

    private static PokeBuilder Metapod()
    {
        var b = new PokeBuilder("Metapod", 0.6f, BodyPlan.Floating, V(0, 0.3f, -0.05f)) { Coat = Shell };
        var green = Rgb(132, 196, 84);
        var seam = Rgb(92, 150, 60);
        // A hard green shell curled like a crescent, its point hooked forward over a heavy-lidded eye
        var path = new[] { V(0, 0.07f, -0.2f), V(0, 0.12f, -0.12f), V(0, 0.23f, -0.08f), V(0, 0.36f, -0.05f), V(0, 0.47f, 0.0f), V(0, 0.54f, 0.09f), V(0, 0.57f, 0.2f) };
        float[] radius = { 0.06f, 0.1f, 0.13f, 0.13f, 0.105f, 0.065f, 0.02f };
        for (int i = 0; i < path.Length - 1; i++)
            b.Limb(Body, path[i], path[i + 1], radius[i], radius[i + 1], green, blend: 0.03f);
        foreach (int i in new[] { 1, 2, 3, 4 })
        {
            var along = Vector3.Normalize(path[i + 1] - path[i - 1]);
            b.PaintTorus(Body, path[i] + along * 0.02f, radius[i] * 1.0f, 0.007f, seam, Euler(along));
        }
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.1f * s, 0.3f, -0.08f), V(0.006f, 0.12f, 0.06f), seam, V(0, 0, 10f * s), 0.008f));
        var c = V(0, 0.38f, -0.05f);
        var rr = V(0.13f, 0.14f, 0.13f);
        PokeBuilder.Both(s => b.Eye(Body, On(c, rr, 0.055f * s, 0.39f), V(0.45f * s, 0.1f, 1f), 0.034f, sclera: true, pupil: Rgb(30, 30, 30), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Butterfree

    /// <summary>
    /// Butterfree's body and head: a round dark body with small blue hands and feet, great red eyes, two little fangs
    /// and black feelers. Returns the head's bone.
    /// </summary>
    private static int ButterfreeBody(PokeBuilder b, Color body)
    {
        var blue = Rgb(90, 166, 214);
        b.Ell(Body, V(0, 0.42f, 0), V(0.07f, 0.085f, 0.068f), body);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.46f, 0.03f));
            b.Limb(arm, V(0.05f * s, 0.46f, 0.03f), V(0.07f * s, 0.42f, 0.07f), 0.016f, 0.014f, body);
            b.Ell(arm, V(0.075f * s, 0.41f, 0.08f), V(0.022f, 0.02f, 0.022f), blue);
            int leg = b.Leg(s, V(0.035f * s, 0.36f, 0.02f));
            b.Limb(leg, V(0.035f * s, 0.36f, 0.02f), V(0.045f * s, 0.3f, 0.035f), 0.018f, 0.016f, body);
            b.Ell(leg, V(0.047f * s, 0.285f, 0.04f), V(0.02f, 0.024f, 0.02f), blue);
        });
        int head = b.Head(V(0, 0.5f, 0.01f));
        var c = V(0, 0.55f, 0.02f);
        var r = V(0.085f, 0.075f, 0.075f);
        b.Ell(head, c, r, body);
        PokeBuilder.Both(s => b.Spike(head, V(0.015f * s, 0.505f, 0.08f), V(0.016f * s, 0.485f, 0.085f), 0.008f, White, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.02f * s, 0.62f, 0.02f));
            b.Tube(ear, Smooth(3, V(0.02f * s, 0.61f, 0.02f), V(0.05f * s, 0.7f, 0.02f), V(0.07f * s, 0.78f, 0.0f), V(0.1f * s, 0.84f, -0.02f)), 0.008f, 0.007f, Rgb(36, 34, 44));
            b.Ell(ear, V(0.105f * s, 0.85f, -0.025f), V(0.014f, 0.016f, 0.014f), Rgb(36, 34, 44), blend: 0.006f);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.048f * s, 0.565f), V(0.75f * s, 0.05f, 0.7f), 0.04f, sclera: true, white: Rgb(222, 52, 64), pupil: Rgb(140, 24, 36)));
        return head;
    }

    private static PokeBuilder Butterfree()
    {
        var b = new PokeBuilder("Butterfree", 0.76f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        // White wings rimmed in black, the upper pair broad and the lower small
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.04f * s, 0.46f, -0.03f));
            FlatWing(b, w, V(0.24f * s, 0.6f, -0.035f), 0.22f, 0.15f, 22f * s, Rgb(248, 248, 252), Rgb(40, 38, 48), 0.026f);
            b.Mark(w, V(0.22f * s, 0.6f, -0.02f), V(0, 0, 1f), 0.11f, 0.009f, Rgb(60, 58, 70), MarkShape.Bar, 22f * s);
            FlatWing(b, w, V(0.16f * s, 0.36f, -0.04f), 0.13f, 0.1f, -42f * s, Rgb(248, 248, 252), Rgb(40, 38, 48), 0.022f);
        });
        ButterfreeBody(b, Rgb(86, 78, 142));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Weedle line

    private static PokeBuilder Weedle()
    {
        var path = new[] { V(0, 0.058f, -0.24f), V(0, 0.062f, -0.155f), V(0, 0.074f, -0.07f), V(0, 0.11f, 0.0f), V(0, 0.175f, 0.035f) };
        var b = new PokeBuilder("Weedle", 0.48f, BodyPlan.Serpent, path[0]) { Coat = Fur };
        var tan = Rgb(208, 150, 88);
        var pink = Rgb(226, 126, 164);
        // Tan segments with a pink bead on each side, a pale stinger on its tail
        int neck = Grub(b, path, 0.058f, 0.07f, tan, PixelCanvas.Light1(tan, 0.15f), (i, seg, at, r) =>
            PokeBuilder.Both(s => b.Ell(seg, at + V(r * 0.88f * s, -r * 0.3f, 0.01f), V(0.018f, 0.018f, 0.018f), pink, blend: 0.006f)));
        int tail = b.Tail(path[0]);
        b.Tube(tail, Smooth(3, path[0] + V(0, 0.02f, -0.04f), path[0] + V(0, 0.06f, -0.1f), path[0] + V(0, 0.12f, -0.11f)), 0.03f, 0.008f, Rgb(236, 232, 226), Shell, 0f);

        int head = b.Head(path[^1] + V(0, 0.05f, 0.01f), neck);
        var c = V(0, 0.29f, 0.05f);
        var r = V(0.1f, 0.095f, 0.095f);
        b.Ell(head, c, r, tan);
        // A big pink nose, little black eyes and the poison horn on its head
        b.Ell(head, V(0, 0.265f, 0.145f), V(0.05f, 0.042f, 0.04f), pink, blend: 0.012f);
        b.Spike(head, V(0, 0.37f, 0.04f), V(0, 0.5f, 0.02f), 0.038f, Rgb(240, 238, 234), mat: Shell);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.32f), V(0.45f * s, 0.15f, 1f), 0.02f, pupil: Rgb(30, 30, 30)));
        return b;
    }

    private static PokeBuilder Kakuna()
    {
        var b = new PokeBuilder("Kakuna", 0.6f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell };
        var gold = Rgb(236, 204, 98);
        var dark = Rgb(160, 126, 50);
        // A golden shell standing on its point, a hood over its eyes, its arms folded flat against its sides
        b.Ell(Body, V(0, 0.3f, 0), V(0.14f, 0.23f, 0.13f), gold);
        b.Spike(Body, V(0, 0.14f, 0.0f), V(0, 0.0f, -0.02f), 0.09f, gold);
        b.Ell(Body, V(0, 0.5f, 0.03f), V(0.155f, 0.12f, 0.15f), gold, V(-10f, 0, 0));
        b.PaintEll(Body, V(0, 0.42f, 0.12f), V(0.11f, 0.035f, 0.05f), dark);
        PokeBuilder.Both(s => b.Ell(Body, V(0.105f * s, 0.27f, 0.05f), V(0.05f, 0.13f, 0.06f), gold, V(-8f, 0, 6f * s), blend: 0.012f));
        foreach (float y in new[] { 0.18f, 0.1f })
            b.PaintTorus(Body, V(0, y, -0.005f), 0.1f + (y - 0.1f) * 0.6f, 0.006f, dark, V(10f, 0, 0));
        PokeBuilder.Both(s => b.Eye(Body, V(0.052f * s, 0.41f, 0.117f), V(0.3f * s, -0.1f, 1f), 0.032f, sclera: true, white: Rgb(40, 34, 30), pupil: Rgb(250, 250, 250), glare: true));
        return b;
    }

    /// <summary>
    /// Beedrill and its Mega Evolution: a yellow head with great red eyes, a dark thorax, a striped abdomen ending
    /// in a stinger, stingers for forearms and four clear wings. <paramref name="mega"/> is longer and sharper all over.
    /// </summary>
    private static PokeBuilder BeedrillBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Beedrill-Mega" : "Beedrill", mega ? 0.92f : 0.82f, BodyPlan.Bird, V(0, 0.5f, 0)) { Coat = Fur }.Hover();
        var yellow = Rgb(248, 212, 66);
        var black = Rgb(50, 46, 56);
        var sting = Rgb(222, 226, 234);
        var wing = Rgb(236, 242, 250);
        float k = mega ? 1.5f : 1f;
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.04f * s, 0.54f, -0.05f));
            FlatWing(b, w, V(0.14f * s, 0.67f, -0.045f), 0.15f * k, 0.065f, 55f * s, wing, Rgb(150, 156, 172), 0.012f);
            FlatWing(b, w, V(0.13f * s, 0.56f, -0.05f), 0.11f * k, 0.05f, 18f * s, wing, Rgb(150, 156, 172), 0.011f);
        });
        b.Ell(Body, V(0, 0.5f, 0), V(0.065f, 0.075f, 0.065f), black);
        b.PaintEll(Body, V(0, 0.53f, 0.05f), V(0.05f, 0.03f, 0.03f), yellow);
        // The abdomen, banded black and yellow, tapering to its stinger
        var top = V(0, 0.43f, -0.04f);
        var end = mega ? V(0, 0.04f, -0.06f) : V(0, 0.16f, -0.14f);
        var axis = Vector3.Normalize(end - top);
        float len = Vector3.Distance(top, end);
        float girth = mega ? 0.07f : 0.085f;
        b.Ell(Body, top + axis * len * 0.38f, V(girth, len * 0.42f, girth * 0.95f), yellow, Euler(axis));
        int bands = mega ? 5 : 3;
        float step = mega ? 0.13f : 0.18f;
        for (int i = 0; i < bands; i++)
            b.PaintTorus(Body, top + axis * len * (0.16f + step * i), girth * MathF.Sin(MathF.PI * (0.2f + step * 0.95f * i)), mega ? 0.018f : 0.022f, black, Euler(axis));
        b.Spike(Body, top + axis * len * 0.72f, end + axis * 0.06f * k, 0.042f, sting, mat: Shell);
        // Thin dark legs hanging under it
        foreach (var (z, y) in new[] { (0.02f, 0.47f), (-0.04f, 0.44f) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.04f * s, y, z), z > 0f);
                b.Tube(leg, new[] { V(0.04f * s, y, z), V(0.09f * s, y - 0.1f, z + 0.03f), V(0.07f * s, y - 0.22f, z + 0.04f) }, 0.011f, 0.009f, black, blend: 0f);
                if (mega && z < 0f) b.Spike(leg, V(0.07f * s, y - 0.21f, z + 0.04f), V(0.07f * s, y - 0.3f, z + 0.04f), 0.014f, sting, mat: Shell);
            });
        // Arms that end in great lances of stingers
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.53f, 0.02f));
            b.Tube(arm, new[] { V(0.06f * s, 0.53f, 0.02f), V(0.13f * s, 0.47f, 0.06f), V(0.16f * s, 0.5f, 0.1f) }, 0.014f, 0.013f, black, blend: 0f);
            b.Spike(arm, V(0.16f * s, 0.49f, 0.1f), V(0.16f * s, 0.49f, 0.1f) + V(0.04f * s, 0.2f, 0.12f) * k, 0.045f, sting, mat: Shell);
        });

        int head = b.Head(V(0, 0.57f, 0.02f));
        var c = V(0, 0.63f, 0.04f);
        var r = V(0.068f, 0.062f, 0.064f);
        b.Ell(head, c, r, yellow);
        b.Ell(head, V(0, 0.585f, 0.09f), V(0.03f, 0.022f, 0.025f), yellow, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.02f * s, 0.68f, 0.03f));
            b.Tube(ear, new[] { V(0.02f * s, 0.68f, 0.03f), V(0.04f * s, 0.74f, 0.02f), V(0.09f * s, 0.78f, 0.04f), V(0.13f * s, 0.78f, 0.08f) }, 0.009f, 0.007f, black, blend: 0f);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.036f * s, 0.645f), V(0.75f * s, 0.05f, 0.65f), 0.032f, sclera: true, white: Rgb(222, 48, 56), pupil: Rgb(130, 20, 30), glare: true));
        return Lift(b);
    }

    private static PokeBuilder Beedrill() => BeedrillBuild(false);

    // ------------------------------------------------------------------ Pidgey line

    /// <summary>A bird's two legs from hips <paramref name="x"/> apart at <paramref name="hip"/>, each with three toes forward and one back, tipped with pale talons.</summary>
    private static void BirdLegs(PokeBuilder b, float x, float hip, float z, float thick, Color color)
    {
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(x * s, hip, z));
            var ankle = V(x * 1.08f * s, 0.035f, z + 0.02f);
            b.Limb(leg, V(x * s, hip, z), ankle, thick, thick * 0.8f, color, Scales, 0.008f);
            foreach (var (dx, dz) in new[] { (-0.5f, 1f), (0f, 1.1f), (0.5f, 1f), (0f, -0.7f) })
            {
                var toe = ankle + V(dx * thick * 2.2f * s, -0.02f, dz * thick * 3.2f);
                b.Limb(leg, ankle, toe, thick * 0.6f, thick * 0.45f, color, Scales, 0.006f);
                b.Spike(leg, toe, toe + Vector3.Normalize(toe - ankle) * thick * 1.4f + V(0, -0.006f, 0), thick * 0.4f, Claw, mat: Shell, blend: 0.003f);
            }
        });
    }

    /// <summary>A wing folded against a bird's side on side <paramref name="s"/>, from its shoulder back to its tip, the tip in another colour.</summary>
    private static int FoldedWing(PokeBuilder b, float s, Vector3 shoulder, Vector3 tip, float width, Color color, Color tipColor)
    {
        int wing = b.Wing(s, shoulder);
        var d = tip - shoulder;
        b.Ell(wing, shoulder + d * 0.5f, V(width * 0.35f, d.Length() * 0.52f, width), color, Euler(d), blend: 0.012f);
        b.PaintEll(wing, shoulder + d * 0.85f, V(width * 0.6f, d.Length() * 0.25f, width * 1.2f), tipColor, Euler(d));
        return wing;
    }

    /// <summary>A fan of tail feathers from <paramref name="root"/>, spread over <paramref name="spread"/> degrees and lying back and a little down.</summary>
    private static void TailFan(PokeBuilder b, int bone, Vector3 root, int count, float spread, float length, float width, Color color, Color tipColor, float droop = 0.25f)
    {
        for (int i = 0; i < count; i++)
        {
            float a = (count == 1 ? 0f : -spread / 2f + spread * i / (count - 1)) * Degree;
            var dir = Vector3.Normalize(V(MathF.Sin(a), -droop, -MathF.Cos(a)));
            var tip = root + dir * length;
            Frond(b, bone, root, tip, width, color, V(0, 1f, 0), 0.3f, Fur, 0.01f);
            b.PaintEll(bone, root + dir * length * 0.86f, V(width * 1.3f, length * 0.2f, width * 1.3f), tipColor, Euler(dir));
        }
    }

    private static PokeBuilder Pidgey()
    {
        var b = new PokeBuilder("Pidgey", 0.52f, BodyPlan.Bird, V(0, 0.23f, 0)) { Coat = Fur }.Hover();
        var brown = Rgb(180, 130, 82);
        var cream = Rgb(248, 228, 180);
        var dark = Rgb(124, 84, 54);
        BirdLegs(b, 0.06f, 0.12f, 0.02f, 0.017f, Rgb(232, 158, 150));
        b.Ell(Body, V(0, 0.24f, 0), V(0.16f, 0.15f, 0.19f), brown);
        b.PaintEll(Body, V(0, 0.21f, 0.09f), V(0.12f, 0.12f, 0.12f), cream);
        int tail = b.Tail(V(0, 0.24f, -0.16f));
        TailFan(b, tail, V(0, 0.25f, -0.15f), 3, 40f, 0.16f, 0.045f, brown, dark);
        PokeBuilder.Both(s => FoldedWing(b, s, V(0.13f * s, 0.3f, 0.04f), V(0.16f * s, 0.21f, -0.2f), 0.06f, brown, dark));

        int head = b.Head(V(0, 0.35f, 0.07f));
        var c = V(0, 0.42f, 0.09f);
        var r = V(0.12f, 0.115f, 0.12f);
        b.Ell(head, c, r, brown);
        b.PaintEll(head, V(0, 0.38f, 0.16f), V(0.1f, 0.06f, 0.08f), cream);
        // A tuft swept back from its crown, a short beak, and the black streak that runs back from each eye
        foreach (var (x, h, z) in new[] { (0f, 0.07f, -0.1f), (-0.022f, 0.05f, -0.11f), (0.022f, 0.05f, -0.11f) })
            b.Spike(head, V(x, 0.51f, 0.1f), V(x * 2f, 0.51f + h, z), 0.032f, brown, 0.5f);
        b.Spike(head, V(0, 0.41f, 0.2f), V(0, 0.39f, 0.28f), 0.035f, Rgb(206, 174, 170), 0.8f, Shell);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, V(0.08f * s, 0.425f, 0.1f), V(0.03f, 0.022f, 0.06f), Rgb(40, 34, 32), V(0, 30f * s, -10f * s));
            b.Eye(head, On(c, r, 0.062f * s, 0.43f), V(0.6f * s, 0.05f, 1f), 0.03f);
        });
        return b;
    }

    /// <summary>
    /// Pidgeotto and Pidgeot: a pale breast under brown wings half spread, a plume swept back from the crown, a fan
    /// of red tail feathers and the black streak behind each eye.
    /// </summary>
    private static PokeBuilder BigPidgey(bool pidgeot)
    {
        float k = pidgeot ? 1.2f : 1f;
        var b = new PokeBuilder(pidgeot ? "Pidgeot" : "Pidgeotto", pidgeot ? 0.9f : 0.7f, BodyPlan.Bird, V(0, 0.3f * k, 0)) { Coat = Fur }.Hover();
        var brown = Rgb(176, 124, 76);
        var cream = Rgb(246, 228, 176);
        var red = Rgb(232, 86, 70);
        var gold = Rgb(250, 206, 90);
        BirdLegs(b, 0.075f * k, 0.16f * k, 0.03f, 0.022f * k, Rgb(232, 158, 150));
        b.Ell(Body, V(0, 0.3f * k, 0), V(0.17f * k, 0.18f * k, 0.2f * k), pidgeot ? cream : brown);
        b.PaintEll(Body, V(0, 0.27f * k, 0.09f * k), V(0.13f * k, 0.15f * k, 0.12f * k), cream);
        int tail = b.Tail(V(0, 0.28f * k, -0.17f * k));
        TailFan(b, tail, V(0, 0.28f * k, -0.16f * k), pidgeot ? 5 : 4, 54f, 0.26f * k, 0.05f * k, gold, red, 0.35f);
        // Wings held half open: five long flight feathers fanned over shorter ones at the shoulder
        PokeBuilder.Both(s =>
        {
            var root = V(0.13f * s, 0.39f, 0.02f) * k;
            int wing = b.Wing(s, root);
            b.Ell(wing, root + V(0.09f * s, 0.0f, -0.03f) * k, V(0.11f, 0.08f, 0.035f) * k, brown, V(0, 0, 20f * s));
            for (int i = 0; i < 5; i++)
            {
                float a = (62f - i * 20f) * Degree;
                float len = (0.3f + 0.06f * MathF.Sin(MathF.PI * i / 4f)) * k;
                var tip = root + V(MathF.Cos(a) * s, MathF.Sin(a), -0.35f) * len;
                Frond(b, wing, root + V(0.04f * s, 0, -0.02f) * k, tip, 0.055f * k, brown, V(0, 0.2f, 1f), 0.26f, Fur, 0.012f);
                b.PaintEll(wing, root + (tip - root) * 0.86f, V(0.065f, 0.06f, 0.065f) * k, pidgeot ? cream : Rgb(214, 176, 120), Euler(tip - root));
            }
        });

        int head = b.Head(V(0, 0.46f * k, 0.08f));
        var c = V(0, 0.54f * k, 0.1f);
        var r = V(0.1f * k, 0.095f * k, 0.105f * k);
        b.Ell(head, c, r, pidgeot ? cream : brown);
        b.PaintEll(head, V(0, 0.505f * k, 0.15f), V(0.085f * k, 0.05f * k, 0.07f * k), cream);
        b.Spike(head, c + V(0, -0.01f, r.Z * 0.9f), c + V(0, -0.04f * k, r.Z * 0.9f + 0.09f * k), 0.032f * k, Rgb(214, 166, 170), 0.8f, Shell);
        // The plume: red on Pidgeotto's crown, on Pidgeot long gold streamers tipped in red, swept far back
        int plume = b.Part("plume", head, c + V(0, r.Y * 0.8f, 0), PokeRole.Ear);
        var crown = c + V(0, r.Y * 0.85f, 0.02f);
        foreach (var (x, lift, reach) in pidgeot ? new[] { (-0.03f, 0.1f, 0.5f), (0f, 0.14f, 0.56f), (0.03f, 0.1f, 0.5f) } : new[] { (-0.02f, 0.1f, 0.24f), (0f, 0.14f, 0.28f), (0.02f, 0.1f, 0.24f) })
        {
            var path = Smooth(3, crown + V(x, 0, 0.04f), crown + V(x * 1.5f, lift * k, -reach * 0.3f * k), crown + V(x * 2f, lift * 0.8f * k, -reach * 0.7f * k), crown + V(x * 2.5f, lift * 0.2f * k, -reach * k));
            b.Tube(plume, path, 0.026f * k, 0.012f * k, pidgeot ? gold : red, Fur, 0f);
            if (pidgeot) b.PaintEll(plume, path[^1], V(0.05f, 0.04f, 0.12f) * k, red);
        }
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.065f * s, 0.005f, -0.01f) * k, V(0.03f, 0.02f, 0.06f) * k, Rgb(40, 34, 32), V(0, 30f * s, -10f * s));
            b.Eye(head, On(c, r, 0.05f * s * k, c.Y + 0.01f * k), V(0.6f * s, 0.05f, 1f), 0.026f * k, glare: true);
        });
        return b;
    }

    private static PokeBuilder Pidgeotto() => BigPidgey(false);

    private static PokeBuilder Pidgeot() => BigPidgey(true);

    // ------------------------------------------------------------------ Spearow line

    private static PokeBuilder Spearow()
    {
        var b = new PokeBuilder("Spearow", 0.52f, BodyPlan.Bird, V(0, 0.25f, 0)) { Coat = Fur }.Hover();
        var brown = Rgb(156, 98, 62);
        var cream = Rgb(244, 224, 196);
        var red = Rgb(222, 98, 100);
        BirdLegs(b, 0.06f, 0.14f, 0.02f, 0.017f, Rgb(226, 160, 150));
        b.Ell(Body, V(0, 0.255f, 0), V(0.15f, 0.15f, 0.17f), brown);
        b.PaintEll(Body, V(0, 0.225f, 0.08f), V(0.11f, 0.12f, 0.11f), cream);
        int tail = b.Tail(V(0, 0.245f, -0.14f));
        TailFan(b, tail, V(0, 0.255f, -0.13f), 3, 36f, 0.14f, 0.04f, brown, Rgb(110, 70, 44));
        // Wings of rosy red, folded at its sides
        PokeBuilder.Both(s => FoldedWing(b, s, V(0.12f * s, 0.325f, 0.04f), V(0.15f * s, 0.215f, -0.18f), 0.065f, red, PixelCanvas.Shadow(red, 0.15f)));

        int head = b.Head(V(0, 0.375f, 0.07f));
        var c = V(0, 0.445f, 0.08f);
        var r = V(0.11f, 0.105f, 0.11f);
        b.Ell(head, c, r, brown);
        // A shock of ragged feathers on its crown, a short hooked beak, a glare
        foreach (var (x, y, z) in new[] { (0f, 0.635f, -0.02f), (-0.06f, 0.595f, -0.06f), (0.06f, 0.595f, -0.06f), (-0.03f, 0.575f, -0.12f), (0.04f, 0.555f, -0.13f), (-0.09f, 0.525f, 0.0f), (0.09f, 0.525f, 0.0f) })
            b.Spike(head, c + V(x * 0.4f, 0.04f, z * 0.3f), V(x, y, z), 0.034f, brown, 0.5f);
        b.Spike(head, V(0, 0.425f, 0.18f), V(0, 0.385f, 0.24f), 0.03f, Rgb(232, 170, 160), 0.8f, Shell);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.455f), V(0.6f * s, 0.05f, 1f), 0.026f, glare: true));
        return b;
    }

    private static PokeBuilder Fearow()
    {
        var b = new PokeBuilder("Fearow", 0.9f, BodyPlan.Bird, V(0, 0.36f, 0)) { Coat = Fur }.Hover();
        var brown = Rgb(170, 112, 66);
        var light = Rgb(222, 186, 132);
        var cream = Rgb(240, 222, 190);
        var red = Rgb(224, 74, 64);
        BirdLegs(b, 0.08f, 0.2f, 0.03f, 0.022f, Rgb(226, 160, 150));
        b.Ell(Body, V(0, 0.36f, 0), V(0.17f, 0.17f, 0.24f), brown);
        b.PaintEll(Body, V(0, 0.32f, 0.11f), V(0.12f, 0.13f, 0.11f), light);
        int tail = b.Tail(V(0, 0.36f, -0.2f));
        TailFan(b, tail, V(0, 0.37f, -0.19f), 3, 30f, 0.28f, 0.05f, brown, light, 0.3f);
        PokeBuilder.Both(s => FoldedWing(b, s, V(0.13f * s, 0.46f, 0.07f), V(0.18f * s, 0.32f, -0.32f), 0.1f, brown, light));
        // A long neck with a pale ruff where it meets the body
        b.Limb(Body, V(0, 0.46f, 0.1f), V(0, 0.7f, 0.15f), 0.07f, 0.055f, brown);
        b.Ell(Body, V(0, 0.49f, 0.11f), V(0.11f, 0.07f, 0.1f), cream, blend: 0.03f);
        int head = b.Head(V(0, 0.7f, 0.15f));
        var c = V(0, 0.77f, 0.16f);
        var r = V(0.08f, 0.075f, 0.095f);
        b.Ell(head, c, r, brown);
        // The long, thin beak it spears prey with, and a red crest of feathers
        b.Spike(head, V(0, 0.76f, 0.24f), V(0, 0.7f, 0.54f), 0.034f, Rgb(232, 170, 164), 0.8f, Shell);
        foreach (var (x, h, z) in new[] { (0f, 0.16f, 0.0f), (-0.025f, 0.12f, -0.04f), (0.025f, 0.12f, -0.04f) })
            b.Spike(head, c + V(x, 0.05f, z + 0.02f), c + V(x * 2f, 0.05f + h, z - 0.08f), 0.03f, red, 0.5f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 0.785f), V(0.7f * s, 0.05f, 0.7f), 0.024f, glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Rattata line

    /// <summary>Whiskers fanned out from each side of a muzzle at <paramref name="at"/> (mirrored), <paramref name="length"/> long.</summary>
    private static void Whiskers(PokeBuilder b, int bone, Vector3 at, float length, Color color, float thick = 0.006f)
    {
        PokeBuilder.Both(s =>
        {
            foreach (float a in new[] { 14f, 0f, -14f })
            {
                var dir = Vector3.Normalize(V(s, MathF.Tan(a * Degree), 0.25f));
                var root = V(at.X * s, at.Y, at.Z);
                b.Tube(bone, new[] { root, root + dir * length * 0.5f + V(0, -0.006f, 0), root + dir * length }, thick, thick * 0.6f, color, blend: 0f);
            }
        });
    }

    /// <summary>A rat's pair of long front teeth hanging under its muzzle at <paramref name="at"/>.</summary>
    private static void BuckTeeth(PokeBuilder b, int bone, Vector3 at, float w, float h)
    {
        PokeBuilder.Both(s => b.Box(bone, at + V(w * 0.55f * s, -h * 0.5f, 0), V(w * 0.48f, h * 0.5f, w * 0.32f), w * 0.3f, White, V(-8f, 0, 0), Shell, 0.004f));
    }

    /// <summary>
    /// Rattata, and its Alolan form: a crouching rat with big round ears, a curled tail, buck teeth and whiskers.
    /// </summary>
    private static PokeBuilder RattataBuild(bool alola)
    {
        var b = new PokeBuilder(alola ? "Rattata-Alola" : "Rattata", 0.52f, BodyPlan.Quadruped, V(0, 0.17f, -0.04f)) { Coat = Fur };
        var fur = alola ? Rgb(64, 66, 80) : Rgb(172, 140, 204);
        var cream = alola ? Rgb(232, 214, 184) : Rgb(248, 228, 196);
        var paw = alola ? Rgb(170, 150, 134) : cream;
        foreach (var (z, front) in new[] { (0.1f, true), (-0.17f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.09f * s, 0.12f, z), front);
                b.Limb(leg, V(0.09f * s, 0.13f, z), V(0.1f * s, 0.04f, z + 0.02f), 0.04f, 0.03f, fur);
                b.Ell(leg, V(0.1f * s, 0.022f, z + 0.045f), V(0.034f, 0.022f, 0.05f), paw);
            });
        b.Ell(Body, V(0, 0.17f, -0.04f), V(0.12f, 0.11f, 0.2f), fur);
        b.PaintEll(Body, V(0, 0.12f, 0.02f), V(0.09f, 0.06f, 0.15f), cream);
        // A long tail, curled at its end
        int tail = b.Tail(V(0, 0.18f, -0.22f));
        b.Tube(tail, Smooth(3, V(0, 0.18f, -0.22f), V(0, 0.28f, -0.32f), V(0.02f, 0.42f, -0.34f)), 0.022f, 0.018f, fur, blend: 0f);
        Curl(b, tail, V(0.02f, 0.42f, -0.34f), V(0.02f, 0.47f, -0.3f), V(0, -0.2f, -1f), V(0, 1f, -0.2f), 0.05f, 0.9f, 0.018f, fur);

        int head = b.Head(V(0, 0.2f, 0.12f));
        var c = V(0, 0.26f, 0.17f);
        var r = V(0.1f, 0.09f, 0.1f);
        b.Ell(head, c, r, fur);
        b.Ell(head, V(0, 0.235f, 0.25f), V(0.06f, 0.05f, 0.06f), fur);
        b.PaintEll(head, V(0, 0.215f, 0.26f), V(alola ? 0.11f : 0.065f, 0.045f, 0.06f), cream);
        if (alola) PokeBuilder.Both(s => b.Ell(head, V(0.075f * s, 0.215f, 0.22f), V(0.05f, 0.04f, 0.045f), cream, blend: 0.02f));
        b.Ell(head, V(0, 0.255f, 0.31f), V(0.02f, 0.016f, 0.015f), alola ? Rgb(40, 34, 40) : Rgb(170, 110, 140), blend: 0.006f);
        BuckTeeth(b, head, V(0, 0.2f, 0.29f), 0.028f, 0.04f);
        Whiskers(b, head, V(0.045f, 0.23f, 0.28f), alola ? 0.1f : 0.12f, alola ? Rgb(220, 214, 200) : Rgb(70, 60, 80));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.32f, 0.13f));
            b.Ell(ear, V(0.08f * s, 0.37f, 0.13f), V(0.06f, 0.065f, 0.018f), fur, V(0, -15f * s, -25f * s));
            b.PaintEll(ear, V(0.085f * s, 0.37f, 0.145f), V(0.04f, 0.045f, 0.015f), alola ? Rgb(120, 120, 136) : cream, V(0, -15f * s, -25f * s));
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.29f), V(0.6f * s, 0.15f, 0.8f), 0.028f, Rgb(214, 40, 56), glare: alola));
        return b;
    }

    private static PokeBuilder Rattata() => RattataBuild(false);

    /// <summary>
    /// Raticate, and its Alolan form: a heavy rat sitting up on big hind feet, buck teeth, whiskers and a long scaly
    /// tail; the Alolan one is fat with great pale cheeks.
    /// </summary>
    private static PokeBuilder RaticateBuild(bool alola)
    {
        var b = new PokeBuilder(alola ? "Raticate-Alola" : "Raticate", 0.74f, BodyPlan.Biped, V(0, 0.27f, 0)) { Coat = Fur };
        var fur = alola ? Rgb(62, 60, 72) : Rgb(200, 150, 90);
        var lower = alola ? Rgb(96, 70, 56) : fur;
        var cream = alola ? Rgb(236, 220, 192) : Rgb(244, 222, 176);
        var tailColor = alola ? Rgb(226, 190, 170) : Rgb(214, 176, 122);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.12f * s, 0.12f, 0.0f));
            b.Ell(leg, V(0.13f * s, 0.1f, 0.0f), V(0.08f, 0.09f, 0.1f), lower);
            b.Ell(leg, V(0.14f * s, 0.025f, 0.08f), V(0.05f, 0.025f, 0.1f), cream);
            Claws(b, leg, V(0.14f * s, 0.02f, 0.18f), V(0.018f, 0, 0), V(0, -0.1f, 1f), 0.016f, 0.008f);
        });
        float girth = alola ? 1.15f : 1f;
        b.Ell(Body, V(0, 0.27f, 0), V(0.18f * girth, 0.22f, 0.16f * girth), fur);
        b.PaintEll(Body, V(0, 0.22f, 0.07f), V(0.13f * girth, 0.16f, 0.11f), alola ? lower : cream);
        if (alola) b.PaintEll(Body, V(0, 0.12f, 0), V(0.22f, 0.1f, 0.2f), lower);
        // A long scaly tail lying out behind it
        int tail = b.Tail(V(0, 0.12f, -0.14f));
        var path = Smooth(3, V(0, 0.12f, -0.14f), V(0.04f, 0.04f, -0.3f), V(0.14f, 0.03f, -0.42f), V(0.26f, 0.08f, -0.44f), V(0.32f, 0.2f, -0.4f));
        b.Tube(tail, path, 0.03f, 0.016f, tailColor, blend: 0f);
        for (int i = 2; i < path.Length - 2; i += 2)
            b.PaintTorus(tail, path[i], 0.026f - 0.0012f * i, 0.004f, PixelCanvas.Shadow(tailColor, 0.2f), Euler(path[i + 1] - path[i - 1]));
        // Small paws held up in front of it
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.34f, 0.08f));
            b.Limb(arm, V(0.12f * s, 0.34f, 0.08f), V(0.13f * s, 0.29f, 0.17f), 0.035f, 0.03f, fur);
            b.Ell(arm, V(0.13f * s, 0.28f, 0.19f), V(0.03f, 0.026f, 0.03f), cream);
            Claws(b, arm, V(0.13f * s, 0.26f, 0.21f), V(0.012f, 0, 0), V(0, -1f, 0.3f), 0.016f, 0.007f);
        });

        int head = b.Head(V(0, 0.44f, 0.04f));
        var c = V(0, 0.52f, 0.06f);
        var r = V(0.12f, 0.11f, 0.12f);
        b.Ell(head, c, r, fur);
        b.Ell(head, V(0, 0.48f, 0.15f), V(0.075f, 0.06f, 0.07f), alola ? cream : fur);
        if (alola) PokeBuilder.Both(s => b.Ell(head, V(0.1f * s, 0.46f, 0.1f), V(0.08f, 0.07f, 0.07f), cream, blend: 0.03f));
        else b.PaintEll(head, V(0, 0.46f, 0.17f), V(0.07f, 0.045f, 0.06f), cream);
        b.Ell(head, V(0, 0.5f, 0.215f), V(0.022f, 0.018f, 0.016f), Rgb(60, 40, 40), blend: 0.006f);
        BuckTeeth(b, head, V(0, 0.44f, 0.2f), 0.034f, 0.058f);
        Whiskers(b, head, V(0.05f, 0.48f, 0.2f), 0.17f, alola ? Rgb(240, 236, 226) : Rgb(244, 232, 210), 0.007f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.08f * s, 0.6f, 0.02f));
            b.Ell(ear, V(0.11f * s, 0.64f, 0.01f), V(0.045f, 0.05f, 0.016f), fur, V(0, -10f * s, -30f * s));
            b.PaintEll(ear, V(0.11f * s, 0.64f, 0.02f), V(0.028f, 0.032f, 0.012f), alola ? Rgb(110, 100, 110) : cream, V(0, -10f * s, -30f * s));
        });
        if (!alola) foreach (var (x, h) in new[] { (-0.03f, 0.08f), (0.0f, 0.1f), (0.03f, 0.08f) })
            b.Spike(head, V(x, 0.6f, 0.0f), V(x * 1.4f, 0.6f + h, -0.06f), 0.02f, fur, 0.5f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.55f), V(0.55f * s, 0.15f, 1f), 0.026f, alola ? Rgb(200, 40, 50) : null, glare: true));
        return b;
    }

    private static PokeBuilder Raticate() => RaticateBuild(false);

    // ------------------------------------------------------------------ Ekans line

    /// <summary>
    /// A snake's body: a tube along <paramref name="points"/> from the tail to the neck, a bone for every
    /// <paramref name="per"/> points, its radius given along it. Returns the last bone, for the head to hang from.
    /// </summary>
    private static int Coils(PokeBuilder b, Vector3[] points, Func<float, float> radius, Color color, int per = 2)
    {
        int parent = Body, bone = Body;
        for (int i = 0; i < points.Length - 1; i++)
        {
            if (i > 0 && i % per == 0)
            {
                bone = b.Part("seg" + i / per, parent, points[i], PokeRole.Segment, i / per);
                parent = bone;
            }
            float t0 = i / (float)(points.Length - 1), t1 = (i + 1) / (float)(points.Length - 1);
            b.Limb(bone, points[i], points[i + 1], radius(t0), radius(t1), color, blend: 0f);
        }
        return bone;
    }

    /// <summary>A coil of <paramref name="turns"/> laid on the ground round the middle, tightening as it rises a little, for a snake to rest on.</summary>
    private static Vector3[] Spiral3(float r0, float r1, float y0, float y1, float a0, float turns, int steps)
    {
        var points = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps, a = a0 + t * MathF.Tau * turns, rad = r0 + (r1 - r0) * t;
            points[i] = V(MathF.Sin(a) * rad, y0 + (y1 - y0) * t, -MathF.Cos(a) * rad);
        }
        return points;
    }

    private static PokeBuilder Ekans()
    {
        var purple = Rgb(176, 112, 196);
        var yellow = Rgb(244, 214, 82);
        // The tail's rattle raised behind, the body coiled twice on the ground, the neck rising out of the middle
        var coil = Spiral3(0.21f, 0.11f, 0.06f, 0.14f, 2.4f, 1.35f, 18);
        var points = new[] { V(0.12f, 0.2f, -0.3f), V(0.18f, 0.1f, -0.24f) }.Concat(coil)
            .Concat(new[] { V(0.0f, 0.22f, 0.06f), V(0, 0.33f, 0.08f), V(0, 0.42f, 0.07f) }).ToArray();
        var b = new PokeBuilder("Ekans", 0.7f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int neck = Coils(b, points, t => 0.028f + 0.03f * MathF.Min(1f, t * 3f), purple);
        // The yellow rattle on the tail's end, a yellow band at its throat
        int tail = b.Tail(points[0]);
        for (int i = 0; i < 4; i++)
            b.Ell(tail, points[0] + V(-0.01f * i, 0.03f + 0.035f * i, -0.01f * i), V(0.024f - 0.002f * i, 0.02f, 0.024f - 0.002f * i), yellow, blend: 0.006f);
        b.PaintTorus(neck, V(0, 0.36f, 0.078f), 0.058f, 0.016f, yellow, V(-6f, 0, 0));
        int head = b.Head(points[^1], neck);
        var c = V(0, 0.46f, 0.09f);
        var r = V(0.075f, 0.06f, 0.09f);
        b.Ell(head, c, r, purple);
        // Its jaws open wide, round yellow eyes on top of its head
        Grin(b, head, V(0, 0.44f, 0.17f), V(0.045f, 0.022f, 0.035f), Rgb(232, 130, 150));
        PokeBuilder.Both(s => b.Mark(head, V(0.015f * s, 0.475f, 0.172f), V(0.1f * s, 0.4f, 1f), 0.008f, 0.008f, Rgb(70, 40, 80)));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.04f * s, 0.49f), V(0.6f * s, 0.4f, 0.7f), 0.026f, sclera: true, white: yellow, pupil: Rgb(30, 24, 30)));
        return b;
    }

    private static PokeBuilder Arbok()
    {
        var purple = Rgb(150, 116, 184);
        var belly = Rgb(214, 198, 160);
        var coil = Spiral3(0.28f, 0.15f, 0.07f, 0.17f, 2.2f, 1.3f, 20);
        var points = new[] { V(0.34f, 0.05f, -0.36f), V(0.3f, 0.06f, -0.3f) }.Concat(coil)
            .Concat(new[] { V(0.0f, 0.28f, 0.08f), V(0, 0.44f, 0.1f), V(0, 0.58f, 0.08f), V(0, 0.72f, 0.06f) }).ToArray();
        var b = new PokeBuilder("Arbok", 0.95f, BodyPlan.Serpent, points[0]) { Coat = Scales };
        int neck = Coils(b, points, t => 0.022f + 0.05f * MathF.Min(1f, t * 2.5f), purple, 3);
        // The hood spread wide behind its head, the fearsome face on it in red, yellow and black
        int hood = b.Part("hood", neck, V(0, 0.6f, 0.06f), PokeRole.Fin);
        b.Ell(hood, V(0, 0.62f, 0.02f), V(0.2f, 0.19f, 0.035f), purple, blend: 0.03f);
        PokeBuilder.Both(s => b.Ell(hood, V(0.14f * s, 0.6f, 0.04f), V(0.11f, 0.17f, 0.035f), purple, V(0, -22f * s, 6f * s), blend: 0.04f));
        b.PaintEll(neck, V(0, 0.55f, 0.15f), V(0.05f, 0.14f, 0.04f), belly);
        PokeBuilder.Both(s =>
        {
            b.Mark(hood, V(0.13f * s, 0.66f, 0.072f), V(0.15f * s, 0, 1f), 0.06f, 0.075f, Rgb(244, 206, 70), MarkShape.Disc, 20f * s);
            b.Mark(hood, V(0.13f * s, 0.66f, 0.074f), V(0.15f * s, 0, 1f), 0.035f, 0.05f, Rgb(214, 54, 54), MarkShape.Disc, 20f * s);
            b.Mark(hood, V(0.11f * s, 0.54f, 0.068f), V(0.15f * s, 0, 1f), 0.05f, 0.025f, Rgb(36, 30, 40), MarkShape.Wave, -25f * s);
        });
        int head = b.Head(points[^1], neck);
        var c = V(0, 0.77f, 0.09f);
        var r = V(0.075f, 0.06f, 0.1f);
        b.Ell(head, c, r, purple);
        Gape(b, head, V(0, 0.745f, 0.18f), V(0.045f, 0.025f, 0.035f), Rgb(200, 90, 110), 0.025f);
        b.Tube(head, new[] { V(0, 0.74f, 0.17f), V(0, 0.735f, 0.24f), V(0.012f, 0.74f, 0.27f) }, 0.006f, 0.005f, Rgb(232, 110, 130), blend: 0f);
        b.Tube(head, new[] { V(0, 0.735f, 0.24f), V(-0.012f, 0.73f, 0.27f) }, 0.006f, 0.005f, Rgb(232, 110, 130), blend: 0f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.795f), V(0.6f * s, 0.4f, 0.7f), 0.024f, sclera: true, white: Rgb(244, 214, 82), pupil: Rgb(30, 24, 30), glare: true));
        return b;
    }

    // ------------------------------------------------------------------ Sandshrew line

    /// <summary>Seams of armour painted round an upright body: rings at the heights given, and two that run over the top.</summary>
    private static void ArmourSeams(PokeBuilder b, int bone, Vector3 c, Vector3 r, Color seam, params float[] heights)
    {
        foreach (float y in heights)
        {
            float f = MathF.Sqrt(MathF.Max(0.05f, 1f - (y - c.Y) * (y - c.Y) / (r.Y * r.Y)));
            b.PaintTorus(bone, V(c.X, y, c.Z), r.X * f, 0.006f, seam, default, sz: r.Z / r.X);
        }
        foreach (float yaw in new[] { -32f, 32f })
            b.PaintTorus(bone, c, r.Y, 0.006f, seam, V(0, yaw, 90f), sz: r.Z / r.Y);
    }

    /// <summary>Sandshrew and its Alolan form: a round little armadillo standing upright, its back armoured in plates.</summary>
    private static PokeBuilder SandshrewBuild(bool alola)
    {
        var b = new PokeBuilder(alola ? "Sandshrew-Alola" : "Sandshrew", 0.58f, BodyPlan.Biped, V(0, 0.27f, 0)) { Coat = Shell };
        var skin = alola ? Rgb(210, 230, 248) : Rgb(236, 204, 112);
        var seam = alola ? Rgb(124, 166, 214) : Rgb(176, 136, 64);
        var belly = alola ? Rgb(244, 238, 210) : Rgb(248, 234, 196);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.15f, 0.0f));
            b.Ell(leg, V(0.11f * s, 0.12f, 0.01f), V(0.075f, 0.09f, 0.085f), skin);
            b.Ell(leg, V(0.115f * s, 0.03f, 0.06f), V(0.05f, 0.03f, 0.07f), skin);
            Claws(b, leg, V(0.115f * s, 0.02f, 0.125f), V(0.02f, 0, 0), V(0, -0.2f, 1f), 0.022f, 0.01f);
        });
        var c = V(0, 0.27f, -0.01f);
        var r = V(0.16f, 0.18f, 0.15f);
        b.Ell(Body, c, r, skin);
        ArmourSeams(b, Body, c, r, seam, 0.18f, 0.27f, 0.36f);
        b.PaintEll(Body, V(0, 0.25f, 0.08f), V(0.11f, 0.14f, 0.09f), belly);
        int tail = b.Tail(V(0, 0.16f, -0.12f));
        b.Limb(tail, V(0, 0.16f, -0.12f), V(0, 0.05f, -0.3f), 0.065f, 0.025f, skin);
        foreach (float t in new[] { 0.3f, 0.6f })
            b.PaintTorus(tail, Vector3.Lerp(V(0, 0.16f, -0.12f), V(0, 0.05f, -0.3f), t), 0.065f - 0.04f * t, 0.006f, seam, Euler(V(0, -0.11f, -0.18f)));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.33f, 0.04f));
            b.Limb(arm, V(0.13f * s, 0.33f, 0.04f), V(0.17f * s, 0.26f, 0.1f), 0.04f, 0.035f, skin);
            Claws(b, arm, V(0.18f * s, 0.24f, 0.12f), V(0.014f, 0, 0), V(0.2f * s, -0.6f, 1f), 0.02f, 0.009f);
        });
        int head = b.Head(V(0, 0.4f, 0.02f));
        var hc = V(0, 0.48f, 0.03f);
        var hr = V(0.11f, 0.1f, 0.11f);
        b.Ell(head, hc, hr, skin);
        // A pointed snout pale underneath, little pointed ears, big dark eyes
        b.Ell(head, V(0, 0.45f, 0.12f), V(0.06f, 0.045f, 0.075f), skin);
        b.PaintEll(head, V(0, 0.43f, 0.13f), V(0.065f, 0.03f, 0.08f), belly);
        b.Ell(head, V(0, 0.46f, 0.195f), V(0.016f, 0.012f, 0.012f), alola ? Rgb(70, 100, 150) : Rgb(120, 80, 40), blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.56f, 0.0f));
            b.Spike(ear, V(0.06f * s, 0.55f, 0.0f), V(0.085f * s, 0.615f, -0.02f), 0.03f, skin, 0.5f);
        });
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.055f * s, 0.5f), V(0.55f * s, 0.12f, 1f), 0.032f, alola ? Rgb(60, 110, 200) : Rgb(40, 74, 150)));
        return b;
    }

    private static PokeBuilder Sandshrew() => SandshrewBuild(false);

    /// <summary>
    /// Sandslash and its Alolan form: hunched forward, its back bristling with quills (sharp brown spines, or spikes of
    /// ice), two great claws on each hand.
    /// </summary>
    private static PokeBuilder SandslashBuild(bool alola)
    {
        var b = new PokeBuilder(alola ? "Sandslash-Alola" : "Sandslash", 0.8f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Shell };
        var skin = alola ? Rgb(200, 226, 246) : Rgb(236, 198, 106);
        var belly = alola ? Rgb(244, 244, 240) : Rgb(248, 234, 196);
        var quill = alola ? Rgb(150, 212, 246) : Rgb(146, 96, 58);
        var tip = alola ? Rgb(226, 246, 255) : Rgb(200, 156, 104);
        var claw = alola ? Rgb(188, 216, 238) : White;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.22f, -0.02f));
            b.Ell(leg, V(0.12f * s, 0.18f, 0.0f), V(0.08f, 0.1f, 0.09f), skin);
            b.Limb(leg, V(0.13f * s, 0.1f, 0.02f), V(0.13f * s, 0.04f, 0.04f), 0.045f, 0.04f, skin);
            b.Ell(leg, V(0.13f * s, 0.03f, 0.065f), V(0.046f, 0.03f, 0.062f), skin);
            Claws(b, leg, V(0.13f * s, 0.025f, 0.11f), V(0.02f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.011f);
        });
        var c = V(0, 0.36f, -0.02f);
        var r = V(0.16f, 0.2f, 0.15f);
        b.Ell(Body, c, r, skin, V(15f, 0, 0));
        b.PaintEll(Body, V(0, 0.33f, 0.08f), V(0.11f, 0.15f, 0.09f), belly, V(15f, 0, 0));
        // Rows of quills over its back, swept back and up
        for (int row = 0; row < 4; row++)
            for (int col = 0; col < 5 - (row == 0 ? 2 : 0); col++)
            {
                float elev = (70f - row * 20f) * Degree;
                int cols = 5 - (row == 0 ? 2 : 0);
                float phi = ((col - (cols - 1) / 2f) * (row == 0 ? 34f : 30f)) * Degree;
                var dir = Vector3.Normalize(V(MathF.Sin(phi) * MathF.Cos(elev), MathF.Sin(elev), -MathF.Cos(phi) * MathF.Cos(elev)));
                var root = c + dir * V(r.X, r.Y, r.Z) * 0.82f;
                float len = (alola ? 0.24f : 0.2f) - row * 0.02f;
                var to = root + Vector3.Normalize(dir + V(0, alola ? 0.5f : 0.15f, -0.45f)) * len;
                b.Spike(Body, root, to, alola ? 0.04f : 0.05f, quill, alola ? 0.75f : 0.45f, Shell, 0.012f);
                b.PaintEll(Body, Vector3.Lerp(root, to, 0.82f), V(0.03f, 0.05f, 0.03f), tip, Euler(to - root));
            }
        int tail = b.Tail(V(0, 0.22f, -0.14f));
        b.Limb(tail, V(0, 0.22f, -0.14f), V(0, 0.1f, -0.3f), 0.06f, 0.03f, skin);
        b.Spike(tail, V(0, 0.16f, -0.24f), V(0, 0.26f, -0.36f), 0.035f, quill, 0.5f, Shell);
        // Arms held forward, two great curved claws on each hand
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.44f, 0.06f));
            b.Limb(arm, V(0.13f * s, 0.44f, 0.06f), V(0.19f * s, 0.34f, 0.16f), 0.045f, 0.04f, skin);
            foreach (float dx in new[] { -0.02f, 0.02f })
                b.Tube(arm, Smooth(3, V((0.2f + dx) * s, 0.33f, 0.18f), V((0.22f + dx) * s, 0.27f, 0.25f), V((0.21f + dx) * s, 0.2f, 0.27f)), 0.02f, 0.006f, claw, Shell, 0f);
        });
        int head = b.Head(V(0, 0.5f, 0.08f));
        var hc = V(0, 0.56f, 0.1f);
        var hr = V(0.095f, 0.085f, 0.1f);
        b.Ell(head, hc, hr, skin);
        b.Ell(head, V(0, 0.53f, 0.18f), V(0.05f, 0.04f, 0.07f), skin);
        b.PaintEll(head, V(0, 0.515f, 0.19f), V(0.055f, 0.025f, 0.075f), belly);
        b.Ell(head, V(0, 0.545f, 0.25f), V(0.014f, 0.011f, 0.011f), alola ? Rgb(70, 100, 150) : Rgb(120, 80, 40), blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.055f * s, 0.63f, 0.06f));
            b.Spike(ear, V(0.055f * s, 0.62f, 0.06f), V(0.09f * s, 0.71f, 0.02f), 0.03f, skin, 0.5f);
        });
        if (alola) foreach (var (x, h) in new[] { (0f, 0.14f), (-0.04f, 0.1f), (0.04f, 0.1f) })
            b.Spike(head, V(x, 0.63f, 0.08f), V(x * 1.5f, 0.63f + h, 0.0f), 0.025f, quill, 0.7f, Shell);
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.05f * s, 0.575f), V(0.6f * s, 0.12f, 1f), 0.024f, alola ? Rgb(60, 110, 200) : Rgb(40, 74, 150), glare: true));
        return b;
    }

    private static PokeBuilder Sandslash() => SandslashBuild(false);

    // ------------------------------------------------------------------ Nidoran lines

    /// <summary>
    /// The four-legged Nidoran: Nidoran♀ and Nidorina (<paramref name="male"/> false), Nidoran♂ and Nidorino. Great
    /// round ears, spines down the back, dark spots, two small teeth showing, and on the males a horn.
    /// </summary>
    private static PokeBuilder NidoBuild(bool male, bool evolved)
    {
        string name = male ? (evolved ? "Nidorino" : "Nidoran♂") : (evolved ? "Nidorina" : "Nidoran♀");
        float k = evolved ? 1.25f : 1f;
        var b = new PokeBuilder(name, evolved ? 0.7f : 0.5f, BodyPlan.Quadruped, V(0, 0.2f * k, -0.03f)) { Coat = Fur };
        var skin = male ? Rgb(190, 128, 200) : Rgb(162, 190, 232);
        var spot = male ? Rgb(134, 74, 152) : Rgb(92, 116, 186);
        var inner = male ? Rgb(64, 156, 150) : Rgb(80, 140, 170);
        foreach (var (z, front) in new[] { (0.1f, true), (-0.15f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.1f * s * k, 0.14f * k, z * k), front);
                b.Limb(leg, V(0.1f * s * k, 0.15f * k, z * k), V(0.11f * s * k, 0.04f, (z + 0.02f) * k), 0.045f * k, 0.038f * k, skin);
                b.Ell(leg, V(0.11f * s * k, 0.025f, (z + 0.05f) * k), V(0.042f, 0.025f, 0.055f) * k, skin);
                Claws(b, leg, V(0.11f * s * k, 0.018f, (z + 0.1f) * k), V(0.016f * k, 0, 0), V(0, -0.2f, 1f), 0.016f * k, 0.007f * k);
            });
        var c = V(0, 0.2f, -0.03f) * k;
        var r = V(0.13f, 0.12f, 0.16f) * k * (evolved ? V(1.1f, 1.15f, 1f) : V(1f, 1f, 1f));
        b.Ell(Body, c, r, skin);
        foreach (var (x, y, z) in new[] { (0.11f, 0.24f, 0.02f), (0.1f, 0.19f, -0.1f), (0.06f, 0.29f, -0.06f), (-0.11f, 0.22f, -0.04f), (-0.08f, 0.27f, 0.06f), (-0.1f, 0.18f, -0.14f) })
            b.PaintEll(Body, V(x, y, z) * k, V(0.025f, 0.02f, 0.025f) * k, spot);
        // Spines down its back, bigger on the males
        for (int i = 0; i < 4; i++)
        {
            var root = V(0, 0.3f + 0.01f * (i == 0 ? 1 : 0), 0.06f - 0.07f * i) * k;
            float h = (male ? 0.09f : 0.06f) * (1f - 0.12f * i) * k;
            b.Spike(Body, root, root + V(0, h, -h * 0.5f), (male ? 0.034f : 0.028f) * k, skin, 0.5f);
        }
        int tail = b.Tail(V(0, 0.2f, -0.2f) * k);
        b.Spike(tail, V(0, 0.2f, -0.19f) * k, V(0, 0.24f, -0.28f) * k, 0.035f * k, skin);

        int head = b.Head(V(0, 0.27f, 0.1f) * k);
        var hc = V(0, 0.31f, 0.15f) * k;
        var hr = V(0.115f, 0.105f, 0.11f) * k;
        b.Ell(head, hc, hr, skin);
        b.Ell(head, V(0, 0.275f, 0.23f) * k, V(0.062f, 0.05f, 0.06f) * k, skin);
        // Two small teeth jutting from its upper lip, fangs below on Nidorina, a pointed horn on the males
        PokeBuilder.Both(s => b.Spike(head, V(0.035f * s, 0.27f, 0.265f) * k, V(0.09f * s, 0.25f, 0.285f) * k, 0.008f * k, White, mat: Shell, blend: 0.003f));
        if (male) b.Spike(head, V(0, 0.38f, 0.2f) * k, V(0, (evolved ? 0.52f : 0.47f), (evolved ? 0.25f : 0.23f)) * k, (evolved ? 0.035f : 0.03f) * k, skin, mat: Shell);
        else if (evolved) PokeBuilder.Both(s => b.Spike(head, V(0.025f * s, 0.255f, 0.275f) * k, V(0.025f * s, 0.23f, 0.278f) * k, 0.008f * k, White, mat: Shell, blend: 0.003f));
        b.Ell(head, V(0, 0.29f, 0.285f) * k, V(0.015f, 0.012f, 0.01f) * k, PixelCanvas.Shadow(spot, 0.2f), blend: 0.005f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.38f, 0.12f) * k);
            var ec = V(0.125f * s, 0.45f, 0.1f) * k;
            float big = male ? 1.15f : 1f;
            b.Ell(ear, ec, V(0.065f, 0.085f, 0.02f) * k * big, skin, V(-10f, 25f * s, -28f * s));
            b.PaintEll(ear, ec + V(0, 0, 0.014f) * k, V(0.045f, 0.065f, 0.016f) * k * big, inner, V(-10f, 25f * s, -28f * s));
            if (evolved) b.Spike(ear, ec + V(0.04f * s, 0.06f, -0.01f) * k, ec + V(0.07f * s, 0.12f, -0.02f) * k, 0.02f * k, skin, 0.5f);
        });
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.055f * s * k, 0.335f * k), V(0.6f * s, 0.12f, 1f), 0.029f * k, Rgb(212, 44, 56), glare: evolved));
        return b;
    }

    private static PokeBuilder NidoranF() => NidoBuild(false, false);

    private static PokeBuilder Nidorina() => NidoBuild(false, true);

    private static PokeBuilder NidoranM() => NidoBuild(true, false);

    private static PokeBuilder Nidorino() => NidoBuild(true, true);

    /// <summary>
    /// Nidoqueen and Nidoking: standing up on thick legs, a pale belly, great ears, spines down the back and a heavy
    /// tail; Nidoqueen's armour plated and her mouth open wide, Nidoking with his great horn and fangs.
    /// </summary>
    private static PokeBuilder NidoMonarch(bool king)
    {
        var b = new PokeBuilder(king ? "Nidoking" : "Nidoqueen", 0.95f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Scales };
        var skin = king ? Rgb(176, 116, 192) : Rgb(96, 150, 210);
        var belly = king ? Rgb(232, 222, 226) : Rgb(236, 220, 168);
        var inner = king ? Rgb(66, 150, 140) : Rgb(130, 96, 80);
        var spot = PixelCanvas.Shadow(skin, 0.3f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.14f * s, 0.28f, 0.0f));
            b.Ell(leg, V(0.16f * s, 0.24f, 0.02f), V(0.1f, 0.12f, 0.11f), skin);
            b.Limb(leg, V(0.17f * s, 0.15f, 0.03f), V(0.17f * s, 0.05f, 0.03f), 0.075f, 0.07f, skin);
            b.Ell(leg, V(0.175f * s, 0.035f, 0.07f), V(0.08f, 0.035f, 0.1f), skin);
            Claws(b, leg, V(0.175f * s, 0.025f, 0.16f), V(0.03f, 0, 0), V(0, -0.1f, 1f), 0.026f, 0.012f);
        });
        var c = V(0, 0.44f, 0);
        var r = V(0.2f, 0.24f, 0.17f);
        b.Ell(Body, c, r, skin);
        b.PaintEll(Body, V(0, 0.42f, 0.09f), V(0.15f, 0.21f, 0.1f), belly);
        if (!king)
            foreach (float y in new[] { 0.5f, 0.38f })
            {
                var at = On(c, r, 0, y);
                b.Mark(Body, at, Outward(c, r, at), 0.11f, 0.009f, PixelCanvas.Shadow(belly, 0.3f), MarkShape.Bar);
            }
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.17f * s, 0.5f, -0.06f), V(0.04f, 0.035f, 0.04f), spot));
        // Spines down the back and a thick tail
        for (int i = 0; i < 5; i++)
        {
            float y = 0.64f - 0.07f * i, z = -0.1f - 0.025f * i;
            b.Spike(Body, V(0, y, z), V(0, y + 0.06f, z - 0.1f), 0.04f, skin, 0.45f);
        }
        int tail = b.Tail(V(0, 0.28f, -0.14f));
        b.Tube(tail, Smooth(3, V(0, 0.28f, -0.14f), V(0.02f, 0.14f, -0.32f), V(0.06f, 0.08f, (king ? -0.52f : -0.46f))), 0.09f, 0.035f, skin, blend: 0f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.18f * s, 0.56f, 0.04f));
            b.Limb(arm, V(0.18f * s, 0.56f, 0.04f), V(0.28f * s, 0.44f, 0.12f), 0.06f, 0.05f, skin);
            b.Ell(arm, V(0.29f * s, 0.42f, 0.14f), V(0.05f, 0.045f, 0.05f), skin);
            Claws(b, arm, V(0.3f * s, 0.39f, 0.18f), V(0.018f, 0, 0), V(0.1f * s, -0.6f, 1f), 0.028f, 0.01f);
        });
        int head = b.Head(V(0, 0.64f, 0.06f));
        var hc = V(0, 0.73f, 0.09f);
        var hr = V(0.11f, 0.095f, 0.12f);
        b.Ell(head, hc, hr, skin);
        b.Ell(head, V(0, 0.7f, 0.18f), V(0.075f, 0.06f, 0.08f), skin);
        if (king)
        {
            // The great horn on his brow, fangs bared
            b.Spike(head, V(0, 0.8f, 0.14f), V(0, 0.98f, 0.2f), 0.045f, skin, mat: Shell);
            Gape(b, head, V(0, 0.675f, 0.245f), V(0.045f, 0.018f, 0.03f), Rgb(150, 60, 80), 0.02f);
        }
        else
        {
            Gape(b, head, V(0, 0.68f, 0.24f), V(0.05f, 0.035f, 0.035f), Rgb(220, 120, 140), 0.016f);
            b.Spike(head, V(0, 0.81f, 0.12f), V(0, 0.87f, 0.14f), 0.025f, skin, mat: Shell);
        }
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.06f * s, 0.79f, 0.06f));
            var ec = V(0.1f * s, 0.84f, 0.05f);
            b.Ell(ear, ec, V(0.06f, 0.08f, 0.02f), skin, V(-10f, 25f * s, -25f * s));
            b.PaintEll(ear, ec + V(0, 0, 0.014f), V(0.04f, 0.06f, 0.016f), inner, V(-10f, 25f * s, -25f * s));
        });
        PokeBuilder.Both(s => b.Eye(head, On(hc, hr, 0.055f * s, 0.75f), V(0.6f * s, 0.12f, 1f), 0.026f, king ? Rgb(60, 150, 100) : Rgb(212, 44, 56), glare: true));
        return b;
    }

    private static PokeBuilder Nidoqueen() => NidoMonarch(false);

    private static PokeBuilder Nidoking() => NidoMonarch(true);

    // ------------------------------------------------------------------ Vulpix line

    /// <summary>
    /// A tail from <paramref name="root"/> out along <paramref name="dir"/>, <paramref name="length"/> long, its end
    /// curled up and over toward the body.
    /// </summary>
    private static void CurledTail(PokeBuilder b, int bone, Vector3 root, Vector3 dir, float length, float thick, Color color, float curl)
    {
        var up = Vector3.Normalize(Vector3.Cross(Vector3.Cross(dir, V(0, 1f, 0)), dir));
        if (up.Y < 0f) up = -up;
        var end = root + dir * length + up * length * 0.15f;
        b.Tube(bone, Smooth(3, root, root + dir * length * 0.5f + up * length * 0.12f, end), thick, thick * 0.85f, color, blend: 0f);
        Curl(b, bone, end, end + up * curl, -up, dir, curl, 0.95f, thick * 0.85f, color);
    }

    /// <summary>Vulpix and its Alolan form: a little fox with six curled tails and curls of hair on its head.</summary>
    private static PokeBuilder VulpixBuild(bool alola)
    {
        var b = new PokeBuilder(alola ? "Vulpix-Alola" : "Vulpix", 0.56f, BodyPlan.Quadruped, V(0, 0.2f, -0.04f)) { Coat = Fur };
        var fur = alola ? Rgb(242, 246, 252) : Rgb(202, 106, 70);
        var chest = alola ? Rgb(252, 252, 255) : Rgb(246, 224, 186);
        var curl = alola ? Rgb(210, 230, 250) : Rgb(246, 148, 74);
        var paw = alola ? Rgb(184, 212, 238) : Rgb(128, 64, 42);
        foreach (var (z, front) in new[] { (0.09f, true), (-0.15f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.07f * s, 0.15f, z), front);
                b.Limb(leg, V(0.07f * s, 0.16f, z), V(0.075f * s, 0.04f, z + 0.01f), 0.033f, 0.026f, fur);
                b.PaintEll(leg, V(0.075f * s, 0.06f, z + 0.01f), V(0.035f, 0.05f, 0.035f), paw);
                b.Ell(leg, V(0.076f * s, 0.022f, z + 0.03f), V(0.03f, 0.022f, 0.04f), paw);
            });
        b.Ell(Body, V(0, 0.2f, -0.03f), V(0.095f, 0.09f, 0.16f), fur);
        b.PaintEll(Body, V(0, 0.2f, 0.1f), V(0.07f, 0.075f, 0.05f), chest);
        // Six tails fanned up behind it, each curled at its end
        int tail = b.Tail(V(0, 0.22f, -0.17f));
        for (int i = 0; i < 6; i++)
        {
            float a = (-70f + 28f * i) * Degree;
            float rise = i % 2 == 0 ? 0.95f : 0.6f;
            var dir = Vector3.Normalize(V(MathF.Sin(a) * 1.1f, rise - 0.3f * MathF.Abs(MathF.Sin(a)), -0.5f));
            CurledTail(b, tail, V(0, 0.23f, -0.17f), dir, i % 2 == 0 ? 0.2f : 0.17f, alola ? 0.038f : 0.032f, curl, alola ? 0.048f : 0.042f);
        }
        int head = b.Head(V(0, 0.26f, 0.09f));
        var c = V(0, 0.3f, 0.12f);
        var r = V(0.085f, 0.08f, 0.085f);
        b.Ell(head, c, r, fur);
        b.Ell(head, V(0, 0.275f, 0.19f), V(0.04f, 0.032f, 0.05f), fur);
        b.PaintEll(head, V(0, 0.265f, 0.2f), V(0.042f, 0.022f, 0.05f), chest);
        b.Ell(head, V(0, 0.285f, 0.24f), V(0.012f, 0.009f, 0.008f), alola ? Rgb(120, 140, 170) : Rgb(60, 36, 30), blend: 0.004f);
        // Pointed ears dark inside, and curls of hair piled on its crown
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.05f * s, 0.36f, 0.1f));
            b.Spike(ear, V(0.05f * s, 0.35f, 0.1f), V(0.1f * s, 0.47f, 0.07f), 0.04f, fur, 0.5f);
            b.PaintEll(ear, V(0.075f * s, 0.41f, 0.095f), V(0.02f, 0.045f, 0.02f), alola ? Rgb(170, 200, 230) : Rgb(110, 56, 40));
        });
        foreach (var (x, y, z, rr) in new[] { (0f, 0.4f, 0.11f, 0.03f), (-0.035f, 0.385f, 0.14f, 0.025f), (0.035f, 0.385f, 0.14f, 0.025f), (0f, 0.39f, 0.07f, 0.028f) })
            Curl(b, head, V(x * 0.5f, y - 0.02f, z), V(x, y, z), V(0, -1f, 0), Vector3.Normalize(V(x * 3f, 0, 1f)), rr, 0.9f, 0.018f, curl);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.042f * s, 0.315f), V(0.6f * s, 0.12f, 1f), 0.028f, alola ? Rgb(70, 130, 210) : Rgb(120, 66, 44)));
        return b;
    }

    private static PokeBuilder Vulpix() => VulpixBuild(false);

    /// <summary>
    /// Ninetales and its Alolan form: a slender fox standing tall, a ruff at its breast, a crest swept back from its
    /// head and nine long tails fanned out behind; gold tipped in orange, or pale blue drifting like snow.
    /// </summary>
    private static PokeBuilder NinetalesBuild(bool alola)
    {
        var b = new PokeBuilder(alola ? "Ninetales-Alola" : "Ninetales", 0.9f, BodyPlan.Quadruped, V(0, 0.34f, -0.04f)) { Coat = Fur };
        var fur = alola ? Rgb(238, 244, 252) : Rgb(246, 228, 164);
        var tails = alola ? Rgb(198, 226, 248) : Rgb(248, 232, 170);
        var tip = alola ? Rgb(150, 196, 240) : Rgb(246, 156, 72);
        var ruff = alola ? Rgb(250, 252, 255) : Rgb(252, 246, 222);
        foreach (var (z, front) in new[] { (0.12f, true), (-0.2f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.08f * s, 0.28f, z), front);
                b.Limb(leg, V(0.08f * s, 0.3f, z), V(0.085f * s, 0.05f, z + 0.01f), 0.04f, 0.028f, fur);
                b.Ell(leg, V(0.086f * s, 0.025f, z + 0.035f), V(0.032f, 0.025f, 0.045f), fur);
            });
        b.Ell(Body, V(0, 0.36f, -0.04f), V(0.11f, 0.1f, 0.21f), fur);
        foreach (var (x, y, z) in new[] { (0f, 0.42f, 0.14f), (-0.05f, 0.38f, 0.13f), (0.05f, 0.38f, 0.13f), (0f, 0.34f, 0.14f) })
            b.Ell(Body, V(x, y, z), V(0.06f, 0.06f, 0.05f), ruff, blend: 0.03f);
        // Nine tails fanned out behind it like a great plume
        int tail = b.Tail(V(0, 0.4f, -0.22f));
        for (int i = 0; i < 9; i++)
        {
            // Each rises from the rump, arches back and flows down toward the ground, the nine spread like a fan
            float a = (-72f + 18f * i) * Degree;
            var flat = Vector3.Normalize(V(MathF.Sin(a), 0, -MathF.Cos(a) * 0.9f));
            var root = V(0, 0.41f, -0.22f);
            float len = 0.5f - 0.08f * MathF.Abs(MathF.Sin(a));
            float sway = alola ? MathF.Sin(i * 1.7f) * 0.04f : 0f;
            var path = Smooth(4, root, root + flat * len * 0.3f + V(sway, 0.2f, 0), root + flat * len * 0.66f + V(-sway, 0.2f, 0),
                root + flat * len + V(sway, alola ? 0.08f : 0.04f, 0));
            b.Tube(tail, path, 0.066f, 0.034f, tails, blend: 0f);
            b.PaintEll(tail, path[^1], V(0.055f, 0.06f, 0.09f), tip, Euler(path[^1] - path[^4]));
        }
        b.Limb(Body, V(0, 0.44f, 0.12f), V(0, 0.56f, 0.16f), 0.055f, 0.045f, fur);
        int head = b.Head(V(0, 0.56f, 0.16f));
        var c = V(0, 0.61f, 0.17f);
        var r = V(0.075f, 0.07f, 0.08f);
        b.Ell(head, c, r, fur);
        b.Ell(head, V(0, 0.585f, 0.24f), V(0.035f, 0.03f, 0.055f), fur);
        b.Ell(head, V(0, 0.59f, 0.29f), V(0.011f, 0.009f, 0.008f), alola ? Rgb(120, 140, 170) : Rgb(70, 50, 40), blend: 0.004f);
        // Pointed ears, and the long crest of fur swept back from its brow
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.045f * s, 0.67f, 0.14f));
            b.Spike(ear, V(0.045f * s, 0.66f, 0.14f), V(0.09f * s, 0.77f, 0.1f), 0.035f, fur, 0.5f);
        });
        int crest = b.Part("crest", head, V(0, 0.67f, 0.15f), PokeRole.Ear);
        foreach (var (x, lift) in new[] { (-0.03f, 0.1f), (0f, 0.14f), (0.03f, 0.1f), (-0.015f, 0.06f), (0.015f, 0.06f) })
            b.Tube(crest, Smooth(3, V(x * 0.5f, 0.67f, 0.17f), V(x, 0.67f + lift, 0.08f), V(x * 1.5f, 0.66f + lift * 0.8f, -0.06f), V(x * 2f, 0.6f + lift * 0.3f, -0.18f)), 0.024f, 0.01f, alola ? tails : fur, blend: 0f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.037f * s, 0.625f), V(0.65f * s, 0.12f, 0.8f), 0.022f, alola ? Rgb(70, 130, 210) : Rgb(214, 50, 56), glare: true));
        return b;
    }

    private static PokeBuilder Ninetales() => NinetalesBuild(false);

    // ------------------------------------------------------------------ Jigglypuff line

    private static PokeBuilder Jigglypuff()
    {
        var b = new PokeBuilder("Jigglypuff", 0.5f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var pink = Rgb(252, 198, 212);
        var black = Rgb(48, 40, 52);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.06f, 0.02f));
            b.Ell(leg, V(0.085f * s, 0.03f, 0.06f), V(0.05f, 0.03f, 0.07f), pink);
            int arm = b.Arm(s, V(0.16f * s, 0.2f, 0.04f));
            b.Ell(arm, V(0.185f * s, 0.19f, 0.06f), V(0.04f, 0.03f, 0.035f), pink);
        });
        // A round balloon of a body that is all head, pointed ears dark inside, a curl of hair on its brow
        var c = V(0, 0.21f, 0);
        var r = V(0.19f, 0.18f, 0.18f);
        b.Ell(Body, c, r, pink);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(Body, s, V(0.1f * s, 0.34f, 0.0f));
            b.Spike(ear, V(0.1f * s, 0.32f, 0.0f), V(0.16f * s, 0.44f, 0.0f), 0.065f, pink, 0.5f);
            b.PaintEll(ear, V(0.13f * s, 0.39f, 0.02f), V(0.03f, 0.05f, 0.03f), black, V(0, 0, -25f * s));
        });
        Curl(b, Body, V(-0.04f, 0.37f, 0.06f), V(0.0f, 0.31f, 0.14f), V(-1f, 0.3f, 0), V(0, 0.95f, 0.3f), 0.05f, 1.0f, 0.026f, pink);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.075f * s, 0.23f), V(0.45f * s, 0.05f, 1f), 0.058f, sclera: true, pupil: Rgb(40, 150, 172)));
        b.Mark(Body, On(c, r, 0, 0.14f), V(0, -0.3f, 1f), 0.04f, 0.014f, Rgb(160, 60, 80), MarkShape.Wave);
        return b;
    }

    private static PokeBuilder Wigglytuff()
    {
        var b = new PokeBuilder("Wigglytuff", 0.74f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var pink = Rgb(252, 198, 212);
        var white = Rgb(252, 246, 248);
        var black = Rgb(48, 40, 52);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.08f, 0.02f));
            b.Ell(leg, V(0.1f * s, 0.035f, 0.06f), V(0.055f, 0.035f, 0.08f), pink);
            int arm = b.Arm(s, V(0.18f * s, 0.3f, 0.04f));
            b.Limb(arm, V(0.18f * s, 0.3f, 0.04f), V(0.24f * s, 0.27f, 0.08f), 0.036f, 0.03f, pink);
        });
        // A tall soft body, white down its front, long ears and the curl of hair on its brow
        var c = V(0, 0.29f, 0);
        var r = V(0.2f, 0.26f, 0.18f);
        b.Ell(Body, c, r, pink);
        b.PaintEll(Body, V(0, 0.22f, 0.1f), V(0.15f, 0.18f, 0.1f), white);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(Body, s, V(0.09f * s, 0.48f, 0.0f));
            b.Spike(ear, V(0.09f * s, 0.46f, 0.0f), V(0.16f * s, 0.68f, -0.01f), 0.06f, pink, 0.5f);
            b.PaintEll(ear, V(0.13f * s, 0.58f, 0.025f), V(0.028f, 0.08f, 0.03f), black, V(0, 0, -18f * s));
        });
        Curl(b, Body, V(-0.04f, 0.53f, 0.04f), V(0.0f, 0.47f, 0.12f), V(-1f, 0.3f, 0), V(0, 0.95f, 0.3f), 0.05f, 1.0f, 0.026f, pink);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.07f * s, 0.38f), V(0.45f * s, 0.05f, 1f), 0.045f, sclera: true, pupil: Rgb(40, 150, 172)));
        Grin(b, Body, On(c, r, 0, 0.305f) + V(0, 0, -0.02f), V(0.04f, 0.02f, 0.03f), Rgb(200, 80, 100));
        return b;
    }

    // ------------------------------------------------------------------ Oddish line

    private static PokeBuilder Oddish()
    {
        var b = new PokeBuilder("Oddish", 0.5f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var blue = Rgb(74, 108, 178);
        var leaf = Rgb(88, 176, 86);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.08f, 0.0f));
            b.Ell(leg, V(0.065f * s, 0.045f, 0.01f), V(0.04f, 0.05f, 0.045f), blue);
        });
        // A round blue body that is its head, five long leaves sprouting from its crown
        var c = V(0, 0.2f, 0);
        var r = V(0.13f, 0.13f, 0.12f);
        b.Ell(Body, c, r, blue);
        int top = b.Part("leaves", Body, V(0, 0.32f, 0), PokeRole.Leaf);
        foreach (var (a, lean) in new[] { (0f, 0.5f), (72f, 0.9f), (144f, 1.1f), (216f, 1.1f), (288f, 0.9f) })
        {
            var d = V(MathF.Sin(a * Degree) * lean, 1f, -MathF.Cos(a * Degree) * lean * 0.7f);
            var root = V(0, 0.31f, -0.01f);
            Frond(b, top, root, root + Vector3.Normalize(d) * 0.26f, 0.06f, a == 0f ? PixelCanvas.Light1(leaf, 0.1f) : leaf, V(-d.X, 0.5f, -d.Z + 0.2f), 0.22f, Leaf, 0.01f);
        }
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.045f * s, 0.23f), V(0.4f * s, 0.05f, 1f), 0.02f, Rgb(214, 50, 56), pupil: Rgb(170, 30, 40)));
        Grin(b, Body, On(c, r, 0, 0.17f) + V(0, 0, -0.012f), V(0.022f, 0.016f, 0.02f), Rgb(214, 90, 110));
        return b;
    }

    private static PokeBuilder Gloom()
    {
        var b = new PokeBuilder("Gloom", 0.68f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var blue = Rgb(74, 108, 178);
        var bud = Rgb(152, 84, 66);
        var spot = Rgb(226, 176, 156);
        var leaf = Rgb(224, 108, 64);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.08f, 0.0f));
            b.Ell(leg, V(0.075f * s, 0.045f, 0.01f), V(0.045f, 0.05f, 0.05f), blue);
            int arm = b.Arm(s, V(0.13f * s, 0.22f, 0.03f));
            b.Limb(arm, V(0.13f * s, 0.22f, 0.03f), V(0.19f * s, 0.18f, 0.05f), 0.03f, 0.026f, blue);
        });
        var c = V(0, 0.22f, 0);
        var r = V(0.16f, 0.15f, 0.14f);
        b.Ell(Body, c, r, blue);
        // A great drooping bud of dark red petals spotted pale, and two broad leaves hanging out from under it
        int top = b.Part("flower", Body, V(0, 0.36f, 0), PokeRole.Leaf);
        b.Ell(top, V(0, 0.39f, -0.01f), V(0.1f, 0.06f, 0.1f), bud);
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f;
            var at = V(MathF.Sin(a) * 0.085f, 0.42f, -MathF.Cos(a) * 0.085f - 0.01f);
            b.Ell(top, at, V(0.075f, 0.065f, 0.075f), bud, blend: 0.02f);
            b.PaintEll(top, at + V(MathF.Sin(a) * 0.04f, 0.04f, -MathF.Cos(a) * 0.04f), V(0.022f, 0.02f, 0.022f), spot);
        }
        b.Ell(top, V(0, 0.48f, -0.01f), V(0.06f, 0.05f, 0.06f), bud, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            Frond(b, top, V(0.08f * s, 0.36f, 0.0f), V(0.26f * s, 0.42f, 0.02f), 0.06f, leaf, V(0, 1f, 0.3f), 0.25f, Leaf, 0.01f);
            Frond(b, top, V(0.22f * s, 0.42f, 0.02f), V(0.3f * s, 0.3f, 0.04f), 0.05f, leaf, V(s, 0.4f, 0.3f), 0.28f, Leaf, 0.01f);
        });
        // Drowsy eyes, a sour mouth with honey dribbling from it
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.06f * s, 0.26f), V(0.45f * s, 0.05f, 1f), 0.026f, closed: true));
        b.Mark(Body, On(c, r, 0, 0.19f), V(0, -0.2f, 1f), 0.05f, 0.016f, Rgb(120, 60, 120), MarkShape.Wave);
        b.Ell(Body, On(c, r, 0.025f, 0.165f) + V(0, -0.02f, -0.01f), V(0.014f, 0.035f, 0.014f), Rgb(246, 236, 210), blend: 0.008f);
        return b;
    }

    private static PokeBuilder Vileplume()
    {
        var b = new PokeBuilder("Vileplume", 0.88f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var blue = Rgb(74, 108, 178);
        var petal = Rgb(230, 112, 128);
        var core = Rgb(206, 158, 86);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.08f, 0.0f));
            b.Ell(leg, V(0.075f * s, 0.045f, 0.01f), V(0.045f, 0.05f, 0.05f), blue);
            int arm = b.Arm(s, V(0.13f * s, 0.22f, 0.03f));
            b.Limb(arm, V(0.13f * s, 0.22f, 0.03f), V(0.2f * s, 0.2f, 0.05f), 0.03f, 0.026f, blue);
        });
        var c = V(0, 0.22f, 0);
        var r = V(0.15f, 0.15f, 0.14f);
        b.Ell(Body, c, r, blue);
        // The huge flower it carries on its head: five broad petals spotted white round a ring of pollen
        int top = b.Part("flower", Body, V(0, 0.36f, 0), PokeRole.Leaf);
        b.Ell(top, V(0, 0.38f, 0), V(0.12f, 0.06f, 0.12f), petal);
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f + MathF.PI / 5f;
            var dir = V(MathF.Sin(a), 0, -MathF.Cos(a));
            var root = V(0, 0.42f, 0) + dir * 0.06f;
            Frond(b, top, root, root + dir * 0.36f - V(0, 0.05f, 0), 0.17f, petal, V(0, 1f, 0) + dir * 0.2f, 0.2f, Leaf, 0.02f);
            foreach (var (along, side) in new[] { (0.16f, 0.05f), (0.24f, -0.06f), (0.3f, 0.02f) })
                b.PaintEll(top, root + dir * along + Vector3.Cross(V(0, 1f, 0), dir) * side + V(0, 0.01f, 0), V(0.022f, 0.04f, 0.022f), White);
        }
        b.Torus(top, V(0, 0.45f, 0), 0.06f, 0.025f, core, mat: Leaf, blend: 0.012f);
        b.PaintEll(top, V(0, 0.47f, 0), V(0.04f, 0.02f, 0.04f), Rgb(110, 70, 40));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.055f * s, 0.25f), V(0.45f * s, 0.05f, 1f), 0.024f, closed: true));
        b.Mark(Body, On(c, r, 0, 0.19f), V(0, -0.2f, 1f), 0.03f, 0.012f, Rgb(120, 60, 120), MarkShape.Wave);
        return b;
    }
}
