using System.Collections.Generic;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>A row of a facility's board: a way of taking its challenge, the longest streak in it and the latest, and whether the latest still runs.</summary>
/// <param name="RecordCount">A second number a facility keeps with its record streak (the Battle Factory's trades), and with its latest; null where it keeps none.</param>
public sealed record FrontierBoardRow(string Label, int Record, int Latest, bool Running, int? RecordCount = null, int? LatestCount = null);

/// <summary>
/// What a facility's records machine shows (plan 06 · R18a; the original's Frontier records application,
/// <c>src/applications/frontier/records</c>, opened by <c>scripts_battle_frontier_records.s</c>): a row for each way
/// its challenge is taken, the Battle Points held and the commemorative print won. Each facility gives its own rows
/// (<see cref="For"/>); the Tower's are here. No drawing or input.
/// </summary>
public sealed record FrontierBoard(FrontierFacility Facility, string Title, IReadOnlyList<FrontierBoardRow> Rows, int BattlePoints, FrontierPrint Print, int Victories)
{
    /// <summary>What the rows' second numbers count ("TRADES"); null when the rows have none.</summary>
    public string? CountLabel { get; init; }

    public static string NameOf(FrontierFacility facility) => facility switch
    {
        FrontierFacility.Tower => "Battle Tower",
        FrontierFacility.Factory or FrontierFacility.FactoryOpen => "Battle Factory",
        FrontierFacility.Castle => "Battle Castle",
        FrontierFacility.Hall => "Battle Hall",
        _ => "Battle Arcade"
    };

    /// <summary>The board of a facility as the records stand. A facility not built yet has a board with no rows.</summary>
    public static FrontierBoard For(FrontierFacility facility, FrontierRecords records, StoryState story)
    {
        var rows = new List<FrontierBoardRow>();
        if (facility == FrontierFacility.Tower)
        {
            foreach (var (mode, label) in new[] { (TowerMode.Single, "Single Battle"), (TowerMode.Double, "Double Battle"), (TowerMode.Multi, "Multi Battle") })
                rows.Add(new FrontierBoardRow(label, records.Get(BattleTowerChallenge.RecordStat(mode)), records.Get(BattleTowerChallenge.LatestStat(mode)),
                    records.IsRunning(FrontierFacility.Tower, (int)mode)));
        }
        string? counted = null;
        if (facility is FrontierFacility.Factory or FrontierFacility.FactoryOpen)
        {
            // The Battle Factory: its two levels, each taken alone with one Pokémon out or with two, and the trades of each streak
            counted = "TRADES";
            foreach (bool open in new[] { false, true })
                foreach (var (way, label) in new[] { (FrontierChallenge.Single, "Single"), (FrontierChallenge.Double, "Double") })
                    rows.Add(new FrontierBoardRow((open ? "Open Level " : "Lv. 50 ") + label,
                        records.Get(BattleFactoryChallenge.RecordStreakStat(way, open)), records.Get(BattleFactoryChallenge.LatestStreakStat(way, open)),
                        records.IsRunning(BattleFactoryChallenge.FacilityOf(open), (int)way),
                        records.Get(BattleFactoryChallenge.RecordTradesStat(way, open)), records.Get(BattleFactoryChallenge.LatestTradesStat(way, open))));
            facility = FrontierFacility.Factory;
        }
        return new FrontierBoard(facility, NameOf(facility), rows, records.BattlePoints, FrontierBrains.PrintOf(story, facility), records.VictoriesAt(facility)) { CountLabel = counted };
    }
}
