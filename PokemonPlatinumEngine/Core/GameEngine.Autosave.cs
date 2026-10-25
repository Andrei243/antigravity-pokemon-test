using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core.Diagnostics;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumEngine.Core;

/// <summary>The moments the game writes an autosave at (plan 12 · Q3, the five the plan names).</summary>
public enum AutosaveMoment { Battle, Heal, Badge, Catch, Arrival }

public partial class GameEngine
{
    /// <summary>How long the little disc in the corner turns while the autosave is written (style guide, "Field menus and notices").</summary>
    public const float AutosaveDiscSeconds = 1.6f;

    /// <summary>
    /// The least play time between two autosaves that come of arriving in a place (every door is one): the other four
    /// moments are rare and always write.
    /// </summary>
    public const float AutosaveArrivalGap = 60f;

    // A moment has come and the write waits for the field to be free; when the last write was (in play time), and how
    // long its disc has been turning
    private AutosaveMoment? autosaveDue;
    private float lastAutosaveAt = float.NegativeInfinity;
    private float autosaveDisc = -1f;

    // What the title offered of an autosave after the last start: the slot it belongs to and its game
    private AutosaveOffer? titleAutosave;

    /// <summary>The autosaves this game has written since it was made (a tool or a test counts them).</summary>
    internal int AutosavesWritten { get; private set; }

    /// <summary>
    /// A moment worth an autosave has come: after a battle, a heal, a Badge, a catch, or on coming to a place. With the
    /// options' AUTOSAVE off nothing happens. The write is not made here but at the first moment the field is free
    /// (<see cref="KeepTheAutosave"/>): never in a battle, never while a script runs, never under a fade or a menu.
    /// </summary>
    internal void AutosaveSoon(AutosaveMoment moment)
    {
        if (!Settings.Autosave || !gameStarted) return;
        // Coming to a place is the most frequent moment, and is held to a gap; any other stands over it
        if (moment == AutosaveMoment.Arrival && flow.PlayTime - lastAutosaveAt < AutosaveArrivalGap) return;
        if (autosaveDue == null || moment != AutosaveMoment.Arrival) autosaveDue = moment;
    }

    /// <summary>Whether the field is free for a quiet write: the same moment the start menu's SAVE is, and no run that has ended.</summary>
    private bool CanAutosaveNow => CanSaveNow && !flow.RunState.Ended && !WaitingToBeNamed;

    // (A Pokémon caught under the Nuzlocke is named once the field is back; the keyboard is up before it)
    private bool WaitingToBeNamed => flow.NameAfterBattle != null;

    /// <summary>Called every frame: writes the autosave once its moment's turn has come, and turns the disc.</summary>
    private void KeepTheAutosave(float dt)
    {
        if (autosaveDisc >= 0f)
        {
            autosaveDisc += dt;
            if (autosaveDisc >= AutosaveDiscSeconds) autosaveDisc = -1f;
        }
        if (autosaveDue == null) return;
        if (!Settings.Autosave || !gameStarted)
        {
            autosaveDue = null;
            return;
        }
        if (!CanAutosaveNow) return;
        var moment = autosaveDue.Value;
        autosaveDue = null;
        WriteAutosave(moment);
    }

    private void WriteAutosave(AutosaveMoment moment)
    {
        bool written = AutosaveFile.Write(SavePaths.Current, flow.SaveSlot, BuildSave());
        if (written)
        {
            AutosavesWritten++;
            lastAutosaveAt = flow.PlayTime;
            autosaveDisc = 0f;
            Log.Info($"The game was autosaved ({moment}) for slot {flow.SaveSlot} at {PlaceKey()} {flow.Player.GridX},{flow.Player.GridY}.");
        }
        else Log.Warn($"The game couldn't be autosaved ({moment}).");
    }

    /// <summary>The little disc: turning in the corner while the autosave is written; nothing at other times (<see cref="ModernUi.AutosaveDisc"/>).</summary>
    private void DrawAutosaveDisc()
    {
        if (autosaveDisc >= 0f) ModernUi.AutosaveDisc(VirtualWidth, VirtualHeight, autosaveDisc / AutosaveDiscSeconds);
    }

    // ------------------------------------------------------------------ the title

    /// <summary>
    /// What the title offers after the slots are read: the autosave, if it is newer than its slot's game, or the only
    /// game of a slot never saved by hand (<see cref="AutosaveFile.Offer"/>). The cards are the slots', each with the
    /// autosave that stands beside it.
    /// </summary>
    private TitleSlot[] TitleCards()
    {
        titleAutosave = AutosaveFile.Offer(SavePaths.Current, titleSlots);
        var cards = titleSlots.Where(s => s.Taken)
            .Select(s => new TitleSlot(s.Number, s.Save, titleAutosave is { Orphan: false } offer && offer.Slot == s.Number ? offer.Save : null))
            .ToList();
        if (titleAutosave is { Orphan: true } orphan) cards.Add(new TitleSlot(orphan.Slot, orphan.Save, null, AutosaveOnly: true));
        if (titleAutosave != null)
            Log.Info($"An autosave for slot {titleAutosave.Slot} is offered{(titleAutosave.Orphan ? " (the slot holds no save of its own)" : ", newer than the slot's save")}.");
        else if (AutosaveFile.Exists(SavePaths.Current))
            Log.Info("An autosave file is there but is not offered: it can't be read, or it is not newer than the game saved beside it.");
        return cards.ToArray();
    }

    // The game of a slot's autosave, when the title's Continue was answered with it
    private SaveData? AutosaveFor(int slot) => titleAutosave is { } offer && offer.Slot == slot ? offer.Save : null;

    private sealed partial class FieldHost
    {
        public void Milestone(AutosaveMoment moment) => game.AutosaveSoon(moment);
    }
}
