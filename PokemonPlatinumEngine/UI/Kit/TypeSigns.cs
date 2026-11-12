using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>What a part of a sign is.</summary>
public enum SignPartKind { Circle, Ring, Line, Box, Triangle }

/// <summary>
/// One part of a type's sign, in the sign's own square from −1 to 1: a circle (<see cref="A"/> its middle, <see cref="Size"/>
/// its radius), a ring (the same, <see cref="Width"/> thick), a round-ended line from <see cref="A"/> to <see cref="B"/>
/// <see cref="Width"/> thick, a rounded box (<see cref="A"/> its middle, <see cref="B"/> its half size, <see cref="Size"/> its
/// corners' radius) or a triangle (<see cref="A"/>, <see cref="B"/>, <see cref="C"/>). <see cref="Cut"/> draws it in the
/// colour behind the sign, cutting it out of what came before.
/// </summary>
public readonly record struct SignPart(SignPartKind Kind, Vector2 A, Vector2 B, Vector2 C, float Size, float Width, bool Cut)
{
    /// <summary>The smallest box that holds the part, in the sign's square.</summary>
    public (Vector2 Min, Vector2 Max) Extent => Kind switch
    {
        SignPartKind.Circle or SignPartKind.Ring => (A - new Vector2(Size), A + new Vector2(Size)),
        SignPartKind.Line => (Vector2.Min(A, B) - new Vector2(Width / 2f), Vector2.Max(A, B) + new Vector2(Width / 2f)),
        SignPartKind.Box => (A - B, A + B),
        _ => (Vector2.Min(A, Vector2.Min(B, C)) - new Vector2(TypeSigns.Rounding), Vector2.Max(A, Vector2.Max(B, C)) + new Vector2(TypeSigns.Rounding))
    };
}

/// <summary>
/// The nineteen types' signs (Sound, plan 46 · CP2, the last) (plan 12 · Q8; style guide, "Shapes and components", "Type signs"): our own one-colour
/// signs, drawn on a type pill and a move card's band when a colour remap or high contrast is in force, so a type
/// reads without its colour. The parts are data (<see cref="Parts"/>, GPU-free, which a test holds inside their square);
/// <see cref="Draw"/> lays them down with <see cref="UiShapes"/>.
/// </summary>
public static class TypeSigns
{
    /// <summary>How far a triangle's corners are rounded out, in the sign's square.</summary>
    public const float Rounding = 0.04f;

    private static SignPart Circle(float x, float y, float r, bool cut = false) => new(SignPartKind.Circle, new(x, y), default, default, r, 0f, cut);
    private static SignPart Ring(float x, float y, float r, float w) => new(SignPartKind.Ring, new(x, y), default, default, r, w, false);
    private static SignPart Line(float x0, float y0, float x1, float y1, float w, bool cut = false) => new(SignPartKind.Line, new(x0, y0), new(x1, y1), default, 0f, w, cut);
    private static SignPart Box(float x, float y, float hx, float hy, float r) => new(SignPartKind.Box, new(x, y), new(hx, hy), default, r, 0f, false);
    private static SignPart Tri(float x0, float y0, float x1, float y1, float x2, float y2) => new(SignPartKind.Triangle, new(x0, y0), new(x1, y1), new(x2, y2), 0f, 0f, false);

    /// <summary>A type's sign as its parts, in the order they are drawn.</summary>
    public static IReadOnlyList<SignPart> Parts(PokemonType type) => type switch
    {
        PokemonType.Normal => new[] { Circle(0, 0, 0.56f) },
        PokemonType.Fire => new[]
        {
            Circle(0, 0.42f, 0.5f),
            Tri(0, -0.94f, -0.44f, 0.3f, 0.44f, 0.3f),
            Tri(-0.64f, -0.42f, -0.78f, 0.42f, -0.16f, 0.36f),
            Tri(0.64f, -0.42f, 0.16f, 0.36f, 0.78f, 0.42f)
        },
        PokemonType.Water => new[] { Tri(0, -0.94f, -0.56f, 0.2f, 0.56f, 0.2f), Circle(0, 0.32f, 0.6f) },
        PokemonType.Grass => new[]
        {
            // A leaf pointed at both ends, wide in its middle, on a short stem
            Line(-0.86f, 0.86f, -0.5f, 0.5f, 0.16f),
            Circle(0, 0, 0.42f),
            Tri(0.82f, -0.82f, 0.3f, 0.3f, -0.3f, -0.3f),
            Tri(-0.64f, 0.64f, 0.3f, 0.3f, -0.3f, -0.3f),
            Line(-0.42f, 0.42f, 0.5f, -0.5f, 0.09f, cut: true)
        },
        PokemonType.Electric => new[] { Tri(0.24f, -0.94f, -0.62f, 0.14f, 0.12f, 0.12f), Tri(-0.12f, -0.12f, 0.62f, -0.14f, -0.24f, 0.94f) },
        PokemonType.Ice => new[]
        {
            Line(0, -0.84f, 0, 0.84f, 0.2f),
            Line(-0.73f, -0.42f, 0.73f, 0.42f, 0.2f),
            Line(-0.73f, 0.42f, 0.73f, -0.42f, 0.2f)
        },
        PokemonType.Fighting => new[]
        {
            Box(0, 0.1f, 0.68f, 0.62f, 0.26f),
            Line(-0.3f, -0.5f, -0.3f, -0.06f, 0.1f, cut: true),
            Line(0.06f, -0.5f, 0.06f, -0.06f, 0.1f, cut: true),
            Line(0.4f, -0.5f, 0.4f, -0.06f, 0.1f, cut: true)
        },
        PokemonType.Poison => new[] { Circle(-0.34f, 0.36f, 0.5f), Circle(0.44f, -0.1f, 0.36f), Circle(-0.06f, -0.62f, 0.26f) },
        PokemonType.Ground => new[]
        {
            Line(-0.84f, 0.62f, 0.84f, 0.62f, 0.26f),
            Line(-0.56f, 0.04f, 0.56f, 0.04f, 0.26f),
            Line(-0.26f, -0.54f, 0.26f, -0.54f, 0.26f)
        },
        PokemonType.Flying => new[]
        {
            Line(-0.66f, 0.64f, 0.78f, -0.76f, 0.24f),
            Line(-0.66f, 0.64f, 0.84f, -0.16f, 0.24f),
            Line(-0.66f, 0.64f, 0.7f, 0.46f, 0.24f)
        },
        PokemonType.Psychic => new[] { Ring(0, 0, 0.82f, 0.2f), Circle(0, 0, 0.3f) },
        PokemonType.Bug => new[]
        {
            Circle(0, 0.3f, 0.56f),
            Circle(0, -0.46f, 0.3f),
            Line(-0.12f, -0.68f, -0.46f, -0.9f, 0.1f),
            Line(0.12f, -0.68f, 0.46f, -0.9f, 0.1f)
        },
        PokemonType.Rock => new[]
        {
            Tri(-0.86f, 0.72f, -0.36f, -0.76f, 0.86f, 0.72f),
            Tri(-0.36f, -0.76f, 0.58f, -0.34f, 0.86f, 0.72f),
            Line(-0.2f, -0.3f, 0.1f, 0.3f, 0.1f, cut: true)
        },
        PokemonType.Ghost => new[]
        {
            Circle(0, -0.22f, 0.62f),
            Box(0, 0.32f, 0.62f, 0.5f, 0.12f),
            Circle(-0.24f, -0.24f, 0.13f, cut: true),
            Circle(0.24f, -0.24f, 0.13f, cut: true)
        },
        PokemonType.Dragon => new[]
        {
            Line(-0.72f, 0.78f, -0.36f, -0.78f, 0.2f),
            Line(-0.12f, 0.78f, 0.24f, -0.78f, 0.2f),
            Line(0.46f, 0.78f, 0.8f, -0.6f, 0.2f)
        },
        PokemonType.Steel => new[]
        {
            Line(0, -0.84f, 0, 0.84f, 0.3f),
            Line(-0.73f, -0.42f, 0.73f, 0.42f, 0.3f),
            Line(-0.73f, 0.42f, 0.73f, -0.42f, 0.3f),
            Circle(0, 0, 0.6f),
            Circle(0, 0, 0.24f, cut: true)
        },
        PokemonType.Dark => new[] { Circle(0, 0, 0.78f), Circle(0.36f, -0.26f, 0.62f, cut: true) },
        PokemonType.Fairy => new[]
        {
            Tri(0, -0.94f, -0.22f, 0, 0.22f, 0),
            Tri(0, 0.94f, 0.22f, 0, -0.22f, 0),
            Tri(-0.94f, 0, 0, 0.22f, 0, -0.22f),
            Tri(0.94f, 0, 0, -0.22f, 0, 0.22f)
        },
        PokemonType.Sound => new[]
        {
            // A note (plan 46 · CP2): a round head leaning left, its stem rising from its right, a flag swept off the top
            Circle(-0.26f, 0.5f, 0.38f),
            Line(0.06f, 0.46f, 0.06f, -0.8f, 0.2f),
            Line(0.06f, -0.8f, 0.62f, -0.42f, 0.2f),
            Line(0.62f, -0.42f, 0.52f, -0.06f, 0.16f)
        },
        _ => Array.Empty<SignPart>()
    };

    /// <summary>Draws a type's sign about 2 × <paramref name="s"/> across round <paramref name="c"/>, in <paramref name="ink"/>, its cut-outs in <paramref name="back"/>.</summary>
    public static void Draw(PokemonType type, Vector2 c, float s, Color ink, Color back)
    {
        foreach (var part in Parts(type))
        {
            var colour = part.Cut ? back : ink;
            switch (part.Kind)
            {
                case SignPartKind.Circle:
                    UiShapes.Circle(c + part.A * s, part.Size * s, colour);
                    break;
                case SignPartKind.Ring:
                    UiShapes.Ring(c + part.A * s, part.Size * s, part.Width * s, colour);
                    break;
                case SignPartKind.Line:
                    UiShapes.Line(c + part.A * s, c + part.B * s, part.Width * s, colour);
                    break;
                case SignPartKind.Box:
                    var mid = c + part.A * s;
                    UiShapes.Fill(new Rectangle(mid.X - part.B.X * s, mid.Y - part.B.Y * s, part.B.X * 2f * s, part.B.Y * 2f * s), part.Size * s, colour);
                    break;
                default:
                    UiShapes.Triangle(c + part.A * s, c + part.B * s, c + part.C * s, colour, Rounding * s);
                    break;
            }
        }
    }
}
