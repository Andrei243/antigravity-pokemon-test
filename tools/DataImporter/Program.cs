// Rebuilds species.json, moves.json, abilities.json, items.json and trainers.json in PokemonPlatinumEngine/Data from the Platinum
// decompilation and PokeAPI, then applies the hand corrections in tools/DataImporter/Overrides and writes the coverage
// report docs/mechanics/coverage.md. The moves of our own past level 100 (plan 06 · R31) are whole rows of
// Overrides/moves.json, handed out to the learnsets by MovesPastHundred. See tools/DataImporter/README.md.
//
//   dotnet run --project tools/DataImporter                         fetch the pinned sources (once) and import everything
//   dotnet run --project tools/DataImporter -- --decomp <dir> --pokeapi <dir/data/v2/csv>   use existing checkouts
//   dotnet run --project tools/DataImporter -- --last-species 493   stop after Platinum's National Pokédex
//   dotnet run --project tools/DataImporter -- --check-entries      the written Pokédex entries against the games' own (plan 08 · P13)

using System.Text.Json.Nodes;
using DataImporter;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

string repo = FindRepo();
string? decompDir = Arg("--decomp");
string? pokeApiDir = Arg("--pokeapi");
int lastSpecies = int.Parse(Arg("--last-species") ?? "1025");
string outDir = Arg("--out") ?? Path.Combine(repo, "PokemonPlatinumEngine", "Data");
string cache = Path.Combine(repo, "tools", "DataImporter", ".cache");

if (args.Contains("--check-entries"))
{
    // Only PokeAPI's flavour texts are read, and nothing is written: exit 1 on a run of words shared
    string? entries = Arg("--check-entries");
    if (entries == null || entries.StartsWith("--")) entries = Path.Combine(repo, "PokemonPlatinumEngine", "Data", DexEntries.FileName);
    if (pokeApiDir == null)
    {
        Console.WriteLine("Fetching PokeAPI…");
        pokeApiDir = Path.Combine(Sources.Checkout(cache, "pokeapi", Sources.PokeApiRepo, Sources.PokeApiCommit, Sources.PokeApiFolders), "data", "v2", "csv");
    }
    return EntryCheck.Check(entries, pokeApiDir) == 0 ? 0 : 1;
}

if (args.Contains("--hoenn-contest"))
{
    // Emerald's contest tables alone (plan 21 · H11): hoenn-contest.json, from three headers of pret/pokeemerald
    Write(Path.Combine(outDir, HoennContestDatabase.FileName), GameDataFiles.Serialize(HoennContestImport.Build(repo, cache)));
    return 0;
}

if (decompDir == null)
{
    Console.WriteLine("Fetching pret/pokeplatinum…");
    decompDir = Sources.Checkout(cache, "pokeplatinum", Sources.DecompRepo, Sources.DecompCommit, Sources.DecompFolders);
}
if (pokeApiDir == null)
{
    Console.WriteLine("Fetching PokeAPI…");
    pokeApiDir = Path.Combine(Sources.Checkout(cache, "pokeapi", Sources.PokeApiRepo, Sources.PokeApiCommit, Sources.PokeApiFolders), "data", "v2", "csv");
}

string showdownDir = Arg("--showdown") ?? FetchShowdown();

var importer = new Importer(new Decomp(decompDir), new PokeApi(pokeApiDir), lastSpecies);
var showdown = new Showdown(showdownDir);
string overrides = Path.Combine(repo, "tools", "DataImporter", "Overrides");

Console.WriteLine("Moves…");
var imported = importer.Moves();
importer.AddLaterMechanics(imported, showdown);
var moves = Overrides.Apply(imported, Path.Combine(overrides, "moves.json")).OrderBy(m => m.Id).ToList();
Console.WriteLine("Abilities…");
var abilities = Overrides.Apply(importer.Abilities(), Path.Combine(overrides, "abilities.json")).OrderBy(a => a.Id).ToList();
Console.WriteLine("Species…");
var importedSpecies = importer.Species();
importer.AddFormColors(importedSpecies, showdown);
var species = Overrides.Apply(importedSpecies, Path.Combine(overrides, "species.json")).OrderBy(s => s.DexNumber).ToList();
// The moves of our own past level 100 (plan 06 · R31), handed out by their rule after the species' own learnsets
var pastHundred = MovesPastHundred.Append(species, moves);
Console.WriteLine($"  moves past 100: {string.Join(", ", pastHundred.Select(c => $"{c.Value} species learn {c.Key}"))} ({pastHundred.Sum(c => c.Key * c.Value)} learnings)");
Console.WriteLine("Items…");
var evolutionItems = species.SelectMany(s => s.Evolutions ?? new()).Select(e => e.Item).OfType<string>().ToHashSet();
var importedItems = importer.Items(evolutionItems, moves);
importer.AddLaterMechanics(importedItems, showdown);
importer.AddLaterItemEffects(importedItems, showdown);
var items = Overrides.Apply(importedItems, Path.Combine(overrides, "items.json")).OrderBy(i => i.Id).ToList();
importer.AddLaterMachines(species, items);
importer.AddTechnicalRecords(species, items);
importer.AddHoennMachines(species, items);
var marts = importer.Marts();
// Counters of other regions's own (plan 20 · J7: the Goldenrod Department Store's, from HeartGold's src/scrcmd_mart.c), which
// Platinum's lists don't have: Overrides/marts.json's specialties, laid over the imported ones
Overrides.AddMarts(marts, Path.Combine(overrides, "marts.json"));
var contests = importer.Contests();
contests.Backdrops = importer.Backdrops();
var seals = importer.Seals();
var ribbons = importer.Ribbons();
var underground = importer.Underground(items.Select(i => i.Name).ToHashSet());
var frontier = importer.Frontier();
var migration = importer.Migration(items, moves);

Console.WriteLine("Trainers…");
var trainers = importer.Trainers(species);
// The Master Trainers (plan 14 · B14): a master for every species, its moves from Showdown's random-battle sets
var masters = MasterTrainers.Build(showdownDir, species, moves, items);
Console.WriteLine($"  {masters.Masters.Count} masters: {masters.FromSets} with moves of a set ({masters.MadeUp} made up with what their level taught them), {masters.WithoutMoves} with none (they know what their level taught them)");

Check(species, moves, abilities, items);

Write(Path.Combine(outDir, PokemonDatabase.FileName), GameDataFiles.Serialize(species));
Write(Path.Combine(outDir, MoveDatabase.FileName), GameDataFiles.Serialize(moves));
Write(Path.Combine(outDir, AbilityDatabase.FileName), GameDataFiles.Serialize(abilities));
Write(Path.Combine(outDir, ItemDatabase.FileName), GameDataFiles.Serialize(items));
Write(Path.Combine(outDir, TrainerDatabase.FileName), GameDataFiles.Serialize(trainers));
Write(Path.Combine(outDir, TrainerDatabase.MastersFileName), GameDataFiles.Serialize(masters.Masters));
Write(Path.Combine(outDir, MartDatabase.FileName), GameDataFiles.Serialize(marts));
Write(Path.Combine(outDir, SuperContestDatabase.FileName), GameDataFiles.Serialize(contests));
Write(Path.Combine(outDir, SealDatabase.FileName), GameDataFiles.Serialize(seals));
Write(Path.Combine(outDir, RibbonDatabase.FileName), GameDataFiles.Serialize(ribbons));
Write(Path.Combine(outDir, UndergroundDatabase.FileName), GameDataFiles.Serialize(underground));
Write(Path.Combine(outDir, FrontierDatabase.FileName), GameDataFiles.Serialize(frontier));
Write(Path.Combine(outDir, MigrationDatabase.FileName), GameDataFiles.Serialize(migration));
// Plan 17 · N3: the names in every language PokeAPI has them in, one file each beside the language's interface table.
// Only names.json is written: a language's hand-written names.<kind>.json (plan 17 · N6, LocalNames.OwnFilePattern) are
// never written, moved or deleted here
var pokeApi = new PokeApi(pokeApiDir);
foreach (var (tag, ids) in PokeApi.Languages)
{
    string folder = Path.Combine(outDir, "text", tag);
    Directory.CreateDirectory(folder);
    Write(Path.Combine(folder, NameFiles.FileName), NameFiles.Build(pokeApi, ids, species, moves, abilities, items));
}
Write(Coverage.ReportPath(repo), Coverage.Report(species, moves, abilities, items, Path.Combine(outDir, "text")));
Console.WriteLine($"{species.Count} species, {moves.Count} moves, {abilities.Count} abilities, {items.Count} items and {trainers.Count} trainers in {outDir}");
return 0;

string FetchShowdown()
{
    Console.WriteLine("Fetching Pokémon Showdown's move, item and species tables…");
    return Sources.Download(cache, "showdown", Sources.ShowdownRaw, Sources.ShowdownCommit, Sources.ShowdownFiles);
}

// A file is only written when what it says has changed: one that is the same but for its line endings (git checks
// the data files out with the machine's own) is left alone, so a run that changes nothing touches nothing.
void Write(string path, string text)
{
    if (File.Exists(path) && File.ReadAllText(path).ReplaceLineEndings("\n") == text.ReplaceLineEndings("\n"))
    {
        Console.WriteLine($"  unchanged  {Path.GetFileName(path)}");
        return;
    }
    File.WriteAllText(path, text);
    Console.WriteLine($"  wrote      {Path.GetFileName(path)}");
}

string? Arg(string name)
{
    int i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string FindRepo()
{
    foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
    {
        for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "PokemonPlatinum.sln"))) return dir.FullName;
    }
    throw new DirectoryNotFoundException("Run the importer from inside the repository");
}

// Every name the data refers to must exist: a typo in an override or a source change would otherwise turn into
// Tackle (moves) or a missing species at run time.
static void Check(List<PokemonSpecies> species, List<MoveData> moves, List<AbilityDatabase.AbilityRecord> abilities, List<ItemData> items)
{
    var problems = new List<string>();
    var speciesNames = species.Select(s => s.Name).ToHashSet();
    var moveNames = moves.Select(m => m.Name).ToHashSet();
    var abilityNames = abilities.Select(a => a.Name).ToHashSet();
    var itemNames = items.Select(i => i.Name).ToHashSet();

    void Unique<T>(string what, IEnumerable<T> keys)
    {
        foreach (var dup in keys.GroupBy(k => k).Where(g => g.Count() > 1)) problems.Add($"duplicate {what} {dup.Key}");
    }
    Unique("species name", species.Select(s => s.Name));
    Unique("move name", moves.Select(m => m.Name));
    Unique("move id", moves.Select(m => m.Id));
    Unique("item name", items.Select(i => i.Name));
    Unique("item id", items.Select(i => i.Id));
    Unique("ability name", abilities.Select(a => a.Name));

    foreach (var s in species)
    {
        problems.AddRange(s.Learnset.Where(l => !moveNames.Contains(l.MoveName)).Select(l => $"{s.Name} learns unknown move {l.MoveName}"));
        problems.AddRange(s.Abilities.Append(s.HiddenAbility).OfType<string>().Where(a => !abilityNames.Contains(a)).Select(a => $"{s.Name} has unknown ability {a}"));
        foreach (var e in s.Evolutions ?? new())
        {
            if (!speciesNames.Contains(e.TargetSpecies)) problems.Add($"{s.Name} evolves into unknown {e.TargetSpecies}");
            if (e.Item != null && !itemNames.Contains(e.Item)) problems.Add($"{s.Name} evolves with unknown item {e.Item}");
            if (e.Move != null && !moveNames.Contains(e.Move)) problems.Add($"{s.Name} evolves knowing unknown move {e.Move}");
            if (e.Species != null && !speciesNames.Contains(e.Species)) problems.Add($"{s.Name} evolves with unknown species {e.Species}");
        }
        if (s.Abilities.Count == 0) problems.Add($"{s.Name} has no ability");
        if (s.Learnset.Count == 0) problems.Add($"{s.Name} learns no moves");

        // Plan 03 · D11: a form's moves and abilities, and the forms evolutions start from and lead to
        foreach (var f in s.Forms ?? new())
        {
            problems.AddRange((f.Learnset ?? new()).Where(l => !moveNames.Contains(l.MoveName)).Select(l => $"{f.Name} learns unknown move {l.MoveName}"));
            problems.AddRange((f.Abilities ?? new()).Append(f.HiddenAbility).OfType<string>().Where(a => !abilityNames.Contains(a)).Select(a => $"{f.Name} has unknown ability {a}"));
            if (f.Types is { Count: 0 or > 2 }) problems.Add($"{f.Name} has {f.Types.Count} types");
        }
        foreach (var e in s.Evolutions ?? new())
        {
            if (e.FromForm != null && s.Form(e.FromForm) == null) problems.Add($"{s.Name} evolves from unknown form {e.FromForm}");
            if (e.TargetForm != null && species.FirstOrDefault(x => x.Name == e.TargetSpecies)?.Form(e.TargetForm) == null)
                problems.Add($"{s.Name} evolves into unknown form {e.TargetForm}");
        }
    }
    Unique("form name", species.SelectMany(s => (s.Forms ?? new()).Select(f => f.Name)).Concat(speciesNames));
    problems.AddRange(items.Where(i => i.TeachesMove != null && !moveNames.Contains(i.TeachesMove)).Select(i => $"{i.Name} teaches unknown move {i.TeachesMove}"));
    problems.AddRange(items.Where(i => i.MegaStone is { } m && species.FirstOrDefault(x => x.Name == m.Species)?.Form(m.Form) == null)
        .Select(i => $"{i.Name} brings out unknown form {i.MegaStone!.Form}"));
    problems.AddRange(items.Where(i => i.MegaStone?.HeldByForm is { } held && species.FirstOrDefault(x => x.Name == i.MegaStone.Species)?.Form(held) == null)
        .Select(i => $"{i.Name} is held by unknown form {i.MegaStone!.HeldByForm}"));

    if (problems.Count > 0)
    {
        foreach (var p in problems) Console.Error.WriteLine(p);
        throw new InvalidDataException($"{problems.Count} problems in the imported data");
    }
}

/// <summary>Hand corrections: <c>Overrides/&lt;file&gt;.json</c> maps a name to the fields to change (null removes one);
/// a name the import doesn't have adds a whole new entry.</summary>
static class Overrides
{
    /// <summary>The specialty counters of <c>Overrides/marts.json</c> (<c>{ "specialties": { key: [items] } }</c>), added to the imported ones.</summary>
    public static void AddMarts(PokemonPlatinumEngine.Data.MartData marts, string path)
    {
        if (!File.Exists(path)) return;
        var node = JsonNode.Parse(File.ReadAllText(path), documentOptions: new() { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true })!.AsObject();
        foreach (var (key, stock) in node["specialties"]!.AsObject())
            marts.Specialties[key] = stock!.AsArray().Select(i => (string)i!).ToList();
    }

    public static List<T> Apply<T>(List<T> records, string path)
    {
        if (!File.Exists(path)) return records;
        var patches = JsonNode.Parse(File.ReadAllText(path), documentOptions: new() { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true })!.AsObject();
        var nodes = JsonNode.Parse(PokemonPlatinumEngine.Data.GameDataFiles.Serialize(records))!.AsArray();
        foreach (var (name, patch) in patches)
        {
            var target = nodes.OfType<JsonObject>().FirstOrDefault(n => (string?)n["name"] == name);
            if (target == null)
            {
                var added = patch!.DeepClone().AsObject();
                added["name"] = name;
                nodes.Add(added);
                continue;
            }
            foreach (var (key, value) in patch!.AsObject())
            {
                if (value == null) target.Remove(key);
                else target[key] = value.DeepClone();
            }
        }
        return System.Text.Json.JsonSerializer.Deserialize<List<T>>(nodes.ToJsonString(), PokemonPlatinumEngine.Data.GameDataFiles.Json)!;
    }
}
