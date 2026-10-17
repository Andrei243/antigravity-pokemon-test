using System;
using System.Collections.Generic;
using System.Linq;
using Raylib_cs;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>The two kinds of input an action has, one column of the Controls page each.</summary>
public enum ControlsColumn { Keyboard, Pad }

/// <summary>What the Controls page is doing.</summary>
public enum ControlsPhase
{
    /// <summary>The list: the cursor on an action and a column.</summary>
    Browsing,
    /// <summary>Waiting for the next key (or pad button) to become the action's.</summary>
    Capturing,
    /// <summary>The key pressed is another action's: swap or keep.</summary>
    Choosing,
    /// <summary>The question before every binding is put back.</summary>
    ResettingAll
}

/// <summary>
/// The Controls page under OPTIONS (plan 12 · Q5; style guide, "Menu screens", Controls): every action with its keys and
/// its pad buttons, and the way to change them. A is "change": the next key (or button, in the pad column) pressed becomes
/// the action's first, Esc cancels. A key that is another action's asks whether to swap the two or keep things as they
/// were (<see cref="InputBindings.Conflicts"/> is what shows a clash already in the file). The item button puts one action
/// back as the Button Mode has it, and the last row puts them all back. Everything is saved with the settings as the
/// overrides over the Button Mode's preset. The state moves through methods that take no input (<see cref="Capture"/>,
/// <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness drive it; <see cref="Update"/> maps the keys on.
/// </summary>
public sealed class ControlsScreen
{
    /// <summary>The actions in the order of the list; the row after them puts everything back.</summary>
    public static readonly GameAction[] Actions = Enum.GetValues<GameAction>();

    /// <summary>How many rows the list shows at once.</summary>
    public const int VisibleRows = 11;

    /// <summary>The most keys (and the most buttons) an action may have.</summary>
    public const int MostInputs = 3;

    /// <summary>A pad column capture gives up after this many frames of nothing pressed (a pad has no Esc).</summary>
    public const int PadCaptureFrames = 600;

    /// <summary>The actions without which the game could not be answered: none may be left with no key and no button.</summary>
    private static readonly GameAction[] Essential = { GameAction.Confirm, GameAction.Cancel, GameAction.Menu };

    private InputBindings working = InputBindings.For(ButtonMode.Normal);
    private ButtonMode mode;
    private PendingBinding? pending;
    private int captureFrames;

    /// <summary>A capture that clashes: the action that would take the input, and the ones that have it.</summary>
    public sealed record PendingBinding(GameAction Action, ControlsColumn Column, CapturedInput Input, GameAction[] Holders);

    public bool IsActive { get; private set; }
    public ControlsPhase Phase { get; private set; }
    public ControlsColumn Column { get; private set; }

    /// <summary>The row the cursor is on: an action's, or <see cref="ResetRow"/>.</summary>
    public int Selected { get; private set; }
    public int First { get; private set; }

    /// <summary>The line under the list: what the last change did, or why it was refused (a key of the string table, with its arguments).</summary>
    public string Notice { get; private set; } = "";

    /// <summary>The row that puts every binding back.</summary>
    public static int ResetRow => Actions.Length;

    public static int RowCount => Actions.Length + 1;

    /// <summary>The bindings as they stand now.</summary>
    public InputBindings Working => working;

    public PendingBinding? Pending => pending;

    /// <summary>The action under the cursor; null on the reset row.</summary>
    public GameAction? SelectedAction => Selected < Actions.Length ? Actions[Selected] : null;

    /// <summary>Opens on the first action with the player's bindings, the keyboard column first.</summary>
    public void Open(GameSettings settings)
    {
        IsActive = true;
        mode = settings.ButtonMode;
        working = InputBindings.From(settings);
        Phase = ControlsPhase.Browsing;
        Column = ControlsColumn.Keyboard;
        Selected = 0;
        First = 0;
        Notice = "";
        pending = null;
        InputManager.CaptureRequested = false;
    }

    public void Close()
    {
        IsActive = false;
        InputManager.CaptureRequested = false;
    }

    /// <summary>The actions that read the same key or button as <paramref name="action"/> does in the bindings now.</summary>
    public IEnumerable<GameAction> ClashesOf(GameAction action) =>
        working.Conflicts().Where(c => c.First == action || c.Second == action).Select(c => c.First == action ? c.Second : c.First).Distinct();

    /// <summary>Whether an action's inputs differ from the Button Mode's preset.</summary>
    public bool IsChanged(GameAction action)
    {
        var preset = InputBindings.For(mode)[action];
        var own = working[action];
        return !own.Keys.SequenceEqual(preset.Keys) || !own.Pad.SequenceEqual(preset.Pad);
    }

    // ------------------------------------------------------------------ moving about

    /// <summary>One step up or down the rows (the reset row last), wrapping round.</summary>
    public void Move(int dy)
    {
        if (Phase != ControlsPhase.Browsing) return;
        Selected = (Selected + dy + RowCount) % RowCount;
        First = UiNav.Window(First, Selected, RowCount, VisibleRows);
        Notice = "";
    }

    /// <summary>One step between the keyboard's column and the pad's.</summary>
    public void MoveColumn(int dx)
    {
        if (Phase != ControlsPhase.Browsing || dx == 0) return;
        Column = Column == ControlsColumn.Keyboard ? ControlsColumn.Pad : ControlsColumn.Keyboard;
    }

    // ------------------------------------------------------------------ the buttons

    /// <summary>A: on an action, wait for its new input; on the reset row, ask; while choosing, swap the two actions; while asked, put everything back.</summary>
    public void Confirm()
    {
        switch (Phase)
        {
            case ControlsPhase.Browsing:
                if (Selected == ResetRow)
                {
                    Phase = ControlsPhase.ResettingAll;
                    break;
                }
                Phase = ControlsPhase.Capturing;
                captureFrames = 0;
                Notice = "";
                InputManager.CaptureRequested = true;
                break;
            case ControlsPhase.Choosing:
                Resolve(swap: true);
                break;
            case ControlsPhase.ResettingAll:
                working = InputBindings.For(mode);
                Phase = ControlsPhase.Browsing;
                Notice = "controls.page.allReset";
                break;
        }
    }

    /// <summary>B: give up what is under way, or leave the page.</summary>
    public void Cancel()
    {
        switch (Phase)
        {
            case ControlsPhase.Capturing:
                StopCapturing();
                Notice = "controls.page.cancelled";
                break;
            case ControlsPhase.Choosing:
                Resolve(swap: false);
                break;
            case ControlsPhase.ResettingAll:
                Phase = ControlsPhase.Browsing;
                break;
            default:
                Close();
                break;
        }
    }

    /// <summary>The item button: the action under the cursor goes back to the Button Mode's keys and buttons.</summary>
    public bool ResetSelected()
    {
        if (Phase != ControlsPhase.Browsing || SelectedAction is not { } action) return false;
        working = working.With(action, InputBindings.For(mode)[action]);
        Notice = "controls.page.reset";
        return true;
    }

    private void StopCapturing()
    {
        Phase = ControlsPhase.Browsing;
        InputManager.CaptureRequested = false;
    }

    // ------------------------------------------------------------------ capturing

    /// <summary>
    /// The key or button pressed while waiting. Esc cancels a keyboard capture (a pad's button cancels too, as the
    /// pad's player has no Esc), and the wrong kind of input is passed over; a key that is another action's asks
    /// first. Returns true when a binding was changed.
    /// </summary>
    public bool Capture(CapturedInput input)
    {
        if (Phase != ControlsPhase.Capturing || SelectedAction is not { } action) return false;
        if (Column == ControlsColumn.Keyboard)
        {
            if (input.Button != null) { Cancel(); return false; }
            if (input.Key is not { } key) return false;
            if (key == KeyboardKey.Escape) { Cancel(); return false; }
        }
        else
        {
            if (input.Key == KeyboardKey.Escape) { Cancel(); return false; }
            if (input.Button is null) return false;
        }

        var holders = HoldersOf(action, input).ToArray();
        if (holders.Length > 0)
        {
            pending = new PendingBinding(action, Column, input, holders);
            Phase = ControlsPhase.Choosing;
            InputManager.CaptureRequested = false;
            return false;
        }
        return Apply(action, Column, input, Array.Empty<GameAction>());
    }

    /// <summary>The other actions that would clash with <paramref name="action"/> if it took the input.</summary>
    private IEnumerable<GameAction> HoldersOf(GameAction action, CapturedInput input)
    {
        var trial = With(working, action, Column, input);
        string name = input.Key?.ToString() ?? input.Button.ToString()!;
        return trial.Conflicts().Where(c => c.Input == name && (c.First == action || c.Second == action))
            .Select(c => c.First == action ? c.Second : c.First).Distinct();
    }

    /// <summary>The swap or the refusal of a clash.</summary>
    public bool Resolve(bool swap)
    {
        if (Phase != ControlsPhase.Choosing || pending is not { } p) return false;
        pending = null;
        Phase = ControlsPhase.Browsing;
        if (!swap)
        {
            Notice = "controls.page.kept";
            return false;
        }
        return Apply(p.Action, p.Column, p.Input, p.Holders);
    }

    /// <summary>
    /// Makes <paramref name="input"/> the first of the action's column. Each action that had it gets, in its place,
    /// what the action had first (nothing, if it had nothing). Refused if it would leave Confirm, Cancel or Menu with nothing.
    /// </summary>
    private bool Apply(GameAction action, ControlsColumn column, CapturedInput input, GameAction[] holders)
    {
        var next = With(working, action, column, input);
        var before = working[action];
        foreach (var other in holders)
        {
            var theirs = next[other];
            var mine = next[action];
            if (column == ControlsColumn.Keyboard)
            {
                var keys = theirs.Keys.Where(k => k != input.Key).ToList();
                // What the action had first goes to the other, and the action lets it go
                if (before.Keys.Length > 0 && !keys.Contains(before.Keys[0]) && keys.Count < MostInputs)
                {
                    keys.Insert(0, before.Keys[0]);
                    mine = mine with { Keys = mine.Keys.Where(k => k != before.Keys[0]).ToArray() };
                }
                next = next.With(action, mine).With(other, theirs with { Keys = keys.ToArray() });
            }
            else
            {
                var pad = theirs.Pad.Where(b => b != input.Button).ToList();
                if (before.Pad.Length > 0 && !pad.Contains(before.Pad[0]) && pad.Count < MostInputs)
                {
                    pad.Insert(0, before.Pad[0]);
                    mine = mine with { Pad = mine.Pad.Where(b => b != before.Pad[0]).ToArray() };
                }
                next = next.With(action, mine).With(other, theirs with { Pad = pad.ToArray() });
            }
        }
        if (Essential.Any(a => !next[a].IsBound))
        {
            Notice = "controls.page.needOne";
            StopCapturing();
            return false;
        }
        working = next;
        StopCapturing();
        Notice = holders.Length > 0 ? "controls.page.swapped" : "controls.page.bound";
        return true;
    }

    private static InputBindings With(InputBindings bindings, GameAction action, ControlsColumn column, CapturedInput input)
    {
        var own = bindings[action];
        if (column == ControlsColumn.Keyboard && input.Key is { } key)
            return bindings.With(action, own with { Keys = new[] { key }.Concat(own.Keys.Where(k => k != key)).Take(MostInputs).ToArray() });
        if (column == ControlsColumn.Pad && input.Button is { } button)
            return bindings.With(action, own with { Pad = new[] { button }.Concat(own.Pad.Where(b => b != button)).Take(MostInputs).ToArray() });
        return bindings;
    }

    // ------------------------------------------------------------------ with the settings

    /// <summary>Writes the bindings as overrides over the Button Mode's preset into the settings.</summary>
    public void WriteTo(GameSettings settings) => settings.Bindings = working.OverridesOn(settings.ButtonMode);

    /// <summary>
    /// One frame. Returns true when the bindings changed and the settings should be saved and put in force. While
    /// waiting for an input the game's own buttons are not read: the next key or button is the answer.
    /// </summary>
    public bool Update(GameSettings settings)
    {
        if (!IsActive) return false;
        var before = working;
        if (Phase == ControlsPhase.Capturing)
        {
            captureFrames++;
            if (InputManager.Captured is { } read) Capture(read);
            else if (Column == ControlsColumn.Pad && captureFrames > PadCaptureFrames) Cancel();
        }
        else
        {
            int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
            if (dy != 0 && Phase == ControlsPhase.Browsing)
            {
                Move(dy);
                AudioManager.PlaySound("cursor");
            }
            else if (dx != 0 && Phase == ControlsPhase.Browsing)
            {
                MoveColumn(dx);
                AudioManager.PlaySound("cursor");
            }
            else if (InputManager.IsActionPressed(GameAction.Confirm))
            {
                Confirm();
                AudioManager.PlaySound("select");
            }
            else if (InputManager.IsActionPressed(GameAction.Cancel) || (Phase == ControlsPhase.Browsing && InputManager.IsActionPressed(GameAction.Menu)))
            {
                Cancel();
                AudioManager.PlaySound("cancel");
            }
            else if (InputManager.IsActionPressed(GameAction.Item) && ResetSelected())
                AudioManager.PlaySound("select");
        }
        if (ReferenceEquals(before, working)) return false;
        WriteTo(settings);
        return true;
    }

    public void Draw(int sw, int sh)
    {
        if (IsActive) ModernUi.DrawControls(this, sw, sh);
    }
}
