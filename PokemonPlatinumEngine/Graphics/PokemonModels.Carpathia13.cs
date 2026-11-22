using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 13 (plan 46 · CP4 to CP8): Furtunaur to Nazdravan (9 species), the legendaries and the mythicals.
// Our own designs from the briefs of docs/plans/decisions/CP-dex-6-legends-and-mythicals.md. The three brothers of the
// night (Murgila, Miezila, Zorila) share one kit: a mane and a tail of cloud puffs with stars caught in them.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames13() => new[]
    {
        "Furtunaur", "Stindrac", "Ielele", "Mumapadura", "Maiastra", "Murgila", "Miezila", "Zorila", "Nazdravan"
    };

    private static PokeBuilder? CreateCarpathia13(string species) => species switch
    {
        "FURTUNAUR" => C13Furtunaur(),
        "STINDRAC" => C13Stindrac(),
        "IELELE" => C13Ielele(),
        "MUMAPADURA" => C13Mumapadura(),
        "MAIASTRA" => C13Maiastra(),
        "MURGILA" => C13Murgila(),
        "MIEZILA" => C13Miezila(),
        "ZORILA" => C13Zorila(),
        "NAZDRAVAN" => C13Nazdravan(),
        _ => null
    };

    /// <summary>A vector turned about the vertical by <paramref name="yaw"/> degrees (as an ellipsoid's yaw turns it).</summary>
    private static Vector3 C13Yaw(Vector3 v, float yaw) => Vector3.Transform(v, Quaternion.CreateFromYawPitchRoll(yaw * Degree, 0, 0));

    // ------------------------------------------------------------------ Furtunaur

    /// <summary>
    /// Furtunaur, the storm-dragon the solomonari ride: a long dragon of storm-grey scales hovering on wings like
    /// torn storm clouds, its belly and throats cloud-white, three heads on long necks, lightning in pale spines down
    /// its back, a rider's harness of braided lightning round its chest with the old wizard's book chained to its
    /// side, and a long tail curling under it. Only the middle head's eyes are eyes; the side heads' glow as marks.
    /// </summary>
    private static PokeBuilder C13Furtunaur()
    {
        var b = new PokeBuilder("Furtunaur", 1f, BodyPlan.Bird, V(0, 0.55f, 0)) { Coat = Scales }.Hover();
        var grey = Rgb(84, 92, 110);
        var dark = Rgb(58, 62, 78);
        var cloud = Rgb(226, 230, 238);
        var storm = Rgb(128, 138, 160);
        var bolt = Rgb(150, 220, 255);
        var leather = Rgb(122, 74, 44);
        var bc = V(0, 0.55f, 0);
        // Wings first, for their membranes are carved: torn storm cloud between dark struts
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.08f * s, 0.1f, -0.05f);
            int wing = b.Wing(s, shoulder);
            var wrist = bc + V(0.3f * s, 0.33f, -0.1f);
            var tips = new[] { bc + V(0.62f * s, 0.38f, -0.14f), bc + V(0.62f * s, 0.18f, -0.15f), bc + V(0.52f * s, 0.02f, -0.14f), bc + V(0.36f * s, -0.06f, -0.12f) };
            BatMembrane(b, wing, shoulder, wrist, tips, bc + V(0.1f * s, -0.02f, -0.1f), storm);
            BatStruts(b, wing, shoulder, wrist, tips, dark, 0.02f);
            b.Spike(wing, wrist, wrist + V(0.02f * s, 0.07f, 0), 0.016f, Claw, mat: Shell);
            // Lightning crackling along the membrane
            foreach (var t in tips[..3])
                b.Mark(wing, Vector3.Lerp(wrist, t, 0.55f) + V(0, 0, 0.012f), V(0, 0, 1f), 0.035f, 0.012f, bolt, MarkShape.Zigzag, MathF.Atan2(t.Y - wrist.Y, (t.X - wrist.X) * s) * 57.3f * s);
        });
        var br = V(0.13f, 0.12f, 0.22f);
        b.Ell(Body, bc, br, grey);
        b.Ell(Body, bc + V(0, 0.02f, 0.15f), V(0.12f, 0.13f, 0.1f), grey, blend: 0.04f);
        b.PaintEll(Body, bc + V(0, -0.09f, 0.04f), V(0.11f, 0.07f, 0.24f), cloud);
        // Lightning in the spines down its back
        for (int i = 0; i < 5; i++)
        {
            var at = Out(bc, br, default, V(0, 1f, 0.55f - 0.3f * i));
            Blade(b, Body, at - V(0, 0.02f, 0), at + V(0, 0.07f - 0.008f * i, -0.04f), 0.024f, i % 2 == 0 ? bolt : storm, V(1f, 0, 0), 0.3f, Glow);
        }
        // The harness of braided lightning round its chest, and the solomonar's book chained to its side
        b.Torus(Body, bc + V(0, 0, 0.07f), 0.125f, 0.014f, bolt, V(90f, 0, 0), 1f, 0.95f, Glow);
        b.Torus(Body, bc + V(0, 0.005f, 0.1f), 0.122f, 0.009f, cloud, V(90f, 0, 0), 1f, 0.95f, Glow);
        var book = bc + V(0.125f, 0.0f, -0.07f);
        b.Box(Body, book, V(0.022f, 0.055f, 0.045f), 0.008f, leather, V(0, 0, -6f), Shell);
        b.Box(Body, book + V(0.018f, 0, 0), V(0.008f, 0.048f, 0.04f), 0.004f, Rgb(236, 224, 196), V(0, 0, -6f), Shell);
        foreach (float y in new[] { 0.03f, -0.03f })
            b.Torus(Body, book + V(0.012f, y, 0), 0.026f, 0.006f, Rgb(170, 176, 190), V(0, 0, 90f), 1f, 1.6f, Metal);
        b.Limb(Body, book + V(0.0f, 0.05f, 0.03f), bc + V(0.08f, 0.09f, 0.08f), 0.006f, 0.006f, Rgb(170, 176, 190), Metal, 0.004f);
        // Four legs hanging under it, clawed
        foreach (var (z, front) in new[] { (0.12f, true), (-0.12f, false) })
            PokeBuilder.Both(s =>
            {
                var hip = bc + V(0.09f * s, -0.05f, z);
                int leg = b.Leg(s, hip, front);
                var knee = hip + V(0.03f * s, -0.12f, front ? 0.04f : -0.04f);
                var paw = knee + V(0.0f, -0.1f, front ? 0.02f : 0.03f);
                b.Limb(leg, hip, knee, 0.05f, 0.035f, grey);
                b.Limb(leg, knee, paw, 0.032f, 0.025f, grey);
                foreach (float dx in new[] { -0.018f, 0f, 0.018f })
                    b.Spike(leg, paw + V(dx, 0, 0.01f), paw + V(dx * 1.4f, -0.04f, 0.04f), 0.01f, Claw, mat: Shell);
            });
        // The long tail, curling down and under, its underside cloud-white and its tip a tuft of lightning
        int tail = b.Tail(bc + V(0, -0.02f, -0.2f));
        var tp = Smooth(3, bc + V(0, -0.02f, -0.2f), bc + V(0, -0.1f, -0.34f), bc + V(0.04f, -0.28f, -0.4f), bc + V(0.06f, -0.44f, -0.28f));
        b.Tube(tail, tp, 0.085f, 0.05f, grey, blend: 0f);
        int tail2 = b.Part("tail2", tail, tp[^1], PokeRole.Tail, 1f);
        var tq = Smooth(3, tp[^1], bc + V(0.03f, -0.5f, -0.1f), bc + V(-0.05f, -0.46f, 0.02f), bc + V(-0.1f, -0.36f, -0.02f));
        b.Tube(tail2, tq, 0.05f, 0.02f, grey, blend: 0f);
        for (int i = 1; i < tp.Length - 1; i += 2)
            b.PaintEll(tail, tp[i] + V(0, -0.04f, 0.03f), V(0.05f, 0.04f, 0.05f), cloud);
        foreach (var (dx, dy) in new[] { (0f, 0.08f), (-0.04f, 0.05f), (0.04f, 0.06f) })
            Blade(b, tail2, tq[^1], tq[^1] + V(-0.06f + dx, dy, 0.02f), 0.024f, bolt, V(0, 0, 1f), 0.3f, Glow);
        // Three heads on long necks: the middle one high, the two others turned out to the sides
        foreach (var (x, yaw, eyes) in new[] { (0f, 0f, true), (-1f, -28f, false), (1f, 28f, false) })
        {
            var root = bc + V(0.07f * x, 0.05f, 0.16f);
            var c = eyes ? bc + V(0, 0.46f, 0.2f) : bc + V(0.27f * x, 0.3f, 0.28f);
            int head = b.Part(x < 0 ? "headL" : x > 0 ? "headR" : "head", Body, root, PokeRole.Head, eyes ? 0f : 1f + x, x);
            var neck = eyes
                ? Smooth(3, root, bc + V(0, 0.2f, 0.22f), bc + V(0, 0.35f, 0.15f), c + V(0, -0.02f, -0.04f))
                : Smooth(3, root, bc + V(0.15f * x, 0.13f, 0.23f), bc + V(0.24f * x, 0.21f, 0.22f), c + V(0, -0.02f, -0.04f));
            b.Tube(head, neck, 0.055f, 0.04f, grey, blend: 0f);
            for (int i = 2; i < neck.Length - 1; i += 2)
                b.PaintEll(head, neck[i] + C13Yaw(V(0, 0, 0.035f), yaw), V(0.035f, 0.03f, 0.025f), cloud);
            C13StormHead(b, head, c, yaw, eyes, grey, dark, cloud, bolt);
        }
        return Lift(b);
    }

    /// <summary>A head of Furtunaur's at <paramref name="c"/>, turned by <paramref name="yaw"/>: a long snout, a crest of spikes and two horns swept back, and eyes or glowing marks.</summary>
    private static void C13StormHead(PokeBuilder b, int head, Vector3 c, float yaw, bool eyes, Color grey, Color dark, Color cloud, Color bolt)
    {
        var r = V(0.055f, 0.05f, 0.065f);
        b.Ell(head, c, r, grey, V(0, yaw, 0));
        var snout = c + C13Yaw(V(0, -0.012f, 0.075f), yaw);
        b.Ell(head, snout, V(0.036f, 0.028f, 0.055f), grey, V(6f, yaw, 0), blend: 0.02f);
        b.Ell(head, c + C13Yaw(V(0, -0.04f, 0.055f), yaw), V(0.03f, 0.016f, 0.05f), cloud, V(10f, yaw, 0), blend: 0.012f);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + C13Yaw(V(0.025f * s, 0.035f, -0.01f), yaw), c + C13Yaw(V(0.05f * s, 0.11f, -0.1f), yaw), 0.014f, Rgb(200, 204, 214), mat: Shell);
            b.Spike(head, snout + C13Yaw(V(0.018f * s, -0.024f, 0.035f), yaw), snout + C13Yaw(V(0.02f * s, -0.05f, 0.04f), yaw), 0.006f, Claw, mat: Shell, blend: 0.004f);
            var look = C13Yaw(V(0.62f * s, 0.32f, 0.72f), yaw);
            var at = Out(c, r, V(0, yaw, 0), look);
            var n = Vector3.Normalize(look);
            if (eyes) b.Eye(head, at, n, 0.013f, Rgb(250, 228, 90), glare: true);
            else b.Mark(head, at, n, 0.014f, 0.008f, Rgb(250, 228, 90), MarkShape.Diamond, 15f * s);
        });
        // A crest of spikes running back, the lightning in it
        for (int i = 0; i < 4; i++)
        {
            var root = c + C13Yaw(V(0, 0.04f - 0.01f * i, -0.02f - 0.025f * i), yaw);
            Blade(b, head, root, root + C13Yaw(V(0, 0.05f - 0.005f * i, -0.06f), yaw), 0.018f, i % 2 == 0 ? dark : bolt, C13Yaw(V(1f, 0, 0), yaw), 0.3f);
        }
    }

    // ------------------------------------------------------------------ Stindrac

    /// <summary>
    /// Stindrac, the Dacian war banner come alive: a bronze wolf's head howling with its jaws wide, horns swept back,
    /// a neck of bronze plates, and behind it a long body of woven cloth in crimson, gold and green bands, held by
    /// bronze rings, rippling through the air in an S and ending in a spray of torn streamers.
    /// </summary>
    private static PokeBuilder C13Stindrac()
    {
        var b = new PokeBuilder("Stindrac", 1f, BodyPlan.Serpent, V(0, 0.2f, -0.34f)) { Coat = Fur }.Hover();
        var bronze = Rgb(170, 120, 62);
        var deep = Rgb(118, 78, 40);
        var crimson = Rgb(186, 42, 48);
        var gold = Rgb(232, 184, 64);
        var green = Rgb(64, 130, 76);
        var throat = Rgb(120, 34, 40);
        // The cloth body from the tail end to the neck
        var path = Smooth(2,
            V(0.02f, 0.2f, -0.34f), V(-0.02f, 0.28f, -0.4f), V(-0.02f, 0.42f, -0.42f), V(0.03f, 0.56f, -0.38f), V(0.08f, 0.66f, -0.28f),
            V(0.1f, 0.6f, -0.14f), V(0.06f, 0.48f, -0.04f), V(0.0f, 0.5f, 0.06f), V(0f, 0.62f, 0.12f), V(0f, 0.76f, 0.16f));
        int per = 3, bone = Body, parent = Body;
        Color[] bands = { crimson, crimson, gold, crimson, green, crimson, crimson, gold };
        for (int i = 0; i < path.Length - 1; i++)
        {
            if (i > 0 && i % per == 0)
            {
                bone = b.Part("seg" + i / per, parent, path[i], PokeRole.Segment, i / per);
                parent = bone;
            }
            float t0 = i / (float)(path.Length - 1), t1 = (i + 1) / (float)(path.Length - 1);
            bool plated = i >= path.Length - 4;
            var color = plated ? bronze : bands[(i / 2) % bands.Length];
            b.Limb(bone, path[i], path[i + 1], 0.045f + 0.02f * t0, 0.045f + 0.02f * t1, color, plated ? Metal : Fur, 0f);
            if (!plated && i % 4 == 1)
                b.Torus(bone, path[i], 0.05f + 0.02f * t0, 0.011f, bronze, Euler(path[i + 1] - path[i]), mat: Metal);
            if (plated)
                b.Ell(bone, path[i] + V(0, 0.01f, -0.03f), V(0.07f, 0.03f, 0.05f), deep, Euler(path[i + 1] - path[i]) + V(60f, 0, 0), Metal, 0.01f);
        }
        // The bronze collar where the cloth meets the plates
        b.Torus(bone, path[^5], 0.075f, 0.016f, deep, Euler(path[^4] - path[^5]), mat: Metal);
        // Torn streamers trailing from the tail end
        int tail = b.Tail(path[0]);
        foreach (var (dx, dz, col) in new[] { (0.1f, -0.06f, crimson), (-0.08f, -0.08f, gold), (0.02f, -0.12f, green), (0.14f, 0.02f, crimson) })
            Ribbon(b, tail, Smooth(2, path[0], path[0] + V(dx * 0.6f, -0.06f, dz * 0.5f), path[0] + V(dx, -0.12f, dz + 0.04f), path[0] + V(dx * 1.6f, -0.16f, dz * 1.4f)),
                0.03f, col, V(0, 0, 1f), 0.012f);
        // The wolf's head howling, jaws wide
        int head = b.Head(path[^1], bone);
        var c = path[^1] + V(0, 0.09f, 0.04f);
        var r = V(0.075f, 0.07f, 0.08f);
        b.Ell(head, c, r, bronze, mat: Metal);
        b.Ell(head, c + V(0, 0.03f, 0.08f), V(0.045f, 0.03f, 0.08f), bronze, V(-22f, 0, 0), Metal, 0.02f);
        b.Ell(head, c + V(0, -0.05f, 0.07f), V(0.04f, 0.02f, 0.075f), deep, V(28f, 0, 0), Metal, 0.015f);
        b.Ell(head, c + V(0, -0.012f, 0.07f), V(0.034f, 0.03f, 0.04f), throat, blend: 0.01f);
        b.Ell(head, c + V(0, 0.045f, 0.155f), V(0.018f, 0.014f, 0.014f), Rgb(60, 40, 30), blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            foreach (var (y, z) in new[] { (0.015f, 0.1f), (0.03f, 0.14f) })
                b.Spike(head, c + V(0.026f * s, y, z), c + V(0.026f * s, y - 0.03f, z + 0.005f), 0.006f, Claw, mat: Shell, blend: 0.003f);
            foreach (var (y, z) in new[] { (-0.04f, 0.1f), (-0.05f, 0.13f) })
                b.Spike(head, c + V(0.024f * s, y, z), c + V(0.024f * s, y + 0.028f, z + 0.004f), 0.006f, Claw, mat: Shell, blend: 0.003f);
            b.Tube(head, Smooth(3, c + V(0.04f * s, 0.05f, -0.02f), c + V(0.07f * s, 0.1f, -0.07f), c + V(0.07f * s, 0.12f, -0.14f)), 0.016f, 0.005f, Rgb(214, 196, 160), Shell);
            b.Spike(head, c + V(0.045f * s, 0.04f, 0.0f), c + V(0.08f * s, 0.11f, 0.0f), 0.022f, deep, 0.5f, Metal);
            var at = Out(c, r, default, V(0.7f * s, 0.42f, 0.58f));
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(255, 240, 170), glare: true);
        });
        b.Mark(head, Out(c, r, default, V(0, 1f, 0.2f)), V(0, 1f, 0.2f), 0.03f, 0.03f, deep, MarkShape.Ring);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Ielele

    /// <summary>
    /// Ielele, the Iele of the night meadows as one: three slender pale fairies in flowing white veils, their hands
    /// joined in a ring as they dance above the grass, hair streaming like mist, a ring of glowing trampled grass
    /// under their hems. The one turned to the front has her eyes shut as eyes; the two others' lashes are marks.
    /// </summary>
    private static PokeBuilder C13Ielele()
    {
        var b = new PokeBuilder("Ielele", 0.8f, BodyPlan.Floating, V(0, 0.4f, 0)) { Coat = Fur }.Hover();
        var skin = Rgb(214, 214, 238);
        var veil = Rgb(242, 242, 252);
        var veil2 = Rgb(222, 224, 246);
        var mist = Rgb(176, 228, 224);
        var mist2 = Rgb(200, 190, 240);
        var grass = Rgb(150, 236, 176);
        const float R = 0.2f;
        float hem = 0.06f, chest = 0.46f;
        // The ring of glowing grass the dance has trodden, through all three hems
        b.Torus(Body, V(0, hem, 0), R, 0.016f, grass, mat: Glow);
        for (int k = 0; k < 3; k++)
        {
            float a = k * 120f * Degree;
            var d = V(MathF.Sin(a), 0, MathF.Cos(a));
            var side = V(MathF.Cos(a), 0, -MathF.Sin(a));
            var p = d * R;
            float yaw = k * 120f;
            // The gown flaring to its hem, a veil over it
            b.Ell(Body, p + V(0, hem + 0.012f, 0), V(0.062f, 0.024f, 0.056f), veil2, V(0, yaw, 0), blend: 0.02f);
            b.Spike(Body, p + V(0, hem + 0.02f, 0), p + V(0, chest - 0.03f, 0), 0.055f, veil, blend: 0.02f);
            b.Ell(Body, p + V(0, chest, 0), V(0.03f, 0.05f, 0.025f), veil, V(0, yaw, 0), blend: 0.015f);
            Frond(b, Body, p + V(0, chest + 0.02f, 0) - d * 0.02f, p + V(0, hem + 0.12f, 0) - d * 0.07f, 0.035f, veil2, -d, 0.25f);
            // Arms out to the sisters on either side, hands joined between them
            foreach (float s in new[] { -1f, 1f })
            {
                float a2 = (k * 120f + s * 60f) * Degree;
                var hand = V(MathF.Sin(a2), 0, MathF.Cos(a2)) * (R * 0.62f) + V(0, chest - 0.03f, 0);
                var shoulder = p + side * (0.035f * s) + V(0, chest + 0.03f, 0);
                b.Limb(Body, shoulder, hand, 0.013f, 0.011f, skin);
            }
            // The head, its hair of mist streaming up and back
            int head = b.Part(k == 0 ? "head" : "head" + k, Body, p + V(0, chest + 0.06f, 0), PokeRole.Head, k * 1.1f);
            b.Limb(head, p + V(0, chest + 0.03f, 0), p + V(0, chest + 0.09f, 0), 0.014f, 0.013f, skin);
            var hc = p + V(0, chest + 0.13f, 0);
            var hr = V(0.04f, 0.046f, 0.04f);
            b.Ell(head, hc, hr, skin);
            foreach (var (u, lift, col) in new[] { (-0.5f, 0.1f, mist), (0f, 0.16f, mist2), (0.5f, 0.1f, mist) })
            {
                var root = hc + V(0, 0.025f, 0) - d * 0.02f + side * (u * 0.03f);
                b.Tube(head, Smooth(3, root, root - d * 0.06f + V(0, lift * 0.5f, 0) + side * (u * 0.05f), root - d * 0.14f + V(0, lift, 0) + side * (u * 0.12f),
                    root - d * 0.17f + V(0, lift * 0.3f, 0) + side * (u * 0.16f)), 0.018f, 0.007f, col, blend: 0.008f);
            }
            b.Ell(head, hc + V(0, 0.02f, 0) - d * 0.016f, V(0.042f, 0.034f, 0.036f), mist2, V(0, yaw, 0), blend: 0.01f);
            PokeBuilder.Both(s =>
            {
                var look = d * 0.85f + side * (0.42f * s) + V(0, 0.1f, 0);
                var at = Out(hc, hr, default, look);
                if (k == 0) b.Eye(head, at, Outward(hc, hr, at), 0.009f, Rgb(120, 110, 180), closed: true);
                else b.Mark(head, at, Outward(hc, hr, at), 0.01f, 0.005f, Rgb(110, 100, 160), MarkShape.Smile, 180f);
            });
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Mumapadura

    /// <summary>
    /// Mumapadura, the old mother of the forest: a tall hunched crone of gnarled bark standing on root-feet, a hump
    /// of moss on her shoulders with shelf mushrooms, ferns and a little owl in its hollow, her old face thrust
    /// forward with a long nose and eyes that glow pale green out of hollows, moss hanging for her hair, one long arm
    /// of twigs hanging and the other leaning on a crooked branch.
    /// </summary>
    private static PokeBuilder C13Mumapadura()
    {
        var b = new PokeBuilder("Mumapadura", 1f, BodyPlan.Biped, V(0, 0.6f, 0)) { Coat = Leaf };
        var bark = Rgb(112, 86, 62);
        var darkBark = Rgb(80, 60, 44);
        var moss = Rgb(104, 138, 66);
        var moss2 = Rgb(136, 162, 86);
        var fungus = Rgb(206, 186, 160);
        var face = Rgb(140, 112, 86);
        // Legs like old trunks, roots spreading for feet
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.46f, -0.02f));
            var knee = V(0.13f * s, 0.25f, 0.03f);
            var foot = V(0.14f * s, 0.05f, 0.02f);
            b.Limb(leg, V(0.1f * s, 0.48f, -0.02f), knee, 0.07f, 0.055f, bark);
            b.Limb(leg, knee, foot, 0.055f, 0.065f, bark);
            foreach (var (dx, dz) in new[] { (0.06f, 0.08f), (-0.02f, 0.1f), (0.09f, -0.03f), (0.0f, -0.08f), (-0.07f, 0.03f) })
                b.Spike(leg, foot + V(0, -0.01f, 0), V(foot.X + dx * s * 1.4f, 0.015f, foot.Z + dz * 1.3f), 0.035f, darkBark);
            b.PaintEll(leg, knee + V(0.04f * s, 0.02f, 0.03f), V(0.04f, 0.05f, 0.04f), moss);
        });
        // The hunched trunk of a body, the knotted hump of moss on her shoulders
        var bc = V(0, 0.62f, 0.02f);
        b.Ell(Body, bc, V(0.15f, 0.18f, 0.12f), bark, V(22f, 0, 0));
        b.Ell(Body, bc + V(0, -0.12f, -0.01f), V(0.13f, 0.08f, 0.1f), bark, blend: 0.04f);
        var hump = bc + V(0, 0.2f, -0.06f);
        var hr = V(0.18f, 0.12f, 0.15f);
        b.Ell(Body, hump, hr, bark, V(15f, 0, 0));
        b.PaintEll(Body, hump + V(-0.06f, 0.09f, 0.03f), V(0.09f, 0.05f, 0.08f), moss);
        b.PaintEll(Body, hump + V(0.07f, 0.1f, 0.03f), V(0.07f, 0.05f, 0.06f), moss2);
        b.Mark(Body, Out(bc, V(0.15f, 0.18f, 0.12f), V(22f, 0, 0), V(0.3f, -0.2f, 1f)), V(0.3f, -0.2f, 1f), 0.03f, 0.04f, darkBark, MarkShape.Ring);
        // Shelf mushrooms on her shoulders, ferns rising out of the moss
        foreach (var (x, y, z) in new[] { (-0.15f, 0.0f, 0.02f), (-0.16f, -0.05f, -0.03f), (0.13f, 0.02f, -0.1f) })
            b.Ell(Body, hump + V(x, y, z), V(0.065f, 0.014f, 0.05f), fungus, V(0, 0, x < 0 ? -8f : 8f), Shell, 0.01f);
        foreach (var (x, z, lean) in new[] { (-0.07f, -0.02f, -0.5f), (-0.03f, -0.06f, -0.1f), (0.03f, -0.1f, 0.2f), (0.09f, -0.04f, 0.6f), (0.06f, -0.12f, 0.3f), (-0.1f, -0.09f, -0.7f) })
            Frond(b, Body, Out(hump, hr, V(15f, 0, 0), V(x * 5f, 1f, z * 5f)) - V(0, 0.03f, 0), Out(hump, hr, V(15f, 0, 0), V(x * 5f, 1f, z * 5f)) + V(lean * 0.08f, 0.1f, -0.03f), 0.017f, moss2, V(0, 0, 1f), 0.2f, Leaf);
        // The little owl in its hollow
        var owl = hump + V(-0.05f, 0.08f, 0.09f);
        b.Ell(Body, owl, V(0.03f, 0.035f, 0.028f), Rgb(196, 180, 150), blend: 0.008f);
        PokeBuilder.Both(s => b.Spike(Body, owl + V(0.015f * s, 0.025f, 0), owl + V(0.022f * s, 0.05f, 0), 0.008f, Rgb(160, 140, 110), blend: 0.004f));
        b.PaintEll(Body, owl + V(0, 0.008f, 0.024f), V(0.022f, 0.014f, 0.01f), Rgb(240, 232, 210), soft: 0.005f);
        // The head thrust forward and low
        int head = b.Head(bc + V(0, 0.2f, 0.12f));
        var c = bc + V(0, 0.15f, 0.27f);
        var r = V(0.075f, 0.085f, 0.07f);
        b.Limb(head, bc + V(0, 0.18f, 0.08f), c + V(0, 0.0f, -0.03f), 0.05f, 0.045f, bark);
        b.Ell(head, c, r, face);
        b.Ell(head, c + V(0, 0.038f, 0.03f), V(0.06f, 0.022f, 0.04f), darkBark, blend: 0.015f);
        b.Spike(head, c + V(0, 0.005f, 0.05f), c + V(0, -0.045f, 0.13f), 0.02f, face);
        b.Ell(head, c + V(0, -0.065f, 0.02f), V(0.035f, 0.03f, 0.035f), face, blend: 0.02f);
        b.Mark(head, Out(c, r, default, V(0, -0.45f, 1f)), V(0, -0.45f, 1f), 0.022f, 0.006f, darkBark, MarkShape.Bar);
        PokeBuilder.Both(s =>
        {
            var at = Out(c, r, default, V(0.48f * s, 0.12f, 0.88f));
            b.Eye(head, at, Outward(c, r, at), 0.013f, sclera: true, white: Rgb(190, 255, 214), pupil: Rgb(70, 150, 110), glare: true);
            // Hair of hanging moss
            foreach (var (dx, dz, len) in new[] { (0.068f, -0.01f, 0.3f), (0.06f, -0.045f, 0.36f), (0.04f, -0.07f, 0.26f) })
            {
                var root = Out(c, r, default, V(dx * 12f * s, 0.6f, dz * 10f + 0.2f)) - V(0, 0.01f, 0);
                b.Tube(head, Smooth(3, root, root + V(0.03f * s, -0.04f, 0.02f), root + V(0.04f * s, -len * 0.6f, 0.03f), root + V(0.035f * s, -len, 0.0f)), 0.016f, 0.008f, dx > 0.055f ? moss2 : moss, blend: 0.006f);
            }
        });
        // Her arms: one hanging long with twigs for fingers, the other leaning on a crooked branch
        int left = b.Arm(-1f, bc + V(-0.15f, 0.12f, 0.06f));
        var le = bc + V(-0.25f, -0.08f, 0.08f);
        var lh = bc + V(-0.26f, -0.24f, 0.12f);
        b.Limb(left, bc + V(-0.15f, 0.12f, 0.06f), le, 0.04f, 0.03f, bark);
        b.Limb(left, le, lh, 0.03f, 0.022f, bark);
        foreach (var d in new[] { V(-0.03f, -0.08f, 0.03f), V(0f, -0.09f, 0.04f), V(0.02f, -0.07f, 0.05f), V(-0.05f, -0.05f, 0.0f) })
            b.Spike(left, lh, lh + d, 0.01f, darkBark);
        b.Spike(left, bc + V(-0.2f, 0.06f, 0.05f), bc + V(-0.32f, 0.12f, 0.04f), 0.014f, darkBark);
        int right = b.Arm(1f, bc + V(0.15f, 0.12f, 0.08f));
        var re = bc + V(0.25f, -0.02f, 0.16f);
        var rh = bc + V(0.28f, -0.06f, 0.26f);
        b.Limb(right, bc + V(0.15f, 0.12f, 0.08f), re, 0.04f, 0.03f, bark);
        b.Limb(right, re, rh, 0.03f, 0.028f, bark);
        b.Tube(right, Smooth(2, rh + V(0.0f, 0.06f, 0.01f), rh + V(0.02f, -0.12f, 0.02f), rh + V(-0.01f, -0.3f, 0.03f), rh + V(0.03f, -0.48f, 0.02f), V(rh.X + 0.02f, 0.01f, rh.Z + 0.02f)),
            0.018f, 0.015f, darkBark, blend: 0f);
        return b;
    }

    // ------------------------------------------------------------------ Maiastra

    /// <summary>
    /// Maiastra, the wonder bird of the tales: a tall slender bird of polished bronze-gold standing on long legs,
    /// its smooth body upright, its long neck raised and its beak open in song, a crest of flame on its head, its
    /// wings half raised with fiery tips, and a long tail of flame-gold plumes sweeping to the ground behind.
    /// </summary>
    private static PokeBuilder C13Maiastra()
    {
        var b = new PokeBuilder("Maiastra", 0.95f, BodyPlan.Bird, V(0, 0.6f, 0)) { Coat = Fur };
        var bronze = Rgb(214, 166, 82);
        var gold = Rgb(246, 206, 96);
        var flame = Rgb(242, 128, 44);
        var heart = Rgb(255, 230, 120);
        var leg = Rgb(134, 92, 54);
        // Long thin legs to three toes forward and one back
        PokeBuilder.Both(s =>
        {
            int l = b.Leg(s, V(0.05f * s, 0.5f, -0.02f));
            var knee = V(0.055f * s, 0.3f, -0.04f);
            var ankle = V(0.06f * s, 0.05f, 0.0f);
            b.Limb(l, V(0.05f * s, 0.52f, -0.02f), knee, 0.04f, 0.018f, bronze);
            b.Limb(l, knee, ankle, 0.016f, 0.014f, leg, Scales);
            foreach (var (dx, dz) in new[] { (-0.03f, 0.06f), (0f, 0.07f), (0.03f, 0.06f), (0f, -0.04f) })
            {
                var toe = V(ankle.X + dx * s, 0.012f, ankle.Z + dz);
                b.Limb(l, ankle, toe, 0.012f, 0.008f, leg, Scales, 0.006f);
                b.Spike(l, toe, toe + V(0, -0.008f, dz * 0.25f), 0.007f, Claw, mat: Shell, blend: 0.003f);
            }
        });
        // The upright body, smooth and polished as bronze
        var bc = V(0, 0.6f, 0);
        var br = V(0.1f, 0.15f, 0.11f);
        b.Ell(Body, bc, br, bronze, V(-18f, 0, 0), Metal);
        b.PaintEll(Body, bc + V(0, 0.02f, 0.08f), V(0.08f, 0.12f, 0.06f), gold, V(-18f, 0, 0));
        // Wings half raised, the feathers tipped in flame
        PokeBuilder.Both(s => SpreadWing(b, s, bc + V(0.08f * s, 0.06f, -0.02f), bc + V(0.16f * s, 0.14f, -0.08f), 5, 85f, 30f, 0.3f, 0.024f, gold, flame, 0.55f, pointed: true, tipFrom: 2));
        // A long tail of plumes sweeping down to the ground, flame at their ends
        int tail = b.Tail(bc + V(0, -0.1f, -0.08f));
        foreach (var (dx, dy, dz, col) in new[] { (0f, -0.5f, -0.62f, gold), (-0.08f, -0.46f, -0.5f, flame), (0.08f, -0.46f, -0.5f, flame), (-0.05f, -0.34f, -0.64f, gold), (0.05f, -0.34f, -0.64f, gold), (0f, -0.26f, -0.56f, flame) })
        {
            var root = bc + V(dx * 0.3f, -0.1f, -0.08f);
            var end = bc + V(dx, dy, dz);
            b.Tube(tail, Smooth(3, root, root + V(dx * 0.4f, -0.02f, -0.16f), Vector3.Lerp(root, end, 0.6f) + V(0, 0.02f, -0.04f), end), 0.034f, 0.02f, col, Fur, 0.008f);
            FlameTongue(b, tail, end, end + V(dx * 0.5f, 0.07f, -0.08f), 0.024f, flame, heart);
        }
        // The long neck raised, the head lifted and singing
        int head = b.Head(bc + V(0, 0.1f, 0.06f));
        var c = bc + V(0, 0.38f, 0.1f);
        var r = V(0.045f, 0.045f, 0.055f);
        b.Tube(head, Smooth(3, bc + V(0, 0.08f, 0.05f), bc + V(0, 0.2f, 0.1f), bc + V(0, 0.3f, 0.08f), c + V(0, -0.02f, -0.01f)), 0.045f, 0.03f, bronze, Metal, 0f);
        b.Ell(head, c, r, gold, V(-25f, 0, 0), Metal);
        b.Spike(head, c + V(0, 0.01f, 0.04f), c + V(0, 0.07f, 0.12f), 0.018f, Rgb(170, 120, 60), 0.8f, Shell);
        b.Spike(head, c + V(0, -0.012f, 0.04f), c + V(0, 0.01f, 0.115f), 0.013f, Rgb(150, 100, 52), 0.8f, Shell);
        foreach (var (dz, dy) in new[] { (0.0f, 0.1f), (-0.04f, 0.09f), (-0.07f, 0.06f) })
            FlameTongue(b, head, c + V(0, 0.03f, dz), c + V(0, 0.03f + dy, dz - 0.06f), 0.022f, flame, heart);
        PokeBuilder.Both(s =>
        {
            var look = V(0.7f * s, 0.25f, 0.65f);
            var at = Out(c, r, V(-25f, 0, 0), look);
            b.Eye(head, at, Vector3.Normalize(look), 0.012f, Rgb(214, 110, 30));
        });
        return b;
    }

    // ------------------------------------------------------------------ Miezila

    /// <summary>
    /// Miezila, the brother of midnight: a massive night-black beast between a horse and an ox, its coat full of
    /// stars, great curved horns, a slim silver crescent resting on the curls of its brow between them, a mane and
    /// tail of night cloud, and its eyes always shut with a faint silver line beneath each.
    /// </summary>
    private static PokeBuilder C13Miezila()
    {
        var b = new PokeBuilder("Miezila", 1f, BodyPlan.Quadruped, V(0, 0.58f, -0.02f)) { Coat = Fur };
        var night = Rgb(36, 38, 64);
        var deep = Rgb(26, 26, 44);
        var cloud = Rgb(54, 52, 92);
        var cloud2 = Rgb(40, 42, 76);
        var silver = Rgb(214, 220, 236);
        var star = Rgb(236, 238, 255);
        HorseLegs(b, 0.1f, 0.48f, 0.23f, -0.26f, 0.085f, night, deep, Rgb(22, 22, 34));
        var bc = V(0, 0.6f, -0.02f);
        var br = V(0.18f, 0.18f, 0.32f);
        b.Ell(Body, bc, br, night);
        b.Ell(Body, bc + V(0, 0.06f, 0.18f), V(0.18f, 0.18f, 0.14f), night, blend: 0.05f);
        C13Stars(b, Body, bc, br, star, 0.018f, V(1f, 0.3f, 0.2f), V(-1f, 0.4f, -0.1f), V(0.8f, 0.6f, -0.5f), V(-0.7f, 0.2f, 0.6f), V(0.6f, -0.3f, -0.6f), V(-0.5f, 0.7f, 0.3f));
        // The tail of night cloud
        int tail = b.Tail(bc + V(0, 0.08f, -0.3f));
        var tp = Smooth(2, bc + V(0, 0.08f, -0.3f), bc + V(0, 0.02f, -0.4f), bc + V(0.02f, -0.14f, -0.44f), bc + V(0.0f, -0.32f, -0.42f));
        C13Cloud(b, tail, tp, 0.05f, 0.075f, cloud, cloud2);
        C13Stars(b, tail, tp[^2], V(0.075f, 0.065f, 0.075f), star, 0.013f, V(0.9f, 0f, -0.3f), V(-0.9f, 0.2f, -0.3f));
        // The broad head held low on a thick neck
        int head = b.Head(bc + V(0, 0.12f, 0.26f));
        var c = bc + V(0, 0.16f, 0.46f);
        var r = V(0.09f, 0.085f, 0.1f);
        b.Limb(head, bc + V(0, 0.08f, 0.26f), c + V(0, 0, -0.04f), 0.12f, 0.085f, night);
        b.Ell(head, c, r, night, V(20f, 0, 0));
        var mc = c + V(0, -0.06f, 0.08f);
        b.Ell(head, mc, V(0.065f, 0.05f, 0.055f), deep, blend: 0.03f);
        PokeBuilder.Both(s =>
        {
            var nose = Out(mc, V(0.065f, 0.05f, 0.055f), default, V(0.45f * s, -0.1f, 0.88f));
            b.PaintEll(head, nose, V(0.011f, 0.009f, 0.011f), Rgb(12, 12, 20), soft: 0.006f);
            // The ears out to the side and the great horns curving up and in
            b.Ell(head, c + V(0.1f * s, 0.02f, -0.03f), V(0.04f, 0.015f, 0.025f), night, V(0, 0, 20f * s), blend: 0.01f);
            b.Tube(head, Smooth(3, c + V(0.06f * s, 0.06f, -0.02f), c + V(0.17f * s, 0.1f, -0.03f), c + V(0.23f * s, 0.22f, 0.0f), c + V(0.18f * s, 0.34f, 0.04f)), 0.035f, 0.008f, deep, Shell, 0f);
            // The eyes shut, a silver line under each
            var at = Out(c, r, V(20f, 0, 0), V(0.6f * s, 0.32f, 0.72f));
            var n = Outward(c, r, at);
            b.Eye(head, at, n, 0.014f, Rgb(170, 176, 210), closed: true);
            var under = Out(c, r, V(20f, 0, 0), V(0.58f * s, 0.12f, 0.8f));
            b.Mark(head, under, Outward(c, r, under), 0.015f, 0.005f, silver, MarkShape.Smile);
        });
        // Curls of night on its brow, and the crescent resting on them between the horns
        var brow = c + V(0, 0.08f, -0.01f);
        C13Cloud(b, head, new[] { brow + V(-0.04f, 0, 0), brow + V(0.04f, 0.0f, 0), brow + V(0, 0.03f, -0.02f) }, 0.04f, 0.04f, cloud, cloud2, 0.015f);
        var moon = brow + V(0, 0.105f, 0.01f);
        foreach (float side in new[] { -1f, 1f })
        {
            var arc = new Vector3[7];
            for (int i = 0; i < arc.Length; i++)
            {
                float a = (-90f + side * i * 24f) * Degree;
                arc[i] = moon + V(MathF.Cos(a) * 0.075f, MathF.Sin(a) * 0.075f, 0);
            }
            b.Tube(head, arc, 0.018f, 0.005f, silver, Glow, 0f);
        }
        // The mane of night cloud down its neck
        var crest = Smooth(2, brow + V(0, 0.0f, -0.06f), bc + V(0, 0.3f, 0.3f), bc + V(0, 0.27f, 0.17f), bc + V(0, 0.2f, 0.06f));
        C13Cloud(b, head, crest, 0.06f, 0.075f, cloud, cloud2);
        C13Stars(b, head, crest[3], V(0.07f, 0.06f, 0.07f), star, 0.013f, V(0.9f, 0.3f, 0f), V(-0.9f, 0.3f, 0f));
        return b;
    }

    // ------------------------------------------------------------------ Zorila

    /// <summary>
    /// Zorila, the brother of dawn: a light, quick deer, its coat brightening from grey at its slender legs to rose
    /// gold and gold along its back, a mane and tail of streaming morning light in pink and gold, antlers of pale
    /// rose light, and a small golden morning star held up between them.
    /// </summary>
    private static PokeBuilder C13Zorila()
    {
        var b = new PokeBuilder("Zorila", 0.9f, BodyPlan.Quadruped, V(0, 0.6f, -0.02f)) { Coat = Fur };
        var rose = Rgb(226, 190, 170);
        var gold = Rgb(244, 214, 150);
        var grey = Rgb(136, 130, 140);
        var pink = Rgb(250, 150, 170);
        var dawn = Rgb(252, 196, 120);
        var antler = Rgb(250, 180, 200);
        var star = Rgb(255, 212, 80);
        HorseLegs(b, 0.06f, 0.52f, 0.2f, -0.21f, 0.04f, rose, grey, Rgb(150, 110, 110));
        var bc = V(0, 0.6f, -0.02f);
        var br = V(0.1f, 0.11f, 0.24f);
        b.Ell(Body, bc, br, rose);
        b.Ell(Body, bc + V(0, 0.02f, 0.15f), V(0.1f, 0.12f, 0.09f), rose, blend: 0.04f);
        b.PaintEll(Body, bc + V(0, 0.09f, 0.0f), V(0.09f, 0.05f, 0.25f), gold);
        b.PaintEll(Body, bc + V(0, -0.09f, 0.02f), V(0.08f, 0.04f, 0.2f), Rgb(240, 222, 210));
        PokeBuilder.Both(s => b.Mark(Body, Out(bc, br, default, V(s, 0.2f, -0.3f)), V(s, 0.2f, -0.3f), 0.045f, 0.03f, Rgb(250, 232, 200), MarkShape.Wave, -20f * s));
        // The tail of morning light
        int tail = b.Tail(bc + V(0, 0.06f, -0.22f));
        var tp = Smooth(2, bc + V(0, 0.06f, -0.22f), bc + V(0, 0.06f, -0.32f), bc + V(0.02f, -0.06f, -0.4f), bc + V(0.0f, -0.2f, -0.42f));
        C13Cloud(b, tail, tp, 0.035f, 0.05f, pink, dawn);
        // The neck raised, a fine small head, big ears
        int head = b.Head(bc + V(0, 0.1f, 0.18f));
        var c = bc + V(0, 0.4f, 0.3f);
        var r = V(0.045f, 0.05f, 0.065f);
        b.Tube(head, Smooth(3, bc + V(0, 0.06f, 0.18f), bc + V(0, 0.22f, 0.25f), c + V(0, -0.03f, -0.04f)), 0.055f, 0.035f, rose, blend: 0f);
        b.Ell(head, c, r, rose, V(15f, 0, 0));
        b.Ell(head, c + V(0, -0.025f, 0.065f), V(0.028f, 0.026f, 0.04f), rose, V(15f, 0, 0), blend: 0.02f);
        b.Ell(head, c + V(0, -0.03f, 0.1f), V(0.014f, 0.011f, 0.009f), Rgb(110, 80, 80), blend: 0.006f);
        PokeBuilder.Both(s =>
        {
            b.Ell(head, c + V(0.06f * s, 0.04f, -0.03f), V(0.035f, 0.016f, 0.022f), rose, V(0, 0, 30f * s), blend: 0.008f);
            var at = Out(c, r, V(15f, 0, 0), V(0.75f * s, 0.2f, 0.62f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(120, 80, 70));
            // Antlers of rose light: a beam rising and out with three tines
            var root = c + V(0.025f * s, 0.045f, -0.02f);
            var beam = Smooth(2, root, root + V(0.05f * s, 0.08f, -0.02f), root + V(0.08f * s, 0.17f, 0.0f), root + V(0.07f * s, 0.26f, 0.03f));
            b.Tube(head, beam, 0.015f, 0.01f, antler, Glow, 0.004f);
            foreach (var (i, d) in new[] { (2, V(-0.03f * s, 0.07f, 0.03f)), (4, V(0.05f * s, 0.06f, 0.0f)), (5, V(-0.01f * s, 0.06f, -0.04f)) })
                b.Spike(head, beam[i], beam[i] + d, 0.009f, antler, mat: Glow, blend: 0.004f);
        });
        // The morning star on a stem of light between the antlers
        var starAt = c + V(0, 0.22f, -0.01f);
        b.Limb(head, c + V(0, 0.03f, -0.01f), starAt, 0.014f, 0.012f, dawn, Glow, 0.006f);
        b.Ell(head, starAt, V(0.022f, 0.022f, 0.014f), star, mat: Glow, blend: 0.006f);
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f * Degree;
            Blade(b, head, starAt, starAt + V(MathF.Sin(a), MathF.Cos(a), 0) * 0.05f, 0.014f, star, V(0, 0, 1f), 0.4f, Glow);
        }
        // The mane of streaming light
        var crest = Smooth(2, c + V(0, 0.04f, -0.05f), bc + V(0, 0.28f, 0.22f), bc + V(0, 0.18f, 0.16f), bc + V(0, 0.12f, 0.08f));
        C13Cloud(b, head, crest, 0.035f, 0.045f, pink, dawn);
        PokeBuilder.Both(s =>
        {
            for (int i = 0; i < 3; i++)
            {
                var root = crest[1 + i * 2] + V(0.02f * s, 0, 0);
                b.Tube(head, Smooth(3, root, root + V(0.05f * s, -0.05f, -0.03f), root + V(0.06f * s, -0.13f, -0.05f), root + V(0.08f * s, -0.2f, -0.09f)), 0.02f, 0.006f, i == 1 ? dawn : pink, blend: 0.004f);
            }
        });
        C13Stars(b, Body, bc, br, Rgb(255, 250, 220), 0.012f, V(0.9f, 0.5f, 0.2f), V(-0.9f, 0.5f, -0.3f));
        return b;
    }

    // ------------------------------------------------------------------ Nazdravan

    /// <summary>
    /// Nazdravan, the hero's magic horse that ate embers: a lean, powerful horse of burnished bronze running to
    /// smouldering red, great wings of bronze feathers tipped with flame folded at its shoulders, a mane and tail of
    /// fire, sparks of embers in its coat, and old, wise eyes.
    /// </summary>
    private static PokeBuilder C13Nazdravan()
    {
        var b = new PokeBuilder("Nazdravan", 1f, BodyPlan.Quadruped, V(0, 0.6f, -0.02f)) { Coat = Fur };
        var bronze = Rgb(150, 92, 50);
        var red = Rgb(150, 50, 36);
        var dark = Rgb(86, 48, 34);
        var feather = Rgb(128, 90, 52);
        var flame = Rgb(246, 124, 44);
        var heart = Rgb(255, 214, 96);
        HorseLegs(b, 0.08f, 0.52f, 0.22f, -0.24f, 0.065f, bronze, red, Rgb(60, 36, 30));
        var bc = V(0, 0.62f, -0.02f);
        var br = V(0.13f, 0.13f, 0.28f);
        b.Ell(Body, bc, br, bronze);
        b.Ell(Body, bc + V(0, 0.01f, 0.18f), V(0.13f, 0.15f, 0.12f), bronze, blend: 0.04f);
        b.PaintEll(Body, bc + V(0, -0.06f, 0.18f), V(0.1f, 0.1f, 0.08f), red);
        b.PaintEll(Body, bc + V(0, -0.12f, 0.0f), V(0.11f, 0.05f, 0.24f), red);
        foreach (var w in new[] { V(0.4f, -0.5f, 0.8f), V(-0.4f, -0.6f, 0.75f), V(0.8f, -0.75f, -0.2f), V(-0.8f, -0.75f, -0.3f) })
        {
            var at = Out(bc, br, default, w);
            b.Mark(Body, at, Outward(bc, br, at), 0.028f, 0.01f, flame, MarkShape.Zigzag, 60f * MathF.Sign(w.X));
        }
        // Great wings folded at its shoulders, raised a little, every feather tipped in flame
        PokeBuilder.Both(s =>
        {
            var shoulder = bc + V(0.1f * s, 0.08f, 0.14f);
            int wing = b.Wing(s, shoulder);
            var wrist = bc + V(0.17f * s, 0.2f, -0.02f);
            b.Limb(wing, shoulder, wrist, 0.04f, 0.03f, feather);
            for (int i = 0; i < 6; i++)
            {
                float t = i / 5f;
                var root = Vector3.Lerp(shoulder + V(0.01f * s, 0.0f, -0.04f), wrist, t);
                var end = root + V(0.03f * s + 0.03f * t * s, -0.06f - 0.06f * (1f - t), -0.22f - 0.12f * t);
                Frond(b, wing, root, end, 0.036f, feather, V(s, 0.4f, 0), 0.25f);
                b.PaintEll(wing, root + (end - root) * 0.85f, V(0.04f, 0.05f, 0.04f), flame, Euler(end - root));
            }
        });
        // The tail of fire
        int tail = b.Tail(bc + V(0, 0.07f, -0.27f));
        foreach (var (dx, dy, dz) in new[] { (0f, -0.08f, -0.2f), (0.05f, -0.2f, -0.16f), (-0.05f, -0.18f, -0.17f), (0.0f, -0.32f, -0.12f) })
            FlameTongue(b, tail, bc + V(0, 0.07f, -0.27f), bc + V(dx, 0.07f + dy, -0.27f + dz), 0.04f, flame, heart);
        // The head, a strong old horse's, smoke in its nostrils
        int head = b.Head(bc + V(0, 0.1f, 0.22f));
        var c = bc + V(0, 0.34f, 0.36f);
        var r = V(0.058f, 0.064f, 0.09f);
        b.Tube(head, Smooth(3, bc + V(0, 0.06f, 0.2f), bc + V(0, 0.22f, 0.27f), c + V(0, -0.02f, -0.06f)), 0.09f, 0.062f, bronze, blend: 0f);
        b.Ell(head, c, r, bronze, V(20f, 0, 0));
        var mc = c + V(0, -0.055f, 0.085f);
        b.Ell(head, mc, V(0.044f, 0.042f, 0.06f), red, V(25f, 0, 0), blend: 0.025f);
        b.PaintEll(head, mc + V(0, -0.01f, 0.04f), V(0.035f, 0.03f, 0.03f), dark);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, c + V(0.03f * s, 0.05f, -0.04f), c + V(0.045f * s, 0.12f, -0.06f), 0.02f, bronze, 0.55f);
            var at = Out(c, r, V(20f, 0, 0), V(0.75f * s, 0.28f, 0.58f));
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(250, 170, 60));
            var nose = Out(mc, V(0.044f, 0.042f, 0.06f), V(25f, 0, 0), V(0.4f * s, 0.05f, 0.9f));
            b.Mark(head, nose, Vector3.Normalize(V(0.4f * s, 0.05f, 0.9f)), 0.008f, 0.006f, flame);
        });
        // A mane of fire down the crest of its neck
        for (int i = 0; i < 5; i++)
        {
            var root = Vector3.Lerp(c + V(0, 0.05f, -0.05f), bc + V(0, 0.16f, 0.12f), i / 4f);
            FlameTongue(b, head, root, root + V((i % 2 == 0 ? 0.03f : -0.03f), 0.1f - 0.01f * i, -0.13f), 0.035f, flame, heart);
        }
        return b;
    }

    // ------------------------------------------------------------------ the brothers' kit

    /// <summary>
    /// A trail of cloud along <paramref name="points"/>: a puff at each point, from <paramref name="ra"/> to
    /// <paramref name="rb"/>, every other one in <paramref name="second"/> and nudged aside, so it reads as billows.
    /// </summary>
    private static void C13Cloud(PokeBuilder b, int bone, Vector3[] points, float ra, float rb, Color first, Color second, float blend = 0.02f)
    {
        for (int i = 0; i < points.Length; i++)
        {
            float t = points.Length == 1 ? 0f : i / (points.Length - 1f);
            float r = ra + (rb - ra) * t;
            var nudge = V((i % 3 - 1) * r * 0.25f, (i % 2 == 0 ? 1f : -1f) * r * 0.2f, 0);
            b.Ell(bone, points[i] + nudge, V(r, r * 0.85f, r * 1.05f), i % 2 == 0 ? first : second, blend: blend);
            // A core through the puffs, so no billow floats apart from the next
            if (i > 0) b.Limb(bone, points[i - 1], points[i], r * 0.6f, r * 0.6f, second, blend: blend);
        }
    }

    /// <summary>Stars caught in a cloud or a coat: little five-pointed marks laid on an ellipsoid's surface along the given ways out.</summary>
    private static void C13Stars(PokeBuilder b, int bone, Vector3 c, Vector3 r, Color color, float size, params Vector3[] ways)
    {
        foreach (var w in ways)
        {
            var at = Out(c, r, default, w);
            b.Mark(bone, at, Outward(c, r, at), size, size, color, MarkShape.Star5, w.X * 40f);
        }
    }

    // ------------------------------------------------------------------ Murgila

    /// <summary>
    /// Murgila, the brother of dusk: a tall, long-legged horse of dark bay going violet down its legs, the last red
    /// light glowing in cracks of ember along its back, a mane and tail of dusk cloud with the first stars in them,
    /// long patient ears, and its long face carried low, as if it pulled the dark behind it.
    /// </summary>
    private static PokeBuilder C13Murgila()
    {
        var b = new PokeBuilder("Murgila", 1f, BodyPlan.Quadruped, V(0, 0.6f, -0.02f)) { Coat = Fur };
        var bay = Rgb(126, 44, 54);
        var violet = Rgb(70, 46, 96);
        var dusk = Rgb(56, 44, 82);
        var cloud = Rgb(78, 66, 116);
        var cloud2 = Rgb(54, 52, 86);
        var ember = Rgb(255, 120, 60);
        var star = Rgb(255, 236, 150);
        HorseLegs(b, 0.075f, 0.52f, 0.22f, -0.24f, 0.058f, violet, dusk, Rgb(34, 28, 44));
        var bc = V(0, 0.6f, -0.02f);
        var br = V(0.13f, 0.13f, 0.29f);
        b.Ell(Body, bc, br, bay);
        b.Ell(Body, bc + V(0, 0.01f, 0.18f), V(0.13f, 0.15f, 0.12f), bay, blend: 0.04f);
        b.PaintEll(Body, bc + V(0, -0.12f, 0.02f), V(0.12f, 0.06f, 0.26f), violet);
        // Embers of the last light along its back, in cracks
        foreach (var (z, x) in new[] { (0.12f, 0.06f), (0.02f, -0.07f), (-0.08f, 0.07f), (-0.17f, -0.06f) })
        {
            var at = Out(bc, br, default, V(x * 6f, 1f, z * 3.4f));
            b.Mark(Body, at, Outward(bc, br, at), 0.045f, 0.016f, ember, MarkShape.Zigzag, x > 0 ? 70f : 110f);
        }
        // The tail: a long trail of dusk cloud with stars in it, sweeping down behind
        int tail = b.Tail(bc + V(0, 0.07f, -0.27f));
        var tp = Smooth(2, bc + V(0, 0.07f, -0.27f), bc + V(0, 0.02f, -0.37f), bc + V(0.02f, -0.12f, -0.44f), bc + V(0.01f, -0.28f, -0.47f), bc + V(-0.02f, -0.4f, -0.42f));
        C13Cloud(b, tail, tp, 0.045f, 0.07f, cloud, cloud2);
        C13Stars(b, tail, tp[^2], V(0.07f, 0.06f, 0.07f), star, 0.012f, V(0.8f, 0.1f, -0.4f), V(-0.8f, 0.3f, -0.3f));
        // The neck reaching forward and low, the long face of a patient old horse
        int head = b.Head(bc + V(0, 0.1f, 0.22f));
        var c = bc + V(0, 0.24f, 0.44f);
        var r = V(0.058f, 0.062f, 0.085f);
        b.Tube(head, Smooth(3, bc + V(0, 0.06f, 0.2f), bc + V(0, 0.2f, 0.3f), c + V(0, -0.01f, -0.07f)), 0.085f, 0.06f, bay, blend: 0f);
        b.Ell(head, c, r, bay, V(25f, 0, 0));
        var mc = c + V(0, -0.07f, 0.08f);
        b.Ell(head, mc, V(0.043f, 0.045f, 0.06f), violet, V(30f, 0, 0), blend: 0.025f);
        b.PaintEll(head, mc + V(0, -0.01f, 0.04f), V(0.035f, 0.03f, 0.03f), dusk);
        PokeBuilder.Both(s =>
        {
            // Long ears, laid a little back
            b.Spike(head, c + V(0.03f * s, 0.05f, -0.04f), c + V(0.06f * s, 0.17f, -0.1f), 0.022f, bay, 0.55f);
            var at = Out(c, r, V(25f, 0, 0), V(0.75f * s, 0.25f, 0.6f));
            b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(230, 150, 60));
            b.Mark(head, mc + V(0.022f * s, 0.0f, 0.05f), V(0.3f * s, 0, 1f), 0.007f, 0.005f, Rgb(40, 24, 40));
        });
        // A mane of dusk cloud along the crest of its neck, stars in it
        var crest = Smooth(2, c + V(0, 0.06f, -0.06f), bc + V(0, 0.3f, 0.31f), bc + V(0, 0.22f, 0.2f), bc + V(0, 0.15f, 0.1f));
        C13Cloud(b, head, crest, 0.045f, 0.06f, cloud, cloud2);
        C13Stars(b, head, crest[2], V(0.05f, 0.045f, 0.05f), star, 0.011f, V(0.8f, 0.4f, 0f), V(-0.8f, 0.4f, 0f));
        return b;
    }
}
