using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Core.Diagnostics;

namespace PokemonPlatinumEngine.Core;

public enum GraphicsQuality { Low, Medium, High }

/// <summary>What a graphics quality preset turns on.</summary>
/// <param name="SceneScale">3D scene resolution in layout units: 2 renders the scene at 3840x2160, 1 at 1920x1080.</param>
/// <param name="ShadowTaps">Filtered lookups of the shadow map to a pixel: 1, 4 or 9, for an edge two, three or four texels soft.</param>
public readonly record struct QualityProfile(float SceneScale, bool AmbientOcclusion, bool Fxaa, int ShadowTaps, int ShadowMapSize, bool DepthOfField)
{
    public static QualityProfile For(GraphicsQuality quality) => quality switch
    {
        GraphicsQuality.Low => new QualityProfile(1f, false, true, 1, 1024, false),
        GraphicsQuality.Medium => new QualityProfile(1.5f, true, true, 4, 2048, true),
        _ => new QualityProfile(2f, true, true, 9, 2048, true)
    };
}

/// <summary>What the sound comes out of: speakers or headphones as they are, or the handheld's own small speakers.</summary>
public enum SpeakerMode { Stereo, Handheld }

/// <summary>How fast written lines are typed out.</summary>
public enum TextSpeed { Slow, Normal, Fast }

/// <summary>
/// A help the newest games made their default (plan 12): off, as the rules of the game in progress have it (a
/// Platinum game goes without, a modern one has it), or on.
/// </summary>
public enum RulesDefault { Off, Rules, On }

public static class RulesDefaults
{
    /// <summary>Whether the help is on in a game played by these rules.</summary>
    public static bool Holds(this RulesDefault value, Data.RulesPreset preset) =>
        value == RulesDefault.On || value == RulesDefault.Rules && preset == Data.RulesPreset.Modern;
}

/// <summary>Player options, kept in settings.json next to the save.</summary>
public sealed class GameSettings
{
    public const string FileName = "settings.json";

    public static readonly (int Width, int Height)[] WindowSizes = { (1280, 720), (1600, 900), (1920, 1080), (2560, 1440), (3840, 2160) };

    public GraphicsQuality Quality { get; set; } = GraphicsQuality.High;
    public int WindowWidth { get; set; } = 1920;
    public int WindowHeight { get; set; } = 1080;
    public bool Fullscreen { get; set; }
    public bool VSync { get; set; } = true;
    public bool Muted { get; set; }

    /// <summary>How loud the music and the fanfares play, 0 to 100.</summary>
    public int MusicVolume { get; set; } = 100;

    /// <summary>How loud the sound effects play, 0 to 100.</summary>
    public int SoundVolume { get; set; } = 100;

    /// <summary>How loud the Pokémon's cries play, 0 to 100.</summary>
    public int CryVolume { get; set; } = 100;

    /// <summary>How loud the field's ambience (rain, wind, water, caves) plays, 0 to 100.</summary>
    public int AmbienceVolume { get; set; } = 100;

    /// <summary>Stereo, or the handheld's speakers.</summary>
    public SpeakerMode Speakers { get; set; } = SpeakerMode.Stereo;

    /// <summary>The options move a volume by this much.</summary>
    public const int VolumeStep = 10;

    /// <summary>How fast what people say is written out.</summary>
    public TextSpeed TextSpeed { get; set; } = TextSpeed.Normal;

    /// <summary>Characters a second at a text speed (style guide, "Menu screens").</summary>
    public static float CharactersPerSecond(TextSpeed speed) => speed switch
    {
        TextSpeed.Slow => 24f,
        TextSpeed.Fast => 120f,
        _ => 45f
    };

    /// <summary>
    /// Whether a damaging move's card says how it will do against a foe already seen (plan 12 · Q10): Sun and
    /// Moon's way, so a modern game has it and a Platinum game doesn't unless the player asks.
    /// </summary>
    public RulesDefault MoveHints { get; set; } = RulesDefault.Rules;

    /// <summary>
    /// Whether the trainers across the field speak at the battle's turning points (plan 11 · C12): Black and White's
    /// way, so a modern game has it and a Platinum game, whose trainers speak only before and after, doesn't unless
    /// the player asks.
    /// </summary>
    public RulesDefault TrainerLines { get; set; } = RulesDefault.Rules;

    /// <summary>A fixed time of day, or null to follow the clock.</summary>
    public TimeOfDay? TimeOfDay { get; set; }

    /// <summary>
    /// Whether the game waits while its window isn't the one in front (plan 16 · T3): off unless chosen, so the game
    /// runs on behind another window as it always did. On, nothing moves and the sound drops until the player is back
    /// (<see cref="GameEngine.HeldForFocus"/>).
    /// </summary>
    public bool FocusPause { get; set; }

    /// <summary>
    /// The most frames drawn a second (plan 16 · T3), one of <see cref="FrameCaps"/>: 60 unless chosen, which is what
    /// the game always asked of raylib; nought is no limit. A cap and nothing more: V-Sync is its own row.
    /// </summary>
    public int FrameCap { get; set; } = 60;

    /// <summary>The frame limits the options offer, in their order: nought is none.</summary>
    public static readonly int[] FrameCaps = { 30, 60, 120, 0 };

    /// <summary>True when there was no settings file to load, so the window still has to be sized for the monitor.</summary>
    [JsonIgnore]
    public bool FirstRun { get; private set; }

    /// <summary>The largest listed window size a monitor can show with its title bar and the taskbar.</summary>
    public static (int Width, int Height) LargestWindowFor(int monitorWidth, int monitorHeight)
    {
        var best = WindowSizes[0];
        foreach (var size in WindowSizes)
        {
            if (size.Width <= monitorWidth && size.Height <= monitorHeight - 80) best = size;
        }
        return best;
    }

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static GameSettings Load(string path = FileName)
    {
        try
        {
            if (File.Exists(path)) return JsonSerializer.Deserialize<GameSettings>(File.ReadAllText(path), Json) ?? new GameSettings();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            Log.Warn($"Could not read {path}: {e.Message}; using default settings.");
        }
        return new GameSettings { FirstRun = !File.Exists(path) };
    }

    // What a switch of the command line set for this run alone, by the setting's name: the value it had before, which
    // is what the file keeps, and the value the run was given
    private readonly Dictionary<string, (object? Before, object? ThisRun)> forThisRun = new();

    /// <summary>
    /// Sets one of the settings for this run alone (plan 16 · T3: <c>--windowed</c>, <c>--fullscreen</c>,
    /// <c>--safe-mode</c>). The game runs with the value given; <see cref="Save"/> goes on writing the value the
    /// setting had before, so the next ordinary start finds the options as the player left them. Once the player
    /// changes that setting themselves it is theirs again, and saved as they set it.
    /// </summary>
    public void SetForThisRun(string setting, object value)
    {
        var property = typeof(GameSettings).GetProperty(setting) ?? throw new ArgumentException($"There is no setting '{setting}'.", nameof(setting));
        // Set twice in one run (safe mode and --windowed): what the file keeps is still the first "before"
        object? before = forThisRun.TryGetValue(setting, out var held) ? held.Before : property.GetValue(this);
        forThisRun[setting] = (before, value);
        property.SetValue(this, value);
    }

    /// <summary>The options as <c>settings.json</c> holds them: as they are, but for what a switch set for this run alone.</summary>
    public string ToFile()
    {
        var putBack = new List<(System.Reflection.PropertyInfo Property, object? ThisRun)>();
        foreach (var (setting, (before, thisRun)) in forThisRun.ToList())
        {
            var property = typeof(GameSettings).GetProperty(setting)!;
            if (!Equals(property.GetValue(this), thisRun))
            {
                // The player has changed it since: it is their choice now, for good
                forThisRun.Remove(setting);
                continue;
            }
            property.SetValue(this, before);
            putBack.Add((property, thisRun));
        }
        try
        {
            return JsonSerializer.Serialize(this, Json);
        }
        finally
        {
            foreach (var (property, thisRun) in putBack) property.SetValue(this, thisRun);
        }
    }

    public void Save(string path = FileName)
    {
        try
        {
            File.WriteAllText(path, ToFile());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"Could not write {path}: {e.Message}");
        }
    }

    /// <summary>The options as they are in force now: what the log and a crash report say of them.</summary>
    public string Describe() => JsonSerializer.Serialize(this, Json);
}
