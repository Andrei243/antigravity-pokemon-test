// The voice catalogue (plan 26 · V1). See tools/VoiceTool/README.md.

using System.Text;
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
if (words.Count == 0 || words[0] is not ("catalogue" or "export"))
{
    Console.Error.WriteLine("Say catalogue [--add-missing] [--check], or export <voice> [file].");
    return 2;
}
return words[0] == "catalogue" ? Catalogue() : Export();

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
