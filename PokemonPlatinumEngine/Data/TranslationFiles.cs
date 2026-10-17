using System.Collections.Concurrent;
using System.Text.Json;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// One line, or the lines said together, of the game's own files and the translation of them (plan 17 · N4): what the
/// English says (<see cref="En"/>) beside what a language says (<see cref="Text"/>, one entry to a line, empty where
/// the line isn't translated yet). An entry whose <see cref="En"/> no longer equals the English the game has is
/// <em>stale</em>: the scene was rewritten after it was translated, and the entry counts for nothing until the
/// translator has read it again.
/// </summary>
public sealed class LineEntry
{
    public List<string> En { get; set; } = new();
    public List<string> Text { get; set; } = new();

    /// <summary>
    /// Written by <c>TextTool extract --update</c> beside a stale entry: what the language said when the English was
    /// the old one, for the translator to read again. The game never reads it.
    /// </summary>
    public List<string>? Was { get; set; }

    /// <summary>Whether the English this entry was written against is not the English the game has now.</summary>
    public bool IsStale(IReadOnlyList<string> english) => !En.SequenceEqual(english, StringComparer.Ordinal);

    /// <summary>What the language says for line <paramref name="k"/>, or null where it says nothing.</summary>
    public string? Translation(int k) => k >= 0 && k < Text.Count && Text[k].Length > 0 ? Text[k] : null;
}

/// <summary>What a language says for one person of a place: their name and the three kinds of line they have.</summary>
public sealed class PersonTexts
{
    public LineEntry? Name { get; set; }
    /// <summary>What they say when spoken to (<c>sayown</c>).</summary>
    public LineEntry? Dialog { get; set; }
    /// <summary>A trainer's words before the battle and after it.</summary>
    public LineEntry? Before { get; set; }
    public LineEntry? After { get; set; }
}

/// <summary>
/// What a language says for a place of the world (an area's overlay) or a hand-made map: its name, its people by
/// their id (or by the tile they begin on, <c>12,7</c>, where the file gives none), its signs by their id (or tile).
/// </summary>
public sealed class PlaceTexts
{
    public LineEntry? DisplayName { get; set; }
    public Dictionary<string, PersonTexts> People { get; set; } = new();
    public Dictionary<string, LineEntry> Signs { get; set; } = new();
}

/// <summary>
/// The sibling files that translate the game's scripts, overlays and rooms (plan 17 · N4): for a language,
/// <c>Data/text/&lt;tag&gt;/scripts/&lt;file&gt;.json</c> (script name to the entries of its text-bearing lines in
/// <see cref="Story.Instruction.TextIndex"/> order), <c>world/&lt;key&gt;.json</c> for an area's overlay and
/// <c>maps/&lt;Name&gt;.json</c> for a hand-made map (both a <see cref="PlaceTexts"/>). The English files are
/// untouched; a file a language lacks is a language that says everything in English. GPU-free.
/// </summary>
public static class TranslationFiles
{
    public const string ScriptsFolder = "scripts", WorldFolder = "world", MapsFolder = "maps";

    private static readonly ConcurrentDictionary<string, object?> Loaded = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Where a file's text comes from, by its path under <c>Data</c> (null for a file that isn't there). The game's
    /// is the Data folder; a test that needs files of its own swaps it (and calls <see cref="Forget"/>).
    /// </summary>
    public static Func<string, string?> Reader { get; set; } = relative =>
    {
        string path = GameDataFiles.PathOf(relative);
        return File.Exists(path) ? File.ReadAllText(path) : null;
    };

    /// <summary>The files of a folder under <c>Data</c> (their names without the ending), where <see cref="Reader"/> has no way to list them.</summary>
    public static Func<string, IEnumerable<string>> Lister { get; set; } = relative =>
    {
        string path = GameDataFiles.PathOf(relative);
        return Directory.Exists(path) ? Directory.GetFiles(path, "*.json").Select(p => Path.GetFileNameWithoutExtension(p)!).Order(StringComparer.Ordinal).ToList() : new List<string>();
    };

    /// <summary>The names of the files a language has of one kind (<see cref="ScriptsFolder"/>, <see cref="WorldFolder"/> or <see cref="MapsFolder"/>).</summary>
    public static IReadOnlyList<string> Files(string tag, string kind) => Lister(Path.Combine("text", tag, kind)).ToList();

    /// <summary>Lets go of every file read, so the next lookup reads again.</summary>
    public static void Forget() => Loaded.Clear();

    /// <summary>The translations of one script file's scripts, or null where the language has none for it.</summary>
    public static Dictionary<string, List<LineEntry>>? Scripts(string tag, string file) =>
        Read<Dictionary<string, List<LineEntry>>>(Path.Combine("text", tag, ScriptsFolder, file + ".json"));

    /// <summary>The translations of a place, by its area's key or its map's name; null where the language has none for it.</summary>
    public static PlaceTexts? Place(string tag, string place) =>
        Read<PlaceTexts>(Path.Combine("text", tag, WorldFolder, place + ".json"))
        ?? Read<PlaceTexts>(Path.Combine("text", tag, MapsFolder, place + ".json"));

    /// <summary>The translations of a place from one kind of file only (<see cref="WorldFolder"/> or <see cref="MapsFolder"/>).</summary>
    public static PlaceTexts? Place(string tag, string place, string kind) =>
        Read<PlaceTexts>(Path.Combine("text", tag, kind, place + ".json"));

    private static T? Read<T>(string relative) where T : class
    {
        return (T?)Loaded.GetOrAdd(relative.Replace('\\', '/'), _ =>
        {
            string? text = Reader(relative);
            if (text == null) return null;
            try
            {
                return JsonSerializer.Deserialize<T>(text, GameDataFiles.Json);
            }
            catch (JsonException e)
            {
                throw new InvalidDataException($"Translation file {relative} is malformed: {e.Message}", e);
            }
        });
    }

    /// <summary>Whether <paramref name="relative"/> (a path under <c>Data</c>) is a file a language has.</summary>
    public static bool Exists(string relative) => Reader(relative) != null;
}
