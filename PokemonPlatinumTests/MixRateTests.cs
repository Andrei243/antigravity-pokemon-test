using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Xunit;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 31 · MU1: the mix at 48 kHz. The DS sound is mixed at its own rate as it always was and taken into the mix's
/// by a <see cref="Resampler"/>. A mixer made at the DS's rate resamples nothing and must render the very samples the
/// mixer rendered before MU1 (hashes measured before its first line); the resampler is held to its measurements, and
/// the mix at 48 kHz to the DS-rate mix it replaces.
/// </summary>
public class MixRateTests
{
    internal static float[] Render(AudioMixer mixer, double seconds)
    {
        var buffer = new float[(int)(seconds * mixer.OutputRate) * 2];
        for (int done = 0; done < buffer.Length; done += 2048)
            mixer.Render(buffer.AsSpan(done, Math.Min(2048, buffer.Length - done)));
        return buffer;
    }

    private static string Hash(float[] samples)
    {
        var bytes = new byte[samples.Length * 4];
        Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
        return Convert.ToHexString(SHA256.HashData(bytes))[..16];
    }

    /// <summary>A scene set going on <paramref name="mixer"/> and rendered in pieces, as the audio thread asks.</summary>
    private static float[] Scene(string name, AudioMixer mixer)
    {
        var twinleaf = MusicLibrary.Get("sinnoh/twinleaf")!;
        switch (name)
        {
            case "music":
                mixer.Play(twinleaf, night: false);
                return Render(mixer, 4);
            case "everything":
            {
                mixer.Play(MusicLibrary.Get("common/battle_wild")!, night: false, immediate: true);
                mixer.SetAmbience(new[] { new AmbienceLayer(AmbienceBed.Rain, 1f, -0.3f) });
                var a = Render(mixer, 0.5);
                mixer.PlaySound(SoundBank.Get("select")!, AudioBus.Sound, 1f, 0.5f);
                var b = Render(mixer, 0.7);
                mixer.PlaySound(Cries.Get(PokemonDatabase.Get("Piplup")!), AudioBus.Cry);
                var c = Render(mixer, 2.8);
                return [.. a, .. b, .. c];
            }
            case "handheld":
                mixer.Handheld = true;
                mixer.Mono = true;
                mixer.Play(twinleaf, night: true);
                return Render(mixer, 3);
            case "fanfare":
            {
                mixer.Play(twinleaf, night: false);
                var a = Render(mixer, 1);
                mixer.PlayFanfare(MusicLibrary.Get("common/fanfare_heal")!);
                var b = Render(mixer, 4);
                return [.. a, .. b];
            }
            default:
            {
                mixer.Play(twinleaf, night: false);
                var a = Render(mixer, 0.5);
                mixer.Away = true;
                var b = Render(mixer, 1);
                mixer.Away = false;
                var c = Render(mixer, 1);
                return [.. a, .. b, .. c];
            }
        }
    }

    // The mixer at the DS's rate, measured on the Windows machine (x64, .NET 10) before MU1's first line was written
    private static readonly (string Scene, string Hash)[] Goldens =
    {
        ("music", "296DBEA0EA6B234E"),
        ("everything", "C42DE2B487DAA27E"),
        ("handheld", "F6CE44A789C36D5C"),
        ("fanfare", "AEBD6596D8643D75"),
        ("away", "A87526DF013B3EE4"),
    };

    [Fact]
    public void TheMixerAtTheDsRateRendersToTheSampleAsBeforeMU1()
    {
        if (!OperatingSystem.IsWindows()) Assert.Skip("the golden samples were measured on Windows");
        var wrong = new List<string>();
        foreach (var (scene, hash) in Goldens)
        {
            string now = Hash(Scene(scene, new AudioMixer(Synthesizer.DsRate)));
            if (now != hash) wrong.Add($"{scene}: {now} (was {hash})");
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public void TheGamesMixerRunsAtTheDevicesRate()
    {
        Assert.Equal(48000, AudioMixer.Rate);
        Assert.Equal(AudioMixer.Rate, new AudioMixer().OutputRate);
        Assert.Equal(32768, Synthesizer.DsRate);
        Assert.Equal(Synthesizer.DsRate, new AudioMixer(Synthesizer.DsRate).OutputRate);
        Assert.Throws<ArgumentOutOfRangeException>(() => new AudioMixer(44100));
        // The resampler's phases are exact: 32,768 into 48,000 is 256 into 375
        var r = Resampler.ForMix();
        Assert.Equal(375, r.Phases);
        Assert.Equal(256, r.Step);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(440)]
    [InlineData(1000)]
    [InlineData(4000)]
    [InlineData(8000)]
    [InlineData(12000)]
    [InlineData(14000)]
    [InlineData(15000)]
    [InlineData(15500)]
    public void TheResamplerIsFlatAndCleanAcrossTheBand(double f)
    {
        // A sine of −6 dBFS: its level comes through within a fortieth of a decibel, and everything else (images,
        // ripple, rounding) is more than 105 dB under it. MU1 measured −112 dB or better to 15.5 kHz
        var (response, thdN, image) = Tone(Resampler.ForMix(), f);
        Assert.InRange(response, -0.025, 0.025);
        Assert.True(thdN < -105, $"{f} Hz: THD+N {thdN:0.0} dB");
        Assert.True(image < -105, $"{f} Hz: image {image:0.0} dB");
    }

    [Fact]
    public void TheResamplerPassesSilenceAndAnOffsetUntouchedAndEndsWhereTheRatioSays()
    {
        var r = Resampler.ForMix();
        for (int p = 0; p < r.Phases; p++)
        {
            float sum = 0;
            foreach (float h in r.PhaseTaps(p)) sum += h;
            Assert.Equal(1f, sum, 5);
        }
        // Silence is silence, an offset is the offset
        var silent = Resample(new float[Synthesizer.DsRate * 2], r);
        Assert.All(silent, x => Assert.Equal(0f, x));
        var offset = Enumerable.Repeat(0.25f, Synthesizer.DsRate * 2).ToArray();
        var level = Resample(offset, Resampler.ForMix());
        for (int i = 2000; i < level.Length - 2000; i++) Assert.Equal(0.25f, level[i], 5);

        // Ten minutes of output use exactly ten minutes of input: 375 out for every 256 in, with no drift
        var counted = Resampler.ForMix();
        long pushed = 0;
        var block = new float[1536 * 2];
        for (int done = 0; done < AudioMixer.Rate * 600; done += 1536)
        {
            int need = counted.InputFor(1536);
            counted.Push(new float[need * 2]);
            pushed += need;
            counted.Pull(block);
        }
        long outFrames = (long)Math.Ceiling(AudioMixer.Rate * 600 / 1536.0) * 1536;
        long expected = (outFrames - 1) * 256 / 375 + counted.Taps / 2 + 1;
        Assert.Equal(expected, pushed);
    }

    [Fact]
    public void TheResamplersOutputIsTheSameHoweverItIsAskedFor()
    {
        var input = new float[Synthesizer.DsRate * 2];
        var noise = new Random(7);
        for (int i = 0; i < input.Length; i++) input[i] = (float)(noise.NextDouble() - 0.5);
        var whole = Resample(input, Resampler.ForMix(), 1536);
        Assert.Equal(whole, Resample(input, Resampler.ForMix(), 1));
        Assert.Equal(whole, Resample(input, Resampler.ForMix(), 441));
        Assert.Equal(whole, Resample(input, Resampler.ForMix(), 4800));
    }

    [Theory]
    [InlineData("music")]
    [InlineData("everything")]
    [InlineData("handheld")]
    [InlineData("fanfare")]
    [InlineData("away")]
    public void TheMixAt48kHzMeasuresAsTheDsRateMixDid(string scene)
    {
        var before = Scene(scene, new AudioMixer(Synthesizer.DsRate));
        var after = Scene(scene, new AudioMixer());
        Assert.All(after, x => Assert.True(float.IsFinite(x) && MathF.Abs(x) < 1f));
        double a = Loudness.Of(before, 2, Synthesizer.DsRate), b = Loudness.Of(after, 2, AudioMixer.Rate);
        Assert.True(Math.Abs(a - b) < 0.1, $"{scene}: {a:0.00} dB before, {b:0.00} dB after");
    }

    [Theory]
    [InlineData("sinnoh/twinleaf")]
    [InlineData("common/legendary/dialga")]
    public void ASongThroughTheMixAt48kHzNullsAgainstTheDsRateMixResampled(string id)
    {
        // The same song from the same first sample down both paths: the DS stage is the same to the sample, so what is
        // left is the last stage at another rate. Its 30 Hz high-pass loses 0.025 dB of everything above it at the DS's
        // rate and 0.017 dB at the mix's, a difference of 0.008 dB, which nulls to about −60 dB; anything that moved a
        // note or a sample would be far louder
        var song = MusicLibrary.Get(id)!;
        var before = new AudioMixer(Synthesizer.DsRate);
        before.Play(song, night: false, immediate: true);
        var after = new AudioMixer();
        after.Play(song, night: false, immediate: true);
        var expected = Resample(Render(before, 6), Resampler.ForMix());
        var heard = Render(after, 6);
        int n = Math.Min(expected.Length, heard.Length);
        double signal = 0, diff = 0;
        for (int i = 0; i < n; i++)
        {
            signal += expected[i] * (double)expected[i];
            double d = heard[i] - (double)expected[i];
            diff += d * d;
        }
        double nulls = 10 * Math.Log10(diff / signal);
        Assert.True(nulls < -55, $"{id} nulls to {nulls:0.0} dB");
    }

    // ------------------------------------------------------------------ helpers

    private static float[] Resample(float[] stereo, Resampler r, int block = 1536)
    {
        int outFrames = (int)((long)(stereo.Length / 2) * r.To / r.From);
        var output = new float[outFrames * 2];
        int pushed = 0;
        for (int done = 0; done < outFrames; done += block)
        {
            int n = Math.Min(block, outFrames - done);
            int need = r.InputFor(n);
            int have = Math.Min(need, stereo.Length / 2 - pushed);
            r.Push(stereo.AsSpan(pushed * 2, have * 2));
            pushed += have;
            if (have < need) r.Push(new float[(need - have) * 2]);
            r.Pull(output.AsSpan(done * 2, n * 2));
        }
        return output;
    }

    /// <summary>A sine at <paramref name="f"/> through the resampler: its level out over in, everything but it, and its image, each in dB.</summary>
    private static (double Response, double ThdN, double Image) Tone(Resampler r, double f)
    {
        var input = new float[Synthesizer.DsRate * 2];
        for (int i = 0; i < input.Length / 2; i++)
            input[i * 2] = input[i * 2 + 1] = (float)(0.5 * Math.Sin(2 * Math.PI * f * i / Synthesizer.DsRate));
        var output = Resample(input, r);
        int from = AudioMixer.Rate / 5, to = AudioMixer.Rate * 4 / 5;
        var mono = new double[to - from];
        for (int i = 0; i < mono.Length; i++) mono[i] = output[(from + i) * 2];
        var (a, fit) = Fit(mono, f, from);
        var residual = new double[mono.Length];
        double resid = 0, power = 0;
        for (int i = 0; i < mono.Length; i++) { residual[i] = mono[i] - fit[i]; resid += residual[i] * residual[i]; power += fit[i] * fit[i]; }
        double imageF = Synthesizer.DsRate - f;
        if (imageF > AudioMixer.Rate / 2.0) imageF = AudioMixer.Rate - imageF;
        var (ai, _) = Fit(residual, imageF, from);
        return (20 * Math.Log10(a / 0.5), 10 * Math.Log10(Math.Max(resid, 1e-30) / power), 20 * Math.Log10(Math.Max(ai, 1e-15) / a));
    }

    private static (double Amplitude, double[] Fit) Fit(double[] x, double f, int offset)
    {
        double ss = 0, cc = 0, sc = 0, xs = 0, xc = 0;
        for (int i = 0; i < x.Length; i++)
        {
            double t = 2 * Math.PI * f * (i + offset) / AudioMixer.Rate, s = Math.Sin(t), c = Math.Cos(t);
            ss += s * s; cc += c * c; sc += s * c; xs += x[i] * s; xc += x[i] * c;
        }
        double det = ss * cc - sc * sc, a = (xs * cc - xc * sc) / det, b = (xc * ss - xs * sc) / det;
        var fit = new double[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            double t = 2 * Math.PI * f * (i + offset) / AudioMixer.Rate;
            fit[i] = a * Math.Sin(t) + b * Math.Cos(t);
        }
        return (Math.Sqrt(a * a + b * b), fit);
    }
}
