using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The names the interface gives the game's own enums (plan 17 · N1b): a type, a nature, a status's three letters,
/// a move's category and a stat, each a key of the string table by the enum's name. GPU-free.
/// </summary>
public static class Words
{
    public static string Type(PokemonType type) => Text.Of($"types.{type}");

    public static string Nature(Nature nature) => Text.Of($"natures.{nature}");

    /// <summary>A status as the pill shows it (PSN, BRN…); nothing for none.</summary>
    public static string Status(StatusCondition status) => status == StatusCondition.None ? "" : Text.Of($"statuses.{status}");

    /// <summary>A move's category as its tag shows it (PHYSICAL, SPECIAL, STATUS).</summary>
    public static string Category(MoveCategory category) => Text.Of($"categories.{category}");

    /// <summary>A stat's short name as the summary shows it (HP to Speed).</summary>
    public static string Stat(StatType stat) => Text.Of($"summary.stat.{stat}");

    /// <summary>A day as the summary writes it: its number, its month's name and its year.</summary>
    public static string Date(DateTime day) =>
        Text.Of("summary.date", ("day", day.Day), ("month", Text.Of($"summary.month.{day.Month}")), ("year", day.Year));
}
