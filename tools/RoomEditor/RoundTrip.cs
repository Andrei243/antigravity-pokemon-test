using System.Text;
using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace RoomEditor;

/// <summary>
/// The editor's own files held to it with no window (plan 16 · T9, <c>--check</c> and <c>--roundtrip</c>): every room
/// opened and saved by <see cref="EditorDocument"/> must be the text it was opened from, and every overlay opened by
/// <see cref="OverlayDocument"/> must be its text, and every field of every row written back with the value it shows
/// must leave the overlay meaning what it meant (the field's own parsing, the splice and the game's writing of each
/// record together). What differs is a line each: the file, and where and how.
/// </summary>
public static class RoundTrip
{
    /// <summary>A room's file as the editor opens and saves it.</summary>
    public static List<string> Room(string file, string text)
    {
        var found = new List<string>();
        text = text.Replace("\r\n", "\n");
        try
        {
            var document = EditorDocument.Open(text);
            if (document.Text != text) found.Add($"{file}: opened, it reads as other text ({FirstDifference(text, document.Text)})");
            string saved = document.SaveText();
            if (saved != text) found.Add($"{file}: saved, it is other text ({FirstDifference(text, saved)})");
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or ArgumentException or IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            found.Add($"{file}: the editor can't open it: {e.Message}");
        }
        return found;
    }

    /// <summary>An overlay as the editor opens it, and every field of every row written back as it shows.</summary>
    public static List<string> Overlay(string file, string key, WorldAreaFile area, string text)
    {
        var found = new List<string>();
        text = text.Replace("\r\n", "\n");
        OverlayDocument document;
        string meaning;
        try
        {
            document = new OverlayDocument(key, area, text);
            meaning = GameDataFiles.Serialize(document.Overlay);
        }
        catch (JsonException e)
        {
            found.Add($"{file}: it can't be read: {e.Message}");
            return found;
        }
        if (document.Text != text) found.Add($"{file}: opened, it reads as other text");

        foreach (var row in document.Rows())
            foreach (var field in document.Fields(row.Kind, row.Key))
            {
                // A field with nothing in it writes nothing (an entry left out stays out), so only what is there is written back
                if (field.Value.Length == 0) continue;
                try
                {
                    document.SetField(row.Kind, row.Key, field.Label, field.Value);
                }
                catch (Exception e) when (e is ArgumentException or FormatException or OverflowException or JsonException)
                {
                    found.Add($"{file}: {row.Kind} {row.Key}'s {field.Label} can't be written back as it shows: {e.Message}");
                    continue;
                }
                string now = GameDataFiles.Serialize(document.Overlay);
                if (now != meaning)
                {
                    found.Add($"{file}: {row.Kind} {row.Key}'s {field.Label} written back as it shows (\"{Short(field.Value)}\") changes the overlay ({FirstDifference(meaning, now)})");
                    meaning = now;
                }
            }
        return found;
    }

    /// <summary>Every room of a data folder's <c>maps/</c> and every overlay of its worlds; how many files were read.</summary>
    public static (int Files, List<string> Found) All(string dataFolder)
    {
        int files = 0;
        var found = new List<string>();
        string rooms = Path.Combine(dataFolder, MapDatabase.Folder);
        if (Directory.Exists(rooms))
            foreach (string path in Directory.GetFiles(rooms, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                files++;
                found.AddRange(Room(Relative(dataFolder, path), File.ReadAllText(path)));
            }
        foreach (var (path, key, area) in Overlays(dataFolder))
        {
            files++;
            found.AddRange(area == null
                ? new[] { $"{Relative(dataFolder, path)}: no area file of its region is called {key}" }
                : Overlay(Relative(dataFolder, path), key, area, File.ReadAllText(path)));
        }
        return (files, found);
    }

    /// <summary>One file, a room's or an overlay's (by the folder it is in).</summary>
    public static List<string> One(string dataFolder, string path)
    {
        if (!File.Exists(path)) return new() { $"{path}: there is no such file" };
        string folder = Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(path))!);
        if (folder == "overlays")
        {
            string key = Path.GetFileNameWithoutExtension(path);
            string areaPath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath(path))!)!, "areas", key + ".json");
            if (!File.Exists(areaPath)) return new() { $"{path}: no area file of its region is called {key}" };
            return Overlay(path, key, GameDataFiles.Deserialize<WorldAreaFile>(File.ReadAllText(areaPath)), File.ReadAllText(path));
        }
        return Room(path, File.ReadAllText(path));
    }

    private static IEnumerable<(string Path, string Key, WorldAreaFile? Area)> Overlays(string dataFolder)
    {
        string root = Path.Combine(dataFolder, World.Folder);
        if (!Directory.Exists(root)) yield break;
        foreach (string region in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
        {
            string folder = Path.Combine(region, "overlays");
            if (!Directory.Exists(folder)) continue;
            foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                string key = Path.GetFileNameWithoutExtension(path), areaPath = Path.Combine(region, "areas", key + ".json");
                yield return (path, key, File.Exists(areaPath) ? GameDataFiles.Deserialize<WorldAreaFile>(File.ReadAllText(areaPath)) : null);
            }
        }
    }

    private static string Relative(string root, string path) => Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string Short(string text) => text.Length > 30 ? text[..30] + "..." : text;

    /// <summary>The first line two texts differ on, each side's.</summary>
    public static string FirstDifference(string a, string b)
    {
        string[] la = a.Split('\n'), lb = b.Split('\n');
        for (int i = 0; i < Math.Max(la.Length, lb.Length); i++)
        {
            string x = i < la.Length ? la[i] : "(nothing)", y = i < lb.Length ? lb[i] : "(nothing)";
            if (x != y) return $"line {i + 1}: \"{Short(x.Trim())}\" became \"{Short(y.Trim())}\"";
        }
        return "the same lines";
    }
}
