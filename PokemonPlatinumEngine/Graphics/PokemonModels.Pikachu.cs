using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

// Pikachu's caps and costumes and the spiky-eared Pichu, hand-built like the species (after plan 03 · D11): Pikachu's
// own sculpt in a cap or a costume, and Pichu's with a tuft of spikes on one ear. Each is our own sculpt after the design.
internal static partial class PokemonModels
{
    private static readonly Color CapRed = Rgb(214, 48, 52);
    private static readonly Color CapWhite = Rgb(246, 246, 250);
    private static readonly Color CapBlack = Rgb(52, 50, 60);

    /// <summary>The caps Pikachu wears: the crown's colour, the front panel's, the peak's, and the badge on the front.</summary>
    private static readonly Dictionary<string, (Color crown, Color front, Color brim, Color badge, MarkShape shape)> PikachuCaps = new()
    {
        ["Pikachu-Original-Cap"] = (CapRed, CapWhite, CapRed, Rgb(52, 168, 84), MarkShape.Triangle),
        ["Pikachu-Partner-Cap"] = (CapRed, CapWhite, CapRed, Rgb(52, 168, 84), MarkShape.Wave),
        ["Pikachu-Hoenn-Cap"] = (CapRed, CapBlack, CapRed, Rgb(80, 170, 80), MarkShape.Ring),
        ["Pikachu-Sinnoh-Cap"] = (CapBlack, CapBlack, CapBlack, Rgb(70, 150, 220), MarkShape.Disc),
        ["Pikachu-Unova-Cap"] = (CapRed, CapWhite, CapBlack, Rgb(40, 100, 180), MarkShape.Ring),
        ["Pikachu-Kalos-Cap"] = (CapRed, CapRed, Rgb(214, 216, 226), CapWhite, MarkShape.Disc),
        ["Pikachu-Alola-Cap"] = (CapRed, CapRed, CapWhite, CapWhite, MarkShape.Ring),
        ["Pikachu-World-Cap"] = (CapRed, CapBlack, Rgb(96, 96, 104), CapRed, MarkShape.Ring),
    };

    /// <summary>Pikachu's caps and costumes: what <see cref="Pikachu"/> adds to its sculpt for a form.</summary>
    private static void DressPikachu(PokeBuilder b, string form, int head, int tail)
    {
        if (PikachuCaps.TryGetValue(form, out var cap))
        {
            // A cap pulled down over its crown with its ears out at the sides, the peak to the front, a badge on it
            b.Ell(head, V(0, 0.468f, -0.005f), V(0.158f, 0.075f, 0.148f), cap.crown, blend: 0.006f);
            b.Ell(head, V(0, 0.49f, 0.07f), V(0.105f, 0.058f, 0.09f), cap.front, blend: 0.004f);
            b.Ell(head, V(0, 0.458f, 0.15f), V(0.12f, 0.012f, 0.095f), cap.brim, V(-10f, 0, 0), blend: 0.004f);
            b.Ell(head, V(0, 0.542f, 0.0f), V(0.016f, 0.01f, 0.016f), cap.crown, blend: 0.004f);
            b.Mark(head, V(0, 0.505f, 0.155f), V(0, 0.45f, 1f), 0.034f, 0.028f, cap.badge, cap.shape);
            return;
        }
        // The costumes are all worn by females, whose tails end in a heart
        HeartTail(b, tail);
        var black = Rgb(36, 32, 40);
        switch (form)
        {
            case "Pikachu-Rock-Star":
            {
                // A wild grey wig, a red jacket with a fur collar, black trousers, red paint over its eyes
                var gray = Rgb(150, 150, 160);
                foreach (var (dir, len) in new[] { (V(0, 1f, -0.3f), 0.12f), (V(-0.5f, 0.9f, -0.4f), 0.11f), (V(0.5f, 0.9f, -0.4f), 0.11f), (V(-0.8f, 0.4f, -0.4f), 0.09f), (V(0.8f, 0.4f, -0.4f), 0.09f), (V(0, 0.6f, -0.9f), 0.1f), (V(-0.4f, 0.5f, -0.9f), 0.09f), (V(0.4f, 0.5f, -0.9f), 0.09f) })
                {
                    var n = Vector3.Normalize(dir);
                    var root = V(0, 0.38f, 0.01f) + n * V(0.13f, 0.11f, 0.115f);
                    b.Spike(head, root, root + n * len, 0.045f, gray, 0.5f);
                }
                b.PaintEll(Body, V(0, 0.22f, -0.01f), V(0.13f, 0.075f, 0.12f), Rgb(200, 40, 44));
                b.Torus(Body, V(0, 0.265f, 0.0f), 0.095f, 0.03f, gray, blend: 0.01f);
                b.PaintEll(Body, V(0, 0.1f, 0.0f), V(0.14f, 0.07f, 0.12f), black);
                PokeBuilder.Both(s => b.Mark(head, V(0.075f * s, 0.43f, 0.105f), V(0.45f * s, 0.3f, 1f), 0.03f, 0.012f, Rgb(214, 40, 40), MarkShape.Triangle, 20f * s));
                break;
            }
            case "Pikachu-Belle":
            {
                // A tall white bonnet lined in blue, a blue gown with a white bow and a jewel at the neck
                var blue = Rgb(44, 78, 176);
                b.Ell(head, V(0, 0.5f, -0.07f), V(0.17f, 0.2f, 0.11f), CapWhite, blend: 0.012f);
                b.Torus(head, V(0, 0.47f, -0.0f), 0.15f, 0.014f, Rgb(232, 196, 92), V(80f, 0, 0), sx: 1f, sz: 1.25f, mat: Metal, blend: 0.004f);
                b.Ell(Body, V(0, 0.12f, 0.0f), V(0.165f, 0.11f, 0.14f), blue, blend: 0.02f);
                b.PaintEll(Body, V(0, 0.12f, 0.12f), V(0.07f, 0.1f, 0.05f), Rgb(246, 236, 210));
                PokeBuilder.Both(s => b.Ell(Body, V(0.035f * s, 0.255f, 0.095f), V(0.035f, 0.022f, 0.015f), CapWhite, V(0, 0, 20f * s), blend: 0.006f));
                b.Box(Body, V(0, 0.255f, 0.112f), V(0.014f, 0.014f, 0.008f), 0.003f, Rgb(60, 110, 230), V(0, 0, 45f), Shell, 0.003f);
                break;
            }
            case "Pikachu-Pop-Star":
            {
                // A big pink bow on its head, a pink frilled dress and a bow at its chest
                var pink = Rgb(240, 110, 170);
                var head0 = V(0.08f, 0.5f, 0.0f);
                PokeBuilder.Both(s => Frond(b, head, head0, head0 + V(0.09f * s, 0.06f, 0), 0.04f, pink, V(0, 0.2f, 1f), 0.4f));
                b.Ell(head, head0, V(0.022f, 0.022f, 0.02f), pink);
                b.Ell(Body, V(0, 0.12f, 0.0f), V(0.17f, 0.06f, 0.15f), pink, blend: 0.02f);
                for (int i = 0; i < 10; i++)
                {
                    float a = i * MathF.Tau / 10f;
                    b.Ell(Body, V(MathF.Sin(a) * 0.17f, 0.08f, MathF.Cos(a) * 0.15f), V(0.04f, 0.03f, 0.04f), Rgb(250, 190, 214), blend: 0.012f);
                }
                foreach (var (x, y) in new[] { (-0.07f, 0.13f), (0.06f, 0.15f), (0.0f, 0.11f) })
                    b.Mark(Body, V(x, y, 0.15f - MathF.Abs(x) * 0.2f), V(x * 3f, 0.2f, 1f), 0.012f, 0.012f, CapWhite);
                PokeBuilder.Both(s => Frond(b, Body, V(0, 0.24f, 0.095f), V(0.05f * s, 0.25f, 0.1f), 0.022f, pink, V(0, 0, 1f), 0.4f));
                break;
            }
            case "Pikachu-Phd":
            {
                // Round glasses, a mortarboard with a tassel, brown braids tied in green, a white coat over a green vest
                var brown = Rgb(130, 90, 60);
                var green = Rgb(90, 160, 80);
                PokeBuilder.Both(s =>
                {
                    b.Torus(head, V(0.058f * s, 0.4f, 0.135f), 0.033f, 0.006f, black, V(90f, 0, 0), mat: Shell, blend: 0.003f);
                    b.Limb(head, V(0.092f * s, 0.405f, 0.13f), V(0.13f * s, 0.41f, 0.05f), 0.005f, 0.005f, black, Shell, 0.003f);
                    b.Tube(head, Smooth(3, V(0.12f * s, 0.42f, -0.02f), V(0.16f * s, 0.34f, -0.03f), V(0.15f * s, 0.24f, -0.01f), V(0.14f * s, 0.16f, 0.0f)), 0.026f, 0.02f, brown, blend: 0f);
                    b.Ell(head, V(0.14f * s, 0.2f, 0.0f), V(0.024f, 0.014f, 0.024f), green, blend: 0.004f);
                });
                b.Limb(head, V(-0.025f, 0.4f, 0.14f), V(0.025f, 0.4f, 0.14f), 0.005f, 0.005f, black, Shell, 0.003f);
                b.Ell(head, V(0, 0.49f, 0.0f), V(0.1f, 0.04f, 0.1f), black, blend: 0.008f);
                b.Box(head, V(0, 0.535f, 0.0f), V(0.14f, 0.008f, 0.14f), 0.003f, black, V(0, 45f, 0), Shell, 0.004f);
                b.Tube(head, new[] { V(0.0f, 0.545f, 0.0f), V(0.1f, 0.54f, 0.0f), V(0.13f, 0.48f, 0.0f) }, 0.005f, 0.005f, Rgb(220, 60, 60), blend: 0.003f);
                b.Ell(head, V(0.13f, 0.47f, 0.0f), V(0.016f, 0.016f, 0.016f), CapRed, mat: Shell, blend: 0.004f);
                b.Ell(Body, V(0, 0.18f, -0.01f), V(0.13f, 0.15f, 0.115f), CapWhite, blend: 0.012f);
                b.PaintEll(Body, V(0, 0.2f, 0.1f), V(0.045f, 0.09f, 0.04f), green);
                PokeBuilder.Both(s => Frond(b, Body, V(0.06f * s, 0.1f, -0.06f), V(0.12f * s, 0.0f, -0.16f), 0.05f, CapWhite, V(0, 0.4f, -1f), 0.3f));
                break;
            }
            case "Pikachu-Libre":
            {
                // A wrestler's orange mask with a white bolt on its brow, and a black suit with an orange belt
                var orange = Rgb(244, 146, 40);
                b.PaintEll(head, V(0, 0.42f, 0.06f), V(0.16f, 0.08f, 0.11f), orange);
                b.Mark(head, V(0, 0.47f, 0.115f), V(0, 0.4f, 1f), 0.016f, 0.03f, CapWhite, MarkShape.Zigzag);
                PokeBuilder.Both(s => b.Mark(head, V(0.058f * s, 0.4f, 0.124f), V(0.4f * s, 0.05f, 1f), 0.042f, 0.042f, black, MarkShape.Ring));
                b.PaintEll(Body, V(0, 0.15f, -0.01f), V(0.14f, 0.12f, 0.12f), black);
                b.PaintTorus(Body, V(0, 0.15f, 0.0f), 0.12f, 0.02f, orange, default, 1f, 0.85f);
                break;
            }
        }
    }

    /// <summary>A female Pikachu's tail: its last span cut into a heart at the end, and the heart painted black.</summary>
    private static void HeartTail(PokeBuilder b, int tail)
    {
        var tip = V(0.18f, 0.52f, -0.34f);
        b.PaintEll(tail, tip + V(-0.01f, -0.025f, 0.03f), V(0.03f, 0.075f, 0.075f), Rgb(36, 32, 40), V(-20f, 0, 0));
        b.Cut(tail, tip + V(0, 0.012f, -0.01f), V(0.03f, 0.03f, 0.022f), V(-20f, 0, 0), 0.004f);
    }

    /// <summary>The spiky-eared Pichu's ear: a tuft of black spikes standing out of the tip of its left ear.</summary>
    private static void SpikyEar(PokeBuilder b)
    {
        int ear = b.Model.Skeleton.Find("earR");
        var black = Rgb(40, 34, 36);
        var root = V(0.22f, 0.58f, -0.035f);
        foreach (var tip in new[] { V(0.33f, 0.66f, -0.04f), V(0.34f, 0.58f, -0.04f), V(0.26f, 0.7f, -0.04f) })
            b.Spike(ear, root, tip, 0.024f, black, 0.4f);
    }
}
