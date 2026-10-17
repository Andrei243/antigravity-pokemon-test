using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace ContentCheck;

/// <summary>
/// The translations of the scripts, the overlays and the rooms (<c>text/&lt;tag&gt;/scripts</c>, <c>world</c> and
/// <c>maps</c>, plan 17 · N4 and N6): every line a language has written is held to the text box as the English one is
/// (<see cref="Lines"/>), with the same speakers dropped from its start, each by the name the language gives them, and
/// every answer and name to what the interface can draw. A line still untranslated, or stale, is the English one,
/// which the rest of the check holds already; the report (<c>tools/TextTool</c>) counts what is missing.
/// </summary>
public static class Translations
{
    public static void Check(GameContent content, List<Finding> found)
    {
        string text = Path.Combine(content.Root, "text");
        if (!Directory.Exists(text)) return;
        foreach (string dir in Directory.GetDirectories(text).OrderBy(d => d, StringComparer.Ordinal))
        {
            string tag = Path.GetFileName(dir);
            if (tag == "en") continue;
            CheckScripts(content, tag, Path.Combine(dir, TranslationFiles.ScriptsFolder), found);
            foreach (string kind in new[] { TranslationFiles.WorldFolder, TranslationFiles.MapsFolder })
                CheckPlaces(content, tag, kind, Path.Combine(dir, kind), found);
        }
    }

    private static T? Read<T>(GameContent content, string path, List<Finding> found) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), GameDataFiles.Json);
        }
        catch (JsonException e)
        {
            found.Add(new Finding(Kind.Unreadable, content.Relative(path), Path.GetFileNameWithoutExtension(path), "the translation can't be read: " + e.Message));
            return null;
        }
    }

    /// <summary>A line of the entry as the language says it, or null where it says the English (none written, or stale).</summary>
    private static string? Own(LineEntry? entry, IReadOnlyList<string> english, int k) =>
        entry == null || entry.IsStale(english) ? null : entry.Translation(k);

    private static void CheckScripts(GameContent content, string tag, string folder, List<Finding> found)
    {
        if (!Directory.Exists(folder)) return;
        var byFile = content.Scripts.All.GroupBy(s => s.File).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            string file = Path.GetFileNameWithoutExtension(path);
            if (!byFile.TryGetValue(file, out var scripts)) continue;
            var entries = Read<Dictionary<string, List<LineEntry>>>(content, path, found);
            if (entries == null) continue;
            string where = content.Relative(path);
            foreach (var script in scripts)
            {
                if (!entries.TryGetValue(script.Name, out var list)) continue;
                LineEntry? EntryOf(Instruction i) => i.TextIndex >= 0 && i.TextIndex < list.Count ? list[i.TextIndex] : null;
                var all = script.Everything().ToList();
                // The names a line may begin with: each speaker as the language names them, and as English does
                var speakers = all.Where(i => i.Op == Op.Speaker && i.Name.Length > 0)
                    .SelectMany(i => new[] { i.Name, Own(EntryOf(i), i.Words, 0) })
                    .Where(n => n != null).Distinct().ToArray();
                foreach (var i in all.Where(i => i.SpeaksText))
                {
                    var entry = EntryOf(i);
                    string name = $"{script.Name} #{i.TextIndex}";
                    var words = i.Words;
                    for (int k = 0; k < words.Count; k++)
                    {
                        string? own = Own(entry, words, k);
                        if (own == null) continue;
                        if (i.Op == Op.Speaker) Lines.CheckName(found, where, name, own);
                        else if (k > 0 && i.Op is Op.Ask or Op.Choose) Lines.CheckAnswer(found, where, name, own);
                        else Lines.Check(found, where, name, own, speakers);
                    }
                }
            }
        }
    }

    private static void CheckPlaces(GameContent content, string tag, string kind, string folder, List<Finding> found)
    {
        if (!Directory.Exists(folder)) return;
        foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            var place = Read<PlaceTexts>(content, path, found);
            if (place == null) continue;
            string where = content.Relative(path);
            if (place.DisplayName?.Translation(0) is { } shown) Lines.CheckName(found, where, "displayName", shown);
            foreach (var (id, person) in place.People)
            {
                string? named = person.Name?.Translation(0);
                string? english = person.Name?.En.FirstOrDefault();
                if (named != null) Lines.CheckName(found, where, id, named);
                foreach (var entry in new[] { person.Dialog, person.Before, person.After })
                    if (entry != null)
                        for (int k = 0; k < entry.Text.Count; k++)
                            if (entry.Translation(k) is { } own) Lines.Check(found, where, id, own, named, english);
            }
            foreach (var (id, sign) in place.Signs)
                for (int k = 0; k < sign.Text.Count; k++)
                    if (sign.Translation(k) is { } own) Lines.Check(found, where, id, own, "Sign");
        }
    }
}
