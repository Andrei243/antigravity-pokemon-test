using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>The opening and the title menu: what plays, what skips it, and what each choice does.</summary>
// MapDatabase is static, so the classes that rebuild it must not run in parallel
[Collection("MapDatabase")]
public class TitleScreenTests
{
    private const float Step = 1f / 60f;

    private static SaveData Save() => new() { PlayerName = "Lucas", Badges = 0b101, PlayTimeSeconds = 3725f };

    private static void Run(TitleScreen title, float seconds)
    {
        for (float t = 0f; t < seconds; t += Step) title.Advance(Step);
    }

    /// <summary>Skips the opening and opens the menu.</summary>
    private static TitleScreen AtMenu(SaveData? save)
    {
        var title = new TitleScreen(save);
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        return title;
    }

    [Theory]
    [InlineData(0f, "0:00")]
    [InlineData(59f, "0:00")]
    [InlineData(420f, "0:07")]
    [InlineData(3725f, "1:02")]
    [InlineData(12 * 3600f + 34 * 60f + 59f, "12:34")]
    [InlineData(123 * 3600f + 5 * 60f, "123:05")]
    [InlineData(-10f, "0:00")]
    public void PlayTimeIsShownAsHoursAndMinutes(float seconds, string expected)
    {
        Assert.Equal(expected, TitleScreen.FormatPlayTime(seconds));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(0b1, 1)]
    [InlineData(0b101, 2)]
    [InlineData(0b1111_1111, 8)]
    [InlineData(0b1_1111_1111, 8)] // only the eight Sinnoh badges count
    public void BadgesAreCountedFromTheSaveMask(int mask, int expected)
    {
        Assert.Equal(expected, TitleScreen.CountBadges(mask));
    }

    [Fact]
    public void ContinueIsOfferedOnlyWhenThereIsASave()
    {
        Assert.Equal(new[] { TitleChoice.NewGame, TitleChoice.Options, TitleChoice.Quit }, new TitleScreen(null).Entries);
        Assert.Equal(new[] { TitleChoice.Continue, TitleChoice.NewGame, TitleChoice.Options, TitleChoice.Quit }, new TitleScreen(Save()).Entries);
    }

    [Fact]
    public void TheOpeningPlaysThroughToTheTitleByItself()
    {
        var title = new TitleScreen(null);
        Assert.Equal(TitlePhase.Notice, title.Phase);

        Run(title, TitleScreen.NoticeTime + 0.1f);
        Assert.Equal(TitlePhase.Journey, title.Phase);

        Run(title, TitleScreen.SegmentTime * TitleScreen.Segments.Length);
        Assert.Equal(TitlePhase.Reveal, title.Phase);

        Run(title, TitleScreen.RevealTime);
        Assert.Equal(TitlePhase.Idle, title.Phase);

        // It then waits on the title for as long as it takes
        Run(title, 30f);
        Assert.Equal(TitlePhase.Idle, title.Phase);
        Assert.Equal(TitleChoice.None, title.TakeChoice());
    }

    [Theory]
    [InlineData(0.5f)]  // during the notice
    [InlineData(6f)]    // during the fly-over shots
    [InlineData(15f)]   // while Giratina appears
    public void AButtonSkipsTheOpeningToTheTitle(float after)
    {
        var title = new TitleScreen(null);
        Run(title, after);
        Assert.NotEqual(TitlePhase.Idle, title.Phase);

        title.PressConfirm();
        Assert.Equal(TitlePhase.Idle, title.Phase);

        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        Assert.Equal(0, title.SelectedIndex);
    }

    [Fact]
    public void TheMenuCursorWrapsAndCancelReturnsToTheTitle()
    {
        var title = AtMenu(Save());
        title.Move(-1);
        Assert.Equal(TitleChoice.Quit, title.Entries[title.SelectedIndex]);
        title.Move(1);
        Assert.Equal(TitleChoice.Continue, title.Entries[title.SelectedIndex]);

        title.PressCancel();
        Assert.Equal(TitlePhase.Idle, title.Phase);
    }

    [Fact]
    public void ContinueIsHandedOverOnceTheScreenHasFadedOut()
    {
        var title = AtMenu(Save());
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        Assert.Equal(TitleChoice.None, title.TakeChoice());

        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal(TitleChoice.Continue, title.TakeChoice());
        Assert.Equal(TitleChoice.None, title.TakeChoice());
    }

    [Fact]
    public void NewGameStartsStraightAwayWhenNothingIsSaved()
    {
        var title = AtMenu(null);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);

        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal(TitleChoice.NewGame, title.TakeChoice());
    }

    [Fact]
    public void NewGameAsksFirstWhenThereIsASaveAndDefaultsToNo()
    {
        var title = AtMenu(Save());
        title.Move(1);
        title.PressConfirm();
        Assert.Equal(TitlePhase.ConfirmNewGame, title.Phase);
        Assert.False(title.ConfirmYes);

        // "No" goes back to the menu, and so does the B button
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        title.PressConfirm();
        title.PressCancel();
        Assert.Equal(TitlePhase.Menu, title.Phase);

        // "Yes" starts the new game
        title.PressConfirm();
        title.Move(1);
        Assert.True(title.ConfirmYes);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal(TitleChoice.NewGame, title.TakeChoice());
    }

    [Fact]
    public void OptionsOpenAtOnceAndTheMenuStaysUp()
    {
        var title = AtMenu(null);
        title.Move(1);
        title.PressConfirm();
        Assert.Equal(TitleChoice.Options, title.TakeChoice());
        Assert.Equal(TitleChoice.None, title.TakeChoice());
        Assert.Equal(TitlePhase.Menu, title.Phase);
    }

    [Fact]
    public void QuitIsHandedOverOnceTheScreenHasFadedOut()
    {
        var title = AtMenu(null);
        title.Move(-1);
        Assert.Equal(TitleChoice.Quit, title.Entries[title.SelectedIndex]);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        Assert.Equal(TitleChoice.None, title.TakeChoice());

        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal(TitleChoice.Quit, title.TakeChoice());
    }

    [Fact]
    public void TheFlyOverShotsStayInsideMapsThatExist()
    {
        MapDatabase.Initialize();
        Assert.NotEmpty(TitleScreen.Segments);
        foreach (var (name, from, to, hour) in TitleScreen.Segments)
        {
            var map = MapDatabase.Get(name);
            Assert.Equal(name, map.Name);
            foreach (var point in new[] { from, to })
            {
                Assert.InRange(point.X, 0f, map.Width);
                Assert.InRange(point.Y, 0f, map.Height);
                // On the map of the region a shot must look at a place that is built, not at the forest beyond
                if (map.IsStreamed) Assert.True(map.AreaAt((int)point.X, (int)point.Y)?.Open, $"the shot at {point} looks at nothing built");
            }
            Assert.InRange(hour, 0f, 24f);
        }

        // The shots show different times of day
        Assert.True(TitleScreen.Segments.Select(s => GameClock.ForHour((int)s.Hour)).Distinct().Count() >= 3);
    }

    [Theory]
    [InlineData(1366, 768, 1280, 720)]   // too small for anything else: the smallest size
    [InlineData(1920, 1080, 1600, 900)]
    [InlineData(2560, 1440, 1920, 1080)]
    [InlineData(3840, 2160, 2560, 1440)]
    [InlineData(5120, 2880, 3840, 2160)]
    public void TheFirstWindowIsTheLargestTheMonitorCanShow(int monitorW, int monitorH, int width, int height)
    {
        Assert.Equal((width, height), GameSettings.LargestWindowFor(monitorW, monitorH));
    }

    // A borderless OpenGL window of exactly the monitor's size is taken over by the driver as exclusive full
    // screen and flickers (seen on an NVIDIA card at 3840x2160); it has to cover the monitor without matching it
    [Theory]
    [InlineData(1920, 1080)]
    [InlineData(3840, 2160)]
    public void TheFullScreenWindowCoversTheMonitorWithoutMatchingIt(int monitorW, int monitorH)
    {
        var (width, height) = WindowSettings.FullscreenSize(monitorW, monitorH);
        Assert.True(width >= monitorW && height >= monitorH);
        Assert.NotEqual((monitorW, monitorH), (width, height));

        // The picture is letterboxed by the smaller scale, so it still fills the display exactly
        float scale = MathF.Min((float)width / GameEngine.VirtualWidth, (float)height / GameEngine.VirtualHeight);
        Assert.Equal(monitorW, (int)(GameEngine.VirtualWidth * scale));
        Assert.Equal(monitorH, (int)(GameEngine.VirtualHeight * scale));
    }
}
