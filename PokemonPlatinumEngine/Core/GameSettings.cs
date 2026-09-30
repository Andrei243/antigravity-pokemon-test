using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PokemonPlatinumEngine.Core;

public enum GraphicsQuality { Low, Medium, High }

/// <summary>What a graphics quality preset turns on.</summary>
public readonly record struct QualityProfile(int SuperSample, bool AmbientOcclusion, bool Fxaa, int ShadowTaps, int ShadowMapSize, bool DepthOfField)
{
    public static QualityProfile For(GraphicsQuality quality) => quality switch
    {
        GraphicsQuality.Low => new QualityProfile(1, false, true, 4, 1024, false),
        GraphicsQuality.Medium => new QualityProfile(1, true, true, 8, 2048, true),
        _ => new QualityProfile(2, true, false, 16, 2048, true)
    };
}

/// <summary>Player options, kept in settings.json next to the save.</summary>
public sealed class GameSettings
{
    public const string FileName = "settings.json";

    public static readonly (int Width, int Height)[] WindowSizes = { (1280, 720), (1600, 900), (1920, 1080), (2560, 1440) };

    public GraphicsQuality Quality { get; set; } = GraphicsQuality.High;
    public int WindowWidth { get; set; } = 1920;
    public int WindowHeight { get; set; } = 1080;
    public bool Fullscreen { get; set; }
    public bool VSync { get; set; } = true;
    public bool Muted { get; set; }

    /// <summary>A fixed time of day, or null to follow the clock.</summary>
    public TimeOfDay? TimeOfDay { get; set; }

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
        return new GameSettings();
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
