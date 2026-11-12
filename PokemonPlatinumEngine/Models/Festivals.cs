using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// <c>Data/festivals.json</c> (plan 40 · GE9; ours; <c>docs/data-files.md</c>, "Festivals"): the days of the year the towns
/// keep a festival on, and the three great days of the year (New Year, Easter, Christmas) with a quiet fourth (St
/// Valentine's Day) that every region keeps. Read strictly.
/// </summary>
public sealed class FestivalsFile
{
    [DataRequired] public List<Festival> Festivals { get; set; } = new();
}

/// <summary>
/// One festival: its id (<c>lanterns</c>), the region whose town keeps it (none for the days every region keeps), the
/// town (an area's key, for the overlays and the shots), and its days: a window from one day of the year to another,
/// both kept, which may cross the year's end (<c>12-18</c> to <c>01-02</c>), or, for Easter, days counted from Easter
/// Sunday as the Gregorian calendar reckons it (<see cref="Festivals.EasterSunday"/>).
/// </summary>
public sealed class Festival
{
    [DataRequired] public string Id { get; set; } = "";

    /// <summary>The region whose town keeps it; null for a day every region keeps (New Year, Easter, Christmas, St Valentine's).</summary>
    public string? Region { get; set; }

    /// <summary>The town that keeps it, by its area's key (or map's name): where its things stand and its stall keeper.</summary>
    public string? Town { get; set; }

    /// <summary>Its first day, <c>MM-dd</c>; with <see cref="Easter"/> unused.</summary>
    public string? From { get; set; }

    /// <summary>Its last day, <c>MM-dd</c>, the same as <see cref="From"/> for one day; earlier in the year for a window over its end.</summary>
    public string? To { get; set; }

    /// <summary>Easter's days as two counts from Easter Sunday (<c>[-2, 1]</c>: Good Friday to Easter Monday).</summary>
    public List<int>? Easter { get; set; }

    /// <summary>Whether its things light up after dark (the summer night's bursts over the sea, the lanterns).</summary>
    public bool Night { get; set; }

    /// <summary>The stall's counter in <c>marts.json</c> (<c>festival_lanterns</c>); none for a festival with no stall.</summary>
    public string? Stall { get; set; }
}

/// <summary>
/// Which festivals the calendar keeps today (plan 40 · GE9; <c>docs/mechanics/rulings.md</c>, "Festivals on the calendar"),
/// GPU-free, from the date alone: no draw, so a day is the same however often it is asked. The engine keeps the story's
/// <c>FLAG_FESTIVAL_&lt;ID&gt;</c> of each as the day turns (<see cref="Keep"/>), which the overlays' things and stall keepers
/// are shown by and a gift's terms ask.
/// </summary>
public static class Festivals
{
    public const string FileName = "festivals.json";

    private static readonly Lazy<FestivalsFile> file = new(() => GameDataFiles.Load<FestivalsFile>(FileName, strict: true));

    /// <summary>Every festival of <c>festivals.json</c>, in the file's order.</summary>
    public static IReadOnlyList<Festival> All => file.Value.Festivals;

    public static Festival? Get(string id) => All.FirstOrDefault(f => f.Id == id);

    /// <summary>The story's flag a festival's days set: <c>FLAG_FESTIVAL_LANTERNS</c>.</summary>
    public static string FlagOf(string id) => "FLAG_FESTIVAL_" + id.ToUpperInvariant().Replace('-', '_');

    /// <summary>Whether a festival keeps a day.</summary>
    public static bool Keeps(Festival festival, DateTime day)
    {
        day = day.Date;
        if (festival.Easter is { Count: 2 } easter)
        {
            var sunday = EasterSunday(day.Year);
            return day >= sunday.AddDays(easter[0]) && day <= sunday.AddDays(easter[1]);
        }
        if (festival.From is not { } from) return false;
        var (fm, fd) = MonthDay(from);
        var (tm, td) = MonthDay(festival.To ?? from);
        int today = day.Month * 100 + day.Day, first = fm * 100 + fd, last = tm * 100 + td;
        // A window over the year's end keeps the days from its first to the last of December and from January to its last
        return first <= last ? today >= first && today <= last : today >= first || today <= last;
    }

    /// <summary>The festivals a day keeps, in the file's order.</summary>
    public static IReadOnlyList<Festival> On(DateTime day) => All.Where(f => Keeps(f, day)).ToList();

    /// <summary>
    /// Sets each festival's flag the day keeps and clears every other's; true where a flag changed. Run by the engine as the
    /// day turns, as a game begins and as one loads (a save made on a festival's last day wakes the next day without it).
    /// </summary>
    public static bool Keep(StoryState story, DateTime day)
    {
        bool changed = false;
        foreach (var festival in All)
        {
            string flag = FlagOf(festival.Id);
            bool on = Keeps(festival, day);
            if (story.Has(flag) == on) continue;
            if (on) story.Set(flag);
            else story.Unset(flag);
            changed = true;
        }
        return changed;
    }

    /// <summary>Easter Sunday of a year by the Gregorian calendar (the anonymous algorithm of 1876, Meeus's form).</summary>
    public static DateTime EasterSunday(int year)
    {
        int a = year % 19, b = year / 100, c = year % 100, d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30, i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7;
        int m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31, dayOfMonth = (h + l - 7 * m + 114) % 31 + 1;
        return new DateTime(year, month, dayOfMonth);
    }

    /// <summary>A day of the year written <c>MM-dd</c>.</summary>
    public static (int Month, int Day) MonthDay(string text)
    {
        if (!DateTime.TryParseExact("2000-" + text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            throw new FormatException($"'{text}' is no day of the year (MM-dd)");
        return (d.Month, d.Day);
    }
}
