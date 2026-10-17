using System.Text;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// Every line of the game's own files that a language may translate, and what a language has of them (plan 17 · N4):
/// the scripts' <c>say</c>, <c>text</c>, <c>ask</c>, <c>choose</c> and <c>speaker</c> lines, the people's names and
/// lines, the signs and the rooms' names of the overlays and the hand-made maps. <c>tools/TextTool</c> writes a
/// language's skeleton files and the report from here, and the tests hold them to the same. GPU-free.
/// </summary>
public static class TextSources
{
    /// <summary>The kinds of translation file, which are the names of their folders under a language's.</summary>
    public static readonly IReadOnlyList<string> Kinds = new[] { TranslationFiles.ScriptsFolder, TranslationFiles.WorldFolder, TranslationFiles.MapsFolder };

    // ------------------------------------------------------------------ the English side

    /// <summary>
    /// The scripts of every file, as the entries a translation lists (English filled in, no text yet): file name to
    /// script name to its text-bearing instructions in <see cref="Instruction.TextIndex"/> order. A file or a script
    /// with nothing to say is left out.
    /// </summary>
    public static SortedDictionary<string, Dictionary<string, List<LineEntry>>> Scripts(ScriptLibrary? library = null)
    {
        library ??= ScriptLibrary.Default;
        var files = new SortedDictionary<string, Dictionary<string, List<LineEntry>>>(StringComparer.Ordinal);
        foreach (var script in library.All.OrderBy(s => s.File, StringComparer.Ordinal).ThenBy(s => s.Line))
        {
            var entries = script.Everything().Where(i => i.SpeaksText).OrderBy(i => i.TextIndex)
                .Select(i => new LineEntry { En = i.Words.ToList(), Text = i.Words.Select(_ => "").ToList() }).ToList();
            if (entries.Count == 0) continue;
            if (!files.TryGetValue(script.File, out var scripts)) files[script.File] = scripts = new Dictionary<string, List<LineEntry>>(StringComparer.Ordinal);
            scripts[script.Name] = entries;
        }
        return files;
    }

    /// <summary>The overlays of every open area (<c>world/&lt;key&gt;.json</c>), by the area's key.</summary>
    public static SortedDictionary<string, PlaceTexts> Overlays()
    {
        var places = new SortedDictionary<string, PlaceTexts>(StringComparer.Ordinal);
        string root = GameDataFiles.PathOf(World.Folder);
        if (!Directory.Exists(root)) return places;
        foreach (string region in Directory.GetDirectories(root).OrderBy(p => p, StringComparer.Ordinal))
        {
            string folder = Path.Combine(region, "overlays");
            if (!Directory.Exists(folder)) continue;
            foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                var overlay = System.Text.Json.JsonSerializer.Deserialize<WorldOverlayFile>(File.ReadAllText(path), GameDataFiles.Json);
                if (overlay == null) continue;
                var place = new PlaceTexts();
                if (!string.IsNullOrEmpty(overlay.Name)) place.DisplayName = One(overlay.Name);
                foreach (var (id, person) in overlay.People ?? new())
                    Add(place, id, person.Name, person.Dialog, person.Trainer?.DialogueBefore, person.Trainer?.DialogueAfter);
                foreach (var record in overlay.Npcs ?? new())
                    Add(place, record.Id ?? $"{record.X},{record.Y}", record.Name, record.Dialog, record.Trainer?.DialogueBefore, record.Trainer?.DialogueAfter);
                foreach (var (id, text) in overlay.Signs ?? new())
                    if (!string.IsNullOrEmpty(text)) place.Signs[id] = One(text);
                if (place.DisplayName != null || place.People.Count > 0 || place.Signs.Count > 0) places[Path.GetFileNameWithoutExtension(path)] = place;
            }
        }
        return places;
    }

    /// <summary>The hand-made maps (<c>maps/&lt;Name&gt;.json</c>), by the map's name.</summary>
    public static SortedDictionary<string, PlaceTexts> Maps()
    {
        var places = new SortedDictionary<string, PlaceTexts>(StringComparer.Ordinal);
        string folder = GameDataFiles.PathOf(MapDatabase.Folder);
        foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, StringComparer.Ordinal))
        {
            var map = System.Text.Json.JsonSerializer.Deserialize<MapFile>(File.ReadAllText(path), GameDataFiles.Json);
            if (map == null) continue;
            var place = new PlaceTexts();
            if (!string.IsNullOrEmpty(map.DisplayName)) place.DisplayName = One(map.DisplayName);
            foreach (var n in map.Npcs)
                Add(place, n.Id ?? $"{n.X},{n.Y}", n.Name, n.Dialog, n.Trainer?.DialogueBefore, n.Trainer?.DialogueAfter);
            foreach (var s in map.Signboards)
                if (!string.IsNullOrEmpty(s.Text)) place.Signs[$"{s.X},{s.Y}"] = One(s.Text);
            if (place.DisplayName != null || place.People.Count > 0 || place.Signs.Count > 0) places[Path.GetFileNameWithoutExtension(path)] = place;
        }
        return places;
    }

    private static LineEntry One(string line) => new() { En = new List<string> { line }, Text = new List<string> { "" } };
    private static LineEntry Many(IEnumerable<string> lines) { var en = lines.ToList(); return new LineEntry { En = en, Text = en.Select(_ => "").ToList() }; }

    private static void Add(PlaceTexts place, string key, string name, IEnumerable<string>? dialog, string? before, string? after)
    {
        var person = new PersonTexts();
        if (!string.IsNullOrEmpty(name)) person.Name = One(name);
        if (dialog != null && dialog.Any()) person.Dialog = Many(dialog);
        if (!string.IsNullOrEmpty(before)) person.Before = One(before);
        if (!string.IsNullOrEmpty(after)) person.After = One(after);
        if (person.Name != null || person.Dialog != null || person.Before != null || person.After != null) place.People[key] = person;
    }

    // ------------------------------------------------------------------ flattening, merging and counting

    /// <summary>Every entry of a file by a path that names it: <c>Nurse#2</c> for a script's third line, <c>people/mom/dialog</c>, <c>signs/12,7</c>, <c>displayName</c>.</summary>
    public static Dictionary<string, LineEntry> Flatten(Dictionary<string, List<LineEntry>> scripts)
    {
        var flat = new Dictionary<string, LineEntry>(StringComparer.Ordinal);
        foreach (var (name, entries) in scripts)
            for (int i = 0; i < entries.Count; i++) flat[$"{name}#{i}"] = entries[i];
        return flat;
    }

    public static Dictionary<string, LineEntry> Flatten(PlaceTexts place)
    {
        var flat = new Dictionary<string, LineEntry>(StringComparer.Ordinal);
        if (place.DisplayName != null) flat["displayName"] = place.DisplayName;
        foreach (var (key, person) in place.People)
        {
            if (person.Name != null) flat[$"people/{key}/name"] = person.Name;
            if (person.Dialog != null) flat[$"people/{key}/dialog"] = person.Dialog;
            if (person.Before != null) flat[$"people/{key}/before"] = person.Before;
            if (person.After != null) flat[$"people/{key}/after"] = person.After;
        }
        foreach (var (key, sign) in place.Signs) flat[$"signs/{key}"] = sign;
        return flat;
    }

    /// <summary>
    /// What a translation of a file comes to against its English: the lines there are, those it translates, those
    /// whose entry was written against other English (stale) and those of entries that match nothing now (unknown).
    /// </summary>
    public readonly record struct Counts(int Lines, int Translated, int Stale, int Unknown)
    {
        public int Untranslated => Lines - Translated - Stale;
        public static Counts operator +(Counts a, Counts b) => new(a.Lines + b.Lines, a.Translated + b.Translated, a.Stale + b.Stale, a.Unknown + b.Unknown);
    }

    public static Counts Count(Dictionary<string, LineEntry> english, Dictionary<string, LineEntry>? translation)
    {
        int lines = 0, translated = 0, stale = 0, unknown = 0;
        foreach (var (path, source) in english)
        {
            lines += source.En.Count;
            if (translation == null || !translation.TryGetValue(path, out var entry)) continue;
            if (entry.IsStale(source.En)) { stale += source.En.Count; continue; }
            for (int k = 0; k < source.En.Count; k++) if (entry.Translation(k) != null) translated++;
        }
        if (translation != null)
            foreach (var (path, entry) in translation)
                if (!english.ContainsKey(path)) unknown += entry.En.Count;
        return new Counts(lines, translated, stale, unknown);
    }

    /// <summary>
    /// The file a language would have for a source, kept up to date: each entry the language has that still matches
    /// its English stays as written; a new one is empty, and a stale one takes the new English with the old text
    /// beside it (<see cref="LineEntry.Was"/>) for the translator to read again. Entries that match nothing are dropped.
    /// </summary>
    public static Dictionary<string, List<LineEntry>> Merge(Dictionary<string, List<LineEntry>> english, Dictionary<string, List<LineEntry>>? existing)
    {
        var merged = new Dictionary<string, List<LineEntry>>(StringComparer.Ordinal);
        foreach (var (name, entries) in english)
        {
            List<LineEntry>? old = null;
            existing?.TryGetValue(name, out old);
            merged[name] = entries.Select((source, i) => Carry(source, old != null && i < old.Count ? old[i] : null)!).ToList();
        }
        return merged;
    }

    public static PlaceTexts Merge(PlaceTexts english, PlaceTexts? existing)
    {
        var merged = new PlaceTexts { DisplayName = Carry(english.DisplayName, existing?.DisplayName) };
        foreach (var (key, person) in english.People)
        {
            PersonTexts? old = null;
            existing?.People.TryGetValue(key, out old);
            merged.People[key] = new PersonTexts
            {
                Name = Carry(person.Name, old?.Name), Dialog = Carry(person.Dialog, old?.Dialog),
                Before = Carry(person.Before, old?.Before), After = Carry(person.After, old?.After)
            };
        }
        foreach (var (key, sign) in english.Signs)
        {
            LineEntry? old = null;
            existing?.Signs.TryGetValue(key, out old);
            merged.Signs[key] = Carry(sign, old)!;
        }
        return merged;
    }

    private static LineEntry? Carry(LineEntry? source, LineEntry? old)
    {
        if (source == null) return null;
        if (old == null) return source;
        if (!old.IsStale(source.En))
        {
            var kept = new LineEntry { En = source.En, Text = old.Text.Take(source.En.Count).ToList() };
            while (kept.Text.Count < source.En.Count) kept.Text.Add("");
            return kept;
        }
        return new LineEntry { En = source.En, Text = source.Text, Was = old.Text.Any(t => t.Length > 0) ? old.Text : null };
    }

    // ------------------------------------------------------------------ the report

    /// <summary>
    /// <c>docs/localisation.md</c>: what there is to translate, and for each language what it has of the interface, the
    /// names and each kind of file. Written by <c>tools/TextTool report</c>; <c>TextTests</c> holds it true.
    /// </summary>
    public static string Report()
    {
        var scripts = Scripts();
        var overlays = Overlays();
        var maps = Maps();
        var scriptLines = scripts.ToDictionary(f => f.Key, f => Flatten(f.Value));
        var overlayLines = overlays.ToDictionary(f => f.Key, f => Flatten(f.Value));
        var mapLines = maps.ToDictionary(f => f.Key, f => Flatten(f.Value));

        int Lines(IEnumerable<Dictionary<string, LineEntry>> files) => files.Sum(f => f.Values.Sum(e => e.En.Count));
        var english = Text.EnglishKeys;

        var report = new StringBuilder();
        report.AppendLine("# Localisation");
        report.AppendLine();
        report.AppendLine("Written by `dotnet run --project tools/TextTool -- report` (plan 17 · N4); `ScriptTranslationTests` fails when this is not what the files give. Do not edit by hand.");
        report.AppendLine();
        report.AppendLine("A line is one string a language may translate. A line is **translated** when its entry has text and the entry's `en` is the English the game has now; **stale** when the English was rewritten after the entry was written (it counts as untranslated, and the game shows the English); **unknown** when the entry matches nothing the game has any more.");
        report.AppendLine();
        report.AppendLine("## What there is to translate");
        report.AppendLine();
        report.AppendLine("| Kind | Files | Lines |");
        report.AppendLine("|---|---|---|");
        report.AppendLine($"| Scripts (`scripts/<file>.json`) | {scripts.Count} | {Lines(scriptLines.Values)} |");
        report.AppendLine($"| Overlays (`world/<key>.json`) | {overlays.Count} | {Lines(overlayLines.Values)} |");
        report.AppendLine($"| Hand-made maps (`maps/<Name>.json`) | {maps.Count} | {Lines(mapLines.Values)} |");
        report.AppendLine();
        report.AppendLine("## Languages");
        report.AppendLine();
        report.AppendLine("Each cell of a kind of file is translated / lines; a language with no file for a kind shows 0. `ui.json` is the number of keys the language's own table has (the interface's keys are not counted here: they change with every screen). Names are `names.json` (PokeAPI's, plan 17 · N3).");
        report.AppendLine();
        report.AppendLine("| Language | `ui.json` | Names | Scripts | Overlays | Maps | Stale | Unknown |");
        report.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var language in Language.All.Where(l => l.Tag != Text.English && !l.Pseudo))
        {
            string tag = language.Tag;
            int own = Text.KeysOf(tag).Count(english.Contains);
            var s = Tally(scriptLines, file => TranslationFiles.Scripts(tag, file) is { } t ? Flatten(t) : null);
            var o = Tally(overlayLines, key => TranslationFiles.Place(tag, key, TranslationFiles.WorldFolder) is { } t ? Flatten(t) : null);
            var m = Tally(mapLines, name => TranslationFiles.Place(tag, name, TranslationFiles.MapsFolder) is { } t ? Flatten(t) : null);
            string names = LocalNames.Has(tag) ? "yes" : "no";
            report.AppendLine($"| {language.Name} (`{tag}`) | {own} | {names} | {s.Translated} / {s.Lines} | {o.Translated} / {o.Lines} | {m.Translated} / {m.Lines} | {s.Stale + o.Stale + m.Stale} | {s.Unknown + o.Unknown + m.Unknown} |");
        }
        report.AppendLine();
        report.AppendLine("The overlays and maps are found by the key or name of the file; a place both an overlay and a map name is looked for as an overlay first.");
        return report.ToString().Replace("\r\n", "\n");
    }

    private static Counts Tally(Dictionary<string, Dictionary<string, LineEntry>> english, Func<string, Dictionary<string, LineEntry>?> translation)
    {
        var total = default(Counts);
        foreach (var (file, lines) in english) total += Count(lines, translation(file));
        return total;
    }

    /// <summary>The files a language has under a kind's folder that match no source file at all (a misspelt name, a script file that was renamed).</summary>
    public static IReadOnlyList<string> UnknownFiles(string tag, string kind, IReadOnlyCollection<string> known) =>
        TranslationFiles.Files(tag, kind).Where(name => !known.Contains(name)).OrderBy(n => n, StringComparer.Ordinal).ToList();

    // ------------------------------------------------------------------ what is wrong with a language's files

    // Every {...} of a line. A placeholder the runner fills ({player}, {var:VAR_X}) is held whole; any other code
    // (plan 09 · L3's markup: {red:...}, {pause 0.4}) by its name alone, since what it wraps is the translator's to write
    private static readonly System.Text.RegularExpressions.Regex Braces = new(@"\{([^{}]*)\}", System.Text.RegularExpressions.RegexOptions.Compiled);
    private static readonly System.Text.RegularExpressions.Regex Placeholder = new(@"^[a-z]+(?::[A-Z0-9_]+)?$", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>The placeholders and markup codes of a line (see above).</summary>
    public static IReadOnlyList<string> CodesOf(string line) =>
        Braces.Matches(line).Select(m =>
        {
            string inner = m.Groups[1].Value;
            if (Placeholder.IsMatch(inner)) return m.Value;
            int end = inner.IndexOfAny(new[] { ':', ' ' });
            return "{" + (end < 0 ? inner : inner[..end]) + "...}";
        }).ToList();

    /// <summary>
    /// What is wrong with the files a language has, one line each (none when they are as they should be): a file that
    /// is no script file, overlay or map of the game, an entry that matches no line (a script, a person or a sign
    /// that isn't there), an entry whose text has not as many lines as its English, and a translation that uses a
    /// placeholder or a markup code its English line does not have. A stale entry is no problem here (the report counts
    /// it, the game shows the English), but its text is not looked at either.
    /// </summary>
    public static IReadOnlyList<string> Problems(string tag)
    {
        var problems = new List<string>();
        var scripts = Scripts();
        CheckKind(TranslationFiles.ScriptsFolder, scripts.Keys, file => TranslationFiles.Scripts(tag, file) is { } t ? Flatten(t) : null, file => Flatten(scripts[file]));
        var overlays = Overlays();
        CheckKind(TranslationFiles.WorldFolder, overlays.Keys, key => PlaceOf(TranslationFiles.WorldFolder, key), key => Flatten(overlays[key]));
        var maps = Maps();
        CheckKind(TranslationFiles.MapsFolder, maps.Keys, name => PlaceOf(TranslationFiles.MapsFolder, name), name => Flatten(maps[name]));
        return problems;

        Dictionary<string, LineEntry>? PlaceOf(string kind, string name) =>
            TranslationFiles.Place(tag, name, kind) is { } t ? Flatten(t) : null;

        void CheckKind(string kind, IReadOnlyCollection<string> known, Func<string, Dictionary<string, LineEntry>?> translation, Func<string, Dictionary<string, LineEntry>> english)
        {
            foreach (string file in TranslationFiles.Files(tag, kind))
            {
                string where = $"text/{tag}/{kind}/{file}.json";
                if (!known.Contains(file)) { problems.Add($"{where}: matches no {(kind == TranslationFiles.ScriptsFolder ? "script file" : kind == TranslationFiles.WorldFolder ? "area overlay" : "map")} of the game"); continue; }
                var found = translation(file);
                if (found == null) continue;
                var source = english(file);
                foreach (var (path, entry) in found)
                {
                    if (!source.TryGetValue(path, out var wanted)) { problems.Add($"{where}: {path} matches no line of the game"); continue; }
                    if (entry.IsStale(wanted.En)) continue;
                    if (entry.Text.Count != wanted.En.Count) { problems.Add($"{where}: {path} has {entry.Text.Count} lines of text for {wanted.En.Count} of English"); continue; }
                    for (int k = 0; k < wanted.En.Count; k++)
                    {
                        if (entry.Text[k].Length == 0) continue;
                        var allowed = CodesOf(wanted.En[k]);
                        foreach (string code in CodesOf(entry.Text[k]).Where(c => !allowed.Contains(c)))
                            problems.Add($"{where}: {path}, line {k + 1}: uses {code}, which its English line does not have");
                    }
                }
            }
        }
    }
}
