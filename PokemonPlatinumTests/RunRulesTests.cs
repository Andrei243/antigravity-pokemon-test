using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 13 · V1: the ways to play chosen at NEW GAME. The record, its presets, the panel that builds it (GPU-free), the
/// title's step, the save that keeps it and the card that shows it. V1 enforces nothing, so these hold only what is
/// asked, kept and shown; Normal changes nothing.
/// </summary>
[Collection("Shared clocks and dice")]
public class RunRulesTests
{
    private static TitleScreen AtMenu()
    {
        var title = new TitleScreen((SaveData?)null);
        title.PressConfirm();
        title.PressConfirm();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        return title;
    }

    private static void To(RunPanel panel, RunRow row)
    {
        for (int guard = 0; guard < 40 && panel.SelectedRow != row; guard++) panel.Move(1);
        Assert.Equal(row, panel.SelectedRow);
    }

    [Fact]
    public void NormalIsTheDefaultAndNothingElseIsNormal()
    {
        Assert.True(new RunRules().IsNormal);
        Assert.True(RunRules.Normal.IsNormal);
        Assert.Equal(Party.MaxSize, RunRules.Normal.PartyLimit);
        Assert.Equal(0, RunRules.Normal.ShinyDenominator);
        Assert.False(RunRules.NuzlockePreset.IsNormal);
        Assert.False(RunRules.Hardcore.IsNormal);
        Assert.False((RunRules.Normal with { NoRunning = true }).IsNormal);
        Assert.False((RunRules.Normal with { Monotype = PokemonType.Fire }).IsNormal);
        Assert.Equal(new[] { "normal", "nuzlocke", "hardcore" }, RunRules.Presets.Select(p => p.Key));
        Assert.Equal("custom", (RunRules.NuzlockePreset with { NoCatching = true }).PresetKey);
    }

    [Fact]
    public void ARunRoundTripsThroughASaveAndNormalWritesNothing()
    {
        var run = RunRules.Hardcore with { Monotype = PokemonType.Water, PartyLimit = 3, ShinyDenominator = 1024 };
        var save = new SaveData { PlayerName = "Lucas", Run = run };
        var back = SaveManager.Read(SaveManager.Write(save))!;
        Assert.Equal(run, back.Run);
        Assert.Contains("\"Water\"", SaveManager.Write(save));

        // Normal is no field at all, so a game played as Platinum's saves what it always saved
        Assert.DoesNotContain("\"Run\"", SaveManager.Write(new SaveData { PlayerName = "Lucas" }));
        // And a save from before the choice reads as Normal
        var old = SaveManager.Read("{\"PlayerName\":\"Lucas\"}")!;
        Assert.Null(old.Run);
    }

    [Fact]
    public void TheTitleAsksTheWaysToPlayAfterNewGameAndNormalLeavesAtOnce()
    {
        var title = AtMenu();
        title.PressConfirm();
        Assert.Equal(TitlePhase.ChooseRun, title.Phase);
        Assert.True(title.Run.Rules.IsNormal);
        Assert.Equal(RunRow.Preset, title.Run.SelectedRow);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        Assert.True(title.ChosenRun.IsNormal);
    }

    [Fact]
    public void BGoesBackToTheMenuAndALaterNewGameStartsFromNormalAgain()
    {
        var title = AtMenu();
        title.PressConfirm();
        title.Change(1);
        Assert.False(title.Run.Rules.IsNormal);
        title.PressCancel();
        Assert.Equal(TitlePhase.Menu, title.Phase);
        title.PressConfirm();
        Assert.Equal(TitlePhase.ChooseRun, title.Phase);
        Assert.True(title.Run.Rules.IsNormal);
    }

    [Fact]
    public void ThePresetRowStepsThroughThePresetsAndConfirmStartsWithTheOneShown()
    {
        var title = AtMenu();
        title.PressConfirm();
        title.Change(1);
        Assert.Equal(RunRules.NuzlockePreset, title.Run.Rules);
        title.Change(1);
        Assert.Equal(RunRules.Hardcore, title.Run.Rules);
        title.Change(1);
        Assert.True(title.Run.Rules.IsNormal);
        title.Change(-1);
        Assert.Equal(RunRules.Hardcore, title.Run.Rules);
        title.PressConfirm();
        Assert.Equal(TitlePhase.Leaving, title.Phase);
        Assert.Equal(RunRules.Hardcore, title.ChosenRun);
    }

    [Fact]
    public void AnEditedRowMakesTheRunCustomUntilItMatchesAPresetAgain()
    {
        var panel = new RunPanel();
        panel.Open();
        To(panel, RunRow.NoRunning);
        panel.Change(1);
        Assert.True(panel.Rules.NoRunning);
        Assert.Equal("custom", panel.Rules.PresetKey);
        // (Confirm on a row of a run that isn't Normal changes it as Right does; START starts)
        panel.Confirm();
        Assert.False(panel.Done);
        Assert.False(panel.Rules.NoRunning);
        Assert.True(panel.Rules.IsNormal);
        // (and on a run that is Normal, Confirm anywhere starts it)
        panel.Confirm();
        Assert.True(panel.Done);
        panel.Open();
        To(panel, RunRow.NoRunning);
        panel.Change(1);
        panel.Change(-1);
        Assert.True(panel.Rules.IsNormal);
        To(panel, RunRow.Start);
        panel.Confirm();
        Assert.True(panel.Done);
    }

    [Fact]
    public void TheNuzlockesClausesShowOnlyWhileItIsOn()
    {
        var panel = new RunPanel();
        panel.Open();
        Assert.DoesNotContain(RunRow.DupesClause, panel.Rows);
        To(panel, RunRow.Nuzlocke);
        panel.Change(1);
        Assert.Contains(RunRow.DupesClause, panel.Rows);
        Assert.Contains(RunRow.ShinyClause, panel.Rows);
        Assert.Contains(RunRow.GiftsCount, panel.Rows);
        panel.Change(1);
        Assert.False(panel.Rules.Nuzlocke);
        Assert.DoesNotContain(RunRow.ShinyClause, panel.Rows);
        Assert.True(panel.Rules.IsNormal);
    }

    [Fact]
    public void TheValueRowsStepWithinTheirLimitsAndEveryRowHasWords()
    {
        var panel = new RunPanel();
        panel.Open();
        To(panel, RunRow.PartyLimit);
        for (int i = 0; i < 9; i++) panel.Change(-1);
        Assert.Equal(1, panel.Rules.PartyLimit);
        for (int i = 0; i < 9; i++) panel.Change(1);
        Assert.Equal(6, panel.Rules.PartyLimit);

        To(panel, RunRow.Monotype);
        Assert.Equal(Text.Of("run.none"), panel.ValueText(RunRow.Monotype));
        panel.Change(1);
        Assert.Equal(PokemonType.Normal, panel.Rules.Monotype);
        panel.Change(-1);
        panel.Change(-1);
        Assert.Equal(PokemonType.Fairy, panel.Rules.Monotype);
        panel.Change(1);
        Assert.Null(panel.Rules.Monotype);

        To(panel, RunRow.ShinyOdds);
        panel.Change(1);
        Assert.Equal(4096, panel.Rules.ShinyDenominator);
        panel.Change(-1);
        Assert.True(panel.Rules.IsNormal);

        To(panel, RunRow.Difficulty);
        panel.Change(1);
        Assert.Equal(Difficulty.Challenge, panel.Rules.Difficulty);
        panel.Change(1);
        Assert.Equal(Difficulty.Challenge, panel.Rules.Difficulty);

        foreach (RunRow row in Enum.GetValues<RunRow>())
        {
            Assert.NotEqual($"run.row.{row}", Text.Of($"run.row.{row}"));
            Assert.NotEqual($"run.help.{row}", Text.Of($"run.help.{row}"));
        }
    }

    [Fact]
    public void ThePanelScrollsItsWindowWithTheCursor()
    {
        var panel = new RunPanel();
        panel.Open();
        To(panel, RunRow.Nuzlocke);
        panel.Change(1);
        Assert.True(panel.Rows.Count > RunPanel.VisibleRows);
        To(panel, RunRow.Start);
        Assert.Equal(panel.Rows.Count - RunPanel.VisibleRows, panel.First);
        panel.Move(1);
        Assert.Equal(0, panel.Selected);
        Assert.Equal(0, panel.First);
    }

    [Fact]
    public void TheMarkIsForAGameThatIsNotNormal()
    {
        Assert.Null(ModernUi.RunMark(null));
        Assert.Null(ModernUi.RunMark(RunRules.Normal));
        Assert.Equal(Text.Of("run.preset.nuzlocke"), ModernUi.RunMark(RunRules.NuzlockePreset));
        Assert.Equal(Text.Of("run.preset.custom"), ModernUi.RunMark(RunRules.Normal with { NoCenter = true }));
    }

    [Fact]
    public void TheCardHasAPageForTheRunOnlyWhenThereIsOneAndItComesAfterTheBack()
    {
        var plain = new TrainerCardScreen();
        plain.Open();
        Assert.False(plain.HasRunPage);
        Assert.False(plain.Turn(1));

        var card = new TrainerCardScreen();
        card.Open(regionPages: 0, hasRun: true);
        Assert.True(card.Turn(1));
        Assert.Equal(CardPage.Back, card.Page);
        Assert.True(card.Turn(1));
        Assert.Equal(CardPage.Run, card.Page);
        Assert.False(card.Turn(1));
        card.Flip();
        Assert.Equal(CardPage.Front, card.Page);

        // With regions too, theirs follow the run's
        var both = new TrainerCardScreen();
        both.Open(regionPages: 2, hasRun: true);
        both.Turn(1);
        both.Turn(1);
        Assert.Equal(CardPage.Run, both.Page);
        both.Turn(1);
        Assert.Equal((CardPage.Region, 0), (both.Page, both.RegionShown));
        both.Turn(1);
        Assert.Equal((CardPage.Region, 1), (both.Page, both.RegionShown));
        Assert.False(both.Turn(1));
    }

    [Fact]
    public void AGameStartedWithARunKeepsItThroughASaveAndAContinue()
    {
        string folder = Path.Combine(Path.GetTempPath(), "run-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var paths = SavePaths.Current;
        try
        {
            SavePaths.Current = SavePaths.In(folder);
            var engine = new GameEngine(new GameSettings { Muted = true });
            engine.InitializeHeadless();
            engine.StartNewGame("Lucas", PlayerLook.Boy, null, RunRules.NuzlockePreset);
            Assert.Equal(RunRules.NuzlockePreset, engine.Drive.Run);
            Assert.Equal(RunRules.NuzlockePreset, RunRules.Current);
            Assert.Equal(RunRules.NuzlockePreset, engine.Drive.BuildSave().Run);
            Assert.True(engine.Drive.Save());
            int slot = engine.Drive.SaveSlot;

            // A game begun as Normal in the same process puts Normal back, and saves no field
            engine.StartNewGame("Lucas", PlayerLook.Boy);
            Assert.True(engine.Drive.Run.IsNormal);
            Assert.True(RunRules.Current.IsNormal);
            Assert.Null(engine.Drive.BuildSave().Run);

            var again = new GameEngine(new GameSettings { Muted = true });
            again.InitializeHeadless();
            again.ContinueGame(slot);
            for (int i = 0; i < 600 && again.Drive.State != GameState.Overworld; i++) again.Update(1f / 60f);
            Assert.Equal(RunRules.NuzlockePreset, again.Drive.Run);
            Assert.Equal(RunRules.NuzlockePreset, RunRules.Current);
        }
        finally
        {
            RunRules.Current = RunRules.Normal;
            Ruleset.Use(RulesPreset.Platinum);
            SavePaths.Current = paths;
            try { Directory.Delete(folder, recursive: true); } catch (IOException) { }
        }
    }
}
