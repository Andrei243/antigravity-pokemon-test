using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Audio;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// What the field sounds like where the player stands (plan 05 · A7), with no drawing or audio device: the
/// weather's bed (rain, wind, a blizzard, a sandstorm), a cave's rumble, and the water round them, each from the
/// side of the screen it is on. Water is heard by its tiles' behaviour within <see cref="Reach"/> tiles, each tile
/// counting less the further off it is, so a waterfall grows as the player walks up to it and a shore is louder
/// the more of the view is sea. Sounds of a place are panned the same way (<see cref="PanOf"/>).
/// </summary>
public static class FieldAmbience
{
    /// <summary>How far water is heard, in tiles.</summary>
    public const int Reach = 9;

    /// <summary>Half the screen's width in tiles: a sound this far to the side is heard from that side alone.</summary>
    public const float HalfView = 8f;

    /// <summary>How far a sound of the field leans to its side at the screen's edge (never wholly out of one ear).</summary>
    public const float PanReach = 0.8f;

    /// <summary>The pan of something <paramref name="dx"/> tiles to the right of the player (negative: to the left).</summary>
    public static float PanOf(float dx) => Math.Clamp(dx / HalfView, -1f, 1f) * PanReach;

    /// <summary>A tile's weight in what is heard: 1 underfoot, a fifth at five tiles.</summary>
    private static float Near(int dx, int dy)
    {
        float d = MathF.Sqrt(dx * dx + dy * dy) / 2.5f;
        return 1f / (1f + d * d);
    }

    /// <summary>The weather's bed and how strong, or null for weather that makes no sound (clear, clouds, fog, ash).</summary>
    public static (AmbienceBed Bed, float Gain)? OfWeather(FieldWeather weather) => weather switch
    {
        FieldWeather.Rain => (AmbienceBed.Rain, 0.85f),
        FieldWeather.HeavyRain => (AmbienceBed.HeavyRain, 0.9f),
        FieldWeather.Thunderstorm => (AmbienceBed.HeavyRain, 1f),
        FieldWeather.Hail => (AmbienceBed.Hail, 0.85f),
        FieldWeather.Snow => (AmbienceBed.Wind, 0.4f),
        FieldWeather.HeavySnow => (AmbienceBed.Wind, 0.75f),
        FieldWeather.Blizzard => (AmbienceBed.Blizzard, 0.95f),
        FieldWeather.Sandstorm => (AmbienceBed.Sandstorm, 0.9f),
        _ => null
    };

    /// <summary>Every bed heard at a tile of a map, loudest first.</summary>
    public static List<AmbienceLayer> Around(Map map, int x, int y)
    {
        var layers = new List<AmbienceLayer>();
        if (OfWeather(map.WeatherAt(x, y)) is var (bed, gain)) layers.Add(new AmbienceLayer(bed, gain, 0f));
        if (map.IsCave) layers.Add(new AmbienceLayer(AmbienceBed.Cave, 0.8f, 0f));

        if (!map.IsIndoors)
        {
            // The water round the player, by kind: how much of it, weighted by nearness, and on which side
            Span<float> sum = stackalloc float[3], side = stackalloc float[3];
            for (int dy = -Reach; dy <= Reach; dy++)
                for (int dx = -Reach; dx <= Reach; dx++)
                {
                    int tx = x + dx, ty = y + dy;
                    if (tx < 0 || ty < 0 || tx >= map.Width || ty >= map.Height || dx * dx + dy * dy > Reach * Reach) continue;
                    int kind = map.BehaviourAt(tx, ty) switch
                    {
                        TileBehavior.Waterfall => 0,
                        TileBehavior.River => 1,
                        TileBehavior.Sea => 2,
                        _ => -1
                    };
                    if (kind < 0) continue;
                    float w = Near(dx, dy);
                    sum[kind] += w;
                    side[kind] += w * dx;
                }
            // A waterfall of a few tiles roars at full a step or two away; a river's one-tile line half-fills the
            // sum of a waterfall; the open sea fills about fifty, a lake's shore a third of that
            Add(layers, AmbienceBed.Waterfall, sum[0] / 3f, sum[0], side[0]);
            Add(layers, AmbienceBed.River, sum[1] / 5f, sum[1], side[1]);
            Add(layers, AmbienceBed.Waves, (sum[2] - 2f) / 30f, sum[2], side[2]);
        }
        layers.Sort((a, b) => b.Gain.CompareTo(a.Gain));
        return layers;
    }

    private static void Add(List<AmbienceLayer> layers, AmbienceBed bed, float gain, float sum, float side)
    {
        gain = Math.Clamp(gain, 0f, 1f);
        if (gain < 0.03f) return;
        layers.Add(new AmbienceLayer(bed, gain, PanOf(side / sum)));
    }
}
