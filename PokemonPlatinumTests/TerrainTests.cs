using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>Terrain and nature (plan 04 · G4): the water mask, the plants' pixel art and the new ground kinds.</summary>
public class TerrainTests
{
    private const int T = GroundBaker.ArtTile;

    private static (PixelCanvas Ground, PixelCanvas? Water) Bake(Map map) =>
        (PixelGround.Bake(map, MapScene.OutdoorMargin, MapStructures.FindBuildings(map), out var water), water);

    private static double IsolatedShare(PixelCanvas c, bool wrap)
    {
        int isolated = 0, total = 0;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                if (!wrap && (x == 0 || y == 0 || x == c.Width - 1 || y == c.Height - 1)) continue;
                total++;
                var col = c.Get(x, y);
                bool alone = true;
                for (int dy = -1; dy <= 1 && alone; dy++)
                    for (int dx = -1; dx <= 1 && alone; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = (x + dx + c.Width) % c.Width, ny = (y + dy + c.Height) % c.Height;
                        if (c.Get(nx, ny).Equals(col)) alone = false;
                    }
                if (alone) isolated++;
            }
        return isolated / (double)total;
    }

    private static int Shades(PixelCanvas c)
    {
        var seen = new HashSet<(byte, byte, byte)>();
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
                if (c.IsOpaque(x, y)) seen.Add((c.Get(x, y).R, c.Get(x, y).G, c.Get(x, y).B));
        return seen.Count;
    }

    // ------------------------------------------------------------------ water

    [Fact]
    public void ShoreDistanceCountsTexelsToTheNearestShore()
    {
        // A 9x9 pool in a 15x15 field
        const int n = 15;
        var surface = new bool[n * n];
        for (int y = 3; y < 12; y++)
            for (int x = 3; x < 12; x++)
                surface[y * n + x] = true;
        var d = PixelGround.ShoreDistance(surface, n, n);

        Assert.Equal(0f, d[0]);
        Assert.Equal(1f, d[3 * n + 7], 3);   // on the edge
        Assert.Equal(5f, d[7 * n + 7], 3);   // the middle is five texels in
        Assert.Equal(1f, d[3 * n + 3], 3);   // a corner texel still touches the shore
        Assert.InRange(d[4 * n + 4], 1.9f, 2.1f);
    }

    [Fact]
    public void MapsWithoutWaterHaveNoWaterMask()
    {
        MapDatabase.Initialize();
        Assert.Null(Bake(MapDatabase.Get("Route201")).Water);
        Assert.NotNull(Bake(MapDatabase.Get("TwinleafTown")).Water);
    }

    [Fact]
    public void TheLakesMaskHoldsTheShoreDistanceAndLeavesTheNorthBankDry()
    {
        MapDatabase.Initialize();
        var map = MapDatabase.Get("LakeVerity");
        var (_, mask) = Bake(map);
        Assert.NotNull(mask);
        int margin = MapScene.OutdoorMargin;

        // Lake Verity's water covers tiles x 2..22, y 2..8; look down the middle column of tile 12
        int x = (12 + margin) * T + T / 2;
        int top = (2 + margin) * T;
        for (int row = 0; row < PixelGround.BankDepth; row++)
            Assert.False(mask!.IsOpaque(x, top + row), $"bank row {row} should not be water");
        Assert.True(mask!.IsOpaque(x, top + PixelGround.BankDepth));

        // Just under the bank the shore is a texel away; the middle of the lake is deep
        Assert.InRange(mask.Get(x, top + PixelGround.BankDepth).R, 1 * PixelGround.MaskScale, 2 * PixelGround.MaskScale);
        int middle = (5 + margin) * T + T / 2;
        Assert.True(mask.Get(x, middle).R >= 40 * PixelGround.MaskScale, "the middle of the lake should be far from any shore");

        // Land is never water
        Assert.False(mask.IsOpaque((24 + margin) * T + T / 2, middle));
    }

    [Fact]
    public void BouldersInWaterStandOnADryPatchAndBlockTheWay()
    {
        MapDatabase.Initialize();
        var map = MapDatabase.Get("LakeVerity");
        var boulders = map.Props.Where(p => p.Type == PropType.Boulder).ToList();
        Assert.NotEmpty(boulders);

        var (_, mask) = Bake(map);
        int margin = MapScene.OutdoorMargin;
        foreach (var rock in boulders)
        {
            Assert.False(map.IsWalkable(rock.X, rock.Y), $"the boulder at {rock.X},{rock.Y} should be solid");
            if (map.GetGroundTile(rock.X, rock.Y) != TileType.Water) continue;

            int cx = (rock.X + margin) * T + T / 2, cy = (rock.Y + margin) * T + T / 2;
            Assert.False(mask!.IsOpaque(cx, cy));
            // Water starts again past the footprint, a texel or two from this new shore
            int edge = cx + PixelGround.RockFootprint + 1;
            Assert.True(mask.IsOpaque(edge, cy));
            Assert.InRange(mask.Get(edge, cy).R, PixelGround.MaskScale, 3 * PixelGround.MaskScale);
        }
    }

    [Fact]
    public void OnlyMapsWithALakeGetALakesideBattle()
    {
        MapDatabase.Initialize();
        Assert.True(MapDatabase.Get("LakeVerity").HasLake);
        Assert.False(MapDatabase.Get("TwinleafTown").HasLake);   // a garden pond
        Assert.False(MapDatabase.Get("Route201").HasLake);
    }

    // ------------------------------------------------------------------ plants and rocks

    [Fact]
    public void TreeAndRockTexturesAreFlatShadesWithoutNoise()
    {
        var textures = new (string Name, PixelCanvas Art, int MaxShades)[]
        {
            ("needles", NatureArt.Needles(), 4),
            ("leaves", NatureArt.Leaves(cutout: false), 3),
            ("leaf shell", NatureArt.Leaves(cutout: true), 3),
            ("bark", NatureArt.Bark(), 3),
            ("rock", NatureArt.Rock(), 4),
            ("ledge", NatureArt.LedgeFace(), 6)
        };
        foreach (var (name, art, maxShades) in textures)
        {
            Assert.True(Shades(art) <= maxShades, $"{name}: {Shades(art)} shades, at most {maxShades} expected");
            double share = IsolatedShare(art, wrap: true);
            Assert.True(share < 0.01, $"{name}: {share:P2} of texels stand alone");
        }
    }

    [Fact]
    public void GrassAndFlowersAreDrawnInAFewShades()
    {
        Assert.True(Shades(NatureArt.TallGrass()) <= 4);
        Assert.True(Shades(NatureArt.LawnTuft()) <= 4);

        var tall = NatureArt.TallGrass();
        Assert.Equal((32, 16), (tall.Width, tall.Height));
        // Each of the two clumps reaches the top of the texture and stands on its bottom row
        foreach (int clump in new[] { 0, 16 })
        {
            Assert.Contains(Enumerable.Range(clump, 16), x => tall.IsOpaque(x, 0) || tall.IsOpaque(x, 1));
            Assert.Contains(Enumerable.Range(clump, 16), x => tall.IsOpaque(x, 15));
        }

        // Three flower frames, each with a blossom of its own colour
        var flowers = NatureArt.Flowers();
        Assert.Equal((48, 16), (flowers.Width, flowers.Height));
        var blossoms = Enumerable.Range(0, 3).Select(k => flowers.Get(k * 16 + 8, 3)).ToList();
        Assert.Equal(3, blossoms.Distinct().Count());
    }

    // ------------------------------------------------------------------ ground kinds

    [Theory]
    [InlineData(TileType.Sand, 238, 224, 172, 246, 236, 196)]
    [InlineData(TileType.Dirt, 176, 136, 96, 194, 156, 112)]
    [InlineData(TileType.Snow, 204, 218, 240, 220, 230, 246)]
    [InlineData(TileType.CaveFloor, 112, 100, 104, 130, 118, 120)]
    public void EachGroundKindIsItsBaseAndPatchColoursWithCleanMarks(TileType kind, int r, int g, int b, int pr, int pg, int pb)
    {
        var map = new Map(12, 10) { Name = "Sample" + kind };
        for (int y = 2; y < 8; y++)
            for (int x = 2; x < 10; x++)
                map.SetGroundTile(x, y, kind);
        var ground = PixelGround.Bake(map, MapScene.OutdoorMargin, MapStructures.FindBuildings(map));

        // Count the colours inside the patch, away from its rim
        int margin = MapScene.OutdoorMargin;
        var counts = new Dictionary<(byte, byte, byte), int>();
        for (int y = (3 + margin) * T; y < (7 + margin) * T; y++)
            for (int x = (3 + margin) * T; x < (9 + margin) * T; x++)
            {
                var c = ground.Get(x, y);
                counts[(c.R, c.G, c.B)] = counts.GetValueOrDefault((c.R, c.G, c.B)) + 1;
            }
        // Nearly all of it is the flat base and its lighter patches; the rest is marks
        int flat = counts.GetValueOrDefault(((byte)r, (byte)g, (byte)b)) + counts.GetValueOrDefault(((byte)pr, (byte)pg, (byte)pb));
        Assert.True(counts.ContainsKey(((byte)r, (byte)g, (byte)b)), $"{kind}: the base colour is missing");
        Assert.True(flat > 0.9 * counts.Values.Sum(), $"{kind}: only {flat} of {counts.Values.Sum()} texels are base or patch");
        Assert.True(counts.Count <= 5, $"{kind}: {counts.Count} colours in the patch");
        Assert.True(IsolatedShare(ground, wrap: false) < 0.015);
    }

    [Fact]
    public void EveryTileTypeHasAMapFileCode()
    {
        var codes = new HashSet<char>();
        foreach (var type in Enum.GetValues<TileType>())
        {
            char code = TileCodes.CodeOf(type);
            Assert.True(codes.Add(code), $"{type} shares the code '{code}' with another tile type");
            Assert.Equal(type, TileCodes.Parse(code, "test"));
        }
    }
}
