using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PokemonPlatinumEngine.Core;

/// <summary>One line of a scenario: its verb, the words after it, and the line it was written on.</summary>
public sealed record ScenarioStep(string Verb, IReadOnlyList<string> Args, int Line)
{
    /// <summary>Whether the verb is one of the developer's commands (<see cref="DevCommands"/>), run through their table.</summary>
    public bool IsCommand => DevCommands.Find(Verb) != null;

    public override string ToString() => Args.Count == 0 ? Verb : Verb + " " + string.Join(" ", Args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
}

/// <summary>A scenario that can't be read: every mistake, each with its file and line.</summary>
public sealed class ScenarioException : Exception
{
    public IReadOnlyList<string> Problems { get; }

    public ScenarioException(IReadOnlyList<string> problems) : base(string.Join(Environment.NewLine, problems)) => Problems = problems;
}

/// <summary>
/// A scenario (plan 16 · T17): a <c>.scn</c> file of lines a harness plays on a game, read into checked steps before
/// anything is played, a mistake named by its file and line as <c>ScriptParser</c> names one. A line is one of the
/// developer's commands (<see cref="DevCommands"/>: <c>warp</c>, <c>time</c>, <c>weather</c>, <c>give</c>, <c>pokemon</c>,
/// <c>heal</c>, <c>flag</c>, <c>var</c>) or one of the harness's verbs (<see cref="Verbs"/>). <c>#</c> begins a comment, a
/// word with spaces is written in double quotes, and the first line begins a game: <c>newgame</c> or <c>load</c>.
/// Named apart from the test project's battle <c>Scenario</c>.
/// </summary>
public sealed class ShotScenario
{
    /// <summary>The scenario's name: its file's, without the extension. Its shots and state go in a folder of that name.</summary>
    public string Name { get; }

    public string File { get; }

    public IReadOnlyList<ScenarioStep> Steps { get; }

    /// <summary>The flags and variables its lines name (<c>flag</c>, <c>var</c>, <c>expect flag.X</c>), which every snapshot it takes reports.</summary>
    public IReadOnlyList<string> Watched { get; }

    private ShotScenario(string name, string file, List<ScenarioStep> steps, List<string> watched)
    {
        Name = name;
        File = file;
        Steps = steps;
        Watched = watched;
    }

    /// <summary>A harness's verb: how it is written, how many words it takes, and a check of those words (null when good).</summary>
    public sealed record Verb(string Name, string Usage, int Least, int Most, Func<IReadOnlyList<string>, string?> Check);

    /// <summary>The harness's own verbs, beside the developer's commands.</summary>
    public static IReadOnlyList<Verb> Verbs { get; } = new Verb[]
    {
        new("newgame", "newgame [seed] [room]", 0, 2, a => a.Select(w => w == "room" ? null : DevCommands.Int(w, "the seed")).FirstOrDefault(p => p != null)),
        new("load", "load <save> [seed]", 1, 2, a => a.Count > 1 ? DevCommands.Int(a[1], "the seed") : null),
        new("at", "at <map> <x> <y> <up|down|left|right>", 4, 4, a => DevCommands.Int(a[1], "x") ?? DevCommands.Int(a[2], "y") ?? DevCommands.Facing(a[3])),
        // A place in an open map (plan 32 · OW10): a marker of it, or x, y and z in metres with a yaw in degrees (nought south, 90 east)
        new("at3", "at3 <open map> <marker> | at3 <open map> <x> <y> <z> [yaw]", 2, 5, CheckAt3),
        // The open map's orbit camera (plan 32 · OW10): posed by its yaw, pitch and arm, sent to look at a marker, or brought home
        new("look", "look <yaw> [pitch] [arm] | look <marker> [seconds] | look home", 1, 3, CheckLook),
        new("step", "step <up|down|left|right> <n> [running]", 2, 3,
            a => DevCommands.Facing(a[0]) ?? DevCommands.Int(a[1], "the steps", 1, 500) ?? (a.Count > 2 && a[2] != "running" ? $"'{a[2]}' is not 'running'" : null)),
        new("press", "press <action>", 1, 1, a => ActionOf(a[0]) != null ? null : $"'{a[0]}' is no action ({string.Join(", ", Enum.GetNames<GameAction>())})"),
        new("talk", "talk", 0, 0, _ => null),
        new("answer", "answer <n>", 1, 1, a => DevCommands.Int(a[0], "the answer (0 is the first)", 0, 99)),
        new("read", "read", 0, 0, _ => null),
        new("next", "next", 0, 0, _ => null),
        new("wait", "wait <seconds>", 1, 1, a => SecondsOf(a[0]) != null ? null : $"the wait must be a number of seconds from 0 to 600, not '{a[0]}'"),
        new("wild", "wild <species or form> <level>", 2, 2, a => DevCommands.Int(a[1], "the level", 1, Data.Ruleset.HighestLevel)),
        new("trainer", "trainer <id>", 1, 1, _ => null),
        new("menu", "menu", 0, 0, _ => null),
        new("move", "move <n>", 1, 1, a => DevCommands.Int(a[0], "the move (0 is the first)", 0, 3)),
        new("shot", "shot <name> [x y w h scale]", 1, 6, a => a.Count is not (1 or 6) ? "a shot is a name, or a name and x y w h scale"
            : a[0].IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ? $"'{a[0]}' can't be a file's name"
            : a.Skip(1).Select((w, i) => DevCommands.Int(w, new[] { "x", "y", "w", "h", "scale" }[i], i < 2 ? 0 : 1)).FirstOrDefault(p => p != null)),
        new("state", "state", 0, 0, _ => null),
        new("expect", "expect <field> <value>", 2, 2, _ => null),
        // A player's report played back to a moment, which the scenario goes on from (plan 16 · T20)
        new("replay", "replay <folder> [to <seconds>]", 1, 3, a => a.Count == 1 ? null
            : a.Count == 3 && a[1] == "to" && double.TryParse(a[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double s) && s >= 0 ? null
            : "a replay is a folder, or a folder, 'to' and a number of seconds")
    };

    public static Verb? FindVerb(string name) => Verbs.FirstOrDefault(v => v.Name == name);

    /// <summary>A number in metres or degrees as a scenario writes it (a point, never a comma), or null.</summary>
    public static double? NumberOf(string word) =>
        double.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out double n) && double.IsFinite(n) ? n : null;

    // at3's words: a map and a marker (a word that is no number), or a map, x, y, z in metres and a yaw in degrees
    private static string? CheckAt3(IReadOnlyList<string> a)
    {
        if (a.Count == 2) return NumberOf(a[1]) == null ? null : $"'{a[1]}' is a number: a place is a marker, or x, y and z";
        if (a.Count < 4) return "a place is a marker, or x, y and z in metres (and a yaw in degrees)";
        string[] names = { "x", "y", "z", "the yaw" };
        for (int i = 1; i < a.Count; i++)
            if (NumberOf(a[i]) is not { } n || Math.Abs(n) > 1_000_000) return $"{names[i - 1]} must be a number of {(i == 4 ? "degrees" : "metres")}, not '{a[i]}'";
        return null;
    }

    // look's words: home; a yaw with a pitch and an arm in degrees and metres; or a marker with the seconds the look takes
    private static string? CheckLook(IReadOnlyList<string> a)
    {
        if (a[0] == "home") return a.Count == 1 ? null : "'look home' takes nothing after it";
        if (NumberOf(a[0]) == null)
            return a.Count > 2 ? "a look at a marker is its name and the seconds it takes"
                : a.Count == 2 && SecondsOf(a[1]) == null ? $"the look's seconds must be a number from 0 to 600, not '{a[1]}'" : null;
        if (a.Count > 1 && NumberOf(a[1]) == null) return $"the pitch must be a number of degrees, not '{a[1]}'";
        if (a.Count > 1 && NumberOf(a[1])!.Value is < -60 or > 89) return "the pitch is from -60 to 89 degrees";
        if (a.Count > 2 && (NumberOf(a[2]) is not { } arm || arm <= 0 || arm > 120)) return $"the arm must be a number of metres over nought and up to 120, not '{a[2]}'";
        return null;
    }

    /// <summary>An action by its name, any case (<c>confirm</c>, <c>Menu</c>).</summary>
    public static GameAction? ActionOf(string word) =>
        Enum.TryParse<GameAction>(word, ignoreCase: true, out var action) && Enum.IsDefined(action) && !int.TryParse(word, out _) ? action : null;

    public static double? SecondsOf(string word) =>
        double.TryParse(word, NumberStyles.Float, CultureInfo.InvariantCulture, out double s) && s >= 0 && s <= 600 ? s : null;

    /// <summary>Reads a scenario file.</summary>
    public static ShotScenario Load(string path) => Parse(System.IO.File.ReadAllText(path), path);

    /// <summary>Reads a scenario's text, or throws with every mistake in it, each as <c>file:line: what is wrong</c>.</summary>
    public static ShotScenario Parse(string text, string file)
    {
        var steps = new List<ScenarioStep>();
        var problems = new List<string>();
        var watched = new List<string>();
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];
            int hash = line.IndexOf('#');
            if (hash >= 0 && line[..hash].Count(c => c == '"') % 2 == 0) line = line[..hash];
            if (line.Trim().Length == 0) continue;
            void Problem(string what) => problems.Add($"{file}:{number}: {what}");

            var words = DevCommands.Split(line);
            if (words == null) { Problem("a quote is never closed"); continue; }
            string verb = words[0].ToLowerInvariant();
            var args = words.Skip(1).ToList();

            if (DevCommands.Find(verb) != null)
            {
                if (DevCommands.Check(verb, args) is { } why) { Problem(why); continue; }
                if (verb == "flag" && !watched.Contains("flag." + args[0])) watched.Add("flag." + args[0]);
                if (verb == "var" && !watched.Contains("var." + args[0])) watched.Add("var." + args[0]);
            }
            else if (FindVerb(verb) is { } known)
            {
                if (args.Count < known.Least || args.Count > known.Most)
                {
                    Problem($"'{verb}' takes {(known.Least == known.Most ? known.Least.ToString(CultureInfo.InvariantCulture) : $"{known.Least} to {known.Most}")} word(s) after it: {known.Usage}");
                    continue;
                }
                if (known.Check(args) is { } why) { Problem(why); continue; }
                if (verb == "expect" && (args[0].StartsWith("flag.", StringComparison.Ordinal) || args[0].StartsWith("var.", StringComparison.Ordinal)) && !watched.Contains(args[0]))
                    watched.Add(args[0]);
            }
            else
            {
                Problem($"'{words[0]}' is no command or verb");
                continue;
            }

            if (steps.Count == 0 && verb is not ("newgame" or "load" or "replay"))
                Problem($"a scenario begins a game first (newgame, load or replay), not with '{verb}'");
            steps.Add(new ScenarioStep(verb, args, number));
        }
        if (steps.Count == 0 && problems.Count == 0) problems.Add($"{file}:1: the scenario has no lines");
        if (problems.Count > 0) throw new ScenarioException(problems);
        return new ShotScenario(Path.GetFileNameWithoutExtension(file), file, steps, watched);
    }
}
