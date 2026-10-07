using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// The forms Platinum itself gives species of its Sinnoh Pokédex, hand-built like the species (after plan 03 · D11):
// Rotom's five appliances, Giratina's Origin Forme, Burmy's and Wormadam's sandy and trash cloaks, Shellos's and
// Gastrodon's East Sea colours, Cherrim in the sun and Unown's other letters; and those it gives the popular species
// from outside its Pokédex, hand-built since: Castform's weathers and Deoxys's formes (their builds in
// PokemonModels.Hoenn3.cs), Shaymin's Sky Forme and Arceus's types (Arceus's build in PokemonModels.Sinnoh5.cs).
// Each is our own sculpt after the design.
internal static partial class PokemonModels
{
    /// <summary>What Burmy has wrapped round itself, and what Wormadam's gown grew from.</summary>
    private enum Cloak { Plant, Sandy, Trash }

    // ------------------------------------------------------------------ Rotom's appliances

    private static readonly Color RotomOrange = Rgb(244, 114, 42);

    /// <summary>Rotom's eyes, white with a pupil in the colour of the appliance's glow, and the point of its plasma on top.</summary>
    private static void RotomFace(PokeBuilder b, int bone, Vector3 left, Vector3 right, Vector3 facing, float size, Color glow, Vector3 crown, Vector3 crownTip, float crownRadius)
    {
        b.Spike(bone, crown, crownTip, crownRadius, RotomOrange);
        b.Eye(bone, left, Vector3.Normalize(facing + V(-0.3f, 0, 0)), size, sclera: true, pupil: glow);
        b.Eye(bone, right, Vector3.Normalize(facing + V(0.3f, 0, 0)), size, sclera: true, pupil: glow);
    }

    private static PokeBuilder RotomHeat()
    {
        var b = new PokeBuilder("Rotom-Heat", 0.5f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var glow = Rgb(236, 82, 92);
        var glass = Rgb(40, 38, 46);

        // A microwave oven, domed like a helmet, its dark window low across the front
        var c = V(0, 0.3f, 0);
        b.Box(Body, c + V(0, -0.02f, 0), V(0.16f, 0.1f, 0.13f), 0.06f, RotomOrange);
        b.Ell(Body, c + V(0, 0.035f, -0.005f), V(0.165f, 0.13f, 0.135f), RotomOrange);
        b.Box(Body, c + V(0, -0.055f, 0.105f), V(0.14f, 0.06f, 0.04f), 0.035f, glass, mat: Shell, blend: 0.004f);
        b.Box(Body, c + V(0, -0.035f, 0.146f), V(0.11f, 0.008f, 0.004f), 0.004f, Rgb(120, 120, 132), mat: Shell, blend: 0.002f);
        RotomFace(b, Body, c + V(-0.055f, 0.065f, 0.125f), c + V(0.055f, 0.065f, 0.125f), V(0, 0.1f, 1f), 0.03f, glow,
            c + V(0, 0.14f, -0.02f), c + V(0, 0.27f, -0.07f), 0.05f);
        b.Mark(Body, c + V(0, 0.025f, 0.133f), V(0, 0, 1f), 0.01f, 0.014f, Rgb(250, 190, 150));
        // Short arms of plasma hanging down to flat, notched hands
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.14f * s, 0.24f, 0.03f));
            b.Tube(arm, new[] { V(0.14f * s, 0.24f, 0.03f), V(0.2f * s, 0.2f, 0.05f), V(0.19f * s, 0.13f, 0.06f), V(0.21f * s, 0.09f, 0.07f) }, 0.025f, 0.021f, glow, Glow);
            b.Box(arm, V(0.24f * s, 0.05f, 0.08f), V(0.065f, 0.045f, 0.016f), 0.012f, glow, V(0, 0, 20f * s), Glow, 0.006f);
            b.CutBox(arm, V(0.275f * s, 0.01f, 0.08f), V(0.014f, 0.025f, 0.03f), Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 20f * s * Degree));
        });
        return Lift(b);
    }

    private static PokeBuilder RotomWash()
    {
        var b = new PokeBuilder("Rotom-Wash", 0.5f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Shell }.Hover();
        var glow = Rgb(56, 176, 214);
        var ring = Rgb(40, 38, 46);

        // A front-loading washing machine: a square body, a round door with blue glass
        var c = V(0, 0.3f, 0);
        b.Box(Body, c, V(0.125f, 0.13f, 0.11f), 0.035f, RotomOrange);
        b.Torus(Body, c + V(0, -0.025f, 0.105f), 0.066f, 0.02f, ring, V(90f, 0, 0), mat: Shell, blend: 0.004f);
        b.Ell(Body, c + V(0, -0.025f, 0.1f), V(0.058f, 0.058f, 0.024f), Rgb(70, 170, 210), mat: Shell, blend: 0.004f);
        b.Ell(Body, c + V(0.02f, -0.005f, 0.122f), V(0.016f, 0.012f, 0.006f), Rgb(170, 226, 244), mat: Shell, blend: 0.002f);
        RotomFace(b, Body, c + V(-0.045f, 0.08f, 0.11f), c + V(0.045f, 0.08f, 0.11f), V(0, 0.05f, 1f), 0.024f, glow,
            c + V(0, 0.12f, 0.0f), c + V(0.0f, 0.24f, -0.02f), 0.04f);
        // A tuft of plasma either side of its point, a little grin
        PokeBuilder.Both(s => b.Spike(Body, c + V(0.05f * s, 0.12f, 0), c + V(0.1f * s, 0.2f, -0.02f), 0.025f, RotomOrange));
        b.Mark(Body, c + V(0, 0.045f, 0.112f), V(0, 0, 1f), 0.016f, 0.006f, Rgb(120, 40, 20), MarkShape.Wave);
        // The hose out of its side, and arms of plasma: one ends in a clamp
        b.Tube(Body, new[] { c + V(-0.1f, -0.1f, 0.05f), c + V(-0.17f, -0.13f, 0.07f), c + V(-0.25f, -0.12f, 0.09f) }, 0.02f, 0.02f, RotomOrange);
        b.Torus(Body, c + V(-0.255f, -0.12f, 0.09f), 0.016f, 0.008f, ring, V(0, 0, 90f), mat: Shell, blend: 0.003f);
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.12f * s, 0.32f, 0));
            var hand = s > 0 ? V(0.26f, 0.14f, 0.02f) : V(-0.2f, 0.16f, 0.03f);
            FlatBolt(b, arm, new[] { V(0.11f * s, 0.32f, 0), V(0.19f * s, 0.27f, 0.01f), hand }, new[] { 0.018f, 0.016f }, 0.012f, glow);
            if (s > 0) b.Torus(arm, hand + V(0.02f, -0.02f, 0), 0.03f, 0.011f, glow, V(90f, 0, 0), mat: Glow, blend: 0.004f);
        });
        return Lift(b);
    }

    private static PokeBuilder RotomFrost()
    {
        var b = new PokeBuilder("Rotom-Frost", 0.56f, BodyPlan.Floating, V(0, 0.32f, 0)) { Coat = Shell }.Hover();
        var glow = Rgb(170, 140, 222);
        var seam = Rgb(150, 60, 24);

        // A tall refrigerator leaning a little: a freezer door on top, two doors with handles below
        var c = V(0, 0.32f, 0);
        var lean = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, -6f * Degree);
        Vector3 At(float x, float y, float z) => c + Vector3.Transform(V(x, y, z), lean);
        b.Box(Body, c, V(0.105f, 0.17f, 0.095f), 0.03f, RotomOrange, V(0, 0, -6f));
        b.Box(Body, At(0, 0.06f, 0.094f), V(0.1f, 0.004f, 0.006f), 0.003f, seam, V(0, 0, -6f), blend: 0.002f);
        b.Box(Body, At(0, -0.06f, 0.094f), V(0.004f, 0.11f, 0.006f), 0.003f, seam, V(0, 0, -6f), blend: 0.002f);
        PokeBuilder.Both(s => b.Ell(Body, At(0.022f * s, -0.04f, 0.1f), V(0.01f, 0.03f, 0.012f), Rgb(250, 150, 90), V(0, 0, -6f), Shell, 0.004f));
        RotomFace(b, Body, At(-0.04f, 0.115f, 0.096f), At(0.04f, 0.115f, 0.096f), V(-0.1f, 0, 1f), 0.022f, glow,
            At(0, 0.16f, -0.01f), At(-0.02f, 0.28f, -0.04f), 0.04f);
        b.Mark(Body, At(0, -0.13f, 0.096f), V(-0.1f, 0, 1f), 0.024f, 0.014f, Rgb(70, 30, 20));
        // Wings of plasma spread from its shoulders, two points each
        PokeBuilder.Both(s =>
        {
            int wing = b.Wing(s, At(0.09f * s, 0.08f, 0));
            var root = At(0.09f * s, 0.08f, -0.02f);
            Blade(b, wing, root, root + V(0.22f * s, 0.12f, -0.02f), 0.05f, glow, V(0, 0, 1f), 0.25f, Glow);
            Blade(b, wing, root + V(0.0f, -0.03f, 0), root + V(0.18f * s, -0.06f, -0.02f), 0.04f, glow, V(0, 0, 1f), 0.25f, Glow);
        });
        return Lift(b);
    }

    private static PokeBuilder RotomFan()
    {
        var b = new PokeBuilder("Rotom-Fan", 0.56f, BodyPlan.Floating, V(0, 0.2f, 0)) { Coat = Shell }.Hover();
        var glow = Rgb(246, 210, 100);
        var blade = Rgb(58, 52, 58);

        // An electric fan: a round head with three dark blades round an orange hub, on a neck from Rotom's own body
        var h = V(0, 0.44f, 0);
        b.Ell(Body, h + V(0, 0, -0.02f), V(0.13f, 0.13f, 0.035f), RotomOrange);
        b.Torus(Body, h, 0.125f, 0.02f, glow, V(90f, 0, 0), mat: Glow);
        for (int i = 0; i < 3; i++)
        {
            float a = i * MathF.Tau / 3f + 0.4f;
            var dir = V(MathF.Cos(a), MathF.Sin(a), 0);
            b.Ell(Body, h + dir * 0.058f + V(0, 0, 0.012f), V(0.04f, 0.058f, 0.008f), blade, V(0, 0, a / Degree - 90f), Shell, 0.004f);
        }
        b.Spike(Body, h + V(0, 0, 0.01f), h + V(0, 0, 0.1f), 0.03f, RotomOrange);
        b.Limb(Body, h + V(0, -0.11f, -0.01f), V(0, 0.22f, 0.0f), 0.022f, 0.026f, RotomOrange);
        var c = V(0, 0.16f, 0.01f);
        var r = V(0.075f, 0.07f, 0.065f);
        b.Ell(Body, c, r, RotomOrange);
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.03f * s, 0.175f), V(0.35f * s, 0.05f, 1f), 0.02f, sclera: true, pupil: Rgb(236, 112, 28)));
        b.Mark(Body, On(c, r, 0, 0.14f), V(0, -0.1f, 1f), 0.013f, 0.005f, Rgb(120, 40, 20), MarkShape.Wave);
        // Arms of plasma curled into spirals, one off the head, one off the body
        PokeBuilder.Both(s =>
        {
            var root = s < 0 ? h + V(-0.12f, 0.04f, 0) : c + V(0.07f, -0.01f, 0);
            var center = s < 0 ? V(-0.27f, 0.6f, 0) : V(0.24f, 0.1f, 0);
            int arm = b.Arm(s, root);
            var curl = Spiral(center, V(-s, 0, 0), V(0, s, 0), 0.07f, 0.02f, 0f, MathF.Tau * 1.15f, 16);
            b.Tube(arm, new[] { root }.Concat(curl).ToArray(), 0.018f, 0.012f, glow, Glow);
        });
        return Lift(b);
    }

    private static PokeBuilder RotomMow()
    {
        var b = new PokeBuilder("Rotom-Mow", 0.56f, BodyPlan.Floating, V(0, 0.26f, 0)) { Coat = Shell }.Hover();
        var glow = Rgb(122, 200, 84);
        var dark = Rgb(40, 38, 46);

        // A lawnmower: a deck sloping down to a grinning face, two wheels, a handle standing up from the back
        var c = V(0, 0.26f, 0);
        b.Box(Body, c, V(0.15f, 0.085f, 0.13f), 0.05f, RotomOrange);
        b.Ell(Body, c + V(0, 0.04f, -0.02f), V(0.15f, 0.08f, 0.12f), RotomOrange);
        b.Box(Body, c + V(-0.04f, 0.1f, -0.05f), V(0.075f, 0.02f, 0.06f), 0.015f, dark, V(-10f, 0, 0), Shell, 0.004f);
        b.Limb(Body, c + V(0.05f, 0.1f, -0.06f), c + V(0.05f, 0.27f, -0.1f), 0.02f, 0.018f, RotomOrange);
        b.Ell(Body, c + V(0.05f, 0.29f, -0.1f), V(0.085f, 0.02f, 0.05f), RotomOrange);
        PokeBuilder.Both(s =>
        {
            b.Ell(Body, c + V(0.155f * s, -0.07f, 0.03f), V(0.032f, 0.07f, 0.07f), dark, mat: Shell, blend: 0.006f);
            b.Ell(Body, c + V(0.18f * s, -0.07f, 0.03f), V(0.012f, 0.03f, 0.03f), Rgb(120, 120, 130), mat: Metal, blend: 0.003f);
        });
        var face = V(0, 0, 1f);
        RotomFace(b, Body, c + V(-0.045f, 0.03f, 0.13f), c + V(0.045f, 0.03f, 0.13f), face, 0.026f, glow,
            c + V(-0.07f, 0.1f, 0.02f), c + V(-0.12f, 0.2f, 0.0f), 0.035f);
        b.Mark(Body, c + V(0, -0.045f, 0.132f), face, 0.075f, 0.026f, dark);
        b.Mark(Body, c + V(0, -0.045f, 0.1325f), face, 0.065f, 0.016f, White, MarkShape.Zigzag);
        // A bolt of plasma under it
        int tail = b.Tail(c + V(0, -0.08f, 0.02f));
        FlatBolt(b, tail, new[] { c + V(0.0f, -0.07f, 0.03f), c + V(0.05f, -0.13f, 0.03f), c + V(0.0f, -0.15f, 0.03f), c + V(0.05f, -0.22f, 0.03f) },
            new[] { 0.016f, 0.02f, 0.02f }, 0.012f, glow);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Giratina's Origin Forme

    private static PokeBuilder GiratinaOrigin()
    {
        // From the tail's end to the head: a long body looped in the air, the neck rising in front
        var spine = new[]
        {
            V(0.06f, 0.6f, -0.34f), V(0.09f, 0.44f, -0.42f), V(0.06f, 0.29f, -0.32f), V(0.0f, 0.23f, -0.12f), V(0.0f, 0.27f, 0.06f),
            V(0.0f, 0.42f, 0.17f), V(0.0f, 0.6f, 0.24f), V(0.0f, 0.75f, 0.3f)
        };
        float Radius(int i) => i switch { 0 => 0.07f, 1 => 0.095f, 2 => 0.11f, 3 => 0.115f, 4 => 0.11f, 5 => 0.095f, 6 => 0.085f, _ => 0.08f };
        var b = new PokeBuilder("Giratina-Origin", 1f, BodyPlan.Serpent, spine[0]) { Coat = Scales }.Hover();
        var gray = Rgb(176, 176, 188);
        var black = Rgb(40, 36, 50);
        var red = Rgb(214, 56, 62);
        var gold = Rgb(236, 196, 72);
        var shadow = Rgb(48, 38, 60);

        // Banded black and red all along, golden spikes standing out from its sides
        int parent = Body;
        for (int i = 1; i < spine.Length - 1; i++)
        {
            int seg = b.Part("seg" + i, parent, spine[i], PokeRole.Segment, i);
            b.Limb(seg, spine[i], spine[i + 1], Radius(i), Radius(i + 1), gray);
            var along = Vector3.Normalize(spine[i + 1] - spine[i]);
            var mid = (spine[i] + spine[i + 1]) / 2f;
            float r = (Radius(i) + Radius(i + 1)) / 2f;
            b.PaintTorus(seg, mid - along * 0.02f, r, 0.028f, black, Euler(along), 1.05f, 1.05f);
            b.PaintTorus(seg, mid + along * 0.035f, r, 0.013f, red, Euler(along), 1.05f, 1.05f);
            var side = Vector3.Normalize(Vector3.Cross(along, V(0, 0, 1f)) + V(0.001f, 0, 0));
            if (MathF.Abs(side.X) < 0.5f) side = V(1f, 0, 0);
            PokeBuilder.Both(s => b.Spike(seg, mid + side * s * r * 0.75f, mid + side * s * (r + 0.13f) - along * 0.06f, 0.028f, gold, mat: Metal));
            parent = seg;
        }
        b.Limb(Body, spine[0], spine[1], Radius(0), Radius(1), gray);
        b.PaintTorus(Body, (spine[0] + spine[1]) / 2f, 0.085f, 0.025f, black, Euler(spine[1] - spine[0]), 1.05f, 1.05f);
        // The tail curls forward over the loop
        int tail = b.Tail(spine[0]);
        b.Tube(tail, Smooth(3, spine[0], V(0.04f, 0.68f, -0.24f), V(-0.02f, 0.66f, -0.15f), V(-0.06f, 0.6f, -0.12f)), 0.07f, 0.025f, gray, Scales, 0.0f);
        b.Spike(tail, V(0.07f, 0.68f, -0.26f), V(0.12f, 0.8f, -0.33f), 0.025f, gold, mat: Metal);

        // Six shadowy tendrils streaming back from its shoulders, each tipped in red
        for (int i = 0; i < 3; i++)
            PokeBuilder.Both(s =>
            {
                var root = V(0.05f * s, 0.5f - i * 0.06f, 0.12f - i * 0.04f);
                int tendril = b.Part((s < 0 ? "tendrilL" : "tendrilR") + i, Body, root, PokeRole.Tail, i * 1.3f + (s > 0 ? 0.6f : 0f), s);
                var tip = V(s * (0.22f + i * 0.08f), 1.0f - i * 0.12f, -0.2f - i * 0.16f);
                // Each one waves, and swells toward its claw
                var d = tip - root;
                var curve = Smooth(3, root, root + d * 0.33f + V(s * 0.06f, 0.04f, 0.04f), root + d * 0.66f + V(-s * 0.04f, 0.02f, 0), tip);
                int half = curve.Length * 2 / 3;
                b.Tube(tendril, curve.Take(half + 1).ToArray(), 0.034f, 0.056f, shadow, Scales, 0.0f);
                b.Tube(tendril, curve.Skip(half).ToArray(), 0.056f, 0.042f, shadow, Scales, 0.0f);
                var dir = Vector3.Normalize(tip - curve[^3]);
                b.Spike(tendril, tip, tip + dir * 0.1f, 0.026f, red, mat: Shell);
            });

        // A golden crown of points over a grey face, gold plates down its jaw, red eyes in a dark mask
        int head = b.Head(spine[^1], parent);
        var hc = V(0, 0.85f, 0.37f);
        b.Ell(head, hc, V(0.11f, 0.1f, 0.155f), gray);
        b.Ell(head, hc + V(0, 0.065f, -0.02f), V(0.12f, 0.06f, 0.165f), gold, V(-12f, 0, 0), Metal, 0.012f);
        b.Spike(head, hc + V(0, 0.095f, 0.07f), hc + V(0, 0.24f, 0.24f), 0.048f, gold, mat: Metal);
        PokeBuilder.Both(s =>
        {
            b.Spike(head, hc + V(0.07f * s, 0.085f, 0.035f), hc + V(0.16f * s, 0.22f, 0.12f), 0.038f, gold, mat: Metal);
            b.Ell(head, hc + V(0.095f * s, -0.05f, 0.0f), V(0.036f, 0.09f, 0.095f), gold, V(15f, 0, 0), Metal, 0.008f);
            b.Spike(head, hc + V(0.105f * s, -0.1f, -0.035f), hc + V(0.15f * s, -0.22f, -0.12f), 0.036f, gold, mat: Metal);
        });
        b.Ell(head, hc + V(0, -0.025f, 0.145f), V(0.06f, 0.042f, 0.054f), black, blend: 0.01f);
        PokeBuilder.Both(s => b.Eye(head, hc + V(0.058f * s, 0.018f, 0.13f), V(0.6f * s, 0.1f, 1f), 0.028f, red));
        // Golden plates down the back of the neck
        for (int i = 0; i < 3; i++)
        {
            var at = Vector3.Lerp(spine[5], spine[7], i / 2.5f);
            Blade(b, parent, at + V(0, 0.02f, -0.06f), at + V(0, 0.1f, -0.22f), 0.05f, gold, V(1f, 0, 0), 0.25f, Metal);
        }
        return Lift(b);
    }

    // ------------------------------------------------------------------ Burmy's and Wormadam's cloaks

    /// <summary>
    /// Lumps over an upright ellipsoid, spread by the golden angle, in two shades: a cloak of sand or rags. Nothing
    /// grows on the front above <paramref name="faceAbove"/>, where the face looks out.
    /// </summary>
    private static void Lumps(PokeBuilder b, int bone, Vector3 center, Vector3 radii, int count, float size, Color color, Color light, float faceAbove)
    {
        float golden = MathF.PI * (3f - MathF.Sqrt(5f));
        for (int i = 0; i < count; i++)
        {
            float u = -0.85f + 1.75f * (i + 0.5f) / count, ring = MathF.Sqrt(1f - u * u), a = i * golden;
            var at = center + V(MathF.Sin(a) * ring * radii.X, u * radii.Y, MathF.Cos(a) * ring * radii.Z);
            if (at.Z > center.Z && at.Y > faceAbove && MathF.Abs(at.X - center.X) < radii.X * 0.75f) continue;
            float k = 0.8f + 0.35f * ((i * 7) % 5) / 4f;
            b.Ell(bone, at, V(size, size * 0.85f, size) * k, i % 3 == 0 ? light : color, blend: 0.02f);
        }
    }

    /// <summary>Burmy's cloak of sand with pebbles stuck in it, or of pink rags with two pipes poking out and a knot below.</summary>
    private static void BurmyCloak(PokeBuilder b, Cloak cloak)
    {
        var c = V(0, 0.3f, 0);
        var r = V(0.15f, 0.19f, 0.13f);
        if (cloak == Cloak.Sandy)
        {
            var sand = Rgb(232, 212, 156);
            b.Ell(Body, c, r, sand);
            Lumps(b, Body, c, r, 44, 0.032f, sand, Rgb(244, 230, 186), 0.38f);
            foreach (var (x, y) in new[] { (-0.075f, 0.25f), (0.08f, 0.3f), (0.0f, 0.15f) })
            {
                var at = On(c, r, x, y);
                b.Ell(Body, at, V(0.04f, 0.034f, 0.026f), Rgb(128, 128, 140), Toward(Outward(c, r, at)), Shell, 0.006f);
            }
            return;
        }
        var pink = Rgb(236, 142, 158);
        var dark = Rgb(212, 112, 136);
        b.Ell(Body, c, r, pink);
        // Frilled tiers of rag, one under another
        for (int t = 0; t < 4; t++)
        {
            float y = 0.42f - t * 0.075f, u = (y - c.Y) / r.Y, ring = MathF.Sqrt(MathF.Max(0.05f, 1f - u * u));
            for (int i = 0; i < 9; i++)
            {
                float a = (i + 0.5f * (t % 2)) * MathF.Tau / 9f;
                var at = c + V(MathF.Sin(a) * r.X * ring, y - c.Y, MathF.Cos(a) * r.Z * ring);
                if (t == 0 && at.Z > 0.04f && MathF.Abs(at.X) < 0.1f) continue;
                b.Ell(Body, at, V(0.045f, 0.03f, 0.045f), t % 2 == 0 ? pink : dark, blend: 0.018f);
            }
        }
        PokeBuilder.Both(s => b.Tube(Body, new[] { V(0.08f * s, 0.17f, 0.05f), V(0.17f * s, 0.14f, 0.06f), V(0.21f * s, 0.08f, 0.07f) }, 0.012f, 0.011f, Rgb(150, 150, 162), Metal));
        b.Ell(Body, V(0, 0.1f, 0.0f), V(0.035f, 0.03f, 0.035f), dark, blend: 0.012f);
        foreach (float x in new[] { -0.025f, 0f, 0.025f })
            b.Limb(Body, V(x * 0.5f, 0.08f, 0.0f), V(x, 0.03f, 0.01f), 0.01f, 0.008f, dark, blend: 0.006f);
    }

    /// <summary>Wormadam's gown of sand with red stones and a stole of rocks, or of pink rags with a scarf.</summary>
    private static void WormadamGown(PokeBuilder b, Cloak cloak)
    {
        var lower = V(0, 0.28f, 0);
        var lowerR = V(0.17f, 0.18f, 0.14f);
        if (cloak == Cloak.Sandy)
        {
            var sand = Rgb(232, 212, 156);
            var rock = Rgb(168, 118, 76);
            b.Ell(Body, lower, lowerR, sand);
            b.Ell(Body, V(0, 0.42f, 0.01f), V(0.1f, 0.07f, 0.08f), sand);
            Lumps(b, Body, V(0, 0.26f, 0), V(0.17f, 0.16f, 0.14f), 22, 0.05f, sand, Rgb(244, 230, 186), 0.4f);
            foreach (var (x, y, size) in new[] { (-0.07f, 0.33f, 0.03f), (0.07f, 0.33f, 0.03f), (-0.035f, 0.22f, 0.018f), (0.045f, 0.2f, 0.018f), (0.0f, 0.15f, 0.016f) })
            {
                var at = On(lower, lowerR, x, y);
                b.Ell(Body, at, V(size, size, 0.01f), Rgb(232, 98, 78), Toward(Outward(lower, lowerR, at)), Shell, 0.004f);
            }
            // A stole of rocks out from its shoulders, curling up at the ends
            PokeBuilder.Both(s =>
            {
                int arm = b.Arm(s, V(0.1f * s, 0.42f, 0));
                foreach (var (x, y, size) in new[] { (0.13f, 0.43f, 0.04f), (0.2f, 0.44f, 0.045f), (0.27f, 0.47f, 0.042f), (0.32f, 0.53f, 0.036f), (0.15f, 0.5f, 0.026f) })
                    b.Box(arm, V(x * s, y, -0.01f), V(size, size * 0.9f, size * 0.85f), size * 0.6f, rock, V(15f, 25f * s, 30f * s), Shell, 0.01f);
            });
            return;
        }
        var pink = Rgb(236, 146, 160);
        var dark = Rgb(214, 116, 138);
        var scarf = Rgb(196, 72, 150);
        b.Ell(Body, lower, lowerR, pink);
        b.Ell(Body, V(0, 0.42f, 0.01f), V(0.1f, 0.07f, 0.08f), pink);
        for (int t = 0; t < 3; t++)
        {
            float y = 0.38f - t * 0.09f, u = (y - lower.Y) / lowerR.Y, ring = MathF.Sqrt(MathF.Max(0.05f, 1f - u * u));
            for (int i = 0; i < 10; i++)
            {
                float a = (i + 0.5f * (t % 2)) * MathF.Tau / 10f;
                b.Ell(Body, lower + V(MathF.Sin(a) * lowerR.X * ring, y - lower.Y, MathF.Cos(a) * lowerR.Z * ring), V(0.05f, 0.032f, 0.05f), t % 2 == 0 ? pink : dark, blend: 0.018f);
            }
        }
        b.Ell(Body, V(0, 0.09f, 0.0f), V(0.04f, 0.03f, 0.04f), dark, blend: 0.012f);
        foreach (float x in new[] { -0.03f, 0f, 0.03f })
            b.Limb(Body, V(x * 0.5f, 0.07f, 0.0f), V(x, 0.02f, 0.01f), 0.011f, 0.009f, dark, blend: 0.006f);
        // A scarf over its shoulders: one end lifted in the air, the other hanging down
        PokeBuilder.Both(s =>
        {
            int arm = b.Arm(s, V(0.08f * s, 0.45f, -0.02f));
            var root = V(0.07f * s, 0.46f, -0.03f);
            var end = s < 0 ? V(-0.32f, 0.58f, -0.06f) : V(0.3f, 0.3f, -0.03f);
            var bend = s < 0 ? V(-0.18f, 0.56f, -0.05f) : V(0.18f, 0.42f, -0.04f);
            Frond(b, arm, root, bend, 0.04f, scarf, V(0, 0, 1f), 0.3f);
            Frond(b, arm, bend - Vector3.Normalize(bend - root) * 0.02f, end, 0.045f, scarf, V(0, 0, 1f), 0.3f);
            var dir = Vector3.Normalize(end - bend);
            var across = Vector3.Normalize(Vector3.Cross(dir, V(0, 0, 1f)));
            foreach (float k in new[] { -1f, 0f, 1f })
                b.Limb(arm, end - dir * 0.03f + across * k * 0.014f, end + across * k * 0.03f + dir * 0.05f, 0.009f, 0.007f, scarf, blend: 0.004f);
        });
    }

    // ------------------------------------------------------------------ the East Sea's Shellos and Gastrodon

    /// <summary>The East Sea Shellos's back: a rounded fin standing up from each segment, rimmed in yellow.</summary>
    private static void EastRidge(PokeBuilder b, int seg, Vector3 at, float r, Color back, Color yellow) =>
        RimmedFin(b, seg, at + V(0, r * 1.2f, -r * 0.1f), V(0.014f, r * 0.42f, r * 0.62f), V(0, 0, 0), back, yellow, 0.012f);

    /// <summary>The East Sea's crest: a blue knob and a white horn for Shellos, two green lobes swept up for Gastrodon.</summary>
    private static void EastCrest(PokeBuilder b, int head, float hy, float k, bool grown, Color back)
    {
        if (!grown)
        {
            b.Ell(head, V(-0.03f, hy + 0.1f, 0.09f), V(0.05f, 0.045f, 0.05f), back);
            b.Tube(head, Smooth(3, V(0.02f, hy + 0.09f, 0.1f), V(0.07f, hy + 0.13f, 0.09f), V(0.11f, hy + 0.2f, 0.05f)), 0.03f, 0.008f, Rgb(240, 242, 244), Shell, 0.006f);
            return;
        }
        PokeBuilder.Both(s => Blade(b, head, V(0.05f * s * k, hy + 0.07f * k, 0.1f * k),
            V((s > 0 ? 0.19f : 0.12f) * s * k, hy + (s > 0 ? 0.26f : 0.18f) * k, 0.05f * k), (s > 0 ? 0.06f : 0.045f) * k, back, V(0, 0, 1f), 0.35f));
    }

    // ------------------------------------------------------------------ Cherrim in the sun

    private static PokeBuilder CherrimSunshine()
    {
        var b = new PokeBuilder("Cherrim-Sunshine", 0.62f, BodyPlan.Floating, V(0, 0.3f, 0)) { Coat = Leaf };
        var petal = Rgb(246, 178, 196);
        var yellow = Rgb(250, 226, 112);
        var cherry = Rgb(222, 64, 100);

        PokeBuilder.Both(s =>
        {
            int leg = b.Leg(s, V(0.045f * s, 0.2f, 0.02f));
            b.Limb(leg, V(0.045f * s, 0.21f, 0.02f), V(0.055f * s, 0.04f, 0.035f), 0.022f, 0.024f, yellow);
            b.Ell(leg, V(0.055f * s, 0.025f, 0.045f), V(0.035f, 0.025f, 0.045f), yellow);
        });
        // Open in the sun: a round yellow face, five cherry-blossom petals behind it, two cherries on top
        var c = V(0, 0.32f, 0.02f);
        var r = V(0.13f, 0.12f, 0.11f);
        b.Ell(Body, c, r, yellow);
        for (int i = 0; i < 5; i++)
        {
            float a = (i - 2) * 0.62f;
            Petal(b, Body, V(MathF.Sin(a) * 0.04f, 0.2f, 0.03f), V(MathF.Sin(a) * 0.12f, 0.13f, 0.05f + MathF.Cos(a) * 0.04f), 0.045f, petal, Leaf);
        }
        b.Ell(Body, V(0, 0.225f, 0.09f), V(0.02f, 0.015f, 0.02f), Rgb(64, 176, 80), blend: 0.006f);
        int top = b.Part("petals", Body, V(0, 0.34f, -0.05f), PokeRole.Leaf);
        for (int i = 0; i < 5; i++)
        {
            float a = i * MathF.Tau / 5f;
            var dir = V(MathF.Sin(a), MathF.Cos(a), 0);
            var across = V(dir.Y, -dir.X, 0);
            var root = V(0, 0.34f, -0.06f) + dir * 0.06f;
            var tip = V(0, 0.34f, -0.06f) + dir * 0.31f;
            // Two lobes that part at the tip, the notch of a cherry blossom's petal
            Frond(b, top, root, tip + across * 0.06f, 0.062f, petal, V(0, 0, 1f), 0.16f, Leaf);
            Frond(b, top, root, tip - across * 0.06f, 0.062f, petal, V(0, 0, 1f), 0.16f, Leaf);
        }
        PokeBuilder.Both(s => b.Ell(Body, V(0.08f * s, 0.43f, 0.05f), V(0.048f, 0.048f, 0.045f), cherry, mat: Shell, blend: 0.008f));
        PokeBuilder.Both(s => b.Eye(Body, On(c, r, 0.045f * s, 0.335f), V(0.3f * s, 0.05f, 1f), 0.018f, Rgb(200, 40, 80)));
        b.Mark(Body, On(c, r, 0, 0.27f), V(0, -0.25f, 1f), 0.036f, 0.028f, Rgb(176, 52, 84));
        return b;
    }

    // ------------------------------------------------------------------ Unown's letters

    /// <summary>A circle's arc in the front plane, centred at (x, y), from a0 to a1 degrees (anticlockwise from the right).</summary>
    private static Vector3[] Arc(float x, float y, float r, float a0, float a1, int steps = 12)
    {
        var points = new Vector3[steps + 1];
        for (int i = 0; i <= steps; i++)
        {
            float a = (a0 + (a1 - a0) * i / steps) * Degree;
            points[i] = V(x + MathF.Cos(a) * r, y + MathF.Sin(a) * r, 0);
        }
        return points;
    }

    private static Vector3[] Line(params float[] xy)
    {
        var points = new Vector3[xy.Length / 2];
        for (int i = 0; i < points.Length; i++) points[i] = V(xy[2 * i], xy[2 * i + 1], 0);
        return points;
    }

    private static Vector3[] Curve(params float[] xy) => Smooth(4, Line(xy));

    private static Vector3[] Then(Vector3[] a, Vector3[] b) => a.Concat(b.Skip(1)).ToArray();

    /// <summary>A letter of the Unown: where its eye is, whether the eye is half shut (the signs), and its strokes.</summary>
    private sealed record UnownGlyph(Vector2 Eye, bool Lidded, Vector3[][] Strokes);

    private static UnownGlyph G(float x, float y, params Vector3[][] strokes) => new(new Vector2(x, y), false, strokes);

    /// <summary>
    /// Every letter but A, which <see cref="Unown()"/> builds: a great eye in a black ring, and strokes of the same
    /// black making the letter round it, all in the front plane.
    /// </summary>
    private static readonly Dictionary<string, UnownGlyph> UnownGlyphs = new()
    {
        ["B"] = G(0, 0.56f, Line(0, 0.68f, 0.01f, 0.75f), Line(0.12f, 0.6f, 0.18f, 0.62f), Then(Line(0, 0.44f, 0, 0.36f), Arc(0, 0.25f, 0.11f, 90, 450, 16)), Line(-0.02f, 0.38f, -0.09f, 0.39f)),
        ["C"] = G(0.04f, 0.46f, Arc(0, 0.46f, 0.25f, 70, 310, 16), Line(-0.05f, 0.57f, -0.11f, 0.66f), Curve(0.15f, 0.48f, 0.22f, 0.5f, 0.24f, 0.56f)),
        ["D"] = G(-0.04f, 0.45f, Then(Line(-0.04f, 0.57f, -0.04f, 0.72f), Arc(-0.04f, 0.45f, 0.27f, 90, -90, 14))),
        ["E"] = G(0, 0.45f, Line(-0.12f, 0.45f, -0.2f, 0.46f), Line(0.12f, 0.45f, 0.19f, 0.44f), Curve(0, 0.57f, 0, 0.66f, 0.04f, 0.71f, 0.19f, 0.71f), Curve(0, 0.33f, 0, 0.24f, 0.04f, 0.19f, 0.19f, 0.19f)),
        ["F"] = G(-0.05f, 0.58f, Line(0.07f, 0.62f, 0.27f, 0.64f, 0.1f, 0.5f, 0.06f, 0.53f), Line(-0.06f, 0.46f, -0.08f, 0.2f), Line(-0.07f, 0.34f, 0.02f, 0.36f), Line(-0.08f, 0.21f, -0.14f, 0.14f), Line(-0.08f, 0.21f, -0.01f, 0.15f)),
        ["G"] = G(0, 0.5f, Curve(-0.08f, 0.6f, -0.08f, 0.72f, 0.0f, 0.75f, 0.11f, 0.73f, 0.1f, 0.61f), Line(-0.02f, 0.38f, 0.06f, 0.31f, -0.1f, 0.24f, 0.03f, 0.13f)),
        ["H"] = G(0, 0.45f, Arc(0, 0.45f, 0.27f, 125, 235, 10), Arc(0, 0.45f, 0.27f, -55, 55, 10), Line(-0.12f, 0.45f, -0.27f, 0.45f), Line(0.12f, 0.45f, 0.27f, 0.45f)),
        ["I"] = G(0, 0.45f, Line(0, 0.57f, 0.02f, 0.75f), Line(0, 0.33f, -0.02f, 0.14f)),
        ["J"] = G(0.04f, 0.56f, Line(0.13f, 0.64f, 0.21f, 0.72f), Curve(-0.03f, 0.44f, -0.05f, 0.27f, -0.07f, 0.17f, -0.14f, 0.13f, -0.19f, 0.18f)),
        ["K"] = G(-0.05f, 0.45f, Line(-0.05f, 0.57f, -0.03f, 0.75f), Line(-0.05f, 0.33f, -0.07f, 0.14f), Line(0.06f, 0.5f, 0.2f, 0.62f), Line(0.06f, 0.4f, 0.2f, 0.27f)),
        ["L"] = G(0, 0.58f, Line(-0.07f, 0.68f, -0.12f, 0.76f), Curve(-0.01f, 0.46f, -0.03f, 0.22f, 0.01f, 0.17f, 0.15f, 0.17f)),
        ["M"] = G(0, 0.36f, Then(Then(Line(-0.25f, 0.17f, -0.263f, 0.324f), Arc(0, 0.42f, 0.28f, 200, -20, 16)), Line(0.263f, 0.324f, 0.25f, 0.17f)), Line(0, 0.48f, 0, 0.7f)),
        ["N"] = G(0, 0.45f, Line(-0.24f, 0.16f, -0.15f, 0.73f, -0.07f, 0.55f), Line(0.07f, 0.35f, 0.14f, 0.17f, 0.24f, 0.74f)),
        ["O"] = G(0, 0.4f, Arc(0, 0.46f, 0.28f, -90, 270, 24), Line(0, 0.28f, 0, 0.18f)),
        ["P"] = G(0.06f, 0.58f, Line(-0.06f, 0.58f, -0.1f, 0.4f, -0.13f, 0.15f), Line(-0.02f, 0.69f, -0.08f, 0.77f)),
        ["Q"] = G(-0.04f, 0.56f, Line(0.05f, 0.46f, 0.21f, 0.24f), Line(0.14f, 0.29f, 0.24f, 0.32f)),
        ["R"] = G(0.04f, 0.58f, Line(-0.08f, 0.6f, -0.11f, 0.4f, -0.14f, 0.15f), Line(0.1f, 0.48f, 0.21f, 0.35f)),
        ["S"] = G(0.02f, 0.45f, Curve(-0.07f, 0.55f, -0.14f, 0.63f, -0.08f, 0.71f, 0.03f, 0.73f), Curve(0.1f, 0.35f, 0.15f, 0.26f, 0.08f, 0.19f, -0.06f, 0.19f)),
        ["T"] = G(0, 0.3f, Line(0, 0.42f, 0, 0.69f), Line(-0.19f, 0.71f, 0.19f, 0.71f)),
        ["U"] = G(0, 0.56f, Arc(0, 0.47f, 0.27f, 180, 360, 16), Line(-0.07f, 0.45f, -0.17f, 0.27f), Line(0, 0.43f, 0, 0.21f), Line(0.07f, 0.45f, 0.17f, 0.27f), Line(0, 0.68f, 0, 0.73f)),
        ["V"] = G(0, 0.3f, Curve(-0.07f, 0.41f, -0.14f, 0.54f, -0.12f, 0.68f, 0.0f, 0.73f, 0.12f, 0.67f, 0.1f, 0.55f, 0.06f, 0.42f), Line(0.12f, 0.67f, 0.2f, 0.73f)),
        ["W"] = G(0, 0.33f, Line(0, 0.46f, 0.01f, 0.73f), Curve(-0.12f, 0.38f, -0.2f, 0.47f, -0.22f, 0.6f, -0.18f, 0.7f), Curve(0.12f, 0.38f, 0.2f, 0.47f, 0.22f, 0.6f, 0.18f, 0.7f)),
        ["X"] = G(0, 0.45f, Line(-0.09f, 0.54f, -0.23f, 0.69f), Line(0.09f, 0.54f, 0.23f, 0.69f), Line(-0.09f, 0.36f, -0.23f, 0.2f), Line(0.09f, 0.36f, 0.23f, 0.2f)),
        ["Y"] = G(0, 0.52f, Line(-0.08f, 0.62f, -0.16f, 0.73f), Line(0.08f, 0.62f, 0.17f, 0.73f), Line(0, 0.39f, 0, 0.2f), Line(0, 0.21f, -0.07f, 0.14f), Line(0, 0.21f, 0.07f, 0.14f)),
        ["Z"] = G(0.02f, 0.45f, Curve(-0.06f, 0.56f, -0.1f, 0.68f, 0.0f, 0.72f), Line(0.08f, 0.34f, 0.14f, 0.21f, 0.21f, 0.19f)),
        ["EXCLAMATION"] = new(new Vector2(0, 0.28f), true, new[] { Line(0, 0.41f, 0, 0.76f) }),
        ["QUESTION"] = new(new Vector2(0, 0.28f), true, new[] { Curve(0, 0.41f, 0, 0.5f, 0.09f, 0.58f, 0.1f, 0.68f, 0.03f, 0.75f, -0.07f, 0.71f) })
    };

    /// <param name="letter">The letter, or EXCLAMATION or QUESTION, as the form's name spells it in capitals.</param>
    private static PokeBuilder Unown(string letter)
    {
        var glyph = UnownGlyphs[letter];
        string name = letter.Length == 1 ? letter : letter[0] + letter[1..].ToLowerInvariant();
        var b = new PokeBuilder("Unown-" + name, 0.55f, BodyPlan.Floating, V(glyph.Eye.X, glyph.Eye.Y, 0)) { Coat = Fur }.Hover();
        var black = Rgb(40, 40, 46);
        var eye = V(glyph.Eye.X, glyph.Eye.Y, 0);

        b.Torus(Body, eye, 0.095f, 0.035f, black, V(90f, 0, 0), blend: 0.01f);
        b.Ell(Body, eye + V(0, 0, -0.012f), V(0.1f, 0.1f, 0.02f), black, blend: 0.006f);
        b.Ell(Body, eye + V(0, 0, 0.006f), V(0.1f, 0.1f, 0.018f), White, blend: 0.006f);
        foreach (var stroke in glyph.Strokes) b.Tube(Body, stroke, 0.026f, 0.023f, black);
        b.Eye(Body, eye + V(0, 0, 0.023f), V(0, 0, 1f), 0.05f, sclera: true, pupil: black, glare: glyph.Lidded);
        return Lift(b);
    }

    // ------------------------------------------------------------------ Castform's weathers (its build in PokemonModels.Hoenn3.cs)

    private static PokeBuilder CastformSunny() => CastformBuild(1);

    private static PokeBuilder CastformRainy() => CastformBuild(2);

    private static PokeBuilder CastformSnowy() => CastformBuild(3);

    // ------------------------------------------------------------------ Deoxys's formes (its build in PokemonModels.Hoenn3.cs)

    private static PokeBuilder DeoxysAttack() => DeoxysBuild(1);

    private static PokeBuilder DeoxysDefense() => DeoxysBuild(2);

    private static PokeBuilder DeoxysSpeed() => DeoxysBuild(3);

    // ------------------------------------------------------------------ Shaymin's Sky Forme

    /// <summary>
    /// Shaymin in its Sky Forme: a slender little white deer, its legs green from the knee, a tuft of green on its
    /// crown, great white ears spread from the sides of its head like wings of feathers, a scarf of two red leaves round
    /// its neck, green eyes and a short white tail.
    /// </summary>
    private static PokeBuilder ShayminSky()
    {
        var b = new PokeBuilder("Shaymin-Sky", 0.6f, BodyPlan.Quadruped, V(0, 0.27f, 0)) { Coat = Fur };
        var white = Rgb(240, 242, 244);
        var green = Rgb(112, 192, 92);
        var red = Rgb(214, 70, 64);
        BeastLegs(b, 0.04f, 0.25f, 0.07f, -0.08f, 0.026f, white, green, green);
        var bc = V(0, 0.27f, -0.005f);
        b.Ell(Body, bc, V(0.065f, 0.06f, 0.11f), white);
        var neck = bc + V(0, 0.12f, 0.1f);
        b.Limb(Body, bc + V(0, 0.03f, 0.07f), neck, 0.04f, 0.032f, white);
        // The scarf: a band of red round the neck and two red leaves fluttering from it to one side
        b.Ell(Body, bc + V(0, 0.075f, 0.085f), V(0.044f, 0.018f, 0.04f), red, V(-30f, 0, 0), blend: 0.01f);
        var knot = bc + V(0.035f, 0.07f, 0.09f);
        Frond(b, Body, knot, knot + V(0.09f, -0.03f, -0.03f), 0.022f, red, V(0, 1f, 0.3f), 0.25f, Leaf);
        Frond(b, Body, knot, knot + V(0.08f, 0.02f, -0.07f), 0.02f, red, V(0, 1f, 0.3f), 0.25f, Leaf);
        int head = b.Head(neck);
        var c = neck + V(0, 0.05f, 0.01f);
        var r = V(0.048f, 0.048f, 0.05f);
        b.Ell(head, c, r, white);
        b.Ell(head, c + V(0, -0.015f, 0.045f), V(0.026f, 0.022f, 0.03f), white, blend: 0.02f);
        b.Mark(head, c + V(0, -0.008f, 0.075f), V(0, 0.2f, 1f), 0.007f, 0.005f, Rgb(50, 50, 56));
        // The tuft of green on its crown
        foreach (var (x, up, back) in new[] { (-0.02f, 0.06f, 0.03f), (0f, 0.075f, 0.01f), (0.02f, 0.06f, 0.03f), (0f, 0.05f, 0.06f) })
            b.Spike(head, c + V(x, 0.035f, -0.01f), c + V(x * 2f, 0.035f + up, -0.01f - back), 0.022f, green, 0.55f, Leaf);
        PokeBuilder.Both(s =>
        {
            // The ears: broad white fans spread out and up, cut into three feathered points
            var root = c + V(0.035f * s, 0.02f, -0.015f);
            int ear = b.Ear(head, s, root);
            Frond(b, ear, root, root + V(0.12f * s, 0.05f, -0.03f), 0.04f, white, V(0, 0.4f, 1f), 0.2f);
            foreach (var d in new[] { V(0.17f * s, 0.11f, -0.04f), V(0.19f * s, 0.05f, -0.04f), V(0.16f * s, -0.01f, -0.03f) })
                Blade(b, ear, root + V(0.08f * s, 0.035f, -0.02f), root + d, 0.022f, white, V(0, 0.4f, 1f));
            var at = Out(c, r, default, V(0.55f * s, 0.2f, 0.8f));
            b.Eye(head, at, Outward(c, r, at), 0.012f, Rgb(64, 170, 96));
        });
        int tail = b.Tail(bc + V(0, 0.02f, -0.1f));
        foreach (float x in new[] { -0.02f, 0f, 0.02f })
            b.Spike(tail, bc + V(0, 0.02f, -0.09f), bc + V(x * 2f, 0.06f, -0.17f), 0.022f, white, 0.6f);
        return b;
    }

    // ------------------------------------------------------------------ Arceus's types (its build and its plates in PokemonModels.Sinnoh5.cs)

    /// <summary>Arceus holding the plate of one type, from the form's name (ARCEUS-FIRE), or null for a name that names no plate.</summary>
    private static PokeBuilder? ArceusForm(string form)
    {
        int i = Array.FindIndex(ArceusPlates, p => p.Type != null && ("ARCEUS-" + p.Type).Equals(form, StringComparison.OrdinalIgnoreCase));
        return i < 0 ? null : ArceusBuild(i);
    }
}
