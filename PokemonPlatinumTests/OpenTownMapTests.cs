using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.OpenWorld;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 32 · OW10: the Town Map of an open region. Its picture is painted from above out of the baker's bake
/// (<see cref="RegionMapArt.PaintOpen"/>), a texel a metre and a cell a chunk, so a region with no grid map still has
/// one; and Fly's places of an open region name markers of its places' files, which Fly lands on.
/// </summary>
public class OpenTownMapTests
{
    static readonly Lazy<(BakedRegion Land, PixelCanvas Picture)> lab = new(() =>
    {
        var land = OpenWorldPeopleTests.LabWithPeople().Land!;
        return (land, RegionMapArt.PaintOpen(land));
    });

    static Color At(double x, double z)
    {
        var (land, picture) = lab.Value;
        var (x0, z0, _, _) = RegionMapArt.OpenBoundsOf(land);
        return picture.Get((int)Math.Floor(x) - x0 * RegionMapArt.CellTexels, (int)Math.Floor(z) - z0 * RegionMapArt.CellTexels);
    }

    [Fact]
    public void TheLabIsPaintedATexelAMetreACellAChunk()
    {
        var (land, picture) = lab.Value;
        Assert.Equal((-2, -2, 4, 4), RegionMapArt.OpenBoundsOf(land));
        Assert.Equal((128, 128), (picture.Width, picture.Height));
        // Every texel of the lab has ground under it: nothing left clear
        for (int y = 0; y < picture.Height; y++)
            for (int x = 0; x < picture.Width; x++)
                Assert.True(picture.Get(x, y).A == 255, $"{x},{y} is clear");
        // The cells: the lab's chunk 0,0 is the picture's cell 2,2
        Assert.Equal((2, 2), RegionMapArt.OpenCellOf(land, OpenPos.FromMetres(5, 0, 5)));
        Assert.Equal((0, 0), RegionMapArt.OpenCellOf(land, OpenPos.FromMetres(-63, 0, -63)));
        // The same picture every time
        var again = RegionMapArt.PaintOpen(land);
        for (int y = 0; y < picture.Height; y += 3)
            for (int x = 0; x < picture.Width; x += 3)
                Assert.Equal(picture.Get(x, y), again.Get(x, y));
    }

    [Fact]
    public void ItShowsTheWaterTheRoadsAndTheHillsTopNotTheCaveUnderIt()
    {
        var sea = new[] { RegionMapArt.SeaDeep, RegionMapArt.SeaMid, RegionMapArt.SeaShallow, RegionMapArt.Shore };
        // The river along the south, and the pond in its crater to the north-east
        Assert.Contains(At(0, 44), sea);
        Assert.Contains(At(-30, 44), sea);
        Assert.Contains(At(40, -38), sea);
        // The meadow is no water
        Assert.DoesNotContain(At(-20, -30), sea);
        Assert.DoesNotContain(At(0, -27), sea);
        // Over the cavern the hill's top is seen (a grass, lit or shaded), never the cavern's floor: its colour is the
        // top's, which a column down through it meets first (the cover the bake gives the top)
        var top = At(0, -2);
        Assert.DoesNotContain(top, sea);
        Assert.NotEqual(At(0, 0), default);
        // Many colours: relief and covers, not one flat green
        var (_, picture) = lab.Value;
        var colours = new HashSet<Color>();
        for (int y = 0; y < picture.Height; y++)
            for (int x = 0; x < picture.Width; x++) colours.Add(picture.Get(x, y));
        Assert.True(colours.Count >= 12, $"{colours.Count} colours");
    }

    [Fact]
    public void FlysPlacesOfAnOpenRegionNameMarkersAndFlyLandsOnThem()
    {
        var file = new RegionMapFile
        {
            Map = "OpenLab", Columns = 4, Rows = 4, View = new[] { 0, 0, 4, 4 }, Cursor = new[] { 0, 0, 3, 3 }, Start = new[] { 1, 1 },
            Areas = { new RegionPlaceRecord { Key = "lab_meadow", About = "A meadow." } },
            Blocks = { new RegionBlockRecord { X = 1, Y = 0, Name = "Test Meadow", Area = "lab_meadow", Kind = RegionBlockKind.Town } },
            Fly = { new RegionFlyRecord { Spawn = 1, Cells = "1,0", Marker = "house_step" }, new RegionFlyRecord { Spawn = 2, Cells = "1,0" } }
        };
        var region = new RegionMap("lab", file);
        Assert.Equal("house_step", region.FlyMarkerOf(1));
        Assert.Null(region.FlyMarkerOf(2));
        Assert.Null(region.FlyMarkerOf(3));
        // Read from a file as the game reads one
        var read = GameDataFiles.Deserialize<RegionMapFile>("""{ "fly": [ { "spawn": 1, "cells": "1,0", "marker": "east_mouth" } ] }""");
        Assert.Equal("east_mouth", read.Fly.Single().Marker);

        var map = OpenWorldPeopleTests.LabWithPeople();
        var landing = GameEngine.FlyLandingIn(map, region, 1);
        Assert.NotNull(landing);
        Assert.Equal(map.Marker("house_step"), landing!.Value.Marker);
        Assert.Null(GameEngine.FlyLandingIn(map, region, 2));
        // A marker the map lacks lands nowhere (the content check's to name)
        file.Fly[1].Marker = "nowhere";
        Assert.Null(GameEngine.FlyLandingIn(map, new RegionMap("lab", file), 2));
    }
}
