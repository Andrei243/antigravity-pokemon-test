using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PokemonPlatinumEngine.Core;

public enum GraphicsQuality { Low, Medium, High }

/// <summary>What a graphics quality preset turns on.</summary>
/// <param name="SceneScale">3D scene resolution in layout units: 2 renders the scene at 3840x2160, 1 at 1920x1080.</param>
public readonly record struct QualityProfile(float SceneScale, bool AmbientOcclusion, bool Fxaa, int ShadowTaps, int ShadowMapSize, bool DepthOfField)
{
    public static QualityProfile For(GraphicsQuality quality) => quality switch
    {
        GraphicsQuality.Low => new QualityProfile(1f, false, true, 5, 1024, false),
        GraphicsQuality.Medium => new QualityProfile(1.5f, true, true, 7, 2048, true),
        _ => new QualityProfile(2f, true, true, 9, 2048, true)
    };
}

/// <summary>How fast written lines are typed out.</summary>
public enum TextSpeed { Slow, Normal, Fast }

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

    /// <summary>How fast what people say is written out.</summary>
    public TextSpeed TextSpeed { get; set; } = TextSpeed.Normal;

    /// <summary>Characters a second at a text speed (style guide, "Menu screens").</summary>
    public static float CharactersPerSecond(TextSpeed speed) => speed switch
    {
        TextSpeed.Slow => 24f,
        TextSpeed.Fast => 120f,
        _ => 45f
    };

    /// <summary>A fixed time of day, or null to follow the clock.</summary>
    public TimeOfDay? TimeOfDay { get; set; }

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
            Console.WriteLine($"Could not read {path}: {e.Message}; using default settings.");
        }
        return new GameSettings { FirstRun = !File.Exists(path) };
    }

    public void Save(string path = FileName)
    {
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(this, Json));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Could not write {path}: {e.Message}");
        }
    }
}
