using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// <c>Data/charms.json</c> (plan 40 · GE5, decision 3; ours; <c>docs/data-files.md</c>, "Charms"): which charm a Pokédex
/// filled earns. Read strictly, as every file a person writes is.
/// </summary>
public sealed class CharmsFile
{
    /// <summary>
    /// The charms the regional Pokédexes seen whole earn, in the order they are earned: the first regional Pokédex the
    /// player completes, whichever region's it is, earns the first, the second the second, and so on.
    /// </summary>
    [DataRequired] public List<string> Regional { get; set; } = new();

    /// <summary>What the National Pokédex caught whole earns (its diploma's goal, <see cref="Pokedex.Goal"/>).</summary>
    [DataRequired] public string National { get; set; } = "";
}

/// <summary>
/// The later games' charms for a Pokédex filled (plan 40 · GE5; <c>docs/mechanics/rulings.md</c>, "The charms, critical
/// captures and EXP for a catch"), GPU-free. Which are earned is read from the Pokédex itself, never kept apart: each
/// regional Pokédex the player has that is seen whole, or whose diploma they hold, earns the next charm of
/// <see cref="CharmsFile.Regional"/>, and the National Pokédex caught whole earns <see cref="CharmsFile.National"/>. A charm
/// is a key item, and one in the bag has been given, so what is due is what is earned and not in the bag: any region's
/// designer hands it over (the script's <c>charm due</c>, <c>charm give</c>). What each does while it is in the bag:
/// <list type="bullet">
/// <item>the Oval Charm, the Day Care's chance of an Egg (<see cref="OvalChance"/>, read in <see cref="DayCare.Step"/>);</item>
/// <item>the Exp. Charm, half as much EXP again in battle (<see cref="ExpShare"/>, read in <c>BattleCore.Give</c>);</item>
/// <item>the Catching Charm, a critical capture's chance doubled (<see cref="Battle.Sim.Formulas.CriticalCapture"/>);</item>
/// <item>the Mark Charm, two more tries at each mark (<see cref="MarkTries"/>, read in <see cref="EncounterMarks.For"/>);</item>
/// <item>the Shiny Charm, two more rolls at a shiny (<see cref="ShinyCharm"/>, plan 13 · V5).</item>
/// </list>
/// </summary>
public static class Charms
{
    public const string FileName = "charms.json";

    /// <summary>The charms' names in <c>items.json</c>.</summary>
    public const string Oval = "Oval Charm", Exp = "Exp. Charm", Catching = "Catching Charm", Mark = "Mark Charm", Shiny = ShinyCharm.ItemName;

    private static readonly Lazy<CharmsFile> file = new(() => GameDataFiles.Load<CharmsFile>(FileName, strict: true));

    /// <summary>What <c>charms.json</c> says.</summary>
    public static CharmsFile Table => file.Value;

    /// <summary>Whether the bag holds a charm.</summary>
    public static bool Held(Inventory bag, string charm) => bag.AllItems.Any(s => s.Quantity > 0 && s.Name == charm);

    /// <summary>How many of the player's regional Pokédexes are seen whole (or have their diploma): the regional charms earned.</summary>
    public static int RegionalDone(Pokedex pokedex) =>
        pokedex.Regional.Count(dex => pokedex.Diplomas.Contains(dex) || pokedex.IsComplete(dex));

    /// <summary>Whether the National Pokédex is the player's and caught whole (or its diploma held).</summary>
    public static bool NationalDone(Pokedex pokedex) =>
        pokedex.NationalUnlocked && (pokedex.Diplomas.Contains(Pokedex.National) || pokedex.IsComplete(Pokedex.National));

    /// <summary>The charms a Pokédex has earned, in the table's order: the regional ones its completions reach, then the National one's.</summary>
    public static IReadOnlyList<string> Earned(Pokedex pokedex)
    {
        var earned = Table.Regional.Take(RegionalDone(pokedex)).ToList();
        if (NationalDone(pokedex) && Table.National.Length > 0) earned.Add(Table.National);
        return earned;
    }

    /// <summary>The charms earned and not yet in the bag, in the order a designer hands them over.</summary>
    public static IReadOnlyList<string> Due(Pokedex pokedex, Inventory bag) =>
        Earned(pokedex).Where(charm => !Held(bag, charm)).ToList();

    /// <summary>
    /// The Oval Charm's chance of an Egg out of a hundred (Black 2 and White 2 on): 40 where the two don't get on, 80 where
    /// they do and 88 where they get on very well, in place of the pair's 20, 50 and 70 (<see cref="Compatibility"/>); none
    /// for a pair that can't breed.
    /// </summary>
    public static int OvalChance(Compatibility compatibility) => compatibility switch
    {
        Compatibility.Low => 40,
        Compatibility.Medium => 80,
        Compatibility.High => 88,
        _ => 0
    };

    /// <summary>The Exp. Charm's share (decision 4): half as much again, the Lucky Egg's, rounded down.</summary>
    public static int ExpShare(int exp) => (int)((long)exp * 150 / 100);

    /// <summary>The tries at each mark with the Mark Charm in the bag: its own and two more, each with a salt of its own.</summary>
    public const int MarkTries = 3;
}
