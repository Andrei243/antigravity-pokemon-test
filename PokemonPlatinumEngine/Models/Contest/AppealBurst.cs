using System;
using System.Collections.Generic;
using System.Numerics;

namespace PokemonPlatinumEngine.Models.Contest;

/// <summary>The shape a spark of an appeal is drawn as.</summary>
public enum SparkShape { Star, Heart, Ring, Shard, Glint }

/// <summary>One spark of an appeal's burst: where it is from the appealer's middle (in layout units at a sprite's scale of one), its size, how much of it shows and its shape.</summary>
public readonly record struct Spark(Vector2 Offset, float Size, float Alpha, SparkShape Shape);

/// <summary>
/// What an appeal looks like on the contest's stage (plan 21 · H11, part f; style guide, "Hoenn's contests and Pokéblocks",
/// "An appeal's burst"; our own, Emerald plays each move's battle animation there): a burst of sparks round the appealer
/// in the move's own quality, shaped by it (stars for cool, rising glints for beauty, hearts for cute, rings for smart,
/// shards flung out for tough) and laid out by the move itself, so two moves of one quality differ. A pure function of
/// the quality, the time since the appeal and the move's seed (<see cref="SeedOf"/>), so the same appeal draws the same.
/// No drawing or input.
/// </summary>
public static class AppealBurst
{
    /// <summary>How long a burst lasts, in seconds.</summary>
    public const float Seconds = 1.2f;

    /// <summary>The most sparks a burst has.</summary>
    public const int MostSparks = 14;

    /// <summary>A move's seed: its name hashed by our own rule (never <see cref="string.GetHashCode()"/>, which changes from run to run).</summary>
    public static int SeedOf(string move)
    {
        uint h = 2166136261u;
        foreach (char ch in move) { h ^= ch; h *= 16777619u; }
        return (int)(h & 0x7FFFFFFF);
    }

    /// <summary>The sparks of a burst <paramref name="age"/> seconds in; none before it starts or after it ends.</summary>
    public static IReadOnlyList<Spark> Sparks(ContestStat quality, float age, int seed)
    {
        var sparks = new List<Spark>();
        if (age < 0f || age >= Seconds) return sparks;
        float u = age / Seconds;
        // Fades in over the first tenth and out over the last third
        float fade = Math.Clamp(u / 0.1f, 0f, 1f) * Math.Clamp((1f - u) / 0.33f, 0f, 1f);
        int count = 8 + (int)((uint)seed % 7u);
        float turn = ((uint)seed >> 3) % 360u * MathF.PI / 180f;
        for (int k = 0; k < count; k++)
        {
            uint r = Mix((uint)seed, (uint)k);
            float angle = turn + k * MathF.Tau / count + (r % 100u) / 100f * 0.4f;
            float jitter = 0.8f + ((r >> 8) % 100u) / 250f;
            var dir = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            switch (quality)
            {
                case ContestStat.Cool:
                    // Stars shot out fast and slowing
                    sparks.Add(new Spark(dir * (60f + 170f * Ease(u) * jitter), 18f * (1.1f - u * 0.5f), fade, SparkShape.Star));
                    break;
                case ContestStat.Beauty:
                    // Glints rising in a column and swaying
                    sparks.Add(new Spark(new Vector2(dir.X * 120f * jitter + MathF.Sin(age * 6f + k) * 14f, 60f - 220f * u * jitter),
                        12f + 6f * MathF.Abs(MathF.Sin(age * 9f + k)), fade, SparkShape.Glint));
                    break;
                case ContestStat.Cute:
                    // Hearts floating up and out
                    sparks.Add(new Spark(new Vector2(dir.X * (70f + 90f * u), -40f - 160f * u * jitter + dir.Y * 30f), 16f + 4f * u, fade, SparkShape.Heart));
                    break;
                case ContestStat.Smart:
                    // Rings round the appealer, turning as they widen
                    float a = angle + age * 2.4f;
                    sparks.Add(new Spark(new Vector2(MathF.Cos(a), MathF.Sin(a) * 0.55f) * (90f + 80f * u), 10f + 14f * u, fade * 0.9f, SparkShape.Ring));
                    break;
                default:
                    // Shards flung out and falling
                    var d = dir * (80f + 200f * u * jitter);
                    sparks.Add(new Spark(new Vector2(d.X, d.Y + 260f * u * u), 16f * (1.2f - u * 0.6f), fade, SparkShape.Shard));
                    break;
            }
        }
        return sparks;
    }

    private static float Ease(float u) => 1f - (1f - u) * (1f - u);

    private static uint Mix(uint a, uint b)
    {
        uint x = a * 0x9E3779B1u ^ (b + 0x7F4A7C15u);
        x ^= x >> 15; x *= 0x2C1B3C6Du; x ^= x >> 12; x *= 0x297A2D39u; x ^= x >> 15;
        return x;
    }
}
