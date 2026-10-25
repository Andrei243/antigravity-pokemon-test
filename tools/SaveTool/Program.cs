// Dumps, validates, migrates and diffs the game's saves without the game. See tools/SaveTool/README.md.
//
//   dotnet run --project tools/SaveTool -- dump <save> [--all]              who, where, the team, the boxes, the bag, the story's size
//   dotnet run --project tools/SaveTool -- validate <save>...               every Pokémon, item, move, level, IV, place and version held to the data; ends with 1 on an error
//   dotnet run --project tools/SaveTool -- migrate <save> [--out <file>]    what the game's migrations would do to it, line by line; --out writes the migrated copy
//   dotnet run --project tools/SaveTool -- diff <save> <other save>         every place two saves differ; ends with 1 when they do

using System.Text.Json;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using SaveTool;

if (args.Length == 0 || args[0] is "--help" or "-h" or "help")
{
    Console.WriteLine("SaveTool: dump <save> [--all] | validate <save>... | migrate <save> [--out <file>] | diff <save> <other save>");
    return args.Length == 0 ? 2 : 0;
}

string command = args[0];
var files = new List<string>();
string? outFile = null;
bool all = false, quiet = false;
for (int i = 1; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--out" when i + 1 < args.Length: outFile = args[++i]; break;
        case "--all": all = true; break;
        case "--errors": quiet = true; break;
        default:
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"What is '{args[i]}'? The switches are --out <file> (migrate), --all (dump) and --errors (validate: no notes).");
                return 2;
            }
            files.Add(args[i]);
            break;
    }
}
bool right = command switch
{
    "dump" or "migrate" => files.Count == 1,
    "validate" => files.Count >= 1,
    "diff" => files.Count == 2,
    _ => false
};
if (!right)
{
    Console.Error.WriteLine(command is "dump" or "validate" or "migrate" or "diff"
        ? $"'{command}' is given the wrong number of saves."
        : $"What is '{command}'? The commands are dump, validate, migrate and diff.");
    return 2;
}

// The game plays by the modern rules, and a save is read by them
Ruleset.Use(RulesPreset.Modern);
MoveDatabase.Initialize();
PokemonDatabase.Initialize();
ItemDatabase.Initialize();
MapDatabase.Initialize();

try
{
    switch (command)
    {
        case "dump":
            Console.Write(SaveDump.Describe(Read(files[0]).Save, all));
            return 0;

        case "validate":
        {
            int errors = 0;
            foreach (string file in files)
            {
                var (save, text) = Read(file);
                var findings = SaveChecks.Validate(save, text);
                var shown = findings.Where(f => !quiet || f.Severity == Severity.Error).ToList();
                Console.WriteLine($"{file}: {findings.Count(f => f.Severity == Severity.Error)} error(s), {findings.Count(f => f.Severity == Severity.Note)} note(s)");
                foreach (var finding in shown) Console.WriteLine("  " + finding);
                errors += findings.Count(f => f.Severity == Severity.Error);
            }
            return errors > 0 ? 1 : 0;
        }

        case "migrate":
        {
            var (save, _) = Read(files[0]);
            var result = SaveMigrate.Run(save);
            if (result.Unchanged) Console.WriteLine($"{files[0]}: already as the game has it; nothing to migrate.");
            else
            {
                Console.WriteLine($"{files[0]}: {result.Changes.Count} change(s)");
                foreach (string change in result.Changes) Console.WriteLine("  " + change);
            }
            if (outFile != null)
            {
                if (Path.GetFullPath(outFile) == Path.GetFullPath(files[0]))
                {
                    Console.Error.WriteLine("--out names the save itself: the tool never writes over a save. Name another file.");
                    return 2;
                }
                File.WriteAllText(outFile, SaveManager.Write(result.Migrated));
                Console.WriteLine($"Written to {outFile}.");
            }
            else if (!result.Unchanged) Console.WriteLine("(Nothing was written: --out <file> writes the migrated copy.)");
            return 0;
        }

        case "diff":
        {
            var lines = SaveDiff.Compare(Read(files[0]).Text, Read(files[1]).Text);
            foreach (string line in lines) Console.WriteLine(line);
            Console.WriteLine(lines.Count == 0 ? "The saves are the same." : $"{lines.Count} difference(s).");
            return lines.Count == 0 ? 0 : 1;
        }
    }
}
catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(e.Message);
    return 2;
}
return 2;

static (SaveData Save, string Text) Read(string path)
{
    string text = File.ReadAllText(path);
    return (SaveManager.Read(text) ?? throw new JsonException($"{path} reads as nothing."), text);
}
