using System;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Popular species from outside the Sinnoh Pokédex (plan 03, decision 3), Kanto's second batch in National Pokédex
// order: Paras (46) to Kingler (99), but for the species of the Sinnoh Pokédex in that range (the Psyduck, Abra,
// Machop, Tentacool, Geodude, Ponyta, Magnemite and Gastly lines and Onix), hand-built before. Their forms are in
// PokemonModels.Megas.cs, PokemonModels.Gigantamax.cs and PokemonModels.Regional.cs with the other forms. Helpers
// shared with the earlier batches are in PokemonModels.Sinnoh1.cs to PokemonModels.Sinnoh4.cs and
// PokemonModels.Kanto1.cs.
internal static partial class PokemonModels
{
    /// <summary>
    /// A coat of shaggy fur: tufts of points all over an ellipsoid, spread by the golden angle and pointing out from
    /// it, the front within <paramref name="face"/> of straight ahead left bare for the face, nothing grown under
    /// <paramref name="below"/>, and each bent down by <paramref name="droop"/>, from bristling to hanging shaggy.
    /// </summary>
    private static void FurTufts(PokeBuilder b, int bone, Vector3 center, Vector3 radii, int count, float length, float r, Color color, float face = 0.7f, float below = -0.6f, float droop = 0.25f)
    {
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < count; i++)
        {
            float y = 1f - 2f * (i + 0.5f) / count;
            if (y < below) continue;
            float ring = MathF.Sqrt(1f - y * y), a = i * golden;
            var n = V(MathF.Sin(a) * ring, y, MathF.Cos(a) * ring);
            if (n.Z > face && y > -0.4f) continue;
            var root = center + n * radii * 0.9f;
            var dir = Vector3.Normalize(n / radii);
            b.Spike(bone, root, root + Vector3.Normalize(dir + V(0, -droop, 0)) * length, r, color, 0.6f, blend: 0.015f);
        }
    }

    /// <summary>The point on the surface of an ellipsoid turned as <c>Ell</c> turns it, straight out from its centre along <paramref name="dir"/>.</summary>
    private static Vector3 Out(Vector3 c, Vector3 r, Vector3 turn, Vector3 dir)
    {
        var q = Quaternion.CreateFromYawPitchRoll(turn.Y * Degree, turn.X * Degree, turn.Z * Degree);
        var local = Vector3.Transform(Vector3.Normalize(dir), Quaternion.Inverse(q));
        return c + Vector3.Transform(local / (local / r).Length(), q);
    }

    // ------------------------------------------------------------------ Paras line

    /// <summary>A crab's pincer at the end of an arm: a palm from <paramref name="wrist"/> and two curved blades closing toward each other.</summary>
    private static void Pincer(PokeBuilder b, int bone, Vector3 wrist, Vector3 dir, float size, Color color)
    {
        dir = Vector3.Normalize(dir);
        var palm = wrist + dir * size * 0.6f;
        b.Ell(bone, palm, V(size * 0.55f, size * 0.45f, size * 0.65f), color, Euler(dir));
        var tip = palm + dir * size * 1.5f;
        b.Spike(bone, palm + dir * size * 0.3f + V(0, size * 0.15f, 0), tip + V(0, size * 0.25f, 0), size * 0.42f, color, 0.55f);
        b.Spike(bone, palm + dir * size * 0.3f - V(0, size * 0.15f, 0), tip - V(0, size * 0.35f, 0), size * 0.38f, color, 0.55f);
    }

    /// <summary>
    /// Round spots painted over the top of a cap: an ellipsoid turned by <paramref name="turn"/> as <c>Ell</c> turns
    /// it, each spot centred on its surface at (x, z) of the cap's own frame, given as fractions of its radii.
    /// </summary>
    private static void CapSpots(PokeBuilder b, int bone, Vector3 center, Vector3 radii, Vector3 turn, Color color, params (float x, float z, float size)[] spots)
    {
        var q = Quaternion.CreateFromYawPitchRoll(turn.Y * Degree, turn.X * Degree, turn.Z * Degree);
        foreach (var (x, z, size) in spots)
        {
            float y = MathF.Sqrt(MathF.Max(0f, 1f - x * x - z * z));
            b.PaintEll(bone, center + Vector3.Transform(V(x, y, z) * radii, q), V(size, size * 0.6f, size), color, turn);
        }
    }

    private static PokeBuilder Paras()
    {
        var b = new PokeBuilder("Paras", 0.5f, BodyPlan.Quadruped, V(0, 0.15f, -0.03f)) { Coat = Shell };
        var orange = Rgb(244, 154, 84);
        var leg = Rgb(214, 122, 66);
        var cap = Rgb(214, 108, 148);
        var spot = Rgb(250, 202, 96);
        // Four thin legs splayed under a low round body
        foreach (var (z, front) in new[] { (0.0f, true), (-0.11f, false) })
            PokeBuilder.Both(s =>
            {
                int l = b.Leg(s, V(0.1f * s, 0.12f, z), front);
                b.Tube(l, new[] { V(0.1f * s, 0.12f, z), V(0.19f * s, 0.11f, z - 0.02f), V(0.23f * s, 0.012f, z - 0.03f) }, 0.018f, 0.012f, leg, blend: 0f);
            });
        b.Ell(Body, V(0, 0.15f, -0.03f), V(0.14f, 0.09f, 0.15f), orange);
        foreach (var (x, z) in new[] { (0.05f, 0.04f), (-0.06f, 0.02f), (0.0f, -0.08f), (0.08f, -0.06f), (-0.08f, -0.08f) })
            b.PaintEll(Body, V(x, 0.235f, z), V(0.012f, 0.01f, 0.012f), leg);
        // Two red mushrooms spotted gold growing from its back
        foreach (float x in new[] { -0.07f, 0.07f })
        {
            int shroom = b.Part(x < 0 ? "shroomL" : "shroomR", Body, V(x, 0.21f, -0.06f), PokeRole.Leaf, x * 8f);
            b.Limb(shroom, V(x, 0.2f, -0.06f), V(x * 1.4f, 0.27f, -0.07f), 0.022f, 0.02f, Rgb(246, 220, 180));
            var top = V(x * 1.55f, 0.3f, -0.07f);
            var turn = V(0, 0, x * 150f);
            b.Ell(shroom, top, V(0.075f, 0.05f, 0.075f), cap, turn);
            CapSpots(b, shroom, top, V(0.075f, 0.05f, 0.075f), turn, spot, (0.35f, 0.4f, 0.02f), (-0.45f, 0.15f, 0.022f), (0.1f, -0.5f, 0.02f), (0.55f, -0.35f, 0.016f), (-0.15f, 0.75f, 0.016f));
        }
        int head = b.Head(V(0, 0.15f, 0.08f));
        var c = V(0, 0.155f, 0.12f);
        var r = V(0.1f, 0.075f, 0.075f);
        b.Ell(head, c, r, orange);
        PokeBuilder.Both(s => b.Spike(head, V(0.025f * s, 0.115f, 0.185f), V(0.012f * s, 0.085f, 0.205f), 0.013f, White, mat: Shell, blend: 0.003f));
        // The claws it digs with, curved and split at their tips
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.15f, 0.07f));
            b.Limb(arm, V(0.1f * s, 0.15f, 0.07f), V(0.17f * s, 0.13f, 0.15f), 0.03f, 0.028f, orange);
            Pincer(b, arm, V(0.17f * s, 0.13f, 0.15f), V(0.25f * s, -0.15f, 1f), 0.064f, orange);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 0.17f), V(0.5f * s, 0.1f, 1f), 0.03f, sclera: true, pupil: Rgb(30, 30, 30)));
        return b;
    }

    private static PokeBuilder Parasect()
    {
        var b = new PokeBuilder("Parasect", 0.74f, BodyPlan.Quadruped, V(0, 0.2f, -0.04f)) { Coat = Shell };
        var orange = Rgb(242, 150, 84);
        var leg = Rgb(214, 122, 66);
        var cap = Rgb(210, 104, 150);
        var spot = Rgb(250, 202, 96);
        foreach (var (z, front) in new[] { (0.02f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int l = b.Leg(s, V(0.12f * s, 0.16f, z), front);
                b.Tube(l, new[] { V(0.12f * s, 0.16f, z), V(0.24f * s, 0.15f, z - 0.02f), V(0.29f * s, 0.014f, z - 0.03f) }, 0.024f, 0.016f, leg, blend: 0f);
            });
        b.Ell(Body, V(0, 0.19f, -0.04f), V(0.15f, 0.1f, 0.17f), orange);
        // The mushroom has taken it over: a great cap spotted gold, its pale gills over the bug's face
        int mushroom = b.Part("mushroom", Body, V(0, 0.28f, -0.06f), PokeRole.Leaf);
        var top = V(0, 0.37f, -0.08f);
        var capR = V(0.27f, 0.17f, 0.29f);
        b.Ell(mushroom, top, capR, cap, V(-12f, 0, 0));
        b.Ell(mushroom, top + V(0, -0.07f, 0.02f), V(0.25f, 0.06f, 0.27f), Rgb(246, 220, 186), V(-12f, 0, 0), blend: 0.015f);
        CapSpots(b, mushroom, top, capR, V(-12f, 0, 0), spot,
            (0.3f, 0.15f, 0.045f), (-0.4f, -0.1f, 0.04f), (0.05f, -0.45f, 0.042f), (0.65f, -0.4f, 0.034f), (-0.7f, -0.35f, 0.036f),
            (0.05f, 0.55f, 0.034f), (-0.35f, 0.45f, 0.03f), (0.75f, 0.2f, 0.03f), (-0.8f, 0.25f, 0.028f), (0.3f, -0.8f, 0.03f), (-0.3f, -0.85f, 0.028f));
        int head = b.Head(V(0, 0.18f, 0.1f));
        var c = V(0, 0.19f, 0.14f);
        var r = V(0.1f, 0.07f, 0.075f);
        b.Ell(head, c, r, orange);
        PokeBuilder.Both(s => b.Spike(head, V(0.025f * s, 0.15f, 0.205f), V(0.012f * s, 0.12f, 0.225f), 0.014f, White, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.19f, 0.07f));
            b.Limb(arm, V(0.12f * s, 0.19f, 0.07f), V(0.21f * s, 0.16f, 0.17f), 0.036f, 0.032f, orange);
            Pincer(b, arm, V(0.21f * s, 0.16f, 0.17f), V(0.25f * s, -0.2f, 1f), 0.08f, orange);
        });
        // Its eyes have gone blank
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 0.205f), V(0.5f * s, 0.1f, 1f), 0.026f, sclera: true, pupil: Rgb(226, 226, 230)));
        return b;
    }

    // ------------------------------------------------------------------ Venonat line

    private static PokeBuilder Venonat()
    {
        var b = new PokeBuilder("Venonat", 0.6f, BodyPlan.Biped, V(0, 0.26f, 0)) { Coat = Fur };
        var purple = Rgb(132, 106, 190);
        var cream = Rgb(232, 214, 176);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.07f * s, 0.1f, 0.02f));
            b.Ell(leg, V(0.07f * s, 0.048f, 0.05f), V(0.06f, 0.048f, 0.08f), cream);
            int arm = b.Arm(s, V(0.16f * s, 0.22f, 0.1f));
            b.Ell(arm, V(0.19f * s, 0.2f, 0.13f), V(0.035f, 0.03f, 0.035f), cream);
        });
        // A ball of fuzzy fur that is all head, great red compound eyes, two little fangs and two feelers
        var c = V(0, 0.27f, 0);
        var r = V(0.19f, 0.19f, 0.18f);
        b.Ell(Body, c, r, purple);
        FurTufts(b, Body, c, r, 46, 0.06f, 0.03f, purple, 0.6f, droop: 0.45f);
        PokeBuilder.Both(s => b.Spike(Body, V(0.03f * s, 0.19f, 0.165f), V(0.02f * s, 0.15f, 0.185f), 0.014f, White, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(Body, s, V(0.04f * s, 0.44f, 0.04f));
            b.Tube(ear, Smooth(3, V(0.04f * s, 0.44f, 0.04f), V(0.08f * s, 0.54f, 0.04f), V(0.16f * s, 0.6f, 0.02f)), 0.012f, 0.01f, purple);
            b.Ell(ear, V(0.17f * s, 0.605f, 0.02f), V(0.03f, 0.018f, 0.018f), Rgb(240, 238, 246), blend: 0.006f);
        });
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.08f * s, 0.3f), V(0.45f * s, 0.05f, 1f), 0.06f, sclera: true, white: Rgb(232, 84, 124), pupil: Rgb(176, 40, 80)));
        return b;
    }

    private static PokeBuilder Venomoth()
    {
        var b = new PokeBuilder("Venomoth", 0.8f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var lilac = Rgb(214, 196, 240);
        var vein = Rgb(160, 130, 206);
        var purple = Rgb(144, 112, 196);
        // Broad pale wings, veined, the scales on them the poison it scatters
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.04f * s, 0.46f, -0.03f));
            FlatWing(b, w, V(0.26f * s, 0.6f, -0.035f), 0.24f, 0.16f, 26f * s, lilac, vein, 0.02f);
            foreach (float a in new[] { 10f, 26f, 42f })
                b.Mark(w, V(0.26f * s, 0.6f, -0.02f) + V(MathF.Cos(a * Degree) * s, MathF.Sin(a * Degree), 0) * 0.04f, V(0, 0, 1f), 0.1f, 0.006f, vein, MarkShape.Bar, a * s);
            FlatWing(b, w, V(0.17f * s, 0.34f, -0.04f), 0.15f, 0.1f, -40f * s, lilac, vein, 0.018f);
        });
        b.Ell(Body, V(0, 0.46f, 0), V(0.06f, 0.06f, 0.055f), purple);
        // The abdomen, pale and ringed
        b.Ell(Body, V(0, 0.34f, -0.02f), V(0.055f, 0.1f, 0.05f), Rgb(222, 214, 236), V(10f, 0, 0));
        foreach (float y in new[] { 0.37f, 0.33f, 0.29f })
            b.PaintTorus(Body, V(0, y, -0.015f - (0.37f - y) * 0.2f), 0.052f * MathF.Sqrt(1f - (y - 0.34f) * (y - 0.34f) / 0.012f), 0.005f, vein, V(10f, 0, 0));
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.03f * s, 0.42f, 0.03f));
            b.Tube(leg, new[] { V(0.03f * s, 0.42f, 0.03f), V(0.06f * s, 0.36f, 0.06f), V(0.05f * s, 0.3f, 0.07f) }, 0.01f, 0.008f, purple, blend: 0f);
        });
        int head = b.Head(V(0, 0.5f, 0.02f));
        var c = V(0, 0.55f, 0.03f);
        var r = V(0.065f, 0.06f, 0.06f);
        b.Ell(head, c, r, purple);
        PokeBuilder.Both(s => b.Spike(head, V(0.015f * s, 0.51f, 0.085f), V(0.01f * s, 0.485f, 0.095f), 0.008f, White, mat: Shell, blend: 0.003f));
        // Two feelers like horns, great blue eyes
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, V(0.02f * s, 0.6f, 0.02f));
            b.Spike(ear, V(0.02f * s, 0.59f, 0.02f), V(0.06f * s, 0.72f, 0.0f), 0.022f, purple, 0.6f);
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.035f * s, 0.56f), V(0.7f * s, 0.05f, 0.7f), 0.03f, sclera: true, white: Rgb(110, 170, 236), pupil: Rgb(40, 70, 150)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Diglett line

    /// <summary>
    /// The mound of stones Diglett rise from, on the root so it stays put while they bob and strike: a low disc of
    /// earth and rocks heaped round its rim, shaded light and dark.
    /// </summary>
    private static void Burrow(PokeBuilder b, float rx, float rz, int count, Color stone, uint seed)
    {
        var rng = new GenomeRandom(seed);
        b.Ell(Root, V(0, 0.022f, 0), V(rx, 0.036f, rz), PixelCanvas.Mix(stone, Rgb(40, 40, 44), 0.35f), mat: Shell, blend: 0.02f);
        for (int i = 0; i < count; i++)
        {
            float a = MathF.Tau * (i + rng.Range(-0.2f, 0.2f)) / count;
            float k = rng.Range(0.03f, 0.048f), reach = rng.Range(0.8f, 0.97f);
            var at = V(MathF.Sin(a) * rx * reach, rng.Range(0.035f, 0.055f), MathF.Cos(a) * rz * reach);
            var tone = PixelCanvas.Mix(stone, i % 3 == 0 ? Rgb(255, 255, 255) : Rgb(40, 40, 44), i % 3 == 1 ? 0.22f : 0.12f);
            b.Ell(Root, at, V(k * 1.25f, k * 0.72f, k), tone, V(rng.Range(-18f, 18f), a / Degree + rng.Range(-40f, 40f), rng.Range(-14f, 14f)), Shell, 0.01f);
        }
    }

    /// <summary>One Diglett rising from the ground at <paramref name="foot"/>: a smooth dome, two small black eyes and a big round nose.</summary>
    private static void DiglettDome(PokeBuilder b, int bone, Vector3 foot, float s, Color brown, Color nose)
    {
        b.Limb(bone, foot + V(0, 0.04f, 0) * s, foot + V(0, 0.3f, 0) * s, 0.14f * s, 0.125f * s, brown);
        b.Ell(bone, foot + V(0, 0.25f, 0.116f) * s, V(0.07f, 0.042f, 0.04f) * s, nose, mat: Shell, blend: 0.008f);
        var top = foot + V(0, 0.3f, 0) * s;
        var round = V(0.125f, 0.125f, 0.125f) * s;
        PokeBuilder.Both(sd => b.Eye(bone, On(top, round, foot.X + 0.042f * s * sd, foot.Y + 0.325f * s), V(0.3f * sd, 0.1f, 1f), 0.021f * s));
    }

    /// <summary>
    /// Long wavy hair over an Alolan Diglett's crown and down to the ground: a cap over the top and a lock falling
    /// from it at each of <paramref name="angles"/> (degrees round from the front, toward +x), the face left clear.
    /// </summary>
    private static void Mane(PokeBuilder b, int bone, Vector3 foot, float s, Color blond, params float[] angles)
    {
        b.Ell(bone, foot + V(0, 0.33f, -0.035f) * s, V(0.15f, 0.12f, 0.135f) * s, blond, mat: Fur, blend: 0.02f);
        b.Ell(bone, foot + V(0, 0.2f, -0.07f) * s, V(0.165f, 0.2f, 0.11f) * s, blond, mat: Fur, blend: 0.03f);
        var crown = foot + V(0, 0.41f, -0.02f) * s;
        for (int i = 0; i < angles.Length; i++)
        {
            var way = V(MathF.Sin(angles[i] * Degree), 0, MathF.Cos(angles[i] * Degree));
            float wave = i % 2 == 0 ? 1f : -1f;
            var path = Smooth(3, crown + way * 0.05f * s, foot + (way * 0.165f + V(0, 0.34f, 0)) * s, foot + (way * (0.2f + 0.02f * wave) + V(0, 0.2f, 0)) * s,
                foot + (way * (0.175f - 0.02f * wave) + V(0, 0.09f, 0)) * s, foot + (way * 0.23f + V(0, 0.035f, 0)) * s);
            b.Tube(bone, path, 0.056f * s, 0.036f * s, blond, Fur, 0f);
        }
    }

    private static PokeBuilder DiglettBuild(bool alola)
    {
        var b = new PokeBuilder(alola ? "Diglett-Alola" : "Diglett", 0.48f, BodyPlan.Biped, V(0, 0.05f, 0)) { Coat = Scales };
        var brown = alola ? Rgb(146, 88, 46) : Rgb(172, 124, 94);
        var nose = alola ? Rgb(232, 96, 110) : Rgb(228, 150, 196);
        Burrow(b, 0.22f, 0.2f, 13, alola ? Rgb(92, 90, 100) : Rgb(162, 162, 166), alola ? 82u : 81u);
        DiglettDome(b, Body, V(0, 0, 0), 1f, brown, nose);
        // The ground cuts it off flat, where it goes on down into the earth
        b.CutBox(Root, V(0, -0.3f, 0), V(0.6f, 0.3f, 0.6f), Quaternion.Identity);
        if (alola)
        {
            // Three fine golden hairs on its crown, as tough as steel
            int hair = b.Part("hair", Body, V(0, 0.42f, 0), PokeRole.Leaf);
            foreach (float x in new[] { -1f, 0f, 1f })
                b.Tube(hair, Smooth(3, V(0.01f * x, 0.415f, 0), V(0.035f * x, 0.48f, -0.005f), V(0.075f * x, 0.53f - 0.02f * MathF.Abs(x), -0.01f), V(0.12f * x, 0.53f - 0.06f * MathF.Abs(x), -0.015f)),
                    0.009f, 0.006f, Rgb(236, 206, 104), Fur, 0f);
        }
        return b;
    }

    private static PokeBuilder Diglett() => DiglettBuild(false);

    private static PokeBuilder DugtrioBuild(bool alola)
    {
        var b = new PokeBuilder(alola ? "Dugtrio-Alola" : "Dugtrio", 0.66f, BodyPlan.Biped, V(0, 0.05f, -0.06f)) { Coat = Scales };
        var brown = alola ? Rgb(146, 88, 46) : Rgb(172, 124, 94);
        var nose = alola ? Rgb(232, 96, 110) : Rgb(228, 150, 196);
        var blond = Rgb(244, 222, 128);
        Burrow(b, 0.36f, 0.28f, 18, alola ? Rgb(92, 90, 100) : Rgb(162, 162, 166), alola ? 84u : 83u);
        // Three Diglett as one: the tallest behind, the others before it to either side, each bobbing on its own
        var middle = V(0, 0, -0.07f);
        var left = V(-0.15f, 0, 0.05f);
        var right = V(0.15f, 0, 0.07f);
        int l = b.Part("left", Root, left + V(0, 0.05f, 0), PokeRole.Head, 1.7f, -1f);
        int r = b.Part("right", Root, right + V(0, 0.05f, 0), PokeRole.Head, 3.1f, 1f);
        if (alola)
        {
            Mane(b, Body, middle, 1.12f, blond, -100f, 100f, 140f, 180f, 220f);
            Mane(b, l, left, 0.92f, blond, -130f, -100f, -70f, -160f, 170f);
            Mane(b, r, right, 0.8f, blond, 70f, 100f, 130f, 160f, -170f);
        }
        DiglettDome(b, Body, middle, 1.12f, brown, nose);
        DiglettDome(b, l, left, 0.92f, brown, nose);
        DiglettDome(b, r, right, 0.8f, brown, nose);
        b.CutBox(Root, V(0, -0.3f, 0), V(0.8f, 0.3f, 0.8f), Quaternion.Identity);
        return b;
    }

    private static PokeBuilder Dugtrio() => DugtrioBuild(false);

    // ------------------------------------------------------------------ Meowth line

    /// <summary>A cat's big pointed ear from <paramref name="root"/> to <paramref name="tip"/>, flat, its front painted <paramref name="inner"/>.</summary>
    private static void CatEar(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float r, Color outer, Color inner)
    {
        b.Spike(bone, root, tip, r, outer, 0.42f);
        var d = tip - root;
        b.PaintEll(bone, root + d * 0.36f + V(0, 0, r * 0.3f), V(r * 0.58f, d.Length() * 0.36f, r * 0.5f), inner, Euler(d));
    }

    /// <summary>
    /// The coin on a Meowth's brow at <paramref name="at"/>, tipped back <paramref name="tilt"/> degrees to lie on it,
    /// with ridges stamped across it.
    /// </summary>
    private static void Coin(PokeBuilder b, int bone, Vector3 at, float tilt, float w, float h, Color metal, Color ridge, int ridges, SurfaceMaterial mat = Metal)
    {
        float t = tilt * Degree;
        var normal = V(0, MathF.Sin(t), MathF.Cos(t));
        var up = V(0, MathF.Cos(t), -MathF.Sin(t));
        b.Ell(bone, at, V(w, h, w * 0.32f), metal, V(-tilt, 0, 0), mat, 0.006f);
        for (int i = 0; i < ridges; i++)
            b.Mark(bone, at + normal * w * 0.3f + up * h * (0.36f - 0.3f * i), normal, w * 0.62f, h * 0.085f, ridge, MarkShape.Bar);
    }

    /// <summary>Meowth and its Alolan and Galarian forms: a cat on its hind legs, mostly head, with a coin on its brow.</summary>
    private static PokeBuilder MeowthBuild(bool alola, bool galar)
    {
        var b = new PokeBuilder(alola ? "Meowth-Alola" : galar ? "Meowth-Galar" : "Meowth", galar ? 0.62f : 0.6f, BodyPlan.Biped, V(0, 0.33f, 0)) { Coat = Fur };
        var fur = alola ? Rgb(178, 182, 220) : galar ? Rgb(138, 120, 104) : Rgb(246, 234, 200);
        var paw = alola ? Rgb(226, 228, 242) : galar ? Rgb(54, 48, 52) : Rgb(166, 110, 72);
        var ear = alola ? Rgb(80, 84, 112) : Rgb(54, 46, 52);
        var inner = alola ? Rgb(132, 136, 168) : galar ? Rgb(100, 86, 78) : Rgb(156, 102, 70);
        var dark = Rgb(34, 30, 34);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.26f, 0));
            b.Limb(leg, V(0.05f * s, 0.27f, 0), V(0.06f * s, 0.05f, 0.01f), galar ? 0.04f : 0.031f, galar ? 0.034f : 0.025f, galar ? paw : fur);
            b.Ell(leg, V(0.062f * s, 0.026f, 0.035f), V(0.038f, 0.026f, 0.058f), paw);
            if (!galar) b.PaintEll(leg, V(0.06f * s, 0.062f, 0.014f), V(0.04f, 0.03f, 0.04f), paw);
        });
        if (galar)
        {
            // A round body of shaggy dark fur, hardened like steel from living among the ships of Galar
            b.Ell(Body, V(0, 0.33f, 0), V(0.13f, 0.12f, 0.11f), fur);
            FurTufts(b, Body, V(0, 0.33f, 0), V(0.13f, 0.12f, 0.11f), 34, 0.06f, 0.028f, fur, 2f, -0.7f, 1.1f);
        }
        else
            b.Ell(Body, V(0, 0.34f, 0), V(0.082f, 0.11f, 0.072f), fur);

        // Thin arms held up and ready, the Galarian's black and clawed
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.41f, 0.01f));
            var elbow = V((galar ? 0.17f : 0.15f) * s, 0.35f, 0.04f);
            var hand = V((galar ? 0.2f : 0.17f) * s, 0.43f, 0.09f);
            b.Limb(arm, V(0.07f * s, 0.41f, 0.01f), elbow, galar ? 0.034f : 0.028f, 0.025f, galar ? paw : fur);
            b.Limb(arm, elbow, hand, 0.025f, 0.023f, galar ? paw : fur);
            b.Ell(arm, hand + V(0, 0.01f, 0.004f), V(0.033f, 0.03f, 0.031f), galar ? paw : fur);
            if (galar)
                for (int i = -1; i <= 1; i++)
                    b.Spike(arm, hand + V(0.014f * i, 0.025f, 0.018f), hand + V(0.022f * i, 0.085f, 0.06f), 0.011f, dark, mat: Shell);
        });

        int head = b.Head(V(0, 0.44f, 0.01f));
        var c = V(0, 0.58f, 0.02f);
        var r = V(0.165f, 0.135f, 0.13f);
        // Pointed ears, black outside; the Alolan's bigger and curving out
        PokeBuilder.Both(s =>
        {
            int e = b.Ear(head, s, V(0.1f * s, 0.66f, 0));
            CatEar(b, e, V(0.095f * s, 0.65f, 0), alola ? V(0.2f * s, 0.84f, -0.03f) : V(0.17f * s, 0.83f, -0.025f), alola ? 0.07f : 0.065f, ear, inner);
        });
        b.Ell(head, c, r, fur);
        PokeBuilder.Both(s => b.Ell(head, V(0.08f * s, 0.535f, 0.05f), V(0.09f, 0.07f, 0.08f), fur, blend: 0.04f));
        if (galar) FurTufts(b, head, c, r * 1.02f, 40, 0.055f, 0.027f, fur, 0.45f, -0.5f, 0.9f);

        // The coin on its brow, gold, or tarnished dark on the Galarian
        var brow = On(c, r, 0, 0.675f);
        Coin(b, head, brow, 44f, 0.04f, alola ? 0.056f : 0.05f, galar ? Rgb(96, 82, 72) : Rgb(244, 200, 72), galar ? Rgb(176, 160, 140) : Rgb(190, 140, 40), galar ? 1 : 3);

        var mouth = On(c, r, 0, 0.5f);
        if (galar)
        {
            // A wide grimace full of jagged teeth
            b.Ell(head, mouth + V(0, 0.002f, -0.012f), V(0.075f, 0.03f, 0.022f), White, mat: Shell, blend: 0.008f);
            b.Mark(head, mouth + V(0, 0.002f, 0.009f), V(0, 0, 1f), 0.068f, 0.014f, dark, MarkShape.Zigzag);
        }
        else if (alola)
        {
            b.Mark(head, mouth + V(0, 0.004f, -0.002f), V(0, -0.1f, 1f), 0.026f, 0.008f, dark, MarkShape.Wave);
            b.Spike(head, mouth + V(0.012f, 0.0f, -0.006f), mouth + V(0.011f, -0.022f, 0.004f), 0.008f, White, mat: Shell, blend: 0.003f);
        }
        else
        {
            Grin(b, head, mouth, V(0.042f, 0.03f, 0.03f), Rgb(150, 58, 72));
            PokeBuilder.Both(s => b.Spike(head, mouth + V(0.02f * s, 0.022f, -0.004f), mouth + V(0.019f * s, 0.0f, 0.004f), 0.008f, White, mat: Shell, blend: 0.003f));
        }
        if (!galar) Whiskers(b, head, V(0.115f, 0.535f, 0.1f), alola ? 0.26f : 0.22f, Rgb(244, 244, 242), alola ? 0.0075f : 0.0062f);

        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.062f * s, 0.585f), V(0.4f * s, 0.05f, 1f), galar ? 0.046f : alola ? 0.04f : 0.045f, sclera: true,
            white: galar ? Rgb(246, 206, 74) : null, pupil: Black, glare: alola));

        // A long tail curled at its end
        int tail = b.Tail(V(0, 0.27f, -0.06f));
        var tailRoot = V(0, 0.27f, -0.06f);
        var tailWay = Vector3.Normalize(V(0.35f, 0.3f, -1f));
        CurledTail(b, tail, tailRoot, tailWay, 0.24f, galar ? 0.02f : 0.015f, fur, 0.05f);
        b.PaintEll(tail, tailRoot + tailWay * 0.24f + V(0, 0.06f, 0), V(0.07f, 0.075f, 0.07f), alola ? Rgb(226, 228, 242) : galar ? dark : paw);
        return b;
    }

    private static PokeBuilder Meowth() => MeowthBuild(false, false);

    /// <summary>Persian and its Alolan form: a sleek cat on four long legs, a gem on its brow and a tail curled at its end.</summary>
    private static PokeBuilder PersianBuild(bool alola)
    {
        var b = new PokeBuilder(alola ? "Persian-Alola" : "Persian", 0.78f, BodyPlan.Quadruped, V(0, 0.31f, -0.01f)) { Coat = Fur };
        var fur = alola ? Rgb(176, 182, 218) : Rgb(246, 232, 194);
        var ear = alola ? Rgb(78, 82, 110) : Rgb(60, 52, 58);
        var inner = alola ? Rgb(132, 136, 170) : Rgb(150, 120, 116);
        var gem = alola ? Rgb(64, 170, 228) : Rgb(222, 48, 62);

        foreach (var (z, front) in new[] { (0.14f, true), (-0.15f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.06f * s, 0.28f, z), front);
                b.Limb(leg, V(0.06f * s, 0.29f, z), V(0.065f * s, 0.035f, z + (front ? 0.02f : -0.01f)), front ? 0.032f : 0.038f, 0.022f, fur);
                b.Ell(leg, V(0.066f * s, 0.022f, z + (front ? 0.04f : 0.01f)), V(0.03f, 0.022f, 0.042f), fur);
            });
        b.Ell(Body, V(0, 0.31f, -0.01f), V(0.088f, 0.078f, 0.21f), fur);
        b.Ell(Body, V(0, 0.33f, 0.12f), V(0.08f, 0.088f, 0.085f), fur, blend: 0.04f);

        // A broad flat face, the Alolan's rounder and wider still, with long whiskers
        int head = b.Head(V(0, 0.38f, 0.16f));
        var c = V(0, 0.46f, 0.21f);
        var r = alola ? V(0.175f, 0.125f, 0.11f) : V(0.115f, 0.085f, 0.09f);
        PokeBuilder.Both(s =>
        {
            int e = b.Ear(head, s, V(0.065f * s, 0.52f, 0.19f));
            CatEar(b, e, alola ? V(0.08f * s, 0.52f, 0.19f) : V(0.06f * s, 0.51f, 0.19f), alola ? V(0.16f * s, 0.63f, 0.16f) : V(0.11f * s, 0.62f, 0.17f), alola ? 0.055f : 0.045f, ear, inner);
        });
        b.Ell(head, c, r, fur);
        PokeBuilder.Both(s => b.Ell(head, V((alola ? 0.095f : 0.06f) * s, 0.425f, 0.25f), alola ? V(0.1f, 0.075f, 0.075f) : V(0.075f, 0.055f, 0.065f), fur, blend: 0.03f));
        b.Ell(head, V(0, 0.43f, 0.3f), V(0.038f, 0.03f, 0.03f), fur, blend: 0.02f);
        b.Ell(head, V(0, 0.448f, 0.33f), V(0.012f, 0.008f, 0.008f), alola ? Rgb(84, 86, 112) : Rgb(226, 140, 150), mat: Shell, blend: 0.004f);
        var brow = On(c, r, 0, 0.515f);
        b.Ell(head, brow + V(0, 0, -0.002f), V(0.022f, 0.022f, 0.012f), gem, V(-40f, 0, 0), Metal, 0.004f);
        Whiskers(b, head, V(0.07f, 0.432f, 0.31f), alola ? 0.24f : 0.2f, Rgb(236, 236, 240), 0.0075f);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.047f * s, 0.475f), V(0.45f * s, 0.1f, 1f), 0.026f, sclera: true, pupil: alola ? Black : Rgb(190, 38, 52), glare: true));

        int tail = b.Tail(V(0, 0.33f, -0.21f));
        CurledTail(b, tail, V(0, 0.33f, -0.21f), Vector3.Normalize(V(0.2f, 0.7f, -1f)), 0.3f, 0.017f, fur, 0.06f);
        return b;
    }

    private static PokeBuilder Persian() => PersianBuild(false);

    // ------------------------------------------------------------------ Mankey line

    /// <summary>A pig's snout on the front of a face at <paramref name="at"/>, with two nostrils.</summary>
    private static void Snout(PokeBuilder b, int bone, Vector3 at, float w, Color pink)
    {
        b.Ell(bone, at, V(w, w * 0.75f, w * 0.6f), pink, blend: 0.012f);
        PokeBuilder.Both(s => b.Mark(bone, at + V(w * 0.38f * s, 0, w * 0.58f), V(0, 0, 1f), w * 0.16f, w * 0.24f, Rgb(120, 60, 66)));
    }

    /// <summary>
    /// Mankey's line: a ball of pale shaggy fur with a pig's snout and furious eyes, on long brown limbs held up to
    /// fight; Primeape is bigger, its wrists and ankles in black shackles.
    /// </summary>
    private static PokeBuilder ApeBuild(bool primeape)
    {
        var b = new PokeBuilder(primeape ? "Primeape" : "Mankey", primeape ? 0.72f : 0.6f, BodyPlan.Biped, V(0, primeape ? 0.42f : 0.38f, 0)) { Coat = Fur };
        var fur = Rgb(242, 232, 216);
        var limb = Rgb(168, 124, 94);
        var pink = Rgb(232, 168, 170);
        var iron = Rgb(46, 44, 50);
        float k = primeape ? 1.25f : 1f;
        float thick = primeape ? 1.55f : 1f;
        var c = V(0, primeape ? 0.43f : 0.38f, 0);
        var r = V(0.17f, 0.16f, 0.15f) * k;
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, c.Y - 0.12f * k, 0));
            var knee = V(0.18f * k * s, c.Y - 0.22f * k, 0.04f);
            var ankle = V(0.16f * k * s, 0.06f, 0.05f);
            b.Tube(leg, new[] { V(0.08f * s, c.Y - 0.11f * k, 0), knee, ankle }, 0.024f * thick, 0.022f * thick, limb, blend: 0f);
            b.Ell(leg, V(0.16f * k * s, 0.03f, 0.075f), V(0.038f, 0.03f, 0.065f) * k, limb);
            if (primeape) b.Torus(leg, ankle + V(0, 0.015f, 0), 0.04f, 0.014f, iron, Euler(knee - ankle), mat: Metal);

            int arm = b.Arm(s, V(0.14f * k * s, c.Y, 0));
            var elbow = V(0.26f * k * s, c.Y + 0.05f * k, 0.02f);
            var wrist = V(0.27f * k * s, c.Y + 0.17f * k, 0.03f);
            b.Tube(arm, new[] { V(0.13f * k * s, c.Y, 0), elbow, wrist }, 0.024f * thick, 0.022f * thick, limb, blend: 0f);
            b.Ell(arm, wrist + V(0, 0.035f * k, 0), V(0.045f, 0.042f, 0.042f) * k, limb);
            if (primeape) b.Torus(arm, wrist - V(0, 0.005f, 0), 0.042f, 0.015f, iron, Euler(wrist - elbow), mat: Metal);
        });
        b.Ell(Body, c, r, fur);
        FurTufts(b, Body, c, r, primeape ? 64 : 50, 0.05f * k, 0.026f * k, fur, 0.62f, -0.75f, 0.1f);
        PokeBuilder.Both(s =>
        {
            int e = b.Ear(Body, s, V(0.1f * k * s, c.Y + 0.12f * k, 0));
            CatEar(b, e, V(0.1f * k * s, c.Y + 0.11f * k, 0), V(0.16f * k * s, c.Y + 0.22f * k, -0.01f), 0.045f * k, fur, Rgb(236, 190, 180));
        });
        Snout(b, Body, On(c, r, 0, c.Y - 0.03f * k) + V(0, 0, 0.004f), 0.042f * k, pink);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.056f * k * s, c.Y + 0.035f * k), V(0.4f * s, 0.05f, 1f), 0.026f * k, sclera: true, pupil: Rgb(150, 56, 48), glare: true));
        if (!primeape)
        {
            int tail = b.Tail(V(0, 0.3f, -0.14f));
            b.Tube(tail, Smooth(3, V(0, 0.3f, -0.14f), V(0.05f, 0.24f, -0.28f), V(0.13f, 0.28f, -0.37f), V(0.19f, 0.38f, -0.36f), V(0.17f, 0.44f, -0.31f)), 0.014f, 0.01f, limb);
        }
        return b;
    }

    private static PokeBuilder Mankey() => ApeBuild(false);

    private static PokeBuilder Primeape() => ApeBuild(true);

    // ------------------------------------------------------------------ Growlithe line

    /// <summary>
    /// Dark stripes round a body: bands across the back and down the flanks at each of <paramref name="zs"/>, stopping
    /// short of the belly, their tops leaning back <paramref name="lean"/> degrees.
    /// </summary>
    private static void Stripes(PokeBuilder b, int bone, Vector3 center, Vector3 radii, Color color, float width, float lean, params float[] zs)
    {
        foreach (float z in zs)
            b.PaintEll(bone, V(center.X, center.Y + radii.Y * 0.25f, z), V(radii.X * 1.2f, radii.Y * 0.95f, width), color, V(-lean, 0, 0), 0.008f);
    }

    /// <summary>
    /// Growlithe and its Hisuian form: a puppy striped black, with a cream crest, chest and tail; the Hisuian's head,
    /// front paws and tail are white rock.
    /// </summary>
    private static PokeBuilder GrowlitheBuild(bool hisui)
    {
        var b = new PokeBuilder(hisui ? "Growlithe-Hisui" : "Growlithe", 0.58f, BodyPlan.Quadruped, V(0, 0.24f, -0.02f)) { Coat = Fur };
        var orange = hisui ? Rgb(214, 82, 50) : Rgb(244, 150, 72);
        var cream = Rgb(248, 228, 184);
        var rock = Rgb(230, 226, 216);
        var stripe = Rgb(40, 38, 48);
        foreach (var (z, front) in new[] { (0.1f, true), (-0.14f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.065f * s, 0.2f, z), front);
                b.Limb(leg, V(0.065f * s, 0.21f, z), V(0.07f * s, 0.045f, z + 0.01f), 0.038f, 0.03f, orange);
                b.Ell(leg, V(0.071f * s, 0.026f, z + 0.03f), V(0.036f, 0.026f, 0.046f), orange);
                b.PaintEll(leg, V(0.068f * s, 0.13f, z + 0.005f), V(0.05f, 0.011f, 0.05f), stripe, V(0, 0, -18f * s), 0.008f);
                if (hisui && front)
                    b.Ell(leg, V(0.071f * s, 0.055f, z + 0.03f), V(0.052f, 0.056f, 0.056f), rock, mat: Shell);
                else if (front)
                    for (int i = -1; i <= 1; i++)
                        b.Spike(leg, V(0.071f * s + 0.017f * i, 0.017f, z + 0.064f), V(0.071f * s + 0.02f * i, 0.012f, z + 0.086f), 0.008f, White, mat: Shell, blend: 0.004f);
            });
        var bc = V(0, 0.24f, -0.02f);
        var br = V(0.1f, 0.095f, 0.17f);
        b.Ell(Body, bc, br, orange);
        Stripes(b, Body, bc, br, stripe, 0.013f, 22f, 0.04f, -0.04f, -0.12f);
        b.Ell(Body, V(0, 0.27f, 0.12f), V(0.075f, 0.085f, 0.06f), hisui ? orange : cream, blend: 0.03f);
        if (!hisui) FurTufts(b, Body, V(0, 0.27f, 0.12f), V(0.075f, 0.085f, 0.06f), 18, 0.04f, 0.022f, cream, 2f, -0.8f, 0.9f);

        // A plume of a tail, cream, or a curl of white rock
        int tail = b.Tail(V(0, 0.29f, -0.17f));
        var tc = V(0, 0.37f, -0.24f);
        b.Limb(tail, V(0, 0.29f, -0.17f), tc, 0.03f, 0.04f, hisui ? rock : cream);
        b.Ell(tail, tc + V(0, 0.02f, -0.01f), V(0.062f, 0.085f, 0.06f), hisui ? rock : cream, V(-25f, 0, 0), hisui ? Shell : null);
        if (hisui) PokeBuilder.Both(s => b.Mark(tail, tc + V(0.058f * s, 0.025f, -0.01f), V(s, 0, 0), 0.03f, 0.03f, Rgb(168, 164, 158), MarkShape.Ring));
        else FurTufts(b, tail, tc + V(0, 0.02f, -0.01f), V(0.062f, 0.085f, 0.06f), 16, 0.035f, 0.022f, cream, 2f, -0.5f, 0.2f);

        int head = b.Head(V(0, 0.32f, 0.12f));
        var c = V(0, 0.4f, 0.15f);
        var r = V(0.1f, 0.09f, 0.09f);
        if (hisui)
        {
            // A hood of white rock over its head, its face looking out from under it, ears poking out at its sides
            b.Ell(head, c + V(0, 0.035f, -0.025f), V(0.14f, 0.12f, 0.13f), rock, mat: Shell);
            b.Spike(head, c + V(0, 0.13f, -0.03f), c + V(0, 0.21f, -0.07f), 0.05f, rock, mat: Shell);
            b.Cut(head, c + V(0, -0.025f, 0.12f), V(0.085f, 0.072f, 0.085f));
        }
        PokeBuilder.Both(s =>
        {
            int e = b.Ear(head, s, V(0.07f * s, 0.46f, 0.13f));
            if (hisui) CatEar(b, e, V(0.11f * s, 0.46f, 0.11f), V(0.18f * s, 0.5f, 0.1f), 0.035f, orange, Rgb(250, 200, 160));
            else CatEar(b, e, V(0.065f * s, 0.46f, 0.13f), V(0.115f * s, 0.55f, 0.115f), 0.042f, orange, Rgb(250, 214, 170));
        });
        b.Ell(head, c, r, orange);
        b.Ell(head, V(0, 0.365f, 0.235f), V(0.046f, 0.036f, 0.046f), orange, blend: 0.02f);
        if (!hisui) b.PaintEll(head, V(0, 0.343f, 0.24f), V(0.042f, 0.02f, 0.045f), cream);
        b.Ell(head, V(0, 0.384f, 0.28f), V(0.018f, 0.012f, 0.011f), stripe, mat: Shell, blend: 0.004f);
        if (!hisui)
            foreach (var (x, z, h) in new[] { (0f, 0.17f, 0.11f), (-0.03f, 0.15f, 0.085f), (0.03f, 0.15f, 0.085f), (-0.015f, 0.12f, 0.08f), (0.015f, 0.12f, 0.08f) })
                b.Spike(head, V(x, 0.465f, z), V(x * 1.6f, 0.465f + h, z - 0.07f), 0.03f, cream, 0.6f);
        if (hisui) Grin(b, head, V(0, 0.343f, 0.262f), V(0.03f, 0.016f, 0.02f), Rgb(170, 60, 70));
        else b.Mark(head, V(0, 0.35f, 0.274f), V(0, -0.3f, 1f), 0.02f, 0.007f, stripe, MarkShape.Wave);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 0.412f), V(0.45f * s, 0.1f, 1f), 0.024f, Rgb(96, 58, 40)));
        return b;
    }

    private static PokeBuilder Growlithe() => GrowlitheBuild(false);

    /// <summary>
    /// Arcanine and its Hisuian form: a great striped dog, proud under a heavy mane; the Hisuian's mane dark as smoke
    /// and streaked with fire, a horn of rock on its head.
    /// </summary>
    private static PokeBuilder ArcanineBuild(bool hisui)
    {
        // Everything above the paws stands this much higher than on a Growlithe's proportions: long, strong legs
        const float Lift = 0.09f;
        var b = new PokeBuilder(hisui ? "Arcanine-Hisui" : "Arcanine", 0.92f, BodyPlan.Quadruped, V(0, 0.38f + Lift, -0.02f)) { Coat = Fur };
        var orange = hisui ? Rgb(208, 74, 46) : Rgb(244, 150, 72);
        var mane = hisui ? Rgb(86, 88, 94) : Rgb(248, 228, 184);
        var face = hisui ? Rgb(70, 70, 78) : orange;
        var stripe = Rgb(40, 38, 48);
        foreach (var (z, front) in new[] { (0.18f, true), (-0.22f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.085f * s, 0.32f + Lift, z), front);
                b.Limb(leg, V(0.085f * s, 0.33f + Lift, z), V(0.09f * s, 0.05f, z + 0.01f), front ? 0.052f : 0.06f, 0.04f, orange);
                b.Ell(leg, V(0.091f * s, 0.03f, z + 0.035f), V(0.048f, 0.03f, 0.062f), orange);
                foreach (float y in new[] { 0.2f, 0.29f })
                    b.PaintEll(leg, V(0.088f * s, y, z), V(0.065f, 0.013f, 0.065f), stripe, V(0, 0, -16f * s), 0.008f);
                // Tufts of fur behind its ankles
                foreach (float t in new[] { -1f, 0f, 1f })
                    b.Spike(leg, V(0.09f * s + 0.015f * t, 0.13f, z - 0.025f), V(0.095f * s + 0.03f * t, 0.075f, z - 0.085f), 0.022f, mane, 0.6f);
            });
        var bc = V(0, 0.38f + Lift, -0.02f);
        var br = V(0.13f, 0.12f, 0.27f);
        b.Ell(Body, bc, br, orange);
        Stripes(b, Body, bc, br, stripe, 0.016f, 24f, 0.07f, -0.02f, -0.11f, -0.2f);
        // The great mane round its neck and chest
        var mc = V(0, 0.5f + Lift, 0.16f);
        var mr = hisui ? V(0.18f, 0.18f, 0.14f) : V(0.155f, 0.15f, 0.12f);
        b.Ell(Body, mc, mr, mane);
        FurTufts(b, Body, mc, mr, hisui ? 40 : 32, hisui ? 0.09f : 0.07f, 0.034f, mane, 2f, -0.8f, 0.7f);
        if (hisui)
            PokeBuilder.Both(s =>
            {
                foreach (var (y, z, a) in new[] { (0.45f, 0.25f, 20f), (0.55f, 0.21f, 35f), (0.47f, 0.09f, 15f) })
                    b.PaintEll(Body, V(0.17f * s, y + Lift, z), V(0.03f, 0.075f, 0.03f), orange, V(a, 0, 25f * s), 0.01f);
            });

        int tail = b.Tail(V(0, 0.44f + Lift, -0.28f));
        var tc = V(0, 0.55f + Lift, -0.39f);
        var tr = V(0.09f, 0.13f, 0.085f) * (hisui ? 1.2f : 1f);
        b.Limb(tail, V(0, 0.44f + Lift, -0.28f), tc, 0.04f, 0.06f, mane);
        b.Ell(tail, tc + V(0, 0.03f, -0.02f), tr, mane, V(-30f, 0, 0));
        FurTufts(b, tail, tc + V(0, 0.03f, -0.02f), tr, 22, 0.05f, 0.03f, mane, 2f, -0.6f, 0.3f);

        // A proud head held high on its mane, with a long muzzle
        int head = b.Head(V(0, 0.55f + Lift, 0.21f));
        var c = V(0, 0.64f + Lift, 0.25f);
        var r = V(0.115f, 0.1f, 0.1f);
        PokeBuilder.Both(s =>
        {
            int e = b.Ear(head, s, V(0.075f * s, c.Y + 0.07f, 0.23f));
            CatEar(b, e, V(0.075f * s, c.Y + 0.06f, 0.23f), V(0.135f * s, c.Y + 0.16f, 0.21f), 0.045f, hisui ? mane : orange, hisui ? Rgb(120, 120, 128) : Rgb(250, 214, 170));
        });
        b.Ell(head, c, r, face);
        b.Ell(head, V(0, c.Y - 0.04f, 0.345f), V(0.056f, 0.045f, 0.07f), face, blend: 0.02f);
        if (!hisui) b.PaintEll(head, V(0, c.Y - 0.065f, 0.36f), V(0.05f, 0.024f, 0.06f), mane);
        b.Ell(head, V(0, c.Y - 0.015f, 0.413f), V(0.022f, 0.015f, 0.013f), stripe, mat: Shell, blend: 0.004f);
        if (hisui)
        {
            // A horn of rock on its brow, and a mask of flame-red round its eyes
            b.Spike(head, c + V(0, 0.06f, 0.01f), c + V(0, 0.2f, -0.05f), 0.05f, Rgb(118, 118, 124), mat: Shell);
            PokeBuilder.Both(s => b.PaintEll(head, On(c, r, 0.05f * s, c.Y + 0.015f), V(0.05f, 0.03f, 0.04f), orange, V(0, 0, -15f * s)));
            Grin(b, head, V(0, c.Y - 0.065f, 0.385f), V(0.036f, 0.018f, 0.024f), Rgb(170, 60, 70));
        }
        else
        {
            foreach (var (x, z, h) in new[] { (0f, 0.27f, 0.12f), (-0.035f, 0.25f, 0.095f), (0.035f, 0.25f, 0.095f), (-0.018f, 0.21f, 0.09f), (0.018f, 0.21f, 0.09f) })
                b.Spike(head, V(x, c.Y + 0.08f, z), V(x * 1.6f, c.Y + 0.08f + h, z - 0.08f), 0.035f, mane, 0.6f);
            b.Mark(head, V(0, c.Y - 0.058f, 0.408f), V(0, -0.3f, 1f), 0.024f, 0.008f, stripe, MarkShape.Wave);
        }
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, c.Y + 0.015f), V(0.45f * s, 0.1f, 1f), 0.026f, hisui ? Rgb(250, 190, 60) : Rgb(150, 86, 36), glare: hisui));
        return b;
    }

    private static PokeBuilder Arcanine() => ArcanineBuild(false);

    // ------------------------------------------------------------------ Poliwag line

    /// <summary>
    /// A spiral painted on the front of an upright ellipsoid (<paramref name="c"/>, <paramref name="r"/>) round
    /// (<paramref name="cx"/>, <paramref name="cy"/>), widening from <paramref name="r0"/> to <paramref name="r1"/> over
    /// <paramref name="turns"/> turns, in short strokes along it; <paramref name="way"/> 1 or -1 winds it either way.
    /// </summary>
    private static void Swirl(PokeBuilder b, int bone, Vector3 c, Vector3 r, float cx, float cy, float r0, float r1, float turns, float width, Color color, float way = 1f)
    {
        float sweep = turns * MathF.Tau;
        int n = Math.Max(8, (int)(r1 * sweep / (width * 2f)));
        Vector2 At(float t)
        {
            float a = way * sweep * t, rho = r0 + (r1 - r0) * t;
            return new Vector2(cx + rho * MathF.Cos(a), cy + rho * MathF.Sin(a));
        }
        for (int i = 0; i < n; i++)
        {
            Vector2 p0 = At(i / (float)n), p1 = At((i + 1) / (float)n), d = p1 - p0, mid = (p0 + p1) * 0.5f;
            b.PaintEll(bone, On(c, r, mid.X, mid.Y), V(d.Length() * 0.5f + width * 0.45f, width * 0.5f, width * 1.6f), color,
                V(0, 0, MathF.Atan2(d.Y, d.X) / Degree), width * 0.35f);
        }
    }

    private static PokeBuilder Poliwag()
    {
        var b = new PokeBuilder("Poliwag", 0.55f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Scales };
        var blue = Rgb(82, 124, 198);
        var ink = Rgb(30, 30, 40);
        var fin = Rgb(178, 210, 238);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.1f, 0.02f));
            b.Limb(leg, V(0.09f * s, 0.11f, 0.02f), V(0.11f * s, 0.035f, 0.05f), 0.035f, 0.03f, blue);
            b.Ell(leg, V(0.115f * s, 0.028f, 0.065f), V(0.04f, 0.028f, 0.052f), blue);
        });
        // A round body with its eyes on top, its skin so thin that the spiral of its insides shows through
        var c = V(0, 0.24f, 0);
        var r = V(0.19f, 0.18f, 0.17f);
        b.Ell(Body, c, r, blue);
        var bump = V(0.068f, 0.06f, 0.06f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.078f * s, 0.385f, 0.06f), bump, blue, blend: 0.035f));
        b.PaintEll(Body, V(0, 0.215f, 0.14f), V(0.135f, 0.13f, 0.08f), White);
        Swirl(b, Body, c, r, 0, 0.215f, 0.004f, 0.112f, 2.5f, 0.019f, ink);
        b.Ell(Body, On(c, r, 0, 0.345f) - V(0, 0, 0.004f), V(0.026f, 0.016f, 0.016f), Rgb(234, 150, 178), mat: Shell, blend: 0.008f);
        PokeBuilder.Both(s => b.Eye(Body, On(V(0.078f * s, 0.385f, 0.06f), bump, 0.084f * s, 0.392f), V(0.3f * s, 0.15f, 1f), 0.037f, sclera: true, pupil: Black));
        // A long flat tail, pale and clear as a tadpole's
        int tail = b.Tail(V(0, 0.15f, -0.14f));
        b.Tube(tail, Smooth(3, V(0, 0.15f, -0.13f), V(0.04f, 0.12f, -0.27f), V(0.1f, 0.1f, -0.38f)), 0.034f, 0.022f, fin, blend: 0f);
        Frond(b, tail, V(0.04f, 0.11f, -0.25f), V(0.2f, 0.09f, -0.52f), 0.115f, fin, V(0, 1f, 0), 0.14f);
        return b;
    }

    /// <summary>
    /// Poliwhirl and Poliwrath: a round body with the spiral on its belly, its eyes on top, white gloves and strong
    /// legs; Poliwrath is broader, its arms thick with muscle and its spiral turning the other way.
    /// </summary>
    private static PokeBuilder PoliBuild(bool wrath)
    {
        var b = new PokeBuilder(wrath ? "Poliwrath" : "Poliwhirl", wrath ? 0.8f : 0.72f, BodyPlan.Biped, V(0, 0.4f, 0)) { Coat = Scales };
        var blue = wrath ? Rgb(66, 104, 184) : Rgb(84, 126, 200);
        var glove = Rgb(228, 232, 242);
        var ink = Rgb(30, 30, 40);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.2f, 0.01f));
            b.Limb(leg, V(0.1f * s, 0.21f, 0.01f), V(0.12f * s, 0.06f, 0.03f), wrath ? 0.06f : 0.05f, 0.042f, blue);
            b.Ell(leg, V(0.125f * s, 0.035f, 0.07f), V(0.066f, 0.035f, 0.088f), blue);
        });
        var c = V(0, 0.4f, 0);
        var r = wrath ? V(0.24f, 0.22f, 0.2f) : V(0.23f, 0.23f, 0.21f);
        b.Ell(Body, c, r, blue);
        if (wrath) PokeBuilder.Both(s => b.Ell(Body, V(0.16f * s, 0.5f, -0.01f), V(0.11f, 0.08f, 0.1f), blue, blend: 0.06f));
        var bump = V(0.072f, 0.062f, 0.064f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.09f * s, 0.6f, 0.07f), bump, blue, blend: 0.04f));
        b.PaintEll(Body, V(0, 0.38f, 0.16f), V(0.175f, 0.17f, 0.09f), White);
        Swirl(b, Body, c, r, 0, 0.38f, 0.004f, 0.155f, 2.7f, 0.025f, ink, wrath ? -1f : 1f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.2f * s, 0.47f, 0.01f));
            // Arms bent up like a boxer's, fists in white gloves
            var elbow = V(0.31f * s, 0.42f, 0.03f);
            var hand = V(0.35f * s, wrath ? 0.53f : 0.56f, 0.13f);
            b.Limb(arm, V(0.18f * s, 0.47f, 0.01f), elbow, wrath ? 0.054f : 0.04f, wrath ? 0.046f : 0.035f, blue);
            b.Limb(arm, elbow, hand, wrath ? 0.046f : 0.035f, wrath ? 0.042f : 0.032f, blue);
            if (wrath) b.Ell(arm, V(0.25f * s, 0.46f, 0.02f), V(0.07f, 0.055f, 0.058f), blue, blend: 0.03f);
            b.Ell(arm, hand + V(0.02f * s, 0, 0.02f), V(0.07f, 0.064f, 0.066f) * (wrath ? 1.12f : 1f), glove, mat: Fur);
        });
        PokeBuilder.Both(s => b.Eye(Body, On(V(0.09f * s, 0.6f, 0.07f), bump, 0.097f * s, 0.607f), V(0.3f * s, 0.15f, 1f), 0.039f, sclera: true, pupil: Black, glare: wrath));
        return b;
    }

    private static PokeBuilder Poliwhirl() => PoliBuild(false);

    private static PokeBuilder Poliwrath() => PoliBuild(true);

    // ------------------------------------------------------------------ Bellsprout line

    /// <summary>
    /// A bell's mouth at <paramref name="at"/>, opening along <paramref name="facing"/>: a hollow carved in and painted
    /// dark, <paramref name="depth"/> times the radius deep, and a fleshy lip round it.
    /// </summary>
    private static void BellMouth(PokeBuilder b, int bone, Vector3 at, Vector3 facing, float radius, Color lip, Color inside, float depth = 0.7f)
    {
        var f = Vector3.Normalize(facing);
        var turn = Euler(f);
        b.Cut(bone, at + f * radius * 0.3f, V(radius * 0.8f, radius * (depth + 0.3f), radius * 0.8f), turn);
        b.PaintEll(bone, at - f * radius * depth * 0.5f, V(radius * 0.85f, radius * depth * 0.9f, radius * 0.85f), inside, turn);
        b.Torus(bone, at, radius, radius * 0.3f, lip, turn, mat: Shell, blend: 0.012f);
    }

    /// <summary>A broad leaf from <paramref name="root"/> narrowing to a point at <paramref name="tip"/>, flat across <paramref name="facing"/>.</summary>
    private static void PointedLeaf(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float width, Color color, Vector3 facing, float thin = 0.16f)
    {
        var d = tip - root;
        Frond(b, bone, root, root + d * 0.78f, width, color, facing, thin, Leaf);
        Blade(b, bone, root + d * 0.45f, tip, width * 0.72f, color, facing, thin * 0.9f, Leaf);
    }

    private static PokeBuilder Bellsprout()
    {
        var b = new PokeBuilder("Bellsprout", 0.62f, BodyPlan.Biped, V(0, 0.25f, 0)) { Coat = Leaf };
        var stem = Rgb(146, 134, 84);
        var bell = Rgb(242, 228, 112);
        var leaf = Rgb(126, 196, 104);
        var lip = Rgb(234, 164, 164);
        // Roots for feet, splayed over the ground
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.02f * s, 0.09f, 0));
            var end = V(0.15f * s, 0.012f, 0.03f);
            b.Tube(leg, Smooth(2, V(0.004f * s, 0.11f, 0), V(0.06f * s, 0.04f, 0.012f), end), 0.015f, 0.01f, stem, blend: 0f);
            foreach (var (dx, dz) in new[] { (0.05f, 0.04f), (0.06f, -0.025f) })
                b.Tube(leg, new[] { end - V(0.03f * s, -0.002f, 0.005f), end + V(dx * s, -0.002f, dz) }, 0.009f, 0.007f, stem, blend: 0f);
        });
        // A thin stem, bent like a stalk in the wind, and two leaves held out from it like arms
        b.Tube(Body, Smooth(3, V(0, 0.1f, 0), V(0.025f, 0.24f, -0.01f), V(-0.012f, 0.38f, -0.02f), V(0, 0.53f, -0.02f)), 0.017f, 0.014f, stem, blend: 0f);
        var node = V(0.01f, 0.3f, -0.012f);
        b.Ell(Body, node, V(0.022f, 0.018f, 0.02f), stem);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, node);
            b.Limb(arm, node, node + V(0.04f * s, 0.006f, 0.004f), 0.01f, 0.009f, stem);
            PointedLeaf(b, arm, node + V(0.03f * s, 0.005f, 0.004f), node + V(0.23f * s, 0.04f, 0.03f), 0.06f, leaf, V(0, 1f, 0.35f));
        });
        // A bell of a head, its mouth at the front
        int head = b.Head(V(0, 0.5f, -0.035f));
        var c = V(0, 0.62f, 0.0f);
        var hr = V(0.094f, 0.135f, 0.1f);
        var turn = V(-25f, 0, 0);
        b.Ell(head, c, hr, bell, turn);
        BellMouth(b, head, Out(c, hr, turn, V(0, -0.45f, 1f)), V(0, -0.4f, 1f), 0.045f, lip, Rgb(110, 50, 60));
        PokeBuilder.Both(s => b.Eye(head, Out(c, hr, turn, V(0.62f * s, 0.25f, 0.8f)), V(0.62f * s, 0.25f, 0.8f), 0.016f));
        return b;
    }

    private static PokeBuilder Weepinbell()
    {
        var b = new PokeBuilder("Weepinbell", 0.66f, BodyPlan.Floating, V(0, 0.38f, 0)) { Coat = Leaf }.Hover();
        var yellow = Rgb(238, 228, 98);
        var spot = Rgb(176, 188, 72);
        var leaf = Rgb(126, 196, 104);
        var lip = Rgb(234, 164, 164);
        var stalk = Rgb(136, 96, 62);
        // Two broad leaves it flaps as it floats
        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.12f * s, 0.44f, -0.02f));
            PointedLeaf(b, w, V(0.1f * s, 0.44f, -0.02f), V(0.4f * s, 0.52f, -0.05f), 0.095f, leaf, V(0, 0.35f, 1f));
        });
        // A bell of a body, narrow at the top and wide at its great mouth, hanging from a hooked stalk
        var c = V(0, 0.38f, 0.02f);
        var r = V(0.15f, 0.2f, 0.15f);
        var turn = V(-40f, 0, 0);
        b.Ell(Body, c, r, yellow, turn);
        var top = Out(c, r, turn, V(0, 1f, -0.6f));
        b.Spike(Body, c + (top - c) * 0.4f, top + (top - c) * 0.35f, 0.09f, yellow);
        foreach (var d in new[] { V(0.7f, 0.5f, 0.3f), V(-0.75f, 0.3f, 0.4f), V(0.5f, -0.2f, -0.8f), V(-0.4f, 0.4f, -0.8f), V(0.85f, -0.3f, -0.2f), V(-0.8f, -0.4f, -0.1f), V(0.2f, 0.9f, -0.2f) })
            b.PaintEll(Body, Out(c, r, turn, d), V(0.022f, 0.02f, 0.022f), spot);
        BellMouth(b, Body, Out(c, r, turn, V(0, -0.5f, 1f)), V(0, -0.45f, 1f), 0.085f, lip, Rgb(110, 48, 62), 0.9f);
        var tip = top + (top - c) * 0.3f;
        b.Tube(Body, Smooth(3, top, tip + V(0, 0.06f, -0.02f), tip + V(0, 0.1f, 0.04f), tip + V(0, 0.075f, 0.09f)), 0.017f, 0.011f, stalk, blend: 0f);
        PokeBuilder.Both(s => b.Eye(Body, Out(c, r, turn, V(0.42f * s, 0.32f, 0.85f)), V(0.42f * s, 0.32f, 0.85f), 0.03f, sclera: true, pupil: Black));
        return Lift(b);
    }

    private static PokeBuilder Victreebel()
    {
        var b = new PokeBuilder("Victreebel", 0.82f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Leaf };
        var yellow = Rgb(212, 220, 98);
        var spot = Rgb(150, 172, 64);
        var leaf = Rgb(100, 176, 90);
        var lip = Rgb(232, 172, 160);
        var vine = Rgb(130, 96, 60);
        PokeBuilder.Both(s =>
        {
            int w = b.Arm(s, V(0.17f * s, 0.27f, 0.02f));
            PointedLeaf(b, w, V(0.15f * s, 0.27f, 0.03f), V(0.46f * s, 0.36f, 0.1f), 0.095f, leaf, V(0.2f * s, 0.6f, 1f));
        });
        // A great pitcher, round below, its mouth gaping up and forward
        var c = V(0, 0.3f, 0);
        var r = V(0.21f, 0.3f, 0.2f);
        var turn = V(-12f, 0, 0);
        b.Ell(Body, c, r, yellow, turn);
        foreach (var d in new[] { V(0.7f, 0.1f, 0.6f), V(-0.6f, -0.2f, 0.7f), V(0.2f, -0.6f, 0.8f), V(0.85f, -0.3f, -0.3f), V(-0.8f, 0.2f, -0.4f), V(0.3f, 0.1f, -0.95f), V(-0.3f, -0.5f, -0.8f), V(0.75f, 0.55f, -0.2f) })
            b.PaintEll(Body, Out(c, r, turn, d), V(0.028f, 0.034f, 0.028f), spot);
        var f = Vector3.Normalize(V(0, 0.75f, 0.66f));
        var m = Out(c, r, turn, f);
        var front = V(0, -f.Z, f.Y);
        BellMouth(b, Body, m, f, 0.135f, lip, Rgb(70, 30, 40), 1.1f);
        // Two little fangs on its lower lip, and its eyes deep in its mouth
        PokeBuilder.Both(s => b.Spike(Body, m + front * 0.11f + V(0.04f * s, 0, 0), m + front * 0.09f + f * 0.035f + V(0.036f * s, 0, 0), 0.012f, White, mat: Shell, blend: 0.004f));
        PokeBuilder.Both(s => b.Eye(Body, m - f * 0.047f - front * 0.0845f + V(0.045f * s, 0, 0), front * 0.85f + V(-0.45f * s, 0, 0) + f * 0.5f, 0.02f, sclera: true, pupil: Black));
        // The leaf that stands up behind its mouth and leans over it like a lid
        PointedLeaf(b, Body, Out(c, r, turn, V(0, 0.6f, -0.8f)) + V(0, -0.02f, 0.03f), V(0, 0.88f, 0.08f), 0.15f, leaf, V(0, 0.3f, 1f), 0.12f);
        // A long vine from its back, a leaf at its end
        int tail = b.Tail(V(0, 0.17f, -0.18f));
        b.Tube(tail, Smooth(3, V(0, 0.17f, -0.17f), V(-0.12f, 0.1f, -0.3f), V(-0.3f, 0.08f, -0.25f), V(-0.4f, 0.14f, -0.1f)), 0.016f, 0.012f, vine, blend: 0f);
        Frond(b, tail, V(-0.4f, 0.14f, -0.1f), V(-0.46f, 0.08f, 0.04f), 0.04f, leaf, V(1f, 0.3f, 0), 0.2f, Leaf);
        return b;
    }

    // ------------------------------------------------------------------ Slowpoke line

    /// <summary>
    /// A Slowpoke's head at <paramref name="c"/>, <paramref name="k"/> times the size: round, with a broad pale muzzle
    /// over a dopey open mouth, little curled ears and blank round eyes; <paramref name="crown"/> colours its top and ears.
    /// </summary>
    private static void SlowHead(PokeBuilder b, int head, Vector3 c, float k, Color pink, Color? crown, Color muzzle)
    {
        var r = V(0.15f, 0.13f, 0.13f) * k;
        b.Ell(head, c, r, pink);
        PokeBuilder.Both(s => b.Torus(head, c + V(0.105f * s, 0.095f, -0.02f) * k, 0.026f * k, 0.013f * k, pink, V(0, 0, 90f - 30f * s), mat: Scales));
        if (crown is Color top) b.PaintEll(head, c + V(0, 0.12f, -0.03f) * k, V(0.19f, 0.08f, 0.17f) * k, top, soft: 0.02f * k);
        b.Ell(head, c + V(0, -0.04f, 0.105f) * k, V(0.115f, 0.05f, 0.07f) * k, muzzle, blend: 0.02f);
        Grin(b, head, c + V(0, -0.085f, 0.115f) * k, V(0.08f, 0.026f, 0.035f) * k, Rgb(176, 72, 96));
        PokeBuilder.Both(s => b.Spike(head, c + V(0.04f * s, -0.055f, 0.13f) * k, c + V(0.04f * s, -0.088f, 0.136f) * k, 0.009f * k, White, mat: Shell, blend: 0.003f));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.062f * k * s, c.Y + 0.045f * k), V(0.45f * s, 0.1f, 1f), 0.032f * k, sclera: true, pupil: Black));
    }

    /// <summary>
    /// A Shellder clamped on whatever goes into its open end at <paramref name="mouth"/>: a grey shell spiralling to a
    /// point along <paramref name="axis"/>, banded and bristling with spikes, white teeth round its dark opening.
    /// </summary>
    private static void ShellderOn(PokeBuilder b, int bone, Vector3 mouth, Vector3 axis, float size, Color shell, Color spike, Color band)
    {
        var a = Vector3.Normalize(axis);
        b.Ell(bone, mouth + a * size * 0.5f, V(size * 1.05f, size * 0.75f, size * 1.05f), shell, Euler(a), Shell);
        b.Spike(bone, mouth + a * size * 0.6f, mouth + a * size * 2.3f, size * 0.95f, shell, mat: Shell);
        for (int i = 0; i < 3; i++)
            b.PaintTorus(bone, mouth + a * size * (0.75f + 0.42f * i), size * (0.98f - 0.27f * i), size * 0.1f, band, Euler(a + V(0.08f, 0, 0)));
        var u = Vector3.Normalize(Vector3.Cross(a, MathF.Abs(a.Y) > 0.9f ? V(1f, 0, 0) : V(0, 1f, 0)));
        var w = Vector3.Cross(a, u);
        for (int i = 0; i < 6; i++)
        {
            float turn = i * MathF.Tau / 6f + 0.3f, along = i % 2 == 0 ? 0.65f : 1.25f;
            var outward = u * MathF.Cos(turn) + w * MathF.Sin(turn);
            var root = mouth + a * size * along + outward * size * (1.02f - along * 0.32f);
            b.Spike(bone, root - outward * size * 0.1f, root + (outward + a * 0.45f) * size * 0.5f, size * 0.17f, spike, mat: Shell);
        }
        b.PaintEll(bone, mouth + a * size * 0.05f, V(size * 0.82f, size * 0.2f, size * 0.82f), Rgb(60, 40, 56), Euler(a));
        for (int i = 0; i < 8; i++)
        {
            float turn = i * MathF.Tau / 8f;
            var rim = mouth + a * size * 0.12f + (u * MathF.Cos(turn) + w * MathF.Sin(turn)) * size * 0.8f;
            b.Spike(bone, rim + a * size * 0.05f, rim - a * size * 0.16f, size * 0.09f, White, mat: Shell, blend: 0.004f);
        }
    }

    /// <summary>Slowpoke and its Galarian form: lying low, a long thick tail curved up behind it; the Galarian's crown and tail tip yellow.</summary>
    private static PokeBuilder SlowpokeBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Slowpoke-Galar" : "Slowpoke", 0.6f, BodyPlan.Quadruped, V(0, 0.15f, -0.06f)) { Coat = Scales };
        var pink = Rgb(240, 160, 184);
        var cream = Rgb(246, 232, 204);
        var yellow = Rgb(248, 222, 92);
        foreach (var (z, front) in new[] { (0.09f, true), (-0.21f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.1f * s, 0.12f, z), front);
                b.Limb(leg, V(0.1f * s, 0.12f, z), V(0.13f * s, 0.04f, z + 0.02f), 0.045f, 0.038f, pink);
                b.Ell(leg, V(0.135f * s, 0.028f, z + 0.04f), V(0.045f, 0.028f, 0.05f), pink);
                foreach (float t in new[] { -1f, 1f })
                    b.Ell(leg, V(0.135f * s + 0.018f * t, 0.016f, z + 0.083f), V(0.011f, 0.011f, 0.013f), White, mat: Shell, blend: 0.004f);
            });
        b.Ell(Body, V(0, 0.15f, -0.06f), V(0.15f, 0.1f, 0.24f), pink);
        int head = b.Head(V(0, 0.2f, 0.13f));
        SlowHead(b, head, V(0, 0.26f, 0.21f), 1.15f, pink, galar ? yellow : null, cream);
        int tail = b.Tail(V(0, 0.18f, -0.28f));
        var path = Smooth(3, V(0, 0.17f, -0.27f), V(0, 0.28f, -0.42f), V(0, 0.47f, -0.4f), V(0, 0.56f, -0.26f));
        b.Tube(tail, path, 0.055f, 0.04f, pink, blend: 0f);
        b.PaintEll(tail, path[^1], V(0.065f, 0.065f, 0.075f), galar ? yellow : Rgb(250, 226, 232));
        return b;
    }

    private static PokeBuilder Slowpoke() => SlowpokeBuild(false);

    /// <summary>
    /// Slowbro and its Galarian form: standing, heavy, its pale belly ringed; a Shellder clamped on its tail, or on
    /// the Galarian's arm, whose crown and tail tip are purple.
    /// </summary>
    private static PokeBuilder SlowbroBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Slowbro-Galar" : "Slowbro", 0.84f, BodyPlan.Biped, V(0, 0.34f, 0)) { Coat = Scales };
        var pink = Rgb(240, 160, 184);
        var purple = Rgb(150, 104, 190);
        var cream = Rgb(240, 230, 196);
        var line = Rgb(204, 188, 150);
        var shell = Rgb(182, 194, 210);
        var spike = Rgb(140, 148, 166);
        var band = Rgb(146, 158, 178);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.1f * s, 0.2f, 0));
            b.Limb(leg, V(0.1f * s, 0.21f, 0), V(0.12f * s, 0.06f, 0.02f), 0.075f, 0.06f, pink);
            b.Ell(leg, V(0.125f * s, 0.035f, 0.05f), V(0.06f, 0.035f, 0.075f), pink);
            foreach (float t in new[] { -1f, 1f })
                b.Ell(leg, V(0.125f * s + 0.024f * t, 0.02f, 0.118f), V(0.012f, 0.012f, 0.014f), White, mat: Shell, blend: 0.004f);
        });
        b.Ell(Body, V(0, 0.34f, 0), V(0.18f, 0.21f, 0.15f), pink);
        b.PaintEll(Body, V(0, 0.3f, 0.1f), V(0.13f, 0.15f, 0.08f), cream);
        foreach (float y in new[] { 0.23f, 0.29f, 0.35f })
            b.PaintEll(Body, V(0, y, 0.145f), V(0.11f, 0.007f, 0.04f), line, soft: 0.008f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, 0.44f, 0.03f));
            var hand = V(0.2f * s, 0.32f, 0.12f);
            b.Limb(arm, V(0.15f * s, 0.44f, 0.03f), hand, 0.045f, 0.038f, pink);
            foreach (float t in new[] { -1f, 0f, 1f })
                b.Spike(arm, hand + V(0.012f * t, -0.02f, 0.02f), hand + V(0.016f * t, -0.046f, 0.036f), 0.009f, White, mat: Shell, blend: 0.004f);
            if (galar && s > 0) ShellderOn(b, arm, V(0.17f * s, 0.4f, 0.06f), V(0.25f * s, -0.45f, 1f), 0.11f, shell, spike, band);
        });
        int head = b.Head(V(0, 0.52f, 0.02f));
        SlowHead(b, head, V(0, 0.65f, 0.05f), 0.95f, pink, galar ? purple : null, cream);
        int tail = b.Tail(V(0, 0.22f, -0.13f));
        var path = Smooth(3, V(0, 0.22f, -0.13f), V(0.08f, 0.12f, -0.28f), V(0.2f, 0.09f, -0.34f), V(0.3f, 0.12f, -0.3f));
        b.Tube(tail, path, 0.06f, 0.05f, pink, blend: 0f);
        if (galar) b.PaintEll(tail, path[^1], V(0.085f, 0.075f, 0.085f), purple);
        else ShellderOn(b, tail, path[^1] - Vector3.Normalize(path[^1] - path[^2]) * 0.03f, path[^1] - path[^2], 0.13f, shell, spike, band);
        return b;
    }

    private static PokeBuilder Slowbro() => SlowbroBuild(false);

    // ------------------------------------------------------------------ Farfetch'd

    /// <summary>A leek from its white foot at <paramref name="foot"/> up to its green leaves, split and spread at <paramref name="top"/>.</summary>
    private static void Leek(PokeBuilder b, int bone, Vector3 foot, Vector3 top, float r, Vector3 facing)
    {
        var green = Rgb(96, 170, 72);
        var d = top - foot;
        b.Limb(bone, foot, foot + d * 0.55f, r, r * 0.95f, Rgb(242, 244, 232), Leaf, 0.01f);
        b.Limb(bone, foot + d * 0.5f, foot + d * 0.65f, r * 0.95f, r, green, Leaf, 0.02f);
        var side = Vector3.Normalize(Vector3.Cross(d, facing));
        foreach (float t in new[] { -1f, 0f, 1f })
            Frond(b, bone, foot + d * 0.6f, top + side * t * d.Length() * 0.12f, r * 1.25f, green, facing, 0.3f, Leaf);
    }

    /// <summary>
    /// Farfetch'd and its Galarian form: a plump duck with a flat yellow bill and dark brows, a leek under its wing;
    /// the Galarian dark and fierce, its leek's stalk raised like a sword and its leaves a shield on its back.
    /// </summary>
    private static PokeBuilder FarfetchdBuild(bool galar)
    {
        var b = new PokeBuilder(galar ? "Farfetch'd-Galar" : "Farfetch'd", 0.7f, BodyPlan.Biped, V(0, 0.3f, 0)) { Coat = Fur };
        var brown = galar ? Rgb(108, 82, 70) : Rgb(178, 158, 140);
        var dark = galar ? Rgb(64, 48, 44) : Rgb(116, 96, 84);
        var belly = galar ? Rgb(236, 226, 206) : Rgb(212, 198, 182);
        var beak = Rgb(244, 192, 64);
        var feet = Rgb(244, 178, 52);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.055f * s, 0.15f, 0));
            b.Limb(leg, V(0.055f * s, 0.16f, 0), V(0.065f * s, 0.03f, 0.02f), 0.018f, 0.015f, feet, Scales);
            Frond(b, leg, V(0.065f * s, 0.016f, 0.0f), V(0.075f * s, 0.012f, 0.1f), 0.042f, feet, V(0, 1f, 0), 0.25f, Scales);
        });
        b.Ell(Body, V(0, 0.3f, -0.01f), V(0.13f, 0.16f, 0.13f), brown, V(12f, 0, 0));
        b.PaintEll(Body, V(0, 0.26f, 0.08f), V(0.1f, 0.13f, 0.08f), belly);
        int tail = b.Tail(V(0, 0.22f, -0.12f));
        Blade(b, tail, V(0, 0.22f, -0.1f), V(0, 0.26f, -0.27f), 0.05f, dark, V(0, 1f, 0), 0.3f);
        // Folded wings, the one on its left gripping the leek
        int grip = 0;
        PokeBuilder.Both(s =>
        {
            int w = FoldedWing(b, s, V(0.12f * s, 0.38f, 0.02f), V(0.15f * s, 0.22f, -0.12f), 0.07f, brown, dark);
            if (s < 0) grip = w;
        });
        b.Ell(grip, V(-0.13f, 0.3f, 0.06f), V(0.045f, 0.065f, 0.05f), brown, V(0, 0, 10f));
        if (galar)
        {
            b.Limb(grip, V(-0.14f, 0.31f, 0.07f), V(-0.3f, 0.74f, -0.06f), 0.026f, 0.024f, Rgb(242, 244, 232), Leaf, 0.01f);
            PokeBuilder.Both(s => Frond(b, Body, V(0.03f * s + 0.02f, 0.34f, -0.1f), V(0.09f * s + 0.12f, 0.12f, -0.52f), 0.075f, Rgb(70, 140, 60), V(0, 1f, 0.3f), 0.18f, Leaf));
        }
        else Leek(b, grip, V(-0.15f, 0.06f, 0.1f), V(-0.22f, 0.78f, -0.02f), 0.022f, V(0, 0, 1f));
        int head = b.Head(V(0, 0.44f, 0.02f));
        var c = V(0, 0.54f, 0.04f);
        var r = V(0.095f, 0.09f, 0.09f);
        b.Ell(head, c, r, brown);
        b.Ell(head, V(0, 0.505f, 0.15f), V(0.055f, 0.017f, 0.065f), beak, mat: Shell, blend: 0.01f);
        b.Ell(head, V(0, 0.49f, 0.13f), V(0.045f, 0.012f, 0.05f), beak, mat: Shell, blend: 0.008f);
        foreach (float x in new[] { -0.02f, 0f, 0.02f })
            b.Spike(head, V(x, 0.61f, 0.03f), V(x * 2.2f, 0.7f, -0.04f), 0.018f, dark, 0.5f);
        PokeBuilder.Both(s => b.Mark(head, On(c, r, 0.042f * s, 0.578f), V(0.4f * s, 0.3f, 1f), 0.03f, 0.008f, Rgb(40, 34, 34), MarkShape.Bar, -22f * s));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 0.552f), V(0.45f * s, 0.05f, 1f), 0.02f, glare: galar));
        return b;
    }

    private static PokeBuilder Farfetchd() => FarfetchdBuild(false);

    // ------------------------------------------------------------------ Doduo line

    /// <summary>
    /// One head of a Doduo or Dodrio on a long black neck rising from <paramref name="from"/>: a round head at
    /// <paramref name="at"/> with a long thin beak pointing along <paramref name="look"/>, and its two eyes.
    /// </summary>
    private static void BirdHead(PokeBuilder b, int bone, Vector3 from, Vector3 at, Vector3 look, float r, Color fur, Color neck, Color beak, bool glare)
    {
        b.Tube(bone, Smooth(3, from, Vector3.Lerp(from, at, 0.55f) + V(0, 0, -0.025f), at - V(0, r * 0.6f, 0)), r * 0.3f, r * 0.26f, neck, blend: 0f);
        b.Ell(bone, at, V(r, r * 0.95f, r), fur);
        var d = Vector3.Normalize(look);
        b.Spike(bone, at + d * r * 0.6f, at + d * r * 3.4f, r * 0.26f, beak, 0.75f, Shell);
        var side = Vector3.Normalize(Vector3.Cross(V(0, 1f, 0), d));
        PokeBuilder.Both(s =>
        {
            var e = Vector3.Normalize(d * 0.55f + side * 0.75f * s + V(0, 0.35f, 0));
            b.Eye(bone, at + e * r * 0.97f, e, r * 0.21f, Black, glare: glare);
        });
    }

    private static PokeBuilder Doduo()
    {
        var b = new PokeBuilder("Doduo", 0.78f, BodyPlan.Biped, V(0, 0.36f, 0)) { Coat = Fur };
        var fur = Rgb(190, 136, 96);
        var neck = Rgb(44, 40, 44);
        var beak = Rgb(232, 212, 180);
        BirdLegs(b, 0.045f, 0.3f, 0, 0.015f, Rgb(224, 198, 160));
        var c = V(0, 0.36f, 0);
        var r = V(0.12f, 0.11f, 0.11f);
        b.Ell(Body, c, r, fur);
        FurTufts(b, Body, c, r, 34, 0.035f, 0.02f, fur, 2f, -0.8f, 0.5f);
        // Two heads on long black necks, one held higher than the other, each looking its own way
        int left = b.Part("headL", Body, V(-0.04f, 0.45f, 0), PokeRole.Head, 0.6f, -1f);
        int right = b.Part("headR", Body, V(0.04f, 0.45f, 0), PokeRole.Head, 2.3f, 1f);
        BirdHead(b, left, V(-0.04f, 0.44f, 0.01f), V(-0.1f, 0.71f, 0.03f), V(-1f, -0.05f, 0.5f), 0.074f, fur, neck, beak, false);
        BirdHead(b, right, V(0.04f, 0.44f, 0.01f), V(0.07f, 0.8f, 0.02f), V(-0.25f, 0.85f, 0.45f), 0.074f, fur, neck, beak, false);
        return b;
    }

    private static PokeBuilder Dodrio()
    {
        var b = new PokeBuilder("Dodrio", 0.92f, BodyPlan.Biped, V(0, 0.44f, 0)) { Coat = Fur };
        var fur = Rgb(192, 136, 96);
        var dark = Rgb(58, 50, 52);
        var neck = Rgb(44, 40, 44);
        var beak = Rgb(232, 212, 180);
        var plume = Rgb(232, 132, 128);
        BirdLegs(b, 0.055f, 0.36f, 0, 0.019f, Rgb(224, 198, 160));
        var c = V(0, 0.44f, 0);
        var r = V(0.15f, 0.13f, 0.14f);
        b.Ell(Body, c, r, fur);
        FurTufts(b, Body, c, r, 40, 0.04f, 0.022f, fur, 2f, -0.8f, 0.5f);
        b.PaintEll(Body, c - V(0, 0.08f, 0), V(0.19f, 0.08f, 0.18f), dark);
        int tail = b.Tail(V(0, 0.46f, -0.13f));
        TailFan(b, tail, V(0, 0.47f, -0.12f), 5, 50f, 0.22f, 0.045f, plume, PixelCanvas.Shadow(plume, 0.2f), 0.1f);
        // Three heads, of three minds: joy, anger and sorrow
        int l = b.Part("headL", Body, V(-0.06f, 0.54f, 0), PokeRole.Head, 0.4f, -1f);
        int m = b.Part("headM", Body, V(0, 0.55f, -0.02f), PokeRole.Head, 1.7f);
        int rr = b.Part("headR", Body, V(0.06f, 0.54f, 0), PokeRole.Head, 3.1f, 1f);
        BirdHead(b, l, V(-0.06f, 0.53f, 0.02f), V(-0.17f, 0.79f, 0.04f), V(-1f, 0.15f, 0.4f), 0.076f, fur, neck, beak, true);
        BirdHead(b, m, V(0, 0.54f, -0.01f), V(0.0f, 0.9f, -0.02f), V(0.15f, 0.25f, 1f), 0.078f, fur, neck, beak, true);
        BirdHead(b, rr, V(0.06f, 0.53f, 0.02f), V(0.16f, 0.82f, 0.05f), V(1f, 0.05f, 0.45f), 0.076f, fur, neck, beak, false);
        foreach (float x in new[] { -0.02f, 0.02f })
            b.Spike(m, V(x, 0.95f, -0.03f), V(x * 3f, 1.04f, -0.12f), 0.018f, dark, 0.5f);
        return b;
    }

    // ------------------------------------------------------------------ Seel line

    /// <summary>A seal's muzzle at <paramref name="at"/>: pale and broad, a black nose on top and two small tusks hanging from it.</summary>
    private static void SealMuzzle(PokeBuilder b, int bone, Vector3 at, float k, Color color, bool tongue)
    {
        b.Ell(bone, at, V(0.075f, 0.045f, 0.05f) * k, color, blend: 0.02f);
        b.Ell(bone, at + V(0, 0.035f, 0.035f) * k, V(0.02f, 0.013f, 0.012f) * k, Rgb(40, 36, 44), mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s => b.Spike(bone, at + V(0.035f * s, -0.02f, 0.03f) * k, at + V(0.04f * s, -0.075f, 0.045f) * k, 0.014f * k, White, mat: Shell, blend: 0.004f));
        if (tongue) b.Ell(bone, at + V(0, -0.045f, 0.035f) * k, V(0.032f, 0.02f, 0.03f) * k, Rgb(232, 168, 196), V(30f, 0, 0), Shell, 0.006f);
    }

    private static PokeBuilder Seel()
    {
        var b = new PokeBuilder("Seel", 0.62f, BodyPlan.Quadruped, V(0, 0.15f, -0.02f)) { Coat = Fur };
        var pale = Rgb(226, 234, 246);
        // Front flippers spread on the ground
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.1f, 0.08f));
            Frond(b, leg, V(0.1f * s, 0.07f, 0.08f), V(0.3f * s, 0.025f, 0.15f), 0.06f, pale, V(0, 1f, 0), 0.3f);
        });
        b.Ell(Body, V(0, 0.15f, -0.03f), V(0.15f, 0.12f, 0.24f), pale);
        // Its tail rises behind it into a fluke of two lobes, curled over at their tips
        int tail = b.Tail(V(0, 0.2f, -0.22f));
        b.Tube(tail, Smooth(3, V(0, 0.17f, -0.22f), V(0, 0.3f, -0.32f), V(0, 0.46f, -0.3f)), 0.08f, 0.045f, pale, blend: 0f);
        PokeBuilder.Both(s => Frond(b, tail, V(0.02f * s, 0.46f, -0.3f), V(0.17f * s, 0.53f, -0.24f), 0.065f, pale, V(0, 0.4f, -1f), 0.3f));
        int head = b.Head(V(0, 0.18f, 0.16f));
        var c = V(0, 0.22f, 0.22f);
        var r = V(0.12f, 0.11f, 0.11f);
        b.Ell(head, c, r, pale);
        b.Spike(head, V(0, 0.3f, 0.21f), V(0, 0.39f, 0.18f), 0.03f, pale);
        SealMuzzle(b, head, V(0, 0.17f, 0.31f), 1f, Rgb(218, 198, 162), true);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.05f * s, 0.25f), V(0.45f * s, 0.05f, 1f), 0.024f, Rgb(40, 80, 90)));
        return b;
    }

    private static PokeBuilder Dewgong()
    {
        var b = new PokeBuilder("Dewgong", 0.86f, BodyPlan.Fish, V(0, 0.3f, -0.05f)) { Coat = Fur }.Hover();
        var white = Rgb(238, 242, 250);
        var fin = Rgb(226, 234, 246);
        // A long white body arched back into a great tail fluke
        b.Tube(Body, Smooth(3, V(0, 0.38f, 0.1f), V(0, 0.24f, -0.06f), V(0, 0.22f, -0.28f), V(0, 0.36f, -0.44f)), 0.14f, 0.055f, white, blend: 0f);
        int tail = b.Tail(V(0, 0.34f, -0.42f));
        PokeBuilder.Both(s => Frond(b, tail, V(0.02f * s, 0.38f, -0.44f), V(0.2f * s, 0.54f, -0.5f), 0.08f, fin, V(0, 0.3f, -1f), 0.18f));
        PokeBuilder.Both(s =>
        {
            int fl = b.Leg(s, V(0.11f * s, 0.28f, 0.06f));
            Frond(b, fl, V(0.1f * s, 0.27f, 0.07f), V(0.27f * s, 0.17f, 0.13f), 0.06f, fin, V(0, 1f, 0.3f), 0.25f);
        });
        int head = b.Head(V(0, 0.44f, 0.1f));
        var c = V(0, 0.53f, 0.15f);
        var r = V(0.1f, 0.09f, 0.1f);
        b.Ell(head, c, r, white);
        b.Spike(head, V(0, 0.6f, 0.13f), V(0, 0.69f, 0.09f), 0.025f, white);
        SealMuzzle(b, head, V(0, 0.49f, 0.24f), 0.82f, white, false);
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.048f * s, 0.555f), V(0.45f * s, 0.05f, 1f), 0.022f, Rgb(70, 50, 40)));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Grimer line

    /// <summary>
    /// Grimer's line and their Alolan forms: a heap of sludge with a gaping mouth, eyes on its top and arms of slime,
    /// streaked darker; the Alolan forms green, their mouths rimmed with yellow, and Alolan Muk's banded in bright
    /// colours and studded with crystals.
    /// </summary>
    private static PokeBuilder SludgeBuild(bool muk, bool alola)
    {
        string name = (muk ? "Muk" : "Grimer") + (alola ? "-Alola" : "");
        var b = new PokeBuilder(name, muk ? 0.86f : 0.66f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Scales };
        var slime = alola ? Rgb(108, 148, 98) : Rgb(162, 122, 182);
        var streak = alola ? Rgb(70, 104, 66) : Rgb(116, 84, 136);
        var inside = alola ? Rgb(64, 104, 186) : Rgb(84, 92, 120);
        float k = muk ? 1.35f : 1f;
        // A puddle spreading under it and a heap rising from it, its head leaning forward; Muk's a great low mound
        var bc = V(0, 0.08f, 0);
        var br = muk ? V(0.38f, 0.1f, 0.3f) : V(0.25f, 0.085f, 0.21f);
        b.Ell(Body, bc, br, slime);
        var hc = muk ? V(0, 0.26f, 0.02f) : V(0, 0.27f, 0.03f);
        var hr = muk ? V(0.26f, 0.21f, 0.21f) : V(0.14f, 0.18f, 0.13f);
        b.Ell(Body, hc, hr, slime, V(-8f, 0, 0));
        foreach (float a in new[] { 0.3f, 1.4f, 2.6f, 3.5f, 4.6f, 5.6f })
            b.Ell(Body, bc + V(MathF.Sin(a) * br.X * 0.9f, -0.02f, MathF.Cos(a) * br.Z * 0.9f), V(0.07f, 0.04f, 0.06f) * k, slime, blend: 0.03f);
        foreach (float f in new[] { -0.6f, -0.2f, 0.2f, 0.55f })
            b.PaintTorus(Body, V(0, hc.Y + hr.Y * f, hc.Z), hr.X * MathF.Sqrt(1f - f * f) * 1.02f, 0.012f * k, streak, V(6f, 0, 4f), sz: hr.Z / hr.X);
        if (muk && alola)
        {
            // Bold bands of colour round its foot and its heap
            b.PaintTorus(Body, V(0, 0.07f, 0), br.X * 0.93f, 0.04f, Rgb(250, 226, 60), sz: br.Z / br.X);
            b.PaintTorus(Body, V(0, 0.12f, 0), br.X * 0.76f, 0.035f, Rgb(52, 92, 204), sz: br.Z / br.X);
            b.PaintTorus(Body, V(0, hc.Y - hr.Y * 0.45f, hc.Z), hr.X * 0.9f, 0.04f, Rgb(232, 102, 160), V(6f, 0, 0), sz: hr.Z / hr.X);
        }
        // Its mouth gapes in the front of its heap
        var mouth = Out(hc, hr, V(-8f, 0, 0), V(0, -0.05f, 1f));
        Grin(b, Body, mouth, V(0.065f, 0.08f, 0.05f) * k, inside);
        if (alola) b.Torus(Body, mouth + V(0, 0, 0.005f), 0.066f * k, 0.012f * k, Rgb(250, 226, 60), V(90f, 0, 0), sz: 1.25f, mat: Shell);
        if (muk && alola)
            for (int i = 0; i < 7; i++)
            {
                float a = -1.2f + i * 0.4f;
                var at = mouth + V(MathF.Sin(a) * 0.085f, MathF.Cos(a) * 0.1f, 0.01f) * k;
                b.Spike(Body, at, at + V(MathF.Sin(a) * 0.04f, MathF.Cos(a) * 0.04f, 0.03f) * k, 0.016f * k, Rgb(236, 238, 248), mat: Shell, blend: 0.004f);
            }
        // Arms of slime reaching forward, blobs for hands
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.24f, 0.05f) * k);
            var hand = V((muk ? 0.34f : 0.22f) * s, (muk ? 0.2f : 0.18f), 0.2f) * k;
            b.Tube(arm, Smooth(3, V(0.1f * s, 0.26f, 0.05f) * k, V(0.18f * s, 0.24f, 0.12f) * k, hand), 0.045f * k, 0.035f * k, slime, blend: 0f);
            b.Ell(arm, hand, V(0.05f, 0.04f, 0.05f) * k, slime);
            for (int i = -1; i <= 1; i++)
                b.Ell(arm, hand + V(0.03f * i, -0.005f, 0.045f) * k, V(0.018f, 0.016f, 0.024f) * k, slime, blend: 0.012f);
            b.PaintTorus(arm, (hand + V(0.14f * s, 0.25f, 0.08f) * k) * 0.5f, 0.045f * k, 0.01f * k, streak, Euler(hand - V(0.14f * s, 0.25f, 0.08f) * k));
        });
        PokeBuilder.Both(s => b.Eye(Body, Out(hc, hr, V(-8f, 0, 0), V(0.32f * s, 0.75f, 0.6f)), V(0.32f * s, 0.75f, 0.6f), 0.026f * k, sclera: true, pupil: Black));
        return b;
    }

    private static PokeBuilder Grimer() => SludgeBuild(false, false);

    private static PokeBuilder Muk() => SludgeBuild(true, false);

    // ------------------------------------------------------------------ Shellder line

    private static PokeBuilder Shellder()
    {
        var b = new PokeBuilder("Shellder", 0.5f, BodyPlan.Floating, V(0, 0.22f, 0)) { Coat = Shell }.Hover();
        var shell = Rgb(150, 140, 204);
        var ridge = Rgb(116, 104, 172);
        var pearl = Rgb(38, 34, 52);
        // A dark pearl of a body between the two halves of its shell, its long tongue lolling out
        b.Ell(Body, V(0, 0.22f, 0.02f), V(0.15f, 0.12f, 0.14f), pearl, mat: Shell);
        b.Ell(Body, V(0, 0.34f, -0.04f), V(0.22f, 0.1f, 0.21f), shell, V(-14f, 0, 0));
        b.Ell(Body, V(0, 0.11f, -0.03f), V(0.2f, 0.075f, 0.19f), shell, V(8f, 0, 0));
        foreach (float x in new[] { -0.1f, -0.035f, 0.035f, 0.1f })
            b.PaintEll(Body, V(x, 0.42f, -0.04f), V(0.007f, 0.06f, 0.2f), ridge, V(-14f, 0, 0), 0.008f);
        PokeBuilder.Both(s =>
        {
            b.Spike(Body, V(0.18f * s, 0.27f, -0.06f), V(0.3f * s, 0.32f, -0.06f), 0.035f, shell);
            b.Spike(Body, V(0.15f * s, 0.32f, -0.15f), V(0.25f * s, 0.44f, -0.18f), 0.032f, shell);
        });
        b.Ell(Body, V(0, 0.13f, 0.2f), V(0.05f, 0.018f, 0.09f), Rgb(232, 150, 168), V(35f, 0, 0), Shell, 0.012f);
        PokeBuilder.Both(s => b.Eye(Body, On(V(0, 0.22f, 0.02f), V(0.15f, 0.12f, 0.14f), 0.055f * s, 0.24f), V(0.4f * s, 0.05f, 1f), 0.032f, sclera: true, pupil: Black));
        return Lift(b);
    }

    private static PokeBuilder Cloyster()
    {
        var b = new PokeBuilder("Cloyster", 0.86f, BodyPlan.Floating, V(0, 0.42f, 0)) { Coat = Shell }.Hover();
        var shell = Rgb(142, 132, 180);
        var rim = Rgb(196, 200, 224);
        var pearl = Rgb(44, 40, 74);
        // A great shell bristling with spikes, open at the front
        var c = V(0, 0.42f, -0.04f);
        var r = V(0.3f, 0.35f, 0.26f);
        b.Ell(Body, c, r, shell);
        foreach (var d in new[] { V(0.95f, 0.75f, -0.2f), V(-0.95f, 0.75f, -0.2f), V(1f, -0.05f, -0.1f), V(-1f, 0.1f, -0.1f), V(0.7f, -0.75f, -0.1f), V(-0.65f, -0.8f, -0.15f), V(0.15f, 1f, -0.5f), V(0.1f, -1f, -0.4f), V(0.3f, 0.3f, -1f), V(-0.4f, -0.2f, -1f) })
        {
            var root = Out(c, r, default, d);
            var way = Vector3.Normalize(d);
            float length = 0.12f + 0.06f * MathF.Abs(d.Y);
            b.Spike(Body, root - way * 0.04f, root + way * length, 0.055f, shell);
        }
        b.Cut(Body, V(0, 0.42f, 0.22f), V(0.2f, 0.27f, 0.18f));
        b.Torus(Body, V(0, 0.42f, 0.17f), 0.2f, 0.028f, rim, V(90f, 0, 0), sz: 1.32f, mat: Shell);
        // Inside, its dark pearl of a body, a horn on top, a wide grin and narrowed eyes
        b.Ell(Body, V(0, 0.42f, 0.04f), V(0.17f, 0.22f, 0.15f), pearl, mat: Shell);
        b.Spike(Body, V(-0.01f, 0.58f, 0.09f), V(-0.05f, 0.67f, 0.25f), 0.03f, Rgb(170, 178, 200), mat: Metal);
        var face = On(V(0, 0.42f, 0.04f), V(0.17f, 0.22f, 0.15f), 0, 0.35f);
        b.Ell(Body, face - V(0, 0, 0.006f), V(0.075f, 0.024f, 0.018f), White, mat: Shell, blend: 0.006f);
        b.Mark(Body, face + V(0, 0, 0.012f), V(0, 0, 1f), 0.07f, 0.012f, pearl, MarkShape.Zigzag);
        PokeBuilder.Both(s => b.Eye(Body, On(V(0, 0.42f, 0.04f), V(0.17f, 0.22f, 0.15f), 0.055f * s, 0.44f), V(0.4f * s, 0.05f, 1f), 0.032f, sclera: true, pupil: Black, glare: true));
        return Lift(b);
    }

    // ------------------------------------------------------------------ Drowzee line

    private static PokeBuilder Drowzee()
    {
        var b = new PokeBuilder("Drowzee", 0.8f, BodyPlan.Biped, V(0, 0.38f, 0)) { Coat = Fur };
        var yellow = Rgb(246, 210, 82);
        var brown = Rgb(88, 80, 76);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.09f * s, 0.2f, 0));
            b.Limb(leg, V(0.09f * s, 0.22f, 0), V(0.1f * s, 0.06f, 0.02f), 0.07f, 0.055f, brown);
            b.Ell(leg, V(0.105f * s, 0.03f, 0.05f), V(0.055f, 0.03f, 0.065f), yellow);
        });
        // A pot belly, dark below the waist as if in trousers, their edge scalloped
        b.Ell(Body, V(0, 0.38f, 0), V(0.18f, 0.22f, 0.15f), yellow);
        b.PaintEll(Body, V(0, 0.25f, 0), V(0.24f, 0.14f, 0.22f), brown);
        for (int i = 0; i < 9; i++)
        {
            float a = i * MathF.Tau / 9f;
            b.PaintEll(Body, V(MathF.Sin(a) * 0.175f, 0.35f, MathF.Cos(a) * 0.145f), V(0.045f, 0.034f, 0.045f), brown);
        }
        // Arms held out, one raised, as if it were putting someone to sleep
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.16f * s, 0.5f, 0.02f));
            var shoulder = V(0.15f * s, 0.5f, 0.02f);
            var hand = s > 0 ? V(0.3f, 0.52f, 0.12f) : V(-0.24f, 0.7f, 0.04f);
            b.Limb(arm, shoulder, hand, 0.045f, 0.036f, yellow);
            b.Ell(arm, hand, V(0.038f, 0.04f, 0.035f), yellow);
            Digits(b, arm, hand, hand - shoulder, V(0, 0.5f, 0), 0.04f, 0.013f, yellow);
        });
        // Its head, with a trunk of a nose drooping from it, eyes shut and mouth open
        int head = b.Head(V(0, 0.56f, 0.02f));
        var c = V(0, 0.68f, 0.04f);
        var r = V(0.13f, 0.11f, 0.11f);
        b.Ell(head, c, r, yellow);
        b.Tube(head, Smooth(3, V(0, 0.69f, 0.12f), V(0, 0.68f, 0.21f), V(0, 0.63f, 0.25f)), 0.045f, 0.035f, yellow, blend: 0f);
        foreach (float y in new[] { 0.745f, 0.775f })
            b.PaintEll(head, V(0, y, 0.07f), V(0.1f, 0.008f, 0.1f), brown, soft: 0.008f);
        Grin(b, head, V(0, 0.61f, 0.12f), V(0.05f, 0.03f, 0.03f), Rgb(180, 80, 90));
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.055f * s, 0.7f), V(0.4f * s, 0.05f, 1f), 0.025f, closed: true));
        return b;
    }

    private static PokeBuilder Hypno()
    {
        var b = new PokeBuilder("Hypno", 0.92f, BodyPlan.Biped, V(0, 0.5f, 0)) { Coat = Fur };
        var yellow = Rgb(250, 214, 82);
        var ruff = Rgb(246, 246, 240);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.08f * s, 0.36f, 0));
            var knee = V(0.15f * s, 0.2f, 0.04f);
            b.Limb(leg, V(0.08f * s, 0.37f, 0), knee, 0.06f, 0.045f, yellow);
            b.Limb(leg, knee, V(0.13f * s, 0.05f, 0.02f), 0.045f, 0.035f, yellow);
            b.Ell(leg, V(0.135f * s, 0.028f, 0.05f), V(0.045f, 0.028f, 0.07f), yellow);
        });
        b.Ell(Body, V(0, 0.5f, 0), V(0.12f, 0.16f, 0.1f), yellow);
        // A collar of white fluff round its neck
        var rc = V(0, 0.64f, 0);
        var rr = V(0.15f, 0.06f, 0.12f);
        b.Ell(Body, rc, rr, ruff);
        FurTufts(b, Body, rc, rr, 26, 0.04f, 0.022f, ruff, 2f, -0.9f, 0.8f);
        // It swings a pendulum on a string from its hand
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.13f * s, 0.6f, 0));
            var elbow = V(0.23f * s, 0.6f, 0.02f);
            var hand = s > 0 ? V(0.27f, 0.74f, 0.06f) : V(-0.3f, 0.66f, 0.08f);
            b.Limb(arm, V(0.12f * s, 0.6f, 0), elbow, 0.04f, 0.034f, yellow);
            b.Limb(arm, elbow, hand, 0.034f, 0.03f, yellow);
            b.Ell(arm, hand, V(0.04f, 0.04f, 0.04f), yellow);
            if (s > 0)
            {
                b.Tube(arm, new[] { hand - V(0, 0.03f, -0.01f), hand - V(0, 0.14f, -0.02f) }, 0.008f, 0.008f, Rgb(200, 200, 210), blend: 0f);
                b.Torus(arm, hand - V(0, 0.17f, -0.02f), 0.03f, 0.011f, Rgb(200, 204, 216), V(90f, 0, 0), mat: Metal);
            }
        });
        int head = b.Head(V(0, 0.68f, 0.01f));
        var c = V(0, 0.78f, 0.02f);
        var r = V(0.1f, 0.1f, 0.1f);
        b.Ell(head, c, r, yellow);
        b.Limb(head, V(0, 0.77f, 0.08f), V(0, 0.73f, 0.19f), 0.045f, 0.03f, yellow);
        PokeBuilder.Both(s =>
        {
            int e = b.Ear(head, s, V(0.06f * s, 0.86f, 0));
            CatEar(b, e, V(0.06f * s, 0.85f, 0), V(0.11f * s, 0.99f, -0.02f), 0.045f, yellow, Rgb(176, 120, 70));
        });
        PokeBuilder.Both(s => b.Eye(head, On(c, r, 0.045f * s, 0.8f), V(0.45f * s, 0.05f, 1f), 0.022f, closed: true));
        return b;
    }

    // ------------------------------------------------------------------ Krabby line

    /// <summary>
    /// A crab's claw from <paramref name="wrist"/> along <paramref name="dir"/>: a swollen hand, and two fingers bowed
    /// apart round an open gap and closing at their tips, the gap turned toward <paramref name="inward"/>. Gives where
    /// the fingers start and the way across the gap.
    /// </summary>
    private static (Vector3 root, Vector3 across) CrabClaw(PokeBuilder b, int bone, Vector3 wrist, Vector3 dir, Vector3 inward, float size, Color color)
    {
        var d = Vector3.Normalize(dir);
        var w = Vector3.Normalize(inward - d * Vector3.Dot(inward, d));
        var hand = wrist + d * size * 0.8f;
        Frond(b, bone, wrist, hand + d * size * 0.75f, size * 0.6f, color, Vector3.Cross(d, w), 0.75f);
        var root = hand + d * size * 0.55f;
        b.Tube(bone, Smooth(3, root - w * size * 0.28f, root + d * size * 0.5f - w * size * 0.38f, root + d * size * 1.05f - w * size * 0.04f), size * 0.27f, size * 0.07f, color, blend: 0f);
        b.Tube(bone, Smooth(3, root + w * size * 0.28f, root + d * size * 0.45f + w * size * 0.34f, root + d * size * 0.9f + w * size * 0.06f), size * 0.24f, size * 0.07f, color, blend: 0f);
        return (root, w);
    }

    /// <summary>
    /// A crab of Krabby's line: an orange shell pale beneath, eyes on top, three thin legs a side, two pincers raised
    /// (sized <paramref name="left"/> and <paramref name="right"/>) and <paramref name="crown"/> spikes on its head.
    /// </summary>
    private static PokeBuilder CrabBuild(string name, float fill, float left, float right, int crown, Color shell, Color belly)
    {
        var b = new PokeBuilder(name, fill, BodyPlan.Quadruped, V(0, 0.17f, 0)) { Coat = Shell };
        var leg = Rgb(214, 210, 204);
        PokeBuilder.Both(s =>
        {
            int l = b.Leg(s, V(0.1f * s, 0.14f, 0));
            foreach (float z in new[] { 0.05f, -0.02f, -0.09f })
                b.Tube(l, new[] { V(0.1f * s, 0.14f, z), V(0.22f * s, 0.15f, z - 0.01f), V(0.27f * s, 0.012f, z - 0.02f) }, 0.016f, 0.011f, leg, blend: 0f);
        });
        var c = V(0, 0.17f, 0);
        var r = V(0.16f, 0.09f, 0.12f);
        b.Ell(Body, c, r, shell);
        b.PaintEll(Body, c - V(0, 0.05f, -0.02f), V(0.18f, 0.06f, 0.14f), belly);
        var bump = V(0.04f, 0.035f, 0.035f);
        PokeBuilder.Both(s => b.Ell(Body, V(0.055f * s, 0.24f, 0.06f), bump, shell, blend: 0.02f));
        for (int i = 0; i < crown; i++)
        {
            float x = -0.06f + 0.12f * i / (crown - 1);
            b.Spike(Body, V(x, 0.235f, -0.01f), V(x * 1.6f, 0.33f - MathF.Abs(x) * 0.6f, -0.03f), 0.018f, shell);
        }
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.14f * s, 0.18f, 0.04f));
            var elbow = V(0.22f * s, 0.24f, 0.08f);
            b.Limb(arm, V(0.13f * s, 0.18f, 0.04f), elbow, 0.024f, 0.022f, leg);
            CrabClaw(b, arm, elbow, V(0.15f * s, 1f, 0.35f), V(-s, 0, 0), s < 0 ? left : right, shell);
        });
        PokeBuilder.Both(s => b.Eye(Body, On(V(0.055f * s, 0.24f, 0.06f), bump, 0.058f * s, 0.245f), V(0.3f * s, 0.2f, 1f), 0.024f, sclera: true, pupil: Black));
        return b;
    }

    private static PokeBuilder Krabby() => CrabBuild("Krabby", 0.58f, 0.075f, 0.075f, 2, Rgb(232, 112, 60), Rgb(242, 234, 222));

    private static PokeBuilder Kingler() => CrabBuild("Kingler", 0.8f, 0.07f, 0.13f, 5, Rgb(228, 108, 58), Rgb(242, 234, 222));
}
