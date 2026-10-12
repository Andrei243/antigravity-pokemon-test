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

        // It then waits on the title, for most of half a minute
        Run(title, TitleScreen.IdleTime - 1f);
        Assert.Equal(TitlePhase.Idle, title.Phase);
        Assert.Equal(TitleChoice.None, title.TakeChoice());
    }

    [Fact]
    public void LeftAloneTheTitlePlaysItsOpeningAgain()
    {
        // The attract loop (plan 09 · L4): thirty seconds on the title with no button, and the fly-over shots and the
        // reveal play again, never the notice, and so on for as long as nobody comes
        Assert.Equal(30f, TitleScreen.IdleTime);
        var title = new TitleScreen(null);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Idle, title.Phase);

        for (int round = 0; round < 3; round++)
        {
            Run(title, TitleScreen.IdleTime - 0.5f);
            Assert.Equal(TitlePhase.Idle, title.Phase);
            Run(title, 0.6f);
            Assert.Equal(TitlePhase.Journey, title.Phase);
            Run(title, TitleScreen.SegmentTime * TitleScreen.Segments.Length - 0.5f);
            Assert.Equal(TitlePhase.Journey, title.Phase);
            Run(title, 0.5f);
            Assert.Equal(TitlePhase.Reveal, title.Phase);
            Run(title, TitleScreen.RevealTime);
            Assert.Equal(TitlePhase.Idle, title.Phase);
            Assert.Equal(TitleChoice.None, title.TakeChoice());
        }
    }

    [Theory]
    [InlineData(2f)]   // in the first fly-over shot
    [InlineData(9f)]   // in the last
    [InlineData(12f)]  // while Giratina appears again
    public void AButtonDuringTheOpeningPlayedAgainComesBackToTheTitle(float after)
    {
        var title = new TitleScreen(Save());
        title.PressConfirm();
        Run(title, TitleScreen.IdleTime + 0.1f + after);
        Assert.NotEqual(TitlePhase.Idle, title.Phase);

        // As the first time: the title, then the menu with its first entry highlighted
        title.PressConfirm();
        Assert.Equal(TitlePhase.Idle, title.Phase);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        Assert.Equal(0, title.SelectedIndex);
        Assert.Equal(TitleChoice.Continue, title.Entries[title.SelectedIndex]);
    }

    [Fact]
    public void OnlyTheTitleItselfLoops()
    {
        // The menu, a prompt and a notice wait for as long as it takes
        var menu = AtMenu(Save());
        Run(menu, TitleScreen.IdleTime * 3f);
        Assert.Equal(TitlePhase.Menu, menu.Phase);

        menu.Move(1);
        menu.PressConfirm();
        Assert.Equal(TitlePhase.ConfirmNewGame, menu.Phase);
        Run(menu, TitleScreen.IdleTime * 3f);
        Assert.Equal(TitlePhase.ConfirmNewGame, menu.Phase);

        menu.Move(1);
        menu.PressConfirm();
        Assert.Equal(TitlePhase.ChooseRules, menu.Phase);
        Run(menu, TitleScreen.IdleTime * 3f);
        Assert.Equal(TitlePhase.ChooseRules, menu.Phase);

        var warned = new TitleScreen(Save(), TitleNotice.ForSave(SaveLoad.Recovered));
        warned.PressConfirm();
        warned.PressConfirm();
        Assert.Equal(TitlePhase.Warning, warned.Phase);
        Run(warned, TitleScreen.IdleTime * 3f);
        Assert.Equal(TitlePhase.Warning, warned.Phase);

        // Back on the title from the menu, the half minute begins again
        menu.PressCancel();
        menu.PressCancel();
        Assert.Equal(TitlePhase.Idle, menu.Phase);
        Run(menu, TitleScreen.IdleTime - 1f);
        Assert.Equal(TitlePhase.Idle, menu.Phase);
        Run(menu, 1.1f);
        Assert.Equal(TitlePhase.Journey, menu.Phase);
    }

    [Fact]
    public void ANoticeNotYetReadIsStillToldAfterTheLoop()
    {
        var title = new TitleScreen(Save(), TitleNotice.ForSave(SaveLoad.Recovered));
        title.PressConfirm();
        Run(title, TitleScreen.IdleTime + 3f);
        Assert.Equal(TitlePhase.Journey, title.Phase);
        Assert.NotNull(title.Notice);

        title.PressConfirm();
        Assert.Equal(TitlePhase.Idle, title.Phase);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Warning, title.Phase);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
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
    public void NewGameAsksOnlyForTheRulesWhenNothingIsSaved()
    {
        var title = AtMenu(null);
        title.PressConfirm();
        Assert.Equal(TitlePhase.ChooseRules, title.Phase);
        Assert.Equal(RulesPreset.Platinum, title.Rules);

        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal(TitleChoice.NewGame, title.TakeChoice());
        Assert.Equal(RulesPreset.Platinum, title.Rules);
    }

    [Fact]
    public void TheRulesAreChosenBeforeANewGameAndPlatinumsAreOffered()
    {
        var title = AtMenu(null);
        title.PressConfirm();

        // Any direction flips the answer; each has its own words, and both say it is for good
        Assert.Contains("Platinum", TitleScreen.RulesBlurb(title.Rules));
        title.Move(1);
        Assert.Equal(RulesPreset.Modern, title.Rules);
        Assert.Contains("newest", TitleScreen.RulesBlurb(title.Rules));
        Assert.All(new[] { RulesPreset.Platinum, RulesPreset.Modern }, r => Assert.Contains("can't be changed", TitleScreen.RulesBlurb(r)));
        title.Move(-1);
        Assert.Equal(RulesPreset.Platinum, title.Rules);

        // B goes back to the menu without starting anything, and the question starts over at Platinum's
        title.Move(1);
        title.PressCancel();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        Assert.Equal(TitleChoice.None, title.TakeChoice());
        title.PressConfirm();
        Assert.Equal(TitlePhase.ChooseRules, title.Phase);
        Assert.Equal(RulesPreset.Platinum, title.Rules);

        // The modern rules, chosen: the new game is handed over with them
        title.Move(1);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal(TitleChoice.NewGame, title.TakeChoice());
        Assert.Equal(RulesPreset.Modern, title.Rules);
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

        // "Yes" goes on to the rules, and those to the new game
        title.PressConfirm();
        title.Move(1);
        Assert.True(title.ConfirmYes);
        title.PressConfirm();
        Assert.Equal(TitlePhase.ChooseRules, title.Phase);
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

    // ------------------------------------------------------------------ a save that couldn't be read (plan 12 · Q1)

    [Fact]
    public void OnlyASaveThatCouldntBeReadHasANotice()
    {
        Assert.Null(TitleNotice.ForSave(SaveLoad.Ok));
        Assert.Null(TitleNotice.ForSave(SaveLoad.None));
        Assert.NotNull(TitleNotice.ForSave(SaveLoad.Recovered));
        Assert.NotNull(TitleNotice.ForSave(SaveLoad.Damaged));
        Assert.NotEqual(TitleNotice.ForSave(SaveLoad.Recovered), TitleNotice.ForSave(SaveLoad.Damaged));
    }

    [Fact]
    public void TheNoticeComesBeforeTheMenu()
    {
        var notice = TitleNotice.ForSave(SaveLoad.Recovered)!;
        var title = new TitleScreen(Save(), notice);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Idle, title.Phase);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Warning, title.Phase);
        Assert.Same(notice, title.Notice);

        // The menu waits under it: a direction does nothing, and nothing is chosen
        title.Move(1);
        Run(title, 5f);
        Assert.Equal(TitlePhase.Warning, title.Phase);
        Assert.Equal(TitleChoice.None, title.TakeChoice());

        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        Assert.Null(title.Notice);
        Assert.Equal(TitleChoice.Continue, title.Entries[title.SelectedIndex]);
    }

    [Fact]
    public void NoticesAreReadOneAfterAnotherAndBPutsThemAwayToo()
    {
        var first = new TitleNotice("First", "One.");
        var second = new TitleNotice("Second", "Two.");
        var title = new TitleScreen(null, first, null, second);
        title.PressConfirm();
        title.PressConfirm();
        Assert.Same(first, title.Notice);
        title.PressCancel();
        Assert.Equal(TitlePhase.Warning, title.Phase);
        Assert.Same(second, title.Notice);
        title.PressCancel();
        Assert.Equal(TitlePhase.Menu, title.Phase);

        // Once read, they don't come back when the menu is backed out of and opened again
        title.PressCancel();
        Assert.Equal(TitlePhase.Idle, title.Phase);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
    }

    [Fact]
    public void ADamagedSaveLeavesNoGameToContinue()
    {
        var title = new TitleScreen(null, TitleNotice.ForSave(SaveLoad.Damaged));
        Assert.DoesNotContain(TitleChoice.Continue, title.Entries);
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal(TitlePhase.Warning, title.Phase);
        title.PressConfirm();
        Assert.Equal(TitleChoice.NewGame, title.Entries[title.SelectedIndex]);
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
