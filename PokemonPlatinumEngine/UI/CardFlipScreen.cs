using System;
using System.Numerics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI.Kit;

namespace PokemonPlatinumEngine.UI;

/// <summary>Where the card-flip table stands: asking to play, a round on the board, asking to quit, the look after a round, the case full.</summary>
public enum CardFlipPhase { Menu, Board, ConfirmQuit, Reveal, Full }

/// <summary>
/// The card-flip table (plan 42 · PS3; style guide, "Fossils, the Game Corner, the Villa and Pal Park", "The card-flip
/// game"): a sitting of <see cref="CardFlipGame"/> with a cursor over its cards, the memo's marks and a way to quit. Its
/// steps take no input of their own (<see cref="Move"/>, <see cref="Flip"/>, <see cref="Memo"/>, <see cref="Quit"/>), so
/// tests and the harness drive it; the pointer finds what it points at from the rectangles the drawing uses
/// (<see cref="HitTest"/>: on the board the cards 0 to 24, the memo's marks 25 to 28, QUIT 29; a question's two buttons
/// 0 and 1; anywhere after a round). What the sitting won is <see cref="Outcome"/>.
/// </summary>
public sealed class CardFlipScreen : IPointable
{
    private const float AppearTime = 0.35f;

    /// <summary>How long a card takes to turn over.</summary>
    public const float FlipTime = 0.25f;

    /// <summary>The pointer's indices on the board beyond the cards.</summary>
    public const int FirstMark = CardFlipBoard.Cards, QuitButton = FirstMark + CardFlipGame.Marks;

    public bool IsActive { get; private set; }
    public CardFlipPhase Phase { get; private set; }
    public CardFlipGame? Game { get; private set; }

    /// <summary>The card under the cursor, 0 to 24 from the top left along each row.</summary>
    public int Cursor { get; private set; }

    /// <summary>The memo's mark the item button puts on and takes off (0 a bust, 1 to 3).</summary>
    public int MemoMark { get; private set; }

    /// <summary>The answer under the cursor of a question: 0 PLAY or YES, 1 LEAVE or NO.</summary>
    public int Choice { get; private set; }

    public float Age { get; private set; }

    /// <summary>The card turned last and how long ago, for its turn.</summary>
    public int LastFlipped { get; private set; } = -1;
    public float FlipAge { get; private set; } = FlipTime;

    /// <summary>Which cards were face up before the look after a round, which turns the rest.</summary>
    public bool[] UpBefore { get; } = new bool[CardFlipBoard.Cards];

    /// <summary>What the table says now, under the panel.</summary>
    public string Line { get; private set; } = "";

    /// <summary>The levels the last round moved the player, shown on the question (up over nought).</summary>
    public int LevelChange { get; private set; }

    /// <summary>The coins the sitting won (the case now against the case at the start; a bust only loses the round's).</summary>
    public int Outcome => Game?.Net ?? 0;

    /// <summary>The coins in the case now.</summary>
    public int Coins => Game?.Coins ?? 0;

    /// <summary>A sitting begun with the case's coins, drawing on <paramref name="next"/>(n), a number from 0 to n − 1.</summary>
    public void Open(int coins, Func<int, int> next)
    {
        Game = new CardFlipGame(next, coins);
        Cursor = 0;
        MemoMark = 0;
        Choice = 0;
        Age = 0f;
        LastFlipped = -1;
        FlipAge = FlipTime;
        LevelChange = 0;
        Array.Clear(UpBefore);
        IsActive = true;
        // VoltorbFlipTaskEngine_InitChecks_Begin: a full case deals nothing; otherwise the first board is dealt at once
        Game.Deal();
        ToMenu();
        AudioManager.PlaySound("menu_open");
    }

    /// <summary>A sitting drawing on a generator the caller made.</summary>
    public void Open(int coins, Random random) => Open(coins, random.Next);

    public void Close()
    {
        if (!IsActive) return;
        IsActive = false;
        AudioManager.PlaySound("menu_close");
    }

    private void ToMenu()
    {
        Choice = 0;
        if (Game!.CaseFull)
        {
            Phase = CardFlipPhase.Full;
            Line = Text.Of("cardFlip.line.full");
            return;
        }
        Phase = CardFlipPhase.Menu;
        Line = Text.Of("cardFlip.line.menu");
    }

    /// <summary>The cursor over the cards (wrapping round a row or a column), or between a question's two answers.</summary>
    public void Move(int dx, int dy)
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case CardFlipPhase.Board when dx != 0 || dy != 0:
            {
                int side = CardFlipBoard.Side;
                int column = ((Cursor % side + dx) % side + side) % side;
                int row = ((Cursor / side + dy) % side + side) % side;
                Cursor = row * side + column;
                AudioManager.PlaySound("cursor");
                break;
            }
            case CardFlipPhase.Menu or CardFlipPhase.ConfirmQuit when dx != 0 || dy != 0:
                Choice = 1 - Choice;
                AudioManager.PlaySound("cursor");
                break;
        }
    }

    /// <summary>
    /// The A button: the card under the cursor turned; PLAY or LEAVE taken; YES or NO to quitting; the look after a round
    /// put away.
    /// </summary>
    public void Flip()
    {
        if (!IsActive) return;
        var game = Game!;
        switch (Phase)
        {
            case CardFlipPhase.Menu:
                if (Choice == 1)
                {
                    Close();
                    return;
                }
                if (!game.InPlay && !game.Deal()) { ToMenu(); return; }
                Phase = CardFlipPhase.Board;
                LastFlipped = -1;
                FlipAge = FlipTime;
                Line = Text.Of("cardFlip.line.level", ("level", game.ShownLevel));
                AudioManager.PlaySound("select");
                break;
            case CardFlipPhase.Board:
            {
                if (game.Board!.IsFlipped(Cursor))
                {
                    AudioManager.PlaySound("error");
                    return;
                }
                int before = game.Payout;
                var face = game.Flip(Cursor);
                LastFlipped = Cursor;
                FlipAge = 0f;
                if (face == CardFace.Bust)
                {
                    // The cracked tile: rock_smash brings its own rumble (Haptics.Pulses)
                    AudioManager.PlaySound("rock_smash");
                    Line = Text.Of("cardFlip.line.bust");
                    ToReveal();
                    return;
                }
                int value = CardFlipBoard.ValueOf(face);
                AudioManager.PlaySound(value > 1 ? "coin_flip" : "page");
                Line = before == 0
                    ? Text.Of("cardFlip.line.first", ("coins", game.Payout))
                    : value > 1 ? Text.Of("cardFlip.line.times", ("times", value), ("coins", game.Payout)) : Text.Of("cardFlip.line.one", ("coins", game.Payout));
                if (game.Outcome == CardFlipOutcome.Won)
                {
                    AudioManager.PlaySound("coin_land");
                    Line = Text.Of("cardFlip.line.won", ("coins", game.Payout));
                    ToReveal();
                }
                break;
            }
            case CardFlipPhase.ConfirmQuit:
                if (Choice == 0)
                {
                    int payout = game.Payout;
                    game.Quit();
                    if (payout > 0) AudioManager.PlaySound("coin_land");
                    Line = payout > 0 ? Text.Of("cardFlip.line.kept", ("coins", payout)) : Text.Of("cardFlip.line.nothing");
                    ToReveal();
                }
                else
                {
                    Phase = CardFlipPhase.Board;
                    AudioManager.PlaySound("cancel");
                }
                break;
            case CardFlipPhase.Reveal:
                game.EndRound();
                game.Deal();
                // The round after: dealt face down, its level and how it moved shown on the question
                LevelChange = game.LevelsGained;
                AskForTheNext();
                break;
            case CardFlipPhase.Full:
                Close();
                break;
        }
    }

    /// <summary>
    /// The question before a round (<c>VoltorbFlipTaskEngine_NewRound_Begin</c>, then <c>SelectMainMenu</c>): the board
    /// is dealt first, since dealing chooses its level, and the question asks to play that level over it face down.
    /// </summary>
    private void AskForTheNext()
    {
        Array.Clear(UpBefore);
        LastFlipped = -1;
        FlipAge = FlipTime;
        Choice = 0;
        if (Game!.CaseFull)
        {
            Phase = CardFlipPhase.Full;
            Line = Text.Of("cardFlip.line.full");
            return;
        }
        Phase = CardFlipPhase.Menu;
        Line = LevelChange > 0 ? Text.Of("cardFlip.line.up", ("level", Game.ShownLevel))
            : LevelChange < 0 ? Text.Of("cardFlip.line.down", ("level", Game.ShownLevel))
            : Text.Of("cardFlip.line.menu");
    }

    private void ToReveal()
    {
        var board = Game!.Board!;
        for (int i = 0; i < CardFlipBoard.Cards; i++) UpBefore[i] = board.IsFlipped(i);
        Game.Reveal();
        Phase = CardFlipPhase.Reveal;
        Choice = 0;
    }

    /// <summary>The memo's mark <paramref name="mark"/> (0 a bust, 1 to 3) on or off the card under the cursor.</summary>
    public void Memo(int mark)
    {
        if (!IsActive || Phase != CardFlipPhase.Board) return;
        MemoMark = Math.Clamp(mark, 0, CardFlipGame.Marks - 1);
        AudioManager.PlaySound(Game!.ToggleMemo(Cursor, MemoMark) ? "select" : "error");
    }

    /// <summary>The memo's chosen mark one along, wrapping.</summary>
    public void ChangeMark(int step)
    {
        if (!IsActive || Phase != CardFlipPhase.Board || step == 0) return;
        MemoMark = UiNav.Wrap(MemoMark, Math.Sign(step), CardFlipGame.Marks);
        AudioManager.PlaySound("cursor");
    }

    /// <summary>
    /// The B button: on the board the question whether to quit (keeping the round's coins); at a question NO, or LEAVE
    /// on the first; the look after a round put away.
    /// </summary>
    public void Quit()
    {
        if (!IsActive) return;
        switch (Phase)
        {
            case CardFlipPhase.Board:
                Phase = CardFlipPhase.ConfirmQuit;
                Choice = 1;
                AudioManager.PlaySound("select");
                break;
            case CardFlipPhase.ConfirmQuit:
                Choice = 1;
                Flip();
                break;
            case CardFlipPhase.Menu:
                Choice = 1;
                Flip();
                break;
            case CardFlipPhase.Reveal or CardFlipPhase.Full:
                Flip();
                break;
        }
    }

    public void Update(float dt)
    {
        if (!IsActive) return;
        Age += dt;
        FlipAge = Math.Min(FlipTime, FlipAge + dt);
        if (InputManager.IsActionPressed(GameAction.Cancel)) Quit();
        else if (InputManager.IsActionPressed(GameAction.Confirm)) Flip();
        else if (InputManager.IsActionPressed(GameAction.Item)) Memo(MemoMark);
        else if (InputManager.IsActionPressed(GameAction.PageLeft)) ChangeMark(-1);
        else if (InputManager.IsActionPressed(GameAction.PageRight)) ChangeMark(1);
        else Move(InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Up, GameAction.Down));
    }

    // ------------------------------------------------------------------ the pointer

    public int? HitTest(Vector2 at)
    {
        if (!IsActive) return null;
        int sw = GameEngine.VirtualWidth, sh = GameEngine.VirtualHeight;
        switch (Phase)
        {
            case CardFlipPhase.Board:
                for (int i = 0; i < CardFlipBoard.Cards; i++)
                    if (Inside(ModernUi.CardFlipCardRect(i), at)) return i;
                for (int m = 0; m < CardFlipGame.Marks; m++)
                    if (Inside(ModernUi.CardFlipMarkRect(m), at)) return FirstMark + m;
                return Inside(ModernUi.CardFlipQuitRect(), at) ? QuitButton : null;
            case CardFlipPhase.Menu or CardFlipPhase.ConfirmQuit:
                return UiPointer.Hit(ModernUi.PromptButtons(sw, sh, 2), at);
            case CardFlipPhase.Reveal or CardFlipPhase.Full:
                return UiPointer.Anywhere;
            default:
                return null;
        }
    }

    public void PointAt(int index)
    {
        if (!IsActive) return;
        if (Phase == CardFlipPhase.Board && index >= 0 && index < CardFlipBoard.Cards && index != Cursor) Cursor = index;
        else if (Phase is CardFlipPhase.Menu or CardFlipPhase.ConfirmQuit && index is 0 or 1) Choice = index;
    }

    /// <summary>A memo's mark clicked puts it on or off the card under the cursor; QUIT clicked asks to quit.</summary>
    public bool Press(int index)
    {
        if (!IsActive || Phase != CardFlipPhase.Board) return false;
        if (index >= FirstMark && index < QuitButton)
        {
            Memo(index - FirstMark);
            return true;
        }
        if (index == QuitButton)
        {
            Quit();
            return true;
        }
        return false;
    }

    private static bool Inside(Raylib_cs.Rectangle r, Vector2 at) =>
        at.X >= r.X && at.X < r.X + r.Width && at.Y >= r.Y && at.Y < r.Y + r.Height;

    public void Draw(int screenWidth, int screenHeight)
    {
        if (!IsActive) return;
        ModernUi.DrawCardFlip(screenWidth, screenHeight, this, Math.Clamp(Age / AppearTime, 0f, 1f));
    }
}
