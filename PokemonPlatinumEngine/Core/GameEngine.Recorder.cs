using System;
using System.IO;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Vs. Recorder (plan 08 · P10; design "Recorded battles"): a trainer's battle, once the field is back, ends with
/// the question "Record this battle?" when the Vs. Recorder is in the bag and the options' Record battles row is on, and
/// the battles of the Elite Four and the Champion are kept without a word whenever it is in the bag, as the plan says
/// of the Hall of Fame's. A recording is a <see cref="BattleRecordFile"/> in the <c>replays</c> folder beside the saves
/// (<see cref="SavePaths.Replays"/>), never in the working directory. A Frontier battle is not offered (its own end,
/// <c>EndFrontierBattle</c>, never comes here), nor a wild one.
/// </summary>
public partial class GameEngine
{
    /// <summary>Where recordings are written: the folder beside the saves unless a tool or a test names another.</summary>
    internal string ReplaysFolder
    {
        get => replaysFolder ?? SavePaths.Current.Replays;
        set => replaysFolder = value;
    }

    private string? replaysFolder;

    /// <summary>The file the last recording was written to, or null if nothing has been recorded this run.</summary>
    internal string? LastRecorded { get; private set; }

    /// <summary>
    /// Whether a battle's trainers are the Hall of Fame's (the Elite Four's or the Champion's, rematches too), whose
    /// battles are kept whatever the row says.
    /// </summary>
    internal static bool IsLeagueBattle(BattleEngine battle) =>
        battle.Trainers.Count > 0 && BattleMeeting.ClassRow(battle.Trainers[0].TrainerClass) is MeetingRow.EliteFour or MeetingRow.Champion;

    private bool HasTheRecorder => ItemDatabase.Get(BattleRecordFile.RecorderItem) is { } recorder && flow.Bag.GetQuantity(recorder) > 0;

    /// <summary>
    /// As a battle ends: a league battle is written at once, any other trainer's battle is kept waiting for the
    /// question while the row is on. Nothing is drawn from the dice and nothing of the battle is changed.
    /// </summary>
    private void KeepTheRecord(BattleEngine? battle)
    {
        flow.RecordOffered = null;
        flow.RecordAsked = false;
        if (battle == null || !battle.IsTrainerBattle || battle.IsFrontier || !HasTheRecorder) return;
        bool league = IsLeagueBattle(battle);
        if (!league && !Settings.RecordBattles) return;
        var file = battle.RecordFile(GameClock.Today);
        if (file == null) return;
        if (league) WriteRecord(file);
        else flow.RecordOffered = file;
    }

    /// <summary>
    /// Once the field is back, the question about the battle just fought: its answers beside its line, as a script's
    /// question has them. True while it takes the field.
    /// </summary>
    private bool AskToRecord()
    {
        if (flow.RecordOffered == null || flow.RecordAsked) return false;
        flow.RecordAsked = true;
        flow.Choice.Dismiss();
        flow.PendingAnswers = new[] { Text.Of("scriptLines.yes"), Text.Of("scriptLines.no") };
        flow.PendingCancel = 1;
        flow.Dialogue.ShowQuestion("", Text.Of("engine.recordBattle.question"));
        flow.State = GameState.Dialogue;
        return true;
    }

    /// <summary>The answer to the question: YES writes the file and says so; NO lets the battle go.</summary>
    private void AnswerTheRecord(bool yes)
    {
        var file = flow.RecordOffered;
        flow.RecordOffered = null;
        flow.RecordAsked = false;
        if (!yes || file == null) return;
        bool kept = WriteRecord(file);
        flow.Dialogue.ShowDialogue("", Text.Of(kept ? "engine.recordBattle.saved" : "engine.recordBattle.notSaved"));
        flow.State = GameState.Dialogue;
    }

    /// <summary>Writes a recording into <see cref="ReplaysFolder"/>; false (and a line in the log) where it can't.</summary>
    private bool WriteRecord(BattleRecordFile file)
    {
        try
        {
            LastRecorded = file.WriteTo(ReplaysFolder);
            Log.Info($"Battle recorded: {LastRecorded}");
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warn($"The battle couldn't be recorded in {ReplaysFolder}: {e.Message}");
            return false;
        }
    }
}
