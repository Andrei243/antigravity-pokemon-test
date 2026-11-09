// The voice catalogue (plan 26 · V1). See tools/VoiceTool/README.md.

using System.Diagnostics;
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

// The words of the command, leaving out the switches and the values of those that take one
string[] valued = { "--language", "--voice", "--out", "--pack", "--engine", "--which", "--jobs", "--count", "--top", "--region", "--takes", "--into" };
var words = args.Where((a, i) => !a.StartsWith("--", StringComparison.Ordinal) && (i == 0 || !valued.Contains(args[i - 1]))).ToList();
var switches = args.Where(a => a.StartsWith("--", StringComparison.Ordinal)).ToHashSet();
string samplesRoot = Path.Combine(engine, "Data", "voice", "samples");
Dictionary<string, SortedSet<string>>? selfMap = null;
string lexiconPath = Path.Combine(engine, "Data", "voice", "lexicon.json");
string[] commands = { "catalogue", "export", "manifest", "check", "vowels", "check-samples", "bake-samples", "bake", "jobs", "generate", "reroll", "trial", "reel", "lexicon", "status", "design", "speak" };
if (words.Count == 0 || !commands.Contains(words[0]))
{
    Console.Error.WriteLine("Say one of: " + string.Join(", ", commands) + ". tools/VoiceTool/README.md says what each does.");
    return 2;
}
return words[0] switch
{
    "catalogue" => Catalogue(),
    "export" => Export(),
    "manifest" => Manifest(),
    "check" => Check(),
    "check-samples" => CheckSamples(),
    "bake-samples" => BakeSamples(),
    "bake" => Bake(),
    "jobs" => Jobs(),
    "generate" => Generate(),
    "reroll" => Reroll(),
    "trial" => Trial(),
    "reel" => Reel(),
    "lexicon" => Lexicon(),
    "status" => Status(),
    "design" => Design(),
    "speak" => Speak(),
    _ => Vowels()
};

string? Option(string name) => args.SkipWhile(a => a != name).Skip(1).FirstOrDefault();

// ------------------------------------------------------------------ the user's samples (plan 26 · V3)

string SamplesFolder(string voice) => words.Count > 1 ? Path.GetFullPath(words[1]) : Path.Combine(samplesRoot, voice);

SamplesFile ScriptOf(string voice)
{
    string own = Path.Combine(SamplesFolder(voice), "samples.json");
    return SamplesFile.Read(File.Exists(own) ? own : Path.Combine(samplesRoot, voice, "samples.json"));
}

int CheckSamples()
{
    string voice = Option("--voice") ?? VoiceCast.Rival;
    string folder = SamplesFolder(voice);
    var script = ScriptOf(voice);
    var report = VoiceSamples.Check(folder, script);
    Console.WriteLine($"{script.Passages.Count} passages of {voice}'s samples in {folder}:");
    foreach (var m in report.Measures)
        Console.WriteLine($"  {m.Passage,-26} {m.Seconds,5:0.0} s  peak {m.PeakDb,6:0.0} dB  room {m.FloorDb,4:0} dB  quiet {m.LeadSeconds:0.00}/{m.TailSeconds:0.00} s  {m.WordsPerSecond:0.0} words/s  {m.Rate} Hz {m.Bits}-bit {(m.Channels == 1 ? "mono" : "stereo")}");
    if (report.Missing.Count > 0) Console.WriteLine($"not recorded yet ({report.Missing.Count}): " + string.Join(", ", report.Missing));
    foreach (var f in report.Findings) Console.WriteLine($"{(f.Severity == VoiceSamples.Severity.Error ? "error" : "warning")}: {f.Passage}: {f.What}");
    Console.WriteLine($"{report.Measures.Count} takes, {report.TotalSeconds:0} seconds in all: {report.Errors} errors, {report.Findings.Count - report.Errors} warnings, {report.Missing.Count} missing.");
    return report.Errors > 0 || report.Missing.Count > 0 ? 1 : 0;
}

int BakeSamples()
{
    string voice = Option("--voice") ?? VoiceCast.Rival;
    string folder = SamplesFolder(voice);
    string output = Option("--out") is { } o ? Path.GetFullPath(o) : Path.Combine(repo, "voice", "samples-clean", voice);
    var script = ScriptOf(voice);
    Directory.CreateDirectory(output);
    int n = 0;
    foreach (var passage in script.Passages)
    {
        string path = Path.Combine(folder, passage.Id + ".wav");
        if (!File.Exists(path)) continue;
        var wave = WavDecoder.Read(File.ReadAllBytes(path));
        var clean = VoiceBake.Clean(wave.Mono, wave.Rate, passage.Text, outRate: wave.Rate, level: false);
        File.WriteAllBytes(Path.Combine(output, passage.Id + ".wav"), WavDecoder.Encode(clean.Samples, clean.Rate, 24, 1));
        foreach (string f in clean.Findings) Console.WriteLine($"{passage.Id}: {f}");
        n++;
    }
    File.Copy(Path.Combine(samplesRoot, voice, "samples.json"), Path.Combine(output, "samples.json"), true);
    Console.WriteLine($"{n} samples cleaned into {output} (trimmed, rumble and plosives softened, at their own rate and loudness): what an engine clones from.");
    return 0;
}

// ------------------------------------------------------------------ generating and baking (plan 26 · V3, V4)

// The catalogue, or with --region <id> only the lines of that region's places (VoiceCatalogue.RegionOf)
VoiceFile CatalogueNow()
{
    var file = VoiceCatalogue.Parse(File.ReadAllText(linesFile));
    if (Option("--region") is { } region)
        // (a line of no region, the common scripts' and the phone's, belongs to every region)
        file.Lines.RemoveAll(l => VoiceCatalogue.RegionOf(l.Key) is { } of && !string.Equals(of, region, StringComparison.OrdinalIgnoreCase));
    return file;
}

// The voices of the people who run a script whose lines are the person spoken to's (found once, by walking the game's maps)
IReadOnlyCollection<string> SelfSpeakers(string key)
{
    selfMap ??= SelfVoices.Of(new VoiceCast(GameDataFiles.Deserialize<CastFile>(File.ReadAllText(castPath))), Option("--region"));
    return SelfVoices.ScriptOf(key) is { } script && selfMap.TryGetValue(script, out var set) ? set : Array.Empty<string>();
}

VoiceLibrary? PackNow() => VoiceLibrary.Open(Option("--pack") is { } p ? Path.GetFullPath(p) : Path.Combine(repo, "voice", LanguageOf()));

string PackFolderOption() => Option("--pack") is { } p ? Path.GetFullPath(p) : Path.Combine(repo, "voice", LanguageOf());

Func<string, string, (string Path, string Text)?>? SamplesFor(string voice)
{
    var cast = GameDataFiles.Deserialize<CastFile>(File.ReadAllText(castPath));
    if (!cast.Voices.TryGetValue(voice, out var v) || v.Clone == null) return null;
    string folder = Path.Combine(repo, "voice", "samples-clean", voice);
    if (!Directory.Exists(folder)) folder = Path.Combine(engine, v.Clone);
    string script = Path.Combine(engine, v.Clone, "samples.json");
    return File.Exists(script) ? VoiceGeneration.SamplesOf(folder, SamplesFile.Read(script), repo) : null;
}

int Bake()
{
    if (words.Count < 2 || Option("--voice") is not { } voice) { Console.Error.WriteLine("bake <folder of takes> --voice <id> [--pack <folder>] [--engine <name>]: each take named <key>.wav, the key's colon written '+'."); return 2; }
    var from = new VoiceGeneration.FolderEngine(Path.GetFullPath(words[1]), Option("--engine") ?? "by hand");
    var jobs = VoiceGeneration.Plan(CatalogueNow(), voice, null, VoiceGeneration.Which.All, LanguageOf(), takes: 1);
    jobs.Jobs.RemoveAll(j => !from.Has(j.Key));
    var (kept, failed) = VoiceGeneration.Generate(jobs, from, PackFolderOption());
    foreach (var (key, findings) in failed) Console.WriteLine($"{key}: {string.Join("; ", findings)}");
    Console.WriteLine($"{kept.Count} takes baked into {PackFolderOption()}; {failed.Count} with findings.");
    return 0;
}

int Jobs()
{
    if (words.Count < 2) { Console.Error.WriteLine("jobs <voice> [--which missing|stale|all] [--out <file>]"); return 2; }
    var which = Enum.TryParse<VoiceGeneration.Which>(Option("--which") ?? "missing", true, out var w) ? w : VoiceGeneration.Which.Missing;
    var jobs = VoiceGeneration.Plan(CatalogueNow(), words[1], PackNow(), which, LanguageOf(), SamplesFor(words[1]), VoiceLexicon.Default, selfSpeakers: SelfSpeakers);
    string output = Option("--out") ?? Path.Combine(repo, "voice", "jobs", VoicePacks.FileNameOf(words[1]) + ".json");
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
    File.WriteAllText(output, GameDataFiles.Serialize(jobs));
    Console.WriteLine($"{jobs.Jobs.Count} lines of {words[1]} ({which.ToString().ToLowerInvariant()}), {jobs.Jobs.Count(j => j.Guessed)} with a guessed direction, written to {output}.");
    return 0;
}

int Generate()
{
    string config = Path.Combine(repo, "tools", "VoiceGen", "engine.json");
    // generate <voice>... : each voice's lines with no current take (with --region, only that region's), one run of the engine
    var files = new List<VoiceJobFile>();
    if (Option("--jobs") is { } file) files.Add(GameDataFiles.Deserialize<VoiceJobFile>(File.ReadAllText(file)));
    else
    {
        var catalogue = CatalogueNow();
        var pack = PackNow();
        foreach (string voice in words.Skip(1))
            files.Add(VoiceGeneration.Plan(catalogue, voice, pack, VoiceGeneration.Which.Stale, LanguageOf(), SamplesFor(voice), VoiceLexicon.Default, takes: int.TryParse(Option("--takes"), out int takes) && takes > 0 ? takes : VoiceGeneration.Takes, selfSpeakers: SelfSpeakers));
    }
    if (files.Count == 0) { Console.Error.WriteLine("generate <voice>... [--region <id>] | --jobs <file>"); return 2; }
    if (!File.Exists(config))
    {
        Console.WriteLine($"No engine yet: {config} is not there. tools/VoiceGen/README.md says how to set it up (setup-kokoro.sh). {files.Sum(f => f.Jobs.Count)} lines wait.");
        return 2;
    }
    using var command = CommandEngine.Read(config, repo);
    int keptCount = 0, failedCount = 0;
    var clock = Stopwatch.StartNew();
    foreach (var jobs in files)
    {
        var (kept, failed) = VoiceGeneration.Generate(jobs, command, Option("--into") is { } into ? Path.GetFullPath(into) : PackFolderOption(), Console.WriteLine);
        foreach (var (key, findings) in failed) Console.WriteLine($"{key}: {string.Join("; ", findings)}");
        Console.WriteLine($"{jobs.Voice}: {kept.Count} lines generated with {command.Name}; {failed.Count} with findings or failed.");
        keptCount += kept.Count;
        failedCount += failed.Count;
    }
    Console.WriteLine($"In all {keptCount} lines in {clock.Elapsed.TotalSeconds:0} s ({(keptCount > 0 ? clock.Elapsed.TotalSeconds / keptCount : 0):0.00} s a line); {failedCount} with findings or failed.");
    return failedCount > 0 ? 1 : 0;
}

int Reroll()
{
    if (words.Count < 2) { Console.Error.WriteLine("reroll <key>"); return 2; }
    var catalogue = CatalogueNow();
    string lineKey = words[1].Contains('~') ? words[1][..words[1].IndexOf('~')] : words[1];
    string voice = catalogue.Lines.FirstOrDefault(l => l.Key == lineKey)?.Voice ?? "";
    var jobs = VoiceGeneration.Reroll(catalogue, words[1], PackNow(), SamplesFor(voice), VoiceLexicon.Default);
    string output = Path.Combine(repo, "voice", "jobs", "reroll.json");
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    File.WriteAllText(output, GameDataFiles.Serialize(jobs));
    Console.WriteLine($"New seeds for {words[1]} ({string.Join(", ", jobs.Jobs[0].Seeds)}) in {output}; run generate --jobs {output}.");
    return 0;
}

int Trial()
{
    var voices = words.Skip(1).ToList();
    if (voices.Count == 0) { Console.Error.WriteLine("trial <voice>... (trial rival; trial rowan mom rival_mom assistant_boy assistant_girl looker cheryl roark mars)"); return 2; }
    var catalogue = CatalogueNow();
    var lines = VoiceGeneration.TrialLines(catalogue, voices, int.TryParse(Option("--count"), out int c) ? c : 20);
    var jobs = VoiceGeneration.Plan(new VoiceFile { Lines = lines }, "all", null, VoiceGeneration.Which.All, LanguageOf(), voices.Count == 1 ? SamplesFor(voices[0]) : null, VoiceLexicon.Default, takes: 1);
    string folder = Path.Combine(repo, "voice", "trial");
    Directory.CreateDirectory(folder);
    string name = string.Join("_", voices.Select(VoicePacks.FileNameOf));
    File.WriteAllText(Path.Combine(folder, name + ".json"), GameDataFiles.Serialize(jobs));
    var sb = new StringBuilder($"# Trial lines: {string.Join(", ", voices)}\n\nThe same lines for every engine. Listen to each engine's takes of them and choose by ear.\n\n");
    foreach (var j in jobs.Jobs) sb.Append($"- `{j.Key}` ({j.Voice}, {j.Direction}{(j.Guessed ? ", guessed" : "")}): {j.Text}\n");
    File.WriteAllText(Path.Combine(folder, name + ".md"), sb.ToString());
    Console.WriteLine($"{jobs.Jobs.Count} trial lines in {Path.Combine(folder, name + ".json")} (and .md). For each engine: generate --jobs <that file> --pack voice/trial/<engine>.");
    return 0;
}

int Reel()
{
    string? voice = Option("--voice");
    if (words.Count < 2 && voice == null) { Console.Error.WriteLine("reel <key prefix> | --voice <id> [--count n] [--region <id>] [--pack <folder>] [--out <file.wav>] (reel world:jubilife_city.; reel --voice rowan --count 12)"); return 2; }
    var pack = PackNow();
    if (pack == null) { Console.WriteLine("No pack to make a reel from."); return 2; }
    var catalogue = CatalogueNow();
    if (voice != null)
    {
        // A voice's reel: its first lines in the catalogue's order that have a take, spread over its directions
        int count = int.TryParse(Option("--count"), out int c) ? c : 12;
        // (a voice chosen at run time, the assistant's, is made in its own voices: assistant_girl's reel has her takes of the assistant's lines)
        var mine = catalogue.Lines.Where(l => pack.Entries.Any(e => VoiceGeneration.Matches(e.Voice, voice) && (e.Key == l.Key || e.Key.StartsWith(l.Key + "~", StringComparison.Ordinal)))).ToList();
        var picked = mine.GroupBy(l => l.Direction is { Length: > 0 } d ? d : VoiceMoods.Guess(l.En))
            .SelectMany(g => g.Select((l, i) => (l, i))).OrderBy(x => x.i).ThenBy(x => mine.IndexOf(x.l)).Take(count).Select(x => x.l)
            .OrderBy(l => mine.IndexOf(l)).ToList();
        catalogue = new VoiceFile { Lines = picked };
    }
    string prefix = words.Count > 1 ? words[1] : "";
    var (samples, keys) = VoiceGeneration.Reel(pack, catalogue, prefix, voice: voice);
    string output = Option("--out") ?? Path.Combine(repo, "voice", "reels", VoicePacks.FileNameOf(voice ?? prefix.TrimEnd('.')) + ".wav");
    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
    File.WriteAllBytes(output, WavDecoder.Encode(samples));
    // Beside the reel, its takes in order with the words each says, for listening along
    var spoken = VoicePacks.KeysOf(catalogue).GroupBy(k => k.Key).ToDictionary(g => g.Key, g => g.First().English);
    File.WriteAllLines(Path.ChangeExtension(output, ".txt"), keys.Select(k => { string key = k[..k.IndexOf(' ')]; return spoken.TryGetValue(key, out var en) ? $"{k}: {en}" : k; }));
    Console.WriteLine($"{keys.Count} takes, {samples.Length / (double)Synthesizer.DsRate:0} seconds, in {output} (their keys in order beside it).");
    return 0;
}

// What a pack has of the catalogue (or of a region's part of it) voice by voice: current, stale, missing, and whether the voice has a design
int Status()
{
    var cast = GameDataFiles.Deserialize<CastFile>(File.ReadAllText(castPath));
    var catalogue = CatalogueNow();
    var pack = PackNow();
    var have = new Dictionary<(string Key, string Voice), VoicePackEntry>();
    if (pack != null) foreach (var e in pack.Entries) have[(e.Key, e.Voice)] = e;
    var rows = new SortedDictionary<string, int[]>(StringComparer.Ordinal); // current, stale, missing
    var missingKeys = new List<(string Voice, string Key)>();
    int self = 0, shownOnly = 0;
    foreach (var line in catalogue.Lines)
    {
        if (line.Status != VoiceLine.StatusSpoken || line.Spoken == null) { shownOnly++; continue; }
        IReadOnlyCollection<string> speakers;
        if (line.Voice == VoiceCast.Self)
        {
            speakers = SelfSpeakers(line.Key);
            if (speakers.Count == 0) { self++; continue; }
        }
        else speakers = cast.Voices.TryGetValue(line.Voice, out var entry) && entry.Chooses is { Count: > 0 } chooses ? chooses : new List<string> { line.Voice };
        var keys = line.Variants is { Count: > 0 } v ? v.Select(x => line.Key + x.Key).ToList() : new List<string> { line.Key };
        foreach (string speaker in speakers)
            foreach (string key in keys)
            {
                if (!rows.TryGetValue(speaker, out var counts)) rows[speaker] = counts = new int[3];
                int state = have.TryGetValue((key, speaker), out var take) ? (take.TextHash == VoiceCatalogue.HashOf(line.En) ? 0 : 1) : 2;
                counts[state]++;
                if (state > 0) missingKeys.Add((speaker, key));
            }
    }
    bool Designed(string voice) => cast.Voices.TryGetValue(voice, out var c) && c.Design != null;
    int[] total = new int[3];
    var undesigned = new List<(string Voice, int Lines)>();
    foreach (var (voice, counts) in rows)
    {
        for (int i = 0; i < 3; i++) total[i] += counts[i];
        if (!Designed(voice)) undesigned.Add((voice, counts.Sum()));
    }
    if (switches.Contains("--voices"))
        foreach (var (voice, counts) in rows) Console.WriteLine($"{voice,-34} current {counts[0],5}  stale {counts[1],4}  missing {counts[2],5}  {(Designed(voice) ? "" : "NO DESIGN")}");
    Console.WriteLine($"{rows.Count} voices speak {total.Sum()} takes' worth of spoken lines: {total[0]} current, {total[1]} stale, {total[2]} missing; {self} lines of no voice (said by whoever is spoken to in scripts no person runs) and are nobody's; {shownOnly} shown only (no spoken text).");
    Console.WriteLine($"{undesigned.Count} of those voices have no design ({undesigned.Sum(u => u.Lines)} takes).");
    if (switches.Contains("--shown"))
        foreach (var line in catalogue.Lines.Where(l => l.Status != VoiceLine.StatusSpoken || l.Spoken == null)) Console.WriteLine($"  {line.Voice}	{line.Key}	{line.En}");
    if (switches.Contains("--missing"))
        foreach (var (voice, key) in missingKeys) Console.WriteLine($"  {voice}\t{key}");
    return 0;
}

// Brief and design for every voice that speaks the catalogue (or the region) and has none: tools/VoiceTool/Designs.cs
int Design()
{
    var cast = GameDataFiles.Deserialize<CastFile>(File.ReadAllText(castPath));
    var catalogue = CatalogueNow();
    var wanted = new SortedSet<string>(StringComparer.Ordinal);
    foreach (var line in catalogue.Lines)
    {
        if (line.Status != VoiceLine.StatusSpoken) continue;
        if (line.Voice == VoiceCast.Self) { foreach (string s in SelfSpeakers(line.Key)) wanted.Add(s); }
        else if (cast.Voices.TryGetValue(line.Voice, out var entry) && entry.Chooses is { Count: > 0 } chooses) foreach (string c in chooses) wanted.Add(c);
        else wanted.Add(line.Voice);
    }
    var taken = new HashSet<(string, double, double)>(cast.Voices.Values.Where(v => v.Design != null).Select(v => (v.Design!.Blend, v.Design.Speed, v.Design.Pitch)));
    int made = 0, generic = 0;
    foreach (string voice in wanted)
    {
        if (cast.Voices.TryGetValue(voice, out var existing) && (existing.Design != null || existing.Chooses != null || existing.Clone != null)) continue;
        var (brief, design, plain) = Designs.Make(voice, taken);
        if (existing == null) cast.Voices[voice] = existing = new CastVoice();
        existing.Brief = brief;
        existing.Design = design;
        existing.Provisional = null;
        made++;
        if (plain) { generic++; Console.WriteLine($"plain: {voice} has no profile in Designs.cs, so it speaks as a person of the town"); }
    }
    File.WriteAllText(castPath, GameDataFiles.Serialize(cast));
    Console.WriteLine($"{made} voices designed ({generic} from the plain profile), written to {castPath}; {wanted.Count - made} already had a design.");
    return 0;
}

// A line shown only (its text names the player or the rival) given a spoken text written by hand: speak <file of {"key": "spoken text"}>
int Speak()
{
    if (words.Count < 2) { Console.Error.WriteLine("speak <file>: a JSON object of line keys to the text that is said in place of the line (no placeholders; kept across catalogue runs while the line's English holds)."); return 2; }
    var given = GameDataFiles.Deserialize<Dictionary<string, string>>(File.ReadAllText(words[1]));
    var catalogue = VoiceCatalogue.Parse(File.ReadAllText(linesFile));
    int set = 0;
    foreach (var (key, text) in given)
    {
        var line = catalogue.Lines.FirstOrDefault(l => l.Key == key);
        if (line == null) { Console.WriteLine($"wrong: no line {key}"); continue; }
        if (text.Contains('{')) { Console.WriteLine($"wrong: {key}: a spoken text holds no placeholder"); continue; }
        line.Spoken = text;
        line.Status = VoiceLine.StatusSpoken;
        line.Manual = true;
        line.Variants = null;
        set++;
    }
    File.WriteAllText(linesFile, VoiceCatalogue.Serialize(catalogue.Lines));
    Console.WriteLine($"{set} lines given a spoken text by hand; run catalogue to rewrite the report.");
    return 0;
}

int Lexicon()
{
    var lexicon = VoiceLexicon.Default;
    foreach (var e in lexicon.File.Entries.Where(e => !VoiceLexicon.WellMade(e.Say))) Console.WriteLine($"wrong: {e.Name}'s respelling '{e.Say}' is not syllables of letters with one in capitals");
    var cast = GameDataFiles.Deserialize<CastFile>(File.ReadAllText(castPath));
    var candidates = PokemonPlatinumEngine.Data.PokemonDatabase.Everyone().Select(s => s.Name)
        .Concat(cast.Names.Where(n => !VoiceCast.IsArchetype(n.Value) && !n.Key.Contains('{') && !n.Key.Contains(' ')).Select(n => n.Key));
    var missing = lexicon.Missing(CatalogueNow(), candidates);
    Console.WriteLine($"{lexicon.File.Entries.Count} names in the list; {missing.Count} names the lines say are not, the most said first:");
    foreach (var (name, lines) in missing.Take(int.TryParse(Option("--top"), out int top) ? top : 60)) Console.WriteLine($"  {name} ({lines} lines)");
    return 0;
}

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

    if (switches.Contains("--ways"))
    {
        Console.WriteLine($"{built.Differs.Count} lines whose voice the way through their script decides otherwise than the file's order; {built.Ways.Count} lines with more than one way:");
        foreach (var g in built.Differs.GroupBy(d => VoiceCatalogue.RegionOf(d.Key) ?? "(none)").OrderBy(g => g.Key)) Console.WriteLine($"  {g.Key}: {g.Count()} differ");
        foreach (var g in built.Ways.GroupBy(d => VoiceCatalogue.RegionOf(d.Key) ?? "(none)").OrderBy(g => g.Key)) Console.WriteLine($"  {g.Key}: {g.Count()} with several ways");
        foreach (var d in built.Differs.Where(d => VoiceCatalogue.RegionOf(d.Key) == (Option("--region") ?? "kanto")).Take(60)) Console.WriteLine($"    {d.Key}: file order {d.Linear}, ways {d.Ways}");
        foreach (var w in built.Ways.Where(d => VoiceCatalogue.RegionOf(d.Key) == (Option("--region") ?? "kanto")).Take(60)) Console.WriteLine($"    {w.Key}: {string.Join(", ", w.Value)}");
        return 0;
    }

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
    var file = CatalogueNow();
    var mine = file.Lines.Where(l => voice == "all" || l.Voice == voice || l.Voice.StartsWith(voice + "#", StringComparison.Ordinal)).ToList();
    if (switches.Contains("--tsv"))
    {
        var rows = mine.Select(l => string.Join("\t", l.Key, l.Voice, l.Speaker ?? "", l.Status == VoiceLine.StatusSpoken ? "spoken" : "shown", l.En.Replace("\n", " "))).ToList();
        if (words.Count > 2) File.WriteAllLines(words[2], rows); else foreach (var r in rows) Console.WriteLine(r);
        return 0;
    }
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
