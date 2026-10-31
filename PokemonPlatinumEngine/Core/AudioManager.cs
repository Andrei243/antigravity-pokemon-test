using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core.Diagnostics;
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

    [ThreadStatic] private static bool hushed;

    /// <summary>
    /// While set, every sound effect, cry and fanfare asked for on this thread is dropped, unheard: a scene being
    /// skipped (plan 12 · Q20) plays none of the sounds of what it hurries through, whoever would have asked for
    /// them. A change of music is kept: the song is the one a played scene would have left.
    /// </summary>
    public static bool Hushed { get => hushed; set => hushed = value; }

    /// <summary>Plays a sound effect by name, over the music. <paramref name="pan"/> is -1 for the left, 1 for the right.</summary>
    public static void PlaySound(string soundName, float pan = 0f)
    {
        if (hushed) return;
        heard?.Add(soundName);
        // The pad trembles with what it stands for, muted or not (plan 12 · Q24)
        Haptics.FromSound(soundName);
        if (isMuted || !isInitialized) return;
        var sound = SoundBank.Get(soundName);
        if (sound == null)
        {
            Log.Warn($"AUDIO: no sound '{soundName}'");
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
        if (hushed) return;
        string name = species.Form(form)?.Name ?? species.Name;
        heard?.Add(mode == CryMode.Normal ? $"cry {name}" : $"cry {name} {mode}");
        if (isMuted || !isInitialized) return;
        mixer.PlaySound(Cries.Get(species, form, mode), AudioBus.Cry, 1f, pan);
    }

    /// <summary>
    /// Plays a species' cry (or one of its forms') bent by the Pokédex's cry page (plan 08 · P18): pitched, doubled,
    /// rung or filtered as <paramref name="shape"/> says, and at the place its pan puts it. Tests hear it as
    /// "cry &lt;name&gt; shaped &lt;shape&gt;".
    /// </summary>
    public static void PlayCry(PokemonSpecies species, string? form, CryShape shape)
    {
        if (hushed) return;
        string name = species.Form(form)?.Name ?? species.Name;
        heard?.Add($"cry {name} shaped {shape.Clamped().Key}");
        if (isMuted || !isInitialized) return;
        mixer.PlaySound(Cries.Shaped(species, form, shape), AudioBus.Cry, 1f, shape.Clamped().PanPosition);
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
            Log.Warn($"AUDIO: no song '{songId}'");
            return;
        }
        NoteHeard(song.Id);
        if (!isInitialized) return;
        mixer.Play(song, MusicDirector.IsNightArrangement(GameClock.Now), immediate);
    }

    private static readonly SortedSet<string> heardSongs = new(StringComparer.Ordinal);

    /// <summary>
    /// Every song the game has played for the player so far (plan 12 · Q15), by id: what the jukebox unlocks. It is
    /// noted whenever a song or fanfare is asked for, with or without an audio device, and is kept in the save.
    /// </summary>
    public static IReadOnlyCollection<string> Heard => heardSongs;

    private static void NoteHeard(string songId) => heardSongs.Add(songId);

    /// <summary>Puts the songs heard back as a save had them (a game loaded), dropping ids that are no song.</summary>
    public static void SetHeard(IEnumerable<string>? ids)
    {
        heardSongs.Clear();
        if (ids == null) return;
        foreach (var id in ids)
            if (MusicLibrary.Get(id) is { } song) heardSongs.Add(song.Id);
    }

    /// <summary>
    /// The jukebox's play (plan 12 · Q15): a song in the arrangement asked for, from the top, at once. It notes
    /// nothing as heard (only a heard song can be asked for); tests hear it as "music &lt;id&gt; [night] [lowhp] [giant]".
    /// </summary>
    public static void PreviewMusic(string songId, bool night = false, bool lowHp = false, bool giant = false)
    {
        var song = MusicLibrary.Get(songId);
        if (song == null) return;
        heard?.Add($"music {song.Id}{(night ? " night" : "")}{(lowHp ? " lowhp" : "")}{(giant ? " giant" : "")}");
        if (!isInitialized) return;
        mixer.LowHp = lowHp;
        mixer.Giant = giant;
        mixer.Play(song, night, immediate: true);
    }

    /// <summary>The jukebox's bed: one ambience bed at full strength, or none (null). Tests hear it as "bed &lt;name&gt;".</summary>
    public static void PreviewBed(AmbienceBed? bed)
    {
        if (bed is { } b) heard?.Add($"bed {b}");
        SetAmbience(bed is { } on ? new[] { new AmbienceLayer(on, 1f, 0f) } : Array.Empty<AmbienceLayer>());
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

    /// <summary>
    /// Plays the current region's song for a scene that no role names (<see cref="MusicDirector.SceneSong"/>), or the
    /// role's where the region has none: the credits' and the Hall of Fame's.
    /// </summary>
    public static void PlaySceneMusic(string scene, MusicRole fallback, bool immediate = false)
    {
        if (!isInitialized) return;
        if (MusicDirector.SceneSong(Region, scene, MusicLibrary.Exists) is { } own) PlayMusic(own, immediate);
        else PlayMusic(fallback, immediate);
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

    /// <summary>
    /// Cuts to the battle theme for meeting a wild Pokémon of this species: a legendary or mythical one's own, or
    /// the wild battle theme (<see cref="MusicDirector.WildBattleTheme"/>).
    /// </summary>
    public static void PlayWildBattleMusic(PokemonSpecies species)
    {
        if (!isInitialized) return;
        string? id = MusicDirector.WildBattleTheme(species, Region, MusicLibrary.Exists);
        if (id != null) PlayMusic(id, immediate: true);
    }

    /// <summary>Pauses the music for a jingle (healing, an item, a level-up), then lets it carry on.</summary>
    public static void PlayFanfare(MusicRole role)
    {
        if (hushed) return;
        heard?.Add($"fanfare {role}");
        if (!isInitialized) return;
        string? id = MusicDirector.Resolve(role, Region, MusicLibrary.Exists);
        var song = id == null ? null : MusicLibrary.Get(id);
        if (song != null) NoteHeard(song.Id);
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

    /// <summary>
    /// Whether the music plays its giant arrangement (plan Y · YD5): set by the battle while a Dynamaxed Pokémon stands
    /// on the field (<c>BattleEngine.GiantStands</c>), beside <see cref="LowHp"/> and cleared with it.
    /// </summary>
    public static bool Giant
    {
        get => mixer.Giant;
        set => mixer.Giant = value;
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

    /// <summary>Whether everything plays in one channel (the options' Speakers on Mono, plan 08 · P1).</summary>
    public static bool Mono
    {
        get => mixer.Mono;
        set => mixer.Mono = value;
    }

    /// <summary>Whether everything sounds as it would from the handheld's own speakers (the options' Speakers).</summary>
    public static bool Handheld
    {
        get => mixer.Handheld;
        set => mixer.Handheld = value;
    }

    /// <summary>
    /// Whether the game is waiting behind another window (plan 16 · T3): everything drops to a quarter
    /// (<see cref="AudioMixer.AwayLevel"/>) until the player is back. The engine says so every frame.
    /// </summary>
    public static bool Away
    {
        get => mixer.Away;
        set => mixer.Away = value;
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
