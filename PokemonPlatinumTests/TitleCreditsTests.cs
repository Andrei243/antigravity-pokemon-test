using System;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 16 · T1: the title's CREDITS reads the repository's two records into sections and lines the typeface can
/// draw, opens from the title's row between OPTIONS and QUIT, scrolls and jumps by section, and goes back to the title.
/// </summary>
[Collection("Shared clocks and dice")]
public class TitleCreditsTests
{
    private const float Frame = 1f / 60f;

    [Fact]
    public void TheBuildCarriesBothRecordsAndTheyAreTheRepositorysOwn()
    {
        foreach (var name in TitleCredits.Files)
            Assert.True(File.Exists(Path.Combine(TitleCredits.Folder, name)), $"{name} is not beside the build");
        string repo = Repository.Root;
        Assert.Equal(File.ReadAllText(Path.Combine(repo, "docs", "art", "CREDITS.md")), File.ReadAllText(Path.Combine(TitleCredits.Folder, "CREDITS.md")));
        Assert.Equal(File.ReadAllText(Path.Combine(repo, "THIRD-PARTY-NOTICES.md")), File.ReadAllText(Path.Combine(TitleCredits.Folder, "THIRD-PARTY-NOTICES.md")));
        Assert.True(File.Exists(Path.Combine(repo, "LICENSE")));
        Assert.Contains("MIT License", File.ReadAllText(Path.Combine(repo, "LICENSE")));
    }

    [Fact]
    public void TheRecordsReadIntoSectionsWithNoneEmptyAndNothingTheTypefaceCantDraw()
    {
        var sections = TitleCredits.Read();
        Assert.True(sections.Count >= 6, $"only {sections.Count} sections");
        Assert.All(sections, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.Heading));
            Assert.NotEmpty(s.Lines);
            Assert.All(s.Lines, l => Assert.False(string.IsNullOrWhiteSpace(l)));
        });
        // What the records name is there: the typeface, the data's sources and the licences
        string all = string.Join("\n", sections.SelectMany(s => s.Lines.Prepend(s.Heading)));
        foreach (var word in new[] { "Nunito", "SIL Open Font License", "PokeAPI", "pret", "Pokémon Showdown", "MIT License" })
            Assert.Contains(word, all);
        // Plain text: no Markdown's marks left
        Assert.DoesNotContain("**", all);
        Assert.DoesNotContain("`", all);
        Assert.DoesNotContain("|", all);
        Assert.DoesNotContain("](", all);

        var screen = new TitleCreditsScreen();
        screen.Open();
        foreach (var row in screen.Rows)
        {
            var weight = row.Kind == CreditsRowKind.Heading ? UiWeight.Black : UiWeight.ExtraBold;
            Assert.True(TextMetrics.Undrawable(row.Text, weight).Count == 0,
                $"'{row.Text}' has characters the typeface can't draw: {string.Join(" ", TextMetrics.Undrawable(row.Text, weight))}");
            float size = row.Kind == CreditsRowKind.Heading ? TitleCreditsScreen.HeadingSize : TitleCreditsScreen.TextSize;
            Assert.True(TextMetrics.Measure(row.Text, size, weight) <= TitleCreditsScreen.TextWidth + 0.5f, $"'{row.Text}' is wider than the panel");
        }
    }

    [Fact]
    public void MarkdownIsReadAsTheScreenShowsIt()
    {
        const string md = "# Title\r\n\r\nFirst line\r\nand its second.\r\n\r\n## Fonts\r\n\r\n| Asset | Files | Licence |\r\n|---|---|---|\r\n| **Nunito** | `a.ttf` | OFL (<https://x.org>) |\r\n\r\n- an [item](http://y)\r\n\r\n```\r\nPermission is granted\r\nto all.\r\n\r\nNo warranty.\r\n```\r\n";
        var sections = TitleCredits.Parse(md);
        Assert.Equal(new[] { "Title", "Fonts" }, sections.Select(s => s.Heading));
        Assert.Equal(new[] { "First line and its second." }, sections[0].Lines);
        Assert.Equal(new[]
        {
            "Asset: Nunito · Files: a.ttf · Licence: OFL (https://x.org)",
            "an item",
            "Permission is granted to all.",
            "No warranty."
        }, sections[1].Lines);
    }

    [Fact]
    public void AFolderWithoutTheRecordsShowsTheProjectsOwnSectionAlone()
    {
        string folder = Path.Combine(Path.GetTempPath(), "credits-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            Assert.Empty(TitleCredits.Read(folder));
            var screen = new TitleCreditsScreen();
            screen.Open(folder);
            Assert.Single(screen.Sections);
            Assert.Equal(TitleCreditsScreen.OwnWork().Heading, screen.Sections[0].Heading);
            Assert.Equal(0, screen.LastFirst);
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [Fact]
    public void TheFirstSectionIsTheProjectsOwnAndTheListScrollsAndJumpsBySection()
    {
        var screen = new TitleCreditsScreen();
        screen.Open();
        Assert.True(screen.IsActive);
        Assert.Equal(TitleCreditsScreen.OwnWork().Heading, screen.Sections[0].Heading);
        Assert.Equal(CreditsRowKind.Heading, screen.Rows[0].Kind);
        Assert.True(screen.Rows.Count > TitleCreditsScreen.VisibleRows, "the records should be more than a panel");

        // Up at the top does nothing; down a row; never past the last panel
        screen.Move(-1);
        Assert.Equal(0, screen.First);
        screen.Move(1);
        Assert.Equal(1, screen.First);
        for (int i = 0; i < screen.Rows.Count + 5; i++) screen.Move(1);
        Assert.Equal(screen.LastFirst, screen.First);

        // From the top, each jump lands on the next section's heading (or the end of the list)
        screen.Open();
        screen.Jump(1);
        Assert.Equal(CreditsRowKind.Heading, screen.Rows[screen.First].Kind);
        Assert.Equal(1, screen.Rows[screen.First].Section);
        screen.Move(1);
        // Back from part way into a section goes to its own heading, then to the one before
        screen.Jump(-1);
        Assert.Equal(1, screen.Rows[screen.First].Section);
        Assert.Equal(CreditsRowKind.Heading, screen.Rows[screen.First].Kind);
        screen.Jump(-1);
        Assert.Equal(0, screen.First);

        screen.Cancel();
        Assert.False(screen.IsActive);
    }

    [Fact]
    public void TheTitleOffersCreditsBetweenOptionsAndQuit()
    {
        var title = new TitleScreen(null);
        var entries = title.Entries.ToList();
        Assert.Equal(entries.IndexOf(TitleChoice.Options) + 1, entries.IndexOf(TitleChoice.Credits));
        Assert.Equal(entries.IndexOf(TitleChoice.Quit) - 1, entries.IndexOf(TitleChoice.Credits));
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        title.Move(-2);
        title.PressConfirm();
        Assert.Equal(TitleChoice.Credits, title.TakeChoice());
        Assert.Equal(TitleChoice.None, title.TakeChoice());
        Assert.Equal(TitlePhase.Menu, title.Phase);
    }

    [Fact]
    public void AHeadlessGameReachesTheCreditsFromTheTitleAndComesBack()
    {
        var engine = new GameEngine(new GameSettings { Muted = true });
        try
        {
            engine.InitializeHeadless();
            var game = engine.Drive;
            Assert.Equal(GameState.Title, game.State);
            for (int i = 0; i < 6 && game.Title.Phase != TitlePhase.Menu; i++) Run(engine, GameAction.Confirm);
            Assert.Equal(TitlePhase.Menu, game.Title.Phase);
            while (game.Title.Entries[game.Title.SelectedIndex] != TitleChoice.Credits) Run(engine, GameAction.Up);
            Run(engine, GameAction.Confirm);
            Assert.Equal(GameState.TitleCredits, game.State);
            Assert.True(game.TitleCredits.IsActive);

            Run(engine, GameAction.Down);
            Assert.Equal(1, game.TitleCredits.First);
            Run(engine, GameAction.Right);
            Assert.Equal(1, game.TitleCredits.Section);

            Run(engine, GameAction.Cancel);
            Assert.Equal(GameState.Title, game.State);
            Assert.False(game.TitleCredits.IsActive);
            Assert.Equal(TitlePhase.Menu, game.Title.Phase);
            Assert.Equal(TitleChoice.Credits, game.Title.Entries[game.Title.SelectedIndex]);
        }
        finally
        {
            InputManager.Feed(null);
            InputManager.Reset();
            engine.Close();
        }
    }

    private static void Run(GameEngine engine, GameAction action)
    {
        InputManager.Feed(ActionFrame.Press(action));
        engine.Update(Frame);
        InputManager.Feed(ActionFrame.None);
        engine.Update(Frame);
    }
}
