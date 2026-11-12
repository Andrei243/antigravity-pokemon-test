using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>One of the day's outbreaks as the report tells it: the outbreak and how many of its species were met there today.</summary>
public sealed record ReportedOutbreak(Outbreak Outbreak, int Met);

/// <summary>
/// What a region has today beyond its tables (plan 38 · EX2; style guide, "The Town Map", "The day's report"): its outbreaks
/// with their counts, the visitor a scan called up and its time left, whether a scan may be made now, and (EX5) the hour's
/// rift with the hour it closes at. The Town Map turns to it on the item button. Plan 41's board and plan 43's phone read it
/// too. GPU-free; no input or drawing.
/// </summary>
public sealed record FieldReport(string Region, IReadOnlyList<ReportedOutbreak> Outbreaks, VisitorCall? Visitor, ScanState Scan)
{
    /// <summary>The hour's rift (plan 38 · EX5); null where the hour has none.</summary>
    public Rift? Rift { get; init; }

    /// <summary>The hour of the clock the rift closes at.</summary>
    public int RiftUntil { get; init; }

    /// <summary>
    /// The report of a region now; null before its Hall of Fame is entered, when it has nothing to tell.
    /// </summary>
    /// <param name="region">The region whose map is open.</param>
    /// <param name="story">The story, for the Hall of Fame.</param>
    /// <param name="encounters">What the game remembers of its wild Pokémon: the day's number, the counts, the visitor.</param>
    /// <param name="nationalDex">Whether the player has the National Pokédex, which a scan wants.</param>
    /// <param name="today">The date, whose day of the week picks the visitor's row.</param>
    /// <param name="swarmArea">The day's swarm's place in the region, which keeps outbreaks off it; null for none.</param>
    public static FieldReport? Of(Region? region, Story.StoryState story, SpecialEncounters encounters, bool nationalDex, DateTime today, string? swarmArea = null)
    {
        if (region == null || !Overworld.Outbreaks.Open(region, story)) return null;
        var day = Overworld.Outbreaks.Today(region.Id, encounters.DailyNumber, swarmArea);
        var told = day.Select(o => new ReportedOutbreak(o, encounters.OutbreakMetIn(o.Area))).ToList();
        var visitor = encounters.Visitor is { } v && string.Equals(v.Region, region.Id, StringComparison.OrdinalIgnoreCase) ? v : null;
        var scan = Visitors.Check(region, story, encounters, nationalDex, today.DayOfWeek, day);
        return new FieldReport(region.Id, told, visitor, scan);
    }

    /// <summary>The report with the hour's rift on it (plan 38 · EX5): it closes as the hour after <paramref name="hour"/> begins.</summary>
    public FieldReport WithRift(Rift? rift, int hour) => this with { Rift = rift, RiftUntil = (hour + 1) % 24 };

    /// <summary>A place of a region by the name its area file gives it, as the language shows it; the key where no file has it.</summary>
    public static string PlaceName(string region, string area)
    {
        foreach (var world in World.LoadAll())
            if (string.Equals(world.Index.Region, region, StringComparison.OrdinalIgnoreCase) && world.Area(area) is { } file)
                return LocalNames.PlaceShown(null, file.Name);
        return area;
    }
}
