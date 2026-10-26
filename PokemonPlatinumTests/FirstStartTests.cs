using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T24, the first start: on the first run (no <c>settings.json</c>), a page of options comes before the
/// title; it lists only the options' rows that exist, a change on it is applied at once, and CONTINUE writes the
/// options so the page never comes again. The games here are headless and share the save folder, the language and
/// the keys with the whole process, so the class runs alone.
/// </summary>
[Collection("Shared clocks and dice")]
public class FirstStartTests
{
    private const float Frame = 1f / 60f;

    [Fact]
    public void ThePageListsOnlyRowsThatExistInItsOwnOrder()
    {
        Assert.Equal(new[] { OptionRow.ButtonMode, OptionRow.Language },
            FirstStartScreen.RowsThatExist(new[] { "ButtonMode", "NoSuchRow", "Language", "ButtonMode" }));

        var rows = new FirstStartScreen().Rows;
        Assert.NotEmpty(rows);
        Assert.All(rows, row => Assert.True(Enum.IsDefined(row)));
        // In the page's order, each a name it asks for
        var asked = FirstStartScreen.Wanted.ToList();
        Assert.Equal(rows.OrderBy(r => asked.IndexOf(r.ToString())), rows);
        // What exists today: the language first, the button preset, and the quality T23 will measure
        Assert.Equal(OptionRow.Language, rows[0]);
        Assert.Contains(OptionRow.ButtonMode, rows);
        Assert.Contains(OptionRow.Quality, rows);
        // Nothing that is no setting (HELP, the jukebox, the reset)
        Assert.DoesNotContain(OptionRow.Help, rows);
        Assert.DoesNotContain(OptionRow.ResetDefaults, rows);
    }

    [Fact]
    public void TheCursorWrapsThroughTheRowsAndContinue()
    {
        var page = new FirstStartScreen(new[] { OptionRow.Language, OptionRow.Quality });
        page.Open();
        Assert.Equal(OptionRow.Language, page.Selected);
        page.Move(1);
        page.Move(1);
        Assert.True(page.OnContinue);
        Assert.Null(page.Selected);
        page.Move(1);
        Assert.Equal(OptionRow.Language, page.Selected);
        page.Move(-1);
        Assert.True(page.OnContinue);

        // A on a row changes it as right does; A on CONTINUE ends the page, said once
        var settings = new GameSettings();
        page.Move(-1);
        var quality = settings.Quality;
        Assert.False(page.Confirm(settings));
        Assert.NotEqual(quality, settings.Quality);
        page.Move(1);
        Assert.True(page.Confirm(settings));
        Assert.False(page.IsActive);
        Assert.True(page.TakeContinue());
        Assert.False(page.TakeContinue());
    }

    [Fact]
    public void AFirstRunOpensOnThePageAChangeAppliesAtOnceAndContinueWritesTheSettings()
    {
        string folder = Path.Combine(Path.GetTempPath(), "first-start-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        AudioManager.Listen();
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            var settings = GameSettings.Load(SavePaths.Current.SettingsFile);
            Assert.True(settings.FirstRun);
            settings.Muted = true;

            var engine = new GameEngine(settings);
            engine.InitializeHeadless();
            var game = engine.Drive;
            Assert.Equal(GameState.FirstStart, game.State);
            Assert.True(game.FirstStart.IsActive);
            Assert.Equal(OptionRow.Language, game.FirstStart.Selected);

            // The language moved on the page is the language of the page at once, before CONTINUE
            Run(engine, GameAction.Right);
            if (Language.Offered.Count > 1)
            {
                Assert.NotEqual("en", settings.Language);
                Assert.Equal(settings.Language, Text.Current);
            }
            Assert.False(File.Exists(SavePaths.Current.SettingsFile));

            // Down to CONTINUE and A: the settings written, the title begun
            for (int i = 0; i < game.FirstStart.Rows.Count; i++) Run(engine, GameAction.Down);
            Assert.True(game.FirstStart.OnContinue);
            Run(engine, GameAction.Confirm);
            Assert.Equal(GameState.Title, game.State);
            Assert.True(File.Exists(SavePaths.Current.SettingsFile));
            Assert.Equal(settings.Language, GameSettings.Load(SavePaths.Current.SettingsFile).Language);
            engine.Close();

            // The second start finds the settings and never asks again
            var again = GameSettings.Load(SavePaths.Current.SettingsFile);
            Assert.False(again.FirstRun);
            again.Muted = true;
            var second = new GameEngine(again);
            second.InitializeHeadless();
            Assert.Equal(GameState.Title, second.Drive.State);
            second.Close();
        }
        finally
        {
            InputManager.Feed(null);
            InputManager.Reset();
            Text.Use("en");
            AudioManager.StopListening();
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }

    [Fact]
    public void OptionsMadeByAToolAreNoFirstRun()
    {
        // The harness and the tests make their own options, so `all` and every headless test never meet the page
        Assert.False(new GameSettings().FirstRun);
        var engine = new GameEngine(new GameSettings { Muted = true });
        engine.InitializeHeadless();
        Assert.Equal(GameState.Title, engine.Drive.State);
        engine.Close();
    }

    private static void Run(GameEngine engine, GameAction action)
    {
        InputManager.Feed(ActionFrame.Press(action));
        engine.Update(Frame);
        InputManager.Feed(ActionFrame.None);
        engine.Update(Frame);
    }
}
