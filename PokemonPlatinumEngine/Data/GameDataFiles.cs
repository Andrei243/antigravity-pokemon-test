using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// <summary>
    /// The folder the data files are read from: the copy beside the executable, or, in developer mode, the repository's
    /// own <c>PokemonPlatinumEngine/Data</c>, so the file saved is the file read (plan 16 · T6, <c>Core.DevMode</c>,
    /// which sets it before the game loads anything).
    /// </summary>
    public static string Directory { get; set; } = Path.Combine(AppContext.BaseDirectory, "Data");

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

    /// <summary>
    /// Where a data file is read from: the game's own <see cref="Directory"/>, or the last pack in use that carries a
    /// file of that name (plan 16 · T14, <see cref="DataLayers.PathOf"/>); with no pack, the game's own path.
    /// </summary>
    public static string PathOf(string relativePath) => DataLayers.PathOf(relativePath);

    /// <summary>
    /// The five files a battle's rules are made of (plan 08 · P10): what <see cref="Version"/> is a hash of. A file the
    /// importer rewrites with another move, species, ability, item or trainer changes it.
    /// </summary>
    public static readonly string[] VersionedFiles = { "species.json", "moves.json", "abilities.json", "items.json", "trainers.json" };

    private static string? version;

    /// <summary>
    /// The version of the game's data (plan 08 · P10): a hash of <see cref="VersionedFiles"/>, sixteen hexadecimal
    /// digits. A recorded battle carries it and is refused under any other (plan 08's risk "A replay goes stale"); plan
    /// 07's rooms, plan 12 · Q25's suspended battles and Q29's Home box, plan 13 · V22's trainer files and plan 16 ·
    /// T19's recordings refuse the same way. Read once, the first time it is asked.
    /// </summary>
    public static string Version => version ??= VersionOf(VersionedFiles.SelectMany(name =>
        // A pack's patches of a file are part of the data it makes (plan 16 · T14); with no pack, the five files alone
        new[] { File.ReadAllBytes(PathOf(name)) }.Concat(DataLayers.PatchesOf(name).Select(File.ReadAllBytes))));

    /// <summary>The data's version worked out again the next time it is asked: the packs in use have changed.</summary>
    public static void ForgetVersion() => version = null;

    /// <summary>
    /// The hash <see cref="Version"/> is: FNV-1a of 64 bits over the files in their order, each followed by a nought,
    /// passing over carriage returns, so a copy of the files checked out with Windows' line endings is the same data.
    /// (A string's <c>GetHashCode</c> changes from run to run and is a build error, plan 24 · X1.)
    /// </summary>
    public static string VersionOf(IEnumerable<byte[]> files)
    {
        const ulong offset = 14695981039346656037UL, prime = 1099511628211UL;
        ulong hash = offset;
        foreach (var bytes in files)
        {
            foreach (byte b in bytes)
            {
                if (b == (byte)'\r') continue;
                hash = (hash ^ b) * prime;
            }
            hash *= prime;
        }
        return hash.ToString("x16");
    }

    /// <summary>Reads and parses one data file; a missing or malformed file stops start-up with the file named.</summary>
    public static T Load<T>(string relativePath)
    {
        string path = PathOf(relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Game data file not found: {path}", path);

        try
        {
            // A pack's patch of the file laid over it (plan 16 · T14); the file as it is when there is none
            return JsonSerializer.Deserialize<T>(DataLayers.ReadPatched(relativePath), Json)
                ?? throw new InvalidDataException($"Game data file is empty: {path}");
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"Game data file {path} is malformed: {e.Message}", e);
        }
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json) + "\n";

    /// <summary>Reads a value back from what <see cref="Serialize{T}"/> wrote.</summary>
    public static T Deserialize<T>(string text) => JsonSerializer.Deserialize<T>(text, Json) ?? throw new InvalidDataException("Nothing to read.");
}
