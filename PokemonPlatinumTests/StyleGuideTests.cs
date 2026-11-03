using System;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumTests;

/// <summary>Rules from docs/art/style-guide.md that can be checked without a GPU.</summary>
// MapDatabase is static, so the classes that rebuild it must not run in parallel
[Collection("MapDatabase")]
public class StyleGuideTests
{
    [Theory]
    [InlineData("Nunito-Bold.ttf")]
    [InlineData("Nunito-ExtraBold.ttf")]
    [InlineData("Nunito-Black.ttf")]
    [InlineData("OFL.txt")]
    [InlineData("NotoSansCJK-Bold-Subset.ttf")]
    [InlineData("OFL-NotoSansCJK.txt")]
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
    [InlineData("JubilifeCity")]
    public void TheFieldGroundIsCleanPixelArt(string mapName)
    {
        MapDatabase.Initialize();
        var map = Fixtures.Any(mapName);
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

    /// <summary>
    /// Plan 12 · Q8 (style guide, "Shapes and components", "Seeing"): the text tokens keep their contrast on their panels
    /// as an eye of each kind sees them once the remap for it has drawn them (Machado's simulation of the remapped colour).
    /// </summary>
    [Theory]
    [InlineData(PokemonPlatinumEngine.Core.ColourVision.Normal)]
    [InlineData(PokemonPlatinumEngine.Core.ColourVision.Protan)]
    [InlineData(PokemonPlatinumEngine.Core.ColourVision.Deutan)]
    [InlineData(PokemonPlatinumEngine.Core.ColourVision.Tritan)]
    public void EveryTextTokenKeepsItsContrastOnItsPanelUnderEachRemap(PokemonPlatinumEngine.Core.ColourVision vision)
    {
        Raylib_cs.Color Seen(Raylib_cs.Color c) =>
            PokemonPlatinumEngine.Core.Seeing.Simulate(PokemonPlatinumEngine.Core.Seeing.Daltonise(c, vision), vision);
        float On(Raylib_cs.Color ink, Raylib_cs.Color panel) => PokemonPlatinumEngine.Core.Seeing.Contrast(Seen(ink), Seen(panel));

        var t = PokemonPlatinumEngine.UI.ModernUi.TextTokens;
        var pairs = new (string Name, Raylib_cs.Color Ink, Raylib_cs.Color Panel, float Least)[]
        {
            ("Ink on the panel's top", t.Ink, t.PanelTop, 4.5f), ("Ink on the panel's foot", t.Ink, t.PanelBottom, 4.5f),
            ("Frame on the panel's top", t.Frame, t.PanelTop, 4.5f), ("Frame on the panel's foot", t.Frame, t.PanelBottom, 4.5f),
            ("white on the high-contrast box", Raylib_cs.Color.White, t.ContrastBack, 4.5f),
            ("the red run on the high-contrast box", t.ContrastRed, t.ContrastBack, 4.5f),
            ("the blue run on the high-contrast box", t.ContrastBlue, t.ContrastBack, 4.5f),
            ("the money run on the high-contrast box", t.ContrastMoney, t.ContrastBack, 4.5f),
            ("Muted on the panel's top", t.Muted, t.PanelTop, 3f), ("Muted on the panel's foot", t.Muted, t.PanelBottom, 3f),
            ("the red run on the panel", t.RunRed, t.PanelBottom, 3f), ("the blue run on the panel", t.RunBlue, t.PanelBottom, 3f),
            ("the money run on the panel", t.RunMoney, t.PanelBottom, 3f)
        };
        foreach (var (name, ink, panel, least) in pairs)
            Assert.True(On(ink, panel) >= least, $"{vision}: {name} is {On(ink, panel):0.00} : 1, under {least}");
    }
}
