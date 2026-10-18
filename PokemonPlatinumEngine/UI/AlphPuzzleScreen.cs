using System;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.UI;

/// <summary>
/// A stone panel of the Ruins of Alph on screen (plan 20 · J5; style guide, "Menu screens", "The Ruins of Alph's
/// panels"): the board of <see cref="AlphPuzzle"/>, a cursor over it and the piece in hand. Once the picture is whole
/// the panel glows a moment and closes by itself, solved; B with nothing in hand leaves it as it is. Its logic takes no
/// input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>, <see cref="Turn"/>, <see cref="Advance"/>), so
/// tests and the harness drive it.
/// </summary>
public sealed class AlphPuzzleScreen
{
    /// <summary>How long the whole picture glows before the panel closes.</summary>
    public const float SolvedTime = 1.6f;

    private const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public AlphPuzzle? Puzzle { get; private set; }

    /// <summary>Seconds since the panel opened, and since it was solved (null until then).</summary>
    public float Age { get; private set; }
    public float? SolvedAge { get; private set; }

    /// <summary>How the panel closed: true solved, false left; null while it is open.</summary>
    public bool? Outcome { get; private set; }

    public void Open(string name)
    {
        Puzzle = new AlphPuzzle(name);
        IsActive = true;
        Age = 0f;
        SolvedAge = null;
        Outcome = null;
    }

    public void Move(int dx, int dy)
    {
        if (!IsActive || SolvedAge != null || Puzzle == null) return;
        Puzzle.Move(dx, dy);
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        if (!IsActive || SolvedAge != null || Puzzle == null) return;
        if (!Puzzle.Confirm()) { AudioManager.PlaySound("error"); return; }
        AudioManager.PlaySound("select");
        if (Puzzle.Solved) Solve();
    }

    public void Turn(int way)
    {
        if (!IsActive || SolvedAge != null || Puzzle == null) return;
        if (!Puzzle.Turn(way)) return;
        AudioManager.PlaySound("cursor");
        if (Puzzle.Solved) Solve();
    }

    public void Cancel()
    {
        if (!IsActive || SolvedAge != null || Puzzle == null) return;
        if (Puzzle.Cancel()) { AudioManager.PlaySound("cancel"); return; }
        AudioManager.PlaySound("cancel");
        Close(false);
    }

    /// <summary>The clock: the glow of a solved panel runs out and closes it.</summary>
    public void Advance(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        if (SolvedAge is { } solved)
        {
            SolvedAge = solved + dt;
            if (SolvedAge >= SolvedTime) Close(true);
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Advance(dt);
        if (!IsActive || SolvedAge != null) return;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (dx != 0 || dy != 0) Move(dx, dx != 0 ? 0 : dy);
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.PageRight)) Turn(1);
        else if (InputManager.IsActionPressed(GameAction.PageLeft)) Turn(-1);
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive || Puzzle == null) return;
        ModernUi.DrawAlphPuzzle(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }

    private void Solve()
    {
        SolvedAge = 0f;
        AudioManager.PlaySound("levelup");
    }

    private void Close(bool solved)
    {
        Outcome = solved;
        IsActive = false;
    }
}
