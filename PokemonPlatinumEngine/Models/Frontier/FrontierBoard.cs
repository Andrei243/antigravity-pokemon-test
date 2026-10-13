using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models.Frontier;

/// <summary>A row of a facility's board: a way of taking its challenge, the longest streak in it and the latest, and whether the latest still runs.</summary>
/// <param name="RecordCount">A second number a facility keeps with its record streak (the Battle Factory's trades), and with its latest; null where it keeps none.</param>
public sealed record FrontierBoardRow(string Label, int Record, int Latest, bool Running, int? RecordCount = null, int? LatestCount = null);

/// <summary>A species' own records at the Battle Hall: its longest streak in a Single challenge and in a Double one.</summary>
public sealed record FrontierSpeciesRow(string Species, int Single, int Double);

/// <summary>
/// What a facility's records machine shows (plan 06 · R18a; the original's Frontier records application,
/// <c>src/applications/frontier/records</c>, opened by <c>scripts_battle_frontier_records.s</c>): a row for each way
/// its challenge is taken, the Battle Points held and the commemorative print won. Each facility gives its own rows
/// (<see cref="For"/>). No drawing or input.
/// </summary>
public sealed record FrontierBoard(FrontierFacility Facility, string Title, IReadOnlyList<FrontierBoardRow> Rows, int BattlePoints, FrontierPrint Print, int Victories)
{
    /// <summary>What the rows' second numbers count ("TRADES"); null when the rows have none.</summary>
    public string? CountLabel { get; init; }

    /// <summary>The species with records of their own, the longest Single streak first (the Battle Hall's); empty for every other facility.</summary>
    public IReadOnlyList<FrontierSpeciesRow> Species { get; init; } = System.Array.Empty<FrontierSpeciesRow>();

    public static string NameOf(FrontierFacility facility) => facility switch
    {
        FrontierFacility.Tower => Text.Of("records.facility.tower"),
        FrontierFacility.Factory or FrontierFacility.FactoryOpen => Text.Of("records.facility.factory"),
        FrontierFacility.Castle => Text.Of("records.facility.castle"),
        FrontierFacility.Hall => Text.Of("records.facility.hall"),
        _ => Text.Of("records.facility.arcade")
    };

    /// <summary>The board of a facility as the records stand: every one of the five has rows of its own.</summary>
    public static FrontierBoard For(FrontierFacility facility, FrontierRecords records, StoryState story)
    {
        var rows = new List<FrontierBoardRow>();
        if (facility == FrontierFacility.Tower)
        {
            foreach (var (mode, label) in new[] { (TowerMode.Single, Text.Of("records.row.single")), (TowerMode.Double, Text.Of("records.row.double")), (TowerMode.Multi, Text.Of("records.row.multi")) })
                rows.Add(new FrontierBoardRow(label, records.Get(BattleTowerChallenge.RecordStat(mode)), records.Get(BattleTowerChallenge.LatestStat(mode)),
                    records.IsRunning(FrontierFacility.Tower, (int)mode)));
        }
        string? counted = null;
        if (facility is FrontierFacility.Factory or FrontierFacility.FactoryOpen)
        {
            // The Battle Factory: its two levels, each taken alone with one Pokémon out or with two, and the trades of each streak
            counted = Text.Of("records.counted.trades");
            foreach (var (open, way, label) in new[]
            {
                (false, FrontierChallenge.Single, Text.Of("records.row.lv50Single")), (false, FrontierChallenge.Double, Text.Of("records.row.lv50Double")),
                (true, FrontierChallenge.Single, Text.Of("records.row.openSingle")), (true, FrontierChallenge.Double, Text.Of("records.row.openDouble"))
            })
                rows.Add(new FrontierBoardRow(label,
                    records.Get(BattleFactoryChallenge.RecordStreakStat(way, open)), records.Get(BattleFactoryChallenge.LatestStreakStat(way, open)),
                    records.IsRunning(BattleFactoryChallenge.FacilityOf(open), (int)way),
                    records.Get(BattleFactoryChallenge.RecordTradesStat(way, open)), records.Get(BattleFactoryChallenge.LatestTradesStat(way, open))));
            facility = FrontierFacility.Factory;
        }
        var species = new List<FrontierSpeciesRow>();
        if (facility == FrontierFacility.Hall)
        {
            // The Battle Hall (plan 06 · R18c): a streak is a species' own, so a row shows the best any species has
            // done, and the species follow, each with its two records
            foreach (var (way, label) in new[] { (FrontierChallenge.Single, Text.Of("records.row.single")), (FrontierChallenge.Double, Text.Of("records.row.double")) })
            {
                var kept = records.Hall.Of(way);
                rows.Add(new FrontierBoardRow(label, kept.Count == 0 ? 0 : kept.Values.Max(), records.Get(BattleHallChallenge.LatestStreakStat(way)),
                    records.IsRunning(FrontierFacility.Hall, (int)way)));
            }
            foreach (string name in records.Hall.Singles.Keys.Union(records.Hall.Doubles.Keys))
            {
                int single = records.Hall.RecordOf(FrontierChallenge.Single, name), twice = records.Hall.RecordOf(FrontierChallenge.Double, name);
                if (single > 0 || twice > 0) species.Add(new FrontierSpeciesRow(name, single, twice));
            }
            species = species.OrderByDescending(r => r.Single).ThenByDescending(r => r.Double).ThenBy(r => r.Species, System.StringComparer.Ordinal).ToList();
        }
        if (facility == FrontierFacility.Castle)
        {
            // The Battle Castle (plan 06 · R18c): beside each streak, the Castle Points it was made with
            counted = Text.Of("records.counted.points");
            foreach (var (way, label) in new[] { (FrontierChallenge.Single, Text.Of("records.row.single")), (FrontierChallenge.Double, Text.Of("records.row.double")) })
                rows.Add(new FrontierBoardRow(label, records.Get(BattleCastleChallenge.RecordStreakStat(way)), records.Get(BattleCastleChallenge.LatestStreakStat(way)),
                    records.IsRunning(FrontierFacility.Castle, (int)way),
                    records.Get(BattleCastleChallenge.RecordPointsStat(way)), records.Get(BattleCastleChallenge.LatestPointsStat(way))));
        }
        if (facility == FrontierFacility.Arcade)
        {
            // The Battle Arcade (plan 06 · R18d): a streak and nothing beside it
            foreach (var (way, label) in new[] { (FrontierChallenge.Single, Text.Of("records.row.single")), (FrontierChallenge.Double, Text.Of("records.row.double")) })
                rows.Add(new FrontierBoardRow(label, records.Get(BattleArcadeChallenge.RecordStreakStat(way)), records.Get(BattleArcadeChallenge.LatestStreakStat(way)),
                    records.IsRunning(FrontierFacility.Arcade, (int)way)));
        }
        return new FrontierBoard(facility, NameOf(facility), rows, records.BattlePoints, FrontierBrains.PrintOf(story, facility), records.VictoriesAt(facility)) { CountLabel = counted, Species = species };
    }
}
