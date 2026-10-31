using System;
using System.IO;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The run timer (plan 13 · V10): run every frame the game is played, paused on the title, in the options and behind
/// another window; its splits taken whenever the story has moved; its corner drawn over the field and the battle and
/// <c>splits.json</c> written beside the save only while the options' RUN TIMER row names a category.
/// </summary>
public partial class GameEngine
{
    /// <summary>The story's revision the splits were last looked for at; -1 to look at once (a game begun or loaded).</summary>
    private long timerLookedAt = -1;

    /// <summary>The timer of a save from before it: begun at the save's play time, what the story had reached kept untimed.</summary>
    internal static RunTimer RunTimerBegunLate(float playTime, StoryState story)
    {
        var timer = new RunTimer { BegunLate = true, RealSeconds = playTime, GameSeconds = playTime };
        timer.Know(story);
        return timer;
    }

    /// <summary>The states the timer stands still in: the title and its screens, the options, the close button's question.</summary>
    internal static bool TimerStandsStill(GameState state) => state is GameState.Title or GameState.Options or GameState.WindowClosing
        or GameState.Jukebox or GameState.TitleCredits or GameState.FirstStart or GameState.Migrate or GameState.Intro;

    /// <summary>
    /// A frame of the run: the timer moves on (real time from <see cref="WallClock.UtcNow"/>, never the system's clock),
    /// and when the story has moved the splits it has reached are taken; one the chosen category counts writes the file.
    /// </summary>
    private void KeepTheTimer(float dt)
    {
        if (flow.RunState.Timer is not { } timer) return;
        if (!gameStarted || TimerStandsStill(flow.State))
        {
            timer.Pause();
            return;
        }
        timer.Tick(dt, WallClock.UtcNow);
        TakeSplits();
    }

    /// <summary>Takes the splits the story has reached, if it has moved since the last look; writes the file when one counts.</summary>
    internal void TakeSplits()
    {
        if (flow.RunState.Timer is not { } timer || flow.Story.Revision == timerLookedAt) return;
        timerLookedAt = flow.Story.Revision;
        var taken = timer.Take(flow.Story);
        if (taken.Count == 0 || Settings.RunTimer == TimerCategory.Off) return;
        foreach (var point in taken)
            if (point.CountsIn(Settings.RunTimer))
            {
                WriteSplits();
                break;
            }
    }

    /// <summary><c>splits.json</c> beside the save, written whole (plan 13 · V10); a file that can't be written is told in the log and the game goes on.</summary>
    internal void WriteSplits()
    {
        if (flow.RunState.Timer is not { } timer || Settings.RunTimer == TimerCategory.Off) return;
        string path = SavePaths.Current.Splits;
        try
        {
            File.WriteAllText(path, SplitsFile.Of(timer, Settings.RunTimer, flow.SaveSlot, PlayerIdentity.Name).ToJson());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"The run timer's splits couldn't be written to {path}: {e.Message}");
        }
    }

    /// <summary>What the timer reads now for the Hall of Fame's record: the region's own split if taken, else the timer as it stands.</summary>
    internal (double Real, double Game)? TimeForTheHallOfFame()
    {
        if (flow.RunState.Timer is not { } timer) return null;
        TakeSplits();
        string id = RunTimer.HallOfFameId(Here.Id);
        foreach (var split in timer.Splits)
            if (split.Id == id) return (split.RealSeconds, split.GameSeconds);
        return (timer.RealSeconds, timer.GameSeconds);
    }

    /// <summary>Whether a panel that counts something stands at the top right of the field, which the timer's corner goes under.</summary>
    private bool ACountStandsTopRight =>
        flow.State is GameState.Overworld or GameState.Dialogue
        && (flow.Safari.Active || (flow.BugContest.Active && !flow.BugContest.Judged && flow.Map.Name == BugContestPark)
            || (InPalPark && flow.Pastimes.PalPark.Show != null) || (showBattlePoints && ScriptRunning) || (showCastlePoints && ScriptRunning));

    /// <summary>The corner (style guide, "Field menus and notices", "The run timer"): with the row on, over the field and over a battle's commands and lines.</summary>
    private void DrawTheTimer()
    {
        if (Settings.RunTimer == TimerCategory.Off || !gameStarted || flow.RunState.Timer is not { } timer) return;
        if (flow.State == GameState.Battle && flow.Battle?.HUD.MenuState is not (PokemonPlatinumEngine.Battle.BattleMenuState.Main or PokemonPlatinumEngine.Battle.BattleMenuState.Message or PokemonPlatinumEngine.Battle.BattleMenuState.Moves))
            return;
        var finish = timer.Finish;
        var last = timer.Last(Settings.RunTimer);
        ModernUi.RunTimerCorner(VirtualWidth, ACountStandsTopRight ? ModernUi.TimerCornerLow : ModernUi.TimerCornerTop,
            RunTimer.Format(finish?.RealSeconds ?? timer.RealSeconds), RunTimer.Format(finish?.GameSeconds ?? timer.GameSeconds, tenths: false),
            last is { } l ? (l.Point.Shown, RunTimer.Format(l.Split.RealSeconds, tenths: false)) : null, finish != null);
    }
}
