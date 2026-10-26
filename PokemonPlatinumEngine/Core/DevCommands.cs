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
            a => a.Length > 1 ? Int(a[1], "value", int.MinValue) : null, Var)
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
}
