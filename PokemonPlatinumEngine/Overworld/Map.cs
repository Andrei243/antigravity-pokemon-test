using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;

namespace PokemonPlatinumEngine.Overworld;

public class Map
{
    public string Name { get; set; } = "Twinleaf Town";
    public string DisplayName { get; set; } = "Twinleaf Town";
    public string BgmTrack { get; set; } = "Twinleaf";
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

    public void DrawGroundAndEntities(int cameraX, int cameraY, Player player)
    {
        var tileset = PixelArtGenerator.GetTilesetTexture();
        int tile = Player.TileSize; // 32px

        // Ground layer
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int px = x * tile - cameraX;
                int py = y * tile - cameraY;

                TileType t = groundLayer[y * Width + x];
                Rectangle src = GetTileSourceRect(t);
                Raylib.DrawTextureRec(tileset, src, new Vector2(px, py), Color.White);
            }
        }

        // NPCs
        foreach (var npc in NPCs.OrderBy(n => n.GridY))
        {
            int nx = npc.GridX * tile - cameraX;
            int ny = npc.GridY * tile - cameraY;

            if (npc.IsStarterBriefcase)
            {
                // Draw Starter Briefcase HD
                Raylib.DrawRectangle(nx + 2, ny + 6, 28, 20, new Color(160, 112, 64, 255));
                Raylib.DrawRectangle(nx + 6, ny + 2, 20, 4, new Color(112, 72, 40, 255));
                Raylib.DrawCircle(nx + 16, ny + 16, 5, Color.Gold);
            }
            else
            {
                var npcTex = PixelArtGenerator.GetNpcSprite(npc.NpcType, npc.Facing);
                Raylib.DrawTexture(npcTex, nx - 8, ny - 14, Color.White);
            }

            if (npc.HasSpottedPlayer && npc.ExclamationTimer > 0f)
            {
                Raylib.DrawCircle(nx + 16, ny - 24, 12, Color.White);
                Raylib.DrawCircle(nx + 16, ny - 24, 9, Palette.UiAccent);
                Raylib.DrawText("!", nx + 13, ny - 30, 16, Color.White);
            }
        }

        // Player
        player.Draw(cameraX, cameraY);
    }

    public void DrawOverhead(int cameraX, int cameraY)
    {
        var tileset = PixelArtGenerator.GetTilesetTexture();
        int tile = Player.TileSize;

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int px = x * tile - cameraX;
                int py = y * tile - cameraY;

                var t = overheadLayer[y * Width + x];
                if (t.HasValue)
                {
                    Rectangle src = GetTileSourceRect(t.Value);
                    Raylib.DrawTextureRec(tileset, src, new Vector2(px, py), Color.White);
                }
            }
        }
    }

    private static Rectangle GetTileSourceRect(TileType t)
    {
        int tile = 32;
        int col = t switch
        {
            TileType.Grass => 0,
            TileType.FlowerGrass => 1,
            TileType.TallGrass => 2,
            TileType.Path => 3,
            TileType.Water => 4,
            TileType.LedgeDown => 5,
            TileType.Tree => 6,
            TileType.TreeTrunk => 7,
            TileType.RoofRed => 8,
            TileType.RoofBlue => 9,
            TileType.Wall => 10,
            TileType.Door => 11,
            TileType.Floor => 12,
            TileType.Signpost => 13,
            TileType.PC => 14,
            _ => 0
        };
        return new Rectangle(col * tile, 0, tile, tile);
    }
}
