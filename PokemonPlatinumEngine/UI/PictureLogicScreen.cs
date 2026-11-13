using System;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models.Cabinets;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the picture-logic cabinet stands: finding a picture, none to offer, the grid, asking to give up, the picture solved.</summary>
public enum PicturePhase { Finding, NoneLeft, Board, ConfirmQuit, Solved }

/// <summary>
/// The picture-logic cabinet (plan 42 · PS6; style guide, "The arcade"): the next species of the Pokédex's seen whose
/// silhouette solves alone (<see cref="Silhouettes"/>), as a <see cref="PictureLogic"/> with a cursor over its grid. The
/// A button fills a cell, the item button crosses it. Its steps take no input of their own (<see cref="Move"/>,
/// <see cref="Fill"/>, <see cref="Cross"/>, <see cref="Cancel"/>, <see cref="Advance"/>), so tests and the harness drive
/// it; the pointer's indices are the cells on the grid and a question's two buttons. What the picture won is <see cref="Outcome"/>.
/// </summary>
public sealed class PictureLogicScreen : IPointable
{
    private const float AppearTime = 0.35f;

    /// <summary>How many species are looked through in a frame while no picture is known.</summary>
    public const int LookedAtAFrame = 4;

    public bool IsActive { get; private set; }
    public PicturePhase Phase { get; private set; }
    public PictureLogic? Puzzle { get; private set; }

    public int CursorX { get; private set; }
    public int CursorY { get; private set; }
    public int Choice { get; private set; }
    public float Age { get; private set; }

    /// <summary>When the picture was solved (its reveal runs from then).</summary>
    public float SolvedAge { get; private set; }

    public string Line { get; private set; } = "";

    /// <summary>The coins the picture won (nothing until it is solved).</summary>
    public int Outcome => Phase == PicturePhase.Solved && Puzzle != null ? CabinetDatabase.PayFor(Puzzle.Size, Puzzle.Mistakes) : 0;

    private readonly Queue<string> candidates = new();

    /// <summary>A sitting that looks through <paramref name="species"/> in order for the first with a picture.</summary>
    public void Open(IEnumerable<string> species)
    {
        candidates.Clear();
        foreach (var s in species) candidates.Enqueue(s);
        Puzzle = null;
        Choice = 0;
        Age = 0f;
        SolvedAge = 0f;
        Phase = PicturePhase.Finding;
        Line = Text.Of("cabinet.picture.line.finding");
        IsActive = true;
        AudioManager.PlaySound("menu_open");
        Advance();
    }

    /// <summary>A sitting on a picture already made (a test's, the harness's).</summary>
    public void Open(PictureLogic puzzle)
    {
        candidates.Clear();
        Begin(puzzle);
        Age = 0f;
        IsActive = true;
    }

    private void Begin(PictureLogic puzzle)
    {
        Puzzle = puzzle;
        CursorX = 0;
        CursorY = 0;
        Choice = 0;
        Phase = PicturePhase.Board;
        Line = Text.Of("cabinet.picture.line.board", ("size", puzzle.Size));
    }

    /// <summary>While finding: asks the next species' picture, a few a frame, and begins on the first that has one.</summary>
    public void Advance()
    {
        if (!IsActive || Phase != PicturePhase.Finding) return;
        for (int n = 0; n < LookedAtAFrame && candidates.Count > 0; n++)
        {
            string species = candidates.Peek();
            if (!Silhouettes.Ask(species, out var picture)) return;
            candidates.Dequeue();
            if (picture == null) continue;
            Begin(new PictureLogic(species, picture.Size, picture.Cells));
            return;
        }
        if (candidates.Count == 0)
        {
            Phase = PicturePhase.NoneLeft;
            Line = Text.Of("cabinet.picture.line.none");
        }
    }

    public void Close()
    {
        if (!IsActive) return;
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    public void Move(int dx, int dy)
    {
        if (!IsActive || (dx == 0 && dy == 0)) return;
        if (Phase == PicturePhase.Board)
        {
            int size = Puzzle!.Size;
            CursorX = ((CursorX + dx) % size + size) % size;
            CursorY = ((CursorY + dy) % size + size) % size;
            AudioManager.PlaySound("cursor");
        }
        else if (Phase == PicturePhase.ConfirmQuit)
        {
            Choice = 1 - Choice;
            AudioManager.PlaySound("cursor");
        }
    }

    /// <summary>The A button: the cell under the cursor filled (a mistake crosses it); YES or NO to giving up; the end put away.</summary>
    public void Fill()
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case PicturePhase.Board:
            {
                var puzzle = Puzzle!;
                if (puzzle[CursorX, CursorY] != CellMark.None) { AudioManager.PlaySound("error"); return; }
                if (puzzle.Fill(CursorX, CursorY)) AudioManager.PlaySound("select");
                else
                {
                    AudioManager.PlaySound("error");
                    Line = Text.Of("cabinet.picture.line.mistake", ("mistakes", puzzle.Mistakes));
                }
                if (puzzle.Solved)
                {
                    Phase = PicturePhase.Solved;
                    SolvedAge = Age;
                    AudioManager.PlaySound("coin_land");
                    Line = Text.Of("cabinet.picture.line.solved", ("name", Data.LocalNames.Species(puzzle.Species)), ("coins", Outcome));
                }
                break;
            }
            case PicturePhase.ConfirmQuit:
                if (Choice == 0) Close();
                else { Phase = PicturePhase.Board; AudioManager.PlaySound("cancel"); }
                break;
            case PicturePhase.Solved or PicturePhase.NoneLeft:
                Close();
                break;
        }
    }

    /// <summary>The item button: the cell under the cursor crossed out, or its cross taken off.</summary>
    public void Cross()
    {
        if (!IsActive || Phase != PicturePhase.Board) return;
        Puzzle!.Cross(CursorX, CursorY);
        AudioManager.PlaySound("page");
    }

    /// <summary>The B button: on the grid the question whether to give up; at it NO; while finding or at the end, away.</summary>
    public void Cancel()
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case PicturePhase.Board:
                Phase = PicturePhase.ConfirmQuit;
                Choice = 1;
                AudioManager.PlaySound("select");
                break;
            case PicturePhase.ConfirmQuit:
                Phase = PicturePhase.Board;
                AudioManager.PlaySound("cancel");
                break;
            default:
                Close();
                break;
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        Advance();
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Fill();
        else if (InputManager.IsActionPressed(GameAction.Item)) Cross();
        else Move(InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Up, GameAction.Down));
    }

    // ------------------------------------------------------------------ the pointer

    public int? HitTest(Vector2 at)
    {
        if (!IsActive) return null;
        switch (Phase)
        {
            case PicturePhase.Board:
            {
                int size = Puzzle!.Size;
                for (int i = 0; i < size * size; i++)
                    if (Inside(ModernUi.PictureCellRect(size, i % size, i / size), at)) return i;
                return null;
            }
            case PicturePhase.ConfirmQuit:
                return UiPointer.Hit(ModernUi.PromptButtons(GameEngine.VirtualWidth, GameEngine.VirtualHeight, 2), at);
            case PicturePhase.Solved or PicturePhase.NoneLeft:
                return UiPointer.Anywhere;
            default:
                return null;
        }
    }

    public void PointAt(int index)
    {
        if (!IsActive) return;
        if (Phase == PicturePhase.Board && index >= 0 && index < Puzzle!.Size * Puzzle.Size)
        {
            CursorX = index % Puzzle.Size;
            CursorY = index / Puzzle.Size;
        }
        else if (Phase == PicturePhase.ConfirmQuit && index is 0 or 1) Choice = index;
    }

    private static bool Inside(Raylib_cs.Rectangle r, Vector2 at) =>
        at.X >= r.X && at.X < r.X + r.Width && at.Y >= r.Y && at.Y < r.Y + r.Height;

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawPictureLogic(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
