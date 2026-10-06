using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Johto's first batch in National Pokédex
// order: Chikorita (152) to Corsola (222), but for the species of the Sinnoh Pokédex in that range, hand-built before.
// Their forms are in PokemonModels.Megas.cs and PokemonModels.Regional.cs with the other forms. Helpers shared with the
// earlier batches are in PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs and PokemonModels.Kanto1.cs to
// PokemonModels.Kanto3.cs.
internal static partial class PokemonModels
{
    /// <summary>Four short legs under a body, each ending in a round foot: front pair at <paramref name="front"/>, back pair at <paramref name="back"/>.</summary>
    private static void StubbyLegs(PokeBuilder b, float x, float hip, float front, float back, float r, Color color, Color? foot = null)
    {
        foreach (var (z, isFront) in new[] { (front, true), (back, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(x * s, hip, z), isFront);
                b.Limb(leg, V(x * s, hip, z), V(x * 1.08f * s, r * 0.9f, z + 0.01f), r, r * 0.85f, color);
                b.Ell(leg, V(x * 1.08f * s, r * 0.55f, z + r * 0.45f), V(r * 0.95f, r * 0.55f, r * 1.2f), foot ?? color);
            });
    }

    /// <summary>An antenna from <paramref name="root"/> by <paramref name="mid"/> to a knob at <paramref name="tip"/>.</summary>
    private static void Antenna(PokeBuilder b, int bone, Vector3 root, Vector3 mid, Vector3 tip, float r, Color color, Color knob, float knobR, SurfaceMaterial? mat = null)
    {
        b.Tube(bone, Smooth(3, root, mid, tip), r, r * 0.85f, color, blend: 0f);
        b.Ell(bone, tip, V(knobR, knobR, knobR), knob, mat: mat, blend: 0.006f);
    }

    // ------------------------------------------------------------------ Chikorita line

    private static PokeBuilder Chikorita()
    {
        var b = new PokeBuilder("Chikorita", 0.5f, BodyPlan.Quadruped, V(0, 0.15f, 0)) { Coat = Fur };
        var green = Rgb(186, 226, 142);
        var leaf = Rgb(98, 170, 82);
        StubbyLegs(b, 0.06f, 0.12f, 0.05f, -0.08f, 0.032f, green);
        b.Ell(Body, V(0, 0.15f, -0.01f), V(0.09f, 0.085f, 0.11f), green);
        // A ring of buds round its neck
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            b.Ell(Body, V(MathF.Sin(a) * 0.08f, 0.2f, 0.045f + MathF.Cos(a) * 0.07f), V(0.017f, 0.017f, 0.017f), Rgb(76, 146, 70), blend: 0.006f);
        }
        int head = b.Head(V(0, 0.2f, 0.06f));
        var c = V(0, 0.28f, 0.08f);
        var r = V(0.115f, 0.105f, 0.105f);
        b.Ell(head, c, r, green);
        // The great leaf on its head, rising and curving back
        int sprout = b.Part("leaf", head, c + V(0, r.Y, 0), PokeRole.Leaf);
        b.Limb(sprout, c + V(0, r.Y - 0.01f, -0.01f), c + V(0, r.Y + 0.05f, -0.02f), 0.012f, 0.01f, leaf);
        PointedLeaf(b, sprout, c + V(0, r.Y + 0.04f, -0.02f), c + V(0.02f, r.Y + 0.27f, -0.13f), 0.085f, leaf, V(0.6f, 0.3f, 0.8f), 0.2f);
        b.Mark(head, Out(c, r, default, V(0, -0.25f, 1f)), V(0, -0.25f, 1f), 0.02f, 0.006f, Rgb(80, 120, 60), MarkShape.Smile);
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.03f, Rgb(176, 52, 52));
        });
        return b;
    }

    private static PokeBuilder Bayleef()
    {
        var b = new PokeBuilder("Bayleef", 0.7f, BodyPlan.Quadruped, V(0, 0.25f, -0.03f)) { Coat = Fur };
        var cream = Rgb(240, 222, 150);
        var leaf = Rgb(106, 168, 80);
        StubbyLegs(b, 0.08f, 0.2f, 0.08f, -0.14f, 0.045f, cream);
        b.Ell(Body, V(0, 0.25f, -0.03f), V(0.12f, 0.11f, 0.17f), cream);
        // A necklace of rolled leaves round the root of its neck
        for (int i = 0; i < 10; i++)
        {
            float a = i * MathF.Tau / 10f;
            var o = V(MathF.Sin(a), -0.35f, MathF.Cos(a) * 0.9f);
            var at = V(0, 0.32f, 0.09f) + V(o.X * 0.075f, 0, o.Z * 0.07f);
            b.Ell(Body, at + Vector3.Normalize(o) * 0.01f, V(0.022f, 0.045f, 0.022f), leaf, Euler(o), Leaf, 0.008f);
        }
        int head = b.Head(V(0, 0.36f, 0.12f));
        b.Tube(head, Smooth(3, V(0, 0.3f, 0.1f), V(0, 0.42f, 0.15f), V(0, 0.5f, 0.17f)), 0.045f, 0.04f, cream, blend: 0f);
        var c = V(0, 0.55f, 0.19f);
        var r = V(0.08f, 0.075f, 0.09f);
        b.Ell(head, c, r, cream);
        // A leaf rolled into a hook on its head
        int sprout = b.Part("leaf", head, c + V(0, r.Y, 0), PokeRole.Leaf);
        PointedLeaf(b, sprout, c + V(0, r.Y - 0.01f, 0.0f), c + V(0, r.Y + 0.13f, -0.06f), 0.05f, leaf, V(1f, 0.2f, 0.2f), 0.28f);
        Frond(b, sprout, c + V(0, r.Y + 0.12f, -0.05f), c + V(0, r.Y + 0.08f, -0.13f), 0.035f, leaf, V(1f, 0, 0), 0.4f, Leaf);
        PokeBuilder.Both(s =>
        {
            var look = V(0.5f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.024f, Rgb(176, 52, 52));
        });
        int tail = b.Tail(V(0, 0.25f, -0.18f));
        b.Spike(tail, V(0, 0.25f, -0.17f), V(0, 0.2f, -0.29f), 0.04f, cream);
        return b;
    }

    /// <summary>
    /// Meganium and its Mega Evolution: a long-necked green beast with a great pink flower round its neck and two
    /// feelers on its head; the Mega's flower doubled into a red heart in a pink ring tipped white, its feelers ringed.
    /// </summary>
    private static PokeBuilder MeganiumBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Meganium-Mega" : "Meganium", mega ? 1f : 0.92f, BodyPlan.Quadruped, V(0, 0.3f, -0.06f)) { Coat = Fur };
        var green = Rgb(150, 206, 112);
        var pink = Rgb(244, 132, 152);
        var red = Rgb(222, 52, 64);
        var yellow = Rgb(250, 216, 80);
        StubbyLegs(b, 0.1f, 0.24f, 0.08f, -0.2f, 0.06f, green, mega ? Rgb(90, 160, 80) : null);
        b.Ell(Body, V(0, 0.3f, -0.06f), V(0.15f, 0.14f, 0.23f), green);
        int tail = b.Tail(V(0, 0.3f, -0.27f));
        b.Spike(tail, V(0, 0.3f, -0.26f), V(0, 0.24f, mega ? -0.48f : -0.4f), 0.05f, green);
        // The flower round its neck: a ring of petals, the Mega's in two layers
        var fc = V(0, 0.44f, 0.11f);
        void Ring(int count, float inner, float outer, float width, float lift, Color color, Color edge, float turn)
        {
            for (int i = 0; i < count; i++)
            {
                float a = (i + turn) * MathF.Tau / count;
                var o = V(MathF.Sin(a), 0, MathF.Cos(a));
                var from = fc + o * inner + V(0, lift, 0);
                var to = fc + o * outer + V(0, lift - 0.04f, 0);
                Frond(b, Body, from, to, width, color, Vector3.Normalize(V(0, 1f, 0) - o * 0.3f), 0.2f, Leaf, 0.01f);
                b.PaintEll(Body, to - o * 0.015f, V(width * 1.1f, 0.03f, width * 1.1f), edge, Euler(to - from));
            }
        }
        if (mega)
        {
            Ring(12, 0.06f, 0.4f, 0.1f, -0.02f, pink, White, 0.5f);
            Ring(10, 0.05f, 0.27f, 0.095f, 0.03f, red, pink, 0f);
        }
        else Ring(9, 0.05f, 0.3f, 0.1f, 0f, pink, White, 0f);
        foreach (float a in new[] { -0.5f, 0f, 0.5f })
            b.Spike(Body, fc + V(0, 0.02f, 0.06f), fc + V(MathF.Sin(a) * 0.05f, 0.06f, 0.1f), 0.012f, yellow, blend: 0.004f);
        // A long neck, a small head, two feelers
        int head = b.Head(V(0, 0.48f, 0.14f));
        float top = mega ? 0.8f : 0.86f;
        b.Tube(head, Smooth(3, V(0, 0.38f, 0.1f), V(0, 0.6f, 0.18f), V(0, top - 0.05f, 0.19f)), 0.055f, 0.045f, green, blend: 0f);
        var c = V(0, top, 0.22f);
        var r = V(0.075f, 0.07f, 0.085f);
        b.Ell(head, c, r, green);
        PokeBuilder.Both(s =>
        {
            var root = c + V(0.025f * s, r.Y * 0.8f, -0.01f);
            if (mega)
            {
                b.Tube(head, Smooth(3, root, root + V(0.03f * s, 0.07f, 0), root + V(0.05f * s, 0.12f, 0.0f)), 0.008f, 0.007f, Rgb(70, 130, 60), blend: 0f);
                b.Torus(head, root + V(0.07f * s, 0.15f, 0), 0.03f, 0.008f, yellow, V(90f, 0, 0));
            }
            else Antenna(b, head, root, root + V(0.03f * s, 0.08f, 0.01f), root + V(0.05f * s, 0.14f, 0.0f), 0.008f, Rgb(80, 140, 70), yellow, 0.014f);
        });
        PokeBuilder.Both(s =>
        {
            var look = V(0.55f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.022f, Rgb(196, 140, 40));
        });
        return b;
    }

    private static PokeBuilder Meganium() => MeganiumBuild(false);

    // ------------------------------------------------------------------ Cyndaquil line

    /// <summary>Flames bursting from a back: <paramref name="count"/> tongues spread from <paramref name="from"/> to <paramref name="to"/>, pointing along <paramref name="way"/>.</summary>
    private static void FlameRow(PokeBuilder b, int bone, Vector3 from, Vector3 to, Vector3 way, int count, float length, float r, Color rim, Color heart, float fan = 0.6f)
    {
        var d = Vector3.Normalize(way);
        var side = Vector3.Normalize(to - from);
        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : i / (count - 1f);
            var root = Vector3.Lerp(from, to, t);
            var dir = Vector3.Normalize(d + side * (t - 0.5f) * fan);
            float len = length * (0.75f + 0.25f * MathF.Sin(MathF.PI * t));
            FlameTongue(b, bone, root, root + dir * len, r, rim, heart);
        }
    }

    private static PokeBuilder Cyndaquil()
    {
        var b = new PokeBuilder("Cyndaquil", 0.5f, BodyPlan.Biped, V(0, 0.16f, 0)) { Coat = Fur };
        var navy = Rgb(48, 86, 106);
        var cream = Rgb(250, 226, 150);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.09f, 0));
            b.Limb(leg, V(0.05f * s, 0.09f, 0), V(0.055f * s, 0.03f, 0.02f), 0.03f, 0.026f, cream);
            b.Ell(leg, V(0.056f * s, 0.018f, 0.035f), V(0.028f, 0.018f, 0.04f), cream);
        });
        b.Ell(Body, V(0, 0.17f, 0), V(0.085f, 0.11f, 0.09f), cream);
        b.PaintEll(Body, V(0, 0.22f, -0.07f), V(0.11f, 0.1f, 0.09f), navy);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.18f, 0.04f));
            b.Limb(arm, V(0.07f * s, 0.18f, 0.04f), V(0.09f * s, 0.13f, 0.08f), 0.018f, 0.016f, cream);
        });
        int head = b.Head(V(0, 0.25f, 0.03f));
        var c = V(0, 0.29f, 0.05f);
        var r = V(0.07f, 0.065f, 0.075f);
        b.Ell(head, c, r, navy);
        b.Ell(head, c + V(0, -0.015f, 0.09f), V(0.04f, 0.033f, 0.075f), cream, V(8f, 0, 0), blend: 0.025f);
        b.PaintEll(head, c + V(0, -0.04f, 0.04f), V(0.075f, 0.035f, 0.08f), cream);
        b.Ell(head, c + V(0, -0.02f, 0.165f), V(0.012f, 0.009f, 0.008f), Rgb(60, 40, 40), mat: Shell, blend: 0.004f);
        // Its eyes are always shut; the fire bursts from its back
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.15f, 0.8f);
            b.Eye(head, Out(c, r, default, look), look, 0.016f, closed: true);
        });
        int flame = b.Part("flame", Body, V(0, 0.24f, -0.08f), PokeRole.Flame);
        FlameRow(b, flame, V(0, 0.27f, -0.06f), V(0, 0.16f, -0.09f), V(0, 0.5f, -1f), 5, 0.15f, 0.035f, Rgb(244, 100, 50), Rgb(255, 214, 90), 1.2f);
        return b;
    }

    private static PokeBuilder Quilava()
    {
        var b = new PokeBuilder("Quilava", 0.72f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var navy = Rgb(48, 86, 106);
        var cream = Rgb(248, 222, 150);
        var rim = Rgb(244, 100, 50);
        var heart = Rgb(255, 214, 90);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.16f, 0));
            b.Limb(leg, V(0.06f * s, 0.17f, 0), V(0.065f * s, 0.04f, 0.02f), 0.04f, 0.03f, cream);
            b.Ell(leg, V(0.066f * s, 0.022f, 0.045f), V(0.032f, 0.022f, 0.055f), cream);
        });
        b.Ell(Body, V(0, 0.3f, 0), V(0.085f, 0.17f, 0.08f), cream);
        b.PaintEll(Body, V(0, 0.36f, -0.08f), V(0.1f, 0.18f, 0.08f), navy);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.36f, 0.03f));
            b.Limb(arm, V(0.07f * s, 0.36f, 0.03f), V(0.085f * s, 0.28f, 0.08f), 0.02f, 0.017f, cream);
        });
        int head = b.Head(V(0, 0.46f, 0.02f));
        var c = V(0, 0.5f, 0.04f);
        var r = V(0.062f, 0.058f, 0.07f);
        b.Ell(head, c, r, navy);
        b.Ell(head, c + V(0, -0.015f, 0.08f), V(0.036f, 0.03f, 0.07f), cream, V(8f, 0, 0), blend: 0.025f);
        b.PaintEll(head, c + V(0, -0.035f, 0.03f), V(0.065f, 0.03f, 0.075f), cream);
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.15f, 0.8f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f, Rgb(176, 40, 40), glare: true);
        });
        // Flames from its brow and its hindquarters
        int flame = b.Part("flame", head, c + V(0, r.Y, -0.02f), PokeRole.Flame);
        FlameRow(b, flame, c + V(0, r.Y * 0.8f, 0.02f), c + V(0, r.Y * 0.5f, -0.05f), V(0, 1f, -0.4f), 3, 0.12f, 0.03f, rim, heart, 0.8f);
        int tail = b.Tail(V(0, 0.22f, -0.08f));
        FlameRow(b, tail, V(0, 0.26f, -0.07f), V(0, 0.16f, -0.08f), V(0, 0.4f, -1f), 4, 0.17f, 0.035f, rim, heart, 1.0f);
        return b;
    }

    /// <summary>
    /// Typhlosion and its Hisuian form: a big cream beast, dark blue over its back, a collar of fire round its neck;
    /// the Hisuian slighter, its fire a ghostly purple mane over its head and a wisp for a tail, red orbs at its collar.
    /// </summary>
    private static PokeBuilder TyphlosionBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Typhlosion-Hisui" : "Typhlosion", 0.95f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var navy = hisui ? Rgb(52, 46, 84) : Rgb(48, 86, 106);
        var cream = Rgb(248, 226, 162);
        var rim = hisui ? Rgb(160, 80, 200) : Rgb(244, 100, 50);
        var heart = hisui ? Rgb(250, 120, 190) : Rgb(255, 214, 90);
        float w = hisui ? 0.85f : 1f;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.2f, 0));
            b.Limb(leg, V(0.09f * s, 0.22f, 0), V(0.1f * s, 0.05f, 0.03f), 0.06f * w, 0.045f, cream);
            b.Ell(leg, V(0.1f * s, 0.028f, 0.06f), V(0.045f, 0.028f, 0.07f), cream);
        });
        var bc = V(0, 0.42f, 0);
        b.Ell(Body, bc, V(0.17f * w, 0.25f, 0.15f * w), cream);
        b.PaintEll(Body, bc + V(0, 0.08f, -0.12f), V(0.2f, 0.25f, 0.11f), navy);
        int tail = b.Tail(V(0, 0.24f, -0.12f));
        if (hisui) FlameRow(b, tail, V(0, 0.26f, -0.12f), V(0, 0.2f, -0.13f), V(0, -0.2f, -1f), 3, 0.24f, 0.035f, rim, heart, 0.6f);
        else b.Spike(tail, V(0, 0.26f, -0.11f), V(0, 0.18f, -0.26f), 0.05f, navy);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.14f * s, 0.52f, 0.04f));
            var hand = V(0.24f * s, 0.46f, 0.12f);
            b.Limb(arm, V(0.13f * s, 0.52f, 0.04f), hand, 0.04f, 0.033f, cream);
            Digits(b, arm, hand, V(0.4f * s, -0.2f, 1f), V(0.4f, 0, 0), 0.03f, 0.009f, Claw, Shell);
        });
        int head = b.Head(V(0, 0.64f, 0.04f));
        var c = V(0, 0.71f, 0.07f);
        var r = V(0.085f, 0.075f, 0.095f);
        b.Ell(head, c, r, navy);
        b.Ell(head, c + V(0, -0.025f, 0.08f), V(0.055f, 0.045f, 0.08f), cream, V(10f, 0, 0), blend: 0.025f);
        b.PaintEll(head, c + V(0, -0.05f, 0.04f), V(0.085f, 0.04f, 0.09f), cream);
        Grin(b, head, c + V(0, -0.05f, 0.15f), V(0.035f, 0.022f, 0.03f), Rgb(170, 60, 70));
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.15f, 0.8f);
            b.Eye(head, Out(c, r, default, look), look, 0.018f, Rgb(176, 40, 40), glare: true);
        });
        // The collar of fire round its neck, or the Hisuian's mane of ghostly flame and its red orbs
        int flame = b.Part("flame", Body, V(0, 0.62f, -0.02f), PokeRole.Flame);
        if (hisui)
        {
            FlameRow(b, flame, c + V(0, r.Y * 0.9f, 0.02f), c + V(0, 0.0f, -0.09f), V(-0.3f, 0.9f, -1f), 6, 0.32f, 0.04f, rim, heart, 0.9f);
            for (int i = 0; i < 7; i++)
            {
                float a = (-90f + i * 30f) * Degree;
                b.Ell(Body, V(MathF.Sin(a) * 0.1f, 0.62f, 0.02f + MathF.Cos(a) * 0.09f), V(0.024f, 0.024f, 0.024f), Rgb(196, 40, 80), mat: Glow, blend: 0.006f);
            }
        }
        else
            for (int i = 0; i < 9; i++)
            {
                float a = (90f + i * 22.5f) * Degree;
                var o = V(MathF.Sin(a), 0, MathF.Cos(a));
                var root = V(0, 0.62f, -0.02f) + o * 0.1f;
                FlameTongue(b, flame, root, root + Vector3.Normalize(o + V(0, 0.9f, 0)) * 0.2f, 0.045f, rim, heart);
            }
        return b;
    }

    private static PokeBuilder Typhlosion() => TyphlosionBuild(false);

    // ------------------------------------------------------------------ Totodile line

    /// <summary>
    /// Totodile's line (stage 0, 1, 2) and Mega Feraligatr: a blue crocodile on two legs, a yellow mark down its chest,
    /// red spikes down its back and tail, its great jaws open; Croconaw and Feraligatr growing a crest of red spines,
    /// the Mega's a sail of red over its head and back and red armour on its arms.
    /// </summary>
    private static PokeBuilder CrocBuild(int stage, bool mega)
    {
        string name = mega ? "Feraligatr-Mega" : stage switch { 0 => "Totodile", 1 => "Croconaw", _ => "Feraligatr" };
        float k = stage switch { 0 => 1f, 1 => 1.35f, _ => 1.85f };
        var b = new PokeBuilder(name, stage switch { 0 => 0.55f, 1 => 0.75f, _ => 1f }, BodyPlan.Biped, V(0, 0.2f * k, 0)) { Coat = Scales };
        var blue = mega ? Rgb(60, 140, 200) : Rgb(90, 172, 214);
        var yellow = Rgb(244, 214, 116);
        var red = Rgb(214, 60, 70);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.11f, 0) * k);
            b.Limb(leg, V(0.06f * s, 0.12f, 0) * k, V(0.07f * s, 0.035f, 0.015f) * k, 0.04f * k, 0.032f * k, blue);
            b.Ell(leg, V(0.07f * s, 0.02f, 0.04f) * k, V(0.035f, 0.02f, 0.05f) * k, blue);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.07f * s + 0.018f * i, 0.016f, 0.08f) * k, V(0.07f * s + 0.022f * i, 0.008f, 0.1f) * k, 0.008f * k, Claw, mat: Shell, blend: 0.003f);
        });
        var bc = V(0, 0.2f, 0) * k;
        b.Ell(Body, bc, (stage == 2 ? V(0.12f, 0.12f, 0.1f) : V(0.1f, 0.12f, 0.09f)) * k, blue);
        b.PaintEll(Body, bc + V(0, 0.01f, 0.07f) * k, V(0.06f, 0.08f, 0.04f) * k, yellow, V(0, 0, 0));
        b.PaintEll(Body, bc + V(0, 0.08f, 0.07f) * k, V(0.065f, 0.02f, 0.04f) * k, yellow);
        // Arms; Feraligatr's ridged, the Mega's in red armour
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.27f, 0.02f) * k);
            var hand = (stage == 2 ? V(0.18f * s, 0.21f, 0.11f) : V(0.15f * s, 0.24f, 0.09f)) * k;
            b.Limb(arm, V(0.08f * s, 0.27f, 0.02f) * k, hand, 0.024f * k, 0.022f * k, blue);
            b.Ell(arm, hand, V(0.026f, 0.024f, 0.026f) * k, blue);
            Digits(b, arm, hand, V(0.3f * s, -0.3f, 1f), V(0.45f, 0, 0), 0.025f * k, 0.007f * k, Claw, Shell);
            if (stage == 2)
                foreach (float t in new[] { 0.35f, 0.6f })
                    b.Torus(arm, Vector3.Lerp(V(0.08f * s, 0.27f, 0.02f) * k, hand, t), 0.024f * k, 0.007f * k, PixelCanvas.Mix(blue, Rgb(20, 40, 80), 0.25f), Euler(hand - V(0.08f * s, 0.27f, 0.02f) * k));
            if (mega)
                b.Spike(arm, Vector3.Lerp(V(0.08f * s, 0.27f, 0.02f) * k, hand, 0.4f), Vector3.Lerp(V(0.08f * s, 0.27f, 0.02f) * k, hand, 0.4f) + V(0.06f * s, 0.05f, -0.04f) * k, 0.02f * k, red, 0.6f, Shell);
        });
        // A tail with red spikes along it
        int tail = b.Tail(V(0, 0.14f, -0.07f) * k);
        var tp = Smooth(3, V(0, 0.15f, -0.07f) * k, V(0, 0.08f, -0.17f) * k, V(0, 0.05f, -0.26f) * k);
        b.Tube(tail, tp, 0.045f * k, 0.018f * k, blue, blend: 0f);
        for (int i = 0; i < 3; i++)
        {
            var at = tp[2 + i * 2];
            Blade(b, tail, at + V(0, 0.015f, 0) * k, at + V(0, 0.06f - 0.01f * i, -0.02f) * k, 0.022f * k, red, V(1f, 0, 0), 0.4f);
        }
        // Spines down its back: a few on Totodile, a crest on its evolutions, a sail on the Mega
        int spines = stage == 0 ? 2 : 4;
        for (int i = 0; i < spines; i++)
        {
            var at = bc + V(0, 0.09f - i * 0.05f, -0.07f) * k;
            Blade(b, Body, at, at + V(0, 0.05f, -0.05f) * k * (mega ? 1.8f : 1f), 0.028f * k, red, V(1f, 0, 0), 0.4f);
        }
        // The head: a long jaw open wide, eyes high on its head, red spines behind
        int head = b.Head(V(0, 0.3f, 0.02f) * k);
        var c = V(0, 0.37f, 0.04f) * k;
        var r = V(0.09f, 0.075f, 0.09f) * k;
        b.Ell(head, c, r, blue);
        b.Ell(head, c + V(0, 0.005f, 0.09f) * k, V(0.065f, 0.035f, 0.075f + 0.02f * stage) * k, blue, blend: 0.025f);
        b.Ell(head, c + V(0, -0.055f, 0.08f) * k, V(0.06f, 0.025f, 0.07f + 0.015f * stage) * k, stage == 0 ? blue : yellow, V(15f, 0, 0), blend: 0.02f);
        b.PaintEll(head, c + V(0, -0.03f, 0.1f) * k, V(0.055f, 0.022f, 0.08f + 0.02f * stage) * k, Rgb(196, 70, 90), soft: 0.008f);
        PokeBuilder.Both(s =>
        {
            foreach (float t in new[] { 0.3f, 0.7f })
                b.Spike(head, c + V(0.04f * s, -0.01f, 0.06f + 0.08f * t) * k, c + V(0.04f * s, -0.035f, 0.06f + 0.08f * t) * k, 0.008f * k, White, mat: Shell, blend: 0.003f);
        });
        int crest = stage == 0 ? 3 : 4;
        for (int i = 0; i < crest; i++)
        {
            var at = c + V(0, r.Y * 0.6f - i * 0.025f, -0.04f - i * 0.03f) * k;
            float h = (stage == 0 ? 0.04f : 0.08f) * (mega ? 1.8f : 1f);
            Blade(b, head, at, at + V(0, h, -0.03f) * k, 0.025f * k, red, V(1f, 0, 0), 0.4f);
        }
        if (mega)
            // The sail: a fan of red over its head and down its back
            for (int i = 0; i < 6; i++)
            {
                float a = (20f - i * 18f) * Degree;
                var root = c + V(0, r.Y * 0.4f, -0.05f) * k;
                Frond(b, head, root, root + V(0, MathF.Cos(a), -1.2f + MathF.Sin(a)) * 0.13f * k, 0.035f * k, red, V(1f, 0, 0), 0.3f, Shell, 0.01f);
            }
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.4f, 0.7f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f * k, Rgb(170, 40, 40), glare: stage > 0);
        });
        return b;
    }

    private static PokeBuilder Totodile() => CrocBuild(0, false);

    private static PokeBuilder Croconaw() => CrocBuild(1, false);

    private static PokeBuilder Feraligatr() => CrocBuild(2, false);

    // ------------------------------------------------------------------ Sentret line

    private static PokeBuilder Sentret()
    {
        var b = new PokeBuilder("Sentret", 0.6f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var brown = Rgb(150, 110, 88);
        var dark = Rgb(96, 66, 52);
        var cream = Rgb(240, 220, 180);
        // It stands up tall on its own great striped tail
        int tail = b.Tail(V(0, 0.28f, -0.04f));
        var tp = Smooth(3, V(0, 0.3f, -0.04f), V(0, 0.2f, -0.06f), V(0, 0.1f, -0.02f), V(0, 0.08f, 0.05f));
        Striped(b, tail, tp, 0.045f, 0.075f, brown, dark, 4);
        b.Ell(tail, V(0, 0.08f, 0.02f), V(0.08f, 0.075f, 0.09f), brown);
        b.PaintTorus(tail, V(0, 0.1f, 0.02f), 0.075f, 0.016f, dark, sz: 1.1f);
        var c = V(0, 0.38f, 0.01f);
        var r = V(0.07f, 0.095f, 0.065f);
        b.Ell(Body, c, r, brown);
        b.PaintEll(Body, On(c, r, 0, 0.36f), V(0.05f, 0.06f, 0.03f), cream);
        b.Mark(Body, On(c, r, 0, 0.36f), V(0, 0, 1f), 0.018f, 0.022f, brown);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.04f * s, 0.3f, 0.02f));
            b.Ell(leg, V(0.045f * s, 0.29f, 0.05f), V(0.022f, 0.018f, 0.03f), brown);
            int arm = b.Arm(s, V(0.06f * s, 0.43f, 0.03f));
            b.Ell(arm, V(0.075f * s, 0.41f, 0.06f), V(0.018f, 0.02f, 0.018f), brown);
        });
        int head = b.Head(V(0, 0.46f, 0.01f));
        var hc = V(0, 0.53f, 0.02f);
        var hr = V(0.075f, 0.065f, 0.068f);
        b.Ell(head, hc, hr, brown);
        b.PaintEll(head, hc + V(0, -0.025f, 0.05f), V(0.04f, 0.025f, 0.03f), cream);
        PokeBuilder.Both(s => MouseEar(b, head, hc + V(0.035f * s, 0.045f, -0.01f), hc + V(0.05f * s, 0.15f, -0.02f), 0.022f, brown, dark));
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.1f, 1f);
            b.Eye(head, Out(hc, hr, default, look), look, 0.014f, Black);
        });
        return b;
    }

    private static PokeBuilder Furret()
    {
        var b = new PokeBuilder("Furret", 0.86f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var tan = Rgb(214, 186, 150);
        var brown = Rgb(130, 94, 72);
        var cream = Rgb(246, 228, 196);
        // A long body standing up, ringed brown, short legs and arms
        var spine = Smooth(3, V(0, 0.07f, -0.04f), V(0, 0.25f, -0.02f), V(0, 0.45f, 0.01f), V(0, 0.56f, 0.03f));
        Striped(b, Body, spine, 0.085f, 0.07f, tan, brown, 5);
        b.PaintEll(Body, V(0, 0.32f, 0.06f), V(0.055f, 0.22f, 0.04f), cream);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.06f, 0));
            b.Ell(leg, V(0.06f * s, 0.025f, 0.03f), V(0.03f, 0.025f, 0.045f), tan);
            int arm = b.Arm(s, V(0.06f * s, 0.42f, 0.04f));
            b.Limb(arm, V(0.06f * s, 0.42f, 0.04f), V(0.08f * s, 0.38f, 0.08f), 0.018f, 0.016f, tan);
        });
        int tail = b.Tail(V(0, 0.1f, -0.1f));
        BushyTail(b, tail, new[] { V(0, 0.1f, -0.1f), V(0, 0.12f, -0.26f), V(0, 0.26f, -0.32f), V(0, 0.36f, -0.25f) }, 0.05f, 0.04f, tan, brown, V(0, 0.4f, -1f));
        int head = b.Head(V(0, 0.58f, 0.03f));
        var c = V(0, 0.63f, 0.05f);
        var r = V(0.07f, 0.06f, 0.07f);
        b.Ell(head, c, r, tan);
        b.PaintEll(head, c + V(0, 0.04f, -0.02f), V(0.025f, 0.05f, 0.08f), brown);
        b.PaintEll(head, c + V(0, -0.03f, 0.05f), V(0.04f, 0.025f, 0.03f), cream);
        PokeBuilder.Both(s => CatEar(b, head, c + V(0.045f * s, 0.035f, -0.01f), c + V(0.065f * s, 0.085f, -0.015f), 0.02f, tan, brown));
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.014f, Rgb(70, 50, 40));
            b.PaintEll(head, Out(c, r, default, V(0.75f * s, -0.1f, 0.6f)), V(0.006f, 0.02f, 0.02f), brown, soft: 0.005f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Ledyba line

    /// <summary>
    /// The ladybugs: Ledyba, a red shell with a cream belly and six arms tipped with white mitts, its face below the
    /// shell's rim; Ledian flying, a red head with great blue eyes, wings out of its shell and four arms in white gloves.
    /// </summary>
    private static PokeBuilder LadybugBuild(bool ledian)
    {
        var b = new PokeBuilder(ledian ? "Ledian" : "Ledyba", ledian ? 0.86f : 0.56f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var red = Rgb(226, 74, 66);
        var cream = Rgb(246, 228, 170);
        var black = Rgb(40, 36, 44);
        var mitt = Rgb(240, 240, 236);
        float k = ledian ? 1.15f : 1f;
        var c = V(0, 0.3f, 0) * k;
        // The shell over its back, a black seam down the middle, a cream belly in front
        b.Ell(Body, c, V(0.15f, 0.16f, 0.13f) * k, red);
        b.PaintEll(Body, c + V(0, -0.02f, 0.09f) * k, V(0.12f, 0.13f, 0.06f) * k, cream);
        b.PaintEll(Body, c + V(0, 0.02f, -0.12f) * k, V(0.008f, 0.16f, 0.05f) * k, black, soft: 0.006f);
        if (!ledian)
            foreach (var (x, y) in new[] { (-0.07f, 0.12f), (0.07f, 0.12f), (-0.1f, 0.0f), (0.1f, 0.0f) })
                b.Mark(Body, Out(c, V(0.15f, 0.16f, 0.13f) * k, default, V(x, y + 0.3f, -1f)), V(x, y + 0.3f, -1f), 0.02f, 0.02f, black);
        // Arms tipped with white mitts: three a side on Ledyba, two on Ledian
        int pairs = ledian ? 2 : 3;
        for (int i = 0; i < pairs; i++)
            PokeBuilder.Both(s =>
            {
                float y = (0.34f - i * 0.09f) * k;
                float rx = 0.15f * MathF.Sqrt(MathF.Max(0f, 1f - MathF.Pow((y / k - 0.3f) / 0.16f, 2))) - 0.025f;
                int arm = b.Part((s < 0 ? "armL" : "armR") + i, Body, V(0.12f * s, y, 0.02f) * k, PokeRole.Arm, i * 0.7f, s);
                var hand = V(0.22f * s, y - 0.01f, 0.08f) * k + V(0, ledian ? 0.04f * (1 - i) : 0, 0);
                b.Limb(arm, V(rx * s, y / k, 0.02f) * k, hand, 0.012f * k, 0.011f * k, black);
                b.Ell(arm, hand, V(0.028f, 0.028f, 0.028f) * k, mitt);
            });
        if (ledian)
        {
            // Legs below, red feet; wings spread from under its shell
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.18f, 0) * k);
                b.Limb(leg, V(0.05f * s, 0.18f, 0) * k, V(0.06f * s, 0.06f, 0.02f) * k, 0.013f, 0.012f, black);
                b.Ell(leg, V(0.062f * s, 0.045f, 0.03f) * k, V(0.024f, 0.035f, 0.024f) * k, red);
                int wing = b.Wing(s, V(0.06f * s, 0.4f, -0.08f) * k);
                Frond(b, wing, V(0.06f * s, 0.4f, -0.09f) * k, V(0.3f * s, 0.46f, -0.16f) * k, 0.08f * k, Rgb(250, 238, 190), V(0, 0.3f, -1f), 0.12f, Shell);
            });
        }
        // The head: below the shell's rim on Ledyba, a red helmet with big blue eyes on Ledian; antennae either way
        int head = b.Head(V(0, ledian ? 0.42f : 0.32f, 0.08f) * k);
        var hc = ledian ? V(0, 0.5f, 0.06f) * k : V(0, 0.3f, 0.1f);
        var hr = ledian ? V(0.08f, 0.075f, 0.08f) * k : V(0.09f, 0.075f, 0.07f);
        b.Ell(head, hc, hr, ledian ? red : cream);
        if (!ledian) b.PaintEll(head, hc + V(0, 0.05f, 0), V(0.1f, 0.04f, 0.09f), red);
        PokeBuilder.Both(s => b.Tube(head, Smooth(3, hc + V(0.03f * s, hr.Y * 0.8f, -0.01f), hc + V(0.06f * s, hr.Y + 0.08f, -0.01f), hc + V(0.08f * s, hr.Y + 0.14f, 0.02f)),
            0.012f, 0.009f, black, blend: 0f));
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.05f, 1f);
            if (ledian) b.Eye(head, Out(hc, hr, default, look), look, 0.03f, Rgb(70, 110, 220));
            else b.Eye(head, Out(hc, hr, default, look), look, 0.03f, sclera: true, pupil: Black);
        });
        return Lift(b);
    }

    private static PokeBuilder Ledyba() => LadybugBuild(false);

    private static PokeBuilder Ledian() => LadybugBuild(true);

    // ------------------------------------------------------------------ Spinarak line

    /// <summary>
    /// The spiders: Spinarak, a round green body with a face on its back, a white horn and six legs banded yellow;
    /// Ariados red, its long legs banded purple and yellow, a white horn and fangs.
    /// </summary>
    private static PokeBuilder SpiderBuild(bool ariados)
    {
        var b = new PokeBuilder(ariados ? "Ariados" : "Spinarak", ariados ? 0.85f : 0.5f, BodyPlan.Quadruped, V(0, 0.15f, 0)) { Coat = Shell };
        float k = ariados ? 1.5f : 1f;
        var body = ariados ? Rgb(210, 60, 72) : Rgb(162, 208, 120);
        var legColor = ariados ? Rgb(150, 110, 190) : Rgb(70, 120, 120);
        var band = Rgb(250, 214, 80);
        var c = V(0, 0.14f, -0.03f) * k;
        var r = V(0.14f, 0.1f, 0.15f) * k;
        b.Ell(Body, c, r, body);
        if (ariados)
        {
            b.Ell(Body, c + V(0, 0.02f, -0.16f) * k, V(0.13f, 0.1f, 0.12f) * k, body, blend: 0.03f);
            b.PaintEll(Body, c + V(0, 0.08f, -0.17f) * k, V(0.1f, 0.06f, 0.1f) * k, Rgb(250, 220, 90));
            b.PaintEll(Body, c + V(0, 0.08f, -0.17f) * k, V(0.04f, 0.07f, 0.1f) * k, Rgb(40, 34, 40));
        }
        else
        {
            // The face on its back
            PokeBuilder.Both(s => b.Mark(Body, Out(c, r, default, V(0.3f * s, 1f, -0.4f)), V(0.3f * s, 1f, -0.4f), 0.018f, 0.012f, Rgb(80, 120, 60)));
            b.Mark(Body, Out(c, r, default, V(0, 1f, -0.9f)), V(0, 1f, -0.9f), 0.03f, 0.01f, Rgb(80, 120, 60), MarkShape.Smile);
        }
        // Six legs, banded, three a side
        for (int i = 0; i < 3; i++)
            PokeBuilder.Both(s =>
            {
                float z = (0.06f - i * 0.08f) * k;
                var hip = V(0.1f * s, 0.15f, z) * k * 1f;
                int leg = i == 0 ? b.Leg(s, hip, true) : i == 2 ? b.Leg(s, hip, false) : b.Part(s < 0 ? "midL" : "midR", Root, hip, PokeRole.Leg, 0.8f, s);
                var knee = V((0.22f + 0.04f * (ariados ? 1 : 0)) * s, 0.22f + (ariados ? 0.06f : 0f), z * 1.2f) * k;
                var foot = V((0.3f + (ariados ? 0.08f : 0f)) * s, 0.005f, z * 1.5f + 0.02f) * k;
                b.Tube(leg, new[] { hip, knee, foot }, 0.02f * k, 0.012f * k, legColor, blend: 0f);
                b.PaintEll(leg, Vector3.Lerp(knee, foot, 0.35f), V(0.03f, 0.03f, 0.03f) * k, band, soft: 0.008f);
                b.PaintEll(leg, Vector3.Lerp(hip, knee, 0.5f), V(0.03f, 0.03f, 0.03f) * k, band, soft: 0.008f);
            });
        // Its head in front: a white horn, red mandibles, black eyes
        int head = b.Head(V(0, 0.14f, 0.1f) * k);
        var hc = c + V(0, 0.0f, 0.13f) * k;
        var hr = V(0.09f, 0.075f, 0.06f) * k;
        b.Ell(head, hc, hr, body);
        b.Spike(head, hc + V(0, hr.Y * 0.6f, 0), hc + V(0, hr.Y + 0.07f * k, 0.03f * k), 0.018f * k, Rgb(240, 240, 240), mat: Shell);
        PokeBuilder.Both(s => b.Spike(head, hc + V(0.03f * s, -0.04f, 0.04f) * k, hc + V(0.035f * s, -0.09f, 0.07f) * k, 0.012f * k, ariados ? White : Rgb(232, 90, 80), mat: Shell, blend: 0.004f));
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.1f, 1f);
            if (ariados) b.Eye(head, Out(hc, hr, default, look), look, 0.02f * k, Rgb(130, 90, 170), glare: true);
            else b.Eye(head, Out(hc, hr, default, look), look, 0.024f, sclera: true, pupil: Black);
        });
        return b;
    }

    private static PokeBuilder Spinarak() => SpiderBuild(false);

    private static PokeBuilder Ariados() => SpiderBuild(true);

    // ------------------------------------------------------------------ Chinchou line

    private static PokeBuilder Chinchou()
    {
        var b = new PokeBuilder("Chinchou", 0.55f, BodyPlan.Fish, V(0, 0.3f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(74, 108, 206);
        var light = Rgb(246, 230, 140);
        var c = V(0, 0.3f, 0);
        var r = V(0.14f, 0.12f, 0.12f);
        b.Ell(Body, c, r, blue);
        // Two feelers arching up and out, each hung with a glowing drop
        PokeBuilder.Both(s =>
        {
            int ear = b.Part(s < 0 ? "lureL" : "lureR", Body, c + V(0.03f * s, r.Y, 0), PokeRole.Ear, s, s);
            var path = Smooth(3, c + V(0.03f * s, r.Y * 0.9f, 0), c + V(0.12f * s, r.Y + 0.15f, -0.02f), c + V(0.28f * s, r.Y + 0.1f, -0.03f), c + V(0.33f * s, -0.02f, -0.02f));
            b.Tube(ear, path, 0.009f, 0.008f, blue, blend: 0f);
            b.Ell(ear, path[^1] + V(0, -0.03f, 0), V(0.035f, 0.045f, 0.035f), light, mat: Glow, blend: 0.006f);
            Frond(b, Body, c + V(0.11f * s, -0.03f, 0.0f), c + V(0.2f * s, -0.07f, -0.03f), 0.03f, Rgb(170, 190, 240), V(0, 1f, 0), 0.3f);
        });
        // Eyes like crosses on yellow, a little mouth
        PokeBuilder.Both(s =>
        {
            var look = V(0.42f * s, 0.05f, 1f);
            var at = Out(c, r, default, look);
            b.Mark(at.Z > 0 ? Body : Body, at, look, 0.03f, 0.03f, light);
            b.Mark(Body, at + Vector3.Normalize(look) * 0.001f, look, 0.022f, 0.005f, Rgb(40, 40, 70), MarkShape.Bar, 45f);
            b.Mark(Body, at + Vector3.Normalize(look) * 0.002f, look, 0.022f, 0.005f, Rgb(40, 40, 70), MarkShape.Bar, -45f);
        });
        b.Mark(Body, Out(c, r, default, V(0, -0.35f, 1f)), V(0, -0.35f, 1f), 0.012f, 0.008f, Rgb(40, 40, 70));
        return Lift(b);
    }

    private static PokeBuilder Lanturn()
    {
        var b = new PokeBuilder("Lanturn", 0.85f, BodyPlan.Fish, V(0, 0.32f, 0)) { Coat = Scales }.Hover();
        var blue = Rgb(70, 110, 200);
        var pale = Rgb(150, 190, 236);
        var yellow = Rgb(250, 220, 90);
        var c = V(0, 0.32f, 0);
        var r = V(0.15f, 0.14f, 0.22f);
        b.Ell(Body, c, r, blue);
        b.PaintEll(Body, c + V(0, -0.07f, 0.04f), V(0.16f, 0.08f, 0.22f), pale);
        // Its lure: one feeler from its brow arching forward, a glowing ball at its end
        int lure = b.Part("lure", Body, c + V(0, r.Y, 0.1f), PokeRole.Ear);
        var path = Smooth(3, c + V(0, r.Y * 0.8f, 0.1f), c + V(0, r.Y + 0.12f, 0.12f), c + V(0, r.Y + 0.1f, 0.28f));
        b.Tube(lure, path, 0.012f, 0.01f, blue, blend: 0f);
        b.Ell(lure, path[^1] + V(0, -0.02f, 0.02f), V(0.04f, 0.04f, 0.04f), yellow, mat: Glow, blend: 0.006f);
        // Fins at its sides and a tail of two lobes, edged yellow
        PokeBuilder.Both(s =>
        {
            int fin = b.Part(s < 0 ? "finL" : "finR", Body, c + V(0.13f * s, -0.04f, 0.05f), PokeRole.Fin, s, s);
            Frond(b, fin, c + V(0.12f * s, -0.04f, 0.05f), c + V(0.28f * s, -0.1f, 0.0f), 0.05f, pale, V(0, 1f, 0.2f), 0.3f);
            b.PaintEll(fin, c + V(0.26f * s, -0.095f, 0.0f), V(0.04f, 0.04f, 0.05f), yellow);
        });
        int tail = b.Tail(c + V(0, 0, -0.2f));
        PokeBuilder.Both(s => Frond(b, tail, c + V(0, 0, -0.2f), c + V(0.11f * s, 0.07f, -0.34f), 0.05f, blue, V(1f, 0, 0), 0.3f));
        b.PaintEll(tail, c + V(0, 0.05f, -0.34f), V(0.12f, 0.04f, 0.05f), yellow);
        Grin(b, Body, Out(c, r, default, V(0, -0.2f, 1f)), V(0.05f, 0.025f, 0.04f), Rgb(150, 60, 90));
        PokeBuilder.Both(s =>
        {
            var look = V(0.55f * s, 0.25f, 0.9f);
            b.Eye(Body, Out(c, r, default, look), look, 0.026f, Rgb(220, 70, 50), sclera: true, white: yellow, pupil: Rgb(200, 40, 40));
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Igglybuff

    private static PokeBuilder Igglybuff()
    {
        var b = new PokeBuilder("Igglybuff", 0.42f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = Fur };
        var pink = Rgb(250, 200, 210);
        var c = V(0, 0.18f, 0);
        var r = V(0.15f, 0.15f, 0.14f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.05f, 0.02f));
            b.Ell(leg, V(0.065f * s, 0.022f, 0.04f), V(0.032f, 0.022f, 0.04f), pink);
            int arm = b.Arm(s, V(0.13f * s, 0.16f, 0.04f));
            b.Ell(arm, V(0.15f * s, 0.15f, 0.05f), V(0.025f, 0.025f, 0.025f), pink);
        });
        b.Ell(Body, c, r, pink);
        // A curl on its forehead
        Curl(b, Body, c + V(0, r.Y * 0.95f, 0.02f), c + V(0, r.Y + 0.035f, 0.04f), V(0, 1f, 0), V(0, 0, 1f), 0.03f, 1.2f, 0.016f, pink);
        b.Mark(Body, On(c, r, 0, 0.22f), V(0, 0.4f, 1f), 0.025f, 0.025f, Rgb(220, 140, 160), MarkShape.Ring);
        b.Mark(Body, On(c, r, 0, 0.14f), V(0, -0.1f, 1f), 0.022f, 0.008f, Rgb(170, 70, 90), MarkShape.Smile);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.055f * s, 0.18f), V(0.35f * s, 0f, 1f), 0.035f, Rgb(176, 60, 70)));
        return b;
    }

    // ------------------------------------------------------------------ Natu line

    private static PokeBuilder Natu()
    {
        var b = new PokeBuilder("Natu", 0.46f, BodyPlan.Bird, V(0, 0.2f, 0)) { Coat = Fur };
        var green = Rgb(110, 190, 74);
        var yellow = Rgb(250, 210, 70);
        var red = Rgb(214, 56, 62);
        BirdLegs(b, 0.05f, 0.07f, 0.0f, 0.014f, red);
        var c = V(0, 0.21f, 0);
        var r = V(0.15f, 0.15f, 0.14f);
        b.Ell(Body, c, r, green);
        // Little folded wings of yellow, red tail feathers barred black, a red tuft on its head
        PokeBuilder.Both(s => FoldedWing(b, s, c + V(0.12f * s, 0.0f, -0.02f), c + V(0.15f * s, -0.08f, -0.14f), 0.05f, yellow, red));
        int tail = b.Tail(c + V(0, -0.02f, -0.12f));
        foreach (float x in new[] { -0.03f, 0.03f })
        {
            Frond(b, tail, c + V(x, -0.02f, -0.12f), c + V(x * 1.5f, -0.06f, -0.26f), 0.035f, red, V(0, 1f, 0), 0.3f);
            b.PaintEll(tail, c + V(x * 1.4f, -0.05f, -0.22f), V(0.04f, 0.04f, 0.012f), Black, soft: 0.006f);
        }
        b.Spike(Body, c + V(0, r.Y * 0.9f, -0.03f), c + V(0, r.Y + 0.08f, -0.1f), 0.022f, red, 0.5f);
        b.Spike(Body, On(c, r, 0, c.Y), On(c, r, 0, c.Y) + V(0, -0.02f, 0.1f), 0.04f, yellow, 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.25f, 0.75f);
            b.Eye(Body, Out(c, r, default, look), look, 0.035f, sclera: true, pupil: Black, glare: true);
        });
        return b;
    }

    private static PokeBuilder Xatu()
    {
        var b = new PokeBuilder("Xatu", 0.9f, BodyPlan.Bird, V(0, 0.4f, 0)) { Coat = Fur };
        var green = Rgb(110, 190, 74);
        var white = Rgb(244, 244, 238);
        var red = Rgb(214, 56, 62);
        BirdLegs(b, 0.05f, 0.1f, 0.0f, 0.016f, red);
        // Its body is hidden in its wings, folded round it like a cloak and marked in bands of colour
        var c = V(0, 0.38f, 0);
        var r = V(0.15f, 0.28f, 0.12f);
        b.Ell(Body, c, r, white);
        b.PaintEll(Body, V(0, 0.15f, 0), V(0.2f, 0.06f, 0.2f), Rgb(80, 160, 90));
        b.PaintEll(Body, V(0, 0.2f, 0), V(0.2f, 0.015f, 0.2f), red, soft: 0.006f);
        b.PaintEll(Body, V(0, 0.23f, 0), V(0.2f, 0.01f, 0.2f), Black, soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(Body, On(c, r, 0.07f * s, 0.5f), V(0.03f, 0.04f, 0.04f), Rgb(80, 180, 90));
            b.Mark(Body, On(c, r, 0.07f * s, 0.5f), V(0.2f * s, 0, 1f), 0.016f, 0.01f, red);
        });
        int tail = b.Tail(c + V(0, -0.1f, -0.1f));
        foreach (float x in new[] { -0.04f, 0.04f })
            Frond(b, tail, c + V(x, 0.0f, -0.1f), c + V(x * 1.3f, -0.25f, -0.24f), 0.04f, red, V(0, 0.3f, -1f), 0.3f);
        // A round green head with a yellow beak and heavy-lidded eyes
        int head = b.Head(V(0, 0.64f, 0.0f));
        var hc = V(0, 0.72f, 0.02f);
        var hr = V(0.09f, 0.09f, 0.09f);
        b.Ell(head, hc, hr, green);
        b.Spike(head, hc + V(0, -0.01f, 0.07f), hc + V(0, -0.04f, 0.16f), 0.03f, Rgb(250, 196, 70), 0.7f, Shell);
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.15f, 0.8f);
            b.Eye(head, Out(hc, hr, default, look), look, 0.02f, sclera: true, pupil: Black, glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Mareep line

    private static PokeBuilder Mareep()
    {
        var b = new PokeBuilder("Mareep", 0.56f, BodyPlan.Quadruped, V(0, 0.22f, -0.02f)) { Coat = Fur };
        var blue = Rgb(92, 140, 220);
        var wool = Rgb(246, 238, 204);
        var black = Rgb(40, 38, 46);
        var yellow = Rgb(250, 214, 70);
        StubbyLegs(b, 0.07f, 0.14f, 0.07f, -0.1f, 0.032f, blue);
        // A body all of wool
        var c = V(0, 0.22f, -0.02f);
        var r = V(0.15f, 0.13f, 0.16f);
        b.Ell(Body, c, r, wool);
        Lumps(b, Body, c, r, 26, 0.06f, wool, PixelCanvas.Mix(wool, White, 0.5f), 10f);
        // Its tail, black and yellow, tipped with an orange ball
        int tail = b.Tail(c + V(0, 0.04f, -0.15f));
        var tp = Smooth(3, c + V(0, 0.04f, -0.15f), c + V(0, 0.16f, -0.22f), c + V(0, 0.27f, -0.2f));
        Striped(b, tail, tp, 0.018f, 0.016f, black, yellow, 3);
        b.Ell(tail, tp[^1] + V(0, 0.02f, 0), V(0.03f, 0.03f, 0.03f), Rgb(250, 140, 60), mat: Glow, blend: 0.006f);
        int head = b.Head(V(0, 0.24f, 0.13f));
        var hc = V(0, 0.27f, 0.17f);
        var hr = V(0.075f, 0.07f, 0.08f);
        b.Ell(head, hc, hr, blue);
        b.Ell(head, hc + V(0, 0.05f, -0.04f), V(0.06f, 0.04f, 0.05f), wool, blend: 0.02f);
        // Ears like horns, banded black and yellow
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, hc + V(0.06f * s, 0.02f, -0.02f));
            var tip = hc + V(0.16f * s, 0.02f, -0.06f);
            b.Spike(ear, hc + V(0.05f * s, 0.02f, -0.02f), tip, 0.03f, black, 0.8f);
            b.PaintEll(ear, Vector3.Lerp(hc + V(0.05f * s, 0.02f, -0.02f), tip, 0.45f), V(0.015f, 0.04f, 0.04f), yellow, soft: 0.006f);
        });
        PokeBuilder.Both(s =>
        {
            var look = V(0.5f * s, 0.05f, 1f);
            b.Eye(head, Out(hc, hr, default, look), look, 0.018f, Black);
        });
        return b;
    }

    private static PokeBuilder Flaaffy()
    {
        var b = new PokeBuilder("Flaaffy", 0.66f, BodyPlan.Biped, V(0, 0.26f, 0)) { Coat = Fur };
        var pink = Rgb(246, 168, 190);
        var wool = Rgb(238, 240, 246);
        var black = Rgb(40, 38, 46);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.15f, 0));
            b.Limb(leg, V(0.06f * s, 0.16f, 0), V(0.065f * s, 0.04f, 0.02f), 0.04f, 0.032f, pink);
            b.Ell(leg, V(0.066f * s, 0.024f, 0.04f), V(0.032f, 0.024f, 0.045f), White);
            int arm = b.Arm(s, V(0.09f * s, 0.34f, 0.03f));
            b.Limb(arm, V(0.08f * s, 0.34f, 0.03f), V(0.14f * s, 0.28f, 0.07f), 0.022f, 0.02f, pink);
        });
        b.Ell(Body, V(0, 0.27f, 0), V(0.1f, 0.14f, 0.09f), pink);
        // A ruff of wool round its neck and a tuft on its head
        Lumps(b, Body, V(0, 0.38f, 0.0f), V(0.1f, 0.05f, 0.09f), 14, 0.04f, wool, White, 10f);
        int tail = b.Tail(V(0, 0.2f, -0.08f));
        var tp = Smooth(3, V(0, 0.2f, -0.08f), V(0, 0.12f, -0.2f), V(0.04f, 0.08f, -0.3f));
        Striped(b, tail, tp, 0.022f, 0.018f, pink, black, 3);
        b.Ell(tail, tp[^1] + V(0.01f, 0, -0.02f), V(0.032f, 0.032f, 0.032f), Rgb(80, 150, 230), mat: Glow, blend: 0.006f);
        int head = b.Head(V(0, 0.42f, 0.02f));
        var c = V(0, 0.48f, 0.05f);
        var r = V(0.07f, 0.065f, 0.08f);
        b.Ell(head, c, r, pink);
        Lumps(b, head, c + V(0, 0.05f, -0.01f), V(0.07f, 0.04f, 0.07f), 10, 0.035f, wool, White, c.Y + 0.02f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.06f * s, 0.01f, -0.01f));
            b.Spike(ear, c + V(0.05f * s, 0.01f, -0.01f), c + V(0.12f * s, -0.02f, -0.03f), 0.022f, black, 0.6f);
        });
        PokeBuilder.Both(s =>
        {
            var look = V(0.5f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f, Black);
        });
        return b;
    }

    /// <summary>
    /// Ampharos and its Mega Evolution: a yellow lighthouse of a beast, white of belly, its long ears and tail banded
    /// black, a red jewel glowing on its brow and at its tail's end; the Mega's head and tail grown long white wool.
    /// </summary>
    private static PokeBuilder AmpharosBuild(bool mega)
    {
        var b = new PokeBuilder(mega ? "Ampharos-Mega" : "Ampharos", 0.92f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Scales };
        var yellow = Rgb(250, 212, 70);
        var white = Rgb(248, 246, 236);
        var black = Rgb(40, 38, 46);
        var jewel = Rgb(226, 50, 60);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.18f, 0));
            b.Limb(leg, V(0.07f * s, 0.2f, 0), V(0.08f * s, 0.05f, 0.02f), 0.05f, 0.04f, yellow);
            b.Ell(leg, V(0.08f * s, 0.028f, 0.05f), V(0.038f, 0.028f, 0.06f), yellow);
            int arm = b.Arm(s, V(0.1f * s, 0.46f, 0.02f));
            b.Limb(arm, V(0.09f * s, 0.46f, 0.02f), V(0.2f * s, 0.4f, 0.06f), 0.03f, 0.024f, yellow);
        });
        b.Ell(Body, V(0, 0.34f, 0), V(0.12f, 0.19f, 0.1f), yellow);
        b.PaintEll(Body, V(0, 0.32f, 0.06f), V(0.09f, 0.17f, 0.06f), white);
        // A long tail banded black, the jewel at its end
        int tail = b.Tail(V(0, 0.22f, -0.08f));
        var tp = Smooth(3, V(0, 0.22f, -0.08f), V(0.05f, 0.1f, -0.22f), V(0.14f, 0.08f, -0.32f), V(0.2f, 0.16f, -0.36f));
        Striped(b, tail, tp, 0.04f, 0.022f, yellow, black, 4);
        b.Ell(tail, tp[^1] + V(0.01f, 0.02f, 0), V(0.032f, 0.032f, 0.032f), jewel, mat: Glow, blend: 0.006f);
        // A long neck ringed black, a small head, ears banded black
        int head = b.Head(V(0, 0.52f, 0.02f));
        b.Tube(head, Smooth(3, V(0, 0.48f, 0), V(0, 0.6f, 0.02f), V(0, 0.7f, 0.04f)), 0.045f, 0.04f, yellow, blend: 0f);
        foreach (float y in new[] { 0.58f, 0.63f })
            b.PaintTorus(head, V(0, y, 0.015f), 0.045f, 0.009f, black);
        var c = V(0, 0.75f, 0.06f);
        var r = V(0.07f, 0.065f, 0.085f);
        b.Ell(head, c, r, yellow);
        b.Ell(head, c + V(0, r.Y * 0.9f, 0.02f), V(0.022f, 0.022f, 0.022f), jewel, mat: Glow, blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.05f * s, 0.03f, -0.02f));
            var tip = c + V(0.13f * s, 0.1f, -0.06f);
            b.Spike(ear, c + V(0.04f * s, 0.03f, -0.02f), tip, 0.028f, yellow, 0.7f);
            b.PaintEll(ear, Vector3.Lerp(c + V(0.04f * s, 0.03f, -0.02f), tip, 0.75f), V(0.04f, 0.035f, 0.04f), black);
        });
        if (mega)
        {
            // Long white wool flowing back from its head, and its tail grown into a plume of it with red beads
            for (int i = 0; i < 6; i++)
            {
                float x = -0.05f + 0.02f * i;
                var path = Smooth(3, c + V(x, 0.04f, -0.04f), c + V(x * 1.6f, 0.0f, -0.18f), c + V(x * 2.2f, -0.18f, -0.26f), c + V(x * 2.4f, -0.36f, -0.24f));
                b.Tube(head, path, 0.03f, 0.014f, white, blend: 0f);
            }
            for (int i = 0; i < 5; i++)
            {
                var at = tp[Math.Min(tp.Length - 1, 2 + i * 2)];
                b.Ell(tail, at + V(0, 0.04f, -0.02f), V(0.06f, 0.045f, 0.05f), white, blend: 0.03f);
                b.Ell(tail, at + V(0.03f, 0.08f, -0.03f), V(0.014f, 0.014f, 0.014f), jewel, mat: Glow, blend: 0.004f);
            }
        }
        PokeBuilder.Both(s =>
        {
            var look = V(0.5f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.017f, Black);
        });
        return b;
    }

    private static PokeBuilder Ampharos() => AmpharosBuild(false);

    // ------------------------------------------------------------------ shared by the rest of the batch

    /// <summary>A flower of <paramref name="count"/> petals spread round <paramref name="at"/>, its face toward <paramref name="facing"/>, a disc of <paramref name="heart"/> in its middle.</summary>
    private static void FlowerHead(PokeBuilder b, int bone, Vector3 at, Vector3 facing, int count, float r, float width, Color petal, Color heart, float heartR, float turn = 0f)
    {
        var n = Vector3.Normalize(facing);
        var u = Vector3.Normalize(Vector3.Cross(n, MathF.Abs(n.Y) > 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
        var w = Vector3.Cross(n, u);
        for (int i = 0; i < count; i++)
        {
            float a = (i + turn) * MathF.Tau / count;
            var o = u * MathF.Cos(a) + w * MathF.Sin(a);
            Frond(b, bone, at + o * heartR * 0.5f, at + o * r - n * r * 0.12f, width, petal, n, 0.25f, Leaf);
        }
        b.Ell(bone, at + n * heartR * 0.2f, V(heartR, heartR * 0.6f, heartR), heart, Euler(n), Leaf);
    }

    /// <summary>A branch of coral from <paramref name="root"/> to a round tip at <paramref name="tip"/>, a twig forking off along <paramref name="twig"/>.</summary>
    private static void Coral(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float r, Color color, Vector3 twig)
    {
        b.Limb(bone, root, tip, r, r * 0.8f, color, blend: 0.012f);
        b.Ell(bone, tip, V(r * 0.85f, r * 0.85f, r * 0.85f), color, blend: 0.006f);
        var m = Vector3.Lerp(root, tip, 0.55f);
        b.Limb(bone, m, m + twig, r * 0.75f, r * 0.6f, color, blend: 0.01f);
        b.Ell(bone, m + twig, V(r * 0.65f, r * 0.65f, r * 0.65f), color, blend: 0.006f);
    }

    // ------------------------------------------------------------------ Bellossom

    /// <summary>Bellossom: a little green dancer in a skirt of leaves, green and gold, two red flowers in its hair.</summary>
    private static PokeBuilder Bellossom()
    {
        var b = new PokeBuilder("Bellossom", 0.5f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Leaf };
        var green = Rgb(132, 186, 100);
        var dark = Rgb(66, 132, 74);
        var gold = Rgb(226, 190, 74);
        var red = Rgb(232, 86, 52);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.14f, 0));
            b.Limb(leg, V(0.035f * s, 0.15f, 0), V(0.04f * s, 0.03f, 0.01f), 0.024f, 0.02f, green);
            b.Ell(leg, V(0.04f * s, 0.016f, 0.025f), V(0.024f, 0.016f, 0.032f), green);
        });
        b.Ell(Body, V(0, 0.2f, 0), V(0.075f, 0.085f, 0.065f), green);
        // The skirt: ten leaves round its hips, dark green and gold in turn
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 10f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, V(0, 0.19f, 0) + o * 0.05f, V(0, 0.035f, 0) + o * 0.18f, 0.055f, i % 2 == 0 ? dark : gold, o * 0.16f + V(0, 0.13f, 0), 0.2f, Leaf);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.06f * s, 0.25f, 0.02f));
            b.Limb(arm, V(0.055f * s, 0.25f, 0.02f), V(0.12f * s, 0.3f, 0.05f), 0.02f, 0.016f, green);
        });
        int head = b.Head(V(0, 0.28f, 0));
        var c = V(0, 0.37f, 0.01f);
        var r = V(0.09f, 0.085f, 0.085f);
        b.Ell(head, c, r, green);
        PokeBuilder.Both(s => FlowerHead(b, head, c + V(0.055f * s, 0.07f, -0.01f), V(0.6f * s, 1f, 0.2f), 5, 0.075f, 0.035f, red, Rgb(250, 200, 70), 0.025f, 0.25f));
        PokeBuilder.Both(s =>
        {
            var look = V(0.4f * s, 0.05f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.024f, Rgb(60, 120, 200));
            var cheek = V(0.7f * s, -0.3f, 0.7f);
            b.Mark(head, Out(c, r, default, cheek), cheek, 0.015f, 0.01f, Rgb(240, 140, 150));
        });
        b.Mark(head, Out(c, r, default, V(0, -0.4f, 1f)), V(0, -0.4f, 1f), 0.018f, 0.008f, Rgb(170, 60, 70), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Politoed

    /// <summary>Politoed: a green frog standing on bent legs, a green swirl on its yellow belly, a curl of blue on its head.</summary>
    private static PokeBuilder Politoed()
    {
        var b = new PokeBuilder("Politoed", 0.72f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Scales };
        var green = Rgb(110, 196, 84);
        var yellow = Rgb(240, 228, 100);
        var curl = Rgb(110, 150, 220);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.2f, 0));
            var knee = V(0.16f * s, 0.12f, 0.05f);
            b.Limb(leg, V(0.08f * s, 0.2f, 0), knee, 0.055f, 0.045f, green);
            b.Limb(leg, knee, V(0.14f * s, 0.03f, 0.06f), 0.045f, 0.035f, green);
            b.Ell(leg, V(0.145f * s, 0.022f, 0.1f), V(0.045f, 0.022f, 0.06f), green);
            for (int i = -1; i <= 1; i++)
                b.Ell(leg, V(0.145f * s + 0.028f * i, 0.016f, 0.16f), V(0.016f, 0.016f, 0.016f), yellow, blend: 0.006f);
        });
        var c = V(0, 0.3f, 0);
        var r = V(0.15f, 0.17f, 0.13f);
        b.Ell(Body, c, r, green);
        b.PaintEll(Body, V(0, 0.27f, 0.08f), V(0.11f, 0.12f, 0.07f), yellow);
        Swirl(b, Body, c, r, 0, 0.27f, 0.004f, 0.09f, 2.2f, 0.018f, green);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.38f, 0.02f));
            var hand = V(0.22f * s, 0.46f, 0.07f);
            b.Limb(arm, V(0.12f * s, 0.38f, 0.02f), hand, 0.03f, 0.026f, green);
            b.Ell(arm, hand, V(0.03f, 0.03f, 0.03f), green);
            for (int i = -1; i <= 1; i++)
                b.Ell(arm, hand + V(0.02f * i, 0.035f, 0.01f), V(0.013f, 0.013f, 0.013f), yellow, blend: 0.006f);
        });
        int head = b.Head(V(0, 0.42f, 0.02f));
        var hc = V(0, 0.5f, 0.04f);
        var hr = V(0.12f, 0.1f, 0.11f);
        b.Ell(head, hc, hr, green);
        b.PaintEll(head, hc + V(0, -0.055f, 0.06f), V(0.11f, 0.045f, 0.08f), yellow);
        // The curl: a stalk up from the top of its head, wound round at its end
        var top = hc + V(0, hr.Y + 0.07f, -0.01f);
        b.Tube(head, new[] { hc + V(0, hr.Y * 0.85f, -0.01f), top }, 0.011f, 0.01f, curl);
        Curl(b, head, top, top + V(0.035f, 0, 0), V(-1f, 0, 0), V(0, 1f, 0), 0.035f, 1.3f, 0.01f, curl);
        PokeBuilder.Both(s =>
        {
            var look = V(0.65f * s, 0.45f, 0.6f);
            b.Eye(head, Out(hc, hr, default, look), look, 0.032f, sclera: true, pupil: Black);
            var cheek = V(0.85f * s, -0.15f, 0.5f);
            b.Mark(head, Out(hc, hr, default, cheek), cheek, 0.022f, 0.016f, Rgb(244, 150, 160));
        });
        return b;
    }

    // ------------------------------------------------------------------ Hoppip line

    private static PokeBuilder Hoppip()
    {
        var b = new PokeBuilder("Hoppip", 0.45f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Fur }.Hover();
        var pink = Rgb(246, 150, 182);
        var green = Rgb(110, 190, 80);
        var c = V(0, 0.2f, 0);
        var r = V(0.12f, 0.11f, 0.11f);
        b.Ell(Body, c, r, pink);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.11f, 0.02f));
            b.Ell(leg, V(0.05f * s, 0.1f, 0.03f), V(0.03f, 0.022f, 0.035f), pink);
            int ear = b.Ear(Body, s, c + V(0.07f * s, 0.07f, 0));
            CatEar(b, ear, c + V(0.06f * s, 0.06f, 0), c + V(0.13f * s, 0.16f, -0.01f), 0.045f, pink, s > 0 ? Rgb(60, 44, 60) : Rgb(236, 120, 156));
        });
        // Two leaves on a stalk
        int sprout = b.Part("leaf", Body, c + V(0, r.Y, 0), PokeRole.Leaf);
        var stalk = c + V(0, r.Y + 0.05f, 0);
        b.Tube(sprout, new[] { c + V(0, r.Y * 0.85f, 0), stalk }, 0.012f, 0.01f, green);
        PokeBuilder.Both(s => Frond(b, sprout, stalk, stalk + V(0.15f * s, 0.07f, 0), 0.04f, green, V(-0.3f * s, 1f, 0.2f), 0.2f, Leaf));
        PokeBuilder.Both(s =>
        {
            var look = V(0.35f * s, 0.05f, 1f);
            b.Eye(Body, Out(c, r, default, look), look, 0.018f, Rgb(240, 200, 60));
        });
        b.Mark(Body, Out(c, r, default, V(0, -0.25f, 1f)), V(0, -0.25f, 1f), 0.02f, 0.012f, Rgb(170, 60, 80), MarkShape.Smile);
        return Lift(b);
    }

    private static PokeBuilder Skiploom()
    {
        var b = new PokeBuilder("Skiploom", 0.48f, BodyPlan.Quadruped, V(0, 0.13f, 0)) { Coat = Leaf };
        var green = Rgb(130, 196, 90);
        var c = V(0, 0.13f, 0);
        var r = V(0.17f, 0.1f, 0.13f);
        StubbyLegs(b, 0.08f, 0.07f, 0.06f, -0.06f, 0.026f, green);
        b.Ell(Body, c, r, green);
        b.Ell(Body, c + V(0, 0.02f, 0), V(0.21f, 0.05f, 0.15f), green, blend: 0.04f);
        // The yellow flower on its head
        int flower = b.Part("flower", Body, c + V(0, r.Y, 0), PokeRole.Leaf);
        FlowerHead(b, flower, c + V(0, 0.1f, 0), V(0, 1f, 0), 10, 0.13f, 0.035f, Rgb(250, 214, 70), Rgb(232, 240, 210), 0.05f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.35f * s, 0.1f, 1f);
            b.Eye(Body, Out(c, r, default, look), look, 0.02f, Rgb(210, 50, 60));
        });
        b.Mark(Body, Out(c, r, default, V(0, -0.2f, 1f)), V(0, -0.2f, 1f), 0.02f, 0.01f, Rgb(60, 90, 50), MarkShape.Smile);
        return b;
    }

    private static PokeBuilder Jumpluff()
    {
        var b = new PokeBuilder("Jumpluff", 0.55f, BodyPlan.Floating, V(0, 0.25f, 0)) { Coat = Fur }.Hover();
        var blue = Rgb(104, 132, 222);
        var cotton = Rgb(246, 234, 200);
        var light = Rgb(252, 246, 226);
        var green = Rgb(110, 190, 80);
        var c = V(0, 0.25f, 0);
        var r = V(0.11f, 0.1f, 0.1f);
        b.Ell(Body, c, r, blue);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.16f, 0.02f));
            b.Ell(leg, V(0.05f * s, 0.155f, 0.03f), V(0.03f, 0.022f, 0.035f), blue);
            // A ball of cotton held out in each paw
            int arm = b.Arm(s, V(0.09f * s, 0.24f, 0.02f));
            b.Limb(arm, V(0.08f * s, 0.24f, 0.02f), V(0.15f * s, 0.2f, 0.04f), 0.025f, 0.022f, blue);
            var puff = V(0.21f * s, 0.18f, 0.05f);
            b.Ell(arm, puff, V(0.07f, 0.065f, 0.07f), cotton);
            Lumps(b, arm, puff, V(0.065f, 0.06f, 0.065f), 10, 0.025f, cotton, light, 99f);
        });
        int sprout = b.Part("leaf", Body, c + V(0, r.Y, 0), PokeRole.Leaf);
        var stalk = c + V(0, r.Y + 0.1f, 0);
        b.Tube(sprout, new[] { c + V(0, r.Y * 0.85f, 0), stalk }, 0.012f, 0.01f, green);
        PokeBuilder.Both(s => Frond(b, sprout, c + V(0, r.Y + 0.03f, 0), c + V(0.06f * s, r.Y + 0.05f, 0.01f), 0.02f, green, V(0, 1f, 0.2f), 0.25f, Leaf));
        var top = c + V(0, r.Y + 0.17f, 0);
        b.Ell(sprout, top, V(0.09f, 0.08f, 0.09f), cotton);
        Lumps(b, sprout, top, V(0.085f, 0.075f, 0.085f), 12, 0.03f, cotton, light, 99f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.35f * s, 0.05f, 1f);
            b.Eye(Body, Out(c, r, default, look), look, 0.018f, Rgb(210, 60, 80));
        });
        b.Mark(Body, Out(c, r, default, V(0, -0.25f, 1f)), V(0, -0.25f, 1f), 0.018f, 0.01f, Rgb(170, 60, 80), MarkShape.Smile);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Sunkern and Sunflora

    private static PokeBuilder Sunkern()
    {
        var b = new PokeBuilder("Sunkern", 0.45f, BodyPlan.Floating, V(0, 0.14f, 0)) { Coat = Leaf };
        var brown = Rgb(74, 50, 38);
        var yellow = Rgb(246, 210, 72);
        var green = Rgb(100, 176, 80);
        var c = V(0, 0.14f, 0);
        var r = V(0.13f, 0.14f, 0.12f);
        // A seed, yellow above and dark below, banded yellow, a crown of points round its top
        b.Ell(Body, c, r, brown);
        b.PaintEll(Body, c + V(0, 0.1f, 0), V(0.15f, 0.085f, 0.14f), yellow);
        foreach (float y in new[] { -0.045f, -0.095f })
            b.PaintEll(Body, c + V(0, y, 0), V(0.2f, 0.012f, 0.2f), yellow, soft: 0.008f);
        for (int i = 0; i < 9; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 9f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            var root = c + V(o.X * 0.095f, 0.09f, o.Z * 0.085f);
            Blade(b, Body, root, root + o * 0.03f + V(0, 0.065f, 0), 0.026f, yellow, o, 0.3f);
        }
        int sprout = b.Part("leaf", Body, c + V(0, r.Y, 0), PokeRole.Leaf);
        var stalk = c + V(0, r.Y + 0.06f, 0);
        b.Tube(sprout, new[] { c + V(0, r.Y * 0.85f, 0), stalk }, 0.012f, 0.01f, green);
        PokeBuilder.Both(s => Frond(b, sprout, stalk, stalk + V(0.12f * s, 0.04f, 0), 0.045f, green, V(-0.3f * s, 1f, 0.1f), 0.22f, Leaf));
        PokeBuilder.Both(s =>
        {
            var look = V(0.4f * s, -0.05f, 1f);
            b.Eye(Body, Out(c, r, default, look), look, 0.03f, sclera: true, pupil: Black);
        });
        b.Mark(Body, Out(c, r, default, V(0, -0.45f, 1f)), V(0, -0.45f, 1f), 0.025f, 0.018f, Rgb(236, 130, 140));
        return b;
    }

    private static PokeBuilder Sunflora()
    {
        var b = new PokeBuilder("Sunflora", 0.62f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Leaf };
        var green = Rgb(112, 182, 92);
        var petal = Rgb(250, 196, 50);
        var face = Rgb(250, 228, 124);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.14f, 0));
            b.Limb(leg, V(0.025f * s, 0.15f, 0), V(0.07f * s, 0.03f, 0.02f), 0.016f, 0.013f, green);
            b.Ell(leg, V(0.09f * s, 0.013f, 0.04f), V(0.045f, 0.013f, 0.03f), green, V(0, 30f * s, 0));
        });
        b.Ell(Body, V(0, 0.2f, 0), V(0.065f, 0.08f, 0.06f), green);
        // Two leaves for arms
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.05f * s, 0.24f, 0));
            Frond(b, arm, V(0.04f * s, 0.24f, 0), V(0.22f * s, 0.3f, 0.02f), 0.06f, green, V(0, 1f, 0.3f), 0.2f, Leaf);
        });
        // Its head the flower: a pale face ringed by petals
        int head = b.Head(V(0, 0.28f, 0));
        b.Limb(head, V(0, 0.26f, 0), V(0, 0.36f, 0.01f), 0.022f, 0.02f, green);
        var c = V(0, 0.43f, 0.02f);
        FlowerHead(b, head, c, V(0, 0, 1f), 13, 0.16f, 0.04f, petal, face, 0.095f);
        var fc = c + V(0, 0, 0.019f);
        var fr = V(0.095f, 0.095f, 0.057f);
        PokeBuilder.Both(s => b.Eye(head, On(fc, fr, 0.032f * s, fc.Y + 0.018f), V(0.2f * s, 0.1f, 1f), 0.022f, closed: true));
        b.Mark(head, On(fc, fr, 0, fc.Y - 0.035f), V(0, -0.2f, 1f), 0.03f, 0.016f, Rgb(180, 80, 60), MarkShape.Smile);
        return b;
    }

    // ------------------------------------------------------------------ Slowking

    /// <summary>
    /// Slowking and its Galarian form: Slowpoke standing tall and wise, a white ruff round its neck and a Shellder for a
    /// crown, a jewel set in it; the Galarian's head purple, its crown the purple Shellder of Galar with a yellow jewel
    /// and a dark cloak over its shoulders.
    /// </summary>
    private static PokeBuilder SlowkingBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Slowking-Galar" : "Slowking", 0.86f, BodyPlan.Biped, V(0, 0.34f, 0)) { Coat = Scales };
        var pink = Rgb(240, 160, 184);
        var cream = Rgb(240, 230, 196);
        var headColor = galar ? Rgb(126, 104, 156) : pink;
        var shell = galar ? Rgb(120, 96, 160) : Rgb(208, 210, 220);
        var spike = galar ? Rgb(80, 62, 104) : Rgb(170, 176, 190);
        var band = galar ? Rgb(96, 76, 128) : Rgb(180, 186, 200);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.2f, 0));
            b.Limb(leg, V(0.09f * s, 0.21f, 0), V(0.11f * s, 0.06f, 0.02f), 0.065f, 0.055f, pink);
            b.Ell(leg, V(0.115f * s, 0.035f, 0.05f), V(0.055f, 0.035f, 0.07f), pink);
            foreach (float t in new[] { -1f, 1f })
                b.Ell(leg, V(0.115f * s + 0.022f * t, 0.02f, 0.112f), V(0.012f, 0.012f, 0.014f), White, mat: Shell, blend: 0.004f);
        });
        b.Ell(Body, V(0, 0.34f, 0), V(0.16f, 0.2f, 0.13f), pink);
        b.PaintEll(Body, V(0, 0.3f, 0.09f), V(0.12f, 0.14f, 0.07f), cream);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.14f * s, 0.44f, 0.03f));
            var hand = V(0.07f * s, 0.34f, 0.13f);
            b.Limb(arm, V(0.13f * s, 0.44f, 0.03f), V(0.17f * s, 0.36f, 0.08f), 0.04f, 0.035f, pink);
            b.Limb(arm, V(0.17f * s, 0.36f, 0.08f), hand, 0.035f, 0.03f, pink);
            b.Ell(arm, hand, V(0.032f, 0.03f, 0.03f), pink);
        });
        // A ruff of white round its neck, or the Galarian's dark cloak
        if (galar)
        {
            var cloak = Rgb(70, 56, 92);
            b.Torus(Body, V(0, 0.5f, 0), 0.1f, 0.03f, cloak, mat: Fur);
            for (int i = 0; i < 9; i++)
            {
                float a = (90f + i * 22.5f) * Degree;
                var o = V(MathF.Sin(a), 0, MathF.Cos(a));
                Frond(b, Body, V(0, 0.51f, 0) + o * 0.1f, V(0, 0.36f, 0) + o * 0.19f, 0.05f, cloak, o + V(0, 0.4f, 0), 0.2f, Fur);
            }
        }
        else
            for (int i = 0; i < 12; i++)
            {
                float a = i * MathF.Tau / 12f;
                var o = V(MathF.Sin(a), 0, MathF.Cos(a));
                b.Ell(Body, V(0, 0.52f, 0.0f) + o * V(0.11f, 0, 0.1f), V(0.04f, 0.028f, 0.04f), White, Euler(o + V(0, 1.5f, 0)), Fur, 0.01f);
            }
        int head = b.Head(V(0, 0.54f, 0.02f));
        var hc = V(0, 0.66f, 0.04f);
        SlowHead(b, head, hc, 0.9f, headColor, null, cream);
        // The crown: a Shellder biting down on its head, a jewel set in its front
        var mouth = hc + V(0, 0.095f, 0.0f);
        ShellderOn(b, head, mouth, V(0, 1f, -0.12f), 0.085f, shell, spike, band);
        b.Ell(head, mouth + V(0, 0.06f, 0.085f), V(0.022f, 0.022f, 0.016f), galar ? Rgb(200, 230, 60) : Rgb(220, 50, 60), mat: Glow, blend: 0.006f);
        int tail = b.Tail(V(0, 0.22f, -0.12f));
        var path = Smooth(3, V(0, 0.22f, -0.12f), V(0.06f, 0.1f, -0.26f), V(0.17f, 0.07f, -0.32f), V(0.26f, 0.1f, -0.3f));
        b.Tube(tail, path, 0.055f, 0.045f, pink, blend: 0f);
        b.PaintEll(tail, path[^1], V(0.07f, 0.065f, 0.07f), galar ? Rgb(110, 84, 140) : Rgb(250, 226, 232));
        return b;
    }

    private static PokeBuilder Slowking() => SlowkingBuild(false);

    // ------------------------------------------------------------------ Wobbuffet

    /// <summary>Wobbuffet: a tall blue blob, arms raised and eyes shut tight, its black tail behind it watching with two eyes of its own.</summary>
    private static PokeBuilder Wobbuffet()
    {
        var b = new PokeBuilder("Wobbuffet", 0.8f, BodyPlan.Biped, V(0, 0.34f, 0)) { Coat = Scales };
        var blue = Rgb(92, 172, 222);
        var black = Rgb(40, 40, 52);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.08f, 0));
            b.Limb(leg, V(0.07f * s, 0.08f, 0), V(0.08f * s, 0.03f, 0.02f), 0.045f, 0.04f, blue);
            b.Ell(leg, V(0.085f * s, 0.022f, 0.05f), V(0.05f, 0.022f, 0.065f), blue);
        });
        var c = V(0, 0.34f, 0);
        var r = V(0.16f, 0.3f, 0.12f);
        b.Ell(Body, c, r, blue);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.5f, 0));
            var hand = V(0.2f * s, 0.72f, 0.0f);
            b.Limb(arm, V(0.11f * s, 0.5f, 0), V(0.2f * s, 0.6f, 0), 0.045f, 0.04f, blue);
            b.Limb(arm, V(0.2f * s, 0.6f, 0), hand, 0.04f, 0.038f, blue);
            b.Ell(arm, hand, V(0.042f, 0.035f, 0.04f), blue);
        });
        Grin(b, Body, On(c, r, 0, 0.42f), V(0.07f, 0.045f, 0.04f), Rgb(222, 112, 132));
        PokeBuilder.Both(s =>
        {
            var look = V(0.3f * s, 0.32f, 1f);
            b.Eye(Body, Out(c, r, default, look), look, 0.028f, closed: true);
        });
        // The tail: a black stalk ending in a head with two white eyes
        int tail = b.Tail(V(0, 0.1f, -0.1f));
        var path = Smooth(3, V(0, 0.1f, -0.08f), V(0.08f, 0.05f, -0.22f), V(0.18f, 0.08f, -0.32f));
        b.Tube(tail, path, 0.03f, 0.025f, black, blend: 0f);
        var ball = V(0.22f, 0.11f, -0.36f);
        var br = V(0.06f, 0.05f, 0.055f);
        b.Ell(tail, ball, br, black);
        foreach (float t in new[] { -1f, 1f })
        {
            var look = Vector3.Normalize(V(0.8f, 0.2f, 0.3f + 0.25f * t));
            b.Mark(tail, Out(ball, br, default, look), look, 0.011f, 0.016f, White);
        }
        return b;
    }

    // ------------------------------------------------------------------ Pineco and Forretress

    private static PokeBuilder Pineco()
    {
        var b = new PokeBuilder("Pineco", 0.5f, BodyPlan.Floating, V(0, 0.17f, 0)) { Coat = Shell };
        var teal = Rgb(72, 124, 124);
        var dark = Rgb(44, 84, 90);
        var c = V(0, 0.17f, 0);
        var r = V(0.15f, 0.17f, 0.14f);
        b.Ell(Body, c, r, teal);
        // Rows of scales overlapping down its sides, a slit between them where its eyes peer out
        int row = 0;
        foreach (float h in new[] { 0.08f, 0.0f, -0.08f })
        {
            float ring = MathF.Sqrt(1f - (h / r.Y) * (h / r.Y));
            for (int i = 0; i < 8; i++)
            {
                float a = (i + (row % 2) * 0.5f) * MathF.Tau / 8f;
                if (row == 0 && MathF.Abs(MathF.IEEERemainder(a, MathF.Tau)) < 0.6f) continue;
                var o = V(MathF.Sin(a), 0, MathF.Cos(a));
                var root = c + V(o.X * r.X * ring * 0.9f, h, o.Z * r.Z * ring * 0.9f);
                Blade(b, Body, root, root + o * 0.06f + V(0, -0.05f, 0), 0.042f, row % 2 == 0 ? teal : dark, o + V(0, 0.6f, 0), 0.3f, Shell);
            }
            row++;
        }
        b.Spike(Body, c + V(0, r.Y * 0.85f, 0), c + V(0, r.Y + 0.08f, 0), 0.04f, teal, mat: Shell);
        b.PaintEll(Body, c + V(0, 0.07f, 0.13f), V(0.09f, 0.03f, 0.04f), Rgb(28, 40, 42));
        PokeBuilder.Both(s =>
        {
            var look = V(0.3f * s, 0.42f, 1f);
            b.Eye(Body, Out(c, r, default, look), look, 0.02f, Rgb(220, 60, 60), glare: true);
        });
        return b;
    }

    private static PokeBuilder Forretress()
    {
        var b = new PokeBuilder("Forretress", 0.75f, BodyPlan.Floating, V(0, 0.25f, 0)) { Coat = Shell };
        var shell = Rgb(214, 198, 210);
        var red = Rgb(196, 52, 52);
        var dark = Rgb(40, 30, 36);
        var c = V(0, 0.25f, 0);
        var r = V(0.24f, 0.24f, 0.23f);
        b.Ell(Body, c, r, shell);
        // Pocked all over
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 16; i++)
        {
            float u = -0.9f + 1.8f * (i + 0.5f) / 16f, ring = MathF.Sqrt(1f - u * u), a = i * golden;
            if (MathF.Abs(u) < 0.35f) continue;
            var at = c + V(MathF.Sin(a) * ring * r.X, u * r.Y, MathF.Cos(a) * ring * r.Z);
            b.Cut(Body, at, V(0.028f, 0.028f, 0.028f));
        }
        // The seam where its shell opens, red-rimmed and dark inside, its eyes peering from it
        b.PaintEll(Body, c, V(0.3f, 0.055f, 0.3f), dark, soft: 0.006f);
        foreach (float y in new[] { -0.065f, 0.065f })
            b.Torus(Body, c + V(0, y, 0), 0.232f, 0.018f, red, mat: Shell);
        PokeBuilder.Both(s =>
        {
            var look = V(0.25f * s, 0f, 1f);
            b.Eye(Body, Out(c, r, default, look), look, 0.03f, sclera: true, pupil: Black, white: Rgb(240, 226, 200));
            b.Limb(Body, c + V(0.2f * s, 0, 0), c + V(0.36f * s, 0, 0), 0.042f, 0.032f, red, Shell);
            b.Ell(Body, c + V(0.36f * s, 0, 0), V(0.012f, 0.03f, 0.03f), red, mat: Shell, blend: 0.006f);
        });
        return b;
    }

    // ------------------------------------------------------------------ Dunsparce

    /// <summary>Dunsparce: a yellow grub lying low, blue bands over its back, two little wings and a drill for a tail.</summary>
    private static PokeBuilder Dunsparce()
    {
        var b = new PokeBuilder("Dunsparce", 0.6f, BodyPlan.Floating, V(0, 0.12f, -0.02f)) { Coat = Scales };
        var yellow = Rgb(242, 224, 110);
        var blue = Rgb(70, 150, 196);
        var pale = Rgb(250, 242, 206);
        b.Ell(Body, V(0, 0.12f, -0.02f), V(0.12f, 0.11f, 0.17f), yellow);
        foreach (float z in new[] { -0.09f, -0.035f, 0.02f })
            b.PaintEll(Body, V(0, 0.21f, z), V(0.14f, 0.08f, 0.016f), blue, soft: 0.008f);
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, V(0.05f * s, 0.21f, 0));
            Frond(b, wing, V(0.04f * s, 0.21f, 0.01f), V(0.12f * s, 0.29f, -0.05f), 0.035f, White, V(s, 0.3f, 0), 0.25f);
        });
        // The drill
        int tail = b.Tail(V(0, 0.12f, -0.18f));
        var path = Smooth(3, V(0, 0.12f, -0.16f), V(0, 0.13f, -0.26f), V(0, 0.2f, -0.32f));
        b.Tube(tail, path, 0.06f, 0.042f, yellow, blend: 0f);
        var tip = V(0, 0.36f, -0.38f);
        b.Spike(tail, path[^1], tip, 0.048f, yellow, mat: Shell);
        var axis = tip - path[^1];
        foreach (float f in new[] { 0.2f, 0.45f, 0.7f })
            b.PaintTorus(tail, path[^1] + axis * f, 0.048f * (1f - f), 0.008f, blue, Euler(axis));
        int head = b.Head(V(0, 0.15f, 0.1f));
        var hc = V(0, 0.17f, 0.16f);
        var hr = V(0.11f, 0.105f, 0.1f);
        b.Ell(head, hc, hr, yellow);
        b.PaintEll(head, hc + V(0, -0.01f, 0.09f), V(0.065f, 0.065f, 0.03f), pale);
        b.Mark(head, Out(hc, hr, default, V(0, -0.1f, 1f)), V(0, -0.1f, 1f), 0.07f, 0.07f, blue, MarkShape.Ring);
        b.Mark(head, Out(hc, hr, default, V(0, -0.1f, 1f)), V(0, -0.1f, 1f), 0.04f, 0.006f, Rgb(80, 70, 60), MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            var look = V(0.6f * s, 0.4f, 0.7f);
            b.Eye(head, Out(hc, hr, default, look), look, 0.02f, sclera: true, pupil: Black);
        });
        return b;
    }

    // ------------------------------------------------------------------ Snubbull and Granbull

    private static PokeBuilder Snubbull()
    {
        var b = new PokeBuilder("Snubbull", 0.55f, BodyPlan.Biped, V(0, 0.15f, 0)) { Coat = Fur };
        var pink = Rgb(246, 168, 190);
        var muzzle = Rgb(234, 140, 168);
        var blue = Rgb(130, 180, 230);
        var black = Rgb(52, 44, 54);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.1f, 0));
            b.Limb(leg, V(0.05f * s, 0.1f, 0), V(0.06f * s, 0.03f, 0.01f), 0.035f, 0.03f, pink);
            b.Ell(leg, V(0.062f * s, 0.02f, 0.04f), V(0.035f, 0.02f, 0.045f), Rgb(250, 220, 200));
        });
        // A body flaring like a dress, spotted blue
        b.Ell(Body, V(0, 0.16f, 0), V(0.1f, 0.1f, 0.09f), pink);
        b.Ell(Body, V(0, 0.1f, 0), V(0.13f, 0.05f, 0.12f), pink, blend: 0.04f);
        foreach (var (x, z) in new[] { (-0.06f, 0.1f), (0.07f, 0.09f), (0.12f, -0.02f), (-0.12f, -0.03f), (0.0f, -0.12f) })
        {
            var look = V(x, -0.1f, z);
            b.Mark(Body, Out(V(0, 0.1f, 0), V(0.13f, 0.05f, 0.12f), default, look) + Vector3.Normalize(look) * 0.01f, look, 0.02f, 0.016f, blue);
        }
        b.Torus(Body, V(0, 0.24f, 0.01f), 0.075f, 0.018f, blue, mat: Fur);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.21f, 0.02f));
            b.Limb(arm, V(0.08f * s, 0.21f, 0.02f), V(0.15f * s, 0.26f, 0.05f), 0.026f, 0.022f, pink);
        });
        int head = b.Head(V(0, 0.27f, 0.02f));
        var c = V(0, 0.34f, 0.02f);
        var r = V(0.12f, 0.105f, 0.105f);
        b.Ell(head, c, r, pink);
        b.Ell(head, c + V(0, -0.045f, 0.08f), V(0.07f, 0.045f, 0.05f), muzzle, blend: 0.02f);
        b.Ell(head, c + V(0, -0.02f, 0.13f), V(0.02f, 0.014f, 0.012f), Rgb(120, 60, 80), blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.035f * s, -0.075f, 0.12f), c + V(0.038f * s, -0.035f, 0.13f), 0.012f, White, mat: Shell, blend: 0.004f);
            int ear = b.Ear(head, s, c + V(0.1f * s, 0.07f, -0.02f));
            b.Ell(ear, c + V(0.14f * s, 0.09f, -0.02f), V(0.06f, 0.06f, 0.022f), black, V(0, 0, -30f * s));
            var look = V(0.4f * s, 0.25f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.024f, sclera: true, pupil: Black, glare: true);
        });
        return b;
    }

    private static PokeBuilder Granbull()
    {
        var b = new PokeBuilder("Granbull", 0.8f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var purple = Rgb(204, 164, 214);
        var black = Rgb(52, 44, 54);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.2f, 0));
            b.Limb(leg, V(0.09f * s, 0.21f, 0), V(0.1f * s, 0.05f, 0.02f), 0.06f, 0.05f, purple);
            b.Ell(leg, V(0.1f * s, 0.03f, 0.06f), V(0.05f, 0.03f, 0.07f), purple);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.1f * s + 0.02f * i, 0.02f, 0.115f), V(0.1f * s + 0.024f * i, 0.012f, 0.14f), 0.01f, White, mat: Shell, blend: 0.004f);
        });
        b.Ell(Body, V(0, 0.36f, 0), V(0.15f, 0.2f, 0.13f), purple);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.46f, 0.03f));
            var hand = V(0.22f * s, 0.38f, 0.12f);
            b.Limb(arm, V(0.12f * s, 0.47f, 0.03f), hand, 0.045f, 0.04f, purple);
            b.Torus(arm, Vector3.Lerp(V(0.12f * s, 0.47f, 0.03f), hand, 0.75f), 0.04f, 0.012f, black, Euler(hand - V(0.12f * s, 0.47f, 0.03f)));
            b.Ell(arm, hand, V(0.045f, 0.042f, 0.045f), purple);
        });
        // A great underbite, the jaw lined black, two fangs standing up beside its lip
        int head = b.Head(V(0, 0.52f, 0.04f));
        var c = V(0, 0.62f, 0.06f);
        var r = V(0.11f, 0.09f, 0.1f);
        b.Ell(head, c, r, purple);
        b.Ell(head, c + V(0, -0.075f, 0.06f), V(0.105f, 0.06f, 0.075f), purple, blend: 0.03f);
        b.PaintEll(head, c + V(0, -0.125f, 0.03f), V(0.12f, 0.035f, 0.1f), black);
        b.PaintEll(head, c + V(0, -0.04f, 0.13f), V(0.07f, 0.014f, 0.03f), Rgb(170, 80, 110), soft: 0.006f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.06f * s, -0.06f, 0.11f), c + V(0.07f * s, 0.03f, 0.1f), 0.016f, White, mat: Shell, blend: 0.004f);
            int ear = b.Ear(head, s, c + V(0.07f * s, 0.06f, -0.02f));
            Frond(b, ear, c + V(0.06f * s, 0.06f, -0.02f), c + V(0.2f * s, 0.09f, -0.08f), 0.05f, purple, V(0, 1f, 0.3f), 0.25f);
            var look = V(0.4f * s, 0.35f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.018f, sclera: true, pupil: Black, glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Qwilfish

    /// <summary>
    /// Qwilfish and its Hisuian form: a round puffer bristling with spikes, a pouting mouth, a round tail fin; the
    /// Hisuian dark, its belly cream and its spikes purple.
    /// </summary>
    private static PokeBuilder QwilfishBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Qwilfish-Hisui" : "Qwilfish", 0.55f, BodyPlan.Fish, V(0, 0.25f, 0)) { Coat = Scales }.Hover();
        var back = hisui ? Rgb(44, 44, 74) : Rgb(62, 112, 112);
        var belly = hisui ? Rgb(232, 228, 204) : Rgb(204, 222, 150);
        var spikes = hisui ? Rgb(150, 92, 190) : back;
        var c = V(0, 0.25f, 0);
        var r = V(0.17f, 0.16f, 0.16f);
        b.Ell(Body, c, r, back);
        b.PaintEll(Body, c + V(0, -0.08f, 0.05f), V(0.18f, 0.12f, 0.16f), belly);
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < 22; i++)
        {
            float u = -0.85f + 1.7f * (i + 0.5f) / 22f, ring = MathF.Sqrt(1f - u * u), a = i * golden;
            var dir = V(MathF.Sin(a) * ring, u, MathF.Cos(a) * ring);
            if (dir.Z > 0.55f && dir.Y > -0.4f) continue;
            if (dir.Z < -0.8f && MathF.Abs(dir.Y) < 0.4f) continue;
            var root = c + dir * r * 0.95f;
            b.Spike(Body, root, root + dir * 0.06f, 0.022f, dir.Y < -0.2f && !hisui ? belly : spikes, mat: Shell);
        }
        // The tail fin, round and marked
        int tail = b.Tail(c + V(0, 0, -0.14f));
        b.Limb(tail, c + V(0, 0, -0.12f), c + V(0, 0.01f, -0.22f), 0.035f, 0.028f, back);
        var fin = c + V(0, 0.02f, -0.29f);
        b.Ell(tail, fin, V(0.016f, 0.07f, 0.08f), back);
        PokeBuilder.Both(s => b.Mark(tail, fin + V(0.016f * s, 0, 0), V(s, 0, 0), 0.035f, 0.045f, hisui ? belly : Rgb(170, 200, 130), hisui ? MarkShape.Ring : MarkShape.Disc));
        var mouth = Out(c, r, default, V(0, -0.15f, 1f));
        b.Torus(Body, mouth, 0.026f, 0.014f, hisui ? Rgb(200, 120, 190) : Rgb(236, 120, 140), V(90f, 0, 0));
        PokeBuilder.Both(s =>
        {
            var look = V(0.55f * s, 0.35f, 0.75f);
            b.Eye(Body, Out(c, r, default, look), look, 0.03f, sclera: true, pupil: Black, glare: hisui);
        });
        return Lift(b);
    }

    private static PokeBuilder Qwilfish() => QwilfishBuild(false);

    // ------------------------------------------------------------------ Shuckle

    /// <summary>Shuckle: a red shell pierced with pale holes, yellow legs and a long yellow neck poking out of them.</summary>
    private static PokeBuilder Shuckle()
    {
        var b = new PokeBuilder("Shuckle", 0.55f, BodyPlan.Quadruped, V(0, 0.17f, -0.02f)) { Coat = Shell };
        var red = Rgb(198, 60, 56);
        var cream = Rgb(242, 222, 172);
        var yellow = Rgb(242, 208, 82);
        var c = V(0, 0.17f, -0.02f);
        var r = V(0.16f, 0.13f, 0.17f);
        b.Ell(Body, c, r, red);
        foreach (var d in new[] { V(0, 1f, 0), V(0.7f, 0.6f, 0.3f), V(-0.7f, 0.6f, 0.3f), V(0.7f, 0.6f, -0.5f), V(-0.7f, 0.6f, -0.5f), V(0, 0.5f, -1f), V(0, 0.8f, 0.6f) })
        {
            var at = Out(c, r, default, d);
            b.PaintEll(Body, at, V(0.042f, 0.042f, 0.042f), cream);
            b.PaintEll(Body, at, V(0.02f, 0.02f, 0.02f), Rgb(70, 34, 30), soft: 0.006f);
        }
        foreach (var (z, front) in new[] { (0.06f, true), (-0.1f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.13f * s, 0.1f, z), front);
                var foot = V(0.24f * s, 0.03f, z + (front ? 0.06f : -0.06f));
                b.Limb(leg, V(0.12f * s, 0.1f, z), foot, 0.03f, 0.028f, yellow);
                b.Ell(leg, foot, V(0.032f, 0.03f, 0.032f), yellow);
            });
        int head = b.Head(V(0, 0.2f, 0.12f));
        b.Tube(head, Smooth(3, V(0, 0.17f, 0.11f), V(0, 0.29f, 0.17f), V(0, 0.37f, 0.17f)), 0.035f, 0.032f, yellow, blend: 0f);
        var hc = V(0, 0.4f, 0.19f);
        var hr = V(0.05f, 0.045f, 0.055f);
        b.Ell(head, hc, hr, yellow);
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.2f, 1f);
            b.Eye(head, Out(hc, hr, default, look), look, 0.012f, iris: Black);
        });
        return b;
    }

    // ------------------------------------------------------------------ Teddiursa and Ursaring

    private static PokeBuilder Teddiursa()
    {
        var b = new PokeBuilder("Teddiursa", 0.55f, BodyPlan.Biped, V(0, 0.2f, 0)) { Coat = Fur };
        var orange = Rgb(226, 140, 70);
        var cream = Rgb(250, 214, 140);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.06f * s, 0.12f, 0));
            b.Limb(leg, V(0.06f * s, 0.12f, 0), V(0.065f * s, 0.035f, 0.01f), 0.04f, 0.035f, orange);
            b.Ell(leg, V(0.066f * s, 0.022f, 0.04f), V(0.036f, 0.022f, 0.046f), orange);
            b.PaintEll(leg, V(0.066f * s, 0.022f, 0.08f), V(0.025f, 0.018f, 0.012f), cream);
        });
        b.Ell(Body, V(0, 0.2f, 0), V(0.1f, 0.11f, 0.09f), orange);
        b.Ell(Body, V(0, 0.15f, -0.09f), V(0.03f, 0.03f, 0.03f), orange);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.25f, 0.03f));
            var hand = s > 0 ? V(0.035f, 0.32f, 0.11f) : V(-0.12f, 0.18f, 0.06f);
            b.Limb(arm, V(0.08f * s, 0.25f, 0.03f), hand, 0.03f, 0.026f, orange);
            b.Ell(arm, hand, V(0.03f, 0.03f, 0.03f), orange);
        });
        int head = b.Head(V(0, 0.3f, 0.02f));
        var c = V(0, 0.38f, 0.02f);
        var r = V(0.1f, 0.09f, 0.09f);
        b.Ell(head, c, r, orange);
        b.Ell(head, c + V(0, -0.03f, 0.08f), V(0.045f, 0.035f, 0.035f), cream, blend: 0.02f);
        b.Ell(head, c + V(0, -0.015f, 0.115f), V(0.014f, 0.01f, 0.008f), Black, blend: 0.004f);
        // The crescent moon on its forehead
        b.PaintEll(head, c + V(0, 0.05f, 0.08f), V(0.05f, 0.05f, 0.04f), cream);
        b.PaintEll(head, c + V(0.012f, 0.068f, 0.085f), V(0.042f, 0.042f, 0.04f), orange);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.07f * s, 0.07f, -0.01f));
            b.Ell(ear, c + V(0.08f * s, 0.085f, -0.01f), V(0.04f, 0.04f, 0.025f), orange);
            b.PaintEll(ear, c + V(0.08f * s, 0.085f, 0.012f), V(0.022f, 0.022f, 0.015f), cream);
            var look = V(0.4f * s, 0.15f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.022f, sclera: true, pupil: Black);
        });
        return b;
    }

    private static PokeBuilder Ursaring()
    {
        var b = new PokeBuilder("Ursaring", 1f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var brown = Rgb(132, 92, 62);
        var ring = Rgb(240, 206, 110);
        var muzzle = Rgb(222, 192, 142);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.24f, 0));
            b.Limb(leg, V(0.11f * s, 0.25f, 0), V(0.12f * s, 0.06f, 0.02f), 0.07f, 0.06f, brown);
            b.Ell(leg, V(0.12f * s, 0.035f, 0.06f), V(0.06f, 0.035f, 0.08f), brown);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.12f * s + 0.024f * i, 0.025f, 0.125f), V(0.12f * s + 0.028f * i, 0.012f, 0.155f), 0.011f, White, mat: Shell, blend: 0.004f);
        });
        b.Ell(Body, V(0, 0.42f, 0), V(0.19f, 0.25f, 0.16f), brown);
        b.PaintTorus(Body, V(0, 0.38f, 0.145f), 0.09f, 0.015f, ring, V(90f, 0, 0));
        // A shaggy collar over its shoulders
        for (int i = 0; i < 10; i++)
        {
            float a = (i + 0.5f) * MathF.Tau / 10f;
            var o = V(MathF.Sin(a), 0, MathF.Cos(a));
            Frond(b, Body, V(0, 0.6f, 0) + o * V(0.13f, 0, 0.1f), V(0, 0.5f, 0) + o * V(0.21f, 0, 0.18f), 0.05f, brown, o + V(0, 0.5f, 0), 0.25f, Fur);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.17f * s, 0.55f, 0.03f));
            var hand = V(0.27f * s, 0.36f, 0.1f);
            b.Limb(arm, V(0.16f * s, 0.55f, 0.03f), hand, 0.055f, 0.045f, brown);
            b.Ell(arm, hand, V(0.045f, 0.045f, 0.045f), brown);
            Digits(b, arm, hand + V(0.01f * s, -0.03f, 0.01f), V(0.1f * s, -1f, 0.3f), V(0.35f, 0, 0), 0.05f, 0.01f, Claw, Shell);
        });
        int head = b.Head(V(0, 0.62f, 0.02f));
        b.Limb(head, V(0, 0.6f, 0.02f), V(0, 0.72f, 0.04f), 0.08f, 0.07f, brown);
        var c = V(0, 0.75f, 0.05f);
        var r = V(0.1f, 0.09f, 0.09f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.03f, 0.08f), V(0.05f, 0.04f, 0.045f), muzzle, blend: 0.02f);
        b.Ell(head, c + V(0, -0.01f, 0.125f), V(0.018f, 0.012f, 0.01f), Black, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.07f * s, 0.07f, -0.01f));
            b.Ell(ear, c + V(0.075f * s, 0.085f, -0.01f), V(0.032f, 0.032f, 0.022f), brown);
            var look = V(0.4f * s, 0.2f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.018f, sclera: true, pupil: Black, glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Slugma and Magcargo

    /// <summary>Slugma and Magcargo: a slug of red magma rising from a puddle of itself, yellow eyes bulging; Magcargo with a shell of cooled rock on its back, cracked with fire.</summary>
    private static PokeBuilder SlugBuild(bool magcargo)
    {
        var b = new PokeBuilder(magcargo ? "Magcargo" : "Slugma", magcargo ? 0.7f : 0.6f, BodyPlan.Floating, V(0, 0.1f, 0)) { Coat = Scales };
        var red = Rgb(228, 92, 62);
        var deep = Rgb(196, 64, 50);
        var yellow = Rgb(248, 226, 100);
        b.Ell(Body, V(0, 0.035f, 0), V(0.17f, 0.035f, 0.17f), red);
        foreach (var (x, z) in new[] { (0.1f, 0.08f), (-0.11f, 0.04f), (0.05f, -0.12f), (-0.07f, -0.1f) })
            b.PaintEll(Body, V(x, 0.07f, z), V(0.02f, 0.012f, 0.02f), deep, soft: 0.006f);
        b.Limb(Body, V(0, 0.1f, -0.03f), V(0, 0.22f, 0.03f), 0.1f, 0.08f, red, blend: 0.04f);
        int head = b.Head(V(0, 0.24f, 0.03f));
        var c = V(0, 0.3f, 0.05f);
        var r = V(0.1f, 0.08f, 0.09f);
        b.Ell(head, c, r, red);
        // Drips down its front, flame-like locks on its head
        foreach (var (x, len) in new[] { (-0.06f, 0.07f), (0.07f, 0.05f), (0.0f, 0.04f) })
            b.Limb(head, c + V(x, -0.02f, 0.07f), c + V(x * 1.1f, -0.02f - len, 0.08f), 0.018f, 0.014f, red, blend: 0.015f);
        foreach (var (x, z) in new[] { (-0.04f, -0.02f), (0.04f, -0.03f), (0.0f, -0.06f) })
            FlameTongue(b, head, c + V(x, r.Y * 0.7f, z), c + V(x * 2f, r.Y + 0.08f, z - 0.05f), 0.03f, red, Rgb(240, 120, 80));
        PokeBuilder.Both(s =>
        {
            var bulge = c + V(0.05f * s, 0.035f, 0.06f);
            var br = V(0.04f, 0.035f, 0.035f);
            b.Ell(head, bulge, br, red, blend: 0.015f);
            b.Eye(head, Out(bulge, br, default, V(0.2f * s, 0.1f, 1f)), V(0.2f * s, 0.1f, 1f), 0.026f, sclera: true, white: yellow, pupil: Black);
        });
        b.Mark(head, Out(c, r, default, V(0, -0.45f, 1f)), V(0, -0.45f, 1f), 0.025f, 0.01f, deep, MarkShape.Smile);
        if (magcargo)
        {
            // The shell of rock on its back, cracked with glowing seams, a flame from its top
            var shell = Rgb(96, 98, 112);
            var sc = V(0, 0.24f, -0.12f);
            var sr = V(0.14f, 0.15f, 0.13f);
            int rock = b.Part("shell", Body, sc, PokeRole.Body);
            b.Ell(rock, sc, sr, shell, mat: Shell);
            Lumps(b, rock, sc, sr * 0.95f, 14, 0.035f, shell, Rgb(70, 72, 86), 99f);
            foreach (var turn in new[] { V(30f, 0, 20f), V(-25f, 40f, 70f), V(80f, 20f, -30f) })
                b.PaintEll(rock, sc, V(0.25f, 0.007f, 0.25f), Rgb(250, 140, 50), turn, soft: 0.005f);
            FlameTongue(b, rock, sc + V(0, sr.Y * 0.8f, 0), sc + V(0.03f, sr.Y + 0.12f, -0.03f), 0.035f, Rgb(244, 100, 50), Rgb(255, 214, 90));
        }
        return b;
    }

    private static PokeBuilder Slugma() => SlugBuild(false);

    private static PokeBuilder Magcargo() => SlugBuild(true);

    // ------------------------------------------------------------------ Corsola

    /// <summary>Corsola and its Galarian form: a round pink coral on four stubby feet, branches of coral up from its back; the Galarian ghost-white and grey, its branches thin and broken, its look forlorn.</summary>
    private static PokeBuilder CorsolaBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Corsola-Galar" : "Corsola", 0.55f, BodyPlan.Quadruped, V(0, 0.17f, 0)) { Coat = Shell };
        var coral = galar ? Rgb(236, 236, 240) : Rgb(246, 140, 160);
        var under = galar ? Rgb(212, 212, 222) : Rgb(182, 222, 240);
        var c = V(0, 0.18f, 0);
        var r = V(0.14f, 0.13f, 0.14f);
        StubbyLegs(b, 0.07f, 0.1f, 0.06f, -0.06f, 0.035f, under);
        b.Ell(Body, c, r, coral);
        b.PaintEll(Body, c + V(0, -0.11f, 0), V(0.17f, 0.07f, 0.17f), under);
        if (galar)
            foreach (var d in new[] { V(0.6f, 0.4f, 0.4f), V(-0.7f, 0.1f, -0.3f), V(0.1f, 0.6f, -0.8f) })
                b.PaintEll(Body, Out(c, r, default, d), V(0.035f, 0.03f, 0.035f), Rgb(170, 170, 184));
        float k = galar ? 0.8f : 1f;
        int branches = b.Part("coral", Body, c + V(0, r.Y, 0), PokeRole.Leaf);
        PokeBuilder.Both(s =>
        {
            Coral(b, branches, c + V(0.05f * s, 0.1f, -0.01f), c + V(0.2f * s, 0.3f, -0.03f), 0.03f * k, coral, V(0.07f * s, 0.03f, 0.02f));
            Coral(b, branches, c + V(0.11f * s, 0.03f, -0.03f), c + V(0.26f * s, 0.09f, -0.05f), 0.025f * k, coral, V(0.01f * s, 0.06f, 0));
            if (galar)
                b.Limb(branches, c + V(0.17f * s, 0.25f, -0.03f), c + V(0.15f * s, 0.33f, -0.01f), 0.016f, 0.012f, coral, blend: 0.01f);
        });
        Coral(b, branches, c + V(0, 0.1f, -0.06f), c + V(0, 0.29f, -0.13f), 0.028f * k, coral, V(0.05f, 0.05f, -0.03f));
        PokeBuilder.Both(s =>
        {
            var look = V(0.35f * s, -0.05f, 1f);
            if (galar) b.Eye(Body, Out(c, r, default, look), look, 0.022f, Rgb(120, 120, 140), sclera: true, pupil: Rgb(60, 60, 80));
            else b.Eye(Body, Out(c, r, default, look), look, 0.022f, Rgb(60, 40, 60));
        });
        b.Mark(Body, Out(c, r, default, V(0, -0.3f, 1f)), V(0, -0.3f, 1f), 0.018f, 0.008f, galar ? Rgb(110, 110, 124) : Rgb(170, 60, 80), MarkShape.Smile, galar ? 180f : 0f);
        return b;
    }

    private static PokeBuilder Corsola() => CorsolaBuild(false);
}
