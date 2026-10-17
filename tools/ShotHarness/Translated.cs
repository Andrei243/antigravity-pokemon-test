// How a mode knows the line it waits for when the game speaks another language (plan 17 · N6): the chapter modes
// read a scene on to a line by a piece of its English, which a translated line doesn't hold. Under English this is
// the piece itself and nothing else, so no mode's pictures move; under another language it is also the longest
// stretch without a placeholder of every translated line whose English holds the piece (the language's own files
// of the scripts, the overlays and the rooms: Data/text/<tag>/scripts, world and maps).

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

static class Translated
{
    static string? indexed;
    static List<(string En, string Own)> lines = new();
    static readonly Dictionary<string, List<string>> probes = new(StringComparer.Ordinal);

    /// <summary>Whether the text box's line is the one whose English holds <paramref name="english"/>.</summary>
    public static bool Saying(string line, string english)
    {
        if (line.Contains(english)) return true;
        if (Text.Current == Text.English) return false;
        foreach (string probe in ProbesFor(english))
            if (line.Contains(probe)) return true;
        return false;
    }

    static List<string> ProbesFor(string english)
    {
        if (indexed != Text.Current) { Index(Text.Current); indexed = Text.Current; probes.Clear(); }
        if (probes.TryGetValue(english, out var known)) return known;
        var found = new List<string>();
        foreach (var (en, own) in lines)
            if (en.Contains(english) && Probe(own) is { Length: > 0 } probe && !found.Contains(probe)) found.Add(probe);
        return probes[english] = found;
    }

    // What the box shows of a line whatever fills its placeholders: the whole line where it has none ("Bum!!"), else
    // its longest stretch between them, if that is long enough not to turn up in another line by chance
    static string Probe(string own)
    {
        if (!own.Contains('{')) return own.Trim();
        string longest = Regex.Split(own, @"\{[^}]*\}").Select(s => s.Trim()).OrderByDescending(s => s.Length).FirstOrDefault() ?? "";
        return longest.Length >= 6 ? longest : "";
    }

    static void Index(string tag)
    {
        lines = new();
        foreach (string folder in new[] { TranslationFiles.ScriptsFolder, TranslationFiles.WorldFolder, TranslationFiles.MapsFolder })
        {
            string dir = GameDataFiles.PathOf(Path.Combine("text", tag, folder));
            if (!Directory.Exists(dir)) continue;
            foreach (string file in Directory.GetFiles(dir, "*.json"))
                Walk(JsonNode.Parse(File.ReadAllText(file)));
        }
    }

    static void Walk(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject o when o["en"] is JsonArray en && o["text"] is JsonArray text:
                for (int k = 0; k < en.Count && k < text.Count; k++)
                    if (en[k]?.GetValue<string>() is { } e && text[k]?.GetValue<string>() is { Length: > 0 } t)
                        foreach (string row in t.Split('\n')) lines.Add((e, row));
                break;
            case JsonObject o:
                foreach (var (_, child) in o) Walk(child);
                break;
            case JsonArray a:
                foreach (var child in a) Walk(child);
                break;
        }
    }
}
