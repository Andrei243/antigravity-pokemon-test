// Rebuilds species.json, moves.json, abilities.json and items.json in PokemonPlatinumEngine/Data from the Platinum
// decompilation and PokeAPI, then applies the hand corrections in tools/DataImporter/Overrides and writes the coverage
// report docs/mechanics/coverage.md. See tools/DataImporter/README.md.
//
//   dotnet run --project tools/DataImporter                         fetch the pinned sources (once) and import everything
//   dotnet run --project tools/DataImporter -- --decomp <dir> --pokeapi <dir/data/v2/csv>   use existing checkouts
//   dotnet run --project tools/DataImporter -- --last-species 493   stop after Platinum's National Pokédex

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
Console.WriteLine("Items…");
var evolutionItems = species.SelectMany(s => s.Evolutions ?? new()).Select(e => e.Item).OfType<string>().ToHashSet();
var importedItems = importer.Items(evolutionItems, moves);
importer.AddLaterMechanics(importedItems, showdown);
var items = Overrides.Apply(importedItems, Path.Combine(overrides, "items.json")).OrderBy(i => i.Id).ToList();

Check(species, moves, abilities, items);

Write(Path.Combine(outDir, PokemonDatabase.FileName), GameDataFiles.Serialize(species));
Write(Path.Combine(outDir, MoveDatabase.FileName), GameDataFiles.Serialize(moves));
Write(Path.Combine(outDir, AbilityDatabase.FileName), GameDataFiles.Serialize(abilities));
Write(Path.Combine(outDir, ItemDatabase.FileName), GameDataFiles.Serialize(items));
Write(Coverage.ReportPath(repo), Coverage.Report(species, moves, abilities, items));
Console.WriteLine($"{species.Count} species, {moves.Count} moves, {abilities.Count} abilities and {items.Count} items in {outDir}");
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
