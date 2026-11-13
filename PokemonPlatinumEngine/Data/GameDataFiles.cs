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
    /// <see cref="Json"/> for a file a person writes (plan 24 · X4): a key no property has is refused rather than
    /// passed over (and case counts, so <c>BgmTrack</c> is refused, not read), a property marked
    /// <see cref="DataRequiredAttribute"/> must be there, and the small records written on one line
    /// (<see cref="OneLine{T}"/>) refuse unknown keys too. Generated files and the player's own are read with
    /// <see cref="Json"/>; the tests read the generated ones with this.
    /// </summary>
    public static readonly JsonSerializerOptions Strict = MakeStrict();

    private static JsonSerializerOptions MakeStrict()
    {
        var strict = new JsonSerializerOptions(Json)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
            {
                Modifiers = { MarkRequired }
            }
        };
        return strict;
    }

    // Only the strict reading asks for the properties marked required, so a lenient read of the same type is unchanged
    private static void MarkRequired(System.Text.Json.Serialization.Metadata.JsonTypeInfo info)
    {
        if (info.Kind != System.Text.Json.Serialization.Metadata.JsonTypeInfoKind.Object) return;
        foreach (var property in info.Properties)
            if (property.AttributeProvider?.IsDefined(typeof(DataRequiredAttribute), inherit: true) == true)
                property.IsRequired = true;
    }

    /// <summary>
    /// Where a data file is read from: the game's own <see cref="Directory"/>, or the last pack in use that carries a
    /// file of that name (plan 16 · T14, <see cref="DataLayers.PathOf"/>); with no pack, the game's own path.
    /// </summary>
    public static string PathOf(string relativePath) => DataLayers.PathOf(relativePath);

    /// <summary>
    /// The files a battle's rules are made of (plan 08 · P10): what <see cref="Version"/> is a hash of. A file the
    /// importer rewrites with another move, species, ability, item or trainer changes it, and so does a tale's rule
    /// (plan 46 · CP12: <c>tales.json</c>, whose rules a recorded battle with a tale told is fought by).
    /// </summary>
    public static readonly string[] VersionedFiles = { "species.json", "moves.json", "abilities.json", "items.json", "trainers.json", "tales.json" };

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

    /// <summary>
    /// Reads one data file a person writes strictly (plan 24 · X4, <see cref="Strict"/>): an unknown key, a value of the
    /// wrong type or a missing required field stops the load with the file, the JSON path and the key named
    /// (<c>world/sinnoh/overlays/route_204_south.json: $.people.lass_sarah.hidenBy: no such field</c>).
    /// </summary>
    public static T Load<T>(string relativePath, bool strict)
    {
        if (!strict) return Load<T>(relativePath);
        string path = PathOf(relativePath);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Game data file not found: {path}", path);
        return ReadStrict<T>(DataLayers.ReadPatched(relativePath), relativePath.Replace('\\', '/'));
    }

    /// <summary>
    /// Reads a text strictly as <paramref name="file"/> (<see cref="Strict"/>); a finding is an
    /// <see cref="InvalidDataException"/> whose message is <see cref="Finding"/>'s.
    /// </summary>
    public static T ReadStrict<T>(string text, string file)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(text, Strict) ?? throw new InvalidDataException($"{file}: the file is empty");
        }
        catch (JsonException e)
        {
            throw new InvalidDataException(Finding(file, e), e);
        }
    }

    /// <summary>
    /// What a strict read found, on one line: the file, the JSON path down to the key, and what is wrong with it.
    /// </summary>
    public static string Finding(string file, JsonException e)
    {
        string path = e.Path ?? "$";
        string message = e.Message;
        if (e is UnknownKeyException unknown)
        {
            path = Join(path, unknown.Key);
            message = "no such field";
        }
        else if (IsUnknownKey(e))
        {
            // The serializer's own refusal: its path already ends in the key
            message = "no such field";
        }
        else if (message.Contains("missing required properties", StringComparison.Ordinal))
        {
            int names = message.IndexOf("including: ", StringComparison.Ordinal);
            message = names < 0 ? "a required field is missing" : "missing " + message[(names + "including: ".Length)..].TrimEnd('.');
        }
        else
        {
            // A value of the wrong type: the serializer's sentence, without the path it repeats
            int cut = message.IndexOf(" Path: ", StringComparison.Ordinal);
            if (cut >= 0) message = message[..cut];
        }
        return $"{file}: {path}: {message}";
    }

    /// <summary>Whether a strict read's refusal is of a key no field has (rather than a value or a missing field).</summary>
    public static bool IsUnknownKey(JsonException e) =>
        e is UnknownKeyException || e.Message.Contains("could not be mapped to any .NET member", StringComparison.Ordinal);

    private static string Join(string path, string key) =>
        key.All(c => char.IsLetterOrDigit(c) || c == '_') ? $"{path}.{key}" : $"{path}['{key}']";

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Json) + "\n";

    /// <summary>Reads a value back from what <see cref="Serialize{T}"/> wrote.</summary>
    public static T Deserialize<T>(string text) => JsonSerializer.Deserialize<T>(text, Json) ?? throw new InvalidDataException("Nothing to read.");
}
