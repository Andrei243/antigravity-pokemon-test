using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Data;

/// <summary>One item of the common counter's list and the step of the badge count it is sold from.</summary>
public sealed class MartItem
{
    public string Item { get; set; } = string.Empty;
    public int Badges { get; set; }
}

/// <summary>What <c>marts.json</c> holds: the common counter's list and every specialty counter's, by key.</summary>
public sealed class MartData
{
    public List<MartItem> Common { get; set; } = new();
    public Dictionary<string, List<string>> Specialties { get; set; } = new();
}

/// <summary>
/// The Poké Marts' stock (plan 06 · R11), imported from the decompilation's lists (<c>include/data/mart_items.h</c>):
/// every Mart has a counter whose stock grows with the badges (<c>ScrCmd_PokeMartCommon</c>) and, in most towns, a
/// second counter with the town's specialties (<c>ScrCmd_PokeMartSpecialties</c>), named by the original's id
/// (<c>jubilife</c>, <c>eterna_house</c> for the herb shop). A clerk carries the key of their counter
/// (<see cref="Overworld.NPC.Mart"/>); one with none keeps the common counter.
/// </summary>
public static class MartDatabase
{
    public const string FileName = "marts.json";

    private static readonly MartData Data = GameDataFiles.Load<MartData>(FileName);

    public static IReadOnlyList<MartItem> Common => Data.Common;

    public static IReadOnlyDictionary<string, List<string>> Specialties => Data.Specialties;

    /// <summary>
    /// The step of the common list that so many badges open, as the original reckons it: one step with none, two
    /// with one or two, three with three or four, four with five or six, five with seven, six with all eight.
    /// </summary>
    public static int StepFor(int badges) => badges switch
    {
        <= 0 => 1,
        1 or 2 => 2,
        3 or 4 => 3,
        5 or 6 => 4,
        7 => 5,
        8 => 6,
        _ => 1
    };

    /// <summary>
    /// What a counter sells: the common list for the badges (<paramref name="mart"/> null or empty), or a specialty
    /// counter's own list, in the original's order. An unknown key sells nothing.
    /// </summary>
    public static List<ItemData> Stock(string? mart, int badges)
    {
        IEnumerable<string> names = string.IsNullOrEmpty(mart)
            ? Data.Common.Where(i => StepFor(badges) >= i.Badges).Select(i => i.Item)
            : Data.Specialties.GetValueOrDefault(mart) ?? new List<string>();
        return names.Select(ItemDatabase.Get).OfType<ItemData>().ToList();
    }
}
