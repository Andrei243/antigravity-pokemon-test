using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

public class Map
{
    public string Name { get; set; } = "Twinleaf Town";
    public string DisplayName { get; set; } = "Twinleaf Town";
    public string BgmTrack { get; set; } = "Twinleaf";
    public InteriorStyle Interior { get; set; } = InteriorStyle.None;
    public bool IsIndoors => Interior != InteriorStyle.None;
    public TreeStyle Trees { get; set; } = TreeStyle.Round;
    public int Width { get; }
    public int Height { get; }

    private readonly TileType[] groundLayer;
    private readonly TileType?[] overheadLayer;
    private readonly bool[] solidGrid;

    public List<NPC> NPCs { get; } = new();
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

    public void SetOverheadTile(int x, int y, TileType type)
    {
        if (!InBounds(x, y)) return;
        overheadLayer[y * Width + x] = type;
    }

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

    public WildEncounterEntry? RollWildEncounter()
    {
        if (WildEncounters.Count == 0) return null;
        if (rng.Next(100) < 18)
        {
            int totalWeight = WildEncounters.Sum(e => e.Weight);
            int roll = rng.Next(totalWeight);
            int curr = 0;
            foreach (var e in WildEncounters)
            {
                curr += e.Weight;
                if (roll < curr) return e;
            }
            return WildEncounters.First();
        }
        return null;
    }
}
