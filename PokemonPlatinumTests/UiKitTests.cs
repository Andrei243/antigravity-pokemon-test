using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>The interface kit's rules that need no window (plan 04 · G3): motion, menu cursors, the start menu and its quit prompt.</summary>
public class UiKitTests
{
    private const float Step = 1f / 60f;

    // ------------------------------------------------------------------ motion

    [Fact]
    public void EasingsStartAtZeroEndAtOneAndNeverGoBack()
    {
        foreach (var ease in new System.Func<float, float>[] { UiMotion.EaseOut, UiMotion.EaseIn, UiMotion.EaseInOut })
        {
            Assert.Equal(0f, ease(0f), 4);
            Assert.Equal(1f, ease(1f), 4);
            Assert.Equal(0f, ease(-1f), 4);
            Assert.Equal(1f, ease(2f), 4);
            float last = 0f;
            for (float t = 0f; t <= 1f; t += 0.05f)
            {
                Assert.True(ease(t) >= last - 1e-5f);
                last = ease(t);
            }
        }

        // Arriving things are already most of the way there halfway through; leaving things have barely moved
        Assert.True(UiMotion.EaseOut(0.5f) > 0.8f);
        Assert.True(UiMotion.EaseIn(0.5f) < 0.2f);
    }

    [Fact]
    public void ThingsSlideInOverTheirOpenTimeAndLeaveFaster()
    {
        var reveal = new UiReveal(openTime: 0.2f, closeTime: 0.1f);
        Assert.False(reveal.Visible);

        reveal.Open();
        reveal.Update(0.1f);
        Assert.True(reveal.Visible);
        Assert.InRange(reveal.Shown, 0.5f, 0.99f);
        reveal.Update(0.11f);
        Assert.Equal(1f, reveal.Shown, 4);

        reveal.Close();
        reveal.Update(0.05f);
        Assert.True(reveal.Visible);
        Assert.InRange(reveal.Shown, 0.01f, 0.99f);
        reveal.Update(0.06f);
        Assert.False(reveal.Visible);

        reveal.Snap(true);
        Assert.Equal(1f, reveal.Shown, 4);
    }

    // ------------------------------------------------------------------ menu cursors

    [Theory]
    // Six slots, three to a row (the battle's switch panel)
    [InlineData(0, 6, 3, 1, 0, 1)]
    [InlineData(2, 6, 3, 1, 0, 0)]   // wraps along the row
    [InlineData(0, 6, 3, -1, 0, 2)]
    [InlineData(1, 6, 3, 0, 1, 4)]
    [InlineData(4, 6, 3, 0, 1, 1)]   // wraps down the column
    // A team of four in that panel: the second row has one Pokémon
    [InlineData(0, 4, 3, 0, 1, 3)]
    [InlineData(1, 4, 3, 0, 1, 1)]   // nothing under the second slot
    [InlineData(3, 4, 3, 1, 0, 3)]   // nothing beside the fourth
    [InlineData(3, 4, 3, 0, -1, 0)]
    // Three Pokémon in the two-column Pokémon menu
    [InlineData(0, 3, 2, 1, 0, 1)]
    [InlineData(0, 3, 2, 0, 1, 2)]
    [InlineData(2, 3, 2, 1, 0, 2)]
    [InlineData(1, 3, 2, 0, 1, 1)]
    // One Pokémon: the cursor has nowhere to go
    [InlineData(0, 1, 2, 1, 0, 0)]
    [InlineData(0, 1, 2, 0, -1, 0)]
    public void GridCursorsWrapAndSkipEmptySlots(int index, int count, int columns, int dx, int dy, int expected)
    {
        Assert.Equal(expected, UiNav.Grid(index, count, columns, dx, dy));
    }

    [Fact]
    public void ThePartyCursorMovesOverTwoColumnsAndStepsThroughTheTeamInTheSummary()
    {
        var screen = new PartyScreen();
        screen.Open();
        screen.MoveCursor(1, 0, 4);
        Assert.Equal(1, screen.SelectedIndex);
        screen.MoveCursor(0, 1, 4);
        Assert.Equal(3, screen.SelectedIndex);

        screen.ShowSummary = true;
        screen.MoveCursor(0, 1, 4);
        Assert.Equal(0, screen.SelectedIndex);
        screen.MoveCursor(0, -1, 4);
        Assert.Equal(3, screen.SelectedIndex);
    }

    // ------------------------------------------------------------------ start menu

    private static StartMenu OpenMenu()
    {
        var menu = new StartMenu();
        menu.Open();
        return menu;
    }

    [Fact]
    public void TheStartMenuKeepsPlatinumsOrderThenQuit()
    {
        var expected = new[]
        {
            StartMenuChoice.Pokedex, StartMenuChoice.Pokemon, StartMenuChoice.Bag, StartMenuChoice.Trainer,
            StartMenuChoice.Save, StartMenuChoice.Options
        };
        for (int i = 0; i < expected.Length; i++)
        {
            var menu = OpenMenu();
            for (int step = 0; step < i; step++) menu.Move(1);
            Assert.Equal(expected[i], menu.Confirm());
        }
        Assert.Equal(expected.Length + 2, new StartMenu().EntryCount);
    }

    [Fact]
    public void TheStartMenuCursorWrapsAndCloseAndCancelPutItAway()
    {
        var menu = OpenMenu();
        menu.Move(-1);
        Assert.Equal(menu.EntryCount - 1, menu.SelectedIndex);
        menu.Move(1);
        Assert.Equal(0, menu.SelectedIndex);

        // CLOSE is the entry before the last
        menu.Move(-2);
        Assert.Equal(StartMenuChoice.None, menu.Confirm());
        Assert.False(menu.IsActive);

        menu = OpenMenu();
        menu.Cancel();
        Assert.False(menu.IsActive);
    }

    [Fact]
    public void QuittingAsksFirstAndStayingIsTheDefault()
    {
        var menu = OpenMenu();
        menu.Move(-1);
        Assert.Equal(StartMenuChoice.None, menu.Confirm());
        Assert.True(menu.AskingToQuit);
        Assert.Equal(0, menu.QuitIndex);

        // The default answer keeps playing, and so does the B button; the menu stays open either way
        Assert.Equal(StartMenuChoice.None, menu.Confirm());
        Assert.False(menu.AskingToQuit);
        Assert.True(menu.IsActive);
        menu.Confirm();
        menu.Cancel();
        Assert.False(menu.AskingToQuit);
        Assert.True(menu.IsActive);
    }

    [Fact]
    public void TheQuitPromptOffersToSaveFirst()
    {
        Assert.Equal(new[] { "KEEP PLAYING", "SAVE AND QUIT", "QUIT" }, StartMenu.QuitAnswers);

        var menu = OpenMenu();
        menu.Move(-1);
        menu.Confirm();
        menu.Move(1);
        Assert.Equal(StartMenuChoice.SaveAndQuit, menu.Confirm());

        menu = OpenMenu();
        menu.Move(-1);
        menu.Confirm();
        menu.Move(1);
        menu.Move(1);
        menu.Move(1); // stops at the last answer
        Assert.Equal(2, menu.QuitIndex);
        Assert.Equal(StartMenuChoice.Quit, menu.Confirm());
    }

    // ------------------------------------------------------------------ sign and notices

    [Fact]
    public void TheLocationSignDropsInHoldsAndLeaves()
    {
        var sign = new LocationSign();
        Assert.False(sign.Visible);

        sign.Show("Route 201");
        Assert.True(sign.Visible);
        Assert.Equal(0f, sign.Shown, 4);

        for (float t = 0f; t < LocationSign.SlideTime + 0.05f; t += Step) sign.Update(Step);
        Assert.Equal(1f, sign.Shown, 4);
        Assert.Equal("Route 201", sign.Name);

        for (float t = 0f; t < LocationSign.HoldTime + LocationSign.SlideTime * 0.5f; t += Step) sign.Update(Step);
        Assert.InRange(sign.Shown, 0f, 0.99f);

        for (float t = 0f; t < LocationSign.SlideTime; t += Step) sign.Update(Step);
        Assert.False(sign.Visible);

        sign.Show("Twinleaf Town");
        sign.Hide();
        Assert.False(sign.Visible);
    }

    [Fact]
    public void ANoticeReplacingAnotherDoesNotSlideInAgain()
    {
        var toast = new Toast();
        toast.Show("Game saved.");
        for (float t = 0f; t < 1f; t += Step) toast.Update(Step);
        Assert.Equal(1f, toast.Shown, 4);

        toast.Show("Sound off");
        Assert.Equal("Sound off", toast.Message);
        Assert.Equal(1f, toast.Shown, 4);

        for (float t = 0f; t < Toast.HoldTime + Toast.SlideTime + 0.1f; t += Step) toast.Update(Step);
        Assert.False(toast.Visible);
    }

    [Fact]
    public void DialogueShowsTheSpeakerOnTheTagNotInTheLine()
    {
        var dialogue = new PokemonPlatinumEngine.Overworld.DialogueManager();
        dialogue.ShowDialogue("Barry", new[] { "Barry: Hey! You're late!", "See you at the lake." });
        Assert.Equal("Hey! You're late!", dialogue.CurrentLine);

        // Only a prefix that names the speaker is dropped
        dialogue.ShowDialogue("Sign", "Route 201: Sandgem Town ahead.");
        Assert.Equal("Route 201: Sandgem Town ahead.", dialogue.CurrentLine);
    }

    // ------------------------------------------------------------------ colours and icons

    [Fact]
    public void PlacesAndBarsUseTheStyleGuidesColours()
    {
        Assert.Equal(ModernUi.Green, ModernUi.PlaceColor("Route 201"));
        Assert.Equal(ModernUi.Blue, ModernUi.PlaceColor("Lake Verity"));
        Assert.Equal(ModernUi.Gold, ModernUi.PlaceColor("Twinleaf Town"));
        Assert.Equal(ModernUi.Gold, ModernUi.PlaceColor("Jubilife City"));

        // HP: green above half, amber above a fifth, red below
        Assert.NotEqual(ModernUi.HpColor(0.51f), ModernUi.HpColor(0.5f));
        Assert.NotEqual(ModernUi.HpColor(0.21f), ModernUi.HpColor(0.2f));
        Assert.Equal(ModernUi.HpColor(1f), ModernUi.HpColor(0.51f));

        // Stat bars step through four colours as the base stat rises
        Assert.Equal(4, new[] { 30, 60, 90, 130 }.Select(ModernUi.StatColor).Distinct().Count());
    }

    [Fact]
    public void EveryItemHasAnIconThatFillsItsFrame()
    {
        ItemDatabase.Initialize();
        foreach (var item in ItemDatabase.GetAll().Where(i => i.Pocket != ItemPocket.PokeBalls))
        {
            var icon = PixelArtGenerator.ItemIcon(item);
            Assert.Equal(20, icon.Width);
            Assert.Equal(20, icon.Height);
            int opaque = 0;
            for (int y = 0; y < icon.Height; y++)
                for (int x = 0; x < icon.Width; x++)
                    if (icon.IsOpaque(x, y)) opaque++;
            Assert.True(opaque > 80, $"{item.Name}: only {opaque} pixels drawn");
        }
    }
}
