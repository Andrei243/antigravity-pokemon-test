using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Graphics.OpenWorld;

/// <summary>A piece of weather in the air: a streak or a flake from its head to its tail, its width and colour, in metres.</summary>
internal readonly record struct WeatherPiece(Vector3 Head, Vector3 Tail, float Width, Color Colour);

/// <summary>
/// What falls or blows in the open world's air (plan 32 · OW8; style guide, "Open world", "Weather in the air"): pieces
/// in a box round the camera, <see cref="Across"/> metres across and <see cref="Tall"/> tall, each moving through the
/// world by its weather's way and coming round again on the box's far side, so nothing slides with the camera. Each
/// piece is a pure function of its number, its weather, the camera's place and the time, as the field's
/// <c>WeatherFx</c> is. GPU-free.
/// </summary>
internal static class OpenWeatherFx
{
    public const float Across = 24f, Tall = 16f;

    /// <summary>How near the camera a piece may be, in metres: nearer, one streak would fill the picture.</summary>
    public const float Nearest = 1.8f;

    /// <summary>How many pieces a weather keeps in the air round the camera.</summary>
    public static int CountOf(FieldWeather weather) => weather switch
    {
        FieldWeather.Rain => 2400,
        FieldWeather.HeavyRain or FieldWeather.Thunderstorm => 4400,
        FieldWeather.Snow => 1600,
        FieldWeather.HeavySnow => 3000,
        FieldWeather.Blizzard => 4000,
        FieldWeather.Hail => 1400,
        FieldWeather.Sandstorm => 2600,
        FieldWeather.Ash => 900,
        _ => 0
    };

    /// <summary>A weather's way through the air (metres a second), a piece's length along it, its width and its colour.</summary>
    private static (Vector3 Velocity, float Length, float Width, Color Colour, float Sway) KindOf(FieldWeather weather) => weather switch
    {
        FieldWeather.Rain or FieldWeather.HeavyRain or FieldWeather.Thunderstorm => (new Vector3(0.9f, -9f, 0.2f), 0.5f, 0.012f, new Color(196, 214, 236, 128), 0f),
        FieldWeather.Snow or FieldWeather.HeavySnow => (new Vector3(0.25f, -1.1f, 0.1f), 0.05f, 0.05f, new Color(240, 244, 252, 220), 0.3f),
        FieldWeather.Blizzard => (new Vector3(3.2f, -4f, 0.8f), 0.12f, 0.05f, new Color(236, 242, 252, 210), 0.3f),
        FieldWeather.Hail => (new Vector3(0.4f, -12f, 0.1f), 0.06f, 0.03f, new Color(226, 236, 248, 230), 0f),
        FieldWeather.Sandstorm => (new Vector3(8f, -0.4f, 1.2f), 0.6f, 0.02f, new Color(214, 190, 140, 150), 0f),
        FieldWeather.Ash => (new Vector3(0.1f, -0.5f, 0.05f), 0.04f, 0.04f, new Color(120, 112, 110, 200), 0.2f),
        _ => (Vector3.Zero, 0f, 0f, default, 0f)
    };

    private static float Rand(int i, int salt) => GroundBaker.Rand01(i, salt, 977);

    /// <summary>A coordinate brought into the box's span round its middle: where a piece that has left the box comes in again.</summary>
    private static float Round(float value, float middle, float span)
    {
        float low = middle - span / 2f;
        float t = (value - low) % span;
        if (t < 0) t += span;
        return low + t;
    }

    /// <summary>
    /// The pieces in the air round <paramref name="camera"/> at <paramref name="time"/> seconds, <paramref name="share"/>
    /// of the weather's count (the cave's share takes them away), into a list of the caller's, cleared first. Where
    /// <paramref name="shelter"/> is given, a piece under its height at the piece's place (the first ground the sky meets
    /// there) is left out: nothing falls in a tunnel or inside a hill.
    /// </summary>
    public static void Pieces(FieldWeather weather, Vector3 camera, double time, float share, List<WeatherPiece> into,
        Func<float, float, float>? shelter = null)
    {
        into.Clear();
        int count = (int)(CountOf(weather) * Math.Clamp(share, 0f, 1f));
        if (count <= 0) return;
        var (velocity, length, width, colour, sway) = KindOf(weather);
        var along = Vector3.Normalize(velocity);
        for (int i = 0; i < count; i++)
        {
            float speed = 0.85f + 0.3f * Rand(i, 3);
            // Its place when the clock began, then where its way has taken it since, wrapped round the box (in double, so hours of clock don't jitter)
            double t = time + Rand(i, 4) * 10.0;
            float x = (float)((Rand(i, 0) * Across + velocity.X * speed * t) % Across);
            float y = (float)((Rand(i, 1) * Tall + velocity.Y * speed * t) % Tall);
            float z = (float)((Rand(i, 2) * Across + velocity.Z * speed * t) % Across);
            if (sway > 0f)
            {
                x += sway * MathF.Sin((float)(t * 1.3 % (Math.PI * 2)) + i);
                z += sway * MathF.Cos((float)(t * 1.1 % (Math.PI * 2)) + i * 0.7f);
            }
            var head = new Vector3(Round(x, camera.X, Across), Round(y, camera.Y, Tall), Round(z, camera.Z, Across));
            if (Vector3.DistanceSquared(head, camera) < Nearest * Nearest) continue;
            if (shelter != null && head.Y < shelter(head.X, head.Z)) continue;
            into.Add(new WeatherPiece(head, head - along * length, width, colour));
        }
    }
}
