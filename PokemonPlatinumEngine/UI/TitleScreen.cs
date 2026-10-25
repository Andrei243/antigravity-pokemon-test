using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// Where the opening is: the notice, the fly-over shots, Giratina's reveal, the idle title, then the menu and the
/// question a new game asks where a save exists (whether to leave it behind; whose rules is no longer asked, plan
/// 06 · R19: every game is played by the modern ones). Between the title and
/// the menu, <see cref="Warning"/> tells what the player must know before choosing (a save that couldn't be read,
/// plan 12 · Q1), one <see cref="TitleNotice"/> at a time. Deleting a save (plan 12 · Q2) is <see cref="ChooseDelete"/>,
/// the cursor on the saves' cards alone, and <see cref="ConfirmDelete"/>, the question that names the slot.
/// </summary>
public enum TitlePhase { Notice, Journey, Reveal, Idle, Menu, ConfirmNewGame, Leaving, Warning, ChooseDelete, ConfirmDelete, ChooseRun, ChooseAutosave }

/// <summary>
/// A slot the title has a card for (plan 12 · Q2): its number from 1 and the game saved in it, or no game for a
/// slot whose save and backup are both damaged, which can only be deleted.
/// </summary>
public sealed record TitleSlot(int Number, SaveData? Save, SaveData? Autosave = null, bool AutosaveOnly = false);

/// <summary>
/// Something the title tells the player before its menu opens, as Platinum tells of a damaged save (plan 12 · Q1):
/// a heading and a line or two of our own, put away with the A or the B button. A save's is
/// <see cref="ForSave"/>; plan 16 · T3's word after a crash is another of the same kind.
/// </summary>
public sealed record TitleNotice(string Heading, string Body)
{
    /// <summary>
    /// What the title says of a slot's save as it was read (plan 12 · Q2: a notice names its slot): nothing when it
    /// was whole or the slot is empty.
    /// </summary>
    public static TitleNotice? ForSlot(int slot, SaveLoad load) => load switch
    {
        SaveLoad.Recovered => new(Text.Of("title.notice.recovered.heading", ("slot", slot)), Text.Of("title.notice.recovered.body")),
        SaveLoad.Damaged => new(Text.Of("title.notice.damaged.heading", ("slot", slot)), Text.Of("title.notice.damaged.body")),
        _ => null
    };

    /// <summary>What the title says of a lone save as it was read: slot 1's notice.</summary>
    public static TitleNotice? ForSave(SaveLoad load) => ForSlot(1, load);

    /// <summary>NEW GAME with every slot taken: a new game needs a slot of its own.</summary>
    public static TitleNotice NoFreeSlot => new(Text.Of("title.notice.noFreeSlot.heading"), Text.Of("title.notice.noFreeSlot.body"));

    /// <summary>A slot the player asked to delete is still there: something holds its files.</summary>
    public static TitleNotice CouldNotDelete(int slot) =>
        new(Text.Of("title.notice.couldNotDelete.heading", ("slot", slot)), Text.Of("title.notice.couldNotDelete.body"));

    /// <summary>
    /// What the title says of a save from before there were slots that couldn't be made one (plan 12 · Q2): nothing
    /// when it was moved or there was none.
    /// </summary>
    public static TitleNotice? ForOldSave(OldSaveOutcome moved) => moved.Move switch
    {
        OldSaveMove.NoRoom => new(Text.Of("title.notice.oldSaveNoRoom.heading"), Text.Of("title.notice.oldSaveNoRoom.body", ("file", SavePaths.OldSaveName))),
        OldSaveMove.Failed => new(Text.Of("title.notice.oldSaveFailed.heading"), Text.Of("title.notice.oldSaveFailed.body", ("file", SavePaths.OldSaveName))),
        _ => null
    };

    /// <summary>
    /// What the title says, once, on the start after a crash (plan 16 · T3): that the game stopped, what became of
    /// the save (it is intact, or there was none yet; a save that couldn't be read has its own notice, which comes
    /// after this one), where the report is and whether the game as it stood was kept beside the save. Null when
    /// the last run left no note: it ended as the player chose.
    /// </summary>
    public static TitleNotice? ForCrash(Core.Diagnostics.CrashNote? crash, SaveLoad load) => ForCrash(crash, new[] { load });

    /// <summary>The same with several slots (plan 12 · Q2): "Your saves are intact." when more than one holds a save and all read whole.</summary>
    public static TitleNotice? ForCrash(Core.Diagnostics.CrashNote? crash, IReadOnlyCollection<SaveLoad> slots)
    {
        if (crash == null) return null;
        var taken = slots.Where(s => s != SaveLoad.None).ToList();
        string? save = taken.Count == 0 ? Text.Of("title.notice.crash.noSave")
            : taken.Any(s => s != SaveLoad.Ok) ? null
            : Text.Of("title.notice.crash.intact", ("n", taken.Count));
        string report = crash.SaveKept
            ? Text.Of("title.notice.crash.reportKept", ("report", crash.Report), ("file", Core.Diagnostics.CrashReport.SaveName))
            : Text.Of("title.notice.crash.report", ("report", crash.Report));
        return new(Text.Of("title.notice.crash.heading"), save == null ? report : save + " " + report);
    }
}

/// <summary>
/// An entry of the title menu, and what the player picked there. <see cref="Delete"/> (the DELETE A SAVE row) and
/// <see cref="Unreadable"/> (the card of a slot that can't be read) are entries only: neither is ever handed over
/// by <see cref="TitleScreen.TakeChoice"/>; a deletion is <see cref="TitleScreen.TakeDeletion"/>'s.
/// </summary>
public enum TitleChoice { None, Continue, NewGame, Options, Quit, Delete, Unreadable, Migrate, Jukebox }

/// <summary>
/// The opening and the title screen: a short notice, fly-over shots of Sinnoh at different times of day, Giratina
/// appearing out of the dark with the title lettering, "press start", and the menu to continue a saved game
/// (showing its badges, play time, Pokédex and party) or begin a new one. Any button skips ahead to the title.
/// <para>
/// A game is saved in one of three slots (plan 12 · Q2), and the menu has a card for each that holds a save
/// (<see cref="Cards"/>): one to continue from, or, for a slot that can't be read, one that can only be deleted.
/// NEW GAME takes the first empty slot (<see cref="FreeSlot"/>), and DELETE A SAVE asks which and then asks again.
/// The screen deletes nothing itself: it hands the slot over (<see cref="TakeDeletion"/>) and is told when the
/// slot is gone (<see cref="Forget"/>).
/// </para>
/// </summary>
public sealed class TitleScreen
{
    public const float NoticeTime = 3.4f, SegmentTime = 3.6f, RevealTime = 3.2f, LeaveTime = 0.8f;

    /// <summary>
    /// How long the title waits for a button before it plays its opening again (plan 09 · L4), as Platinum's title
    /// returns to its film: the fly-over shots and the reveal, never the notice.
    /// </summary>
    public const float IdleTime = 30f;

    internal static readonly (string Map, Vector2 From, Vector2 To, float Hour)[] Segments =
    {
        // Up Twinleaf Town's road at dawn, east along Route 201 at midday, through Sandgem Town in the evening
        ("Sinnoh", new Vector2(112f, 888f), new Vector2(112f, 876f), 7f),
        ("Sinnoh", new Vector2(120f, 853f), new Vector2(142f, 851f), 13.5f),
        ("Sinnoh", new Vector2(166f, 846f), new Vector2(184f, 846f), 20.5f)
    };

    private readonly List<TitleSlot> cards = new();
    private readonly List<TitleChoice> entries = new();
    private readonly Queue<TitleNotice> notices = new();
    private TitleChoice chosen = TitleChoice.None;
    private bool optionsRequested, migrateRequested, jukeboxRequested, noticeOverMenu, migrateOffered;
    private float phaseTime, totalTime, menuBlend, skipFlash;
    private int warmed;
    // The slot the delete question is about, where the question was asked from, and the slot to be deleted once
    // the answer was yes, until the game takes it
    private int deleting;
    private TitlePhase deleteFrom = TitlePhase.Menu;
    private int deleteRow;
    private int? deletion;

    public TitlePhase Phase { get; private set; } = TitlePhase.Notice;
    public int SelectedIndex { get; private set; }

    /// <summary>In the "start a new game?" and the "delete this save?" prompts: true while Yes is highlighted.</summary>
    public bool ConfirmYes { get; private set; }

    /// <summary>Whether there is a game to continue: a slot whose save could be read.</summary>
    public bool HasSave => cards.Any(c => c.Save != null);
    public IReadOnlyList<TitleChoice> Entries => entries;

    /// <summary>The slots that hold something, in their order: the menu's first entries are their cards.</summary>
    public IReadOnlyList<TitleSlot> Cards => cards;

    /// <summary>The slot a new game would be saved in: the lowest with nothing in it; null when all are taken.</summary>
    public int? FreeSlot
    {
        get
        {
            for (int slot = 1; slot <= SavePaths.Slots; slot++)
                if (cards.All(c => c.Number != slot)) return slot;
            return null;
        }
    }

    /// <summary>
    /// The slot of the choice handed over (<see cref="TakeChoice"/>): the one to continue from, or the one the new
    /// game is saved in.
    /// </summary>
    public int Slot { get; private set; } = 1;

    /// <summary>
    /// Whether the game to continue is the slot's autosave and not its save by hand (plan 12 · Q3): set when the
    /// player chose the autosave in <see cref="TitlePhase.ChooseAutosave"/>, or the slot holds nothing else.
    /// </summary>
    public bool UseAutosave { get; private set; }

    /// <summary>The slot the delete question is asking about (<see cref="TitlePhase.ConfirmDelete"/>).</summary>
    public int DeletingSlot => deleting;

    /// <summary>The notice being shown in <see cref="TitlePhase.Warning"/>, or the next to be shown; null when none is left.</summary>
    public TitleNotice? Notice => notices.Count > 0 ? notices.Peek() : null;

    /// <summary>The title with one save, in slot 1, or with none.</summary>
    /// <param name="notices">What to tell before the menu opens (null entries are passed over).</param>
    public TitleScreen(SaveData? save, params TitleNotice?[] notices)
        : this(save != null ? new[] { new TitleSlot(1, save) } : Array.Empty<TitleSlot>(), notices) { }

    /// <summary>The title with the slots that hold something (plan 12 · Q2), each with its game or with none for one that can't be read.</summary>
    public static TitleScreen WithSlots(IEnumerable<TitleSlot> taken, params TitleNotice?[] notices) => new(taken.ToArray(), notices);

    private TitleScreen(TitleSlot[] taken, TitleNotice?[] notices)
    {
        foreach (var notice in notices)
            if (notice != null) this.notices.Enqueue(notice);
        cards.AddRange(taken.Where(c => SavePaths.IsSlot(c.Number)).DistinctBy(c => c.Number).OrderBy(c => c.Number));
        Lay();
    }

    /// <summary>
    /// Whether the migrate folder holds something the readers know (plan 08 · P21): MIGRATE is on the menu only then, and
    /// only with a save to bring Pokémon to, as Platinum's shows only with a cartridge in the slot. The menu without it
    /// is the menu the title has always had.
    /// </summary>
    public void OfferMigration(bool offered)
    {
        if (migrateOffered == offered) return;
        migrateOffered = offered;
        var kept = SelectedIndex < entries.Count ? entries[SelectedIndex] : TitleChoice.None;
        Lay();
        int at = entries.IndexOf(kept);
        SelectedIndex = at >= 0 ? at : Math.Min(SelectedIndex, entries.Count - 1);
    }

    // The menu from top to bottom: a card for each slot that holds something, then the rows
    private void Lay()
    {
        entries.Clear();
        foreach (var card in cards) entries.Add(card.Save != null ? TitleChoice.Continue : TitleChoice.Unreadable);
        entries.Add(TitleChoice.NewGame);
        if (cards.Count > 0) entries.Add(TitleChoice.Delete);
        // Pal Park's migration (plan 08 · P21) needs a save to bring the Pokémon to, and something to bring them from
        if (migrateOffered && cards.Any(c => c.Save != null)) entries.Add(TitleChoice.Migrate);
        entries.Add(TitleChoice.Jukebox);
        entries.Add(TitleChoice.Options);
        entries.Add(TitleChoice.Quit);
    }

    /// <summary>Hours and minutes played, as on the continue panel: 0:07, 12:34, 123:05.</summary>
    public static string FormatPlayTime(float seconds)
    {
        int minutes = (int)(Math.Max(0f, seconds) / 60f);
        return $"{minutes / 60}:{minutes % 60:D2}";
    }

    /// <summary>How many badges a region's mask holds (plan 18 · W2: a region has none to eight, so only eight bits count).</summary>
    public static int CountBadges(int mask) => System.Numerics.BitOperations.PopCount((uint)mask & 0xFF);

    // ------------------------------------------------------------------ logic

    private void Enter(TitlePhase phase)
    {
        Phase = phase;
        phaseTime = 0f;
        readyForTheLoop = false;
    }

    // How long before the opening plays again its first shot's ground is asked for, and whether it has been
    private const float AheadOfTheLoop = 3f;
    private bool readyForTheLoop;

    /// <summary>Moves the opening forward in time.</summary>
    public void Advance(float dt)
    {
        phaseTime += dt;
        totalTime += dt;
        skipFlash = Math.Max(0f, skipFlash - dt);
        bool menuUp = Phase is TitlePhase.Menu or TitlePhase.ConfirmNewGame or TitlePhase.Leaving
            or TitlePhase.ChooseDelete or TitlePhase.ChooseRun or TitlePhase.ConfirmDelete or TitlePhase.ChooseAutosave || Phase == TitlePhase.Warning && noticeOverMenu;
        menuBlend = Math.Clamp(menuBlend + (menuUp ? dt : -dt) / 0.45f, 0f, 1f);

        switch (Phase)
        {
            case TitlePhase.Notice when phaseTime >= NoticeTime: Enter(TitlePhase.Journey); break;
            case TitlePhase.Journey when phaseTime >= SegmentTime * Segments.Length: Enter(TitlePhase.Reveal); break;
            case TitlePhase.Reveal when phaseTime >= RevealTime: Enter(TitlePhase.Idle); break;
            // Left alone, the title plays its opening again: the attract loop (the menu and its prompts wait for good)
            case TitlePhase.Idle when phaseTime >= IdleTime: Enter(TitlePhase.Journey); break;
        }
    }

    /// <summary>The A button: skips the opening, opens the menu, picks the highlighted entry.</summary>
    public void PressConfirm()
    {
        switch (Phase)
        {
            case TitlePhase.Notice:
            case TitlePhase.Journey:
            case TitlePhase.Reveal:
                Enter(TitlePhase.Idle);
                skipFlash = 0.4f;
                break;
            case TitlePhase.Idle:
                // What the player must know comes before the menu, as Platinum tells of a damaged save
                Enter(notices.Count > 0 ? TitlePhase.Warning : TitlePhase.Menu);
                SelectedIndex = 0;
                AudioManager.PlaySound("select");
                // As in the original, Giratina answers the button
                if (PokemonDatabase.Get("Giratina") is { } giratina) AudioManager.PlayCry(giratina);
                break;
            case TitlePhase.Warning:
                PutNoticeAway();
                break;
            case TitlePhase.Menu:
                AudioManager.PlaySound("select");
                switch (entries[SelectedIndex])
                {
                    case TitleChoice.Continue:
                        Slot = cards[SelectedIndex].Number;
                        UseAutosave = false;
                        // A game that stopped on a crash after an autosave: the player says which to take up (plan 12 · Q3)
                        if (cards[SelectedIndex].Autosave != null)
                        {
                            ConfirmYes = true;
                            Enter(TitlePhase.ChooseAutosave);
                            break;
                        }
                        // A slot whose only game is its autosave, never saved by hand, can only be taken up
                        UseAutosave = cards[SelectedIndex].AutosaveOnly;
                        Leave(TitleChoice.Continue);
                        break;
                    // Nothing can be done with a slot that can't be read but delete it
                    case TitleChoice.Unreadable: AskDelete(cards[SelectedIndex].Number); break;
                    // A new game takes the first empty slot, and with none it can't begin
                    case TitleChoice.NewGame when FreeSlot == null: Tell(TitleNotice.NoFreeSlot); break;
                    case TitleChoice.NewGame when cards.Count > 0:
                        ConfirmYes = false;
                        Enter(TitlePhase.ConfirmNewGame);
                        break;
                    case TitleChoice.NewGame: OpenRunPanel(); break;
                    // With one save there is no asking which
                    case TitleChoice.Delete when cards.Count == 1: AskDelete(cards[0].Number); break;
                    case TitleChoice.Delete:
                        deleteRow = SelectedIndex;
                        SelectedIndex = 0;
                        Enter(TitlePhase.ChooseDelete);
                        break;
                    case TitleChoice.Options: optionsRequested = true; break;
                    case TitleChoice.Migrate: migrateRequested = true; break;
                    case TitleChoice.Jukebox: jukeboxRequested = true; break;
                    case TitleChoice.Quit: Leave(TitleChoice.Quit); break;
                }
                break;
            case TitlePhase.ConfirmNewGame:
                AudioManager.PlaySound(ConfirmYes ? "select" : "cancel");
                if (ConfirmYes) OpenRunPanel();
                else Enter(TitlePhase.Menu);
                break;
            case TitlePhase.ChooseRun:
                AudioManager.PlaySound("select");
                Run.Confirm();
                if (Run.Done) BeginNewGame();
                break;
            case TitlePhase.ChooseAutosave:
                AudioManager.PlaySound("select");
                UseAutosave = ConfirmYes;
                Leave(TitleChoice.Continue);
                break;
            case TitlePhase.ChooseDelete:
                AudioManager.PlaySound("select");
                AskDelete(cards[SelectedIndex].Number);
                break;
            case TitlePhase.ConfirmDelete:
                AudioManager.PlaySound(ConfirmYes ? "select" : "cancel");
                if (ConfirmYes)
                {
                    // The game deletes the slot and says when it is gone (Forget); the menu waits under its cursor
                    deletion = deleting;
                    if (deleteFrom == TitlePhase.ChooseDelete) SelectedIndex = deleteRow;
                    Enter(TitlePhase.Menu);
                }
                else Enter(deleteFrom);
                break;
        }
    }

    /// <summary>The question that comes before a slot is deleted: it names the slot, and No is highlighted.</summary>
    private void AskDelete(int slot)
    {
        deleting = slot;
        deleteFrom = Phase == TitlePhase.ChooseDelete ? TitlePhase.ChooseDelete : TitlePhase.Menu;
        ConfirmYes = false;
        Enter(TitlePhase.ConfirmDelete);
    }

    /// <summary>
    /// The slot the player has asked to delete and said yes to, once; null otherwise. Whoever takes it deletes the
    /// slot's files and calls <see cref="Forget"/>, or <see cref="Tell"/> if they couldn't be deleted.
    /// </summary>
    public int? TakeDeletion()
    {
        int? slot = deletion;
        deletion = null;
        return slot;
    }

    /// <summary>A slot is gone: its card goes, the column closes up, and the cursor is on the first entry.</summary>
    public void Forget(int slot)
    {
        if (cards.RemoveAll(c => c.Number == slot) == 0) return;
        Lay();
        SelectedIndex = 0;
        if (Phase is TitlePhase.ChooseDelete or TitlePhase.ConfirmDelete) Enter(TitlePhase.Menu);
    }

    /// <summary>Something to tell now, over the menu: it is read and put away like any notice, and the menu is back.</summary>
    public void Tell(TitleNotice notice)
    {
        notices.Enqueue(notice);
        noticeOverMenu = Phase != TitlePhase.Idle;
        Enter(TitlePhase.Warning);
    }

    /// <summary>
    /// A new game is handed over, in the first empty slot. Every game is played by the modern rules (plan 06 · R19, the
    /// user's ruling of 2026-10-12), so nothing more is asked: the question of whose rules is gone.
    /// </summary>
    private void BeginNewGame()
    {
        Slot = FreeSlot ?? 1;
        ChosenRun = Run.Rules;
        Leave(TitleChoice.NewGame);
    }

    /// <summary>
    /// NEW GAME was chosen (and, with saves in the way, agreed to): the ways to play are asked next (plan 13 · V1), in a
    /// panel of rows with the preset first. Confirm on Normal leaves at once; B goes back to the menu.
    /// </summary>
    private void OpenRunPanel()
    {
        Run.Open();
        Enter(TitlePhase.ChooseRun);
    }

    /// <summary>The panel of the ways to play (plan 13 · V1), open in <see cref="TitlePhase.ChooseRun"/>.</summary>
    public RunPanel Run { get; } = new();

    /// <summary>The ways to play the new game handed over was started with: Normal unless the panel chose otherwise.</summary>
    public RunRules ChosenRun { get; private set; } = RunRules.Normal;

    /// <summary>Left and right in the panel of the ways to play: the selected row's next value.</summary>
    public void Change(int direction)
    {
        if (Phase != TitlePhase.ChooseRun) return;
        Run.Change(direction);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>A notice read: the next one, or the menu once none is left.</summary>
    private void PutNoticeAway()
    {
        AudioManager.PlaySound("select");
        if (notices.Count > 0) notices.Dequeue();
        if (notices.Count == 0) noticeOverMenu = false;
        Enter(notices.Count > 0 ? TitlePhase.Warning : TitlePhase.Menu);
    }

    /// <summary>The B button: backs out of a prompt, then out of the menu; a notice is put away as with A.</summary>
    public void PressCancel()
    {
        if (Phase == TitlePhase.Warning)
        {
            PutNoticeAway();
            return;
        }
        if (Phase is TitlePhase.ConfirmNewGame or TitlePhase.ChooseAutosave) Enter(TitlePhase.Menu);
        // Out of the ways to play to the menu NEW GAME was chosen on (plan 13 · V1)
        else if (Phase == TitlePhase.ChooseRun) Enter(TitlePhase.Menu);
        // Out of the delete question to where it was asked; out of the choice of a save to the row that asked for it
        else if (Phase == TitlePhase.ConfirmDelete) Enter(deleteFrom);
        else if (Phase == TitlePhase.ChooseDelete)
        {
            SelectedIndex = deleteRow;
            Enter(TitlePhase.Menu);
        }
        else if (Phase == TitlePhase.Menu) Enter(TitlePhase.Idle);
        else return;
        AudioManager.PlaySound("cancel");
    }

    /// <summary>Up/down in the menu (among the saves alone while one is chosen to delete); any direction flips the answer in a prompt.</summary>
    public void Move(int step)
    {
        if (step == 0) return;
        if (Phase == TitlePhase.Menu) SelectedIndex = ((SelectedIndex + step) % entries.Count + entries.Count) % entries.Count;
        else if (Phase == TitlePhase.ChooseDelete) SelectedIndex = ((SelectedIndex + step) % cards.Count + cards.Count) % cards.Count;
        else if (Phase == TitlePhase.ChooseRun) Run.Move(step);
        else if (Phase is TitlePhase.ConfirmNewGame or TitlePhase.ConfirmDelete or TitlePhase.ChooseAutosave) ConfirmYes = !ConfirmYes;
        else return;
        AudioManager.PlaySound("cursor");
    }

    private void Leave(TitleChoice choice)
    {
        chosen = choice;
        Enter(TitlePhase.Leaving);
    }

    /// <summary>
    /// The player's choice, once: Options straight away; Continue, New Game or Quit when the screen has faded out.
    /// </summary>
    public TitleChoice TakeChoice()
    {
        if (optionsRequested)
        {
            optionsRequested = false;
            return TitleChoice.Options;
        }
        if (jukeboxRequested)
        {
            jukeboxRequested = false;
            return TitleChoice.Jukebox;
        }
        if (migrateRequested)
        {
            migrateRequested = false;
            return TitleChoice.Migrate;
        }
        if (Phase == TitlePhase.Leaving && phaseTime >= LeaveTime && chosen != TitleChoice.None)
        {
            var result = chosen;
            chosen = TitleChoice.None;
            return result;
        }
        return TitleChoice.None;
    }

    public void Update(float dt)
    {
        Advance(dt);
        if (InputManager.IsActionPressed(GameAction.Confirm) || InputManager.IsActionPressed(GameAction.Menu)) PressConfirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) PressCancel();
        else if (Phase == TitlePhase.ChooseRun && InputManager.IsActionPressed(GameAction.Left)) Change(-1);
        else if (Phase == TitlePhase.ChooseRun && InputManager.IsActionPressed(GameAction.Right)) Change(1);
        else if (InputManager.IsActionPressed(GameAction.Up) || InputManager.IsActionPressed(GameAction.Left)) Move(-1);
        else if (InputManager.IsActionPressed(GameAction.Down) || InputManager.IsActionPressed(GameAction.Right)) Move(1);
    }

    // ------------------------------------------------------------------ drawing

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0f, 1f);
        return t * t * (3f - 2f * t);
    }

    private float Reveal => Phase == TitlePhase.Reveal ? Smooth(1.2f, 2.4f, phaseTime) : 1f;

    /// <summary>Renders this frame's 3D picture offscreen. Call outside any other texture mode.</summary>
    internal void Render(RenderContext context, WorldRenderer world, TitleScene scene)
    {
        switch (Phase)
        {
            case TitlePhase.Notice:
                // Behind the notice, build what the opening needs (one map a frame, then Giratina) so no shot hitches
                if (warmed < Segments.Length)
                {
                    var first = Segments[warmed];
                    world.RenderCinematic(MapDatabase.Get(first.Map), first.From.X, first.From.Y, first.Hour);
                }
                else if (warmed == Segments.Length) scene.Render(context, 0f, 0f, 0f);
                warmed = Math.Min(warmed + 1, Segments.Length + 1);
                break;
            case TitlePhase.Journey:
                int index = Math.Min((int)(phaseTime / SegmentTime), Segments.Length - 1);
                var (map, from, to, hour) = Segments[index];
                float t = (phaseTime - index * SegmentTime) / SegmentTime;
                var focus = Vector2.Lerp(from, to, t);
                world.RenderCinematic(MapDatabase.Get(map), focus.X, focus.Y, hour);
                break;
            default:
                // About to play the opening again: the first shot's ground starts baking behind the title
                if (Phase == TitlePhase.Idle && phaseTime >= IdleTime - AheadOfTheLoop && !readyForTheLoop)
                {
                    readyForTheLoop = true;
                    world.MakeReady(MapDatabase.Get(Segments[0].Map), Segments[0].From.X, Segments[0].From.Y);
                }
                float ease = menuBlend * menuBlend * (3f - 2f * menuBlend);
                scene.Render(context, totalTime, Reveal, ease * 2.7f);
                break;
        }
    }

    /// <summary>Draws the opening or the title into the current target (the virtual screen).</summary>
    internal void Draw(int sw, int sh, RenderContext context, WorldRenderer world)
    {
        switch (Phase)
        {
            case TitlePhase.Notice:
                DrawNotice(sw, sh);
                return;
            case TitlePhase.Journey:
                DrawJourney(sw, sh, world);
                return;
        }

        context.Composite(new Rectangle(0, 0, sw, sh));
        DrawMotes(sw, sh);

        float ease = menuBlend * menuBlend * (3f - 2f * menuBlend);
        float logoAlpha = Phase == TitlePhase.Reveal ? Smooth(1.7f, 2.7f, phaseTime) : 1f;
        DrawLogo(sw, ease, logoAlpha);

        if (Phase == TitlePhase.Idle)
        {
            float pulse = 0.55f + 0.45f * MathF.Sin(totalTime * 3.2f);
            string prompt = Text.Of("title.press");
            float w = UiFonts.Measure(prompt, 40, UiWeight.Black);
            UiFonts.Draw(prompt, (sw - w) / 2f + 2, 954, 40, new Color(0, 0, 0, (int)(120 * pulse)), UiWeight.Black);
            UiFonts.Draw(prompt, (sw - w) / 2f, 952, 40, new Color(255, 255, 255, (int)(255 * pulse)), UiWeight.Black);
        }
        if (menuBlend > 0.01f) DrawMenu(sw, ease);
        if (Phase == TitlePhase.ConfirmNewGame) DrawConfirm(sw, sh);
        if (Phase == TitlePhase.ConfirmDelete) DrawDelete(sw, sh);
        if (Phase == TitlePhase.ChooseAutosave) DrawAutosave(sw, sh);
        if (Phase == TitlePhase.ChooseRun) ModernUi.RunChoice(sw, sh, Run, Smooth(0f, 0.2f, phaseTime));
        if (Phase == TitlePhase.Warning && Notice is { } notice)
            ModernUi.Notice(sw, sh, notice.Heading, notice.Body, Text.Of("shared.ok"), Smooth(0f, 0.18f, phaseTime));

        string credit = Text.Of("title.credit");
        float cw = UiFonts.Measure(credit, 20, UiWeight.Bold);
        UiFonts.Draw(credit, (sw - cw) / 2f, sh - 44, 20, new Color(220, 214, 240, (int)(150 * logoAlpha)), UiWeight.Bold);

        // Fades: up from black as Giratina appears, a flash as it lights up or the opening is skipped, out to black on leaving
        if (Phase == TitlePhase.Reveal)
        {
            float black = 1f - Smooth(0f, 1.1f, phaseTime);
            if (black > 0f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(255 * black)));
            float flash = Math.Max(0f, 1f - MathF.Abs(phaseTime - 1.35f) / 0.3f);
            if (flash > 0f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(255, 250, 255, (int)(190 * flash)));
        }
        if (skipFlash > 0f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(255, 250, 255, (int)(200 * skipFlash / 0.4f)));
        if (Phase == TitlePhase.Leaving)
            Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(255 * Math.Clamp(phaseTime / LeaveTime, 0f, 1f))));
    }

    private void DrawNotice(int sw, int sh)
    {
        Raylib.DrawRectangle(0, 0, sw, sh, Color.Black);
        float alpha = Smooth(0.2f, 0.9f, phaseTime) * (1f - Smooth(NoticeTime - 0.8f, NoticeTime - 0.1f, phaseTime));
        var white = new Color(255, 255, 255, (int)(255 * alpha));
        var dim = new Color(190, 196, 214, (int)(255 * alpha));
        void Centered(string text, float y, float size, Color color, UiWeight weight)
        {
            float w = UiFonts.Measure(text, size, weight);
            UiFonts.Draw(text, (sw - w) / 2f, y, size, color, weight);
        }
        Centered(Text.Of("title.disclaimer.heading"), sh / 2f - 96, 54, white, UiWeight.Black);
        Centered(Text.Of("title.disclaimer.owners"), sh / 2f, 26, dim, UiWeight.Bold);
        Centered(Text.Of("title.disclaimer.original"), sh / 2f + 42, 26, dim, UiWeight.Bold);
    }

    private void DrawJourney(int sw, int sh, WorldRenderer world)
    {
        world.DrawToScreen(sw, sh);

        // Cinema bars, and a dip to black between shots
        const int bar = 120;
        Raylib.DrawRectangle(0, 0, sw, bar, Color.Black);
        Raylib.DrawRectangle(0, sh - bar, sw, bar, Color.Black);
        float local = phaseTime % SegmentTime;
        float black = Math.Max(1f - Smooth(0f, 0.6f, local), Smooth(SegmentTime - 0.6f, SegmentTime, local));
        if (black > 0f) Raylib.DrawRectangle(0, 0, sw, sh, new Color(0, 0, 0, (int)(255 * black)));
    }

    /// <summary>Motes of light rising slowly through the void.</summary>
    private void DrawMotes(int sw, int sh)
    {
        var glow = SceneTextures.SoftGlow;
        var src = new Rectangle(0, 0, glow.Width, glow.Height);
        float reveal = Reveal;
        Raylib.BeginBlendMode(BlendMode.Additive);
        for (int i = 0; i < 70; i++)
        {
            float seed = i * 12.9898f;
            float fx = MathF.Abs(MathF.Sin(seed) * 43758.5453f) % 1f;
            float fy = MathF.Abs(MathF.Sin(seed * 1.7f) * 24634.6345f) % 1f;
            float speed = 0.012f + (i % 7) * 0.004f;
            float y = 1f - (fy + totalTime * speed) % 1f;
            float x = fx + 0.015f * MathF.Sin(totalTime * 0.6f + i);
            float size = 10f + (i % 5) * 7f;
            float twinkle = 0.5f + 0.5f * MathF.Sin(totalTime * (1.2f + i % 4 * 0.5f) + i);
            var color = i % 3 == 0 ? new Color(255, 214, 150, 255) : new Color(190, 150, 255, 255);
            color.A = (byte)(150 * twinkle * reveal * MathF.Sin(y * MathF.PI));
            Raylib.DrawTexturePro(glow, src, new Rectangle(x * sw - size / 2, y * sh - size / 2, size, size), Vector2.Zero, 0f, color);
        }
        Raylib.EndBlendMode();
    }

    /// <summary>
    /// The title lettering (our own, not the games' logo): large and centred on the idle title, small in the top
    /// left once the menu is up.
    /// </summary>
    private void DrawLogo(int sw, float menu, float alpha)
    {
        if (alpha <= 0.01f) return;
        float scale = 1f - 0.42f * menu;
        const string word = "PLATINUM", series = "Pokémon";
        float size = 196f * scale, spacing = 16f * scale;
        float width = UiFonts.MeasureDisplay(word, size, spacing);
        float x = (sw - width) / 2f * (1f - menu) + 96f * menu;
        float y = (150f - 30f * (1f - alpha)) * (1f - menu) + 86f * menu;

        float seriesSize = 78f * scale;
        var cream = new Color(255, 244, 224, (int)(255 * alpha));
        UiFonts.DrawDisplay(series, x + 6 * scale + 3, y - seriesSize * 0.78f + 4, seriesSize, 2f, new Color(10, 6, 24, (int)(170 * alpha)));
        UiFonts.DrawDisplay(series, x + 6 * scale, y - seriesSize * 0.78f, seriesSize, 2f, cream);

        // Platinum lettering: a dark drop, then a silver gradient with a band of light sweeping across now and then
        UiFonts.DrawDisplay(word, x + 5 * scale, y + 8 * scale, size, spacing, new Color(10, 6, 24, (int)(200 * alpha)));
        float sweep = totalTime % 5.5f;
        float shineX = sweep < 1.6f ? x - 200f + (width + 500f) * (sweep / 1.6f) : float.NaN;
        UiFonts.DrawDisplayGradient(word, x, y, size, spacing, new Color(255, 255, 255, 255), new Color(150, 162, 196, 255), alpha, shineX);

        // A thin rule with a diamond, and the subtitle
        float ruleY = y + size * 0.98f;
        var silver = new Color(214, 222, 240, (int)(230 * alpha));
        UiShapes.Fill(new Rectangle(x + 4, ruleY, width - 8, 4 * scale), 2 * scale, silver);
        const string sub = "A  FAN  REMAKE";
        UiFonts.Draw(sub, x + 6, ruleY + 14 * scale, 30 * scale, new Color(226, 220, 244, (int)(230 * alpha)), UiWeight.Black);
    }

    // ------------------------------------------------------------------ menu

    /// <summary>The menu's column: where it stands, and how tall a lone save's card and one of several are (style guide, "The slots").</summary>
    public const float ColumnX = 1016, ColumnWidth = 824, LoneCardHeight = 452, ShortCardHeight = 144;

    /// <summary>A lone save that can be read keeps the whole card; two or three, or a slot that can't be read, are short cards.</summary>
    public bool LoneCard => cards.Count == 1 && cards[0].Save != null;

    /// <summary>
    /// Where each entry of the menu stands once the menu is up, in the entries' order: with nothing saved the three
    /// rows as they always were, with a save the cards and four slimmer rows, centred on the same line.
    /// </summary>
    public IReadOnlyList<Rectangle> EntryRects()
    {
        bool saves = cards.Count > 0;
        // MIGRATE is a fifth row beside the cards, which the column holds by slimming its rows
        int rowsN = entries.Count - cards.Count;
        float row = rowsN >= 6 ? 56 : rowsN == 5 ? 64 : saves ? 76 : 92, gap = rowsN >= 6 ? 10 : rowsN == 5 ? 12 : saves ? 18 : 20;
        float card = LoneCard ? LoneCardHeight : ShortCardHeight;
        float total = cards.Count * card + (entries.Count - cards.Count) * row + (entries.Count - 1) * gap;
        float y = saves ? 590 - total / 2f : (1080 - total) / 2f + 40;
        var rects = new List<Rectangle>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            float h = i < cards.Count ? card : row;
            rects.Add(new Rectangle(ColumnX, y, ColumnWidth, h));
            y += h + gap;
        }
        return rects;
    }

    private void DrawMenu(int sw, float ease)
    {
        float slide = (1f - ease) * 520f;
        var rects = EntryRects();
        bool removing = Phase is TitlePhase.ChooseDelete or TitlePhase.ConfirmDelete;
        for (int i = 0; i < entries.Count; i++)
        {
            var r = rects[i] with { X = rects[i].X + slide };
            var card = i < cards.Count ? cards[i] : null;
            // While a save is being deleted the glow is on its card, and it is Red
            bool selected = Phase == TitlePhase.ConfirmDelete ? card?.Number == DeletingSlot : i == SelectedIndex && Phase != TitlePhase.Idle;
            ModernUi.Card(r, 34, selected, removing && card != null ? ModernUi.Red : null);

            if (card != null)
            {
                if (LoneCard) ModernUi.SaveSummary(r, card.Save!, Text.Of("title.menu.continue"), card.Number);
                else ModernUi.SaveCard(r, card.Number, card.Save);
                continue;
            }
            string label = entries[i] switch
            {
                TitleChoice.NewGame => Text.Of("title.menu.newGame"),
                TitleChoice.Delete => Text.Of(Phase == TitlePhase.ChooseDelete ? "title.menu.deleteWhich" : "title.menu.delete"),
                TitleChoice.Migrate => Text.Of("title.menu.migrate"),
                TitleChoice.Jukebox => Text.Of("title.menu.jukebox"),
                TitleChoice.Options => Text.Of("title.menu.options"),
                _ => Text.Of("title.menu.quit")
            };
            UiFonts.DrawCentered(label, r.X + 48, r.Y + r.Height / 2f, cards.Count > 0 ? 36 : 40, ModernUi.Ink, UiWeight.Black);
        }
    }

    /// <summary>What "start a new game?" says under its heading: the slot the new game takes, and that nothing is lost.</summary>
    public static string NewGameBlurb(int slot, int saves) =>
        Text.Of("title.newGame.blurb", ("slot", slot), ("n", saves));

    // (Yes is Green since the slots: a new game takes an empty slot, so nothing is lost by it. Red is the delete question's)
    private void DrawConfirm(int sw, int sh) =>
        ModernUi.Prompt(sw, sh, Text.Of("title.newGame.heading"), NewGameBlurb(FreeSlot ?? 1, cards.Count),
            new[] { (Text.Of("title.newGame.no"), ModernUi.Blue), (Text.Of("title.newGame.yes"), ModernUi.Green) }, ConfirmYes ? 1 : 0);

    /// <summary>The autosave question's two answers (plan 12 · Q3): the game saved by hand, then the autosave.</summary>
    public static string[] AutosaveAnswers => new[] { Text.Of("title.autosave.saved"), Text.Of("title.autosave.auto") };

    /// <summary>What the autosave question says under its heading: how far each game got, and where the autosave stands.</summary>
    public static string AutosaveBlurb(SaveData saved, SaveData autosave, string? place) => Text.Of("title.autosave.blurb",
        ("saved", FormatPlayTime(saved.PlayTimeSeconds)), ("auto", FormatPlayTime(autosave.PlayTimeSeconds)), ("place", place ?? ""));

    private void DrawAutosave(int sw, int sh)
    {
        var card = cards[Math.Clamp(SelectedIndex, 0, cards.Count - 1)];
        if (card.Save == null || card.Autosave == null) return;
        ModernUi.Prompt(sw, sh, Text.Of("title.autosave.heading", ("slot", card.Number)),
            AutosaveBlurb(card.Save, card.Autosave, ModernUi.PlaceOf(card.Autosave)),
            new[] { (AutosaveAnswers[0], ModernUi.Blue), (AutosaveAnswers[1], ModernUi.Green) }, ConfirmYes ? 1 : 0);
    }

    /// <summary>The delete question's two answers, in the language of the moment.</summary>
    public static string[] DeleteAnswers => new[] { Text.Of("title.delete.no"), Text.Of("title.delete.yes") };

    /// <summary>The delete question's heading: it names the slot.</summary>
    public static string DeleteHeading(int slot) => Text.Of("title.delete.heading", ("slot", slot));

    /// <summary>What the delete question says under its heading: whose game it is and where it stands, and that it can't be undone.</summary>
    /// <param name="place">Where the game was saved, as the card names it.</param>
    public static string DeleteBlurb(SaveData? save, string? place) => save == null
        ? Text.Of("title.delete.unreadable")
        : string.IsNullOrEmpty(place)
            ? Text.Of("title.delete.blurb", ("name", save.PlayerName), ("time", FormatPlayTime(save.PlayTimeSeconds)))
            : Text.Of("title.delete.blurbIn", ("name", save.PlayerName), ("place", place), ("time", FormatPlayTime(save.PlayTimeSeconds)));

    private void DrawDelete(int sw, int sh)
    {
        var save = cards.FirstOrDefault(c => c.Number == DeletingSlot)?.Save;
        ModernUi.Prompt(sw, sh, DeleteHeading(DeletingSlot), DeleteBlurb(save, save != null ? ModernUi.PlaceOf(save) : null),
            new[] { (DeleteAnswers[0], ModernUi.Blue), (DeleteAnswers[1], ModernUi.Red) }, ConfirmYes ? 1 : 0);
    }
}
