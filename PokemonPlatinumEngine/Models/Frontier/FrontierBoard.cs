using System.Collections.Generic;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>A row of a facility's board: a way of taking its challenge, the longest streak in it and the latest, and whether the latest still runs.</summary>
public sealed record FrontierBoardRow(string Label, int Record, int Latest, bool Running);

/// <summary>
/// What a facility's records machine shows (plan 06 · R18a; the original's Frontier records application,
/// <c>src/applications/frontier/records</c>, opened by <c>scripts_battle_frontier_records.s</c>): a row for each way
/// its challenge is taken, the Battle Points held and the commemorative print won. Each facility gives its own rows
/// (<see cref="For"/>); the Tower's are here. No drawing or input.
/// </summary>
public sealed record FrontierBoard(FrontierFacility Facility, string Title, IReadOnlyList<FrontierBoardRow> Rows, int BattlePoints, FrontierPrint Print, int Victories)
{
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
        return new FrontierBoard(facility, NameOf(facility), rows, records.BattlePoints, FrontierBrains.PrintOf(story, facility), records.VictoriesAt(facility));
    }
}
