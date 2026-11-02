using System;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// Plan 31 · MU2: the modern sound's gentle compressor on a song's own mix, before the mixer's fade, duck and bus
/// (<c>docs/music-format.md</c>, "The compressor"). Stereo-linked and fed forward: the larger of the two sides' power,
/// smoothed by a one-pole that rises in <see cref="AttackSeconds"/> and falls in <see cref="ReleaseSeconds"/>, sets a
/// gain over a soft knee, worked out once a block and eased across it. Nothing random and no clock, so a song renders
/// to the same samples every time. The DS sound has none.
/// </summary>
public sealed class Compressor
{
    public const float ThresholdDb = -10f;
    public const float Ratio = 2f;
    public const float KneeDb = 8f;
    public const float MostReductionDb = 6f;
    public const float MakeUpDb = 0.5f;
    public const float AttackSeconds = 0.010f;
    public const float ReleaseSeconds = 0.200f;

    private readonly float attack, release;
    private float power;
    private float gain;

    /// <summary>The gain reduction last worked out, in dB (0 or less, before the make-up): how hard it is working.</summary>
    public float ReductionDb { get; private set; }

    private double reductionSum;
    private long blocks;

    /// <summary>The mean gain reduction over every block since it was made, in dB: what <c>MusicRender</c> prints.</summary>
    public float MeanDb => blocks == 0 ? 0f : (float)(reductionSum / blocks);

    /// <summary>The most it has taken, in dB, since it was made.</summary>
    public float MostDb { get; private set; }

    public Compressor(int rate)
    {
        attack = 1f - MathF.Exp(-1f / (AttackSeconds * rate));
        release = 1f - MathF.Exp(-1f / (ReleaseSeconds * rate));
        gain = Db(MakeUpDb);
    }

    /// <summary>Forgets what it has heard (a new song starting).</summary>
    public void Reset()
    {
        power = 0f;
        ReductionDb = 0f;
        gain = Db(MakeUpDb);
    }

    /// <summary>The gain, in dB, for a detector level of <paramref name="levelDb"/>, before the make-up: 0 under the knee, the ratio's slope over it, a quadratic between.</summary>
    public static float ReductionFor(float levelDb)
    {
        float over = levelDb - ThresholdDb;
        float reduction;
        if (over <= -KneeDb / 2f) reduction = 0f;
        else if (over >= KneeDb / 2f) reduction = over * (1f / Ratio - 1f);
        else
        {
            float x = over + KneeDb / 2f;
            reduction = (1f / Ratio - 1f) * x * x / (2f * KneeDb);
        }
        return MathF.Max(reduction, -MostReductionDb);
    }

    /// <summary>Compresses interleaved stereo in place: the detector runs sample by sample, the gain is set at the block's end and eased across it.</summary>
    public void Process(Span<float> stereo)
    {
        int frames = stereo.Length / 2;
        if (frames == 0) return;
        float p = power;
        for (int i = 0; i < frames; i++)
        {
            float l = stereo[i * 2], r = stereo[i * 2 + 1];
            float x = MathF.Max(l * l, r * r);
            p += (x > p ? attack : release) * (x - p);
        }
        power = p;
        float levelDb = 10f * MathF.Log10(MathF.Max(p, 1e-12f));
        ReductionDb = ReductionFor(levelDb);
        reductionSum += ReductionDb;
        blocks++;
        MostDb = MathF.Min(MostDb, ReductionDb);
        float target = Db(ReductionDb + MakeUpDb);
        float from = gain;
        for (int i = 0; i < frames; i++)
        {
            float g = from + (target - from) * (i + 1) / frames;
            stereo[i * 2] *= g;
            stereo[i * 2 + 1] *= g;
        }
        gain = target;
    }

    private static float Db(float db) => MathF.Pow(10f, db / 20f);
}
