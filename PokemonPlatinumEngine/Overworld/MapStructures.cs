using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

public enum BuildingKind
{
    House,
    PokemonCenter,
    PokeMart,
    Lab,

    // Named by the map (see Map.BuildingKinds): a door's destination doesn't say what these are
    School,
    Office,
    TvStation,
    Terminal,
    Apartments,

    // Named by the model of the imported world (Data/WorldModels): what stands in Sinnoh's towns besides houses
    Gym,
    /// <summary>The house a road passes through between a route and a city.</summary>
    Gate,
    Museum,
    Library,
    /// <summary>A business with its name over the door: a flower shop, a café, a market, a department store.</summary>
    Shop,
    Hotel,
    /// <summary>A large public hall: the Contest Hall, Pal Park, the Battle Frontier's facilities.</summary>
    Hall,
    /// <summary>Hearthome's Foreign Building: tall, steep-roofed, with a rose window.</summary>
    Chapel,
    /// <summary>Snowpoint's temple: old stone in two tiers.</summary>
    Temple,
    /// <summary>A small wooden shrine under a heavy roof (Celestic).</summary>
    Shrine,
    /// <summary>A tall stone tower (the Lost Tower, the Battle Tower).</summary>
    Tower,
    Lighthouse,
    /// <summary>Works under sheet metal: the mine, the Windworks, the Ironworks.</summary>
    Factory,
    Warehouse,
    Mansion,
    /// <summary>Team Galactic's buildings: grey panels, slit windows, spikes on the roof.</summary>
    Galactic,
    League
}

/// <summary>A building found on an outdoor map: the rectangle of roof/wall tiles and what sits on its front row.</summary>
public sealed class BuildingInfo
{
    public int X0 { get; init; }
    public int Y0 { get; init; }
    public int X1 { get; init; }
    public int Y1 { get; init; }
    public BuildingKind Kind { get; init; }
    public TileType RoofTile { get; init; }

    /// <summary>The original's short name for the model this building stands in for; empty on a hand-made map.</summary>
    public string Model { get; init; } = "";

    /// <summary>How it is built, for a building of the world, whose model says which town's it is; null follows the map.</summary>
    public Architecture? Town { get; init; }

    /// <summary>How many storeys it has; 0 leaves that to its style.</summary>
    public int Storeys { get; init; }

    /// <summary>The name over its door, where the model is a business; null for the kind's own sign.</summary>
    public string? Sign { get; init; }

    /// <summary>For a gym: its leader's type, which colours it.</summary>
    public PokemonType? Theme { get; init; }

    /// <summary>A wing of a larger structure beside its main block: drawn plain, without an entrance.</summary>
    public bool Annex { get; init; }

    /// <summary>How tall the original's model stands, in tiles; 0 when nothing says.</summary>
    public float Height { get; init; }

    /// <summary>Doors on the front (bottom) row, with the map each one warps to (null for decorative doors).</summary>
    public List<(int X, string? Target)> Doors { get; } = new();

    /// <summary>Wall-mounted signs on the front row.</summary>
    public List<int> Plaques { get; } = new();

    /// <summary>
    /// The tiles (by their x) of an entrance built out a tile in front of the wall, on the row south of
    /// <see cref="Y1"/>: the walls of a porch. Empty when the doors are in the front wall itself.
    /// </summary>
    public List<int> Porch { get; } = new();

    /// <summary>
    /// True when the way in is open ground between the porch's sides, with the door set back in the wall behind;
    /// false when the door is in the porch's own front.
    /// </summary>
    public bool PorchIsOpen { get; set; }

    /// <summary>The rows of the doors in the side walls: a gate house on a road that runs east and west.</summary>
    public List<int> SideDoors { get; } = new();

    public int Width => X1 - X0 + 1;
    public int Depth => Y1 - Y0 + 1;
}

/// <summary>Derives larger structures (buildings) from a map's tile grid so they can be rendered as single 3D models.</summary>
public static class MapStructures
{
    public static bool IsRoof(TileType t) => t is TileType.RoofRed or TileType.RoofBlue or TileType.RoofGreen;

    /// <summary>A signpost set into a wall row is a name plate on the building rather than a free-standing sign.</summary>
    public static bool IsWallSign(Map map, int x, int y)
    {
        // On a map of the world a building is exactly its model's footprint, and a sign beside it stands on its own
        if (map.PlacedBuildings != null) return false;
        if (!map.InBounds(x, y) || map.GetGroundTile(x, y) != TileType.Signpost) return false;
        return IsWallOrDoor(map, x - 1, y) || IsWallOrDoor(map, x + 1, y);
    }

    public static bool IsBuildingTile(Map map, int x, int y)
    {
        if (map.IsIndoors || !map.InBounds(x, y)) return false;
        var t = map.GetGroundTile(x, y);
        return IsRoof(t) || t == TileType.Wall || t == TileType.Door || IsWallSign(map, x, y);
    }

    /// <summary>
    /// The buildings of a map as it was when first asked: found once and kept, which matters for the map of the
    /// whole world. Use <see cref="FindBuildings"/> after changing a map's tiles.
    /// </summary>
    public static IReadOnlyList<BuildingInfo> BuildingsOf(Map map)
    {
        lock (map) return map.BuildingCache ??= FindBuildings(map);
    }

    public static List<BuildingInfo> FindBuildings(Map map)
    {
        var result = new List<BuildingInfo>();
        if (map.IsIndoors) return result;
        // A map of the world was given its buildings by the import's models (WorldMapBuilder)
        if (map.PlacedBuildings != null) return new List<BuildingInfo>(map.PlacedBuildings);

        var seen = new bool[map.Width, map.Height];
        for (int sy = 0; sy < map.Height; sy++)
        {
            for (int sx = 0; sx < map.Width; sx++)
            {
                if (seen[sx, sy] || !IsBuildingTile(map, sx, sy)) continue;

                // Flood fill one connected block of building tiles
                int x0 = sx, x1 = sx, y0 = sy, y1 = sy;
                TileType roof = TileType.RoofRed;
                bool roofFound = false;
                var stack = new Stack<(int X, int Y)>();
                stack.Push((sx, sy));
                seen[sx, sy] = true;
                var tiles = new List<(int X, int Y)>();

                while (stack.Count > 0)
                {
                    var (x, y) = stack.Pop();
                    tiles.Add((x, y));
                    x0 = System.Math.Min(x0, x); x1 = System.Math.Max(x1, x);
                    y0 = System.Math.Min(y0, y); y1 = System.Math.Max(y1, y);
                    if (!roofFound && IsRoof(map.GetGroundTile(x, y)))
                    {
                        roof = map.GetGroundTile(x, y);
                        roofFound = true;
                    }

                    foreach (var (nx, ny) in new[] { (x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1) })
                    {
                        if (map.InBounds(nx, ny) && !seen[nx, ny] && IsBuildingTile(map, nx, ny))
                        {
                            seen[nx, ny] = true;
                            stack.Push((nx, ny));
                        }
                    }
                }

                var doors = new List<(int X, string? Target)>();
                var plaques = new List<int>();
                foreach (var (x, y) in tiles)
                {
                    if (y != y1) continue;
                    if (map.GetGroundTile(x, y) == TileType.Door) doors.Add((x, map.GetWarpAt(x, y)?.TargetMap));
                    else if (IsWallSign(map, x, y)) plaques.Add(x);
                }
                doors.Sort((a, b) => a.X.CompareTo(b.X));
                plaques.Sort();

                var kind = BuildingKind.House;
                foreach (var (_, target) in doors)
                {
                    kind = target switch
                    {
                        // Each town has its own Center and Mart ("JubilifePokemonCenter"), named after Sandgem's
                        string t when t.EndsWith("PokemonCenter") => BuildingKind.PokemonCenter,
                        string t when t.EndsWith("PokeMart") => BuildingKind.PokeMart,
                        "RowanLab" => BuildingKind.Lab,
                        _ => kind
                    };
                }

                // What the map says outright wins over what the door suggests
                foreach (var (tile, named) in map.BuildingKinds)
                {
                    if (tile.X >= x0 && tile.X <= x1 && tile.Y >= y0 && tile.Y <= y1) kind = named;
                }

                var info = new BuildingInfo { X0 = x0, Y0 = y0, X1 = x1, Y1 = y1, Kind = kind, RoofTile = roof };
                info.Doors.AddRange(doors);
                info.Plaques.AddRange(plaques);
                result.Add(info);
            }
        }
        return result;
    }

    private static bool IsWallOrDoor(Map map, int x, int y) =>
        map.InBounds(x, y) && map.GetGroundTile(x, y) is TileType.Wall or TileType.Door;
}
