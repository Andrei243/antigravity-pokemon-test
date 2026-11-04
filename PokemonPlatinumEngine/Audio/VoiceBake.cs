using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.Audio;

/// <summary>
/// The bake (plan 26 · V3, <c>VoiceTool bake</c> and <c>bake-samples</c>), GPU-free and with no Python: a take as an
/// engine wrote it (or a sample as the user recorded it) cleaned the same way every time. It trims the silence at each
/// end, takes out the rumble under 80 Hz, softens plosives, brings the line to the middle of the voice bus's loudness
/// window with a gentle limiter keeping its peak under −1 dB, fades its ends to nothing, and takes it to the rate the
/// game plays voices at. What it finds on the way (a clip, a noisy room, a length that doesn't fit the words) is said
/// beside the result, and a take with a finding is the one a tool chooses last.
/// </summary>
public static class VoiceBake
{
    public sealed record Result(float[] Samples, int Rate, List<string> Findings)
    {
        public double Seconds => Samples.Length / (double)Rate;
    }

    /// <summary>Where the rumble is cut.</summary>
    public const float HighPass = 80f;

    /// <summary>Where a plosive's thump lives, under the voice's own body.</summary>
    public const float PlosiveBand = 200f;

    /// <summary>How much quiet is left before the voice and after it.</summary>
    public const double KeepBefore = 0.08, KeepAfter = 0.15;

    /// <summary>The fades at each end, so a take starts and ends at nothing.</summary>
    public const double Fade = 0.006;

    /// <summary>The ceiling the limiter holds a peak to: half a decibel under the checks' −1 dB.</summary>
    public const double Ceiling = Loudness.VoicePeak - 0.5;

    /// <summary>Words a second a line can be spoken at; outside them the take has gone wrong (a word skipped or repeated, a long gap).</summary>
    public const double SlowestWords = 0.8, FastestWords = 6.0;

    /// <summary>
    /// Cleans a take: <paramref name="samples"/> at <paramref name="rate"/>, mono. <paramref name="text"/> is what it says
    /// (null for no check of its length). The result is at <paramref name="outRate"/> (the DS's rate the game plays voices
    /// at; the samples keep their own with <c>bake-samples</c>), and at the voice bus's level unless <paramref name="level"/>
    /// is false (a sample is only cleaned, its loudness left as it was spoken).
    /// </summary>
    public static Result Clean(float[] samples, int rate, string? text, int outRate = Synthesizer.DsRate, bool level = true)
    {
        var findings = new List<string>();
        if (samples.Length == 0) return new Result(Array.Empty<float>(), outRate, new List<string> { "it holds no sound" });

        int clipped = samples.Count(x => MathF.Abs(x) >= WavDecoder.ClipLevel);
        if (clipped > 0) findings.Add($"it clips ({clipped} samples at full scale)");
        var levels = VoiceSamples.Levels(samples, rate);
        double floor = levels.OrderBy(l => l).ElementAt(Math.Clamp(levels.Count / 10, 0, levels.Count - 1));
        if (floor > VoiceSamples.TooNoisyFloor) findings.Add($"a noise floor of {floor:0} dB");

        var x = (float[])samples.Clone();
        double mean = x.Average();
        for (int i = 0; i < x.Length; i++) x[i] -= (float)mean;
        HighPassInPlace(x, rate, HighPass);
        SoftenPlosives(x, rate);

        // The silence at each end: what is quieter than the room's noise plus 12 dB, or 40 dB under the loudest window
        levels = VoiceSamples.Levels(x, rate);
        double peakWindow = levels.Max();
        floor = levels.OrderBy(l => l).ElementAt(Math.Clamp(levels.Count / 10, 0, levels.Count - 1));
        double threshold = Math.Max(floor + 12, peakWindow - 40);
        int first = levels.FindIndex(l => l > threshold), last = levels.FindLastIndex(l => l > threshold);
        if (first < 0) return new Result(Array.Empty<float>(), outRate, findings.Append("no voice could be told from the noise").ToList());
        int window = Math.Max(1, (int)(VoiceSamples.Window * rate));
        int start = Math.Max(0, first * window - (int)(KeepBefore * rate));
        int end = Math.Min(x.Length, (last + 1) * window + (int)(KeepAfter * rate));
        x = x[start..end];

        if (outRate != rate) x = Resample(x, rate, outRate);

        if (level)
        {
            var (low, high) = Loudness.Target(AudioBus.Voice);
            double measured = Loudness.Of(x, 1, outRate);
            float gain = Loudness.Linear((low + high) / 2 - measured);
            for (int i = 0; i < x.Length; i++) x[i] *= gain;
            Limit(x, outRate, Loudness.Linear(Ceiling));
            double after = Loudness.Of(x, 1, outRate);
            if (after < low - 0.5) findings.Add($"so peaky the limiter left it at {after:0.0} dB, under the voice bus's window");
        }
        FadeEnds(x, outRate);

        double seconds = x.Length / (double)outRate;
        if (text != null && !Fits(text, seconds, out double perSecond))
            findings.Add($"{perSecond:0.0} words a second over {seconds:0.0} s: the length doesn't fit the words (a word skipped or repeated, or a long gap)");
        return new Result(x, outRate, findings);
    }

    /// <summary>Whether a take of <paramref name="seconds"/> can be <paramref name="text"/> spoken at a plausible pace.</summary>
    public static bool Fits(string text, double seconds, out double wordsPerSecond)
    {
        int words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(w => w.Any(char.IsLetterOrDigit));
        // A line of a word or two is said in about a second whatever it is
        wordsPerSecond = words / Math.Max(seconds - 0.25, 0.1);
        if (words <= 2) return seconds is > 0.2 and < 4;
        return wordsPerSecond is >= SlowestWords and <= FastestWords;
    }

    /// <summary>A second-order Butterworth high-pass, in place.</summary>
    public static void HighPassInPlace(float[] x, int rate, float cutoff)
    {
        double w = Math.Tan(Math.PI * cutoff / rate), q = Math.Sqrt(0.5);
        double norm = 1 / (1 + w / q + w * w);
        double b0 = norm, b1 = -2 * norm, b2 = norm, a1 = 2 * (w * w - 1) * norm, a2 = (1 - w / q + w * w) * norm;
        Biquad(x, b0, b1, b2, a1, a2);
    }

    /// <summary>A second-order Butterworth low-pass of <paramref name="x"/>, as a new array.</summary>
    public static float[] LowPass(float[] x, int rate, float cutoff)
    {
        var y = (float[])x.Clone();
        double w = Math.Tan(Math.PI * cutoff / rate), q = Math.Sqrt(0.5);
        double norm = 1 / (1 + w / q + w * w);
        Biquad(y, w * w * norm, 2 * w * w * norm, w * w * norm, 2 * (w * w - 1) * norm, (1 - w / q + w * w) * norm);
        return y;
    }

    private static void Biquad(float[] x, double b0, double b1, double b2, double a1, double a2)
    {
        double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double input = x[i];
            double y = b0 * input + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1; x1 = input; y2 = y1; y1 = y;
            x[i] = (float)y;
        }
    }

    /// <summary>
    /// Softens a plosive's thump (a "p" or "b" into the microphone): where the band under <see cref="PlosiveBand"/> carries
    /// most of the sound and is loud, that band is turned down by up to 9 dB, eased over a few milliseconds. A voice
    /// spoken past the microphone has none, and is left as it is.
    /// </summary>
    public static void SoftenPlosives(float[] x, int rate)
    {
        var low = LowPass(x, rate, PlosiveBand);
        int size = Math.Max(1, rate / 200); // 5 ms
        float most = Loudness.Linear(-9);
        float gain = 1f, attack = 1f - MathF.Exp(-1f / (0.002f * rate)), release = 1f - MathF.Exp(-1f / (0.04f * rate));
        double lowSum = 0, allSum = 0;
        var lowRing = new double[size];
        var allRing = new double[size];
        var gains = new float[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            double l = low[i] * (double)low[i], a = x[i] * (double)x[i];
            int r = i % size;
            lowSum += l - lowRing[r]; allSum += a - allRing[r];
            lowRing[r] = l; allRing[r] = a;
            double lowDb = Loudness.Db(Math.Sqrt(Math.Max(lowSum, 0) / size));
            double share = allSum > 1e-12 ? lowSum / allSum : 0;
            float want = 1f;
            if (lowDb > -24 && share > 0.6) want = MathF.Max(most, (float)(1 - (share - 0.6) / 0.4 * (1 - most)));
            gain += (want - gain) * (want < gain ? attack : release);
            gains[i] = gain;
        }
        // The gain is taken from a little ahead, so the thump's first swing is caught too
        int ahead = Math.Max(1, rate / 100);
        // The least gain of the next 'ahead' samples, kept by a queue of rising gains
        var queue = new LinkedList<int>();
        int added = 0;
        for (int i = 0; i < x.Length; i++)
        {
            int until = Math.Min(x.Length - 1, i + ahead);
            for (; added <= until; added++)
            {
                while (queue.Count > 0 && gains[queue.Last!.Value] >= gains[added]) queue.RemoveLast();
                queue.AddLast(added);
            }
            while (queue.First!.Value < i) queue.RemoveFirst();
            x[i] -= low[i] * (1f - gains[queue.First.Value]);
        }
    }

    /// <summary>
    /// A look-ahead limiter: each sample is held under <paramref name="ceiling"/> by a gain that falls ahead of a peak
    /// over two milliseconds and comes back over sixty, so a loud syllable is turned down without a click.
    /// </summary>
    public static void Limit(float[] x, int rate, float ceiling)
    {
        int ahead = Math.Max(1, rate / 500);
        var want = new float[x.Length];
        for (int i = 0; i < x.Length; i++) want[i] = MathF.Min(1f, ceiling / MathF.Max(MathF.Abs(x[i]), 1e-9f));
        // The least gain wanted within the look-ahead
        var least = new float[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            float m = 1f;
            for (int j = i; j < Math.Min(x.Length, i + ahead); j++) m = MathF.Min(m, want[j]);
            least[i] = m;
        }
        float release = 1f - MathF.Exp(-1f / (0.06f * rate));
        float g = 1f;
        for (int i = 0; i < x.Length; i++)
        {
            g = least[i] < g ? least[i] : g + (least[i] - g) * release;
            x[i] = Math.Clamp(x[i] * g, -ceiling, ceiling);
        }
    }

    /// <summary>Fades the first and last <see cref="Fade"/> seconds in and out.</summary>
    public static void FadeEnds(float[] x, int rate)
    {
        int n = Math.Min(x.Length / 2, Math.Max(1, (int)(Fade * rate)));
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            x[i] *= t;
            x[x.Length - 1 - i] *= t;
        }
        if (x.Length > 0) { x[0] = 0f; x[^1] = 0f; }
    }

    /// <summary>Mono samples from one rate to another through the mixer's own band-limited resampler (plan 31).</summary>
    public static float[] Resample(float[] x, int from, int to)
    {
        if (from == to || x.Length == 0) return x;
        float cutoff = 0.45f * Math.Min(from, to);
        var r = new Resampler(from, to, 64, Resampler.MixBeta, cutoff);
        var stereo = new float[x.Length * 2];
        for (int i = 0; i < x.Length; i++) stereo[i * 2] = stereo[i * 2 + 1] = x[i];
        r.Push(stereo);
        int frames = (int)((long)x.Length * to / from);
        // Silence after the end, so the last outputs read nothing beyond it
        r.Push(new float[(r.Taps + 2) * 2]);
        while (frames > 0 && r.InputFor(frames) > 0) frames--;
        var outStereo = new float[frames * 2];
        r.Pull(outStereo);
        var y = new float[frames];
        for (int i = 0; i < frames; i++) y[i] = outStereo[i * 2];
        return y;
    }
}
