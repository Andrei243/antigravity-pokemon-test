using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.OpenWorld;

// What stands in an open map's place (plan 32 · OW5, "The world described, and baked"; docs/data-files.md, "Open
// worlds"): Data/world3d/<region>/places/<key>.json, the overlay's role at metre scale, one to an area of world.json.
// Read strictly (GameDataFiles.Strict), then held to what JSON can't say; a finding names the file, the JSON path and
// the key, and stops the load. Every length is in metres, as the region's other descriptions; x east, z south, y up.

/// <summary><c>places/&lt;key&gt;.json</c>: what stands in an area of an open map. Its key is the file's name.</summary>
public sealed class OpenPlaceFile
{
    /// <summary>Named places a script, a door and the harness name: unique in the map, across its places.</summary>
    public List<OpenMarkerFile> Markers { get; set; } = new();

    /// <summary>Volumes that run a script as the capsule's centre crosses into them.</summary>
    public List<OpenTriggerFile> Triggers { get; set; } = new();

    /// <summary>Volumes that go into a grid room, and the marker the room's way out lands on.</summary>
    public List<OpenDoorFile> Doors { get; set; } = new();

    /// <summary>Encounter zones: polygons between heights with a table (OW7 spawns from them).</summary>
    public List<OpenZoneFile> Zones { get; set; } = new();

    /// <summary>Water laid by hand: a rectangle on the ground and its surface (a fountain's basin, a pool).</summary>
    public List<OpenWaterFile> Water { get; set; } = new();

    /// <summary>Solid boxes the capsule collides with (a building's block, a prop's), each drawn as a building where it says how it is built (OW9).</summary>
    public List<OpenSolidFile> Solids { get; set; } = new();

    /// <summary>People and trainers (plan 32 · OW6): unique in the map by name, and never named as a marker is.</summary>
    public List<OpenPersonFile> People { get; set; } = new();

    /// <summary>Trees standing one by one (plan 32 · OW9): scenery, which the capsule walks through.</summary>
    public List<OpenTreeFile> Trees { get; set; } = new();

    /// <summary>Groves (plan 32 · OW9): polygons filled with trees on a jittered grid.</summary>
    public List<OpenGroveFile> Groves { get; set; } = new();

    /// <summary>
    /// The places some Pokémon evolve at in this area (plan 33 · GL2: <c>Stone Arch</c>, the Runerigus line's), as a
    /// grid overlay's <c>evolutionSites</c>: a level-up anywhere in the area counts as at them.
    /// </summary>
    public List<string> EvolutionSites { get; set; } = new();
}

/// <summary>A tree: its foot ([x, z] on the highest ground there, or [x, y, z] on the ground nearest y), its kind and its size.</summary>
public sealed class OpenTreeFile
{
    [DataRequired] public double[] At { get; set; } = Array.Empty<double>();
    public OpenTreeKind Kind { get; set; } = OpenTreeKind.Pine;
    public double Size { get; set; } = 1;

    /// <summary>Whether its trunk is solid, walked round and never through (<see cref="OpenTrunks"/>); scenery unless it says.</summary>
    public bool Trunk { get; set; }
}

/// <summary>
/// A grove: a polygon on the ground, how far apart its trees stand (<c>spacing</c>, metres), the share of its cells
/// that hold one (<c>fill</c>), the share of those that are pines (<c>pines</c>), their sizes ([least, most]) and a seed.
/// </summary>
public sealed class OpenGroveFile
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public double[][] Polygon { get; set; } = Array.Empty<double[]>();
    public double Spacing { get; set; } = 6;
    public double Fill { get; set; } = 0.8;
    public double Pines { get; set; } = 0.5;
    public double[] Size { get; set; } = { 0.9, 1.25 };
    public int Seed { get; set; }

    /// <summary>Whether its trees' trunks are solid (<see cref="OpenTrunks"/>): a forest walked round; scenery unless it says.</summary>
    public bool Trunks { get; set; }
}

/// <summary>
/// How a solid is built (plan 32 · OW9): the kit's kind of building, the town whose way of building a house follows,
/// its storeys (nought: its style's), the way its front faces (<c>down</c>, south, unless told) and the name over its door.
/// </summary>
public sealed class OpenBuildingFile
{
    public BuildingKind Kind { get; set; } = BuildingKind.House;
    public Architecture Town { get; set; } = Architecture.Timber;
    public int Storeys { get; set; }
    public Direction Faces { get; set; } = Direction.Down;
    public string? Sign { get; set; }
}

/// <summary>
/// Someone of an open map (plan 32 · OW6): their name (what a script's <c>walkto</c> and <c>faceto</c> name), where they
/// stand ([x, y, z], the feet's) and the way they face in degrees; their look, the name over their lines, their own
/// script or their lines; the trainer of the data they battle as and their cone (<c>sight</c>); how they move
/// (<c>still</c>, <c>lookAbout</c>, <c>wander</c>) within a <c>radius</c> of metres or a polygon (<c>bounds</c>); and
/// the flags that hide or show them.
/// </summary>
public sealed class OpenPersonFile
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public double[] At { get; set; } = Array.Empty<double>();
    public double Yaw { get; set; }
    public string? Look { get; set; }
    public string? DisplayName { get; set; }
    public string? Script { get; set; }
    public List<string> Lines { get; set; } = new();
    public string? Trainer { get; set; }
    public OpenSightFile? Sight { get; set; }
    public OpenPersonMoves Moves { get; set; } = OpenPersonMoves.Still;
    public double? Radius { get; set; }
    public double[][]? Bounds { get; set; }
    public string? HiddenBy { get; set; }
    public string? ShownBy { get; set; }
}

/// <summary>A trainer's cone: how far they see, in metres, and how wide the cone is in all, in degrees.</summary>
public sealed class OpenSightFile
{
    [DataRequired] public double Length { get; set; }
    public double Angle { get; set; } = 60;
}

/// <summary>A marker: its name, its place in the air ([x, y, z], the feet's) and the way it faces in degrees (nought south, 90 east).</summary>
public sealed class OpenMarkerFile
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public double[] At { get; set; } = Array.Empty<double>();
    public double Yaw { get; set; }
}

/// <summary>An upright box between two corners in the air.</summary>
public sealed class OpenBoxFile
{
    [DataRequired] public double[] Min { get; set; } = Array.Empty<double>();
    [DataRequired] public double[] Max { get; set; } = Array.Empty<double>();
}

/// <summary>An upright cylinder standing at [x, z] from its bottom to its top.</summary>
public sealed class OpenCylinderFile
{
    [DataRequired] public double[] At { get; set; } = Array.Empty<double>();
    [DataRequired] public double Radius { get; set; }
    [DataRequired] public double Bottom { get; set; }
    [DataRequired] public double Top { get; set; }
}

/// <summary>A trigger: a <c>box</c> or a <c>cylinder</c>, the script it starts and the variable and value it waits for.</summary>
public sealed class OpenTriggerFile
{
    [DataRequired] public string Name { get; set; } = "";
    public OpenBoxFile? Box { get; set; }
    public OpenCylinderFile? Cylinder { get; set; }
    [DataRequired] public string Script { get; set; } = "";
    public string? Var { get; set; }
    public int Value { get; set; }
}

/// <summary>
/// A door into a grid room: a <c>box</c> or a <c>cylinder</c>, the room's map, the tile it lands on (<c>at</c>, [x, y])
/// facing <c>facing</c>, the marker the room's way back out lands on (<c>out</c>), and a flag it waits for.
/// </summary>
public sealed class OpenDoorFile
{
    [DataRequired] public string Name { get; set; } = "";
    public OpenBoxFile? Box { get; set; }
    public OpenCylinderFile? Cylinder { get; set; }
    [DataRequired] public string Room { get; set; } = "";
    [DataRequired] public int[] At { get; set; } = Array.Empty<int>();
    public Direction Facing { get; set; } = Direction.Up;
    [DataRequired] public string Out { get; set; } = "";
    public string? OpenedBy { get; set; }
}

/// <summary>An encounter zone: a polygon on the ground between two heights, its table, its density (to a thousand square metres) and its cap.</summary>
public sealed class OpenZoneFile
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public double[][] Polygon { get; set; } = Array.Empty<double[]>();
    public double MinY { get; set; } = -1000;
    public double MaxY { get; set; } = 10000;
    [DataRequired] public List<OpenSlotFile> Slots { get; set; } = new();
    public int Density { get; set; } = 4;
    public int Cap { get; set; } = 4;

    /// <summary>
    /// The zone's rustling grass's table (plan 33 · GL2): met by touching a patch of its grass that shakes now and then,
    /// never seen walking about. Left out where the grass never rustles.
    /// </summary>
    public List<OpenSlotFile>? Rustle { get; set; }
}

/// <summary>A row of a zone's table.</summary>
public sealed class OpenSlotFile
{
    [DataRequired] public string Species { get; set; } = "";
    [DataRequired] public int MinLevel { get; set; }
    [DataRequired] public int MaxLevel { get; set; }
    public int Weight { get; set; } = 10;

    /// <summary>When in the day the row is met (plan 32 · OW7): <c>Any</c> unless told, <c>Morning</c>, <c>Day</c> or <c>Night</c>.</summary>
    public OpenSlotTime Time { get; set; }

    /// <summary>The weathers the row is met under (plan 33 · GL2: <c>Rain</c>, <c>Snow</c>...); left out, under any.</summary>
    public List<FieldWeather>? Weather { get; set; }
}

/// <summary>Water laid by hand: [x, z] corners and the surface's height.</summary>
public sealed class OpenWaterFile
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public double[] Min { get; set; } = Array.Empty<double>();
    [DataRequired] public double[] Max { get; set; } = Array.Empty<double>();
    [DataRequired] public double Level { get; set; }
}

/// <summary>A solid box: its name and two corners in the air, and how it is built where it is a building (plan 32 · OW9).</summary>
public sealed class OpenSolidFile
{
    [DataRequired] public string Name { get; set; } = "";
    [DataRequired] public double[] Min { get; set; } = Array.Empty<double>();
    [DataRequired] public double[] Max { get; set; } = Array.Empty<double>();
    public OpenBuildingFile? Building { get; set; }
}

/// <summary>What the game reads of a region's <c>world.json</c>: its areas and its maps (the baker reads the whole file strictly).</summary>
public sealed class OpenRegionIndexFile
{
    public string Region { get; set; } = "";
    public List<OpenRegionAreaFile> Areas { get; set; } = new();
    public List<OpenRegionMapFile> Maps { get; set; } = new();
}

public sealed class OpenRegionAreaFile
{
    public string Key { get; set; } = "";
    public string? Name { get; set; }
    public double[][] Polygon { get; set; } = Array.Empty<double[]>();
    public double MinY { get; set; } = -1000;
    public double MaxY { get; set; } = 10000;
    public string? Music { get; set; }
    public string? Weather { get; set; }
}

public sealed class OpenRegionMapFile
{
    public string Name { get; set; } = "";
    public List<string> Areas { get; set; } = new();
}

/// <summary>
/// Reads an open region's places and lays them on its maps (plan 32 · OW5). GPU-free; reads files and nothing else.
/// </summary>
public static class OpenPlaceFiles
{
    /// <summary>The folder of a region's places, under its description folder.</summary>
    public const string Folder = "places";

    /// <summary>A place's file read strictly and held to what JSON can't say; <paramref name="file"/> names it in a finding.</summary>
    public static OpenPlaceFile Read(string text, string file)
    {
        var place = GameDataFiles.ReadStrict<OpenPlaceFile>(text, file);
        Validate(place, file);
        return place;
    }

    /// <summary>A yaw in degrees (nought south, 90 east) as 65,536ths of a turn.</summary>
    public static int YawOfDegrees(double degrees) =>
        OpenYaw.Normalize((int)Math.Round(degrees * OpenYaw.Turn / 360.0, MidpointRounding.AwayFromZero));

    static OpenPos Air(double[] p) => OpenPos.FromMetres(p[0], p[1], p[2]);
    static (int X, int Z) Ground(double[] p) => (OpenPos.Millimetres(p[0]), OpenPos.Millimetres(p[^1]));

    /// <summary>A trigger's or a door's shape.</summary>
    public static OpenShape ShapeOf(OpenBoxFile? box, OpenCylinderFile? cylinder) =>
        box != null
            ? OpenShape.Box(Air(box.Min), Air(box.Max))
            : OpenShape.Cylinder(OpenPos.Millimetres(cylinder!.At[0]), OpenPos.Millimetres(cylinder.At[1]),
                OpenPos.Millimetres(cylinder.Bottom), OpenPos.Millimetres(cylinder.Top), OpenPos.Millimetres(cylinder.Radius));

    /// <summary>Lays a place on its map: its markers, triggers, doors, zones, water and solids, each knowing the place (its script file).</summary>
    public static void Lay(OpenMap map, string key, OpenPlaceFile place)
    {
        foreach (var m in place.Markers) map.Add(new OpenMarker(m.Name, Air(m.At), YawOfDegrees(m.Yaw)));
        foreach (var t in place.Triggers)
            map.Add(new OpenTrigger(t.Name, key, ShapeOf(t.Box, t.Cylinder), t.Script, t.Var, t.Value));
        foreach (var d in place.Doors)
            map.Add(new OpenDoor(d.Name, key, ShapeOf(d.Box, d.Cylinder), d.Room, d.At[0], d.At[1], d.Facing, d.Out, d.OpenedBy));
        foreach (var z in place.Zones)
            map.Add(new OpenZone(z.Name, key, z.Polygon.Select(Ground).ToList(), OpenPos.Millimetres(z.MinY), OpenPos.Millimetres(z.MaxY),
                z.Slots.Select(SlotOf).ToList(), z.Density, z.Cap) { Rustle = z.Rustle?.Select(SlotOf).ToList() ?? (IReadOnlyList<OpenSlot>)Array.Empty<OpenSlot>() });
        foreach (var w in place.Water) map.Add(OpenWaterBox.Metres(w.Name, w.Min[0], w.Min[1], w.Max[0], w.Max[1], w.Level));
        foreach (var s in place.Solids)
        {
            var box = OpenBox.Metres(s.Name, s.Min[0], s.Min[1], s.Min[2], s.Max[0], s.Max[1], s.Max[2]);
            map.Add(box);
            if (s.Building is { } b) map.Add(new OpenBuilding(box, b.Kind, b.Town, b.Storeys, b.Faces, b.Sign));
        }
        foreach (var p in place.People) map.Add(PersonOf(key, p));
        // Scenery (plan 32 · OW9): the trees standing one by one, then the groves'
        foreach (var t in place.Trees) map.Add(TreeOf(t));
        foreach (var g in place.Groves) map.Add(GroveOf(g));
        if (place.EvolutionSites.Count > 0) map.AddSites(key, place.EvolutionSites);
    }

    /// <summary>A row of a zone's table as the map keeps it.</summary>
    static OpenSlot SlotOf(OpenSlotFile s) =>
        new(s.Species, s.MinLevel, s.MaxLevel, s.Weight, s.Time) { Weather = s.Weather is { Count: > 0 } w ? w.ToList() : null };

    /// <summary>A tree of a place as the map keeps it (plan 32 · OW9).</summary>
    public static OpenTree TreeOf(OpenTreeFile t)
    {
        bool high = t.At.Length == 3;
        int x = OpenPos.Millimetres(t.At[0]), z = OpenPos.Millimetres(t.At[^1]);
        // A tree standing by itself takes its shape and its turn from where it stands
        uint h = Core.SplitMix.Mix(0x7e3f1a2bu, (uint)x, (uint)z);
        return new OpenTree(x, z, high ? OpenPos.Millimetres(t.At[1]) : null, t.Kind, (int)Math.Round(t.Size * 1000, MidpointRounding.AwayFromZero),
            (int)(h % OpenTree.Variants), (int)((h >> 8) & 0xFFFF)) { Trunk = t.Trunk };
    }

    /// <summary>A grove of a place as the map keeps it (plan 32 · OW9): its numbers in whole millimetres and thousandths.</summary>
    public static OpenGrove GroveOf(OpenGroveFile g) =>
        new(g.Name, g.Polygon.Select(Ground).ToList(), OpenPos.Millimetres(g.Spacing),
            (int)Math.Round(g.Fill * 1000, MidpointRounding.AwayFromZero), (int)Math.Round(g.Pines * 1000, MidpointRounding.AwayFromZero),
            (int)Math.Round(g.Size[0] * 1000, MidpointRounding.AwayFromZero), (int)Math.Round(g.Size[1] * 1000, MidpointRounding.AwayFromZero), g.Seed, g.Trunks);

    /// <summary>Someone of a place as the map keeps them (plan 32 · OW6).</summary>
    public static OpenPerson PersonOf(string key, OpenPersonFile p)
    {
        var home = Air(p.At);
        return new OpenPerson(p.Name, key, home, YawOfDegrees(p.Yaw))
        {
            Look = p.Look,
            ShownName = p.DisplayName,
            Script = p.Script,
            Lines = p.Lines.ToList(),
            TrainerId = p.Trainer,
            Sight = p.Sight is { } s ? OpenSight.Of(s.Length, s.Angle) : null,
            Moves = p.Moves,
            Bounds = p.Bounds is { } bounds ? bounds.Select(Ground).ToList() : OpenPerson.Circle(home, OpenPos.Millimetres(p.Radius ?? OpenPerson.WanderRadius / 1000.0)),
            HiddenBy = p.HiddenBy,
            ShownBy = p.ShownBy
        };
    }

    /// <summary>An area of <c>world.json</c> as the map keeps it.</summary>
    public static OpenArea AreaOf(OpenRegionAreaFile a) =>
        new(a.Key, a.Name ?? a.Key, a.Polygon.Select(Ground).ToList(), OpenPos.Millimetres(a.MinY), OpenPos.Millimetres(a.MaxY), a.Music, a.Weather);

    /// <summary>
    /// A region's open maps from its description folder (plan 32 · OW5): one for each of <c>world.json</c>'s maps, with
    /// its areas in the region's order, the baked land where <c>baked/index.json</c> is there, and the places of its
    /// areas laid on it. A place whose key is no area of a map, or a file that doesn't read, stops the load with the
    /// file named, as the grid's overlays do.
    /// </summary>
    public static List<OpenMap> LoadRegion(string folder, bool withLand = true)
    {
        string worldPath = Path.Combine(folder, "world.json");
        var index = JsonSerializer.Deserialize<OpenRegionIndexFile>(File.ReadAllText(worldPath), GameDataFiles.Json)
            ?? throw new InvalidDataException($"{worldPath}: the file is empty");
        string baked = Path.Combine(folder, "baked");
        BakedRegion? land = withLand && File.Exists(Path.Combine(baked, "index.json")) ? BakedRegion.Open(baked) : null;

        // The region's weather by area and day (plan 33 · GL2), where it keeps one
        var calendar = OpenCalendar.Read(folder);
        var maps = new List<OpenMap>();
        var mapOfArea = new Dictionary<string, OpenMap>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in index.Maps)
        {
            var map = new OpenMap(entry.Name, index.Region) { Land = land, Calendar = calendar };
            foreach (var area in index.Areas.Where(a => entry.Areas.Contains(a.Key, StringComparer.OrdinalIgnoreCase)))
            {
                map.Add(AreaOf(area));
                mapOfArea[area.Key] = map;
            }
            maps.Add(map);
        }

        string places = Path.Combine(folder, Folder);
        if (Directory.Exists(places))
            foreach (string path in Directory.GetFiles(places, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                string key = Path.GetFileNameWithoutExtension(path), file = $"{Folder}/{key}.json";
                if (!mapOfArea.TryGetValue(key, out var map)) throw new InvalidDataException($"{file}: is named for no area of a map of world.json");
                try
                {
                    Lay(map, key, Read(File.ReadAllText(path), file));
                }
                catch (ArgumentException e)
                {
                    throw new InvalidDataException($"{file}: {e.Message}", e);
                }
            }
        return maps;
    }

    // ---------------------------------------------------------------- what the format can't say

    static void Validate(OpenPlaceFile place, string file)
    {
        for (int i = 0; i < place.Markers.Count; i++)
        {
            Point(place.Markers[i].At, 3, file, $"$.markers[{i}].at");
            Name(place.Markers[i].Name, file, $"$.markers[{i}].name");
        }
        for (int i = 0; i < place.Triggers.Count; i++)
        {
            var t = place.Triggers[i];
            Shape(t.Box, t.Cylinder, file, $"$.triggers[{i}]");
            if (t.Script.Length == 0) Fail(file, $"$.triggers[{i}].script", "a trigger starts a script");
        }
        for (int i = 0; i < place.Doors.Count; i++)
        {
            var d = place.Doors[i];
            Shape(d.Box, d.Cylinder, file, $"$.doors[{i}]");
            if (d.At.Length != 2) Fail(file, $"$.doors[{i}].at", "a room's tile is [x, y]");
            if (d.Room.Length == 0) Fail(file, $"$.doors[{i}].room", "a door goes into a room");
            if (d.Out.Length == 0) Fail(file, $"$.doors[{i}].out", "a door names the marker its room's way out lands on");
        }
        for (int i = 0; i < place.Zones.Count; i++)
        {
            var z = place.Zones[i];
            Polygon(z.Polygon, file, $"$.zones[{i}].polygon");
            if (z.MaxY <= z.MinY) Fail(file, $"$.zones[{i}]", "maxY is over minY");
            if (z.Slots.Count == 0) Fail(file, $"$.zones[{i}].slots", "a zone has a row of its table or more");
            if (z.Cap < 1) Fail(file, $"$.zones[{i}].cap", "is one or more");
            if (z.Density < 1) Fail(file, $"$.zones[{i}].density", "is one or more");
            var rows = z.Slots.Select((slot, n) => (slot, path: $"$.zones[{i}].slots[{n}]"))
                .Concat((z.Rustle ?? new()).Select((slot, n) => (slot, path: $"$.zones[{i}].rustle[{n}]")));
            if (z.Rustle is { Count: 0 }) Fail(file, $"$.zones[{i}].rustle", "a zone's rustling grass has a row of its table or more, or is left out");
            foreach (var (slot, path) in rows)
            {
                if (slot.Weather is { } weathers)
                    for (int w = 0; w < weathers.Count; w++)
                        if (!Enum.IsDefined(weathers[w])) Fail(file, $"{path}.weather[{w}]", "is a weather of the field's");
                if (slot.MinLevel < 1 || slot.MaxLevel < slot.MinLevel) Fail(file, path, "levels from one, maxLevel no less than minLevel");
                if (slot.MaxLevel > Ruleset.HighestLevel) Fail(file, $"{path}.maxLevel", $"is no more than {Ruleset.HighestLevel}, the highest level a Pokémon may have");
                if (!Enum.IsDefined(slot.Time)) Fail(file, $"{path}.time", "is any, morning, day or night");
                if (slot.Weight < 1) Fail(file, $"{path}.weight", "is one or more");
            }
        }
        for (int i = 0; i < place.Water.Count; i++)
        {
            Point(place.Water[i].Min, 2, file, $"$.water[{i}].min");
            Point(place.Water[i].Max, 2, file, $"$.water[{i}].max");
            if (place.Water[i].Max[0] <= place.Water[i].Min[0] || place.Water[i].Max[1] <= place.Water[i].Min[1]) Fail(file, $"$.water[{i}]", "max is more than min on both axes");
        }
        for (int i = 0; i < place.Solids.Count; i++)
        {
            Point(place.Solids[i].Min, 3, file, $"$.solids[{i}].min");
            Point(place.Solids[i].Max, 3, file, $"$.solids[{i}].max");
            if (place.Solids[i].Min.Zip(place.Solids[i].Max).Any(p => p.Second <= p.First)) Fail(file, $"$.solids[{i}]", "max is more than min on every axis");
            if (place.Solids[i].Building is { } b && (b.Storeys < 0 || b.Storeys > MostStoreys)) Fail(file, $"$.solids[{i}].building.storeys", $"is nought (its style's own) to {MostStoreys}");
        }
        Validate(place.People, file);
        for (int i = 0; i < place.Trees.Count; i++)
        {
            var t = place.Trees[i];
            if (t.At.Length is not (2 or 3)) Fail(file, $"$.trees[{i}].at", "a tree's foot is [x, z], or [x, y, z] near a height");
            if (!(t.Size > 0) || t.Size * 1000 > OpenTree.Largest) Fail(file, $"$.trees[{i}].size", $"is more than nought and {OpenTree.Largest / 1000} at most");
        }
        for (int i = 0; i < place.Groves.Count; i++)
        {
            var g = place.Groves[i];
            string at = $"$.groves[{i}]";
            Polygon(g.Polygon, file, at + ".polygon");
            if (!(g.Spacing >= LeastSpacing) || g.Spacing > MostSpacing) Fail(file, at + ".spacing", $"is {LeastSpacing} to {MostSpacing} metres");
            if (!(g.Fill > 0) || g.Fill > 1) Fail(file, at + ".fill", "is a share of the cells, more than nought and one at most");
            if (!(g.Pines >= 0) || g.Pines > 1) Fail(file, at + ".pines", "is a share of the trees, nought to one");
            if (g.Size.Length != 2 || !(g.Size[0] > 0) || g.Size[1] < g.Size[0] || g.Size[1] * 1000 > OpenTree.Largest)
                Fail(file, at + ".size", $"is [least, most], more than nought, the most no less than the least and {OpenTree.Largest / 1000} at most");
        }
    }

    /// <summary>How many storeys a building of a place may have.</summary>
    public const int MostStoreys = 6;

    /// <summary>How near and how far apart a grove's trees may stand, in metres.</summary>
    public const double LeastSpacing = 1.5, MostSpacing = 50;

    static void Validate(List<OpenPersonFile> people, string file)
    {
        for (int i = 0; i < people.Count; i++)
        {
            var p = people[i];
            string at = $"$.people[{i}]";
            Name(p.Name, file, at + ".name");
            if (p.Name == "player") Fail(file, at + ".name", "'player' is the player in a script; someone else has a name of their own");
            Point(p.At, 3, file, at + ".at");
            if (p.Trainer is { Length: 0 }) Fail(file, at + ".trainer", "names a trainer of the data, or is left out");
            if (p.Sight is { } sight)
            {
                if (p.Trainer == null) Fail(file, at + ".sight", "only a trainer looks out for the player");
                if (!(sight.Length > 0) || sight.Length * OpenPos.PerMetre > OpenSight.Longest) Fail(file, at + ".sight.length", $"is more than nought and {OpenSight.Longest / OpenPos.PerMetre} m at most");
                if (!(sight.Angle > 0) || sight.Angle > 360) Fail(file, at + ".sight.angle", "is the cone's width in all, more than nought and 360° at most");
            }
            if (p.Radius is { } radius && !(radius > 0)) Fail(file, at + ".radius", "is more than nought");
            if (p.Bounds is { } bounds)
            {
                if (p.Radius != null) Fail(file, at, "wanders within a radius or within bounds, not both");
                Polygon(bounds, file, at + ".bounds");
            }
            if ((p.Radius != null || p.Bounds != null) && p.Moves != OpenPersonMoves.Wander) Fail(file, at, "has bounds to wander in and doesn't wander (moves: wander)");
        }
    }

    static void Shape(OpenBoxFile? box, OpenCylinderFile? cylinder, string file, string path)
    {
        if ((box == null) == (cylinder == null)) Fail(file, path, "a volume is a box or a cylinder, not both");
        if (box != null)
        {
            Point(box.Min, 3, file, path + ".box.min");
            Point(box.Max, 3, file, path + ".box.max");
            if (box.Min.Zip(box.Max).Any(p => p.Second <= p.First)) Fail(file, path + ".box", "max is more than min on every axis");
        }
        if (cylinder != null)
        {
            Point(cylinder.At, 2, file, path + ".cylinder.at");
            if (!(cylinder.Radius > 0)) Fail(file, path + ".cylinder.radius", "is more than nought");
            if (cylinder.Top <= cylinder.Bottom) Fail(file, path + ".cylinder", "the top is over the bottom");
        }
    }

    static void Name(string name, string file, string path)
    {
        if (name.Length == 0 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_')) Fail(file, path, "a marker's name is letters, digits and _, as a script names it");
    }

    static void Fail(string file, string path, string message) => throw new InvalidDataException($"{file}: {path}: {message}");
    static void Point(double[] p, int length, string file, string path) { if (p.Length != length) Fail(file, path, length == 2 ? "a point on the ground is [x, z]" : "a point in the air is [x, y, z]"); }
    static void Polygon(double[][] points, string file, string path)
    {
        if (points.Length < 3) Fail(file, path, "a polygon has three points or more");
        for (int i = 0; i < points.Length; i++) Point(points[i], 2, file, $"{path}[{i}]");
    }
}
