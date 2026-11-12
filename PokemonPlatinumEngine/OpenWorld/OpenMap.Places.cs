using System;
using System.Collections.Generic;
using System.Linq;

namespace PokemonPlatinumEngine.OpenWorld;

/// <summary>
/// What a map's places lay on it (plan 32 · OW5): its areas, triggers, doors and encounter zones, beside the markers,
/// solids and water OW1 and OW3 gave it. A region's <c>places/&lt;key&gt;.json</c> are read into these
/// (<see cref="OpenPlaceFiles"/>); a tool or a test may add them by hand.
/// </summary>
public sealed partial class OpenMap
{
    private readonly List<OpenArea> areas = new();
    private readonly List<OpenTrigger> triggers = new();
    private readonly List<OpenDoor> doors = new();
    private readonly List<OpenZone> zones = new();

    /// <summary>The map's areas, in the region's order: where two hold a place the later is its area.</summary>
    public IReadOnlyList<OpenArea> Areas => areas;

    /// <summary>The map's triggers, in the order laid.</summary>
    public IReadOnlyList<OpenTrigger> Triggers => triggers;

    /// <summary>The map's doors into grid rooms, in the order laid.</summary>
    public IReadOnlyList<OpenDoor> Doors => doors;

    /// <summary>The map's encounter zones, in the order laid.</summary>
    public IReadOnlyList<OpenZone> Zones => zones;

    public void Add(OpenArea area)
    {
        if (areas.Any(a => string.Equals(a.Key, area.Key, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"open map {Name} has two areas keyed {area.Key}");
        areas.Add(area);
    }

    /// <summary>Adds a trigger; two of one name are refused, since the crossings know them by name.</summary>
    public void Add(OpenTrigger trigger)
    {
        trigger.Shape.Checked(trigger.Name);
        if (triggers.Any(t => t.Name == trigger.Name)) throw new ArgumentException($"open map {Name} has two triggers named {trigger.Name}");
        triggers.Add(trigger);
    }

    /// <summary>Adds a door; two of one name are refused.</summary>
    public void Add(OpenDoor door)
    {
        door.Shape.Checked(door.Name);
        if (doors.Any(d => d.Name == door.Name)) throw new ArgumentException($"open map {Name} has two doors named {door.Name}");
        doors.Add(door);
    }

    public void Add(OpenZone zone)
    {
        if (zone.Polygon.Count < 3 || zone.MinY >= zone.MaxY) throw new ArgumentException($"the zone {zone.Name} has no inside");
        if (zones.Any(z => z.Name == zone.Name)) throw new ArgumentException($"open map {Name} has two zones named {zone.Name}");
        zones.Add(zone);
    }

    /// <summary>The area a place is in (the last of the map's areas that holds it), or null.</summary>
    public OpenArea? AreaAt(OpenPos pos)
    {
        for (int i = areas.Count - 1; i >= 0; i--)
            if (areas[i].Contains(pos)) return areas[i];
        return null;
    }

    private readonly Dictionary<string, List<string>> sites = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The places some Pokémon evolve at in an area (plan 33 · GL2: the Stone Arch, rulings' Runerigus line), what a
    /// place's <c>evolutionSites</c> names: an open map's <c>Map.EvolutionSitesAt</c>.
    /// </summary>
    public void AddSites(string area, IEnumerable<string> names)
    {
        if (!sites.TryGetValue(area, out var list)) sites[area] = list = new();
        foreach (string name in names) if (!list.Contains(name)) list.Add(name);
    }

    /// <summary>The evolution sites of the area a place is in; none where it names none.</summary>
    public IReadOnlyList<string> SitesAt(OpenPos pos) =>
        AreaAt(pos) is { } area && sites.TryGetValue(area.Key, out var list) ? list : Array.Empty<string>();

    /// <summary>Every area's evolution sites, by the area's key.</summary>
    public IReadOnlyDictionary<string, List<string>> Sites => sites;

    /// <summary>
    /// The region's weather calendar (plan 33 · GL2: Galar's <c>calendar.json</c>, a column to an area), or null where
    /// the region has none and every area keeps the weather it names (<see cref="OpenWeather.Of"/>).
    /// </summary>
    public OpenCalendar? Calendar { get; set; }

    /// <summary>The script file of a place: its area's key, or the map's name where no area holds it.</summary>
    public string ScriptFileAt(OpenPos pos) => AreaAt(pos)?.Key ?? Name;

    /// <summary>
    /// The door that brings someone back out of a grid room by its warp at <paramref name="x"/>, <paramref name="y"/>
    /// (plan 32 · OW5): of the doors that lead into that room, the one whose tile in the room is nearest the warp's
    /// (a room with two doors to one open map is left by each the way it was come in by); null when no door leads in.
    /// </summary>
    public OpenDoor? DoorBackFrom(string room, int x, int y) =>
        doors.Where(d => string.Equals(d.Room, room, StringComparison.OrdinalIgnoreCase))
            .OrderBy(d => (long)(d.RoomX - x) * (d.RoomX - x) + (long)(d.RoomY - y) * (d.RoomY - y))
            .FirstOrDefault();

    /// <summary>The encounter zone a place is in (the first that holds it), or null.</summary>
    public OpenZone? ZoneAt(OpenPos pos) => zones.FirstOrDefault(z => z.Contains(pos));
}
