// Reads the game's hand-written files (overlays, rooms, scripts) and names what is wrong with them; or draws a room
// or an open area from above. See tools/ContentCheck/README.md.
//
//   dotnet run --project tools/ContentCheck                                  check the game's own files; ends with 1 on a finding
//   dotnet run --project tools/ContentCheck -- --data <dir>                  check another folder laid out like Data/
//   dotnet run --project tools/ContentCheck -- --plan <out dir> <name>...    plans of rooms (by map name) and open areas (by key), as PNGs
//   dotnet run --project tools/ContentCheck -- --plan <out dir> <name>@x,y,w,h   a rectangle of a map's tiles alone
//   ... --tile <pixels>                                                      how large a plan draws a tile (8, 16, 32, 48, 64 or 96; fitted unless said)
//   dotnet run --project tools/ContentCheck -- --rows "<line>"               a line as the text box breaks it (\n for a break), row by row

using System.Diagnostics;
using System.Globalization;
using ContentCheck;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI.Kit;

var clock = Stopwatch.StartNew();
string root = GameDataFiles.Directory;
string? planDir = null;
int? tile = null;
var names = new List<string>();
for (int i = 0; i < args.Length; i++)
{
    string Value() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
    try
    {
        switch (args[i])
        {
            case "--data": root = Value(); break;
            case "--plan": planDir = Value(); break;
            case "--tile": tile = int.Parse(Value(), CultureInfo.InvariantCulture); break;
            case "--rows":
            {
                // A line as the text box breaks it, for whoever is writing one: each row with its width
                string line = Lines.Filled(Value().Replace("\\n", "\n"));
                var rows = DialogueBox.RowsOf(line, GameEngine.VirtualWidth);
                foreach (string row in rows)
                    Console.WriteLine($"{TextMetrics.Measure(row, DialogueBox.TextSize, DialogueBox.TextWeight),7:F1} of {DialogueBox.TextWidth(GameEngine.VirtualWidth):F0}  {row}");
                Console.WriteLine($"{rows.Count} row{(rows.Count == 1 ? "" : "s")}; the box holds {DialogueBox.Rows}.");
                return rows.Count <= DialogueBox.Rows ? 0 : 1;
            }
            default: names.Add(args[i]); break;
        }
    }
    catch (Exception e) when (e is ArgumentException or FormatException)
    {
        Console.Error.WriteLine(e.Message);
        return 2;
    }
}
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"No data folder at {root}");
    return 2;
}
if (planDir == null && names.Count > 0)
{
    Console.Error.WriteLine($"What is '{names[0]}'? The switches are --data <dir>, --plan <out dir> <name>... and --tile <pixels>.");
    return 2;
}
var content = GameContent.Load(root);

if (planDir != null)
{
    if (names.Count == 0)
    {
        Console.Error.WriteLine("--plan <out dir> <name>...: name at least one room (PlayerHouse) or open area (twinleaf_town)");
        return 2;
    }
    int failed = 0;
    foreach (string name in names)
    {
        try
        {
            foreach (var plan in Plans.Of(content, name, tile))
            {
                string path = Path.Combine(planDir, plan.Name.Replace('@', '_').Replace(',', '_') + ".png");
                plan.Save(path);
                Console.WriteLine($"{path}  ({plan.Picture.Width}x{plan.Picture.Height}, {plan.Tile} px a tile)");
                foreach (string line in plan.Legend) Console.WriteLine("  " + line);
            }
        }
        catch (Exception e) when (e is ArgumentException or FormatException)
        {
            failed++;
            Console.Error.WriteLine($"{name}: {e.Message}");
        }
    }
    return failed == 0 ? 0 : 2;
}

var found = Checks.All(content);
foreach (var finding in found) Console.WriteLine(finding);
if (found.Count > 0) Console.WriteLine();
Console.WriteLine($"Read {content.Regions.Sum(r => r.Overlays.Count)} overlays, {content.Rooms.Count} rooms and {content.Scripts.Files.Count} script files of {root} ({clock.Elapsed.TotalSeconds:F1} s).");
Console.WriteLine($"Measured {found.LinesByRows.Values.Sum()} lines against the text box as the game measures them (the typeface's own widths), with {{player}} and {{rival}} at \"{Lines.LongestName}\" and what a script fills in at the widest of its kind: "
    + string.Join(", ", found.LinesByRows.Select(r => $"{r.Value} of {r.Key} row{(r.Key == 1 ? "" : "s")}")) + $"; the box holds {DialogueBox.Rows}.");
if (found.Count == 0)
{
    Console.WriteLine("Nothing found.");
    return 0;
}
Console.WriteLine($"{found.Count} finding{(found.Count == 1 ? "" : "s")}: "
    + string.Join(", ", found.GroupBy(f => f.Kind).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Select(g => $"{g.Count()} {g.Key}")));
return 1;
