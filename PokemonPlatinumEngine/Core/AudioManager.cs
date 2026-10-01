using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

public static class AudioManager
{
    private static readonly Dictionary<string, Sound> SoundEffects = new(StringComparer.OrdinalIgnoreCase);
    private static bool isInitialized = false;
    private static string currentTrack = "";
    private static float bgmTimer = 0f;
    private static int bgmNoteIndex = 0;
    private static bool isMuted = false;
    public static bool IsMuted => isMuted;

    // Sinnoh melody frequencies (Hz)
    private static readonly (float Freq, float Duration)[] TwinleafTheme = new[]
    {
        (392.00f, 0.4f), (493.88f, 0.4f), (587.33f, 0.6f), (523.25f, 0.2f),
        (493.88f, 0.4f), (440.00f, 0.4f), (392.00f, 0.6f), (329.63f, 0.2f),
        (349.23f, 0.4f), (392.00f, 0.4f), (440.00f, 0.6f), (392.00f, 0.2f),
        (349.23f, 0.4f), (329.63f, 0.4f), (293.66f, 0.8f), (0f, 0.2f)
    };

    private static readonly (float Freq, float Duration)[] Route201Theme = new[]
    {
        (523.25f, 0.2f), (587.33f, 0.2f), (659.25f, 0.4f), (523.25f, 0.2f),
        (783.99f, 0.4f), (659.25f, 0.4f), (587.33f, 0.2f), (523.25f, 0.2f),
        (440.00f, 0.4f), (523.25f, 0.4f), (587.33f, 0.6f), (0f, 0.1f),
        (659.25f, 0.3f), (587.33f, 0.3f), (523.25f, 0.6f), (0f, 0.2f)
    };

    private static readonly (float Freq, float Duration)[] BattleTheme = new[]
    {
        (220.00f, 0.12f), (246.94f, 0.12f), (261.63f, 0.12f), (293.66f, 0.12f),
        (329.63f, 0.12f), (349.23f, 0.12f), (392.00f, 0.12f), (440.00f, 0.12f),
        (587.33f, 0.18f), (523.25f, 0.18f), (493.88f, 0.18f), (440.00f, 0.18f),
        (392.00f, 0.15f), (440.00f, 0.15f), (493.88f, 0.15f), (523.25f, 0.15f)
    };

    private static readonly (float Freq, float Duration)[] VictoryTheme = new[]
    {
        (523.25f, 0.15f), (523.25f, 0.15f), (523.25f, 0.15f), (523.25f, 0.45f),
        (415.30f, 0.45f), (466.16f, 0.45f), (523.25f, 0.3f), (0f, 0.1f),
        (466.16f, 0.15f), (523.25f, 0.8f), (0f, 0.5f)
    };

    // The title screen: slow and minor, our own melody
    private static readonly (float Freq, float Duration)[] TitleTheme = new[]
    {
        (293.66f, 0.6f), (440.00f, 0.6f), (349.23f, 0.6f), (466.16f, 0.9f), (0f, 0.3f),
        (440.00f, 0.45f), (392.00f, 0.45f), (349.23f, 0.45f), (329.63f, 0.9f), (0f, 0.3f),
        (293.66f, 0.6f), (349.23f, 0.6f), (440.00f, 0.6f), (587.33f, 1.2f),
        (523.25f, 0.6f), (466.16f, 0.6f), (440.00f, 1.2f), (0f, 0.6f)
    };

    public static void Initialize()
    {
        if (isInitialized) return;
        try
        {
            Raylib.InitAudioDevice();
            if (Raylib.IsAudioDeviceReady())
            {
                isInitialized = true;
                GenerateSoundEffects();
            }
        }
        catch
        {
            // Fallback gracefully
        }
    }

    private static void GenerateSoundEffects()
    {
        RegisterSynthSound("select", 880f, 0.08f, WaveType.Square);
        RegisterSynthSound("cursor", 440f, 0.04f, WaveType.Square);
        RegisterSynthSound("cancel", 220f, 0.10f, WaveType.Triangle);
        RegisterSamples("bump", SynthesizeBump());
        RegisterSynthSound("grass", 600f, 0.06f, WaveType.Noise);
        RegisterSynthSound("exclaim", 1318.5f, 0.18f, WaveType.Square);
        RegisterSynthSound("hit_normal", 240f, 0.12f, WaveType.Noise);
        RegisterSynthSound("hit_super", 380f, 0.20f, WaveType.Square);
        RegisterSynthSound("faint", 150f, 0.50f, WaveType.Sawtooth);
        RegisterSynthSound("ball_throw", 700f, 0.15f, WaveType.Sine);
        RegisterSynthSound("ball_shake", 400f, 0.10f, WaveType.Triangle);
        RegisterSynthSound("levelup", 1046.5f, 0.35f, WaveType.Sine);
        RegisterSynthSound("heal", 659.25f, 0.40f, WaveType.Sine);
    }

    private enum WaveType { Sine, Square, Triangle, Sawtooth, Noise }

    private const uint SampleRate = 22050;

    private static void RegisterSynthSound(string name, float baseFreq, float durationSec, WaveType type)
    {
        int totalSamples = (int)(SampleRate * durationSec);
        var samples = new float[totalSamples];
        Random rng = new(42);

        for (int i = 0; i < totalSamples; i++)
        {
            float t = (float)i / SampleRate;
            float progress = (float)i / totalSamples;
            float envelope = 1.0f - progress;
            float sample = 0f;

            float freq = baseFreq;
            if (name == "select") freq += progress * 400f;
            if (name == "faint") freq -= progress * 80f;

            switch (type)
            {
                case WaveType.Sine:
                    sample = MathF.Sin(2f * MathF.PI * freq * t);
                    break;
                case WaveType.Square:
                    sample = MathF.Sin(2f * MathF.PI * freq * t) >= 0 ? 0.7f : -0.7f;
                    break;
                case WaveType.Triangle:
                    sample = (MathF.Abs((t * freq % 1f) - 0.5f) * 4f) - 1f;
                    break;
                case WaveType.Sawtooth:
                    sample = ((t * freq % 1f) * 2f) - 1f;
                    break;
                case WaveType.Noise:
                    sample = (float)(rng.NextDouble() * 2.0 - 1.0);
                    break;
            }

            samples[i] = sample * envelope * (12000f / 32767f);
        }

        RegisterSamples(name, samples);
    }

    /// <summary>
    /// A soft, low "thud" for walking into something: a sine body whose pitch drops quickly, with a
    /// little low-passed noise for the impact and a short fade-in so it doesn't click.
    /// </summary>
    private static float[] SynthesizeBump()
    {
        const float duration = 0.14f;
        var samples = new float[(int)(SampleRate * duration)];
        var rng = new Random(7);
        float phase = 0f, noise = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float t = (float)i / SampleRate;
            float freq = 105f + 150f * MathF.Exp(-t * 30f);
            phase += MathF.Tau * freq / SampleRate;

            // Second harmonic keeps the thud audible on small speakers
            float body = (MathF.Sin(phase) + 0.35f * MathF.Sin(phase * 2f)) * MathF.Exp(-t * 26f);
            noise += ((float)rng.NextDouble() * 2f - 1f - noise) * 0.11f;
            float impact = noise * MathF.Exp(-t * 70f) * 1.4f;
            float attack = Math.Min(1f, t / 0.004f);

            samples[i] = (body * 0.7f + impact) * attack * 0.42f;
        }
        return samples;
    }

    /// <summary>Loads mono samples in [-1, 1] as a sound effect, replacing any earlier sound of that name.</summary>
    private static unsafe void RegisterSamples(string name, float[] samples)
    {
        try
        {
            int byteCount = samples.Length * sizeof(short);
            IntPtr unmanagedMem = Marshal.AllocHGlobal(byteCount);
            short* ptr = (short*)unmanagedMem.ToPointer();
            for (int i = 0; i < samples.Length; i++)
            {
                ptr[i] = (short)(Math.Clamp(samples[i], -1f, 1f) * 32767f);
            }

            Wave wave = new()
            {
                SampleCount = (uint)samples.Length,
                SampleRate = SampleRate,
                SampleSize = 16,
                Channels = 1,
                Data = (void*)unmanagedMem
            };

            Sound snd = Raylib.LoadSoundFromWave(wave);
            Marshal.FreeHGlobal(unmanagedMem);
            if (SoundEffects.TryGetValue(name, out var oldSnd))
            {
                Raylib.UnloadSound(oldSnd);
            }
            SoundEffects[name] = snd;
        }
        catch
        {
            // Fallback gracefully
        }
    }

    public static void PlaySound(string soundName)
    {
        if (isMuted || !isInitialized) return;
        if (SoundEffects.TryGetValue(soundName, out var snd))
        {
            Raylib.PlaySound(snd);
        }
    }

    public static void PlayBGM(string trackName)
    {
        if (currentTrack == trackName) return;
        currentTrack = trackName;
        bgmTimer = 0f;
        bgmNoteIndex = 0;
    }

    public static void Update(float dt)
    {
        if (isMuted || !isInitialized || string.IsNullOrEmpty(currentTrack)) return;

        var playlist = currentTrack switch
        {
            "Twinleaf" => TwinleafTheme,
            "Route201" => Route201Theme,
            "Battle" => BattleTheme,
            "Victory" => VictoryTheme,
            "Title" => TitleTheme,
            _ => TwinleafTheme
        };

        if (playlist.Length == 0) return;

        bgmTimer += dt;
        var currentNote = playlist[bgmNoteIndex];

        if (bgmTimer >= currentNote.Duration)
        {
            bgmTimer = 0f;
            bgmNoteIndex = (bgmNoteIndex + 1) % playlist.Length;
            var nextNote = playlist[bgmNoteIndex];

            if (nextNote.Freq > 20f)
            {
                PlayToneNote(nextNote.Freq, nextNote.Duration * 0.8f);
            }
        }
    }

    private static void PlayToneNote(float freq, float duration)
    {
        RegisterSynthSound("current_tone", freq, Math.Min(duration, 0.3f), WaveType.Triangle);
        if (SoundEffects.TryGetValue("current_tone", out var snd))
        {
            Raylib.SetSoundVolume(snd, 0.25f);
            Raylib.PlaySound(snd);
        }
    }

    public static void ToggleMute()
    {
        isMuted = !isMuted;
    }

    public static void Close()
    {
        if (!isInitialized) return;
        foreach (var kvp in SoundEffects)
        {
            Raylib.UnloadSound(kvp.Value);
        }
        SoundEffects.Clear();
        Raylib.CloseAudioDevice();
        isInitialized = false;
    }
}
