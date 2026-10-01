using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// Reads the game's data files (species, moves, items, maps) from the <c>Data</c> folder next to the executable.
/// The files are checked in under <c>PokemonPlatinumEngine/Data</c> and copied to the build output.
/// </summary>
public static class GameDataFiles
{
    /// <summary>The folder the data files are read from.</summary>
    public static string Directory { get; } = Path.Combine(AppContext.BaseDirectory, "Data");

    /// <summary>camelCase names, enums as their names, accented text kept readable.</summary>
    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        NewLine = "\n"
    };

    public static string PathOf(string relativePath) => Path.Combine(Directory, relativePath);

    /// <summary>Reads and parses one data file; a missing or malformed file stops start-up with the file named.</summary>
    public static T Load<T>(string relativePath)
    {
        string path = PathOf(relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Game data file not found: {path}", path);

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), Json)
                ?? throw new InvalidDataException($"Game data file is empty: {path}");
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"Game data file {path} is malformed: {e.Message}", e);
        }
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json) + "\n";
}
