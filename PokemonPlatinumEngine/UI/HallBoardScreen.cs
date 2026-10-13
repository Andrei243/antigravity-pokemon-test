using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the Battle Hall's board stands (the original's <c>State_SelectNextBattle</c>, for one player).</summary>
public enum HallBoardStep
{
    /// <summary>The cursor is on the board.</summary>
    Pick,
    /// <summary>A type is chosen: "battle this type?".</summary>
    Confirm,
    /// <summary>The Hall Matron is on her way: said until a button is pressed.</summary>
    MatronNotice,
    /// <summary>The entered Pokémon's summary is being read.</summary>
    Summary
}

/// <summary>
/// The Battle Hall's board of types (plan 06 · R18c; style guide, "The Battle Hall and the Battle Castle"), on the
/// original's application (<c>src/applications/frontier/battle_hall/main.c</c>): twenty places, four to a row. The
/// first seventeen are the types with the rank each stands at; the next two, which the cursor takes as one, are the
/// entered Pokémon, whose summary they open; the last is the Hall Matron's, which can't be chosen until she comes. A
/// type beaten ten times can't be chosen any more. Before battles 50 and 170 of a Single streak the type is chosen
/// and agreed to as ever, and then every type goes dark and only the last place is left: the type chosen first is
/// still the one whose rank the win will raise. There is no way out without choosing, as in the original. Its logic
/// takes no input (<see cref="Move"/>, <see cref="Confirm"/>, <see cref="Cancel"/>), so tests and the harness drive it.
/// </summary>
public sealed class HallBoardScreen
{
    /// <summary><c>GRID_WIDTH</c>, <c>GRID_HEIGHT</c>, and the places of the entered Pokémon and of the Hall Matron.</summary>
    public const int Columns = 4, Rows = 5, Cells = Columns * Rows, SummaryCell = 17, MatronCell = 19;

    private const float AppearTime = 0.3f;

    public bool IsActive { get; private set; }
    public HallBoardStep Step { get; private set; }
    public BattleHallChallenge? Challenge { get; private set; }

    /// <summary>The Pokémon entered: one, or the two of a Double challenge.</summary>
    public IReadOnlyList<Pokemon> Entered { get; private set; } = Array.Empty<Pokemon>();

    /// <summary>The place under the cursor, 0 to 19 (<c>cursorPos</c>); 17 and 18 are one place.</summary>
    public int Cursor { get; private set; }

    /// <summary>The cursor in the yes and the no.</summary>
    public int MenuCursor { get; private set; }

    /// <summary>The Hall Matron comes next (<c>NextOpponentIsHallMatron</c>), and has been announced (<c>matronBattleAvailable</c>).</summary>
    public bool MatronNext { get; private set; }
    public bool MatronOpen { get; private set; }

    /// <summary>The type agreed to before the Hall Matron was announced (<c>savedCursorPos</c>); -1 until then.</summary>
    public int Saved { get; private set; } = -1;

    /// <summary>The place of the type chosen as the screen closed; -1 while it is open.</summary>
    public int Outcome { get; private set; } = -1;

    public float Age { get; private set; }
    public float StepAge { get; private set; }

    // The last place the cursor stood on that wasn't the Pokémon's (prevCursorPos): where it goes back to from there
    private int previous;

    /// <summary>The board is opened for the battle to come (<c>BattleHallApp_Init</c>): the cursor is where it was left.</summary>
    public void Open(BattleHallChallenge challenge, IReadOnlyList<Pokemon> entered)
    {
        Challenge = challenge;
        Entered = entered;
        IsActive = true;
        Cursor = previous = Math.Max(0, challenge.Cell);
        MenuCursor = 0;
        MatronNext = challenge.ArgentaNext != FrontierPrint.None;
        MatronOpen = false;
        Saved = Outcome = -1;
        Age = 0f;
        Go(HallBoardStep.Pick);
        AudioManager.PlaySound("select");
    }

    private void Go(HallBoardStep step)
    {
        Step = step;
        StepAge = 0f;
    }

    public static bool IsSummary(int cell) => cell is SummaryCell or SummaryCell + 1;

    /// <summary>Whether the place under the cursor may be chosen now (<c>GrayOutUnselectableCells</c>).</summary>
    public bool CanChoose(int cell)
    {
        if (IsSummary(cell)) return true;
        if (MatronOpen) return cell == MatronCell;
        return cell < BattleHallChallenge.Board.Length && Challenge is { } challenge && !challenge.Beaten(cell);
    }

    public string Prompt => Step switch
    {
        HallBoardStep.Confirm when MatronOpen => Text.Of("hallBoard.prompt.matron"),
        HallBoardStep.Confirm => Text.Of("hallBoard.prompt.type", ("type", Words.Type(BattleHallChallenge.Board[Cursor]))),
        HallBoardStep.MatronNotice => Text.Of("hallBoard.prompt.matronComing"),
        _ when MatronOpen => Text.Of("hallBoard.prompt.matronWaiting"),
        _ => Text.Of("hallBoard.prompt.choose")
    };

    // ------------------------------------------------------------------ the buttons

    /// <summary>
    /// <c>UpdateCursorPosition</c>: the cursor goes round the edges of the board; the Pokémon's two places are come
    /// to as one and left for the place they were come from.
    /// </summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive) return;
        if (Step == HallBoardStep.Confirm)
        {
            if (dx != 0) { MenuCursor = 1 - MenuCursor; AudioManager.PlaySound("cursor"); }
            return;
        }
        if (Step != HallBoardStep.Pick || dx == 0 && dy == 0) return;
        bool summary = IsSummary(Cursor);
        if (!summary) previous = Cursor;
        if (dx < 0) Cursor = Cursor % Columns == 0 ? Cursor + Columns - 1 : summary ? 16 : Cursor - 1;
        else if (dx > 0) Cursor = Cursor % Columns == Columns - 1 ? Cursor - (Columns - 1) : summary ? MatronCell : Cursor + 1;
        else if (dy < 0)
        {
            if (Cursor < Columns) Cursor += Columns * (Rows - 1);
            else if (summary) Cursor = previous switch { 16 or 13 or 1 => 13, 19 or 14 or 2 => 14, _ => Cursor };
            else Cursor -= Columns;
        }
        else if (Cursor >= Columns * (Rows - 1)) Cursor -= Columns * (Rows - 1);
        else Cursor += Columns;
        AudioManager.PlaySound("cursor");
    }

    public void Confirm()
    {
        if (!IsActive) return;
        switch (Step)
        {
            case HallBoardStep.Pick:
                if (IsSummary(Cursor)) Go(HallBoardStep.Summary);
                else if (!CanChoose(Cursor)) { AudioManager.PlaySound("error"); return; }
                else
                {
                    MenuCursor = 0;
                    Go(HallBoardStep.Confirm);
                }
                AudioManager.PlaySound("select");
                break;
            case HallBoardStep.Confirm:
                if (MenuCursor != 0) { Cancel(); return; }
                if (MatronNext && !MatronOpen)
                {
                    Saved = Cursor;
                    Go(HallBoardStep.MatronNotice);
                    AudioManager.PlaySound("select");
                    return;
                }
                Outcome = Saved >= 0 ? Saved : Cursor;
                IsActive = false;
                AudioManager.PlaySound("select");
                break;
            case HallBoardStep.MatronNotice:
                // SELECT_STATE_HIGHLIGHT_MATRON_CELL: every type goes dark, and the cursor stands on the last place
                MatronOpen = true;
                Cursor = MatronCell;
                Go(HallBoardStep.Pick);
                AudioManager.PlaySound("select");
                break;
            case HallBoardStep.Summary:
                Go(HallBoardStep.Pick);
                AudioManager.PlaySound("cancel");
                break;
        }
    }

    /// <summary>Back out of a question or a summary. The board itself can't be left without a type.</summary>
    public void Cancel()
    {
        if (!IsActive) return;
        switch (Step)
        {
            case HallBoardStep.Confirm:
            case HallBoardStep.Summary:
                Go(HallBoardStep.Pick);
                AudioManager.PlaySound("cancel");
                break;
            case HallBoardStep.MatronNotice:
                Confirm();
                break;
        }
    }

    // ------------------------------------------------------------------ the frame

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        StepAge += dt;
        int dx = InputManager.Axis(GameAction.Left, GameAction.Right), dy = InputManager.Axis(GameAction.Up, GameAction.Down);
        if (dx != 0 || dy != 0) Move(dx, dx != 0 ? 0 : dy);
        else if (InputManager.IsActionPressed(GameAction.Cancel)) Cancel();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Confirm();
    }

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive || Challenge == null) return;
        if (Step == HallBoardStep.Summary && Entered.Count > 0)
            ModernUi.DrawSummary(screenWidth, screenHeight, Entered[0], 0, 1, Math.Clamp(StepAge / AppearTime, 0f, 1f));
        else ModernUi.DrawHallBoard(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
