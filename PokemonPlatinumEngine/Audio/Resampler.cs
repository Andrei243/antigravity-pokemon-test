using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// Plan 31 · MU1: stereo from one rate into another, by a polyphase filter whose phases are exact. The two rates
/// are reduced to a ratio of whole numbers (the DS's 32,768 Hz into the mix's 48,000 Hz is 256 into 375), so the
/// output's place between two input samples cycles through the same <see cref="Phases"/> forever and never drifts:
/// a stream resampled for an hour is the same as one resampled for a second, sample for sample. Each phase is a
/// Kaiser-windowed sinc of <see cref="Taps"/> input samples, scaled so its taps add up to exactly one, so silence
/// stays silence and an offset passes unchanged. The filter is centred on the output's own time: output sample
/// <c>k</c> is the input at <c>k · from / to</c>, with no delay, which takes <see cref="Taps"/>/2 input samples of
/// look-ahead (<see cref="InputFor"/>). GPU-free and deterministic; the mixer pulls a block at a time.
/// </summary>
public sealed class Resampler
{
    /// <summary>The filter the mixer uses (MU1's measurements chose it; <c>docs/music-format.md</c>, "The mix's rate").</summary>
    public const int MixTaps = 128;
    public const float MixBeta = 11f;
    public const float MixCutoff = 16_200f;

    public int From { get; }
    public int To { get; }
    public int Taps { get; }

    /// <summary>How many places between two input samples the output falls on: the output rate over its common factor with the input's.</summary>
    public int Phases { get; }

    /// <summary>How many input samples one output sample moves on by, in <see cref="Phases"/>ths.</summary>
    public int Step { get; }

    private readonly int half;
    private readonly float[] table;
    private float[] left, right;
    private int count, pos, phase;

    /// <summary>The DS's rate into the mix's, with the mixer's filter.</summary>
    public static Resampler ForMix() => new(Synthesizer.DsRate, AudioMixer.Rate, MixTaps, MixBeta, MixCutoff);

    /// <param name="cutoff">Where the filter is half down (−6 dB), in Hz: under the lower rate's Nyquist, so the images of what is above it are gone by the time they would reach the output.</param>
    public Resampler(int from, int to, int taps, float beta, float cutoff)
    {
        if (taps < 8 || taps % 8 != 0) throw new ArgumentException("taps must be a multiple of 8", nameof(taps));
        From = from;
        To = to;
        Taps = taps;
        half = taps / 2;
        int g = Gcd(from, to);
        Phases = to / g;
        Step = from / g;

        // The sinc's cutoff over the input's Nyquist: the time between taps is one input sample
        double fc = Math.Min(cutoff / (from / 2.0), Math.Min(1.0, (double)to / from));
        double i0Beta = BesselI0(beta);
        table = new float[Phases * taps];
        var row = new double[taps];
        for (int p = 0; p < Phases; p++)
        {
            double sum = 0;
            for (int j = 0; j < taps; j++)
            {
                // Tap j reads the input sample (half − 1 − j) before the output's own time, plus the phase's share
                double tau = (double)p / Phases + (half - 1 - j);
                double x = fc * tau;
                double sinc = Math.Abs(x) < 1e-12 ? 1.0 : Math.Sin(Math.PI * x) / (Math.PI * x);
                double w = tau / half;
                double window = Math.Abs(w) >= 1 ? 0 : BesselI0(beta * Math.Sqrt(1 - w * w)) / i0Beta;
                row[j] = fc * sinc * window;
                sum += row[j];
            }
            for (int j = 0; j < taps; j++) table[p * taps + j] = (float)(row[j] / sum);
        }

        left = new float[4096];
        right = new float[4096];
        Reset();
    }

    /// <summary>Forgets everything pushed: the next output is the first, at the input's first sample.</summary>
    public void Reset()
    {
        Array.Clear(left);
        Array.Clear(right);
        // The taps before the first sample read silence
        count = half - 1;
        pos = half - 1;
        phase = 0;
    }

    /// <summary>How many more input frames must be pushed before <paramref name="outputFrames"/> can be pulled.</summary>
    public int InputFor(int outputFrames)
    {
        if (outputFrames <= 0) return 0;
        long last = pos + ((long)phase + (long)(outputFrames - 1) * Step) / Phases;
        return (int)Math.Max(0, last + half + 1 - count);
    }

    /// <summary>Adds input frames (interleaved stereo).</summary>
    public void Push(ReadOnlySpan<float> stereo)
    {
        int n = stereo.Length / 2;
        if (count + n > left.Length)
        {
            int size = left.Length;
            while (size < count + n) size *= 2;
            Array.Resize(ref left, size);
            Array.Resize(ref right, size);
        }
        for (int i = 0; i < n; i++)
        {
            left[count + i] = stereo[i * 2];
            right[count + i] = stereo[i * 2 + 1];
        }
        count += n;
    }

    /// <summary>Writes the next output frames (interleaved stereo) into <paramref name="output"/>, overwriting it. Enough input must have been pushed (<see cref="InputFor"/>).</summary>
    public void Pull(Span<float> output)
    {
        int frames = output.Length / 2;
        if (InputFor(frames) > 0) throw new InvalidOperationException("not enough input pushed");
        var taps = table.AsSpan();
        for (int k = 0; k < frames; k++)
        {
            int start = pos - (half - 1);
            var h = taps.Slice(phase * Taps, Taps);
            output[k * 2] = Dot(left.AsSpan(start, Taps), h);
            output[k * 2 + 1] = Dot(right.AsSpan(start, Taps), h);
            phase += Step;
            pos += phase / Phases;
            phase %= Phases;
        }
        // What no later output reads is let go
        int drop = pos - (half - 1);
        if (drop > 0)
        {
            Array.Copy(left, drop, left, 0, count - drop);
            Array.Copy(right, drop, right, 0, count - drop);
            count -= drop;
            pos -= drop;
        }
    }

    private static float Dot(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        int n = a.Length, i = 0;
        float sum = 0f;
        if (Vector.IsHardwareAccelerated && n >= Vector<float>.Count)
        {
            var va = MemoryMarshal.Cast<float, Vector<float>>(a);
            var vb = MemoryMarshal.Cast<float, Vector<float>>(b);
            var acc = Vector<float>.Zero;
            for (int v = 0; v < va.Length; v++) acc += va[v] * vb[v];
            sum = Vector.Sum(acc);
            i = va.Length * Vector<float>.Count;
        }
        for (; i < n; i++) sum += a[i] * b[i];
        return sum;
    }

    /// <summary>The filter's taps for one phase (for the measurements and the tests).</summary>
    public ReadOnlySpan<float> PhaseTaps(int p) => table.AsSpan(p * Taps, Taps);

    private static int Gcd(int a, int b)
    {
        while (b != 0) (a, b) = (b, a % b);
        return a;
    }

    /// <summary>The modified Bessel function of the first kind, order nought, by its series.</summary>
    private static double BesselI0(double x)
    {
        double sum = 1, term = 1, q = x * x / 4;
        for (int k = 1; k < 64; k++)
        {
            term *= q / (k * k);
            sum += term;
            if (term < sum * 1e-17) break;
        }
        return sum;
    }
}
