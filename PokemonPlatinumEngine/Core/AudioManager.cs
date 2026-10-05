using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PokemonPlatinumEngine.Audio;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// Everything the game hears: music (songs from <c>Data/music</c>), fanfares and sound effects (<see cref="SoundBank"/>),
/// mixed by one <see cref="AudioMixer"/> into an audio stream that raylib's audio thread pulls from. The options'
/// volumes set the mixer's buses (<see cref="SetVolumes"/>). Everything here is a no-op until <see cref="Initialize"/>
/// has opened the audio device, so the game's logic and tests can call it freely.
/// </summary>
public static class AudioManager
{
    private static readonly AudioMixer mixer = new();
    private static bool isInitialized = false;
    private static AudioStream stream;
    private static bool isMuted = false;
    public static bool IsMuted => isMuted;

    /// <summary>The region whose versions of the shared themes (battles, victories) play; set on entering a map.</summary>
    public static string? Region { get; set; }

    /// <summary>The song playing now, or about to once the last one has faded.</summary>
    public static string? CurrentMusic => mixer.CurrentId;

    /// <summary>The sounds <see cref="PlaySound"/> knows, by name: what a script's <c>sound</c> may ask for.</summary>
    public static string[] SoundNames => SoundBank.Names;

    public static void Initialize()
    {
        if (isInitialized) return;
        try
        {
            Raylib.InitAudioDevice();
            if (Raylib.IsAudioDeviceReady())
            {
                isInitialized = true;
                SoundBank.Preload();
                StartStream();
            }
        }
        catch
        {
            // Fallback gracefully
        }
    }

    /// <summary>
    /// The audio thread asks for samples about every 30 ms; the mixer renders them on that thread, so the music
    /// keeps going while the game thread is busy loading a map.
    /// </summary>
    private static unsafe void StartStream()
    {
        try
        {
            Raylib.SetAudioStreamBufferSizeDefault(1024);
            stream = Raylib.LoadAudioStream((uint)Synthesizer.SampleRate, 32, 2);
            Raylib.SetAudioStreamCallback(stream, &FillStream);
            Raylib.PlayAudioStream(stream);
        }
        catch
        {
            // Fallback gracefully: the game runs without sound
        }
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void FillStream(void* buffer, uint frames)
    {
        var output = new Span<float>(buffer, (int)frames * 2);
        try
        {
            mixer.Render(output);
        }
        catch
        {
            output.Clear();
        }
    }

    /// <summary>Plays a sound effect by name, over the music. <paramref name="pan"/> is -1 for the left, 1 for the right.</summary>
    public static void PlaySound(string soundName, float pan = 0f)
    {
        if (isMuted || !isInitialized) return;
        var sound = SoundBank.Get(soundName);
        if (sound == null)
        {
            Console.WriteLine($"WARNING: AUDIO: no sound '{soundName}'");
            return;
        }
        mixer.PlaySound(sound, AudioBus.Sound, 1f, pan);
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
        mixer.Play(song, MusicDirector.IsNightArrangement(GameClock.Now), immediate);
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
        if (song != null) mixer.PlayFanfare(song);
    }

    /// <summary>Fades the music out.</summary>
    public static void StopMusic()
    {
        if (isInitialized) mixer.Stop();
    }

    /// <summary>
    /// The options' volumes, 0 to 1: the music's bus carries the music and the fanfares, the sound's the effects,
    /// the cries and the ambience.
    /// </summary>
    public static void SetVolumes(float music, float sound)
    {
        mixer.SetVolume(AudioBus.Music, music);
        mixer.SetVolume(AudioBus.Fanfare, music);
        mixer.SetVolume(AudioBus.Sound, sound);
        mixer.SetVolume(AudioBus.Cry, sound);
        mixer.SetVolume(AudioBus.Ambience, sound);
    }

    public static void ToggleMute()
    {
        isMuted = !isMuted;
        mixer.Muted = isMuted;
    }

    public static void Close()
    {
        if (!isInitialized) return;
        Raylib.StopAudioStream(stream);
        Raylib.UnloadAudioStream(stream);
        Raylib.CloseAudioDevice();
        isInitialized = false;
    }
}
