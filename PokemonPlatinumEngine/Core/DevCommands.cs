using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>What a developer's command came to: done, with a line to print, or refused, with why.</summary>
internal readonly record struct DevResult(bool Ok, string Text)
{
    public static DevResult Done(string text) => new(true, text);
    public static DevResult Refused(string why) => new(false, why);
}

/// <summary>
/// One developer's command (plan 16 · T6, T17): its name, how it is written, an example that is a good line, how many
/// words it takes after its name, a check of those words that needs no game (null when they are good, otherwise why
/// not), and what it does to a game.
/// </summary>
internal sealed record DevCommand(
    string Name, string Usage, string Example, int Least, int Most,
    Func<string[], string?> Check, Func<GameEngine, string[], DevResult> Run);

/// <summary>
/// The developer's commands as one table (plan 16 · T17, written ahead of T6's console, which takes this table as its
/// own): a scenario's line that names one of them is run through it, so the scenarios and the console never grow two
/// languages. Every command reaches the game through its <see cref="GameEngine.Driver"/>, and none draws anything.
/// What they print is for the developer and stays in English.
/// </summary>
internal static class DevCommands
{
    /// <summary>The table, in the order <c>help</c> lists it.</summary>
    public static IReadOnlyList<DevCommand> Commands { get; } = new DevCommand[]
    {
        new("warp", "warp <map> <x> <y> [up|down|left|right]", "warp Sinnoh 116 886 down", 3, 4,
            a => Int(a[1], "x") ?? Int(a[2], "y") ?? (a.Length > 3 ? Facing(a[3]) : null), Warp),
        new("time", "time <morning|day|twilight|night|latenight|clock> [yyyy-mm-dd]", "time night 2026-06-01", 1, 2,
            a => (a[0] == "clock" || TimeOf(a[0]) != null ? null : $"'{a[0]}' is no time of day")
                 ?? (a.Length > 1 && DateOf(a[1]) == null ? $"'{a[1]}' is no date (yyyy-mm-dd)" : null), Time),
        new("weather", "weather <" + string.Join("|", Enum.GetNames<FieldWeather>().Select(n => n.ToLowerInvariant())) + "|off>", "weather rain", 1, 1,
            a => a[0] == "off" || WeatherOf(a[0]) != null ? null : $"'{a[0]}' is no weather", Weather),
        new("give", "give <item> [count]", "give \"Poké Ball\" 5", 1, 2,
            a => a.Length > 1 ? Int(a[1], "count", 1) : null, Give),
        new("pokemon", "pokemon <species or form> <level>", "pokemon Starly 5", 2, 2,
            a => Int(a[1], "level", 1, Ruleset.HighestLevel), AddPokemon),
        new("heal", "heal", "heal", 0, 0, _ => null, Heal),
        new("flag", "flag <name> [on|off]", "flag FLAG_HAS_POKEDEX on", 1, 2,
            a => a.Length > 1 && a[1] != "on" && a[1] != "off" ? $"a flag is on or off, not '{a[1]}'" : null, Flag),
        new("var", "var <name> [value]", "var VAR_SPAWN_LOCATION 1", 1, 2,
            a => a.Length > 1 ? Int(a[1], "value", int.MinValue) : null, Var),
        // Plan 16 · T6's own: the console takes this table over, and these are its commands beside T17's
        new("flags", "flags [part of a name]", "flags POKEDEX", 0, 1, _ => null, Flags),
        new("encounters", "encounters [on|off]", "encounters off", 0, 1, a => a.Length > 0 ? OnOff(a[0]) : null, Encounters),
        new("trace", "trace [on|off]", "trace on", 0, 1, a => a.Length > 0 ? OnOff(a[0]) : null, Trace),
        new("reload", "reload [scripts|maps|music|models|all]", "reload scripts", 0, 1,
            a => a.Length > 0 && Reload.Named(a[0]) == null ? $"'{a[0]}' is nothing to reload (scripts, maps, music, models, all)" : null, ReloadFiles),
        new("fps", "fps [on|off|sections]", "fps on", 0, 1,
            a => a.Length > 0 && a[0] is not ("on" or "off" or "sections") ? $"fps is on, off or sections, not '{a[0]}'" : null, Fps),
        new("screenshot", "screenshot", "screenshot", 0, 0, _ => null, (engine, _) => engine.DevShot()),
        new("help", "help [command]", "help warp", 0, 1, a => a.Length > 0 && Find(a[0]) == null ? $"no command is called '{a[0]}'" : null, Help),
        new("layers", "layers <" + string.Join("|", Graphics.DebugLayers.Names.Select(n => n.Name)) + "|all|off>...", "layers solid,sight", 1, 9, Graphics.DebugLayers.CheckWords, Graphics.DebugLayers.Command)
    };

    public static DevCommand? Find(string name) => Commands.FirstOrDefault(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Why a command's words are no good, or null when they are: the name, how many words, and what each must be.</summary>
    public static string? Check(string name, IReadOnlyList<string> args)
    {
        if (Find(name) is not { } command) return $"no command is called '{name}'";
        if (args.Count < command.Least || args.Count > command.Most)
            return $"'{command.Name}' takes {(command.Least == command.Most ? command.Least.ToString(CultureInfo.InvariantCulture) : $"{command.Least} to {command.Most}")} word(s) after it: {command.Usage}";
        return command.Check(args.ToArray());
    }

    /// <summary>Runs a command on a game: checked first, so a bad line is refused with why and changes nothing.</summary>
    public static DevResult Run(GameEngine engine, string name, IReadOnlyList<string> args)
    {
        if (Check(name, args) is { } why) return DevResult.Refused(why);
        return Find(name)!.Run(engine, args.ToArray());
    }

    /// <summary>A line as its words: split at spaces, a word in double quotes kept whole (<c>give "Poké Ball" 5</c>). Null when a quote is never closed.</summary>
    public static List<string>? Split(string line)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        bool quoted = false, any = false;
        foreach (char c in line)
        {
            if (c == '"') { quoted = !quoted; any = true; continue; }
            if (!quoted && char.IsWhiteSpace(c))
            {
                if (any) words.Add(word.ToString());
                word.Clear();
                any = false;
                continue;
            }
            word.Append(c);
            any = true;
        }
        if (quoted) return null;
        if (any) words.Add(word.ToString());
        return words;
    }

    // ------------------------------------------------------------------ the checks

    internal static string? Int(string word, string what, int least = 0, int most = int.MaxValue) =>
        int.TryParse(word, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= least && n <= most
            ? null : $"{what} must be a whole number{(least > int.MinValue ? $" from {least}" : "")}{(most < int.MaxValue ? $" to {most}" : "")}, not '{word}'";

    internal static string? Facing(string word) => DirectionOf(word) != null ? null : $"'{word}' is no way to face (up, down, left, right)";

    internal static Direction? DirectionOf(string word) => word.ToLowerInvariant() switch
    {
        "up" or "north" => Direction.Up,
        "down" or "south" => Direction.Down,
        "left" or "west" => Direction.Left,
        "right" or "east" => Direction.Right,
        _ => null
    };

    private static TimeOfDay? TimeOf(string word) =>
        Enum.TryParse<TimeOfDay>(word, ignoreCase: true, out var time) && Enum.IsDefined(time) && !int.TryParse(word, out _) ? time : null;

    private static DateTime? DateOf(string word) =>
        DateTime.TryParseExact(word, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day) ? day : null;

    private static FieldWeather? WeatherOf(string word) =>
        Enum.TryParse<FieldWeather>(word, ignoreCase: true, out var weather) && Enum.IsDefined(weather) && !int.TryParse(word, out _) ? weather : null;

    private static int Number(string word) => int.Parse(word, NumberStyles.Integer, CultureInfo.InvariantCulture);

    // ------------------------------------------------------------------ what they do

    private static DevResult Warp(GameEngine engine, string[] a)
    {
        if (MapDatabase.TryGet(a[0]) is not { } map) return DevResult.Refused($"no map is called '{a[0]}'");
        int x = Number(a[1]), y = Number(a[2]);
        if (!map.InBounds(x, y)) return DevResult.Refused($"{x}, {y} is off {map.Name}, which is {map.Width} by {map.Height}");
        var facing = a.Length > 3 ? DirectionOf(a[3])!.Value : Direction.Down;
        engine.Drive.Place(map, x, y, facing);
        return DevResult.Done($"on {map.Name} at {x}, {y}, facing {facing.ToString().ToLowerInvariant()}");
    }

    private static DevResult Time(GameEngine engine, string[] a)
    {
        engine.Settings.TimeOfDay = a[0] == "clock" ? null : TimeOf(a[0]);
        if (a.Length > 1) GameClock.FixedDate = DateOf(a[1]);
        engine.ApplySettings(window: false);
        return DevResult.Done($"the time is {GameClock.Now.ToString().ToLowerInvariant()} on {GameClock.Today:yyyy-MM-dd}");
    }

    private static DevResult Weather(GameEngine engine, string[] a)
    {
        var map = engine.Drive.Map;
        map.WeatherOverride = a[0] == "off" ? null : WeatherOf(a[0]);
        return DevResult.Done(map.WeatherOverride is { } w ? $"{map.Name} is under {w.ToString().ToLowerInvariant()}" : $"{map.Name} has its own weather again");
    }

    private static DevResult Give(GameEngine engine, string[] a)
    {
        if (ItemDatabase.Get(a[0]) is not { } item) return DevResult.Refused($"no item is called '{a[0]}'");
        int count = a.Length > 1 ? Number(a[1]) : 1;
        engine.Drive.Bag.AddItem(item, count);
        return DevResult.Done($"{count} × {item.Name} into the bag ({engine.Drive.Bag.GetQuantity(item)} now)");
    }

    private static DevResult AddPokemon(GameEngine engine, string[] a)
    {
        var drive = engine.Drive;
        if (drive.Party.IsFull) return DevResult.Refused("the team is full");
        int level = Number(a[1]);
        Pokemon pokemon;
        if (PokemonDatabase.Get(a[0]) is { } species) pokemon = new Pokemon(species, level);
        else if (PokemonDatabase.SpeciesOfForm(a[0]) is { } formOf) pokemon = Pokemon.InForm(formOf, a[0], level);
        else return DevResult.Refused($"no species or form is called '{a[0]}'");
        drive.Party.Add(pokemon);
        drive.Pokedex.RegisterSeen(pokemon.Species.DexNumber);
        drive.Pokedex.RegisterCaught(pokemon.Species.DexNumber);
        drive.GiveIds();
        return DevResult.Done($"{pokemon.Species.Name} of level {level} joins the team ({drive.Party.Count} now)");
    }

    private static DevResult Heal(GameEngine engine, string[] a)
    {
        engine.Drive.Party.HealAll();
        return DevResult.Done("the team is well");
    }

    private static DevResult Flag(GameEngine engine, string[] a)
    {
        var story = engine.Drive.Story;
        if (a.Length > 1)
        {
            if (a[1] == "on") story.Set(a[0]);
            else story.Unset(a[0]);
        }
        return DevResult.Done($"{a[0]} is {(story.Has(a[0]) ? "on" : "off")}");
    }

    private static DevResult Var(GameEngine engine, string[] a)
    {
        var story = engine.Drive.Story;
        if (a.Length > 1) story.SetVar(a[0], Number(a[1]));
        return DevResult.Done($"{a[0]} is {story.Var(a[0])}");
    }

    internal static string? OnOff(string word) => word is "on" or "off" ? null : $"it is on or off, not '{word}'";

    /// <summary>The most flags and variables <c>flags</c> lists at once.</summary>
    public const int MostListed = 40;

    private static DevResult Flags(GameEngine engine, string[] a)
    {
        var story = engine.Drive.Story;
        string part = a.Length > 0 ? a[0] : "";
        var flags = story.Flags.Where(f => f.Contains(part, StringComparison.OrdinalIgnoreCase)).Order(StringComparer.Ordinal).ToList();
        var vars = story.Variables.Where(v => v.Key.Contains(part, StringComparison.OrdinalIgnoreCase)).OrderBy(v => v.Key, StringComparer.Ordinal).ToList();
        var lines = flags.Take(MostListed).Concat(vars.Take(Math.Max(0, MostListed - flags.Count)).Select(v => $"{v.Key} = {v.Value}")).ToList();
        int more = flags.Count + vars.Count - lines.Count;
        lines.Insert(0, $"{flags.Count} flag(s) on and {vars.Count} variable(s){(part.Length > 0 ? $" with '{part}' in the name" : "")}:");
        if (more > 0) lines.Add($"... and {more} more: name a part of a name");
        return DevResult.Done(string.Join("\n", lines));
    }

    private static DevResult Encounters(GameEngine engine, string[] a)
    {
        if (a.Length > 0) engine.WildEncountersOff = a[0] == "off";
        return DevResult.Done(engine.WildEncountersOff ? "no wild Pokémon is met in the field" : "wild Pokémon are met in the field");
    }

    private static DevResult Trace(GameEngine engine, string[] a) =>
        engine.DevTrace(a.Length > 0 ? a[0] == "on" : !engine.Tracing);

    private static DevResult ReloadFiles(GameEngine engine, string[] a) =>
        engine.DevReload(a.Length > 0 ? Reload.Named(a[0])!.Value : ReloadKind.All);

    private static DevResult Fps(GameEngine engine, string[] a) => engine.DevFrameTimes(a.Length > 0 ? a[0] : "on");

    private static DevResult Help(GameEngine engine, string[] a)
    {
        if (a.Length > 0)
        {
            var command = Find(a[0])!;
            return DevResult.Done($"{command.Usage}\n  for example: {command.Example}");
        }
        return DevResult.Done("The commands (a word in double quotes is kept whole; up and down go through the lines typed before):\n"
            + string.Join("\n", Commands.Select(c => "  " + c.Usage)));
    }
}
