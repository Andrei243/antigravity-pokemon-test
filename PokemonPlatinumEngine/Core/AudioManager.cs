using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Models;
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
                Ambience.RequestAll();
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

    [ThreadStatic] private static List<string>? heard;

    /// <summary>
    /// Starts noting every sound asked for on this thread, with or without an audio device, in a list that fills as
    /// they are: how tests hear what the game plays. <see cref="StopListening"/> ends it.
    /// </summary>
    public static List<string> Listen() => heard = new List<string>();

    public static void StopListening() => heard = null;

    /// <summary>Plays a sound effect by name, over the music. <paramref name="pan"/> is -1 for the left, 1 for the right.</summary>
    public static void PlaySound(string soundName, float pan = 0f)
    {
        heard?.Add(soundName);
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
    /// Plays a Pokémon's cry on the cry bus, which dips the music under it. <paramref name="pan"/> is -1 for the
    /// left, 1 for the right. Tests hear it as "cry &lt;species or form&gt;", with the mode after unless it is normal.
    /// </summary>
    public static void PlayCry(Pokemon pokemon, CryMode mode = CryMode.Normal, float pan = 0f) => PlayCry(pokemon.Species, pokemon.Form, mode, pan);

    public static void PlayCry(PokemonSpecies species, string? form = null, CryMode mode = CryMode.Normal, float pan = 0f)
    {
        string name = species.Form(form)?.Name ?? species.Name;
        heard?.Add(mode == CryMode.Normal ? $"cry {name}" : $"cry {name} {mode}");
        if (isMuted || !isInitialized) return;
        mixer.PlaySound(Cries.Get(species, form, mode), AudioBus.Cry, 1f, pan);
    }

    /// <summary>Makes the cries a scene is about to play on a worker, so none is made on the frame that plays it. Nothing without an audio device.</summary>
    public static void RequestCries(IEnumerable<Pokemon> pokemon)
    {
        if (isInitialized) Cries.Request(pokemon);
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

    /// <summary>What the field sounds like now (the beds asked for last), with or without an audio device: how tests hear the ambience.</summary>
    public static IReadOnlyList<AmbienceLayer> CurrentAmbience { get; private set; } = Array.Empty<AmbienceLayer>();

    /// <summary>
    /// The field's ambience (plan 05 · A7): the beds that should sound now, each at its gain and pan. The game asks
    /// every frame; a bed not yet made is left out until its worker has made it, then fades in.
    /// </summary>
    public static void SetAmbience(IReadOnlyList<AmbienceLayer> layers)
    {
        CurrentAmbience = layers;
        if (!isInitialized) return;
        ready.Clear();
        foreach (var layer in layers)
            if (Ambience.TryGet(layer.Bed) != null) ready.Add(layer);
        // The mixer is told only when something changed: other beds, or one that has just been made
        if (ReferenceEquals(layers, forwarded) && ready.Count == forwardedCount) return;
        forwarded = layers;
        forwardedCount = ready.Count;
        mixer.SetAmbience(ready);
    }

    private static readonly List<AmbienceLayer> ready = new();
    private static IReadOnlyList<AmbienceLayer>? forwarded;
    private static int forwardedCount;

    /// <summary>Pauses the music for a jingle (healing, an item, a level-up), then lets it carry on.</summary>
    public static void PlayFanfare(MusicRole role)
    {
        if (!isInitialized) return;
        string? id = MusicDirector.Resolve(role, Region, MusicLibrary.Exists);
        var song = id == null ? null : MusicLibrary.Get(id);
        if (song != null) mixer.PlayFanfare(song);
    }

    /// <summary>
    /// Whether the music plays its low-HP arrangement: set by the battle while the player's Pokémon is in the red,
    /// the way Black and White turn the battle theme agitated instead of sounding an alarm.
    /// </summary>
    public static bool LowHp
    {
        get => mixer.LowHp;
        set => mixer.LowHp = value;
    }

    /// <summary>Fades the music out.</summary>
    public static void StopMusic()
    {
        if (isInitialized) mixer.Stop();
    }

    /// <summary>
    /// The options' volumes, 0 to 1, one for each bus but the fanfares', which follow the music (a fanfare is the
    /// music's own pause).
    /// </summary>
    public static void SetVolumes(float music, float sound, float cries, float ambience)
    {
        mixer.SetVolume(AudioBus.Music, music);
        mixer.SetVolume(AudioBus.Fanfare, music);
        mixer.SetVolume(AudioBus.Sound, sound);
        mixer.SetVolume(AudioBus.Cry, cries);
        mixer.SetVolume(AudioBus.Ambience, ambience);
    }

    /// <summary>A bus's volume as the options last set it.</summary>
    public static float VolumeOf(AudioBus bus) => mixer.VolumeOf(bus);

    /// <summary>Whether everything sounds as it would from the handheld's own speakers (the options' Speakers).</summary>
    public static bool Handheld
    {
        get => mixer.Handheld;
        set => mixer.Handheld = value;
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
