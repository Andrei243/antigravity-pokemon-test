using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Other shapes of screen (rulings, "Other shapes of screen, and the resolution"): the canvas a window gets, the safe
/// area in it, the camera's field of view, the mouse's mapping and the RESOLUTION row's choices.
/// </summary>
public class AspectLayoutTests
{
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(3840, 2160)]
    [InlineData(1280, 720)]
    [InlineData(1920, 1081)] // the full-screen window, one row taller than a 16:9 monitor
    [InlineData(2560, 1441)]
    [InlineData(1366, 768)]
    [InlineData(0, 0)] // a window that has said nothing
    public void AWindowNearSixteenByNineIsTheStandardPictureToTheUnit(int width, int height)
    {
        var layout = AspectLayout.For(width, height);
        Assert.True(layout.IsStandard);
        Assert.Equal(AspectLayout.Standard, layout);
        Assert.Equal((0, 0), (layout.PadX, layout.PadY));
    }

    [Theory]
    [InlineData(2560, 1080, 2560, 1080)] // 21:9
    [InlineData(2560, 1081, 2558, 1080)] // its full-screen window
    [InlineData(3440, 1440, 2580, 1080)]
    [InlineData(3840, 1600, 2592, 1080)]
    [InlineData(5120, 1440, 3840, 1080)] // 32:9
    [InlineData(7680, 1080, 3840, 1080)] // wider than 32:9: barred there
    [InlineData(1440, 1080, 1920, 1440)] // 4:3
    [InlineData(1600, 1200, 1920, 1440)]
    [InlineData(1280, 800, 1920, 1200)] // 16:10, the Deck's
    [InlineData(1080, 1440, 1920, 1440)] // 3:4, a monitor on its side: 4:3 with bars
    [InlineData(1080, 1920, 1920, 1440)] // 9:16
    public void TheCanvasFollowsTheWindowsShapeWithinItsBounds(int width, int height, int canvasWidth, int canvasHeight)
    {
        var layout = AspectLayout.For(width, height);
        Assert.Equal((canvasWidth, canvasHeight), (layout.CanvasWidth, layout.CanvasHeight));
        Assert.True(layout.CanvasWidth >= AspectLayout.SafeWidth && layout.CanvasHeight >= AspectLayout.SafeHeight);
        // Either 1080 tall or 1920 wide, and the safe area in its middle in whole units
        Assert.True(layout.CanvasHeight == 1080 || layout.CanvasWidth == 1920);
        Assert.Equal(layout.CanvasWidth, 2 * layout.PadX + 1920);
        Assert.Equal(layout.CanvasHeight, 2 * layout.PadY + 1080);
        Assert.InRange(layout.Aspect, AspectLayout.Narrowest - 0.01f, AspectLayout.Widest + 0.01f);
    }

    [Fact]
    public void TheSafeAreaShowsTheSixteenByNineViewOnEveryCanvas()
    {
        const float fov = 16.18f;
        // Wider: the same vertical view, to the bit
        Assert.Equal(fov, AspectLayout.For(2560, 1080).FovYFor(fov));
        Assert.Equal(fov, AspectLayout.Standard.FovYFor(fov));
        // Narrower: the vertical view widens so the 1080 rows of the safe area cover what 16:9's did
        var classic = AspectLayout.For(1440, 1080);
        float wider = classic.FovYFor(fov);
        Assert.True(wider > fov);
        double ratio = Math.Tan(wider * Math.PI / 360) / Math.Tan(fov * Math.PI / 360);
        Assert.Equal(1440.0 / 1080.0, ratio, 4);
    }

    [Fact]
    public void ASixteenByNineWindowShowsThePictureAsItAlwaysDid()
    {
        foreach (var (w, h) in new[] { (1920, 1080), (2560, 1440), (1280, 800), (1920, 1200), (3840, 2160) })
        {
            // (a 16:10 window on a 16:9 canvas still has its bars: the canvas is 16:10's own only through For)
            Assert.Equal(Viewport.Fit(w, h), AspectLayout.Standard.ViewportIn(w, h));
        }
    }

    [Fact]
    public void TheMouseIsReadInTheSafeAreasUnitsOnAWideWindow()
    {
        var layout = AspectLayout.For(2560, 1080);
        var viewport = layout.ViewportIn(2560, 1080);
        // The window's middle is the safe area's middle; its left edge is beyond the safe area, off the picture
        Assert.Equal(new Vector2(960, 540), viewport.ToLayout(new Vector2(1280, 540)));
        var edge = viewport.ToLayout(new Vector2(0, 0));
        Assert.Equal(new Vector2(-320, 0), edge);
        Assert.False(Viewport.OnThePicture(edge));
        // The canvas fills the window: no bars
        var (x, y, scale, w, h) = layout.FitIn(2560, 1080);
        Assert.Equal((0f, 0f, 1f, 2560, 1080), (x, y, scale, w, h));

        // 4:3: the safe area stands in the middle of a taller canvas
        var classic = AspectLayout.For(1440, 1080);
        var view = classic.ViewportIn(1440, 1080);
        Assert.Equal(new Vector2(960, 540), view.ToLayout(new Vector2(720, 540)));
        Assert.Equal(new Vector2(0, -180), view.ToLayout(new Vector2(0, 0)));
    }

    [Fact]
    public void TheResolutionsOfferedAreTheMonitorsOwnRatio()
    {
        Assert.Equal(new[] { (1280, 720), (1600, 900), (1920, 1080), (2134, 1200), (2560, 1440) }, AspectLayout.SizesFor(2560, 1440));
        Assert.Equal(new[] { (1706, 720), (2134, 900), (2560, 1080) }, AspectLayout.SizesFor(2560, 1080));
        Assert.Equal(new[] { (1720, 720), (2150, 900), (2580, 1080), (2866, 1200), (3440, 1440) }, AspectLayout.SizesFor(3440, 1440));
        Assert.Equal(new[] { (960, 720), (1200, 900), (1440, 1080), (1600, 1200) }, AspectLayout.SizesFor(1600, 1200));
        // A 16:9-ish monitor offers 16:9's own sizes
        Assert.Equal(new[] { (1280, 720), (1366, 768) }, AspectLayout.SizesFor(1366, 768));
        // Nothing said of the monitor: 1920×1080
        Assert.Equal((1920, 1080), AspectLayout.NativeOr(0, 0));
        Assert.Equal((3440, 1440), AspectLayout.NativeOr(3440, 1440));
        Assert.Equal((1920, 1080), AspectLayout.SizesFor(0, 0)[^1]);
    }

    [Fact]
    public void TheFirstWindowIsTheLargestOfTheMonitorsRatioThatFits()
    {
        Assert.Equal((2866, 1200), AspectLayout.LargestWindowFor(3440, 1440));
        Assert.Equal((2134, 900), AspectLayout.LargestWindowFor(2560, 1080));
        Assert.Equal((1280, 800), AspectLayout.LargestWindowFor(1280, 800));
        Assert.Equal((1440, 1080), AspectLayout.LargestWindowFor(1600, 1200));
    }

    [Theory]
    [InlineData(2560, 1080, "21:9")]
    [InlineData(3440, 1440, "21:9")]
    [InlineData(1920, 1080, "16:9")]
    [InlineData(1440, 1080, "4:3")]
    [InlineData(1280, 800, "16:10")]
    [InlineData(1080, 1440, "3:4")]
    [InlineData(5120, 1440, "32:9")]
    public void ARatioHasItsName(int width, int height, string name) => Assert.Equal(name, AspectLayout.RatioName(width, height));

    [Fact]
    public void AFullScreenResolutionCapsTheScenesShare()
    {
        Assert.Equal(1f, AspectLayout.CapFor(0, 2160)); // NATIVE
        Assert.Equal(0.5f, AspectLayout.CapFor(1080, 2160));
        Assert.Equal(1f, AspectLayout.CapFor(1440, 1080)); // never above the preset's own
    }

    [Fact]
    public void TheResolutionRowIsADisplayRowBeforeHelpAndOffersTheMonitorsSizes()
    {
        Assert.Equal(OptionCategory.Display, OptionsScreen.CategoryOf(OptionRow.Resolution));
        var rows = Enum.GetValues<OptionRow>();
        Assert.Equal(OptionRow.Resolution, rows[Array.IndexOf(rows, OptionRow.Help) - 1]);

        var before = OptionsScreen.Monitor;
        try
        {
            OptionsScreen.Monitor = (2560, 1080);
            // In a window: the window's size, among the monitor's ratio smaller than the monitor
            var s = new GameSettings { WindowWidth = 1920, WindowHeight = 1080 };
            OptionsScreen.Change(s, OptionRow.Resolution, 1);
            Assert.Equal((1706, 720), (s.WindowWidth, s.WindowHeight));
            OptionsScreen.Change(s, OptionRow.Resolution, 1);
            Assert.Equal((2134, 900), (s.WindowWidth, s.WindowHeight));
            OptionsScreen.Change(s, OptionRow.Resolution, 1);
            Assert.Equal((1706, 720), (s.WindowWidth, s.WindowHeight));
            Assert.Contains("21:9", OptionsScreen.Describe(s, OptionRow.Resolution).Help);

            // In full screen: NATIVE, then the smaller heights; the window's own size is left alone
            var f = new GameSettings { Fullscreen = true };
            Assert.Equal(0, f.FullscreenHeight);
            Assert.Contains("2560", OptionsScreen.Describe(f, OptionRow.Resolution).Value);
            OptionsScreen.Change(f, OptionRow.Resolution, 1);
            Assert.Equal(720, f.FullscreenHeight);
            Assert.Contains("1706", OptionsScreen.Describe(f, OptionRow.Resolution).Value);
            OptionsScreen.Change(f, OptionRow.Resolution, -1);
            Assert.Equal(0, f.FullscreenHeight);
            OptionsScreen.Change(f, OptionRow.Resolution, -1);
            Assert.Equal(900, f.FullscreenHeight);
            Assert.Equal((1920, 1080), (f.WindowWidth, f.WindowHeight));
        }
        finally { OptionsScreen.Monitor = before; }
    }
}
