using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>The four kinds of place the Johto Safari Zone's objects count toward (HeartGold's <c>SAFARI_ZONE_OBJECT_TYPE_*</c>).</summary>
public enum SafariObjectKind { Plains, Forest, Peak, Water }

/// <summary>Which of an area's tables a slot the objects bring is met in.</summary>
public enum SafariWay { Land, Water, OldRod, GoodRod, SuperRod }

/// <summary>
/// A slot the objects bring to an area of the Johto Safari Zone (plan 38 · EX8; HeartGold's <c>bonus_mons</c>): its
/// way, its species in the morning, by day and at night, its level, and what it asks of the area's objects.
/// </summary>
public sealed record SafariSlot(SafariWay Way, string Species, string? Day, string? Night, int Level, IReadOnlyList<(SafariObjectKind Kind, int Count)> Needs)
{
    /// <summary>The species met at a time of day, as the grass's slots give it (the morning's the slot's own).</summary>
    public string SpeciesAt(TimeOfDay time) => time switch
    {
        TimeOfDay.Day or TimeOfDay.Twilight => Day ?? Species,
        TimeOfDay.Night or TimeOfDay.LateNight => Night ?? Species,
        _ => Species
    };
}

/// <summary>
/// One of the objects an Owner may place in an area (plan 38 · EX8; ours, since the pinned pokeheartgold holds no table
/// of them): its key (its name is <c>safariObjects.name.&lt;key&gt;</c>), the tiles it takes and its points of each kind.
/// </summary>
public sealed record SafariObjectType(int Id, string Key, int Width, int Depth, int Plains, int Forest, int Peak, int Water)
{
    public string Name => Text.Of($"safariObjects.name.{Key}");

    public int PointsOf(SafariObjectKind kind) => kind switch
    {
        SafariObjectKind.Plains => Plains,
        SafariObjectKind.Forest => Forest,
        SafariObjectKind.Peak => Peak,
        _ => Water
    };
}

/// <summary>An object standing in an area: its type, its corner on the zone's map, and the day it was set down (<see cref="ApricornBox.DayOf"/>).</summary>
public sealed class PlacedSafariObject
{
    public int Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int Day { get; set; }
}

/// <summary>What the game keeps of the Safari Zone's objects, as a save has it: each area's by its key.</summary>
public sealed class SafariObjectsSave
{
    public Dictionary<string, List<PlacedSafariObject>>? Areas { get; set; }
}

/// <summary>
/// The Johto Safari Zone's objects (plan 38 · EX8; <c>docs/mechanics/rulings.md</c>, "The Johto Safari Zone's
/// objects"). An Owner (<c>VAR_UNK_4057</c> 6 and on, as HeartGold asks) sets objects of ours down in an area at the
/// gate hall's Area Customizer, thirty at most to an area. Each gives points of the kinds it stands for, counted from the
/// day after it is set down and twice once it has stood ten days; a slot the objects bring opens in its area once the
/// area's points meet every count it asks (HeartGold's own counts, <c>safari_enc.json</c>), and the k-th of a way open
/// takes that way's slot k. Nothing here draws from any chance; the day is always the one handed over.
/// </summary>
public sealed class SafariObjects
{
    public const string OwnerVariable = "VAR_UNK_4057";
    public const int OwnerFrom = 6;

    /// <summary>The most objects an area holds (HeartGold's thirty).</summary>
    public const int MostInAnArea = 30;

    /// <summary>How many of each object an area's store has to set down.</summary>
    public const int CopiesOfEach = 3;

    /// <summary>The days an object stands before its points count twice.</summary>
    public const int DaysToDouble = 10;

    /// <summary>The zone's map, and its twelve areas' keys in the Area Customizer's order.</summary>
    public const string ZoneMap = "SafariZone";
    public static readonly string[] AreaKeys = Enumerable.Range(1, 12).Select(n => $"safari_zone_{n:00}").ToArray();

    /// <summary>The tile the gate's warp puts a player down on in the zone, and the ring of tiles kept clear round it.</summary>
    public const int ArrivalX = 79, ArrivalY = 99, ArrivalClear = 2;

    /// <summary>The objects of ours, in the store's order.</summary>
    public static IReadOnlyList<SafariObjectType> Catalogue { get; } = new SafariObjectType[]
    {
        new(1, "flowerPatch", 1, 1, 1, 0, 0, 0),
        new(2, "haystack", 1, 1, 2, 0, 0, 0),
        new(3, "fencePost", 1, 1, 1, 0, 1, 0),
        new(4, "sapling", 1, 1, 0, 1, 0, 0),
        new(5, "oldStump", 1, 1, 0, 2, 0, 0),
        new(6, "mushroomRing", 1, 1, 0, 1, 0, 1),
        new(7, "pebbles", 1, 1, 0, 0, 1, 0),
        new(8, "boulder", 1, 1, 0, 0, 2, 0),
        new(9, "stonePillar", 2, 2, 0, 0, 4, 0),
        new(10, "puddle", 1, 1, 0, 0, 0, 1),
        new(11, "reeds", 1, 1, 1, 0, 0, 1),
        new(12, "pond", 2, 2, 0, 0, 0, 4)
    };

    public static SafariObjectType? TypeOf(int id) => Catalogue.FirstOrDefault(t => t.Id == id);

    public static bool IsOwner(StoryState story) => story.Var(OwnerVariable) >= OwnerFrom;

    private readonly Dictionary<string, List<PlacedSafariObject>> areas = new(StringComparer.Ordinal);

    public bool IsEmpty => areas.Values.All(a => a.Count == 0);

    /// <summary>The objects standing in an area.</summary>
    public IReadOnlyList<PlacedSafariObject> In(string area) => areas.TryGetValue(area, out var list) ? list : Array.Empty<PlacedSafariObject>();

    /// <summary>An area's objects set as given (what the Area Customizer leaves).</summary>
    public void Set(string area, IEnumerable<PlacedSafariObject> objects)
    {
        var list = objects.ToList();
        if (list.Count == 0) areas.Remove(area);
        else areas[area] = list;
    }

    public void Clear() => areas.Clear();

    public void Load(SafariObjectsSave? save)
    {
        areas.Clear();
        foreach (var (area, list) in save?.Areas ?? new())
            if (list.Count > 0) areas[area] = list.Where(o => TypeOf(o.Type) != null).Select(o => new PlacedSafariObject { Type = o.Type, X = o.X, Y = o.Y, Day = o.Day }).ToList();
    }

    /// <summary>The save's part: null while nothing stands anywhere, so an older save is untouched.</summary>
    public SafariObjectsSave? ToSave() => IsEmpty ? null : new SafariObjectsSave
    {
        Areas = areas.Where(a => a.Value.Count > 0).ToDictionary(a => a.Key, a => a.Value.Select(o => new PlacedSafariObject { Type = o.Type, X = o.X, Y = o.Y, Day = o.Day }).ToList())
    };

    // ------------------------------------------------------------------ points and slots

    /// <summary>The points of a kind a list of objects gives on a day: none on the day it is set down, twice after ten days.</summary>
    public static int Points(IEnumerable<PlacedSafariObject> objects, SafariObjectKind kind, int today)
    {
        int sum = 0;
        foreach (var o in objects)
        {
            if (TypeOf(o.Type) is not { } type || o.Day >= today) continue;
            int points = type.PointsOf(kind);
            sum += today - o.Day >= DaysToDouble ? points * 2 : points;
        }
        return sum;
    }

    /// <summary>Whether a slot is open on a day for an area's objects: every count it asks met.</summary>
    public static bool Opens(SafariSlot slot, IReadOnlyList<PlacedSafariObject> objects, int today) =>
        objects.Count > 0 && slot.Needs.Count > 0 && slot.Needs.All(n => Points(objects, n.Kind, today) >= n.Count);

    /// <summary>
    /// A way's table with the open slots laid in (the k-th slot of the way, open or not, is slot k's): each open slot's
    /// species at the time and its level. False, and the table untouched, where none is open.
    /// </summary>
    public static bool Lay(List<WildEncounterEntry> table, IReadOnlyList<SafariSlot> slots, SafariWay way, IReadOnlyList<PlacedSafariObject> objects, int today, TimeOfDay time)
    {
        if (objects.Count == 0 || slots.Count == 0) return false;
        bool changed = false;
        int k = 0;
        foreach (var slot in slots)
        {
            if (slot.Way != way) continue;
            int at = k++;
            if (at >= table.Count || !Opens(slot, objects, today)) continue;
            var entry = table[at];
            entry.SpeciesName = slot.SpeciesAt(time);
            entry.DaySpecies = null;
            entry.NightSpecies = null;
            entry.FormName = null;
            entry.MinLevel = entry.MaxLevel = slot.Level;
            changed = true;
        }
        return changed;
    }

    /// <summary>Whether any slot of a way could be opened in a list (a quick answer before a table is copied).</summary>
    public static bool AnyOpen(IReadOnlyList<SafariSlot> slots, SafariWay way, IReadOnlyList<PlacedSafariObject> objects, int today) =>
        objects.Count > 0 && slots.Any(s => s.Way == way && Opens(s, objects, today));

    // ------------------------------------------------------------------ the Area Customizer

    /// <summary>Whether an object's corner may stand on a tile of an area: every tile it takes open floor of the area.</summary>
    public static bool FloorAt(Map map, (int X, int Y, int Width, int Height) bounds, int x, int y)
    {
        // The area's edge is where the ways between areas run, and the gate's warp puts the player down by the way in
        if (x < bounds.X + 2 || y < bounds.Y + 2 || x >= bounds.X + bounds.Width - 2 || y >= bounds.Y + bounds.Height - 2) return false;
        if (Math.Abs(x - ArrivalX) <= ArrivalClear && Math.Abs(y - ArrivalY) <= ArrivalClear) return false;
        if (!map.InBounds(x, y) || !map.IsWalkable(x, y) || map.GetWarpAt(x, y) != null || map.IsTallGrass(x, y)) return false;
        return true;
    }

    /// <summary>
    /// The area as a room to decorate: its rectangle on the zone's map, its open floor, the objects standing (each a
    /// piece of its store's slot), thirty at most. The store is <see cref="StoreOf"/>'s, built from the same objects.
    /// </summary>
    public static DecorationRoom Room(Map map, (int X, int Y, int Width, int Height) bounds, IReadOnlyList<PlacedSafariObject> objects, IReadOnlyList<StoredDecoration> store)
    {
        bool Fits(int x, int y, int w, int d)
        {
            for (int ty = y; ty < y + d; ty++)
                for (int tx = x; tx < x + w; tx++)
                    if (!FloorAt(map, bounds, tx, ty)) return false;
            return true;
        }
        var pieces = new List<RoomPiece>();
        var used = new HashSet<int>();
        foreach (var o in objects)
        {
            if (TypeOf(o.Type) is not { } type) continue;
            int slot = Enumerable.Range(0, store.Count).First(i => store[i].Kind == o.Type && store[i].Out && !used.Contains(i));
            used.Add(slot);
            pieces.Add(new RoomPiece { Kind = type.Id, Name = type.Name, X = o.X, Y = o.Y, Width = type.Width, Depth = type.Depth, Slot = slot });
        }
        // The cursor starts in the middle of the area, or the nearest tile of floor to it
        int cx = bounds.X + bounds.Width / 2, cy = bounds.Y + bounds.Height / 2;
        var start = Enumerable.Range(bounds.Y, bounds.Height).SelectMany(y => Enumerable.Range(bounds.X, bounds.Width).Select(x => (x, y)))
            .Where(t => FloorAt(map, bounds, t.x, t.y)).OrderBy(t => Math.Abs(t.x - cx) + Math.Abs(t.y - cy)).ThenBy(t => t.y).ThenBy(t => t.x)
            .Select(t => ((int, int)?)t).FirstOrDefault() ?? (cx, cy);
        return new DecorationRoom
        {
            Left = bounds.X,
            Top = bounds.Y,
            Width = bounds.Width,
            Height = bounds.Height,
            Pieces = pieces,
            Limit = MostInAnArea,
            Start = start,
            Allows = (piece, x, y) => Fits(x, y, piece.Width, piece.Depth),
            TileAt = (x, y) => FloorAt(map, bounds, x, y) ? RoomTile.Floor : RoomTile.Rock
        };
    }

    /// <summary>An area's store: <see cref="CopiesOfEach"/> of every object, those standing marked out.</summary>
    public static List<StoredDecoration> StoreOf(IReadOnlyList<PlacedSafariObject> objects)
    {
        var store = new List<StoredDecoration>();
        foreach (var type in Catalogue)
        {
            int standing = objects.Count(o => o.Type == type.Id);
            for (int i = 0; i < Math.Max(CopiesOfEach, standing); i++) store.Add(new StoredDecoration { Kind = type.Id, Out = i < standing });
        }
        return store;
    }

    /// <summary>A kind of object as the decorating knows it.</summary>
    public static DecorationKind KindOf(int id) => TypeOf(id) is { } t ? new DecorationKind(t.Id, t.Name, t.Width, t.Depth) : new DecorationKind(id, "?", 1, 1);

    /// <summary>
    /// What an area holds once the Area Customizer is left: every piece standing, an object that stood before at the
    /// same corner keeping its day, a new one or one moved the day given.
    /// </summary>
    public static List<PlacedSafariObject> Keep(DecorationRoom room, IReadOnlyList<PlacedSafariObject> before, int today) =>
        room.Pieces.Where(p => !p.IsFixture).Select(p => new PlacedSafariObject
        {
            Type = p.Kind,
            X = p.X,
            Y = p.Y,
            Day = before.FirstOrDefault(o => o.Type == p.Kind && o.X == p.X && o.Y == p.Y)?.Day ?? today
        }).ToList();
}
