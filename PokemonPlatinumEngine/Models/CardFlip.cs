using System;
using System.Collections.Generic;

namespace PokemonPlatinumEngine.Models;

// The card-flip game (plan 42 · PS3; docs/mechanics/rulings.md, "The card-flip game"): HeartGold's rules, ported in their
// order from pret/pokeheartgold's src/voltorb_flip/voltorb_flip_game.c and the round's course of voltorb_flip.c, with
// pieces and words of our own. Each function names the original's. No drawing, no input, no clock: every draw is on the
// generator a sitting is handed, as (u32)MTRandom() % n is in the original.

/// <summary>What lies under a card (<c>CardType</c>): nothing yet, a 1, a 2, a 3, or a bust (the cracked tile).</summary>
public enum CardFace : byte { None, One, Two, Three, Bust }

/// <summary>How a round ended (<c>VoltorbFlipRoundOutcome</c>), in the original's order.</summary>
public enum CardFlipOutcome : byte { None, Quit, Won, Lost }

/// <summary>
/// One of the eighty boards a round can be (<c>BoardConfig</c>): the busts, the 2s and the 3s laid on it, and the most
/// "free" multipliers (2s and 3s in a row or a column without a bust, to be had at no risk) one line and the whole board
/// may hold.
/// </summary>
public readonly record struct CardFlipConfig(int Busts, int Twos, int Threes, int MostFreeInALine, int MostFree)
{
    /// <summary>What the board pays when every multiplier is found: 2 to the 2s times 3 to the 3s.</summary>
    public int Payout
    {
        get
        {
            int p = 1;
            for (int i = 0; i < Twos; i++) p *= 2;
            for (int i = 0; i < Threes; i++) p *= 3;
            return p;
        }
    }
}

/// <summary>What one round left behind, for choosing the next level (<c>RoundSummary</c>).</summary>
public readonly record struct CardFlipRound(CardFlipOutcome Outcome, int CardsFlipped, int BoardId, int Level);

/// <summary>
/// The levels and their boards (<c>sBoardConfigs</c>, <c>sBoardIdDistribution</c>). A level is kept as the original
/// keeps it, upside down: 7 is level 1 and 0 is level 8 (<see cref="Shown"/>). A board's id is its row of
/// <see cref="Configs"/>; ten rows to a level, the second five of each with one free multiplier fewer.
/// </summary>
public static class CardFlipLevels
{
    /// <summary>How many levels there are, and how many boards.</summary>
    public const int Count = 8, Boards = 80;

    /// <summary><c>sBoardConfigs</c>, row for row: busts, 2s, 3s, the most free in a line, the most free in all.</summary>
    public static readonly CardFlipConfig[] Configs =
    {
        // Lv. 1
        new(6, 3, 1, 3, 3), new(6, 0, 3, 2, 2), new(6, 5, 0, 3, 4), new(6, 2, 2, 3, 3), new(6, 4, 1, 3, 4),
        new(6, 3, 1, 3, 3), new(6, 0, 3, 2, 2), new(6, 5, 0, 3, 4), new(6, 2, 2, 3, 3), new(6, 4, 1, 3, 4),
        // Lv. 2
        new(7, 1, 3, 2, 3), new(7, 6, 0, 3, 4), new(7, 3, 2, 2, 3), new(7, 0, 4, 2, 3), new(7, 5, 1, 3, 4),
        new(7, 1, 3, 2, 2), new(7, 6, 0, 3, 3), new(7, 3, 2, 2, 2), new(7, 0, 4, 2, 2), new(7, 5, 1, 3, 3),
        // Lv. 3
        new(8, 2, 3, 2, 3), new(8, 7, 0, 3, 4), new(8, 4, 2, 3, 4), new(8, 1, 4, 2, 3), new(8, 6, 1, 4, 3),
        new(8, 2, 3, 2, 2), new(8, 7, 0, 3, 3), new(8, 4, 2, 3, 3), new(8, 1, 4, 2, 2), new(8, 6, 1, 3, 3),
        // Lv. 4
        new(8, 3, 3, 4, 3), new(8, 0, 5, 2, 3), new(10, 8, 0, 4, 5), new(10, 5, 2, 3, 4), new(10, 2, 4, 3, 4),
        new(8, 3, 3, 3, 3), new(8, 0, 5, 2, 2), new(10, 8, 0, 4, 4), new(10, 5, 2, 3, 3), new(10, 2, 4, 3, 3),
        // Lv. 5
        new(10, 7, 1, 4, 5), new(10, 4, 3, 3, 4), new(10, 1, 5, 3, 4), new(10, 9, 0, 4, 5), new(10, 6, 2, 4, 5),
        new(10, 7, 1, 4, 4), new(10, 4, 3, 3, 3), new(10, 1, 5, 3, 3), new(10, 9, 0, 4, 4), new(10, 6, 2, 4, 4),
        // Lv. 6
        new(10, 3, 4, 3, 4), new(10, 0, 6, 3, 4), new(10, 8, 1, 4, 5), new(10, 5, 3, 4, 5), new(10, 2, 5, 3, 4),
        new(10, 3, 4, 3, 3), new(10, 0, 6, 3, 3), new(10, 8, 1, 4, 4), new(10, 5, 3, 4, 4), new(10, 2, 5, 3, 3),
        // Lv. 7
        new(10, 7, 2, 4, 5), new(10, 4, 4, 4, 5), new(13, 1, 6, 3, 4), new(13, 9, 1, 5, 6), new(10, 6, 3, 4, 5),
        new(10, 7, 2, 4, 4), new(10, 4, 4, 4, 4), new(13, 1, 6, 3, 3), new(13, 9, 1, 5, 5), new(10, 6, 3, 4, 4),
        // Lv. 8
        new(10, 0, 7, 3, 4), new(10, 8, 2, 5, 6), new(10, 5, 4, 4, 5), new(10, 2, 6, 4, 5), new(10, 7, 3, 5, 6),
        new(10, 0, 7, 3, 3), new(10, 8, 2, 5, 5), new(10, 5, 4, 4, 4), new(10, 2, 6, 4, 4), new(10, 7, 3, 5, 5)
    };

    /// <summary>
    /// <c>sBoardIdDistribution</c>: for each level as kept (0 is level 8), the running share out of a hundred at which
    /// each board stops being drawn. Every level gives its own ten boards ten each, so the table is written out by rule.
    /// </summary>
    public static readonly byte[][] Distribution = MakeDistribution();

    private static byte[][] MakeDistribution()
    {
        var table = new byte[Count][];
        for (int level = 0; level < Count; level++)
        {
            table[level] = new byte[Boards];
            int first = (Count - 1 - level) * 10;
            for (int k = 0; k < 10; k++) table[level][first + k] = (byte)((k + 1) * 10);
        }
        return table;
    }

    /// <summary>The level as the player sees it (1 to 8) for one as kept (7 to 0).</summary>
    public static int Shown(int kept) => Count - kept;

    /// <summary>The level as the player sees it that a board belongs to.</summary>
    public static int ShownOfBoard(int boardId) => boardId / 10 + 1;

    /// <summary><c>LEVEL_AT_LEAST</c>: whether a board is of at least that shown level.</summary>
    public static bool AtLeast(int boardId, int shownLevel) => boardId >= 10 * (shownLevel - 1);

    /// <summary>
    /// <c>VoltorbFlipGameState_SelectBoardId</c>'s draw: the first board whose running share is over the draw out of a
    /// hundred, for the level as kept.
    /// </summary>
    public static int BoardFor(int keptLevel, int drawOutOfHundred)
    {
        var row = Distribution[keptLevel];
        int i;
        for (i = 0; i < Boards; i++)
            if (drawOutOfHundred < row[i]) break;
        return i;
    }

    /// <summary>
    /// <c>VoltorbFlipGameState_CalcNextLevel</c>: the next level (as kept) from the five rounds remembered, a ring whose
    /// next free place is <paramref name="head"/>. Its branches in their order: a win at level 8 stays at 8; from level 5
    /// up, five rounds none lost with eight cards or more turned each reach 8; then from 7 down to 2, a round at that
    /// level or above with that many cards turned, or a win one level lower, reaches it; else level 1. Rounds never
    /// played count as nothing turned, and a quit counts as not lost.
    /// </summary>
    public static int NextLevel(IReadOnlyList<CardFlipRound> history, int head)
    {
        var prev = history[head == 0 ? 4 : head - 1];
        var outcome = prev.Outcome;
        if (outcome == CardFlipOutcome.Won && AtLeast(prev.BoardId, 8)) return 0;

        int boardId = prev.BoardId;
        if (AtLeast(boardId, 5))
        {
            int i;
            for (i = 0; i < 5; i++)
            {
                var round = history[i];
                if (round.CardsFlipped < 8 || round.Outcome == CardFlipOutcome.Lost) break;
            }
            if (i == 5) return 0;
        }

        bool won = outcome == CardFlipOutcome.Won;
        if (AtLeast(boardId, 7) && prev.CardsFlipped >= 7 || AtLeast(boardId, 6) && won) return 1;
        if (AtLeast(boardId, 6) && prev.CardsFlipped >= 6 || AtLeast(boardId, 5) && won) return 2;
        if (AtLeast(boardId, 5) && prev.CardsFlipped >= 5 || AtLeast(boardId, 4) && won) return 3;
        if (AtLeast(boardId, 4) && prev.CardsFlipped >= 4 || AtLeast(boardId, 3) && won) return 4;
        if (AtLeast(boardId, 3) && prev.CardsFlipped >= 3 || AtLeast(boardId, 2) && won) return 5;
        if (AtLeast(boardId, 2) && prev.CardsFlipped >= 2 || won) return 6;
        return 7;
    }
}

/// <summary>
/// Twenty-five cards in five rows of five, numbered from the top left along each row (<c>CardID</c>), with what each
/// line adds up to and how many busts it hides.
/// </summary>
public sealed class CardFlipBoard
{
    public const int Side = 5, Cards = Side * Side, Tries = 1000, PlacingAttempts = 100;

    /// <summary>The most a round pays (<c>MultiplyPayoutAndUpdateCardsFlipped</c>, <c>CalcBoardMaxPayout</c>).</summary>
    public const int MostPayout = 50000;

    private readonly CardFace[] faces = new CardFace[Cards];
    private readonly bool[] flipped = new bool[Cards];
    private readonly int[] memo = new int[Cards];
    private readonly int[] pointsInRow = new int[Side], pointsInColumn = new int[Side];
    private readonly int[] bustsInRow = new int[Side], bustsInColumn = new int[Side];

    public CardFlipConfig Config { get; }

    /// <summary>How many of the thousand draws it took (the last kept as it fell when none held the limits).</summary>
    public int Draws { get; private set; }

    /// <summary>Whether the board kept holds its configuration's limits (<see cref="Breaks"/> false).</summary>
    public bool HoldsItsLimits { get; private set; }

    /// <summary>What every card on it multiplied together pays (<c>CalcBoardMaxPayout</c>).</summary>
    public int MaxPayout { get; private set; }

    /// <summary>How many 2s and 3s lie on it (<c>CountMultiplierCards</c>).</summary>
    public int MultiplierCards { get; private set; }

    private CardFlipBoard(CardFlipConfig config) { Config = config; }

    public CardFace Face(int card) => faces[card];
    public bool IsFlipped(int card) => flipped[card];
    public int PointsInRow(int row) => pointsInRow[row];
    public int PointsInColumn(int column) => pointsInColumn[column];
    public int BustsInRow(int row) => bustsInRow[row];
    public int BustsInColumn(int column) => bustsInColumn[column];

    /// <summary>The memo's marks on a card: bit 1 a bust, 2 a 1, 4 a 2, 8 a 3.</summary>
    public int Memo(int card) => memo[card];

    /// <summary><c>IsCardMemoFlagOn</c> for mark 0 (a bust) to 3.</summary>
    public bool HasMark(int card, int mark) => (memo[card] & (1 << mark)) != 0;

    /// <summary><c>GetCardValueFromType</c>: a card's number, nought for a bust or nothing.</summary>
    public static int ValueOf(CardFace face) => face switch { CardFace.One => 1, CardFace.Two => 2, CardFace.Three => 3, _ => 0 };

    /// <summary><c>IS_MULTIPLIER_CARD</c>.</summary>
    public static bool IsMultiplier(CardFace face) => face is CardFace.Two or CardFace.Three;

    /// <summary>
    /// <c>VoltorbFlipGameState_GenerateBoard</c>, then <c>CountPointsInRowCols</c>, <c>CalcBoardMaxPayout</c> and
    /// <c>CountMultiplierCards</c> as <c>NewBoard</c> calls them: up to a thousand times every card a 1, the busts, the
    /// 2s and the 3s laid on 1s, the busts counted, and done once the limits hold; after the thousandth the last is kept.
    /// </summary>
    public static CardFlipBoard Deal(CardFlipConfig config, Func<int, int> next)
    {
        var board = new CardFlipBoard(config);
        for (int i = 0; i < Tries; i++)
        {
            board.Draws = i + 1;
            board.Place(CardFace.One, Cards, false, next);
            board.Place(CardFace.Bust, config.Busts, true, next);
            board.Place(CardFace.Two, config.Twos, true, next);
            board.Place(CardFace.Three, config.Threes, true, next);
            board.CountBusts();
            board.HoldsItsLimits = !board.Breaks();
            if (board.HoldsItsLimits) break;
        }
        board.CountPoints();
        board.MaxPayout = board.CalcMaxPayout();
        board.MultiplierCards = 0;
        for (int i = 0; i < Cards; i++)
            if (IsMultiplier(board.faces[i])) board.MultiplierCards++;
        return board;
    }

    /// <summary>
    /// <c>VoltorbFlipGameState_PlaceCardsOnBoard</c>: <paramref name="n"/> cards of a kind, each on a card drawn at random
    /// that is still a 1; a draw on another kind tries again, and after a hundred such misses in all the rest are left
    /// out. Laying the 1s draws nothing: card <c>i</c> takes the <c>i</c>th.
    /// </summary>
    private void Place(CardFace face, int n, bool drawn, Func<int, int> next)
    {
        int attempts = 0;
        int i = 0;
        while (i < n)
        {
            int card = drawn ? next(Cards) : i;
            if (faces[card] == CardFace.One || !drawn) faces[card] = face;
            else
            {
                attempts++;
                if (attempts >= PlacingAttempts) break;
                i--;
            }
            i++;
        }
    }

    /// <summary><c>VoltorbFlipGameState_CountVoltorbsInRowCols</c>.</summary>
    private void CountBusts()
    {
        for (int r = 0; r < Side; r++)
        {
            bustsInRow[r] = 0;
            for (int c = 0; c < Side; c++)
                if (faces[r * Side + c] == CardFace.Bust) bustsInRow[r]++;
        }
        for (int c = 0; c < Side; c++)
        {
            bustsInColumn[c] = 0;
            for (int r = 0; r < Side; r++)
                if (faces[r * Side + c] == CardFace.Bust) bustsInColumn[c]++;
        }
    }

    /// <summary><c>VoltorbFlipGameState_CountPointsInRowCols</c>.</summary>
    private void CountPoints()
    {
        for (int r = 0; r < Side; r++)
        {
            pointsInRow[r] = 0;
            for (int c = 0; c < Side; c++) pointsInRow[r] += ValueOf(faces[r * Side + c]);
        }
        for (int c = 0; c < Side; c++)
        {
            pointsInColumn[c] = 0;
            for (int r = 0; r < Side; r++) pointsInColumn[c] += ValueOf(faces[r * Side + c]);
        }
    }

    /// <summary><c>VoltorbFlipGameState_CalcBoardMaxPayout</c>.</summary>
    private int CalcMaxPayout()
    {
        int payout = 1;
        for (int i = 0; i < Cards; i++)
            if (faces[i] != CardFace.Bust) payout *= ValueOf(faces[i]);
        return Math.Min(payout, MostPayout);
    }

    /// <summary>
    /// The free multipliers (<c>VoltorbFlipGameState_RetryBoardGen</c>): a 2 or a 3 whose row or column hides no bust,
    /// counted for its row, its column and the whole board.
    /// </summary>
    public (int Total, int[] InRows, int[] InColumns) FreeMultipliers()
    {
        var rows = new int[Side];
        var columns = new int[Side];
        int total = 0;
        for (int i = 0; i < Cards; i++)
        {
            if (!IsMultiplier(faces[i])) continue;
            int column = i % Side, row = i / Side;
            if (bustsInRow[row] == 0 || bustsInColumn[column] == 0)
            {
                columns[column]++;
                rows[row]++;
                total++;
            }
        }
        return (total, rows, columns);
    }

    /// <summary>
    /// <c>VoltorbFlipGameState_RetryBoardGen</c>: true when the board must be drawn again, the free multipliers reaching
    /// the most allowed on the whole board or in any one line.
    /// </summary>
    public bool Breaks()
    {
        var (total, rows, columns) = FreeMultipliers();
        if (Config.MostFree <= total) return true;
        for (int i = 0; i < Side; i++)
            if (Config.MostFreeInALine <= columns[i] || Config.MostFreeInALine <= rows[i]) return true;
        return false;
    }

    /// <summary><c>VoltorbFlipGameState_FlipCard</c>: turned face up, its marks wiped.</summary>
    internal void Turn(int card)
    {
        flipped[card] = true;
        memo[card] = 0;
    }

    /// <summary>Every card face up, for the look after a round (the original's reveal, which changes no rule).</summary>
    internal void TurnAll()
    {
        for (int i = 0; i < Cards; i++) flipped[i] = true;
    }

    /// <summary><c>VoltorbFlipGameState_ToggleCardMemo</c>.</summary>
    internal void ToggleMemo(int card, int flag)
    {
        if ((memo[card] & flag) != 0) memo[card] -= flag;
        else memo[card] |= flag;
    }

    /// <summary><c>VoltorbFlipGameState_CountFlippedCardsAlongAxis</c> for a row.</summary>
    public int FlippedInRow(int row)
    {
        int n = 0;
        for (int c = 0; c < Side; c++) if (flipped[row * Side + c]) n++;
        return n;
    }

    /// <summary><c>VoltorbFlipGameState_CountFlippedCardsAlongAxis</c> for a column.</summary>
    public int FlippedInColumn(int column)
    {
        int n = 0;
        for (int r = 0; r < Side; r++) if (flipped[r * Side + column]) n++;
        return n;
    }
}

/// <summary>
/// A sitting at the card-flip table (<c>VoltorbFlipGameState</c> with the round's course of <c>voltorb_flip.c</c>):
/// a round is dealt at the level the last five rounds earn, each card turned multiplies the round's coins, a bust loses
/// them, quitting keeps them, and finding every 2 and 3 wins the round. A sitting keeps nothing but the coins it adds:
/// the five rounds remembered go when it ends, as HeartGold keeps nothing.
/// </summary>
public sealed class CardFlipGame
{
    /// <summary>The memo's four marks, as the bits <c>ToggleCardMemo</c> keeps: 0 a bust, 1 to 3 the numbers.</summary>
    public const int Marks = 4;

    private readonly Func<int, int> next;
    private readonly CardFlipRound[] history = new CardFlipRound[5];
    private int historyHead;
    private bool playedOne;

    /// <summary>A sitting whose draws are <paramref name="next"/>(n), a number from 0 to n − 1.</summary>
    public CardFlipGame(Func<int, int> next, int coins)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
        Coins = Math.Clamp(coins, 0, CoinCase.Most);
        Started = Coins;
    }

    /// <summary>A sitting drawing on a generator the caller made (never <c>Dice</c>'s by itself).</summary>
    public CardFlipGame(Random random, int coins) : this(random.Next, coins) { }

    /// <summary>The coins in the case, the round's added when it is won or quit.</summary>
    public int Coins { get; private set; }

    /// <summary>The coins the sitting began with.</summary>
    public int Started { get; }

    /// <summary>The board of the round in play or just ended; null before the first deal.</summary>
    public CardFlipBoard? Board { get; private set; }

    /// <summary>The board's row of <see cref="CardFlipLevels.Configs"/>.</summary>
    public int BoardId { get; private set; }

    /// <summary>The level as kept, 7 (level 1) to 0 (level 8).</summary>
    public int Level { get; private set; } = 7;

    /// <summary>The level as the player sees it.</summary>
    public int ShownLevel => CardFlipLevels.Shown(Level);

    /// <summary>The round's coins so far.</summary>
    public int Payout { get; private set; }

    public int CardsFlipped { get; private set; }
    public int MultipliersFlipped { get; private set; }
    public CardFlipOutcome Outcome { get; private set; }

    /// <summary>Whether a round is dealt and still going.</summary>
    public bool InPlay => Board != null && Outcome == CardFlipOutcome.None;

    /// <summary>Whether the case can take no more (<c>VoltorbFlipTaskEngine_InitChecks_Begin</c>: no round is dealt then).</summary>
    public bool CaseFull => Coins >= CoinCase.Most;

    /// <summary>The five rounds remembered, as a ring.</summary>
    public IReadOnlyList<CardFlipRound> History => history;
    public int HistoryHead => historyHead;

    /// <summary>
    /// <c>VoltorbFlipGameState_CalculateLevelsGained</c>: the levels the last round moved the player up (down if under
    /// nought), nought before any round has been played.
    /// </summary>
    public int LevelsGained => playedOne ? history[historyHead == 0 ? 4 : historyHead - 1].Level - Level : 0;

    /// <summary>
    /// <c>VoltorbFlipGameState_NewBoard</c>: the level chosen from the rounds remembered, a board drawn among the level's
    /// (a draw out of a hundred), and the board dealt. Nothing if a round is still going or the case is full.
    /// </summary>
    public bool Deal()
    {
        if (InPlay || CaseFull) return false;
        if (Board != null) EndRound();
        // VoltorbFlipGameState_SelectBoardId: the draw out of a hundred comes before CalcNextLevel's reckoning
        int draw = next(100);
        Level = CardFlipLevels.NextLevel(history, historyHead);
        BoardId = CardFlipLevels.BoardFor(Level, draw);
        Board = CardFlipBoard.Deal(CardFlipLevels.Configs[BoardId], next);
        Payout = 0;
        CardsFlipped = 0;
        MultipliersFlipped = 0;
        Outcome = CardFlipOutcome.None;
        return true;
    }

    /// <summary>
    /// A card turned (<c>VoltorbFlipGameState_FlipCard</c>, then <c>MultiplyPayoutAndUpdateCardsFlipped</c> for a
    /// number): a bust loses the round's coins; the first number sets them, every one after multiplies them, at most
    /// 50,000; once they come to what the board pays the round is won and the coins go into the case. A card already up
    /// does nothing and gives <see cref="CardFace.None"/>.
    /// </summary>
    public CardFace Flip(int card)
    {
        if (!InPlay || Board!.IsFlipped(card)) return CardFace.None;
        var face = Board.Face(card);
        Board.Turn(card);
        if (face == CardFace.Bust)
        {
            Outcome = CardFlipOutcome.Lost;
            return face;
        }
        CardsFlipped++;
        if (CardFlipBoard.IsMultiplier(face)) MultipliersFlipped++;
        int value = CardFlipBoard.ValueOf(face);
        Payout = Payout == 0 ? value : Math.Min(Payout * value, CardFlipBoard.MostPayout);
        // VoltorbFlipTaskEngine_CardFlipEffect_End: HasEarnedMaxPayout wins the round
        if (Payout == Board.MaxPayout)
        {
            Outcome = CardFlipOutcome.Won;
            Collect();
        }
        return face;
    }

    /// <summary><c>VoltorbFlipGameState_ToggleCardMemo</c>: a mark (0 a bust, 1 to 3 the numbers) on or off a card face down.</summary>
    public bool ToggleMemo(int card, int mark)
    {
        if (!InPlay || mark < 0 || mark >= Marks || Board!.IsFlipped(card)) return false;
        Board.ToggleMemo(card, 1 << mark);
        return true;
    }

    /// <summary><c>VoltorbFlipTaskEngine_QuitRound_Run</c>'s yes: the round given up with its coins kept.</summary>
    public bool Quit()
    {
        if (!InPlay) return false;
        Outcome = CardFlipOutcome.Quit;
        Collect();
        return true;
    }

    /// <summary>
    /// <c>VoltorbFlip_AwardPayoutToPlayer</c>: the round's coins into the case (<c>Coins_Add</c>, at most
    /// <see cref="CoinCase.Most"/>).
    /// </summary>
    private void Collect()
    {
        Coins = Math.Min(CoinCase.Most, Coins + Payout);
    }

    /// <summary>The coins the sitting has won (or, never, lost) so far: the case now against the case at the start.</summary>
    public int Net => Coins - Started;

    /// <summary>
    /// <c>VoltorbFlipGameState_UpdateHistoryAndReset</c>: the ended round remembered in the ring and the board cleared.
    /// <see cref="Deal"/> calls it for a round left ended; the board stays for its look until then.
    /// </summary>
    public void EndRound()
    {
        if (Board == null || Outcome == CardFlipOutcome.None) return;
        // VoltorbFlipGameState_PushBoardHistory
        history[historyHead] = new CardFlipRound(Outcome, CardsFlipped, BoardId, Level);
        historyHead = (historyHead + 1) % 5;
        playedOne = true;
        Board = null;
        Payout = 0;
        CardsFlipped = 0;
        MultipliersFlipped = 0;
        Outcome = CardFlipOutcome.None;
    }

    /// <summary>Every card turned face up for the look after a round (the reveal, which changes no rule).</summary>
    public void Reveal()
    {
        if (Board != null && Outcome != CardFlipOutcome.None) Board.TurnAll();
    }
}
