using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>Rules from docs/art/style-guide.md that can be checked without a GPU.</summary>
public class StyleGuideTests
{
    [Theory]
    [InlineData("Nunito-Bold.ttf")]
    [InlineData("Nunito-ExtraBold.ttf")]
    [InlineData("Nunito-Black.ttf")]
    [InlineData("OFL.txt")]
    public void TheInterfaceFontShipsWithTheBuild(string file)
    {
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", file)), $"{file} is not copied next to the executable");
    }

    /// <summary>
    /// "No single-pixel noise": in the HD-2D field ground almost every texel shares its colour with a neighbour.
    /// Only deliberate one-texel accents (tuft tips, pebble highlights, flower centres) stand alone.
    /// </summary>
    [Theory]
    [InlineData("TwinleafTown")]
    [InlineData("Route201")]
    [InlineData("SandgemTown")]
    [InlineData("LakeVerity")]
    public void TheFieldGroundIsCleanPixelArt(string mapName)
    {
        MapDatabase.Initialize();
        var map = MapDatabase.Get(mapName);
        var ground = PixelGround.Bake(map, MapScene.OutdoorMargin, MapStructures.FindBuildings(map));

        int isolated = 0, total = 0;
        for (int y = 1; y < ground.Height - 1; y++)
            for (int x = 1; x < ground.Width - 1; x++)
            {
                total++;
                var c = ground.Get(x, y);
                bool alone = true;
                for (int dy = -1; dy <= 1 && alone; dy++)
                    for (int dx = -1; dx <= 1 && alone; dx++)
                        if ((dx != 0 || dy != 0) && ground.Get(x + dx, y + dy).Equals(c)) alone = false;
                if (alone) isolated++;
            }

        double share = isolated / (double)total;
        Assert.True(share < 0.015, $"{mapName}: {share:P2} of ground texels are isolated specks");
    }
}
