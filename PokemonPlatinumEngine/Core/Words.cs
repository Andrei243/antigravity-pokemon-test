using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The names the interface gives the game's own enums (plan 17 · N1b): a type, a nature, a status's three letters,
/// a move's category, a stat and a Badge (plan 17 · N1c), each a key of the string table by the enum's name. GPU-free.
/// </summary>
public static class Words
{
    /// <summary>A type's name: the language's own table first, then PokeAPI's name for it (plan 17 · N3), then English's.</summary>
    public static string Type(PokemonType type) =>
        Text.HasOwn($"types.{type}") ? Text.Of($"types.{type}") : LocalNames.TypeName(type) ?? Text.Of($"types.{type}");

    public static string Nature(Nature nature) =>
        Text.HasOwn($"natures.{nature}") ? Text.Of($"natures.{nature}") : LocalNames.NatureName(nature) ?? Text.Of($"natures.{nature}");

    /// <summary>A status as the pill shows it (PSN, BRN…); nothing for none.</summary>
    public static string Status(StatusCondition status) => status == StatusCondition.None ? "" : Text.Of($"statuses.{status}");

    /// <summary>A move's category as its tag shows it (PHYSICAL, SPECIAL, STATUS).</summary>
    public static string Category(MoveCategory category) => Text.Of($"categories.{category}");

    /// <summary>A stat's short name as the summary shows it (HP to Speed).</summary>
    public static string Stat(StatType stat) => Text.Of($"summary.stat.{stat}");

    /// <summary>A day as the summary writes it: its number, its month's name and its year.</summary>
    public static string Date(DateTime day) =>
        Text.Of("summary.date", ("day", day.Day), ("month", Text.Of($"summary.month.{day.Month}")), ("year", day.Year));

    /// <summary>A day as a stored Pokémon's MET writes it: its number, its month's short name and its year.</summary>
    public static string ShortDate(DateTime day) =>
        Text.Of("summary.date", ("day", day.Day), ("month", Text.Of($"summary.monthShort.{day.Month}")), ("year", day.Year));

    /// <summary>A month's name: January to December.</summary>
    public static string Month(DateTime day)
    {
        int month = day.Month;
        return Text.Of($"summary.month.{month}");
    }

    /// <summary>A day's name: Monday to Sunday.</summary>
    public static string Weekday(DateTime day) => Weekday(day.DayOfWeek);

    /// <summary>A day of the week's name (a television's time check, plan 08 · P8).</summary>
    public static string Weekday(DayOfWeek name) => Text.Of($"summary.weekday.{name}");

    /// <summary>A Badge's name as the Trainer Card writes it under its medallion, by its name in its region's list (Coal, Boulder).</summary>
    public static string Badge(string name) => Text.Of($"badges.{name}");
}
