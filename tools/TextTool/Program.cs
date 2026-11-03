// Writes the skeleton of a language's translation files and reports what a language still lacks (plan 17 · N4).
// See tools/TextTool/README.md.
//
//   dotnet run --project tools/TextTool -- extract <tag> [--update]     the language's scripts/, world/ and maps/ files, every English line and an empty text
//   dotnet run --project tools/TextTool -- report [tag] [--lines]       what is untranslated, stale or unknown, per file; rewrites docs/localisation.md
//   dotnet run --project tools/TextTool -- report --check               ends with 1 if docs/localisation.md is not what the files give

using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

string? repo = FindRepository();
if (repo == null)
{
    Console.Error.WriteLine("Run it from inside the repository (the folder with PokemonPlatinum.sln).");
    return 2;
}
string sourceData = Path.Combine(repo, "PokemonPlatinumEngine", "Data");

// The translation files are read from the repository, not from the build output copy, so what `extract` just wrote counts
TranslationFiles.Reader = relative =>
{
    string path = Path.Combine(sourceData, relative);
    return File.Exists(path) ? File.ReadAllText(path) : null;
};

TranslationFiles.Lister = relative =>
{
    string path = Path.Combine(sourceData, relative);
    return Directory.Exists(path) ? Directory.GetFiles(path, "*.json").Select(p => Path.GetFileNameWithoutExtension(p)!).Order(StringComparer.Ordinal).ToList() : new List<string>();
};

var words = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
var switches = args.Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToHashSet();
if (words.Count == 0 || words[0] is not ("extract" or "report" or "font"))
{
    Console.Error.WriteLine("Say extract <tag> [--update], report [tag] [--lines] [--check] or font <Noto Sans SC Bold .ttf> <Noto Sans KR Bold .ttf>.");
    return 2;
}

return words[0] switch { "extract" => Extract(), "font" => Font(), _ => Report() };

// The CJK face cut down to what the game's files use (plan 17 · N5): every CJK character of every file under
// Data/text (the names of Japanese, Korean and both Chinese, a language's ui.json and scripts once written,
// languages.json with each language's own name and the CJK pseudo-language's brackets) and CjkFace.Marks
int Font()
{
    if (words.Count < 2 || words.Skip(1).Any(w => !File.Exists(w)))
    {
        Console.Error.WriteLine("font needs the whole faces to cut, in order: font <NotoSansSC-Bold.ttf> <NotoSansKR-Bold.ttf> (docs/art/CREDITS.md says where they come from).");
        return 2;
    }
    var wanted = new SortedSet<int>(PokemonPlatinumEngine.UI.Kit.CjkFace.Marks);
    string textFolder = Path.Combine(sourceData, "text");
    foreach (string path in Directory.EnumerateFiles(textFolder, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        foreach (int c in PokemonPlatinumEngine.UI.Kit.CjkFace.CjkIn(File.ReadAllText(path))) wanted.Add(c);
    var sources = words.Skip(1).Select(File.ReadAllBytes).ToList();
    var maps = sources.Select(FontSubset.Cmap).ToList();
    var missing = wanted.Where(c => !maps.Any(m => m.ContainsKey(c)) && !(c is >= 0x3000 and <= 0x30FF or >= 0x3131 and <= 0x318E or >= 0xFF01 and <= 0xFF5E)).ToList();
    var (cut, glyphs) = FontSubset.Subset(sources, wanted);
    string output = Path.Combine(repo, "PokemonPlatinumEngine", "Assets", "Fonts", PokemonPlatinumEngine.UI.Kit.CjkFace.FileName);
    File.WriteAllBytes(output, cut);
    Console.WriteLine($"{wanted.Count} codepoints asked for, {glyphs} glyphs kept, {cut.Length / 1024} KB written to {output} (the whole faces are {sources.Sum(f => f.Length) / 1024} KB).");
    if (missing.Count > 0)
        Console.WriteLine($"The face has no glyph for {missing.Count} of the files' characters: {string.Concat(missing.Take(40).Select(char.ConvertFromUtf32))}");
    return 0;
}

int Extract()
{
    if (words.Count != 2)
    {
        Console.Error.WriteLine("extract needs the language's tag: extract <tag> [--update].");
        return 2;
    }
    string tag = words[1];
    if (Language.All.FirstOrDefault(l => l.Tag == tag) is not { } language || language.Tag == Text.English || language.Pseudo)
    {
        Console.Error.WriteLine($"'{tag}' is no language to translate into: the tags are {string.Join(", ", Language.All.Where(l => l.Tag != Text.English && !l.Pseudo).Select(l => l.Tag))}.");
        return 2;
    }
    bool update = switches.Contains("--update");
    int written = 0, kept = 0;

    foreach (var (file, scripts) in TextSources.Scripts())
        Write(TranslationFiles.ScriptsFolder, file, scripts, TextSources.Merge(scripts, TranslationFiles.Scripts(tag, file)));
    foreach (var (key, place) in TextSources.Overlays())
        Write(TranslationFiles.WorldFolder, key, place, TextSources.Merge(place, TranslationFiles.Place(tag, key, TranslationFiles.WorldFolder)));
    foreach (var (name, place) in TextSources.Maps())
        Write(TranslationFiles.MapsFolder, name, place, TextSources.Merge(place, TranslationFiles.Place(tag, name, TranslationFiles.MapsFolder)));

    Console.WriteLine($"{written} files written, {kept} left as they are (add --update to bring them up to date with the English).");
    return 0;

    void Write<T>(string folder, string name, T english, T merged)
    {
        string path = Path.Combine(sourceData, "text", tag, folder, name + ".json");
        bool exists = File.Exists(path);
        if (exists && !update) { kept++; return; }
        string text = GameDataFiles.Serialize(merged);
        if (exists && File.ReadAllText(path).Replace("\r\n", "\n") == text) { kept++; return; }
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
        written++;
    }
}

int Report()
{
    string tag = words.Count > 1 ? words[1] : "";
    if (tag.Length > 0) ListLanguage(tag, switches.Contains("--lines"));
    else
        foreach (var language in Language.All.Where(l => l.Tag != Text.English && !l.Pseudo))
            if (HasFiles(language.Tag)) ListLanguage(language.Tag, false);

    string docs = Path.Combine(repo, "docs", "localisation.md");
    string report = TextSources.Report();
    if (switches.Contains("--check"))
    {
        bool same = File.Exists(docs) && File.ReadAllText(docs).Replace("\r\n", "\n") == report;
        Console.WriteLine(same ? "docs/localisation.md is what the files give." : "docs/localisation.md is not what the files give: run report without --check.");
        return same ? 0 : 1;
    }
    File.WriteAllText(docs, report);
    Console.WriteLine("docs/localisation.md written.");
    return 0;
}

bool HasFiles(string tag) => TextSources.Kinds.Any(kind => Directory.Exists(Path.Combine(sourceData, "text", tag, kind)));

void ListLanguage(string tag, bool lines)
{
    Console.WriteLine($"== {tag}");
    int gaps = 0;
    Gaps(TranslationFiles.ScriptsFolder, TextSources.Scripts().ToDictionary(f => f.Key, f => TextSources.Flatten(f.Value)),
        file => TranslationFiles.Scripts(tag, file) is { } t ? TextSources.Flatten(t) : null);
    Gaps(TranslationFiles.WorldFolder, TextSources.Overlays().ToDictionary(f => f.Key, f => TextSources.Flatten(f.Value)),
        key => TranslationFiles.Place(tag, key, TranslationFiles.WorldFolder) is { } t ? TextSources.Flatten(t) : null);
    Gaps(TranslationFiles.MapsFolder, TextSources.Maps().ToDictionary(f => f.Key, f => TextSources.Flatten(f.Value)),
        name => TranslationFiles.Place(tag, name, TranslationFiles.MapsFolder) is { } t ? TextSources.Flatten(t) : null);
    foreach (string problem in TextSources.Problems(tag))
    {
        gaps++;
        Console.WriteLine("wrong: " + problem);
    }
    if (gaps == 0) Console.WriteLine("nothing untranslated, stale, unknown or wrong.");

    void Gaps(string kind, Dictionary<string, Dictionary<string, LineEntry>> english, Func<string, Dictionary<string, LineEntry>?> translation)
    {
        foreach (var (file, source) in english)
        {
            var found = translation(file);
            var counts = TextSources.Count(source, found);
            if (counts.Untranslated == 0 && counts.Stale == 0 && counts.Unknown == 0) continue;
            gaps++;
            Console.WriteLine($"{kind}/{file}.json: {counts.Translated} of {counts.Lines} translated, {counts.Untranslated} untranslated, {counts.Stale} stale, {counts.Unknown} unknown");
            if (!lines) continue;
            foreach (var (path, entry) in source)
            {
                var own = found != null && found.TryGetValue(path, out var e) ? e : null;
                if (own == null) Console.WriteLine($"    untranslated  {path}");
                else if (own.IsStale(entry.En)) Console.WriteLine($"    stale         {path}");
                else if (Enumerable.Range(0, entry.En.Count).Any(k => own.Translation(k) == null)) Console.WriteLine($"    untranslated  {path}");
            }
            if (found != null)
                foreach (var path in found.Keys.Where(p => !source.ContainsKey(p)))
                    Console.WriteLine($"    unknown       {path}");
        }
        foreach (string name in TextSources.UnknownFiles(tag, kind, english.Keys.ToHashSet()))
        {
            gaps++;
            Console.WriteLine($"{kind}/{name}.json: matches no file of the game");
        }
    }
}

static string? FindRepository()
{
    for (string? dir = Directory.GetCurrentDirectory(); dir != null; dir = Path.GetDirectoryName(dir))
        if (File.Exists(Path.Combine(dir, "PokemonPlatinum.sln"))) return dir;
    for (string? dir = AppContext.BaseDirectory; dir != null; dir = Path.GetDirectoryName(dir))
        if (File.Exists(Path.Combine(dir, "PokemonPlatinum.sln"))) return dir;
    return null;
}
