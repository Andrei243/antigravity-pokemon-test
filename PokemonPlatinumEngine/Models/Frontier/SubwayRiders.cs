using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>A rider of the Battle Subway's table: one of Unova's classes, the look its rider wears, and eight names of ours.</summary>
public sealed class SubwayRiderRecord
{
    public string Class { get; set; } = "";
    public string Look { get; set; } = "";
    public List<string> Names { get; set; } = new();
}

/// <summary>The Battle Subway's own table (<c>Data/subway.json</c>, written by hand): its riders, its staff and its prizes.</summary>
public sealed class SubwayData
{
    public List<SubwayRiderRecord> Riders { get; set; } = new();

    /// <summary>The rider whose class the Multi lines' staff partner wears (a Depot Agent).</summary>
    public int Staff { get; set; }

    public List<FrontierPrizeRecord> Prizes { get; set; } = new();
}

/// <summary>
/// The Battle Subway's own trainers, prints and Battle Point counter (plan 22 · U11's leftovers; rulings, "The Battle
/// Subway's riders, prints and counter"). GPU-free.
/// <para>
/// <b>Riders</b>: the Frontier's trainers' teams (<see cref="FrontierTrainers"/>, drawn by the Tower's rooms as before, so
/// no draw moves) ride the cars dressed as riders of Unova's classes with names of ours (<see cref="Dress"/>): the rider by
/// the Frontier trainer's number modulo the riders, the name by the number divided by the riders, modulo eight. The two of a
/// Multi pair never wear the same name.
/// </para>
/// <para>
/// <b>Prints</b>: the Frontier's frame (<see cref="FrontierBrains"/>'s states 0 to 4) on a variable of the Subway's own,
/// <see cref="PrintVariable"/>: Ingo beaten in the 21st car of any line waits as the silver print, Emmet beaten in any 49th car
/// (a Super train's Boss counts as the gold) as the gold, and the desks hand them over. The Subway is no facility of the
/// Frontier's, so its print counts toward none of the Frontier's own (the all-gold flag, Lilycove's passes).
/// </para>
/// <para>
/// <b>The counter</b>: a booth of the Gear Station that trades the Frontier's Battle Points (<see cref="FrontierRecords"/>)
/// for prizes of ours (<see cref="Prizes"/>), as the Exchange Service Corner does.
/// </para>
/// </summary>
public static class SubwayRiders
{
    public const string FileName = "subway.json";

    private static readonly SubwayData Data = GameDataFiles.Load<SubwayData>(FileName);

    public static IReadOnlyList<SubwayRiderRecord> Riders => Data.Riders;

    /// <summary>The counter's prizes, in the order the booth lists them.</summary>
    public static IReadOnlyList<FrontierPrizeRecord> Prizes => Data.Prizes;

    /// <summary>What a prize costs in Battle Points; 0 for what the counter doesn't have.</summary>
    public static int PriceOf(string item) => Data.Prizes.FirstOrDefault(p => p.Item == item)?.Price ?? 0;

    /// <summary>The names a rider has.</summary>
    public const int NamesEach = 8;

    /// <summary>
    /// Who a Frontier trainer of that number rides the Subway as: the rider of the number modulo the riders, its name the
    /// number divided by the riders, modulo eight, or the next of its names where that one is <paramref name="avoid"/>.
    /// </summary>
    public static FrontierDress DressOf(int number, string? avoid = null)
    {
        number = Math.Max(0, number);
        var rider = Data.Riders[number % Data.Riders.Count];
        int at = number / Data.Riders.Count % rider.Names.Count;
        string name = rider.Names[at];
        if (name == avoid) name = rider.Names[(at + 1) % rider.Names.Count];
        return new FrontierDress(IdOf(rider, name), rider.Class, name, rider.Look);
    }

    /// <summary>The staff partner of the Multi lines for a Frontier trainer of that number: always a Depot Agent.</summary>
    public static FrontierDress StaffOf(int number)
    {
        var rider = Data.Riders[Math.Clamp(Data.Staff, 0, Data.Riders.Count - 1)];
        string name = rider.Names[Math.Max(0, number) % rider.Names.Count];
        return new FrontierDress(IdOf(rider, name), rider.Class, name, rider.Look);
    }

    private static string IdOf(SubwayRiderRecord rider, string name) =>
        "subway_rider_" + Trainer.KeyOf(rider.Class).ToLowerInvariant() + "_" + name.ToLowerInvariant();

    /// <summary>The trainers of a car dressed as the Subway's own riders; the second of a pair kept from the first's name.</summary>
    public static List<FrontierOpponent> Dress(List<FrontierOpponent> opponents)
    {
        var dressed = new List<FrontierOpponent>();
        foreach (var op in opponents)
        {
            string? avoid = dressed.Count > 0 && DressOf(op.Number).Class == dressed[0].ShownClass ? dressed[0].ShownName : null;
            dressed.Add(op.DressedAs(DressOf(op.Number, avoid)));
        }
        return dressed;
    }

    // ------------------------------------------------------------------ prints

    /// <summary>The variable the Subway's print is kept in, 0 to 4 as the Frontier's are.</summary>
    public const string PrintVariable = "VAR_UNOVA_SUBWAY_PRINT_STATE";

    /// <summary>
    /// A Subway Boss beaten (<paramref name="which"/> 1 Ingo, 2 Emmet, as <see cref="BattleSubway.BossAt(int, SubwayTrain)"/>
    /// says): the silver print waits after Ingo's first defeat, the gold after Emmet's once the silver is in hand. Returns the
    /// print that now waits (none where nothing changed).
    /// </summary>
    public static FrontierPrint Beaten(StoryState story, int which)
    {
        int state = story.Var(PrintVariable);
        if (which == 1 && state == 0) { story.SetVar(PrintVariable, 1); return FrontierPrint.Silver; }
        if (which == 2 && state == 2) { story.SetVar(PrintVariable, 3); return FrontierPrint.Gold; }
        // Emmet beaten before the silver is in hand (a Super Double Train never meets Ingo): the silver waits first
        if (which == 2 && state == 0) { story.SetVar(PrintVariable, 1); return FrontierPrint.Silver; }
        return FrontierPrint.None;
    }

    /// <summary>A desk hands over the print that waits (1 becomes 2, 3 becomes 4): which one, or none.</summary>
    public static FrontierPrint HandOver(StoryState story)
    {
        switch (story.Var(PrintVariable))
        {
            case 1: story.SetVar(PrintVariable, 2); return FrontierPrint.Silver;
            case 3: story.SetVar(PrintVariable, 4); return FrontierPrint.Gold;
            default: return FrontierPrint.None;
        }
    }

    /// <summary>The print the player has been handed for the Subway.</summary>
    public static FrontierPrint PrintOf(StoryState story) =>
        story.Var(PrintVariable) switch { >= 4 => FrontierPrint.Gold, >= 2 => FrontierPrint.Silver, _ => FrontierPrint.None };
}
