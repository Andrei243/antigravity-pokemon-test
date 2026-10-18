using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.UI;

namespace PokemonPlatinumTests;

/// <summary>
/// The Ruins of Alph's stone panels (plan 20 · J5, <see cref="AlphPuzzle"/>): sixteen pieces that begin on the ring
/// round the frame, picked up, turned and put down, the panel solved once each lies in its place upright; and the
/// screen that closes by itself once the picture is whole.
/// </summary>
public class AlphPuzzleTests
{
    [Fact]
    public void APanelBeginsWithItsSixteenPiecesOnTheRingTheSameEveryTime()
    {
        foreach (string name in AlphPuzzle.Pictures.Keys)
        {
            var puzzle = new AlphPuzzle(name);
            var again = new AlphPuzzle(name);
            var seen = new HashSet<int>();
            for (int y = 0; y < AlphPuzzle.Side; y++)
                for (int x = 0; x < AlphPuzzle.Side; x++)
                {
                    int piece = puzzle.PieceAt(x, y);
                    Assert.Equal(piece, again.PieceAt(x, y));
                    if (piece < 0) continue;
                    Assert.False(AlphPuzzle.InFrame(x, y), $"{name}: a piece begins in the frame");
                    Assert.False(AlphPuzzle.IsCorner(x, y), $"{name}: a piece begins on a corner");
                    Assert.True(seen.Add(piece));
                    Assert.Equal(again.TurnsOf(piece), puzzle.TurnsOf(piece));
                }
            Assert.Equal(AlphPuzzle.Pieces, seen.Count);
            Assert.False(puzzle.Solved);
        }
        // Two panels don't begin alike
        Assert.NotEqual(Layout(new AlphPuzzle("kabuto")), Layout(new AlphPuzzle("omanyte")));
    }

    [Fact]
    public void PiecesArePickedUpSwappedPutBackAndNeverOnACorner()
    {
        var puzzle = new AlphPuzzle("kabuto");
        var (x, y) = Where(puzzle, 0);
        puzzle.Point(x, y);
        Assert.True(puzzle.Confirm());
        Assert.Equal(0, puzzle.Held);
        Assert.Equal(-1, puzzle.PieceAt(x, y));
        // Not on a corner
        puzzle.Point(0, 0);
        Assert.False(puzzle.Confirm());
        Assert.Equal(0, puzzle.Held);
        // B puts it back where it was picked up
        Assert.True(puzzle.Cancel());
        Assert.Null(puzzle.Held);
        Assert.Equal(0, puzzle.PieceAt(x, y));
        // Down on another piece, the two change places: the other is in hand
        var (ox, oy) = Where(puzzle, 1);
        puzzle.Point(x, y);
        puzzle.Confirm();
        puzzle.Point(ox, oy);
        puzzle.Confirm();
        Assert.Equal(0, puzzle.PieceAt(ox, oy));
        Assert.Equal(1, puzzle.Held);
        // and B finds it an empty square, since its own is taken
        Assert.True(puzzle.Cancel());
        Assert.Null(puzzle.Held);
        Assert.Equal(AlphPuzzle.Pieces, Layout(puzzle).Count(p => p >= 0));
        // With nothing in hand, B leaves the panel
        Assert.False(puzzle.Cancel());
    }

    [Fact]
    public void ThePanelIsSolvedOnlyWithEveryPieceInItsPlaceUpright()
    {
        var puzzle = new AlphPuzzle("hooh");
        Solve(puzzle, upright: false);
        Assert.False(puzzle.Solved);
        // Turned upright one by one, the last turn solves it
        for (int piece = 0; piece < AlphPuzzle.Pieces; piece++)
        {
            puzzle.Point(1 + piece % AlphPuzzle.Frame, 1 + piece / AlphPuzzle.Frame);
            while (puzzle.TurnsOf(piece) != 0) puzzle.Turn(1);
        }
        Assert.True(puzzle.Solved);
        // Solved, nothing moves any more
        Assert.False(puzzle.Confirm());
        Assert.False(puzzle.Turn(1));
    }

    [Fact]
    public void TheScreenGlowsAndClosesSolvedOrIsLeftWithB()
    {
        var screen = new AlphPuzzleScreen();
        screen.Open("omanyte");
        Assert.True(screen.IsActive);
        screen.Cancel();
        Assert.False(screen.IsActive);
        Assert.False(screen.Outcome);

        screen.Open("omanyte");
        var puzzle = screen.Puzzle!;
        for (int piece = 0; piece < AlphPuzzle.Pieces; piece++)
        {
            var (x, y) = Where(puzzle, piece);
            puzzle.Point(x, y);
            screen.Confirm();
            puzzle.Point(1 + piece % AlphPuzzle.Frame, 1 + piece / AlphPuzzle.Frame);
            screen.Confirm();
            while (puzzle.TurnsOf(piece) != 0) screen.Turn(-1);
        }
        Assert.True(puzzle.Solved);
        Assert.NotNull(screen.SolvedAge);
        // The glow runs its time, the buttons do nothing meanwhile, and the screen closes solved
        screen.Cancel();
        Assert.True(screen.IsActive);
        screen.Advance(AlphPuzzleScreen.SolvedTime + 0.1f);
        Assert.False(screen.IsActive);
        Assert.True(screen.Outcome);
    }

    private static List<int> Layout(AlphPuzzle puzzle) =>
        Enumerable.Range(0, AlphPuzzle.Side * AlphPuzzle.Side).Select(c => puzzle.PieceAt(c % AlphPuzzle.Side, c / AlphPuzzle.Side)).ToList();

    private static (int X, int Y) Where(AlphPuzzle puzzle, int piece)
    {
        int cell = Layout(puzzle).IndexOf(piece);
        return (cell % AlphPuzzle.Side, cell / AlphPuzzle.Side);
    }

    // Every piece carried to its own square, upright or as it lay
    private static void Solve(AlphPuzzle puzzle, bool upright)
    {
        for (int piece = 0; piece < AlphPuzzle.Pieces; piece++)
        {
            var (x, y) = Where(puzzle, piece);
            puzzle.Point(x, y);
            puzzle.Confirm();
            puzzle.Point(1 + piece % AlphPuzzle.Frame, 1 + piece / AlphPuzzle.Frame);
            puzzle.Confirm();
            if (upright) while (puzzle.TurnsOf(piece) != 0) puzzle.Turn(1);
        }
    }
}
