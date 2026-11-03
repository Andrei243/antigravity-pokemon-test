// The voice catalogue (plan 26 · V1). See tools/VoiceTool/README.md.

using System.Text;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;

string? repo = FindRepository();
if (repo == null)
{
    Console.Error.WriteLine("Run it from inside the repository (the folder with PokemonPlatinum.sln).");
    return 2;
}
string engine = Path.Combine(repo, "PokemonPlatinumEngine");
string linesFile = Path.Combine(engine, "Data", "voice", "lines.json");
string castPath = Path.Combine(engine, "Data", "voice", "cast.json");
string coverageFile = Path.Combine(repo, "docs", "voice", "coverage.md");

var words = args.Where(a => !a.StartsWith("--", StringComparison.Ordinal)).ToList();
var switches = args.Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToHashSet();
if (words.Count == 0 || words[0] is not ("catalogue" or "export" or "manifest" or "check" or "vowels"))
{
    Console.Error.WriteLine("Say catalogue [--add-missing] [--check], export <voice> [file], manifest [folder] [--language en], check [folder], or vowels [folder] [--voice <id>] [--all].");
    return 2;
}
return words[0] switch
{
    "catalogue" => Catalogue(),
    "export" => Export(),
    "manifest" => Manifest(),
    "check" => Check(),
    _ => Vowels()
};

// ------------------------------------------------------------------ voice packs (plan 26 · V2)

string LanguageOf() => args.SkipWhile(a => a != "--language").Skip(1).FirstOrDefault() ?? "en";

// A pack's folder: the one named, or voice/<language> at the repository's root (git ignores it, as the game's own beside it)
string PackFolder() => words.Count > 1 ? Path.GetFullPath(words[1]) : Path.Combine(repo, "voice", LanguageOf());

int Manifest()
{
    string folder = PackFolder();
    if (!Directory.Exists(folder)) { Console.Error.WriteLine($"{folder} is no folder."); return 2; }
    var catalogue = VoiceCatalogue.Parse(File.ReadAllText(linesFile));
    var pack = VoicePacks.Manifest(folder, catalogue, LanguageOf());
    File.WriteAllText(Path.Combine(folder, VoiceLibrary.PackFileName), GameDataFiles.Serialize(pack));
    Console.WriteLine($"{pack.Lines.Count} takes listed in {Path.Combine(folder, VoiceLibrary.PackFileName)}, each stamped with its line's English as the catalogue has it now.");
    int loose = Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Count(f => Path.GetFileName(f) != VoiceLibrary.PackFileName) - pack.Lines.Count;
    if (loose > 0) Console.WriteLine($"{loose} files name no line of the catalogue (or have no decoder) and were left out: a take is <voice>/<key>.wav, the key's colon written '+'.");
    return 0;
}

int Check()
{
    string folder = PackFolder();
    var pack = VoiceLibrary.Open(folder);
    if (pack == null) { Console.WriteLine($"No pack at {folder} (no pack.json): nothing to check."); return 0; }
    var catalogue = VoiceCatalogue.Parse(File.ReadAllText(linesFile));
    var (wrong, stale) = VoicePacks.Check(pack, catalogue);
    foreach (var f in wrong.Take(200)) Console.WriteLine($"wrong: {f.Key}: {f.What}");
    if (wrong.Count > 200) Console.WriteLine($"... and {wrong.Count - 200} more");
    List("stale (made from English that has changed since: the game never plays them until they are made again)", stale);
    Console.WriteLine($"{pack.Entries.Count} takes: {wrong.Count} wrong, {stale.Count} stale.");
    return wrong.Count > 0 ? 1 : 0;
}

int Vowels()
{
    string folder = PackFolder();
    string? voice = args.SkipWhile(a => a != "--voice").Skip(1).FirstOrDefault();
    if (voice == null && !switches.Contains("--all")) { Console.Error.WriteLine("vowels needs --voice <id> (rival, rowan, look:lass#2) or --all."); return 2; }
    var catalogue = VoiceCatalogue.Parse(File.ReadAllText(linesFile));
    int n = VoicePacks.WriteStandIns(folder, catalogue, LanguageOf(), (_, v) => voice == null || v == voice || v.StartsWith(voice + "#", StringComparison.Ordinal));
    Console.WriteLine($"{n} stand-in takes of synthesised vowels and their pack.json written in {folder}. Turn VOICES on in the options to hear them.");
    return 0;
}

int Catalogue()
{
    var cast = GameDataFiles.Deserialize<CastFile>(File.ReadAllText(castPath));
    bool check = switches.Contains("--check");
    var previous = File.Exists(linesFile) ? VoiceCatalogue.Parse(File.ReadAllText(linesFile)) : null;
    var built = VoiceCatalogue.Build(cast, previous);

    if (switches.Contains("--add-missing") && built.Unlisted.Count > 0)
    {
        foreach (var (name, where) in built.Unlisted)
        {
            string id = VoiceCast.Slug(name), unique = id;
            for (int n = 2; cast.Voices.ContainsKey(unique); n++) unique = id + "_" + n;
            cast.Voices[unique] = new CastVoice { Provisional = true, Note = "first spoken at " + where };
            cast.Names[name] = unique;
            Console.WriteLine($"cast: {name} is {unique} (provisional)");
        }
        File.WriteAllText(castPath, GameDataFiles.Serialize(cast));
        built = VoiceCatalogue.Build(cast, previous);
    }

    foreach (var (id, where) in built.UnknownIds) Console.WriteLine($"wrong: speaker id '{id}' at {where} is no voice of the cast");
    foreach (var (name, where) in built.Unlisted) Console.WriteLine($"wrong: speaker '{name}' (first at {where}) has no cast entry");

    var changes = VoiceCatalogue.Compare(previous, built);
    Console.WriteLine($"{built.Lines.Count} lines: {built.Lines.Count(l => l.Status == VoiceLine.StatusSpoken)} spoken, {built.Lines.Count(l => l.Status == VoiceLine.StatusShownOnly)} shown only.");
    List("new", changes.Added);
    List("stale (the English changed: its audio is not played until redone)", changes.Stale);
    List("recast (another voice speaks it now)", changes.Recast);
    List("gone (no line has this key any more)", changes.Orphaned);

    string lines = VoiceCatalogue.Serialize(built.Lines);
    string report = VoiceCatalogue.Report(built, cast);
    bool problems = built.Unlisted.Count + built.UnknownIds.Count > 0;
    if (check)
    {
        bool same = File.Exists(linesFile) && File.ReadAllText(linesFile).Replace("\r\n", "\n") == lines
            && File.Exists(coverageFile) && File.ReadAllText(coverageFile).Replace("\r\n", "\n") == report;
        Console.WriteLine(same ? "lines.json and coverage.md are what the game gives." : "lines.json or coverage.md is not what the game gives: run catalogue without --check.");
        return same && !problems ? 0 : 1;
    }
    Directory.CreateDirectory(Path.GetDirectoryName(coverageFile)!);
    File.WriteAllText(linesFile, lines);
    File.WriteAllText(coverageFile, report);
    Console.WriteLine("Data/voice/lines.json and docs/voice/coverage.md written.");
    return problems ? 1 : 0;
}

void List(string what, IReadOnlyList<string> keys)
{
    if (keys.Count == 0) return;
    Console.WriteLine($"{keys.Count} {what}:");
    foreach (string key in keys.Take(40)) Console.WriteLine("    " + key);
    if (keys.Count > 40) Console.WriteLine($"    ... and {keys.Count - 40} more");
}

int Export()
{
    if (words.Count < 2) { Console.Error.WriteLine("export needs a voice: export <voice> [file]."); return 2; }
    string voice = words[1];
    var file = VoiceCatalogue.Parse(File.ReadAllText(linesFile));
    var mine = file.Lines.Where(l => l.Voice == voice || l.Voice.StartsWith(voice + "#", StringComparison.Ordinal)).ToList();
    var sb = new StringBuilder($"# Lines of {voice} ({mine.Count})\n\n");
    foreach (var l in mine)
    {
        sb.Append(l.Key).Append("  [").Append(l.Direction ?? "calm").Append("]\n");
        sb.Append("  shown:  ").Append(l.En).Append('\n');
        sb.Append("  spoken: ").Append(l.Spoken ?? "(shown only)").Append('\n');
        foreach (var v in l.Variants ?? new()) sb.Append("  ").Append(v.Key).Append(": ").Append(v.Spoken).Append('\n');
        sb.Append('\n');
    }
    if (words.Count > 2) File.WriteAllText(words[2], sb.ToString());
    else Console.Write(sb);
    return 0;
}

static string? FindRepository()
{
    for (string? dir = Directory.GetCurrentDirectory(); dir != null; dir = Path.GetDirectoryName(dir))
        if (File.Exists(Path.Combine(dir, "PokemonPlatinum.sln"))) return dir;
    for (string? dir = AppContext.BaseDirectory; dir != null; dir = Path.GetDirectoryName(dir))
        if (File.Exists(Path.Combine(dir, "PokemonPlatinum.sln"))) return dir;
    return null;
}
