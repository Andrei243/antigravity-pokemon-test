using System.Text.RegularExpressions;

namespace MapImporter.Gba;

/// <summary>
/// The Trick House's puzzles laid block by block (plan 21 · H12, part j). Emerald's second and third puzzles change
/// their rooms with <c>setmetatile</c> as their buttons are stepped on: the second covers a hole for each of its four
/// buttons, the third swaps its red doors and its blue ones at every press. Each state is written as a layout of its
/// own (<see cref="GbaMaps.GbaPatch"/>), read from the map's own <c>scripts.inc</c> (its labelled runs of
/// <c>setmetatile</c>, the metatiles named in <c>metatile_labels.h</c>), and swapped in by a variable of the room that
/// Hoenn's <c>world.json</c> names: the second's <c>VAR_TEMP_5</c> as the sum of the buttons pressed (1, 2, 4 and 8),
/// the third's <c>VAR_TEMP_7</c> as five times its doors' state and the last button pressed.
/// </summary>
public static class GbaTrickHouse
{
    private const string Puzzle2 = "LAYOUT_ROUTE110_TRICK_HOUSE_PUZZLE2", Puzzle3 = "LAYOUT_ROUTE110_TRICK_HOUSE_PUZZLE3";
    private const int ButtonUp = 0x258, ButtonPressed = 0x259, Shuttered = 0x26A;

    /// <summary>The second puzzle's buttons and the hole each covers (<c>…PressButton1</c> to <c>4</c>).</summary>
    private static readonly ((int X, int Y) Button, (int X, int Y) Hole)[] HolesOf =
    {
        ((11, 12), (1, 13)), ((0, 4), (5, 6)), ((14, 5), (7, 15)), ((7, 11), (14, 12)),
    };

    /// <summary>The third puzzle's buttons (<c>…UpdateButtonMetatiles</c>), first to fourth.</summary>
    private static readonly (int X, int Y)[] Buttons3 = { (4, 14), (3, 11), (12, 5), (8, 2) };

    /// <summary>The third puzzle's layout as it is first seen: its doors as state 0 and every button up.</summary>
    public const string Puzzle3First = Puzzle3 + "_0";

    /// <summary>The layouts swapped in (<see cref="GbaWorld.Alternates"/>), in the order their chunks are numbered.</summary>
    public static IEnumerable<(string Map, string Layout)> Alternates()
    {
        for (int sum = 1; sum <= 15; sum++) yield return ("MAP_ROUTE110_TRICK_HOUSE_PUZZLE2", $"{Puzzle2}_{sum}");
        foreach (int value in Puzzle3Values) yield return ("MAP_ROUTE110_TRICK_HOUSE_PUZZLE3", $"{Puzzle3}_{value}");
    }

    /// <summary>The third puzzle's values of <c>VAR_TEMP_7</c> but the first: state 0 or 1 by five, and the button 1 to 4.</summary>
    public static readonly int[] Puzzle3Values = { 1, 2, 3, 4, 6, 7, 8, 9 };

    /// <summary>Every layout of the two puzzles, read from Emerald's scripts under <paramref name="dir"/>.</summary>
    public static IEnumerable<GbaMaps.GbaPatch> Patches(string dir)
    {
        string scripts = Path.Combine(dir, "data", "maps", "Route110_TrickHousePuzzle3", "scripts.inc");
        string labels = Path.Combine(dir, "include", "constants", "metatile_labels.h");
        // The second puzzle's are its buttons and holes alone
        for (int sum = 1; sum <= 15; sum++)
        {
            var blocks = new List<(int, int, int, bool)>();
            for (int b = 0; b < 4; b++)
                if ((sum & (1 << b)) != 0)
                {
                    blocks.Add((HolesOf[b].Button.X, HolesOf[b].Button.Y, ButtonPressed, false));
                    blocks.Add((HolesOf[b].Hole.X, HolesOf[b].Hole.Y, Shuttered, false));
                }
            yield return new GbaMaps.GbaPatch($"{Puzzle2}_{sum}", Puzzle2, blocks.ToArray());
        }
        if (!File.Exists(scripts) || !File.Exists(labels)) yield break;
        var named = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(File.ReadAllText(labels), @"#define (METATILE_\w+)\s+0x([0-9A-Fa-f]+)"))
            named[m.Groups[1].Value] = Convert.ToInt32(m.Groups[2].Value, 16);
        var lines = File.ReadAllLines(scripts);
        var state0 = Run(lines, "Route110_TrickHousePuzzle3_EventScript_SetDoorsState0", named);
        var state1 = Run(lines, "Route110_TrickHousePuzzle3_EventScript_SetDoorsState1", named);
        foreach (int value in Puzzle3Values.Prepend(0))
        {
            var blocks = new List<(int, int, int, bool)>(value >= 5 ? state1 : state0);
            int pressed = value % 5;
            for (int b = 0; b < 4; b++) blocks.Add((Buttons3[b].X, Buttons3[b].Y, b + 1 == pressed ? ButtonPressed : ButtonUp, false));
            yield return new GbaMaps.GbaPatch($"{Puzzle3}_{value}", Puzzle3, blocks.ToArray());
        }
    }

    /// <summary>A label's run of <c>setmetatile x, y, METATILE_…, TRUE|FALSE</c> lines, to its <c>return</c>.</summary>
    private static List<(int, int, int, bool)> Run(string[] lines, string label, Dictionary<string, int> named)
    {
        var blocks = new List<(int, int, int, bool)>();
        bool on = false;
        foreach (var line in lines)
        {
            if (line.StartsWith(label + "::", StringComparison.Ordinal)) { on = true; continue; }
            if (!on) continue;
            if (line.Trim() == "return") break;
            var m = Regex.Match(line, @"setmetatile (\d+), (\d+), (METATILE_\w+), (TRUE|FALSE)");
            if (m.Success && named.TryGetValue(m.Groups[3].Value, out int metatile))
                blocks.Add((int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), metatile, m.Groups[4].Value == "TRUE"));
        }
        return blocks;
    }
}
