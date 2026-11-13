using System;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models.Cabinets;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the match-three cabinet stands: the stage's card, the board, asking to give up, the stage's end.</summary>
public enum MatchPhase { Intro, Board, ConfirmQuit, Result }

/// <summary>
/// The match-three cabinet (plan 42 · PS6; style guide, "The arcade"): a stage of <see cref="MatchThree"/> with a cursor
/// over the board. A cell is picked with the A button and swapped with the neighbour the next arrow (or the next cell
/// clicked) names. Its steps take no input of their own (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>,
/// <see cref="SwapToward"/>), so tests and the harness drive it; the pointer's indices are the cells 0 to 35 on the board
/// and a question's two buttons. What the stage won is <see cref="Outcome"/>.
/// </summary>
public sealed class MatchThreeScreen : IPointable
{
    private const float AppearTime = 0.35f;

    /// <summary>How long a swap's clears flash before the board settles.</summary>
    public const float ClearTime = 0.45f;

    public bool IsActive { get; private set; }
    public MatchPhase Phase { get; private set; }
    public MatchThree? Board { get; private set; }

    public int Cursor { get; private set; }

    /// <summary>The cell picked to swap, or −1.</summary>
    public int Picked { get; private set; } = -1;

    /// <summary>The answer under the cursor of a question: 0 PLAY or YES, 1 LEAVE or NO.</summary>
    public int Choice { get; private set; }

    public float Age { get; private set; }
    public float ClearAge { get; private set; } = ClearTime;

    /// <summary>What the cabinet says now, under the board.</summary>
    public string Line { get; private set; } = "";

    /// <summary>Whether the stage was played to its end (cleared or out of swaps), not left before it began or given up.</summary>
    public bool Played { get; private set; }

    /// <summary>The coins the stage won (nothing unless it was cleared).</summary>
    public int Outcome => Played ? Board?.Payout ?? 0 : 0;

    public void Open(CabinetStage stage, IReadOnlyList<string> species)
    {
        Board = new MatchThree(stage, species);
        Cursor = MatchThree.Cells / 2 - MatchThree.Side / 2;
        Picked = -1;
        Choice = 0;
        Age = 0f;
        ClearAge = ClearTime;
        Played = false;
        Phase = MatchPhase.Intro;
        Line = Text.Of("cabinet.match.line.intro", ("hp", stage.Hp), ("swaps", stage.Swaps));
        IsActive = true;
        AudioManager.PlaySound("menu_open");
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
        switch (Phase)
        {
            case MatchPhase.Board:
                if (Picked >= 0)
                {
                    SwapToward(dx, dy);
                    return;
                }
                int side = MatchThree.Side;
                int column = ((Cursor % side + dx) % side + side) % side;
                int row = ((Cursor / side + dy) % side + side) % side;
                Cursor = row * side + column;
                AudioManager.PlaySound("cursor");
                break;
            case MatchPhase.Intro or MatchPhase.ConfirmQuit:
                Choice = 1 - Choice;
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    /// <summary>The picked cell swapped with its neighbour one step that way (the cursor goes with it).</summary>
    public void SwapToward(int dx, int dy)
    {
        if (!IsActive || Phase != MatchPhase.Board || Picked < 0) return;
        int x = Picked % MatchThree.Side + Math.Sign(dx), y = Picked / MatchThree.Side + (dx != 0 ? 0 : Math.Sign(dy));
        if (x < 0 || y < 0 || x >= MatchThree.Side || y >= MatchThree.Side)
        {
            AudioManager.PlaySound("error");
            return;
        }
        SwapWith(y * MatchThree.Side + x);
    }

    private void SwapWith(int other)
    {
        var board = Board!;
        int from = Picked;
        Picked = -1;
        var result = board.Swap(from, other);
        if (result != SwapResult.Made)
        {
            AudioManager.PlaySound("error");
            Line = Text.Of(result == SwapResult.NoLine ? "cabinet.match.line.noLine" : "cabinet.match.line.notNeighbours");
            return;
        }
        Cursor = other;
        ClearAge = 0f;
        int damage = 0, cascades = 0;
        float best = 0f;
        foreach (var c in board.LastClears)
        {
            damage += c.Damage;
            cascades = Math.Max(cascades, c.Cascade);
            best = MathF.Max(best, c.Effect);
        }
        AudioManager.PlaySound(best > 1f ? "hit_super" : best < 1f ? "hit_weak" : "hit_normal");
        Line = cascades > 1 ? Text.Of("cabinet.match.line.cascade", ("damage", damage), ("cascades", cascades))
            : best > 1f ? Text.Of("cabinet.match.line.super", ("damage", damage))
            : best < 1f ? Text.Of("cabinet.match.line.weak", ("damage", damage))
            : Text.Of("cabinet.match.line.hit", ("damage", damage));
        if (board.Over) ToResult();
    }

    private void ToResult()
    {
        var board = Board!;
        Played = true;
        Phase = MatchPhase.Result;
        if (board.Cleared)
        {
            AudioManager.PlaySound("coin_land");
            Line = Text.Of("cabinet.match.line.cleared", ("coins", board.Payout));
        }
        else Line = Text.Of("cabinet.match.line.failed");
    }

    /// <summary>The A button: PLAY or LEAVE; a cell picked (or a picked cell let go, or swapped with the cell under the cursor); YES or NO to giving up; the end put away.</summary>
    public void Confirm()
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case MatchPhase.Intro:
                if (Choice == 1) { Close(); return; }
                Phase = MatchPhase.Board;
                Line = Text.Of("cabinet.match.line.pick");
                AudioManager.PlaySound("select");
                break;
            case MatchPhase.Board:
                if (Picked == Cursor) { Picked = -1; AudioManager.PlaySound("cancel"); }
                else if (Picked >= 0 && MatchThree.Neighbours(Picked, Cursor)) SwapWith(Cursor);
                else { Picked = Cursor; AudioManager.PlaySound("select"); }
                break;
            case MatchPhase.ConfirmQuit:
                if (Choice == 0) Close();
                else { Phase = MatchPhase.Board; AudioManager.PlaySound("cancel"); }
                break;
            case MatchPhase.Result:
                Close();
                break;
        }
    }

    /// <summary>The B button: a picked cell let go; on the board the question whether to give up; at a question NO, or LEAVE; the end put away.</summary>
    public void Cancel()
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case MatchPhase.Board when Picked >= 0:
                Picked = -1;
                AudioManager.PlaySound("cancel");
                break;
            case MatchPhase.Board:
                Phase = MatchPhase.ConfirmQuit;
                Choice = 1;
                AudioManager.PlaySound("select");
                break;
            case MatchPhase.ConfirmQuit:
                Phase = MatchPhase.Board;
                AudioManager.PlaySound("cancel");
                break;
            case MatchPhase.Intro:
                Close();
                break;
            case MatchPhase.Result:
                Close();
                break;
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        ClearAge = Math.Min(ClearTime, ClearAge + dt);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else Move(InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Up, GameAction.Down));
    }

    // ------------------------------------------------------------------ the pointer

    public int? HitTest(Vector2 at)
    {
        if (!IsActive) return null;
        switch (Phase)
        {
            case MatchPhase.Board:
                for (int i = 0; i < MatchThree.Cells; i++)
                    if (Inside(ModernUi.MatchCellRect(i), at)) return i;
                return null;
            case MatchPhase.Intro or MatchPhase.ConfirmQuit:
                return UiPointer.Hit(ModernUi.PromptButtons(GameEngine.VirtualWidth, GameEngine.VirtualHeight, 2), at);
            case MatchPhase.Result:
                return UiPointer.Anywhere;
            default:
                return null;
        }
    }

    public void PointAt(int index)
    {
        if (!IsActive) return;
        if (Phase == MatchPhase.Board && index >= 0 && index < MatchThree.Cells) Cursor = index;
        else if (Phase is MatchPhase.Intro or MatchPhase.ConfirmQuit && index is 0 or 1) Choice = index;
    }

    private static bool Inside(Raylib_cs.Rectangle r, Vector2 at) =>
        at.X >= r.X && at.X < r.X + r.Width && at.Y >= r.Y && at.Y < r.Y + r.Height;

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawMatchThree(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
