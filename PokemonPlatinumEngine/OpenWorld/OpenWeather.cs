using System;
using System.Collections.Generic;
using System.IO;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// An open region's weather calendar (plan 33 · GL2; <c>docs/mechanics/rulings.md</c>, "The Wild Area"): the areas it
/// keeps a column for, and for each day of a leap year, from the first of January, each area's weather by the
/// <see cref="FieldWeather"/> names. The shape of the grid's <see cref="WorldCalendarFile"/> (plan 01 · M7), read the
/// same way (<see cref="Weathers.CalendarDay"/>), its places an open map's area keys. GPU-free.
/// </summary>
public sealed class OpenCalendar
{
    private readonly Dictionary<string, int> column;
    private readonly FieldWeather[][] days;

    public OpenCalendar(IReadOnlyList<string> places, IReadOnlyList<IReadOnlyList<string>> days)
    {
        if (days.Count != 366) throw new InvalidDataException($"a weather calendar has 366 days, the 29th of February among them, not {days.Count}");
        column = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < places.Count; i++) column[places[i]] = i;
        this.days = new FieldWeather[days.Count][];
        for (int d = 0; d < days.Count; d++)
        {
            if (days[d].Count != places.Count) throw new InvalidDataException($"day {d + 1} of the weather calendar has {days[d].Count} weathers for {places.Count} places");
            this.days[d] = new FieldWeather[places.Count];
            for (int p = 0; p < places.Count; p++) this.days[d][p] = OpenWeather.Named(days[d][p]);
        }
        Places = places;
    }

    /// <summary>The areas it keeps a column for, in its order.</summary>
    public IReadOnlyList<string> Places { get; }

    /// <summary>Whether it keeps a column for an area.</summary>
    public bool Keeps(string area) => column.ContainsKey(area);

    /// <summary>An area's weather on a day, or null where the calendar keeps no column for it.</summary>
    public FieldWeather? On(string area, DateTime date) =>
        column.TryGetValue(area, out int i) ? days[Weathers.CalendarDay(date)][i] : null;

    /// <summary>A region's calendar from its description folder's <c>calendar.json</c>, or null where it has none.</summary>
    public static OpenCalendar? Read(string folder)
    {
        string path = Path.Combine(folder, WorldCalendarFile.FileName);
        if (!File.Exists(path)) return null;
        var file = GameDataFiles.ReadStrict<WorldCalendarFile>(File.ReadAllText(path), $"world3d/{Path.GetFileName(folder)}/{WorldCalendarFile.FileName}");
        return new OpenCalendar(file.Places, file.Days);
    }
}

/// <summary>
/// The weather of an open map's area on a day (plan 33 · GL2): its calendar's where the region keeps one for it, the
/// area's own (<c>world.json</c>'s <c>weather</c>) otherwise, clear where it names none. The one answer the picture, the
/// wild Pokémon, a battle's opening weather and an evolution that waits for rain all ask. GPU-free.
/// </summary>
public static class OpenWeather
{
    /// <summary>A weather by its name: a <see cref="FieldWeather"/>'s own, or a header's of Platinum's (<see cref="Weathers.Of"/>).</summary>
    public static FieldWeather Named(string? name) =>
        string.IsNullOrEmpty(name) ? FieldWeather.Clear
        : Enum.TryParse<FieldWeather>(name, ignoreCase: true, out var weather) && Enum.IsDefined(weather) ? weather : Weathers.Of(name);

    /// <summary>The weather of an area of a map on a day.</summary>
    public static FieldWeather Of(OpenMap map, OpenArea? area, DateTime date)
    {
        if (area == null) return FieldWeather.Clear;
        if (map.Calendar?.On(area.Key, date) is { } day) return day;
        return Named(area.Weather);
    }

    /// <summary>The weather where a place of a map is, on a day.</summary>
    public static FieldWeather At(OpenMap map, OpenPos pos, DateTime date) => Of(map, map.AreaAt(pos), date);

    /// <summary>The weather of the area a zone belongs to (its place), on a day.</summary>
    public static FieldWeather OfZone(OpenMap map, OpenZone zone, DateTime date)
    {
        foreach (var area in map.Areas)
            if (string.Equals(area.Key, zone.Place, StringComparison.OrdinalIgnoreCase)) return Of(map, area, date);
        return FieldWeather.Clear;
    }
}
