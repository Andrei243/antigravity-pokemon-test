using OpenWorldBake;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;

namespace ContentCheck;

/// <summary>
/// The open world's rules (plan 32 · OW5), for each open region of the folder (<c>world3d/&lt;region&gt;/</c>):
/// <list type="bullet">
/// <item>a carve's roof thinner than half a metre where the land runs beside it (<see cref="CarveShells"/>: the
/// mesher turns faces in a shell thinner than a cell, OW2's finding);</item>
/// <item>a marker whose body would stand in rock, and a trigger or a door whose middle is in rock;</item>
/// <item>a door into a room that is no map, onto a tile of it that is off it or blocked, to a marker its map hasn't, a
/// room with no way back out to the map, and a flag it waits for that nothing sets;</item>
/// <item>a room's way out to an open map that no door of that map leads back into;</item>
/// <item>a trigger's script that neither its place's file nor the common one has;</item>
/// <item>a zone's species that the data hasn't, and a zone with no ground to stand on inside it (OW4's navigation mesh
/// will be what it is held to);</item>
/// <item>what <c>ScriptLibrary.OpenProblems</c> says of the scripts: a tile form in an open place, a marker its map
/// hasn't, an open form in a grid place, a <c>warpto</c> to no open map's marker, a <c>warp</c> to an open map.</item>
/// </list>
/// A trainer's cone that sees nothing waits for OW6's trainers, and a place nobody can come to for OW4's <c>OpenWalk</c>.
/// </summary>
public static class OpenWorlds
{
    /// <summary>Everything this check finds in the folder's open regions, and in the rooms and scripts that lead to them.</summary>
    public static void Check(GameContent content, Checks.Flags flags, List<Finding> found)
    {
        foreach (var region in content.OpenRegions) CheckRegion(region, content.MapNamed, content.Scripts, flags, found);
        CheckWaysIn(content.Rooms.Where(r => r.Map != null).Select(r => (r.File, r.Map!)), content.OpenMapNamed, found);
        CheckScripts(content, found);
    }

    /// <summary>One open region's land, places and doors (a test holds the baker's open test world to them with rooms of its own).</summary>
    public static void CheckRegion(GameContent.OpenRegion region, Func<string, Map?> roomNamed, PokemonPlatinumEngine.Story.ScriptLibrary scripts, Checks.Flags? flags, List<Finding> found)
    {
        if (region.Description is { } description && region.Land is { } land)
        {
            // The carves in the land's order: file by file, as RegionDescription.Land lays them
            var names = description.Carves.SelectMany(c => c.Carve.Carves.Select(s => (c.File, Name: s.Name ?? s.GetType().Name))).ToList();
            foreach (var thin in CarveShells.Thin(land))
            {
                var (file, name) = names[thin.Carve];
                found.Add(new Finding(Kind.Land, $"{region.Folder}/{file}", name,
                    $"its roof has {thin.Thinnest * 100:0} cm of rock under the land at [{thin.X:0.##}, {thin.CeilingY:0.##}, {thin.Z:0.##}], and less than {CarveShells.LeastRock * 100:0} cm over {thin.Area:0.##} m² beside it: "
                    + "the mesher turns faces in a shell thinner than a cell; lower the roof, raise the land or open it to the air"));
            }
        }
        if (region.Maps is not { } maps) return;
        foreach (var map in maps) CheckMap(region, map, roomNamed, scripts, flags, found);
    }

    static string PlaceFile(GameContent.OpenRegion region, string key) => $"{region.Folder}/{OpenPlaceFiles.Folder}/{key}.json";

    static void CheckMap(GameContent.OpenRegion region, OpenMap map, Func<string, Map?> roomNamed, PokemonPlatinumEngine.Story.ScriptLibrary scripts, Checks.Flags? flags, List<Finding> found)
    {
        var land = region.Land;
        bool InRock(OpenPos p) => land != null && land.Field(new D3(p.MetresX, p.MetresY, p.MetresZ)) < 0;
        string FileOfMarker(OpenMarker m) => map.AreaAt(m.Pos) is { } area ? PlaceFile(region, area.Key) : $"{region.Folder}/world.json";

        foreach (var marker in map.Markers)
            if (InRock(OpenCrossings.CentreOf(marker.Pos)))
                found.Add(new Finding(Kind.Place, FileOfMarker(marker), marker.Name, $"stands in rock: a body on it at {marker.Pos} would have its middle inside the land"));

        foreach (var trigger in map.Triggers)
        {
            string file = PlaceFile(region, trigger.Place);
            if (InRock(trigger.Shape.Middle)) found.Add(new Finding(Kind.Place, file, trigger.Name, $"is in rock: its middle {trigger.Shape.Middle} is inside the land, so no body crosses into it there"));
            if (scripts.Find(trigger.Script, trigger.Place) == null)
                found.Add(new Finding(Kind.Script, file, trigger.Name, $"starts the script '{trigger.Script}', which is in neither scripts/{trigger.Place}.txt nor scripts/{PokemonPlatinumEngine.Story.ScriptLibrary.Common}.txt"));
        }

        foreach (var door in map.Doors)
        {
            string file = PlaceFile(region, door.Place);
            if (InRock(door.Shape.Middle)) found.Add(new Finding(Kind.Place, file, door.Name, $"is in rock: its middle {door.Shape.Middle} is inside the land"));
            if (map.Marker(door.Out) == null) found.Add(new Finding(Kind.Way, file, door.Name, $"brings the player back out onto the marker '{door.Out}', which {map.Name} hasn't"));
            flags?.Check(found, file, door.Name, door.OpenedBy, "waits for");
            if (roomNamed(door.Room) is not { } room)
            {
                found.Add(new Finding(Kind.Way, file, door.Name, $"goes into '{door.Room}', which is no room"));
                continue;
            }
            if (!room.InBounds(door.RoomX, door.RoomY)) found.Add(new Finding(Kind.Way, file, door.Name, $"puts the player down at {door.RoomX},{door.RoomY} of {door.Room}, which is {room.Width} by {room.Height} tiles"));
            else if (room.IsSolid(door.RoomX, door.RoomY)) found.Add(new Finding(Kind.Way, file, door.Name, $"puts the player down on a blocked tile of {door.Room} ({door.RoomX},{door.RoomY})"));
            if (!room.Warps.Any(w => string.Equals(w.TargetMap, map.Name, StringComparison.OrdinalIgnoreCase)))
                found.Add(new Finding(Kind.Way, file, door.Name, $"goes into {door.Room}, where no way leads back out to {map.Name}: the player could never come out"));
        }

        foreach (var zone in map.Zones)
        {
            string file = PlaceFile(region, zone.Place);
            foreach (var slot in zone.Slots)
                if (PokemonDatabase.Get(slot.Species) == null && PokemonDatabase.SpeciesOfForm(slot.Species) == null)
                    found.Add(new Finding(Kind.Data, file, zone.Name, $"has a '{slot.Species}', which is no species or form"));
            if (land != null && !HasGround(land, zone))
                found.Add(new Finding(Kind.Place, file, zone.Name, $"has no ground to stand on between {zone.MinY} mm and {zone.MaxY} mm inside it: nothing of its table could stand there"));
        }
    }

    /// <summary>Whether a zone holds ground facing up anywhere between its heights: a column of its, every two metres, crossing from air down into rock.</summary>
    static bool HasGround(TerrainWorld land, OpenZone zone)
    {
        int minX = zone.Polygon.Min(p => p.X), maxX = zone.Polygon.Max(p => p.X), minZ = zone.Polygon.Min(p => p.Z), maxZ = zone.Polygon.Max(p => p.Z);
        for (int x = minX + 500; x <= maxX; x += 2000)
            for (int z = minZ + 500; z <= maxZ; z += 2000)
            {
                if (!OpenPolygons.Holds(zone.Polygon, x, z)) continue;
                bool airAbove = land.Field(new D3(x / 1000.0, zone.MaxY / 1000.0, z / 1000.0)) >= 0;
                for (int y = zone.MaxY - 250; y >= zone.MinY; y -= 250)
                {
                    bool air = land.Field(new D3(x / 1000.0, y / 1000.0, z / 1000.0)) >= 0;
                    if (airAbove && !air) return true;
                    airAbove = air;
                }
            }
        return false;
    }

    /// <summary>Every grid room's way out to an open map: some door of that map must lead back into the room, or the way lands nowhere.</summary>
    public static void CheckWaysIn(IEnumerable<(string File, Map Room)> rooms, Func<string, OpenMap?> openMapNamed, List<Finding> found)
    {
        foreach (var (file, room) in rooms)
            foreach (var warp in room.Warps)
                if (openMapNamed(warp.TargetMap) is { } open && open.DoorBackFrom(room.Name, warp.SourceX, warp.SourceY) == null)
                    found.Add(new Finding(Kind.Way, file, $"warp at {warp.SourceX},{warp.SourceY}", $"leads out to the open map {open.Name}, where no door leads back into {room.Name}: it lands on no marker"));
    }

    /// <summary>What the scripts say of the open world's places (<c>ScriptLibrary.OpenProblems</c>), as findings of the scripts.</summary>
    static void CheckScripts(GameContent content, List<Finding> found)
    {
        var mapOfArea = new Dictionary<string, OpenMap>(StringComparer.OrdinalIgnoreCase);
        foreach (var map in content.OpenRegions.SelectMany(r => r.Maps ?? Array.Empty<OpenMap>()))
            foreach (var area in map.Areas) mapOfArea[area.Key] = map;
        foreach (string problem in OpenProblems(content.Scripts, mapOfArea, content.OpenMapNamed))
            found.Add(Scripts.FindingOf(problem));
    }

    /// <summary>The scripts' problems with the open world's places, given which areas are whose open maps.</summary>
    public static List<string> OpenProblems(PokemonPlatinumEngine.Story.ScriptLibrary scripts, IReadOnlyDictionary<string, OpenMap> mapOfArea, Func<string, OpenMap?> openMapNamed) =>
        scripts.OpenProblems(
            file => mapOfArea.TryGetValue(file, out var map) ? map.Markers.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) : null,
            name => openMapNamed(name) is { } map ? map.Markers.Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase) : null);
}
