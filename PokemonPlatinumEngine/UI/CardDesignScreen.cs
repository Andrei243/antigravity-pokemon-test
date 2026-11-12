using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.UI;

/// <summary>The card designer's rows (plan 43 · LF1), in their order down the panel.</summary>
public enum CardDesignRow { Background, Frame, Pose, Face, Title, Shown, Greeting, Signature, Print }

/// <summary>What the designer is doing: its rows, picking the six shown, or signing on the pad.</summary>
public enum CardDesignMode { Rows, PickShown, Pad }

/// <summary>A Pokémon the designer may show: its id, its name and level as the list writes them, and the Pokémon itself (for its icon).</summary>
public sealed record CardCandidate(Guid Id, string Name, int Level, Pokemon Pokemon);

/// <summary>A title the card may carry under the name: its kind, the Hall of Fame's region for a champion's, and the words.</summary>
public sealed record CardTitleChoice(CardTitleKind Kind, string? Region, string Words);

/// <summary>
/// The card designer (plan 43 · LF1; style guide, "The card designer"): rows of what the front shows (BACKGROUND, FRAME,
/// POSE, FACE, TITLE, SHOWN, GREETING, SIGNATURE, PRINT), left and right changing a row's value, A opening the six's
/// list, the greeting's picker, the signature's pad or the print. Its logic takes no input (<see cref="Move"/>,
/// <see cref="Change"/>, <see cref="Confirm"/>, <see cref="Cancel"/>, the pad's own), so tests and the harness drive it;
/// what it asks of the game comes back as requests (<see cref="TakeGreetingRequest"/>, <see cref="TakePrintRequest"/>).
/// The design is kept as it is changed; leaving keeps it.
/// </summary>
public sealed class CardDesignScreen
{
    private const float AppearTime = 0.3f;

    /// <summary>How many of the six's list show at once.</summary>
    public const int VisiblePicks = 8;

    /// <summary>How far the pen moves on the pad at a press, in cells.</summary>
    public const int PenStep = 2;

    private readonly List<string> backgrounds = new(), frames = new();
    private readonly List<CardTitleChoice> titles = new();
    private readonly List<CardCandidate> candidates = new();
    private readonly HeldKey upDown = new(), sideways = new();
    private float openAge;
    private bool greetingWanted, printWanted;

    public bool IsActive { get; private set; }
    public CardDesign Design { get; private set; } = new();
    public CardDesignMode Mode { get; private set; }
    public CardDesignRow Row { get; private set; }

    /// <summary>The seconds since it opened: the figure's clock.</summary>
    public float Time { get; private set; }

    public IReadOnlyList<string> Backgrounds => backgrounds;
    public IReadOnlyList<string> Frames => frames;
    public IReadOnlyList<CardTitleChoice> Titles => titles;
    public IReadOnlyList<CardCandidate> Candidates => candidates;

    /// <summary>The six's list: the cursor and the first row shown.</summary>
    public int PickIndex { get; private set; }
    public int FirstPick { get; private set; }

    /// <summary>The signature's pad and its pen: where it is, whether it writes as it moves, and whether it rubs out.</summary>
    public SignaturePad Pad { get; private set; } = new();
    public int PenX { get; private set; } = SignaturePad.Width / 2;
    public int PenY { get; private set; } = SignaturePad.Height / 2;
    public bool PenDown { get; private set; }
    public bool Eraser { get; private set; }

    /// <summary>The greeting as it is shown: its two words, or empty while none is given.</summary>
    public string Greeting { get; set; } = "";

    /// <summary>Opens on <paramref name="design"/> (a copy is changed) with what the player may choose from.</summary>
    public void Open(CardDesign? design, IEnumerable<string> backgroundsOpen, IEnumerable<string> framesOpen,
        IEnumerable<CardTitleChoice> titlesOpen, IEnumerable<CardCandidate> owned, string greeting)
    {
        Design = design?.Copy() ?? new CardDesign();
        backgrounds.Clear();
        backgrounds.AddRange(backgroundsOpen);
        frames.Clear();
        frames.AddRange(framesOpen);
        titles.Clear();
        titles.Add(new CardTitleChoice(CardTitleKind.None, null, Text.Of("cardDesign.title.none")));
        titles.AddRange(titlesOpen);
        candidates.Clear();
        candidates.AddRange(owned);
        // A look no longer open (a save moved between games) falls back to the first
        if (backgrounds.Count > 0 && !backgrounds.Contains(Design.Background)) Design.Background = backgrounds[0];
        if (frames.Count > 0 && !frames.Contains(Design.Frame)) Design.Frame = frames[0];
        if (TitleIndex < 0) (Design.Title, Design.TitleRegion) = (CardTitleKind.None, null);
        Pad = SignaturePad.FromSaved(Design.Signature);
        Greeting = greeting;
        Mode = CardDesignMode.Rows;
        Row = CardDesignRow.Background;
        PickIndex = FirstPick = 0;
        PenDown = Eraser = false;
        (PenX, PenY) = (SignaturePad.Width / 2, SignaturePad.Height / 2);
        greetingWanted = printWanted = false;
        openAge = Time = 0f;
        upDown.Release();
        sideways.Release();
        IsActive = true;
        AudioManager.PlaySound("select");
    }

    public void Close()
    {
        Design.Signature = Pad.ToSaved();
        IsActive = false;
    }

    /// <summary>Which of <see cref="Titles"/> the design carries; -1 for one no longer offered.</summary>
    public int TitleIndex => titles.FindIndex(t => t.Kind == Design.Title && (t.Kind != CardTitleKind.Champion || string.Equals(t.Region, Design.TitleRegion, StringComparison.OrdinalIgnoreCase)));

    /// <summary>Whether a Pokémon is among the six chosen (with the team as it stands, whether it is of the team's first six).</summary>
    public bool IsShown(Guid id) => Design.Shown?.Contains(id) ?? candidates.Where(c => !c.Pokemon.IsEgg).Take(Party.MaxSize).Any(c => c.Id == id);

    /// <summary>Up and down: the rows, the six's list or the pen.</summary>
    public void Move(int dy, bool held = false)
    {
        if (dy == 0) return;
        switch (Mode)
        {
            case CardDesignMode.Rows:
            {
                var rows = Enum.GetValues<CardDesignRow>();
                int to = held ? Math.Clamp((int)Row + dy, 0, rows.Length - 1) : UiNav.Wrap((int)Row, dy, rows.Length);
                if (to == (int)Row) return;
                Row = (CardDesignRow)to;
                AudioManager.PlaySound("cursor");
                break;
            }
            case CardDesignMode.PickShown:
            {
                if (candidates.Count == 0) return;
                int to = held ? Math.Clamp(PickIndex + dy, 0, candidates.Count - 1) : UiNav.Wrap(PickIndex, dy, candidates.Count);
                if (to == PickIndex) return;
                PickIndex = to;
                FirstPick = UiNav.Window(FirstPick, PickIndex, candidates.Count, VisiblePicks);
                AudioManager.PlaySound("cursor");
                break;
            }
            case CardDesignMode.Pad:
                MovePen(0, dy * PenStep);
                break;
        }
    }

    /// <summary>Left and right: a row's value, or the pen across the pad.</summary>
    public void Change(int dx)
    {
        if (dx == 0) return;
        if (Mode == CardDesignMode.Pad) { MovePen(dx * PenStep, 0); return; }
        if (Mode != CardDesignMode.Rows) return;
        int step = Math.Sign(dx);
        switch (Row)
        {
            case CardDesignRow.Background when backgrounds.Count > 1:
                Design.Background = backgrounds[UiNav.Wrap(Math.Max(0, backgrounds.IndexOf(Design.Background)), step, backgrounds.Count)];
                break;
            case CardDesignRow.Frame when frames.Count > 1:
                Design.Frame = frames[UiNav.Wrap(Math.Max(0, frames.IndexOf(Design.Frame)), step, frames.Count)];
                break;
            case CardDesignRow.Pose:
                Design.SetPose(Design.PoseIndex + step);
                break;
            case CardDesignRow.Face:
                Design.Mood = (Mood)UiNav.Wrap((int)Design.Mood, step, Enum.GetValues<Mood>().Length);
                break;
            case CardDesignRow.Title when titles.Count > 1:
            {
                var t = titles[UiNav.Wrap(Math.Max(0, TitleIndex), step, titles.Count)];
                (Design.Title, Design.TitleRegion) = (t.Kind, t.Region);
                break;
            }
            case CardDesignRow.Shown:
                // Between the team as it stands and a six of the player's own choosing (the team's first six to begin with)
                Design.Shown = Design.Shown == null
                    ? candidates.Where(c => !c.Pokemon.IsEgg).Take(Party.MaxSize).Select(c => c.Id).ToList()
                    : null;
                break;
            default:
                return;
        }
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The A button: a row's own action, a Pokémon of the list in or out of the six, or the pen down or up.</summary>
    public void Confirm()
    {
        switch (Mode)
        {
            case CardDesignMode.PickShown:
                TogglePick();
                return;
            case CardDesignMode.Pad:
                PenDown = !PenDown;
                if (PenDown) Pad.Stroke(PenX, PenY, PenX, PenY, Eraser);
                AudioManager.PlaySound("cursor");
                return;
        }
        switch (Row)
        {
            case CardDesignRow.Shown:
                Design.Shown ??= candidates.Where(c => !c.Pokemon.IsEgg).Take(Party.MaxSize).Select(c => c.Id).ToList();
                Mode = CardDesignMode.PickShown;
                PickIndex = FirstPick = 0;
                AudioManager.PlaySound("select");
                break;
            case CardDesignRow.Greeting:
                greetingWanted = true;
                AudioManager.PlaySound("select");
                break;
            case CardDesignRow.Signature:
                Mode = CardDesignMode.Pad;
                PenDown = false;
                AudioManager.PlaySound("select");
                break;
            case CardDesignRow.Print:
                printWanted = true;
                Design.Signature = Pad.ToSaved();
                break;
            default:
                Change(1);
                break;
        }
    }

    /// <summary>A Pokémon of the list put in the six or taken out (seven are never chosen, nor an Egg).</summary>
    private void TogglePick()
    {
        if (PickIndex >= candidates.Count) return;
        var c = candidates[PickIndex];
        var shown = Design.Shown ??= new List<Guid>();
        if (shown.Remove(c.Id)) AudioManager.PlaySound("cancel");
        else if (c.Pokemon.IsEgg || shown.Count >= Party.MaxSize) AudioManager.PlaySound("error");
        else
        {
            shown.Add(c.Id);
            AudioManager.PlaySound("select");
        }
    }

    /// <summary>The B button: out of the list or the pad back to the rows, then out of the designer, keeping the design.</summary>
    public void Cancel()
    {
        if (Mode != CardDesignMode.Rows)
        {
            Mode = CardDesignMode.Rows;
            PenDown = false;
            Design.Signature = Pad.ToSaved();
        }
        else Close();
        AudioManager.PlaySound("cancel");
    }

    // ------------------------------------------------------------------ the pad

    /// <summary>The pen moved by cells, writing (or rubbing out) the line it leaves while it is down.</summary>
    public void MovePen(int dx, int dy)
    {
        int x = Math.Clamp(PenX + dx, 0, SignaturePad.Width - 1), y = Math.Clamp(PenY + dy, 0, SignaturePad.Height - 1);
        PenTo(x, y);
    }

    /// <summary>The pen put on a cell (the pointer's way), writing the line from where it was while it is down.</summary>
    public void PenTo(int x, int y)
    {
        x = Math.Clamp(x, 0, SignaturePad.Width - 1);
        y = Math.Clamp(y, 0, SignaturePad.Height - 1);
        if (PenDown) Pad.Stroke(PenX, PenY, x, y, Eraser);
        (PenX, PenY) = (x, y);
    }

    /// <summary>The pen turned to an eraser and back (the item button on the pad).</summary>
    public void ToggleEraser()
    {
        if (Mode != CardDesignMode.Pad) return;
        Eraser = !Eraser;
        AudioManager.PlaySound("cursor");
    }

    /// <summary>The pad cleared (the left shoulder on the pad).</summary>
    public void ClearPad()
    {
        if (Mode != CardDesignMode.Pad) return;
        Pad.Clear();
        PenDown = false;
        AudioManager.PlaySound("page");
    }

    // ------------------------------------------------------------------ what it asks of the game

    /// <summary>True once, after A on GREETING: the engine opens the phrase picker and tells <see cref="Greeting"/> after.</summary>
    public bool TakeGreetingRequest()
    {
        bool wanted = greetingWanted;
        greetingWanted = false;
        return wanted;
    }

    /// <summary>True once, after A on PRINT: the engine writes the card's two sides as one picture.</summary>
    public bool TakePrintRequest()
    {
        bool wanted = printWanted;
        printWanted = false;
        return wanted;
    }

    // ------------------------------------------------------------------ the pointer

    /// <summary>
    /// What the pointer is over: a row (its number), a Pokémon of the list (100 and on) or a cell of the pad (10000 and
    /// on, row by row); null for nothing. From the rectangles <see cref="Draw"/> draws with.
    /// </summary>
    public int? HitTest(Vector2 at)
    {
        if (!IsActive) return null;
        if (Mode == CardDesignMode.Pad)
        {
            var pad = ModernUi.CardPadRect();
            if (!Raylib.CheckCollisionPointRec(at, pad)) return null;
            int cx = (int)((at.X - pad.X) / pad.Width * SignaturePad.Width), cy = (int)((at.Y - pad.Y) / pad.Height * SignaturePad.Height);
            return 10000 + Math.Clamp(cy, 0, SignaturePad.Height - 1) * SignaturePad.Width + Math.Clamp(cx, 0, SignaturePad.Width - 1);
        }
        if (Mode == CardDesignMode.PickShown)
        {
            var rects = ModernUi.CardPickRows(Math.Min(VisiblePicks, candidates.Count - FirstPick));
            for (int i = 0; i < rects.Count; i++)
                if (Raylib.CheckCollisionPointRec(at, rects[i])) return 100 + FirstPick + i;
            return null;
        }
        var rows = ModernUi.CardDesignRows();
        for (int i = 0; i < rows.Count; i++)
            if (Raylib.CheckCollisionPointRec(at, rows[i])) return i;
        return null;
    }

    /// <summary>The pointer over something: the cursor goes there (the pen to its cell).</summary>
    public void PointAt(int index)
    {
        if (index >= 10000)
        {
            int cell = index - 10000;
            PenTo(cell % SignaturePad.Width, cell / SignaturePad.Width);
        }
        else if (index >= 100) PickIndex = Math.Clamp(index - 100, 0, Math.Max(0, candidates.Count - 1));
        else if (index >= 0 && index < Enum.GetValues<CardDesignRow>().Length && Mode == CardDesignMode.Rows) Row = (CardDesignRow)index;
    }

    // ------------------------------------------------------------------ the frame

    public void Update(float dt = 1f / 60f)
    {
        if (!IsActive) return;
        openAge += dt;
        Time += dt;
        int dy = upDown.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down), InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        int dx = sideways.Advance(dt, InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Left, GameAction.Right, held: true));
        for (int i = 0; i < Math.Abs(dy); i++) Move(Math.Sign(dy), upDown.Repeating && Mode != CardDesignMode.Pad);
        for (int i = 0; i < Math.Abs(dx); i++) Change(Math.Sign(dx));

        if (InputManager.IsActionPressed(GameAction.Cancel) || InputManager.IsActionPressed(GameAction.Menu)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Item)) ToggleEraser();
        else if (InputManager.IsActionPressed(GameAction.PageLeft)) ClearPad();
    }

    /// <param name="face">The card's face as the design has it, made by the engine (its figure, its six, its words).</param>
    public void Draw(int screenWidth, int screenHeight, TrainerCardFace face)
    {
        if (!IsActive) return;
        FirstPick = UiNav.Window(FirstPick, PickIndex, candidates.Count, VisiblePicks);
        ModernUi.DrawCardDesigner(screenWidth, screenHeight, this, face, Math.Clamp(openAge / AppearTime, 0f, 1f));
    }
}
