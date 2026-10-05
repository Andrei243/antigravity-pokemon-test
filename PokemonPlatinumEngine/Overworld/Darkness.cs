using System;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// The dark of a cave nobody has lit (plan 01 · M5; style guide, "Caves"): the player sees a circle of the floor
/// round themselves and nothing beyond it, until a Pokémon of theirs that knows Flash lights the place. Only the
/// rule and its numbers are here; <c>WorldRenderer</c> draws it over the picture.
/// </summary>
public static class Darkness
{
    /// <summary>How far from the player the cave is seen as it is, in tiles.</summary>
    public const float Radius = 2.2f;

    /// <summary>The tiles beyond that over which the light gives out.</summary>
    public const float Soft = 1.2f;

    /// <summary>Where nothing is seen at all: <see cref="Radius"/> and <see cref="Soft"/> together.</summary>
    public const float Reach = Radius + Soft;

    /// <summary>What the picture falls to in the dark.</summary>
    public static readonly (byte R, byte G, byte B) Colour = (8, 6, 12);

    /// <summary>
    /// Whether the dark closes round whoever walks a map: it is a dark cave, and none of their Pokémon knows
    /// Flash. (As with Surf, knowing the move is all that is asked until plan 02 · S2.)
    /// </summary>
    public static bool Covers(Map map, FieldMoves moves) => map.IsDark && !moves.HasFlag(FieldMoves.Flash);

    /// <summary>How dark it is a number of tiles from the player: 0 where the cave is seen, 1 where nothing is.</summary>
    public static float At(float tiles)
    {
        float t = Math.Clamp((tiles - Radius) / Soft, 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
