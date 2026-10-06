using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Kalos's second batch in National Pokédex
// order: Inkay (686) to Volcanion (721), so every species of Kalos. Their forms are in PokemonModels.Regional.cs and
// PokemonModels.Megas.cs with the other forms. Helpers shared with the earlier batches are in PokemonModels.Sinnoh1.cs
// to PokemonModels.Sinnoh4.cs, PokemonModels.Kanto1.cs to PokemonModels.Kanto3.cs, PokemonModels.Johto1.cs,
// PokemonModels.Johto2.cs, PokemonModels.Hoenn1.cs to PokemonModels.Hoenn3.cs, PokemonModels.Unova1.cs to
// PokemonModels.Unova4.cs and PokemonModels.Kalos1.cs.
internal static partial class PokemonModels
{
    // ------------------------------------------------------------------ Inkay and Malamar

    /// <summary>Inkay: a little squid floating upside down, a pink hood spotted yellow and rimmed white over its round blue face, great eyes and a little pink beak, short blue tentacles below.</summary>
    private static PokeBuilder Inkay()
    {
        var b = new PokeBuilder("Inkay", 0.45f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(90, 130, 200);
        var deep = Rgb(60, 90, 160);
        var pink = Rgb(240, 170, 190);
        var yellow = Rgb(250, 220, 90);
        // Short tentacles hanging under the face
        for (int i = 0; i < 6; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 6f;
            var at = V(MathF.Sin(a) * 0.035f, 0.14f, MathF.Cos(a) * 0.03f);
            b.Spike(Body, at + V(0, 0.02f, 0), at + V(MathF.Sin(a) * 0.01f, -0.04f, MathF.Cos(a) * 0.01f), 0.018f, deep, 0.7f);
        }
        b.Ell(Body, V(0, 0.17f, 0), V(0.06f, 0.045f, 0.05f), deep);
        int head = b.Head(V(0, 0.19f, 0.01f));
        var c = V(0, 0.21f, 0.01f);
        var r = V(0.068f, 0.062f, 0.058f);
        b.Ell(head, c, r, blue);
        // The hood: a pink cap rimmed white, pointed at its two top corners, its white flaps hanging down at the sides
        var hc = c + V(0, 0.065f, -0.012f);
        b.Ell(head, hc, V(0.085f, 0.05f, 0.065f), pink, blend: 0.015f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, hc + V(0.06f * s, 0.02f, 0), hc + V(0.1f * s, 0.07f, -0.01f), 0.025f, White, 0.4f);
            Blade(b, head, hc + V(0.075f * s, -0.01f, 0), c + V(0.095f * s, -0.06f, -0.01f), 0.028f, White, V(s, 0, 0.3f), 0.25f);
        });
        b.PaintTorus(head, hc + V(0, -0.012f, 0), 0.084f, 0.012f, White, sz: 0.78f);
        foreach (var (x, y) in new[] { (-0.04f, 0.02f), (0f, 0.035f), (0.04f, 0.02f) })
            b.Mark(head, Out(hc, V(0.085f, 0.05f, 0.065f), default, V(x / 0.085f, (y + 0.01f) / 0.05f, 0.8f)), V(x * 6f, 0.6f, 1f), 0.012f, 0.012f, yellow);
        b.Ell(head, On(c, r, 0, c.Y - 0.03f), V(0.012f, 0.009f, 0.008f), pink, blend: 0.005f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.022f, Rgb(50, 40, 40), sclera: true);
        });
        return Lift(b);
    }

    /// <summary>
    /// Malamar and its Mega Evolution: a tall squid standing on its head, its body dark purple down the back and spotted
    /// yellow down the front, a pink collar of tentacles curling out at its neck, a crown of blue-violet tentacles above
    /// its small head with its yellow eyes and pink beak, standing on two long white fins. Mega Malamar's collar has
    /// grown into two great rings, green and pink, its body shines like a rainbow down the front, and it floats on one
    /// long point.
    /// </summary>
    private static PokeBuilder MalamarBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Malamar-Mega" : "Malamar", mega ? 1f : 0.9f, mega ? BodyPlan.Floating : BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Scales };
        if (mega) b.Hover();
        var purple = Rgb(70, 60, 120);
        var violet = Rgb(120, 110, 190);
        var teal = Rgb(110, 200, 210);
        var pink = Rgb(226, 60, 120);
        var yellow = Rgb(246, 220, 90);
        var fin = Rgb(236, 232, 246);
        var bc = V(0, 0.4f, 0);
        var br = V(0.075f, 0.17f, 0.06f);
        if (mega)
        {
            // One long point to float on, and two white fins rising from the hips
            b.Spike(Body, bc + V(0, -0.12f, 0), V(0, 0.02f, 0.01f), 0.04f, fin, 0.6f, Shell);
            PokeBuilder.Both(s => Blade(b, Body, bc + V(0.05f * s, -0.08f, -0.02f), bc + V(0.22f * s, 0.12f, -0.06f), 0.04f, fin, V(0, 0, 1f), 0.25f, Shell));
        }
        else
            PokeBuilder.Both(s =>
            {
                // Two long white fins to stand on, sweeping out and down to points
                int leg = b.Leg(s, bc + V(0.04f * s, -0.12f, 0));
                var path = Smooth(3, bc + V(0.04f * s, -0.12f, 0), bc + V(0.1f * s, -0.24f, 0.02f), V(0.13f * s, 0.06f, 0.03f), V(0.16f * s, 0.005f, 0.05f));
                Ribbon(b, leg, path, 0.04f, fin, V(0, 0, 1f), 0.012f);
            });
        b.Ell(Body, bc, br, purple);
        b.PaintEll(Body, bc + V(0, -0.01f, 0.04f), V(0.05f, 0.15f, 0.03f), mega ? teal : violet);
        if (mega)
        {
            b.PaintEll(Body, bc + V(0, 0.08f, 0.05f), V(0.05f, 0.05f, 0.03f), pink, soft: 0.02f);
            b.PaintEll(Body, bc + V(0, -0.02f, 0.05f), V(0.05f, 0.04f, 0.03f), yellow, soft: 0.02f);
        }
        foreach (var y in new[] { 0.06f, 0.0f, -0.06f, -0.11f })
            PokeBuilder.Both(s =>
            {
                var at = Out(bc, br, default, V(0.4f * s, y / br.Y, 0.9f));
                b.Mark(Body, at, Outward(bc, br, at), 0.009f, 0.014f, yellow);
            });
        // The collar of tentacles at the neck: pink curls, or Mega Malamar's two great rings
        var neck = bc + V(0, br.Y * 0.95f, 0.01f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, neck + V(0.04f * s, 0, 0));
            if (mega)
            {
                var ring = neck + V(0.2f * s, 0.02f, 0.03f);
                var loop = Spiral(ring, V(-s, 0, 0), V(0, 1f, 0), 0.13f, 0.13f, 0f, MathF.Tau * 0.82f, 20);
                b.Tube(arm, new[] { neck + V(0.04f * s, 0, 0.02f) }.Concat(loop).ToArray(), 0.022f, 0.012f, s < 0 ? Rgb(120, 220, 120) : Rgb(236, 120, 190), Glow, 0f);
            }
            else
            {
                var path = Smooth(3, neck + V(0.03f * s, 0, 0.02f), neck + V(0.12f * s, 0.05f, 0.04f), neck + V(0.18f * s, -0.02f, 0.06f), neck + V(0.15f * s, -0.09f, 0.08f));
                b.Tube(arm, path, 0.022f, 0.012f, pink, blend: 0f);
            }
        });
        b.Ell(Body, neck, V(0.06f, 0.025f, 0.05f), pink, blend: 0.015f);
        int head = b.Head(neck + V(0, 0.02f, 0));
        var c = neck + V(0, 0.07f, 0.01f);
        var r = V(0.05f, 0.045f, 0.045f);
        b.Ell(head, c, r, violet);
        b.Spike(head, On(c, r, 0, c.Y - 0.015f) - V(0, 0, 0.01f), On(c, r, 0, c.Y - 0.015f) + V(0, -0.02f, 0.025f), 0.012f, pink, 0.6f, Shell);
        // A crown of tentacles above the head, curling out at their tips
        int n = mega ? 9 : 7;
        for (int i = 0; i < n; i++)
        {
            float a = (i / (n - 1f) - 0.5f) * (mega ? 220f : 180f) * Degree;
            var d = V(MathF.Sin(a), MathF.Cos(a) * 0.9f + 0.3f, -0.3f);
            float len = (mega ? 0.16f : 0.12f) * (1f - 0.25f * MathF.Abs(MathF.Sin(a)));
            var root = c + V(MathF.Sin(a) * 0.03f, 0.03f, -0.01f);
            var path = Smooth(3, root, root + d * len * 0.6f, root + d * len + V(MathF.Sin(a) * 0.04f, -0.02f, 0));
            b.Tube(head, path, 0.016f, 0.006f, violet, blend: 0f);
            b.PaintEll(head, path[^1], V(0.016f, 0.016f, 0.016f), teal);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(250, 210, 60), glare: true);
        });
        return mega ? Lift(b) : b;
    }

    private static PokeBuilder Malamar() => MalamarBuild(false);

    // ------------------------------------------------------------------ Binacle and Barbaracle

    /// <summary>A hand of the Binacle line with a face: brown, five cream nails standing up from its top, round orange cheeks, and angry eyes.</summary>
    private static void BarnacleHand(PokeBuilder b, int bone, Vector3 c, float k, bool eyes, Color skin, Color nail)
    {
        var r = V(0.045f, 0.05f, 0.04f) * k;
        b.Ell(bone, c, r, skin);
        for (int i = 0; i < 5; i++)
        {
            float a = (i / 4f - 0.5f) * 2.2f;
            var root = c + V(MathF.Sin(a) * 0.034f, 0.03f, MathF.Cos(a) * 0.012f - 0.006f) * k;
            b.Spike(bone, root, root + V(MathF.Sin(a) * 0.035f, 0.065f, 0.004f) * k, 0.011f * k, nail, 0.5f, Shell);
        }
        if (!eyes) return;
        PokeBuilder.Both(s =>
        {
            b.PaintEll(bone, On(c, r, c.X + 0.022f * s * k, c.Y - 0.018f * k), V(0.013f, 0.013f, 0.01f) * k, Rgb(232, 120, 70));
            var at = On(c, r, c.X + 0.017f * s * k, c.Y + 0.006f * k);
            b.Eye(bone, at, Outward(c, r, at), 0.009f * k, Rgb(40, 36, 30), glare: true);
        });
        b.Mark(bone, On(c, r, c.X, c.Y - 0.02f * k), V(0, -0.2f, 1f), 0.008f * k, 0.004f * k, Rgb(60, 40, 30), MarkShape.Bar);
    }

    /// <summary>Binacle: two brown hands with faces, each on a neck banded orange and white, growing from one gray rock.</summary>
    private static PokeBuilder Binacle()
    {
        var b = new PokeBuilder("Binacle", 0.5f, BodyPlan.Biped, V(0, 0.08f, 0)) { Coat = Shell };
        var rock = Rgb(120, 126, 150);
        var brown = Rgb(130, 96, 70);
        var orange = Rgb(236, 130, 70);
        b.Ell(Body, V(0, 0.06f, 0), V(0.12f, 0.06f, 0.09f), rock);
        foreach (var at in new[] { V(-0.06f, 0.1f, 0.05f), V(0.04f, 0.11f, -0.04f), V(0.08f, 0.08f, 0.05f), V(-0.03f, 0.115f, -0.02f) })
            b.PaintEll(Body, at, V(0.018f, 0.01f, 0.018f), Rgb(222, 226, 236));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.045f * s, 0.1f, 0));
            var top = V(0.06f * s, 0.2f, 0.01f);
            b.Limb(arm, V(0.045f * s, 0.1f, 0), top, 0.024f, 0.022f, orange);
            b.PaintEll(arm, Vector3.Lerp(V(0.045f * s, 0.1f, 0), top, 0.55f), V(0.03f, 0.012f, 0.03f), White);
            BarnacleHand(b, arm, top + V(0.005f * s, 0.045f, 0.005f), 1f, true, brown, Rgb(240, 236, 190));
        });
        return b;
    }

    /// <summary>
    /// Barbaracle and its Mega Evolution: seven hands joined into one, its body two gray rocks one over the other, a
    /// hand with a face for its head, four arms and two legs banded orange and white. Mega Barbaracle stands as tall as
    /// a totem, black banded orange, eight arms spread round three stacked rocks, a red eye on every hand.
    /// </summary>
    private static PokeBuilder BarbaracleBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Barbaracle-Mega" : "Barbaracle", 1f, BodyPlan.Biped, V(0, 0.45f, 0)) { Coat = Shell };
        var rock = mega ? Rgb(140, 146, 170) : Rgb(130, 136, 160);
        var limb = mega ? Rgb(50, 46, 56) : Rgb(130, 96, 70);
        var orange = Rgb(236, 120, 60);
        var nail = mega ? Rgb(70, 66, 76) : Rgb(236, 232, 186);
        // Two legs banded orange and white, nails for toes
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.28f, 0));
            var foot = V((mega ? 0.04f : 0.1f) * s, 0.03f, 0.02f);
            b.Limb(leg, V(0.06f * s, 0.29f, 0), foot, 0.04f, 0.034f, limb);
            foreach (var t in new[] { 0.3f, 0.65f })
                b.PaintEll(leg, Vector3.Lerp(V(0.06f * s, 0.29f, 0), foot, t), V(0.045f, 0.018f, 0.045f), orange);
            b.Ell(leg, foot + V(0, -0.005f, 0.02f), V(0.04f, 0.025f, 0.05f), limb);
            Claws(b, leg, foot + V(0, 0, 0.06f), V(0.018f, 0, 0), V(0, -0.2f, 1f), 0.025f, 0.01f);
        });
        // The rocks of its body, stacked
        var rocks = mega
            ? new[] { (V(0, 0.34f, 0), V(0.09f, 0.05f, 0.08f)), (V(0, 0.5f, 0), V(0.14f, 0.07f, 0.1f)), (V(0, 0.68f, 0), V(0.11f, 0.06f, 0.09f)) }
            : new[] { (V(0, 0.32f, 0), V(0.1f, 0.05f, 0.08f)), (V(0, 0.48f, 0), V(0.14f, 0.08f, 0.1f)) };
        var spine = new[] { V(0, 0.28f, 0) }.Concat(rocks.Select(x => x.Item1)).Append(rocks[^1].Item1 + V(0, 0.08f, 0)).ToArray();
        b.Tube(Body, spine, 0.035f, 0.03f, limb, blend: 0f);
        foreach (var (at, r) in rocks)
        {
            b.Ell(Body, at, r, rock, mat: Shell, blend: 0.015f);
            b.PaintEll(Body, at + V(0, r.Y * 0.6f, 0), V(r.X * 0.8f, r.Y * 0.4f, r.Z * 0.8f), Rgb(226, 230, 240));
            b.PaintEll(Body, at + V(r.X * 0.4f, -r.Y * 0.2f, r.Z * 0.8f), V(0.012f, 0.012f, 0.012f), Rgb(80, 84, 100));
        }
        b.Spike(Body, rocks[^1].Item1 + V(0, 0, rocks[^1].Item2.Z * 0.8f), rocks[^1].Item1 + V(0, -0.02f, rocks[^1].Item2.Z + 0.05f), 0.025f, White, 0.6f, Shell);
        // Arms: four, or Mega Barbaracle's eight, each ending in a hand of nails
        var top = rocks[^1].Item1;
        var arms = mega
            ? new[] { (top + V(0.08f, 0.02f, 0), V(0.3f, 0.86f, 0.02f)), (top + V(0.1f, -0.03f, 0), V(0.34f, 0.62f, 0.04f)), (rocks[1].Item1 + V(0.12f, 0, 0), V(0.34f, 0.44f, 0.04f)), (rocks[1].Item1 + V(0.1f, -0.03f, 0), V(0.28f, 0.28f, 0.03f)) }
            : new[] { (top + V(0.1f, 0.03f, 0), V(0.26f, 0.7f, 0.03f)), (top + V(0.1f, -0.03f, 0), V(0.3f, 0.36f, 0.05f)) };
        for (int i = 0; i < arms.Length; i++)
        {
            var (from, to) = arms[i];
            PokeBuilder.Both(s =>
            {
                var a = V(from.X * s, from.Y, from.Z);
                var h = V(to.X * s, to.Y, to.Z);
                int arm = i == 0 ? b.Arm(s, a) : b.Part("arm" + (i + 1) + (s < 0 ? "L" : "R"), Body, a, PokeRole.Arm, s * (1f + i * 0.3f), s);
                var elbow = Vector3.Lerp(a, h, 0.5f) + V(0.02f * s, i % 2 == 0 ? -0.04f : 0.04f, 0);
                b.Tube(arm, Smooth(3, a, elbow, h), 0.026f, 0.022f, limb, blend: 0f);
                b.PaintEll(arm, elbow, V(0.03f, 0.03f, 0.03f), orange);
                var dir = Vector3.Normalize(h - elbow);
                var hc = h + dir * 0.03f;
                b.Ell(arm, hc, V(0.035f, 0.035f, 0.03f), limb);
                for (int k = -1; k <= 1; k++)
                {
                    var side = Vector3.Normalize(Vector3.Cross(dir, V(0, 0, 1f))) * k * 0.018f;
                    b.Spike(arm, hc + side + dir * 0.015f, hc + side * 1.3f + dir * 0.07f, 0.013f, nail, 0.55f, Shell);
                }
                if (mega) b.Mark(arm, hc + V(0, 0, 0.03f), V(0, 0, 1f), 0.012f, 0.008f, Rgb(220, 40, 40));
            });
        }
        // The head: a hand with a face, on a short neck
        int head = b.Head(top + V(0, 0.08f, 0));
        BarnacleHand(b, head, top + V(0, 0.13f, 0.01f), 1.1f, true, limb, nail);
        return b;
    }

    private static PokeBuilder Barbaracle() => BarbaracleBuild(false);

    // ------------------------------------------------------------------ Skrelp and Dragalge

    /// <summary>Skrelp: a little sea dragon hiding as a scrap of kelp, purple and ringed darker, a brown leaf over its head, red eyes, two leafy brown feet and pale blue fins round as drops of water.</summary>
    private static PokeBuilder Skrelp()
    {
        var b = new PokeBuilder("Skrelp", 0.5f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Scales }.Hover();
        var purple = Rgb(170, 130, 190);
        var ring = Rgb(130, 90, 150);
        var brown = Rgb(130, 96, 60);
        var drop = Rgb(170, 210, 240);
        var spine = Smooth(3, V(0, 0.28f, 0.0f), V(0, 0.22f, -0.05f), V(0, 0.14f, -0.03f), V(0, 0.08f, 0.02f), V(0, 0.07f, 0.07f), V(0, 0.1f, 0.09f));
        b.Tube(Body, spine, 0.03f, 0.016f, purple, blend: 0f);
        for (int i = 1; i < spine.Length - 1; i += 2)
            b.PaintTorus(Body, spine[i], 0.026f, 0.005f, ring, Euler(spine[i + 1] - spine[i - 1]));
        PokeBuilder.Both(s => Frond(b, Body, V(0.012f * s, 0.07f, 0.02f), V(0.05f * s, 0.02f, 0.05f), 0.025f, brown, V(0, 1f, 0.2f), 0.22f, Leaf));
        foreach (var (at, size) in new[] { (V(0, 0.2f, -0.07f), 0.03f), (V(0, 0.09f, -0.035f), 0.022f) })
            b.Ell(Body, at, V(size * 0.5f, size, size * 1.2f), drop, mat: Glow, blend: 0.012f);
        int head = b.Head(V(0, 0.28f, 0.0f));
        var c = V(0, 0.3f, 0.02f);
        var r = V(0.032f, 0.03f, 0.036f);
        b.Ell(head, c, r, purple);
        b.Limb(head, c + V(0, -0.005f, 0.02f), c + V(0, -0.012f, 0.07f), 0.016f, 0.01f, purple);
        // The leaf over its head, pointed back and up, and a drop of a fin above it
        Frond(b, head, c + V(0, 0.025f, 0.03f), c + V(0, 0.07f, -0.1f), 0.065f, brown, V(0, 1f, 0.3f), 0.16f, Leaf);
        Blade(b, head, c + V(0, 0.03f, 0.02f), c + V(0, 0.06f, 0.06f), 0.02f, brown, V(1f, 0, 0), 0.3f, Leaf);
        b.Ell(head, c + V(0, 0.08f, -0.1f), V(0.016f, 0.028f, 0.03f), drop, mat: Glow, blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(200, 40, 50), sclera: true);
        });
        return Lift(b);
    }

    /// <summary>
    /// Dragalge and its Mega Evolution: a sea dragon of poison, its long purple body ringed darker and hung all over with
    /// brown fins ragged like dried kelp, a crest of red fronds on its head and two stalks with purple bulbs, little
    /// leafy arms. Mega Dragalge trails a great cape of red fins from its head, darker at the edges.
    /// </summary>
    private static PokeBuilder DragalgeBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Dragalge-Mega" : "Dragalge", 1f, BodyPlan.Floating, V(0, 0.36f, 0)) { Coat = Scales }.Hover();
        var purple = Rgb(110, 80, 140);
        var ring = Rgb(170, 130, 190);
        var brown = Rgb(120, 82, 50);
        var red = Rgb(190, 60, 80);
        var spine = Smooth(3, V(0, 0.5f, 0.14f), V(0, 0.42f, 0.04f), V(0, 0.34f, -0.08f), V(0, 0.2f, -0.12f), V(0, 0.12f, -0.04f), V(0, 0.1f, 0.08f));
        b.Tube(Body, spine, 0.045f, 0.018f, purple, blend: 0f);
        for (int i = 1; i < spine.Length - 1; i += 2)
            b.PaintTorus(Body, spine[i], 0.04f - 0.0015f * i, 0.007f, ring, Euler(spine[i + 1] - spine[i - 1]));
        // Ragged brown fins hanging from the body, and leafy fins at the tail
        for (int i = 2; i < spine.Length - 1; i += 3)
            PokeBuilder.Both(s => Frond(b, Body, spine[i] + V(0.02f * s, 0, 0), spine[i] + V(0.1f * s, -0.04f, -0.02f), 0.025f, brown, V(0, 1f, 0.2f), 0.2f, Leaf));
        PokeBuilder.Both(s => Frond(b, Body, spine[^1], spine[^1] + V(0.06f * s, -0.01f, 0.06f), 0.03f, brown, V(0, 1f, 0), 0.2f, Leaf));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.03f * s, 0.42f, 0.06f));
            var hand = V(0.09f * s, 0.34f, 0.12f);
            b.Limb(arm, V(0.03f * s, 0.42f, 0.06f), hand, 0.014f, 0.011f, purple);
            foreach (var t in new[] { -1f, 0f, 1f })
                Frond(b, arm, hand, hand + V((0.03f + t * 0.01f) * s, -0.04f, 0.02f + t * 0.02f), 0.012f, brown, V(0, 0, 1f), 0.25f, Leaf);
        });
        int head = b.Head(V(0, 0.5f, 0.12f));
        var c = V(0, 0.55f, 0.15f);
        var r = V(0.045f, 0.04f, 0.05f);
        b.Ell(head, c, r, purple);
        b.Limb(head, c + V(0, -0.01f, 0.03f), c + V(0, -0.025f, 0.09f), 0.02f, 0.012f, purple);
        // A crest of red fronds, and two stalks topped with purple bulbs
        foreach (var (x, len) in new[] { (-0.02f, 0.09f), (0f, 0.12f), (0.02f, 0.09f) })
            Frond(b, head, c + V(x, 0.03f, -0.01f), c + V(x * 3f, 0.03f + len, -0.06f), 0.025f, red, V(0, 0.3f, 1f), 0.2f, Leaf);
        PokeBuilder.Both(s => Antenna(b, head, c + V(0.025f * s, 0.03f, 0.0f), c + V(0.05f * s, 0.09f, 0.02f), c + V(0.07f * s, 0.12f, 0.04f), 0.006f, purple, Rgb(140, 70, 170), 0.016f, Glow));
        if (mega)
        {
            // The cape: two great sheets of red fin streaming back from the head, dark along their edges
            PokeBuilder.Both(s =>
            {
                int cape = b.Part(s < 0 ? "capeL" : "capeR", head, c, PokeRole.Tail, 0.3f, s);
                var path = Smooth(3, c + V(0.02f * s, 0.03f, -0.03f), c + V(0.14f * s, 0.12f, -0.12f), c + V(0.3f * s, 0.06f, -0.22f), c + V(0.42f * s, -0.12f, -0.26f));
                Ribbon(b, cape, path, 0.12f, red, V(0, 0.4f, 1f), 0.02f);
                b.PaintEll(cape, path[^1], V(0.1f, 0.12f, 0.1f), Rgb(100, 40, 110));
            });
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.024f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(170, 60, 90), glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Dragalge() => DragalgeBuild(false);

    // ------------------------------------------------------------------ Clauncher and Clawitzer

    /// <summary>
    /// Clauncher and Clawitzer: shrimps with one claw as big as their bodies, held before their faces to shoot water,
    /// their tails in bands. Clauncher is pale blue with a blue claw banded dark, two long yellow feelers; Clawitzer's
    /// claw is a great dark blue cannon, its tail set with yellow and black spikes.
    /// </summary>
    private static PokeBuilder ClawShrimpBuild(bool clawitzer)
    {
        float k = clawitzer ? 1.35f : 1f;
        var b = new PokeBuilder(clawitzer ? "Clawitzer" : "Clauncher", clawitzer ? 0.95f : 0.55f, BodyPlan.Quadruped, V(0, 0.1f * k, 0)) { Coat = Shell };
        var pale = clawitzer ? Rgb(110, 180, 230) : Rgb(140, 210, 240);
        var claw = clawitzer ? Rgb(40, 80, 170) : Rgb(90, 170, 230);
        var band = Rgb(36, 40, 60);
        var yellow = Rgb(250, 210, 70);
        var bc = V(0, 0.1f, 0) * k;
        var br = V(0.06f, 0.055f, 0.07f) * k;
        // Little legs under the body
        foreach (var (z, front) in new[] { (0.03f, true), (-0.04f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.04f * s, 0.07f, z) * k, front);
                b.Limb(leg, V(0.04f * s, 0.07f, z) * k, V(0.07f * s, 0.005f, z + 0.01f) * k, 0.011f * k, 0.008f * k, pale);
            });
        b.Ell(Body, bc, br, pale);
        // A tail of shrinking bands curling up behind, ending in a fan
        var tail = b.Tail(bc + V(0, 0, -br.Z * 0.8f));
        var tp = new[] { bc + V(0, 0.01f, -0.08f) * k, bc + V(0, 0.03f, -0.13f) * k, bc + V(0, 0.06f, -0.17f) * k, bc + V(0, 0.1f, -0.19f) * k };
        for (int i = 0; i < tp.Length; i++)
        {
            float rr = (0.045f - i * 0.008f) * k;
            b.Ell(tail, tp[i], V(rr, rr * 0.85f, rr * 0.8f), pale, blend: 0.006f);
            b.PaintTorus(tail, tp[i], rr * 0.95f, 0.004f * k, band, Euler(i + 1 < tp.Length ? tp[i + 1] - tp[i] : tp[i] - tp[i - 1]));
            if (clawitzer)
                PokeBuilder.Both(s => b.Spike(tail, tp[i] + V(rr * 0.6f * s, rr * 0.5f, 0), tp[i] + V(rr * 1.4f * s, rr * 1.3f, -0.01f), 0.01f * k, i % 2 == 0 ? yellow : band, 0.5f, Shell));
        }
        foreach (float t in new[] { -1f, 0f, 1f })
            Frond(b, tail, tp[^1], tp[^1] + V(t * 0.035f, 0.05f, -0.01f) * k, 0.018f * k, pale, V(0, 0, 1f), 0.25f, Shell);
        // The great claw before the face
        int arm = b.Arm(1f, bc + V(0.035f, 0.03f, 0.04f) * k);
        var wrist = bc + V(0.05f, 0.04f, 0.07f) * k;
        b.Limb(arm, bc + V(0.035f, 0.03f, 0.04f) * k, wrist, 0.018f * k, 0.022f * k, claw);
        b.Torus(arm, wrist + V(0, 0, 0.01f) * k, 0.022f * k, 0.007f * k, band, V(90f, 0, 0), blend: 0.004f);
        var cc = bc + V(0.055f, 0.07f, 0.14f) * k;
        var cr = clawitzer ? V(0.085f, 0.075f, 0.1f) * k : V(0.075f, 0.06f, 0.085f) * k;
        b.Ell(arm, cc, cr, claw, mat: Shell);
        if (clawitzer)
        {
            // The cannon's mouth, ringed yellow, and its hooked tip below
            b.Cut(arm, cc + V(0, 0.01f, cr.Z * 0.95f), V(0.03f, 0.03f, 0.03f) * k);
            b.PaintTorus(arm, cc + V(0, 0.01f, cr.Z * 0.9f), 0.036f * k, 0.008f * k, yellow, V(90f, 0, 0));
            b.Spike(arm, cc + V(0, -cr.Y * 0.5f, cr.Z * 0.6f), cc + V(0, -cr.Y * 1.1f, cr.Z * 1.2f), 0.03f * k, claw, 0.6f, Shell);
            b.PaintEll(arm, cc + V(0, cr.Y * 0.6f, 0), V(cr.X * 0.9f, cr.Y * 0.3f, cr.Z * 0.9f), Rgb(60, 110, 200));
        }
        else
        {
            // The pincer: a lower jaw under the great upper one, a gap between
            b.Ell(arm, cc + V(0, -cr.Y * 0.9f, cr.Z * 0.2f), V(cr.X * 0.7f, cr.Y * 0.35f, cr.Z * 0.75f), claw, mat: Shell, blend: 0.008f);
            b.Cut(arm, cc + V(0, -cr.Y * 0.55f, cr.Z * 0.85f), V(cr.X * 0.8f, cr.Y * 0.18f, cr.Z * 0.5f));
            b.PaintEll(arm, cc + V(0, 0, -cr.Z * 0.55f), V(cr.X * 1.05f, cr.Y * 1.05f, cr.Z * 0.18f), band);
        }
        // The face under the claw: two yellow eyes and long feelers
        float hx = clawitzer ? -0.045f : -0.025f;
        int head = b.Head(bc + V(hx + 0.005f, 0.03f, 0.05f) * k);
        var c = bc + V(hx, 0.035f, 0.06f) * k;
        var r = V(0.04f, 0.035f, 0.035f) * k;
        b.Ell(head, c, r, pale);
        PokeBuilder.Both(s =>
        {
            Antenna(b, head, c + V(0.012f * s, 0.03f, 0.0f) * k, c + V(0.03f * s, 0.12f, -0.05f) * k, c + V(0.05f * s, 0.17f, -0.14f) * k, 0.004f * k, yellow, yellow, 0.005f * k);
            var at = On(c, r, c.X + 0.018f * s * k, c.Y + 0.006f * k);
            b.Eye(head, at, Outward(c, r, at), 0.01f * k, Rgb(250, 200, 50), glare: clawitzer);
        });
        return b;
    }

    private static PokeBuilder Clauncher() => ClawShrimpBuild(false);

    private static PokeBuilder Clawitzer() => ClawShrimpBuild(true);

    // ------------------------------------------------------------------ Helioptile and Heliolisk

    /// <summary>Helioptile: a little yellow lizard on its hind legs, a black hood over its head with two long flaps hanging at its sides, great blue eyes, and a tail tipped black and orange.</summary>
    private static PokeBuilder Helioptile()
    {
        var b = new PokeBuilder("Helioptile", 0.5f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Scales };
        var yellow = Rgb(250, 226, 110);
        var black = Rgb(56, 56, 64);
        var orange = Rgb(236, 130, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.08f, -0.01f));
            b.Limb(leg, V(0.03f * s, 0.08f, -0.01f), V(0.04f * s, 0.015f, 0.01f), 0.016f, 0.012f, yellow);
            PadDigits(b, leg, V(0.04f * s, 0.008f, 0.015f), V(0, 0, 1f), V(0.5f, 0, 0), 0.02f, 0.004f, black);
            int arm = b.Arm(s, V(0.035f * s, 0.15f, 0.02f));
            b.Limb(arm, V(0.035f * s, 0.15f, 0.02f), V(0.06f * s, 0.11f, 0.05f), 0.009f, 0.008f, yellow);
            PadDigits(b, arm, V(0.06f * s, 0.11f, 0.05f), V(0.4f * s, -0.5f, 0.6f), V(0.3f, 0.3f, 0), 0.015f, 0.003f, black);
        });
        b.Ell(Body, V(0, 0.12f, 0), V(0.04f, 0.055f, 0.04f), yellow, V(-10f, 0, 0));
        int tail = b.Tail(V(0, 0.08f, -0.03f));
        var tp = Smooth(3, V(0, 0.08f, -0.03f), V(0, 0.04f, -0.1f), V(0, 0.02f, -0.18f), V(0, 0.03f, -0.24f));
        b.Tube(tail, tp, 0.02f, 0.009f, yellow, blend: 0f);
        b.PaintEll(tail, tp[^2], V(0.016f, 0.016f, 0.012f), orange);
        b.PaintEll(tail, tp[^1], V(0.016f, 0.016f, 0.022f), black);
        int head = b.Head(V(0, 0.18f, 0.02f));
        var c = V(0, 0.22f, 0.03f);
        var r = V(0.045f, 0.04f, 0.05f);
        b.Ell(head, c, r, yellow);
        // The black hood over the head, its two long flaps hanging down at the sides
        b.Ell(head, c + V(0, 0.025f, -0.012f), V(0.05f, 0.03f, 0.05f), black, blend: 0.01f);
        PokeBuilder.Both(s => Frond(b, head, c + V(0.04f * s, 0.02f, -0.02f), c + V(0.075f * s, -0.065f, -0.03f), 0.02f, black, V(s, 0.2f, 0.4f), 0.24f));
        PokeBuilder.Both(s => b.PaintEll(head, c + V(0.075f * s, -0.06f, -0.03f), V(0.016f, 0.016f, 0.016f), yellow));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y - 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(80, 160, 230), sclera: true);
        });
        return b;
    }

    /// <summary>Heliolisk: a slender yellow lizard standing tall, its head black with a frill of black and gold round its neck like a sun, orange triangles down its body, black forearms and shins, a long tail banded orange and black.</summary>
    private static PokeBuilder Heliolisk()
    {
        var b = new PokeBuilder("Heliolisk", 0.9f, BodyPlan.Biped, V(0, 0.32f, 0)) { Coat = Scales };
        var yellow = Rgb(250, 220, 90);
        var black = Rgb(50, 50, 58);
        var orange = Rgb(236, 120, 50);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.24f, -0.01f));
            var knee = V(0.06f * s, 0.13f, 0.03f);
            b.Limb(leg, V(0.04f * s, 0.25f, -0.01f), knee, 0.024f, 0.017f, yellow);
            b.Limb(leg, knee, V(0.06f * s, 0.02f, 0.0f), 0.017f, 0.013f, black);
            PadDigits(b, leg, V(0.06f * s, 0.012f, 0.01f), V(0, 0, 1f), V(0.5f, 0, 0), 0.03f, 0.005f, black);
            var shoulder = V(0.045f * s, 0.4f, 0.01f);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.05f * s, -0.07f, 0.02f);
            b.Limb(arm, shoulder, elbow, 0.014f, 0.012f, yellow);
            b.Limb(arm, elbow, elbow + V(0.03f * s, -0.07f, 0.03f), 0.012f, 0.011f, black);
            PadDigits(b, arm, elbow + V(0.03f * s, -0.07f, 0.03f), V(0.2f * s, -1f, 0.3f), V(0.3f, 0, 0.3f), 0.022f, 0.004f, black);
        });
        var bc = V(0, 0.32f, 0);
        var br = V(0.045f, 0.1f, 0.04f);
        b.Ell(Body, bc, br, yellow);
        foreach (var y in new[] { 0.05f, -0.01f, -0.07f })
            b.Mark(Body, Out(bc, br, default, V(0, y / br.Y, 1f)), V(0, 0, 1f), 0.016f, 0.012f, orange, MarkShape.Triangle);
        int tail = b.Tail(bc + V(0, -0.07f, -0.03f));
        var tp = Smooth(3, bc + V(0, -0.07f, -0.03f), V(0, 0.16f, -0.12f), V(0, 0.06f, -0.24f), V(0, 0.04f, -0.36f));
        b.Tube(tail, tp, 0.024f, 0.01f, yellow, blend: 0f);
        for (int i = 3; i < tp.Length; i += 2)
            b.PaintEll(tail, tp[i], V(0.02f, 0.02f, 0.012f), i % 4 == 1 ? black : orange);
        // The frill round the neck: black, set with gold points like the rays of a sun
        var neck = bc + V(0, 0.1f, 0.0f);
        for (int i = 0; i < 9; i++)
        {
            float a = (i / 8f - 0.5f) * 240f * Degree;
            var d = V(MathF.Sin(a), -MathF.Cos(a) * 0.4f + 0.1f, -0.25f);
            Blade(b, Body, neck + V(0, 0.01f, -0.01f), neck + Vector3.Normalize(d) * 0.08f, 0.026f, black, V(0, 0.3f, 1f), 0.22f);
            b.PaintEll(Body, neck + Vector3.Normalize(d) * 0.06f, V(0.012f, 0.012f, 0.012f), Rgb(250, 200, 60));
        }
        int head = b.Head(neck + V(0, 0.02f, 0.01f));
        var c = neck + V(0, 0.06f, 0.03f);
        var r = V(0.04f, 0.038f, 0.055f);
        b.Ell(head, c, r, black);
        b.PaintEll(head, c + V(0, -0.02f, 0.03f), V(0.03f, 0.015f, 0.03f), yellow);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.022f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(80, 160, 230), sclera: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Tyrunt and Tyrantrum

    /// <summary>Tyrunt: a brown tyrant of a dinosaur's chick, its head a great jaw, gray below, orange horns along its crown, a white beard of spikes behind its jaw, stout legs and a thick tail.</summary>
    private static PokeBuilder Tyrunt()
    {
        var b = new PokeBuilder("Tyrunt", 0.6f, BodyPlan.Biped, V(0, 0.15f, -0.02f)) { Coat = Scales };
        var brown = Rgb(130, 100, 80);
        var gray = Rgb(176, 170, 170);
        var orange = Rgb(236, 140, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.12f, -0.04f));
            b.Limb(leg, V(0.05f * s, 0.13f, -0.04f), V(0.06f * s, 0.04f, -0.01f), 0.035f, 0.025f, brown);
            b.Ell(leg, V(0.062f * s, 0.02f, 0.01f), V(0.03f, 0.02f, 0.04f), brown);
            Claws(b, leg, V(0.062f * s, 0.015f, 0.045f), V(0.013f, 0, 0), V(0, -0.2f, 1f), 0.015f, 0.007f);
            int arm = b.Arm(s, V(0.04f * s, 0.16f, 0.04f));
            b.Limb(arm, V(0.04f * s, 0.16f, 0.04f), V(0.05f * s, 0.13f, 0.07f), 0.009f, 0.008f, brown);
        });
        b.Ell(Body, V(0, 0.15f, -0.02f), V(0.065f, 0.06f, 0.08f), brown, V(-15f, 0, 0));
        b.PaintEll(Body, V(0, 0.13f, 0.04f), V(0.05f, 0.04f, 0.03f), gray);
        int tail = b.Tail(V(0, 0.15f, -0.08f));
        b.Tube(tail, Smooth(3, V(0, 0.15f, -0.08f), V(0, 0.13f, -0.16f), V(0, 0.1f, -0.24f)), 0.04f, 0.012f, brown, blend: 0f);
        int head = b.Head(V(0, 0.2f, 0.04f));
        var c = V(0, 0.25f, 0.08f);
        var r = V(0.07f, 0.055f, 0.085f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.045f, 0.02f), V(0.06f, 0.03f, 0.075f), gray, blend: 0.015f);
        Grin(b, head, c + V(0, -0.025f, 0.075f), V(0.045f, 0.012f, 0.02f), Rgb(170, 70, 80));
        foreach (float x in new[] { -0.02f, 0.02f })
            b.Spike(head, c + V(x, -0.015f, 0.08f), c + V(x, -0.03f, 0.085f), 0.005f, White, mat: Shell, blend: 0.003f);
        // Orange horns along the crown, and the white beard of spikes behind the jaw
        foreach (var (z, len) in new[] { (0.03f, 0.03f), (-0.01f, 0.04f), (-0.05f, 0.035f) })
            b.Spike(head, c + V(0, 0.045f, z), c + V(0, 0.045f + len, z - 0.02f), 0.013f, orange, 0.6f, Shell);
        PokeBuilder.Both(s =>
        {
            foreach (var (y, len) in new[] { (-0.03f, 0.05f), (-0.005f, 0.045f), (0.02f, 0.035f) })
                b.Spike(head, c + V(0.05f * s, y, -0.05f), c + V((0.05f + len * 0.6f) * s, y + 0.01f, -0.05f - len), 0.014f, White, 0.5f);
            var at = On(c, r, 0.035f * s, c.Y + 0.02f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(30, 28, 30), sclera: true);
        });
        return b;
    }

    /// <summary>Tyrantrum: a great red-brown tyrant of a dinosaur, its hide set with plates, a white mane of feathers from its head down its neck, an orange crown of spikes, a jaw full of teeth, small arms and mighty legs.</summary>
    private static PokeBuilder Tyrantrum()
    {
        var b = new PokeBuilder("Tyrantrum", 1f, BodyPlan.Biped, V(0, 0.38f, -0.04f)) { Coat = Scales };
        var red = Rgb(170, 70, 60);
        var plate = Rgb(206, 110, 70);
        var cream = Rgb(236, 226, 214);
        var orange = Rgb(236, 140, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.32f, -0.08f));
            var knee = V(0.13f * s, 0.2f, 0.0f);
            b.Limb(leg, V(0.09f * s, 0.33f, -0.08f), knee, 0.07f, 0.05f, red);
            b.Limb(leg, knee, V(0.12f * s, 0.05f, -0.04f), 0.04f, 0.03f, red);
            b.Ell(leg, V(0.12f * s, 0.03f, 0.0f), V(0.045f, 0.03f, 0.07f), red);
            Claws(b, leg, V(0.12f * s, 0.02f, 0.07f), V(0.02f, 0, 0), V(0, -0.2f, 1f), 0.03f, 0.011f);
            int arm = b.Arm(s, V(0.08f * s, 0.46f, 0.08f));
            b.Limb(arm, V(0.08f * s, 0.46f, 0.08f), V(0.1f * s, 0.4f, 0.14f), 0.018f, 0.014f, red);
            Claws(b, arm, V(0.1f * s, 0.39f, 0.15f), V(0.008f, 0, 0), V(0, -0.5f, 1f), 0.018f, 0.006f);
        });
        var bc = V(0, 0.38f, -0.04f);
        var br = V(0.12f, 0.12f, 0.16f);
        b.Ell(Body, bc, br, red, V(-25f, 0, 0));
        b.PaintEll(Body, bc + V(0, -0.02f, 0.12f), V(0.08f, 0.1f, 0.05f), cream);
        foreach (var (x, y, z) in new[] { (0.09f, 0.42f, -0.1f), (-0.09f, 0.4f, -0.04f), (0.1f, 0.34f, 0.02f), (-0.08f, 0.32f, -0.14f), (0.06f, 0.46f, -0.16f) })
            b.PaintEll(Body, V(x, y, z), V(0.035f, 0.025f, 0.035f), plate);
        int tail = b.Tail(bc + V(0, 0, -0.14f));
        b.Tube(tail, Smooth(3, bc + V(0, 0, -0.14f), bc + V(0, -0.08f, -0.3f), bc + V(0, -0.16f, -0.46f), bc + V(0, -0.2f, -0.56f)), 0.07f, 0.015f, red, blend: 0f);
        // The neck under a white mane of feathers, and the head
        var neck = bc + V(0, 0.14f, 0.08f);
        b.Limb(Body, bc + V(0, 0.06f, 0.06f), neck + V(0, 0.04f, 0.04f), 0.07f, 0.06f, red);
        for (int i = 0; i < 4; i++)
            PokeBuilder.Both(s =>
            {
                var root = neck + V(0.05f * s, 0.08f - i * 0.04f, -0.01f - i * 0.025f);
                b.Spike(Body, root, root + V((0.12f - i * 0.015f) * s, -0.02f - i * 0.01f, -0.05f), 0.03f, White, 0.35f);
            });
        int head = b.Head(neck + V(0, 0.06f, 0.06f));
        var c = neck + V(0, 0.1f, 0.12f);
        var r = V(0.08f, 0.06f, 0.11f);
        b.Ell(head, c, r, red);
        b.Ell(head, c + V(0, -0.05f, 0.02f), V(0.07f, 0.03f, 0.1f), cream, blend: 0.015f);
        Grin(b, head, c + V(0, -0.025f, 0.1f), V(0.055f, 0.016f, 0.025f), Rgb(150, 50, 60));
        foreach (float x in new[] { -0.03f, -0.01f, 0.01f, 0.03f })
            b.Spike(head, c + V(x, -0.012f, 0.1f), c + V(x, -0.03f, 0.105f), 0.005f, White, mat: Shell, blend: 0.003f);
        // An orange crown of spikes from its brow back
        foreach (var (x, z, len) in new[] { (0f, 0.04f, 0.04f), (-0.03f, 0.0f, 0.05f), (0.03f, 0.0f, 0.05f), (0f, -0.04f, 0.06f), (-0.04f, -0.06f, 0.05f), (0.04f, -0.06f, 0.05f) })
            b.Spike(head, c + V(x, 0.045f, z), c + V(x * 1.4f, 0.05f + len, z - 0.03f), 0.015f, orange, 0.55f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.042f * s, c.Y + 0.018f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(250, 200, 60), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Amaura and Aurorus

    /// <summary>Amaura: a little pale blue dinosaur with a long neck, two sails like a moth's wings on its head that run from yellow to pink, a blue diamond on each side and great blue eyes.</summary>
    private static PokeBuilder Amaura()
    {
        var b = new PokeBuilder("Amaura", 0.6f, BodyPlan.Quadruped, V(0, 0.12f, -0.02f)) { Coat = Scales };
        var blue = Rgb(150, 210, 240);
        var crystal = Rgb(120, 190, 240);
        BeastLegs(b, 0.04f, 0.1f, 0.05f, -0.07f, 0.024f, blue, blue);
        var bc = V(0, 0.12f, -0.02f);
        var br = V(0.06f, 0.055f, 0.085f);
        b.Ell(Body, bc, br, blue);
        PokeBuilder.Both(s => Gem(b, Body, Out(bc, br, default, V(s, 0.1f, 0)) + V(0.004f * s, 0, 0), V(s, 0.1f, 0), 0.016f, crystal));
        int tail = b.Tail(bc + V(0, 0, -br.Z * 0.85f));
        b.Tube(tail, Smooth(3, bc + V(0, 0, -br.Z * 0.85f), bc + V(0, -0.02f, -0.14f), bc + V(0, -0.04f, -0.18f)), 0.025f, 0.008f, blue, blend: 0f);
        var neckPath = Smooth(3, bc + V(0, 0.02f, 0.06f), bc + V(0, 0.12f, 0.1f), bc + V(0, 0.22f, 0.1f));
        b.Tube(Body, neckPath, 0.026f, 0.02f, blue, blend: 0f);
        int head = b.Head(neckPath[^1]);
        var c = neckPath[^1] + V(0, 0.03f, 0.02f);
        var r = V(0.045f, 0.04f, 0.05f);
        b.Ell(head, c, r, blue);
        // Two sails on its head, like a moth's wings, yellow at the root and pink at the edge
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.025f * s, 0.03f, -0.01f);
            var tip = root + V(0.07f * s, 0.09f, -0.02f);
            Frond(b, head, root, tip, 0.04f, Rgb(250, 236, 160), V(0.3f * s, -0.2f, 1f), 0.15f);
            b.PaintEll(head, tip, V(0.04f, 0.04f, 0.04f), Rgb(246, 170, 170), soft: 0.015f);
        });
        b.Ell(head, c + V(0, 0.035f, 0.0f), V(0.012f, 0.012f, 0.012f), crystal, mat: Glow, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.024f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.015f, Rgb(70, 120, 220), sclera: true);
        });
        return b;
    }

    /// <summary>Aurorus: a great blue dinosaur with a long neck and a tail coiled up behind, a sail like the aurora behind its head and down its neck, yellow running to pink, diamonds along its neck and sides, a pale belly.</summary>
    private static PokeBuilder Aurorus()
    {
        var b = new PokeBuilder("Aurorus", 1f, BodyPlan.Quadruped, V(0, 0.32f, -0.04f)) { Coat = Scales };
        var blue = Rgb(110, 150, 220);
        var pale = Rgb(190, 214, 240);
        var crystal = Rgb(150, 220, 250);
        BeastLegs(b, 0.08f, 0.28f, 0.1f, -0.16f, 0.05f, blue, blue);
        var bc = V(0, 0.32f, -0.04f);
        var br = V(0.12f, 0.11f, 0.18f);
        b.Ell(Body, bc, br, blue);
        b.PaintEll(Body, bc + V(0, -0.07f, 0.02f), V(0.1f, 0.05f, 0.16f), pale);
        PokeBuilder.Both(s =>
        {
            foreach (var z in new[] { -0.08f, 0.04f })
            {
                var n = V(s, 0.1f, z / br.Z);
                Gem(b, Body, Out(bc, br, default, n), n, 0.02f, crystal);
            }
        });
        // A tail that coils up behind it
        int tail = b.Tail(bc + V(0, 0.02f, -0.17f));
        Curl(b, tail, bc + V(0, 0.02f, -0.17f), bc + V(0, 0.08f, -0.3f), V(0, -1f, 0), V(0, 0, -1f), 0.07f, 1.1f, 0.03f, blue);
        // The long neck rising forward, the sail behind it
        var neckPath = Smooth(3, bc + V(0, 0.06f, 0.14f), bc + V(0, 0.24f, 0.2f), bc + V(0, 0.44f, 0.22f), bc + V(0, 0.56f, 0.24f));
        b.Tube(Body, neckPath, 0.065f, 0.048f, blue, blend: 0f);
        for (int i = 1; i < neckPath.Length; i += 2)
            PokeBuilder.Both(s => Gem(b, Body, neckPath[i] + V(0.05f * s, 0, 0), V(s, 0, 0.3f), 0.014f, crystal));
        int head = b.Head(neckPath[^1]);
        var c = neckPath[^1] + V(0, 0.04f, 0.03f);
        var r = V(0.055f, 0.045f, 0.065f);
        b.Ell(head, c, r, blue);
        // The sail: a curtain of fins down the back of the neck, yellow at the root and pink at the edge, widest behind the head
        int sailBone = b.Part("sail", head, c, PokeRole.Ear, 0.2f);
        var along = neckPath.Reverse().Take(8).ToArray();
        for (int i = 0; i < along.Length; i++)
        {
            float k = 1f - i * 0.09f;
            var root = along[i] + V(0, 0.02f, -0.05f);
            var tip = root + V(0, 0.06f * k, -0.2f * k);
            Frond(b, sailBone, root, tip, 0.085f * k, Rgb(250, 240, 170), V(1f, 0, 0), 0.12f, Glow, 0.02f);
            b.PaintEll(sailBone, tip, V(0.04f, 0.07f, 0.07f) * k, Rgb(246, 170, 190), soft: 0.02f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(60, 110, 210), sclera: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Sylveon

    /// <summary>
    /// Sylveon: a slender cream fox of the Eevee line, its ears, legs and tail pink, a pink bow at its neck and another
    /// at its ear, each with a blue heart and long ribbon feelers trailing, white running to blue and pink at their ends.
    /// </summary>
    private static PokeBuilder Sylveon()
    {
        var b = new PokeBuilder("Sylveon", 0.75f, BodyPlan.Quadruped, V(0, 0.2f, -0.02f)) { Coat = Fur };
        var cream = Rgb(250, 244, 236);
        var pink = Rgb(246, 150, 186);
        var blue = Rgb(110, 190, 236);
        BeastLegs(b, 0.04f, 0.17f, 0.07f, -0.09f, 0.025f, cream, pink, pink);
        var bc = V(0, 0.2f, -0.02f);
        b.Ell(Body, bc, V(0.06f, 0.06f, 0.1f), cream);
        int tail = b.Tail(bc + V(0, 0.02f, -0.09f));
        b.Tube(tail, Smooth(3, bc + V(0, 0.02f, -0.09f), bc + V(0, 0.08f, -0.16f), bc + V(0, 0.15f, -0.17f), bc + V(0, 0.2f, -0.13f)), 0.022f, 0.008f, pink, blend: 0f);
        int head = b.Head(bc + V(0, 0.08f, 0.08f));
        var c = bc + V(0, 0.15f, 0.11f);
        var r = V(0.06f, 0.055f, 0.055f);
        b.Ell(head, bc + V(0, 0.08f, 0.08f), V(0.035f, 0.04f, 0.035f), cream, blend: 0.02f);
        b.Ell(head, c, r, cream);
        b.Ell(head, c + V(0, -0.02f, 0.045f), V(0.025f, 0.02f, 0.02f), cream);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.035f * s, 0.035f, -0.01f));
            CatEar(b, ear, c + V(0.035f * s, 0.035f, -0.01f), c + V(0.09f * s, 0.11f, -0.02f), 0.032f, pink, blue);
            var at = On(c, r, 0.027f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.016f, Rgb(70, 170, 230), sclera: true);
        });
        // A bow at the neck and another at the left ear, with ribbon feelers trailing from them
        void Bow(int bone, Vector3 at, float k, Vector3[][] feelers)
        {
            PokeBuilder.Both(s => b.Ell(bone, at + V(0.018f * s * k, 0, 0), V(0.016f, 0.012f, 0.008f) * k, pink, V(0, 0, 20f * s), blend: 0.006f));
            b.Ell(bone, at, V(0.009f, 0.009f, 0.008f) * k, blue, blend: 0.004f);
            foreach (var path in feelers)
            {
                Ribbon(b, bone, path, 0.02f * k, cream, V(0, 0.3f, 1f), 0.0045f);
                b.PaintEll(bone, path[^1], V(0.018f, 0.018f, 0.018f) * k, pink, soft: 0.006f);
                b.PaintEll(bone, path[^2], V(0.012f, 0.012f, 0.012f) * k, blue, soft: 0.006f);
            }
        }
        var bowAt = bc + V(0.035f, 0.09f, 0.1f);
        Bow(head, bowAt, 1.2f, new[]
        {
            Smooth(4, bowAt, bowAt + V(0.04f, 0.0f, 0.03f), bowAt + V(0.08f, -0.04f, 0.02f), bowAt + V(0.1f, -0.1f, -0.02f), bowAt + V(0.12f, -0.13f, -0.06f)),
            Smooth(4, bowAt, bowAt + V(0.02f, -0.03f, 0.05f), bowAt + V(0.03f, -0.08f, 0.08f), bowAt + V(0.06f, -0.12f, 0.07f), bowAt + V(0.08f, -0.15f, 0.04f))
        });
        var earBow = c + V(-0.05f, 0.06f, 0.0f);
        Bow(head, earBow, 0.9f, new[]
        {
            Smooth(4, earBow, earBow + V(-0.04f, 0.02f, -0.03f), earBow + V(-0.07f, -0.02f, -0.07f), earBow + V(-0.08f, -0.08f, -0.1f), earBow + V(-0.09f, -0.12f, -0.12f))
        });
        return b;
    }

    // ------------------------------------------------------------------ Hawlucha

    /// <summary>
    /// Hawlucha and its Mega Evolution: a wrestler of a bird, its wings spread green, its head and mask red with a white
    /// brow and orange plumes, a white ruff of feathers on its chest, pale legs and yellow talons. Mega Hawlucha's wings
    /// are vast and jagged, a gold ruff blazes on its chest and its face is masked black.
    /// </summary>
    private static PokeBuilder HawluchaBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Hawlucha-Mega" : "Hawlucha", mega ? 1f : 0.8f, BodyPlan.Bird, V(0, 0.26f, 0)) { Coat = Fur };
        var green = Rgb(60, 170, 120);
        var red = Rgb(214, 60, 50);
        var white = Rgb(244, 242, 236);
        var orange = Rgb(246, 150, 50);
        var gold = Rgb(246, 200, 60);
        var pale = Rgb(214, 220, 180);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.18f, 0));
            var knee = V(0.06f * s, 0.1f, 0.03f);
            b.Limb(leg, V(0.04f * s, 0.19f, 0), knee, 0.03f, 0.02f, mega ? white : green);
            b.Limb(leg, knee, V(0.06f * s, 0.025f, 0.0f), 0.016f, 0.012f, pale);
            foreach (var (dx, dz) in new[] { (-0.4f, 1f), (0f, 1.1f), (0.4f, 1f), (0f, -0.6f) })
            {
                var toe = V(0.06f * s + dx * 0.02f * s, 0.008f, dz * 0.03f);
                b.Limb(leg, V(0.06f * s, 0.025f, 0.0f), toe, 0.008f, 0.006f, pale);
                b.Spike(leg, toe, toe + V(0, -0.006f, dz * 0.012f), 0.005f, Rgb(246, 210, 80), mat: Shell, blend: 0.003f);
            }
        });
        var bc = V(0, 0.26f, 0);
        var br = V(0.06f, 0.075f, 0.05f);
        b.Ell(Body, bc, br, red);
        // The ruff on its chest: white feathers, or Mega Hawlucha's blaze of gold
        for (int row = 0; row < 3; row++)
            for (int i = -2; i <= 2; i++)
            {
                var root = bc + V(i * 0.018f, 0.03f - row * 0.03f, 0.035f + 0.004f * (2 - Math.Abs(i)));
                b.Spike(Body, root, root + V(i * 0.008f, -(mega ? 0.05f : 0.035f), 0.015f), 0.014f, mega ? gold : white, 0.5f);
            }
        b.PaintEll(Body, bc + V(0, -0.01f, 0.04f), V(0.05f, 0.07f, 0.03f), mega ? gold : white);
        PokeBuilder.Both(s => SpreadWing(b, s, bc + V(0.05f * s, 0.04f, 0), bc + V((mega ? 0.22f : 0.16f) * s, 0.1f, 0.02f), mega ? 7 : 5, 40f, -50f,
            mega ? 0.22f : 0.13f, mega ? 0.035f : 0.028f, green, mega ? Rgb(30, 110, 90) : red, 0.15f, mega, mega ? 0 : 3));
        int head = b.Head(bc + V(0, 0.07f, 0.01f));
        var c = bc + V(0, 0.12f, 0.02f);
        var r = V(0.045f, 0.045f, 0.045f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, 0.01f, 0.03f), V(0.04f, 0.02f, 0.03f), mega ? Rgb(40, 38, 46) : white);
        b.Spike(head, On(c, r, 0, c.Y - 0.01f) - V(0, 0, 0.01f), On(c, r, 0, c.Y - 0.01f) + V(0, -0.015f, 0.03f), 0.012f, Rgb(240, 200, 70), 0.6f, Shell);
        // Plumes rising from the head and falling back
        foreach (var (x, up) in new[] { (-0.015f, 0.08f), (0.015f, 0.08f), (0f, 0.1f) })
            Frond(b, head, c + V(x, 0.03f, -0.01f), c + V(x * 3f, 0.03f + up * (mega ? 1.3f : 1f), -0.06f), 0.014f, mega ? red : orange, V(0, 0.3f, 1f), 0.25f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(250, 210, 60), glare: true);
        });
        return b;
    }

    private static PokeBuilder Hawlucha() => HawluchaBuild(false);

    // ------------------------------------------------------------------ Dedenne

    /// <summary>Dedenne: a round orange mouse, a cream belly, great round ears rimmed dark brown and gold within, black whiskers standing out of its cheeks like antennae, and a thin black tail ending in a plug.</summary>
    private static PokeBuilder Dedenne()
    {
        var b = new PokeBuilder("Dedenne", 0.45f, BodyPlan.Biped, V(0, 0.09f, 0)) { Coat = Fur };
        var orange = Rgb(246, 160, 70);
        var cream = Rgb(250, 236, 180);
        var brown = Rgb(100, 70, 50);
        var black = Rgb(44, 42, 50);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.04f, 0));
            b.Ell(leg, V(0.04f * s, 0.015f, 0.025f), V(0.02f, 0.015f, 0.025f), cream);
            int arm = b.Arm(s, V(0.05f * s, 0.09f, 0.03f));
            b.Limb(arm, V(0.05f * s, 0.09f, 0.03f), V(0.04f * s, 0.07f, 0.06f), 0.012f, 0.01f, cream);
        });
        b.Ell(Body, V(0, 0.08f, 0), V(0.07f, 0.065f, 0.06f), orange);
        b.PaintEll(Body, V(0, 0.06f, 0.04f), V(0.05f, 0.045f, 0.03f), cream);
        int tail = b.Tail(V(0, 0.05f, -0.05f));
        var tp = Smooth(3, V(0, 0.05f, -0.05f), V(0, 0.03f, -0.1f), V(0.03f, 0.04f, -0.14f), V(0.06f, 0.08f, -0.14f));
        b.Tube(tail, tp, 0.005f, 0.004f, black, blend: 0f);
        foreach (float t in new[] { -1f, 0f, 1f })
            b.Limb(tail, tp[^1], tp[^1] + V(0.01f, 0.012f, t * 0.01f), 0.004f, 0.003f, black);
        int head = b.Head(V(0, 0.13f, 0.01f));
        var c = V(0, 0.17f, 0.015f);
        var r = V(0.075f, 0.062f, 0.06f);
        b.Ell(head, c, r, orange);
        PokeBuilder.Both(s =>
        {
            // A great round ear, dark rimmed and gold within
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.04f, -0.01f));
            var ec = c + V(0.09f * s, 0.09f, -0.015f);
            b.Limb(ear, c + V(0.05f * s, 0.04f, -0.01f), ec, 0.015f, 0.015f, brown);
            b.Ell(ear, ec, V(0.05f, 0.05f, 0.014f), brown, V(0, 0, -25f * s), blend: 0.008f);
            b.PaintEll(ear, ec + V(0, 0, 0.012f), V(0.038f, 0.038f, 0.01f), cream, V(0, 0, -25f * s));
            // Whiskers like antennae, two to a cheek, each with a little crossbar
            foreach (float up in new[] { 1f, -1f })
            {
                var root = c + V(0.06f * s, -0.01f + up * 0.008f, 0.02f);
                var tip = root + V(0.08f * s, up * 0.03f, 0.01f);
                b.Limb(head, root, tip, 0.004f, 0.003f, black);
                var mid = Vector3.Lerp(root, tip, 0.7f);
                b.Limb(head, mid + V(-0.004f * s, 0.012f, 0), mid + V(0.004f * s, -0.012f, 0), 0.0035f, 0.0035f, black);
            }
            var at = On(c, r, 0.03f * s, c.Y + 0.005f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(40, 36, 40));
        });
        b.Ell(head, On(c, r, 0, c.Y - 0.015f), V(0.006f, 0.004f, 0.004f), Rgb(80, 50, 40), blend: 0.003f);
        return b;
    }

    // ------------------------------------------------------------------ Carbink

    /// <summary>Carbink: a gray rock floating with its face peeping out of white fluff on top, pale blue crystals growing from its crown and sides.</summary>
    private static PokeBuilder Carbink()
    {
        var b = new PokeBuilder("Carbink", 0.5f, BodyPlan.Floating, V(0, 0.16f, 0)) { Coat = Shell }.Hover();
        var rock = Rgb(130, 128, 140);
        var dark = Rgb(96, 94, 108);
        var fluff = Rgb(240, 238, 244);
        var crystal = Rgb(170, 220, 246);
        // The rock, rough and tapering below
        b.Ell(Body, V(0, 0.15f, 0), V(0.085f, 0.07f, 0.075f), rock, mat: Shell);
        b.Spike(Body, V(0, 0.12f, 0), V(0.01f, 0.03f, 0.01f), 0.06f, rock, 0.8f, Shell);
        Lumps(b, Body, V(0, 0.13f, 0), V(0.08f, 0.07f, 0.07f), 12, 0.03f, rock, dark, 0.15f);
        foreach (var (at, to) in new[] { (V(0.07f, 0.14f, 0.03f), V(0.13f, 0.12f, 0.06f)), (V(-0.06f, 0.1f, 0.04f), V(-0.1f, 0.06f, 0.08f)), (V(0.03f, 0.08f, -0.06f), V(0.05f, 0.03f, -0.1f)) })
            b.Spike(Body, at, to, 0.02f, crystal, 0.7f, Glow);
        int head = b.Head(V(0, 0.2f, 0.01f));
        var c = V(0, 0.235f, 0.025f);
        var r = V(0.05f, 0.04f, 0.045f);
        // The white fluff round its face
        foreach (var (x, y, z, k) in new[] { (-0.06f, 0.0f, 0.0f, 0.035f), (0.06f, 0.0f, 0.0f, 0.035f), (-0.035f, -0.03f, 0.02f, 0.03f), (0.035f, -0.03f, 0.02f, 0.03f), (0f, 0.035f, -0.01f, 0.04f), (0f, -0.035f, 0.01f, 0.03f) })
            b.Ell(head, c + V(x, y, z), V(k, k * 0.85f, k), fluff, blend: 0.015f);
        b.Ell(head, c, r, Rgb(206, 202, 216));
        // A great crystal on its crown and two like ears
        b.Spike(head, c + V(0, 0.03f, -0.01f), c + V(0, 0.12f, -0.02f), 0.035f, crystal, 0.6f, Glow);
        PokeBuilder.Both(s => b.Spike(head, c + V(0.05f * s, 0.02f, -0.01f), c + V(0.12f * s, 0.07f, -0.02f), 0.025f, crystal, 0.5f, Glow));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, c.Y + 0.002f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(90, 170, 230), sclera: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Goomy line

    /// <summary>Goomy: a little lilac drop of slime resting in a darker puddle of itself, two horns on its head, green spots at its sides, little black eyes and a smile.</summary>
    private static PokeBuilder Goomy()
    {
        var b = new PokeBuilder("Goomy", 0.45f, BodyPlan.Biped, V(0, 0.08f, 0)) { Coat = Scales };
        var lilac = Rgb(206, 176, 226);
        var purple = Rgb(140, 100, 180);
        var green = Rgb(110, 190, 120);
        b.Ell(Body, V(0, 0.035f, 0), V(0.075f, 0.035f, 0.075f), purple);
        b.Ell(Body, V(0, 0.08f, 0.005f), V(0.055f, 0.055f, 0.05f), lilac, blend: 0.02f);
        int head = b.Head(V(0, 0.11f, 0.01f));
        var c = V(0, 0.14f, 0.015f);
        var r = V(0.045f, 0.045f, 0.042f);
        b.Ell(head, c, r, lilac);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.042f * s, -0.012f, -0.005f), V(0.012f, 0.014f, 0.014f), green);
            Antenna(b, head, c + V(0.016f * s, 0.035f, 0), c + V(0.024f * s, 0.07f, 0.0f), c + V(0.03f * s, 0.09f, -0.01f), 0.008f, lilac, lilac, 0.011f);
            var at = On(c, r, 0.016f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.008f, Rgb(40, 36, 44));
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.014f), V(0, -0.2f, 1f), 0.012f, 0.006f, Rgb(80, 50, 80), MarkShape.Smile);
        return b;
    }

    /// <summary>A shell of gray metal over a slime's back (the Hisuian Sliggoo and Goodra): a smooth dome with a spiral drawn round each side.</summary>
    private static void SlimeShell(PokeBuilder b, Vector3 c, Vector3 r)
    {
        b.Ell(Body, c, r, Rgb(176, 180, 194), mat: Metal, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            foreach (var k in new[] { 0.6f, 0.32f })
                b.PaintTorus(Body, c + V(r.X * 0.92f * s, 0, 0), r.Y * k, 0.004f, Rgb(120, 124, 140), V(0, 0, 90f), 1f, r.Z / r.Y);
        });
    }

    /// <summary>
    /// Sliggoo and its Hisuian form: a slime rising from a dark puddle that curls at its edges into a neck and a head,
    /// two long feelers falling back, green spots on the cheeks, green eyes. The Hisuian Sliggoo carries a shell of gray
    /// metal over its back.
    /// </summary>
    private static PokeBuilder SliggooBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Sliggoo-Hisui" : "Sliggoo", 0.7f, BodyPlan.Biped, V(0, 0.12f, 0)) { Coat = Scales };
        var lilac = hisui ? Rgb(196, 178, 226) : Rgb(206, 176, 226);
        var purple = Rgb(140, 100, 180);
        var green = Rgb(110, 190, 120);
        b.Ell(Body, V(0, 0.04f, 0), V(0.09f, 0.04f, 0.09f), purple);
        PokeBuilder.Both(s => b.Spike(Body, V(0.07f * s, 0.035f, 0.04f), V(0.12f * s, 0.03f, 0.08f), 0.025f, purple, 0.6f));
        var neck = Smooth(3, V(0, 0.05f, 0), V(0, 0.14f, 0.0f), V(0, 0.24f, 0.03f));
        b.Tube(Body, neck, 0.06f, 0.035f, lilac, blend: 0f);
        if (hisui) SlimeShell(b, V(0, 0.1f, -0.06f), V(0.085f, 0.085f, 0.075f));
        int head = b.Head(neck[^1]);
        var c = V(0, 0.29f, 0.05f);
        var r = V(0.045f, 0.04f, 0.045f);
        b.Ell(head, c, r, lilac);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.04f * s, -0.01f, -0.005f), V(0.012f, 0.014f, 0.014f), green);
            int ear = b.Ear(head, s, c + V(0.02f * s, 0.03f, -0.01f));
            b.Tube(ear, Smooth(3, c + V(0.02f * s, 0.03f, -0.01f), c + V(0.05f * s, 0.09f, -0.05f), c + V(0.07f * s, 0.07f, -0.12f), c + V(0.075f * s, 0.02f, -0.15f)), 0.01f, 0.006f, lilac, blend: 0f);
            var at = On(c, r, 0.017f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(90, 170, 110));
        });
        b.Mark(head, On(c, r, 0, c.Y - 0.014f), V(0, -0.2f, 1f), 0.012f, 0.006f, Rgb(80, 50, 80), MarkShape.Smile);
        return b;
    }

    private static PokeBuilder Sliggoo() => SliggooBuild(false);

    /// <summary>
    /// Goodra and its Hisuian form: a great soft dragon of slime, lilac in front and purple behind, green spots at its
    /// neck and tail, little arms dripping, two long feelers curling back from its head, green eyes and a happy mouth.
    /// The Hisuian Goodra carries a great shell of gray metal on its back.
    /// </summary>
    private static PokeBuilder GoodraBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Goodra-Hisui" : "Goodra", 1f, BodyPlan.Biped, V(0, 0.28f, 0)) { Coat = Scales };
        var lilac = hisui ? Rgb(196, 176, 226) : Rgb(206, 176, 226);
        var purple = Rgb(140, 100, 180);
        var green = Rgb(100, 180, 110);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.15f, 0));
            b.Limb(leg, V(0.07f * s, 0.16f, 0), V(0.08f * s, 0.04f, 0.02f), 0.05f, 0.045f, lilac);
            b.Ell(leg, V(0.085f * s, 0.025f, 0.04f), V(0.05f, 0.025f, 0.06f), lilac);
            int arm = b.Arm(s, V(0.1f * s, 0.36f, 0.04f));
            var hand = V(0.15f * s, 0.3f, 0.1f);
            b.Limb(arm, V(0.1f * s, 0.36f, 0.04f), hand, 0.03f, 0.022f, lilac);
            b.Ell(arm, hand + V(0, -0.03f, 0), V(0.012f, 0.022f, 0.012f), lilac, blend: 0.01f);
        });
        b.Ell(Body, V(0, 0.27f, 0), V(0.13f, 0.16f, 0.11f), lilac);
        b.PaintEll(Body, V(0, 0.22f, -0.07f), V(0.13f, 0.13f, 0.08f), purple);
        int tail = b.Tail(V(0, 0.18f, -0.08f));
        var tp = Smooth(3, V(0, 0.18f, -0.08f), V(0, 0.1f, -0.22f), V(0.05f, 0.05f, -0.34f), V(0.11f, 0.05f, -0.38f));
        b.Tube(tail, tp, 0.07f, 0.02f, purple, blend: 0f);
        foreach (var i in new[] { 3, 6 })
            PokeBuilder.Both(s => b.PaintEll(tail, tp[i] + V(0.045f * s, 0.01f, 0), V(0.018f, 0.018f, 0.02f), green));
        var neck = Smooth(3, V(0, 0.38f, 0.02f), V(0, 0.48f, 0.04f), V(0, 0.56f, 0.06f));
        b.Tube(Body, neck, 0.075f, 0.055f, lilac, blend: 0f);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.06f * s, 0.46f, 0.03f), V(0.02f, 0.025f, 0.02f), green));
        if (hisui) SlimeShell(b, V(0, 0.36f, -0.13f), V(0.15f, 0.16f, 0.1f));
        int head = b.Head(neck[^1]);
        var c = V(0, 0.61f, 0.08f);
        var r = V(0.065f, 0.052f, 0.065f);
        b.Ell(head, c, r, lilac);
        Grin(b, head, On(c, r, 0, c.Y - 0.022f) - V(0, 0, 0.012f), V(0.022f, 0.01f, 0.014f), Rgb(170, 90, 120));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.04f, -0.02f));
            b.Tube(ear, Smooth(3, c + V(0.03f * s, 0.04f, -0.02f), c + V(0.08f * s, 0.12f, -0.08f), c + V(0.12f * s, 0.08f, -0.2f), c + V(0.1f * s, -0.02f, -0.26f), c + V(0.07f * s, -0.03f, -0.22f)), 0.016f, 0.008f, lilac, blend: 0f);
            var at = On(c, r, 0.026f * s, c.Y + 0.01f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(90, 170, 110));
        });
        return b;
    }

    private static PokeBuilder Goodra() => GoodraBuild(false);

    // ------------------------------------------------------------------ Klefki

    /// <summary>Klefki: a little key-collecting fairy hanging inside a ring of steel, a pink cap joining its round white face to the ring, a pink tongue, and four keys of brass, black iron and copper on the ring.</summary>
    private static PokeBuilder Klefki()
    {
        var b = new PokeBuilder("Klefki", 0.45f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Metal }.Hover();
        var steel = Rgb(206, 210, 220);
        var pink = Rgb(236, 150, 170);
        b.Torus(Body, V(0, 0.2f, 0), 0.1f, 0.008f, steel, V(90f, 0, 0), mat: Metal, blend: 0.004f);
        // Keys on the ring, pointing outward
        var keys = new[] { (200f, Rgb(220, 180, 80)), (245f, Rgb(60, 56, 66)), (295f, Rgb(190, 110, 80)), (340f, Rgb(220, 180, 80)) };
        foreach (var (deg, color) in keys)
        {
            float a = deg * Degree;
            var d = V(MathF.Cos(a), MathF.Sin(a), 0);
            var at = V(0, 0.2f, 0) + d * 0.1f;
            b.Torus(Body, at + d * 0.018f, 0.014f, 0.0045f, color, V(90f, 0, 0), mat: Metal, blend: 0.004f);
            b.Limb(Body, at + d * 0.03f, at + d * 0.09f, 0.006f, 0.006f, color, Metal, 0.003f);
            var side = V(-d.Y, d.X, 0);
            b.Box(Body, at + d * 0.08f + side * 0.01f, V(0.008f, 0.006f, 0.004f), 0.002f, color, V(0, 0, deg), Metal, 0.003f);
        }
        int head = b.Head(V(0, 0.28f, 0));
        var c = V(0, 0.245f, 0.005f);
        var r = V(0.035f, 0.035f, 0.03f);
        b.Limb(head, V(0, 0.3f, 0), c + V(0, 0.02f, 0), 0.012f, 0.016f, pink);
        b.Ell(head, c + V(0, 0.028f, -0.005f), V(0.026f, 0.014f, 0.024f), pink, blend: 0.008f);
        b.Ell(head, c, r, Rgb(244, 240, 246));
        b.Ell(head, c + V(0, -0.035f, 0.01f), V(0.009f, 0.016f, 0.006f), pink, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.012f * s, c.Y + 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.007f, Rgb(40, 36, 44));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Phantump and Trevenant

    /// <summary>Phantump: a little stump haunted by a ghost, its face hollow-eyed and gaping, two twigs with a leaf each for arms, a black wisp of hair above and a black ghostly tail below.</summary>
    private static PokeBuilder Phantump()
    {
        var b = new PokeBuilder("Phantump", 0.45f, BodyPlan.Floating, V(0, 0.17f, 0)) { Coat = Fur }.Hover();
        var bark = Rgb(130, 100, 72);
        var dark = Rgb(46, 42, 52);
        var green = Rgb(80, 160, 80);
        b.Spike(Body, V(0, 0.15f, 0), V(0.03f, 0.0f, -0.03f), 0.05f, dark, 0.8f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.05f * s, 0.12f, 0.02f), V(0.018f, 0.014f, 0.016f), dark, blend: 0.012f));
        int head = b.Head(V(0, 0.17f, 0));
        var c = V(0, 0.22f, 0);
        var r = V(0.08f, 0.07f, 0.07f);
        b.Ell(head, c, r, bark);
        Grin(b, head, On(c, r, 0, c.Y - 0.03f) - V(0, 0, 0.01f), V(0.025f, 0.015f, 0.02f), dark);
        b.Tube(head, Smooth(3, c + V(0, 0.06f, 0), c + V(0.02f, 0.12f, -0.02f), c + V(-0.02f, 0.16f, -0.03f), c + V(0.0f, 0.19f, -0.01f)), 0.02f, 0.008f, dark, blend: 0f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, c + V(0.07f * s, 0.0f, 0));
            var tip = c + V(0.13f * s, 0.06f, 0.0f);
            b.Limb(arm, c + V(0.07f * s, 0.0f, 0), tip, 0.014f, 0.009f, bark);
            Petal(b, arm, tip, tip + V(0.03f * s, 0.04f, 0.01f), 0.02f, green, Leaf);
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.PaintEll(head, at, V(0.022f, 0.024f, 0.02f), dark);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(240, 110, 130));
        });
        return Lift(b);
    }

    /// <summary>Trevenant: a tree possessed, its trunk split by a jagged mouth and one red eye glaring from a dark hollow, branches for arms ending in clumps of leaves, roots for legs and a crown of leafy twigs.</summary>
    private static PokeBuilder Trevenant()
    {
        var b = new PokeBuilder("Trevenant", 1f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Fur };
        var bark = Rgb(122, 98, 72);
        var dark = Rgb(56, 44, 40);
        var leaf = Rgb(70, 140, 72);
        var leafLight = Rgb(110, 176, 96);
        // Roots for legs, three to a side
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.2f, 0));
            foreach (var (dx, dz) in new[] { (0.1f, 0.04f), (0.06f, 0.12f), (0.08f, -0.08f) })
                b.Tube(leg, Smooth(3, V(0.05f * s, 0.22f, 0), V((0.05f + dx * 0.6f) * s, 0.1f, dz * 0.6f), V((0.06f + dx) * s, 0.0f, dz)), 0.032f, 0.01f, bark, blend: 0f);
        });
        var bc = V(0, 0.38f, 0);
        var br = V(0.1f, 0.21f, 0.09f);
        b.Ell(Body, bc, br, bark);
        foreach (var (x, y) in new[] { (0.05f, 0.25f), (-0.06f, 0.32f), (0.07f, 0.4f) })
            b.PaintEll(Body, V(x, y, br.Z * 0.6f), V(0.004f, 0.05f, 0.03f), dark, soft: 0.004f);
        // A jagged mouth across the trunk and one red eye in a dark hollow above it
        b.Mark(Body, Out(bc, br, default, V(0, -0.05f, 1f)), V(0, 0, 1f), 0.06f, 0.015f, dark, MarkShape.Zigzag);
        var eyeAt = Out(bc, br, default, V(-0.35f, 0.38f, 1f));
        b.PaintEll(Body, eyeAt, V(0.035f, 0.03f, 0.03f), dark);
        b.Eye(Body, eyeAt, Outward(bc, br, eyeAt), 0.014f, Rgb(220, 50, 60), glare: true);
        // Branches for arms, each ending in a clump of leaves
        void Clump(int bone, Vector3 at, float k)
        {
            foreach (var (x, y, z) in new[] { (0f, 0f, 0f), (0.04f, 0.02f, 0.01f), (-0.03f, 0.03f, 0.0f), (0.01f, -0.035f, 0.02f), (0.0f, 0.02f, -0.03f) })
                b.Ell(bone, at + V(x, y, z) * k, V(0.04f, 0.035f, 0.04f) * k, (int)(x * 100) % 2 == 0 ? leaf : leafLight, mat: Leaf, blend: 0.015f);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, bc + V(0.08f * s, 0.1f, 0));
            var path = Smooth(3, bc + V(0.08f * s, 0.1f, 0), bc + V(0.2f * s, 0.18f, 0.02f), bc + V(0.3f * s, 0.24f, 0.04f));
            b.Tube(arm, path, 0.03f, 0.018f, bark, blend: 0f);
            foreach (var d in new[] { V(0.05f * s, 0.06f, 0), V(0.07f * s, -0.01f, 0.02f), V(0.02f * s, 0.07f, 0.03f) })
                b.Limb(arm, path[^1], path[^1] + d, 0.012f, 0.006f, bark);
            Clump(arm, path[^1] + V(0.04f * s, 0.03f, 0.01f), 1.2f);
        });
        // A crown of twigs, leafy at their ends
        foreach (var (x, z) in new[] { (-0.05f, 0.0f), (0.05f, -0.01f), (0f, -0.04f) })
        {
            var top = bc + V(x * 2.4f, br.Y + 0.12f, z);
            b.Limb(Body, bc + V(x, br.Y * 0.8f, z), top, 0.022f, 0.012f, bark);
            Clump(Body, top, 0.9f);
        }
        return b;
    }

    // ------------------------------------------------------------------ Pumpkaboo and Gourgeist

    /// <summary>The names of the sizes of the Pumpkaboo line, small to super; the average is the species itself.</summary>
    private static readonly string?[] PumpkinSizes = { "Small", null, "Large", "Super" };

    /// <summary>
    /// Pumpkaboo in each size: an orange pumpkin ribbed darker, two yellow lights shining from it, the spirit in it
    /// peeping out on top under a dark brown hood with a ragged brim and a curl of a stalk, little yellow eyes and a fang.
    /// The bigger the pumpkin the bigger the spirit has made it.
    /// </summary>
    private static PokeBuilder PumpkabooBuild(int size)
    {
        float k = new[] { 0.85f, 1f, 1.12f, 1.25f }[size];
        var name = PumpkinSizes[size] is { } sz ? "Pumpkaboo-" + sz : "Pumpkaboo";
        var b = new PokeBuilder(name, new[] { 0.42f, 0.5f, 0.58f, 0.68f }[size], BodyPlan.Floating, V(0, 0.1f * k, 0)) { Coat = Leaf }.Hover();
        var orange = Rgb(232, 130, 70);
        var rib = Rgb(196, 96, 50);
        var brown = Rgb(74, 58, 46);
        var glow = Rgb(250, 214, 80);
        var pc = V(0, 0.1f * k, 0);
        var pr = V(0.1f, 0.085f, 0.09f) * k;
        b.Ell(Body, pc, pr, orange);
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 8f;
            b.PaintEll(Body, pc + V(MathF.Sin(a) * pr.X, 0, MathF.Cos(a) * pr.Z) * 0.97f, V(0.006f, pr.Y * 0.95f, 0.006f), rib, V(0, a * 180f / MathF.PI, 0), 0.004f);
        }
        PokeBuilder.Both(s => b.Mark(Body, Out(pc, pr, default, V(0.4f * s, -0.15f, 1f)), V(0.4f * s, -0.15f, 1f), 0.017f * k, 0.017f * k, glow));
        int head = b.Head(pc + V(0, pr.Y * 0.8f, 0));
        var hc = pc + V(0, pr.Y + 0.03f, 0.005f);
        var hr = V(0.06f, 0.04f, 0.05f);
        b.Ell(head, hc, hr, brown);
        // The hood's ragged brim, hanging over the pumpkin
        for (int i = 0; i < 7; i++)
        {
            float a = (i / 6f - 0.5f) * 260f * Degree;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            b.Spike(head, hc + d * 0.04f, hc + d * (0.09f + 0.01f * k) + V(0, -0.04f, 0), 0.028f, brown, 0.4f);
        }
        b.Tube(head, Smooth(3, hc + V(0, 0.03f, -0.01f), hc + V(0.01f, 0.08f, -0.015f), hc + V(0.04f, 0.1f, -0.01f), hc + V(0.045f, 0.075f, 0.0f)), 0.014f, 0.008f, brown, blend: 0f);
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.018f * s, hc.Y - 0.002f);
            b.Eye(head, at, Outward(hc, hr, at), 0.008f, Rgb(250, 210, 60), glare: true);
        });
        b.Spike(head, On(hc, hr, 0.006f, hc.Y - 0.02f), On(hc, hr, 0.006f, hc.Y - 0.02f) + V(0, -0.01f, 0.002f), 0.004f, White, mat: Shell, blend: 0.002f);
        return Lift(b);
    }

    private static PokeBuilder Pumpkaboo() => PumpkabooBuild(1);

    /// <summary>
    /// Gourgeist in each size: a dark pumpkin of a body carved with a glowing jack-o'-lantern face, a pale little face
    /// above it under a mop of salmon hair, two long locks of hair for arms ending in hands; the bigger the pumpkin, the
    /// bigger the body.
    /// </summary>
    private static PokeBuilder GourgeistBuild(int size)
    {
        float k = new[] { 0.88f, 1f, 1.1f, 1.22f }[size];
        var name = PumpkinSizes[size] is { } sz ? "Gourgeist-" + sz : "Gourgeist";
        var b = new PokeBuilder(name, new[] { 0.75f, 0.85f, 0.92f, 1f }[size], BodyPlan.Floating, V(0, 0.16f * k, 0)) { Coat = Leaf }.Hover();
        var dark = Rgb(66, 56, 48);
        var rib = Rgb(44, 38, 34);
        var glow = Rgb(250, 210, 70);
        var tan = Rgb(220, 190, 140);
        var hair = Rgb(236, 150, 124);
        var pc = V(0, 0.16f * k, 0);
        var pr = V(0.1f, 0.13f, 0.1f) * k;
        b.Ell(Body, pc, pr, dark);
        for (int i = 0; i < 8; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 8f;
            b.PaintEll(Body, pc + V(MathF.Sin(a) * pr.X, 0, MathF.Cos(a) * pr.Z) * 0.97f, V(0.006f, pr.Y * 0.95f, 0.006f), rib, V(0, a * 180f / MathF.PI, 0), 0.004f);
        }
        // A jack-o'-lantern face carved low on the pumpkin, glowing
        PokeBuilder.Both(s => b.Mark(Body, Out(pc, pr, default, V(0.42f * s, 0.25f, 1f)), V(0.42f * s, 0.25f, 1f), 0.02f * k, 0.016f * k, glow, MarkShape.Triangle, 180f + 20f * s));
        b.Mark(Body, Out(pc, pr, default, V(0, -0.35f, 1f)), V(0, -0.35f, 1f), 0.04f * k, 0.018f * k, glow, MarkShape.Smile);
        // The pale face above, under its hair
        var hc = pc + V(0, pr.Y + 0.035f, 0.02f);
        var hr = V(0.04f, 0.042f, 0.036f);
        b.Limb(Body, pc + V(0, pr.Y * 0.8f, 0), hc, 0.03f, 0.025f, tan);
        int head = b.Head(hc - V(0, 0.03f, 0));
        b.Ell(head, hc, hr, tan);
        b.Ell(head, hc + V(0, 0.022f, -0.018f), V(0.048f, 0.04f, 0.042f), hair, blend: 0.012f);
        Frond(b, head, hc + V(-0.01f, 0.035f, 0.01f), hc + V(0.03f, -0.03f, 0.035f), 0.02f, hair, V(0, 0, 1f), 0.3f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, hc + V(0.04f * s, 0.02f, -0.01f));
            var path = Smooth(3, hc + V(0.04f * s, 0.02f, -0.01f), hc + V(0.12f * s, 0.06f, -0.02f), hc + V(0.2f * s, 0.03f, 0.0f), hc + V(0.24f * s, -0.05f, 0.02f));
            Ribbon(b, arm, path, 0.035f, hair, V(0, 0.5f, 1f), 0.012f);
            var hand = path[^1];
            foreach (var t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand, hand + V(t * 0.012f * s, -0.035f, 0.01f + t * 0.008f), 0.006f, hair, mat: Fur, blend: 0.004f);
            var at = On(hc, hr, 0.015f * s, hc.Y - 0.004f);
            b.Eye(head, at, Outward(hc, hr, at), 0.008f, Rgb(250, 200, 70), closed: size == 3);
        });
        return Lift(b);
    }

    private static PokeBuilder Gourgeist() => GourgeistBuild(1);

    // ------------------------------------------------------------------ Bergmite and Avalugg

    /// <summary>Bergmite: a chunk of pale blue ice cut in facets, a crystal standing up from its crown, a row of icy teeth round its foot, two great yellow eyes rimmed lilac.</summary>
    private static PokeBuilder Bergmite()
    {
        var b = new PokeBuilder("Bergmite", 0.55f, BodyPlan.Biped, V(0, 0.1f, 0)) { Coat = Shell };
        var ice = Rgb(206, 228, 246);
        var shade = Rgb(170, 200, 236);
        b.Ell(Body, V(0, 0.1f, 0.0f), V(0.085f, 0.08f, 0.075f), ice, mat: Shell);
        b.Box(Body, V(0, 0.09f, -0.01f), V(0.06f, 0.06f, 0.06f), 0.012f, ice, V(20f, 45f, 15f), Shell, 0.012f);
        b.Box(Body, V(0, 0.13f, -0.02f), V(0.045f, 0.04f, 0.045f), 0.01f, shade, V(-15f, 20f, 30f), Shell, 0.012f);
        b.PaintEll(Body, V(0, 0.15f, -0.04f), V(0.08f, 0.04f, 0.06f), shade);
        b.Spike(Body, V(0, 0.16f, -0.01f), V(0.02f, 0.27f, -0.03f), 0.04f, ice, 0.5f, Shell);
        b.Spike(Body, V(-0.04f, 0.15f, -0.01f), V(-0.065f, 0.22f, -0.03f), 0.025f, ice, 0.5f, Shell);
        PokeBuilder.Both(s => Blade(b, Body, V(0.07f * s, 0.09f, 0), V(0.12f * s, 0.05f, -0.01f), 0.03f, ice, V(0, 0, 1f), 0.3f, Shell));
        for (int i = -2; i <= 2; i++)
        {
            var root = V(i * 0.026f, 0.04f, 0.055f - 0.008f * Math.Abs(i));
            b.Spike(Body, root, root + V(0, -0.04f, 0.01f), 0.012f, White, 0.6f, Shell);
        }
        var c = V(0, 0.1f, 0.0f);
        var r = V(0.085f, 0.08f, 0.075f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, 0.11f);
            b.PaintEll(Body, at, V(0.026f, 0.029f, 0.02f), Rgb(150, 120, 196));
            b.Eye(Body, at, Outward(c, r, at), 0.016f, sclera: true, white: Rgb(250, 200, 60), pupil: Rgb(40, 30, 50));
        });
        return b;
    }

    /// <summary>
    /// Avalugg and its Hisuian form: a great flat slab of ice on four pillars of legs, banded purple across its top, its
    /// small face looking out from under the slab's front edge. The Hisuian Avalugg's body is dark rock under the ice,
    /// and its slab is pointed in front like the prow of an icebreaker.
    /// </summary>
    private static PokeBuilder AvaluggBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Avalugg-Hisui" : "Avalugg", 1f, BodyPlan.Quadruped, V(0, 0.28f, 0)) { Coat = Shell };
        var ice = Rgb(214, 232, 248);
        var band = Rgb(150, 130, 196);
        var deep = Rgb(160, 190, 226);
        var rock = Rgb(100, 84, 76);
        var under = hisui ? rock : deep;
        foreach (var (z, front) in new[] { (0.12f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.14f * s, 0.22f, z), front);
                b.Limb(leg, V(0.14f * s, 0.22f, z), V(0.15f * s, 0.05f, z), 0.06f, 0.055f, under, Shell);
                b.Box(leg, V(0.15f * s, 0.03f, z + 0.01f), V(0.06f, 0.03f, 0.065f), 0.012f, hisui ? rock : ice, mat: Shell, blend: 0.01f);
                foreach (float t in new[] { -1f, 0f, 1f })
                    b.Spike(leg, V(0.15f * s + t * 0.03f, 0.02f, z + 0.07f), V(0.15f * s + t * 0.035f, 0.01f, z + 0.1f), 0.012f, White, 0.6f, Shell);
            });
        b.Ell(Body, V(0, 0.24f, 0), V(0.2f, 0.08f, 0.24f), under, mat: Shell);
        if (hisui)
        {
            b.Box(Body, V(0, 0.32f, -0.03f), V(0.22f, 0.07f, 0.24f), 0.04f, ice, mat: Shell, blend: 0.02f);
            Blade(b, Body, V(0, 0.32f, 0.16f), V(0, 0.34f, 0.42f), 0.2f, ice, V(0, 1f, 0), 0.4f, Shell);
            foreach (var z in new[] { -0.14f, 0.0f, 0.14f })
                b.PaintEll(Body, V(0, 0.39f, z), V(0.24f, 0.01f, 0.018f), band, V(0, 12f, 0), 0.006f);
        }
        else
        {
            b.Box(Body, V(0, 0.32f, 0), V(0.24f, 0.08f, 0.28f), 0.045f, ice, mat: Shell, blend: 0.02f);
            foreach (var z in new[] { -0.14f, 0.0f, 0.14f })
                b.PaintEll(Body, V(0, 0.4f, z), V(0.26f, 0.012f, 0.02f), band, V(0, 12f, 0), 0.006f);
        }
        int head = b.Head(V(0, 0.22f, 0.2f));
        var c = V(0, 0.2f, 0.25f);
        var r = V(0.08f, 0.05f, 0.05f);
        b.Ell(head, c, r, under, mat: Shell);
        Grin(b, head, On(c, r, 0, c.Y - 0.018f) - V(0, 0, 0.01f), V(0.04f, 0.01f, 0.015f), Rgb(60, 70, 110));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, hisui ? Rgb(250, 200, 70) : Rgb(50, 70, 130), sclera: !hisui);
        });
        return b;
    }

    private static PokeBuilder Avalugg() => AvaluggBuild(false);

    // ------------------------------------------------------------------ Noibat and Noivern

    /// <summary>Noibat: a little purple bat with ears far bigger than its head, dark within, a fluffy gray body, purple wings of skin, yellow eyes and a fanged mouth.</summary>
    private static PokeBuilder Noibat()
    {
        var b = new PokeBuilder("Noibat", 0.5f, BodyPlan.Bird, V(0, 0.2f, 0)) { Coat = Fur }.Hover();
        var purple = Rgb(156, 116, 196);
        var dark = Rgb(76, 66, 90);
        var gray = Rgb(96, 90, 106);
        var inner = Rgb(60, 50, 80);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.04f * s, 0.21f, 0));
            BatWing(b, wing, V(0.04f * s, 0.21f, 0), V(0.13f * s, 0.25f, -0.01f),
                new[] { V(0.21f * s, 0.2f, -0.02f), V(0.19f * s, 0.13f, -0.02f), V(0.11f * s, 0.11f, -0.01f) }, V(0.04f * s, 0.16f, -0.005f), purple, dark, 0.008f);
        });
        b.Ell(Body, V(0, 0.19f, 0), V(0.048f, 0.05f, 0.045f), gray);
        FurTufts(b, Body, V(0, 0.19f, 0), V(0.048f, 0.05f, 0.045f), 14, 0.018f, 0.012f, gray, 1.1f, -0.9f, 0.6f);
        DanglingLegs(b, 0.02f, 0.15f, 0, 0.03f, 0.006f, dark);
        int head = b.Head(V(0, 0.23f, 0.01f));
        var c = V(0, 0.265f, 0.02f);
        var r = V(0.045f, 0.04f, 0.04f);
        b.Ell(head, c, r, purple);
        Gape(b, head, On(c, r, 0, c.Y - 0.018f) - V(0, 0, 0.008f), V(0.016f, 0.01f, 0.012f), Rgb(80, 40, 70), 0.008f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.03f, 0));
            var ec = c + V(0.075f * s, 0.075f, -0.01f);
            b.Limb(ear, c + V(0.03f * s, 0.03f, 0), ec, 0.016f, 0.016f, purple);
            b.Ell(ear, ec, V(0.055f, 0.06f, 0.015f), purple, V(0, 0, -30f * s), blend: 0.008f);
            b.PaintEll(ear, ec + V(0, 0, 0.012f), V(0.04f, 0.045f, 0.01f), inner, V(0, 0, -30f * s));
            var at = On(c, r, 0.02f * s, c.Y + 0.008f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(250, 210, 60), sclera: true, white: Rgb(250, 220, 90), pupil: Rgb(40, 36, 44));
        });
        return Lift(b);
    }

    /// <summary>Noivern: a dragon of a bat in flight, black with a white ruff, great wings of dark skin washed purple at their edges, red claws, ears like loudspeakers ringed green, a fanged mouth and a long tail.</summary>
    private static PokeBuilder Noivern()
    {
        var b = new PokeBuilder("Noivern", 1f, BodyPlan.Bird, V(0, 0.44f, 0)) { Coat = Fur }.Hover();
        var black = Rgb(56, 52, 66);
        var purple = Rgb(150, 100, 200);
        var white = Rgb(236, 236, 240);
        var green = Rgb(70, 210, 150);
        var red = Rgb(214, 50, 60);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.07f * s, 0.5f, -0.02f));
            var wrist = V(0.3f * s, 0.66f, -0.04f);
            var tips = new[] { V(0.56f * s, 0.56f, -0.06f), V(0.5f * s, 0.36f, -0.06f), V(0.34f * s, 0.24f, -0.05f) };
            DragonWing(b, wing, V(0.07f * s, 0.5f, -0.02f), wrist, tips, V(0.06f * s, 0.36f, -0.03f), black, black, 0.014f);
            foreach (var t in tips)
                b.PaintEll(wing, Vector3.Lerp(wrist, t, 0.8f), V(0.08f, 0.08f, 0.04f), purple, soft: 0.03f);
            b.Spike(wing, wrist, wrist + V(0.02f * s, 0.05f, 0.01f), 0.012f, red, 0.6f, Shell);
        });
        b.Ell(Body, V(0, 0.44f, 0), V(0.08f, 0.11f, 0.07f), black);
        b.Ell(Body, V(0, 0.52f, 0.01f), V(0.075f, 0.04f, 0.065f), white, blend: 0.02f);
        FurTufts(b, Body, V(0, 0.52f, 0.01f), V(0.075f, 0.04f, 0.065f), 16, 0.03f, 0.016f, white, 1.1f, -0.8f, 0.7f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.36f, 0));
            var foot = V(0.07f * s, 0.25f, 0.03f);
            b.Limb(leg, V(0.05f * s, 0.37f, 0), foot, 0.025f, 0.016f, black);
            Claws(b, leg, foot + V(0, -0.005f, 0.01f), V(0.01f, 0, 0), V(0, -0.6f, 0.5f), 0.025f, 0.007f);
        });
        int tail = b.Tail(V(0, 0.36f, -0.06f));
        b.Tube(tail, Smooth(3, V(0, 0.36f, -0.06f), V(0, 0.28f, -0.16f), V(0, 0.26f, -0.3f), V(0, 0.3f, -0.4f)), 0.03f, 0.008f, black, blend: 0f);
        int head = b.Head(V(0, 0.56f, 0.02f));
        var c = V(0, 0.62f, 0.05f);
        var r = V(0.055f, 0.045f, 0.06f);
        b.Ell(head, c, r, black);
        Gape(b, head, On(c, r, 0, c.Y - 0.02f) - V(0, 0, 0.01f), V(0.022f, 0.012f, 0.016f), Rgb(140, 40, 60), 0.012f);
        PokeBuilder.Both(s =>
        {
            // Ears like loudspeakers, a green ring round each dark cone
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.03f, -0.01f));
            var ec = c + V(0.1f * s, 0.05f, -0.01f);
            b.Limb(ear, c + V(0.04f * s, 0.03f, -0.01f), ec, 0.025f, 0.03f, black);
            b.Ell(ear, ec, V(0.035f, 0.06f, 0.055f), black, V(0, 0, -20f * s), blend: 0.01f);
            b.PaintTorus(ear, ec + V(0.03f * s, 0, 0), 0.035f, 0.008f, green, V(0, 0, 90f + 20f * s), 1.5f, 1f);
            var at = On(c, r, 0.025f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.011f, Rgb(250, 200, 60), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Xerneas and Yveltal

    /// <summary>
    /// Xerneas, in its Neutral or its Active Mode: a great deer of life, blue in front and black behind, lines of pale
    /// blue down its body, long thin black legs, and antlers like a tree of light. Resting, the antlers are pale; active,
    /// every tine shines its own colour of the rainbow.
    /// </summary>
    private static PokeBuilder XerneasBuild(bool active)
    {
        var b = new PokeBuilder(active ? "Xerneas-Active" : "Xerneas", 1f, BodyPlan.Quadruped, V(0, 0.48f, -0.01f)) { Coat = Fur };
        var blue = Rgb(70, 120, 200);
        var black = Rgb(40, 44, 60);
        var light = Rgb(160, 210, 246);
        var horn = active ? Rgb(246, 244, 236) : Rgb(206, 226, 246);
        var tips = active
            ? new[] { Rgb(240, 80, 80), Rgb(246, 160, 60), Rgb(250, 220, 80), Rgb(110, 200, 110), Rgb(110, 150, 240), Rgb(180, 110, 220) }
            : new[] { Rgb(120, 170, 236) };
        BeastLegs(b, 0.06f, 0.38f, 0.14f, -0.16f, 0.038f, black, black);
        var bc = V(0, 0.45f, -0.01f);
        var br = V(0.09f, 0.1f, 0.2f);
        b.Ell(Body, bc, br, black);
        b.PaintEll(Body, bc + V(0, 0.01f, 0.13f), V(0.085f, 0.09f, 0.08f), blue);
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.03f, -0.04f), (-0.02f, -0.1f) })
                b.PaintEll(Body, Out(bc, br, default, V(s, y / br.Y, z / br.Z)), V(0.01f, 0.05f, 0.012f), light, V(30f, 0, 0), 0.004f);
        });
        int tail = b.Tail(bc + V(0, 0.04f, -0.17f));
        foreach (float a in new[] { -20f, 0f, 20f })
            b.Spike(tail, bc + V(0, 0.04f, -0.16f), bc + V(MathF.Sin(a * Degree) * 0.05f, 0.1f, -0.24f), 0.02f, black, 0.5f);
        var neck = Smooth(3, bc + V(0, 0.04f, 0.13f), bc + V(0, 0.18f, 0.19f), bc + V(0, 0.3f, 0.21f));
        b.Tube(Body, neck, 0.045f, 0.032f, blue, blend: 0f);
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.03f, 0.04f);
        var r = V(0.038f, 0.04f, 0.065f);
        b.Ell(head, c, r, blue, V(25f, 0, 0));
        int n = 0;
        PokeBuilder.Both(s =>
        {
            CatEar(b, head, c + V(0.03f * s, 0.02f, -0.03f), c + V(0.08f * s, 0.03f, -0.06f), 0.014f, blue, black);
            // The antlers: a beam rising and spreading, its tines each tipped in a colour
            var b0 = c + V(0.02f * s, 0.035f, -0.03f);
            var b1 = c + V(0.07f * s, 0.12f, -0.05f);
            var b2 = c + V(0.12f * s, 0.22f, -0.07f);
            var b3 = c + V(0.15f * s, 0.31f, -0.09f);
            b.Tube(head, Smooth(3, b0, b1, b2, b3), 0.012f, 0.006f, horn, Glow, 0f);
            foreach (var (from, d) in new[] { (b1, V(0.07f * s, 0.06f, 0.02f)), (b1, V(-0.02f * s, 0.1f, 0.0f)), (b2, V(0.08f * s, 0.05f, 0.0f)), (b2, V(-0.01f * s, 0.1f, -0.02f)), (b3, V(0.05f * s, 0.03f, 0.0f)) })
            {
                var tip = from + d;
                b.Limb(head, from, tip, 0.007f, 0.004f, horn, Glow, 0.004f);
                b.PaintEll(head, tip, V(0.022f, 0.022f, 0.022f), tips[n++ % tips.Length], soft: 0.006f);
            }
            b.PaintEll(head, b3, V(0.022f, 0.022f, 0.022f), tips[n++ % tips.Length], soft: 0.006f);
            var at = Out(c, r, V(25f, 0, 0), V(0.6f * s, 0.25f, 0.75f));
            b.Eye(head, at, V(0.6f * s, 0.25f, 0.75f), 0.01f, Rgb(70, 160, 230), glare: true);
        });
        return b;
    }

    private static PokeBuilder Xerneas() => XerneasBuild(false);

    /// <summary>Yveltal: a great bird of destruction in flight, black and red, its wings spread wide in long clawed feathers, a long neck to a hooked head with a red crest, and a tail of three long feathers like a Y.</summary>
    private static PokeBuilder Yveltal()
    {
        var b = new PokeBuilder("Yveltal", 1f, BodyPlan.Bird, V(0, 0.46f, 0)) { Coat = Fur }.Hover();
        var red = Rgb(196, 40, 52);
        var black = Rgb(40, 36, 46);
        var gray = Rgb(110, 106, 118);
        PokeBuilder.Both(s => SpreadWing(b, s, V(0.06f * s, 0.5f, -0.02f), V(0.3f * s, 0.6f, -0.04f), 6, 35f, -45f, 0.26f, 0.045f, red, black, 0.2f, true, 0));
        b.Ell(Body, V(0, 0.46f, 0), V(0.07f, 0.12f, 0.065f), black);
        b.PaintEll(Body, V(0, 0.44f, 0.05f), V(0.05f, 0.09f, 0.03f), red);
        foreach (var (x, len) in new[] { (-0.06f, 0.3f), (0f, 0.34f), (0.06f, 0.3f) })
            Blade(b, Body, V(x * 0.4f, 0.36f, -0.04f), V(x * 2f, 0.36f - len, -0.06f), 0.04f, x == 0 ? black : red, V(0, 0, 1f), 0.22f);
        DanglingLegs(b, 0.04f, 0.38f, 0.0f, 0.08f, 0.012f, gray);
        var neck = Smooth(3, V(0, 0.55f, 0.02f), V(0, 0.66f, 0.05f), V(0, 0.72f, 0.12f));
        b.Tube(Body, neck, 0.04f, 0.028f, black, blend: 0f);
        int head = b.Head(neck[^1]);
        var c = neck[^1] + V(0, 0.01f, 0.04f);
        var r = V(0.035f, 0.032f, 0.055f);
        b.Ell(head, c, r, black);
        b.Spike(head, c + V(0, -0.005f, 0.04f), c + V(0, -0.04f, 0.1f), 0.02f, gray, 0.6f, Shell);
        foreach (var (x, len) in new[] { (0f, 0.1f), (-0.015f, 0.08f), (0.015f, 0.08f) })
            Blade(b, head, c + V(x, 0.02f, -0.02f), c + V(x * 3f, 0.04f, -0.02f - len), 0.025f, red, V(0, 1f, 0), 0.25f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.02f * s, c.Y + 0.006f);
            b.Eye(head, at, Outward(c, r, at), 0.009f, Rgb(70, 180, 220), glare: true);
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Zygarde

    /// <summary>
    /// Zygarde and its formes, black and set with green cells: the 50% Forme a serpent coiled on the ground, a hood of
    /// black fins round its head and its tail's end green; the 10% Forme a lean black dog, jaws open and a green streak
    /// flying from its neck; the Complete Forme a giant standing tall, white cells on its chest and two great capes of
    /// black from its back; and Mega Zygarde its green core hanging under a vast black cylinder banded red, a purple
    /// cross on its end.
    /// </summary>
    private static PokeBuilder ZygardeBuild(int form)
    {
        var black = Rgb(40, 44, 42);
        var green = Rgb(120, 220, 90);
        var white = Rgb(236, 240, 236);
        var cyan = Rgb(90, 220, 200);
        switch (form)
        {
            case 1:
            {
                // The 10% Forme: a lean dog of black, jaws open, cells of green on its flanks
                var b = new PokeBuilder("Zygarde-10", 0.85f, BodyPlan.Quadruped, V(0, 0.3f, -0.02f)) { Coat = Scales };
                BeastLegs(b, 0.05f, 0.26f, 0.12f, -0.14f, 0.03f, black, black);
                var bc = V(0, 0.3f, -0.02f);
                var br = V(0.06f, 0.07f, 0.16f);
                b.Ell(Body, bc, br, black);
                PokeBuilder.Both(s =>
                {
                    foreach (var z in new[] { -0.08f, 0.03f })
                        b.PaintEll(Body, Out(bc, br, default, V(s, 0.2f, z / br.Z)), V(0.025f, 0.025f, 0.025f), green);
                });
                int tail = b.Tail(bc + V(0, 0.02f, -0.15f));
                b.Tube(tail, Smooth(3, bc + V(0, 0.02f, -0.15f), bc + V(0, 0.0f, -0.24f), bc + V(0, -0.06f, -0.3f)), 0.02f, 0.006f, black, blend: 0f);
                var neck = Smooth(3, bc + V(0, 0.03f, 0.13f), bc + V(0, 0.1f, 0.2f), bc + V(0, 0.14f, 0.26f));
                b.Tube(Body, neck, 0.035f, 0.028f, black, blend: 0f);
                Ribbon(b, Body, Smooth(3, bc + V(0, 0.08f, 0.17f), bc + V(0, 0.1f, 0.06f), bc + V(0, 0.08f, -0.1f), bc + V(0, 0.1f, -0.24f)), 0.04f, green, V(0, 1f, 0), 0.01f);
                int head = b.Head(neck[^1]);
                var c = neck[^1] + V(0, 0.02f, 0.04f);
                var r = V(0.035f, 0.035f, 0.06f);
                b.Ell(head, c, r, black);
                Gape(b, head, c + V(0, -0.01f, 0.04f), V(0.02f, 0.016f, 0.025f), green, 0.012f);
                PokeBuilder.Both(s =>
                {
                    CatEar(b, head, c + V(0.02f * s, 0.025f, -0.03f), c + V(0.05f * s, 0.07f, -0.07f), 0.014f, black, black);
                    var at = On(c, r, 0.02f * s, c.Y + 0.014f);
                    b.Eye(head, at, Outward(c, r, at), 0.008f, green, glare: true);
                });
                return b;
            }
            case 2:
            {
                // The Complete Forme: a giant, two capes of black from its back
                var b = new PokeBuilder("Zygarde-Complete", 1f, BodyPlan.Biped, V(0, 0.48f, 0)) { Coat = Scales };
                PokeBuilder.Both(s =>
                {
                    int leg = b.Leg(s, V(0.07f * s, 0.36f, 0));
                    var knee = V(0.09f * s, 0.2f, 0.03f);
                    b.Limb(leg, V(0.07f * s, 0.37f, 0), knee, 0.045f, 0.035f, black);
                    b.Limb(leg, knee, V(0.1f * s, 0.04f, 0.0f), 0.035f, 0.028f, black);
                    b.Ell(leg, V(0.1f * s, 0.03f, 0.02f), V(0.035f, 0.03f, 0.06f), black);
                    b.PaintEll(leg, knee, V(0.03f, 0.03f, 0.03f), green);
                    var shoulder = V(0.09f * s, 0.6f, 0);
                    int arm = b.Arm(s, shoulder);
                    var elbow = shoulder + V(0.06f * s, -0.12f, 0.03f);
                    b.Limb(arm, shoulder, elbow, 0.035f, 0.028f, black);
                    b.Limb(arm, elbow, elbow + V(0.02f * s, -0.12f, 0.04f), 0.028f, 0.024f, black);
                    b.PaintEll(arm, shoulder, V(0.04f, 0.04f, 0.04f), green);
                    Claws(b, arm, elbow + V(0.02f * s, -0.14f, 0.05f), V(0.01f, 0, 0), V(0, -1f, 0.2f), 0.025f, 0.007f);
                    // A great cape of black from the back, marked red and green
                    int cape = b.Part(s < 0 ? "capeL" : "capeR", Body, V(0.04f * s, 0.6f, -0.04f), PokeRole.Wing, 0.2f, s);
                    var path = Smooth(3, V(0.04f * s, 0.6f, -0.04f), V(0.2f * s, 0.8f, -0.1f), V(0.3f * s, 0.6f, -0.12f), V(0.32f * s, 0.3f, -0.12f), V(0.28f * s, 0.08f, -0.1f));
                    Ribbon(b, cape, path, 0.1f, black, V(0, 0, 1f), 0.016f);
                    b.PaintEll(cape, path[5], V(0.05f, 0.06f, 0.05f), Rgb(200, 50, 50));
                    b.PaintEll(cape, path[8], V(0.04f, 0.05f, 0.05f), green);
                });
                var bc = V(0, 0.5f, 0);
                var br = V(0.1f, 0.16f, 0.08f);
                b.Ell(Body, bc, br, black);
                foreach (var (x, y) in new[] { (0f, 0.08f), (-0.04f, 0.03f), (0.04f, 0.03f), (0f, -0.02f) })
                    HexCell(b, Body, Out(bc, br, default, V(x / br.X, y / br.Y, 1f)) - V(0, 0, 0.004f), V(0, 0, 1f), 0.02f, 0.006f, white);
                int head = b.Head(bc + V(0, br.Y, 0.01f));
                var c = bc + V(0, br.Y + 0.06f, 0.02f);
                var r = V(0.045f, 0.05f, 0.05f);
                b.Ell(head, c, r, black);
                b.Spike(head, c + V(0, 0.03f, -0.01f), c + V(0, 0.13f, -0.04f), 0.03f, black, 0.4f);
                PokeBuilder.Both(s =>
                {
                    b.Spike(head, c + V(0.03f * s, 0.02f, -0.01f), c + V(0.09f * s, 0.08f, -0.04f), 0.02f, black, 0.4f);
                    var at = On(c, r, 0.02f * s, c.Y + 0.004f);
                    b.Eye(head, at, Outward(c, r, at), 0.01f, cyan, glare: true);
                });
                return b;
            }
            case 3:
            {
                // Mega Zygarde: its core hanging under a vast black cylinder
                var b = new PokeBuilder("Zygarde-Mega", 1f, BodyPlan.Floating, V(0, 0.5f, 0)) { Coat = Scales }.Hover();
                var top = V(0, 0.72f, 0);
                b.Limb(Body, top + V(0, 0, -0.32f), top + V(0, 0, 0.28f), 0.15f, 0.16f, black, Shell, 0f);
                foreach (var z in new[] { -0.2f, -0.06f, 0.08f, 0.2f })
                    b.PaintTorus(Body, top + V(0, 0, z), 0.155f, 0.012f, Rgb(200, 40, 50), V(90f, 0, 0));
                b.Mark(Body, top + V(0, 0, 0.44f), V(0, 0, 1f), 0.09f, 0.012f, Rgb(160, 110, 220), MarkShape.Bar, 45f);
                b.Mark(Body, top + V(0, 0.001f, 0.44f), V(0, 0, 1f), 0.09f, 0.012f, Rgb(160, 110, 220), MarkShape.Bar, -45f);
                // The core: a slender green figure hanging under it
                int head = b.Head(top + V(0, -0.15f, 0.08f));
                var c = top + V(0, -0.18f, 0.1f);
                var r = V(0.035f, 0.035f, 0.035f);
                b.Limb(head, top + V(0, -0.12f, 0.08f), c, 0.02f, 0.02f, green);
                b.Ell(head, c, r, green, mat: Glow);
                var spine = Smooth(3, c + V(0, -0.02f, -0.01f), V(0, 0.38f, 0.08f), V(0, 0.22f, 0.06f), V(0, 0.08f, 0.08f));
                b.Tube(Body, spine, 0.03f, 0.012f, green, Glow, 0f);
                for (int i = 2; i < spine.Length - 2; i += 2)
                    b.PaintTorus(Body, spine[i], 0.026f, 0.004f, black, Euler(spine[i + 1] - spine[i - 1]));
                PokeBuilder.Both(s =>
                {
                    int arm = b.Arm(s, V(0.02f * s, 0.46f, 0.09f));
                    b.Tube(arm, Smooth(3, V(0.02f * s, 0.46f, 0.09f), V(0.1f * s, 0.42f, 0.1f), V(0.14f * s, 0.32f, 0.12f)), 0.012f, 0.008f, green, Glow, 0f);
                    int leg = b.Leg(s, V(0.02f * s, 0.26f, 0.07f));
                    b.Tube(leg, Smooth(3, V(0.02f * s, 0.26f, 0.07f), V(0.05f * s, 0.16f, 0.08f), V(0.04f * s, 0.06f, 0.06f)), 0.014f, 0.008f, green, Glow, 0f);
                    var at = On(c, r, 0.014f * s, c.Y + 0.004f);
                    b.Eye(head, at, Outward(c, r, at), 0.007f, white, glare: true);
                });
                return Lift(b);
            }
            default:
            {
                // The 50% Forme: a serpent coiled on the ground, rising to a hood of fins
                var coil = Spiral3(0.16f, 0.1f, 0.05f, 0.1f, 2.6f, 1.1f, 16);
                var points = new[] { V(0.08f, 0.14f, -0.24f), V(0.14f, 0.08f, -0.2f) }.Concat(coil)
                    .Concat(new[] { V(0, 0.16f, 0.06f), V(0, 0.28f, 0.0f), V(0, 0.4f, 0.04f), V(0, 0.5f, 0.1f) }).ToArray();
                var b = new PokeBuilder("Zygarde", 1f, BodyPlan.Serpent, points[0]) { Coat = Scales };
                int neck = Coils(b, points, t => 0.022f + 0.04f * MathF.Min(1f, t * 2f), black);
                int tail = b.Tail(points[0]);
                b.Ell(tail, points[0], V(0.03f, 0.03f, 0.03f), green, blend: 0.01f);
                foreach (float a in new[] { -40f, 0f, 40f })
                    Blade(b, tail, points[0], points[0] + V(MathF.Sin(a * Degree) * 0.06f, 0.08f, -0.03f), 0.022f, green, V(0, 0, 1f), 0.25f);
                for (int i = 4; i < coil.Length; i += 4)
                    b.PaintEll(1 + (2 + i) / 2, points[2 + i] + V(0, 0.04f, 0), V(0.02f, 0.02f, 0.02f), green);
                int head = b.Head(points[^1], neck);
                var c = points[^1] + V(0, 0.02f, 0.03f);
                var r = V(0.04f, 0.04f, 0.055f);
                b.Ell(head, c, r, black);
                // The hood: fins fanned behind the head, each with a cell of green
                for (int i = 0; i < 7; i++)
                {
                    float a = (i / 6f - 0.5f) * 200f * Degree;
                    var d = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a) * 0.8f + 0.2f, -0.4f));
                    var root = c + V(0, 0, -0.02f);
                    Frond(b, head, root, root + d * 0.16f, 0.03f, black, V(0, 0.2f, 1f), 0.22f);
                    b.PaintEll(head, root + d * 0.12f, V(0.014f, 0.014f, 0.014f), green);
                }
                PokeBuilder.Both(s =>
                {
                    var at = On(c, r, 0.02f * s, c.Y + 0.008f);
                    b.Eye(head, at, Outward(c, r, at), 0.009f, green, glare: true);
                });
                return b;
            }
        }
    }

    private static PokeBuilder Zygarde() => ZygardeBuild(0);

    // ------------------------------------------------------------------ Diancie

    /// <summary>
    /// Diancie and its Mega Evolution: a princess of jewels, a great pink diamond in her crown framed by pink crystals,
    /// a gray face with red eyes and a white gown, standing on a rock studded with pink. Mega Diancie floats in a gown
    /// whose skirt is pink crystal, long white veils hanging from her shoulders and her crown's diamond grown greater.
    /// </summary>
    private static PokeBuilder DiancieBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Diancie-Mega" : "Diancie", mega ? 1f : 0.65f, mega ? BodyPlan.Floating : BodyPlan.Biped, V(0, mega ? 0.44f : 0.24f, 0)) { Coat = Shell };
        if (mega) b.Hover();
        var pink = Rgb(246, 170, 196);
        var deep = Rgb(220, 110, 150);
        var gray = Rgb(150, 146, 160);
        var white = Rgb(246, 244, 248);
        var rock = Rgb(120, 116, 128);
        float y0 = mega ? 0.2f : 0f;
        if (mega)
        {
            // A skirt of pink crystal, points hanging below
            for (int i = 0; i < 10; i++)
            {
                float a = i * MathF.Tau / 10f;
                var d = V(MathF.Sin(a), 0, MathF.Cos(a));
                Blade(b, Body, V(0, 0.36f, 0) + d * 0.04f, V(0, 0.08f, 0) + d * 0.1f, 0.05f, i % 2 == 0 ? pink : deep, d, 0.3f, Glow);
            }
            PokeBuilder.Both(s =>
            {
                int veil = b.Part(s < 0 ? "veilL" : "veilR", Body, V(0.035f * s, 0.47f, -0.01f), PokeRole.Wing, 0.3f, s);
                Ribbon(b, veil, Smooth(3, V(0.035f * s, 0.47f, -0.01f), V(0.14f * s, 0.46f, -0.04f), V(0.19f * s, 0.3f, -0.05f), V(0.2f * s, 0.08f, -0.04f)), 0.08f, white, V(0, 0, 1f), 0.005f);
            });
        }
        else
        {
            // The rock she stands on, studded with pink crystals
            b.Ell(Body, V(0, 0.06f, 0), V(0.09f, 0.06f, 0.08f), rock, mat: Shell);
            foreach (var (at, to) in new[] { (V(0.06f, 0.06f, 0.04f), V(0.11f, 0.08f, 0.07f)), (V(-0.07f, 0.05f, 0.02f), V(-0.12f, 0.06f, 0.04f)), (V(0.02f, 0.04f, -0.07f), V(0.03f, 0.05f, -0.12f)) })
                b.Spike(Body, at, to, 0.02f, pink, 0.6f, Glow);
        }
        // The white gown and body
        b.Ell(Body, V(0, 0.18f + y0, 0), V(0.07f, 0.07f, 0.06f), white);
        foreach (var (x, z) in new[] { (-0.05f, 0.02f), (0.05f, 0.02f), (0f, 0.05f), (0f, -0.04f) })
            b.Ell(Body, V(x, 0.15f + y0, z), V(0.035f, 0.035f, 0.035f), white, blend: 0.02f);
        b.Ell(Body, V(0, 0.26f + y0, 0), V(0.04f, 0.04f, 0.035f), white, blend: 0.02f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.035f * s, 0.27f + y0, 0));
            b.Limb(arm, V(0.035f * s, 0.27f + y0, 0), V(0.09f * s, 0.24f + y0, 0.03f), 0.012f, 0.009f, gray);
            b.Ell(arm, V(0.095f * s, 0.235f + y0, 0.035f), V(0.012f, 0.01f, 0.012f), gray, blend: 0.006f);
        });
        int head = b.Head(V(0, 0.3f + y0, 0.01f));
        var c = V(0, 0.34f + y0, 0.015f);
        var r = V(0.04f, 0.04f, 0.036f);
        b.Ell(head, c, r, gray);
        // The crown: a great pink diamond on the brow, pink crystals fanned round
        float k = mega ? 1.4f : 1f;
        Gem(b, head, c + V(0, 0.05f * k, 0.01f), V(0, 0, 1f), 0.035f * k, pink);
        for (int i = 0; i < 7; i++)
        {
            float a = (i / 6f - 0.5f) * 200f * Degree;
            var d = V(MathF.Sin(a), MathF.Cos(a), -0.15f);
            Blade(b, head, c + d * 0.03f + V(0, 0.01f, -0.01f), c + d * (0.1f + 0.03f * MathF.Cos(a)) * k + V(0, 0.01f, -0.02f), 0.025f, i % 2 == 0 ? pink : deep, V(0, 0, 1f), 0.3f, Glow);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.017f * s, c.Y - 0.004f);
            b.Eye(head, at, Outward(c, r, at), 0.01f, Rgb(220, 50, 60), glare: false);
        });
        return mega ? Lift(b) : b;
    }

    private static PokeBuilder Diancie() => DiancieBuild(false);

    // ------------------------------------------------------------------ Hoopa

    /// <summary>
    /// Hoopa, Confined or Unbound. Confined, a little floating imp, gray-lilac with a big head, pink horns curling and
    /// ringed gold, green eyes over a wide grin, a pink jewel on its brow and a great gold ring held to one side.
    /// Unbound, a towering djinn: a pink and purple body over a coiling gray tail, a dark hollow in its middle, a mane of
    /// pink horns, two great arms banded gold and four more hands floating free about it, each in a ring of gold.
    /// </summary>
    private static PokeBuilder HoopaBuild(bool unbound)
    {
        var b = new PokeBuilder(unbound ? "Hoopa-Unbound" : "Hoopa", unbound ? 1f : 0.5f, BodyPlan.Floating, V(0, unbound ? 0.46f : 0.2f, 0)) { Coat = Fur }.Hover();
        var lilac = Rgb(170, 166, 196);
        var pink = Rgb(226, 70, 130);
        var purple = Rgb(130, 80, 150);
        var gold = Rgb(240, 200, 70);
        var green = Rgb(140, 220, 90);
        if (!unbound)
        {
            // A little body tapering to two small feet, the head much larger
            b.Ell(Body, V(0, 0.17f, 0), V(0.04f, 0.05f, 0.035f), lilac);
            b.PaintEll(Body, V(0, 0.15f, 0.02f), V(0.04f, 0.015f, 0.03f), pink);
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.02f * s, 0.13f, 0));
                b.Limb(leg, V(0.02f * s, 0.13f, 0), V(0.035f * s, 0.07f, 0.02f), 0.014f, 0.012f, lilac);
                int arm = b.Arm(s, V(0.04f * s, 0.2f, 0));
                var hand = V(0.1f * s, 0.2f, 0.03f);
                b.Limb(arm, V(0.04f * s, 0.2f, 0), hand, 0.012f, 0.011f, lilac);
                b.Torus(arm, Vector3.Lerp(V(0.04f * s, 0.2f, 0), hand, 0.6f), 0.014f, 0.004f, gold, V(0, 0, 90f), mat: Metal, blend: 0.003f);
                b.Ell(arm, hand, V(0.016f, 0.016f, 0.016f), lilac);
                if (s > 0) b.Torus(arm, hand + V(0.07f, 0.04f, 0), 0.07f, 0.008f, gold, V(90f, 0, 0), mat: Metal, blend: 0.004f);
            });
            int head = b.Head(V(0, 0.22f, 0.01f));
            var c = V(0, 0.28f, 0.015f);
            var r = V(0.065f, 0.055f, 0.055f);
            b.Ell(head, c, r, lilac);
            Grin(b, head, On(c, r, 0, c.Y - 0.022f) - V(0, 0, 0.012f), V(0.03f, 0.012f, 0.015f), Rgb(80, 40, 70));
            b.Mark(head, On(c, r, 0, c.Y + 0.032f), V(0, 0.5f, 1f), 0.012f, 0.012f, pink);
            PokeBuilder.Both(s =>
            {
                int ear = b.Ear(head, s, c + V(0.04f * s, 0.04f, -0.01f));
                b.Tube(ear, Smooth(3, c + V(0.04f * s, 0.04f, -0.01f), c + V(0.09f * s, 0.08f, -0.02f), c + V(0.13f * s, 0.05f, -0.01f), c + V(0.13f * s, 0.0f, 0.01f)), 0.022f, 0.01f, pink, blend: 0f);
                b.Torus(ear, c + V(0.11f * s, 0.07f, -0.015f), 0.018f, 0.006f, gold, V(0, 0, 60f * s), mat: Metal, blend: 0.004f);
                var at = On(c, r, 0.025f * s, c.Y + 0.008f);
                b.Eye(head, at, Outward(c, r, at), 0.012f, green, sclera: true, white: Rgb(250, 230, 120));
            });
            return Lift(b);
        }
        // Unbound: a tail coiling under it, a pink and purple body with a dark hollow in its middle
        var tail = Smooth(3, V(0, 0.36f, 0), V(0.05f, 0.24f, 0.0f), V(0.0f, 0.14f, -0.06f), V(-0.08f, 0.1f, -0.02f), V(-0.12f, 0.06f, 0.06f), V(-0.06f, 0.02f, 0.1f));
        b.Tube(Body, tail, 0.08f, 0.015f, lilac, blend: 0f);
        var bc = V(0, 0.48f, 0);
        var br = V(0.11f, 0.13f, 0.09f);
        b.Ell(Body, bc, br, purple);
        b.PaintEll(Body, bc + V(0, 0.06f, 0.02f), V(0.12f, 0.08f, 0.09f), pink);
        b.Mark(Body, Out(bc, br, default, V(0, -0.15f, 1f)), V(0, -0.1f, 1f), 0.035f, 0.035f, Rgb(30, 24, 36));
        PokeBuilder.Both(s =>
        {
            // Two great arms banded gold
            var shoulder = bc + V(0.1f * s, 0.07f, 0);
            int arm = b.Arm(s, shoulder);
            var elbow = shoulder + V(0.1f * s, -0.04f, 0.04f);
            var hand = elbow + V(0.06f * s, 0.08f, 0.06f);
            b.Limb(arm, shoulder, elbow, 0.04f, 0.03f, purple);
            b.Limb(arm, elbow, hand, 0.03f, 0.028f, lilac);
            b.Torus(arm, Vector3.Lerp(elbow, hand, 0.5f), 0.034f, 0.008f, gold, Euler(hand - elbow), mat: Metal, blend: 0.004f);
            b.Ell(arm, hand, V(0.035f, 0.035f, 0.03f), lilac);
            Claws(b, arm, hand + V(0, 0.03f, 0.02f), V(0.012f, 0, 0), V(0, 1f, 0.3f), 0.025f, 0.008f);
            // Two more hands floating free beside it, each in its ring of gold
            foreach (var (at, k) in new[] { (bc + V(0.28f * s, 0.18f, -0.04f), 0), (bc + V(0.26f * s, -0.12f, 0.02f), 1) })
            {
                int free = b.Part("hand" + k + (s < 0 ? "L" : "R"), Body, at, PokeRole.Arm, 0.5f + k * 0.4f, s);
                b.Ell(free, at, V(0.03f, 0.03f, 0.026f), lilac);
                b.Limb(free, at, at + V(-0.03f * s, -0.02f, 0), 0.02f, 0.016f, lilac);
                Claws(b, free, at + V(0.01f * s, 0.025f, 0.015f), V(0.01f, 0, 0), V(0.2f * s, 1f, 0.3f), 0.02f, 0.007f);
                b.Torus(free, at + V(-0.03f * s, -0.02f, 0), 0.02f, 0.006f, gold, V(0, 0, 90f + 30f * s), mat: Metal, blend: 0.004f);
            }
        });
        int head2 = b.Head(bc + V(0, br.Y * 0.9f, 0.02f));
        var hc = bc + V(0, br.Y + 0.05f, 0.04f);
        var hr = V(0.06f, 0.055f, 0.055f);
        b.Ell(head2, hc, hr, purple);
        Grin(b, head2, On(hc, hr, 0, hc.Y - 0.022f) - V(0, 0, 0.012f), V(0.035f, 0.014f, 0.016f), Rgb(60, 20, 40));
        foreach (float x in new[] { -0.02f, 0f, 0.02f })
            b.Spike(head2, On(hc, hr, x, hc.Y - 0.012f) - V(0, 0, 0.006f), On(hc, hr, x, hc.Y - 0.012f) + V(0, -0.012f, 0.002f), 0.006f, White, mat: Shell, blend: 0.003f);
        // A mane of pink horns, curving back and out
        for (int i = 0; i < 7; i++)
        {
            float a = (i / 6f - 0.5f) * 220f * Degree;
            var d = Vector3.Normalize(V(MathF.Sin(a), MathF.Cos(a) * 0.7f + 0.4f, -0.5f));
            b.Spike(head2, hc + d * 0.04f, hc + d * (0.16f + 0.04f * MathF.Cos(a)), 0.03f, i % 2 == 0 ? pink : purple, 0.5f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(hc, hr, 0.025f * s, hc.Y + 0.01f);
            b.Eye(head2, at, Outward(hc, hr, at), 0.012f, green, sclera: true, white: Rgb(250, 230, 120), glare: true);
        });
        return Lift(b);
    }

    private static PokeBuilder Hoopa() => HoopaBuild(false);

    // ------------------------------------------------------------------ Volcanion

    /// <summary>Volcanion: a squat red beast armoured like a kiln, a ring of two great arms arched over its back studded with blue lights, a tan mask over its face and chest, gold points at its shoulders and stout clawed legs.</summary>
    private static PokeBuilder Volcanion()
    {
        var b = new PokeBuilder("Volcanion", 1f, BodyPlan.Quadruped, V(0, 0.3f, -0.02f)) { Coat = Shell };
        var red = Rgb(170, 50, 50);
        var dark = Rgb(110, 36, 40);
        var tan = Rgb(220, 186, 130);
        var cyan = Rgb(90, 210, 230);
        var gold = Rgb(240, 196, 70);
        BeastLegs(b, 0.11f, 0.24f, 0.12f, -0.14f, 0.065f, red, tan, dark);
        var bc = V(0, 0.3f, -0.02f);
        var br = V(0.16f, 0.13f, 0.19f);
        b.Ell(Body, bc, br, red);
        b.PaintEll(Body, bc + V(0, 0.08f, 0), V(0.15f, 0.06f, 0.18f), dark);
        b.PaintEll(Body, bc + V(0, -0.02f, 0.15f), V(0.1f, 0.1f, 0.05f), tan);
        PokeBuilder.Both(s => b.Spike(Body, bc + V(0.13f * s, 0.06f, 0.08f), bc + V(0.2f * s, 0.12f, 0.1f), 0.03f, gold, 0.5f, Shell));
        // The ring of its arms, arched over its back, studded with blue lights
        var rc = bc + V(0, 0.22f, -0.04f);
        float ring = 0.2f;
        b.Torus(Body, rc, ring, 0.04f, red, V(90f, 0, 0), mat: Shell, blend: 0.02f);
        for (int i = 0; i < 10; i++)
        {
            float a = (i / 9f) * MathF.PI * 1.1f - MathF.PI * 0.05f;
            var at = rc + V(MathF.Cos(a), MathF.Sin(a), 0) * ring + V(0, 0, 0.035f);
            b.Ell(Body, at, V(0.017f, 0.017f, 0.012f), cyan, mat: Glow, blend: 0.006f);
        }
        int tail = b.Tail(bc + V(0, 0.02f, -0.18f));
        b.Spike(tail, bc + V(0, 0.02f, -0.17f), bc + V(0, -0.04f, -0.3f), 0.05f, red, 0.6f, Shell);
        int head = b.Head(bc + V(0, 0.06f, 0.17f));
        var c = bc + V(0, 0.08f, 0.23f);
        var r = V(0.07f, 0.06f, 0.07f);
        b.Ell(head, c, r, red);
        b.PaintEll(head, c + V(0, -0.02f, 0.05f), V(0.05f, 0.03f, 0.03f), tan);
        b.Spike(head, c + V(0, 0.04f, 0.0f), c + V(0, 0.09f, -0.06f), 0.03f, dark, 0.5f, Shell);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.03f * s, c.Y + 0.012f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, gold, glare: true);
        });
        return b;
    }
}
