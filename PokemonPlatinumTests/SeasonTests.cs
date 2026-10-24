using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI;
using Raylib_cs;

namespace PokemonPlatinumTests;

/// <summary>
/// Unova's seasons, the clock, the look and the sign (plan 22 · U3; style guide, "Seasons"): Black 2's month rule, the
/// options' override, the palettes held to the style guide, spring as the field's own look texel for texel, a chunk
/// of Unova the same under one season twice and another under two, only Unova's maps seasonal, the options' row and
/// the sign's second line. GPU-free.
/// </summary>
[Collection("Shared clocks and dice")]
public class SeasonTests
{
    private static readonly Lazy<Map> Unova = new(() =>
        World.LoadAll().Single(w => w.Index.Region == "Unova").BuildMaps().Single(m => m.Name == "Unova"));

    [Theory]
    [InlineData(1, Season.Spring)]
    [InlineData(2, Season.Summer)]
    [InlineData(3, Season.Autumn)]
    [InlineData(4, Season.Winter)]
    [InlineData(5, Season.Spring)]
    [InlineData(6, Season.Summer)]
    [InlineData(7, Season.Autumn)]
    [InlineData(8, Season.Winter)]
    [InlineData(9, Season.Spring)]
    [InlineData(10, Season.Summer)]
    [InlineData(11, Season.Autumn)]
    [InlineData(12, Season.Winter)]
    public void ASeasonIsAMonthRoundAndRound(int month, Season season)
    {
        Assert.Equal(season, GameClock.SeasonOf(new DateTime(2026, month, 15)));
        Assert.Equal(season, GameClock.SeasonOf(new DateTime(2011, month, 1)));
    }

    [Fact]
    public void TheCalendarGivesTheSeasonUnlessTheOptionsFixOne()
    {
        var date = GameClock.FixedDate;
        var fixedSeason = GameClock.FixedSeason;
        try
        {
            GameClock.FixedDate = new DateTime(2026, 4, 2);
            GameClock.FixedSeason = null;
            Assert.Equal(Season.Winter, GameClock.Season);
            GameClock.FixedSeason = Season.Autumn;
            Assert.Equal(Season.Autumn, GameClock.Season);
        }
        finally
        {
            GameClock.FixedDate = date;
            GameClock.FixedSeason = fixedSeason;
        }
    }

    [Fact]
    public void OnlyUnovasMapsHaveSeasons()
    {
        foreach (var region in RegionDatabase.All)
            foreach (var map in region.Maps.Take(5))
                Assert.Equal(region.Id == RegionDatabase.Unova, RegionDatabase.RegionOfMap(map)?.HasSeasons == true);
    }

    [Fact]
    public void NoSeasonAndSpringAreTheFieldsOwnLook()
    {
        Assert.Same(SeasonPalette.Default, SeasonPalette.For(null));
        Assert.Same(SeasonPalette.Default, SeasonPalette.For(Season.Spring));
        // The numbers the field had before seasons (style guide, "Ground" and "Grass, flowers, trees, ledges and rocks")
        var d = SeasonPalette.Default;
        Assert.Equal((104, 190, 98), Rgb(d.Grass));
        Assert.Equal((120, 200, 102), Rgb(d.GrassLight));
        Assert.Equal((58, 126, 82), Rgb(d.Forest));
        Assert.Equal((48, 124, 70), Rgb(d.TallGround));
        Assert.Equal((70, 150, 82), Rgb(d.Tufts[0]));
        Assert.Equal((160, 222, 122), Rgb(d.Tufts[3]));
        Assert.Equal((36, 110, 62), Rgb(d.TallGrass[0]));
        Assert.Equal((170, 232, 130), Rgb(d.TallGrass[3]));
        Assert.False(d.Snow);
        // And the sheets of a season that is the field's own are the field's own
        Assert.Equal("tall_grass", SeasonPalette.SheetKey("tall_grass", null));
        Assert.Equal("tall_grass", SeasonPalette.SheetKey("tall_grass", Season.Spring));
        Assert.Equal("tall_grass_winter", SeasonPalette.SheetKey("tall_grass", Season.Winter));
        Same(NatureArt.TallGrass(), NatureArt.TallGrass(SeasonPalette.Default));
        Same(NatureArt.LawnTuft(), NatureArt.LawnTuft(SeasonPalette.Default));
        Same(NatureArt.Flowers(), NatureArt.Flowers(SeasonPalette.Default));
    }

    [Fact]
    public void ThePalettesAreTheStyleGuides()
    {
        Assert.Equal((86, 176, 84), Rgb(SeasonPalette.Summer.Grass));
        Assert.Equal((102, 188, 90), Rgb(SeasonPalette.Summer.GrassLight));
        Assert.Equal((176, 170, 92), Rgb(SeasonPalette.Autumn.Grass));
        Assert.Equal((192, 182, 104), Rgb(SeasonPalette.Autumn.GrassLight));
        Assert.Equal((214, 120, 58), Rgb(SeasonPalette.Autumn.RoundFrom));
        Assert.Equal((226, 164, 70), Rgb(SeasonPalette.Autumn.RoundTo));
        // Winter's lawn is the ground's own snow, which stays well below white
        Assert.Equal((186, 204, 232), Rgb(SeasonPalette.Winter.Grass));
        Assert.Equal((196, 212, 238), Rgb(SeasonPalette.Winter.GrassLight));
        Assert.True(SeasonPalette.Winter.Snow);
        foreach (var season in Enum.GetValues<Season>())
        {
            var p = SeasonPalette.For(season);
            Assert.Equal(4, p.Tufts.Length);
            Assert.Equal(4, p.TallGrass.Length);
            Assert.Equal(3, p.Flowers.Length);
            foreach (var c in new[] { p.Grass, p.GrassLight, p.Forest, p.TallGround })
                Assert.True(c.R + c.G + c.B < 3 * 240, $"{season}'s ground is too near white");
        }
    }

    [Fact]
    public void AnUnovaChunkIsTheSameUnderOneSeasonTwiceAndAnotherUnderTwo()
    {
        var map = Unova.Value;
        // Route 19's chunk, with its lawn, its tall grass and its trees
        var window = MapScene.ChunkWindow(map, 73 / Map.ChunkTiles, 693 / Map.ChunkTiles);
        var none = PixelGround.Bake(map, window, PixelGround.ChunkPad, worldSeeds: true, out _);
        var spring = PixelGround.Bake(map, window, PixelGround.ChunkPad, worldSeeds: true, Season.Spring, out _);
        var summer = PixelGround.Bake(map, window, PixelGround.ChunkPad, worldSeeds: true, Season.Summer, out _);
        var summerAgain = PixelGround.Bake(map, window, PixelGround.ChunkPad, worldSeeds: true, Season.Summer, out _);
        var winter = PixelGround.Bake(map, window, PixelGround.ChunkPad, worldSeeds: true, Season.Winter, out _);
        Same(none, spring);
        Same(summer, summerAgain);
        Assert.True(Differing(summer, winter) > 1000, "winter's snow should lie on Route 19's lawn");
        Assert.True(Differing(none, summer) > 1000, "summer's lawn should be its own");
    }

    [Fact]
    public void TheOptionsRowGoesRoundTheCalendarAndTheFourSeasons()
    {
        var s = new GameSettings();
        Assert.Null(s.Season);
        var seen = new List<Season?>();
        for (int i = 0; i < 5; i++)
        {
            OptionsScreen.Change(s, OptionRow.Season, 1);
            seen.Add(s.Season);
        }
        Assert.Equal(new Season?[] { Season.Spring, Season.Summer, Season.Autumn, Season.Winter, null }, seen);
        OptionsScreen.Change(s, OptionRow.Season, -1);
        Assert.Equal(Season.Winter, s.Season);
        // A new row stands before the pages at the end
        var rows = Enum.GetValues<OptionRow>();
        Assert.True(Array.IndexOf(rows, OptionRow.Season) < Array.IndexOf(rows, OptionRow.Controls));
    }

    [Fact]
    public void TheSignTellsTheSeasonOnItsSecondLineAndNothingElsewhere()
    {
        var sign = new LocationSign();
        sign.Show("Route 19", Season.Winter);
        Assert.Equal("Winter", sign.SecondLine);
        sign.Show("Twinleaf Town");
        Assert.Null(sign.SecondLine);
        Assert.Equal("Spring has come to Unova.", GameEngine.SeasonArrived(Season.Spring, "Unova"));
    }

    [Fact]
    public void ASeasonsLightIsOnlyUnovas()
    {
        var rig = ArtLook.FieldRig(13f, Unova.Value);
        Assert.Equal(rig, ArtLook.Seasoned(rig, null));
        Assert.Equal(rig, ArtLook.Seasoned(rig, Season.Spring));
        Assert.NotEqual(rig, ArtLook.Seasoned(rig, Season.Winter));
    }

    private static (int, int, int) Rgb(Color c) => (c.R, c.G, c.B);

    private static int Differing(PixelCanvas a, PixelCanvas b)
    {
        int n = 0;
        for (int y = 0; y < a.Height; y++)
            for (int x = 0; x < a.Width; x++)
            {
                Color p = a.Get(x, y), q = b.Get(x, y);
                if (p.R != q.R || p.G != q.G || p.B != q.B || p.A != q.A) n++;
            }
        return n;
    }

    private static void Same(PixelCanvas a, PixelCanvas b)
    {
        Assert.Equal((a.Width, a.Height), (b.Width, b.Height));
        Assert.Equal(0, Differing(a, b));
    }
}
