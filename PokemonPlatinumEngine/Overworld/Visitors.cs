using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>Why a scan may not be made now, or that it may (plan 38 · EX2).</summary>
public enum ScanState
{
    /// <summary>A scan may be made: today's visitor has a place.</summary>
    Ready,

    /// <summary>The region's Hall of Fame isn't entered.</summary>
    NotOpen,

    /// <summary>The player hasn't the National Pokédex.</summary>
    NoNationalDex,

    /// <summary>A scan was made today already.</summary>
    Scanned,

    /// <summary>Nothing calls today: the region has no row for the day, or every place of the day's row has an outbreak.</summary>
    Nothing
}

/// <summary>
/// The day's visitor (plan 38 · EX2; <c>docs/mechanics/rulings.md</c>, "The field beyond the tables"): each region has a
/// row of its own for each day of the week (<c>visitors.json</c>, ours), a species of another region's Pokédex at a place
/// of this one. Once a day, after the region's Hall of Fame and with the National Pokédex, a scan from the Town Map's
/// report calls the day's row up for <see cref="Length"/> seconds of the game's own time; it is met in its place's tall
/// grass in slots <see cref="FirstSlot"/> to <see cref="LastSlot"/>, after the day's outbreak and never where one is (a row
/// names places in order and the first with no outbreak that day is its own). Nothing is drawn: the row is the weekday's.
/// GPU-free; no input or drawing.
/// </summary>
public static class Visitors
{
    /// <summary>How long a visitor stays: an hour of the game's own time.</summary>
    public const float Length = 3600f;

    /// <summary>The grass's slots a visitor takes (weights 10, 10, 10, 10: forty meetings in a hundred).</summary>
    public const int FirstSlot = 2, LastSlot = 5;

    /// <summary>How many meetings in a hundred are the visitor's in its place.</summary>
    public static int Share => WorldAreaFile.LandSlotWeights.Skip(FirstSlot).Take(LastSlot - FirstSlot + 1).Sum();

    // What was read, by region: the rows, and the places' names (World.LoadAll reads the files afresh each time)
    private static readonly Dictionary<string, IReadOnlyList<VisitorRow>> rows = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<(string Region, string Area), string?> names = new();
    private static readonly object gate = new();

    /// <summary>Forgets what was read, for a tool or a test that has laid other data layers.</summary>
    public static void Forget()
    {
        lock (gate) { rows.Clear(); names.Clear(); }
    }

    /// <summary>A region's rows, as its <c>visitors.json</c> has them (none for a region with no file); read once.</summary>
    public static IReadOnlyList<VisitorRow> RowsOf(string region)
    {
        lock (gate)
        {
            if (rows.TryGetValue(region, out var held)) return held;
            return rows[region] = World.LoadAll().Where(w => string.Equals(w.Index.Region, region, StringComparison.OrdinalIgnoreCase))
                .SelectMany(w => w.Visitors.Rows).ToList();
        }
    }

    /// <summary>The English name a region's area file gives a place, read once; null where no file has it.</summary>
    internal static string? AreaName(string region, string area)
    {
        lock (gate)
        {
            if (names.TryGetValue((region, area), out var held)) return held;
            string? name = null;
            foreach (var world in World.LoadAll())
                if (string.Equals(world.Index.Region, region, StringComparison.OrdinalIgnoreCase) && world.Area(area) is { } file)
                    name = file.Name;
            return names[(region, area)] = name;
        }
    }

    /// <summary>The row of a day of the week in a region; null where it has none.</summary>
    public static VisitorRow? RowFor(string region, DayOfWeek day) =>
        RowsOf(region).FirstOrDefault(r => string.Equals(r.Day, day.ToString(), StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The place a row is met in on a day: the first of its places with no outbreak that day (<paramref name="outbreaks"/>);
    /// null where every one has one.
    /// </summary>
    public static string? PlaceOf(VisitorRow row, IReadOnlyList<Outbreak>? outbreaks) =>
        row.Places.FirstOrDefault(p => Outbreaks.At(outbreaks, p) == null);

    /// <summary>
    /// Whether a scan may be made now, and why not: the region's Hall of Fame, the National Pokédex, one a day (by the day's
    /// number), and a row for the weekday whose place is free of the day's outbreaks.
    /// </summary>
    public static ScanState Check(Region? region, Story.StoryState story, SpecialEncounters encounters, bool nationalDex, DayOfWeek day, IReadOnlyList<Outbreak>? outbreaks)
    {
        if (!Outbreaks.Open(region, story)) return ScanState.NotOpen;
        if (!nationalDex) return ScanState.NoNationalDex;
        if (encounters.ScannedOn == encounters.DailyNumber) return ScanState.Scanned;
        if (RowFor(region!.Id, day) is not { } row || PlaceOf(row, outbreaks) == null) return ScanState.Nothing;
        return ScanState.Ready;
    }

    /// <summary>
    /// Makes the scan where <see cref="Check"/> allows it: the day's row is called up for <see cref="Length"/> seconds and
    /// the scan is spent for the day. Returns the visitor, or null where none could be called.
    /// </summary>
    public static VisitorCall? Scan(Region? region, Story.StoryState story, SpecialEncounters encounters, bool nationalDex, DayOfWeek day, IReadOnlyList<Outbreak>? outbreaks)
    {
        if (Check(region, story, encounters, nationalDex, day, outbreaks) != ScanState.Ready) return null;
        var row = RowFor(region!.Id, day)!;
        encounters.ScannedOn = encounters.DailyNumber;
        return encounters.Visitor = new VisitorCall
        {
            Region = region.Id,
            Species = row.Species,
            Area = PlaceOf(row, outbreaks)!,
            MinLevel = row.MinLevel,
            MaxLevel = Math.Max(row.MinLevel, row.MaxLevel),
            SecondsLeft = Length
        };
    }

    /// <summary>
    /// The game's own time goes by while a visitor is out: its seconds run down, and once they are gone it leaves. Returns
    /// the visitor that has just left, once.
    /// </summary>
    public static VisitorCall? Tick(SpecialEncounters encounters, float dt)
    {
        if (encounters.Visitor is not { } visitor || dt <= 0f) return null;
        visitor.SecondsLeft -= dt;
        if (visitor.SecondsLeft > 0f) return null;
        encounters.Visitor = null;
        return visitor;
    }

    /// <summary>The visitor met in a place's grass now: the one out, in its own region and place; null elsewhere.</summary>
    public static VisitorCall? At(VisitorCall? visitor, string? area) =>
        visitor != null && area != null && visitor.Area == area ? visitor : null;

    /// <summary>Lays a visitor over a place's twelve slots: its species and levels in <see cref="FirstSlot"/> to <see cref="LastSlot"/>, each keeping its weight.</summary>
    public static void Lay(List<WildEncounterEntry> slots, VisitorCall visitor)
    {
        if (slots.Count != 12) return;
        for (int i = FirstSlot; i <= LastSlot; i++)
        {
            var slot = slots[i];
            slot.SpeciesName = visitor.Species;
            slot.Outbreak = false;
            slot.Moves = null;
            if (visitor.MinLevel > 0)
            {
                slot.MinLevel = visitor.MinLevel;
                slot.MaxLevel = visitor.MaxLevel;
            }
        }
    }
}
