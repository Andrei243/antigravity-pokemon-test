using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.UI.Kit;

/// <summary>Where the phrase picker's cursor is: on the phrase's two words and OK, among the groups, or among a group's words.</summary>
public enum PhraseStage { Slots, Groups, Words }

/// <summary>What a press on the phrase picker did.</summary>
public enum PhrasePress { Nothing, Opened, Chose, Done, Refused }

/// <summary>
/// Choosing a phrase of two words (plan 08 · P9; style guide, "Field menus and notices", "Phrases"), as Platinum's
/// easy chat does it with lists of our own (<see cref="PhraseGroups"/>): the cursor stands on one of the phrase's two
/// words or on OK; a press on a word opens the groups, a press on a group its words, and a press on a word puts it in
/// the phrase and comes back to the next word, or to OK once both are filled. B goes back a step, and from the phrase
/// itself gives the phrase up. No drawing or input, like <see cref="NameEntry"/>: tests and the harness move the
/// cursor and press.
/// </summary>
public sealed class PhraseEntry
{
    /// <summary>The groups stand two to a row; a group's words three to a row, <see cref="WordRows"/> rows of them in view.</summary>
    public const int GroupColumns = 2, WordColumns = 3, WordRows = 5;

    /// <summary>The place of OK along the phrase's row, after its two words.</summary>
    public const int OkSlot = PhraseGroups.Length;

    private readonly string?[] words = new string?[PhraseGroups.Length];

    /// <summary>A picker over <paramref name="groups"/>, its two words those of <paramref name="initial"/> where they are words (the answer given last time).</summary>
    public PhraseEntry(PhraseGroups groups, IReadOnlyList<string>? initial = null)
    {
        Groups = groups;
        if (initial != null)
            for (int i = 0; i < Math.Min(initial.Count, words.Length); i++)
                if (PhraseGroups.IsWord(initial[i])) words[i] = initial[i];
        Slot = Complete ? OkSlot : Array.IndexOf(words, null);
    }

    public PhraseGroups Groups { get; }

    public PhraseStage Stage { get; private set; } = PhraseStage.Slots;

    /// <summary>The phrase so far, a key for each word chosen (<see cref="PhraseGroups.Key"/>) and null for one not yet.</summary>
    public IReadOnlyList<string?> Words => words;

    /// <summary>The word of the phrase being chosen, or <see cref="OkSlot"/> for OK.</summary>
    public int Slot { get; private set; }

    /// <summary>The group under the cursor, or open.</summary>
    public PhraseGroup Group { get; private set; }

    /// <summary>The word under the cursor in the open group, and the first row of its words in view.</summary>
    public int Word { get; private set; }
    public int FirstRow { get; private set; }

    /// <summary>True once OK has been pressed on a whole phrase; <see cref="Cancelled"/> once it was given up.</summary>
    public bool Done { get; private set; }
    public bool Cancelled { get; private set; }

    /// <summary>Whether both words are chosen.</summary>
    public bool Complete => words.All(w => w != null);

    /// <summary>The phrase, once it is done; null before, or when it was given up.</summary>
    public IReadOnlyList<string>? Result => Done ? words.Select(w => w!).ToList() : null;

    /// <summary>The open group's words.</summary>
    public IReadOnlyList<string> GroupWords => Groups.WordsOf(Group);

    /// <summary>How many rows the open group's words take.</summary>
    public int WordRowCount => (GroupWords.Count + WordColumns - 1) / WordColumns;

    /// <summary>One step of the cursor. On the phrase it goes along its row; among the groups and the words, rows and columns wrap round.</summary>
    public void Move(int dx, int dy)
    {
        if (Done || Cancelled) return;
        switch (Stage)
        {
            case PhraseStage.Slots:
                // The two words and OK lie in a row: up and down go along it too, as the cursor has nowhere else
                int step = dx != 0 ? Math.Sign(dx) : Math.Sign(dy);
                if (step != 0) Slot = UiNav.Wrap(Slot, step, OkSlot + 1);
                break;
            case PhraseStage.Groups:
                Group = (PhraseGroup)UiNav.Grid((int)Group, PhraseGroups.All.Count, GroupColumns, dx, dy);
                break;
            case PhraseStage.Words:
                if (GroupWords.Count == 0) return;
                Word = UiNav.Grid(Word, GroupWords.Count, WordColumns, dx, dy);
                FirstRow = UiNav.Window(FirstRow, Word / WordColumns, WordRowCount, WordRows, lead: 1);
                break;
        }
    }

    /// <summary>The A button.</summary>
    public PhrasePress Press()
    {
        if (Done || Cancelled) return PhrasePress.Nothing;
        switch (Stage)
        {
            case PhraseStage.Slots:
                if (Slot == OkSlot)
                {
                    if (!Complete) return PhrasePress.Refused;
                    Done = true;
                    return PhrasePress.Done;
                }
                // The groups open on the group of the word already there, if any
                if (PhraseGroups.TryParse(words[Slot], out var current, out _)) Group = current;
                Stage = PhraseStage.Groups;
                return PhrasePress.Opened;
            case PhraseStage.Groups:
                if (Groups.WordsOf(Group).Count == 0) return PhrasePress.Refused;
                Stage = PhraseStage.Words;
                // On the word already there when it is of this group, else the first
                int at = words[Slot] is { } now ? IndexOf(GroupWords, now) : -1;
                Word = Math.Max(0, at);
                FirstRow = UiNav.Window(0, Word / WordColumns, WordRowCount, WordRows, lead: 1);
                return PhrasePress.Opened;
            default:
                if (GroupWords.Count == 0) return PhrasePress.Refused;
                words[Slot] = GroupWords[Word];
                Stage = PhraseStage.Slots;
                // On to the next word not chosen, or to OK
                int next = Array.IndexOf(words, null);
                Slot = next >= 0 ? next : OkSlot;
                return PhrasePress.Chose;
        }
    }

    /// <summary>The B button: back a step; from the phrase itself it is given up. Returns true when it was given up.</summary>
    public bool Back()
    {
        if (Done || Cancelled) return Cancelled;
        switch (Stage)
        {
            case PhraseStage.Words:
                Stage = PhraseStage.Groups;
                return false;
            case PhraseStage.Groups:
                Stage = PhraseStage.Slots;
                return false;
            default:
                Cancelled = true;
                return true;
        }
    }

    /// <summary>The Start button: back to the phrase with the cursor on OK.</summary>
    public void ToDone()
    {
        if (Done || Cancelled) return;
        Stage = PhraseStage.Slots;
        Slot = OkSlot;
    }

    /// <summary>Chooses a word by its key at once, as a press on it would (a tool's or a test's hand); false if no group of the picker has it.</summary>
    public bool Choose(int slot, string key)
    {
        if (slot < 0 || slot >= words.Length || !PhraseGroups.TryParse(key, out var group, out _)) return false;
        int at = IndexOf(Groups.WordsOf(group), key);
        if (at < 0) return false;
        Stage = PhraseStage.Slots;
        Slot = slot;
        Press();
        Group = group;
        Press();
        Word = at;
        FirstRow = UiNav.Window(FirstRow, Word / WordColumns, WordRowCount, WordRows, lead: 1);
        return Press() == PhrasePress.Chose;
    }

    private static int IndexOf(IReadOnlyList<string> list, string key)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == key) return i;
        return -1;
    }
}
