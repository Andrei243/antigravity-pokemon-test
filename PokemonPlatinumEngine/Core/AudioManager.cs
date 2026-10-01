using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PokemonPlatinumEngine.Audio;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Sound effects (short synthesised samples played through raylib) and music (songs from <c>Data/music</c>
/// rendered by <see cref="MusicMixer"/> into an audio stream that raylib's audio thread pulls from).
/// Everything here is a no-op until <see cref="Initialize"/> has opened the audio device, so the game's logic and
/// tests can call it freely.
/// </summary>
public static class AudioManager
{
    private static readonly Dictionary<string, Sound> SoundEffects = new(StringComparer.OrdinalIgnoreCase);
    private static readonly MusicMixer music = new();
    private static bool isInitialized = false;
    private static AudioStream musicStream;
    private static bool isMuted = false;
    public static bool IsMuted => isMuted;

    /// <summary>The region whose versions of the shared themes (battles, victories) play; set on entering a map.</summary>
    public static string? Region { get; set; }

    /// <summary>The song playing now, or about to once the last one has faded.</summary>
    public static string? CurrentMusic => music.CurrentId;

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
                StartMusicStream();
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

    /// <summary>
    /// The audio thread asks for samples about every 30 ms; the mixer renders them on that thread, so the music
    /// keeps going while the game thread is busy loading a map.
    /// </summary>
    private static unsafe void StartMusicStream()
    {
        try
        {
            Raylib.SetAudioStreamBufferSizeDefault(1024);
            musicStream = Raylib.LoadAudioStream((uint)Synthesizer.SampleRate, 32, 2);
            Raylib.SetAudioStreamCallback(musicStream, &FillMusicStream);
            Raylib.PlayAudioStream(musicStream);
        }
        catch
        {
            // Fallback gracefully: the game runs without music
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void FillMusicStream(void* buffer, uint frames)
    {
        var output = new Span<float>(buffer, (int)frames * 2);
        try
        {
            music.Render(output);
        }
        catch
        {
            output.Clear();
        }
    }

    /// <summary>
    /// Plays a song by id (<c>sinnoh/twinleaf</c>), in its night arrangement at night. <paramref name="immediate"/>
    /// starts it from the top at once instead of fading out the current song.
    /// </summary>
    public static void PlayMusic(string songId, bool immediate = false)
    {
        var song = MusicLibrary.Get(songId);
        if (song == null)
        {
            Console.WriteLine($"WARNING: AUDIO: no song '{songId}'");
            return;
        }
        if (!isInitialized) return;
        music.Play(song, MusicDirector.IsNightArrangement(GameClock.Now), immediate);
    }

    /// <summary>
    /// Plays the current region's song for a role, such as the wild battle theme. <paramref name="immediate"/> cuts
    /// straight to it (battles) instead of fading the old song out first.
    /// </summary>
    public static void PlayMusic(MusicRole role, bool immediate = false)
    {
        if (!isInitialized) return;
        string? id = MusicDirector.Resolve(role, Region, MusicLibrary.Exists);
        if (id != null) PlayMusic(id, immediate);
    }

    /// <summary>Pauses the music for a jingle (healing, an item, a level-up), then lets it carry on.</summary>
    public static void PlayFanfare(MusicRole role)
    {
        if (!isInitialized) return;
        string? id = MusicDirector.Resolve(role, Region, MusicLibrary.Exists);
        var song = id == null ? null : MusicLibrary.Get(id);
        if (song != null) music.PlayFanfare(song);
    }

    /// <summary>Fades the music out.</summary>
    public static void StopMusic()
    {
        if (isInitialized) music.Stop();
    }

    public static void ToggleMute()
    {
        isMuted = !isMuted;
        music.Muted = isMuted;
    }

    public static void Close()
    {
        if (!isInitialized) return;
        foreach (var kvp in SoundEffects)
        {
            Raylib.UnloadSound(kvp.Value);
        }
        SoundEffects.Clear();
        Raylib.StopAudioStream(musicStream);
        Raylib.UnloadAudioStream(musicStream);
        Raylib.CloseAudioDevice();
        isInitialized = false;
    }
}
