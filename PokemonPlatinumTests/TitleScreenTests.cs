using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

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
        // With a save there is a game to continue and a save that can be deleted (plan 12 · Q2)
        Assert.Equal(new[] { TitleChoice.Continue, TitleChoice.NewGame, TitleChoice.Delete, TitleChoice.Options, TitleChoice.Quit }, new TitleScreen(Save()).Entries);
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
    public void NewGameAsksNothingWhenNothingIsSaved()
    {
        // Every game is played by the modern rules (plan 06 · R19): the question of whose rules is gone
        var title = AtMenu(null);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal(TitleChoice.NewGame, title.TakeChoice());
        Assert.DoesNotContain("ChooseRules", Enum.GetNames<TitlePhase>());
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

        // "Yes" goes on to the new game
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

    // ------------------------------------------------------------------ the slots (plan 12 · Q2)

    private static SaveData Game(string name, float hours = 1f, int badges = 0) =>
        new() { PlayerName = name, PlayTimeSeconds = hours * 3600f, Badges = (1 << badges) - 1 };

    /// <summary>The title with these slots taken, its opening skipped and its menu open.</summary>
    private static TitleScreen WithSlots(params TitleSlot[] taken)
    {
        var title = TitleScreen.WithSlots(taken);
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        return title;
    }

    private static TitleScreen ThreeSaves() =>
        WithSlots(new TitleSlot(1, Game("Lucas")), new TitleSlot(2, Game("Dawn")), new TitleSlot(3, Game("Barry")));

    private static TitleChoice Under(TitleScreen title) => title.Entries[title.SelectedIndex];

    [Fact]
    public void TheMenuHasACardForEachSlotThatHoldsASave()
    {
        var three = ThreeSaves();
        Assert.Equal(new[] { TitleChoice.Continue, TitleChoice.Continue, TitleChoice.Continue, TitleChoice.NewGame, TitleChoice.Delete, TitleChoice.Options, TitleChoice.Quit }, three.Entries);
        Assert.Equal(new[] { 1, 2, 3 }, three.Cards.Select(c => c.Number));
        Assert.True(three.HasSave);
        Assert.Null(three.FreeSlot);

        // The cards stand in the slots' order whatever order they are handed over in, a slot once, and only real slots
        var mixed = TitleScreen.WithSlots(new[] { new TitleSlot(3, Game("Barry")), new TitleSlot(1, Game("Lucas")), new TitleSlot(3, Game("Again")), new TitleSlot(7, Game("Nobody")) });
        Assert.Equal(new[] { (1, "Lucas"), (3, "Barry") }, mixed.Cards.Select(c => (c.Number, c.Save!.PlayerName)));
        Assert.Equal(2, mixed.FreeSlot);

        // The title with one save, as it always was made, is the title with slot 1 taken; with none, no card at all
        var one = new TitleScreen(Save());
        Assert.Equal((1, 1, 2), (one.Cards.Count, one.Cards[0].Number, one.FreeSlot!.Value));
        var none = new TitleScreen(null);
        Assert.Empty(none.Cards);
        Assert.False(none.HasSave);
        Assert.Equal(1, none.FreeSlot);
        Assert.DoesNotContain(TitleChoice.Delete, none.Entries);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    public void ACardContinuesItsOwnSlotsGame(int card, int slot)
    {
        var title = ThreeSaves();
        title.Move(card);
        Assert.Equal(TitleChoice.Continue, Under(title));
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal(TitleChoice.Continue, title.TakeChoice());
        Assert.Equal(slot, title.Slot);
        Assert.Null(title.TakeDeletion());
    }

    [Fact]
    public void ALoneSaveInAnySlotIsContinuedFromThatSlot()
    {
        var title = WithSlots(new TitleSlot(3, Game("Barry")));
        Assert.True(title.LoneCard);
        title.PressConfirm();
        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal((TitleChoice.Continue, 3), (title.TakeChoice(), title.Slot));
    }

    [Fact]
    public void ANewGameTakesTheFirstEmptySlot()
    {
        // With nothing saved it is slot 1, and nothing is asked
        var fresh = AtMenu(null);
        fresh.PressConfirm();
        Run(fresh, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal((TitleChoice.NewGame, 1), (fresh.TakeChoice(), fresh.Slot));

        // With slots 1 and 3 taken it is slot 2, and the question says so and that nothing is lost
        var title = WithSlots(new TitleSlot(1, Game("Lucas")), new TitleSlot(3, Game("Barry")));
        Assert.Equal(2, title.FreeSlot);
        title.Move(2);
        Assert.Equal(TitleChoice.NewGame, Under(title));
        title.PressConfirm();
        Assert.Equal(TitlePhase.ConfirmNewGame, title.Phase);
        Assert.False(title.ConfirmYes);
        Assert.Equal("It will be saved in slot 2. The saves you have are kept as they are.", TitleScreen.NewGameBlurb(title.FreeSlot!.Value, title.Cards.Count));
        Assert.Equal("It will be saved in slot 2. The save you have is kept as it is.", TitleScreen.NewGameBlurb(2, 1));
        title.Move(1);
        title.PressConfirm();
        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal((TitleChoice.NewGame, 2), (title.TakeChoice(), title.Slot));
    }

    [Fact]
    public void WithEverySlotTakenANewGameStartsNothingAndSaysWhy()
    {
        var title = ThreeSaves();
        title.Move(3);
        Assert.Equal(TitleChoice.NewGame, Under(title));
        title.PressConfirm();

        // A notice, read and put away like any other, and the menu is back under the same cursor
        Assert.Equal(TitlePhase.Warning, title.Phase);
        Assert.Equal(TitleNotice.NoFreeSlot, title.Notice);
        Assert.Contains("DELETE A SAVE", title.Notice!.Body);
        Run(title, TitleScreen.LeaveTime + 1f);
        Assert.Equal(TitleChoice.None, title.TakeChoice());
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        Assert.Equal(TitleChoice.NewGame, Under(title));
        Assert.Null(title.Notice);

        // Once a slot is free it begins there
        title.Forget(2);
        Assert.Equal(TitleChoice.Continue, Under(title));
        title.Move(2);
        title.PressConfirm();
        Assert.Equal(TitlePhase.ConfirmNewGame, title.Phase);
        Assert.Equal(2, title.FreeSlot);
    }

    [Fact]
    public void DeletingASaveAsksWhichAndThenAsksAgain()
    {
        var title = ThreeSaves();
        title.Move(4);
        Assert.Equal(TitleChoice.Delete, Under(title));
        title.PressConfirm();

        // Which: the cursor goes to the cards and stays among them
        Assert.Equal(TitlePhase.ChooseDelete, title.Phase);
        Assert.Equal(0, title.SelectedIndex);
        title.Move(-1);
        Assert.Equal(2, title.SelectedIndex);
        title.Move(1);
        title.Move(1);
        Assert.Equal(1, title.SelectedIndex);
        Assert.Null(title.TakeDeletion());

        // Then the question, which names the slot, with No highlighted
        title.PressConfirm();
        Assert.Equal(TitlePhase.ConfirmDelete, title.Phase);
        Assert.Equal(2, title.DeletingSlot);
        Assert.False(title.ConfirmYes);
        Assert.Equal("Delete slot 2?", TitleScreen.DeleteHeading(title.DeletingSlot));
        Assert.Equal(new[] { "NO, KEEP IT", "YES, DELETE" }, TitleScreen.DeleteAnswers);

        // No goes back to the choice of a save, and so does B; B again goes back to the row that asked
        title.PressConfirm();
        Assert.Equal((TitlePhase.ChooseDelete, 1), (title.Phase, title.SelectedIndex));
        title.PressConfirm();
        title.Move(1);
        Assert.True(title.ConfirmYes);
        title.PressCancel();
        Assert.Equal(TitlePhase.ChooseDelete, title.Phase);
        Assert.Null(title.TakeDeletion());
        title.PressCancel();
        Assert.Equal((TitlePhase.Menu, TitleChoice.Delete), (title.Phase, Under(title)));
        Assert.Equal(3, title.Cards.Count);

        // Yes hands the slot over once; the screen deletes nothing itself and keeps the card until it is told
        title.PressConfirm();
        title.Move(2);
        title.PressConfirm();
        Assert.False(title.ConfirmYes);
        title.Move(1);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        Assert.Equal(3, title.TakeDeletion());
        Assert.Null(title.TakeDeletion());
        Assert.Equal(3, title.Cards.Count);
        Assert.Equal(TitleChoice.None, title.TakeChoice());

        // Told the slot is gone, its card goes, the column closes up and a new game has a slot
        title.Forget(3);
        Assert.Equal(new[] { 1, 2 }, title.Cards.Select(c => c.Number));
        Assert.Equal(new[] { TitleChoice.Continue, TitleChoice.Continue, TitleChoice.NewGame, TitleChoice.Delete, TitleChoice.Options, TitleChoice.Quit }, title.Entries);
        Assert.Equal((TitlePhase.Menu, 0, 3), (title.Phase, title.SelectedIndex, title.FreeSlot!.Value));
        // (a slot that was never there changes nothing)
        title.Forget(3);
        Assert.Equal(2, title.Cards.Count);
    }

    [Fact]
    public void WithOneSaveThereIsNoAskingWhich()
    {
        var title = AtMenu(Save());
        title.Move(2);
        Assert.Equal(TitleChoice.Delete, Under(title));
        title.PressConfirm();
        Assert.Equal((TitlePhase.ConfirmDelete, 1, false), (title.Phase, title.DeletingSlot, title.ConfirmYes));

        // B goes back to the menu, since no choice of a save came before the question
        title.PressCancel();
        Assert.Equal((TitlePhase.Menu, TitleChoice.Delete), (title.Phase, Under(title)));
        title.PressConfirm();
        title.Move(1);
        title.PressConfirm();
        Assert.Equal(1, title.TakeDeletion());

        // The last save gone, the title is the one with nothing saved
        title.Forget(1);
        Assert.Equal(new[] { TitleChoice.NewGame, TitleChoice.Options, TitleChoice.Quit }, title.Entries);
        Assert.Equal((TitleChoice.NewGame, 1, false), (Under(title), title.FreeSlot!.Value, title.HasSave));
    }

    [Fact]
    public void ASaveThatCouldntBeDeletedIsStillThereAndTheTitleSaysSo()
    {
        var title = ThreeSaves();
        title.Move(4);
        title.PressConfirm();
        title.PressConfirm();
        title.Move(1);
        title.PressConfirm();
        Assert.Equal(1, title.TakeDeletion());

        // Whoever was to delete it couldn't: the card stays, a notice says so over the menu, and the menu is back after it
        title.Tell(TitleNotice.CouldNotDelete(1));
        Assert.Equal(TitlePhase.Warning, title.Phase);
        Assert.Equal("Slot 1 couldn't be deleted", title.Notice!.Heading);
        title.PressCancel();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        Assert.Equal(3, title.Cards.Count);
    }

    [Fact]
    public void ASlotThatCantBeReadIsACardThatCanOnlyBeDeleted()
    {
        var title = WithSlots(new TitleSlot(1, Game("Lucas")), new TitleSlot(2, null));
        Assert.Equal(new[] { TitleChoice.Continue, TitleChoice.Unreadable, TitleChoice.NewGame, TitleChoice.Delete, TitleChoice.Options, TitleChoice.Quit }, title.Entries);
        Assert.False(title.LoneCard);
        // The slot is taken: a new game goes to the third
        Assert.Equal(3, title.FreeSlot);

        // A press on it continues nothing: it asks whether to delete it
        title.Move(1);
        title.PressConfirm();
        Assert.Equal((TitlePhase.ConfirmDelete, 2, false), (title.Phase, title.DeletingSlot, title.ConfirmYes));
        Run(title, TitleScreen.LeaveTime + 0.05f);
        Assert.Equal(TitleChoice.None, title.TakeChoice());
        Assert.Contains("can't be read", TitleScreen.DeleteBlurb(null, null));
        title.PressConfirm();
        Assert.Equal((TitlePhase.Menu, TitleChoice.Unreadable), (title.Phase, Under(title)));

        // It is one of the saves DELETE A SAVE asks about
        title.Move(2);
        title.PressConfirm();
        Assert.Equal(TitlePhase.ChooseDelete, title.Phase);
        title.Move(1);
        title.PressConfirm();
        Assert.Equal(2, title.DeletingSlot);

        // Alone it is no game to continue, and no whole card
        var only = WithSlots(new TitleSlot(2, null));
        Assert.Equal(new[] { TitleChoice.Unreadable, TitleChoice.NewGame, TitleChoice.Delete, TitleChoice.Options, TitleChoice.Quit }, only.Entries);
        Assert.False(only.HasSave || only.LoneCard);
        Assert.Equal(1, only.FreeSlot);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void TheColumnFitsTheScreenHoweverManySavesThereAre(int saves)
    {
        var title = WithSlots(Enumerable.Range(1, saves).Select(n => new TitleSlot(n, Game($"Player {n}"))).ToArray());
        var rects = title.EntryRects();
        Assert.Equal(title.Entries.Count, rects.Count);

        // In its place on the right, under the top edge and clear of the disclaimer at the foot (which begins at 1036)
        Assert.All(rects, r => Assert.Equal((TitleScreen.ColumnX, TitleScreen.ColumnWidth), (r.X, r.Width)));
        Assert.True(rects[0].Y >= 100f, $"the column begins at {rects[0].Y}");
        Assert.True(rects[^1].Y + rects[^1].Height <= 1016f, $"the column ends at {rects[^1].Y + rects[^1].Height}");
        // One under another, with the same gap between
        float gap = saves == 0 ? 20f : 18f;
        for (int i = 1; i < rects.Count; i++) Assert.Equal(rects[i - 1].Y + rects[i - 1].Height + gap, rects[i].Y, 3);

        // A lone save keeps the whole card; two or three are short cards; the rows are slimmer once there is a save
        float card = saves == 1 ? TitleScreen.LoneCardHeight : TitleScreen.ShortCardHeight;
        Assert.All(rects.Take(saves), r => Assert.Equal(card, r.Height));
        Assert.All(rects.Skip(saves), r => Assert.Equal(saves == 0 ? 92f : 76f, r.Height));
        Assert.Equal(saves == 1, title.LoneCard);

        // With nothing saved the three rows stand where they always did; with a save the column is centred on one line
        if (saves == 0) Assert.Equal(422f, rects[0].Y, 3);
        else Assert.Equal(590f, (rects[0].Y + rects[^1].Y + rects[^1].Height) / 2f, 3);
    }

    [Fact]
    public void ANoticeNamesItsSlot()
    {
        foreach (int slot in new[] { 1, 2, 3 })
        {
            Assert.Null(TitleNotice.ForSlot(slot, SaveLoad.Ok));
            Assert.Null(TitleNotice.ForSlot(slot, SaveLoad.None));
            Assert.Equal($"Slot {slot}'s last save was damaged", TitleNotice.ForSlot(slot, SaveLoad.Recovered)!.Heading);
            Assert.Equal($"Slot {slot} can't be read", TitleNotice.ForSlot(slot, SaveLoad.Damaged)!.Heading);
            Assert.Contains("DELETE A SAVE", TitleNotice.ForSlot(slot, SaveLoad.Damaged)!.Body);
        }
        // A lone save's notice is slot 1's
        Assert.Equal(TitleNotice.ForSlot(1, SaveLoad.Recovered), TitleNotice.ForSave(SaveLoad.Recovered));
        Assert.Equal(TitleNotice.ForSlot(1, SaveLoad.Damaged), TitleNotice.ForSave(SaveLoad.Damaged));

        // One to a slot, in the slots' order, before the menu
        var title = TitleScreen.WithSlots(new[] { new TitleSlot(1, Game("Lucas")), new TitleSlot(2, null), new TitleSlot(3, Game("Barry")) },
            TitleNotice.ForSlot(1, SaveLoad.Ok), TitleNotice.ForSlot(2, SaveLoad.Damaged), TitleNotice.ForSlot(3, SaveLoad.Recovered));
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal("Slot 2 can't be read", title.Notice!.Heading);
        title.PressConfirm();
        Assert.Equal("Slot 3's last save was damaged", title.Notice!.Heading);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);

        // An old save that couldn't be made a slot is told too; one that was moved, or none, is not
        Assert.Null(TitleNotice.ForOldSave(new OldSaveOutcome(OldSaveMove.Nothing)));
        Assert.Null(TitleNotice.ForOldSave(new OldSaveOutcome(OldSaveMove.Moved, 1)));
        Assert.Contains("savegame.json", TitleNotice.ForOldSave(new OldSaveOutcome(OldSaveMove.NoRoom))!.Body);
        Assert.Contains("savegame.json", TitleNotice.ForOldSave(new OldSaveOutcome(OldSaveMove.Failed, Problem: "in use"))!.Body);
    }

    [Fact]
    public void AfterACrashTheNoticeSpeaksOfEverySave()
    {
        var note = new PokemonPlatinumEngine.Core.Diagnostics.CrashNote("logs/crash-20260601-120000.txt", true, new DateTime(2026, 6, 1, 12, 0, 0));
        string Says(params SaveLoad[] slots) => TitleNotice.ForCrash(note, slots)!.Body;
        Assert.StartsWith("No game had been saved yet. ", Says(SaveLoad.None, SaveLoad.None, SaveLoad.None));
        Assert.StartsWith("Your save is intact. ", Says(SaveLoad.None, SaveLoad.Ok, SaveLoad.None));
        Assert.StartsWith("Your saves are intact. ", Says(SaveLoad.Ok, SaveLoad.None, SaveLoad.Ok));
        // A slot that couldn't be read is never called intact: its own notice follows
        Assert.DoesNotContain("intact", Says(SaveLoad.Ok, SaveLoad.Recovered, SaveLoad.None));
        Assert.DoesNotContain("intact", Says(SaveLoad.Damaged, SaveLoad.Ok, SaveLoad.Ok));
        Assert.Null(TitleNotice.ForCrash(null, new[] { SaveLoad.Ok, SaveLoad.Ok }));
        // With one slot it is what it always said
        Assert.Equal(TitleNotice.ForCrash(note, SaveLoad.Ok), TitleNotice.ForCrash(note, new[] { SaveLoad.Ok }));
    }

    [Fact]
    public void WhatTheSlotsSayFitsTheirPanels()
    {
        // A notice's panel and a question's are 1040 wide with 56 at each side; a heading is 48 Black on one line, a
        // notice's body 30 ExtraBold on three lines at most, a question's on two (its buttons stand under them)
        const float width = 1040 - 112;
        void Fits(string heading, string body, int rows)
        {
            Assert.True(TextMetrics.Measure(heading, 48, UiWeight.Black) <= width, heading);
            var wrapped = TextMetrics.Wrap(body, width, 30, UiWeight.ExtraBold);
            Assert.True(wrapped.Count <= rows, $"{wrapped.Count} rows: {body}");
            Assert.Empty(TextMetrics.Undrawable(heading + body, UiWeight.ExtraBold));
        }
        foreach (int slot in new[] { 1, 2, 3 })
        {
            foreach (var load in new[] { SaveLoad.Recovered, SaveLoad.Damaged })
                Fits(TitleNotice.ForSlot(slot, load)!.Heading, TitleNotice.ForSlot(slot, load)!.Body, 3);
            Fits(TitleNotice.CouldNotDelete(slot).Heading, TitleNotice.CouldNotDelete(slot).Body, 3);
            // The longest a name, a place and a time get
            Fits(TitleScreen.DeleteHeading(slot), TitleScreen.DeleteBlurb(Game("WWWWWWWW", 999.99f), "Team Galactic Eterna Building"), 2);
            Fits(TitleScreen.DeleteHeading(slot), TitleScreen.DeleteBlurb(null, null), 2);
            foreach (int saves in new[] { 1, 2 }) Fits("Start a new game?", TitleScreen.NewGameBlurb(slot, saves), 2);
        }
        Fits(TitleNotice.NoFreeSlot.Heading, TitleNotice.NoFreeSlot.Body, 3);
        foreach (var move in new[] { OldSaveMove.NoRoom, OldSaveMove.Failed })
            Fits(TitleNotice.ForOldSave(new OldSaveOutcome(move))!.Heading, TitleNotice.ForOldSave(new OldSaveOutcome(move))!.Body, 3);
        Assert.Equal("Lucas in Sandgem Town, 12:34 played. The save and its backup are deleted for good.",
            TitleScreen.DeleteBlurb(new SaveData { PlayerName = "Lucas", PlayTimeSeconds = 12 * 3600 + 34 * 60 + 20 }, "Sandgem Town"));

        // The save panel's question names the slot too, on one line of its 1080 wide panel (44 Black, 56 at each side)
        foreach (int slot in new[] { 1, 2, 3 })
        {
            Assert.Equal($"Save the game in slot {slot}?", SaveScreen.Question(slot));
            Assert.True(TextMetrics.Measure(SaveScreen.Question(slot), 44, UiWeight.Black) <= 1080 - 112);
        }
    }

    [Fact]
    public void WithSeveralSavesTheTitleStillPlaysItsOpeningAgainAndOnlyTheTitleItself()
    {
        // The attract loop (plan 09 · L4) with the column of cards: thirty seconds on the title and the opening plays
        // again; the choice of a save to delete and its question wait for as long as it takes, as the menu does
        var title = TitleScreen.WithSlots(new[] { new TitleSlot(1, Game("Lucas")), new TitleSlot(2, Game("Dawn")) });
        title.PressConfirm();
        Run(title, TitleScreen.IdleTime + 0.1f);
        Assert.Equal(TitlePhase.Journey, title.Phase);
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal((TitlePhase.Menu, 0), (title.Phase, title.SelectedIndex));

        title.Move(3);
        title.PressConfirm();
        Assert.Equal(TitlePhase.ChooseDelete, title.Phase);
        Run(title, TitleScreen.IdleTime * 3f);
        Assert.Equal(TitlePhase.ChooseDelete, title.Phase);
        title.PressConfirm();
        Run(title, TitleScreen.IdleTime * 3f);
        Assert.Equal(TitlePhase.ConfirmDelete, title.Phase);
        Assert.Null(title.TakeDeletion());
        Assert.Equal(TitleChoice.None, title.TakeChoice());
    }
}
