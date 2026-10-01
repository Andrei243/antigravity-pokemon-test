using System.Collections.Generic;

namespace PokemonPlatinumEngine.Overworld;

public enum BuildingKind
{
    House,
    PokemonCenter,
    PokeMart,
    Lab
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

    /// <summary>Doors on the front (bottom) row, with the map each one warps to (null for decorative doors).</summary>
    public List<(int X, string? Target)> Doors { get; } = new();

    /// <summary>Wall-mounted signs on the front row.</summary>
    public List<int> Plaques { get; } = new();

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
        if (!map.InBounds(x, y) || map.GetGroundTile(x, y) != TileType.Signpost) return false;
        return IsWallOrDoor(map, x - 1, y) || IsWallOrDoor(map, x + 1, y);
    }

    public static bool IsBuildingTile(Map map, int x, int y)
    {
        if (map.IsIndoors || !map.InBounds(x, y)) return false;
        var t = map.GetGroundTile(x, y);
        return IsRoof(t) || t == TileType.Wall || t == TileType.Door || IsWallSign(map, x, y);
    }

    public static List<BuildingInfo> FindBuildings(Map map)
    {
        var result = new List<BuildingInfo>();
        if (map.IsIndoors) return result;

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
