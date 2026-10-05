using System;
using System.Collections.Concurrent;

namespace PokemonPlatinumEngine.Audio;

/// <summary>A sound effect: mono samples in [-1, 1] at the synthesiser's rate, ready for the mixer.</summary>
public sealed class SoundSample
{
    public string Name { get; }
    public float[] Samples { get; }

    public SoundSample(string name, float[] samples)
    {
        Name = name;
        Samples = samples;
    }

    public double Duration => Samples.Length / (double)Synthesizer.SampleRate;
}

/// <summary>
/// The game's sound effects, synthesised in code the first time each is asked for (layered oscillators, noise,
/// filters, envelopes and pitch sweeps) and kept. Everything here runs without an audio device, so tests and
/// <c>tools/MusicRender</c> can render and check every sound. Plan 05 · A3 designs the full set; the thirteen here
/// are the ones the game had before the mixer played them.
/// </summary>
public static class SoundBank
{
    /// <summary>Every sound the bank makes, by name: what <c>AudioManager.PlaySound</c> and a script's <c>sound</c> may ask for.</summary>
    public static readonly string[] Names =
    {
        "select", "cursor", "cancel", "bump", "grass", "exclaim", "hit_normal", "hit_super", "faint", "ball_throw", "ball_shake", "levelup", "heal"
    };

    private static readonly ConcurrentDictionary<string, SoundSample> Made = new(StringComparer.OrdinalIgnoreCase);

    public static bool Exists(string name) => Array.FindIndex(Names, n => n.Equals(name, StringComparison.OrdinalIgnoreCase)) >= 0;

    /// <summary>The sound of that name, made on first use, or null for a name the bank doesn't know.</summary>
    public static SoundSample? Get(string name)
    {
        if (!Exists(name)) return null;
        return Made.GetOrAdd(name.ToLowerInvariant(), n => new SoundSample(n, Make(n)));
    }

    /// <summary>Makes every sound now, so the first blip in the game costs nothing on its frame.</summary>
    public static void Preload()
    {
        foreach (var name in Names) Get(name);
    }

    private enum Wave { Sine, Square, Triangle, Saw, Noise }

    private static float[] Make(string name) => name switch
    {
        "select" => Finish(Tone(0.08f, Wave.Square, 880f, 1280f, Linear), 0.4f),
        "cursor" => Finish(Tone(0.04f, Wave.Square, 440f, 440f, Linear), 0.35f),
        "cancel" => Finish(Tone(0.10f, Wave.Triangle, 220f, 220f, Linear), 0.45f),
        "bump" => Finish(Bump(), 0.42f),
        "grass" => Finish(Rustle(0.06f), 0.35f),
        "exclaim" => Finish(Tone(0.18f, Wave.Square, 1318.5f, 1318.5f, Linear), 0.4f),
        "hit_normal" => Finish(Thud(0.12f), 0.5f),
        "hit_super" => Finish(Tone(0.20f, Wave.Square, 380f, 380f, Linear), 0.45f),
        "faint" => Finish(Tone(0.50f, Wave.Saw, 150f, 70f, Linear), 0.4f),
        "ball_throw" => Finish(Tone(0.15f, Wave.Sine, 700f, 700f, Linear), 0.5f),
        "ball_shake" => Finish(Tone(0.10f, Wave.Triangle, 400f, 400f, Linear), 0.5f),
        "levelup" => Finish(Tone(0.35f, Wave.Sine, 1046.5f, 1046.5f, Linear), 0.5f),
        "heal" => Finish(Tone(0.40f, Wave.Sine, 659.25f, 659.25f, Linear), 0.5f),
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, "not a sound of the bank")
    };

    private const int Rate = Synthesizer.SampleRate;

    /// <summary>An envelope falling straight from 1 to 0 over the sound.</summary>
    private static float Linear(float progress) => 1f - progress;

    /// <summary>One oscillator whose pitch glides from <paramref name="from"/> to <paramref name="to"/> under an envelope.</summary>
    private static float[] Tone(float seconds, Wave wave, float from, float to, Func<float, float> envelope)
    {
        int n = (int)(Rate * seconds);
        var s = new float[n];
        uint seed = 0x9E3779B9u;
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float p = i / (float)n;
            float freq = from + (to - from) * p;
            phase += freq / Rate;
            phase -= MathF.Floor(phase);
            float x = wave switch
            {
                Wave.Sine => MathF.Sin(MathF.Tau * phase),
                Wave.Square => phase < 0.5f ? 0.7f : -0.7f,
                Wave.Triangle => MathF.Abs(phase - 0.5f) * 4f - 1f,
                Wave.Saw => phase * 2f - 1f,
                _ => Noise(ref seed)
            };
            s[i] = x * envelope(p);
        }
        return s;
    }

    /// <summary>
    /// A soft, low "thud" for walking into something: a sine body whose pitch drops quickly, with a little
    /// low-passed noise for the impact and a short fade-in so it doesn't click.
    /// </summary>
    private static float[] Bump()
    {
        const float duration = 0.14f;
        var s = new float[(int)(Rate * duration)];
        uint seed = 7;
        float phase = 0f, noise = 0f;
        for (int i = 0; i < s.Length; i++)
        {
            float t = (float)i / Rate;
            float freq = 105f + 150f * MathF.Exp(-t * 30f);
            phase += MathF.Tau * freq / Rate;

            // The second harmonic keeps the thud audible on small speakers
            float body = (MathF.Sin(phase) + 0.35f * MathF.Sin(phase * 2f)) * MathF.Exp(-t * 26f);
            noise += (Noise(ref seed) - noise) * 0.11f;
            float impact = noise * MathF.Exp(-t * 70f) * 1.4f;
            float attack = Math.Min(1f, t / 0.004f);
            s[i] = (body * 0.7f + impact) * attack;
        }
        return s;
    }

    /// <summary>Tall grass brushing past: a short burst of noise with its lows taken out.</summary>
    private static float[] Rustle(float seconds)
    {
        int n = (int)(Rate * seconds);
        var s = new float[n];
        uint seed = 0x2545F491u;
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float x = Noise(ref seed);
            lp += (x - lp) * 0.3f;
            s[i] = (x - lp) * (1f - i / (float)n);
        }
        return s;
    }

    /// <summary>A blow landing: low-passed noise that dies away.</summary>
    private static float[] Thud(float seconds)
    {
        int n = (int)(Rate * seconds);
        var s = new float[n];
        uint seed = 0xA511E9B3u;
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            lp += (Noise(ref seed) - lp) * 0.2f;
            s[i] = lp * 2.2f * (1f - i / (float)n);
        }
        return s;
    }

    /// <summary>White noise in [-1, 1] from a xorshift generator, the same every time.</summary>
    private static float Noise(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        seed ^= seed << 5;
        return (seed & 0xFFFFFF) / 8388608f - 1f;
    }

    /// <summary>Brings a sound to its peak level and rounds its ends off (1.5 ms in, 4 ms out), so it never clicks.</summary>
    private static float[] Finish(float[] s, float peak)
    {
        float max = 0f;
        foreach (float x in s) max = MathF.Max(max, MathF.Abs(x));
        float gain = max > 0f ? peak / max : 0f;
        int fadeIn = Math.Min(s.Length / 2, (int)(Rate * 0.0015f));
        int fadeOut = Math.Min(s.Length / 2, (int)(Rate * 0.004f));
        for (int i = 0; i < s.Length; i++)
        {
            float g = gain;
            if (i < fadeIn) g *= i / (float)fadeIn;
            int left = s.Length - 1 - i;
            if (left < fadeOut) g *= left / (float)fadeOut;
            s[i] *= g;
        }
        return s;
    }
}
