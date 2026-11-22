using System;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Carpathia, batch 3 (plan 46 · CP4 to CP8): Panusel to Cerbanor (20 species), sculpted from nothing after the briefs of
// docs/plans/decisions/CP-dex-1b-plateau-more.md and CP-dex-2-mountains.md and the user's reference pictures. Each line shares
// its palette and motifs and grows in size and detail. Every helper of this file is named C3… so no other batch's clashes.
internal static partial class PokemonModels
{
    private static string[] CarpathiaNames3() => new[]
    {
        "Panusel", "Panusa", "Melcut", "Melcobor", "Cartitel", "Cartiton", "Pupazel", "Pupazoi", "Noptila", "Fluturnopt",
        "Nevastel", "Nevastuica", "Urslet", "Ursabrun", "Lupling", "Lupodac", "Caprita", "Capraneagra", "Cerbel", "Cerbanor"
    };

    private static PokeBuilder? CreateCarpathia3(string species) => species switch
    {
        "PANUSEL" => C3HuskDoll(false),
        "PANUSA" => C3HuskDoll(true),
        "MELCUT" => C3Melcut(),
        "MELCOBOR" => C3Melcobor(),
        "CARTITEL" => C3Cartitel(),
        "CARTITON" => C3Cartiton(),
        "PUPAZEL" => C3Pupazel(),
        "PUPAZOI" => C3Pupazoi(),
        "NOPTILA" => C3Noptila(),
        "FLUTURNOPT" => C3Fluturnopt(),
        "NEVASTEL" => C3Weasel(false),
        "NEVASTUICA" => C3Weasel(true),
        "URSLET" => C3Urslet(),
        "URSABRUN" => C3Ursabrun(),
        "LUPLING" => C3Lupling(),
        "LUPODAC" => C3Lupodac(),
        "CAPRITA" => C3Caprita(),
        "CAPRANEAGRA" => C3Capraneagra(),
        "CERBEL" => C3Cerbel(),
        "CERBANOR" => C3Cerbanor(),
        _ => null
    };

    // ------------------------------------------------------------------ helpers of this batch

    /// <summary>A heap of earth over the top of an ellipsoid: lumps spread by the golden angle over its upper half, in two shades.</summary>
    private static void C3Mound(PokeBuilder b, int bone, Vector3 center, Vector3 radii, int count, float size, Color color, Color light)
    {
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < count; i++)
        {
            float u = 0.15f + 0.8f * (i + 0.5f) / count, ring = MathF.Sqrt(1f - u * u), a = i * golden;
            var at = center + V(MathF.Sin(a) * ring * radii.X, u * radii.Y, MathF.Cos(a) * ring * radii.Z);
            float k = 0.8f + 0.35f * ((i * 7) % 5) / 4f;
            b.Ell(bone, at, V(size, size * 0.8f, size) * k, i % 3 == 0 ? light : color, blend: 0.02f);
        }
    }

    /// <summary>
    /// A ring of spiral bands painted on the flat side <paramref name="s"/> of a shell standing on its edge (an ellipsoid
    /// whose y and z radii are both <paramref name="r"/> and whose x radius is <paramref name="depth"/>): each band a ring on
    /// the surface at its own radius, drawn a little toward the coil's middle as it shrinks, as a whorl does.
    /// </summary>
    private static void C3Whorl(PokeBuilder b, int bone, Vector3 center, float depth, float r, float s, int bands, float tube, Color color, bool raised)
    {
        for (int k = 0; k < bands; k++)
        {
            float ring = r * (0.9f - 0.78f * k / bands);
            float x = depth * MathF.Sqrt(MathF.Max(0f, 1f - ring * ring / (r * r)));
            var at = center + V(x * s, 0.06f * r * k / bands, 0.1f * r * k / bands);
            if (raised) b.Torus(bone, at, ring, tube, color, V(0, 0, 90f), mat: Shell, blend: 0.008f);
            else b.PaintTorus(bone, at, ring, tube, color, V(0, 0, 90f));
        }
    }

    /// <summary>An ungulate's leg on side <paramref name="s"/>: thigh, shank and a hoof, the shank and hoof in colours of their own.</summary>
    private static int C3HoofLeg(PokeBuilder b, float s, float x, float hip, float z, bool front, float thick, Color upper, Color lower, Color hoof)
    {
        int leg = b.Leg(s, V(x * s, hip, z), front);
        var knee = V(x * 1.04f * s, hip * 0.5f, z + (front ? 0.01f : -0.03f) * hip / 0.3f);
        var foot = V(x * 1.06f * s, thick * 1.1f, z + 0.01f);
        b.Limb(leg, V(x * s, hip + thick * 0.4f, z), knee, thick * 1.5f, thick, upper);
        b.Limb(leg, knee, foot, thick, thick * 0.85f, lower);
        b.Ell(leg, V(foot.X, thick * 0.9f, foot.Z + thick * 0.4f), V(thick * 1.05f, thick * 0.9f, thick * 1.25f), hoof, mat: Shell, blend: 0.008f);
        return leg;
    }

    /// <summary>The point on the middle of the shank of a <see cref="C3HoofLeg"/> at height <paramref name="y"/> (under its knee).</summary>
    private static Vector3 C3ShankAt(float s, float x, float hip, float z, bool front, float thick, float y)
    {
        var knee = V(x * 1.04f * s, hip * 0.5f, z + (front ? 0.01f : -0.03f) * hip / 0.3f);
        var foot = V(x * 1.06f * s, thick * 1.1f, z + 0.01f);
        return Vector3.Lerp(foot, knee, Math.Clamp((y - foot.Y) / (knee.Y - foot.Y), 0f, 1f));
    }

    /// <summary>A crescent of pale fur on a bear's chest: a pale oval with the coat's colour painted over its upper half.</summary>
    private static void C3Crescent(PokeBuilder b, int bone, Vector3 at, float w, float h, Color pale, Color coat)
    {
        b.PaintEll(bone, at, V(w, h, w * 0.7f), pale);
        b.PaintEll(bone, at + V(0, h * 0.55f, h * 0.1f), V(w * 0.92f, h * 0.85f, w * 0.75f), coat);
    }

    // ------------------------------------------------------------------ Panusel line

    /// <summary>
    /// Panusel and Panusa: dolls of dried corn husk, tied at the neck and waist with coarse cord, a skirt of husk blades
    /// and a head of golden corn silk. Panusel is a child's harvest doll with two dark seeds for eyes; Panusa a tall husk
    /// maiden who walks the stubble at dusk, hooded and faceless but for two glowing amber seeds, her layered skirt
    /// ragged, long husk fingers and corn silk streaming behind her as if in a wind.
    /// </summary>
    private static PokeBuilder C3HuskDoll(bool maiden)
    {
        float k = maiden ? 2f : 1f;
        var b = new PokeBuilder(maiden ? "Panusa" : "Panusel", maiden ? 0.95f : 0.5f, BodyPlan.Biped, V(0, 0.3f * k, 0)) { Coat = Leaf };
        if (maiden) b.Hover();
        var husk = Rgb(228, 210, 160);
        var fold = Rgb(196, 172, 118);
        var green = Rgb(190, 196, 132);
        var cord = Rgb(132, 98, 62);
        var silk = maiden ? Rgb(238, 150, 52) : Rgb(238, 190, 70);
        var silkDeep = maiden ? Rgb(214, 112, 40) : Rgb(212, 152, 46);

        // Thin husk legs under the skirt
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s * k, 0.13f * k, 0));
            var foot = V(0.042f * s * k, 0.02f * k, 0.01f * k);
            b.Limb(leg, V(0.035f * s * k, 0.13f * k, 0), foot, 0.02f * k * (maiden ? 0.6f : 1f), 0.018f * k * (maiden ? 0.6f : 1f), fold);
            if (maiden)
                b.Spike(leg, foot, foot + V(0.01f * s, -0.016f * k, 0.05f * k), 0.012f * k, fold, 0.5f);
            else
                b.Ell(leg, foot + V(0, -0.002f, 0.01f), V(0.022f, 0.018f, 0.03f), fold);
        });

        // The skirt: a core of husk under a ring of blades flaring from the waist, a second shorter ring over it on the maiden
        b.Ell(Body, V(0, 0.18f * k, 0), V(0.085f, 0.075f, 0.08f) * k, husk);
        int blades = maiden ? 15 : 12;
        for (int i = 0; i < blades; i++)
        {
            float a = i * MathF.Tau / blades + 0.13f;
            var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
            float drop = (i % 3) * 0.02f * k;
            var root = V(0, 0.25f * k, 0) + dir * 0.05f * k;
            var tip = V(0, 0.055f * k + drop, 0) + dir * 0.16f * k;
            Blade(b, Body, root, tip, 0.045f * k, i % 4 == 1 ? green : i % 4 == 3 ? fold : husk, dir, 0.18f, Leaf);
        }
        if (maiden)
            for (int i = 0; i < 10; i++)
            {
                float a = i * MathF.Tau / 10f + 0.4f;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                Blade(b, Body, V(0, 0.255f * k, 0) + dir * 0.05f * k, V(0, 0.13f * k + (i % 2) * 0.02f * k, 0) + dir * 0.13f * k, 0.04f * k,
                    i % 3 == 0 ? green : i % 3 == 1 ? husk : fold, dir, 0.18f, Leaf);
            }

        // The bodice, tied at the waist
        b.Ell(Body, V(0, 0.3f * k, 0), V(0.06f, 0.065f, 0.05f) * k, husk);
        PokeBuilder.Both(s => b.PaintEll(Body, V(0.022f * s * k, 0.3f * k, 0.04f * k), V(0.004f * k, 0.07f * k, 0.03f * k), fold, soft: 0.006f));
        b.Torus(Body, V(0, 0.252f * k, 0), 0.056f * k, 0.011f * k, cord, mat: Fur);
        if (maiden)
        {
            b.Torus(Body, V(0, 0.27f * k, 0), 0.057f * k, 0.009f * k, cord, mat: Fur);
            // Husk bands crossed over the breast, and frayed husk at the shoulders
            b.PaintEll(Body, V(0, 0.32f * k, 0.045f * k), V(0.007f * k, 0.07f * k, 0.03f * k), fold, V(0, 0, 32f), 0.006f);
            b.PaintEll(Body, V(0, 0.32f * k, 0.045f * k), V(0.007f * k, 0.07f * k, 0.03f * k), fold, V(0, 0, -32f), 0.006f);
            PokeBuilder.Both(s => b.Spike(Body, V(0.05f * s, 0.35f, 0) * k, V(0.085f * s, 0.37f, -0.01f) * k, 0.02f * k, fold, 0.4f));
        }
        else
            b.Spike(Body, V(0.05f, 0.255f, 0.02f) * k, V(0.085f, 0.24f, 0.03f) * k, 0.008f * k, cord);

        // Arms of rolled husk, tied at the wrist; the doll's ends flare out, the maiden's sleeves hang in rags over long fingers
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.05f * s, 0.335f, 0) * k;
            int arm = b.Arm(s, shoulder);
            if (maiden)
            {
                var elbow = V(0.09f * s, 0.285f, 0.015f) * k;
                var wrist = V(0.12f * s, 0.225f, 0.03f) * k;
                b.Limb(arm, shoulder, elbow, 0.016f * k, 0.013f * k, husk);
                b.Limb(arm, elbow, wrist, 0.013f * k, 0.01f * k, husk);
                for (int j = 0; j < 3; j++)
                    Blade(b, arm, Vector3.Lerp(elbow, wrist, 0.2f + 0.2f * j) + V(0.004f * s, 0, 0), Vector3.Lerp(elbow, wrist, 0.45f + 0.2f * j) + V(0.022f * s, -0.04f, -0.01f + 0.01f * j) * k,
                        0.018f * k, j == 1 ? fold : husk, V(s, 0, 0.3f), 0.2f, Leaf);
                b.Torus(arm, Vector3.Lerp(elbow, wrist, 0.85f), 0.011f * k, 0.004f * k, cord, V(0, 0, 90f - 40f * s), mat: Fur);
                b.Ell(arm, wrist + V(0.003f * s, -0.008f, 0) * k, V(0.011f, 0.014f, 0.01f) * k, husk);
                Digits(b, arm, wrist + V(0.004f * s, -0.014f, 0) * k, V(0.2f * s, -1f, 0.1f), V(0.35f * s, 0, 0.25f), 0.04f * k, 0.0045f * k, fold, Leaf);
            }
            else
            {
                var hand = V(0.125f * s, 0.255f, 0.04f) * k;
                b.Limb(arm, shoulder, hand, 0.02f * k, 0.016f * k, husk);
                var along = Vector3.Normalize(hand - shoulder);
                b.Ell(arm, hand - along * 0.012f, V(0.02f, 0.008f, 0.02f), cord, Euler(along));
                b.Ell(arm, hand + along * 0.012f, V(0.022f, 0.022f, 0.022f), husk, Euler(along));
            }
        });

        // The neck, tied with cord
        b.Limb(Body, V(0, 0.33f, 0) * k, V(0, 0.395f, 0) * k, 0.028f * k, 0.026f * k, husk);
        b.Torus(Body, V(0, 0.37f * k, 0), 0.028f * k, 0.01f * k, cord, mat: Fur);
        if (maiden) b.Torus(Body, V(0, 0.385f * k, 0), 0.027f * k, 0.008f * k, cord, mat: Fur);

        int head = b.Head(V(0, 0.38f * k, 0));
        var c = maiden ? V(0, 0.82f, 0.012f) : V(0, 0.46f, 0);
        var r = maiden ? V(0.056f, 0.074f, 0.056f) : V(0.08f, 0.088f, 0.074f);
        if (maiden)
        {
            // A hood of husk over a pale, faceless head, hair streaming out from under it to one side
            var hood = V(0, 0.835f, -0.012f);
            b.Ell(head, hood, V(0.074f, 0.09f, 0.07f), fold);
            b.Cut(head, hood + V(0, -0.012f, 0.062f), V(0.056f, 0.072f, 0.05f));
            b.Ell(head, c, r, Rgb(238, 230, 204));
            b.PaintEll(head, c + V(0, -0.01f, 0.05f), V(0.004f, 0.06f, 0.02f), Rgb(214, 202, 168), soft: 0.006f);
            for (int i = 0; i < 12; i++)
            {
                float t = i / 11f;
                var root = hood + V(-0.05f + 0.1f * t, 0.03f - 0.08f * t * t, -0.05f);
                float sweep = 0.16f + 0.08f * ((i * 5) % 3);
                var path = Smooth(3, root, root + V(0.05f, 0.02f, -0.05f), root + V(0.05f + sweep * 0.6f, -0.02f, -0.1f),
                    root + V(0.06f + sweep, -0.07f - 0.03f * (i % 3), -0.12f));
                b.Tube(head, path, 0.012f, 0.006f, i % 3 == 1 ? silkDeep : silk, Fur, 0f);
            }
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.022f * s, 0.828f);
                b.Eye(head, at, Outward(c, r, at), 0.01f, sclera: true, white: Rgb(255, 196, 70), pupil: Rgb(130, 54, 20));
            });
        }
        else
        {
            b.Ell(head, c, r, husk);
            foreach (float x in new[] { -0.052f, -0.016f, 0.016f, 0.052f })
                b.PaintEll(head, c + V(x, 0, 0), V(0.004f, 0.095f, 0.085f), fold, soft: 0.006f);
            // A tuft of corn silk on the crown, strands springing out all round but the face
            b.Ell(head, c + V(0, 0.075f, -0.01f), V(0.055f, 0.028f, 0.05f), silk);
            for (int i = 0; i < 14; i++)
            {
                float a = i * MathF.Tau / 14f + 0.2f;
                var dir = V(MathF.Sin(a), 0, MathF.Cos(a));
                if (dir.Z > 0.55f) continue;
                var root = c + V(dir.X * 0.045f, 0.075f, dir.Z * 0.04f - 0.01f);
                var path = Smooth(3, root, root + dir * 0.05f + V(0, 0.06f, 0), root + dir * 0.11f + V(0, 0.03f, 0), root + dir * 0.14f + V(0, -0.03f - 0.01f * (i % 3), 0));
                b.Tube(head, path, 0.009f, 0.005f, i % 3 == 0 ? silkDeep : silk, Fur, 0f);
            }
            PokeBuilder.Both(s =>
            {
                var at = On(c, r, 0.03f * s, 0.462f);
                b.Eye(head, at, Outward(c, r, at), 0.013f, Rgb(84, 46, 28));
            });
        }
        if (maiden) Lift(b);
        return b;
    }

    // ------------------------------------------------------------------ Melcut line

    private static PokeBuilder C3Melcut()
    {
        var b = new PokeBuilder("Melcut", 0.42f, BodyPlan.Floating, V(0, 0.07f, 0)) { Coat = Scales };
        var cream = Rgb(244, 226, 190);
        var yellow = Rgb(236, 230, 140);
        var brown = Rgb(150, 116, 76);

        // A soft cream foot, a body that rises into the head in front
        b.Ell(Body, V(0, 0.045f, -0.02f), V(0.065f, 0.045f, 0.17f), cream);
        b.Spike(Body, V(0, 0.035f, -0.14f), V(0, 0.016f, -0.235f), 0.034f, cream, 0.7f);
        b.Limb(Body, V(0, 0.06f, 0.08f), V(0, 0.13f, 0.13f), 0.05f, 0.044f, cream);
        int head = b.Head(V(0, 0.12f, 0.12f));
        var c = V(0, 0.15f, 0.15f);
        b.Ell(head, c, V(0.045f, 0.048f, 0.045f), cream);
        PokeBuilder.Both(s => b.Limb(head, c + V(0.02f * s, -0.02f, 0.035f), c + V(0.036f * s, -0.04f, 0.058f), 0.009f, 0.007f, cream));
        b.Mark(head, c + V(0, -0.018f, 0.043f), V(0, -0.3f, 1f), 0.014f, 0.006f, brown, MarkShape.Smile);
        // Two stalks lifting curious eyes
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.018f * s, 0.035f, -0.005f));
            b.Tube(ear, new[] { c + V(0.018f * s, 0.03f, -0.005f), c + V(0.028f * s, 0.08f, 0.0f), c + V(0.038f * s, 0.12f, 0.006f) }, 0.011f, 0.009f, cream, blend: 0f);
            var ball = c + V(0.04f * s, 0.135f, 0.008f);
            var br = V(0.021f, 0.021f, 0.02f);
            b.Ell(ear, ball, br, cream, blend: 0.008f);
            var look = V(0.25f * s, 0.1f, 1f);
            b.Eye(ear, Out(ball, br, default, look), look, 0.0095f, sclera: true, pupil: Black);
        });
        // The round shell, striped in a whorl of pale yellow and brown
        var sc = V(0, 0.155f, -0.045f);
        int shell = b.Part("shell", Body, sc, PokeRole.Body);
        b.Ell(shell, sc, V(0.062f, 0.11f, 0.11f), yellow, mat: Shell);
        PokeBuilder.Both(s => C3Whorl(b, shell, sc, 0.062f, 0.11f, s, 4, 0.009f, brown, false));
        PokeBuilder.Both(s => b.PaintEll(shell, sc + V(0.06f * s, 0.006f, 0.01f), V(0.01f, 0.012f, 0.012f), brown));
        return b;
    }

    private static PokeBuilder C3Melcobor()
    {
        var b = new PokeBuilder("Melcobor", 0.85f, BodyPlan.Floating, V(0, 0.18f, 0)) { Coat = Scales };
        var grey = Rgb(150, 148, 140);
        var dark = Rgb(116, 112, 106);
        var tan = Rgb(198, 170, 128);
        var stone = Rgb(176, 176, 168);
        var ridge = Rgb(140, 140, 134);

        b.Ell(Body, V(0, 0.085f, -0.02f), V(0.15f, 0.085f, 0.34f), grey);
        b.Spike(Body, V(0, 0.06f, -0.3f), V(0, 0.025f, -0.46f), 0.07f, grey, 0.6f);
        b.PaintEll(Body, V(0, 0.015f, 0), V(0.17f, 0.035f, 0.38f), tan);
        b.Limb(Body, V(0, 0.12f, 0.17f), V(0, 0.3f, 0.25f), 0.12f, 0.1f, grey);
        // A wrinkled tan throat
        b.PaintEll(Body, V(0, 0.19f, 0.31f), V(0.085f, 0.15f, 0.06f), tan);
        for (int i = 0; i < 4; i++)
            b.PaintEll(Body, V(0, 0.1f + 0.045f * i, 0.32f), V(0.09f, 0.006f, 0.07f), Rgb(170, 140, 104), V(-15f, 0, 0), 0.006f);
        int head = b.Head(V(0, 0.3f, 0.25f));
        var c = V(0, 0.35f, 0.29f);
        var r = V(0.095f, 0.075f, 0.085f);
        b.Ell(head, c, r, grey);
        foreach (var (x, len) in new[] { (-0.06f, 0.06f), (0.065f, 0.07f), (0.0f, 0.045f) })
            b.Limb(head, c + V(x, -0.03f, 0.065f), c + V(x * 1.25f, -0.03f - len, 0.085f), 0.017f, 0.013f, grey, blend: 0.012f);
        b.Mark(head, Out(c, r, default, V(0, -0.35f, 1f)), V(0, -0.35f, 1f), 0.035f, 0.012f, dark, MarkShape.Wave);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.035f * s, 0.06f, -0.01f));
            b.Tube(ear, new[] { c + V(0.035f * s, 0.055f, -0.01f), c + V(0.05f * s, 0.12f, 0.0f), c + V(0.07f * s, 0.18f, 0.01f) }, 0.019f, 0.015f, grey, blend: 0f);
            var ball = c + V(0.073f * s, 0.2f, 0.012f);
            var br = V(0.031f, 0.031f, 0.029f);
            b.Ell(ear, ball, br, grey, blend: 0.01f);
            var look = V(0.25f * s, 0.1f, 1f);
            b.Eye(ear, Out(ball, br, default, look), look, 0.013f, sclera: true, pupil: Rgb(70, 46, 34));
        });
        // The great shell of limestone: raised whorls, and ammonites pressed into it
        var sc = V(0, 0.4f, -0.1f);
        int shell = b.Part("shell", Body, sc, PokeRole.Body);
        b.Ell(shell, sc, V(0.16f, 0.27f, 0.27f), stone, mat: Shell);
        PokeBuilder.Both(s =>
        {
            C3Whorl(b, shell, sc, 0.16f, 0.27f, s, 4, 0.013f, ridge, true);
            b.Ell(shell, sc + V(0.15f * s, 0.016f, 0.027f), V(0.025f, 0.035f, 0.035f), ridge, mat: Shell, blend: 0.012f);
            foreach (var (y, z, size) in new[] { (0.17f, -0.12f, 0.035f), (-0.17f, -0.06f, 0.028f), (0.05f, -0.22f, 0.03f) })
            {
                var dir = V(0.9f * s, y / 0.27f, z / 0.27f);
                var at = Out(sc, V(0.16f, 0.27f, 0.27f), default, dir);
                var n = Outward(sc, V(0.16f, 0.27f, 0.27f), at);
                b.Mark(shell, at, n, size, size, ridge, MarkShape.Ring);
                b.Mark(shell, at, n, size * 0.5f, size * 0.5f, dark, MarkShape.Ring);
            }
        });
        var top = Out(sc, V(0.16f, 0.27f, 0.27f), default, V(0.25f, 1f, -0.3f));
        b.Mark(shell, top, Outward(sc, V(0.16f, 0.27f, 0.27f), top), 0.03f, 0.03f, ridge, MarkShape.Ring);
        return b;
    }

    // ------------------------------------------------------------------ Cartitel line

    private static PokeBuilder C3Cartitel()
    {
        var b = new PokeBuilder("Cartitel", 0.48f, BodyPlan.Biped, V(0, 0.17f, 0)) { Coat = Fur };
        var black = Rgb(52, 52, 62);
        var belly = Rgb(74, 74, 86);
        var pink = Rgb(236, 158, 158);
        var claw = Rgb(222, 138, 140);
        var earth = Rgb(120, 86, 56);
        var earthLight = Rgb(150, 112, 74);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.055f * s, 0.07f, 0));
            b.Limb(leg, V(0.055f * s, 0.08f, 0), V(0.06f * s, 0.035f, 0.01f), 0.035f, 0.03f, black);
            b.Ell(leg, V(0.062f * s, 0.018f, 0.035f), V(0.032f, 0.018f, 0.045f), pink);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.062f * s + 0.015f * i, 0.014f, 0.07f), V(0.062f * s + 0.02f * i, 0.008f, 0.088f), 0.008f, claw, mat: Shell, blend: 0.004f);
        });
        b.Ell(Body, V(0, 0.18f, 0), V(0.12f, 0.15f, 0.11f), black);
        b.PaintEll(Body, V(0, 0.16f, 0.08f), V(0.08f, 0.1f, 0.06f), belly);
        int tail = b.Tail(V(0, 0.1f, -0.1f));
        b.Spike(tail, V(0, 0.1f, -0.09f), V(0, 0.13f, -0.17f), 0.02f, pink);
        // Broad pink spades of hands, clawed
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.21f, 0.04f));
            b.Limb(arm, V(0.1f * s, 0.21f, 0.04f), V(0.135f * s, 0.15f, 0.08f), 0.035f, 0.03f, black);
            var palm = V(0.15f * s, 0.12f, 0.09f);
            b.Ell(arm, palm, V(0.045f, 0.05f, 0.02f), pink, V(-20f, 25f * s, 0));
            Digits(b, arm, palm + V(0.005f * s, -0.035f, 0.01f), V(0.15f * s, -1f, 0.3f), V(0.38f, 0, 0), 0.045f, 0.011f, claw, Shell);
        });
        int head = b.Head(V(0, 0.27f, 0.02f));
        var c = V(0, 0.32f, 0.03f);
        var r = V(0.09f, 0.08f, 0.085f);
        b.Ell(head, c, r, black);
        // A long pink snout poked up, sniffing
        b.Limb(head, c + V(0, -0.01f, 0.06f), c + V(0, 0.035f, 0.165f), 0.034f, 0.022f, pink, Shell);
        b.Ell(head, c + V(0, 0.04f, 0.172f), V(0.025f, 0.022f, 0.018f), pink, mat: Shell, blend: 0.01f);
        PokeBuilder.Both(s => b.Mark(head, c + V(0.009f * s, 0.044f, 0.188f), V(0, 0.3f, 1f), 0.005f, 0.006f, Rgb(150, 80, 84)));
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.045f * s, 0.335f);
            b.Eye(head, at, Outward(c, r, at), 0.012f, closed: true);
        });
        // A mound of fresh earth on its head like a hat
        var mound = c + V(0, 0.068f, -0.01f);
        b.Ell(head, mound, V(0.065f, 0.032f, 0.058f), earth);
        C3Mound(b, head, mound, V(0.06f, 0.03f, 0.055f), 9, 0.018f, earth, earthLight);
        return b;
    }

    private static PokeBuilder C3Cartiton()
    {
        var b = new PokeBuilder("Cartiton", 0.9f, BodyPlan.Biped, V(0, 0.42f, 0)) { Coat = Fur };
        var black = Rgb(50, 50, 60);
        var belly = Rgb(160, 158, 164);
        var pink = Rgb(230, 152, 152);
        var iron = Rgb(86, 84, 88);
        var scar = Rgb(128, 124, 124);
        var earth = Rgb(112, 80, 52);
        var earthLight = Rgb(144, 106, 70);
        var rock = Rgb(120, 118, 122);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.11f * s, 0.2f, 0));
            b.Limb(leg, V(0.11f * s, 0.22f, 0), V(0.12f * s, 0.07f, 0.02f), 0.075f, 0.065f, black);
            b.Ell(leg, V(0.122f * s, 0.03f, 0.07f), V(0.06f, 0.03f, 0.085f), pink);
            for (int i = -1; i <= 1; i++)
                b.Spike(leg, V(0.122f * s + 0.03f * i, 0.025f, 0.14f), V(0.122f * s + 0.036f * i, 0.012f, 0.17f), 0.014f, pink, mat: Shell, blend: 0.004f);
        });
        // A thickset body hunched forward, a grey belly
        b.Ell(Body, V(0, 0.42f, 0), V(0.22f, 0.26f, 0.19f), black);
        b.PaintEll(Body, V(0, 0.38f, 0.13f), V(0.15f, 0.17f, 0.1f), belly);
        int tail = b.Tail(V(0, 0.22f, -0.16f));
        b.Spike(tail, V(0, 0.22f, -0.15f), V(0, 0.28f, -0.3f), 0.035f, pink);
        // Spade claws of iron, shaped like ploughshares and scarred with use
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.19f * s, 0.52f, 0.06f));
            b.Limb(arm, V(0.19f * s, 0.52f, 0.06f), V(0.26f * s, 0.36f, 0.14f), 0.075f, 0.06f, black);
            var hand = V(0.27f * s, 0.3f, 0.17f);
            b.Ell(arm, hand, V(0.065f, 0.06f, 0.06f), pink);
            Blade(b, arm, hand + V(0.005f * s, -0.03f, 0.02f), hand + V(0.03f * s, -0.22f, 0.09f), 0.085f, iron, V(s, 0, 0.35f), 0.22f, Metal);
            b.PaintEll(arm, hand + V(0.022f * s, -0.15f, 0.07f), V(0.05f, 0.006f, 0.06f), scar, V(30f, 0, 20f * s), 0.005f);
            b.PaintEll(arm, hand + V(0.018f * s, -0.11f, 0.06f), V(0.04f, 0.005f, 0.05f), scar, V(30f, 0, -15f * s), 0.005f);
            for (int i = -1; i <= 1; i++)
                b.Spike(arm, hand + V(0.03f * i, -0.02f, 0.05f), hand + V(0.04f * i + 0.01f * s, -0.1f, 0.12f), 0.016f, iron, mat: Metal, blend: 0.006f);
        });
        int head = b.Head(V(0, 0.6f, 0.1f));
        var c = V(0, 0.64f, 0.15f);
        var r = V(0.12f, 0.1f, 0.11f);
        b.Ell(head, c, r, black);
        b.Limb(head, c + V(0, -0.02f, 0.09f), c + V(0, -0.04f, 0.22f), 0.05f, 0.034f, pink, Shell);
        // A soft amber glow on the tip of its nose, like a miner's lamp
        b.Ell(head, c + V(0, -0.04f, 0.235f), V(0.034f, 0.03f, 0.022f), Rgb(250, 182, 90), mat: Glow, blend: 0.01f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.06f * s, 0.668f);
            b.Eye(head, at, Outward(c, r, at), 0.014f, Rgb(60, 50, 50), glare: true);
        });
        // The mound of earth spread over head and shoulders, broken rock in it
        var mound = c + V(0, 0.075f, -0.05f);
        b.Ell(head, mound, V(0.13f, 0.05f, 0.12f), earth);
        C3Mound(b, head, mound, V(0.12f, 0.045f, 0.11f), 14, 0.03f, earth, earthLight);
        foreach (var (x, z, lean) in new[] { (-0.07f, -0.02f, -0.4f), (0.06f, -0.05f, 0.5f), (0.0f, -0.11f, 0.1f), (0.09f, 0.03f, 0.3f) })
            b.Spike(head, mound + V(x, 0.03f, z), mound + V(x + lean * 0.05f, 0.09f, z - 0.02f), 0.022f, rock, 0.6f, Shell, 0.008f);
        return b;
    }

    // ------------------------------------------------------------------ Pupazel line

    /// <summary>Barred black-and-white bands painted across a feather or wing from <paramref name="root"/> to <paramref name="tip"/>, at the fractions given.</summary>
    private static void C3Bars(PokeBuilder b, int bone, Vector3 root, Vector3 tip, float across, float thick, Color color, params float[] at)
    {
        var d = tip - root;
        foreach (float t in at)
            b.PaintEll(bone, root + d * t, V(across, thick, across), color, Euler(d), 0.006f);
    }

    private static PokeBuilder C3Pupazel()
    {
        var b = new PokeBuilder("Pupazel", 0.45f, BodyPlan.Bird, V(0, 0.2f, 0)) { Coat = Fur }.Hover();
        var cinnamon = Rgb(204, 128, 74);
        var cream = Rgb(238, 196, 150);
        var black = Rgb(44, 40, 42);
        var white = Rgb(244, 240, 232);
        var beak = Rgb(76, 66, 66);

        BirdLegs(b, 0.045f, 0.1f, 0.0f, 0.014f, Rgb(108, 100, 104));
        b.Ell(Body, V(0, 0.2f, 0), V(0.13f, 0.12f, 0.14f), cinnamon);
        b.PaintEll(Body, V(0, 0.18f, 0.09f), V(0.09f, 0.085f, 0.08f), cream);
        int tail = b.Tail(V(0, 0.18f, -0.12f));
        TailFan(b, tail, V(0, 0.19f, -0.11f), 3, 30f, 0.12f, 0.034f, black, black);
        b.PaintEll(tail, V(0, 0.16f, -0.18f), V(0.08f, 0.012f, 0.03f), white, V(-75f, 0, 0), 0.006f);
        // Folded wings barred black and white
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.105f * s, 0.24f, 0.03f);
            var tip = V(0.13f * s, 0.14f, -0.15f);
            int wing = FoldedWing(b, s, shoulder, tip, 0.05f, black, black);
            b.PaintEll(wing, Vector3.Lerp(shoulder, tip, 0.1f), V(0.05f, 0.05f, 0.07f), cinnamon, Euler(tip - shoulder));
            C3Bars(b, wing, shoulder, tip, 0.06f, 0.011f, white, 0.38f, 0.58f, 0.78f);
        });
        int head = b.Head(V(0, 0.29f, 0.05f));
        var c = V(0, 0.35f, 0.06f);
        var r = V(0.088f, 0.084f, 0.084f);
        b.Ell(head, c, r, cinnamon);
        // A short, slightly curved beak held open, begging
        b.Spike(head, c + V(0, 0.0f, 0.075f), c + V(0, -0.008f, 0.17f), 0.02f, beak, 0.75f, Shell, 0.008f);
        b.Spike(head, c + V(0, -0.022f, 0.07f), c + V(0, -0.05f, 0.145f), 0.015f, beak, 0.75f, Shell, 0.008f);
        b.Ell(head, c + V(0, -0.016f, 0.082f), V(0.016f, 0.01f, 0.014f), Rgb(222, 120, 120), blend: 0.004f);
        // A small fan of crest, tipped black over white
        for (int i = 0; i < 5; i++)
        {
            float a = (-55f + 25f * i) * Degree;
            var root = c + V(0, r.Y * 0.8f, -0.01f);
            var tip = root + V(0, MathF.Cos(a), MathF.Sin(a) - 0.25f) * 0.085f;
            Frond(b, head, root, tip, 0.02f, cinnamon, V(1f, 0, 0), 0.32f, Fur, 0.01f);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.78f), V(0.03f, 0.008f, 0.03f), white, Euler(tip - root), 0.004f);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.94f), V(0.03f, 0.012f, 0.03f), black, Euler(tip - root), 0.004f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.05f * s, 0.365f);
            b.Eye(head, at, V(0.6f * s, 0.05f, 1f), 0.022f, Rgb(84, 52, 34));
        });
        return b;
    }

    private static PokeBuilder C3Pupazoi()
    {
        var b = new PokeBuilder("Pupazoi", 0.85f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var cinnamon = Rgb(206, 120, 90);
        var cream = Rgb(240, 196, 160);
        var black = Rgb(42, 38, 40);
        var white = Rgb(244, 240, 232);
        var beak = Rgb(80, 72, 74);

        BirdLegs(b, 0.06f, 0.26f, 0.0f, 0.02f, Rgb(104, 98, 102));
        b.Ell(Body, V(0, 0.43f, 0), V(0.13f, 0.17f, 0.14f), cinnamon, V(-12f, 0, 0));
        b.PaintEll(Body, V(0, 0.44f, 0.09f), V(0.09f, 0.13f, 0.08f), cream);
        b.Limb(Body, V(0, 0.54f, 0.04f), V(0, 0.64f, 0.08f), 0.075f, 0.06f, cinnamon);
        // A long tail, black with a white band
        int tail = b.Tail(V(0, 0.36f, -0.12f));
        for (int i = 0; i < 4; i++)
        {
            float a = (-18f + 12f * i) * Degree;
            var root = V(0, 0.36f, -0.11f);
            var tip = root + Vector3.Normalize(V(MathF.Sin(a), -0.45f, -1f)) * 0.32f;
            Frond(b, tail, root, tip, 0.045f, black, V(0, 1f, 0), 0.26f, Fur, 0.01f);
            C3Bars(b, tail, root, tip, 0.06f, 0.025f, white, 0.55f);
        }
        // Wings half spread and raised, every feather barred
        PokeBuilder.Both(s =>
        {
            var root = V(0.11f * s, 0.55f, 0.0f);
            int wing = b.Wing(s, root);
            b.Ell(wing, root + V(0.07f * s, 0.02f, -0.02f), V(0.1f, 0.075f, 0.04f), cinnamon, V(0, 0, 35f * s));
            for (int i = 0; i < 6; i++)
            {
                float a = (80f - i * 15f) * Degree;
                float len = 0.25f + 0.07f * MathF.Sin(MathF.PI * i / 5f);
                var from = root + V(0.04f * s, 0.01f, -0.02f);
                var tip = from + V(MathF.Cos(a) * s, MathF.Sin(a), -0.3f) * len;
                Frond(b, wing, from, tip, 0.048f, black, V(0, 0.1f, 1f), 0.26f, Fur, 0.012f);
                C3Bars(b, wing, from, tip, 0.06f, 0.016f, white, 0.45f, 0.65f, 0.85f);
            }
        });
        int head = b.Head(V(0, 0.64f, 0.08f));
        var c = V(0, 0.71f, 0.11f);
        var r = V(0.082f, 0.078f, 0.084f);
        b.Ell(head, c, r, cinnamon);
        // A long curved beak, open and calling
        b.Tube(head, Smooth(3, c + V(0, -0.005f, 0.06f), c + V(0, -0.008f, 0.15f), c + V(0, -0.03f, 0.25f), c + V(0, -0.065f, 0.31f)), 0.019f, 0.008f, beak, Shell, 0f);
        b.Tube(head, Smooth(3, c + V(0, -0.03f, 0.055f), c + V(0, -0.05f, 0.13f), c + V(0, -0.085f, 0.2f)), 0.015f, 0.008f, beak, Shell, 0f);
        b.Ell(head, c + V(0, -0.025f, 0.075f), V(0.02f, 0.012f, 0.02f), Rgb(222, 120, 120), blend: 0.004f);
        // The tall crest fanned open like a crown, tipped white then black
        for (int i = 0; i < 9; i++)
        {
            float a = (-72f + 15f * i) * Degree;
            var root = c + V(0, r.Y * 0.75f, -0.015f);
            float len = 0.2f + 0.035f * MathF.Sin(MathF.PI * i / 8f);
            var tip = root + V(0, MathF.Cos(a), MathF.Sin(a)) * len;
            Frond(b, head, root, tip, 0.028f, cinnamon, V(1f, 0, 0), 0.3f, Fur, 0.01f);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.8f), V(0.04f, 0.012f, 0.04f), white, Euler(tip - root), 0.004f);
            b.PaintEll(head, Vector3.Lerp(root, tip, 0.95f), V(0.04f, 0.018f, 0.04f), black, Euler(tip - root), 0.004f);
        }
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.046f * s, 0.725f);
            b.Eye(head, at, V(0.6f * s, 0.05f, 1f), 0.019f, Rgb(70, 44, 30), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Noptila line

    private static PokeBuilder C3Noptila()
    {
        // Hunched in a curve, its face low at the front
        var path = new[] { V(0, 0.055f, -0.17f), V(0, 0.1f, -0.12f), V(0, 0.145f, -0.055f), V(0, 0.16f, 0.02f), V(0, 0.14f, 0.085f), V(0, 0.105f, 0.13f) };
        var b = new PokeBuilder("Noptila", 0.45f, BodyPlan.Serpent, path[0]) { Coat = Fur };
        var grey = Rgb(138, 130, 124);
        var under = Rgb(118, 110, 104);
        var bristle = Rgb(164, 156, 148);
        var pale = Rgb(236, 230, 210);
        var dark = Rgb(62, 58, 58);

        int neck = Grub(b, path, 0.058f, 0.066f, grey, under, (i, seg, at, r) =>
        {
            FurTufts(b, seg, at, V(r, r, r), 7, r * 0.45f, 0.0055f, bristle, 2f, -0.1f, 0f);
            if (i == 3)
                PokeBuilder.Both(s =>
                {
                    var spot = at + V(r * 0.78f * s, r * 0.5f, 0);
                    var n = V(0.8f * s, 0.6f, 0);
                    b.Mark(seg, spot, n, r * 0.4f, r * 0.4f, pale);
                    b.Mark(seg, spot, n, r * 0.18f, r * 0.18f, dark);
                });
            if (i >= 1 && i <= 4)
                PokeBuilder.Both(s => b.Ell(seg, at + V(r * 0.5f * s, -r * 0.8f, 0.01f), V(r * 0.25f, r * 0.3f, r * 0.25f), under, blend: 0.01f));
        });
        int tail = b.Tail(path[0]);
        b.Spike(tail, path[0] + V(0, 0, -0.03f), path[0] + V(0, -0.02f, -0.09f), 0.035f, grey, 0.8f);
        int head = b.Head(path[^1] + V(0, -0.01f, 0.03f), neck);
        var c = V(0, 0.075f, 0.18f);
        var r = V(0.058f, 0.054f, 0.052f);
        b.Ell(head, c, r, grey);
        b.PaintEll(head, c + V(0, -0.012f, 0.04f), V(0.035f, 0.03f, 0.025f), dark);
        FurTufts(b, head, c, r, 7, 0.022f, 0.0055f, bristle, 0.4f, 0f, 0f);
        PokeBuilder.Both(s =>
        {
            var look = V(0.45f * s, 0.0f, 1f);
            var at = Out(c + V(0, -0.012f, 0), r, default, look);
            b.Eye(head, at, look, 0.008f, Black);
        });
        return b;
    }

    private static PokeBuilder C3Fluturnopt()
    {
        var b = new PokeBuilder("Fluturnopt", 0.85f, BodyPlan.Bird, V(0, 0.42f, 0)) { Coat = Fur }.Hover();
        var grey = Rgb(132, 124, 120);
        var light = Rgb(170, 162, 156);
        var purple = Rgb(134, 112, 150);
        var rim = Rgb(98, 80, 112);
        var gold = Rgb(222, 196, 110);
        var pale = Rgb(232, 226, 206);
        var dark = Rgb(52, 48, 56);

        PokeBuilder.Both(s =>
        {
            int w = b.Wing(s, V(0.05f * s, 0.46f, -0.05f));
            // Forewings with great owl-eyes, rounder hindwings below
            var fore = V(0.24f * s, 0.6f, -0.07f);
            FlatWing(b, w, fore, 0.24f, 0.15f, 35f * s, purple, rim, 0.02f);
            b.Mark(w, fore + V(0.03f * s, 0.02f, 0.012f), V(0, 0, 1f), 0.065f, 0.05f, gold, MarkShape.Ring, 35f * s);
            b.Mark(w, fore + V(0.03f * s, 0.02f, 0.012f), V(0, 0, 1f), 0.035f, 0.028f, dark, MarkShape.Disc, 35f * s);
            var hind = V(0.18f * s, 0.36f, -0.08f);
            FlatWing(b, w, hind, 0.15f, 0.1f, -25f * s, purple, rim, 0.018f);
            b.Mark(w, hind + V(0.02f * s, -0.01f, 0.011f), V(0, 0, 1f), 0.045f, 0.035f, pale, MarkShape.Ring, -25f * s);
            b.Mark(w, hind + V(0.02f * s, -0.01f, 0.011f), V(0, 0, 1f), 0.022f, 0.018f, dark, MarkShape.Disc, -25f * s);
        });
        // A plush fuzzy body: a thorax with a pale collar, a banded abdomen
        b.Ell(Body, V(0, 0.43f, 0.0f), V(0.1f, 0.1f, 0.1f), grey);
        FurTufts(b, Body, V(0, 0.45f, 0.02f), V(0.095f, 0.09f, 0.09f), 12, 0.035f, 0.012f, light, 0.6f, -0.2f, 0.4f);
        b.Ell(Body, V(0, 0.31f, -0.09f), V(0.085f, 0.11f, 0.12f), grey, V(-35f, 0, 0));
        for (int i = 0; i < 3; i++)
            b.PaintEll(Body, V(0, 0.3f - 0.035f * i, -0.08f - 0.03f * i), V(0.1f, 0.008f, 0.1f), Rgb(110, 102, 98), V(-35f, 0, 0), 0.006f);
        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.05f * s, 0.32f, 0.02f));
            b.Limb(leg, V(0.05f * s, 0.32f, 0.02f), V(0.065f * s, 0.24f, 0.05f), 0.018f, 0.015f, grey);
            b.Spike(leg, V(0.065f * s, 0.24f, 0.05f), V(0.07f * s, 0.21f, 0.07f), 0.012f, dark, mat: Shell, blend: 0.004f);
        });
        // Little arms in front: the left one holds up a lantern it can't keep away from
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.07f * s, 0.43f, 0.07f));
            var hand = V(0.1f * s, 0.36f, 0.12f);
            b.Limb(arm, V(0.07f * s, 0.43f, 0.07f), hand, 0.02f, 0.017f, grey);
            b.Ell(arm, hand, V(0.02f, 0.02f, 0.02f), grey);
            if (s < 0)
            {
                var metal = Rgb(96, 84, 70);
                b.Torus(arm, hand + V(0, -0.022f, 0), 0.016f, 0.006f, metal, V(90f, 0, 0), mat: Metal);
                b.Ell(arm, hand + V(0, -0.045f, 0), V(0.024f, 0.011f, 0.024f), metal, mat: Metal, blend: 0.006f);
                b.Ell(arm, hand + V(0, -0.078f, 0), V(0.022f, 0.028f, 0.022f), Rgb(252, 214, 120), mat: Glow, blend: 0.006f);
                b.Ell(arm, hand + V(0, -0.108f, 0), V(0.026f, 0.01f, 0.026f), metal, mat: Metal, blend: 0.006f);
            }
            else
                b.Spike(arm, hand + V(0, -0.01f, 0.01f), hand + V(0.005f, -0.035f, 0.03f), 0.008f, dark, mat: Shell, blend: 0.004f);
        });
        int head = b.Head(V(0, 0.5f, 0.06f));
        var c = V(0, 0.55f, 0.09f);
        var r = V(0.08f, 0.074f, 0.074f);
        b.Ell(head, c, r, grey);
        b.PaintEll(head, c + V(0, -0.04f, 0.04f), V(0.06f, 0.03f, 0.04f), light);
        b.Ell(head, c + V(0, -0.025f, 0.072f), V(0.022f, 0.018f, 0.015f), dark, blend: 0.008f);
        PokeBuilder.Both(s =>
        {
            var at = On(c, r, 0.04f * s, 0.565f);
            b.Eye(head, at, Outward(c, r, at), 0.022f, sclera: true, pupil: dark);
        });
        // Broad feathery antennae curling forward
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.06f, 0.0f));
            var root = c + V(0.03f * s, 0.06f, 0.0f);
            var tip = c + V(0.09f * s, 0.21f, 0.07f);
            b.Tube(ear, Smooth(3, root, root + V(0.03f * s, 0.08f, 0.0f), tip), 0.008f, 0.006f, gold, Fur, 0f);
            Frond(b, ear, root + V(0.02f * s, 0.04f, 0.0f), tip + V(0, 0.01f, 0.01f), 0.032f, gold, V(0, 0.2f, 1f), 0.2f, Fur, 0.008f);
            for (int i = 0; i < 4; i++)
            {
                var at = Vector3.Lerp(root + V(0.02f * s, 0.05f, 0), tip, 0.15f + 0.2f * i);
                b.PaintEll(ear, at, V(0.04f, 0.006f, 0.04f), Rgb(184, 160, 90), Euler(tip - root), 0.004f);
            }
        });
        return Lift(b);
    }

    // ------------------------------------------------------------------ Nevastel line

    /// <summary>
    /// Nevastel and Nevastuica: slim weasels standing up on their hind legs, a warm brown back and a clean white throat
    /// and belly, round ears and a long tail darker at its tip. Nevastuica, "the little bride", is bigger and wears an
    /// embroidered veil edged in red and black and a wreath of small wildflowers, one paw raised shyly to its chest.
    /// </summary>
    private static PokeBuilder C3Weasel(bool bride)
    {
        float k = bride ? 1.45f : 1f;
        var b = new PokeBuilder(bride ? "Nevastuica" : "Nevastel", bride ? 0.72f : 0.5f, BodyPlan.Biped, V(0, 0.22f * k, 0)) { Coat = Fur };
        var brown = Rgb(176, 100, 54);
        var dark = Rgb(106, 62, 36);
        var white = Rgb(246, 238, 222);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.035f * s, 0.09f, 0) * k);
            b.Limb(leg, V(0.035f * s, 0.1f, 0) * k, V(0.04f * s, 0.025f, 0.015f) * k, 0.024f * k, 0.017f * k, brown);
            b.Ell(leg, V(0.04f * s, 0.014f, 0.035f) * k, V(0.018f, 0.014f, 0.032f) * k, dark);
        });
        var spine = Smooth(3, V(0, 0.09f, -0.01f) * k, V(0, 0.2f, 0.0f) * k, V(0, 0.31f, 0.02f) * k, V(0, 0.4f, 0.04f) * k);
        b.Tube(Body, spine, 0.066f * k, 0.038f * k, brown, blend: 0.02f);
        b.PaintEll(Body, V(0, 0.26f, 0.05f) * k, V(0.036f, 0.17f, 0.04f) * k, white);
        int tail = b.Tail(V(0, 0.1f, -0.05f) * k);
        var tp = Smooth(3, V(0, 0.1f, -0.05f) * k, V(0, 0.07f, -0.15f) * k, V(0, 0.13f, -0.25f) * k, V(0, 0.23f, -0.27f) * k, V(0, 0.3f, -0.21f) * k);
        b.Tube(tail, tp, 0.022f * k, 0.03f * k, brown, blend: 0.01f);
        b.Ell(tail, tp[^1], V(0.032f, 0.04f, 0.032f) * k, dark, blend: 0.012f);
        b.PaintEll(tail, tp[^3], V(0.04f, 0.035f, 0.04f) * k, dark);
        // Little forepaws held up at the chest, one raised to the chin on the bride
        PokeBuilder.Both(s =>
        {
            var shoulder = V(0.035f * s, 0.33f, 0.05f) * k;
            int arm = b.Arm(s, shoulder);
            var paw = bride && s > 0 ? V(0.028f, 0.385f, 0.095f) * k : V(0.042f * s, 0.27f, 0.085f) * k;
            b.Limb(arm, shoulder, paw, 0.014f * k, 0.013f * k, brown);
            b.Ell(arm, paw, V(0.016f, 0.016f, 0.016f) * k, dark);
        });
        int head = b.Head(V(0, 0.42f, 0.04f) * k);
        var c = V(0, 0.47f, 0.06f) * k;
        var r = V(0.052f, 0.048f, 0.055f) * k;
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.015f, 0.045f) * k, V(0.03f, 0.022f, 0.03f) * k, brown, blend: 0.015f);
        b.PaintEll(head, c + V(0, -0.03f, 0.035f) * k, V(0.05f, 0.022f, 0.06f) * k, white);
        b.Ell(head, c + V(0, -0.008f, 0.076f) * k, V(0.01f, 0.008f, 0.007f) * k, dark, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.037f * s, 0.035f, -0.01f) * k);
            b.Ell(ear, c + V(0.042f * s, 0.045f, -0.012f) * k, V(0.019f, 0.019f, 0.011f) * k, brown);
            b.PaintEll(ear, c + V(0.042f * s, 0.045f, 0.0f) * k, V(0.01f, 0.01f, 0.008f) * k, dark);
            var look = V(0.45f * s, 0.15f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.012f * k, Black);
        });
        if (bride)
        {
            // An embroidered veil falling from the crown behind the head, edged in red and black thread
            var veil = Rgb(246, 240, 222);
            var vc = c + V(0, -0.045f, -0.06f) * k;
            b.Ell(head, vc, V(0.07f, 0.11f, 0.015f) * k, veil, V(-12f, 0, 0), blend: 0.015f);
            PokeBuilder.Both(s => b.Ell(head, vc + V(0.05f * s, -0.02f, 0.03f) * k, V(0.012f, 0.1f, 0.035f) * k, veil, V(-12f, 25f * s, 0), blend: 0.012f));
            b.PaintEll(head, vc + V(0, -0.09f, 0.0f) * k, V(0.12f, 0.012f, 0.08f) * k, Rgb(196, 50, 46), V(-12f, 0, 0), 0.005f);
            b.PaintEll(head, vc + V(0, -0.072f, 0.0f) * k, V(0.12f, 0.005f, 0.08f) * k, Rgb(50, 40, 40), V(-12f, 0, 0), 0.004f);
            // A wreath of wildflowers round the crown
            var wc = c + V(0, 0.032f, -0.004f) * k;
            float wr = 0.048f * k;
            b.Torus(head, wc, wr, 0.008f * k, Rgb(90, 150, 70), V(-14f, 0, 0), mat: Leaf);
            var colors = new[] { Rgb(220, 60, 60), Rgb(246, 212, 70), Rgb(90, 140, 230), Rgb(244, 244, 236) };
            for (int i = 0; i < 9; i++)
            {
                float a = (i - 4) * 0.36f;
                var at = wc + V(MathF.Sin(a) * wr, MathF.Sin(-14f * Degree) * -MathF.Cos(a) * wr + 0.004f, MathF.Cos(a) * wr * MathF.Cos(14f * Degree));
                b.Ell(head, at, V(0.012f, 0.009f, 0.012f) * k, colors[i % 4], mat: Leaf, blend: 0.006f);
            }
        }
        return b;
    }

    // ------------------------------------------------------------------ Urslet line

    private static PokeBuilder C3Urslet()
    {
        var b = new PokeBuilder("Urslet", 0.55f, BodyPlan.Biped, V(0, 0.22f, 0)) { Coat = Fur };
        var brown = Rgb(140, 94, 62);
        var cream = Rgb(240, 216, 166);
        var honey = Rgb(234, 166, 52);
        var comb = Rgb(222, 168, 80);
        var dark = Rgb(98, 62, 40);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.065f * s, 0.12f, 0));
            b.Limb(leg, V(0.065f * s, 0.13f, 0), V(0.07f * s, 0.04f, 0.01f), 0.048f, 0.042f, brown);
            b.Ell(leg, V(0.07f * s, 0.025f, 0.035f), V(0.042f, 0.025f, 0.052f), brown);
        });
        b.Ell(Body, V(0, 0.22f, 0), V(0.12f, 0.13f, 0.1f), brown);
        b.Ell(Body, V(0, 0.14f, -0.1f), V(0.03f, 0.03f, 0.03f), brown);
        C3Crescent(b, Body, V(0, 0.3f, 0.085f), 0.055f, 0.028f, cream, brown);
        // A broken honeycomb hugged to its belly, honey dripping off it
        foreach (var at in new[] { V(-0.028f, 0.2f, 0.1f), V(0.028f, 0.2f, 0.1f), V(0f, 0.152f, 0.098f), V(0.0f, 0.248f, 0.094f) })
        {
            HexCell(b, Body, at, V(0, 0, 1f), 0.028f, 0.014f, comb);
            b.PaintEll(Body, at + V(0, 0, 0.014f), V(0.013f, 0.013f, 0.006f), Rgb(176, 110, 40), soft: 0.005f);
        }
        b.Limb(Body, V(0.01f, 0.14f, 0.11f), V(0.012f, 0.11f, 0.112f), 0.008f, 0.007f, honey, Glow, 0.006f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.1f * s, 0.27f, 0.03f));
            var hand = V(0.055f * s, 0.19f, 0.12f);
            b.Limb(arm, V(0.1f * s, 0.27f, 0.03f), hand, 0.036f, 0.03f, brown);
            b.Ell(arm, hand, V(0.03f, 0.03f, 0.028f), brown);
        });
        int head = b.Head(V(0, 0.32f, 0.02f));
        var c = V(0, 0.4f, 0.03f);
        var r = V(0.11f, 0.1f, 0.095f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.03f, 0.085f), V(0.048f, 0.038f, 0.034f), cream, blend: 0.02f);
        // Honey on its nose
        b.Ell(head, c + V(0, -0.012f, 0.122f), V(0.019f, 0.013f, 0.011f), honey, mat: Glow, blend: 0.004f);
        b.Limb(head, c + V(0.006f, -0.02f, 0.124f), c + V(0.008f, -0.04f, 0.12f), 0.006f, 0.005f, honey, Glow, 0.004f);
        b.Mark(head, c + V(0, -0.052f, 0.112f), V(0, -0.3f, 1f), 0.014f, 0.006f, dark, MarkShape.Smile);
        foreach (float x in new[] { -0.02f, 0f, 0.02f })
            b.Spike(head, c + V(x, r.Y * 0.9f, 0.0f), c + V(x * 1.5f, r.Y + 0.03f, -0.01f), 0.015f, brown, 0.5f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.075f * s, 0.07f, -0.01f));
            b.Ell(ear, c + V(0.082f * s, 0.083f, -0.01f), V(0.035f, 0.035f, 0.022f), brown);
            b.PaintEll(ear, c + V(0.082f * s, 0.083f, 0.01f), V(0.019f, 0.019f, 0.014f), dark);
            var look = V(0.4f * s, 0.1f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.02f, Rgb(62, 40, 28));
        });
        return b;
    }

    private static PokeBuilder C3Ursabrun()
    {
        var b = new PokeBuilder("Ursabrun", 1f, BodyPlan.Quadruped, V(0, 0.55f, 0)) { Coat = Fur };
        var brown = Rgb(130, 86, 56);
        var legs = Rgb(96, 62, 40);
        var cream = Rgb(232, 206, 156);
        var silver = Rgb(186, 186, 190);
        var moss = Rgb(108, 140, 64);
        var claw = Rgb(150, 150, 156);

        foreach (var (z, front) in new[] { (0.2f, true), (-0.22f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.13f * s, 0.48f, z), front);
                var knee = V(0.14f * s, 0.22f, z + 0.02f);
                b.Limb(leg, V(0.13f * s, 0.5f, z), knee, 0.095f, 0.078f, legs);
                b.Limb(leg, knee, V(0.145f * s, 0.07f, z + 0.04f), 0.078f, 0.072f, legs);
                var paw = V(0.145f * s, 0.04f, z + 0.07f);
                b.Ell(leg, paw, V(0.078f, 0.04f, 0.09f), legs);
                for (int i = 0; i < 4; i++)
                {
                    float x = -0.045f + 0.03f * i;
                    b.Spike(leg, paw + V(x, 0.0f, 0.07f), paw + V(x * 1.1f, -0.03f, 0.14f), 0.013f, claw, mat: Shell, blend: 0.004f);
                }
            });
        // A massive body, a hump of muscle over the shoulders
        b.Ell(Body, V(0, 0.55f, -0.02f), V(0.22f, 0.22f, 0.36f), brown);
        b.Ell(Body, V(0, 0.68f, 0.12f), V(0.18f, 0.13f, 0.17f), brown);
        C3Crescent(b, Body, V(0, 0.5f, 0.33f), 0.09f, 0.045f, cream, brown);
        // A grizzled silver mane down its back
        b.PaintEll(Body, V(0, 0.79f, 0.02f), V(0.12f, 0.08f, 0.3f), silver);
        FurTufts(b, Body, V(0, 0.7f, -0.02f), V(0.14f, 0.1f, 0.3f), 22, 0.11f, 0.035f, silver, 2f, 0.35f, 0.55f);
        // Moss and pine needles in its coat
        foreach (var (at, rr) in new[] { (V(0.19f, 0.62f, -0.12f), V(0.06f, 0.05f, 0.08f)), (V(-0.2f, 0.56f, -0.2f), V(0.05f, 0.06f, 0.06f)), (V(0.16f, 0.7f, 0.08f), V(0.04f, 0.035f, 0.05f)) })
            b.PaintEll(Body, at, rr, moss);
        PokeBuilder.Both(s => b.Spike(Body, V(0.18f * s, 0.66f, -0.14f), V(0.24f * s, 0.7f, -0.18f), 0.008f, Rgb(70, 110, 54), 0.5f));
        int tail = b.Tail(V(0, 0.6f, -0.36f));
        b.Ell(tail, V(0, 0.6f, -0.38f), V(0.045f, 0.045f, 0.04f), brown);
        int head = b.Head(V(0, 0.62f, 0.32f));
        var c = V(0, 0.63f, 0.42f);
        var r = V(0.13f, 0.12f, 0.13f);
        b.Ell(head, c, r, brown);
        b.Ell(head, c + V(0, -0.04f, 0.11f), V(0.07f, 0.06f, 0.07f), cream, blend: 0.025f);
        b.Ell(head, c + V(0, -0.02f, 0.18f), V(0.03f, 0.022f, 0.018f), Rgb(150, 86, 50), mat: Shell, blend: 0.006f);
        b.Mark(head, c + V(0, -0.075f, 0.168f), V(0, -0.4f, 1f), 0.025f, 0.008f, legs, MarkShape.Wave);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.09f * s, 0.08f, -0.03f));
            b.Ell(ear, c + V(0.1f * s, 0.095f, -0.03f), V(0.038f, 0.036f, 0.024f), brown);
            b.PaintEll(ear, c + V(0.1f * s, 0.095f, -0.008f), V(0.02f, 0.02f, 0.014f), legs);
            var look = V(0.5f * s, 0.12f, 1f);
            var at = Out(c, r, default, look);
            b.PaintEll(head, at + V(0, 0.018f, 0), V(0.03f, 0.008f, 0.02f), legs, soft: 0.006f);
            b.Eye(head, at, look, 0.016f, Rgb(70, 44, 30), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Lupling line

    private static PokeBuilder C3Lupling()
    {
        var b = new PokeBuilder("Lupling", 0.55f, BodyPlan.Quadruped, V(0, 0.2f, 0)) { Coat = Fur };
        var grey = Rgb(150, 150, 158);
        var dark = Rgb(70, 70, 80);
        var pale = Rgb(226, 226, 230);
        var amber = Rgb(222, 146, 50);

        foreach (var (z, front) in new[] { (0.07f, true), (-0.09f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.05f * s, 0.16f, z), front);
                b.Limb(leg, V(0.05f * s, 0.18f, z), V(0.055f * s, 0.03f, z + 0.01f), 0.03f, 0.024f, grey);
                b.Ell(leg, V(0.056f * s, 0.02f, z + 0.025f), V(0.028f, 0.02f, 0.036f), grey);
            });
        var bc = V(0, 0.2f, -0.01f);
        b.Ell(Body, bc, V(0.075f, 0.075f, 0.12f), grey);
        b.PaintEll(Body, bc + V(0, 0.045f, -0.01f), V(0.07f, 0.04f, 0.1f), dark);
        b.PaintEll(Body, bc + V(0, -0.05f, 0.02f), V(0.06f, 0.03f, 0.1f), pale);
        FurTufts(b, Body, bc + V(0, 0.03f, 0.07f), V(0.06f, 0.05f, 0.04f), 7, 0.04f, 0.014f, grey, 0.8f, -0.3f, 0.2f);
        int tail = b.Tail(V(0, 0.22f, -0.12f));
        BushyTail(b, tail, new[] { V(0, 0.22f, -0.12f), V(0, 0.25f, -0.19f), V(0, 0.31f, -0.24f) }, 0.025f, 0.033f, grey, dark, V(0, 1f, -0.3f));
        b.Limb(Body, bc + V(0, 0.03f, 0.08f), V(0, 0.3f, 0.1f), 0.045f, 0.04f, grey);
        // Head tilted back, howling at nothing in particular
        int head = b.Head(V(0, 0.3f, 0.1f));
        var c = V(0, 0.36f, 0.11f);
        var r = V(0.065f, 0.06f, 0.06f);
        b.Ell(head, c, r, grey);
        b.PaintEll(head, c + V(0, -0.012f, 0.045f), V(0.05f, 0.04f, 0.04f), pale);
        b.Limb(head, c + V(0, 0.0f, 0.04f), c + V(0, 0.06f, 0.1f), 0.03f, 0.021f, pale);
        b.Ell(head, c + V(0, 0.068f, 0.106f), V(0.014f, 0.012f, 0.012f), Rgb(40, 40, 46), mat: Shell, blend: 0.004f);
        b.Mark(head, c + V(0, 0.022f, 0.085f), V(0, -0.6f, 0.8f), 0.013f, 0.011f, Rgb(90, 50, 54));
        // A fringe of fur over the brow, oversized ears
        foreach (float x in new[] { -0.015f, 0.0f, 0.015f })
            b.Spike(head, c + V(x, 0.05f, 0.02f), c + V(x * 1.6f, 0.035f, 0.075f), 0.014f, pale, 0.5f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.035f * s, 0.04f, -0.015f));
            CatEar(b, ear, c + V(0.035f * s, 0.035f, -0.015f), c + V(0.1f * s, 0.13f, -0.05f), 0.03f, grey, pale);
            b.PaintEll(ear, c + V(0.095f * s, 0.12f, -0.048f), V(0.02f, 0.025f, 0.02f), dark);
            var look = V(0.55f * s, 0.25f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.013f, amber);
        });
        return b;
    }

    private static PokeBuilder C3Lupodac()
    {
        var b = new PokeBuilder("Lupodac", 0.92f, BodyPlan.Quadruped, V(0, 0.42f, 0)) { Coat = Fur };
        var grey = Rgb(144, 144, 152);
        var dark = Rgb(60, 60, 70);
        var pale = Rgb(226, 226, 230);
        var bronze = Rgb(186, 120, 66);
        var amber = Rgb(240, 150, 40);

        foreach (var (z, front) in new[] { (0.15f, true), (-0.17f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = b.Leg(s, V(0.075f * s, 0.38f, z), front);
                var knee = V(0.08f * s, 0.2f, z + (front ? 0.01f : -0.03f));
                b.Limb(leg, V(0.075f * s, 0.4f, z), knee, 0.055f, 0.035f, grey);
                b.Limb(leg, knee, V(0.082f * s, 0.045f, z + 0.01f), 0.033f, 0.027f, grey);
                b.Ell(leg, V(0.082f * s, 0.025f, z + 0.03f), V(0.03f, 0.025f, 0.042f), grey);
                // Bands of bronze-coloured fur round the forelegs, like a warrior's bracers
                if (front)
                {
                    b.Torus(leg, V(0.081f * s, 0.15f, z + 0.01f), 0.032f, 0.013f, bronze, mat: Fur, blend: 0.006f);
                    b.Torus(leg, V(0.081f * s, 0.12f, z + 0.01f), 0.031f, 0.012f, bronze, mat: Fur, blend: 0.006f);
                }
            });
        var bc = V(0, 0.44f, -0.02f);
        b.Ell(Body, bc, V(0.12f, 0.12f, 0.24f), grey);
        b.PaintEll(Body, bc + V(0, 0.07f, -0.04f), V(0.1f, 0.06f, 0.2f), dark);
        b.PaintEll(Body, bc + V(0, -0.09f, 0.02f), V(0.08f, 0.04f, 0.18f), pale);
        // A dark mane like a mantle over its shoulders
        b.Ell(Body, V(0, 0.54f, 0.13f), V(0.115f, 0.1f, 0.1f), dark, blend: 0.03f);
        FurTufts(b, Body, V(0, 0.54f, 0.13f), V(0.12f, 0.11f, 0.11f), 18, 0.11f, 0.034f, dark, 0.85f, -0.35f, 0.35f);
        int tail = b.Tail(V(0, 0.5f, -0.24f));
        BushyTail(b, tail, new[] { V(0, 0.5f, -0.24f), V(0, 0.56f, -0.36f), V(0, 0.66f, -0.42f), V(0, 0.76f, -0.4f) }, 0.04f, 0.055f, grey, dark, V(0, 1f, -0.2f));
        b.PaintEll(tail, V(0, 0.75f, -0.4f), V(0.05f, 0.04f, 0.05f), pale);
        b.Limb(Body, V(0, 0.5f, 0.17f), V(0, 0.6f, 0.25f), 0.08f, 0.065f, dark);
        int head = b.Head(V(0, 0.6f, 0.25f));
        var c = V(0, 0.64f, 0.3f);
        var r = V(0.085f, 0.075f, 0.085f);
        b.Ell(head, c, r, grey);
        b.PaintEll(head, c + V(0, -0.03f, 0.05f), V(0.07f, 0.04f, 0.06f), pale);
        // A long muzzle, lips drawn back over its teeth
        b.Limb(head, c + V(0, -0.015f, 0.06f), c + V(0, -0.03f, 0.16f), 0.045f, 0.03f, pale);
        b.Ell(head, c + V(0, -0.022f, 0.172f), V(0.018f, 0.015f, 0.014f), Rgb(36, 36, 42), mat: Shell, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.03f * s, -0.045f, 0.11f), V(0.006f, 0.008f, 0.05f), Rgb(80, 40, 44), V(0, 8f * s, 0), 0.004f);
            for (int i = 0; i < 2; i++)
                b.Spike(head, c + V(0.027f * s, -0.04f, 0.13f - 0.03f * i), c + V(0.03f * s, -0.062f, 0.13f - 0.03f * i), 0.007f, White, mat: Shell, blend: 0.003f);
            int ear = b.Ear(head, s, c + V(0.045f * s, 0.05f, -0.02f));
            CatEar(b, ear, c + V(0.045f * s, 0.05f, -0.02f), c + V(0.075f * s, 0.15f, -0.04f), 0.03f, dark, pale);
            var look = V(0.5f * s, 0.15f, 1f);
            b.Eye(head, Out(c, r, default, look), look, 0.015f, amber, glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Caprita line

    private static PokeBuilder C3Caprita()
    {
        var b = new PokeBuilder("Caprita", 0.56f, BodyPlan.Quadruped, V(0, 0.3f, 0)) { Coat = Fur };
        var tawny = Rgb(204, 146, 88);
        var white = Rgb(246, 242, 234);
        var stripe = Rgb(70, 50, 40);
        var horn = Rgb(48, 44, 44);
        var stone = Rgb(150, 150, 150);

        foreach (var (z, front) in new[] { (0.08f, true), (-0.09f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = C3HoofLeg(b, s, 0.04f, 0.28f, z, front, 0.016f, tawny, tawny, stone);
                foreach (var (y, dx, dz) in new[] { (0.09f, 0.011f, 0.005f), (0.13f, -0.01f, 0.008f), (0.06f, -0.01f, -0.005f) })
                    b.Ell(leg, C3ShankAt(s, 0.04f, 0.28f, z, front, 0.016f, y) + V(dx * s, 0, dz), V(0.01f, 0.009f, 0.01f), stone, mat: Shell, blend: 0.006f);
            });
        var bc = V(0, 0.31f, -0.01f);
        b.Ell(Body, bc, V(0.065f, 0.07f, 0.13f), tawny);
        b.PaintEll(Body, bc + V(0, -0.05f, 0.02f), V(0.05f, 0.025f, 0.1f), Rgb(226, 186, 130));
        FurTufts(b, Body, bc + V(0, 0.04f, 0.0f), V(0.04f, 0.04f, 0.11f), 6, 0.03f, 0.01f, tawny, 2f, 0.6f, 0.4f);
        int tail = b.Tail(bc + V(0, 0.03f, -0.12f));
        b.Spike(tail, bc + V(0, 0.03f, -0.12f), bc + V(0, 0.01f, -0.18f), 0.02f, tawny, 0.6f);
        b.Limb(Body, bc + V(0, 0.03f, 0.09f), V(0, 0.45f, 0.13f), 0.035f, 0.029f, tawny);
        int head = b.Head(V(0, 0.45f, 0.13f));
        var c = V(0, 0.48f, 0.15f);
        var r = V(0.043f, 0.043f, 0.05f);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.022f, 0.048f), V(0.024f, 0.023f, 0.036f), white, blend: 0.015f);
        b.Ell(head, c + V(0, -0.022f, 0.083f), V(0.01f, 0.008f, 0.006f), Black, blend: 0.004f);
        b.PaintEll(head, c + V(0, 0.01f, -0.035f), V(0.05f, 0.045f, 0.03f), tawny);
        PokeBuilder.Both(s =>
        {
            // A dark stripe through each eye from the snout to the ear
            b.PaintEll(head, c + V(0.026f * s, 0.0f, 0.02f), V(0.024f, 0.011f, 0.055f), stripe, V(18f, 0, 0), 0.006f);
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.03f, -0.015f));
            CatEar(b, ear, c + V(0.03f * s, 0.03f, -0.015f), c + V(0.075f * s, 0.07f, -0.03f), 0.016f, tawny, Rgb(240, 214, 180));
            // Little hooked black horns
            b.Tube(head, Smooth(2, c + V(0.015f * s, 0.035f, 0.0f), c + V(0.017f * s, 0.075f, -0.005f), c + V(0.018f * s, 0.095f, -0.025f), c + V(0.017f * s, 0.088f, -0.042f)), 0.009f, 0.0055f, horn, Shell, 0f);
            var look = V(0.75f * s, 0.12f, 0.7f);
            b.Eye(head, Out(c, r, default, look), look, 0.011f, Rgb(110, 70, 40));
        });
        return b;
    }

    private static PokeBuilder C3Capraneagra()
    {
        var b = new PokeBuilder("Capraneagra", 0.9f, BodyPlan.Quadruped, V(0, 0.5f, 0)) { Coat = Fur };
        var black = Rgb(56, 54, 58);
        var white = Rgb(244, 242, 236);
        var tan = Rgb(150, 104, 66);
        var horn = Rgb(90, 98, 112);
        var stone = Rgb(146, 140, 128);
        var slate = Rgb(82, 92, 106);

        foreach (var (z, front) in new[] { (0.15f, true), (-0.17f, false) })
            PokeBuilder.Both(s =>
            {
                int leg = C3HoofLeg(b, s, 0.07f, 0.46f, z, front, 0.03f, black, black, slate);
                // Plates of stone over the knees, pebbles stuck to the shanks
                var knee = V(0.073f * s, 0.23f, z + (front ? 0.04f : -0.02f));
                b.Ell(leg, knee, V(0.03f, 0.05f, 0.025f), stone, V(-10f, 0, 8f * s), Shell, 0.008f);
                b.Spike(leg, knee + V(0.005f * s, 0.02f, 0.01f), knee + V(0.01f * s, 0.07f, 0.0f), 0.018f, stone, 0.5f, Shell, 0.006f);
                foreach (var (y, dx) in new[] { (0.12f, 0.02f), (0.15f, -0.018f), (0.09f, -0.02f) })
                    b.Ell(leg, C3ShankAt(s, 0.07f, 0.46f, z, front, 0.03f, y) + V(dx * s, 0, 0.018f), V(0.012f, 0.011f, 0.012f), stone, mat: Shell, blend: 0.006f);
            });
        var bc = V(0, 0.52f, -0.02f);
        b.Ell(Body, bc, V(0.13f, 0.14f, 0.26f), black);
        b.PaintEll(Body, bc + V(0, -0.11f, 0.0f), V(0.09f, 0.03f, 0.22f), tan);
        // A thick ridge of fur along its spine
        for (int i = 0; i < 7; i++)
        {
            float z = 0.18f - 0.06f * i;
            b.Spike(Body, V(0, 0.64f - 0.004f * i, z), V(0, 0.7f - 0.004f * i, z - 0.04f), 0.03f, black, 0.45f);
        }
        // Plates of stone armouring its shoulders
        PokeBuilder.Both(s =>
        {
            var at = V(0.12f * s, 0.58f, 0.12f);
            b.Ell(Body, at, V(0.03f, 0.08f, 0.06f), stone, V(-20f, 0, 15f * s), Shell, 0.01f);
            b.Spike(Body, at + V(0.01f * s, 0.04f, -0.02f), at + V(0.03f * s, 0.13f, -0.04f), 0.03f, stone, 0.5f, Shell, 0.008f);
            b.Spike(Body, at + V(0.015f * s, -0.04f, 0.03f), at + V(0.04f * s, -0.08f, 0.07f), 0.022f, stone, 0.5f, Shell, 0.008f);
        });
        int tail = b.Tail(bc + V(0, 0.05f, -0.24f));
        b.Spike(tail, bc + V(0, 0.05f, -0.24f), bc + V(0, 0.02f, -0.32f), 0.035f, black, 0.6f);
        b.Limb(Body, bc + V(0, 0.06f, 0.18f), V(0, 0.73f, 0.28f), 0.075f, 0.055f, black);
        b.PaintEll(Body, V(0, 0.64f, 0.27f), V(0.05f, 0.08f, 0.05f), tan);
        int head = b.Head(V(0, 0.73f, 0.28f));
        var c = V(0, 0.77f, 0.31f);
        var r = V(0.06f, 0.058f, 0.07f);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.03f, 0.065f), V(0.034f, 0.032f, 0.05f), white, blend: 0.015f);
        b.Ell(head, c + V(0, -0.03f, 0.112f), V(0.014f, 0.011f, 0.008f), Black, blend: 0.004f);
        b.PaintEll(head, c + V(0, 0.012f, -0.05f), V(0.07f, 0.06f, 0.04f), black);
        PokeBuilder.Both(s =>
        {
            b.PaintEll(head, c + V(0.036f * s, 0.0f, 0.03f), V(0.034f, 0.015f, 0.075f), black, V(18f, 0, 0), 0.006f);
            b.PaintEll(head, c + V(0.05f * s, -0.03f, -0.01f), V(0.015f, 0.03f, 0.03f), tan);
            int ear = b.Ear(head, s, c + V(0.045f * s, 0.035f, -0.02f));
            CatEar(b, ear, c + V(0.045f * s, 0.035f, -0.02f), c + V(0.1f * s, 0.07f, -0.04f), 0.022f, tan, Rgb(220, 190, 160));
            // Thick hooked horns of slate curving back, ridged
            var hp = Smooth(3, c + V(0.022f * s, 0.045f, 0.0f), c + V(0.03f * s, 0.13f, -0.03f), c + V(0.04f * s, 0.17f, -0.11f), c + V(0.045f * s, 0.12f, -0.18f), c + V(0.048f * s, 0.06f, -0.16f));
            b.Tube(head, hp, 0.022f, 0.009f, horn, Shell, 0f);
            for (int i = 2; i < hp.Length - 3; i += 2)
                b.PaintEll(head, hp[i], V(0.03f, 0.005f, 0.03f), slate, Euler(hp[i + 1] - hp[i]), 0.004f);
            var look = V(0.7f * s, 0.12f, 0.7f);
            b.Eye(head, Out(c, r, default, look), look, 0.014f, Rgb(120, 84, 50), glare: true);
        });
        return b;
    }

    // ------------------------------------------------------------------ Cerbel line

    private static PokeBuilder C3Cerbel()
    {
        var b = new PokeBuilder("Cerbel", 0.6f, BodyPlan.Quadruped, V(0, 0.33f, 0)) { Coat = Fur };
        var red = Rgb(186, 96, 56);
        var cream = Rgb(244, 226, 196);
        var hoof = Rgb(70, 56, 44);
        var green = Rgb(110, 176, 72);

        foreach (var (z, front) in new[] { (0.09f, true), (-0.1f, false) })
            PokeBuilder.Both(s => C3HoofLeg(b, s, 0.04f, 0.3f, z, front, 0.017f, red, red, hoof));
        var bc = V(0, 0.33f, -0.01f);
        var br = V(0.075f, 0.08f, 0.14f);
        b.Ell(Body, bc, br, red);
        b.PaintEll(Body, bc + V(0, -0.06f, 0.03f), V(0.06f, 0.03f, 0.12f), cream);
        // White spots along its back
        foreach (var d in new[] { V(0.5f, 0.75f, 0.3f), V(-0.5f, 0.75f, 0.3f), V(0.6f, 0.6f, -0.1f), V(-0.6f, 0.6f, -0.1f), V(0.4f, 0.8f, -0.45f), V(-0.4f, 0.8f, -0.45f), V(0.7f, 0.45f, 0.1f), V(-0.7f, 0.45f, 0.1f) })
        {
            var at = Out(bc, br, default, d);
            b.Mark(Body, at, Outward(bc, br, at), 0.009f, 0.007f, cream);
        }
        int tail = b.Tail(bc + V(0, 0.04f, -0.13f));
        b.Spike(tail, bc + V(0, 0.03f, -0.13f), bc + V(0, 0.09f, -0.17f), 0.022f, red, 0.6f);
        b.PaintEll(tail, bc + V(0, 0.06f, -0.17f), V(0.025f, 0.03f, 0.015f), cream);
        b.Limb(Body, bc + V(0, 0.03f, 0.1f), V(0, 0.49f, 0.13f), 0.034f, 0.029f, red);
        b.PaintEll(Body, V(0, 0.43f, 0.14f), V(0.03f, 0.06f, 0.025f), cream);
        // A vine looped round its neck
        b.Torus(Body, V(0, 0.44f, 0.12f), 0.034f, 0.0065f, green, V(-25f, 0, 0), mat: Leaf, blend: 0.004f);
        foreach (float a in new[] { -1.2f, 0.4f, 1.6f })
            Petal(b, Body, V(MathF.Sin(a) * 0.036f, 0.44f, 0.12f + MathF.Cos(a) * 0.03f), V(MathF.Sin(a) * 0.06f, 0.42f, 0.12f + MathF.Cos(a) * 0.05f), 0.01f, green, Leaf, 0.006f);
        int head = b.Head(V(0, 0.49f, 0.13f));
        var c = V(0, 0.52f, 0.15f);
        var r = V(0.048f, 0.048f, 0.053f);
        b.Ell(head, c, r, red);
        b.Ell(head, c + V(0, -0.022f, 0.05f), V(0.024f, 0.022f, 0.036f), red, blend: 0.015f);
        b.PaintEll(head, c + V(0, -0.035f, 0.065f), V(0.024f, 0.014f, 0.03f), cream);
        b.Ell(head, c + V(0, -0.018f, 0.085f), V(0.01f, 0.008f, 0.007f), Rgb(50, 40, 36), blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.03f * s, 0.035f, -0.015f));
            var root = c + V(0.03f * s, 0.035f, -0.015f);
            var tip = c + V(0.1f * s, 0.085f, -0.03f);
            Frond(b, ear, root, tip, 0.026f, red, V(0.2f * s, 0.3f, 1f), 0.3f);
            b.PaintEll(ear, Vector3.Lerp(root, tip, 0.55f) + V(0, 0, 0.006f), V(0.016f, 0.03f, 0.008f), cream, Euler(tip - root));
            var look = V(0.6f * s, 0.12f, 0.8f);
            var at = Out(c, r, default, look);
            b.PaintEll(head, at, V(0.018f, 0.018f, 0.014f), Rgb(226, 170, 130));
            b.Eye(head, at, look, 0.014f, Rgb(60, 42, 34));
            // Two green shoots with tiny leaves where its antlers will grow
            var sr = c + V(0.016f * s, 0.04f, -0.005f);
            var sp = new[] { sr, sr + V(0.008f * s, 0.04f, 0.0f), sr + V(0.0f, 0.08f, 0.01f), sr + V(0.008f * s, 0.11f, 0.0f) };
            b.Tube(head, sp, 0.0075f, 0.006f, green, Leaf, 0f);
            Petal(b, head, sp[1], sp[1] + V(0.03f * s, 0.012f, 0.005f), 0.009f, green, Leaf, 0.004f);
            Petal(b, head, sp[2], sp[2] + V(-0.026f * s, 0.016f, 0.0f), 0.009f, green, Leaf, 0.004f);
            b.Ell(head, sp[3], V(0.01f, 0.014f, 0.01f), green, mat: Leaf, blend: 0.004f);
        });
        return b;
    }

    private static PokeBuilder C3Cerbanor()
    {
        var b = new PokeBuilder("Cerbanor", 1f, BodyPlan.Quadruped, V(0, 0.6f, 0)) { Coat = Fur };
        var russet = Rgb(176, 88, 50);
        var cream = Rgb(240, 222, 190);
        var hoof = Rgb(60, 48, 40);
        var mossLeg = Rgb(110, 120, 64);
        var antler = Rgb(112, 84, 58);
        var moss = Rgb(84, 140, 62);
        var ivy = Rgb(64, 120, 56);
        var halo = Rgb(236, 244, 196);

        foreach (var (z, front) in new[] { (0.17f, true), (-0.18f, false) })
            PokeBuilder.Both(s => C3HoofLeg(b, s, 0.07f, 0.56f, z, front, 0.03f, russet, mossLeg, hoof));
        var bc = V(0, 0.6f, -0.02f);
        var br = V(0.13f, 0.14f, 0.27f);
        b.Ell(Body, bc, br, russet);
        b.PaintEll(Body, bc + V(0, -0.11f, 0.03f), V(0.1f, 0.04f, 0.23f), cream);
        foreach (var d in new[] { V(0.45f, 0.8f, -0.1f), V(-0.45f, 0.8f, -0.1f), V(0.6f, 0.65f, -0.35f), V(-0.6f, 0.65f, -0.35f), V(0.4f, 0.85f, -0.5f), V(-0.4f, 0.85f, -0.5f), V(0.7f, 0.5f, -0.2f), V(-0.7f, 0.5f, -0.2f) })
        {
            var at = Out(bc, br, default, d);
            b.Mark(Body, at, Outward(bc, br, at), 0.014f, 0.011f, cream);
        }
        int tail = b.Tail(bc + V(0, 0.06f, -0.26f));
        b.Spike(tail, bc + V(0, 0.05f, -0.26f), bc + V(0, 0.1f, -0.32f), 0.03f, russet, 0.6f);
        b.Tube(Body, new[] { bc + V(0, 0.06f, 0.18f), V(0, 0.76f, 0.25f), V(0, 0.86f, 0.3f) }, 0.08f, 0.055f, russet, blend: 0.012f);
        b.PaintEll(Body, V(0, 0.74f, 0.31f), V(0.05f, 0.09f, 0.04f), cream);
        // A thick ruff of moss and ivy round the neck
        var ruff = V(0, 0.73f, 0.24f);
        C3Mound(b, Body, ruff + V(0, -0.07f, 0), V(0.11f, 0.08f, 0.1f), 16, 0.035f, moss, Rgb(110, 164, 80));
        b.Ell(Body, ruff, V(0.1f, 0.07f, 0.09f), moss, V(-30f, 0, 0), Leaf, 0.02f);
        foreach (var (x, z) in new[] { (-0.09f, 0.28f), (0.1f, 0.26f), (0.0f, 0.33f), (-0.11f, 0.18f), (0.11f, 0.17f) })
            Petal(b, Body, V(x, 0.69f, z), V(x * 1.15f, 0.61f, z + 0.015f), 0.014f, ivy, Leaf, 0.006f);
        int head = b.Head(V(0, 0.86f, 0.3f));
        var c = V(0, 0.9f, 0.33f);
        var r = V(0.06f, 0.06f, 0.075f);
        b.Ell(head, c, r, russet);
        b.Ell(head, c + V(0, -0.03f, 0.07f), V(0.032f, 0.03f, 0.05f), russet, blend: 0.015f);
        b.PaintEll(head, c + V(0, -0.045f, 0.09f), V(0.03f, 0.018f, 0.04f), cream);
        b.Ell(head, c + V(0, -0.025f, 0.118f), V(0.013f, 0.011f, 0.009f), Rgb(50, 40, 36), blend: 0.004f);
        // The halo of light hangs between the antlers, held where the inner tines pass through it
        var hc = c + V(0, 0.26f, -0.05f);
        b.Torus(head, hc, 0.11f, 0.011f, halo, mat: Glow, blend: 0.004f);
        PokeBuilder.Both(s =>
        {
            int ear = b.Ear(head, s, c + V(0.04f * s, 0.03f, -0.02f));
            Frond(b, ear, c + V(0.04f * s, 0.03f, -0.02f), c + V(0.11f * s, 0.06f, -0.04f), 0.026f, russet, V(0.2f * s, 0.3f, 1f), 0.3f);
            var look = V(0.6f * s, 0.12f, 0.8f);
            b.Eye(head, Out(c, r, default, look), look, 0.014f, Rgb(56, 40, 32));
            // Huge branching antlers like boughs, mossy, ivy hanging from them, small white flowers among the moss
            var a0 = c + V(0.025f * s, 0.045f, -0.025f);
            var a1 = c + V(0.11f * s, 0.15f, -0.058f);
            var a2 = c + V(0.2f * s, 0.27f, -0.05f);
            var a3 = c + V(0.24f * s, 0.4f, 0.0f);
            b.Tube(head, Smooth(2, a0, a1, a2, a3), 0.02f, 0.011f, antler, Shell, 0f);
            b.Limb(head, a1, c + V(0.11f * s, 0.36f, -0.045f), 0.012f, 0.011f, antler, Shell, 0.004f);
            b.Limb(head, Vector3.Lerp(a0, a1, 0.6f), Vector3.Lerp(a0, a1, 0.6f) + V(0.01f * s, 0.07f, 0.08f), 0.012f, 0.011f, antler, Shell, 0.004f);
            b.Limb(head, a2, a2 + V(0.08f * s, 0.08f, -0.02f), 0.012f, 0.011f, antler, Shell, 0.004f);
            b.Limb(head, Vector3.Lerp(a2, a3, 0.4f), Vector3.Lerp(a2, a3, 0.4f) + V(-0.02f * s, 0.1f, -0.05f), 0.012f, 0.011f, antler, Shell, 0.004f);
            foreach (float t in new[] { 0.25f, 0.55f })
                b.PaintEll(head, Vector3.Lerp(a1, a2, t) + V(0, 0.012f, 0), V(0.045f, 0.02f, 0.03f), moss, Euler(a2 - a1), 0.008f);
            b.PaintEll(head, Vector3.Lerp(a2, a3, 0.3f) + V(0, 0.01f, 0), V(0.03f, 0.03f, 0.03f), moss, soft: 0.008f);
            foreach (var at in new[] { Vector3.Lerp(a1, a2, 0.3f), a2 + V(0.01f * s, 0.0f, 0.0f), Vector3.Lerp(a2, a3, 0.5f) })
                b.Ell(head, at + V(0, 0.014f, 0.01f), V(0.011f, 0.008f, 0.011f), Rgb(250, 250, 244), mat: Leaf, blend: 0.004f);
            foreach (float t in new[] { 0.45f, 0.9f })
            {
                var from = Vector3.Lerp(a1, a2, t);
                Petal(b, head, from + V(0, -0.008f, 0), from + V(0.01f * s, -0.07f, 0.01f), 0.012f, ivy, Leaf, 0.006f);
            }
        });
        return b;
    }
}
