using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// A named part of a large map: a town or a route of the imported world. It has the name, the music, the wild
/// Pokémon and the look that a small map has for the whole of itself.
/// </summary>
public sealed class MapArea
{
    /// <summary>The area's key in the world files: <c>twinleaf_town</c>.</summary>
    public string Key { get; init; } = "";
    public string DisplayName { get; set; } = "";
    public string BgmTrack { get; set; } = "";

    /// <summary>False for an area that is drawn as scenery but not yet built: nobody can walk into it.</summary>
    public bool Open { get; set; }

    public TreeStyle? Trees { get; set; }
    public Architecture? Architecture { get; set; }
    public BattleArena? Arena { get; set; }
    public List<string> EvolutionSites { get; } = new();
    public List<WildEncounterEntry> WildEncounters { get; } = new();
}

public class Map
{
    /// <summary>Tiles along each side of a chunk of a streamed map.</summary>
    public const int ChunkTiles = 32;

    public string Name { get; set; } = "Twinleaf Town";
    public string DisplayName { get; set; } = "Twinleaf Town";
    public string BgmTrack { get; set; } = "";
    public InteriorStyle Interior { get; set; } = InteriorStyle.None;
    public bool IsIndoors => Interior != InteriorStyle.None;

    /// <summary>True for maps with a sizeable body of water (not a garden pond): battles there have a lake behind them.</summary>
    public bool HasLake => groundLayer.Count(t => t == TileType.Water) >= 40;

    /// <summary>
    /// Whether a battle that starts at a tile has a lake behind it. A small map is judged as a whole; on a map of
    /// the imported world only the water within sight counts, and a pond is anything smaller than 64 tiles.
    /// </summary>
    public bool HasLakeNear(int x, int y)
    {
        if (!IsStreamed) return HasLake;
        const int reach = 14;
        int water = 0;
        for (int ty = Math.Max(0, y - reach); ty <= Math.Min(Height - 1, y + reach); ty++)
            for (int tx = Math.Max(0, x - reach); tx <= Math.Min(Width - 1, x + reach); tx++)
                if (groundLayer[ty * Width + tx] == TileType.Water) water++;
        return water >= 64;
    }

    public TreeStyle Trees { get; set; } = TreeStyle.Round;

    // ------------------------------------------------------------------ areas of a large map

    private MapArea?[]? areaGrid;
    private readonly List<MapArea> areas = new();

    /// <summary>See <see cref="MapStructures.BuildingsOf"/>.</summary>
    internal List<BuildingInfo>? BuildingCache;

    /// <summary>
    /// True for a map of the imported world: it is too large to draw whole, so it is drawn a chunk at a time,
    /// and its name, music and wild Pokémon change from area to area.
    /// </summary>
    public bool IsStreamed => areaGrid != null;

    public int ChunkColumns => (Width + ChunkTiles - 1) / ChunkTiles;
    public int ChunkRows => (Height + ChunkTiles - 1) / ChunkTiles;

    public IReadOnlyList<MapArea> Areas => areas;

    /// <summary>Says which area a chunk belongs to (and makes this a streamed map).</summary>
    public void SetArea(int chunkX, int chunkY, MapArea area)
    {
        areaGrid ??= new MapArea?[ChunkColumns * ChunkRows];
        if (chunkX < 0 || chunkY < 0 || chunkX >= ChunkColumns || chunkY >= ChunkRows) return;
        areaGrid[chunkY * ChunkColumns + chunkX] = area;
        if (!areas.Contains(area)) areas.Add(area);
    }

    /// <summary>The area a tile lies in; null on a small map, which is one place.</summary>
    public MapArea? AreaAt(int x, int y)
    {
        if (areaGrid == null || !InBounds(x, y)) return null;
        return areaGrid[y / ChunkTiles * ChunkColumns + x / ChunkTiles];
    }

    public MapArea? FindArea(string key) => areas.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>The tiles an area's chunks span, or null if the map has no such area.</summary>
    public (int X, int Y, int Width, int Height)? AreaBounds(string key)
    {
        if (areaGrid == null || FindArea(key) is not { } area) return null;
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
        for (int cy = 0; cy < ChunkRows; cy++)
            for (int cx = 0; cx < ChunkColumns; cx++)
            {
                if (areaGrid[cy * ChunkColumns + cx] != area) continue;
                x0 = Math.Min(x0, cx); x1 = Math.Max(x1, cx);
                y0 = Math.Min(y0, cy); y1 = Math.Max(y1, cy);
            }
        if (x1 < 0) return null;
        return (x0 * ChunkTiles, y0 * ChunkTiles, (x1 - x0 + 1) * ChunkTiles, (y1 - y0 + 1) * ChunkTiles);
    }

    /// <summary>The name of the place a tile is in: its area's, or the map's own.</summary>
    public string DisplayNameAt(int x, int y) => AreaAt(x, y)?.DisplayName is { Length: > 0 } name ? name : DisplayName;

    public string BgmTrackAt(int x, int y) => AreaAt(x, y)?.BgmTrack is { Length: > 0 } track ? track : BgmTrack;

    public TreeStyle TreesAt(int x, int y) => AreaAt(x, y)?.Trees ?? Trees;

    public Architecture ArchitectureAt(int x, int y) => AreaAt(x, y)?.Architecture ?? Architecture;

    public IReadOnlyList<string> EvolutionSitesAt(int x, int y) => AreaAt(x, y) is { } area ? area.EvolutionSites : EvolutionSites;

    /// <summary>The stage this map's battles are fought on; null lets the map decide (a room, or the ground under the player).</summary>
    public BattleArena? Arena { get; set; }

    /// <summary>For a gym or the League: the type the stage is themed on (in the League, null is the Champion's room).</summary>
    public PokemonType? ArenaType { get; set; }

    /// <summary>
    /// The special places this map has that some Pokémon evolve at when they level up there: "Moss Rock",
    /// "Ice Rock", "Magnetic Field" (the names the evolutions in species.json use).
    /// </summary>
    public List<string> EvolutionSites { get; set; } = new();

    /// <summary>
    /// The stage for a battle that starts with the player on (<paramref name="x"/>, <paramref name="y"/>):
    /// rooms are indoors unless the map names its stage; outdoors water, sand, snow and cave floors under the
    /// player win over the map's own stage, which is grass if it names none.
    /// </summary>
    public BattleArena ArenaAt(int x, int y)
    {
        if (IsIndoors) return Arena ?? BattleArena.Indoors;
        var named = AreaAt(x, y)?.Arena ?? Arena;
        if (named is BattleArena.Gym or BattleArena.League) return named.Value;
        if (InBounds(x, y))
        {
            switch (GetGroundTile(x, y))
            {
                case TileType.Water: return BattleArena.Water;
                case TileType.Sand: return BattleArena.Sand;
                case TileType.Snow: return BattleArena.Snow;
                case TileType.CaveFloor: return BattleArena.Cave;
            }
        }
        return named ?? BattleArena.Grass;
    }

    /// <summary>How the houses of this town are built.</summary>
    public Architecture Architecture { get; set; } = Architecture.Timber;

    /// <summary>
    /// What kind of building covers a tile, where the map says so. Other buildings are told by where their door
    /// leads (see <see cref="MapStructures.FindBuildings"/>).
    /// </summary>
    public Dictionary<(int X, int Y), BuildingKind> BuildingKinds { get; } = new();
    public int Width { get; }
    public int Height { get; }

    private readonly TileType[] groundLayer;
    private readonly TileType?[] overheadLayer;
    private readonly bool[] solidGrid;

    public List<NPC> NPCs { get; } = new();
    public List<Prop> Props { get; } = new();
    public List<Warp> Warps { get; } = new();
    public List<WildEncounterEntry> WildEncounters { get; } = new();
    public Dictionary<(int X, int Y), string> Signboards { get; } = new();

    private readonly Random rng = new();

    public Map(int width, int height)
    {
        Width = width;
        Height = height;
        groundLayer = new TileType[width * height];
        overheadLayer = new TileType?[width * height];
        solidGrid = new bool[width * height];

        for (int i = 0; i < groundLayer.Length; i++)
        {
            groundLayer[i] = TileType.Grass;
        }
    }

    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public TileType GetGroundTile(int x, int y) => groundLayer[y * Width + x];

    public void SetGroundTile(int x, int y, TileType type, bool isSolid = false)
    {
        if (!InBounds(x, y)) return;
        int idx = y * Width + x;
        groundLayer[idx] = type;
        solidGrid[idx] = isSolid;
    }

    public TileType? GetOverheadTile(int x, int y) => overheadLayer[y * Width + x];

    /// <summary>Whether the tile itself blocks movement (walls, water, furniture), ignoring NPCs.</summary>
    public bool IsSolid(int x, int y) => solidGrid[y * Width + x];

    public void SetOverheadTile(int x, int y, TileType type)
    {
        if (!InBounds(x, y)) return;
        overheadLayer[y * Width + x] = type;
    }

    /// <summary>Places furniture or decoration; furniture makes the tiles it covers solid.</summary>
    public Prop AddProp(PropType type, int x, int y, int width = 1, int depth = 1)
    {
        var prop = new Prop { Type = type, X = x, Y = y, Width = width, Depth = depth };
        Props.Add(prop);
        if (prop.IsSolid)
        {
            for (int ty = y; ty < y + depth; ty++)
                for (int tx = x; tx < x + width; tx++)
                    SetSolid(tx, ty, true);
        }
        return prop;
    }

    public bool IsCounter(int x, int y) => Props.Any(p => p.IsCounter && p.Covers(x, y));

    public void SetSolid(int x, int y, bool isSolid)
    {
        if (InBounds(x, y))
        {
            solidGrid[y * Width + x] = isSolid;
        }
    }

    public bool IsWalkable(int x, int y, bool isLedgeLanding = false)
    {
        if (!InBounds(x, y)) return false;
        int idx = y * Width + x;

        if (solidGrid[idx]) return false;

        if (NPCs.Any(n => n.GridX == x && n.GridY == y)) return false;

        if (groundLayer[idx] == TileType.LedgeDown && !isLedgeLanding) return false;

        return true;
    }

    public bool IsLedge(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        return groundLayer[y * Width + x] == TileType.LedgeDown;
    }

    public bool IsTallGrass(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        return groundLayer[y * Width + x] == TileType.TallGrass;
    }

    public Warp? GetWarpAt(int x, int y)
    {
        return Warps.FirstOrDefault(w => w.SourceX == x && w.SourceY == y);
    }

    public string? GetSignboardAt(int x, int y)
    {
        return Signboards.TryGetValue((x, y), out var text) ? text : null;
    }

    public NPC? GetNpcAt(int x, int y)
    {
        return NPCs.FirstOrDefault(n => n.GridX == x && n.GridY == y);
    }

    public WildEncounterEntry? RollWildEncounter() => Roll(WildEncounters);

    /// <summary>A wild Pokémon for a step onto a tile: from its area's table on a large map, the map's own otherwise.</summary>
    public WildEncounterEntry? RollWildEncounter(int x, int y) => Roll(AreaAt(x, y)?.WildEncounters ?? WildEncounters);

    private WildEncounterEntry? Roll(List<WildEncounterEntry> table)
    {
        if (table.Count == 0) return null;
        if (rng.Next(100) < 18)
        {
            int totalWeight = table.Sum(e => e.Weight);
            int roll = rng.Next(totalWeight);
            int curr = 0;
            foreach (var e in table)
            {
                curr += e.Weight;
                if (roll < curr) return e;
            }
            return table.First();
        }
        return null;
    }
}
